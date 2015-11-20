
using System.Web.Caching;
using System.Globalization;

class FilterSort
{


	public Panel ContrastSpacer()
	{

		//contrast spacer
		Panel pnl = new Panel();
		pnl.CssClass = "matrixCell";
		pnl.Attributes("Style") = "width:3em;";

		Literal lit = new Literal();
		//we have to have *something* in the panel - or it isn't rendered
		lit.Text = "&nbsp;";
		pnl.Controls.Add(lit);

		return pnl;

	}



	/// <summary>
	/// The sub datatable has the Branch (or object) ID's - Filtering and Sorting are achieved with Dataviews onto a datatables which carry extracted Int64 numeric data
	/// </summary>
	/// <param name="bi"></param>
	/// <param name="dic"></param>
	/// <param name="errors"></param>
	/// <returns></returns>
	/// <remarks></remarks>


	private List<int> FilterFieldIDs(string filter)
	{

		//Note filter$ is a module level variable containing 
		//a ^ (circumflex)  delimited list of filters each of which has 3 | (pipe) delimited segements,  FieldID|Operator|Value
		//ultimately the filter as applied to the dataview

		FilterFieldIDs = null;
		if (filter != "") {
			FilterFieldIDs = new List<int>();

			List<string> f = Split(filter, "^").ToList;
			foreach ( i in f) {
				//only return those filters that have an operand
				if (Split(i, "|").Count == 3) {
					FilterFieldIDs.Add(Split(i, "|")(0));
				}
			}
		}

	}


	public Panel Gutter()
	{

		Gutter = new Panel();
		Gutter.Style("width") = ".75em";
		Gutter.Style("float") = "left";
		Literal lit = new Literal();
		lit.Text = "&nbsp;";
		// we have to put *something* in a div.. or it isn't rendered by ASP.NET
		Gutter.Controls.Add(lit);

	}

	public Panel FilledDDL(clsField f, object selectedValue, clsLanguage language, string controlid, bool enabled, Panel rowPanel, int depth, ref List<string> errorMessages)
	{
		// DropDownList

		//Returns a panel conaining a TextBox and a filled DropDown list with script for autosuggest attached

		FilledDDL = new Panel();

		FilledDDL.Attributes("style") = "overflow:visible;display:inline-block;";
		//Overflow so the DDL can hang oout of the div

		TextBox TypedTxt = new TextBox();
		//caries the selected/typed text
		TextBox txtObjID = new TextBox();
		//carries the ID of the selected item

			//light blue
			//INPUT elements do not behave well with inline-block
			//leave room for the buttons - there is no good way yo do this - this is the best way i can find  http://coding.smashingmagazine.com/2013/02/27/css-form-elements-problem/             
		 // ERROR: Not supported in C#: WithStatement


			//.CssClass = "SelectedID input" 'The input class is vital it tags it a a data carrier
			//The input class is vital it tags it a a data carrier
			//this hidden ctextBox carries the ID of the selected item
		 // ERROR: Not supported in C#: WithStatement


		FilledDDL.Controls.Add(TypedTxt);
		FilledDDL.Controls.Add(txtObjID);
		//invisible

		//Edit this list button
		if (rowPanel != null) {
			Panel Elpnl = new Panel();
			rowPanel.Controls.Add(Elpnl);
			Elpnl.ID = f.lookupOf;
			FilledDDL.Controls.Add(editor.MakeButton(true, "El", "Edit this list", editor.EmbedScript(f.lookupOf, f.lookupOf, "" + depth + 1, false, true)));


			//Edit the target button
			Panel etpnl = new Panel();
			rowPanel.Controls.Add(etpnl);
			etpnl.ID = f.lookupOf;
			if (selectedValue != null) {
				etpnl.ID = f.lookupOf + "(" + selectedValue.id + ")";
				object targetPath = f.lookupOf + "(" + selectedValue.id + ")";
				FilledDDL.Controls.Add(editor.MakeButton(true, "Et", "Edit the target " + f.labelText.text(language), editor.EmbedScript(targetPath, targetPath, "", false, true)));
			}

		}

		Panel ddh = new Panel();
		//Drop down holder.. (for the actual DDL)
		ddh.CssClass = "dropdownHolder";
		FilledDDL.Controls.Add(ddh);

		//ddh.Attributes("style") = "overflow:visible;min-height:100px;"
		//ddh.Attributes("z-index") = 100
		ddh.ID = "ddh_" + controlid;

		object dic = null;
		string[] bits = Split(f.lookupOf, "(");
		//The 'lookupof' a a fied may have a (field=value) filter on the end e.g.  States(group=TH) - returns only states whos group = TH

		string luObj = bits(0);

		dic = Reflection.WalkPropertyValue(iq, luObj, errorMessages);
		//look in a root level dictionary

		if (selectedValue != null) {
			TypedTxt.Text = selectedValue.displayname(language);
			//IMPORTANT - show the selected value
			if (object.ReferenceEquals(dic, iq.States)) {
				TypedTxt.Style("background-color") = selectedValue.colour;
				//Colour code the textbox - if it's a state from the states dictionary
			}
		}

		//function suggest(textBoxID,valueBoxID, divID, dicName) //Used by the editor
		if ((f.InputType.code == "translate")) {
			TypedTxt.Attributes("onkeyup") = "suggest('" + TypedTxt.ID + "','" + txtObjID.ID + "','" + ddh.ID + "','translation');";
			//<any filter is passed here (through to suggest.aspx)  - call the autosuggest on every keyUp
		} else {
			TypedTxt.Attributes("onkeyup") = "suggest('" + TypedTxt.ID + "','" + txtObjID.ID + "','" + ddh.ID + "','" + f.lookupOf + "');";
			//<any filter is passed here (through to suggest.aspx)  - call the autosuggest on every keyUp
		}

		//select all the text in the textbox when it's clicked (via Jquesry #id selector) 
		//- display the list, and populate with ALL matches (to a blank string)

		object oc;
		//On clicking the typed text box . . 
		oc = "$(document.getElementById('" + TypedTxt.ID + "')).select();";
		oc += "display('" + ddh.ID + "','inline-block');";
		if ((f.InputType.code == "translate")) {
			oc += "suggest('" + TypedTxt.ID + "','" + txtObjID.ID + "','" + ddh.ID + "','translation');";
		} else {
			oc += "suggest('" + TypedTxt.ID + "','" + txtObjID.ID + "','" + ddh.ID + "','" + f.lookupOf + "');";
		}

		//oc$ &= "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','" & PathName & "." & f.propertyName "');"

		//";"
		TypedTxt.Attributes("onclick") = oc;
		//setTimeout(function(){a(d
		TypedTxt.Attributes("onblur") = "setTimeout(function(){display('" + ddh.ID + "','none')},500);";
		//hide the DDL when we move off the textbox - BUT GIVE IT long enough to accept a selection !!!

	}
	public Panel FilledTranslation(clsField f, clsTranslation selectedValue, clsLanguage language, string controlid, bool enabled, Panel subPanel, int depth, ref List<string> errorMessages)
	{
		// DropDownList

		//Returns a panel conaining a TextBox and a filled DropDown list with script for autosuggest attached


		FilledTranslation = new Panel();

		FilledTranslation.Attributes("style") = "overflow:visible;display:inline-block;";
		//Overflow so the DDL can hang oout of the div

		TextBox TypedTxt = new TextBox();
		//caries the selected/typed text
		TextBox txtObjID = new TextBox();
		//carries the ID of the selected item

			//light blue
			//INPUT elements do not behave well with inline-block
			//leave room for the buttons - there is no good way yo do this - this is the best way i can find  http://coding.smashingmagazine.com/2013/02/27/css-form-elements-problem/             
		 // ERROR: Not supported in C#: WithStatement


			//.CssClass = "SelectedID input" 'The input class is vital it tags it a a data carrier
			//The input class is vital it tags it a a data carrier
			//this hidden ctextBox carries the ID of the selected item
		 // ERROR: Not supported in C#: WithStatement


		FilledTranslation.Controls.Add(TypedTxt);
		FilledTranslation.Controls.Add(txtObjID);
		//invisible

		if (subPanel != null) {
			//Edit this list button
			FilledTranslation.Controls.Add(editor.MakeButton(true, "El", "Edit this list", editor.EmbedScript(f.lookupOf, subPanel.ID, "" + depth + 1, false, true)));

			//Edit the target button
			//If selectedValue IsNot Nothing Then
			//    Dim targetPath$ = f.lookupOf & "(" & selectedValue.id & ")"
			//    FilledTranslation.Controls.Add(editor.MakeButton(True, "Et", "Edit the target " & f.labelText, editor.EmbedScript(targetPath, subPanel.ID, "", False, True)))
			//End If
		}

		Panel ddh = new Panel();
		//Drop down holder.. (for the actual DDL)
		ddh.CssClass = "dropdownHolder";
		FilledTranslation.Controls.Add(ddh);

		//ddh.Attributes("style") = "overflow:visible;min-height:100px;"
		//ddh.Attributes("z-index") = 100
		ddh.ID = "ddh_" + controlid;

		object dic = null;
		string[] bits = Split(f.lookupOf, "(");
		//The 'lookupof' a a fied may have a (field=value) filter on the end e.g.  States(group=TH) - returns only states whos group = TH

		string luObj = bits(0);

		dic = Reflection.WalkPropertyValue(iq, luObj, errorMessages);
		//look in a root level dictionary

		if (selectedValue != null) {
			TypedTxt.Text = selectedValue.textTranslation(language);
			//IMPORTANT - show the selected value
			//If dic Is iq.States Then
			//    TypedTxt.Style("background-color") = selectedValue.colour  'Colour code the textbox - if it's a state from the states dictionary
			//End If
		}

		//function suggest(textBoxID,valueBoxID, divID, dicName) //Used by the editor
		if ((f.InputType.code == "translate")) {
			TypedTxt.Attributes("onkeyup") = "suggest('" + TypedTxt.ID + "','" + txtObjID.ID + "','" + ddh.ID + "','translation');";
			//<any filter is passed here (through to suggest.aspx)  - call the autosuggest on every keyUp
		} else {
			TypedTxt.Attributes("onkeyup") = "suggest('" + TypedTxt.ID + "','" + txtObjID.ID + "','" + ddh.ID + "','" + f.lookupOf + "');";
			//<any filter is passed here (through to suggest.aspx)  - call the autosuggest on every keyUp
		}

		//select all the text in the textbox when it's clicked (via Jquesry #id selector) 
		//- display the list, and populate with ALL matches (to a blank string)

		object oc;
		//On clicking the typed text box . . 
		oc = "$(document.getElementById('" + TypedTxt.ID + "')).select();";
		oc += "display('" + ddh.ID + "','inline-block');";
		if ((f.InputType.code == "translate")) {
			oc += "suggest('" + TypedTxt.ID + "','" + txtObjID.ID + "','" + ddh.ID + "','translation');";
		} else {
			oc += "suggest('" + TypedTxt.ID + "','" + txtObjID.ID + "','" + ddh.ID + "','" + f.lookupOf + "');";
		}

		//oc$ &= "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','" & PathName & "." & f.propertyName "');"

		//";"
		TypedTxt.Attributes("onclick") = oc;
		//setTimeout(function(){a(d
		TypedTxt.Attributes("onblur") = "setTimeout(function(){display('" + ddh.ID + "','none')},500);";
		//hide the DDL when we move off the textbox - BUT GIVE IT long enough to accept a selection !!!

	}
	public string RemoveFilter(filters, string toRemove)
	{

		if (filters == "") {
			Beep();
			//this should never happen (becuase remove filter buttons are dynamically disabled
		}


		List<string> l = Split(filters, "^").ToList;

		List<string> output = new List<string>();

		foreach ( i in l) {
			[] p = Split(i, "|");
			if (Trim(p(0)) != Trim(toRemove)) {
				output.Add(i);
				//Add all but the one we're deleting
			}
		}

		return Join(output.ToArray, "^");
		//join all the | delimited filters back together

	}


	public Int64 sortValue(string myString)
	{

		//Computes a sortable vaue from the multiword Input string
		//Uses 6 bits per character and encodes into a INT64 (8 bytes/64 bits)

		sortValue = 0;

		myString = Replace(myString, "  ", " ");
		// remove any double spaces
		string[] words = Split(myString);

		int chars = 6;
		if (words.Count >= 3) {
			chars = 2;
		} else if (words.Count == 2) {
			chars = 3;
		} else if (words.Count == 1) {
			chars = 6;
		} else {
			System.Diagnostics.Debugger.Break();
			//this should never happen
		}

		Int64 pwr = Int64.MaxValue;
		pwr = pwr / 64;
		//Integer divide by 64 'for the most significiant '


		foreach ( w in words.Take(3)) {
			for (c = 1; c <= chars; c++) {
				if (c <= Len(w)) {
					int cv = Asc(Mid(w, c, 1)) - 64;
					//the 'ascii' values start around 64 for an @ A=65 (this doesn't really handle more elaborate non-western encodings - and will ultimately require a (fairly big) rethink
					if (cv < 0)
						cv = 0;
					if (cv > 63)
						cv = 63;

					sortValue += pwr * cv;
				}

				pwr = pwr / 64;
				if (pwr <= 0)
					System.Diagnostics.Debugger.Break();
			}
		}

	}



	public string basetype(ty)
	{

		basetype = "System." + ty;

		if (ty == "translate" | ty == "one" | ty == "nullstring") {
			basetype = "System.string";
		} else if (ty == "many") {
			basetype = "System.Int32";
			// we sort by the number of attached items
		} else if (ty == "customerprice" | ty == "nullprice") {
			basetype = "System.Single";
		} else if (ty == "xnote") {
			basetype = "System.string";

		} else if (ty == "icon") {
			basetype = "System.string";

		}

	}

	private string textRep(Dictionary<clsField, Dictionary<clsFilter, string>> activeFilters)
	{

		//returns a text representation of the supplied set of filter values - used as a key to the cache of views (you can't use a view unless the filters are the same)

		textRep = "";
		if (activeFilters != null) {
			foreach ( field in activeFilters.Keys) {
				textRep += Trim(field.ID) + "^";
				foreach ( flt in activeFilters(field).Keys) {
					textRep += Trim(flt.ID) + "^" + activeFilters(field)(flt) + "^";
					//final key value pair
				}
			}
		}

	}


	public List<string> DeBracket(l, ob = "[", cb = "]")
	{

		//returns a list of all the items within [brackets] in l$

		int o = 1;
		int c = 1;

		DeBracket = new List<string>();

		//use a for loop so it can never lock up 
		for (i = 1; i <= 1000; i++) {
			o = InStr(c, l, ob);
			if (o == 0)
				break; // TODO: might not be correct. Was : Exit For
			c = InStr(o, l, cb);

			DeBracket.Add(Mid(l, o + 1, (c - o - 1)));
		}

	}



}
