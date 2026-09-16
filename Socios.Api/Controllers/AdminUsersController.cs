using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Socios.Api.Auth;
using Socios.Application.DTOs;
using Socios.Application.Services;

namespace Socios.Api.Controllers;

// Gestión de administradores del panel: solo el administrador principal
[ApiController]
[Route("api/admin-users")]
[Authorize(Policy = AdminPolicies.SuperAdmin)]
[EnableRateLimiting("AdminPolicy")]
public class AdminUsersController : ControllerBase
{
    private readonly AdminUserManagementService _management;

    public AdminUsersController(AdminUserManagementService management)
    {
        _management = management;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _management.GetAllAsync(cancellationToken);
        return Ok(users.Select(AdminUserResponse.From));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _management.CreateAdminAsync(request.Email, request.Name, cancellationToken);
        return ToTemporaryPasswordResult(result);
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken cancellationToken)
    {
        var result = await _management.ResetPasswordAsync(User.GetAdminUserId(), id, cancellationToken);
        return ToTemporaryPasswordResult(result);
    }

    [HttpPut("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetAdminUserActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await _management.SetActiveAsync(User.GetAdminUserId(), id, request.IsActive, cancellationToken);

        if (result.IsNotFound)
            return NotFound(new { Error = result.Error });

        if (!result.Succeeded)
            return BadRequest(new { Error = result.Error });

        return Ok(AdminUserResponse.From(result.User!));
    }

    private IActionResult ToTemporaryPasswordResult(AdminUserResult result)
    {
        if (result.IsNotFound)
            return NotFound(new { Error = result.Error });

        if (!result.Succeeded)
            return BadRequest(new { Error = result.Error });

        return Ok(new TemporaryPasswordResponse
        {
            User = AdminUserResponse.From(result.User!),
            TemporaryPassword = result.TemporaryPassword!
        });
    }
}
