using dataAccess;
using System.Runtime.Serialization;

[DataContract()]
public class clsLanguage
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
	private string LocalName {
		get { return m_LocalName; }
		set { m_LocalName = Value; }
	}
	private string m_LocalName;
	private bool RTL {
		get { return m_RTL; }
		set { m_RTL = Value; }
	}
	private bool m_RTL;
	private bool Live {
		get { return m_Live; }
		set { m_Live = Value; }
	}
	private bool m_Live;
	private bool Active {
		get { return m_Active; }
		set { m_Active = Value; }
	}
	private bool m_Active;


	public  displayName {
		get { displayName = this.LocalName + " (" + this.Code + ")"; }
	}


	public clsLanguage()
	{
		//required for reflection
	}


	public clsLanguage(int id, string code, string LocalName, bool RTL, bool live, bool active)
	{
		//This is an overriden constructor - becuase the ID *is* specified - it *knows* we' dont want to do a database insert
		this.ID = id;
		this.Code = code;
		this.LocalName = LocalName;
		this.RTL = RTL;
		this.Live = live;
		this.Active = active;

		iq.Languages.Add(this.ID, this);
		//add this language to the master list
		iq.i_language_Code.Add(this.Code, this);

	}

	public clsLanguage(string code, string LocalName, bool RTL, bool live, bool active)
	{
		//Creates a new (instance of the class cls)Language - populates its ID

		object sql;
		sql = "INSERT INTO [language] ([code],[LocalName],[RTL],[live],[active]) VALUES (" + da.SqlEncode(code) + "," + da.SqlEncode(LocalName) + "," + (int)RTL + "," + (int)live + "," + (int)active + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.Code = code;
		this.LocalName = LocalName;
		this.RTL = RTL;
		this.Live = live;
		this.Active = active;
		iq.Languages.Add(this.ID, this);
		//add this language to the master list
		iq.i_language_Code.Add(this.Code, this);

	}



}
