


begin transaction

declare @transKey		int
declare @translation1	nvarchar(max)
declare @translation38	nvarchar(max)

-- Pick up the existing translations
select	@transKey = [key]
from	[Translation]
where	[Text] = 'learnMore'
and		[Group] = 'UI'
and		[FK_Language_ID] = 2

select	@translation1 = [Text]
from	[Translation]
where	[Key] = @transKey
and		[FK_Language_ID] = 1

select	@translation38 = [Text]
from	[Translation]
where	[Key] = @transKey
and		[FK_Language_ID] = 38

-- Current highest translation key
select @transKey = max([key]) from translation

if @translation1 is not null or @translation38 is not null
begin

	-- Insert learnMore HPE lookup
	select @transKey = @transKey + 1
	insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
	values (@transKey, 'learnMoreHPE', 2, 0, 'UI', 1, 0)

	if @translation1 is not null
	begin
		insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
		values (@transKey, @translation1, 1, 0, 'UI', 1, 0)
	end

	if @translation38 is not null
	begin
		insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
		values (@transKey, @translation38, 38, 0, 'UI', 1, 0)
	end

	-- Insert learnMore HPI lookup
	select @transKey = @transKey + 1
	insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
	values (@transKey, 'learnMoreHPI', 2, 0, 'UI', 1, 0)

	if @translation1 is not null
	begin
		insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
		values (@transKey, @translation1, 1, 0, 'UI', 1, 0)
	end

	if @translation38 is not null
	begin
		insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
		values (@transKey, @translation38, 38, 0, 'UI', 1, 0)
	end

end

commit transaction
