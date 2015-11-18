Public Class QuoteSummary
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim qid As Integer
        qid = Request("QuoteID")

        Dim lid As uint64 = Request.QueryString("lid")
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        If qid = 0 Then
            Response.Write("No QuoteID parameter specified")
        Else

            Dim Quote As clsQuote
            If UserIsAdmin(lid) Then
                Quote = iq.Quotes(qid)
            Else
                Quote = agentAccount.Quotes(qid)
            End If

            If Quote.RootItem.Children.Count = 0 Then 'Only load if no quote item has been loaded
                Quote.LoadItems(errorMessages) 'IMPORTANT !
            End If
            Dim priceChanges As Boolean = False
            form1.Controls.Add(Quote.HtmlSummary(Quote.AgentAccount.Language, False, lid, priceChanges, errorMessages)) 'adds an HTML table summarising the quote

            Dim btnload As Button
            btnload = New Button

            btnload.OnClientClick = "redirect('listquotes.aspx?quoteid=" & qid & "&lid=" & lid & "');return false;"

            btnload.CssClass = "hpbluebutton"

            Dim btnNextVersion As New Button
            btnNextVersion.Text = Xlt("Create next version", agentAccount.Language)
            btnNextVersion.ToolTip = Xlt("Creates a copy leaving the original quote intact", agentAccount.Language)
            'the createnextVersion will set the iq.sesh(lid,"QuoteID") so we can just redirect to quote.aspx
            btnNextVersion.OnClientClick = " $('#" & btnNextVersion.ClientID & "').hide();rExec('Manipulation.aspx?command=createNextVersion&quoteId=" & Quote.ID & "', gotoTree);return false;"

            Dim btnCopy As New Button
            btnCopy.Text = Xlt("Copy", agentAccount.Language)
            btnCopy.ToolTip = Xlt("Create a new quote using this template", agentAccount.Language)
            'the createnextVersion will set the iq.sesh(lid,"QuoteID") so we can just redirect to quote.aspx
            btnCopy.OnClientClick = "showCopy('manipulation.aspx?command=CopyQuote&QID=" & Quote.ID & "');return false;"


            'Price Change PlaceHolder - this is displayed until and unless you update the quote
            'because the page is never (well, rarely) posted back the performance'cost' is acceptable
            ' Dim PCP As PlaceHolder = Quote.HtmlSummary(Quote.AgentAccount.Language, True, lid, priceChanges, errorMessages)
            If priceChanges = False And Quote.Locked = False Then
                'Prices are unchanged .. procede with quote as normal
                btnload.Text = Xlt("Edit", Quote.BuyerAccount.Language)
                btnload.ToolTip = Xlt("Edits the original quote", Quote.BuyerAccount.Language)
            Else
                btnload.Text = Xlt("View", Quote.BuyerAccount.Language)
                btnload.ToolTip = Xlt("View the original quote", Quote.BuyerAccount.Language)
            End If
            form1.Controls.Add(btnload)

            If Not Quote.Saved Then
                btnNextVersion.Visible = False
            End If
            If Quote.Locked And Quote.Saved And priceChanges Then
                btnload.Visible = False
                btnCopy.Visible = False
                Dim lit As Literal = New Literal()
                lit.Text = "<div > Price for the saved quote has changed. Please create a new version. </div>"
                form1.Controls.Add(lit)
            End If

            form1.Controls.Add(btnCopy)
            form1.Controls.Add(btnNextVersion)

            If Quote.AgentAccount.SellerChannel IsNot agentAccount.SellerChannel Then
                form1.Controls.Add(NewLit("<span color='red'>Warning, loading this quote will change your account to " & Quote.AgentAccount.SellerChannel.Code & "</span>"))
            End If

        End If

        OutputErrors(form1.Controls, errorMessages, lid)


    End Sub



End Class