using HotelBillingSystem.Models;

namespace HotelBillingSystem.Services
{
    public interface IMenuService
    {
        Task<List<FoodCategory>> GetActiveCategoriesWithItemsAsync(bool availableOnly = true);
        Task<List<FoodItem>> GetAllFoodItemsAsync();
        Task<FoodItem?> GetFoodItemByIdAsync(int id);
        Task<bool> SetItemAvailabilityAsync(int foodId, bool isAvailable);
        Task<FoodItem> CreateFoodItemAsync(FoodItem item);
        Task<bool> UpdateFoodItemAsync(FoodItem item);
        Task<List<FoodCategory>> GetAllCategoriesAsync();
        Task<FoodCategory> CreateCategoryAsync(FoodCategory category);
    }
}
