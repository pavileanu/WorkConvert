using dataAccess;

public class clsRole
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
	private Dictionary<int, clsRight> Rights {
		get { return m_Rights; }
		set { m_Rights = Value; }
	}
	private Dictionary<int, clsRight> m_Rights;
	private Dictionary<string, clsRight> i_right_code {
		get { return m_i_right_code; }
		set { m_i_right_code = Value; }
	}
	private Dictionary<string, clsRight> m_i_right_code;


	private  DisplayName {
		get { DisplayName = this.Translation.text(language); }
	}

	private  EnglishDisplayName {
		get { EnglishDisplayName = this.Translation.text(English); }
	}

	public clsRole()
	{
		this.ID = -1;
		this.Rights = new Dictionary<int, clsRight>();
		this.i_right_code = new Dictionary<string, clsRight>();

	}

	public clsRole(string Code, clsTranslation translation)
	{
		object sql;
		sql = "INSERT INTO [Role] (code,fk_Translation_key) ";
		sql += " values (" + da.SqlEncode(Code) + "," + translation.Key + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.Code = Code;
		this.Translation = translation;
		this.Rights = new Dictionary<int, clsRight>();
		this.i_right_code = new Dictionary<string, clsRight>();

		iq.i_role_Code.Add(this.Code, this);

	}


	public clsRole(int Id, string Code, clsTranslation translation)
	{
		this.ID = Id;
		this.Code = Code;
		this.Translation = translation;
		this.Rights = new Dictionary<int, clsRight>();
		this.i_right_code = new Dictionary<string, clsRight>();

		iq.i_role_Code.Add(this.Code, this);

	}

	public void AddRight(clsRight right)
	{
		if (Rights.ContainsKey(right.ID))
			return;
		object sql;
		sql = "INSERT INTO [RoleRight] (fk_Role_Id,fk_right_id) ";
		sql += " values (" + this.ID + "," + right.ID + ");";

		da.DBExecutesql(sql, true);
		this.Rights.Add(right.ID, right);
		this.i_right_code.Add(right.Code, right);
	}

}
//clsRole

