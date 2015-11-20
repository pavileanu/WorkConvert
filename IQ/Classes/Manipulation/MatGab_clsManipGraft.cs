using dataAccess;

public class clsManipGraft : ManipulationMethod
{

	public string PerformAction()
	{
		TargetBranch.Graft(SourceBranch, LoginId, "", errormessages);
		//Creates the new graft

		//we must delete the cached dataview - otherwise we won't see the change
		wipeCachedDataView(TargetPath, LoginId);

		base.PerformAction();

		//if the graft fails, we put an error in the response which the JS will place into the tree
		return string.Join(",", errormessages);
	}

	public string UndoAction()
	{
		TargetBranch.childBranches.Remove(SourceBranch.ID);

		object sql;
		sql = "DELETE FROM [graft] WHERE fk_branch_id_target=" + TargetBranch.ID.ToString() + " AND fk_branch_id_source=" + SourceBranch.ID.ToString();
		da.DBExecutesql(sql, false);
		//return the ID of the graft record



		wipeCachedDataView(TargetPath, LoginId);

		base.UndoAction();

		return string.Join(",", errormessages);
	}
}
