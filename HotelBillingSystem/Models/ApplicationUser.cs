using Microsoft.AspNetCore.Identity;

namespace HotelBillingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string StaffRole { get; set; } = string.Empty; // "Admin", "KitchenStaff", "CashierStaff"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
