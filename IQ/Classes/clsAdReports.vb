Option Strict On
Option Explicit On

Imports System.Data.SqlClient
Public Class clsAdReports
    Private conString As String = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString
    Public Function getAdImpressions(advertiserAccount As clsAccount, startDate As Date, endDate As Date) As DataTable
        Dim results As DataTable

        Dim con As SqlConnection = New SqlConnection(conString)
        con.Open()
        Dim command As SqlCommand = New SqlCommand()
        command.CommandText = "AddAdvert"
        command.CommandType = CommandType.StoredProcedure
        command.Connection = con
      

        command.ExecuteNonQuery()

        con.Close()




        Return results
    End Function



End Class
