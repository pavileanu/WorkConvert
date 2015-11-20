using System.Data.SqlClient;



/// <summary>
///
/// </summary>
/// <remarks></remarks>
public class clsCampaign : i_Editable
{

    public int ID { get; set; }
    public string Name { get; set; }
    public clsChannel Advertiser { get; set; }
    public clsRegion Region { get; set; }
    public clsChannel Seller { get; set; }
    public clsChannel Buyer { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Dictionary<int, clsAdvert> Adverts { get; set; }
    private string conString; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    public clsCampaign()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);

        this.Adverts = new Dictionary<int, clsAdvert>();

    }

    public clsCampaign(string name, clsChannel advertiser, clsRegion region, clsChannel seller, clsChannel buyer, DateTime startdate, DateTime enddate)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);


        this.Adverts = new Dictionary<int, clsAdvert>();

        SqlConnection con = new SqlConnection(conString);
        con.Open();
        SqlCommand command = new SqlCommand();

        command.CommandText = "AddCampaign";
        command.CommandType = CommandType.StoredProcedure;

        SqlParameter paramName = new SqlParameter("@name", SqlDbType.VarChar, 100);
        paramName.Value = name;
        SqlParameter paramAdvertID = new SqlParameter("@advertiserid", SqlDbType.Int);
        paramAdvertID.Value = Advertiser.ID;
        SqlParameter paramRegionID = new SqlParameter("@regionid", SqlDbType.Int);
        paramRegionID.Value = Region.ID;
        SqlParameter paramSellerID = new SqlParameter("@sellerid", SqlDbType.Int);
        paramSellerID.Value = Seller.ID;
        SqlParameter paramBuyerID = new SqlParameter("@buyerid", SqlDbType.Int);
        paramBuyerID.Value = Buyer.ID;
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


        this.ID = System.Convert.ToInt32(Convert.ToInt32(paramReturn.Value));
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

    public clsCampaign(int ID, string name, clsChannel advertiser, clsRegion region, clsChannel seller, clsChannel buyer, DateTime startdate, DateTime enddate)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);


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


    public void delete(ref List<string> errorMessages)
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
        foreach (clsAdvert adv in this.Adverts.Values.ToList())
        {
            adv.delete(errorMessages);
        }
        this.Advertiser.Campaigns.Remove(this.ID);

    }

    public string displayName(clsLanguage Language)
    {

    }

    public dynamic Insert(ref List<string> errorMessages)
    {

        return new clsCampaign(this.Name, this.Advertiser, this.Region, this.Seller, this.Buyer, this.StartDate, this.EndDate);

    }

    public void update(ref List<string> errorMessages)
    {
        if (this.ID > 0)
        {
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