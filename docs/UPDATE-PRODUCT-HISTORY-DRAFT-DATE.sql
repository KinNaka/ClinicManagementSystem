BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023173429_AddProductEditHistoryAndDraftSaleDate'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sales]') AND [c].[name] = N'SaleDate');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Sales] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Sales] ALTER COLUMN [SaleDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023173429_AddProductEditHistoryAndDraftSaleDate'
)
BEGIN
    UPDATE Sales SET SaleDate = NULL WHERE Status = 'Draft';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023173429_AddProductEditHistoryAndDraftSaleDate'
)
BEGIN
    UPDATE s SET SaleDate = t.ConfirmedAt
                    FROM Sales s INNER JOIN (
                        SELECT ReferenceNumber, MIN(TransactionDate) AS ConfirmedAt
                        FROM InventoryTransactions
                        WHERE TransactionType = 'StockOut' AND Reason = N'Xuất kho bán hàng'
                        GROUP BY ReferenceNumber
                    ) t ON s.SaleCode = t.ReferenceNumber
                    WHERE s.Status = 'Confirmed';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023173429_AddProductEditHistoryAndDraftSaleDate'
)
BEGIN
    CREATE TABLE [ProductEditHistories] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NOT NULL,
        [ProductCode] nvarchar(100) NOT NULL,
        [ProductName] nvarchar(500) NOT NULL,
        [ActorId] nvarchar(450) NULL,
        [ActorName] nvarchar(256) NULL,
        [ChangedAt] datetime2 NOT NULL,
        [FieldName] nvarchar(100) NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        CONSTRAINT [PK_ProductEditHistories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023173429_AddProductEditHistoryAndDraftSaleDate'
)
BEGIN
    CREATE INDEX [IX_ProductEditHistories_ProductId_ChangedAt_Id] ON [ProductEditHistories] ([ProductId], [ChangedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023173429_AddProductEditHistoryAndDraftSaleDate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023173429_AddProductEditHistoryAndDraftSaleDate', N'10.0.12');
END;

COMMIT;
GO

