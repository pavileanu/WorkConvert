using dataAccess;
using System.Threading;


public class clsstock //Should really be called 'shipments' - many can exist for a single variant (future shipments)
{

    //Shipments are (real or predicted) absolute stock positions
    //ONE shipment is flagged as current

    public int ID { get; set; }
    //Public Product As clsProduct  'We save memory by *not* having there (at the expense of a more difficult lookup from the Stock Instance)
    // Property Seller As clsChannel
    public clsVariant SKUvariant { get; set; }
    public int quantity { get; set; }
    public DateTime Arrival { get; set; } //The point in time at which this (total) quantity will be available
    public DateTime LastUpdated { get; set; } //timestamp of the record
    public string Source { get; set; }
    public bool IsCurrent { get; set; } //only one stock record per product/variant/seller channel can be current (enforced by a unique index)

    //    Dim oSeller As clsChannel
    //    Dim oProduct As clsProduct
    clsVariant oSKUVariant;
    DateTime oArrival;


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

    public clsstock(clsVariant SKUvariant, int Quantity, DateTime Arrival, string Source, bool iscurrent)
    {

        //If Arrival.Date = CDate("01/01/2000").Date Then
        // Me.IsCurrent = True
        // End If

        //If Me.IsCurrent Then Arrival = CDate("01/01/2000").Date

        //    Me.Product = Product
        //    Me.Seller = Seller
        this.SKUvariant = SKUvariant;
        this.quantity = Quantity;
        this.Arrival = Arrival;
        this.LastUpdated = DateTime.Now;
        this.IsCurrent = iscurrent;


        clsVariant with_1 = SKUvariant;
        if (!with_1.shipments.ContainsKey(this.Arrival)) //Because some rows are duplictaed in the feeds
        {
            with_1.shipments.Add(this.Arrival, this);
        }
        else
        {
            with_1.shipments(this.Arrival) = this;
        }

        //stock is no longer in the products - but is global
        //If Not .Product.i_Stock.ContainsKey(.sellerChannel) Then .Product.i_Stock.Add(.sellerChannel, New Dictionary(Of clsVariant, SortedDictionary(Of Date, clsstock)))
        //If Not .Product.i_Stock(.sellerChannel).ContainsKey(SKUvariant) Then .Product.i_Stock(.sellerChannel)(SKUvariant) = New SortedDictionary(Of Date, clsStock)
        //.Product.i_Stock(.sellerChannel)(SKUvariant).Add(Arrival, Me)
        //  iq.Stock.Add(Me.ID, Me)

        //            oProduct = Product 'keep a record of the 'original' versions - so that we can maintain the index when things are opdated
        //            oSeller = Seller


        if (Quantity == -1)
        {
            this.ID = System.Convert.ToInt32(temporaryID());
        }
        else
        {
            object sql = null;
            sql = "INSERT INTO STOCK(FK_variant_ID, quantity,Arrival,datestamp,isCurrent,source) VALUES ";
            sql += "(" + SKUvariant.ID + "," + System.Convert.ToString(Quantity) + "," + da.UniversalDate(Arrival) + ",getdate()," + System.Convert.ToString(iscurrent ? "1" : "0") + "," + da.SqlEncode(Source) + ");";

            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));

        }

        iq.Stock.Add(this.ID, this);



        oSKUVariant = SKUvariant;
        oArrival = Arrival;

    }

    public clsstock Insert()
    {

        return new clsstock(this.SKUvariant, this.quantity, this.Arrival, this.Source, this.IsCurrent);

    }

    public clsstock(int ID, clsVariant SKUVariant, int Quantity, DateTime Arrival, DateTime datestamp, bool isCurrent, List<string> errormessages)
    {

        this.ID = ID;
        //Me.Product = product
        // Me.Seller = Seller

        this.SKUvariant = SKUVariant;
        this.quantity = Quantity;
        this.Arrival = Arrival;
        this.LastUpdated = datestamp;
        this.IsCurrent = isCurrent;


        if (iq.Stock.ContainsKey(this.ID))
        {

            errormessages.Add("* Duplicate stockID & me.id !");
        }
        else
        {

            iq.Stock.Add(this.ID, this);

            clsVariant with_1 = SKUVariant;

            if (!with_1.shipments.ContainsKey(Arrival))
            {
                with_1.shipments.Add(Arrival, this);
                oSKUVariant = SKUVariant;
                oArrival = Arrival;
            }
            else
            {
                // errormessages.Add("* Duplicate shipment date ! SVID:" & SKUVariant.ID & " (" & Arrival.ToString & ") SID:" & Me.ID)

            }
        }

    }

    public clsstock update()
    {

        object sql = null;
        sql = "UPDATE [Stock] SET quantity =" + System.Convert.ToString(this.quantity) + ",arrival=" + da.UniversalDate(this.Arrival) + ",dateStamp=" + da.UniversalDate(this.LastUpdated) + ",isCurrent=" + System.Convert.ToString(this.IsCurrent ? "1" : "0");
        sql += " WHERE ID = " + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

        oSKUVariant.shipments.Remove(oArrival);
        SKUvariant.shipments(Arrival) = this;

        return this;

    }

    public void Delete()
    {

        object sql = null;
        sql = "DELETE FROM [Stock] WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

        oSKUVariant.shipments.Remove(oArrival);

    }



}