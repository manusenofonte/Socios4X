-- Ejecutar una sola vez contra la base de SociosDevConnection (dev y producción).
-- Crea la tabla de usuarios del panel de administración del asistente.
-- El primer superadmin se crea con: dotnet Socios.Api.dll create-superadmin <email> [nombre]

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminUsers')
BEGIN
    CREATE TABLE AdminUsers (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Email NVARCHAR(256) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(500) NULL,
        MustChangePassword BIT NOT NULL DEFAULT 0,
        TemporaryPasswordExpiresAt DATETIME2 NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        FailedLoginAttempts INT NOT NULL DEFAULT 0,
        LockoutEndsAt DATETIME2 NULL,
        SessionVersion INT NOT NULL DEFAULT 0,
        PasswordResetTokenHash NVARCHAR(100) NULL,
        PasswordResetTokenExpiresAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL,
        LastLoginAt DATETIME2 NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AdminUsers_Email' AND object_id = OBJECT_ID('AdminUsers'))
    CREATE UNIQUE INDEX UX_AdminUsers_Email ON AdminUsers (Email);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AdminUsers_PasswordResetTokenHash' AND object_id = OBJECT_ID('AdminUsers'))
    CREATE INDEX IX_AdminUsers_PasswordResetTokenHash ON AdminUsers (PasswordResetTokenHash);
