Public Class signOut1
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim lid As UInt64 = Request.QueryString("lid")

        Dim message = "Thank you for using iQuote - see you again soon."
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        If agentAccount IsNot Nothing Then
            litThanks.Text = Xlt(message, agentAccount.Language)
        Else
            litThanks.Text = message
        End If

        ' SK - Store the Manufacturer as a discrete object so that the Master Page can use it to work
        ' out which style sheet to apply to this page. The whole sesh is killed in the PreRender event,
        ' by which time the Master Page will have made use of the value.
        If Not iq.sesh(lid, "AgentAccount") Is Nothing Then
            iq.sesh(lid, "MFR") = agentAccount.Manufacturer
            iq.seshDic(lid).Remove("AgentAccount")
        End If

    End Sub

    Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender

        Dim lid As UInt64 = Request.QueryString("lid")

        If iq IsNot Nothing Then
            iq.KillSesh(lid)
        End If

    End Sub

End Class