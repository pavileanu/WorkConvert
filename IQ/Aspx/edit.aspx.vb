Public Class edit1
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim q$
        q$ = Request.RawUrl
        q$ = Split(q$, "?")(1)

        Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))
        Dim EditHeaders As Dictionary(Of String, clsEditHeader)
        EditHeaders = New Dictionary(Of String, clsEditHeader)

        iq.sesh(lid, "editHeaders") = EditHeaders

        Dim asPage As List(Of String)  'Switches individual rows between Page And ListRow mode - things default to list, if their (path) is in here they're in page mode.
        If Not iq.SeshContains(lid, "asPage") Then
            asPage = New List(Of String)
            iq.sesh(lid, "asPage") = asPage
        End If

        'REMOVE THE lid (not sure why ??)
        Dim B() = Split(q$, "&")
        For I = 0 To UBound(B)
            If UCase(Left(B(I), 4)) = "LID=" Then B(I) = "FGFG=0"
        Next
        q$ = Join(B, "&")

        If drpLanguage.SelectedIndex > -1 Then
            q$ = q$ & "&language=" & drpLanguage.SelectedValue
        End If

        'Response.Write("<script language='javaScript'>alert('hello');</script>")
        'drop the lid (becuase Embed adds it)

        Dim script$

        ' embed(url, divID, append, sendNVPs) {
        script$ = "embed('../editor/editor.aspx?" & q$ & "','EditPanel',false,false);"  'will *REPLACE* EditPanel
       

        Dim lit As Literal = New Literal
        lit.Text = "<script language='JavaScript'>" & script$ & "</script>;"
        Page.Controls.Add(lit)

        If Not Page.IsPostBack Then
            drpLanguage.DataSource = iq.ActiveLanguages.Values
            drpLanguage.DataTextField = "LocalName"
            drpLanguage.DataValueField = "ID"
            drpLanguage.DataBind()
        End If


    End Sub

    Protected Sub drpLanguage_SelectedIndexChanged(sender As Object, e As EventArgs)
        Dim selection As String = String.Empty
    End Sub
End Class