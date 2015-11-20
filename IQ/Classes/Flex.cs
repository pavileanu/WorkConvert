using dataAccess;
using System.IO;


public class clsFlexOPG
{

    public int ID { get; set; }
    public string OPGRef { get; set; }
    public string Description { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public clsCurrency Currency { get; set; }
    public int MinOptions;
    public int MaxOptions;
    public string OPGSysType { get; set; }
    public Dictionary<int, clsFlexRule> Rules;
    public Dictionary<int, clsFlexLine> Lines;
    //Public Regions As Dictionary(Of Integer, clsFlexRegion) 'NOTE - this is NOT a dictionary of clsRegions! (the clsFlexRegion allows the required many:many relationship - mostly for editing)
    public Dictionary<int, clsRegion> regions;

    //Public Sub serialize(Sw As streamwriter)

    //    'for any object - you only need write the ID
    //    'For any dictionary - you write the IDs

    //End Sub

    //Public Sub deSerialize(sr As streamreader)

    //End Sub

    public clsFlexOPG(int ID, string OPGref, string Description, DateTime validFrom, DateTime validTo, clsCurrency Currency, int minOptions, int maxOptions, string OPGSysType)
    {

        this.ID = ID;
        this.OPGRef = OPGref;
        this.Description = Description;
        this.ValidFrom = validFrom;
        this.ValidTo = validTo;
        this.Currency = Currency;
        this.MinOptions = MinOptions;
        this.MaxOptions = MaxOptions;
        this.OPGSysType = OPGSysType;

        this.Rules = new Dictionary<int, clsFlexRule>();
        this.Lines = new Dictionary<int, clsFlexLine>();
        this.regions = new Dictionary<int, clsRegion>(); //clsFlexRegion)
        iq.FlexOPGs.Add(this.ID, this);

    }

    public clsFlexOPG(string OPGref, string Description, DateTime validFrom, DateTime validTo, clsCurrency Currency, int MinOptions, int maxOptions, string OPGSysType, DataTable dt = null)
    {

        this.ID = ID;
        this.OPGRef = OPGref;
        this.Description = Description;
        this.ValidFrom = validFrom;
        this.ValidTo = validTo;
        this.Currency = Currency;
        this.MinOptions = MinOptions;
        this.MaxOptions = maxOptions;
        this.OPGSysType = OPGSysType;

        this.Rules = new Dictionary<int, clsFlexRule>();
        this.Lines = new Dictionary<int, clsFlexLine>();
        this.regions = new Dictionary<int, clsRegion>(); // clsFlexRegion)

        if (dt == null)
        {
            string Sql = " INSERT INTO Flex (OPGref,description,validFrom,validTo,FK_currency_ID,minoptions,maxoptions,OPGSysType) ";
            Sql += "values (" + this.OPGRef + "," + da.SqlEncode(Description) + "," + da.UniversalDate(this.ValidFrom) + "," + da.UniversalDate(this.ValidTo) + ",";
            Sql += this.Currency.ID + "," + System.Convert.ToString(this.MinOptions) + "," + System.Convert.ToString(this.MaxOptions) + "," + da.SqlEncode(this.OPGSysType) + ");";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(Sql, true));

            iq.FlexOPGs.Add(this.ID, this);
        }
        else
        {
            DataRow dr = default(DataRow);
            dr = dt.NewRow;
            dr.Item("OPGRef") = this.OPGRef;
            dr.Item("Description") = this.Description;
            dr.Item("validFrom") = this.ValidFrom;
            dr.Item("validTo") = this.ValidTo;
            dr.Item("FK_Currency_ID") = this.Currency.ID;
            dr.Item("MinOptions") = this.MinOptions;
            dr.Item("MaxOptions") = this.MaxOptions;
            dr.Item("OPGSysType") = this.OPGSysType;

            dt.Rows.Add(dr);
            this.ID = -1;
        }
    }
    public clsFlexRule getRule(clsProductType ProductType)
    {
        clsFlexRule returnValue = default(clsFlexRule);

        System.Boolean r = from v in this.Rules.Values where v.ProductType == ProductType select v;
        if (r.Any)
        {
            returnValue = r.First;
        }
        else
        {
            returnValue = null;
        }

        return returnValue;
    }
    public bool isCurrent()
    {
        bool returnValue = false;

        returnValue = false;
        if (this.ValidFrom < DateTime.Now && this.ValidTo > DateTime.Now)
        {
            returnValue = true;
        }

        return returnValue;
    }

    public bool AppliesToRegion(clsRegion region)
    {
        bool returnValue = false;

        returnValue = false;

        //    If Me.Regions.Values.Count = 0 Then Stop

        foreach (var r in this.regions.Values) //Each flexOPG potentially applies in many regions (becuase although regions are a hierarchy, a single region doesn't quite cut it (eg. Peru & Mexico)
        {
            if (r.Encompasses(region))
            {
                returnValue = true;
                break;
            }
        }

        return returnValue;
    }

    /// <summary>Returns the FlexLines from this FlexOPG which match the supllied critera</summary>
    public List<clsFlexLine> MatchingFlexLines(clsProduct product = null, int qty = 0, object dateTime = null, clsRegion region = null)
    {
        List<clsFlexLine> returnValue = default(List<clsFlexLine>);

        Pmark("matchingFlexLines");
        //returns the FlexLines (containing rebate information)  is for the sepcified prodType,qty..etc (which are all optional)

        returnValue = new List<clsFlexLine>();

        bool dateValid = false;
        if (dateTime == null)
        {
            dateValid = true;
        }
        else
        {
            if (dateTime > this.ValidFrom && dateTime < this.ValidTo)
            {
                dateValid = true;
            }
        }

        bool regionValid = false;
        if (region == null)
        {
            regionValid = true;
        }
        else
        {
            foreach (var r in this.regions.Values) //Each flexOPG potentially applies in many regions (becuase although regions are a hierarchy, a single region doesn't quite cut it (eg. Peru & Mexico)
            {
                if (r.Encompasses(region))
                {
                    regionValid = true;
                }
                break;
            }
        }

        if (dateValid && regionValid)
        {

            //build an index to look up rules for each product type quickly (this should probably go in the clsFlex object for speed)
            Dictionary<clsProductType, clsFlexRule> i_rules_producttype = new Dictionary<clsProductType, clsFlexRule>();
            foreach (var r in this.Rules.Values)
            {
                i_rules_producttype.Add(r.ProductType, r);
            }

            foreach (var flexline in this.Lines.Values)
            {

                if (flexline.Product == product || product == null)
                {

                    clsFlexRule rule = null;
                    if (i_rules_producttype.ContainsKey(flexline.Product.ProductType))
                    {
                        rule = i_rules_producttype[flexline.Product.ProductType];
                    }

                    bool rulevalid = false;
                    if (rule == null)
                    {
                        rulevalid = true;
                    }
                    else
                    {
                        if ((qty >= rule.min & qty <= rule.max) || qty == 0)
                        {
                            rulevalid = true;
                        }
                    }

                    if (rulevalid)
                    {
                        returnValue.Add(flexline);
                    }
                }
            }
        }

        Pacc("matchingFlexLines");

        return returnValue;
    }


}


public class clsFlexLine
{
    public int ID { get; set; }
    public clsFlexOPG FlexOPG { get; set; }
    public clsProduct Product { get; set; }
    public decimal rebate { get; set; }
    public DateTime validFrom { get; set; }
    public DateTime validTo { get; set; }

    public clsFlexLine(int ID, clsFlexOPG FlexOPG, clsProduct Product, float Rebate, DateTime validFrom, DateTime validTo)
    {

        this.ID = ID;
        this.FlexOPG = FlexOPG;
        this.Product = Product;
        this.rebate = (decimal)Rebate;
        this.validFrom = validFrom;
        this.validTo = validTo;

        Product.OPGflexLines.Add(this.ID, this);
        FlexOPG.Lines.Add(this.ID, this);

    }

    public clsFlexLine(clsFlexOPG FlexOPG, clsProduct Product, float rebate, DateTime validFrom, DateTime validTo, DataTable dt = null)
    {

        this.ID = ID;
        this.FlexOPG = FlexOPG;
        this.Product = Product;
        this.rebate = (decimal)rebate;
        this.validFrom = validFrom;
        this.validTo = validTo;

        if (dt != null)
        {


            DataRow dr = dt.NewRow;
            dr["fk_flex_id"] = FlexOPG.ID;
            dr["FK_Product_id"] = Product.ID;
            dr["rebate"] = rebate;
            dr["validFrom"] = validFrom;
            dr["validTo"] = validTo;
            dt.Rows.Add(dr);
            this.ID = -1;
        }
        else
        {
            string sql = "INSERT INTO FlexLine (FK_Product_ID,rebate,FK_Flex_ID,validFrom,ValidTo) VALUES ";
            sql += "(" + this.Product.ID + "," + System.Convert.ToString(this.rebate) + "," + System.Convert.ToString(this.FlexOPG.ID) + "," + da.UniversalDate(this.validFrom) + "," + da.UniversalDate(this.validTo) + ");";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
            FlexOPG.Lines.Add(this.ID, this);
            Product.OPGflexLines.Add(this.ID, this);

        }


    }

    public bool isCurrent()
    {
        bool returnValue = false;

        returnValue = false;
        if (this.validFrom < DateTime.Now && this.validTo > DateTime.Now)
        {
            returnValue = true;
        }

        return returnValue;
    }


}

public class clsFlexRule
{
    public int ID { get; set; }
    public clsProductType ProductType { get; set; }
    public int min { get; set; } //NullableInt
    public int max { get; set; }
    public bool optionalRule { get; set; }
    public clsFlexOPG flexOPG { get; set; }

    public clsFlexRule(int ID, clsFlexOPG flexOPG, clsProductType ProductType, int min, int max, bool optionalRule)
    {

        this.ID = ID;
        this.ProductType = ProductType;
        this.min = min;
        this.max = max;
        this.optionalRule = optionalRule;
        FlexOPG.Rules.Add(this.ID, this);

    }

    public clsFlexRule(clsFlexOPG FlexOPG, clsProductType ProductType, int min, int max, bool optionalRule, DataTable dt)
    {

        this.flexOPG = FlexOPG;
        this.ProductType = ProductType;
        this.min = min;
        this.max = max;
        this.optionalRule = optionalRule;

        if (dt != null)
        {
            DataRow dr = dt.NewRow;
            dr["FK_Flex_id"] = this.flexOPG.ID;
            dr["FK_ProductType_id"] = this.ProductType.ID;
            dr["min"] = this.min;
            dr["max"] = this.max;
            dr["optionalRule"] = this.optionalRule;
            dt.Rows.Add(dr);
            this.ID = -1;
        }
        else
        {
            string sql = "INSERT INTO FlexRule (FK_Flex_ID,FK_ProductType_ID,[min],[max],[optionalRule]) VALUES ";
            sql += "(" + System.Convert.ToString(this.flexOPG.ID) + "," + this.ProductType.ID + "," + System.Convert.ToString(this.min) + "," + System.Convert.ToString(this.max) + "," + System.Convert.ToString(this.optionalRule) + ");";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
            FlexOPG.Rules.Add(this.ID, this);
        }

    }
}

public class clsFlexRegion //Should Allow an opgs regions to be edited (and presisted) - The editor can get you edit the FlexOPG's regions - but can't 'store' them
{
    public int ID { get; set; }
    public clsFlexOPG FlexOPG { get; set; }
    public clsRegion Region { get; set; }

    public clsFlexRegion(int ID, clsFlexOPG flexOPG, clsRegion Region)
    {

        this.ID = ID;
        this.FlexOPG = flexOPG;
        this.Region = Region;
        flexOPG.regions.Add(this.Region.ID, this.Region); //ME

    }

    public clsFlexRegion(clsFlexOPG FlexOPG, clsRegion region, DataTable dt = null)
    {

        this.FlexOPG = FlexOPG;
        this.Region = region;


        if (dt != null)
        {
            DataRow dr = dt.NewRow;
            dr["FK_Flex_id"] = this.FlexOPG.ID;
            dr["FK_region_id"] = this.Region.ID;
            dt.Rows.Add(dr);
            this.ID = -1;
        }
        else
        {
            string sql = "INSERT INTO FlexRegion(FK_Flex_ID,FK_region_ID,[min],[max]) VALUES ";
            sql += "(" + System.Convert.ToString(this.FlexOPG.ID) + "," + this.Region.ID + ");";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
            this.FlexOPG.regions.Add(this.Region.ID, this.Region);
        }



    }

}