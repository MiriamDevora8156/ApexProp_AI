using System.Text.Json;
using ApexProp.API.Models;
using Microsoft.Data.SqlClient; // עבור .NET Core / .NET 5+

namespace ApexProp.API.Middleware;

/// <summary>
/// GlobalExceptionHandlingMiddleware - טיפול בשגיאות בצורה בטוחה
/// לא חושף פרטים רגישים ללקוח
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // בדוק גודל בקשה (הגנה מפני DDoS)
            if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB limit
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsJsonAsync(
                    ApiResponse.CreateError("Request body too large", "PAYLOAD_TOO_LARGE"));
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. RequestPath: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);

            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // בפיתוח - הראה פרטים מלאים
        // בייצור - אל תחשוף פרטים רגישים
        var isProduction = _environment.IsProduction();

        var (statusCode, apiResponse) = exception switch
        {
            // Validation errors
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                ApiResponse.CreateError(argEx.Message, "VALIDATION_ERROR")
            ),

            // Invalid operations
            InvalidOperationException invEx => (
                StatusCodes.Status400BadRequest,
                isProduction
                    ? ApiResponse.CreateError("Invalid operation", "INVALID_OPERATION")
                    : ApiResponse.CreateError(invEx.Message, "INVALID_OPERATION")
            ),

            // Not found
            KeyNotFoundException keyEx => (
                StatusCodes.Status404NotFound,
                ApiResponse.CreateError("Resource not found", "NOT_FOUND")
            ),

            // SQL errors - אל תחשוף
            SqlException sqlEx => (
                StatusCodes.Status500InternalServerError,
                ApiResponse.CreateError(
                    "Database error occurred",
                    isProduction ? "DATABASE_ERROR" : sqlEx.Message)
            ),

            // Default
            _ => (
                StatusCodes.Status500InternalServerError,
                ApiResponse.CreateError(
                    "An internal server error occurred",
                    isProduction ? "INTERNAL_SERVER_ERROR" : exception.GetType().Name)
            )
        };

        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(apiResponse);
    }
}