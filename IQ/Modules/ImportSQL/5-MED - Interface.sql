/* MED ATTRIBUTE IMPORT */

select options.*,h.*  from h3.iq.products.options with (nolock) inner JOIN h3.[channelcentral].products.Hierarchy h with (nolock) ON h.upcNUM = optsku where optsku='C8017A'

if (select count(*) from translation where text='Technology') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Technology',1,1,'',0,1,'')
if (select count(*) from attribute where code='Technology') = 0 insert into Attribute values ('Technology',(select top 1 [key] from translation where text='Technology'),2)

select distinct pid, optsku ,
isnull(translation,Technology) as technology, case optfamily when 'FLASH_MEDIA' then 'Flash' when 'TAPE_MEDIA' then 'Tape' else optFamily end  as MediaType,
case when optfamily = 'FLASH_MEDIA' then  unitqty else speedunitqty end as capacity,
case when optfamily = 'FLASH_MEDIA' then 1 else unitqty end as Quantity
into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID  from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.FK_Translation_Key_Text  where a.Code = 'optType' and t.Text = 'MED') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[key]=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.[key]=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
( select * from h3.iq.products.options left outer join h3.iq.dbo.Abbreviations ab on ab.code=technology) iq1 on iq1.optsku collate database_default=iq2.text collate database_default


DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Technology')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where technology is not null)

select distinct technology into #tmp1 from #tmp

insert into Translation 
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY technology),technology,1,1,'',0,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = technology collate database_default
WHERE t.text is null and technology is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select max([key]) from translation where text  collate database_default=technology and FK_Language_ID = 1),1 
from
#tmp where technology is not null

GO
if (select count(*) from translation where text='MediaType') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'MediaType',1,1,'',0,1,'')
if (select count(*) from attribute where code='MediaType') = 0 insert into Attribute values ('MediaType',(select top 1 [key] from translation where text='MediaType'),2)

declare @AttributeId  int = (select max(a.id) from Attribute a where code = 'MediaType')

select distinct MediaType into #tmp1 from #tmp

insert into Translation 
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY mediatype),mediatype,1,1,'',0,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = mediatype collate database_default
WHERE t.text is null and mediatype is not null

drop table #tmp1

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where mediatype is not null)

insert into productattribute select 
@AttributeId,pid,0,(select max([key]) from translation where text  collate database_default=mediatype and FK_Language_ID = 1),1 
from
#tmp where mediatype is not null and mediatype is not null

/* Capacity */
if (select count(*) from translation where text='Capacity') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Capacity',1,1,'',0,1,'')
if (select count(*) from attribute where code='Capacity') = 0 insert into Attribute values ('Capacity',(select top 1 [key] from translation where text='Capacity'),2)


set @AttributeId = (select max(a.id) from Attribute a where code = 'Capacity')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where capacity is not null)

insert into productattribute select 
@AttributeId,pid,Capacity,null,1 
from
#tmp where Capacity is not null

/* Quantity */
if (select count(*) from translation where text='Quantity') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Quantity',1,1,'',0,1,'')
if (select count(*) from attribute where code='Quantity') = 0 insert into Attribute values ('Quantity',(select top 1 [key] from translation where text='Quantity'),2)


set @AttributeId = (select max(a.id) from Attribute a where code = 'Quantity')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where Quantity is not null)

insert into productattribute select 
@AttributeId,pid,Quantity,null,1 
from
#tmp where Quantity is not null
drop table #tmp