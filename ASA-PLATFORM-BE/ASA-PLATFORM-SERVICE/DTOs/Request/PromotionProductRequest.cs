using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class PromotionProductRequest
    {
        public long PromotionId { get; set; }
        public long ProductId { get; set; }
    }

    public class PromotionProductGetRequest
    {
        public long? PromotionProductId { get; set; }
        public long? PromotionId { get; set; }
        public long? ProductId { get; set; }
    }
}
