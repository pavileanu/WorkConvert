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
public class clsPageLogging : Page
{
public clsPageLogging()
{
// VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
errorMessages = new List<string>();

}

internal System.Collections.Generic.List<string> errorMessages; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

Stopwatch sw = new Stopwatch();

public void Page_Init(object sender, System.EventArgs e)
{

sw.Start();
//MyBase.OnInit(e)
}

protected override void OnLoadComplete(EventArgs e)
{
sw.Stop();

UInt64 lid = 0;
try
{
if (Context.Request != null && Context.Request.QueryString.Count > 0)
{
lid = Context.Request.QueryString["lid"];
}

AuditLog.Instance.Add(
lid, 
"PageLoad", 
"", 
"", 
errorMessages, null, 
this.Context.Request.Path, 
this.Context.Request.RawUrl, 
sw.ElapsedMilliseconds, 
this.Context.Request.HttpMethod, System.Convert.ToString(this.Context.Request.UrlReferrer != null ? this.Context.Request.UrlReferrer.OriginalString : string.Empty));
}
catch (Exception)
{
//Oops, dont crash
}

base.OnLoadComplete(e);
}


}

}
