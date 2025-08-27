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
        private readonly IMapper _mapper;
        public ProductService(ProductRepo productRepo, IMapper mapper)
        {
            _productRepo = productRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ProductResponse>> CreateAsync(ProductRequest request)
        {
            try
            {
                var entity = _mapper.Map<Product>(request);

                var affected = await _productRepo.CreateAsync(entity);

                if (affected > 0)
                {
                    var response = _mapper.Map<ProductResponse>(entity);
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
                        Message = "Category not found",
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

            return new PagedResponse<ProductResponse>
            {
                Items = _mapper.Map<IEnumerable<ProductResponse>>(items),
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
                        Message = "Category not found",
                        Data = null
                    };

                // Map dữ liệu từ DTO sang entity, bỏ Id
                _mapper.Map(request, existing);

                var affected = await _productRepo.UpdateAsync(existing);
                if (affected > 0)
                {
                    var response = _mapper.Map<ProductResponse>(existing);
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
    }
}
