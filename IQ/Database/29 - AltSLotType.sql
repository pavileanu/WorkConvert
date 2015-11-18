/*
   16 February 201514:34:45
   User: 
   Server: localhost\sqlexpress
   Database: iQuote2_LIVE_UA7
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
ALTER TABLE dbo.AltSlotType
	DROP CONSTRAINT FK_AltSlotType_SlotType
GO
ALTER TABLE dbo.AltSlotType
	DROP CONSTRAINT FK_AltSlotType_SlotType1
GO
ALTER TABLE dbo.SlotType SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
BEGIN TRANSACTION
GO
CREATE TABLE dbo.Tmp_AltSlotType
	(
	ID int NOT NULL IDENTITY (1, 1),
	FK_SlotType_ID int NOT NULL,
	Fk_SlotType_ID_Alternative int NOT NULL,
	Priority int NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_AltSlotType SET (LOCK_ESCALATION = TABLE)
GO
DECLARE @v sql_variant 
SET @v = N'For each ''root'' type of slot..'
EXECUTE sp_addextendedproperty N'MS_Description', @v, N'SCHEMA', N'dbo', N'TABLE', N'Tmp_AltSlotType', N'COLUMN', N'FK_SlotType_ID'
GO
DECLARE @v sql_variant 
SET @v = N'Consider this alternative'
EXECUTE sp_addextendedproperty N'MS_Description', @v, N'SCHEMA', N'dbo', N'TABLE', N'Tmp_AltSlotType', N'COLUMN', N'Fk_SlotType_ID_Alternative'
GO
DECLARE @v sql_variant 
SET @v = N'In this order'
EXECUTE sp_addextendedproperty N'MS_Description', @v, N'SCHEMA', N'dbo', N'TABLE', N'Tmp_AltSlotType', N'COLUMN', N'Priority'
GO
SET IDENTITY_INSERT dbo.Tmp_AltSlotType ON
GO
IF EXISTS(SELECT * FROM dbo.AltSlotType)
	 EXEC('INSERT INTO dbo.Tmp_AltSlotType (ID, FK_SlotType_ID, Fk_SlotType_ID_Alternative, Priority)
		SELECT ID, FK_SlotType_ID, Fk_SlotType_ID_Alternative, Priority FROM dbo.AltSlotType WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_AltSlotType OFF
GO
DROP TABLE dbo.AltSlotType
GO
EXECUTE sp_rename N'dbo.Tmp_AltSlotType', N'AltSlotType', 'OBJECT' 
GO
ALTER TABLE dbo.AltSlotType ADD CONSTRAINT
	PK_AltSlotType PRIMARY KEY CLUSTERED 
	(
	ID
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.AltSlotType ADD CONSTRAINT
	FK_AltSlotType_SlotType FOREIGN KEY
	(
	FK_SlotType_ID
	) REFERENCES dbo.SlotType
	(
	ID
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.AltSlotType ADD CONSTRAINT
	FK_AltSlotType_SlotType1 FOREIGN KEY
	(
	Fk_SlotType_ID_Alternative
	) REFERENCES dbo.SlotType
	(
	ID
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
COMMIT
