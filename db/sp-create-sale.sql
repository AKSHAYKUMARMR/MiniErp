IF OBJECT_ID('dbo.Sp_CreateSale', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sp_CreateSale;
GO

CREATE PROCEDURE dbo.Sp_CreateSale
(
    @CustomerId        INT,
    @SaleDate          DATETIME2,
    @CreatedByUserId   INT,
    @ItemsJson         NVARCHAR(MAX)  -- JSON array: [{ "ProductId":1, "Quantity":2, "UnitPrice":100.0 }, ...]
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @SaleItems TABLE
        (
            ProductId  INT,
            Quantity   DECIMAL(18,2),
            UnitPrice  DECIMAL(18,2),
            LineTotal  DECIMAL(18,2)
        );

        INSERT INTO @SaleItems (ProductId, Quantity, UnitPrice, LineTotal)
        SELECT
            j.ProductId,
            j.Quantity,
            j.UnitPrice,
            j.Quantity * j.UnitPrice
        FROM OPENJSON(@ItemsJson)
        WITH
        (
            ProductId INT             '$.ProductId',
            Quantity  DECIMAL(18,2)   '$.Quantity',
            UnitPrice DECIMAL(18,2)   '$.UnitPrice'
        ) AS j;

        IF NOT EXISTS (SELECT 1 FROM @SaleItems)
        BEGIN
            RAISERROR('Sale must contain at least one item.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        ;WITH RequiredQty AS
        (
            SELECT ProductId, SUM(Quantity) AS QtyRequired
            FROM @SaleItems
            GROUP BY ProductId
        ),
        CurrentStock AS
        (
            SELECT
                sl.ProductId,
                SUM(sl.QuantityChange) AS QtyOnHand
            FROM StockLedger sl WITH (UPDLOCK, HOLDLOCK)
            WHERE sl.ProductId IN (SELECT ProductId FROM RequiredQty)
            GROUP BY sl.ProductId
        )
        SELECT r.ProductId
        INTO #Insufficient
        FROM RequiredQty r
        LEFT JOIN CurrentStock c ON c.ProductId = r.ProductId
        WHERE ISNULL(c.QtyOnHand, 0) - r.QtyRequired < 0;

        IF EXISTS (SELECT 1 FROM #Insufficient)
        BEGIN
            RAISERROR('Insufficient stock for one or more products.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        DROP TABLE #Insufficient;

        DECLARE @TotalAmount DECIMAL(18,2);

        SELECT @TotalAmount = SUM(LineTotal) FROM @SaleItems;

        -- Insert with temporary unique SaleNumber; will set to S-{SaleId} after we have the Id
        DECLARE @TempSaleNumber NVARCHAR(50) = CONCAT('S-', FORMAT(SYSUTCDATETIME(), 'yyyyMMddHHmmssfff'));

        INSERT INTO Sales (SaleNumber, CustomerId, SaleDate, TotalAmount, CreatedByUserId, CreatedAt)
        VALUES (@TempSaleNumber, @CustomerId, @SaleDate, @TotalAmount, @CreatedByUserId, SYSUTCDATETIME());

        DECLARE @SaleId BIGINT = SCOPE_IDENTITY();

        UPDATE Sales SET SaleNumber = CONCAT('S-', @SaleId) WHERE Id = @SaleId;

        INSERT INTO SalesItems (SaleId, ProductId, Quantity, UnitPrice, LineTotal)
        SELECT
            @SaleId,
            ProductId,
            Quantity,
            UnitPrice,
            LineTotal
        FROM @SaleItems;

        INSERT INTO StockLedger (ProductId, ReferenceType, ReferenceId, QuantityChange, CreatedAt)
        SELECT
            ProductId,
            'Sale',
            @SaleId,
            -Quantity,
            SYSUTCDATETIME()
        FROM @SaleItems;

        SELECT @SaleId AS SaleId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000), @ErrSeverity INT;
        SELECT @ErrMsg = ERROR_MESSAGE(), @ErrSeverity = ERROR_SEVERITY();
        RAISERROR(@ErrMsg, @ErrSeverity, 1);
    END CATCH
END;

