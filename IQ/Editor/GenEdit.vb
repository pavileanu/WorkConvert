Public Module GenEdit

    Public Function NullID(obj As clsValidation) As String
        If obj Is Nothing Then
            Return "null"
        Else
            Return obj.ID.ToString
        End If

    End Function

    Public Function Plural(n$) As String

        If LCase(Right$(n$, 1)) = "y" Then
            Plural = Left$(n$, Len(n$) - 1) & "ies"
        ElseIf LCase(Right$(n$, 2) = "us") Then
            Plural = n$ & "es"
        Else
            Plural = n$ & "s"
        End If

    End Function

    Public Function EmptyPanel(id As String) As Panel

        Dim pnl As Panel = New Panel
        pnl.ID = id '& "." & Rnd(1).ToString  'TERRIBLE TODO CHANGE

        'Dim lbl As Label = New Label
        'lbl.ForeColor = Drawing.Color.White
        'lbl.BackColor = Drawing.Color.Blue
        'lbl.Text = pnl.ID
        'pnl.Controls.Add(lbl)

        Return pnl

    End Function

End Module
