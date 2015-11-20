//Option Strict On

public class accounts : clsPageLogging
{

	public DropDownList hpiList;

	public DropDownList hpeList;
	// This page is responsible for deciding which Account is used within IQ2
	//
	// It can be called by either SignIn.aspx or Gatekeeper.aspx. On either
	// route, the following are looked for in the iq.sesh dictionary:
	//
	//   UserID          - Mandatory. The string ID of the logged-on user.
	//   AccountList     - Mandatory. An IEnumerable(Of clsAccount) of all the user's possible accounts.
	//   Host            - Optional. A seller channel ID. Used to restricts the list of selectable accounts.
	//   MFR             - Optional. HPE/HPI. Used to restricts the list of selectable accounts.
	//   Base            - Optional. A SKU code. Used to restricts the list of selectable accounts and also to
	//                     work out which side of the HPE/HPI split we're on (potentially overriding any MFR value).
	//
	// The general principle is that the list of possible accounts is worked out from the above and the following takes place: 
	// - If the list contains no items, an informative message is displayed (we could maybe try redirecting to the referring page?)
	// - If the list contains one item, the account is used and we redirect to Tree.aspx with no account selection UI shown
	// - If the list contains more than one item, the account selection UI is shown (split into HPE/HPI as appropriate) and shown.
	//   Once a choice is made we redirect to Tree.aspx.


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{

		UInt64 lid = (UInt64)Request.QueryString("lid");

		if (!(iq.sesh(lid, "UserID")) is int)
			return;
		if (!(iq.sesh(lid, "AccountList")) is IEnumerable<clsAccount>)
			return;

		clsUser u = iq.Users(iq.sesh(lid, "UserID"));
		IEnumerable<clsAccount> accountList = iq.sesh(lid, "AccountList");

		//iq.sesh(lid, "BuyerAccount") = Nothing
		//iq.sesh(lid, "AgentAccount") = Nothing

		// Filter the account list by Host (if specified) and sort it into quote count order
		u.CountQuotesPerAccount();
		IEnumerable<clsAccount> sortedAccounts;
		if (!(iq.sesh(lid, "Host")) is string || string.IsNullOrEmpty(iq.sesh(lid, "Host"))) {
			sortedAccounts = from ac in accountListorderby ac.NumQuotes descending;
		} else {
			string host = iq.sesh(lid, "Host").ToString();
			sortedAccounts = from ac in accountListwhere string.Equals(ac.SellerChannel.Code, host, StringComparison.InvariantCultureIgnoreCase)orderby ac.NumQuotes descending;
		}

		// If a deep link SKU has been specified, use this now to infer the Manufacturer (i.e. HPE/HPI) - 
		// potentially overriding any MFR specified
		if (!iq.sesh(lid, "Base") == null) {
			string sku = iq.sesh(lid, "Base").ToString();
			if (iq.i_SKU.ContainsKey(sku)) {
				object product = iq.i_SKU(sku);
				if (product.Manufacturer != Manufacturer.Unknown) {
					iq.sesh(lid, "MFR") = product.Manufacturer;
				}
			}
		}

		// HPI/HPE - create split lists of accounts
		IEnumerable<clsAccount> sortedAccountsHPI = null;
		IEnumerable<clsAccount> sortedAccountsHPE = null;

		// If we have a manufacturer (either MFR or Base specified on the QueryString), only look for accounts on this side
		Manufacturer mfr = Manufacturer.Unknown;
		if (!iq.sesh(lid, "MFR") == null) {
			mfr = iq.sesh(lid, "MFR");
		}

		int count = 0;
		if (mfr == Manufacturer.HPI || mfr == Manufacturer.Unknown) {
			sortedAccountsHPI = from ac in sortedAccountswhere ac.Manufacturer == Manufacturer.HPI;
			count += sortedAccountsHPI.Count;
		}

		if (mfr == Manufacturer.HPE || mfr == Manufacturer.Unknown) {
			//                                                                                         ADDED by Nick / 26/05/2015
			sortedAccountsHPE = from ac in sortedAccountswhere ac.Manufacturer == Manufacturer.HPE | ac.Manufacturer == Manufacturer.Unknown;
			count += sortedAccountsHPE.Count;
		}

		// If we ended up with no options, display a message to the user
		// If we have only one possible option select it automatically
		// If there's more than one possible account, display the account selector to the user
		clsLanguage language = English;
		if (count == 0) {
			Literal noAccounts = new Literal();
			noAccounts.Text = Xlt("Sorry, no accounts found. Please log out and try again.", language);
			panelContent.Controls.Add(noAccounts);
		} else if (count == 1) {
			int accountID;
			if (!sortedAccountsHPI == null && sortedAccountsHPI.Count == 1) {
				accountID = sortedAccountsHPI(0).ID;
			} else {
				accountID = sortedAccountsHPE(0).ID;
			}
			SelectAccount(lid, accountID);
		} else {
			Literal instructions = new Literal();
			instructions.Text = string.Format("<h2>{0}</h2>", Xlt("Where would you like to visit?", language));
			panelContent.Controls.Add(instructions);

			if (!sortedAccountsHPI == null && sortedAccountsHPI.Count > 0) {
				panelContent.Controls.Add(BuildAccountList("HPI", sortedAccountsHPI, language));
			}

			if (!sortedAccountsHPE == null && sortedAccountsHPE.Count > 0) {
				panelContent.Controls.Add(BuildAccountList("HPE", sortedAccountsHPE, language));
			}
		}

	}

	private Panel BuildAccountList(string mfrCode, IEnumerable<clsAccount> accounts, clsLanguage language)
	{

		BuildAccountList = new Panel();
		BuildAccountList.CssClass = "HostList";

		Image img = new Image();
		img.ImageUrl = string.Format("/images/{0}-Logo.jpg", mfrCode);
		BuildAccountList.Controls.Add(img);


		if (accounts.Count > 0) {
			// Build either the HPI or HPE list
			DropDownList list = null;
			if (mfrCode == "HPI") {
				this.hpiList = new DropDownList();
				list = hpiList;
			} else if (mfrCode == "HPE") {
				this.hpeList = new DropDownList();
				list = hpeList;
			}


			if (!list == null) {
				if (accounts.Count > 1) {
					ListItem item = new ListItem();
					item.Text = Xlt("Select account", language);
					item.Value = -1;
					list.Items.Add(item);
				}


				foreach ( account in accounts) {
					ListItem item = new ListItem();

					item.Value = account.ID;
					item.Attributes.Add("HostID", account.SellerChannel.Code);
					item.Attributes.Add("BuyerRegion", account.BuyerChannel.Region.Code);
					item.Attributes.Add("BuyerID", account.BuyerChannel.ID);
					item.Attributes.Add("AccountCurrency", account.Currency.Code);
					item.Text = string.Format("{0} ({1}) - {2} {3}", account.SellerChannel.Name, account.SellerChannel.Region.Code, account.NumQuotes, Xlt("quotes", language));

					list.Items.Add(item);

				}

				BuildAccountList.Controls.Add(list);

				Button button = new Button();
				button.Text = Xlt("Go", language);
				button.CommandName = mfrCode;
				button.Click += OnAccountSelected;
				BuildAccountList.Controls.Add(button);

			}

		}

	}


	private void OnAccountSelected(object sender, System.EventArgs e)
	{

		if (object.ReferenceEquals(sender.GetType(), typeof(Button))) {
			UInt64 lid = (UInt64)Request.QueryString("lid");
			Button button = sender;
			List<string> errorMessages = new List<string>();
			int accountID = int.MinValue;

			if ((button.CommandName == "HPI") && (!hpiList == null)) {
				accountID = hpiList.SelectedValue;
			} else if ((button.CommandName == "HPE") && (!hpeList == null)) {
				accountID = hpeList.SelectedValue;
			}

			SelectAccount(lid, accountID);

		}

	}


	private void SelectAccount(UInt64 lid, int accountID)
	{

		if (accountID >= 0) {

			if (iq.Accounts.ContainsKey(accountID)) {
				object account = iq.Accounts(accountID);

				// Moved from Gatekeeper.aspx as the required account is now only known here
				if (!iq.sesh(lid, "viaGatekeeper") == null) {
					if (!string.IsNullOrEmpty(iq.sesh(lid, "gk_cPriceBand"))) {
						account.Priceband = iq.getPriceBand(iq.sesh(lid, "gk_cPriceBand"));
					} else if (!string.IsNullOrEmpty(iq.sesh(lid, "gk_cAccountNum"))) {
						account.Priceband = iq.getPriceBand(iq.sesh(lid, "gk_cAccountNum"));
					}
					account.update(errorMessages);
				}

				SwitchAccount(lid, account, account, errorMessages);

				Form.Controls.Add(ErrorDymo("Scanning promotions (could take a few seconds) . . .", lid));
				if (errorMessages.Count == 0)
					OutputErrors(Form.Controls, errorMessages, lid, true);

				Response.Redirect("scanpromos.aspx?lid=" + lid, false);

			}

		}

	}

}
