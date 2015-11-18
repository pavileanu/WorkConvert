
Option Strict On
Option Explicit On

Imports System.Data.SqlClient
''' <summary>
''' 
''' </summary>
''' <remarks></remarks>
Public Class clsCampaign
    Implements i_Editable

    Property ID As Integer
    Property Name As String
    Property Advertiser As clsChannel
    Property Region As clsRegion
    Property Seller As clsChannel
    Property Buyer As clsChannel
    Property StartDate As Date
    Property EndDate As Date

    Property Adverts As Dictionary(Of Integer, clsAdvert)
    Private conString As String = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString

    Public Sub New()
        Me.Adverts = New Dictionary(Of Integer, clsAdvert)

    End Sub

    Public Sub New(name As String, advertiser As clsChannel, region As clsRegion, seller As clsChannel, buyer As clsChannel, startdate As Date, enddate As Date)

        Me.Adverts = New Dictionary(Of Integer, clsAdvert)

        Dim con As SqlConnection = New SqlConnection(conString)
        con.Open()
        Dim command As SqlCommand = New SqlCommand()

        command.CommandText = "AddCampaign"
        command.CommandType = CommandType.StoredProcedure

        Dim paramName As New SqlParameter("@name", SqlDbType.VarChar, 100)
        paramName.Value = name
        Dim paramAdvertID As New SqlParameter("@advertiserid", SqlDbType.Int)
        paramAdvertID.Value = advertiser.ID
        Dim paramRegionID As New SqlParameter("@regionid", SqlDbType.Int)
        paramRegionID.Value = region.ID
        Dim paramSellerID As New SqlParameter("@sellerid", SqlDbType.Int)
        paramSellerID.Value = seller.ID
        Dim paramBuyerID As New SqlParameter("@buyerid", SqlDbType.Int)
        paramBuyerID.Value = buyer.ID
        Dim paramStartDate As New SqlParameter("@startdate", SqlDbType.DateTime)
        paramStartDate.Value = startdate
        Dim paramEndDate As New SqlParameter("@enddate", SqlDbType.DateTime)
        paramEndDate.Value = enddate

        Dim paramReturn As New SqlParameter("@return_value", SqlDbType.Int)
        paramReturn.Direction = ParameterDirection.ReturnValue

        command.Parameters.Add(paramName)
        command.Parameters.Add(paramAdvertID)
        command.Parameters.Add(paramRegionID)
        command.Parameters.Add(paramSellerID)
        command.Parameters.Add(paramBuyerID)
        command.Parameters.Add(paramStartDate)
        command.Parameters.Add(paramEndDate)
        command.Parameters.Add(paramReturn)
        command.Connection = con
        command.ExecuteNonQuery()


        Me.ID = Convert.ToInt32(paramReturn.Value)
        Me.Name = name
        Me.Advertiser = advertiser
        Me.Region = region
        Me.Seller = seller
        Me.Buyer = buyer
        Me.StartDate = startdate
        Me.EndDate = enddate

        Me.Advertiser.Campaigns.Add(Me.ID, Me)
        con.Close()


    End Sub

    Public Sub New(ID As Integer, name As String, advertiser As clsChannel, region As clsRegion, seller As clsChannel, buyer As clsChannel, startdate As Date, enddate As Date)

        Me.Adverts = New Dictionary(Of Integer, clsAdvert)

        Me.ID = ID
        Me.Name = name
        Me.Advertiser = advertiser
        Me.Region = region
        Me.Seller = seller
        Me.Buyer = buyer
        Me.StartDate = startdate
        Me.EndDate = enddate

        Me.Advertiser.Campaigns.Add(Me.ID, Me)
    End Sub


    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete


        Dim con As SqlConnection = New SqlConnection(conString)
        con.Open()
        Dim command As SqlCommand = New SqlCommand()

        command.CommandText = "DeleteCampaign"
        command.CommandType = CommandType.StoredProcedure

        Dim paramID As New SqlParameter("@id", SqlDbType.Int)
        paramID.Value = Me.ID

        Dim paramReturn As New SqlParameter("@return_value", SqlDbType.Int)
        paramReturn.Direction = ParameterDirection.ReturnValue

        command.Parameters.Add(paramID)

        command.Parameters.Add(paramReturn)
        command.Connection = con
        command.ExecuteNonQuery()
        For Each adv As clsAdvert In Me.Adverts.Values.ToList()
            adv.delete(errorMessages)
        Next
        Me.Advertiser.Campaigns.Remove(Me.ID)

    End Sub

    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName

    End Function

    Public Function Insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsCampaign(Me.Name, Me.Advertiser, Me.Region, Me.Seller, Me.Buyer, Me.StartDate, Me.EndDate)

    End Function

    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update
        If Me.ID > 0 Then
            Dim con As SqlConnection = New SqlConnection(conString)
            con.Open()
            Dim command As SqlCommand = New SqlCommand()

            command.CommandText = "UpdateCampaign"
            command.CommandType = CommandType.StoredProcedure

            Dim paramID As New SqlParameter("@ID", SqlDbType.Int)
            paramID.Value = Me.ID
            Dim paramName As New SqlParameter("@name", SqlDbType.VarChar, 100)
            paramName.Value = Me.Name
            Dim paramAdvertID As New SqlParameter("@advertiserid", SqlDbType.Int)
            paramAdvertID.Value = Me.Advertiser.ID
            Dim paramRegionID As New SqlParameter("@regionid", SqlDbType.Int)
            paramRegionID.Value = Me.Region.ID
            Dim paramSellerID As New SqlParameter("@sellerid", SqlDbType.Int)
            paramSellerID.Value = Me.Seller.ID
            Dim paramBuyerID As New SqlParameter("@buyerid", SqlDbType.Int)
            paramBuyerID.Value = Me.Buyer.ID
            Dim paramStartDate As New SqlParameter("@startdate", SqlDbType.DateTime)
            paramStartDate.Value = Me.StartDate
            Dim paramEndDate As New SqlParameter("@enddate", SqlDbType.DateTime)
            paramEndDate.Value = Me.EndDate

            Dim paramReturn As New SqlParameter("@return_value", SqlDbType.Int)
            paramReturn.Direction = ParameterDirection.ReturnValue

            command.Parameters.Add(paramID)
            command.Parameters.Add(paramName)
            command.Parameters.Add(paramAdvertID)
            command.Parameters.Add(paramRegionID)
            command.Parameters.Add(paramSellerID)
            command.Parameters.Add(paramBuyerID)
            command.Parameters.Add(paramStartDate)
            command.Parameters.Add(paramEndDate)
            command.Parameters.Add(paramReturn)
            command.Connection = con
            command.ExecuteNonQuery()


            con.Close()


        End If
    End Sub
End Class
