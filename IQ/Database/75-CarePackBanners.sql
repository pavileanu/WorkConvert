if not exists (select * from [Advert] where [Name] = 'HPIDUMMY')
begin

-- Switch the Care Pack banner to HPE
update	[Advert]
set		[mfrCode] = 'HPE'
where	[Name] = 'CarePack'


-- Create a dummy HPI banner (to prove functionality)
insert into	[Advert]
(
       [FK_Campaign_ID]
      ,[Name]
      ,[URL]
      ,[Type]
      ,[BasketProductBelowAbsent]
      ,[BasketProductBelowPresent]
      ,[FK_ProdType_Present]
      ,[FK_ProdType_Absent]
      ,[FK_SlotType_ID]
      ,[FillThresholdPercent]
      ,[imageurl]
      ,[imagewide]
      ,[SlotTypeCode]
      ,[FK_Region_Id_Present]
      ,[FK_Region_Id_Absent]
      ,[visible]
      ,[mfrCode]
)
values
(
		 1006
		,'HPIDUMMY'
		,null
		,1
		,null
		,100
		,1
		,1
		,288
		,10
		,'http://www.channelcentral.net/images/pimgpsh_fullsize_distr.jpg'
		,0
		,null
		,1
		,null
		,1
		,'HPI'
)

end