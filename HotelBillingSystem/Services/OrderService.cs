using HotelBillingSystem.Data;
using HotelBillingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITableService _tableService;
        private readonly IKitchenAssignmentService _kitchenAssignmentService;

        public OrderService(
            ApplicationDbContext context,
            ITableService tableService,
            IKitchenAssignmentService kitchenAssignmentService)
        {
            _context = context;
            _tableService = tableService;
            _kitchenAssignmentService = kitchenAssignmentService;
        }

        public async Task<CreateOrderResult> CreateOrderAsync(int tableId, string customerName, string customerPhone, List<CartItemRequest> items, string customerNotes)
        {
            if (items == null || !items.Any())
            {
                return new CreateOrderResult { Success = false, ErrorMessage = "Cart cannot be empty." };
            }

            var table = await _tableService.GetTableByIdAsync(tableId);
            if (table == null || !table.IsActive)
            {
                return new CreateOrderResult { Success = false, ErrorMessage = "Invalid or inactive restaurant table." };
            }

            // Get or create table session
            var session = await _tableService.GetOrCreateActiveSessionAsync(tableId);

            // Fetch food items from DB to validate availability & real server prices
            var foodItemIds = items.Select(i => i.FoodItemId).Distinct().ToList();
            var dbFoodItems = await _context.FoodItems
                .Where(f => foodItemIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

            var orderItems = new List<OrderItem>();
            decimal subTotal = 0;

            foreach (var item in items)
            {
                if (item.Quantity <= 0) continue;

                if (!dbFoodItems.TryGetValue(item.FoodItemId, out var foodItem) || !foodItem.IsActive)
                {
                    return new CreateOrderResult
                    {
                        Success = false,
                        ErrorMessage = $"One of the requested items is no longer on the menu."
                    };
                }

                if (!foodItem.IsAvailable)
                {
                    return new CreateOrderResult
                    {
                        Success = false,
                        ErrorMessage = $"Sorry, '{foodItem.Name}' is currently out of stock/unavailable. Please update your order."
                    };
                }

                // Server-calculated price
                decimal itemSubTotal = foodItem.Price * item.Quantity;
                subTotal += itemSubTotal;

                orderItems.Add(new OrderItem
                {
                    FoodItemId = foodItem.Id,
                    FoodName = foodItem.Name,
                    UnitPrice = foodItem.Price,
                    Quantity = item.Quantity,
                    SubTotal = itemSubTotal,
                    SpecialInstructions = item.SpecialInstructions ?? string.Empty
                });
            }

            if (!orderItems.Any())
            {
                return new CreateOrderResult { Success = false, ErrorMessage = "No valid items selected." };
            }

            // Calculate service charge and tax (e.g. 10% service charge)
            decimal serviceCharge = Math.Round(subTotal * 0.10m, 2);
            decimal taxAmount = 0m; // Can be configured
            decimal grandTotal = subTotal + serviceCharge + taxAmount;

            string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";

            var order = new Order
            {
                OrderNumber = orderNumber,
                TableSessionId = session.Id,
                TableId = tableId,
                CustomerName = string.IsNullOrEmpty(customerName) ? "Table Guest" : customerName,
                CustomerPhone = customerPhone ?? string.Empty,
                Status = OrderStatus.New,
                CustomerNotes = customerNotes ?? string.Empty,
                SubTotal = subTotal,
                ServiceCharge = serviceCharge,
                TaxAmount = taxAmount,
                TotalAmount = grandTotal,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Record initial history
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                PreviousStatus = OrderStatus.New,
                NewStatus = OrderStatus.New,
                ChangedAt = DateTime.UtcNow,
                Remarks = "Customer placed new order via QR."
            });

            await _context.SaveChangesAsync();

            // Run Smart Kitchen Staff Assignment Algorithm
            await _kitchenAssignmentService.AssignOrderToBestKitchenStaffAsync(order.Id);

            // Update session total
            session.TotalAmount += grandTotal;
            await _context.SaveChangesAsync();

            // Reload full order entity
            var createdOrder = await GetOrderByIdAsync(order.Id);

            return new CreateOrderResult
            {
                Success = true,
                Order = createdOrder
            };
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.TableSession)
                .Include(o => o.AssignedKitchenStaff)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.FoodItem)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetOrdersByTableSessionAsync(int sessionId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.AssignedKitchenStaff)
                .Where(o => o.TableSessionId == sessionId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<ClaimOrderResult> ClaimOrderAsync(int orderId, string kitchenStaffUserId)
        {
            var order = await _context.Orders
                .Include(o => o.AssignedKitchenStaff)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new ClaimOrderResult { Success = false, ErrorMessage = "Order not found." };
            }

            if (order.AssignedKitchenStaffId != null && order.AssignedKitchenStaffId != kitchenStaffUserId)
            {
                string chefName = order.AssignedKitchenStaff?.FullName ?? "another chef";
                return new ClaimOrderResult
                {
                    Success = false,
                    ErrorMessage = $"Order #{order.OrderNumber} was already claimed by {chefName}.",
                    ClaimedByChefName = chefName
                };
            }

            var chef = await _context.Users.FindAsync(kitchenStaffUserId);
            string chefFullName = chef?.FullName ?? "Kitchen Chef";

            var oldStatus = order.Status;
            order.AssignedKitchenStaffId = kitchenStaffUserId;
            order.AssignedAt = DateTime.UtcNow;
            order.AcceptedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Accepted;
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                PreviousStatus = oldStatus,
                NewStatus = OrderStatus.Accepted,
                ChangedByUserId = kitchenStaffUserId,
                ChangedAt = DateTime.UtcNow,
                Remarks = $"Claimed by {chefFullName} (First-come, First-served)"
            });

            await _context.SaveChangesAsync();

            return new ClaimOrderResult
            {
                Success = true,
                ClaimedByChefName = chefFullName
            };
        }

        public async Task<List<Order>> GetKitchenAssignedOrdersAsync(string kitchenStaffUserId)
        {
            var activeStatuses = new[] { OrderStatus.Assigned, OrderStatus.Accepted, OrderStatus.Preparing, OrderStatus.Ready };

            return await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .Where(o => o.AssignedKitchenStaffId == kitchenStaffUserId && activeStatuses.Contains(o.Status))
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllActiveOrdersAsync()
        {
            var activeStatuses = new[] { OrderStatus.New, OrderStatus.Assigned, OrderStatus.Accepted, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Served };

            return await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.AssignedKitchenStaff)
                .Include(o => o.OrderItems)
                .Where(o => activeStatuses.Contains(o.Status))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string userId, string remarks = "")
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            var oldStatus = order.Status;
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            switch (newStatus)
            {
                case OrderStatus.Accepted:
                    order.AcceptedAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Preparing:
                    order.PreparingAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Ready:
                    order.ReadyAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Served:
                    order.ServedAt = DateTime.UtcNow;
                    break;
            }

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                PreviousStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByUserId = string.IsNullOrEmpty(userId) ? null : userId,
                ChangedAt = DateTime.UtcNow,
                Remarks = string.IsNullOrEmpty(remarks) ? $"Status changed from {oldStatus} to {newStatus}" : remarks
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId)
        {
            return await _context.OrderStatusHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.OrderId == orderId)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> RequestBillAsync(int tableId)
        {
            var activeSession = await _tableService.GetActiveSessionAsync(tableId);
            if (activeSession == null) return false;

            activeSession.IsBillRequested = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
