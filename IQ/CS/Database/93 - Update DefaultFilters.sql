
declare @responseKey int
select @responseKey = FK_Translation_Key_Title from Response where ResponseDefault = 1 and mfrCode = 'HPE'
update Field 
Set  DefaultFilterValues = RTRIM('EQ|' +  cast( @responseKey as varchar (10)))
 from Screen  s 
inner join Field f on f.FK_Screen_ID = s.ID
where s.Code = 'optCPK' AND F.Property LIKE '%Response%'

declare @serviceLevelKey int
select @serviceLevelKey = FK_Translation_Key_Title from ServiceType where ServiceTypeDefault = 1 and mfrCode = 'HPE'
select  RTRIM('EQ|' +  cast( @serviceLevelKey as varchar (10)))
update Field 
Set  DefaultFilterValues = RTRIM('EQ|' +  cast( @serviceLevelKey as varchar (10)))
 from Screen  s 
inner join Field f on f.FK_Screen_ID = s.ID
where s.Code = 'optCPK' AND F.Property LIKE '%serviceLevel%'


update Translation
set [Order] = 30 
 where [Text] in ('CDMR') and deleted = 0 

  
update Translation
set [Order] = 15 
 where [Text] in ('DMR') and deleted = 0 

 update Translation
set [Order] = 0 
 where [Text] in ('No DMR') and deleted = 0 


