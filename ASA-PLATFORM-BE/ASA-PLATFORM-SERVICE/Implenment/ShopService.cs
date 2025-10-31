using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.DTOs.Common;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using ASA_PLATFORM_SERVICE.Interface;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using static ASA_PLATFORM_REPO.Repository.ShopRepo;
using ASA_PLATFORM_SERVICE.DTOs.Response;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class ShopService : IShopService
    {
        private readonly HttpClient _httpClient;
        private readonly ShopRepo _shopRepo;
        private readonly IMapper _mapper;
        private readonly OrderRepo _orderRepo;
        private readonly IConfiguration _configuration;
		private readonly IEmailService _emailService;
        private readonly ProductRepo _productRepo;

		public ShopService(ShopRepo shopRepo, IMapper mapper, OrderRepo orderRepo, IConfiguration configuration, IHttpClientFactory httpClientFactory, IEmailService emailService, ProductRepo productRepo)
        {
            _shopRepo = shopRepo;
            _mapper = mapper;
            _orderRepo = orderRepo;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("BETenantUrl");
			_emailService = emailService;
            _productRepo = productRepo;
        }

        public async Task<ApiResponse<ShopResponse>> CreateAsync(ShopRequest request)
        {
            try
            {
                // 1. Map sang entity
                var entity = _mapper.Map<Shop>(request);
                

                // Validate and normalize Vietnamese phone number
                string normalizedPhone;
                if (!TryNormalizeVietnamPhoneNumber(request.Phonenumber, out normalizedPhone))
                {
                    return new ApiResponse<ShopResponse>
                    {
                        Success = false,
                        Message = "Invalid Vietnamese phone number format",
                        Data = null
                    };
                }

                // Check duplicate phone in Shop table and set normalized phone on entity
                var shopPhoneExists = await _shopRepo
                    .GetFiltered(new Shop { Phonenumber = normalizedPhone })
                    .AnyAsync(s => s.Phonenumber == normalizedPhone);
                if (shopPhoneExists)
                {
                    return new ApiResponse<ShopResponse>
                    {
                        Success = false,
                        Message = "Phone number already exists in shop",
                        Data = null
                    };
                }
                entity.Phonenumber = normalizedPhone;

                // Check duplicate email in Shop table (if email is provided)
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var shopsWithEmail = await _shopRepo
                        .GetFiltered(new Shop { Email = request.Email })
                        .Where(s => !string.IsNullOrEmpty(s.Email))
                        .ToListAsync();
                    
                    var shopEmailExists = shopsWithEmail.Any(s => s.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));
                    if (shopEmailExists)
                    {
                        return new ApiResponse<ShopResponse>
                        {
                            Success = false,
                            Message = "Email already exists in shop",
                            Data = null
                        };
                    }
                }

                // 2. Gọi API Tenant trước để hạn chế lỗi phía Platform
                var tenantApiUrl = _configuration.GetValue<string>("BETenantUrl:Url");
                var createShopEndpoint = $"{tenantApiUrl}/api/shops";

                var tenantResponse = await _httpClient.PostAsJsonAsync(createShopEndpoint, new
                {
                    shopName = request.shopName,
                    address = request.address,
                    shopToken = request.ShopToken ?? "",
                    status = request.status ?? 2, // Sử dụng status từ request, mặc định là 2 (trial)
                    qrcodeUrl = request.QrcodeUrl,
                    sepayApiKey = request.SepayApiKey ?? "", 
                    currentRequest = 0, // Luôn default là 0
                    currentAccount = 0, // Luôn default là 0
                    bankName = request.BankName,
                    bankCode = request.BankCode,
                    bankNum = request.BankNum,
                    productId = request.ProductId,
                    username = normalizedPhone
                });

                if (!tenantResponse.IsSuccessStatusCode)
                {
                    return new ApiResponse<ShopResponse>
                    {
                        Success = false,
                        Message = $"Tenant API failed: {tenantResponse.StatusCode}",
                        Data = null
                    };
                }

                // 3. Lưu shop vào DB Platform (sau khi Tenant OK)
                var affected = await _shopRepo.CreateAsync(entity);
                if (affected <= 0)
                {
                    return new ApiResponse<ShopResponse>
                    {
                        Success = false,
                        Message = "Create failed in Platform DB",
                        Data = null
                    };
                }

                // 4. Trả về kết quả
                var shopResponse = _mapper.Map<ShopResponse>(entity);
                
                // Áp dụng logic cho trial shops
                if (entity.Status == 2) // Trial shop
                {
                    shopResponse.productType = "Gói Basic";
                    shopResponse.expiredAt = entity.CreatedAt?.AddDays(7);
                }
                
                // Đọc thông tin đăng nhập từ Tenant API response (nếu có)
                var tenantRaw = await tenantResponse.Content.ReadAsStringAsync();
				if (TryParseTenantCredentials(tenantRaw, out var adminUsername, out var adminPassword))
                {
                    shopResponse.Username = adminUsername;
                    shopResponse.Password = adminPassword;

					// Gửi email chào mừng với thông tin đăng nhập (chỉ cho shop dùng thử)
					if (entity.Status == 2) // Status = 2 là trial/dùng thử
					{
						var displayName = !string.IsNullOrWhiteSpace(request.Fullname) ? request.Fullname : (!string.IsNullOrWhiteSpace(request.shopName) ? request.shopName : normalizedPhone);
						if (!string.IsNullOrWhiteSpace(request.Email))
						{
							try
							{
								await _emailService.SendWelcomeEmailAsync(request.Email, displayName, adminUsername, adminPassword);
							}
							catch { /* ignore email errors to not block creation flow */ }
						}
					}
                }
                return new ApiResponse<ShopResponse>
                {
                    Success = true,
                    Message = "Create successfully",
                    Data = shopResponse
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ShopResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            try
            {
                var existing = await _shopRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Shop not found",
                        Data = false
                    };

                var affected = await _shopRepo.RemoveAsync(existing);
                return new ApiResponse<bool>
                {
                    Success = affected,
                    Message = affected ? "Deleted successfully" : "Delete failed",
                    Data = affected
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = false
                };
            }
        }

        public async Task<PagedResponse<ShopResponse>> GetFilteredShopsAsync(ShopGetRequest filterDto, int page, int pageSize)
        {
            var filter = _mapper.Map<Shop>(filterDto);
            var query = _shopRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var shopResponses = _mapper.Map<List<ShopResponse>>(items);

            foreach (var shopResponse in shopResponses)
            {
                var currentOrder = await GetCurrentShopProduct(shopResponse.shopId);
                if (currentOrder != null)
                {
                    shopResponse.productType = currentOrder.Product?.ProductName; 
                    shopResponse.expiredAt = currentOrder.ExpiredAt;
                }
                else if (shopResponse.status == 2) // Trial shop
                {
                    shopResponse.productType = "Gói Basic";
                    shopResponse.expiredAt = shopResponse.createdAt?.AddDays(7);
                }
            }

            return new PagedResponse<ShopResponse>
            {
                Items = shopResponses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<ShopExpiryResponse>> GetTopExpiringShopsAsync(int top = 5)
        {
            var now = DateTime.UtcNow;

            var allShops = await _shopRepo.GetAllAsync();
            var activeOrTrialShops = allShops.Where(s => s.Status == 1 || s.Status == 2).ToList();

            var results = new List<(Shop shop, Order? order, DateTime? expiredAt, string packageName, decimal? price)>();

            foreach (var shop in activeOrTrialShops)
            {
                var currentOrder = await _orderRepo.GetCurrentShopProduct(shop.ShopId);

                DateTime? expiredAt = null;
                string packageName = null;
                decimal? packagePrice = null;

                if (currentOrder != null)
                {
                    packageName = currentOrder.Product?.ProductName;
                    packagePrice = currentOrder.Product?.Price;
                    expiredAt = currentOrder.ExpiredAt;
                }
                else if (shop.Status == 2)
                {
                    // Trial logic: default 7 days from createdAt
                    packageName = "Gói Basic - Trial";
                    expiredAt = shop.CreatedAt?.AddDays(7);
                    packagePrice = 0;
                }

                if (expiredAt.HasValue)
                {
                    results.Add((shop, currentOrder, expiredAt, packageName ?? string.Empty, packagePrice));
                }
            }

            var topShops = results
                .OrderBy(r => (r.expiredAt.HasValue ? (r.expiredAt.Value - now).TotalDays : double.MaxValue))
                .ThenBy(r => r.shop.ShopId)
                .Take(top)
                .Select(r => new ShopExpiryResponse
                {
                    ShopName = r.shop.ShopName,
                    OwnerName = r.shop.Fullname,
                    CurrentPackageName = r.packageName,
                    CurrentPackagePrice = r.price,
                    ExpiredAt = r.expiredAt.HasValue 
                        ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(r.expiredAt.Value, DateTimeKind.Utc), TimeZoneInfo.Local).ToString("dd-MM-yyyy") 
                        : null
                })
                .ToList();

            return topShops;
        }

        public async Task<ApiResponse<ShopResponse>> UpdateAsync(long id, ShopRequest request)
        {
            try
            {
                var existing = await _shopRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<ShopResponse>
                    {
                        Success = false,
                        Message = "Shop not found",
                        Data = null
                    };

                // Map dữ liệu từ DTO sang entity, bỏ Id
                _mapper.Map(request, existing);

                var affected = await _shopRepo.UpdateAsync(existing);
                if (affected > 0)
                {
                    var response = _mapper.Map<ShopResponse>(existing);
                    
                    // Áp dụng logic cho trial shops
                    if (existing.Status == 2) // Trial shop
                    {
                        response.productType = "Gói Basic";
                        response.expiredAt = existing.CreatedAt?.AddDays(7);
                    }
                    
                    return new ApiResponse<ShopResponse>
                    {
                        Success = true,
                        Message = "Update successfully",
                        Data = response
                    };
                }

                return new ApiResponse<ShopResponse>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ShopResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }
        public async Task<Shop?> GetShopById(long id)
        {
            return await _shopRepo.GetByIdAsync(id);
        }

        private async Task<Order?> GetCurrentShopProduct (long shopId)
        {
           return await _orderRepo.GetCurrentShopProduct(shopId);
        }

        private class TenantCreateShopResponse
        {
            public string CreatedAdminUsername { get; set; }
            public string CreatedAdminPassword { get; set; }
        }

        private class TenantCreateShopEnvelope
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public TenantCreateShopResponse Data { get; set; }
        }

        private static bool TryParseTenantCredentials(string json, out string username, out string password)
        {
            username = null;
            password = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var envelope = JsonSerializer.Deserialize<TenantCreateShopEnvelope>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (envelope?.Data != null &&
                    !string.IsNullOrEmpty(envelope.Data.CreatedAdminUsername) &&
                    !string.IsNullOrEmpty(envelope.Data.CreatedAdminPassword))
                {
                    username = envelope.Data.CreatedAdminUsername;
                    password = envelope.Data.CreatedAdminPassword;
                    return true;
                }
            }
            catch { /* ignore and try flat */ }

            try
            {
                var flat = JsonSerializer.Deserialize<TenantCreateShopResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (flat != null && !string.IsNullOrEmpty(flat.CreatedAdminUsername) && !string.IsNullOrEmpty(flat.CreatedAdminPassword))
                {
                    username = flat.CreatedAdminUsername;
                    password = flat.CreatedAdminPassword;
                    return true;
                }
            }
            catch { /* ignore */ }

            return false;
        }

        private static bool TryNormalizeVietnamPhoneNumber(string input, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var phone = input.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);

            if (phone.StartsWith("+84"))
            {
                phone = "0" + phone.Substring(3);
            }
            else if (phone.StartsWith("84"))
            {
                phone = "0" + phone.Substring(2);
            }

            if (Regex.IsMatch(phone, @"^0(3|5|7|8|9)\d{8}$"))
            {
                normalized = phone;
                return true;
            }

            return false;
        }

        public Task<List<ShopTrialDto>> CheckTrialShops()
        {
            return _shopRepo.CheckTrialShops();
        }

        public async Task<int> UpdateExpiredShopStatusAsync()
        {
            try
            {
                Console.WriteLine($"[ShopService] Bắt đầu kiểm tra trạng thái shop hết hạn...");

                var currentTime = DateTime.UtcNow;

                // Lấy tất cả shop có status = 1 (active)
                var activeShops = await _shopRepo.GetFiltered(new Shop { Status = 1 }).ToListAsync();

                Console.WriteLine($"[ShopService] Tìm thấy {activeShops.Count} shop đang active");

                int updatedCount = 0;

                foreach (var shop in activeShops)
                {
                    try
                    {
                        // Lấy order mới nhất của shop này
                        var latestOrder = await _orderRepo.GetCurrentShopProduct(shop.ShopId);

                        if (latestOrder != null && latestOrder.ExpiredAt.HasValue)
                        {
                            // Kiểm tra nếu ExpiredAt đã qua so với hiện tại
                            if (latestOrder.ExpiredAt.Value < currentTime)
                            {
                                // Cập nhật status của shop thành 0 (inactive)
                                shop.Status = 0;
                                shop.UpdatedAt = currentTime;
                                
                                await _shopRepo.UpdateAsync(shop);
                                updatedCount++;
                                Console.WriteLine($"[ShopService] Đã cập nhật shop ID {shop.ShopId} (ShopName: {shop.ShopName}) thành inactive. Order mới nhất hết hạn lúc: {latestOrder.ExpiredAt:yyyy-MM-dd HH:mm:ss}");
                            }
                            else
                            {
                                Console.WriteLine($"[ShopService] Shop ID {shop.ShopId} (ShopName: {shop.ShopName}) vẫn còn hiệu lực. Order mới nhất hết hạn lúc: {latestOrder.ExpiredAt:yyyy-MM-dd HH:mm:ss}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ShopService] Shop ID {shop.ShopId} (ShopName: {shop.ShopName}) không có order hoặc order không có ExpiredAt");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ShopService] Lỗi khi xử lý shop ID {shop.ShopId}: {ex.Message}");
                    }
                }

                Console.WriteLine($"[ShopService] Hoàn thành. Đã cập nhật {updatedCount} shop thành inactive");
                return updatedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShopService] Lỗi tổng quát: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> UpdateExpiredTrialShopStatusAsync()
        {
            try
            {
                Console.WriteLine($"[ShopService] Bắt đầu kiểm tra trạng thái shop trial (status = 2) hết hạn...");

                var currentTime = DateTime.UtcNow;
                var sevenDaysAgo = currentTime.AddDays(-7);

                // Lấy tất cả shop có status = 2 (trial) và CreatedAt <= 7 ngày trước
                var expiredTrialShops = await _shopRepo.GetFiltered(new Shop { Status = 2 })
                    .Where(s => s.CreatedAt.HasValue && s.CreatedAt.Value <= sevenDaysAgo)
                    .ToListAsync();

                Console.WriteLine($"[ShopService] Tìm thấy {expiredTrialShops.Count} shop trial đã hết hạn");

                int updatedCount = 0;

                // Cập nhật tất cả bằng 1 lần batch update
                foreach (var shop in expiredTrialShops)
                {
                    try
                    {
                        var daysPassed = (currentTime - shop.CreatedAt.Value).TotalDays;
                        shop.Status = 0;
                        shop.UpdatedAt = currentTime;
                        
                        await _shopRepo.UpdateAsync(shop);
                        updatedCount++;
                        
                        Console.WriteLine($"[ShopService] Shop trial ID {shop.ShopId} ({shop.ShopName}) -> inactive. Đã {daysPassed:F1} ngày");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ShopService] Lỗi khi cập nhật shop ID {shop.ShopId}: {ex.Message}");
                    }
                }

                Console.WriteLine($"[ShopService] Hoàn thành. Đã cập nhật {updatedCount} shop trial");
                return updatedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShopService] Lỗi tổng quát: {ex.Message}");
                return 0;
            }
        }

        public async Task<object> GetShopCountByPackageAsync()
        {
            try
            {
                // Lấy tất cả products từ database
                var allProducts = await _productRepo.GetAllAsync();
                
                // Lấy tất cả shop ĐANG HOẠT ĐỘNG (status = 1 hoặc 2)
                var allShops = await _shopRepo.GetAllAsync();
                var activeShops = allShops.Where(s => s.Status == 1 || s.Status == 2).ToList();
                
                // Tạo dictionary để lưu count cho mỗi package
                var packageCounts = new Dictionary<string, int>();
                
                // Initialize tất cả products với count = 0
                foreach (var product in allProducts)
                {
                    if (!string.IsNullOrEmpty(product.ProductName))
                    {
                        packageCounts[product.ProductName] = 0;
                    }
                }
                
                // Đếm số shop cho mỗi product (chỉ tính shop đang hoạt động)
                foreach (var shop in activeShops)
                {
                    var currentOrder = await _orderRepo.GetCurrentShopProduct(shop.ShopId);
                    var now = DateTime.UtcNow;
                    
                    if (currentOrder != null && currentOrder.Product != null)
                    {
                        // Kiểm tra order chưa hết hạn (nếu có ExpiredAt)
                        bool isOrderValid = true;
                        if (currentOrder.ExpiredAt.HasValue && currentOrder.ExpiredAt.Value < now)
                        {
                            isOrderValid = false;
                        }
                        
                        if (isOrderValid)
                        {
                            var packageName = currentOrder.Product.ProductName;
                            if (!string.IsNullOrEmpty(packageName) && packageCounts.ContainsKey(packageName))
                            {
                                packageCounts[packageName]++;
                            }
                            else if (!string.IsNullOrEmpty(packageName))
                            {
                                packageCounts[packageName] = 1;
                            }
                        }
                    }
                    else if (shop.Status == 2) // Trial shop
                    {
                        var trialPackageName = "Gói Basic (Trial)";
                        if (packageCounts.ContainsKey(trialPackageName))
                        {
                            packageCounts[trialPackageName]++;
                        }
                        else
                        {
                            packageCounts[trialPackageName] = 1;
                        }
                    }
                }
                
                // Convert sang list of objects
                var result = packageCounts.Select(kvp => new
                {
                    PackageName = kvp.Key,
                    Count = kvp.Value
                }).OrderByDescending(x => x.Count).ToList();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShopService] Lỗi khi lấy thống kê shop theo gói: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<object> GetShopNewVsRenewalStatsAsync()
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var monthlyStats = new List<object>();

                // Lấy tất cả shops và orders
                var allShops = await _shopRepo.GetAllAsync();
                var allOrders = await _orderRepo.GetAllAsync();

                // Kiểm tra dữ liệu
                Console.WriteLine($"[ShopService] Tổng số shops: {allShops.Count()}");
                Console.WriteLine($"[ShopService] Tổng số orders: {allOrders.Count()}");

                // Tạo danh sách 12 tháng (từ tháng hiện tại về trước)
                for (int i = 0; i < 12; i++)
                {
                    var targetMonth = currentTime.AddMonths(-i);
                    var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var newShops = 0;
                    var renewalShops = 0;

                    // Đếm shop mới và shop gia hạn trong tháng này
                    foreach (var shop in allShops)
                    {
                        // Lấy orders của shop trong tháng này
                        var monthOrders = allOrders
                            .Where(o => o.ShopId == shop.ShopId && o.CreatedAt.HasValue && 
                                       o.CreatedAt.Value >= monthStart && 
                                       o.CreatedAt.Value <= monthEnd)
                            .OrderBy(o => o.CreatedAt)
                            .ToList();

                        if (monthOrders.Any())
                        {
                            // Lấy order đầu tiên của shop (trong toàn bộ thời gian)
                            var firstOrderEver = allOrders
                                .Where(o => o.ShopId == shop.ShopId && o.CreatedAt.HasValue)
                                .OrderBy(o => o.CreatedAt)
                                .FirstOrDefault();

                            if (firstOrderEver != null && firstOrderEver.CreatedAt.HasValue)
                            {
                                // Nếu order đầu tiên nằm trong tháng này => shop mới
                                if (firstOrderEver.CreatedAt.Value >= monthStart && 
                                    firstOrderEver.CreatedAt.Value <= monthEnd)
                                {
                                    newShops++;
                                }
                                // Nếu có orders trong tháng nhưng không phải order đầu tiên => gia hạn
                                else if (monthOrders.Any())
                                {
                                    renewalShops++;
                                }
                            }
                        }
                    }

                    monthlyStats.Add(new
                    {
                        Month = targetMonth.ToString("MM/yyyy"),
                        NewShops = newShops,
                        RenewalShops = renewalShops,
                        TotalShops = newShops + renewalShops
                    });
                }

                // Tính tổng cho toàn bộ 12 tháng
                var totalNewShops = monthlyStats.Sum(x => ((dynamic)x).NewShops);
                var totalRenewalShops = monthlyStats.Sum(x => ((dynamic)x).RenewalShops);

                return new
                {
                    monthlyData = monthlyStats,
                    summary = new
                    {
                        totalNewShops = totalNewShops,
                        totalRenewalShops = totalRenewalShops,
                        totalShops = totalNewShops + totalRenewalShops,
                        period = $"12 tháng từ {currentTime.AddMonths(-11):MM/yyyy} đến {currentTime:MM/yyyy}"
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShopService] Lỗi khi lấy thống kê shop mới vs gia hạn: {ex.Message}");
                Console.WriteLine($"[ShopService] Stack trace: {ex.StackTrace}");
                return new
                {
                    monthlyData = new List<object>(),
                    summary = new
                    {
                        totalNewShops = 0,
                        totalRenewalShops = 0,
                        totalShops = 0,
                        period = "Lỗi khi tính toán"
                    }
                };
            }
        }

        public async Task<object> GetShopCountByProvinceAsync()
        {
            try
            {
                var allShops = await _shopRepo.GetAllAsync();
                var provinceStats = new Dictionary<string, int>();

                foreach (var shop in allShops)
                {
                    if (!string.IsNullOrEmpty(shop.Address))
                    {
                        // Lấy phần cuối của address (tỉnh/thành phố)
                        var addressParts = shop.Address.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        var province = addressParts.LastOrDefault()?.Trim();
                        
                        if (!string.IsNullOrEmpty(province))
                        {
                            if (provinceStats.ContainsKey(province))
                            {
                                provinceStats[province]++;
                            }
                            else
                            {
                                provinceStats[province] = 1;
                            }
                        }
                    }
                }

                var result = provinceStats.Select(kvp => new
                {
                    Province = kvp.Key,
                    Count = kvp.Value
                }).OrderByDescending(x => x.Count).ToList();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShopService] Lỗi khi lấy thống kê shop theo tỉnh: {ex.Message}");
                return new List<object>();
            }
        }
    }
}
