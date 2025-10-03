using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_SERVICE.DTOs.Common;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ASA_PLATFORM_REPO.Repository.ShopRepo;

namespace ASA_PLATFORM_SERVICE.Interface
{
    public interface IShopService
    {
        Task<PagedResponse<ShopResponse>> GetFilteredShopsAsync(ShopGetRequest filterDto, int page, int pageSize);
        Task<ApiResponse<ShopResponse>> CreateAsync(ShopRequest request);
        Task<ApiResponse<ShopResponse>> UpdateAsync(long id, ShopRequest request);
        Task<ApiResponse<bool>> DeleteAsync(long id);
        Task<Shop?> GetShopById(long id);
        Task<List<ShopTrialDto>> CheckTrialShops();
    }
}
