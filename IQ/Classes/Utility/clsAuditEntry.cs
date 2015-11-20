using System.Threading;

public class clsAuditEntry
{
    public DateTime DateTime;
    public UInt64 lid;
    public string Action;
    public string SourcePath;
    public string TargetPath;
    public string SecondaryMessage;
    public string Message;
    public string PageName;
    public string SourceURL;
    public double TimeToLoad;
    public string HttpRequestMethod;
    public string UrlReferrer;
    public string AuditType;
    public bool ActionUndone = false;
    public int? AuditId = null;
    public int DBMethod = 0;

    private static readonly object @lock = new object();



    public bool Save()
    {


        try
        {

            //URL
            object Url = dataAccess.da.SqlEncode((string.IsNullOrEmpty(this.UrlReferrer)) ? "" : (new Uri(this.UrlReferrer).PathAndQuery));

            if (DBMethod == 0)
            {
                //Nick - Added the transaction wrapper as we were getting a table deadlock !
                //refactored onto multiple lines  23/03/15
                //if it recurrs there is a suggestion to 'add an index on the self referencing column' here
                //http://stackoverflow.com/questions/5898743/deadlock-using-self-referential-foreign-key

                // Serialize calls through this DB call as a possible way of avoiding the deadlock issues
                lock (@lock)
                {

                    //Nick removed for now - as nobody ever checks the audi log and i suspect it to be the source of perfromance and stability problems (dbexecuteSQL gailing and not closing connections /deadlock problem(s))

                    //dataAccess.da.DBExecutesql(String.Format( _
                    //"BEGIN TRAN;" & _
                    //"INSERT INTO auditlog (MachineName,DateTime,lid,Action,SourcePath,TargetPath,Messages,SecondaryMessage,PageName,SourceURL,TimeToLoad_MS,HttpRequestMethod,UrlReferrer,ParentId)" & _
                    //" VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}," & _
                    //"(select top 1 id from auditlog AS parent where  parent.SourceURL = " & Url & " and " & lid & "=parent.lid and parent.lid <> 0 order by id desc)" & _
                    //" );" & _
                    //"COMMIT TRAN;", _
                    //dataAccess.da.SqlEncode(Environment.MachineName), _
                    //dataAccess.da.UniversalDate(Me.DateTime), _
                    //Me.lid, dataAccess.da.SqlEncode(Me.Action), _
                    //dataAccess.da.SqlEncode(Me.SourcePath), _
                    //dataAccess.da.SqlEncode(Me.TargetPath), _
                    //dataAccess.da.SqlEncode(Me.Message), _
                    //dataAccess.da.SqlEncode(Me.SecondaryMessage), _
                    //dataAccess.da.SqlEncode(Me.PageName), _
                    //dataAccess.da.SqlEncode(Me.SourceURL), _
                    //Me.TimeToLoad, _
                    //dataAccess.da.SqlEncode(Me.HttpRequestMethod), _
                    //dataAccess.da.SqlEncode(Me.UrlReferrer)))
                }
            }
            else
            {
                dataAccess.da.DBExecutesql(string.Format("UPDATE auditlog SET ActionUndone = " + dataAccess.da.SqlEncode(ActionUndone) + " WHERE id=" + AuditId.ToString()));
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

}