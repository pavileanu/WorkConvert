using dataAccess;

public class clsExclude
{

	//Stores a multually exclusive SET of branches - EG. UDIMM/RDIMM

	//It tempting to do this by path - but that would require an entry for every system in a family - this way, the excludes work on the (Grafted) copies of the option pranches (under every system)

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
		//having any of these
	public List<clsBranch> havingAnyOf;
		//excludes all of these
	public List<clsBranch> excludesAllOf;
	private string Reason {
		get { return m_Reason; }
		set { m_Reason = Value; }
	}
	private string m_Reason;


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
		this.ID = da.DBExecutesql("INSERT INTO [exclude] (fk_branch_id_having,fk_branch_id_excludes,reason) VALUES(" + Having.ID + "," + excludes.ID + "," + da.SqlEncode(reason) + ");", true);

		iq.Excludes.Add(ID, this);

	}

	public object Delete()
	{


		iq.Excludes.Remove(this.ID);
		da.DBExecutesql("Delete from exclude where id=" + this.ID);

		Delete = "";

	}


}
