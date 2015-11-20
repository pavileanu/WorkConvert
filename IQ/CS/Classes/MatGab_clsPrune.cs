using dataAccess;

public class clsPrune
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string Path {
		get { return m_Path; }
		set { m_Path = Value; }
	}
	private string m_Path;
	private NullableInt ChannelID {
		get { return m_ChannelID; }
		set { m_ChannelID = Value; }
	}
	private NullableInt m_ChannelID;
	//clsChannel 'the seller channel to wich this prune applices (not yet implimented) - but would handle BU's
	private string Source {
		get { return m_Source; }
		set { m_Source = Value; }
	}
	private string m_Source;
	private DateTime Created {
		get { return m_Created; }
		set { m_Created = Value; }
	}
	private DateTime m_Created;


	public clsPrune()
	{
	}


	public void update()
	{
		object sql;
		sql = "UPDATE [PRUNE] set source=" + da.SqlEncode(Source) + " WHERE ID=" + this.ID;
		da.dbexecutesql(sql);

	}

	public clsPrune(string path, NullableInt ChannelID, string Source, ref DataTable writecache = null, ref int nextPruneId__1 = 0)
	{


		if (writecache == null) {
			object sql;
			sql = "INSERT INTO [prune] (path,fk_channel_id,Created,source) VALUES (" + da.SqlEncode(path) + "," + ChannelID.sqlvalue + ",getdate()," + da.SqlEncode(Source) + ");";
			this.ID = da.DBExecutesql(sql, true);

		} else {
			this.ID = nextpruneid;
			//they will get their true ID's next time they're loaded
			nextpruneid += 1;
			System.Data.DataRow row;
			row = writecache.NewRow();
			row("Path") = path;
			if (ChannelID.sqlvalue == "null") {
				row("FK_Channel_id") = DBNull.Value;
			} else {
				row("FK_Channel_id") = ChannelID.sqlvalue;
			}

			row("Created") = Now;
			row("Source") = Source;
			writecache.Rows.Add(row);

		}


		this.ChannelID = ChannelID;
		this.Path = path;
		object fp = PathName(path);

		this.Source = Source;
		this.Created = Now;

		clsBranch branch = iq.Branches(Split(path, ".").Last);

		if (branch.Prunes.Count > 5000)
			System.Diagnostics.Debugger.Break();
		branch.Prunes.Add(this.ID, this);

	}


	public clsPrune(int id, string path, NullableInt ChannelID, string source, DateTime created)
	{
		this.ID = id;
		this.ChannelID = ChannelID;
		this.Path = path;
		this.Created = created;
		this.Source = source;

		if (!iq.Branches.ContainsKey(Split(path, ".").Last)) {
			this.delete();
		} else {
			clsBranch BRANCH = iq.Branches(Split(path, ".").Last);
			BRANCH.Prunes.Add(this.ID, this);
			if (BRANCH.Product != null) {
				if (BRANCH.Product.SKU == "AN975A") {
					Beep();
				}
			}
		}
	}


	public void delete()
	{
		da.DBExecutesql("DELETE FROM PRUNE WHERE ID=" + this.ID);

		if (iq.Branches.ContainsKey(Split(Path, ".").Last)) {
			if (iq.Branches(Split(Path, ".").Last).Prunes.ContainsKey(this.ID)) {
				iq.Branches(Split(Path, ".").Last).Prunes.Remove(this.ID);
			}
		}

	}

}
//clsPrune


