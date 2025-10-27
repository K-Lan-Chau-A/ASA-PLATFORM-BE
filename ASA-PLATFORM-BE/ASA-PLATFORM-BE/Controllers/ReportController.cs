using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.Implenment;
using ASA_PLATFORM_SERVICE.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASA_PLATFORM_BE.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IShopService _shopService;
        private readonly IOrderService _orderService;
        
        public ReportController(IReportService reportService, IShopService shopService, IOrderService orderService)
        {
            _reportService = reportService;
            _shopService = shopService;
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Report>>> GetFiltered([FromQuery] ReportGetRequest requestDto, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _reportService.GetFilteredReportAsync(requestDto, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("shop-by-package")]
        public async Task<ActionResult<object>> GetShopCountByPackage()
        {
            try
            {
                var result = await _shopService.GetShopCountByPackageAsync();
                return Ok(new
                {
                    Success = true,
                    Message = "Lấy thống kê shop theo gói thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }


        [HttpGet("shop-new-vs-renewal")]
        public async Task<ActionResult<object>> GetShopNewVsRenewalStats()
        {
            try
            {
                var result = await _shopService.GetShopNewVsRenewalStatsAsync();
                return Ok(new
                {
                    Success = true,
                    Message = "Lấy thống kê shop mới vs gia hạn thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpGet("shop-by-province")]
        public async Task<ActionResult<object>> GetShopCountByProvince()
        {
            try
            {
                var result = await _shopService.GetShopCountByProvinceAsync();
                return Ok(new
                {
                    Success = true,
                    Message = "Lấy thống kê shop theo tỉnh thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }
    }
}
