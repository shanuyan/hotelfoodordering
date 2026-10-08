using HotelBillingSystem.Data;
using HotelBillingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBillingSystem.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string userName, string action, string entityName, string entityId, string details)
        {
            var log = new AuditLog
            {
                UserId = string.IsNullOrEmpty(userId) ? null : userId,
                UserName = string.IsNullOrEmpty(userName) ? "System/Customer" : userName,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetLogsAsync(int count = 100)
        {
            return await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}
