

//each user has many accounts - each having a password (which *may* be the same as other accounts)
//The account *can* have a team (meaning the user can be a member of many teams - 1 per account)
//The account is 'with' a seller channel - this is who the user is buying stuff (and seeing prices and stock) from.
//The account has a role - giving a set of rights - see clsRole

using dataAccess;
[Serializable()]
public class clsAccount : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string Password {
		get { return m_Password; }
		set { m_Password = Value; }
	}
	private string m_Password;
	private bool MustChangePassword {
		get { return m_MustChangePassword; }
		set { m_MustChangePassword = Value; }
	}
	private bool m_MustChangePassword;
	private clsChannel SellerChannel {
		get { return m_SellerChannel; }
		set { m_SellerChannel = Value; }
	}
	private clsChannel m_SellerChannel;
	//The channel that's selling - ie.that this account buys from or is 'with'  - this is NOT editable (add a customerAccount under a Channel to make one of these)
	private clsTeam Team {
		get { return m_Team; }
		set { m_Team = Value; }
	}
	private clsTeam m_Team;
	private clsUser User {
		get { return m_User; }
		set { m_User = Value; }
	}
	private clsUser m_User;
	//again NOT editable - add an account under a user
	private Dictionary<int, clsQuote> Quotes {
		get { return m_Quotes; }
		set { m_Quotes = Value; }
	}
	private Dictionary<int, clsQuote> m_Quotes;
	private clsLanguage Language {
		get { return m_Language; }
		set { m_Language = Value; }
	}
	private clsLanguage m_Language;
	private clsCurrency Currency {
		get { return m_Currency; }
		set { m_Currency = Value; }
	}
	private clsCurrency m_Currency;
	private clsCulture Culture {
		get { return m_Culture; }
		set { m_Culture = Value; }
	}
	private clsCulture m_Culture;
	private clsRole[] Roles {
		get { return i_roles_code.Values.ToArray; }
	}
	private Dictionary<string, clsRole> i_roles_code {
		get { return m_i_roles_code; }
		set { m_i_roles_code = Value; }
	}
	private Dictionary<string, clsRole> m_i_roles_code;
	private int NumQuotes {
		get { return m_NumQuotes; }
		set { m_NumQuotes = Value; }
	}
	private int m_NumQuotes;
	//Update via the USER.countQuotesPerAccount method (which will count the quotes for all a users' accounts) - this is NOT persisted to the database
	private clsPriceBand Priceband {
		get { return m_Priceband; }
		set { m_Priceband = Value; }
	}
	private clsPriceBand m_Priceband;
	//String
	private clsChannel BuyerChannel {
		get { return m_BuyerChannel; }
		set { m_BuyerChannel = Value; }
	}
	private clsChannel m_BuyerChannel;
	// carries information about the company this buyer works for (who, coincidently, may also be a seller in their own right)
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
	private string mfrCode {
		get { return m_mfrCode; }
		set { m_mfrCode = Value; }
	}
	private string m_mfrCode;

	//BRAZIL
		//The default (empty string) will display all warehouses (variants), Set to 'NONE' to see list prices,  any valid warehouse code will display only variants for that warehouse,
	public string wareHouseFilter = "";

	public string i_Editable.displayName(clsLanguage lang)
	{
		displayName = this.BuyerChannel.DisplayName(lang) + "-" + this.User.RealName;
	}


	public void i_Editable.delete(ref List<string> errorMessages)
	{
		iq.Accounts.Remove(this.ID);
		iq.Users(this.User.ID).Accounts.Remove(this.ID);

		try {
			da.DBExecutesql("DELETE FROM account where id=" + this.ID);
		} catch (Exception ex) {
			errorMessages.Add(ex.Message.ToString);
		}

	}


	public clsAccount()
	{
		this.ID = -1;
		this.Quotes = new Dictionary<int, clsQuote>();
		this.Impressions = new Dictionary<int, clsImpression>();
		this.ClickThrus = new Dictionary<int, clsClickThru>();

	}

	public int MaxQuoteVersion(clsQuote rootQuote)
	{

		MaxQuoteVersion = 0;

		//returns the highest version number of the quote with the specified root quoteid

		foreach ( quote in this.Quotes.Values.ToList) {
			if (object.ReferenceEquals(quote.RootQuote, rootQuote)) {
				if (quote.Version > MaxQuoteVersion)
					MaxQuoteVersion = quote.Version;
			}
		}

	}

	public clsAccount(clsUser user, string Password, clsChannel BuyerChannel, clsRole[] roles, clsTeam Team, clsLanguage Language, clsCurrency currency, clsChannel sellerChannel, clsPriceBand priceBand, clsCulture culture,

	string mfrCode, DataTable wc = null, ref int nextid = null)
	{
		string teamOrNull;

		if (Team == null)
			teamOrNull = "null";
		else
			teamOrNull = Team.ID.ToString;

		//If user Is Nothing Then user = iq.Users.Values.First 'This is a bit of a dirty fix to allow users to be created in the editor

		this.i_roles_code = new Dictionary<string, clsRole>();
		this.Password = Password;
		this.BuyerChannel = BuyerChannel;
		this.Team = Team;
		this.Language = Language;
		this.Currency = currency;
		this.User = user;
		this.SellerChannel = sellerChannel;
		this.Priceband = priceBand;
		this.Culture = culture;
		this.mfrCode = mfrCode;

		foreach ( r in roles) {
			this.i_roles_code.Add(r.Code, r);
		}


		if (wc == null) {
			object sql;
			sql = "INSERT INTO [Account] (FK_user_id,Password,fk_team_id,fk_language_id,fk_currency_id,fk_channel_id_buyer,fk_channel_id_seller,priceBand, fk_culture_id,mfrCode) ";
			sql += "VALUES (" + user.ID + "," + da.SqlEncode(Password) + "," + teamOrNull + "," + Language.ID + "," + currency.ID + "," + BuyerChannel.ID + "," + sellerChannel.ID + "," + da.SqlEncode(priceBand.text) + "," + this.Culture.ID + "," + da.SqlEncode(this.mfrCode) + ");";

			this.ID = da.DBExecutesql(sql, true);

			foreach ( r in this.i_roles_code.Values) {
				sql = "INSERT INTO AccountRoles VALUES (" + this.ID + "," + r.ID + ")";
				da.DBExecutesql(sql);
			}


		} else {
			this.ID = nextid;
			nextid += 1;

			System.Data.DataRow row;
			row = wc.NewRow();
			row("ID") = this.ID;
			//- we EXPLICITLY set ids on branches
			row("FK_User_id") = this.User.ID;
			row("password") = this.Password;
			if (this.Team == null) {
				row("fk_team_id") = DBNull.Value;
			} else {
				row("fk_team_id") = this.Team.ID;
			}

			row("fk_language_id") = this.Language.ID;
			row("fk_currency_id") = this.Currency.ID;
			//Multiple currency from one disti are not yet supported (or required) - but probably will be
			row("fk_channel_id_buyer") = BuyerChannel.ID;
			row("fk_channel_id_seller") = sellerChannel.ID;
			row("priceBand") = this.Priceband;
			row("mfrCode") = this.mfrCode;

			row("fk_culture_id") = this.Culture.ID;

			wc.Rows.Add(row);

		}
		this.User.Accounts.Add(this.ID, this);

		//iq.Users(user.ID).Accounts.Add(Me.ID, Me)
		iq.Accounts.Add(this.ID, this);
		//add to the MASTER list
		object ck;
		ck = this.SellerChannel.Code + "^" + this.Priceband.text;
		if (!iq.i_Account_HostIDpriceBand.ContainsKey(ck)) {
			iq.i_Account_HostIDpriceBand.Add(ck, this);
			//Add a compund key of (seller) hostID^accountpriceBand - used during hostPrices import
		} else {
			//  Throw New Exception("Duplicate host accountnum compound key:" & ck$)
		}

		sellerChannel.CustomerAccounts.Add(this.ID, this);

		Quotes = new Dictionary<int, clsQuote>();
		this.Impressions = new Dictionary<int, clsImpression>();
		this.ClickThrus = new Dictionary<int, clsClickThru>();
		NumQuotes = -1;

	}

	public clsAccount(int Id, clsUser user, string Password, clsChannel BuyerChannel, clsRole[] roles, clsTeam Team, clsLanguage Language, clsCurrency Currency, clsChannel sellerchannel, clsPriceBand priceBand,

	clsCulture culture, string mfrCode)
	{
		this.i_roles_code = new Dictionary<string, clsRole>();
		this.ID = Id;
		this.Password = Password;
		this.BuyerChannel = BuyerChannel;
		this.Team = Team;
		this.Language = Language;
		this.Currency = Currency;
		this.User = user;
		this.SellerChannel = sellerchannel;
		this.Priceband = priceBand;
		this.Culture = culture;
		this.mfrCode = mfrCode;

		foreach ( r in roles) {
			this.i_roles_code.Add(r.Code, r);
		}
		this.User.Accounts.Add(this.ID, this);

		if (sellerchannel == null)
			System.Diagnostics.Debugger.Break();

		//iq.Users(user.ID).Accounts.Add(Me.ID, Me)
		iq.Accounts.Add(this.ID, this);
		//add to the MASTER list

		object ck;
		ck = this.SellerChannel.Code + "^" + this.Priceband.text;

		if (iq.i_Account_HostIDpriceBand.ContainsKey(ck)) {
		//  Logit(ck$ & " is duplicated ")
		} else {
			iq.i_Account_HostIDpriceBand.Add(ck, this);
			//Add a compund key of (seller) hostID^accountpriceBand - used during hostPrices import
		}

		sellerchannel.CustomerAccounts.Add(this.ID, this);

		this.Quotes = new Dictionary<int, clsQuote>();
		this.Impressions = new Dictionary<int, clsImpression>();
		this.ClickThrus = new Dictionary<int, clsClickThru>();
		NumQuotes = -1;

	}


	public void LoadQuotes(int BuyerFilter)
	{
		//loads all quotes pertaining to this agentAccount (into the account.quotes) - AND the root level quotes dictionary
		//NB: Does NOT laod a quotes quoteItems

		this.Quotes.Clear();
		//Clears the quotes collection of this ACCOUNT

		StringBuilder sql = new StringBuilder(string.Empty);
		sql.AppendFormat("{0}", "SELECT [ID],[FK_Account_ID_Agent],[FK_State_ID],[Created],[FK_Account_ID_Buyer],[Locked],[Hidden],[Notes],[Description],[Price],[Version],[FK_Quote_ID_Root],[Name],[FK_Currency_ID],[Reference],[Updated],[FK_import_id],[saved],[totalrebate] ");
		sql.AppendFormat("{0}{1}", " FROM quote WHERE fk_account_id_agent = ", this.ID);
		if (BuyerFilter != 0) {
			sql.AppendFormat("{0}{1}", "AND fk_account_id_buyer= " + BuyerFilter.ToString);
		}


		SqlClient.SqlConnection con;
		con = da.OpenDatabase();

		SqlClient.SqlDataReader rdr;
		rdr = da.DBExecuteReader(con, sql.ToString);

		clsAccount buyerAccount = null;
		clsQuote aQuote;
		List<string> errorMessages = new List<string>();
		int rows = 0;
		while (rdr.Read) {
			rows += 1;
			if (this.SellerChannel.CustomerAccounts.ContainsKey((int)rdr.Item("fk_account_id_agent"))) {
				//If iq.Accounts.ContainsKey(rdr.Item("fk_account_id_agent")) Then
				buyerAccount = this.SellerChannel.CustomerAccounts((int)rdr.Item("fk_account_id_buyer"));
				nullableString notes = new nullableString(rdr.Item("Notes"));
				//    If rdr.Item("name") IsNot DBNull.Value Then Stop
				nullableString name = new nullableString(rdr.Item("name"));
				nullableString desc = new nullableString(rdr.Item("Description"));
				clsCurrency currency = iq.Currencies((int)rdr.Item("fk_currency_id"));
				//It doesn't need a sellerAccount becuase that's the buyerAccount's FK_Channel_ID_seller
				//Dim culture As clsCulture = iq.Cultures(CInt(rdr.Item("fk_culture_id")))


					//aQuote = New clsQuote(CInt(.Item("id")), buyerAccount, Me, created, updated, notes, rootQuote, CInt(.Item("Version")), state, New nullablePrice(currency), currency, desc, name, rdr.Item("reference").ToString)
					//fix unsaved/legacy quotes


				 // ERROR: Not supported in C#: WithStatement

			} else {
				Logit("Missing buyer account for quote " + rdr.Item("Id").ToString);
			}

		}

		rdr.Close();

		con.Close();

	}

	public object i_Editable.insert(ref List<string> errorMessages)
	{

		return new clsAccount(this.User, this.Password, this.BuyerChannel, this.i_roles_code.Values.ToArray, this.Team, this.Language, this.Currency, this.SellerChannel, this.Priceband, this.Culture,
		this.mfrCode);

	}


	public void i_Editable.update(ref List<string> errorMessages)
	{
		//Martin has left - and I don't like the idea of queing all the data - we only need send the id of the object to re-instance from the database to the 'other' machine
		// simple implemntaion need only deal with the root level dictionaries  

		//Dim toUpdate As List(Of Tuple(Of String, String, Object, Int32?)) = New List(Of Tuple(Of String, String, Object, Int32?))()

		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("FK_User_Id", "User", Me.User.ID, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("FK_Channel_Id_Seller", "SellerChannel", Me.SellerChannel.ID, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("Password", "Password", Me.Password, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("fk_team_id", "Team", If(Me.Team Is Nothing, Nothing, CType(Me.Team.ID, Int32?)), Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("fk_language_id", "Language", Me.Language.ID, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("fk_currency_id", "Currency", Me.Currency.ID, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("priceBand", "Priceband", Me.Priceband.text, Nothing))
		//toUpdate.Add(New Tuple(Of String, String, Object, Int32?)("Fk_culture_ID", "Culture", Me.Culture.ID, Nothing))

		object sql__1;
		//Try
		//    clsBrokerManager.Update("Accounts(" & Me.ID & ")", toUpdate)
		//Catch ex As Exception
		//    ErrorLog.Add(ex)
		//    'Broker is down, what do we want to do?
		//    'Ultimately this needs to go in a threaded queue and be retried, for now fall back to the original code
		string teamID;
		if (this.Team == null)
			teamID = "null";
		else
			teamID = this.Team.ID.ToString;

		Sql = "update [Account] ";
		Sql += "SET FK_user_id=" + this.User.ID + ",fk_Channel_id_seller=" + this.SellerChannel.ID;
		Sql += ",password=" + da.SqlEncode(this.Password);
		Sql += ",fk_team_id=" + teamID + ",fk_language_id=" + this.Language.ID + ",fk_currency_id=" + this.Currency.ID;
		Sql += ",priceBand=" + da.SqlEncode(this.Priceband.text) + ", Fk_culture_ID =" + this.Culture.ID;
		Sql += " WHERE id=" + this.ID;

		da.DBExecutesql(Sql, false);
		// End Try
		//If Me.ID < 0 Then Stop

		//Dim teamID As String
		//If Me.Team Is Nothing Then teamID = "null" Else teamID = Me.Team.ID.ToString
		//sql$ = "update [Account] "
		//sql$ &= "SET FK_user_id=" & Me.User.ID & ",fk_Channel_id_seller=" & Me.SellerChannel.ID
		//sql$ &= ",password=" & da.SqlEncode(Me.Password)
		//sql$ &= ",fk_team_id=" & teamID & ",fk_language_id=" & Me.Language.ID & ",fk_currency_id=" & Me.Currency.ID
		//sql$ &= ",priceBand=" & da.SqlEncode(Me.Priceband.Text) & ", Fk_culture_ID =" & Me.Culture.ID
		//sql$ &= " WHERE id=" & Me.ID

		//da.DBExecutesql(sql$, False)

		//Set roles
		Sql = "DELETE FROM AccountRoles WHERE FK_Account_Id=" + this.ID;
		da.DBExecutesql(Sql, false);

		if (i_roles_code.Count > 0) {
			Sql = string.Join(";", i_roles_code.Select(irc => "INSERT INTO AccountRoles (FK_Account_Id,FK_Role_Id) VALUES (" + this.ID + "," + irc.Value.ID + ")"));
			da.DBExecutesql(Sql, false);
		}

	}

	/// <summary>
	/// Checks id this account has the selected right code in any roles
	/// </summary>
	/// <param name="right">Right code as string</param>
	/// <returns>User has this right</returns>
	/// <remarks></remarks>
	public bool HasRight(string right)
	{
		if (!iq.i_right_Code.ContainsKey(right))
			return false;
		return this.i_roles_code.SelectMany(rid => rid.Value.Rights.Select(ri => ri.Value)).Contains(iq.i_right_Code(right));
	}

	public void AddRole(clsRole role)
	{
		if (!this.i_roles_code.ContainsKey(role.Code)) {
			this.i_roles_code.Add(role.Code, role);

			object sql__1;
			Sql = "INSERT INTO AccountRoles (FK_Account_Id,FK_Role_Id) VALUES (" + this.ID + "," + role.ID + ")";
			da.DBExecutesql(Sql, false);
		}

	}
	public void RemoveRole(clsRole role)
	{
		if (this.i_roles_code.ContainsKey(role.Code)) {
			this.i_roles_code.Remove(role.Code);

			object sql;
			sql = "DELETE FROM AccountRoles WHERE fk_Account_id=" + this.ID + " AND fk_Role_Id=" + role.ID;
			da.DBExecutesql(sql, false);
		}

	}

	public List<string> ResetPassword()
	{

		string pw;
		pw = GeneratePassword();

		//we do NOT set the password on the account (yet) - the act of clicking the URL (will actually do the reset)
		List<string> em = new List<string>();
		Dictionary<string, string> tags = new Dictionary<string, string>();

		string baseurl = ConfigurationManager.AppSettings("BaseURL");

		//The link specifies the accountID (to reset), the new (salted) hash of the password, and a hash of the pair (as an anti-tamper device)
		//Although this is one place an attacker could see both the plaintext, and the hashed version -the salt makes it impossible to determine anything useful

		tags.Add("url", baseurl + "/aspx/signin.aspx?reset=" + Trim(this.ID.ToString) + "&pw=" + simpleHash(pw) + "&antiTamper=" + simpleHash(Trim(this.ID.ToString) + simpleHash(pw)).ToString);

		tags.Add("password", pw);
		tags.Add("firstname", Split(this.User.RealName, " ")(0));
		tags.Add("hostname", this.SellerChannel.DisplayName(English));
		tags.Add("email", this.User.Email);
		tags.Add("mfr", this.mfrCode);
		tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

		SendEmail(this.User.Email, "forgotten.htm", tags, this.Language, em, false);

		return em;

	}

	public List<string> ResendWelcomeEmail()
	{
		ResendWelcomeEmail = new List<string>();
		string baseurl = ConfigurationManager.AppSettings("BaseURL");
		string pw;
		pw = GeneratePassword();
		Dictionary<string, string> tags = new Dictionary<string, string>();

		string url;
		url = baseurl + "signin.aspx";
		tags.Add("hostname", SellerChannel.DisplayName(Language));
		tags.Add("email", User.Email);
		tags.Add("password", pw);
		tags.Add("firstname", User.RealName);
		tags.Add("url", url);
		tags.Add("mfr", this.mfrCode);
		tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

		SendEmail(User.Email, "WelcomeEmail.htm", tags, Language, ResendWelcomeEmail, true);
	}

	public Manufacturer Manufacturer {


		get {
			Manufacturer = Manufacturer.Unknown;

			if (string.Equals(this.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase)) {
				Manufacturer = Manufacturer.HPI;
			} else if (string.Equals(this.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase)) {
				Manufacturer = Manufacturer.HPE;
			}

		}
	}


	public string ManufacturerDescription {


		get {
			ManufacturerDescription = string.Empty;

			if (this.Manufacturer == global::IQ.Manufacturer.HPE) {
				ManufacturerDescription = "Hewlett Packard Enterprise";
			} else if (this.Manufacturer == global::IQ.Manufacturer.HPI) {
				ManufacturerDescription = "HP Inc.";
			}

		}
	}


}
//End of clsAccount


