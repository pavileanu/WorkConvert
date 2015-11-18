

Public Class manipulation
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'this page is typically execute by iQuote2.js

        '   rexec("manipulation.aspx?command=graft&source=" + copySourceBranchID + "target=" + targetBranchID);

        Dim exception As Exception = Nothing

        Dim targetBranch As clsBranch = Nothing
        If Request("TargetBranch") IsNot Nothing Then targetBranch = iq.Branches(Request("TargetBranch"))
        Dim sourcebranch As clsBranch = Nothing
        If Request("SourceBranch") IsNot Nothing Then sourcebranch = iq.Branches(Request("SourceBranch"))
        Dim targetPath As String = String.Empty
        If Request("TargetPath") IsNot Nothing Then targetPath = Request("targetPath")
        Dim sourcePath As String = String.Empty
        If Request("SourcePath") IsNot Nothing Then sourcePath = Request("sourcePath")

        Dim lid As UInt64 = Request.QueryString("lid")

        If iq.sesh(lid, "UserID") Is Nothing Then Response.Redirect("Loading.aspx", False) : Exit Sub

        Dim username As String = iq.UserAccountName(iq.sesh(lid, "UserID"), iq.sesh(lid, "AgentAccount").id)
        Dim agentaccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        Dim currentQuote As clsQuote = Nothing

        If iq.SeshContains(lid, "QuoteID") Then
            If iq.sesh(lid, "QuoteID") IsNot Nothing Then
                Dim qid As Integer = iq.sesh(lid, "QuoteID")
                currentQuote = agentaccount.Quotes(qid)
            End If
        End If

        Try

            Select Case Request("command")
                Case Is = "graft"

                    Dim errormessage As String = targetBranch.Graft(sourcebranch, username, "", errormessages)  'Creates the new graft

                    'we must delete the cached dataview - otherwise we won't see the change
                    wipeCachedDataView(targetPath, lid)

                    'if the graft fails, we put an error in the response which the JS will place into the tree
                    Panel1.Controls.Add(ErrorDymo(errormessage, lid))

                    'Case Is = "adopt" '(reparent - many branches )

                    '    wipeCachedDataView(targetPath, lid)
                    '    Dim newParent As clsBranch = iq.Branches(Split(targetPath, ".").Last)

                    '    'the JS compiles a list of sources from the checked branches (their paths)
                    '    For Each s In Request("sources").Split(",")
                    '        If s <> "" Then 'the JS untidily leaves an extra comma - but it's easier to deal with here 
                    '            sourcebranch = iq.Branches(s.Split(".").Last)
                    '            sourcebranch.Parent = newParent
                    '            sourcebranch.Update(errorMessages)
                    '        End If
                    '    Next

                    '    'slots and quantities (on descendants) will need re pathing
                    '    'any grafts and prunes in force will need manipulating
                    '    'some quoteitems paths may be invalidated


                Case Is = "clone"

                    wipeCachedDataView(oneAbove(sourcePath), lid)  'we need to clear the PARENTs dataview

                    Dim bid As Integer = Split(sourcePath, ".").Last
                    Dim aBranch As clsBranch = iq.Branches(bid).clone(sourcePath, errorMessages)

                Case Is = "having"
                    'just a stub.. allows the branch to be displayed open - with the Ex buttons

                Case Is = "exclude"

                    'rExec('manipulation.aspx?command=exclusivity&bid=' + bid + '&val=' + tb.value, nullFunc);
                    Dim v As Object = DBNull.Value
                    Dim branch As clsBranch = Nothing
                    'Having and Excludes (request variables) are complete paths - we're only interested in the final branch
                    Dim hvg As Integer = CInt(Split(sourcePath, ".").Last)
                    Dim exc As Integer = CInt(Split(targetPath, ".").Last)
                    If iq.Branches.ContainsKey(hvg) Then
                        Dim ex As clsExclude = New clsExclude(iq.Branches(hvg), iq.Branches(exc), "No reason specified")
                    End If

                Case Is = "prune"

                    wipeCachedDataView(sourcePath, lid)
                    iq.Prune(sourcePath, username)

                Case Is = "retract"

                    wipeCachedDataView(oneAbove(sourcePath), lid)  'we need to clear the PARENTs dataview
                    iq.Retract(iq.Branches(Split(sourcePath, ".").Last), username, errorMessages)

                Case Is = "cursor"
                    iq.sesh(lid, "treeCursor") = sourcePath

                Case Is = "createNextVersion"

                    Dim Quote As clsQuote
                    Dim RevisedQuote As clsQuote

                    Quote = agentaccount.Quotes(Request("QuoteID"))
                    RevisedQuote = Quote.CreateNextVersion(errorMessages)

                    iq.sesh(lid, "QuoteID") = RevisedQuote.ID
                    iq.sesh(lid, "quoteCursor") = RevisedQuote.RootItem.Children(0).ID

                Case Is = "startQuote", "startquote"

                    DiscardUnChangedQuote(lid)
                    iq.sesh(lid, "branchStates").clear() 'wipetreestate


                    Dim bid As Integer
                    If Request("buyerid") = "" Then
                        bid = iq.sesh(lid, "AgentAccount")
                    Else
                        bid = Request("buyerid")
                    End If

                    '           If Not iq.Accounts(bid).SellerChannel Is agentaccount.sellerchannel Then

                    'this buyer does not have an account with this seller (yet).. so make one
                    'we'll need a priceBand (amongst other things)


                    '          End If

                    startQuote(bid, lid)

                Case Is = "adminON"
                    iq.sesh(lid, "admin") = True
                Case Is = "adminOFF"
                    iq.sesh(lid, "admin") = False

                Case Is = "CreateChannel"

                    Dim sellerChannel As clsChannel = agentaccount.SellerChannel
                    Dim achanel As clsChannel = New clsChannel(sellerChannel, "New Company", "Holding company", "", "NEW1", sellerChannel.Region, New nullableString(), New nullableString(), New nullableString(), 15, "tree.1", "", 0, 0, "R", "", "", iq.i_currency_code("GBP"), sellerChannel.Universal, sellerChannel.orderEmail, "", "")


                Case Is = "CreateSiblingAccount"
                    'Creates a new user - and an account for them them
                    ' we may need to migrate the accounts to
                    Dim acToCopy As clsAccount = iq.Accounts(Request("AccID")) ' this is 
                    Dim NewUser As clsUser = New clsUser(acToCopy.BuyerChannel, DomainPart(acToCopy.User.Email), "", acToCopy.User.tel1, acToCopy.User.tel2)
                    With acToCopy
                        Dim anAccount As clsAccount = New clsAccount(NewUser, "Password", .BuyerChannel, .Roles, .Team, .Language, .Currency, .SellerChannel, acToCopy.Priceband, .BuyerChannel.Region.Culture, .mfrCode)
                        anAccount.insert(errorMessages) 'does the insert and sets the ID
                        iq.sesh(lid, "BuyerAccount") = anAccount.ID
                    End With


                Case Is = "delNote"
                    Dim qi As clsQuoteItem = currentQuote.RootItem.FindRecursive(Request("qiid"))
                    qi.Note = New nullableString()
                    qi.Update()

                Case Is = "addNote"


                    Dim qi As clsQuoteItem = currentQuote.RootItem.FindRecursive(Request("qiid"))
                    qi.Note = New nullableString("Your note")
                    qi.Update()

                Case Is = "saveNote"

                    Dim qiid As Integer = Val(Mid(Request("qiid"), 5))  'The qiid parameter is now the element name - with is the QuoteItemID prefixed by 'note'  - e.g. note3458830
                    Dim qi As clsQuoteItem = currentQuote.RootItem.FindRecursive(qiid)
                    If qi IsNot Nothing Then  'If the remove an item to which they just added a note - we wont be able to locate/save it
                        qi.Note = New nullableString(Request("text"))
                        qi.Update()
                    End If

                Case Is = "CopyQuote"

                    Dim quote As clsQuote = agentaccount.Quotes(Request("QID"))

                    Dim newQuote = quote.Copy(Nothing, 0, errorMessages)
                    iq.sesh(lid, "quoteCursor") = newQuote.RootItem.Children(0).ID
                    iq.sesh(lid, "QuoteID") = newQuote.ID

                Case Is = "MarkAsWon"

                    Dim quote As clsQuote = agentaccount.Quotes(Request("QID"))
                    If quote.PassesValidation(lid) Then errorMessages = quote.MarkAsWon(lid) Else Panel1.Controls.Add(NewLit("[FV]"))

                Case Is = "DiscardQuote"

                    Dim quote As clsQuote = agentaccount.Quotes(Request("QID"))
                    quote.State = iq.i_state_GroupCode("QT-#CX")  'mark as closed
                    quote.Update()

                Case Is = "ExpandQuoteVersions"

                    If Not iq.SeshContains(lid, "expandedQuotes") Then
                        iq.sesh(lid, "expandedQuotes") = New List(Of Integer)
                    End If

                    If agentaccount.Quotes.ContainsKey(Request("RQID")) Then  'this should be redundant 

                        Dim expandedQuotes As List(Of Integer) = iq.sesh(lid, "expandedQuotes")  ' a list of the root quotes which are expanded
                        If Not expandedQuotes.Contains(CInt(Request("RQID"))) Then
                            expandedQuotes.Add(CInt(Request("RQID")))
                        End If
                    End If


                Case Is = "CollapseQuoteVersions"
                    iq.sesh(lid, "expandedQuotes").remove(Request("RQID"))  'The agent account has had all its quotes loaded - we add the quote form there

                Case Is = "focus"  'focus can be set to a comma seperated value (which a *set* of 'focuses' - eg. Receta+Budget) (or whatever) - ProductVisible checks Focus (if it's set)

                    If Request("value") = "" Then
                        iq.sesh(lid, "focus") = New List(Of String)  'Spliting and empty string into a list products a single empty value (ie a list with one entry) - so we have a special case
                    Else
                        iq.sesh(lid, "focus") = Split(Request("value"), ",").ToList 'A LIST(of Strings) goes into the session variable
                    End If

                Case Is = "quoteNameChange"

                    Dim quote As clsQuote = agentaccount.Quotes(Request("QID"))
                    quote.Name = New nullableString(Regex.Replace(Request("quoteName"), "<[^>]+>", ""))
                    quote.Update()

                Case Else
                    Debug.Print(Request("Command"))
                    Stop

            End Select
            Panel1.Controls.Add(NewLit("<p>" & String.Join(Environment.NewLine, errormessages) & "</p>"))
        Catch ex As Exception
            ErrorLog.Add(ex)
            exception = ex
        End Try
        'Audit Trail
        AuditLog.Instance.Add(lid, Request("command").ToString(), If(sourcePath Is Nothing, If(sourcebranch Is Nothing, String.Empty, sourcebranch.ID), sourcePath), If(targetPath Is Nothing, If(targetBranch IsNot Nothing, targetBranch.ID, Nothing), targetPath), errorMessages, exception, "", "", 0, Context.Request.HttpMethod, Context.Request.UrlReferrer.AbsoluteUri)

    End Sub



    Private Function DomainPart(email$) As String

        Dim at As Integer
        at = InStr(email$, "@")
        If at Then Return Mid$(email$, at) Else Return ""

    End Function


    Public Sub startQuote(BuyerID As Integer, lid As UInt64)

        If lid = 0 Then Stop

        If iq.SeshContains(lid, "QuoteID") Then
            'save any changes to the quote in progress
            Dim inprogress As clsQuote
            inprogress = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))
            inprogress.Update()
        End If

        iq.sesh(lid, "BuyerAccount") = BuyerID

        'start a new quote 
        Dim aQuote As clsQuote
        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)



        'Dim l$ = UpdatePrices(buyeraccount)  'cache' (grab the latest prices from IQ2

        Dim nullprice As NullablePrice 'The quote will start life with an unknown price
        nullprice = New NullablePrice(buyerAccount.Currency)
        aQuote = New clsQuote(buyerAccount, agentAccount, Nothing, Now, Now, 1, iq.i_state_GroupCode("QT-#NW"), nullprice, buyerAccount.Currency, False, False, False, False, New nullableString(), New nullableString(), 0)


        iq.sesh(lid, "QuoteID") = aQuote.ID

        'populate the customer name, display quote #

        'txtBuyer.Text = buyeraccount.displayname(s_lang)
        'txtBuyer.Enabled = False ' lock it !
        'LblBuyer.Text = buyeraccount.displayname(s_lang)

        'reveal the Product tree (if they have a quote on the go)
        'Response.Write("<script display('treeHolder','inline')></script>;")
        ' PnlProductTree.CssClass = "visible"
    End Sub

End Class