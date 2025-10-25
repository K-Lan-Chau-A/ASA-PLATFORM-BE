using ASA_PLATFORM_SERVICE.Interface;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.CronJobs
{
    public class PromotionExpiryJob : IJob
    {
        private readonly IPromotionService _promotionService;
        
        public PromotionExpiryJob(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                Console.WriteLine($"[PromotionExpiryJob] Starting promotion expiry check at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                var updatedCount = await _promotionService.UpdateExpiredPromotionsAsync();
                
                Console.WriteLine($"[PromotionExpiryJob] Completed. Updated {updatedCount} expired promotions to status 0");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PromotionExpiryJob] Error: {ex.Message}");
            }
        }
    }
}
