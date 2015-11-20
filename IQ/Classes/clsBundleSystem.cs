using dataAccess;


public class clsBundleSystem
{

    public int ID { get; set; }
    public clsBundle Bundle { get; set; }
    public clsProduct System { get; set; }
    public float rebate { get; set; }

    public clsBundleSystem(clsBundle bundle, clsProduct system, float rebate, DataTable writecache = null)
    {

        //price is some absolute overriding price on this bundle item (guaranteed by HP to be less then anyones buy price)
        //Rebate - is relative discount of their normal 'buy price' - rebate is subtracted - so use positive numbers

        if (writecache == null)
        {
            object sql = null;
            sql = "INSERT INTO BundleSystem(fk_Bundle_id,fk_product_id_system,rebate) VALUES (" + Bundle.ID + "," + Bundle.ID + "," + system.ID + "," + System.Convert.ToString(rebate) + ")";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql));
            if (system.Bundles == null)
            {
                system.Bundles = new Dictionary<int, clsBundle>();
            }
            system.Bundles.Add(Bundle.ID, bundle);

        }
        else
        {
            this.ID = -1;

            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();

            row["fk_bundle_id"] = Bundle.ID;
            row["fk_product_id_system"] = system.ID;
            row["rebate"] = rebate;
            writecache.Rows.Add(row);

        }

        this.Bundle = bundle;
        this.System = system;
        this.rebate = rebate;

    }

    public clsBundleSystem(int ID, clsBundle bundle, clsProduct system, float rebate)
    {

        this.ID = ID;
        this.Bundle = bundle;
        this.System = system;
        this.rebate = rebate;

        if (system.Bundles == null)
        {
            system.Bundles = new Dictionary<int, clsBundle>();
        }
        system.Bundles.Add(Bundle.ID, bundle);


    }


}