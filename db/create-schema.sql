IF DB_ID('MiniErp') IS NULL
BEGIN
    CREATE DATABASE MiniErp;
END;
GO

USE MiniErp;
GO

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users (Id),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles (Id)
);

CREATE TABLE RefreshTokens (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Token NVARCHAR(200) NOT NULL UNIQUE,
    JwtId NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    RevokedAt DATETIME2 NULL,
    ReplacedByToken NVARCHAR(200) NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users (Id)
);

CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Sku NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    ReorderLevel DECIMAL(18,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(256) NULL,
    Phone NVARCHAR(50) NULL,
    BillingAddress NVARCHAR(500) NULL,
    ShippingAddress NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE Sales (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SaleNumber NVARCHAR(50) NOT NULL UNIQUE,
    CustomerId INT NOT NULL,
    SaleDate DATETIME2 NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    CreatedByUserId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Sales_Customers FOREIGN KEY (CustomerId) REFERENCES Customers (Id),
    CONSTRAINT FK_Sales_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users (Id)
);

CREATE TABLE SalesItems (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SaleId BIGINT NOT NULL,
    ProductId INT NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_SalesItems_Sales FOREIGN KEY (SaleId) REFERENCES Sales (Id),
    CONSTRAINT FK_SalesItems_Products FOREIGN KEY (ProductId) REFERENCES Products (Id)
);

CREATE TABLE StockLedger (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    ReferenceType NVARCHAR(50) NOT NULL,
    ReferenceId BIGINT NOT NULL,
    QuantityChange DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_StockLedger_Products FOREIGN KEY (ProductId) REFERENCES Products (Id)
);

CREATE INDEX IX_Users_Username ON Users (Username);
CREATE INDEX IX_Users_Email ON Users (Email);
CREATE INDEX IX_Products_Name ON Products (Name);
CREATE INDEX IX_Customers_Name ON Customers (Name);
CREATE INDEX IX_Sales_SaleDate ON Sales (SaleDate);
CREATE INDEX IX_Sales_CustomerId ON Sales (CustomerId);
CREATE INDEX IX_SalesItems_SaleId ON SalesItems (SaleId);
CREATE INDEX IX_SalesItems_ProductId ON SalesItems (ProductId);
CREATE INDEX IX_StockLedger_ProductId ON StockLedger (ProductId);
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens (UserId);

CREATE TABLE PasswordResetTokens (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Token NVARCHAR(200) NOT NULL UNIQUE,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2 NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES Users (Id)
);

CREATE INDEX IX_PasswordResetTokens_UserId ON PasswordResetTokens (UserId);
CREATE INDEX IX_PasswordResetTokens_Token ON PasswordResetTokens (Token);

