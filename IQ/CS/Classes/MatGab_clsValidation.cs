using dataAccess;

public class clsValidation
{

	private string ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private string m_ID;
	private string description {
		get { return m_description; }
		set { m_description = Value; }
	}
	private string m_description;
	private string regEx {
		get { return m_regEx; }
		set { m_regEx = Value; }
	}
	private string m_regEx;
	private string ViolationMessage {
		get { return m_ViolationMessage; }
		set { m_ViolationMessage = Value; }
	}
	private string m_ViolationMessage;


	public clsValidation()
	{
	}

	public clsValidation Insert()
	{
		return new clsValidation(this.description, this.regEx, this.ViolationMessage);
	}

	public  DisplayName {

		get { return this.description; }
	}



	public clsValidation(int id, string description, string regex, string violation)
	{
		this.ID = id;
		this.description = description;
		this.regEx = regex;
		this.ViolationMessage = violation;

		iq.Validations.Add(this.ID, this);

	}


	public clsValidation(string description, string regex, string violation)
	{
		this.description = description;
		this.regEx = regex;
		this.ViolationMessage = violation;

		object sql;
		sql = "INSERT INTO [validation] (descripion,regex,violation) VALUES(" + da.SqlEncode(this.description) + "," + da.SqlEncode(this.regEx) + "," + da.SqlEncode(this.ViolationMessage) + ");";
		this.ID = da.DBExecutesql(sql, true);

		iq.Validations.Add(this.ID, this);

	}


	public void update()
	{
		object sql;
		sql = "UPDATE [validation] set ";
		sql += "description=" + da.SqlEncode(this.description) + ",";
		sql += "regex=" + da.SqlEncode(this.regEx) + ",";
		sql += "viloationmessage=" + da.SqlEncode(this.ViolationMessage);

		da.dbexecutesql(sql, false);

	}




}
