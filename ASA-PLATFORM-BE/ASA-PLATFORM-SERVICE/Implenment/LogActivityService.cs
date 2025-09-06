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
    public class LogActivityService : ILogActivityService
    {
        private readonly LogActivityRepo _logActivityRepo;
        private readonly IMapper _mapper;
        public LogActivityService(LogActivityRepo logActivityRepo, IMapper mapper)
        {
            _logActivityRepo = logActivityRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<LogActivityResponse>> CreateAsync(LogActivityRequest request)
        {
            try
            {
                var entity = _mapper.Map<LogActivity>(request);

                var affected = await _logActivityRepo.CreateAsync(entity);

                if (affected > 0)
                {
                    var response = _mapper.Map<LogActivityResponse>(entity);
                    return new ApiResponse<LogActivityResponse>
                    {
                        Success = true,
                        Message = "Create successfully",
                        Data = response
                    };
                }

                return new ApiResponse<LogActivityResponse>
                {
                    Success = false,
                    Message = "Create failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LogActivityResponse>
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
                var existing = await _logActivityRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found",
                        Data = false
                    };

                var affected = await _logActivityRepo.RemoveAsync(existing);
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

        public async Task<PagedResponse<LogActivityResponse>> GetFilteredProductsAsync(LogActivityGetRequest filterDto, int page, int pageSize)
        {
            var filter = _mapper.Map<LogActivity>(filterDto);
            var query = _logActivityRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<LogActivityResponse>
            {
                Items = _mapper.Map<IEnumerable<LogActivityResponse>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ApiResponse<LogActivityResponse>> UpdateAsync(long id, LogActivityRequest request)
        {
            try
            {
                var existing = await _logActivityRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<LogActivityResponse>
                    {
                        Success = false,
                        Message = "LogActivity not found",
                        Data = null
                    };

                // Map dữ liệu từ DTO sang entity, bỏ Id
                _mapper.Map(request, existing);

                var affected = await _logActivityRepo.UpdateAsync(existing);
                if (affected > 0)
                {
                    var response = _mapper.Map<LogActivityResponse>(existing);
                    return new ApiResponse<LogActivityResponse>
                    {
                        Success = true,
                        Message = "Update successfully",
                        Data = response
                    };
                }

                return new ApiResponse<LogActivityResponse>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LogActivityResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}
