using dataAccess;

public class clsRegion
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string Code {
		get { return m_Code; }
		set { m_Code = Value; }
	}
	private string m_Code;
	private clsTranslation Name {
		get { return m_Name; }
		set { m_Name = Value; }
	}
	private clsTranslation m_Name;
	private bool isCountry {
		get { return m_isCountry; }
		set { m_isCountry = Value; }
	}
	private bool m_isCountry;
	private clsCulture Culture {
		get { return m_Culture; }
		set { m_Culture = Value; }
	}
	private clsCulture m_Culture;
	//.net culture code
	//note: currency does not appear here as there is no absolute link between geography and currency - currency is a function of the buyer account (but is defaulted from the culture)
	private clsCurrency DefaultCurrency {
		get { return m_DefaultCurrency; }
		set { m_DefaultCurrency = Value; }
	}
	private clsCurrency m_DefaultCurrency;
	private clsLanguage DefaultLanguage {
		get { return m_DefaultLanguage; }
		set { m_DefaultLanguage = Value; }
	}
	private clsLanguage m_DefaultLanguage;
	private string Notes {
		get { return m_Notes; }
		set { m_Notes = Value; }
	}
	private string m_Notes;
	private bool isPlaceholder {
		get { return m_isPlaceholder; }
		set { m_isPlaceholder = Value; }
	}
	private bool m_isPlaceholder;
	//Should NOT be used for localisation assignments (not an 'official' region)
	private int geoRegion {
		get { return m_geoRegion; }
		set { m_geoRegion = Value; }
	}
	private int m_geoRegion;
	private clsRegion Parent {
		get { return m_Parent; }
		set { m_Parent = Value; }
	}
	private clsRegion m_Parent;
	//needed to recurse up (through wider regions)
	private Dictionary<int, clsRegion> Children {
		get { return m_Children; }
		set { m_Children = Value; }
	}
	private Dictionary<int, clsRegion> m_Children;

		//Flag to say that the quanities (autoadds and increments)  have been loaded for this region
	public bool quantitiesLoaded;
		//Flag to say that the slots (Gives and takes)  have been loaded for this region
	public bool slotsLoaded;


	clsRegion oParent;

	public clsRegion()
	{
	}

	public static clsRegion getOrMake(clsRegion parent, string code, string Name, bool isCountry, bool isPlaceholder, string notes)
	{

		//Returns the clsRegion with the specified code - making one if it doesn't exist

		if (!iq.i_region_code.ContainsKey(code)) {
			clsRegion aRegion = new clsRegion(parent, code, iq.AddTranslation(Name, English, "region", 0, null, 0, false), isCountry, iq.i_culture_code("en-gb"), isPlaceholder, notes);
		}

		return iq.i_region_code(code);

	}

	/// <summary>Returns a list of this region and all its ancestors</summary>
	/// <returns></returns>
	/// <remarks>e.g.  UK,GWE,EMEMA,XW</remarks>
	public List<clsRegion> ancestors()
	{

		ancestors = new List<clsRegion>();

		clsRegion a;
		a = this;

		do {
			ancestors.Add(a);
			if (object.ReferenceEquals(a, r_worldwide))
				break; // TODO: might not be correct. Was : Exit Do
			a = a.Parent;
		} while (true);

	}



	public static Dictionary<string, List<string>> containment()
	{

		containment = new Dictionary<string, List<string>>();
		foreach (clsRegion r in iq.Regions.Values) {
			if (r.isCountry == false) {
				containment.Add(r.Code, r.Descendants(false));
			}
		}

	}

	public WebControls.TreeNode treeNode()
	{

		treeNode = new WebControls.TreeNode(this.Displayname(English));
		treeNode.Value = this.ID;

		foreach ( child in this.Children.Values) {
			treeNode.ChildNodes.Add(child.treeNode);
		}

	}

	public List<string> Descendants(bool includeSelf)
	{

		Descendants = new List<string>();
		if (includeSelf) {
			Descendants.Add(this.Code);
		}

		foreach ( child in this.Children.Values) {
			Descendants.AddRange(child.Descendants(true));
		}

	}

	public clsRegion Insert()
	{
		return new clsRegion(this.Parent, this.Code, this.Name, this.isCountry, this.Culture, this.isPlaceholder, this.Notes);
	}

	public string Displayname {

		get { return this.Code + "- " + this.Name.text(language); }
	}


	/// <summary>Determines wether this instance of a region contains the specified region (recursively)</summary>
	/// <remarks>For example 'Europe' contains 'Cornwall' </remarks>
	public bool Encompasses(clsRegion region)
	{
		//This is called 'encompasses' (rather than contains) - to clearly differntiate from a dictioanry.contains

		Encompasses = false;
		if (object.ReferenceEquals(this, region)){Encompasses = true;return;
}
		//A region encompasses itself - eg. FRANCE encompasses FRANCE

		foreach ( r in this.Children.Values) {
			if (r.Encompasses(region)){Encompasses = true;return;
}
		}

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
		if (geoRegionId != "") {
			this.geoRegion = (int)geoRegionId;

		}


		if (this.Parent != null) {
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
			System.Diagnostics.Debugger.Break();

		object pid;
		if (parent == null) {
			pid = "null";
		} else {
			pid = parent.ID;
		}

		object sql;
		sql = "INSERT INTO [Region] ([fk_region_id_parent],[Code],[fk_translation_key_Name],[iscountry],[culture],isplaceholder, notes) ";
		sql += "VALUES (" + pid + "," + da.SqlEncode(code) + "," + Name.Key + "," + IIf(isCountry, 1, 0) + "," + this.Culture.ID + ",";
		sql += IIf(isPlaceholder, 1, 0) + "," + da.SqlEncode(Notes) + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.Code = code;
		this.Name = Name;
		this.Culture = culture;
		this.isCountry = isCountry;
		this.Parent = parent;
		this.isPlaceholder = isPlaceholder;

		if (this.Parent != null) {
			this.Parent.Children.Add(this.ID, this);
		}


		iq.Regions.Add(this.ID, this);
		iq.i_region_code.Add(this.Code, this);

		oParent = parent;

		this.Children = new Dictionary<int, clsRegion>();


	}


	public void Update()
	{
		object sql;
		sql = "UPDATE [Region] set ";

		sql += "[Code]=" + da.SqlEncode(this.Code) + ",";
		sql += "[fk_translation_key_name]=" + this.Name.Key + ",";
		if (this.Parent == null) {
			sql += "[fk_region_id_parent]=null";
		} else {
			sql += "[fk_region_id_parent]=" + this.Parent.ID;
		}
		sql += ",isCountry=" + IIf(this.isCountry, 1, 0);
		sql += ",isPlaceHolder=" + IIf(this.isPlaceholder, 1, 0);
		sql += ",notes=" + da.SqlEncode(this.Notes);
		sql += ",[FK_Region_ID_Geo]=" + this.geoRegion;
		sql += ",[FK_Culture_ID]=" + this.Culture.ID;
		sql += " WHERE ID=" + this.ID;

		da.DBExecutesql(sql, false);

		if (this.oParent != null) {
			this.oParent.Children.Remove(this.ID);
		}

		if (this.Parent != null) {
			if (!this.Parent.Children.ContainsKey(this.ID)) {
				this.Parent.Children.Add(this.ID, this);
			}
		}

		oParent = Parent;

	}

	public void Remove()
	{
		string sql = "Delete [Region] where ID=" + this.ID;
		da.DBExecutesql(sql, false);
		if (this.oParent != null) {
			this.oParent.Children.Remove(this.ID);
		}

	}

}
