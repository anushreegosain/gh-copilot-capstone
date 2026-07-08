namespace Api.Domain.Entities;

/// <summary>
/// Represents a user account in the system.
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The unique username (case-insensitive, 3+ alphanumeric/underscore characters).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The BCrypt-hashed password. Never stored as plaintext.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the account was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The timestamp when the account was last updated.
    /// </summary>
    public DateTime UpdatedDate { get; set; }
}
