ALTER table Channel ADD FK_Currency_ID_Default int null
GO

update channel set FK_Currency_ID_Default = (select id from currency where code  collate database_default=currencycode  collate database_default)
from channel 
inner join h3.ChannelCentral.customers.vHostSummary ha on ha.hostid collate database_default = code collate database_default
