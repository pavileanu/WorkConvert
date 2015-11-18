Imports System.Globalization

Public Class HpSignup
    Inherits System.Web.UI.Page

    Private universalChannel As clsChannel
    Private selectedlang As String
    Private selectedCountry As String

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim errormessages As List(Of String) = New List(Of String)

        If Request.QueryString("lang") IsNot Nothing AndAlso Request.QueryString("mfr") IsNot Nothing Then

            Dim langarray() As String = Split(Request.QueryString("lang"), "|")
            selectedlang = langarray(0)
            selectedCountry = langarray(1)

            Dim language = English
            If iq.i_language_Code.ContainsKey(selectedCountry) Then language = iq.i_language_Code(selectedCountry)

            header.InnerHtml = Xlt(String.Format("Register for iQuote Universal ({0})", selectedCountry), language)

            Dim regsEnum As IEnumerable(Of clsChannel) = From j In iq.Channels.Values Where j.Code.StartsWith("MHP") And j.Code.EndsWith("U") And j.Universal = True And UCase(j.Region.Code) = UCase(selectedCountry) And UCase(j.Region.Culture.Code) = UCase(selectedlang)

            If regsEnum.Count > 0 Then

                litMsg.Text = "<span class=""HPSignupInfo"" > " & regsEnum(0).Code & "</span>"
                universalChannel = regsEnum(0)

                LabelFullName.Text = Xlt("Full Name", language)
                LabelEmailName.Text = Xlt("Email Address", language)
                LabelConfirmEmail.Text = Xlt("Confirm Email Address", language)
                LabelCompanyName.Text = Xlt("Company Name", language)

                LabelUserType.Text = Xlt("User Type", language)
                If ddlUserType.Items.Count = 0 Then
                    ddlUserType.Items.Add(New ListItem(Xlt("Please select", language), String.Empty))
                    ddlUserType.Items.Add(New ListItem(Xlt("Distributor", language), "USERTYPE_DISTRIBUTOR"))
                    ddlUserType.Items.Add(New ListItem(Xlt("Reseller", language), "USERTYPE_RESELLER"))
                    ddlUserType.Items.Add(New ListItem(Xlt("End User", language), "USERTYPE_ENDUSER"))

                    If String.Equals(Request.QueryString("mfr"), "hpe", StringComparison.InvariantCultureIgnoreCase) Then
                        ddlUserType.Items.Add(New ListItem(Xlt("HPE Employee", language), "USERTYPE_HPEMPLOYEE"))
                    Else
                        ddlUserType.Items.Add(New ListItem(Xlt("HP Employee", language), "USERTYPE_HPEMPLOYEE"))
                    End If

                    ddlUserType.Items.Add(New ListItem(Xlt("Other", language), "USERTYPE_OTHER"))
                End If

                LabelPostCode.Text = Xlt("Post Code", language)
                LabelTelephone.Text = Xlt("Telephone", language)
                LabelREquiredField.Text = Xlt("Required field", language)
                HeaderTandC.Text = Xlt("Terms and Conditions", language)

                BtnSave.Visible = True
                BtnSave.Text = Xlt("Register", language)

                BtnCancel.Visible = True
                BtnCancel.Text = Xlt("Cancel", language)

                If iq.Legal.ContainsKey("HPUniversalT&C") Then litLegal.Text = iq.Legal("HPUniversalT&C").Translation.text(language).Replace("[mfr]", GetMfrDisplay(Request.QueryString("mfr")))
                chkAgree.Text = Xlt("I agree.", language)

            Else
                litMsg.Text = "<span class=""HPSignupError"" > " & Xlt("Could not load HP Channel", language) & "</span>"
                BtnSave.Visible = False
                litLegal.Visible = False
                chkAgree.Visible = False
            End If

        Else

            Response.Redirect("SignIn.aspx")
        End If

    End Sub

    Private Function GetMfrDisplay(mfrCode As String) As String

        GetMfrDisplay = String.Empty

        If String.Equals(mfrCode, "hpe", StringComparison.InvariantCultureIgnoreCase) Then
            GetMfrDisplay = "HPE"
        ElseIf String.Equals(mfrCode, "hpi", StringComparison.InvariantCultureIgnoreCase) Then
            GetMfrDisplay = "HPI"
        End If

    End Function


    Protected Sub BtnSave_Click(sender As Object, e As EventArgs) Handles BtnSave.Click

        Dim fullName As String = txtFullName.Text
        Dim email As String = txtEmailName.Text
        Dim emailConfirm As String = txtConfirmEmail.Text
        Dim companyName As String = txtCompanyName.Text
        Dim postCode As String = txtPostCode.Text
        Dim telephoneNumber As String = txtTelephone.Text
        Dim agree As Boolean = chkAgree.Checked
        Dim user As clsUser
        Dim password = String.Empty
        Dim inputOK As Boolean = True

        litRegistered.Text = String.Empty
        litError.Text = String.Empty

        Dim language As clsLanguage = English
        If Not universalChannel Is Nothing AndAlso Not universalChannel.Region Is Nothing AndAlso Not universalChannel.Region.DefaultLanguage Is Nothing Then
            language = universalChannel.Region.DefaultLanguage
        End If

        If String.IsNullOrWhiteSpace(fullName) Then
            litError.Text += Xlt("Full Name is missing,", language) & "<br/>"
            inputOK = False
        End If
        If String.IsNullOrWhiteSpace(email) Then
            litError.Text += Xlt("Email Address is missing", language) & "<br/>"
            inputOK = False
        End If
        If String.Compare(email, emailConfirm, True) <> 0 Then
            litError.Text += Xlt("Email Addresses don't match", language) & "<br/>"
            inputOK = False
        End If
        If String.IsNullOrWhiteSpace(companyName) Then
            litError.Text += Xlt("Company Name is missing", language) & "<br/>"
            inputOK = False
        End If
        If ddlUserType.SelectedIndex = 0 Then
            litError.Text += Xlt("User Type not selected", language) & "<br/>"
            inputOK = False
        End If
        If String.IsNullOrWhiteSpace(postCode) Then
            litError.Text += Xlt("Post Code is missing", language) & "<br/>"
            inputOK = False
        End If
        If agree = False Then
            litError.Text += Xlt("Terms and conditions not accepted", language) & "<br/>"
            inputOK = False
        End If

        Dim mfrCode As String = Request("mfr")
        If mfrCode = "" Then litError.Text &= Xlt("No manufacturer (request parameter mfr) supplied", language) : inputOK = False

        If inputOK Then

            Dim errorMessages As List(Of String) = New List(Of String)

            ' Pick up language
            If selectedlang.Contains("-") Then
                selectedlang = Split(selectedlang, "-")(0)
            End If
            Dim accountLanguage As clsLanguage = iq.i_language_Code(selectedlang)

            ' Pick up currency
            Dim regionInfo As RegionInfo = New RegionInfo(selectedCountry)
            Dim currency As clsCurrency = New clsCurrency()
            If regionInfo IsNot Nothing AndAlso iq.i_currency_code.ContainsKey(regionInfo.ISOCurrencySymbol) Then
                currency = iq.i_currency_code(regionInfo.ISOCurrencySymbol)
            End If

            ' Pick up user type
            Dim userType = ddlUserType.SelectedValue

            ' Pick up region
            Dim region As clsRegion = iq.i_region_code(selectedCountry)

            ' Give the universal channel a team if it doesn't have one
            If universalChannel.Teams.Count = 0 Then
                Dim team As clsTeam = New clsTeam(universalChannel, "EveryOne")
            End If

            ' Create or reference a user
            If iq.i_user_email.ContainsKey(email) Then
                user = iq.i_user_email(email)
            Else
                user = New clsUser(universalChannel, email, fullName, New nullableString(telephoneNumber), New nullableString())
            End If

            ' Create or reference a buyer channel
            Dim buyerID As String = UCase("R" & Left(companyName, 2) & postCode.Replace(" ", ""))
            Dim buyerChannel As clsChannel = (From c In iq.Channels.Values Where c.Code.Equals(buyerID)).FirstOrDefault
            If buyerChannel Is Nothing Then
                buyerChannel = New clsChannel(universalChannel, companyName, companyName, "", buyerID, universalChannel.Region, New nullableString(), New nullableString(), New nullableString(Left(email, InStr(email, "@") - 1)), 15, "tree.1", "", 0, 0, "R", "", "", universalChannel.DefaultCurrency, False, "", "", "")
            End If

            ' Create or reference an account for this user/buyer channel/universal channel
            Dim account As clsAccount = (From a In iq.Accounts.Values Where a.User.Equals(user) AndAlso a.BuyerChannel.ID = buyerChannel.ID AndAlso a.SellerChannel.ID = universalChannel.ID).FirstOrDefault
            Dim accountCreated As Boolean = False
            If account Is Nothing Then
                password = GeneratePassword()
                account = New clsAccount(user, simpleHash(password), buyerChannel, {iq.i_role_Code("user")}, universalChannel.Teams.First.Value, accountLanguage, currency, universalChannel, iq.getPriceBand(""), region.Culture, mfrCode)
                accountCreated = True
            End If

            ' Ensure the account has the selected user type role
            If iq.i_role_Code.ContainsKey(userType) AndAlso Not account.i_roles_code.ContainsKey(userType) Then
                account.i_roles_code.Add(userType, iq.i_role_Code(userType))
            End If

            If accountCreated Then

                ' New account created - inform the user
                Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
                Dim baseurl As String = ConfigurationManager.AppSettings("BaseURL")

                Dim url As String
                url = baseurl & "/Aspx/SignIn.aspx?mfr=" & Request("mfr")

                tags.Add("hostname", universalChannel.DisplayName(accountLanguage))
                tags.Add("email", email)
                tags.Add("password", password)
                tags.Add("firstname", fullName)
                tags.Add("url", url)
                tags.Add("extratext", String.Empty)
                tags.Add("mfr", Request("mfr"))
                tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English))

                Dim em As List(Of String) = New List(Of String)  'Returns any error messages encountered whilst emailing
                SendEmail(email, "WelcomeEmail.htm", tags, account.Language, em, False)

                Response.Redirect(String.Format("HPSignedup.aspx?mfr={0}&lang={1}", Request("mfr"), Request("lang")))
            Else
                ' User already has a relevant account - tell them so they can go sign in
                Response.Redirect(String.Format("HPSignedup.aspx?mfr={0}&lang={1}&existingAccount=Y", Request("mfr"), Request("lang")))
            End If

        End If

    End Sub

    Protected Sub BtnCancel_Click(sender As Object, e As EventArgs) Handles BtnCancel.Click

        Response.Redirect(String.Format("SignIn.aspx?Universal&mfr={0}", Request("mfr")))

    End Sub

End Class