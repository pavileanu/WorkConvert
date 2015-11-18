Public Class clsVisibility

    Public path As String
    Public hideReasonList As List(Of String)
    Public branch As clsBranch

    Public Sub New(branch As clsBranch, path As String, HideReasonList As List(Of String))
        Me.path = path
        Me.hideReasonList = HideReasonList
        Me.branch = branch

    End Sub

End Class
