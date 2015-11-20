using dataAccess;
using System.Globalization;
using System.Net;
using System.Xml;

public class gateKeeper : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//The gateKeeper allows a user to be authenticated by the disti and to then bypass our login page
		//We *must not* allow the gatekeeper to be accessed by any agent not known to be authenticated - becuase our system contains confidential, customer specific pricing.
		//Any disti who has implemented iQuote integration *knows* the technical implementation - so obscurity/secrecy gives us no protection (and should never be relied upon)

		//we cannot rely upon the referer - as Internet Security Suites, Proxies and firewalls can potentially strip this information - also it is easily manipulated

		//We need a simple mechanism for the disti to tell us that a user is authenticated so
		//They call our https://webservice GetOneTimeToken(hostID,password) as string - and for a given email address - recieve a one time token
		//they then response.redirecet their client here (gateKeeper.aspx) passing only the token
		//The token they receive will have a short lifespan (the length of the session on their system) and can only be used one
		//The token becomes the *only* thing the client needs to submit as it provides the key to the full set of pre-stored NVP's

		//for legacy clients who may not want to (or have the technology to) call a webservice
		//a backward compatible form does what the IQ1 Gatekeeper did - accepting the NVP's and calling our webservcie 
		//(also prompting for missing, mandatory values)

		// Dim lit As Literal

		// If Not Request.IsSecureConnection Then
		// form1.Controls.Add(ErrorDymo("The connection is not reporting as secure"))
		// Else
		//If Request.UrlReferrer Is Nothing Then
		//    lit = ErrorDymo("The referrer is blank - you may have been directed to this page via a script, bookmark or response.redirect, Please ensure you arrive via a direct POST to this URL from a known referring domain.")
		//    Form.Controls.Add(lit)
		//    lit = New Literal
		//    lit.Text = "Please contact development@channelcentral.net if you are having trouble integrating iQuote 2"
		//    Form.Controls.Add(lit)
		//Else

		List<string> errorMessages = new List<string>();


		if (iq == null) {
			//iq = New clsIQ  'This IS the 'object model'

			Application("IQ") = iq;
			//holding a reference to the (entire) object mode means it will never time out - and we don't need asp.net's sessions
			iq.load(errorMessages);

		}

		if (Request("token") == null) {
			form1.Controls.Add(ErrorDymo("No TOKEN was supplied - you need to call our webservice submitting the data for a session, to receive a 20 character 'one time' token - which should be passed to this page to gain access to iQuote"));

		} else {
			object error = "";
			Dictionary<string, string> nvps;

			if (Request("token") == "none") {
				nvps = NVPsFromRequest(Request, errorMessages);
			} else if (Request("token") == "tdeu") {
				nvps = NVPsFromTechdataEU(Request, errorMessages);
			} else {
				string token = Request("Token");
				nvps = NVPsFromDB(token, errorMessages);
				//The webserives has put them in the DB !

				//Ingram Micro specific (EU) - may need to change for US
				if (errorMessages.Count == 0) {
					if (nvps("host").ToUpper.StartsWith("DIN")) {
						if (!nvps.ContainsKey("cPriceBand")) {
							nvps.Add("cPriceBand", "");
						}

						//this is confusing - but the IngramPriceBands Method was added to the existing feedreader logging webservice.
						IngramPB.I_LoggingClient pbclient = new IngramPB.I_LoggingClient();
						int pb = 0;
						try {
							pb = pbclient.IngramPriceBand(nvps("host"), nvps("cAccountNum"));
						} catch (System.Exception ex) {
							errorMessages.Add(ex.Message);
						}

						if (pb > 0) {
							//*** accounts are located by the users email
							//and the priceband is loaded into the caccount num
							//becuase it will be assigend back into priceband shortly
							nvps("cPriceBand") = pb.ToString;
						//we may have to add cAccountNum back to account - such that it can be passed back with a basket
						} else {
							errorMessages.Add("Unable to retreive price band - result was " + pb);
						}
					}
				}

			}

			// Pick up any MFR - default to HPE
			string mfrCode = "HPE";
			Manufacturer mfr = Manufacturer.Unknown;
			bool mfrSpecified = false;

			// MFR should be the key in use, although the docs also mention MFG
			if ((nvps.ContainsKey("mfr")) && (!string.IsNullOrEmpty(nvps("mfr")))) {
				mfrCode = nvps("mfr");
				mfrSpecified = true;
			} else if ((nvps.ContainsKey("mfg")) && (!string.IsNullOrEmpty(nvps("mfg")))) {
				mfrCode = nvps("mfg");
				mfrSpecified = true;
			}

			if (!string.IsNullOrEmpty(mfrCode)) {
				if (string.Equals(mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase)) {
					mfr = Manufacturer.HPE;
				} else if (string.Equals(mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase)) {
					mfr = Manufacturer.HPI;
				} else {
					mfrCode = string.Empty;
					mfrSpecified = false;
				}
			}

			//'synnex don't submit a host ID (allegedy)
			//If InStr(ref$, "synnex.ca") > 0 Then
			//    nvps.Add("host", "DSYCAN1H1B4")
			//ElseIf InStr(ref, "synnex.com") > 0 Then
			//    nvps.Add("host", "DSYUS94538")
			//End If

			clsUser user = null;
			if (errorMessages.Count) {
				//The token might have already been used, or not be found/valid
				foreach ( msg in errorMessages) {
					form1.Controls.Add(ErrorDymo(msg));
				}
			} else {
				clsChannel hostChannel;
				hostChannel = iq.i_channel_code(nvps("host"));
				//we *know* it will be there (the oneTimeToken webservice created this and already validated)

				clsAccount Account = null;
				if (nvps("token") != hostChannel.WebToken & nvps("token") != "tdeu" & nvps("token") != "none") {
					//the only way this could covcievably happen is if we changed a host ID or token (whlist the system was running)
					form1.Controls.Add(ErrorDymo("Token mismatch (should never happen - the HostID or Token must have changed ?)"));
				} else {
					string uEmail = nvps("uEmail");
					clsChannel reseller;
					//all the user account and the reseller
					object CompanyAccounts = from ca in hostChannel.CustomerAccounts.Valueswhere ca.Priceband.text == nvps("cAccountNum");
					//LINQ  
					if (CompanyAccounts.Any) {
						reseller = CompanyAccounts.First.BuyerChannel;
					} else {
						//There is no (existing) account (for anyone at that company)
						if (!nvps.ContainsKey("cName") | !nvps.ContainsKey("cPCode")) {
							form1.Controls.Add(ErrorDymo("We have no account '" + nvps("cAccountNum") + "' - and you haven't provided a 'cName' AND 'cPcode'  (so we can't create a [channel] to place the [account] on)"));
						} else {
							object buyerID = UCase("R" + Left(nvps("cName"), 2) + nvps("cPCode").Replace(" ", ""));

							if (iq.i_channel_code.ContainsKey(buyerID)) {
								reseller = iq.i_channel_code(buyerID);
							} else {
								reseller = new clsChannel(hostChannel, nvps("cName"), nvps("cName"), "", buyerID, hostChannel.Region, new nullableString(), new nullableString(), new nullableString(Left(uEmail, InStr(uEmail, "@") - 1)), 15,
								"tree.1", "", 0, 0, "R", "", "", hostChannel.DefaultCurrency, false, "",
								"", "");
							}
						}
					}

					//CompanyAccounts is the set of (user) accounts belonging to people at the company with the priceBand 
					//NB: There may be more than one ! (Fred, Bill and Jane May all work at FredsComputers.com)

					//the USER may already exist ('under' another disti) - (even if they don't have an account with this one)
					if (!iq.i_user_email.ContainsKey(uEmail.ToLower)) {
						//Nope - no user (at all) with this email

						//find the users buyer company (who do they work for)
						// Dim buyerCompany As clsChannel = CompanyAccounts.First.BuyerChannel
						string uName__1 = "";
						if (nvps.ContainsKey("uName"))
							uname = nvps("uName");
						string uTel = "";
						if (nvps.ContainsKey("uTel"))
							uTel = nvps("uTel");

						user = new clsUser(reseller, nvps("uEmail"), uName__1, new nullableString(uTel), new nullableString());
					} else {
						user = iq.i_user_email(uEmail);
					}

					List<clsAccount> buyerAccounts = (from ba in hostChannel.CustomerAccounts.Valueswhere ba.User.Email.ToLower == nvps("uEmail").ToLower()).ToList();
					//And ba.mfrCode = (If(mfrSpecified, mfrCode, ba.mfrCode))


					if (!buyerAccounts.Any) {
						//There is no buyeraccount/cusutomeraccount for this user(email)
						object cl = "EN-gb";
						if (hostChannel.Region.Culture.Code != "")
							cl = hostChannel.Region.Culture.Code;
						CultureInfo ci = new CultureInfo(cl);
						clsLanguage language = iq.i_language_Code(UCase(ci.TwoLetterISOLanguageName));
						//ISO 639-1  'GB,DE,FR etc.
						RegionInfo ri = new RegionInfo(cl);
						clsCurrency currency = hostChannel.DefaultCurrency;
						// iq.i_currency_code(ri.ISOCurrencySymbol) 'Three Char ISO 4217 code (GBP, EUR, USD e.t.c.)

						//an explict priceband (in the NVPs) will override a cAccountNum
						string priceband = "";
						if (nvps.ContainsKey("cPriceBand"))
							priceband = nvps("cPriceBand");
						if (priceband == "" && nvps.ContainsKey("cAccountNum"))
							priceband = nvps("cAccountNum");

						Account = new clsAccount(user, "PeeWee3", reseller, { iq.i_role_Code("user") }, null, language, currency, hostChannel, iq.getPriceBand(priceband), hostChannel.Region.Culture,
						mfrCode);
						buyerAccounts.Add(Account);

						//ElseIf buyerAccounts.Count = 1 Then
						//    Account = buyerAccounts.First 'We have an exact match (by email and priceBand - with the Hosts customerAccounts

						//Else
						//    form1.Controls.Add(ErrorDymo("There is more than one user account with that email"))
					}



					if (buyerAccounts.Any) {
						object tid = iq.recordLogin(user, false, user.Email, string.Empty);
						object lid = simpleHash((string)tid);
						iq.updateLogin(tid, lid);

						foreach ( nvp in nvps) {
							iq.sesh(lid, "gk_" + nvp.Key) = nvp.Value;
						}
						iq.sesh(lid, "viaGatekeeper") = true;

						// Mark the token as used
						if (Request("token") != null) {
							if (Request("tom") == null) {
								if (Request("token") != "tdeu" & Request("token") != "none") {
									da.DBExecutesql("UPDATE gk.token SET [usedAt]=getdate() WHERE Token='" + nvps("token") + "';");
								}
							}
						}

						// Add User ID to the session
						iq.sesh(lid, "UserID") = user.ID;

						// Add the account list to the session; Accounts.aspx will handle which one is used
						iq.sesh(lid, "AccountList") = buyerAccounts;

						// Add MFR to the session if specified
						if (mfrSpecified && mfr != Manufacturer.Unknown) {
							iq.sesh(lid, "MFR") = mfr;
						}

						// Add any requested deep link SKU to the session
						if ((nvps.ContainsKey("base")) && (!string.IsNullOrEmpty(nvps("base")))) {
							iq.sesh(lid, "Base") = nvps("base");
						}

						// Add any requested Host (seller channel) to the session
						if (nvps.ContainsKey("host")) {
							iq.sesh(lid, "Host") = Request("host");
						}

						// Accounts.aspx will handle selecting the account to use
						Response.Redirect("accounts.aspx?lid=" + lid);

					} else {
						form1.Controls.Add(ErrorDymo("Could not locate/create account"));

					}
				}
			}
		}

	}


	private Dictionary<string, string> NVPsFromTechdataEU(HttpRequest request, ref List<string> errorMessages)
	{

		NVPsFromTechdataEU = new Dictionary<string, string>();



			//dicCRs(request("corpregionid")))


			//'<?xml version="1.0"?>
			// <usrObjRpl>
			//           <usrObjRplHdr>
			//               <sessionID>13780271351937408A88E92B46C41319991D6FEC84BAD21</sessionID>
			//               <return><text>OK</text><code>0</code></return>
			//           </usrObjRplHdr>
			//          <usrObjRplBdy>
			//               <user><internal>0</internal>
			//<ID>1378027</ID><LoginName>javi</LoginName><status>1</status><name1>Nombre</name1><name2>Apellidos</name2><stdLang>ES</stdLang><country>40 </country>
			//<rights><order>1</order><dropShip>1</dropShip><administrate>1</administrate><inTouch>1</inTouch><LOL>3</LOL>
			//<Menu>1,2,3,5,10,11,12,20,23,24,25,26,27,28,29,65,66,71</Menu></rights>
			//</user>
			//<customer><ID>429475</ID><status>1</status><currencyCode>EUR</currencyCode><name1>INTERSOFT OFIMATICA Y</name1>
			//<address><name1>INTERSOFT OFIMATICA Y</name1><name2>DESARROLLO, SL</name2><street>Sierra de Loja,13 P.La Juaida</street><PCPrefix>04240</PCPrefix><city>VIATOR</city>
			//<postCountry>ES </postCountry></address></customer>
			//<properties>
			//   <value0></value0><value1></value1><value2></value2><value3></value3><value4></value4><value5></value5><value6>1</value6>
			//   <value7>1</value7><value8>0</value8>
			//   <value9>
			//               ev=2|CurrencyCode=EUR|DefPriceCode=|IP=80.35.80.102|ListPriceCode=LP|PriceGroup=C6|RightsList=AddressMaintenance,OrderSubmit,OrderTracking,DropShipment,exactquantity,AIO,LOL,ResellerAdmin,eRMA,TopConfig,pc2000,MarketingCampaigns,UserProfile,TechSelect,TechPartner,OrderReservation,OrderModification,EndUserQuotation|CorpRegionCode=ES|CurrencyCulture=de-DE|DateCulture=en-GB|LangCulture=es-ES|NumberCulture=de-DE|CompleteDelivery=|IsPostCodeCountry=1|lastAccess=2013-07-19 12:32:17
			//   </value9>
			//</properties>
			//</usrObjRplBdy>
			//</usrObjRpl> 


			//       WITH userInfo (status, uName, uEmail, ordering, curr,cAccount,cName,cPCode,cCountry,uInternal) AS (
			//SELECT 'OK' AS Status, FirstName+' '+LastName as uName,uEmail,ordering,curr,cAccount,cName,cPCode,cCountry,uInternal
			//FROM OPENXML(@hdoc, 'usrObjRpl/usrObjRplBdy',3)   
			//WITH (
			//	FirstName varchar(40)'user/name1', 
			//	LastName varchar(40)'user/name2',
			//	uEmail varchar(100)'user/email',
			//	ordering bit 'user/rights/order',
			//	curr char(3) 'customer/currencyCode',
			//	cAccount varchar(10) 'customer/ID',
			//	cName varchar(100) 'customer/name1',
			//	cPCode varchar(10) 'customer/address/PCPrefix',
			//	cCountry char(2) 'customer/address/postCountry',
			//	uInternal bit 'user/internal'
			//	)










			//techdata don't 



		 // ERROR: Not supported in C#: WithStatement



	}

	private Dictionary<string, string> NVPsFromRequest(HttpRequest request, List<string> erromessages)
	{

		//for a tokenless (insecure) implementation

		NVPsFromRequest = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		foreach ( k in request.Form.Keys) {
			NVPsFromRequest.Add(k, request.Form.Item(k));
		}

	}

	/// <summary>Fetches the set of name-value pairs associated with the supplied 'one-time' token</summary>
	/// <returns>A (populated) dictionary of Name>Value - or an ErrorMessage</returns>

	private Dictionary<string, string> NVPsFromDB(string token, ref List<string> errorMessages)
	{
		object sql = "SELECT n.name, t.timestamp,t.usedAt,[fk_Name_ID],value FROM gk.token T ";
		sql += "JOIN gk.value V on fk_token_id=t.id ";
		sql += "JOIN gk.[name] N ON N.ID=V.FK_name_id WHERE t.token=" + da.SqlEncode(Request("token"));

		SqlClient.SqlConnection con = da.OpenDatabase();
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);

		NVPsFromDB = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

		if (!rdr.HasRows) {
			errorMessages.Add("The token does not exist (or is very old and was deleted)");
		} else {
			while (rdr.Read) {
				if (!IsDBNull(rdr.Item("usedat"))) {
					errorMessages.Add("This token has already been used - you may need to 'go out' and 'come in' again ");
					break; // TODO: might not be correct. Was : Exit Do
				}

				if (DateDiff(DateInterval.Minute, Now, rdr.Item("Timestamp")) > 1) {
				}
				NVPsFromDB.Add(rdr.Item("Name"), rdr.Item("Value"));
			}
		}

		rdr.Close();
		con.Close();

	}

}
