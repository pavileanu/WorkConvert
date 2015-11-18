Option Strict On

Public Class Universal
    Inherits clsPageLogging

    Protected countryList As ListBox = New ListBox()

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim mfr As String = Nothing
        Dim cnames As Boolean = False    ' TODO - temporary flag to allow us to switch between having and not having iquote.hp(e).com CNAMEs set up. Will always need to be true soon...

        ' Create a case-insensitive dictionary for the Request parameters
        Dim requestParams As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.InvariantCultureIgnoreCase)
        For Each key As String In Request.QueryString
            If Not key Is Nothing Then requestParams.Add(key, Request.QueryString(key).ToLower())
        Next

        ' Try and get the mfr
        If requestParams.ContainsKey("mfr") Then
            If requestParams("mfr") = "hpe" Then
                mfr = "hpe"
            ElseIf requestParams("mfr") = "hpi" Then
                mfr = "hpi"
            End If
        End If

        ' We might be able to work out the mfr from the request host
        If String.IsNullOrEmpty(mfr) Then
            mfr = InferUniversalManufacturer(Request)
        End If

        If String.IsNullOrEmpty(mfr) Then

            ' Don't have the mfr - display UI to retrieve it
            If cnames Then
                BuildMfrSelector("hpi", panelHPI, "Desktops, Laptops, Workstations", "http://iquote.<b>hp</b>.com")
                BuildMfrSelector("hpe", panelHPE, "Servers, Storage, Networking", "http://iquote.<b>hpe</b>.com")
                panelBanner.Visible = True
            Else
                BuildMfrSelector("hpi", panelHPI, "Desktops, Laptops, Workstations", Nothing)
                BuildMfrSelector("hpe", panelHPE, "Servers, Storage, Networking", Nothing)
            End If


            univSubtitle.Text = Xlt("HP Separation Update:", English)
            univInstructions.Text = Xlt("Use the links below to access iQuote Universal for the company of your choice.&nbsp;&nbsp;If you already had an iQuote Universal login prior to August 1st 2015, your login credentials have been copied to both instances as separate accounts.&nbsp;&nbsp;Any saved quotes have been moved to the appropriate instance based on the product type.", English)

        Else

            ' We have all we need - redirect
            Redirect(mfr)

        End If

    End Sub

    Private Sub BuildMfrSelector(mfr As String, panel As Panel, description As String, url As String)

        Dim box As New Panel()
        box.CssClass = "HostList"

        Dim label As New Label
        label.Text = description
        label.CssClass = "mfrDescription"
        box.Controls.Add(label)

        Dim img As New Image
        img.ImageUrl = String.Format("/images/{0}-Logo.jpg", mfr)
        box.Controls.Add(img)

        panel.Controls.Add(box)

        Dim lit As New Literal
        lit.Text = "<br/><br/><br/>"
        panel.Controls.Add(lit)

        Dim button As New LinkButton()

        If url Is Nothing Then
            'button.Text = Xlt("Enter", English)
            button.Text = "Enter"
        Else
            'button.Text = Xlt(url, English)
            button.Text = url
        End If

        button.Attributes.Add("mfr", mfr)
        button.CssClass = "mfrButton"
        AddHandler button.Click, AddressOf OnMfrSelected
        panel.Controls.Add(button)

    End Sub

    Private Sub OnMfrSelected(sender As Object, e As System.EventArgs)

        If sender.GetType() Is GetType(LinkButton) Then

            Response.Redirect(BuildPostbackUrl(CType(sender, LinkButton)))

        End If

    End Sub

    Private Function BuildPostbackUrl(button As LinkButton) As String

        ' Build a URL to postback to this page with the details we have so far
        Dim url As String = Request.Path.ToString()

        If Not button.Attributes("mfr") Is Nothing Then
            url = String.Format("{0}?mfr={1}", url, button.Attributes("mfr"))
        End If

        Return url

    End Function

    Private Sub Redirect(mfr As String)

        Response.Redirect(String.Format("Signin.aspx?Universal&mfr={0}", mfr))

    End Sub

End Class