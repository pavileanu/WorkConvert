using dataAccess;


public class clsUser : IQ.i_Editable
{
	//this defines a set of function this class *must* implement/expose

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	//Public UserName As String - use email !
	private string RealName {
		get { return m_RealName; }
		set { m_RealName = Value; }
	}
	private string m_RealName;

	private string Email {
		get { return m_Email; }
		set { m_Email = Value; }
	}
	private string m_Email;
	private nullableString tel1 {
		get { return m_tel1; }
		set { m_tel1 = Value; }
	}
	private nullableString m_tel1;
	private nullableString tel2 {
		get { return m_tel2; }
		set { m_tel2 = Value; }
	}
	private nullableString m_tel2;
	private bool Disabled {
		get { return m_Disabled; }
		set { m_Disabled = Value; }
	}
	private bool m_Disabled;
	private clsChannel Channel {
		get { return m_Channel; }
		set { m_Channel = Value; }
	}
	private clsChannel m_Channel;
	private Dictionary<int, clsAccount> Accounts {
		get { return m_Accounts; }
		set { m_Accounts = Value; }
	}
	private Dictionary<int, clsAccount> m_Accounts;

	//Public Buyer As clsChannel 'Works for



	public void CountQuotesPerAccount()
	{
		//Populates numquotes for each user.account

		object sql;
		sql = "SELECT fk_account_id_agent as AcID,count(*) AS c FROM quote q JOIN account a ON a.id=q.fk_account_id_agent ";
		sql += "WHERE a.fk_user_id = " + this.ID;
		//user id
		sql += " GROUP BY fk_account_id_agent ";

		foreach ( ac in this.Accounts.Values) {
			ac.NumQuotes = 0;
			//Zero them all (becuase they're not all in the recordset)
		}

		SqlClient.SqlConnection con;
		con = da.opendatabase();
		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, sql);
		if (rdr.HasRows) {
			while (rdr.Read) {
				//It's possible that this OM has no knoweledge of an account created in the database by external means (ie. another instance point at the same database)
				//
				if (this.Accounts.ContainsKey(rdr.Item("acid"))) {
					this.Accounts(rdr.Item("ACiD")).NumQuotes = rdr.Item("c");
				}
			}
		}

		rdr.Close();
		con.Close();

	}

	//Parameterless constructor is called by the editor - which subsequently sets defualt values for most properties
	public clsUser()
	{

		this.ID = -1;
		this.Accounts = new Dictionary<int, clsAccount>();

	}

	public string i_Editable.displayName(clsLanguage Language)
	{

		displayName = this.RealName;

	}

	public object i_Editable.Insert(ref List<string> errormessages)
	{

		return new clsUser(this.Channel, this.Email, this.RealName, this.tel1, this.tel2);

	}



	public clsUser(clsChannel channel, string email, string RealName, nullableString tel1, nullableString tel2, DataTable uwc = null, ref int nextID = -1)
	{
		this.Channel = channel;
		// Me.UserName = Username
		this.RealName = RealName;
		this.Email = email;
		this.tel1 = tel1;
		this.tel2 = tel2;
		this.Disabled = false;

		//Me.Buyer = BuyerChannel


		if (uwc == null) {
			object sql;
			sql = "INSERT INTO [User] (realname,email,tel1,tel2,fk_channel_id) ";
			sql += " VALUES (" + da.SqlEncode(RealName) + "," + da.SqlEncode(email, true) + ",";
			sql += tel1.sqlValue + "," + tel2.sqlValue + "," + channel.ID + ");";
			//NB: SqlValue also SQLencodes

			this.ID = da.DBExecutesql(sql, true);

		} else {
			this.ID = nextID;
			nextID += 1;

			System.Data.DataRow row;
			row = uwc.NewRow();

			row("realname") = RealName;
			row("email") = email;
			row("tel1") = tel1.value.ToString();
			row("tel2") = tel2.value.ToString();
			row("disabled") = Disabled;
			row("fk_channel_id") = channel.ID;
			uwc.Rows.Add(row);
		}

		if (!iq.i_user_email.ContainsKey(LCase(Trim(this.Email)))) {
			iq.i_user_email.Add(LCase(Trim(this.Email)), this);
			iq.Users.Add(this.ID, this);
			//add to the MASTER list
		} else {
			Beep();
		}

		Accounts = new Dictionary<int, clsAccount>();
		this.Channel.Users.Add(this.ID, this);

	}

	public void i_Editable.delete(ref List<string> errormessages)
	{
		//Deleteing users would require deleing their quotes, accounts and other descendant objects
		//IQ.Users.REMOVE

		errormessages.Add("Users cannot be deleted - because of dependencies (quotes etc)");

	}


	public clsUser(int id, clsChannel channel, string email, string RealName, nullableString tel1, nullableString tel2, bool disabled)
	{
		this.ID = id;
		//Me.UserName = Username
		this.RealName = RealName;
		this.Email = email;
		this.tel1 = tel1;
		this.tel2 = tel2;
		this.Disabled = disabled;
		this.Channel = channel;

		//Me.Buyer = Buyer

		iq.Users.Add(this.ID, this);
		//add to the MASTER list

		if (!iq.i_user_email.ContainsKey(Trim(LCase(this.Email)))) {
			iq.i_user_email.Add(LCase(Trim(this.Email)), this);
		} else {
			// Stop - this email is not unique
		}

		Accounts = new Dictionary<int, clsAccount>();
		this.Channel.Users.Add(this.ID, this);
		this.Accounts = new Dictionary<int, clsAccount>();

	}


	public void i_Editable.update(ref List<string> errormessages)
	{

		//Dim toUpdate As List(Of Tuple(Of String, String, Object, Int32?)) = New List(Of Tuple(Of String, String, Object, Int32?))()

		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("Disabled", "Disabled", IIf(Me.Disabled, 1, 0), Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("Email", "Email", Me.Email, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("RealName", "RealName", Me.RealName, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("tel1", "tel1", Me.tel1.v, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("tel2", "tel2", Me.tel2.v, Nothing))

		//Try
		//    clsBrokerManager.Update("Users(" & Me.ID & ")", toUpdate)
		//Catch ex As Exception
		//ErrorLog.Add(ex)
		//            If Me.ID = -1 Then Stop

		object sql;
		if (this.Email.Length > 100 | this.RealName.Length > 100)
			errormessages.Add("Email and Real Name length must be less than 100");
		if (this.tel1.value.ToString().Length > 50 | this.tel2.value.ToString().Length > 50)
			errormessages.Add("Telephone numbers must be less than 50");

		sql = "UPDATE [User] SET disabled=" + IIf(this.Disabled, 1, 0) + ",email=" + da.SqlEncode(this.Email) + ",realname=" + da.SqlEncode(this.RealName, true) + ",tel1=" + this.tel1.sqlValue + ",tel2=" + this.tel2.sqlValue + " WHERE ID=" + this.ID;
		da.DBExecutesql(sql);
		//  End Try
	}

}
//User
