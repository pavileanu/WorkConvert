using dataAccess;
using System.Globalization;

public class clsCurrency : i_Editable
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
	private string Code_HP {
		get { return m_Code_HP; }
		set { m_Code_HP = Value; }
	}
	private string m_Code_HP;
	private clsTranslation translation {
		get { return m_translation; }
		set { m_translation = Value; }
	}
	private clsTranslation m_translation;
	//of the currency name (into other languages) - "Dollars" might be Somethign else in chinese/russian etc.
	private string Symbol {
		get { return m_Symbol; }
		set { m_Symbol = Value; }
	}
	private string m_Symbol;
	private float Rate {
		get { return m_Rate; }
		set { m_Rate = Value; }
	}
	private float m_Rate;
	private clsTranslation Notes {
		get { return m_Notes; }
		set { m_Notes = Value; }
	}
	private clsTranslation m_Notes;

	//Moved to clsAccount - Euro and Swiss Franc which may be used in multiple cultures - mean this culture should be per account - giving maximum flexibility (it's defaulted from the buyers region)
	//Property Culture As String '.NET culture code for decimal point, thousands seperator etc. (default is EN)

	public string i_Editable.DisplayName(clsLanguage language)
	{
		DisplayName = this.translation.text(language) + " (" + this.Code + ") " + this.Symbol;
	}



	public clsCurrency()
	{
	}

	public string format(decimal v, string culture, ref List<string> errorMessages, int decimalPlaces = 2)
	{

		format = "unable to format currency";
		CultureInfo ci = null;
		try {
			ci = new CultureInfo(culture);
			format = this.Symbol.Trim + v.ToString("N" + decimalPlaces.ToString.Trim, ci).Trim;
			//Format as a currency.. to the cirrenct number of decimal places
		} catch {
			errorMessages.Add("The culture code " + culture + " is probably wrong.");
		}

	}

	public object i_Editable.Insert(ref List<string> errorMessages)
	{

		return new clsCurrency(this.Code, this.Code_HP, this.translation, this.Symbol, this.Rate, this.Notes);

	}


	public void i_Editable.update(ref List<string> errorMessages)
	{
		object sql;
		//sql$ = "UPDATE [Currency] set code=" & da.SqlEncode(Me.Code) & ",symbol=" & da.SqlEncode(Me.Symbol) & ",rate=" & Me.Rate & ",fk_translation_key_notes=" & Me.translation.Key & ",culture=" & da.SqlEncode(Me.Culture) & " WHERE ID=" & Me.ID
		sql = "UPDATE [Currency] set code=" + da.SqlEncode(this.Code) + ",symbol=" + da.SqlEncode(this.Symbol) + ",rate=" + this.Rate + ",fk_translation_key_notes=" + this.translation.Key + " WHERE ID=" + this.ID;
		da.DBExecutesql(sql);

	}


	public void i_Editable.Delete(ref List<string> errorMessages)
	{
		object SQL;
		SQL = "DELETE FROM [CURRENCY] WHERE ID=" + this.ID;

		try {
			//there's a good chance this will fail (due to RI)
			da.DBExecutesql(SQL);
			iq.Currencies.Remove(this.ID);


		} catch (Exception ex) {
			errorMessages.Add(ex.Message.ToString);
		}

	}

	//, culture As String)
	public clsCurrency(string Code, string Code_HP, clsTranslation translation, string symbol, float rate, clsTranslation Notes)
	{

		object nk;
		if (Notes == null)
			nk = "null";
		else
			nk = Notes.Key;

		object sql;
		//sql$ = "INSERT INTO Currency (Code,Symbol,Rate,fk_translation_key_Name,fk_translation_key_notes,culture) VALUES ("
		sql = "INSERT INTO Currency (Code,Code_HP,Symbol,Rate,fk_translation_key_Name,fk_translation_key_notes) VALUES (";
		//sql$ &= SqlEncode(Code) & "," & da.SqlEncode(symbol) & "," & rate & "," & translation.Key & "," & nk & "," & da.SqlEncode(culture) & " );"
		sql += da.SqlEncode(Code) + "," + da.SqlEncode(Code_HP) + "," + da.SqlEncode(symbol) + "," + rate + "," + translation.Key + "," + nk + " );";

		this.ID = da.DBExecutesql(sql, true);
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

	//, culture As String)
	public clsCurrency(int ID, string Code, string Code_HP, clsTranslation translation, string symbol, float rate, clsTranslation notes)
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
