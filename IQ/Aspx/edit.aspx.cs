public class edit1 : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		object q;
		q = Request.RawUrl;
		q = Split(q, "?")(1);

		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));
		Dictionary<string, clsEditHeader> EditHeaders;
		EditHeaders = new Dictionary<string, clsEditHeader>();

		iq.sesh(lid, "editHeaders") = EditHeaders;

		List<string> asPage;
		//Switches individual rows between Page And ListRow mode - things default to list, if their (path) is in here they're in page mode.
		if (!iq.SeshContains(lid, "asPage")) {
			asPage = new List<string>();
			iq.sesh(lid, "asPage") = asPage;
		}

		//REMOVE THE lid (not sure why ??)
		[] B = Split(q, "&");
		for (I = 0; I <= UBound(B); I++) {
			if (UCase(Left(B(I), 4)) == "LID=")
				B(I) = "FGFG=0";
		}
		q = Join(B, "&");

		if (drpLanguage.SelectedIndex > -1) {
			q = q + "&language=" + drpLanguage.SelectedValue;
		}

		//Response.Write("<script language='javaScript'>alert('hello');</script>")
		//drop the lid (becuase Embed adds it)

		object script;

		// embed(url, divID, append, sendNVPs) {
		script = "embed('../editor/editor.aspx?" + q + "','EditPanel',false,false);";
		//will *REPLACE* EditPanel


		Literal lit = new Literal();
		lit.Text = "<script language='JavaScript'>" + script + "</script>;";
		Page.Controls.Add(lit);

		if (!Page.IsPostBack) {
			drpLanguage.DataSource = iq.ActiveLanguages.Values;
			drpLanguage.DataTextField = "LocalName";
			drpLanguage.DataValueField = "ID";
			drpLanguage.DataBind();
		}


	}

	protected void drpLanguage_SelectedIndexChanged(object sender, EventArgs e)
	{
		string selection = string.Empty;
	}
}
