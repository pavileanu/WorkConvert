Public Class help
    Inherits System.Web.UI.Page


    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim cl As GK.OneTimeTokenClient
        cl = New GK.OneTimeTokenClient

        Dim t As Table = New Table
        t.CellPadding = 3
        '   t.BorderWidth = 1


        t.Attributes("class") = "wsHelpTable"
        '   t.Attributes("style") = "border-collapse:collapse;cellpadding:2px;"
        Form.Controls.Add(t)
        t.Controls.Add(MakeTHR("Name,Required,Notes,Example(value),min length,Max length,RegEx", "", ""))

        For Each i In cl.Help()
            t.Controls.Add(helpTableRow(i))
        Next

    End Sub

    Public Function helpTableRow(i As GK.clsName)

        helpTableRow = New TableRow

        Dim td As TableCell = New TableCell
        helpTableRow.controls.add(td)
        td.Text = i.name
        
        td = New TableCell
        td.BorderStyle = BorderStyle.Solid
        td.BorderWidth = 1

        helpTableRow.controls.add(td)
        td.Text = i.Required.ToString

        td = New TableCell
        td.BorderStyle = BorderStyle.Solid
        td.BorderWidth = 1

        helpTableRow.controls.add(td)
        td.Text = i.Notes

        td = New TableCell
        helpTableRow.controls.add(td)
        td.BorderStyle = BorderStyle.Solid
        td.BorderWidth = 1

        td.Text = i.Example

        td = New TableCell
        td.BorderStyle = BorderStyle.Solid
        td.BorderWidth = 1

        helpTableRow.controls.add(td)
        td.Text = i.MinLength

        td = New TableCell
        td.BorderStyle = BorderStyle.Solid
        td.BorderWidth = 1

        helpTableRow.controls.add(td)
        td.Text = i.MaxLength

        td = New TableCell
        td.BorderStyle = BorderStyle.Solid
        td.BorderWidth = 1

        helpTableRow.controls.add(td)
        td.Text = i.RegEx

    End Function

End Class