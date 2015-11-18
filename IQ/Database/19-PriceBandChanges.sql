alter table account add priceband nvarchar(32) not null default ''
GO
update Account set priceband=HostAccountNum
GO
alter table price alter column fk_channel_id_buyer int null
GO



insert into [gk].[Name]
            ([Name]
      ,[Example]
      ,[Required]
      ,[RegEx]
      ,[MinLength]
      ,[MaxLength]
      ,[Notes])
      values('cPriceBand','A, Gold, or 12929',0,'',1,100,'Which set of pricing a customer should see/query your web service for - if supplied, this overrides the cAccountNum')