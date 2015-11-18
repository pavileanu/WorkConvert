Public Class poller
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Request("key") = "rcpolling_1" Then
            'Add any logic in here to respond to the dyn service, its expecting "True"
            Response.Write(clsIQ.IsLoaded.ToString)
            Response.End()
        End If
    End Sub

End Class