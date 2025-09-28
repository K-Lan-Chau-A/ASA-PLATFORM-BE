using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Models;
using EDUConnect_Repositories.Basic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_REPO.Repository
{
    public class ReportRepo : GenericRepository<Report>
    {

        public ReportRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }

        public IQueryable<Report> GetFiltered(Report filter)
        {
            var query = _context.Reports.AsQueryable();
            if (filter.ReportId > 0)
                query = query.Where(r => r.ReportId == filter.ReportId);
            if (!string.IsNullOrEmpty(filter.Type))
                query = query.Where(r => r.Type == filter.Type);
            if (filter.StartDate != null)
                query = query.Where(r => r.StartDate == filter.StartDate);
            if (filter.EndDate != null)
                query = query.Where(r => r.EndDate == filter.EndDate);
            if (filter.CreatedAt != null)
                query = query.Where(r => r.CreatedAt == filter.CreatedAt);
            if (filter.OrderCounter > 0)
                query = query.Where(r => r.OrderCounter == filter.OrderCounter);
            if (filter.Revenue > 0)
                query = query.Where(r => r.Revenue == filter.Revenue);
            return query;
        }

        /// <summary>
        /// Tạo report hàng tuần (tính revenue & order counter)
        /// </summary>
        public async Task GenerateWeeklyReportAsync()
        {
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            // Xác định khoảng tuần (Mon – Sun)
            int dow = (int)localNow.DayOfWeek;
            int offsetToMonday = dow == 0 ? 6 : dow - 1; // Chủ nhật = 0
            var startOfWeek = DateOnly.FromDateTime(localNow.Date.AddDays(-offsetToMonday - 7));
            var endOfWeek = startOfWeek.AddDays(6);

            var weekStartDateTime = DateTime.SpecifyKind(startOfWeek.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var weekEndDateTime = DateTime.SpecifyKind(endOfWeek.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            bool exists = await _context.Reports
            .AnyAsync(r => r.Type == "Weekly"
                && r.StartDate == startOfWeek
                && r.EndDate == endOfWeek);
            if (exists) return;

            // Query Orders trong tuần
            var ordersInWeek = await _context.Orders
                .Where(o => o.CreatedAt >= weekStartDateTime && o.CreatedAt <= weekEndDateTime)
                .Where(o => o.Status == 1) // Chỉ tính các đơn đã hoàn thành
                .ToListAsync();

            var revenue = ordersInWeek.Sum(o => o.TotalPrice);
            var orderCounter = ordersInWeek.Count;

            var report = new Report
            {
                Type = "Weekly",
                StartDate = DateOnly.FromDateTime(weekStartDateTime),
                EndDate = DateOnly.FromDateTime(weekEndDateTime),
                CreatedAt = DateTime.UtcNow,
                Revenue = revenue,
                OrderCounter = orderCounter,
            };

            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();

            // Gom nhóm orders theo ProductId để tạo ReportDetail
            var productStats = ordersInWeek
                .GroupBy(o => o.ProductId)
                .Select(g => new ReportDetail
                {
                    ReportId = report.ReportId,
                    ProductId = g.Key ?? 0,
                    Quantity = g.Count(),
                })
                .ToList();

            await _context.ReportDetails.AddRangeAsync(productStats);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Tạo report hàng tháng (gom revenue & order counter từ các tuần trong tháng trước)
        /// </summary>
        public async Task GenerateMonthlyReportAsync()
        {
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            // Xác định tháng trước
            var currentMonthStart = new DateOnly(localNow.Year, localNow.Month, 1);
            var prevMonthStart = currentMonthStart.AddMonths(-1);
            var prevMonthEnd = currentMonthStart.AddDays(-1);

            var monthStartDateTime = DateTime.SpecifyKind(prevMonthStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var monthEndDateTime = DateTime.SpecifyKind(prevMonthEnd.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            bool exists = await _context.Reports
                .AnyAsync(r => r.Type == "Monthly"
                && r.StartDate == prevMonthStart
                && r.EndDate == prevMonthEnd);
            if (exists) return;

            // Query Orders trong tháng trước
            var ordersInMonth = await _context.Orders
                .Where(o => o.CreatedAt >= monthStartDateTime && o.CreatedAt <= monthEndDateTime)
                .Where(o => o.Status == 1) // Chỉ tính các đơn đã hoàn thành
                .ToListAsync();

            var revenue = ordersInMonth.Sum(o => o.TotalPrice);
            var orderCounter = ordersInMonth.Count;

            var report = new Report
            {
                Type = "Monthly",
                StartDate = DateOnly.FromDateTime(monthStartDateTime),
                EndDate = DateOnly.FromDateTime(monthEndDateTime),
                CreatedAt = DateTime.UtcNow,
                Revenue = revenue,
                OrderCounter = orderCounter,
            };

            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();

            // Gom nhóm orders theo ProductId để tạo ReportDetail
            var productStats = ordersInMonth
                .GroupBy(o => o.ProductId)
                .Select(g => new ReportDetail
                {
                    ReportId = report.ReportId,
                    ProductId = g.Key ?? 0,
                    Quantity = g.Count(),
                })
                .ToList();

            await _context.ReportDetails.AddRangeAsync(productStats);
            await _context.SaveChangesAsync();
        }
    }
}
