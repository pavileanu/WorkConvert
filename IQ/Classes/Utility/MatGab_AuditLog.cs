using System.Threading.Tasks;

public class AuditLog
{
	private static AuditLog _instance;
	public static AuditLog Instance {
		get {
			if (_instance == null) {
				_instance = new AuditLog();
			}
			return _instance;
		}
	}

	private Queue<clsAuditEntry> log = new Queue<clsAuditEntry>();

	public Action<clsAuditEntry> TAction = (clsAuditEntry cae) => { cae.Save(); };

	private System.Threading.Thread thisThread;

	//Default Add for page loads in clsPageLogging
	public void Add(UInt64 lid, string Action, string SourcePath, string TargetPath, List<string> errormessages, Exception ex, string PageName, string SourceURL, double TimeToLoad, string HttpRequestMethod,
	string UrlReferrer)
	{
		object t = Task.Factory.StartNew(() => TAction(new clsAuditEntry {
			lid = lid,
			Action = Action,
			SourcePath = SourcePath,
			TargetPath = TargetPath,
			DateTime = DateTime.Now,
			Message = string.Join(",", errormessages),
			SecondaryMessage = ex != null ? ex.Message : string.Empty,
			SourceURL = SourceURL,
			TimeToLoad = TimeToLoad,
			PageName = PageName,
			HttpRequestMethod = HttpRequestMethod,
			UrlReferrer = UrlReferrer
		}));
	}

	public void Add(UInt64 lid, string Action, List<string> errormessages, string UrlReferrer)
	{
		object t = Task.Factory.StartNew(() => TAction(new clsAuditEntry {
			lid = lid,
			Action = Action,
			DateTime = DateTime.Now,
			Message = string.Join(",", errormessages),
			UrlReferrer = UrlReferrer
		}));
	}

}
