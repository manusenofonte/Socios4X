using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Socios.Application.Interfaces;
using Socios.Application.Services;
using Socios.Domain.Entities;

namespace Socios.Application.Tests;

public class AdminAuthTests
{
    private readonly FakeUsers _users = new();
    private readonly FakeEmail _email = new();
    private readonly FakeTime _time = new(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));
    private readonly AdminAuthService _auth;
    private readonly AdminUserManagementService _management;

    public AdminAuthTests()
    {
        var options = Options.Create(new AdminAuthOptions { FrontendBaseUrl = "https://panel.test" });
        var hasher = new FakeHasher();
        _auth = new AdminAuthService(_users, hasher, new FakeTokens(), _email, options, _time, NullLogger<AdminAuthService>.Instance);
        _management = new AdminUserManagementService(_users, hasher, options, _time);
    }

    [Theory]
    [InlineData("corta1", false)]
    [InlineData("sinnumerosaca", false)]
    [InlineData("1234567890", false)]
    [InlineData("clave-segura-2026", true)]
    public void PoliticaDeContrasenias(string password, bool valida)
    {
        Assert.Equal(valida, AdminPasswords.Validate(password) is null);
    }

    [Fact]
    public void ContraseniaTemporal_CumpleLaPolitica()
    {
        for (var i = 0; i < 50; i++)
            Assert.Null(AdminPasswords.Validate(AdminPasswords.GenerateTemporary()));
    }

    [Fact]
    public async Task SuperAdmin_EntraConTemporalYQuedaObligadoACambiarla()
    {
        var created = await _management.CreateOrResetSuperAdminAsync("Miguel@4x.com.ar", "Miguel");

        var login = await _auth.LoginAsync("miguel@4x.com.ar", created.TemporaryPassword!);

        Assert.True(login.Succeeded);
        Assert.True(login.User!.MustChangePassword);
        Assert.Equal(AdminUser.RoleSuperAdmin, login.User.Role);
    }

    [Fact]
    public async Task CambiarContrasenia_QuitaLaObligacionEInvalidaLasSesionesAnteriores()
    {
        var created = await _management.CreateOrResetSuperAdminAsync("miguel@4x.com.ar", "Miguel");
        var user = created.User!;
        var versionAntes = user.SessionVersion;

        var result = await _auth.ChangePasswordAsync(user.Id, created.TemporaryPassword!, "mi-clave-nueva-1");

        Assert.True(result.Succeeded);
        Assert.False(user.MustChangePassword);
        Assert.True(user.SessionVersion > versionAntes);
        Assert.True((await _auth.LoginAsync("miguel@4x.com.ar", "mi-clave-nueva-1")).Succeeded);
        Assert.False((await _auth.LoginAsync("miguel@4x.com.ar", created.TemporaryPassword!)).Succeeded);
    }

    [Fact]
    public async Task EmailInexistenteYContraseniaIncorrecta_DevuelvenElMismoMensaje()
    {
        await CreateActiveAdminAsync("ana@club.com", "clave-de-ana-1");

        var noExiste = await _auth.LoginAsync("nadie@club.com", "lo-que-sea-1");
        var incorrecta = await _auth.LoginAsync("ana@club.com", "incorrecta-99");

        Assert.Equal(AdminAuthService.InvalidCredentialsMessage, noExiste.Error);
        Assert.Equal(noExiste.Error, incorrecta.Error);
    }

    [Fact]
    public async Task CincoIntentosFallidos_BloqueanLaCuenta15Minutos()
    {
        await CreateActiveAdminAsync("ana@club.com", "clave-de-ana-1");

        for (var i = 0; i < 5; i++)
            await _auth.LoginAsync("ana@club.com", "incorrecta-99");

        Assert.False((await _auth.LoginAsync("ana@club.com", "clave-de-ana-1")).Succeeded);

        _time.Advance(TimeSpan.FromMinutes(16));
        Assert.True((await _auth.LoginAsync("ana@club.com", "clave-de-ana-1")).Succeeded);
    }

    [Fact]
    public async Task ContraseniaTemporalVencida_NoPermiteEntrar()
    {
        var created = await _management.CreateAdminAsync("ana@club.com", "Ana");

        _time.Advance(TimeSpan.FromHours(49));
        var login = await _auth.LoginAsync("ana@club.com", created.TemporaryPassword!);

        Assert.False(login.Succeeded);
        Assert.Contains("venció", login.Error);
    }

    [Fact]
    public async Task UsuarioDesactivado_NoPuedeEntrar()
    {
        var superAdmin = (await _management.CreateOrResetSuperAdminAsync("miguel@4x.com.ar", "Miguel")).User!;
        var ana = await CreateActiveAdminAsync("ana@club.com", "clave-de-ana-1");

        var result = await _management.SetActiveAsync(superAdmin.Id, ana.Id, active: false);

        Assert.True(result.Succeeded);
        Assert.False((await _auth.LoginAsync("ana@club.com", "clave-de-ana-1")).Succeeded);
    }

    [Fact]
    public async Task NoSePuedeDesactivarAlSuperAdminNiASiMismo()
    {
        var superAdmin = (await _management.CreateOrResetSuperAdminAsync("miguel@4x.com.ar", "Miguel")).User!;

        Assert.False((await _management.SetActiveAsync(superAdmin.Id, superAdmin.Id, active: false)).Succeeded);
    }

    [Fact]
    public async Task OlvideMiContrasenia_SinCorreoConfigurado_NoHaceNada()
    {
        var ana = await CreateActiveAdminAsync("ana@club.com", "clave-de-ana-1");
        _email.IsConfigured = false;

        await _auth.RequestPasswordResetAsync("ana@club.com");

        Assert.Empty(_email.Sent);
        Assert.Null(ana.PasswordResetTokenHash);
    }

    [Fact]
    public async Task OlvideMiContrasenia_LinkSirveUnaSolaVezYVence()
    {
        await CreateActiveAdminAsync("ana@club.com", "clave-de-ana-1");

        await _auth.RequestPasswordResetAsync("ANA@club.com");
        var mail = Assert.Single(_email.Sent);
        var token = ExtractToken(mail.Body);

        var reset = await _auth.ResetPasswordAsync(token, "clave-nueva-de-ana-2");
        Assert.True(reset.Succeeded);
        Assert.True((await _auth.LoginAsync("ana@club.com", "clave-nueva-de-ana-2")).Succeeded);

        // Segundo uso del mismo link
        Assert.False((await _auth.ResetPasswordAsync(token, "otra-clave-3")).Succeeded);

        // Un link nuevo, usado después de vencer
        await _auth.RequestPasswordResetAsync("ana@club.com");
        var expiredToken = ExtractToken(_email.Sent[^1].Body);
        _time.Advance(TimeSpan.FromMinutes(31));
        Assert.False((await _auth.ResetPasswordAsync(expiredToken, "otra-clave-3")).Succeeded);
    }

    [Fact]
    public async Task OlvideMiContrasenia_EmailInexistente_NoEnviaNada()
    {
        await _auth.RequestPasswordResetAsync("nadie@club.com");

        Assert.Empty(_email.Sent);
    }

    private async Task<AdminUser> CreateActiveAdminAsync(string email, string password)
    {
        var created = await _management.CreateAdminAsync(email, email);
        var result = await _auth.ChangePasswordAsync(created.User!.Id, created.TemporaryPassword!, password);
        Assert.True(result.Succeeded, result.Error);
        return created.User;
    }

    private static string ExtractToken(string body)
    {
        var start = body.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var end = body.IndexOf('"', start);
        return Uri.UnescapeDataString(body[start..end]);
    }

    private sealed class FakeUsers : IAdminUserRepository
    {
        private readonly List<AdminUser> _users = [];

        public Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

        public Task<AdminUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<AdminUser?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.PasswordResetTokenHash == tokenHash));

        public Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdminUser>>(_users);

        public Task AddAsync(AdminUser user, CancellationToken cancellationToken = default)
        {
            // Simula el Id que asigna la base
            typeof(AdminUser).GetProperty(nameof(AdminUser.Id))!.SetValue(user, _users.Count + 1);
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash:" + password;

        public bool Verify(string passwordHash, string password) => passwordHash == "hash:" + password;
    }

    private sealed class FakeTokens : IAdminTokenService
    {
        public AdminSession CreateSession(AdminUser user) => new($"token-{user.Id}-{user.SessionVersion}", DateTime.UtcNow.AddHours(8));
    }

    private sealed class FakeEmail : IEmailSender
    {
        public bool IsConfigured { get; set; } = true;

        public List<(string To, string Subject, string Body)> Sent { get; } = [];

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTime(DateTime start) : TimeProvider
    {
        private DateTimeOffset _now = new(start);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
