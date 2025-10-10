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
    public interface IUserService
    {
        Task<PagedResponse<UserResponse>> GetFilteredUsersAsync(UserGetRequest filterDto, int page, int pageSize);
        Task<ApiResponse<UserResponse>> CreateAsync(UserCreateRequest request);
        Task<ApiResponse<UserResponse>> UpdateAsync(long id, UserUpdateRequest request);
        Task<ApiResponse<bool>> DeleteAsync(long id);
        Task<User> GetUserByUsername(string username);
        string HashPassword(string password);
    }
}
