
insert into Translation 
select (select MAX([key])+1 from translation) + ROW_NUMBER() OVER (ORDER BY label),label,1,1,'FLDLBL'
from (SELECT DISTINCT label FROM Field) a left outer join translation on text collate database_default = label and translation.[group] = 'FLDLBL'
where translation.[key] is null and label is not null

UPDATE Field SET label = (select MIN([key]) from translation where text = label and [group]='FLDLBL')

ALTER TABLE Field ALTER COLUMN Label int not null

sp_RENAME 'Field.Label', 'FK_Translation_Key_Label' , 'COLUMN'