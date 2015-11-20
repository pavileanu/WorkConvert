Public Class ClickThru
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim advertID As Integer = CInt(Request("advertid"))
        Dim clickedAdvert As clsAdvert = iq.Adverts(advertID)
        Dim lid As UInt64 = CULng(Request.QueryString("lid"))
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim advertClick As clsClickThru = New clsClickThru(agentAccount, clickedAdvert, Now)

    End Sub

End Class