using System.Globalization;

public class ExportedQuotes : System.Web.UI.Page
{
	public string version;
	public string quoteType;
	public string quoteDate;
	private clsAccount agentAccount;
	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid;
		bool sessionFailed = false;
		agentAccount = null;
		try {
			lid = Request.QueryString("lid");
			agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		//System.Web.HttpRequestValidationException
		} catch (Exception ex) {
			sessionFailed = true;

		}
		lblQuoteID.Text = Xlt("Quote ID : ", agentAccount.Language);
		version = Xlt("Version", agentAccount.Language);
		quoteType = Xlt("Type", agentAccount.Language);
		quoteDate = Xlt("Date", agentAccount.Language);

		int quoteID;
		DataTable quoteHistory;
		if (Request("quoteRootID") != null) {
			quoteID = Request("quoteRootID");
			quoteHistory = getQuoteExport(quoteID);
		} else {
			quoteID = Request("QuoteID");
			quoteHistory = getQuoteVersionExports(quoteID, agentAccount);
		}
		quoteNumber.Text = quoteID;
		if (quoteHistory.Rows.Count > 0) {
			exportTable.DataSource = quoteHistory;
			exportTable.DataBind();
		} else {
			Response.Write("This quote was not exported");
		}

	}
	public string TranslateType(string strType)
	{
		if (Trim(strType).Length > 0 & agentAccount != null) {
			return Xlt(strType, agentAccount.Language);
		} else {
			return strType;
		}
	}
	public string ConvertDate(DateTime d)
	{
		return d.ToString("G", CultureInfo.CreateSpecificCulture(agentAccount.Culture.Code));
	}
}
