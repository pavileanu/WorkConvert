public class WaitMessage : clsPageLogging
{
	public string lid;
	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		lid = Request.QueryString("lid");
	}

}
