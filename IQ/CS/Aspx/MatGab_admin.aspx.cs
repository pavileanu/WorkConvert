
using System.Reflection;
using dataAccess;

public class WebForm1 : System.Web.UI.Page
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
	public List<clsRole> AvailableRoles;

	private List<clsUser> NewCreatedUsers;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");
		if (!iq.SeshAlive(lid) || !(AccountHasRight(lid, "ADMINMENU") | AccountHasRight(lid, "GLOBALADM"))) {
			Response.Redirect("signin.aspx");
		}

		AvailableRoles = iq.i_role_Code.Values.ToList;




		//If Not Page.IsPostBack Then
		dtUsers = new DataTable();
		DataTable dtActivity = new DataTable();
		dtUsers.Columns.Add("ID", typeof(int));
		dtUsers.Columns.Add("RealName", typeof(string));
		dtUsers.Columns.Add("ChannelName", typeof(string));
		dtUsers.Columns.Add("Disabled", typeof(bool));
		dtUsers.Columns.Add("Quotes", typeof(int));
		dtUsers.Columns.Add("Options", typeof(int));
		dtUsers.Columns.Add("Systems", typeof(int));
		dtUsers.Columns.Add("Pitch", typeof(decimal));
		dtUsers.Columns.Add("LastUsed", typeof(string));
		dtUsers.Columns.Add("Roles", typeof(clsRole[]));


		dtActivity.Columns.Add("Type", typeof(string));
		dtActivity.Columns.Add("Today", typeof(int));
		dtActivity.Columns.Add("7days", typeof(int));
		dtActivity.Columns.Add("MTD", typeof(int));
		dtActivity.Columns.Add("LastMonth", typeof(int));
		//End If
		agentaccount = iq.sesh(lid, "AgentAccount");
		buyeraccount = iq.sesh(lid, "BuyerAccount");

		if (agentaccount == null)
			Response.Redirect("signin.aspx");
		//Dim dtUsers As New  DataTable
		int userToday = 0;
		int user7Days = 0;
		int userMTD = 0;
		int userLastMonth = 0;
		int[,] quoteSummary = new int[3, 3];

		if (agentaccount.HasRight("DISABLEUSR"))
			AccountCanDisableUsers = true;
		if (agentaccount.HasRight("CREATEUSR"))
			AccountCanSetupUsers = true;
		if (agentaccount.HasRight("PWDRESET"))
			AccountCanResetPasswords = true;
		if (agentaccount.HasRight("DISTADMIN") | agentaccount.HasRight("GLOBALADM"))
			AccountIsDistiAdmin = true;

		if (agentaccount.HasRight("GLOBALADM")) {
			PnlMultiSend.Visible = true;
			PnlHostOverride.Visible = true;
		}



		if (!IsPostBack) {
			txtMultiHost.Text = agentaccount.SellerChannel.Code;
		}

		List<string> errormessages = new List<string>();


		if (!IsPostBack) {
			object list = from j in iq.Accounts.Valueswhere object.ReferenceEquals(j.SellerChannel, agentaccount.SellerChannel);

			int cnt = list.Count;

			//run a select to find out which accounts have a quote in the last 60 days . .

			SqlClient.SqlConnection con;
			con = da.OpenDatabase;

			object sql = "SELECT distinct(fk_account_id_agent) FROM quote q ";
			sql += "JOIN account a ON q.fk_account_id_agent=a.id  ";
			sql += "WHERE updated>getdate()-100 AND a.fk_channel_id_seller=" + agentaccount.SellerChannel.ID;

			HashSet<int> toReportOn = new HashSet<int>();
			SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);
			while (rdr.Read) {
				toReportOn.Add(rdr.Item(0));
			}
			rdr.Close();



			//This is WAY too slow - there are MANY accounts 5k for westcoast.. (and only a few quotes atm)

			foreach ( account in list) {
				//Therse are acocunts which did a quote in the last 100 days
				if (toReportOn.Contains(account.ID)) {
					int optionCount = 0;
					int systemCount = 0;
					decimal pitchRate = 0;
					System.DateTime lastDate;

					account.LoadQuotes(0);
					if (account.Quotes.Values.Count > 0) {
						//NA - was getting 'collection was modified;' cannot enumerate - type messages - added .tolist
						foreach ( quotedetails in account.Quotes.Values.ToList) {
							//This report does'n involve any quote older than 2 months
							if (quotedetails.Updated > System.DateTime.Today.AddDays(-62)) {
								quotedetails.LoadItems(errormessages);
								clsFlatList flatList = quotedetails.RootItem.Flattened(true, false, 0);
								foreach (clsFlatListItem item in flatList.items) {
									//Debug.WriteLine(account.User.RealName & "  :  " & item.Quantity & "  :  " & item.QuoteItem.Branch.Product.isSystem)
									if (!item.QuoteItem.Branch.Product.isSystem) {
										optionCount += item.Quantity;
									} else {
										systemCount += item.Quantity;
									}
								}
								if (quotedetails.Created > lastDate)
									lastDate = quotedetails.Created;
								switch (quotedetails.State.code) {
									case "#NW":
										if (quotedetails.Created == System.DateTime.Today)
											quoteSummary(1, 0) += 1;
										if (quotedetails.Created > System.DateTime.Today.AddDays(-7))
											quoteSummary(1, 1) += 1;
										if (quotedetails.Created > System.DateTime.Today.AddDays(-30))
											quoteSummary(1, 2) += 1;
										if (quotedetails.Created.Month == System.DateTime.Today.Month - 1)
											quoteSummary(1, 3) += 1;
									case "#CV":
										if (quotedetails.Created == System.DateTime.Today)
											quoteSummary(2, 0) += 1;
										if (quotedetails.Created > System.DateTime.Today.AddDays(-7))
											quoteSummary(2, 1) += 1;
										if (quotedetails.Created > System.DateTime.Today.AddDays(-30))
											quoteSummary(2, 2) += 1;
										if (quotedetails.Created.Month == System.DateTime.Today.Month - 1)
											quoteSummary(2, 3) += 1;
									case "#QS":
										if (quotedetails.Created == System.DateTime.Today)
											quoteSummary(3, 0) += 1;
										if (quotedetails.Created > System.DateTime.Today.AddDays(-7))
											quoteSummary(3, 1) += 1;
										if (quotedetails.Created > System.DateTime.Today.AddDays(-30))
											quoteSummary(3, 2) += 1;
										if (quotedetails.Created.Month == System.DateTime.Today.Month - 1)
											quoteSummary(3, 3) += 1;
								}
							}
						}
						if (systemCount == 0)
							systemCount = 1;
						// otherwise we'll get a DBZ ! (WE DO!)
						pitchRate = optionCount / systemCount;
						pitchRate = decimal.Round(pitchRate, 2);
					}
					string dateString = IIf(lastDate == null, "", lastDate.ToString("yyyy-MM-dd"));
					dtUsers.Rows.Add(account.User.ID, account.User.RealName, account.BuyerChannel.Name, account.User.Disabled, account.Quotes.Values.Count, optionCount, systemCount, pitchRate, dateString, account.Roles);
				}
			}
			dtActivity.Rows.Add("Users", quoteSummary(0, 0), quoteSummary(0, 1), quoteSummary(0, 2), quoteSummary(0, 3));
			dtActivity.Rows.Add("Quotes - New", quoteSummary(1, 0), quoteSummary(1, 1), quoteSummary(1, 2), quoteSummary(1, 3));
			dtActivity.Rows.Add("Quotes - Saved", quoteSummary(2, 0), quoteSummary(2, 1), quoteSummary(2, 2), quoteSummary(2, 3));
			dtActivity.Rows.Add("Quotes - Exported", quoteSummary(3, 0), quoteSummary(3, 1), quoteSummary(3, 2), quoteSummary(3, 3));

			dtUsers.DefaultView.Sort = "Quotes DESC";
			iq.sesh(lid, "UserDetailsTable") = dtUsers;
			iq.sesh(lid, "ActivitySummaryTable") = dtActivity;

			agentaccount.SellerChannel.fixteams(errormessages);



			drpTeams.DataSource = agentaccount.SellerChannel.Teams.Values;
			drpTeams.DataTextField = "Name";
			drpTeams.DataValueField = "ID";
			drpTeams.DataBind();

			drpDomain.DataSource = agentaccount.SellerChannel.Domains;
			drpDomain.DataBind();

			drpDomain.Items.Add("gmail.com");
			drpDomain.Items.Add("channelcentral.net");

			drpCurrency.DataSource = iq.Currencies.Values;
			drpCurrency.DataTextField = "Code";
			drpCurrency.DataValueField = "ID";
			drpCurrency.DataBind();


		} else {
			dtUsers = iq.sesh(lid, "UserDetailsTable");


			dtActivity = iq.sesh(lid, "ActivitySummaryTable");

		}
		if (iq.sesh(lid, "NewUsers") != null) {
			NewCreatedUsers = iq.sesh(lid, "NewUsers");
			object list = (from j in iq.Accounts.Valueswhere object.ReferenceEquals(j.SellerChannel, agentaccount.SellerChannel)).ToList();
			foreach ( usr in NewCreatedUsers) {
				clsAccount account = (from k in listwhere k.User.ID == usr.ID).FirstOrDefault;
				dtUsers.Rows.Add(usr.ID, usr.RealName, account.BuyerChannel.Name, usr.Disabled, 0, 0, 0, 0.0, Today.ToString("yyyy-MM-dd"), account.Roles);
			}
		}

		grdActivity.DataSource = dtActivity;
		grdActivity.DataBind();
		//grdUser.DataSource = agentaccount.sellerchannel.users.values
		grdUser.DataSource = dtUsers;
		grdUser.DataBind();

		OutputErrors(Form.Controls, errormessages, lid, true);

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
	protected void chkDisabled_CheckedChanged1(object sender, EventArgs e)
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
		//  emailDomain = drpDomain.SelectedValue
		fullName = txtFullName.Text;
		telephoneNumber = TxtTelephone.Text;

		bool allGood = false;
		//fail safe
		List<string> errorMessages__1 = new List<string>();


		if (!(string.IsNullOrWhiteSpace(emailName) | string.IsNullOrWhiteSpace(fullName))) {
			//emailName = emailName & "@" & emailDomain
			//only create a new user if they dont' already exist (they may pre-exist and have accounts at another channel)
			clsUser user;
			Literal lit = new Literal();

			clsChannel OnChannel = agentaccount.SellerChannel;

			if (txtHostOverride.Text != "") {
				if (iq.i_channel_code.ContainsKey(txtHostOverride.Text)) {
					OnChannel = iq.i_channel_code(txtHostOverride.Text);
				} else {
					OnChannel = null;
				}
			}

			if (OnChannel == null) {
				errormessages.Add("Host Override is invalid");
			} else {
				OnChannel.fixteams(errorMessages__1);
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

					UInt64 lid = Request.QueryString("lid");
					if (iq.sesh(lid, "NewUsers") == null) {
						NewCreatedUsers = new List<clsUser>();
					} else {
						NewCreatedUsers = iq.sesh(lid, "NewUsers");
					}
					NewCreatedUsers.Add(user);
					iq.sesh(lid, "NewUsers") = NewCreatedUsers;

					//Check if user type is admin otherwise select role as user
					string userType = "user";
					if (chkAdminUser.Checked)
						userType = "admin";
					object rolesSelected = from r in iq.i_role_Code.Valueswhere r.Code.ToUpper == userType.ToUpperr;
					clsRole role = rolesSelected(0);

					// Create  account
					//NB Accoutns are created with the same currency as the user (agentAccount) setting them up !
					//If you create an account in the wrong currency it will see no prices !
					clsCurrency currency = iq.Currencies(drpCurrency.SelectedValue);

					clsAccount account = new clsAccount(user, password, OnChannel, { role }, OnChannel.Teams.Values(0), agentaccount.Language, currency, OnChannel, agentaccount.Priceband, agentaccount.Culture,
					agentaccount.mfrCode);
					Dictionary<string, string> tags = new Dictionary<string, string>();
					string baseurl = ConfigurationManager.AppSettings("BaseURL");

					string url;
					url = baseurl + "/aspx/signin.aspx";

					tags.Add("hostname", OnChannel.DisplayName(agentaccount.Language));
					tags.Add("email", emailName);
					tags.Add("password", pw);
					tags.Add("firstname", fullName);
					tags.Add("url", url);
					tags.Add("mfr", buyeraccount.mfrCode);
					tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

					List<string> em = new List<string>();
					//Returns any error messages encountered whilst emailing
					if (chkEmailUser.Checked) {
						SendEmail(emailName, "WelcomeEmail.htm", tags, agentaccount.Language, em, false);
					}
					if (chkEmailAdmin.Checked) {
						SendEmail(agentaccount.User.Email, "WelcomeEmail.htm", tags, agentaccount.Language, em, false);
					}
					drpDomain.SelectedIndex = 0;
					drpTeams.SelectedIndex = 0;
					txtEmailName.Text = "";
					txtFullName.Text = "";
					TxtTelephone.Text = "";

				}
			}
		}

		OutputErrors(this.Form.Controls, errormessages, Request("lid"));


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
			a.Value.AddRole(iq.i_role_Code(sender.parent.parent.cells(10).controls(7).selecteditem.value));
			//For reload lets frig the source
			Response.Redirect("admin.aspx" + Request.Url.Query);
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
			a.Value.RemoveRole(iq.i_role_Code(sender.parent.parent.cells(10).controls(1).selecteditem.value));
			Response.Redirect("admin.aspx" + Request.Url.Query);
		}
	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnMultisend_Click(object sender, EventArgs e)
	{
		//For each account - at the host - with the email address
		//reset the password and send the welcome mail

		List<string> errorMessages__1 = new List<string>();
		if (!iq.i_channel_code.ContainsKey(txtMultiHost.Text)) {
			errorMessages__1.Add(txtMultiHost.Text + " is not a valid channel Code");
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

									account.update(errorMessages__1);

									tags.Add("hostname", agentaccount.SellerChannel.DisplayName(agentaccount.Language));
									tags.Add("email", Usr.Email);
									tags.Add("password", pw);
									tags.Add("firstname", Usr.RealName);
									tags.Add("url", url);
									tags.Add("mfr", agentaccount.mfrCode);
									tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

									SendEmail(Usr.Email, "WelcomeEmail.htm", tags, agentaccount.Language, errorMessages__1, true);
								} else {
									PnlMultiSend.Controls.Add(NewLit("Will do: " + Usr.Email + "<br/>"));
								}
							}
						}
					}
				}
			}

		}


		OutputErrors(PnlMultiSend.Controls, errormessages, Request("Lid"));

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
}

