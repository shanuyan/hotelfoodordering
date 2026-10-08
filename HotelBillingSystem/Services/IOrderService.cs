using HotelBillingSystem.Models;

namespace HotelBillingSystem.Services
{
    public class CartItemRequest
    {
        public int FoodItemId { get; set; }
        public int Quantity { get; set; }
        public string SpecialInstructions { get; set; } = string.Empty;
    }

    public class CreateOrderResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Order? Order { get; set; }
    }

    public interface IOrderService
    {
        Task<CreateOrderResult> CreateOrderAsync(int tableId, List<CartItemRequest> items, string customerNotes);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetOrdersByTableSessionAsync(int sessionCode);
        Task<List<Order>> GetKitchenAssignedOrdersAsync(string kitchenStaffUserId);
        Task<List<Order>> GetAllActiveOrdersAsync();
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string userId, string remarks = "");
        Task<bool> RequestBillAsync(int tableId);
    }
}
