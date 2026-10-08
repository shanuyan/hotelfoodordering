using HotelBillingSystem.Models;

namespace HotelBillingSystem.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string userId, string userName, string action, string entityName, string entityId, string details);
        Task<List<AuditLog>> GetLogsAsync(int count = 100);
    }
}
