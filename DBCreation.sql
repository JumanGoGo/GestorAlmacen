-- =============================================
-- SCRIPT DE BASE DE DATOS: GESTOR ALMACEN (WMS)
-- Autor: Generado para Proyecto Universitario
-- Fecha: 04/12/2025
-- =============================================

USE master;
GO

-- 1. Crear la Base de Datos
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'WMS_DB')
BEGIN
    CREATE DATABASE WMS_DB;
END
GO

USE WMS_DB;
GO

-- =============================================
-- 2. TABLAS MAESTRAS (CATÁLOGOS)
-- =============================================

-- Categorías de Productos
IF OBJECT_ID('dbo.Categories', 'U') IS NULL
CREATE TABLE dbo.Categories (
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(250) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- Áreas / Pasillos del Almacén
IF OBJECT_ID('dbo.Areas', 'U') IS NULL
CREATE TABLE dbo.Areas (
    area_id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(10) NOT NULL UNIQUE, -- Ej: A1, Z9
    name NVARCHAR(150) NOT NULL,
    preferred_category_id INT NULL,
    capacity INT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Areas_Categories FOREIGN KEY (preferred_category_id) 
        REFERENCES dbo.Categories(category_id)
);
GO

-- Usuarios y Roles
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE dbo.Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    password_hash NVARCHAR(256) NOT NULL, -- En prod usar SHA256
    display_name NVARCHAR(150) NOT NULL,
    role NVARCHAR(20) NOT NULL, -- ADMIN, SUPERVISOR, OPERADOR
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- Productos
IF OBJECT_ID('dbo.Products', 'U') IS NULL
CREATE TABLE dbo.Products (
    product_id INT IDENTITY(1,1) PRIMARY KEY,
    sku NVARCHAR(7) NOT NULL UNIQUE, -- Formato AAA-000
    name NVARCHAR(200) NOT NULL,
    category_id INT NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    reorder_threshold INT NULL, -- Punto de reorden
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (category_id) 
        REFERENCES dbo.Categories(category_id),
    -- Validación de formato SKU (3 Letras - 3 Números)
    CONSTRAINT CK_Products_SKU_Format CHECK (sku LIKE '[A-Z][A-Z][A-Z]-[0-9][0-9][0-9]')
);
GO

-- Stock (Inventario Físico)
IF OBJECT_ID('dbo.Stock', 'U') IS NULL
CREATE TABLE dbo.Stock (
    stock_id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT NOT NULL,
    area_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 0,
    last_update DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Stock_ProductArea UNIQUE (product_id, area_id), -- Un producto solo aparece una vez por área
    CONSTRAINT FK_Stock_Product FOREIGN KEY (product_id) REFERENCES dbo.Products(product_id),
    CONSTRAINT FK_Stock_Area FOREIGN KEY (area_id) REFERENCES dbo.Areas(area_id)
);
GO

-- =============================================
-- 3. TABLAS TRANSACCIONALES (MOVIMIENTOS)
-- =============================================

-- Secuencias para Folios (ENTR-001, SAL-005)
IF OBJECT_ID('dbo.Sequences', 'U') IS NULL
CREATE TABLE dbo.Sequences (
    sequence_name NVARCHAR(50) PRIMARY KEY, -- ENTR, SAL, TRIN, TROUT
    current_value BIGINT NOT NULL DEFAULT 0
);
GO

-- Cabecera de Movimientos
IF OBJECT_ID('dbo.Movements', 'U') IS NULL
CREATE TABLE dbo.Movements (
    movement_id INT IDENTITY(1,1) PRIMARY KEY,
    folio NVARCHAR(30) NOT NULL UNIQUE,
    movement_type NVARCHAR(20) NOT NULL, -- ENTRADA, SALIDA, TRANS
    status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE', -- ACTIVE, CANCELLED
    movement_date DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    user_id INT NOT NULL, -- Quién lo hizo
    comment NVARCHAR(400) NULL,
    CONSTRAINT FK_Movements_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id)
);
GO

-- Detalle de Movimientos (Productos por movimiento)
IF OBJECT_ID('dbo.MovementDetails', 'U') IS NULL
CREATE TABLE dbo.MovementDetails (
    detail_id INT IDENTITY(1,1) PRIMARY KEY,
    movement_id INT NOT NULL,
    product_id INT NOT NULL,
    area_id INT NOT NULL, -- Origen (para salida) o Destino (para entrada)
    quantity INT NOT NULL,
    previous_stock INT NULL, -- Auditoría: Cuánto había antes
    CONSTRAINT FK_MovDet_Mov FOREIGN KEY (movement_id) REFERENCES dbo.Movements(movement_id) ON DELETE CASCADE,
    CONSTRAINT FK_MovDet_Prod FOREIGN KEY (product_id) REFERENCES dbo.Products(product_id),
    CONSTRAINT FK_MovDet_Area FOREIGN KEY (area_id) REFERENCES dbo.Areas(area_id)
);
GO

-- Auditoría General (Cambios sensibles)
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
CREATE TABLE dbo.AuditLogs (
    audit_id INT IDENTITY(1,1) PRIMARY KEY,
    table_name NVARCHAR(50) NOT NULL,
    record_id NVARCHAR(50) NOT NULL,
    action NVARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE
    user_id INT NULL,
    change_date DATETIME2 DEFAULT SYSUTCDATETIME(),
    old_value NVARCHAR(MAX) NULL,
    new_value NVARCHAR(MAX) NULL
);
GO

-- =============================================
-- 4. PROCEDIMIENTOS ALMACENADOS
-- =============================================

-- SP para generar siguiente folio de forma segura
IF OBJECT_ID('dbo.GetNextFolio', 'P') IS NOT NULL DROP PROCEDURE dbo.GetNextFolio;
GO

CREATE PROCEDURE dbo.GetNextFolio
    @seq_name NVARCHAR(50),
    @prefix NVARCHAR(10),
    @folio_generado NVARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
        -- Si no existe la secuencia, la crea
        IF NOT EXISTS (SELECT 1 FROM Sequences WHERE sequence_name = @seq_name)
        BEGIN
            INSERT INTO Sequences (sequence_name, current_value) VALUES (@seq_name, 0);
        END

        -- Incrementa y obtiene valor
        UPDATE Sequences
        SET current_value = current_value + 1
        WHERE sequence_name = @seq_name;

        DECLARE @val BIGINT;
        SELECT @val = current_value FROM Sequences WHERE sequence_name = @seq_name;

        -- Formato: PREFIJO-00000X
        SET @folio_generado = @prefix + '-' + RIGHT('000000' + CAST(@val AS NVARCHAR(20)), 6);
    COMMIT TRANSACTION;
END
GO

-- =============================================
-- 5. DATOS SEMILLA (INICIALES)
-- =============================================

-- Categorías Base
IF NOT EXISTS (SELECT 1 FROM Categories)
BEGIN
    INSERT INTO Categories (name, description) VALUES 
    ('Electronica', 'Dispositivos y gadgets'),
    ('Línea Blanca', 'Electrodomésticos'),
    ('Ropa', 'Indumentaria general'),
    ('Alimentos', 'No perecederos');
END

-- Áreas Base (A1 a A5)
IF NOT EXISTS (SELECT 1 FROM Areas)
BEGIN
    INSERT INTO Areas (code, name, capacity) VALUES 
    ('A1', 'Pasillo A - Sección 1', 100),
    ('A2', 'Pasillo A - Sección 2', 100),
    ('B1', 'Zona de Carga', 500),
    ('RECEPCION', 'Muelle de Entrada', NULL);
END

-- Usuario Admin por Defecto (Pass: 123)
IF NOT EXISTS (SELECT 1 FROM Users WHERE username = 'admin')
BEGIN
    INSERT INTO Users (username, password_hash, display_name, role) 
    VALUES ('admin', '123', 'Administrador del Sistema', 'ADMIN');
END

-- Secuencias Iniciales
IF NOT EXISTS (SELECT 1 FROM Sequences)
BEGIN
    INSERT INTO Sequences (sequence_name, current_value) VALUES 
    ('ENTR', 0), ('SAL', 0), ('TRANS', 0);
END
GO

PRINT 'Base de datos WMS_DB creada y configurada correctamente.';

