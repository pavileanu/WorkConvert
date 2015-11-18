alter table Advert 
 add [SlotTypeCode] [nvarchar](50) NULL
 GO
 alter table Advert 
 Add	[AdRegion_Present] [nvarchar](50) Not NULL DEFault ('XW')
 GO
 alter table Advert 
Add	[AdRegion_Absent] [nvarchar](50) NULL
GO
 alter table Advert 
Add	[Visible] [bit] NOT NULL  DEFAULT (1)
GO

