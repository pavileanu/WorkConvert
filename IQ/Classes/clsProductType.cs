using dataAccess;


public class clsProductType
{

    public int ID { get; set; }
    public string Code { get; set; }
    public clsTranslation Translation { get; set; }
    public short Order { get; set; }

    string oCode;

    public clsProductType(string code, clsTranslation translation, short order)
    {

        object sql = null;
        sql = "INSERT INTO ProductType (code,fk_Translation_key_text,[order]) VALUES (\'" + code + "\'," + Translation.Key + "," + System.Convert.ToString(order) + ");";
        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.Code = code;
        this.Translation = translation;
        this.Order = order;

        iq.ProductTypes.Add(this.ID, this);
        iq.i_ProductType_Code.Add(this.Code, this);

        oCode = this.Code;

    }
    public string get_DisplayName(clsLanguage Language)
    {
        string returnValue = "";
        returnValue = this.Code + " " + this.Translation.text(Language);
        return returnValue;
    }

    public clsProductType()
    {

    }

    public dynamic Insert()
    {

        return new clsProductType(this.Code, this.Translation, (short)0);

    }

    public void Update()
    {

        object sql = null;
        sql = "UPDATE [ProductType] SET code=" + da.SqlEncode(this.Code) + ",fk_translation_key_text=" + this.Translation.Key + ",[order]=" + System.Convert.ToString(Order) + " WHERE ID=" + System.Convert.ToString(this.ID);

        try
        {
            iq.i_ProductType_Code.Remove(oCode);
            iq.i_ProductType_Code.Add(this.Code, this);
            da.dbexecutesql(sql);

        }
        catch (System.Exception)
        {
            Debugger.Break(); // probably a duplictae code
        }

    }

    public void Delete()
    {

        object sql = null;
        sql = "DELETE FROM [ProductType] WHERE ID=" + System.Convert.ToString(this.ID);
        da.dbexecutesql(sql);

    }

    public clsProductType(int ID, string code, clsTranslation translation, short order)
    {

        this.ID = ID;
        this.Code = code;
        this.Translation = translation;
        this.Order = order;

        iq.ProductTypes.Add(this.ID, this);
        iq.i_ProductType_Code.Add(this.Code, this);

        oCode = this.Code;

    }


}