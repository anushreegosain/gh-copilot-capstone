using Api.Domain.Entities;

namespace Api.Repositories;

/// <summary>
/// Repository interface for User entity data access.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by username (case-insensitive).
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates and persists a new user.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user with ID.</returns>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a username already exists (case-insensitive).
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if username exists; otherwise false.</returns>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken);
}
