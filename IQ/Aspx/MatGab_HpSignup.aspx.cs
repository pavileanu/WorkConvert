using System.Globalization;

public class HpSignup : System.Web.UI.Page
{

	private clsChannel universalChannel;
	private string selectedlang;

	private string selectedCountry;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		List<string> errormessages = new List<string>();


		if (Request.QueryString("lang") != null && Request.QueryString("mfr") != null) {
			string[] langarray = Split(Request.QueryString("lang"), "|");
			selectedlang = langarray(0);
			selectedCountry = langarray(1);

			object language = English;
			if (iq.i_language_Code.ContainsKey(selectedCountry))
				language = iq.i_language_Code(selectedCountry);

			header.InnerHtml = Xlt(string.Format("Register for iQuote Universal ({0})", selectedCountry), language);

			IEnumerable<clsChannel> regsEnum = from j in iq.Channels.Valueswhere j.Code.StartsWith("MHP") & j.Code.EndsWith("U") & j.Universal == true & UCase(j.Region.Code) == UCase(selectedCountry) & UCase(j.Region.Culture.Code) == UCase(selectedlang);


			if (regsEnum.Count > 0) {
				litMsg.Text = "<span class=\"HPSignupInfo\" > " + regsEnum(0).Code + "</span>";
				universalChannel = regsEnum(0);

				LabelFullName.Text = Xlt("Full Name", language);
				LabelEmailName.Text = Xlt("Email Address", language);
				LabelConfirmEmail.Text = Xlt("Confirm Email Address", language);
				LabelCompanyName.Text = Xlt("Company Name", language);

				LabelUserType.Text = Xlt("User Type", language);
				if (ddlUserType.Items.Count == 0) {
					ddlUserType.Items.Add(new ListItem(Xlt("Please select", language), string.Empty));
					ddlUserType.Items.Add(new ListItem(Xlt("Distributor", language), "USERTYPE_DISTRIBUTOR"));
					ddlUserType.Items.Add(new ListItem(Xlt("Reseller", language), "USERTYPE_RESELLER"));
					ddlUserType.Items.Add(new ListItem(Xlt("End User", language), "USERTYPE_ENDUSER"));

					if (string.Equals(Request.QueryString("mfr"), "hpe", StringComparison.InvariantCultureIgnoreCase)) {
						ddlUserType.Items.Add(new ListItem(Xlt("HPE Employee", language), "USERTYPE_HPEMPLOYEE"));
					} else {
						ddlUserType.Items.Add(new ListItem(Xlt("HP Employee", language), "USERTYPE_HPEMPLOYEE"));
					}

					ddlUserType.Items.Add(new ListItem(Xlt("Other", language), "USERTYPE_OTHER"));
				}

				LabelPostCode.Text = Xlt("Post Code", language);
				LabelTelephone.Text = Xlt("Telephone", language);
				LabelREquiredField.Text = Xlt("Required field", language);
				HeaderTandC.Text = Xlt("Terms and Conditions", language);

				BtnSave.Visible = true;
				BtnSave.Text = Xlt("Register", language);

				BtnCancel.Visible = true;
				BtnCancel.Text = Xlt("Cancel", language);

				if (iq.Legal.ContainsKey("HPUniversalT&C"))
					litLegal.Text = iq.Legal("HPUniversalT&C").Translation.text(language).Replace("[mfr]", GetMfrDisplay(Request.QueryString("mfr")));
				chkAgree.Text = Xlt("I agree.", language);

			} else {
				litMsg.Text = "<span class=\"HPSignupError\" > " + Xlt("Could not load HP Channel", language) + "</span>";
				BtnSave.Visible = false;
				litLegal.Visible = false;
				chkAgree.Visible = false;
			}


		} else {
			Response.Redirect("SignIn.aspx");
		}

	}

	private string GetMfrDisplay(string mfrCode)
	{

		GetMfrDisplay = string.Empty;

		if (string.Equals(mfrCode, "hpe", StringComparison.InvariantCultureIgnoreCase)) {
			GetMfrDisplay = "HPE";
		} else if (string.Equals(mfrCode, "hpi", StringComparison.InvariantCultureIgnoreCase)) {
			GetMfrDisplay = "HPI";
		}

	}



	protected void  // ERROR: Handles clauses are not supported in C#
BtnSave_Click(object sender, EventArgs e)
	{
		string fullName = txtFullName.Text;
		string email = txtEmailName.Text;
		string emailConfirm = txtConfirmEmail.Text;
		string companyName = txtCompanyName.Text;
		string postCode = txtPostCode.Text;
		string telephoneNumber = txtTelephone.Text;
		bool agree = chkAgree.Checked;
		clsUser user;
		object password = string.Empty;
		bool inputOK = true;

		litRegistered.Text = string.Empty;
		litError.Text = string.Empty;

		clsLanguage language = English;
		if (!universalChannel == null && !universalChannel.Region == null && !universalChannel.Region.DefaultLanguage == null) {
			language = universalChannel.Region.DefaultLanguage;
		}

		if (string.IsNullOrWhiteSpace(fullName)) {
			litError.Text += Xlt("Full Name is missing,", language) + "<br/>";
			inputOK = false;
		}
		if (string.IsNullOrWhiteSpace(email)) {
			litError.Text += Xlt("Email Address is missing", language) + "<br/>";
			inputOK = false;
		}
		if (string.Compare(email, emailConfirm, true) != 0) {
			litError.Text += Xlt("Email Addresses don't match", language) + "<br/>";
			inputOK = false;
		}
		if (string.IsNullOrWhiteSpace(companyName)) {
			litError.Text += Xlt("Company Name is missing", language) + "<br/>";
			inputOK = false;
		}
		if (ddlUserType.SelectedIndex == 0) {
			litError.Text += Xlt("User Type not selected", language) + "<br/>";
			inputOK = false;
		}
		if (string.IsNullOrWhiteSpace(postCode)) {
			litError.Text += Xlt("Post Code is missing", language) + "<br/>";
			inputOK = false;
		}
		if (agree == false) {
			litError.Text += Xlt("Terms and conditions not accepted", language) + "<br/>";
			inputOK = false;
		}

		string mfrCode = Request("mfr");
		if (mfrCode == ""){litError.Text += Xlt("No manufacturer (request parameter mfr) supplied", language);inputOK = false;}


		if (inputOK) {
			List<string> errorMessages = new List<string>();

			// Pick up language
			if (selectedlang.Contains("-")) {
				selectedlang = Split(selectedlang, "-")(0);
			}
			clsLanguage accountLanguage = iq.i_language_Code(selectedlang);

			// Pick up currency
			RegionInfo regionInfo = new RegionInfo(selectedCountry);
			clsCurrency currency = new clsCurrency();
			if (regionInfo != null && iq.i_currency_code.ContainsKey(regionInfo.ISOCurrencySymbol)) {
				currency = iq.i_currency_code(regionInfo.ISOCurrencySymbol);
			}

			// Pick up user type
			object userType = ddlUserType.SelectedValue;

			// Pick up region
			clsRegion region = iq.i_region_code(selectedCountry);

			// Give the universal channel a team if it doesn't have one
			if (universalChannel.Teams.Count == 0) {
				clsTeam team = new clsTeam(universalChannel, "EveryOne");
			}

			// Create or reference a user
			if (iq.i_user_email.ContainsKey(email)) {
				user = iq.i_user_email(email);
			} else {
				user = new clsUser(universalChannel, email, fullName, new nullableString(telephoneNumber), new nullableString());
			}

			// Create or reference a buyer channel
			string buyerID = UCase("R" + Left(companyName, 2) + postCode.Replace(" ", ""));
			clsChannel buyerChannel = (from c in iq.Channels.Valueswhere c.Code.Equals(buyerID)).FirstOrDefault;
			if (buyerChannel == null) {
				buyerChannel = new clsChannel(universalChannel, companyName, companyName, "", buyerID, universalChannel.Region, new nullableString(), new nullableString(), new nullableString(Left(email, InStr(email, "@") - 1)), 15,
				"tree.1", "", 0, 0, "R", "", "", universalChannel.DefaultCurrency, false, "",
				"", "");
			}

			// Create or reference an account for this user/buyer channel/universal channel
			clsAccount account = (from a in iq.Accounts.Valueswhere a.User.Equals(user) && a.BuyerChannel.ID == buyerChannel.ID && a.SellerChannel.ID == universalChannel.ID).FirstOrDefault;
			bool accountCreated = false;
			if (account == null) {
				password = GeneratePassword();
				account = new clsAccount(user, simpleHash(password), buyerChannel, { iq.i_role_Code("user") }, universalChannel.Teams.First.Value, accountLanguage, currency, universalChannel, iq.getPriceBand(""), region.Culture,
				mfrCode);
				accountCreated = true;
			}

			// Ensure the account has the selected user type role
			if (iq.i_role_Code.ContainsKey(userType) && !account.i_roles_code.ContainsKey(userType)) {
				account.i_roles_code.Add(userType, iq.i_role_Code(userType));
			}


			if (accountCreated) {
				// New account created - inform the user
				Dictionary<string, string> tags = new Dictionary<string, string>();
				string baseurl = ConfigurationManager.AppSettings("BaseURL");

				string url;
				url = baseurl + "/Aspx/SignIn.aspx?mfr=" + Request("mfr");

				tags.Add("hostname", universalChannel.DisplayName(accountLanguage));
				tags.Add("email", email);
				tags.Add("password", password);
				tags.Add("firstname", fullName);
				tags.Add("url", url);
				tags.Add("extratext", string.Empty);
				tags.Add("mfr", Request("mfr"));
				tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

				List<string> em = new List<string>();
				//Returns any error messages encountered whilst emailing
				SendEmail(email, "WelcomeEmail.htm", tags, account.Language, em, false);

				Response.Redirect(string.Format("HPSignedup.aspx?mfr={0}&lang={1}", Request("mfr"), Request("lang")));
			} else {
				// User already has a relevant account - tell them so they can go sign in
				Response.Redirect(string.Format("HPSignedup.aspx?mfr={0}&lang={1}&existingAccount=Y", Request("mfr"), Request("lang")));
			}

		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnCancel_Click(object sender, EventArgs e)
	{
		Response.Redirect(string.Format("SignIn.aspx?Universal&mfr={0}", Request("mfr")));

	}

}
