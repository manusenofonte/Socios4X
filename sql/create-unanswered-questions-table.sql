-- Ejecutar una sola vez contra la base de SociosDevConnection (dev y producción).
-- Crea la tabla donde el asistente registra las preguntas que no pudo responder,
-- para listarlas y borrarlas desde el panel de administración.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UnansweredQuestions')
BEGIN
    CREATE TABLE UnansweredQuestions (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Question NVARCHAR(1000) NOT NULL,
        ClubId INT NULL,
        CreatedAt DATETIME2 NOT NULL
    );

    -- El listado siempre se ordena de la más reciente a la más vieja
    CREATE INDEX IX_UnansweredQuestions_CreatedAt ON UnansweredQuestions (CreatedAt DESC);
END
