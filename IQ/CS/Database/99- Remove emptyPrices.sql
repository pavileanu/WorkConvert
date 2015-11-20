Delete Price 
where FK_Variant_ID in (select distinct v.ID from Variant v inner join Price pr on pr.FK_Variant_ID = v.ID
inner join Product p on p.id = v.FK_Product_ID
inner join ProductType pt  on pt.ID = p.FK_ProductType_ID
where Price <= 0  and pt.code in('svc','edu','wty','sup' )
)

GO
update Variant 
set deleted = 1
 where ID not in (select distinct fk_Variant_id from Price) and deleted = 0

 GO

delete stock 
from Stock s inner join Variant v  on s.FK_Variant_ID = v.ID
where v.deleted =1

GO
 update Quote  set Reference = 'toDelete'
   from Variant v inner join QuoteItem qi on v.ID = qi.FK_Variant_ID
  inner join Quote q on q.ID = qi.FK_Quote_ID
  and v.deleted =1

Go
  delete QuoteItem
  from QuoteItem qi inner join Quote q on q.ID = qi.FK_Quote_ID
  where q.Reference = 'toDelete'
Go
  delete Quote where Reference = 'toDelete'
GO
  delete Variant where deleted = 1
Go
