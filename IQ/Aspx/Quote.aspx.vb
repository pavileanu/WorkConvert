Imports System
Imports System.IO
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Net
Imports System.Xml.Serialization
Imports System.Globalization

'Option Strict On

Public Class quote
    Inherits clsPageLogging

    Public Enum QuoteStyleEnum
        Hierarchical
        flat
    End Enum

    Private Sub quote_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
        Me.EnableViewState = False
    End Sub
    Private quote As clsQuote
    Private lid As UInt64
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        If Not clsIQ.IsLoaded Then Response.Redirect("Loading.aspx", False) : Exit Sub 'vegas

        Dim lid As UInt64 = 0
        If Not UInt64.TryParse(Request("lid"), lid) Then Exit Sub

        'Dim fi As WebControls.Image = Nothing 'Fetched Image (used to attach script to for the fetching preinstalled )
        Dim updateHandle As Integer = 0 'used to fetch pricing for the preinstalled parts
        Dim displayContext As Boolean = False

        Dim msgs As List(Of String) = New List(Of String)
        '  lid = Convert.ToUInt64(Request("lid"))
        If iq.SeshAlive(lid) Then

            'called primarily from the flex() javascript in tree.aspx
            quote = Nothing
            Dim qty As Integer = 0
            Dim Absolute As Boolean
            Dim ItemID As Integer = 0
            Dim Branch As clsBranch = Nothing  'the last segment of the path IS the branch
            Dim SKUvariant As clsVariant = Nothing

            Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
            Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
            Dim ru As String = Request.RawUrl

            Dim qiid As Integer = CInt(Request("qiid"))  'quote item ID
            Dim path As String = Request("Path")

            Dim qid As Integer = 0

            If iq.SeshContains(lid, "QuoteID") Then
                qid = CInt(iq.sesh(lid, "QuoteID"))  'use the session variable
            End If
            If CInt(Request("QuoteID")) <> 0 Then
                qid = CInt(Request("QuoteID")) 'but override if it's specified as a request parameter... (from the QuoteList screen)
                iq.sesh(lid, "QuoteID") = qid
            End If
            Dim savedlabel As Literal = New Literal()
            Dim saveResult As String = ""
            '  If Request("QuoteView") <> "" Then iq.sesh(lid, "QuoteView") = Request("QuoteView") 'persist any change to the quote view type (breakdown or summary) - set when a header is clicked, by a call through setQuoteView
            If qid <> 0 Then
                Dim ac As clsAccount = agentAccount
                If Not ac.Quotes.ContainsKey(qid) Then
                    ac.LoadQuotes(0)
                End If
                'Should always be Agent account.quotes instead of iq.quotes
                quote = ac.Quotes(qid)
                'Commented the if statement as changes were not being reflected whent the quote refreshed 
                If quote.RootItem.Children.Count = 0 Then
                    quote.LoadItems(errorMessages)
                End If
            End If

            If Request("cmd") = "Upsell" Then
                Pnlquote.Controls.Add(NewLit("!EndQuote"))
                If quote IsNot Nothing Then
                    If quote.Cursor IsNot Nothing Then  'We get the VM's for the currently selected system
                        For Each m In quote.Cursor.Flattened(True, True, 0).items
                            If m.QuoteItem Is quote.Cursor Then
                                For Each vm In m.QuoteItem.AllChildMsgs
                                    If vm.type = enumValidationMessageType.Upsell Then
                                        Pnlquote.Controls.Add(vm.UIExpanded(buyerAccount, agentAccount.Language, errorMessages, quote.ID))
                                    End If
                                Next
                            End If
                        Next
                    End If
                End If

                Pnlquote.Controls.Add(NewLit("!EndUpsells"))
            Else
                If Not String.IsNullOrWhiteSpace(Request("itemID")) Then
                    ItemID = CInt(Request("itemID"))
                End If

                If Not String.IsNullOrWhiteSpace(Request("qty")) Then
                    'We are editing this, if its not an active quote then create a new version (new quote workflow 16/12/14)

                    qty = CInt(Request("qty"))
                    Absolute = (Request("absolute") = "true")

                    If quote IsNot Nothing AndAlso quote.Locked Then
                        quote = quote.CreateNextVersion(ItemID, qty, errorMessages)
                        iq.sesh(lid, "QuoteID") = quote.ID
                        iq.sesh(lid, "quoteCursor") = quote.RootItem.Children(0).ID
                    Else
                        If ItemID <> 0 Then
                            quote.SetQtyByItemID(ItemID, qty, Absolute, quote.RootItem.Margin, errorMessages)
                            'ItemID is a QUOTE item - so things are a little simpler (this is when twiddling an existing item in the quote)
                        Else
                            If Request("SKUvariantID") = "" Then
                                'Guess it?

                                If Not quote Is Nothing AndAlso quote.RootItem.Children.Count > 0 Then
                                    Dim rootVariant = quote.RootItem.Children.First.SKUVariant
                                    'for debug/wathcing
                                    Dim pn$ = PathName(path)
                                    SKUvariant = iq.Branches(Split(path, ".").Last).Product.Variants.Values.Where(Function(v) v.sellerChannel Is buyerAccount.SellerChannel AndAlso v.Localisation = rootVariant.Localisation AndAlso v.Warehouse = rootVariant.Warehouse).FirstOrDefault
                                    If SKUvariant Is Nothing Then SKUvariant = iq.Branches(Split(path, ".").Last).Product.Variants.Values.FirstOrDefault
                                    Dim p As clsPrice
                                    If SKUvariant IsNot Nothing Then
                                        If SKUvariant.prices IsNot Nothing AndAlso SKUvariant.prices.Count = 0 Then
                                            p = New clsPrice(SKUvariant, iq.priceBands(""), New NullablePrice(buyerAccount.Currency), "CPQJIT")
                                        Else
                                            p = SKUvariant.prices.Values.FirstOrDefault
                                        End If
                                    Else

                                    End If
                                End If

                                If UserIsAdmin(lid) Then
                                    errorMessages.Add("SkuvariantID was absent for SetQtyByPath")
                                End If
                            Else

                                If iq.Variants.ContainsKey(CInt(Request("SKUvariantID"))) Then
                                    SKUvariant = iq.Variants(CInt(Request("SKUvariantID"))) '  'Branch.Product.Variants(BuyerAccount.SellerChannel)(Request("SKUvariantID"))
                                Else
                                    SKUvariant = iq.AllVariants
                                End If
                            End If
                            If SKUvariant IsNot Nothing Then
                                If Request("qty") = "" Then
                                    errorMessages.Add("qty was '' or SetQtyByPath")
                                ElseIf SKUvariant.Product.Manufacturer <> agentAccount.Manufacturer Then
                                    errorMessages.Add(GetSplitMessage(agentAccount.Manufacturer, agentAccount.Language))
                                Else
                                    If quote Is Nothing Then
                                        'START A NEW QUOTE
                                        If agentAccount.BuyerChannel.Region.Code = "BR" And iq.sesh(lid, "custContext") Is Nothing Then
                                            displayContext = True
                                        End If
                                        quote = New clsQuote(agentAccount, buyerAccount, Nothing, Now, Now, 1, iq.i_state_GroupCode("QT-#NW"), New NullablePrice(buyerAccount.Currency), buyerAccount.Currency, False, False, False, String.Empty, New nullableString(), New nullableString(), 0)
                                        iq.sesh(lid, "QuoteID") = quote.ID

                                    End If
                                End If


                                'was commented - and shouldnt have been !
                                iq.sesh(lid, "Paradigm") = enumParadigm.configuringSystem

                                If Not quote Is Nothing AndAlso quote.Locked = False Then
                                    Dim i As clsQuoteItem = quote.setQtyByPath(Request("path"), SKUvariant, qty, Absolute, 1, errorMessages)
                                    If i IsNot Nothing Then
                                        i.Margin = i.Parent.Margin  'Inherrit margin from the parent item Basecamp thread 044

                                        If i.Branch.Product.isSystem(path) Then
                                            'Adds the preisntalled componentry and returns a handle to the webservice call which will get prices for them
                                            updateHandle = i.fetchPreinstalled(lid, buyerAccount, errorMessages)


                                        End If
                                        'quote.MostRecent = i
                                        'quote.Cursor = i
                                        If i.Branch.Product.isSystem(Request("path")) Then iq.sesh(lid, "quoteCursor") = i.ID
                                    End If
                                End If
                                'set the quote cursor to the new Item *only* if its branch has children (is a (sub) system)

                                'If quote.MostRecent.Quantity > 0 And quote.MostRecent.Branch.childBranches.Count > 0 Then
                                ' quote.Cursor = quote.MostRecent '.ID
                                'End If
                            End If
                        End If
                    End If
                Else
                    ' Quote is being reloaded 

                End If

                If quote IsNot Nothing Then

                    Dim buttonCommand As String = Request("cmd")
                    If Not (String.IsNullOrEmpty(buttonCommand)) Then
                        Dim quoteName As String = Request.QueryString("quoteName")

                        Select Case buttonCommand
                            Case "Save"
                                saveResult = quote.Save(quoteName, lid)
                            Case "Email"
                                If Request("originalName") IsNot Nothing Then quoteName &= "|" & Request("originalName")
                                saveResult = EmailQuote(quoteName, Request("email"))
                            Case "PDF"
                                If quote.PassesValidation(lid) Then saveResult = quote.ExportPDF(lid, MapPath("../drive.p12"), quoteName, errorMessages) Else Pnlquote.Controls.Add(NewLit("!Result!VF!!")) : Exit Sub
                            Case "Excel"
                                If quote.PassesValidation(lid) Then saveResult = quote.ExportExcel(lid, quoteName, errorMessages, False) Else Pnlquote.Controls.Add(NewLit("!Result!VF!!")) : Exit Sub
                            Case "XMLAdv"
                                If quote.PassesValidation(lid) Then saveResult = quote.ExportXMLAdv(lid, quoteName, errorMessages) Else Pnlquote.Controls.Add(NewLit("!Result!VF!!")) : Exit Sub
                            Case "XML"
                                If quote.PassesValidation(lid) Then saveResult = quote.ExportXML(lid, quoteName, errorMessages) Else Pnlquote.Controls.Add(NewLit("!Result!VF!!")) : Exit Sub
                            Case "XMLSmartQuote"
                                If quote.PassesValidation(lid) Then saveResult = quote.ExportXMLSmart(lid, quoteName, errorMessages) Else Pnlquote.Controls.Add(NewLit("!Result!VF!!")) : Exit Sub
                            Case "Addbasket"
                                saveResult = quote.Save(quoteName, lid)
                                quote.Locked = True
                                quote.State = iq.i_state_GroupCode("QT-#WN")
                                quote.Update()
                                If quote.AgentAccount.SellerChannel.Code.StartsWith("DSYUS") Or quote.AgentAccount.SellerChannel.Code.StartsWith("DSYCA") Then

                                    ' Dim sessionID As String = iq.sesh(lid, "GK_SessionID")
                                    If iq.sesh(lid, "GK_BasketURL") IsNot Nothing Then
                                        Dim urlString As String = addtoBasketSynnex(lid)
                                        urlString = iq.sesh(lid, "GK_BasketURL").ToString() & urlString
                                        saveResult = "SYNNEX|" & urlString
                                    End If

                                ElseIf quote.AgentAccount.SellerChannel.orderEmail <> "" Then
                                    Dim emailQuote As String = addToBasket(lid)
                                    saveResult = EmailBasket(lid)


                                Else

                                    saveResult = addToBasket(lid)
                                End If

                            Case "MarkAsWon"
                                If quote.PassesValidation(lid) Then quote.MarkAsWon(lid) Else Pnlquote.Controls.Add(NewLit("!Result!VF!!")) : Exit Sub
                        End Select
                    End If



                    Dim qc As String = Request("quoteCursor")
                    If (qc Is Nothing OrElse qc = "undefined") AndAlso iq.seshDic(lid).ContainsKey("quoteCursor") AndAlso quote.RootItem.FindRecursive(CInt(iq.sesh(lid, "quoteCursor"))) IsNot Nothing Then qc = iq.sesh(lid, "quoteCursor")
                    If qc Is Nothing And quote.RootItem.Children.Count = 1 And qty = -1 Then
                        qc = quote.RootItem.Children(0).ID
                    End If
                    If qc <> "undefined" And qc IsNot Nothing Then
                        'start at the quote cursor when attemting to find the item to flex
                        '     Dim quotecursor As clsQuoteItem
                        iq.sesh(lid, "quoteCursor") = qc
                        If CInt(qc) <> 0 Then

                            Dim cmd As String = Request("cmd")

                            Dim qi As clsQuoteItem
                            qi = quote.RootItem.FindRecursive(CInt(qc))

                            If qi Is Nothing Then Stop 'temproaror

                            If cmd <> "collapse" And cmd <> "closePanel" Then 'Don't Move quote cursor if theyr'e 'only' collapsing
                                quote.Cursor = qi
                                quote.MostRecent = qi  'new
                            End If

                            If cmd = "expand" Then qi.collapsed = False
                            If cmd = "collapse" Then qi.collapsed = True

                            'tab switching with a tab=x command
                            Dim bits() As String = Split(Request("cmd"), "=")
                            If bits(0) = "openPanel" Then qi.ExpandedPanels.Add(CType(bits(1), panelEnum))
                            If bits(0) = "closePanel" Then qi.ExpandedPanels.Remove(CType(bits(1), panelEnum))

                            If bits(0) = "margin" Then
                                Dim propagate As Boolean = CBool(Request("propagate"))
                                Dim margin As Single = bits(1)
                                Dim clamped As Boolean = True
                                With agentAccount.SellerChannel
                                    If margin > .marginMax Then
                                        margin = .marginMax
                                        msgs.Add(Xlt("Your margin was restriced to its upper limit of ", agentAccount.Language) & .marginMax & "%")
                                    ElseIf margin < .marginMin Then
                                        margin = .marginMin
                                        msgs.Add(Xlt("Your margin was restriced to its lower limit of ", agentAccount.Language) & .marginMin & "%")
                                    End If
                                End With

                                If bits(2) = "R" Then
                                    'retained margin
                                    qi.ApplyMargin(100 / (100 - margin), propagate)  'eg * 1/.98

                                ElseIf bits(2) = "C" Then
                                    'cost plus
                                    qi.ApplyMargin((100 + margin) / 100, propagate)
                                Else
                                    errorMessages.Add("Unknown margin type (not R or C):" & bits(2))
                                End If
                                qi.updateRecursive()
                            End If
                        End If
                    End If
                End If


                If quote IsNot Nothing Then
                    If quote.Locked = True Then
                        iq.sesh(lid, "QuoteLocked") = True
                    Else
                        iq.sesh(lid, "QuoteLocked") = False
                    End If

                    quote.Update()

                End If


                'build a hashset from the CD list stored in the sesstion variable
                Dim foci As HashSet(Of String) = New HashSet(Of String)(Split(iq.sesh(lid, "foci"), ",").ToList)


                If quote Is Nothing OrElse quote.RootItem.Children.Count = 0 Then  'If the 'inviisible/placeholding 'rootitem' has no children... there's nothing in the basket
                    Pnlquote.Controls.Add(EmptyQuote(buyerAccount, lid))
                Else
                    If Request("cmd") Is Nothing OrElse Request("cmd") <> "Upsell" Then quote.validate(lid, buyerAccount, agentAccount, errorMessages)
                    Pnlquote.Controls.Add(NewLit("!BeginQuote"))

                    If quote.RootItem.Children.Count = 1 Then
                        iq.sesh(lid, "lastbranch") = quote.RootItem.Children(0).Path
                    End If
                    'If fi IsNot Nothing Then
                    ' Pnlquote.Controls.Add(fi)
                    ' End If

                    Pnlquote.Controls.Add(outputMessages(msgs))

                    'Pnlquote.Controls.Add(MarginUI(quote))
                    Pnlquote.Controls.Add(quote.UI(foci, lid))

                End If

                'used for Export to Excel and pdf . The value triggers the js script to call streamer.aspx
                saveResult = "<input type = 'hidden' value = '" + saveResult + "' id='hdnMsgValue' />"
                Pnlquote.Controls.Add(NewLit(saveResult))
                OutputErrors(Pnlquote.Controls, errorMessages, lid, True)

                If quote IsNot Nothing Then
                    If Request("qty") <> "" Then
                        qty = CInt(Request("qty"))
                        If qty <= 0 Then
                            Dim lit As Literal = New Literal
                            lit.Text = "<input type=""hidden"" name=""previousPath"" value=""" & quote.MostRecent.Path & """  />"
                            Pnlquote.Controls.Add(lit)
                        End If
                    End If

                    If displayContext Then
                        Dim lit As Literal = New Literal
                        lit.Text = "<input type=""hidden"" id=""wareHouseHidden"" value=""True""  />"
                        Pnlquote.Controls.Add(lit)
                    End If

                End If

                Pnlquote.Controls.Add(NewLit("!EndQuote"))


                If quote IsNot Nothing Then
                    If quote.Cursor IsNot Nothing Then  'We get the VM's for the currently selected system
                        For Each m In quote.Cursor.Flattened(True, True, 0).items
                            If m.QuoteItem Is quote.Cursor Then
                                For Each vm In m.QuoteItem.AllChildMsgs
                                    If vm.type = enumValidationMessageType.Upsell Then
                                        Pnlquote.Controls.Add(vm.UIExpanded(buyerAccount, agentAccount.Language, errorMessages, quote.ID))
                                    End If
                                Next
                            End If
                        Next
                    End If
                End If

                Pnlquote.Controls.Add(NewLit("!EndUpsells"))
                Pnlquote.Controls.Add(NewLit("!BeginUpdateHandle"))

                'well always write the updatehandle - which will me 0 if there's nothing to fetch
                'the JS will only check prices (and refresh the quote!) if there was an updatehandle (something to check !)
                Pnlquote.Controls.Add(NewLit(updateHandle & "!EndUpdateHandle"))

            End If
        End If

    End Sub

    Private Function EmptyQuote(agentaccount As clsAccount, Optional lidlocal As UInt64 = 0) As PlaceHolder

        EmptyQuote = New PlaceHolder
        Dim lit As Literal

        lit = New Literal
        lit.Text = "!BeginQuote<!-EmptyQuote-->"  'Marker (for the JS) that the quote is empty and the export tools (pnlQuoteTools) should be hidden
        EmptyQuote.Controls.Add(lit)
        Dim oldpath As String = iq.sesh(lidlocal, "lastbranch")
        Dim pathArr() As String = Split(oldpath, ".")
        Dim previousPath As String = String.Join(".", pathArr.Take(pathArr.Length - 2))
        lit = New Literal
        If oldpath IsNot Nothing Then

            iq.sesh(lidlocal, "path") = previousPath
            lit.Text = "<input type=""hidden"" name=""previousPath"" value=""" & previousPath & """  />" 'Xlt("Your quote is empty", agentaccount.Language)
        Else
            lit.Text = "" 'Xlt("Your quote is empty", agentaccount.Language)
        End If

        EmptyQuote.Controls.Add(lit)


        If agentaccount.BuyerChannel.Region.Code = "BR" And iq.sesh(lidlocal, "Quote") IsNot Nothing Then
            lit = New Literal
            lit.Text = "<input type=""hidden"" id=""wareHouseHidden"" value=""True""  />"
            ' Pnlquote.Controls.Add(lit)
            iq.sesh(lidlocal, "custContext") = Nothing
            EmptyQuote.Controls.Add(lit)
        End If
        lit = New Literal
        lit.Text = "!EndQuote" 'no id returned (we've not started a quote yet)
        EmptyQuote.Controls.Add(lit)

    End Function


    Private Function EmailQuote(quoteName As String, emailto As String) As String

        Dim state$ = ""

        Dim splitNames() As String = Split(quoteName, "|")

        Dim lid As UInt64 = Request.QueryString("lid")
        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        If iq.sesh(lid, "QuoteID") Is Nothing Then

            errorMessages.Add(Xlt("You must add something to the quote first", agentAccount.Language))
        Else

            state$ = "RQ"
            Dim fullpath$

            Dim QUOTE As clsQuote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"))
            QUOTE.Locked = True
            QUOTE.Saved = True
            state$ = "UQ"
            If Trim(splitNames(1)).Length > 0 Then
                QUOTE.Name = New nullableString(Trim(splitNames(1)))
            ElseIf QUOTE.Name.sqlValue = "null" Then
                QUOTE.Name = New nullableString(agentAccount.User.RealName)

            End If
            QUOTE.Update()
            QUOTE.ExportLogging("Email")

            state$ = "OQ"
            Dim errors As String = ""
            fullpath$ = ODS.OutputQuote(QUOTE, "Quotes", errorMessages) 'the OutputQuote() function returns the full physical path the the file generated on the server

            state$ = "AR"


            Dim vPath = HttpContext.Current.Request.ApplicationPath
            Dim pPath = HttpContext.Current.Request.MapPath(vPath) & "\"


            state$ = "SR " & pPath
            Dim tr As StreamReader = Nothing
            Dim b$ = ""
            Try
                tr = New StreamReader(pPath & "EMT/quote.htm")
                b$ = tr.ReadToEnd()
                tr.Close()
            Catch ex As System.Exception
                tr.Dispose()
            End Try

            'Tags are...
            '<subject>Welcome to Iqoute 2</subject>
            '<p>Dear <customerName/>,</p>
            '<p>Your <hostName/> iQuote quotation ID:<quoteID/> prepared for you by <agentName/> is shown below - You will also find an spreadsheet compatible version attached.
            '<quoteBody/>

            state$ = "RT "
            Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
            tags.Add("customerName", Split(buyerAccount.User.RealName, " ")(0))
            tags.Add("quoteID", QUOTE.RootQuote.ID & "-" & QUOTE.Version.ToString)
            tags.Add("hostName", buyerAccount.SellerChannel.DisplayName(buyerAccount.Language))
            tags.Add("agentName", agentAccount.User.RealName)
            tags.Add("agentEmail", agentAccount.User.Email)
            tags.Add("mfr", agentAccount.Manufacturer.ToString())

            Dim runningTotal As NullablePrice = New NullablePrice(buyerAccount.Currency)
            Dim qb$ = QUOTE.RootItem.EmailSummary(True, buyerAccount, agentAccount, errorMessages, runningTotal)
            tags.Add("quoteBody", qb$)

            Dim to$
            ' to$ = buyerAccount.User.Email
            to$ = splitNames(0)

            Dim attachment As System.Net.Mail.Attachment = Nothing
            If errorMessages.Count = 0 And fullpath$ <> "" Then
                attachment = New System.Net.Mail.Attachment(fullpath$)
                tags.Add("attachmentInfo", " You will find a spreadsheet compatible version attached.")
            Else
                tags.Add("attachmentInfo", "The spreadsheet compatible attachment is not presently available - but please contact us if you require one.")
            End If

            SendEmail(to$, "quote.htm", tags, buyerAccount.Language, errorMessages, False, attachment) 'agentAccount.User.Email

            If errorMessages.Count > 0 Then SimpleEmail("Support@channelcentral.net", "iQuote2 - config issue", Join(errorMessages.ToArray, ","))


            'state$ = "IC "
            'Dim smtpclient As New System.Net.Mail.SmtpClient


            'msg = New MailMessage("support@channelcentral.net", to$, "Your iQuote 2 quotation" & QUOTE.RootQuote.ID & "-" & QUOTE.Version & " from " & buyerAccount.SellerChannel.DisplayName(buyerAccount.Language), b$)

            'msg.ReplyToList.Add(New MailAddress(AgentAccount.User.Email))
            'msg.CC.Add(New MailAddress("support@channelcentral.net"))
            'msg.CC.Add(New MailAddress(AgentAccount.User.Email))  'CC the agent

            If errorMessages.Count = 0 Then
                'LblSave.BackColor = Drawing.Color.Green
                'LblSave.ForeColor = Drawing.Color.White
                Return Xlt("Mail sent successfully", agentAccount.Language)
            Else
                'sendmail will have added errors if it failed (which will be output below)

            End If
        End If
        Return String.Join(",", errorMessages.ToArray())
        'PnlErrors.Controls.Add(OutputErrors(errorMessages, lid))
    End Function

    Private Function addtoBasketSynnex() As String

        'Returns the required parameters to the GET string 

        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"))

        'the iq.Sesh(lid,"gk_BasketURL") should contain:-
        'http://ec.synnex.com/ecexpress/order/shoppingCart.do'

        Dim r$ = "?subAction=6&ref_id=" & iq.sesh(lid, "gk_token") & "&newBasket=" & quote.ID & "&quickAdd="

        For Each flatListItem In quote.RootItem.Flattened(True, False, 0).items
            r$ &= flatListItem.QuoteItem.SKUVariant.DistiSku & "+" & flatListItem.Quantity & ","
        Next

        If Right$(r$, 1) = "," Then 'which it will be !
            r$ = Left$(r$, Len(r$) - 1) 'remove the last comma
        End If

        Return r$

    End Function

    Private Function addToBasket(lidLocal As ULong, Optional ignoreCheck As Boolean = False) As String
        Dim agentAccount As clsAccount = CType(iq.sesh(lidLocal, "AgentAccount"), clsAccount)

        If quote IsNot Nothing Then
            Dim url As String = IIf(iq.sesh(lidLocal, "GK_BasketURL") Is Nothing, "", iq.sesh(lidLocal, "GK_BasketURL"))
            Dim xmlString As String = ""
            If agentAccount.SellerChannel.basketMode = "FRM" Then

                xmlString = quote.basketAsHiddenFields(lidLocal)
            Else

                If url.Length > 0 Or quote.AgentAccount.SellerChannel.orderEmail <> "" Then
                    'Generate the xml using the proxy class
                    Dim dt As Data = New Data()
                    dt.Quote = New DataQuote()
                    dt.Quote.ID = quote.ID
                    dt.Quote.Name = quote.Name.value
                    dt.Quote.CreatedBy = quote.AgentAccount.User.RealName
                    dt.Quote.Supplier = quote.AgentAccount.SellerChannel.Name
                    'dt.Quote.URLProductImage = quote.RootItem.Note.value 'need to ask nick abt this
                    Dim products As List(Of DataQuoteProduct) = New List(Of DataQuoteProduct)
                    Dim product As DataQuoteProduct
                    For Each flatListItem In quote.RootItem.Flattened(True, False, 0).items
                        product = New DataQuoteProduct()
                        product.Class = flatListItem.QuoteItem.Branch.Product.ProductType.Code
                        product.PartNum = flatListItem.QuoteItem.Branch.Product.SKU
                        product.SupplierPartNum = flatListItem.QuoteItem.SKUVariant.DistiSku

                        product.ListPrice = flatListItem.QuoteItem.ListPrice.value
                        product.Description = flatListItem.QuoteItem.Branch.DisplayName(quote.BuyerAccount.Language)
                        product.Qty = flatListItem.Quantity
                        product.URLProductImage = flatListItem.QuoteItem.Branch.Picture

                        products.Add(product)

                    Next
                    dt.Quote.Product = products.ToArray()

                    xmlString = SerializeToString(dt)

                End If
            End If

            iq.sesh(lidLocal, "basketContent") = xmlString

            Dim trueUri As Uri = New Uri(Request.Url.AbsoluteUri)
            Dim uri As String = "BasketPost.aspx"

            Return uri
        End If
    End Function

    Private Function addtoBasketSynnex(lidLocal As UInt64) As String

        'Returns the required parameters to the GET string 

        Dim agentAccount As clsAccount = CType(iq.sesh(lidLocal, "AgentAccount"), clsAccount)
        quote = agentAccount.Quotes(iq.sesh(lidLocal, "QuoteID"))

        'the iq.Sesh(lid,"gk_BasketURL") should contain:-
        'http://ec.synnex.com/ecexpress/order/shoppingCart.do'

        Dim r$ = "?subAction=6&ref_id=" & iq.sesh(lidLocal, "gk_token") & "&newBasket=" & quote.ID & "&quickAdd="

        For Each flatListItem In quote.RootItem.Flattened(True, False, 0).items
            r$ &= flatListItem.QuoteItem.SKUVariant.DistiSku & "+" & flatListItem.Quantity & ","
        Next

        If Right$(r$, 1) = "," Then 'which it will be !
            r$ = Left$(r$, Len(r$) - 1) 'remove the last comma
        End If

        Return r$

    End Function

    Private Function EmailBasket(lidlocal As ULong) As String
        'find the virtual, and from that the physical path to the app folder

        Dim buyerAccount As clsAccount = CType(iq.sesh(lidlocal, "BuyerAccount"), clsAccount)
        Dim agentAccount As clsAccount = CType(iq.sesh(lidlocal, "AgentAccount"), clsAccount)
        Dim QUOTE As clsQuote = agentAccount.Quotes(iq.sesh(lidlocal, "QuoteID"))
        Dim vPath = HttpContext.Current.Request.ApplicationPath
        Dim pPath = HttpContext.Current.Request.MapPath(vPath) & "\"

        Dim tf As String
        Dim fn As String
        fn = "Quotes\" & QUOTE.RootQuote.ID & "-" & QUOTE.Version & ".txt"
        tf = pPath & fn

        Try
            If My.Computer.FileSystem.FileExists(tf$) Then My.Computer.FileSystem.DeleteFile(tf$)
        Catch ex As Exception
            ErrorLog.Add(ex)

        End Try

        Dim objWriter As New System.IO.StreamWriter(tf)
        objWriter.WriteLine(iq.sesh(lidlocal, "basketContent"))
        objWriter.Close()

        Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
        tags.Add("customerName", Split(buyerAccount.User.RealName, " ")(0))
        tags.Add("quoteID", QUOTE.RootQuote.ID & "-" & QUOTE.Version.ToString)
        tags.Add("hostName", buyerAccount.SellerChannel.DisplayName(buyerAccount.Language))
        tags.Add("agentName", agentAccount.User.RealName)
        tags.Add("agentEmail", agentAccount.User.Email)
        tags.Add("mfr", agentAccount.Manufacturer.ToString())

        Dim runningTotal As NullablePrice = New NullablePrice(buyerAccount.Currency)
        Dim qb$ = QUOTE.RootItem.EmailSummary(True, buyerAccount, agentAccount, errorMessages, runningTotal)
        tags.Add("quoteBody", qb$)

        Dim toEmail As String = agentAccount.SellerChannel.orderEmail

        Dim attachment As System.Net.Mail.Attachment = Nothing
        If errorMessages.Count = 0 And tf <> "" Then
            attachment = New System.Net.Mail.Attachment(tf)
            tags.Add("attachmentInfo", " You will find basket xml attached")
        Else
            tags.Add("attachmentInfo", "Failed to generate attachment.")
        End If

        SendEmail(toEmail, "quote.htm", tags, buyerAccount.Language, errorMessages, False, attachment) 'agentAccount.User.Email

        If errorMessages.Count > 0 Then SimpleEmail("Support@channelcentral.net", "iQuote2 - config issue", Join(errorMessages.ToArray, ","))

        If errorMessages.Count = 0 Then
            Return Xlt("Mail sent successfully", agentAccount.Language)
        Else
            'sendmail will have added errors if it failed (which will be output below)
            Return Xlt("Failed to send email", agentAccount.Language)
        End If

    End Function


End Class