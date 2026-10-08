using HotelBillingSystem.Data;
using HotelBillingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Services
{
    public class KitchenAssignmentService : IKitchenAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly Random _random = new Random();

        public KitchenAssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser?> FindBestKitchenStaffAsync()
        {
            // 1. Get all active users with role KitchenStaff and status Available
            var activeKitchenUsers = await (from u in _context.Users
                                             join k in _context.KitchenStaffStatuses on u.Id equals k.UserId
                                             where u.IsActive && u.StaffRole == "KitchenStaff"
                                                   && (k.CurrentStatus == StaffStatus.Available || k.CurrentStatus == StaffStatus.Busy)
                                             select u).ToListAsync();

            if (!activeKitchenUsers.Any())
            {
                // Fallback: any active KitchenStaff user if no status explicitly specified
                activeKitchenUsers = await _context.Users
                    .Where(u => u.IsActive && u.StaffRole == "KitchenStaff")
                    .ToListAsync();
            }

            if (!activeKitchenUsers.Any()) return null;

            // 2. Count active assigned orders for each candidate staff member
            var activeStatuses = new[] { OrderStatus.New, OrderStatus.Assigned, OrderStatus.Accepted, OrderStatus.Preparing };

            var workloads = await _context.Orders
                .Where(o => activeStatuses.Contains(o.Status) && o.AssignedKitchenStaffId != null)
                .GroupBy(o => o.AssignedKitchenStaffId!)
                .Select(g => new { StaffId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StaffId, x => x.Count);

            // Calculate workload map for active candidate users
            var staffWorkloadList = activeKitchenUsers.Select(u => new
            {
                User = u,
                ActiveCount = workloads.ContainsKey(u.Id) ? workloads[u.Id] : 0
            }).ToList();

            // 3. Find minimum active workload
            int minWorkload = staffWorkloadList.Min(x => x.ActiveCount);

            // 4. Filter candidate staff members who have the minimum workload
            var lowestWorkloadCandidates = staffWorkloadList
                .Where(x => x.ActiveCount == minWorkload)
                .Select(x => x.User)
                .ToList();

            // 5. Randomly pick one among the lowest workload candidates for fair balancing
            int selectedIndex = _random.Next(lowestWorkloadCandidates.Count);
            return lowestWorkloadCandidates[selectedIndex];
        }

        public async Task<bool> AssignOrderToBestKitchenStaffAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            var bestStaff = await FindBestKitchenStaffAsync();
            if (bestStaff == null) return false;

            var prevStaffId = order.AssignedKitchenStaffId;
            order.AssignedKitchenStaffId = bestStaff.Id;
            order.AssignedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Assigned;
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                PreviousStatus = OrderStatus.New,
                NewStatus = OrderStatus.Assigned,
                ChangedByUserId = bestStaff.Id,
                ChangedAt = DateTime.UtcNow,
                Remarks = prevStaffId == null
                    ? $"Smart assigned to {bestStaff.FullName} (Workload balanced)"
                    : $"Reassigned to {bestStaff.FullName}"
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> ReassignTimedOutOrdersAsync(int timeoutSeconds = 60)
        {
            var cutoffTime = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

            var timedOutOrders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Assigned
                         && o.AssignedAt.HasValue
                         && o.AssignedAt.Value < cutoffTime)
                .ToListAsync();

            int reassignedCount = 0;
            foreach (var order in timedOutOrders)
            {
                var newStaff = await FindBestKitchenStaffAsync();
                if (newStaff != null && newStaff.Id != order.AssignedKitchenStaffId)
                {
                    string oldStaffId = order.AssignedKitchenStaffId ?? "None";
                    order.AssignedKitchenStaffId = newStaff.Id;
                    order.AssignedAt = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;

                    _context.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        PreviousStatus = OrderStatus.Assigned,
                        NewStatus = OrderStatus.Assigned,
                        ChangedAt = DateTime.UtcNow,
                        Remarks = $"Timed out after {timeoutSeconds}s. Reassigned from user {oldStaffId} to {newStaff.FullName}"
                    });

                    reassignedCount++;
                }
            }

            if (reassignedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return reassignedCount;
        }

        public async Task<Dictionary<string, int>> GetStaffWorkloadsAsync()
        {
            var activeStatuses = new[] { OrderStatus.New, OrderStatus.Assigned, OrderStatus.Accepted, OrderStatus.Preparing };
            return await _context.Orders
                .Where(o => activeStatuses.Contains(o.Status) && o.AssignedKitchenStaffId != null)
                .GroupBy(o => o.AssignedKitchenStaffId!)
                .Select(g => new { StaffId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StaffId, x => x.Count);
        }
    }
}
