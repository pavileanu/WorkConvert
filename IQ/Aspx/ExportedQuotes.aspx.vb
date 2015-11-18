Imports System.Globalization

Public Class ExportedQuotes
    Inherits System.Web.UI.Page
    Public version As String
    Public quoteType As String
    Public quoteDate As String
    Private agentAccount As clsAccount
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim lid As UInt64
        Dim sessionFailed As Boolean = False
        agentAccount = Nothing
        Try
            lid = Request.QueryString("lid")
            agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        Catch ex As Exception 'System.Web.HttpRequestValidationException
            sessionFailed = True

        End Try
        lblQuoteID.Text = Xlt("Quote ID : ", agentAccount.Language)
        version = Xlt("Version", agentAccount.Language)
        quoteType = Xlt("Type", agentAccount.Language)
        quoteDate = Xlt("Date", agentAccount.Language)

        Dim quoteID As Integer
        Dim quoteHistory As DataTable
        If Request("quoteRootID") IsNot Nothing Then
            quoteID = Request("quoteRootID")
            quoteHistory = getQuoteExport(quoteID)
        Else
            quoteID = Request("QuoteID")
            quoteHistory = getQuoteVersionExports(quoteID, agentAccount)
        End If
        quoteNumber.Text = quoteID
        If quoteHistory.Rows.Count > 0 Then
            exportTable.DataSource = quoteHistory
            exportTable.DataBind()
        Else
            Response.Write("This quote was not exported")
        End If

    End Sub
    Public Function TranslateType(strType As String) As String
        If Trim(strType).Length > 0 And agentAccount IsNot Nothing Then
            Return Xlt(strType, agentAccount.Language)
        Else
            Return strType
        End If
    End Function
    Public Function ConvertDate(d As DateTime) As String
        Return d.ToString("G", CultureInfo.CreateSpecificCulture(agentAccount.Culture.Code))
    End Function
End Class