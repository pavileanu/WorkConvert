//Option Strict On

using dataAccess;
public class clsEditHeader
{

	public string path;
	public clsScreen matrix;
		//This a a dictionary of the ACTIVE filters, per field - each field can have more than one filter applied at once
	public Dictionary<clsField, Dictionary<clsFilter, string>> Filters = null;
	public Dictionary<int, clsPriorityDirection> sorts;
	public Dictionary<clsField, float> ColWidth;
	public int Fromindex;
	public int PerPage;
	//Public divID As String 'Panel
	public clsLanguage translationLanguage;
	public DataView VW;

	public DataTable DT;
	//If Not iq.SeshContains(bi.lid, "matrix." & bi.path) Then
	//            iq.sesh(bi.lid, "sorts." & bi.path$) = matrix.DefaultSorts  'when the matrix button is pressed.. set the default sort orders and filters (per the DefaultFilter and Defautl sort Properties of the clsFields within the clsScreen (aka matrix)
	//            iq.sesh(bi.lid, "colstate." & bi.path) = New Dictionary(Of clsField, enumColState) 'holds the expanded/collapsed info
	//            iq.sesh(bi.lid, "filters." & bi.path) = New Dictionary(Of clsField, Dictionary(Of clsFilter, String))  'matrix.DefaultFilters
	//            iq.sesh(bi.lid, "matrix." & bi.path) = matrix.ID 'True
	//        End If


	public clsEditHeader(string Path, object dicOrObj, clsScreen matrix, clsAccount buyeraccount, clsLanguage Language, ref List<string> errormessages, UInt64 lid)
	{
		if (matrix == null)
			System.Diagnostics.Debugger.Break();

		this.path = Path;
		this.matrix = matrix;
		this.Filters = new Dictionary<clsField, Dictionary<clsFilter, string>>();
		//TODO  default filters
		this.sorts = matrix.DefaultSorts;
		this.ColWidth = new Dictionary<clsField, float>();
		//   Me.divID = divid 'panel
		this.translationLanguage = Language;

		if (IsDictionary(dicOrObj)) {
			this.DT = MakeStubDataTable(dicOrObj);
			this.VW = new DataView(DT);
			addMissingColumns(dicOrObj, buyeraccount, Language, new HashSet<string>(), errormessages, lid);
		}
	}

	public int RowIndex(int id)
	{

		//Returns -1 if a there is no row in the VIEW with an ID column containing ID
		//NB: The underlying tdatatable *may* contain such a row - bit the view might be filtering it

		string keep = VW.Sort;
		this.VW.Sort = "ID";
		RowIndex = VW.Find(id);
		this.VW.Sort = keep;

	}

	private DataTable MakeStubDataTable(object dic)
	{

		DataColumn col;
		col = new DataColumn("ID", typeof(Int32));

		DataTable dt = new DataTable();
		dt.Columns.Add(col);

		dt.PrimaryKey = new DataColumn[] { col };
		//Set the PK on the datatable so we can FIND (primarily to delete)

		object[] c = new object[-1];
		//ID column in the data table
		foreach ( k in dic.Keys) {
			c(0) = k;
			dt.Rows.Add(c);
		}

		return dt;

	}


	public void addRow(UInt64 lid, object instance, clsScreen screen, clsAccount buyerAccount, clsLanguage language, ref List<string> errormessages)
	{
		//Adds a row, containing the essential columns (for the dataview - which is providing filtering and sorting on the dictionary we're editing)

		List<object> row = new List<object>();
		//this is an array of fields that will form the new row

		foreach (DataColumn c in DT.Columns) {
			//find the field which fills this column (in the datatable)
			object ff = from f in screen.Fields.Valueswhere f.propertyName == c.ColumnName;
			if (ff.Any) {
				clsField fld = ff.First;
				row.Add(fld.CellValue(instance, fld.propertyName, buyerAccount, language, "", null, false, null, null, new HashSet<string>(),
				errormessages, lid));
			}
		}

		DT.Rows.Add(row.ToArray);

	}


	public void fillCol(clsField f, DataColumn col, object dic, clsAccount buyeraccount, clsLanguage language, hashset<string> foci, ref List<string> errormessages, UInt64 lid)
	{
		//This is specifically for the the editor - Dic is allways a dictionary of integer>clsSomething
		int r = 0;

		foreach ( id in dic.Keys) {
			DT.Rows(r).Item(col) = f.CellValue(dic(id), f.propertyName, buyeraccount, language, "", null, false, null, null, foci,
			errormessages, lid);
			//            DT.Rows(r).Item(col) = f.CellValue(dic(id), prop, buyeraccount, language, Nothing, False, Nothing, Nothing, foci, errormessages)
			r += 1;
		}

	}


	public void addMissingColumns(object dic, clsAccount buyeraccount, clsLanguage Language, hashset<string> foci, ref List<string> errormessages, UInt64 lid)
	{
		//dic is always a dictionary of integer>someCls  (clsUser,clsAccount,clsField,ClsWhatever)

		DataColumn col;
		foreach ( fld in this.Filters.Keys) {
			if (!DT.Columns.Contains(fld.propertyName)) {
				if (fld.InputType.code == "string") {
					col = new DataColumn(fld.propertyName, typeof(string));
					//Single))
				} else {
					col = new DataColumn(fld.propertyName, typeof(Int64));
					//Single))
				}

				DT.Columns.Add(col);
				this.fillCol(fld, col, dic, buyeraccount, Language, new HashSet<string>(), errormessages, lid);
			}
		}

		//do the same for sorts (add a column to the underlying datatable for everything we're sorting on)
		foreach ( so in sorts.Values) {
			//Dont add the same column twice (we may already have added it for filtering)
			if (!DT.Columns.Contains(so.column.propertyName)) {


				if (so.column.InputType.code == "string") {
					col = new DataColumn(so.column.propertyName, typeof(string));
					//Single))
				} else {
					col = new DataColumn(so.column.propertyName, typeof(Int64));
					//Single))
				}

				DT.Columns.Add(col);
				this.fillCol(so.column, col, dic, buyeraccount, Language, new HashSet<string>(), errormessages, lid);
			}
		}

		string ft = ConstructFilter();

		this.VW.RowFilter = ft;


	}

	public void setColWidth(clsField fld, float width)
	{
		ColWidth(fld) = width;
	}

	/// <summary>Used for debugging - displays a textual representatiom of the current filters applied by this matrixHeader</summary>
	public string currentFilters()
	{

		currentFilters = "";
		foreach ( fld in Filters.Keys) {
			currentFilters += (fld.displayName(English) + ":");
			foreach ( fltVp in Filters(fld)) {
				currentFilters += fltVp.Key.DisplayText.text(English) + "|" + fltVp.Value + " ";
			}
		}

	}


	public void UpdateSorts(clsPriorityDirection NewPriority)
	{
		//gives us a Field, priority and direction to update
		if (!this.sorts.ContainsKey(NewPriority.Priority)) {
			this.sorts.Add(NewPriority.Priority, NewPriority);
		} else {
			this.sorts(NewPriority.Priority) = NewPriority;
		}
	}

	public string SortsString()
	{

		return "";

	}


	public void RemoveFilter(string toRemove)
	{
		//to remove contains the fieldID^Filter ID

		string[] p = Split(toRemove, "|");
		this.Filters(iq.Fields((int)p(0))).Remove(iq.i_Filters_Code(p(1)));

		this.VW.RowFilter = ConstructFilter();

	}

	//TextFrag As String)
	public void UpdateFilters(int fieldID, string filterCode, string Value)
	{

		//Uses the changefilter parameter which
		//looks like 738|GE|1.02
		//Field ID, Filter Code (operator), New operand (value)

		//Dim c$()
		//c$ = Split(changefilter, "|")

		//If UBound(c) <> 2 Then
		//    Beep()
		//End If

		clsField fld;
		clsFilter flt;
		fld = iq.Fields(fieldID);
		flt = iq.i_Filters_Code(filterCode);
		//Greater than, Equal, Less than like etc..

			//sortValue(TextFrag) 'may contain an integer            
		 // ERROR: Not supported in C#: WithStatement


	}

	private string ConstructFilter()
	{

		//Turns the current filters dictionary -  into something actually usable by the dataview
		//note, the operator segment generaly contains [filterValue] and [col] placeholders - which is replaced with the value segment and column name
		//thus  738|SW|4
		//becomes
		//[Displayname]=LIKE 'home*' AND [years]=4

		ConstructFilter = "";
		if (Filters == null)
			return;

		foreach ( fld in Filters.Keys) {
			foreach ( flt in Filters(fld).Keys) {
				//Where we have a string value - it needs to be 'quoted' (form factors)

				object colname;
				object qp;

				qp = Replace(flt.Filter, "[filterValue]", this.Filters(fld)(flt));
				//grab the template fo rthis filter criterea and replace the current value

				colname = "[" + fld.propertyName + "]";
				qp = Replace(qp, "[col]", colname);
				ConstructFilter += qp + " AND ";
			}
		}

		if (ConstructFilter != "")
			ConstructFilter = Left(ConstructFilter, Len(ConstructFilter) - 5);
		//Take the last AND off

	}

	public Panel UI(clsAccount buyeraccount, clsLanguage language, ref List<string> errorMessages, Panel inpanel)
	{

		//this uses the path of the branch being displayed as a matrix - which IS NOT the same as the matrix path - the actual matrix in used may be defined much higher (eg. at the 'servers' level')..
		//But a set of filters, sorts, and collapsed columns is maintained for each branch displaying its children as a matrix (for example each server family branch)
		//This is difficult to grasp - but it's vital you understand before messing with it (or you will create some horrible bugs)

		//Returns the user facing UI for filtering and sorting this matirx
		UI = new Panel();
		UI.CssClass = "editHeader";
		DropDownList drpLanguage = new DropDownList();
		drpLanguage.DataSource = iq.ActiveLanguages.Values;
		drpLanguage.DataTextField = "LocalName";
		drpLanguage.DataValueField = "ID";
		//drpLanguage.ID = Me.divID & "_lang"
		drpLanguage.ID = this.path + "_lang";
		drpLanguage.SelectedValue = translationLanguage.ID;
		drpLanguage.DataBind();
		// Dim dropdownScript As String = "var ddl = document.getElementById('" & Me.divID & "_lang" & "'); var spd=ddl.options[ddl.selectedIndex].value;"
		string dropdownScript = "var ddl = document.getElementById('" + this.path + "_lang" + "'); var spd=ddl.options[ddl.selectedIndex].value;";
		//dropdownScript &= "embed('../editor/editor.aspx?cmd=language&path=" & path$ & "&value='+spd" & ",'" & Me.divID & "',false,false);return false;"
		dropdownScript += "embed('../editor/editor.aspx?cmd=language&path=" + path + "&value='+spd" + ",'" + this.path + "',true,false);return false;";
		drpLanguage.Attributes("onchange") = dropdownScript;
		UI.Controls.Add(drpLanguage);
		UI.Controls.Add(this.matrix.EditTitles(this, language));
		UI.Controls.Add(this.FiltersUI(language, inpanel.ID, errorMessages));
		//.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate
		UI.Controls.Add(this.SortsUI);
		UI.Controls.Add(this.ControlsUI);


	}

	private Panel ControlsUI()
	{
		//Adds the widen,narrow,promote,demote, show and hide buttons for a column

		ControlsUI = new Panel();

		ControlsUI.Controls.Add(NewLit("<div class='efcSpacer'></div>"));

		//iterate the fields in order of their Order property

		foreach ( f in from v in this.matrix.Fields.Valuesorderby v.order) {
			ControlsUI.Controls.Add(col);

			string fid = Trim(f.ID.ToString);

			if (!f.visibleList) {
				//Hidden columns Only have a 'show' button
				//col.Width = New Unit(1, ut)
				col.Controls.Add(editor.MakeButton(true, "S", Xlt("Show this column", this.translationLanguage) + "(" + f.propertyName + ")", editor.EmbedScript(path, path, "show," + fid, false, true)));
			//btn = ControlButton("S", 
			//col.Controls.Add(btn)
			//btn.Attributes("onmousedown") = 

			} else {
				col.Controls.Add(editor.MakeButton(true, "+", Xlt("widen this column", this.translationLanguage), editor.EmbedScript(path, path, "widen," + fid, false, true)));

				//don't let them narrow a column to nothing !!
				if (f.width > 1) {
					col.Controls.Add(editor.MakeButton(true, "-", Xlt("Narrow this column", this.translationLanguage), editor.EmbedScript(path, path, "narrow," + fid, false, true)));
				}

				col.Controls.Add(editor.MakeButton(true, "H", Xlt("Hide this column", this.translationLanguage), editor.EmbedScript(path, path, "hide," + fid, false, true)));
				col.Controls.Add(editor.MakeButton(true, "<", Xlt("Promote this column", this.translationLanguage), editor.EmbedScript(path, path, "promote," + fid, false, true)));
				col.Controls.Add(editor.MakeButton(true, ">", Xlt("Demote this column", this.translationLanguage), editor.EmbedScript(path, path, "demote," + fid, false, true)));

			}
		}

	}


	public void changeLayout(string cmd, clsField fld, ref List<string> errorMessages)
	{
		switch (cmd) {

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"widen":

				fld.width += 1;

				fld.update(errorMessages);
			//If Me.ColWidth(fld) > 100 Then
			//    errorMessages.Add("You can't widen this column any further")
			//Else
			//    'Me.ColWidth(fld) += 1
			//End If

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"narrow":

				fld.width -= 1;

				fld.update(errorMessages);

			//If Me.ColWidth(fld) > 1 Then
			//    Me.ColWidth(fld) -= 1
			//Else
			//    errorMessages.Add("You can't narrow this column any further - you can hide it if you like")
			//End If
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"promote":
				//hmm this means the only have one, global position - the editHeader should probably contain a sorted list to make this per user
				fld.promote(errorMessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"demote":
				errorMessages.Add("Demote is not supported");
			//                fld.demote(errorMessages) 'hmm this means the only have one, global position - the editHeader should probably contain a sorted list to make this per user
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"demote":
				//hmm this means the only have one, global position - the editHeader should probably contain a sorted list to make this per user
				fld.demote();
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"show":
				fld.visibleList = true;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"hide":
				fld.visibleList = false;
			default:

				errorMessages.Add("Unrecognised changelayout cmd " + cmd);
		}

		fld.update(errorMessages);
		//persist the changes in the DB (expensive)

	}

	private Panel FiltersUI(clsLanguage language, string holdingRowID, ref List<string> errormessages)
	{

		//returns a panel containing Filtering UI for the EDITOR - A full set of columns

		Panel pnl = new Panel();
		Panel col;

		// pnl.Controls.Add(ContrastSpacer) 'make room for the 'contrast' checkboxes

		pnl.CssClass = "editFilters";
		//pnl.Attributes("style") = "display:inline-block;"
		//for each column in the matrix we can have a value for each filter - so we can say >5 AND <10
		bool AreSome = false;

		pnl.Controls.Add(NewLit("<div class='efcSpacer'></div>"));
		//iterate the fields in order of their Order property

		//Me.ColIsCollapsed(fld))
		foreach ( fld in from v in this.matrix.Fields.Valuesorderby v.order) {
			pnl.Controls.Add(col);


			if (fld.visibleList) {
				// Dim ddl As DropDownList = fld.OperatorDDL() - this *was* the operator DDL (Deprecated)
				// ddl.ID = "ops." & holdingRowID & "." & fld.ID 'give this DDL a uniqueID
				// col.Controls.Add(ddl)

				Panel pnlTxt = new Panel();
				col.Controls.Add(pnlTxt);

				string valueControlID;
				if (fld.InputType.code == "one") {
					//One type fields get an autoSuggest

					Int64 currentindex = 0;
					//This is an FK/Pointer
					if (!this.Filters == null) {
						if (this.Filters.ContainsKey(fld)) {
							currentindex = (int)this.Filters(fld).Values(0);
						}
					}

					object selectedtarget;
					if (currentindex == 0) {
						selectedtarget = null;
					} else {
						selectedtarget = Reflection.WalkPropertyValue(iq, fld.lookupOf, errormessages)(currentindex);
						//this object is the 'target' of the foregin key - and contains the selected value 
					}

					valueControlID = "f_" + this.path + "f" + fld.ID;
					Panel suggest = FilledDDL(fld, selectedtarget, language, valueControlID, true, null, 0, errormessages);
					//Obj2 carries the ID
					//for 'one' type fields this is a autocomplete DDL - which yields the FK to macth on
					pnlTxt.Controls.Add(suggest);

				} else if (fld.InputType.code == "translate") {
					//One type fields get an autoSuggest

					Int64 currentindex = 0;
					//This is an FK/Pointer
					if (!this.Filters == null) {
						if (this.Filters.ContainsKey(fld)) {
							currentindex = (int)this.Filters(fld).Values(0);
						}
					}

					clsTranslation selectedTranslation;
					if (currentindex == 0) {
						selectedTranslation = null;
					} else {
						selectedTranslation = iq.Translations(currentindex);
						//this object is the 'target' of the foregin key - and contains the selected value 
					}

					valueControlID = "f_" + this.path + "f" + fld.ID;
					Panel suggest = FilledTranslation(fld, selectedTranslation, language, valueControlID, true, null, 0, errormessages);
					//Obj2 carries the ID
					//for 'one' type fields this is a autocomplete DDL - which yields the FK to macth on
					pnlTxt.Controls.Add(suggest);


				} else {
					TextBox ft = new TextBox();
					//Filtert tex 'EG dl38'
					ft.ID = "ft." + holdingRowID + "." + fld.ID;
					ft.CssClass = "editFilterTextBox";
					// ft.Attributes("style") &= "width:" & CStr(fld.width - 1.5) & "em;"
					pnlTxt.Controls.Add(ft);
					valueControlID = ft.ID;

					//populate with any exisitng (filter) value
					if (this.Filters.ContainsKey(fld)) {
						if (this.Filters(fld).Count) {
							if (fld.InputType.code == "string") {
								ft.Text = this.Filters(fld)(iq.i_Filters_Code("CN"));
								//populate the textbox with the CONTAINS substring filter (for strings) 
							} else {
								ft.Text = this.Filters(fld)(iq.i_Filters_Code("EQ"));
								//populate the textbox with the 
							}
						}
					}
				}

				//unfinished
				//CMD is the cmd parameter of the Ajax callback 
				//Dim cmd$ = "changefilter," & fld.ID & ",'"  '<<-IMPORTANT SINGLE QUOTE - breaks out of the JS literal string and back into JS    
				//cmd$ &= "+getElementById('" & ddl.ID & "').value +','+getElementById('" & valueControlID & "').value " '  'this is the filters code
				//cmd$ &= "+'" 'back into the literal string

				string op;
				if (fld.InputType.code == "string") {
					op = "CN";
				} else {
					op = "EQ";
				}

				object cmd = "changefilter," + fld.ID + "," + op + ",'+getElementById('" + valueControlID + "').value +'";
				//Some very importanst 's  in here breaking in and out of JS
				pnlTxt.Controls.Add(editor.MakeButton(true, "go", "apply this filter", editor.EmbedScript(this.path, this.path, cmd, false, true)));

				Panel cancelButtons = new Panel();
				cancelButtons.CssClass = "editFilterCancel";
				pnlTxt.Controls.Add(cancelButtons);

				//remove' buttons for the (currently applied) filters

				if (!this.Filters == null) {
					if (this.Filters.ContainsKey(fld)) {
						//ME.filters are those 'active' filters in this header 
						foreach ( flt in this.Filters(fld).Keys) {
							cancelButtons.Controls.Add(editor.MakeButton(true, "x", "Remove this filter " + fld.propertyName + " " + flt.DisplayText.text(language) + " " + this.Filters(fld)(flt), editor.EmbedScript(this.path, this.path, "removeFilter," + Trim(fld.ID.ToString) + "|" + Trim(flt.Code), false, true)));
							AreSome = true;
						}
					}
				}
			}
		}

		// pnl.CssClass = "filtersUI"

		// If AreSome Then pnl.Attributes("style") &= "height:" & collapsedColumnWidth & "em;"

		return pnl;

	}

	public Panel SortsUI()
	{

		SortsUI = new Panel();
		SortsUI.CssClass = "editSorts";


		SortsUI.Controls.Add(NewLit("<div class='efcSpacer'></div>"));
		Panel col;

		//iterate the fields in order of their Order property

		//Me.ColIsCollapsed(f))
		foreach ( f in from v in this.matrix.Fields.Valuesorderby v.order) {
			col.ID = path + ".F" + f.ID;

			SortsUI.Controls.Add(col);


			if (f.visibleList) {
				string currentValue = "-";

				//ptb = f.SortPriorityTextBox(Me.sorts$, currentValue)
				//occ$ = "sortPath='" & path$ & "';sortFieldID=" & f.ID & ";showSortPriorityPicker('" & col.ID & "','" & currentValue & "');"
				//ptb.Attributes("onfocus") = occ$
				// ptb.ToolTip = "Set the priority and direction of this sorting for this column"
				// col.Controls.Add(ptb)

			}
		}

	}

}
