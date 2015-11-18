
Imports System.Reflection
Imports dataAccess

Public Class WebForm1
    Inherits System.Web.UI.Page
    Private agentaccount As clsAccount
    Private buyeraccount As clsAccount
    Private dtUsers As DataTable
    Private iFrameStyle, iFrameSrc As String
    Public AccountCanDisableUsers As Boolean = False
    Public AccountCanSetupUsers As Boolean = False
    Public AccountCanResetPasswords As Boolean = False
    Public AccountIsDistiAdmin As Boolean = False
    Public AvailableRoles As List(Of clsRole)
    Private NewCreatedUsers As List(Of clsUser)

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim lid As UInt64 = Request.QueryString("lid")
        If Not iq.SeshAlive(lid) OrElse Not (AccountHasRight(lid, "ADMINMENU") Or AccountHasRight(lid, "GLOBALADM")) Then
            Response.Redirect("signin.aspx")
        End If

        AvailableRoles = iq.i_role_Code.Values.ToList




        'If Not Page.IsPostBack Then
        dtUsers = New DataTable
        Dim dtActivity As New DataTable
        dtUsers.Columns.Add("ID", GetType(Integer))
        dtUsers.Columns.Add("RealName", GetType(String))
        dtUsers.Columns.Add("ChannelName", GetType(String))
        dtUsers.Columns.Add("Disabled", GetType(Boolean))
        dtUsers.Columns.Add("Quotes", GetType(Integer))
        dtUsers.Columns.Add("Options", GetType(Integer))
        dtUsers.Columns.Add("Systems", GetType(Integer))
        dtUsers.Columns.Add("Pitch", GetType(Decimal))
        dtUsers.Columns.Add("LastUsed", GetType(String))
        dtUsers.Columns.Add("Roles", GetType(clsRole()))


        dtActivity.Columns.Add("Type", GetType(String))
        dtActivity.Columns.Add("Today", GetType(Integer))
        dtActivity.Columns.Add("7days", GetType(Integer))
        dtActivity.Columns.Add("MTD", GetType(Integer))
        dtActivity.Columns.Add("LastMonth", GetType(Integer))
        'End If
        agentaccount = iq.sesh(lid, "AgentAccount")
        buyeraccount = iq.sesh(lid, "BuyerAccount")

        If agentaccount Is Nothing Then Response.Redirect("signin.aspx")
        'Dim dtUsers As New  DataTable
        Dim userToday As Integer = 0
        Dim user7Days As Integer = 0
        Dim userMTD As Integer = 0
        Dim userLastMonth As Integer = 0
        Dim quoteSummary(4, 4) As Integer

        If agentaccount.HasRight("DISABLEUSR") Then AccountCanDisableUsers = True
        If agentaccount.HasRight("CREATEUSR") Then AccountCanSetupUsers = True
        If agentaccount.HasRight("PWDRESET") Then AccountCanResetPasswords = True
        If agentaccount.HasRight("DISTADMIN") Or agentaccount.HasRight("GLOBALADM") Then AccountIsDistiAdmin = True

        If agentaccount.HasRight("GLOBALADM") Then
            PnlMultiSend.Visible = True
            PnlHostOverride.Visible = True
        End If



        If Not IsPostBack Then
            txtMultiHost.Text = agentaccount.SellerChannel.Code
        End If

        Dim errormessages As List(Of String) = New List(Of String)

        If Not IsPostBack Then

            Dim list = From j In iq.Accounts.Values Where j.SellerChannel Is agentaccount.SellerChannel

            Dim cnt As Integer = list.Count

            'run a select to find out which accounts have a quote in the last 60 days . .

            Dim con As SqlClient.SqlConnection
            con = da.OpenDatabase

            Dim sql$ = "SELECT distinct(fk_account_id_agent) FROM quote q "
            sql$ &= "JOIN account a ON q.fk_account_id_agent=a.id  "
            sql$ &= "WHERE updated>getdate()-100 AND a.fk_channel_id_seller=" & agentaccount.SellerChannel.ID

            Dim toReportOn As New HashSet(Of Integer)
            Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)
            While rdr.Read
                toReportOn.Add(rdr.Item(0))
            End While
            rdr.Close()



            'This is WAY too slow - there are MANY accounts 5k for westcoast.. (and only a few quotes atm)
            For Each account In list

                If toReportOn.Contains(account.ID) Then  'Therse are acocunts which did a quote in the last 100 days
                    Dim optionCount As Integer = 0
                    Dim systemCount As Integer = 0
                    Dim pitchRate As Decimal = 0
                    Dim lastDate As Date

                    account.LoadQuotes(0)
                    If account.Quotes.Values.Count > 0 Then
                        For Each quotedetails In account.Quotes.Values.ToList 'NA - was getting 'collection was modified;' cannot enumerate - type messages - added .tolist
                            If quotedetails.Updated > Date.Today.AddDays(-62) Then 'This report does'n involve any quote older than 2 months
                                quotedetails.LoadItems(errormessages)
                                Dim flatList As clsFlatList = quotedetails.RootItem.Flattened(True, False, 0)
                                For Each item As clsFlatListItem In flatList.items
                                    'Debug.WriteLine(account.User.RealName & "  :  " & item.Quantity & "  :  " & item.QuoteItem.Branch.Product.isSystem)
                                    If Not item.QuoteItem.Branch.Product.isSystem Then
                                        optionCount += item.Quantity
                                    Else
                                        systemCount += item.Quantity
                                    End If
                                Next
                                If quotedetails.Created > lastDate Then lastDate = quotedetails.Created
                                Select Case quotedetails.State.code
                                    Case "#NW"
                                        If quotedetails.Created = Date.Today Then quoteSummary(1, 0) += 1
                                        If quotedetails.Created > Date.Today.AddDays(-7) Then quoteSummary(1, 1) += 1
                                        If quotedetails.Created > Date.Today.AddDays(-30) Then quoteSummary(1, 2) += 1
                                        If quotedetails.Created.Month = Date.Today.Month - 1 Then quoteSummary(1, 3) += 1
                                    Case "#CV"
                                        If quotedetails.Created = Date.Today Then quoteSummary(2, 0) += 1
                                        If quotedetails.Created > Date.Today.AddDays(-7) Then quoteSummary(2, 1) += 1
                                        If quotedetails.Created > Date.Today.AddDays(-30) Then quoteSummary(2, 2) += 1
                                        If quotedetails.Created.Month = Date.Today.Month - 1 Then quoteSummary(2, 3) += 1
                                    Case "#QS"
                                        If quotedetails.Created = Date.Today Then quoteSummary(3, 0) += 1
                                        If quotedetails.Created > Date.Today.AddDays(-7) Then quoteSummary(3, 1) += 1
                                        If quotedetails.Created > Date.Today.AddDays(-30) Then quoteSummary(3, 2) += 1
                                        If quotedetails.Created.Month = Date.Today.Month - 1 Then quoteSummary(3, 3) += 1
                                End Select
                            End If
                        Next
                        If systemCount = 0 Then systemCount = 1 ' otherwise we'll get a DBZ ! (WE DO!)
                        pitchRate = optionCount / systemCount
                        pitchRate = Decimal.Round(pitchRate, 2)
                    End If
                    Dim dateString As String = IIf(lastDate = Nothing, "", lastDate.ToString("yyyy-MM-dd"))
                    dtUsers.Rows.Add(account.User.ID, account.User.RealName, account.BuyerChannel.Name, account.User.Disabled, account.Quotes.Values.Count, optionCount, systemCount, pitchRate, dateString, account.Roles)
                End If
            Next
            dtActivity.Rows.Add("Users", quoteSummary(0, 0), quoteSummary(0, 1), quoteSummary(0, 2), quoteSummary(0, 3))
            dtActivity.Rows.Add("Quotes - New", quoteSummary(1, 0), quoteSummary(1, 1), quoteSummary(1, 2), quoteSummary(1, 3))
            dtActivity.Rows.Add("Quotes - Saved", quoteSummary(2, 0), quoteSummary(2, 1), quoteSummary(2, 2), quoteSummary(2, 3))
            dtActivity.Rows.Add("Quotes - Exported", quoteSummary(3, 0), quoteSummary(3, 1), quoteSummary(3, 2), quoteSummary(3, 3))

            dtUsers.DefaultView.Sort = "Quotes DESC"
            iq.sesh(lid, "UserDetailsTable") = dtUsers
            iq.sesh(lid, "ActivitySummaryTable") = dtActivity

            agentaccount.SellerChannel.fixteams(errormessages)
            


            drpTeams.DataSource = agentaccount.SellerChannel.Teams.Values
            drpTeams.DataTextField = "Name"
            drpTeams.DataValueField = "ID"
            drpTeams.DataBind()

            drpDomain.DataSource = agentaccount.SellerChannel.Domains
            drpDomain.DataBind()

            drpDomain.Items.Add("gmail.com")
            drpDomain.Items.Add("channelcentral.net")

            drpCurrency.DataSource = iq.Currencies.Values
            drpCurrency.DataTextField = "Code"
            drpCurrency.DataValueField = "ID"
            drpCurrency.DataBind()


        Else
            dtUsers = iq.sesh(lid, "UserDetailsTable")


            dtActivity = iq.sesh(lid, "ActivitySummaryTable")

        End If
        If iq.sesh(lid, "NewUsers") IsNot Nothing Then
            NewCreatedUsers = iq.sesh(lid, "NewUsers")
            Dim list = (From j In iq.Accounts.Values Where j.SellerChannel Is agentaccount.SellerChannel).ToList()
            For Each usr In NewCreatedUsers
                Dim account As clsAccount = (From k In list Where k.User.ID = usr.ID).FirstOrDefault
                dtUsers.Rows.Add(usr.ID, usr.RealName, account.BuyerChannel.Name, usr.Disabled, 0, 0, 0, 0.0, Today.ToString("yyyy-MM-dd"), account.Roles)
            Next
        End If

        grdActivity.DataSource = dtActivity
        grdActivity.DataBind()
        'grdUser.DataSource = agentaccount.sellerchannel.users.values
        grdUser.DataSource = dtUsers
        grdUser.DataBind()

        OutputErrors(Form.Controls, errormessages, lid, True)

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
    Protected Sub chkDisabled_CheckedChanged1(sender As Object, e As EventArgs)
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
        '  emailDomain = drpDomain.SelectedValue
        fullName = txtFullName.Text
        telephoneNumber = TxtTelephone.Text

        Dim allGood As Boolean = False 'fail safe
        Dim errorMessages As List(Of String) = New List(Of String)

        If Not (String.IsNullOrWhiteSpace(emailName) Or String.IsNullOrWhiteSpace(fullName)) Then

            'emailName = emailName & "@" & emailDomain
            'only create a new user if they dont' already exist (they may pre-exist and have accounts at another channel)
            Dim user As clsUser
            Dim lit As Literal = New Literal

            Dim OnChannel As clsChannel = agentaccount.SellerChannel

            If txtHostOverride.Text <> "" Then
                If iq.i_channel_code.ContainsKey(txtHostOverride.Text) Then
                    OnChannel = iq.i_channel_code(txtHostOverride.Text)
                Else
                    OnChannel = Nothing
                End If
            End If

            If OnChannel Is Nothing Then
                errormessages.Add("Host Override is invalid")
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

                    Dim lid As UInt64 = Request.QueryString("lid")
                    If iq.sesh(lid, "NewUsers") Is Nothing Then
                        NewCreatedUsers = New List(Of clsUser)
                    Else
                        NewCreatedUsers = iq.sesh(lid, "NewUsers")
                    End If
                    NewCreatedUsers.Add(user)
                    iq.sesh(lid, "NewUsers") = NewCreatedUsers

                    'Check if user type is admin otherwise select role as user
                    Dim userType As String = "user"
                    If chkAdminUser.Checked Then userType = "admin"
                    Dim rolesSelected = From r In iq.i_role_Code.Values Where r.Code.ToUpper = userType.ToUpper Select r
                    Dim role As clsRole = rolesSelected(0)

                    ' Create  account
                    'NB Accoutns are created with the same currency as the user (agentAccount) setting them up !
                    'If you create an account in the wrong currency it will see no prices !
                    Dim currency As clsCurrency = iq.Currencies(drpCurrency.SelectedValue)

                    Dim account As clsAccount = New clsAccount(user, password, OnChannel, {role}, OnChannel.Teams.Values(0), agentaccount.Language, currency, OnChannel, agentaccount.Priceband, agentaccount.Culture, agentaccount.mfrCode)
                    Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
                    Dim baseurl As String = ConfigurationManager.AppSettings("BaseURL")

                    Dim url As String
                    url = baseurl & "/aspx/signin.aspx"

                    tags.Add("hostname", OnChannel.DisplayName(agentaccount.Language))
                    tags.Add("email", emailName)
                    tags.Add("password", pw$)
                    tags.Add("firstname", fullName)
                    tags.Add("url", url)
                    tags.Add("mfr", buyeraccount.mfrCode)
                    tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English))

                    Dim em As List(Of String) = New List(Of String)  'Returns any error messages encountered whilst emailing
                    If chkEmailUser.Checked Then
                        SendEmail(emailName, "WelcomeEmail.htm", tags, agentaccount.Language, em, False)
                    End If
                    If chkEmailAdmin.Checked Then
                        SendEmail(agentaccount.User.Email, "WelcomeEmail.htm", tags, agentaccount.Language, em, False)
                    End If
                    drpDomain.SelectedIndex = 0
                    drpTeams.SelectedIndex = 0
                    txtEmailName.Text = ""
                    txtFullName.Text = ""
                    TxtTelephone.Text = ""

                End If
            End If
        End If

        OutputErrors(Me.Form.Controls, errormessages, Request("lid"))


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
            a.Value.AddRole(iq.i_role_Code(sender.parent.parent.cells(10).controls(7).selecteditem.value))
            'For reload lets frig the source
            Response.Redirect("admin.aspx" & Request.Url.Query)
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
            a.Value.RemoveRole(iq.i_role_Code(sender.parent.parent.cells(10).controls(1).selecteditem.value))
            Response.Redirect("admin.aspx" & Request.Url.Query)
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


        OutputErrors(PnlMultiSend.Controls, errormessages, Request("Lid"))

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
End Class

