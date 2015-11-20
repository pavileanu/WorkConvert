[Serializable()]
public class clsUserState
{
	public string lid;
	public string root;
	public string path;
	public int? QuoteID;
		//is a CD list
	public string foci;
	public string treeCursorPath;
	public List<KeyValuePair<string, clsBranchState>> branchStates;
	public int AgentAccount;
	public int BuyerAccount;
	public List<clsScreenHeaderState> ScreenHeaders;
		//Render only this (system) branch (formerly 'configuring')
	public int showOnly;
	public enumParadigm Paradigm;
	public List<KeyValuePair<object, object>> mopUpvalues;
}

public class clsScreenHeaderState
{
	public string Path;
	public bool QuickFiltersVisible;
		//As List(Of KeyValuePair(Of clsField, List(Of KeyValuePair(Of clsFilter, List(Of Int64)))))
	public List<KeyValuePair<int, List<KeyValuePair<clsFilter, List<Int64>>>>> Filters;
	public List<KeyValuePair<int, clsPriorityDirection>> Sorts;
}
