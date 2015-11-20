using System.Threading.Tasks;


public class AuditLog
{
    public AuditLog()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        log = new Queue<clsAuditEntry>();

    }
    private static AuditLog _instance;
    public static AuditLog Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AuditLog();
            }
            return _instance;
        }
    }

    private Queue<clsAuditEntry> log; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
    public Action<clsAuditEntry> TAction = cae =>
    {
        cae.Save();
    };


    private System.Threading.Thread thisThread;

    //Default Add for page loads in clsPageLogging
    public void Add(UInt64 lid, string Action, string SourcePath, string TargetPath, List<string> errormessages, Exception ex, string PageName, string SourceURL, double TimeToLoad, string HttpRequestMethod, string UrlReferrer)
    {
        System.Threading.Tasks.Task t = Task.Factory.StartNew(() => TAction[new clsAuditEntry() { lid = lid, Action = Action, SourcePath = SourcePath, TargetPath = TargetPath, DateTime = DateTime.Now, Message = string.Join(",", errormessages), SecondaryMessage = (ex != null ? ex.Message : string.Empty), SourceURL = SourceURL, TimeToLoad = TimeToLoad, PageName = PageName, HttpRequestMethod = HttpRequestMethod, UrlReferrer = UrlReferrer }]);
    }

    public void Add(UInt64 lid, string Action, List<string> errormessages, string UrlReferrer)
    {
        System.Threading.Tasks.Task t = Task.Factory.StartNew(() => TAction[new clsAuditEntry() { lid = lid, Action = Action, DateTime = DateTime.Now, Message = string.Join(",", errormessages), UrlReferrer = UrlReferrer }]);
    }

    public void Add(AuditType Type, string Message, string PageName, UInt64? lid = null)
    {
        System.Threading.Tasks.Task t = Task.Factory.StartNew(() => TAction[new clsAuditEntry() { AuditType = Type.ToString(), DateTime = DateTime.Now, PageName = PageName, Message = Message, lid = lid.Value }]);
    }

    //Sub sendQueue()
    //    'Make this async with more time - ML
    //    'Do
    //    While log.Count > 0
    //        Try
    //            Dim a = log.Peek()
    //            If a IsNot Nothing Then
    //                If a.Save() And log.Count > 0 Then log.Dequeue()
    //            End If
    //            System.Threading.Thread.Sleep(100)
    //        Catch ex As Exception
    //            ErrorLog.Add(ex)
    //        End Try
    //    End While

    //    'System.Threading.Thread.Sleep(10000)
    //    'Loop
    //End Sub

    public void MarkUndone(int AuditId, UInt64 lid, string Action, List<string> errormessages, string URLReferer)
    {
        System.Threading.Tasks.Task t = Task.Factory.StartNew(() => TAction[new clsAuditEntry() { AuditId = AuditId, DBMethod = 1, ActionUndone = true }]);
        Add(lid, Action, errormessages, URLReferer);
    }
}
public enum AuditType
{
    Debug,
    @Error,
    Warning,
    Information,
    Editor
}