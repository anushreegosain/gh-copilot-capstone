using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs.Auth;
using Api.DTOs.Common;
using Xunit;

namespace Api.Tests.Integration;

/// <summary>
/// Integration tests for the complete authentication flow.
/// Tests signup, signin, and authenticated requests end-to-end using WebApplicationFactory.
/// </summary>
public sealed class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AuthenticationFlow_SignupThenSignin_ReturnsTokenBothTimes()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "integrationuser",
            Password = "securepassword123",
            PasswordConfirmation = "securepassword123"
        };

        var signupResponse = await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);

        Assert.Equal(HttpStatusCode.Created, signupResponse.StatusCode);
        var signupEnvelope = await ReadJsonAsync<ItemResponseDto<AuthResponseDto>>(signupResponse);
        Assert.NotNull(signupEnvelope?.Item);
        Assert.False(string.IsNullOrWhiteSpace(signupEnvelope.Item.Token));

        var signinRequest = new SigninRequestDto
        {
            Username = "integrationuser",
            Password = "securepassword123"
        };

        var signinResponse = await _client.PostAsJsonAsync("/v1/auth/signin", signinRequest);

        Assert.Equal(HttpStatusCode.OK, signinResponse.StatusCode);
        var signinEnvelope = await ReadJsonAsync<ItemResponseDto<AuthResponseDto>>(signinResponse);
        Assert.NotNull(signinEnvelope?.Item);
        Assert.False(string.IsNullOrWhiteSpace(signinEnvelope.Item.Token));
    }

    [Fact]
    public async Task AuthenticationFlow_SignupWithDuplicateUsername_ReturnsBadRequest()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "duplicateuser",
            Password = "securepassword123",
            PasswordConfirmation = "securepassword123"
        };

        var firstSignup = await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);
        Assert.Equal(HttpStatusCode.Created, firstSignup.StatusCode);

        var secondSignup = await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);

        Assert.Equal(HttpStatusCode.BadRequest, secondSignup.StatusCode);
        var errorEnvelope = await ReadJsonAsync<ErrorResponseDto>(secondSignup);
        Assert.NotNull(errorEnvelope);
        Assert.Equal("ORG-VAL-001", errorEnvelope.Code);
    }

    [Fact]
    public async Task Signin_WithInvalidPassword_ReturnsUnauthorized()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "wrongpasswordtest",
            Password = "correctpassword123",
            PasswordConfirmation = "correctpassword123"
        };

        await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);

        var signinRequest = new SigninRequestDto
        {
            Username = "wrongpasswordtest",
            Password = "wrongpassword"
        };

        var response = await _client.PostAsJsonAsync("/v1/auth/signin", signinRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var errorEnvelope = await ReadJsonAsync<ErrorResponseDto>(response);
        Assert.NotNull(errorEnvelope);
        Assert.Equal("ORG-AUT-001", errorEnvelope.Code);
    }

    [Fact]
    public async Task Signin_WithNonexistentUser_ReturnsUnauthorized()
    {
        var signinRequest = new SigninRequestDto
        {
            Username = "nonexistentuser",
            Password = "anypassword"
        };

        var response = await _client.PostAsJsonAsync("/v1/auth/signin", signinRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var errorEnvelope = await ReadJsonAsync<ErrorResponseDto>(response);
        Assert.NotNull(errorEnvelope);
        Assert.Equal("ORG-AUT-001", errorEnvelope.Code);
    }

    [Fact]
    public async Task Signup_WithEmptyUsername_ReturnsBadRequest()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "",
            Password = "validpassword123",
            PasswordConfirmation = "validpassword123"
        };

        var response = await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorEnvelope = await ReadJsonAsync<ErrorResponseDto>(response);
        Assert.NotNull(errorEnvelope);
        Assert.Equal("ORG-VAL-001", errorEnvelope.Code);
    }

    [Fact]
    public async Task Signup_WithPasswordTooShort_ReturnsBadRequest()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "newuser",
            Password = "short",
            PasswordConfirmation = "short"
        };

        var response = await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorEnvelope = await ReadJsonAsync<ErrorResponseDto>(response);
        Assert.NotNull(errorEnvelope);
        Assert.Equal("ORG-VAL-001", errorEnvelope.Code);
    }

    [Fact]
    public async Task Signup_WithPasswordsMismatched_ReturnsBadRequest()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "newuser",
            Password = "password123",
            PasswordConfirmation = "differentpassword456"
        };

        var response = await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorEnvelope = await ReadJsonAsync<ErrorResponseDto>(response);
        Assert.NotNull(errorEnvelope);
        Assert.Equal("ORG-VAL-001", errorEnvelope.Code);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithValidToken_ReturnsCurrentUser()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "tokenuser",
            Password = "securepassword123",
            PasswordConfirmation = "securepassword123"
        };

        var signupResponse = await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);
        var signupEnvelope = await ReadJsonAsync<ItemResponseDto<AuthResponseDto>>(signupResponse);
        var token = signupEnvelope?.Item?.Token;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var requestWithToken = new HttpRequestMessage(HttpMethod.Get, "/v1/auth/me");
        requestWithToken.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(requestWithToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var userEnvelope = await ReadJsonAsync<ItemResponseDto<UserDto>>(response);
        Assert.NotNull(userEnvelope?.Item);
        Assert.Equal("tokenuser", userEnvelope.Item.Username);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignupResponse_FollowsEnvelopeStructure()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "envelopetest",
            Password = "securepassword123",
            PasswordConfirmation = "securepassword123"
        };

        var response = await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);
        var envelope = await ReadJsonAsync<ItemResponseDto<AuthResponseDto>>(response);

        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Item);
        Assert.NotNull(envelope.Metadata);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Metadata.TransactionId));
        Assert.NotNull(envelope.Links);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Links.Self));
    }

    [Fact]
    public async Task SigninResponse_IncludesValidTokenStructure()
    {
        var signupRequest = new SignupRequestDto
        {
            Username = "tokenstructuretest",
            Password = "securepassword123",
            PasswordConfirmation = "securepassword123"
        };

        await _client.PostAsJsonAsync("/v1/auth/signup", signupRequest);

        var signinRequest = new SigninRequestDto
        {
            Username = "tokenstructuretest",
            Password = "securepassword123"
        };

        var response = await _client.PostAsJsonAsync("/v1/auth/signin", signinRequest);
        var envelope = await ReadJsonAsync<ItemResponseDto<AuthResponseDto>>(response);

        Assert.NotNull(envelope?.Item?.Token);
        var tokenParts = envelope.Item.Token.Split('.');
        Assert.Equal(3, tokenParts.Length);
        Assert.NotEqual(0, envelope.Item.ExpiresIn);
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
