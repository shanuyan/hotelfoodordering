using HotelBillingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("daily-sales")]
        public async Task<IActionResult> GetDailySales([FromQuery] int days = 7)
        {
            var data = await _reportService.GetDailySalesChartDataAsync(days);
            return Ok(data);
        }

        [HttpGet("category-sales")]
        public async Task<IActionResult> GetCategorySales()
        {
            var data = await _reportService.GetSalesByCategoryAsync();
            return Ok(data);
        }

        [HttpGet("top-items")]
        public async Task<IActionResult> GetTopItems([FromQuery] int top = 10)
        {
            var data = await _reportService.GetTopSellingItemsAsync(top);
            return Ok(data);
        }

        [HttpGet("peak-hours")]
        public async Task<IActionResult> GetPeakHours()
        {
            var data = await _reportService.GetOrdersByHourAsync();
            return Ok(data);
        }

        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var data = await _reportService.GetRevenueByPaymentMethodAsync();
            return Ok(data);
        }

        [HttpGet("kitchen-performance")]
        public async Task<IActionResult> GetKitchenPerformance()
        {
            var data = await _reportService.GetKitchenPerformanceAsync();
            return Ok(data);
        }
    }
}
