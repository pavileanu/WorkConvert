Public Class clsManipPrune
    Inherits ManipulationMethod

    Public Overloads Function PerformAction() As String
        wipeCachedDataView(SourcePath, LoginId)
        iq.Prune(SourcePath, LoginId)

        Return ""
    End Function

    Public Overloads Function UndoAction() As String
        wipeCachedDataView(SourcePath, LoginId)
        Dim pid As System.Collections.Generic.KeyValuePair(Of Integer, clsPrune) = iq.Branches(SourceBranch.ID).Prunes.Where(Function(p) p.Value.Path = SourcePath AndAlso p.Value.ChannelID.value Is DBNull.Value).FirstOrDefault()
        If pid.Value IsNot Nothing Then iq.Branches(SourceBranch.ID).Prunes.Remove(pid.Key) Else Return "Prune not found!"

        Dim sql$
        sql$ = "DELETE FROM [prune] WHERE path=" + dataAccess.da.SqlEncode(SourcePath) + " AND fk_channel_id is NULL"
        dataAccess.da.DBExecutesql(sql, False)

        MyBase.UndoAction()
        Return "Success"
    End Function
End Class
