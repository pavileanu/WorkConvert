Public Class Listquotes
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim lid As UInt64 = 0
        If Request("QuoteId") IsNot Nothing AndAlso UInt64.TryParse(Request.QueryString("lid"), lid) Then

            Dim agent = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
            Dim quote = iq.Quotes(Request("QuoteId"))

            If agent.SellerChannel IsNot quote.AgentAccount.SellerChannel Then
                'Switch account, must be an admin??
                Dim found = False
                For Each a In agent.User.Accounts.Values
                    If a.SellerChannel Is quote.AgentAccount.SellerChannel AndAlso a.Password = agent.Password Then
                        SwitchAccount(lid, a, a, errorMessages)
                        agent = a
                        found = True
                        Exit For
                    End If
                Next
                If Not found Then Exit Sub 'Add warning message here...
            End If

            iq.sesh(lid, "QuoteID") = Request("QuoteId")
            If Not agent.Quotes.ContainsKey(Request("QuoteId")) Then agent.LoadQuotes(0)
            If quote.RootItem.Children.Count = 0 Then quote.LoadItems(errorMessages)
            'If Not quote.Saved Then quote.Editable = True 'needs 

            iq.sesh(lid, "root") = "tree.1"
            If quote.RootItem.Children.Count > 0 Then  '
                If Not iq.seshDic(lid).ContainsKey("branchstates") OrElse iq.seshDic(lid)("branchStates") Is Nothing Then iq.sesh(lid, "branchStates") = New Dictionary(Of String, clsBranchState)
                clsBranchState.PloughPath(lid, quote.RootItem.Children(0).Path, errorMessages, 0, enumParadigm.configuringSystem)
                iq.sesh(lid, "path") = quote.RootItem.Children(0).Path
                iq.sesh(lid, "quoteCursor") = quote.RootItem.Children(0).ID
                iq.sesh(lid, "Paradigm") = enumParadigm.configuringSystem
            End If

            Response.Redirect("tree.aspx?lid=" & lid.ToString & If(Request("elid") IsNot Nothing, "&elid=" & Request("elid"), ""))
        End If
    End Sub


    Private Function RecursiveFindControlByID(ByRef control As WebControl, id As String) As WebControl

        RecursiveFindControlByID = Nothing

        If control.ID = id Then
            Return control
        End If


        For Each c In control.Controls
            If Not TypeOf c Is System.Web.UI.WebControls.Literal And Not TypeOf c Is LiteralControl Then
                Dim ac As WebControl
                ac = RecursiveFindControlByID(c, id)
                If Not ac Is Nothing Then Return ac
            End If
        Next

    End Function

    
End Class