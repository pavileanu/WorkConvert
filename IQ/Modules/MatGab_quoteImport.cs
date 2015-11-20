using System.Data;
using dataAccess;

class quoteImport
{

	public string all(SqlClient.SqlConnection con, string HostID = null)
	{



		Dictionary<string, clsBranch> dicSystems = loadDic(con, iq.Branches, "system");
		Dictionary<string, clsAccount> dicAccounts = loadDic(con, iq.Accounts, "account");
		Dictionary<string, clsCurrency> DicRegionCurrency = loadDic(con, iq.Currencies, "coCurr");

		//load *all* known quotes 
		//the string key is the ID-Version (a compund key)

		Dictionary<int, clsQuote> dicOneQUOTE;
		dicOneQUOTE = new Dictionary<int, clsQuote>();
		Dictionary<string, clsQuote> dicAllQuotes = loadDic(con, dicOneQUOTE, "quote");


		//quotes (around 1:40)
		//Makes the basic 'stub' quotes - to which all the quoted sytems, and subsequently options will be attached
		//we import every version (export) of the quotes - but only attach the quoteItems to the last(most up to date) version held in dicquotes

		// Dim importid As Integer
		// importid = ' DBExecutesql("INSERT INTO IMPORTS (timestamp) values(getdate())", True)


		//Dans ID:Final quote version/export
		Dictionary<string, clsQuote> dicLastquotes = new Dictionary<string, clsQuote>();
		try {
			dicLastquotes = loadDic(con, dicAllQuotes, "lastquote");
			//NB: there is no 'master list' of quotes on the object model (as it would be very large) - an accounts quotes are loaded dynamically


		} catch (Exception ex) {
		}


		//somwehere around 10 seconds for 10,000 quotes
		quoteImport.quotes(con, dicLastquotes, dicAllQuotes, dicAccounts, DicRegionCurrency, 0, HostID);

		con.Close();
		con.Dispose();
		con = da.OpenDatabase();

		saveDic(con, dicLastquotes, "lastquote");
		//these are the final quotes (to which we will attach QuoteItems)
		saveDic(con, dicAllQuotes, "lastquote");

		dicAllQuotes = null;
		//get rid of this ASAP (as it's very large!) - we still have dicLastQuotes - the final versions

		//Get all the systems on quotes - create those line items first (because they will be the parents of all the options)
		Dictionary<int, clsQuoteItem> Qsample = new Dictionary<int, clsQuoteItem>();
		Dictionary<string, clsQuoteItem> dicQuoteItems = loadDic(con, Qsample, "QIsystem");
		//the dictionary of Quote system items we've already (previously) imported
		//around 3 seconds
		List<string> errorMessage = new List<string>();
		QuoteSystemItems(con, dicQuoteItems, dicLastquotes, dicSystems, errorMessage);

		saveDic(con, dicQuoteItems, "QIsystem");
		//  Logit("Imported " & added & " quote system items in " & TimeSince(LastMilestone))

		//Important to empty this
		Dictionary<int, clsQuoteItem> qiSample = new Dictionary<int, clsQuoteItem>();
		dicQuoteItems = loadDic(con, qiSample, "QIoption");
		//now loadUp the dictionary with previously imported quote options

		//get all the options - hook them up to the system quoteItems
		//around 7 secs
		QuoteOptionItems(con, dicLastquotes, dicQuoteItems, errorMessage);
		//recalculate/sanity check the totals - and generate the headline descriptions

		//around 11 secs
		Import.updateQuoteDescriptionsAndTotals(con, dicLastquotes);

		return string.Empty;

	}
	public bool QuotesByHostID(SqlClient.SqlConnection con, string hostID, ref List<string> errorMessages)
	{

		//Retrieve all the quotes based on HostID
		SqlClient.SqlDataReader rdr;
		clsAccount anaccount;
		Dictionary<string, clsCurrency> dicRegionCurrencies = new Dictionary<string, clsCurrency>();
		//loadDic(con, iq.Currencies, "coCurr")
		Dictionary<string, clsAccount> dicAccounts = new Dictionary<string, clsAccount>();
		// loadDic(con, iq.Accounts, "account")

		Dictionary<string, clsQuote> dicAllQuotes = new Dictionary<string, clsQuote>();
		//Dim dicLastQuotes As Dictionary(Of String, clsQuote) = New Dictionary(Of String, clsQuote)
		string sqlQuery = string.Empty;
		clsQuote aquote = null;
		DataTable QuoteWriteCache;
		int quotesCount = 0;
		SqlClient.SqlConnection con2 = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
		con.Close();
		con = da.OpenDatabase();
		da.DBExecutesql(con, "IF EXISTS(SELECT * FROM sys.indexes WHERE object_id = object_id('dbo.quote')and  NAME ='ix_quote') DROP INDEX quote.ix_quote;");

		QuoteWriteCache = da.MakeWriteCacheFor(con, "Quote");

		sqlQuery = "select distinct CountryCode,currency from [iq].dbo.countries";
		rdr = da.DBExecuteReader(con2, sqlQuery);
		while (rdr.Read) {
			if (!IsDBNull(rdr.Item("countrycode"))) {
				if (!IsDBNull(rdr.Item("currency"))) {
					dicRegionCurrencies.Add(rdr.Item("countrycode"), iq.i_currency_code(rdr.Item("currency")));
				}
			}
		}
		rdr.Close();

		IEnumerable<clsAccount> allAccounts = from a in iq.Accounts.Valueswhere a.SellerChannel.Code == hostID;
		foreach ( act in allAccounts) {
			dicAccounts.Add(act.User.Email, act);
		}
		//clear out string builder

		// made so that this is the basis of the select statement
		// see function getQuotesfromerver 
		sqlQuery = getQuotesFromServer(hostID, false);


		rdr = da.DBExecuteReader(con2, sqlQuery);

		int oid;
		Dictionary<int, object> dicOPG = new Dictionary<int, object>();
		Dictionary<int, object> dicBundle = new Dictionary<int, object>();
		Dictionary<int, float> dicMargin = new Dictionary<int, float>();
		bool bootstrap = true;
		//we must INSERT the very first quote (it can't be bulk inserted) as we need *something* to point all quotes fk_quote_id_root at
		// If dicAllQuotes.Count > 0 Then bootstrap = False

		if (da.DBSelectFirst("select count(*) from quote where id=1") == 1)
			bootstrap = false;


		int qc = 0;
		//quote count


		while (rdr.Read) {
			if (dicAccounts.ContainsKey(rdr.Item("email"))) {
				anaccount = dicAccounts(rdr.Item("email"));
				//buyer

				if (anaccount.SellerChannel.Region.Code != "AA") {
					clsCurrency currency;

					currency = dicRegionCurrencies(anaccount.SellerChannel.Region.Code);

					if (!object.ReferenceEquals(rdr.Item("totalvalue"), DBNull.Value)) {
						//we make a quote for every version/export
						string importquoteID = rdr.Item("id");

						nullableString quoteName = new nullableString(rdr.Item("listname") + "-IQ1[" + importquoteID + "]");
						decimal totalRebate = (decimal)rdr("totalrebate");

						//                                                                                                 Version \/
						aquote = new clsQuote(anaccount, anaccount, null, rdr.Item("qcreated"), rdr.Item("updated"), rdr.Item("exports"), iq.i_state_GroupCode("QT-" + rdr.Item("quotestatus")), new NullablePrice(rdr.Item("totalvalue"), currency, false), currency, 0,
						0, 0, importquoteID, quoteName, new nullableString(), totalRebate, bootstrap, QuoteWriteCache, 25);
						//put check so that just in case key is already dicAllQuotes.
						//*******************************************************************************************************************
						// kept falling over so put this check so could test this need looking into for the cause only 99 records are export why!!!!
						if (!dicAllQuotes.ContainsKey(importquoteID)) {
							dicAllQuotes.Add(importquoteID, aquote);
						} else {
							System.Diagnostics.Debugger.Break();
							// break dont want to run if not importing all quotes.
						}
						//********************************************************************************************************************
						quotesCount += 1;

						aquote.TEMP_IMPORT_MARGIN = rdr.Item("quotemargin") < 1 ? 1 : rdr.Item("quotemargin");
						if (IsDBNull(rdr.Item("multiplier"))) {
							aquote.TEMP_IMPORT_MULTIPLIER = 1;
							//One quote had a null 'systems'
						} else {
							aquote.TEMP_IMPORT_MULTIPLIER = rdr.Item("multiplier");
						}

						bootstrap = false;
						//ok - we can bulk insert the remainder

						//put the latest version of the quote in the dictionary
						// Dim id$

							// .Name = New nullableString(rdr.Item("listname"))
						 // ERROR: Not supported in C#: WithStatement

						qc += 1;
					}

				}
			}

		}
		rdr.Close();
		if (qc > 0) {
			da.BulkWrite(con, QuoteWriteCache, "Quote");
			QuoteWriteCache = null;

			//fix the quote root pointers - (now the quotes have their ID's)
			da.DBExecutesql(con, "Update quote set fk_quote_id_root=quote.id ");

			sqlQuery = "CREATE UNIQUE NONCLUSTERED INDEX [IX_Quote] ON [dbo].[Quote] ([Version] ASC,[FK_Quote_ID_Root] Asc) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]";

			da.DBExecutesql(con, sqlQuery);

			//After the bulk insert - and before we can attach quote items (to the LAST) quote version,
			//we need to give every quote (in the dictionary) its' correct ID (so that quoteitems can have a valid fk_quote_id)
			//we do this only for th quotes we import in this batch

			string[] allQuoteKeys = dicAllQuotes.Keys.ToArray();
			//For i As Integer = 0 To allQuoteKeys.Count - 1
			//    allQuoteKeys(i) = "'" & allQuoteKeys(i) & "'"
			//Next

			string allQuoteKeysString = Join(allQuoteKeys, "','");
			allQuoteKeysString = "('" + allQuoteKeysString + "')";

			string sqlQuery2 = "Select ID,reference from Quote where reference in " + allQuoteKeysString;
			SqlClient.SqlDataReader rdr2;
			rdr2 = da.DBExecuteReader(con, sqlQuery2);
			clsQuote quote2;
			while (rdr2.Read) {
				if (dicAllQuotes.ContainsKey(rdr2("reference"))) {
					quote2 = dicAllQuotes(rdr2("reference"));
					quote2.ID = rdr2("ID");

				}
			}


			//Get all the systems on quotes - create those line items first (because they will be the parents of all the options)
			Dictionary<int, clsQuoteItem> Qsample = new Dictionary<int, clsQuoteItem>();
			Dictionary<string, clsQuoteItem> dicQuoteItems = new Dictionary<string, clsQuoteItem>();
			//loadDic(con, Qsample, "QIsystem")
			Dictionary<string, clsBranch> dicSystems = new Dictionary<string, clsBranch>();
			//loadDic(con, iq.Branches, "system")

			object allSystems = from s in iq.Branches.Valueswhere !IsNothing(s.Product) && s.Product.isSystem;

			foreach ( prodSys in allSystems) {
				if (!dicSystems.ContainsKey(prodSys.Product.sku)) {
					dicSystems.Add(prodSys.Product.sku, prodSys);
				}
			}
			//the dictionary of Quote system items we've already (previously) imported
			//around 3 seconds
			QuoteSystemItems(con, dicQuoteItems, dicAllQuotes, dicSystems, errorMessages, con2);

			//  saveDic(con, dicQuoteItems, "QIsystem")
			//  Logit("Imported " & added & " quote system items in " & TimeSince(LastMilestone))

			//Important to empty this
			Dictionary<int, clsQuoteItem> qiSample = new Dictionary<int, clsQuoteItem>();
			// dicQuoteItems = loadDic(con, qiSample, "QIoption") 'now loadUp the dictionary with previously imported quote options

			//get all the options - hook them up to the system quoteItems
			//around 7 secs
			QuoteOptionItems(con, dicAllQuotes, dicQuoteItems, errorMessages, con2);
			//recalculate/sanity check the totals - and generate the headline descriptions

			//around 11 secs
			Import.updateQuoteDescriptionsAndTotals(con, dicAllQuotes);

		} else {

			errorMessages.Add("No Quotes to import");

		}

		return false;
	}

	public int QuoteSystemItems(SqlClient.SqlConnection Con, ref Dictionary<string, clsQuoteItem> dicQuoteItems, Dictionary<string, clsQuote> dicQuotes, Dictionary<string, clsBranch> dicSystems, ref List<string> errorMessage, SqlClient.SqlConnection Con2 = null)
	{


		//dicQuoteItems uses listid^mfrpartnum as a unique key
		QuoteSystemItems = 0;

		Dictionary<string, clsQuoteItem> dicNewQuoteItems = new Dictionary<string, clsQuoteItem>();

		string sql = string.Empty;
		SqlClient.SqlDataReader rdr;
		clsQuote quote = null;

		DataTable swc = new DataTable();
		//systems write cache
		swc = da.MakeWriteCacheFor(Con, "quoteItem");

		clsQuoteItem anItem;
		//sql$ = "SELECT ListID,mfrpartnum,qty,savedprice,opttype from " & server$ & "[iq].quote.quotestore where ListID>3000 order by listid,case when rtrim(opttype)='sys' then 1 else 2 end"
		object allkeys = dicQuotes.Keys.ToArray;
		string allQuteIDs = string.Empty;
		if (allkeys(0).Contains("-")) {
			foreach ( key in allkeys) {
				allQuteIDs += Split(key, "-")(0) + ",";
			}
		} else {
			allQuteIDs = Join(allkeys, ",");
		}

		sql = "SELECT ListID,mfrpartnum,qty,savedprice,opttype from [iq].quote.quotestore where ListID in ( " + allQuteIDs + " ) and opttype = 'sys'";
		rdr = da.DBExecuteReader(Con2, sql);

		clsBranch sysBranch = null;
		//Each quote (should) have exactly one system - it will become a root level item in the new quote
		string systemPath = string.Empty;
		//path to the system unit - in the product tree

		clsBranch branch = null;
		//branch used for constructing the additional quote items (options)
		string SKU = string.Empty;

		//need to load the branches (so that grafts are done !) before we can recurse throught eh product tree to find options by SKU

		int qs = 0;
		//quote systems
		int qo = 0;
		//quote options

		string sysSKU = string.Empty;
		int oi = 0;
		//orphaned items

		bool skipquoteoptions = false;
		//used to skip options where the system is missing

		//If dicQuotes.Count = 0 Then Stop

		quote = null;

		string ck = string.Empty;
		string mfr = string.Empty;
		int rowCount = 0;
		while (rdr.Read) {
			rowCount += 1;
			systemPath = string.Empty;
			mfr = Trim(rdr.Item("mfrpartnum"));
			ck = rdr.Item("listid") + "-" + mfr;
			//compound key (uniquely identifies a quote item)
			if (dicQuoteItems.ContainsKey(ck)) {
			//already imported
			} else {
				if (dicSystems.ContainsKey(mfr)) {
					sysBranch = dicSystems(mfr);
					//a system - the (new) quotes root item
					sysSKU = mfr;
					//locate this option part number in the new product catalogue

					//systemPath$ = "tree." & Trim$(iq.RootBranch.ID)
					//systemPath$ &= "." & Trim$(sysBranch.Parent.Parent.Parent.ID) 'System type (desktops/notebooks/servers)
					//systemPath$ &= "." & Trim$(sysBranch.Parent.Parent.ID) 'Family
					//systemPath$ &= "." & Trim$(sysBranch.Parent.ID) 'supply chain (Smart Buy/Top Value/ regular)
					//systemPath$ &= "." & Trim$(sysBranch.ID) 'system
					GetFullPath(sysBranch, systemPath);
					systemPath = "tree" + systemPath;



					if (!dicQuotes.ContainsKey(rdr.Item("listid"))) {
						Logit("Quote " + rdr.Item("listid") + " does not exsit.");
						oi += 1;
						//orphaned items
					} else {
						quote = dicQuotes(rdr.Item("listid"));

						//when creating the quote line items we must multily the quantity by the 'systems' multiplier from the original export

						clsVariant skuvariant;
						if (sysBranch.Product.i_Variants == null) {
							string str = "";
						//legacy part (no variant)

						} else {
							if (sysBranch.Product.i_Variants.ContainsKey(quote.BuyerAccount.SellerChannel)) {
								skuvariant = sysBranch.Product.i_Variants(quote.BuyerAccount.SellerChannel)(0);
								bool islist = (bool)object.ReferenceEquals(skuvariant.sellerChannel, HP);
								anItem = new clsQuoteItem(quote, sysBranch, skuvariant, systemPath, rdr.Item("qty") * quote.TEMP_IMPORT_MULTIPLIER, new NullablePrice(rdr.Item("savedprice"), quote.Currency, islist), new NullablePrice(quote.Currency), false, null, new nullableString(),
								new nullableString(), 0, quote.TEMP_IMPORT_MARGIN, new nullableString(), 10, swc);

								dicQuoteItems.Add(ck, anItem);
								dicNewQuoteItems.Add(quote.Reference, anItem);
								quote.RootItem.Children.Add(anItem);
								QuoteSystemItems += 1;
							} else {
								string str = "";
								//legacy part (no variant)
							}
						}
					}
				} else {
					Logit("System " + rdr.Item("mfrpartnum") + " does not exist (so the quote could not be imported)");
					skipquoteoptions = true;
				}
			}
		}

		rdr.Close();
		if (QuoteSystemItems > 0) {
			da.BulkWrite(Con, swc, "QuoteItem");
			swc = null;

			string[] quoteIDs = (from q in dicNewQuoteItems.Valuesq.quote.ID).ToArray().Select(x => x.ToString()).ToArray();
			//Stop
			string[] quoteKeys = dicNewQuoteItems.Keys.ToArray();
			string allimportedQuoteItems = Join(quoteIDs, ",");
			//Read in the ID's onto the QuoteItems we've created thus far (via the bulk write) (because they need to be valid parents)
			sql = "SELECT q.reference,qi.id from quoteitem qi join quote as q on qi.fk_quote_id=q.id WHERE qi.fk_quote_id in (" + allimportedQuoteItems + ")";
			// & parentEvent.ID WORK NEEDED HERE
			rdr = da.DBExecuteReader(Con, sql);
			int quc = 0;
			List<string> errorMessages = new List<string>();
			while (rdr.Read) {
				clsQuote aquote = dicQuotes(rdr.Item("reference"));
				aquote.RootItem.Children(0).ID = rdr.Item("id");
				clsQuoteItem sysItem = aquote.RootItem.Children(0);
				aquote.addPreinstalledRecursive(sysItem, sysItem.Branch, sysItem.Path, false, errorMessages);
				aquote.Update();
				quc += 1;
			}
			rdr.Close();

			Logit("Stamped " + quc + " IDs onto system quoteitems");
		} else {

			errorMessage.Add("Failed to add system Items");
		}

	}

	public int quotes(SqlClient.SqlConnection con, Dictionary<string, clsQuote> dicLastQuotes, Dictionary<string, clsQuote> dicAllQuotes, Dictionary<string, clsAccount> dicAccounts, Dictionary<string, clsCurrency> dicRegionCurrencies, int importID, string hostID)
	{


		SqlClient.SqlDataReader rdr;
		clsAccount anaccount;

		con.Close();
		con = da.OpenDatabase();

		DataTable QuoteWriteCache;
		QuoteWriteCache = da.MakeWriteCacheFor(con, "Quote");

		quotes = 0;

		string sql = string.Empty;

		clsQuote aquote = null;

		//NB: margin is not on the quote in the new model.. it's on every item in the quote

		// sql$ = "dELETE FROM QUOTE"
		// DBExecutesql(con, sql$)

		// For Each ACCOUNT In iq.Accounts.Values
		// ACCOUNT.quotes.Clear()
		// Next

		//temporarily remove the unique index on FK_quoute_id_root, version  (which stops us creating two versions of the quote with same version number)
		//so we can do the bulk insert

		// da.DBExecutesql(con, "drop index quote.ix_quote")

		sql = "select CountryCode,currency from " + server + "[iq].dbo.countries";
		rdr = da.DBExecuteReader(con, sql);
		while (rdr.Read) {
			if (!IsDBNull(rdr.Item("countrycode"))) {
				if (!IsDBNull(rdr.Item("currency"))) {
					dicRegionCurrencies.Add(rdr.Item("countrycode"), iq.i_currency_code(rdr.Item("currency")));
				}
			}
		}
		rdr.Close();

		//REMOVE the TOP 100 !

		//There may be more than one quote (export) with the same ID (they will have different versions)
		// this could be ported to getQuotesfromServer.
		sql = getQuotesFromServer(hostID, true);
		//If hostID Is Nothing Then
		//    Dim daysOld As Integer = 14
		//    sql = "Select top 100 ID,username,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
		//    sql &= "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex "
		//    sql &= " join (SELECT QuoteID,RANK() OVER (PARTITION bY QuoteID ORDER BY timestamp ) as version,Margin,OPG,Rebate,TotalValue,Systems,Options,timestamp FROM " & server & "iq.quote.vExportsDistinct where timestamp>getdate()-" & daysOld & ") as qc on qc.quoteid=id "
		//    sql &= " join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null"
		//Else
		//    Dim daysOld As Integer = 120
		//    sql = "Select top 100 ID,username,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
		//    sql &= "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex "
		//    sql &= " join (SELECT QuoteID,RANK() OVER (PARTITION bY QuoteID ORDER BY timestamp ) as version,Margin,OPG,Rebate,TotalValue,Systems,Options,timestamp FROM " & server & "iq.quote.vExportsDistinct where timestamp>getdate()-" & daysOld & ") as qc on qc.quoteid=id "
		//    sql &= " join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null and u.ChanID = '" & hostID & "'"
		//End If
		rdr = da.DBExecuteReader(con, sql);

		int oid;
		Dictionary<int, object> dicOPG = new Dictionary<int, object>();
		Dictionary<int, object> dicBundle = new Dictionary<int, object>();
		Dictionary<int, float> dicMargin = new Dictionary<int, float>();

		bool bootstrap = true;
		//we must INSERT the very first quote (it can't be bulk inserted) as we need *something* to point all quotes fk_quote_id_root at
		if (dicAllQuotes.Count > 0)
			bootstrap = false;

		if (da.DBSelectFirst("select count(*) from quote where id=1") == 1)
			bootstrap = false;


		int qc = 0;
		//quote count


		while (rdr.Read) {
			if (dicAccounts.ContainsKey(rdr.Item("username"))) {
				anaccount = dicAccounts(rdr.Item("username"));
				//buyer

				if (anaccount.SellerChannel == null) {
					object an;
					an = rdr.Item("username");
				//anevent = New clsEvent(QuotesEvent, "account " & an$ & " has no seller channel", ev_Warning)

				} else {
					if (anaccount.SellerChannel.Region.Code != "AA") {
						clsCurrency currency;

						currency = dicRegionCurrencies(anaccount.SellerChannel.Region.Code);
						string ck = rdr.Item("id") + "-" + rdr.Item("exports");
						if (dicAllQuotes.ContainsKey(ck)) {
						//already imported

						} else {
							if (!object.ReferenceEquals(rdr.Item("totalvalue"), DBNull.Value)) {
								//we make a quote for every version/export
								//                                                                                             Version \/
								aquote = new clsQuote(anaccount, anaccount, null, rdr.Item("qcreated"), rdr.Item("updated"), rdr.Item("exports"), iq.i_state_GroupCode("QT-" + rdr.Item("quotestatus")), new NullablePrice(rdr.Item("totalvalue"), currency, false), currency, 0,
								0, 0, rdr.Item("ID"), new nullableString(), new nullableString(), (decimal)rdr("totalrebate"), bootstrap, QuoteWriteCache, importID);
								dicAllQuotes.Add(Trim(rdr.Item("id")) + "-" + rdr.Item("exports"), aquote);
								quotes += 1;

								aquote.TEMP_IMPORT_MARGIN = rdr.Item("quotemargin");
								if (IsDBNull(rdr.Item("multiplier"))) {
									aquote.TEMP_IMPORT_MULTIPLIER = 1;
									//One quote had a null 'systems'
								} else {
									aquote.TEMP_IMPORT_MULTIPLIER = rdr.Item("multiplier");
								}

								bootstrap = false;
								//ok - we can bulk insert the remainder

								//put the latest version of the quote in the dictionary
								string id = rdr.Item("id");
								if (dicLastQuotes.ContainsKey(id)) {
									if (aquote.Created > dicLastQuotes(id).Created) {
										dicLastQuotes(id) = aquote;
									}
								} else {
									dicLastQuotes.Add(rdr.Item("ID"), aquote);
									//these are Dan's, iQuote1 -  Quote (list ID's) 
								}



									//.Updated = rdr.Item("Updated")
									//margin, VoucherCodes, budlerefs apply (now) to ITEMS not Quotes
									//  .margin = rdr.Item("quotemargin")
									//  dicOPG.Add(oid, rdr.Item("quoteOPG")) '
									//  dicBundle.Add(oid, rdr.Item("bundleRef"))


									// .Update() 'IMPORTANT (makes descriptions etc) - but cant do it till we have some items

								 // ERROR: Not supported in C#: WithStatement

								qc += 1;
							}
						}
					}
				}

				//            Else
				//    Debug.Print("Skipped orphaned quote for " & rdr.Item("username"))
			}

		}
		rdr.Close();

		da.BulkWrite(con, QuoteWriteCache, "Quote");
		QuoteWriteCache = null;

		//fix the quote root pointers - (now the quotes have their ID's)
		da.DBExecutesql(con, "Update quote set fk_quote_id_root=quote.id");

		//sql$ = "CREATE UNIQUE NONCLUSTERED INDEX [IX_Quote] ON [dbo].[Quote]"
		//sql$ &= "([Version] ASC,[FK_Quote_ID_Root] Asc) "
		//sql$ &= " WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"

		//da.DBExecutesql(con, sql$)

		//After the bulk insert - and before we can attach quote items (to the LAST) quote version,
		//we need to give every quote (in the dictionary) its' correct ID (so that quoteitems can have a valid fk_quote_id)
		//we do this only for th quotes we import in this batch
		rdr = da.DBExecuteReader(con, "SELECT id,reference FROM quote WHERE fk_import_id=" + importID + ";");
		while (rdr.Read) {
			dicLastQuotes(rdr.Item("reference")).ID = rdr.Item("Id");
		}
		rdr.Close();




	}
	//host is nothing
	// sqlQuery = "Select top 100 ID,username,email,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
	// sqlQuery &= "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex"
	// sqlQuery &= " join (SELECT e1.QuoteID,RANK() OVER (PARTITION bY e1.QuoteID ORDER BY timestamp ) as version,e1.Margin,e1.OPG,e1.Rebate,e1.TotalValue,e1.Systems,e1.Options,e1.timestamp 
	//  FROM " & server & "iq.quote.vExportsDistinct e1 
	//  where e1.timestamp>getdate()-" & daysOld & ") as qc on qc.quoteid=id "
	// sqlQuery &= " join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null"
	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 
	//Host is not nothing
	// sqlQuery = "Select top 100 ID,username,email,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
	// sqlQuery &= "quotemargin,quoteopg,quotenotes,qcreated,qc.MostRecently as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex "
	// sqlQuery &= " join  (SELECT * FROM ( SELECT e1.QuoteID,RANK() OVER (PARTITION bY e1.QuoteID ORDER BY timestamp DESC )"
	// sqlQuery &= " as MostRecently,e1.Margin,e1.OPG,e1.Rebate,e1.TotalValue,e1.Systems,e1.Options,e1.timestamp FROM " & server & "[iq].quote.vExportsDistinct e1  WHERE e1.timestamp>getdate()-" & daysOld & ")"
	// sqlQuery &= " compiled  WHERE compiled.MostRecently=1) as qc on qc.quoteid=id join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid"
	// sqlQuery &= "  where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null and u.userHostID = '" & hostID & "'"
	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	// ALL SQL
	// sql = "Select top 100 ID,username,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
	// sql &= "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex "
	// sql &= " join (SELECT QuoteID,RANK() OVER (PARTITION bY QuoteID ORDER BY timestamp ) as version,Margin,OPG,Rebate,TotalValue,Systems,Options,timestamp FROM " & server & "iq.quote.vExportsDistinct where timestamp>getdate()-" & daysOld & ") as qc on qc.quoteid=id "
	// sql &= " join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null and u.ChanID = '" & hostID & "'"
	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	/// <summary> ' These sql statements where being used in  quotesbyhostid and all.</summary>
	/// <param name="hostid">A string object that represents the channel ID. can be null.</param>
	/// <returns>A string object that represents a string.</returns>
	/// <remarks> 
	/// </remarks>
	private string getQuotesFromServer(string hostid, bool all)
	{
		StringBuilder sqlQuery = new StringBuilder(string.Empty);
		int daysOld = string.IsNullOrWhiteSpace(hostid) ? 14 : 120;

		if (all) {
		}
		if (string.IsNullOrWhiteSpace(hostid)) {
			sqlQuery.AppendFormat("{0}", "Select top 100 ID,username,email,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,");
			sqlQuery.AppendFormat("{0}{1}{2}", "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from ", server, "[iq].quote.quoteindex");
			sqlQuery.AppendFormat("{0}", " join (SELECT e1.QuoteID,RANK() OVER (PARTITION bY e1.QuoteID ORDER BY timestamp ) as version,e1.Margin,e1.OPG,e1.Rebate,e1.TotalValue,e1.Systems,e1.Options,e1.timestamp");
			sqlQuery.AppendFormat("{0}{1}{2}", " FROM ", server, "iq.quote.vExportsDistinct e1");
			sqlQuery.AppendFormat("{0}{1}{2}", " where e1.timestamp>getdate()-", daysOld, ") as qc on qc.quoteid=id ");
			sqlQuery.AppendFormat("{0}{1}{2}{3}{4}", " join ", server, "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-", daysOld, " and totalvalue is not null");

		} else {
			if (all) {
				sqlQuery.AppendFormat("{0}", "Select top 100 ID,username,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,");
				sqlQuery.AppendFormat("{0}{1}{2}", "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from ", server, "[iq].quote.quoteindex ");
				sqlQuery.AppendFormat("{0}{1}{2}{3}{4}", " join (SELECT QuoteID,RANK() OVER (PARTITION bY QuoteID ORDER BY timestamp ) as version,Margin,OPG,Rebate,TotalValue,Systems,Options,timestamp FROM ", server, "iq.quote.vExportsDistinct where timestamp>getdate()-", daysOld, ") as qc on qc.quoteid=id ");
				sqlQuery.AppendFormat("{0}{1}{2}{3}{4}{5}{6}", " join ", server, "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-", daysOld, " and totalvalue is not null and u.ChanID = '", hostid, "'");
			} else {
				sqlQuery.AppendFormat("{0}", "Select top 100 ID,username,email,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,");
				sqlQuery.AppendFormat("{0}{1}{2}", "quotemargin,quoteopg,quotenotes,qcreated,qc.MostRecently as exports,totalvalue,rebate as totalrebate from ", server, "[iq].quote.quoteindex ");
				sqlQuery.AppendFormat("{0}", " join  (SELECT * FROM ( SELECT e1.QuoteID,RANK() OVER (PARTITION bY e1.QuoteID ORDER BY timestamp DESC )");
				sqlQuery.AppendFormat("{0}{1}{2}{3}{4}", " as MostRecently,e1.Margin,e1.OPG,e1.Rebate,e1.TotalValue,e1.Systems,e1.Options,e1.timestamp FROM ", server, "[iq].quote.vExportsDistinct e1  WHERE e1.timestamp>getdate()-", daysOld, ")");
				sqlQuery.AppendFormat("{0}{1}{2}", " compiled  WHERE compiled.MostRecently=1) as qc on qc.quoteid=id join ", server, "channelcentral.customers.users u on u.upkid =  quoteindex.upkid");
				sqlQuery.AppendFormat("{0}{1}{2}{3}{4}", " where id>3000 and updated>getdate()-", daysOld, " and totalvalue is not null and u.userHostID = '", hostid, "'");
			}
		}
		return sqlQuery.ToString;
	}
	public int QuoteOptionItems(SqlClient.SqlConnection con, Dictionary<string, clsQuote> dicLastquotes, ref Dictionary<string, clsQuoteItem> dicQuoteOptions_pi, ref List<string> errorMessages, SqlClient.SqlConnection con2 = null)
	{

		//Creates Quote Line items for the all the options (not the systems) on a quote

		QuoteOptionItems = 0;

		int qo;
		// counter
		DataTable owc;
		// options write cache
		string sql = string.Empty;
		SqlClient.SqlDataReader rdr;
		clsQuote quote;
		// reference to the quote
		clsQuoteItem newItem;
		//for constructing the new items
		clsQuoteItem sysItem;
		//THE item containing THE system on this quote
		string sku;
		int oi;

		owc = da.MakeWriteCacheFor(con, "QuoteItem");


		string iqQuoteIds = Join((from x in dicLastquotes.Valuesx.Reference).ToArray(), ",");



		//get an ordered list of non system options,ordered by the system they were attached to
		//allows us to cache the tree paths (of the options under its system).. making the import an order of magnitude faster
		sql = "SELECT qs.ListID,qs.mfrpartnum,sysunits.MfrPartNum AS SU,qty,savedprice,optType ";
		sql += "from [iq].quote.quotestore AS qs ";
		sql += "JOIN (SELECT mfrpartnum,listid from [iq].quote.quotestore WHERE optType='sys') AS sysunits ON sysunits.listid = qs.listid ";
		sql += "WHERE  optType <> 'sys' and qs.listID in ( " + iqQuoteIds + ")";
		sql += "ORDER BY qs.listID,SU,mfrpartnum";

		rdr = da.DBExecuteReader(con2, sql);


		//we will clear this each time the system unit changes
		//and check/add to it for each option - saves a LOT of recursive looking up of child branches 
		//(basically each (option)branch is looked up ONCE under each system - instead of it having to be looked up EVERY time it appears)
		string OptionPath = string.Empty;
		OptionPath = null;

		clsBranch optionBranch = null;
		sysItem = null;


		int nosystem = 0;

		string su = string.Empty;
		string startat = string.Empty;

		string ck = string.Empty;
		//compound key



		while (rdr.Read) {
			ck = rdr.Item("listid") + "^" + Trim(rdr.Item("mfrpartnum"));
			if (!dicQuoteOptions_pi.ContainsKey(ck)) {

				if (rdr.Item("listid") == 328145) {
					string str1 = "";
				}

				if (!dicLastquotes.ContainsKey(rdr.Item("listid"))) {
					//this items (parent) quote no longer exists
					oi += 1;
					//orphaned items
				} else {
					//make this item as a child of the root item (system)
					quote = dicLastquotes(rdr.Item("listid"));
					if (quote.RootItem.Children.Count == 0) {
						Logit("Quote " + quote.Reference + " has no system on it ??");
						nosystem += 1;
					} else {
						sysItem = quote.RootItem.Children(0);
						//The first child of the quotes root item - IS the system
						sku = Trim(rdr.Item("mfrpartnum"));
						//locate this option part number in the new product catalogue (under the system)

						//If quote.RootItem.Children.Count = 0 Then/
						//    Logit("quote " & rdr.Item("listID") & " didn't appear to have a system on it ?")
						//    nosystem += 1
						//Else
						// If rdr.Item("SU") <> su Then
						su = Trim(rdr.Item("su"));
						startat = sysItem.Path;
						OptionPath = string.Empty;
						Pmark("FindChildBySku");
						optionBranch = null;
						optionBranch = sysItem.Branch.findChildBySKU2(startat, sku, OptionPath);
						//staring at this branch/path - recurse down until you find the sku - returns branch and its address 
						Pacc("FindChildBySku");
						//End If

						if (optionBranch == null) {
							// Stop 'the option SKU wasn't found under the system
							Logit(sku + " is not an option for " + rdr.Item("su"));

						} else {
							//If Not systemItem Is dicquotes(rdr.Item("listid")) Then Stop
							NullablePrice listprice = new NullablePrice(quote.QuotedPrice.currency);
							NullablePrice price;
							if (object.ReferenceEquals(rdr.Item("savedPrice"), DBNull.Value)) {
								price = new NullablePrice(rdr.Item("savedPrice"), quote.Currency, false);
							} else {
								price = new NullablePrice(rdr.Item("savedPrice"), quote.Currency, false);
							}


							//If sysItem Is Nothing Then Stop
							//If sysItem.ID = -1 Then Stop

							//when creating the quote line items we must multiply the quantity by the 'systems' multiplier from the original export

							//NB:- We do NOT multiplty the option quantities by * quote.TEMP_IMPORT_MULTIPLIER - becuase the system (quoteitem) has been (multiplied already)

							nullableString opg = new nullableString();
							nullableString bundle = new nullableString();

							if (optionBranch.Product.i_Variants == null) {
								string strtes = "";
							//legacy option (no variant)
							} else {
								if (!optionBranch.Product.i_Variants.ContainsKey(quote.BuyerAccount.SellerChannel)) {
								//legacy option (no variant)
								} else {
									clsVariant SKUvariant = optionBranch.Product.i_Variants(quote.BuyerAccount.SellerChannel)(0);
									newItem = new clsQuoteItem(quote, optionBranch, SKUvariant, OptionPath, rdr.Item("qty"), price, listprice, false, sysItem, opg,
									bundle, 0, quote.TEMP_IMPORT_MARGIN, new nullableString(), 10, owc);
									QuoteOptionItems += 1;
									//how many we've added (function return value)
								}
							}
							//sysItem.Children.Add(anItem) - DONT need to do this (specifying sysitem as the parent does it)
						}
						qo += 1;
						// quote options (count)
						// End If
					}
				}
			}

		}
		rdr.Close();

		if (QuoteOptionItems > 0) {
			int rows = owc.Rows.Count;
			da.BulkWrite(con, owc, "QuoteItem");
			owc = null;
		} else {
			errorMessages.Add("Failed to add any options");
		}

		//QOIEvent.update(qo & "option quote items")

	}




	private void GetFullPath(clsBranch sysBranch, ref string fullpath)
	{
		fullpath = "." + Trim(sysBranch.ID) + fullpath;

		if (!IsNothing(sysBranch.Parent)) {
			GetFullPath(sysBranch.Parent, fullpath);
		}


	}



}
