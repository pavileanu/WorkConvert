

/****** Object:  Table [dbo].[ValidationInclusion]    Script Date: 28/01/2015 14:21:13 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ValidationInclusion](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[MajorSlotType] [varchar](20) NOT NULL,
	[MinorSlotType] [varchar](20) NULL,
	[InclusionType] [smallint] NOT NULL,
 CONSTRAINT [PK_ValidationInclusion_1] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO


