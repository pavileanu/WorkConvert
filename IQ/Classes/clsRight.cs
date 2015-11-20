using dataAccess;


public class clsRight
{
    public int ID { get; set; }
    public string Code { get; set; }
    public clsTranslation Translation { get; set; }

    public clsRight(string Code, clsTranslation translation)
    {

        object sql = null;
        sql = "INSERT INTO [Right] (code,fk_Translation_key) ";
        sql += " values (" + da.SqlEncode(Code) + "," + Translation.Key + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.Code = Code;
        this.Translation = translation;
        iq.i_right_Code.Add(Code, this);

    }

    public clsRight()
    {
        this.ID = -1;
    }


    public clsRight(int ID, string Code, clsTranslation translation)
    {

        this.ID = ID;
        this.Code = Code;
        this.Translation = translation;
        iq.i_right_Code.Add(Code, this);
    }


    public string displayName(clsLanguage Language)
    {
        string returnValue = "";

        returnValue = System.Convert.ToString(this.Translation.text(Language));

        return returnValue;
    }

    public clsRight Insert()
    {

        return new clsRight(this.Code, this.Translation);

    }


    public void update()
    {

        if (this.ID == -1)
        {
            Debugger.Break();
        }

        object sql = null;
        sql = "UPDATE [Right] SET code=" + da.SqlEncode(this.Code) + ",fk_translation_key=" + this.Translation.Key + " WHERE ID=" + System.Convert.ToString(this.ID);
        da.dbexecutesql(sql);

    }



}