using dataAccess;
using System.Runtime.Serialization;


[DataContract()]
public class clsInputType
{

    public int ID { get; set; }
    public string code { get; set; }
    public string name { get; set; }

    public dynamic get_displayName(clsLanguage langauge)
    {
        return this.name + " (" + this.code + ")";
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

        object sql = null;
        sql = "INSERT INTO [InputType] (code,name) values (" + da.SqlEncode(code) + "," + da.SqlEncode(name) + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.code = code;
        this.name = name;

        iq.InputTypes.Add(this.ID, this);
        iq.i_inputType_code.Add(this.code, this);

    }
}