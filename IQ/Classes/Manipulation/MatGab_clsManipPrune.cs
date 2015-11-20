public class clsManipPrune : ManipulationMethod
{

	public string PerformAction()
	{
		wipeCachedDataView(SourcePath, LoginId);
		iq.Prune(SourcePath, LoginId);

		return "";
	}

	public string UndoAction()
	{
		wipeCachedDataView(SourcePath, LoginId);
		System.Collections.Generic.KeyValuePair<int, clsPrune> pid = iq.Branches(SourceBranch.ID).Prunes.Where(p => p.Value.Path == SourcePath && object.ReferenceEquals(p.Value.ChannelID.value, DBNull.Value)).FirstOrDefault();
		if (pid.Value != null)
			iq.Branches(SourceBranch.ID).Prunes.Remove(pid.Key);
		else
			return "Prune not found!";

		object sql;
		sql = "DELETE FROM [prune] WHERE path=" + dataAccess.da.SqlEncode(SourcePath) + " AND fk_channel_id is NULL";
		dataAccess.da.DBExecutesql(sql, false);

		base.UndoAction();
		return "Success";
	}
}
