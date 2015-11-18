/****** Object:  Table [dbo].[FieldRestriction]    Script Date: 05/02/2015 16:18:53 ******/
SET ANSI_NULLS ON
GO
use iquote2_test


SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FieldRestriction](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FK_Field_ID] [int] NOT NULL,
	[FK_Region_ID] [int] NOT NULL,
 CONSTRAINT [PK_FieldRestriction] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


