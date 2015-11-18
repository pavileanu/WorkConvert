

/****** Object:  Table [dbo].[PromoProduct]    Script Date: 06/02/2015 14:41:28 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PromoProduct](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FK_Product_ID] [int] NOT NULL,
	[FK_Promo_ID] [int] NOT NULL,
	[FK_Region_ID] [int] NOT NULL,
 CONSTRAINT [PK_PromoProduct] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Promo]    Script Date: 06/02/2015 14:41:33 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Promo](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Code] [varchar](10) NOT NULL,
	[FK_Translation_Key_Description] [int] NOT NULL,
	[FieldProperty_Filter] [varchar](50) NOT NULL,
	[FieldProperty_Value] [varchar](50) NULL,
 CONSTRAINT [PK_Promo] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO



/****** Object:  Table [dbo].[PromoRegion]    Script Date: 06/02/2015 14:41:26 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PromoRegion](
	[FK_Promo_ID] [int] NULL,
	[FK_Region_ID] [int] NULL
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[PromoSystemType]    Script Date: 06/02/2015 14:41:57 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[PromoSystemType](
	[FK_Promo_ID] [int] NOT NULL,
	[SystemType] [varchar](10) NOT NULL,
 CONSTRAINT [PK_PromoSystemType] PRIMARY KEY CLUSTERED 
(
	[FK_Promo_ID] ASC,
	[SystemType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

insert into promoproduct
select p.id,(select id from promo where code='R'),(select id from region where code='XW') from product p inner join ProductAttribute pa on pa.FK_Product_ID = p.ID and FK_Attribute_ID =8 inner join translation t on t.[key] = pa.FK_Translation_Key_Text 
inner join h3.iq.products.systems on systems.modelsku collate database_default= t.text collate database_default
where recetasystem =1
union
select p.id,(select id from promo where code='R'),(select id from region where code='XW') from product p inner join ProductAttribute pa on pa.FK_Product_ID = p.ID and FK_Attribute_ID =8 inner join translation t on t.[key] = pa.FK_Translation_Key_Text 
inner join h3.iq.products.options on options.optsku collate database_default= t.text collate database_default
where receta  =1
GO

INSERT INTO field 
select distinct screen.id,'Promos(R)',(select top 1 [key] from translation where text='Receta'),'',NULL,'',519,0,1,2,'',0,0,1.5,'','',1,(select top 1 [key] from translation where text='Receta'),'NUMS',1,0,NULL,0,'eq|1'
from screen

GO