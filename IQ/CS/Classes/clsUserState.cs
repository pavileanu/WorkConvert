// VBConversions Note: VB project level imports
using System.Text.RegularExpressions;
using System.Web.UI.HtmlControls;
using System.Diagnostics;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data;
using System.Collections.Specialized;
using System.Web.Profile;
using Microsoft.VisualBasic;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections;
using System;
using System.Web;
using System.Web.UI;
using System.Web.SessionState;
using System.Text;
using System.Web.Caching;
using System.Web.UI.WebControls.WebParts;
using System.Linq;
// End of VB project level imports


namespace IQ
{
[Serializable]public class clsUserState
{
public string lid;
public string root;
public string path;
public int? QuoteID;
public string foci; //is a CD list
public string treeCursorPath;
public System.Collections.Generic.List<CoreCode.KeyValuePair<string, clsBranchState>> branchStates;
public int AgentAccount;
public int BuyerAccount;
public System.Collections.Generic.List<clsScreenHeaderState> ScreenHeaders;
public int showOnly; //Render only this (system) branch (formerly 'configuring')
public enumParadigm Paradigm;
public System.Collections.Generic.List<CoreCode.KeyValuePair<object, object>> mopUpvalues;
}

public class clsScreenHeaderState
{
public string Path;
public bool QuickFiltersVisible;
public System.Collections.Generic.List<CoreCode.KeyValuePair<int, List<CoreCode.KeyValuePair<clsFilter, List<long>>>>> Filters; //As List(Of KeyValuePair(Of clsField, List(Of KeyValuePair(Of clsFilter, List(Of Int64)))))
public System.Collections.Generic.List<CoreCode.KeyValuePair<int, clsPriorityDirection>> Sorts;
}
}
