using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.DTOs.Common;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using ASA_PLATFORM_SERVICE.Interface;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class UserService : IUserService
    {
        private readonly UserRepo _userRepo;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        public UserService(UserRepo userRepo, IMapper mapper, IPhotoService photoService)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _photoService = photoService;
        }

        public async Task<ApiResponse<UserResponse>> CreateAsync(UserCreateRequest request)
        {
            try
            {
                // Uniqueness checks: Username, Email, PhoneNumber
                var usernameExists = await _userRepo
                    .GetFiltered(new User { Username = request.Username })
                    .AnyAsync(u => u.Username == request.Username);
                if (usernameExists)
                {
                    return new ApiResponse<UserResponse>
                    {
                        Success = false,
                        Message = "Username already exists",
                        Data = null
                    };
                }

                var emailExists = await _userRepo
                    .GetFiltered(new User { Email = request.Email })
                    .AnyAsync(u => u.Email == request.Email);
                if (emailExists)
                {
                    return new ApiResponse<UserResponse>
                    {
                        Success = false,
                        Message = "Email already exists",
                        Data = null
                    };
                }

                var phoneExists = await _userRepo
                    .GetFiltered(new User { PhoneNumber = request.PhoneNumber })
                    .AnyAsync(u => u.PhoneNumber == request.PhoneNumber);
                if (phoneExists)
                {
                    return new ApiResponse<UserResponse>
                    {
                        Success = false,
                        Message = "Phone number already exists",
                        Data = null
                    };
                }

                var entity = _mapper.Map<User>(request);
                entity.Password = HashPassword(request.Password);
                entity.Status = 1; // default active
                if (request.AvatarFile != null)
                {
                    var imageUrl = await _photoService.UploadImageAsync(request.AvatarFile);
                    entity.Avatar = imageUrl;
                }

                var affected = await _userRepo.CreateAsync(entity);

                if (affected > 0)
                {
                    var response = _mapper.Map<UserResponse>(entity);
                    return new ApiResponse<UserResponse>
                    {
                        Success = true,
                        Message = "Create successfully",
                        Data = response
                    };
                }

                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = "Create failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            try
            {
                var existing = await _userRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found",
                        Data = false
                    };

                var affected = await _userRepo.RemoveAsync(existing);
                return new ApiResponse<bool>
                {
                    Success = affected,
                    Message = affected ? "Deleted successfully" : "Delete failed",
                    Data = affected
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = false
                };
            }
        }

        public async Task<PagedResponse<UserResponse>> GetFilteredUsersAsync(UserGetRequest filterDto, int page, int pageSize)
        {
            var filter = _mapper.Map<User>(filterDto);
            var query = _userRepo.GetFiltered(filter);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<UserResponse>
            {
                Items = _mapper.Map<IEnumerable<UserResponse>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            return await _userRepo.GetUserByUsername(username);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public async Task<ApiResponse<UserResponse>> UpdateAsync(long id, UserUpdateRequest request)
        {
            try
            {
                var existing = await _userRepo.GetByIdAsync(id);
                if (existing == null)
                    return new ApiResponse<UserResponse>
                    {
                        Success = false,
                        Message = "User not found",
                        Data = null
                    };

                // Preserve Username (not updatable)
                var currentUsername = existing.Username;
                _mapper.Map(request, existing);
                existing.Username = currentUsername;

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    existing.Password = HashPassword(request.Password);
                }

                if (request.AvatarFile != null)
                {
                    var imageUrl = await _photoService.UploadImageAsync(request.AvatarFile);
                    existing.Avatar = imageUrl;
                }

                var affected = await _userRepo.UpdateAsync(existing);
                if (affected > 0)
                {
                    var response = _mapper.Map<UserResponse>(existing);
                    return new ApiResponse<UserResponse>
                    {
                        Success = true,
                        Message = "Update successfully",
                        Data = response
                    };
                }

                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}
