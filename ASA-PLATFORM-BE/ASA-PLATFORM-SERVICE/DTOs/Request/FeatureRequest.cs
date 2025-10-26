using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class FeatureRequest
    {
        public string FeatureName { get; set; }
        public string Description { get; set; }
        // Removed: IsActive, CreatedAt, UpdatedAt - BE will handle these
    }

    public class FeatureGetRequest
    {
        public long? FeatureId { get; set; }
        public string? FeatureName { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
