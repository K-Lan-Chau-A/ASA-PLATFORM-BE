using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
    public class LoginResponse
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public short? Status { get; set; }
        public short? Role { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Avatar { get; set; }
        public string? AccessToken { get; set; }
    }

    public class ValidateShopResponse
    {
        public string? AccessToken { get; set; }
    }
}
