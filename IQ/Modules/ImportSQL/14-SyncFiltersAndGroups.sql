update t2 set t2.text=t1.text 
--select field.label,t1.text,fk_screen_id,field.id 
from translation t1
inner join field on field.FK_Translation_Key_WidgetGroup = t1.[key]
inner join translation t2 on t2.[key] = field.fk_translation_key_label
where t2.text COLLATE Latin1_General_CS_AS <> t1.text COLLATE Latin1_General_CS_AS and fk_screen_id > 721 and fk_screen_id in (select distinct FK_Screen_ID_matrix from branch)
