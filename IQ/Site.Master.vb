'Option Strict On
Imports IQ.clsBranchState
Imports System.Net.Mail
Imports System.Linq
Imports System.Threading
Imports System.Reflection

Public Class Site
    Inherits System.Web.UI.MasterPage
    Public submitString As String = "Submit Feedback"
    Dim language As clsLanguage

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid"), lid)

        Page.Title = "iQuote"

        Dim agentAccount As clsAccount = Nothing
        If lid <> 0 Then
            If iq.SeshContains(lid, "AgentAccount") Then
                agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
            End If
        End If
        chkTax.Attributes.Add("onclick", "return HandleOnCheck()")
        ' hpSPLIT dynamic stylesheet - see also updateHeader(css)
        ' SK - expanded to handle manufacturer-specific style sheet selection for the Universal sign-in pages
        ' SK - now also handles channelcentral.css on the SignIn and Accounts pages to override the HP styles
        Dim css As Literal = New Literal
        Dim mfrCode As String = Nothing
        Dim universal As Boolean = False
        Dim stylesheet As String = Nothing
        Dim s As String = Nothing


        If agentAccount Is Nothing OrElse String.IsNullOrEmpty(agentAccount.mfrCode) Then

            ' Not logged in yet, but we might be on a manufacturer-specific HP Universal sign-in page, in which case
            ' we want to pick the HPE/HPI-specific style sheet before login

            ' Create a case-insensitive dictionary for the Request parameters; could be used more widely
            Dim requestParams As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.InvariantCultureIgnoreCase)
            For Each key As String In Request.QueryString
                If Not key Is Nothing Then requestParams.Add(key, Request.QueryString(key))
            Next

            ' Read or infer the MFR code to determine whether we can use a HPE or HPI specific stylesheet
            If iq.SeshContains(lid, "MFR") Then
                Dim mfr As Manufacturer = iq.sesh(lid, "MFR")
                If mfr = Manufacturer.HPE Then
                    mfrCode = "HPE"
                ElseIf mfr = Manufacturer.HPI Then
                    mfrCode = "HPI"
                End If
            ElseIf requestParams.ContainsKey("base") Then
                Dim sku As String = requestParams("base")
                If iq.i_SKU.ContainsKey(sku) Then
                    Dim product = iq.i_SKU(sku)
                    If product.Manufacturer <> Manufacturer.Unknown Then
                        mfrCode = product.mfrCode
                    End If
                End If

            ElseIf requestParams.ContainsKey("mfr") Or requestParams.ContainsKey("mfg") Then

                If requestParams.ContainsKey("mfr") Then
                    mfrCode = requestParams("mfr")
                ElseIf requestParams.ContainsKey("mfg") Then
                    mfrCode = requestParams("mfg")
                End If

                If (Not String.Equals(mfrCode, "hpi", StringComparison.InvariantCultureIgnoreCase)) AndAlso (Not String.Equals(mfrCode, "hpe", StringComparison.InvariantCultureIgnoreCase)) Then
                    mfrCode = Nothing
                End If
            Else
                ' Might be able to infer the manufacturer from the referrer URL
                mfrCode = InferUniversalManufacturer(Request)
                If Not String.IsNullOrEmpty(mfrCode) Then universal = True
            End If

            If Not String.IsNullOrEmpty(mfrCode) Then
                stylesheet = String.Format("Site-{0}", mfrCode)
            Else
                stylesheet = "channelcentral"
            End If
        Else
            mfrCode = agentAccount.mfrCode
            stylesheet = String.Format("Site-{0}", mfrCode)
        End If

        If Not String.IsNullOrEmpty(mfrCode) Then

            If (String.Equals(mfrCode, "hpi", StringComparison.InvariantCultureIgnoreCase)) Then
                Page.Title = "iQuote - HP Inc."
            ElseIf (String.Equals(mfrCode, "hpe", StringComparison.InvariantCultureIgnoreCase)) Then
                Page.Title = "iQuote - Hewlett Packard Enterprise"
            End If

        End If

        css.Text = String.Format("<link href='{0}Styles/{1}.css' rel='stylesheet' type='text/css' />", ResolveUrl("~/"), stylesheet)
        Page.Header.Controls.Add(css)

        'This WAS the static version (in the designer)
        '<link href="<%# ResolveUrl("~/") %>Styles/Site.css" rel="stylesheet" type="text/css" /> 

        If Not clsIQ.IsLoaded Then
            If String.IsNullOrEmpty(mfrCode) Then
                Response.Redirect("Loading.aspx?path=" + Request.Url.AbsoluteUri, False) : Exit Sub
            Else
                Response.Redirect(String.Format("Loading.aspx?path={0}&mfr={1}", Request.Url.AbsoluteUri, mfrCode), False) : Exit Sub
            End If
        End If

        If Application("IQ") Is Nothing Then Application("IQ") = iq

        If Request("elevate") <> "" And Not Request.Url.AbsoluteUri.Contains("signin.aspx") Then
            Response.Redirect("signin.aspx?lid=" & lid & "&elevate=1", False)
            Exit Sub
        End If

        Dim a As Assembly = Assembly.GetExecutingAssembly

        litVersion.Text = a.GetName.Version.ToString

        ' Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1))
        ' Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache)
        ' Response.Cache.SetNoStore()
        BtnFeedback.Visible = True
        UpdateHeader(css) 'shows user name info and signed in status

        'quick fix for the fact the tools manned was visible whne swtiching accounts
        If InStr(LCase(Me.Parent.ToString), "accounts_aspx") > 0 Then newHeader.Visible = False

        If InStr(LCase(Me.Parent.ToString), "universal_aspx") > 0 Then
            loginDisplay.Visible = False
            newHeader.Visible = False
            litSubmit.Visible = False
            legalLink.Value = ""
            litVersion.Visible = False
            legalLink.Visible = False
        End If
        If agentAccount IsNot Nothing AndAlso agentAccount.BuyerChannel.Region.Code = "BR" Then
            wareHouseli.Visible = True
        Else
            wareHouseli.Visible = False
        End If

        If InStr(LCase(Me.Parent.ToString), "signin_aspx") > 0 Then
            'we're on the sign in page - hide the signOut Button
            'BtnSignOut.Visible = False
        Else
            If lid <> 0 Then
                If iq.sesh(lid, "AgentAccount") IsNot Nothing Then
                    iq.sesh(lid, "currentPage") = Me.Request.RawUrl.ToString

                End If
            End If

            'If iq.sesh(lid,"UserID") Is Nothing And InStr(Me.Parent.ToString, "default_aspx") = 0 Then
            '    Response.Redirect("signin.aspx")
            '    Response.End()
            '    Exit Sub
            'End If

            ' If iq.sesh(lid,"AgentAccount") = "" Then
            ' Response.Redirect("signin.aspx")
            ' Response.End()
            '  End If

        End If

        'hide the tools link if were on quotes or resources pages (Tools need a tree div to populate)
        If InStr(LCase(Me.Parent.ToString), "tree_aspx") = 0 Then
            toolsLink.Visible = False
        End If

        'If InStr(LCase(Me.Parent.ToString), "accountsettings") > 0 Then
        '    NavigationMenu.Items(0).Enabled = True
        'Else
        '    NavigationMenu.Items(0).Enabled = False
        'End If

        language = English

        If Not agentAccount Is Nothing Then
            If Not agentAccount.HasRight("SHOWALL") Then btnPortFolio.Visible = False
            If Not agentAccount.HasRight("SHOWERRORS") Then btnErrorDisplay.Visible = False
            language = agentAccount.Language
            submitString = Xlt("Submit Feedback", language)
        End If

        If Not IsPostBack Then
            If (Not iq.sesh(lid, "showAll") Is Nothing) AndAlso (CBool(iq.sesh(lid, "showAll"))) Then
                btnPortFolio.Text = "Show Portfolio"
                btnPortFolio.CommandArgument = "port"
            End If

            If (Not iq.sesh(lid, "ErrorDisplay") Is Nothing) AndAlso (CBool(iq.sesh(lid, "ErrorDisplay") = False)) Then
                btnErrorDisplay.Text = "Hide Errors"
                btnErrorDisplay.CommandArgument = "hide"
            End If

            If (Not iq.sesh(lid, "treeMode") Is Nothing) AndAlso (CBool(iq.sesh(lid, "treeMode"))) Then
                BtnTreeMode.Text = "Normal Mode" 'set the button text to switch 'back' (to normal  mode)
                BtnTreeMode.CommandArgument = "norm"
            End If
        End If

        'If (Request.QueryString(s) IsNot Nothing) AndAlso (Request.QueryString(s).ToLower().Contains("universal")) Then
        '    legalLink.Visible = False
        '    litSubmit.Text = "<input type=""button"" class=""textButton"" />"
        '    litVersion.Visible = False
        'Else
        '    litSubmit.Text = "<input type=""button"" value=""" & submitString & """  onclick=""feedbackClick();"" class=""textButton"" />"
        'End If

        ' Hide Legal/Submit Feedback/Version No. UI from sign up/register screens for Universal
        Dim url As String = Request.Url.AbsoluteUri.ToLower()
        Dim hide As Boolean = False
        If url.Contains("hpsignup.aspx") Then
            hide = True
        ElseIf url.Contains("signin.aspx") Then
            If (universal) OrElse (Request.QueryString(s) IsNot Nothing AndAlso Request.QueryString(s).ToLower().Contains("universal")) Then
                hide = True
            End If
        End If

        If hide Then
            legalLink.Visible = False
            litSubmit.Text = "<input type=""button"" class=""textButton"" />"
            litVersion.Visible = False
        Else
            litSubmit.Text = "<input type=""button"" value=""" & submitString & """  onclick=""feedbackClick();"" class=""textButton"" />"
        End If

        If Not IsPostBack Then
            If agentAccount IsNot Nothing Then
                txtFeedBackFrom.Text = agentAccount.User.Email
            Else
                txtFeedBackFrom.Text = "you@youremail.com"
            End If
        End If

        If Not Request.Url.AbsolutePath.ToLower.Contains("tree.aspx") Then

            searchMenuItem.Style.Add("visibility", "hidden")
        End If


        '~~~If Request.Url.AbsolutePath.Contains("accounts.aspx") Then btnBrowse.Visible = False
        'BtnFeedback.Attributes("onclick") = "thanks.style.display='block';"  'adds the script to show a thank you when they submit
        'TRANSLATION - of all the 'static' elements on all pages - labels, tooltips etc on things like login pages, account choice, charting
        'Note content that is subsequently ajax'd in (eg the basket) is generally translated 'just in time'
        If lid <> 0 Then
            For Each c In MainContent.Controls
                If TypeOf (c) Is WebControls.TextBox Or TypeOf (c) Is WebControls.Image Or TypeOf (c) Is WebControls.Label Then
                    c.tooltip = Xlt(c.text, language)
                End If
                If TypeOf (c) Is WebControls.Label Or TypeOf (c) Is WebControls.Button Then
                    c.text = Xlt(c.text, language)
                End If
            Next
            If iq.sesh(lid, "feedbackSent") IsNot Nothing Then
                lblMsg.Text = iq.sesh(lid, "feedbackSent").ToString()
                iq.sesh(lid, "feedbackSent") = ""
            End If

        End If

        'If (Request("Tools") = "True") Then
        '    PnlShoppingList.Visible = True
        'Else
        '    PnlShoppingList.Visible = False
        'End If

        BtnFeedback.Text = TranslateUI("Feedback")

        BtnFeedback.Attributes("class") = "hpOrangeButton sfb"

        feedbacktype.Items.Add(New ListItem(TranslateUI("Logon or password problem"), "logon"))
        feedbacktype.Items.Add(New ListItem(TranslateUI("Usability issue"), "usability"))
        feedbacktype.Items.Add(New ListItem(TranslateUI("Data/catalogue issues"), "data"))
        feedbacktype.Items.Add(New ListItem(TranslateUI("Spelling or grammar correction"), "spelling"))
        feedbacktype.Items.Add(New ListItem(TranslateUI("Language/Translation correction"), "translation"))
        feedbacktype.Items.Add(New ListItem(TranslateUI("Feature request"), "feature"))
        feedbacktype.Items.Add(New ListItem(TranslateUI("Suggestion"), "suggest"))
        feedbacktype.Items.Add(New ListItem(TranslateUI("Bug report"), "bug"))
        feedbacktype.Items.Add(New ListItem(TranslateUI("Other"), "other"))


        For Each wareHouseLoc In iq.Locations
            drpLocation.Items.Add(New ListItem(wareHouseLoc.Value, wareHouseLoc.Key))
        Next

        drpWareHouse.Items.Add(New ListItem("All", ""))
        drpWareHouse.Items.Add(New ListItem("None", "NONE"))
        drpWareHouse.Items.Add(New ListItem("Test", "TST"))

        chkAllow.Text = TranslateUI("Allow support staff to see my iQuote session")
        Label1.Text = TranslateUI("Email Address")
        Label2.Text = TranslateUI("Feedback Type")
        Label3.Text = TranslateUI("Your feedback")
        Label4.Text = TranslateUI("Consent")
        Label5.Text = TranslateUI("Thanks for your feedback - we will respond shortly !")
        txtFeedbackLanguage.Items.AddRange(iq.ActiveLanguages.Select(Function(lan) New ListItem(lan.Value.displayName(English), lan.Value.ID)).ToArray)

        Dim buyerAccount As clsAccount
        If lid <> 0 AndAlso iq.SeshAlive(lid) AndAlso iq.seshDic(lid).ContainsKey("BuyerAccount") AndAlso Not iq.sesh(lid, "BuyerAccount") Is Nothing Then
            buyerAccount = iq.sesh(lid, "BuyerAccount")
            txtFeedbackLanguage.SelectedValue = buyerAccount.Language.ID
        Else
            txtFeedbackLanguage.SelectedValue = English.ID
        End If

        '  Literal1.Text = "<input type="" button"" value=""" & TranslateUI("Submit feedback") & """ onclick=""feedbackClick();"" class=""textButton"" />"

        Label5.Text = TranslateUI("Thank you for contacting HP iQuote Support. Your feedback has been received and you will be contacted shortly.")
        'Literal1.Text = "<input type=""button"" value=""" & TranslateUI("Feedback") & """ onclick=""feedbackClick();"" class=""textButton"" />"

        If agentAccount IsNot Nothing AndAlso (agentAccount.HasRight("ADMINMENU") Or agentAccount.HasRight("GLOBALADM")) Then admMenu.Visible = True

        'on the face of it - this is a bit crap - BUT - the masterpage is only loaded very infrequently (eveyrthing is ajax'd)
        'so having the full terms and conditions in the response isn't actually a big deal

        If agentAccount Is Nothing Then
            If String.Equals(mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase) AndAlso iq.Legal.ContainsKey("HPELegal") Then
                terms.Text = iq.Legal("HPELegal").Translation.text(English)
            ElseIf String.Equals(mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase) AndAlso iq.Legal.ContainsKey("HPILegal") Then
                terms.Text = iq.Legal("HPILegal").Translation.text(English)
            ElseIf iq.Legal.ContainsKey("CCLegal") Then
                terms.Text = iq.Legal("CCLegal").Translation.text(English)
            Else
                terms.Text = "Usage of iQuote means that you agree to the following Terms & Conditions:<br/><br/> Every care is taken to ensure that the information contained within this site is accurate, however Errors and Omissions Excepted."
            End If
        Else
            ' We're logged-in: display legal terms from the seller channel
            terms.Text = agentAccount.SellerChannel.Legal
        End If

    End Sub

    Public Sub UpdateHeader(ByRef css As Literal)

        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid"), lid)

        'NavigationMenu.Visible = False
        newHeader.Visible = False
        HyperBack.Visible = False
        newMenu.Visible = False
        btnNav.Visible = False

        If lid = 0 Then
            LblRole.Text = "Not logged in"
            'if we're not signed in yet and we're NOT on the signin page (ie, we're on reset password, or choose account) then 
            'show the 'back to signin' link
            If InStr(LCase(Me.Parent.ToString), "signin_aspx") = 0 Then
                HyperBack.Visible = True
            End If
            Exit Sub
        End If

        'switch account
        'NavigationMenu.Items(1).Text = ""
        'NavigationMenu.Items(1).Enabled = False

        If InStr(LCase(Me.Parent.ToString), "signout_aspx") = 0 Then
            newMenu.Visible = True
            If InStr(LCase(Me.Parent.ToString), "accounts_aspx") > 0 Then
                switchAccount.Visible = False
                accountSetting.Visible = False
            End If
        End If

        If iq.sesh(lid, "screenName") IsNot Nothing AndAlso InStr(LCase(Me.Parent.ToString), "signout_aspx") = 0 Then
            Dim screenName As String = iq.sesh(lid, "screenName")
            LblRole.Text = screenName
        Else
            LblRole.Text = "Not logged in"
        End If

        If iq.SeshContains(lid, "AgentAccount") And Not iq.sesh(lid, "AgentAccount") Is Nothing Then

            'NavigationMenu.Visible = True
            newHeader.Visible = True
            newMenu.Visible = True
            btnNav.Visible = True
            Dim agentAccount As clsAccount
            Dim buyerAccount As clsAccount
            agentAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
            buyerAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)

            If (iq.sesh(lid, "viaGatekeeper")) AndAlso (TypeOf (iq.sesh(lid, "AccountList")) Is IEnumerable(Of clsAccount)) Then
                ' If we've logged in via the gatekeeper, use the list of buyer accounts created there (i.e. filtered by channel) to work out if
                ' there are multiple accounts available
                Dim accountList As IEnumerable(Of clsAccount) = iq.sesh(lid, "AccountList")
                If Not accountList Is Nothing AndAlso accountList.Count = 1 Then
                    switchAccount.Visible = False
                End If
            ElseIf agentAccount.User.Accounts.Count = 1 Then
                switchAccount.Visible = False
            End If

            If Not String.IsNullOrEmpty(agentAccount.mfrCode) Then
                ' Add a manufacturer-specific style sheet to override any styles in the generic sheet
                css.Text = "<link href='" & ResolveUrl("~/") & "Styles/Site-" & agentAccount.mfrCode.ToLower & ".css ' rel='stylesheet' type='text/css' />"
            End If

            Dim t1 As String = ""
            If agentAccount.SellerChannel.pricesLoadedFor.ContainsKey(buyerAccount.Priceband) Then
                t1 = agentAccount.SellerChannel.pricesLoadedFor(buyerAccount.Priceband).ToString()
            End If

            If InStr(LCase(Me.Parent.ToString), "accounts_aspx") = 0 Then
                LblRole.Text = agentAccount.User.RealName & " - " & String.Join(",", agentAccount.Roles.Select(Function(r) r.Translation.text(s_lang))) & " - " & agentAccount.SellerChannel.Name & " (" & agentAccount.SellerChannel.Region.Code & ")" & If(iq.seshDic(lid).ContainsKey("Elevated"), "<font color='red'>ELEVATED</font>", "")
                LblRole.ToolTip = "AgAcID:" & agentAccount.ID & " " & agentAccount.SellerChannel.Code & " " & _
                    "SlrChID:" & agentAccount.SellerChannel.ID & " variants:" & agentAccount.SellerChannel.countVariants & " PrcCfg:" & agentAccount.SellerChannel.priceConfig & " BaPB:" & buyerAccount.Priceband.text & " Pc(pb):" & t1 & " MfrCode:" & buyerAccount.mfrCode
            End If

            If AccountHasRight(lid, "TREEVIEW") Then
                BtnTreeMode.Visible = True
                'Dim treemode As Boolean = iq.sesh(lid, "treeMode")
                'If treemode Then BtnTreeMode.Text = "Normal Mode" Else BtnTreeMode.Text = "Tree Mode" 'this seems backwards - but isn't  -- Ml 110914 removed this bit as its fired at the start and then after the button click so the logic reverses in each case, moved to the actual button click
            Else
                BtnTreeMode.Visible = False
            End If

            '  LblQuotingFor.Text = iq.Accounts(iq.sesh(lid,"buyeraccount")).displayname(s_lang)
            ''Admin mode

            'If agentaccount.User.Accounts.Count > 1 Then
            '    NavigationMenu.Items(1).Text = Xlt("Switch Account", agentaccount.Language)
            '    NavigationMenu.Items(1).Enabled = True
            'End If
            'ElseIf lid > 0 And iq.SeshContains(lid, "AgentAccount") = False Then
            '    newHeader.Visible = True
            '    newMenu.Visible = True
            '    'btnNav.Visible = True
            '    accountSetting.Visible = False
            '    switchAccount.Visible = False
        ElseIf InStr(LCase(Me.Parent.ToString), "accounts_aspx") > 0 Then
            newMenu.Visible = True
            switchAccount.Visible = False
            accountSetting.Visible = False
        End If


        ttlMN.Attributes.Add("title", Environment.MachineName)
    End Sub
    'Protected Sub NavigationMenu_MenuItemClick(sender As Object, e As System.Web.UI.WebControls.MenuEventArgs) Handles NavigationMenu.MenuItemClick

    '    Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))

    '    Dim mi As WebControls.Menu
    '    mi = CType(sender, WebControls.Menu)


    '    'sp:MenuItem NavigateUrl="" Text="New Quote" Value="new"/>
    '    '    <asp:MenuItem NavigateUrl="~/listquotes.aspx" Text="Find Quote" Value="find"/>
    '    '    <asp:MenuItem Text="Sign Out" Value="signOut"></asp:MenuItem>
    '    '    <asp:MenuItem Text="Admin Mode" Value="admin"></asp:MenuItem>
    '    '    <asp:MenuItem Text="Shopping List"  
    '    'ToolTip = "Build a quote from an ordered list of systems and their options."
    '    '        Value="shoppingList"></asp:MenuItem>
    '    '    <asp:MenuItem Text="Swift" 
    '    'ToolTip = "Display a 'flat' list of the valid options for a system unit"
    '    '        Value="swift"></asp:MenuItem>
    '    '</Ite

    '    If Not iq.SeshAlive(lid) Then
    '        Response.Redirect("Signin.aspx")
    '    Else

    '        If mi.SelectedValue = "new" Then
    '            iq.sesh(lid, "QuoteID") = Nothing
    '            iq.sesh(lid, "branchStates").clear() 'wipetreestate
    '            Dim errormessages As List(Of String) = New List(Of String)

    '            'set the root node to render its' children as squares Bootstrap the tree (we cant create branchinfo or know the visiblechildren util we know the account)
    '            'clsBranchState.setBranchState(lid, "tree.1", oc.open, bt.BreadCrumb, False)
    '            'Dim bi As clsBranchInfo = New clsBranchInfo(lid, "tree.1", Nothing, 1000)
    '            'bi.setChildBranches(oc.closed, bt.Square, False)

    '            setBranchState(lid, "tree.1", oc.open, bt.BreadCrumb, False) ' this ID needed (without it we cant make BranchInfo)
    '            Dim bi As clsBranchInfo = New clsBranchInfo(lid, "tree.1", Nothing, 1000, enumParadigm.AddingSystem, errormessages)
    '            bi.setChildBranches(oc.closed, bt.Square, False, errormessages)

    '            Response.Redirect("tree.aspx?lid=" & lid)  '

    '        ElseIf mi.SelectedValue = "signOut" Then
    '            'Session.Abandon()
    '            Response.Redirect("Signout.aspx?lid=" & lid)  'AddHeader("Refresh", "1") 'this page will refresh (afetr 1 second) - without a session it will redirect to the login page
    '        ElseIf mi.SelectedValue = "switchAccount" Then
    '            'Session.Abandon()
    '            Dim dic As Dictionary(Of String, Object) = iq.getSeshDic(lid)
    '            dic.Remove("AgentAccount")
    '            'NavigationMenu.Visible = False
    '            Response.Redirect("accounts.aspx?lid=" & lid)  'AddHeader("Refresh", "1") 'this page will refresh (afetr 1 second) - without a session it will redirect to the login page

    '        ElseIf mi.SelectedValue = "admin" Then

    '            Dim adminMode As Boolean = False
    '            If iq.SeshContains(lid, "admin") Then
    '                adminMode = CBool(iq.sesh(lid, "admin"))
    '            End If

    '            If adminMode Then
    '                'switch admin off
    '                iq.sesh(lid, "showAll") = False
    '                iq.sesh(lid, "admin") = False
    '            Else
    '                'switch admin on
    '                iq.sesh(lid, "showAll") = True
    '                iq.sesh(lid, "admin") = True

    '                'For Each k In Session.Keys
    '                '    If Left(k, 5) = "open." Then
    '                '        toKill.Add(k)
    '                '    End If
    '                '    If Left(k, 4) = "rca." Then
    '                '        toKill.Add(k)
    '                '    End If
    '                'For Each k In toKill : Session.Remove(k) : Next

    '            End If

    '            UpdateHeader()

    '            '    Session.Abandon()
    '            '    Response.Redirect("Signin.aspx")  'AddHeader("Refresh", "1") 'this page will refresh (afetr 1 second) - without a session it will redirect to the login page
    '        ElseIf mi.SelectedValue = "find" Then
    '            Response.Redirect("listquotes.aspx?lid=" & lid)

    '        ElseIf mi.SelectedValue = "shoppingList" Then

    '            PnlShoppingList.Visible = True


    '        ElseIf mi.SelectedValue = "settings" Then
    '            Response.Redirect("accountSettings.aspx?lid=" & lid)
    '        ElseIf mi.SelectedValue = "myquote" Then
    '            Response.Redirect("tree.aspx?lid=" & lid)

    '        Else
    '            Beep()
    '        End If
    '    End If

    'End Sub


    Protected Sub BtnFeedback_Click(sender As Object, e As EventArgs) Handles BtnFeedback.Click


        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))

        Dim smtpclient As New System.Net.Mail.SmtpClient

        Dim msg As MailMessage

        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        Try

            Dim address As String = iq.Addresses("iQuoteSupportEmail").Translation.text(English)
            msg = New MailMessage(address, address) '"Support@channelcentral.net", "development@channelcentral.net")
            msg.Subject = "iQuote2 Feedback" '.User.RealName() & " at " & .BuyerChannel.BusinessName
            'msg.CC.Add("tom.legge@channelcentral.net")
            msg.Body = "<h2>Feedback Session Details</h2>" & _
            "<table>" & _
            "<tr><th>Manufacturer</th><td>" & If(agentAccount IsNot Nothing, agentAccount.ManufacturerDescription, "Not logged in") & "</td></tr>" & _
            "<tr><th>Date</th><td>" & DateTime.Now.ToString() & "</td></tr>" & _
            "<tr><th>Account UserName/Email</th><td>" & If(agentAccount IsNot Nothing, agentAccount.User.Email, "Not logged in") & "</td></tr>" & _
            "<tr><th>Entered email</th><td>" & txtFeedBackFrom.Text & "</td></tr>" & _
            "<tr><th>Contact Name</th><td>" & txtFeedbackName.Text & "</td></tr>" & _
            "<tr><th>IP Address</th><td>" & Request.UserHostAddress.ToString & "</td></tr>" & _
            "<tr><th>User Agent</th><td>" & Request.UserAgent.ToString & "</td></tr>" & _
            "<tr><th>Session lid</th><td>" & If(agentAccount IsNot Nothing, lid.ToString, "Not logged in") & "</td></tr>" & _
            "<tr><th>Buyer Host Id</th><td>" & If(agentAccount IsNot Nothing, agentAccount.BuyerChannel.Code, "Not logged in") & "</td></tr>" & _
            "<tr><th>Seller Host Id</th><td>" & If(agentAccount IsNot Nothing, agentAccount.SellerChannel.Code, "Not logged in") & "</td></tr>" & _
            "<tr><th>Price Config</th><td>" & If(agentAccount IsNot Nothing, agentAccount.BuyerChannel.DecodedPriceConfig, "Not logged in") & "</td></tr>" & _
            "<tr><th>Gatekeeper</th><td>" & If(agentAccount IsNot Nothing, If(iq.seshDic(lid).ContainsKey("gk_token"), "YES", "NO"), "Not logged in") & "</td></tr>" ' Need to bring GK accross

            'If agentAccount IsNot Nothing AndAlso agentAccount.BuyerChannel.priceConfig And 4 Then
            '    Try
            '        Dim sql$ = "select top 1 * from pricing.pna.feed where BuyerAccount_ID = " & agentAccount.BuyerChannel.ID & " order by timestamp desc for xml auto"
            '        msg.Body &= "<tr><th>Price Config</th><td>" & dataAccess.da.DBSelectFirst(sql)(0).ToString & "</td></tr>"
            '    Catch ex As Exception
            '        ErrorLog.Add(ex)
            '    End Try
            'End If

            msg.Body &= "<tr><th>Prefered Contact (if any)</th><td>" & txtContactDetails.Text & "</td></tr>" & _
            "<tr><th>Preferred Language</th><td>" & txtFeedbackLanguage.SelectedItem.Text & "</td></tr>" & _
            "<tr><th>Consent to access session</th><td>" & If(chkAllow.Checked, "Yes", "No") & "</td></tr>" & _
            "<tr><th>Product revision</th><td>" & Assembly.GetExecutingAssembly.GetName.Version.ToString & "</td></tr>" & _
            "<tr><th>Category</th><td>" & feedbacktype.SelectedValue & "</td></tr>" & _
            "</table>"

            btndiv.Style("display") = "none"
            msg.ReplyToList.Add(txtFeedBackFrom.Text)

            msg.Body &= "<h2>Notes</h2><br>"
            msg.Body &= txtFeedback.Text

            '            msg.Body &= "<p><b>User agent:" & Request.UserAgent.ToString & "</b>"
            '           msg.Body &= "<p><b>IP:" & Request.UserHostAddress.ToString & "</b>"

            'msg.Body &= "<p><b>IP:" & Request.UserHostAddress.ToString & "</b>"
            'msg.Body &= "<p><b>IP:" & Request.UserHostAddress.ToString & "</b>"

            'Dim a As Assembly = Assembly.GetExecutingAssembly
            'msg.Body &= "<p><b>BUILD:" & a.GetName.Version.ToString & "</b>"




            'If chkAllow.Checked Then
            'msg.Body &= "User has given consent to <a href='" & Request.Url.AbsoluteUri & "'>View Session</a>"
            'else
            'MSg.Body &= "<p style=color:red>The user has not allowed access to their session</p>"
            'End If


            msg.IsBodyHtml = True
            msg.Priority = MailPriority.High
            'End With

            'smtpclient.Host = "smtp.fasthosts.co.uk"
            'smtpclient.EnableSsl = False
            'smtpclient.Credentials = New Net.NetworkCredential("support@hpiquote.net", "ny7zZLvk9s0c")
           
            smtpclient.ServicePoint.MaxIdleTime = 1
            '  smtpclient.DeliveryMethod = SmtpDeliveryMethod.Network
            smtpclient.Send(msg)
            lblMsg.Text = TranslateUI("Email sent successfully")
            iq.sesh(lid, "feedbackSent") = TranslateUI("Email sent successfully")
            'Response.Redirect(Request.Url.ToString())
        Catch ex As Exception
            ErrorLog.Add(ex)
            lblMsg.Text = TranslateUI("Failed to send email. please try again later.")
            iq.sesh(lid, "feedbackSent") = TranslateUI("Failed to send email. please try again later.")

        Finally
            txtFeedback.Text = ""
        End Try
    End Sub

    Protected Sub feedbacktype_SelectedIndexChanged(sender As Object, e As EventArgs) Handles feedbacktype.SelectedIndexChanged

    End Sub


    Protected Sub btnErrorDisplay_Click(sender As Object, e As EventArgs) Handles btnErrorDisplay.Click

        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))

        ' Use the button's CommandArgument to work out what mode we're switching to. Simply toggling means a browser refresh
        ' triggers a toggle, which is probably unintended
        Dim btnErrorDisplay As Button = DirectCast(sender, Button)
        If btnErrorDisplay.CommandArgument = "show" Then
            btnErrorDisplay.Text = "Hide Errors"
            btnErrorDisplay.CommandArgument = "hide"
            iq.sesh(lid, "ErrorDisplay") = False
        Else
            btnErrorDisplay.Text = "Show Errors"
            btnErrorDisplay.CommandArgument = "show"
            iq.sesh(lid, "ErrorDisplay") = True
        End If

    End Sub

    Protected Sub btnPortfolio_Click(sender As Object, e As EventArgs) Handles btnPortFolio.Click

        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))

        ' Destroy all the data tables (we'll need to re-make these) now we're switching
        Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
        matrixHeaders.Clear()

        ' Use the button's CommandArgument to work out what mode we're switching to. Simply toggling means a browser refresh
        ' triggers a toggle, which is probably unintended
        Dim btnPortfolio As Button = DirectCast(sender, Button)
        If btnPortfolio.CommandArgument = "port" Then
            btnPortfolio.Text = "Show All"
            btnPortfolio.CommandArgument = "all"
            iq.sesh(lid, "showAll") = False 'Switch to showing portfolio only
        Else
            btnPortfolio.Text = "Show Portfolio"
            btnPortfolio.CommandArgument = "port"
            iq.sesh(lid, "showAll") = True 'SWITCH to showing all
        End If

    End Sub


    Protected Sub BtnTreeMode_Click(sender As Object, e As EventArgs) Handles BtnTreeMode.Click

        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))

        ' Use the button's CommandArgument to work out what mode we're switching to. Simply toggling means a browser refresh
        ' triggers a toggle, which is probably unintended
        Dim btnTreeMode As Button = DirectCast(sender, Button)
        If btnTreeMode.CommandArgument = "norm" Then
            iq.sesh(lid, "treeMode") = False 'Switch to 'normal' mode 
            setBranchState(lid, "tree.1", New clsBranchState(lid, "tree.1", enumBt.Square, False, 0, 100))
            btnTreeMode.Text = "Tree Mode" 'set the button text to switch 'back' (to treee mode)
            btnTreeMode.CommandArgument = "tree"
        Else
            iq.sesh(lid, "treeMode") = True 'SWITCH to tree mode
            btnTreeMode.Text = "Normal Mode" 'set the button text to switch 'back' (to normal  mode)
            btnTreeMode.CommandArgument = "norm"
        End If

    End Sub

    Public Function TranslateUI(text As String) As String

        ''WHY ??? - If Not clsIQ.IsLoaded Then Response.Redirect("SystemMaintenance.aspx", False) : Exit Function

        If clsIQ.IsLoaded Then
            If language IsNot Nothing Then
                Return Xlt(text, language)
            Else
                Return Xlt(text, iq.i_language_Code("EN"))
            End If
        Else
            Return "XX"
        End If

    End Function

    Public Sub HideContent()
        MainContent.Visible = False
    End Sub
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Page.Header.DataBind()
    End Sub


    Protected Sub btnContinue_Click(sender As Object, e As EventArgs)
        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))
        Dim custContext As clsCustomerContext = New clsCustomerContext()
        custContext.Location = drpLocation.SelectedValue
        custContext.Tax = chkTax.SelectedValue
        custContext.WareHouse = drpWareHouse.SelectedValue
        iq.sesh(lid, "custContext") = custContext
        btnContext.Value = "Modify"
        btnContext.Attributes.Remove("class")
        btnContext.Attributes.Add("class", "hpBlueButton2")
        iq.sesh(lid, "Quote") = Nothing

    End Sub

    Protected Sub btnCancel_Click(sender As Object, e As EventArgs)
        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))
        If iq.SeshContains(lid, "QuoteID") And iq.sesh(lid, "custContext") Is Nothing Then
            iq.sesh(lid, "QuoteID") = Nothing
            iq.sesh(lid, "Quote") = Nothing
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
    End Sub
End Class