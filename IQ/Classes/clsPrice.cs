using dataAccess;
using System.Threading;


public class clsPrice
{

    //A price links a Buyer and a SkuVariant (which is a Seller, product, warehouse combo)

    public int ID { get; set; }
    // Public Product As clsProduct
    //    Property Seller As clsChannel 'note - there is no seller, op product here.. they come from the variant
    public clsVariant SKUVariant { get; set; }
    //    Property Buyer As clsChannel        'can be 'Everyone' (for list prices)
    public clsPriceBand PriceBand { get; set; }
    public nullablePrice Price { get; set; } //contains the currency, and message
    public Dictionary<int, clsOffer> Offers { get; set; } //Not implemented (a generalised offer framework to encompass Flex, Avalanche, Bundles and more)

    public DateTime lastRequested { get; set; } //when it was last requested (webservice request fired off)
    public DateTime lastUpdated { get; set; } //When it was last successfully updated (webservice result/update)

    public string Source { get; set; }

    public int tempID; //temporary negative ID used to allow late INSERTS (when the webservice returns a price)

    public clsPrice insert()
    {

        return new clsPrice(this.SKUVariant, this.PriceBand, this.Price, this.Source, null, -1);

    }

    public clsPrice clone(clsVariant SkuVariant)
    {
        clsPrice returnValue = default(clsPrice);

        returnValue = new clsPrice(SkuVariant, this.PriceBand, this.Price, "cloned", null, -1);

        return returnValue;
    }

    public clsPrice(clsAccount buyeraccount, clsVariant SKUvariant)
    {

        //POA - NB: Does not add the price to the Product, or Insert to the database

        this.ID = -1;
        //Me.Product = Product
        this.SKUVariant = SKUvariant;
        this.Price = new nullablePrice(buyeraccount.Currency);
        this.PriceBand = buyeraccount.Priceband;
        //       Me.Seller = buyeraccount.SellerChannel
        this.Source = "Contact the seller for current pricing";
        this.lastRequested = DateAndTime.DateAdd(DateInterval.Day, -1, DateTime.Now);
        this.lastUpdated = this.lastRequested;
        this.Offers = new Dictionary<int, clsOffer>();

        if (this.SKUVariant != null)
        {
            this.SKUVariant.i_prices.Add(buyeraccount.Priceband, new Dictionary<clsCurrency, clsPrice>());
            this.SKUVariant.i_prices(buyeraccount.Priceband).Add(buyeraccount.Currency, this);
            this.SKUVariant.prices.Add(this.ID, this);
        }

    }


    public clsPrice(clsPrice BasePrice, float Factor)
    {

        //creates a new price - from an exsisting (typically list) price - multiplied by the specified factor (margin based pricing

        this.ID = -1;
        this.SKUVariant = BasePrice.SKUVariant;
        this.Price = new NullablePrice(BasePrice.Price.value, BasePrice.Price.currency, false);
        //Note - we do not preserve isList - as multiplying a list price by a factor... makes it no longer a list price ! (OK.. unless it's 1 - smart arse)

        this.PriceBand = BasePrice.PriceBand;
        this.Source = "Margin based price";
        this.Price.Message = "Estimated (Margin based) price";

        this.Offers = new Dictionary<int, clsOffer>();
        //                                                                                                   buyer
        //the vairant holds a dictionary of prices ..  Property prices As Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))  'TODO fill ?

        //this is a special constructore - we don't want to add marging based prices to the editable dictionary

        //     Me.SKUVariant.i_prices(Me.Buyer).Add(BasePrice.Price.currency, Me)
        //     Me.SKUVariant.prices.Add(Me.ID, Me)

    }

    public clsPrice()
    {

        this.Offers = new Dictionary<int, clsOffer>();

    }


    //Public Sub New(product As clsProduct, SKUvariant As clsVariant, buyeraccount As clsAccount, source As String)

    //    'this particular oveload is *just* to make the temporary 'POA' ones


    //    Dim POAprice As clsPrice
    //    POAprice = New clsPrice(product, SKUvariant, buyeraccount.SellerChannel, buyeraccount.BuyerChannel, New nullablePrice(buyeraccount.Currency), "Webservice")
    //    Return POAprice

    //    'Me.ID = -1
    //    'Me.Product = product
    //    'Me.SKUVariant = SKUvariant
    //    'Me.Seller = buyeraccount.SellerChannel
    //    'Me.Price = New nullablePrice(buyeraccount.Currency)
    //    'Me.Buyer = Buyer
    //    'Me.Source = "Requesting Price . ."
    //    'Me.Price.Message = ""

    //    'Me.Offers = New Dictionary(Of Integer, clsOffer)

    //End Sub



    // VBConversions Note: Former VB static variables moved to class level because they aren't supported in C#.
    static int temporaryID_countdown = 0;

    public static dynamic temporaryID()
    {
        dynamic returnValue = default(dynamic);

        //assigned the next avialble (and negative) temporary ID for a (in memory only for now) clsPrice
        //INSERTs which would normally yield an ID are very slow - and we don't actually want to persist a record until it has a price (back from the webserivce) anyway


        object @lock = new object();

        lock (@lock)
        {
            // static int countdown = 0; VBConversions Note: Static variable moved to class level and renamed temporaryID_countdown. Local static variables are not supported in C#.
            temporaryID_countdown--;

            returnValue = temporaryID_countdown;
        }



        return returnValue;
    }

    public clsPrice(clsVariant SKUvariant, clsPriceBand Priceband, NullablePrice price, string source, DataTable writecache, ref int nextID)
    {

        //If buyer Is Nothing Then Stop

        Pmark("Price_NEW (db)");

        if (writecache == null)
        {
            object sql = null;

            if (Price.sqlvalue == 0)
            {

                this.ID = System.Convert.ToInt32(temporaryID());
            }
            else
            {

                sql = "INSERT INTO PRICE(fk_variant_id,priceband,price,fk_currency_id,datestamp,source) VALUES ";
                sql += "(" + SKUVariant.ID + "," + da.SqlEncode(PriceBand.text) + "," + Price.sqlvalue + "," + Price.currency.ID + ",getdate()," + da.SqlEncode(source) + ");";

                try
                {
                    this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));

                }
                catch (System.Exception ex)
                {

                    Logit(ex.Message.ToString());
                    if (ex.InnerException != null)
                    {
                        Logit(ex.InnerException.Message.ToString());
                    }
                    Logit("In clsPrice_New " + System.Convert.ToString(sql));
                    //Stop

                    //Beep()

                }
            }


        }
        else
        {

            this.ID = nextID; //
            nextID++;
            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();
            //  row("FK_product_id") = product.ID
            //  row("FK_Channel_id_seller") = seller.ID

            if (nextID != -1)
            {
                row["ID"] = nextID; //NEW
            }

            row["FK_variant_id"] = SKUVariant.ID;

            row["Priceband"] = PriceBand.text;
            //If Buyer Is Nothing Then
            //    row("FK_Channel_id_buyer") = DBNull.Value
            //Else
            //    row("FK_Channel_id_buyer") = buyer.ID
            //End If

            row["price"] = Price.value;
            row["fk_currency_id"] = Price.currency.ID;
            row["fk_variant_id"] = SKUVariant.ID;
            row["datestamp"] = DateTime.Now;
            row["source"] = source + "(bulk)";

            writecache.Rows.Add(row);

        }

        this.Price = price;
        this.SKUVariant = SKUvariant;
        this.lastUpdated = DateTime.Now;
        this.lastRequested = DateTime.Now;

        this.Source = source;
        this.PriceBand = Priceband;

        //add myself to the master price list
        //  iq.Prices.Add(Me.ID, Me)

        //add into the products i_prices  - Obsoleted as the Products.Variants(sellerchannel) now provides a natural index
        //SKUvariant.Product.AddPrice(Me)

        this.Offers = new Dictionary<int, clsOffer>();
        if (!this.SKUVariant.i_prices.ContainsKey(Priceband))
        {
            this.SKUVariant.i_prices.Add(this.PriceBand, new Dictionary<clsCurrency, clsPrice>());
        }


        if (this.SKUVariant.i_prices(this.PriceBand).ContainsKey(this.Price.currency))
        {
            this.SKUVariant.i_prices(this.PriceBand)[this.Price.currency] = this;
        }
        else
        {
            this.SKUVariant.i_prices(this.PriceBand).Add(this.Price.currency, this);
        }

        if (this.ID == 0)
        {
            throw (new Exception("attempted to add a price with an ID of 0"));
        }

        if (this.ID != -1) //for an insert WITHOUt the write cache - me.ID will be the ID generated by the SQL INSERT
        {
            this.SKUVariant.prices.Add(this.ID, this);
        }

        if (this.ID != -1 && da.DatabaseAlive) //TODO prices rely on DB
        {
            iq.Prices.Add(this.ID, this);
        }

        Pacc("Price_NEW (db)");

    }

    public Panel Ui(clsAccount buyeraccount, float margin, UInt64 lid)
    {
        Panel returnValue = default(Panel);

        List<string> errorMessages = new List<string>();

        returnValue = new Panel();

        //If Me.SKUVariant Is Nothing Or Me.SKUVariant.Product Is Nothing Then
        //    lbl.Text = "POA"
        //    Ui.BackColor = Drawing.Color.Red
        //    lbl.ToolTip = "Price " & Me.ID & " has no Product "
        //Else

        returnValue.ID = "P_" + System.Convert.ToString(this.ID);
        returnValue.CssClass = "P_" + System.Convert.ToString(this.ID);
        returnValue.CssClass += " Refresh"; //this class doesn't exist - but is used by the script to identify those elements to refresh - so DONT REMOVE IT !!!
        returnValue.CssClass += " PriceUI";

        if (this.MinutesOld() > 60)
        {
            returnValue.CssClass += " unconfirmed";
        }
        else
        {
            returnValue.CssClass += " upToDate";
        }

        NullablePrice priceIncludingMargin = this.Price * margin;
        //lbl.Text = Me.Price * margin.DisplayPrice(buyeraccount, errorMessages).Text

        Panel pp = priceIncludingMargin.DisplayPrice(buyeraccount, errorMessages);

        if (false) //'REINSTATE for price source info
        {
            pp.ToolTip = this.Source + "\r\n";
            pp.Attributes("Style") += "background-color:blue;padding:5px;";

            string rcode = "";
            if (this.SKUVariant.Region != null)
            {
                rcode = System.Convert.ToString(this.SKUVariant.Region.Code);
            }
            pp.ToolTip += "VarID:" + this.SKUVariant.ID + " ProdID:" + this.SKUVariant.Product.ID + "\r\n";
            pp.ToolTip += "DistiSKU:" + this.SKUVariant.DistiSku + " Warehouse:" + SKUVariant.Warehouse + " region:" + rcode + "\r\n";
            pp.ToolTip += "SlrID:" + this.SKUVariant.sellerChannel.ID + " SlrCode:" + this.SKUVariant.sellerChannel.Code + "\r\n";
            pp.ToolTip += "PriceBand:" + this.PriceBand.text;

        }

        //Brazil Changes
        if (buyeraccount.BuyerChannel.Region.Code == "BR")
        {
            pp.ToolTip = iq.AddTranslation("For actual prices and stock levels, please set the Customer Context", English, "", 1, null, 0, false).text(buyeraccount.Language);
        }

        // End If

        returnValue.Controls.Add(pp); //price panel
        OutputErrors(returnValue.Controls, errorMessages, lid);

        return returnValue;
    }
    public int MinutesOld()
    {
        return DateAndTime.DateDiff(DateInterval.Minute, this.lastUpdated, DateTime.Now); //how old is it at the moment (when was it last *requested* )
    }

    public clsPrice Update()
    {

        //If Me.ID = -1 Then Stop 'TODO REMOVE
        //If LCase(Me.Price.sqlvalue) = "null" Then Stop

        this.lastUpdated = DateTime.Now;

        object sql = null;
        sql = "UPDATE PRICE";
        sql += " SET price=" + this.Price.sqlvalue + ",";
        sql += " datestamp=" + da.UniversalDate(DateTime.Now);
        if (this.Source != null)
        {
            sql += ",source=" + da.SqlEncode(this.Source);
        }
        sql += " WHERE ID=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql);

        return this;

    }

    public clsPrice(int id, clsVariant SKUvariant, clsPriceBand priceband, decimal price, clsCurrency currency, DateTime datestamp, string source)
    {

        //If buyer Is Nothing Then Stop

        this.ID = id;
        this.SKUVariant = SKUvariant;
        // Me.Buyer = buyer

        bool islistPrice = (SKUVariant.sellerChannel == HP) && PriceBand.text == ""; //=(buyer Is Everyone)

        this.Price = new NullablePrice(price, currency, islistPrice);
        this.lastUpdated = datestamp; //NOT Now (otherwise datestamps are not restored from the database properly)
        this.lastRequested = datestamp;
        this.Source = source;

        this.Offers = new Dictionary<int, clsOffer>();

        //add myself to the master price list
        iq.Prices.Add(this.ID, this);
        this.PriceBand = priceband;


        this.SKUVariant.prices.Add(this.ID, this);
        if (!this.SKUVariant.i_prices.ContainsKey(this.PriceBand))
        {
            this.SKUVariant.i_prices.Add(this.PriceBand, new Dictionary<clsCurrency, clsPrice>());
        }

        if (this.SKUVariant.i_prices(this.PriceBand).ContainsKey(currency))
        {
            // Beep() 'ut oh - we already had a price for that buyer in that currency
            bool a = false;
        }
        else
        {
            this.SKUVariant.i_prices(this.PriceBand).Add(currency, this);
        }

    }

}