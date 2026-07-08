using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Domain.Entities;
using Api.DTOs.Auth;
using Api.Repositories;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Api.Tests.Services;

/// <summary>
/// Unit tests for AuthService authentication and registration operations.
/// Uses Moq for repository isolation and AAA pattern for test clarity.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Configure mock JWT settings
        _mockConfiguration.Setup(c => c["Jwt:Secret"]).Returns("super-secret-key-1234567890123456789012345678901234");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("dotnet-react-starter");
        _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("1440");

        _authService = new AuthService(_mockRepository.Object, _mockConfiguration.Object);
    }

    #region RegisterUserAsync Tests

    [Fact]
    public async Task RegisterUserAsync_WithValidCredentials_ReturnsSuccessAndToken()
    {
        // Arrange
        var username = "validuser";
        var password = "validpassword123";
        var passwordConfirmation = "validpassword123";
        
        _mockRepository.Setup(r => r.UsernameExistsAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => new User 
            { 
                Id = 1,
                Username = u.Username,
                PasswordHash = u.PasswordHash,
                CreatedDate = u.CreatedDate,
                UpdatedDate = u.UpdatedDate
            });

        // Act
        var result = await _authService.RegisterUserAsync(
            username, 
            password, 
            passwordConfirmation, 
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.NotEmpty(result.Response.Token);
        Assert.Equal(1, result.Response.User.UserId);
        Assert.Equal(username, result.Response.User.Username);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task RegisterUserAsync_WithDuplicateUsername_ReturnsFalseWithError()
    {
        // Arrange
        var username = "existinguser";
        var password = "validpassword123";
        var passwordConfirmation = "validpassword123";
        
        _mockRepository.Setup(r => r.UsernameExistsAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterUserAsync(
            username, 
            password, 
            passwordConfirmation, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Contains("credentials are invalid", result.Error);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task RegisterUserAsync_WithUsernameTooShort_ReturnsFalseWithError()
    {
        // Arrange
        var username = "ab";  // Less than 3 characters
        var password = "validpassword123";
        var passwordConfirmation = "validpassword123";

        // Act
        var result = await _authService.RegisterUserAsync(
            username, 
            password, 
            passwordConfirmation, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Contains("at least 3 characters", result.Error);
    }

    [Fact]
    public async Task RegisterUserAsync_WithInvalidUsernameCharacters_ReturnsFalseWithError()
    {
        // Arrange
        var username = "invalid-user!";  // Contains invalid characters
        var password = "validpassword123";
        var passwordConfirmation = "validpassword123";

        // Act
        var result = await _authService.RegisterUserAsync(
            username, 
            password, 
            passwordConfirmation, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Contains("letters, numbers, and underscores", result.Error);
    }

    [Fact]
    public async Task RegisterUserAsync_WithPasswordTooShort_ReturnsFalseWithError()
    {
        // Arrange
        var username = "validuser";
        var password = "short";  // Less than 8 characters
        var passwordConfirmation = "short";

        // Act
        var result = await _authService.RegisterUserAsync(
            username, 
            password, 
            passwordConfirmation, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Contains("at least 8 characters", result.Error);
    }

    [Fact]
    public async Task RegisterUserAsync_WithPasswordMismatch_ReturnsFalseWithError()
    {
        // Arrange
        var username = "validuser";
        var password = "validpassword123";
        var passwordConfirmation = "differentpassword456";

        // Act
        var result = await _authService.RegisterUserAsync(
            username, 
            password, 
            passwordConfirmation, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Contains("do not match", result.Error);
    }

    [Fact]
    public async Task RegisterUserAsync_WithEmptyUsername_ReturnsFalseWithError()
    {
        // Arrange
        var username = "";
        var password = "validpassword123";
        var passwordConfirmation = "validpassword123";

        // Act
        var result = await _authService.RegisterUserAsync(
            username, 
            password, 
            passwordConfirmation, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task RegisterUserAsync_WhenDatabaseThrowsException_ReturnsFalseWithError()
    {
        // Arrange
        var username = "validuser";
        var password = "validpassword123";
        var passwordConfirmation = "validpassword123";
        
        _mockRepository.Setup(r => r.UsernameExistsAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _authService.RegisterUserAsync(
            username, 
            password, 
            passwordConfirmation, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Contains("Registration failed", result.Error);
    }

    #endregion

    #region AuthenticateUserAsync Tests

    [Fact]
    public async Task AuthenticateUserAsync_WithValidCredentials_ReturnsSuccessAndToken()
    {
        // Arrange
        var username = "testuser";
        var password = "validpassword123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        
        var user = new User
        {
            Id = 1,
            Username = username,
            PasswordHash = passwordHash,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        
        _mockRepository.Setup(r => r.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.AuthenticateUserAsync(
            username, 
            password, 
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.NotEmpty(result.Response.Token);
        Assert.Equal(1, result.Response.User.UserId);
        Assert.Equal(username, result.Response.User.Username);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task AuthenticateUserAsync_WithInvalidPassword_ReturnsFalseWithError()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";
        var correctPassword = "validpassword123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword, workFactor: 12);
        
        var user = new User
        {
            Id = 1,
            Username = username,
            PasswordHash = passwordHash,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        
        _mockRepository.Setup(r => r.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.AuthenticateUserAsync(
            username, 
            password, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Contains("credentials are invalid", result.Error);
    }

    [Fact]
    public async Task AuthenticateUserAsync_WithUnknownUsername_ReturnsFalseWithError()
    {
        // Arrange
        var username = "nonexistentuser";
        var password = "anypassword";
        
        _mockRepository.Setup(r => r.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.AuthenticateUserAsync(
            username, 
            password, 
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Contains("credentials are invalid", result.Error);
    }

    #endregion

    #region GenerateJwt Tests

    [Fact]
    public void GenerateJwt_WithValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "somehash",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var token = _authService.GenerateJwt(user);

        // Assert
        Assert.NotEmpty(token);
        Assert.IsType<string>(token);
        
        // Verify token structure (3 parts separated by dots)
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateJwt_GeneratedToken_CanBeParsed()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            Username = "jwtuser",
            PasswordHash = "hash",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var token = _authService.GenerateJwt(user);
        
        // Parse token manually (just verify it's not malformed)
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.NotNull(jwtToken);
        Assert.Contains(jwtToken.Claims, c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier 
            && c.Value == user.Id.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Name 
            && c.Value == user.Username);
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "tokenuser",
            PasswordHash = "hash",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        
        var token = _authService.GenerateJwt(user);
        
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.ValidateTokenAsync(token, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("tokenuser", result.Username);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _authService.ValidateTokenAsync(invalidToken, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("-1");
        var expiredService = new AuthService(_mockRepository.Object, _mockConfiguration.Object);
        
        var user = new User
        {
            Id = 1,
            Username = "expireduser",
            PasswordHash = "hash",
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            UpdatedDate = DateTime.UtcNow.AddDays(-1)
        };
        
        var expiredToken = expiredService.GenerateJwt(user);
        
        // Small delay to ensure token is expired
        await Task.Delay(100);

        // Act
        var result = await _authService.ValidateTokenAsync(expiredToken, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithUserId_CompletesSuccessfully()
    {
        // Arrange
        var userId = 1;

        // Act
        var task = _authService.LogoutAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(task);
        await task; // Should complete without throwing
    }

    #endregion
}
