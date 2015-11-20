
using System.Data.SqlClient;

/// <summary>
/// 
/// </summary>
/// <remarks></remarks>
public class clsAdvert : i_Editable
{
	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsCampaign Campaign {
		get { return m_Campaign; }
		set { m_Campaign = Value; }
	}
	private clsCampaign m_Campaign;
	private string Name {
		get { return m_Name; }
		set { m_Name = Value; }
	}
	private string m_Name;
	private string ImageUrl {
		get { return m_ImageUrl; }
		set { m_ImageUrl = Value; }
	}
	private string m_ImageUrl;
	private string URL {
		get { return m_URL; }
		set { m_URL = Value; }
	}
	private string m_URL;
	private Int16 Type {
		get { return m_Type; }
		set { m_Type = Value; }
	}
	private Int16 m_Type;
	private string BasketProductBelowAbsent {
		get { return m_BasketProductBelowAbsent; }
		set { m_BasketProductBelowAbsent = Value; }
	}
	private string m_BasketProductBelowAbsent;
	private string BasketProductBelowPresent {
		get { return m_BasketProductBelowPresent; }
		set { m_BasketProductBelowPresent = Value; }
	}
	private string m_BasketProductBelowPresent;
	private clsProductType Present {
		get { return m_Present; }
		set { m_Present = Value; }
	}
	private clsProductType m_Present;
	private clsProductType Absent {
		get { return m_Absent; }
		set { m_Absent = Value; }
	}
	private clsProductType m_Absent;
	private clsSlotType SlotType {
		get { return m_SlotType; }
		set { m_SlotType = Value; }
	}
	private clsSlotType m_SlotType;
	private int FillThresholdPercent {
		get { return m_FillThresholdPercent; }
		set { m_FillThresholdPercent = Value; }
	}
	private int m_FillThresholdPercent;
	private bool ImageWide {
		get { return m_ImageWide; }
		set { m_ImageWide = Value; }
	}
	private bool m_ImageWide;
	private string SlotTypeCode {
		get { return m_SlotTypeCode; }
		set { m_SlotTypeCode = Value; }
	}
	private string m_SlotTypeCode;
	private clsRegion AdRegionPresent {
		get { return m_AdRegionPresent; }
		set { m_AdRegionPresent = Value; }
	}
	private clsRegion m_AdRegionPresent;
	private clsRegion AdRegionAbsent {
		get { return m_AdRegionAbsent; }
		set { m_AdRegionAbsent = Value; }
	}
	private clsRegion m_AdRegionAbsent;
	private bool Visible {
		get { return m_Visible; }
		set { m_Visible = Value; }
	}
	private bool m_Visible;

	//New for HP Split
	private string mfrCode {
		get { return m_mfrCode; }
		set { m_mfrCode = Value; }
	}
	private string m_mfrCode;


	private Dictionary<int, clsImpression> Impressions {
		get { return m_Impressions; }
		set { m_Impressions = Value; }
	}
	private Dictionary<int, clsImpression> m_Impressions;
	private Dictionary<int, clsClickThru> ClickThrus {
		get { return m_ClickThrus; }
		set { m_ClickThrus = Value; }
	}
	private Dictionary<int, clsClickThru> m_ClickThrus;


	private string conString = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString;
	public clsAdvert()
	{
		Impressions = new Dictionary<int, clsImpression>();
		ClickThrus = new Dictionary<int, clsClickThru>();

	}
	public clsAdvert(clsCampaign campaign, string name, string imageurl, string url, Int16 type, string basketabsent, string basketpresent, clsProductType present, clsProductType absent, clsSlotType slotType,

	int fillthreshold, string mfrCode)
	{

		SqlConnection con = new SqlConnection(conString);
		con.Open();
		SqlCommand command = new SqlCommand();
		command.CommandText = "AddAdvert";
		command.CommandType = CommandType.StoredProcedure;
		command.Connection = con;
		SqlParameter paramCampaignID = new SqlParameter("@campaignid", SqlDbType.Int);
		paramCampaignID.Value = campaign.ID;
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
		paramPresent.Value = present.ID;
		SqlParameter paramAbsent = new SqlParameter("@prodtype_absent", SqlDbType.Int);
		paramAbsent.Value = absent.ID;
		SqlParameter paraSlotType = new SqlParameter("@slottypeid", SqlDbType.Int);
		paraSlotType.Value = slotType.ID;
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
		this.ID = Convert.ToInt32(paramReturn.Value);
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
	public clsAdvert(int ID, clsCampaign campaign, string name, string imageurl, string url, Int16 type, string basketabsent, string basketpresent, clsProductType present, clsProductType absent,

	clsSlotType slotType, int fillthreshold, bool imageWide, string slotTypeCode, clsRegion adRegionPresent, clsRegion adRegionAbsent, bool visible, string mfrCode)
	{
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

	public void i_Editable.delete(ref List<string> errorMessages)
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

		foreach (clsClickThru clickthru in this.ClickThrus.Values.ToList()) {
			clickthru.delete(errorMessages);
		}
		foreach (clsImpression impression in this.Impressions.Values.ToList()) {
			impression.delete(errorMessages);
		}

		this.Campaign.Adverts.Remove(this.ID);

	}

	public string i_Editable.displayName(clsLanguage Language)
	{

	}

	public object i_Editable.Insert(ref List<string> errorMessages)
	{
		return new clsAdvert(this.Campaign, this.Name, this.ImageUrl, this.URL, this.Type, this.BasketProductBelowAbsent, this.BasketProductBelowPresent, this.Present, this.Absent, this.SlotType,
		this.FillThresholdPercent, this.mfrCode);
	}

	public void i_Editable.update(ref List<string> errorMessages)
	{
		if (this.ID > 0) {
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

	public Manufacturer Manufacturer {


		get {
			Manufacturer = Manufacturer.Unknown;

			if (!string.IsNullOrEmpty(this.mfrCode)) {
				if (string.Equals(this.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase)) {
					Manufacturer = Manufacturer.HPI;
				} else if (string.Equals(this.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase)) {
					Manufacturer = Manufacturer.HPE;
				}
			}

		}
	}


}
