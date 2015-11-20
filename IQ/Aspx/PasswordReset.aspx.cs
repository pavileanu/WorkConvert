
public class PasswordReset : clsPageLogging
{

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		clsUser u = iq.Users((int)Request("uid"));

		Literal lit = new Literal();
		Pnl.Controls.Add(lit);
		lit.Text = string.Format("<div id='whichAccount'>{0}<br/><br/></div>", Xlt("Choose an account to reset its password.<br/>An email will be sent to you with instructions on how to complete the reset.", u.Accounts.Values(0).Language));

		object al = from ac in u.Accounts.Valuesorderby ac.displayName(u.Accounts.Values(0).Language);
		//Where ac.SellerChannel.BusinessName.Contains("estcoast")
		if (!al.Any) {
			LblFailed.Text += UiTrans("There is no account for ") + u.Email;
			LblFailed.Visible = true;

		} else {
			//as clsAccount In u.Accounts.Values where 
			foreach ( account in al) {
				//LblFailed.Text &= UiTrans("A new password has been sent to ") & u.Email & " " & UiTrans("please check your email.")

				Panel pnlIn = new Panel();
				pnlIn.CssClass = "resetOuter";
				Pnl.Controls.Add(pnlIn);

				Button btn = new Button();
				pnlIn.Controls.Add(btn);
				btn.Text = Xlt("Reset", account.Language);
				btn.Attributes("ac") = account.ID.ToString;

				lit = new Literal();
				lit.Text = "<div class='resetLabel'>" + account.SellerChannel.DisplayName(u.Accounts.Values(0).Language);
				lit.Text += "</div>";
				pnlIn.Controls.Add(lit);

				lit = new Literal();
				if (account.Manufacturer == Manufacturer.HPI) {
					lit.Text = " <img src='../images/HPI-Logo.jpg' height='18'/>";
				} else if (account.Manufacturer == Manufacturer.HPE) {
					lit.Text = " <img src='../images/HPE-Logo.jpg' height='18'/>";
				}
				pnlIn.Controls.Add(lit);

				btn.Click += resetAccount;

			}
		}

	}


	protected void resetAccount(object sender, EventArgs e)
	{
		Button b = (Button)sender;

		clsAccount account = iq.Accounts((int)b.Attributes("ac"));

		object em = account.ResetPassword();
		if (em.Count > 0) {
			foreach ( m in em) {
				LblFailed.Text += (m + "|");
			}
			LblFailed.Visible = true;
		} else {
			LblFailed.Text = UiTrans("A new password has been sent to ") + account.User.Email + " " + UiTrans("please check your email.");
			LblFailed.BackColor = Drawing.Color.Green;
			LblFailed.Visible = true;

			Response.AddHeader("REFRESH", "5;url=signin.aspx");
			//Response.Redirect("signin.aspx")

		}

	}
}
