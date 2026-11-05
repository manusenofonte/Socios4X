-- Ejecutar antes de desplegar esta versión, en la base de SociosDevConnection.
-- No reconstruye consultas anteriores: el registro comienza al activar la versión.
IF OBJECT_ID(N'dbo.QueryUsages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QueryUsages (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        StartedAt DATETIME2 NOT NULL,
        ClubId INT NULL,
        Completed BIT NOT NULL,
        InputTokens BIGINT NOT NULL,
        CachedInputTokens BIGINT NOT NULL,
        OutputTokens BIGINT NOT NULL,
        EmbeddingTokens BIGINT NOT NULL,
        KnownCostUsd DECIMAL(20, 10) NOT NULL,
        IsCostComplete BIT NOT NULL
    );
    CREATE INDEX IX_QueryUsages_StartedAt ON dbo.QueryUsages (StartedAt)
        INCLUDE (Completed, KnownCostUsd, IsCostComplete);
END;
