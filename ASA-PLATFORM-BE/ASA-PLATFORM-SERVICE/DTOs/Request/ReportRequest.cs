using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class ReportRequest
    {
    }
    public class ReportGetRequest
    {
        public long? ReportId { get; set; }
        public string? Type { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? OrderCounter { get; set; }
        public decimal? Revenue { get; set; }
    }
}
