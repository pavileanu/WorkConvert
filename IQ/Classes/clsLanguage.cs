using dataAccess;
using System.Runtime.Serialization;


[DataContract()]
public class clsLanguage
{
    public int ID { get; set; }
    public string Code { get; set; }
    public string LocalName { get; set; }
    public bool RTL { get; set; }
    public bool Live { get; set; }
    public bool Active { get; set; }


    public dynamic get_displayName(clsLanguage language)
    {
        dynamic returnValue = default(dynamic);
        returnValue = this.LocalName + " (" + this.Code + ")";
        return returnValue;
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

        iq.Languages.Add(this.ID, this); //add this language to the master list
        iq.i_language_Code.Add(this.Code, this);

    }
    public clsLanguage(string code, string LocalName, bool RTL, bool live, bool active)
    {

        //Creates a new (instance of the class cls)Language - populates its ID

        object sql = null;
        sql = "INSERT INTO [language] ([code],[LocalName],[RTL],[live],[active]) VALUES (" + da.SqlEncode(code) + "," + da.SqlEncode(LocalName) + "," + System.Convert.ToString(RTL) + "," + System.Convert.ToString(live) + "," + System.Convert.ToString(active) + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.Code = code;
        this.LocalName = LocalName;
        this.RTL = RTL;
        this.Live = live;
        this.Active = active;
        iq.Languages.Add(this.ID, this); //add this language to the master list
        iq.i_language_Code.Add(this.Code, this);

    }



}