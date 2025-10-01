using ASA_PLATFORM_SERVICE.Interface;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.CronJobs
{
    public class MonthlyReportJob : IJob
    {
        private readonly IReportService _reportService;

        public MonthlyReportJob(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("📊 MonthlyReportJob triggered at " + DateTime.Now);

            await _reportService.GenerateMonthlyReportAsync();
        }
    }
}
