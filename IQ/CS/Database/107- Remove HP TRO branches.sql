Update Branch 
set deleted = 1 
from Branch b inner join Translation t 
on b.FK_Translation_key = t.[key] and FK_Language_ID = 1 
where t.[text] like '%Top Recommended'

GO

Delete from Graft where Source = 'RefreshPQWSTRO'

GO