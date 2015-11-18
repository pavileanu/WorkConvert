USE [iQuote2]
GO

/****** Object:  Table [dbo].[Region]    Script Date: 18/09/2014 11:59:32 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER TABLE [dbo].[FlexRule]
ADD [optionalRule] [bit] NULL

GO
ALTER TABLE [dbo].[FlexRule] ADD  CONSTRAINT [DF_FlexRule_optionalRule]  DEFAULT ((0)) FOR [optionalRule]
GO

UPDATE FlexRule SET optionalRule = 0
GO
