Truncate table [ServiceLevelAttributeMap]
GO
SET IDENTITY_INSERT [dbo].[ServiceLevelAttributeMap] ON 

INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (1, N'FK_ServiceType_ID', N'servicelevel')
INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (2, N'FK_Response_ID', N'response')
INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (3, N'hpeDMR', N'DMR_ISS')
INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (4, N'hpeCDMR', N'DMR_ISS')
INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (5, N'hpiADP', N'ADP')
INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (6, N'hpiDMR', N'DMR')
INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (7, N'hpiTravel', N'travel')
INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (8, N'hpiTracing', N'tracing')
INSERT [dbo].[ServiceLevelAttributeMap] ([ID], [Code], [FK_Attribute_Code]) VALUES (9, N'hpiTheft', N'theft')
SET IDENTITY_INSERT [dbo].[ServiceLevelAttributeMap] OFF
