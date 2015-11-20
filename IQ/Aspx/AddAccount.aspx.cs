
//used from the customer search screen - does not manage currency, role, etc..

public class NewAccount : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		uint64 lid = Request.QueryString("lid");


		if (!IsPostBack) {
			clsAccount buyerAccount__1 = (clsAccount)iq.sesh(lid, "BuyerAccount");
			clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

			TxtCompany.Text = buyeraccount.BuyerChannel.DisplayName(buyeraccount.Language);
			//this is locked
			TxtEmail.Text = buyeraccount.User.Email;
			//this has the @company.com
			TxtName.Text = buyeraccount.User.RealName;
			//initally empty
			TxtpriceBand.Text = buyerAccount__1.Priceband.Text;
			//this'll be blank initially

		}

		//add the script to close the iframe
		//  BtnGo.Attributes("onclick") = "closeIFrame('" & Request("frameid") & "'');"
		//we only want to close the frame if the validation is OK
		BtnCancel.Attributes("onclick") = "window.parent.closeIFrame('" + Request("frameid") + "');";

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnGo_Click(object sender, EventArgs e)
	{
		List<string> errormessages = new List<string>();

		UInt64 lid = Request.QueryString("lid");


		if (InStr(TxtEmail.Text, ".") & InStr(TxtEmail.Text, "@") & Left(TxtEmail.Text, 1) != "@") {
			clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");


			buyerAccount.User.Email = TxtEmail.Text;
			buyerAccount.User.RealName = TxtName.Text;

			object pw = GeneratePassword();
			buyerAccount.Password = simpleHash(pw);
			buyerAccount.Priceband = iq.getPriceBand(TxtpriceBand.Text);
			buyerAccount.MustChangePassword = true;

			buyerAccount.update(errormessages);
			buyerAccount.User.update(errormessages);

			Dictionary<string, string> tags = new Dictionary<string, string>();


			string baseurl = ConfigurationManager.AppSettings("BaseURL");

			object url;
			url = baseurl + "/aspx/signin.aspx";

			tags.Add("hostname", buyerAccount.SellerChannel.DisplayName(buyerAccount.Language));
			tags.Add("email", buyerAccount.User.Email);
			tags.Add("password", pw);
			tags.Add("firstname", Split(buyerAccount.User.RealName, " ")(0));
			tags.Add("url", url);
			tags.Add("extratext", baseurl == "http://uat.hpiquote.net" ? "<p>Please note this is a login for test purposes</p>" : "");
			tags.Add("mfr", buyerAccount.mfrCode);
			tags.Add("iquotesupportemail", iq.Addresses("iQuoteSupportEmail").Translation.text(English));

			List<string> em = new List<string>();
			if (chkWelcome.Checked) {
				SendEmail(buyerAccount.User.Email, "WelcomeEmail.htm", tags, buyerAccount.Language, em, false);
			}

			if (em.Count == 0) {
				Response.Write("<script language='JavaScript'>window.parent.closeIFrame('" + Request("frameid") + "');</script>");
			}

		} else {
			//dud email address
			TxtEmail.BackColor = Drawing.Color.Red;
		}

	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnCancel_Click(object sender, EventArgs e)
	{
	}
}
