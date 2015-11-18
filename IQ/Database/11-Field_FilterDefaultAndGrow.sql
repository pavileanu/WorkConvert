/*
   18 November 201411:29:29
   User: editor
   Server: iquote2.channelcentral.net,8484
   Database: imported2
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
ALTER TABLE dbo.Field ADD
	Grows bit NOT NULL CONSTRAINT DF_Field_Grows DEFAULT 0,
	DefaultFilterValues varchar(200) NULL
GO
ALTER TABLE dbo.Field SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.Field', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.Field', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.Field', 'Object', 'CONTROL') as Contr_Per 