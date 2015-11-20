public class Loading : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		if (clsIQ.IsLoaded)
			Response.Redirect("signin.aspx");

		object a = iq.Quotes.Count;
		// do something to trigger the load

		if (Request("debug") != null)
			lblStatus.Visible = true;

		if (Request("path") != null)
			hidref.Value = Request("Path");
		if (Request.UrlReferrer != null)
			hidref.Value = Request.UrlReferrer.AbsoluteUri;

		Literal css = new Literal();
		string stylesheet = null;

		if (Request("mfr") == null) {
			stylesheet = "channelcentral";
		} else {
			stylesheet = string.Format("Site-{0}", Request("mfr"));
		}

		if (!string.IsNullOrEmpty(stylesheet)) {
			css.Text = string.Format("<link href='{0}Styles/{1}.css' rel='stylesheet' type='text/css' />", ResolveUrl("~/"), stylesheet);
			Page.Header.Controls.Add(css);
		}

		//       Dim d As String() = New String(clsIQ.messages.Count) {}
		//        clsIQ.messages.CopyTo(d)
		//lblStatus.Text = String.Join("<br>", d.Reverse())
		//End If

		//progressBar.Width = New Unit(CDbl(clsIQ.messages.Count) / 61 * CDbl(progressPanel.Width.Value))
	}

}
