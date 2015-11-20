using dataAccess;
using System.Globalization;
using System.Collections.Concurrent;
using System.Threading;

[Serializable()]
public class clsChannel : i_Editable
{

	//NB:-ALL channels are clones ! - they have an IsCloneOf member - it's just that 'non clones' are clones of themselves.
	//This way we can *always* present pricing as that of the channels clone (wether it's a clone or not)

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string Name {
		get { return m_Name; }
		set { m_Name = Value; }
	}
	private string m_Name;
	private string BusinessName {
		get { return m_BusinessName; }
		set { m_BusinessName = Value; }
	}
	private string m_BusinessName;
	private string Address {
		get { return m_Address; }
		set { m_Address = Value; }
	}
	private string m_Address;
	private string Code {
		get { return m_Code; }
		set { m_Code = Value; }
	}
	private string m_Code;
	//IQ1 HostID
	//Public ChannelAcID As String ' buyer account number
	private Dictionary<int, clsUser> Users {
		get { return m_Users; }
		set { m_Users = Value; }
	}
	private Dictionary<int, clsUser> m_Users;
	//users (who work at this channel - and are generally buyers at another channel, or sales agents at this one)
	private clsRegion Region {
		get { return m_Region; }
		set { m_Region = Value; }
	}
	private clsRegion m_Region;
	//Country As clsCountry - previously country - but countries *are* now regions
	private Dictionary<int, clsTeam> Teams {
		get { return m_Teams; }
		set { m_Teams = Value; }
	}
	private Dictionary<int, clsTeam> m_Teams;
	private Dictionary<int, clsAccount> CustomerAccounts {
		get { return m_CustomerAccounts; }
		set { m_CustomerAccounts = Value; }
	}
	private Dictionary<int, clsAccount> m_CustomerAccounts;
	//the people this channel sells to
	private string WebToken {
		get { return m_WebToken; }
		set { m_WebToken = Value; }
	}
	private string m_WebToken;
	//Used as a unique (and 'unguessable') token (instead of a username/password) for webservice operations
	//                              buyer                   sector          
	private Dictionary<clsChannel, Dictionary<clsSector, clsMargin>> Margin {
		get { return m_Margin; }
		set { m_Margin = Value; }
	}
	private Dictionary<clsChannel, Dictionary<clsSector, clsMargin>> m_Margin;
	private clsChannel IsCloneOf {
		get { return m_IsCloneOf; }
		set { m_IsCloneOf = Value; }
	}
	private clsChannel m_IsCloneOf;
	private clsChannel Parent {
		get { return m_Parent; }
		set { m_Parent = Value; }
	}
	private clsChannel m_Parent;
	//Channels are placed in a heirarchy for organisational/display puproses.. much like threads
	private Dictionary<int, clsChannel> Children {
		get { return m_Children; }
		set { m_Children = Value; }
	}
	private Dictionary<int, clsChannel> m_Children;
	//we use a dictionary - rather than a list, so that indiviual elements can be addressed for editing

	private nullableString pic1 {
		get { return m_pic1; }
		set { m_pic1 = Value; }
	}
	private nullableString m_pic1;
	private nullableString pic2 {
		get { return m_pic2; }
		set { m_pic2 = Value; }
	}
	private nullableString m_pic2;
	private nullableString URL {
		get { return m_URL; }
		set { m_URL = Value; }
	}
	private nullableString m_URL;

	private string TreePath {
		get { return m_TreePath; }
		set { m_TreePath = Value; }
	}
	private string m_TreePath;
	private string Focus {
		get { return m_Focus; }
		set { m_Focus = Value; }
	}
	private string m_Focus;
	//Recta,smartbuy etc. (intinital filter against the 'Focus' Attributte of products. (can be a CD list)  'iq.dbo.countries.hpreceta
	private List<string> Domains {
		get { return m_Domains; }
		set { m_Domains = Value; }
	}
	private List<string> m_Domains;
	private Dictionary<int, clsCampaign> Campaigns {
		get { return m_Campaigns; }
		set { m_Campaigns = Value; }
	}
	private Dictionary<int, clsCampaign> m_Campaigns;
	private float marginMin {
		get { return m_marginMin; }
		set { m_marginMin = Value; }
	}
	private float m_marginMin;
	//Most negative permissable margin (negative margin is reducing the cost)
	private float marginMax {
		get { return m_marginMax; }
		set { m_marginMax = Value; }
	}
	private float m_marginMax;
	//largest allowable margin (markup)
	private string marginType {
		get { return m_marginType; }
		set { m_marginType = Value; }
	}
	private string m_marginType;
	//R' or 'C' for  Retained or 'CostPlus'
	private string Legal {
		get { return m_Legal; }
		set { m_Legal = Value; }
	}
	private string m_Legal;
	//host specific terms and conditions 
	private string SchemeOverride {
		get { return m_SchemeOverride; }
		set { m_SchemeOverride = Value; }
	}
	private string m_SchemeOverride;
	//Host specific loyalty points scheme codes (comma delimited list) - having an entry here will override the usual (regionalised) Loyalty schems
	private clsCurrency DefaultCurrency {
		get { return m_DefaultCurrency; }
		set { m_DefaultCurrency = Value; }
	}
	private clsCurrency m_DefaultCurrency;
	private bool Universal {
		get { return m_Universal; }
		set { m_Universal = Value; }
	}
	private bool m_Universal;
	private string orderEmail {
		get { return m_orderEmail; }
		set { m_orderEmail = Value; }
	}
	private string m_orderEmail;
	private string basketMode {
		get { return m_basketMode; }
		set { m_basketMode = Value; }
	}
	private string m_basketMode;
	private string basketURL {
		get { return m_basketURL; }
		set { m_basketURL = Value; }
	}
	private string m_basketURL;

	//    Public Variants As Dictionary(Of clsProduct, List(Of clsVariant))

	private Int16 priceConfig {
		get { return m_priceConfig; }
		set { m_priceConfig = Value; }
	}
	private Int16 m_priceConfig;
	//eger ' contains a set of bitwise flags controlling which prices (and therefore products) are displayed - Per SELLER channel (at the moment - could be moved to Buyer channel, or even account without great difficulty)


	//Public variantsLoaded As Integer 'used on the seller channel - to indicate that the variants (containing host partnumbers, and indexing prices) are loaded
	public DateTime variantsLoadedAt;
		//used on sellerchannel - to indicate how many prices have been loaded for each priceband
	public Dictionary<clsPriceBand, int> pricesLoadedFor;
		//Specific to the HP channel - and used to know whether to load list prices for the users region at signin
	public Dictionary<clsRegion, int> listPricesLoadedFor;

	public bool stockLoaded;

	//DistiSKU|wharehousecode>clsVariant - compound key (NB warehouse will *often* be blank
		//use the FindVariant public helper function to get to his
	private Dictionary<string, clsVariant> i_variantCK;
	//Private SKUs As Dictionary(Of clsProduct, List(Of clsVariant)) 'the variant(s) contains the DistsSKU(s) (or blank if they don't have one ..ie they use the HP partNumber

	private string i_Editable.DisplayName(clsLanguage language)
	{
		DisplayName = Name + " (" + Region.Name.text(language) + ")";
	}


	/// <summary>
	/// If the channel has no teams, makes one (an Everyone) .. and assigns all existintg users to it
	/// </summary>
	/// <remarks></remarks>
	public void fixteams(ref List<string> errormessages)
	{

		if ((this.Teams.Values.Count < 1)) {
			clsTeam newTeam = new clsTeam(this, "Everyone");
			clsUser existingUser = new clsUser();

			foreach ( existingUser in this.Users.Values) {
				//If existingUser.Accounts.Count = 0 Then
				//    Dim newAccount As clsAccount = New clsAccount(existingUser, )
				//    existingUser.Accounts.Add(agentaccount.SellerChannel.ID, agentaccount)
				//    existingUser.update()
				//End If

				object userAccounts = from j in existingUser.Accounts.Valueswhere object.ReferenceEquals(j.SellerChannel, this);
				if (userAccounts.Any) {
					userAccounts.First.Team = newTeam;
					userAccounts.First.Team.update(errormessages);
				}
			}
		}
	}

	public int countVariants()
	{
		return this.i_variantCK.Count;
	}

	public object i_Editable.Insert(ref List<string> errorMessages)
	{

		clsChannel achannel = new clsChannel(this.Parent, this.Name, this.BusinessName, this.Address, this.Code, this.Region, this.pic1, this.pic2, this.URL, this.priceConfig,
		this.TreePath, this.Focus, this.marginMin, this.marginMax, this.marginType, this.SchemeOverride, this.Legal, null, this.Universal, this.orderEmail,
		this.basketMode, this.basketURL);
		return achannel;

	}

	public bool deIndexVariant(clsVariant v, List<string> errorMessages)
	{

		if (i_variantCK.ContainsKey(v.CK)) {
			i_variantCK.Remove(v.CK);
			return true;
		} else {
			errorMessages.Add("Could not locate " + v.CK + "");
			return false;
		}

	}

	public void indexVariant(clsVariant v)
	{
		if (!this.i_variantCK.ContainsKey(v.CK))
			this.i_variantCK.Add(v.CK, v);
	}


	/// <summary>Returns a specific variant (matching DistiSku) - Variants effectively join, sellers, products and warehouses - allowing us to store different price and stock per variant/buyer. </summary>
	public bool findVariant(string DistiSKU, string warehouse, ref clsStockPriceSvc.clsResult result, ref IQ.clsVariant SKUvariant)
	{

		//mfsrSKU is the hostManufacturer part number and may contain a #

		findVariant = false;
		result = null;
		//Any error msg
		// Dim product As clsProduct

		//mfrsku = Split(mfrsku, "#")(0)  'Not sure about this - all IQ2 Mfrpartnums have no #
		// Dim stub As String = Split(MfrSku, "#")(0)

		string ck = DistiSKU + "|" + warehouse;
		if (!this.i_variantCK.ContainsKey(ck)) {
			result = new clsStockPriceSvc.clsResult(false, this.i_variantCK.Count + " variants - none with the host SKU|warehouse combo  " + ck + " use AddVariant to add new variants.", 56);
		} else {
			result = new clsStockPriceSvc.clsResult(true, "OK", 0);
			SKUvariant = this.i_variantCK(ck);
			findVariant = true;
		}

	}


	/// <summary>Maintains a Distis portfolio - Calls the allProducts method on wsConsumer - which proxies the Distis AllProducts Method / OR returns variants based on the pricing database</summary>
	/// <remarks></remarks>
	public string freshenVariants(List<string> errorMessages)
	{

		//Note this is called *after* the variants are loaded
		//called on the seller channel

		wsconsumer.I_UniTranClient cl = new wsconsumer.I_UniTranClient();
		cl.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(10);
		cl.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(10);


		try {
			string[] SKUlist = cl.AllProducts(this.Code);


			if (SKUlist.Count < 10) {
				errorMessages.Add("Crazy small feed ! - nothing freshened");

				return "Refresh failed";


			} else {
				HashSet<string> FeedCKs;
				//we construct a list of all the CK's in the response (to check for dupes) - Note a HashSet is much faster than a list for Contians ops.
				FeedCKs = new HashSet<string>();
				int added = 0;
				int deleted = 0;
				int existed = 0;

				SqlClient.SqlConnection con = da.OpenDatabase;

				int nvid = 0;
				DataTable vwc = da.MakeWriteCacheFor(con, "variant", nvid, true);

				//errorMessages.Clear()
				//HP renew parts don't have list prices !! - really not sure what the fix is

				FeedCKs.Clear();

				foreach ( line in SKUlist) {
					if (Trim(line) == "") {
						errorMessages.Add("blank line in AllProducts response");
					}

					//MrfSKU|DistisSKu|Warehouse
					[] bits = Split(line, "|");
					object mfrSKU = bits(0);
					object distisku = bits(1);

					if (bits.Count > 3) {
						errorMessages.Add(line + " in AllProducts response contained too many segments.");
					} else {
						string warehouse = "";
						if (bits.Count == 3)
							warehouse = bits(3);

						string ck = distisku + "|" + warehouse;
						if (FeedCKs.Contains(ck)) {
							//Duplicate (By DisitSKU|Warwhouse)
							errorMessages.Add("Duplicated line " + line);
						} else {
							FeedCKs.Add(ck);
							//warehouse
							if (this.i_variantCK.ContainsKey(ck)) {
								//all good - the variant exists
								existed += 1;
							} else {
								//Need to create a new variant
								clsProduct product;
								if (iq.i_SKU.ContainsKey(mfrSKU)) {
									product = iq.i_SKU(mfrSKU);
									//this could be *much* faster with a 'writecahe' (but additions should 
									clsVariant newVariant = new clsVariant("", product, this, distisku, "", warehouse, "", null, false, vwc,
									nvid);
									added += 1;
								} else {
									if (errorMessages.Count < 100) {
										errorMessages.Add("Unrecognised part " + mfrSKU + " could not create variant");
									}
								}
							}
						}
					}
				}

				da.BulkWrite(con, vwc, "variant", batchsize: 1000, writeIDs: true);
				con.Close();

				foreach ( ck in this.i_variantCK.Keys.ToArray) {
					//Pauls fake parts for the Unhosted instance (so he can add anything to a basket)
					if (!ck.Contains("FAKE")) {
						if (!FeedCKs.Contains(ck) && !this.i_variantCK(ck).DistiSku.StartsWith("###") && !this.i_variantCK(ck).Deleted) {
							//variant no longer in the feed - 'delete' it
							deleted += 1;
							this.i_variantCK(ck).Delete(errorMessages);
							//NOTE - We NEVER actually delete variants - becuase they're referenced by quote items -the are flagged as deleted - and removed from the indicies
						}
					}
				}

				return this.Name + "(" + this.Code + ") - FreshenVariants - Existed:" + existed + " Added:" + added + " Deleted:" + deleted;

			}

		} catch (System.Exception ex) {
			ErrorLog.Add(ex);
		}


	}


	public void i_Editable.delete(ref List<string> errorMessages)
	{
		errorMessages.Add("Delete is not yet Implemented on the Channel object");

	}

	public string LoadStock()
	{


		List<string> errorMessages = new List<string>();
		double ts = Stopwatch.GetTimestamp;

		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader r;

		string dzero = da.UniversalDate((System.DateTime)"01/01/2000");

		object sql;
		sql = "SELECT stock.ID,fk_variant_id,v.fk_product_id,v.fk_channel_id_seller,quantity,Arrival,datestamp,iscurrent ";
		sql += "FROM [stock] ";
		sql += "JOIN [variant] AS v ON v.id=fk_variant_id  ";
		sql += " WHERE fk_channel_id_seller=" + this.ID + " AND arrival = " + dzero + " AND isCurrent = 1 Or Arrival > " + da.UniversalDate(DateAdd(DateInterval.Day, -30, Now));

		r = da.DBExecuteReader(con, sql);

		int count;
		clsProduct Product;
		clsChannel Seller;
		clsstock stock;
		clsVariant SKUvariant;
		int duds = 0;

		if (r.HasRows) {

			while (r.Read) {
				Seller = iq.Channels(r.Item("fk_channel_id_seller"));

				int pid = r.Item("fk_product_id");
				if (!iq.Products.ContainsKey(pid)) {
					if (duds < 10) {
						Logit(this.DisplayName(English) + " LoadStock referenced product " + pid + " which is not in the OM of " + iq.Products.Count + " products");
						duds += 1;
					}


				} else {
					if (iq.Products.ContainsKey(pid)) {
						Product = iq.Products(pid);
					} else {
						Product = iq.REMAPS(pid);
					}

					int vid = r.Item("fk_variant_id");
					if (!iq.Variants.ContainsKey(vid)) {
					//Logit(Me.DisplayName(English) & " LoadStock referenced variant " & vid & " which is not in the OM")
					} else {
						SKUvariant = iq.Variants(vid);
						//just creating the stock adds it to the product AND iq.stock (the 'flat' list used for import
						stock = new clsstock(r.Item("ID"), SKUvariant, r.Item("quantity"), r.Item("arrival"), r.Item("datestamp"), r.Item("isCurrent"), errorMessages);
						count += 1;
					}
				}
			}
		}
		r.Close();
		con.Close();
		con.Dispose();

		this.stockLoaded = true;

		object v = "Loaded " + count + " Stock records in " + TimeSince(ts) + "<br/>";

		foreach ( e in errorMessages) {
			v += "<p>" + e + "</p>";
			if (Len(v) > 1000)
				break; // TODO: might not be correct. Was : Exit For
		}

		return v;

	}


	/// <summary>Loads (from the DB) the variants for this channel - and 'freshens' them via a webservice call if necessary</summary>
	/// <param name="errormessages"></param>
	/// <param name="maxAgeHrs">Variants will not be re-loaded if they are 'fresher' than this</param>
	/// <returns></returns>
	/// <remarks></remarks>
	public string LoadVariants(ref List<string> errormessages, float maxAgeHrs)
	{

		//Called on the sellerchannel (when an account is selected at login) - to load the SKUvariants - (loosely equivilent to IQ1 tbhostPartnums)
		//Doing this 'just in time' (per channel) like this saves around 400MB and 5 seconds from the OBject model and its startup time

		float ts = Stopwatch.GetTimestamp;
		int count = 0;
		int dupes = 0;
		int bad = 0;


		if (this.variantsLoadedAt == null) {
			//Read them from the IQ2 Database

			clsChannel seller = this;
			//just for clarity
			SqlClient.SqlConnection con = da.OpenDatabase;
			SqlClient.SqlDataReader r;


			//NB: - we don't load deleted variants
			r = da.DBExecuteReader(con, "SELECT v.id,code,distiSKU,fk_channel_id_seller,fk_product_id,displaytext,warehouse,localisation,fk_region_id,v.deleted  " + "FROM [Variant] v inner join Product p on p.id = fk_product_id WHERE p.deleted = 0 and  fk_channel_id_seller=" + this.ID);

			clsVariant v;
			clsProduct product;
			clsRegion region;
			string warehouse;
			string distiSKU;

			this.i_variantCK.Clear();

			while (r.Read) {
				//iq.Channels(r.Item("fk_channel_id")).addSKU(iq.Products(r.Item("fk_product_id")), iq.Variants(r.Item("fk_variant_id")), r.Item("channelSKU"))

				distiSKU = r.Item("distiSKU");

				int pid = r.Item("fk_product_id");
				if (iq.Products.ContainsKey(pid)) {
					product = iq.Products(pid);
				} else {
					bad += 1;
					continue;
					product = iq.REMAPS(pid);
				}

				//    seller = iq.Channels(r.Item("fk_channel_id_seller")) this IS ME
				region = null;
				if (!object.ReferenceEquals(r.Item("fk_region_id"), DBNull.Value))
					region = iq.Regions(r.Item("fk_region_id"));
				warehouse = r.Item("warehouse");


				if (region != null && region.Code == "CO") {
					object a = 0;
				}

				if (distiSKU == "") {
					if (errormessages.Count < 10) {
						errormessages.Add("distiSKU was blank for variant " + r.Item("ID").ToString);
					}
				} else {
					if (object.ReferenceEquals(this, HP) && i_variantCK.ContainsKey(distiSKU + "|" + region.Code)) {
						if (errormessages.Count < 10) {
							errormessages.Add("Duplicate HP variant " + distiSKU + "|" + region.Code);
						}

						dupes += 1;
					} else if (i_variantCK.ContainsKey(distiSKU + "|" + warehouse)) {
						v = new clsVariant((int)r.Item("id"), r.Item("code"), product, this, distiSKU, r.Item("displaytext"), warehouse, r.Item("Localisation"), region, r.Item("deleted"),
						false);
						if (errormessages.Count < 10) {
							errormessages.Add("Duplicate variant " + distiSKU + "|" + warehouse + " for " + this.Code + "(" + this.Name + ")");
						}
						dupes += 1;
					} else {
						v = new clsVariant((int)r.Item("id"), r.Item("code"), product, this, distiSKU, r.Item("displaytext"), warehouse, r.Item("Localisation"), region, r.Item("deleted"),
						true);
						count += 1;
					}
				}
			}

			r.Close();
			con.Close();
			con.Dispose();

			this.variantsLoadedAt = DateAdd(DateInterval.Hour, -100, Now);
			//force a freshen

		}

		if ((Math.Abs(DateDiff(DateInterval.Hour, Now, this.variantsLoadedAt))) > maxAgeHrs) {
			//Freshen' them from the webservice (this may add and delete variants!)

			this.variantsLoadedAt = Now;
			//we set this 'early' so that it's not called twice for multiple users logging int simultaneousness
			object j = this.freshenVariants(errormessages);

		}

		return "Loaded " + count + " variants SKUs in " + TimeSince(ts) + " skipped " + bad + " bad (deleted products)<br/>";

	}


	public string LoadPrices(clsPriceBand PriceBand, ref List<string> errorMessages, clsRegion region = null)
	{
		// If Environment.MachineName = "LINGM-LAPTOP" Then Exit Function
		//Called on the sellerchannel, passing the buyerchannel to load the prices for/of the buyerchannel

		float ts = Stopwatch.GetTimestamp;

		int already = 0;

		clsChannel Seller = this;

		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader r;

		//r = da.dbexecuteReader(con, "SELECT Id,fk_product_id,fk_variant_id,fk_channel_id_seller,fk_channel_id_buyer,price,fk_currency_id,datestamp,source from [Price]")
		object sql;

		sql = "SELECT Price.Id as priceID,V.FK_PRODUCT_ID,fk_variant_id,v.fk_channel_id_seller,priceband,price,fk_currency_id,datestamp,source ";
		sql += "FROM [Price]";
		sql += "JOIN [Variant] AS v on v.id= fk_variant_id ";
		sql += "JOIN [product] AS p on p.id= v.fk_product_id ";
		sql += "WHERE fk_channel_id_seller=" + Seller.ID + " AND priceband='" + PriceBand.text + "'";
		sql += "AND p.deleted = 0 and v.deleted = 0";

		if (region != null) {
			sql += " AND fk_region_id=" + region.ID;
		}

		r = da.DBExecuteReader(con, sql);

		int count;
		clsPrice aPrice;
		clsProduct Product;
		//Dim buyer As clsChannel = buyerchannel
		clsCurrency Currency;
		decimal price;
		clsVariant SKUvariant;

		if (r.HasRows) {

			while (r.Read) {
				int vid = r.Item("fk_variant_id");

				if (iq.Variants.ContainsKey(vid)) {
					SKUvariant = iq.Variants(vid);

					//check its not already loaded
					if (!SKUvariant.prices.ContainsKey(r.Item("PriceID"))) {

						int pid = r.Item("fk_product_id");

						//SHOULD mbe removed (but without it it crashes!)
						if (iq.Products.ContainsKey(pid)) {
							Product = iq.Products(pid);
						} else {
							Product = iq.REMAPS(pid);
						}

						Currency = iq.Currencies(r.Item("fk_currency_id"));
						price = r.Item("price");

						//will add the price into both the master price list - and into the product.price(seller)(buyer)(currency)
						DateTime datestamp;
						if (IsDBNull(r.Item("datestamp"))) {
							datestamp = Now;
						} else {
							datestamp = r.Item("datestamp");
						}

						//WE DONT WANT ZERO PRICES (for now)
						if (price != 0) {
							if (r.Item("priceid") < 1)
								errorMessages.Add("a price has an id <1");
							aPrice = new clsPrice(r.Item("priceid"), SKUvariant, iq.getPriceBand(r.Item("Priceband")), price, Currency, datestamp, r.Item("source"));
						}

						count += 1;

					} else {
						already += 1;
					}
				} else {
					//missing variant ??
				}

			}
		}
		r.Close();
		r.Close();
		con.Close();
		con.Dispose();

		if (!this.pricesLoadedFor.ContainsKey(PriceBand))
			pricesLoadedFor.Add(PriceBand, 0);
		this.pricesLoadedFor(PriceBand) = (int)count + already;

		//only list prices are region specific... we track how many were loaded to know wther we need to load them for the users country at logon
		if (region != null) {
			if (!this.listPricesLoadedFor.ContainsKey(region))
				this.listPricesLoadedFor.Add(region, 0);
			this.listPricesLoadedFor(region) = count + already;
		}

		return "Loaded " + count + " Prices in " + TimeSince(ts) + " " + already + " were already loaded<br/>";

	}



	public void i_Editable.Update(ref List<string> errorMessages)
	{
		object sql;
		sql = "UPDATE CHANNEL SET ";
		sql += "FK_Channel_id_cloneof=" + this.IsCloneOf.ID + ",";
		if (this.Parent == null) {
			sql += "FK_Channel_id_parent=null,";
		} else {
			sql += "FK_Channel_id_parent=" + this.Parent.ID + ",";
		}

		sql += "Name=" + da.SqlEncode(this.Name) + ",";
		sql += "Address=" + da.SqlEncode(this.Address) + ",";
		sql += "FK_Region_ID=" + this.Region.ID + ",";
		sql += "webtoken=" + da.SqlEncode(this.WebToken) + ",";
		sql += "code=" + da.SqlEncode(this.Code) + ",";
		sql += "pic1=" + this.pic1.sqlValue + ",";
		sql += "pic2=" + this.pic2.sqlValue + ",";
		sql += "URL=" + this.URL.sqlValue + ",";
		sql += "priceconfig=" + this.priceConfig + ",";
		sql += "focus=" + da.SqlEncode(this.Focus) + ",";
		sql += "treepath=" + da.SqlEncode(this.TreePath) + ",";
		sql += "marginMin=" + this.marginMin + ",";
		sql += "marginMax=" + this.marginMax + ",";
		sql += "marginType=" + da.SqlEncode(this.marginType) + ",";
		sql += "schemeOverride=" + da.SqlEncode(this.SchemeOverride) + ",";
		sql += "legal=" + da.SqlEncode(this.Legal) + ",";
		sql += "fk_currency_id_default=";
		if (this.DefaultCurrency == null) {
			sql += "null,";
		} else {
			sql += this.DefaultCurrency.ID + ",";
		}
		sql += "universal=" + IIf(this.Universal, "1", "0") + ",";
		sql += "orderEmail=" + da.SqlEncode(this.orderEmail) + ",";
		sql += "basketMode=" + da.SqlEncode(this.basketMode) + ",";
		sql += "basketURL=" + da.SqlEncode(this.basketURL);

		sql += " WHERE ID = " + this.ID;

		da.DBExecutesql(sql);

	}

	//Public Function addSKU(Product As clsProduct, SKUvariant As clsVariant)

	//    If Not Me.SKUs.ContainsKey(Product) Then Me.SKUs.Add(Product, New List(Of clsVariant))
	//    Me.SKUs(Product).Add(SKUvariant)

	//End Function

	//Public Function ChannelSKUs(product) As Dictionary(Of clsVariant, String)

	//    'returns a dictionary of all the variants>DistiSKUs - for the sepcifed product

	//    ChannelSKUs = New Dictionary(Of clsVariant, String) 'return an empty dictionary by default
	//    If SKUs.ContainsKey(product) Then
	//        Return SKUs(product)
	//    End If

	//End Function

	//Public Function ChannelSKU(product As clsProduct, skuvariant As clsVariant) As String

	//    ChannelSKU = ""
	//    If SKUs.ContainsKey(product) Then
	//        If SKUs(product).ContainsKey(skuvariant) Then
	//            Return SKUs(product)(skuvariant)
	//        End If
	//    End If
	//End Function

	public clsChannel(clsChannel Parent, string Name, string BusinessName, string Address, string code, clsRegion Region, nullableString pic1, nullableString pic2, nullableString url, int priceConfig,
	string treepath, string focus, float marginMin, float MarginMax, string MarginType, string SchemeOverride, string Legal, clsCurrency DefaultCurrency, bool universal, string orderEmail,

	string basketMode, string basketURL, DataTable writecache = null, ref int nextID = -1)
	{
		//EVERY channel is created AS A CLONE, AND PARENT OF ITSELF
		//Those that are actually clones of something else are subsequenty UPDATED

		Guid aguid = new Guid();
		this.WebToken = aguid.ToString("D");

		//This bit is important to understand - where isCloneOf is passed as nothing .. it will insert a row that refers to itself

		string pid;
		if (Parent == null)
			pid = "null";
		else
			pid = Parent.ID.ToString;

		this.Name = Name;
		this.BusinessName = BusinessName;
		this.Address = Address;
		this.IsCloneOf = this;

		this.Code = Trim(code);
		this.Region = Region;

		if ((DefaultCurrency == null) && (!iq.DefaultCurrencies == null) && (iq.DefaultCurrencies.ContainsKey("USD"))) {
			DefaultCurrency = iq.DefaultCurrencies("USD");
		}

		this.Users = new Dictionary<int, clsUser>();
		this.Teams = new Dictionary<int, clsTeam>();

		this.pic1 = pic1;
		this.pic2 = pic2;
		this.URL = url;
		this.priceConfig = priceConfig;
		this.Children = new Dictionary<int, clsChannel>();
		this.TreePath = treepath;
		this.Focus = focus;
		this.Domains = new List<string>();
		this.marginMax = MarginMax;
		//These are the margins applied via buttons in the basket
		this.marginMin = marginMin;
		this.marginType = MarginType;
		this.SchemeOverride = SchemeOverride;
		this.Legal = Legal;
		this.DefaultCurrency = DefaultCurrency;
		this.Universal = universal;
		this.orderEmail = orderEmail;
		this.basketMode = basketMode;
		this.basketURL = basketURL;


		if (writecache == null) {
			object sql;
			sql = "INSERT INTO Channel (fk_channel_id_parent,Name,BusinessName,Address,fk_region_id,webtoken,code,pic1,pic2,url,FK_Channel_ID_CloneOf,priceconfig,treepath,focus,marginMax,marginMin,marginType,SchemeOverride,legal,FK_Currency_ID_Default,Universal,orderEmail,basketMode,basketURL) ";
			sql += "VALUES (" + pid + "," + da.SqlEncode(Name) + "," + da.SqlEncode(BusinessName) + "," + da.SqlEncode(Address) + "," + Region.ID + ",'" + this.WebToken + "'," + da.SqlEncode(code) + "," + pic1.sqlValue + "," + pic2.sqlValue + "," + url.sqlValue + ",";
			sql += "IDENT_CURRENT('Channel')," + priceConfig + "," + da.SqlEncode(treepath) + "," + da.SqlEncode("focus") + ",";
			sql += MarginMax + "," + marginMin + "," + da.SqlEncode(MarginType) + "," + da.SqlEncode(SchemeOverride) + "," + da.SqlEncode(Legal) + "," + DefaultCurrency.ID + "," + IIf(universal, "1", "0") + "," + da.SqlEncode(orderEmail) + "," + da.SqlEncode(basketMode) + "," + da.SqlEncode(basketURL);
			sql += ");";
			//<<COID

			this.ID = da.DBExecutesql(sql, true);

		} else {
			if (nextID == -1)
				System.Diagnostics.Debugger.Break();

			System.Data.DataRow row;
			row = writecache.NewRow();

			this.ID = nextID;

			//            row("ID") = Me.ID - there are 'autonumbers'

			row("FK_channel_id_parent") = IIf(pid == "null", DBNull.Value, pid);
			row("Name") = Name;
			row("BusinessName") = BusinessName;
			row("Address") = Address;
			row("fk_region_id") = Region.ID;
			row("webtoken") = this.WebToken;
			row("code") = code;
			row("pic1") = pic1.value;
			row("pic2") = pic2.value;
			row("url") = url.value;
			row("FK_Channel_ID_cloneof") = nextID;
			//  coid - clone of self
			row("priceconfig") = priceConfig;
			row("treepath") = treepath;
			row("focus") = focus;
			row("MarginMax") = MarginMax;
			row("MarginMin") = marginMin;
			row("MarginType") = MarginType;
			row("schemeOverride") = MarginType;
			row("Legal") = Legal;
			row("FK_Currency_ID_Default") = DefaultCurrency.ID;
			row("universal") = universal;
			row("orderemail") = orderEmail;
			row("basketMode") = basketMode;
			row("basketURL") = basketURL;

			nextID += 1;
			writecache.Rows.Add(row);

		}

		iq.Channels.Add(this.ID, this);

		// If Not iq.i_channel_code.ContainsKey(Me.Code) Then  'this shouldn't be needed and imples a problem
		iq.i_channel_code.Add(this.Code, this);
		//   End If

		if (iq.Channels.Count == 1) {
			if (!this.Parent == null)
				System.Diagnostics.Debugger.Break();
			//The root channel should not have a parent
			iq.RootChannel = this;
		}

		if (!this.Parent == null) {
			this.Parent.Children.Add(this.ID, this);
			//add me to my parents children (to create the heirarchy)
		}

		this.CustomerAccounts = new Dictionary<int, clsAccount>();
		this.Campaigns = new Dictionary<int, clsCampaign>();


		this.Margin = new Dictionary<clsChannel, Dictionary<clsSector, clsMargin>>();
		//        SKUs = New Dictionary(Of clsProduct, clsVariant)

		this.pricesLoadedFor = new Dictionary<clsPriceBand, int>();
		//used on sellerchannel - to indicate which buyers prices have been loaded for 
		this.listPricesLoadedFor = new Dictionary<clsRegion, int>();
		this.i_variantCK = new Dictionary<string, clsVariant>();
		//DistiSKU|Warehouse>Variant


	}

	public clsChannel()
	{
		this.ID = -1;
		this.Parent = null;

		this.Users = new Dictionary<int, clsUser>();

		this.Region = iq.Regions.Values(0);
		this.Teams = new Dictionary<int, clsTeam>();
		this.CustomerAccounts = new Dictionary<int, clsAccount>();
		//the people this channel sells to

		Guid aguid = new Guid();
		this.WebToken = aguid.ToString("D");

		//                              buyer                   sector       margin
		Margin = new Dictionary<clsChannel, Dictionary<clsSector, clsMargin>>();

		this.Children = new Dictionary<int, clsChannel>();
		this.pic1 = new nullableString();
		this.pic2 = new nullableString();
		this.URL = new nullableString();
		this.TreePath = "";
		this.Focus = "";
		this.Domains = new List<string>();
		this.marginMax = 0;
		this.marginMin = 0;
		this.marginType = "R";
		this.Legal = "";
		this.SchemeOverride = "";
		this.DefaultCurrency = null;
		this.Universal = false;
		this.orderEmail = "";
		this.basketMode = "";
		this.basketURL = "";


		//SKUs = New Dictionary(Of clsProduct, Dictionary(Of clsVariant, String)) ' This channels 'internal' SKU for each product (they sell) - the first dimension contains a compound key of Product.id+Variant.id

		this.pricesLoadedFor = new Dictionary<clsPriceBand, int>();
		//used on sellerchannel - to indicate how many prices have  been loaded for each buyer
		this.listPricesLoadedFor = new Dictionary<clsRegion, int>();

		this.Campaigns = new Dictionary<int, clsCampaign>();
		this.i_variantCK = new Dictionary<string, clsVariant>();

	}

	public clsChannel(int ID, clsChannel Parent, string Name, string BusinessName, clsChannel CloneOf, string Address, string code, clsRegion region, string webtoken, nullableString pic1,
	nullableString pic2, nullableString url, int priceConfig, string treepath, string focus, float marginMin, float MarginMax, string MarginType, string SchemeOverride, string Legal,

	clsCurrency DefaultCurrency, bool universal, string orderEmail, string basketMode, string basketURL)
	{
		this.ID = ID;
		this.Parent = Parent;
		this.Name = Name;
		this.BusinessName = BusinessName;
		this.IsCloneOf = IsCloneOf;
		this.Address = Address;
		this.Region = region;
		this.Code = Trim(code);
		this.Users = new Dictionary<int, clsUser>();
		this.Teams = new Dictionary<int, clsTeam>();
		this.WebToken = webtoken;
		this.pic1 = pic1;
		this.pic2 = pic2;
		this.URL = url;
		this.priceConfig = priceConfig;
		this.TreePath = treepath;
		this.Focus = focus;
		this.Domains = new List<string>();
		this.marginMax = MarginMax;
		//These are the margins applied via buttons in the basket
		this.marginMin = marginMin;
		this.marginType = MarginType;
		this.SchemeOverride = SchemeOverride;
		this.Legal = Legal;
		this.DefaultCurrency = DefaultCurrency;
		this.Universal = universal;
		this.orderEmail = orderEmail;
		this.basketMode = basketMode;
		this.basketURL = basketURL;

		if (this.IsCloneOf == null)
			this.IsCloneOf = this;
		//!IMPORTANT  (when we're loding channels from the DB they're not yet in the dictionary - so we cant point them to themselves - this work's around that
		this.Children = new Dictionary<int, clsChannel>();

		iq.Channels.Add(this.ID, this);
		iq.i_channel_code.Add(this.Code, this);

		if (iq.Channels.Count == 1) {
			if (!this.Parent == null)
				System.Diagnostics.Debugger.Break();
			//The root channel should not have a parent
			iq.RootChannel = this;
		}

		if (!this.Parent == null) {
			this.Parent.Children.Add(this.ID, this);
			//add me to my parents children (to create the heirarchy)
		}

		CustomerAccounts = new Dictionary<int, clsAccount>();
		Margin = new Dictionary<clsChannel, Dictionary<clsSector, clsMargin>>();
		this.pricesLoadedFor = new Dictionary<clsPriceBand, int>();
		//used on sellerchannel - how many prices loaded for each buyer
		this.listPricesLoadedFor = new Dictionary<clsRegion, int>();

		this.Campaigns = new Dictionary<int, clsCampaign>();
		this.i_variantCK = new Dictionary<string, clsVariant>();

		//moved into Product.variants
		//SKUs = New Dictionary(Of clsProduct, Dictionary(Of clsVariant, String)) ' This channels 'internal' SKU for each product (they sell) - the first dimension contains a compound key of Product.id+Variant.id

	}

	public bool IsUniversal()
	{
		return this.Code.EndsWith("U");
		//i_variant_distisku.Count = 0 'TODO ML have no idea if this is a measure of if they are univeral, need some input on this one...
	}

	/// <summary>
	/// Check if a particular scheme is enabled for this account and region
	/// </summary>
	/// <param name="scheme">The char denoting the scheme to check (F,A, etc)</param>
	/// <returns>...</returns>
	/// <remarks></remarks>
	public bool SchemeEnabled(char scheme)
	{
		switch (scheme) {
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"F":
				if (IsUniversal() && !SchemeOverride.Split(",").Contains("F"))
					return false;
				else
					return true;
		}
	}

	//Private _attributeDataTable As ConcurrentDictionary(Of clsChannel, DataTable)
	//Private _attributeDataTableAge As ConcurrentDictionary(Of clsChannel, DateTime)
	//Private _dataPathsLoaded As ConcurrentDictionary(Of clsChannel, List(Of String)) = New ConcurrentDictionary(Of clsChannel, List(Of String))()
	//Public DataTableMutexLock As ConcurrentDictionary(Of clsChannel, Mutex) = New ConcurrentDictionary(Of clsChannel, Mutex)()

	//Public Function AttributeDataTable(buyerChannel As clsChannel) As DataTable
	//    If _attributeDataTable Is Nothing Then _attributeDataTable = New ConcurrentDictionary(Of clsChannel, DataTable)()
	//    If _attributeDataTableAge Is Nothing Then _attributeDataTableAge = New ConcurrentDictionary(Of clsChannel, DateTime)()
	//    If DataTableMutexLock Is Nothing Then DataTableMutexLock = New ConcurrentDictionary(Of clsChannel, Mutex)()

	//    If Not _attributeDataTable.ContainsKey(buyerChannel) Then

	//        _attributeDataTable.TryAdd(buyerChannel, New DataTable() With {.Locale = New CultureInfo(If(Region.Culture IsNot Nothing, Region.Culture, "En-gb"))})
	//        _attributeDataTableAge.TryAdd(buyerChannel, DateTime.Now)
	//        DataTableMutexLock.TryAdd(buyerChannel, New Mutex())
	//        'Get or create the lid's root data table

	//        Dim col As DataColumn
	//        col = New DataColumn("ID", GetType(Int32))
	//        _attributeDataTable(buyerChannel).Columns.Add(col)

	//        'Populate it with all id's in descendants
	//        Dim c(0) As Object 'ID column in the data table
	//        Dim dv = _attributeDataTable(buyerChannel).AsDataView()
	//        dv.Sort = "[ID]"

	//    End If
	//    AttributeDataTable = _attributeDataTable(buyerChannel)
	//    If DateDiff(DateInterval.Minute, _attributeDataTableAge(buyerChannel), DateTime.Now) > If(ConfigurationManager.AppSettings("MaxDataTableAge") Is Nothing, 15, ConfigurationManager.AppSettings("MaxDataTableAge")) Then RegenerateTable(buyerChannel)
	//End Function

	//Private Sub RegenerateTable(buyerChannel As clsChannel)
	//    _attributeDataTable.TryRemove(buyerChannel, Nothing)
	//    _attributeDataTableAge.TryRemove(buyerChannel, Nothing)
	//    DataTableMutexLock.TryRemove(buyerChannel, Nothing)
	//End Sub

	//Public Function DataPathLoaded(buyerChannel As clsChannel, path As String) As Boolean
	//    Dim dpl = _dataPathsLoaded.GetOrAdd(buyerChannel, New List(Of String))
	//    If dpl.Contains(path) Then Return True Else dpl.Add(path)
	//    Return False
	//End Function

	/// <summary>
	/// Gets whether channel uses BinaryStock 
	/// </summary>
	/// <returns>A boolean value.</returns>
	/// <remarks></remarks>
	public bool BinaryStock()
	{
		if ((this.priceConfig & 16) != 0)
			return true;
		return false;
	}
	private object DecodedPriceConfig()
	{
		if (priceConfig & 8)
			return "Web Service";
		if (priceConfig & 4)
			return "Brice Band/Feed File";
		if (priceConfig & 2)
			return "List Price";
		if (priceConfig & 1)
			return "Show products with no price with '...' (Don't Hide them completely)";
	}

	public string CompoundDisplayName {
		get { return Name + " - " + this.Code; }
	}
}
//clsChannel



