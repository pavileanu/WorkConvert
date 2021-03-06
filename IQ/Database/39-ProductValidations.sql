/*
   16 March 201516:09:21
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
ALTER TABLE dbo.ProductValidations ADD
	LinkOptionFamily varchar(20) NULL
ALTER TABLE dbo.ProductValidations ADD
	LinkTechnology varchar(20) NULL
ALTER TABLE dbo.ProductValidations ADD
	LinkOptType varchar(4) NULL
GO
ALTER TABLE dbo.ProductValidations SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
