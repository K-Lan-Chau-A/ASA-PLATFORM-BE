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
    public class FeatureService : IFeatureService
    {
        private readonly FeatureRepo _featureRepo;
        private readonly IMapper _mapper;

        public FeatureService(FeatureRepo featureRepo, IMapper mapper)
        {
            _featureRepo = featureRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<FeatureResponse>> CreateAsync(FeatureRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.FeatureName))
                {
                    return new ApiResponse<FeatureResponse>
                    {
                        Success = false,
                        Message = "FeatureName is required",
                        Data = null
                    };
                }

                // Check if feature name already exists
                var existingFeature = await _featureRepo.GetFiltered(new Feature { FeatureName = request.FeatureName })
                    .FirstOrDefaultAsync();
                
                if (existingFeature != null)
                {
                    return new ApiResponse<FeatureResponse>
                    {
                        Success = false,
                        Message = "Feature with this name already exists",
                        Data = null
                    };
                }

                var entity = _mapper.Map<Feature>(request);
                // BE tự động set các giá trị - sử dụng UTC cho PostgreSQL
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsActive = true; // Mặc định active khi tạo mới

                var affected = await _featureRepo.CreateAsync(entity);

                if (affected > 0)
                {
                    var response = _mapper.Map<FeatureResponse>(entity);
                    return new ApiResponse<FeatureResponse>
                    {
                        Success = true,
                        Message = "Create successfully",
                        Data = response
                    };
                }

                return new ApiResponse<FeatureResponse>
                {
                    Success = false,
                    Message = "Create failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<FeatureResponse>
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
                var existing = await _featureRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Feature not found",
                        Data = false
                    };

                var affected = await _featureRepo.RemoveAsync(existing);
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

        public async Task<PagedResponse<FeatureResponse>> GetFilteredFeaturesAsync(FeatureGetRequest filterDto, int page, int pageSize)
        {
            var filter = _mapper.Map<Feature>(filterDto);
            var query = _featureRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var featureResponses = _mapper.Map<List<FeatureResponse>>(items);

            return new PagedResponse<FeatureResponse>
            {
                Items = featureResponses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ApiResponse<FeatureResponse>> UpdateAsync(long id, FeatureRequest request)
        {
            try
            {
                var existing = await _featureRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<FeatureResponse>
                    {
                        Success = false,
                        Message = "Feature not found",
                        Data = null
                    };

                // Map dữ liệu từ DTO sang entity, bỏ Id
                _mapper.Map(request, existing);
                existing.UpdatedAt = DateTime.UtcNow; // Sử dụng UTC cho PostgreSQL

                var affected = await _featureRepo.UpdateAsync(existing);

                if (affected > 0)
                {
                    var response = _mapper.Map<FeatureResponse>(existing);
                    return new ApiResponse<FeatureResponse>
                    {
                        Success = true,
                        Message = "Update successfully",
                        Data = response
                    };
                }

                return new ApiResponse<FeatureResponse>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<FeatureResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}
