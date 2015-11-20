

public class manipulation : clsPageLogging
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//this page is typically execute by iQuote2.js

		//   rexec("manipulation.aspx?command=graft&source=" + copySourceBranchID + "target=" + targetBranchID);

		Exception exception = null;

		clsBranch targetBranch = null;
		if (Request("TargetBranch") != null)
			targetBranch = iq.Branches(Request("TargetBranch"));
		clsBranch sourcebranch = null;
		if (Request("SourceBranch") != null)
			sourcebranch = iq.Branches(Request("SourceBranch"));
		string targetPath = string.Empty;
		if (Request("TargetPath") != null)
			targetPath = Request("targetPath");
		string sourcePath = string.Empty;
		if (Request("SourcePath") != null)
			sourcePath = Request("sourcePath");

		UInt64 lid = Request.QueryString("lid");

		if (iq.sesh(lid, "UserID") == null){Response.Redirect("Loading.aspx", false);return;
}

		string username = iq.UserAccountName(iq.sesh(lid, "UserID"), iq.sesh(lid, "AgentAccount").id);
		clsAccount agentaccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		clsQuote currentQuote = null;

		if (iq.SeshContains(lid, "QuoteID")) {
			if (iq.sesh(lid, "QuoteID") != null) {
				int qid = iq.sesh(lid, "QuoteID");
				currentQuote = agentaccount.Quotes(qid);
			}
		}


		try {
			switch (Request("command")) {
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"graft":

					string errormessage = targetBranch.Graft(sourcebranch, username, "", errormessages);
					//Creates the new graft

					//we must delete the cached dataview - otherwise we won't see the change
					wipeCachedDataView(targetPath, lid);

					//if the graft fails, we put an error in the response which the JS will place into the tree

					Panel1.Controls.Add(ErrorDymo(errormessage, lid));
				//Case Is = "adopt" '(reparent - many branches )

				//    wipeCachedDataView(targetPath, lid)
				//    Dim newParent As clsBranch = iq.Branches(Split(targetPath, ".").Last)

				//    'the JS compiles a list of sources from the checked branches (their paths)
				//    For Each s In Request("sources").Split(",")
				//        If s <> "" Then 'the JS untidily leaves an extra comma - but it's easier to deal with here 
				//            sourcebranch = iq.Branches(s.Split(".").Last)
				//            sourcebranch.Parent = newParent
				//            sourcebranch.Update(errorMessages)
				//        End If
				//    Next

				//    'slots and quantities (on descendants) will need re pathing
				//    'any grafts and prunes in force will need manipulating
				//    'some quoteitems paths may be invalidated


				case  // ERROR: Case labels with binary operators are unsupported : Equality
"clone":

					wipeCachedDataView(oneAbove(sourcePath), lid);
					//we need to clear the PARENTs dataview

					int bid = Split(sourcePath, ".").Last;

					clsBranch aBranch = iq.Branches(bid).clone(sourcePath, errorMessages);
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"having":
				//just a stub.. allows the branch to be displayed open - with the Ex buttons

				case  // ERROR: Case labels with binary operators are unsupported : Equality
"exclude":

					//rExec('manipulation.aspx?command=exclusivity&bid=' + bid + '&val=' + tb.value, nullFunc);
					object v = DBNull.Value;
					clsBranch branch = null;
					//Having and Excludes (request variables) are complete paths - we're only interested in the final branch
					int hvg = (int)Split(sourcePath, ".").Last;
					int exc = (int)Split(targetPath, ".").Last;
					if (iq.Branches.ContainsKey(hvg)) {
						clsExclude ex = new clsExclude(iq.Branches(hvg), iq.Branches(exc), "No reason specified");

					}
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"prune":

					wipeCachedDataView(sourcePath, lid);

					iq.Prune(sourcePath, username);
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"retract":

					wipeCachedDataView(oneAbove(sourcePath), lid);
					//we need to clear the PARENTs dataview

					iq.Retract(iq.Branches(Split(sourcePath, ".").Last), username, errorMessages);
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"cursor":

					iq.sesh(lid, "treeCursor") = sourcePath;
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"createNextVersion":

					clsQuote Quote__1;
					clsQuote RevisedQuote;

					Quote__1 = agentaccount.Quotes(Request("QuoteID"));
					RevisedQuote = Quote__1.CreateNextVersion(errorMessages);

					iq.sesh(lid, "QuoteID") = RevisedQuote.ID;

					iq.sesh(lid, "quoteCursor") = RevisedQuote.RootItem.Children(0).ID;
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"startQuote":
				case "startquote":

					DiscardUnChangedQuote(lid);
					iq.sesh(lid, "branchStates").clear();
					//wipetreestate


					int bid;
					if (Request("buyerid") == "") {
						bid = iq.sesh(lid, "AgentAccount");
					} else {
						bid = Request("buyerid");
					}

					//           If Not iq.Accounts(bid).SellerChannel Is agentaccount.sellerchannel Then

					//this buyer does not have an account with this seller (yet).. so make one
					//we'll need a priceBand (amongst other things)


					//          End If


					startQuote(bid, lid);
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"adminON":
					iq.sesh(lid, "admin") = true;
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"adminOFF":

					iq.sesh(lid, "admin") = false;
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"CreateChannel":

					clsChannel sellerChannel = agentaccount.SellerChannel;
					clsChannel achanel = new clsChannel(sellerChannel, "New Company", "Holding company", "", "NEW1", sellerChannel.Region, new nullableString(), new nullableString(), new nullableString(), 15,
					"tree.1", "", 0, 0, "R", "", "", iq.i_currency_code("GBP"), sellerChannel.Universal, sellerChannel.orderEmail,

					"", "");

				case  // ERROR: Case labels with binary operators are unsupported : Equality
"CreateSiblingAccount":
					//Creates a new user - and an account for them them
					// we may need to migrate the accounts to
					clsAccount acToCopy = iq.Accounts(Request("AccID"));
					// this is 
					clsUser NewUser = new clsUser(acToCopy.BuyerChannel, DomainPart(acToCopy.User.Email), "", acToCopy.User.tel1, acToCopy.User.tel2);
						//does the insert and sets the ID

					 // ERROR: Not supported in C#: WithStatement


				case  // ERROR: Case labels with binary operators are unsupported : Equality
"delNote":
					clsQuoteItem qi = currentQuote.RootItem.FindRecursive(Request("qiid"));
					qi.Note = new nullableString();

					qi.Update();
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"addNote":


					clsQuoteItem qi = currentQuote.RootItem.FindRecursive(Request("qiid"));
					qi.Note = new nullableString("Your note");

					qi.Update();
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"saveNote":

					int qiid = Val(Mid(Request("qiid"), 5));
					//The qiid parameter is now the element name - with is the QuoteItemID prefixed by 'note'  - e.g. note3458830
					clsQuoteItem qi = currentQuote.RootItem.FindRecursive(qiid);
					//If the remove an item to which they just added a note - we wont be able to locate/save it
					if (qi != null) {
						qi.Note = new nullableString(Request("text"));
						qi.Update();

					}
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"CopyQuote":

					clsQuote quote__2 = agentaccount.Quotes(Request("QID"));

					object newQuote = quote__2.Copy(null, 0, errorMessages);
					iq.sesh(lid, "quoteCursor") = newQuote.RootItem.Children(0).ID;

					iq.sesh(lid, "QuoteID") = newQuote.ID;
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"MarkAsWon":

					clsQuote quote__2 = agentaccount.Quotes(Request("QID"));
					if (quote__2.PassesValidation(lid))
						errorMessages = quote__2.MarkAsWon(lid);
					else

						Panel1.Controls.Add(NewLit("[FV]"));
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"DiscardQuote":

					clsQuote quote__2 = agentaccount.Quotes(Request("QID"));
					quote__2.State = iq.i_state_GroupCode("QT-#CX");
					//mark as closed

					quote__2.Update();
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"ExpandQuoteVersions":

					if (!iq.SeshContains(lid, "expandedQuotes")) {
						iq.sesh(lid, "expandedQuotes") = new List<int>();
					}

					//this should be redundant 
					if (agentaccount.Quotes.ContainsKey(Request("RQID"))) {

						List<int> expandedQuotes = iq.sesh(lid, "expandedQuotes");
						// a list of the root quotes which are expanded
						if (!expandedQuotes.Contains((int)Request("RQID"))) {
							expandedQuotes.Add((int)Request("RQID"));
						}

					}

				case  // ERROR: Case labels with binary operators are unsupported : Equality
"CollapseQuoteVersions":
					//The agent account has had all its quotes loaded - we add the quote form there
					iq.sesh(lid, "expandedQuotes").@remove(Request("RQID"));

				case  // ERROR: Case labels with binary operators are unsupported : Equality
"focus":
					//focus can be set to a comma seperated value (which a *set* of 'focuses' - eg. Receta+Budget) (or whatever) - ProductVisible checks Focus (if it's set)

					if (Request("value") == "") {
						iq.sesh(lid, "focus") = new List<string>();
						//Spliting and empty string into a list products a single empty value (ie a list with one entry) - so we have a special case
					} else {
						iq.sesh(lid, "focus") = Split(Request("value"), ",").ToList;
						//A LIST(of Strings) goes into the session variable

					}
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"quoteNameChange":

					clsQuote quote__2 = agentaccount.Quotes(Request("QID"));
					quote__2.Name = new nullableString(Regex.Replace(Request("quoteName"), "<[^>]+>", ""));

					quote__2.Update();
				default:
					Debug.Print(Request("Command"));

					System.Diagnostics.Debugger.Break();
			}
			Panel1.Controls.Add(NewLit("<p>" + string.Join(Environment.NewLine, errormessages) + "</p>"));
		} catch (Exception ex) {
			ErrorLog.Add(ex);
			exception = ex;
		}
		//Audit Trail
		AuditLog.Instance.Add(lid, Request("command").ToString(), sourcePath == null ? sourcebranch == null ? string.Empty : sourcebranch.ID : sourcePath, targetPath == null ? targetBranch != null ? targetBranch.ID : null : targetPath, errorMessages, exception, "", "", 0, Context.Request.HttpMethod,
		Context.Request.UrlReferrer.AbsoluteUri);

	}



	private string DomainPart(email)
	{

		int at;
		at = InStr(email, "@");
		if (at)
			return Mid(email, at);
		else
			return "";

	}



	public void startQuote(int BuyerID, UInt64 lid)
	{
		if (lid == 0)
			System.Diagnostics.Debugger.Break();

		if (iq.SeshContains(lid, "QuoteID")) {
			//save any changes to the quote in progress
			clsQuote inprogress;
			inprogress = ((clsAccount)iq.sesh(lid, "AgentAccount")).Quotes(iq.sesh(lid, "QuoteID"));
			inprogress.Update();
		}

		iq.sesh(lid, "BuyerAccount") = BuyerID;

		//start a new quote 
		clsQuote aQuote;
		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");



		//Dim l$ = UpdatePrices(buyeraccount)  'cache' (grab the latest prices from IQ2

		NullablePrice nullprice;
		//The quote will start life with an unknown price
		nullprice = new NullablePrice(buyerAccount.Currency);
		aQuote = new clsQuote(buyerAccount, agentAccount, null, Now, Now, 1, iq.i_state_GroupCode("QT-#NW"), nullprice, buyerAccount.Currency, false,
		false, false, false, new nullableString(), new nullableString(), 0);


		iq.sesh(lid, "QuoteID") = aQuote.ID;

		//populate the customer name, display quote #

		//txtBuyer.Text = buyeraccount.displayname(s_lang)
		//txtBuyer.Enabled = False ' lock it !
		//LblBuyer.Text = buyeraccount.displayname(s_lang)

		//reveal the Product tree (if they have a quote on the go)
		//Response.Write("<script display('treeHolder','inline')></script>;")
		// PnlProductTree.CssClass = "visible"
	}

}
