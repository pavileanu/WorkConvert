using dataAccess;
using System.Globalization;


public class clsCurrency : i_Editable
{

    public int ID { get; set; }
    public string Code { get; set; }
    public string Code_HP { get; set; }
    public clsTranslation translation { get; set; } //of the currency name (into other languages) - "Dollars" might be Somethign else in chinese/russian etc.
    public string Symbol { get; set; }
    public float Rate { get; set; }
    public clsTranslation Notes { get; set; }

    //Moved to clsAccount - Euro and Swiss Franc which may be used in multiple cultures - mean this culture should be per account - giving maximum flexibility (it's defaulted from the buyers region)
    //Property Culture As String '.NET culture code for decimal point, thousands seperator etc. (default is EN)

    public string displayName(clsLanguage language)
    {
        string returnValue = "";
        returnValue = this.translation.text(language) + " (" + this.Code + ") " + this.Symbol;
        return returnValue;
    }


    public clsCurrency()
    {

    }

    public string format(decimal v, string culture, List<string> errorMessages, int decimalPlaces = 2)
    {
        string returnValue = "";

        returnValue = "unable to format currency";
        CultureInfo ci = null;
        try
        {
            ci = new CultureInfo(culture);
            returnValue = this.Symbol.Trim() + v.ToString("N" + decimalPlaces.ToString().Trim(), ci).Trim(); //Format as a currency.. to the cirrenct number of decimal places
        }
        catch
        {
            errorMessages.Add("The culture code " + culture + " is probably wrong.");
        }

        return returnValue;
    }

    public dynamic Insert(ref List<string> errorMessages)
    {

        return new clsCurrency(this.Code, this.Code_HP, this.translation, this.Symbol, this.Rate, this.Notes);

    }

    public void update(ref List<string> errorMessages)
    {

        object sql = null;
        //sql$ = "UPDATE [Currency] set code=" & da.SqlEncode(Me.Code) & ",symbol=" & da.SqlEncode(Me.Symbol) & ",rate=" & Me.Rate & ",fk_translation_key_notes=" & Me.translation.Key & ",culture=" & da.SqlEncode(Me.Culture) & " WHERE ID=" & Me.ID
        sql = "UPDATE [Currency] set code=" + da.SqlEncode(this.Code) + ",symbol=" + da.SqlEncode(this.Symbol) + ",rate=" + System.Convert.ToString(this.Rate) + ",fk_translation_key_notes=" + this.translation.Key + " WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

    }

    public void delete(ref List<string> errorMessages)
    {

        object SQL = null;
        SQL = "DELETE FROM [CURRENCY] WHERE ID=" + System.Convert.ToString(this.ID);

        try
        {
            //there's a good chance this will fail (due to RI)
            da.DBExecutesql(SQL);
            iq.Currencies.Remove(this.ID);

        }
        catch (Exception ex)
        {

            errorMessages.Add(ex.Message.ToString());
        }

    }

    public clsCurrency(string Code, string Code_HP, clsTranslation translation, string symbol, float rate, clsTranslation Notes) //, culture As String)
    {

        object nk = null;
        if (Notes == null)
        {
            nk = "null";
        }
        else
        {
            nk = Notes.Key;
        }

        object sql = null;
        //sql$ = "INSERT INTO Currency (Code,Symbol,Rate,fk_translation_key_Name,fk_translation_key_notes,culture) VALUES ("
        sql = "INSERT INTO Currency (Code,Code_HP,Symbol,Rate,fk_translation_key_Name,fk_translation_key_notes) VALUES (";
        //sql$ &= SqlEncode(Code) & "," & da.SqlEncode(symbol) & "," & rate & "," & translation.Key & "," & nk & "," & da.SqlEncode(culture) & " );"
        sql += da.SqlEncode(Code) + "," + da.SqlEncode(Code_HP) + "," + da.SqlEncode(symbol) + "," + System.Convert.ToString(rate) + "," + translation.Key + "," + System.Convert.ToString(nk) + " );";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.Code = Code;
        this.Code_HP = Code_HP;
        this.Symbol = symbol;
        this.Rate = rate;
        this.Notes = Notes;
        this.translation = translation;
        //Me.Culture = culture

        iq.Currencies.Add(this.ID, this);
        iq.i_currency_code.Add(this.Code, this);

    }

    public clsCurrency(int ID, string Code, string Code_HP, clsTranslation translation, string symbol, float rate, clsTranslation notes) //, culture As String)
    {

        this.ID = ID;
        this.Code = Code;
        this.Code_HP = Code_HP;
        this.Symbol = symbol;
        this.Rate = rate;
        this.Notes = notes;
        this.translation = translation;
        //    Me.Culture = culture
        iq.Currencies.Add(this.ID, this);
        iq.i_currency_code.Add(this.Code, this);

    }


}