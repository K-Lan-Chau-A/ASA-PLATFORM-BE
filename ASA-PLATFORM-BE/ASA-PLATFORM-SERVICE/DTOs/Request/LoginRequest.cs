using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class LoginRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class ValidateTenantLoginRequest
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public short? Role { get; set; }
        public long ShopId { get; set; }
        public List<long> FeatureIds { get; set; }
    }
}
