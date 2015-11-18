Option Strict On
Option Explicit On

Imports System.Data.SqlClient

''' <summary>
''' 
''' </summary>
''' <remarks></remarks>
Public Class clsAdvert
    Implements i_Editable
    Property ID As Integer
    Property Campaign As clsCampaign
    Property Name As String
    Property ImageUrl As String
    Property URL As String
    Property Type As Int16
    Property BasketProductBelowAbsent As String
    Property BasketProductBelowPresent As String
    Property Present As clsProductType
    Property Absent As clsProductType
    Property SlotType As clsSlotType
    Property FillThresholdPercent As Integer
    Property ImageWide As Boolean
    Property SlotTypeCode As String
    Property AdRegionPresent As clsRegion
    Property AdRegionAbsent As clsRegion
    Property Visible As Boolean

    'New for HP Split
    Property mfrCode As String


    Property Impressions As Dictionary(Of Integer, clsImpression)
    Property ClickThrus As Dictionary(Of Integer, clsClickThru)

    Private conString As String = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString

    Public Sub New()
        Impressions = New Dictionary(Of Integer, clsImpression)
        ClickThrus = New Dictionary(Of Integer, clsClickThru)

    End Sub
    Public Sub New(campaign As clsCampaign, name As String, imageurl As String, url As String, type As Int16, basketabsent As String, basketpresent As String, present As clsProductType, absent As clsProductType, slotType As clsSlotType, fillthreshold As Integer, mfrCode As String)


        Dim con As SqlConnection = New SqlConnection(conString)
        con.Open()
        Dim command As SqlCommand = New SqlCommand()
        command.CommandText = "AddAdvert"
        command.CommandType = CommandType.StoredProcedure
        command.Connection = con
        Dim paramCampaignID As New SqlParameter("@campaignid", SqlDbType.Int)
        paramCampaignID.Value = campaign.ID
        Dim paramName As New SqlParameter("@name", SqlDbType.VarChar, 100)
        paramName.Value = name
        Dim paramImageUrl As New SqlParameter("@imageurl", SqlDbType.VarChar, 255)
        paramImageUrl.Value = imageurl
        Dim paramUrl As New SqlParameter("@url", SqlDbType.VarChar, 255)
        paramUrl.Value = url
        Dim paramType As New SqlParameter("@type", SqlDbType.SmallInt)
        paramType.Value = type
        Dim paramBasketAbsent As New SqlParameter("@basket_absent", SqlDbType.NVarChar, 255)
        paramBasketAbsent.Value = basketabsent
        Dim paramBasketPresent As New SqlParameter("@basket_present", SqlDbType.NVarChar, 255)
        paramBasketPresent.Value = basketpresent
        Dim paramPresent As New SqlParameter("@prodtype_present", SqlDbType.Int)
        paramPresent.Value = present.ID
        Dim paramAbsent As New SqlParameter("@prodtype_absent", SqlDbType.Int)
        paramAbsent.Value = absent.ID
        Dim paraSlotType As New SqlParameter("@slottypeid", SqlDbType.Int)
        paraSlotType.Value = slotType.ID
        Dim paramFillThreshold As New SqlParameter("@fillthresholdpercent", SqlDbType.Int)
        paramFillThreshold.Value = fillthreshold
        Dim paramMfrCode As New SqlParameter("@mfrCode", SqlDbType.NVarChar, 3)
        paramMfrCode.Value = mfrCode

        Dim paramReturn As New SqlParameter("@ret", SqlDbType.Int)
        paramReturn.Direction = ParameterDirection.ReturnValue



        command.Parameters.Add(paramCampaignID)
        command.Parameters.Add(paramName)
        command.Parameters.Add(paramImageUrl)
        command.Parameters.Add(paramUrl)
        command.Parameters.Add(paramType)
        command.Parameters.Add(paramBasketAbsent)
        command.Parameters.Add(paramBasketPresent)
        command.Parameters.Add(paramPresent)
        command.Parameters.Add(paramAbsent)
        command.Parameters.Add(paraSlotType)
        command.Parameters.Add(paramFillThreshold)
        command.Parameters.Add(paramMfrCode)
        command.Parameters.Add(paramReturn)


        command.ExecuteNonQuery()

        con.Close()
        Me.ID = Convert.ToInt32(paramReturn.Value)
        Me.Name = name
        Me.Campaign = campaign
        Me.ImageUrl = imageurl
        Me.URL = url
        Me.Type = type
        Me.BasketProductBelowAbsent = basketabsent
        Me.BasketProductBelowPresent = basketpresent
        Me.Present = present
        Me.Absent = absent
        Me.SlotType = slotType
        Me.FillThresholdPercent = fillthreshold
        Me.mfrCode = mfrCode

        Me.Campaign.Adverts.Add(Me.ID, Me)

        Impressions = New Dictionary(Of Integer, clsImpression)
        ClickThrus = New Dictionary(Of Integer, clsClickThru)
        iq.Adverts.Add(Me.ID, Me)

    End Sub
    Public Sub New(ID As Integer, campaign As clsCampaign, name As String, imageurl As String, url As String, type As Int16, basketabsent As String, basketpresent As String, present As clsProductType, absent As clsProductType, slotType As clsSlotType, fillthreshold As Integer, imageWide As Boolean, slotTypeCode As String, adRegionPresent As clsRegion, adRegionAbsent As clsRegion, visible As Boolean, mfrCode As String)

        Me.ID = ID
        Me.Campaign = campaign
        Me.Name = name
        Me.ImageUrl = imageurl
        Me.URL = url
        Me.Type = type
        Me.BasketProductBelowAbsent = basketabsent
        Me.BasketProductBelowPresent = basketpresent
        Me.Absent = absent
        Me.Present = present
        Me.SlotType = slotType
        Me.FillThresholdPercent = fillthreshold
        Me.ImageWide = imageWide
        Me.SlotTypeCode = slotTypeCode
        Me.AdRegionPresent = adRegionPresent
        Me.AdRegionAbsent = adRegionAbsent
        Me.Visible = visible
        Me.mfrCode = mfrCode

        Me.Campaign.Adverts.Add(Me.ID, Me)
        iq.Adverts.Add(Me.ID, Me)
        Impressions = New Dictionary(Of Integer, clsImpression)
        ClickThrus = New Dictionary(Of Integer, clsClickThru)

    End Sub
    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim con As SqlConnection = New SqlConnection(conString)
        con.Open()
        Dim command As SqlCommand = New SqlCommand()

        command.CommandText = "DeleteAdvert"
        command.CommandType = CommandType.StoredProcedure

        Dim paramID As New SqlParameter("@id", SqlDbType.Int)
        paramID.Value = Me.ID

        Dim paramReturn As New SqlParameter("@return_value", SqlDbType.Int)
        paramReturn.Direction = ParameterDirection.ReturnValue

        command.Parameters.Add(paramID)

        command.Parameters.Add(paramReturn)
        command.Connection = con
        command.ExecuteNonQuery()

        For Each clickthru As clsClickThru In Me.ClickThrus.Values.ToList()
            clickthru.delete(errorMessages)
        Next
        For Each impression As clsImpression In Me.Impressions.Values.ToList()
            impression.delete(errorMessages)
        Next

        Me.Campaign.Adverts.Remove(Me.ID)

    End Sub

    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName

    End Function

    Public Function Insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert
        Return New clsAdvert(Me.Campaign, Me.Name, Me.ImageUrl, Me.URL, Me.Type, Me.BasketProductBelowAbsent, Me.BasketProductBelowPresent, Me.Present, Me.Absent, Me.SlotType, Me.FillThresholdPercent, Me.mfrCode)
    End Function

    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update
        If Me.ID > 0 Then
            Dim con As SqlConnection = New SqlConnection(conString)
            con.Open()
            Dim command As SqlCommand = New SqlCommand()
            command.CommandText = "UpdateAdvert"
            command.CommandType = CommandType.StoredProcedure
            command.Connection = con
            Dim paramID As New SqlParameter("@ID", SqlDbType.Int)
            paramID.Value = Me.ID
            Dim paramCampaignID As New SqlParameter("@campaignid", SqlDbType.Int)
            paramCampaignID.Value = Me.Campaign.ID
            Dim paramName As New SqlParameter("@name", SqlDbType.VarChar, 100)
            paramName.Value = Me.Name
            Dim paramImageUrl As New SqlParameter("@imageurl", SqlDbType.VarChar, 255)
            paramImageUrl.Value = Me.ImageUrl
            Dim paramUrl As New SqlParameter("@url", SqlDbType.VarChar, 255)
            paramUrl.Value = Me.URL
            Dim paramType As New SqlParameter("@type", SqlDbType.SmallInt)
            paramType.Value = Me.Type
            Dim paramBasketAbsent As New SqlParameter("@basket_absent", SqlDbType.NVarChar, 255)
            paramBasketAbsent.Value = Me.BasketProductBelowAbsent
            Dim paramBasketPresent As New SqlParameter("@basket_present", SqlDbType.NVarChar, 255)
            paramBasketPresent.Value = Me.BasketProductBelowPresent
            Dim paramPresent As New SqlParameter("@prodtype_present", SqlDbType.Int)
            paramPresent.Value = Me.Present.ID
            Dim paramAbsent As New SqlParameter("@prodtype_absent", SqlDbType.Int)
            paramAbsent.Value = Me.Absent.ID
            Dim paraSlotType As New SqlParameter("@slottypeid", SqlDbType.Int)
            paraSlotType.Value = Me.SlotType.ID
            Dim paramFillThreshold As New SqlParameter("@fillthresholdpercent", SqlDbType.Int)
            paramFillThreshold.Value = Me.FillThresholdPercent
            Dim paramMfrCode As New SqlParameter("@mfrCode", SqlDbType.NVarChar, 3)
            paramMfrCode.Value = Me.mfrCode

            Dim paramReturn As New SqlParameter("@ret", SqlDbType.Int)
            paramReturn.Direction = ParameterDirection.ReturnValue

            command.Parameters.Add(paramID)
            command.Parameters.Add(paramCampaignID)
            command.Parameters.Add(paramName)
            command.Parameters.Add(paramImageUrl)
            command.Parameters.Add(paramUrl)
            command.Parameters.Add(paramType)
            command.Parameters.Add(paramBasketAbsent)
            command.Parameters.Add(paramBasketPresent)
            command.Parameters.Add(paramPresent)
            command.Parameters.Add(paramAbsent)
            command.Parameters.Add(paraSlotType)
            command.Parameters.Add(paramFillThreshold)
            command.Parameters.Add(paramMfrCode)
            command.Parameters.Add(paramReturn)

            command.Connection = con

            command.ExecuteNonQuery()

            con.Close()

        End If
    End Sub

    Public ReadOnly Property Manufacturer As Manufacturer

        Get

            Manufacturer = Manufacturer.Unknown

            If Not String.IsNullOrEmpty(Me.mfrCode) Then
                If String.Equals(Me.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase) Then
                    Manufacturer = Manufacturer.HPI
                ElseIf String.Equals(Me.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase) Then
                    Manufacturer = Manufacturer.HPE
                End If
            End If

        End Get

    End Property

End Class
