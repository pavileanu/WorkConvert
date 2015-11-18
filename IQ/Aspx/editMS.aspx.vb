Public Class edit
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim q$
        q$ = Split(Request.RawUrl, "?")(1)

        '     Response.Write("<script language='javaScript'>alert('hello');</script>")

        Dim script$
        script$ = "embed('/editor/editor.aspx?" & q$ & "&panelID=ctl00_MainContent_EditPanel','ctl00_MainContent_EditPanel',false,false);"

        Dim lit As Literal = New Literal
        lit.Text = "<script language='JavaScript'>" & script$ & "</script>;"
        Page.Controls.Add(lit)

    End Sub

End Class