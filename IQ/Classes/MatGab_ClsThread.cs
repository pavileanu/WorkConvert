using dataAccess;

public class clsThread
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsUser CreatedBy {
		get { return m_CreatedBy; }
		set { m_CreatedBy = Value; }
	}
	private clsUser m_CreatedBy;
	private clsUser AssignedTo {
		get { return m_AssignedTo; }
		set { m_AssignedTo = Value; }
	}
	private clsUser m_AssignedTo;
	private clsThread Parent {
		get { return m_Parent; }
		set { m_Parent = Value; }
	}
	private clsThread m_Parent;
	private clsState Priority {
		get { return m_Priority; }
		set { m_Priority = Value; }
	}
	private clsState m_Priority;
	private clsState Status {
		get { return m_Status; }
		set { m_Status = Value; }
	}
	private clsState m_Status;
	private float hours {
		get { return m_hours; }
		set { m_hours = Value; }
	}
	private float m_hours;
	private string title {
		get { return m_title; }
		set { m_title = Value; }
	}
	private string m_title;
	private nullableString Text {
		get { return m_Text; }
		set { m_Text = Value; }
	}
	private nullableString m_Text;
	private Dictionary<int, clsThread> Children {
		get { return m_Children; }
		set { m_Children = Value; }
	}
	private Dictionary<int, clsThread> m_Children;
	//replies
	//Property EventLog As clsEvent
	private DateTime Created {
		get { return m_Created; }
		set { m_Created = Value; }
	}
	private DateTime m_Created;
	private DateTime Updated {
		get { return m_Updated; }
		set { m_Updated = Value; }
	}
	private DateTime m_Updated;
	private bool @internal {
		get { return m_internal; }
		set { m_internal = Value; }
	}
	private bool m_internal;


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

	public clsThread(int id, clsUser CreatedBy, clsUser AssignedTo, clsThread Parent, clsState priority, clsState status, float hours, string title, nullableString text, DateTime Created,

	DateTime Updated, bool Internal)
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
		if (iq.Threads.Count == 1) {
			if (!this.Parent == null)
				System.Diagnostics.Debugger.Break();
			// the root thread should not have a parent
			iq.RootThread = this;
		}

		if (!this.Parent == null) {
			this.Parent.Children.Add(this.ID, this);
			//add me to my parents children (to create the heirarchy)
		}

		oParent = Parent;

	}

	public clsThread(clsUser CreatedBy, clsUser AssignedTo, clsThread Parent, clsState priority, clsState status, float hours, string title, nullableString text, DateTime created, DateTime updated,

	bool Internal)
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

		object sql;

		object pid;
		if (this.Parent == null) {
			pid = "null";
		} else {
			pid = this.Parent.ID;
		}

		object elid;
		//If EventLog Is Nothing Then
		elid = "null";
		// Else
		// elid = EventLog.id
		// End If

		sql = "INSERT INTO Thread (FK_User_ID_CreatedBy,FK_User_ID_AssignedTo,FK_Thread_ID_Parent,FK_State_ID_Priority,FK_State_ID_Status,[hours],title,[Text],FK_Event_ID,[created],[Updated],[Internal]) VALUES (";
		sql += CreatedBy.ID + "," + AssignedTo.ID + "," + pid + "," + priority.ID + "," + status.ID + "," + hours + "," + da.SqlEncode(title) + "," + text.sqlValue + "," + elid + ",getdate(),getdate()," + IIf(Internal, 1, 0) + ");";

		this.ID = da.DBExecutesql(sql, true);
		//this is important !

		iq.Threads.Add(this.ID, this);
		if (iq.Threads.Count == 1) {
			if (!this.Parent == null)
				System.Diagnostics.Debugger.Break();
			// the root thread should not have a parent
			iq.RootThread = this;
		}

		if (!this.Parent == null) {
			this.Parent.Children.Add(this.ID, this);
			//add me to my parents children (to create the heirarchy)
		}

		oParent = Parent;


	}

	public object Delete()
	{

		if (!this.oParent == null) {
			this.oParent.Children.Remove(this.ID);
		}

		object sql__1;
		Sql = "DELETE FROM Thread where id=" + this.ID;

		da.dbexecutesql(sql__1);

		return true;

	}

	public clsThread Insert()
	{
		//called after the default values (and parent) have been set - see setDefaults()
		//returns the new, 'real' thread - complete with ID (@@IDENTITY)
		//AND adds it to it's parents children

		return new clsThread(this.CreatedBy, this.AssignedTo, this.Parent, this.Priority, this.Status, this.hours, this.title, this.Text, this.Created, this.Updated,
		this.@internal);

	}


	public void update()
	{
		object sql;

		if (!this.Parent == null) {
			if (!this.Parent.Children.ContainsKey(this.ID)) {
				System.Diagnostics.Debugger.Break();
				//You have reparented a thread - it needs removing from it's original parents childern, and adding to its new parents children
			}

		}

		sql = "UPDATE thread set ";
		sql += "fk_user_id_createdby=" + this.CreatedBy.ID + ",";
		sql += "fk_user_id_assignedto=" + this.AssignedTo.ID + ",";
		if (this.Parent == null) {
			sql += "fk_thread_id_parent=null" + ",";
		} else {
			sql += "fk_thread_id_parent=" + this.Parent.ID + ",";
		}

		sql += "fk_state_id_priority=" + this.Priority.ID + ",";
		sql += "fk_state_id_status=" + this.Status.ID + ",";
		sql += "hours=" + hours + ",";
		sql += "title=" + da.SqlEncode(this.title) + ",";

		sql += "text=" + Text.sqlValue + ",";
		//If Me.EventLog Is Nothing Then
		sql += "fk_event_id=null,";
		// Else
		// sql$ &= "fk_event_id=" & Me.EventLog.ID & ","
		// End If
		sql += "[updated]=getdate(),";
		sql += "[internal]=" + IIf(@internal, 1, 0);

		sql += " WHERE id=" + this.ID;

		da.DBExecutesql(sql, false);


		//Supports reparenting
		this.oParent.Children.Remove(this.ID);
		this.Parent.Children.Add(this.ID, this);


	}

	private  displayName {
		get { return this.title + " (" + this.ID + ")"; }
	}


}

