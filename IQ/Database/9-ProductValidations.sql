/****** Object:  Table [dbo].[ProductValidations]    Script Date: 10/11/2014 14:51:33 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ProductValidations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OptType] [varchar](10) NOT NULL,
	[ValidationType] [varchar](20) NOT NULL,
	[Severity] [varchar](20) NOT NULL,
	[FK_Translation_Key_Message] [int] NOT NULL,
	[RequiredQuantity] [int] NOT NULL CONSTRAINT [DF_ProductValidations_RequiredQuantity]  DEFAULT ((0)),
	[CheckAttribute] [varchar](50) NULL,
	[DependantOptType] [varchar](10) NULL,
	[DependantCheckAttribute] [varchar](50) NULL,
	[DependantCheckAttributeValue] [varchar](200) NULL,
	[CheckAttributeValue] [varchar](200) NULL,
	[OptionFamily] [varchar](50) NULL,
	[FK_Translation_Key_CorrectMessage] [int] NULL
 CONSTRAINT [PK_ProductValidations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO


/****** Object:  Table [dbo].[ProductValidationMappings]    Script Date: 10/11/2014 14:52:50 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ProductValidationMappings](
	[SystemType] [varchar](50) NOT NULL,
	[FK_ProductValidation_ID] [int] NOT NULL,
 CONSTRAINT [PK_ProductValidationMappings] PRIMARY KEY CLUSTERED 
(
	[SystemType] ASC,
	[FK_ProductValidation_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

