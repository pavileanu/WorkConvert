
declare @transKey int

select	@transKey = [FK_Translation_Key_Address]
from	[Address]
where	[Code] = 'HPEUniversalHost'

if @transKey is not null
begin
	update	[translation]
	set		[Text] = 'iquote.hpe.com'
	where	[key] = @transKey
end

select	@transKey = [FK_Translation_Key_Address]
from	[Address]
where	[Code] = 'HPIUniversalHost'

if @transKey is not null
begin
	update	[translation]
	set		[Text] = 'iquote.hp.com'
	where	[key] = @transKey
end



if not exists (select * from [Address] where [Code] = 'IQ1Host')
begin

begin transaction

select @transKey = max([key]) from translation


-- Insert IQ1 URL
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'www.channelcentral.net/iquote', 1, 0, 'Address', 0, 1)

insert into [Address]([Code], [FK_Translation_Key_Address])
values ('IQ1Host', @transKey)


commit transaction

end