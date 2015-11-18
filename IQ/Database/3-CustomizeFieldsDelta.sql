SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

GO
CREATE TABLE [dbo].[AccountScreenOverride] (
		[FK_Account_ID]         [int] NOT NULL,
		[FK_Screen_ID]          [int] NOT NULL,
		[FK_Field_ID]           [int] NOT NULL,
		[Path]                  [nvarchar](255) NOT NULL,
		[ForceVisibilityTo]     [bit] NULL,
		[ForceOrderTo]          [int] NULL,
		[ForceWidthTo]          [float] NULL,
		[ForceSortTo]           [nvarchar](2) NULL,
		[ForceFilterTo]         [nvarchar](100)  NULL,
		CONSTRAINT [PK_AccountScreenOverride]
		PRIMARY KEY
		CLUSTERED
		([FK_Account_ID], [FK_Screen_ID], [FK_Field_ID], [Path])
)

GO
ALTER TABLE [dbo].[AccountScreenOverride]
	ADD
	CONSTRAINT [DF_AccountScreenOverride_ForceField]
	DEFAULT ((0)) FOR [ForceVisibilityTo]
GO
ALTER TABLE [dbo].[AccountScreenOverride]
	WITH CHECK
	ADD CONSTRAINT [FK_AccountScreenOverride_Account]
	FOREIGN KEY ([FK_Account_ID]) REFERENCES [dbo].[Account] ([ID])
ALTER TABLE [dbo].[AccountScreenOverride]
	CHECK CONSTRAINT [FK_AccountScreenOverride_Account]

GO
ALTER TABLE [dbo].[AccountScreenOverride]
	WITH CHECK
	ADD CONSTRAINT [FK_AccountScreenOverride_Field]
	FOREIGN KEY ([FK_Field_ID]) REFERENCES [dbo].[Field] ([ID])
ALTER TABLE [dbo].[AccountScreenOverride]
	CHECK CONSTRAINT [FK_AccountScreenOverride_Field]

GO
ALTER TABLE [dbo].[AccountScreenOverride]
	WITH CHECK
	ADD CONSTRAINT [FK_AccountScreenOverride_Screen]
	FOREIGN KEY ([FK_Screen_ID]) REFERENCES [dbo].[Screen] ([ID])
ALTER TABLE [dbo].[AccountScreenOverride]
	CHECK CONSTRAINT [FK_AccountScreenOverride_Screen]

GO
ALTER TABLE [dbo].[AccountScreenOverride] SET (LOCK_ESCALATION = TABLE)
GO

ALTER TABLE Field ADD CanUserSelect bit NOT NULL
CONSTRAINT [DF_Fields_CanUserSelect] DEFAULT ((0)) 
GO

update field set CanUserSelect = (VisibleList | VisiblePage | VisibleSquare )
GO