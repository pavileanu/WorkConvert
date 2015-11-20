using dataAccess;


public class clsPromo : i_Editable
{

    public int Id { get; set; }
    public string Code { get; set; }
    public clsTranslation Description { get; set; }
    public string FieldProperty_Filter { get; set; }
    public string FieldProperty_Value { get; set; }
    public clsRegion Region { get; set; }

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
        {
            iq.i_PromoRegions.Add(Region, new List<clsPromo>());
        }
        if (!iq.i_PromoRegions(Region).Contains(this))
        {
            iq.i_PromoRegions(Region).Add(this);
        }

        foreach (var r in Region.Children.Values)
        {
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
        {
            iq.i_PromoSystemTypes.Add(this, new List<string>());
        }
        if (!iq.i_PromoSystemTypes(this).Contains(systype))
        {
            iq.i_PromoSystemTypes(this).Add(systype);
        }
    }


    public void delete(ref List<string> Errormessages)
    {

    }

    public string displayName(clsLanguage Language)
    {
        return Description.text(Language);
    }

    public dynamic Insert(ref List<string> Errormessages)
    {
        return null;
    }

    public void update(ref List<string> Errormessages)
    {

        //UNFINISHED / TESTED

        object sql = null;
        sql = "UPDATE [promo] set code=" + da.SqlEncode(this.Code) + ",fk_translation_key_description=" + this.Description.Key + " WHERE id=" + System.Convert.ToString(this.Id);
        da.DBExecutesql(sql);


    }
}