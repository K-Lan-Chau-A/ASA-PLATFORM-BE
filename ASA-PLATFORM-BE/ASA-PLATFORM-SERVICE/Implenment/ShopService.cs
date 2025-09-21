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
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class ShopService : IShopService
    {
        private readonly HttpClient _httpClient;
        private readonly ShopRepo _shopRepo;
        private readonly IMapper _mapper;
        private readonly OrderRepo _orderRepo;
        private readonly IConfiguration _configuration;

        public ShopService(ShopRepo shopRepo, IMapper mapper, OrderRepo orderRepo, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _shopRepo = shopRepo;
            _mapper = mapper;
            _orderRepo = orderRepo;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("BETenantUrl");
        }

        public async Task<ApiResponse<ShopResponse>> CreateAsync(ShopRequest request)
        {
            try
            {
                // 1. Map sang entity
                var entity = _mapper.Map<Shop>(request);

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
                    status = 1,
                    qrcodeUrl = request.QrcodeUrl,
                    sepayApiKey = request.SepayApiKey,
                    currentRequest = request.CurrentRequest,
                    currentAccount = request.CurrentAccount,
                    bankName = request.BankName,
                    bankCode = request.BankCode,
                    bankNum = request.BankNum
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
    }
}
