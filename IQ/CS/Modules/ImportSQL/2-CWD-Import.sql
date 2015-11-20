/* SWD IMPORT */

select *
into #iq1
from h3.iq.products.systems 
inner join h3.[channelcentral].products.Hierarchy H on H.upcNUM = systems.modelsku
where systemtype='SWD'

select distinct pid, #iq1.*  into #tmp
 from
(select t.text,p.id as pid
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'focus' and t.Text = 'SWD') dd on p.id=dd.FK_Product_ID 
inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.id=b.FK_Translation_key 
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku') iq2
inner join
#iq1 on #iq1.modelsku collate database_default=iq2.text collate database_default

select 
case when ccdescription like '%Storeonce%' or [long desc] like '%Disk to Disk%' or [long desc] like '% D2%'   then 'StoreOnce / D2D' else
case when ccdescription like '%Storeeasy%' or [long desc] like '%Network Storage System%' then 'StoreEasy / NAS' else
case when ccdescription like '%StoreVirtual%' then 'StoreVirtual / SAN' else
case when [long desc] like '%tape%' or  [long desc] like '%LTO%' or  [long desc] like '%Ultrium%' or  [long desc] like '%autoloader%' or ccdescription like '%StoreEver%' then 'StoreEver / Tape' else
case when [long desc] like '%Enclosure%' or [long desc] like '%HDD%' or [long desc] like '%Disk storage system%' or [long desc] like '%SAS Storage System%' then 'Disk Expansion / DAS' else
case when [long desc] like '%SAN%' or [long desc] like '%Modular smart%' or [long desc] like '%MSA%' then 'Storevirtual / SAN' else
ccdescription 
end 
end 
end
end
end
end,
* from #tmp whERE ACTIVE=1 and eol=0

/* Category - Derived */
if (select count(*) from translation where text='Category') = 0 insert into Translation values ((SELECT MAX([Key])+1 FROM translation),'Category',1,1,'',0,1,'')
if (select count(*) from attribute where code='Category') = 0 insert into Attribute values ('Category',(select top 1 [key] from translation where text='Category'),2)

DECLARE @AttributeId int = (select max(a.id) from Attribute a where code = 'Category')

delete from productattribute where fk_attribute_id=@AttributeId and fk_product_id in (select pid from #tmp)

select distinct 
case when ccdescription like '%Storeonce%' or [long desc] like '%Disk to Disk%' or [long desc] like '% D2%'   then 'StoreOnce / D2D' else
case when ccdescription like '%Storeeasy%' or [long desc] like '%Network Storage System%' then 'StoreEasy / NAS' else
case when ccdescription like '%StoreVirtual%' then 'StoreVirtual / SAN' else
case when [long desc] like '%tape%' or  [long desc] like '%LTO%' or  [long desc] like '%Ultrium%' or  [long desc] like '%autoloader%' or ccdescription like '%StoreEver%' then 'StoreEver / Tape' else
case when [long desc] like '%Enclosure%' or [long desc] like '%HDD%' or [long desc] like '%Disk storage system%' or [long desc] like '%SAS Storage System%' then 'Disk Expansion / DAS' else
case when [long desc] like '%SAN%' or [long desc] like '%Modular smart%' or [long desc] like '%MSA%' then 'Storevirtual / SAN' else
ccdescription 
end 
end 
end
end
end
end as ccdescription into #tmp1 from #tmp

insert into Translation ([key],[text],FK_Language_ID,[order],[group])
select (select max([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY ccdescription ),
ccdescription ,1,1,''
from #tmp1 LEFT OUTER JOIN Translation t on t.Text = ccdescription  collate database_default
WHERE t.text is null and ccdescription is not null

drop table #tmp1

select pid,
case when ccdescription like '%Storeonce%' or [long desc] like '%Disk to Disk%' or [long desc] like '% D2%'   then 'StoreOnce / D2D' else
case when ccdescription like '%Storeeasy%' or [long desc] like '%Network Storage System%' then 'StoreEasy / NAS' else
case when ccdescription like '%StoreVirtual%' then 'StoreVirtual / SAN' else
case when [long desc] like '%tape%' or  [long desc] like '%LTO%' or  [long desc] like '%Ultrium%' or  [long desc] like '%autoloader%' or ccdescription like '%StoreEver%' then 'StoreEver / Tape' else
case when [long desc] like '%Enclosure%' or [long desc] like '%HDD%' or [long desc] like '%Disk storage system%' or [long desc] like '%SAS Storage System%' then 'Disk Expansion / DAS' else
case when [long desc] like '%SAN%' or [long desc] like '%Modular smart%' or [long desc] like '%MSA%' then 'Storevirtual / SAN' else
ccdescription 
end 
end 
end
end
end
end  as code into #tmp2 from #tmp

insert into productattribute select 
@AttributeId,
pid,0,(select top 1 [key] from translation where text=code collate database_default),1 
from
#tmp2 where code is not null
/* END Category */


drop table #tmp
drop table #iq1
