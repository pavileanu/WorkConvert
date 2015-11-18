select * from Attribute where code like '%raid%'

update Attribute set [order]=1 where code='MfrSKU'
update translation set text='Part Number' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='MfrSKU'
update Attribute set [order]=2 where code='FormFactor'
update translation set text='Form Factor' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='formfactor'
update Attribute set [order]=0 where code='CPUsku'
update translation set text='CPU' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='cpuSku'
--update Attribute set [order]=2 where code='Mem?'
update Attribute set [order]=5 where code='Graphics'
update Attribute set [order]=6 where code='Networking'
--update Attribute set [order]=2 where code='disk?'
update Attribute set [order]=8 where code='HDD'
update translation set text='Disk Storage Backplane' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='HDD'
update Attribute set [order]=9 where code='OPT'
--update Attribute set [order]=2 where code='psu?'
update Attribute set [order]=11 where code='ILOhardware'
update translation set text='Management' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='ILOhardware'
update Attribute set [order]=12 where code='warrantyCode'
update translation set text='Warranty' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='warrantycode'
update Attribute set [order]=13 where code='Document Links'

update attribute set [order]=14 where code='Also included'
update attribute set [order]=15 where code='options'
update Translation set text='Other Features' 
from translation inner join attribute on attribute.FK_Translation_key_Name = translation.[key] where Attribute.Code='options'
update attribute set [order]=0 where code='SC'

--HPN
update attribute set [order]=5 where code='Management'
update attribute set [order]=6 where code='PriConnectivity'
update translation set text='Primary Connectivity' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='PriConnectivity'
update attribute set [order]=7 where code='SecConnectivity'
update translation set text='Secondary Connectivity' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='SecConnectivity'
update attribute set [order]=8 where code='POE'
update translation set text='Power Over Ethernet' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='POE'
update attribute set [order]=9 where code='POEPower'
update translation set text='POE Power' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='POEPower'
update attribute set [order]=10 where code='UpConnectivity'
update translation set text='Uplink Connectivity' from translation inner join attribute a on a.FK_Translation_key_Name=translation.[key] where code='UpConnectivity'

--update slottype set fk_translation_key_short 

--update st set fk_translation_key_short = tl.[key]
----select * 
--from iQuote2_RES.dbo.slottype s
--inner join iQuote2_RES.dbo.translation t on t.[key] = s.fk_translation_key_short
--inner join translation tl on tl.text= t.text
--inner join slottype st on st.majorcode = s.majorcode and st.minorcode=s.minorcode
--where s.fk_translation_key_short is not null

delete pa from productattribute pa inner join translation t on t.[key] = pa.FK_Translation_Key_Text where t.text ='' and [group] not in ('hides','shows')

delete pa from ProductAttribute pa inner join Attribute a on a.ID = pa.FK_Attribute_ID 
inner join product p on p.ID = pa.FK_Product_ID 
inner join producttype pt on pt.ID  = p.FK_ProductType_ID 
where a.code='Networking' and pt.code in ('HPN' ,'STO')