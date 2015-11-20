public class SolutionStoreLanding : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = 0;
		if (Request("lid") != null && UInt64.TryParse(Request("lid"), lid)) {
			//Validate the user is on this server...

			if (iq.SeshAlive(lid)) {
				//Read basket and add using shopping list
				object slstring = "";
				object skus = Split(Request("SKUS"), ",");
				object qtys = Split(Request("QTYS"), ",");
				for (i = 0; i <= skus.Length - 1; i++) {
					slstring += skus(i) + "*" + qtys(i).ToString + ";";
				}

				object agentAccount = iq.seshTyped<clsAccount>(lid, "AgentAccount");
				object buyerAccount = iq.seshTyped<clsAccount>(lid, "BuyerAccount");
				object errormessages = new List<string>();
				object FirstSysPath = "";
				clsQuote.FromShoppingList(lid, agentAccount, buyerAccount, slstring, errormessages, FirstSysPath);

				iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem;
				iq.sesh(lid, "path") = "tree.1";

				//Response.Write("<script>this.parent.postMessage(""reloadplease"", ""https://" & Request("post") & ".hpiquote.net"");</script>")
				scriptManager.RegisterStartupScript(updatePanel, this.GetType(), "reload", "this.parent.postMessage(\"reload\", \"https://" + Request("post") + ".hpiquote.net\");", true);

			}
		}

	}

}
