using dataAccess;

public class OLDclsEvent
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private Dictionary<int, clsEvent> children {
		get { return m_children; }
		set { m_children = Value; }
	}
	private Dictionary<int, clsEvent> m_children;
	private string message {
		get { return m_message; }
		set { m_message = Value; }
	}
	private string m_message;
	private clsEvent parent {
		get { return m_parent; }
		set { m_parent = Value; }
	}
	private clsEvent m_parent;
	private clsState EventType {
		get { return m_EventType; }
		set { m_EventType = Value; }
	}
	private clsState m_EventType;
	private DateTime timeStamp {
		get { return m_timeStamp; }
		set { m_timeStamp = Value; }
	}
	private DateTime m_timeStamp;
	private int duration {
		get { return m_duration; }
		set { m_duration = Value; }
	}
	private int m_duration;
	//duration in milliseconds
	private double startTick {
		get { return m_startTick; }
		set { m_startTick = Value; }
	}
	private double m_startTick;
	//used in the calculation of duration (via calls to .Update)
	private int severity {
		get { return m_severity; }
		set { m_severity = Value; }
	}
	private int m_severity;

	private string displayName {
		get { return this.EventType.Translation.text(Language) + " - " + message; }
	}



	public OLDclsEvent(int id, clsEvent parent, string message, clsState EventType, DateTime timestamp, int duration)
	{
		//re-constructor (typically from the database)
		//But, we also construct events with a -1 ID 

		this.ID = id;
		this.parent = parent;

		this.message = message;
		this.EventType = EventType;
		this.timeStamp = timestamp;
		this.duration = duration;
		this.children = new Dictionary<int, clsEvent>();

		iq.Events.Add(this.ID, this);

		if (iq.Events.Count == 1) {
			if (!this.parent == null)
				System.Diagnostics.Debugger.Break();
			// the root event should not have a parent
			iq.RootEvent = this;
		}

		if (!this.parent == null) {
			this.parent.children.Add(this.ID, this);
			//add me to my parents children (to create the heirarchy)
		}

		this.startTick = Stopwatch.GetTimestamp;

	}

	//does not persist to the database (they should be bulk written en-masse periocially for performance)
	public void close()
	{

		this.duration = (Stopwatch.GetTimestamp - this.startTick) / Stopwatch.Frequency * 1000;

	}

	public OLDclsEvent(clsEvent parent, string message, clsState EventType)
	{
		this.message = message;
		this.EventType = EventType;
		this.timeStamp = Now;
		this.duration = -1;
		this.children = new Dictionary<int, clsEvent>();

		object sql;
		string pid;
		if (parent == null)
			pid = "null";
		else
			pid = parent.ID;

		sql = "INSERT INTO EVENT (message,fk_event_id_parent,fk_state_id_eventtype,timestamp,duration,severity) VALUES (";
		sql += da.SqlEncode(message) + "," + pid + "," + EventType.ID + ",getdate(),0," + this.severity.ToString + ");";
		this.ID = da.DBExecutesql(sql, true);
		iq.Events.Add(this.ID, this);

		if (iq.Events.Count == 1) {
			if (!this.parent == null)
				System.Diagnostics.Debugger.Break();
			// the root event should not have a parent
			iq.RootEvent = this;
		}

		if (!this.parent == null) {
			this.parent.children.Add(this.ID, this);
			//add me to my parents children (to create the heirarchy)
		}

		this.startTick = Stopwatch.GetTimestamp;

	}


	public void update(Message = "")
	{
		//records the duration 

		if (Message != "")
			this.message = Message;

		object sql;
		this.duration = (Stopwatch.GetTimestamp - this.startTick) / Stopwatch.Frequency * 1000;
		sql = "UPDATE [Event] SET message=" + da.SqlEncode(this.message) + ",FK_State_ID_EventType=" + EventType.ID + ",duration=" + this.duration + " WHERE ID=" + this.ID;
		da.DBExecutesql(sql);

	}


}
