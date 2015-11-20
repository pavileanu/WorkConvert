
if not exists(select * from sys.columns 
            where Name = N'Code_HP' and Object_ID = Object_ID(N'Currency'))
begin
ALTER TABLE dbo.Currency ADD
	Code_HP char(2) NULL
end
GO

update [Currency] set [Code_HP] = 'NK' where code = 'NOK'
update [Currency] set [Code_HP] = 'CK' where code = 'CZK'
update [Currency] set [Code_HP] = 'EC' where code = 'EUR'
update [Currency] set [Code_HP] = 'BP' where code = 'GBP'
update [Currency] set [Code_HP] = 'PZ' where code = 'PLN'
update [Currency] set [Code_HP] = 'RR' where code = 'RUB'
update [Currency] set [Code_HP] = 'SK' where code = 'SEK'
update [Currency] set [Code_HP] = 'SF' where code = 'SFR'
update [Currency] set [Code_HP] = 'UD' where code = 'USD'
update [Currency] set [Code_HP] = 'RD' where code = 'ZAR'
GO

