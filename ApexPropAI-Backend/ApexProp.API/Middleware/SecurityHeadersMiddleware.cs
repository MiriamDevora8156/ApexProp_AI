namespace ApexProp.API.Middleware;

/// <summary>
/// SecurityHeadersMiddleware - הוספת headers אבטחה
/// מונע attacks כמו XSS, Clickjacking וכו'
/// </summary>
public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // ============= XSS Protection =============
            // מונע שימוש בטג script זדוני
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

            // ============= Content Security Policy =============
            // מגביל משאבים שמותר לטעון
            context.Response.Headers.Add("Content-Security-Policy",
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'");

            // ============= Referrer Policy =============
            // לא שלח referrer לאתרים חיצוניים
            context.Response.Headers.Add("Referrer-Policy", "no-referrer");

            // ============= Permissions Policy =============
            // בדוק הרשאות של דפדפן
            context.Response.Headers.Add("Permissions-Policy",
                "geolocation=(), microphone=(), camera=(), payment=()");

            // ============= Remove Server Header =============
            // אל תחשוף ת"י של השרת
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");
            context.Response.Headers.Remove("X-AspNet-Version");

            // ============= Strict Transport Security =============
            context.Response.Headers.Add("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");

            await next();
        });

        return app;
    }
}