public class ErrorLog
{
	private static LoggingList<string> ErrorList = new LoggingList<string>();
	public static void Add(Exception ex)
	{
		if (ex != null) {
			ErrorList.Add(string.Format("{0}\\r\\n{1}\\r\\n{2}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty, ex.StackTrace));
			try {
				dataAccess.da.DBExecutesql(string.Format("INSERT INTO ErrorLog (DateTime,Message,StackTrace,InnerException) VALUES ({0},{1},{2},{3})", dataAccess.da.UniversalDate(DateTime.Now), dataAccess.da.SqlEncode(ex.Message), dataAccess.da.SqlEncode(ex.StackTrace), dataAccess.da.SqlEncode(ex.InnerException != null ? ex.InnerException.Message : string.Empty)));
			} catch (Exception exe) {
				//Dont crash, thats what we are trying to avoid!!
			}
		}

	}


}
