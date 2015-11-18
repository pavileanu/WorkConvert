
alter table field add filtervisible bit CONSTRAINT df_field_filtervisible default (1) not null
GO
update field set filtervisible = 1
update field set filtervisible  = 0 where Property ='Promos(R)'