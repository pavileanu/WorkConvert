using dataAccess;


public class clsScheme : i_Editable
{

	public int ID;
	public clsTranslation Name;
	public string code;
	public clsRegion Region;
	public System.DateTime StartDate;
	public System.DateTime EndDate;

	public bool Active;

	public void i_Editable.delete(ref List<string> errormessages)
	{
		object sql;
		sql = "delete from Scheme where id=me.id";
		try {
			da.DBExecutesql(sql);
			iq.Schemes.Remove(this.ID);

		} catch (Exception ex) {
			errormessages.Add(ex.Message);
		}


	}

	public string i_Editable.displayName(clsLanguage Language)
	{

		return this.Name.text(Language);

	}


	//the editor requires a parameterless constructor
	public clsScheme()
	{



	}

	public string compoundKey()
	{
		return this.Region.ID + "^" + this.StartDate + "^" + this.EndDate;
	}

	public clsScheme(int id, string code, clsTranslation name, clsRegion Region, System.DateTime Startdate, System.DateTime Enddate)
	{
		this.ID = id;
		this.code = code;
		this.Name = name;
		this.Region = Region;
		this.StartDate = Startdate;
		this.EndDate = Enddate;

		iq.Schemes.Add(this.ID, this);
		if (!iq.i_scheme_code.ContainsKey(this.code))
			iq.i_scheme_code.Add(this.code, new List<clsScheme>());
		iq.i_scheme_code(this.code).Add(this);

	}


	private clsScheme(string code, clsTranslation name, clsRegion Region, System.DateTime Startdate, System.DateTime Enddate, DataTable writecache = null)
	{
		this.code = code;
		this.Name = name;
		this.Region = Region;
		this.StartDate = Startdate;
		this.EndDate = Enddate;

		if (!iq.i_scheme_code.ContainsKey(this.code))
			iq.i_scheme_code.Add(this.code, new List<clsScheme>());
		iq.i_scheme_code(this.code).Add(this);


		if (writecache == null) {
			object sql;
			sql = "INSERT INTO [Scheme] (code,fk_translation_key_name,StartDate,EndDate,fk_region_id) ";
			sql += "VALUES (" + da.SqlEncode(this.code) + "," + name.Key + "," + da.UniversalDate(Startdate) + "," + da.UniversalDate(Enddate) + "," + Region.ID + ");";
			this.ID = da.DBExecutesql(sql, true);

			iq.Schemes.Add(this.ID, this);

		} else {
			System.Data.DataRow row;
			row = writecache.NewRow();
			row("code") = this.code;
			row("fk_translation_key_name") = this.Name.Key;
			row("startdate") = Startdate;
			row("enddate") = Enddate;
			row("fk_region_id") = Region.ID;
			writecache.Rows.Add(row);
		}

	}

	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsScheme("new", iq.AddTranslation("New Loyalty Scheme", English, "Lschemes", 0, null, 0, true), r_worldwide, Now, DateAdd(DateInterval.Year, 1, Now));

	}


	public void i_Editable.update(ref List<string> errormessages)
	{
		object sql;
		sql = "UPDATE [scheme] set (code=" + da.SqlEncode(this.code) + ",fk_translation_key_name=" + this.Name.Key + ",StartDate=" + da.UniversalDate(this.StartDate) + ",enddate=" + da.UniversalDate(this.EndDate) + ",fk_region_id=" + this.Region.ID + " WHERE ID=" + this.ID;
		da.DBExecutesql(sql, false);

	}

}
