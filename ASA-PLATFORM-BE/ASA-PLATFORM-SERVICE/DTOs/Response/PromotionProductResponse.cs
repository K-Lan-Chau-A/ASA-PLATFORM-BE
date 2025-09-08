using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
    public class PromotionProductResponse
    {
        public long PromotionProductId { get; set; }
        public long? PromotionId { get; set; }
        public string? PromotionName { get; set; }
        public string? Description { get; set; }
        public long? ProductId { get; set; }
        public string? ProductName { get; set; }
    }
}
