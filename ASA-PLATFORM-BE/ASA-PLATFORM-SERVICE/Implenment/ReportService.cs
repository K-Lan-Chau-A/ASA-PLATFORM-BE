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

        public async Task<List<MonthlyRevenueResponse>> GetMonthlyRevenueLast12MonthsAsync()
        {
            // Lấy dữ liệu từ repository
            var monthlyData = await _reportRepo.GetMonthlyRevenueLast12MonthsAsync();

            // Tạo dictionary để dễ dàng tra cứu
            var revenueDict = monthlyData.ToDictionary(
                x => $"{x.Year}-{x.Month:D2}", 
                x => x.Revenue
            );

            // Lấy năm hiện tại
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var currentYear = localNow.Year;

            // Tạo danh sách kết quả cho 12 tháng (từ tháng 1 đến tháng 12)
            var result = new List<MonthlyRevenueResponse>();
            
            for (int month = 1; month <= 12; month++)
            {
                var monthKey = $"{currentYear}-{month:D2}";
                var monthName = $"{currentYear}-{month:D2}";
                
                result.Add(new MonthlyRevenueResponse
                {
                    Month = monthName,
                    Revenue = revenueDict.ContainsKey(monthKey) ? revenueDict[monthKey] : 0
                });
            }

            return result;
        }

        public async Task<DashboardStatsResponse> GetDashboardStatsAsync()
        {
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            // 1. Thống kê hôm nay
            var todayStart = localNow.Date;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);
            var todayStartUtc = DateTime.SpecifyKind(todayStart.ToUniversalTime(), DateTimeKind.Utc);
            var todayEndUtc = DateTime.SpecifyKind(todayEnd.ToUniversalTime(), DateTimeKind.Utc);

            var todayStats = await _reportRepo.GetStatsByPeriodAsync(todayStartUtc, todayEndUtc);

            // 2. Thống kê tuần này (Monday - Sunday)
            int dow = (int)localNow.DayOfWeek;
            int offsetToMonday = dow == 0 ? 6 : dow - 1; // Chủ nhật = 0
            var weekStart = todayStart.AddDays(-offsetToMonday);
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);
            var weekStartUtc = DateTime.SpecifyKind(weekStart.ToUniversalTime(), DateTimeKind.Utc);
            var weekEndUtc = DateTime.SpecifyKind(weekEnd.ToUniversalTime(), DateTimeKind.Utc);

            var weekStats = await _reportRepo.GetStatsByPeriodAsync(weekStartUtc, weekEndUtc);

            // 3. Thống kê tháng này
            var monthStart = new DateTime(localNow.Year, localNow.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var monthStartUtc = DateTime.SpecifyKind(monthStart.ToUniversalTime(), DateTimeKind.Utc);
            var monthEndUtc = DateTime.SpecifyKind(monthEnd.ToUniversalTime(), DateTimeKind.Utc);

            var monthStats = await _reportRepo.GetStatsByPeriodAsync(monthStartUtc, monthEndUtc);

            return new DashboardStatsResponse
            {
                Today = new PeriodStats
                {
                    NewShops = todayStats.NewShops,
                    ActiveShops = todayStats.ActiveShops,
                    Revenue = todayStats.Revenue,
                    PremiumSold = todayStats.PremiumSold
                },
                Week = new PeriodStats
                {
                    NewShops = weekStats.NewShops,
                    ActiveShops = weekStats.ActiveShops,
                    Revenue = weekStats.Revenue,
                    PremiumSold = weekStats.PremiumSold
                },
                Month = new PeriodStats
                {
                    NewShops = monthStats.NewShops,
                    ActiveShops = monthStats.ActiveShops,
                    Revenue = monthStats.Revenue,
                    PremiumSold = monthStats.PremiumSold
                }
            };
        }

        public async Task<DashboardStatsResponse> GetStatsByPeriodAsync()
        {
            // Tự động tính toán từ ngày hiện tại, giống như GetDashboardStatsAsync
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            // 1. Thống kê hôm nay
            var todayStart = localNow.Date;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);
            var todayStartUtc = DateTime.SpecifyKind(todayStart.ToUniversalTime(), DateTimeKind.Utc);
            var todayEndUtc = DateTime.SpecifyKind(todayEnd.ToUniversalTime(), DateTimeKind.Utc);

            var todayStats = await _reportRepo.GetStatsByPeriodAsync(todayStartUtc, todayEndUtc);

            // 2. Thống kê tuần này (Monday - Sunday)
            int dow = (int)localNow.DayOfWeek;
            int offsetToMonday = dow == 0 ? 6 : dow - 1; // Chủ nhật = 0
            var weekStart = todayStart.AddDays(-offsetToMonday);
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);
            var weekStartUtc = DateTime.SpecifyKind(weekStart.ToUniversalTime(), DateTimeKind.Utc);
            var weekEndUtc = DateTime.SpecifyKind(weekEnd.ToUniversalTime(), DateTimeKind.Utc);

            var weekStats = await _reportRepo.GetStatsByPeriodAsync(weekStartUtc, weekEndUtc);

            // 3. Thống kê tháng này
            var monthStart = new DateTime(localNow.Year, localNow.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var monthStartUtc = DateTime.SpecifyKind(monthStart.ToUniversalTime(), DateTimeKind.Utc);
            var monthEndUtc = DateTime.SpecifyKind(monthEnd.ToUniversalTime(), DateTimeKind.Utc);

            var monthStats = await _reportRepo.GetStatsByPeriodAsync(monthStartUtc, monthEndUtc);

            return new DashboardStatsResponse
            {
                Today = new PeriodStats
                {
                    NewShops = todayStats.NewShops,
                    ActiveShops = todayStats.ActiveShops,
                    Revenue = todayStats.Revenue,
                    PremiumSold = todayStats.PremiumSold
                },
                Week = new PeriodStats
                {
                    NewShops = weekStats.NewShops,
                    ActiveShops = weekStats.ActiveShops,
                    Revenue = weekStats.Revenue,
                    PremiumSold = weekStats.PremiumSold
                },
                Month = new PeriodStats
                {
                    NewShops = monthStats.NewShops,
                    ActiveShops = monthStats.ActiveShops,
                    Revenue = monthStats.Revenue,
                    PremiumSold = monthStats.PremiumSold
                }
            };
        }
    }
}
