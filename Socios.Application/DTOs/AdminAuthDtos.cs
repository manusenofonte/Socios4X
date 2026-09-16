using System.ComponentModel.DataAnnotations;
using Socios.Domain.Entities;

namespace Socios.Application.DTOs;

public class LoginRequest
{
    [Required, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required, StringLength(256)]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required, StringLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public class CreateAdminUserRequest
{
    [Required, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
}

public class SetAdminUserActiveRequest
{
    public bool IsActive { get; set; }
}

public class AdminUserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public static AdminUserResponse From(AdminUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        Role = user.Role,
        IsActive = user.IsActive,
        MustChangePassword = user.MustChangePassword,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}

public class AdminSessionResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AdminUserResponse User { get; set; } = new();
}

public class TemporaryPasswordResponse
{
    public AdminUserResponse User { get; set; } = new();

    // Se muestra una sola vez: no se puede volver a consultar
    public string TemporaryPassword { get; set; } = string.Empty;
}
