public class clsVisibility
{

    public string path;
    public List<string> hideReasonList;
    public clsBranch branch;

    public clsVisibility(clsBranch branch, string path, List<string> HideReasonList)
    {
        this.path = path;
        this.hideReasonList = HideReasonList;
        this.branch = branch;

    }

}