public class ManipulationMethod
{
    public ManipulationMethod()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        SourceBranchId = new Nullable<int>();
        TargetBranchId = new Nullable<int>();
        errormessages = new List<string>();

    }
    public int? SourceBranchId { get; set; }
    public int? TargetBranchId { get; set; }
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public UInt64 LoginId { get; set; }
    public int? AuditId { get; set; }
    internal List<string> errormessages; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    public clsBranch TargetBranch
    {
        get
        {
            if (TargetBranchId == null)
            {
                if (TargetPath == null)
                {
                    return null;
                }
                else
                {
                    return iq.Branches(System.Convert.ToInt32(TargetPath.Split('.')[TargetPath.Split('.').Length - 1]));
                }
            }
            else
            {
                return iq.Branches(TargetBranchId);
            }
        }
    }
    public clsBranch SourceBranch
    {
        get
        {
            if (SourceBranchId == null)
            {
                if (SourcePath == null)
                {
                    return null;
                }
                else
                {
                    return iq.Branches(System.Convert.ToInt32(SourcePath.Split('.')[SourcePath.Split('.').Length - 1]));
                }
            }
            else
            {
                return iq.Branches(SourceBranchId);
            }
        }
    }


    public string PerformAction()
    {

    }

    public string UndoAction()
    {
        AuditLog.Instance.MarkUndone(AuditId, LoginId, "Undo" + this.GetType().Name, errormessages, ""); //Add referer
    }

}