using System.Text.RegularExpressions;
using dataAccess;
using System.Data;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;


[DataContractAttribute()]
public class clsNameValuePair
{
	[DataMemberAttribute()]
	public string Name;
	[DataMemberAttribute()]
	public string value;
}

[DataContractAttribute()]
public class clsToken
{
	[DataMemberAttribute()]
	public string Value;
	[DataMemberAttribute()]
	public List<string> Errors;
}


[DataContractAttribute()]
public class clsGKAccount
{

	[DataMemberAttribute()]
	public string sc_hostCode;
	[DataMemberAttribute()]
	public string u_Name;
	[DataMemberAttribute()]
	public string u_email;
	[DataMemberAttribute()]
	public string a_PriceBand;
	[DataMemberAttribute()]
	public string u_Telephone;
	[DataMemberAttribute()]
	public string bc_CompanyName;
	[DataMemberAttribute()]

	public string bc_PostCode;

	public clsGKAccount(string sc_hc, string u_name, string u_email, string a_priceband, string u_telephone, string bc_CompanyName, string bc_postcode)
	{
		this.sc_hostCode = sc_hc;
		this.u_Name = u_name;
		this.u_email = u_email;
		this.a_PriceBand = a_priceband;
		this.u_Telephone = u_telephone;
		this.bc_PostCode = bc_postcode;
		this.bc_CompanyName = bc_CompanyName;

	}

}


[DataContractAttribute()]
public class clsName
{
	public int ID;
	[DataMemberAttribute()]
	public string name;
	[DataMemberAttribute()]
	public string Example;
	[DataMemberAttribute()]
	public bool Required;
	[DataMemberAttribute()]
	public string RegEx;
	[DataMemberAttribute()]
	public int MinLength;
	[DataMemberAttribute()]
	public int MaxLength;
	[DataMemberAttribute()]

	public string Notes;

	public clsName(int ID, string Name, string Example, bool Required, string RegEx, int MinLength, int MaxLength, string Notes)
	{
		this.ID = ID;
		this.name = Name;
		this.Example = Example;
		this.Required = Required;
		this.RegEx = RegEx;
		this.MinLength = MinLength;
		this.MaxLength = MaxLength;
		this.Notes = Notes;

	}

}




[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
public class TokenFactory : IOneTimeToken
{


	/// <summary>Fetches a diictionary of all permissible Name-Value-Pair Names, including their validation rules</summary>
	/// <returns>Dictionary Name>clsName</returns>

	private Dictionary<string, clsName> LoadNames()
	{

		LoadNames = new Dictionary<string, clsName>(StringComparer.CurrentCultureIgnoreCase);
		SqlClient.SqlConnection con;
		con = da.OpenDatabase();
		object sql = "SELECT ID,Name,Example,Required,RegEx,MinLength,MaxLength,Notes FROM gk.name";

		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (LoadNames.ContainsKey(rdr.Item("Name")))
				throw new Exception("The NAME " + rdr.Item("Name") + " is defined more than once !");
			LoadNames.Add(rdr.Item("Name"), new clsName(rdr.Item("ID"), rdr.Item("Name"), rdr.Item("Example"), rdr.Item("Required"), rdr.Item("Regex"), rdr.Item("MinLength"), rdr.Item("Maxlength"), rdr.Item("Notes")));
		}
		rdr.Close();
		con.Close();

	}

	public clsToken IOneTimeToken.GetToken(string HostId, string HostToken, List<clsNameValuePair> NameValuePairs)
	{

		//Gives us the names and validation rules for each possible Name-Value Pair
		Dictionary<string, clsName> names = LoadNames();

		clsToken retval = new clsToken();
		retval.Value = "";
		retval.Errors = new List<string>();


		try {
			//Convert the supplied 'Pairs' into a Dictionary of name>value (it's a shame we cant expose dictionaries via SOAP webservices !)
			Dictionary<string, string> NVPs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
			foreach ( p in NameValuePairs) {
				if (NVPs.ContainsKey(LCase(p.Name))) {
					retval.Errors.Add("You have provided more than one value for " + p.Name + " (max one).");
				}
				NVPs.Add(LCase(p.Name), p.value);
			}

			if (!iq.i_channel_code.ContainsKey(HostId)) {
				retval.Errors.Add("HostID " + HostId + " is not recognised.(object model not loaded?) - there are " + iq.Channels.Count + " channels loaded");
			} else {
				clsChannel channel = iq.i_channel_code(HostId);
				if (HostToken != channel.WebToken) {
					retval.Errors.Add("Invalid password ('fixed' token) - logon is forbidden for 5 seconds");
					//that's a lie
				} else {
					foreach ( name in names.Values) {
						if (name.Required & !NVPs.ContainsKey(name.name)) {
							retval.Errors.Add("A Name/value pair for '" + name.name + "' is required. for example " + name.Example + " Help:" + name.Notes);
						} else if (NVPs.ContainsKey(name.name)) {
							if (name.Required & Trim(NVPs(name.name)) == "") {
								retval.Errors.Add("You have supplied an empty value for for '" + name.name + "' which is a required field. for example " + name.Example + " Help:" + name.Notes);
							} else {
								string value = NVPs(name.name);
								if ((!name.Required) & (value == "")) {
								//Skip emmpty non required fields
								} else {
									//but validate it if it's required OR populated
									if (Len(value) < name.MinLength) {
										retval.Errors.Add("The value '" + value + "' for '" + name.name + "' must be at least " + name.MinLength + " characters." + name.Notes);
									} else if (Len(value) > name.MaxLength) {
										retval.Errors.Add("The value '" + value + "' for '" + name.name + "' must a maximum of " + name.MinLength + " characters." + name.Notes);
									}
									if (name.RegEx != "") {
										if (!Regex.IsMatch(value, name.RegEx)) {
											retval.Errors.Add("The value '" + value + "' for '" + name.name + "' fails the Regular Expression " + name.RegEx + " " + name.Notes);
										}
									}
								}
							}
						}
					}

					if (NVPs.ContainsKey("base")) {
						if (NVPs("base") != "") {
							if (!iq.i_SKU.ContainsKey(NVPs("Base"))) {
								retval.Errors.Add(NVPs("Base") + " is not a valid HP part Number");
							}
						}
					}

					if (retval.Errors.Count == 0) {
						string token = makeToken();
						int tokenID = WriteToken(token, channel);

						try {
							WriteValues(tokenID, NVPs, names);
							retval.Value = token;
						} catch (System.Exception ex) {
							retval.Errors.Add("Error writing values (field overflow ?) " + ex.Message.ToString);
							if (!ex.InnerException == null) {
								retval.Errors.Add(ex.InnerException.ToString);
							}
						}
					}
				}
			}


		} catch (Exception ex) {
			retval.Errors.Add(ex.Message + " " + ex.StackTrace);

		}

		return retval;

	}

	public List<clsName> IOneTimeToken.Help()
	{

		Help = new List<clsName>();
		Dictionary<string, clsName> dicNames = LoadNames();

		foreach ( name in dicNames.Values) {
			Help.Add(name);
		}

	}

	///<summary>Writes the suplied random token to the databse</summary>
	/// <returns>The Integer ID of the token</returns>

	private int WriteToken(string token, clsChannel Channel)
	{

		WriteToken = da.DBExecutesql("INSERT INTO GK.Token (Token,Timestamp,FK_Channel_ID_Host) VALUES (" + da.SqlEncode(token) + ",getdate()," + Channel.ID + " );", true);

	}


	private void WriteValues(int TokenID, Dictionary<string, string> NVPs, Dictionary<string, clsName> names)
	{
		//we don't really need to persist the vaues - or the tokens

		SqlClient.SqlConnection con = da.OpenDatabase();

		DataTable dt = da.MakeWriteCacheFor(con, "gk.value");

		DataRow dr;

		foreach ( k in NVPs.Keys) {
			//They may have submitted some unknown values (UserID)
			if (names.ContainsKey(k)) {
				dr = dt.NewRow;
				dt.Rows.Add(dr);
				dr.Item("FK_Name_id") = names(k).ID;
				dr.Item("FK_Token_id") = TokenID;
				dr.Item("Value") = NVPs(k);
			}
		}

		da.BulkWrite(con, dt, "gk.value");
		con.Close();

	}

	private string makeToken()
	{

		//The RND() function is not cryptographically strong - It is possible to work out the seed (system timestamp) for a given set of pseudorandom numbers.. then by 'synchronsing watches' with the server - one could generate many tokens - and have a small chance of predicting (and using) the next token generated.
		//whilst we could obscure things with XORs or Hashes or different seeds ... it's better to use a 'real' random key generated from the Cryptographic Service Provider.

		//hence 
		RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
		byte[] bytes = new byte[18];
		rngCsp.GetBytes(bytes);

		for (i = 0; i <= 19; i++) {
			bytes(i) = 65 + bytes(i) / 255 * 25;

		}
		makeToken = Encoding.ASCII.GetString(bytes);

	}

	public clsGKAccount IOneTimeToken.GetUserDetails(string HostCode, string Email, string Priceband)
	{

		//for synnex their USERID is stored in the account.priceband (formerly HostAccountNum)
		GetUserDetails = null;
		object j = from a in iq.Accounts.Valueswhere a.SellerChannel.Code == HostCode & (a.User.Email == Email | (a.Priceband.text == Priceband & Priceband != ""));


		if (j.Any) {
			clsAccount ac = j.First;
			 // ERROR: Not supported in C#: WithStatement

		}

	}

}


