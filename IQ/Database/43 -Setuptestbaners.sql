
Delete Impression
Go
Delete ClicKThru
GO
Delete Advert
GO
Delete  Campaign
GO
Declare @channelID int

Select @channelID = ID from Channel where Code = 'DWERG74AH'


Declare @CampaignID int


INSERT [dbo].[Campaign] ([Name], [FK_Channel_ID_Advertiser], [FK_Region_ID], [FK_Channel_ID_Seller], [FK_Channel_ID_Buyer], [StartDate], [EndDate]) VALUES ( N'TestCampaign', @channelID, 1, @channelID, @channelID, GETDATE(), DATEADD(yy,1,GETDATE()))


Select @CampaignID = ID from Campaign where [FK_Channel_ID_Advertiser] = @channelID

Select @CampaignID

INSERT [dbo].[Advert] ([FK_Campaign_ID], [Name], [URL], [Type], [BasketProductBelowAbsent], [BasketProductBelowPresent], [FK_ProdType_Present], [FK_ProdType_Absent], [FK_SlotType_ID], [FillThresholdPercent], [imageurl], [imagewide], [SlotTypeCode], [Visible], [FK_Region_Id_Present], [FK_Region_Id_Absent]) VALUES ( @CampaignID, N'Microserver', N'JAVASCRIPT|MICROSERVER', 1, NULL, N'100', 12, 1, 288, 10, N'http://www.channelcentral.net/images/banners/BANNER_HP_MicroserverSwitch_240x120.jpg', 0, NULL, 1, 1, NULL)

INSERT [dbo].[Advert] ([FK_Campaign_ID], [Name], [URL], [Type], [BasketProductBelowAbsent], [BasketProductBelowPresent], [FK_ProdType_Present], [FK_ProdType_Absent], [FK_SlotType_ID], [FillThresholdPercent], [imageurl], [imagewide], [SlotTypeCode], [Visible], [FK_Region_Id_Present], [FK_Region_Id_Absent]) VALUES ( @CampaignID, N'Financial', N'http://www8.hp.com/uk/en/hp-financial-services/index.html#.UayjcyLD9zM', 1, NULL, N'100', 12, 1, 288, 10, N'http://www.channelcentral.net/images/banners/BANNER_HPFS_EMEA.png', 0, NULL, 1, 1, NULL)

INSERT [dbo].[Advert] ([FK_Campaign_ID], [Name], [URL], [Type], [BasketProductBelowAbsent], [BasketProductBelowPresent], [FK_ProdType_Present], [FK_ProdType_Absent], [FK_SlotType_ID], [FillThresholdPercent], [imageurl], [imagewide], [SlotTypeCode], [Visible], [FK_Region_Id_Present], [FK_Region_Id_Absent]) VALUES ( @CampaignID, N'CarePack', N'http://www8.hp.com/us/en/business-services/index.html#.VGyC3WcfzDc', 1, NULL, N'100', 12, 1, 288, 10, N'http://www.channelcentral.net/images/banners/BANNER_HP_CarePacks_en.gif', 0, NULL, 1, 1, NULL)

INSERT [dbo].[Advert] ([FK_Campaign_ID], [Name], [URL], [Type], [BasketProductBelowAbsent], [BasketProductBelowPresent], [FK_ProdType_Present], [FK_ProdType_Absent], [FK_SlotType_ID], [FillThresholdPercent], [imageurl], [imagewide], [SlotTypeCode], [Visible], [FK_Region_Id_Present], [FK_Region_Id_Absent]) VALUES ( @CampaignID, N'Insight', N'http://h17007.www1.hp.com/us/en/enterprise/servers/management/insight-control/index.aspx#.VGyCDGcfzDc', 1, NULL, N'100', 27, 1, 288, 10, N'http://www.channelcentral.net/images/banners/BANNER_HP_CarePacks_en.gif', 1, N'ILO', 1, 1, NULL)

INSERT [dbo].[Advert] ([FK_Campaign_ID], [Name], [URL], [Type], [BasketProductBelowAbsent], [BasketProductBelowPresent], [FK_ProdType_Present], [FK_ProdType_Absent], [FK_SlotType_ID], [FillThresholdPercent], [imageurl], [imagewide], [SlotTypeCode], [Visible], [FK_Region_Id_Present], [FK_Region_Id_Absent]) VALUES ( @CampaignID, N'Emulex1', N'JAVASCRIPT|EMULEX1', 1, NULL, N'100', 110, 1, 288, 10, N'http://www.channelcentral.net/images/banners/Emulex/BANNER_Emulex_16gb.jpg', 1, NULL, 1, 1, NULL)

INSERT [dbo].[Advert] ([FK_Campaign_ID], [Name], [URL], [Type], [BasketProductBelowAbsent], [BasketProductBelowPresent], [FK_ProdType_Present], [FK_ProdType_Absent], [FK_SlotType_ID], [FillThresholdPercent], [imageurl], [imagewide], [SlotTypeCode], [Visible], [FK_Region_Id_Present], [FK_Region_Id_Absent]) VALUES ( @CampaignID, N'Emulex2', N'JAVASCRIPT|EMULEX2', 1, NULL, N'100', 110, 1, 288, 10, N'http://www.channelcentral.net/images/banners/Emulex/BANNER_Emulex_10gbe.jpg', 1, NULL, 1, 1, NULL)

INSERT [dbo].[Advert] ([FK_Campaign_ID], [Name], [URL], [Type], [BasketProductBelowAbsent], [BasketProductBelowPresent], [FK_ProdType_Present], [FK_ProdType_Absent], [FK_SlotType_ID], [FillThresholdPercent], [imageurl], [imagewide], [SlotTypeCode], [Visible], [FK_Region_Id_Present], [FK_Region_Id_Absent]) VALUES ( @CampaignID, N'UPS', N'JAVASCRIPT|UPS', 1, NULL, N'100', 110, 1, 288, 10, N'http://www.channelcentral.net/images/banners/BANNER_HP_UPStool.gif', 1, NULL, 1, 1, NULL)

INSERT [dbo].[Advert] ([FK_Campaign_ID], [Name], [URL], [Type], [BasketProductBelowAbsent], [BasketProductBelowPresent], [FK_ProdType_Present], [FK_ProdType_Absent], [FK_SlotType_ID], [FillThresholdPercent], [imageurl], [imagewide], [SlotTypeCode], [Visible], [FK_Region_Id_Present], [FK_Region_Id_Absent]) VALUES ( @CampaignID, N'ROK', N'JAVASCRIPT|ROK', 1, NULL, N'100', 110, 83, 288, 10, N'http://www.channelcentral.net/images/banners/BANNER_HP_ROK.png', 1, NULL, 1, 1, NULL)
GO
