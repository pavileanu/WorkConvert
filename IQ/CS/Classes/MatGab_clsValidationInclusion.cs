using dataAccess;

public class clsValidationInclusion : i_Editable
{
	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public string MajorCode {
		get { return m_MajorCode; }
		set { m_MajorCode = Value; }
	}
	private string m_MajorCode;
	public string MinorCode {
		get { return m_MinorCode; }
		set { m_MinorCode = Value; }
	}
	private string m_MinorCode;
	public enumInclusionType InclusionType {
		get { return m_InclusionType; }
		set { m_InclusionType = Value; }
	}
	private enumInclusionType m_InclusionType;


	public clsValidationInclusion(int ID, string MajorCode, string MinorCode, enumInclusionType InclusionType)
	{
		this.MajorCode = MajorCode;
		this.MinorCode = MinorCode;
		this.InclusionType = InclusionType;
		this.ID = ID;
		iq.ValidationInclusions.Add(ID, this);

	}


	public clsValidationInclusion(string MajorCode, string MinorCode, enumInclusionType InclusionType)
	{
		this.MajorCode = MajorCode;
		this.MinorCode = MinorCode;
		this.InclusionType = InclusionType;

		this.ID = da.DBExecutesql("INSERT INTO validationInclusion VALUES (" + da.SqlEncode(this.MajorCode) + "," + da.SqlEncode(this.MinorCode) + "," + da.SqlEncode(this.InclusionType.ToString) + ")", true);

		iq.ValidationInclusions.Add(ID, this);
	}

	public string i_Editable.displayName(clsLanguage lang)
	{
		displayName = string.Format("{0} - {1} - {2}", this.MajorCode, this.MinorCode, this.InclusionType.ToString);
	}

	public void i_Editable.delete(ref List<string> errorMessages)
	{
		iq.ValidationInclusions.Remove(this.ID);

		try {
			dataAccess.da.DBExecutesql("DELETE FROM validationinclusion where id=" + this.ID);
		} catch (Exception ex) {
			errorMessages.Add(ex.Message.ToString);
		}
	}

	public object i_Editable.insert(ref List<string> errorMessages)
	{
		return new clsValidationInclusion(MajorCode, MinorCode, InclusionType);
	}


	public void i_Editable.update(ref List<string> errorMessages)
	{
		if (this.ID < 0)
			System.Diagnostics.Debugger.Break();

		object sql;
		sql = "update [ValidationInclusions] ";
		sql += "SET majorcode=" + da.SqlEncode(this.MajorCode) + ",minorcode=" + da.SqlEncode(this.MinorCode);
		sql += ",inclusiontype=" + da.SqlEncode(this.InclusionType.ToString);
		sql += " WHERE id=" + this.ID;

		da.DBExecutesql(sql, false);

	}

}

public enum enumInclusionType
{
	Validated = 0,
	Unvalidated = 1
}

