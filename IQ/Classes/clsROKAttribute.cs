using dataAccess;
using System.IO;



// Represents a collection of OS ROK attributes

public class clsROKAttribute
{

    public int ID;
    public string OsCode;
    public string Code;
    public clsTranslation Translation;

    public clsROKAttribute(int ID, string OsCode, string Code, clsTranslation Translation)
    {

        this.ID = ID;
        this.OsCode = OsCode;
        this.Code = Code;
        this.Translation = Translation;

    }

    public void update()
    {

        string sql = "UPDATE ROKAttributes set fk_translation_key_name=" + this.Translation.Key + " WHERE id = " + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

    }

}