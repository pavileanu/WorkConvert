alter table margin drop column fk_producttype_id
alter table margin add priceband nvarchar(10) not null default ''