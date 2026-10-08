using HotelBillingSystem.Models;

namespace HotelBillingSystem.Services
{
    public interface IKitchenAssignmentService
    {
        Task<ApplicationUser?> FindBestKitchenStaffAsync();
        Task<bool> AssignOrderToBestKitchenStaffAsync(int orderId);
        Task<int> ReassignTimedOutOrdersAsync(int timeoutSeconds = 60);
        Task<Dictionary<string, int>> GetStaffWorkloadsAsync();
    }
}
