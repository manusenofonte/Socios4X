using Socios.Application.Services;

namespace Socios.Api.Auth;

// Comandos de consola, se ejecutan en lugar de levantar la API:
//   dotnet Socios.Api.dll create-superadmin <email> [nombre]
// Crea el administrador principal o, si ya existe, le genera una contraseña temporal nueva
// (sirve si se olvida la suya). La contraseña temporal se muestra solo en esta consola.
public static class AdminCommands
{
    public const string CreateSuperAdmin = "create-superadmin";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine($"Uso: dotnet Socios.Api.dll {CreateSuperAdmin} <email> [nombre]");
            return 1;
        }

        var email = args[1];
        var name = args.Length > 2 ? string.Join(' ', args.Skip(2)) : string.Empty;

        using var scope = services.CreateScope();
        var management = scope.ServiceProvider.GetRequiredService<AdminUserManagementService>();
        var result = await management.CreateOrResetSuperAdminAsync(email, name);

        if (!result.Succeeded)
        {
            Console.Error.WriteLine($"Error: {result.Error}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"Administrador principal: {result.User!.Email}");
        Console.WriteLine($"Contraseña temporal:     {result.TemporaryPassword}");
        Console.WriteLine();
        Console.WriteLine("Vence en 48 horas. Al ingresar a /admin se le va a pedir que elija su propia contraseña.");
        Console.WriteLine("Pasásela por un canal privado; esta contraseña no se puede volver a consultar.");
        return 0;
    }
}
