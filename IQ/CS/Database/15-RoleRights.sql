CREATE TABLE dbo.AccountRoles
	(
	FK_Account_Id int NOT NULL,
	FK_Role_Id int NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.AccountRoles ADD CONSTRAINT
	PK_AccountRoles PRIMARY KEY CLUSTERED 
	(
	FK_Account_Id,
	FK_Role_Id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.AccountRoles SET (LOCK_ESCALATION = TABLE)
GO

ALTER TABLE dbo.Account
	DROP COLUMN FK_Role_ID
GO
ALTER TABLE dbo.Account SET (LOCK_ESCALATION = TABLE)
GO

insert into accountroles select account.id,case when email='tim.moyle@channelcentral.net' then 2 else 1 end from account inner join [user] on fk_user_id=[user].id