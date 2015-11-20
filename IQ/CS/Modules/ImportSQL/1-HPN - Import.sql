/* MEMORY ATTRIBUTE IMPORT */

select h.ccdescription,isnull(poetech.ccdescription,raidtech) as poetech,options.ccdescription as options,WLAN.ccDescription as management,poe.ccDescription as POE ,POEPower.ccDescription as PoeW,pristor.ccDescription as PrimaryConnectivity,pristorqty,secstor.ccDescription as SecondaryConnectivity,secstorqty,terstor.ccDescription as UplinkConnectivity,terstorqty,RAID,systems.modelsku
into #iq1
from h3.iq.products.systems 
inner join h3.[channelcentral].products.Hierarchy H on H.upcNUM = systems.modelsku
inner join h3.[channelcentral].products.Hierarchy PriStor on PriStor.upcNUM = systems.pristor
left outer join h3.[channelcentral].products.Hierarchy SecStor on SecStor.upcNUM = systems.secstor
left outer join h3.[channelcentral].products.Hierarchy TerStor on TerStor.upcNUM = systems.terstor
left outer join h3.[channelcentral].products.Hierarchy POE on POE.upcNUM = systems.raid
left outer join h3.[channelcentral].products.Hierarchy POEPower on POEPower.upcNUM = systems.raidcache
left outer join h3.[channelcentral].products.Hierarchy WLAN on WLAN.upcNUM = systems.WLAN
left outer join h3.[channelcentral].products.Hierarchy Options on Options.upcNUM = systems.options
left outer join h3.[channelcentral].products.Hierarchy POETech on POETech.upcNUM = systems.raidtech
where systemtype='HPN'


select distinct pid, #iq1.*  into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'focus' and t.Text = 'HPN') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.id=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
#iq1 on #iq1.modelsku collate database_default=iq2.text collate database_default


/* Management */
if (select count(*) from translation where text='Management') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Management',1,1,'',0,1,'')
if (select count(*) from attribute where code='Management') = 0 insert into Attribute values ('Management',(select top 1 [key] from translation where text='Management'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Management')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct management into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY management),management,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = management collate database_default
WHERE t.text is null and management is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=management collate database_default),1 
from
#tmp where management is not null

/* END MANAGEMENT */
GO

/* Primary Connectivity */
if (select count(*) from translation where text='PriConnectivity') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'PriConnectivity',1,1,'',0,1,'')
if (select count(*) from attribute where code='PriConnectivity') = 0 insert into Attribute values ('PriConnectivity',(select top 1 [key] from translation where text='PriConnectivity'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'PriConnectivity')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct PrimaryConnectivity  into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY PrimaryConnectivity ),PrimaryConnectivity ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = PrimaryConnectivity  collate database_default
WHERE t.text is null and PrimaryConnectivity  is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=PrimaryConnectivity  collate database_default),1 
from
#tmp where PrimaryConnectivity  is not null

/* END Primary Connectivity */

GO
/* Secondary Connectivity */
if (select count(*) from translation where text='SecConnectivity') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'SecConnectivity',1,1,'',0,1,'')
if (select count(*) from attribute where code='SecConnectivity') = 0 insert into Attribute values ('SecConnectivity',(select top 1 [key] from translation where text='SecConnectivity'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'SecConnectivity')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct SecondaryConnectivity  into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY SecondaryConnectivity ),SecondaryConnectivity ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = SecondaryConnectivity  collate database_default
WHERE t.text is null and SecondaryConnectivity  is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=SecondaryConnectivity  collate database_default),1 
from
#tmp where SecondaryConnectivity  is not null

/* END Secondary Connectivity */

GO
/* Uplink Connectivity */
if (select count(*) from translation where text='UpConnectivity') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'UpConnectivity',1,1,'',0,1,'')
if (select count(*) from attribute where code='UpConnectivity') = 0 insert into Attribute values ('UpConnectivity',(select top 1 [key] from translation where text='UpConnectivity'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'UpConnectivity')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct UplinkConnectivity  into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY UplinkConnectivity ),UplinkConnectivity ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = UplinkConnectivity  collate database_default
WHERE t.text is null and UplinkConnectivity  is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=UplinkConnectivity  collate database_default),1 
from
#tmp where UplinkConnectivity  is not null

/* END Uplink Connectivity */

GO

/* Uplink Ports */
if (select count(*) from translation where text='UpPorts') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'UpPorts',1,1,'',0,1,'')
if (select count(*) from attribute where code='UpPorts') = 0 insert into Attribute values ('UpPorts',(select top 1 [key] from translation where text='UpPorts'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'UpPorts')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

insert into productattribute select 
@AttributeId,
pid,terstorqty,null,1 
from
#tmp where terstorqty  is not null

/* END Uplink Ports */

GO
/* Primary Ports */
if (select count(*) from translation where text='PriPorts') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'PriPorts',1,1,'',0,1,'')
if (select count(*) from attribute where code='PriPorts') = 0 insert into Attribute values ('PriPorts',(select top 1 [key] from translation where text='PriPorts'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'PriPorts')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

insert into productattribute select 
@AttributeId,
pid,pristorqty,null,2 
from
#tmp where pristorqty  is not null

/* END Primary Ports */

GO
/* Secondary Ports */
if (select count(*) from translation where text='SecPorts') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'SecPorts',1,1,'',0,1,'')
if (select count(*) from attribute where code='SecPorts') = 0 insert into Attribute values ('SecPorts',(select top 1 [key] from translation where text='SecPorts'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'SecPorts')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

insert into productattribute select 
@AttributeId,
pid,secstorqty,null,2
from
#tmp where secstorqty  is not null

/* END Secondary Ports */

GO

/* Options*/
if (select count(*) from translation where text='Options') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Options',1,1,'',0,1,'')
if (select count(*) from attribute where code='Options') = 0 insert into Attribute values ('Options',(select top 1 [key] from translation where text='Options'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Options')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct options  into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY Options ),Options ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = Options  collate database_default
WHERE t.text is null and Options  is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=Options  collate database_default),1 
from
#tmp where Options  is not null

/* END Options */
GO

/* POE*/
if (select count(*) from translation where text='POE') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'POE',1,1,'',0,1,'')
if (select count(*) from attribute where code='POE') = 0 insert into Attribute values ('POE',(select top 1 [key] from translation where text='POE'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'POE')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct POE  into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY POE ),POE ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = POE  collate database_default
WHERE t.text is null and POE is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=POE  collate database_default),1 
from
#tmp where POE  is not null

/* END POE */

GO
/* POE Power*/
if (select count(*) from translation where text='POE Power') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'POE Power',1,1,'',0,1,'')
if (select count(*) from attribute where code='POEPower') = 0 insert into Attribute values ('POEPower',(select top 1 [key] from translation where text='POE Power'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'POEPower')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct POEW  into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY POEW ),POEW ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = POEW  collate database_default
WHERE t.text is null and POEW is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=POEW  collate database_default),1 
from
#tmp where POEW  is not null

/* END POE Power */

GO
/* POE Tech*/
if (select count(*) from translation where text='POE Tech') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'POE Tech',1,1,'',0,1,'')
if (select count(*) from attribute where code='POETech') = 0 insert into Attribute values ('POETech',(select top 1 [key] from translation where text='POE Tech'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'POETech')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct POETech  into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY POETech ),POETech ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = POETech  collate database_default
WHERE t.text is null and POETech is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=POETech  collate database_default),1 
from
#tmp where POETech  is not null

/* END POE Tech */

GO
/* Category - Derived */
if (select count(*) from translation where text='Category') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Category',1,1,'',0,1,'')
if (select count(*) from attribute where code='Category') = 0 insert into Attribute values ('Category',(select top 1 [key] from translation where text='Category'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Category')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct 
case when ccdescription like '% Switch %' AND ccdescription like '% Managed %' THEN 'Managed Switch' ELSE
case when ccdescription like '% Switch %' AND ccdescription like '% Unmanaged %' THEN 'Unmanaged Switch' ELSE
case when ccdescription like '%Access Point%' THEN 'Wireless Access Point' ELSE
case when ccdescription like '%Mobility Controller%' THEN 'Mobility Controller' ELSE 
case when ccdescription like '%Access Controller%' THEN 'Access Controller' ELSE 
case when ccdescription like '%Walljack%' THEN 'Walljack' ELSE case when ccdescription like '% SWITCH%' THEN 'Switch' ELSE ccdescription END 
END 
END
END
END
END
END as ccdescription into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY ccdescription ),
ccdescription ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = ccdescription  collate database_default
WHERE t.text is null and ccdescription is not null

drop table #tmp1

select pid,case when ccdescription like '% Switch %' AND ccdescription like '% Managed %' THEN 'Managed Switch' ELSE
case when ccdescription like '% Switch %' AND ccdescription like '% Unmanaged %' THEN 'Unmanaged Switch' ELSE
case when ccdescription like '%Access Point%' THEN 'Wireless Access Point' ELSE
case when ccdescription like '%Mobility Controller%' THEN 'Mobility Controller' ELSE 
case when ccdescription like '%Access Controller%' THEN 'Access Controller' ELSE 
case when ccdescription like '%Walljack%' THEN 'Walljack' ELSE case when ccdescription like '% SWITCH%' THEN 'Switch' ELSE ccdescription END 
END 
END
END
END
END
END  as code into #tmp2 from #tmp

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=code collate database_default),1 
from
#tmp2 where code is not null
/* END Category */

drop table #tmp2
drop table #tmp
drop table #iq1
