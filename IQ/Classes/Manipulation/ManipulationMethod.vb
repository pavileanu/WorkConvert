Public Class ManipulationMethod
    Property SourceBranchId As Integer? = New Nullable(Of Integer)
    Property TargetBranchId As Integer? = New Nullable(Of Integer)
    Property SourcePath As String
    Property TargetPath As String
    Property LoginId As UInt64
    Property AuditId As Integer?
    Friend errormessages As List(Of String) = New List(Of String)()

    ReadOnly Property TargetBranch As clsBranch
        Get
            If TargetBranchId Is Nothing Then
                If TargetPath Is Nothing Then
                    Return Nothing
                Else
                    Return iq.Branches(CInt(TargetPath.Split(".")(TargetPath.Split(".").Length - 1)))
                End If
            Else
                Return iq.Branches(TargetBranchId)
            End If
        End Get
    End Property
    ReadOnly Property SourceBranch As clsBranch
        Get
            If SourceBranchId Is Nothing Then
                If SourcePath Is Nothing Then
                    Return Nothing
                Else
                    Return iq.Branches(CInt(SourcePath.Split(".")(SourcePath.Split(".").Length - 1)))
                End If
            Else
                Return iq.Branches(SourceBranchId)
            End If
        End Get
    End Property


    Function PerformAction() As String

    End Function

    Function UndoAction() As String
        AuditLog.Instance.MarkUndone(AuditId, LoginId, "Undo" + Me.GetType().Name, errormessages, "") 'Add referer?
    End Function

End Class
