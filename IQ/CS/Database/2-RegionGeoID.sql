USE [iQuote2]
GO

/****** Object:  Table [dbo].[Region]    Script Date: 18/09/2014 11:59:32 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



ALTER TABLE [dbo].[Region]
ADD [FK_Region_ID_Geo] [int] NULL



ALTER TABLE [dbo].[Region]
ADD [IsPlaceHolder] [bit] default 0 not null

ALTER TABLE [dbo].[Region]
ADD [Notes] nvarchar(1000) default '' not null


