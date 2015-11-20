public class scanPromos : clsPageLogging
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{

		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));
		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");

		// THIS NEEDS INVESTIGATING -todo - quick fix for security testing
		if (buyerAccount != null) {
			TagPromoBranches(buyerAccount, errorMessages);
		}
		Response.Redirect("WaitMessage.aspx?lid=" + (string)lid, false);
		//"WaitMessage.aspx?lid=" & lid, False)
		//Response.Redirect("tree.aspx?lid=" & CStr(lid))

	}

}
