USE MiniErp;
GO

------------------------------------------------------------
-- ROLES
------------------------------------------------------------
INSERT INTO Roles (Name)
VALUES ('Admin'), ('User');

DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Admin');
DECLARE @UserRoleId  INT = (SELECT Id FROM Roles WHERE Name = 'User');

------------------------------------------------------------
-- USERS (initial insert with placeholder hashes)
-- After running this, run db/seed-update-passwords.sql to set
-- real hashes: POST /api/auth/hash with body "admin@123", paste result.
------------------------------------------------------------
INSERT INTO Users (Username, Email, PasswordHash, IsActive)
VALUES
('admin',  'admin@example.com',  'dummy-hash-admin', 1),
('user1',  'user1@example.com',  'dummy-hash-user1', 1);

DECLARE @AdminUserId INT = (SELECT Id FROM Users WHERE Username = 'admin');
DECLARE @User1Id     INT = (SELECT Id FROM Users WHERE Username = 'user1');

------------------------------------------------------------
-- USER ROLES
------------------------------------------------------------
INSERT INTO UserRoles (UserId, RoleId)
VALUES
(@AdminUserId, @AdminRoleId),
(@User1Id,     @UserRoleId);

------------------------------------------------------------
-- PRODUCTS
------------------------------------------------------------
INSERT INTO Products (Sku, Name, Description, UnitPrice, ReorderLevel, IsActive)
VALUES
('P-001', 'Laptop 14"',        'Business laptop',   800.00,  5.00, 1),
('P-002', 'Wireless Mouse',    'Optical mouse',      20.00, 10.00, 1),
('P-003', 'Mechanical Keyboard','Gaming keyboard',   60.00,  5.00, 1);

DECLARE @ProdLaptopId   INT = (SELECT Id FROM Products WHERE Sku = 'P-001');
DECLARE @ProdMouseId    INT = (SELECT Id FROM Products WHERE Sku = 'P-002');
DECLARE @ProdKeyboardId INT = (SELECT Id FROM Products WHERE Sku = 'P-003');

------------------------------------------------------------
-- CUSTOMERS
------------------------------------------------------------
INSERT INTO Customers (Code, Name, Email, Phone, BillingAddress, ShippingAddress, IsActive)
VALUES
('C-001', 'Acme Corp',      'contact@acme.com',  '111-111-1111', 'Acme Billing Addr', 'Acme Shipping Addr', 1),
('C-002', 'Globex Ltd',     'info@globex.com',   '222-222-2222', 'Globex Billing',    'Globex Shipping',    1),
('C-003', 'Initech LLC',    'sales@initech.com', '333-333-3333', 'Initech Billing',   'Initech Shipping',   1);

DECLARE @CustAcmeId   INT = (SELECT Id FROM Customers WHERE Code = 'C-001');
DECLARE @CustGlobexId INT = (SELECT Id FROM Customers WHERE Code = 'C-002');

------------------------------------------------------------
-- INITIAL STOCK (OPENING BALANCE) INTO STOCKLEDGER
------------------------------------------------------------
INSERT INTO StockLedger (ProductId, ReferenceType, ReferenceId, QuantityChange)
VALUES
(@ProdLaptopId,   'Opening', 0, 50.00),
(@ProdMouseId,    'Opening', 0, 200.00),
(@ProdKeyboardId, 'Opening', 0, 100.00);

------------------------------------------------------------
-- SAMPLE SALES (DIRECT INSERTS JUST FOR TESTING GETs)
-- For production inserts, use Sp_CreateSale with JSON.
------------------------------------------------------------

-- Sale 1: Acme buys 2 laptops and 5 mice
INSERT INTO Sales (SaleNumber, CustomerId, SaleDate, TotalAmount, CreatedByUserId)
VALUES ('S-0001', @CustAcmeId, SYSUTCDATETIME(), 2*800.00 + 5*20.00, @AdminUserId);

DECLARE @Sale1Id BIGINT = SCOPE_IDENTITY();

INSERT INTO SalesItems (SaleId, ProductId, Quantity, UnitPrice, LineTotal)
VALUES
(@Sale1Id, @ProdLaptopId,  2.00, 800.00, 2.00*800.00),
(@Sale1Id, @ProdMouseId,   5.00,  20.00, 5.00* 20.00);

-- Sale 2: Globex buys 1 laptop and 1 keyboard
INSERT INTO Sales (SaleNumber, CustomerId, SaleDate, TotalAmount, CreatedByUserId)
VALUES ('S-0002', @CustGlobexId, DATEADD(DAY, -1, SYSUTCDATETIME()), 1*800.00 + 1*60.00, @AdminUserId);

DECLARE @Sale2Id BIGINT = SCOPE_IDENTITY();

INSERT INTO SalesItems (SaleId, ProductId, Quantity, UnitPrice, LineTotal)
VALUES
(@Sale2Id, @ProdLaptopId,   1.00, 800.00, 1.00*800.00),
(@Sale2Id, @ProdKeyboardId, 1.00,  60.00, 1.00* 60.00);

------------------------------------------------------------
-- STOCK MOVEMENTS FOR THESE SALES (SO STOCK REPORTS WORK)
------------------------------------------------------------
INSERT INTO StockLedger (ProductId, ReferenceType, ReferenceId, QuantityChange)
VALUES
-- Sale 1
(@ProdLaptopId,   'Sale', @Sale1Id, -2.00),
(@ProdMouseId,    'Sale', @Sale1Id, -5.00),
-- Sale 2
(@ProdLaptopId,   'Sale', @Sale2Id, -1.00),
(@ProdKeyboardId, 'Sale', @Sale2Id, -1.00);

------------------------------------------------------------
-- OPTIONAL: SAMPLE REFRESH TOKEN ROW (NOT NEEDED FOR GETs)
------------------------------------------------------------
-- INSERT INTO RefreshTokens (UserId, Token, JwtId, CreatedAt, ExpiresAt, IsRevoked)
-- VALUES (@AdminUserId, 'sample-refresh-token', NULL, SYSUTCDATETIME(), DATEADD(DAY, 7, SYSUTCDATETIME()), 0);

