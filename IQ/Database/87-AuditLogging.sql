
CREATE TABLE [dbo].[AuditLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DateTime] [datetime] NOT NULL,
	[lid] [decimal](38, 0) NOT NULL,
	[Action] [varchar](50) NOT NULL,
	[SourcePath] [varchar](200) NOT NULL,
	[TargetPath] [varchar](200) NULL,
	[ErrorMessage] [varchar](max) NULL,
	[ExceptionDetails] [varchar](max) NULL,
	[PageName] [varchar](250) NULL,
	[SourceURL] [varchar](max) NULL,
	[TimeToLoad] [float] NULL,
	[HttpRequestMethod] [varchar](50) NULL,
	[SourceIP] [varchar](50) NULL,
	[UrlReferrer] [varchar](max) NULL,
 CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE Field ADD FK_Field_ID_Linked int

ALTER TABLE [Login] ADD lid decimal(38,0)
ALTER TABLE [Login] ADD FK_UserAgent_ID int
ALTER TABLE [Login] ADD ServerNode varchar(200)

GO

CREATE PROCEDURE [dbo].[usp_GetLoggingTree]
	-- Add the parameters for the stored procedure here
	@lid bigint,
	@ParentId int 
AS
BEGIN
	SELECT child.id as ChildId,(select top 1 id from auditlog parent where 'http://localhost:17540' + parent.SourceURL = child.UrlReferrer and child.urlreferrer <> '' and parent.lid=child.lid and parent.lid <> 0 and parent.id<child.id order by id desc) as ParentId,*
	FROM AuditLog child 
	WHERE 
	CASE WHEN lid IS NULL THEN child.lid else lid END = child.lid AND 
	case when @parentId IS NOT NULL THEN (select top 1 id from auditlog parent where 'http://localhost:17540' + parent.SourceURL = child.UrlReferrer and child.urlreferrer <> '' and parent.lid=child.lid and parent.lid <> 0 and parent.id<child.id order by id desc) ELSE -1 END = CASE WHEN @ParentId IS NULL THEN -1 ELSE @ParentId END AND
	CASE WHEN @ParentId IS NULL THEN (select top 1 id from auditlog parent where 'http://localhost:17540' + parent.SourceURL = child.UrlReferrer and child.urlreferrer <> '' and parent.lid=child.lid and parent.lid <> 0 and parent.id<child.id order by id desc) ELSE NULL END IS NULL 
	
	and lid<>0

END

GO
CREATE TABLE [dbo].[UserAgents](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AgentName] [varchar](200) NOT NULL,
 CONSTRAINT [PK_UserAgents] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE PROCEDURE [dbo].[usp_UpdateUserAgentList]
	-- Add the parameters for the stored procedure here
	@AgentName varchar(200) 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	if (select count(*) FROM UserAgents WHERE AgentName = @AgentName) = 0  begin
		INSERT INTO UserAgents (AgentName) VALUES (@AgentName)
	end
END

