
declare @widgetGroupKey		int
declare @labelKey			int

-- Server HMC: switch Duration/Response/Service Level/Options fields to "Sausage" view and fix filter column order
update	 [Field]
set		 [FK_InputType_ID] = 531
where	 [FK_Screen_ID] = 786
and		 [Property] = 'Product.i_Attributes_Code(capacity)(0)'


update	 [Field]
set		 [FK_InputType_ID] = 531
where	 [FK_Screen_ID] = 786
and		 [Property] = 'Product.i_Attributes_Code(Response)(0)'

select	 @widgetGroupKey = [FK_Translation_Key_WidgetGroup]
from	 [Field]
where	 [FK_Screen_ID] = 786
and		 [Property] = 'Product.i_Attributes_Code(Response)(0)'

update	 [Translation]
set		 [Order] = 40
where	 [Key] = @widgetGroupKey


update	 [Field]
set		 [FK_InputType_ID] = 531
where	 [FK_Screen_ID] = 786
and		 [Property] = 'Product.i_Attributes_Code(servicelevel)(0)'

select	 @widgetGroupKey = [FK_Translation_Key_WidgetGroup]
from	 [Field]
where	 [FK_Screen_ID] = 786
and		 [Property] = 'Product.i_Attributes_Code(servicelevel)(0)'

update	 [Translation]
set		 [Order] = 50
where	 [Key] = @widgetGroupKey


update	 [Field]
set		 [FK_InputType_ID] = 531
where	 [FK_Screen_ID] = 786
and		 [Property] = 'Product.i_Attributes_Code(DMR_ISS)(0)'

select	 @widgetGroupKey = [FK_Translation_Key_WidgetGroup]
from	 [Field]
where	 [FK_Screen_ID] = 786
and		 [Property] = 'Product.i_Attributes_Code(DMR_ISS)(0)'

update	 [Translation]
set		 [Order] = 60
where	 [Key] = @widgetGroupKey


-- Desktop HMC: Fix filter column order and make sure translations exist
select	 @widgetGroupKey = [FK_Translation_Key_WidgetGroup]
		,@labelKey = [FK_Translation_Key_Label]
from	 [Field]
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(servicedelivery)(0)'

update	 [Translation]
set		 [Order] = 10
where	 [Key] = @widgetGroupKey


select	 @widgetGroupKey = [FK_Translation_Key_WidgetGroup]
from	 [Field]
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(Travel)(0)'

update	 [Translation]
set		 [Order] = 20
where	 [Key] = @widgetGroupKey


select	 @widgetGroupKey = [FK_Translation_Key_WidgetGroup]
		,@labelKey = [FK_Translation_Key_Label]
from	 [Field]
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(capacity)(0)'

update	 [Translation]
set		 [Order] = 30
where	 [Key] = @widgetGroupKey


select	 @labelKey = [FK_Translation_Key_Label]
from	 [Field]
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(Travel)(0)'

update	 [Translation]
set		 [Text] = 'Travel Coverage'
where	 [Key] = @labelKey
and		 [FK_Language_ID] = 1


select	 @labelKey = [FK_Translation_Key_Label]
from	 [Field]
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(DMR)(0)'

update	 [Translation]
set		 [Text] = 'DMR'
where	 [Key] = @labelKey
and		 [FK_Language_ID] = 1


select	 @labelKey = [FK_Translation_Key_Label]
from	 [Field]
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(ADP)(0)'

update	 [Translation]
set		 [Text] = 'ADP'
where	 [Key] = @labelKey
and		 [FK_Language_ID] = 1

select	 @labelKey = [FK_Translation_Key_Label]
from	 [Field]
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(Tracing)(0)'

update	 [Translation]
set		 [Text] = 'Security Tracing'
where	 [Key] = @labelKey
and		 [FK_Language_ID] = 1


-- Set order of HPE HMC Service Level fields
update [Translation]
set [Order] = 30
where [Key] in
(
	select distinct [FK_Translation_Key_Text] from [ProductAttribute] where [FK_Attribute_ID] in
	(
		select [ID] from [Attribute] where [Code] = 'servicelevel'
	)
)
and [Group] = 'CPQ'
and	[FK_Language_ID] = 1  
and [Text] = 'Proactive Care Advanced'


update [Translation]
set [Order] = 20
where [Key] in
(
	select distinct [FK_Translation_Key_Text] from [ProductAttribute] where [FK_Attribute_ID] in
	(
		select [ID] from [Attribute] where [Code] = 'servicelevel'
	)
)
and [Group] = 'CPQ'
and	[FK_Language_ID] = 1  
and [Text] = 'Proactive Care'


update [Translation]
set [Order] = 10
where [Key] in
(
	select distinct [FK_Translation_Key_Text] from [ProductAttribute] where [FK_Attribute_ID] in
	(
		select [ID] from [Attribute] where [Code] = 'servicelevel'
	)
)
and [Group] = 'CPQ'
and	[FK_Language_ID] = 1  
and [Text] = 'Foundation Care'



-- Set order of HPE HMC Response fields
update [Translation]
set [Order] = 40
where [Key] in
(
	select distinct [FK_Translation_Key_Text] from [ProductAttribute] where [FK_Attribute_ID] in
	(
		select [ID] from [Attribute] where [Code] = 'response'
	)
)
and [Group] = 'CPQ'
and	[FK_Language_ID] = 1  
and [Text] = '6hr CTR'


update [Translation]
set [Order] = 30
where [Key] in
(
	select distinct [FK_Translation_Key_Text] from [ProductAttribute] where [FK_Attribute_ID] in
	(
		select [ID] from [Attribute] where [Code] = 'response'
	)
)
and [Group] = 'CPQ'
and	[FK_Language_ID] = 1  
and [Text] = '24x7, 4hr'


update [Translation]
set [Order] = 20
where [Key] in
(
	select distinct [FK_Translation_Key_Text] from [ProductAttribute] where [FK_Attribute_ID] in
	(
		select [ID] from [Attribute] where [Code] = 'response'
	)
)
and [Group] = 'CPQ'
and	[FK_Language_ID] = 1  
and [Text] = '9x5'


update [Translation]
set [Order] = 10
where [Key] in
(
	select distinct [FK_Translation_Key_Text] from [ProductAttribute] where [FK_Attribute_ID] in
	(
		select [ID] from [Attribute] where [Code] = 'response'
	)
)
and [Group] = 'CPQ'
and	[FK_Language_ID] = 1  
and [Text] = 'Next Business Day'

