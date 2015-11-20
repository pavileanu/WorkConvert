create table #MissingRoles(UserId int not null, HPIId int not null, HPEId int null)

-- All HPI accounts with no AccountRoles rows
insert into	#MissingRoles (UserId, HPIId)
select		a.[FK_User_ID],
			a.[Id]
from		[Account] a
left join	[AccountRoles] ar on a.[Id] = ar.[FK_Account_Id]
where		ar.[FK_Account_Id] is null
and			a.[mfrCode] = 'HPI'

-- Find the corresponding Account Id from the HPE side
update		#MissingRoles
set			HPEId = a.[Id]
from		[Account] a
inner join	#MissingRoles mr on mr.[UserId] = a.[FK_User_ID]
where		a.[mfrCode] = 'HPE'

-- Duplicate all HPE account roles onto the HPI side
insert into	[AccountRoles] (FK_Account_Id, FK_Role_Id)
select		mr.[HPIId]
		   ,ar.[FK_Role_Id] 	
from		#MissingRoles mr
inner join	[AccountRoles] ar on ar.[FK_Account_Id] = mr.[HPEId]


drop table #MissingRoles


