//Option Strict On
using IQ.clsBranchState;
using System.Net.Mail;
using System.Linq;
using System.Threading;
using System.Reflection;

public class Site : System.Web.UI.MasterPage
{
	public string submitString = "Submit Feedback";

	clsLanguage language;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid"), lid);

		Page.Title = "iQuote";

		clsAccount agentAccount = null;
		if (lid != 0) {
			if (iq.SeshContains(lid, "AgentAccount")) {
				agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
			}
		}
		chkTax.Attributes.Add("onclick", "return HandleOnCheck()");
		// hpSPLIT dynamic stylesheet - see also updateHeader(css)
		// SK - expanded to handle manufacturer-specific style sheet selection for the Universal sign-in pages
		// SK - now also handles channelcentral.css on the SignIn and Accounts pages to override the HP styles
		Literal css = new Literal();
		string mfrCode = null;
		bool universal = false;
		string stylesheet = null;
		string s = null;



		if (agentAccount == null || string.IsNullOrEmpty(agentAccount.mfrCode)) {
			// Not logged in yet, but we might be on a manufacturer-specific HP Universal sign-in page, in which case
			// we want to pick the HPE/HPI-specific style sheet before login

			// Create a case-insensitive dictionary for the Request parameters; could be used more widely
			Dictionary<string, string> requestParams = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (string key in Request.QueryString) {
				if (!key == null)
					requestParams.Add(key, Request.QueryString(key));
			}

			// Read or infer the MFR code to determine whether we can use a HPE or HPI specific stylesheet
			if (iq.SeshContains(lid, "MFR")) {
				Manufacturer mfr = iq.sesh(lid, "MFR");
				if (mfr == Manufacturer.HPE) {
					mfrCode = "HPE";
				} else if (mfr == Manufacturer.HPI) {
					mfrCode = "HPI";
				}
			} else if (requestParams.ContainsKey("base")) {
				string sku = requestParams("base");
				if (iq.i_SKU.ContainsKey(sku)) {
					object product = iq.i_SKU(sku);
					if (product.Manufacturer != Manufacturer.Unknown) {
						mfrCode = product.mfrCode;
					}
				}


			} else if (requestParams.ContainsKey("mfr") | requestParams.ContainsKey("mfg")) {
				if (requestParams.ContainsKey("mfr")) {
					mfrCode = requestParams("mfr");
				} else if (requestParams.ContainsKey("mfg")) {
					mfrCode = requestParams("mfg");
				}

				if ((!string.Equals(mfrCode, "hpi", StringComparison.InvariantCultureIgnoreCase)) && (!string.Equals(mfrCode, "hpe", StringComparison.InvariantCultureIgnoreCase))) {
					mfrCode = null;
				}
			} else {
				// Might be able to infer the manufacturer from the referrer URL
				mfrCode = InferUniversalManufacturer(Request);
				if (!string.IsNullOrEmpty(mfrCode))
					universal = true;
			}

			if (!string.IsNullOrEmpty(mfrCode)) {
				stylesheet = string.Format("Site-{0}", mfrCode);
			} else {
				stylesheet = "channelcentral";
			}
		} else {
			mfrCode = agentAccount.mfrCode;
			stylesheet = string.Format("Site-{0}", mfrCode);
		}


		if (!string.IsNullOrEmpty(mfrCode)) {
			if ((string.Equals(mfrCode, "hpi", StringComparison.InvariantCultureIgnoreCase))) {
				Page.Title = "iQuote - HP Inc.";
			} else if ((string.Equals(mfrCode, "hpe", StringComparison.InvariantCultureIgnoreCase))) {
				Page.Title = "iQuote - Hewlett Packard Enterprise";
			}

		}

		css.Text = string.Format("<link href='{0}Styles/{1}.css' rel='stylesheet' type='text/css' />", ResolveUrl("~/"), stylesheet);
		Page.Header.Controls.Add(css);

		//This WAS the static version (in the designer)
		//<link href="<%# ResolveUrl("~/") %>Styles/Site.css" rel="stylesheet" type="text/css" /> 

		if (!clsIQ.IsLoaded) {
			if (string.IsNullOrEmpty(mfrCode)) {
				Response.Redirect("Loading.aspx?path=" + Request.Url.AbsoluteUri, false);
				return;
			} else {
				Response.Redirect(string.Format("Loading.aspx?path={0}&mfr={1}", Request.Url.AbsoluteUri, mfrCode), false);
				return;
			}
		}

		if (Application("IQ") == null)
			Application("IQ") = iq;

		if (Request("elevate") != "" & !Request.Url.AbsoluteUri.Contains("signin.aspx")) {
			Response.Redirect("signin.aspx?lid=" + lid + "&elevate=1", false);
			return;
		}

		Assembly a = Assembly.GetExecutingAssembly;

		litVersion.Text = a.GetName.Version.ToString;

		// Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1))
		// Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache)
		// Response.Cache.SetNoStore()
		BtnFeedback.Visible = true;
		UpdateHeader(css);
		//shows user name info and signed in status

		//quick fix for the fact the tools manned was visible whne swtiching accounts
		if (InStr(LCase(this.Parent.ToString), "accounts_aspx") > 0)
			newHeader.Visible = false;

		if (InStr(LCase(this.Parent.ToString), "universal_aspx") > 0) {
			loginDisplay.Visible = false;
			newHeader.Visible = false;
			litSubmit.Visible = false;
			legalLink.Value = "";
			litVersion.Visible = false;
			legalLink.Visible = false;
		}
		if (agentAccount != null && agentAccount.BuyerChannel.Region.Code == "BR") {
			wareHouseli.Visible = true;
		} else {
			wareHouseli.Visible = false;
		}

		if (InStr(LCase(this.Parent.ToString), "signin_aspx") > 0) {
		//we're on the sign in page - hide the signOut Button
		//BtnSignOut.Visible = False
		} else {
			if (lid != 0) {
				if (iq.sesh(lid, "AgentAccount") != null) {
					iq.sesh(lid, "currentPage") = this.Request.RawUrl.ToString;

				}
			}

			//If iq.sesh(lid,"UserID") Is Nothing And InStr(Me.Parent.ToString, "default_aspx") = 0 Then
			//    Response.Redirect("signin.aspx")
			//    Response.End()
			//    Exit Sub
			//End If

			// If iq.sesh(lid,"AgentAccount") = "" Then
			// Response.Redirect("signin.aspx")
			// Response.End()
			//  End If

		}

		//hide the tools link if were on quotes or resources pages (Tools need a tree div to populate)
		if (InStr(LCase(this.Parent.ToString), "tree_aspx") == 0) {
			toolsLink.Visible = false;
		}

		//If InStr(LCase(Me.Parent.ToString), "accountsettings") > 0 Then
		//    NavigationMenu.Items(0).Enabled = True
		//Else
		//    NavigationMenu.Items(0).Enabled = False
		//End If

		language = English;

		if (!agentAccount == null) {
			if (!agentAccount.HasRight("SHOWALL"))
				btnPortFolio.Visible = false;
			if (!agentAccount.HasRight("SHOWERRORS"))
				btnErrorDisplay.Visible = false;
			language = agentAccount.Language;
			submitString = Xlt("Submit Feedback", language);
		}

		if (!IsPostBack) {
			if ((!iq.sesh(lid, "showAll") == null) && (bool)iq.sesh(lid, "showAll")) {
				btnPortFolio.Text = "Show Portfolio";
				btnPortFolio.CommandArgument = "port";
			}

			if ((!iq.sesh(lid, "ErrorDisplay") == null) && (bool)iq.sesh(lid, "ErrorDisplay") == false) {
				btnErrorDisplay.Text = "Hide Errors";
				btnErrorDisplay.CommandArgument = "hide";
			}

			if ((!iq.sesh(lid, "treeMode") == null) && (bool)iq.sesh(lid, "treeMode")) {
				BtnTreeMode.Text = "Normal Mode";
				//set the button text to switch 'back' (to normal  mode)
				BtnTreeMode.CommandArgument = "norm";
			}
		}

		//If (Request.QueryString(s) IsNot Nothing) AndAlso (Request.QueryString(s).ToLower().Contains("universal")) Then
		//    legalLink.Visible = False
		//    litSubmit.Text = "<input type=""button"" class=""textButton"" />"
		//    litVersion.Visible = False
		//Else
		//    litSubmit.Text = "<input type=""button"" value=""" & submitString & """  onclick=""feedbackClick();"" class=""textButton"" />"
		//End If

		// Hide Legal/Submit Feedback/Version No. UI from sign up/register screens for Universal
		string url = Request.Url.AbsoluteUri.ToLower();
		bool hide = false;
		if (url.Contains("hpsignup.aspx")) {
			hide = true;
		} else if (url.Contains("signin.aspx")) {
			if ((universal) || (Request.QueryString(s) != null && Request.QueryString(s).ToLower().Contains("universal"))) {
				hide = true;
			}
		}

		if (hide) {
			legalLink.Visible = false;
			litSubmit.Text = "<input type=\"button\" class=\"textButton\" />";
			litVersion.Visible = false;
		} else {
			litSubmit.Text = "<input type=\"button\" value=\"" + submitString + "\"  onclick=\"feedbackClick();\" class=\"textButton\" />";
		}

		if (!IsPostBack) {
			if (agentAccount != null) {
				txtFeedBackFrom.Text = agentAccount.User.Email;
			} else {
				txtFeedBackFrom.Text = "you@youremail.com";
			}
		}


		if (!Request.Url.AbsolutePath.ToLower.Contains("tree.aspx")) {
			searchMenuItem.Style.Add("visibility", "hidden");
		}


		//~~~If Request.Url.AbsolutePath.Contains("accounts.aspx") Then btnBrowse.Visible = False
		//BtnFeedback.Attributes("onclick") = "thanks.style.display='block';"  'adds the script to show a thank you when they submit
		//TRANSLATION - of all the 'static' elements on all pages - labels, tooltips etc on things like login pages, account choice, charting
		//Note content that is subsequently ajax'd in (eg the basket) is generally translated 'just in time'
		if (lid != 0) {
			foreach ( c in MainContent.Controls) {
				if ((c) is WebControls.TextBox | (c) is WebControls.Image | (c) is WebControls.Label) {
					c.tooltip = Xlt(c.text, language);
				}
				if ((c) is WebControls.Label | (c) is WebControls.Button) {
					c.text = Xlt(c.text, language);
				}
			}
			if (iq.sesh(lid, "feedbackSent") != null) {
				lblMsg.Text = iq.sesh(lid, "feedbackSent").ToString();
				iq.sesh(lid, "feedbackSent") = "";
			}

		}

		//If (Request("Tools") = "True") Then
		//    PnlShoppingList.Visible = True
		//Else
		//    PnlShoppingList.Visible = False
		//End If

		BtnFeedback.Text = TranslateUI("Feedback");

		BtnFeedback.Attributes("class") = "hpOrangeButton sfb";

		feedbacktype.Items.Add(new ListItem(TranslateUI("Logon or password problem"), "logon"));
		feedbacktype.Items.Add(new ListItem(TranslateUI("Usability issue"), "usability"));
		feedbacktype.Items.Add(new ListItem(TranslateUI("Data/catalogue issues"), "data"));
		feedbacktype.Items.Add(new ListItem(TranslateUI("Spelling or grammar correction"), "spelling"));
		feedbacktype.Items.Add(new ListItem(TranslateUI("Language/Translation correction"), "translation"));
		feedbacktype.Items.Add(new ListItem(TranslateUI("Feature request"), "feature"));
		feedbacktype.Items.Add(new ListItem(TranslateUI("Suggestion"), "suggest"));
		feedbacktype.Items.Add(new ListItem(TranslateUI("Bug report"), "bug"));
		feedbacktype.Items.Add(new ListItem(TranslateUI("Other"), "other"));


		foreach ( wareHouseLoc in iq.Locations) {
			drpLocation.Items.Add(new ListItem(wareHouseLoc.Value, wareHouseLoc.Key));
		}

		drpWareHouse.Items.Add(new ListItem("All", ""));
		drpWareHouse.Items.Add(new ListItem("None", "NONE"));
		drpWareHouse.Items.Add(new ListItem("Test", "TST"));

		chkAllow.Text = TranslateUI("Allow support staff to see my iQuote session");
		Label1.Text = TranslateUI("Email Address");
		Label2.Text = TranslateUI("Feedback Type");
		Label3.Text = TranslateUI("Your feedback");
		Label4.Text = TranslateUI("Consent");
		Label5.Text = TranslateUI("Thanks for your feedback - we will respond shortly !");
		txtFeedbackLanguage.Items.AddRange(iq.ActiveLanguages.Select(lan => new ListItem(lan.Value.displayName(English), lan.Value.ID)).ToArray);

		clsAccount buyerAccount;
		if (lid != 0 && iq.SeshAlive(lid) && iq.seshDic(lid).ContainsKey("BuyerAccount") && !iq.sesh(lid, "BuyerAccount") == null) {
			buyerAccount = iq.sesh(lid, "BuyerAccount");
			txtFeedbackLanguage.SelectedValue = buyerAccount.Language.ID;
		} else {
			txtFeedbackLanguage.SelectedValue = English.ID;
		}

		//  Literal1.Text = "<input type="" button"" value=""" & TranslateUI("Submit feedback") & """ onclick=""feedbackClick();"" class=""textButton"" />"

		Label5.Text = TranslateUI("Thank you for contacting HP iQuote Support. Your feedback has been received and you will be contacted shortly.");
		//Literal1.Text = "<input type=""button"" value=""" & TranslateUI("Feedback") & """ onclick=""feedbackClick();"" class=""textButton"" />"

		if (agentAccount != null && (agentAccount.HasRight("ADMINMENU") | agentAccount.HasRight("GLOBALADM")))
			admMenu.Visible = true;

		//on the face of it - this is a bit crap - BUT - the masterpage is only loaded very infrequently (eveyrthing is ajax'd)
		//so having the full terms and conditions in the response isn't actually a big deal

		if (agentAccount == null) {
			if (string.Equals(mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase) && iq.Legal.ContainsKey("HPELegal")) {
				terms.Text = iq.Legal("HPELegal").Translation.text(English);
			} else if (string.Equals(mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase) && iq.Legal.ContainsKey("HPILegal")) {
				terms.Text = iq.Legal("HPILegal").Translation.text(English);
			} else if (iq.Legal.ContainsKey("CCLegal")) {
				terms.Text = iq.Legal("CCLegal").Translation.text(English);
			} else {
				terms.Text = "Usage of iQuote means that you agree to the following Terms & Conditions:<br/><br/> Every care is taken to ensure that the information contained within this site is accurate, however Errors and Omissions Excepted.";
			}
		} else {
			// We're logged-in: display legal terms from the seller channel
			terms.Text = agentAccount.SellerChannel.Legal;
		}

	}


	public void UpdateHeader(ref Literal css)
	{
		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid"), lid);

		//NavigationMenu.Visible = False
		newHeader.Visible = false;
		HyperBack.Visible = false;
		newMenu.Visible = false;
		btnNav.Visible = false;

		if (lid == 0) {
			LblRole.Text = "Not logged in";
			//if we're not signed in yet and we're NOT on the signin page (ie, we're on reset password, or choose account) then 
			//show the 'back to signin' link
			if (InStr(LCase(this.Parent.ToString), "signin_aspx") == 0) {
				HyperBack.Visible = true;
			}
			return;
		}

		//switch account
		//NavigationMenu.Items(1).Text = ""
		//NavigationMenu.Items(1).Enabled = False

		if (InStr(LCase(this.Parent.ToString), "signout_aspx") == 0) {
			newMenu.Visible = true;
			if (InStr(LCase(this.Parent.ToString), "accounts_aspx") > 0) {
				switchAccount.Visible = false;
				accountSetting.Visible = false;
			}
		}

		if (iq.sesh(lid, "screenName") != null && InStr(LCase(this.Parent.ToString), "signout_aspx") == 0) {
			string screenName = iq.sesh(lid, "screenName");
			LblRole.Text = screenName;
		} else {
			LblRole.Text = "Not logged in";
		}


		if (iq.SeshContains(lid, "AgentAccount") & !iq.sesh(lid, "AgentAccount") == null) {
			//NavigationMenu.Visible = True
			newHeader.Visible = true;
			newMenu.Visible = true;
			btnNav.Visible = true;
			clsAccount agentAccount;
			clsAccount buyerAccount;
			agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
			buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");

			if ((iq.sesh(lid, "viaGatekeeper")) && ((iq.sesh(lid, "AccountList")) is IEnumerable<clsAccount>)) {
				// If we've logged in via the gatekeeper, use the list of buyer accounts created there (i.e. filtered by channel) to work out if
				// there are multiple accounts available
				IEnumerable<clsAccount> accountList = iq.sesh(lid, "AccountList");
				if (!accountList == null && accountList.Count == 1) {
					switchAccount.Visible = false;
				}
			} else if (agentAccount.User.Accounts.Count == 1) {
				switchAccount.Visible = false;
			}

			if (!string.IsNullOrEmpty(agentAccount.mfrCode)) {
				// Add a manufacturer-specific style sheet to override any styles in the generic sheet
				css.Text = "<link href='" + ResolveUrl("~/") + "Styles/Site-" + agentAccount.mfrCode.ToLower + ".css ' rel='stylesheet' type='text/css' />";
			}

			string t1 = "";
			if (agentAccount.SellerChannel.pricesLoadedFor.ContainsKey(buyerAccount.Priceband)) {
				t1 = agentAccount.SellerChannel.pricesLoadedFor(buyerAccount.Priceband).ToString();
			}

			if (InStr(LCase(this.Parent.ToString), "accounts_aspx") == 0) {
				LblRole.Text = agentAccount.User.RealName + " - " + string.Join(",", agentAccount.Roles.Select(r => r.Translation.text(s_lang))) + " - " + agentAccount.SellerChannel.Name + " (" + agentAccount.SellerChannel.Region.Code + ")" + iq.seshDic(lid).ContainsKey("Elevated") ? "<font color='red'>ELEVATED</font>" : "";
				LblRole.ToolTip = "AgAcID:" + agentAccount.ID + " " + agentAccount.SellerChannel.Code + " " + "SlrChID:" + agentAccount.SellerChannel.ID + " variants:" + agentAccount.SellerChannel.countVariants + " PrcCfg:" + agentAccount.SellerChannel.priceConfig + " BaPB:" + buyerAccount.Priceband.text + " Pc(pb):" + t1 + " MfrCode:" + buyerAccount.mfrCode;
			}

			if (AccountHasRight(lid, "TREEVIEW")) {
				BtnTreeMode.Visible = true;
			//Dim treemode As Boolean = iq.sesh(lid, "treeMode")
			//If treemode Then BtnTreeMode.Text = "Normal Mode" Else BtnTreeMode.Text = "Tree Mode" 'this seems backwards - but isn't  -- Ml 110914 removed this bit as its fired at the start and then after the button click so the logic reverses in each case, moved to the actual button click
			} else {
				BtnTreeMode.Visible = false;
			}

		//  LblQuotingFor.Text = iq.Accounts(iq.sesh(lid,"buyeraccount")).displayname(s_lang)
		//'Admin mode

		//If agentaccount.User.Accounts.Count > 1 Then
		//    NavigationMenu.Items(1).Text = Xlt("Switch Account", agentaccount.Language)
		//    NavigationMenu.Items(1).Enabled = True
		//End If
		//ElseIf lid > 0 And iq.SeshContains(lid, "AgentAccount") = False Then
		//    newHeader.Visible = True
		//    newMenu.Visible = True
		//    'btnNav.Visible = True
		//    accountSetting.Visible = False
		//    switchAccount.Visible = False
		} else if (InStr(LCase(this.Parent.ToString), "accounts_aspx") > 0) {
			newMenu.Visible = true;
			switchAccount.Visible = false;
			accountSetting.Visible = false;
		}


		ttlMN.Attributes.Add("title", Environment.MachineName);
	}
	//Protected Sub NavigationMenu_MenuItemClick(sender As Object, e As System.Web.UI.WebControls.MenuEventArgs) Handles NavigationMenu.MenuItemClick

	//    Dim lid As UInt64 = Convert.ToUInt64(Request.QueryString("lid"))

	//    Dim mi As WebControls.Menu
	//    mi = CType(sender, WebControls.Menu)


	//    'sp:MenuItem NavigateUrl="" Text="New Quote" Value="new"/>
	//    '    <asp:MenuItem NavigateUrl="~/listquotes.aspx" Text="Find Quote" Value="find"/>
	//    '    <asp:MenuItem Text="Sign Out" Value="signOut"></asp:MenuItem>
	//    '    <asp:MenuItem Text="Admin Mode" Value="admin"></asp:MenuItem>
	//    '    <asp:MenuItem Text="Shopping List"  
	//    'ToolTip = "Build a quote from an ordered list of systems and their options."
	//    '        Value="shoppingList"></asp:MenuItem>
	//    '    <asp:MenuItem Text="Swift" 
	//    'ToolTip = "Display a 'flat' list of the valid options for a system unit"
	//    '        Value="swift"></asp:MenuItem>
	//    '</Ite

	//    If Not iq.SeshAlive(lid) Then
	//        Response.Redirect("Signin.aspx")
	//    Else

	//        If mi.SelectedValue = "new" Then
	//            iq.sesh(lid, "QuoteID") = Nothing
	//            iq.sesh(lid, "branchStates").clear() 'wipetreestate
	//            Dim errormessages As List(Of String) = New List(Of String)

	//            'set the root node to render its' children as squares Bootstrap the tree (we cant create branchinfo or know the visiblechildren util we know the account)
	//            'clsBranchState.setBranchState(lid, "tree.1", oc.open, bt.BreadCrumb, False)
	//            'Dim bi As clsBranchInfo = New clsBranchInfo(lid, "tree.1", Nothing, 1000)
	//            'bi.setChildBranches(oc.closed, bt.Square, False)

	//            setBranchState(lid, "tree.1", oc.open, bt.BreadCrumb, False) ' this ID needed (without it we cant make BranchInfo)
	//            Dim bi As clsBranchInfo = New clsBranchInfo(lid, "tree.1", Nothing, 1000, enumParadigm.AddingSystem, errormessages)
	//            bi.setChildBranches(oc.closed, bt.Square, False, errormessages)

	//            Response.Redirect("tree.aspx?lid=" & lid)  '

	//        ElseIf mi.SelectedValue = "signOut" Then
	//            'Session.Abandon()
	//            Response.Redirect("Signout.aspx?lid=" & lid)  'AddHeader("Refresh", "1") 'this page will refresh (afetr 1 second) - without a session it will redirect to the login page
	//        ElseIf mi.SelectedValue = "switchAccount" Then
	//            'Session.Abandon()
	//            Dim dic As Dictionary(Of String, Object) = iq.getSeshDic(lid)
	//            dic.Remove("AgentAccount")
	//            'NavigationMenu.Visible = False
	//            Response.Redirect("accounts.aspx?lid=" & lid)  'AddHeader("Refresh", "1") 'this page will refresh (afetr 1 second) - without a session it will redirect to the login page

	//        ElseIf mi.SelectedValue = "admin" Then

	//            Dim adminMode As Boolean = False
	//            If iq.SeshContains(lid, "admin") Then
	//                adminMode = CBool(iq.sesh(lid, "admin"))
	//            End If

	//            If adminMode Then
	//                'switch admin off
	//                iq.sesh(lid, "showAll") = False
	//                iq.sesh(lid, "admin") = False
	//            Else
	//                'switch admin on
	//                iq.sesh(lid, "showAll") = True
	//                iq.sesh(lid, "admin") = True

	//                'For Each k In Session.Keys
	//                '    If Left(k, 5) = "open." Then
	//                '        toKill.Add(k)
	//                '    End If
	//                '    If Left(k, 4) = "rca." Then
	//                '        toKill.Add(k)
	//                '    End If
	//                'For Each k In toKill : Session.Remove(k) : Next

	//            End If

	//            UpdateHeader()

	//            '    Session.Abandon()
	//            '    Response.Redirect("Signin.aspx")  'AddHeader("Refresh", "1") 'this page will refresh (afetr 1 second) - without a session it will redirect to the login page
	//        ElseIf mi.SelectedValue = "find" Then
	//            Response.Redirect("listquotes.aspx?lid=" & lid)

	//        ElseIf mi.SelectedValue = "shoppingList" Then

	//            PnlShoppingList.Visible = True


	//        ElseIf mi.SelectedValue = "settings" Then
	//            Response.Redirect("accountSettings.aspx?lid=" & lid)
	//        ElseIf mi.SelectedValue = "myquote" Then
	//            Response.Redirect("tree.aspx?lid=" & lid)

	//        Else
	//            Beep()
	//        End If
	//    End If

	//End Sub



	protected void  // ERROR: Handles clauses are not supported in C#
BtnFeedback_Click(object sender, EventArgs e)
	{

		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));

		System.Net.Mail.SmtpClient smtpclient = new System.Net.Mail.SmtpClient();

		MailMessage msg;

		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");


		try {
			string address = iq.Addresses("iQuoteSupportEmail").Translation.text(English);
			msg = new MailMessage(address, address);
			//"Support@channelcentral.net", "development@channelcentral.net")
			msg.Subject = "iQuote2 Feedback";
			//.User.RealName() & " at " & .BuyerChannel.BusinessName
			//msg.CC.Add("tom.legge@channelcentral.net")
			msg.Body = "<h2>Feedback Session Details</h2>" + "<table>" + "<tr><th>Manufacturer</th><td>" + agentAccount != null ? agentAccount.ManufacturerDescription : "Not logged in" + "</td></tr>" + "<tr><th>Date</th><td>" + DateTime.Now.ToString() + "</td></tr>" + "<tr><th>Account UserName/Email</th><td>" + agentAccount != null ? agentAccount.User.Email : "Not logged in" + "</td></tr>" + "<tr><th>Entered email</th><td>" + txtFeedBackFrom.Text + "</td></tr>" + "<tr><th>Contact Name</th><td>" + txtFeedbackName.Text + "</td></tr>" + "<tr><th>IP Address</th><td>" + Request.UserHostAddress.ToString + "</td></tr>" + "<tr><th>User Agent</th><td>" + Request.UserAgent.ToString + "</td></tr>" + "<tr><th>Session lid</th><td>" + agentAccount != null ? lid.ToString : "Not logged in" + "</td></tr>" + "<tr><th>Buyer Host Id</th><td>" + agentAccount != null ? agentAccount.BuyerChannel.Code : "Not logged in" + "</td></tr>" + "<tr><th>Seller Host Id</th><td>" + agentAccount != null ? agentAccount.SellerChannel.Code : "Not logged in" + "</td></tr>" + "<tr><th>Price Config</th><td>" + agentAccount != null ? agentAccount.BuyerChannel.DecodedPriceConfig : "Not logged in" + "</td></tr>" + "<tr><th>Gatekeeper</th><td>" + agentAccount != null ? iq.seshDic(lid).ContainsKey("gk_token") ? "YES" : "NO" : "Not logged in" + "</td></tr>";
			// Need to bring GK accross

			//If agentAccount IsNot Nothing AndAlso agentAccount.BuyerChannel.priceConfig And 4 Then
			//    Try
			//        Dim sql$ = "select top 1 * from pricing.pna.feed where BuyerAccount_ID = " & agentAccount.BuyerChannel.ID & " order by timestamp desc for xml auto"
			//        msg.Body &= "<tr><th>Price Config</th><td>" & dataAccess.da.DBSelectFirst(sql)(0).ToString & "</td></tr>"
			//    Catch ex As Exception
			//        ErrorLog.Add(ex)
			//    End Try
			//End If

			msg.Body += "<tr><th>Prefered Contact (if any)</th><td>" + txtContactDetails.Text + "</td></tr>" + "<tr><th>Preferred Language</th><td>" + txtFeedbackLanguage.SelectedItem.Text + "</td></tr>" + "<tr><th>Consent to access session</th><td>" + chkAllow.Checked ? "Yes" : "No" + "</td></tr>" + "<tr><th>Product revision</th><td>" + Assembly.GetExecutingAssembly.GetName.Version.ToString + "</td></tr>" + "<tr><th>Category</th><td>" + feedbacktype.SelectedValue + "</td></tr>" + "</table>";

			btndiv.Style("display") = "none";
			msg.ReplyToList.Add(txtFeedBackFrom.Text);

			msg.Body += "<h2>Notes</h2><br>";
			msg.Body += txtFeedback.Text;

			//            msg.Body &= "<p><b>User agent:" & Request.UserAgent.ToString & "</b>"
			//           msg.Body &= "<p><b>IP:" & Request.UserHostAddress.ToString & "</b>"

			//msg.Body &= "<p><b>IP:" & Request.UserHostAddress.ToString & "</b>"
			//msg.Body &= "<p><b>IP:" & Request.UserHostAddress.ToString & "</b>"

			//Dim a As Assembly = Assembly.GetExecutingAssembly
			//msg.Body &= "<p><b>BUILD:" & a.GetName.Version.ToString & "</b>"




			//If chkAllow.Checked Then
			//msg.Body &= "User has given consent to <a href='" & Request.Url.AbsoluteUri & "'>View Session</a>"
			//else
			//MSg.Body &= "<p style=color:red>The user has not allowed access to their session</p>"
			//End If


			msg.IsBodyHtml = true;
			msg.Priority = MailPriority.High;
			//End With

			//smtpclient.Host = "smtp.fasthosts.co.uk"
			//smtpclient.EnableSsl = False
			//smtpclient.Credentials = New Net.NetworkCredential("support@hpiquote.net", "ny7zZLvk9s0c")

			smtpclient.ServicePoint.MaxIdleTime = 1;
			//  smtpclient.DeliveryMethod = SmtpDeliveryMethod.Network
			smtpclient.Send(msg);
			lblMsg.Text = TranslateUI("Email sent successfully");
			iq.sesh(lid, "feedbackSent") = TranslateUI("Email sent successfully");
		//Response.Redirect(Request.Url.ToString())
		} catch (Exception ex) {
			ErrorLog.Add(ex);
			lblMsg.Text = TranslateUI("Failed to send email. please try again later.");
			iq.sesh(lid, "feedbackSent") = TranslateUI("Failed to send email. please try again later.");

		} finally {
			txtFeedback.Text = "";
		}
	}


	protected void  // ERROR: Handles clauses are not supported in C#
feedbacktype_SelectedIndexChanged(object sender, EventArgs e)
	{
	}



	protected void  // ERROR: Handles clauses are not supported in C#
btnErrorDisplay_Click(object sender, EventArgs e)
	{
		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));

		// Use the button's CommandArgument to work out what mode we're switching to. Simply toggling means a browser refresh
		// triggers a toggle, which is probably unintended
		Button btnErrorDisplay = (Button)sender;
		if (btnErrorDisplay.CommandArgument == "show") {
			btnErrorDisplay.Text = "Hide Errors";
			btnErrorDisplay.CommandArgument = "hide";
			iq.sesh(lid, "ErrorDisplay") = false;
		} else {
			btnErrorDisplay.Text = "Show Errors";
			btnErrorDisplay.CommandArgument = "show";
			iq.sesh(lid, "ErrorDisplay") = true;
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
btnPortfolio_Click(object sender, EventArgs e)
	{
		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));

		// Destroy all the data tables (we'll need to re-make these) now we're switching
		Dictionary<string, clsScreenHeader> matrixHeaders = (Dictionary<string, clsScreenHeader>)iq.sesh(lid, "screenHeaders");
		matrixHeaders.Clear();

		// Use the button's CommandArgument to work out what mode we're switching to. Simply toggling means a browser refresh
		// triggers a toggle, which is probably unintended
		Button btnPortfolio = (Button)sender;
		if (btnPortfolio.CommandArgument == "port") {
			btnPortfolio.Text = "Show All";
			btnPortfolio.CommandArgument = "all";
			iq.sesh(lid, "showAll") = false;
			//Switch to showing portfolio only
		} else {
			btnPortfolio.Text = "Show Portfolio";
			btnPortfolio.CommandArgument = "port";
			iq.sesh(lid, "showAll") = true;
			//SWITCH to showing all
		}

	}



	protected void  // ERROR: Handles clauses are not supported in C#
BtnTreeMode_Click(object sender, EventArgs e)
	{
		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));

		// Use the button's CommandArgument to work out what mode we're switching to. Simply toggling means a browser refresh
		// triggers a toggle, which is probably unintended
		Button btnTreeMode = (Button)sender;
		if (btnTreeMode.CommandArgument == "norm") {
			iq.sesh(lid, "treeMode") = false;
			//Switch to 'normal' mode 
			setBranchState(lid, "tree.1", new clsBranchState(lid, "tree.1", enumBt.Square, false, 0, 100));
			btnTreeMode.Text = "Tree Mode";
			//set the button text to switch 'back' (to treee mode)
			btnTreeMode.CommandArgument = "tree";
		} else {
			iq.sesh(lid, "treeMode") = true;
			//SWITCH to tree mode
			btnTreeMode.Text = "Normal Mode";
			//set the button text to switch 'back' (to normal  mode)
			btnTreeMode.CommandArgument = "norm";
		}

	}

	public string TranslateUI(string text)
	{

		//'WHY ??? - If Not clsIQ.IsLoaded Then Response.Redirect("SystemMaintenance.aspx", False) : Exit Function

		if (clsIQ.IsLoaded) {
			if (language != null) {
				return Xlt(text, language);
			} else {
				return Xlt(text, iq.i_language_Code("EN"));
			}
		} else {
			return "XX";
		}

	}

	public void HideContent()
	{
		MainContent.Visible = false;
	}
	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		Page.Header.DataBind();
	}


	protected void btnContinue_Click(object sender, EventArgs e)
	{
		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));
		clsCustomerContext custContext = new clsCustomerContext();
		custContext.Location = drpLocation.SelectedValue;
		custContext.Tax = chkTax.SelectedValue;
		custContext.WareHouse = drpWareHouse.SelectedValue;
		iq.sesh(lid, "custContext") = custContext;
		btnContext.Value = "Modify";
		btnContext.Attributes.Remove("class");
		btnContext.Attributes.Add("class", "hpBlueButton2");
		iq.sesh(lid, "Quote") = null;

	}

	protected void btnCancel_Click(object sender, EventArgs e)
	{
		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));
		if (iq.SeshContains(lid, "QuoteID") & iq.sesh(lid, "custContext") == null) {
			iq.sesh(lid, "QuoteID") = null;
			iq.sesh(lid, "Quote") = null;
			iq.sesh(lid, "QuoteLocked") = false;
			object bts = (Dictionary<string, clsBranchState>)iq.sesh(lid, "branchStates");
			if (bts != null)
				bts.Clear();
			//wipetreestate
			iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem;
			object shs = (Dictionary<string, clsScreenHeader>)iq.sesh(lid, "screenHeaders");
			if (shs != null)
				shs.Clear();
			iq.sesh(lid, "lastbranch") = null;
			if (iq.seshDic(lid).ContainsKey("promoinforce"))
				iq.seshDic(lid).Remove("promoinforce");
			//re-boot the tree
			string root = iq.sesh(lid, "Root").ToString;

			iq.sesh(lid, "path") = root;

			Response.Redirect("tree.aspx?lid=" + Request.QueryString("lid") + Request("elid") != "" ? "&elid=" + Request("elid") : "", false);
			return;

		}
	}
}
