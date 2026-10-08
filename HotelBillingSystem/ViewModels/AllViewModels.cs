using System.ComponentModel.DataAnnotations;
using HotelBillingSystem.Models;

namespace HotelBillingSystem.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class CustomerMenuViewModel
    {
        public RestaurantTable Table { get; set; } = null!;
        public TableSession Session { get; set; } = null!;
        public List<FoodCategory> Categories { get; set; } = new List<FoodCategory>();
        public List<Order> SessionOrders { get; set; } = new List<Order>();
    }

    public class OrderTrackingViewModel
    {
        public Order Order { get; set; } = null!;
        public List<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    }

    public class KitchenDashboardViewModel
    {
        public ApplicationUser KitchenUser { get; set; } = null!;
        public StaffStatus CurrentStaffStatus { get; set; }
        public List<Order> AssignedActiveOrders { get; set; } = new List<Order>();
        public Dictionary<string, int> AllStaffWorkloads { get; set; } = new Dictionary<string, int>();
    }

    public class CashierDashboardViewModel
    {
        public List<TableSession> ActiveSessions { get; set; } = new List<TableSession>();
        public List<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
        public List<Bill> RecentBills { get; set; } = new List<Bill>();
    }

    public class BillInvoiceViewModel
    {
        public Bill Bill { get; set; } = null!;
        public TableSession Session { get; set; } = null!;
        public RestaurantTable Table { get; set; } = null!;
        public List<Order> Orders { get; set; } = new List<Order>();
    }

    public class AdminDashboardViewModel
    {
        public Services.AdminDashboardSummary Summary { get; set; } = null!;
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
        public List<Services.KitchenStaffPerformanceData> KitchenPerformance { get; set; } = new List<Services.KitchenStaffPerformanceData>();
    }

    public class UserCreateEditViewModel
    {
        public string? Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string StaffRole { get; set; } = "KitchenStaff"; // "Admin", "KitchenStaff", "CashierStaff"

        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class FoodItemCreateEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = "/images/food-placeholder.png";

        public int PrepTimeMinutes { get; set; } = 15;
        public bool IsAvailable { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}
