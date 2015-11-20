using dataAccess;

public class ClsAvalancheOPG
{

	//systems carry a list of avalancheOPG's
	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private  OPGref {
		get { return m_OPGref; }
		set { m_OPGref = Value; }
	}
	private  m_OPGref;
	private clsRegion Region {
		get { return m_Region; }
		set { m_Region = Value; }
	}
	private clsRegion m_Region;
	private DateTime ValidFrom {
		get { return m_ValidFrom; }
		set { m_ValidFrom = Value; }
	}
	private DateTime m_ValidFrom;
	private DateTime ValidTo {
		get { return m_ValidTo; }
		set { m_ValidTo = Value; }
	}
	private DateTime m_ValidTo;
	private int OptMin {
		get { return m_OptMin; }
		set { m_OptMin = Value; }
	}
	private int m_OptMin;
	private int OptMax {
		get { return m_OptMax; }
		set { m_OptMax = Value; }
	}
	private int m_OptMax;
	//Property LPDiscountPercent As Single 'percent discount off list price (prices(HP))
	private Dictionary<int, ClsAvalancheOption> Options {
		get { return m_Options; }
		set { m_Options = Value; }
	}
	private Dictionary<int, ClsAvalancheOption> m_Options;

	private string DisplayName {
		get { DisplayName = OPGref; }
	}
	public ClsAvalancheOPG Insert()
	{

		ClsAvalancheOPG av = new ClsAvalancheOPG(this.OPGref, this.region, this.ValidFrom, this.ValidTo, this.OptMin, this.OptMax);
		return av;

	}

	public List<clsAvalancheOption> getAvalancheOptions(string prodref = "", int qty = 0, object dateTime = null, clsRegion region = null)
	{

		Pmark("getAvalancheOptions");
		//returns the avalancheOptions (containing % rebate information)  is for the sepcified prodref,qty..etc (which are all optional)

		getAvalancheOptions = new List<clsAvalancheOption>();

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
			regionValid = this.Region.Encompasses(region);
		}

		if (dateValid) {
			if ((qty >= this.OptMin & qty <= this.OptMax) | qty == 0) {
				if (regionValid) {
					foreach ( o in this.Options.Values) {
						if (o.ProdRef == prodref | prodref == "") {
							//avalanche gives a discount as a percentage of list price
							getAvalancheOptions.Add(o);
						}
					}
				}
			}
		}

		Pacc("getAvalancheOptions");

	}


	public void Update()
	{
		object sql;
		sql = "UPDATE Avalance SET ";
		sql += "opgRef=" + this.OPGref + ",";
		//  sql$ &= "prodRef=" & Me.ProdRef & ","
		sql += "FK_region_id=" + this.region.ID + ",";
		sql += "validFrom=" + da.universaldate(this.ValidFrom) + ",";
		sql += "validTo=" + da.universaldate(this.ValidTo) + ",";
		// sql$ &= "LPDiscountPercent =" & Me.LPDiscountPercent & ","
		sql += "optMin=" + this.OptMin + ",";
		sql += "optMax=" + this.OptMax + "";
		sql += " WHERE ID = " + this.ID;

		da.DBExecutesql(sql);

		//TODO : - need to update iq.i_OPGref 

	}


	public ClsAvalancheOPG(string opgRef, clsRegion Region, DateTime validFrom, DateTime ValidTo, int optMin, int optMax, DataTable writecache = null)
	{
		this.OPGref = opgRef;
		//Me.ProdRef = prodRef
		this.region = Region;
		this.ValidFrom = validFrom;
		this.ValidTo = ValidTo;
		this.OptMin = optMin;
		this.OptMax = optMax;
		this.Options = new Dictionary<int, ClsAvalancheOption>();


		if (writecache == null) {
			object sql;
			sql = "INSERT INTO avalancheOPG (opgref,optmin,optmax,validFrom,validTo,fk_region_id) ";
			sql += "VALUES (" + opgRef + "," + optMin + "," + optMax + "," + da.universaldate(validFrom) + "," + da.universaldate(ValidTo) + "," + Region.ID + ")";

			this.ID = da.DBExecutesql(sql, true);
			iq.AvalancheOPGs.Add(this.ID, this);


		} else {
			System.Data.DataRow row;
			row = writecache.NewRow();

			row("opgref") = this.OPGref;
			//row("prodref") = Me.ProdRef
			row("optmin") = this.OptMin;
			row("optmax") = this.OptMax;
			//row("LPDiscountPercent") = Me.LPDiscountPercent
			row("validFrom") = this.ValidFrom;
			row("validTo") = this.ValidTo;
			row("fk_region_id") = this.region.ID;

			writecache.Rows.Add(row);

		}

		iq.i_OpgRef.Add(opgRef, this);

		//If a system has a descendant price with  offer which points to a pool contaiining the system

	}

	public ClsAvalancheOPG()
	{
		this.ID = -1;


	}



	public ClsAvalancheOPG(int ID, string opgRef, clsRegion Region, DateTime validFrom, DateTime ValidTo, int optMin, int optMax)
	{
		//the OPG's don't carry a set of systems - becuase the systems have the OPG's added (product.avalancheOPGs)... which is how they're needed 
		//(we typically want to know the OPG's for a system - not the systems for an OPG)

		this.ID = ID;
		this.OPGref = opgRef;
		this.region = Region;
		this.ValidFrom = validFrom;
		this.ValidTo = ValidTo;
		this.OptMin = optMin;
		this.OptMax = optMax;

		iq.AvalancheOPGs.Add(this.ID, this);
		iq.i_OpgRef.Add(opgRef, this);

		this.Options = new Dictionary<int, ClsAvalancheOption>();

	}


}
