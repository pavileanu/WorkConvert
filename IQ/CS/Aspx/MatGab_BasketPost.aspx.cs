public class BasketPost : System.Web.UI.Page
{
	public string xmlString;
	public string accountNum;
	public string sessionID;
	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		ulong lid;
		lid = Request("lid");
		Label1.Visible = false;
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		if (iq.sesh(lid, "basketContent") != null) {
			if (agentAccount.SellerChannel.basketMode == "FRM") {

				Literal1.Text = iq.sesh(lid, "basketContent");

			} else {
				xmlString = HttpUtility.HtmlEncode(iq.sesh(lid, "basketContent").ToString());

			}
			string accountNum = iq.sesh(lid, "GK_cAccountNum");
			string sessionID = iq.sesh(lid, "GK_SessionID");
			form1.Action = iq.sesh(lid, "GK_BasketURL").ToString();

		} else {
			xmlString = "Basket Empty";
			form1.Action = "Basketdisplay.aspx";
			//Label1.Visible = True
		}

	}

}
