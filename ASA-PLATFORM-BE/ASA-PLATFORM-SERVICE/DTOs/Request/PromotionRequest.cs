using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class PromotionRequest
    {
        public string? promotionName { get; set; }
        public string? description { get; set; }
        public DateOnly? startDate { get; set; }
        public DateOnly? endDate { get; set; }
        public decimal? value { get; set; }
        public int type { get; set; }
        public short? status { get; set; }
        public HashSet<long>? ProductIds { get; set; }
    }

    public class PromotionGetRequest
    {
        public long? promotionId { get; set; }
        public string? promotionName { get; set; }
        public string? description { get; set; }
        public DateOnly? startDate { get; set; }
        public DateOnly? endDate { get; set; }
        public decimal? value { get; set; }
        public string? type { get; set; }
        public short? status { get; set; }
    }
    public class PromotionCreateRequest
    {
        [Required(ErrorMessage = "Promotion name is required")]
        public string promotionName { get; set; }
        
        public string? description { get; set; }
        
        public DateOnly? startDate { get; set; }
        
        public DateOnly? endDate { get; set; }
        
        [Required(ErrorMessage = "Value is required")]
        public decimal value { get; set; }
        
        [Required(ErrorMessage = "Type is required")]
        public int type { get; set; }
        
        public HashSet<long>? ProductIds { get; set; }
    }
}
