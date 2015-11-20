using dataAccess;


public class clsRegion
{

    public int ID { get; set; }
    public string Code { get; set; }
    public clsTranslation Name { get; set; }
    public bool isCountry { get; set; }
    public clsCulture Culture { get; set; } //.net culture code
    //note: currency does not appear here as there is no absolute link between geography and currency - currency is a function of the buyer account (but is defaulted from the culture)
    public clsCurrency DefaultCurrency { get; set; }
    public clsLanguage DefaultLanguage { get; set; }
    public string Notes { get; set; }
    public bool isPlaceholder { get; set; } //Should NOT be used for localisation assignments (not an 'official' region)
    public int geoRegion { get; set; }
    public clsRegion Parent { get; set; } //needed to recurse up (through wider regions)
    public Dictionary<int, clsRegion> Children { get; set; }

    public bool quantitiesLoaded; //Flag to say that the quanities (autoadds and increments)  have been loaded for this region
    public bool slotsLoaded; //Flag to say that the slots (Gives and takes)  have been loaded for this region

    clsRegion oParent;

    public clsRegion()
    {

    }

    public static clsRegion getOrMake(clsRegion parent, string code, string Name, bool isCountry, bool isPlaceholder, string notes)
    {

        //Returns the clsRegion with the specified code - making one if it doesn't exist

        if (!iq.i_region_code.ContainsKey(code))
        {
            clsRegion aRegion = new clsRegion(parent, code, iq.AddTranslation(Name, English, "region", 0, null, 0, false), isCountry, iq.i_culture_code("en-gb"), isPlaceholder, notes);
        }

        return iq.i_region_code(code);

    }

    /// <summary>Returns a list of this region and all its ancestors</summary>
    /// <returns></returns>
    /// <remarks>e.g.  UK,GWE,EMEMA,XW</remarks>
    public List<clsRegion> ancestors()
    {
        List<clsRegion> returnValue = default(List<clsRegion>);

        returnValue = new List<clsRegion>();

        clsRegion a = default(clsRegion);
        a = this;

        do
        {
            returnValue.Add(a);
            if (a == r_worldwide)
            {
                break;
            }
            a = a.Parent;
        } while (true);

        return returnValue;
    }



    public static Dictionary<string, List<string>> containment()
    {
        Dictionary<string, List<string>> returnValue = default(Dictionary<string, List<string>>);

        returnValue = new Dictionary<string, List<string>>();
        foreach (clsRegion r in iq.Regions.Values)
        {
            if (r.isCountry == false)
            {
                returnValue.Add(r.Code, r.Descendants(false));
            }
        }

        return returnValue;
    }

    public WebControls.TreeNode treeNode()
    {
        WebControls.TreeNode returnValue = default(WebControls.TreeNode);

        returnValue = new WebControls.TreeNode(this.get_Displayname(English));
        returnValue.Value = (this.ID).ToString();

        foreach (var child in this.Children.Values)
        {
            returnValue.ChildNodes.Add(child.treeNode);
        }

        return returnValue;
    }

    public List<string> Descendants(bool includeSelf)
    {
        List<string> returnValue = default(List<string>);

        returnValue = new List<string>();
        if (includeSelf)
        {
            returnValue.Add(this.Code);
        }

        foreach (var child in this.Children.Values)
        {
            returnValue.AddRange(child.Descendants(true));
        }

        return returnValue;
    }

    public clsRegion Insert()
    {
        return new clsRegion(this.Parent, this.Code, this.Name, this.isCountry, this.Culture, this.isPlaceholder, this.Notes);
    }


    public string get_Displayname(clsLanguage language)
    {
        return this.Code + "- " + this.Name.text(language);
    }


    /// <summary>Determines wether this instance of a region contains the specified region (recursively)</summary>
    /// <remarks>For example 'Europe' contains 'Cornwall' </remarks>
    public bool Encompasses(clsRegion region)
    {
        bool returnValue = false;
        //This is called 'encompasses' (rather than contains) - to clearly differntiate from a dictioanry.contains

        returnValue = false;
        if (this == region)
        {
            returnValue = true;
        }
        return returnValue; //A region encompasses itself - eg. FRANCE encompasses FRANCE

        //		foreach (var r in this.Children.Values)
        //		{
        //			if (r.Encompasses(region))
        //			{
        //				returnValue = true;
        //				}
        //				return returnValue;
        //				}

        return returnValue;
    }

    public clsRegion(int id, clsRegion Parent, string code, clsTranslation Name, bool isCountry, clsCulture culture, bool isPlaceholder, string notes, string geoRegionId)
    {

        //This is an overriden constructor - becuase the ID *is* specified - it *knows* we' dont want to do a database insert
        this.ID = id;
        this.Code = code;
        this.Name = Name;
        this.Culture = culture;
        this.isCountry = isCountry;
        this.Parent = Parent;
        this.isPlaceholder = isPlaceholder;
        this.Notes = notes;
        if (geoRegionId != "")
        {
            this.geoRegion = int.Parse(geoRegionId);

        }


        if (this.Parent != null)
        {
            this.Parent.Children.Add(this.ID, this);
        }

        iq.Regions.Add(this.ID, this);
        iq.i_region_code.Add(this.Code, this);

        this.Children = new Dictionary<int, clsRegion>();
        oParent = Parent;

    }

    public clsRegion(clsRegion parent, string code, clsTranslation Name, bool isCountry, clsCulture culture, bool isPlaceholder, string Notes)
    {

        //Creates a new (instance of the class cls)Language - populates its ID

        if (code == "UK")
        {
            Debugger.Break();
        }

        object pid = null;
        if (parent == null)
        {
            pid = "null";
        }
        else
        {
            pid = (parent.ID).ToString();
        }

        object sql = null;
        sql = "INSERT INTO [Region] ([fk_region_id_parent],[Code],[fk_translation_key_Name],[iscountry],[culture],isplaceholder, notes) ";
        sql += "VALUES (" + System.Convert.ToString(pid) + "," + da.SqlEncode(code) + "," + Name.Key + "," + System.Convert.ToString(isCountry ? 1 : 0) + "," + this.Culture.ID + ",";
        sql += (isPlaceholder ? 1 : 0) + "," + da.SqlEncode(Notes) + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.Code = code;
        this.Name = Name;
        this.Culture = culture;
        this.isCountry = isCountry;
        this.Parent = parent;
        this.isPlaceholder = isPlaceholder;

        if (this.Parent != null)
        {
            this.Parent.Children.Add(this.ID, this);
        }


        iq.Regions.Add(this.ID, this);
        iq.i_region_code.Add(this.Code, this);

        oParent = parent;

        this.Children = new Dictionary<int, clsRegion>();


    }

    public void Update()
    {

        object sql = null;
        sql = "UPDATE [Region] set ";

        sql += "[Code]=" + da.SqlEncode(this.Code) + ",";
        sql += "[fk_translation_key_name]=" + this.Name.Key + ",";
        if (this.Parent == null)
        {
            sql += "[fk_region_id_parent]=null";
        }
        else
        {
            sql += "[fk_region_id_parent]=" + System.Convert.ToString(this.Parent.ID);
        }
        sql += ",isCountry=" + System.Convert.ToString(this.isCountry ? 1 : 0);
        sql += ",isPlaceHolder=" + System.Convert.ToString(this.isPlaceholder ? 1 : 0);
        sql += ",notes=" + da.SqlEncode(this.Notes);
        sql += ",[FK_Region_ID_Geo]=" + System.Convert.ToString(this.geoRegion);
        sql += ",[FK_Culture_ID]=" + this.Culture.ID;
        sql += " WHERE ID=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql, false);

        if (this.oParent != null)
        {
            this.oParent.Children.Remove(this.ID);
        }

        if (this.Parent != null)
        {
            if (!this.Parent.Children.ContainsKey(this.ID))
            {
                this.Parent.Children.Add(this.ID, this);
            }
        }

        oParent = Parent;

    }

    public void Remove()
    {
        string sql = "Delete [Region] where ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);
        if (this.oParent != null)
        {
            this.oParent.Children.Remove(this.ID);
        }

    }

}