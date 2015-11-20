public class clsLimit
{

	//[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref]
	private int Qinstalled {
		get { return m_Qinstalled; }
		set { m_Qinstalled = Value; }
	}
	private int m_Qinstalled;
	private int Qmin {
		get { return m_Qmin; }
		set { m_Qmin = Value; }
	}
	private int m_Qmin;
	private int Qmax {
		get { return m_Qmax; }
		set { m_Qmax = Value; }
	}
	private int m_Qmax;
	private int MinIncr {
		get { return m_MinIncr; }
		set { m_MinIncr = Value; }
	}
	private int m_MinIncr;
	private int PrefIncr {
		get { return m_PrefIncr; }
		set { m_PrefIncr = Value; }
	}
	private int m_PrefIncr;


	public clsLimit(int Installed, int Qmin, int Qmax, int MinIncr, int PrefIncr)
	{
		this.Qinstalled = Installed;
		this.Qmin = Qmin;
		this.Qmax = Qmax;
		this.MinIncr = MinIncr;
		this.PrefIncr = PrefIncr;

	}

}
