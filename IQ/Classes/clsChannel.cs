using dataAccess;
using System.Globalization;
using System.Collections.Concurrent;
using System.Threading;


[Serializable]
public class clsChannel : i_Editable
{

    //NB:-ALL channels are clones ! - they have an IsCloneOf member - it's just that 'non clones' are clones of themselves.
    //This way we can *always* present pricing as that of the channels clone (wether it's a clone or not)

    public int ID { get; set; }
    public string Name { get; set; }
    public string BusinessName { get; set; }
    public string Address { get; set; }
    public string Code { get; set; } //IQ1 HostID
    //Public ChannelAcID As String ' buyer account number
    public Dictionary<int, clsUser> Users { get; set; } //users (who work at this channel - and are generally buyers at another channel, or sales agents at this one)
    public clsRegion Region { get; set; } //Country As clsCountry - previously country - but countries *are* now regions
    public Dictionary<int, clsTeam> Teams { get; set; }
    public Dictionary<int, clsAccount> CustomerAccounts { get; set; } //the people this channel sells to
    public string WebToken { get; set; } //Used as a unique (and 'unguessable') token (instead of a username/password) for webservice operations
    //                              buyer                   sector
    public Dictionary<clsChannel, Dictionary<clsSector, clsMargin>> Margin { get; set; }
    public clsChannel IsCloneOf { get; set; }
    public clsChannel Parent { get; set; } //Channels are placed in a heirarchy for organisational/display puproses.. much like threads
    public Dictionary<int, clsChannel> Children { get; set; } //we use a dictionary - rather than a list, so that indiviual elements can be addressed for editing

    public nullableString pic1 { get; set; }
    public nullableString pic2 { get; set; }
    public nullableString URL { get; set; }

    public string TreePath { get; set; }
    public string Focus { get; set; } //Recta,smartbuy etc. (intinital filter against the 'Focus' Attributte of products. (can be a CD list)  'iq.dbo.countries.hpreceta
    public List<string> Domains { get; set; }
    public Dictionary<int, clsCampaign> Campaigns { get; set; }
    public float marginMin { get; set; } //Most negative permissable margin (negative margin is reducing the cost)
    public float marginMax { get; set; } //largest allowable margin (markup)
    public string marginType { get; set; } //R' or 'C' for  Retained or 'CostPlus'
    public string Legal { get; set; } //host specific terms and conditions
    public string SchemeOverride { get; set; } //Host specific loyalty points scheme codes (comma delimited list) - having an entry here will override the usual (regionalised) Loyalty schems
    public clsCurrency DefaultCurrency { get; set; }
    public bool Universal { get; set; }
    public string orderEmail { get; set; }
    public string basketMode { get; set; }
    public string basketURL { get; set; }

    //    Public Variants As Dictionary(Of clsProduct, List(Of clsVariant))

    public short priceConfig { get; set; } //eger ' contains a set of bitwise flags controlling which prices (and therefore products) are displayed - Per SELLER channel (at the moment - could be moved to Buyer channel, or even account without great difficulty)


    //Public variantsLoaded As Integer 'used on the seller channel - to indicate that the variants (containing host partnumbers, and indexing prices) are loaded
    public DateTime variantsLoadedAt;
    public Dictionary<clsPriceBand, int> pricesLoadedFor; //used on sellerchannel - to indicate how many prices have been loaded for each priceband
    public Dictionary<clsRegion, int> listPricesLoadedFor; //Specific to the HP channel - and used to know whether to load list prices for the users region at signin
    public bool stockLoaded;


    //DistiSKU|wharehousecode>clsVariant - compound key (NB warehouse will *often* be blank
    private Dictionary<string, clsVariant> i_variantCK; //use the FindVariant public helper function to get to his
    //Private SKUs As Dictionary(Of clsProduct, List(Of clsVariant)) 'the variant(s) contains the DistsSKU(s) (or blank if they don't have one ..ie they use the HP partNumber

    public string displayName(clsLanguage language)
    {
        string returnValue = "";
        returnValue = Name + " (" + Region.Name.text(language) + ")";
        return returnValue;
    }


    /// <summary>
    /// If the channel has no teams, makes one (an Everyone) .. and assigns all existintg users to it
    /// </summary>
    /// <remarks></remarks>
    public void fixteams(List<string> errormessages)
    {
        if (this.Teams.Values.Count < 1)
        {

            clsTeam newTeam = new clsTeam(this, "Everyone");
            clsUser existingUser = new clsUser();

            foreach (clsUser tempLoopVar_existingUser in this.Users.Values)
            {
                existingUser = tempLoopVar_existingUser;
                //If existingUser.Accounts.Count = 0 Then
                //    Dim newAccount As clsAccount = New clsAccount(existingUser, )
                //    existingUser.Accounts.Add(agentaccount.SellerChannel.ID, agentaccount)
                //    existingUser.update()
                //End If

                System.Boolean userAccounts = from j in existingUser.Accounts.Values where j.SellerChannel == this select j;
                if (userAccounts.Any)
                {
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

    public dynamic Insert(ref List<string> errorMessages)
    {

        clsChannel achannel = new clsChannel(this.Parent, this.Name, this.BusinessName, this.Address, this.Code, this.Region, this.pic1, this.pic2, this.URL, this.priceConfig, this.TreePath, this.Focus, this.marginMin, this.marginMax, this.marginType, this.SchemeOverride, this.Legal, null, this.Universal, this.orderEmail, this.basketMode, this.basketURL, null, -1);
        return achannel;

    }

    public bool deIndexVariant(clsVariant v, List<string> errorMessages)
    {

        if (i_variantCK.ContainsKey(v.CK))
        {
            i_variantCK.Remove(v.CK);
            return true;
        }
        else
        {
            errorMessages.Add("Could not locate " + v.CK + "");
            return false;
        }

    }

    public void indexVariant(clsVariant v)
    {
        if (!this.i_variantCK.ContainsKey(v.CK))
        {
            this.i_variantCK.Add(v.CK, v);
        }
    }


    /// <summary>Returns a specific variant (matching DistiSku) - Variants effectively join, sellers, products and warehouses - allowing us to store different price and stock per variant/buyer. </summary>
    public bool findVariant(string DistiSKU, string warehouse, ref clsStockPriceSvc.clsResult result, ref IQ.clsVariant SKUvariant)
    {
        bool returnValue = false;

        //mfsrSKU is the hostManufacturer part number and may contain a #

        returnValue = false;
        result = null; //Any error msg
        // Dim product As clsProduct

        //mfrsku = Split(mfrsku, "#")(0)  'Not sure about this - all IQ2 Mfrpartnums have no #
        // Dim stub As String = Split(MfrSku, "#")(0)

        string ck = DistiSKU + "|" + warehouse;
        if (!this.i_variantCK.ContainsKey(ck))
        {
            result = new clsStockPriceSvc.clsResult(false, this.i_variantCK.Count + " variants - none with the host SKU|warehouse combo  " + ck + " use AddVariant to add new variants.", 56);
        }
        else
        {
            result = new clsStockPriceSvc.clsResult(true, "OK", 0);
            SKUvariant = this.i_variantCK[ck];
            returnValue = true;
        }

        return returnValue;
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


        try
        {
            string[] SKUlist = cl.AllProducts(this.Code);

            if (SKUlist.Count() < 10)
            {

                errorMessages.Add("Crazy small feed ! - nothing freshened");

                return "Refresh failed";

            }
            else
            {

                HashSet<string> FeedCKs = default(HashSet<string>); //we construct a list of all the CK's in the response (to check for dupes) - Note a HashSet is much faster than a list for Contians ops.
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
                foreach (var line in SKUlist)
                {

                    if (line.Trim() == "")
                    {
                        errorMessages.Add("blank line in AllProducts response");
                    }

                    //MrfSKU|DistisSKu|Warehouse
                    object[] bits = line.Split('|');
                    System.String mfrSKU = bits[0];
                    System.String distisku = bits[1];

                    if (bits.Count() > 3)
                    {
                        errorMessages.Add(line + " in AllProducts response contained too many segments.");
                    }
                    else
                    {
                        string warehouse = "";
                        if (bits.Count() == 3)
                        {
                            warehouse = bits[3];
                        }

                        string ck = distisku + "|" + warehouse;
                        if (FeedCKs.Contains(ck))
                        {
                            //Duplicate (By DisitSKU|Warwhouse)
                            errorMessages.Add("Duplicated line " + line);
                        }
                        else
                        {
                            FeedCKs.Add(ck);
                            if (this.i_variantCK.ContainsKey(ck)) //warehouse
                            {
                                //all good - the variant exists
                                existed++;
                            }
                            else
                            {
                                //Need to create a new variant
                                clsProduct product = default(clsProduct);
                                if (iq.i_SKU.ContainsKey(mfrSKU))
                                {
                                    product = iq.i_SKU(mfrSKU);
                                    //this could be *much* faster with a 'writecahe' (but additions should
                                    clsVariant newVariant = new clsVariant("", product, this, distisku, "", warehouse, "", null, false, vwc, nvid);
                                    added++;
                                }
                                else
                                {
                                    if (errorMessages.Count < 100)
                                    {
                                        errorMessages.Add("Unrecognised part " + System.Convert.ToString(mfrSKU) + " could not create variant");
                                    }
                                }
                            }
                        }
                    }
                }

                da.BulkWrite(con, vwc, "variant", 1000, true);
                con.Close();

                foreach (var ck in this.i_variantCK.Keys.ToArray)
                {
                    if (!ck.Contains("FAKE")) //Pauls fake parts for the Unhosted instance (so he can add anything to a basket)
                    {
                        if (!FeedCKs.Contains(ck) && !this.i_variantCK[ck].DistiSku.StartsWith("###") && !this.i_variantCK[ck].Deleted)
                        {
                            //variant no longer in the feed - 'delete' it
                            deleted++;
                            this.i_variantCK[ck].Delete(errorMessages); //NOTE - We NEVER actually delete variants - becuase they're referenced by quote items -the are flagged as deleted - and removed from the indicies
                        }
                    }
                }

                return this.Name + "(" + this.Code + ") - FreshenVariants - Existed:" + System.Convert.ToString(existed) + " Added:" + System.Convert.ToString(added) + " Deleted:" + System.Convert.ToString(deleted);

            }
        }
        catch (System.Exception ex)
        {

            ErrorLog.Add(ex);
        }


    }

    public void delete(ref List<string> errorMessages)
    {

        errorMessages.Add("Delete is not yet Implemented on the Channel object");

    }

    public string LoadStock()
    {


        List<string> errorMessages = new List<string>();
        double ts = System.Convert.ToDouble(Stopwatch.GetTimestamp);

        SqlClient.SqlConnection con = da.OpenDatabase;
        SqlClient.SqlDataReader r = default(SqlClient.SqlDataReader);

        string dzero = System.Convert.ToString(da.UniversalDate(DateTime.Parse("01/01/2000")));

        object sql = null;
        sql = "SELECT stock.ID,fk_variant_id,v.fk_product_id,v.fk_channel_id_seller,quantity,Arrival,datestamp,iscurrent ";
        sql += "FROM [stock] ";
        sql += "JOIN [variant] AS v ON v.id=fk_variant_id  ";
        sql += " WHERE fk_channel_id_seller=" + System.Convert.ToString(this.ID) + " AND arrival = " + dzero + " AND isCurrent = 1 Or Arrival > " + da.UniversalDate(DateAndTime.DateAdd(DateInterval.Day, -30, DateTime.Now));

        r = da.DBExecuteReader(con, sql);

        int count = 0;
        clsProduct Product;
        clsChannel Seller;
        clsstock stock;
        clsVariant SKUvariant = default(clsVariant);
        int duds = 0;

        if (r.HasRows)
        {
            while (r.Read)
            {

                Seller = iq.Channels(r.Item("fk_channel_id_seller"));

                int pid = System.Convert.ToInt32(r.Item("fk_product_id"));
                if (!iq.Products.ContainsKey(pid))
                {
                    if (duds < 10)
                    {
                        Logit(this.displayName(English) + " LoadStock referenced product " + System.Convert.ToString(pid) + " which is not in the OM of " + iq.Products.Count + " products");
                        duds++;
                    }

                }
                else
                {

                    if (iq.Products.ContainsKey(pid))
                    {
                        Product = iq.Products(pid);
                    }
                    else
                    {
                        Product = iq.REMAPS(pid);
                    }

                    int vid = System.Convert.ToInt32(r.Item("fk_variant_id"));
                    if (!iq.Variants.ContainsKey(vid))
                    {
                        //Logit(Me.DisplayName(English) & " LoadStock referenced variant " & vid & " which is not in the OM")
                    }
                    else
                    {
                        SKUvariant = iq.Variants(vid);
                        //just creating the stock adds it to the product AND iq.stock (the 'flat' list used for import
                        stock = new clsstock(r.Item("ID"), SKUvariant, r.Item("quantity"), r.Item("arrival"), r.Item("datestamp"), r.Item("isCurrent"), errorMessages);
                        count++;
                    }
                }
            }
        }
        r.Close();
        con.Close();
        con.Dispose();

        this.stockLoaded = true;

        string v = "Loaded " + System.Convert.ToString(count) + " Stock records in " + TimeSince(ts) + "<br/>";

        foreach (var e in errorMessages)
        {
            v += "<p>" + e + "</p>";
            if (v.Length > 1000)
            {
                break;
            }
        }

        return v;

    }


    /// <summary>Loads (from the DB) the variants for this channel - and 'freshens' them via a webservice call if necessary</summary>
    /// <param name="errormessages"></param>
    /// <param name="maxAgeHrs">Variants will not be re-loaded if they are 'fresher' than this</param>
    /// <returns></returns>
    /// <remarks></remarks>
    public string LoadVariants(List<string> errormessages, float maxAgeHrs)
    {

        //Called on the sellerchannel (when an account is selected at login) - to load the SKUvariants - (loosely equivilent to IQ1 tbhostPartnums)
        //Doing this 'just in time' (per channel) like this saves around 400MB and 5 seconds from the OBject model and its startup time

        float ts = System.Convert.ToSingle(Stopwatch.GetTimestamp);
        int count = 0;
        int dupes = 0;
        int bad = 0;

        if (string.IsNullOrEmpty(this.variantsLoadedAt))
        {

            //Read them from the IQ2 Database

            clsChannel seller = this; //just for clarity
            SqlClient.SqlConnection con = da.OpenDatabase;
            SqlClient.SqlDataReader r = default(SqlClient.SqlDataReader);


            //NB: - we don't load deleted variants
            r = da.DBExecuteReader(con, "SELECT v.id,code,distiSKU,fk_channel_id_seller,fk_product_id,displaytext,warehouse,localisation,fk_region_id,v.deleted  " + "FROM [Variant] v inner join Product p on p.id = fk_product_id WHERE p.deleted = 0 and  fk_channel_id_seller=" + System.Convert.ToString(this.ID));

            clsVariant v;
            clsProduct product = default(clsProduct);
            clsRegion region = default(clsRegion);
            string warehouse = "";
            string distiSKU = "";

            this.i_variantCK.Clear();

            while (r.Read)
            {
                //iq.Channels(r.Item("fk_channel_id")).addSKU(iq.Products(r.Item("fk_product_id")), iq.Variants(r.Item("fk_variant_id")), r.Item("channelSKU"))

                distiSKU = System.Convert.ToString(r.Item("distiSKU"));

                int pid = System.Convert.ToInt32(r.Item("fk_product_id"));
                if (iq.Products.ContainsKey(pid))
                {
                    product = iq.Products(pid);
                }
                else
                {
                    bad++;
                    continue;
                    product = iq.REMAPS(pid);
                }

                //    seller = iq.Channels(r.Item("fk_channel_id_seller")) this IS ME
                region = null;
                if (r.Item("fk_region_id") != DBNull.Value)
                {
                    region = iq.Regions(r.Item("fk_region_id"));
                }
                warehouse = System.Convert.ToString(r.Item("warehouse"));


                if (region != null && region.Code == "CO")
                {
                    int a = 0;
                }

                if (string.IsNullOrEmpty(distiSKU))
                {
                    if (errormessages.Count < 10)
                    {
                        errormessages.Add("distiSKU was blank for variant " + r.Item("ID").ToString());
                    }
                }
                else
                {
                    if (this == HP && i_variantCK.ContainsKey(distiSKU + "|" + region.Code))
                    {
                        if (errormessages.Count < 10)
                        {
                            errormessages.Add("Duplicate HP variant " + distiSKU + "|" + region.Code);
                        }

                        dupes++;
                    }
                    else if (i_variantCK.ContainsKey(distiSKU + "|" + warehouse))
                    {
                        v = new clsVariant(System.Convert.ToInt32(r.Item("id")), r.Item("code"), product, this, distiSKU, r.Item("displaytext"), warehouse, r.Item("Localisation"), region, r.Item("deleted"), false);
                        if (errormessages.Count < 10)
                        {
                            errormessages.Add("Duplicate variant " + distiSKU + "|" + warehouse + " for " + this.Code + "(" + this.Name + ")");
                        }
                        dupes++;
                    }
                    else
                    {
                        v = new clsVariant(System.Convert.ToInt32(r.Item("id")), r.Item("code"), product, this, distiSKU, r.Item("displaytext"), warehouse, r.Item("Localisation"), region, r.Item("deleted"), true);
                        count++;
                    }
                }
            }

            r.Close();
            con.Close();
            con.Dispose();

            this.variantsLoadedAt = DateAndTime.DateAdd(DateInterval.Hour, -100, DateTime.Now); //force a freshen

        }

        if ((Math.Abs(DateAndTime.DateDiff(DateInterval.Hour, DateTime.Now, this.variantsLoadedAt))) > maxAgeHrs)
        {
            //Freshen' them from the webservice (this may add and delete variants!)

            this.variantsLoadedAt = DateTime.Now; //we set this 'early' so that it's not called twice for multiple users logging int simultaneousness
            System.String j = this.freshenVariants(errormessages);

        }

        return "Loaded " + System.Convert.ToString(count) + " variants SKUs in " + TimeSince(ts) + " skipped " + System.Convert.ToString(bad) + " bad (deleted products)<br/>";

    }


    public string LoadPrices(clsPriceBand PriceBand, List<string> errorMessages, clsRegion region = null)
    {
        // If Environment.MachineName = "LINGM-LAPTOP" Then Exit Function
        //Called on the sellerchannel, passing the buyerchannel to load the prices for/of the buyerchannel

        float ts = System.Convert.ToSingle(Stopwatch.GetTimestamp);

        int already = 0;

        clsChannel Seller = this;

        SqlClient.SqlConnection con = da.OpenDatabase;
        SqlClient.SqlDataReader r = default(SqlClient.SqlDataReader);

        //r = da.dbexecuteReader(con, "SELECT Id,fk_product_id,fk_variant_id,fk_channel_id_seller,fk_channel_id_buyer,price,fk_currency_id,datestamp,source from [Price]")
        object sql = null;

        sql = "SELECT Price.Id as priceID,V.FK_PRODUCT_ID,fk_variant_id,v.fk_channel_id_seller,priceband,price,fk_currency_id,datestamp,source ";
        sql += "FROM [Price]";
        sql += "JOIN [Variant] AS v on v.id= fk_variant_id ";
        sql += "JOIN [product] AS p on p.id= v.fk_product_id ";
        sql += "WHERE fk_channel_id_seller=" + System.Convert.ToString(Seller.ID) + " AND priceband=\'" + PriceBand.text + "\'";
        sql += "AND p.deleted = 0 and v.deleted = 0";

        if (region != null)
        {
            sql += " AND fk_region_id=" + Region.ID;
        }

        r = da.DBExecuteReader(con, sql);

        int count = 0;
        clsPrice aPrice;
        clsProduct Product;
        //Dim buyer As clsChannel = buyerchannel
        clsCurrency Currency = default(clsCurrency);
        decimal price = new decimal();
        clsVariant SKUvariant = default(clsVariant);

        if (r.HasRows)
        {
            while (r.Read)
            {

                int vid = System.Convert.ToInt32(r.Item("fk_variant_id"));

                if (iq.Variants.ContainsKey(vid))
                {
                    SKUvariant = iq.Variants(vid);

                    if (!SKUvariant.prices.ContainsKey(r.Item("PriceID"))) //check its not already loaded
                    {

                        int pid = System.Convert.ToInt32(r.Item("fk_product_id"));

                        if (iq.Products.ContainsKey(pid)) //SHOULD mbe removed (but without it it crashes!)
                        {
                            Product = iq.Products(pid);
                        }
                        else
                        {
                            Product = iq.REMAPS(pid);
                        }

                        Currency = iq.Currencies(r.Item("fk_currency_id"));
                        price = System.Convert.ToDecimal(r.Item("price"));

                        //will add the price into both the master price list - and into the product.price(seller)(buyer)(currency)
                        DateTime datestamp = default(DateTime);
                        if (Information.IsDBNull(r.Item("datestamp")))
                        {
                            datestamp = DateTime.Now;
                        }
                        else
                        {
                            datestamp = System.Convert.ToDateTime(r.Item("datestamp"));
                        }

                        //WE DONT WANT ZERO PRICES (for now)
                        if (price != 0)
                        {
                            if (r.Item("priceid") < 1)
                            {
                                errorMessages.Add("a price has an id <1");
                            }
                            aPrice = new clsPrice(r.Item("priceid"), SKUvariant, iq.getPriceBand(r.Item("Priceband")), price, Currency, datestamp, r.Item("source"));
                        }

                        count++;

                    }
                    else
                    {
                        already++;
                    }
                }
                else
                {
                    //missing variant ?
                }

            }
        }
        r.Close();
        r.Close();
        con.Close();
        con.Dispose();

        if (!this.pricesLoadedFor.ContainsKey(PriceBand))
        {
            pricesLoadedFor.Add(PriceBand, 0);
        }
        this.pricesLoadedFor[PriceBand] = count + already;

        //only list prices are region specific... we track how many were loaded to know wther we need to load them for the users country at logon
        if (region != null)
        {
            if (!this.listPricesLoadedFor.ContainsKey(region))
            {
                this.listPricesLoadedFor.Add(region, 0);
            }
            this.listPricesLoadedFor[region] = count + already;
        }

        return "Loaded " + System.Convert.ToString(count) + " Prices in " + TimeSince(ts) + " " + System.Convert.ToString(already) + " were already loaded<br/>";

    }


    public void update(ref List<string> errorMessages)
    {

        object sql = null;
        sql = "UPDATE CHANNEL SET ";
        sql += "FK_Channel_id_cloneof=" + System.Convert.ToString(this.IsCloneOf.ID) + ",";
        if (this.Parent == null)
        {
            sql += "FK_Channel_id_parent=null,";
        }
        else
        {
            sql += "FK_Channel_id_parent=" + System.Convert.ToString(this.Parent.ID) + ",";
        }

        sql += "Name=" + da.SqlEncode(this.Name) + ",";
        sql += "Address=" + da.SqlEncode(this.Address) + ",";
        sql += "FK_Region_ID=" + this.Region.ID + ",";
        sql += "webtoken=" + da.SqlEncode(this.WebToken) + ",";
        sql += "code=" + da.SqlEncode(this.Code) + ",";
        sql += "pic1=" + this.pic1.sqlValue + ",";
        sql += "pic2=" + this.pic2.sqlValue + ",";
        sql += "URL=" + this.URL.sqlValue + ",";
        sql += "priceconfig=" + System.Convert.ToString(this.priceConfig) + ",";
        sql += "focus=" + da.SqlEncode(this.Focus) + ",";
        sql += "treepath=" + da.SqlEncode(this.TreePath) + ",";
        sql += "marginMin=" + System.Convert.ToString(this.marginMin) + ",";
        sql += "marginMax=" + System.Convert.ToString(this.marginMax) + ",";
        sql += "marginType=" + da.SqlEncode(this.marginType) + ",";
        sql += "schemeOverride=" + da.SqlEncode(this.SchemeOverride) + ",";
        sql += "legal=" + da.SqlEncode(this.Legal) + ",";
        sql += "fk_currency_id_default=";
        if (this.DefaultCurrency == null)
        {
            sql += "null,";
        }
        else
        {
            sql += this.DefaultCurrency.ID + ",";
        }
        sql += "universal=" + System.Convert.ToString(this.Universal ? "1" : "0") + ",";
        sql += "orderEmail=" + da.SqlEncode(this.orderEmail) + ",";
        sql += "basketMode=" + da.SqlEncode(this.basketMode) + ",";
        sql += "basketURL=" + da.SqlEncode(this.basketURL);

        sql += " WHERE ID = " + System.Convert.ToString(this.ID);

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

    public clsChannel(clsChannel Parent, string Name, string BusinessName, string Address, string code, clsRegion Region, nullableString pic1, nullableString pic2, nullableString url, int priceConfig, string treepath, string focus, float marginMin, float MarginMax, string MarginType, string SchemeOverride, string Legal, clsCurrency DefaultCurrency, bool universal, string orderEmail, string basketMode, string basketURL, DataTable writecache, ref int nextID)
    {

        //EVERY channel is created AS A CLONE, AND PARENT OF ITSELF
        //Those that are actually clones of something else are subsequenty UPDATED

        Guid aguid = new Guid();
        this.WebToken = System.Convert.ToString(aguid.ToString("D"));

        //This bit is important to understand - where isCloneOf is passed as nothing .. it will insert a row that refers to itself

        string pid = "";
        if (Parent == null)
        {
            pid = "null";
        }
        else
        {
            pid = Parent.ID.ToString();
        }

        this.Name = Name;
        this.BusinessName = BusinessName;
        this.Address = Address;
        this.IsCloneOf = this;

        this.Code = code.Trim();
        this.Region = Region;

        if ((DefaultCurrency == null) && (!(iq.DefaultCurrencies == null)) && (iq.DefaultCurrencies.ContainsKey("USD")))
        {
            DefaultCurrency = iq.DefaultCurrencies("USD");
        }

        this.Users = new Dictionary<int, clsUser>();
        this.Teams = new Dictionary<int, clsTeam>();

        this.pic1 = pic1;
        this.pic2 = pic2;
        this.URL = url;
        this.priceConfig = (short)priceConfig;
        this.Children = new Dictionary<int, clsChannel>();
        this.TreePath = treepath;
        this.Focus = focus;
        this.Domains = new List<string>();
        this.marginMax = MarginMax; //These are the margins applied via buttons in the basket
        this.marginMin = marginMin;
        this.marginType = MarginType;
        this.SchemeOverride = SchemeOverride;
        this.Legal = Legal;
        this.DefaultCurrency = DefaultCurrency;
        this.Universal = universal;
        this.orderEmail = orderEmail;
        this.basketMode = basketMode;
        this.basketURL = basketURL;

        if (writecache == null)
        {

            object sql = null;
            sql = "INSERT INTO Channel (fk_channel_id_parent,Name,BusinessName,Address,fk_region_id,webtoken,code,pic1,pic2,url,FK_Channel_ID_CloneOf,priceconfig,treepath,focus,marginMax,marginMin,marginType,SchemeOverride,legal,FK_Currency_ID_Default,Universal,orderEmail,basketMode,basketURL) ";
            sql += "VALUES (" + pid + "," + da.SqlEncode(Name) + "," + da.SqlEncode(BusinessName) + "," + da.SqlEncode(Address) + "," + Region.ID + ",\'" + this.WebToken + "\'," + da.SqlEncode(code) + "," + pic1.sqlValue + "," + pic2.sqlValue + "," + URL.sqlValue + ",";
            sql += "IDENT_CURRENT(\'Channel\')," + System.Convert.ToString(priceConfig) + "," + da.SqlEncode(treepath) + "," + da.SqlEncode("focus") + ",";
            sql += MarginMax + "," + System.Convert.ToString(marginMin) + "," + da.SqlEncode(MarginType) + "," + da.SqlEncode(SchemeOverride) + "," + da.SqlEncode(Legal) + "," + DefaultCurrency.ID + "," + System.Convert.ToString(universal ? "1" : "0") + "," + da.SqlEncode(orderEmail) + "," + da.SqlEncode(basketMode) + "," + da.SqlEncode(basketURL);
            sql += ");"; //<<COID

            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        }
        else
        {

            if (nextID == -1)
            {
                Debugger.Break();
            }

            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();

            this.ID = nextID;

            //            row("ID") = Me.ID - there are 'autonumbers'

            row["FK_channel_id_parent"] = pid == "null" ? DBNull.Value : pid;
            row["Name"] = Name;
            row["BusinessName"] = BusinessName;
            row["Address"] = Address;
            row["fk_region_id"] = Region.ID;
            row["webtoken"] = this.WebToken;
            row["code"] = code;
            row["pic1"] = pic1.value;
            row["pic2"] = pic2.value;
            row["url"] = URL.value;
            row["FK_Channel_ID_cloneof"] = nextID; //  coid - clone of self
            row["priceconfig"] = priceConfig;
            row["treepath"] = treepath;
            row["focus"] = focus;
            row["MarginMax"] = MarginMax;
            row["MarginMin"] = marginMin;
            row["MarginType"] = MarginType;
            row["schemeOverride"] = MarginType;
            row["Legal"] = Legal;
            row["FK_Currency_ID_Default"] = DefaultCurrency.ID;
            row["universal"] = universal;
            row["orderemail"] = orderEmail;
            row["basketMode"] = basketMode;
            row["basketURL"] = basketURL;

            nextID++;
            writecache.Rows.Add(row);

        }

        iq.Channels.Add(this.ID, this);

        // If Not iq.i_channel_code.ContainsKey(Me.Code) Then  'this shouldn't be needed and imples a problem
        iq.i_channel_code.Add(this.Code, this);
        //   End If

        if (iq.Channels.Count == 1)
        {
            if (!(this.Parent == null))
            {
                Debugger.Break(); //The root channel should not have a parent
            }
            iq.RootChannel = this;
        }

        if (!(this.Parent == null))
        {
            this.Parent.Children.Add(this.ID, this); //add me to my parents children (to create the heirarchy)
        }

        this.CustomerAccounts = new Dictionary<int, clsAccount>();
        this.Campaigns = new Dictionary<int, clsCampaign>();


        this.Margin = new Dictionary<clsChannel, Dictionary<clsSector, clsMargin>>();
        //        SKUs = New Dictionary(Of clsProduct, clsVariant)

        this.pricesLoadedFor = new Dictionary<clsPriceBand, int>(); //used on sellerchannel - to indicate which buyers prices have been loaded for
        this.listPricesLoadedFor = new Dictionary<clsRegion, int>();
        this.i_variantCK = new Dictionary<string, clsVariant>(); //DistiSKU|Warehouse>Variant


    }
    public clsChannel()
    {

        this.ID = -1;
        this.Parent = null;

        this.Users = new Dictionary<int, clsUser>();

        this.Region = iq.Regions.Values(0);
        this.Teams = new Dictionary<int, clsTeam>();
        this.CustomerAccounts = new Dictionary<int, clsAccount>(); //the people this channel sells to

        Guid aguid = new Guid();
        this.WebToken = System.Convert.ToString(aguid.ToString("D"));

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

        this.pricesLoadedFor = new Dictionary<clsPriceBand, int>(); //used on sellerchannel - to indicate how many prices have  been loaded for each buyer
        this.listPricesLoadedFor = new Dictionary<clsRegion, int>();

        this.Campaigns = new Dictionary<int, clsCampaign>();
        this.i_variantCK = new Dictionary<string, clsVariant>();

    }

    public clsChannel(int ID, clsChannel Parent, string Name, string BusinessName, clsChannel CloneOf, string Address, string code, clsRegion region, string webtoken, nullableString pic1, nullableString pic2, nullableString url, int priceConfig, string treepath, string focus, float marginMin, float MarginMax, string MarginType, string SchemeOverride, string Legal, clsCurrency DefaultCurrency, bool universal, string orderEmail, string basketMode, string basketURL)
    {

        this.ID = ID;
        this.Parent = Parent;
        this.Name = Name;
        this.BusinessName = BusinessName;
        this.IsCloneOf = IsCloneOf;
        this.Address = Address;
        this.Region = region;
        this.Code = code.Trim();
        this.Users = new Dictionary<int, clsUser>();
        this.Teams = new Dictionary<int, clsTeam>();
        this.WebToken = webtoken;
        this.pic1 = pic1;
        this.pic2 = pic2;
        this.URL = url;
        this.priceConfig = (short)priceConfig;
        this.TreePath = treepath;
        this.Focus = focus;
        this.Domains = new List<string>();
        this.marginMax = MarginMax; //These are the margins applied via buttons in the basket
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
        {
            this.IsCloneOf = this; //!IMPORTANT  (when we're loding channels from the DB they're not yet in the dictionary - so we cant point them to themselves - this work's around that
        }
        this.Children = new Dictionary<int, clsChannel>();

        iq.Channels.Add(this.ID, this);
        iq.i_channel_code.Add(this.Code, this);

        if (iq.Channels.Count == 1)
        {
            if (!(this.Parent == null))
            {
                Debugger.Break(); //The root channel should not have a parent
            }
            iq.RootChannel = this;
        }

        if (!(this.Parent == null))
        {
            this.Parent.Children.Add(this.ID, this); //add me to my parents children (to create the heirarchy)
        }

        CustomerAccounts = new Dictionary<int, clsAccount>();
        Margin = new Dictionary<clsChannel, Dictionary<clsSector, clsMargin>>();
        this.pricesLoadedFor = new Dictionary<clsPriceBand, int>(); //used on sellerchannel - how many prices loaded for each buyer
        this.listPricesLoadedFor = new Dictionary<clsRegion, int>();

        this.Campaigns = new Dictionary<int, clsCampaign>();
        this.i_variantCK = new Dictionary<string, clsVariant>();

        //moved into Product.variants
        //SKUs = New Dictionary(Of clsProduct, Dictionary(Of clsVariant, String)) ' This channels 'internal' SKU for each product (they sell) - the first dimension contains a compound key of Product.id+Variant.id

    }

    public bool IsUniversal()
    {
        return this.Code.EndsWith("U"); //i_variant_distisku.Count = 0 'TODO ML have no idea if this is a measure of if they are univeral, need some input on this one...
    }

    /// <summary>
    /// Check if a particular scheme is enabled for this account and region
    /// </summary>
    /// <param name="scheme">The char denoting the scheme to check (F,A, etc)</param>
    /// <returns>...</returns>
    /// <remarks></remarks>
    public bool SchemeEnabled(char scheme)
    {
        switch (scheme)
        {
            case 'F':
                if (IsUniversal() && !SchemeOverride.Split(',').Contains("F"))
                {
                    return false;
                }
                else
                {
                    return true;
                }
                break;
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
        {
            return true;
        }
        return false;
    }
    public dynamic DecodedPriceConfig()
    {
        if (priceConfig & 8)
        {
            return "Web Service";
        }
        if (priceConfig & 4)
        {
            return "Brice Band/Feed File";
        }
        if (priceConfig & 2)
        {
            return "List Price";
        }
        if (priceConfig & 1)
        {
            return "Show products with no price with \'...\' (Don\'t Hide them completely)";
        }
    }

    public string CompoundDisplayName
    {
        get
        {
            return Name + " - " + this.Code;
        }
    }
} //clsChannel