 Update Field 
 Set Fk_InputType_ID = 519,
 WidgetUI = 'TKEY'
  from  Screen s
inner join Field f on f.FK_Screen_ID = s.ID 
inner join Translation t2 on f.FK_Translation_Key_Label = t2.[key] and t2.FK_Language_ID = 1
 where [Code] ='optcpk' and f.Property = 'Product.i_Attributes_Code(capacity)(0)'