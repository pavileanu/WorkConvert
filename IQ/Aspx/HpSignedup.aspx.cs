using System.Globalization;

public class HpSignedup : System.Web.UI.Page
{
	private clsChannel selectedChannel;
	private string selectedlang;

	private string selectedCountry;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{

		if (Request.QueryString("lang") != null && Request.QueryString("mfr") != null) {
			string[] langarray = Split(Request.QueryString("lang"), "|");
			selectedlang = langarray(0);
			selectedCountry = langarray(1);
			labelCountry.Text = selectedCountry;

			object url = ConfigurationManager.AppSettings("BaseURL") + "/Aspx/SignIn.aspx?mfr=" + Request.QueryString("mfr");

			if (Request.QueryString("existingAccount") == "Y") {
				litRegistered.Text = string.Format("You already have a relevant {0} iQuote Universal account. Go <a href='{1}'>here</a> to log in or to reset your password.", Request.QueryString("mfr"), url);
			} else {
				litRegistered.Text = string.Format("You have successfully registered. Please check your email for a welcome message containing your password and details of how to <a href='{0}'>log in</a>.", url);
			}
		} else {
			Response.Redirect("SignIn.aspx");
		}

	}

}
