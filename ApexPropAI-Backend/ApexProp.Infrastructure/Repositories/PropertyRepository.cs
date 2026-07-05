using Microsoft.EntityFrameworkCore;
using ApexProp.Application.DTOs;
using ApexProp.Domain.Entities;
using ApexProp.Domain.Interfaces;
using ApexProp.Domain.Models;
using ApexProp.Infrastructure.Data;

namespace ApexProp.Infrastructure.Repositories;

/// <summary>
/// PropertyRepository - מממש את IPropertyRepository
/// זה מטפל בכל הפעולות עם טבלת Properties
/// </summary>
public class PropertyRepository : IPropertyRepository
{
    private readonly AppDbContext _context;

    public PropertyRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// קבל את כל הנכסים מה-DB
    /// </summary>
    public async Task<IEnumerable<Property>> GetAllAsync()
    {
        return await _context.Properties
            .Include(p => p.Images)
            .Include(p => p.NearbyLocations)  // הוסף שורה זו
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// קבל נכסים באזור מסוים (ברדיוס מ-Latitude/Longitude)
    /// זה משמש ל-Heatmap באנגולר
    /// 
    /// נוסחה לחישוב מרחק: Haversine Formula
    /// Distance = 2 * R * ASIN(SQRT(SIN((lat2-lat1)/2)^2 + COS(lat1) * COS(lat2) * SIN((lon2-lon1)/2)^2))
    /// כאשר R = 6371 (רדיוס כדור הארץ בקילומטרים)
    /// </summary>
    public async Task<IEnumerable<Property>> GetByAreaAsync(double lat, double lng, double radiusKm)
    {
        // קבע את גבולות ה-Latitude/Longitude (קירוב פשוט)
        // 1 degree ≈ 111 km
        var latDelta = radiusKm / 111.0;
        var lngDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

        return await _context.Properties
            .Where(p => p.Latitude >= lat - latDelta && p.Latitude <= lat + latDelta &&
                        p.Longitude >= lng - lngDelta && p.Longitude <= lng + lngDelta)
            .AsNoTracking()
            .OrderByDescending(p => p.AIScore) // הכי טובים ראשונים
            .ToListAsync();
    }

    /// <summary>
    /// צור נכס חדש וחזור עם ה-ID שנוצר
    /// </summary>
    public async Task<Property> CreateAsync(Property property)
    {
        // וודא שכל הנתונים תקינים
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        if (string.IsNullOrWhiteSpace(property.Title))
            throw new ArgumentException("Title is required", nameof(property));

        if (string.IsNullOrWhiteSpace(property.Address))
            throw new ArgumentException("Address is required", nameof(property));

        if (property.Price <= 0)
            throw new ArgumentException("Price must be greater than 0", nameof(property));

        // הוסף לDBو
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        return property;
    }

    /// <summary>
    /// עדכן נכס קיים
    /// </summary>
    public async Task UpdateAsync(Property property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        var existingProperty = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == property.Id);

        if (existingProperty == null)
            throw new InvalidOperationException($"Property with ID {property.Id} not found");

        // עדכון שדות רגילים
        existingProperty.Title = property.Title;
        existingProperty.Description = property.Description;
        existingProperty.Address = property.Address;
        existingProperty.Latitude = property.Latitude;
        existingProperty.Longitude = property.Longitude;
        existingProperty.Price = property.Price;
        existingProperty.Rooms = property.Rooms;
        existingProperty.AreaSqm = property.AreaSqm;
        existingProperty.AIScore = property.AIScore;
        existingProperty.EstimatedValue = property.EstimatedValue;
        existingProperty.AIAnalysisNotes = property.AIAnalysisNotes;

        // מחק ישנות
        var oldLocations = await _context.Locations
            .Where(l => l.PropertyId == existingProperty.Id)
            .ToListAsync();
        _context.Locations.RemoveRange(oldLocations);

        // הוסף חדשות — צור objects חדשים לחלוטין מהנתונים הגולמיים
        var newLocations = property.NearbyLocations
            .Select(loc => new Location
            {
                Name = loc.Name,
                Type = loc.Type,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                PropertyId = existingProperty.Id
            }).ToList();

        await _context.Locations.AddRangeAsync(newLocations);
        await _context.SaveChangesAsync();
    }

    // ============= PATCH UPDATE =============
    /// <summary>
    /// עדכון חלקי (PATCH) - רק את השדות שנשלחו
    /// </summary>
    public async Task<Property> PatchUpdateAsync(int id, Dictionary<string, object> updates)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null)
            throw new InvalidOperationException($"Property with ID {id} not found");

        foreach (var update in updates)
        {
            var key = update.Key.ToLower();
            var value = update.Value;

            // עזר כדי לחלץ את הערך בצורה בטוחה ללא קשר לסוג האובייקט
            string valueString = value?.ToString() ?? string.Empty;

            switch (key)
            {
                case "title":
                    property.Title = valueString;
                    break;
                case "description":
                    property.Description = valueString;
                    break;
                case "address":
                    property.Address = valueString;
                    break;
                case "price":
                    if (decimal.TryParse(valueString, out var price)) property.Price = price;
                    break;
                case "rooms":
                    if (int.TryParse(valueString, out var rooms)) property.Rooms = rooms;
                    break;
                case "areasqm":
                    if (double.TryParse(valueString, out var area)) property.AreaSqm = area;
                    break;
                case "aiscore":
                    if (double.TryParse(valueString, out var score)) property.AIScore = score;
                    break;
            }
        }

        // שורה קריטית: מודיעה ל-EF שהאובייקט השתנה (לפעמים FindAsync לא תופס שינויים פנימיים ב-Dictionary)
        _context.Entry(property).State = EntityState.Modified;

        await _context.SaveChangesAsync();
        return property;
    }

// ============= PAGINATION =============
/// <summary>
/// קבל נכסים עם pagination
/// </summary>
public async Task<PagedResponse<Property>> GetAllPaginatedAsync(int pageNumber = 1, int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var totalCount = await _context.Properties.CountAsync();
        var skip = (pageNumber - 1) * pageSize;

        var properties = await _context.Properties
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<Property>
        {
            Items = properties,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    // ============= SORTING + FILTERING =============
    /// <summary>
    /// חיפוש מתקדם עם sorting
    /// </summary>
    public async Task<PagedResponse<Property>> SearchAsync(
        string? searchTerm = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? minRooms = null,
        int? maxRooms = null,
        double? minScore = null,
        string? sortBy = "CreatedAt",
        bool ascending = false,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.Properties.AsQueryable();

        // Search by title or address
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(term) ||
                p.Address.ToLower().Contains(term));
        }

        // Price range
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        // Rooms
        if (minRooms.HasValue)
            query = query.Where(p => p.Rooms >= minRooms.Value);
        if (maxRooms.HasValue)
            query = query.Where(p => p.Rooms <= maxRooms.Value);

        // AI Score
        if (minScore.HasValue)
            query = query.Where(p => p.AIScore >= minScore.Value);

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "price" => ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
            "rooms" => ascending ? query.OrderBy(p => p.Rooms) : query.OrderByDescending(p => p.Rooms),
            "area" => ascending ? query.OrderBy(p => p.AreaSqm) : query.OrderByDescending(p => p.AreaSqm),
            "aiscore" => ascending ? query.OrderBy(p => p.AIScore) : query.OrderByDescending(p => p.AIScore),
            _ => query.OrderByDescending(p => p.CreatedAt) // Default
        };

        var totalCount = await query.CountAsync();
        var skip = (pageNumber - 1) * pageSize;

        var properties = await query
            .AsNoTracking()
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<Property>
        {
            Items = properties,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// מחק נכס לפי ID
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null)
            throw new InvalidOperationException($"Property with ID {id} not found");

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// קבל נכסים לפי מחיר (עבור סינון)
    /// </summary>
    public async Task<IEnumerable<Property>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        return await _context.Properties
            .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
            .AsNoTracking()
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    /// <summary>
    /// קבל נכסים לפי מספר חדרים
    /// </summary>
    public async Task<IEnumerable<Property>> GetByRoomsAsync(int rooms)
    {
        return await _context.Properties
            .Where(p => p.Rooms == rooms)
            .AsNoTracking()
            .OrderByDescending(p => p.AIScore)
            .ToListAsync();
    }

    /// <summary>
    /// קבל את הנכסים הטובים ביותר לפי AI Score
    /// </summary>
    public async Task<IEnumerable<Property>> GetTopPropertiesByScoreAsync(int count = 10)
    {
        return await _context.Properties
            .OrderByDescending(p => p.AIScore)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// בדוק אם נכס קיים
    /// </summary>
    public async Task<bool> PropertyExistsAsync(int id)
    {
        return await _context.Properties.AnyAsync(p => p.Id == id);
    }

    // דוגמה למימוש מועדפים
    public async Task<bool> AddToFavoritesAsync(int userId, int propertyId)
    {
        var user = await _context.Users.Include(u => u.SavedProperties).FirstOrDefaultAsync(u => u.Id == userId);
        var property = await _context.Properties.FindAsync(propertyId);

        if (user == null || property == null) return false;

        if (!user.SavedProperties.Any(p => p.Id == propertyId))
        {
            user.SavedProperties.Add(property);
            await _context.SaveChangesAsync();
        }
        return true;
    }

    // מימוש סטטיסטיקות ל-Dashboard (שימוש ב-AI Score ובמחירים)
    public async Task<object> GetDashboardStatsAsync()
    {
        return new
        {
            TotalProperties = await _context.Properties.CountAsync(),
            AveragePrice = await _context.Properties.AverageAsync(p => (double)p.Price),
            TopRatedCount = await _context.Properties.CountAsync(p => p.AIScore > 80),
            NewestProperties = await _context.Properties.CountAsync(p => p.CreatedAt > DateTime.UtcNow.AddDays(-7))
        };
    }

    // עדכון פונקציית GetByIdAsync שתכלול תמונות
    public async Task<Property?> GetByIdAsync(int id)
    {
        return await _context.Properties
            .Include(p => p.Images)
            .Include(p => p.NearbyLocations)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // 1. הסרה ממועדפים (היה חסר)
    public async Task<bool> RemoveFromFavoritesAsync(int userId, int propertyId)
    {
        var user = await _context.Users
            .Include(u => u.SavedProperties)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return false;

        var property = user.SavedProperties.FirstOrDefault(p => p.Id == propertyId);
        if (property != null)
        {
            user.SavedProperties.Remove(property);
            await _context.SaveChangesAsync();
        }
        return true;
    }

    // 2. הוספת תמונות (היה חסר)
    public async Task AddImagesToPropertyAsync(int propertyId, List<string> imageUrls)
    {
        var property = await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId);

        if (property == null) return;

        foreach (var url in imageUrls)
        {
            // הערה: וודאי שיש לך ישות שנקראת PropertyImage או שהשדה ב-Property מעודכן
            property.Images.Add(new PropertyImage { Url = url, PropertyId = propertyId });
        }
        await _context.SaveChangesAsync();
    }

    // 3. שליפת כמה נכסים לפי רשימת IDs (עבור השוואה)
    public async Task<IEnumerable<Property>> GetMultipleByIdsAsync(List<int> ids)
    {
        return await _context.Properties
            .Include(p => p.Images)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(int propertyId)
    {
        return await _context.PriceHistories
            .Where(ph => ph.PropertyId == propertyId)
            .OrderBy(ph => ph.RecordedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}