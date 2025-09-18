using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
    public class ShopResponse
    {
        public long shopId { get; set; }

        public string shopName { get; set; }

        public string address { get; set; }

        public short? status { get; set; }

        public string? productType { get; set; }

        public DateTime? expiredAt { get; set; }

        public DateTime? createdAt { get; set; }

        public DateTime? updatedAt { get; set; }
    }
}
