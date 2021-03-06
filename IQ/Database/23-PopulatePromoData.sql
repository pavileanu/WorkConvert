ALTER TABLE PromoSystemType ALTER COLUMN SystemType varchar(30) not null
GO

truncate table promo
truncate table promoregion
truncate table promosystemtype



GO
INSERT [dbo].[Promo] ( [Code], [FK_Translation_Key_Description], [FieldProperty_Filter], [FieldProperty_Value]) VALUES ( N'R', 77601, N'Promos(R)', N'1')
GO
INSERT [dbo].[Promo] ( [Code], [FK_Translation_Key_Description], [FieldProperty_Filter], [FieldProperty_Value]) VALUES ( N'F', 235942, N'Promos(F)', N'1')
GO
INSERT [dbo].[Promo] ( [Code], [FK_Translation_Key_Description], [FieldProperty_Filter], [FieldProperty_Value]) VALUES ( N'SB', 1876, N'Product.i_Attributes_Code(SC)(0)', N'1876')
GO
INSERT [dbo].[Promo] ([Code], [FK_Translation_Key_Description], [FieldProperty_Filter], [FieldProperty_Value]) VALUES ( N'TV', 1875, N'Product.i_Attributes_Code(SC)(0)', N'1875')
GO

INSERT INTO PromoRegion values ((select id from promo where code='TV'),(select id from region where code='EMEA'))
INSERT INTO PromoRegion values ((select id from promo where code='R'),(select id from region where code='MCA1'))
INSERT INTO PromoRegion values ((select id from promo where code='R'),(select id from region where code='MCA2'))
INSERT INTO PromoRegion values ((select id from promo where code='F'),(select id from region where code='MCA2'))
INSERT INTO PromoRegion values ((select id from promo where code='F'),(select id from region where code='MCA1'))

INSERT INTO PromoRegion values ((select id from promo where code='R'),(select id from region where code='US'))
INSERT INTO PromoRegion values ((select id from promo where code='F'),(select id from region where code='US'))

INSERT INTO PromoSystemType  select id,'HP Networking' from promo where code='TV'
INSERT INTO PromoSystemType  select id,'HP Storage' from promo where code='TV'

INSERT INTO PromoSystemType  select id,'HP Storage' from promo where code='F'
INSERT INTO PromoSystemType  select id,'Servers' from promo where code='F'
INSERT INTO PromoSystemType  select id,'Servers' from promo where code='R'
INSERT INTO PromoSystemType  select id,'Servers' from promo where code='TV'
INSERT INTO PromoSystemType  select id,'Servers' from promo where code='SB'
INSERT INTO PromoSystemType  select id,'HP Storage' from promo where code='R'

select * from PromoSystemType 