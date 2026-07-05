using ApexProp.Domain.Entities;
using ApexProp.Domain.Models;

namespace ApexProp.Domain.Interfaces;

public interface IPropertyRepository
{
    Task<IEnumerable<Property>> GetAllAsync();
    Task<Property?> GetByIdAsync(int id);
    Task<IEnumerable<Property>> GetByAreaAsync(double lat, double lng, double radiusKm);
    Task<IEnumerable<Property>> GetTopPropertiesByScoreAsync(int count);
    Task<IEnumerable<Property>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<IEnumerable<Property>> GetByRoomsAsync(int rooms);
    Task<PagedResponse<Property>> SearchAsync(string? searchTerm, decimal? minPrice, decimal? maxPrice, int? minRooms, int? maxRooms, double? minScore, string sortBy, bool ascending, int pageNumber, int pageSize);
    Task<PagedResponse<Property>> GetAllPaginatedAsync(int pageNumber, int pageSize);
    Task<bool> PropertyExistsAsync(int id);
    Task<Property> CreateAsync(Property property);
    Task UpdateAsync(Property property);
    Task<Property> PatchUpdateAsync(int id, Dictionary<string, object> updates);
    Task DeleteAsync(int id);
    Task<bool> AddToFavoritesAsync(int userId, int propertyId);
    Task<bool> RemoveFromFavoritesAsync(int userId, int propertyId);
    Task AddImagesToPropertyAsync(int propertyId, List<string> imageUrls);
    Task<IEnumerable<Property>> GetMultipleByIdsAsync(List<int> ids);
    Task<object> GetDashboardStatsAsync(); // מחזיר נתונים כלליים למסך הראשי
    Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(int propertyId);
}