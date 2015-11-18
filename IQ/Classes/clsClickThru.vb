Option Strict On
Option Explicit On

Imports System.Data.SqlClient

''' <summary>
''' 
''' </summary>
''' <remarks></remarks>

Public Class clsClickThru
    Implements i_Editable

    Property ID As Integer
    Property Account As clsAccount
    Property Advert As clsAdvert
    Property TimeStamp As Date
    Private conString As String = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString

    Public Sub New()


    End Sub
    Public Sub New(account As clsAccount, advert As clsAdvert, timestamp As Date)

        Dim con As SqlConnection = New SqlConnection(conString)
        con.Open()
        Dim command As SqlCommand = New SqlCommand()
        command.CommandText = "AddClickThru"
        command.CommandType = CommandType.StoredProcedure
        command.Connection = con
        Dim paramUserID As New SqlParameter("@accountid", SqlDbType.Int)
        paramUserID.Value = account.ID
        Dim paramAdvertID As New SqlParameter("@advertid", SqlDbType.Int)
        paramAdvertID.Value = advert.ID
        Dim paramTimeStamp As New SqlParameter("@timestamp", SqlDbType.DateTime)
        paramTimeStamp.Value = timestamp

        Dim paramReturn As New SqlParameter("@ret", SqlDbType.Int)
        paramReturn.Direction = ParameterDirection.ReturnValue

        command.Parameters.Add(paramUserID)
        command.Parameters.Add(paramAdvertID)
        command.Parameters.Add(paramTimeStamp)
        command.Parameters.Add(paramReturn)

        command.ExecuteNonQuery()

        con.Close()

        Me.ID = Convert.ToInt32(paramReturn.Value)
        Me.Account = account
        Me.Advert = advert
        Me.TimeStamp = timestamp

        Me.Advert.ClickThrus.Add(Me.ID, Me)
        Me.Account.ClickThrus.Add(Me.ID, Me)

    End Sub
    Public Sub New(ID As Integer, user As clsUser, advert As clsAdvert, timestamp As Date)
        Me.ID = ID
        Me.Account = Account
        Me.Advert = advert
        Me.TimeStamp = timestamp

        Me.Advert.ClickThrus.Add(Me.ID, Me)
        Me.Account.ClickThrus.Add(Me.ID, Me)

    End Sub
    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete
        Me.Advert.ClickThrus.Remove(Me.ID)
        Me.Account.ClickThrus.Remove(Me.ID)
    End Sub

    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName

    End Function

    Public Function Insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert
        Return New clsClickThru(Me.Account, Me.Advert, Me.TimeStamp)
    End Function

    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update
        If Me.ID > 0 Then

            Dim con As SqlConnection = New SqlConnection(conString)
            con.Open()
            Dim command As SqlCommand = New SqlCommand()
            command.CommandText = "UpdateClickThru"
            command.CommandType = CommandType.StoredProcedure
            command.Connection = con
            Dim paramID As New SqlParameter("@ID", SqlDbType.Int)
            paramID.Value = Me.ID
            Dim paramUserID As New SqlParameter("@accountid", SqlDbType.Int)
            paramUserID.Value = Account.ID
            Dim paramAdvertID As New SqlParameter("@adverid", SqlDbType.Int)
            paramAdvertID.Value = Advert.ID
            Dim paramTimeStamp As New SqlParameter("@url", SqlDbType.DateTime)
            paramTimeStamp.Value = TimeStamp

            Dim paramReturn As New SqlParameter("@ret", SqlDbType.Int)
            paramReturn.Direction = ParameterDirection.ReturnValue

            command.Parameters.Add(paramUserID)
            command.Parameters.Add(paramAdvertID)
            command.Parameters.Add(paramTimeStamp)
            command.Parameters.Add(paramReturn)

            command.ExecuteNonQuery()

            con.Close()
        End If
    End Sub
End Class
