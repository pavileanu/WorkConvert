public class Register : System.Web.UI.Page
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		RegisterUser.ContinueDestinationPageUrl = Request.QueryString("ReturnUrl");
	}

	protected void  // ERROR: Handles clauses are not supported in C#
RegisterUser_CreatedUser(object sender, EventArgs e)
	{
		//        FormsAuthentication.SetAuthCookie(RegisterUser.UserName, False)

		string continueUrl = RegisterUser.ContinueDestinationPageUrl;
		if (string.IsNullOrEmpty(continueUrl)) {
			continueUrl = "~/";
		}

		Response.Redirect(continueUrl);
	}
}
