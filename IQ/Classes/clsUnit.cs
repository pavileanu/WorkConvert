using dataAccess;


public class clsUnit : i_Editable
{

    public int ID { get; set; }
    public clsTranslation Translation { get; set; } //carries the translation.key - and exposes (via an indexed defautl property) the underlying text
    public string Symbol { get; set; }
    public string Code { get; set; } //our internal code for referencing these units eg KG (most of the time it will be the same as the name)
    public int MeasureID { get; set; }

    string oCode;

    public string displayName(clsLanguage language)
    {

        return this.Translation.text(language);
    }

    public clsUnit()
    {


    }

    public void delete(ref List<string> errormessages)
    {



        try
        {
            da.DBExecutesql("DELETE FROM UNIT WHERE ID=" + System.Convert.ToString(this.ID)); //will often fail due to RI (expose this error through the editor)
        }
        catch (Exception ex)
        {
            errormessages.Add(ex.Message.ToString());
        }

    }


    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsUnit(this.Code, this.Translation, this.Symbol, this.MeasureID);

    }

    public void update(ref List<string> errormessages)
    {

        object sql = null;
        sql = "UPDATE [Unit] set ";
        sql += "code=" + da.SqlEncode(this.Code) + ",";
        sql += "fk_translation_key_name=" + this.Translation.Key + ",";
        sql += "symbol=" + da.SqlEncode(this.Symbol);
        sql += " WHERE ID=" + System.Convert.ToString(this.ID);

        iq.i_unit_code.Remove(oCode);
        iq.i_unit_code.Add(this.Code, this);

        oCode = this.Code;

        da.DBExecutesql(sql);

    }


    public clsUnit(string code, clsTranslation translation, string Symbol, int MeasureID)
    {

        this.Translation = translation;
        this.Symbol = Symbol;
        this.Code = code;
        this.MeasureID = MeasureID;

        object sql = null;
        sql = "Insert into [Unit] ([code],[FK_Translation_key_name],[symbol],FK_Measure_ID) values (" + da.SqlEncode(code) + "," + Translation.Key + "," + da.SqlEncode(this.Symbol) + "," + da.SqlEncode(this.MeasureID) + ");";
        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));

        iq.Units.Add(this.ID, this);
        if (!iq.i_unit_code.ContainsKey(code)) //hmm not sure why this is needed
        {
            iq.i_unit_code.Add(this.Code, this);
        }

        oCode = this.Code;


    }



    public clsUnit(int id, string code, clsTranslation translation, string Symbol, int MeasureID)
    {

        this.ID = id;
        this.Translation = translation;
        this.Symbol = Symbol;
        this.Code = code;
        this.MeasureID = MeasureID;

        iq.Units.Add(this.ID, this);

        iq.i_unit_code.Add(this.Code, this);

        oCode = this.Code;

    }

}