using dataAccess;
[Serializable()]
public class clsTeam : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string Name {
		get { return m_Name; }
		set { m_Name = Value; }
	}
	private string m_Name;
	private List<clsUser> Members {
		get { return m_Members; }
		set { m_Members = Value; }
	}
	private List<clsUser> m_Members;
	private clsChannel Channel {
		get { return m_Channel; }
		set { m_Channel = Value; }
	}
	private clsChannel m_Channel;



	public clsTeam()
	{
		//this is the 'delayed create' version - called by the generic editor
		//an instance is created - but it is not added to its parent channel unti it is Update()d
		this.ID = -1;
		this.Channel = null;
		this.Members = new List<clsUser>();
		this.Channel = null;

	}



	public clsTeam(clsChannel channel, string Name)
	{
		object sql;
		sql = "INSERT INTO Team(Name,FK_Channel_ID) VALUES ('" + Name + "'," + channel.ID + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.Name = Name;
		this.Channel = channel;

		channel.Teams.Add(this.ID, this);
		iq.Teams.Add(this.ID, this);

		this.Members = new List<clsUser>();


	}


	public clsTeam(int id, clsChannel channel, string Name)
	{
		this.ID = id;
		this.Name = Name;

		channel.Teams.Add(this.ID, this);
		iq.Teams.Add(this.ID, this);

		this.Members = new List<clsUser>();
		this.Channel = channel;

	}

	public string i_Editable.DisplayName(clsLanguage Language)
	{
		DisplayName = this.Name;
		//& "(" & Me.Code & ")"
	}


	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsTeam(this.Channel, this.Name);
		//we *now* call the constructor which makes a team and adds it to the approprtiate dictionaries/parent object

	}


	public void i_Editable.update(ref List<string> errormessages)
	{
		object sql;
		sql = "UPDATE [team] set name =" + da.SqlEncode(this.Name) + " WHERE ID=" + this.ID;
		da.DBExecutesql(sql, false);

	}


	public void i_Editable.delete(ref List<string> errormessages)
	{

		object sql;
		sql = "DELETE FROM [team] WHERE id=" + this.ID;

		try {
			da.DBExecutesql(sql, false);
		} catch (Exception ex) {
			errormessages.Add(ex.Message);
		}


	}


}

