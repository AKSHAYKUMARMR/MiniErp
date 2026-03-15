USE MiniErp;
GO

--------------------------------------------------------------------------------
-- Insight Reporting Queries (Admin-only; run after authentication)
-- Use these for reference or in reporting tools.
--------------------------------------------------------------------------------

-- 1. Total Sales Today
SELECT ISNULL(SUM(TotalAmount), 0) AS TotalSalesToday
FROM Sales
WHERE CAST(SaleDate AS DATE) = CAST(SYSUTCDATETIME() AS DATE);

-- 2. Revenue by Date Range (set @FromDate, @ToDate)
DECLARE @FromDate DATETIME2 = '2026-01-01';
DECLARE @ToDate   DATETIME2 = '2026-12-31';

SELECT ISNULL(SUM(TotalAmount), 0) AS Revenue
FROM Sales
WHERE SaleDate >= @FromDate
  AND SaleDate < DATEADD(DAY, 1, CAST(@ToDate AS DATE));

-- 3. Top Selling Products (set @FromDate, @ToDate, @TopN)
DECLARE @TopN INT = 10;

SELECT TOP (@TopN)
    p.Id          AS ProductId,
    p.Name        AS ProductName,
    SUM(si.Quantity)   AS TotalQuantity,
    SUM(si.LineTotal)  AS Revenue
FROM SalesItems si
INNER JOIN Sales s  ON s.Id = si.SaleId
INNER JOIN Products p ON p.Id = si.ProductId
WHERE s.SaleDate >= @FromDate
  AND s.SaleDate < DATEADD(DAY, 1, CAST(@ToDate AS DATE))
GROUP BY p.Id, p.Name
ORDER BY TotalQuantity DESC;

-- 4. Top Customers by Revenue (set @FromDate, @ToDate, @TopN)
SELECT TOP (@TopN)
    c.Id    AS CustomerId,
    c.Name  AS CustomerName,
    SUM(s.TotalAmount) AS Revenue
FROM Sales s
INNER JOIN Customers c ON c.Id = s.CustomerId
WHERE s.SaleDate >= @FromDate
  AND s.SaleDate < DATEADD(DAY, 1, CAST(@ToDate AS DATE))
GROUP BY c.Id, c.Name
ORDER BY Revenue DESC;

-- 5. Low Stock Products (products at or below reorder level)
;WITH StockOnHand AS (
    SELECT ProductId, SUM(QuantityChange) AS QtyOnHand
    FROM StockLedger
    GROUP BY ProductId
)
SELECT
    p.Id          AS ProductId,
    p.Name        AS ProductName,
    ISNULL(s.QtyOnHand, 0) AS QtyOnHand,
    p.ReorderLevel
FROM Products p
LEFT JOIN StockOnHand s ON s.ProductId = p.Id
WHERE p.ReorderLevel IS NOT NULL
  AND ISNULL(s.QtyOnHand, 0) <= p.ReorderLevel
ORDER BY QtyOnHand ASC;

-- 6. Current Stock Summary per Product
;WITH StockOnHand AS (
    SELECT ProductId, SUM(QuantityChange) AS QtyOnHand
    FROM StockLedger
    GROUP BY ProductId
)
SELECT
    p.Id          AS ProductId,
    p.Name        AS ProductName,
    p.Sku,
    ISNULL(s.QtyOnHand, 0) AS QtyOnHand,
    p.ReorderLevel
FROM Products p
LEFT JOIN StockOnHand s ON s.ProductId = p.Id
ORDER BY p.Name;

-- 7. Monthly Sales Trend
SELECT
    YEAR(SaleDate)   AS [Year],
    MONTH(SaleDate)  AS [Month],
    SUM(TotalAmount) AS Revenue
FROM Sales
GROUP BY YEAR(SaleDate), MONTH(SaleDate)
ORDER BY [Year], [Month];
