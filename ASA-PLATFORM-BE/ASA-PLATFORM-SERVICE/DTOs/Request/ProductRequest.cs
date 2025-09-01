using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class ProductRequest
    {
        public string productName { get; set; }

        public string description { get; set; }

        public decimal? price { get; set; }

        public int? requestLimit { get; set; }

        public int? accountLimit { get; set; }

        public DateTime? createdAt { get; set; }

        public DateTime? updatedAt { get; set; }

        public decimal? discount { get; set; }

        public short? status { get; set; }

        public string qrcodeUrl { get; set; }
    }
    public class ProductGetRequest
    {
        public long? productId { get; set; }

        public string? productName { get; set; }

        public decimal? price { get; set; }

        public int? requestLimit { get; set; }

        public int? accountLimit { get; set; }

        public DateTime? createdAt { get; set; }

        public DateTime? updatedAt { get; set; }

        public decimal? discount { get; set; }

        public short? status { get; set; }

    }
}
