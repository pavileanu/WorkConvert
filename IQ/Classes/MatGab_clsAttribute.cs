using dataAccess;
//There is one master set of attributes... weight, width, height, power consumption, color, flavour etc..
//Insances of this class represent thise attributes themselves (and the localised versions)... they DONT represent the values of those attributes - for that see clsProductAttribute

public enum EnumAttributeType
{
	Numeric,
	translation,
	rawText,
	KVP
	//translations - ordered by their numeric value (such as (for example) sets of CPU's

}
public class clsAttribute : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string Code {
		get { return m_Code; }
		set { m_Code = Value; }
	}
	private string m_Code;
	private clsTranslation Translation {
		get { return m_Translation; }
		set { m_Translation = Value; }
	}
	private clsTranslation m_Translation;
	//of the attribute itself (eg. "width","height","Speed")
	private int Order {
		get { return m_Order; }
		set { m_Order = Value; }
	}
	private int m_Order;
	private EnumAttributeType type {
		get { return m_type; }
		set { m_type = Value; }
	}
	private EnumAttributeType m_type;

	private Dictionary<int, clsProduct> Products {
		get { return m_Products; }
		set { m_Products = Value; }
	}
	private Dictionary<int, clsProduct> m_Products;

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
		object sql;
		sql = "INSERT INTO ATTRIBUTE(code,fk_translation_key_name,[order]) values (" + da.SqlEncode(Code) + "," + translation.Key + "," + order + ");";
		this.ID = da.DBExecutesql(sql, true);

		//now add it to the 'master' list of attributes
		iq.Attributes.Add(this.ID, this);
		iq.i_attribute_code.Add(this.Code, this);

		oCode = this.Code;


	}

	public object i_Editable.insert(ref List<string> errorMessages)
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


	public void i_Editable.delete(ref List<string> errorMessages)
	{
		//You won't be able to delete an attribute that is in use

		object sql;
		sql = "DELETE FROM [attribute] where id=" + this.ID;
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
	public void i_Editable.update(ref List<string> Errormessages)
	{
		object sql;
		sql = "UPDATE [attribute] set code=" + da.SqlEncode(this.Code) + ",fk_translation_key_name=" + this.Translation.Key + ",[order]=" + this.Order + " WHERE id=" + this.ID;
		da.DBExecutesql(sql);

		//remove from the master index and add back (in case the code has been changed)... which it will have for newly added Attributes (from the intial "" )
		iq.i_attribute_code.Remove(oCode);
		iq.i_attribute_code.Add(this.Code, this);
		oCode = this.Code;
	}


	public string i_Editable.displayName(clsLanguage language)
	{
		return this.Translation.text(language);
	}
}
