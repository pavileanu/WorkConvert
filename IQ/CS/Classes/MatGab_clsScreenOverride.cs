using System.Runtime.Serialization;
using dataAccess;

[DataContract()]
public class clsScreenOverride
{
	[DataMember()]
	public int AccountID;
	[DataMember()]
	public int ScreenID;
	[DataMember()]
	public string Path;
	[DataMember()]
	public int FieldId;
	[DataMember()]
	public Nullable<bool> ForceVisibilityTo;
	[DataMember()]
	public Nullable<int> ForceOrderTo;
	[DataMember()]
	public Nullable<double> ForceWidthTo;
	[DataMember()]
	public string ForceSortTo;
	[DataMember()]
	public string ForceFilterTo;
	[DataMember()]
	public string FieldName;
	[DataMember()]

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

		object sql;
		sql = "Update AccountScreenOverride set ForceVisibilityTo = " + this.ForceVisibilityTo == null ? "NULL" : da.SqlEncode(this.ForceVisibilityTo) + ",ForceOrderTo = " + this.ForceOrderTo == null ? "NULL" : da.SqlEncode(this.ForceOrderTo) + ",ForceWidthTo = " + this.ForceWidthTo == null ? "NULL" : da.SqlEncode(this.ForceWidthTo) + ", ForceSortTo = " + this.ForceSortTo == null ? "NULL" : da.SqlEncode(this.ForceSortTo) + ",ForceFilterTo=" + this.ForceFilterTo == null ? "NULL" : da.SqlEncode(this.ForceFilterTo) + " , FK_DisplayUnit_ID = " + this.DisplayUnit == null ? "NULL" : this.DisplayUnit.ID + " where Path = " + da.SqlEncode(this.Path) + " AND [FK_Screen_Id]=" + this.ScreenID + " and FK_Account_Id=" + this.AccountID + " AND FK_Field_Id = " + this.FieldId;
		return da.DBExecutesql(sql, false) > 0;
	}

	public bool Insert()
	{
		object sql;
		sql = "INSERT INTO AccountScreenOverride (FK_Account_Id,FK_Screen_Id,Path,FK_Field_Id,ForceVisibilityTo,ForceOrderTo,ForceWidthTo,ForceSortTo,ForceFilterTo,FK_DisplayUnit_ID) VALUES (" + this.AccountID + "," + this.ScreenID + "," + this.Path == null ? "NULL" : da.SqlEncode(this.Path) + "," + this.FieldId + "," + this.ForceVisibilityTo == null ? "NULL" : da.SqlEncode(this.ForceVisibilityTo) + "," + this.ForceOrderTo == null ? "NULL" : da.SqlEncode(this.ForceOrderTo) + "," + this.ForceWidthTo == null ? "NULL" : da.SqlEncode(this.ForceWidthTo) + "," + this.ForceSortTo == null ? "NULL" : da.SqlEncode(this.ForceSortTo) + "," + this.ForceFilterTo == null ? "NULL" : da.SqlEncode(this.ForceFilterTo) + "," + this.DisplayUnit == null ? "NULL" : this.DisplayUnit.ID + ")";
		return da.DBExecutesql(sql, false) > 0;
	}



}
