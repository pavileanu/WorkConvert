using dataAccess;

public class clsPromo : i_Editable
{

	private int Id {
		get { return m_Id; }
		set { m_Id = Value; }
	}
	private int m_Id;
	private string Code {
		get { return m_Code; }
		set { m_Code = Value; }
	}
	private string m_Code;
	private clsTranslation Description {
		get { return m_Description; }
		set { m_Description = Value; }
	}
	private clsTranslation m_Description;
	private string FieldProperty_Filter {
		get { return m_FieldProperty_Filter; }
		set { m_FieldProperty_Filter = Value; }
	}
	private string m_FieldProperty_Filter;
	private string FieldProperty_Value {
		get { return m_FieldProperty_Value; }
		set { m_FieldProperty_Value = Value; }
	}
	private string m_FieldProperty_Value;
	private clsRegion Region {
		get { return m_Region; }
		set { m_Region = Value; }
	}
	private clsRegion m_Region;

	public clsPromo(int ID, string Code, clsTranslation Description, clsRegion Region, string FieldProperty_Filter, string FieldProperty_Value, string SystemType)
	{
		this.Id = ID;
		this.Code = Code;
		this.Description = Description;
		this.FieldProperty_Filter = FieldProperty_Filter;
		this.Region = Region;
		this.FieldProperty_Value = FieldProperty_Value;

		iq.Promos.Add(this.Id, this);
		AddRegion(Region);
		AddSystemType(SystemType);
	}

	public void loadRegionIteration(clsRegion Region)
	{
		if (!iq.i_PromoRegions.ContainsKey(Region))
			iq.i_PromoRegions.Add(Region, new List<clsPromo>());
		if (!iq.i_PromoRegions(Region).Contains(this))
			iq.i_PromoRegions(Region).Add(this);

		foreach ( r in Region.Children.Values) {
			loadRegionIteration(r);
		}
	}
	public void AddRegion(clsRegion region)
	{
		loadRegionIteration(region);
	}
	public void AddSystemType(string systype)
	{
		if (!iq.i_PromoSystemTypes.ContainsKey(this))
			iq.i_PromoSystemTypes.Add(this, new List<string>());
		if (!iq.i_PromoSystemTypes(this).Contains(systype))
			iq.i_PromoSystemTypes(this).Add(systype);
	}



	public void i_Editable.delete(ref List<string> Errormessages)
	{
	}

	public string i_Editable.displayName(clsLanguage Language)
	{
		return Description.text(Language);
	}

	public object i_Editable.Insert(ref List<string> Errormessages)
	{
		return null;
	}


	public void i_Editable.update(ref List<string> Errormessages)
	{
		//UNFINISHED / TESTED

		object sql;
		sql = "UPDATE [promo] set code=" + da.SqlEncode(this.Code) + ",fk_translation_key_description=" + this.Description.Key + " WHERE id=" + this.Id;
		da.DBExecutesql(sql);


	}
}

