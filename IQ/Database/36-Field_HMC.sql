/*
   10 March 201509:24:46
   User: 
   Server: localhost\sqlexpress
   Database: iQuote2_LIVE_UA8
   Application: 
*/

/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
EXECUTE sp_rename N'dbo.Field.filtervisible', N'Tmp_FilterVisible_7', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Field.Tmp_FilterVisible_7', N'FilterVisible', 'COLUMN' 
GO
ALTER TABLE dbo.Field ADD
	HMC_MutualExclusivity bit NOT NULL CONSTRAINT DF_Field_HMC_MutualExclusivity DEFAULT 0,
	InvertFilterOrder bit NOT NULL CONSTRAINT DF_Field_InvertFilterOrder DEFAULT 0
GO
ALTER TABLE dbo.Field SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
