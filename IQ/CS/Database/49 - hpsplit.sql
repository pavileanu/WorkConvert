

alter table product add sku nvarchar(50) default '' not null
alter table product add mfrCode nvarchar(3) default '' not null
alter table product add buCode nvarchar(3) default '' not null
alter table product add plCode nvarchar(3) default '' not null
alter table account add mfrCode nvarchar(3) default '' not null


/* you need to Press the 'HPSplit(manufacturers) button on your default page on your database 