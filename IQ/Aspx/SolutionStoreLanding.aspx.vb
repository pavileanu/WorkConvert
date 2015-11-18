Public Class SolutionStoreLanding
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim lid As UInt64 = 0
        If Request("lid") IsNot Nothing AndAlso UInt64.TryParse(Request("lid"), lid) Then
            'Validate the user is on this server...

            If iq.SeshAlive(lid) Then
                'Read basket and add using shopping list
                Dim slstring = ""
                Dim skus = Split(Request("SKUS"), ",")
                Dim qtys = Split(Request("QTYS"), ",")
                For i = 0 To skus.Length - 1
                    slstring &= skus(i) & "*" & qtys(i).ToString & ";"
                Next

                Dim agentAccount = iq.seshTyped(Of clsAccount)(lid, "AgentAccount")
                Dim buyerAccount = iq.seshTyped(Of clsAccount)(lid, "BuyerAccount")
                Dim errormessages = New List(Of String)
                Dim FirstSysPath = ""
                clsQuote.FromShoppingList(lid, agentAccount, buyerAccount, slstring, errormessages, FirstSysPath)

                iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem
                iq.sesh(lid, "path") = "tree.1"

                'Response.Write("<script>this.parent.postMessage(""reloadplease"", ""https://" & Request("post") & ".hpiquote.net"");</script>")
                scriptManager.RegisterStartupScript(updatePanel, Me.GetType(), "reload", "this.parent.postMessage(""reload"", ""https://" & Request("post") & ".hpiquote.net"");", True)

            End If
        End If

    End Sub

End Class