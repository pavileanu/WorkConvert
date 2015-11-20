//Option Strict On
using System.Linq;
using System.Reflection;

public class suggest : clsPageLogging
{

	public int searchID;

	public bool abandon = false;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//If iq.sesh(lid,"AgentAccount") Is Nothing Then
		// Response.StatusCode = 401 : Response.End() 'return a '401' which will be detected by the ajax and cause a redirect to signin.aspx
		// Else

		//NB: This is used by the keyword search and from the editor
		UInt64 lid;

		try {
			lid = Request.QueryString("lid");
		//Validate that the request query string is not deemed as dangerous (Javascript injection etc), if so return nothing back
		} catch (System.Web.HttpRequestValidationException ex) {
			return;
		}

		//if a user triggers many searches (by for example.. typing!) - we should abandon any in progress


		iq.nextSearchID += 1;
		searchID = iq.nextSearchID;

		//if the session variable searchID ever diverges from the local variable searchID -
		// this (same) user has started another search - and this search can be abandoned.
		iq.sesh(lid, "searchID") = searchID;



		clsAccount buyerAccount;
		clsAccount agentAccount;
		buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		clsLanguage language;
		if (Request.QueryString("lid") == null) {
			language = English;
		} else {
			lid = Request.QueryString("lid");
			language = agentAccount.Language;
		}

		object ru;
		ru = Request.RawUrl;

		int fl;
		string frag;
		Literal lit = new Literal();
		frag = LCase(Request("frag"));
		//the frag is the contents of the seach textbox - the context of a keyword search it may be several words (or comma seperated phrases)
		fl = frag.Length;
		lit = new Literal();
		lit.Text = "!Begin";

		Form.Controls.Add(lit);

		//You can pas a "path" e.g. Path=channel(1).CustomerAccounts
		//OR 
		//a root level dictioanry and (optional) filter e.g. dic=states(group=TH)

		object path = Request("path");
		//This is an object model path - for the editor - NOT a searchPath

		object searchpath = Request("searchPath");
		//keyword search path

		string qs = Request.RawUrl;

		object filter;

		object obj = iq;
		object dic = null;
		if (path != "") {
			filter = ParsePath(path, obj, dic, errorMessages);
		//DicSearch(LCase(frag), dic, language, errorMessages, filter)


		} else {
			if (Request("dic") == "keywords") {
				if (frag != "") {
					Form.Controls.Add(KeywordSearch(agentAccount, buyerAccount, LCase(frag), Request("searchType"), searchpath, errorMessages));
				}

			//the account search (from the opening quote screen - has very different presentation and additional functionality (for creating new Channels and users)
			} else if (Request("dic") == "accounts") {
				//AccountSearch(LCase(frag), agentAccount.SellerChannel.CustomerAccounts, agentAccount.Language)
				AccountSearch(LCase(frag), iq.Accounts, agentAccount.Language);
				//we need to search the GLOBAL list of accoutns
			} else if (Request("dic") == "translation") {
				TranslationSearch(LCase(frag), agentAccount.Language);


			} else {
				//var url = "suggest.aspx?valueBox=&" + valueboxID + "&textBoxID=" + textBoxID + "&frag=" + textBox.value + "&divID=" + divID + "&dic=" + dicName 

				//Search a root dictionary - with an optional filter
				//find the root level dictionary this field looks up values in

				//this should be consolidated/replaced with the above

				object lu = Request("dic");
				filter = GetParenthesisValue(Request("dic"));
				int op = InStr(lu, "(");
				if (op > 0) {
					dic = Reflection.WalkPropertyValue(iq, Left(lu, op - 1), errorMessages);
					//could go pack to getpropertyvalue for a slight speedup
				} else {
					dic = Reflection.WalkPropertyValue(iq, lu, errorMessages);
				}

				Literal myLit = null;
				object div;
				string valuebox = Request("ValueBoxID");
				string textBoxID = Request("textBoxID");
				string divid = Request("divid");
				div = "<div class=|dropDownRow| onclick=|document.getElementById('" + valuebox + "').value='null';document.getElementById('" + textBoxID + "').value='None';display('" + divid + "','none');|>";
				div += "None('null')";
				div += "</div>";
				myLit = new Literal();
				myLit.Text = Replace(div, "|", Chr(34));
				Form.Controls.Add(myLit);

				foreach ( kvp in SearchResults(LCase(frag), dic, language, errorMessages, filter, 1000)) {
					//                                                                     \/ this is an ID (of an object)
					div = "<div class=|dropDownRow| onclick=|document.getElementById('" + valuebox + "').value=" + kvp.Key + ";document.getElementById('" + textBoxID + "').value='" + kvp.Value + "';display('" + divid + "','none');|>";
					div += kvp.Value + "(" + kvp.Key + ")";
					div += "</div>";

					myLit = new Literal();
					myLit.Text = Replace(div, "|", Chr(34));
					Form.Controls.Add(myLit);

				}
			}
		}

		if (errorMessages.Count > 0) {
			OutputErrors(Form.Controls, errorMessages, lid);
		}


		lit = new Literal();
		lit.Text = "!End";
		Form.Controls.Add(lit);

	}


	public void AccountSearch(frag, Dictionary<int, clsAccount> dicAccounts, clsLanguage Language)
	{
		UInt64 lid = (UInt64)Request.QueryString("lid");

		clsChannel sellerChannel = ((clsAccount)iq.sesh(lid, "AgentAccount")).SellerChannel;

		//we uses leading spaces plus the fragment to find words beggining with the frag
		object m = from a in dicAccounts.Valueswhere (LCase(" " + a.BuyerChannel.DisplayName(Language)).Contains(" " + frag) | LCase(" " + a.Priceband.text).Contains(" " + frag) | LCase(" " + a.User.RealName).Contains(" " + frag))orderby a.SellerChannel.ID == sellerChannel.ID descending, a.BuyerChannel.ID, a.User.ID;

		//Dim m = From a In dicAccounts.Values Where LCase(a.User.RealName.Contains(frag)) Order By a.displayName(Language) ' Take 15  '  Downt work as expected -> Or a.User.RealName Like "*" & frag & "*"

		int count = 0;
		clsAccount OA = null;

		//we will iterate all the matching accounts outputting the first account for each buyer company - which will be wi
		//note:- accounts link a buyer channel to a sellerchannel and can be considred as belonging to a (buying) user
		//a single buyng company may therefore have many accounts with a selling channel (one for each buyer) - they will all have the same priceBand
		bool nextUser = false;
		bool nextCompany__1 = false;

		Literal lit;

		//any account that is with the current seller will apear first
		foreach (clsAccount Ac in m) {

			nextUser = false;
			nextCompany__1 = false;
			if (OA == null) {
				nextUser = true;
				nextCompany__1 = true;
			} else {
				if (!object.ReferenceEquals(Ac.User, OA.User))
					nextUser = true;
				if (!object.ReferenceEquals(Ac.BuyerChannel, OA.BuyerChannel))
					nextcompany = true;
			}


			if (nextUser) {
				//If nextCompany Then
				//    If Not OA Is Nothing Then
				//        lit = New Literal
				//        lit.Text = "-" & OA.ID & "^--------New contact" & "]"  'We return NEGATIVE the ID of the last account in the PREVIOUS company - to create a sibiling account of
				//        Form.Controls.Add(lit)
				//    End If
				//End If

				if (nextCompany__1) {
					lit = new Literal();
					//We return NEGATIVE the ID of the first account in the company - to create a sibiling account of it
					lit.Text = -Ac.ID + "^" + Ac.BuyerChannel.DisplayName(Language) + " - " + Ac.BuyerChannel.Address + "- New contact]";
					Form.Controls.Add(lit);
				}

				lit = new Literal();
				lit.Text = Ac.ID + "^--------" + Ac.User.RealName + " - " + Ac.Priceband.text + "]";
				Form.Controls.Add(lit);

			}

			OA = Ac;

			count += 1;
			if (count == 50) {
				lit = new Literal();
				lit.Text = Xlt("0^This list is incomplete - please type more of the contact or company name to narrow the results]", Language);
				//Return -1 for the 'new company' option
				Form.Controls.Add(lit);

				break; // TODO: might not be correct. Was : Exit For
			}

		}

		lit = new Literal();
		lit.Text = Xlt("-1^New Company]", Language);
		//Return -1 for the 'new company' option
		Form.Controls.Add(lit);


	}

	public void TranslationSearch(frag, clsLanguage Language)
	{
		UInt64 lid = (UInt64)Request.QueryString("lid");

		clsChannel sellerChannel = ((clsAccount)iq.sesh(lid, "AgentAccount")).SellerChannel;


		//  Dim translationResults As clsTranslation
		//we uses leading spaces plus the fragment to find words beggining with the frag
		object m = from a in iq.Translations.Valueswhere (LCase(" " + a.textTranslation(Language)).Contains(frag))orderby a.Order;

		//Dim m = From a In dicAccounts.Values Where LCase(a.User.RealName.Contains(frag)) Order By a.displayName(Language) ' Take 15  '  Downt work as expected -> Or a.User.RealName Like "*" & frag & "*"

		clsAccount OA = null;

		//we will iterate all the matching accounts outputting the first account for each buyer company - which will be wi
		//note:- accounts link a buyer channel to a sellerchannel and can be considred as belonging to a (buying) user
		//a single buyng company may therefore have many accounts with a selling channel (one for each buyer) - they will all have the same priceBand
		bool nextUser = false;
		bool nextCompany = false;

		Literal lit;


		Literal myLit = null;
		object div;
		string valuebox = Request("ValueBoxID");
		string textBoxID = Request("textBoxID");
		string divid = Request("divid");

		foreach (clsTranslation tr in m) {
			div = "<div class=|dropDownRow| onclick=|document.getElementById('" + valuebox + "').value=" + tr.Key + ";document.getElementById('" + textBoxID + "').value='" + tr.textTranslation(Language) + "';display('" + divid + "','none');|>";
			div += tr.textTranslation(Language) + "(" + tr.Key + ")";
			div += "</div>";
			lit = new Literal();
			lit.Text = Replace(div, "|", Chr(34));
			Form.Controls.Add(lit);

			count += 1;
			if (count == 50) {
				lit = new Literal();
				lit.Text = Xlt("0^This list is incomplete - please type more of the contact or company name to narrow the results]", Language);
				//Return -1 for the 'new company' option
				Form.Controls.Add(lit);

				break; // TODO: might not be correct. Was : Exit For
			}

		}

		//lit = New Literal
		//'lit.Text = Xlt("-1^New Company]", Language)  'Return -1 for the 'new company' option
		//Form.Controls.Add(lit)


	}
	public Dictionary<int, string> SearchResults(frag, object dic, clsLanguage language, ref List<string> errorMessages, filter = "", Int32 maxResults = 10)
	{

		//A case insensitive leftmost search on the specified dictionaries values 'displayname'
		//with an optional simple filter on some Property=Value  e.g. code=threads
		//used by AutoSuggest (in the editor amongtsh other places)

		//returns a list of ^ delimited ID^Text^Color strings

		SearchResults = new Dictionary<int, string>();

		int matches;
		string vn;
		Literal Lit = null;

		Dictionary<string, object> rdic;
		//results dictionary (of matching entities)
		rdic = new Dictionary<string, object>();

		int fl = Len(frag);
		string txt;

		string prop = "";
		//This property of the object we're about to add to the DDL . . .
		string mask = "";
		//must match this (literal string)
		if (filter != "") {
			string[] pv = Split(filter, "=");
			prop = pv(0);
			mask = pv(1);
		}

		bool showall;
		if (Request("showAll") != null) {
			if (Request("showAll") == true)
				showall = true;
			//used when the box is initially displayed (with a selected value)
		}

		bool filterOK;

		//could easily be converted to use LINQ for a possible speedup
		foreach ( v in dic.values) {
			//If dic Is iq.States Then colour = v.colour Else colour = "#ffffffff"

			txt = v.displayname(language);
			vn = LCase(Left(txt, fl));
			if (vn == frag | showall == true) {
				//check them against the filter here - (using reflection)
				if (filter == "") {
					filterOK = true;
				} else {
					filterOK = (Reflection.WalkPropertyValue(v, prop, errorMessages) == mask);
				}

				if (filterOK) {
					SearchResults.Add(v.ID, txt);

					matches += 1;
					if (matches > maxResults)
						break; // TODO: might not be correct. Was : Exit For
				}
			}
		}

		//Lit = New Literal
		//Lit.Text = "!End"
		//Form.Controls.Add(Lit)

	}

	public Panel KeywordSearch(clsAccount agentAccount, clsAccount buyerAccount, string searchText, string searchType, path, ref List<string> errorMessages)
	{

		//Searchtype is 'add','priced', or 'Stocked' - as determined by the radio buttons in the front end
		//searchScope is 'global' or 'local' - ie. from the RootBranch - Or from the TreeCursor

		UInt64 lid = Request.QueryString("lid");
		//TODO - by keyword searching every graft first - we could know which sets of branches have which scores

		//find our start point in the tree (the branch represented by the current treecursor)
		//This code was desigent to work from any 'entry point' in the tree - in practise it's used either from the root branch for a 'global ' search, or from a system - for an options search

		clsBranch startBranch;

		bool crossSystems = false;
		//whether to 'cross systems' when recursing (i.e. is ths an options search ?)
		bool isDiagView = AccountHasRight(lid, "DIAGVIEW");
		if (path == "") {
			startBranch = iq.RootBranch;
			path = iq.sesh(lid, "Root");
			crossSystems = false;
			if (isDiagView)
				crossSystems = true;
			//allow a deep search through systems for diagview
		} else {
			object seg = Split(path, ".");
			startBranch = iq.Branches((int)seg(UBound(seg)));
			crossSystems = true;
		}


		if (Len(searchText) > 2 & searchText != "enter a sku to find") {
			double t;
			int et;
			t = Stopwatch.GetTimestamp;

			//Fetch a score for every branch - a total number of matches and bitwise flags for the fragments matched.

			//Dim p As clsProduct

			//10% of the time
			Dictionary<int, clsKwScore> branchScores = ScoreBranches(Fragments(searchText), agentAccount.Language);

			Dictionary<string, int> PathScore;
			PathScore = new Dictionary<string, int>();
			//paths>scores

			//recurse every path 

			PathScore.Clear();
			int numsegs;
			Array segs = segments(path, numsegs);
			//records the segments of the path to each matching point
			//Important we start at a depth of numsegs (prepared segments - from the treecursor path) - so as to construct complete and valid paths


			if (!abandon) {
				object pth = "";
				//cacehing the score/matched bits of a branch (and it's descendats) would improve the speed of options search (but do nothign for a systems search)
				startBranch.Score(branchScores, segs, 0, 0, 0, 0, 0, numsegs - 1, PathScore, buyerAccount,
				searchType, crossSystems, lid, searchID, abandon, 0, pth);
				//<< THIS is where all the hard work happens (Deeply recursive)

			}

			et = (Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000;
			if (abandon) {
				Panel apanel = new Panel();


				apanel.Controls.Add(NewLit("<p>Abandoned search at " + et + " milliseconds</p>"));
				return apanel;
			} else {
				return outputResults(lid, PathScore, searchType, buyerAccount, agentAccount.Language, errorMessages, crossSystems, isDiagView, et);
			}


		} else {
			return new Panel();
		}

	}

	private Panel outputResults(UInt64 lid, Dictionary<string, int> results, string searchtype, clsAccount buyeraccount, clsLanguage language, ref List<string> errorMessages, bool isoptionsSearch, bool isDiagView, int et)
	{

		outputResults = new Panel();
		outputResults.ID = "KWresultSet";
		outputResults.CssClass = "KWresultSet";

		List<clsVariant> NeedUpdate = null;

		//customer specific (webservice) pricing
		if ((bool)buyeraccount.SellerChannel.priceConfig & 8) {
			NeedUpdate = new List<clsVariant>();
			//Dictionary(Of String, ClsProductVariant)
		}

		outputResults.Controls.Add(NewLit("<p>" + et + " milliseconds</p>"));


		//We have to go through all the results becuase we don't know what visible yet - the search is conducted on *all* branches/products
		//(except we dont recurse into systems not in the feed)

		// Dim topscores As Dictionary(Of String, Integer)
		object topscores = from r in resultswhere r.Value > 0orderby r.Value descending;

		int maxValue = topscores(0).Value;

		int maxOccurs = topscoreswhere v.Value == maxValueCount();
		// Link query to find how many time the max score occurs if it is one then its a sku
		Literal lit;

		//the keys are the paths, the values are the scores
		clsBranch branch;
		Panel resultRow;

		int output = 0;
		object cc = topscores.Count;

		int maxresults = 25;
		if (isDiagView)
			maxresults = 500;


		string rfh;
		foreach ( kvp in topscores) {
			//populate branch by surfing down the path held in the key
			branch = iq.Branches(Split(kvp.Key, ".").Last);

			rfh = "";
			//Readons For Hide !

			bool include = false;
			//Default to false (don't display)
			bool greyed = false;


			//GetSkudDescendants - with 'includeself' will gives us a set of hidereaons for a given branch/product
			clsBranchInfo BI = new clsBranchInfo(lid, "tree" + kvp.Key);
			Dictionary<clsBranch, clsVisibility> sd;
			// = BI.visibleChildren(errorMessages, True, 0, 0, False, False)
			sd = new Dictionary<clsBranch, clsVisibility>();
			//AM I Visible - first param is IncludeSelf
			BI.showAll = true;
			//We WANT all branches - with any of their resonsfor hide returned
			sd = branch.getSKUdDescendants(true, BI.path, BI, false, 1, 0, errorMessages);

			if (sd.Any)
				rfh = Join(sd.First.Value.hideReasonList.ToArray, ",");

			//if the search type is 'priced' then we only show products if there is no resont to hide them
			//Only show things that are in the feed file (although we searched everything)
			if (searchtype == "priced" & rfh == "") {
				include = true;
			//ElseIf searchtype = "stocked" Then ' loosley equivilent to 'feed only'

			} else if (searchtype == "all") {
				//if the search type is 'all' we show the product regardless of 'reasons for hide'
				include = true;
			}

			object price = "";
			object stock = "";

			int nv = -1;

			// include Then
			if (include) {

				resultRow = new Panel();
				resultRow.CssClass = "KWresultRow";
				outputResults.Controls.Add(resultRow);
				Form.Controls.Add(resultRow);
				output += 1;

				//these are reasons to hide this result (out of region, not in feed etc, EOL etc.)
				if (rfh != "") {
					Literal tt = new Literal();
					tt.Text = "<span title='" + rfh + "'>*</span>";
					resultRow.Controls.Add(tt);
					greyed = true;
				} else {
					greyed = false;
				}

				Panel segs = KWbreadcrumbs(lid, "tree" + kvp.Key, language, isoptionsSearch, greyed, rfh, isDiagView);
				//branch.keywords(language)
				resultRow.Controls.Add(segs);

				if (branch.HasSKU) {
					if (NeedUpdate != null)
						NeedUpdate.AddRange(branch.StalePrices(buyeraccount, errorMessages));
					//returns a set of variants
					resultRow.Controls.Add(branch.BuyUI(buyeraccount, "tree" + kvp.Key, lid, true));
				}

				//SCORE
				//segs.Controls.Add(NewLit("&nbsp;<b>" & kvp.Value & "</b>"))

				//If Not (searchtype = "stocked" And nv < 0) Then
				// lit = New Literal
				// lit.Text = "tree" & kvp.Key & "^" & kvp.Value & PathName$("tree" & kvp.Key, language) & sku$ & " " & price$ & " " & stock & "]"
				// Form.Controls.Add(lit)
				//End If
			}
			// If maxOccurs = 1 Then Exit For  WTF ???? WASTED AN HOUR (or two!)
			if (output > maxresults)
				break; // TODO: might not be correct. Was : Exit For

		}


		Panel footer = new Panel();
		Form.Controls.Add(footer);

		if (output == 0) {
			lit = new Literal();
			lit.Text = "<p class='KWNoResults'>" + Xlt("No matching results", language) + "</p>";
			footer.Controls.Add(lit);

		} else if (output > maxresults) {
			lit = new Literal();
			lit.Text = "<p class='KWNoResults'>" + Xlt("There are more results, please add keywords to refine your search", language) + "</p>";
			footer.Controls.Add(lit);



		} else if (results.Count == 1000) {
			lit = new Literal();
			lit.Text = "<p class='KWTooMany'>" + Xlt("Please add more search terms for better results", language) + "</p>";
			footer.Controls.Add(lit);

		}


		if (NeedUpdate != null && NeedUpdate.Count > 0) {
			int handle;
			handle = ModUniTran.DispatchUpdateRequest(lid, NeedUpdate, "", errorMessages);
			//This issues a request to the Universal Translating webservice - and (instantly) returns a handle 

			//pbi.path$ - tree.1 was pbi.path - but there's no real reason (apart from perhaps swift) not to placeprices across the whole tree
			if (handle == 0) {
				errorMessages.Add("* Could not dispatch web request (handle was 0)");
			} else {
				//"KWresultSet"
				//outputResults.Controls.Add(fetcherImage("KWresultSet", handle)) 'inserts an image with an onload script which calls the js FillPrices() after 5 seconds
				outputResults.Controls.Add(fetcherImage("KwResultsHolder", handle, NeedUpdate));
				//inserts an image with an onload script which calls the js FillPrices() after 5 seconds

			}
		}


	}

	/// <summary>
	/// Scores every Branch against the keywords (or fragments) - bear in mind branches appear in many places - see also clsBranch.Score
	/// </summary>
	/// <param name="frags"></param>
	/// <returns></returns>
	/// <remarks></remarks>
	private Dictionary<int, clsKwScore> ScoreBranches(List<string> frags, clsLanguage language)
	{

		int m;
		//bit mask (gets doubled and 'OR'd in)
		m = 1;

		UInt64 lid = Request.QueryString("lid");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		object viewAllRight = AccountHasRight(lid, "VIEWALL");

		Dictionary<int, clsKwScore> scores;
		scores = new Dictionary<int, clsKwScore>(1000);
		//size "estimate"

		int minorMatches = 0;
		int majorMatches = 0;

		object t = Stopwatch.GetTimestamp;

		string frag;
		int cc = 0;
		//Frags are either indiviudal words (that were separated by spaces) or phrases (that were separtaed by commas) from the searchText
		foreach ( frag in frags) {
			frag = frag.ToLower;

			if (Len(frag) > 1) {
				//use LINQ to quickly fetch all the branches that feature the keyword
				//a future optimisation might be to search the traslations only (and then return the branches that reference them)
				//Dim j = From branch In iq.Branches.Values Where LCase(branch.keywords(s_lang)).Contains(frag) And Not branch.Hidden  ' Or InStr(LCase(rr.keywords(s_lang)), kw) > 0

				//                Dim bids = iq.Branches.Keys.ToList()
				// bid In iq.Branches.Keys ' bids
				foreach ( branch in iq.Branches.Values) {
					//Dim branch = iq.Branches(bid)
					//For now, this has been used to supress SBSO 'Acceeories and services' - as a speed up
					if (!branch.unSearchable & !branch.deleted) {

						// HPI/HPE split - make sure products from the other side aren't visible to the current agent (unless the user has the VIEWALL right)
						if (!viewAllRight) {
							if (!branch.Product == null) {
								if (branch.Product.Manufacturer != agentAccount.Manufacturer)
									continue;
							}
						}

						//NEW match by sku first
						if (branch.Product != null && branch.Product.SKU.Contains(frag)) {
							if (!scores.ContainsKey(branch.ID)) {
								scores.Add(branch.ID, new clsKwScore());
							}

								//wether there is a match on this particular fragment (this may get set more than once)
								//the total number of matches (of this fragment)
							 // ERROR: Not supported in C#: WithStatement


						} else if (branch.Majorkeywords(language).ToLower.Contains(frag)) {
							if (!scores.ContainsKey(branch.ID)) {
								scores.Add(branch.ID, new clsKwScore());
							}

								//wether there is a match on this particular fragment (this may get set more than once)
								//the total number of matches (of this fragment)
							 // ERROR: Not supported in C#: WithStatement

						} else if (branch.minorKeywords(language).ToLower.Contains(frag)) {
							if (!scores.ContainsKey(branch.ID)) {
								scores.Add(branch.ID, new clsKwScore());
							}

								//wether there is a match on this particular fragment (this may get set more than once)
								//the total number of matches (of this fragment)
							 // ERROR: Not supported in C#: WithStatement

						}

						cc += 1;
						if (cc == 100) {
							int ssid = iq.sesh(lid, "searchID");
							//abandon
							if (ssid != searchID) {
								abandon = true;
								return scores;
								//BAIL
							}
							cc = 0;
						}
						//if the session variable searchID everty diverged from the local variable searchID - this (same) used has started another search - and this search can be abandoned.

						//    If scores.Count > 1000 Then Exit For ' significant speedup/optimisation - at the cost that result sets for short fragments may not be entirely  'correct'
					}
				}

				m = m + m;
				//Double the Bit mask (to address the next bit column in the Integer) - and the next fragment
			}

		}

		float et = (Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000;

		return scores;
		//Note Scores will ONLY contains values for scoring branches

	}

	private Array segments(path, ref int segnum)
	{

		//pre-load the first part of the results with the treecursors' segments
		//(so we search only from the current point in the tree)
		int[] seg = new int[49];

		foreach (string ss in Split(path, ".")) {
			if (ss != "tree") {
				seg(segnum) = (int)ss;
			}
			segnum += 1;
		}

		return seg;

	}
	/// <summary>Splits the supplied searchtext into words and any comma seperated phrases</summary>
	private List<string> Fragments(string searchText)
	{

		Fragments = new List<string>();
		//Break the search text into fragments (phrases are comma seperated)
		int c;
		//comma
		int s;
		//space
		if (Right(searchText, 1) != " ")
			searchText += " ";
		do {
			c = InStr(searchText, ",");
			if (c > 0) {
				Fragments.Add(Left(searchText, c - 1));
				searchText = Mid(searchText, c + 1);
			} else {
				s = InStr(searchText, " ");
				Fragments.Add(Left(searchText, s - 1));
				searchText = Mid(searchText, s + 1);
			}
		} while (!(searchText == ""));

	}


	//Public Sub Junk()


	//    Dim j As List(Of Integer)
	//    j = New List(Of Integer)
	//    j.Add(23)
	//    j.Add(123)
	//    j.Add(223)
	//    j.Add(323)
	//    j.Add(5423)
	//    j.Add(5323)
	//    j.Add(243)
	//    j.Add(223)

	//    Dim t As Double
	//    Dim et As Double
	//    Dim frag
	//    Dim sql$



	//    t = Diagnostics.Stopwatch.GetTimestamp

	//    Dim scores As Dictionary(Of String, Integer)
	//    'iq.Root.score(tl, scores)


	//    et = (Diagnostics.Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000




	//    '      Dim sql$
	//    Sql$ = "SELECT id, count(*) as score FROM branch b join translation t on b.fk_translation_key = t.key translation where t.text like '" & frag & "%' and t.l;"  'OR - oR

	//    Sql$ = "SELECT"

	//    For Each branch In iq.Branches.Values
	//        If LCase(Left$(branch.Text(s_lang), Len(frag))) = frag Then

	//        End If
	//    Next

	//    If Len(frag) > 2 Then
	//        Dim path$
	//        path$ = iq.sesh(lid,"TreeCursor")

	//        'a product set is all the products that appear below a specific branch - eg. all 'notebooks' (and their options, options options etc)

	//        'See if we have a product set for this path cached.. otherwise, add one
	//        Dim productSet As List(Of clsProduct)

	//        With iq.PathCache  'this object contains a cache of recently/frequently used product sets by path, shared accross all user
	//            'each set is the distinct products that appear under the specified branch
	//            If .Sets.ContainsKey(path$) Then
	//                productSet = .Sets(path$)
	//            Else
	//                If .Sets.Count > 15 Then 'allow up to 15 cached sets - as the largest sets are kept, these will tend to be the BU's /large families
	//                    .removeSmallest() ' remove the smallest set (as it costs least to replace should we need to)
	//                End If
	//                productSet = .Add(path$)
	//            End If
	//        End With

	//        'terms enclosed in commas are ANDed, the remainder are OR'd
	//        Dim parts As List(Of String)
	//        parts = New List(Of String)

	//        '7.2k hdd,500gb  - searches for 7.2k hdd (as a term) OR 500gb
	//        'blue ray drive - searches for blue OR ray OR drive

	//        Dim c As Integer
	//        Dim s As Integer
	//        If Right$(frag, 1) <> " " Then frag &= " "

	//        Do
	//            c = InStr(frag, ",")
	//            If c Then
	//                parts.Add(Left(frag, c - 1))
	//                frag = Mid$(frag, c + 1)
	//            Else
	//                s = InStr(frag, " ")
	//                parts.Add(Left(frag, s - 1))
	//                frag = Mid$(frag, s + 1)
	//            End If
	//        Loop Until frag = ""

	//        Dim lit As Literal
	//        Dim matches As Integer = 0
	//        Dim score As Integer

	//        Dim results As Dictionary(Of clsProduct, Integer)
	//        results = New Dictionary(Of clsProduct, Integer)

	//        For Each p In productSet
	//            score = 0
	//            For Each a In p.Attributes.Values
	//                For Each kw In parts 'keyword
	//                    If InStr(LCase(iq.Translations(a.TextKey).text(s_lang)), kw) Then
	//                        score = score + 1
	//                    End If
	//                Next
	//            Next
	//            If score Then results.Add(p, score)
	//        Next

	//        'Use LINQ to return a sorted version of the dictionary (by descending value of score)
	//        Dim sorted = From rr In results Order By (rr.Value) Descending

	//        For Each kv In sorted  'iterate the key value (product, score)  pairs 
	//            lit = New Literal
	//            lit.Text = kv.Key.ID & "^" & kv.Value.ToString & " " & kv.Key.displayName(s_lang) & "]"
	//            Form.Controls.Add(lit)
	//            matches += 1
	//            If matches > 15 Then Exit For
	//        Next

	//        'sort the 'red list' - by matches withing the bracnh name
	//    End If




	//End Sub

}
