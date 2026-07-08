using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.DTOs.Auth;
using Api.Domain.Entities;
using Api.Repositories;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

/// <summary>
/// Service implementation for authentication operations.
/// Handles user registration, authentication, JWT generation, and token validation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _repository;
    private readonly IConfiguration _configuration;
    private const int MinUsernameLength = 3;
    private const int MinPasswordLength = 8;

    /// <summary>
    /// Initializes a new instance of the AuthService class.
    /// </summary>
    public AuthService(IUserRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<(bool Success, AuthResponseDto? Response, string? Error)> RegisterUserAsync(
        string username,
        string password,
        string passwordConfirmation,
        CancellationToken cancellationToken)
    {
        // Validate username length
        if (string.IsNullOrWhiteSpace(username) || username.Length < MinUsernameLength)
        {
            return (false, null, $"Username must be at least {MinUsernameLength} characters");
        }

        // Validate username format (alphanumeric + underscores only)
        if (!IsValidUsername(username))
        {
            return (false, null, "Username can only contain letters, numbers, and underscores");
        }

        // Validate password length
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
        {
            return (false, null, $"Password must be at least {MinPasswordLength} characters");
        }

        // Validate passwords match
        if (password != passwordConfirmation)
        {
            return (false, null, "Passwords do not match");
        }

        // Check if username already exists
        var usernameExists = await _repository.UsernameExistsAsync(username, cancellationToken);
        if (usernameExists)
        {
            // Return generic error for security (don't leak username existence)
            return (false, null, "The provided credentials are invalid");
        }

        // Hash password with BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        // Create user entity
        var user = new User
        {
            Username = username,
            PasswordHash = passwordHash,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Save to database
        try
        {
            var createdUser = await _repository.CreateAsync(user, cancellationToken);
            user.Id = createdUser.Id;
        }
        catch (Exception)
        {
            // Handle database errors
            return (false, null, "Registration failed. Please try again.");
        }

        // Generate JWT and return response
        var token = GenerateJwt(user);
        var response = new AuthResponseDto
        {
            Token = token,
            ExpiresIn = GetTokenExpirySeconds(),
            User = new UserDto
            {
                UserId = user.Id,
                Username = user.Username,
                CreatedDate = user.CreatedDate
            }
        };

        return (true, response, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, AuthResponseDto? Response, string? Error)> AuthenticateUserAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        // Find user by username (case-insensitive)
        var user = await _repository.GetByUsernameAsync(username, cancellationToken);
        if (user == null)
        {
            // Return generic error for security
            return (false, null, "The provided credentials are invalid");
        }

        // Verify password hash
        var passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!passwordValid)
        {
            // Return generic error for security
            return (false, null, "The provided credentials are invalid");
        }

        // Generate JWT and return response
        var token = GenerateJwt(user);
        var response = new AuthResponseDto
        {
            Token = token,
            ExpiresIn = GetTokenExpirySeconds(),
            User = new UserDto
            {
                UserId = user.Id,
                Username = user.Username,
                CreatedDate = user.CreatedDate
            }
        };

        return (true, response, null);
    }

    /// <inheritdoc />
    public string GenerateJwt(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "dotnet-react-starter";
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("username", user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public async Task<User?> ValidateTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            // Extract user ID from claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            // Load user from database
            var user = await _repository.GetByIdAsync(userId, cancellationToken);
            return user;
        }
        catch
        {
            // Token validation failed
            return null;
        }
    }

    /// <inheritdoc />
    public Task LogoutAsync(int userId, CancellationToken cancellationToken)
    {
        // For POC, logout is primarily client-side (token deletion)
        // This method is a placeholder for future token blacklist implementation
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates username format (alphanumeric + underscores).
    /// </summary>
    private static bool IsValidUsername(string username)
    {
        return username.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    /// <summary>
    /// Gets the token expiry time in seconds.
    /// </summary>
    private int GetTokenExpirySeconds()
    {
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440");
        return expiryMinutes * 60;
    }
}
