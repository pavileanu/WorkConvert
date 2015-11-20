public class edit : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		object q;
		q = Split(Request.RawUrl, "?")(1);

		//     Response.Write("<script language='javaScript'>alert('hello');</script>")

		object script;
		script = "embed('/editor/editor.aspx?" + q + "&panelID=ctl00_MainContent_EditPanel','ctl00_MainContent_EditPanel',false,false);";

		Literal lit = new Literal();
		lit.Text = "<script language='JavaScript'>" + script + "</script>;";
		Page.Controls.Add(lit);

	}

}
