using dataAccess;
using System.IO;



// Represents a localized legal statement

public class clsLegal : i_Editable
{

    public int ID;
    public string Code;
    public clsTranslation Translation;

    public clsLegal()
    {

    }

    public clsLegal(int ID, string Code, clsTranslation Translation)
    {

        this.ID = ID;
        this.Code = Code;
        this.Translation = Translation;

    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsLegal(this.ID, this.Code, this.Translation);

    }

    public void delete(ref List<string> errorMessages)
    {

        string sql = "delete from [Legal] where id=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql);

    }

    public void update(ref List<string> errorMessages)
    {

        System.Char sql = string.Format("update [Legal] set Code=\'{0}\' where ID={1}", this.Code, this.ID);

        da.DBExecutesql(sql, false);

    }

    public string displayName(clsLanguage language)
    {

        return this.Translation.text(language);

    }

}