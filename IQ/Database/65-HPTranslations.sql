
-- Fix hard-coded "HP" strings by either adding the [mfr] placeholder (so that HPE/HPI is substituted in at run-time), or by removing the "HP"

update	[Translation]
set		[Text] = '[mfr] Top Recommended'
where	[Text] = 'HP Top Recommended'
and		[deleted] = 0

update	[Translation]
set		[Text] = '[mfr] Networking'
where	[Text] = 'HP Networking'
and		[deleted] = 0

update	[Translation]
set		[Text] = '[mfr] Storage'
where	[Text] = 'HP Storage'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'iLO & Insight Power Management'
where	[Text] = 'HP iLO & Insight Power Management'
or		[Text] = '[mfr] iLO & Insight Power Management'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Virtual Control Enterprise Manager'
where	[Text] = 'HP Virtual Control Enterprise Manager'
or		[Text] = '[mfr] Virtual Control Enterprise Manager'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Integrated Lights-Out Management'
where	[Text] = 'HP Integrated Lights-Out Management'
or		[Text] = '[mfr] Integrated Lights-Out Management'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Insight Dynamics Virtual Server Environment'
where	[Text] = 'HP Insight Dynamics Virtual Server Environment'
or		[Text] = '[mfr] Insight Dynamics Virtual Server Environment'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Insight Management & Microsoft System Center Essentials'
where	[Text] = 'HP Insight Management & Microsoft System Center Essentials'
or		[Text] = '[mfr] Insight Management & Microsoft System Center Essentials'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Cluster Management'
where	[Text] = 'HP Cluster Management '
or		[Text] = '[mfr] Cluster Management '
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Data Protector Express'
where	[Text] = 'HP Data Protector Express'
or		[Text] = '[mfr] Data Protector Express'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Virtual Room Conferencing Software'
where	[Text] = 'HP Virtual Room Conferencing Software'
or		[Text] = '[mfr] Virtual Room Conferencing Software'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Software'
where	[Text] = 'HP Software'
or		[Text] = '[mfr] Software'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Digital Persona Pro Workgroup'
where	[Text] = 'HP Digital Persona Pro Workgroup'
or		[Text] = '[mfr] Digital Persona Pro Workgroup'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Digital Persona Pro Enterprise'
where	[Text] = 'HP Digital Persona Pro Enterprise'
or		[Text] = '[mfr] Digital Persona Pro Enterprise'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Flash / Firmware Upgrade'
where	[Text] = 'HP Flash / Firmware Upgrade'
or		[Text] = '[mfr] Flash / Firmware Upgrade'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'SkyRoom Conferencing Software'
where	[Text] = 'HP SkyRoom Conferencing Software'
or		[Text] = '[mfr] SkyRoom Conferencing Software'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'PDF Complete Corporate Edition'
where	[Text] = 'HP PDF Complete Corporate Edition'
or		[Text] = '[mfr] PDF Complete Corporate Edition'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Recovery Management Software'
where	[Text] = 'HP Recovery Management Software'
or		[Text] = '[mfr] Recovery Management Software'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Data at Rest Encryption'
where	[Text] = 'HP 3PAR Data at Rest Encryption'
or		[Text] = '[mfr] 3PAR Data at Rest Encryption'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Policy Manager'
where	[Text] = 'HP 3PAR Policy Manager'
or		[Text] = '[mfr] 3PAR Policy Manager'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Operating System'
where	[Text] = 'HP 3PAR Operating System'
or		[Text] = '[mfr] 3PAR Operating System'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Replication Software Suite'
where	[Text] = 'HP 3PAR Replication Software Suite'
or		[Text] = '[mfr] 3PAR Replication Software Suite'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Security Software Suite'
where	[Text] = 'HP 3PAR Security Software Suite'
or		[Text] = '[mfr] 3PAR Security Software Suite'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Data Optimization Suite'
where	[Text] = 'HP 3PAR Data Optimization Suite'
or		[Text] = '[mfr] 3PAR Data Optimization Suite'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Data Optimization Suite v2'
where	[Text] = 'HP 3PAR Data Optimization Suite v2'
or		[Text] = '[mfr] 3PAR Data Optimization Suite v2'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Reporting Software Suite'
where	[Text] = 'HP 3PAR Reporting Software Suite'
or		[Text] = '[mfr] 3PAR Reporting Software Suite'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Application Software Suite for Oracle'
where	[Text] = 'HP 3PAR Application Software Suite for Oracle'
or		[Text] = '[mfr] 3PAR Application Software Suite for Oracle'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Application Software Suite for SQL'
where	[Text] = 'HP 3PAR Application Software Suite for SQL'
or		[Text] = '[mfr] 3PAR Application Software Suite for SQL'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Application Software Suite for Exchange'
where	[Text] = 'HP 3PAR Application Software Suite for Exchange'
or		[Text] = '[mfr] 3PAR Application Software Suite for Exchange'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Application Software Suite for Hyper-V'
where	[Text] = 'HP 3PAR Application Software Suite for Hyper-V'
or		[Text] = '[mfr] 3PAR Application Software Suite for Hyper-V'
and		[deleted] = 0

update	[Translation]
set		[Text] = '3PAR Application Software Suite for VMware'
where	[Text] = 'HP 3PAR Application Software Suite for VMware'
or		[Text] = '[mfr] 3PAR Application Software Suite for VMware'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Cloud Network Management'
where	[Text] = 'HP Cloud Network Management'
or		[Text] = '[mfr] Cloud Network Management'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Redes [mfr]'
where	[Text] = 'Redes HP'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Almacenamiento [mfr]'
where	[Text] = 'Almacenamiento HP'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Más recomendado por [mfr]'
where	[Text] = 'Más recomendado por HP'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Portátiles [mfr]'
where	[Text] = 'Portátiles HP'
and		[deleted] = 0

update	[Translation]
set		[Text] = '[mfr] Almacenamiento'
where	[Text] = 'HP Almacenamiento'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Tous les produits [mfr]'
where	[Text] = 'Tous les produits HP'
and		[deleted] = 0

update	[Translation]
set		[Text] = '[mfr] Networking'
where	[Text] = 'HP Networking'
and		[deleted] = 0

update	[Translation]
set		[Text] = '[mfr] Storage'
where	[Text] = 'HP Storage'
and		[deleted] = 0

update	[Translation]
set		[Text] = ' [mfr] recommande le plus'
where	[Text] = ' HP recommande le plus'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Alle [mfr] Produkte'
where	[Text] = 'Alle HP Produkte'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Top-Empfehlungen von [mfr]'
where	[Text] = 'Top-Empfehlungen von HP'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Rapid Deployment Pack'
where	[Text] = 'HP Rapid Deployment Pack'
or		[Text] = '[mfr] Rapid Deployment Pack'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Insight Control'
where	[Text] = 'HP Insight Control'
or		[Text] = '[mfr] Insight Control'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'Virtual Control Enterprise Manager'
where	[Text] = 'HP Virtual Control Enterprise Manager'
or		[Text] = '[mfr] Virtual Control Enterprise Manager'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'RAID Storage Management'
where	[Text] = 'HP RAID Storage Management'
or		[Text] = '[mfr] RAID Storage Management'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'IntelliJack Gigabit Switch Series'
where	[Text] = 'HP IntelliJack Gigabit Switch Series'
or		[Text] = '[mfr] IntelliJack Gigabit Switch Series'
and		[deleted] = 0

update	[Translation]
set		[Text] = N'笔记本电脑'
where	[Text] = N'HP 笔记本电脑'
or		[Text] = N'[mfr] 笔记本电脑'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'offers OEM versions of several operating systems including Microsoft Windows Server Reseller Option Kit.'
where	[Text] = 'HP offers OEM versions of several operating systems including Microsoft Windows Server Reseller Option Kit'
or		[Text] = '[mfr] offers OEM versions of several operating systems including Microsoft Windows Server Reseller Option Kit.'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'For best performance on many workloads, 10Gb network adaptors are recommended. Select an Intel 10Gb option.'
where	[Text] = 'For best performance on many workloads, HP recommends 10Gb network adaptors.  Select an Intel 10Gb option'
or		[Text] = 'For best performance on many workloads, [mfr] recommends 10Gb network adaptors. Select an Intel 10Gb option.'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'OneView or an iLO licence is recommended for the best management experience.'
where	[Text] = 'HP recommends HP OneView or an iLO licence for the best management experience'
or		[Text] = '[mfr] recommends [mfr] OneView or an iLO licence for the best management experience.'
and		[deleted] = 0

update	[Translation]
set		[Text] = 'For best performance on many workloads, Solid State Drives are recommended for local storage. Select an SSD.'
where	[Text] = 'For best performance on many workloads, HP recommends Solid State Drives for local storage  Select an SSD'
or		[Text] = 'For best performance on many workloads, [mfr] recommends Solid State Drives for local storage. Select an SSD.'
and		[deleted] = 0

