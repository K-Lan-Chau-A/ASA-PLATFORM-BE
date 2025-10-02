using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class OrderRequest
    {
        public long? ShopId { get; set; }

        public long? ProductId { get; set; }

        public long? UserId { get; set; }

        public decimal? TotalPrice { get; set; }

        public string PaymentMethod { get; set; }

        public short? Status { get; set; }

        public decimal? Discount { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ExpiredAt { get; set; }

        public string? Note { get; set; }

        // Optional: Shop info to auto-create Shop when ShopId is null
        public string? shopName { get; set; }
        public string? address { get; set; }
        public string? ShopToken { get; set; }
        public string? QrcodeUrl { get; set; }
        public string? SepayApiKey { get; set; }
        public int? CurrentRequest { get; set; }
        public int? CurrentAccount { get; set; }
        public string? BankName { get; set; }
        public string? BankCode { get; set; }
        public string? BankNum { get; set; }
    }
    public class OrderGetRequest
    {
        public long? OrderId { get; set; }

        public long? ShopId { get; set; }

        public long? ProductId { get; set; }

        public long? UserId { get; set; }

        public decimal? TotalPrice { get; set; }

        public string? PaymentMethod { get; set; }

        public short? Status { get; set; }

        public decimal? Discount { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ExpiredAt { get; set; }

        public string? Note { get; set; }
    }
}
