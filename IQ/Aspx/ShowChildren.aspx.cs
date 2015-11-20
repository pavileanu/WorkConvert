
using IQ.clsBranchState;
//Allows 
using System.IO;
using System.Xml;

public class showbranch : clsPageLogging
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//If iq Is Nothing Then Response.Redirect("signin.aspx")

		//This is ASPX is called via Ajax/js getBranches 
		//request paramaters may include CMD,Path,Into

		//Generally a call this this page is going to perform some manipulation (as determined by the CMD)
		//.. and then spit out a set of branches - which will replace the content of the DIV at 'Path'
		//Often (for a simple 'open a branch') that content is the branch being opened - and its children.

		if (!clsIQ.IsLoaded)
			return;

		if (Request.QueryString("lid") == null)
			throw new Exception("ShowChildren was called without an LID ! - querystring was '" + Request.RawUrl + "'");


		UInt64 lid = Convert.ToUInt64(Request.QueryString("lid"));
		if (lid == 0)
			throw new Exception("LID evaluates to 0 - querystring was '" + Request.RawUrl + "'");


		// This is an ajax call so redirect IT to the login screen is a terrible idea!
		//If iq.SeshAlive(lid) = False Then Response.Redirect("signin.aspx", True) 

		//Dim cmd As String = Request("cmd")
		//Dim path As String = Request("path")
		float Treewidth = (float)Request("treewidth");
		float emConversion = (float)Request("emPixel");

		//happens if they cick a a system (or breadcrumb) in systems search
		if (Request("Paradigm") != "") {
			switch (UCase(Request("Paradigm"))) {
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"B":
					iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem;
					//Browse Mode
					iq.sesh(lid, "showOnly") = null;
					ClearBranchStates(lid);
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"C":
					//configure
					iq.sesh(lid, "Paradigm") = enumParadigm.configuringSystem;
				default:
					Beep();
			}
		}

		if (Request("showOnly") != "") {
			iq.sesh(lid, "showOnly") = Request("showOnly");
		} else {
			iq.sesh(lid, "showOnly") = 0;
		}

		if (Request("to") != "") {
			ClearBranchStates(lid);
		}

		enumParadigm Paradigm = (enumParadigm)iq.sesh(lid, "Paradigm");

		try {
			Debug.Print(Request.RawUrl + "xx");
		} catch (System.Exception ex) {
			if (ex.Message.Contains("A potentially dangerous Request.RawUrl value")) {
				errorMessages.Add("Tags are not permitted on this screen.");
			}
		}

		//Dim pth As String = If(Request("filterPath") IsNot Nothing, Request("filterPath").ToString(), Request("path")) 'Path generally equates to DivToFill - so for squares is often the root path (tree.1)
		string pth = Request("path");
		//Path generally equates to DivToFill - so for squares is often the root path (tree.1)
		if (pth == "") {
			pth = (string)iq.sesh(lid, "path");
		} else {
			iq.sesh(lid, "path") = pth;
			// pth 'tree.aspx will render from here in future (and if refreshed)
		}

		clsBranchInfo bi = new clsBranchInfo(lid, pth, null, Treewidth, Paradigm, errorMessages);
		//Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
		if (Request("Promolink") != null) {
			iq.sesh(lid, "promoinforce") = iq.Promos((int)Request("promolink")).Id;
		} else if (pth == "tree.1" & Request("into") == "tree" && iq.seshDic(lid).ContainsKey("refreshing") && !(bool)iq.sesh(lid, "refreshing")) {
			if (iq.seshDic(lid).ContainsKey("promoinforce"))
				iq.seshDic(lid).Remove("promoinforce");
			//   Dim sh = iq.seshTyped(Of Dictionary(Of String, clsScreenHeader))(lid, "screenHeaders")
			//     If sh IsNot Nothing AndAlso sh.ContainsKey("tree.1") Then
			bi.InvalidateMatrixBelow("tree.1");
			//End If
		}

		if (lid == 0) {
			errorMessages.Add("* Session ID was 0");
		} else if (iq.sesh(lid, "BuyerAccount") == null) {
			errorMessages.Add("Sorry, Your session has been reset - please log in again");
		}

		string cursorpath = "";

		//     If Request("path") = "" Then Stop
		string EndPath = string.Empty;
		string cmd = Request("cmd");

		if (errorMessages.Count == 0) {
			//The clsBranchInfo class encapsulates alot of parameters that need to be passed forward (into 
			//ProcessCommand returns information about the branch to render from

			clsBranchState bs = getbranchstate(bi.lid, bi.path);
			clsBranchState pbs = getBranchStateAbove(lid, bi.path, errorMessages);
			//we may be looking at united branches - their (direct) parent may never have been opened and has no state

			//   If pbs Is Nothing Then Stop

			//processcommand - returns the 'new' cursor path 
			//bi.divtofill is populated by processCommand (often to 'Path', sometimes (for OpenFrom's) to "tree"

			cursorpath = ProcessCommand(bi, bs, pbs, errorMessages);
			//NB: Both BI as BS can be manipulated - as some commands require us to render the tree from the root

			iq.sesh(bi.lid, "treeCursorPath") = cursorpath;
			if (bi.path == cursorpath)
				bi.isTreeCursor = true;

			Form.Controls.Add(NewLit("!DivToFill:" + bi.divToFill));
			//<tells JS showBranches() which Div to replace

			Form.Controls.Add(NewLit("!BeginBranches"));
			//<start of content marker
			if ((cmd == "shoppingList" | cmd == "optionsPriceList") & errorMessages.Count > 0) {
				string errorlist = "";
				foreach ( errMsg in errorMessages) {
					errorlist = errorlist + errMsg + "|";
				}
				errorlist = "!ToolsError" + errorlist + "!EndToolsError";
				Form.Controls.Add(NewLit(errorlist));
			} else {
				OutputErrors(Form.Controls, errorMessages, lid);
				//OUTPUT pre branch (post command) errors
			}

			//  errorMessages.Clear()

			//this recurses for any children that are open - rendering a (potentially large) segment of the tree
			//    Form.Controls.Add(ErrorDymo(Treewidth.ToString))


			Panel pnl = bi.branch.UI(bi, EndPath, errorMessages);
			//<<<HERE'S WHERE THE MAIN OUPUT IS GENERATED

			if (bi.divToFill == "tree") {
				Panel tp = new Panel();
				tp.ID = "tree";

				tp.Controls.Add(pnl);

				Panel bep = new Panel();
				bep.CssClass = "basketErrors";

				//these are shoppling list exceptions/warnings
				tp.Controls.Add(bep);
				bep.Controls.Add(outputMessages(bi.userMessages));

				OutputErrors(tp.Controls, errorMessages, lid);
				//OUTPUT pre branch (post command) errors

				Form.Controls.Add(tp);
			} else {
				Form.Controls.Add(pnl);
				//NB: the branch UI may include error messages
				OutputErrors(Form.Controls, errorMessages, lid);
				//OUTPUT pre branch (post command) errors
				//these are shoppling list exceptions/warnings
				Form.Controls.Add(outputMessages(bi.userMessages));
			}

		} else {
			//Form.Controls.Add(NewLit("!DivToFill:" & bi.path)) '<tell JS showBranches() which Div to replace
			Form.Controls.Add(NewLit("!DivToFill:tree"));
			//<tell JS showBranches() which Div to replace
			Form.Controls.Add(NewLit("!BeginBranches"));
			//<start of content marker
			OutputErrors(Form.Controls, errorMessages, lid, true);
			//OUTPUT ANY pre branch (post command) errors

		}

		//The oputput of this ASPX goes (as a result of the callback specified on the rExec in the JS getBranches()) 
		//to the JS ShowBranches Function... 
		Form.Controls.Add(NewLit("!EndBranches"));
		//End of content marker
		Form.Controls.Add(NewLit("!BreadCrumbs"));
		if (Paradigm != (enumBt)iq.sesh(lid, "Paradigm"))
			Paradigm = (enumParadigm)iq.sesh(lid, "Paradigm");
		if (Paradigm == enumParadigm.AddingSystem) {
			Form.Controls.Add(NewLit("<span class='paradigmIndicator'>Browsing<span>"));
		} else if (Paradigm == enumParadigm.configuringSystem) {
			Form.Controls.Add(NewLit("<span class='paradigmIndicator'>Configuring<span>"));
		} else if (Paradigm == enumParadigm.errorNotSet) {
			Form.Controls.Add(NewLit("<span class='paradigmIndicator'>NOT SET<span>"));

		}





		//MakeRoundButton("setwarehouse.png","Set the warehousetxtWarehouse.Items.Add("TST")

		if (cursorpath != "") {
			Form.Controls.Add(clsBranch.Breadcrumbs(lid, EndPath != string.Empty ? EndPath : cursorpath, ((clsAccount)iq.sesh(lid, "AgentAccount")).Language, errorMessages));
		}

		Form.Controls.Add(NewLit("!EndBreadcrumbs"));
		//End of content marker

		//Pass path and screen to client
		Form.Controls.Add(NewLit("!BeginPath" + pth + "!EndPath"));

		// HPE/HPI system messages - display only when we're at the top of the tree, and don't display if the
		// user suppressed them (clicked the X) in this session
		object messageHtml = string.Empty;
		object msgs = string.Empty;
		object agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		object suppressKey = string.Format("Suppress{0}SystemMessages", agentAccount.mfrCode);

		if ((!agentAccount.Manufacturer == Manufacturer.Unknown) && (pth == iq.sesh(lid, "Root").ToString()) && (iq.sesh(lid, suppressKey) == null)) {
			string key = string.Format("{0}SystemMessage", agentAccount.mfrCode.ToUpper());
			if (iq.UserMessages.ContainsKey(key)) {
				foreach (clsMessage message in iq.UserMessages(key).Where(m => (m.Enabled && m.ChannelID <= 1 && m.ValidFrom <= Today && m.ValidTo >= Today))) {
					msgs += string.Format("<p>{0}</p>", Server.HtmlDecode(message.Translation.text(agentAccount.Language)));
				}
				if (!string.IsNullOrEmpty(msgs))
					messageHtml = string.Format("<div ID='systemMessage' ClientIDMode='Static' runat='server'>{0}<a id='closeButton' onclick=\"burstBubble(event);HideSystemMessage();\"></a></div>", msgs);
			}

			// Also display any channel-specific messages found
			key = "ChannelMessage";
			if (iq.UserMessages.ContainsKey(key)) {
				foreach (clsMessage message in iq.UserMessages(key).Where(m => (m.Enabled && m.ChannelID == agentAccount.SellerChannel.ID && m.ValidFrom <= Today && m.ValidTo >= Today))) {
					msgs += string.Format("<p>{0}</p>", Server.HtmlDecode(message.Translation.text(agentAccount.Language)));
				}
				if (!string.IsNullOrEmpty(msgs))
					messageHtml = string.Format("<div ID='systemMessage' ClientIDMode='Static' runat='server'>{0}</div>", msgs);
			}

		}
		Form.Controls.Add(NewLit(string.Format("!BeginMessages{0}!EndMessages", messageHtml)));

		Form.Controls.Add(NewLit("!BeginBanner" + getAdverts(lid, EndPath) + "!EndBanner"));
		if (Request("cmd") == "optionsPriceList") {
			Form.Controls.Add(NewLit("!Beginexp" + cursorpath + "!Endexp"));

		}
		if (bi.EffectiveHeader != null)
			Form.Controls.Add(NewLit("!BeginScreen" + bi.EffectiveHeader.screen.ID + "!EndScreen"));

		//Dim quoteHasItems As Boolean = False
		//If iq.SeshContains(lid, "QuoteID") Then
		//    Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
		//    Dim quote As clsQuote = agentAccount.Quotes(CInt(iq.sesh(lid, "QuoteID")))
		//    If quote.RootItem.Children.Count > 0 Then quoteHasItems = True
		//End If

		//Form.Controls.Add(NewLit("!State"))
		//If quoteHasItems Then
		//    Form.Controls.Add(NewLit("35em"))
		//Else
		//    Form.Controls.Add(NewLit("1em"))
		//End If
		//Form.Controls.Add(NewLit("!End"))  'End of content marker

	}

	protected void SuppressSystemMessages(object sender, EventArgs e)
	{
		System.Diagnostics.Debugger.Break();
	}


	/// <summary>Performs some manipulation generally on the state of the branch at path - based on the CMD. </summary>
	/// <param name="cmd">'branch','close',switch to 'squares' (etc.)</param>
	/// <returns>A clsBranchInfo object saying what to render</returns>
	/// <remarks>Generally alters sesh variables which affect the subsequent appearance of the tree.</remarks>
	/// returns the 'TreeCursortPath'
	private string ProcessCommand(ref clsBranchInfo bi, ref clsBranchState branchState, clsBranchState pbs, ref List<string> errormessages)
	{

		//Dim treeCursorPath As String = CType(iq.sesh(bi.lid, "treeCursorPath"), String)

		//Dim msgs As List(Of String) = New List(Of String) 'We'll populate this list with any (shopping list/swift) errors
		//Dim parentpath = oneAbove(bi.path)
		// Dim p() As String

		string cmd;

		// p = Split(cmd, "=")
		cmd = Request("cmd");
		//p(0)
		if (InStr(cmd, "=") != 0)
			System.Diagnostics.Debugger.Break();

		ProcessCommand = bi.path;
		//return the path (to become the treecursor)

		bi.divToFill = ProcessCommand;
		if (Request("into") != "") {
			bi.divToFill = Request("into");
			//If bi.divToFill <> "tree" Then Stop ML - Removed
		}

		if (cmd.Length > 0) {
			iq.sesh(bi.lid, "previouscommand") = cmd;
		} else {
			cmd = iq.sesh(bi.lid, "previouscommand").ToString();
		}

		clsBranch branch = iq.Branches((int)Split(ProcessCommand, ".").Last);

		//Some (but not all) commands manipulate the descendants - so only get them if necessary (it's relatively expensive)
		Dictionary<clsBranch, clsVisibility> descendants = null;

		Dictionary<string, clsScreenHeader> mhs = (Dictionary<string, clsScreenHeader>)iq.sesh(bi.lid, "screenHeaders");

		switch (cmd) {

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"setWarehouse":
				//BRAZIL

				bi.buyerAccount.wareHouseFilter = Request("warehouse");
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"quoteAll":

				clsAccount agentAccount = (clsAccount)iq.sesh(bi.lid, "agentAccount");
				clsAccount buyerAccount = (clsAccount)iq.sesh(bi.lid, "buyerAccount");

				int quoteID = (int)iq.sesh(bi.lid, "quoteid");
				clsQuote quote;
				if (quoteID == 0) {
					NullablePrice NullPrice = new NullablePrice(buyerAccount.Currency);
					quote = new clsQuote(buyerAccount, agentAccount, null, Now, Now, (int)1, iq.i_state_GroupCode("QT-#NW"), NullPrice, buyerAccount.Currency, false,
					false, false, string.Empty, new nullableString(), new nullableString(), 0);
					iq.sesh(bi.lid, "QuoteID") = quote.ID;
				} else {
					quote = agentAccount.Quotes(quoteID);
				}

				List<string> results = new List<string>();
				branch.QuoteAllSystemsBelow(bi.lid, bi.path, quote, errormessages, results);


				branch.message = "<PRE>" + Join(results.ToArray, vbCrLf) + "</PRE>";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"expandpanel":
				object ky = Request("key");

				iq.sesh(bi.lid, ky) = "x";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"collapsepanel":
				object ky = Request("key");

				iq.seshDic(bi.lid).Remove(ky);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"adopt":
				//(reparent - many branches )

				wipeCachedDataView(bi.path, bi.lid);
				clsBranch newParent = iq.Branches((int)Split(bi.path, ".".ToCharArray).Last);

				//the JS compiles a list of sources from the checked branches (their paths)
				clsBranch sourcebranch;
				foreach ( s in Request("sources").Split(",".ToCharArray)) {
					//the JS untidily leaves an extra comma - but it's easier to deal with here 
					if (s != "") {
						sourcebranch = iq.Branches((int)Split(s, ".").Last);
						sourcebranch.Parent.childBranches.Remove(sourcebranch.ID);
						//otherwise we leave a copy behind !
						sourcebranch.Parent = newParent;
						sourcebranch.Update(errormessages);

						foreach ( slot in sourcebranch.slots.Values) {
							if (slot.path != "")
								slot.path = bi.path + "." + sourcebranch.ID;
							slot.update(errormessages);
						}

						foreach ( q in sourcebranch.slots.Values) {
							if (q.path != "")
								q.path = bi.path + "." + sourcebranch.ID;
							q.update(errormessages);
						}
					}
				}


				//     clsQuoteItem.replacepaths()

				bi.divToFill = "tree." + iq.RootBranch.ID.ToString;
				bi.branch = iq.RootBranch;

				bi.path = bi.divToFill;
			//slots and quantities (on descendants) will need re pathing
			//any grafts and prunes in force will need manipulating
			//some quoteitems paths may be invalidated
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"unprune":

				branch.Prunes((int)Request("id")).delete();
			//branch.Prunes.Remove(CInt(Request("id"))) - the delete (above) does this

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"snap":
				//XMK serialize /snapshot

				clsBranch b = iq.Branches((int)bi.path.Split(".".ToCharArray).Last);


				//When called on a system we will 'cross SKus' (recurse the options)
				bool crossSKUs = false;
				if (b.Product != null && b.Product.isSystem)
					crossSKUs = true;

				XmlTextWriter xmlw = new XmlTextWriter("c:\\temp\\snap.xml", Encoding.UTF8);
				//'xmlw.WriteRaw(Replace("<?xml version='1.0' encoding='UTF-8' standalone='yes' ?>", "'", Chr(34)))
				object pth = bi.path;
				List<string> em = new List<string>();
				b.serializeRecursive(bi, 0, pth, xmlw, crossSKUs, errormessages);
				xmlw.Close();

				Utility.writeSystemsBelow(b);

				Utility.writeOptionsBelow(b);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"deleteVariant":
				clsVariant v__1 = iq.Variants((int)Request("ID"));

				v__1.Delete(errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"deleteSlot":
				clsSlot s = branch.slots((int)Request("ID"));

				s.deleted = true;
				s.update(errormessages);

				string newpath = bi.path;
				clsBranch sysbranch = branch.FindSystemAbove(bi.path, newpath);

				bi = new clsBranchInfo(bi.lid, newpath);
				bi.divToFill = newpath;

				bi.branch = sysbranch;

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"unDeleteSlot":
				clsSlot s = branch.slots((int)Request("ID"));

				s.deleted = false;
				//new 'soft' delete
				s.update(errormessages);

				string newpath = bi.path;
				clsBranch sysbranch = branch.FindSystemAbove(bi.path, newpath);
				bi = new clsBranchInfo(bi.lid, newpath);
				bi.divToFill = newpath;

				bi.branch = sysbranch;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"deleteBranch":

				branch.deleted = true;
				//branch is determined by the last segement of request("path")

				branch.Update(errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"previewShredBranch":


				Dictionary<string, int> counts = new Dictionary<string, int>();
				//total numbers of records by type affected
				string summary = "";
				branch.HardDelete(errormessages, summary, 0, false, counts);
				string tt = Xlt("Shred branch (completely destroys this branch and all its descendants and dependecies) - as above (you cannot undo !)", bi.agentAccount.Language);

				Literal btn = CoreCode.MakeRoundButton("shredBranch.png", tt, clsBranch.ButtonScript("cmd=shredBranch&path=" + bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language);
				// the <pre> tag tells the browser this is preformatted text, it will be rendered in fixed pitch with tabs, spaces and CRLF's preserved
				branch.message = "<div class='shredSummary'>" + summary + btn.Text + "&lt;--CAUTION </div>";

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"ShredBranch":

				string summary = "";
				Dictionary<string, int> counts = new Dictionary<string, int>();
				//total numbers of records by type affected
				branch.HardDelete(errormessages, summary, 0, true, counts);

				branch.message = "<div class='Shredsummary'>" + summary + "</div>";

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"unDeleteBranch":

				branch.deleted = false;

				branch.Update(errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"deleteQuantity":
				clsQuantity q = iq.Quantities((int)Request("ID"));
				//c.delete(errormessages)
				q.deleted = true;

				q.update(errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"unDeleteQuantity":
				clsQuantity q = iq.Quantities((int)Request("ID"));
				//c.delete(errormessages)
				q.deleted = false;

				q.update(errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"deleteProductAttribute":
				int paid = (int)Request("PAID");
				int ProdId = (int)Request("ID");
				clsProduct product = iq.Products(ProdId);
				clsProductAttribute pa = product.Attributes(paid);
				pa.deleted = true;
				pa.update(errormessages);

				//de-index - it to make it 'dissapear' from the UI (It will be 'really' gone next time the OM is loaded)
				product.i_Attributes_Code(pa.Attribute.Code).Remove(pa);
				if (product.i_Attributes_Code(pa.Attribute.Code).Count == 0) {
					product.i_Attributes_Code.Remove(pa.Attribute.Code);

				}

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"unDeleteProductAttribute":
				int paid = (int)Request("PAID");
				int ProdId = (int)Request("ID");
				clsProduct product = iq.Products(ProdId);
				clsProductAttribute pa = product.Attributes(paid);

				//re-index - it to make it 'reappear' in the UI
				if (!product.i_Attributes_Code.ContainsKey(pa.Attribute.Code)) {
					product.i_Attributes_Code.Add(pa.Attribute.Code, new List<clsProductAttribute>());
				}
				product.i_Attributes_Code(pa.Attribute.Code).Add(pa);

				pa.deleted = false;

				pa.update(errormessages);

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"showQuickFilters":
				//show (existing) quickfilters
				bi.setQuickFiltersVisible(true);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"hideQuickFilters":
				bi.setQuickFiltersVisible(false);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"shoppingList":
				// set the branch to tree.1
				branch = iq.Branches((int)Split("tree.1", ".").Last);


				clsAccount agentAccount = (clsAccount)iq.sesh(bi.lid, "AgentAccount");
				string shoppingListSku = Request("list");

				if ((string.IsNullOrEmpty(shoppingListSku))) {
					//If (Not (errormessages.Contains("Please Enter An SKU (Some Text) In The Import Box."))) Then
					errormessages.Add("Please enter a SKU (some text) in the Import box.");
					//End If
				}

				shoppingListSku = Replace(shoppingListSku, vbCrLf, vbCr);
				//Switch all delimiters to CR's
				shoppingListSku = Replace(shoppingListSku, ";", vbCr);
				shoppingListSku = Replace(shoppingListSku, ",", vbCr);

				string[] p = Split(shoppingListSku, vbCr);
				clsProduct systemProduct;
				HashSet<clsBranch> checkedBranches = new HashSet<clsBranch>();
				Dictionary<string, clsBranch> productPath = new Dictionary<string, clsBranch>();
				int lastItem = p.Length - 1;
				if (p(lastItem).Contains("*"))
					p(lastItem) = Split(p(lastItem), "*")(0);
				p(lastItem) = Trim(p(lastItem));

				while (productPath.Count == 0) {
					if (iq.i_SKU.ContainsKey(p(lastItem))) {
						systemProduct = iq.i_SKU(p(lastItem));
						if (systemProduct.isSystem) {
							productPath = branch.findProductBranches("tree.1", agentAccount.SellerChannel, systemProduct, false, checkedBranches, true);
						}
					}
					lastItem = lastItem - 1;
					if (lastItem < 0)
						break; // TODO: might not be correct. Was : Exit While
					if (p(lastItem).Contains("*"))
						p(lastItem) = Split(p(lastItem), "*")(0);
					p(lastItem) = Trim(p(lastItem));

				}
				bi.userMessages = clsQuote.FromShoppingList(bi.lid, bi.agentAccount, bi.buyerAccount, Request("list"), errormessages);
				//the usermessages are rendered out


				if (bi.userMessages.Count == 0 & errormessages.Count == 0) {
					if (productPath.Count > 0) {
						bi.branch = productPath.Values(0);

						bi.path = productPath.Keys(0);
						//this is the only place (other than the branchinfo constructor) that I set Path - Marting has (totally legitimately) switched it to readonly - but i need set it for shopping list and don't have the time for a bigger restructure NA
					} else {
						bi.branch = branch;
					}
					string[] parentPathArray = Split(bi.path, ".");
					string[] parentpathreduced = parentPathArray.Take(parentPathArray.Length - 1).ToArray();
					string parentPAth__2 = Join(parentpathreduced, ".");

					bi.divToFill = "tree";
					bi.Paradigm = enumParadigm.configuringSystem;
					//new (first level branches beneath systems were rendering as squares)
					branchState = new clsBranchState(bi.lid, parentPAth__2, enumBt.Branch, false, bi.rownum, 100);
					iq.sesh(bi.lid, "Paradigm") = enumParadigm.configuringSystem;

				} else {
					iq.sesh(bi.lid, "Paradigm") = enumParadigm.AddingSystem;
					//    bi = New clsBranchInfo(bi.lid, "tree.1", Nothing, bi.treeWidth, enumParadigm.AddingSystem, errormessages)
					//  iq.sesh(bi.lid, "refreshing") = True
					if (bi.userMessages.Count > 0) {
						foreach ( msg in bi.userMessages) {
							errormessages.Add(msg);
						}
					}
				}

				Open(bi, "open", descendants);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"optionsPriceList":

				string systemSKU = Request("systemsku");

				iq.sesh(bi.lid, "systemSKU") = systemSKU;
				iq.sesh(bi.lid, "toolsCSVExport") = true;

				clsBranchInfo systemBI = bi;

				enumBt branchType = branchState.rca;
				bool switchedBranchType = false;
				if (!branchState.rca == enumBt.gridrow) {
					systemBI.switchTo(enumBt.gridrow, errormessages);
					switchedBranchType = true;
				}

				string sysPath = FindSystemPath(bi, systemBI, systemSKU, errormessages);

				bool invalidSku = false;
				try {
					systemBI = new clsBranchInfo(bi.lid, sysPath, null, bi.treeWidth, bi.Paradigm, errormessages);
				} catch (Exception ex) {
					invalidSku = true;
				}

				if ((!systemBI == null) && (!systemBI.branch == null) && (!systemBI.branch.Product == null) && (systemBI.branch.Product.Manufacturer != systemBI.agentAccount.Manufacturer)) {
					invalidSku = true;
				}

				if (invalidSku) {
					errormessages.Add(Xlt(" Part number not recognised. Please enter a valid SKU for this manufacturer.", systemBI.agentAccount.Language));
				}

				if (errormessages.Count == 0) {
					ProcessCommand = bi.path;
					bi.divToFill = "tree";

					int priceConfig = systemBI.buyerAccount.SellerChannel.priceConfig;
					object hideReasons = systemBI.branch.ReasonsForHide(systemBI.buyerAccount, systemBI.foci, sysPath, priceConfig, false, errormessages);

					clsVisibility sysBiVisibility = new clsVisibility(systemBI.branch, sysPath, hideReasons);

					descendants = systemBI.visibleChildren(errormessages, true, 0, 0, true, false, true);


					if (!descendants.ContainsKey(systemBI.branch)) {
						// Add the system branch
						descendants.Add(systemBI.branch, sysBiVisibility);

						// Sort by:
						// 1 - system branch to the top
						// 2 - product type
						// 3 - product
						descendants = descendants.OrderBy(x => object.ReferenceEquals(x.Key, systemBI.branch) ? 0 : 1).ThenBy(x => x.Key.Product.ProductType.Code).ThenBy(x => x.Key.order).ToDictionary(x => x.Key, y => y.Value);

						clsScreenHeader screenHeader = new clsScreenHeader(bi, descendants, false);

						screenHeader.screen = iq.i_screens_code("ExCSV");

						screenHeader.exportCSV(systemBI.lid, descendants, systemBI.buyerAccount, systemBI.agentAccount.Language, systemBI.foci, errormessages, true, true);
					} else {
						errormessages.Add(" Please enter a valid SKU.");
					}

				}

				if (switchedBranchType) {
					bi.switchTo(branchType, errormessages);
				}

				bi.divToFill = string.Empty;


				return string.Empty;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"exportGrid":

				//'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
				descendants = bi.visibleChildren(errormessages, true, 0, 0, true, true);

				bi.EffectiveHeader.exportCSV(bi.lid, descendants, bi.buyerAccount, bi.agentAccount.Language, bi.foci, errormessages, false, true);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"showProducts":
				//Shows an (arbitrary) list of products (as a matrix)
				string lst = Request("list");
				if (lst != "") {
					int branchid = 0;
					//Msgs are 'error's which we *DO* neeed to espose

					bi = new clsBranchInfo(bi.lid, "tree.1", bi.lblMatches, bi.treeWidth, bi.Paradigm, errormessages);
					// A showProducts (swift2) command looks likes this getbranches('tree.1','showProducts=ABC123,253728-B21,9284749')
					bi.userMessages = ShowProducts(bi.lid, Request("list"), branchid, bi.treeWidth, errormessages);
					//bi.path = "tree." & branchid  'We pass the temporary (negative) branchID back as the bi.path -  I admit - this isn't very pretty 
					bi.branch = iq.Branches(branchid);
					//this will be negative
					bi.divToFill = "tree";

					//invalidate any headers we may have
					object pth = "tree." + branchid;
					Dictionary<string, clsScreenHeader> matrixHeaders = (Dictionary<string, clsScreenHeader>)iq.sesh(bi.lid, "screenHeaders");
					if (matrixHeaders.ContainsKey(pth))
						matrixHeaders.Remove(pth);

					clsBranchState bs = bi.open(errormessages, true);
					bi.switchTo(enumBt.gridrow, errormessages);
					bs.rca = enumBt.gridrow;


				}
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"switchTo":

				enumBt bt = enumBt.errorNotSet;
				//to notset (in which case we will use the Branch.rca property - see bi.Open
				string typechar = Request("bt");
				bt = (enumBt)BTchar.IndexOf(typechar);
				bi.switchTo(bt, errormessages);
			//bi.InvalidateMatrixBelow(bi.path, True)

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"promofilter":
				descendants = bi.visibleChildren(errormessages, true, 0, 0, false, true);
				//get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice

				bi.CreateMatrixHeader(descendants);
				//If Not mhs.ContainsKey(bi.path) Then
				//    bi.matrixHeader = New clsScreenHeader(bi, descendants, True, errormessages) 'this creates the clsmatrix header AND stores it in the users session
				//End If

				//See if the promo field is filterable??
				if (bi.ScreenHeader.screen.i_field_property.ContainsKey("promos(" + Request("promoType") + ")")) {
				//MH Removal bi.ScreenHeader.UpdateFilters(CType(bi.ScreenHeader.matrix.i_field_property("promos(" + Request("promoType") + ")").ID, String) + "|EQ|1")
				} else {
					//Do something, what??
				}


				Open(bi, "open", descendants);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"open":
			case "openTab":
				//this sets how the descendants will be rendered - 'Read as - set (or change) the way you render your children - consolidate with view maybe
				if (iq.sesh(bi.lid, "custContext") != null) {
					clsCustomerContext custContext = new clsCustomerContext();
					custContext = (clsCustomerContext)iq.sesh(bi.lid, "custContext");
					bi.buyerAccount.wareHouseFilter = custContext.WareHouse;
				}


				Open(bi, cmd, descendants);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"openFiltered":

				descendants = bi.visibleChildren(errormessages, true, 0, 0, false, true);
				//get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
				bi.CreateMatrixHeader(descendants, true);
				//this creates the clsmatrix header AND stores it in the users session
				string filterFieldNotFound = "";
				bi.ScreenHeader.SetDefaultFilterOn(filterFieldNotFound);
				bi.setQuickFiltersVisible(true);
				if (filterFieldNotFound != "") {
					clsField fld = (from f in bi.ScreenHeader.Filters.Keyswhere f.ID == (int)filterFieldNotFound).FirstOrDefault;
					if (fld != null) {
						bi.ScreenHeader.Filters.Remove(fld);

					}
				}
				descendants = bi.visibleChildren(errormessages, true, 0, 0, true, true);
				//If descendants.Count = 0 Then
				//    Dim strFailedFilters As String = bi.ScreenHeader.Vw.RowFilter
				//    Dim newFilterString As List(Of String) = New List(Of String)
				//    Dim filterString() As String = Split(strFailedFilters, "AND")

				//    Dim r As Random = New Random
				//    ' Get random numbers between 1 and 3.
				//    ' ... The values 1 and 2 are possible.
				//    d(r.Next(0, 3))
				//    Console.WriteLine(r.Next(0, 3))
				//    Console.WriteLine(r.Next(1, 3))
				//End If

				Open(bi, cmd, descendants);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"openSquare":
				CloseAbove(bi.lid, bi.path);

				Open(bi, cmd, descendants);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"close":
				bi.close(errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"unite":
				//(unite - view all descendant products)
				branchState.United = true;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"divide":
				//(divide - view categories)
				//It's important we flatten *after* having found the first visible child.                
				branchState.United = false;

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"sort":
				//update the sort orders

				clsPriorityDirection pd;
				object V__3;
				if (Request("value") != "") {
					V__3 = Request("value");
					pd = new clsPriorityDirection(V__3);
				} else {
					pd = new clsPriorityDirection(iq.Fields((int)Request("colID")), (int)Request("priority"), (string)Request("direction"));
				}


				bi.ScreenHeader.UpdateSorts(pd);

				descendants = bi.visibleChildren(errormessages, true, 0, 0, true, true);

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"removeSort":

				bi.ScreenHeader.RemoveSort((int)Request("priority"));
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"clearFilter":

				bi.ScreenHeader.ClearFilter(Request("filterId"));

				bi.InvalidateMatrixBelow(bi.path, false);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"clearGroupFilter":

				bi.ScreenHeader.ClearGroupFilter(Request("filterId"));

				bi.InvalidateMatrixBelow(bi.path, false);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"changeFilter":

				//  If descendants Is Nothing Then Stop
				// descendants = bi.visibleChildren(errormessages, True, 0, 0, True, True)  '

				bi.ScreenHeader.UpdateFilters(Request("filterParams"));


				bi.InvalidateMatrixBelow(bi.path, false);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"removeFilter":
				//Removes ONE of the filters from an active matrix header
				object fp = Request("filterPath");
				string ru = Request.RawUrl;

				mhs(fp).RemoveFilter(Request("filterParams"), errormessages);

				bi.InvalidateMatrixBelow(fp);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"removeFilters":
				//removes ALL of the filters from an active header

				object fp = Request("filterPath");
				mhs(fp).removeFilters();
				mhs(fp).QuickFiltersVisible = false;
				//hide the (now empty) filters

				//MH Removal - do we need this now/rebuild every view/databatale 'below' this
				//For Each pth In mhs.Keys
				//    Dim cbi As clsBranchInfo = New clsBranchInfo(bi.lid, pth, Nothing, bi.treeWidth, bi.Paradigm, errormessages)
				//    descendants = cbi.visibleChildren(errormessages, True, 0, 0, True, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
				//    mhs(pth).rebuild(cbi, descendants, errormessages)

				//Next
				Open(bi, "openSquare", descendants);
				//Dim bs As clsBranchState = bi.open(errormessages, True) '<<<THIS DOES THE BIZ (creates branchstate - including RCA)
				//bs.rca = enumBt.OpenSquare
				//Dim bs As clsBranchState = New clsBranchState(bi.lid, "tree.1", enumBt.OpenSquare, False, 0, 1000)
				//Beep()

				bi.InvalidateMatrixBelow(bi.path);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"maxrows":
				iq.sesh(bi.lid, "maxrows." + bi.path) = (int)Request("rows");
				branchState.maxChildren = (int)Request("rows");
			//  bi.path = "tree.1"
			//   bi.Branch = iq.Branches(1)

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"expandColumn":
				bi.ScreenHeader.setColState(iq.Fields((int)Request("fieldid")), enumColState.HardExpanded);

				bi.ScreenHeader.CollapseColumns(bi.treeWidth, errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"collapseColumn":

				bi.ScreenHeader.setColState(iq.Fields((int)Request("fieldid")), enumColState.HardCollapsed);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"defFilterOn":
				Open(bi, cmd, descendants);

				object parentPath__4 = Request("to").Replace("." + Split(Request("to"), ".").Last.ToString, "");
				clsBranchInfo bi3 = new clsBranchInfo(bi.lid, parentPath__4, null, bi.treeWidth, bi.Paradigm, errormessages);
				//Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)

				// AndAlso bi3.branch.rca.Contains("M") Then
				if (bi3 != null) {
					//Dealing with an L3 (ROK) switch the parent and set up the filters on the children
					bi3.switchTo(enumBt.helpMechoose, errormessages);
					// Switch to special HMC view (if its available on the parent branch)
					foreach ( child in bi3.branch.childBranches.Values) {
						clsBranchInfo bi_child = new clsBranchInfo(bi.lid, parentPath__4 + "." + child.ID.ToString, null, bi.treeWidth, bi.Paradigm, errormessages);
						//Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
						descendants = bi_child.visibleChildren(errormessages, true, 0, 0, false, true);
						//get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
						bi_child.CreateMatrixHeader(descendants, true);
						//this creates the clsmatrix header AND stores it in the users session
						//bi_child.ScreenHeader.SetDefaultFilterOn()
						object srt = bi_child.ScreenHeader.FieldResultSet.Where(f => f.Key.propertyName.Contains("technology")).FirstOrDefault;
						if (srt.Key != null) {
							bi_child.ScreenHeader.UpdateSorts(new clsPriorityDirection(srt.Key, 1, "desc"));
						}

						bi_child.setQuickFiltersVisible(true);
					}
				} else {
					clsBranchInfo bi2 = new clsBranchInfo(bi.lid, Request("to"), null, bi.treeWidth, bi.Paradigm, errormessages);
					//Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
					descendants = bi2.visibleChildren(errormessages, true, 0, 0, false, true);
					//get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
					bi2.CreateMatrixHeader(descendants, true);
					//this creates the clsmatrix header AND stores it in the users session
					bi2.ScreenHeader.SetDefaultFilterOn();
					bi2.setQuickFiltersVisible(true);

				}
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"quickFilter":
				//makes (and shows) a set of quickfilters on this branch  switches to grid and showsQuickFilter (help me choose button)

				//   branchState.United = True
				//descendants = bi.visibleChildren(errormessages, True, 0, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice

				//If Not mhs.ContainsKey(bi.path) Then
				//    bi.matrixHeader = New clsScreenHeader(bi, descendants, True, errormessages) 'this creates the clsmatrix header AND stores it in the users session
				//End If

				//We fetch all prices so filtering and sorting can work (except the datatable isn't updated ! ( ) - the re-sort button needs to to repopulate the datatable (from the OM)
				//         For Each b In descendants.Keys
				//b.Product.GetPrices(bi.BuyerAccount, bi.BuyerAccount.SellerChannel.priceConfig, iq.AllVariants, errormessages, True) 'call the webservice for ALL descendants (becuase we will want to sort by price)
				//Next

				bi.setQuickFiltersVisible(true);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"removePromoLink":
				if (iq.seshDic(bi.lid).ContainsKey("promoinforce"))
					iq.seshDic(bi.lid).Remove("promoinforce");
				//Need to wipe out the promo in force field defs
				//If iq.seshTyped(Of List(Of String))(bi.lid, "pathDataLoaded") IsNot Nothing Then iq.seshTyped(Of List(Of String))(bi.lid, "pathDataLoaded").Clear()

				Open(bi, "openSquare", descendants);
				bi.InvalidateMatrixBelow(bi.path);
			//CloseBelow(bi.lid, "tree.1")
			//iq.seshTyped(Of Dictionary(Of String, clsBranchState))(bi.lid, "branchStates").Clear()
			//PloughPath(bi.lid, bi.path, errormessages, bi.treeWidth, bi.Paradigm)

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"lock":
				branch.locked = true;

				branch.Update(errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"unlock":
				branch.locked = false;

				branch.Update(errormessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"":
				//It's OK to have no CMD
				Beep();
			default:
				errormessages.Add("Unrecognised command " + cmd);
		}
		//ProcessCommand = "tree"
	}
	private void Open(ref clsBranchInfo bi, string cmd, ref Dictionary<clsBranch, clsVisibility> descendants)
	{
		//   Dim bsa As clsBranchState = getbranchstate(bi.lid, bi.path)
		//   If bsa IsNot Nothing Then
		// descendants = bi.visibleChildren(errormessages, True, 0, 0, True, False)
		// If descendants.Count = 0 Then Stop
		// End If
		//D are 'detail' squares
		if (getbranchstate(bi.lid, bi.path) == null & bi.branch.rca.StartsWith("D")) {
			//First open, no switch, lets fill out the matrixheader - not sure this is great but it works and doesnt do anything unnescessary
			Dictionary<string, clsScreenHeader> sh = (Dictionary<string, clsScreenHeader>)iq.sesh(bi.lid, "screenHeaders");
			if (!sh.ContainsKey(bi.path)) {
				if (descendants == null)
					descendants = bi.visibleChildren(errorMessages, true, 0, 0, true, true);
				if (bi.ScreenHeader == null)
					bi.CreateMatrixHeader(descendants, false);
				bi.ScreenHeader.QuickFiltersVisible = false;
			}
		}
		if (!iq.SeshContains(bi.lid, "refreshing") || iq.sesh(bi.lid, "refreshing") == null && Request("To") == null)
			CloseBelow(bi.lid, bi.path);
		iq.sesh(bi.lid, "refreshing") = null;

		clsBranchState bs = null;

		if (Request("to") != "") {
			object topath = Request("to");
			PloughPath(bi.lid, Request("to"), errorMessages, bi.treeWidth, bi.Paradigm);
			CloseAbove(bi.lid, topath);
			//*KW
		} else {
			bs = bi.open(errorMessages, true);
			//<<<THIS DOES THE BIZ (creates branchstate - including RCA)
		}

		//Opening a tab renders from (and into) its parent (to refresh the sibilings, one of which must must be deactivated)
		if (cmd == "openTab" | cmd == "openFiltered") {
			HideSiblings(bi.lid, bi.path);
			bi = new clsBranchInfo(bi.lid, Left(bi.path, InStrRev(bi.path, ".") - 1), bi.lblMatches, bi.treeWidth, bi.Paradigm, errorMessages);
			//bi.path = Left$(bi.path, InStrRev(bi.path, ".") - 1)
			bi.divToFill = bi.path;
			//Left$(bi.path, InStrRev(bi.path, ".") - 1)
			//bi.Branch = iq.Branches(CInt(Split(bi.path, ".").Last)) 'note the bi.path has already been trimmed
		}

	}





	private string FindSystemPath(ref clsBranchInfo originalbi, ref clsBranchInfo systembi, sku, ref List<string> errorMessages)
	{

		//Displays All options for a specified system
		//return the path to the system

		clsTranslation part = iq.AddTranslation("Part", English, "UI", 0, null, 0, false);
		clsTranslation parts = iq.AddTranslation("Parts", English, "UI", 0, null, 0, false);

		clsAccount agentAccount = (clsAccount)iq.sesh(originalbi.lid, "AgentAccount");

		clsProduct systemProduct;
		object systemPath = "";

		object partno;
		sku = Replace(sku, vbTab, "");
		//remove any crap they might have pasted in
		sku = Replace(sku, vbCrLf, "");
		sku = Replace(sku, vbLf, "");

		partno = Trim(sku);
		if (iq.i_SKU.ContainsKey(partno)) {
			systemProduct = iq.i_SKU(Trim(sku));
			HashSet<clsBranch> checkedBranches = new HashSet<clsBranch>();

			Dictionary<string, clsBranch> locations = iq.Branches(1).findProductBranches("tree.1", agentAccount.SellerChannel, systemProduct, false, checkedBranches, true);

			systemPath = locations.Keys(0);
			clsBranch systemBranch = locations.Values(0);

			//we pass an additional parameter with the matrix command which is the 'real' path
			//simply' open the system branch (with a matrix)

			//  iq.sesh(bi.lid, "treeCursor") = systemPath
			//  CloseBelow(bi.lid, systemPath) '@@

			//Dim nbi As clsBranchInfo = New clsBranchInfo(bi.lid, systemPath, Nothing, bi.treeWidth, enumParadigm.configuringSystem, errorMessages)

			//          nbi.InvalidateMatrixBelow(bi.path, True)


			if (systemPath != null) {
				//ML - added back in, path should NEVER be changed in branch info, its the key and has attached matrix headers which will be cached against the wrong path
				//'   systembi = New clsBranchInfo(originalbi.lid, systemPath, originalbi.lblMatches, originalbi.treeWidth, originalbi.Paradigm, errorMessages)

				//bi.path = systemPath
				//bi.branch = systemBranch

				//'systembi.ScreenHeader = Nothing
				//'systembi.switchTo(enumBt.gridrow, errorMessages) 'was gridrow
				//'systembi.open(errorMessages, False)

				//Dim bs As clsBranchState = nbi.open(errorMessages, True)

				//    bi.EffectiveHeader = matrixHeaderAbove(bi.lid, bi.path, errorMessages)
				//   Debug.Print(bi.EffectiveHeader.screen.code)

				//Dim descendants As dictionary(of clsbranch,clsvisibility) = bi.visibleChildren(errorMessages, True, 0, 0, False, False)
				//Dim mh As clsScreenHeader = New clsScreenHeader(bi, descendants, False, errorMessages) ' reference is held in the users Sesh (so this wont go out of scope)
			}
		} else {
			// SK - a duplicate error message is already displayed by ProcessCommand
			//errorMessages.Add("Part number " & sku$ & " is not recognised.")
		}

		return systemPath;

	}


	private void ClearBranchStates(UInt64 lid)
	{
		//Dim branchStates As Dictionary(Of String, clsBranchState)
		((Dictionary<string, clsBranchState>)iq.sesh(lid, "branchStates")).Clear();
		//branchStates.Clear()

	}

	/// <summary>Displays stock and price an (arbitrary, flat, comma delimited) list of parts -  creates a branch and set of child branches with a neagtive IDs (which can be rendered as a matrix) </summary>
	public List<string> ShowProducts(UInt64 lid, l, ref int branchid, float treewidth, ref List<string> errormessages)
	{

		List<string> errs = new List<string>();

		clsTranslation part = iq.AddTranslation("Part", English, "UI", 0, null, 0, false);
		clsTranslation parts = iq.AddTranslation("Parts", English, "UI", 0, null, 0, false);
		int mbid;
		//minumium branch ID (Some negative number)

		//lowest (most negative) - we create a slew of temproary negative branches - starting with a placeholder one, and then a bunch of children
		if (iq.SeshContains(lid, "swiftStart")) {
			TidySwiftBranches(lid);
			//removes any temporary swift branches we've created before
		}

		object J = from q in iq.Branches.Keys;
		//Use LINQ to find the lowest (most negative!) branch ID (this is OK for multiple uses as the brahes *are* enetered into the golbal collection - even though ther'ye never inserted into the database)
		mbid = J.Min();
		if (mbid > 0)
			mbid = 0;
		mbid -= 1;
		//start at one *less* than the current min
		iq.sesh(lid, "swiftStart") = mbid;


		//create a floating branch (to hook all the products to)... it's not persisted (no insert is made to the database) because we use the constructor (sub new) with an ID parameter
		clsBranch HeaderBranch = new clsBranch(mbid, null, null, iq.AddTranslation("Requested parts", English, "UI", 0, null, 0, false), "", parts, part, iq.Screens(719), 100, false,
		false, "B");

		//create some branch info - so we can render it
		clsBranchInfo bi = new clsBranchInfo(lid, "tree." + mbid, null, treewidth, enumParadigm.configuringSystem, errormessages);

		branchid = mbid;
		//This is passed back byref and is the parent branch of all the items in the list

		//l$ = Replace(l$, vbLf, "")

		clsBranch abranch;
		foreach ( partno in Split(l, ";")) {
			if (iq.i_SKU.ContainsKey(partno)) {
				mbid -= 1;
				//Decrement - to create descending (negative) branch ID's
				abranch = new clsBranch(mbid, iq.i_SKU(partno), HeaderBranch, iq.AddTranslation(partno, English, "UI", 0, null, 0, false), "", parts, part, null, 100, false,
				false, "G");
			//  setBranchState(lid, "tree." & branchid & "." & mbid.ToString, oc.open, bt.gridrow, False)
			} else {
				errs.Add(partno + " is not in iQuote (invalid part number ?)");
			}
		}

		iq.sesh(lid, "swiftEnd") = mbid;
		//store the last (most negative) branch we created so we can clean up later

		return errs;

	}
	private string getAdverts(UInt64 lid, string endpath)
	{
		Dictionary<int, clsAdvert> adverts = new Dictionary<int, clsAdvert>();
		clsQuote quote = null;
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
		foreach (clsAdvert advert in from a in iq.Adverts.Valueswhere a.Visible == true && (a.Manufacturer == Manufacturer.Unknown || a.Manufacturer == agentAccount.Manufacturer)) {
			if (advert.Campaign.StartDate.Date <= Today.Date & advert.Campaign.EndDate.Date >= Today.Date) {
				// If advert.Campaign.Seller Is agentAccount.SellerChannel Then
				if (advert.AdRegionPresent.Encompasses(agentAccount.SellerChannel.Region)) {
					if (advert.AdRegionAbsent == null | (advert.AdRegionAbsent != null && !advert.AdRegionAbsent.Encompasses(agentAccount.SellerChannel.Region))) {
						if (quote == null | (enumParadigm)iq.sesh(lid, "Paradigm") == enumParadigm.AddingSystem) {
							if (advert.ImageWide == false) {
								adverts.Add(advert.ID, advert);
							}
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
							bool found = false;
							if (advert.SlotTypeCode != null) {
								if (iq.i_slotType_Code.ContainsKey(advert.SlotTypeCode)) {
									foreach (clsSlotType slotType in iq.i_slotType_Code(advert.SlotTypeCode).Values.ToArray()) {
										List<clsQuoteItem> systemItems;
										systemItems = quote.RootItem.findSystemItems;
										foreach ( itm in systemItems) {
											Dictionary<clsSlotType, clsSlotSummary> dicslots = new Dictionary<clsSlotType, clsSlotSummary>();

											itm.ValidateSlots2(dicslots, true);
											//Recursive ! - compiles (and uses internally the quotes dicslots) =- Gives
											itm.ValidateSlots2(dicslots, false);
											//Now for takes, to fill fallbacks
											itm.dicslots = dicslots;

											object l = (from n in itm.dicslots.Keyswhere n.MajorCode == slotType.MajorCode).ToList();
											if (l != null & l.Count > 0) {
												foreach ( s in l) {
													object iloCont = itm.dicslots(s);
													if (iloCont.Given > 0) {
														if (iloCont.taken == 0) {
															addAdvert = true;
														} else {
															addAdvert = false;
															found = true;
															break; // TODO: might not be correct. Was : Exit For
														}
													}
												}
											} else {
												addAdvert = false;
											}
										}
										if (found)
											break; // TODO: might not be correct. Was : Exit For
									}
								}
							}
							if (addAdvert) {
								adverts.Add(advert.ID, advert);
							}
						}
					}

				}
				// End If
			}
		}


		Random randomClass = new Random();
		int rndNumber;
		HashSet<int> RememberSet = new HashSet<int>();
		int adNumber = 2;
		bool imageWide = true;

		if (endpath.Trim.ToLower == "tree.1") {
			adNumber = 3;
			imageWide = false;
			adLiteral = "<div id=\"bannerAd\" class =\"bannerdivright\">";
		} else {
			adLiteral = "<div id=\"bannerAd\" class =\"bannerdivtop\">";
		}
		if (adNumber > adverts.Values.Count) {
			adNumber = adverts.Values.Count;
		}
		//   Dim selectedAdverts = From a In adverts.Values Where a.ImageWide = imageWide
		while (RememberSet.Count < adNumber && adverts.Values.Count > 0) {
			rndNumber = randomClass.Next(0, adverts.Values.Count);
			if (RememberSet.Add(rndNumber)) {
				clsAdvert selectedAdvert = adverts.Values(rndNumber);

				if (selectedAdvert != null) {
					clsImpression advertImpressions = new clsImpression(agentAccount, selectedAdvert, Now);
					string urlString = selectedAdvert.ImageUrl;
					string navigateURL = selectedAdvert.URL;
					if (quote != null && quote.Cursor != null && quote.Cursor.Branch != null) {
						clsQuoteItem sysitem = quote.Cursor;
						if (selectedAdvert.URL.Contains("EMULEX")) {
							object x = sysitem.Branch.findAllProductPathsByAttributeValueRecursive(sysitem.Path, "optType", "PCI*", true, agentAccount);
							navigateURL = navigateURL + "|" + sysitem.Path + "|" + x.FirstOrDefault;
						} else if (selectedAdvert.URL.Contains("ROK")) {
							object x = sysitem.Branch.findAllProductPathsByAttributeValueRecursive(sysitem.Path, "optType", "SOF1", true, agentAccount);
							navigateURL = navigateURL + "|" + sysitem.Path + "|" + x.FirstOrDefault;
						}
					}
					if (string.IsNullOrEmpty(urlString) == false) {
						adLiteral += "<a onclick=\"clickthru(" + selectedAdvert.ID + ",'" + navigateURL + "');\"><img class = \"" + IIf(imageWide, "bannerimgtop", "bannerimg").ToString() + "\"  alt=\"\" src=\"" + urlString + "\"   /></a>";
					}

				}
			}

		}
		adLiteral = adLiteral + "</div>";
		if (RememberSet.Count == 0 | (endpath.Trim.ToLower == "tree.1" & quote != null)) {
			adLiteral = "";
		}

		return adLiteral;
	}


}
