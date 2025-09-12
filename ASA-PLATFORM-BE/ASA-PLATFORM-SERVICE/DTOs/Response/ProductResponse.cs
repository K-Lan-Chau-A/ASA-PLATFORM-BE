using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
    public class ProductResponse
    {
        public long ProductId { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }

        public decimal? Price { get; set; }

        public int? RequestLimit { get; set; }

        public int? AccountLimit { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public decimal? Discount { get; set; }

        public short? Status { get; set; }

        public string QrcodeUrl { get; set; }

        //Thông tin từ bảng Promotion thông qua bảng PromotionProduct
        public decimal? PromotionValue { get; set; }
        public string? PromotionType { get; set; }

        //Thông tin từ bảng Feature thông qua bảng ProductFeature
        public List<FeatureResponse> Features { get; set; }

    }
}
