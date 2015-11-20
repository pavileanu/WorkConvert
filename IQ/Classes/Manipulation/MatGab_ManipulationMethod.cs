public class ManipulationMethod
{
	private int? SourceBranchId {
		get { return m_SourceBranchId; }
		set { m_SourceBranchId = Value; }
	}
	private int? m_SourceBranchId;
	private int? TargetBranchId {
		get { return m_TargetBranchId; }
		set { m_TargetBranchId = Value; }
	}
	private int? m_TargetBranchId;
	private string SourcePath {
		get { return m_SourcePath; }
		set { m_SourcePath = Value; }
	}
	private string m_SourcePath;
	private string TargetPath {
		get { return m_TargetPath; }
		set { m_TargetPath = Value; }
	}
	private string m_TargetPath;
	private UInt64 LoginId {
		get { return m_LoginId; }
		set { m_LoginId = Value; }
	}
	private UInt64 m_LoginId;
	private int? AuditId {
		get { return m_AuditId; }
		set { m_AuditId = Value; }
	}
	private int? m_AuditId;

	internal List<string> errormessages = new List<string>();
	private clsBranch TargetBranch {
		get {
			if (TargetBranchId == null) {
				if (TargetPath == null) {
					return null;
				} else {
					return iq.Branches((int)TargetPath.Split(".")(TargetPath.Split(".").Length - 1));
				}
			} else {
				return iq.Branches(TargetBranchId);
			}
		}
	}
	private clsBranch SourceBranch {
		get {
			if (SourceBranchId == null) {
				if (SourcePath == null) {
					return null;
				} else {
					return iq.Branches((int)SourcePath.Split(".")(SourcePath.Split(".").Length - 1));
				}
			} else {
				return iq.Branches(SourceBranchId);
			}
		}
	}


	private string PerformAction()
	{

	}

	private string UndoAction()
	{
		AuditLog.Instance.MarkUndone(AuditId, LoginId, "Undo" + this.GetType().Name, errormessages, "");
		//Add referer?
	}

}
