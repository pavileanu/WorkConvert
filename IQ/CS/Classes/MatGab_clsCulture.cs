using dataAccess;

public class clsCulture
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
	private string Name {
		get { return m_Name; }
		set { m_Name = Value; }
	}
	private string m_Name;
	public clsCulture(int ID, string code, string name)
	{
		this.ID = ID;
		this.Code = code;
		this.Name = name;
		iq.Cultures.Add(ID, this);
		iq.i_culture_code.Add(code, this);
	}
}
