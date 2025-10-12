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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class PromotionService : IPromotionService
    {
        private readonly PromotionRepo _promotionRepo;
        private readonly PromotionProductRepo _promotionProductRepo;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        
        public PromotionService(PromotionRepo promotionRepo, IMapper mapper, PromotionProductRepo promotionProductRepo, 
            IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _promotionRepo = promotionRepo;
            _mapper = mapper;
            _promotionProductRepo = promotionProductRepo;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ApiResponse<PromotionResponse>> CreateAsync(PromotionCreateRequest request)
        {
            try
            {
                if (request.ProductIds != null && request.ProductIds.Any())
                {
                    var invalidIds = await _promotionRepo.GetInvalidProductIdsAsync(request.ProductIds);
                    if (invalidIds.Any())
                    {
                        return new ApiResponse<PromotionResponse>
                        {
                            Success = false,
                            Message = $"Invalid product id(s): {string.Join(", ", invalidIds)}",
                            Data = null
                        };
                    }
                }

                var entity = _mapper.Map<Promotion>(request);
                
                // Tự động set status = 1 (active) khi tạo promotion
                entity.Status = 1;
                
                if(!string.IsNullOrEmpty(entity.Type) && entity.Type.ToUpper() == "PERCENTAGE")
                {
                    entity.Type = "%";
                }      
                var affected = await _promotionRepo.CreateAsync(entity);

                // if has productIds, add to PromotionProduct
                if (affected > 0 && request.ProductIds != null && request.ProductIds.Any() == true )
                {
                    foreach (var productId in request.ProductIds)
                    {
                        var pp = new PromotionProduct
                        {
                            PromotionId = entity.PromotionId,
                            ProductId = productId
                        };
                        await _promotionProductRepo.CreateAsync(pp);
                    }   
                }

                if (affected > 0)
                {
                    // Gửi broadcast notification đến tất cả shop
                    await SendBroadcastNotificationAsync(request.promotionName, request.description);
                    
                    var response = _mapper.Map<PromotionResponse>(entity);
                    return new ApiResponse<PromotionResponse>
                    {
                        Success = true,
                        Message = "Create successfully",
                        Data = response
                    };
                }

                return new ApiResponse<PromotionResponse>
                {
                    Success = false,
                    Message = "Create failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PromotionResponse>
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
                var existing = await _promotionRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Promotion not found",
                        Data = false
                    };

                var affected = await _promotionRepo.RemoveAsync(existing);
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

        public async Task<PagedResponse<PromotionResponse>> GetFilteredPromotionsAsync(PromotionGetRequest filterDto, int page, int pageSize)
        {
            var filter = _mapper.Map<Promotion>(filterDto);
            var query = _promotionRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<PromotionResponse>
            {
                Items = _mapper.Map<IEnumerable<PromotionResponse>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ApiResponse<PromotionResponse>> UpdateAsync(long id, PromotionRequest request)
        {
            try
            {
                var existing = await _promotionRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<PromotionResponse>
                    {
                        Success = false,
                        Message = "Promotion not found",
                        Data = null
                    };

                if (request.ProductIds != null && request.ProductIds.Any())
                {
                    var invalidIds = await _promotionRepo.GetInvalidProductIdsAsync(request.ProductIds);
                    if (invalidIds.Any())
                    {
                        return new ApiResponse<PromotionResponse>
                        {
                            Success = false,
                            Message = $"Invalid product id(s): {string.Join(", ", invalidIds)}",
                            Data = null
                        };
                    }
                }

                // Map dữ liệu cơ bản
                _mapper.Map(request, existing);

                var affected = await _promotionRepo.UpdateAsync(existing);

                // Nếu có danh sách ProductIds
                if (affected > 0 && request.ProductIds != null)
                {
                    // Lấy danh sách product hiện tại trong DB
                    var existingProducts = await _promotionProductRepo.GetByPromotionIdAsync(existing.PromotionId);

                    // Convert sang HashSet<long>
                    var existingProductIds = existingProducts
                        .Where(x => x.ProductId.HasValue)     // tránh null
                        .Select(x => x.ProductId.Value)       // ép sang long
                        .ToHashSet();

                    var newProductIds = request.ProductIds ?? new HashSet<long>();

                    // Những product cần xóa
                    var toRemove = existingProductIds.Except(newProductIds);
                    foreach (var pid in toRemove)
                    {
                        var pp = existingProducts.First(x => x.ProductId == pid);
                        await _promotionProductRepo.RemoveAsync(pp);
                    }

                    // Những product cần thêm
                    var toAdd = newProductIds.Except(existingProductIds);
                    foreach (var pid in toAdd)
                    {
                        var pp = new PromotionProduct
                        {
                            PromotionId = existing.PromotionId,
                            ProductId = pid
                        };
                        await _promotionProductRepo.CreateAsync(pp);
                    }
                }

                if (affected > 0)
                {
                    var response = _mapper.Map<PromotionResponse>(existing);
                    return new ApiResponse<PromotionResponse>
                    {
                        Success = true,
                        Message = "Update successfully",
                        Data = response
                    };
                }

                return new ApiResponse<PromotionResponse>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PromotionResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        private async Task SendBroadcastNotificationAsync(string promotionName, string description)
        {
            try
            {
                Console.WriteLine($"SendBroadcastNotificationAsync: Starting broadcast for promotion '{promotionName}'");
                
                // Lấy tenant API URL từ configuration
                var tenantApiUrl = _configuration.GetValue<string>("BETenantUrl:Url");
                if (string.IsNullOrEmpty(tenantApiUrl))
                {
                    Console.WriteLine("BETenantUrl:Url not configured in appsettings");
                    return;
                }

                Console.WriteLine($"Tenant API URL: {tenantApiUrl}");

                // Tạo request payload
                var broadcastRequest = new BroadcastNotificationRequest
                {
                    Title = promotionName ?? "Khuyến mãi mới",
                    Content = description ?? "Có khuyến mãi mới từ hệ thống",
                    Type = 2 // Type 2 cho thông báo hệ thống
                };

                // Serialize request
                var jsonContent = JsonSerializer.Serialize(broadcastRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                Console.WriteLine($"Broadcast payload: {jsonContent}");

                // Tạo HTTP request
                using var httpClient = _httpClientFactory.CreateClient("BETenantUrl");
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                using var response = await httpClient.PostAsync("/api/notifications/broadcast", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Broadcast notification sent successfully. Response: {responseContent}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to send broadcast notification. Status: {response.StatusCode}, Error: {errorContent}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP error when sending broadcast notification: {httpEx.Message}");
            }
            catch (TaskCanceledException timeoutEx)
            {
                Console.WriteLine($"Timeout when sending broadcast notification: {timeoutEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON serialization error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error when sending broadcast notification: {ex.Message}");
            }
        }
    }
}
