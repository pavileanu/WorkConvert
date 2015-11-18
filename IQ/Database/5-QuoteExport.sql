USE [iQuote2]
GO

/****** Object:  Table [dbo].[QuoteExport]    Script Date: 23/09/2014 09:50:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[QuoteExport](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FK_Quote_ID] [int] NOT NULL,
	[Type] [nvarchar](50) NULL,
	[TimeStamp] [datetime] NULL,
 CONSTRAINT [PK_QuoteExport] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[QuoteExport] ADD  CONSTRAINT [DF_QuoteExport_TimeStamp]  DEFAULT (getdate()) FOR [TimeStamp]
GO


