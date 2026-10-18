BEGIN TRANSACTION;
ALTER TABLE [InventoryTransactions] ADD [PaidAt] datetime2 NULL;

ALTER TABLE [InventoryTransactions] ADD [PaidBy] nvarchar(100) NULL;

ALTER TABLE [InventoryTransactions] ADD [PatientId] bigint NULL;

UPDATE t SET PatientId = s.PatientId
                FROM InventoryTransactions t INNER JOIN Sales s ON t.ReferenceNumber = s.SaleCode
                WHERE t.TransactionType = 'StockOut' AND t.Reason = N'Xuất kho bán hàng';

CREATE INDEX [IX_InventoryTransactions_PatientId] ON [InventoryTransactions] ([PatientId]);

ALTER TABLE [InventoryTransactions] ADD CONSTRAINT [FK_InventoryTransactions_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261018161818_AddInventoryPaymentAndPatient', N'10.0.12');

COMMIT;
GO

