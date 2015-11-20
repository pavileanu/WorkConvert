

using System.Data.SqlClient;
/// <summary>
/// 
/// </summary>
/// <remarks></remarks>
public class clsCampaign : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string Name {
		get { return m_Name; }
		set { m_Name = Value; }
	}
	private string m_Name;
	private clsChannel Advertiser {
		get { return m_Advertiser; }
		set { m_Advertiser = Value; }
	}
	private clsChannel m_Advertiser;
	private clsRegion Region {
		get { return m_Region; }
		set { m_Region = Value; }
	}
	private clsRegion m_Region;
	private clsChannel Seller {
		get { return m_Seller; }
		set { m_Seller = Value; }
	}
	private clsChannel m_Seller;
	private clsChannel Buyer {
		get { return m_Buyer; }
		set { m_Buyer = Value; }
	}
	private clsChannel m_Buyer;
	private System.DateTime StartDate {
		get { return m_StartDate; }
		set { m_StartDate = Value; }
	}
	private System.DateTime m_StartDate;
	private System.DateTime EndDate {
		get { return m_EndDate; }
		set { m_EndDate = Value; }
	}
	private System.DateTime m_EndDate;

	private Dictionary<int, clsAdvert> Adverts {
		get { return m_Adverts; }
		set { m_Adverts = Value; }
	}
	private Dictionary<int, clsAdvert> m_Adverts;

	private string conString = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString;
	public clsCampaign()
	{
		this.Adverts = new Dictionary<int, clsAdvert>();

	}


	public clsCampaign(string name, clsChannel advertiser, clsRegion region, clsChannel seller, clsChannel buyer, System.DateTime startdate, System.DateTime enddate)
	{
		this.Adverts = new Dictionary<int, clsAdvert>();

		SqlConnection con = new SqlConnection(conString);
		con.Open();
		SqlCommand command = new SqlCommand();

		command.CommandText = "AddCampaign";
		command.CommandType = CommandType.StoredProcedure;

		SqlParameter paramName = new SqlParameter("@name", SqlDbType.VarChar, 100);
		paramName.Value = name;
		SqlParameter paramAdvertID = new SqlParameter("@advertiserid", SqlDbType.Int);
		paramAdvertID.Value = advertiser.ID;
		SqlParameter paramRegionID = new SqlParameter("@regionid", SqlDbType.Int);
		paramRegionID.Value = region.ID;
		SqlParameter paramSellerID = new SqlParameter("@sellerid", SqlDbType.Int);
		paramSellerID.Value = seller.ID;
		SqlParameter paramBuyerID = new SqlParameter("@buyerid", SqlDbType.Int);
		paramBuyerID.Value = buyer.ID;
		SqlParameter paramStartDate = new SqlParameter("@startdate", SqlDbType.DateTime);
		paramStartDate.Value = startdate;
		SqlParameter paramEndDate = new SqlParameter("@enddate", SqlDbType.DateTime);
		paramEndDate.Value = enddate;

		SqlParameter paramReturn = new SqlParameter("@return_value", SqlDbType.Int);
		paramReturn.Direction = ParameterDirection.ReturnValue;

		command.Parameters.Add(paramName);
		command.Parameters.Add(paramAdvertID);
		command.Parameters.Add(paramRegionID);
		command.Parameters.Add(paramSellerID);
		command.Parameters.Add(paramBuyerID);
		command.Parameters.Add(paramStartDate);
		command.Parameters.Add(paramEndDate);
		command.Parameters.Add(paramReturn);
		command.Connection = con;
		command.ExecuteNonQuery();


		this.ID = Convert.ToInt32(paramReturn.Value);
		this.Name = name;
		this.Advertiser = advertiser;
		this.Region = region;
		this.Seller = seller;
		this.Buyer = buyer;
		this.StartDate = startdate;
		this.EndDate = enddate;

		this.Advertiser.Campaigns.Add(this.ID, this);
		con.Close();


	}


	public clsCampaign(int ID, string name, clsChannel advertiser, clsRegion region, clsChannel seller, clsChannel buyer, System.DateTime startdate, System.DateTime enddate)
	{
		this.Adverts = new Dictionary<int, clsAdvert>();

		this.ID = ID;
		this.Name = name;
		this.Advertiser = advertiser;
		this.Region = region;
		this.Seller = seller;
		this.Buyer = buyer;
		this.StartDate = startdate;
		this.EndDate = enddate;

		this.Advertiser.Campaigns.Add(this.ID, this);
	}



	public void i_Editable.delete(ref List<string> errorMessages)
	{

		SqlConnection con = new SqlConnection(conString);
		con.Open();
		SqlCommand command = new SqlCommand();

		command.CommandText = "DeleteCampaign";
		command.CommandType = CommandType.StoredProcedure;

		SqlParameter paramID = new SqlParameter("@id", SqlDbType.Int);
		paramID.Value = this.ID;

		SqlParameter paramReturn = new SqlParameter("@return_value", SqlDbType.Int);
		paramReturn.Direction = ParameterDirection.ReturnValue;

		command.Parameters.Add(paramID);

		command.Parameters.Add(paramReturn);
		command.Connection = con;
		command.ExecuteNonQuery();
		foreach (clsAdvert adv in this.Adverts.Values.ToList()) {
			adv.delete(errorMessages);
		}
		this.Advertiser.Campaigns.Remove(this.ID);

	}

	public string i_Editable.displayName(clsLanguage Language)
	{

	}

	public object i_Editable.Insert(ref List<string> errorMessages)
	{

		return new clsCampaign(this.Name, this.Advertiser, this.Region, this.Seller, this.Buyer, this.StartDate, this.EndDate);

	}

	public void i_Editable.update(ref List<string> errorMessages)
	{
		if (this.ID > 0) {
			SqlConnection con = new SqlConnection(conString);
			con.Open();
			SqlCommand command = new SqlCommand();

			command.CommandText = "UpdateCampaign";
			command.CommandType = CommandType.StoredProcedure;

			SqlParameter paramID = new SqlParameter("@ID", SqlDbType.Int);
			paramID.Value = this.ID;
			SqlParameter paramName = new SqlParameter("@name", SqlDbType.VarChar, 100);
			paramName.Value = this.Name;
			SqlParameter paramAdvertID = new SqlParameter("@advertiserid", SqlDbType.Int);
			paramAdvertID.Value = this.Advertiser.ID;
			SqlParameter paramRegionID = new SqlParameter("@regionid", SqlDbType.Int);
			paramRegionID.Value = this.Region.ID;
			SqlParameter paramSellerID = new SqlParameter("@sellerid", SqlDbType.Int);
			paramSellerID.Value = this.Seller.ID;
			SqlParameter paramBuyerID = new SqlParameter("@buyerid", SqlDbType.Int);
			paramBuyerID.Value = this.Buyer.ID;
			SqlParameter paramStartDate = new SqlParameter("@startdate", SqlDbType.DateTime);
			paramStartDate.Value = this.StartDate;
			SqlParameter paramEndDate = new SqlParameter("@enddate", SqlDbType.DateTime);
			paramEndDate.Value = this.EndDate;

			SqlParameter paramReturn = new SqlParameter("@return_value", SqlDbType.Int);
			paramReturn.Direction = ParameterDirection.ReturnValue;

			command.Parameters.Add(paramID);
			command.Parameters.Add(paramName);
			command.Parameters.Add(paramAdvertID);
			command.Parameters.Add(paramRegionID);
			command.Parameters.Add(paramSellerID);
			command.Parameters.Add(paramBuyerID);
			command.Parameters.Add(paramStartDate);
			command.Parameters.Add(paramEndDate);
			command.Parameters.Add(paramReturn);
			command.Connection = con;
			command.ExecuteNonQuery();


			con.Close();


		}
	}
}
