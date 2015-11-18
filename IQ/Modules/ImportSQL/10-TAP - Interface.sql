/* TAPE DRIVE ATTRIBUTE IMPORT */

select options.*,h.ccdescription into #iq1 from h3.iq.products.options with (nolock) inner JOIN h3.[channelcentral].products.Hierarchy h with (nolock) ON h.upcNUM = optsku

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
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'optType' and t.Text = 'TAP') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.id=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
( select * from h3.iq.products.options ) iq1 on iq1.optsku collate database_default=iq2.text collate database_default

 select * from h3.iq.products.options where optsku='EH926B'

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
/* TECHNOLOGY */

if (select count(*) from translation where text='Technology') = 0 insert into Translation values ((SELECT IDENT_CURRENT('Translation')),'Technology',1,1,'',0,1,'')
if (select count(*) from attribute where code='Technology') = 0 insert into Attribute values ('TECHNOLOGY',(select top 1 id from translation where text='Technology'),1)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Technology')

select distinct pid, optsku ,
		case when ccdescription  like '%LTO%' then substring(ccdescription,charindex('LTO',ccdescription),charindex(' ',ccdescription,charindex('LTO',ccdescription)+4)-charindex('LTO',ccdescription))  else
			case when ccdescription  like '%Ultrium%' then substring(ccdescription,charindex('Ultrium',ccdescription),charindex(' ',ccdescription,charindex('Ultrium',ccdescription)+8)-charindex('Ultrium',ccdescription)) else 
				case when ccdescription like '%DAT%' then substring(ccdescription,charindex('DAT',ccdescription),charindex(' ',ccdescription,charindex('DAT',ccdescription)+4)-charindex('DAT',ccdescription))  else NULL  end end end as code 
 into #tmp 
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'optType' and t.Text = 'TAP') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.id=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
#iq1 on #iq1.optsku collate database_default=iq2.text collate database_default
select * from #tmp

select * from h3.iq.products.options where optsku='EH926B'
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


/* IN/EXT */

if (select count(*) from translation where text='formfactor') = 0 insert into Translation values ((SELECT IDENT_CURRENT('formfactor')),'formfactor',1,1,'',0,1,'')
if (select count(*) from attribute where code='formfactor') = 0 insert into Attribute values ('formfactor',(select top 1 id from translation where text='formfactor'),1)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'formfactor')

select distinct pid, optsku ,
		case when ccdescription  like '%EXTERNAL%' then 'EXTERNAL' else
		case when ccdescription  like '%MOUNT%' then 'MOUNT' else
			'INTERNAL' end end as code
 into #tmp 
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'optType' and t.Text = 'TAP') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.id=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
#iq1 on #iq1.optsku collate database_default=iq2.text collate database_default
select * from #tmp

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct code into #tmp1 from #tmp

insert into Translation 
select IDENT_CURRENT('translation') + ROW_NUMBER() OVER (ORDER BY code),code,1,1,'',0,1,''
from #tmp1 left outer join translation on text collate database_default = code
where translation.[key] is null and code is not null

drop table #tmp1

insert into productattribute 
select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text  collate database_default=code and FK_Language_ID = 1),1 
from
#tmp where code is not null

drop table #tmp
