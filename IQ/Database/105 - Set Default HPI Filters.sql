
declare @responseKey int
select @responseKey = FK_Translation_Key_Title from Response where ResponseDefault = 1 and mfrCode = 'HPI'
update Field 
Set  DefaultFilterValues = RTRIM('EQ|' +  cast( @responseKey as varchar (10))),
DefaultFilter ='EQ'
 from Screen  s 
inner join Field f on f.FK_Screen_ID = s.ID
where s.Code = 'optCPKdto' AND F.Property LIKE '%Response%'

declare @serviceLevelKey int
select @serviceLevelKey = FK_Translation_Key_Title from ServiceType where ServiceTypeDefault = 1 and mfrCode = 'HPI'
select  RTRIM('EQ|' +  cast( @serviceLevelKey as varchar (10)))
update Field 
Set  DefaultFilterValues = RTRIM('EQ|' +  cast( @serviceLevelKey as varchar (10))),
DefaultFilter ='EQ'

 from Screen  s 
inner join Field f on f.FK_Screen_ID = s.ID
where s.Code = 'optCPKdto' AND F.Property LIKE '%servicedelivery%'




--select * from Field f 
--inner join Screen s on f.FK_Screen_ID = s.ID
-- where  s.Code in ( 'optCPKdto', 'optCPK')