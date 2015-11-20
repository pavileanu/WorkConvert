using dataAccess;


public class clsSlotType
{

    //Branches have slots of one or more types (see clsbranch.slots)
    //quantities attach to 'child' branches to 'consume' slots of a given type in a give 'parent' branch
    //NB: 'parent' and 'child' in this discussion are not strictly parent or child.. a (typically) outer branch consumes the slots of some (typically) inner branch
    //- as specified by the quantity.TakesSlotsIn

    public int ID { get; set; }
    public string MajorCode { get; set; }
    public string MinorCode { get; set; }
    public clsTranslation Translation { get; set; } //this is what's displayed
    public clsTranslation TranslationShort { get; set; }
    public SortedDictionary<int, clsSlotType> Fallback { get; set; } //Which type of slot should(can) we use if this type is unavialble - eg a 4x PCI card would fall back to an 8x slot (seems backwards - but we want to occupy the least 'expensive' slots first)
    public bool EnforceMinorCode { get; set; }

    public string get_displayName(clsLanguage language)
    {
        return this.MajorCode + "/" + this.MinorCode;
    }


    public string get_shortDisplayName(clsLanguage language)
    {
        if (this.TranslationShort != null)
        {
            return this.TranslationShort.text(language);
        }
        else
        {
            return get_displayName(language);
        }
    }

    public clsSlotType(string MajorCode, string MinorCode)
    {
        //Must be a dummy
        this.MajorCode = MajorCode;
        this.MinorCode = MinorCode;
        ID = System.Convert.ToInt32(iq.SlotTypes.Min(g => g.Key) - 1);
        if (!iq.i_slotType_Code.ContainsKey(this.MajorCode))
        {
            iq.i_slotType_Code(this.MajorCode).Add(this.MajorCode.ToUpper(), this);
        }
        iq.i_slotType_Code(this.MajorCode).Add(this.MinorCode.ToUpper(), this);
        iq.SlotTypes.Add(ID, this);
    }


    public clsSlotType(int id, string MajorCode, string MinorCode, clsTranslation translation, clsTranslation translationShort, bool EnforceMinorCode)
    {

        this.ID = id;
        this.MajorCode = MajorCode.ToUpper();
        this.MinorCode = MinorCode.ToUpper();
        this.TranslationShort = translationShort;
        this.Translation = translation;
        this.Fallback = new SortedDictionary<int, clsSlotType>();
        this.EnforceMinorCode = EnforceMinorCode;

        iq.SlotTypes.Add(this.ID, this);

        if (!iq.i_slotType_Code.ContainsKey(this.MajorCode))
        {
            iq.i_slotType_Code.Add(this.MajorCode, new Dictionary<string, clsSlotType>(StringComparer.InvariantCultureIgnoreCase));
        }
        if (!iq.i_slotType_Code(this.MajorCode).ContainsKey(MinorCode))
        {
            iq.i_slotType_Code(this.MajorCode).Add(this.MinorCode, this);
        }
        else
        {
            Interaction.Beep();
        }



    }

    public clsSlotType(string majorCode, string minorCode, clsTranslation translation)
    {

        object sql = null;
        sql = "INSERT INTO SlotType(fk_translation_key,majorCode,MinorCode) VALUES (" + Translation.Key + "," + da.SqlEncode(majorCode) + "," + da.SqlEncode(minorCode) + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        this.MajorCode = majorCode.ToUpper();
        this.MinorCode = minorCode.ToUpper();

        this.Translation = translation;
        this.Fallback = new SortedDictionary<int, clsSlotType>();

        iq.SlotTypes.Add(this.ID, this);

        if (!iq.i_slotType_Code.ContainsKey(this.MajorCode))
        {
            iq.i_slotType_Code.Add(this.MajorCode, new Dictionary<string, clsSlotType>(StringComparer.InvariantCultureIgnoreCase));
        }
        if (!iq.i_slotType_Code(this.MajorCode).ContainsKey(minorCode))
        {
            iq.i_slotType_Code(this.MajorCode).Add(this.MinorCode, this);
        }
        else
        {
            Interaction.Beep();
        }

    }

    public clsSlotType Insert()
    {

        return new clsSlotType(this.MajorCode, this.MinorCode, this.Translation);

    }

    public void Update()
    {

        object sql = null;
        sql = "UPDATE slottype set majorcode=" + da.SqlEncode(this.MajorCode) + ",minorcode=" + da.SqlEncode(this.MinorCode) + ",fk_translation_key=" + this.Translation.Key + ",fk_translation_key_short=" + System.Convert.ToString(this.TranslationShort == null ? "null" : this.TranslationShort.Key);
        sql += " WHERE ID = " + System.Convert.ToString(this.ID);
        da.dbexecutesql(sql, false);

    }

    public bool Delete()
    {

        object sql = null;
        sql = "Delete from slottype where id=" + System.Convert.ToString(this.ID);

        try
        {
            //this may fail due to RI
            da.dbexecutesql(sql, false);

            iq.SlotTypes.Remove(this.ID);
            return true;

        }
        catch (Exception)
        {

            return false;

        }

    }

    public void AddFallback(int pos, clsSlotType st)
    {
        Fallback.Add(pos, st);
        string sql = "INSERT INTO altSlotType VALUES (" + System.Convert.ToString(this.ID) + "," + System.Convert.ToString(st.ID) + "," + System.Convert.ToString(pos) + ")";
        da.DBExecutesql(sql);
    }

} //clsSlotType