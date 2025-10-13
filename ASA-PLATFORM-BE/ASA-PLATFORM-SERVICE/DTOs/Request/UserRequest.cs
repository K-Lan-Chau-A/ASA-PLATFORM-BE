using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.DTOs.Request
{
    public class UserCreateRequest
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "Password cannot be longer than 100 characters.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[^\w\d\s]).+$", ErrorMessage = "Password must contain at least one uppercase letter and one special character.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Invaid Phone number format, 10 numbers is required and start with '0'")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public short Role { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    public class UserUpdateRequest
    {
        // Username is not updatable via API
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Invaid Phone number format, 10 numbers is required and start with '0'")]
        public string? PhoneNumber { get; set; }

        public short? Role { get; set; }
        public short? Status { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    public class UserGetRequest
    {
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public short? Role { get; set; }
        public short? Status { get; set; }
        public string? Avatar { get; set; }
    }
}
