using System.Data.SqlClient;



/// <summary>
///
/// </summary>
/// <remarks></remarks>

public class clsImpression : i_Editable
{
    public int ID { get; set; }
    public clsAdvert Advert { get; set; }
    public clsAccount Account { get; set; }
    public int Count { get; set; }
    public DateTime IDate { get; set; }
    private string conString; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    public clsImpression()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);


    }
    public clsImpression(clsAccount account, clsAdvert advert, DateTime timestamp)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);


        if (advert != null)
        {

            SqlConnection con = new SqlConnection(conString);
            con.Open();
            SqlCommand command = new SqlCommand();
            command.CommandText = "Addimpression";
            command.CommandType = CommandType.StoredProcedure;
            command.Connection = con;
            SqlParameter paramAdvertID = new SqlParameter("@advertid", SqlDbType.Int);
            paramAdvertID.Value = Advert.ID;
            SqlParameter paramAccountID = new SqlParameter("@accountid", SqlDbType.Int);
            paramAccountID.Value = Account.ID;
            SqlParameter paramTimeStamp = new SqlParameter("@impDate", SqlDbType.DateTime);
            paramTimeStamp.Value = timestamp;

            SqlParameter paramReturn = new SqlParameter("@ret", SqlDbType.Int);
            paramReturn.Direction = ParameterDirection.ReturnValue;

            command.Parameters.Add(paramAccountID);
            command.Parameters.Add(paramAdvertID);
            command.Parameters.Add(paramTimeStamp);
            command.Parameters.Add(paramReturn);

            command.ExecuteNonQuery();

            con.Close();

            this.ID = System.Convert.ToInt32(Convert.ToInt32(paramReturn.Value));
            this.Account = account;
            this.Advert = advert;
            this.IDate = timestamp;
            this.Count = 1;
            this.Advert.Impressions.Add(this.ID, this);
            this.Account.Impressions.Add(this.ID, this);

        }
    }

    public clsImpression(int ID, clsAccount account, clsAdvert advert, DateTime timestamp)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);

        this.ID = ID;
        this.Account = account;
        this.Advert = advert;
        this.IDate = timestamp;
        this.Count = 1;
        this.Advert.Impressions.Add(this.ID, this);
        this.Account.Impressions.Add(this.ID, this);

    }

    public void delete(ref List<string> errorMessages)
    {

        //this doesnt appear to remove from the db ?/
        this.Advert.Impressions.Remove(this.ID);
        this.Account.Impressions.Remove(this.ID);

    }

    public string displayName(clsLanguage Language)
    {

    }

    public dynamic Insert(ref List<string> errorMessages)
    {
        return new clsImpression(this.Account, this.Advert, this.IDate);
    }

    public void update(ref List<string> errorMessages)
    {
        if (this.ID > 0)
        {
            SqlConnection con = new SqlConnection(conString);
            con.Open();
            SqlCommand command = new SqlCommand();
            command.CommandText = "UpdateImpression";
            command.CommandType = CommandType.StoredProcedure;
            command.Connection = con;
            SqlParameter paramID = new SqlParameter("@ID", SqlDbType.Int);
            paramID.Value = this.ID;
            SqlParameter paramAdvertID = new SqlParameter("@advertid", SqlDbType.Int);
            paramAdvertID.Value = this.Advert.ID;
            SqlParameter paramAccountID = new SqlParameter("@accountid", SqlDbType.Int);
            paramAccountID.Value = this.Account.ID;
            SqlParameter paramTimeStamp = new SqlParameter("@impDate", SqlDbType.DateTime);
            paramTimeStamp.Value = this.IDate;

            SqlParameter paramReturn = new SqlParameter("@ret", SqlDbType.Int);
            paramReturn.Direction = ParameterDirection.ReturnValue;

            command.Parameters.Add(paramID);
            command.Parameters.Add(paramAccountID);
            command.Parameters.Add(paramAdvertID);
            command.Parameters.Add(paramTimeStamp);
            command.Parameters.Add(paramReturn);

            command.ExecuteNonQuery();

            con.Close();


        }
    }
}