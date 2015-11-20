using dataAccess;


public class clsMargin
{

    //A margin is an amount of money made on the sale of each product
    //It is expressed as a factor - by which the base price for each product is multiplied
    //(each seller has a base price for each product.. see Product.prices(seller))

    //A factor of 1.05 is 'cost plus 5%'
    //retained margins can also be expressed as a simple factor, 5% retained margin is a factor of 1.052631578947368
    //each (selling) channel - has a dictionary of Margins for each of its buying customers

    //Retained/CostPlus margin calculations

    //SELECT @value=CONVERT(Decimal(12,2),CONVERT(Money, CASE @mode '
    //WHEN 'ret' THEN 1/(1-(@margin*0.01))*@value
    //WHEN 'cplus' THEN @value*(100+@margin)*0.01 END))
    //Return @value
    //END

    public int ID { get; set; }
    public clsChannel Seller; //these are members, not properties - becuase we don't want them exposed for editing
    public clsChannel buyer;
    public float Factor { get; set; }
    //Property ProductType As clsProductType - We should replace Sector with a Hierarchy of product types Where the current sectors are the l1 Brances (ISS/PSG) - L2 might be system types - with a sepeate set of optios
    public string SampledSKU { get; set; }
    public clsSector Sector { get; set; }
    public string PriceBand { get; set; } //which price is used as the source for prices generated via this margin
    private bool _bad = false; //used during import - inconsistent margins ar removed and replaced with per buyer/seller/vairant prices
    public bool bad
    {
        get
        {
            return _bad;
        }
        set
        {
            _bad = value;
        }
    }

    public clsMargin(int id, clsChannel seller, clsChannel buyer, float factor, string priceband, clsSector sector, string sampledSKU)
    {

        this.ID = id;
        this.Seller = Seller;
        this.buyer = buyer;
        this.Factor = factor;
        //   Me.ProductType = producttype
        this.SampledSKU = sampledSKU;
        this.Sector = sector;
        this.PriceBand = priceband;

        //add the margin for this ProductType to the correct seller/buyer
        if (!seller.Margin.ContainsKey(buyer))
        {
            seller.Margin.Add(buyer, new Dictionary<clsSector, clsMargin>());
        }

        //If Not seller.Margin(buyer).ContainsKey(sector) Then
        // seller.Margin(buyer).Add(sector, New Dictionary(Of clsProductType, clsMargin))
        // End If


    }

    public clsMargin(clsChannel seller, clsChannel buyer, float factor, string Priceband, clsSector sector, string sampledsku)
    {

        object sql = null;
        sql = "INSERT INTO Margin (fk_Channel_id_seller,fk_channel_id_buyer,factor,priceband,sampledsku,fk_sector_id) ";
        sql += " VALUES (" + Seller.ID + "," + buyer.ID + "," + System.Convert.ToString(factor) + "," + da.SqlEncode(Priceband) + "," + da.SqlEncode(sampledsku) + "," + Sector.ID + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));

        //call the 'other' constructor - to get it added to the seller.margin(buyer) dictionary
        clsMargin aMargin;
        aMargin = new clsMargin(this.ID, seller, buyer, factor, Priceband, sector, sampledsku);

        this.Sector = sector;

    }


}