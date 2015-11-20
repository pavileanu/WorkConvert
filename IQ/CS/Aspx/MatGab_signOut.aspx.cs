public class signOut1 : clsPageLogging
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");

		object message = "Thank you for using iQuote - see you again soon.";
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		if (agentAccount != null) {
			litThanks.Text = Xlt(message, agentAccount.Language);
		} else {
			litThanks.Text = message;
		}

		// SK - Store the Manufacturer as a discrete object so that the Master Page can use it to work
		// out which style sheet to apply to this page. The whole sesh is killed in the PreRender event,
		// by which time the Master Page will have made use of the value.
		if (!iq.sesh(lid, "AgentAccount") == null) {
			iq.sesh(lid, "MFR") = agentAccount.Manufacturer;
			iq.seshDic(lid).Remove("AgentAccount");
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Page_PreRender(object sender, System.EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");

		if (iq != null) {
			iq.KillSesh(lid);
		}

	}

}
