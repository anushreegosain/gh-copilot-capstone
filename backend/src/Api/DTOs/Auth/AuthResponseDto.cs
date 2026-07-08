using System.Text.Json.Serialization;

namespace Api.DTOs.Auth;

/// <summary>
/// Response payload for authentication endpoints (signup, signin).
/// Contains the JWT token and authenticated user information.
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// The JWT bearer token for subsequent authenticated requests.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The token expiry time in seconds (e.g., 86400 for 24 hours).
    /// </summary>
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// The authenticated user information.
    /// </summary>
    [JsonPropertyName("user")]
    public UserDto User { get; set; } = new();
}
