Option Strict On
Imports System.Web.UI.DataVisualization
Imports System.Net.Mail
Imports System.IO
Imports IQ.clsBranchState


Public Class tree1
    Inherits clsPageLogging

    Private Sub tree1_Init(sender As Object, e As System.EventArgs) Handles Me.Init
        'FillDDL(ddlbuyer, iq.channel.Values)
    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim quote As clsQuote
        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid").ToString, lid)

        If Not clsIQ.IsLoaded OrElse Not iq.SeshAlive(lid) Then Response.Redirect(String.Format("signin.aspx?badlid={0}", lid), False) : Exit Sub

        '    Dim adverts As Dictionary(Of Integer, clsAdvert) = New Dictionary(Of Integer, clsAdvert)
        If (Request("Quote") = "Browse") Then
            CloseBelow(lid, "tree.1")
            Dim root As String = iq.seshTyped(Of String)(lid, "Root")
            iq.sesh(lid, "path") = root
            Dim bts = iq.seshTyped(Of Dictionary(Of String, clsBranchState))(lid, "branchStates")
            If bts IsNot Nothing Then bts.Clear() 'wipetreestate
            iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem
            iq.sesh(lid, "showOnly") = Nothing
            iq.sesh(lid, "treeCursorPath") = root
            'iq.sesh(lid, "matrixHeaders").clear()
            If iq.seshDic(lid).ContainsKey("promoinforce") Then iq.seshDic(lid).Remove("promoinforce")
            iq.sesh(lid, "Quote") = Nothing

            Response.Redirect("tree.aspx?lid=" & Request.QueryString("lid") & If(Request("elid") <> "", "&elid=" & Request("elid"), ""), False) : Exit Sub
        End If
        If (Request("Quote") = "New") Then

            iq.sesh(lid, "QuoteID") = Nothing
            iq.sesh(lid, "Quote") = "New"
            iq.sesh(lid, "QuoteLocked") = False
            Dim bts = CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
            If bts IsNot Nothing Then bts.Clear() 'wipetreestate
            iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem
            Dim shs = CType(iq.sesh(lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
            If shs IsNot Nothing Then shs.Clear()
            iq.sesh(lid, "lastbranch") = Nothing
            If iq.seshDic(lid).ContainsKey("promoinforce") Then iq.seshDic(lid).Remove("promoinforce")
            're-boot the tree
            Dim root As String = iq.sesh(lid, "Root").ToString

            iq.sesh(lid, "path") = root

            Response.Redirect("tree.aspx?lid=" & Request.QueryString("lid") & If(Request("elid") <> "", "&elid=" & Request("elid"), ""), False) : Exit Sub

        End If

        ' If Not iq.sesh(lid, "UserID") Is Nothing Then 'are we logged in ?

        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        'this will add the full set of filter buttons into the form tag of the page
        If Not agentAccount Is Nothing Then Form.Controls.Add(MakeFilterButtons(agentAccount.Language))

        'the first time the page loads

        'when coming here from the listquotes page..
        'buyeraccount is *always* set - as without it there can be no pricing

        'needs moving to a aspx of its own which outputs new link each time - to be called from a 'showFocus() function - whch would be called back by any change
        'ie showFocus would Rexec 'focus.aspx' - which would output the (updated) title/link (into a DIV in Tree.aspx) - which would showFocus()
        'Foci as stroed in the session variable as a comma delimited list = but are made into a hashset   

        'build a hashset from the CD list stored in the sesstion variable
        Dim foci As HashSet(Of String) = New HashSet(Of String)(Split(iq.sesh(lid, "foci").ToString, ",").ToList)

        If buyerAccount.SellerChannel.Focus <> "" Then

            AddHandler FocusButton.Click, AddressOf SwitchFocus
            If foci.Count > 0 Then
                FocusLabel.Text = Xlt("Currently displaying:" & Join(foci.ToArray, ",") & " switch to ", buyerAccount.Language)
                FocusButton.Text = Xlt("All HP Products", buyerAccount.Language)
                FocusButton.Attributes("focus") = ""
                '    switch.Attributes("onclick") = "rExec('Manipulation.aspx?command=focus&value=', showQuote);"
            Else
                FocusLabel.Text = Xlt("Currently displaying: All HP products switch to ", buyerAccount.Language)
                FocusButton.Text = buyerAccount.SellerChannel.Focus
                FocusButton.Attributes("focus") = buyerAccount.SellerChannel.Focus
                '    Dim script$ = "rExec('Manipulation.aspx?command=focus&value=" & switch.Text & "', showQuote);"
                '    switch.Attributes("onclick") = script$
            End If
        End If

        If IsPostBack Then
            'BootStrap the tree 
            Dim root As String = iq.seshTyped(Of String)(lid, "Root")
            Dim pth As String = iq.seshTyped(Of String)(lid, "path")
            ' errorMessages.Add("page posted back - everything should be ajax")
            iq.sesh(lid, "path") = root 'BootStrap/Reset the tree
            iq.sesh(lid, "refreshing") = "true"

            Dim bi As clsBranchInfo = New clsBranchInfo(lid, root, Nothing, 1000, CType(iq.sesh(lid, "Paradigm"), enumParadigm), errorMessages)
            bi.open(errorMessages, False)
        Else

            'first' time to the page (from signin - or from find a quote) - (or F5 is pressed - which is NOT a postback)
            'BootStrap the tree 
            Dim root As String = iq.seshTyped(Of String)(lid, "Root")
            Dim pth As String = iq.seshTyped(Of String)(lid, "path")
            'If pth = "" Then
            iq.sesh(lid, "path") = root 'BootStrap/Reset the tree
            iq.sesh(lid, "refreshing") = "true"

            ''Create the dictionary that holds all the (important) information about branch state for this user session
            If iq.sesh(lid, "branchStates") Is Nothing Then
                Dim branchStates As Dictionary(Of String, clsBranchState) = New Dictionary(Of String, clsBranchState)
                iq.sesh(lid, "branchStates") = branchStates
            End If

            Dim bi As clsBranchInfo = New clsBranchInfo(lid, root, Nothing, 1000, CType(iq.sesh(lid, "Paradigm"), enumParadigm), errorMessages)

            bi.open(errorMessages, False)

            'we re-point the autosuggest dictionary to the SellerChannels CustomerAccounts - just in time
            'so that we can provide autosuggest of only this sellers customers
            iq.Gateway("accounts") = agentAccount.SellerChannel.CustomerAccounts

            'we want to create a new account - where the seller channel is the agents channel and the buyer channel is th one in Autosuggested channel 
            'Then launch the editor

            'Dim btnAddContact As Button = New Button
            'btnAddContact.Text = "Add Contact"
            'pnlAddContactButton.Controls.Add(btnAddContact)

            'Dim scr$
            'scr$ = "rExec('Manipulation.aspx?command=CreateSiblingAccount&AccID='+ctl00_MainContent_txtBuyerID, nullFunc);"

            'Dim callback$ 'this is executed when the rExec of manipualtion.aspx finishes - really want some way fetch/pass the ID of the accountwe just created

            ''      callback$ = "function(){injectIframe('editor/Editor.aspx?path=Accounts','PnlAddContact');return false;}"
            'Dim url$ = "../editor/Editor.aspx?path=Accounts(" & iq.sesh(lid,"BuyerAccountIDd") & ")"
            'callback$ = "function(){injectIframe('" & url$ & "','ctl00_MainContent_PnlAddContact');return false;}"
            ''Manipulation.aspc createSibilingAccount will set iq.sesh(lid,"buyeraccount") 
            'scr$ = "rExec('Manipulation.aspx?command=CreateSiblingAccount&AccID='+ctl00_MainContent_txtBuyerID.value," & callback & ");return false;"

            ''We give it 300ms to create the account - which should be plenty of time - then.. edit it
            ''scr$ &= "setTimeout(function(){injectIframe('editor/Editor.aspx?dic=channels&key='+ctl00_MainContent_txtBuyerID+'&screenid=" & iq.i_screens_code("Chann").ID & "','PnlAddContact');return false;},300);"
            'btnAddContact.OnClientClick = scr$

            If Not buyerAccount Is agentAccount Then txtBuyer.Enabled = False ' lock it ! (if the quote is already assigned to a customer)

            ' LblBuyer.Text = buyerAccount.displayName(s_lang)
            txtBuyer.Text = buyerAccount.displayName(s_lang)
            '  PnlProductTree.CssClass = "visible"

            'retreiving a previewed quote - TODO - check the agentaccount is correct as a security measure
            If Request("QuoteID") IsNot Nothing AndAlso Val(Request("QuoteID")) <> 0 Then
                iq.sesh(lid, "QuoteID") = CInt(Request("QuoteID"))
            End If

            If iq.SeshContains(lid, "QuoteID") Then
                Dim quoteID As Integer = iq.seshTyped(Of Integer)(lid, "QuoteID")
                If quoteID <> 0 Then
                    quote = agentAccount.Quotes(quoteID)
                    If quote.RootItem.Children.Count = 0 Then quote.LoadItems(errorMessages)

                    txtQuoteName.Text = quote.Name.DisplayValue 'important (especially wehn reloading quotes!)


                End If
            Else
                'txtBuyer.Text = buyerAccount.displayName(s_lang)

            End If
            txtBuyer.Text = buyerAccount.displayName(s_lang)

            If Not iq.sesh(lid, "Base") Is Nothing Then
                ' Register a client-side script to handle adding the requested SKU to the basket
                ' The sesh variable is deleted to ensure this is a one-off add
                Dim sku = iq.sesh(lid, "Base").ToString()
                iq.sesh(lid, "Base") = Nothing
                RegisterDeepLinkScript(lid, sku, buyerAccount)
            End If

        End If

        'BRAZIL - Very temporary - Sam will put this somewhere more sensible
        Dim ddlWarehouse As New DropDownList
        With ddlWarehouse
            .Items.Add("")    'Shows all warehouses/varaints
            .Items.Add("NONE") 'show no warehouse prices (shows list price)
            .Items.Add("TST")  'Shows the specific TST warehouse variants

            .ID = "ddlWarehouse"

            '  <select name="ctl00$MainContent$ddlWarehouse" id="ctl00_MainContent_ddlWarehouse" onchange="burstBubble(event);getBranches('cmd=setWarehouse&amp;warehouse=' + document.getElementById('ddlWarehouse').value);return false;">
		
            Dim script As String = "burstBubble(event);getBranches('cmd=setWarehouse&warehouse=' + document.getElementById('ctl00_MainContent_ddlWarehouse').value);return false;"
            .Attributes("onchange") = script
            warehousePanel.Controls.Add(ddlWarehouse)
        End With

        ' litAdvert.Text = getAdverts(lid)


        'Randomize()

        ''   If adverts.Count > 1 Then
        'rndNumber = Int(Rnd() * adverts.Count)
        ''Else
        ''rndNumber = 1
        ''End If

        'Dim selectedAdvert As clsAdvert = adverts.Values(rndNumber)
        'If selectedAdvert IsNot Nothing Then

        '    Dim advertImpressions As clsImpression = New clsImpression(agentAccount, selectedAdvert, Now)
        '    Dim urlString As String = selectedAdvert.ImageUrl
        '    If String.IsNullOrEmpty(urlString) = False Then
        '        advertLiteral.Text = "<a onclick=""clickthru(" & selectedAdvert.ID & ");""><img alt="""" src=""" & urlString & """   /></a>"
        '    End If
        'End If

        ' kwlit.Text = "<h2>" & Xlt("Keyword Search", buyerAccount.Language) & "</h2>"

        OutputErrors(Page.Controls, errorMessages, lid, True)

        'NB: The JS OnLoad() in the page (tree.aspx) - will now fire.. calling getbranches (see bottom to tree.aspx markup)


    End Sub

    Private Sub RegisterDeepLinkScript(lid As ULong, sku As String, buyerAccount As clsAccount)

        Dim cs As ClientScriptManager = Page.ClientScript

        Dim script As String
        Dim baseSku = sku

        If (Not String.IsNullOrEmpty(baseSku)) AndAlso (baseSku.Length > 4) Then
            Dim i As Integer = baseSku.IndexOf("#", baseSku.Length - 4)
            If i > 0 Then
                ' sku ends with #ABC - trim off to get the base code
                baseSku = baseSku.Substring(0, i)
            End If
        End If

        If iq.i_SKU.ContainsKey(baseSku) Then

            Dim product = iq.i_SKU(baseSku)
            Dim branch As clsBranch = iq.Branches.Values.Where(Function(b) (Not b.Product Is Nothing) AndAlso (b.Product.isSystem) AndAlso (b.Product.ID = product.ID)).FirstOrDefault

            If Not branch Is Nothing Then

                ' Build the path
                Dim path As String = String.Empty
                Dim root As Integer = 0
                BuildPath(branch, path, root)
                If Not String.IsNullOrEmpty(path) Then
                    path = "tree." & path
                End If

                If iq.RootBranch.ID = root Then

                    ' Find the variant

                    Dim priceList = product.GetPrices(buyerAccount, buyerAccount.SellerChannel.priceConfig, iq.AllVariants, errorMessages, True)
                    If priceList.Count > 0 Then

                        Dim price As clsPrice = Nothing
                        If priceList.Count = 1 Then
                            price = priceList(0)
                        Else
                            price = priceList.Where(Function(p) String.Equals(p.SKUVariant.DistiSku, sku, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault
                            If price Is Nothing Then
                                price = priceList(0)
                            End If
                        End If

                        If (Not price Is Nothing) AndAlso (Not price.SKUVariant Is Nothing) Then
                            ' Register the client-side script to add the requested SKU to the basket
                            script = String.Format("deepLink('{0}', '{1}');", path, price.SKUVariant.ID)
                            cs.RegisterStartupScript(Me.GetType(), "DeepLinkScript", script, True)

                            ' Call PloughPath to render the tree to the required point
                            PloughPath(lid, path, errorMessages, 0, enumParadigm.configuringSystem)
                        End If

                    End If
                End If
            End If
        End If

    End Sub


    ''' <summary>
    ''' Builds the path of the passed branch
    ''' </summary>
    ''' <param name="branch"></param>
    ''' <param name="path"></param>
    ''' <remarks></remarks>
    Private Sub BuildPath(branch As clsBranch, ByRef path As String, ByRef root As Integer)

        If path = String.Empty Then
            path = branch.ID.ToString()
        Else
            path = branch.ID.ToString() & "." & path
        End If
        root = branch.ID

        If Not branch.Parent Is Nothing Then
            BuildPath(branch.Parent, path, root)
        End If

    End Sub

    Private Function SwitchFocus(b As Object, e As System.EventArgs) As EventHandler
        Dim but = CType(b, Button)

        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid").ToString, lid)


        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)

        Dim focus As String = but.Attributes("focus")
        If focus = "" Then 'We're switching to 'all'
            iq.sesh(lid, "focus") = New List(Of String)
            FocusLabel.Text = Xlt("Currenct displaying All HP Products swtich to ", buyerAccount.Language)
            but.Text = buyerAccount.SellerChannel.Focus
        Else
            'we're focusing on one.. (or more)
            FocusLabel.Text = Xlt("Currently displaying:" & focus & " switch to ", buyerAccount.Language)
            but.Text = Xlt("All HP Products", buyerAccount.Language)
            iq.sesh(lid, "foci") = focus
        End If

    End Function


    'Protected Sub btnXMLQuote_Click(sender As Object, e As EventArgs) Handles btnXMLQuote.Click



    '    Stop


    '    'DEPRECATED (I'm pretty sure) - nick

    '    Dim lid As UInt64 = Request.QueryString("lid")

    '    'should be invisible really (until we have a quote)
    '    If iq.sesh(lid, "QuoteID") IsNot Nothing Then


    '        Dim quote As clsQuote
    '        quote = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))

    '        quote.Name = New nullableString(txtQuoteName.Text)
    '        quote.Update()

    '        Dim fullpath$
    '        fullpath$ = SaveXML(quote.XMLDoc(errorMessages), "/quotes/" & quote.RootQuote.ID & "-" & quote.Version & ".xml", LblSave.Text)

    '        OutputErrors(PnlErrors.Controls, errorMessages, lid)

    '        If fullpath <> "FAIL" Then
    '            iq.sesh(lid, "tostream") = fullpath$
    '            Response.Redirect("streamer.aspx?lid=" & lid)
    '        Else
    '            LblSave.BackColor = Drawing.Color.Red : LblSave.ForeColor = Drawing.Color.White

    '        End If
    '    End If

    '    'Response.Clear()
    '    'Response.AppendHeader("Content-Disposition", "attachment;filename=Quote-" & quote.ID & ".xml")
    '    'Response.ContentType = "text/xml"
    '    'Response.AddHeader("Content-Length", My.Computer.FileSystem.GetFileInfo(fullpath).Length.ToString())

    '    'Response.Flush()
    '    'Response.WriteFile(fullpath$)
    '    'Response.End()

    'End Sub


    'Protected Sub BtnExcel_Click(sender As Object, e As EventArgs) Handles BtnExcel.Click


    '    'Obsolete/Deprecated

    '    Stop
    '    Dim lid As UInt64 = Request.QueryString("lid")

    '    'should be invisible really (until we have a quote)
    '    If iq.sesh(lid, "QuoteID") Is Nothing Then
    '        errorMessages.Add("You need to add some items to the quote first")
    '    Else

    '        Dim fullpath$
    '        Dim QUOTE As clsQuote = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))

    '        QUOTE.Name = New nullableString(txtQuoteName.Text)
    '        QUOTE.Update()

    '        fullpath$ = ODS.OutputQuote(QUOTE, "Quotes", errorMessages) 'the OutputQuote() function returns the full physical path the the file generated on the server

    '        If errorMessages.Count = 0 Then
    '            iq.sesh(lid, "tostream") = fullpath$
    '            iq.sesh(lid, "streamcontent-type") = "application/vnd.ms-excel;charset=UTF-8"
    '            iq.sesh(lid, "DeleteStreamed") = True
    '            Response.Redirect("streamer.aspx?lid=" & lid)
    '        Else
    '            'LblSave.Text = errors : LblSave.BackColor = Drawing.Color.Red : LblSave.ForeColor = Drawing.Color.White

    '        End If
    '    End If

    '    OutputErrors(PnlErrors.Controls, errorMessages, lid)

    'End Sub

    'Protected Sub BtnPDF_Click(sender As Object, e As EventArgs) Handles BtnPDF.Click

    '    Dim lid As UInt64 = Request.QueryString("lid")

    '    Dim fullpath$
    '    Dim QUOTE As clsQuote = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))

    '    QUOTE.Name = New nullableString(txtQuoteName.Text)
    '    QUOTE.Update()


    '    'will write an ODS - PDFgen on the server is (or should be !) watching the folder

    '    fullpath$ = ODS.OutputQuote(QUOTE, "PDFQuotes", errorMessages) 'the OutputQuote() function returns the full physical path the the file generated on the server

    '    'PDF Gen should spot it in second or so.. (and convert it to PDF)

    '    If errorMessages.Count = 0 Then
    '        iq.sesh(lid, "tostream") = Left$(fullpath$, InStrRev(fullpath$, ".")) & ".pdf"
    '        iq.sesh(lid, "streamcontent-type") = "application/pdf"
    '        iq.sesh(lid, "DeleteStreamed") = True

    '        BtnPDF.Text = Xlt("One moment please...", iq.sesh(lid, "language"))

    '        'gives the PDGgen time to happen
    '        Response.AddHeader("REFRESH", "3;URL=streamer.aspx?lid=" & lid)
    '    Else
    '        'LblSave.Text = errors : LblSave.BackColor = Drawing.Color.Red : LblSave.ForeColor = Drawing.Color.White
    '        OutputErrors(PnlErrors.Controls, errorMessages, lid)

    '    End If


    'End Sub

    'Protected Sub Btnsave_Click(sender As Object, e As EventArgs) Handles Btnsave.Click

    '    Dim lid As UInt64 = Request.QueryString("lid")

    '    'should be invisible really (until we have a quote)
    '    If iq.sesh(lid, "QuoteID") IsNot Nothing Then

    '        Dim quote As clsQuote
    '        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)


    '        quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"))
    '        quote.Name = New nullableString(txtQuoteName.Text)
    '        quote.Update()
    '        quote.RootItem.updateRecursive()

    '        '        Dim NewQuote As clsquote

    '        '        NewQuote = quote.CreateNextVersion

    '        'agentaccount.Quotes.Add(NewQuote.ID, NewQuote) - not neccessary - creating the quote adds it to the agents list

    '        'iq.sesh(lid,"QuoteID") = NewQuote.ID

    '        LblSave.Text = Xlt("Version " & quote.Version & " saved.", agentAccount.Language) ' ,version " & NewQuote.Version & " created"

    '    End If


    'End Sub

    'Protected Sub BtnStartQuote_Click(sender As Object, e As EventArgs) Handles BtnStartQuote.Click

    '    'Don't look here ! .... see manipulation.aspx - StartQuote  (which is called via javascript and ajax)

    'End Sub

    'Protected Sub BtnEmail_Click(sender As Object, e As EventArgs) Handles BtnEmail.Click

    '    Dim state$ = ""

    '    Dim lid As UInt64 = Request.QueryString("lid")

    '    If iq.sesh(lid, "QuoteID") Is Nothing Then

    '        errorMessages.Add("You must add something to the quote first")
    '    Else

    '        state$ = "RQ"
    '        Dim fullpath$
    '        Dim QUOTE As clsQuote = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))

    '        state$ = "UQ"
    '        QUOTE.Name = New nullableString(txtQuoteName.Text)
    '        QUOTE.Update()

    '        state$ = "OQ"
    '        Dim errors As String = ""
    '        fullpath$ = ODS.OutputQuote(QUOTE, "Quotes", errorMessages) 'the OutputQuote() function returns the full physical path the the file generated on the server

    '        state$ = "AR"
    '        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
    '        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

    '        Dim vPath = HttpContext.Current.Request.ApplicationPath
    '        Dim pPath = HttpContext.Current.Request.MapPath(vPath) & "\"


    '        state$ = "SR " & pPath
    '        Dim tr As StreamReader = Nothing
    '        Dim b$ = ""
    '        Try
    '            tr = New StreamReader(pPath & "EMT/quote.htm")
    '            b$ = tr.ReadToEnd()
    '            tr.Close()
    '        Catch ex As System.Exception
    '            tr.Dispose()
    '        End Try

    '        'Tags are...
    '        '<subject>Welcome to Iqoute 2</subject>
    '        '<p>Dear <customerName/>,</p>
    '        '<p>Your <hostName/> iQuote quotation ID:<quoteID/> prepared for you by <agentName/> is shown below - You will also find an spreadsheet compatible version attached.
    '        '<quoteBody/>

    '        state$ = "RT "

    '        Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
    '        tags.Add("customername", Split(buyerAccount.User.RealName, " ")(0))
    '        tags.Add("quoteid", QUOTE.RootQuote.ID & "-" & QUOTE.Version.ToString)
    '        tags.Add("hostname", buyerAccount.SellerChannel.DisplayName(buyerAccount.Language))
    '        tags.Add("agentname", agentAccount.User.RealName)

    '        Dim runningTotal As NullablePrice = New NullablePrice(buyerAccount.Currency)
    '        Dim qb$ = QUOTE.RootItem.EmailSummary(True, buyerAccount, agentAccount, errorMessages, runningTotal)
    '        tags.Add("quotebody", qb$)

    '        Dim to$
    '        to$ = buyerAccount.User.Email

    '        Dim attachment As System.Net.Mail.Attachment = New System.Net.Mail.Attachment(fullpath$)


    '        SendEmail(agentAccount.User.Email, "quote.htm", tags, buyerAccount.Language, errorMessages, False, attachment)

    '        'state$ = "IC "
    '        'Dim smtpclient As New System.Net.Mail.SmtpClient


    '        'msg = New MailMessage("support@channelcentral.net", to$, "Your iQuote 2 quotation" & QUOTE.RootQuote.ID & "-" & QUOTE.Version & " from " & buyerAccount.SellerChannel.DisplayName(buyerAccount.Language), b$)

    '        'msg.ReplyToList.Add(New MailAddress(AgentAccount.User.Email))
    '        'msg.CC.Add(New MailAddress("support@channelcentral.net"))
    '        'msg.CC.Add(New MailAddress(AgentAccount.User.Email))  'CC the agent

    '        If errorMessages.Count = 0 Then
    '            LblSave.BackColor = Drawing.Color.Green
    '            LblSave.ForeColor = Drawing.Color.White
    '            LblSave.Text = Xlt("Mail sent successfully", agentAccount.Language)
    '        Else
    '            'sendmail will have added errors if it failed (which will be output below)

    '        End If
    '    End If

    '    OutputErrors(PnlErrors.Controls, errorMessages, lid)

    'End Sub
    Private Function getAdverts(lid As UInt64) As String
        Dim adverts As Dictionary(Of Integer, clsAdvert) = New Dictionary(Of Integer, clsAdvert)
        Dim quote As clsQuote
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim adLiteral As String = ""
        If iq.SeshContains(lid, "QuoteID") Then
            Dim quoteID As Integer = iq.seshTyped(Of Integer)(lid, "QuoteID")
            If quoteID <> 0 Then
                quote = agentAccount.Quotes(quoteID)
                If quote.RootItem.Children.Count = 0 Then quote.LoadItems(errorMessages)
            End If
        End If

        adverts.Clear()
        For Each advert As clsAdvert In From a In iq.Adverts.Values Where a.Manufacturer = Manufacturer.Unknown OrElse a.Manufacturer = agentAccount.Manufacturer
            If advert.Campaign.StartDate.Date <= Today.Date And advert.Campaign.EndDate.Date >= Today.Date Then
                If advert.Campaign.Seller Is agentAccount.SellerChannel Then
                    If advert.Campaign.Region.Encompasses(agentAccount.SellerChannel.Region) Then
                        If quote Is Nothing Then
                            adverts.Add(advert.ID, advert)
                        Else
                            Dim addAdvert As Boolean = True
                            'If advert.Present IsNot Nothing Then
                            If advert.Present.Code <> "none" Then
                                If Not quote.RootItem.HasProductType(advert.Present) Then
                                    addAdvert = False
                                End If
                            End If
                            'If advert.Absent IsNot Nothing Then
                            If advert.Absent.Code <> "none" Then
                                If quote.RootItem.HasProductType(advert.Absent) Then
                                    addAdvert = False
                                End If
                            End If
                            If addAdvert Then
                                adverts.Add(advert.ID, advert)
                            End If
                        End If
                    End If
                End If
            End If
        Next


        Dim randomClass As New Random()
        Dim rndNumber As Integer
        Dim RememberSet As New HashSet(Of Integer)

        While RememberSet.Count < 3 AndAlso adverts.Count > 1
            rndNumber = randomClass.Next(0, adverts.Count - 1)
            If RememberSet.Add(rndNumber) Then
                Dim selectedAdvert As clsAdvert = adverts.Values(rndNumber)
                If selectedAdvert IsNot Nothing Then
                    Dim advertImpressions As clsImpression = New clsImpression(agentAccount, selectedAdvert, Now)
                    Dim urlString As String = selectedAdvert.ImageUrl
                    If String.IsNullOrEmpty(urlString) = False Then
                        adLiteral &= "<a onclick=""clickthru(" & selectedAdvert.ID & ",'" & selectedAdvert.URL & "');""><img classs = ""bannerimg""  alt="""" src=""" & urlString & """   /></a>"
                    End If
                End If
            End If

        End While
        If adLiteral.Length > 0 Then
            If iq.seshTyped(Of String)(lid, "Root").ToString() = iq.seshTyped(Of String)(lid, "path").ToString() Then
                adLiteral = "<div id=""bannerAd"" class =""bannerdivright"">" & adLiteral & " </div>"
            Else
                adLiteral = "<div id=""bannerAd"" class =""bannerdivtop"">" & adLiteral & " </div>"
            End If

        End If

        Return adLiteral
    End Function
End Class