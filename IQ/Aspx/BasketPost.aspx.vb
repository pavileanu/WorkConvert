Public Class BasketPost
    Inherits System.Web.UI.Page
    Public xmlString As String
    Public accountNum As String
    Public sessionID As String
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim lid As ULong
        lid = Request("lid")
        Label1.Visible = False
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        If iq.sesh(lid, "basketContent") IsNot Nothing Then
            If agentAccount.SellerChannel.basketMode = "FRM" Then
                
                Literal1.Text = iq.sesh(lid, "basketContent")

            Else
                xmlString = HttpUtility.HtmlEncode(iq.sesh(lid, "basketContent").ToString())

            End If
            Dim accountNum As String = iq.sesh(lid, "GK_cAccountNum")
            Dim sessionID As String = iq.sesh(lid, "GK_SessionID")
            form1.Action = iq.sesh(lid, "GK_BasketURL").ToString()

        Else
            xmlString = "Basket Empty"
            form1.Action = "Basketdisplay.aspx"
            'Label1.Visible = True
        End If

    End Sub

End Class