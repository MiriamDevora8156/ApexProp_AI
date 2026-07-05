using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using ApexProp.API.Middleware;
using ApexProp.Application.Mappings;
using ApexProp.Application.Validators;
using ApexProp.Domain.Interfaces;
using ApexProp.Infrastructure.Data;
using ApexProp.Infrastructure.Repositories;
using ApexProp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============= הגדרות אבטחה בסיסיות =============
// שימוש ב-HTTPS בהצפה
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443;
    options.RedirectStatusCode = 307; // Temporary Redirect
});

// ============= DbContext =============
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("ApexProp.Infrastructure");
            // הגדרות אבטחה ל-SQL Server
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(3);
        }
    )
);

// ============= Repositories =============
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ============= Services =============
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();

// ============= JWT Authentication - תיקון שמות משתנים =============
// שינוי מ-JwtSettings ל-Jwt (כפי שמופיע ב-appsettings.json)
var jwtSettings = builder.Configuration.GetSection("Jwt");
// שינוי מ-Key ל-SecretKey
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "YourSuperSecretKeyHere");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidateAudience = true,
        // ודאי שהשמות כאן תואמים למפתחות ב-JSON
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero
    };
});

// ============= AutoMapper =============
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ============= EXTERNAL SERVICES - HttpClient with Resilience =============
builder.Services.AddHttpClient<IExternalLocationService, OpenStreetMapService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddPolicyHandler((serviceProvider, request) =>
    {
        // בנה את ה-Policy עם גישה ל-ServiceProvider
        return HttpPolicyExtensions
            .HandleTransientHttpError() // טפל בכל HTTP error (5xx, timeout)
            .WaitAndRetryAsync(
                retryCount: 3, // נסה 3 פעמים
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2s, 4s, 8s
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // כאן אנחנו משתמשים ב-serviceProvider שקיבלנו ב-AddPolicyHandler
                    // זה השונה מהקוד המקורי שהיה בעיה בו
                    var logger = serviceProvider.GetRequiredService<ILogger<OpenStreetMapService>>();

                    var errorMessage = outcome.Exception?.Message
                        ?? $"HTTP {outcome.Result?.StatusCode}";

                    logger.LogWarning(
                        "Retry attempt {RetryCount} after {DelayMs}ms. Error: {Error}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        errorMessage);
                });
    });

// ============= AI Services =============
builder.Services.AddScoped<IAIScoreService, AIScoreService>();

// ============= FluentValidation - תאימות אוטומטית =============
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePropertyValidator>();

// ============= Controllers עם הגדרות אבטחה =============
builder.Services.AddControllers(options =>
{
    // הגדרת מקסימום פריטים ב-Collection (מונע הצפה של השרת)
    options.MaxModelBindingCollectionSize = 1024;
}) 
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Validation failed",
            errorCode = "VALIDATION_ERROR",
            errors = errors
        });
    };
});

// ============= Swagger - עם אבטחה JWT =============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ApexProp AI - Real Estate Platform",
        Version = "v1",
        Description = "Advanced API for property analysis with AI scoring",
        Contact = new OpenApiContact
        {
            Name = "ApexProp Team",
            Email = "contact@ApexProp.com"
        },
        License = new OpenApiLicense
        {
            Name = "Proprietary - All Rights Reserved",
            Url = new Uri("https://ApexProp.com/license")
        }
    });

    // JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

    // תיעוד מפורט
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "ApexProp.API.xml"), includeControllerXmlComments: true);
});

// ============= CORS - הגדרה מתוקנת וחסינה =============
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDevelopment", policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:51528", "http://localhost:4200")
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .AllowCredentials();
    });

    //options.AddPolicy("AllowAngularProduction", policyBuilder =>
    //{
    //    policyBuilder.WithOrigins("https://your-production-url.com")
    //                 .AllowAnyMethod()
    //                 .AllowAnyHeader()
    //                 .AllowCredentials();
    //});
    // מדיניות לפיתוח - מאפשרת לכל פורט מקומי לעבוד
    options.AddPolicy("AllowAngularDevelopment", policyBuilder =>
    {
        policyBuilder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .AllowCredentials()
                     .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});


// ============= Logging מתקדם =============
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();

    // רק בפיתוח
    if (builder.Environment.IsDevelopment())
    {
        config.SetMinimumLevel(LogLevel.Information);
    }
    else
    {
        config.SetMinimumLevel(LogLevel.Warning);
    }
});

// ============= Content Security & Response Compression =============
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.EnableForHttps = true;
});

// ============= Hsts - HTTPS Strict Transport Security =============
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

var app = builder.Build();

// ============= Database Initialization =============
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Initializing database...");
        context.Database.EnsureCreated();
        SeedData.Initialize(context);
        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database");
        // אל תזרוק - תן ללאפליקציה להתחיל בדי-קיים
    }
}

// ============= Middleware - בסדר נכון (חשוב מאוד!) =============
// 7. CORS (חייב להיות לפני Authentication ו-Authorization!)
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAngularDevelopment");
}
else
{
    app.UseCors("AllowAngularProduction");
}

// 1. Exception Handling (ראשון!)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. Response Compression
app.UseResponseCompression();

// 4. HSTS (HTTP Strict Transport Security)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// 5. Security Headers Middleware (נוסיף בהמשך)
app.UseSecurityHeaders();

// 6. Swagger (רק בפיתוח)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApexProp AI API v1");
        c.RoutePrefix = string.Empty;
        c.DefaultModelsExpandDepth(-1); // אל תציג schemas בברירת המחדל
    });
}
else
{
    // בייצור - סגור את Swagger
    app.Map("/swagger", _ => _.Run(async context =>
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsJsonAsync(new { error = "Not Found" });
    }));
}

// 8. Authentication (אם יש)
app.UseAuthentication();

// 9. Authorization
app.UseAuthorization();

// 10. Routing
app.MapControllers();

// 11. Health Check Endpoint
app.MapGet("/api/health", () =>
{
    return new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    };
}).WithOpenApi().Produces(200);

builder.Configuration.AddUserSecrets<Program>();

app.Run();