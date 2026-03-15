using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniErp.Application;
using MiniErp.Infrastructure;

namespace MiniErp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Dev helper: returns PBKDF2 hash for the given password. Use to update Users.PasswordHash in seed data.
    /// Example: POST /api/auth/hash with body "admin@123"
    /// </summary>
    [HttpPost("hash")]
    [AllowAnonymous]
    public ActionResult<string> Hash([FromBody] string password)
    {
        if (string.IsNullOrEmpty(password))
            return BadRequest("Password required.");
        return Ok(AuthService.HashPassword(password));
    }

    /// <summary>
    /// Dev helper: verifies if a password matches a stored hash. Hash cannot be reversed to password.
    /// Example: POST /api/auth/verify with body { "password": "admin@123", "hash": "dtjIcSLQIqM6oT99wENDHJHl6Cf/0qUD5MiiDg5QpzpRR3E0uYTRRXKDz7BUOn2k" }
    /// Returns: { "isValid": true } if password matches hash.
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous]
    public ActionResult<object> Verify([FromBody] VerifyPasswordRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Hash))
            return BadRequest("Password and hash required.");
        var isValid = AuthService.VerifyPasswordHash(request.Password, request.Hash);
        return Ok(new { isValid });
    }

    /// <summary>
    /// Returns current user from JWT. Use to verify your token is accepted (200 = token OK).
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        var name = User.Identity?.Name ?? "?";
        var roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        return Ok(new { userName = name, roles });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
