-- Script idempotente: se puede ejecutar muchas veces sin romper nada.
SET QUOTED_IDENTIFIER ON;   -- necesario para índices filtrados
GO
IF DB_ID('GymTracker') IS NULL CREATE DATABASE GymTracker;
GO
USE GymTracker;
GO

-- ===== Rutinas =====
IF OBJECT_ID('dbo.Routine') IS NULL
CREATE TABLE dbo.Routine(
    Id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Routine PRIMARY KEY,
    UserId    UNIQUEIDENTIFIER NOT NULL,
    Name      NVARCHAR(100)    NOT NULL,
    CreatedAt DATETIME2        NOT NULL CONSTRAINT DF_Routine_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO
IF OBJECT_ID('dbo.RoutineExercise') IS NULL
CREATE TABLE dbo.RoutineExercise(
    Id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RoutineExercise PRIMARY KEY,
    RoutineId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_RoutineExercise_Routine REFERENCES dbo.Routine(Id),
    Name      NVARCHAR(100)    NOT NULL,
    Sets      INT              NOT NULL CONSTRAINT CK_RoutineExercise_Sets CHECK (Sets > 0),
    Reps      INT              NOT NULL CONSTRAINT CK_RoutineExercise_Reps CHECK (Reps > 0)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Routine_User')
    CREATE INDEX IX_Routine_User ON dbo.Routine(UserId) INCLUDE (Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RoutineExercise_Routine')
    CREATE INDEX IX_RoutineExercise_Routine ON dbo.RoutineExercise(RoutineId);
GO

-- ===== Sesiones y series =====
IF OBJECT_ID('dbo.WorkoutSession') IS NULL
CREATE TABLE dbo.WorkoutSession(
    Id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WorkoutSession PRIMARY KEY,
    UserId    UNIQUEIDENTIFIER NOT NULL,
    RoutineId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_WorkoutSession_Routine REFERENCES dbo.Routine(Id),
    [Date]    DATETIME2        NOT NULL
);
GO
IF OBJECT_ID('dbo.SetLog') IS NULL
CREATE TABLE dbo.SetLog(
    Id          BIGINT IDENTITY  NOT NULL CONSTRAINT PK_SetLog PRIMARY KEY,
    SessionId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_SetLog_Session  REFERENCES dbo.WorkoutSession(Id),
    ExerciseId  UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_SetLog_Exercise REFERENCES dbo.RoutineExercise(Id),
    SetNumber   INT           NOT NULL,
    Reps        INT           NOT NULL CONSTRAINT CK_SetLog_Reps   CHECK (Reps BETWEEN 1 AND 1000),
    WeightKg    DECIMAL(7,3)  NOT NULL CONSTRAINT CK_SetLog_Weight CHECK (WeightKg BETWEEN 0 AND 1000),
    -- Red de seguridad contra concurrencia: dos peticiones simultáneas no pueden crear la misma serie.
    CONSTRAINT UQ_SetLog_Number UNIQUE (SessionId, ExerciseId, SetNumber)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkoutSession_User')
    CREATE INDEX IX_WorkoutSession_User ON dbo.WorkoutSession(UserId, [Date] DESC);
-- Índice "cubriente" para la consulta de progreso: SQL Server responde sin tocar la tabla.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SetLog_Exercise')
    CREATE INDEX IX_SetLog_Exercise ON dbo.SetLog(ExerciseId) INCLUDE (SessionId, Reps, WeightKg);
GO

-- ===== Medidas corporales =====
IF OBJECT_ID('dbo.BodyMeasurement') IS NULL
CREATE TABLE dbo.BodyMeasurement(
    Id       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BodyMeasurement PRIMARY KEY,
    UserId   UNIQUEIDENTIFIER NOT NULL,
    [Date]   DATETIME2        NOT NULL,
    WeightKg DECIMAL(5,2)     NOT NULL CONSTRAINT CK_BodyMeasurement_Weight CHECK (WeightKg > 0),
    WaistCm  DECIMAL(5,2)     NULL,
    ChestCm  DECIMAL(5,2)     NULL,
    ArmCm    DECIMAL(5,2)     NULL,
    PhotoKey NVARCHAR(100)    NULL     -- clave del archivo, no una ruta: el almacenamiento puede cambiar
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BodyMeasurement_User')
    CREATE INDEX IX_BodyMeasurement_User ON dbo.BodyMeasurement(UserId, [Date] DESC);
GO

-- ===== Mensajería: Outbox + idempotencia =====
IF OBJECT_ID('dbo.Outbox') IS NULL
CREATE TABLE dbo.Outbox(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Outbox PRIMARY KEY,
    [Type]      NVARCHAR(100)    NOT NULL,
    Payload     NVARCHAR(MAX)    NOT NULL,
    CreatedAt   DATETIME2        NOT NULL CONSTRAINT DF_Outbox_CreatedAt DEFAULT SYSUTCDATETIME(),
    PublishedAt DATETIME2        NULL
);
GO
-- Índice filtrado: solo contiene lo pendiente, así se mantiene diminuto aunque la tabla crezca.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Outbox_Pending')
    CREATE INDEX IX_Outbox_Pending ON dbo.Outbox(CreatedAt) WHERE PublishedAt IS NULL;
GO
IF OBJECT_ID('dbo.ProcessedMessage') IS NULL
CREATE TABLE dbo.ProcessedMessage(
    MessageId   UNIQUEIDENTIFIER NOT NULL,
    Consumer    NVARCHAR(100)    NOT NULL,   -- cada consumidor lleva su propio registro
    ProcessedAt DATETIME2        NOT NULL CONSTRAINT DF_ProcessedMessage_At DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ProcessedMessage PRIMARY KEY (MessageId, Consumer)
);
GO