-- Create Address table
CREATE TABLE [dbo].[Address](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[FK_Translation_Key_Address] [int] NOT NULL,
 CONSTRAINT [PK_Address] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

-- Populate Address table
begin transaction

declare @transKey int
select @transKey = max([key]) from translation

-- Insert Channel Central privacy policy link
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'http://www.channelcentral.net/privacy-policy.asp', 1, 0, 'Address', 0, 1)

insert into [Address]([Code], [FK_Translation_Key_Address])
values ('CCPrivacyPolicyUrl', @transKey)


-- Insert HPE privacy policy link
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'http://welcome.hp.com/country/us/en/privacy.html', 1, 0, 'Address', 0, 1)

insert into [Address]([Code], [FK_Translation_Key_Address])
values ('HPEPrivacyPolicyUrl', @transKey)


-- Insert HPI privacy policy link
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'http://welcome.hp.com/country/us/en/privacy.html', 1, 0, 'Address', 0, 1)

insert into [Address]([Code], [FK_Translation_Key_Address])
values ('HPIPrivacyPolicyUrl', @transKey)


commit transaction


-- Create Legal table
CREATE TABLE [dbo].[Legal](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[FK_Translation_Key_Name] [int] NOT NULL,
 CONSTRAINT [PK_Legal] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


begin transaction

declare @transKey int
select @transKey = max([key]) from translation

-- Insert Channel Central legal text
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Usage of iQuote means that you agree to the following Terms & Conditions:<br/><br/> Every care is taken to ensure that the information contained within this site is accurate, however Errors and Omissions Excepted.', 1, 0, 'Legal', 0, 1)

insert into [Legal]([Code], [FK_Translation_Key_Name])
values ('CCLegal', @transKey)


-- Insert HPE legal text
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Terms and Conditions<br/><br/>Costs are subject to change in line with vendor pricing.<br/><br/>All stock availability is subject to confirmation at time and date of order. Errors & Omissions Excepted.<br/><br/>Promotional pricing is only applicable during the promotion period specified by Hewlett Packard Enterprise or other vendors; for full details please contact your account manager.', 1, 0, 'Legal', 0, 1)

insert into [Legal]([Code], [FK_Translation_Key_Name])
values ('HPELegal', @transKey)


-- Insert HPI legal text
select @transKey = @transKey + 1
insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Terms and Conditions<br/><br/>Costs are subject to change in line with vendor pricing.<br/><br/>All stock availability is subject to confirmation at time and date of order. Errors & Omissions Excepted.<br/><br/>Promotional pricing is only applicable during the promotion period specified by HP Inc. or other vendors; for full details please contact your account manager.', 1, 0, 'Legal', 0, 1)

insert into [Legal]([Code], [FK_Translation_Key_Name])
values ('HPILegal', @transKey)


commit transaction