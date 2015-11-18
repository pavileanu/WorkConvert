Option Explicit On
Option Strict On

Public Class PasswordReset
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim u As clsUser = iq.Users(CInt(Request("uid")))

        Dim lit As Literal = New Literal
        Pnl.Controls.Add(lit)
        lit.Text = String.Format("<div id='whichAccount'>{0}<br/><br/></div>", Xlt("Choose an account to reset its password.<br/>An email will be sent to you with instructions on how to complete the reset.", u.Accounts.Values(0).Language))

        Dim al = From ac In u.Accounts.Values Order By ac.displayName(u.Accounts.Values(0).Language) 'Where ac.SellerChannel.BusinessName.Contains("estcoast")
        If Not al.Any Then
            LblFailed.Text &= UiTrans("There is no account for ") & u.Email
            LblFailed.Visible = True
        Else

            For Each account In al 'as clsAccount In u.Accounts.Values where 
                'LblFailed.Text &= UiTrans("A new password has been sent to ") & u.Email & " " & UiTrans("please check your email.")

                Dim pnlIn As Panel = New Panel
                pnlIn.CssClass = "resetOuter"
                Pnl.Controls.Add(pnlIn)

                Dim btn As Button = New Button
                pnlIn.Controls.Add(btn)
                btn.Text = Xlt("Reset", account.Language)
                btn.Attributes("ac") = account.ID.ToString

                lit = New Literal
                lit.Text = "<div class='resetLabel'>" & account.SellerChannel.DisplayName(u.Accounts.Values(0).Language)
                lit.Text &= "</div>"
                pnlIn.Controls.Add(lit)

                lit = New Literal
                If account.Manufacturer = Manufacturer.HPI Then
                    lit.Text = " <img src='../images/HPI-Logo.jpg' height='18'/>"
                ElseIf account.Manufacturer = Manufacturer.HPE Then
                    lit.Text = " <img src='../images/HPE-Logo.jpg' height='18'/>"
                End If
                pnlIn.Controls.Add(lit)

                AddHandler btn.Click, AddressOf resetAccount

            Next
        End If

    End Sub

    Protected Sub resetAccount(sender As Object, e As EventArgs)

        Dim b As Button = CType(sender, Button)

        Dim account As clsAccount = iq.Accounts(CInt(b.Attributes("ac")))

        Dim em = account.ResetPassword()
        If em.Count > 0 Then
            For Each m In em
                LblFailed.Text &= (m & "|")
            Next
            LblFailed.Visible = True
        Else
            LblFailed.Text = UiTrans("A new password has been sent to ") & account.User.Email & " " & UiTrans("please check your email.")
            LblFailed.BackColor = Drawing.Color.Green
            LblFailed.Visible = True

            Response.AddHeader("REFRESH", "5;url=signin.aspx")
            'Response.Redirect("signin.aspx")

        End If

    End Sub
End Class