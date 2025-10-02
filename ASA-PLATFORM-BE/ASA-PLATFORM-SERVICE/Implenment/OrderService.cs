using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.DTOs.Common;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using ASA_PLATFORM_SERVICE.Interface;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class OrderService : IOrderService
    {
        private readonly OrderRepo _orderRepo;
        private readonly IMapper _mapper;
        private readonly IShopService _shopService;
        private readonly ProductRepo _productRepo;
        private readonly ILogger<OrderService> _logger;
        public OrderService(OrderRepo orderRepo, IMapper mapper, IShopService shopService, ProductRepo productRepo, ILogger<OrderService> logger)
        {
            _orderRepo = orderRepo;
            _mapper = mapper;
            _shopService = shopService;
            _productRepo = productRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<OrderResponse>> CreateAsync(OrderRequest request)
        {
            try
            {
                // Auto-create Shop if missing
                if (!request.ShopId.HasValue)
                {
                    _logger.LogInformation("[OrderService] ShopId is null. Auto-creating shop with body fields: phone={Phone}, shopName={ShopName}, address={Address}, bankCode={BankCode}, bankNum={BankNum}",
                        request.Phonenumber, request.shopName, request.address, request.BankCode, request.BankNum);

                    if (string.IsNullOrWhiteSpace(request.Phonenumber))
                    {
                        return new ApiResponse<OrderResponse>
                        {
                            Success = false,
                            Message = "Phonenumber is required when creating a new shop",
                            Data = null
                        };
                    }
                    var createShopReq = new ShopRequest
                    {
                        shopName = string.IsNullOrWhiteSpace(request.shopName) ? $"Shop-Auto-{DateTime.UtcNow:yyyyMMddHHmmss}" : request.shopName,
                        address = request.address ?? string.Empty,
                        status = 0,
                        ShopToken = request.ShopToken ?? string.Empty,
                        QrcodeUrl = request.QrcodeUrl,
                        SepayApiKey = request.SepayApiKey,
                        CurrentRequest = request.CurrentRequest,
                        CurrentAccount = request.CurrentAccount,
                        BankName = request.BankName,
                        BankCode = request.BankCode,
                        BankNum = request.BankNum,
                        Phonenumber = request.Phonenumber,
                        ProductId = request.ProductId
                    };
                    var createdShop = await _shopService.CreateAsync(createShopReq);
                    _logger.LogInformation("[OrderService] Create shop result: Success={Success}, Message={Message}, ReturnedShopId={ReturnedShopId}",
                        createdShop.Success, createdShop.Message, createdShop.Data?.shopId);
                    if (createdShop == null || !createdShop.Success || createdShop.Data == null || createdShop.Data.shopId <= 0)
                    {
                        _logger.LogWarning("[OrderService] Tenant did not create shop. Aborting order creation. Message={Message}", createdShop?.Message);
                        return new ApiResponse<OrderResponse>
                        {
                            Success = false,
                            Message = $"Tenant failed to create shop: {createdShop?.Message}",
                            Data = null
                        };
                    }

                    request.ShopId = createdShop.Data.shopId;
                    _logger.LogInformation("[OrderService] Assigned request.ShopId={ShopId} after shop creation", request.ShopId);
                }

                // Derive TotalPrice and ExpiredAt from Product (apply discount 0..100%)
                if (request.ProductId > 0)
                {
                    var product = await _productRepo.GetByIdAsync(request.ProductId);
                    if (product != null)
                    {
                        if (product.Price.HasValue)
                        {
                            var basePrice = product.Price.Value;
                            var discountRate = request.Discount ?? 0m;
                            if (discountRate < 0m) discountRate = 0m;
                            if (discountRate > 100m) discountRate = 100m;
                            var finalPrice = basePrice * (1m - discountRate / 100m);
                            request.TotalPrice = finalPrice;
                            _logger.LogInformation("[OrderService] Derived TotalPrice from ProductId={ProductId}: Base={Base}, Discount={Discount}%, Final={Final}",
                                request.ProductId, basePrice, discountRate, finalPrice);
                        }

                        // Do NOT compute ExpiredAt here; it will be set on webhook success
                    }
                    else
                    {
                        _logger.LogWarning("[OrderService] Product not found or Price is null for ProductId={ProductId}", request.ProductId);
                    }
                }

                var entity = _mapper.Map<Order>(request);
                // Do NOT set ExpiredAt at creation time; it will be set on webhook success
                _logger.LogInformation("[OrderService] Mapping to entity: ShopId={ShopId}, ProductId={ProductId}, TotalPrice={TotalPrice}",
                    entity.ShopId, entity.ProductId, entity.TotalPrice);

                var affected = await _orderRepo.CreateAsync(entity);

                if (affected > 0)
                {
                    var response = _mapper.Map<OrderResponse>(entity);
                    return new ApiResponse<OrderResponse>
                    {
                        Success = true,
                        Message = "Create successfully",
                        Data = response
                    };
                }

                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Create failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<OrderResponse>
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
                var existing = await _orderRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Order not found",
                        Data = false
                    };

                var affected = await _orderRepo.RemoveAsync(existing);
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

        public async Task<PagedResponse<OrderResponse>> GetFilteredProductsAsync(OrderGetRequest filterDto, int page, int pageSize)
        {
            var filter = _mapper.Map<Order>(filterDto);
            var query = _orderRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<OrderResponse>
            {
                Items = _mapper.Map<IEnumerable<OrderResponse>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ApiResponse<OrderResponse>> UpdateAsync(long id, OrderRequest request)
        {
            try
            {
                var existing = await _orderRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<OrderResponse>
                    {
                        Success = false,
                        Message = "Order not found",
                        Data = null
                    };

                // Map dữ liệu từ DTO sang entity, bỏ Id
                _mapper.Map(request, existing);

                var affected = await _orderRepo.UpdateAsync(existing);
                if (affected > 0)
                {
                    var response = _mapper.Map<OrderResponse>(existing);
                    return new ApiResponse<OrderResponse>
                    {
                        Success = true,
                        Message = "Update successfully",
                        Data = response
                    };
                }

                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<OrderResponse>> GetByIdAsync(long id)
        {
            try
            {
                var entity = await _orderRepo.GetByIdAsync(id);
                if (entity == null)
                {
                    return new ApiResponse<OrderResponse>
                    {
                        Success = false,
                        Message = "Order not found",
                        Data = null
                    };
                }

                var response = _mapper.Map<OrderResponse>(entity);
                return new ApiResponse<OrderResponse>
                {
                    Success = true,
                    Message = "Get successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<OrderResponse>> GetByNoteAsync(string note)
        {
            try
            {
                var entity = await _orderRepo.GetByNoteAsync(note);
                if (entity == null)
                {
                    return new ApiResponse<OrderResponse>
                    {
                        Success = false,
                        Message = "Order not found",
                        Data = null
                    };
                }

                var response = _mapper.Map<OrderResponse>(entity);
                return new ApiResponse<OrderResponse>
                {
                    Success = true,
                    Message = "Get successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, short status)
        {
            try
            {
                var existing = await _orderRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Order not found",
                        Data = false
                    };

                existing.Status = status;
                var affected = await _orderRepo.UpdateAsync(existing);
                
                return new ApiResponse<bool>
                {
                    Success = affected > 0,
                    Message = affected > 0 ? "Status updated successfully" : "Update failed",
                    Data = affected > 0
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

        public async Task<ApiResponse<bool>> UpdateExpiryFromProductAsync(long id)
        {
            try
            {
                var existing = await _orderRepo.GetByIdAsync(id);
                if (existing == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Order not found",
                        Data = false
                    };
                }

                if (!existing.ProductId.HasValue)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Order missing ProductId",
                        Data = false
                    };
                }

                var product = await _productRepo.GetByIdAsync(existing.ProductId.Value);
                if (product == null || !product.Duration.HasValue)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found or missing Duration",
                        Data = false
                    };
                }

                var createdAt = existing.CreatedAt ?? DateTime.UtcNow;
                existing.CreatedAt = createdAt;
                existing.ExpiredAt = createdAt + product.Duration.Value;

                var affected = await _orderRepo.UpdateAsync(existing);
                return new ApiResponse<bool>
                {
                    Success = affected > 0,
                    Message = affected > 0 ? "Expiry updated successfully" : "Update failed",
                    Data = affected > 0
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
    }
}