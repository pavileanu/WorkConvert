using dataAccess;
public class clsAvalancheOption
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string ProdRef {
		get { return m_ProdRef; }
		set { m_ProdRef = Value; }
	}
	private string m_ProdRef;
	private float LPDiscountPercent {
		get { return m_LPDiscountPercent; }
		set { m_LPDiscountPercent = Value; }
	}
	private float m_LPDiscountPercent;
	private ClsAvalancheOPG AvalancheOPG {
		get { return m_AvalancheOPG; }
		set { m_AvalancheOPG = Value; }
	}
	private ClsAvalancheOPG m_AvalancheOPG;


	public clsAvalancheOption(ClsAvalancheOPG avOPG, string prodRef, float LPDiscountPercent, DataTable WriteCache = null)
	{

		if (LPDiscountPercent == 0)
			System.Diagnostics.Debugger.Break();

		if (WriteCache == null) {
			object sql;
			sql = "INSERT INTO AvalancheOption(fk_avalancheOPG_ID,prodref,lpdiscountpercent) VALUES (" + avOPG.ID + "," + da.SqlEncode(prodRef) + "," + LPDiscountPercent + ")";
			this.ID = da.DBExecutesql(sql);
			avOPG.Options.Add(this.ID, this);
		} else {
			this.ID = -1;

			System.Data.DataRow row;
			row = WriteCache.NewRow();

			row("fk_avalancheOPG_id") = avOPG.ID;
			row("prodref") = prodRef;
			row("LPDiscountPercent") = LPDiscountPercent;
			WriteCache.Rows.Add(row);

		}

		this.ProdRef = prodRef;
		this.LPDiscountPercent = LPDiscountPercent;
		this.AvalancheOPG = avOPG;


	}


	public clsAvalancheOption(int ID, ClsAvalancheOPG avOPG, string prodRef, float LPDiscountPercent)
	{
		this.ID = ID;
		this.ProdRef = prodRef;
		this.LPDiscountPercent = LPDiscountPercent;

		if (this.LPDiscountPercent == 0)
			System.Diagnostics.Debugger.Break();

		this.AvalancheOPG = avOPG;
		avOPG.Options.Add(this.ID, this);

	}

}
