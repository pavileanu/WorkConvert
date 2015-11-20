using dataAccess;

public class clsRight
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


	public clsRight(string Code, clsTranslation translation)
	{
		object sql;
		sql = "INSERT INTO [Right] (code,fk_Translation_key) ";
		sql += " values (" + da.SqlEncode(Code) + "," + translation.Key + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.Code = Code;
		this.Translation = translation;
		iq.i_right_Code.Add(Code, this);

	}

	public clsRight()
	{
		this.ID = -1;
	}



	public clsRight(int ID, string Code, clsTranslation translation)
	{
		this.ID = ID;
		this.Code = Code;
		this.Translation = translation;
		iq.i_right_Code.Add(Code, this);
	}


	public string displayName(clsLanguage Language)
	{

		displayName = this.Translation.text(Language);

	}

	public clsRight Insert()
	{

		return new clsRight(this.Code, this.Translation);

	}



	public void update()
	{
		if (this.ID == -1)
			System.Diagnostics.Debugger.Break();

		object sql;
		sql = "UPDATE [Right] SET code=" + da.SqlEncode(this.Code) + ",fk_translation_key=" + this.Translation.Key + " WHERE ID=" + this.ID;
		da.dbexecutesql(sql);

	}



}



