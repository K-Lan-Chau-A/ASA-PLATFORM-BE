using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace ASA_PLATFORM_BE.Controllers
{
    [Route("api/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private const int MaxFailedAttempts = 5;
        private const int BlockMinutes = 1;

        private readonly IAuthenticationService _authenticationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthenticationController(IAuthenticationService authenticationService, IMemoryCache cache, IHttpContextAccessor httpContextAccessor)
        {
            _authenticationService = authenticationService;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string key = $"login:{loginRequest.Username}:{clientIp}";

            // Check is user.IP is blocked yet?
            if (_cache.TryGetValue($"{key}:blocked", out _))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    $"This IP is block temporary {BlockMinutes} due to many login fail request with {MaxFailedAttempts} times.");
            }

            try
            {
                var result = await _authenticationService.Login(loginRequest);
                if (!result.Success)
                {
                    //  GetOrCreate find "key" in cache, if not found, create new with initial value 0, and set expiration time
                    int fails = _cache.GetOrCreate(key, entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(BlockMinutes);
                        return 0;
                    });

                    fails++;
                    _cache.Set(key, fails, TimeSpan.FromMinutes(BlockMinutes));

                    // Block IP if reach max failed attempts
                    if (fails >= MaxFailedAttempts)
                    {
                        _cache.Set($"{key}:blocked", true, TimeSpan.FromMinutes(BlockMinutes));
                        return StatusCode(StatusCodes.Status429TooManyRequests,
                            $"Login fail over {MaxFailedAttempts} attempts. Block in {BlockMinutes} minutes.");
                    }

                    return Unauthorized(new {
                        Success = false,
                        result.Message, 
                        RemainingAttempts = MaxFailedAttempts - fails 
                    });
                }

                // Login success, clear cache
                _cache.Remove(key);
                _cache.Remove($"{key}:blocked");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("get-current-account")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var username = _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized("User is not authenticated.");
                }
                var user = await _authenticationService.GetCurrentAccount(username);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("validate-tenant-login")]
        public async Task<IActionResult> ValidateTenantLogin([FromBody] ValidateTenantLoginRequest dto)
        {
            try
            {
                Console.WriteLine("Received DTO:");
                Console.WriteLine(dto);
                var result = await _authenticationService.ValidateShop(dto); 
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
