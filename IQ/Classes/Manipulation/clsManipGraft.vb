Imports dataAccess

Public Class clsManipGraft
    Inherits ManipulationMethod

    Public Overloads Function PerformAction() As String
        TargetBranch.Graft(SourceBranch, LoginId, "", errormessages)  'Creates the new graft

        'we must delete the cached dataview - otherwise we won't see the change
        wipeCachedDataView(TargetPath, LoginId)

        MyBase.PerformAction()

        'if the graft fails, we put an error in the response which the JS will place into the tree
        Return String.Join(",", errormessages)
    End Function

    Public Overloads Function UndoAction() As String
        TargetBranch.childBranches.Remove(SourceBranch.ID)

        Dim sql$
        sql$ = "DELETE FROM [graft] WHERE fk_branch_id_target=" + TargetBranch.ID.ToString() + " AND fk_branch_id_source=" + SourceBranch.ID.ToString()
        da.DBExecutesql(sql, False)  'return the ID of the graft record



        wipeCachedDataView(TargetPath, LoginId)

        MyBase.UndoAction()

        Return String.Join(",", errormessages)
    End Function
End Class
