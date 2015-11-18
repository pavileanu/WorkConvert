Option Explicit On
'Option Strict On

Public Class accounts
    Inherits clsPageLogging

    Public hpiList As DropDownList
    Public hpeList As DropDownList

    ' This page is responsible for deciding which Account is used within IQ2
    '
    ' It can be called by either SignIn.aspx or Gatekeeper.aspx. On either
    ' route, the following are looked for in the iq.sesh dictionary:
    '
    '   UserID          - Mandatory. The string ID of the logged-on user.
    '   AccountList     - Mandatory. An IEnumerable(Of clsAccount) of all the user's possible accounts.
    '   Host            - Optional. A seller channel ID. Used to restricts the list of selectable accounts.
    '   MFR             - Optional. HPE/HPI. Used to restricts the list of selectable accounts.
    '   Base            - Optional. A SKU code. Used to restricts the list of selectable accounts and also to
    '                     work out which side of the HPE/HPI split we're on (potentially overriding any MFR value).
    '
    ' The general principle is that the list of possible accounts is worked out from the above and the following takes place: 
    ' - If the list contains no items, an informative message is displayed (we could maybe try redirecting to the referring page?)
    ' - If the list contains one item, the account is used and we redirect to Tree.aspx with no account selection UI shown
    ' - If the list contains more than one item, the account selection UI is shown (split into HPE/HPI as appropriate) and shown.
    '   Once a choice is made we redirect to Tree.aspx.

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load


        Dim lid As UInt64 = CType(Request.QueryString("lid"), UInt64)

        If Not TypeOf (iq.sesh(lid, "UserID")) Is Integer Then Exit Sub
        If Not TypeOf (iq.sesh(lid, "AccountList")) Is IEnumerable(Of clsAccount) Then Exit Sub

        Dim u As clsUser = iq.Users(iq.sesh(lid, "UserID"))
        Dim accountList As IEnumerable(Of clsAccount) = iq.sesh(lid, "AccountList")

        'iq.sesh(lid, "BuyerAccount") = Nothing
        'iq.sesh(lid, "AgentAccount") = Nothing

        ' Filter the account list by Host (if specified) and sort it into quote count order
        u.CountQuotesPerAccount()
        Dim sortedAccounts As IEnumerable(Of clsAccount)
        If Not TypeOf (iq.sesh(lid, "Host")) Is String OrElse String.IsNullOrEmpty(iq.sesh(lid, "Host")) Then
            sortedAccounts = From ac In accountList
                             Order By ac.NumQuotes Descending
        Else
            Dim host As String = iq.sesh(lid, "Host").ToString()
            sortedAccounts = From ac In accountList
                             Where String.Equals(ac.SellerChannel.Code, host, StringComparison.InvariantCultureIgnoreCase)
                             Order By ac.NumQuotes Descending
        End If

        ' If a deep link SKU has been specified, use this now to infer the Manufacturer (i.e. HPE/HPI) - 
        ' potentially overriding any MFR specified
        If Not iq.sesh(lid, "Base") Is Nothing Then
            Dim sku As String = iq.sesh(lid, "Base").ToString()
            If iq.i_SKU.ContainsKey(sku) Then
                Dim product = iq.i_SKU(sku)
                If product.Manufacturer <> Manufacturer.Unknown Then
                    iq.sesh(lid, "MFR") = product.Manufacturer
                End If
            End If
        End If

        ' HPI/HPE - create split lists of accounts
        Dim sortedAccountsHPI As IEnumerable(Of clsAccount) = Nothing
        Dim sortedAccountsHPE As IEnumerable(Of clsAccount) = Nothing

        ' If we have a manufacturer (either MFR or Base specified on the QueryString), only look for accounts on this side
        Dim mfr As Manufacturer = Manufacturer.Unknown
        If Not iq.sesh(lid, "MFR") Is Nothing Then
            mfr = iq.sesh(lid, "MFR")
        End If

        Dim count As Integer = 0
        If mfr = Manufacturer.HPI OrElse mfr = Manufacturer.Unknown Then
            sortedAccountsHPI = From ac In sortedAccounts Where ac.Manufacturer = Manufacturer.HPI
            count += sortedAccountsHPI.Count
        End If

        If mfr = Manufacturer.HPE OrElse mfr = Manufacturer.Unknown Then
            '                                                                                         ADDED by Nick / 26/05/2015
            sortedAccountsHPE = From ac In sortedAccounts Where ac.Manufacturer = Manufacturer.HPE Or ac.Manufacturer = Manufacturer.Unknown
            count += sortedAccountsHPE.Count
        End If

        ' If we ended up with no options, display a message to the user
        ' If we have only one possible option select it automatically
        ' If there's more than one possible account, display the account selector to the user
        Dim language As clsLanguage = English
        If count = 0 Then
            Dim noAccounts As New Literal
            noAccounts.Text = Xlt("Sorry, no accounts found. Please log out and try again.", language)
            panelContent.Controls.Add(noAccounts)
        ElseIf count = 1 Then
            Dim accountID As Integer
            If Not sortedAccountsHPI Is Nothing AndAlso sortedAccountsHPI.Count = 1 Then
                accountID = sortedAccountsHPI(0).ID
            Else
                accountID = sortedAccountsHPE(0).ID
            End If
            SelectAccount(lid, accountID)
        Else
            Dim instructions As New Literal
            instructions.Text = String.Format("<h2>{0}</h2>", Xlt("Where would you like to visit?", language))
            panelContent.Controls.Add(instructions)

            If Not sortedAccountsHPI Is Nothing AndAlso sortedAccountsHPI.Count > 0 Then
                panelContent.Controls.Add(BuildAccountList("HPI", sortedAccountsHPI, language))
            End If

            If Not sortedAccountsHPE Is Nothing AndAlso sortedAccountsHPE.Count > 0 Then
                panelContent.Controls.Add(BuildAccountList("HPE", sortedAccountsHPE, language))
            End If
        End If

    End Sub

    Private Function BuildAccountList(mfrCode As String, accounts As IEnumerable(Of clsAccount), language As clsLanguage) As Panel

        BuildAccountList = New Panel()
        BuildAccountList.CssClass = "HostList"

        Dim img As New Image
        img.ImageUrl = String.Format("/images/{0}-Logo.jpg", mfrCode)
        BuildAccountList.Controls.Add(img)

        If accounts.Count > 0 Then

            ' Build either the HPI or HPE list
            Dim list As DropDownList = Nothing
            If mfrCode = "HPI" Then
                Me.hpiList = New DropDownList()
                list = hpiList
            ElseIf mfrCode = "HPE" Then
                Me.hpeList = New DropDownList()
                list = hpeList
            End If

            If Not list Is Nothing Then

                If accounts.Count > 1 Then
                    Dim item As New ListItem()
                    item.Text = Xlt("Select account", language)
                    item.Value = -1
                    list.Items.Add(item)
                End If

                For Each account In accounts

                    Dim item As New ListItem()

                    item.Value = account.ID
                    item.Attributes.Add("HostID", account.SellerChannel.Code)
                    item.Attributes.Add("BuyerRegion", account.BuyerChannel.Region.Code)
                    item.Attributes.Add("BuyerID", account.BuyerChannel.ID)
                    item.Attributes.Add("AccountCurrency", account.Currency.Code)
                    item.Text = String.Format("{0} ({1}) - {2} {3}", account.SellerChannel.Name, account.SellerChannel.Region.Code, account.NumQuotes, Xlt("quotes", language))

                    list.Items.Add(item)

                Next

                BuildAccountList.Controls.Add(list)

                Dim button As New Button()
                button.Text = Xlt("Go", language)
                button.CommandName = mfrCode
                AddHandler button.Click, AddressOf OnAccountSelected
                BuildAccountList.Controls.Add(button)

            End If

        End If

    End Function

    Private Sub OnAccountSelected(sender As Object, e As System.EventArgs)

        If sender.GetType() Is GetType(Button) Then

            Dim lid As UInt64 = CType(Request.QueryString("lid"), UInt64)
            Dim button As Button = sender
            Dim errorMessages As List(Of String) = New List(Of String)
            Dim accountID As Integer = Integer.MinValue

            If (button.CommandName = "HPI") AndAlso (Not hpiList Is Nothing) Then
                accountID = hpiList.SelectedValue
            ElseIf (button.CommandName = "HPE") AndAlso (Not hpeList Is Nothing) Then
                accountID = hpeList.SelectedValue
            End If

            SelectAccount(lid, accountID)

        End If

    End Sub

    Private Sub SelectAccount(lid As UInt64, accountID As Integer)

        If accountID >= 0 Then

            If iq.Accounts.ContainsKey(accountID) Then

                Dim account = iq.Accounts(accountID)

                ' Moved from Gatekeeper.aspx as the required account is now only known here
                If Not iq.sesh(lid, "viaGatekeeper") Is Nothing Then
                    If Not String.IsNullOrEmpty(iq.sesh(lid, "gk_cPriceBand")) Then
                        account.Priceband = iq.getPriceBand(iq.sesh(lid, "gk_cPriceBand"))
                    ElseIf Not String.IsNullOrEmpty(iq.sesh(lid, "gk_cAccountNum")) Then
                        account.Priceband = iq.getPriceBand(iq.sesh(lid, "gk_cAccountNum"))
                    End If
                    account.update(errorMessages)
                End If

                SwitchAccount(lid, account, account, errorMessages)

                Form.Controls.Add(ErrorDymo("Scanning promotions (could take a few seconds) . . .", lid))
                If errorMessages.Count = 0 Then OutputErrors(Form.Controls, errorMessages, lid, True)

                Response.Redirect("scanpromos.aspx?lid=" & lid, False)

            End If

        End If

    End Sub

End Class