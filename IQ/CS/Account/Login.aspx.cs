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
public partial class Login : System.Web.UI.Page
{

protected void Page_Load(object sender, System.EventArgs e)
{
RegisterHyperLink.NavigateUrl = "Register.aspx?ReturnUrl=" + HttpUtility.UrlEncode(Request.QueryString["ReturnUrl"]);
}


protected void UserName_TextChanged(object sender, EventArgs e)
{

}

protected void LoginButton_Click(object sender, EventArgs e)
{

}
}
}
