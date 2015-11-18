

-- Desktop HMC: Fix filter translations
declare @translationKey int


begin transaction


select @translationKey = max([key]) + 1 from [Translation]
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
	 @translationKey
	,'Travel Coverage'
	,1
	,1
	,'CP-HMC'
	,0
	,1
	,null	
)

update	 [Field] set FK_Translation_Key_Label = @translationKey
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(Travel)(0)'


select @translationKey = @translationKey + 1
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
	 @translationKey
	,'DMR'
	,1
	,1
	,'CP-HMC'
	,0
	,1
	,null	
)

update	 [Field] set FK_Translation_Key_Label = @translationKey
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(DMR)(0)'


select @translationKey = @translationKey + 1
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
	 @translationKey
	,'ADP'
	,1
	,1
	,'CP-HMC'
	,0
	,1
	,null	
)

update	 [Field] set FK_Translation_Key_Label = @translationKey
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(ADP)(0)'


select @translationKey = @translationKey + 1
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
	 @translationKey
	,'Security Tracing'
	,1
	,1
	,'CP-HMC'
	,0
	,1
	,null	
)

update	 [Field] set FK_Translation_Key_Label = @translationKey
where	 [FK_Screen_ID] = 1812
and		 [Property] = 'Product.i_Attributes_Code(Tracing)(0)'


commit transaction