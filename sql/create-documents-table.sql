-- Ejecutar una sola vez contra la base de SociosDevConnection (dev y producción).
-- Crea la tabla que trackea los PDFs/.txt subidos al asistente, para poder
-- listarlos y borrarlos desde /api/documents.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Documents')
BEGIN
    CREATE TABLE Documents (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FileName NVARCHAR(500) NOT NULL,
        FileType NVARCHAR(100) NOT NULL,
        ClubId INT NULL,
        ChunkCount INT NOT NULL DEFAULT 0,
        UploadDate DATETIME2 NOT NULL,
        ProcessedDate DATETIME2 NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending'
    );
END
