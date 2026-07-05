using Microsoft.EntityFrameworkCore;
using ApexProp.Domain.Entities;
using ApexProp.Domain.Interfaces;
using ApexProp.Infrastructure.Data;

namespace ApexProp.Infrastructure.Repositories;

/// <summary>
/// UserRepository - מממש את IUserRepository
/// זה מטפל בכל הפעולות עם משתמשים
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// קבל משתמש לפי ID
    /// </summary>
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// קבל משתמש לפי אימייל
    /// (משמש בהתחברות)
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// בדוק אם אימייל קיים
    /// (משמש בהרשמה כדי למנוע כפילות)
    /// </summary>
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// צור משתמש חדש
    /// </summary>
    public async Task<User> CreateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(user.FullName))
            throw new ArgumentException("Full name is required", nameof(user));

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Email is required", nameof(user));

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            throw new ArgumentException("Password hash is required", nameof(user));

        // בדוק שהאימייל לא קיים כבר
        if (await ExistsByEmailAsync(user.Email))
            throw new InvalidOperationException($"User with email '{user.Email}' already exists");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// קבל את כל המשתמשים (אדמין בלבד)
    /// </summary>
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// עדכן משתמש
    /// </summary>
    public async Task UpdateAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        // בגלל שהשתמשנו ב-AutoMapper ב-Controller, האובייקט 'user' כבר מעודכן.
        // אנחנו רק צריכים להגיד ל-EF שהישות השתנתה.
        _context.Entry(user).State = EntityState.Modified;

        // מניעת עדכון סיסמה אם היא לא השתנתה (אופציונלי אך מומלץ)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _context.Entry(user).Property(x => x.PasswordHash).IsModified = false;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// מחק משתמש לפי ID
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new InvalidOperationException($"User with ID {id} not found");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }


    /// <summary>
    /// שליפת כל הנכסים השמורים של משתמש ספציפי
    /// </summary>
    public async Task<IEnumerable<Property>> GetSavedPropertiesAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.SavedProperties)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.SavedProperties ?? new List<Property>();
    }

    /// <summary>
    /// הוספת נכס לרשימת השמורים
    /// </summary>
    public async Task AddSavedPropertyAsync(int userId, int propertyId)
    {
        // חובה לשלוף ללא AsNoTracking כדי שנוכל לעדכן את הרשימה
        var user = await _context.Users
            .Include(u => u.SavedProperties)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new InvalidOperationException("User not found");

        // נוודא שהנכס לא נמצא כבר ברשימה
        if (!user.SavedProperties.Any(p => p.Id == propertyId))
        {
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null) throw new InvalidOperationException("Property not found");

            user.SavedProperties.Add(property);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// הסרת נכס מרשימת השמורים
    /// </summary>
    public async Task RemoveSavedPropertyAsync(int userId, int propertyId)
    {
        var user = await _context.Users
            .Include(u => u.SavedProperties)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new InvalidOperationException("User not found");

        var propertyToRemove = user.SavedProperties.FirstOrDefault(p => p.Id == propertyId);
        if (propertyToRemove != null)
        {
            user.SavedProperties.Remove(propertyToRemove);
            await _context.SaveChangesAsync();
        }
    }
}