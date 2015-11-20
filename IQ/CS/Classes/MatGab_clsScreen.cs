
using System.Web.UI.DataVisualization.Charting;
using dataAccess;
using System.Xml.Serialization;
using System.IO;

public class clsPriorityDirection
{
	[XmlIgnore()]
	public clsField _column;
	[XmlIgnore()]
	public clsField column {
		get {
			if (_column == null && columnid != 0)
				_column = iq.Fields(columnid);
			return _column;
		}
		set { _column = value; }
	}
		//1 is the highest priority (5 is lower)
	public int Priority;
		//ASC DESC
	public string Direction;

		//for serialization
	public int columnid;

	//for serialization
	public clsPriorityDirection()
	{

	}

	public clsPriorityDirection(clsField column, int Priority, string direction)
	{
		this.column = column;
		this.Priority = Priority;
		this.Direction = direction;

	}
	//Special constructor makes one from something like 283,2D
	public clsPriorityDirection(l)
	{

		string[] bits = Split(l, ",");
		this.column = iq.Fields((int)bits(0));

		string pd = bits(1);
		this.Priority = (int)Left(pd, 1);
		this.Direction = "A";
		if (UCase(Right(pd, 1)) == "D")
			this.Direction = "D";

	}

	//Public Sub New(field As clsField, PD As String)
	//    Me.column = field
	//    Me.Priority = CInt(Left(PD, 1))
	//    Me.Direction = "ASC"
	//    If UCase(Right(PD, 1)) = "D" Then Me.Direction = "DESC"
	//End Sub

	public Panel UI(path, clsLanguage language)
	{
		//returns an arrow button for flipping the sort order of this column from ascending to descending
		UI = new Panel();
		UI.CssClass = "sort_" + this.Direction + " pri_" + this.Priority + " sortArrow";

		Literal lit = new Literal();
		if (this.Direction == "A") {
			UI.Attributes("onmousedown") = "getBranches('cmd=sort&path=" + path + "&colID=" + this.column.ID + "&priority=" + this.Priority + "&direction=D');";
			// + sortFieldID + ',' + v);"
			lit.Text = "&nbsp;";
			//the column shows an indication of how you are
			UI.Controls.Add(lit);
			UI.ToolTip = Xlt("switch to descending", language);
		} else if (this.Direction == "D") {
			//It's currently Descending

			UI.Attributes("onmousedown") = "getBranches('cmd=sort&path=" + path + "&colID=" + this.column.ID + "&priority=" + this.Priority + "&direction=A');";
			// + sortFieldID + ',' + v);"
			lit.Text = "&nbsp;";
			//the column shows an indication of how you are
			UI.Controls.Add(lit);
			UI.ToolTip = Xlt("switch to ascending", language);
		} else {
			Beep();
		}

	}

}


public class clsScreen : i_Editable
{

	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public string code {
		get { return m_code; }
		set { m_code = Value; }
	}
	private string m_code;
	public string title {
		get { return m_title; }
		set { m_title = Value; }
	}
	private string m_title;
	//(title)
	public string Obj {
		get { return m_Obj; }
		set { m_Obj = Value; }
	}
	private string m_Obj;
	//what class of object does this screen edit  - NB: there is no reference to *which dictionary* this screen maintains, becuase it can maintain many (often the 'children' collections of objects
	public Dictionary<int, clsField> Fields {
		get { return m_Fields; }
		set { m_Fields = Value; }
	}
	private Dictionary<int, clsField> m_Fields;

		// which fied represents which property
	public Dictionary<string, clsField> i_field_property;

	// Public Property DicName As String '
		//Set during makescreen if the object has an AuditRoot property 
	public bool Auditable;

	string oCode;

	string oTitle;
	//Automatically collapses columns that won't fit in the available screen space (based on their priority)
	//priority is not necessarily the same as order - as you may want a high priorty column (such as price) to appear at the end of a row
	//Priority '1' is the highest priority


	public clsScreen()
	{
		//the editor requires a parameterless constructor

	}

	public float TotalWidth()
	{

		TotalWidth = 0;
		foreach ( f in Fields.Values) {
			if (f.visibleList) {
				TotalWidth += +f.width;
			} else {
				TotalWidth += collapsedColumnWidth;
			}
		}

		TotalWidth += 3;
		//some space for margins gutters etc

	}


	public string i_Editable.displayName(clsLanguage langauge)
	{
		return title + " (" + Obj + ")";
	}

	public clsScreen copy(ref List<string> errormessages)
	{

		copy = new clsScreen(this.Obj, this.code + "_2", "copy of " + this.title, errormessages);

		clsField afield;
		foreach ( f in this.Fields.Values) {
			afield = new clsField(copy, f.propertyName, f.lookupOf, f.labelText, f.helpText, f.Validation, f.InputType, f.length, f.order, f.width,
			f.height, f.defaultValue, f.visibleList, f.visiblePage, f.defaultFilter, f.defaultSort, f.priority, f.QuickFilterGroup, f.QuickFilterUItype, f.CanUserSelect,
			f.LinkedFieldID, f.FilterVisible);
		}

	}

	public Dictionary<int, clsPriorityDirection> DefaultSorts()
	{

		DefaultSorts = new Dictionary<int, clsPriorityDirection>();
		foreach ( f in this.Fields.Values) {
			if (f.defaultSort != "") {
				clsPriorityDirection dso = new clsPriorityDirection(f, (int)Left(f.defaultSort, 1), Right(f.defaultSort, 1));
				// using a dictionary ensures that the priorites are unique - but we don't want to crash and burn if there's a dupe
				if (!DefaultSorts.ContainsKey(dso.Priority)) {
					DefaultSorts.Add(dso.Priority, dso);
				}
			}
		}

	}



	public clsScreen(int id, string code, string obj, string Title)
	{
		this.ID = id;
		this.Obj = obj;
		this.code = code;

		this.title = Title;
		//  Me.DicName = dicname
		this.Fields = new Dictionary<int, clsField>();
		iq.Screens.Add(this.ID, this);
		if (!iq.i_screens_title.ContainsKey(this.title)) {
			iq.i_screens_title.Add(this.title, this);
		} else {
			//  Beep()
		}
		//iq.I_SCREENS_TYPE.Add(Me.code, Me)
		if (!iq.i_screens_code.ContainsKey(code)) {
			if (code != "")
				iq.i_screens_code.Add(code, this);
		}

		this.i_field_property = new Dictionary<string, clsField>();

		this.oCode = this.code;
		this.oTitle = this.title;

	}

	public Panel EditTitles(clsEditHeader EditHeader, clsLanguage language)
	{

		EditTitles = new Panel();
		//Labels are positioned absolutely within this panel
		EditTitles.CssClass = "editHeaderLabels";
		//"innerEditHeader"


		object vPath = HttpContext.Current.Request.ApplicationPath;
		object pPath = HttpContext.Current.Request.MapPath(vPath);

		//        Dim img As Image

		//        Dim fields = (From f In Me.Fields.Values Where f.visibleList = True Order By f.order)
		object fields = (from f in this.Fields.Valuesorderby f.order);

		EditTitles.Controls.Add(NewLit("<div class='efcSpacer'></div>"));
		//order by order
		foreach ( f in fields) {

			Panel c = f.emptyCell(!f.visibleList, true);

			EditTitles.Controls.Add(c);
			if (f.visibleList) {
				Label lbl = new Label();
				lbl.Text = f.displayName(language);
				//displayName(language)
				c.Controls.Add(lbl);
			}
		}

		//' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"

	}

	public Panel MatrixHeaders(clsScreenHeader MatrixHeader, clsBranchInfo bi, clsLanguage language)
	{

		MatrixHeaders = new Panel();
		//Labels are positioned absolutely within this panel
		MatrixHeaders.CssClass = "innerMatrixHeader";
		MatrixHeaders.ID = "innerMatrixHeader";

		Chart mychart;

		//Dim matrix As clsScreen
		//        matrix = iq.Screens(Chart.Attributes("MatrixID"))
		//If Not matrix Is Nothing Then

		float x;
		//!! f*cked me over for 20 minutes

		x = 0;
		// We start 2 ems accross - leaving room for the contrast/shortlist checkboxes

		object vPath = HttpContext.Current.Request.ApplicationPath;
		object pPath = HttpContext.Current.Request.MapPath(vPath);

		// Start further left on the options screens in basic user mode as no expand/collapse control is displayed
		if (!UserIsAdmin(bi.lid) && (bi.branch.rca.StartsWith("GTB") | bi.branch.rca.Equals("G"))) {
			MatrixHeaders.Controls.Add(NewLit("<div class='LeftPad' style='display:inline-block;width:0.5em;'>&nbsp;</div>"));
		} else {
			MatrixHeaders.Controls.Add(NewLit("<div class='LeftPad' style='display:inline-block;width:4.25em;'>&nbsp;</div>"));
		}

		Image img;

		//Dim fields = (From f In Me.Fields.Values Where f.visibleList = True Order By f.order)
		//order by order
		foreach ( f in MatrixHeader.EffectiveFields) {
			string filename = f.labelText.text(language).Replace("/", "") + ".png";
			foreach ( c in IO.Path.GetInvalidFileNameChars) {
				filename = filename.Replace(c, "");
			}
			string filePath = pPath + "\\matrixLabels\\" + filename;

			if (!My.Computer.FileSystem.FileExists(filePath)) {
				mychart = new Chart();
				//  mychart.BackGradientStyle = GradientStyle.TopBottom
				//  mychart.BackSecondaryColor = Drawing.Color.FromArgb(255, 220, 220, 255)

				mychart.Width = 100;
				mychart.Height = 100;

				mychart.BackColor = System.Drawing.Color.Transparent;
				mychart.AntiAliasing = AntiAliasingStyles.All;

				//mychart.Attributes.Add("MatrixID", Me.ID)
				mychart.Attributes.Add("Text", f.labelText.text(language));
				mychart.PostPaint += postpaint;
				//Renders the diagonal labels
				//     matrixHeader.Controls.Add(mychart)

				mychart.ToolTip = f.helpText;
				//    mychart.Attributes("Style") = "position:absolute;left:" & x & "em;"
				mychart.Attributes("Style") = "width:" + MatrixHeader.FieldResultSet(f).GrownWidth.ToString() + "em;";
				mychart.SaveImage(filePath);
			}

			Panel ph = new Panel();
			if (MatrixHeader.ColIsCollapsed(f)) {
				ph.Width = new Unit(collapsedColumnWidth, UnitType.Em);
			} else {
				ph.Width = new Unit(MatrixHeader.FieldResultSet(f).GrownWidth, UnitType.Em);
			}


			img = new Image();
			img.ID = f.ID.ToString();
			img.Width = Unit.Pixel(100);
			img.Height = Unit.Pixel(100);
			img.ImageUrl = "../matrixlabels/" + filename;
			//img.Attributes("Style") = "position:relative;left:" & x & "em;"
			ph.Style("display") = "inline-block";
			ph.Controls.Add(img);
			MatrixHeaders.Controls.Add(ph);

			if (MatrixHeader.ColIsCollapsed(f)) {
				x = x + collapsedColumnWidth;
			} else {
				x = x + MatrixHeader.FieldResultSet(f).GrownWidth;
				//in ems
			}
		}

		//' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"

	}




	public clsScreen(string obj, string Code, string Title, List<string> errormessages)
	{
		object sql;

		sql = "INSERT INTO [Screen] (object, code, title) values (" + da.SqlEncode(obj) + "," + da.SqlEncode(Code) + "," + da.SqlEncode(Title) + ");";
		this.ID = da.DBExecutesql(sql, true);
		this.Obj = obj;
		this.code = Code;
		this.title = Title;
		// Me.DicName = dicname

		this.Fields = new Dictionary<int, clsField>();
		iq.Screens.Add(this.ID, this);
		if (iq.i_screens_title.ContainsKey(Title)) {
			errormessages.Add("There is more than one screen witht the title '" + Title + "'");
		} else {
			iq.i_screens_title.Add(this.title, this);
		}


		this.i_field_property = new Dictionary<string, clsField>();

		if (Code != "") {
			if (iq.i_screens_code.ContainsKey(Code)) {
				errormessages.Add("screen code '" + Code + "' is not unique !");
			} else {
				iq.i_screens_code.Add(Code, this);
			}
		}

		this.oCode = this.code;
		this.oTitle = this.title;

	}

	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsScreen(this.Obj, this.code, this.title, errormessages);

	}


	public void i_Editable.Update(ref List<string> errormessages)
	{
		object sql;
		sql = "UPDATE [screen] set object =" + da.SqlEncode(this.Obj) + ",code=" + da.SqlEncode(this.code) + ",title=" + da.SqlEncode(this.title) + " WHERE ID=" + this.ID;

		iq.i_screens_title.Remove(oTitle);
		iq.i_screens_title.Add(this.title, this);
		this.oTitle = this.title;
		iq.i_screens_code.Remove(oCode);
		iq.i_screens_code.Add(this.code, this);
		this.oCode = this.code;


		da.DBExecutesql(sql, false);

	}


	public void i_Editable.Delete(ref List<string> errormessages)
	{

		try {
			//Kill all the fields with one delete (instead of deleting them individually)
			object sql;
			sql = "Delete FROM [Field] WHERE [FK_Screen_ID]=" + this.ID;
			da.DBExecutesql(sql);

			foreach ( field in this.Fields.Values) {
				iq.Fields.Remove(field.ID);
			}

			iq.i_screens_title.Remove(this.title);
			iq.i_screens_code.Remove(this.code);
			iq.Screens.Remove(this.ID);

			sql = "Delete FROM [screen] WHERE id=" + this.ID;
			da.DBExecutesql(sql);



		} catch (System.Exception ex) {
			errormessages.Add(ex.Message);

		}


	}



	public PlaceHolder MatrixRow(object obj, clsBranchInfo bi, ref List<string> errorMessages, bool United)
	{
		//Obj would usually be a Product (but in theory - doesn't have to be one)

		UnitType em;
		em = UnitType.Em;
		//Percentage

		PlaceHolder pnl;
		//Panel
		pnl = new PlaceHolder();
		//Panel

		Panel cell;
		object script = "";

		//Note, the expand collapse butotn is acutally outside the matrix row

		Literal cb;
		//CheckBox

		//add the 'contrast/compare/shortlist column (of checkboxes)
		cell = new Panel();
		cell.CssClass = "matrixCell hideOverflow";
		cell.Attributes("style") = "position:relative;width:1.5em;";
		// col.Attributes("onmouseover") = "if(!lockedFbs){this.appendChild(ctl00_filterButtons);showFilterButtons('ctl00_btnContrast');};return false;"

		pnl.Controls.Add(cell);

		// cell.Controls.Add(bi.Branch.PromoIndicators(bi))

		//compare/constrast/scales function - removed for now
		if (false) {
			cb = new Literal();
			//CheckBox - DO NOT attempt to use checkbox controls !  .NET has a horrible habbit of wrapping checkboxes in span tags - and the complications that causes are not worth dealing with - so we use literals
			cb.Text = "<input type='checkbox'  class='sl' id='sl_" + bi.path + "'/>";
			// we need the full path to each branch we're comparing - as paths are required to evaluate (preinstalled) quanitites (in contrast.aspx).. they cant' be derived from just the branch ID's
			cell.Controls.Add(cb);
		}

		float x;
		//use LINQ to order by order
		// Dim fields = (From f In Me.Fields.Values Order By f.order)

		//x = 4
		clsScreenHeader useHeader;
		if (United) {
			object screenHeaders = (Dictionary<string, clsScreenHeader>)iq.sesh(bi.lid, "screenHeaders");
			useHeader = screenHeaders(bi.rootPath);
		} else {
			useHeader = bi.EffectiveHeader;
		}

		// screen.Fields.Values
		foreach ( f in useHeader.EffectiveFields) {

			//If f.visibleList Then
			//matricpath was headerpath 
			bool collapsed = useHeader.ColIsCollapsed(f);
			//.isCollapsedAt(bi.lid, bi.MatrixHeader.pathEffectiveMatrixPath)
			cell = f.CellUI(obj, bi.path, bi.buyerAccount, bi.agentAccount.Language, useHeader, collapsed, bi.foci, errorMessages, bi.lid, useHeader.FieldResultSet(f).GrownWidth);
			//adds the main UI element (dropdown, textbox, calendar tickbox etc) 

			// cell.Style.Add("whitespace", "nowrap") - failed attemp to stop wordwrap in cells
			cell.Style.Add("position", "relative");
			//positioning the elements explicitly is the only way (i could find) to stop them wrapping (they are inline-block, I tried whitespace:no wrap - and othe 'solutions' to no avail)
			//  cell.Style.Add("left", x & "em")
			pnl.Controls.Add(cell);

			if (collapsed) {
				x = x + collapsedColumnWidth;
			} else {
				x = x + useHeader.FieldResultSet(f).GrownWidth;
			}

			//        script$ &= f.validationScript

			// pnl.Controls.Add(Gutter) 'the gap between columns - a sperate div because of issues with em's not geing consistent accross input boxes and the header columns
			//Else
			//hidden column (set to 1 em so it can be reinstated via it's show button)
			//col = New Panel
			//col.Width = New Unit(1, em)
			//lit = New Literal
			//lit.Text = "&nbsp;" ' we have to put something in the column - or it's not rendered !
			//col.Controls.Add(lit)
			//pnl.Controls.Add(col)
			// End If
		}

		//   Dim clear As Panel
		//   clear = New Panel
		//   clear.Style.Add("clear", "both")
		//   pnl.Controls.Add(clear)

		return pnl;

	}


}


