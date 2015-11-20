delete price 
 from Price p inner join Variant v 
on p.FK_Variant_ID = v.ID 
 where v.FK_Channel_ID_Seller = 40098 and v.FK_Region_ID = 8

 delete Stock 
  from Stock s inner join Variant v 
on s.FK_Variant_ID = v.ID 
 where v.FK_Channel_ID_Seller = 40098 and v.FK_Region_ID = 8

 delete QuoteItem
   from QuoteItem q inner join Variant v 
on q.FK_Variant_ID = v.ID 
 where v.FK_Channel_ID_Seller = 40098 and v.FK_Region_ID = 8

 Delete Variant where FK_Channel_ID_Seller = 40098 and FK_Region_ID = 8