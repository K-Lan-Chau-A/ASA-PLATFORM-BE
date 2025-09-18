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
    public class OrderService : IOrderService
    {
        private readonly OrderRepo _orderRepo;
        private readonly IMapper _mapper;
        public OrderService(OrderRepo orderRepo, IMapper mapper)
        {
            _orderRepo = orderRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<OrderResponse>> CreateAsync(OrderRequest request)
        {
            try
            {
                var entity = _mapper.Map<Order>(request);

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
    }
}
