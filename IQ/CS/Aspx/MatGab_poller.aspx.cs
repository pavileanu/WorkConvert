public class poller : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		if (Request("key") == "rcpolling_1") {
			//Add any logic in here to respond to the dyn service, its expecting "True"
			Response.Write(clsIQ.IsLoaded.ToString);
			Response.End();
		}
	}

}
