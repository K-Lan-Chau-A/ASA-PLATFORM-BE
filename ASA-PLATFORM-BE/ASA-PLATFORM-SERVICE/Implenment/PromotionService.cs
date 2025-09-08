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
    public class PromotionService : IPromotionService
    {
        private readonly PromotionRepo _promotionRepo;
        private readonly PromotionProductRepo _promotionProductRepo;
        private readonly IMapper _mapper;
        public PromotionService(PromotionRepo promotionRepo, IMapper mapper, PromotionProductRepo promotionProductRepo)
        {
            _promotionRepo = promotionRepo;
            _mapper = mapper;
            _promotionProductRepo = promotionProductRepo;
        }

        public async Task<ApiResponse<PromotionResponse>> CreateAsync(PromotionRequest request)
        {
            try
            {
                var entity = _mapper.Map<Promotion>(request);
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

    }
}
