
--update Product 
--set sku = t.[text]
--From product p inner join productattribute pa on pa.FK_Product_ID = p.ID 
--inner join Attribute a on pa.FK_Attribute_ID = a.ID
--inner join Translation t on t.[key] = pa.FK_Translation_Key_Text
--where p.sku ='' and a.Code = 'mfrSKU'
--GO


GO
update Variant 
set deleted = 1
 from Variant v inner join Product p on v.FK_Product_ID = p.ID
where v.deleted = 0 and p.deleted =1 
GO






