declare @cpkScreen int 
declare @cpkScreendto int 
Select @cpkScreen = ID from Screen where [Code] ='optcpk' 
Select @cpkScreendto = ID from Screen where [Code] ='optcpkdto' 



insert into Field
(FK_Screen_ID,Property,FK_Translation_Key_Label,HelpText,FK_InputType_ID,[length],[Order],Width,VisibleList,VisiblePage,Height,DefaultFilter,DefaultSort,[Priority],FK_Translation_Key_WidgetGroup,WidgetUI,CanUserSelect,Grows,DefaultFilterValues,FilterVisible,InvertFilterOrder,DefaultValue)
Select @cpkScreendto,Property,FK_Translation_Key_Label,HelpText,FK_InputType_ID,[length],[Order],Width,VisibleList,VisiblePage,Height,DefaultFilter,DefaultSort,[Priority],FK_Translation_Key_WidgetGroup,WidgetUI,CanUserSelect,Grows,DefaultFilterValues,FilterVisible,InvertFilterOrder,DefaultValue
from Field where FK_Screen_ID = @cpkScreen and Property ='Product.i_Attributes_Code(Response)(0)'


 Update  Translation
 Set [Order] = 50
   from Field f
 inner join Translation t on f.FK_Translation_Key_WidgetGroup = t.[key]
    where FK_Screen_ID in (@cpkScreen,@cpkScreendto) and t.[Text] = 'Service Delivery'



 Update  Translation
 Set [Order] = 60
   from Field f
 inner join Translation t on f.FK_Translation_Key_WidgetGroup = t.[key]
    where FK_Screen_ID in (@cpkScreen,@cpkScreendto) and t.[Text] = 'Enhanced Features'