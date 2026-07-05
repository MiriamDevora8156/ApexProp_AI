using Microsoft.EntityFrameworkCore;
using ApexProp.Domain.Entities;

namespace ApexProp.Infrastructure.Data;

/// <summary>
/// AppDbContext - הקשר בין האפליקציה למסד הנתונים SQL Server
/// זה ה"מתורגמן" בין C# לSQL - כל שינוי אומרים לו, והוא מדבר עם SQL
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // ============= DbSets - הטבלאות שלנו =============
    public DbSet<Property> Properties { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistories { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<PropertyImage> PropertyImages { get; set; } = null!; // <--- הטבלה החדשה לתמונות

    // ============= OnModelCreating - הגדרת כללי הטבלאות =============
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============= טבלת PROPERTIES =============
        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("Properties");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Title).IsRequired().HasMaxLength(200);

            // השדה החדש של התיאור
            entity.Property(p => p.Description).HasMaxLength(2000).IsRequired(false);

            entity.Property(p => p.Address).IsRequired().HasMaxLength(500);
            entity.Property(p => p.Latitude).IsRequired().HasColumnType("float");
            entity.Property(p => p.Longitude).IsRequired().HasColumnType("float");
            entity.Property(p => p.Price).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(p => p.Rooms).IsRequired();
            entity.Property(p => p.AreaSqm).IsRequired().HasColumnType("float");

            entity.Property(p => p.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(p => p.AIScore).IsRequired().HasDefaultValue(0.0);

            // --- השדות החדשים של ה-AI ---
            entity.Property(p => p.EstimatedValue).HasColumnType("decimal(18,2)").IsRequired(false);
            entity.Property(p => p.AIAnalysisNotes).HasColumnType("nvarchar(max)").IsRequired(false);

            entity.HasIndex(p => new { p.Latitude, p.Longitude }).HasDatabaseName("IX_Properties_Location");
            entity.HasIndex(p => p.Price).HasDatabaseName("IX_Properties_Price");
            // הגדרת קשר: לנכס אחד יש בעלים אחד (משתמש)
            entity.HasOne(p => p.Owner)
                  .WithMany() // משתמש יכול להיות בעלים של הרבה נכסים
                  .HasForeignKey(p => p.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict); // אם מוחקים משתמש, לא נמחק את הנכסים אוטומטית (או שנהנה ל-Cascade אם תרצי)
        });

        // ============= טבלת PROPERTYIMAGES =============
        modelBuilder.Entity<PropertyImage>(entity =>
        {
            entity.ToTable("PropertyImages");
            entity.HasKey(pi => pi.Id);

            entity.Property(pi => pi.Url)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("Url");

            entity.Property(pi => pi.Description)
                .HasMaxLength(500)
                .HasColumnName("Description");

            entity.Property(pi => pi.DisplayOrder)
                .HasDefaultValue(0)
                .HasColumnName("DisplayOrder");

            entity.Property(pi => pi.UploadedAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()")
                .HasColumnName("UploadedAt");

            entity.Property(pi => pi.PropertyId)
                .IsRequired()
                .HasColumnName("PropertyId");

            entity.HasOne(pi => pi.Property)
                  .WithMany(p => p.Images)
                  .HasForeignKey(pi => pi.PropertyId)
                  .HasConstraintName("FK_PropertyImages_Properties")
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pi => pi.PropertyId)
                .HasDatabaseName("IX_PropertyImages_PropertyId");
        });
        // ============= טבלת LOCATIONS =============
        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("Name");

            entity.Property(l => l.Type)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Type");

            entity.Property(l => l.Latitude)
                .IsRequired()
                .HasColumnType("float")
                .HasColumnName("Latitude");

            entity.Property(l => l.Longitude)
                .IsRequired()
                .HasColumnType("float")
                .HasColumnName("Longitude");

            entity.Property(l => l.PropertyId)
                .IsRequired()
                .HasColumnName("PropertyId");

            entity.HasOne(l => l.Property)
                  .WithMany(p => p.NearbyLocations)
                  .HasForeignKey(l => l.PropertyId)
                  .HasConstraintName("FK_Locations_Properties")
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => l.PropertyId)
                .HasDatabaseName("IX_Locations_PropertyId");

            entity.HasIndex(l => l.Type)
                .HasDatabaseName("IX_Locations_Type");
        });
     
        // ============= טבלת PRICEHISTORY =============
        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.ToTable("PriceHistories");
            entity.HasKey(ph => ph.Id);

            entity.Property(ph => ph.Price).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(ph => ph.RecordedAt).IsRequired().HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(ph => ph.Property)
                  .WithMany(p => p.PriceHistories) // <--- מעודכן לשימוש ברשימה שבנכס
                  .HasForeignKey(ph => ph.PropertyId)
                  .HasConstraintName("FK_PriceHistories_Properties")
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ph => ph.PropertyId).HasDatabaseName("IX_PriceHistories_PropertyId");
            entity.HasIndex(ph => ph.RecordedAt).HasDatabaseName("IX_PriceHistories_RecordedAt");
        });

        // ============= טבלת USERS =============
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);

            // --- שדות פרופיל חדשים ---
            entity.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired(false);
            entity.Property(u => u.AvatarUrl).HasMaxLength(1000).IsRequired(false);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");

            entity.Property(u => u.CreatedAt).IsRequired().HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email_Unique");

            // ============= קשר מועדפים (רבים-לרבים) =============
            entity.HasMany(u => u.SavedProperties)
                  .WithMany(p => p.SavedByUsers)
                  .UsingEntity(j => j.ToTable("UserFavoriteProperties")); // SQL ייצור טבלת ביניים בשם הזה

            entity.Property(u => u.RefreshToken)
        .HasMaxLength(500)
        .IsRequired(false);

            entity.Property(u => u.RefreshTokenExpiry)
                .HasColumnType("datetime2")
                .IsRequired(false);
        });
    }
}