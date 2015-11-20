public class Listquotes : clsPageLogging
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = 0;

		if (Request("QuoteId") != null && UInt64.TryParse(Request.QueryString("lid"), lid)) {
			object agent = (clsAccount)iq.sesh(lid, "AgentAccount");
			object quote = iq.Quotes(Request("QuoteId"));

			if (!object.ReferenceEquals(agent.SellerChannel, quote.AgentAccount.SellerChannel)) {
				//Switch account, must be an admin??
				object found = false;
				foreach ( a in agent.User.Accounts.Values) {
					if (object.ReferenceEquals(a.SellerChannel, quote.AgentAccount.SellerChannel) && a.Password == agent.Password) {
						SwitchAccount(lid, a, a, errorMessages);
						agent = a;
						found = true;
						break; // TODO: might not be correct. Was : Exit For
					}
				}
				if (!found)
					return;
				//Add warning message here...
			}

			iq.sesh(lid, "QuoteID") = Request("QuoteId");
			if (!agent.Quotes.ContainsKey(Request("QuoteId")))
				agent.LoadQuotes(0);
			if (quote.RootItem.Children.Count == 0)
				quote.LoadItems(errorMessages);
			//If Not quote.Saved Then quote.Editable = True 'needs 

			iq.sesh(lid, "root") = "tree.1";
			//
			if (quote.RootItem.Children.Count > 0) {
				if (!iq.seshDic(lid).ContainsKey("branchstates") || iq.seshDic(lid)("branchStates") == null)
					iq.sesh(lid, "branchStates") = new Dictionary<string, clsBranchState>();
				clsBranchState.PloughPath(lid, quote.RootItem.Children(0).Path, errorMessages, 0, enumParadigm.configuringSystem);
				iq.sesh(lid, "path") = quote.RootItem.Children(0).Path;
				iq.sesh(lid, "quoteCursor") = quote.RootItem.Children(0).ID;
				iq.sesh(lid, "Paradigm") = enumParadigm.configuringSystem;
			}

			Response.Redirect("tree.aspx?lid=" + lid.ToString + Request("elid") != null ? "&elid=" + Request("elid") : "");
		}
	}


	private WebControl RecursiveFindControlByID(ref WebControl control, string id)
	{

		RecursiveFindControlByID = null;

		if (control.ID == id) {
			return control;
		}


		foreach ( c in control.Controls) {
			if (!c is System.Web.UI.WebControls.Literal & !c is LiteralControl) {
				WebControl ac;
				ac = RecursiveFindControlByID(c, id);
				if (!ac == null)
					return ac;
			}
		}

	}


}
