
-- Remove HPE/HPI
update	[Translation]
set		[Text] = 'Top Recommended'
where	[Text] = '[mfr] Top Recommended'
and		[Group] = 'UI'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Más recomendado'
where	[Text] = 'Más recomendado por [mfr]'
and		[Group] = 'UI'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Recommande le plus'
where	[Text] = '[mfr] recommande le plus'
and		[Group] = 'UI'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Top-Empfehlungen'
where	[Text] = 'Top-Empfehlungen von [mfr]'
and		[Group] = 'UI'
and		[deleted] = 0

update	[Translation]
set		[Text] = N'首推'
where	[Text] = N'[mfr]首推'
and		[Group] = 'UI'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Networking'
where	[Text] = '[mfr] Networking'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Storage'
where	[Text] = '[mfr] Storage'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Redes'
where	[Text] = 'Redes [mfr]'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Almacenamiento'
where	[Text] = 'Almacenamiento [mfr]'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Más recomendado'
where	[Text] = 'Más recomendado por [mfr]'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Portátiles'
where	[Text] = 'Portátiles [mfr]'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Almacenamiento'
where	[Text] = '[mfr] Almacenamiento'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Tous les produits'
where	[Text] = 'Tous les produits [mfr]'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Networking'
where	[Text] = '[mfr] Networking'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Storage'
where	[Text] = '[mfr] Storage'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Recommande le plus'
where	[Text] = ' [mfr] recommande le plus'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Alle Produkte'
where	[Text] = 'Alle [mfr] Produkte'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Top-Empfehlungen'
where	[Text] = 'Top-Empfehlungen von [mfr]'
and		[deleted] = 0


-- Fix the Desktops label
update	[Translation]
set		[Text] = 'Desktops & Workstations'
where	[Text] = 'Desktops'
and		[Deleted] = 0
and		[Group] = 'SysTypes'
