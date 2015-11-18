Public Class Profile
    Inherits clsPageLogging

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'Outputs the current code profile - genertaed by calls to Pmark() and pAcc()  - (profile, mark and profile accumulate)

        Dim tb As New Table
        Form.Controls.Add(tb)
        tb.BorderStyle = BorderStyle.Solid
        tb.BorderWidth = 1


        tb.Controls.Add(headerrow())

        For Each k In Profiling.Profile.Keys
            For Each r In Profiling.Profile(k).Results(k)
                tb.Controls.Add(r)
            Next
        Next

    End Sub

    Private Function HeaderRow() As TableHeaderRow

        HeaderRow = New TableHeaderRow
        Dim th As TableHeaderCell
        HeaderRow.BackColor = Drawing.Color.CornflowerBlue
        th = New TableHeaderCell
        th.Text = "Name/rDepth"
        HeaderRow.Controls.Add(th)

        th = New TableHeaderCell
        th.Text = "Calls"
        HeaderRow.Controls.Add(th)

        th = New TableHeaderCell
        th.Text = "Time (ms)"
        HeaderRow.Controls.Add(th)

        th = New TableHeaderCell
        th.Text = "Avg Time (µs)"
        HeaderRow.Controls.Add(th)


        th = New TableHeaderCell
        th.Text = "Min Time (µs)"
        HeaderRow.Controls.Add(th)

        th = New TableHeaderCell
        th.Text = "Max Time (ms)"
        HeaderRow.Controls.Add(th)

    End Function

    Protected Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Profiling.Profile.Clear()

    End Sub
End Class