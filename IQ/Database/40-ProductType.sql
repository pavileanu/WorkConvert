/*
   16 March 201516:08:52
   User: 
   Server: localhost\sqlexpress
   Database: iQuote2_LIVE_UA10
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
ALTER TABLE dbo.ProductType ADD
	[order] smallint NOT NULL CONSTRAINT DF_ProductType_order DEFAULT 0
GO
ALTER TABLE dbo.ProductType SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
