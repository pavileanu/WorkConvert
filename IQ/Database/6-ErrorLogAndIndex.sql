USE [iQuote2]
GO
/****** Object:  Table [dbo].[ErrorLog]    Script Date: 26/09/2014 13:07:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ErrorLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DateTime] [datetime] NOT NULL,
	[Message] [varchar](max) NOT NULL,
	[StackTrace] [varchar](max) NOT NULL,
	[InnerException] [varchar](max) NULL,
 CONSTRAINT [PK_ErrorLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO

/***** Index for getting account prices, speeds up first account logon considerably****/

GO
CREATE NONCLUSTERED INDEX idxVariantSellerID
ON [dbo].[Variant] ([FK_Channel_ID_Seller])
INCLUDE ([ID],[FK_Product_ID])
GO


		alter table login add fk_account_id_agent integer

		ALTER TABLE AuditLog ADD ParentId int

		alter table currency alter column [symbol] nvarchar(5)