
if not exists (select * from [Role] where [Code] = 'USERTYPE_DISTRIBUTOR')
begin

begin transaction

declare @transKey int
select @transKey = max([key]) from translation

select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Distributor', 1, 0, 'UserType', 0, 1)

insert into [Role] ([Code], [FK_Translation_key])
values
(
	'USERTYPE_DISTRIBUTOR'
	,@transKey
)

select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Reseller', 1, 0, 'UserType', 0, 1)

insert into [Role] ([Code], [FK_Translation_key])
values
(
	'USERTYPE_RESELLER'
	,@transKey
)

select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'End User', 1, 0, 'UserType', 0, 1)

insert into [Role] ([Code], [FK_Translation_key])
values
(
	'USERTYPE_ENDUSER'
	,@transKey
)

select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'HP Employee', 1, 0, 'UserType', 0, 1)

insert into [Role] ([Code], [FK_Translation_key])
values
(
	'USERTYPE_HPEMPLOYEE'
	,@transKey
)

select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Other', 1, 0, 'UserType', 0, 1)

insert into [Role] ([Code], [FK_Translation_key])
values
(
	'USERTYPE_OTHER'
	,@transKey
)

commit transaction

end