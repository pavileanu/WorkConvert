using dataAccess;


public class clsScheme : i_Editable
{


    public int ID;
    public clsTranslation Name;
    public string code;
    public clsRegion Region;
    public DateTime StartDate;
    public DateTime EndDate;
    public bool Active;

    public void delete(ref List<string> errormessages)
    {

        object sql = null;
        sql = "delete from Scheme where id=me.id";
        try
        {
            da.DBExecutesql(sql);
            iq.Schemes.Remove(this.ID);

        }
        catch (Exception ex)
        {
            errormessages.Add(ex.Message);
        }


    }

    public string displayName(clsLanguage Language)
    {

        return this.Name.text(Language);

    }


    public clsScheme() //the editor requires a parameterless constructor
    {



    }

    public string compoundKey()
    {
        return this.Region.ID + "^" + System.Convert.ToString(this.StartDate) + "^" + System.Convert.ToString(this.EndDate);
    }
    public clsScheme(int id, string code, clsTranslation name, clsRegion Region, DateTime Startdate, DateTime Enddate)
    {

        this.ID = id;
        this.code = code;
        this.Name = name;
        this.Region = Region;
        this.StartDate = Startdate;
        this.EndDate = Enddate;

        iq.Schemes.Add(this.ID, this);
        if (!iq.i_scheme_code.ContainsKey(this.code))
        {
            iq.i_scheme_code.Add(this.code, new List<clsScheme>());
        }
        iq.i_scheme_code(this.code).Add(this);

    }

    public clsScheme(string code, clsTranslation name, clsRegion Region, DateTime Startdate, DateTime Enddate, DataTable writecache = null)
    {

        this.code = code;
        this.Name = name;
        this.Region = Region;
        this.StartDate = Startdate;
        this.EndDate = Enddate;

        if (!iq.i_scheme_code.ContainsKey(this.code))
        {
            iq.i_scheme_code.Add(this.code, new List<clsScheme>());
        }
        iq.i_scheme_code(this.code).Add(this);


        if (writecache == null)
        {
            object sql = null;
            sql = "INSERT INTO [Scheme] (code,fk_translation_key_name,StartDate,EndDate,fk_region_id) ";
            sql += "VALUES (" + da.SqlEncode(this.code) + "," + Name.Key + "," + da.UniversalDate(Startdate) + "," + da.UniversalDate(Enddate) + "," + Region.ID + ");";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));

            iq.Schemes.Add(this.ID, this);

        }
        else
        {
            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();
            row["code"] = this.code;
            row["fk_translation_key_name"] = this.Name.Key;
            row["startdate"] = Startdate;
            row["enddate"] = Enddate;
            row["fk_region_id"] = Region.ID;
            writecache.Rows.Add(row);
        }

    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsScheme("new", iq.AddTranslation("New Loyalty Scheme", English, "Lschemes", 0, null, 0, true), r_worldwide, DateTime.Now, DateAndTime.DateAdd(DateInterval.Year, 1, DateTime.Now));

    }

    public void update(ref List<string> errormessages)
    {

        object sql = null;
        sql = "UPDATE [scheme] set (code=" + da.SqlEncode(this.code) + ",fk_translation_key_name=" + this.Name.Key + ",StartDate=" + da.UniversalDate(this.StartDate) + ",enddate=" + da.UniversalDate(this.EndDate) + ",fk_region_id=" + this.Region.ID + " WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);

    }

}