IF OBJECT_ID(N'dbo.Universal', N'U') IS NULL
BEGIN

	CREATE TABLE [dbo].[Universal](
		[ID] [int] IDENTITY(1,1) NOT NULL,
		[Name] [nvarchar](50) NOT NULL,
		[IQ2] [bit] NOT NULL,
		[IQ1HID] [int] NOT NULL,
		[IQ2Host] [nvarchar](20) NOT NULL,
		[Enabled] [bit] NOT NULL,
	CONSTRAINT [PK_Universal] PRIMARY KEY CLUSTERED 
	(
		[ID] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

END
go

delete from [universal]
go

 insert into [Universal] ([Name], [IQ2], [IQ1HID], [IQ2Host], [Enabled])
values
('Argentina', 0, 387, 'MHPAR24U', 1),
('Australia', 0, 645, 'MHPAU3130U', 1),
('Austria', 0, 370, 'MHPAT1120U', 1),
('Belgium', 0, 231, 'MHPBE1831U', 1),
('Bosnia', 0, 304, 'MHPBA1000U', 1),
('Brazil', 0, 394, 'MHPBR24U', 1),
('Canada', 0, 239, 'MHPCAL4W5G1U', 1),
('Chile', 0, 523, 'MHPCL310U', 1),
('China', 0, 788, 'MHPCN100022U', 0),
('Colombia', 0, 524, 'MHPCOBOG7U', 1),
('Croatia', 0, 306, 'MHPHR10000U', 1),
('Czech Republic', 0, 416, 'MHPCZ1410U', 1),
('Denmark', 0, 238, 'MHPDK3450U', 1),
('Finland', 0, 237, 'MHPFI487136U', 1),
('France', 0, 212, 'MHPFR92024U', 1),
('Germany', 0, 226, 'MHPDE71034U', 1),
('Greece', 0, 287, 'MHPGR15231U', 1),
('Hungary', 0, 307, 'MHPHU1117U', 1),
('India', 0, 703, 'MHPIN560030U', 0),
('Ireland', 0, 305, 'MHPBT370ZRU', 1),
('Italy', 0, 235, 'MHPIT20063U', 1),
('Kenya', 0, 308, 'MHPKE2144U', 1),
('Luxembourg', 0, 232, 'MHPLU1831U', 1),
('Macedonia', 0, 309, 'MHPMK24U', 1),
('Malta', 0, 310, 'MHPMT24U', 1),
('Mexico', 0, 311, 'MHPMX1210U', 1),
('Netherlands', 0, 230, 'MHPNL2909U', 1),
('New Zealand', 0, 690, 'MHPNZ1001U', 1),
('Norway', 0, 236, 'MHPNO0609U', 1),
('Peru', 0, 312, 'MHPPE147U', 1),
('Poland', 0, 234, 'MHPPL50257U', 1),
('Portugal', 0, 233, 'MHPPT2770U', 1),
('Russian Federation', 0, 313, 'MHPRU125171U', 1),
('South Africa', 0, 316, 'MHPZA2144U', 1),
('Spain', 0, 228, 'MHPES28232U', 1),
('Sweden', 0, 229, 'MHPSE16985U', 1),
('Switzerland', 0, 227, 'MHPCH8600U', 1),
('Tanzania', 0, 315, 'MHPTZ2144U', 1),
('Turkey', 0, 314, 'MHPTR6510U', 1),
('Ukraine', 0, 525, 'MHPUA01032U', 1),
('United Arab Emirates', 0, 284, 'MHPAE11598U', 1),
('United Kingdom', 0, 243, 'MHPRG121HNU', 1),
('USA', 0, 216, 'MHPUS77070U', 1),
('Venezuela', 0, 386, 'MHPVE24U', 1)
go