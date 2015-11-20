using dataAccess;


public class clsValidationInclusion : i_Editable
{
    public int ID { get; set; }
    public string MajorCode { get; set; }
    public string MinorCode { get; set; }
    public enumInclusionType InclusionType { get; set; }

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

        this.ID = System.Convert.ToInt32(da.DBExecutesql("INSERT INTO validationInclusion VALUES (" + da.SqlEncode(this.MajorCode) + "," + da.SqlEncode(this.MinorCode) + "," + da.SqlEncode(this.InclusionType.ToString()) + ")", true));

        iq.ValidationInclusions.Add(ID, this);
    }

    public string displayName(clsLanguage lang)
    {
        string returnValue = "";
        returnValue = string.Format("{0} - {1} - {2}", this.MajorCode, this.MinorCode, this.InclusionType.ToString());
        return returnValue;
    }

    public void delete(ref List<string> errorMessages)
    {
        iq.ValidationInclusions.Remove(this.ID);

        try
        {
            dataAccess.da.DBExecutesql("DELETE FROM validationinclusion where id=" + System.Convert.ToString(this.ID));
        }
        catch (Exception ex)
        {
            errorMessages.Add(ex.Message.ToString());
        }
    }

    public dynamic Insert(ref List<string> errorMessages)
    {
        return new clsValidationInclusion(MajorCode, MinorCode, InclusionType);
    }

    public void update(ref List<string> errorMessages)
    {

        if (this.ID < 0)
        {
            Debugger.Break();
        }

        object sql = null;
        sql = "update [ValidationInclusions] ";
        sql += "SET majorcode=" + da.SqlEncode(this.MajorCode) + ",minorcode=" + da.SqlEncode(this.MinorCode);
        sql += ",inclusiontype=" + da.SqlEncode(this.InclusionType.ToString());
        sql += " WHERE id=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql, false);

    }

}

public enum enumInclusionType
{
    Validated = 0,
    Unvalidated = 1
}