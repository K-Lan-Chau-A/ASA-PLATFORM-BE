using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_SERVICE.DTOs.Common;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Interface
{
    public interface IAuthenticationService
    {
        public Task<ApiResponse<LoginResponse>> Login(LoginRequest loginRequest);
        public Task<string> GenerateToken(User account);
        public Task<ApiResponse<CurrentAccount>> GetCurrentAccount(string username);
        Task<ApiResponse<ValidateShopResponse>> ValidateShop(ValidateTenantLoginRequest dto);
    }
}
