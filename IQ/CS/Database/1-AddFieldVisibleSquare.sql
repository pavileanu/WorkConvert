
ALTER Table Field ADD [VisibleSquare] [bit] NOT NULL CONSTRAINT [DF_Field_VisibleSquare]  DEFAULT ((0))

update field set VisibleSquare = 1 WHERE id IN 
(
1739,
1740,
1751,
1896,
1894,
1955
)
insert into field ( [FK_Screen_ID]
      ,[Property]
      ,[Label]
      ,[HelpText]
      ,[length]
      ,[Order]
      ,[Width]
      ,[DefaultValue]
      ,[VisibleList]
      ,[VisiblePage]
      ,[Height]
      ,[DefaultFilter]
      ,[DefaultSort]
      ,[Priority]
      ,[WidgetUI]
      ,[VisibleSquare]
      ,[CanUserSelect],
	  FK_InputType_ID )
	  VALUES (705,'SLOTS(RAM)','RAM Slots','',10,0,10,'0',0,0,30,'','',0,'',1,1,519)