if not exists (select * from [Address] where [Code] = 'HPEUniversalHost')
begin

begin transaction

declare @transKey int
select @transKey = max([key]) from translation


-- Insert Universal/HPE request host
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'hpe.hpiquote.com', 1, 0, 'Address', 0, 1)

insert into [Address]([Code], [FK_Translation_Key_Address])
values ('HPEUniversalHost', @transKey)


-- Insert external support email address
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'hpi.hpiquote.com', 1, 0, 'Address', 0, 1)

insert into [Address]([Code], [FK_Translation_Key_Address])
values ('HPIUniversalHost', @transKey)


commit transaction

end