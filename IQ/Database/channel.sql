


alter table channel add MarginType char(1) default 'R' not null
alter table channel add legal nvarchar(max) default '' not null
alter table channel add marginMin float default -.1 not null
alter table channel add marginMax float default .25 not null
alter table channel add SchemeOverride nvarchar(20) default '' not null

update channel set marginMin=0,marginmax=0,legal='Errors &amp; ommisions excepted.',schemeoverride='',margintype='R'
