
public class compare : clsPageLogging
{

	private Dictionary<string, clsBranch> FetchBranchesToCompare(string checkboxIds)
	{

		FetchBranchesToCompare = new Dictionary<string, clsBranch>();
		//path>branch

		//We've been supplied a comma seperated list of checkbox control id's of the form 
		//sl_tree.1.5.1920.8292.63627,sl_tree.1.5.1920.12939.39939 . delimited paths
		foreach (string I in Split(checkboxIds, ",").ToList) {
			//there's a trailing comma - which is easier to ignore here than faff about with in JS
			if (I != "") {
				FetchBranchesToCompare.Add(Split(I, "_")(1), iq.Branches((int)Split(I, ".").Last));
				//
			}
		}

	}


	private Dictionary<string, Dictionary<clsBranch, string>> BuildCompareMatrix(UInt64 lid, Dictionary<string, clsBranch> BranchesToCompare, ref List<string> errorMessages__1)
	{

		//The display data is assembled in a dictionary of dictioanries 
		//The first index being a string representation of the attribute title "Size", "PowerConsumprion", "SKU" etc
		//each row in this outer dictionary contains a dictionary - of BRANCH>display value (for each attribute)

		Dictionary<string, Dictionary<clsBranch, string>> cm = new Dictionary<string, Dictionary<clsBranch, string>>();
		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		clsQuantity q;

		//pass 1, create a matrix - with one row for each attribute/quantity/slot for each branch being compared

		object ky;

		clsBranch branch;
		// each branch forms one column in the comparison matrix
		//the keys are the paths, the values are the terminal branches (systems we're comparing)
		foreach ( pth in BranchesToCompare.Keys) {

			//product attributes
			branch = BranchesToCompare(pth);
			foreach ( pa in BranchesToCompare(pth).Product.Attributes.Values) {
				if (pa.Attribute.Order == 0)
					continue;
				//Ignore anything which is order 0

				//supress some attributes here
				if (InStr("mfrsku,", LCase(pa.Attribute.Code)) == 0) {

					ky = pa.Attribute.displayName(agentAccount.Language);
					if (!cm.ContainsKey(ky)) {
						cm.Add(ky, new Dictionary<clsBranch, string>());
					}
					if (!cm(ky).ContainsKey(branch)) {
						cm(ky).Add(branch, pa.displayName(agentAccount.Language));
						//this is the display value of the product attribute EG. 16gb - NOT the attribute (eg. "memory")
					}
				}
			}

			//Preinstalled options
			//path$ & "." & Trim$(branch.ID))
			foreach ( q in branch.preInstalled(buyerAccount, pth, errormessages)) {
				ky = q.Branch.Product.ProductType.DisplayName(agentAccount.Language);
				//q.Branch.Product.DisplayName(agentAccount.Language)
				if (!cm.ContainsKey(ky)) {
					cm.Add(ky, new Dictionary<clsBranch, string>());
				}
				object v = q.Branch.Product.DisplayName(agentAccount.Language);
				if (!cm(ky).ContainsKey(branch))
					cm(ky).Add(branch, IIf(q.NumPreInstalled > 1, q.NumPreInstalled + " x " + v, v).ToString);
			}

			//consolidate the slot info (count up the number of PCI slots of each type)
			Dictionary<clsSlotType, int> slottypes;
			//slottype > count
			slottypes = new Dictionary<clsSlotType, int>();

			foreach ( s in BranchesToCompare(pth).slots.Values) {
				if (s.path == pth | s.path == "") {
					//normal' slots
					if (object.ReferenceEquals(s.slotNum.value, DBNull.Value)) {
						slottypes.Add(s.Type, s.numSlots);
					} else {
						//numbered, PCI slots
						if (s.numSlots != 1)
							System.Diagnostics.Debugger.Break();
						if (!slottypes.ContainsKey(s.Type)) {
							slottypes.Add(s.Type, 1);
						} else {
							slottypes(s.Type) += 1;
							//add another (of this type of PCI slot)
						}
					}
				}
			}

			object stn;
			//slot type name
			foreach ( s in slottypes.Keys) {
				stn = s.displayName(agentAccount.Language);
				if (!cm.ContainsKey(stn)) {
					cm.Add(stn, new Dictionary<clsBranch, string>());
				}
				cm(stn)(branch) = slottypes(s).ToString;
				//set the value to the number of slots of this type                
			}
		}

		return cm;

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//this ASPX is called AJAX'd in from the JS contrast() function 
		//thus:-
		//rExec("contrast.aspx?BPs=" + elids + '&path=' + path, placeContrast); 

		//We render a column (in the matrix) for each BRANCH being compared

		UInt64 lid = (UInt64)Request.QueryString("lid");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");

		Dictionary<string, clsBranch> BranchesToCompare = FetchBranchesToCompare(Request("BPs"));

		List<string> errorMessages__1 = new List<string>();
		Dictionary<string, Dictionary<clsBranch, string>> cm = BuildCompareMatrix(lid, BranchesToCompare, errormessages);

		Pnl.Controls.Add(outputTable(cm, BranchesToCompare, Request("path"), agentAccount, buyerAccount, lid));

		OutputErrors(Pnl.Controls, errorMessages__1, lid, true);

	}

	private Table outputTable(Dictionary<string, Dictionary<clsBranch, string>> cm, Dictionary<string, clsBranch> branches, path, clsAccount AgentAccount, clsAccount BuyerAccount, UInt64 lid)
	{

		Table t = new Table();
		Pnl.Controls.Add(t);
		t.CssClass = "compareTable";

		//make the headers
		TableRow tr;
		tr = new TableRow();

		//top left grid cell placeholder
		t.Controls.Add(tr);

		TableCell topLeft = ContrastCell("&nbsp;", "compareTopLeftCell", branches.Count + 1, 30);
		tr.Controls.Add(topLeft);

		object occ;
		occ = "document.getElementById('contrast." + path + "').innerHTML='';return false;";

		//Dim btnclose As ImageButton = New ImageButton
		// btnclose.Attributes("onmousedown") = occ$
		// btnclose.ID = "btnCloseContrast"
		// btnclose.OnClientClick = "return false;" 'stop it from posting back
		// btnclose.ImageUrl = "/images/navigation/close.png"

		//topLeft.Controls.Add(MakeRoundButton("close.png", "Finish comparing (close)", occ$, "", "", AgentAccount.Language))

		TableCell colheader;
		//Panel
		//these are the paths
		foreach ( k__1 in branches.Keys) {
			//branches are the things we're comparing - so branches.count is the number of columns (+1 for the row heads)
			colheader = ContrastCell(branches(k__1).Product.DisplayName(AgentAccount.Language), "compareColumnHeads", branches.Count + 1, 0);
			Literal lit = new Literal();
			lit.Text = "<br/>";
			colheader.Controls.Add(lit);
			colheader.Controls.Add(branches(k__1).BuyUI(BuyerAccount, k__1, lid));
			((Panel)colheader.Controls(colheader.Controls.Count - 1)).Style("text-align") = "center;";
			tr.Controls.Add(colheader);
		}

		//headerRow.Controls.Add(UnFloat)

		//key (attribute name/title) >Distinct column count
		Dictionary<string, int> distinctCount = new Dictionary<string, int>();
		List<string> distinct;

		//the keys are the attribute names/titles (row headers - column 0)
		foreach ( K__2 in cm.Keys) {


			distinct = new List<string>();
			if (cm(K__2).Values.Count < branches.Count) {
				distinct.Add("");
				//we have at least one 'null' (how many - doesnt actually matter)
			}


			foreach ( v in cm(K__2).Values) {
				if (!distinct.Contains(v))
					distinct.Add(v);
			}

			distinctCount.Add(K__2, distinct.Count);
			// The number of matching columns (columns that carry the same value) is the number of columns (branches we are contrasting)minus the number of distinct ones
		}

		object css;

		//LINQ
		object iter = (from kvp in distinctCountorderby kvp.Value descending);
		//Use linq to create an iterator object which steps through the keys in desceding order of the number of distinct values

		object value;

		TableRow row;
		//Panel
		List<string> headers = new List<string>();


		foreach ( kvp in iter) {

			object h = "Differences";
			if (kvp.Value > 1 & !headers.Contains(h)) {
				t.Controls.Add(Sectionheader(h, branches.Count));
				headers.Add(h);
			} else {
				h = "Similarities";
				if (kvp.Value == 1 & !headers.Contains(h)) {
					t.Controls.Add(Sectionheader(h, branches.Count));
					headers.Add(h);
				}
			}

			row = new TableRow();
			t.Controls.Add(row);

			//row header (attribute name)
			//                                                                            %width
			row.Controls.Add(ContrastCell(kvp.Key, "compareRowHeads", branches.Count + 1, 30));

			distinct = new List<string>();
			//used when rendering each row 

			foreach ( branch in branches.Values) {
				if (cm(kvp.Key).ContainsKey(branch)) {
					value = cm(kvp.Key)(branch);

					//they're all the same
					if (distinctCount(kvp.Key) == 1) {
						css = "compareSetAllSame";
					} else {
						if (!distinct.Contains(value))
							distinct.Add(value);
						css = "compareSet" + Trim(distinct.IndexOf(value).ToString);
						//render each disitnct group in a different colour
					}
					row.Controls.Add(ContrastCell(value, css, branches.Count + 1, 0));
				} else {
					row.Controls.Add(ContrastCell("-", "compareWithout", branches.Count + 1, 0));
				}
			}
			//row.Controls.Add(UnFloat())
		}

		return t;

	}

	private TableRow Sectionheader(h, int cols)
	{

		Sectionheader = new TableRow();
		TableHeaderCell td = new TableHeaderCell();
		td.CssClass = "sectionHead";
		td.ColumnSpan = cols + 1;
		Sectionheader.Controls.Add(td);
		Literal lit = new Literal();
		lit.Text = "<span>" + h + "</span>";
		td.Controls.Add(lit);

	}

	private Literal UnFloat()
	{
		Literal lit = new Literal();
		lit.Text = "<div style='float:left;clear:both;'>  &nbsp; </div>";
		return lit;

	}
	private TableCell ContrastCell(text, css, int cols, int width)
	{

		TableCell tc = new TableCell();
		Literal lit;
		//hd.Attributes("style") = "height:inherit;width:" & Int(99 / cols) & "%;float:left;color:white;background-color:" & css & ";text-align:" & textAlign & ";border-style:solid;border-width:0px;border-right-width:1px;border-bottom-width:1px;"
		//tc.Attributes("style") = "color:white;background-color:" & css & ";text-align:" & textAlign & ";"
		tc.CssClass = css;
		if (width != 0)
			tc.Attributes("style") += "width:" + width + "em;";
		lit = new Literal();
		lit.Text = text;
		tc.Controls.Add(lit);

		return tc;

	}

}
