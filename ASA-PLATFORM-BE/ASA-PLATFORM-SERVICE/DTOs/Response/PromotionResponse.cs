using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
    public class PromotionResponse
    {
        public long promotionId { get; set; }
        public string promotionName { get; set; }
        public string description { get; set; }
        public DateOnly? startDate { get; set; }
        public DateOnly? endDate { get; set; }
        public decimal? value { get; set; }
        public string type { get; set; }
        public short? status { get; set; }
        public DateTime? createdAt { get; set; }
    }
}
