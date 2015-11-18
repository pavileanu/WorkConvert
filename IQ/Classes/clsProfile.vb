

Public Class clsProfile


    'for each labelled profile (eg. 'SaveQuote') - we store an array of stats for every recursion level - higher levels (lower numbers) include calls to deeper ones
    Private Structure stcStats

        Public TotalTicks As Int64
        Public Mark As Double
        Public Calls As Int64
        Public Min As Int64
        Public Max As Int64

    End Structure

    'Carries a set of stats for each level of recursion - ie.. wh

    Private stats() As stcStats
    Private depth As Integer
    Private maxDepth As Integer

    Public Sub PMark()

        Me.depth = Me.depth + 1
        If Me.depth > Me.maxDepth Then Me.maxDepth = Me.depth 'how 'deep' did we go (recusion depth)
        If Me.depth > 40 Then Stop
        If Me.depth > UBound(Me.stats) Then ReDim Preserve Me.stats(UBound(Me.stats) + 5)
        Me.stats(Me.depth).Mark = System.Diagnostics.Stopwatch.GetTimestamp

    End Sub

    Public Sub PAccumulate()

        Dim et As Int64  'elapsed time (in stopwatch ticks)
        With Me.stats(Me.depth)
            et = System.Diagnostics.Stopwatch.GetTimestamp - .Mark
            .TotalTicks += et
            .Calls += 1
            If et < .Min Or .Min = 0 Then .Min = et
            If et > .Max Then .Max = et
        End With
        Me.depth = Me.depth - 1

        'If Me.depth < 0 Then Stop

    End Sub

    Public Function Results(name) As List(Of TableRow)

        'returna list of tabel rows - one for each recusion depth level (for the sub/fuction being profiled)

        Results = New List(Of TableRow)
        Dim tr As TableRow
        Dim td As TableCell
        For i = 1 To Me.maxDepth
            tr = New TableRow
            Results.Add(tr)

            With stats(i)
                Dim totalMs As Integer
                totalMs = .TotalTicks / Stopwatch.Frequency * 1000

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = name & " depth" & i  'Procedure lable and recursion depth

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = Format(.Calls, "##")

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = Format(totalMs, "##")

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = Format(totalMs / .Calls * 1000, "##")

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = Format(.Min / Stopwatch.Frequency * 1000000, "##")

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = Format(.Max / Stopwatch.Frequency * 1000, "##")

            End With
        Next

    End Function

    Public Sub New()
        ReDim Me.stats(5)
    End Sub

End Class
