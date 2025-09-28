using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.Implenment;
using ASA_PLATFORM_SERVICE.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASA_PLATFORM_BE.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
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
    }
}
