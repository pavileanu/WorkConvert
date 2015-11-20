public class ClickThru : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		int advertID = (int)Request("advertid");
		clsAdvert clickedAdvert = iq.Adverts(advertID);
		UInt64 lid = Request.QueryString("lid");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		clsClickThru advertClick = new clsClickThru(agentAccount, clickedAdvert, Now);

	}

}
