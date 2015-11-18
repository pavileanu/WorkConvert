Public Class QuotesTable
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim lid As UInt64
        Dim searchFailed As Boolean = False
        Dim agentAccount As clsAccount = Nothing
        Try
            lid = Request.QueryString("lid")
            agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Catch ex As Exception 'System.Web.HttpRequestValidationException
            searchFailed = True
        End Try

        litDraft.Text = Xlt("Draft Quotes", agentAccount.Language)
        litSaved.Text = Xlt("Saved Quotes", agentAccount.Language)

        Dim pnl As Panel = New Panel
        pnl.ID = "quoteFilterPanel"

        Form.Controls.Add(pnl)

        Dim LblFilter As Literal = New Literal
        LblFilter.Text = "<span class =""quotePanel"" style='vertical-align: middle;' >" & Xlt("Search", If(agentAccount Is Nothing, English, agentAccount.Language)) & "</span>"
        pnl.Controls.Add(LblFilter)

        Dim txtFilter As TextBox
        txtFilter = New TextBox
        txtFilter.ID = "txtFilter"
        txtFilter.CssClass = "quotePanel"

        If Request("filter") IsNot Nothing Then
            txtFilter.Text = Server.UrlDecode(Request("filter"))
        End If

        If txtFilter.Text <> "" Then pnl.CssClass &= " filterActive"
        pnl.Controls.Add(txtFilter)


        'NB - Even after the quotesTable (this ASPX) was Ajax'd into the ListquotesASPX (MasterPage based holder'd ListOfQuotes DIV - the DIV survives (becuase it's innerHTML is set in Blow() )
        Dim applyButton As Literal = New Literal
        Dim omd$ = "burstBubble(event); var fv;fv=document.getElementById('txtFilter').value; var savedP='false';   var indexP= $('ul li.ui-state-active').index(); if (indexP == 1) { savedP = 'true';}  showQuotes(fv,savedP);".Replace("'", Chr(34))
        applyButton.Text = "<div class='hpOrangeButton' style='display:inline-block;' onclick='" & omd$ & "'>" & Xlt("Apply", agentAccount.Language) & "</div>"
        pnl.Controls.Add(applyButton)

        Dim cancelButton As Literal = New Literal
        omd$ = "var savedP= $('#SavedPanel').attr('aria-expanded');showQuotes('',savedP);".Replace("'", Chr(34))
        cancelButton.Text = "<div class='hpGreyButton' style='display:inline-block' onmousedown='" & omd$ & "'>" & Xlt("Clear", agentAccount.Language) & "</div>"
        pnl.Controls.Add(cancelButton)



        Dim tbl As New Table
        Dim tbl2 As New Table
        tbl.CssClass = "quotesTable"
        tbl2.CssClass = "quotesTable"
        tbl.ID = "DraftPanel"
        tbl2.ID = "SavedPanel"
        ' tbl.BorderWidth = 1
        ' tbl2.BorderWidth = 1
        Dim CSS() As String
        Dim language = If(agentAccount Is Nothing, English, agentAccount.Language)
        'Dim H$ = Xlt("ID,Ver,Name,Customer,Supplier,Updated,Status,Value", If(agentAccount Is Nothing, English, agentAccount.Language))
        'Dim H$ = "ID,Ver,Name,Customer,Supplier,Systems,Updated,Status,Value"
        'Dim H$ = "ID,Version,Name,Customer,Supplier,Systems,Options,Updated,Status,Value,!Buttons"
        'CSS = Split(H$, ",")
        'For i = 0 To UBound(CSS)
        ' CSS(i) = "quotesList1Col-" & CSS(i)
        ' Next

        'creates a set of spans - with classes
        Dim hr = New TableHeaderRow()
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("", language)})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("ID", language), .CssClass = "quoteTableHeader", .ColumnSpan = 1})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Version", language), .CssClass = "quoteTableHeader"})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Name", language), .CssClass = "quoteTableHeader", .ColumnSpan = 2})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Updated", language), .CssClass = "quoteTableHeader"})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Status", language), .CssClass = "quoteTableHeader"})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Exports", language), .CssClass = "quoteTableHeader"})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Value", language), .CssClass = "quoteTableHeader"})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("", language)})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("", language)})
        hr.Cells.Add(New TableHeaderCell() With {.Text = Xlt("", language)})

        Dim hr2 = New TableHeaderRow()
        hr2.Cells.Add(New TableHeaderCell() With {.Text = Xlt("", language)})
        hr2.Cells.Add(New TableHeaderCell() With {.Text = Xlt("ID", language), .CssClass = "quoteTableHeader", .ColumnSpan = 1})
        hr2.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Version", language), .CssClass = "quoteTableHeader"})
        hr2.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Updated", language), .CssClass = "quoteTableHeader"})
        hr2.Cells.Add(New TableHeaderCell() With {.Text = Xlt("Value", language), .CssClass = "quoteTableHeader"})
        hr2.Cells.Add(New TableHeaderCell() With {.Text = Xlt("", language)})
        hr2.Cells.Add(New TableHeaderCell() With {.Text = Xlt("", language)})
        hr2.Cells.Add(New TableHeaderCell() With {.Text = Xlt("", language)})
        'Dim hr2 = New TableHeaderRow()
        'Dim ar1() As TableHeaderCell
        'hr.Cells.CopyTo(ar1, 0)
        'For Each ce In ar1
        ' If ce.text <> Xlt("Name", language) Then hr2.Cells.Add(ce)
        ' Next


        tbl.Rows.Add(hr2)
        'tbl.Controls.Add(FloatNone)
        tbl2.Rows.Add(hr)
        'tbl2.Controls.Add(FloatNone)
        ' DiscardUnChangedQuote(Session) 'not a good plan as they may go 'back' to ontinue to work on the next draft of the qoute - instead we simply don't display unchanged quotes
        Dim quoteCount As Int32 = 0


        If Not searchFailed Then
            'filter by the supplied buyerID
            agentAccount.LoadQuotes(Val(Request("buyerID")))

            ''build a dictionary of root quote IDs to latest versions  (this isn't terribly efficient - but in practise won't be an issue)
            'Dim dicLatest As Dictionary(Of clsQuote, clsQuote) = New Dictionary(Of clsQuote, clsQuote)
            'For Each q In agentAccount.Quotes.Values
            '    If Not dicLatest.ContainsKey(q.RootQuote) Then
            '        dicLatest.Add(q.RootQuote, q)
            '    Else
            '        If q.Version > dicLatest(q.RootQuote).Version Then
            '            dicLatest(q.RootQuote) = q
            '        End If
            '    End If
            'Next
           

            'uses LINQ to sort data from the object model
            Dim sortedQuotes = From q In agentAccount.Quotes.Values Order By (q.RootQuote.ID + q.Version / 100) Descending  ' quiteBy (q.Created) Descending
            If UserIsAdmin(Request("lid")) Then
                sortedQuotes = From q In iq.Quotes.Values Order By (q.RootQuote.ID + q.Version / 100) Descending
                If IsNumeric(txtFilter.Text) AndAlso sortedQuotes.Where(Function(sq) sq.ID = txtFilter.Text).Count = 0 Then
                    'Ok so how do we find who this quote belongs to?
                    Dim aa As Object
                    Dim dt = dataAccess.da.FilledDataTable(dataAccess.da.OpenDatabase(), "SELECT FK_Account_ID_Agent FROM quote where id=" & txtFilter.Text)
                    If dt.Rows.Count > 0 AndAlso Not IsDBNull(dt.Rows(0)("FK_Account_ID_Agent")) Then
                        Dim aga = iq.Accounts(CInt(dt.Rows(0)("FK_Account_ID_Agent")))
                        aga.LoadQuotes(0)
                    End If

                End If
            End If
            Dim maxVersions = sortedQuotes.GroupBy(Function(qu) qu.RootQuote.ID).ToDictionary(Function(qu) qu.Key, Function(qu) qu.Max(Function(qui) qui.Version))


            Dim state_cancelled As clsState = iq.i_state_GroupCode("QT-#CX")
            Dim state_new As clsState = iq.i_state_GroupCode("QT-#NW")

            Dim row As TableRow

            Dim odd As Boolean = True 'toggles for each line

            Dim Expanded As List(Of Integer) = iq.SeshValue(lid, "expandedQuotes", Nothing) 'a list of the root expanded quotes


            Dim lang As clsLanguage = agentAccount.Language
            Dim txt As String = txtFilter.Text
            txt = LCase(txtFilter.Text) 'make case insensitive

            Dim comparer As StringComparer = StringComparer.CurrentCultureIgnoreCase
            For Each quote As clsQuote In (From q In sortedQuotes Select q Where q.Saved = True) '.quotes.Values
                ' Dim adic As New Dictionary(Of String, Object)(StringComparer.CurrentCultureIgnoreCase)
                With quote

                    'Apply any filtering . . .

                    Dim rowAdd As Boolean = False

                    If Trim(txt) = "" Then
                        rowAdd = True
                    ElseIf .Name.DisplayValue.ToLower.Contains(txt) Or .BuyerAccount.BuyerChannel.DisplayName(lang).ToLower.Contains(txt) Or _
                       .BuyerAccount.displayName(agentAccount.Language).ToLower.Contains(txt) Or _
                       CType(.RootQuote.ID, String).Contains(txt) Or .KeywordExists(txt) Then
                        rowAdd = True
                    End If

                    If rowAdd Then
                        Dim isExpanded As Boolean = False
                        If Expanded IsNot Nothing Then
                            If agentAccount.Quotes.ContainsKey(quote.RootQuote.ID) Then
                                'If quote.RootQuote.ID = 32 Then Stop
                                If Expanded.Contains(quote.RootQuote.ID) Then
                                    isExpanded = True
                                End If
                            End If
                        End If

                        If quote.Version = maxVersions(quote.RootQuote.ID) Or isExpanded Then  'show all versions of 'Expanded' quotes, for non expanded ones we only show the latest revision
                            'If quote.RootQuoteID = 11051 Then Stop
                            If Not quote.State Is state_cancelled Then
                                '  If Not (quote.State Is state_new And quote.RootItem.Children.Count = 0) Then  'dont show quotes with nothing on them (yet)


                                row = quote.ListRow(quote, CSS, lid, quote.Version = maxVersions(quote.RootQuote.ID), errorMessages)
                                '    If odd Then row.CssClass &= " quotesListOdd" Else row.CssClass &= " quotesListEven"
                                odd = Not odd 'toggle betwen the odd and even classes for a stripey (more read accrossable) list
                                quoteCount += 1
                                tbl2.Controls.Add(row)
                                '  tbl2.Controls.Add(FloatNone())


                            End If
                        End If
                    End If
                End With
                If quoteCount > 100 Then Exit For
            Next
            For Each quote As clsQuote In (From q In sortedQuotes Select q Where q.Saved = False) '.quotes.Values
                ' Dim adic As New Dictionary(Of String, Object)(StringComparer.CurrentCultureIgnoreCase)
                With quote

                    'Apply any filtering . . .
                    Dim rowAdd As Boolean = False

                    If Trim(txt) = "" Then
                        rowAdd = True
                    ElseIf .Name.DisplayValue.ToLower.Contains(txt) Or .BuyerAccount.BuyerChannel.DisplayName(lang).ToLower.Contains(txt) Or _
                       .BuyerAccount.displayName(agentAccount.Language).ToLower.Contains(txt) Or _
                       CType(.RootQuote.ID, String).Contains(txt) Or .KeywordExists(txt) Then
                        rowAdd = True
                    End If

                    If rowAdd Then
                        Dim isExpanded As Boolean = False
                        If Expanded IsNot Nothing Then
                            If agentAccount.Quotes.ContainsKey(quote.RootQuote.ID) Then
                                'If quote.RootQuote.ID = 32 Then Stop
                                If Expanded.Contains(quote.RootQuote.ID) Then
                                    isExpanded = True
                                End If
                            End If
                        End If

                        If quote.Version = maxVersions(quote.RootQuote.ID) Or isExpanded Then  'show all versions of 'Expanded' quotes, for non expanded ones we only show the latest revision
                            'If quote.RootQuoteID = 11051 Then Stop
                            If Not quote.State Is state_cancelled Then
                                '  If Not (quote.State Is state_new And quote.RootItem.Children.Count = 0) Then  'dont show quotes with nothing on them (yet)

                                row = quote.ListRow(quote, CSS, lid, quote.Version = maxVersions(quote.RootQuote.ID), errorMessages)
                                '    If odd Then row.CssClass &= " quotesListOdd" Else row.CssClass &= " quotesListEven"
                                odd = Not odd 'toggle betwen the odd and even classes for a stripey (more read accrossable) list
                                quoteCount += 1
                                tbl.Controls.Add(row)
                                ''    tbl.Controls.Add(FloatNone())


                            End If
                        End If
                    End If
                End With
                If quoteCount > 100 Then Exit For
            Next
        End If
        Dim tabLiteral As Literal = New Literal
        Dim tabLiteral2 As Literal = New Literal
        'tabLiteral.Text = "<div id=""tabs""><ul><li><a href=""#DraftPanel"">Draft Quotes</a></li><li><a href=""#SavedPanel"">Saved Quotes</a></li></ul>"
        tabLiteral2.Text = "</div>"
        'form1.Controls.Add(tabLiteral)

        If quoteCount = 0 Or searchFailed Then
            Dim trw = New TableRow() With {.HorizontalAlign = HorizontalAlign.Center, .CssClass = "center"}
            trw.Cells.Add(New TableCell() With {.Text = Xlt("No Quotes Found", language), .ColumnSpan = 10, .CssClass = "center", .HorizontalAlign = HorizontalAlign.Center})
            Dim trw2 = New TableRow() With {.HorizontalAlign = HorizontalAlign.Center, .CssClass = "center"}
            trw2.Cells.Add(New TableCell() With {.Text = Xlt("No Quotes Found", language), .ColumnSpan = 10, .CssClass = "center", .HorizontalAlign = HorizontalAlign.Center})
            tbl.Rows.Add(trw)
            tbl2.Rows.Add(trw2)
        End If
        form1.Controls.Add(tbl)
        form1.Controls.Add(tbl2)
        form1.Controls.Add(tabLiteral2)
        OutputErrors(form1.Controls, errorMessages, lid)

        'Dim client As ClientScriptManager = Me.Page.ClientScript
        'If (client.IsClientScriptBlockRegistered(Me.GetType(), "Alert")) Then
        '    client.RegisterClientScriptBlock(Me.GetType(), "Alert", "$(""#tabs"").tabs();", True)
        'End If



    End Sub


    Public Function FloatNone() As Literal

        FloatNone = New Literal
        FloatNone.Text = "<div style='clear:both'></div>"


    End Function
End Class

