using dataAccess;

//There is one master set of attributes... weight, width, height, power consumption, color, flavour etc..
//Insances of this class represent thise attributes themselves (and the localised versions)... they DONT represent the values of those attributes - for that see clsProductAttribute

public enum EnumAttributeType
{
    Numeric,
    translation,
    rawText,
    KVP //translations - ordered by their numeric value (such as (for example) sets of CPU's

}
public class clsAttribute : i_Editable
{

    public int ID { get; set; }
    public string Code { get; set; }
    public clsTranslation Translation { get; set; } //of the attribute itself (eg. "width","height","Speed")
    public int Order { get; set; }
    public EnumAttributeType type { get; set; }

    public Dictionary<int, clsProduct> Products { get; set; }

    string oCode;
    public clsAttribute()
    {


    }
    public clsAttribute(string Code, clsTranslation translation, int order)
    {

        //master' Attributes are instatiated with a code, and one translation of their name (probably the english one)
        this.Translation = translation;
        this.Code = Code;
        this.Order = order;
        Products = new Dictionary<int, clsProduct>();
        object sql = null;
        sql = "INSERT INTO ATTRIBUTE(code,fk_translation_key_name,[order]) values (" + da.SqlEncode(Code) + "," + Translation.Key + "," + System.Convert.ToString(order) + ");";
        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));

        //now add it to the 'master' list of attributes
        iq.Attributes.Add(this.ID, this);
        iq.i_attribute_code.Add(this.Code, this);

        oCode = this.Code;


    }

    public dynamic Insert(ref List<string> errorMessages)
    {


        return new clsAttribute(this.Code, this.Translation, this.Order);

    }

    //Public Sub Update()

    //    Dim sql$
    //    sql$ = "UPDATE [attribute] set code=" & da.SqlEncode(Me.Code$) & ",fk_translation_key_name=" & Me.Translation.Key & ",[order]=" & Me.Order & " WHERE id=" & Me.ID
    //    da.DBExecutesql(sql)

    //    'remove from the master index and add back (in case the code has been changed)... which it will have for newly added Attributes (from the intial "" )
    //    iq.i_attribute_code.Remove(oCode)
    //    iq.i_attribute_code.Add(Me.Code, Me)
    //    oCode = Me.Code


    //End Sub

    public void delete(ref List<string> errorMessages)
    {

        //You won't be able to delete an attribute that is in use

        object sql = null;
        sql = "DELETE FROM [attribute] where id=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

        iq.Attributes.Remove(this.ID);
        iq.i_attribute_code.Remove(this.Code);


    }

    public clsAttribute(int ID, string Code, clsTranslation translation, int order)
    {

        //This version of the constructor ('new' sub)... DOESNT persist (write to the database) the attribute

        this.ID = ID;
        this.Translation = translation;
        this.Code = Code;
        this.Order = order;
        //now add it to the 'master' list of attributes
        iq.Attributes.Add(this.ID, this);
        iq.i_attribute_code.Add(this.Code, this);

        Products = new Dictionary<int, clsProduct>();

        oCode = this.Code;


    }
    public void update(ref List<string> Errormessages)
    {
        object sql = null;
        sql = "UPDATE [attribute] set code=" + da.SqlEncode(this.Code) + ",fk_translation_key_name=" + this.Translation.Key + ",[order]=" + System.Convert.ToString(this.Order) + " WHERE id=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

        //remove from the master index and add back (in case the code has been changed)... which it will have for newly added Attributes (from the intial "" )
        iq.i_attribute_code.Remove(oCode);
        iq.i_attribute_code.Add(this.Code, this);
        oCode = this.Code;
    }


    public string displayName(clsLanguage language)
    {
        return this.Translation.text(language);
    }
}