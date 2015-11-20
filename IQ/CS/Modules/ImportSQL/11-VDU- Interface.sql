/* VDU ATTRIBUTE IMPORT */

select options.*,h.*  from h3.iq.products.options with (nolock) inner JOIN h3.[channelcentral].products.Hierarchy h with (nolock) ON h.upcNUM = optsku where optsku='C9V75AA'

if (select count(*) from translation where text='Resolution') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Resolution',1,1,'',0,1,'')
if (select count(*) from attribute where code='Resolution') = 0 insert into Attribute values ('Resolution',(select top 1 [key] from translation where text='Resolution'),2)

select distinct pid, optsku ,
VDUres as Resolution
into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID  from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.FK_Translation_Key_Text  where a.Code = 'optType' and t.Text = 'VDU') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[key]=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.[key]=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
( select * from h3.iq.products.options left outer join h3.iq.dbo.Abbreviations ab on ab.code=technology) iq1 on iq1.optsku collate database_default=iq2.text collate database_default


DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Resolution')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where Resolution is not null)

select distinct Resolution into #tmp1 from #tmp

insert into Translation 
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY Resolution),Resolution,1,1,'',0,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = Resolution collate database_default
WHERE t.text is null and Resolution is not null

drop table #tmp1

insert into productattribute select 
@AttributeId,
pid,0,(select max([key]) from translation where text  collate database_default=Resolution and FK_Language_ID = 1),1 
from
#tmp where Resolution is not null

drop table #tmp