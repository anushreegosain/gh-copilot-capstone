using System.Text.Json.Serialization;

namespace Api.DTOs.Auth;

/// <summary>
/// Request payload for user authentication (signin).
/// </summary>
public class SigninRequestDto
{
    /// <summary>
    /// The username of the account to authenticate.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password of the account.
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
