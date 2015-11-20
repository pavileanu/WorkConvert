using dataAccess;

public class clsBundle
{

	//each bundle is added to one or more systems Bundles  (the bundle applies to (potentially) many systems
	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsTranslation Name {
		get { return m_Name; }
		set { m_Name = Value; }
	}
	private clsTranslation m_Name;
	private string OPGRef {
		get { return m_OPGRef; }
		set { m_OPGRef = Value; }
	}
	private string m_OPGRef;
	private string Code {
		get { return m_Code; }
		set { m_Code = Value; }
	}
	private string m_Code;
	private DateTime validFrom {
		get { return m_validFrom; }
		set { m_validFrom = Value; }
	}
	private DateTime m_validFrom;
	private DateTime validTo {
		get { return m_validTo; }
		set { m_validTo = Value; }
	}
	private DateTime m_validTo;
	private clsRegion Region {
		get { return m_Region; }
		set { m_Region = Value; }
	}
	private clsRegion m_Region;
	//Property Channel as clschannel ' would be asy to implement but Dan says not a priority - would give disti/customer specific bundles - would need to recurse the channel tree
	//Rebate as single :NB- Rebate has been moved into the BundleSystem - allowing different rebates on differnet systems (with the same bundle of options)

	private Dictionary<int, clsBundleSystem> Systems {
		get { return m_Systems; }
		set { m_Systems = Value; }
	}
	private Dictionary<int, clsBundleSystem> m_Systems;
	//allows the generic editor to view/add systems to a bundle conveniently.. note the bundles are also added the the systems in question but are not editable in that context (they are not a property)
	private Dictionary<int, clsBundleItem> Items {
		get { return m_Items; }
		set { m_Items = Value; }
	}
	private Dictionary<int, clsBundleItem> m_Items;
	//The options in the bundle.. it's called Items as - in future it may also contain (sub) systems

	private string DisplayName {
		get { DisplayName = OPGRef; }
	}
	public clsBundle Insert()
	{

		clsBundle av = new clsBundle(this.Name, this.OPGRef, this.Code, this.Region, this.validFrom, this.validTo);
		return av;

	}


	public void Update()
	{
		object sql;
		sql = "UPDATE bundle SET ";
		sql += "fk_Translation_key_name=" + this.Name.Key + ",";
		sql += "Opgref=" + da.SqlEncode(this.OPGRef) + ",";
		sql += "code=" + da.SqlEncode(this.Code) + ",";
		sql += "FK_region_id=" + this.Region.ID + ",";
		sql += "validFrom=" + da.UniversalDate(this.validFrom) + ",";
		sql += "validTo=" + da.universaldate(this.validTo) + ",";
		sql += " WHERE ID = " + this.ID;

		da.DBExecutesql(sql);

		//TODO : - need to update iq.i_bundle_code

	}


	public clsBundle(clsTranslation name, string opgRef, string code, clsRegion Region, DateTime validFrom, DateTime ValidTo, DataTable writecache = null)
	{
		this.Name = name;
		this.OPGRef = opgRef;
		this.Code = code;
		this.Region = Region;
		this.validFrom = validFrom;
		this.validTo = ValidTo;
		//    Me.Rebate = rebate
		this.Items = new Dictionary<int, clsBundleItem>();


		if (writecache == null) {
			object sql;
			sql = "INSERT INTO Bundle (fk_translation_key,opgref,code,validfrom,validto,fk_region_id,rebate) ";
			sql += "VALUES (" + this.Name.Key + "," + da.SqlEncode(opgRef) + "," + da.SqlEncode(code) + "," + da.universaldate(validFrom) + "," + da.universaldate(ValidTo) + "," + Region.ID + ")";

			this.ID = da.DBExecutesql(sql, true);
			iq.Bundles.Add(this.ID, this);


		} else {
			System.Data.DataRow row;
			row = writecache.NewRow();

			row("fk_translation_key_name") = this.Name.Key;
			row("opgref") = this.OPGRef;
			row("code") = this.Code;
			row("validFrom") = this.validFrom;
			row("validTo") = this.validTo;
			row("fk_region_id") = this.Region.ID;


			writecache.Rows.Add(row);

		}

		iq.i_Bundle_code.Add(this.Code, this);

		//If a system has a descendant price with  offer which points to a pool contaiining the system

	}

	public clsBundle()
	{
		this.ID = -1;


	}

	public Panel UI()
	{

		UI = new Panel();

		Label lbl = new Label();
		lbl.Text = "Bundles available";


	}


	public clsBundle(int ID, clsTranslation name, string opgRef, string code, clsRegion Region, DateTime validFrom, DateTime ValidTo)
	{

		this.ID = ID;
		this.Name = name;
		this.OPGRef = opgRef;
		this.Code = code;
		this.Region = Region;
		this.validFrom = validFrom;
		this.validTo = ValidTo;

		iq.Bundles.Add(this.ID, this);

		this.Items = new Dictionary<int, clsBundleItem>();

		iq.i_Bundle_code.Add(this.Code, this);


	}


}
