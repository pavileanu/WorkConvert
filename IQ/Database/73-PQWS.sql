
IF OBJECT_ID(N'dbo.TROAA', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[TROAA]
END
GO

IF OBJECT_ID(N'dbo.ServiceLevelMap', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[ServiceLevelMap]
END
GO

IF OBJECT_ID(N'dbo.DMR', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[DMR]
END
GO

IF OBJECT_ID(N'dbo.Response', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[Response]
END
GO

IF OBJECT_ID(N'dbo.ServiceType', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[ServiceType]
END
GO

IF OBJECT_ID(N'dbo.AttributeMap', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[AttributeMap]
END
GO

IF OBJECT_ID(N'dbo.ServiceLevelAttributeMap', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[ServiceLevelAttributeMap]
END
GO

IF OBJECT_ID(N'dbo.ServiceLevelAttribute', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[ServiceLevelAttribute]
END
GO

IF OBJECT_ID(N'dbo.ServiceLevel', N'U') IS NOT NULL
BEGIN
	drop table [DBO].[ServiceLevel]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Response](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[mfrCode] [nvarchar](3) NULL,
	[FK_Translation_Key_Title] [int] NOT NULL,
	[FK_Translation_Key_Description] [int] NOT NULL,
	[ResponseDefault] [bit] NOT NULL CONSTRAINT [DF_Response_ResponseDefault]  DEFAULT ((0)),
	[HPID] [nvarchar](50) NULL,
 CONSTRAINT [PK_Response] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServiceType](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[mfrCode] [nvarchar](3) NULL,
	[FK_Translation_Key_Title] [int] NOT NULL,
	[FK_Translation_Key_Description] [int] NULL,
	[ServiceTypeDefault] [bit] NOT NULL CONSTRAINT [DF_ServiceType_ServiceTypeDefault]  DEFAULT ((0)),
	[HPID] [nvarchar](50) NULL,
 CONSTRAINT [PK_ServiceType] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TROAA](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[SysFamily] [nvarchar](50) NULL,
	[SlotTypeCode] [int] NULL,
	[ServiceLevel] [int] NULL,
	[DisplayOrder] [int] NULL,
	[FK_ServiceLevelMap_ID] [int] NULL,
	[FK_Region_ID] [int] NULL,
 CONSTRAINT [PK_TROAA] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServiceLevelMap](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[mfrCode] [nvarchar](3) NULL,
	[ServiceLevel] [int] NULL,
	[ServiceLevelGroup] [nvarchar](50) NULL,
	[SuperGroup] [nvarchar](50) NULL,
	[FK_Translation_Key_Description] [int] NOT NULL,
	[Duration] [int] NULL,
	[PostWarranty] [bit] NULL,
	[Disabled] [bit] NULL,
	[FK_ServiceType_ID] [int] NULL,
	[FK_Response_ID] [int] NULL,
	[hpeDMR] [bit] NULL,
	[hpeCDMR] [bit] NULL,
	[hpiADP] [bit] NULL,
	[hpiDMR] [bit] NULL,
	[hpiTravel] [bit] NULL,
	[hpiTracing] [bit] NULL,
	[hpiTheft] [bit] NULL,
 CONSTRAINT [PK_ServiceLevelMap] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServiceLevelAttributeMap](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Code]	[nvarchar](50) NOT NULL,
	[FK_Attribute_Code] [varchar](20) NOT NULL,
 CONSTRAINT [PK_ServiceLevelAttributeMap] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('FK_ServiceType_ID', 'servicelevel')
insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('FK_Response_ID', 'response')
insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('hpeDMR', 'DMR_ISS')
insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('hpeCDMR', 'DMR_ISS')
insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('hpiADP', 'ADP')
insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('hpiDMR', 'DMR')
insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('hpiTravel', 'travel')
insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('hpiTracing', 'tracing')
insert into [dbo].[ServiceLevelAttributeMap]([Code], [FK_Attribute_Code]) values ('hpiTheft', 'theft')

GO

ALTER TABLE [dbo].[ServiceLevelMap]  WITH CHECK ADD  CONSTRAINT [FK_ServiceLevelMap_Response] FOREIGN KEY([FK_Response_ID])
REFERENCES [dbo].[Response] ([ID])
GO
ALTER TABLE [dbo].[ServiceLevelMap] CHECK CONSTRAINT [FK_ServiceLevelMap_Response]
GO

ALTER TABLE [dbo].[ServiceLevelMap]  WITH CHECK ADD  CONSTRAINT [FK_ServiceLevelMap_ServiceType] FOREIGN KEY([Fk_ServiceType_ID])
REFERENCES [dbo].[ServiceType] ([ID])
GO
ALTER TABLE [dbo].[ServiceLevelMap] CHECK CONSTRAINT [FK_ServiceLevelMap_ServiceType]
GO

ALTER TABLE [dbo].[TROAA]  WITH CHECK ADD  CONSTRAINT [FK_TROAA_ServiceLevelMap] FOREIGN KEY([Fk_ServiceLevelMap_ID])
REFERENCES [dbo].[ServiceLevelMap] ([ID])
GO
ALTER TABLE [dbo].[TROAA] CHECK CONSTRAINT [FK_TROAA_ServiceLevelMap]
GO

