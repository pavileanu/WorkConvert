Public Class WaitMessage
    Inherits clsPageLogging
    Public lid As String
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        lid = Request.QueryString("lid")
    End Sub

End Class