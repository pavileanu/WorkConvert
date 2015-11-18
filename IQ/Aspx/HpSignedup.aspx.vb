Imports System.Globalization

Public Class HpSignedup
    Inherits System.Web.UI.Page
    Private selectedChannel As clsChannel
    Private selectedlang As String
    Private selectedCountry As String

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        If Request.QueryString("lang") IsNot Nothing AndAlso Request.QueryString("mfr") IsNot Nothing Then

            Dim langarray() As String = Split(Request.QueryString("lang"), "|")
            selectedlang = langarray(0)
            selectedCountry = langarray(1)
            labelCountry.Text = selectedCountry

            Dim url = ConfigurationManager.AppSettings("BaseURL") & "/Aspx/SignIn.aspx?mfr=" & Request.QueryString("mfr")

            If Request.QueryString("existingAccount") = "Y" Then
                litRegistered.Text = String.Format("You already have a relevant {0} iQuote Universal account. Go <a href='{1}'>here</a> to log in or to reset your password.", Request.QueryString("mfr"), url)
            Else
                litRegistered.Text = String.Format("You have successfully registered. Please check your email for a welcome message containing your password and details of how to <a href='{0}'>log in</a>.", url)
            End If
        Else
            Response.Redirect("SignIn.aspx")
        End If

    End Sub

End Class