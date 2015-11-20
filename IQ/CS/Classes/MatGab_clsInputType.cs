using dataAccess;
using System.Runtime.Serialization;

[DataContract()]
public class clsInputType
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
	private string name {
		get { return m_name; }
		set { m_name = Value; }
	}
	private string m_name;

	public  displayName {
		get { return this.name + " (" + this.code + ")"; }
	}


	public clsInputType(int id, string code, string name)
	{
		this.ID = id;
		this.code = code;
		this.name = name;

		iq.InputTypes.Add(this.ID, this);
		iq.i_inputType_code.Add(this.code, this);

	}




	public clsInputType(string code, string name)
	{
		object sql;
		sql = "INSERT INTO [InputType] (code,name) values (" + da.SqlEncode(code) + "," + da.SqlEncode(name) + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.code = code;
		this.name = name;

		iq.InputTypes.Add(this.ID, this);
		iq.i_inputType_code.Add(this.code, this);

	}
}
