using System.Web.UI.DataVisualization.Charting;
using dataAccess;
using System.Xml.Serialization;
using System.IO;



public class clsPriorityDirection
{
    [XmlIgnore]
    public clsField _column;
    [XmlIgnore]
    public clsField column
    {
        get
        {
            if (_column == null && columnid != 0)
            {
                _column = iq.Fields(columnid);
            }
            return _column;
        }
        set
        {
            _column = value;
        }
    }
    public int Priority; //1 is the highest priority (5 is lower)
    public string Direction; //ASC DESC

    public int columnid; //for serialization

    public clsPriorityDirection() //for serialization
    {

    }
    public clsPriorityDirection(clsField column, int Priority, string direction)
    {

        this.column = column;
        this.Priority = Priority;
        this.Direction = direction;

    }
    public clsPriorityDirection(object l) //Special constructor makes one from something like 283,2D
    {

        string[] bits = Strings.Split(System.Convert.ToString(l), ",");
        this.column = iq.Fields(int.Parse(bits[0]));

        string pd = bits[1];
        this.Priority = int.Parse(pd.Substring(0, 1));
        this.Direction = "A";
        if (pd.Substring(pd.Length - 1, 1).ToUpper() == "D")
        {
            this.Direction = "D";
        }

    }

    //Public Sub New(field As clsField, PD As String)
    //    Me.column = field
    //    Me.Priority = CInt(Left(PD, 1))
    //    Me.Direction = "ASC"
    //    If UCase(Right(PD, 1)) = "D" Then Me.Direction = "DESC"
    //End Sub

    public Panel UI(object path, clsLanguage language)
    {
        Panel returnValue = default(Panel);
        //returns an arrow button for flipping the sort order of this column from ascending to descending
        returnValue = new Panel();
        returnValue.CssClass = "sort_" + this.Direction + " pri_" + System.Convert.ToString(this.Priority) + " sortArrow";

        Literal lit = new Literal();
        if (this.Direction == "A")
        {
            returnValue.Attributes("onmousedown") = "getBranches(\'cmd=sort&path=" + System.Convert.ToString(path) + "&colID=" + this.column.ID + "&priority=" + System.Convert.ToString(this.Priority) + "&direction=D\');"; // + sortFieldID + ',' + v);"
            lit.Text = "&nbsp;"; //the column shows an indication of how you are
            returnValue.Controls.Add(lit);
            returnValue.ToolTip = Xlt("switch to descending", language);
        }
        else if (this.Direction == "D")
        {
            //It's currently Descending

            returnValue.Attributes("onmousedown") = "getBranches(\'cmd=sort&path=" + System.Convert.ToString(path) + "&colID=" + this.column.ID + "&priority=" + System.Convert.ToString(this.Priority) + "&direction=A\');"; // + sortFieldID + ',' + v);"
            lit.Text = "&nbsp;"; //the column shows an indication of how you are
            returnValue.Controls.Add(lit);
            returnValue.ToolTip = Xlt("switch to ascending", language);
        }
        else
        {
            Interaction.Beep();
        }

        return returnValue;
    }

}


public class clsScreen : i_Editable
{

    public int ID { get; set; }
    public string code { get; set; }
    public string title { get; set; } //(title)
    public string Obj { get; set; } //what class of object does this screen edit  - NB: there is no reference to *which dictionary* this screen maintains, becuase it can maintain many (often the 'children' collections of objects
    public Dictionary<int, clsField> Fields { get; set; }

    public Dictionary<string, clsField> i_field_property; // which fied represents which property

    // Public Property DicName As String '
    public bool Auditable; //Set during makescreen if the object has an AuditRoot property

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
        float returnValue = 0;

        returnValue = 0;
        foreach (var f in Fields.Values)
        {
            if (f.visibleList)
            {
                returnValue += System.Convert.ToSingle(+f.width);
            }
            else
            {
                returnValue += System.Convert.ToSingle(collapsedColumnWidth);
            }
        }

        returnValue += 3; //some space for margins gutters etc

        return returnValue;
    }


    public string displayName(clsLanguage langauge)
    {
        return title + " (" + Obj + ")";
    }

    public clsScreen copy(List<string> errormessages)
    {
        clsScreen returnValue = default(clsScreen);

        returnValue = new clsScreen(this.Obj, this.code + "_2", "copy of " + this.title, errormessages);

        clsField afield;
        foreach (var f in this.Fields.Values)
        {
            afield = new clsField(returnValue, f.propertyName, f.lookupOf, f.labelText, f.helpText, f.Validation, f.InputType, f.length, f.order, f.width, f.height, f.defaultValue, f.visibleList, f.visiblePage, f.defaultFilter, f.defaultSort, f.priority, f.QuickFilterGroup, f.QuickFilterUItype, f.CanUserSelect, f.LinkedFieldID, f.FilterVisible);
        }

        return returnValue;
    }

    public Dictionary<int, clsPriorityDirection> DefaultSorts()
    {
        Dictionary<int, clsPriorityDirection> returnValue = default(Dictionary<int, clsPriorityDirection>);

        returnValue = new Dictionary<int, clsPriorityDirection>();
        foreach (var f in this.Fields.Values)
        {
            if (f.defaultSort != "")
            {
                clsPriorityDirection dso = new clsPriorityDirection(f, int.Parse(Strings.Left(System.Convert.ToString(f.defaultSort), 1)), Strings.Right(System.Convert.ToString(f.defaultSort), 1));
                if (!returnValue.ContainsKey(dso.Priority)) // using a dictionary ensures that the priorites are unique - but we don't want to crash and burn if there's a dupe
                {
                    returnValue.Add(dso.Priority, dso);
                }
            }
        }

        return returnValue;
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
        if (!iq.i_screens_title.ContainsKey(this.title))
        {
            iq.i_screens_title.Add(this.title, this);
        }
        else
        {
            //  Beep()
        }
        //iq.I_SCREENS_TYPE.Add(Me.code, Me)
        if (!iq.i_screens_code.ContainsKey(code))
        {
            if (code != "")
            {
                iq.i_screens_code.Add(code, this);
            }
        }

        this.i_field_property = new Dictionary<string, clsField>();

        this.oCode = this.code;
        this.oTitle = this.title;

    }

    public Panel EditTitles(clsEditHeader EditHeader, clsLanguage language)
	{
		Panel returnValue = default(Panel);
		
		returnValue = new Panel(); //Labels are positioned absolutely within this panel
		returnValue.CssClass = "editHeaderLabels"; //"innerEditHeader"
		
		
		object vPath = HttpContext.Current.Request.ApplicationPath;
		object pPath = HttpContext.Current.Request.MapPath(vPath);
		
		//        Dim img As Image
		
		//        Dim fields = (From f In Me.Fields.Values Where f.visibleList = True Order By f.order)
		System.Object fields = from f in this.Fields.Values orderby f.order select f;
		
		returnValue.Controls.Add(NewLit("<div class=\'efcSpacer\'></div>"));
		foreach (var f in fields) //order by order
		{
			
			Panel c = f.emptyCell(!f.visibleList, true);
			
			returnValue.Controls.Add(c);
			if (f.visibleList)
			{
				Label lbl = new Label();
				lbl.Text = f.displayName(language); //displayName(language)
				c.Controls.Add(lbl);
			}
		}
		
		//' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"
		
		return returnValue;
	}

    public Panel MatrixHeaders(clsScreenHeader MatrixHeader, clsBranchInfo bi, clsLanguage language)
    {
        Panel returnValue = default(Panel);

        returnValue = new Panel(); //Labels are positioned absolutely within this panel
        returnValue.CssClass = "innerMatrixHeader";
        returnValue.ID = "innerMatrixHeader";

        Chart mychart = default(Chart);

        //Dim matrix As clsScreen
        //        matrix = iq.Screens(Chart.Attributes("MatrixID"))
        //If Not matrix Is Nothing Then

        float x = 0; //!! f*cked me over for 20 minutes

        x = 0; // We start 2 ems accross - leaving room for the contrast/shortlist checkboxes

        object vPath = HttpContext.Current.Request.ApplicationPath;
        object pPath = HttpContext.Current.Request.MapPath(vPath);

        // Start further left on the options screens in basic user mode as no expand/collapse control is displayed
        if (!UserIsAdmin(bi.lid) && (bi.branch.rca.StartsWith("GTB") || bi.branch.rca.Equals("G")))
        {
            returnValue.Controls.Add(NewLit("<div class=\'LeftPad\' style=\'display:inline-block;width:0.5em;\'>&nbsp;</div>"));
        }
        else
        {
            returnValue.Controls.Add(NewLit("<div class=\'LeftPad\' style=\'display:inline-block;width:4.25em;\'>&nbsp;</div>"));
        }

        Image img = default(Image);

        //Dim fields = (From f In Me.Fields.Values Where f.visibleList = True Order By f.order)
        foreach (var f in MatrixHeader.EffectiveFields) //order by order
        {
            string filename = f.labelText.text(language).Replace("/", "") + ".png";
            foreach (var c in IO.Path.GetInvalidFileNameChars)
            {
                filename = filename.Replace(System.Convert.ToString(c), "");
            }
            string filePath = pPath + "\\matrixLabels\\" + filename;

            if (!(new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.FileExists(filePath))
            {
                mychart = new Chart();
                //  mychart.BackGradientStyle = GradientStyle.TopBottom
                //  mychart.BackSecondaryColor = Drawing.Color.FromArgb(255, 220, 220, 255)

                mychart.Width = 100;
                mychart.Height = 100;

                mychart.BackColor = System.Drawing.Color.Transparent;
                mychart.AntiAliasing = AntiAliasingStyles.All;

                //mychart.Attributes.Add("MatrixID", Me.ID)
                mychart.Attributes.Add("Text", f.labelText.text(language));
                mychart.PostPaint += new System.EventHandler(postpaint); //Renders the diagonal labels
                //     matrixHeader.Controls.Add(mychart)

                mychart.ToolTip = f.helpText;
                //    mychart.Attributes("Style") = "position:absolute;left:" & x & "em;"
                mychart.Attributes("Style") = "width:" + MatrixHeader.FieldResultSet(f).GrownWidth.ToString() + "em;";
                mychart.SaveImage(filePath);
            }

            Panel ph = new Panel();
            if (MatrixHeader.ColIsCollapsed(f))
            {
                ph.Width = new Unit(collapsedColumnWidth, UnitType.Em);
            }
            else
            {
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
            returnValue.Controls.Add(ph);

            if (MatrixHeader.ColIsCollapsed(f))
            {
                x = x + collapsedColumnWidth;
            }
            else
            {
                x = x + MatrixHeader.FieldResultSet(f).GrownWidth; //in ems
            }
        }

        //' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"

        return returnValue;
    }



    public clsScreen(string obj, string Code, string Title, List<string> errormessages)
    {

        object sql = null;

        sql = "INSERT INTO [Screen] (object, code, title) values (" + da.SqlEncode(obj) + "," + da.SqlEncode(Code) + "," + da.SqlEncode(Title) + ");";
        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.Obj = obj;
        this.code = Code;
        this.title = Title;
        // Me.DicName = dicname

        this.Fields = new Dictionary<int, clsField>();
        iq.Screens.Add(this.ID, this);
        if (iq.i_screens_title.ContainsKey(Title))
        {
            errormessages.Add("There is more than one screen witht the title \'" + Title + "\'");
        }
        else
        {
            iq.i_screens_title.Add(this.title, this);
        }


        this.i_field_property = new Dictionary<string, clsField>();

        if (Code != "")
        {
            if (iq.i_screens_code.ContainsKey(Code))
            {
                errormessages.Add("screen code \'" + Code + "\' is not unique !");
            }
            else
            {
                iq.i_screens_code.Add(Code, this);
            }
        }

        this.oCode = this.code;
        this.oTitle = this.title;

    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsScreen(this.Obj, this.code, this.title, errormessages);

    }

    public void update(ref List<string> errormessages)
    {

        object sql = null;
        sql = "UPDATE [screen] set object =" + da.SqlEncode(this.Obj) + ",code=" + da.SqlEncode(this.code) + ",title=" + da.SqlEncode(this.title) + " WHERE ID=" + System.Convert.ToString(this.ID);

        iq.i_screens_title.Remove(oTitle);
        iq.i_screens_title.Add(this.title, this);
        this.oTitle = this.title;
        iq.i_screens_code.Remove(oCode);
        iq.i_screens_code.Add(this.code, this);
        this.oCode = this.code;


        da.DBExecutesql(sql, false);

    }

    public void delete(ref List<string> errormessages)
    {


        try
        {
            //Kill all the fields with one delete (instead of deleting them individually)
            object sql = null;
            sql = "Delete FROM [Field] WHERE [FK_Screen_ID]=" + System.Convert.ToString(this.ID);
            da.DBExecutesql(sql);

            foreach (var field in this.Fields.Values)
            {
                iq.Fields.Remove(field.ID);
            }

            iq.i_screens_title.Remove(this.title);
            iq.i_screens_code.Remove(this.code);
            iq.Screens.Remove(this.ID);

            sql = "Delete FROM [screen] WHERE id=" + System.Convert.ToString(this.ID);
            da.DBExecutesql(sql);



        }
        catch (System.Exception ex)
        {
            errormessages.Add(ex.Message);

        }


    }



    public PlaceHolder MatrixRow(object obj, clsBranchInfo bi, List<string> errorMessages, bool United)
    {
        //Obj would usually be a Product (but in theory - doesn't have to be one)

        UnitType em;
        em = UnitType.Em; //Percentage

        PlaceHolder pnl = default(PlaceHolder); //Panel
        pnl = new PlaceHolder(); //Panel

        Panel cell = default(Panel);
        string script = "";

        //Note, the expand collapse butotn is acutally outside the matrix row

        Literal cb = default(Literal); //CheckBox

        //add the 'contrast/compare/shortlist column (of checkboxes)
        cell = new Panel();
        cell.CssClass = "matrixCell hideOverflow";
        cell.Attributes("style") = "position:relative;width:1.5em;";
        // col.Attributes("onmouseover") = "if(!lockedFbs){this.appendChild(ctl00_filterButtons);showFilterButtons('ctl00_btnContrast');};return false;"

        pnl.Controls.Add(cell);

        // cell.Controls.Add(bi.Branch.PromoIndicators(bi))

        //compare/constrast/scales function - removed for now
        if (false)
        {
            cb = new Literal(); //CheckBox - DO NOT attempt to use checkbox controls !  .NET has a horrible habbit of wrapping checkboxes in span tags - and the complications that causes are not worth dealing with - so we use literals
            cb.Text = "<input type=\'checkbox\'  class=\'sl\' id=\'sl_" + bi.path + "\'/>"; // we need the full path to each branch we're comparing - as paths are required to evaluate (preinstalled) quanitites (in contrast.aspx).. they cant' be derived from just the branch ID's
            cell.Controls.Add(cb);
        }

        float x = 0;
        //use LINQ to order by order
        // Dim fields = (From f In Me.Fields.Values Order By f.order)

        //x = 4
        clsScreenHeader useHeader = default(clsScreenHeader);
        if (United)
        {
            System.Object screenHeaders = (Dictionary<string, clsScreenHeader>)(iq.sesh(bi.lid, "screenHeaders"));
            useHeader = screenHeaders[bi.rootPath];
        }
        else
        {
            useHeader = bi.EffectiveHeader;
        }

        foreach (var f in useHeader.EffectiveFields) // screen.Fields.Values
        {

            //If f.visibleList Then
            //matricpath was headerpath
            bool collapsed = System.Convert.ToBoolean(useHeader.ColIsCollapsed(f)); //.isCollapsedAt(bi.lid, bi.MatrixHeader.pathEffectiveMatrixPath)
            cell = f.CellUI(obj, bi.path, bi.buyerAccount, bi.agentAccount.Language, useHeader, collapsed, bi.foci, errorMessages, bi.lid, useHeader.FieldResultSet(f).GrownWidth); //adds the main UI element (dropdown, textbox, calendar tickbox etc)

            // cell.Style.Add("whitespace", "nowrap") - failed attemp to stop wordwrap in cells
            cell.Style.Add("position", "relative"); //positioning the elements explicitly is the only way (i could find) to stop them wrapping (they are inline-block, I tried whitespace:no wrap - and othe 'solutions' to no avail)
            //  cell.Style.Add("left", x & "em")
            pnl.Controls.Add(cell);

            if (collapsed)
            {
                x = x + collapsedColumnWidth;
            }
            else
            {
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