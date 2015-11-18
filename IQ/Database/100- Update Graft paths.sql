update Graft 
set [path] =''
 where [Source] like 'RefreshPQWS%' and [path] != ''

GO

 delete
from Prune where [Source] like 'CPQ%'

GO

update Graft 
set [path] =''
 where [Source] in ( 'TROCPQ'  ,'CPQJIT') and [path] != ''
 GO
