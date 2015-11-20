//Option Strict On
using dataAccess;
using System.Runtime.Serialization;


public class clsField : i_Editable
{

	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public clsScreen Screen {
		get { return m_Screen; }
		set { m_Screen = Value; }
	}
	private clsScreen m_Screen;
	//The generic editor needs to 'see' this so it can populate it with its parent as a defualt.. otherwise, we cannot create instances of it
	public string propertyName {
		get { return m_propertyName; }
		set { m_propertyName = Value; }
	}
	private string m_propertyName;
	//which property does this field edit/display (on the instance of the object that its' screen edits)
	//Public Property PropertyClass As String 'what is the class of this property (for example clsUser)
	public string lookupOf {
		get { return m_lookupOf; }
		set { m_lookupOf = Value; }
	}
	private string m_lookupOf;
	// used for 1:1 relationships - the dictionary to look in - with optional (field=value) filter eg. threads.stautus might lookup staus(group=Threads)
	public clsTranslation labelText {
		get { return m_labelText; }
		set { m_labelText = Value; }
	}
	private clsTranslation m_labelText;
	public string helpText {
		get { return m_helpText; }
		set { m_helpText = Value; }
	}
	private string m_helpText;
	public clsValidation Validation {
		get { return m_Validation; }
		set { m_Validation = Value; }
	}
	private clsValidation m_Validation;
	public clsInputType InputType {
		get { return m_InputType; }
		set { m_InputType = Value; }
	}
	private clsInputType m_InputType;
	public int length {
		get { return m_length; }
		set { m_length = Value; }
	}
	private int m_length;
	// max character length
	public int order {
		get { return m_order; }
		set { m_order = Value; }
	}
	private int m_order;
	public float height {
		get { return m_height; }
		set { m_height = Value; }
	}
	private float m_height;
	//only applies in page/expanded mode
	public string defaultFilter {
		get { return m_defaultFilter; }
		set { m_defaultFilter = Value; }
	}
	private string m_defaultFilter;
	//carries a comma seperate list of the code of filtes which can be applied to this field
	public Dictionary<int, clsFilter> Filters;
	public string defaultSort {
		get { return m_defaultSort; }
		set { m_defaultSort = Value; }
	}
	private string m_defaultSort;

	//need to make lookup fields look in a specific place (not necessarily Hygn's root dictionaries)
	// Public Property EmbedScreen As clsScreen  'where a property is a list - defining a 1:M relationship .. manage the 'many' using this screen
	public float width {
		get { return m_width; }
		set { m_width = Value; }
	}
	private float m_width;
	//onscreen width in ems - only applies in list mode
	public string defaultValue {
		get { return m_defaultValue; }
		set { m_defaultValue = Value; }
	}
	private string m_defaultValue;
	public bool visibleList {
		get { return m_visibleList; }
		set { m_visibleList = Value; }
	}
	private bool m_visibleList;
	public bool visiblePage {
		get { return m_visiblePage; }
		set { m_visiblePage = Value; }
	}
	private bool m_visiblePage;
	public bool visibleSquare {
		get { return m_visibleSquare; }
		set { m_visibleSquare = Value; }
	}
	private bool m_visibleSquare;
	public bool Grow {
		get { return m_Grow; }
		set { m_Grow = Value; }
	}
	private bool m_Grow;
	public string DefaultFilterValues {
		get { return m_DefaultFilterValues; }
		set { m_DefaultFilterValues = Value; }
	}
	private string m_DefaultFilterValues;
	public bool FilterVisible {
		get { return m_FilterVisible; }
		set { m_FilterVisible = Value; }
	}
	private bool m_FilterVisible;

	public int priority {
		get { return m_priority; }
		set { m_priority = Value; }
	}
	private int m_priority;
	//How 'important' is this column - higher numbers are collapsed sooner

	//the 'quickfilter' is an optional, additional set of filtering UI which can appear on top of any matrix
	public clsTranslation QuickFilterGroup {
		get { return m_QuickFilterGroup; }
		set { m_QuickFilterGroup = Value; }
	}
	private clsTranslation m_QuickFilterGroup;
	//multiple fields are grouped to form a single set of radioButtons (or checkboxes) - this is both the title and the 'grouper'
	public string QuickFilterUItype {
		get { return m_QuickFilterUItype; }
		set { m_QuickFilterUItype = Value; }
	}
	private string m_QuickFilterUItype;
	//How to presents this fields quickfilter UI  Check,Radio, with/without  '
	public bool CanUserSelect {
		get { return m_CanUserSelect; }
		set { m_CanUserSelect = Value; }
	}
	private bool m_CanUserSelect;
	public int? LinkedFieldID {
		get { return m_LinkedFieldID; }
		set { m_LinkedFieldID = Value; }
	}
	private int? m_LinkedFieldID;
	public Dictionary<int, clsRegion> ValidRegions {
		get { return m_ValidRegions; }
		set { m_ValidRegions = Value; }
	}
	private Dictionary<int, clsRegion> m_ValidRegions;
	public bool HMC_MutuallyExclusive {
		get { return m_HMC_MutuallyExclusive; }
		set { m_HMC_MutuallyExclusive = Value; }
	}
	private bool m_HMC_MutuallyExclusive;
	public bool InvertFilterOrder {
		get { return m_InvertFilterOrder; }
		set { m_InvertFilterOrder = Value; }
	}
	private bool m_InvertFilterOrder;

	public string validationScript;

	string oPropertyName;

	public clsField()
	{
		propertyName = "";
		//PropertyClass = ""
		lookupOf = "";
		this.labelText = iq.AddTranslation("", English, "", 1, null, 0, false);
		this.helpText = "";
		this.priority = 1;

		oPropertyName = propertyName;

	}

	//Public Sub New(screen As clsScreen, ID As Integer, propertyname As String, lookupof As String, embedScreen As clsScreen, labeltext As String, helptext As String, validation As clsValidation, inputtype As clsInputType, length As Integer, order As Integer, width As Single, defaultvalue As String, visible As Boolean)
	//Public Sub New(screen As clsScreen, ID As Integer, propertyname As String, PropertyClass As String, lookupof As String, labeltext As String, helptext As String, validation As clsValidation, inputtype As clsInputType, length As Integer, order As Integer, width As Single, height As Single, defaultvalue As String, visibleList As Boolean, visiblePage As Boolean, defaultFilter As String, defaultSort As String)
	public clsField(clsScreen screen, int ID, string propertyname, string lookupof, clsTranslation labeltext, string helptext, clsValidation validation, clsInputType inputtype, int length, int order,
	float width, float height, string defaultvalue, bool visibleList, bool visiblePage, bool visibleSquare, string defaultFilter, string defaultSort, int priority, clsTranslation quickFilterGroup,

	string quickFilterUIType, bool canUserSelect, Nullable<int> LinkedFieldID, bool Grow, string DefaultFilterValues, bool FilterVisible, bool HMC_MutuallyExclusive, bool InvertFilterOrder)
	{
		screen.Fields.Add(ID, this);

		this.Screen = screen;
		this.ID = ID;
		this.propertyName = propertyname;
		//This is the property this field edits - it may be a simple Element, string, integer etc, 
		//                                another object (lookup of displayed as a dropdown list) .. or a dictionary - a collection of objects of another type
		//    Me.PropertyClass = PropertyClass  'what is the class of this property (for example clsUser)
		this.lookupOf = lookupof;
		//Me.EmbedScreen = embedScreen

		this.labelText = labeltext;
		this.helpText = helptext;
		this.Validation = validation;
		this.InputType = inputtype;
		this.length = length;
		this.order = order;
		this.width = width;
		this.height = height;
		this.defaultValue = defaultvalue;
		this.visibleList = visibleList;
		this.visiblePage = visiblePage;
		this.defaultFilter = defaultFilter;
		this.defaultSort = defaultSort;
		this.priority = priority;
		this.QuickFilterGroup = quickFilterGroup;
		this.QuickFilterUItype = quickFilterUIType;
		this.CanUserSelect = canUserSelect;
		this.visibleSquare = visibleSquare;
		this.LinkedFieldID = LinkedFieldID;
		this.Grow = Grow;
		this.DefaultFilterValues = DefaultFilterValues;
		this.ValidRegions = new Dictionary<int, clsRegion>();
		this.FilterVisible = FilterVisible;
		this.HMC_MutuallyExclusive = HMC_MutuallyExclusive;
		this.InvertFilterOrder = InvertFilterOrder;

		iq.Fields.Add(this.ID, this);
		if (!screen.i_field_property.ContainsKey(this.propertyName)) {
			screen.i_field_property.Add(this.propertyName, this);
		} else {
			ErrorLog.Add(new Exception("Screen " + screen.displayName(English) + " contains multiple references to " + this.propertyName));
		}

		oPropertyName = propertyname;

	}

	public Panel emptyCell(bool collapsed, bool editor)
	{
		return emptyCell(collapsed, null, editor);
	}
	public Panel emptyCell(bool collapsed, double? width, bool editor)
	{

		//the width of a cell is a bit of a red herring - 
		//becuase although the matrix cells are inline-block - they are positioned absolutely (to avoid wrapping)  - it would be nicer if they were in the flow (positioned relative)

		emptyCell = new Panel();

		if (editor) {
			emptyCell.CssClass = "editCell";
		} else {
			emptyCell.CssClass = "matrixCell";
		}

		float cellWidth = width == null ? this.width : width;

		if (collapsed)
			cellWidth = collapsedColumnWidth;
		emptyCell.Attributes("style") = "width:" + cellWidth + "em;";
		if (width == null ? this.width : width == 0) {
			emptyCell.Attributes("style") += "background-color:#ff9090;";
		}

		//we must put *something* in the DIV or .net won't render it
		//Dim lt As Literal
		//lt = New Literal
		//lt.Text = "&nbsp;"
		//emptyCell.Controls.Add(lt)

	}

	public Literal NoPreferenceRadioButton(string path, List<clsFilter> filters)
	{

		Literal rb = new Literal();
		rb.Text = "";

		if (filters.Count) {
			rb.Text = "<input type=~radio~ onclick=~{getBranches('path=" + path + "&cmd=removeFilter&filterParams=" + this.ID + "|";
			rb.Text += Join((from f in filtersf.Code).ToArray, "|") + "')}~>";
			rb.Text += "No preference</input>";
			rb.Text = rb.Text.Replace("~", Chr(34));

			return rb;
		}


	}


	public TextBox SortPriorityTextBox(sort, ref string Current)
	{
		//DropDownList

		//builds a standard sort priority DDL, and Selects the current value in it for field
		//NB: Sort$ (the current sort parameters)  is module level

		SortPriorityTextBox = new TextBox();
		//DropDownList
		SortPriorityTextBox.CssClass = "sortPriorityTextBox";

		//are we currently sorting by this column (at all)
		int c = 0;

		Current = "-";
		SortPriorityTextBox.Text = " -";
		foreach ( p in Split(sort, ",")) {
			c = c + 1;
			//this is the sort priorty 1,2,3,4 or 5  (where it appears in the request.sort parameter comma seperated list)
			if (InStr(p, "[" + this.propertyName + "]") > 0) {
				//yes we are sorting by this
				if (InStr(p, "] ASC") > 0) {
					//SortPriority.SelectedValue = Trim$(c.ToString) & "A"
					SortPriorityTextBox.Text = Trim(c.ToString) + "⇧";
					Current = Trim((string)c) + "A";
					//we're currently soring price this column with priority C, ascending
				} else {
					//SortPriority.SelectedValue = Trim$(c.ToString) & "D"
					SortPriorityTextBox.Text = Trim(c.ToString) + "⇩";
					Current = Trim((string)c) + "D";
					//we're currently soring price this column with priority C, descending
				}

			}
		}

	}

	public DropDownList OperatorDDL()
	{

		//Returns a dropdown list filled with  approriate set of logical operators for filtering based on this inputType
		//Seletcts the current value in the list based on whats in the filter$ 

		//See IQ.loadfilters for a better undrestanding of how the filters work
		//eg. filters.Add("PM20", "[col]>=[filterValue]*.8 and [col]<=[filterValue]*1.2")

		OperatorDDL = new DropDownList();



			// .Items.Add(New ListItem("Equals", "EQ"))

			//WATCH OUT WHEN USING ' (single quotes)   - you need to ESQ() them !!!

			//one's are treated as strings - but should ultimately become an 'is/in' filter

			//Note - the values (typed in the textboxes) get enclosed in single quotes
			//.Items.Add(New ListItem("Ends with", "EW"))
			//.Items.Add(New ListItem("Contains", "CN"))
			//.Items.Add(New ListItem("Only", "ONLY"))

			//for 'many' colums - we can only filter by the number of children presently)
			//again, ultimately a 'has' filter - would be nice
			//TODO with/without
			//TODO with/without


		 // ERROR: Not supported in C#: WithStatement


	}



	//Public Sub New(screen As clsScreen, propertyName As String, lookupOf As String, embedScreen As clsScreen, labelText As String, helpText As String, validation As clsValidation, inputType As clsInputType, length As Integer, order As Integer, width As Single, defaultvalue As String, visible As Boolean)

	//Public Sub New(screen As clsScreen, propertyName As String, propertyClass As String, lookupOf As String, labelText As String, helpText As String, validation As clsValidation, inputType As clsInputType, _
	//                           length As Integer, order As Integer, width As Single, height As Single, defaultValue As String, visibleList As Boolean, visiblePage As Boolean, defaultFilter$, defaultSort$)
	public clsField(clsScreen screen, string propertyName, string lookupOf, clsTranslation labelText, string helpText, clsValidation validation, clsInputType inputType, int length, int order, float width,
	float height, string defaultValue, bool visibleList, bool visiblePage, defaultFilter, defaultSort, int priority, clsTranslation quickFilterGroup, string quickFilterUIType, bool CanUserSelect,
	int? LinkedFieldID, bool FilterVisible)
	{
		object sql;

		//   sql$ = "INSERT INTO [field] (fk_screen_id,property,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],fk_screen_id_embed,[width],defaultValue,visible) "
		//   sql$ &= "VALUES (" & screen.ID & "," & da.SqlEncode(propertyName) & "," & da.SqlEncode(labelText) & "," & da.SqlEncode(helpText) & "," & NullID(validation) & ","
		//   sql$ &= SqlEncode(lookupOf) & "," & inputType.ID & "," & length & "," & order & "," & NullID(embedScreen) & "," & width & "," & da.SqlEncode(defaultvalue) & "," & IIf(visible, 1, 0) & ");"

		//    sql$ = "INSERT INTO [field] (fk_screen_id,property,propertyClass,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],[width],height,defaultValue,visibleList,VisiblePage,defaultfilter,defaultsort) "
		//    sql$ &= "VALUES (" & screen.ID & "," & da.SqlEncode(propertyName) & "," & da.SqlEncode(propertyClass) & "," & da.SqlEncode(labelText) & "," & da.SqlEncode(helpText) & "," & NullID(validation) & ","
		//    sql$ &= SqlEncode(lookupOf) & "," & inputType.ID & "," & length & "," & order & "," & width & "," & height & "," & da.SqlEncode(defaultValue) & "," & IIf(visibleList, "1", "0").ToString & "," & IIf(visiblePage, "1", "0").ToString & "," & da.SqlEncode(defaultFilter) & "," & da.SqlEncode(defaultSort) & ");"

		sql = "INSERT INTO [field] (fk_screen_id,property,fk_translation_key_label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],[width],height,defaultValue,visibleList,VisiblePage,defaultfilter,defaultsort,[priority],FK_Translation_key_WidgetGroup,WidgetUI,CanUserSelect,VisibleSquare,Grows,DefaultFilterValues) ";
		sql += "VALUES (" + screen.ID + "," + da.SqlEncode(propertyName) + "," + labelText.Key + "," + da.SqlEncode(helpText) + "," + NullID(validation) + ",";
		sql += da.SqlEncode(lookupOf) + "," + inputType.ID + "," + length + "," + order + "," + width + "," + height + "," + da.SqlEncode(defaultValue) + "," + IIf(visibleList, "1", "0").ToString + "," + IIf(visiblePage, "1", "0").ToString + ",";
		sql += da.SqlEncode(defaultFilter) + "," + da.SqlEncode(defaultSort) + "," + priority + "," + TranslationKey(quickFilterGroup) + "," + da.SqlEncode(quickFilterUIType) + "," + da.SqlEncode(CanUserSelect) + "," + da.SqlEncode(visibleSquare) + "," + da.SqlEncode(Grow) + "," + da.SqlEncode(DefaultFilterValues) + ");";

		this.ID = da.DBExecutesql(sql, true);
		screen.Fields.Add(this.ID, this);

		this.Screen = screen;
		this.propertyName = propertyName;
		//This is the property (on the object) this field edits (or displays) - it may be a simple Element, string, integer etc, 
		//                                another object (lookup of displayed as a dropdown list) .. or a dictionary - a collection of objects of another type
		//Me.PropertyClass = propertyClass 'what is the class of this property (for example clsUser)

		this.lookupOf = lookupOf;
		//Me.EmbedScreen = embedScreen
		this.labelText = labelText;
		this.helpText = helpText;
		this.Validation = validation;
		this.InputType = inputType;
		this.length = length;
		this.order = order;
		this.width = width;
		this.height = height;
		this.defaultValue = defaultValue;
		this.visibleList = visibleList;
		this.visiblePage = visiblePage;
		this.defaultFilter = defaultFilter;
		this.defaultSort = defaultSort;
		this.priority = priority;
		this.QuickFilterGroup = quickFilterGroup;
		this.QuickFilterUItype = quickFilterUIType;
		this.CanUserSelect = CanUserSelect;
		this.LinkedFieldID = LinkedFieldID;
		this.DefaultFilterValues = DefaultFilterValues;
		this.Grow = Grow;
		this.FilterVisible = FilterVisible;

		iq.Fields.Add(this.ID, this);
		if (this.propertyName != "") {
			this.Screen.i_field_property.Add(this.propertyName, this);
		}
		//Me.Screen.Fields.Add(Me.ID, Me)

		oPropertyName = propertyName;

	}

	public object i_Editable.Insert(ref List<string> errorMessages)
	{

		//Return New clsField(Me.Screen, Me.propertyName, Me.PropertyClass, Me.lookupOf, Me.labelText, Me.helpText, Me.Validation, Me.InputType, Me.length, Me.order, Me.width, Me.height, Me.defaultValue, Me.visibleList, Me.visiblePage, Me.defaultFilter, Me.defaultSort)
		return new clsField(this.Screen, this.propertyName, this.lookupOf, this.labelText, this.helpText, this.Validation, this.InputType, this.length, this.order, this.width,
		this.height, this.defaultValue, this.visibleList, this.visiblePage, this.defaultFilter, this.defaultSort, this.priority, this.QuickFilterGroup, this.QuickFilterUItype, this.CanUserSelect,
		this.LinkedFieldID, this.FilterVisible);

	}


	public void demote()
	{
	}

	/// <summary>Returns a singe field value as Text (quoted for 'strings') suitable for a CSV export</summary>

	public string CSV(object obj, string path, clsAccount buyerAccount, clsLanguage language, clsScreenHeader MatrixHeader, bool collapsed, HashSet<string> foci, ref List<string> errorMessages, UInt64 lid, double? width,
	bool export = false)
	{

		//this has all become a little messy - Cellvalue could do with a refactor and some of these wrappers could be removed 

		string result = string.Empty;
		Panel pnl = new Panel();
		//throwaway (in this case) panel for UI

		//this is populated                            \/ (byref)
		CellValue(obj, path, buyerAccount, language, result, pnl, collapsed, null, null, foci,
		errorMessages, lid, export);
		if (!string.IsNullOrWhiteSpace(result)) {
			result = result.Replace("&amp;", "&");
		}
		return result;

	}

	public Panel CellUI(object obj, path, clsAccount buyerAccount, clsLanguage language, clsScreenHeader MatrixHeader, bool collapsed, HashSet<string> foci, ref List<string> errorMessages, UInt64 lid, double? width)
	{

		CellUI = this.emptyCell(collapsed, width, false);
		//New Panel

		string numericValue;
		string csv;
		//throwaway (in this case) string for CSV export
		//                                                                  \/-  This gets POPULATED
		numericValue = CellValue(obj, path, buyerAccount, language, csv, CellUI, collapsed, null, null, foci,
		errorMessages, lid);

		//If numericValue IsNot Nothing Then value = numericValue Else value = "\'" & value & "\'" 'Escape the quotes we require for filtering against literal strings

		//rdus - replaces dows with underscores
		CellUI.ID = rdus(path) + "_" + this.ID;


		if (!collapsed) {
			List<string> fbids = new List<string>();
			//filter image button ID's
			if (this.defaultFilter != "") {
				foreach ( v in Split(this.defaultFilter, ",")) {
					fbids.Add("ctl00_FIB_" + v);
				}
			}
			//Dim omo$


			if (fbids.Count > 0) {
				//MatrixUI.Attributes("onmouseover") = omo$

				object omd;

				//TODO - replace whats below with this - (and a new JS function) omd$ = "showFilterButtons('" & bi.MatrixPath & "','" & Me.ID & "'," & numericValue.ToString & ",'" & Join(fbids.ToArray, ",") & "','" & MatrixUI.ID ');"

				//omd$ = "if(!onSpeechBubble){this.removeAttribute('Title');"
				//omd$ &= "filterPath='" & MatrixHeader.path & "';filterField=" & Me.ID & ";filterValue='" & numericValue.ToString & "';"
				//omd$ &= "showFilterButtons('" & Join(fbids.ToArray, ",") & "');display('ctl00_filterButtons','block');"
				//omd$ &= "$('#ctl00_filterButtons').position({my:'center top',at:'center bottom',of: '#" & CellUI.ID & "'})"
				//omd$ &= "};return false;"
				//      omd$ &= "var cellpos=$('#" & MatrixUI.ID & "').offset();$('#ctl00_filterButtons').css"

				//	<div id="target" onmousedown="$('#fbs').position({my:'center',at:'center',of:'#target'});"

				//MatrixUI.ToolTip = omd$
				CellUI.ToolTip = Xlt("Click to filter the list based on this value", language);
				CellUI.Attributes("onmousedown") = "arrowClick(onSpeechBubble,'" + MatrixHeader.path + "','" + this.ID + "','" + numericValue.ToString + "','" + Join(fbids.ToArray, ",") + "','" + CellUI.ID + "',this);";

				CellUI.Attributes("class") += "handPointer";

			}
		}

	}

	public object CellValue(object obj, string path, clsAccount buyerAccount, clsLanguage language, ref string csv, ref Panel UI, bool collapsedColumn, ref clsTranslation Translation, ref clsUnit Unit, HashSet<string> foci,
	ref List<string> errorMessages, UInt64 lid, bool export = false)
	{

		//in the customer facing UI the OBJ is a branch - but when used from the editor - 
		//it can be anything (an account, country, language etc,etc)

		//returns a piece of UI (label, claendar, checkbox, graph, textbox  etc) in a placeholder - and a numeric 'INT64' value for filtering and sorting

		csv = "Not Set";
		//should *always* get overrridden

		Translation = null;
		//For cells containing a translation - we return that too (for quickfilters)
		CellValue = Int64.MinValue;
		//Single.MinValue
		bool IsBoolean = false;
		string propName = this.propertyName;
		if (Len(this.propertyName) > 2 && Right(this.propertyName, 3) == "<B>") {
			//This is a boolean field, yes has content or no doesn't only
			IsBoolean = true;
			propName = Left(this.propertyName, Len(this.propertyName) - 3);
		}
		object valueObject = null;
		//If LCase(Me.propertyName).Contains("dmr") Then Stop

		if (UI != null) {
			UI.CssClass += " cls_" + this.labelText.text(English).Replace(" ", "_");
			//include the fields label text name as a CSS Class
		}

		if (SpecialColumn(this, propName, obj, path, buyerAccount, language, UI, CellValue, Translation, csv,
		Unit, foci, errorMessages, lid, valueObject, export)) {
			if (valueObject != null) {
				CellValue = valueObject;
			}
			//NB: CSV was set above (byRef) - and is CSV()'d already

			if (collapsedColumn & UI != null) {
				UI.Controls.Clear();
				Label lbl = new Label();
				lbl.Text = "-";
				UI.Controls.Add(lbl);
			}

		} else {
			//it WASN'T a 'special' column (price/stock/memory/display/supplychain)

			Label lbl;
			lbl = new Label();
			//            lbl.Style("width") = "100%"
			// lbl.Style("whitespace") = "nowrap"
			lbl.Style("overflow") = "hidden";
			lbl.Style("word-wrap") = "break-word";
			if (UI != null)
				UI.Controls.Add(lbl);

			object tobj;
			//the object at the end of the walk

			//Failing to get CP_DMR attribute
			tobj = Reflection.WalkPropertyValue(obj, propName, errorMessages);
			//this is probably slow .. TODO consider short term cacheing at this level  (or possible better within walkproperty .. a cache of paths to recently walked values would speed up rows beyond the visible matrix too


			if (tobj is string) {
				CellValue = tobj;
				csv = Utility.CSV(tobj);
				lbl.Text = tobj;
				lbl.Text = Replace(lbl.Text, " ", "&nbsp;");
				lbl.Text = Replace(lbl.Text, "-", "&#8209");


			} else if (tobj is int | tobj is float) {
				csv = (int)tobj;
				//.ToString("D") 'it's the SERVERS locale/regional settings here - so we shold get 'proper' decimal points and thousands sepearators

				CellValue = tobj;
				//CInt(Val(lbl.Text)) 'well, that was easy - strings probably need more careful hadling
				lbl.Text = Replace(lbl.Text, " ", "&nbsp;");
				lbl.Text = Replace(lbl.Text, "-", "&#8209");

			} else if (tobj is clsTranslation) {
				Translation = (clsTranslation)tobj;
				lbl.Text = Translation.text(language);
				csv = Translation.text(language);
				CellValue = Translation.SortValue(language);

				lbl.Text = Replace(lbl.Text, " ", "&nbsp;");
			//lbl.Text = Replace(lbl.Text, "-", "&#8209")

			//the thing we've walked to is an attribute - like Disk RPM  - Also used for some product attributes which are presented as ICONS
			} else if (tobj is clsProductAttribute) {

				clsProductAttribute prodatt;
				prodatt = (clsProductAttribute)tobj;


				//   If prodatt.Product.Attributes Then


				Translation = prodatt.Translation;
				Unit = prodatt.Unit;



				if (Translation != null) {
					//Any translation (and its ORDER will override any Numeric value (for the purposes of sorting and filtering)

					CellValue = Translation.SortValue(language);
					//some attributes (such as the RPM of an Drive have both text, and a numeric value)


					string text = Translation.text(language);
					csv = Utility.CSV(text);

					//values (productAttributes) to be displayed as sausage buttons have a translation which is their face text
					if (LCase(this.InputType.code) == "sausage") {

						CellValue = (int)prodatt.NumericValue;
						if (UI != null) {
							Panel wb = new Panel();
							wb.CssClass = "sausageButton";
							if (QuickFilterUItype != "TKEY" && prodatt.NumericValue == 0) {
								wb.CssClass += " greyed";
							}

							lbl = new Label();
							lbl.Text = text;
							lbl.ToolTip = prodatt.Attribute.displayName(buyerAccount.Language);
							//Use the full name of the productattribute as the tooltip

							wb.Controls.Add(lbl);
							UI.Controls.Add(wb);

							//                            If prodatt.NumericValue <> 0 Then Stop

							lbl.ToolTip += " (" + CellValue.ToString + ")";

						}
					} else {
						//this is a vanilla (non quickfilter UI)  ProducAttribute (WITH a translation) - we use its numericvalue if present
						lbl.Text = text;
						lbl.ToolTip = Translation.text(language);
						//If prodatt.NumericValue > 0 Then  'we want the translations sortvalue for product attributes with no numeric value '@@@
						// CellValue = CInt(prodatt.NumericValue)
						//End If
					}
				} else {
					//numeric attribute (one where there is no text/translation) and units (and possibly conversions)
					if (InputType.code == "rounddecimal") {
						CellValue = Math.Round((decimal)prodatt.NumericValue, 2);
					} else {
						CellValue = (int)prodatt.NumericValue;
					}

					// Handle sausages

					if (LCase(this.InputType.code) == "sausage") {
						if (UI != null) {
							Panel wb = new Panel();
							wb.CssClass = "sausageButton";
							if (QuickFilterUItype != "TKEY" && prodatt.NumericValue == 0) {
								wb.CssClass += " greyed";
							}

							lbl = new Label();
							lbl.Text = CellValue.ToString();
							lbl.ToolTip = prodatt.Attribute.displayName(buyerAccount.Language);
							//Use the full name of the productattribute as the tooltip

							wb.Controls.Add(lbl);
							UI.Controls.Add(wb);

							lbl.ToolTip += " (" + CellValue.ToString + ")";
						}

					} else {
						lbl.Text = CellValue.ToString;
					}

					csv = prodatt.NumericValue.ToString;
					//this is an integer (may need to change)

					if (prodatt.Unit.Code != "txt") {
						lbl.Text += " " + prodatt.Unit.Symbol;
						// todo Symbol
					}

					//Note - we don't need to check the 'value' of Icons - becuase just having it indicates a 'true'
					if (LCase(this.InputType.code) == "icon") {

						if (UI != null) {
							Image image = new Image();
							//We extract the code form the parentheses at the end of the propertname - to use as the image name
							image.ImageUrl = imagebase + "/images/icons/icon_opttype_" + DeBracket(propName, "(", ")")(0) + ".png";

							//the acutal value becomes a tooltip 
							if (lbl.Text == "-1")
								lbl.Text = Yes.text(language);
							//Quick fix 
							if (lbl.Text == "0")
								lbl.Text = No.text(language);

							image.ToolTip = lbl.Text;
							lbl.Text = "";
							UI.Controls.Add(image);
						}
					}
				}

			} else if (this.InputType.code == "one") {
				lbl.Text = tobj.displayname(buyerAccount.Language);
				csv = Utility.CSV(lbl.Text);
				CellValue = tobj.id;
				//the value is the ID of the object referenced (the foreign key) - used for filtering in the editor

			} else if (tobj == null) {
				csv = Utility.CSV("-");
				lbl.Text = "-";
				CellValue = Int64.MinValue;
				//Single.MinValue
			} else {
				errorMessages.Add("unknown object type " + tobj.GetType.ToString + "|" + path + "|" + propName);
			}
		}

		if (IsBoolean) {
			CellValue = CellValue == Int64.MinValue | CellValue == "0" ? 0 : 1;
			if (UI != null) {
				Literal lit = new Literal();
				Literal l = UI.Controls(0);
				//lit.Text = "<div>" + If(CellValue = Int64.MinValue Or CellValue = "0", _
				//                        No.text(language), _
				//                        Yes.text(language)) + "</div>"

				lit.Text = "<div>" + CellValue == Int64.MinValue | CellValue == "0" ? "<img src='images/navigation/cross.png'/>" : "<img src='images/navigation/cross.png'/>" + "</div>";


				csv = Utility.CSV(CellValue == Int64.MinValue | CellValue == "0" ? No.text(language) : Yes.text(language));

				if (UI != null) {
					UI.Controls.Clear();
					UI.Controls.Add(lit);
				}
			}
		}

		if (!UI == null) {
			if (collapsedColumn){UI.Controls.Clear();Label lbl = new Label();lbl.Text = "-";UI.Controls.Add(lbl);}
		}


	}


	public bool SpecialColumn(clsField f, string propName, object obj, path, clsAccount buyeraccount, clsLanguage language, ref Panel UI, ref Int64 Value, ref clsTranslation Translation, ref string csv,
	ref clsUnit unit, HashSet<string> foci, ref List<string> errormessages, UInt64 lid, ref object valueObject, bool export = false)
	{

		//value will *very often* be a string representation of a 64bit Number

		//Populates the UI and a numeric value for soring and filtering
		//returns TRUE if is *is* a 'special' Column

		Label lbl = new Label();
		lbl.Style("width") = "100%";
		lbl.Text = "-";

		string bp = Split(propName, "(")(0);
		// find the first segment before any open parethesis

		SpecialColumn = true;
		//The CASE ELSE sets this to FALSE for 'non-speicial' columns

		unit = null;

		csv = Utility.CSV("unhandled 'special'column " + f.propertyName);

		switch (Trim(LCase(bp))) {

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"slots":
				//broken by the new major/minor slot type

				//looks on child branches for slots 'gives' of the specified type (becuase the slots are in the chassis/mobo)

				//Returns a dictionary of the number of slots by major type - recursing form the current branch - does not cross systems
				Dictionary<string, int> majorSlots = new Dictionary<string, int>();
				((clsBranch)obj).MajorSlots(majorSlots);
				//Call the majorSlots (recursive) method on the branch object - to fill the dictionary

				string majorSlotType = DeBracket(propName, "(", ")")(0);
				//extract the slot type from the brackets eg slots(CPU)

				if (majorSlots.ContainsKey(majorSlotType)) {
					lbl.Text = (string)majorSlots(majorSlotType);
					if (UI != null)
						UI.Controls.Add(lbl);
					Value = majorSlots(majorSlotType).ToString;
					csv = Value;
				} else {
					if (UI != null)
						UI.Controls.Add(lbl);
					Value = Int64.MinValue.ToString;
					csv = Utility.CSV("-");
				}



				unit = iq.i_unit_code("num");
			//Dim found As Boolean = False
			//For Each cb In obj.childbranches.values

			//    Dim ck$ 'build a compound key - telling us we want the 'gives' slots - no slot number
			//    Dim parms As List(Of String) = DeBracket(f.propertyName, "(", ")") 'extract the slot type from the brackets eg slots(CPU)

			//    ck$ = parms(0) & "_" & path & "." & Trim$(cb.id) & "_1_null"

			//    If cb.i_Slots.ContainsKey(ck$) Then
			//        value = cb.i_Slots(ck$).numSlots
			//        lbl.Text = value.ToString
			//        If UI IsNot Nothing Then UI.Controls.Add(lbl)
			//        found = True
			//        Exit For
			//    End If

			//    ck$ = parms(0) & "__1_null"

			//    If cb.i_Slots.ContainsKey(ck$) Then
			//        value = cb.i_Slots(ck$).numSlots
			//        lbl.Text = value.ToString
			//        If UI IsNot Nothing Then UI.Controls.Add(lbl)
			//        found = True
			//        Exit For
			//    End If
			//Next

			//If Not found Then
			//    value = CInt(0)
			//    If UI IsNot Nothing Then
			//        lbl.Text = "-"
			//        UI.Controls.Add(lbl)
			//    End If
			//End If

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"xtext":
				//external text (formely TextExternal)

				clsProduct product = ((clsBranch)obj).Product;
				//value = 0 ' we must return *something* (even if there was no xtext) to indicate this was a specialcolumn

				csv = "";

				List<ClsValidationMessage> vmsgs = product.getXtext(path, null);
				if (vmsgs.Count == 0) {
					if (UI != null)
						UI.Controls.Add(lbl);
				} else {
					foreach ( msg in vmsgs) {
						if (UI != null)
							UI.Controls.Add(msg.CompactUI(language));
						if (msg.severity > Value) {
							Value = msg.severity;
							//Return the max severity from the set of Xtexts as the (sortable) value
						}
						csv += msg.message.text(language) + vbCrLf;
					}
				}

				csv = Utility.CSV(csv);

				// if we were going to sort xTexts by anything, it would be their Severity - not alphabetically
				Translation = null;

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"scheme":
				clsProduct product;
				product = ((clsBranch)obj).Product;

				string code = DeBracket(propName, "(", ")")(0);
				//extract the scheme code

				if (!iq.i_scheme_code.ContainsKey(code)) {
					if (errormessages.Count < 10) {
						errormessages.Add("Unknown loyalty scheme code " + code);
					}
				} else {
					foreach ( scheme in iq.i_scheme_code(code)) {
						if (scheme.Region.Encompasses(buyeraccount.SellerChannel.Region)) {
							if (product.Points.ContainsKey(scheme)) {
								Value = product.Points(scheme).ToString;
							} else {
								Value = "0";
							}

							if (!UI == null) {
								if ((int)Value > 0) {
									Literal lit = new Literal();
									lit.Text = "<div class='loyaltyPoints'>" + Value.ToString + "</div>";
									UI.Controls.Add(lit);
								}
							}
						}
					}
				}

				if (Value == Int64.MinValue) {
					csv = 0;
				} else {
					csv = Value.ToString;
				}



				Translation = null;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"promo":
			case "promos":

				clsProduct product;
				clsBranch branch = (clsBranch)obj;
				product = ((clsBranch)obj).Product;

				string code = DeBracket(propName, "(", ")")(0);
				//extract the scheme code

				csv = "";

				switch ((code)) {

					case  // ERROR: Case labels with binary operators are unsupported : Equality
"A":
						if (branch.hasAvalanche(buyeraccount.BuyerChannel, path)) {
							Value = "1";
							Literal lit = new Literal();
							lit.Text = "<div class='avalancheGrid'>AV</div>";
							if (UI != null) {
								UI.Controls.Add(lit);
							}
							Translation = iq.AddTranslation("Avalanche", English, "Promos", 0, null, 0, false);
							csv = Utility.CSV(code);

						}

					case  // ERROR: Case labels with binary operators are unsupported : Equality
"F":

						if (branch.hasFlexAttach(buyeraccount, path, foci, errormessages)) {
							Value = "1";
							Literal lit = new Literal();
							lit.Text = "<div class='flexF'>F</div>";
							if (UI != null) {
								UI.Controls.Add(lit);
							}
							Translation = iq.AddTranslation("Flex Attach", English, "Promos", 0, null, 0, false);
							csv = Utility.CSV(code);

						}

					case  // ERROR: Case labels with binary operators are unsupported : Equality
"B":
					case  // ERROR: Case labels with binary operators are unsupported : Equality
"R":
						if (product.hasPromo("R", buyeraccount.BuyerChannel.Region)) {
							Value = "1";
							Literal lit = new Literal();
							lit.Text = "<div class='recetaR'><span>&#x2713;</span></div>";
							if (UI != null) {
								UI.Controls.Add(lit);
							}
							unit = iq.i_unit_code("txt");
							Translation = iq.AddTranslation("Receta", English, "Promos", 0, null, 0, false);
							csv = Utility.CSV(code);
						}

					default:

				}

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"customerprice":

				clsProduct product;
				product = ((clsBranch)obj).Product;

				if (product == null)
					return null;
				//product.i_Attributes_Code.ContainsKey("MfrSKU") Then
				if (product.SKU != "") {

					List<clsPrice> prices;
					if (product.SKU.StartsWith("###") | product.SKU.ToUpper.StartsWith("FAKE")) {
						prices = new List<clsPrice>();
						//no prices for fake products (especially DONT call the webservice!)
					} else {
						//Dim withoutWebService As Integer = buyeraccount.SellerChannel.priceConfig And Not 8
						prices = product.GetPrices(buyeraccount, buyeraccount.SellerChannel.priceConfig, iq.AllVariants, errormessages, true);
						//this can return multiple prices (for multiple variants - different warehouses, localisations etc)
					}

					if (prices.Count == 0 || prices(0) == null) {
						Value = Int64.MinValue.ToString;
						//POA

					} else {
						clsPrice lowest = Utility.LowestPrice(prices);
						//they will all be in the same currency (that of the buyer account)

						// If UI IsNot Nothing Then UI.Controls.Add(CType(obj, clsBranch).BuyUI(buyeraccount, path))
						//Dim quoteLocked As Boolean = False ' Check for if a quote was exported to remove the +button
						//If iq.sesh(lid, "QuoteLocked") IsNot Nothing Then
						//    quoteLocked = CBool(iq.sesh(lid, "QuoteLocked"))

						//End If

						if (UI != null) {
							if (lowest.SKUVariant != null) {
								Panel prp = lowest.Ui(buyeraccount, 1, lid);
								//has its own width and inline-block - this panel (div)  is entirely replaced by the arriving (asynch) prices
								UI.Controls.Add(prp);

								// If there is a current quote, work out whether it's HPI or HPE
								clsQuote quote;
								object quoteSplit = Manufacturer.Unknown;
								if (iq.sesh(lid, "QuoteID") != null) {
									if (iq.sesh(lid, "AgentAccount") != null) {
										clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
										quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"));
										quoteSplit = quote.QuoteSplit;
									}
								}

								// Work out whether adding is enabled according to the HPE/HPI split
								bool addEnabled = true;
								if (product.isSystem(path)) {
									if (!quoteSplit == Manufacturer.Unknown) {
										addEnabled = (quoteSplit == product.Manufacturer);
									}
								}

								// Set up the message to display if the user attempts to create a mixed quote
								string splitMessage = string.Empty;
								if (!addEnabled) {
									splitMessage = GetSplitMessage(quoteSplit, buyeraccount.Language);
								}

								TextBox TB_QTY = new TextBox();
								TB_QTY = new TextBox();
								TB_QTY.ID = "qtytxt." + path;
								TB_QTY.Attributes("style") = "left:5em;width:1.5em;margin-left:.5em;height .9em;margin-top:.05em;border:solid silver 1px;";
								//did have float:left
								if (addEnabled) {
									TB_QTY.CssClass = "qty";
									TB_QTY.Attributes.Add("onmousedown", "burstBubble(event);");
								} else {
									TB_QTY.CssClass = "qtyDisabled";
									TB_QTY.ReadOnly = true;
									TB_QTY.Attributes.Add("onmousedown", string.Format("burstBubble(event); displayAddMsg('{0}', '{1}');", TB_QTY.ID, splitMessage));
								}

								if (!lowest.SKUVariant.Deleted) {
									UI.Controls.Add(TB_QTY);

									UI.Controls.Add(TreeAddButton(TB_QTY, path, obj, lowest.SKUVariant, buyeraccount.Language, addEnabled, splitMessage));
								}
							}
						}
						Value = (lowest.Price.NumericValue * 100).ToString;
					}

					if (Value == Int64.MinValue) {
						csv = Utility.CSV("No Price");
					} else {
						if (export == false) {
							csv = Utility.CSV((Value / 100).ToString("N2"));
							//format as a number to two decimal places
						} else {
							csv = Utility.CSV(buyeraccount.Currency.Symbol + (Value / 100).ToString("N2"));
							//format as a number to two decimal places
						}
					}

				} else {
					//SKUless product/row
					lbl.Text = "NO SKU";
					if (UI != null)
						UI.Controls.Add(lbl);
					Value = Int64.MinValue;
					//Single.MinValue
				}


				Translation = null;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"stock":

				csv = "";
				//fix' for 'unhandled specialcoumn stock'

				clsProduct product;
				product = ((clsBranch)obj).Product;
				if (product != null) {
					if (product.hasSKU) {
						string Disp;
						//we're just fetching a numeric value (for sorting)
						if (UI == null) {

							int stockvalue;
							//TODO -decide if we should sum these take a max or what
							Disp = product.CurrentStock(buyeraccount, stockvalue, iq.AllVariants, errormessages);
							//SETS (numeric) value - passing nothing totalises the stock of all variant
							Value = stockvalue;
							csv = GetStock(buyeraccount, stockvalue, export);


						} else {
							//add a asynch-refreshabe stock number..
							List<clsPrice> prices;
							prices = product.GetPrices(buyeraccount, buyeraccount.SellerChannel.priceConfig, iq.AllVariants, errormessages, true);

							if (prices.Count > 0 && prices(0) != null) {
								UI.Controls.Add(prices(0).SKUVariant.StockUI(1, string.Empty, buyeraccount.Language, buyeraccount.SellerChannel));
								int stockvalue;
								product.CurrentStock(buyeraccount, stockvalue, iq.AllVariants, errormessages);
								//populate the return value with the numeric stock
								Value = stockvalue;
								csv = GetStock(buyeraccount, stockvalue, export);


							} else {
								lbl.Text = "-";
								Value = Int64.MinValue.ToString;
								//Single.MinValue
								UI.Controls.Add(lbl);
								csv = "";

							}
						}
					} else {
						//non skud
						lbl.Text = "-";
						if (UI != null) {
							UI.Controls.Add(lbl);
						}
						Value = Int64.MinValue.ToString;
						//Single.MinValue

						csv = string.Empty;
					}
				}


				Translation = null;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"memory":
				//Need to re-import to get this to work

				//    Dim segs As Integer = Split(path$, ".").Length 'how many segements were there in the path to this point (becuase we will look in the 'next' segment for a branch called 'memory')
				object preinstalled = GetPreinstalled(lid, path, obj, buyeraccount, errormessages);

				int mem = 0;
				string bn;

				foreach ( i in preinstalled) {
					bn = i.Branch.Translation.text(English);
					if (i.Branch.Product != null) {
						if (i.Branch.Product.ProductType.Code.ToUpper() == "MEM") {
							if (i.Branch.Product.i_Attributes_Code.ContainsKey("capacity")) {
								mem = mem + (i.NumPreInstalled) * (int)i.Branch.Product.i_Attributes_Code("capacity")(0).NumericValue;
								//the capcity attribute of the DIMM
							}
						}
					}
				}


				lbl.Text = mem + " GB";
				if (UI != null)
					UI.Controls.Add(lbl);

				Value = mem.ToString;
				csv = Utility.CSV(lbl.Text.ToString);

				Translation = null;

				unit = iq.i_unit_code("Gbyte");
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"drives":

				//Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
				object preinstalled = GetPreinstalled(lid, path, obj, buyeraccount, errormessages);
				int drives = 0;

				foreach ( i in preinstalled) {
					if (i.Branch.Product.ProductType.Code.ToUpper() == "HDD") {
						drives += 1;
					}
				}


				lbl.Text = drives;
				if (UI != null)
					UI.Controls.Add(lbl);

				Value = drives.ToString;
				csv = drives.ToString;

				Translation = null;
			//unit = iq.i_unit_code("Gbyte")

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"drivecapacity":

				//Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
				object preinstalled = GetPreinstalled(lid, path, obj, buyeraccount, errormessages);

				decimal driveCapacity = 0;

				foreach ( i in preinstalled) {

					if (i.Branch.Product.ProductType.Code.ToUpper() == "HDD") {
						if ((i.Branch.Product.i_Attributes_Code.ContainsKey("capacity"))) {
							driveCapacity += i.Branch.Product.i_Attributes_Code("capacity")(0).NumericValue;
						}
					}
				}


				if (driveCapacity == 0) {
					//Go check in attributes and see if its there.....
					if (((clsBranch)obj).Product.i_Attributes_Code.ContainsKey("capacity")) {
						driveCapacity = ((clsBranch)obj).Product.i_Attributes_Code("capacity")(0).NumericValue;
					}
				}
				if (driveCapacity == 0)
					lbl.Text = "NA";
				else
					lbl.Text = driveCapacity + " GB";
				if (UI != null)
					UI.Controls.Add(lbl);

				Value = driveCapacity;
				csv = Value;

				Translation = null;



				unit = iq.i_unit_code("Gbyte");

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"supplychain":
				if (obj.parent != null) {
						//The supply chain is this branches parent (systems 'live' within supply chains)
						// numericValue = .Translation.SortValue(buyeraccount.Language)
					 // ERROR: Not supported in C#: WithStatement


				}

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"display":

				clsTranslation tl = ((clsBranch)obj).Product.i_Attributes_Code("Display")(0).Translation;
				lbl.Text = tl.text(buyeraccount.Language);
				if (UI != null)
					UI.Controls.Add(lbl);
				Value = "0";
				//tl.SortValue(buyeraccount.Language)

				csv = Utility.CSV(tl.text(language));
			//Case Is = "cpuspeed"

			//    lbl.Text = "-"
			//    Dim product As clsProduct = obj.Product

			//    Dim cpu As clsProduct
			//    Dim cpusku$
			//    If product.i_attributes_code.containskey("cpuSKU") Then
			//        cpusku = product.i_attributes_code("cpuSKU").Translation.text(English)
			//        If iq.i_SKU.ContainsKey(cpusku) Then
			//            cpu = iq.i_SKU(cpusku)
			//            If cpu.i_attributes_code.containskey("speed") Then
			//                lbl.Text = cpu.i_attributes_code("speed").NumericValue
			//            End If
			//        End If

			//        MatrixUI.Controls.Add(lbl)

			//    End If
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"operatingsystem":

				//Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
				object preinstalled = GetPreinstalled(lid, path, obj, buyeraccount, errormessages);
				string tl;

				foreach ( i in preinstalled) {
					if (i.Branch.Product.ProductType.Code.ToUpper() == "SOF1") {
						Regex r = new Regex("(Windows [A-z|0-9| ]+ [Foundation]*[Standard]*[Datacenter]*[Essentials]*)[ ]+");
						tl = i.Branch.DisplayName(buyeraccount.Language);
						Match m = r.Match(tl);
						if (m.Groups.Count > 1) {
							tl = m.Groups(1).Value;
						}
					}
				}


				lbl.Text = tl;
				if (UI != null)
					UI.Controls.Add(lbl);
				csv = Utility.CSV(tl);

				Translation = iq.AddTranslation(tl, buyeraccount.Language, "", 0, null, 0, false);
			//unit = iq.i_unit_code("Gbyte")

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"portcountorradio":
				object attrs = ((clsBranch)obj).Product.i_Attributes_Code;
				if (attrs.ContainsKey("PriConnectivity")) {
					if (attrs("PriConnectivity").First.Translation.text(English).Contains("802.1")) {
						lbl.Text = attrs("PriConnectivity").First.Translation.text(English);
						Translation = attrs("PriConnectivity").First.Translation;
						csv = lbl.Text;
					} else {
						if (attrs.ContainsKey("PriPorts")) {
							lbl.Text = attrs("PriPorts").First.NumericValue.ToString();
							Value = attrs("PriPorts").First.NumericValue;
							Translation = iq.AddTranslation(lbl.Text, English, "", 0, null, 0, false);
							csv = attrs("PriPorts").First.NumericValue;
						}
					}
					if (UI != null)
						UI.Controls.Add(lbl);
				}
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"formfactorcompressed":
				object attrs = ((clsBranch)obj).Product.i_Attributes_Code;
				if (attrs.ContainsKey("formFactor")) {
					if (attrs("formFactor").First.Translation.text(English).ToLower.Contains("tower")) {
						Translation = iq.AddTranslation("Tower", English, "", 0, null, 0, false);
					} else {
						Translation = attrs("formFactor").First.Translation;
					}
					lbl.Text = Translation.text(language);
					csv = Translation.text(language);
					if (UI != null)
						UI.Controls.Add(lbl);
				}
			default:
				SpecialColumn = false;
				//this wasn't a 'Special' Column

				Translation = null;
		}

	}
	/// <summary>
	/// Gets stock quantity or message in stock or out of stock for binarystock channels.
	/// </summary>
	/// <param name="account">an instance of clsAccount.</param>
	/// <param name="value">An integer value that represents the quantity of stock.</param>
	/// <param name="export">A boolean value that represents if export is being done.</param>
	/// <returns>A string object that represents the text or number to display in quote export.</returns>
	/// <remarks></remarks>
	private string getStock(clsAccount account, Int64 value, bool export)
	{
		string result = string.Empty;
		if (account.SellerChannel.BinaryStock & value > 0) {
			result = InStock.text(account.Language);
		} else if (account.SellerChannel.BinaryStock & value <= 0) {
			result = OutOfStock.text(account.Language);
		} else if (!account.SellerChannel.BinaryStock & value > 0) {
			result = value;
		} else if (!account.SellerChannel.BinaryStock & value <= 0) {
			if (export) {
				result = "0";
			} else {
				result = value.ToString;
			}
		}
		return result;
	}

	private List<clsQuantity> GetPreinstalled(UInt64 lid, string path, ref object obj, ref clsAccount buyeraccount, ref List<string> errorMessages)
	{

		//Return New List(Of clsQuantity)  '@@@ This was a test to see how expensive GetPreinstalledRecursive is when creating many matrixheaders/detailsquares
		//the answer is .. not very (not more  than a 10 % improvments when i returned an empty list and did none of the work.
		//Persisting the preInstalls dictionary looked attractive - but any gains would be small



		if (iq.sesh(lid, "preInstalls") == null) {
			iq.sesh(lid, "preInstalls") = new Dictionary<string, List<clsQuantity>>();
		}
		Dictionary<string, List<clsQuantity>> preinstalledDic = iq.sesh(lid, "preInstalls");
		if (preinstalledDic.ContainsKey(path)) {
			GetPreinstalled = preinstalledDic(path);
		} else {
			GetPreinstalled = ((clsBranch)obj).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path, errorMessages);
			preinstalledDic(path) = GetPreinstalled;
		}
	}

	public Panel EditUI(object obj, string path, Panel RowPanel, bool enabled, Web.HttpRequest Request, clsLanguage language, clsAccount buyerAccount, bool PageMode, ref List<string> errorMessages, clsLanguage translateLanguage)
	{

		EditUI = this.emptyCell(!this.visibleList, true);
		//New Panel - has an absolute width as defined in the field (ie. me.width - in ems)
		// EditUI.Attributes("style") &= "overflow:visible;display:inline-block;" 'otherwise we don't see the dropdowns wich are absolutely positioned (taking them out of the flow)

		//returns the UI element for editing this fields property  of the supplied OBJ, using the interface defined in the field F
		//the 'enabled' flag enables the element - and is used for history at the moment (but will be useful for role/right stuff)
		//F may contain some straight propert of obj
		//e.g. DisplayName
		//or some derived property such as attributes(17).name

		TextBox tb;
		Panel ddl;
		//DropDownList

		UnitType em;
		em = UnitType.Em;

		switch (LCase(this.InputType.code)) {

			case "string":
			case "int32":
			case "single":
			case "translate":
			case "nullstring":
			case "nullint":
			case "nullprice":
				//simple textbox

				tb = new TextBox();
				EditUI.Controls.Add(tb);

				tb.Style("width") = (string)this.width - 0.5 + "em";
				tb.Style("height") = "100%";
				//for textAreas in paged mode
				tb.Style("text-align") = "top";
				tb.Style("display") = "inline-block";

				if (PageMode) {
					if (this.height > 2) {
						tb.TextMode = TextBoxMode.MultiLine;
					}
				}


				if (this.InputType.code == "translate") {
					clsTranslation tobj;

					tobj = (clsTranslation)Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
					if (tobj == null) {
						tb.BackColor = Drawing.Color.Pink;
						tb.Text = "";
						tb.ToolTip = Xlt("Missing text", language);
					} else {
						if (translateLanguage.Code != null) {
							Label lbl = new Label();
							EditUI.Controls.Add(lbl);
							lbl.Style("width") = (string)this.width - 0.5 + "em";
							lbl.Style("height") = "100%";
							//for textAreas in paged mode
							lbl.Style("text-align") = "top";
							lbl.Style("display") = "inline-block";
							lbl.Text = tobj.text(language);
							if (tobj.textTranslation(translateLanguage).Length > 1) {
								tb.Text = tobj.textTranslation(translateLanguage);
							} else {
								tb.BackColor = Drawing.Color.Pink;
								tb.Text = "";
								tb.ToolTip = Xlt("Missing text", language);
							}
						} else {
							tb.Text = tobj.text(language);
						}
						//Add a button for a new translation
						EditUI.Controls.Add(editor.MakeButton(true, "Nt", "New translation", "createNewTranslation('" + path + "','" + this.propertyName + "');"));

						//                  tb.BackColor = Drawing.Color.CornflowerBlue 'remove
					}

				} else if (this.InputType.code == "nullstring") {
					nullableString ns;
					ns = (nullableString)Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
					tb.Text = ns.DisplayValue;

				} else if (this.InputType.code == "nullint") {
					NullableInt ni;
					ni = (NullableInt)Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
					tb.Text = ni.Displayvalue;
				//Nullable price
				} else if (this.InputType.code == "nullprice") {
					NullablePrice np;
					np = (NullablePrice)Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);

					//every price has a currency - this provides the currency symbol - however the number formatting is determined by buyers,channels, culture 
					tb.Text = np.NumericValue.ToString;
					//DisplayPrice(buyerAccount, errorMessages).Text  'currency formatting is is the culture of the seller (the currency alone doesn't give us suffifient info at euro is pan Eueropean - but NL formats €1.000,00 and IE formats €1,000.00
				} else {
					//straight' text
					object ao = Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
					if (ao == null) {
						tb.Text = "";
					} else {
						tb.Text = ao.ToString;
					}
				}

				//    If LCase(f.propertyName) = "name" Then title = tb.Text
				tb.ID = "c_" + Trim(this.ID.ToString) + "_" + Trim(obj.id.ToString);
				tb.CssClass = "input";
				//This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS) - input carries no styling (neccessarily)
				tb.Enabled = enabled;

				//      tb.ToolTip = tb.ID 'remove
				//        tb.Style("background-color") = "green"

				validationScript = "";
				if (!this.Validation == null) {
					// passes a regEx and length to the JS to validate 
					// validate will disable all controls with a 'save' class, 
					// and make the textbox 'invalid' class (if it's invalid)
					object msg;
					msg = this.Validation.ViolationMessage;
					msg = Replace(msg, "'", "");

					//must escape the backslashes in regex's (we're creating dynamically)
					validationScript = "validate('" + Replace(this.Validation.regEx, "\\", "\\\\") + "','" + msg + "','" + tb.ID + "');";

				}

				if (this.length > 0) {
					validationScript += "validateLength(" + this.length + ",'" + tb.ID + "');";
				}


				tb.Attributes.Add("onKeyUp", validationScript);

			case "boolean":
				WebControls.CheckBox cb;
				cb = new CheckBox();
				EditUI.Controls.Add(cb);
				//cb.Attributes("style") = "width:" & me.width & "em;"

				cb.Checked = (bool)Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);

				//              \/ Note - checkboxes have different handing in the JS because (stupidly) they have a 'checked' property - and their "value" is always "on"
				cb.ID = "cb_" + Trim(this.ID.ToString) + "_" + Trim(obj.id.ToString);
				//       cb.CssClass = "input"  'This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS)
				cb.InputAttributes.Add("class", "input");
				//the above doesn't work - becuase .NET takes it upon itseld to render the checkbox without the the class in a <span> with the class


				cb.Enabled = enabled;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"one":
				object targetObj;
				targetObj = Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
				//this object is the 'target' of the foregin key - and contains the selected value 

				string controlID;
				controlID = "c_" + Trim(this.ID.ToString) + "_" + obj.id;

				ddl = FilledDDL(this, targetObj, language, controlID, enabled, RowPanel, Request("depth"), errorMessages);
				//Obj2 carries the ID

				EditUI.Controls.Add(ddl);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"many":
				//this field holds a collection of things (a dictionary) - we render a button - which will embed editing for that dictionary

				//btn = New Button
				//btn.Style("width") = "100%"

				object dic = Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
				string txt;
				if (dic == null) {
					txt = "Add " + this.labelText.text(language);
					//propertyName
				} else {
					txt = dic.count.ToString + " " + this.labelText.text(language);
					//propertyName
				}

				object td = path + "." + this.propertyName;
				//, subPanel.ID
				EditUI.Controls.Add(editor.MakeButton(true, txt, "Show/edit these", editor.EmbedScript(td, td, "", true, true)));

				Panel tp = new Panel();
				tp.ID = td;

				RowPanel.Controls.Add(tp);



			//btn.CssClass = "input"
			//btn.ID = "c_" & Trim$(Me.ID.ToString) & "_" & Trim$(obj.id.ToString)

			//'Dim descendantPanel = EmptyPanel(inPanel.ID & "." & Me.propertyName) '& "(" & obj.id & ")")
			//'inPanel.Controls.Add(descendantPanel) 'note, there will be one child panel for each 'many' field (editable dictionary)


			//If enabled Then
			//    'depth request(depth+1)

			//    btn.Attributes("onclick") = editor.EmbedScript(path$ & "." & Me.propertyName, subpanel, "depth," & Request("depth") + 1, False, False)
			//    btn.ToolTip = btn.Attributes("onclick")
			//    btn.Enabled = True
			//Else
			//    btn.Enabled = False
			//End If

			//If obj.id = -1 Then btn.Enabled = False : btn.ToolTip = "You must save before you can attach items"
			//EditUI.Controls.Add(btn)

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"date":

				TextBox txtbox;
				txtbox = new TextBox();
				txtbox.ID = "c_" + Trim(this.ID.ToString) + "_" + Trim(obj.id.ToString);
				//This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS)
				txtbox.CssClass = "input";
				txtbox.Style.Add("width", "55%");

				//txtbox.Width = New Unit(f.width / 3 * 2, ut)

				System.DateTime dt;
				dt = (System.DateTime)Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
				txtbox.Text = Format(dt, "yy-MM-dd");
				txtbox.Enabled = enabled;

				TextBox timebox;
				timebox = new TextBox();
				timebox.Text = Format(dt, "HH:mm");
				timebox.ID = "c2_" + Trim(this.ID.ToString) + "_" + Trim(obj.id.ToString);
				//This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS)
				timebox.CssClass = "input";
				timebox.Style.Add("width", "38%");
				//timebox.Width = New Unit(f.width / 3, ut)

				timebox.Enabled = enabled;

				//attach to an onload event of an image
				Image img = new Image();
				img.ImageUrl = eim + "resort.png";
				img.Attributes.Add("onload", "$(function() {$( \"#" + txtbox.ID + "\" ).datepicker({ dateFormat: \"yy-mm-dd\" });});");
				//   img.Attributes.Add("onload", "$(function() {$( ""#" & txtbox.ID & """ ).datepicker({ dateFormat: ""yy-mm-dd"" });}$( ""#" & txtbox.ID & """ ).datepicker( ""setDate"" , '" & txtbox.Text & "' ));")

				img.Width = 1;
				img.Height = 1;
				EditUI.Controls.Add(img);

				EditUI.Controls.Add(txtbox);

				EditUI.Controls.Add(timebox);
			//Case Is = "customerprice"

			// Dim product As clsProduct
			// product = obj

			// Dim lbl As New Label
			//    EditUI.Controls.Add(lbl)
			//    lbl.Text = product.GetPrices(buyerAccount, iq.StandardVariant)(0).Price.DisplayPrice.Text

			default:

				Beep();
		}



	}


	//' DECREASES the [order] of a column (moving it left)
	public void promote(List<string> errorMessages)
	{

		SortedDictionary<int, clsField> sf = new SortedDictionary<int, clsField>();

		foreach ( f in this.Screen.Fields.Values) {
			sf.Add(f.order, f);
		}

		//swap this fields order - with the one of the field before it
		clsField pf = null;
		//previous field
		int pfo;
		//previous fields order (we need a 'spare' variable to perform the swap)

		foreach ( f in sf) {
			if (object.ReferenceEquals(f.Value, this)) {

				if (!pf == null) {
					pfo = pf.order;
					pf.order = f.Value.order;
					f.Value.order = pfo;
					f.Value.update(errorMessages);
					pf.update(errorMessages);

					break; // TODO: might not be correct. Was : Exit For
				}
			}
			pf = f.Value;
		}


	}

	public void i_Editable.update(ref List<string> errorMessages)
	{
		object sql;
		sql = "UPDATE [field] set ";
		sql += "fk_screen_id=" + this.Screen.ID;
		sql += ",property=" + da.SqlEncode(this.propertyName);
		//sql$ &= ",propertyClass=" & da.SqlEncode(Me.PropertyClass)
		sql += ",fk_translation_key_label=" + this.labelText.Key;
		sql += ",helptext=" + da.SqlEncode(this.helpText);
		string vid;
		if (this.Validation == null) {
			vid = "null";
		} else {
			vid = this.Validation.ID;
		}
		sql += ",fk_validation_id=" + vid;
		sql += ",lookupof=" + da.SqlEncode(this.lookupOf);
		sql += ",fk_inputtype_id=" + this.InputType.ID;
		sql += ",length=" + this.length;
		sql += ",[order]=" + this.order;
		//    sql$ &= ",fk_screen_id_embed=" & NullID(Me.EmbedScreen)
		sql += ",[width]=" + this.width;
		sql += ",[height]=" + this.height;
		sql += ",defaultvalue=" + da.SqlEncode(this.defaultValue);
		sql += ",visibleList=" + IIf(this.visibleList, "1", "0").ToString;
		sql += ",visiblePage=" + IIf(this.visiblePage, "1", "0").ToString;
		sql += ",defaultfilter=" + da.SqlEncode(this.defaultFilter);
		sql += ",defaultsort=" + da.SqlEncode(this.defaultSort);
		sql += ",priority=" + this.priority;
		sql += ",fk_translation_key_widgetGroup=" + TranslationKey(this.QuickFilterGroup);
		//NB: this is a clsTranslation (there's an overload for sqlEncode)
		sql += ",widgetUI=" + da.SqlEncode(this.QuickFilterUItype);
		sql += ",Grows=" + da.SqlEncode(this.Grow);
		sql += ",DefaultFilterValues=" + da.SqlEncode(this.DefaultFilterValues);


		sql += " WHERE ID=" + this.ID;

		da.DBExecutesql(sql, false);


		this.Screen.i_field_property.Remove(oPropertyName);
		this.Screen.i_field_property.Add(this.propertyName, this);

		if (!this.Screen.Fields.ContainsKey(this.ID))
			this.Screen.Fields.Add(this.ID, this);
		//This is for when we've added one (using the New button)

		oPropertyName = propertyName;


	}

	public string LooksUp()
	{

		string[] lu;
		lu = Split(this.lookupOf, "(");
		return lu(0);

	}


	public void i_Editable.Delete(ref List<string> errorMessages)
	{
		try {
			object sql;
			sql = "DELETE FROM [field] WHERE ID=" + this.ID;

			da.DBExecutesql(sql);

			this.Screen.Fields.Remove(this.ID);
			this.Screen.i_field_property.Remove(oPropertyName);


		} catch (System.Exception ex) {
			errorMessages.Add("unable to delete " + ex.Message);
		}


	}


	public string i_Editable.displayName(clsLanguage Language)
	{

		return this.propertyName;

	}




}

