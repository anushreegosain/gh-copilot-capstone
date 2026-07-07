using System.Security.Claims;
using System.Text;
using Api.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Api.Middleware;

/// <summary>
/// Middleware for JWT token validation and extraction.
/// Validates bearer tokens and populates HttpContext.User with claims.
/// </summary>
public class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the JwtAuthMiddleware class.
    /// </summary>
    public JwtAuthMiddleware(RequestDelegate next, ILogger<JwtAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to process the request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IAuthService authService, IConfiguration configuration)
    {
        var token = ExtractTokenFromHeader(context);
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var user = await authService.ValidateTokenAsync(token, context.RequestAborted);
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(ClaimTypes.Name, user.Username),
                        new("sub", user.Id.ToString())
                    };
                    var identity = new ClaimsIdentity(claims, "Bearer");
                    context.User = new ClaimsPrincipal(identity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT validation failed");
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Extracts the bearer token from the Authorization header.
    /// </summary>
    private static string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }
}

/// <summary>
/// Extension methods for registering JWT middleware.
/// </summary>
public static class JwtAuthMiddlewareExtensions
{
    /// <summary>
    /// Adds JWT authentication middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<JwtAuthMiddleware>();
    }
}
