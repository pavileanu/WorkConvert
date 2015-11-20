using dataAccess;

public class clsAvalancheOption
{

    public int ID { get; set; }
    public string ProdRef { get; set; }
    public float LPDiscountPercent { get; set; }
    public ClsAvalancheOPG AvalancheOPG { get; set; }

    public clsAvalancheOption(ClsAvalancheOPG avOPG, string prodRef, float LPDiscountPercent, DataTable WriteCache = null)
    {


        if (LPDiscountPercent == 0)
        {
            Debugger.Break();
        }

        if (WriteCache == null)
        {
            object sql = null;
            sql = "INSERT INTO AvalancheOption(fk_avalancheOPG_ID,prodref,lpdiscountpercent) VALUES (" + avOPG.ID + "," + da.SqlEncode(prodRef) + "," + System.Convert.ToString(LPDiscountPercent) + ")";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql));
            avOPG.Options.Add(this.ID, this);
        }
        else
        {
            this.ID = -1;

            System.Data.DataRow row = default(System.Data.DataRow);
            row = WriteCache.NewRow();

            row["fk_avalancheOPG_id"] = avOPG.ID;
            row["prodref"] = prodRef;
            row["LPDiscountPercent"] = LPDiscountPercent;
            WriteCache.Rows.Add(row);

        }

        this.ProdRef = prodRef;
        this.LPDiscountPercent = LPDiscountPercent;
        this.AvalancheOPG = avOPG;


    }

    public clsAvalancheOption(int ID, ClsAvalancheOPG avOPG, string prodRef, float LPDiscountPercent)
    {

        this.ID = ID;
        this.ProdRef = prodRef;
        this.LPDiscountPercent = LPDiscountPercent;

        if (this.LPDiscountPercent == 0)
        {
            Debugger.Break();
        }

        this.AvalancheOPG = avOPG;
        avOPG.Options.Add(this.ID, this);

    }

}