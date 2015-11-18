update branch set [order] = 10 where id=5
update branch set [order] = 20 where id=6
update branch set [order] = 30 where id=3
update branch set [order] = 40 where id=2
update branch set [order] = 50 where id=4
update branch set [order] = 60 where id=(select branch.id from branch inner join translation on branch.fk_translation_key=translation.[key] where translation.text = 'Accessories and Services')
