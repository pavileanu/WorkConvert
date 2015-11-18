Imports dataAccess

Public Class clsExclude

    'Stores a multually exclusive SET of branches - EG. UDIMM/RDIMM

    'It tempting to do this by path - but that would require an entry for every system in a family - this way, the excludes work on the (Grafted) copies of the option pranches (under every system)

    Property ID As Integer
    Public havingAnyOf As List(Of clsBranch)   'having any of these
    Public excludesAllOf As List(Of clsBranch) 'excludes all of these
    Property Reason As String

    Public Sub New(id As Integer, Having As clsBranch, excludes As clsBranch, reason As String)

        Me.havingAnyOf = Having.Descendants
        Me.excludesAllOf = excludes.Descendants
        Me.Reason = reason
        iq.Excludes.Add(id, Me)

    End Sub

    Public Sub New(Having As clsBranch, excludes As clsBranch, reason As String)

        Me.havingAnyOf = Having.Descendants
        Me.excludesAllOf = excludes.Descendants
        Me.Reason = reason
        Me.ID = da.DBExecutesql("INSERT INTO [exclude] (fk_branch_id_having,fk_branch_id_excludes,reason) VALUES(" & Having.ID & "," & excludes.ID & "," & da.SqlEncode(reason) & ");", True)

        iq.Excludes.Add(ID, Me)

    End Sub

    Public Function Delete()


        iq.Excludes.Remove(Me.ID)
        da.DBExecutesql("Delete from exclude where id=" & Me.ID)

        Delete = ""

    End Function


End Class
