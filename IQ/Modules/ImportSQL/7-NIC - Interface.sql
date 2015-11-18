/* NIC ATTRIBUTE IMPORT */

select options.*,h.ccdescription into #iq1 from h3.iq.products.options with (nolock) inner JOIN h3.[channelcentral].products.Hierarchy h with (nolock) ON h.upcNUM = optsku

if (select count(*) from translation where text='Interface') = 0 insert into Translation values ((SELECT IDENT_CURRENT('Translation')),'Interface',1,1,'',0,1,'')
if (select count(*) from attribute where code='Interface') = 0 insert into Attribute values ('INTERFACE',(select top 1 id from translation where text='Interface'),1)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Interface')

select distinct pid, optsku ,
technology as code,CASE WHEN ccdescription like '%10GB%' or ccdescription like '%10 Gb%' then 10 else case when ccdescription like '%20GB%' then 20 else case when ccdescription like '%1Gb%' or ccdescription like '% Gigabit %' then 1 else case when ccdescription like '%40GB%' then 40 else 
CASE WHEN technology like '%10G%' then 10 else case when technology like '%20G%' then 20 else case when technology like '%1G%' then 1 else  case when technology like '%40G%' then 40 else 
case when provisionrules like '%1000T%' then 1 else null end 
 end end end end end end end end as speed,case when ccdescription like '%dual%' or ccdescription like '%2-port%' or ccdescription like '% 2p %' then 2 else case when ccdescription like '%4-port%' or ccdescription like '% 4p %' then 4 else 1 end end as noports
 into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'optFamily' and t.Text like '%NETWORK%') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.id=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
#iq1 on #iq1.optsku collate database_default=iq2.text collate database_default

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct code into #tmp1 from #tmp

insert into Translation 
select IDENT_CURRENT('translation') + ROW_NUMBER() OVER (ORDER BY code),code,1,1,'',0,1,''
from #tmp1 left outer join translation on text collate database_default = code
where translation.[key] is null and code is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text  collate database_default=code and FK_Language_ID = 1),1 
from
#tmp where code is not null

--drop table #tmp

/* SPEED */

if (select count(*) from translation where text='Speed') = 0 insert into Translation values ((SELECT IDENT_CURRENT('Speed')),'Speed',1,1,'',0,1,'')
if (select count(*) from attribute where code='Speed') = 0 insert into Attribute values ('Speed',(select top 1 id from translation where text='Speed'),1)

SET @AttributeId = (select max(a.id) from Attribute a where code = 'Speed')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

insert into productattribute select 
@AttributeId,
pid,speed,null,1 
from
#tmp where speed is not null

/* NO PORTS */

if (select count(*) from translation where text='intports') = 0 insert into Translation values ((SELECT MAX([key]) from translation)+1,'intports',1,1,'',0,1,'')
if (select count(*) from attribute where code='intports') = 0 insert into Attribute values ('intports',(select top 1 id from translation where text='intports'),1)

SET @AttributeId = (select max(a.id) from Attribute a where code = 'intports')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

insert into productattribute 
select 
@AttributeId,
pid,noports,null,1 
from
#tmp where noports is not null

drop table #tmp
