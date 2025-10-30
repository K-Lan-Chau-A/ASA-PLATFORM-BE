using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.DTOs.Common;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using ASA_PLATFORM_SERVICE.Interface;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class ProductService : IProductService
    {
        private readonly ProductRepo _productRepo;
        private readonly PromotionProductRepo _promotionProductRepo;
        private readonly IMapper _mapper;
        public ProductService(ProductRepo productRepo, PromotionProductRepo promotionProductRepo, IMapper mapper)
        {
            _productRepo = productRepo;
            _promotionProductRepo = promotionProductRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ProductResponse>> CreateAsync(ProductRequest request)
        {
            try
            {
                if (request.featureIds != null && request.featureIds.Any())
                {
                    var invalidIds = await _productRepo.GetInvalidFeatureIdsAsync(request.featureIds);
                    if (invalidIds.Any())
                    {
                        return new ApiResponse<ProductResponse>
                        {
                            Success = false,
                            Message = $"Invalid feature id(s): {string.Join(", ", invalidIds)}",
                            Data = null
                        };
                    }
                }

                var entity = _mapper.Map<Product>(request);

                // Thêm quan hệ ProductFeature nếu có featureIds
                if (request.featureIds != null && request.featureIds.Any())
                {
                    await _productRepo.AttachFeaturesAsync(entity, request.featureIds);
                }

                var affected = await _productRepo.CreateAsync(entity);

                if (affected > 0)
                {
                    var response = _mapper.Map<ProductResponse>(entity);
                    await ApplyPromotionLogic(response);
                    return new ApiResponse<ProductResponse>
                    {
                        Success = true,
                        Message = "Create successfully",
                        Data = response
                    };
                }

                return new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Create failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ProductResponse>
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
                var existing = await _productRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found",
                        Data = false
                    };

                var affected = await _productRepo.RemoveAsync(existing);
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

        public async Task<PagedResponse<ProductResponse>> GetFilteredProductsAsync(ProductGetRequest filterDto, int page, int pageSize)
        {
            var filter = _mapper.Map<Product>(filterDto);
            var query = _productRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var productResponses = _mapper.Map<List<ProductResponse>>(items);

            // Áp dụng logic promotion cho từng product
            foreach (var productResponse in productResponses)
            {
                await ApplyPromotionLogic(productResponse);
            }

            return new PagedResponse<ProductResponse>
            {
                Items = productResponses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ApiResponse<ProductResponse>> UpdateAsync(long id, ProductRequest request)
        {
            try
            {
                var existing = await _productRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Product not found",
                        Data = null
                    };

                if (request.featureIds != null && request.featureIds.Any())
                {
                    var invalidIds = await _productRepo.GetInvalidFeatureIdsAsync(request.featureIds);
                    if (invalidIds.Any())
                    {
                        return new ApiResponse<ProductResponse>
                        {
                            Success = false,
                            Message = $"Invalid feature id(s): {string.Join(", ", invalidIds)}",
                            Data = null
                        };
                    }
                }

                // Map dữ liệu từ DTO sang entity, bỏ Id
                _mapper.Map(request, existing);

                // Gọi repo update, truyền kèm featureIds
                var affected = await _productRepo.UpdateAsync(existing, request.featureIds);

                if (affected > 0)
                {
                    var response = _mapper.Map<ProductResponse>(existing);
                    await ApplyPromotionLogic(response);
                    return new ApiResponse<ProductResponse>
                    {
                        Success = true,
                        Message = "Update successfully",
                        Data = response
                    };
                }

                return new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        private async Task ApplyPromotionLogic(ProductResponse productResponse)
        {
            try
            {
                // Set OriginalPrice = giá gốc từ DB
                productResponse.OriginalPrice = productResponse.Price;

                // Lấy tất cả promotion của product này (chỉ status = 1)
                var promotions = await _promotionProductRepo.GetFiltered(new PromotionProduct { ProductId = productResponse.ProductId })
                    .Include(pp => pp.Promotion)
                    .Where(pp => pp.Promotion.Status == 1) // Chỉ lấy promotion có status = 1
                    .Select(pp => pp.Promotion)
                    .ToListAsync();

                if (promotions.Any())
                {
                    // Tìm promotion tốt nhất (giảm giá nhiều nhất)
                    var bestPromotion = FindBestPromotion(promotions, productResponse.OriginalPrice ?? 0);
                    
                    if (bestPromotion != null)
                    {
                        productResponse.PromotionValue = bestPromotion.Value;
                        productResponse.PromotionType = bestPromotion.Type;
                        productResponse.Price = CalculateFinalPrice(productResponse.OriginalPrice ?? 0, bestPromotion);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không ảnh hưởng đến response
                // Có thể thêm logging ở đây nếu cần
            }
        }

        private Promotion FindBestPromotion(List<Promotion> promotions, decimal originalPrice)
        {
            if (!promotions.Any()) return null;

            Promotion bestPromotion = null;
            decimal bestDiscount = 0;

            foreach (var promotion in promotions)
            {
                var discount = CalculateDiscountAmount(originalPrice, promotion);
                if (discount > bestDiscount)
                {
                    bestDiscount = discount;
                    bestPromotion = promotion;
                }
            }

            return bestPromotion;
        }

        private decimal CalculateDiscountAmount(decimal originalPrice, Promotion promotion)
        {
            if (promotion.Type == "%")
            {
                return originalPrice * (promotion.Value ?? 0) / 100;
            }
            else if (promotion.Type == "VNĐ")
            {
                return promotion.Value ?? 0;
            }
            return 0;
        }

        private decimal CalculateFinalPrice(decimal originalPrice, Promotion promotion)
        {
            if (promotion.Type == "%")
            {
                return originalPrice * (100 - (promotion.Value ?? 0)) / 100;
            }
            else if (promotion.Type == "VNĐ")
            {
                return Math.Max(0, originalPrice - (promotion.Value ?? 0)); // Đảm bảo giá không âm
            }
            return originalPrice;
        }

    }
}
