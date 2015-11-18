BEGIN TRAN

DECLARE @temp TABLE
(
	ID int identity(1,1),
	hostId varchar(14) not null	
)

INSERT INTO @temp
SELECT HostID  FROM H3.ChannelCentral.customers.Host_Properties x 
	WHERE x.[BinaryStock]=1

DECLARE @id INT
DECLARE @hostid  VARCHAR(14)
WHILE (SELECT Count(*) FROM @temp) > 0
BEGIN
      SELECT TOP 1 @hostid= hostId, @id=ID FROM @temp
	  UPDATE [dbo].[Channel] SET PriceConfig = (PriceConfig | 16) WHERE Channel.Code =@hostid
	  DELETE FROM @temp WHERE ID =@Id

END
--TEST SQL
--INSERT INTO @temp
--SELECT x.HostID  FROM H3.ChannelCentral.customers.Host_Properties x 
--	WHERE x.[BinaryStock]=1

--SELECT x.Code,x.PriceConfig  FROM [dbo].[Channel]x 
--INNER JOIN @temp t on x.Code= t.Hostid
	
DELETE from @temp	
Commit tran