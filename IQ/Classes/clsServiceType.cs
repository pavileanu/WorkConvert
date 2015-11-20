using dataAccess;
using System.IO;



// Represents an HPE Care Pack service type level

public class clsServiceType : i_Editable
{

    public int ID;
    public string mfrCode;
    public clsTranslation Title;
    public clsTranslation Description;
    public bool ServiceTypeDefault;

    private const string TABLE = "ServiceType";

    public clsServiceType()
    {

    }

    public clsServiceType(int id, string mfrCode, clsTranslation title, clsTranslation description, bool serviceTypeDefault)
    {

        this.ID = id;
        this.mfrCode = mfrCode;
        this.Title = title;
        this.Description = description;
        this.ServiceTypeDefault = serviceTypeDefault;

    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsServiceType(this.ID, this.mfrCode, this.Title, this.Description, this.ServiceTypeDefault);

    }

    public void delete(ref List<string> errorMessages)
    {

        System.Char sql = string.Format("delete from {0} where id={1}", TABLE, this.ID);

        da.DBExecutesql(sql);

    }

    public void update(ref List<string> errorMessages)
    {

        System.Char sql = string.Format("update {0} set mfrCode=\'{1}\', IsDefault={2} where ID={3}", TABLE, this.mfrCode, this.ServiceTypeDefault, this.ID);

        da.DBExecutesql(sql, false);

    }

    public string displayName(clsLanguage language)
    {

        return this.Title.text(language);

    }

}