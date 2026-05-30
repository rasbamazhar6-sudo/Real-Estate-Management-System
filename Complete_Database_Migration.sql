-- =============================================
-- Complete Database Migration Script
-- Ensures all columns exist for full UI functionality
-- Run this script in SQL Server Management Studio
-- =============================================

USE [RealEstateDB]
GO

PRINT 'Starting comprehensive database migration...'
GO

-- =============================================
-- 1. Add Notes column to Sales table (if missing)
-- =============================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Sales' AND COLUMN_NAME = 'Notes')
BEGIN
    ALTER TABLE [dbo].[Sales]
    ADD [Notes] [nvarchar](500) NULL
    PRINT 'Added Notes column to Sales table'
END
ELSE
BEGIN
    PRINT 'Notes column already exists in Sales table'
END
GO

-- =============================================
-- 2. Add PartyId column to Transactions table (if missing)
-- =============================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Transactions' AND COLUMN_NAME = 'PartyId')
BEGIN
    ALTER TABLE [dbo].[Transactions] 
    ADD [PartyId] [int] NULL
    
    PRINT 'Added PartyId column to Transactions table'
    
    -- Add foreign key constraint
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
                   WHERE name = 'FK_Transactions_PartyId')
    BEGIN
        ALTER TABLE [dbo].[Transactions]
        ADD CONSTRAINT [FK_Transactions_PartyId]
        FOREIGN KEY ([PartyId])
        REFERENCES [dbo].[Parties] ([PartyId])
        ON DELETE NO ACTION
        
        PRINT 'Created foreign key constraint FK_Transactions_PartyId'
    END
END
ELSE
BEGIN
    PRINT 'PartyId column already exists in Transactions table'
END
GO

-- =============================================
-- 3. Add PaymentPlans additional columns (if missing)
-- =============================================

-- PlanType
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'PaymentPlans' AND COLUMN_NAME = 'PlanType')
BEGIN
    ALTER TABLE [dbo].[PaymentPlans]
    ADD [PlanType] [nvarchar](50) NULL DEFAULT('Monthly')
    PRINT 'Added PlanType column to PaymentPlans table'
END
ELSE
BEGIN
    PRINT 'PlanType column already exists in PaymentPlans table'
END
GO

-- Status
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'PaymentPlans' AND COLUMN_NAME = 'Status')
BEGIN
    ALTER TABLE [dbo].[PaymentPlans]
    ADD [Status] [nvarchar](50) NULL DEFAULT('Active')
    PRINT 'Added Status column to PaymentPlans table'
END
ELSE
BEGIN
    PRINT 'Status column already exists in PaymentPlans table'
END
GO

-- OverdueReminder
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'PaymentPlans' AND COLUMN_NAME = 'OverdueReminder')
BEGIN
    ALTER TABLE [dbo].[PaymentPlans]
    ADD [OverdueReminder] [bit] NULL DEFAULT(1)
    PRINT 'Added OverdueReminder column to PaymentPlans table'
END
ELSE
BEGIN
    PRINT 'OverdueReminder column already exists in PaymentPlans table'
END
GO

-- UpcomingReminder
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'PaymentPlans' AND COLUMN_NAME = 'UpcomingReminder')
BEGIN
    ALTER TABLE [dbo].[PaymentPlans]
    ADD [UpcomingReminder] [bit] NULL DEFAULT(1)
    PRINT 'Added UpcomingReminder column to PaymentPlans table'
END
ELSE
BEGIN
    PRINT 'UpcomingReminder column already exists in PaymentPlans table'
END
GO

-- =============================================
-- 4. Update existing records with default values
-- =============================================

-- Update PaymentPlans
UPDATE [dbo].[PaymentPlans]
SET 
    PlanType = ISNULL(PlanType, 'Monthly'),
    Status = ISNULL(Status, 'Active'),
    OverdueReminder = ISNULL(OverdueReminder, 1),
    UpcomingReminder = ISNULL(UpcomingReminder, 1)
WHERE 
    PlanType IS NULL 
    OR Status IS NULL 
    OR OverdueReminder IS NULL 
    OR UpcomingReminder IS NULL
GO

-- =============================================
-- 5. Verify all tables and columns
-- =============================================

PRINT ''
PRINT '=== Database Schema Verification ==='
PRINT ''

-- Check Sales table
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Sales' AND COLUMN_NAME = 'Notes')
    PRINT '✓ Sales.Notes column exists'
ELSE
    PRINT '✗ Sales.Notes column MISSING'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Sales' AND COLUMN_NAME = 'CreatedAt')
    PRINT '✓ Sales.CreatedAt column exists'
ELSE
    PRINT '✗ Sales.CreatedAt column MISSING'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Sales' AND COLUMN_NAME = 'UpdatedAt')
    PRINT '✓ Sales.UpdatedAt column exists'
ELSE
    PRINT '✗ Sales.UpdatedAt column MISSING'

-- Check Transactions table
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Transactions' AND COLUMN_NAME = 'PartyId')
    PRINT '✓ Transactions.PartyId column exists'
ELSE
    PRINT '✗ Transactions.PartyId column MISSING'

-- Check PaymentPlans table
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentPlans' AND COLUMN_NAME = 'PlanType')
    PRINT '✓ PaymentPlans.PlanType column exists'
ELSE
    PRINT '✗ PaymentPlans.PlanType column MISSING'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentPlans' AND COLUMN_NAME = 'Status')
    PRINT '✓ PaymentPlans.Status column exists'
ELSE
    PRINT '✗ PaymentPlans.Status column MISSING'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentPlans' AND COLUMN_NAME = 'OverdueReminder')
    PRINT '✓ PaymentPlans.OverdueReminder column exists'
ELSE
    PRINT '✗ PaymentPlans.OverdueReminder column MISSING'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PaymentPlans' AND COLUMN_NAME = 'UpcomingReminder')
    PRINT '✓ PaymentPlans.UpcomingReminder column exists'
ELSE
    PRINT '✗ PaymentPlans.UpcomingReminder column MISSING'

PRINT ''
PRINT '=== Migration Complete ==='
PRINT 'All required columns have been verified/added to the database.'
GO

