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
    public class PromotionProductService : IPromotionProductService
    {
        private readonly PromotionProductRepo _promotionProductRepo;
        private readonly IMapper _mapper;
        public PromotionProductService(PromotionProductRepo promotionProductRepo, IMapper mapper)
        {
            _promotionProductRepo = promotionProductRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PromotionProductResponse>> CreateAsync(PromotionProductRequest request)
        {
            try
            {
                var entity = _mapper.Map<PromotionProduct>(request);

                var affected = await _promotionProductRepo.CreateAsync(entity);

                if (affected > 0)
                {
                    var response = _mapper.Map<PromotionProductResponse>(entity);
                    return new ApiResponse<PromotionProductResponse>
                    {
                        Success = true,
                        Message = "Create successfully",
                        Data = response
                    };
                }

                return new ApiResponse<PromotionProductResponse>
                {
                    Success = false,
                    Message = "Create failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PromotionProductResponse>
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
                var existing = await _promotionProductRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Promotion product not found",
                        Data = false
                    };

                var affected = await _promotionProductRepo.RemoveAsync(existing);
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

        public async Task<PagedResponse<PromotionProductResponse>> GetFilteredPromotionProductAsync(PromotionProductGetRequest filterDto, int page, int pageSize)
        {
            var filter = _mapper.Map<PromotionProduct>(filterDto);
            var query = _promotionProductRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<PromotionProductResponse>
            {
                Items = _mapper.Map<IEnumerable<PromotionProductResponse>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ApiResponse<PromotionProductResponse>> UpdateAsync(long id, PromotionProductRequest request)
        {
            try
            {
                var existing = await _promotionProductRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<PromotionProductResponse>
                    {
                        Success = false,
                        Message = "Promotion product not found",
                        Data = null
                    };

                
                _mapper.Map(request, existing);

                var affected = await _promotionProductRepo.UpdateAsync(existing);
                if (affected > 0)
                {
                    var response = _mapper.Map<PromotionProductResponse>(existing);
                    return new ApiResponse<PromotionProductResponse>
                    {
                        Success = true,
                        Message = "Update successfully",
                        Data = response
                    };
                }

                return new ApiResponse<PromotionProductResponse>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PromotionProductResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}
