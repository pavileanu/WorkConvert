/* MAN ATTRIBUTE IMPORT */

select options.*,h.* into #iq1 from h3.iq.products.options with (nolock) inner JOIN h3.[channelcentral].products.Hierarchy h with (nolock) ON h.upcNUM = optsku

if (select count(*) from translation where text='Technology') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Technology',1,1,'',0,1,'')
if (select count(*) from attribute where code='Technology') = 0 insert into Attribute values ('Technology',(select top 1 [key] from translation where text='Technology'),2)

select distinct pid, optsku ,
Translation as code
into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID  from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.FK_Translation_Key_Text  where a.Code = 'optType' and t.Text like 'MAN%') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[key]=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.[key]=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
( select * from h3.iq.products.options inner join h3.iq.dbo.Abbreviations ab on ab.code=technology) iq1 on iq1.optsku collate database_default=iq2.text collate database_default

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Technology')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct code into #tmp1 from #tmp

insert into Translation 
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY code),code,1,1,'',0,1,''
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
if (select count(*) from translation where text='Electronic') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Electronic',1,1,'',0,1,'')
if (select count(*) from attribute where code='Electronic') = 0 insert into Attribute values ('Electronic',(select top 1 [key] from translation where text='Electronic'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Electronic')

select distinct  pid, optsku ,
Electronic as code,ccdescription,
case when ccdescription like '%1 year%' or ccdescription like '%1yr%' then 1 else case when ccdescription like '%2 year%' or ccdescription like '%2yr%' then 2 else case when ccdescription like '%3 year%' or ccdescription like '%3yr%' then 3 else case when ccdescription like '%4 year%' or ccdescription like '%4yr%' then 4 else null end end end end as duration
,case when ccdescription like '%No Media%' then 0 else 1 end as media
into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID  from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.FK_Translation_Key_Text  where a.Code = 'optType' and t.Text like 'MAN%') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[key]=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.[key]=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
 #iq1 on #iq1.optsku collate database_default=iq2.text collate database_default
 
delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where code is not null)

insert into productattribute select 
@AttributeId,pid,code,(select max([key]) from translation where text  collate database_default='Electronic' and FK_Language_ID = 1),1 
from
#tmp where code is not null

if (select count(*) from translation where text='Duration') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Duration',1,1,'',0,1,'')
if (select count(*) from attribute where code='Duration') = 0 insert into Attribute values ('Duration',(select top 1 [key] from translation where text='Duration'),2)


set @AttributeId = (select max(a.id) from Attribute a where code = 'Duration')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where duration is not null)

insert into productattribute select 
@AttributeId,pid,duration,null,1 
from
#tmp where duration is not null

if (select count(*) from translation where text='MediaInc') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'MediaInc',1,1,'',0,1,'')
if (select count(*) from translation where text='With Media') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'With Media',1,1,'',0,1,'')
if (select count(*) from attribute where code='MediaInc') = 0 insert into Attribute values ('MediaInc',(select top 1 [key] from translation where text='MediaInc'),2)

set @AttributeId = (select max(a.id) from Attribute a where code = 'MediaInc')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp where media is not null)

insert into productattribute select 
@AttributeId,pid,media,(select max([key]) from translation where text  collate database_default='With Media' and FK_Language_ID = 1),1 
from
#tmp where media is not null


drop table #tmp