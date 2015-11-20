using dataAccess;

public class clsSlotType
{

	//Branches have slots of one or more types (see clsbranch.slots)
	//quantities attach to 'child' branches to 'consume' slots of a given type in a give 'parent' branch
	//NB: 'parent' and 'child' in this discussion are not strictly parent or child.. a (typically) outer branch consumes the slots of some (typically) inner branch 
	//- as specified by the quantity.TakesSlotsIn

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string MajorCode {
		get { return m_MajorCode; }
		set { m_MajorCode = Value; }
	}
	private string m_MajorCode;
	private string MinorCode {
		get { return m_MinorCode; }
		set { m_MinorCode = Value; }
	}
	private string m_MinorCode;
	private clsTranslation Translation {
		get { return m_Translation; }
		set { m_Translation = Value; }
	}
	private clsTranslation m_Translation;
	//this is what's displayed
	private clsTranslation TranslationShort {
		get { return m_TranslationShort; }
		set { m_TranslationShort = Value; }
	}
	private clsTranslation m_TranslationShort;
	private SortedDictionary<int, clsSlotType> Fallback {
		get { return m_Fallback; }
		set { m_Fallback = Value; }
	}
	private SortedDictionary<int, clsSlotType> m_Fallback;
	//Which type of slot should(can) we use if this type is unavialble - eg a 4x PCI card would fall back to an 8x slot (seems backwards - but we want to occupy the least 'expensive' slots first)
	private bool EnforceMinorCode {
		get { return m_EnforceMinorCode; }
		set { m_EnforceMinorCode = Value; }
	}
	private bool m_EnforceMinorCode;

	public string displayName {
		get { return this.MajorCode + "/" + this.MinorCode; }
	}


	public string shortDisplayName {
		get {
			if (this.TranslationShort != null)
				return this.TranslationShort.text(language);
			else
				return displayName(language);
		}
	}

	public clsSlotType(string MajorCode, string MinorCode)
	{
		//Must be a dummy
		this.MajorCode = MajorCode;
		this.MinorCode = MinorCode;
		ID = iq.SlotTypes.Min(g => g.Key) - 1;
		if (!iq.i_slotType_Code.ContainsKey(this.MajorCode))
			iq.i_slotType_Code(this.MajorCode).Add(this.MajorCode.ToUpper, this);
		iq.i_slotType_Code(this.MajorCode).Add(this.MinorCode.ToUpper, this);
		iq.SlotTypes.Add(ID, this);
	}



	public clsSlotType(int id, string MajorCode, string MinorCode, clsTranslation translation, clsTranslation translationShort, bool EnforceMinorCode)
	{
		this.ID = id;
		this.MajorCode = MajorCode.ToUpper;
		this.MinorCode = MinorCode.ToUpper;
		this.TranslationShort = translationShort;
		this.Translation = translation;
		this.Fallback = new SortedDictionary<int, clsSlotType>();
		this.EnforceMinorCode = EnforceMinorCode;

		iq.SlotTypes.Add(this.ID, this);

		if (!iq.i_slotType_Code.ContainsKey(this.MajorCode)) {
			iq.i_slotType_Code.Add(this.MajorCode, new Dictionary<string, clsSlotType>(StringComparer.InvariantCultureIgnoreCase));
		}
		if (!iq.i_slotType_Code(this.MajorCode).ContainsKey(MinorCode)) {
			iq.i_slotType_Code(this.MajorCode).Add(this.MinorCode, this);
		} else {
			Beep();
		}



	}


	public clsSlotType(string majorCode, string minorCode, clsTranslation translation)
	{
		object sql;
		sql = "INSERT INTO SlotType(fk_translation_key,majorCode,MinorCode) VALUES (" + translation.Key + "," + da.SqlEncode(majorCode) + "," + da.SqlEncode(minorCode) + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.MajorCode = majorCode.ToUpper;
		this.MinorCode = minorCode.ToUpper;

		this.Translation = translation;
		this.Fallback = new SortedDictionary<int, clsSlotType>();

		iq.SlotTypes.Add(this.ID, this);

		if (!iq.i_slotType_Code.ContainsKey(this.MajorCode)) {
			iq.i_slotType_Code.Add(this.MajorCode, new Dictionary<string, clsSlotType>(StringComparer.InvariantCultureIgnoreCase));
		}
		if (!iq.i_slotType_Code(this.MajorCode).ContainsKey(minorCode)) {
			iq.i_slotType_Code(this.MajorCode).Add(this.MinorCode, this);
		} else {
			Beep();
		}

	}

	public clsSlotType Insert()
	{

		return new clsSlotType(this.MajorCode, this.MinorCode, this.Translation);

	}


	public void Update()
	{
		object sql;
		sql = "UPDATE slottype set majorcode=" + da.SqlEncode(this.MajorCode) + ",minorcode=" + da.SqlEncode(this.MinorCode) + ",fk_translation_key=" + this.Translation.Key + ",fk_translation_key_short=" + this.TranslationShort == null ? "null" : this.TranslationShort.Key;
		sql += " WHERE ID = " + this.ID;
		da.dbexecutesql(sql, false);

	}

	public bool Delete()
	{

		object sql;
		sql = "Delete from slottype where id=" + this.ID;

		try {
			//this may fail due to RI
			da.dbexecutesql(sql, false);

			iq.SlotTypes.Remove(this.ID);
			return true;


		} catch (Exception ex) {
			return false;

		}

	}

	private void AddFallback(Int32 pos, clsSlotType st)
	{
		Fallback.Add(pos, st);
		object sql = "INSERT INTO altSlotType VALUES (" + this.ID + "," + st.ID + "," + pos + ")";
		da.DBExecutesql(sql);
	}

}
//clsSlotType
