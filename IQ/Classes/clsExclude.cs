using dataAccess;


public class clsExclude
{

    //Stores a multually exclusive SET of branches - EG. UDIMM/RDIMM

    //It tempting to do this by path - but that would require an entry for every system in a family - this way, the excludes work on the (Grafted) copies of the option pranches (under every system)

    public int ID { get; set; }
    public List<clsBranch> havingAnyOf; //having any of these
    public List<clsBranch> excludesAllOf; //excludes all of these
    public string Reason { get; set; }

    public clsExclude(int id, clsBranch Having, clsBranch excludes, string reason)
    {

        this.havingAnyOf = Having.Descendants;
        this.excludesAllOf = excludes.Descendants;
        this.Reason = reason;
        iq.Excludes.Add(id, this);

    }

    public clsExclude(clsBranch Having, clsBranch excludes, string reason)
    {

        this.havingAnyOf = Having.Descendants;
        this.excludesAllOf = excludes.Descendants;
        this.Reason = reason;
        this.ID = System.Convert.ToInt32(da.DBExecutesql("INSERT INTO [exclude] (fk_branch_id_having,fk_branch_id_excludes,reason) VALUES(" + Having.ID + "," + excludes.ID + "," + da.SqlEncode(reason) + ");", true));

        iq.Excludes.Add(ID, this);

    }

    public dynamic Delete()
    {
        dynamic returnValue = default(dynamic);


        iq.Excludes.Remove(this.ID);
        da.DBExecutesql("Delete from exclude where id=" + System.Convert.ToString(this.ID));

        returnValue = "";

        return returnValue;
    }


}