using dataAccess;


public class clsSector
{

    public int ID { get; set; }
    public string code { get; set; }
    public clsTranslation Translation { get; set; }

    string currentCode;

    public dynamic get_DisplayName(clsLanguage language)
    {
        return this.Translation.text(language) + " (" + this.code + ")";
    }


    public clsSector(string Code, clsTranslation translation)
    {

        object sql = null;
        sql = "INSERT INTO [Sector] (code,fk_Translation_key_name) ";
        sql += " values (" + da.SqlEncode(Code) + "," + Translation.Key + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.code = Code;
        this.Translation = translation;

        iq.Sectors.Add(this.ID, this);
        iq.i_sector_code.Add(this.code, this);
        currentCode = this.code;


    }

    public dynamic Insert()
    {

        return new clsSector(this.code, this.Translation);

    }

    public clsSector(int Id, string Code, clsTranslation translation)
    {

        this.ID = Id;
        this.code = Code;
        this.Translation = translation;

        iq.Sectors.Add(this.ID, this);
        iq.i_sector_code.Add(this.code, this);
        currentCode = this.code;

    }

    public string shortCode() // a really dirty fix to back match with IQ1 for gregs snapshots/comparisons
    {
        //DON NOT USE THIS FOR ANYTHING ELSE

        if (this.code.Contains("ISS"))
        {
            return "SVR";
        }
        if (this.code.Contains("SWD"))
        {
            return "SWD";
        }
        if (this.code.Contains("BCS"))
        {
            return "BCS";
        }
        if (this.code.Contains("COM"))
        {
            return "COM";
        }
        if (this.code.Contains("NET"))
        {
            return "NET";
        }

        return this.code;


    }



    public void update()
    {

        object sql = null;
        sql = "UPDATE [Sector] set code=\'" + this.code + "\',fk_translation_key_name=" + this.Translation.Key + " WHERE id=" + System.Convert.ToString(this.ID);

        iq.i_sector_code.Remove(currentCode);

        currentCode = this.code;
        iq.i_sector_code.Add(currentCode, this);

        da.DBExecutesql(sql);

    }

}