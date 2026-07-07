using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Api.Controllers;
using Api.DTOs.Auth;
using Api.DTOs.Common;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Tests.Controllers;

/// <summary>
/// Unit tests for AuthController endpoints (signup, signin, get current user).
/// Uses Moq to isolate service layer and validates envelope response structure.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
    }

    #region SignupAsync Tests

    [Fact]
    public async Task SignupAsync_WithValidRequest_ReturnsCreatedStatusAndToken()
    {
        // Arrange
        var request = new SignupRequestDto
        {
            Username = "newuser",
            Password = "validpassword123",
            PasswordConfirmation = "validpassword123"
        };
        
        var authResponse = new AuthResponseDto
        {
            Token = "jwt.token.here",
            ExpiresIn = 86400,
            User = new UserDto { UserId = 1, Username = "newuser", CreatedDate = DateTime.UtcNow }
        };
        
        _mockAuthService.Setup(s => s.RegisterUserAsync(
                request.Username,
                request.Password,
                request.PasswordConfirmation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, authResponse, null));

        // Act
        var result = await _controller.SignupAsync(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        
        var envelope = Assert.IsType<ItemResponseDto<AuthResponseDto>>(createdResult.Value);
        Assert.NotNull(envelope.Item);
        Assert.Equal("jwt.token.here", envelope.Item.Token);
        Assert.Equal(1, envelope.Item.User.UserId);
        Assert.NotNull(envelope.Metadata);
        Assert.NotNull(envelope.Links);
    }

    [Fact]
    public async Task SignupAsync_WithEmptyUsername_ReturnsBadRequestWithError()
    {
        // Arrange
        var request = new SignupRequestDto
        {
            Username = "",
            Password = "validpassword123",
            PasswordConfirmation = "validpassword123"
        };

        // Act
        var result = await _controller.SignupAsync(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(badRequest.Value);
        Assert.Equal("ORG-VAL-001", errorResponse.Code);
        Assert.NotNull(errorResponse.Details);
        _mockAuthService.Verify(s => s.RegisterUserAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task SignupAsync_WithEmptyPassword_ReturnsBadRequestWithError()
    {
        // Arrange
        var request = new SignupRequestDto
        {
            Username = "newuser",
            Password = "",
            PasswordConfirmation = ""
        };

        // Act
        var result = await _controller.SignupAsync(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(badRequest.Value);
        Assert.Equal("ORG-VAL-001", errorResponse.Code);
    }

    [Fact]
    public async Task SignupAsync_WithDuplicateUsername_ReturnsBadRequestWithError()
    {
        // Arrange
        var request = new SignupRequestDto
        {
            Username = "existinguser",
            Password = "validpassword123",
            PasswordConfirmation = "validpassword123"
        };
        
        _mockAuthService.Setup(s => s.RegisterUserAsync(
                request.Username,
                request.Password,
                request.PasswordConfirmation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "Username already exists"));

        // Act
        var result = await _controller.SignupAsync(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(badRequest.Value);
        Assert.Equal("ORG-VAL-001", errorResponse.Code);
        Assert.Contains("Username already exists", errorResponse.Message);
    }

    [Fact]
    public async Task SignupAsync_WithInvalidPasswordLength_ReturnsBadRequestWithError()
    {
        // Arrange
        var request = new SignupRequestDto
        {
            Username = "newuser",
            Password = "short",
            PasswordConfirmation = "short"
        };
        
        _mockAuthService.Setup(s => s.RegisterUserAsync(
                request.Username,
                request.Password,
                request.PasswordConfirmation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "Password must be at least 8 characters"));

        // Act
        var result = await _controller.SignupAsync(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(badRequest.Value);
        Assert.Contains("at least 8 characters", errorResponse.Message);
    }

    [Fact]
    public async Task SignupAsync_WithValidRequest_CallsServiceWithCancellationToken()
    {
        // Arrange
        var request = new SignupRequestDto
        {
            Username = "newuser",
            Password = "validpassword123",
            PasswordConfirmation = "validpassword123"
        };
        var cancellationToken = new CancellationToken();
        
        var authResponse = new AuthResponseDto
        {
            Token = "jwt.token.here",
            ExpiresIn = 86400,
            User = new UserDto { UserId = 1, Username = "newuser", CreatedDate = DateTime.UtcNow }
        };
        
        _mockAuthService.Setup(s => s.RegisterUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cancellationToken))
            .ReturnsAsync((true, authResponse, null));

        // Act
        await _controller.SignupAsync(request, cancellationToken);

        // Assert
        _mockAuthService.Verify(s => s.RegisterUserAsync(
            request.Username, request.Password, request.PasswordConfirmation, cancellationToken), 
            Times.Once);
    }

    #endregion

    #region SigninAsync Tests

    [Fact]
    public async Task SigninAsync_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new SigninRequestDto
        {
            Username = "testuser",
            Password = "validpassword123"
        };
        
        var authResponse = new AuthResponseDto
        {
            Token = "jwt.token.here",
            ExpiresIn = 86400,
            User = new UserDto { UserId = 1, Username = "testuser", CreatedDate = DateTime.UtcNow }
        };
        
        _mockAuthService.Setup(s => s.AuthenticateUserAsync(
                request.Username,
                request.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, authResponse, null));

        // Act
        var result = await _controller.SigninAsync(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        
        var envelope = Assert.IsType<ItemResponseDto<AuthResponseDto>>(okResult.Value);
        Assert.NotNull(envelope.Item);
        Assert.Equal("jwt.token.here", envelope.Item.Token);
        Assert.Equal(1, envelope.Item.User.UserId);
    }

    [Fact]
    public async Task SigninAsync_WithEmptyUsername_ReturnsBadRequestWithError()
    {
        // Arrange
        var request = new SigninRequestDto
        {
            Username = "",
            Password = "validpassword123"
        };

        // Act
        var result = await _controller.SigninAsync(request, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(badRequest.Value);
        Assert.Equal("ORG-VAL-001", errorResponse.Code);
    }

    [Fact]
    public async Task SigninAsync_WithInvalidCredentials_ReturnsUnauthorizedWithError()
    {
        // Arrange
        var request = new SigninRequestDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        };
        
        _mockAuthService.Setup(s => s.AuthenticateUserAsync(
                request.Username,
                request.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "The provided credentials are invalid"));

        // Act
        var result = await _controller.SigninAsync(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponseDto>(unauthorizedResult.Value);
        Assert.Equal("ORG-AUT-001", errorResponse.Code);
    }

    [Fact]
    public async Task SigninAsync_WithUnknownUser_ReturnsUnauthorizedWithError()
    {
        // Arrange
        var request = new SigninRequestDto
        {
            Username = "nonexistentuser",
            Password = "anypassword"
        };
        
        _mockAuthService.Setup(s => s.AuthenticateUserAsync(
                request.Username,
                request.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "The provided credentials are invalid"));

        // Act
        var result = await _controller.SigninAsync(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task SigninAsync_WithValidRequest_CallsServiceWithCancellationToken()
    {
        // Arrange
        var request = new SigninRequestDto
        {
            Username = "testuser",
            Password = "validpassword123"
        };
        var cancellationToken = new CancellationToken();
        
        var authResponse = new AuthResponseDto
        {
            Token = "jwt.token.here",
            ExpiresIn = 86400,
            User = new UserDto { UserId = 1, Username = "testuser", CreatedDate = DateTime.UtcNow }
        };
        
        _mockAuthService.Setup(s => s.AuthenticateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), cancellationToken))
            .ReturnsAsync((true, authResponse, null));

        // Act
        await _controller.SigninAsync(request, cancellationToken);

        // Assert
        _mockAuthService.Verify(s => s.AuthenticateUserAsync(
            request.Username, request.Password, cancellationToken), 
            Times.Once);
    }

    #endregion

    #region GetCurrentUserAsync Tests

    [Fact]
    public async Task GetCurrentUserAsync_WithValidAuthenticationClaim_ReturnsUserDto()
    {
        // Arrange
        var userId = 1;
        var username = "testuser";
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ItemResponseDto<UserDto>>(okResult.Value);
        Assert.NotNull(envelope.Item);
        Assert.Equal(userId, envelope.Item.UserId);
        Assert.Equal(username, envelope.Item.Username);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithoutAuthenticationClaim_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(unauthorizedResult.Value);
        Assert.Equal("ORG-AUT-001", errorResponse.Code);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithInvalidUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "not-a-number"),
            new(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(unauthorizedResult.Value);
        Assert.Equal("ORG-AUT-001", errorResponse.Code);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithValidAuthenticationClaim_ReturnsEnvelopeWithMetadata()
    {
        // Arrange
        var userId = 1;
        var username = "testuser";
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ItemResponseDto<UserDto>>(okResult.Value);
        Assert.NotNull(envelope.Metadata);
        Assert.NotNull(envelope.Metadata.Timestamp);
        Assert.NotNull(envelope.Metadata.TransactionId);
        Assert.NotNull(envelope.Links);
        Assert.Contains("/v1/auth/me", envelope.Links.Self);
    }

    #endregion
}
