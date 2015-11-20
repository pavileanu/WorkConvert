using dataAccess;
using System.IO;



// Represents an HPE Care Pack Top Recommended Option/Auto Add

public class clsTROAA : i_Editable
{

    public int ID;
    public string SysFamily;
    public int SlotTypeCode;
    public int ServiceLevelID;
    public int DisplayOrder;
    public clsServiceLevel ServiceLevel;

    private const string TABLE = "TROAA";

    public clsTROAA()
    {

    }

    public clsTROAA(int id, string sysFamily, int slotTypeCode, int serviceLevelID, int displayOrder, clsServiceLevel serviceLevel)
    {

        this.ID = id;
        this.SysFamily = sysFamily;
        this.SlotTypeCode = slotTypeCode;
        this.ServiceLevelID = serviceLevelID;
        this.DisplayOrder = displayOrder;
        this.ServiceLevel = serviceLevel;

    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsTROAA(this.ID, this.SysFamily, this.SlotTypeCode, this.ServiceLevelID, this.DisplayOrder, this.ServiceLevel);

    }

    public void delete(ref List<string> errorMessages)
    {

        System.Char sql = string.Format("delete from {0} where id={1}", TABLE, this.ID);

        da.DBExecutesql(sql);

    }

    public void update(ref List<string> errorMessages)
    {

        System.Char sql = string.Format("update {0} set SysFamily=\'{1}\', SlotTypeCode={2}, ServiceLevel={3}, DisplayOrder={4}, FK_ServiceLevelMap_ID={5} where ID={6}", TABLE, this.SysFamily, this.SlotTypeCode, this.ServiceLevelID, this.DisplayOrder, this.ServiceLevel.ID);

        da.DBExecutesql(sql, false);

    }

    public string displayName(clsLanguage language)
    {

        return string.Format("{0}-{1}-{2}", this.SysFamily, this.SlotTypeCode, this.ServiceLevelID);

    }

}