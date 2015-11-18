alter table branch add deleted bit default 0 not null
alter table productAttribute add deleted bit default 0 not null
alter table slot add deleted bit default 0 not null
alter table quantity add deleted bit default 0 not null

CREATE NONCLUSTERED INDEX [qiBranch>]
ON [dbo].[QuoteItem] ([FK_Branch_ID])

CREATE NONCLUSTERED INDEX [graftSource]
ON [dbo].[graft] ([FK_Branch_ID_source])


CREATE NONCLUSTERED INDEX [graftTarget]
ON [dbo].[graft] ([FK_Branch_ID_target])


CREATE NONCLUSTERED INDEX [qtyBranch]
ON [dbo].[quantity] ([FK_Branch_ID])

CREATE NONCLUSTERED INDEX [slotBranch]
ON [dbo].[slot] ([FK_Branch_ID])


