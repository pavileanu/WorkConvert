using dataAccess;


//States are (primarily) for quotes - but are pretty generic so could be extended/re-used for other things (which is what 'group' is for)

public class clsState
{

    public int ID { get; set; }
    public string code { get; set; }
    public clsTranslation Translation { get; set; }
    public string group { get; set; }
    public int Order { get; set; }
    public string Colour { get; set; }

    private string CompoundKey;

    public clsState()
    {
        // Me.Translation = iq.AddTranslation("Edit me", English, Nothing, True) - now done in set defauts (for all translations)
    }

    public clsState Insert()
    {
        return new clsState(this.group, this.code, this.Translation, this.Order, this.Colour);
    }


    public string get_Displayname(clsLanguage language)
    {
        return this.Translation.text(language) + " (" + this.code + ")";
    }


    public clsState(int id, string group, string code, clsTranslation translation, int order, string colour)
    {

        //This is an overriden constructor - becuase the ID *is* specified - it *knows* we' dont want to do a database insert
        this.ID = id;
        this.group = group;
        this.code = code;
        this.Translation = translation;
        this.Order = order;
        this.Colour = colour;

        iq.States.Add(this.ID, this);

        //The states index has a compound key of 'code-group'
        CompoundKey = this.group + "-" + this.code;
        iq.i_state_GroupCode.Add(CompoundKey, this);

    }

    public clsState(string group, string code, clsTranslation translation, int order, string colour)
    {

        //Creates a new (instance of the class cls)Language - populates its ID

        object sql = null;
        sql = "INSERT INTO [State] ([group],[code],fk_translation_key,[order],colour) ";
        sql += "VALUES (" + da.SqlEncode(group) + "," + da.SqlEncode(code) + "," + Translation.Key + "," + System.Convert.ToString(order) + "," + da.SqlEncode(colour) + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.group = group;
        this.code = code;
        this.Translation = translation;
        this.Order = order;
        this.Colour = colour;

        iq.States.Add(this.ID, this);

        //The states index has a compound key of 'code-group'

        CompoundKey = this.group + "-" + this.code;
        iq.i_state_GroupCode.Add(CompoundKey, this);


    }

    public void Update()
    {

        object sql = null;
        sql = "UPDATE [State] set ";
        sql += "[Group]=" + da.SqlEncode(this.group) + ",";
        sql += "[Code]=" + da.SqlEncode(this.code) + ",";
        sql += "[FK_Translation_Key]=" + this.Translation.Key + ",";
        sql += "[Order]=" + System.Convert.ToString(this.Order) + ",";
        sql += "[Colour]=" + da.SqlEncode(this.Colour);
        sql += " WHERE ID=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql, false);

        //Update the 'index' object (becuase my 'key' may have changed)
        iq.i_state_GroupCode.Remove(CompoundKey);
        CompoundKey = this.group + "-" + this.code;
        iq.i_state_GroupCode.Add(CompoundKey, this);



    }




}