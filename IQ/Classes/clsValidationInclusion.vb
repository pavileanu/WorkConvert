Imports dataAccess

Public Class clsValidationInclusion
    Implements i_Editable
    Public Property ID As Integer
    Public Property MajorCode As String
    Public Property MinorCode As String
    Public Property InclusionType As enumInclusionType

    Public Sub New(ID As Integer, MajorCode As String, MinorCode As String, InclusionType As enumInclusionType)

        Me.MajorCode = MajorCode
        Me.MinorCode = MinorCode
        Me.InclusionType = InclusionType
        Me.ID = ID
        iq.ValidationInclusions.Add(ID, Me)

    End Sub

    Public Sub New(MajorCode As String, MinorCode As String, InclusionType As enumInclusionType)

        Me.MajorCode = MajorCode
        Me.MinorCode = MinorCode
        Me.InclusionType = InclusionType

        Me.ID = da.DBExecutesql("INSERT INTO validationInclusion VALUES (" & da.SqlEncode(Me.MajorCode) & "," & da.SqlEncode(Me.MinorCode) & "," & da.SqlEncode(Me.InclusionType.ToString) & ")", True)

        iq.ValidationInclusions.Add(ID, Me)
    End Sub

    Public Function displayName(lang As clsLanguage) As String Implements i_Editable.displayName
        displayName = String.Format("{0} - {1} - {2}", Me.MajorCode, Me.MinorCode, Me.InclusionType.ToString)
    End Function

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete
        iq.ValidationInclusions.Remove(Me.ID)

        Try
            dataAccess.da.DBExecutesql("DELETE FROM validationinclusion where id=" & Me.ID)
        Catch ex As Exception
            errorMessages.Add(ex.Message.ToString)
        End Try
    End Sub

    Public Function insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert
        Return New clsValidationInclusion(MajorCode, MinorCode, InclusionType)
    End Function

    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        If Me.ID < 0 Then Stop

        Dim sql$
        sql$ = "update [ValidationInclusions] "
        sql$ &= "SET majorcode=" & da.SqlEncode(Me.MajorCode) & ",minorcode=" & da.SqlEncode(Me.MinorCode)
        sql$ &= ",inclusiontype=" & da.SqlEncode(Me.InclusionType.ToString)
        sql$ &= " WHERE id=" & Me.ID

        da.DBExecutesql(sql$, False)

    End Sub

End Class

Public Enum enumInclusionType
    Validated = 0
    Unvalidated = 1
End Enum

