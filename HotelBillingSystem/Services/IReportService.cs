using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelBillingSystem.Services
{
    public class AdminDashboardSummary
    {
        public decimal TodaySales { get; set; }
        public int TodayOrdersCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int PreparingOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public int CancelledOrdersCount { get; set; }
        public int OccupiedTablesCount { get; set; }
        public int AvailableTablesCount { get; set; }
        public int ActiveCustomersCount { get; set; }
        public int AvailableKitchenStaffCount { get; set; }
        public int BusyKitchenStaffCount { get; set; }
        public string BestSellingItemName { get; set; } = string.Empty;
    }

    public class SalesChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> SalesValues { get; set; } = new List<decimal>();
        public List<int> OrderCounts { get; set; } = new List<int>();
    }

    public class CategorySalesData
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }

    public class TopFoodItemData
    {
        public string ItemName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class KitchenStaffPerformanceData
    {
        public string StaffName { get; set; } = string.Empty;
        public int CompletedOrders { get; set; }
        public int ActiveOrders { get; set; }
        public double AveragePrepMinutes { get; set; }
    }

    public interface IReportService
    {
        Task<AdminDashboardSummary> GetAdminDashboardSummaryAsync();
        Task<SalesChartData> GetDailySalesChartDataAsync(int days = 7);
        Task<List<CategorySalesData>> GetSalesByCategoryAsync();
        Task<List<TopFoodItemData>> GetTopSellingItemsAsync(int top = 10);
        Task<Dictionary<int, int>> GetOrdersByHourAsync();
        Task<Dictionary<string, decimal>> GetRevenueByPaymentMethodAsync();
        Task<List<KitchenStaffPerformanceData>> GetKitchenPerformanceAsync();
    }
}
