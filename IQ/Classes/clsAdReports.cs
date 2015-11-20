using System.Data.SqlClient;


public class clsAdReports
{
    public clsAdReports()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        conString = System.Convert.ToString(ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString);

    }
    private string conString; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
    public DataTable getAdImpressions(clsAccount advertiserAccount, DateTime startDate, DateTime endDate)
    {
        DataTable results = default(DataTable);

        SqlConnection con = new SqlConnection(conString);
        con.Open();
        SqlCommand command = new SqlCommand();
        command.CommandText = "AddAdvert";
        command.CommandType = CommandType.StoredProcedure;
        command.Connection = con;


        command.ExecuteNonQuery();

        con.Close();




        return results;
    }



}