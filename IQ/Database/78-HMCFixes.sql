
-- Ensure correct order for the Help Me Choose/Windows screens
update [Branch]
set [order] = 0
where FK_Screen_ID_Matrix = 1813
  
update [Branch]
set [order] = 1
where FK_Screen_ID_Matrix = 1814