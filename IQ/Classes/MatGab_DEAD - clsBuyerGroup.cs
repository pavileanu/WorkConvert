using dataAccess;

public class clsBuyerGroup
{

	//DEAD/OBSOLETE (may need to be re-implimented if we can get distis to give pricelists or groups of buyers

	public int ID;
	public string name;
	public List<clsChannel> Channels;
	public clsChannel Owner;

	public string OwnersID;

	public clsBuyerGroup(int id, string name, clsChannel owner, string ownersID)
	{
		this.ID = id;
		this.name = name;
		this.Channels = new List<clsChannel>();
		this.Owner = owner;
		this.OwnersID = ownersID;

		iq.BuyerGroups.Add(this.ID, this);
		//        iq.i_buyerGroups.Add(owner.ID & "_" & ownersID, Me)

	}


	public clsBuyerGroup(string name, clsChannel owner, string ownersID)
	{
		object sql;
		sql = "INSERT INTO [BuyerGroup] (name,fk_channel_id_owner,ownersID) VALUES (" + da.SqlEncode(name) + "," + owner.ID + "," + da.SqlEncode(ownersID) + ");";

		this.ID = da.DBExecutesql(sql, true);
		this.name = name;
		this.Owner = owner;
		this.OwnersID = ownersID;

		iq.BuyerGroups.Add(this.ID, this);

	}

}
