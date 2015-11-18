/****** Object:  Index [IX_QuoteItem_ID_Parent]    Script Date: 13/05/2015 09:28:53 ******/
CREATE NONCLUSTERED INDEX [IX_QuoteItem_ID_Parent] ON [dbo].[QuoteItem]
(
	[FK_QuoteItem_ID_Parent] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO


