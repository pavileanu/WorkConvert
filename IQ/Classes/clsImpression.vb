Option Strict On
Option Explicit On

Imports System.Data.SqlClient

''' <summary>
''' 
''' </summary>
''' <remarks></remarks>

Public Class clsImpression
    Implements i_Editable
    Property ID As Integer
    Property Advert As clsAdvert
    Property Account As clsAccount
    Property Count As Integer
    Property IDate As Date
    Private conString As String = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString

    Public Sub New()

    End Sub
    Public Sub New(account As clsAccount, advert As clsAdvert, timestamp As Date)

        If (advert IsNot Nothing) Then

            Dim con As SqlConnection = New SqlConnection(conString)
            con.Open()
            Dim command As SqlCommand = New SqlCommand()
            command.CommandText = "Addimpression"
            command.CommandType = CommandType.StoredProcedure
            command.Connection = con
            Dim paramAdvertID As New SqlParameter("@advertid", SqlDbType.Int)
            paramAdvertID.Value = advert.ID
            Dim paramAccountID As New SqlParameter("@accountid", SqlDbType.Int)
            paramAccountID.Value = account.ID
            Dim paramTimeStamp As New SqlParameter("@impDate", SqlDbType.DateTime)
            paramTimeStamp.Value = timestamp

            Dim paramReturn As New SqlParameter("@ret", SqlDbType.Int)
            paramReturn.Direction = ParameterDirection.ReturnValue

            command.Parameters.Add(paramAccountID)
            command.Parameters.Add(paramAdvertID)
            command.Parameters.Add(paramTimeStamp)
            command.Parameters.Add(paramReturn)

            command.ExecuteNonQuery()

            con.Close()

            Me.ID = Convert.ToInt32(paramReturn.Value)
            Me.Account = account
            Me.Advert = advert
            Me.IDate = timestamp
            Me.Count = 1
            Me.Advert.Impressions.Add(Me.ID, Me)
            Me.Account.Impressions.Add(Me.ID, Me)

        End If
    End Sub

    Public Sub New(ID As Integer, account As clsAccount, advert As clsAdvert, timestamp As Date)
        Me.ID = ID
        Me.Account = account
        Me.Advert = advert
        Me.IDate = timestamp
        Me.Count = 1
        Me.Advert.Impressions.Add(Me.ID, Me)
        Me.Account.Impressions.Add(Me.ID, Me)

    End Sub

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        'this doesnt appear to remove from the db ?/
        Me.Advert.Impressions.Remove(Me.ID)
        Me.Account.Impressions.Remove(Me.ID)

    End Sub

    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName

    End Function

    Public Function Insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert
        Return New clsImpression(Me.Account, Me.Advert, Me.IDate)
    End Function

    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update
        If Me.ID > 0 Then
            Dim con As SqlConnection = New SqlConnection(conString)
            con.Open()
            Dim command As SqlCommand = New SqlCommand()
            command.CommandText = "UpdateImpression"
            command.CommandType = CommandType.StoredProcedure
            command.Connection = con
            Dim paramID As New SqlParameter("@ID", SqlDbType.Int)
            paramID.Value = Me.ID
            Dim paramAdvertID As New SqlParameter("@advertid", SqlDbType.Int)
            paramAdvertID.Value = Me.Advert.ID
            Dim paramAccountID As New SqlParameter("@accountid", SqlDbType.Int)
            paramAccountID.Value = Me.Account.ID
            Dim paramTimeStamp As New SqlParameter("@impDate", SqlDbType.DateTime)
            paramTimeStamp.Value = Me.IDate

            Dim paramReturn As New SqlParameter("@ret", SqlDbType.Int)
            paramReturn.Direction = ParameterDirection.ReturnValue

            command.Parameters.Add(paramID)
            command.Parameters.Add(paramAccountID)
            command.Parameters.Add(paramAdvertID)
            command.Parameters.Add(paramTimeStamp)
            command.Parameters.Add(paramReturn)

            command.ExecuteNonQuery()

            con.Close()


        End If
    End Sub
End Class
