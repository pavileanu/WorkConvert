
declare @productId			int
declare @calTypeUser		bit
declare @description		nvarchar(max)

declare @DESCATTRIBUTE		int
declare @CALTYPEATTRIBUTE	int
declare @USERCALTRANS		int
declare @DEVICECALTRANS		int

select @DESCATTRIBUTE = [ID]	from [Attribute] where code = 'desc'
select @CALTYPEATTRIBUTE = [ID] from [Attribute] where code = 'caltype'
select @USERCALTRANS = [key]	from [Translation] where [FK_Language_ID] = 1 and [Group] = 'Flt' and [Text] = 'User'
select @DEVICECALTRANS = [key]	from [Translation] where [FK_Language_ID] = 1 and [Group] = 'Flt' and [Text] = 'Device'

declare c cursor for 
select distinct fk_product_id from ProductAttribute where FK_Attribute_ID = @CALTYPEATTRIBUTE

open c

fetch next from c into @productId

while @@FETCH_STATUS = 0
begin

	-- Remove existing caltype attributes
	delete from ProductAttribute
	where		FK_Attribute_ID = @CALTYPEATTRIBUTE
	and			FK_Product_ID = @productId

	-- Get CAL product description to work out if User or Device CAL
	select @description =	t.[Text]
	from					[Translation] t
	inner join				[ProductAttribute] pa on pa.FK_Translation_Key_Text = t.[key]
	where					pa.FK_Product_ID = @productId
	and						pa.FK_Attribute_ID = @DESCATTRIBUTE

	if  CHARINDEX('User CAL', @description) > 0
	begin
		set @calTypeUser = 1
	end
	else
	begin
		set @calTypeUser = 0
	end

	-- Set either User or Device caltype attribute
		insert into ProductAttribute
		(
			 [FK_Attribute_ID]
			,[FK_Product_ID]
			,[NumericValue]
			,[FK_Translation_Key_Text]
			,[FK_Unit_ID]
		)
		values
		(
			 @CALTYPEATTRIBUTE
			,@productId
			,0
			,case @calTypeUser when 1 then @USERCALTRANS else @DEVICECALTRANS end
			,1
		)

	fetch next from c into @productId
end 

close c
deallocate c
