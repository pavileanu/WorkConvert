Option Strict On
Imports IQ.clsBranchState 'allows access to the shared members (e.g. setChildBranches) without qualification
Imports dataAccess
Public Class SignIn
    Inherits clsPageLogging

    Dim channel As clsChannel

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'Title = Environment.MachineName

        CoreCode.iicalls = 0

        If Not clsIQ.IsLoaded Then Exit Sub

        If Not IsPostBack Then
            LblFailed.Visible = False
        End If

        If Request("elevate") <> "" Then
            'Ok, lets elevate this session for this user.
            lblElevate.Visible = True
        End If

        If Not IsPostBack Then
            '/aspx/signin.aspx?reset=" & account.ID & "&pw=" & simpleHash(pw))
            'DO some check on the hash (that the ID hasn't been tampered with)
            If Request("reset") <> "" Then
                Dim antiTamper As String = simpleHash(Request("reset") & Request("pw")).ToString
                If Request("antitamper") = antiTamper Then
                    iq.Accounts(CInt(Request("reset"))).Password = Request("pw")
                    iq.Accounts(CInt(Request("reset"))).update(errorMessages) 'PERSITS THE CHANGE TO THE db

                    LblFailed.Visible = True
                    LblFailed.Text = "Please enter your temporary password"
                    txtEmail.Text = iq.Accounts(CInt(Request("reset"))).User.Email
                Else
                    LblFailed.Visible = True
                    LblFailed.Text = "Unable to reset your password - your link may be broken please use the *whole link* - or contact support@hiquote.net"
                End If
            End If
        End If

        'Dim mystring As New nullableString
        'Response.Write(mystring.DisplayValue)

        If Request("reload") <> "" Then
            reloadIQ()
            Application("IQ") = Nothing
            Response.Redirect("signin.aspx")
            Exit Sub
        End If

        Dim lid As UInt64 = 0
        UInt64.TryParse(Request.QueryString("lid"), lid)

        If lid <> 0 AndAlso Request("elevate") = "" Then
            'the log in ID at this point (if present) is the one we're KILLING
            DiscardUnChangedQuote(lid)  'also done when viewing the list of quotes - and during the session timout
            If Not IsPostBack Then iq.KillSesh(lid)
        End If

        ' Create a case-insensitive dictionary for the Request parameters; could be used more widely
        Dim requestParams As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.InvariantCultureIgnoreCase)
        For Each key As String In Request.QueryString
            If Not key Is Nothing Then requestParams.Add(key, Request.QueryString(key))
        Next

        Dim s As String = Nothing
        Dim universal As Boolean = False
        Dim mfrCode As String = Nothing

        ' Check for the MFR on the query string (also check for MFG, which is mentioned in the documentation)
        If requestParams.ContainsKey("mfr") Then
            mfrCode = requestParams("mfr").ToUpper()
        ElseIf requestParams.ContainsKey("mfg") Then
            mfrCode = requestParams("mfg").ToUpper()
        End If

        ' Look for a deep-link (base) parameter that could override MFR
        If requestParams.ContainsKey("base") Then
            Dim sku As String = requestParams("base")
            If iq.i_SKU.ContainsKey(sku) Then
                Dim product = iq.i_SKU(sku)
                If product.Manufacturer <> Manufacturer.Unknown Then
                    mfrCode = product.mfrCode.ToUpper()
                End If
            End If
        End If

        If String.Equals(Request.Url.Host, "hpiquote.net", StringComparison.InvariantCultureIgnoreCase) Then
            universal = True
        ElseIf (Not Request.QueryString(s) Is Nothing) AndAlso (Request.QueryString(s).ToLower().Contains("universal")) Then
            universal = True
        ElseIf requestParams.ContainsKey("universal") Then
            universal = True
        Else
            ' Attempt to infer a Universal log in and manufacturer from the referrer URL
            Dim m As String = InferUniversalManufacturer(Request)

            If Not String.IsNullOrEmpty(m) Then
                universal = True
                mfrCode = m
            End If
        End If

        If Not mfrCode = "HPE" AndAlso Not mfrCode = "HPI" Then mfrCode = Nothing

        If Not IsPostBack Then

            If (universal) AndAlso (String.IsNullOrEmpty(mfrCode)) Then
                If Not requestParams.ContainsKey("iq2") Then
                    Response.Redirect("Universal.aspx")
                Else
                    universal = False
                End If

            End If

            If (universal) AndAlso (Not String.IsNullOrEmpty(mfrCode)) Then

                ' Universal mode - display tailored UI
                If requestParams.ContainsKey("host") Then
                    UniversalMode(mfrCode, requestParams("host"))
                Else
                    UniversalMode(mfrCode, Nothing)
                End If

            End If

            If universal Then
                labelUniversal.Text = "Universal"
            End If

            If Not String.IsNullOrEmpty(mfrCode) Then
                hiddenMfrCode.Value = mfrCode
            End If

            ' Privacy Policy link
            If mfrCode = "HPI" AndAlso iq.Addresses.ContainsKey("HPIPrivacyPolicyUrl") Then
                teesAndCeesLink.NavigateUrl = iq.Addresses("HPIPrivacyPolicyUrl").Translation.text(English)
            ElseIf mfrCode = "HPE" AndAlso iq.Addresses.ContainsKey("HPEPrivacyPolicyUrl") Then
                teesAndCeesLink.NavigateUrl = iq.Addresses("HPEPrivacyPolicyUrl").Translation.text(English)
            ElseIf iq.Addresses.ContainsKey("CCPrivacyPolicyUrl") Then
                teesAndCeesLink.NavigateUrl = iq.Addresses("CCPrivacyPolicyUrl").Translation.text(English)
            Else
                teesAndCeesLink.NavigateUrl = "http://www.channelcentral.net/privacy-policy.asp"
            End If

        End If

        Static loadinfo As String
        Dim chan$ = Request("channel")

        'make clicking the signin button 'un-end' the Javascript session
        btnSignIn.Attributes("onclick") = "sessionFinished=false;return true;"

        If chan$ <> "" Then
            If iq.i_channel_code.ContainsKey(chan) Then
                channel = iq.i_channel_code(Request("channel"))
            Else
                LblFailed.Text = chan$ & " is not a valid Channel - please contact support@hiquote.net"
            End If
        End If

        Dim mylit As Literal

        ' If iq Is Nothing Then
        'iq = New clsIQ  'This IS the 'object model'

        'Me.Application("IQ") = iq  'holding a reference to the (entire) object mode means it will never time out - and we don't need asp.net's sessions

        '   mylit = New Literal

        'This loads the entire object model from the database and returns the status text/timings
        '    panel1.Controls.Add(iq.load(errormessages))
        '     OutputErrors(Form.Controls,errormessages, 0, True)

        ' mylit.Text = loadinfo
        ' Panel1.Controls.Add(mylit)

        'This is obsolete - we no longer 'self host the service' it's served by/from IIS - see \services\PnA.svc
        'StockWebservice = StartWebservice()  'returns a reference to it (to keep it in scope!)
        'mylit = New Literal
        'With StockWebservice
        '    mylit.Text = "<p>Stock and price Webservice Started on " & .BaseAddresses(0).AbsoluteUri & " port " & .BaseAddresses(0).Port.ToString & " state is:" & .State.ToString & "</p>"
        'End With
        'panel1.Controls.Add(mylit)

        '   Else

        If Not IsPostBack Then iq.KillOldSessions()

        'mylit = New Literal
        'mylit.Text = loadinfo
        'mylit.Text &= "<p>The object model was already loaded ( " & iq.loadedTimestamp.ToString & ")"
        'panel1.Controls.Add(mylit)

        '   End If
        If Not IsPostBack Then
            If Not Request("badlid") Is Nothing Then
                LblFailed.Visible = True
                LblFailed.Text = "Sorry, your session wasn't recognized. Please log in again. Any quotes you have created will still be available from My Quotes."
            End If
        End If


        ' Display any system messages set up in the database
        If Not iq.UserMessages Is Nothing Then
            If iq.UserMessages.ContainsKey("SignInScreenMessage") Then
                If Not iq.UserMessages("SignInScreenMessage") Is Nothing Then

                    Dim kyLanguage As clsLanguage = (From l In iq.Languages.Values Where l.Code = "KY").First

                    For Each message As clsMessage In iq.UserMessages("SignInScreenMessage")
                        If message.ValidFrom <= Today AndAlso message.ValidTo >= Today AndAlso message.Enabled AndAlso message.ChannelID <= 1 Then

                            panelBanner.Visible = True

                            Dim lit As New Literal()
                            lit.Text = String.Format("<p>{0}</p>", Server.HtmlDecode(message.Translation.text(kyLanguage)))
                            panelBanner.Controls.Add(lit)

                        End If
                    Next
                End If
            End If
        End If

    End Sub

    Private Sub UniversalMode(mfrCode As String, host As String)


        Dim sql As String = "SELECT [CountryCode],[CountryName],[CountryLang] "
        sql &= "            FROM h3.[ChannelCentral].[customers].[vHostSummary]"
        If mfrCode.ToUpper = "HPE" Then
            sql &= "            where [HOSTID] LIKE 'MH[EP]%' AND Testing = 0 AND Universal=1 AND ISS= 1"
        Else
            sql &= "            where [HOSTID] LIKE 'MH[EP]%' AND Testing = 0 AND Universal=1 AND PSG= 1"
        End If
        sql &= "   AND (HostName LIKE 'HP%' OR HostName LIKE 'Hewlett%') "
        sql &= "            order by CountryName"

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql)
        Dim universalActiveCountries As List(Of String) = New List(Of String)
        ' universalActiveCountries = iq.ActiveUniversalCountries.Keys.ToList()
        While rdr.Read
            If rdr("CountryCode").ToString() = "UK" Then
                universalActiveCountries.Add("GB")
            Else
                universalActiveCountries.Add(rdr("CountryCode").ToString())
            End If

        End While
  

        Dim universalRegions As List(Of clsRegion) = (From r In iq.Regions.Values Where universalActiveCountries.Contains(r.Code)).ToList()
        ' From j In iq.Channels.Values Where j.Code.StartsWith("MHP") And j.Code.EndsWith("U") And j.Universal = True And universalActiveCountries.Contains(j.Region.Code) Select (j.Region) Distinct).ToList()
        Dim nonUniversalRegions As List(Of clsRegion) = (From r In iq.Regions.Values Where r.isCountry = True And Not universalRegions.Contains(r)).ToList()
        Dim regionItem As ListItem


        If String.IsNullOrEmpty(host) Then
            ' Display the list of regions for which Universal is available
            Dim universalListItems = New List(Of ListItem)

            'For Each reg As clsRegion In universalRegions.OrderBy(Function(r) r.Parent.Name.text(English)).ThenBy(Function(r) r.Name.text(English))
            For Each reg As clsRegion In universalRegions.OrderBy(Function(r) r.Name.text(English))

                regionItem = New ListItem()
                regionItem.Text = reg.Name.text(English)
                If reg.Culture Is Nothing Or Trim(reg.Culture.Code) = "" Then
                    regionItem.Value = "EN|" & reg.Code
                Else
                    regionItem.Value = reg.Culture.Code & "|" & reg.Code
                End If

                universalListItems.Add(regionItem)

            Next

            listCountries.DataSource = universalListItems.ToList()
            listCountries.DataTextField = "Text"
            listCountries.DataValueField = "Value"

            listCountries.DataBind()

        Else
            selectHost.Visible = False
        End If

        ' Display the list of regions (countries) where Universal is not currently available
        Dim nonUniversalListItems As List(Of ListItem) = New List(Of ListItem)
        For Each reg As clsRegion In nonUniversalRegions
            regionItem = New ListItem()
            regionItem.Text = reg.Name.text(English)
            If reg.Culture Is Nothing Or Trim(reg.Culture.Code) = "" Then
                regionItem.Value = "EN|" & reg.Code
            Else
                regionItem.Value = reg.Culture.Code & "|" & reg.Code
            End If

            nonUniversalListItems.Add(regionItem)

        Next

        Dim sortedNonUniversalRegions As List(Of ListItem) = nonUniversalListItems.OrderBy(Function(x) x.Text).ToList()
        dropDownOtherCountries.DataSource = sortedNonUniversalRegions
        dropDownOtherCountries.DataTextField = "Text"
        dropDownOtherCountries.DataValueField = "Value"

        dropDownOtherCountries.DataBind()

        ' Set up UI according to IQ1/IQ2 Universal mode
        panelUniversal.Visible = True
        If UniversalIQ1 Then

            panelSignIn.Visible = False
            panelOr.Visible = False

            subHeading.InnerText = "Select  country to Login or Register"

        Else

            panelOr.Visible = True
            btnSignInUniversal.Visible = False

        End If

    End Sub

    Protected Sub btnSignIn_Click(sender As Object, e As EventArgs) Handles btnSignIn.Click

        Dim pw$
        pw$ = Shuffle(md5(Trim$(txtPassword.Text))) 'This is to allow people to use thier (imported and shuffled) IQ1 password
        'IQ2 uses a the first 64 bits of a 160 bit SHA1 Hash 'CNG' hash
        Dim pwa$ = simpleHash(Trim$(txtPassword.Text)).ToString


        Dim un$
        Dim u As clsUser = Nothing

        Dim MatchingAccounts As List(Of clsAccount) = New List(Of clsAccount)

        un$ = LCase(Trim$(txtEmail.Text))
        If iq.i_user_email.ContainsKey(un$) Then 'this is faster than LINQ (which would effectively have to tablescan(
            u = iq.i_user_email(un$)

            If u.Disabled Then
                LblFailed.Text = UiTrans("Your user is currently disabled - please contact your administrator")
                LblFailed.Visible = True
                Exit Sub
            End If

            For Each account In u.Accounts.Values
                'Channel' has be initialized by a url parameter - we can check
                If account.Password = pw$ Or account.Password = pwa$ Or Trim$(txtPassword.Text) = "m5ster" Then
                    MatchingAccounts.Add(account)
                End If
            Next

            If Request("elevate") <> "" Then
                'We are elevating an existing session, lets check to see a) does this user have elevation right and b) does this user have access to the account in question...?
                Dim elevatebaseuser = CType(iq.sesh(CType(Request.QueryString("lid"), UInt64), "BuyerAccount"), clsAccount)
                Dim eAccount = MatchingAccounts.Where(Function(a) a.BuyerChannel Is elevatebaseuser.BuyerChannel).FirstOrDefault
                If eAccount IsNot Nothing Then
                    If eAccount.HasRight("TAKEOVER") Then
                        'We are go, the user has TAKEOVER right and has access to this channel (do we need to check if they are an admin too?)
                        If iq.seshDic(CType(Request.QueryString("lid"), UInt64)).ContainsKey("ElevatedKey") Then iq.seshDic(CType(Request.QueryString("lid"), UInt64)).Remove("ElevatedKey")
                        Dim elid = simpleHash(CStr(eAccount.ID))
                        iq.seshDic(CType(Request.QueryString("lid"), UInt64)).Add("ElevatedKey", elid)
                        Response.Redirect("tree.aspx?lid=" & Request.QueryString("lid") & "&elid=" & elid.ToString)
                        Exit Sub
                    End If
                End If
            End If
        End If

        Dim lid As UInt64
        'Record the login - passing the ID forward as a request parameter

        Dim tid = iq.recordLogin(u, (MatchingAccounts.Count = 0), un$, Context.Request.UserAgent)
        lid = simpleHash(CStr(tid))
        iq.updateLogin(tid, lid)

        If (u IsNot Nothing) Then
            Dim aClsRole As Dictionary(Of String, clsRole)
            aClsRole = iq.i_role_Code

            'Dim aNewClsRole As clsRole = aClsRole.Values(0)

            'Dim aRole As String = aNewClsRole.Translation.text(English)

            iq.sesh(lid, "screenName") = u.RealName ' + " - " + aRole

        End If

        'Create the dictionary that holds all the (important) information about branch state for this user session
        Dim branchStates As Dictionary(Of String, clsBranchState) = New Dictionary(Of String, clsBranchState)
        iq.sesh(lid, "branchStates") = branchStates
        'NB the root branch itself is rendered as a breadcrumb

        iq.sesh(lid, "QuoteView") = "Breakdown"  'may want to set this to some user defined preference

        If MatchingAccounts.Count > 0 Then

            iq.sesh(lid, "UserID") = u.ID 'Only used until the account is chosen
            iq.sesh(lid, "passwordHash") = pwa$ 'MatchingAccounts.First.Password
            iq.sesh(lid, "passwordMD5") = pw$ 'MatchingAccounts.First.Password
            iq.sesh(lid, "AccountList") = MatchingAccounts

            'iq.sesh(lid, "BuyerAccount") = iq.Accounts(u..ID) 'TODO ML, this looks wrong but not sure how it should be so wont change it now, surely one of the accounts buyer or agent should be set on login, then the other chosen on the accounts screen??

            'Response.Write("<script>document.location='accounts.aspx?lid=" & lid & "';</script>")

            ' Create a case-insensitive dictionary for the Request parameters; could be used more widely
            Dim requestParams As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.InvariantCultureIgnoreCase)
            For Each key As String In Request.QueryString
                If Not key Is Nothing Then requestParams.Add(key, Request.QueryString(key))
            Next

            ' Check for the MFR on the query string (also check for MFG, which is mentioned in the documentation)
            Dim m As String = Nothing
            If requestParams.ContainsKey("mfr") Then
                m = requestParams("mfr")
            ElseIf requestParams.ContainsKey("mfg") Then
                m = requestParams("mfg")
            End If

            ' If no MFR specified, we might be able to infer one from the referrer for a Universal sign-in
            If m Is Nothing Then
                m = InferUniversalManufacturer(Request)
            End If

            Dim mfr As Manufacturer = Manufacturer.Unknown
            If Not m Is Nothing Then
                If String.Equals(m, "HPE", StringComparison.InvariantCultureIgnoreCase) Then
                    mfr = Manufacturer.HPE
                ElseIf String.Equals(m, "HPI", StringComparison.InvariantCultureIgnoreCase) Then
                    mfr = Manufacturer.HPI
                End If
            End If
            If mfr <> Manufacturer.Unknown Then
                iq.sesh(lid, "MFR") = mfr   ' May be overridden by Accounts.aspx if a deep link SKU is specified
            End If

            ' If a deep link has been requested, store the SKU; Tree.aspx will then register a client-side script to plough to the product and add it to the basket
            If requestParams.ContainsKey("base") Then
                Dim sku As String = Request("base")
                iq.sesh(lid, "Base") = sku
            End If

            ' Store any HOST specified
            If requestParams.ContainsKey("host") Then
                iq.sesh(lid, "Host") = Request("host")
            End If

            Response.Redirect("accounts.aspx?lid=" & lid, False)

        Else

            LblFailed.Text = "Failed - Please check email and password - passwords are CaSe SenSitivE"
            LblFailed.Visible = True
        End If


    End Sub

    Protected Sub BtnForgot_Click(sender As Object, e As EventArgs) Handles BtnForgot.Click

        Dim u As clsUser

        If Trim$(txtEmail.Text) = "" Then
            If txtEmail.Text = "" Then
                LblFailed.Text = UiTrans("Please enter your email address above first !")
                LblFailed.Visible = True
            End If
        Else
            If iq.i_user_email.ContainsKey(LCase(Trim$(txtEmail.Text))) Then
                u = iq.i_user_email(LCase(Trim$(txtEmail.Text)))

                If u.Accounts.Count = 0 Then
                    LblFailed.Text = UiTrans("No account(s) for that Email address")
                Else
                    If u.Disabled Then
                        LblFailed.Text = UiTrans("Your user is currently disabled - please contact your administrator")
                        LblFailed.Visible = True
                    Else
                        Response.Redirect("PasswordReset.aspx?uid=" & u.ID.ToString.Trim)
                    End If
                End If
            End If

            LblFailed.Visible = True

        End If
    End Sub

    Protected Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click

        If listCountries.SelectedIndex >= 0 Then

            If UniversalIQ1 Then

                ' Universal IQ1 mode
                If iq.Addresses.ContainsKey("IQ1Host") Then

                    ' For IQ1 we need the Universal host, not the region
                    Dim region As String = Split(listCountries.SelectedValue, "|")(1)
                    Dim channel As clsChannel = iq.Channels.Values.FirstOrDefault(Function(ch) (ch.Code.StartsWith("MHP") AndAlso ch.Code.EndsWith("U") AndAlso ch.Universal = True AndAlso ch.Region.Code = region))

                    If Not channel Is Nothing Then
                        Response.Redirect(String.Format("http://{0}/signup.asp?mfr={1}&host={2}", iq.Addresses("IQ1Host").Translation.text(English), hiddenMfrCode.Value, channel.Code))
                    End If

                End If

            Else

                ' Universal IQ2 mode
                Response.Redirect(String.Format("HPSignup.Aspx?mfr={0}&lang={1}", hiddenMfrCode.Value, listCountries.SelectedValue))

            End If

        End If

    End Sub

    Protected Sub btnSignInUniversal_Click(sender As Object, e As EventArgs) Handles btnSignInUniversal.Click

        If listCountries.SelectedIndex >= 0 Then

            ' This button is only shown in Universal IQ1 mode, but check anyway...
            If UniversalIQ1 Then

                If iq.Addresses.ContainsKey("IQ1Host") Then

                    Dim region As String = Split(listCountries.SelectedValue, "|")(1)
                    Dim channel As clsChannel = iq.Channels.Values.FirstOrDefault(Function(ch) (ch.Code.StartsWith("MHP") AndAlso ch.Code.EndsWith("U") AndAlso ch.Universal = True AndAlso ch.Region.Code = region))

                    If Not channel Is Nothing Then
                        Response.Redirect(String.Format("http://{0}/loginsplit.asp?mfr={1}&host={2}", iq.Addresses("IQ1Host").Translation.text(English), hiddenMfrCode.Value, channel.Code))
                    End If

                End If

            End If

        End If

    End Sub

    Private ReadOnly Property UniversalIQ1() As Boolean

        Get

            Dim iq1 As Boolean = False

            If Not ConfigurationManager.AppSettings("UniversalIQ1") Is Nothing Then
                If String.Equals(ConfigurationManager.AppSettings("UniversalIQ1"), "y", StringComparison.InvariantCultureIgnoreCase) Then
                    iq1 = True
                End If
            End If

            Return iq1

        End Get

    End Property

    Protected Sub btnRequest_Click(sender As Object, e As EventArgs) Handles btnRequest.Click

        Dim emailAddress As String = txtEmailAddress.Text
        Dim country As String = dropDownOtherCountries.SelectedItem.Text
        Dim language As String = dropDownOtherCountries.SelectedItem.Value

        requestNoEmail.Visible = False
        requestFeedback.Visible = False

        If String.IsNullOrEmpty(emailAddress) Then
            requestNoEmail.Visible = True
            Exit Sub
        End If

        Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
        tags.Add("emailAddress", emailAddress)
        tags.Add("country", country)
        tags.Add("mfr", hiddenMfrCode.Value)

        Dim errorMessages As List(Of String) = New List(Of String)

        ' Send an email to support informing them of the request
        If Not String.IsNullOrEmpty(emailAddress) AndAlso Not String.IsNullOrEmpty(country) Then
            SendEmail("support@channelcentral.net", "UniversalRequest.htm", tags, English, errorMessages, False)
        End If

        ' Also send an email to the user for their records
        If language.Contains("-") Then
            language = Split(language, "-")(0)
        End If

        SendEmail(emailAddress, "UniversalRequestUserCopy.htm", tags, iq.i_language_Code(language), errorMessages, False)

        requestFeedback.Visible = True

    End Sub

    Private Sub btnRegister_Command(sender As Object, e As CommandEventArgs) Handles btnRegister.Command

    End Sub
End Class