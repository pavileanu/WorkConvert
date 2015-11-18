Update Field 
Set VisibleList = 0 
from  Screen s
inner join Field f on f.FK_Screen_ID = s.ID 
inner join Translation t2 on f.FK_Translation_Key_Label = t2.[key] and t2.FK_Language_ID = 1
 where [Code] ='optcpkdto' and f.Property = 'Product.i_Attributes_Code(OnSite)(0)'


 Update Field 
Set width  = 2 ,
WidgetUI = 'TKEY'
from  Screen s
inner join Field f on f.FK_Screen_ID = s.ID 
inner join Translation t2 on f.FK_Translation_Key_Label = t2.[key] and t2.FK_Language_ID = 1
 where [Code] ='optcpkdto' and f.Property = 'Product.i_Attributes_Code(capacity)(0)'

 
 Update Field 
Set width  = 4
from  Screen s
inner join Field f on f.FK_Screen_ID = s.ID 
inner join Translation t2 on f.FK_Translation_Key_Label = t2.[key] and t2.FK_Language_ID = 1
 where [Code] ='optcpkdto' and f.Property = 'Product.i_Attributes_Code(servicedelivery)(0)'







