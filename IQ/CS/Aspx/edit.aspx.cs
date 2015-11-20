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
public partial class edit1 : System.Web.UI.Page
{

protected void Page_Load(object sender, System.EventArgs e)
{

object q = null;
q = Request.RawUrl;
q = Strings.Split(System.Convert.ToString(q), "?")[1];

UInt64 lid = Convert.ToUInt64(Request.QueryString["lid"]);
System.Collections.Generic.Dictionary<string, clsEditHeader> EditHeaders = default(System.Collections.Generic.Dictionary<string, clsEditHeader>);
EditHeaders = new Dictionary<string, clsEditHeader>();

CoreCode.iq.set_sesh(lid, "editHeaders", EditHeaders);

System.Collections.Generic.List<string> asPage = default(System.Collections.Generic.List<string>); //Switches individual rows between Page And ListRow mode - things default to list, if their (path) is in here they're in page mode.
if (!CoreCode.iq.SeshContains(lid, "asPage"))
{
asPage = new List<string>();
CoreCode.iq.set_sesh(lid, "asPage", asPage);
}

//REMOVE THE lid (not sure why ??)
object[] B = Strings.Split(System.Convert.ToString(q), "&");
for (var I = 0; I <= (B.Length - 1); I++)
{
if (B[I].Substring(0, 4).ToUpper() == "LID=")
{
B[I] = "FGFG=0";
}
}
q = string.Join("&", B);

if (drpLanguage.SelectedIndex > -1)
{
q = q + "&language=" + drpLanguage.SelectedValue;
}

//Response.Write("<script language='javaScript'>alert('hello');</script>")
//drop the lid (becuase Embed adds it)

object script = null;

// embed(url, divID, append, sendNVPs) {
script = "embed(\'../editor/editor.aspx?" + System.Convert.ToString(q) + "\',\'EditPanel\',false,false);"; //will *REPLACE* EditPanel


Literal lit = new Literal();
lit.Text = "<script language=\'JavaScript\'>" + System.Convert.ToString(script) + "</script>;";
Page.Controls.Add(lit);

if (!Page.IsPostBack)
{
drpLanguage.DataSource = CoreCode.iq.ActiveLanguages.Values;
drpLanguage.DataTextField = "LocalName";
drpLanguage.DataValueField = "ID";
drpLanguage.DataBind();
}


}

protected void drpLanguage_SelectedIndexChanged(object sender, EventArgs e)
{
string selection = string.Empty;
}
}
}
