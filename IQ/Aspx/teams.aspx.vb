Public Class teams
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'returns a delimited set of ID^Team] s matching the Channel request parameter

        Dim lit As Literal
        lit = New Literal
        lit.Text = "!Begin"
        Form.Controls.Add(lit)

        Dim t As clsTeam
        For Each t In iq.Channels(Request("channel")).Teams.Values
            lit = New Literal
            lit.Text = t.ID & "^" & t.Name & "]"
            Form.Controls.Add(lit)
        Next

        lit = New Literal
        lit.Text = "!End"
        Form.Controls.Add(lit)

    End Sub

End Class