using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Core.DTOs;

namespace MSMEDigitize.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        if (!result.IsSuccess) return Unauthorized(new { message = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterBusinessDto dto, CancellationToken ct)
    {
        var result = await _authService.RegisterBusinessAsync(dto, ct);
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken, ct);
        if (!result.IsSuccess) return Unauthorized(new { message = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await _authService.SendPasswordResetEmailAsync(dto.Email, ct);
        return Ok(new { message = "If that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword, ct);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });
        return Ok(new { message = "Password reset successful." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _authService.ChangePasswordAsync(userId, dto, ct);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });
        return Ok(new { message = "Password changed." });
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        await _authService.RevokeTokenAsync(userId, ct);
        return Ok();
    }
}
