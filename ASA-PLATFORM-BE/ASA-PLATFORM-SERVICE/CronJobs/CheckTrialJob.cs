using ASA_PLATFORM_SERVICE.Interface;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.CronJobs
{
    public class CheckTrialJob : IJob
    {
        private readonly IEmailService _emailService;
        private readonly IShopService _shopService; 
        public CheckTrialJob(IEmailService emailService, IShopService shopService )
        {
            _emailService = emailService;
            _shopService = shopService;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            // Lấy danh sách shop cần gửi mail (12, 13, 14 ngày)
            var shopsToNotify = await _shopService.CheckTrialShops();

            foreach (var shop in shopsToNotify)
            {
                var subject = "Thông báo dùng thử sắp hết hạn - ASA Platform";
                var body = $@"
                    <h3>Xin chào {shop.Fullname},</h3>
                    <p>Shop <b>{shop.ShopName}</b> của bạn đang trong giai đoạn dùng thử và sắp hết hạn.</p>
                    <p>Shop của bạn sẽ hết hạn dùng thử sau <b>{shop.DaysLeft} ngày</b>.</p>
                    <p>Vui lòng gia hạn hoặc đăng ký gói mới để tiếp tục sử dụng dịch vụ.</p>
                    <p><a href='https://asa-platform.vn/upgrade'>Bấm vào đây để gia hạn ngay</a></p>
                    <br/>
                    <p>Trân trọng,<br/>ASA Platform</p>";

                await _emailService.SendEmailAsync(shop.Email, subject, body);
            }
        }
    }
}
