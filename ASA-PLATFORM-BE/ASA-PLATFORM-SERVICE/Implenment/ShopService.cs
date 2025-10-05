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

		public ShopService(ShopRepo shopRepo, IMapper mapper, OrderRepo orderRepo, IConfiguration configuration, IHttpClientFactory httpClientFactory, IEmailService emailService)
        {
            _shopRepo = shopRepo;
            _mapper = mapper;
            _orderRepo = orderRepo;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("BETenantUrl");
			_emailService = emailService;
        }

        public async Task<ApiResponse<ShopResponse>> CreateAsync(ShopRequest request)
        {
            try
            {
                // 1. Map sang entity
                var entity = _mapper.Map<Shop>(request);
                entity.Status = 2;

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

                // 2. Lưu shop vào DB Platform
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

                // 3. Gọi API Tenant
                var tenantApiUrl = _configuration.GetValue<string>("BETenantUrl:Url");
                var createShopEndpoint = $"{tenantApiUrl}/api/shops";

                var tenantResponse = await _httpClient.PostAsJsonAsync(createShopEndpoint, new
                {
                    shopName = request.shopName,
                    address = request.address,
                    shopToken = request.ShopToken,
                    status = 2,
                    qrcodeUrl = request.QrcodeUrl,
                    sepayApiKey = request.SepayApiKey,
                    currentRequest = request.CurrentRequest,
                    currentAccount = request.CurrentAccount,
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

                // 4. Trả về kết quả
                var shopResponse = _mapper.Map<ShopResponse>(entity);
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
            }

            return new PagedResponse<ShopResponse>
            {
                Items = shopResponses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
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
    }
}
