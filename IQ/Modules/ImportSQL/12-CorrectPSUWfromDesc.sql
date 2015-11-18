select b.id as branchid,p.id,case when charindex('W ',dtp.text) > 0 then 
case when  charindex('W',dtp.text) > charindex(' ',dtp.text) then
substring(dtp.text,len(dtp.text)-charindex(' ',reverse(dtp.text), charindex(' W',reverse(dtp.text))+1)+2, 
charindex(' ',dtp.text, len(dtp.text)-charindex(' ',reverse(dtp.text), charindex(' W',reverse(dtp.text))+1)+2)- (len(dtp.text)-charindex(' ',reverse(dtp.text), charindex(' W',reverse(dtp.text))+1)+3))
 else substring(dtp.text,0,charindex('W ',dtp.text)) end else null end as newslots,
dtp.text,wt.numslots as oldslots
into #tmp
 from product p  
inner join productattribute pa on pa.fk_product_id=p.id 
inner join attribute a on a.id=pa.fk_attribute_id 
inner join (select ap.FK_Product_ID from productattribute ap inner join attribute a on a.id=ap.FK_Attribute_ID inner join translation t on t.[key]=ap.fk_translation_key_text  where a.Code = 'optType' and t.Text = 'PSU') dd on p.id=dd.FK_Product_ID inner join translation t on pa.fk_translation_key_text = t.[key]
inner join branch b on b.FK_Product_ID = p.id
inner join translation tp on tp.[KEY]=b.FK_Translation_key  and tp.fk_language_id=1
inner join ProductAttribute sku on sku.FK_Product_ID = p.id and sku.FK_Attribute_ID = (select top 1 id from Attribute where code='mfrSku')
inner join ProductAttribute ds on ds.FK_Product_ID = p.id and ds.FK_Attribute_ID = (select top 1 id from Attribute where code='desc')
inner join translation dtp on dtp.[KEY]=ds.FK_Translation_key_text and dtp.fk_language_id=1
inner join slot wt on wt.fk_branch_id = b.id and wt.FK_SlotType_ID  = (select top 1 id from slottype where minorcode='W')
inner join Translation tsku on tsku.id=sku.FK_Translation_Key_Text 
where code='mfrSku'
and t.text <> dtp.text


begin tran
update slot set numslots = newslots 
from slot inner join #tmp on slot.FK_Branch_id = #tmp.branchid  and slot.FK_SlotType_ID  = (select top 1 id from slottype where minorcode='W')
where newslots is not null
rollback tran

begin tran
update productattribute set numericvalue = newslots 
 from productattribute inner join #tmp on productattribute.fk_product_id = #tmp.id and productattribute.fk_attribute_id  = (select top 1 id from attribute where code='capacity')
where newslots is not null
rollback tran


drop table #tmp