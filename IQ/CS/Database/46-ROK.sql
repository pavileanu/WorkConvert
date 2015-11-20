
-- Create ROKAttributes table
CREATE TABLE [dbo].[ROKAttributes](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OS_Code] [nvarchar](50) NOT NULL,
	[FK_Attribute_Code] [nvarchar](50) NOT NULL,
	[FK_Translation_Key_Name] [int] NOT NULL,
 CONSTRAINT [PK_ROKAttributes] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


-- Set up attributes and translations for the ROKAttributes table
begin transaction

declare @licensesTranslationKey int
declare @calsTranslationKey int
declare @maxUsersTranslationKey int
declare @virtualisationTranslationKey int
declare @maxCpusTranslationKey int
declare @maxRamTranslationKey int


-- Insert ROK field names
select @licensesTranslationKey = max([key]) + 1 from [Translation]
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Licenses'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Licenses'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Attribute]
(
	 [Code]
    ,[FK_Translation_key_Name]
    ,[Order]
)
values
(
	 'licences'
	,@licensesTranslationKey
	,0
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'CALs'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'CALs'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Attribute]
(
	 [Code]
    ,[FK_Translation_key_Name]
    ,[Order]
)
values
(
	 'cals'
	,@calsTranslationKey
	,0
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Max. Users'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Max. Users'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Attribute]
(
	 [Code]
    ,[FK_Translation_key_Name]
    ,[Order]
)
values
(
	 'maxusers'
	,@maxUsersTranslationKey
	,0
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Virtualisation'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Virtualisation'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Attribute]
(
	 [Code]
    ,[FK_Translation_key_Name]
    ,[Order]
)
values
(
	 'virtualisation'
	,@virtualisationTranslationKey
	,0
)


insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Max. CPUs'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Max. CPUs'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Attribute]
(
	 [Code]
    ,[FK_Translation_key_Name]
    ,[Order]
)
values
(
	 'maxcpus'
	,@maxCpusTranslationKey
	,0
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Max. RAM'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Max. RAM'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Attribute]
(
	 [Code]
    ,[FK_Translation_key_Name]
    ,[Order]
)
values
(
	 'maxram'
	,@maxRamTranslationKey
	,0
)


declare @osCode nvarchar(50)

-- Insert ROK field values - W2012_ESS
select @osCode = 'W2012_ESS'
select @licensesTranslationKey = @maxRamTranslationKey + 1
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Per Server'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Per Server'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'licences'
		,@licensesTranslationKey
)
  
 insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'No'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'No'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'cals'
		,@calsTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'25'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'25'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxusers'
		,@maxUsersTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Guest Only'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Guest Only'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'virtualisation'
		,@virtualisationTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'2'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'2'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxcpus'
		,@maxCpusTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'64GB'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'64GB'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxram'
		,@maxRamTranslationKey
)


-- Insert ROK field values - W2012_DAT
select @osCode = 'W2012_DAT'
select @licensesTranslationKey = @maxRamTranslationKey + 1
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'2 CPUs per license'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'2 CPUs per license'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'licences'
		,@licensesTranslationKey
)
  
 insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'Yes'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'Yes'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'cals'
		,@calsTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxusers'
		,@maxUsersTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (unlimited guests)'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (unlimited guests)'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'virtualisation'
		,@virtualisationTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxcpus'
		,@maxCpusTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxram'
		,@maxRamTranslationKey
)

-- Insert ROK field values - W2012_STD
select @osCode = 'W2012_STD'
select @licensesTranslationKey = @maxRamTranslationKey + 1
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'2 CPUs per license'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'2 CPUs per license'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'licences'
		,@licensesTranslationKey
)
  
 insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'Yes'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'Yes'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'cals'
		,@calsTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxusers'
		,@maxUsersTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (2 guests included)'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (2 guests included)'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'virtualisation'
		,@virtualisationTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxcpus'
		,@maxCpusTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxram'
		,@maxRamTranslationKey
)

-- Insert ROK field values - W2012_FDN
select @osCode = 'W2012_FDN'
select @licensesTranslationKey = @maxRamTranslationKey + 1
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Per Server'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Per Server'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'licences'
		,@licensesTranslationKey
)
  
 insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'No'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'No'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'cals'
		,@calsTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'15'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'15'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxusers'
		,@maxUsersTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'None'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'None'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'virtualisation'
		,@virtualisationTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'1'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'1'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxcpus'
		,@maxCpusTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'32GB'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'32GB'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxram'
		,@maxRamTranslationKey
)

-- Insert ROK field values - W2012R2_ESS
select @osCode = 'W2012R2_ESS'
select @licensesTranslationKey = @maxRamTranslationKey + 1
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Per Server'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Per Server'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'licences'
		,@licensesTranslationKey
)
  
 insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'No'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'No'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'cals'
		,@calsTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'25'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'25'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxusers'
		,@maxUsersTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (1 guest included)'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (1 guest included)'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'virtualisation'
		,@virtualisationTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'2'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'2'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxcpus'
		,@maxCpusTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'64GB'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'64GB'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxram'
		,@maxRamTranslationKey
)


-- Insert ROK field values - W2012R2_DAT
select @osCode = 'W2012R2_DAT'
select @licensesTranslationKey = @maxRamTranslationKey + 1
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'2 CPUs per license'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'2 CPUs per license'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'licences'
		,@licensesTranslationKey
)
  
 insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'Yes'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'Yes'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'cals'
		,@calsTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxusers'
		,@maxUsersTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (unlimited guests)'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (unlimited guests)'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'virtualisation'
		,@virtualisationTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxcpus'
		,@maxCpusTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxram'
		,@maxRamTranslationKey
)

-- Insert ROK field values - W2012R2_STD
select @osCode = 'W2012R2_STD'
select @licensesTranslationKey = @maxRamTranslationKey + 1
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'2 CPUs per license'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'2 CPUs per license'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'licences'
		,@licensesTranslationKey
)
  
 insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'Yes'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'Yes'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'cals'
		,@calsTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxusers'
		,@maxUsersTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (2 guests included)'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'Host or Guest (2 guests included)'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'virtualisation'
		,@virtualisationTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxcpus'
		,@maxCpusTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Unlimited'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'Unlimited'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxram'
		,@maxRamTranslationKey
)

-- Insert ROK field values - W2012R2_FDN
select @osCode = 'W2012R2_FDN'
select @licensesTranslationKey = @maxRamTranslationKey + 1
select @calsTranslationKey = @licensesTranslationKey + 1
select @maxUsersTranslationKey = @calsTranslationKey + 1
select @virtualisationTranslationKey = @maxUsersTranslationKey + 1
select @maxCpusTranslationKey = @virtualisationTranslationKey + 1
select @maxRamTranslationKey = @maxCpusTranslationKey + 1

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Per Server'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @licensesTranslationKey
	,'Per Server'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'licences'
		,@licensesTranslationKey
)
  
 insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'No'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @calsTranslationKey
	,'No'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'cals'
		,@calsTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'15'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxUsersTranslationKey
	,'15'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxusers'
		,@maxUsersTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'None'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @virtualisationTranslationKey
	,'None'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'virtualisation'
		,@virtualisationTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'1'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxCpusTranslationKey
	,'1'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxcpus'
		,@maxCpusTranslationKey
)
  
insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'32GB'
	,1
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [Translation]
(
	 [key]
    ,[Text]
    ,[FK_Language_ID]
    ,[Order]
    ,[Group]
    ,[HPonly]
    ,[TranslateThis]
    ,[Notes]
)
values
(
	 @maxRamTranslationKey
	,'32GB'
	,2
	,0
	,'UI'
	,0
	,1
	,null	
)

insert into [ROKAttributes]
(
		[OS_Code]
       ,[FK_Attribute_Code]
       ,[FK_Translation_Key_Name]
)
values
(
		 @osCode
		,'maxram'
		,@maxRamTranslationKey
)


commit transaction
GO

-- Fix edition names
update [Translation]
set [Text] = 'Standard Edition'
where [Text] = 'Standard'
and [Group] = ''

update [Translation]
set [Text] = 'Datacenter Edition'
where [Text] = 'Datacenter'
and [Group] = ''

update [Translation]
set [Text] = 'Essentials Edition'
where [Text] = 'Essentials'
and [Group] = ''
GO

