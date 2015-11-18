
-- Create ResourceCategory table
CREATE TABLE [dbo].[ResourceCategory](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[FK_Translation_Key_Name] [int] NOT NULL,
	[Order] [int] NOT NULL,
 CONSTRAINT [PK_ResourceHeader] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


-- Create Resource table
CREATE TABLE [dbo].[Resource](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Description] [nvarchar](50) NOT NULL,
	[FK_Resource_Category_ID] [int] NOT NULL,
	[Type] [nvarchar](20) NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[FK_Translation_Key_Title] [int] NOT NULL,
	[FK_Region_ID] [int] NULL,
	[FK_Language_ID] [int] NULL,
	[FK_SellerChannel_ID] [int] NULL,
	[mfrCode] [nvarchar](3) NULL,
	[Order] [int] NOT NULL,
 CONSTRAINT [PK_Resource] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[Resource]  WITH CHECK ADD  CONSTRAINT [FK_Resource_Category] FOREIGN KEY([FK_Resource_Category_ID])
REFERENCES [dbo].[ResourceCategory] ([ID])
GO

ALTER TABLE [dbo].[Resource] CHECK CONSTRAINT [FK_Resource_Category]
GO

ALTER TABLE [dbo].[Resource]  WITH CHECK ADD  CONSTRAINT [FK_Resource_Channel] FOREIGN KEY([FK_SellerChannel_ID])
REFERENCES [dbo].[Channel] ([ID])
GO

ALTER TABLE [dbo].[Resource] CHECK CONSTRAINT [FK_Resource_Channel]
GO

ALTER TABLE [dbo].[Resource]  WITH CHECK ADD  CONSTRAINT [FK_Resource_Language] FOREIGN KEY([FK_Language_ID])
REFERENCES [dbo].[Language] ([ID])
GO

ALTER TABLE [dbo].[Resource] CHECK CONSTRAINT [FK_Resource_Language]
GO

ALTER TABLE [dbo].[Resource]  WITH CHECK ADD  CONSTRAINT [FK_Resource_Region] FOREIGN KEY([FK_Region_ID])
REFERENCES [dbo].[Region] ([ID])
GO

ALTER TABLE [dbo].[Resource] CHECK CONSTRAINT [FK_Resource_Region]
GO


-- Populate ResourceCategory table
begin transaction

declare @transKey int
select @transKey = max([key]) from translation

-- Insert Channel Central Training Videos category
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Training Videos', 1, 0, 'Resources', 0, 1)

insert into [ResourceCategory]([Name], [FK_Translation_Key_Name], [Order])
values ('TrainingVideos', @transKey, 0)

-- Insert Marketing category
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Marketing', 1, 0, 'Resources', 0, 1)

insert into [ResourceCategory]([Name], [FK_Translation_Key_Name], [Order])
values ('Marketing', @transKey, 0)


-- Populate Resource table

declare @videoCategoryID int
declare @marketingCategoryID int

select @videoCategoryID = [ID] from [ResourceCategory] where [Name] = 'TrainingVideos'
select @marketingCategoryID = [ID] from [ResourceCategory] where [Name] = 'Marketing'


-- Insert iQuote Introduction video (English)
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'iQuote Introduction', 1, 0, 'Resources', 0, 1)

insert into [Resource]([Description], [FK_Resource_Category_ID], [Type], [Code], [FK_Translation_Key_Title], [FK_Region_ID], [FK_Language_ID], [FK_SellerChannel_ID], [mfrCode], [Order])
values ('iQuoteIntro', @videoCategoryID, 'YouTube', 'vKSLSO9LTGc', @transKey, null, 1, null, null, 0)

-- Insert iQuote Introduction video (Spanish)
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Presentamos iQuote', 38, 0, 'Resources', 0, 1)

insert into [Resource]([Description], [FK_Resource_Category_ID], [Type], [Code], [FK_Translation_Key_Title], [FK_Region_ID], [FK_Language_ID], [FK_SellerChannel_ID], [mfrCode], [Order])
values ('iQuoteIntro', @videoCategoryID, 'YouTube', 'svK08M1Tpnk', @transKey, null, 38, null, null, 0)

-- Insert iQuote Training video (English)
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'iQuote 2 User Guide - Training Video', 1, 0, 'Resources', 0, 1)

insert into [Resource]([Description], [FK_Resource_Category_ID], [Type], [Code], [FK_Translation_Key_Title], [FK_Region_ID], [FK_Language_ID], [FK_SellerChannel_ID], [mfrCode], [Order])
values ('iQuoteTraining', @videoCategoryID, 'YouTube', 'vNODHlTAX_4', @transKey, null, 1, null, null, 0)


commit transaction


