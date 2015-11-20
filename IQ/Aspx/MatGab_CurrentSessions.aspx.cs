public class CurrentSessions : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		Pnl.Controls.Add(iq.SessionTable);

	}


}
