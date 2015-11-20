
using System.Data.SqlClient;
public class clsAdReports
{
	private string conString = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString;
	public DataTable getAdImpressions(clsAccount advertiserAccount, System.DateTime startDate, System.DateTime endDate)
	{
		DataTable results;

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
