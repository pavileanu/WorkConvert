
-- Remove HPE/HPI
update	[Translation]
set		[Text] = 'Top Recommended'
where	[Text] = '[mfr] Top Recommended'
and		[deleted] = 0

