using dataAccess;
using System.IO;

public class clsFlexOPG
{

	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public string OPGRef {
		get { return m_OPGRef; }
		set { m_OPGRef = Value; }
	}
	private string m_OPGRef;
	public string Description {
		get { return m_Description; }
		set { m_Description = Value; }
	}
	private string m_Description;
	public System.DateTime ValidFrom {
		get { return m_ValidFrom; }
		set { m_ValidFrom = Value; }
	}
	private System.DateTime m_ValidFrom;
	public System.DateTime ValidTo {
		get { return m_ValidTo; }
		set { m_ValidTo = Value; }
	}
	private System.DateTime m_ValidTo;
	public clsCurrency Currency {
		get { return m_Currency; }
		set { m_Currency = Value; }
	}
	private clsCurrency m_Currency;
	public int MinOptions;
	public int MaxOptions;
	public string OPGSysType {
		get { return m_OPGSysType; }
		set { m_OPGSysType = Value; }
	}
	private string m_OPGSysType;
	public Dictionary<int, clsFlexRule> Rules;
	public Dictionary<int, clsFlexLine> Lines;
	//Public Regions As Dictionary(Of Integer, clsFlexRegion) 'NOTE - this is NOT a dictionary of clsRegions! (the clsFlexRegion allows the required many:many relationship - mostly for editing)

	public Dictionary<int, clsRegion> regions;
	//Public Sub serialize(Sw As streamwriter)

	//    'for any object - you only need write the ID
	//    'For any dictionary - you write the IDs

	//End Sub

	//Public Sub deSerialize(sr As streamreader)

	//End Sub


	public clsFlexOPG(int ID, string OPGref, string Description, System.DateTime validFrom, System.DateTime validTo, clsCurrency Currency, int minOptions, int maxOptions, string OPGSysType)
	{
		this.ID = ID;
		this.OPGRef = OPGref;
		this.Description = Description;
		this.ValidFrom = validFrom;
		this.ValidTo = validTo;
		this.Currency = Currency;
		this.MinOptions = minOptions;
		this.MaxOptions = maxOptions;
		this.OPGSysType = OPGSysType;

		this.Rules = new Dictionary<int, clsFlexRule>();
		this.Lines = new Dictionary<int, clsFlexLine>();
		this.regions = new Dictionary<int, clsRegion>();
		//clsFlexRegion)
		iq.FlexOPGs.Add(this.ID, this);

	}


	public clsFlexOPG(string OPGref, string Description, System.DateTime validFrom, System.DateTime validTo, clsCurrency Currency, int MinOptions, int maxOptions, string OPGSysType, DataTable dt = null)
	{
		this.ID = ID;
		this.OPGRef = OPGref;
		this.Description = Description;
		this.ValidFrom = validFrom;
		this.ValidTo = validTo;
		this.Currency = Currency;
		this.MinOptions = MinOptions;
		this.MaxOptions = maxOptions;
		this.OPGSysType = OPGSysType;

		this.Rules = new Dictionary<int, clsFlexRule>();
		this.Lines = new Dictionary<int, clsFlexLine>();
		this.regions = new Dictionary<int, clsRegion>();
		// clsFlexRegion)

		if (dt == null) {
			object Sql = " INSERT INTO Flex (OPGref,description,validFrom,validTo,FK_currency_ID,minoptions,maxoptions,OPGSysType) ";
			Sql += "values (" + this.OPGRef + "," + da.SqlEncode(Description) + "," + da.UniversalDate(this.ValidFrom) + "," + da.UniversalDate(this.ValidTo) + ",";
			Sql += this.Currency.ID + "," + this.MinOptions + "," + this.MaxOptions + "," + da.SqlEncode(this.OPGSysType) + ");";
			this.ID = da.DBExecutesql(Sql, true);

			iq.FlexOPGs.Add(this.ID, this);
		} else {
			DataRow dr;
			dr = dt.NewRow;
			dr.Item("OPGRef") = this.OPGRef;
			dr.Item("Description") = this.Description;
			dr.Item("validFrom") = this.ValidFrom;
			dr.Item("validTo") = this.ValidTo;
			dr.Item("FK_Currency_ID") = this.Currency.ID;
			dr.Item("MinOptions") = this.MinOptions;
			dr.Item("MaxOptions") = this.MaxOptions;
			dr.Item("OPGSysType") = this.OPGSysType;

			dt.Rows.Add(dr);
			this.ID = -1;
		}
	}
	public clsFlexRule getRule(clsProductType ProductType)
	{

		object r = from v in this.Rules.Valueswhere object.ReferenceEquals(v.ProductType, ProductType);
		if (r.Any)
			getRule = r.First;
		else
			getRule = null;

	}
	public bool isCurrent()
	{

		isCurrent = false;
		if (this.validFrom < Now & this.validTo > Now)
			isCurrent = true;

	}

	public bool AppliesToRegion(clsRegion region)
	{

		AppliesToRegion = false;

		//    If Me.Regions.Values.Count = 0 Then Stop

		//Each flexOPG potentially applies in many regions (becuase although regions are a hierarchy, a single region doesn't quite cut it (eg. Peru & Mexico)
		foreach ( r in this.Regions.Values) {
			if (r.Encompasses(region)) {
				AppliesToRegion = true;
				break; // TODO: might not be correct. Was : Exit For
			}
		}

	}

	/// <summary>Returns the FlexLines from this FlexOPG which match the supllied critera</summary>
	public List<clsFlexLine> MatchingFlexLines(clsProduct product = null, int qty = 0, object dateTime = null, clsRegion region = null)
	{

		Pmark("matchingFlexLines");
		//returns the FlexLines (containing rebate information)  is for the sepcified prodType,qty..etc (which are all optional)

		MatchingFlexLines = new List<clsFlexLine>();

		bool dateValid = false;
		if (dateTime == null) {
			dateValid = true;
		} else {
			if (dateTime > this.ValidFrom & dateTime < this.ValidTo) {
				dateValid = true;
			}
		}

		bool regionValid = false;
		if (region == null) {
			regionValid = true;
		} else {
			//Each flexOPG potentially applies in many regions (becuase although regions are a hierarchy, a single region doesn't quite cut it (eg. Peru & Mexico)
			foreach ( r in this.Regions.Values) {
				if (r.Encompasses(region)){regionValid = true;break; // TODO: might not be correct. Was : Exit For
}
			}
		}


		if (dateValid & regionValid) {
			//build an index to look up rules for each product type quickly (this should probably go in the clsFlex object for speed)
			Dictionary<clsProductType, clsFlexRule> i_rules_producttype = new Dictionary<clsProductType, clsFlexRule>();
			foreach ( r in this.Rules.Values) {
				i_rules_producttype.Add(r.ProductType, r);
			}


			foreach ( flexline in this.Lines.Values) {

				if (object.ReferenceEquals(flexline.Product, product) | product == null) {
					clsFlexRule rule = null;
					if (i_rules_producttype.ContainsKey(flexline.Product.ProductType)) {
						rule = i_rules_producttype(flexline.Product.ProductType);
					}

					bool rulevalid = false;
					if (rule == null) {
						rulevalid = true;
					} else {
						if ((qty >= rule.min & qty <= rule.max) | qty == 0)
							rulevalid = true;
					}

					if (rulevalid)
						MatchingFlexLines.Add(flexline);
				}
			}
		}

		Pacc("matchingFlexLines");

	}


}


public class clsFlexLine
{
	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public clsFlexOPG FlexOPG {
		get { return m_FlexOPG; }
		set { m_FlexOPG = Value; }
	}
	private clsFlexOPG m_FlexOPG;
	public clsProduct Product {
		get { return m_Product; }
		set { m_Product = Value; }
	}
	private clsProduct m_Product;
	public decimal rebate {
		get { return m_rebate; }
		set { m_rebate = Value; }
	}
	private decimal m_rebate;
	public DateTime validFrom {
		get { return m_validFrom; }
		set { m_validFrom = Value; }
	}
	private DateTime m_validFrom;
	public DateTime validTo {
		get { return m_validTo; }
		set { m_validTo = Value; }
	}
	private DateTime m_validTo;


	public clsFlexLine(int ID, clsFlexOPG FlexOPG, clsProduct Product, float Rebate, System.DateTime validFrom, System.DateTime validTo)
	{
		this.ID = ID;
		this.FlexOPG = FlexOPG;
		this.Product = Product;
		this.rebate = Rebate;
		this.validFrom = validFrom;
		this.validTo = validTo;

		Product.OPGflexLines.Add(this.ID, this);
		FlexOPG.Lines.Add(this.ID, this);

	}


	public clsFlexLine(clsFlexOPG FlexOPG, clsProduct Product, float rebate, System.DateTime validFrom, System.DateTime validTo, DataTable dt = null)
	{
		this.ID = ID;
		this.FlexOPG = FlexOPG;
		this.Product = Product;
		this.rebate = rebate;
		this.validFrom = validFrom;
		this.validTo = validTo;


		if (dt != null) {

			DataRow dr = dt.NewRow;
			dr("fk_flex_id") = FlexOPG.ID;
			dr("FK_Product_id") = Product.ID;
			dr("rebate") = rebate;
			dr("validFrom") = validFrom;
			dr("validTo") = validTo;
			dt.Rows.Add(dr);
			this.ID = -1;
		} else {
			object sql = "INSERT INTO FlexLine (FK_Product_ID,rebate,FK_Flex_ID,validFrom,ValidTo) VALUES ";
			sql += "(" + this.Product.ID + "," + this.rebate + "," + this.FlexOPG.ID + "," + da.UniversalDate(this.validFrom) + "," + da.UniversalDate(this.validTo) + ");";
			this.ID = da.DBExecutesql(sql, true);
			FlexOPG.Lines.Add(this.ID, this);
			Product.OPGflexLines.Add(this.ID, this);

		}


	}

	public bool isCurrent()
	{

		isCurrent = false;
		if (this.validFrom < Now & this.validTo > Now)
			isCurrent = true;

	}


}

public class clsFlexRule
{
	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public clsProductType ProductType {
		get { return m_ProductType; }
		set { m_ProductType = Value; }
	}
	private clsProductType m_ProductType;
	public int min {
		get { return m_min; }
		set { m_min = Value; }
	}
	private int m_min;
	//NullableInt
	public int max {
		get { return m_max; }
		set { m_max = Value; }
	}
	private int m_max;
	public bool optionalRule {
		get { return m_optionalRule; }
		set { m_optionalRule = Value; }
	}
	private bool m_optionalRule;
	public clsFlexOPG flexOPG {
		get { return m_flexOPG; }
		set { m_flexOPG = Value; }
	}
	private clsFlexOPG m_flexOPG;


	public clsFlexRule(int ID, clsFlexOPG flexOPG, clsProductType ProductType, int min, int max, bool optionalRule)
	{
		this.ID = ID;
		this.ProductType = ProductType;
		this.min = min;
		this.max = max;
		this.optionalRule = optionalRule;
		flexOPG.Rules.Add(this.ID, this);

	}


	public clsFlexRule(clsFlexOPG FlexOPG, clsProductType ProductType, int min, int max, bool optionalRule, DataTable dt)
	{
		this.flexOPG = FlexOPG;
		this.ProductType = ProductType;
		this.min = min;
		this.max = max;
		this.optionalRule = optionalRule;

		if (dt != null) {
			DataRow dr = dt.NewRow;
			dr("FK_Flex_id") = this.flexOPG.ID;
			dr("FK_ProductType_id") = this.ProductType.ID;
			dr("min") = this.min;
			dr("max") = this.max;
			dr("optionalRule") = this.optionalRule;
			dt.Rows.Add(dr);
			this.ID = -1;
		} else {
			object sql = "INSERT INTO FlexRule (FK_Flex_ID,FK_ProductType_ID,[min],[max],[optionalRule]) VALUES ";
			sql += "(" + this.flexOPG.ID + "," + this.ProductType.ID + "," + this.min + "," + this.max + "," + this.optionalRule + ");";
			this.ID = da.DBExecutesql(sql, true);
			FlexOPG.Rules.Add(this.ID, this);
		}

	}
}

public class clsFlexRegion
{
	//Should Allow an opgs regions to be edited (and presisted) - The editor can get you edit the FlexOPG's regions - but can't 'store' them
	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public clsFlexOPG FlexOPG {
		get { return m_FlexOPG; }
		set { m_FlexOPG = Value; }
	}
	private clsFlexOPG m_FlexOPG;
	public clsRegion Region {
		get { return m_Region; }
		set { m_Region = Value; }
	}
	private clsRegion m_Region;


	public clsFlexRegion(int ID, clsFlexOPG flexOPG, clsRegion Region)
	{
		this.ID = ID;
		this.FlexOPG = flexOPG;
		this.Region = Region;
		flexOPG.regions.Add(this.Region.ID, this.Region);
		//ME

	}


	public clsFlexRegion(clsFlexOPG FlexOPG, clsRegion region, DataTable dt = null)
	{
		this.FlexOPG = FlexOPG;
		this.Region = region;


		if (dt != null) {
			DataRow dr = dt.NewRow;
			dr("FK_Flex_id") = this.FlexOPG.ID;
			dr("FK_region_id") = this.Region.ID;
			dt.Rows.Add(dr);
			this.ID = -1;
		} else {
			object sql = "INSERT INTO FlexRegion(FK_Flex_ID,FK_region_ID,[min],[max]) VALUES ";
			sql += "(" + this.FlexOPG.ID + "," + this.Region.ID + ");";
			this.ID = da.DBExecutesql(sql, true);
			this.FlexOPG.regions.Add(this.Region.ID, this.Region);
		}



	}

}
