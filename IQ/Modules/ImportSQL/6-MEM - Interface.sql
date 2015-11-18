/* MEMORY ATTRIBUTE IMPORT */

select options.*,h.ccdescription into #iq1 from h3.iq.products.options with (nolock) inner JOIN h3.[channelcentral].products.Hierarchy h with (nolock) ON h.upcNUM = optsku

if (select count(*) from translation where text='Low Voltage') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Low Voltage',1,1,'',0,1,'')
if (select count(*) from attribute where code='LV') = 0 insert into Attribute values ('LV',(select top 1 [key] from translation where text='Low Voltage'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'LV')

select distinct pid, optsku , lowenergy as code into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'optType' and t.Text = 'MEM') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.id=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
#iq1 on #iq1.optsku collate database_default=iq2.text collate database_default

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

insert into productattribute select 
@AttributeId,
pid,code,null,1 
from
#tmp where code is not null

drop table #tmp
drop table #iq1