if not exists (select * from [Address] where [Code] = 'CCSupportEmail')
begin

begin transaction

declare @transKey int
select @transKey = max([key]) from translation


-- Insert internal support email address
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'support@channelcentral.net', 1, 0, 'Address', 0, 1)

insert into [Address]([Code], [FK_Translation_Key_Address])
values ('CCSupportEmail', @transKey)


-- Insert external support email address
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'support@hpiquote.net', 1, 0, 'Address', 0, 1)

insert into [Address]([Code], [FK_Translation_Key_Address])
values ('iQuoteSupportEmail', @transKey)


commit transaction

end