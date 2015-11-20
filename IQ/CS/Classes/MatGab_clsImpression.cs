
using System.Data.SqlClient;

/// <summary>
/// 
/// </summary>
/// <remarks></remarks>

public class clsImpression : i_Editable
{
	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsAdvert Advert {
		get { return m_Advert; }
		set { m_Advert = Value; }
	}
	private clsAdvert m_Advert;
	private clsAccount Account {
		get { return m_Account; }
		set { m_Account = Value; }
	}
	private clsAccount m_Account;
	private int Count {
		get { return m_Count; }
		set { m_Count = Value; }
	}
	private int m_Count;
	private System.DateTime IDate {
		get { return m_IDate; }
		set { m_IDate = Value; }
	}
	private System.DateTime m_IDate;

	private string conString = ConfigurationManager.ConnectionStrings("DBConnectString").ConnectionString;

	public clsImpression()
	{
	}

	public clsImpression(clsAccount account, clsAdvert advert, System.DateTime timestamp)
	{

		if ((advert != null)) {
			SqlConnection con = new SqlConnection(conString);
			con.Open();
			SqlCommand command = new SqlCommand();
			command.CommandText = "Addimpression";
			command.CommandType = CommandType.StoredProcedure;
			command.Connection = con;
			SqlParameter paramAdvertID = new SqlParameter("@advertid", SqlDbType.Int);
			paramAdvertID.Value = advert.ID;
			SqlParameter paramAccountID = new SqlParameter("@accountid", SqlDbType.Int);
			paramAccountID.Value = account.ID;
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

			this.ID = Convert.ToInt32(paramReturn.Value);
			this.Account = account;
			this.Advert = advert;
			this.IDate = timestamp;
			this.Count = 1;
			this.Advert.Impressions.Add(this.ID, this);
			this.Account.Impressions.Add(this.ID, this);

		}
	}

	public clsImpression(int ID, clsAccount account, clsAdvert advert, System.DateTime timestamp)
	{
		this.ID = ID;
		this.Account = account;
		this.Advert = advert;
		this.IDate = timestamp;
		this.Count = 1;
		this.Advert.Impressions.Add(this.ID, this);
		this.Account.Impressions.Add(this.ID, this);

	}


	public void i_Editable.delete(ref List<string> errorMessages)
	{
		//this doesnt appear to remove from the db ?/
		this.Advert.Impressions.Remove(this.ID);
		this.Account.Impressions.Remove(this.ID);

	}

	public string i_Editable.displayName(clsLanguage Language)
	{

	}

	public object i_Editable.Insert(ref List<string> errorMessages)
	{
		return new clsImpression(this.Account, this.Advert, this.IDate);
	}

	public void i_Editable.update(ref List<string> errorMessages)
	{
		if (this.ID > 0) {
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
