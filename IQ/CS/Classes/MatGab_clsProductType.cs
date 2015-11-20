using dataAccess;

public class clsProductType
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
	private clsTranslation Translation {
		get { return m_Translation; }
		set { m_Translation = Value; }
	}
	private clsTranslation m_Translation;
	private short Order {
		get { return m_Order; }
		set { m_Order = Value; }
	}
	private short m_Order;


	string oCode;

	public clsProductType(string code, clsTranslation translation, short order)
	{
		object sql;
		sql = "INSERT INTO ProductType (code,fk_Translation_key_text,[order]) VALUES ('" + code + "'," + translation.Key + "," + order + ");";
		this.ID = da.DBExecutesql(sql, true);
		this.Code = code;
		this.Translation = translation;
		this.Order = order;

		iq.ProductTypes.Add(this.ID, this);
		iq.i_ProductType_Code.Add(this.Code, this);

		oCode = this.Code;

	}
	public string DisplayName {
		get { DisplayName = this.Code + " " + this.Translation.text(Language); }
	}


	public clsProductType()
	{
	}

	public object Insert()
	{

		return new clsProductType(this.Code, this.Translation, 0);

	}


	public void Update()
	{
		object sql;
		sql = "UPDATE [ProductType] SET code=" + da.SqlEncode(this.Code) + ",fk_translation_key_text=" + this.Translation.Key + ",[order]=" + Order + " WHERE ID=" + this.ID;

		try {
			iq.i_ProductType_Code.Remove(oCode);
			iq.i_ProductType_Code.Add(this.Code, this);
			da.dbexecutesql(sql);

		} catch (System.Exception ex) {
			System.Diagnostics.Debugger.Break();
			// probably a duplictae code
		}

	}


	public void Delete()
	{
		object sql;
		sql = "DELETE FROM [ProductType] WHERE ID=" + this.ID;
		da.dbexecutesql(sql);

	}


	public clsProductType(int ID, string code, clsTranslation translation, short order)
	{
		this.ID = ID;
		this.Code = code;
		this.Translation = translation;
		this.Order = order;

		iq.ProductTypes.Add(this.ID, this);
		iq.i_ProductType_Code.Add(this.Code, this);

		oCode = this.Code;

	}


}
