using dataAccess;


public class clsThread
{

    public int ID { get; set; }
    public clsUser CreatedBy { get; set; }
    public clsUser AssignedTo { get; set; }
    public clsThread Parent { get; set; }
    public clsState Priority { get; set; }
    public clsState Status { get; set; }
    public float hours { get; set; }
    public string title { get; set; }
    public nullableString Text { get; set; }
    public Dictionary<int, clsThread> Children { get; set; } //replies
    //Property EventLog As clsEvent
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public bool @internal { get; set; }

    public clsThread oParent;

    //There are three 'constructors' (sub New's) - which one is used depends on the type and number of paramaters supplied (this is called 'overloading')
    //The parameterless constructor - sub new() is used by the 'add' button of the generic editor -
    //The one *with* the ID, is used to load up an exiting instance  from the database
    //The one *without* the ID fills the object AND inserts it to the DB, this is typically the one called by INSERT()

    public clsThread()
    {

        //Replies = New Dictionary(Of Integer, clsThread) 'this should really be done via reflection in the generic addnew() which would allow the parameterless constructors to be empty
        this.Children = new Dictionary<int, clsThread>();

        //NB: SetDefaults() sets my parent, and adds me to my parents children

    }

    public clsThread(int id, clsUser CreatedBy, clsUser AssignedTo, clsThread Parent, clsState priority, clsState status, float hours, string title, nullableString text, DateTime Created, DateTime Updated, bool Internal)
    {

        //this overload is the 'reconstructor' used when loading threads up into the OM from the DB - see loadThreads()

        this.ID = id;
        this.CreatedBy = CreatedBy;
        this.AssignedTo = AssignedTo;
        this.Parent = Parent;
        this.Priority = priority;
        this.Status = status;
        this.hours = hours;
        this.title = title;
        this.Text = text;
        this.Children = new Dictionary<int, clsThread>();
        //    Me.EventLog = EventLog
        this.Created = Created;
        this.Updated = Updated;
        this.@internal = Internal;

        iq.Threads.Add(this.ID, this);
        if (iq.Threads.Count == 1)
        {
            if (!(this.Parent == null))
            {
                Debugger.Break(); // the root thread should not have a parent
            }
            iq.RootThread = this;
        }

        if (!(this.Parent == null))
        {
            this.Parent.Children.Add(this.ID, this); //add me to my parents children (to create the heirarchy)
        }

        oParent = Parent;

    }

    public clsThread(clsUser CreatedBy, clsUser AssignedTo, clsThread Parent, clsState priority, clsState status, float hours, string title, nullableString text, DateTime created, DateTime updated, bool Internal)
    {

        this.CreatedBy = CreatedBy;
        this.AssignedTo = AssignedTo;
        this.Parent = Parent;
        this.Priority = priority;
        this.Status = status;
        this.hours = hours;
        this.Text = text;
        this.Children = new Dictionary<int, clsThread>();

        this.Created = created;
        this.Updated = updated;
        this.@internal = Internal;

        object sql = null;

        object pid = null;
        if (this.Parent == null)
        {
            pid = "null";
        }
        else
        {
            pid = (this.Parent.ID).ToString();
        }

        object elid = null;
        //If EventLog Is Nothing Then
        elid = "null";
        // Else
        // elid = EventLog.id
        // End If

        sql = "INSERT INTO Thread (FK_User_ID_CreatedBy,FK_User_ID_AssignedTo,FK_Thread_ID_Parent,FK_State_ID_Priority,FK_State_ID_Status,[hours],title,[Text],FK_Event_ID,[created],[Updated],[Internal]) VALUES (";
        sql += CreatedBy.ID + "," + AssignedTo.ID + "," + System.Convert.ToString(pid) + "," + Priority.ID + "," + Status.ID + "," + System.Convert.ToString(hours) + "," + da.SqlEncode(title) + "," + Text.sqlValue + "," + System.Convert.ToString(elid) + ",getdate(),getdate()," + System.Convert.ToString(Internal ? 1 : 0) + ");";

        this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true)); //this is important !

        iq.Threads.Add(this.ID, this);
        if (iq.Threads.Count == 1)
        {
            if (!(this.Parent == null))
            {
                Debugger.Break(); // the root thread should not have a parent
            }
            iq.RootThread = this;
        }

        if (!(this.Parent == null))
        {
            this.Parent.Children.Add(this.ID, this); //add me to my parents children (to create the heirarchy)
        }

        oParent = Parent;


    }

    public dynamic Delete()
    {

        if (!(this.oParent == null))
        {
            this.oParent.Children.Remove(this.ID);
        }

        object sql = null;
        sql = "DELETE FROM Thread where id=" + System.Convert.ToString(this.ID);

        da.dbexecutesql(sql);

        return true;

    }

    public clsThread Insert()
    {
        //called after the default values (and parent) have been set - see setDefaults()
        //returns the new, 'real' thread - complete with ID (@@IDENTITY)
        //AND adds it to it's parents children

        return new clsThread(this.CreatedBy, this.AssignedTo, this.Parent, this.Priority, this.Status, this.hours, this.title, this.Text, this.Created, this.Updated, this.@internal);

    }

    public void update()
    {

        object sql = null;

        if (!(this.Parent == null))
        {
            if (!this.Parent.Children.ContainsKey(this.ID))
            {
                Debugger.Break(); //You have reparented a thread - it needs removing from it's original parents childern, and adding to its new parents children
            }

        }

        sql = "UPDATE thread set ";
        sql += "fk_user_id_createdby=" + this.CreatedBy.ID + ",";
        sql += "fk_user_id_assignedto=" + this.AssignedTo.ID + ",";
        if (this.Parent == null)
        {
            sql += "fk_thread_id_parent=null" + ",";
        }
        else
        {
            sql += "fk_thread_id_parent=" + System.Convert.ToString(this.Parent.ID) + ",";
        }

        sql += "fk_state_id_priority=" + this.Priority.ID + ",";
        sql += "fk_state_id_status=" + this.Status.ID + ",";
        sql += "hours=" + System.Convert.ToString(hours) + ",";
        sql += "title=" + da.SqlEncode(this.title) + ",";

        sql += "text=" + Text.sqlValue + ",";
        //If Me.EventLog Is Nothing Then
        sql += "fk_event_id=null,";
        // Else
        // sql$ &= "fk_event_id=" & Me.EventLog.ID & ","
        // End If
        sql += "[updated]=getdate(),";
        sql += "[internal]=" + System.Convert.ToString(@internal ? 1 : 0);

        sql += " WHERE id=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql, false);


        //Supports reparenting
        this.oParent.Children.Remove(this.ID);
        this.Parent.Children.Add(this.ID, this);


    }

    public dynamic get_displayName(clsLanguage language)
    {
        return this.title + " (" + System.Convert.ToString(this.ID) + ")";
    }


}