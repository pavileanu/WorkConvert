Public Class Loading
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If clsIQ.IsLoaded Then Response.Redirect("signin.aspx")

        Dim a = iq.Quotes.Count ' do something to trigger the load

        If Request("debug") IsNot Nothing Then lblStatus.Visible = True

        If Request("path") IsNot Nothing Then hidref.Value = Request("Path")
        If Request.UrlReferrer IsNot Nothing Then hidref.Value = Request.UrlReferrer.AbsoluteUri

        Dim css As New Literal
        Dim stylesheet As String = Nothing

        If Request("mfr") Is Nothing Then
            stylesheet = "channelcentral"
        Else
            stylesheet = String.Format("Site-{0}", Request("mfr"))
        End If

        If Not String.IsNullOrEmpty(stylesheet) Then
            css.Text = String.Format("<link href='{0}Styles/{1}.css' rel='stylesheet' type='text/css' />", ResolveUrl("~/"), stylesheet)
            Page.Header.Controls.Add(css)
        End If

        '       Dim d As String() = New String(clsIQ.messages.Count) {}
        '        clsIQ.messages.CopyTo(d)
        'lblStatus.Text = String.Join("<br>", d.Reverse())
        'End If

        'progressBar.Width = New Unit(CDbl(clsIQ.messages.Count) / 61 * CDbl(progressPanel.Width.Value))
    End Sub

End Class