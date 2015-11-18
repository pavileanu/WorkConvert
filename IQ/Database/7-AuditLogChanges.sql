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
CREATE TABLE dbo.Tmp_AuditLog
	(
	Id int NOT NULL IDENTITY (1, 1),
	AuditType varchar(50) NOT NULL,
	DateTime datetime NOT NULL,
	lid decimal(38, 0) NOT NULL,
	Action varchar(50) NOT NULL,
	SourcePath varchar(200) NOT NULL,
	TargetPath varchar(200) NULL,
	[Messages] varchar(MAX) NULL,
	SecondaryMessage varchar(MAX) NULL,
	PageName varchar(250) NULL,
	SourceURL varchar(MAX) NULL,
	TimeToLoad_MS float(53) NULL,
	HttpRequestMethod varchar(50) NULL,
	UrlReferrer varchar(MAX) NULL,
	ParentId int NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_AuditLog SET (LOCK_ESCALATION = TABLE)
GO
ALTER TABLE dbo.Tmp_AuditLog ADD CONSTRAINT
	DF_AuditLog_AuditType DEFAULT 'Information' FOR AuditType
GO
SET IDENTITY_INSERT dbo.Tmp_AuditLog ON
GO
IF EXISTS(SELECT * FROM dbo.AuditLog)
	 EXEC('INSERT INTO dbo.Tmp_AuditLog (Id, DateTime, lid, Action, SourcePath, TargetPath, Messages, SecondaryMessage, PageName, SourceURL, TimeToLoad_MS, HttpRequestMethod, UrlReferrer, ParentId)
		SELECT Id, DateTime, lid, Action, SourcePath, TargetPath, ErrorMessage, ExceptionDetails, PageName, SourceURL, TimeToLoad, HttpRequestMethod, UrlReferrer, ParentId FROM dbo.AuditLog WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_AuditLog OFF
GO
DROP TABLE dbo.AuditLog
GO
EXECUTE sp_rename N'dbo.Tmp_AuditLog', N'AuditLog', 'OBJECT' 
GO
ALTER TABLE dbo.AuditLog ADD CONSTRAINT
	PK_AuditLog PRIMARY KEY CLUSTERED 
	(
	Id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
COMMIT
