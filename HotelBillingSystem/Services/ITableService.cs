using HotelBillingSystem.Models;

namespace HotelBillingSystem.Services
{
    public interface ITableService
    {
        Task<RestaurantTable?> GetTableByTokenAsync(string token);
        Task<RestaurantTable?> GetTableByIdAsync(int id);
        Task<List<RestaurantTable>> GetAllTablesAsync();
        Task<TableSession> GetOrCreateActiveSessionAsync(int tableId);
        Task<TableSession?> GetActiveSessionAsync(int tableId);
        Task<bool> UpdateTableStatusAsync(int tableId, TableStatus status);
        Task<RestaurantTable> CreateTableAsync(string tableNumber, int capacity, string location);
        Task<string> RegenerateQrTokenAsync(int tableId);
    }
}
