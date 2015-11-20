using dataAccess;
using System.IO;



// Represents a single-field address of some sort - e.g. an email address or a URL

public class clsAddress : i_Editable
{

    public int ID;
    public string Code;
    public clsTranslation Translation;

    public clsAddress()
    {

    }

    public clsAddress(int ID, string Code, clsTranslation Translation)
    {

        this.ID = ID;
        this.Code = Code;
        this.Translation = Translation;

    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsAddress(this.ID, this.Code, this.Translation);

    }

    public void delete(ref List<string> errorMessages)
    {

        string sql = "delete from [Address] where id=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql);

    }

    public void update(ref List<string> errorMessages)
    {

        System.Char sql = string.Format("update [Address] set Code=\'{0}\' where ID={1}", this.Code, this.ID);

        da.DBExecutesql(sql, false);

    }

    public string displayName(clsLanguage language)
    {

        return this.Translation.text(language);

    }

}