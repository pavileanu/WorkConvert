using dataAccess;


public class clsUser : IQ.i_Editable
{


    public int ID { get; set; }
    //Public UserName As String - use email !
    public string RealName { get; set; }

    public string Email { get; set; }
    public nullableString tel1 { get; set; }
    public nullableString tel2 { get; set; }
    public bool Disabled { get; set; }
    public clsChannel Channel { get; set; }
    public Dictionary<int, clsAccount> Accounts { get; set; }

    //Public Buyer As clsChannel 'Works for


    public void CountQuotesPerAccount()
    {

        //Populates numquotes for each user.account

        object sql = null;
        sql = "SELECT fk_account_id_agent as AcID,count(*) AS c FROM quote q JOIN account a ON a.id=q.fk_account_id_agent ";
        sql += "WHERE a.fk_user_id = " + System.Convert.ToString(this.ID); //user id
        sql += " GROUP BY fk_account_id_agent ";

        foreach (var ac in this.Accounts.Values)
        {
            ac.NumQuotes = 0; //Zero them all (becuase they're not all in the recordset)
        }

        SqlClient.SqlConnection con = default(SqlClient.SqlConnection);
        con = da.opendatabase();
        SqlClient.SqlDataReader rdr = default(SqlClient.SqlDataReader);
        rdr = da.DBExecuteReader(con, sql);
        if (rdr.HasRows)
        {
            while (rdr.Read)
            {
                //It's possible that this OM has no knoweledge of an account created in the database by external means (ie. another instance point at the same database)
                if (this.Accounts.ContainsKey(rdr.Item("acid"))) //
                {
                    this.Accounts(rdr.Item("ACiD")).NumQuotes = rdr.Item("c");
                }
            }
        }

        rdr.Close();
        con.Close();

    }

    public clsUser() //Parameterless constructor is called by the editor - which subsequently sets defualt values for most properties
    {

        this.ID = -1;
        this.Accounts = new Dictionary<int, clsAccount>();

    }

    public string displayName(clsLanguage Language)
    {
        string returnValue = "";

        returnValue = this.RealName;

        return returnValue;
    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsUser(this.Channel, this.Email, this.RealName, this.tel1, this.tel2, null, -1);

    }


    public clsUser(clsChannel channel, string email, string RealName, nullableString tel1, nullableString tel2, DataTable uwc, ref int nextID)
    {

        this.Channel = channel;
        // Me.UserName = Username
        this.RealName = RealName;
        this.Email = email;
        this.tel1 = tel1;
        this.tel2 = tel2;
        this.Disabled = false;

        //Me.Buyer = BuyerChannel

        if (uwc == null)
        {

            object sql = null;
            sql = "INSERT INTO [User] (realname,email,tel1,tel2,fk_channel_id) ";
            sql += " VALUES (" + da.SqlEncode(RealName) + "," + da.SqlEncode(email, true) + ",";
            sql += tel1.sqlValue + "," + tel2.sqlValue + "," + Channel.ID + ");"; //NB: SqlValue also SQLencodes

            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));

        }
        else
        {
            this.ID = nextID;
            nextID++;

            System.Data.DataRow row = default(System.Data.DataRow);
            row = uwc.NewRow();

            row["realname"] = RealName;
            row["email"] = email;
            row["tel1"] = tel1.value.ToString();
            row["tel2"] = tel2.value.ToString();
            row["disabled"] = Disabled;
            row["fk_channel_id"] = Channel.ID;
            uwc.Rows.Add(row);
        }

        if (!iq.i_user_email.ContainsKey(this.Email.Trim().ToLower()))
        {
            iq.i_user_email.Add(this.Email.Trim().ToLower(), this);
            iq.Users.Add(this.ID, this); //add to the MASTER list
        }
        else
        {
            Interaction.Beep();
        }

        Accounts = new Dictionary<int, clsAccount>();
        this.Channel.Users.Add(this.ID, this);

    }
    public void delete(ref List<string> errormessages)
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

        iq.Users.Add(this.ID, this); //add to the MASTER list

        if (!iq.i_user_email.ContainsKey(this.Email.ToLower().Trim()))
        {
            iq.i_user_email.Add(this.Email.Trim().ToLower(), this);
        }
        else
        {
            // Stop - this email is not unique
        }

        Accounts = new Dictionary<int, clsAccount>();
        this.Channel.Users.Add(this.ID, this);
        this.Accounts = new Dictionary<int, clsAccount>();

    }

    public void update(ref List<string> errormessages)
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

        object sql = null;
        if (this.Email.Length > 100 | this.RealName.Length > 100)
        {
            errormessages.Add("Email and Real Name length must be less than 100");
        }
        if (this.tel1.value.ToString().Length > 50 || this.tel2.value.ToString().Length > 50)
        {
            errormessages.Add("Telephone numbers must be less than 50");
        }

        sql = "UPDATE [User] SET disabled=" + System.Convert.ToString(this.Disabled ? 1 : 0) + ",email=" + da.SqlEncode(this.Email) + ",realname=" + da.SqlEncode(this.RealName, true) + ",tel1=" + this.tel1.sqlValue + ",tel2=" + this.tel2.sqlValue + " WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);
        //  End Try
    }

} //User