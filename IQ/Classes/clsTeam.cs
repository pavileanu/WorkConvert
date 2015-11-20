using dataAccess;

[Serializable]
public class clsTeam : i_Editable
{

    public int ID { get; set; }
    public string Name { get; set; }
    public List<clsUser> Members { get; set; }
    public clsChannel Channel { get; set; }


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

        object sql = null;
        sql = "INSERT INTO Team(Name,FK_Channel_ID) VALUES (\'" + Name + "\'," + Channel.ID + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.Name = Name;
        this.Channel = channel;

        Channel.Teams.Add(this.ID, this);
        iq.Teams.Add(this.ID, this);

        this.Members = new List<clsUser>();


    }

    public clsTeam(int id, clsChannel channel, string Name)
    {

        this.ID = id;
        this.Name = Name;

        Channel.Teams.Add(this.ID, this);
        iq.Teams.Add(this.ID, this);

        this.Members = new List<clsUser>();
        this.Channel = channel;

    }

    public string displayName(clsLanguage Language)
    {
        string returnValue = "";
        returnValue = this.Name; //& "(" & Me.Code & ")"
        return returnValue;
    }


    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsTeam(this.Channel, this.Name); //we *now* call the constructor which makes a team and adds it to the approprtiate dictionaries/parent object

    }

    public void update(ref List<string> errormessages)
    {

        object sql = null;
        sql = "UPDATE [team] set name =" + da.SqlEncode(this.Name) + " WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);

    }

    public void delete(ref List<string> errormessages)
    {


        object sql = null;
        sql = "DELETE FROM [team] WHERE id=" + System.Convert.ToString(this.ID);

        try
        {
            da.DBExecutesql(sql, false);
        }
        catch (Exception ex)
        {
            errormessages.Add(ex.Message);
        }


    }


}