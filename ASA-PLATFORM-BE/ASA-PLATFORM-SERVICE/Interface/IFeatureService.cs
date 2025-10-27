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
    public interface IFeatureService
    {
        Task<PagedResponse<FeatureResponse>> GetFilteredFeaturesAsync(FeatureGetRequest filterDto, int page, int pageSize);
        Task<ApiResponse<FeatureResponse>> CreateAsync(FeatureRequest request);
        Task<ApiResponse<FeatureResponse>> UpdateAsync(long id, FeatureRequest request);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}
