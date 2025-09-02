using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class UserRequest
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "Password cannot be longer than 100 characters.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[^\w\d\s]).+$", ErrorMessage = "Password must contain at least one uppercase letter and one special character.")]
        public string Password { get; set; }
        public short Role { get; set; }
        public short? Status { get; set; }
        public string Avatar { get; set; }
    }

    public class UserGetRequest
    {
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public short? Role { get; set; }
        public short? Status { get; set; }
        public string? Avatar { get; set; }
    }
}
