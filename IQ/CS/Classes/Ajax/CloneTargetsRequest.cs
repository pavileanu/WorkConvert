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

using System.Web.Http;
using System.Net.Http;

namespace IQ
{

public class CloneTargetsRequest : HttpRequestMessage
{

public int ScreenId;
public string Path;
public System.Collections.Generic.List<string> Targets;
public UInt64 lid;
public string Level;
public string LevelValue;

}

}
