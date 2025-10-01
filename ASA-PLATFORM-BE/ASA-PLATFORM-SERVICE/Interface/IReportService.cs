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
    public interface IReportService
    {
        Task GenerateWeeklyReportAsync();
        Task GenerateMonthlyReportAsync();
        Task<PagedResponse<ReportResponse>> GetFilteredReportAsync(ReportGetRequest Filter, int page, int pageSize);
    }
}
