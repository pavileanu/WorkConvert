using System.Runtime.Serialization;
using dataAccess;


[DataContract]
public class clsScreenOverride
{
    [DataMember]
    public int AccountID;
    [DataMember]
    public int ScreenID;
    [DataMember]
    public string Path;
    [DataMember]
    public int FieldId;
    [DataMember]
    public Nullable<bool> ForceVisibilityTo;
    [DataMember]
    public Nullable<int> ForceOrderTo;
    [DataMember]
    public Nullable<double> ForceWidthTo;
    [DataMember]
    public string ForceSortTo;
    [DataMember]
    public string ForceFilterTo;
    [DataMember]
    public string FieldName;
    [DataMember]
    public clsUnit DisplayUnit;


    public clsScreenOverride(int AccountID, int ScreenID, string BranchPath, int FieldId, Nullable<bool> ForceVisibilityTo, Nullable<int> ForceOrderTo, Nullable<double> ForceWidthTo, string ForceSortTo, string ForceFilterTo, clsUnit DisplayUnit)
    {
        this.AccountID = AccountID;
        this.FieldId = FieldId;
        this.ForceVisibilityTo = ForceVisibilityTo;
        this.ForceOrderTo = ForceOrderTo;
        this.ScreenID = ScreenID;
        this.Path = BranchPath;
        this.ForceWidthTo = ForceWidthTo;
        this.ForceSortTo = ForceSortTo;
        this.ForceFilterTo = ForceFilterTo;
        this.DisplayUnit = DisplayUnit;
        iq.ScreenOverrides.Add(this);
    }

    public clsScreenOverride()
    {

    }

    public bool Update()
    {

        object sql = null;
        sql = "Update AccountScreenOverride set ForceVisibilityTo = " + System.Convert.ToString(this.ForceVisibilityTo == null ? "NULL" : (da.SqlEncode(this.ForceVisibilityTo))) + ",ForceOrderTo = " + System.Convert.ToString(this.ForceOrderTo == null ? "NULL" : (da.SqlEncode(this.ForceOrderTo))) + ",ForceWidthTo = " + System.Convert.ToString(this.ForceWidthTo == null ? "NULL" : (da.SqlEncode(this.ForceWidthTo))) + ", ForceSortTo = " + System.Convert.ToString(this.ForceSortTo == null ? "NULL" : (da.SqlEncode(this.ForceSortTo))) + ",ForceFilterTo=" + System.Convert.ToString(this.ForceFilterTo == null ? "NULL" : (da.SqlEncode(this.ForceFilterTo))) + " , FK_DisplayUnit_ID = " + System.Convert.ToString(this.DisplayUnit == null ? "NULL" : this.DisplayUnit.ID) + " where Path = " + da.SqlEncode(this.Path) + " AND [FK_Screen_Id]=" + System.Convert.ToString(this.ScreenID) + " and FK_Account_Id=" + System.Convert.ToString(this.AccountID) + " AND FK_Field_Id = " + System.Convert.ToString(this.FieldId);
        return da.DBExecutesql(sql, false) > 0;
    }

    public bool Insert()
    {
        object sql = null;
        sql = "INSERT INTO AccountScreenOverride (FK_Account_Id,FK_Screen_Id,Path,FK_Field_Id,ForceVisibilityTo,ForceOrderTo,ForceWidthTo,ForceSortTo,ForceFilterTo,FK_DisplayUnit_ID) VALUES (" + System.Convert.ToString(this.AccountID) + "," + System.Convert.ToString(this.ScreenID) + "," + System.Convert.ToString(this.Path == null ? "NULL" : (da.SqlEncode(this.Path))) + "," + System.Convert.ToString(this.FieldId) + "," + System.Convert.ToString(this.ForceVisibilityTo == null ? "NULL" : (da.SqlEncode(this.ForceVisibilityTo))) + "," + System.Convert.ToString(this.ForceOrderTo == null ? "NULL" : (da.SqlEncode(this.ForceOrderTo))) + "," + System.Convert.ToString(this.ForceWidthTo == null ? "NULL" : (da.SqlEncode(this.ForceWidthTo))) + "," + System.Convert.ToString(this.ForceSortTo == null ? "NULL" : (da.SqlEncode(this.ForceSortTo))) + "," + System.Convert.ToString(this.ForceFilterTo == null ? "NULL" : (da.SqlEncode(this.ForceFilterTo))) + "," + System.Convert.ToString(this.DisplayUnit == null ? "NULL" : this.DisplayUnit.ID) + ")";
        return da.DBExecutesql(sql, false) > 0;
    }



}