using System.Security.Cryptography;
using System.Text;

namespace Socios.Application.Services;

public static class AdminPasswords
{
    public const int MinLength = 10;
    public const int MaxLength = 128;

    // null si la contraseña cumple los requisitos; si no, el motivo para mostrar al usuario
    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return $"La contraseña debe tener al menos {MinLength} caracteres.";

        if (password.Length > MaxLength)
            return $"La contraseña no puede tener más de {MaxLength} caracteres.";

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "La contraseña debe tener al menos una letra y un número.";

        return null;
    }

    // Contraseña temporal aleatoria (sin caracteres que se confunden, como 0/O o 1/l)
    public static string GenerateTemporary(int length = 14)
    {
        const string letters = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string all = letters + digits;

        while (true)
        {
            var chars = new char[length];
            for (var i = 0; i < length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

            var password = new string(chars);
            if (Validate(password) is null)
                return password;
        }
    }

    // Token para el link de "Olvidé mi contraseña": 256 bits aleatorios, apto para URL
    public static string GenerateResetToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // En la base solo se guarda el hash del token: si alguien lee la tabla no puede usarlo
    public static string HashResetToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
