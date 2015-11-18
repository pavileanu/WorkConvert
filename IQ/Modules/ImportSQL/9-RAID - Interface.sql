/* RAID ATTRIBUTE IMPORT */
select * into #iq1 from h3.iq.products.options with (nolock) inner JOIN h3.[channelcentral].products.Hierarchy h with (nolock) ON h.upcNUM = optsku  inner join h3.iq.[products].[OptRAIDprops]  rp with (nolock) ON ccDescription LIKE '%'+rp.RAIDfamily+'%' AND OptFamily='RAID_CONTROLLERS' inner join h3.[iq].products.optTypes as OT with (nolock) on OT.optTypeCode=optType 
 
if (select count(*) from translation where text='Technology') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Technology',1,1,'',0,1,'')
if (select count(*) from attribute where code='Technology') = 0 insert into Attribute values ('Technology',(select top 1 [key] from translation where text='Technology'),2)

select distinct pid, optsku,isnull(translation,technology) as code
into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID  from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.FK_Translation_Key_Text  where a.Code = 'optType' and (t.Text like 'PCI%' or t.text like 'MOD%')) dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[key]=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.[key]=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
( select * from h3.iq.products.options left outer join h3.iq.dbo.Abbreviations ab on ab.code=technology) iq1 on iq1.optsku collate database_default=iq2.text collate database_default

DECLARE @AttributeId int = (select min(a.id) from Attribute a where code = 'Technology')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where code is not null)

select distinct code into #tmp1 from #tmp

insert into Translation 
select (select MAX([key]) from translation) + ROW_NUMBER() OVER (ORDER BY code),code,1,1,'',0,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = code collate database_default
WHERE t.text is null and code is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select max([key]) from translation where text  collate database_default=code and FK_Language_ID = 1),1 
from
#tmp where code is not null

drop table #tmp
GO

if (select count(*) from translation where text='IntPorts') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'IntPorts',1,1,'',0,1,'')
if (select count(*) from attribute where code='IntPorts') = 0 insert into Attribute values ('IntPorts',(select top 1 [key] from translation where text='IntPorts'),2)
if (select count(*) from translation where text='ExtPorts') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'ExtPorts',1,1,'',0,1,'')
if (select count(*) from attribute where code='ExtPorts') = 0 insert into Attribute values ('ExtPorts',(select top 1 [key] from translation where text='ExtPorts'),2)

select distinct pid, optsku ,
intPort as code,
extPort as code2
into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID  from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.FK_Translation_Key_Text  where a.Code = 'optType' and (t.Text like 'PCI%' or t.text like 'MOD%') ) dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[key]=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.[key]=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
#iq1 on #iq1.optsku collate database_default=iq2.text collate database_default

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'IntPorts')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where code is not null)

insert into productattribute select 
@AttributeId ,
pid,code,null,1 
from
#tmp where code is not null


SET @AttributeId = (select max(a.id) from Attribute a where code = 'ExtPorts')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where code2 is not null)

insert into productattribute select 
@attributeid,
pid,code2,null,1 
from
#tmp where code2 is not null

drop table #tmp
GO

--1x ,4x,8x,etc

if (select count(*) from translation where text='CardSpeed') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'CardSpeed',1,1,'',0,1,'')
if (select count(*) from attribute where code='CardSpeed') = 0 insert into Attribute values ('CardSpeed',(select top 1 [key] from translation where text='CardSpeed'),2)

select distinct pid, optsku ,
OptTypeName
into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID  from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.FK_Translation_Key_Text  where a.Code = 'optType' and (t.Text like 'PCI%' or t.text like 'MOD%')) dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[key]=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.[key]=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
#iq1 on #iq1.optsku collate database_default=iq2.text collate database_default

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'CardSpeed')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where OptTypeName is not null)

insert into productattribute select 
@AttributeId,
pid,0,(select max([key]) from translation where text  collate database_default=OptTypeName and FK_Language_ID = 1),1 
from
#tmp where OptTypeName is not null

drop table #tmp