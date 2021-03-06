if not exists (select * from [Legal] where [Code] = 'HPUniversalT&C')
begin

begin transaction

declare @transKey int
select @transKey = max([key]) from translation

-- Insert HPE Universal legal text
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Every care is taken to ensure that the information contained within this site is accurate. Errors and Omissions excepted.<br/><br/>All prices shown within iQuote Universal are [mfr] List Price.', 1, 0, 'Legal', 0, 1)

insert into [Legal]([Code], [FK_Translation_Key_Name])
values ('HPUniversalT&C', @transKey)

commit transaction

end
