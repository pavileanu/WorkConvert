SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON



GO

CREATE TABLE [dbo].[Measure] (

		[ID]              [int] IDENTITY(1, 1) NOT NULL,

		[MeasureName]     [varchar](50) COLLATE Latin1_General_CI_AS NOT NULL,

		CONSTRAINT [PK_Measure]

		PRIMARY KEY

		CLUSTERED

		([ID])

)

GO


ALTER TABLE [dbo].[Measure] SET (LOCK_ESCALATION = TABLE)

GO

ALTER TABLE Unit ADD	[FK_Measure_ID]               [int] NOT NULL CONSTRAINT [DF_FK_Measure_ID]  DEFAULT ((0))

ALTER TABLE AccountScreenOverride ADD [FK_DisplayUnit_ID]     [int] NULL

ALTER TABLE [dbo].[attribute]
ADD [order] [int] default 0 not null
