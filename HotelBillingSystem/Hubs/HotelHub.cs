using Microsoft.AspNetCore.SignalR;

namespace HotelBillingSystem.Hubs
{
    public class HotelHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // Broadcasters called from application services
        public async Task NotifyOrderStatusUpdate(int orderId, int tableId, string status)
        {
            await Clients.Group($"Order_{orderId}").SendAsync("OrderStatusChanged", orderId, status);
            await Clients.Group($"Table_{tableId}").SendAsync("OrderStatusChanged", orderId, status);
            await Clients.Group("Cashier").SendAsync("OrderStatusChanged", orderId, status);
            await Clients.Group("Admin").SendAsync("OrderStatusChanged", orderId, status);
        }

        public async Task NotifyAvailabilityChange(int foodId, string foodName, bool isAvailable)
        {
            await Clients.All.SendAsync("FoodAvailabilityUpdated", foodId, foodName, isAvailable);
        }

        public async Task NotifyBillRequested(int tableId, string tableNumber, int sessionId)
        {
            await Clients.Group("Cashier").SendAsync("BillRequestedNotification", tableId, tableNumber, sessionId);
            await Clients.Group("Admin").SendAsync("BillRequestedNotification", tableId, tableNumber, sessionId);
        }
    }
}
