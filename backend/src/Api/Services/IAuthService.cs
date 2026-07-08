using Api.DTOs.Auth;
using Api.Domain.Entities;

namespace Api.Services;

/// <summary>
/// Service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="passwordConfirmation">The password confirmation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple with success flag, auth response, and error message (if any).</returns>
    Task<(bool Success, AuthResponseDto? Response, string? Error)> RegisterUserAsync(
        string username,
        string password,
        string passwordConfirmation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple with success flag, auth response, and error message (if any).</returns>
    Task<(bool Success, AuthResponseDto? Response, string? Error)> AuthenticateUserAsync(
        string username,
        string password,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates a JWT token for a user.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <returns>The JWT token string.</returns>
    string GenerateJwt(User user);

    /// <summary>
    /// Validates a JWT token and returns the associated user.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if token is valid; otherwise null.</returns>
    Task<User?> ValidateTokenAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Logs out a user (cleanup operation).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    Task LogoutAsync(int userId, CancellationToken cancellationToken);
}
