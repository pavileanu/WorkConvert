SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Message](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[FK_Translation_key_Name] [int] NOT NULL,
	[ValidFrom] [datetime] NOT NULL,
	[ValidTo] [datetime] NOT NULL,
	[FK_Channel_ID] [int] NOT NULL,
	[Enabled] [bit] NOT NULL CONSTRAINT [DF_Message_Enabled]  DEFAULT ((1)),
 CONSTRAINT [PK_Message] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Message', @level2type=N'COLUMN',@level2name=N'ID'
GO

declare @transKey int
select @transKey = max([key]) + 1 from translation

insert into [Translation]([Key], [Text], [FK_Language_ID], [Order], [Group], [HPonly], [TranslateThis])
values (@transKey, 'Access sign in system message admin', 1, 0, 'Rights', 0, 1)

insert into [Right]([code], [FK_translation_key])
values ('SYSMESSAGE', @transKey)
GO

-- Give Admin the new SYSMESSAGE right
insert into [RoleRight]([FK_Role_id], [FK_Right_id])
values (1, @@IDENTITY)	
GO

