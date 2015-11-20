using dataAccess;


public class ClsAvalancheOPG
{

    //systems carry a list of avalancheOPG's
    public int ID { get; set; }
    public dynamic OPGref { get; set; }
    public clsRegion Region { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int OptMin { get; set; }
    public int OptMax { get; set; }
    //Property LPDiscountPercent As Single 'percent discount off list price (prices(HP))
    public Dictionary<int, ClsAvalancheOption> Options { get; set; }

    public string get_DisplayName(clsLanguage language)
    {
        string returnValue = "";
        returnValue = System.Convert.ToString(OPGref);
        return returnValue;
    }
    public ClsAvalancheOPG Insert()
    {

        ClsAvalancheOPG av = new ClsAvalancheOPG(System.Convert.ToString(this.OPGref), this.Region, this.ValidFrom, this.ValidTo, this.OptMin, this.OptMax);
        return av;

    }

    public List<clsAvalancheOption> getAvalancheOptions(string prodref = "", int qty = 0, object dateTime = null, clsRegion region = null)
    {
        List<clsAvalancheOption> returnValue = default(List<clsAvalancheOption>);

        Pmark("getAvalancheOptions");
        //returns the avalancheOptions (containing % rebate information)  is for the sepcified prodref,qty..etc (which are all optional)

        returnValue = new List<clsAvalancheOption>();

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
            regionValid = System.Convert.ToBoolean(this.Region.Encompasses(region));
        }

        if (dateValid)
        {
            if ((qty >= this.OptMin & qty <= this.OptMax) || qty == 0)
            {
                if (regionValid)
                {
                    foreach (var o in this.Options.Values)
                    {
                        if (o.ProdRef == prodref || prodref == "")
                        {
                            //avalanche gives a discount as a percentage of list price
                            returnValue.Add(o);
                        }
                    }
                }
            }
        }

        Pacc("getAvalancheOptions");

        return returnValue;
    }

    public void Update()
    {

        object sql = null;
        sql = "UPDATE Avalance SET ";
        sql += "opgRef=" + System.Convert.ToString(this.OPGref) + ",";
        //  sql$ &= "prodRef=" & Me.ProdRef & ","
        sql += "FK_region_id=" + this.Region.ID + ",";
        sql += "validFrom=" + da.universaldate(this.ValidFrom) + ",";
        sql += "validTo=" + da.universaldate(this.ValidTo) + ",";
        // sql$ &= "LPDiscountPercent =" & Me.LPDiscountPercent & ","
        sql += "optMin=" + System.Convert.ToString(this.OptMin) + ",";
        sql += "optMax=" + System.Convert.ToString(this.OptMax) + "";
        sql += " WHERE ID = " + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql);

        //TODO : - need to update iq.i_OPGref

    }

    public ClsAvalancheOPG(string opgRef, clsRegion Region, DateTime validFrom, DateTime ValidTo, int optMin, int optMax, DataTable writecache = null)
    {

        this.OPGref = opgRef;
        //Me.ProdRef = prodRef
        this.Region = Region;
        this.ValidFrom = validFrom;
        this.ValidTo = ValidTo;
        this.OptMin = optMin;
        this.OptMax = optMax;
        this.Options = new Dictionary<int, ClsAvalancheOption>();

        if (writecache == null)
        {

            object sql = null;
            sql = "INSERT INTO avalancheOPG (opgref,optmin,optmax,validFrom,validTo,fk_region_id) ";
            sql += "VALUES (" + opgRef + "," + System.Convert.ToString(optMin) + "," + System.Convert.ToString(optMax) + "," + da.universaldate(validFrom) + "," + da.universaldate(ValidTo) + "," + Region.ID + ")";

            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
            iq.AvalancheOPGs.Add(this.ID, this);

        }
        else
        {

            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();

            row["opgref"] = this.OPGref;
            //row("prodref") = Me.ProdRef
            row["optmin"] = this.OptMin;
            row["optmax"] = this.OptMax;
            //row("LPDiscountPercent") = Me.LPDiscountPercent
            row["validFrom"] = this.ValidFrom;
            row["validTo"] = this.ValidTo;
            row["fk_region_id"] = this.Region.ID;

            writecache.Rows.Add(row);

        }

        iq.i_OpgRef.Add(opgRef, this);

        //If a system has a descendant price with  offer which points to a pool contaiining the system

    }
    public ClsAvalancheOPG()
    {

        this.ID = -1;


    }


    public ClsAvalancheOPG(int ID, string opgRef, clsRegion Region, DateTime validFrom, DateTime ValidTo, int optMin, int optMax)
    {

        //the OPG's don't carry a set of systems - becuase the systems have the OPG's added (product.avalancheOPGs)... which is how they're needed
        //(we typically want to know the OPG's for a system - not the systems for an OPG)

        this.ID = ID;
        this.OPGref = opgRef;
        this.Region = Region;
        this.ValidFrom = validFrom;
        this.ValidTo = ValidTo;
        this.OptMin = optMin;
        this.OptMax = optMax;

        iq.AvalancheOPGs.Add(this.ID, this);
        iq.i_OpgRef.Add(opgRef, this);

        this.Options = new Dictionary<int, ClsAvalancheOption>();

    }


}