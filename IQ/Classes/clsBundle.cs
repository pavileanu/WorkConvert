using dataAccess;


public class clsBundle
{

    //each bundle is added to one or more systems Bundles  (the bundle applies to (potentially) many systems
    public int ID { get; set; }
    public clsTranslation Name { get; set; }
    public string OPGRef { get; set; }
    public string Code { get; set; }
    public DateTime validFrom { get; set; }
    public DateTime validTo { get; set; }
    public clsRegion Region { get; set; }
    //Property Channel as clschannel ' would be asy to implement but Dan says not a priority - would give disti/customer specific bundles - would need to recurse the channel tree
    //Rebate as single :NB- Rebate has been moved into the BundleSystem - allowing different rebates on differnet systems (with the same bundle of options)

    public Dictionary<int, clsBundleSystem> Systems { get; set; } //allows the generic editor to view/add systems to a bundle conveniently.. note the bundles are also added the the systems in question but are not editable in that context (they are not a property)
    public Dictionary<int, clsBundleItem> Items { get; set; } //The options in the bundle.. it's called Items as - in future it may also contain (sub) systems

    public string get_DisplayName(clsLanguage language)
    {
        string returnValue = "";
        returnValue = OPGRef;
        return returnValue;
    }
    public clsBundle Insert()
    {

        clsBundle av = new clsBundle(this.Name, this.OPGRef, this.Code, this.Region, this.validFrom, this.validTo);
        return av;

    }

    public void Update()
    {

        object sql = null;
        sql = "UPDATE bundle SET ";
        sql += "fk_Translation_key_name=" + this.Name.Key + ",";
        sql += "Opgref=" + da.SqlEncode(this.OPGRef) + ",";
        sql += "code=" + da.SqlEncode(this.Code) + ",";
        sql += "FK_region_id=" + this.Region.ID + ",";
        sql += "validFrom=" + da.UniversalDate(this.validFrom) + ",";
        sql += "validTo=" + da.universaldate(this.validTo) + ",";
        sql += " WHERE ID = " + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql);

        //TODO : - need to update iq.i_bundle_code

    }

    public clsBundle(clsTranslation name, string opgRef, string code, clsRegion Region, DateTime validFrom, DateTime ValidTo, DataTable writecache = null)
    {

        this.Name = name;
        this.OPGRef = opgRef;
        this.Code = code;
        this.Region = Region;
        this.validFrom = validFrom;
        this.validTo = ValidTo;
        //    Me.Rebate = rebate
        this.Items = new Dictionary<int, clsBundleItem>();

        if (writecache == null)
        {

            object sql = null;
            sql = "INSERT INTO Bundle (fk_translation_key,opgref,code,validfrom,validto,fk_region_id,rebate) ";
            sql += "VALUES (" + this.Name.Key + "," + da.SqlEncode(opgRef) + "," + da.SqlEncode(code) + "," + da.universaldate(validFrom) + "," + da.universaldate(ValidTo) + "," + Region.ID + ")";

            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
            iq.Bundles.Add(this.ID, this);

        }
        else
        {

            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();

            row["fk_translation_key_name"] = this.Name.Key;
            row["opgref"] = this.OPGRef;
            row["code"] = this.Code;
            row["validFrom"] = this.validFrom;
            row["validTo"] = this.validTo;
            row["fk_region_id"] = this.Region.ID;


            writecache.Rows.Add(row);

        }

        iq.i_Bundle_code.Add(this.Code, this);

        //If a system has a descendant price with  offer which points to a pool contaiining the system

    }
    public clsBundle()
    {

        this.ID = -1;


    }

    public Panel UI()
    {
        Panel returnValue = default(Panel);

        returnValue = new Panel();

        Label lbl = new Label();
        lbl.Text = "Bundles available";


        return returnValue;
    }

    public clsBundle(int ID, clsTranslation name, string opgRef, string code, clsRegion Region, DateTime validFrom, DateTime ValidTo)
    {


        this.ID = ID;
        this.Name = name;
        this.OPGRef = opgRef;
        this.Code = code;
        this.Region = Region;
        this.validFrom = validFrom;
        this.validTo = ValidTo;

        iq.Bundles.Add(this.ID, this);

        this.Items = new Dictionary<int, clsBundleItem>();

        iq.i_Bundle_code.Add(this.Code, this);


    }


}