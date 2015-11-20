using System.Web.UI.DataVisualization;
using System.Net.Mail;
using System.IO;
using IQ.clsBranchState;


public class tree1 : clsPageLogging
{

	private void  // ERROR: Handles clauses are not supported in C#
tree1_Init(object sender, System.EventArgs e)
	{
		//FillDDL(ddlbuyer, iq.channel.Values)
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		clsQuote quote;
		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid").ToString, lid);

		if (!clsIQ.IsLoaded || !iq.SeshAlive(lid)){Response.Redirect(string.Format("signin.aspx?badlid={0}", lid), false);return;
}

		//    Dim adverts As Dictionary(Of Integer, clsAdvert) = New Dictionary(Of Integer, clsAdvert)
		if ((Request("Quote") == "Browse")) {
			CloseBelow(lid, "tree.1");
			string root = iq.seshTyped<string>(lid, "Root");
			iq.sesh(lid, "path") = root;
			object bts = iq.seshTyped<Dictionary<string, clsBranchState>>(lid, "branchStates");
			if (bts != null)
				bts.Clear();
			//wipetreestate
			iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem;
			iq.sesh(lid, "showOnly") = null;
			iq.sesh(lid, "treeCursorPath") = root;
			//iq.sesh(lid, "matrixHeaders").clear()
			if (iq.seshDic(lid).ContainsKey("promoinforce"))
				iq.seshDic(lid).Remove("promoinforce");
			iq.sesh(lid, "Quote") = null;

			Response.Redirect("tree.aspx?lid=" + Request.QueryString("lid") + Request("elid") != "" ? "&elid=" + Request("elid") : "", false);
			return;
		}

		if ((Request("Quote") == "New")) {
			iq.sesh(lid, "QuoteID") = null;
			iq.sesh(lid, "Quote") = "New";
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

		// If Not iq.sesh(lid, "UserID") Is Nothing Then 'are we logged in ?

		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		//this will add the full set of filter buttons into the form tag of the page
		if (!agentAccount == null)
			Form.Controls.Add(MakeFilterButtons(agentAccount.Language));

		//the first time the page loads

		//when coming here from the listquotes page..
		//buyeraccount is *always* set - as without it there can be no pricing

		//needs moving to a aspx of its own which outputs new link each time - to be called from a 'showFocus() function - whch would be called back by any change
		//ie showFocus would Rexec 'focus.aspx' - which would output the (updated) title/link (into a DIV in Tree.aspx) - which would showFocus()
		//Foci as stroed in the session variable as a comma delimited list = but are made into a hashset   

		//build a hashset from the CD list stored in the sesstion variable
		HashSet<string> foci = new HashSet<string>(Split(iq.sesh(lid, "foci").ToString, ",").ToList);


		if (buyerAccount.SellerChannel.Focus != "") {
			FocusButton.Click += SwitchFocus;
			if (foci.Count > 0) {
				FocusLabel.Text = Xlt("Currently displaying:" + Join(foci.ToArray, ",") + " switch to ", buyerAccount.Language);
				FocusButton.Text = Xlt("All HP Products", buyerAccount.Language);
				FocusButton.Attributes("focus") = "";
			//    switch.Attributes("onclick") = "rExec('Manipulation.aspx?command=focus&value=', showQuote);"
			} else {
				FocusLabel.Text = Xlt("Currently displaying: All HP products switch to ", buyerAccount.Language);
				FocusButton.Text = buyerAccount.SellerChannel.Focus;
				FocusButton.Attributes("focus") = buyerAccount.SellerChannel.Focus;
				//    Dim script$ = "rExec('Manipulation.aspx?command=focus&value=" & switch.Text & "', showQuote);"
				//    switch.Attributes("onclick") = script$
			}
		}

		if (IsPostBack) {
			//BootStrap the tree 
			string root = iq.seshTyped<string>(lid, "Root");
			string pth = iq.seshTyped<string>(lid, "path");
			// errorMessages.Add("page posted back - everything should be ajax")
			iq.sesh(lid, "path") = root;
			//BootStrap/Reset the tree
			iq.sesh(lid, "refreshing") = "true";

			clsBranchInfo bi = new clsBranchInfo(lid, root, null, 1000, (enumParadigm)iq.sesh(lid, "Paradigm"), errorMessages);
			bi.open(errorMessages, false);

		} else {
			//first' time to the page (from signin - or from find a quote) - (or F5 is pressed - which is NOT a postback)
			//BootStrap the tree 
			string root = iq.seshTyped<string>(lid, "Root");
			string pth = iq.seshTyped<string>(lid, "path");
			//If pth = "" Then
			iq.sesh(lid, "path") = root;
			//BootStrap/Reset the tree
			iq.sesh(lid, "refreshing") = "true";

			//'Create the dictionary that holds all the (important) information about branch state for this user session
			if (iq.sesh(lid, "branchStates") == null) {
				Dictionary<string, clsBranchState> branchStates = new Dictionary<string, clsBranchState>();
				iq.sesh(lid, "branchStates") = branchStates;
			}

			clsBranchInfo bi = new clsBranchInfo(lid, root, null, 1000, (enumParadigm)iq.sesh(lid, "Paradigm"), errorMessages);

			bi.open(errorMessages, false);

			//we re-point the autosuggest dictionary to the SellerChannels CustomerAccounts - just in time
			//so that we can provide autosuggest of only this sellers customers
			iq.Gateway("accounts") = agentAccount.SellerChannel.CustomerAccounts;

			//we want to create a new account - where the seller channel is the agents channel and the buyer channel is th one in Autosuggested channel 
			//Then launch the editor

			//Dim btnAddContact As Button = New Button
			//btnAddContact.Text = "Add Contact"
			//pnlAddContactButton.Controls.Add(btnAddContact)

			//Dim scr$
			//scr$ = "rExec('Manipulation.aspx?command=CreateSiblingAccount&AccID='+ctl00_MainContent_txtBuyerID, nullFunc);"

			//Dim callback$ 'this is executed when the rExec of manipualtion.aspx finishes - really want some way fetch/pass the ID of the accountwe just created

			//'      callback$ = "function(){injectIframe('editor/Editor.aspx?path=Accounts','PnlAddContact');return false;}"
			//Dim url$ = "../editor/Editor.aspx?path=Accounts(" & iq.sesh(lid,"BuyerAccountIDd") & ")"
			//callback$ = "function(){injectIframe('" & url$ & "','ctl00_MainContent_PnlAddContact');return false;}"
			//'Manipulation.aspc createSibilingAccount will set iq.sesh(lid,"buyeraccount") 
			//scr$ = "rExec('Manipulation.aspx?command=CreateSiblingAccount&AccID='+ctl00_MainContent_txtBuyerID.value," & callback & ");return false;"

			//'We give it 300ms to create the account - which should be plenty of time - then.. edit it
			//'scr$ &= "setTimeout(function(){injectIframe('editor/Editor.aspx?dic=channels&key='+ctl00_MainContent_txtBuyerID+'&screenid=" & iq.i_screens_code("Chann").ID & "','PnlAddContact');return false;},300);"
			//btnAddContact.OnClientClick = scr$

			if (!object.ReferenceEquals(buyerAccount, agentAccount))
				txtBuyer.Enabled = false;
			// lock it ! (if the quote is already assigned to a customer)

			// LblBuyer.Text = buyerAccount.displayName(s_lang)
			txtBuyer.Text = buyerAccount.displayName(s_lang);
			//  PnlProductTree.CssClass = "visible"

			//retreiving a previewed quote - TODO - check the agentaccount is correct as a security measure
			if (Request("QuoteID") != null && Val(Request("QuoteID")) != 0) {
				iq.sesh(lid, "QuoteID") = (int)Request("QuoteID");
			}

			if (iq.SeshContains(lid, "QuoteID")) {
				int quoteID = iq.seshTyped<int>(lid, "QuoteID");
				if (quoteID != 0) {
					quote = agentAccount.Quotes(quoteID);
					if (quote.RootItem.Children.Count == 0)
						quote.LoadItems(errorMessages);

					txtQuoteName.Text = quote.Name.DisplayValue;
					//important (especially wehn reloading quotes!)


				}
			} else {
				//txtBuyer.Text = buyerAccount.displayName(s_lang)

			}
			txtBuyer.Text = buyerAccount.displayName(s_lang);

			if (!iq.sesh(lid, "Base") == null) {
				// Register a client-side script to handle adding the requested SKU to the basket
				// The sesh variable is deleted to ensure this is a one-off add
				object sku = iq.sesh(lid, "Base").ToString();
				iq.sesh(lid, "Base") = null;
				RegisterDeepLinkScript(lid, sku, buyerAccount);
			}

		}

		//BRAZIL - Very temporary - Sam will put this somewhere more sensible
		DropDownList ddlWarehouse = new DropDownList();
			//Shows all warehouses/varaints
			//show no warehouse prices (shows list price)
			//Shows the specific TST warehouse variants


			//  <select name="ctl00$MainContent$ddlWarehouse" id="ctl00_MainContent_ddlWarehouse" onchange="burstBubble(event);getBranches('cmd=setWarehouse&amp;warehouse=' + document.getElementById('ddlWarehouse').value);return false;">

		 // ERROR: Not supported in C#: WithStatement


		// litAdvert.Text = getAdverts(lid)


		//Randomize()

		//'   If adverts.Count > 1 Then
		//rndNumber = Int(Rnd() * adverts.Count)
		//'Else
		//'rndNumber = 1
		//'End If

		//Dim selectedAdvert As clsAdvert = adverts.Values(rndNumber)
		//If selectedAdvert IsNot Nothing Then

		//    Dim advertImpressions As clsImpression = New clsImpression(agentAccount, selectedAdvert, Now)
		//    Dim urlString As String = selectedAdvert.ImageUrl
		//    If String.IsNullOrEmpty(urlString) = False Then
		//        advertLiteral.Text = "<a onclick=""clickthru(" & selectedAdvert.ID & ");""><img alt="""" src=""" & urlString & """   /></a>"
		//    End If
		//End If

		// kwlit.Text = "<h2>" & Xlt("Keyword Search", buyerAccount.Language) & "</h2>"

		OutputErrors(Page.Controls, errorMessages, lid, true);

		//NB: The JS OnLoad() in the page (tree.aspx) - will now fire.. calling getbranches (see bottom to tree.aspx markup)


	}


	private void RegisterDeepLinkScript(ulong lid, string sku, clsAccount buyerAccount)
	{
		ClientScriptManager cs = Page.ClientScript;

		string script;
		object baseSku = sku;

		if ((!string.IsNullOrEmpty(baseSku)) && (baseSku.Length > 4)) {
			int i = baseSku.IndexOf("#", baseSku.Length - 4);
			if (i > 0) {
				// sku ends with #ABC - trim off to get the base code
				baseSku = baseSku.Substring(0, i);
			}
		}


		if (iq.i_SKU.ContainsKey(baseSku)) {
			object product = iq.i_SKU(baseSku);
			clsBranch branch = iq.Branches.Values.Where(b => (!b.Product == null) && (b.Product.isSystem) && (b.Product.ID == product.ID)).FirstOrDefault;


			if (!branch == null) {
				// Build the path
				string path = string.Empty;
				int root = 0;
				BuildPath(branch, path, root);
				if (!string.IsNullOrEmpty(path)) {
					path = "tree." + path;
				}


				if (iq.RootBranch.ID == root) {
					// Find the variant

					object priceList = product.GetPrices(buyerAccount, buyerAccount.SellerChannel.priceConfig, iq.AllVariants, errorMessages, true);

					if (priceList.Count > 0) {
						clsPrice price = null;
						if (priceList.Count == 1) {
							price = priceList(0);
						} else {
							price = priceList.Where(p => string.Equals(p.SKUVariant.DistiSku, sku, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault;
							if (price == null) {
								price = priceList(0);
							}
						}

						if ((!price == null) && (!price.SKUVariant == null)) {
							// Register the client-side script to add the requested SKU to the basket
							script = string.Format("deepLink('{0}', '{1}');", path, price.SKUVariant.ID);
							cs.RegisterStartupScript(this.GetType(), "DeepLinkScript", script, true);

							// Call PloughPath to render the tree to the required point
							PloughPath(lid, path, errorMessages, 0, enumParadigm.configuringSystem);
						}

					}
				}
			}
		}

	}


	/// <summary>
	/// Builds the path of the passed branch
	/// </summary>
	/// <param name="branch"></param>
	/// <param name="path"></param>
	/// <remarks></remarks>

	private void BuildPath(clsBranch branch, ref string path, ref int root)
	{
		if (path == string.Empty) {
			path = branch.ID.ToString();
		} else {
			path = branch.ID.ToString() + "." + path;
		}
		root = branch.ID;

		if (!branch.Parent == null) {
			BuildPath(branch.Parent, path, root);
		}

	}

	private EventHandler SwitchFocus(object b, System.EventArgs e)
	{
		object but = (Button)b;

		UInt64 lid = 0;
		UInt64.TryParse(Request.QueryString("lid").ToString, lid);


		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");

		string focus = but.Attributes("focus");
		//We're switching to 'all'
		if (focus == "") {
			iq.sesh(lid, "focus") = new List<string>();
			FocusLabel.Text = Xlt("Currenct displaying All HP Products swtich to ", buyerAccount.Language);
			but.Text = buyerAccount.SellerChannel.Focus;
		} else {
			//we're focusing on one.. (or more)
			FocusLabel.Text = Xlt("Currently displaying:" + focus + " switch to ", buyerAccount.Language);
			but.Text = Xlt("All HP Products", buyerAccount.Language);
			iq.sesh(lid, "foci") = focus;
		}

	}


	//Protected Sub btnXMLQuote_Click(sender As Object, e As EventArgs) Handles btnXMLQuote.Click



	//    Stop


	//    'DEPRECATED (I'm pretty sure) - nick

	//    Dim lid As UInt64 = Request.QueryString("lid")

	//    'should be invisible really (until we have a quote)
	//    If iq.sesh(lid, "QuoteID") IsNot Nothing Then


	//        Dim quote As clsQuote
	//        quote = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))

	//        quote.Name = New nullableString(txtQuoteName.Text)
	//        quote.Update()

	//        Dim fullpath$
	//        fullpath$ = SaveXML(quote.XMLDoc(errorMessages), "/quotes/" & quote.RootQuote.ID & "-" & quote.Version & ".xml", LblSave.Text)

	//        OutputErrors(PnlErrors.Controls, errorMessages, lid)

	//        If fullpath <> "FAIL" Then
	//            iq.sesh(lid, "tostream") = fullpath$
	//            Response.Redirect("streamer.aspx?lid=" & lid)
	//        Else
	//            LblSave.BackColor = Drawing.Color.Red : LblSave.ForeColor = Drawing.Color.White

	//        End If
	//    End If

	//    'Response.Clear()
	//    'Response.AppendHeader("Content-Disposition", "attachment;filename=Quote-" & quote.ID & ".xml")
	//    'Response.ContentType = "text/xml"
	//    'Response.AddHeader("Content-Length", My.Computer.FileSystem.GetFileInfo(fullpath).Length.ToString())

	//    'Response.Flush()
	//    'Response.WriteFile(fullpath$)
	//    'Response.End()

	//End Sub


	//Protected Sub BtnExcel_Click(sender As Object, e As EventArgs) Handles BtnExcel.Click


	//    'Obsolete/Deprecated

	//    Stop
	//    Dim lid As UInt64 = Request.QueryString("lid")

	//    'should be invisible really (until we have a quote)
	//    If iq.sesh(lid, "QuoteID") Is Nothing Then
	//        errorMessages.Add("You need to add some items to the quote first")
	//    Else

	//        Dim fullpath$
	//        Dim QUOTE As clsQuote = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))

	//        QUOTE.Name = New nullableString(txtQuoteName.Text)
	//        QUOTE.Update()

	//        fullpath$ = ODS.OutputQuote(QUOTE, "Quotes", errorMessages) 'the OutputQuote() function returns the full physical path the the file generated on the server

	//        If errorMessages.Count = 0 Then
	//            iq.sesh(lid, "tostream") = fullpath$
	//            iq.sesh(lid, "streamcontent-type") = "application/vnd.ms-excel;charset=UTF-8"
	//            iq.sesh(lid, "DeleteStreamed") = True
	//            Response.Redirect("streamer.aspx?lid=" & lid)
	//        Else
	//            'LblSave.Text = errors : LblSave.BackColor = Drawing.Color.Red : LblSave.ForeColor = Drawing.Color.White

	//        End If
	//    End If

	//    OutputErrors(PnlErrors.Controls, errorMessages, lid)

	//End Sub

	//Protected Sub BtnPDF_Click(sender As Object, e As EventArgs) Handles BtnPDF.Click

	//    Dim lid As UInt64 = Request.QueryString("lid")

	//    Dim fullpath$
	//    Dim QUOTE As clsQuote = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))

	//    QUOTE.Name = New nullableString(txtQuoteName.Text)
	//    QUOTE.Update()


	//    'will write an ODS - PDFgen on the server is (or should be !) watching the folder

	//    fullpath$ = ODS.OutputQuote(QUOTE, "PDFQuotes", errorMessages) 'the OutputQuote() function returns the full physical path the the file generated on the server

	//    'PDF Gen should spot it in second or so.. (and convert it to PDF)

	//    If errorMessages.Count = 0 Then
	//        iq.sesh(lid, "tostream") = Left$(fullpath$, InStrRev(fullpath$, ".")) & ".pdf"
	//        iq.sesh(lid, "streamcontent-type") = "application/pdf"
	//        iq.sesh(lid, "DeleteStreamed") = True

	//        BtnPDF.Text = Xlt("One moment please...", iq.sesh(lid, "language"))

	//        'gives the PDGgen time to happen
	//        Response.AddHeader("REFRESH", "3;URL=streamer.aspx?lid=" & lid)
	//    Else
	//        'LblSave.Text = errors : LblSave.BackColor = Drawing.Color.Red : LblSave.ForeColor = Drawing.Color.White
	//        OutputErrors(PnlErrors.Controls, errorMessages, lid)

	//    End If


	//End Sub

	//Protected Sub Btnsave_Click(sender As Object, e As EventArgs) Handles Btnsave.Click

	//    Dim lid As UInt64 = Request.QueryString("lid")

	//    'should be invisible really (until we have a quote)
	//    If iq.sesh(lid, "QuoteID") IsNot Nothing Then

	//        Dim quote As clsQuote
	//        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)


	//        quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"))
	//        quote.Name = New nullableString(txtQuoteName.Text)
	//        quote.Update()
	//        quote.RootItem.updateRecursive()

	//        '        Dim NewQuote As clsquote

	//        '        NewQuote = quote.CreateNextVersion

	//        'agentaccount.Quotes.Add(NewQuote.ID, NewQuote) - not neccessary - creating the quote adds it to the agents list

	//        'iq.sesh(lid,"QuoteID") = NewQuote.ID

	//        LblSave.Text = Xlt("Version " & quote.Version & " saved.", agentAccount.Language) ' ,version " & NewQuote.Version & " created"

	//    End If


	//End Sub

	//Protected Sub BtnStartQuote_Click(sender As Object, e As EventArgs) Handles BtnStartQuote.Click

	//    'Don't look here ! .... see manipulation.aspx - StartQuote  (which is called via javascript and ajax)

	//End Sub

	//Protected Sub BtnEmail_Click(sender As Object, e As EventArgs) Handles BtnEmail.Click

	//    Dim state$ = ""

	//    Dim lid As UInt64 = Request.QueryString("lid")

	//    If iq.sesh(lid, "QuoteID") Is Nothing Then

	//        errorMessages.Add("You must add something to the quote first")
	//    Else

	//        state$ = "RQ"
	//        Dim fullpath$
	//        Dim QUOTE As clsQuote = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Quotes(iq.sesh(lid, "QuoteID"))

	//        state$ = "UQ"
	//        QUOTE.Name = New nullableString(txtQuoteName.Text)
	//        QUOTE.Update()

	//        state$ = "OQ"
	//        Dim errors As String = ""
	//        fullpath$ = ODS.OutputQuote(QUOTE, "Quotes", errorMessages) 'the OutputQuote() function returns the full physical path the the file generated on the server

	//        state$ = "AR"
	//        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
	//        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

	//        Dim vPath = HttpContext.Current.Request.ApplicationPath
	//        Dim pPath = HttpContext.Current.Request.MapPath(vPath) & "\"


	//        state$ = "SR " & pPath
	//        Dim tr As StreamReader = Nothing
	//        Dim b$ = ""
	//        Try
	//            tr = New StreamReader(pPath & "EMT/quote.htm")
	//            b$ = tr.ReadToEnd()
	//            tr.Close()
	//        Catch ex As System.Exception
	//            tr.Dispose()
	//        End Try

	//        'Tags are...
	//        '<subject>Welcome to Iqoute 2</subject>
	//        '<p>Dear <customerName/>,</p>
	//        '<p>Your <hostName/> iQuote quotation ID:<quoteID/> prepared for you by <agentName/> is shown below - You will also find an spreadsheet compatible version attached.
	//        '<quoteBody/>

	//        state$ = "RT "

	//        Dim tags As Dictionary(Of String, String) = New Dictionary(Of String, String)
	//        tags.Add("customername", Split(buyerAccount.User.RealName, " ")(0))
	//        tags.Add("quoteid", QUOTE.RootQuote.ID & "-" & QUOTE.Version.ToString)
	//        tags.Add("hostname", buyerAccount.SellerChannel.DisplayName(buyerAccount.Language))
	//        tags.Add("agentname", agentAccount.User.RealName)

	//        Dim runningTotal As NullablePrice = New NullablePrice(buyerAccount.Currency)
	//        Dim qb$ = QUOTE.RootItem.EmailSummary(True, buyerAccount, agentAccount, errorMessages, runningTotal)
	//        tags.Add("quotebody", qb$)

	//        Dim to$
	//        to$ = buyerAccount.User.Email

	//        Dim attachment As System.Net.Mail.Attachment = New System.Net.Mail.Attachment(fullpath$)


	//        SendEmail(agentAccount.User.Email, "quote.htm", tags, buyerAccount.Language, errorMessages, False, attachment)

	//        'state$ = "IC "
	//        'Dim smtpclient As New System.Net.Mail.SmtpClient


	//        'msg = New MailMessage("support@channelcentral.net", to$, "Your iQuote 2 quotation" & QUOTE.RootQuote.ID & "-" & QUOTE.Version & " from " & buyerAccount.SellerChannel.DisplayName(buyerAccount.Language), b$)

	//        'msg.ReplyToList.Add(New MailAddress(AgentAccount.User.Email))
	//        'msg.CC.Add(New MailAddress("support@channelcentral.net"))
	//        'msg.CC.Add(New MailAddress(AgentAccount.User.Email))  'CC the agent

	//        If errorMessages.Count = 0 Then
	//            LblSave.BackColor = Drawing.Color.Green
	//            LblSave.ForeColor = Drawing.Color.White
	//            LblSave.Text = Xlt("Mail sent successfully", agentAccount.Language)
	//        Else
	//            'sendmail will have added errors if it failed (which will be output below)

	//        End If
	//    End If

	//    OutputErrors(PnlErrors.Controls, errorMessages, lid)

	//End Sub
	private string getAdverts(UInt64 lid)
	{
		Dictionary<int, clsAdvert> adverts = new Dictionary<int, clsAdvert>();
		clsQuote quote;
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		string adLiteral = "";
		if (iq.SeshContains(lid, "QuoteID")) {
			int quoteID = iq.seshTyped<int>(lid, "QuoteID");
			if (quoteID != 0) {
				quote = agentAccount.Quotes(quoteID);
				if (quote.RootItem.Children.Count == 0)
					quote.LoadItems(errorMessages);
			}
		}

		adverts.Clear();
		foreach (clsAdvert advert in from a in iq.Adverts.Valueswhere a.Manufacturer == Manufacturer.Unknown || a.Manufacturer == agentAccount.Manufacturer) {
			if (advert.Campaign.StartDate.Date <= Today.Date & advert.Campaign.EndDate.Date >= Today.Date) {
				if (object.ReferenceEquals(advert.Campaign.Seller, agentAccount.SellerChannel)) {
					if (advert.Campaign.Region.Encompasses(agentAccount.SellerChannel.Region)) {
						if (quote == null) {
							adverts.Add(advert.ID, advert);
						} else {
							bool addAdvert = true;
							//If advert.Present IsNot Nothing Then
							if (advert.Present.Code != "none") {
								if (!quote.RootItem.HasProductType(advert.Present)) {
									addAdvert = false;
								}
							}
							//If advert.Absent IsNot Nothing Then
							if (advert.Absent.Code != "none") {
								if (quote.RootItem.HasProductType(advert.Absent)) {
									addAdvert = false;
								}
							}
							if (addAdvert) {
								adverts.Add(advert.ID, advert);
							}
						}
					}
				}
			}
		}


		Random randomClass = new Random();
		int rndNumber;
		HashSet<int> RememberSet = new HashSet<int>();

		while (RememberSet.Count < 3 && adverts.Count > 1) {
			rndNumber = randomClass.Next(0, adverts.Count - 1);
			if (RememberSet.Add(rndNumber)) {
				clsAdvert selectedAdvert = adverts.Values(rndNumber);
				if (selectedAdvert != null) {
					clsImpression advertImpressions = new clsImpression(agentAccount, selectedAdvert, Now);
					string urlString = selectedAdvert.ImageUrl;
					if (string.IsNullOrEmpty(urlString) == false) {
						adLiteral += "<a onclick=\"clickthru(" + selectedAdvert.ID + ",'" + selectedAdvert.URL + "');\"><img classs = \"bannerimg\"  alt=\"\" src=\"" + urlString + "\"   /></a>";
					}
				}
			}

		}
		if (adLiteral.Length > 0) {
			if (iq.seshTyped<string>(lid, "Root").ToString() == iq.seshTyped<string>(lid, "path").ToString()) {
				adLiteral = "<div id=\"bannerAd\" class =\"bannerdivright\">" + adLiteral + " </div>";
			} else {
				adLiteral = "<div id=\"bannerAd\" class =\"bannerdivtop\">" + adLiteral + " </div>";
			}

		}

		return adLiteral;
	}
}
