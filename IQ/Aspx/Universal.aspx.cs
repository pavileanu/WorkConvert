
public class Universal : clsPageLogging
{


	protected ListBox countryList = new ListBox();

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		string mfr = null;
		bool cnames = false;
		// TODO - temporary flag to allow us to switch between having and not having iquote.hp(e).com CNAMEs set up. Will always need to be true soon...

		// Create a case-insensitive dictionary for the Request parameters
		Dictionary<string, string> requestParams = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		foreach (string key in Request.QueryString) {
			if (!key == null)
				requestParams.Add(key, Request.QueryString(key).ToLower());
		}

		// Try and get the mfr
		if (requestParams.ContainsKey("mfr")) {
			if (requestParams("mfr") == "hpe") {
				mfr = "hpe";
			} else if (requestParams("mfr") == "hpi") {
				mfr = "hpi";
			}
		}

		// We might be able to work out the mfr from the request host
		if (string.IsNullOrEmpty(mfr)) {
			mfr = InferUniversalManufacturer(Request);
		}


		if (string.IsNullOrEmpty(mfr)) {
			// Don't have the mfr - display UI to retrieve it
			if (cnames) {
				BuildMfrSelector("hpi", panelHPI, "Desktops, Laptops, Workstations", "http://iquote.<b>hp</b>.com");
				BuildMfrSelector("hpe", panelHPE, "Servers, Storage, Networking", "http://iquote.<b>hpe</b>.com");
				panelBanner.Visible = true;
			} else {
				BuildMfrSelector("hpi", panelHPI, "Desktops, Laptops, Workstations", null);
				BuildMfrSelector("hpe", panelHPE, "Servers, Storage, Networking", null);
			}


			univSubtitle.Text = Xlt("HP Separation Update:", English);
			univInstructions.Text = Xlt("Use the links below to access iQuote Universal for the company of your choice.&nbsp;&nbsp;If you already had an iQuote Universal login prior to August 1st 2015, your login credentials have been copied to both instances as separate accounts.&nbsp;&nbsp;Any saved quotes have been moved to the appropriate instance based on the product type.", English);


		} else {
			// We have all we need - redirect
			Redirect(mfr);

		}

	}


	private void BuildMfrSelector(string mfr, Panel panel, string description, string url)
	{
		Panel box = new Panel();
		box.CssClass = "HostList";

		Label label = new Label();
		label.Text = description;
		label.CssClass = "mfrDescription";
		box.Controls.Add(label);

		Image img = new Image();
		img.ImageUrl = string.Format("/images/{0}-Logo.jpg", mfr);
		box.Controls.Add(img);

		panel.Controls.Add(box);

		Literal lit = new Literal();
		lit.Text = "<br/><br/><br/>";
		panel.Controls.Add(lit);

		LinkButton button = new LinkButton();

		if (url == null) {
			//button.Text = Xlt("Enter", English)
			button.Text = "Enter";
		} else {
			//button.Text = Xlt(url, English)
			button.Text = url;
		}

		button.Attributes.Add("mfr", mfr);
		button.CssClass = "mfrButton";
		button.Click += OnMfrSelected;
		panel.Controls.Add(button);

	}


	private void OnMfrSelected(object sender, System.EventArgs e)
	{

		if (object.ReferenceEquals(sender.GetType(), typeof(LinkButton))) {
			Response.Redirect(BuildPostbackUrl((LinkButton)sender));

		}

	}

	private string BuildPostbackUrl(LinkButton button)
	{

		// Build a URL to postback to this page with the details we have so far
		string url = Request.Path.ToString();

		if (!button.Attributes("mfr") == null) {
			url = string.Format("{0}?mfr={1}", url, button.Attributes("mfr"));
		}

		return url;

	}


	private void Redirect(string mfr)
	{
		Response.Redirect(string.Format("Signin.aspx?Universal&mfr={0}", mfr));

	}

}
