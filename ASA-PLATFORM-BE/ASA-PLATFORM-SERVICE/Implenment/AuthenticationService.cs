using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_SERVICE.DTOs.Common;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using ASA_PLATFORM_SERVICE.Interface;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserService _userService;
        private readonly IShopService _shopService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        public AuthenticationService(IUserService userService, IConfiguration configuration, IMapper mapper, IShopService shopService)
        {
            _userService = userService;
            _configuration = configuration;
            _mapper = mapper;
            _shopService = shopService;
        }
        public async Task<ApiResponse<LoginResponse>> Login(LoginRequest loginRequest)
        {
            try
            {
                if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
                {
                    return new ApiResponse<LoginResponse>
                    {
                        Success = false,
                        Message = "Invalid login request",
                        Data = null
                    };
                }
                var user = await _userService.GetUserByUsername(loginRequest.Username);
                if (user != null)
                {
                    var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password);
                    if (!isPasswordValid)
                    {
                        return new ApiResponse<LoginResponse>
                        {
                            Success = false,
                            Message = "Invalid Username or Password",
                            Data = null
                        };
                    }
                    var accessToken = await GenerateToken(user);
                    var response = _mapper.Map<LoginResponse>(user);
                    response.AccessToken = accessToken;
                    return new ApiResponse<LoginResponse>
                    {
                        Success = true,
                        Message = "Login successfully",
                        Data = response
                    };
                }
                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Invalid Username or Password",
                    Data = null
                };

            }
            catch (Exception ex)
            {
                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        public Task<string> GenerateToken(User account)
        {
            var jwtConfig = _configuration.GetSection("JwtConfig");

            var issuer = jwtConfig["Issuer"];
            var audience = jwtConfig["Audience"];
            var key = jwtConfig["Key"];
            var expiryIn = DateTime.Now.AddMinutes(Double.Parse(jwtConfig["ExpireMinutes"]));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", account.UserId.ToString()),
                    new Claim(ClaimTypes.Name, account.Username),
                    new Claim(ClaimTypes.Role, account.Role.ToString())
                }),
                Expires = expiryIn,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);
            return Task.FromResult(accessToken);
        }

        public async Task<ApiResponse<CurrentAccount>> GetCurrentAccount(string username)
        {
            var user = await _userService.GetUserByUsername(username);
            if (user != null)
            {
                var currentAccount = _mapper.Map<CurrentAccount>(user);
                return new ApiResponse<CurrentAccount>
                {
                    Success = true,
                    Message = "Get currently logging user successfully",
                    Data = currentAccount
                };
            }
            else
            {
                return new ApiResponse<CurrentAccount>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<ValidateShopResponse>> ValidateShop(ValidateTenantLoginRequest dto)
        {
            try
            {
                var shop = await _shopService.GetShopById(dto.ShopId);
                if (shop != null && shop.Status == 1)
                {
                    var userAccount = new User
                    {
                        UserId = dto.ShopId,         
                        Username = dto.Username,     
                        Role = dto.Role             
                    };
                    var accessToken = await GenerateToken(userAccount);
                    return new ApiResponse<ValidateShopResponse>
                    {
                        Success = true,
                        Message = "Validate shop successfully",
                        Data = new ValidateShopResponse
                        {
                            AccessToken = accessToken
                        }
                    };
                }
                return new ApiResponse<ValidateShopResponse>
                {
                    Success = false,
                    Message = "Shop not found or inactive",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ValidateShopResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}

