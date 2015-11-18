
-- Create a temporary table to hold all used translations
if OBJECT_ID('tempdb..#UsedTranslation') is not null
begin
	drop table #UsedTranslation
end
GO

CREATE TABLE #UsedTranslation
(
	[Key] [nvarchar](50) NOT NULL
) 
GO

-- Populate #UsedTranslation with all used translation keys
insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_Name from [attribute] where FK_Translation_key_Name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key from [Branch] where FK_Translation_Key is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Collective from [Branch] where FK_Translation_Key_Collective is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_collectiveSingular from [Branch] where FK_Translation_Key_collectiveSingular is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_name from [Bundle] where FK_Translation_key_name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_conversionname from [Conversion] where FK_Translation_key_conversionname is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_notes from [Currency] where FK_Translation_key_notes is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_name from [Currency] where FK_Translation_key_name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Label from [Field] where FK_Translation_Key_Label is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_WidgetGroup from [Field] where FK_Translation_Key_WidgetGroup is not null
GO

-- Insert translations referenced by the Field.DefaultFilterValues
insert into #UsedTranslation ([Key])
select distinct convert(int, substring([DefaultFilterValues], 4, 999))
from [Field]
where DefaultFilterValues like 'eq|%'

GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_name from [Offer] where FK_Translation_key_name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_countryname from [OLD_Country] where FK_Translation_key_countryname is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Text from [ProductAttribute] where FK_Translation_Key_Text is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_text from [ProductType] where FK_Translation_key_text is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_message from [ProductValidations] where FK_Translation_key_message is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_correctmessage from [ProductValidations] where FK_Translation_key_correctmessage is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_name from [Region] where FK_Translation_key_name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key from [Right] where FK_Translation_key is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key from [Role] where FK_Translation_key is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_ViolationMessage from [Rule] where FK_Translation_key_ViolationMessage is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_RuleName from [Rule] where FK_Translation_key_RuleName is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_ExplainantionMessage from [Rule] where FK_Translation_key_ExplainantionMessage is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Name from [ROKAttributes] where FK_Translation_Key_Name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Name from [Scheme] where FK_Translation_Key_Name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Name from [Sector] where FK_Translation_Key_Name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Notes from [Slot] where FK_Translation_Key_Notes is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key from [SlotType] where FK_Translation_Key is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Short from [SlotType] where FK_Translation_Key_Short is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key from [State] where FK_Translation_Key is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Name from [Unit] where FK_Translation_Key_Name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Address from [Address] where FK_Translation_Key_Address is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Name from [Legal] where FK_Translation_Key_Name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_Key_Description from [Promo] where FK_Translation_Key_Description is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_Name from [Message] where FK_Translation_key_Name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_Name from [ResourceCategory] where FK_Translation_key_Name is not null
GO

insert into #UsedTranslation ([Key])
select distinct FK_Translation_key_Title from [Resource] where FK_Translation_key_Title is not null
GO

-- Mark all unused translations as deleted
update	[Translation]
set		[deleted] = 1
where	[deleted] = 0
and		[group] <> 'UI'
and		[key] not in
(
  select distinct [Key] from #UsedTranslation
)
GO
