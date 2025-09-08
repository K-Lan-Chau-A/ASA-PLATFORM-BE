using ASA_PLATFORM_SERVICE.DTOs.Common;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Interface
{
    public interface IOrderService
    {
        Task<PagedResponse<OrderResponse>> GetFilteredProductsAsync(OrderGetRequest filterDto, int page, int pageSize);
        Task<ApiResponse<OrderResponse>> CreateAsync(OrderRequest request);
        Task<ApiResponse<OrderResponse>> UpdateAsync(long id, OrderRequest request);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}
