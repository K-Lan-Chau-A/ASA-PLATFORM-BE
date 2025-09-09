using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class ShopRequest
    {
        public string shopName { get; set; }

        public string address { get; set; }

        public short? status { get; set; }

        public DateTime? createdAt { get; set; }

        public DateTime? updatedAt { get; set; }
    }
    public class ShopGetRequest
    {
        public long? shopId { get; set; }
        public string? shopName { get; set; }
        public string? address { get; set; }
        public short? status { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime? updatedAt { get; set; }
    }

    public class CurrentShopProductRequest
    {
        public long shopId { get; set; }
    }
}
