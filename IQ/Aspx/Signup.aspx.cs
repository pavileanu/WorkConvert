using System.Security.Cryptography;

public class Signup : clsPageLogging
{


	private void  // ERROR: Handles clauses are not supported in C#
Signup_Init(object sender, System.EventArgs e)
	{
		//If Not IsPostBack Then
		// FillDDL(DDLSeller, iq.Channels.Values)
		FillDDL(ddlLanguage, iq.Languages.Values);
		FillDDL(ddlCurrency, iq.Currencies.Values);
		FillDDL(ddlRole, iq.i_role_Code.Values);
		//End If

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//If DDLSeller.SelectedValue <> "" Then
		//    Dim teams = From v In iq.Teams.Values Where v.Channel.ID = DDLSeller.SelectedValue
		//    FillDDL(ddlTeam, teams)
		//End If

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnSignUp_Click(object sender, EventArgs e)
	{
		clsChannel BuyersChannel;
		if (!iq.Channels.ContainsKey((int)txtBuyerID.Text)){lblError.Text = "Unrecognised buyer channel";return;
}
		BuyersChannel = iq.Channels((int)txtBuyerID.Text);

		clsUser buyerUser = null;
		int aatuid;
		//adding account to user ID
		aatuid = (int)TxtAccountID.Text;
		if (aatuid == 0) {
			buyerUser = new clsUser(BuyersChannel, TxtEmail.Text, TxtRealName.Text, new nullableString(TxtTel1.Text), new nullableString(txtTel2.Text));
			if (buyerUser == null){lblError.Text = "Failed to create user (check field lengths are resonable).";return;
}
		} else {
			if (!iq.Users.ContainsKey(aatuid)){lblError.Text = "Unrecognized user";return;
}
			buyerUser = iq.Users(aatuid);
		}

		clsChannel SellerChannel = null;
		if (!iq.Channels.ContainsKey((int)txtSellerID.Text)){lblError.Text = "Unrecognised seller channel";return;
}
		SellerChannel = iq.Channels((int)txtSellerID.Text);

		clsTeam team = null;
		if (txtTeamID.Text != "") {
			if (!iq.Teams.ContainsKey((int)txtTeamID.Text)){lblError.Text = "Unrecognised Team";return;
}
			team = iq.Teams((int)txtTeamID.Text);
		}

		clsRole role;
		clsLanguage language;
		clsCurrency currency;

		role = iq.i_role_Code(ddlRole.SelectedValue);
		language = iq.Languages(ddlLanguage.SelectedValue);
		currency = iq.Currencies(ddlCurrency.SelectedValue);

		clsPriceBand priceBand = iq.getPriceBand(TxtpriceBand.Text);

		//create the new account
		clsAccount Account;

		object pw = "";
		if (TxtPassword.Text == "") {
			pw = GeneratePassword();
		} else {
			pw = Trim(TxtPassword.Text);
		}

		// Stop
		// Dim buyergroup As New clsBuyerGroup("New Buyer", sellerschannel, accountID)

		if (Request("mfr") == ""){lblError.Text = "No manufacturer request parameter supplied (&mfr=xxx)";return;
}

		string mfrcode = Request("mfr");
		Account = new clsAccount(buyerUser, simpleHash(pw), BuyersChannel, { role }, team, language, currency, SellerChannel, priceBand, SellerChannel.Region.Culture,
		mfrcode);
		Account.MustChangePassword = true;

		List<string> em = new List<string>();

		Dictionary<string, string> tags = new Dictionary<string, string>();
		tags.Add("password", pw);
		string baseurl = ConfigurationManager.AppSettings("BaseURL");

		tags.Add("url", baseurl + "/aspx/signin.aspx");
		tags.Add("email", Account.User.Email);
		tags.Add("hostname", Account.SellerChannel.DisplayName(Account.Language));
		tags.Add("mfr", Account.mfrCode);
		tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

		SendEmail(Account.User.Email, "WelcomeEmail.htm", tags, Account.Language, em, false);

		if (em.Count) {
			Label lbl = new Label();
			lbl.BackColor = Drawing.Color.Red;
			lbl.ForeColor = Drawing.Color.White;
			lbl.Text = string.Format("Sorry - we are presently unable to send your Welcome email - Please contact {0} quoting reference AC {1}", iq.Addresses("iQuoteSupportEmail").Translation.text(English), Account.ID);
			Form.Controls.Add(lbl);
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnFindUser_Click(object sender, EventArgs e)
	{
		object em;
		em = Trim(TxtEmail.Text);
		clsUser u;

		Match Match = Regex.Match(em, "^[a-zA-Z][\\w\\.-]*[a-zA-Z0-9]@[a-zA-Z0-9][\\w\\.-]*[a-zA-Z0-9]\\.[a-zA-Z][a-zA-Z\\.]*[a-zA-Z]$");

		if (Match.Success) {
			if (iq.i_user_email.ContainsKey(Trim(TxtEmail.Text))) {
				u = iq.i_user_email(Trim(TxtEmail.Text));
				TxtTel1.Text = u.tel1.DisplayValue;
				txtTel2.Text = u.tel2.DisplayValue;
				TxtRealName.Text = u.RealName;
				//Session("AddingAccountTo") = u.ID
				TxtAccountID.Text = u.ID;

			} else {
				//u = New clsUser(TxtEmail.Text, "", "", "", iq.Channels(DDLBuyer.SelectedValue))
			}
		} else {
			LblInvalidEmail.Visible = true;
		}

	}

}
