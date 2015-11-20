ALTER TABLE Channel 
Add [Universal] [bit] NOT NULL DEFAULT (0)
GO
ALTER TABLE Channel 
Add orderemail [nvarchar] (100) NOT NULL DEFAULT ('')
GO

update Channel 
Set Universal = 1 
FROM  H3.ChannelCentral.customers.Host_Properties CCHP
LEFT JOIN Channel
ON   CCHP.HostID COLLATE DATABASE_DEFAULT = Channel.Code COLLATE DATABASE_DEFAULT
LEFT JOIN Region
ON   Channel.FK_Region_ID = Region.ID
WHERE  CCHP.HostID LIKE 'MHP%' AND CCHP.Universal=1 
GO