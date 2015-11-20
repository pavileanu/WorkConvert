using dataAccess;


public class clsPrune
{

    public int ID { get; set; }
    public string Path { get; set; }
    public NullableInt ChannelID { get; set; } //clsChannel 'the seller channel to wich this prune applices (not yet implimented) - but would handle BU's
    public string Source { get; set; }
    public DateTime Created { get; set; }

    public clsPrune()
    {

    }

    public void update()
    {

        object sql = null;
        sql = "UPDATE [PRUNE] set source=" + da.SqlEncode(Source) + " WHERE ID=" + System.Convert.ToString(this.ID);
        da.dbexecutesql(sql);

    }
    public clsPrune(string path, NullableInt ChannelID, string Source, DataTable writecache, ref int nextPruneId)
    {



        if (writecache == null)
        {
            object sql = null;
            sql = "INSERT INTO [prune] (path,fk_channel_id,Created,source) VALUES (" + da.SqlEncode(path) + "," + ChannelID.sqlvalue + ",getdate()," + da.SqlEncode(Source) + ");";
            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
        }
        else
        {

            this.ID = nextPruneId; //they will get their true ID's next time they're loaded
            nextPruneId++;
            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();
            row["Path"] = path;
            if (ChannelID.sqlvalue == "null")
            {
                row["FK_Channel_id"] = DBNull.Value;
            }
            else
            {
                row["FK_Channel_id"] = ChannelID.sqlvalue;
            }

            row["Created"] = DateTime.Now;
            row["Source"] = Source;
            writecache.Rows.Add(row);

        }


        this.ChannelID = ChannelID;
        this.Path = path;
        object fp = PathName(path);

        this.Source = Source;
        this.Created = DateTime.Now;

        clsBranch branch = iq.Branches(path.Split('.').Last);

        if (branch.Prunes.Count > 5000)
        {
            Debugger.Break();
        }
        branch.Prunes.Add(this.ID, this);

    }

    public clsPrune(int id, string path, NullableInt ChannelID, string source, DateTime created)
    {

        this.ID = id;
        this.ChannelID = ChannelID;
        this.Path = path;
        this.Created = created;
        this.Source = source;

        if (!iq.Branches.ContainsKey(path.Split('.').Last))
        {
            this.delete();
        }
        else
        {
            clsBranch BRANCH = iq.Branches(path.Split('.').Last);
            BRANCH.Prunes.Add(this.ID, this);
            if (BRANCH.Product != null)
            {
                if (BRANCH.Product.SKU == "AN975A")
                {
                    Interaction.Beep();
                }
            }
        }
    }

    public void delete()
    {

        da.DBExecutesql("DELETE FROM PRUNE WHERE ID=" + System.Convert.ToString(this.ID));

        if (iq.Branches.ContainsKey(Path.Split('.').Last))
        {
            if (iq.Branches(Path.Split('.').Last).Prunes.ContainsKey(this.ID))
            {
                iq.Branches(Path.Split('.').Last).Prunes.Remove(this.ID);
            }
        }

    }

} //clsPrune