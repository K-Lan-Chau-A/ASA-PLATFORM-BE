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
    public class ReportService : IReportService
    {
        private readonly ReportRepo _reportRepo;
        private readonly IMapper _mapper;
        public ReportService(ReportRepo reportRepo, IMapper mapper)
        {
            _reportRepo = reportRepo;
            _mapper = mapper;
        }

        public async Task GenerateMonthlyReportAsync()
        {
            await _reportRepo.GenerateMonthlyReportAsync();
        }

        public async Task GenerateWeeklyReportAsync()
        {
           await _reportRepo.GenerateWeeklyReportAsync();
        }

        public async Task<PagedResponse<ReportResponse>> GetFilteredReportAsync(ReportGetRequest Filter, int page, int pageSize)
        {
            var filter = _mapper.Map<Report>(Filter);
            var query = _reportRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResponse<ReportResponse>
            {
                Items = _mapper.Map<IEnumerable<ReportResponse>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
