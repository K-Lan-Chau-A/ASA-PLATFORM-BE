using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class LogActivityRequest
    {
        public long? UserId { get; set; }

        public string Content { get; set; }

        public short? Type { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
    public class LogActivityGetRequest
    {
        public long? LogActivityId { get; set; }

        public long? UserId { get; set; }

        public string? Content { get; set; }

        public short? Type { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
