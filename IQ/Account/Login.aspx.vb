Public Class Login
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        RegisterHyperLink.NavigateUrl = "Register.aspx?ReturnUrl=" + HttpUtility.UrlEncode(Request.QueryString("ReturnUrl"))
    End Sub


    Protected Sub UserName_TextChanged(sender As Object, e As EventArgs)

    End Sub

    Protected Sub LoginButton_Click(sender As Object, e As EventArgs)

    End Sub
End Class