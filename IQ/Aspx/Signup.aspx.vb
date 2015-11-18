Imports System.Security.Cryptography

Public Class Signup
    Inherits clsPageLogging

    Private Sub Signup_Init(sender As Object, e As System.EventArgs) Handles Me.Init

        'If Not IsPostBack Then
        ' FillDDL(DDLSeller, iq.Channels.Values)
        FillDDL(ddlLanguage, iq.Languages.Values)
        FillDDL(ddlCurrency, iq.Currencies.Values)
        FillDDL(ddlRole, iq.i_role_Code.Values)
        'End If

    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'If DDLSeller.SelectedValue <> "" Then
        '    Dim teams = From v In iq.Teams.Values Where v.Channel.ID = DDLSeller.SelectedValue
        '    FillDDL(ddlTeam, teams)
        'End If

    End Sub

    Protected Sub BtnSignUp_Click(sender As Object, e As EventArgs) Handles BtnSignUp.Click

        Dim BuyersChannel As clsChannel
        If Not iq.Channels.ContainsKey(CInt(txtBuyerID.Text)) Then lblError.Text = "Unrecognised buyer channel" : Exit Sub
        BuyersChannel = iq.Channels(CInt(txtBuyerID.Text))

        Dim buyerUser As clsUser = Nothing
        Dim aatuid As Integer 'adding account to user ID
        aatuid = CInt(TxtAccountID.Text)
        If aatuid = 0 Then
            buyerUser = New clsUser(BuyersChannel, TxtEmail.Text, TxtRealName.Text, New nullableString(TxtTel1.Text), New nullableString(txtTel2.Text))
            If buyerUser Is Nothing Then lblError.Text = "Failed to create user (check field lengths are resonable)." : Exit Sub
        Else
            If Not iq.Users.ContainsKey(aatuid) Then lblError.Text = "Unrecognized user" : Exit Sub
            buyerUser = iq.Users(aatuid)
        End If

        Dim SellerChannel As clsChannel = Nothing
        If Not iq.Channels.ContainsKey(CInt(txtSellerID.Text)) Then lblError.Text = "Unrecognised seller channel" : Exit Sub
        SellerChannel = iq.Channels(CInt(txtSellerID.Text))

        Dim team As clsTeam = Nothing
        If txtTeamID.Text <> "" Then
            If Not iq.Teams.ContainsKey(CInt(txtTeamID.Text)) Then lblError.Text = "Unrecognised Team" : Exit Sub
            team = iq.Teams(CInt(txtTeamID.Text))
        End If

        Dim role As clsRole
        Dim language As clsLanguage
        Dim currency As clsCurrency

        role = iq.i_role_Code(ddlRole.SelectedValue)
        language = iq.Languages(ddlLanguage.SelectedValue)
        currency = iq.Currencies(ddlCurrency.SelectedValue)

        Dim priceBand As clsPriceBand = iq.getPriceBand(TxtpriceBand.Text)

        'create the new account
        Dim Account As clsAccount

        Dim pw$ = ""
        If TxtPassword.Text = "" Then
            pw$ = GeneratePassword()
        Else
            pw$ = Trim$(TxtPassword.Text)
        End If

        ' Stop
        ' Dim buyergroup As New clsBuyerGroup("New Buyer", sellerschannel, accountID)

        If Request("mfr") = "" Then lblError.Text = "No manufacturer request parameter supplied (&mfr=xxx)" : Exit Sub

        Dim mfrcode As String = Request("mfr")
        Account = New clsAccount(buyerUser, simpleHash(pw$), BuyersChannel, {role}, team, language, currency, SellerChannel, priceBand, SellerChannel.Region.Culture, mfrcode)
        Account.MustChangePassword = True

        Dim em As List(Of String) = New List(Of String)

        Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
        tags.Add("password", pw)
        Dim baseurl As String = ConfigurationManager.AppSettings("BaseURL")

        tags.Add("url", baseurl & "/aspx/signin.aspx")
        tags.Add("email", Account.User.Email)
        tags.Add("hostname", Account.SellerChannel.DisplayName(Account.Language))
        tags.Add("mfr", Account.mfrCode)
        tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English))

        SendEmail(Account.User.Email, "WelcomeEmail.htm", tags, Account.Language, em, False)

        If em.Count Then
            Dim lbl As New Label
            lbl.BackColor = Drawing.Color.Red
            lbl.ForeColor = Drawing.Color.White
            lbl.Text = String.Format("Sorry - we are presently unable to send your Welcome email - Please contact {0} quoting reference AC {1}", iq.Addresses("iQuoteSupportEmail").Translation.text(English), Account.ID)
            Form.Controls.Add(lbl)
        End If

    End Sub

    Protected Sub BtnFindUser_Click(sender As Object, e As EventArgs) Handles BtnFindUser.Click

        Dim em$
        em = Trim(TxtEmail.Text)
        Dim u As clsUser

        Dim Match As Match = Regex.Match(em$, "^[a-zA-Z][\w\.-]*[a-zA-Z0-9]@[a-zA-Z0-9][\w\.-]*[a-zA-Z0-9]\.[a-zA-Z][a-zA-Z\.]*[a-zA-Z]$")
        If Match.Success Then

            If iq.i_user_email.ContainsKey(Trim$(TxtEmail.Text)) Then
                u = iq.i_user_email(Trim$(TxtEmail.Text))
                TxtTel1.Text = u.tel1.DisplayValue
                txtTel2.Text = u.tel2.DisplayValue
                TxtRealName.Text = u.RealName
                'Session("AddingAccountTo") = u.ID
                TxtAccountID.Text = u.ID

            Else
                'u = New clsUser(TxtEmail.Text, "", "", "", iq.Channels(DDLBuyer.SelectedValue))
            End If
        Else
            LblInvalidEmail.Visible = True
        End If

    End Sub

End Class