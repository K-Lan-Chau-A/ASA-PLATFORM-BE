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
    public interface IProductService
    {
        Task<PagedResponse<ProductResponse>> GetFilteredProductsAsync(ProductGetRequest filterDto, int page, int pageSize);
        Task<ApiResponse<ProductResponse>> CreateAsync(ProductRequest request);
        Task<ApiResponse<ProductResponse>> UpdateAsync(long id, ProductRequest request);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}
