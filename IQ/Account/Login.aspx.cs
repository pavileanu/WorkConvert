public class Login : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		RegisterHyperLink.NavigateUrl = "Register.aspx?ReturnUrl=" + HttpUtility.UrlEncode(Request.QueryString("ReturnUrl"));
	}



	protected void UserName_TextChanged(object sender, EventArgs e)
	{
	}


	protected void LoginButton_Click(object sender, EventArgs e)
	{
	}
}
