using dataAccess;

public class clsSector
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string code {
		get { return m_code; }
		set { m_code = Value; }
	}
	private string m_code;
	private clsTranslation Translation {
		get { return m_Translation; }
		set { m_Translation = Value; }
	}
	private clsTranslation m_Translation;


	string currentCode;
	public  DisplayName {
		get { return this.Translation.text(language) + " (" + this.code + ")"; }
	}



	public clsSector(string Code, clsTranslation translation)
	{
		object sql;
		sql = "INSERT INTO [Sector] (code,fk_Translation_key_name) ";
		sql += " values (" + da.SqlEncode(Code) + "," + translation.Key + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.code = Code;
		this.Translation = translation;

		iq.Sectors.Add(this.ID, this);
		iq.i_sector_code.Add(this.code, this);
		currentCode = this.code;


	}

	public object Insert()
	{

		return new clsSector(this.code, this.Translation);

	}


	public clsSector(int Id, string Code, clsTranslation translation)
	{
		this.ID = Id;
		this.code = Code;
		this.Translation = translation;

		iq.Sectors.Add(this.ID, this);
		iq.i_sector_code.Add(this.code, this);
		currentCode = this.code;

	}

	public string shortCode()
	{
		// a really dirty fix to back match with IQ1 for gregs snapshots/comparisons
		//DON NOT USE THIS FOR ANYTHING ELSE

		if (this.code.Contains("ISS"))
			return "SVR";
		if (this.code.Contains("SWD"))
			return "SWD";
		if (this.code.Contains("BCS"))
			return "BCS";
		if (this.code.Contains("COM"))
			return "COM";
		if (this.code.Contains("NET"))
			return "NET";

		return this.code;


	}




	public void update()
	{
		object sql;
		sql = "UPDATE [Sector] set code='" + this.code + "',fk_translation_key_name=" + this.Translation.Key + " WHERE id=" + this.ID;

		iq.i_sector_code.Remove(currentCode);

		currentCode = this.code;
		iq.i_sector_code.Add(currentCode, this);

		da.DBExecutesql(sql);

	}

}
