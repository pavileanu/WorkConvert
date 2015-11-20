alter table Advert 
 add [SlotTypeCode] [nvarchar](50) NULL

alter table Advert 
 Add	[FK_Region_Id_Present] int Not NULL DEFault (1)
 GO
  alter table Advert 
Add	[FK_Region_Id_Absent] int NULL
GO

alter table Advert Add [visible] bit NOT NULL Default (1)
