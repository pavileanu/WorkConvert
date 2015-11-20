using System.Globalization;
using System.IO;

[Serializable()]
public class clsMatrixHeader
{

	public string path;
	public clsScreen matrix;
	public Dictionary<clsField, Dictionary<clsFilter, List<Int64>>> Filters = null;
		//The priority is the key (makes sense, honest)
	public Dictionary<int, clsPriorityDirection> sorts;

	public Dictionary<clsField, enumColState> ColState;
	public System.Data.DataView Vw;

	public System.Data.DataTable DT;
	public bool QuickFiltersVisible;
		//Holds the surviving count (distinct) translations for this Quick filter - populated my addmissingcolumns
	private Dictionary<clsField, Dictionary<clsTranslation, int>> dicTrans;
		//QuickFilter fields of type BANDS a
	private Dictionary<clsField, List<clsMatrixHeader.clsBand>> dicBands;
	//Private dicAttrib As Dictionary(Of clsfiled, Dictionary(clsattribute,integer))  'holds the count of
		//All the DISTINCT numeric values (and the survivor counts thereof)
	private Dictionary<clsField, Dictionary<Int64, int>> dicNums;
		//for each (numeric) field detect and validate the UNITS
	private Dictionary<clsField, clsUnit> dicUnits;
	private UInt64 lid;
	private Dictionary<clsField, clsAccountScreenField> _FieldResultSet;
	public Dictionary<clsField, clsAccountScreenField> FieldResultSet {
		get {
			if (_FieldResultSet == null) {
				clsAccount l = iq.sesh(lid, "BuyerAccount");
				List<string> errormessages = new List<string>();
				//TODO add default display unit here
				IEnumerable<clsScreenOverride> asa = iq.ScreenOverrides.Where(so => so.AccountID == l.ID & so.ScreenID == this.matrix.ID & so.Path == this.path).Select(dd => dd);
				object screenOverrideObjects = from s in iq.Screens(this.matrix.ID).Fields.Valuesasas.IDa.FieldIdGroupfrom m in lr.DefaultIfEmpty()new {
					a = new clsAccountScreenField {
						AccountID = l.ID,
						ScreenID = this.matrix.ID,
						Path = path,
						FieldId = s.ID,
						Visibility = m == null || m.ForceVisibilityTo == null ? s.visibleList : m.ForceVisibilityTo,
						Order = m == null || m.ForceOrderTo == null ? s.order : m.ForceOrderTo,
						Width = m == null || m.ForceWidthTo == null ? s.width == 0 ? null : s.width : m.ForceWidthTo,
						Description = s.labelText,
						DisplayUnit = m == null || m.DisplayUnit == null ? null : m.DisplayUnit
					},
					b = s
				};

				if (AccountHasRight(lid, "GLOBALADM")) {
					_FieldResultSet = screenOverrideObjects.ToDictionary(a => a.b, a => a.a);
				} else {
					_FieldResultSet = screenOverrideObjects.Where(f => f.b.CanUserSelect).ToDictionary(a => a.b, a => a.a);
				}
			}
			return _FieldResultSet;
		}
	}

	public List<clsAccountScreenField> AllAvailableFields {
//Get a list of all fields from the screen and populate with any overrides for this user
		get { return FieldResultSet.Select(a => a.Value).ToList(); }
	}
	public List<clsField> EffectiveFields {
//Get a list of all fields from the screen and populate with any overrides for this user
		get { return FieldResultSet.Where(a => a.Value.Visibility | iq.Fields.Values.Where(al => al.LinkedFieldID != null ? al.LinkedFieldID : -1 == a.Key.ID).Count > 0).OrderBy(d => d.Value.Order).Select(a => a.Key).ToList(); }
	}
	public List<clsField> EffectiveFieldsWithFilters {
//Get a list of all fields from the screen and populate with any overrides for this user
		get { return FieldResultSet.Where(a => a.Value.Visibility | a.Key.QuickFilterGroup != null | iq.Fields.Values.Where(al => al.LinkedFieldID != null ? al.LinkedFieldID : -1 == a.Key.ID).Count > 0).OrderBy(d => d.Value.Order).Select(a => a.Key).ToList(); }
	}

	/// <summary>Export the grid as CSV - respecting the filters and sorts</summary>
	/// <remarks>Column visibility (collapsedosity) and ordering are not yet respected </remarks>
	public string exportCSV(UInt64 lid, Dictionary<clsBranch, clsVisibility> descendants, clsAccount buyeraccount, clsLanguage l, HashSet<string> foci, ref List<string> errorMessages)
	{

		Randomize();
		object fn = System.IO.Path.GetTempPath + "\\export-" + buyeraccount.ID + Rnd(1).ToString + ".csv";
		//this isn't terribly robust 

		iq.sesh(lid, "tostream") = fn;
		iq.sesh(lid, "streamcontent-type") = "text/csv;charset=UTF-8\"  ";
		iq.sesh(lid, "DeleteStreamed") = true;

		try {
			// A 'normal' filestream removed the frist CRLF (on the header row WTF ?)
			StreamWriter sw = new StreamWriter(File.Create(fn), System.Text.Encoding.UTF8);


			//Write the quoted names of the columns (in the agent's languae)
			sw.WriteLine(this.headerRow(l));



			for (int i = 0; i <= this.Vw.Count - 1; i++) {
				int bid = (int)this.Vw(i).Item("ID");
				clsBranch branch = iq.Branches(bid);
				clsVisibility vis = descendants(branch);
				this.exportRow(lid, vis, sw, buyeraccount, l, foci, errorMessages);
			}

			sw.Close();


		} catch (System.Exception ex) {
			errorMessages.Add(ex.Message.ToString + " (Could not create the file ? )" + fn);

		}

	}

	public string headerRow(language)
	{

		List<string> qc = new List<string>();
		foreach ( field in this.matrix.Fields.Values) {
			if (field.visibleList) {
				qc.Add(Utility.CSV(field.labelText.text(language)));
			}
		}

		return Join(qc.ToArray, ",");

	}


	//clone method should copy the filters and sorts of - and use the datatable for columns that exist  - and 
	private DataTable MakeStubDataTable(clsAccount buyeraccount, bool showall, object dic, ref List<string> errors)
	{

		string culture = buyeraccount.BuyerChannel.Region.Culture;

		DataTable dt = new DataTable();
		//PathName(bi.path)) 'give it a pretty name (for debugging)
		CultureInfo ci = null;
		try {
			ci = new CultureInfo(culture);
		} catch {
			ci = new CultureInfo("EN-gb");
			Beep();
		}

		dt.Locale = ci;

		DataColumn col;
		col = new DataColumn("ID", typeof(Int32));
		dt.Columns.Add(col);

		object[] c = new object[-1];
		//ID column in the data table

		foreach ( vs in dic.values) {
			// If vs.branch.product Is Nothing Then Stop
			//  If Not vs.branch.hassku Then Stop

			if (vs.hideReasonList.Count == 0 | showall == true) {
				c(0) = vs.branch.id;
				dt.Rows.Add(c);
			}
		}

		return dt;

	}



	public void exportRow(UInt64 lid, clsVisibility row, StreamWriter sw, clsAccount buyeraccount, clsLanguage language, HashSet<string> foci, ref List<string> errorMessages)
	{
		List<string> cols = new List<string>();
		foreach ( f in this.matrix.Fields.Values) {
			if (f.visibleList) {
				cols.Add(f.CSV(row.branch, row.path, buyeraccount, language, this, false, foci, errorMessages, lid, 0));
			}
		}

		sw.WriteLine(Join(cols.ToArray, ","));

	}

	/// <summary>
	/// </summary>
	/// <param name="bi"></param>
	/// <param name="descendants"></param>
	/// <param name="errormessages"></param>
	/// <param name="copyFiltersFrom">Copy the filter values from an existing (usually ancestor) matrix.. used from the 'family finder'</param>
	/// <remarks></remarks>

	public clsMatrixHeader(clsBranchInfo bi, Dictionary<clsBranch, clsVisibility> descendants, bool quickFiltersVisible, ref List<string> errormessages)
	{
		//If descendants.Count = 0 Then Stop

		//We hold a reference to each matrixheader in the users session (keyed by their login ID)
		Dictionary<string, clsMatrixHeader> matrixHeaders = (Dictionary<string, clsMatrixHeader>)iq.sesh(bi.lid, "matrixHeaders");
		if (matrixHeaders.ContainsKey(bi.path)) {
			matrixHeaders.Remove(bi.path);
		}

		matrixHeaders.Add(bi.path, this);

		this.path = bi.path;
		this.matrix = MatrixAbove(this.path);
		//NOTE: this is the ONLY place we find out what TYPE of matrixHeader we need to instance
		this.lid = bi.lid;

		object mha = matrixHeaderAbove(lid, path, errormessages);
		//If Not mha Is Nothing Then
		//    If mha.DT IsNot Nothing Then Me.DT = mha.Vw.ToTable()
		//    Me.Vw = New DataView(DT)
		//End If
		this.setup(bi, descendants, errormessages);
		//calls addmissingColums - populating any column we're sorting of filtering by

		//Me.setup(bi, descendants, errormessages) 'calls addmissingColums - populating any column we're sorting of filtering by

		this.Vw.RowFilter = this.ConstructFilter;
		this.QuickFiltersVisible = quickFiltersVisible;

	}


	public void rebuild(clsBranchInfo bi, Dictionary<clsBranch, clsVisibility> descendants, ref List<string> errormessages)
	{
		//ML - todo why dont we just invalidate this?
		//NA - don' tknow - sounds logical

		//        Me.setup(bi, descendants, errormessages) 'calls addmissingColums - populating any column we're sorting of filtering by
		this.DT = MakeStubDataTable(bi.buyerAccount, bi.showAll, descendants, errormessages);
		this.addMissingColumns(descendants, this.DT, bi.buyerAccount, bi.agentAccount.Language, bi.foci, errormessages, bi.lid);
		//fills the datable (which underlies the dataview we will be returning)


	}

	public clsMatrixHeader()
	{
		System.Diagnostics.Debugger.Break();
	}


	public clsMatrixHeader(UInt64 lid, string path, clsScreen screen)
	{
		// Stop

		//special' constructor - used for the root level bootstrap instance
		this.matrix = screen;
		this.path = path;
		this.ColState = new Dictionary<clsField, enumColState>();
		this.DT = null;
		//these will be populated JIT *if* we switch the root level to a grid
		this.Vw = null;


		//This dictionary of the active filters, sorts and datasets (held in a clsMatrixHeader) - is persisted in each users session (every user has different ones!)
		Dictionary<string, clsMatrixHeader> matrixHeaders = new Dictionary<string, clsMatrixHeader>();
		matrixHeaders.Add(this.path, this);
		iq.sesh(lid, "matrixHeaders") = matrixHeaders;

		this.lid = lid;

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

				object qp = string.Empty;
				foreach ( filt in this.Filters(fld)(flt)) {
					qp += "(" + Replace(flt.Filter, "[filterValue]", filt);
					//grab the template fo rthis filter criterea and replace the current value 
					//Change for GE LE
					if (flt.Code == "LE" && Filters(fld).ContainsKey(iq.i_Filters_Code("GE"))) {
						qp += " AND ";
						qp += Replace(iq.i_Filters_Code("GE").Filter, "[filterValue]", Filters(fld)(iq.i_Filters_Code("GE"))(this.Filters(fld)(flt).IndexOf(filt)));
					}
					qp += ") OR ";
				}
				if (qp != "")
					qp = Left(qp, Len(qp) - 4);
				//Take the last AND off

				colname = "[" + fld.propertyName + "]";
				qp = Replace(qp, "[col]", colname);
				ConstructFilter += "(" + qp + ") AND ";
			}
		}

		if (ConstructFilter != "")
			ConstructFilter = Left(ConstructFilter, Len(ConstructFilter) - 5);
		//Take the last AND off

	}



	public void setup(clsBranchInfo bi, Dictionary<clsBranch, clsVisibility> descendants, ref List<string> errormessages)
	{
		this.Filters = new Dictionary<clsField, Dictionary<clsFilter, List<Int64>>>();
		//TODO  default filters
		this.sorts = this.matrix.DefaultSorts;
		//each field has a defaultsort priority and directon - eg 1A 3D
		this.ColState = new Dictionary<clsField, enumColState>();
		this.dicTrans = new Dictionary<clsField, Dictionary<clsTranslation, int>>();
		this.dicBands = new Dictionary<clsField, List<clsMatrixHeader.clsBand>>();
		this.dicNums = new Dictionary<clsField, Dictionary<Int64, int>>();
		this.dicUnits = new Dictionary<clsField, clsUnit>();

		//Me.DT = New DataTable
		//Me.Vw = New DataView(DT)
		//        Me.FillDataTable(bi, descendants, errormessages)

		//this is a very important speed up - we only want to do this ONCE - not every postabck !
		if (this.DT == null) {
			this.DT = MakeStubDataTable(bi.buyerAccount, bi.showAll, descendants, errormessages);
			//fills the datable (which underlies the dataview we will be returning)
			this.Vw = new DataView(DT);
			//creates a new view onto the 'stub' datatable (wich just contains ID's)
		}

		if (this.DT.Rows.Count == 0) {
			errormessages.Add("datatable was empty in FillDatatTable");
			return;
		}

		object flt;
		// If bi.MatrixHeader IsNot Nothing Then
		//AddmissingColumns populates those columns in the datatable we want to sort or filter by - just in time, and Once ony using reflection
		//all subsequent operations use this pre-loaded data
		addMissingColumns(descendants, DT, bi.buyerAccount, bi.agentAccount.Language, bi.foci, errormessages, bi.lid);
		//adds (and populates) any columns we're wanting to sort or filter by to the datatable
		flt = ConstructFilter();
		//Me.matrix, bi.MatrixHeader.Filters)

		//don't ever set the filter property to nothing - everything dissapears !
		if (flt != "") {
			try {
				this.Vw.RowFilter = flt;

			} catch {
				errormessages.Add("invalid filters " + flt);
			}
		}

		//       Dim sorts As String
		//        If bi.MatrixHeader IsNot Nothing Then
		//        sorts = bi.MatrixHeader.sortsString
		//  Else
		//  sorts = bi.EditHeader.SortsString
		//  End If

		object sorts = this.sortsString;
		try {
			Vw.Sort = sorts;
		} catch (Exception ex) {
			errormessages.Add("invalid sorts " + sorts);
		}

	}




	public void addMissingColumns(Dictionary<clsBranch, clsVisibility> dic, ref DataTable dt, clsAccount buyeraccount, clsLanguage language, HashSet<string> foci, ref List<string> errormessages, UInt64 lid)
	{
		DataColumn col;
		DataColumn uncol;

		//we shouldnt have to do this - add the missing column to the view/datatable and get it from there
		foreach ( fld in this.EffectiveFieldsWithFilters) {
			switch (fld.QuickFilterUItype) {
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"TKEY":
				case "BANDS":
				case "NUMBANDS":
				case "NUMS":
				case "CHECK":
					// If Not Me.dicTrans.ContainsKey(fld) Then

					if (!dt.Columns.Contains(fld.propertyName)) {
						col = new DataColumn(fld.propertyName, typeof(Int64));
						//Single))
						dt.Columns.Add(col);
						uncol = new DataColumn(fld.propertyName + "UNIT", typeof(Int16));
						dt.Columns.Add(uncol);
						this.fillCol(dt, fld, col, dic, buyeraccount, language, foci, errormessages, lid);
						//also populates dicTrans,dicBands  and dicNums
					}
				//End If

				case  // ERROR: Case labels with binary operators are unsupported : Equality
"":
				default:
					errormessages.Add("unrecognised filter UI type :" + fld.QuickFilterUItype);
			}
		}

		foreach ( fld in this.Filters.Keys) {
			if (!dt.Columns.Contains(fld.propertyName)) {
				col = new DataColumn(fld.propertyName, typeof(Int64));
				//Single))

				//    If fld.propertyName.Contains("CP_SVC") Then Stop
				dt.Columns.Add(col);
				uncol = new DataColumn(fld.propertyName + "UNIT", typeof(Int16));
				dt.Columns.Add(uncol);
				this.fillCol(dt, fld, col, dic, buyeraccount, language, foci, errormessages, lid);
			}
		}

		//do the same for sorts (add a column to the underlying datatable for everything we're sorting on)
		foreach ( so in sorts.Values) {
			//Dont add the same column twice (we may already have added it for filtering)
			if (!dt.Columns.Contains(so.column.propertyName)) {
				col = new DataColumn(so.column.propertyName, typeof(Int64));
				//Single))

				dt.Columns.Add(col);
				uncol = new DataColumn(so.column.propertyName + "UNIT", typeof(Int16));
				dt.Columns.Add(uncol);
				this.fillCol(dt, so.column, col, dic, buyeraccount, language, foci, errormessages, lid);
			}
		}

	}

	private void AddFieldToQFDic(clsField f, ref List<string> errormessages)
	{
		switch (f.QuickFilterUItype) {
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"TKEY":
				if (!dicTrans.ContainsKey(f)) {
					dicTrans.Add(f, new Dictionary<clsTranslation, int>());
				}
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"BANDS":
			case "NUMBANDS":
				if (!this.dicBands.ContainsKey(f)) {
					dicBands.Add(f, new List<clsBand>());
				}
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"NUMS":
			case "CHECK":

				if (!dicNums.ContainsKey(f)) {
					dicNums.Add(f, new Dictionary<Int64, int>());
					//counts of distinct VALUES (by field)
				}
			// Case Is = "CHECK"

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"":
			default:
				errormessages.Add("unknown quickFilterUItype:" + f.QuickFilterUItype);
		}

	}

	/// <summary>
	/// Fills the specified column on the datatable with numeric values retrieved for the field F
	/// </summary>
	/// <remarks>Also Populates dicNums,dicTrans and dicBands - used for the QuickFilters</remarks>

	private void fillCol(DataTable dt, clsField f, DataColumn col, Dictionary<clsBranch, clsVisibility> dic, clsAccount buyeraccount, clsLanguage language, HashSet<string> foci, ref List<string> errormessages, UInt64 lid)
	{
		clsTranslation translation = null;

		AddFieldToQFDic(f, errormessages);
		//The QFdics store the distinct values, (and their 'survivor' counts)

		Int64 numericvalue;
		List<Int64> values = new List<Int64>();

		clsBranch branch = null;

		//index the Visbilities - by branch (for fast access to the  PATHs we'll need

		Dictionary<clsBranch, clsVisibility> vbb = new Dictionary<clsBranch, clsVisibility>();
		foreach ( v in dic.Values) {
			if (!vbb.ContainsKey(v.branch))
				vbb.Add(v.branch, v);
		}

		//we're working with the entire unfiltered datatable here always) - no view involved
		foreach (DataRow row in dt.Rows) {

			branch = iq.Branches((Int32)row("id"));

			//The cellvalue returns Numeric Value (if present), or translation.Sortvalue (which IS translation .order from non-zero orders, or an 'alphabetical' sort otherwise
			clsUnit unit = null;
			//this check shouldn't be required but JIT carepacks has some issue
			if (vbb.ContainsKey(branch)) {
				numericvalue = f.CellValue(branch, vbb(branch).path, buyeraccount, language, "", null, false, translation, unit, foci,
				errormessages, lid);
			}

			// If numericvalue <> Int64.MinValue And numericvalue <> 0 Then Stop

			//This remembers the units for this column - which are needed for the labels in numeric quickFilters
			if (unit != null) {
				row.Item(col.ColumnName + "UNIT") = unit.ID;
				if (dicUnits.ContainsKey(f)) {
					//Ut oh, mismatched (or mixed units in a column)
					if (!object.ReferenceEquals(unit, dicUnits(f))) {
						errormessages.Add("Mismatched unit " + unit.Code);
					}
				} else {
					dicUnits(f) = unit;

				}
			}

			if (f.QuickFilterUItype == "TKEY") {
				if (translation != null) {
					row.Item(col) = translation.SortValue(language);

					if (!dicTrans(f).ContainsKey(translation)) {
						dicTrans(f).Add(translation, 1);
					} else {
						dicTrans(f)(translation) += 1;
					}
				} else {
					row.Item(col) = numericvalue;
					//translation.SortValue(language)
				}

			} else {
				//any numeric value on 'check' type fields is treated as a 1
				if (f.QuickFilterUItype == "CHECK" & numericvalue > Int64.MinValue)
					numericvalue = 1;

				row.Item(col) = numericvalue;
				//<<<THIS is 

				if (f.QuickFilterUItype == "BANDS" | f.QuickFilterUItype == "NUMBANDS") {
					if (numericvalue > Int64.MinValue) {
						values.Add(numericvalue);
					}


				} else if (f.QuickFilterUItype == "NUMS" | f.QuickFilterUItype == "CHECK") {
					if (numericvalue > Int64.MinValue) {
						//DONT change this - we WANT to add a 1 
						if (!dicNums(f).ContainsKey(numericvalue)) {
							dicNums(f).Add(numericvalue, 1);
						} else {
							dicNums(f)(numericvalue) += 1;
						}
					}
				}

			}
		}

		//make a set of bands for the set of values we just retrieved - Each band will have (approximately the same NUMBER of values)
		if (f.QuickFilterUItype == "BANDS" | f.QuickFilterUItype == "NUMBANDS") {
			if (values.Count) {
				dicBands(f) = MakeBands(values);
			}
		}


	}


	//Builds the bands such that each contains the same number of results - Eg, for 5 bands and 100 results, each band would have 20 results
	//NOTE: - This means that the min and max VALUES of the bands have not particular relationship to the range 
	//However - it's more useful to be able to fitler to the 'top 20% of laptops) rather than some arbitray value based bands

	private List<clsBand> MakeBands(List<long> values)
	{

		//build the bands - such that each contains the same number of results 

		MakeBands = new List<clsBand>();

		int numBands = 5;
		object sortedValues = from j in valuesorderby jj;
		Int64 bottom = Int64.MinValue;
		Int64 top = 0;
		int chunk = values.Count / numBands;
		clsBand band;



		if (sortedValues.Count < 5) {
			band = new clsBand(sortedValues.First, sortedValues.Last, values.Count);
			MakeBands.Add(band);

		} else {
			bottom = sortedValues.First;
			for (i = 1; i <= numBands - 1; i++) {
				int skip = (int)i / numBands * sortedValues.Count;

				top = (from z in sortedValues.Skip(skip)).First;
				band = new clsBand(bottom, top, chunk);
				MakeBands.Add(band);
				bottom = top;
			}

			//need to make the last band (i think)
			top = sortedValues.Last;
			band = new clsBand(bottom, top, chunk);
			MakeBands.Add(band);

			//Round and overlap the bands
			foreach ( band in MakeBands) {
				//band.Stretch()
			}
		}


	}


	public string sortsString()
	{

		//return the sorts in a format suitable for the sort propert of a dataview

		object s = "";
		foreach ( v in from j in this.sorts.Valuesorderby j.Priority) {
			string sd = "DESC";
			if (v.Direction != "D")
				sd = "";
			s += " [" + v.column.propertyName + "] " + sd + ",";
			//Note there are some pretty ciritcal spaces in here - mess with it at your peril
		}

		if (s.Length > 0) {
			s = Left(s, Len(s) - 1);
		}

		return s;

	}


	public void UpdateSorts(UInt64 lid, clsPriorityDirection NewPriority, Dictionary<clsBranch, clsVisibility> descendants, clsAccount buyeraccount, clsLanguage language, HashSet<string> foci, ref List<string> errormessages)
	{
		//gives us a Field, priority and direction to update

		IEnumerable<clsPriorityDirection> j = from v in this.sorts.Valueswhere object.ReferenceEquals(v.column, NewPriority.column);

		//we're changing an existing sort on this column (de-priortising it) - (by selecting it as a later sort than a pre-existing one)
		if (j.Any) {
			this.sorts.Remove(j.First.Priority);
		}

		if (!this.sorts.ContainsKey(NewPriority.Priority)) {
			this.sorts.Add(NewPriority.Priority, NewPriority);
		} else {
			this.sorts(NewPriority.Priority) = NewPriority;
		}

		this.reNumberSorts();

		this.addMissingColumns(descendants, this.DT, buyeraccount, language, foci, errormessages, lid);


		this.Vw.Sort = this.sortsString;

	}


	public string key(clsAccount buyeraccount)
	{

		//the columns preset are key (we may need to add one if they've added a filter)
		key = this.path + "-" + buyeraccount.SellerChannel.Code + "-" + buyeraccount.BuyerChannel.Code;

	}

	public void setColState(clsField fld, enumColState state)
	{
		ColState(fld) = state;
		//expaned/collapsed (etc)
	}

	// ''' <summary>Used for debugging - displays a textual representatiom of the current filters applied by this matrixHeader</summary>
	//Public Function currentFilters() As String

	//    currentFilters = ""
	//    For Each fld In Filters.Keys
	//        currentFilters &= (fld.displayName(English) & ":")
	//        For Each fltVp In Filters(fld)
	//            currentFilters &= fltVp.Key.DisplayText.text(English) & "|" & fltVp.Value & " "
	//        Next
	//    Next
	//End Function



	private void reNumberSorts()
	{
		//pulling a sort out of the middle of the stack (e.g. delete sort '2 of 3' causes misnumbering 

		object j = from s in this.sorts.Values.ToListorderby s.Priority;

		this.sorts.Clear();
		int p = 1;
		foreach ( i in j) {
			i.Priority = p;
			this.sorts.Add(p, i);
			p += 1;
		}

	}

	public void RemoveSort(int fldid)
	{
		this.sorts.Remove(fldid);
		this.reNumberSorts();

		this.Vw.Sort = this.sortsString;

	}


	public void removeFilters()
	{
		this.Filters.Clear();
		this.Vw.RowFilter = null;

	}

	public void RemoveFilter(string toRemove, ref List<string> errormessages)
	{
		//To remove contains the fieldID|FilterCODE|Filtercode|filtercode

		string[] p = Split(toRemove, "|");
		clsField fld = iq.Fields((int)p(0));
		clsFilter filter;

		if (this.Filters.ContainsKey(fld)) {
			for (i = 1; i <= UBound(p); i++) {
				if (iq.i_Filters_Code.ContainsKey(p(i))) {
					filter = iq.i_Filters_Code(p(i));
					this.Filters(fld).Remove(filter);
					//each field can have more than one filter applied simultaeneously
				} else {
					errormessages.Add("Can't remove filter with the CODE " + p(1));
				}
			}
		} else {
			errormessages.Add("No filter present for field " + p(0));
		}

		this.Vw.RowFilter = ConstructFilter();

	}


	public void UpdateFilters(changefilter)
	{
		//Uses the changefilter parameter which
		//looks like 738|GE|1.02
		//Field ID, Filter Code (operator), New operand (value)

		//have extended to allow - ie, multiple Filter-value pairs on the same field
		//fldID|GE|2000|LE|4000

		string[] p;
		p = Split(changefilter, "|");

		clsField fld;
		clsFilter flt;
		fld = iq.Fields((int)p(0));

		for (i = 1; i <= UBound(p) - 1; i += 2) {
			flt = iq.i_Filters_Code(p(i));

			 // ERROR: Not supported in C#: WithStatement

		}

		// Me.addMissingColumns(descendants, Me.DT, buyeraccount, language, foci, errormessages, lid) ML TODO READD

		this.Vw.RowFilter = this.ConstructFilter;

	}


	private Panel ExpandCollapseColumnButtons(clsLanguage language)
	{

		Panel panel = new Panel();
		panel.ID = "HeaderExpandRow";
		panel.CssClass = "oneRow";
		panel.Attributes("style") = "height:1em;";

		// Dim cols As IEnumerable = (From f In Me.EffectiveFields Order By f.order)
		panel.Controls.Add(NewLit("<div class='LeftPad' style='width:4.25em;display:inline-block;'>&nbsp;</div>"));
		float x = 0;

		foreach (clsField f in this.EffectiveFields) {
			//Dim d As Panel = f.emptyCell(f.isCollapsed(headerPath$, session))
			//img = New Image
			//img.Attributes("style") = "width:1.5em;height:1.5em;"
			//panel.Controls.Add(img)
			//panel.Controls.Add(d)
			// d.Controls.Add(img)

			Panel p = new Panel();
			p.Style("display") = "inline-block";
			if (this.ColIsCollapsed(f)) {
				p.Width = new Unit(collapsedColumnWidth, UnitType.Em);
				p.Controls.Add(MakeRoundButton("expandColumn.png", "Show Column", "getBranches('path=" + this.path + "&cmd=expandColumn&fieldid=" + Trim(f.ID.ToString) + "');return false;", "", "position:relative;", language, f.ID));

				x = x + collapsedColumnWidth;
			} else {
				// img.ImageUrl = "/images/navigation/collapseColumn.png"
				// img.Attributes("onmousedown") = "getBranches('" & headerPath & "','collapseColumn=" & Trim$(f.ID) & "');return false;"
				// img.ToolTip = "hide " & f.labelText & " column"

				p.Width = new Unit(FieldResultSet(f).GrownWidth, UnitType.Em);
				p.Controls.Add(MakeRoundButton("collapseColumn.png", "Hide Column", "getBranches('path=" + this.path + "&cmd=collapseColumn&fieldid=" + Trim(f.ID.ToString) + "');return false;", "", "position:relative;", language, f.ID));
				x = x + FieldResultSet(f).GrownWidth;
				//in ems
			}
			panel.Controls.Add(p);
		}

		//' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"
		return panel;

	}


	public bool hasQuickFilters()
	{

		hasQuickFilters = false;
		foreach ( fld in this.EffectiveFields) {
			if (fld.QuickFilterUItype != "") {
				hasQuickFilters = true;
				break; // TODO: might not be correct. Was : Exit For
			}
		}

	}

	/// <summary>
	/// A 'quickfilter' is a set of checkboxes containing the distinct values in one field (column)
	/// The act of rendering the quickfilters UI also (pre) scans the survivors (determined by the filter on the view) to enable/disable the correct options
	/// </summary>
	/// <returns></returns>
	/// <remarks></remarks>
	private Panel FiltersUI(clsBranchInfo bi, ref List<string> errorMessages)
	{


		FiltersUI = new Panel();
		FiltersUI.Attributes("class") = "quickFilterGroupHolder";
		Panel ButtonPanel = new Panel();
		ButtonPanel.CssClass = "FilterButtonContainer";

		if (this.QuickFiltersVisible) {
			Literal lit = new Literal();
			string bid = "hmcb." + bi.path;
			lit.Text = Replace("<div id=|" + bid + "| class=|QF hpBlueButton hideQF| onclick=|DisableElementsByClassName('FF');getBranches('cmd=hideQuickFilters&path=" + bi.path + "')|>Hide filters</div>", "|", Chr(34));
			ButtonPanel.Controls.Add(lit);
		}
		//If bi.bs IsNot Nothing And Not bi.Branch.HasSKU AndAlso bi.PathLevel <> 1 Then
		if (!bi.branch.HasSKU && bi.PathLevel != 1) {
			//this is an 'open','category' branch (eligible for quick filters)

			//this is the 'show filters' button the hide filters is in the matrixheader itself

			if (bi.MatrixHeader == null || bi.MatrixHeader.QuickFiltersVisible == false && this.matrix.Fields.ToList().Where(f => f.Value.QuickFilterGroup != null).Count() > 0) {
				//need to include parent filters in this!
				if (bi.MoreThanXskus(5) && bi.branch.Translation.Group != "OL3" && this.hasQuickFilters()) {
					string bid = "hmcb." + bi.path;
					//just needs a unique DIV id (serves no other purpose)
					Literal lit = new Literal();
					lit.Text = Replace("<div id=|" + bid + "| class=|hmc hpBlueButton ib showHMC| onclick=|getBranches('cmd=quickFilter&path=" + bi.path + "');return false|> " + this.Filters.ToList().Where(f => f.Value.Count() > 0).Count > 0 ? Xlt("Filtered", bi.buyerAccount.Language) : Xlt("Filter", bi.buyerAccount.Language) + "</div>", "|", Chr(34));
					// ML Removed <div class='clear'></div> as this was adding a huge space in scenaro, add quote click on earlier breadcrumb
					ButtonPanel.Controls.Add(lit);
				}
			}
			string pth = "";
			if (bi.PathLevel() < 6) {
				Dictionary<string, clsMatrixHeader> mh = iq.sesh(bi.lid, "matrixHeaders");
				foreach ( seg in Split(bi.path, ".")) {
					pth += seg;
					if (mh.ContainsKey(pth) && mh(pth).Filters != null) {
						foreach ( f in mh(pth).Filters) {
							object rf = string.Join("|", f.Value.Select(fil => string.Join("|", fil.Value.Select(fd => f.Key.ID.ToString() + "|" + fil.Key.Code + "|" + fd.ToString()))));
							if (f.Value.Count > 0)
								ButtonPanel.Controls.Add(NewLit("<div class=\"FilterButton\" title=\"Remove: " + string.Join(",", f.Value.Select(d => d.Key.DisplayText.text(English) + string.Join(",", d.Value))) + "\" onclick=\"" + bi.branch.ButtonScript("cmd=removeFilter&filterParams=" + rf + "&path=" + bi.path + "&into=" + bi.path + "&filterPath=" + pth) + "\"><span>" + iq.Branches(Split(pth, ".").Last()).DisplayName(English) + " - </span><span>" + f.Key.labelText.text(bi.agentAccount.Language) + "</span></div>"));
						}
						//d.Value.Select(Function(g) iq.Translations(CInt(g)).text(English))
						// If mh(pth).Vw IsNot Nothing Then
						// If mh(pth).Vw.RowFilter <> "" Then
						// FiltersUI.Controls.Add(NewLit("<span class=""FilterButton"" onclick=""" + bi.Branch.ButtonScript("cmd=removeFilters&path=" + bi.path & "&into=" + pth + "&filterPath=" & If(bi.RootPath Is Nothing, bi.path, bi.RootPath)) + """>Rack Mount</span>"))
						// End If
					}

					pth += ".";
				}
				//For Each f In Me.Filters
				// FiltersUI.Controls.Add(NewLit("<span class=""FilterButton"" onclick=""" + bi.Branch.ButtonScript("cmd=removeFilters&path=" + bi.path & "&into=" + bi.path + "&filterPath=" & If(bi.RootPath Is Nothing, bi.path, bi.RootPath)) + """>Rack Mount</span>"))
				// Next
			}
		}
		FiltersUI.Controls.Add(ButtonPanel);

		if (this.QuickFiltersVisible) {
			this.EnableQuickFilters(bi, errorMessages);

			//Dim tKeyDic As Dictionary(Of clsField, List(Of clsTranslation)) = Me.quickfilterTextKeys  

			clsFilter EQfilter = iq.i_Filters_Code("EQ");

			Dictionary<Panel, int> FilterControlArray = new Dictionary<Panel, int>();
			Dictionary<string, Panel> dicPanels = new Dictionary<string, Panel>();
			Panel pnl;

			foreach ( fld in this.FieldResultSet.Keys) {

				if (fld.QuickFilterGroup != null) {
					if (!dicPanels.ContainsKey(fld.QuickFilterGroup.text(English))) {
						pnl = new Panel();
						pnl.CssClass = "quickFilterGroupPanel";
						dicPanels.Add(fld.QuickFilterGroup.text(English), pnl);
						//Dim title As Label = New Label
						//title.Text = fld.QuickFilterGroup.text(bi.AgentAccount.Language)
						pnl.Controls.Add(NewLit("<span class='quickFilterGroupTitle'>" + fld.QuickFilterGroup.text(bi.agentAccount.Language) + "</span>"));
						FilterControlArray.Add(pnl, fld.QuickFilterGroup.Order);
					}
					pnl = dicPanels(fld.QuickFilterGroup.text(English));

					Panel ip = new Panel();
					//inner panel (one for each UI element in the group - tend to be arranged in a column)
					pnl.Controls.Add(ip);

					switch (fld.QuickFilterUItype) {
						case "CHECK":
							//the field must evaluate to a productattribute - checking the box will fitler (by productattribute) to those with true (1) values

							//check the boxes to match the current filters
							bool value = false;
							if (Filters != null) {
								if (Filters.ContainsKey(fld)) {
									clsFilter eq = iq.i_Filters_Code("EQ");
									//EQ is the Equals filter 
									if (Filters(fld).ContainsKey(eq)) {
										if (Filters(fld)(eq).Contains(1)) {
											value = true;
										}
									}
								}
							}

							Literal chkBox = new Literal();
							//You *cant* use actual checkboxes - don't try - .NET has a nasty habbit of wrapping them in spans - and then attatching any script to the span (not the checkbox) - literals are the answer.
							object occ;
							//EQ id the code for the 'Equals" filter (we look for Non-zero values)
							occ = "filterfield=" + fld.ID + ";DisableElementsByClassName('FF');if(this.checked){getBranches('cmd=changeFilter&path=" + this.path + "&filterParams=" + fld.ID + "|EQ|1')}else{getBranches('cmd=changeFilter&path=" + path + "&filterParams=" + fld.ID + "|EQ|1')};";

							int count = 0;
							if (dicNums.ContainsKey(fld)) {
								if (dicNums(fld).ContainsKey(1)) {
									count = dicNums(fld)(1);
									//1 is the 'true' value - and the key to the count
								}
							}

							chkBox.Text = "<input class='FF' type='checkbox' " + (string)IIf(value, "checked", "") + " " + (string)IIf(count == 0, "disabled='disabled'", "") + "  onclick=" + Chr(34) + occ + Chr(34) + "/>";

							//The quickfilterUI Interacts with the filters

							ip.Controls.Add(chkBox);

							Label lbl = new Label();
							lbl.Text = fld.labelText.text(bi.agentAccount.Language);
							object dvs = dicNums(fld).Count;
							lbl.Text += "(" + count + ")";
							//add the count of True values (non true values are not present)

							lbl.CssClass = "quickFilterLabel";
							if (count == 0){lbl.CssClass += " disabled";lbl.ToolTip = Xlt("This option isn't availble (in combination with your other selections)", bi.buyerAccount.Language);}

							ip.Controls.Add(lbl);
						case "BANDS":
						case "NUMBANDS":
							//break numerics/prices into bands

							if (dicBands.ContainsKey(fld)) {

								foreach ( band in dicBands(fld)) {
									Literal chkBox = new Literal();
									object occ;

									//NB this ChangeFilter chanegs/remove TWO filters at once
									occ = "filterfield=" + fld.ID + ";DisableElementsByClassName('FF');if(this.checked){getBranches('cmd=changeFilter&path=" + this.path + "&filterParams=" + fld.ID.ToString + "|GE|" + band.min + "|LE|" + band.max + "')}else{getBranches('cmd=changeFilter&path=" + this.path + "&filterParams=" + fld.ID + "|GE|" + band.min + "|LE|" + band.max + "')};";

									bool selected = band.isSelected(fld, this.Filters);
									//Compare to the currently selected GE/LE filters on this field
									chkBox.Text = "<input class='FF' type='checkbox' " + (string)IIf(selected, "checked='checked'", "") + " " + (string)IIf(band.Survivors == 0, "disabled='disabled'", "") + "  onclick=" + Chr(34) + occ + Chr(34) + ">";
									if (fld.QuickFilterUItype == "NUMBANDS") {
										//Numeric bands (no currency symbol) and NOT * 100
										chkBox.Text += ((decimal)band.min).ToString;
										chkBox.Text += " - ";
										chkBox.Text += ((decimal)band.max).ToString;
									} else {
										//normal' PRICE bands
										chkBox.Text += bi.buyerAccount.Currency.format((decimal)band.min / 100, bi.buyerAccount.BuyerChannel.Region.Culture, errorMessages, 0);
										chkBox.Text += " - ";
										chkBox.Text += bi.buyerAccount.Currency.format((decimal)band.max / 100, bi.buyerAccount.BuyerChannel.Region.Culture, errorMessages, 0);
									}

									chkBox.Text += "&nbsp;(" + band.Survivors + ")";
									//dicBands(fld)(band)

									chkBox.Text += "</input><br/>";
									ip.Controls.Add(chkBox);

								}

							}

						// If Me.Filters.ContainsKey(fld) Then
						//If Me.Filters(fld).Count > 0 Then
						//ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) 'ML - no longer needed now we have check boxes rather than options
						// End If
						//End If

						case "TKEY":
							//this is a single field containing one of a set of transltions
							//they currently present as radio buttons - but would be easy enough to switch to a set of checkboxes giving 'OR' functionality

							object nm = "f_" + fld.ID + "." + bi.path;
							//it SHOULD always be there - but if you've put somehting stupid in the screens flds propertyname - it wont be
							if (dicTrans.ContainsKey(fld)) {
								foreach ( t in dicTrans(fld).Keys) {
									if (t != null) {
										Int64 sv = t.SortValue(bi.agentAccount.Language);
										Literal rb = new Literal();
										rb.Text = "<input class=~FF~ type=~checkbox~ name=~" + nm + "~";
										rb.Text += " onclick=~{DisableElementsByClassName('FF');getBranches('path=" + this.path + "&cmd=changeFilter&filterParams=" + fld.ID + "|EQ|" + sv.ToString + "')}~";
										if (Filters.ContainsKey(fld)) {
											foreach ( activefilter in Filters(fld)) {
												//The quickFilters do a strict equals
												if (object.ReferenceEquals(activefilter.Key, EQfilter)) {
													if (activefilter.Value.Contains(t.SortValue(bi.agentAccount.Language))) {
														rb.Text += " CHECKED=~CHECKED~";
														break; // TODO: might not be correct. Was : Exit For
													}
												}
											}
										}

										//There are no survivors
										if (dicTrans(fld)(t) == 0) {
											rb.Text += " disabled=~disabled~";
										}

										rb.Text += ">" + t.text(bi.agentAccount.Language) + " (" + dicTrans(fld)(t) + ")</input><br/>";
										rb.Text = rb.Text.Replace("~", Chr(34));

										ip.Controls.Add(rb);
									}
								}
							}


							if (this.Filters.ContainsKey(fld)) {
								if (this.Filters(fld).Count > 0) {
									//ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) ' ML - No longer needed now we have check boxes rather than options
								}

							}
						case "NUMS":

							object nm = "f_" + fld.ID + "." + bi.path;
							if (dicNums.Keys.Count == 0) {
								errorMessages.Add("DicNums was not populated");
							} else {
								if (!dicUnits.ContainsKey(fld)) {
									errorMessages.Add("no units present for values in " + fld.propertyName);
								} else {
									//distinct Values of the number
									foreach ( v in (from j in dicNums(fld).Keysorderby j)) {
										Literal rb = new Literal();
										rb.Text = "<input class=~FF~ type=~checkbox~ name=~" + nm + "~";
										rb.Text += " onclick=~{DisableElementsByClassName('FF');getBranches('path=" + this.path + "&cmd=changeFilter&filterParams=" + fld.ID + "|EQ|" + v.ToString + "')}~";
										if (Filters.ContainsKey(fld)) {
											foreach ( activefilter in Filters(fld)) {
												if (object.ReferenceEquals(activefilter.Key, EQfilter)) {
													if (activefilter.Value.Contains(v)) {
														rb.Text += " CHECKED=~CHECKED~";
														break; // TODO: might not be correct. Was : Exit For
													}
												}
											}
										}

										//There are no survivors
										if (dicNums(fld)(v) == 0) {
											rb.Text += " disabled=~disabled~";
										}

										rb.Text += ">" + v.ToString + " " + dicUnits(fld).Symbol + " (" + dicNums(fld)(v) + ")</input><br/>";
										rb.Text = rb.Text.Replace("~", Chr(34));
										ip.Controls.Add(rb);
									}

									if (this.Filters.ContainsKey(fld)) {
										if (this.Filters(fld).Keys.Count > 0) {
											//no preference
											//ip.Controls.Add(fld.NoPreferenceRadioButton(path, Me.Filters(fld).Keys.ToList)) 'ML - No longer needed now we have check boxes instead of options 
										}
									}
								}
							}
						default:

							errorMessages.Add("Unrecognised quickFilterUIType:'" + fld.QuickFilterUItype + "'");
					}

					foreach (Panel p in FilterControlArray.OrderBy(a => a.Value).Select(a => a.Key)) {
						FiltersUI.Controls.Add(p);
					}
				}
			}

			//Else
		}
		//Quick filters


		//  End If

		//End If



	}

	/// <summary>
	/// Scans the 'surviving' (filtered) rows - to enable ony radiobuttons/checkboxes with suriving options
	/// </summary>
	/// <remarks></remarks>
	// As List(Of clsField)
	private void EnableQuickFilters(clsBranchInfo bi, ref List<string> errorMessages)
	{

		//NOTE uses ME.

		Dictionary<clsBranch, clsVisibility> descendants = new Dictionary<clsBranch, clsVisibility>();
		//Of String, clsBranch)
		int pruned = 0;

		//If pbi.RenderChildrenAs = bt.matrixRows Then
		//Get the extended list - of ALL child products (recursing through the placeholder branches) - This isn't ALL descendants - becuase we only recurse down to the next product
		//The key to the dictionary is the (full) path .. the last segment thereof being the branch ID

		clsBranch branch = iq.Branches((int)Split(this.path, ".").Last);


		Int64 numericvalue;
		//Object
		clsTranslation translation = null;

		Dictionary<clsFilter, List<Int64>> holdIt = null;

		foreach ( fld in this.EffectiveFields) {
			holdIt = null;
			if (this.Filters != null) {
				if (this.Filters.ContainsKey(fld)) {
					//Change the filter on the view (for each quickfilter) to drop out this field - so that fields (for example price bands are not 'self-excluding')
					holdIt = this.Filters(fld);
					this.Filters.Remove(fld);
				}
			}

			Vw.RowFilter = this.ConstructFilter;
			//(Me.matrix, Me.Filters)

			//ZERO (but do not remove) the counts

			switch (fld.QuickFilterUItype) {
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"BANDS":
				case "NUMBANDS":
					if (!dicBands.ContainsKey(fld)) {
						errorMessages.Add("Band was not intialised for " + fld.propertyName + " No values ?");
					} else {
						foreach ( band in dicBands(fld)) {
							band.Survivors = 0;
						}

					}
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"TKEY":
					// If dicTrans(fld).Keys.Count = 0 Then errorMessages.Add("TKEY dictionary was not initialised for " & fld.propertyName) - this will happen if there's no data in the column
					//vegas - remove could mask problems
					if (dicTrans.ContainsKey(fld)) {
						//This tolist IS VITAL - as it you iterate over the keys themselves (rather than a copy thereof) you get a 'collection cannot be modified' error
						foreach ( k in dicTrans(fld).Keys.ToList) {
							dicTrans(fld)(k) = 0;
						}

					}
				case  // ERROR: Case labels with binary operators are unsupported : Equality
"NUMS":
				case "CHECK":

					//If dicNums(fld).Keys.Count = 0 Then errorMessages.Add("NUMS dictionary was not initialised for " & fld.propertyName)
					//vegas
					if (dicNums.ContainsKey(fld)) {

						//dicnums contains, an entry (a count) for every distinct value of a field - for example - for the 'Weight' field - i might contain 1KG (5), 2KG (10)
						//the Distinct values are the KEYS in the outer dictionary, the VALUES in the dictionary are the COUNT of 'survivors' 'HAVING' that attribute.
						//in the case of 'check' fields - there is ONLY ONE distinct value ('true')
						foreach ( k in dicNums(fld).Keys.ToList) {
							dicNums(fld)(k) = 0;
						}

					}
			}

			if (!DT.Columns.Contains(fld.propertyName)) {
			// errorMessages.Add("Invalid column specified:'" & fld.propertyName & "' (Check cAsE)")

			} else {

				//This view is a (filtered) subset of the datatable (which is a cache of all the nuericvalues of all the fields)
				for (i = 0; i <= Vw.Count - 1; i++) {


					if (fld.QuickFilterUItype != "") {
						if ((Vw.Item(i)(fld.propertyName)) is DBNull) {
							numericvalue = Int64.MaxValue;
						} else {
							numericvalue = (Int64)Vw.Item(i)(fld.propertyName);
							//.DataView.Item()
						}


						// If LCase(fld.propertyName) = "product.i_attributes_code(tch)(0)" Then Stop

						switch (fld.QuickFilterUItype) {

							case  // ERROR: Case labels with binary operators are unsupported : Equality
"CHECK":

								//any NON minvalue (ie, present) value qwill so and is stored under the value 1
								//int64.minvalue is the default numeric value and means its 'not there'
								if ((Int64)numericvalue != Int64.MinValue) {

									if (!dicNums.ContainsKey(fld))
										dicNums.Add(fld, new Dictionary<long, int>());
									if (!dicNums(fld).ContainsKey(1))
										dicNums(fld).Add(1, 0);
									dicNums(fld)(1) += 1;
									//            Exit For 'once we've found a single value (in the current filtered view)  - we know this is a valid option - so this gives a big speedup

								}

							case  // ERROR: Case labels with binary operators are unsupported : Equality
"TKEY":

								//this is not very elegant - although it should be fast enough as its a small list (distinct trasnlations within 1 field)
								bool found = false;
								//vegas - remove - could mask problems
								if (dicTrans.ContainsKey(fld)) {
									foreach ( t in dicTrans(fld).Keys.ToList) {
										if (t.SortValue(bi.agentAccount.Language) == numericvalue) {
											dicTrans(fld)(t) += 1;
											found = true;
											break; // TODO: might not be correct. Was : Exit For
										}
									}
									if (dicTrans(fld).Count > 0) {
										// If Not found Then errorMessages.Add("TKEY not found for " & fld.propertyName)
									}

								}
							case  // ERROR: Case labels with binary operators are unsupported : Equality
"BANDS":
							case "NUMBANDS":

								if (this.dicBands.ContainsKey(fld)) {
									//this is a list of bands for this field
									foreach ( band in this.dicBands(fld)) {
										if (band.contains(numericvalue))
											band.Survivors += 1;
									}

								}

							case  // ERROR: Case labels with binary operators are unsupported : Equality
"NUMS":
								Dictionary<Int64, int> flddic = dicNums(fld);
								//UGLY - deals with missing values - eg a machine with no figure for installed mem
								if (flddic.ContainsKey(numericvalue)) {
									flddic(numericvalue) += 1;
									//increment the count of occurances of this NumericValue

								}

							case  // ERROR: Case labels with binary operators are unsupported : Equality
"":
							default:
								errorMessages.Add("EnableQuickFilters - Unknown quickFilterUI type" + fld.QuickFilterUItype);
						}
					}
				}

			}

			if (holdIt != null) {
				this.Filters.Add(fld, holdIt);
				//Put back the filter for this column
			}

		}

		bi.survivors = Vw.Count;

	}

	public PlaceHolder UI(clsBranchInfo bi, ref List<string> errormessages, UInt64 lid)
	{

		this.path = bi.path;
		this.matrix = MatrixAbove(this.path);
		//NOTE: this is the ONLY place we find out what TYPE of matrixHeader we need to instance


		//Returns the user facing UI for filtering and sorting this matirx
		UI = new PlaceHolder();
		if (AccountHasRight(lid, "DIAGVIEW"))
			UI.Controls.Add(NewLit(this.matrix.ID));

		object b = this.FieldResultSet.ToList().Where(f => f.Key.QuickFilterGroup != null);
		// remove blank filters
		if (this.FieldResultSet.ToList().Where(f => f.Key.QuickFilterGroup != null).Count() > 0) {

			UI.Controls.Add(this.FiltersUI(bi, errormessages));
		}

		Panel mh = new Panel();
		mh.CssClass = "matrixHeader";
		UI.Controls.Add(mh);
		//MH is a div wich we put the diagonal lables (chart control) plus the filter/sort UI - so it all aligns



		// mh.Controls.Add(pnlHeadSquares)

		OutputErrors(mh.Controls, errormessages, lid, true);

		if (clsBranchState.getbranchstate(bi.lid, this.path).rca == enumBt.gridrow) {
			clsAccount bAccount = iq.sesh(lid, "BuyerAccount");
			if (AccountHasRight(lid, "GLOBALADM"))
				mh.Controls.Add(NewLit(string.Format("<button class=\"CustomizeColumns hmc hpBlueButton ib showHMC\" onclick=\"$('#FieldFilter').draggable();$('#FieldFilter').show('slide right');$.ajax({{ url: '../Data/GetAvailableFields', data: JSON.stringify({{lid:'{0}', BranchPath:'{1}'}}),type: 'POST', contentType: 'application/JSON',success: GetAvailableFields_Success }});return false;\">" + Xlt("Customise Columns", bi.buyerAccount.Language) + "</button>", bi.lid, this.path)));
			//simple screen for OL3, basically a list
			if (bi.branch.Translation.Group == "OL3") {
				//mh.Controls.Add(Me.matrix.MatrixHeaders(Me))  'renders the header with diagonal lables
				mh.Controls.Add(this.RemoveFiltersUI(bi.buyerAccount.Language));
				//.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate
				return;
			}


			mh.Controls.Add(this.SortDropDownsUI(bi.agentAccount.Language));
			//DropDowns
			mh.Controls.Add(this.matrix.MatrixHeaders(this, bi.agentAccount.Language));
			//renders the header with diagonal lables
			Literal blit = new Literal();


			mh.Controls.Add(this.ExpandCollapseColumnButtons(bi.agentAccount.Language));
			mh.Controls.Add(this.RemoveFiltersUI(bi.buyerAccount.Language));
			//.MatrixPath, bi.lid, bi.AgentAccount.Language)) ' todo - reinstate

			mh.Controls.Add(this.SortDirectionsUI(bi.buyerAccount.Language));
			//row of arrows

			blit.Text = "<div style='height:1px;clear:both;'>&nbsp;</div>";
			UI.Controls.Add(blit);

			if (bi.survivors > 0) {
				//Number matching (current filters)
				Label lblmatches;
				lblmatches = new Label();
				lblmatches.Text = bi.survivors.ToString;
				lblmatches.Attributes("class") = "matchingLabel";

				UI.Controls.Add(lblmatches);
				// a refrence to this is returned - so it can be populated later (becuase we render the headers early on - before we have fetched the data, so we don't have a count yet)

			}
		}



	}

	private Panel RemoveFiltersUI(clsLanguage language)
	{

		//returns a panel containing *customer facing*  UI for filtering (NOT for editor)

		Panel pnl = new Panel();
		object occ;
		Panel col;

		// pnl.Controls.Add(ContrastSpacer) 'make room for the 'contrast' checkboxes

		pnl.CssClass = "oneRow";
		pnl.Attributes("style") = "display:inline-block;";

		pnl.Controls.Add(NewLit("<div class='LeftPad' style='width:4.25em;display:inline-block;'>&nbsp;</div>"));
		//for each column in the matrix we can have a value for each filter - so we can say >5 AND <10
		bool AreSome = false;

		//iterate the fields in order of their Order property
		foreach ( fld in from v in this.EffectiveFields) {
			// If fld.visibleList Then

			col = fld.emptyCell(this.ColIsCollapsed(fld), false);
			//   col.CssClass = "removeFilter" 'get rid of the matrixCell class

			pnl.Controls.Add(col);

			//    Dim ib As ImageButton
			if (!this.Filters == null) {
				if (this.Filters.ContainsKey(fld)) {
					foreach ( flt in this.Filters(fld).Keys) {
						//ib = New ImageButton
						//ib.ImageUrl = "/images/navigation/close.png"

						//Special case for a greater than and less than
						if (flt.Code == "LE" && this.Filters(fld).ToList().Where(f => f.Key.Code == "GE").Count() > 0)
							continue;

						Panel rb = new Panel();
						rb.CssClass = "removeButton";
						rb.ToolTip = Xlt("Remove the filter:- ", language) + fld.labelText.text(language) + " " + flt.DisplayText.text(language) + " " + string.Join(",", this.Filters(fld)(flt));
						//ML - todo , add all values to this tip
						occ = "getBranches('path=" + this.path + "&cmd=removeFilter&filterPath=" + this.path + "&filterParams=" + Trim(fld.ID.ToString) + "|" + flt.Code + "');return false;";

						AreSome = true;
						rb.Attributes("onclick") = occ;

						col.Controls.Add(rb);
						col.Width = new Unit(this.FieldResultSet(fld).GrownWidth, UnitType.Em);
					}
				}
			}
			//  End If
		}

		// pnl.CssClass = "filtersUI"

		if (AreSome)
			pnl.Attributes("style") += "height:" + collapsedColumnWidth + "em;";

		return pnl;

	}


	public bool ColIsCollapsed(clsField fld)
	{

		//If Me.ColState IsNot Nothing Then
		if (this.ColState.ContainsKey(fld)) {
			if (this.ColState(fld) == enumColState.SoftCollapsed | this.ColState(fld) == enumColState.HardCollapsed)
				ColIsCollapsed = true;
		}
		//End If
	}


	public Panel SortDirectionsUI(clsLanguage language)
	{

		//formerly sortUI - now just provided the direction arrows

		SortDirectionsUI = new Panel();
		SortDirectionsUI.ID = "sortDirections." + this.path;

		//Dim nl As Literal = New Literal
		//nl.Text = "<br/>"
		//SortsUI.Controls.Add(nl)
		SortDirectionsUI.Controls.Add(NewLit("<div class='LeftPad' style='width:4.25em;display:inline-block;'>&nbsp;</div>"));
		Panel col;

		//SortsUI.Controls.Add(ContrastSpacer)

		SortDirectionsUI.CssClass = "sortsRow";


		Literal btnReSort = new Literal();

		//iterate the fields in order of their Order property
		foreach ( f in from v in this.EffectiveFields) {

			//   If f.visibleList Then
			col = f.emptyCell(this.ColIsCollapsed(f), this.FieldResultSet(f).GrownWidth, false);
			col.ID = path + ".F" + f.ID;

			IEnumerable<clsPriorityDirection> j = null;
			j = from v in this.sorts.Valueswhere object.ReferenceEquals(v.column, f);
			//Pull out the sort info pertaining to this column

			if (j.Any) {
				clsPriorityDirection pd = j.First;

				//Dim currentValue As String = "-"
				//ptb = f.SortPriorityTextBox(Me.sorts$, currentValue)
				col.Controls.Add(pd.UI(path, language));

				//occ$ = "sortPath='" & path$ & "';sortFieldID=" & f.ID & ";showSortPriorityPicker('" & col.ID & "','" & currentValue & "');"

				// ptb.Attributes("onfocus") = occ$
				// ptb.ToolTip = "Set the priority and direction of this sorting for this column"
				// col.Controls.Add(ptb)


				if (LCase(f.propertyName) == "stock" | LCase(f.propertyName) == "customerprice") {
					//todo - check if we're actually sorting by either of these !

					//If InStr(Me.sorts$, f.propertyName) > 0 Then  ' a little crude - but good enough
					btnReSort = new Literal();
					btnReSort.Text = "<div ID=|resort." + path + f.propertyName + "| class=|re-sort| style=|display:none| onmousedown=|getBranches('path=" + path + "&cmd=invalidate');return false;|" + "> &nbsp;</div>";
					btnReSort.Text = Replace(btnReSort.Text, "|", Chr(34));

					//btnReSort.ToolTip = "Click to re-sort based on updated stock/price)"
					//btnReSort.CssClass = "re-sort"
					//btnReSort.Attributes("style") = "display:none;"  'initially hidden - shown by js fillPrices()
					//btnReSort.OnClientClick = occ$
					//btnReSort.ID = "resort." & path$ & f.propertyName 'the need *an* ID (to be made visible - becuase the JS fethces them by class but show()s them by ID)
					col.Controls.Add(btnReSort);
					//End If
				}
			}

			SortDirectionsUI.Controls.Add(col);
			//    End If
		}
		// SortsUI.Style("clear") = "both"

	}


	public Panel SortDropDownsUI(clsLanguage language)
	{

		//returns a set of dropdowns, one for each sort order in force - along with their remove buttons, and a single 'add sort' dropdown (cotaining other available columns to sort by)

		Panel ui = new Panel();
		ui.ID = "sorts." + this.path;
		ui.CssClass = "sortsDropDowns";

		object sortOrders = from v in this.sorts.Valuesorderby v.Priority;

		foreach ( sortOrder in sortOrders) {
			//ui.Controls.Add(NewLit(sortOrder.Priority))
			ui.Controls.Add(this.MakeDDL(sortOrder.column, sortOrder.Direction.First, sortOrder.Priority, language));
		}

		if (sortOrders.Count < 2) {
			ui.Controls.Add(this.MakeDDL(null, "D", this.sorts.Count + 1, language));
			//this 'special case' version contains a 'Add another' sort
		}


		return ui;

	}

	/// <summary>
	/// Returns a panel containing the dropdown list and remove button for a single sort order (for this Grid)
	/// </summary>
	private Panel MakeDDL(clsField SelectedColumn, string direction, int priority, clsLanguage language)
	{
		//sortorder As clsMatrixHeader.clsPriorityDirection) As Panel

		MakeDDL = new Panel();

		DropDownList ddl = new DropDownList();
		MakeDDL.Controls.Add(ddl);
		ddl.ID = "Sort_" + this.path + priority;
		//the DDL needs a unique ID (note, many grides can be present in the made simultaeneously
		MakeDDL.CssClass = "aSortDropDown";

		if (SelectedColumn == null) {
			ListItem i = new ListItem(Xlt("Add a sort", language), "add");
			//You can't actually select this, so it's value is unimportant (The onchange will fire on this DDL, adding a new sort)
			ddl.Items.Add(i);
			ddl.SelectedValue = "add";
		}

		foreach ( f in this.EffectiveFields) {
			if (f.visibleList) {
				ListItem i = new ListItem(f.labelText.text(language), f.ID.ToString + "," + priority + direction);
				ddl.Items.Add(i);
				if (object.ReferenceEquals(f, SelectedColumn)) {
					ddl.SelectedValue = i.Value;
					//Select the right column in this dropdown (these is one dropdown for each active sort order)
				}
			}
			//UI.Attributes("onclick") = "getBranches('" & path & "', 'priority=" & Me.column.ID & "," & Me.Priority & "D');" ' + sortFieldID + ',' + v);"
		}

		ddl.Attributes("onchange") = "DisableElementsByClassName('FF');var ddl=document.getElementById('" + ddl.ID + "');var spd=ddl.options[ddl.selectedIndex].value;getBranches('path=" + this.path + "&cmd=sort&value='+spd);";

		if (SelectedColumn != null) {
			Panel killButton = new Panel();
			killButton.CssClass = "removeButton";
			Literal lit = new Literal();
			lit.Text = "&nbsp;";
			killButton.Controls.Add(lit);
			killButton.Attributes("onclick") = "DisableElementsByClassName('FF');getBranches('cmd=removeSort&path=" + this.path + "&priority=" + priority.ToString.Trim + "');";
			//Removesort works by the priority
			MakeDDL.Controls.Add(killButton);
		}

	}


	public void CollapseColumns(float emsAvailable, ref List<string> errormessages)
	{
		//Dynamically collapses the lowest priority columns in a matrix, based on the available width and which columns have been actively opened or closed

		//Dim ColState As Dictionary(Of clsField, enumColState) = Nothing
		//If iq.SeshContains(lid, "colstate." & path$) Then
		// ColState = iq.sesh(lid, "colstate." & path$)
		// End If

		//re-expand all soft collapsed columns (we will re-collapse as many as necessary  below)
		foreach ( k in ColState.Keys.ToList) {
			if (ColState(k) == enumColState.SoftCollapsed) {
				ColState(k) = enumColState.SoftExpanded;
			}
		}

		float w = 0;
		w = this.currentWidth(errormessages);
		// get the current width (all columns minus the hard collapsed ones)

		//Now collapse columns in descending order of priority until the total width is less than the space available
		int pass;
		bool done = false;
		//on the first pass we collapse the 'soft' columns - on the second.. second hard
		for (pass = 1; pass <= 2; pass++) {
			//iterate the fields in descending order of their priority - ie. knock out the highest numbers (least important columns) first - Priority '1' is the most important column
			foreach ( fld in from v in this.EffectiveFieldsorderby v.priority descending) {
				//If fld.visibleList Then
				if (w < emsAvailable)
					done = true;
				if (done)
					break; // TODO: might not be correct. Was : Exit For
				if ((ColState(fld) == enumColState.SoftExpanded & pass == 1) | (ColState(fld) == enumColState.HardExpanded & pass == 2)) {
					if (fld.priority != 1) {
						w = w - (FieldResultSet(fld).Width - collapsedColumnWidth);
						//this is what we 'gain' by collpasing this column
						this.ColState(fld) = enumColState.SoftCollapsed;
					}
				}
				// End If
			}
			if (done)
				break; // TODO: might not be correct. Was : Exit For
		}


		//Add some small ones back  (most important first)
		foreach ( fld in from v in this.EffectiveFieldsorderby v.priority) {
			// If fld.visibleList Then
			if ((ColState(fld) == enumColState.SoftCollapsed)) {
				if (w + FieldResultSet(fld).Width < emsAvailable) {
					this.ColState(fld) = enumColState.SoftExpanded;
					w += FieldResultSet(fld).Width;
				}
			}
			// End If
		}

		//Mop up and grow
		object growField = this.EffectiveFields.Where(ef => ef.Grow).FirstOrDefault();
		if (growField != null && emsAvailable > w && this.ColState(growField) != enumColState.SoftCollapsed && (emsAvailable - w) > this.FieldResultSet(growField).Width) {
			//growField.width = emsAvailable - w
			this.FieldResultSet(growField).GrownWidth = emsAvailable - w;
			w = emsAvailable;
		} else {
			if (growField != null)
				this.FieldResultSet(growField).GrownWidth = null;
		}

	}

	private float currentWidth(ref List<string> errormessages)
	{

		//this is the total width of all the visible columns (taking in to account wether they're collapsed or not) 

		float w;
		w = 0;


		foreach ( fld in this.EffectiveFields) {
			// If fld.visibleList Then
			if (this.ColState.ContainsKey(fld)) {
				switch (this.ColState(fld)) {
					case enumColState.SoftCollapsed:
						this.ColState(fld) = enumColState.SoftExpanded;
						//we re-expand any soft collapsed column
						w = w + FieldResultSet(fld).Width;
					case enumColState.HardCollapsed:
						//this is a hard collapse (the user explicily collapsed the column)
						w = w + collapsedColumnWidth;
					case enumColState.SoftExpanded:
					case enumColState.HardExpanded:
						w = w + FieldResultSet(fld).Width;
					default:
						errormessages.Add("Unexpected column state");
				}
			} else {
				ColState.Add(fld, enumColState.SoftExpanded);
				//first pass - set all colups to soft expanded
				w = w + FieldResultSet(fld).Width;

			}
			// End If
		}

		return w;


	}

	public clsMatrixHeader Clone()
	{
		return (clsMatrixHeader)this.MemberwiseClone();
	}

	public void InvalidateFields()
	{
		_FieldResultSet = null;
	}

	public void SetDefaultFilterOn()
	{
		//Clear current filters
		Filters.Clear();

		//Fetch defaults
		foreach ( f in EffectiveFields) {
			if (f.DefaultFilterValues != null) {
				UpdateFilters(string.Format("{0}|{1}", f.ID, f.DefaultFilterValues));
			}
		}
	}


}
