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

    public class ClaimOrderResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ClaimedByChefName { get; set; } = string.Empty;
    }

    public interface IOrderService
    {
        Task<CreateOrderResult> CreateOrderAsync(int tableId, string customerName, string customerPhone, List<CartItemRequest> items, string customerNotes);
        Task<ClaimOrderResult> ClaimOrderAsync(int orderId, string kitchenStaffUserId);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetKitchenAssignedOrdersAsync(string kitchenStaffUserId);
        Task<List<Order>> GetOrdersByTableSessionAsync(int tableSessionId);
        Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string changedByUserId, string remarks = "");
        Task<List<Order>> GetAllActiveOrdersAsync();
        Task<bool> RequestBillAsync(int tableId);
    }
}
