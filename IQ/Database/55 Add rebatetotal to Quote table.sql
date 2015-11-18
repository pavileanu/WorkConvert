IF EXISTS (  SELECT *   FROM   sys.columns  WHERE  object_id = OBJECT_ID(N'[dbo].[Quote]')  AND name = 'totalrebate')
BEGIN 
ALTER TABLE [dbo].[Quote] DROP COLUMN totalrebate ;
END
Alter table [dbo].[Quote] add  totalrebate [Money] NULL;