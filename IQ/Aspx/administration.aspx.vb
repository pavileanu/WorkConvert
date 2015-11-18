Imports dataAccess

Public Class administration
    Inherits System.Web.UI.Page
    Private agentaccount As clsAccount
    Private buyeraccount As clsAccount
    Private dtUsers As DataTable
    Private iFrameStyle, iFrameSrc As String
    Public AccountCanDisableUsers As Boolean = False
    Public AccountCanSetupUsers As Boolean = False
    Public AccountCanResetPasswords As Boolean = False
    Public AccountIsDistiAdmin As Boolean = False
    Public IsGlobalAdmin As Boolean = False
    'Public AvailableRoles As List(Of clsRole)
    Private NewCreatedUsers As List(Of clsUser)

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim lid As UInt64 = Request.QueryString("lid")
        If Not iq.SeshAlive(lid) OrElse Not (AccountHasRight(lid, "ADMINMENU") Or AccountHasRight(lid, "GLOBALADM")) Then
            Response.Redirect("signin.aspx")
        End If

        ' Handle any manually-configured postbacks
        Dim eventTarget = Convert.ToString(Request.Params.Get("__EVENTTARGET"))
        Dim eventArgument = Convert.ToString(Request.Params.Get("__EVENTARGUMENT"))
        If String.Equals(eventTarget, "ToggleRole", StringComparison.CurrentCultureIgnoreCase) Then
            ToggleRole(Convert.ToInt32(eventArgument))
        End If

        'AvailableRoles = iq.i_role_Code.Values.ToList


        'If Not Page.IsPostBack Then
        dtUsers = New DataTable
        Dim dtActivity As New DataTable
        dtUsers.Columns.Add("Email", GetType(String))
        dtUsers.Columns.Add("ID", GetType(Integer))
        dtUsers.Columns.Add("AccountID", GetType(Integer))
        dtUsers.Columns.Add("RealName", GetType(String))
        dtUsers.Columns.Add("ChannelName", GetType(String))
        dtUsers.Columns.Add("Disabled", GetType(Boolean))
        'dtUsers.Columns.Add("Quotes", GetType(Integer))
        dtUsers.Columns.Add("DistiAdmin", GetType(Boolean))
        dtUsers.Columns.Add("LastUsed", GetType(String))
        dtUsers.Columns.Add("Roles", GetType(clsRole()))
        dtUsers.Columns.Add("AvailableRoles", GetType(clsRole()))
        dtUsers.Columns.Add("HighestRole", GetType(String))
        dtUsers.Columns.Add("RoleFunction", GetType(String))

        'End If
        agentaccount = iq.sesh(lid, "AgentAccount")
        buyeraccount = iq.sesh(lid, "BuyerAccount")

        If agentaccount Is Nothing Then Response.Redirect("signin.aspx")
        'Dim dtUsers As New  DataTable

        Dim channelCentralUser As Boolean = agentaccount.User.Email.ToLower().EndsWith("@channelcentral.net")

        Dim quoteSummary(4, 4) As Integer

        If agentaccount.HasRight("DISABLEUSR") Or agentaccount.HasRight("FULLDIST") Or agentaccount.HasRight("GLOBALADM") Then AccountCanDisableUsers = True
        If agentaccount.HasRight("CREATEUSR") Or agentaccount.HasRight("FULLDIST") Or agentaccount.HasRight("GLOBALADM") Then AccountCanSetupUsers = True
        If agentaccount.HasRight("PWDRESET") Or agentaccount.HasRight("FULLDIST") Or agentaccount.HasRight("GLOBALADM") Then AccountCanResetPasswords = True
        If agentaccount.HasRight("FULLDIST") Or agentaccount.HasRight("GLOBALADM") Then AccountIsDistiAdmin = True

        'If agentaccount.HasRight("GLOBALADM") Then
        '    PnlMultiSend.Visible = True
        'End If

        If Not IsPostBack Then

            ' Email domain list
            drpDomain.DataSource = agentaccount.SellerChannel.Domains
            drpDomain.DataBind()

            If channelCentralUser Then
                If agentaccount.HasRight("SYSMESSAGE") Then
                    panelSignInMessage.Visible = True
                    panelHpeSystemMessage.Visible = True
                    panelHpiSystemMessage.Visible = True

                    Dim messageExists As Boolean = False

                    ' System sign-in message
                    If Not iq.UserMessages Is Nothing Then
                        If iq.UserMessages.ContainsKey("SignInScreenMessage") Then
                            If iq.UserMessages("SignInScreenMessage").Count > 0 Then
                                messageExists = True
                            End If
                        End If
                    End If
                    If messageExists Then
                        With iq.UserMessages("SignInScreenMessage")(0)
                            Dim kyLanguage As clsLanguage = (From l In iq.Languages.Values Where l.Code = "KY").First
                            SystemMessage = .Translation.text(kyLanguage).Replace("<br />", Environment.NewLine)
                            txtSystemMessageValidFrom.Text = .ValidFrom.ToString("dd-MMM-yyyy")
                            txtSystemMessageValidTo.Text = .ValidTo.ToString("dd-MMM-yyyy")
                            chkSystemMessage.Checked = .Enabled
                        End With

                        btnAmendSystemMessage.Visible = True
                        'btnDeleteSystemMessage.Visible = True
                    Else
                        btnAddSystemMessage.Visible = True
                    End If

                    ' HPE System message
                    messageExists = False
                    If Not iq.UserMessages Is Nothing Then
                        If iq.UserMessages.ContainsKey("HPESystemMessage") Then
                            If iq.UserMessages("HPESystemMessage").Count > 0 Then
                                messageExists = True
                            End If
                        End If
                    End If
                    If messageExists Then
                        With iq.UserMessages("HPESystemMessage")(0)
                            HPESystemMessage = .Translation.text(agentaccount.Language).Replace("<br />", Environment.NewLine)
                            txtHpeSystemMessageValidFrom.Text = .ValidFrom.ToString("dd-MMM-yyyy")
                            txtHpeSystemMessageValidTo.Text = .ValidTo.ToString("dd-MMM-yyyy")
                            hpeMessageEnabled.Checked = .Enabled
                        End With

                        btnAmendHpeSystemMessage.Visible = True
                        'btnDeleteHpeSystemMessage.Visible = True
                    Else
                        btnAddHpeSystemMessage.Visible = True
                    End If

                    ' HPI System message
                    messageExists = False
                    If Not iq.UserMessages Is Nothing Then
                        If iq.UserMessages.ContainsKey("HPISystemMessage") Then
                            If iq.UserMessages("HPISystemMessage").Count > 0 Then
                                messageExists = True
                            End If
                        End If
                    End If
                    If messageExists Then
                        With iq.UserMessages("HPISystemMessage")(0)
                            HPISystemMessage = .Translation.text(agentaccount.Language).Replace("<br />", Environment.NewLine)
                            txtHpiSystemMessageValidFrom.Text = .ValidFrom.ToString("dd-MMM-yyyy")
                            txtHpiSystemMessageValidTo.Text = .ValidTo.ToString("dd-MMM-yyyy")
                            hpiMessageEnabled.Checked = .Enabled
                        End With

                        btnAmendHpiSystemMessage.Visible = True
                        'btnDeleteHpiSystemMessage.Visible = True
                    Else
                        btnAddHpiSystemMessage.Visible = True
                    End If
                Else
                    RemoveMenuItem(adminMenu.Items, "System")
                End If
            Else

                ' Non-Channel Central user; hide the System tab
                RemoveMenuItem(adminMenu.Items, "System")

            End If

        End If


        '        txtMultiHost.Text = agentaccount.SellerChannel.Code

        Dim errormessages As List(Of String) = New List(Of String)

        Dim list = From j In iq.Accounts.Values Where (j.SellerChannel Is agentaccount.BuyerChannel Or j.BuyerChannel Is agentaccount.BuyerChannel) AndAlso j.User.Email.ToLower.Contains(txtFilter.Text.ToLower) AndAlso (Not chkonlyDistiAdmin.Checked OrElse j.HasRight("FULLDIST"))

        Dim cnt As Integer = list.Count

        For Each account In list
            'dtUsers.Rows.Add(account.User.Email, account.User.ID, account.User.RealName, account.BuyerChannel.Name, account.User.Disabled, account.Quotes.Values.Count, account.HasRight("FULLDIST"), DateString, account.Roles)
            dtUsers.Rows.Add(account.User.Email, account.User.ID, account.ID, account.User.RealName, account.BuyerChannel.Name, account.User.Disabled, account.HasRight("FULLDIST"), DateString, account.Roles, GetAvailableRoles(account.Roles), GetHighestRole(account.Roles), GetRoleFunction(account))
        Next

        iq.sesh(lid, "UserDetailsTable") = dtUsers

        agentaccount.SellerChannel.fixteams(errormessages)


        drpCurrency.DataSource = iq.Currencies.Values
        drpCurrency.DataTextField = "Code"
        drpCurrency.DataValueField = "ID"
        drpCurrency.DataBind()
        drpCurrency.Items.FindByValue(agentaccount.Currency.ID).Selected = True


        If Not IsPostBack Then
            ddlChannels.DataSource = agentaccount.User.Accounts.Where(Function(ac) (ac.Value.Password = agentaccount.Password) AndAlso (channelCentralUser OrElse (agentaccount.Manufacturer = ac.Value.Manufacturer))).Select(Function(ac) ac.Value.BuyerChannel).Distinct
            ddlChannels.DataBind()
            ddlChannels.Items.FindByValue(agentaccount.BuyerChannel.ID.ToString()).Selected = True

            If ddlChannels.Items.Count <= 1 Then
                ddlChannels.Visible = False
                lblChannelSelect.Visible = False
            End If


            drpTeams.DataSource = iq.Channels(ddlChannels.SelectedValue).Teams.Values
            drpTeams.DataTextField = "Name"
            drpTeams.DataValueField = "ID"
            drpTeams.DataBind()


            lbRoles.DataSource = If(agentaccount.HasRight("GLOBALADM"), iq.Roles.Values.Where(Function(ro) {"USER", "DISTADMIN", "SUPPORT", "EDITOR", "ADMIN"}.Contains(ro.Code)), iq.Roles.Values.Where(Function(ro) {"USER", "DISTADMIN"}.Contains(ro.Code)))
            lbRoles.DataTextField = "EnglishDisplayName"
            lbRoles.DataValueField = "ID"
            lbRoles.DataBind()

            If Not agentaccount.HasRight("GLOBALADM") Then

                lbRoles.SelectionMode = ListSelectionMode.Single
                lblRoles.Text = "Role"

                drpCurrency.Visible = False
                lblCurrency.Visible = False

            End If

            If Request("page") IsNot Nothing Then
                Dim page As Integer = 0
                If Integer.TryParse(Request("page"), page) Then
                    grdUser.PageIndex = page
                End If
            End If

            'Moved here by nick into NOT isPostback  - as databinding the grid (again) on postback destroys event handlers
            'You only need databind the grid 'once'

            grdUser.DataSource = dtUsers
            grdUser.DataBind()

        End If

        'grdUser.DataSource = dtUsers
        'grdUser.DataBind()


        OutputErrors(Form.Controls, errormessages, lid, True)
    End Sub

    Private Function GetAvailableRoles(currentRoles As clsRole()) As clsRole()

        Dim availRoles = If(agentaccount.HasRight("GLOBALADM"), iq.i_role_Code.Values.Where(Function(ro) {"USER", "DISTADMIN", "SUPPORT", "EDITOR", "ADMIN"}.Contains(ro.Code)), iq.i_role_Code.Values.Where(Function(ro) {"USER", "DISTADMIN"}.Contains(ro.Code))).ToList()

        'availRoles = availRoles.Except(Function(r) (roles.Contains(r)))
        availRoles.RemoveAll(Function(r) (currentRoles.Contains(r)))

        Return availRoles.ToArray()

    End Function


    ' Returns a string representation of the highest role in the passed list
    Private Function GetHighestRole(roles As clsRole()) As String

        Dim highestRole As String = String.Empty

        If roles Is Nothing Then Return highestRole

        Dim role = roles.FirstOrDefault(Function(r) r.Code = "USER")
        If Not role Is Nothing Then
            highestRole = role.Translation.textTranslation(English)
        End If

        role = roles.FirstOrDefault(Function(r) r.Code = "DISTADMIN")
        If Not role Is Nothing Then
            highestRole = role.Translation.textTranslation(English)
        End If

        role = roles.FirstOrDefault(Function(r) r.Code = "SUPPORT")
        If Not role Is Nothing Then
            highestRole = role.Translation.textTranslation(English)
        End If

        role = roles.FirstOrDefault(Function(r) r.Code = "EDITOR")
        If Not role Is Nothing Then
            highestRole = role.Translation.textTranslation(English)
        End If

        role = roles.FirstOrDefault(Function(r) r.Code = "ADMIN")
        If Not role Is Nothing Then
            highestRole = role.Translation.textTranslation(English)
        End If

        Return highestRole

    End Function

    Private Function GetRoleFunction(account As clsAccount) As String

        If agentaccount.HasRight("GLOBALADM") Then

            ' Full Role edit UI
            Return String.Format("showRoles(&quot;rolefloater{0}&quot;);return false;", account.User.ID)

        Else

            ' Simple User/Admin toggle
            Return String.Format("javascript:__doPostBack(&quot;ToggleRole&quot;, &quot;{0}&quot;)", account.ID)

        End If

    End Function

    ' Toggle the passed user between Basic User and Disti Administrator
    Public Sub ToggleRole(accountID As Integer)


        If iq.Accounts.ContainsKey(accountID) Then

            Dim account = iq.Accounts(accountID)

            Dim roles = New List(Of clsRole)(account.Roles)

            If roles.Contains(iq.i_role_Code("DISTADMIN")) Then
                account.RemoveRole(iq.i_role_Code("DISTADMIN"))
            Else
                account.AddRole(iq.i_role_Code("DISTADMIN"))
            End If

        End If

        Response.Redirect("administration.aspx" & Request.Url.Query)


    End Sub

    Private Sub RemoveMenuItem(items As MenuItemCollection, key As String)

        Dim itemToRemove As MenuItem = Nothing

        For Each menuItem As MenuItem In items
            If String.Equals(menuItem.Value, key, StringComparison.InvariantCultureIgnoreCase) Then
                itemToRemove = menuItem
                Exit For
            End If
        Next

        If Not itemToRemove Is Nothing Then
            items.Remove(itemToRemove)
            adminTabsLine.Style("width") = "681px"
        End If

    End Sub

    Public Sub enableUser(o As CheckBox, e As System.EventArgs)

        Dim cb As CheckBox = CType(o, CheckBox)
        Dim u As clsUser = iq.Users(cb.Attributes("uid"))
        u.Disabled = (cb.Checked = False)

        Dim errormessages As List(Of String) = New List(Of String)
        u.update(errormessages)


        Dim lid As ULong = Request.QueryString("lid")
        OutputErrors(Form.Controls, errormessages, lid, True)

    End Sub
    Protected Sub chkDisabled_CheckedChanged(sender As Object, e As EventArgs)

        Dim chkStatus As CheckBox = DirectCast(sender, CheckBox)
        Dim row As GridViewRow = DirectCast(chkStatus.NamingContainer, GridViewRow)
        Dim rowindex As Integer = row.RowIndex
        Dim userid As Integer = Convert.ToInt32(grdUser.DataKeys(rowindex).Value)
        Dim u As clsUser = iq.Users(userid)
        u.Disabled = chkStatus.Checked

        Dim errormessages As List(Of String) = New List(Of String)
        u.update(errormessages)
        Dim lid As ULong = Request.QueryString("lid")
        OutputErrors(Form.Controls, errormessages, lid, True)


    End Sub

    Protected Sub grdUser_PageIndexChanging(sender As Object, e As GridViewPageEventArgs)
        grdUser.PageIndex = e.NewPageIndex
        grdUser.DataSource = dtUsers
        grdUser.DataBind()
    End Sub

    Protected Sub BtnSave_Click(sender As Object, e As EventArgs) Handles BtnSave.Click

        Dim emailDomain As String = String.Empty
        Dim emailName As String = String.Empty
        Dim fullName As String = String.Empty
        Dim telephoneNumber As String = String.Empty
        Dim password As String = String.Empty

        emailName = txtEmailName.Text
        emailDomain = drpDomain.SelectedValue
        fullName = txtFullName.Text
        telephoneNumber = TxtTelephone.Text

        Dim lid = Request("lid")
        Dim agentAccount As clsAccount = iq.sesh(lid, "AgentAccount")

        Dim allGood As Boolean = False 'fail safe
        Dim errorMessages As List(Of String) = New List(Of String)

        If Not (String.IsNullOrWhiteSpace(emailName) Or String.IsNullOrWhiteSpace(fullName)) Then

            emailName = emailName & "@" & emailDomain
            'only create a new user if they dont' already exist (they may pre-exist and have accounts at another channel)
            Dim user As clsUser
            Dim lit As Literal = New Literal

            Dim OnChannel As clsChannel = iq.Channels(ddlChannels.SelectedValue) ' agentaccount.SellerChannel

            'If txtHostOverride.Text <> "" Then
            '    If iq.i_channel_code.ContainsKey(txtHostOverride.Text) Then
            '        OnChannel = iq.i_channel_code(txtHostOverride.Text)
            '    Else
            '        OnChannel = Nothing
            '    End If
            'End If

            If OnChannel Is Nothing Then
                errorMessages.Add("Host Override is invalid")
            Else
                OnChannel.fixteams(errorMessages)
                '
                If iq.i_user_email.ContainsKey(emailName) Then
                    user = iq.i_user_email(emailName)
                    'ok, well the user existed - do they already have an account
                    If (From cA In OnChannel.CustomerAccounts.Values Where cA.User Is user).Any Then
                        lit.Text = "<div><span class='errorLabel'>" & Xlt("An account for that user already exists", English) & "</span></div>"
                        Pnl.Controls.Add(lit)
                    Else
                        allGood = True
                    End If
                Else
                    ' Create a new User
                    user = New clsUser(OnChannel, emailName, fullName, New nullableString(telephoneNumber), New nullableString())
                    allGood = True
                End If

                If allGood Then

                    'Generate a hash password
                    Dim pw$ = GeneratePassword()
                    password = simpleHash(pw$)

                    If iq.sesh(lid, "NewUsers") Is Nothing Then
                        NewCreatedUsers = New List(Of clsUser)
                    Else
                        NewCreatedUsers = iq.sesh(lid, "NewUsers")
                    End If
                    NewCreatedUsers.Add(user)
                    iq.sesh(lid, "NewUsers") = NewCreatedUsers

                    'Check if user type is admin otherwise select role as user
                    'Dim userType As String = "user"
                    'If chkAdminUser.Checked Then userType = "admin"
                    Dim rolesSelected As List(Of clsRole) = New List(Of clsRole)()
                    For Each item In lbRoles.Items
                        If item.Selected Then
                            rolesSelected.Add(iq.Roles(item.value))
                        End If
                    Next
                    'Dim rolesSelected = lbRoles.GetSelectedIndices().Select(Function(si) iq.Roles(CInt(lbRoles.Items(si).Value))).ToArray ' From r In iq.i_role_Code.Values Where r.Code.ToUpper = userType.ToUpper Select r
                    '  Dim role As clsRole = rolesSelected(0)

                    ' Create  account
                    'NB Accoutns are created with the same currency as the user (agentAccount) setting them up !
                    'If you create an account in the wrong currency it will see no prices !
                    Dim currency As clsCurrency = iq.Currencies(drpCurrency.SelectedValue)
                    Dim account As clsAccount = New clsAccount(user, password, OnChannel, rolesSelected.ToArray, OnChannel.Teams.Values(0), agentAccount.Language, currency, agentAccount.SellerChannel, agentAccount.Priceband, agentAccount.Culture, agentAccount.mfrCode)
                    Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
                    Dim baseurl As String = ConfigurationManager.AppSettings("BaseURL")

                    Dim url As String
                    url = baseurl & "/aspx/signin.aspx"

                    tags.Add("hostname", OnChannel.DisplayName(agentAccount.Language))
                    tags.Add("email", emailName)
                    tags.Add("password", pw$)
                    tags.Add("firstname", fullName)
                    tags.Add("url", url)
                    tags.Add("extratext", If(baseurl = "http://uat.hpiquote.net", "<p>Please note this is a login for test purposes</p>", ""))
                    tags.Add("mfr", agentAccount.mfrCode)
                    tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English))

                    Dim em As List(Of String) = New List(Of String)  'Returns any error messages encountered whilst emailing
                    If chkEmailUser.Checked Then
                        SendEmail(emailName, "WelcomeEmail.htm", tags, agentAccount.Language, em, False)
                    End If
                    If chkEmailAdmin.Checked Then
                        SendEmail(agentAccount.User.Email, "WelcomeEmail.htm", tags, agentAccount.Language, em, False)
                    End If
                    drpDomain.SelectedIndex = 0
                    If drpTeams.Items.Count > 0 Then drpTeams.SelectedIndex = 0
                    txtEmailName.Text = ""
                    txtFullName.Text = ""
                    TxtTelephone.Text = ""

                    lit.Text = "<div><span class='messageLabel'>" & Xlt("The user has been successfully created.", English) & "</span></div>"
                    Pnl2.Controls.Add(lit)

                End If
            End If
        End If

        OutputErrors(Me.Form.Controls, errorMessages, Request("lid"))
        '        Response.Redirect(Request.RawUrl)  'err WHY ? - this is why we can't see errors

    End Sub

    Protected Sub btnPasswordReset_Click(sender As Object, e As EventArgs)
        Dim chkStatus As Button = DirectCast(sender, Button)
        Dim row As GridViewRow = DirectCast(chkStatus.NamingContainer, GridViewRow)
        Dim rowindex As Integer = row.RowIndex
        Dim userid As Integer = Convert.ToInt32(grdUser.DataKeys(rowindex).Value)
        Dim u As clsUser = iq.Users(userid)
        'Do reset here
        Dim a = u.Accounts.Where(Function(us) us.Value.BuyerChannel Is CType(iq.sesh(Request.QueryString("lid"), "BuyerAccount"), clsAccount).BuyerChannel).FirstOrDefault
        If a.Value IsNot Nothing Then
            a.Value.ResetPassword()
        End If
    End Sub

    Protected Sub btnAddRole_Click(sender As Object, e As EventArgs)
        Dim chkStatus As Button = DirectCast(sender, Button)
        Dim row As GridViewRow = DirectCast(chkStatus.NamingContainer, GridViewRow)
        Dim rowindex As Integer = row.RowIndex
        Dim userid As Integer = Convert.ToInt32(grdUser.DataKeys(rowindex).Value)
        Dim u As clsUser = iq.Users(userid)
        Dim a = u.Accounts.Where(Function(us) us.Value.BuyerChannel Is CType(iq.sesh(Request.QueryString("lid"), "BuyerAccount"), clsAccount).BuyerChannel).FirstOrDefault
        If a.Value IsNot Nothing Then
            a.Value.AddRole(iq.i_role_Code(sender.parent.parent.cells(9).controls(7).selecteditem.value))
            Response.Redirect("administration.aspx?lid=" & Request.QueryString("lid") & "&page=" & grdUser.PageIndex)
        End If
    End Sub

    Protected Sub btnRemoveRole_Click(sender As Object, e As EventArgs)
        Dim chkStatus As Button = DirectCast(sender, Button)
        Dim row As GridViewRow = DirectCast(chkStatus.NamingContainer, GridViewRow)
        Dim rowindex As Integer = row.RowIndex
        Dim userid As Integer = Convert.ToInt32(grdUser.DataKeys(rowindex).Value)
        Dim u As clsUser = iq.Users(userid)
        Dim a = u.Accounts.Where(Function(us) us.Value.BuyerChannel Is CType(iq.sesh(Request.QueryString("lid"), "BuyerAccount"), clsAccount).BuyerChannel).FirstOrDefault
        If a.Value IsNot Nothing Then
            a.Value.RemoveRole(iq.i_role_Code(sender.parent.parent.cells(9).controls(1).selecteditem.value))
            Response.Redirect("administration.aspx?lid=" & Request.QueryString("lid") & "&page=" & grdUser.PageIndex)
        End If
    End Sub

    Protected Sub BtnMultisend_Click(sender As Object, e As EventArgs) Handles BtnMultisend.Click

        'For each account - at the host - with the email address
        'reset the password and send the welcome mail

        Dim errorMessages As List(Of String) = New List(Of String)
        If Not iq.i_channel_code.ContainsKey(txtMultiHost.Text) Then
            errorMessages.Add(txtMultiHost.Text & " is not a valid channel Code")
        Else
            Dim host As clsChannel = iq.i_channel_code(txtMultiHost.Text)

            For Each Usr In host.Users.Values
                If Usr.Email.Contains("@") Then
                    If LCase(TxtMultisend.Text & ",").Contains(LCase(Split(Usr.Email, "@")(0))) Then
                        For Each account In Usr.Accounts.Values
                            If account.SellerChannel Is host Then

                                Dim ru As String = Request.RawUrl
                                Dim baseurl As String = Left(ru, InStr(ru, "admin.aspx") - 1)

                                Dim url As String
                                url = baseurl & "signin.aspx"

                                Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)

                                If chkMultiDoit.Checked Then
                                    Dim pw$ = GeneratePassword()
                                    Dim passwordHash As ULong = simpleHash(pw$)
                                    account.Password = passwordHash.ToString

                                    account.update(errorMessages)

                                    tags.Add("hostname", agentaccount.SellerChannel.DisplayName(agentaccount.Language))
                                    tags.Add("email", Usr.Email)
                                    tags.Add("password", pw$)
                                    tags.Add("firstname", Usr.RealName)
                                    tags.Add("url", url)
                                    tags.Add("mfr", agentaccount.mfrCode)
                                    tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English))

                                    SendEmail(Usr.Email, "WelcomeEmail.htm", tags, agentaccount.Language, errorMessages, True)
                                Else
                                    PnlMultiSend.Controls.Add(NewLit("Will do: " & Usr.Email & "<br/>"))
                                End If
                            End If
                        Next
                    End If
                End If
            Next

        End If


        OutputErrors(PnlMultiSend.Controls, errorMessages, Request("Lid"))

    End Sub

    Protected Sub btnGetStubs_Click(sender As Object, e As EventArgs) Handles btnGetStubs.Click

        Dim todo As List(Of clsUser) = New List(Of clsUser)

        'for everyone in the domain .. add the internal users

        Dim channel As clsChannel = iq.i_channel_code(txtMultiHost.Text)
        For Each Usr In channel.Users.Values '(From j In iq.Users.Values Where j.Email.ToLower.Contains(TxtMultSendDomain.Text.ToLower))
            If Usr.Channel.Code = txtMultiHost.Text Then 'only 'internal' users
                todo.Add(Usr)
            End If
        Next

        If todo.Count > 0 Then
            TxtMultisend.Text = Join((From j In todo Select j.Email).ToArray, ",")
            txtMultiHost.Text = todo.First.Channel.Code
        End If


    End Sub


    Protected Sub btnSearch_Click(sender As Object, e As EventArgs)

    End Sub

    Protected Sub btnOnlyDistiAdmin_Click(sender As Object, e As EventArgs)

    End Sub

    Protected Sub ddlChannels_SelectedIndexChanged(sender As Object, e As EventArgs)

        drpTeams.DataSource = iq.Channels(ddlChannels.SelectedValue).Teams.Values
        drpTeams.DataTextField = "Name"
        drpTeams.DataValueField = "ID"
        drpTeams.DataBind()

        drpCurrency.SelectedValue = If(iq.Channels(ddlChannels.SelectedValue).DefaultCurrency IsNot Nothing, iq.Channels(ddlChannels.SelectedValue).DefaultCurrency.ID, iq.i_currency_code("GBP").ID)
    End Sub

    Protected Sub btnWelcomeResend_Click(sender As Object, e As EventArgs)
        Dim chkStatus As Button = DirectCast(sender, Button)
        Dim row As GridViewRow = DirectCast(chkStatus.NamingContainer, GridViewRow)
        Dim rowindex As Integer = row.RowIndex
        Dim userid As Integer = Convert.ToInt32(grdUser.DataKeys(rowindex).Value)
        Dim u As clsUser = iq.Users(userid)
        'Do reset here
        Dim a = u.Accounts.Where(Function(us) us.Value.BuyerChannel Is CType(iq.sesh(Request.QueryString("lid"), "BuyerAccount"), clsAccount).BuyerChannel).FirstOrDefault
        If a.Value IsNot Nothing Then
            a.Value.ResendWelcomeEmail()
        End If
    End Sub

    Protected Sub btnAddSystemMessage_Click(sender As Object, e As EventArgs) Handles btnAddSystemMessage.Click

        ' Create translation
        Dim kyLanguage As clsLanguage = (From l In iq.Languages.Values Where l.Code = "KY").First
        Dim translation As New clsTranslation(kyLanguage, SystemMessage)

        ' Create message
        Dim validFrom As DateTime
        Dim validTo As DateTime
        Dim enabled As Boolean = chkSystemMessage.Checked

        If Not DateTime.TryParse(txtSystemMessageValidFrom.Text, validFrom) Then
            validFrom = New DateTime(Today.Year, 1, 1)
            txtSystemMessageValidFrom.Text = validFrom.ToString("dd-MMM-yyyy")
        End If

        If Not DateTime.TryParse(txtSystemMessageValidTo.Text, validTo) Then
            validTo = New DateTime(Today.Year, 12, 31)
            txtSystemMessageValidTo.Text = validTo.ToString("dd-MMM-yyyy")
        End If

        Dim message As New clsMessage("SignInScreenMessage", translation, validFrom, validTo, enabled, 1)

        If Not iq.UserMessages Is Nothing Then
            If Not iq.UserMessages.ContainsKey("SignInScreenMessage") Then
                iq.UserMessages.Add("SignInScreenMessage", New List(Of clsMessage))
            End If
            iq.UserMessages("SignInScreenMessage").Add(message)
        End If

        btnAmendSystemMessage.Visible = True
        'btnDeleteSystemMessage.Visible = True
        btnAddSystemMessage.Visible = False

    End Sub

    Protected Sub btnAmendSystemMessage_Click(sender As Object, e As EventArgs) Handles btnAmendSystemMessage.Click

        If Not iq.UserMessages Is Nothing Then
            If iq.UserMessages.ContainsKey("SignInScreenMessage") Then
                If iq.UserMessages("SignInScreenMessage").Count = 1 Then

                    Dim kyLanguage As clsLanguage = (From l In iq.Languages.Values Where l.Code = "KY").First

                    With iq.UserMessages("SignInScreenMessage")(0).Translation
                        .text(kyLanguage) = SystemMessage
                        .Update(kyLanguage)
                    End With

                    Dim validFrom As DateTime = DateTime.Parse(txtSystemMessageValidFrom.Text)
                    Dim validTo As DateTime = DateTime.Parse(txtSystemMessageValidTo.Text)
                    Dim enabled As Boolean = chkSystemMessage.Checked

                    With iq.UserMessages("SignInScreenMessage")(0)
                        .ValidFrom = validFrom
                        .ValidTo = validTo
                        .Enabled = enabled
                        .Update(Nothing)
                    End With

                End If
            End If
        End If

    End Sub

    Protected Sub btnDeleteSystemMessage_Click(sender As Object, e As EventArgs) Handles btnDeleteSystemMessage.Click

        If Not iq.UserMessages Is Nothing Then
            If iq.UserMessages.ContainsKey("SignInScreenMessage") Then

                txtSignInSystemMessage.Text = String.Empty
                txtSystemMessageValidFrom.Text = String.Empty
                txtSystemMessageValidTo.Text = String.Empty
                btnAmendSystemMessage.Visible = False
                btnDeleteSystemMessage.Visible = False
                btnAddSystemMessage.Visible = True

                ' UI actually only supports one message for now
                For Each message As clsMessage In iq.UserMessages("SignInScreenMessage")
                    message.delete(Nothing)
                Next

                iq.UserMessages("SignInScreenMessage").Clear()

            End If
        End If

    End Sub

    Private Property SystemMessage() As String

        Get

            Dim message As String = txtSignInSystemMessage.Text

            message = message.Replace(Environment.NewLine, "<br />")
            message = Server.HtmlEncode(message)

            SystemMessage = message

        End Get

        Set(value As String)

            Dim message As String = value

            message = Server.HtmlDecode(message)
            message = message.Replace("<br />", Environment.NewLine)

            txtSignInSystemMessage.Text = message

        End Set

    End Property

    Protected Sub btnAddHpeSystemMessage_Click(sender As Object, e As EventArgs) Handles btnAddHpeSystemMessage.Click

        Dim lid As UInt64 = Request.QueryString("lid")
        Dim agentaccount As clsAccount = iq.sesh(lid, "AgentAccount")

        ' Create translation
        Dim translation As New clsTranslation(agentaccount.Language, HPESystemMessage)

        ' Create message
        Dim validFrom As DateTime
        Dim validTo As DateTime
        Dim enabled As Boolean = hpeMessageEnabled.Checked

        If Not DateTime.TryParse(txtHpeSystemMessageValidFrom.Text, validFrom) Then
            validFrom = New DateTime(Today.Year, 1, 1)
            txtHpeSystemMessageValidFrom.Text = validFrom.ToString("dd-MMM-yyyy")
        End If

        If Not DateTime.TryParse(txtHpeSystemMessageValidTo.Text, validTo) Then
            validTo = New DateTime(Today.Year, 12, 31)
            txtHpeSystemMessageValidTo.Text = validTo.ToString("dd-MMM-yyyy")
        End If

        Dim message As New clsMessage("HPESystemMessage", translation, validFrom, validTo, enabled, 1)

        If Not iq.UserMessages Is Nothing Then
            If Not iq.UserMessages.ContainsKey("HPESystemMessage") Then
                iq.UserMessages.Add("HPESystemMessage", New List(Of clsMessage))
            End If
            iq.UserMessages("HPESystemMessage").Add(message)
        End If

        btnAmendHpeSystemMessage.Visible = True
        'btnDeleteHpeSystemMessage.Visible = True
        btnAddHpeSystemMessage.Visible = False

    End Sub

    Protected Sub btnAddHpiSystemMessage_Click(sender As Object, e As EventArgs) Handles btnAddHpiSystemMessage.Click

        Dim lid As UInt64 = Request.QueryString("lid")
        Dim agentaccount As clsAccount = iq.sesh(lid, "AgentAccount")

        ' Create translation
        Dim translation As New clsTranslation(agentaccount.Language, HPISystemMessage)

        ' Create message
        Dim validFrom As DateTime
        Dim validTo As DateTime
        Dim enabled As Boolean = hpiMessageEnabled.Checked

        If Not DateTime.TryParse(txtHpiSystemMessageValidFrom.Text, validFrom) Then
            validFrom = New DateTime(Today.Year, 1, 1)
            txtHpiSystemMessageValidFrom.Text = validFrom.ToString("dd-MMM-yyyy")
        End If

        If Not DateTime.TryParse(txtHpiSystemMessageValidTo.Text, validTo) Then
            validTo = New DateTime(Today.Year, 12, 31)
            txtHpiSystemMessageValidTo.Text = validTo.ToString("dd-MMM-yyyy")
        End If

        Dim message As New clsMessage("HPISystemMessage", translation, validFrom, validTo, enabled, 1)

        If Not iq.UserMessages Is Nothing Then
            If Not iq.UserMessages.ContainsKey("HPISystemMessage") Then
                iq.UserMessages.Add("HPISystemMessage", New List(Of clsMessage))
            End If
            iq.UserMessages("HPISystemMessage").Add(message)
        End If

        btnAmendHpiSystemMessage.Visible = True
        'btnDeleteHpiSystemMessage.Visible = True
        btnAddHpiSystemMessage.Visible = False

    End Sub

    Protected Sub btnAmendHpeSystemMessage_Click(sender As Object, e As EventArgs) Handles btnAmendHpeSystemMessage.Click

        Dim lid As UInt64 = Request.QueryString("lid")
        Dim agentaccount As clsAccount = iq.sesh(lid, "AgentAccount")

        If Not iq.UserMessages Is Nothing Then
            If iq.UserMessages.ContainsKey("HPESystemMessage") Then
                If iq.UserMessages("HPESystemMessage").Count = 1 Then

                    With iq.UserMessages("HPESystemMessage")(0).Translation
                        .text(agentaccount.Language) = HPESystemMessage
                        .Update(agentaccount.Language)
                    End With

                    Dim validFrom As DateTime = DateTime.Parse(txtHpeSystemMessageValidFrom.Text)
                    Dim validTo As DateTime = DateTime.Parse(txtHpeSystemMessageValidTo.Text)
                    Dim enabled As Boolean = hpeMessageEnabled.Checked

                    With iq.UserMessages("HPESystemMessage")(0)
                        .ValidFrom = validFrom
                        .ValidTo = validTo
                        .Enabled = enabled
                        .Update(Nothing)
                    End With

                End If
            End If
        End If

    End Sub

    Protected Sub btnAmendHpiSystemMessage_Click(sender As Object, e As EventArgs) Handles btnAmendHpiSystemMessage.Click

        Dim lid As UInt64 = Request.QueryString("lid")
        Dim agentaccount As clsAccount = iq.sesh(lid, "AgentAccount")

        If Not iq.UserMessages Is Nothing Then
            If iq.UserMessages.ContainsKey("HPISystemMessage") Then
                If iq.UserMessages("HPISystemMessage").Count = 1 Then

                    With iq.UserMessages("HPISystemMessage")(0).Translation
                        .text(agentaccount.Language) = HPISystemMessage
                        .Update(agentaccount.Language)
                    End With

                    Dim validFrom As DateTime = DateTime.Parse(txtHpiSystemMessageValidFrom.Text)
                    Dim validTo As DateTime = DateTime.Parse(txtHpiSystemMessageValidTo.Text)
                    Dim enabled As Boolean = hpiMessageEnabled.Checked

                    With iq.UserMessages("HPISystemMessage")(0)
                        .ValidFrom = validFrom
                        .ValidTo = validTo
                        .Enabled = enabled
                        .Update(Nothing)
                    End With

                End If
            End If
        End If

    End Sub

    Protected Sub btnDeleteHpeSystemMessage_Click(sender As Object, e As EventArgs) Handles btnDeleteHpeSystemMessage.Click

        If Not iq.UserMessages Is Nothing Then
            If iq.UserMessages.ContainsKey("HPESystemMessage") Then

                txtHpeSystemMessage.Text = String.Empty
                txtHpeSystemMessageValidFrom.Text = String.Empty
                txtHpeSystemMessageValidTo.Text = String.Empty
                btnAmendHpeSystemMessage.Visible = False
                btnDeleteHpeSystemMessage.Visible = False
                btnAddHpeSystemMessage.Visible = True

                ' UI actually only supports one message for now
                For Each message As clsMessage In iq.UserMessages("HPESystemMessage")
                    message.delete(Nothing)
                Next

                iq.UserMessages("HPESystemMessage").Clear()

            End If
        End If

    End Sub

    Protected Sub btnDeleteHpiSystemMessage_Click(sender As Object, e As EventArgs) Handles btnDeleteHpiSystemMessage.Click

        If Not iq.UserMessages Is Nothing Then
            If iq.UserMessages.ContainsKey("HPISystemMessage") Then

                txtHpiSystemMessage.Text = String.Empty
                txtHpiSystemMessageValidFrom.Text = String.Empty
                txtHpiSystemMessageValidTo.Text = String.Empty
                btnAmendHpiSystemMessage.Visible = False
                btnDeleteHpiSystemMessage.Visible = False
                btnAddHpiSystemMessage.Visible = True

                ' UI actually only supports one message for now
                For Each message As clsMessage In iq.UserMessages("HPISystemMessage")
                    message.delete(Nothing)
                Next

                iq.UserMessages("HPISystemMessage").Clear()

            End If
        End If

    End Sub

    Private Property HPESystemMessage() As String

        Get

            Dim message As String = txtHpeSystemMessage.Text

            message = message.Replace(Environment.NewLine, "<br />")
            message = Server.HtmlEncode(message)

            HPESystemMessage = message

        End Get

        Set(value As String)

            Dim message As String = value

            message = Server.HtmlDecode(message)
            message = message.Replace("<br />", Environment.NewLine)

            txtHpeSystemMessage.Text = message

        End Set

    End Property

    Private Property HPISystemMessage() As String

        Get

            Dim message As String = txtHpiSystemMessage.Text

            message = message.Replace(Environment.NewLine, "<br />")
            message = Server.HtmlEncode(message)

            HPISystemMessage = message

        End Get

        Set(value As String)

            Dim message As String = value

            message = Server.HtmlDecode(message)
            message = message.Replace("<br />", Environment.NewLine)

            txtHpiSystemMessage.Text = message

        End Set

    End Property

    Protected Sub AdminMenu_MenuItemClick(sender As Object, e As MenuEventArgs) Handles adminMenu.MenuItemClick

        ' Switch tab
        Select Case e.Item.Value.ToLower()

            Case "useradmin"
                adminMultiView.SetActiveView(tabUserAdmin)

            Case "createuser"
                adminMultiView.SetActiveView(tabCreateUser)

            Case "system"
                adminMultiView.SetActiveView(tabSystem)

            Case "reports"
                adminMultiView.SetActiveView(tabReports)

        End Select

    End Sub

    Protected Sub HpiMessageEnabled_CheckedChanged(sender As Object, e As EventArgs)

        ToggleMessageEnabled("HPISystemMessage", hpiMessageEnabled)

    End Sub

    Protected Sub hpeMessageEnabled_CheckedChanged(sender As Object, e As EventArgs)

        ToggleMessageEnabled("HPESystemMessage", hpeMessageEnabled)

    End Sub

    Protected Sub chkSystemMessage_CheckedChanged(sender As Object, e As EventArgs)

        ToggleMessageEnabled("SignInScreenMessage", chkSystemMessage)

    End Sub

    Private Sub ToggleMessageEnabled(userMessage As String, checkbox As CheckBox)

        Dim lid As UInt64 = Request.QueryString("lid")
        Dim agentaccount As clsAccount = iq.sesh(lid, "AgentAccount")

        If Not iq.UserMessages Is Nothing Then
            If iq.UserMessages.ContainsKey(userMessage) Then
                If iq.UserMessages(userMessage).Count = 1 Then

                    With iq.UserMessages(userMessage)(0)
                        .Enabled = checkbox.Checked
                        .Update(Nothing)
                    End With

                End If
            End If
        End If

    End Sub

    Protected Sub roleSelect_Click(sender As Object, e As EventArgs)

        Dim cme As CommandEventArgs = TryCast(e, CommandEventArgs)

        If cme IsNot Nothing Then

            Dim accountID As Integer = Int32.Parse(cme.CommandArgument)

            Dim account = iq.Accounts(accountID)
            Dim roles = account.Roles

            Page.ClientScript.RegisterStartupScript(Me.GetType(), "RoleSelector", "ShowRoleSelector();", True)



        End If

    End Sub
End Class

