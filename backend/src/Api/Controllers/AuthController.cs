using Api.DTOs.Auth;
using Api.DTOs.Common;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

/// <summary>
/// Controller for authentication endpoints (signup, signin, logout, get current user).
/// </summary>
[Route("v1/[controller]")]
[ApiController]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthController class.
    /// </summary>
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account (sign up).
    /// </summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(ItemResponseDto<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ItemResponseDto<AuthResponseDto>>> SignupAsync(
        [FromBody] SignupRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            var errorResponse = new ErrorResponseDto
            {
                Code = "ORG-VAL-001",
                Message = "Validation failed",
                Details = new List<ErrorDetailDto>
                {
                    new() { Field = "username", Message = "Username is required" },
                    new() { Field = "password", Message = "Password is required" }
                }
            };
            return BadRequest(errorResponse);
        }

        var result = await _authService.RegisterUserAsync(
            request.Username,
            request.Password,
            request.PasswordConfirmation,
            cancellationToken);

        if (!result.Success || result.Response is null)
        {
            _logger.LogWarning("Signup failed: {Error}", result.Error);
            var errorResponse = new ErrorResponseDto
            {
                Code = "ORG-VAL-001",
                Message = result.Error ?? "Registration failed"
            };
            return BadRequest(errorResponse);
        }

        var response = new ItemResponseDto<AuthResponseDto>
        {
            Item = result.Response,
            Metadata = new MetadataDto
            {
                Timestamp = DateTime.UtcNow,
                TransactionId = GetTraceIdentifier()
            },
            Links = new LinksDto
            {
                Self = "/v1/auth/signin"
            }
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token (sign in).
    /// </summary>
    [HttpPost("signin")]
    [ProducesResponseType(typeof(ItemResponseDto<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ItemResponseDto<AuthResponseDto>>> SigninAsync(
        [FromBody] SigninRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            var errorResponse = new ErrorResponseDto
            {
                Code = "ORG-VAL-001",
                Message = "Validation failed",
                Details = new List<ErrorDetailDto>
                {
                    new() { Field = "username", Message = "Username is required" },
                    new() { Field = "password", Message = "Password is required" }
                }
            };
            return BadRequest(errorResponse);
        }

        var result = await _authService.AuthenticateUserAsync(
            request.Username,
            request.Password,
            cancellationToken);

        if (!result.Success || result.Response is null)
        {
            _logger.LogWarning("Signin failed for user: {Username}", request.Username);
            var errorResponse = new ErrorResponseDto
            {
                Code = "ORG-AUT-001",
                Message = result.Error ?? "Authentication failed"
            };
            return Unauthorized(errorResponse);
        }

        var response = new ItemResponseDto<AuthResponseDto>
        {
            Item = result.Response,
            Metadata = new MetadataDto
            {
                Timestamp = DateTime.UtcNow,
                TransactionId = GetTraceIdentifier()
            },
            Links = new LinksDto
            {
                Self = "/v1/auth/me"
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets the currently authenticated user information.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ItemResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ItemResponseDto<UserDto>>> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            var errorResponse = new ErrorResponseDto
            {
                Code = "ORG-AUT-001",
                Message = "Authorization required"
            };
            return Unauthorized(errorResponse);
        }

        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var userDto = new UserDto
        {
            UserId = userId,
            Username = username,
            CreatedDate = DateTime.UtcNow
        };

        var response = new ItemResponseDto<UserDto>
        {
            Item = userDto,
            Metadata = new MetadataDto
            {
                Timestamp = DateTime.UtcNow,
                TransactionId = GetTraceIdentifier()
            },
            Links = new LinksDto
            {
                Self = "/v1/auth/me"
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Logs out the current user (clears server-side state if any).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            var errorResponse = new ErrorResponseDto
            {
                Code = "ORG-AUT-001",
                Message = "Authorization required"
            };
            return Unauthorized(errorResponse);
        }

        await _authService.LogoutAsync(userId, cancellationToken);

        _logger.LogInformation("User {UserId} logged out", userId);

        return Ok();
    }

    private string GetTraceIdentifier()
    {
        return HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
    }
}
