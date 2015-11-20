using dataAccess;

public class clsUnit : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsTranslation Translation {
		get { return m_Translation; }
		set { m_Translation = Value; }
	}
	private clsTranslation m_Translation;
	//carries the translation.key - and exposes (via an indexed defautl property) the underlying text
	private string Symbol {
		get { return m_Symbol; }
		set { m_Symbol = Value; }
	}
	private string m_Symbol;
	private string Code {
		get { return m_Code; }
		set { m_Code = Value; }
	}
	private string m_Code;
	//our internal code for referencing these units eg KG (most of the time it will be the same as the name)
	private int MeasureID {
		get { return m_MeasureID; }
		set { m_MeasureID = Value; }
	}
	private int m_MeasureID;


	string oCode;
	public string i_Editable.DisplayName(clsLanguage language)
	{

		return this.Translation.text(language);
	}


	public clsUnit()
	{

	}


	public void i_Editable.delete(ref List<string> errormessages)
	{


		try {
			da.DBExecutesql("DELETE FROM UNIT WHERE ID=" + this.ID);
			//will often fail due to RI (expose this error through the editor)
		} catch (Exception ex) {
			errormessages.Add(ex.Message.ToString);
		}

	}


	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsUnit(this.Code, this.Translation, this.Symbol, this.MeasureID);

	}


	public void i_Editable.Update(ref List<string> errormessages)
	{
		object sql;
		sql = "UPDATE [Unit] set ";
		sql += "code=" + da.SqlEncode(this.Code) + ",";
		sql += "fk_translation_key_name=" + this.Translation.Key + ",";
		sql += "symbol=" + da.SqlEncode(this.Symbol);
		sql += " WHERE ID=" + this.ID;

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

		object sql;
		sql = "Insert into [Unit] ([code],[FK_Translation_key_name],[symbol],FK_Measure_ID) values (" + da.SqlEncode(code) + "," + translation.Key + "," + da.SqlEncode(this.Symbol) + "," + da.SqlEncode(this.MeasureID) + ");";
		this.ID = da.DBExecutesql(sql, true);

		iq.Units.Add(this.ID, this);
		//hmm not sure why this is needed 
		if (!iq.i_unit_code.ContainsKey(code)) {
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
