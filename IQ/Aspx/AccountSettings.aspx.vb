
Imports System.Text.RegularExpressions

Public Class AccountSettings
    Inherits System.Web.UI.Page

    Private Sub Account_Settings_Init(sender As Object, e As System.EventArgs) Handles Me.Init

        Dim activeLiveLanguage = From l In iq.Languages.Values Where l.Active = True And l.Live = True And l.Code <> "KY"
        For Each kvp In activeLiveLanguage
            DDLLanguage.Items.Add(New ListItem(kvp.LocalName, kvp.ID))
        Next

        For Each culturelist In iq.Cultures
            ddlCulture.Items.Add(New ListItem(culturelist.Value.Name, culturelist.Key))
        Next
    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim lid As UInt64 = Request.QueryString("lid")

        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        If agentAccount Is Nothing Then Exit Sub


        If Not IsPostBack Then
            DDLLanguage.SelectedValue = agentAccount.Language.ID
            ddlCulture.SelectedValue = agentAccount.Culture.ID
            TxtFullName.Text = agentAccount.User.RealName
            TxtTelephone.Text = agentAccount.User.tel1.DisplayValue
            txtEmail.Text = agentAccount.User.Email
            TxtChangePassword.Text = ""
            TxtConfirmChangePassword.Text = ""
            TxtpriceBand.Text = agentAccount.Priceband.text
            lbRoles.DataSource = agentAccount.Roles
            lbRoles.DataBind()
            chkUpdadateAccounts.Text = Xlt("  Apply to all my iQuote accounts.", agentAccount.Language)
        End If

        If UserIsAdmin(lid) Then TxtpriceBand.Enabled = True

        LblInfo.Text = "AccountID:" & agentAccount.ID & " UserID:" & agentAccount.User.ID

        CompareValidator1.ErrorMessage = Xlt("The passwords you supplied do not match", agentAccount.Language)
        lblRegex.Text = Xlt("Passwords must be at least 8 characters and include mixed case and a number", agentAccount.Language)
        lblRegex.Visible = False ' vldRegex.Visible = False
        requiredPasswordConfirm.ErrorMessage = CompareValidator1.ErrorMessage

        h1HeaderContainer.InnerHtml = Xlt("Account Settings", agentAccount.Language)
    End Sub

    Protected Sub BtnSave_Click(sender As Object, e As EventArgs) Handles BtnSave.Click

        ' CompareValidators don't raise errors if one of the fields is left blank (!), so we enable a
        ' RequiredFieldValidator too if a new password has been entered
        If TxtChangePassword.Text.Length > 0 Then
            requiredPasswordConfirm.Enabled = True
        Else
            requiredPasswordConfirm.Enabled = False
        End If

        Page.Validate()

        Dim errormessages As List(Of String) = New List(Of String)
        Dim lid As UInt64 = Request.QueryString("lid")
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        Dim rg As New RegularExpressions.Regex("^((?=.*?[a-z])(?=.*?[A-Z])(?=.*?[^a-zA-Z]).{8,})$")


        If TxtChangePassword.Text <> "" AndAlso TxtChangePassword.Text = TxtConfirmChangePassword.Text Then
            If rg.IsMatch(TxtChangePassword.Text) Then
                agentAccount.Password = simpleHash(TxtChangePassword.Text)
                If chkUpdadateAccounts.Checked Then
                    For Each ac In agentAccount.User.Accounts.Values
                        ac.Password = simpleHash(TxtChangePassword.Text)
                        ac.update(errormessages)
                    Next
                End If
            Else
                lblregex.Visible = True
            End If
        End If

        agentAccount.Language = iq.Languages(DDLLanguage.SelectedValue)

        agentAccount.User.RealName = TxtFullName.Text
        agentAccount.User.tel1 = New nullableString(TxtTelephone.Text)
        agentAccount.Priceband = iq.getPriceBand(TxtpriceBand.Text)
        agentAccount.Culture = iq.Cultures(ddlCulture.SelectedValue)

        agentAccount.User.update(errormessages)
        agentAccount.update(errormessages)

        If errormessages.Count = 0 Then
            LblInfo.Text = Xlt("Changes saved successfully.", agentAccount.Language)
        Else
            Dim p = New Panel
            OutputErrors(p.Controls, errormessages, True)
            LblInfo.Controls.Add(p)
        End If



    End Sub


End Class