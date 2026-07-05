using ApexProp.Domain.Entities;


namespace ApexProp.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync();

    // --- Saved Properties ---
    Task<IEnumerable<Property>> GetSavedPropertiesAsync(int userId);
    Task AddSavedPropertyAsync(int userId, int propertyId);
    Task RemoveSavedPropertyAsync(int userId, int propertyId);
}