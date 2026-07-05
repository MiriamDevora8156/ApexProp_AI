namespace ApexProp.API.Models;

/// <summary>
/// ApiResponse - כל תשובה מה-API תהיה בפורמט זה
/// זה מקצועי ומאפשר ללקוח לדעת בדיוק מה קרה
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// האם הבקשה הצליחה
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// הנתונים שחוזרים
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// הודעת שגיאה (אם יש)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// קוד השגיאה (אם יש)
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Timestamp של מתי חזרה התשובה
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Constructor ל-Success
    /// </summary>
    public ApiResponse(T data)
    {
        Success = true;
        Data = data;
        Message = null;
    }

    /// <summary>
    /// Constructor ל-Error
    /// </summary>
    public ApiResponse(bool success, string message, string? errorCode = null)
    {
        Success = success;
        Message = message;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Helper - Success Response
    /// </summary>
    public static ApiResponse<T> CreateSuccess(T data) => new(data);

    /// <summary>
    /// Helper - Error Response
    /// </summary>
    public static ApiResponse<T> CreateError(string message, string? errorCode = null)
        => new(false, message, errorCode);
}

/// <summary>
/// ApiResponse בלי נתונים
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse(bool success, string message, string? errorCode = null)
    {
        Success = success;
        Message = message;
        ErrorCode = errorCode;
    }

    public static ApiResponse CreateSuccess(string message = "Operation successful")
        => new(true, message);

    public static ApiResponse CreateError(string message, string? errorCode = null)
        => new(false, message, errorCode);
}