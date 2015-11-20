-- Add mfrCode column to the Advert table
IF EXISTS (  SELECT *   FROM   sys.columns  WHERE  object_id = OBJECT_ID(N'[dbo].[Advert]')  AND name = 'mfrCode')
BEGIN 
ALTER TABLE [dbo].[Advert] DROP COLUMN mfrCode ;
END
Alter table [dbo].[Advert] add  [mfrCode] [nvarchar](3) NOT NULL CONSTRAINT [DF_Advert_mfrCode]  DEFAULT ('');


-- Allocate adverts to one side of the split (or leave blank for 'either')
update [dbo].[Advert] set [mfrCode] = 'HPE'		-- Most are HPE
update [dbo].[Advert] set [mfrCode] = '' where Name = 'CarePack'
