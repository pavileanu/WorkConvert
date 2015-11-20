using dataAccess;

public class administration : System.Web.UI.Page
{
	private clsAccount agentaccount;
	private clsAccount buyeraccount;
	private DataTable dtUsers;
	private string iFrameStyle;
	private string iFrameSrc;
	public bool AccountCanDisableUsers = false;
	public bool AccountCanSetupUsers = false;
	public bool AccountCanResetPasswords = false;
	public bool AccountIsDistiAdmin = false;
	public bool IsGlobalAdmin = false;
	//Public AvailableRoles As List(Of clsRole)

	private List<clsUser> NewCreatedUsers;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");
		if (!iq.SeshAlive(lid) || !(AccountHasRight(lid, "ADMINMENU") | AccountHasRight(lid, "GLOBALADM"))) {
			Response.Redirect("signin.aspx");
		}

		// Handle any manually-configured postbacks
		object eventTarget = Convert.ToString(Request.Params.Get("__EVENTTARGET"));
		object eventArgument = Convert.ToString(Request.Params.Get("__EVENTARGUMENT"));
		if (string.Equals(eventTarget, "ToggleRole", StringComparison.CurrentCultureIgnoreCase)) {
			ToggleRole(Convert.ToInt32(eventArgument));
		}

		//AvailableRoles = iq.i_role_Code.Values.ToList


		//If Not Page.IsPostBack Then
		dtUsers = new DataTable();
		DataTable dtActivity = new DataTable();
		dtUsers.Columns.Add("Email", typeof(string));
		dtUsers.Columns.Add("ID", typeof(int));
		dtUsers.Columns.Add("AccountID", typeof(int));
		dtUsers.Columns.Add("RealName", typeof(string));
		dtUsers.Columns.Add("ChannelName", typeof(string));
		dtUsers.Columns.Add("Disabled", typeof(bool));
		//dtUsers.Columns.Add("Quotes", GetType(Integer))
		dtUsers.Columns.Add("DistiAdmin", typeof(bool));
		dtUsers.Columns.Add("LastUsed", typeof(string));
		dtUsers.Columns.Add("Roles", typeof(clsRole[]));
		dtUsers.Columns.Add("AvailableRoles", typeof(clsRole[]));
		dtUsers.Columns.Add("HighestRole", typeof(string));
		dtUsers.Columns.Add("RoleFunction", typeof(string));

		//End If
		agentaccount = iq.sesh(lid, "AgentAccount");
		buyeraccount = iq.sesh(lid, "BuyerAccount");

		if (agentaccount == null)
			Response.Redirect("signin.aspx");
		//Dim dtUsers As New  DataTable

		bool channelCentralUser = agentaccount.User.Email.ToLower().EndsWith("@channelcentral.net");

		int[,] quoteSummary = new int[3, 3];

		if (agentaccount.HasRight("DISABLEUSR") | agentaccount.HasRight("FULLDIST") | agentaccount.HasRight("GLOBALADM"))
			AccountCanDisableUsers = true;
		if (agentaccount.HasRight("CREATEUSR") | agentaccount.HasRight("FULLDIST") | agentaccount.HasRight("GLOBALADM"))
			AccountCanSetupUsers = true;
		if (agentaccount.HasRight("PWDRESET") | agentaccount.HasRight("FULLDIST") | agentaccount.HasRight("GLOBALADM"))
			AccountCanResetPasswords = true;
		if (agentaccount.HasRight("FULLDIST") | agentaccount.HasRight("GLOBALADM"))
			AccountIsDistiAdmin = true;

		//If agentaccount.HasRight("GLOBALADM") Then
		//    PnlMultiSend.Visible = True
		//End If


		if (!IsPostBack) {
			// Email domain list
			drpDomain.DataSource = agentaccount.SellerChannel.Domains;
			drpDomain.DataBind();

			if (channelCentralUser) {
				if (agentaccount.HasRight("SYSMESSAGE")) {
					panelSignInMessage.Visible = true;
					panelHpeSystemMessage.Visible = true;
					panelHpiSystemMessage.Visible = true;

					bool messageExists = false;

					// System sign-in message
					if (!iq.UserMessages == null) {
						if (iq.UserMessages.ContainsKey("SignInScreenMessage")) {
							if (iq.UserMessages("SignInScreenMessage").Count > 0) {
								messageExists = true;
							}
						}
					}
					if (messageExists) {
						 // ERROR: Not supported in C#: WithStatement


						btnAmendSystemMessage.Visible = true;
					//btnDeleteSystemMessage.Visible = True
					} else {
						btnAddSystemMessage.Visible = true;
					}

					// HPE System message
					messageExists = false;
					if (!iq.UserMessages == null) {
						if (iq.UserMessages.ContainsKey("HPESystemMessage")) {
							if (iq.UserMessages("HPESystemMessage").Count > 0) {
								messageExists = true;
							}
						}
					}
					if (messageExists) {
						 // ERROR: Not supported in C#: WithStatement


						btnAmendHpeSystemMessage.Visible = true;
					//btnDeleteHpeSystemMessage.Visible = True
					} else {
						btnAddHpeSystemMessage.Visible = true;
					}

					// HPI System message
					messageExists = false;
					if (!iq.UserMessages == null) {
						if (iq.UserMessages.ContainsKey("HPISystemMessage")) {
							if (iq.UserMessages("HPISystemMessage").Count > 0) {
								messageExists = true;
							}
						}
					}
					if (messageExists) {
						 // ERROR: Not supported in C#: WithStatement


						btnAmendHpiSystemMessage.Visible = true;
					//btnDeleteHpiSystemMessage.Visible = True
					} else {
						btnAddHpiSystemMessage.Visible = true;
					}
				} else {
					RemoveMenuItem(adminMenu.Items, "System");
				}

			} else {
				// Non-Channel Central user; hide the System tab
				RemoveMenuItem(adminMenu.Items, "System");

			}

		}


		//        txtMultiHost.Text = agentaccount.SellerChannel.Code

		List<string> errormessages = new List<string>();

		object list = from j in iq.Accounts.Valueswhere (object.ReferenceEquals(j.SellerChannel, agentaccount.BuyerChannel) | object.ReferenceEquals(j.BuyerChannel, agentaccount.BuyerChannel)) && j.User.Email.ToLower.Contains(txtFilter.Text.ToLower) && (!chkonlyDistiAdmin.Checked || j.HasRight("FULLDIST"));

		int cnt = list.Count;

		foreach ( account in list) {
			//dtUsers.Rows.Add(account.User.Email, account.User.ID, account.User.RealName, account.BuyerChannel.Name, account.User.Disabled, account.Quotes.Values.Count, account.HasRight("FULLDIST"), DateString, account.Roles)
			dtUsers.Rows.Add(account.User.Email, account.User.ID, account.ID, account.User.RealName, account.BuyerChannel.Name, account.User.Disabled, account.HasRight("FULLDIST"), DateString, account.Roles, GetAvailableRoles(account.Roles),
			GetHighestRole(account.Roles), GetRoleFunction(account));
		}

		iq.sesh(lid, "UserDetailsTable") = dtUsers;

		agentaccount.SellerChannel.fixteams(errormessages);


		drpCurrency.DataSource = iq.Currencies.Values;
		drpCurrency.DataTextField = "Code";
		drpCurrency.DataValueField = "ID";
		drpCurrency.DataBind();
		drpCurrency.Items.FindByValue(agentaccount.Currency.ID).Selected = true;


		if (!IsPostBack) {
			ddlChannels.DataSource = agentaccount.User.Accounts.Where(ac => (ac.Value.Password == agentaccount.Password) && (channelCentralUser || (agentaccount.Manufacturer == ac.Value.Manufacturer))).Select(ac => ac.Value.BuyerChannel).Distinct;
			ddlChannels.DataBind();
			ddlChannels.Items.FindByValue(agentaccount.BuyerChannel.ID.ToString()).Selected = true;

			if (ddlChannels.Items.Count <= 1) {
				ddlChannels.Visible = false;
				lblChannelSelect.Visible = false;
			}


			drpTeams.DataSource = iq.Channels(ddlChannels.SelectedValue).Teams.Values;
			drpTeams.DataTextField = "Name";
			drpTeams.DataValueField = "ID";
			drpTeams.DataBind();


			lbRoles.DataSource = agentaccount.HasRight("GLOBALADM") ? iq.Roles.Values.Where(ro => {
				"USER",
				"DISTADMIN",
				"SUPPORT",
				"EDITOR",
				"ADMIN"
			}.Contains(ro.Code)) : iq.Roles.Values.Where(ro => {
				"USER",
				"DISTADMIN"
			}.Contains(ro.Code));
			lbRoles.DataTextField = "EnglishDisplayName";
			lbRoles.DataValueField = "ID";
			lbRoles.DataBind();


			if (!agentaccount.HasRight("GLOBALADM")) {
				lbRoles.SelectionMode = ListSelectionMode.Single;
				lblRoles.Text = "Role";

				drpCurrency.Visible = false;
				lblCurrency.Visible = false;

			}

			if (Request("page") != null) {
				int page = 0;
				if (int.TryParse(Request("page"), page)) {
					grdUser.PageIndex = page;
				}
			}

			//Moved here by nick into NOT isPostback  - as databinding the grid (again) on postback destroys event handlers
			//You only need databind the grid 'once'

			grdUser.DataSource = dtUsers;
			grdUser.DataBind();

		}

		//grdUser.DataSource = dtUsers
		//grdUser.DataBind()


		OutputErrors(Form.Controls, errormessages, lid, true);
	}

	private clsRole[] GetAvailableRoles(clsRole[] currentRoles)
	{

		object availRoles = agentaccount.HasRight("GLOBALADM") ? iq.i_role_Code.Values.Where(ro => {
			"USER",
			"DISTADMIN",
			"SUPPORT",
			"EDITOR",
			"ADMIN"
		}.Contains(ro.Code)) : iq.i_role_Code.Values.Where(ro => {
			"USER",
			"DISTADMIN"
		}.Contains(ro.Code)).ToList();

		//availRoles = availRoles.Except(Function(r) (roles.Contains(r)))
		availRoles.RemoveAll(r => (currentRoles.Contains(r)));

		return availRoles.ToArray();

	}


	// Returns a string representation of the highest role in the passed list
	private string GetHighestRole(clsRole[] roles)
	{

		string highestRole = string.Empty;

		if (roles == null)
			return highestRole;

		object role = roles.FirstOrDefault(r => r.Code == "USER");
		if (!role == null) {
			highestRole = role.Translation.textTranslation(English);
		}

		role = roles.FirstOrDefault(r => r.Code == "DISTADMIN");
		if (!role == null) {
			highestRole = role.Translation.textTranslation(English);
		}

		role = roles.FirstOrDefault(r => r.Code == "SUPPORT");
		if (!role == null) {
			highestRole = role.Translation.textTranslation(English);
		}

		role = roles.FirstOrDefault(r => r.Code == "EDITOR");
		if (!role == null) {
			highestRole = role.Translation.textTranslation(English);
		}

		role = roles.FirstOrDefault(r => r.Code == "ADMIN");
		if (!role == null) {
			highestRole = role.Translation.textTranslation(English);
		}

		return highestRole;

	}

	private string GetRoleFunction(clsAccount account)
	{


		if (agentaccount.HasRight("GLOBALADM")) {
			// Full Role edit UI
			return string.Format("showRoles(&quot;rolefloater{0}&quot;);return false;", account.User.ID);


		} else {
			// Simple User/Admin toggle
			return string.Format("javascript:__doPostBack(&quot;ToggleRole&quot;, &quot;{0}&quot;)", account.ID);

		}

	}

	// Toggle the passed user between Basic User and Disti Administrator

	public void ToggleRole(int accountID)
	{


		if (iq.Accounts.ContainsKey(accountID)) {
			object account = iq.Accounts(accountID);

			object roles = new List<clsRole>(account.Roles);

			if (roles.Contains(iq.i_role_Code("DISTADMIN"))) {
				account.RemoveRole(iq.i_role_Code("DISTADMIN"));
			} else {
				account.AddRole(iq.i_role_Code("DISTADMIN"));
			}

		}

		Response.Redirect("administration.aspx" + Request.Url.Query);


	}


	private void RemoveMenuItem(MenuItemCollection items, string key)
	{
		MenuItem itemToRemove = null;

		foreach (MenuItem menuItem in items) {
			if (string.Equals(menuItem.Value, key, StringComparison.InvariantCultureIgnoreCase)) {
				itemToRemove = menuItem;
				break; // TODO: might not be correct. Was : Exit For
			}
		}

		if (!itemToRemove == null) {
			items.Remove(itemToRemove);
			adminTabsLine.Style("width") = "681px";
		}

	}


	public void enableUser(CheckBox o, System.EventArgs e)
	{
		CheckBox cb = (CheckBox)o;
		clsUser u = iq.Users(cb.Attributes("uid"));
		u.Disabled = (cb.Checked == false);

		List<string> errormessages = new List<string>();
		u.update(errormessages);


		ulong lid = Request.QueryString("lid");
		OutputErrors(Form.Controls, errormessages, lid, true);

	}

	protected void chkDisabled_CheckedChanged(object sender, EventArgs e)
	{
		CheckBox chkStatus = (CheckBox)sender;
		GridViewRow row = (GridViewRow)chkStatus.NamingContainer;
		int rowindex = row.RowIndex;
		int userid = Convert.ToInt32(grdUser.DataKeys(rowindex).Value);
		clsUser u = iq.Users(userid);
		u.Disabled = chkStatus.Checked;

		List<string> errormessages = new List<string>();
		u.update(errormessages);
		ulong lid = Request.QueryString("lid");
		OutputErrors(Form.Controls, errormessages, lid, true);


	}

	protected void grdUser_PageIndexChanging(object sender, GridViewPageEventArgs e)
	{
		grdUser.PageIndex = e.NewPageIndex;
		grdUser.DataSource = dtUsers;
		grdUser.DataBind();
	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnSave_Click(object sender, EventArgs e)
	{
		string emailDomain = string.Empty;
		string emailName = string.Empty;
		string fullName = string.Empty;
		string telephoneNumber = string.Empty;
		string password = string.Empty;

		emailName = txtEmailName.Text;
		emailDomain = drpDomain.SelectedValue;
		fullName = txtFullName.Text;
		telephoneNumber = TxtTelephone.Text;

		object lid = Request("lid");
		clsAccount agentAccount = iq.sesh(lid, "AgentAccount");

		bool allGood = false;
		//fail safe
		List<string> errorMessages = new List<string>();


		if (!(string.IsNullOrWhiteSpace(emailName) | string.IsNullOrWhiteSpace(fullName))) {
			emailName = emailName + "@" + emailDomain;
			//only create a new user if they dont' already exist (they may pre-exist and have accounts at another channel)
			clsUser user;
			Literal lit = new Literal();

			clsChannel OnChannel = iq.Channels(ddlChannels.SelectedValue);
			// agentaccount.SellerChannel

			//If txtHostOverride.Text <> "" Then
			//    If iq.i_channel_code.ContainsKey(txtHostOverride.Text) Then
			//        OnChannel = iq.i_channel_code(txtHostOverride.Text)
			//    Else
			//        OnChannel = Nothing
			//    End If
			//End If

			if (OnChannel == null) {
				errorMessages.Add("Host Override is invalid");
			} else {
				OnChannel.fixteams(errorMessages);
				//
				if (iq.i_user_email.ContainsKey(emailName)) {
					user = iq.i_user_email(emailName);
					//ok, well the user existed - do they already have an account
					if ((from cA in OnChannel.CustomerAccounts.Valueswhere object.ReferenceEquals(cA.User, user)).Any) {
						lit.Text = "<div><span class='errorLabel'>" + Xlt("An account for that user already exists", English) + "</span></div>";
						Pnl.Controls.Add(lit);
					} else {
						allGood = true;
					}
				} else {
					// Create a new User
					user = new clsUser(OnChannel, emailName, fullName, new nullableString(telephoneNumber), new nullableString());
					allGood = true;
				}


				if (allGood) {
					//Generate a hash password
					object pw = GeneratePassword();
					password = simpleHash(pw);

					if (iq.sesh(lid, "NewUsers") == null) {
						NewCreatedUsers = new List<clsUser>();
					} else {
						NewCreatedUsers = iq.sesh(lid, "NewUsers");
					}
					NewCreatedUsers.Add(user);
					iq.sesh(lid, "NewUsers") = NewCreatedUsers;

					//Check if user type is admin otherwise select role as user
					//Dim userType As String = "user"
					//If chkAdminUser.Checked Then userType = "admin"
					List<clsRole> rolesSelected = new List<clsRole>();
					foreach ( item in lbRoles.Items) {
						if (item.Selected) {
							rolesSelected.Add(iq.Roles(item.value));
						}
					}
					//Dim rolesSelected = lbRoles.GetSelectedIndices().Select(Function(si) iq.Roles(CInt(lbRoles.Items(si).Value))).ToArray ' From r In iq.i_role_Code.Values Where r.Code.ToUpper = userType.ToUpper Select r
					//  Dim role As clsRole = rolesSelected(0)

					// Create  account
					//NB Accoutns are created with the same currency as the user (agentAccount) setting them up !
					//If you create an account in the wrong currency it will see no prices !
					clsCurrency currency = iq.Currencies(drpCurrency.SelectedValue);
					clsAccount account = new clsAccount(user, password, OnChannel, rolesSelected.ToArray, OnChannel.Teams.Values(0), agentAccount.Language, currency, agentAccount.SellerChannel, agentAccount.Priceband, agentAccount.Culture,
					agentAccount.mfrCode);
					Dictionary<string, string> tags = new Dictionary<string, string>();
					string baseurl = ConfigurationManager.AppSettings("BaseURL");

					string url;
					url = baseurl + "/aspx/signin.aspx";

					tags.Add("hostname", OnChannel.DisplayName(agentAccount.Language));
					tags.Add("email", emailName);
					tags.Add("password", pw);
					tags.Add("firstname", fullName);
					tags.Add("url", url);
					tags.Add("extratext", baseurl == "http://uat.hpiquote.net" ? "<p>Please note this is a login for test purposes</p>" : "");
					tags.Add("mfr", agentAccount.mfrCode);
					tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

					List<string> em = new List<string>();
					//Returns any error messages encountered whilst emailing
					if (chkEmailUser.Checked) {
						SendEmail(emailName, "WelcomeEmail.htm", tags, agentAccount.Language, em, false);
					}
					if (chkEmailAdmin.Checked) {
						SendEmail(agentAccount.User.Email, "WelcomeEmail.htm", tags, agentAccount.Language, em, false);
					}
					drpDomain.SelectedIndex = 0;
					if (drpTeams.Items.Count > 0)
						drpTeams.SelectedIndex = 0;
					txtEmailName.Text = "";
					txtFullName.Text = "";
					TxtTelephone.Text = "";

					lit.Text = "<div><span class='messageLabel'>" + Xlt("The user has been successfully created.", English) + "</span></div>";
					Pnl2.Controls.Add(lit);

				}
			}
		}

		OutputErrors(this.Form.Controls, errorMessages, Request("lid"));
		//        Response.Redirect(Request.RawUrl)  'err WHY ? - this is why we can't see errors

	}

	protected void btnPasswordReset_Click(object sender, EventArgs e)
	{
		Button chkStatus = (Button)sender;
		GridViewRow row = (GridViewRow)chkStatus.NamingContainer;
		int rowindex = row.RowIndex;
		int userid = Convert.ToInt32(grdUser.DataKeys(rowindex).Value);
		clsUser u = iq.Users(userid);
		//Do reset here
		object a = u.Accounts.Where(us => object.ReferenceEquals(us.Value.BuyerChannel, ((clsAccount)iq.sesh(Request.QueryString("lid"), "BuyerAccount")).BuyerChannel)).FirstOrDefault;
		if (a.Value != null) {
			a.Value.ResetPassword();
		}
	}

	protected void btnAddRole_Click(object sender, EventArgs e)
	{
		Button chkStatus = (Button)sender;
		GridViewRow row = (GridViewRow)chkStatus.NamingContainer;
		int rowindex = row.RowIndex;
		int userid = Convert.ToInt32(grdUser.DataKeys(rowindex).Value);
		clsUser u = iq.Users(userid);
		object a = u.Accounts.Where(us => object.ReferenceEquals(us.Value.BuyerChannel, ((clsAccount)iq.sesh(Request.QueryString("lid"), "BuyerAccount")).BuyerChannel)).FirstOrDefault;
		if (a.Value != null) {
			a.Value.AddRole(iq.i_role_Code(sender.parent.parent.cells(9).controls(7).selecteditem.value));
			Response.Redirect("administration.aspx?lid=" + Request.QueryString("lid") + "&page=" + grdUser.PageIndex);
		}
	}

	protected void btnRemoveRole_Click(object sender, EventArgs e)
	{
		Button chkStatus = (Button)sender;
		GridViewRow row = (GridViewRow)chkStatus.NamingContainer;
		int rowindex = row.RowIndex;
		int userid = Convert.ToInt32(grdUser.DataKeys(rowindex).Value);
		clsUser u = iq.Users(userid);
		object a = u.Accounts.Where(us => object.ReferenceEquals(us.Value.BuyerChannel, ((clsAccount)iq.sesh(Request.QueryString("lid"), "BuyerAccount")).BuyerChannel)).FirstOrDefault;
		if (a.Value != null) {
			a.Value.RemoveRole(iq.i_role_Code(sender.parent.parent.cells(9).controls(1).selecteditem.value));
			Response.Redirect("administration.aspx?lid=" + Request.QueryString("lid") + "&page=" + grdUser.PageIndex);
		}
	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnMultisend_Click(object sender, EventArgs e)
	{
		//For each account - at the host - with the email address
		//reset the password and send the welcome mail

		List<string> errorMessages = new List<string>();
		if (!iq.i_channel_code.ContainsKey(txtMultiHost.Text)) {
			errorMessages.Add(txtMultiHost.Text + " is not a valid channel Code");
		} else {
			clsChannel host = iq.i_channel_code(txtMultiHost.Text);

			foreach ( Usr in host.Users.Values) {
				if (Usr.Email.Contains("@")) {
					if (LCase(TxtMultisend.Text + ",").Contains(LCase(Split(Usr.Email, "@")(0)))) {
						foreach ( account in Usr.Accounts.Values) {

							if (object.ReferenceEquals(account.SellerChannel, host)) {
								string ru = Request.RawUrl;
								string baseurl = Left(ru, InStr(ru, "admin.aspx") - 1);

								string url;
								url = baseurl + "signin.aspx";

								Dictionary<string, string> tags = new Dictionary<string, string>();

								if (chkMultiDoit.Checked) {
									object pw = GeneratePassword();
									ulong passwordHash = simpleHash(pw);
									account.Password = passwordHash.ToString;

									account.update(errorMessages);

									tags.Add("hostname", agentaccount.SellerChannel.DisplayName(agentaccount.Language));
									tags.Add("email", Usr.Email);
									tags.Add("password", pw);
									tags.Add("firstname", Usr.RealName);
									tags.Add("url", url);
									tags.Add("mfr", agentaccount.mfrCode);
									tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

									SendEmail(Usr.Email, "WelcomeEmail.htm", tags, agentaccount.Language, errorMessages, true);
								} else {
									PnlMultiSend.Controls.Add(NewLit("Will do: " + Usr.Email + "<br/>"));
								}
							}
						}
					}
				}
			}

		}


		OutputErrors(PnlMultiSend.Controls, errorMessages, Request("Lid"));

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnGetStubs_Click(object sender, EventArgs e)
	{
		List<clsUser> todo = new List<clsUser>();

		//for everyone in the domain .. add the internal users

		clsChannel channel = iq.i_channel_code(txtMultiHost.Text);
		//(From j In iq.Users.Values Where j.Email.ToLower.Contains(TxtMultSendDomain.Text.ToLower))
		foreach ( Usr in channel.Users.Values) {
			//only 'internal' users
			if (Usr.Channel.Code == txtMultiHost.Text) {
				todo.Add(Usr);
			}
		}

		if (todo.Count > 0) {
			TxtMultisend.Text = Join((from j in todoj.Email).ToArray, ",");
			txtMultiHost.Text = todo.First.Channel.Code;
		}


	}



	protected void btnSearch_Click(object sender, EventArgs e)
	{
	}


	protected void btnOnlyDistiAdmin_Click(object sender, EventArgs e)
	{
	}


	protected void ddlChannels_SelectedIndexChanged(object sender, EventArgs e)
	{
		drpTeams.DataSource = iq.Channels(ddlChannels.SelectedValue).Teams.Values;
		drpTeams.DataTextField = "Name";
		drpTeams.DataValueField = "ID";
		drpTeams.DataBind();

		drpCurrency.SelectedValue = iq.Channels(ddlChannels.SelectedValue).DefaultCurrency != null ? iq.Channels(ddlChannels.SelectedValue).DefaultCurrency.ID : iq.i_currency_code("GBP").ID;
	}

	protected void btnWelcomeResend_Click(object sender, EventArgs e)
	{
		Button chkStatus = (Button)sender;
		GridViewRow row = (GridViewRow)chkStatus.NamingContainer;
		int rowindex = row.RowIndex;
		int userid = Convert.ToInt32(grdUser.DataKeys(rowindex).Value);
		clsUser u = iq.Users(userid);
		//Do reset here
		object a = u.Accounts.Where(us => object.ReferenceEquals(us.Value.BuyerChannel, ((clsAccount)iq.sesh(Request.QueryString("lid"), "BuyerAccount")).BuyerChannel)).FirstOrDefault;
		if (a.Value != null) {
			a.Value.ResendWelcomeEmail();
		}
	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnAddSystemMessage_Click(object sender, EventArgs e)
	{
		// Create translation
		clsLanguage kyLanguage = (from l in iq.Languages.Valueswhere l.Code == "KY").First;
		clsTranslation translation = new clsTranslation(kyLanguage, SystemMessage);

		// Create message
		DateTime validFrom;
		DateTime validTo;
		bool enabled = chkSystemMessage.Checked;

		if (!DateTime.TryParse(txtSystemMessageValidFrom.Text, validFrom)) {
			validFrom = new DateTime(Today.Year, 1, 1);
			txtSystemMessageValidFrom.Text = validFrom.ToString("dd-MMM-yyyy");
		}

		if (!DateTime.TryParse(txtSystemMessageValidTo.Text, validTo)) {
			validTo = new DateTime(Today.Year, 12, 31);
			txtSystemMessageValidTo.Text = validTo.ToString("dd-MMM-yyyy");
		}

		clsMessage message = new clsMessage("SignInScreenMessage", translation, validFrom, validTo, enabled, 1);

		if (!iq.UserMessages == null) {
			if (!iq.UserMessages.ContainsKey("SignInScreenMessage")) {
				iq.UserMessages.Add("SignInScreenMessage", new List<clsMessage>());
			}
			iq.UserMessages("SignInScreenMessage").Add(message);
		}

		btnAmendSystemMessage.Visible = true;
		//btnDeleteSystemMessage.Visible = True
		btnAddSystemMessage.Visible = false;

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnAmendSystemMessage_Click(object sender, EventArgs e)
	{
		if (!iq.UserMessages == null) {
			if (iq.UserMessages.ContainsKey("SignInScreenMessage")) {

				if (iq.UserMessages("SignInScreenMessage").Count == 1) {
					clsLanguage kyLanguage = (from l in iq.Languages.Valueswhere l.Code == "KY").First;

					 // ERROR: Not supported in C#: WithStatement


					DateTime validFrom = DateTime.Parse(txtSystemMessageValidFrom.Text);
					DateTime validTo = DateTime.Parse(txtSystemMessageValidTo.Text);
					bool enabled = chkSystemMessage.Checked;

					 // ERROR: Not supported in C#: WithStatement


				}
			}
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnDeleteSystemMessage_Click(object sender, EventArgs e)
	{
		if (!iq.UserMessages == null) {

			if (iq.UserMessages.ContainsKey("SignInScreenMessage")) {
				txtSignInSystemMessage.Text = string.Empty;
				txtSystemMessageValidFrom.Text = string.Empty;
				txtSystemMessageValidTo.Text = string.Empty;
				btnAmendSystemMessage.Visible = false;
				btnDeleteSystemMessage.Visible = false;
				btnAddSystemMessage.Visible = true;

				// UI actually only supports one message for now
				foreach (clsMessage message in iq.UserMessages("SignInScreenMessage")) {
					message.delete(null);
				}

				iq.UserMessages("SignInScreenMessage").Clear();

			}
		}

	}

	private string SystemMessage {


		get {
			string message = txtSignInSystemMessage.Text;

			message = message.Replace(Environment.NewLine, "<br />");
			message = Server.HtmlEncode(message);

			SystemMessage = message;

		}


		set {
			string message = value;

			message = Server.HtmlDecode(message);
			message = message.Replace("<br />", Environment.NewLine);

			txtSignInSystemMessage.Text = message;

		}
	}



	protected void  // ERROR: Handles clauses are not supported in C#
btnAddHpeSystemMessage_Click(object sender, EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");
		clsAccount agentaccount = iq.sesh(lid, "AgentAccount");

		// Create translation
		clsTranslation translation = new clsTranslation(agentaccount.Language, HPESystemMessage);

		// Create message
		DateTime validFrom;
		DateTime validTo;
		bool enabled = hpeMessageEnabled.Checked;

		if (!DateTime.TryParse(txtHpeSystemMessageValidFrom.Text, validFrom)) {
			validFrom = new DateTime(Today.Year, 1, 1);
			txtHpeSystemMessageValidFrom.Text = validFrom.ToString("dd-MMM-yyyy");
		}

		if (!DateTime.TryParse(txtHpeSystemMessageValidTo.Text, validTo)) {
			validTo = new DateTime(Today.Year, 12, 31);
			txtHpeSystemMessageValidTo.Text = validTo.ToString("dd-MMM-yyyy");
		}

		clsMessage message = new clsMessage("HPESystemMessage", translation, validFrom, validTo, enabled, 1);

		if (!iq.UserMessages == null) {
			if (!iq.UserMessages.ContainsKey("HPESystemMessage")) {
				iq.UserMessages.Add("HPESystemMessage", new List<clsMessage>());
			}
			iq.UserMessages("HPESystemMessage").Add(message);
		}

		btnAmendHpeSystemMessage.Visible = true;
		//btnDeleteHpeSystemMessage.Visible = True
		btnAddHpeSystemMessage.Visible = false;

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnAddHpiSystemMessage_Click(object sender, EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");
		clsAccount agentaccount = iq.sesh(lid, "AgentAccount");

		// Create translation
		clsTranslation translation = new clsTranslation(agentaccount.Language, HPISystemMessage);

		// Create message
		DateTime validFrom;
		DateTime validTo;
		bool enabled = hpiMessageEnabled.Checked;

		if (!DateTime.TryParse(txtHpiSystemMessageValidFrom.Text, validFrom)) {
			validFrom = new DateTime(Today.Year, 1, 1);
			txtHpiSystemMessageValidFrom.Text = validFrom.ToString("dd-MMM-yyyy");
		}

		if (!DateTime.TryParse(txtHpiSystemMessageValidTo.Text, validTo)) {
			validTo = new DateTime(Today.Year, 12, 31);
			txtHpiSystemMessageValidTo.Text = validTo.ToString("dd-MMM-yyyy");
		}

		clsMessage message = new clsMessage("HPISystemMessage", translation, validFrom, validTo, enabled, 1);

		if (!iq.UserMessages == null) {
			if (!iq.UserMessages.ContainsKey("HPISystemMessage")) {
				iq.UserMessages.Add("HPISystemMessage", new List<clsMessage>());
			}
			iq.UserMessages("HPISystemMessage").Add(message);
		}

		btnAmendHpiSystemMessage.Visible = true;
		//btnDeleteHpiSystemMessage.Visible = True
		btnAddHpiSystemMessage.Visible = false;

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnAmendHpeSystemMessage_Click(object sender, EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");
		clsAccount agentaccount = iq.sesh(lid, "AgentAccount");

		if (!iq.UserMessages == null) {
			if (iq.UserMessages.ContainsKey("HPESystemMessage")) {

				if (iq.UserMessages("HPESystemMessage").Count == 1) {
					 // ERROR: Not supported in C#: WithStatement


					DateTime validFrom = DateTime.Parse(txtHpeSystemMessageValidFrom.Text);
					DateTime validTo = DateTime.Parse(txtHpeSystemMessageValidTo.Text);
					bool enabled = hpeMessageEnabled.Checked;

					 // ERROR: Not supported in C#: WithStatement


				}
			}
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnAmendHpiSystemMessage_Click(object sender, EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");
		clsAccount agentaccount = iq.sesh(lid, "AgentAccount");

		if (!iq.UserMessages == null) {
			if (iq.UserMessages.ContainsKey("HPISystemMessage")) {

				if (iq.UserMessages("HPISystemMessage").Count == 1) {
					 // ERROR: Not supported in C#: WithStatement


					DateTime validFrom = DateTime.Parse(txtHpiSystemMessageValidFrom.Text);
					DateTime validTo = DateTime.Parse(txtHpiSystemMessageValidTo.Text);
					bool enabled = hpiMessageEnabled.Checked;

					 // ERROR: Not supported in C#: WithStatement


				}
			}
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnDeleteHpeSystemMessage_Click(object sender, EventArgs e)
	{
		if (!iq.UserMessages == null) {

			if (iq.UserMessages.ContainsKey("HPESystemMessage")) {
				txtHpeSystemMessage.Text = string.Empty;
				txtHpeSystemMessageValidFrom.Text = string.Empty;
				txtHpeSystemMessageValidTo.Text = string.Empty;
				btnAmendHpeSystemMessage.Visible = false;
				btnDeleteHpeSystemMessage.Visible = false;
				btnAddHpeSystemMessage.Visible = true;

				// UI actually only supports one message for now
				foreach (clsMessage message in iq.UserMessages("HPESystemMessage")) {
					message.delete(null);
				}

				iq.UserMessages("HPESystemMessage").Clear();

			}
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnDeleteHpiSystemMessage_Click(object sender, EventArgs e)
	{
		if (!iq.UserMessages == null) {

			if (iq.UserMessages.ContainsKey("HPISystemMessage")) {
				txtHpiSystemMessage.Text = string.Empty;
				txtHpiSystemMessageValidFrom.Text = string.Empty;
				txtHpiSystemMessageValidTo.Text = string.Empty;
				btnAmendHpiSystemMessage.Visible = false;
				btnDeleteHpiSystemMessage.Visible = false;
				btnAddHpiSystemMessage.Visible = true;

				// UI actually only supports one message for now
				foreach (clsMessage message in iq.UserMessages("HPISystemMessage")) {
					message.delete(null);
				}

				iq.UserMessages("HPISystemMessage").Clear();

			}
		}

	}

	private string HPESystemMessage {


		get {
			string message = txtHpeSystemMessage.Text;

			message = message.Replace(Environment.NewLine, "<br />");
			message = Server.HtmlEncode(message);

			HPESystemMessage = message;

		}


		set {
			string message = value;

			message = Server.HtmlDecode(message);
			message = message.Replace("<br />", Environment.NewLine);

			txtHpeSystemMessage.Text = message;

		}
	}


	private string HPISystemMessage {


		get {
			string message = txtHpiSystemMessage.Text;

			message = message.Replace(Environment.NewLine, "<br />");
			message = Server.HtmlEncode(message);

			HPISystemMessage = message;

		}


		set {
			string message = value;

			message = Server.HtmlDecode(message);
			message = message.Replace("<br />", Environment.NewLine);

			txtHpiSystemMessage.Text = message;

		}
	}



	protected void  // ERROR: Handles clauses are not supported in C#
AdminMenu_MenuItemClick(object sender, MenuEventArgs e)
	{
		// Switch tab
		switch (e.Item.Value.ToLower()) {

			case "useradmin":

				adminMultiView.SetActiveView(tabUserAdmin);
			case "createuser":

				adminMultiView.SetActiveView(tabCreateUser);
			case "system":

				adminMultiView.SetActiveView(tabSystem);
			case "reports":

				adminMultiView.SetActiveView(tabReports);
		}

	}


	protected void HpiMessageEnabled_CheckedChanged(object sender, EventArgs e)
	{
		ToggleMessageEnabled("HPISystemMessage", hpiMessageEnabled);

	}


	protected void hpeMessageEnabled_CheckedChanged(object sender, EventArgs e)
	{
		ToggleMessageEnabled("HPESystemMessage", hpeMessageEnabled);

	}


	protected void chkSystemMessage_CheckedChanged(object sender, EventArgs e)
	{
		ToggleMessageEnabled("SignInScreenMessage", chkSystemMessage);

	}


	private void ToggleMessageEnabled(string userMessage, CheckBox checkbox)
	{
		UInt64 lid = Request.QueryString("lid");
		clsAccount agentaccount = iq.sesh(lid, "AgentAccount");

		if (!iq.UserMessages == null) {
			if (iq.UserMessages.ContainsKey(userMessage)) {

				if (iq.UserMessages(userMessage).Count == 1) {
					 // ERROR: Not supported in C#: WithStatement


				}
			}
		}

	}


	protected void roleSelect_Click(object sender, EventArgs e)
	{
		CommandEventArgs cme = e as CommandEventArgs;


		if (cme != null) {
			int accountID = Int32.Parse(cme.CommandArgument);

			object account = iq.Accounts(accountID);
			object roles = account.Roles;

			Page.ClientScript.RegisterStartupScript(this.GetType(), "RoleSelector", "ShowRoleSelector();", true);



		}

	}
}

