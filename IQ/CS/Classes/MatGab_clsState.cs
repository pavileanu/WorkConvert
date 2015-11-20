using dataAccess;

//States are (primarily) for quotes - but are pretty generic so could be extended/re-used for other things (which is what 'group' is for)

public class clsState
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
	private string @group {
		get { return m_group; }
		set { m_group = Value; }
	}
	private string m_group;
	private int Order {
		get { return m_Order; }
		set { m_Order = Value; }
	}
	private int m_Order;
	private string Colour {
		get { return m_Colour; }
		set { m_Colour = Value; }
	}
	private string m_Colour;


	private string CompoundKey;
	public clsState()
	{
		// Me.Translation = iq.AddTranslation("Edit me", English, Nothing, True) - now done in set defauts (for all translations)
	}

	public clsState Insert()
	{
		return new clsState(this.@group, this.code, this.Translation, this.Order, this.Colour);
	}

	public string Displayname {

		get { return this.Translation.text(language) + " (" + this.code + ")"; }
	}



	public clsState(int id, string @group, string code, clsTranslation translation, int order, string colour)
	{
		//This is an overriden constructor - becuase the ID *is* specified - it *knows* we' dont want to do a database insert
		this.ID = id;
		this.@group = @group;
		this.code = code;
		this.Translation = translation;
		this.Order = order;
		this.Colour = colour;

		iq.States.Add(this.ID, this);

		//The states index has a compound key of 'code-group'
		CompoundKey = this.@group + "-" + this.code;
		iq.i_state_GroupCode.Add(CompoundKey, this);

	}


	public clsState(string @group, string code, clsTranslation translation, int order, string colour)
	{
		//Creates a new (instance of the class cls)Language - populates its ID

		object sql;
		sql = "INSERT INTO [State] ([group],[code],fk_translation_key,[order],colour) ";
		sql += "VALUES (" + da.SqlEncode(@group) + "," + da.SqlEncode(code) + "," + translation.Key + "," + order + "," + da.SqlEncode(colour) + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.@group = @group;
		this.code = code;
		this.Translation = translation;
		this.Order = order;
		this.Colour = colour;

		iq.States.Add(this.ID, this);

		//The states index has a compound key of 'code-group'

		CompoundKey = this.@group + "-" + this.code;
		iq.i_state_GroupCode.Add(CompoundKey, this);


	}


	public void Update()
	{
		object sql;
		sql = "UPDATE [State] set ";
		sql += "[Group]=" + da.SqlEncode(this.@group) + ",";
		sql += "[Code]=" + da.SqlEncode(this.code) + ",";
		sql += "[FK_Translation_Key]=" + this.Translation.Key + ",";
		sql += "[Order]=" + this.Order + ",";
		sql += "[Colour]=" + da.SqlEncode(this.Colour);
		sql += " WHERE ID=" + this.ID;

		da.DBExecutesql(sql, false);

		//Update the 'index' object (becuase my 'key' may have changed)
		iq.i_state_GroupCode.Remove(CompoundKey);
		CompoundKey = this.@group + "-" + this.code;
		iq.i_state_GroupCode.Add(CompoundKey, this);



	}




}
