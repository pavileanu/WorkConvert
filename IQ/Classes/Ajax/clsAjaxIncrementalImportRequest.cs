using System.Net.Http;

public class clsAjaxIncrementalImportRequest : HttpRequestMessage
{

	public UInt64 lid {
		get { return m_lid; }
		set { m_lid = Value; }
	}
	private UInt64 m_lid;
	public UInt64 elid {
		get { return m_elid; }
		set { m_elid = Value; }
	}
	private UInt64 m_elid;
	public string SKUList {
		get { return m_SKUList; }
		set { m_SKUList = Value; }
	}
	private string m_SKUList;
	public Int32 atPoint {
		get { return m_atPoint; }
		set { m_atPoint = Value; }
	}
	private Int32 m_atPoint;

	public List<System.Collections.Generic.KeyValuePair<int, bool>> SubmitList {
		get { return m_SubmitList; }
		set { m_SubmitList = Value; }
	}
	private List<System.Collections.Generic.KeyValuePair<int, bool>> m_SubmitList;

}
