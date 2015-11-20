/* HARD DISK ATTRIBUTE IMPORT */

if (select count(*) from translation where text='Interface') = 0 insert into Translation values ((SELECT IDENT_CURRENT('Translation')),'Interface',1,1,'',0,1,'')
if (select count(*) from attribute where code='Interface') = 0 insert into Attribute values ('INTERFACE',(select top 1 id from translation where text='Interface'),1)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Interface')

select distinct pid, optsku ,
technology as code 
 into #tmp 
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'optType' and t.Text = 'HDD') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.id=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
( select * from h3.iq.products.options ) iq1 on iq1.optsku collate database_default=iq2.text collate database_default

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

drop table #tmp
GO
/* HOTPLUG */
if (select count(*) from translation where text='HotPlug') = 0 insert into Translation values ((SELECT IDENT_CURRENT('Translation')),'HotPlug',1,1,'',0,1,'')
if (select count(*) from attribute where code='HotPlug') = 0 insert into Attribute values ('HotPlug',(select top 1 id from translation where text='HotPlug'),0)

DECLARE   @AttributeId  int= (select max(a.id) from Attribute a where code = 'HotPlug')

select distinct pid, optsku ,
case when optfamily like 'HPL%' THEN 'HPL' ELSE
CASE WHEN optfamily like 'NHP%' THEN 'NHP' ELSE 
CASE WHEN optfamily like '%_SC' THEN 'SC' ELSE 'NA' END END END as code 
into #tmp 
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID  from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.FK_Translation_Key_Text  where a.Code = 'optType' and t.Text = 'HDD') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[key]=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.[key]=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
( select * from h3.iq.products.options ) iq1 on iq1.optsku collate database_default=iq2.text collate database_default

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct code into #tmp1 from #tmp

insert into Translation 
select IDENT_CURRENT('translation') + ROW_NUMBER() OVER (ORDER BY code),code,1,1,'',0,1,''
from #tmp1  left outer join translation on text collate database_default = code
where translation.[key] is null and code is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select max([key]) from translation where text  collate database_default=code and FK_Language_ID = 1),1 
from
#tmp where code is not null

drop table #tmp


