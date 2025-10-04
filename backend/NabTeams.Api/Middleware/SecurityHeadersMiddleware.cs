using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace NabTeams.Api.Middleware;

public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                await next();
                return;
            }

            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
            context.Response.Headers.TryAdd("X-XSS-Protection", "0");
            context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

            if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                const string csp = "default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; font-src 'self' data:; script-src 'self'; connect-src 'self' wss:";
                context.Response.Headers.TryAdd("Content-Security-Policy", csp);
            }

            await next();
        });
    }
}
