

-- Add IsOption and deleted columns to the Product table
if not exists (select * from sys.columns where object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'IsOption')
begin
	alter table [dbo].[Product] add [IsOption] [bit] not null constraint [DF_Product_IsOption] DEFAULT (0);
end
GO

if not exists (select * from sys.columns where object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'deleted')
begin
	alter table [dbo].[Product] add [deleted] [bit] not null constraint [DF_Product_deleted] DEFAULT (0);
end
GO

-- Populate IsOption
update	[Product]
set		[IsOption] = 1
where	[IsSystem] = 0
and		[SKU] <> ''
GO

-- Create temporary table for duplicated products
create table [#SKU](
	[Code] [nvarchar](50) NOT NULL,
	[SystemCount] integer NULL,
	[OptionCount] integer NULL,
 CONSTRAINT [PK_SKUCode] PRIMARY KEY CLUSTERED 
(
	[Code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Find all Product rows with duplicate SKU codes
insert into [#SKU]([Code])
select		p.[sku]
from		[Product] p
where		p.[sku] <> ''
and			p.[deleted] = 0
and			(p.[IsSystem] = 1 or p.[IsOption] = 1)
group by	p.[sku]
having		count(*) > 1

declare @sku		nvarchar(50)
declare @systemID	integer
declare @optionID	integer

declare c cursor for
select [Code] from [#SKU]

open c
fetch next from c INTO @sku
while @@fetch_status = 0
begin

	select @systemID = [ID] from [Product] where [sku] = @sku and [IsSystem] = 1
	select @optionID = [ID] from [Product] where [sku] = @sku and [IsOption] = 1

	if @systemID is not null and @optionID is not null
	begin

		-- @sku contains the code of a product that has both an IsSystem Product row and two or more IsOption Product rows

		begin transaction

		-- Soft delete ProductAttribute rows for the option
		update	[ProductAttribute]
		set		[deleted] = 1
		where	[FK_Product_ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1)
		
		-- Soft delete Variant rows for the option
		update	[Variant]
		set		[deleted] = 1
		where	[FK_Product_ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1)

		-- Update any Points rows to point to the System row instead of the Option row
		update	[Points]
		set		[FK_Product_ID] = @systemID
		where	[FK_Product_ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1)

		-- Update any PromoProduct rows to point to the System row instead of the Option row
		update	[PromoProduct]
		set		[FK_Product_ID] = @systemID
		where	[FK_Product_ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1)

		-- Set the system row to be IsOption too
		update	[Product]
		set		[IsOption] = 1
		where	[ID] = @systemID

		-- Delete the IsOption row
		update	[Product]
		set		[deleted] = 1
		where	[ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1)

		-- Update Branch rows for the option to refer to the unified Product row
		update	[Branch]
		set		[FK_Product_ID] = @systemID
		where	[FK_Product_ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1)

		commit transaction

	end
	else if @systemID is null and @optionID is not null
	begin

		-- @sku contains the code of a product that has two or more IsOption Product rows but no IsSystem Product rows

		begin transaction

		-- Soft delete ProductAttribute rows for the duplicated options
		update	[ProductAttribute]
		set		[deleted] = 1
		where	[FK_Product_ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1 and [ID] <> @optionId)
		
		-- Soft delete Variant rows for the duplicated options
		update	[Variant]
		set		[deleted] = 1
		where	[FK_Product_ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1 and [ID] <> @optionId)

		-- Delete all but one IsOption row
		update	[Product]
		set		[deleted] = 1
		where	[sku] = @sku
		and		[IsOption] = 1
		and		[ID] <> @optionId

		-- Update Branch rows for the options to refer to the unified Product row
		update	[Branch]
		set		[FK_Product_ID] = @optionId
		where	[FK_Product_ID] in (select [ID] from [Product] where [sku] = @sku and [IsOption] = 1 and [ID] <> @optionId)

		commit transaction

	end

	fetch next from c INTO @sku
end
close c
deallocate c

-- Tidy up
drop table #SKU
GO
