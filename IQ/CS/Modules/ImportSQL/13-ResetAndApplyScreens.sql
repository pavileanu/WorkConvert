update branch set fk_screen_id_matrix = null
delete from field
delete from screen

set identity_insert screen on 
insert into screen ( [ID]
      ,[Code]
      ,[Object]
      ,[Title]) select * from iquote2_sandbox.dbo.screen
set identity_insert screen off

set identity_insert field on 
insert into field (  [ID]
      ,[FK_Screen_ID]
      ,[Property]
      ,fk_translation_key_label
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
	  ,Grows
	  ,DefaultFilterValues
      ) select * from iquote2_sandbox.dbo.field
set identity_insert field off

----insert into Translation 
--select (select max([key]) from translation) + ROW_NUMBER() OVER (ORDER BY b.text),ltrim(rtrim(b.text)),1,1,''
--from (select distinct ti.text
--from field inner join imported.dbo.field fi on fi.id = field.id
--inner join imported.dbo.translation ti on ti.[key] = fi.FK_Translation_Key_WidgetGroup
--left outer join translation t on t.text = ti.text
--where t.text is null
--) b LEFT OUTER JOIN Translation t on t.Text = b.text collate database_default
--WHERE t.text is null and b.text is not null

--update field set FK_Translation_Key_WidgetGroup=t.[key]
----select t.text,ti.text,field.*
--from field inner join imported.dbo.field fi on fi.id = field.id
--inner join imported.dbo.translation ti on ti.[key] = fi.FK_Translation_Key_WidgetGroup
--inner join translation t on t.text = ti.text

update b set FK_Screen_ID_Matrix = screen.id 
--select  b.id,screen.id,branch.id,t.text,tr.text,tr.[key],t.[key]
--select * 
from branch b inner join translation t on t.[key] = b.fk_translation_key
inner join iquote2_sandbox.dbo.translation tr on tr.text = t.text 
inner join iquote2_sandbox.dbo.branch on tr.[key]=branch.FK_Translation_key 
inner join iquote2_sandbox.dbo.screen on branch.fk_screen_id_matrix=screen.id

