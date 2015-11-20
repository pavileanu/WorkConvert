
update Product set deleted = 0 where sku = '' and IsSystem = 0 and IsOption = 0 and deleted =1 
go

update product 
set deleted = 1 
from Product p 
inner join ProductType pt on p.FK_ProductType_ID = pt.id
where code ='wty' and deleted = 0 and mfrCode = ''
Go

Update Branch
set deleted =1 
from Branch b 
inner join Product p on b.FK_Product_ID = p.ID 
where b.deleted = 0 and p.deleted = 1
Go

 update Branch
 set deleted = 1
 where ID in (select b2.id from Branch b 
inner join Translation t on b.FK_Translation_Key = t.[key] and t.FK_Language_ID = 1
inner join Branch b2 on b2.FK_Branch_ID_Parent = b.ID
inner join Translation t2 on b2.FK_Translation_Key = t2.[key] and t2.FK_Language_ID = 1
left join Branch b3 on b3.FK_Branch_ID_Parent = b2.ID
 where t.[Text] = 'Top Recommended' and b.deleted = 0  and b2.deleted = 0 and t2.[Text] = 'Care Pack' and b3.ID is null)
 GO

Delete	Graft
from Graft g inner join Branch b
on g.FK_Branch_ID_Source = b.ID
where b.deleted = 1
Go

update Quantity 
set deleted = 1
 from Quantity q 
inner join Branch b on q.FK_Branch_ID = b.ID
where b.deleted =1
Go

update Slot
set deleted = 1
 from Slot s
inner join Branch b on s.FK_Branch_ID = b.ID
where b.deleted =1
Go


update Variant
set deleted = 1
from Variant v
inner join Product p on v.FK_Product_ID = p.ID
where p.deleted =1
Go

delete Price 
from Price p inner join Variant v
on p.FK_Variant_ID = v.ID
where v.deleted =1 
Go


delete Stock 
from Stock s inner join Variant v
on s.FK_Variant_ID = v.ID
where v.deleted =1 

Go

UPDATE ProductAttribute 
set deleted = 1 
from Product p inner join ProductAttribute pa 
on p.id	= pa.FK_Product_ID
where P.deleted = 1 and pa.deleted =0

update Quote 
set Reference = 'ToDelete'
 from QuoteItem qi inner join Variant v 
on qi.FK_Variant_ID = v.id	 
inner join Quote q on q.ID = qi.FK_Quote_ID
where v.deleted = 1

delete QuoteItem 
from Quote q inner join QuoteItem qi 
on q.id = qi.FK_Quote_ID
where q.Reference = 'ToDelete'

Delete Quote
where Reference = 'ToDelete'

Go
delete ProductAttribute where deleted = 1
go
delete Variant where deleted = 1
go
delete slot	where deleted = 1
go
delete Quantity where deleted = 1
go
delete Branch where deleted = 1
go
delete Points 
from Product p inner join 
Points po on po.FK_Product_ID = p.ID
where P.deleted = 1
go

delete Product where deleted = 1

GO