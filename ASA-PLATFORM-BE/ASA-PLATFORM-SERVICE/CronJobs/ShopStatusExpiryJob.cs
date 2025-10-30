using ASA_PLATFORM_SERVICE.Interface;
using Quartz;
using System;

namespace ASA_PLATFORM_SERVICE.CronJobs
{
    public class ShopStatusExpiryJob : IJob
    {
        private readonly IShopService _shopService;
        
        public ShopStatusExpiryJob(IShopService shopService)
        {
            _shopService = shopService;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                Console.WriteLine($"[ShopStatusExpiryJob] Bắt đầu kiểm tra trạng thái shop hết hạn lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                // 1. Kiểm tra và cập nhật shop trial (status = 2) đã qua 7 ngày
                var trialUpdatedCount = await _shopService.UpdateExpiredTrialShopStatusAsync();
                Console.WriteLine($"[ShopStatusExpiryJob] Đã cập nhật {trialUpdatedCount} shop trial (status = 2) thành inactive");
                
                // 2. Kiểm tra và cập nhật shop active (status = 1) dựa trên order expiration
                var shopUpdatedCount = await _shopService.UpdateExpiredShopStatusAsync();
                Console.WriteLine($"[ShopStatusExpiryJob] Đã cập nhật {shopUpdatedCount} shop active (status = 1) thành inactive");
                
                Console.WriteLine($"[ShopStatusExpiryJob] Hoàn thành. Tổng: {trialUpdatedCount + shopUpdatedCount} shop đã được cập nhật");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShopStatusExpiryJob] Lỗi: {ex.Message}");
            }
        }
    }
}
