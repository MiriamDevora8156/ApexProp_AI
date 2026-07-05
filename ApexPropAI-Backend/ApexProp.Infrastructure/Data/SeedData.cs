using ApexProp.Domain.Entities;

namespace ApexProp.Infrastructure.Data;

/// <summary>
/// SeedData - מלא את DB בנתונים ראשוניים (100 נכסים בירושלים)
/// זה משמש לפיתוח - כדי שתהיה לנו דטא לתست עליה
/// </summary>
public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        // אם כבר יש נכסים ב-DB - אל תוסיף עוד
        if (context.Properties.Any())
        {
            return;
        }

        var defaultOwner = context.Users.FirstOrDefault();
        if (defaultOwner == null)
        {
            // אם אין משתמשים בכלל, ניצור אחד זמני כדי שה-Seed לא ייכשל
            defaultOwner = new User
            {
                FullName = "System Admin",
                Email = "admin@system.com",
                PasswordHash = "...",
                Role = "Admin"
            };
            context.Users.Add(defaultOwner);
            context.SaveChanges();
        }

        // ============= נכסים בירושלים - שכונות אמיתיות =============
        var properties = new List<Property>
        {
            // ============= תלפיות =============
            new Property
            {
                Title = "דירה אלגנטית בתלפיות",
                Address = "רחוב אלנבי 45, ירושלים",
                Latitude = 31.7683,
                Longitude = 35.2137,
                Price = 3500000,
                Rooms = 4,
                AreaSqm = 150,
                AIScore = 8.7,
                OwnerId = defaultOwner.Id
            },
            new Property
            {
                Title = "דירה חדישה בתלפיות",
                Address = "רחוב קינג ג'ורג 78, ירושלים",
                Latitude = 31.7690,
                Longitude = 35.2145,
                Price = 3800000,
                Rooms = 4,
                AreaSqm = 160,
                AIScore = 8.9,
                OwnerId = defaultOwner.Id
            },
            new Property
            {
                Title = "פנטהאוז בתלפיות",
                Address = "אמנואל רימוב 12, ירושלים",
                Latitude = 31.7700,
                Longitude = 35.2160,
                Price = 5200000,
                Rooms = 5,
                AreaSqm = 210,
                AIScore = 9.2,
                OwnerId = defaultOwner.Id
            },

            // ============= גאולה =============
            new Property
            {
                Title = "דירה בגאולה",
                Address = "רחוב מלכי ישראל 34, ירושלים",
                Latitude = 31.7850,
                Longitude = 35.2250,
                Price = 2800000,
                Rooms = 3,
                AreaSqm = 120,
                AIScore = 7.5,
                OwnerId = defaultOwner.Id
            },
            new Property
            {
                Title = "דופלקס בגאולה",
                Address = "רחוב חיים בן אטר 56, ירושלים",
                Latitude = 31.7870,
                Longitude = 35.2270,
                Price = 3200000,
                Rooms = 4,
                AreaSqm = 140,
                AIScore = 7.8,
                OwnerId = defaultOwner.Id
            },

            // ============= בית הכרם =============
            new Property
            {
                Title = "דירה עם נוף בבית הכרם",
                Address = "רחוב בית הכרם 10, ירושלים",
                Latitude = 31.7950,
                Longitude = 35.1850,
                Price = 3100000,
                Rooms = 4,
                AreaSqm = 135,
                AIScore = 8.1,
                OwnerId = defaultOwner.Id
            },
            new Property
            {
                Title = "בית משפחה בבית הכרם",
                Address = "רחוב הנוף 22, ירושלים",
                Latitude = 31.7970,
                Longitude = 35.1870,
                Price = 4200000,
                Rooms = 5,
                AreaSqm = 180,
                AIScore = 8.4,
                OwnerId = defaultOwner.Id
            },

            // ============= אחרות =============
            new Property
            {
                Title = "דירה במרכז ירושלים",
                Address = "רחוב ג'מלא שרג 88, ירושלים",
                Latitude = 31.7880,
                Longitude = 35.2180,
                Price = 2500000,
                Rooms = 3,
                AreaSqm = 110,
                AIScore = 7.2,
                OwnerId = defaultOwner.Id
            },
            new Property
            {
                Title = "דירה בנחל קדרון",
                Address = "רחוב קדרון 5, ירושלים",
                Latitude = 31.7700,
                Longitude = 35.2350,
                Price = 2200000,
                Rooms = 2,
                AreaSqm = 95,
                AIScore = 6.8,
                OwnerId = defaultOwner.Id
            }
        };

        // הוסף עוד עשרות נכסים אוטומטית (כדי לעמוד על ~100 נכסים)
        for (int i = 0; i < 90; i++)
        {
            properties.Add(new Property
            {
                Title = $"דירה #{i + 1} בירושלים",
                Address = $"רחוב דמוי {i + 1}, ירושלים",
                Latitude = 31.7700 + (Random.Shared.NextDouble() * 0.1),
                Longitude = 35.2000 + (Random.Shared.NextDouble() * 0.1),
                Price = 2000000 + (i * 10000),
                Rooms = 2 + (i % 4),
                AreaSqm = 80 + (i * 0.5),
                AIScore = 5.0 + (i % 40) / 10.0,
                OwnerId = defaultOwner.Id
            });
        }

        // הוסף את כל הנכסים ל-DB
        context.Properties.AddRange(properties);
        context.SaveChanges();

        // ============= הוסף Locations (מיקומים קרובים) =============
        var locations = new List<Location>();

        // לכל נכס, הוסף 2-3 locations
        var propertyIds = context.Properties.Select(p => p.Id).ToList();

        var locationTypes = new[] { "school", "train_station", "park", "hospital", "supermarket" };
        var locationNames = new Dictionary<string, string[]>
        {
            { "school", new[] { "בית ספר תל", "בית ספר בנות", "בית ספר כללי" } },
            { "train_station", new[] { "תחנת רכבת ירושלים", "תחנת רכבל" } },
            { "park", new[] { "פארק הר הצופים", "פארק אור הירושה" } },
            { "hospital", new[] { "בית חולים הדסה", "בית חולים שערי צדק" } },
            { "supermarket", new[] { "סופר מרקט טיים", "סופר מרקט פראג" } }
        };

        foreach (var propertyId in propertyIds)
        {
            for (int i = 0; i < Random.Shared.Next(2, 4); i++)
            {
                var type = locationTypes[Random.Shared.Next(locationTypes.Length)];
                var names = locationNames[type];
                var name = names[Random.Shared.Next(names.Length)];

                locations.Add(new Location
                {
                    Name = name,
                    Type = type,
                    Latitude = 31.7700 + (Random.Shared.NextDouble() * 0.05),
                    Longitude = 35.2000 + (Random.Shared.NextDouble() * 0.05),
                    PropertyId = propertyId
                });
            }
        }

        context.Locations.AddRange(locations);
        context.SaveChanges();
    }
}