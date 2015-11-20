
declare @translationKeyEF			int
declare @translationKeyDMRHeading	int
declare @translationKeyDMRGroup		int

declare @TRAVELFIELD int
declare @DMRFIELD int
declare @ADPFIELD int
declare @TRACINGFIELD int
declare @CTRFIELD int

set @TRAVELFIELD = 5291
set @DMRFIELD = 5292
set @ADPFIELD = 5293
set @TRACINGFIELD = 5294
set @CTRFIELD = 5295


begin transaction


-- Fix the DMR attribute so its Code is something unique
update [Attribute] set [Code] = 'DMR_ISS' where [ID] = 338


-- Remove CTR from the HPI Help Me Choose matrix
delete from [field]
where		[ID] = @CTRFIELD


-- Add Enhanced Features grouping
select @translationKeyEF = max([key]) + 1 from [Translation]
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @translationKeyEF
	,'Enhanced Features'
	,1
	,1
	,'FLDLBL'
	,0
	,1
	,null	
)
update [Field] set FK_Translation_Key_WidgetGroup = @translationKeyEF, WidgetUI = 'BOOL' where [ID] in (@TRAVELFIELD, @DMRFIELD, @ADPFIELD, @TRACINGFIELD)


-- Add DMR heading
select @translationKeyDMRHeading = @translationKeyEF + 1
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @translationKeyDMRHeading
	,'Options'
	,1
	,1
	,'FLDLBL'
	,0
	,1
	,null	
)


-- Add DMR grouping
select @translationKeyDMRGroup = @translationKeyDMRHeading + 1
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @translationKeyDMRGroup
	,'Options'
	,1
	,0
	,'CPQ'
	,0
	,1
	,null	
)


-- Add DMR ("Options") field to Server HMC
insert into [Field]
(
       [FK_Screen_ID]
      ,[Property]
      ,[FK_Translation_Key_Label]
      ,[HelpText]
      ,[FK_validation_id]
      ,[LookupOf]
      ,[FK_InputType_ID]
      ,[length]
      ,[Order]
      ,[Width]
      ,[DefaultValue]
      ,[VisibleList]
      ,[VisiblePage]
      ,[Height]
      ,[DefaultFilter]
      ,[DefaultSort]
      ,[Priority]
      ,[FK_Translation_Key_WidgetGroup]
      ,[WidgetUI]
      ,[CanUserSelect]
      ,[VisibleSquare]
      ,[FK_Field_ID_Linked]
      ,[Grows]
      ,[DefaultFilterValues]
      ,[FilterVisible]
      ,[HMC_MutualExclusivity]
      ,[InvertFilterOrder]
)
values
(
		 786
		,'Product.i_Attributes_Code(DMR_ISS)(0)'
		,@translationKeyDMRHeading
		,''
		,null
		,''
		,517
		,0
		,20
		,10
		,''
		,1
		,0
		,1.5
		,'EQ'
		,''
		,30
		,@translationKeyDMRGroup
		,'TKEY'
		,1
		,0
		,null
		,0
		,'EQ|785666'
		,1
		,1
		,0
)


-- Fix missing ProductAttribute translations
update	[ProductAttribute]
set		[FK_Translation_Key_Text] = 114399
where	[FK_Attribute_ID] = 325		-- Travel
and		[FK_Translation_Key_Text] is null

update	[ProductAttribute]
set		[FK_Translation_Key_Text] = 814535
where	[FK_Attribute_ID] = 339		-- Tracing
and		[FK_Translation_Key_Text] is null

update	[ProductAttribute]
set		[FK_Translation_Key_Text] = 114400
where	[FK_Attribute_ID] = 340		-- ADP
and		[FK_Translation_Key_Text] is null

update	[ProductAttribute]
set		[FK_Translation_Key_Text] = 785672
where	[FK_Attribute_ID] = 341		-- DMR
and		[FK_Translation_Key_Text] is null


-- Fix the Next Business Day HMC sort order
update	[Translation] set [Order] = -1
where	[Text] = 'Next Business Day'
and		[Group] = 'CPQ'
and     [id] = 785708


-- Give the Duration group on HPI HMC filters radio button behaviour
update [Field] set HMC_MutualExclusivity = 1 where [ID] = 5298 and [FK_Screen_ID] = 1812


-- Fix DMR HMC filter sort order
update translation set [Order] = 20 where ID = 785783


-- Fix HMC Response column width
update	[Field]
set		[Width] = 10
where	[FK_Screen_ID] = 786
and		[Property] = 'Product.i_Attributes_Code(Response)(0)'


commit transaction