[Serializable]
public class clsUserState
{
    public string lid;
    public string root;
    public string path;
    public int? QuoteID;
    public string foci; //is a CD list
    public string treeCursorPath;
    public List<KeyValuePair<string, clsBranchState>> branchStates;
    public int AgentAccount;
    public int BuyerAccount;
    public List<clsScreenHeaderState> ScreenHeaders;
    public int showOnly; //Render only this (system) branch (formerly 'configuring')
    public enumParadigm Paradigm;
    public List<KeyValuePair<object, object>> mopUpvalues;
}

public class clsScreenHeaderState
{
    public string Path;
    public bool QuickFiltersVisible;
    public List<KeyValuePair<int, List<KeyValuePair<clsFilter, List<long>>>>> Filters; //As List(Of KeyValuePair(Of clsField, List(Of KeyValuePair(Of clsFilter, List(Of Int64)))))
    public List<KeyValuePair<int, clsPriorityDirection>> Sorts;
}