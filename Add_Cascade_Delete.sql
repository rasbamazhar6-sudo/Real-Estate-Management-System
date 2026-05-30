-- =============================================
-- SQL Script to Add ON DELETE CASCADE to Foreign Keys
-- This allows automatic deletion of dependent records
-- Run this script in SQL Server Management Studio against RealEstateDB
-- =============================================

USE [RealEstateDB]
GO

PRINT 'Starting to add ON DELETE CASCADE to foreign keys...'
GO

-- =============================================
-- Step 1: Drop existing foreign keys that need CASCADE
-- =============================================

-- Drop Plots.ProjectId foreign key
DECLARE @FKName1 NVARCHAR(128)
SELECT @FKName1 = fk.name 
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Plots') 
AND fk.referenced_object_id = OBJECT_ID('dbo.Projects')
AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'ProjectId'

IF @FKName1 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[Plots] DROP CONSTRAINT [' + @FKName1 + ']')
    PRINT 'Dropped foreign key: Plots.ProjectId → Projects.ProjectId (' + @FKName1 + ')'
END
ELSE
BEGIN
    PRINT 'Foreign key not found: Plots.ProjectId → Projects.ProjectId (may already be dropped)'
END
GO

-- Drop Sales.ProjectId foreign key (will recreate WITHOUT CASCADE to avoid multiple paths)
-- Note: Sales.ProjectId will remain as NO ACTION because Sales already cascades from Plots
DECLARE @FKName2 NVARCHAR(128)
SELECT @FKName2 = fk.name 
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Sales') 
AND fk.referenced_object_id = OBJECT_ID('dbo.Projects')
AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'ProjectId'

IF @FKName2 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[Sales] DROP CONSTRAINT [' + @FKName2 + ']')
    PRINT 'Dropped foreign key: Sales.ProjectId → Projects.ProjectId (' + @FKName2 + ')'
END
ELSE
BEGIN
    PRINT 'Foreign key not found: Sales.ProjectId → Projects.ProjectId (may already be dropped)'
END
GO

-- Drop Sales.PlotId foreign key
DECLARE @FKName3 NVARCHAR(128)
SELECT @FKName3 = fk.name 
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Sales') 
AND fk.referenced_object_id = OBJECT_ID('dbo.Plots')
AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'PlotId'

IF @FKName3 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[Sales] DROP CONSTRAINT [' + @FKName3 + ']')
    PRINT 'Dropped foreign key: Sales.PlotId → Plots.PlotId (' + @FKName3 + ')'
END
ELSE
BEGIN
    PRINT 'Foreign key not found: Sales.PlotId → Plots.PlotId (may already be dropped)'
END
GO

-- Drop PaymentPlans.SaleId foreign key
DECLARE @FKName4 NVARCHAR(128)
SELECT @FKName4 = fk.name 
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.PaymentPlans') 
AND fk.referenced_object_id = OBJECT_ID('dbo.Sales')
AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'SaleId'

IF @FKName4 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[PaymentPlans] DROP CONSTRAINT [' + @FKName4 + ']')
    PRINT 'Dropped foreign key: PaymentPlans.SaleId → Sales.SaleId (' + @FKName4 + ')'
END
ELSE
BEGIN
    PRINT 'Foreign key not found: PaymentPlans.SaleId → Sales.SaleId (may already be dropped)'
END
GO

-- Drop Installments.PaymentPlanId foreign key
DECLARE @FKName5 NVARCHAR(128)
SELECT @FKName5 = fk.name 
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Installments') 
AND fk.referenced_object_id = OBJECT_ID('dbo.PaymentPlans')
AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'PaymentPlanId'

IF @FKName5 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[Installments] DROP CONSTRAINT [' + @FKName5 + ']')
    PRINT 'Dropped foreign key: Installments.PaymentPlanId → PaymentPlans.PaymentPlanId (' + @FKName5 + ')'
END
ELSE
BEGIN
    PRINT 'Foreign key not found: Installments.PaymentPlanId → PaymentPlans.PaymentPlanId (may already be dropped)'
END
GO

-- Drop Transactions.SaleId foreign key (will recreate WITHOUT CASCADE to avoid multiple paths)
-- Note: Transactions.SaleId will remain as NO ACTION because Transactions already cascades from Installments
DECLARE @FKName6 NVARCHAR(128)
SELECT @FKName6 = fk.name 
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Transactions') 
AND fk.referenced_object_id = OBJECT_ID('dbo.Sales')
AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'SaleId'

IF @FKName6 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[Transactions] DROP CONSTRAINT [' + @FKName6 + ']')
    PRINT 'Dropped foreign key: Transactions.SaleId → Sales.SaleId (' + @FKName6 + ')'
END
ELSE
BEGIN
    PRINT 'Foreign key not found: Transactions.SaleId → Sales.SaleId (may already be dropped)'
END
GO

-- Drop Transactions.InstallmentId foreign key
DECLARE @FKName7 NVARCHAR(128)
SELECT @FKName7 = fk.name 
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Transactions') 
AND fk.referenced_object_id = OBJECT_ID('dbo.Installments')
AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'InstallmentId'

IF @FKName7 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[Transactions] DROP CONSTRAINT [' + @FKName7 + ']')
    PRINT 'Dropped foreign key: Transactions.InstallmentId → Installments.InstallmentId (' + @FKName7 + ')'
END
ELSE
BEGIN
    PRINT 'Foreign key not found: Transactions.InstallmentId → Installments.InstallmentId (may already be dropped)'
END
GO

-- =============================================
-- Step 2: Recreate foreign keys with ON DELETE CASCADE
-- =============================================

-- Recreate Plots.ProjectId with CASCADE
IF NOT EXISTS (SELECT * FROM sys.foreign_keys fk
               INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
               WHERE fk.parent_object_id = OBJECT_ID('dbo.Plots') 
               AND fk.referenced_object_id = OBJECT_ID('dbo.Projects')
               AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'ProjectId')
BEGIN
    ALTER TABLE [dbo].[Plots]
    ADD CONSTRAINT [FK_Plots_ProjectId_Cascade]
    FOREIGN KEY ([ProjectId])
    REFERENCES [dbo].[Projects] ([ProjectId])
    ON DELETE CASCADE
    PRINT 'Created foreign key with CASCADE: Plots.ProjectId → Projects.ProjectId'
END
GO

-- Recreate Sales.ProjectId WITHOUT CASCADE (to avoid multiple cascade paths)
-- Sales already cascades from Plots, so this FK remains as NO ACTION
-- When Project is deleted → Plots cascade → Sales cascade (through Plots)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys fk
               INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
               WHERE fk.parent_object_id = OBJECT_ID('dbo.Sales') 
               AND fk.referenced_object_id = OBJECT_ID('dbo.Projects')
               AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'ProjectId')
BEGIN
    ALTER TABLE [dbo].[Sales]
    ADD CONSTRAINT [FK_Sales_ProjectId_NoAction]
    FOREIGN KEY ([ProjectId])
    REFERENCES [dbo].[Projects] ([ProjectId])
    ON DELETE NO ACTION
    PRINT 'Created foreign key WITHOUT CASCADE: Sales.ProjectId → Projects.ProjectId (NO ACTION - cascades through Plots)'
END
GO

-- Recreate Sales.PlotId with CASCADE
IF NOT EXISTS (SELECT * FROM sys.foreign_keys fk
               INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
               WHERE fk.parent_object_id = OBJECT_ID('dbo.Sales') 
               AND fk.referenced_object_id = OBJECT_ID('dbo.Plots')
               AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'PlotId')
BEGIN
    ALTER TABLE [dbo].[Sales]
    ADD CONSTRAINT [FK_Sales_PlotId_Cascade]
    FOREIGN KEY ([PlotId])
    REFERENCES [dbo].[Plots] ([PlotId])
    ON DELETE CASCADE
    PRINT 'Created foreign key with CASCADE: Sales.PlotId → Plots.PlotId'
END
GO

-- Recreate PaymentPlans.SaleId with CASCADE
IF NOT EXISTS (SELECT * FROM sys.foreign_keys fk
               INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
               WHERE fk.parent_object_id = OBJECT_ID('dbo.PaymentPlans') 
               AND fk.referenced_object_id = OBJECT_ID('dbo.Sales')
               AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'SaleId')
BEGIN
    ALTER TABLE [dbo].[PaymentPlans]
    ADD CONSTRAINT [FK_PaymentPlans_SaleId_Cascade]
    FOREIGN KEY ([SaleId])
    REFERENCES [dbo].[Sales] ([SaleId])
    ON DELETE CASCADE
    PRINT 'Created foreign key with CASCADE: PaymentPlans.SaleId → Sales.SaleId'
END
GO

-- Recreate Installments.PaymentPlanId with CASCADE
IF NOT EXISTS (SELECT * FROM sys.foreign_keys fk
               INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
               WHERE fk.parent_object_id = OBJECT_ID('dbo.Installments') 
               AND fk.referenced_object_id = OBJECT_ID('dbo.PaymentPlans')
               AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'PaymentPlanId')
BEGIN
    ALTER TABLE [dbo].[Installments]
    ADD CONSTRAINT [FK_Installments_PaymentPlanId_Cascade]
    FOREIGN KEY ([PaymentPlanId])
    REFERENCES [dbo].[PaymentPlans] ([PaymentPlanId])
    ON DELETE CASCADE
    PRINT 'Created foreign key with CASCADE: Installments.PaymentPlanId → PaymentPlans.PaymentPlanId'
END
GO

-- Recreate Transactions.SaleId WITHOUT CASCADE (to avoid multiple cascade paths)
-- Transactions already cascades from Installments, so this FK remains as NO ACTION
-- When Sale is deleted → PaymentPlans cascade → Installments cascade → Transactions cascade (through Installments)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys fk
               INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
               WHERE fk.parent_object_id = OBJECT_ID('dbo.Transactions') 
               AND fk.referenced_object_id = OBJECT_ID('dbo.Sales')
               AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'SaleId')
BEGIN
    ALTER TABLE [dbo].[Transactions]
    ADD CONSTRAINT [FK_Transactions_SaleId_NoAction]
    FOREIGN KEY ([SaleId])
    REFERENCES [dbo].[Sales] ([SaleId])
    ON DELETE NO ACTION
    PRINT 'Created foreign key WITHOUT CASCADE: Transactions.SaleId → Sales.SaleId (NO ACTION - cascades through Installments)'
END
GO

-- Recreate Transactions.InstallmentId with CASCADE
IF NOT EXISTS (SELECT * FROM sys.foreign_keys fk
               INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
               WHERE fk.parent_object_id = OBJECT_ID('dbo.Transactions') 
               AND fk.referenced_object_id = OBJECT_ID('dbo.Installments')
               AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'InstallmentId')
BEGIN
    ALTER TABLE [dbo].[Transactions]
    ADD CONSTRAINT [FK_Transactions_InstallmentId_Cascade]
    FOREIGN KEY ([InstallmentId])
    REFERENCES [dbo].[Installments] ([InstallmentId])
    ON DELETE CASCADE
    PRINT 'Created foreign key with CASCADE: Transactions.InstallmentId → Installments.InstallmentId'
END
GO

-- =============================================
-- Verification: List all CASCADE foreign keys
-- =============================================

PRINT ''
PRINT '============================================='
PRINT 'Verification: Foreign Keys with CASCADE'
PRINT '============================================='

SELECT 
    OBJECT_NAME(fk.parent_object_id) AS [Child Table],
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS [Child Column],
    OBJECT_NAME(fk.referenced_object_id) AS [Parent Table],
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS [Parent Column],
    fk.name AS [Constraint Name],
    CASE 
        WHEN fk.delete_referential_action = 1 THEN 'CASCADE'
        WHEN fk.delete_referential_action = 0 THEN 'NO ACTION'
        ELSE 'OTHER'
    END AS [Delete Action]
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.delete_referential_action = 1  -- 1 = CASCADE
ORDER BY [Child Table], [Child Column]
GO

PRINT ''
PRINT '============================================='
PRINT 'CASCADE DELETE setup completed successfully!'
PRINT '============================================='
PRINT ''
PRINT 'Single cascade delete path (avoids Error 1785):'
PRINT '  Projects → Plots → Sales → PaymentPlans → Installments → Transactions'
PRINT ''
PRINT 'Cascade behavior:'
PRINT '  • Delete Project: Cascades to Plots → Sales → PaymentPlans → Installments → Transactions'
PRINT '  • Delete Plot: Cascades to Sales → PaymentPlans → Installments → Transactions'
PRINT '  • Delete Sale: Cascades to PaymentPlans → Installments → Transactions'
PRINT '  • Delete PaymentPlan: Cascades to Installments → Transactions'
PRINT '  • Delete Installment: Cascades to Transactions'
PRINT ''
PRINT 'Note: Sales.ProjectId and Transactions.SaleId are NO ACTION to avoid multiple cascade paths.'
PRINT 'They still maintain referential integrity but cascade through the single path above.'
GO

