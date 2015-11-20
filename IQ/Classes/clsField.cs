using dataAccess;
using System.Runtime.Serialization;

//Option Strict On

public class clsField : i_Editable
{


    public int ID { get; set; }
    public clsScreen Screen { get; set; } //The generic editor needs to 'see' this so it can populate it with its parent as a defualt.. otherwise, we cannot create instances of it
    public string propertyName { get; set; } //which property does this field edit/display (on the instance of the object that its' screen edits)
    //Public Property PropertyClass As String 'what is the class of this property (for example clsUser)
    public string lookupOf { get; set; } // used for 1:1 relationships - the dictionary to look in - with optional (field=value) filter eg. threads.stautus might lookup staus(group=Threads)
    public clsTranslation labelText { get; set; }
    public string helpText { get; set; }
    public clsValidation Validation { get; set; }
    public clsInputType InputType { get; set; }
    public int length { get; set; } // max character length
    public int order { get; set; }
    public float height { get; set; } //only applies in page/expanded mode
    public string defaultFilter { get; set; } //carries a comma seperate list of the code of filtes which can be applied to this field
    public Dictionary<int, clsFilter> Filters;
    public string defaultSort { get; set; }

    //need to make lookup fields look in a specific place (not necessarily Hygn's root dictionaries)
    // Public Property EmbedScreen As clsScreen  'where a property is a list - defining a 1:M relationship .. manage the 'many' using this screen
    public float width { get; set; } //onscreen width in ems - only applies in list mode
    public string defaultValue { get; set; }
    public bool visibleList { get; set; }
    public bool visiblePage { get; set; }
    public bool visibleSquare { get; set; }
    public bool Grow { get; set; }
    public string DefaultFilterValues { get; set; }
    public bool FilterVisible { get; set; }

    public int priority { get; set; } //How 'important' is this column - higher numbers are collapsed sooner

    //the 'quickfilter' is an optional, additional set of filtering UI which can appear on top of any matrix
    public clsTranslation QuickFilterGroup { get; set; } //multiple fields are grouped to form a single set of radioButtons (or checkboxes) - this is both the title and the 'grouper'
    public string QuickFilterUItype { get; set; } //How to presents this fields quickfilter UI  Check,Radio, with/without  '
    public bool CanUserSelect { get; set; }
    public int? LinkedFieldID { get; set; }
    public Dictionary<int, clsRegion> ValidRegions { get; set; }
    public bool HMC_MutuallyExclusive { get; set; }
    public bool InvertFilterOrder { get; set; }

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
    public clsField(clsScreen screen, int ID, string propertyname, string lookupof, clsTranslation labeltext, string helptext, clsValidation validation, clsInputType inputtype, int length, int order, float width, float height, string defaultvalue, bool visibleList, bool visiblePage, bool visibleSquare, string defaultFilter, string defaultSort, int priority, clsTranslation quickFilterGroup, string quickFilterUIType, bool canUserSelect, Nullable<int> LinkedFieldID, bool Grow, string DefaultFilterValues, bool FilterVisible, bool HMC_MutuallyExclusive, bool InvertFilterOrder)
    {

        Screen.Fields.Add(ID, this);

        this.Screen = screen;
        this.ID = ID;
        this.propertyName = propertyname; //This is the property this field edits - it may be a simple Element, string, integer etc,
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
        if (!Screen.i_field_property.ContainsKey(this.propertyName))
        {
            Screen.i_field_property.Add(this.propertyName, this);
        }
        else
        {
            ErrorLog.Add(new Exception("Screen " + Screen.displayName(English) + " contains multiple references to " + this.propertyName));
        }

        oPropertyName = propertyname;

    }

    public Panel emptyCell(bool collapsed, bool editor)
    {
        return emptyCell(collapsed, null, editor);
    }
    public Panel emptyCell(bool collapsed, double? width, bool editor)
    {
        Panel returnValue = default(Panel);

        //the width of a cell is a bit of a red herring -
        //becuase although the matrix cells are inline-block - they are positioned absolutely (to avoid wrapping)  - it would be nicer if they were in the flow (positioned relative)

        returnValue = new Panel();

        if (editor)
        {
            returnValue.CssClass = "editCell";
        }
        else
        {
            returnValue.CssClass = "matrixCell";
        }

        float cellWidth = width == null ? this.width : width;

        if (collapsed)
        {
            cellWidth = System.Convert.ToSingle(collapsedColumnWidth);
        }
        returnValue.Attributes("style") = "width:" + System.Convert.ToString(cellWidth) + "em;";
        if ((width == null ? this.width : width) == 0)
        {
            returnValue.Attributes("style") += "background-color:#ff9090;";
        }

        //we must put *something* in the DIV or .net won't render it
        //Dim lt As Literal
        //lt = New Literal
        //lt.Text = "&nbsp;"
        //emptyCell.Controls.Add(lt)

        return returnValue;
    }

    public Literal NoPreferenceRadioButton(string path, List<clsFilter> filters)
    {

        Literal rb = new Literal();
        rb.Text = "";
        if (Filters.Count)
        {

            rb.Text = "<input type=~radio~ onclick=~{getBranches(\'path=" + path + "&cmd=removeFilter&filterParams=" + System.Convert.ToString(this.ID) + "|";
            rb.Text += string.Join("|", (from f in filters select f.Code).ToArray) + "\')}~>";
            rb.Text += "No preference</input>";
            rb.Text = rb.Text.Replace("~", '\u0022');

            return rb;
        }


    }


    public TextBox SortPriorityTextBox(object sort, ref string Current) //DropDownList
    {
        TextBox returnValue = default(TextBox);

        //builds a standard sort priority DDL, and Selects the current value in it for field
        //NB: Sort$ (the current sort parameters)  is module level

        returnValue = new TextBox(); //DropDownList
        returnValue.CssClass = "sortPriorityTextBox";

        //are we currently sorting by this column (at all)
        int c = 0;

        Current = "-";
        returnValue.Text = " -";
        foreach (var p in Strings.Split(System.Convert.ToString(sort), ","))
        {
            c++; //this is the sort priorty 1,2,3,4 or 5  (where it appears in the request.sort parameter comma seperated list)
            if (p.ToString().IndexOf("[" + this.propertyName + "]") + 1 > 0)
            {
                //yes we are sorting by this
                if (p.ToString().IndexOf("] ASC") + 1 > 0)
                {
                    //SortPriority.SelectedValue = Trim$(c.ToString) & "A"
                    returnValue.Text = c.ToString().Trim() + "⇧";
                    Current = (c).ToString().Trim() + "A"; //we're currently soring price this column with priority C, ascending
                }
                else
                {
                    //SortPriority.SelectedValue = Trim$(c.ToString) & "D"
                    returnValue.Text = c.ToString().Trim() + "⇩";
                    Current = (c).ToString().Trim() + "D"; //we're currently soring price this column with priority C, descending
                }

            }
        }

        return returnValue;
    }

    public DropDownList OperatorDDL()
    {
        DropDownList returnValue = default(DropDownList);

        //Returns a dropdown list filled with  approriate set of logical operators for filtering based on this inputType
        //Seletcts the current value in the list based on whats in the filter$

        //See IQ.loadfilters for a better undrestanding of how the filters work
        //eg. filters.Add("PM20", "[col]>=[filterValue]*.8 and [col]<=[filterValue]*1.2")

        returnValue = new DropDownList();

        DropDownList with_1 = returnValue;


        // .Items.Add(New ListItem("Equals", "EQ"))
        switch (Strings.LCase(System.Convert.ToString(this.InputType.code)))
        {

            //WATCH OUT WHEN USING ' (single quotes)   - you need to ESQ() them !!!

            case "string":
            case "one":
            case "nullstring":
            case "translate":
                //one's are treated as strings - but should ultimately become an 'is/in' filter

                with_1.Items.Add(new ListItem("Is", "EQ")); //Note - the values (typed in the textboxes) get enclosed in single quotes
                //.Items.Add(New ListItem("Ends with", "EW"))
                //.Items.Add(New ListItem("Contains", "CN"))
                //.Items.Add(New ListItem("Only", "ONLY"))
                with_1.Items.Add(new ListItem("Excluding", "EX"));
                break;

            case "many":
                //for 'many' colums - we can only filter by the number of children presently)
                //again, ultimately a 'has' filter - would be nice
                with_1.Items.Add(new ListItem("having n", "HN"));
                with_1.Items.Add(new ListItem("having n or more", "HNM"));
                with_1.Items.Add(new ListItem("having n  less ", "HNL"));
                break;
            case "int32":
            case "nullint":
            case "single":
            case "customerprice":
            case "nullprice":
                with_1.Items.Add(new ListItem("greater than or equal", "GE"));
                with_1.Items.Add(new ListItem("equals", "EQ"));
                with_1.Items.Add(new ListItem("less than or equal", "LE"));
                with_1.Items.Add(new ListItem("plus or minus 10%", "PM10"));
                with_1.Items.Add(new ListItem("plus or minus 20%", "PM20"));
                break;
            case "date":
                with_1.Items.Add(new ListItem("before", "B4"));
                with_1.Items.Add(new ListItem("after", "AFT"));
                with_1.Items.Add(new ListItem("on", "ON"));
                break;
            case "boolean":
                with_1.Items.Add(new ListItem("Ticked", "T"));
                with_1.Items.Add(new ListItem("UnTicked", "F"));
                break;
            case "icon":
                with_1.Items.Add(new ListItem("With", "WITH")); //TODO with/without
                with_1.Items.Add(new ListItem("Without", "WITHOUT")); //TODO with/without
                break;
            default:
                Interaction.Beep();
                break;
        }



        return returnValue;
    }



    //Public Sub New(screen As clsScreen, propertyName As String, lookupOf As String, embedScreen As clsScreen, labelText As String, helpText As String, validation As clsValidation, inputType As clsInputType, length As Integer, order As Integer, width As Single, defaultvalue As String, visible As Boolean)

    //Public Sub New(screen As clsScreen, propertyName As String, propertyClass As String, lookupOf As String, labelText As String, helpText As String, validation As clsValidation, inputType As clsInputType, _
    //                           length As Integer, order As Integer, width As Single, height As Single, defaultValue As String, visibleList As Boolean, visiblePage As Boolean, defaultFilter$, defaultSort$)
    public clsField(clsScreen screen, string propertyName, string lookupOf, clsTranslation labelText, string helpText, clsValidation validation, clsInputType inputType, int length, int order, float width, float height, string defaultValue, bool visibleList, bool visiblePage, object defaultFilter, object defaultSort, int priority, clsTranslation quickFilterGroup, string quickFilterUIType, bool CanUserSelect, int? LinkedFieldID, bool FilterVisible)
    {
        object sql = null;

        //   sql$ = "INSERT INTO [field] (fk_screen_id,property,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],fk_screen_id_embed,[width],defaultValue,visible) "
        //   sql$ &= "VALUES (" & screen.ID & "," & da.SqlEncode(propertyName) & "," & da.SqlEncode(labelText) & "," & da.SqlEncode(helpText) & "," & NullID(validation) & ","
        //   sql$ &= SqlEncode(lookupOf) & "," & inputType.ID & "," & length & "," & order & "," & NullID(embedScreen) & "," & width & "," & da.SqlEncode(defaultvalue) & "," & IIf(visible, 1, 0) & ");"

        //    sql$ = "INSERT INTO [field] (fk_screen_id,property,propertyClass,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],[width],height,defaultValue,visibleList,VisiblePage,defaultfilter,defaultsort) "
        //    sql$ &= "VALUES (" & screen.ID & "," & da.SqlEncode(propertyName) & "," & da.SqlEncode(propertyClass) & "," & da.SqlEncode(labelText) & "," & da.SqlEncode(helpText) & "," & NullID(validation) & ","
        //    sql$ &= SqlEncode(lookupOf) & "," & inputType.ID & "," & length & "," & order & "," & width & "," & height & "," & da.SqlEncode(defaultValue) & "," & IIf(visibleList, "1", "0").ToString & "," & IIf(visiblePage, "1", "0").ToString & "," & da.SqlEncode(defaultFilter) & "," & da.SqlEncode(defaultSort) & ");"

        sql = "INSERT INTO [field] (fk_screen_id,property,fk_translation_key_label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],[width],height,defaultValue,visibleList,VisiblePage,defaultfilter,defaultsort,[priority],FK_Translation_key_WidgetGroup,WidgetUI,CanUserSelect,VisibleSquare,Grows,DefaultFilterValues) ";
        sql += "VALUES (" + Screen.ID + "," + da.SqlEncode(propertyName) + "," + labelText.Key + "," + da.SqlEncode(helpText) + "," + NullID(validation) + ",";
        sql += da.SqlEncode(lookupOf) + "," + InputType.ID + "," + System.Convert.ToString(length) + "," + System.Convert.ToString(order) + "," + System.Convert.ToString(width) + "," + System.Convert.ToString(height) + "," + da.SqlEncode(defaultValue) + "," + (visibleList ? "1" : "0").ToString() + "," + (visiblePage ? "1" : "0").ToString() + ",";
        sql += da.SqlEncode(defaultFilter) + "," + da.SqlEncode(defaultSort) + "," + System.Convert.ToString(priority) + "," + TranslationKey(quickFilterGroup) + "," + da.SqlEncode(quickFilterUIType) + "," + da.SqlEncode(CanUserSelect) + "," + da.SqlEncode(visibleSquare) + "," + da.SqlEncode(Grow) + "," + da.SqlEncode(DefaultFilterValues) + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        Screen.Fields.Add(this.ID, this);

        this.Screen = screen;
        this.propertyName = propertyName; //This is the property (on the object) this field edits (or displays) - it may be a simple Element, string, integer etc,
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
        this.defaultFilter = System.Convert.ToString(defaultFilter);
        this.defaultSort = System.Convert.ToString(defaultSort);
        this.priority = priority;
        this.QuickFilterGroup = quickFilterGroup;
        this.QuickFilterUItype = quickFilterUIType;
        this.CanUserSelect = CanUserSelect;
        this.LinkedFieldID = LinkedFieldID;
        this.DefaultFilterValues = DefaultFilterValues;
        this.Grow = Grow;
        this.FilterVisible = FilterVisible;

        iq.Fields.Add(this.ID, this);
        if (this.propertyName != "")
        {
            this.Screen.i_field_property.Add(this.propertyName, this);
        }
        //Me.Screen.Fields.Add(Me.ID, Me)

        oPropertyName = propertyName;

    }

    public dynamic Insert(ref List<string> errorMessages)
    {

        //Return New clsField(Me.Screen, Me.propertyName, Me.PropertyClass, Me.lookupOf, Me.labelText, Me.helpText, Me.Validation, Me.InputType, Me.length, Me.order, Me.width, Me.height, Me.defaultValue, Me.visibleList, Me.visiblePage, Me.defaultFilter, Me.defaultSort)
        return new clsField(this.Screen, this.propertyName, this.lookupOf, this.labelText, this.helpText, this.Validation, this.InputType, this.length, this.order, this.width, this.height, this.defaultValue, this.visibleList, this.visiblePage, this.defaultFilter, this.defaultSort, this.priority, this.QuickFilterGroup, this.QuickFilterUItype, this.CanUserSelect, this.LinkedFieldID, this.FilterVisible);

    }

    public void demote()
    {

    }

    /// <summary>Returns a singe field value as Text (quoted for 'strings') suitable for a CSV export</summary>

    public string CSV(object obj, string path, clsAccount buyerAccount, clsLanguage language, clsScreenHeader MatrixHeader, bool collapsed, HashSet<string> foci, ref List<string> errorMessages, UInt64 lid, double? width, bool export = false)
    {

        //this has all become a little messy - Cellvalue could do with a refactor and some of these wrappers could be removed

        string result = string.Empty;
        Panel pnl = new Panel(); //throwaway (in this case) panel for UI

        //this is populated                            \/ (byref)
        clsTranslation temp_Translation = null;
        clsUnit temp_Unit = null;
        CellValue(obj, path, buyerAccount, language, ref result, ref pnl, collapsed, ref temp_Translation, ref temp_Unit, foci, ref errorMessages, lid, export);
        if (!string.IsNullOrWhiteSpace(result))
        {
            result = result.Replace("&amp;", "&");
        }
        return result;

    }

    public Panel CellUI(object obj, object path, clsAccount buyerAccount, clsLanguage language, clsScreenHeader MatrixHeader, bool collapsed, HashSet<string> foci, ref List<string> errorMessages, UInt64 lid, double? width)
    {
        Panel returnValue = default(Panel);

        returnValue = this.emptyCell(collapsed, width, false); //New Panel

        string numericValue = "";
        string csv = ""; //throwaway (in this case) string for CSV export
        //                                                                  \/-  This gets POPULATED
        Panel temp_UI = returnValue;
        clsTranslation temp_Translation = null;
        clsUnit temp_Unit = null;
        numericValue = System.Convert.ToString(CellValue(obj, System.Convert.ToString(path), buyerAccount, language, ref csv, ref temp_UI, collapsed, ref temp_Translation, ref temp_Unit, foci, ref errorMessages, lid));

        //If numericValue IsNot Nothing Then value = numericValue Else value = "\'" & value & "\'" 'Escape the quotes we require for filtering against literal strings

        //rdus - replaces dows with underscores
        returnValue.ID = rdus(path) + "_" + System.Convert.ToString(this.ID);

        if (!collapsed)
        {

            List<string> fbids = new List<string>(); //filter image button ID's
            if (this.defaultFilter != "")
            {
                foreach (var v in this.defaultFilter.Split(','))
                {
                    fbids.Add("ctl00_FIB_" + v);
                }
            }
            //Dim omo$

            if (fbids.Count > 0)
            {

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
                returnValue.ToolTip = Xlt("Click to filter the list based on this value", language);
                returnValue.Attributes("onmousedown") = "arrowClick(onSpeechBubble,\'" + MatrixHeader.path + "\',\'" + System.Convert.ToString(this.ID) + "\',\'" + numericValue.ToString() + "\',\'" + string.Join(",", fbids.ToArray) + "\',\'" + returnValue.ID + "\',this);";

                returnValue.Attributes("class") += "handPointer";

            }
        }

        return returnValue;
    }

    public dynamic CellValue(object obj, string path, clsAccount buyerAccount, clsLanguage language, ref string csv, ref Panel UI, bool collapsedColumn, ref clsTranslation Translation, ref clsUnit Unit, HashSet<string> foci, ref List<string> errorMessages, UInt64 lid, bool export = false)
    {
        dynamic returnValue = default(dynamic);

        //in the customer facing UI the OBJ is a branch - but when used from the editor -
        //it can be anything (an account, country, language etc,etc)

        //returns a piece of UI (label, claendar, checkbox, graph, textbox  etc) in a placeholder - and a numeric 'INT64' value for filtering and sorting

        csv = "Not Set"; //should *always* get overrridden

        Translation = null; //For cells containing a translation - we return that too (for quickfilters)
        returnValue = long.MinValue; //Single.MinValue
        bool IsBoolean = false;
        string propName = this.propertyName;
        if (this.propertyName.Length > 2 && this.propertyName.Substring(this.propertyName.Length - 3, 3) == "<B>")
        {
            //This is a boolean field, yes has content or no doesn't only
            IsBoolean = true;
            propName = this.propertyName.Substring(0, this.propertyName.Length - 3);
        }
        object valueObject = null;
        //If LCase(Me.propertyName).Contains("dmr") Then Stop

        if (UI != null)
        {
            UI.CssClass += " cls_" + this.labelText.text(English).Replace(" ", "_"); //include the fields label text name as a CSS Class
        }

        long temp_Value = returnValue;
        if (SpecialColumn(this, propName, obj, path, buyerAccount, language, UI, ref temp_Value, ref Translation, ref csv, ref Unit, foci, ref errorMessages, lid, valueObject, export))
        {
            if (valueObject != null)
            {
                returnValue = valueObject;
            }
            //NB: CSV was set above (byRef) - and is CSV()'d already

            if (collapsedColumn && UI != null)
            {
                UI.Controls.Clear();
                Label lbl = new Label();
                lbl.Text = "-";
                UI.Controls.Add(lbl);
            }

        }
        else
        {
            //it WASN'T a 'special' column (price/stock/memory/display/supplychain)

            Label lbl = default(Label);
            lbl = new Label();
            //            lbl.Style("width") = "100%"
            // lbl.Style("whitespace") = "nowrap"
            lbl.Style("overflow") = "hidden";
            lbl.Style("word-wrap") = "break-word";
            if (UI != null)
            {
                UI.Controls.Add(lbl);
            }

            object tobj = null; //the object at the end of the walk

            //Failing to get CP_DMR attribute
            tobj = Reflection.WalkPropertyValue(obj, propName, errorMessages); //this is probably slow .. TODO consider short term cacheing at this level  (or possible better within walkproperty .. a cache of paths to recently walked values would speed up rows beyond the visible matrix too

            if (tobj is string)
            {

                returnValue = tobj;
                csv = System.Convert.ToString(Utility.CSV(tobj));
                lbl.Text = tobj;
                lbl.Text = Strings.Replace(System.Convert.ToString(lbl.Text), " ", "&nbsp;");
                lbl.Text = Strings.Replace(System.Convert.ToString(lbl.Text), "-", "&#8209");

            }
            else if (tobj is int || tobj is Single)
            {

                csv = System.Convert.ToString(System.Convert.ToInt32(tobj)); //.ToString("D") 'it's the SERVERS locale/regional settings here - so we shold get 'proper' decimal points and thousands sepearators

                returnValue = tobj; //CInt(Val(lbl.Text)) 'well, that was easy - strings probably need more careful hadling
                lbl.Text = Strings.Replace(System.Convert.ToString(lbl.Text), " ", "&nbsp;");
                lbl.Text = Strings.Replace(System.Convert.ToString(lbl.Text), "-", "&#8209");

            }
            else if (tobj is clsTranslation)
            {
                Translation = (clsTranslation)tobj;
                lbl.Text = Translation.text(language);
                csv = System.Convert.ToString(Translation.text(language));
                returnValue = Translation.SortValue(language);

                lbl.Text = Strings.Replace(System.Convert.ToString(lbl.Text), " ", "&nbsp;");
                //lbl.Text = Replace(lbl.Text, "-", "&#8209")

            }
            else if (tobj is clsProductAttribute) //the thing we've walked to is an attribute - like Disk RPM  - Also used for some product attributes which are presented as ICONS
            {

                clsProductAttribute prodatt = default(clsProductAttribute);
                prodatt = (clsProductAttribute)tobj;


                //   If prodatt.Product.Attributes Then


                Translation = prodatt.Translation;
                Unit = prodatt.Unit;


                if (Translation != null)
                {

                    //Any translation (and its ORDER will override any Numeric value (for the purposes of sorting and filtering)

                    returnValue = Translation.SortValue(language); //some attributes (such as the RPM of an Drive have both text, and a numeric value)


                    string text = System.Convert.ToString(Translation.text(language));
                    csv = System.Convert.ToString(Utility.CSV(text));

                    if (Strings.LCase(System.Convert.ToString(this.InputType.code)) == "sausage") //values (productAttributes) to be displayed as sausage buttons have a translation which is their face text
                    {

                        returnValue = System.Convert.ToInt32(prodatt.NumericValue);
                        if (UI != null)
                        {
                            Panel wb = new Panel();
                            wb.CssClass = "sausageButton";
                            if (QuickFilterUItype != "TKEY" && prodatt.NumericValue == 0)
                            {
                                wb.CssClass += " greyed";
                            }

                            lbl = new Label();
                            lbl.Text = text;
                            lbl.ToolTip = prodatt.Attribute.displayName(buyerAccount.Language); //Use the full name of the productattribute as the tooltip

                            wb.Controls.Add(lbl);
                            UI.Controls.Add(wb);

                            //                            If prodatt.NumericValue <> 0 Then Stop

                            lbl.ToolTip += " (" + returnValue.ToString() + ")";

                        }
                    }
                    else
                    {
                        //this is a vanilla (non quickfilter UI)  ProducAttribute (WITH a translation) - we use its numericvalue if present
                        lbl.Text = text;
                        lbl.ToolTip = Translation.text(language);
                        //If prodatt.NumericValue > 0 Then  'we want the translations sortvalue for product attributes with no numeric value '@@@
                        // CellValue = CInt(prodatt.NumericValue)
                        //End If
                    }
                }
                else
                {
                    //numeric attribute (one where there is no text/translation) and units (and possibly conversions)
                    if (InputType.code == "rounddecimal")
                    {
                        returnValue = Math.Round(System.Convert.ToDecimal(prodatt.NumericValue), 2);
                    }
                    else
                    {
                        returnValue = System.Convert.ToInt32(prodatt.NumericValue);
                    }

                    // Handle sausages
                    if (Strings.LCase(System.Convert.ToString(this.InputType.code)) == "sausage")
                    {

                        if (UI != null)
                        {
                            Panel wb = new Panel();
                            wb.CssClass = "sausageButton";
                            if (QuickFilterUItype != "TKEY" && prodatt.NumericValue == 0)
                            {
                                wb.CssClass += " greyed";
                            }

                            lbl = new Label();
                            lbl.Text = returnValue.ToString();
                            lbl.ToolTip = prodatt.Attribute.displayName(buyerAccount.Language); //Use the full name of the productattribute as the tooltip

                            wb.Controls.Add(lbl);
                            UI.Controls.Add(wb);

                            lbl.ToolTip += " (" + returnValue.ToString() + ")";
                        }

                    }
                    else
                    {
                        lbl.Text = returnValue.ToString();
                    }

                    csv = System.Convert.ToString(prodatt.NumericValue.ToString()); //this is an integer (may need to change)

                    if (prodatt.Unit.Code != "txt")
                    {
                        lbl.Text += " " + prodatt.Unit.Symbol; // todo Symbol
                    }

                    //Note - we don't need to check the 'value' of Icons - becuase just having it indicates a 'true'
                    if (Strings.LCase(System.Convert.ToString(this.InputType.code)) == "icon")
                    {
                        if (UI != null)
                        {

                            Image image = new Image();
                            //We extract the code form the parentheses at the end of the propertname - to use as the image name
                            image.ImageUrl = imagebase + "/images/icons/icon_opttype_" + DeBracket(propName, "(", ")")[0] + ".png";

                            //the acutal value becomes a tooltip
                            if (lbl.Text == "-1")
                            {
                                lbl.Text = Yes.text(language); //Quick fix
                            }
                            if (lbl.Text == "0")
                            {
                                lbl.Text = No.text(language);
                            }

                            image.ToolTip = lbl.Text;
                            lbl.Text = "";
                            UI.Controls.Add(image);
                        }
                    }
                }

            }
            else if (this.InputType.code == "one")
            {
                lbl.Text = tobj.displayname(buyerAccount.Language);
                csv = System.Convert.ToString(Utility.CSV(lbl.Text));
                returnValue = tobj.id; //the value is the ID of the object referenced (the foreign key) - used for filtering in the editor

            }
            else if (tobj == null)
            {
                csv = System.Convert.ToString(Utility.CSV("-"));
                lbl.Text = "-";
                returnValue = long.MinValue; //Single.MinValue
            }
            else
            {
                errorMessages.Add("unknown object type " + tobj.GetType().ToString() + "|" + path + "|" + propName);
            }
        }

        if (IsBoolean)
        {
            returnValue = returnValue == long.MinValue || returnValue == "0" ? 0 : 1;
            if (UI != null)
            {
                Literal lit = new Literal();
                Literal l = UI.Controls(0);
                //lit.Text = "<div>" + If(CellValue = Int64.MinValue Or CellValue = "0", _
                //                        No.text(language), _
                //                        Yes.text(language)) + "</div>"

                lit.Text = "<div>" + (returnValue == long.MinValue || returnValue == "0" ? "<img src=\'images/navigation/cross.png\'/>" : "<img src=\'images/navigation/cross.png\'/>") + "</div>";


                csv = System.Convert.ToString(Utility.CSV(returnValue == long.MinValue || returnValue == "0" ? (No.text(language)) : (Yes.text(language))));

                if (UI != null)
                {
                    UI.Controls.Clear();
                    UI.Controls.Add(lit);
                }
            }
        }

        if (!(UI == null))
        {
            if (collapsedColumn)
            {
                UI.Controls.Clear();
            }
            Label lbl = new Label();
            lbl.Text = "-";
            UI.Controls.Add(lbl);
        }


        return returnValue;
    }


    public bool SpecialColumn(clsField f, string propName, object obj, object path, clsAccount buyeraccount, clsLanguage language, Panel UI, ref long Value, ref clsTranslation Translation, ref string csv, ref clsUnit unit, HashSet<string> foci, ref List<string> errormessages, UInt64 lid, object valueObject, bool export = false)
    {
        bool returnValue = false;

        //value will *very often* be a string representation of a 64bit Number

        //Populates the UI and a numeric value for soring and filtering
        //returns TRUE if is *is* a 'special' Column

        Label lbl = new Label();
        lbl.Style("width") = "100%";
        lbl.Text = "-";

        string bp = System.Convert.ToString(propName.Split('(')(0)); // find the first segment before any open parethesis

        returnValue = true; //The CASE ELSE sets this to FALSE for 'non-speicial' columns

        unit = null;

        csv = System.Convert.ToString(Utility.CSV("unhandled \'special\'column " + f.propertyName));

        switch (bp.ToLower().Trim())
        {

            case "slots": //broken by the new major/minor slot type

                //looks on child branches for slots 'gives' of the specified type (becuase the slots are in the chassis/mobo)

                //Returns a dictionary of the number of slots by major type - recursing form the current branch - does not cross systems
                Dictionary<string, int> majorSlots = new Dictionary<string, int>();
                ((clsBranch)obj).MajorSlots(majorSlots); //Call the majorSlots (recursive) method on the branch object - to fill the dictionary

                string majorSlotType = System.Convert.ToString(DeBracket(propName, "(", ")")[0]); //extract the slot type from the brackets eg slots(CPU)

                if (majorSlots.ContainsKey(majorSlotType))
                {
                    lbl.Text = (majorSlots[majorSlotType]).ToString();
                    if (UI != null)
                    {
                        UI.Controls.Add(lbl);
                    }
                    Value = long.Parse(majorSlots[majorSlotType].ToString());
                    csv = Value;
                }
                else
                {
                    if (UI != null)
                    {
                        UI.Controls.Add(lbl);
                    }
                    Value = long.Parse(long.MinValue.ToString());
                    csv = System.Convert.ToString(Utility.CSV("-"));
                }


                unit = iq.i_unit_code("num");
                break;

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

            case "xtext": //external text (formely TextExternal)

                clsProduct product_1 = ((clsBranch)obj).Product;
                //value = 0 ' we must return *something* (even if there was no xtext) to indicate this was a specialcolumn

                csv = "";

                List<ClsValidationMessage> vmsgs = product_1.getXtext(path, null);
                if (vmsgs.Count == 0)
                {
                    if (UI != null)
                    {
                        UI.Controls.Add(lbl);
                    }
                }
                else
                {
                    foreach (var msg in vmsgs)
                    {
                        if (UI != null)
                        {
                            UI.Controls.Add(msg.CompactUI(language));
                        }
                        if (msg.severity > Value)
                        {
                            Value = System.Convert.ToInt64(msg.severity); //Return the max severity from the set of Xtexts as the (sortable) value
                        }
                        csv += msg.message.text(language) + "\r\n";
                    }
                }

                csv = System.Convert.ToString(Utility.CSV(csv));

                Translation = null; // if we were going to sort xTexts by anything, it would be their Severity - not alphabetically
                break;

            case "scheme":
                clsProduct product_2 = default(clsProduct);
                product_2 = ((clsBranch)obj).Product;

                string code_1 = System.Convert.ToString(DeBracket(propName, "(", ")")[0]); //extract the scheme code

                if (!iq.i_scheme_code.ContainsKey(code_1))
                {
                    if (errormessages.Count < 10)
                    {
                        errormessages.Add("Unknown loyalty scheme code " + code_1);
                    }
                }
                else
                {
                    foreach (var scheme in iq.i_scheme_code(code_1))
                    {
                        if (scheme.Region.Encompasses(buyeraccount.SellerChannel.Region))
                        {
                            if (product_2.Points.ContainsKey(scheme))
                            {
                                Value = long.Parse(product_2.Points(scheme).ToString());
                            }
                            else
                            {
                                Value = long.Parse("0");
                            }

                            if (!(UI == null))
                            {
                                if ((int)Value > 0)
                                {
                                    Literal lit = new Literal();
                                    lit.Text = "<div class=\'loyaltyPoints\'>" + Value.ToString() + "</div>";
                                    UI.Controls.Add(lit);
                                }
                            }
                        }
                    }
                }

                if (Value == long.MinValue)
                {
                    csv = System.Convert.ToString(0);
                }
                else
                {
                    csv = Value.ToString();
                }


                Translation = null;
                break;

            case "promo":
            case "promos":

                clsProduct product_3 = default(clsProduct);
                clsBranch branch = (clsBranch)obj;
                product_3 = ((clsBranch)obj).Product;

                string code = System.Convert.ToString(DeBracket(propName, "(", ")")[0]); //extract the scheme code

                csv = "";

                switch (code)
                {

                    case "A":
                        if (branch.hasAvalanche(buyeraccount.BuyerChannel, path))
                        {
                            Value = long.Parse("1");
                            Literal lit = new Literal();
                            lit.Text = "<div class=\'avalancheGrid\'>AV</div>";
                            if (UI != null)
                            {
                                UI.Controls.Add(lit);
                            }
                            Translation = iq.AddTranslation("Avalanche", English, "Promos", 0, null, 0, false);
                            csv = System.Convert.ToString(Utility.CSV(code));
                        }
                        break;


                    case "F":

                        if (branch.hasFlexAttach(buyeraccount, path, foci, errormessages))
                        {
                            Value = long.Parse("1");
                            Literal lit = new Literal();
                            lit.Text = "<div class=\'flexF\'>F</div>";
                            if (UI != null)
                            {
                                UI.Controls.Add(lit);
                            }
                            Translation = iq.AddTranslation("Flex Attach", English, "Promos", 0, null, 0, false);
                            csv = System.Convert.ToString(Utility.CSV(code));
                        }
                        break;

                    case "B":
                        break;

                    case "R":
                        if (product_3.hasPromo("R", buyeraccount.BuyerChannel.Region))
                        {
                            Value = long.Parse("1");
                            Literal lit = new Literal();
                            lit.Text = "<div class=\'recetaR\'><span>&#x2713;</span></div>";
                            if (UI != null)
                            {
                                UI.Controls.Add(lit);
                            }
                            unit = iq.i_unit_code("txt");
                            Translation = iq.AddTranslation("Receta", English, "Promos", 0, null, 0, false);
                            csv = System.Convert.ToString(Utility.CSV(code));
                        }
                        break;
                    default:
                        break;

                }
                break;


            case "customerprice":

                clsProduct product_4 = default(clsProduct);
                product_4 = ((clsBranch)obj).Product;

                if (product_4 == null)
                {
                    return null;
                }
                if (product_4.SKU != "") //product.i_Attributes_Code.ContainsKey("MfrSKU") Then
                {

                    List<clsPrice> prices = default(List<clsPrice>);
                    if (product_4.SKU.StartsWith("###") || product_4.SKU.ToUpper.StartsWith("FAKE"))
                    {
                        prices = new List<clsPrice>(); //no prices for fake products (especially DONT call the webservice!)
                    }
                    else
                    {
                        //Dim withoutWebService As Integer = buyeraccount.SellerChannel.priceConfig And Not 8
                        prices = product_4.GetPrices(buyeraccount, buyeraccount.SellerChannel.priceConfig, iq.AllVariants, errormessages, true); //this can return multiple prices (for multiple variants - different warehouses, localisations etc)
                    }

                    if (prices.Count == 0 || prices[0] == null)
                    {
                        Value = long.Parse(long.MinValue.ToString()); //POA
                    }
                    else
                    {

                        clsPrice lowest = Utility.LowestPrice(prices); //they will all be in the same currency (that of the buyer account)

                        // If UI IsNot Nothing Then UI.Controls.Add(CType(obj, clsBranch).BuyUI(buyeraccount, path))
                        //Dim quoteLocked As Boolean = False ' Check for if a quote was exported to remove the +button
                        //If iq.sesh(lid, "QuoteLocked") IsNot Nothing Then
                        //    quoteLocked = CBool(iq.sesh(lid, "QuoteLocked"))

                        //End If

                        if (UI != null)
                        {
                            if (lowest.SKUVariant != null)
                            {
                                Panel prp = lowest.Ui(buyeraccount, 1, lid); //has its own width and inline-block - this panel (div)  is entirely replaced by the arriving (asynch) prices
                                UI.Controls.Add(prp);

                                // If there is a current quote, work out whether it's HPI or HPE
                                clsQuote quote = default(clsQuote);
                                object quoteSplit = Manufacturer.Unknown;
                                if (iq.sesh(lid, "QuoteID") != null)
                                {
                                    if (iq.sesh(lid, "AgentAccount") != null)
                                    {
                                        clsAccount agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));
                                        quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"));
                                        quoteSplit = quote.QuoteSplit;
                                    }
                                }

                                // Work out whether adding is enabled according to the HPE/HPI split
                                bool addEnabled = true;
                                if (product_4.isSystem(path))
                                {
                                    if (!(quoteSplit == Manufacturer.Unknown))
                                    {
                                        addEnabled = quoteSplit == product_4.Manufacturer;
                                    }
                                }

                                // Set up the message to display if the user attempts to create a mixed quote
                                string splitMessage = string.Empty;
                                if (!addEnabled)
                                {
                                    splitMessage = System.Convert.ToString(GetSplitMessage(quoteSplit, buyeraccount.Language));
                                }

                                TextBox TB_QTY = new TextBox();
                                TB_QTY = new TextBox();
                                TB_QTY.ID = "qtytxt." + System.Convert.ToString(path);
                                TB_QTY.Attributes("style") = "left:5em;width:1.5em;margin-left:.5em;height .9em;margin-top:.05em;border:solid silver 1px;"; //did have float:left
                                if (addEnabled)
                                {
                                    TB_QTY.CssClass = "qty";
                                    TB_QTY.Attributes.Add("onmousedown", "burstBubble(event);");
                                }
                                else
                                {
                                    TB_QTY.CssClass = "qtyDisabled";
                                    TB_QTY.ReadOnly = true;
                                    TB_QTY.Attributes.Add("onmousedown", string.Format("burstBubble(event); displayAddMsg(\'{0}\', \'{1}\');", TB_QTY.ID, splitMessage));
                                }

                                if (!lowest.SKUVariant.Deleted)
                                {
                                    UI.Controls.Add(TB_QTY);

                                    UI.Controls.Add(TreeAddButton(TB_QTY, path, obj, lowest.SKUVariant, buyeraccount.Language, addEnabled, splitMessage));
                                }
                            }
                        }
                        Value = long.Parse((lowest.Price.NumericValue * 100).ToString());
                    }

                    if (Value == long.MinValue)
                    {
                        csv = System.Convert.ToString(Utility.CSV("No Price"));
                    }
                    else
                    {
                        if (export == false)
                        {
                            csv = System.Convert.ToString(Utility.CSV(((double)Value / 100).ToString("N2"))); //format as a number to two decimal places
                        }
                        else
                        {
                            csv = System.Convert.ToString(Utility.CSV(buyeraccount.Currency.Symbol + ((double)Value / 100).ToString("N2"))); //format as a number to two decimal places
                        }
                    }

                }
                else
                {
                    //SKUless product/row
                    lbl.Text = "NO SKU";
                    if (UI != null)
                    {
                        UI.Controls.Add(lbl);
                    }
                    Value = long.MinValue; //Single.MinValue
                }

                Translation = null;
                break;

            case "stock":

                csv = ""; //fix' for 'unhandled specialcoumn stock'

                clsProduct product = default(clsProduct);
                product = ((clsBranch)obj).Product;
                if (product != null)
                {
                    if (product.hasSKU)
                    {
                        string Disp;
                        if (UI == null) //we're just fetching a numeric value (for sorting)
                        {

                            int stockvalue = 0; //TODO -decide if we should sum these take a max or what
                            Disp = System.Convert.ToString(product.CurrentStock(buyeraccount, stockvalue, iq.AllVariants, errormessages)); //SETS (numeric) value - passing nothing totalises the stock of all variant
                            Value = stockvalue;
                            csv = getStock(buyeraccount, stockvalue, export);

                        }
                        else
                        {

                            //add a asynch-refreshabe stock number..
                            List<clsPrice> prices = default(List<clsPrice>);
                            prices = product.GetPrices(buyeraccount, buyeraccount.SellerChannel.priceConfig, iq.AllVariants, errormessages, true);

                            if (prices.Count > 0 && prices[0] != null)
                            {
                                UI.Controls.Add(prices[0].SKUVariant.StockUI(1, string.Empty, buyeraccount.Language, buyeraccount.SellerChannel));
                                int stockvalue = 0;
                                product.CurrentStock(buyeraccount, stockvalue, iq.AllVariants, errormessages); //populate the return value with the numeric stock
                                Value = stockvalue;
                                csv = getStock(buyeraccount, stockvalue, export);


                            }
                            else
                            {
                                lbl.Text = "-";
                                Value = long.Parse(long.MinValue.ToString()); //Single.MinValue
                                UI.Controls.Add(lbl);
                                csv = "";

                            }
                        }
                    }
                    else
                    {
                        //non skud
                        lbl.Text = "-";
                        if (UI != null)
                        {
                            UI.Controls.Add(lbl);
                        }
                        Value = long.Parse(long.MinValue.ToString()); //Single.MinValue

                        csv = string.Empty;
                    }
                }

                Translation = null;
                break;

            case "memory": //Need to re-import to get this to work

                //    Dim segs As Integer = Split(path$, ".").Length 'how many segements were there in the path to this point (becuase we will look in the 'next' segment for a branch called 'memory')
                object preinstalled_1 = GetPreinstalled(lid, System.Convert.ToString(path), obj, buyeraccount, errormessages);

                int mem = 0;
                string bn;

                foreach (var i in preinstalled_1)
                {
                    bn = System.Convert.ToString(i.Branch.Translation.text(English));
                    if (i.Branch.Product != null)
                    {
                        if (i.Branch.Product.ProductType.Code.ToUpper() == "MEM")
                        {
                            if (i.Branch.Product.i_Attributes_Code.ContainsKey("capacity"))
                            {
                                mem = mem + (i.NumPreInstalled) * System.Convert.ToInt32(i.Branch.Product.i_Attributes_Code("capacity")[0].NumericValue); //the capcity attribute of the DIMM
                            }
                        }
                    }
                }

                lbl.Text = mem + " GB";
                if (UI != null)
                {
                    UI.Controls.Add(lbl);
                }

                Value = long.Parse(mem.ToString());
                csv = System.Convert.ToString(Utility.CSV(lbl.Text.ToString()));

                Translation = null;
                unit = iq.i_unit_code("Gbyte");
                break;

            case "drives":

                //Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
                object preinstalled_2 = GetPreinstalled(lid, System.Convert.ToString(path), obj, buyeraccount, errormessages);
                int drives = 0;
                foreach (var i in preinstalled_2)
                {

                    if (i.Branch.Product.ProductType.Code.ToUpper() == "HDD")
                    {
                        drives++;
                    }
                }

                lbl.Text = drives;
                if (UI != null)
                {
                    UI.Controls.Add(lbl);
                }

                Value = long.Parse(drives.ToString());
                csv = drives.ToString();

                Translation = null;
                break;
            //unit = iq.i_unit_code("Gbyte")

            case "drivecapacity":

                //Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
                object preinstalled_3 = GetPreinstalled(lid, System.Convert.ToString(path), obj, buyeraccount, errormessages);

                decimal driveCapacity = 0;
                foreach (var i in preinstalled_3)
                {

                    if (i.Branch.Product.ProductType.Code.ToUpper() == "HDD")
                    {

                        if (i.Branch.Product.i_Attributes_Code.ContainsKey("capacity"))
                        {
                            driveCapacity += System.Convert.ToDecimal(i.Branch.Product.i_Attributes_Code("capacity")[0].NumericValue);
                        }
                    }
                }

                if (driveCapacity == 0)
                {
                    //Go check in attributes and see if its there.....
                    if (((clsBranch)obj).Product.i_Attributes_Code.ContainsKey("capacity"))
                    {
                        driveCapacity = System.Convert.ToDecimal(((clsBranch)obj).Product.i_Attributes_Code("capacity")[0].NumericValue);
                    }
                }
                if (driveCapacity == 0)
                {
                    lbl.Text = "NA";
                }
                else
                {
                    lbl.Text = driveCapacity + " GB";
                }
                if (UI != null)
                {
                    UI.Controls.Add(lbl);
                }

                Value = (long)driveCapacity;
                csv = Value;

                Translation = null;


                unit = iq.i_unit_code("Gbyte");
                break;


            case "supplychain":
                if (obj.parent != null)
                {
                    dynamic with_1 = ((clsBranch)obj).Parent; //The supply chain is this branches parent (systems 'live' within supply chains)
                    lbl.Text = with_1.DisplayName(buyeraccount.Language).ToString();
                    if (UI != null)
                    {
                        UI.Controls.Add(lbl);
                    }
                    // numericValue = .Translation.SortValue(buyeraccount.Language)
                    Translation = with_1.Translation;
                    csv = System.Convert.ToString(Utility.CSV(Translation.text(language)));
                }
                break;


            case "display":

                clsTranslation tl_1 = ((clsBranch)obj).Product.i_Attributes_Code("Display")[0].Translation;
                lbl.Text = tl_1.text(buyeraccount.Language);
                if (UI != null)
                {
                    UI.Controls.Add(lbl);
                }
                Value = long.Parse("0"); //tl.SortValue(buyeraccount.Language)
                csv = System.Convert.ToString(Utility.CSV(tl_1.text(language)));
                break;

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
            case "operatingsystem":

                //Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
                object preinstalled = GetPreinstalled(lid, System.Convert.ToString(path), obj, buyeraccount, errormessages);
                string tl = "";
                foreach (var i in preinstalled)
                {

                    if (i.Branch.Product.ProductType.Code.ToUpper() == "SOF1")
                    {
                        Regex r = new Regex("(Windows [A-z|0-9| ]+ [Foundation]*[Standard]*[Datacenter]*[Essentials]*)[ ]+");
                        tl = System.Convert.ToString(i.Branch.DisplayName(buyeraccount.Language));
                        Match m = r.Match(tl);
                        if (m.Groups.Count > 1)
                        {
                            tl = System.Convert.ToString(m.Groups(1).Value);
                        }
                    }
                }

                lbl.Text = tl;
                if (UI != null)
                {
                    UI.Controls.Add(lbl);
                }
                csv = System.Convert.ToString(Utility.CSV(tl));

                Translation = iq.AddTranslation(tl, buyeraccount.Language, "", 0, null, 0, false);
                break;
            //unit = iq.i_unit_code("Gbyte")

            case "portcountorradio":
                System.Object attrs_1 = ((clsBranch)obj).Product.i_Attributes_Code;
                if (attrs_1.ContainsKey("PriConnectivity"))
                {
                    if (attrs_1["PriConnectivity"].First.Translation.text(English).Contains("802.1"))
                    {
                        lbl.Text = attrs_1["PriConnectivity"].First.Translation.text(English);
                        Translation = attrs_1["PriConnectivity"].First.Translation;
                        csv = System.Convert.ToString(lbl.Text);
                    }
                    else
                    {
                        if (attrs_1.ContainsKey("PriPorts"))
                        {
                            lbl.Text = attrs_1["PriPorts"].First.NumericValue.ToString();
                            Value = System.Convert.ToInt64(attrs_1["PriPorts"].First.NumericValue);
                            Translation = iq.AddTranslation(lbl.Text, English, "", 0, null, 0, false);
                            csv = System.Convert.ToString(attrs_1["PriPorts"].First.NumericValue);
                        }
                    }
                    if (UI != null)
                    {
                        UI.Controls.Add(lbl);
                    }
                }
                break;
            case "formfactorcompressed":
                System.Object attrs = ((clsBranch)obj).Product.i_Attributes_Code;
                if (attrs.ContainsKey("formFactor"))
                {
                    if (attrs["formFactor"].First.Translation.text(English).ToLower.Contains("tower"))
                    {
                        Translation = iq.AddTranslation("Tower", English, "", 0, null, 0, false);
                    }
                    else
                    {
                        Translation = attrs["formFactor"].First.Translation;
                    }
                    lbl.Text = Translation.text(language);
                    csv = System.Convert.ToString(Translation.text(language));
                    if (UI != null)
                    {
                        UI.Controls.Add(lbl);
                    }
                }
                break;
            default:
                returnValue = false; //this wasn't a 'Special' Column
                Translation = null;
                break;

        }

        return returnValue;
    }
    /// <summary>
    /// Gets stock quantity or message in stock or out of stock for binarystock channels.
    /// </summary>
    /// <param name="account">an instance of clsAccount.</param>
    /// <param name="value">An integer value that represents the quantity of stock.</param>
    /// <param name="export">A boolean value that represents if export is being done.</param>
    /// <returns>A string object that represents the text or number to display in quote export.</returns>
    /// <remarks></remarks>
    private string getStock(clsAccount account, long value, bool export)
    {
        string result = string.Empty;
        if (account.SellerChannel.BinaryStock && value > 0)
        {
            result = System.Convert.ToString(InStock.text(account.Language));
        }
        else if (account.SellerChannel.BinaryStock && value <= 0)
        {
            result = System.Convert.ToString(OutOfStock.text(account.Language));
        }
        else if (!account.SellerChannel.BinaryStock && value > 0)
        {
            result = value;
        }
        else if (!account.SellerChannel.BinaryStock && value <= 0)
        {
            if (export)
            {
                result = "0";
            }
            else
            {
                result = value.ToString();
            }
        }
        return result;
    }

    public List<clsQuantity> GetPreinstalled(UInt64 lid, string path, object obj, clsAccount buyeraccount, List<string> errorMessages)
    {
        List<clsQuantity> returnValue = default(List<clsQuantity>);

        //Return New List(Of clsQuantity)  '@@@ This was a test to see how expensive GetPreinstalledRecursive is when creating many matrixheaders/detailsquares
        //the answer is .. not very (not more  than a 10 % improvments when i returned an empty list and did none of the work.
        //Persisting the preInstalls dictionary looked attractive - but any gains would be small



        if (iq.sesh(lid, "preInstalls") == null)
        {
            iq.sesh(lid, "preInstalls") = new Dictionary<string, List<clsQuantity>>();
        }
        Dictionary<string, List<clsQuantity>> preinstalledDic = iq.sesh(lid, "preInstalls");
        if (preinstalledDic.ContainsKey(path))
        {
            returnValue = preinstalledDic[path];
        }
        else
        {
            returnValue = ((clsBranch)obj).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path, errorMessages);
            preinstalledDic[path] = returnValue;
        }
        return returnValue;
    }

    public Panel EditUI(object obj, string path, Panel RowPanel, bool enabled, Web.HttpRequest Request, clsLanguage language, clsAccount buyerAccount, bool PageMode, List<string> errorMessages, clsLanguage translateLanguage)
    {
        Panel returnValue = default(Panel);

        returnValue = this.emptyCell(!this.visibleList, true); //New Panel - has an absolute width as defined in the field (ie. me.width - in ems)
        // EditUI.Attributes("style") &= "overflow:visible;display:inline-block;" 'otherwise we don't see the dropdowns wich are absolutely positioned (taking them out of the flow)

        //returns the UI element for editing this fields property  of the supplied OBJ, using the interface defined in the field F
        //the 'enabled' flag enables the element - and is used for history at the moment (but will be useful for role/right stuff)
        //F may contain some straight propert of obj
        //e.g. DisplayName
        //or some derived property such as attributes(17).name

        TextBox tb = default(TextBox);
        Panel ddl = default(Panel); //DropDownList

        UnitType em;
        em = UnitType.Em;

        switch (Strings.LCase(System.Convert.ToString(this.InputType.code)))
        {

            case "string":
            case "int32":
            case "single":
            case "translate":
            case "nullstring":
            case "nullint":
            case "nullprice":
                //simple textbox

                tb = new TextBox();
                returnValue.Controls.Add(tb);

                tb.Style("width") = (this.width - 0.5).ToString() + "em";
                tb.Style("height") = "100%"; //for textAreas in paged mode
                tb.Style("text-align") = "top";
                tb.Style("display") = "inline-block";

                if (PageMode)
                {
                    if (this.height > 2)
                    {
                        tb.TextMode = TextBoxMode.MultiLine;
                    }
                }

                if (this.InputType.code == "translate")
                {

                    clsTranslation tobj = default(clsTranslation);

                    tobj = (clsTranslation)(Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages));
                    if (tobj == null)
                    {
                        tb.BackColor = Drawing.Color.Pink;
                        tb.Text = "";
                        tb.ToolTip = Xlt("Missing text", language);
                    }
                    else
                    {
                        if (translateLanguage.Code != null)
                        {
                            Label lbl = new Label();
                            returnValue.Controls.Add(lbl);
                            lbl.Style("width") = (this.width - 0.5).ToString() + "em";
                            lbl.Style("height") = "100%"; //for textAreas in paged mode
                            lbl.Style("text-align") = "top";
                            lbl.Style("display") = "inline-block";
                            lbl.Text = tobj.text(language);
                            if (tobj.textTranslation(translateLanguage).Length > 1)
                            {
                                tb.Text = tobj.textTranslation(translateLanguage);
                            }
                            else
                            {
                                tb.BackColor = Drawing.Color.Pink;
                                tb.Text = "";
                                tb.ToolTip = Xlt("Missing text", language);
                            }
                        }
                        else
                        {
                            tb.Text = tobj.text(language);
                        }
                        //Add a button for a new translation
                        returnValue.Controls.Add(editor.MakeButton(true, "Nt", "New translation", "createNewTranslation(\'" + path + "\',\'" + this.propertyName + "\');"));

                        //                  tb.BackColor = Drawing.Color.CornflowerBlue 'remove
                    }

                }
                else if (this.InputType.code == "nullstring")
                {
                    nullableString ns = default(nullableString);
                    ns = (nullableString)(Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages));
                    tb.Text = ns.DisplayValue;

                }
                else if (this.InputType.code == "nullint")
                {
                    NullableInt ni = default(NullableInt);
                    ni = (NullableInt)(Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages));
                    tb.Text = ni.Displayvalue;
                }
                else if (this.InputType.code == "nullprice") //Nullable price
                {
                    NullablePrice np = default(NullablePrice);
                    np = (NullablePrice)(Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages));

                    //every price has a currency - this provides the currency symbol - however the number formatting is determined by buyers,channels, culture
                    tb.Text = np.NumericValue.ToString(); //DisplayPrice(buyerAccount, errorMessages).Text  'currency formatting is is the culture of the seller (the currency alone doesn't give us suffifient info at euro is pan Eueropean - but NL formats €1.000,00 and IE formats €1,000.00
                }
                else
                {
                    //straight' text
                    object ao = Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
                    if (ao == null)
                    {
                        tb.Text = "";
                    }
                    else
                    {
                        tb.Text = ao.ToString();
                    }
                }

                //    If LCase(f.propertyName) = "name" Then title = tb.Text
                tb.ID = "c_" + this.ID.ToString().Trim() + "_" + Strings.Trim(System.Convert.ToString(obj.id.ToString()));
                tb.CssClass = "input"; //This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS) - input carries no styling (neccessarily)
                tb.Enabled = enabled;

                //      tb.ToolTip = tb.ID 'remove
                //        tb.Style("background-color") = "green"

                validationScript = "";
                if (!(this.Validation == null))
                {
                    // passes a regEx and length to the JS to validate
                    // validate will disable all controls with a 'save' class,
                    // and make the textbox 'invalid' class (if it's invalid)
                    object msg = null;
                    msg = this.Validation.ViolationMessage;
                    msg = Strings.Replace(System.Convert.ToString(msg), "\'", "");

                    //must escape the backslashes in regex's (we're creating dynamically)
                    validationScript = "validate(\'" + Strings.Replace(System.Convert.ToString(this.Validation.regEx), "\\", "\\\\") + "\',\'" + System.Convert.ToString(msg) + "\',\'" + tb.ID + "\');";

                }

                if (this.length > 0)
                {
                    validationScript += "validateLength(" + System.Convert.ToString(this.length) + ",\'" + tb.ID + "\');";
                }

                tb.Attributes.Add("onKeyUp", validationScript);
                break;


            case "boolean":
                WebControls.CheckBox cb = default(WebControls.CheckBox);
                cb = new CheckBox();
                returnValue.Controls.Add(cb);
                //cb.Attributes("style") = "width:" & me.width & "em;"

                cb.Checked = System.Convert.ToBoolean(Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages));

                //              \/ Note - checkboxes have different handing in the JS because (stupidly) they have a 'checked' property - and their "value" is always "on"
                cb.ID = "cb_" + this.ID.ToString().Trim() + "_" + Strings.Trim(System.Convert.ToString(obj.id.ToString()));
                //       cb.CssClass = "input"  'This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS)
                cb.InputAttributes.Add("class", "input"); //the above doesn't work - becuase .NET takes it upon itseld to render the checkbox without the the class in a <span> with the class

                cb.Enabled = enabled;
                break;

            case "one":
                object targetObj = null;
                targetObj = Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages); //this object is the 'target' of the foregin key - and contains the selected value

                string controlID = "";
                controlID = "c_" + this.ID.ToString().Trim() + "_" + obj.id;

                ddl = FilledDDL(this, targetObj, language, controlID, enabled, RowPanel, Request("depth"), errorMessages); //Obj2 carries the ID
                returnValue.Controls.Add(ddl);
                break;

            case "many": //this field holds a collection of things (a dictionary) - we render a button - which will embed editing for that dictionary

                //btn = New Button
                //btn.Style("width") = "100%"

                object dic = Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages);
                string txt = "";
                if (dic == null)
                {
                    txt = "Add " + this.labelText.text(language); //propertyName
                }
                else
                {
                    txt = dic.count.ToString() + " " + this.labelText.text(language); //propertyName
                }

                System.String td = path + "." + this.propertyName; //, subPanel.ID
                returnValue.Controls.Add(editor.MakeButton(true, txt, "Show/edit these", editor.EmbedScript(td, td, "", true, true)));

                Panel tp = new Panel();
                tp.ID = td;
                RowPanel.Controls.Add(tp);
                break;




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

            case "date":

                TextBox txtbox = default(TextBox);
                txtbox = new TextBox();
                txtbox.ID = "c_" + this.ID.ToString().Trim() + "_" + Strings.Trim(System.Convert.ToString(obj.id.ToString()));
                //This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS)
                txtbox.CssClass = "input";
                txtbox.Style.Add("width", "55%");

                //txtbox.Width = New Unit(f.width / 3 * 2, ut)

                DateTime dt = default(DateTime);
                dt = System.Convert.ToDateTime(Reflection.WalkPropertyValue(obj, this.propertyName, errorMessages));
                txtbox.Text = Strings.Format(dt, "yy-MM-dd");
                txtbox.Enabled = enabled;

                TextBox timebox = default(TextBox);
                timebox = new TextBox();
                timebox.Text = Strings.Format(dt, "HH:mm");
                timebox.ID = "c2_" + this.ID.ToString().Trim() + "_" + Strings.Trim(System.Convert.ToString(obj.id.ToString()));
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
                returnValue.Controls.Add(img);

                returnValue.Controls.Add(txtbox);
                returnValue.Controls.Add(timebox);
                break;

            //Case Is = "customerprice"

            // Dim product As clsProduct
            // product = obj

            // Dim lbl As New Label
            //    EditUI.Controls.Add(lbl)
            //    lbl.Text = product.GetPrices(buyerAccount, iq.StandardVariant)(0).Price.DisplayPrice.Text

            default:
                Interaction.Beep();
                break;

        }



        return returnValue;
    }


    public void promote(List<string> errorMessages) //' DECREASES the [order] of a column (moving it left)
    {

        SortedDictionary<int, clsField> sf = new SortedDictionary<int, clsField>();

        foreach (var f in this.Screen.Fields.Values)
        {
            sf.Add(f.order, f);
        }

        //swap this fields order - with the one of the field before it
        clsField pf = null; //previous field
        int pfo = 0; //previous fields order (we need a 'spare' variable to perform the swap)

        foreach (var f in sf)
        {
            if (f.Value == this)
            {
                if (!(pf == null))
                {

                    pfo = pf.order;
                    pf.order = System.Convert.ToInt32(f.Value.order);
                    f.Value.order = pfo;
                    f.Value.update(errorMessages);
                    pf.update(ref errorMessages);

                    break;
                }
            }
            pf = f.Value;
        }


    }
    public void update(ref List<string> errorMessages)
    {

        object sql = null;
        sql = "UPDATE [field] set ";
        sql += "fk_screen_id=" + this.Screen.ID;
        sql += ",property=" + da.SqlEncode(this.propertyName);
        //sql$ &= ",propertyClass=" & da.SqlEncode(Me.PropertyClass)
        sql += ",fk_translation_key_label=" + this.labelText.Key;
        sql += ",helptext=" + da.SqlEncode(this.helpText);
        string vid = "";
        if (this.Validation == null)
        {
            vid = "null";
        }
        else
        {
            vid = System.Convert.ToString(this.Validation.ID);
        }
        sql += ",fk_validation_id=" + vid;
        sql += ",lookupof=" + da.SqlEncode(this.lookupOf);
        sql += ",fk_inputtype_id=" + this.InputType.ID;
        sql += ",length=" + System.Convert.ToString(this.length);
        sql += ",[order]=" + System.Convert.ToString(this.order);
        //    sql$ &= ",fk_screen_id_embed=" & NullID(Me.EmbedScreen)
        sql += ",[width]=" + System.Convert.ToString(this.width);
        sql += ",[height]=" + System.Convert.ToString(this.height);
        sql += ",defaultvalue=" + da.SqlEncode(this.defaultValue);
        sql += ",visibleList=" + (this.visibleList ? "1" : "0").ToString();
        sql += ",visiblePage=" + (this.visiblePage ? "1" : "0").ToString();
        sql += ",defaultfilter=" + da.SqlEncode(this.defaultFilter);
        sql += ",defaultsort=" + da.SqlEncode(this.defaultSort);
        sql += ",priority=" + System.Convert.ToString(this.priority);
        sql += ",fk_translation_key_widgetGroup=" + TranslationKey(this.QuickFilterGroup); //NB: this is a clsTranslation (there's an overload for sqlEncode)
        sql += ",widgetUI=" + da.SqlEncode(this.QuickFilterUItype);
        sql += ",Grows=" + da.SqlEncode(this.Grow);
        sql += ",DefaultFilterValues=" + da.SqlEncode(this.DefaultFilterValues);


        sql += " WHERE ID=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql, false);


        this.Screen.i_field_property.Remove(oPropertyName);
        this.Screen.i_field_property.Add(this.propertyName, this);

        if (!this.Screen.Fields.ContainsKey(this.ID))
        {
            this.Screen.Fields.Add(this.ID, this); //This is for when we've added one (using the New button)
        }

        oPropertyName = propertyName;


    }

    public string LooksUp()
    {

        string[] lu = null;
        lu = this.lookupOf.Split('(');
        return lu[0];

    }

    public void delete(ref List<string> errorMessages)
    {

        try
        {
            object sql = null;
            sql = "DELETE FROM [field] WHERE ID=" + System.Convert.ToString(this.ID);

            da.DBExecutesql(sql);

            this.Screen.Fields.Remove(this.ID);
            this.Screen.i_field_property.Remove(oPropertyName);


        }
        catch (System.Exception ex)
        {
            errorMessages.Add("unable to delete " + ex.Message);
        }


    }


    public string displayName(clsLanguage Language)
    {

        return this.propertyName;

    }




}