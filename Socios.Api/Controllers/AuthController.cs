using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Socios.Api.Auth;
using Socios.Application.DTOs;
using Socios.Application.Interfaces;
using Socios.Application.Services;

namespace Socios.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("AuthPolicy")]
public class AuthController : ControllerBase
{
    private readonly AdminAuthService _authService;
    private readonly IAdminUserRepository _users;

    public AuthController(AdminAuthService authService, IAdminUserRepository users)
    {
        _authService = authService;
        _users = users;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);

        if (!result.Succeeded)
            return Unauthorized(new { Error = result.Error });

        return Ok(ToSessionResponse(result));
    }

    // Disponible también con contraseña temporal: es justamente lo que tiene que hacer
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ChangePasswordAsync(User.GetAdminUserId(), request.CurrentPassword, request.NewPassword, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { Error = result.Error });

        return Ok(ToSessionResponse(result));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetAsync(request.Email, cancellationToken);

        // Misma respuesta siempre, exista o no el email
        return Ok(new
        {
            Message = "Si el email corresponde a un administrador, te enviamos un link para elegir una contraseña nueva. " +
                      "Si no te llega en unos minutos, pedile al administrador principal que blanquee tu contraseña."
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { Error = result.Error });

        return Ok(new { Message = "Listo, ya podés ingresar con tu contraseña nueva." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(User.GetAdminUserId(), cancellationToken);
        return user is null ? Unauthorized() : Ok(AdminUserResponse.From(user));
    }

    private static AdminSessionResponse ToSessionResponse(AdminAuthResult result) => new()
    {
        Token = result.Session!.Token,
        ExpiresAt = result.Session.ExpiresAt,
        User = AdminUserResponse.From(result.User!)
    };
}
