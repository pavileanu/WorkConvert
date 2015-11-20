using System.Data.SqlClient;



/// <summary>
///
/// </summary>
/// <remarks></remarks>
public class clsAdvert : i_Editable
{
    public int ID { get; set; }
    public clsCampaign Campaign { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string URL { get; set; }
    public short Type { get; set; }
    public string BasketProductBelowAbsent { get; set; }
    public string BasketProductBelowPresent { get; set; }
    public clsProductType Present { get; set; }
    public clsProductType Absent { get; set; }
    public clsSlotType SlotType { get; set; }
    public int FillThresholdPercent { get; set; }
    public bool ImageWide { get; set; }
    public string SlotTypeCode { get; set; }
    public clsRegion AdRegionPresent { get; set; }
    public clsRegion AdRegionAbsent { get; set; }
    public bool Visible { get; set; }

    //New for HP Split
    public string mfrCode { get; set; }


    public Dictionary<int, clsImpression> Impressions { get; set; }
    public Dictionary<int, clsClickThru> ClickThrus { get; set; }

    private string conString; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    public clsAdvert()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);

        Impressions = new Dictionary<int, clsImpression>();
        ClickThrus = new Dictionary<int, clsClickThru>();

    }
    public clsAdvert(clsCampaign campaign, string name, string imageurl, string url, short type, string basketabsent, string basketpresent, clsProductType present, clsProductType absent, clsSlotType slotType, int fillthreshold, string mfrCode)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);



        SqlConnection con = new SqlConnection(conString);
        con.Open();
        SqlCommand command = new SqlCommand();
        command.CommandText = "AddAdvert";
        command.CommandType = CommandType.StoredProcedure;
        command.Connection = con;
        SqlParameter paramCampaignID = new SqlParameter("@campaignid", SqlDbType.Int);
        paramCampaignID.Value = Campaign.ID;
        SqlParameter paramName = new SqlParameter("@name", SqlDbType.VarChar, 100);
        paramName.Value = name;
        SqlParameter paramImageUrl = new SqlParameter("@imageurl", SqlDbType.VarChar, 255);
        paramImageUrl.Value = imageurl;
        SqlParameter paramUrl = new SqlParameter("@url", SqlDbType.VarChar, 255);
        paramUrl.Value = url;
        SqlParameter paramType = new SqlParameter("@type", SqlDbType.SmallInt);
        paramType.Value = type;
        SqlParameter paramBasketAbsent = new SqlParameter("@basket_absent", SqlDbType.NVarChar, 255);
        paramBasketAbsent.Value = basketabsent;
        SqlParameter paramBasketPresent = new SqlParameter("@basket_present", SqlDbType.NVarChar, 255);
        paramBasketPresent.Value = basketpresent;
        SqlParameter paramPresent = new SqlParameter("@prodtype_present", SqlDbType.Int);
        paramPresent.Value = Present.ID;
        SqlParameter paramAbsent = new SqlParameter("@prodtype_absent", SqlDbType.Int);
        paramAbsent.Value = Absent.ID;
        SqlParameter paraSlotType = new SqlParameter("@slottypeid", SqlDbType.Int);
        paraSlotType.Value = SlotType.ID;
        SqlParameter paramFillThreshold = new SqlParameter("@fillthresholdpercent", SqlDbType.Int);
        paramFillThreshold.Value = fillthreshold;
        SqlParameter paramMfrCode = new SqlParameter("@mfrCode", SqlDbType.NVarChar, 3);
        paramMfrCode.Value = mfrCode;

        SqlParameter paramReturn = new SqlParameter("@ret", SqlDbType.Int);
        paramReturn.Direction = ParameterDirection.ReturnValue;



        command.Parameters.Add(paramCampaignID);
        command.Parameters.Add(paramName);
        command.Parameters.Add(paramImageUrl);
        command.Parameters.Add(paramUrl);
        command.Parameters.Add(paramType);
        command.Parameters.Add(paramBasketAbsent);
        command.Parameters.Add(paramBasketPresent);
        command.Parameters.Add(paramPresent);
        command.Parameters.Add(paramAbsent);
        command.Parameters.Add(paraSlotType);
        command.Parameters.Add(paramFillThreshold);
        command.Parameters.Add(paramMfrCode);
        command.Parameters.Add(paramReturn);


        command.ExecuteNonQuery();

        con.Close();
        this.ID = System.Convert.ToInt32(Convert.ToInt32(paramReturn.Value));
        this.Name = name;
        this.Campaign = campaign;
        this.ImageUrl = imageurl;
        this.URL = url;
        this.Type = type;
        this.BasketProductBelowAbsent = basketabsent;
        this.BasketProductBelowPresent = basketpresent;
        this.Present = present;
        this.Absent = absent;
        this.SlotType = slotType;
        this.FillThresholdPercent = fillthreshold;
        this.mfrCode = mfrCode;

        this.Campaign.Adverts.Add(this.ID, this);

        Impressions = new Dictionary<int, clsImpression>();
        ClickThrus = new Dictionary<int, clsClickThru>();
        iq.Adverts.Add(this.ID, this);

    }
    public clsAdvert(int ID, clsCampaign campaign, string name, string imageurl, string url, short type, string basketabsent, string basketpresent, clsProductType present, clsProductType absent, clsSlotType slotType, int fillthreshold, bool imageWide, string slotTypeCode, clsRegion adRegionPresent, clsRegion adRegionAbsent, bool visible, string mfrCode)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);


        this.ID = ID;
        this.Campaign = campaign;
        this.Name = name;
        this.ImageUrl = imageurl;
        this.URL = url;
        this.Type = type;
        this.BasketProductBelowAbsent = basketabsent;
        this.BasketProductBelowPresent = basketpresent;
        this.Absent = absent;
        this.Present = present;
        this.SlotType = slotType;
        this.FillThresholdPercent = fillthreshold;
        this.ImageWide = imageWide;
        this.SlotTypeCode = slotTypeCode;
        this.AdRegionPresent = adRegionPresent;
        this.AdRegionAbsent = adRegionAbsent;
        this.Visible = visible;
        this.mfrCode = mfrCode;

        this.Campaign.Adverts.Add(this.ID, this);
        iq.Adverts.Add(this.ID, this);
        Impressions = new Dictionary<int, clsImpression>();
        ClickThrus = new Dictionary<int, clsClickThru>();

    }
    public void delete(ref List<string> errorMessages)
    {

        SqlConnection con = new SqlConnection(conString);
        con.Open();
        SqlCommand command = new SqlCommand();

        command.CommandText = "DeleteAdvert";
        command.CommandType = CommandType.StoredProcedure;

        SqlParameter paramID = new SqlParameter("@id", SqlDbType.Int);
        paramID.Value = this.ID;

        SqlParameter paramReturn = new SqlParameter("@return_value", SqlDbType.Int);
        paramReturn.Direction = ParameterDirection.ReturnValue;

        command.Parameters.Add(paramID);

        command.Parameters.Add(paramReturn);
        command.Connection = con;
        command.ExecuteNonQuery();

        foreach (clsClickThru clickthru in this.ClickThrus.Values.ToList())
        {
            clickthru.delete(errorMessages);
        }
        foreach (clsImpression impression in this.Impressions.Values.ToList())
        {
            impression.delete(errorMessages);
        }

        this.Campaign.Adverts.Remove(this.ID);

    }

    public string displayName(clsLanguage Language)
    {

    }

    public dynamic Insert(ref List<string> errorMessages)
    {
        return new clsAdvert(this.Campaign, this.Name, this.ImageUrl, this.URL, this.Type, this.BasketProductBelowAbsent, this.BasketProductBelowPresent, this.Present, this.Absent, this.SlotType, this.FillThresholdPercent, this.mfrCode);
    }

    public void update(ref List<string> errorMessages)
    {
        if (this.ID > 0)
        {
            SqlConnection con = new SqlConnection(conString);
            con.Open();
            SqlCommand command = new SqlCommand();
            command.CommandText = "UpdateAdvert";
            command.CommandType = CommandType.StoredProcedure;
            command.Connection = con;
            SqlParameter paramID = new SqlParameter("@ID", SqlDbType.Int);
            paramID.Value = this.ID;
            SqlParameter paramCampaignID = new SqlParameter("@campaignid", SqlDbType.Int);
            paramCampaignID.Value = this.Campaign.ID;
            SqlParameter paramName = new SqlParameter("@name", SqlDbType.VarChar, 100);
            paramName.Value = this.Name;
            SqlParameter paramImageUrl = new SqlParameter("@imageurl", SqlDbType.VarChar, 255);
            paramImageUrl.Value = this.ImageUrl;
            SqlParameter paramUrl = new SqlParameter("@url", SqlDbType.VarChar, 255);
            paramUrl.Value = this.URL;
            SqlParameter paramType = new SqlParameter("@type", SqlDbType.SmallInt);
            paramType.Value = this.Type;
            SqlParameter paramBasketAbsent = new SqlParameter("@basket_absent", SqlDbType.NVarChar, 255);
            paramBasketAbsent.Value = this.BasketProductBelowAbsent;
            SqlParameter paramBasketPresent = new SqlParameter("@basket_present", SqlDbType.NVarChar, 255);
            paramBasketPresent.Value = this.BasketProductBelowPresent;
            SqlParameter paramPresent = new SqlParameter("@prodtype_present", SqlDbType.Int);
            paramPresent.Value = this.Present.ID;
            SqlParameter paramAbsent = new SqlParameter("@prodtype_absent", SqlDbType.Int);
            paramAbsent.Value = this.Absent.ID;
            SqlParameter paraSlotType = new SqlParameter("@slottypeid", SqlDbType.Int);
            paraSlotType.Value = this.SlotType.ID;
            SqlParameter paramFillThreshold = new SqlParameter("@fillthresholdpercent", SqlDbType.Int);
            paramFillThreshold.Value = this.FillThresholdPercent;
            SqlParameter paramMfrCode = new SqlParameter("@mfrCode", SqlDbType.NVarChar, 3);
            paramMfrCode.Value = this.mfrCode;

            SqlParameter paramReturn = new SqlParameter("@ret", SqlDbType.Int);
            paramReturn.Direction = ParameterDirection.ReturnValue;

            command.Parameters.Add(paramID);
            command.Parameters.Add(paramCampaignID);
            command.Parameters.Add(paramName);
            command.Parameters.Add(paramImageUrl);
            command.Parameters.Add(paramUrl);
            command.Parameters.Add(paramType);
            command.Parameters.Add(paramBasketAbsent);
            command.Parameters.Add(paramBasketPresent);
            command.Parameters.Add(paramPresent);
            command.Parameters.Add(paramAbsent);
            command.Parameters.Add(paraSlotType);
            command.Parameters.Add(paramFillThreshold);
            command.Parameters.Add(paramMfrCode);
            command.Parameters.Add(paramReturn);

            command.Connection = con;

            command.ExecuteNonQuery();

            con.Close();

        }
    }

    public Manufacturer Manufacturer
    {

        get
        {
            Manufacturer returnValue = default(Manufacturer);

            returnValue = returnValue.Unknown;

            if (!string.IsNullOrEmpty(this.mfrCode))
            {
                if (string.Equals(this.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase))
                {
                    returnValue = returnValue.HPI;
                }
                else if (string.Equals(this.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase))
                {
                    returnValue = returnValue.HPE;
                }
            }

            return returnValue;
        }

    }

}