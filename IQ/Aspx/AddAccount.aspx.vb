Public Class NewAccount

    'used from the customer search screen - does not manage currency, role, etc..

    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim lid As uint64 = Request.QueryString("lid")

        If Not IsPostBack Then

            Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
            Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

            TxtCompany.Text = buyeraccount.BuyerChannel.DisplayName(buyeraccount.Language) 'this is locked
            TxtEmail.Text = buyeraccount.User.Email  'this has the @company.com
            TxtName.Text = buyeraccount.User.RealName 'initally empty
            TxtpriceBand.Text = buyerAccount.Priceband.Text 'this'll be blank initially

        End If

        'add the script to close the iframe
        '  BtnGo.Attributes("onclick") = "closeIFrame('" & Request("frameid") & "'');"
        'we only want to close the frame if the validation is OK
        BtnCancel.Attributes("onclick") = "window.parent.closeIFrame('" & Request("frameid") & "');"

    End Sub

    Protected Sub BtnGo_Click(sender As Object, e As EventArgs) Handles BtnGo.Click

        Dim errormessages As List(Of String) = New List(Of String)

        Dim lid As UInt64 = Request.QueryString("lid")

        If InStr(TxtEmail.Text, ".") And InStr(TxtEmail.Text, "@") And Left$(TxtEmail.Text, 1) <> "@" Then

            Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)


            buyerAccount.User.Email = TxtEmail.Text
            buyerAccount.User.RealName = TxtName.Text

            Dim pw$ = GeneratePassword()
            buyerAccount.Password = simpleHash(pw$)
            buyerAccount.Priceband = iq.getPriceBand(TxtpriceBand.Text)
            buyerAccount.MustChangePassword = True

            buyerAccount.update(errormessages)
            buyerAccount.User.update(errormessages)

            Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)


            Dim baseurl As String = ConfigurationManager.AppSettings("BaseURL")

            Dim url$
            url$ = baseurl & "/aspx/signin.aspx"

            tags.Add("hostname", buyerAccount.SellerChannel.DisplayName(buyerAccount.Language))
            tags.Add("email", buyerAccount.User.Email)
            tags.Add("password", pw$)
            tags.Add("firstname", Split(buyerAccount.User.RealName, " ")(0))
            tags.Add("url", url)
            tags.Add("extratext", If(baseurl = "http://uat.hpiquote.net", "<p>Please note this is a login for test purposes</p>", ""))
            tags.Add("mfr", buyerAccount.mfrCode)
            tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English))

            Dim em As List(Of String) = New List(Of String)
            If chkWelcome.Checked Then
                SendEmail(buyerAccount.User.Email, "WelcomeEmail.htm", tags, buyerAccount.Language, em, False)
            End If

            If em.Count = 0 Then
                Response.Write("<script language='JavaScript'>window.parent.closeIFrame('" & Request("frameid") & "');</script>")
            End If

        Else
            'dud email address
            TxtEmail.BackColor = Drawing.Color.Red
        End If

    End Sub

    Protected Sub BtnCancel_Click(sender As Object, e As EventArgs) Handles BtnCancel.Click

    End Sub
End Class