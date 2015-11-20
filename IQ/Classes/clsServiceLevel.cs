using dataAccess;
using System.IO;



// Represents an HP Service Pack service level

public class clsServiceLevel : i_Editable
{

    public int ID;
    public string MfrCode;
    public int ServiceLevel;
    public string ServiceLevelGroup;
    public string SuperGroup;
    public clsTranslation Description;
    public int Duration;
    public bool PostWarranty;
    public bool Disabled;
    public clsServiceType ServiceType;
    public clsResponse Response;
    public bool HpeDmr;
    public bool HpeCdmr;
    public bool HpiAdp;
    public bool HpiDmr;
    public bool HpiTravel;
    public bool HpiTracing;
    public bool HpiTheft;

    private const string TABLE = "ServiceLevelMap";

    public clsServiceLevel()
    {

    }

    public clsServiceLevel(int id, string mfrCode, int serviceLevel, string serviceLevelGroup, string superGroup, clsTranslation description, int duration, bool postWarranty, bool disabled, clsServiceType serviceType, clsResponse response, bool hpeDmr, bool hpeCdmr, bool hpiAdp, bool hpiDmr, bool hpiTravel, bool hpiTracing, bool hpiTheft)
    {

        this.ID = id;
        this.MfrCode = mfrCode;
        this.ServiceLevel = serviceLevel;
        this.ServiceLevelGroup = serviceLevelGroup;
        this.SuperGroup = superGroup;
        this.Description = description;
        this.Duration = duration;
        this.PostWarranty = postWarranty;
        this.Disabled = disabled;
        this.ServiceType = serviceType;
        this.Response = response;
        this.HpeDmr = hpeDmr;
        this.HpeCdmr = hpeCdmr;
        this.HpiAdp = hpiAdp;
        this.HpiDmr = hpiDmr;
        this.HpiTravel = hpiTravel;
        this.HpiTracing = hpiTracing;
        this.HpiTheft = hpiTheft;

    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsServiceLevel(this.ID, this.MfrCode, this.ServiceLevel, this.ServiceLevelGroup, this.SuperGroup, this.Description, this.Duration, this.PostWarranty, this.Disabled, this.ServiceType, this.Response, this.HpeDmr, this.HpeCdmr, this.HpiAdp, this.HpiDmr, this.HpiTravel, this.HpiTracing, this.HpiTheft);

    }

    public void delete(ref List<string> errorMessages)
    {

        System.Char sql = string.Format("delete from {0} where id={1}", TABLE, this.ID);

        da.DBExecutesql(sql);

    }

    public void update(ref List<string> errorMessages)
    {

        System.Char sql = string.Format("update {0} set mfrCode=\'{1}\', ServiceLevel={2}, ServiceLevelGroup=\'{3}\', SuperGroup=\'{4}\', Duration={5}, PostWarranty={6}, Disabled={7}, ServiceType={8}, Response={9}, DMR={10}, HpeDmr={11}, HpeCdmr={12}, HpiAdp={13}, HpiDmr={14}, HpiTravel={15}, HpiTracining={16}, HpiTheft={17) where ID={18}",
            TABLE, this.MfrCode, this.ServiceLevel, this.ServiceLevelGroup, this.SuperGroup, this.Duration, this.PostWarranty, this.Disabled, this.ServiceType.ID, this.Response.ID, this.HpiAdp, this.HpeDmr, this.HpeCdmr, this.HpiDmr, this.HpiTravel, this.HpiTracing, this.HpiTheft, this.ID);

        da.DBExecutesql(sql, false);

    }

    public string displayName(clsLanguage language)
    {

        return this.Description.text(language);

    }

    public Manufacturer Manufacturer
    {

        get
        {
            Manufacturer returnValue = default(Manufacturer);

            returnValue = returnValue.Unknown;

            if (!string.IsNullOrEmpty(this.MfrCode))
            {
                if (string.Equals(this.MfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase))
                {
                    returnValue = returnValue.HPI;
                }
                else if (string.Equals(this.MfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase))
                {
                    returnValue = returnValue.HPE;
                }
            }

            return returnValue;
        }

    }

}