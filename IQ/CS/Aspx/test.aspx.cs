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
public partial class Test : System.Web.UI.Page
{

private void Page_Init(object sender, System.EventArgs e)
{

//Dim tb As New TextBox
//tb.BackColor = Drawing.Color.Green
//Form.Controls.Add(tb)


}

protected void Page_Load(object sender, System.EventArgs e)
{

//  If Not IsPostBack Then
TextBox tb = new TextBox();
tb.BackColor = System.Drawing.Color.Green;
Form.Controls.Add(tb);

// End If

}

protected void Button1_Click(object sender, EventArgs e)
{

Debug.Print(TextBox1.Text);

}
}
}
