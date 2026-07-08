using System.Text.Json.Serialization;

namespace Api.DTOs.Auth;

/// <summary>
/// Request payload for user registration (signup).
/// </summary>
public class SignupRequestDto
{
    /// <summary>
    /// The username for the new account (3-256 alphanumeric + underscore).
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password for the new account (minimum 8 characters).
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation (must match Password).
    /// </summary>
    [JsonPropertyName("passwordConfirmation")]
    public string PasswordConfirmation { get; set; } = string.Empty;
}
