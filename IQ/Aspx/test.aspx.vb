Public Class Test
    Inherits System.Web.UI.Page

    Private Sub Page_Init(sender As Object, e As System.EventArgs) Handles Me.Init

        'Dim tb As New TextBox
        'tb.BackColor = Drawing.Color.Green
        'Form.Controls.Add(tb)


    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        '  If Not IsPostBack Then
        Dim tb As New TextBox
        tb.BackColor = Drawing.Color.Green
        Form.Controls.Add(tb)

        ' End If

    End Sub

    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Debug.Print(TextBox1.Text)

    End Sub
End Class