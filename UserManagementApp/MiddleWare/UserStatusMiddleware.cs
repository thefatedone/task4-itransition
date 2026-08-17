using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserManagementApp.Data;

namespace UserManagementApp.Middleware;

// important: this middleware enforces requirement #5 from the task —
// before EVERY request except registration/login, the server must verify
// that the authenticated user still exists and isn't blocked.
// If they are blocked/deleted, we force sign-out and redirect to the login page.
public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    // note: paths that are always allowed without this check (registration, login, confirmation, static assets)
    private static readonly string[] ExemptPaths =
    {
        "/login", "/register", "/confirm", "/css", "/js", "/lib", "/favicon"
    };

    public UserStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        if (ExemptPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                // note: AsNoTracking — this is a read-only check, we don't want to track the entity here
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

                if (user is null || user.Status == Models.UserStatus.Blocked)
                {
                    // nota bene: user was deleted or blocked since their cookie was issued — force logout
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Login");
                    return;
                }
            }
        }

        await _next(context);
    }
}

// note: small extension method to keep Program.cs tidy
public static class UserStatusMiddlewareExtensions
{
    public static IApplicationBuilder UseUserStatusCheck(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserStatusMiddleware>();
    }
}