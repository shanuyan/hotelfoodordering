using HotelBillingSystem.Data;
using HotelBillingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardSummary> GetAdminDashboardSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;

            var todayBills = await _context.Bills
                .Where(b => b.CreatedAt >= today && b.PaymentStatus == PaymentStatus.Paid)
                .ToListAsync();

            decimal todaySales = todayBills.Sum(b => b.GrandTotal);

            var todayOrders = await _context.Orders
                .Where(o => o.CreatedAt >= today)
                .ToListAsync();

            int pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Assigned);
            int preparingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Accepted || o.Status == OrderStatus.Preparing);
            int completedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Served);
            int cancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled || o.Status == OrderStatus.Rejected);

            int occupiedTables = await _context.RestaurantTables.CountAsync(t => t.Status == TableStatus.Occupied);
            int availableTables = await _context.RestaurantTables.CountAsync(t => t.Status == TableStatus.Available);

            int availableKitchen = await _context.KitchenStaffStatuses.CountAsync(k => k.CurrentStatus == StaffStatus.Available);
            int busyKitchen = await _context.KitchenStaffStatuses.CountAsync(k => k.CurrentStatus == StaffStatus.Busy);

            var topItem = await _context.OrderItems
                .GroupBy(i => i.FoodName)
                .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefaultAsync();

            return new AdminDashboardSummary
            {
                TodaySales = todaySales,
                TodayOrdersCount = todayOrders.Count,
                PendingOrdersCount = pendingOrders,
                PreparingOrdersCount = preparingOrders,
                CompletedOrdersCount = completedOrders,
                CancelledOrdersCount = cancelledOrders,
                OccupiedTablesCount = occupiedTables,
                AvailableTablesCount = availableTables,
                ActiveCustomersCount = occupiedTables * 3, // Estimated
                AvailableKitchenStaffCount = availableKitchen,
                BusyKitchenStaffCount = busyKitchen,
                BestSellingItemName = topItem?.Name ?? "Chicken Fried Rice"
            };
        }

        public async Task<SalesChartData> GetDailySalesChartDataAsync(int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            var paidBills = await _context.Bills
                .Where(b => b.CreatedAt >= startDate && b.PaymentStatus == PaymentStatus.Paid)
                .ToListAsync();

            var result = new SalesChartData();

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var billsForDay = paidBills.Where(b => b.CreatedAt.Date == date).ToList();

                result.Labels.Add(date.ToString("MMM dd"));
                result.SalesValues.Add(billsForDay.Sum(b => b.GrandTotal));
                result.OrderCounts.Add(billsForDay.Count);
            }

            return result;
        }

        public async Task<List<CategorySalesData>> GetSalesByCategoryAsync()
        {
            var items = await _context.OrderItems
                .Include(i => i.FoodItem)
                    .ThenInclude(f => f!.Category)
                .Where(i => i.Order!.Status == OrderStatus.Completed || i.Order.Status == OrderStatus.Served)
                .ToListAsync();

            return items
                .GroupBy(i => i.FoodItem?.Category?.Name ?? "Uncategorized")
                .Select(g => new CategorySalesData
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(x => x.SubTotal),
                    ItemCount = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();
        }

        public async Task<List<TopFoodItemData>> GetTopSellingItemsAsync(int top = 10)
        {
            return await _context.OrderItems
                .GroupBy(i => i.FoodName)
                .Select(g => new TopFoodItemData
                {
                    ItemName = g.Key,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.SubTotal)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(top)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetOrdersByHourAsync()
        {
            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= DateTime.UtcNow.Date.AddDays(-30))
                .ToListAsync();

            var dict = new Dictionary<int, int>();
            for (int h = 0; h < 24; h++) dict[h] = 0;

            foreach (var group in orders.GroupBy(o => o.CreatedAt.Hour))
            {
                dict[group.Key] = group.Count();
            }

            return dict;
        }

        public async Task<Dictionary<string, decimal>> GetRevenueByPaymentMethodAsync()
        {
            var payments = await _context.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Paid)
                .ToListAsync();

            return payments
                .GroupBy(p => p.PaymentMethod.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        }

        public async Task<List<KitchenStaffPerformanceData>> GetKitchenPerformanceAsync()
        {
            var kitchenUsers = await _context.Users
                .Where(u => u.StaffRole == "KitchenStaff")
                .ToListAsync();

            var result = new List<KitchenStaffPerformanceData>();

            foreach (var user in kitchenUsers)
            {
                var orders = await _context.Orders
                    .Where(o => o.AssignedKitchenStaffId == user.Id)
                    .ToListAsync();

                int completed = orders.Count(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Served);
                int active = orders.Count(o => o.Status == OrderStatus.Assigned || o.Status == OrderStatus.Accepted || o.Status == OrderStatus.Preparing);

                // calculate avg prep time in minutes
                var timedOrders = orders.Where(o => o.AcceptedAt.HasValue && o.ReadyAt.HasValue).ToList();
                double avgPrep = timedOrders.Any()
                    ? timedOrders.Average(o => (o.ReadyAt!.Value - o.AcceptedAt!.Value).TotalMinutes)
                    : 12.5;

                result.Add(new KitchenStaffPerformanceData
                {
                    StaffName = string.IsNullOrEmpty(user.FullName) ? user.UserName! : user.FullName,
                    CompletedOrders = completed,
                    ActiveOrders = active,
                    AveragePrepMinutes = Math.Round(avgPrep, 1)
                });
            }

            return result;
        }
    }
}
