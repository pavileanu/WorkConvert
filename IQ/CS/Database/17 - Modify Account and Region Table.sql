ALTER TABLE Account 
Add [FK_Culture_ID] [int] NOT NULL DEFAULT ((50))
GO

Alter Table Region 
Drop Column Culture 

ALTER TABLE Region  
Add [FK_Culture_ID] [int] NOT NULL DEFAULT ((50))
GO


Update Region 
Set FK_Culture_ID = Culture.ID
from culture  inner join Region on  right(culturecode , 2)  = code 
where defaultculture = 1


Update Account 
set FK_Culture_ID = r.FK_Culture_ID 
from Account a inner join Channel c on a.FK_Channel_ID_Buyer = c.ID
inner join Region r on c.FK_Region_ID = r.ID




