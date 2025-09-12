using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
    public class FeatureResponse
    {
        public long FeatureId { get; set; }
        public string FeatureName { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool? IsActive { get; set; }
    }
}
