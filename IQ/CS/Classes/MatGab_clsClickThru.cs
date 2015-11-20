
using System.Data.SqlClient;

/// <summary>
/// 
/// </summary>
/// <remarks></remarks>

public class clsClickThru : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsAccount Account {
		get { return m_Account; }
		set { m_Account = Value; }
	}
	private clsAccount m_Account;
	private clsAdvert Advert {
		get { return m_Advert; }
		set { m_Advert = Value; }
	}
	private clsAdvert m_Advert;
	private System.DateTime TimeStamp {
		get { return m_TimeStamp; }
		set { m_TimeStamp = Value; }
	}
	private System.DateTime m_TimeStamp;

	private string conString = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString;

	public clsClickThru()
	{

	}

	public clsClickThru(clsAccount account, clsAdvert advert, System.DateTime timestamp)
	{
		SqlConnection con = new SqlConnection(conString);
		con.Open();
		SqlCommand command = new SqlCommand();
		command.CommandText = "AddClickThru";
		command.CommandType = CommandType.StoredProcedure;
		command.Connection = con;
		SqlParameter paramUserID = new SqlParameter("@accountid", SqlDbType.Int);
		paramUserID.Value = account.ID;
		SqlParameter paramAdvertID = new SqlParameter("@advertid", SqlDbType.Int);
		paramAdvertID.Value = advert.ID;
		SqlParameter paramTimeStamp = new SqlParameter("@timestamp", SqlDbType.DateTime);
		paramTimeStamp.Value = timestamp;

		SqlParameter paramReturn = new SqlParameter("@ret", SqlDbType.Int);
		paramReturn.Direction = ParameterDirection.ReturnValue;

		command.Parameters.Add(paramUserID);
		command.Parameters.Add(paramAdvertID);
		command.Parameters.Add(paramTimeStamp);
		command.Parameters.Add(paramReturn);

		command.ExecuteNonQuery();

		con.Close();

		this.ID = Convert.ToInt32(paramReturn.Value);
		this.Account = account;
		this.Advert = advert;
		this.TimeStamp = timestamp;

		this.Advert.ClickThrus.Add(this.ID, this);
		this.Account.ClickThrus.Add(this.ID, this);

	}
	public clsClickThru(int ID, clsUser user, clsAdvert advert, System.DateTime timestamp)
	{
		this.ID = ID;
		this.Account = Account;
		this.Advert = advert;
		this.TimeStamp = timestamp;

		this.Advert.ClickThrus.Add(this.ID, this);
		this.Account.ClickThrus.Add(this.ID, this);

	}
	public void i_Editable.delete(ref List<string> errorMessages)
	{
		this.Advert.ClickThrus.Remove(this.ID);
		this.Account.ClickThrus.Remove(this.ID);
	}

	public string i_Editable.displayName(clsLanguage Language)
	{

	}

	public object i_Editable.Insert(ref List<string> errorMessages)
	{
		return new clsClickThru(this.Account, this.Advert, this.TimeStamp);
	}

	public void i_Editable.update(ref List<string> errorMessages)
	{

		if (this.ID > 0) {
			SqlConnection con = new SqlConnection(conString);
			con.Open();
			SqlCommand command = new SqlCommand();
			command.CommandText = "UpdateClickThru";
			command.CommandType = CommandType.StoredProcedure;
			command.Connection = con;
			SqlParameter paramID = new SqlParameter("@ID", SqlDbType.Int);
			paramID.Value = this.ID;
			SqlParameter paramUserID = new SqlParameter("@accountid", SqlDbType.Int);
			paramUserID.Value = Account.ID;
			SqlParameter paramAdvertID = new SqlParameter("@adverid", SqlDbType.Int);
			paramAdvertID.Value = Advert.ID;
			SqlParameter paramTimeStamp = new SqlParameter("@url", SqlDbType.DateTime);
			paramTimeStamp.Value = TimeStamp;

			SqlParameter paramReturn = new SqlParameter("@ret", SqlDbType.Int);
			paramReturn.Direction = ParameterDirection.ReturnValue;

			command.Parameters.Add(paramUserID);
			command.Parameters.Add(paramAdvertID);
			command.Parameters.Add(paramTimeStamp);
			command.Parameters.Add(paramReturn);

			command.ExecuteNonQuery();

			con.Close();
		}
	}
}
