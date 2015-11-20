
using System.Text.RegularExpressions;

public class AccountSettings : System.Web.UI.Page
{


	private void  // ERROR: Handles clauses are not supported in C#
Account_Settings_Init(object sender, System.EventArgs e)
	{
		object activeLiveLanguage = from l in iq.Languages.Valueswhere l.Active == true & l.Live == true & l.Code != "KY";
		foreach ( kvp in activeLiveLanguage) {
			DDLLanguage.Items.Add(new ListItem(kvp.LocalName, kvp.ID));
		}

		foreach ( culturelist in iq.Cultures) {
			ddlCulture.Items.Add(new ListItem(culturelist.Value.Name, culturelist.Key));
		}
	}

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		UInt64 lid = Request.QueryString("lid");

		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		if (agentAccount == null)
			return;


		if (!IsPostBack) {
			DDLLanguage.SelectedValue = agentAccount.Language.ID;
			ddlCulture.SelectedValue = agentAccount.Culture.ID;
			TxtFullName.Text = agentAccount.User.RealName;
			TxtTelephone.Text = agentAccount.User.tel1.DisplayValue;
			txtEmail.Text = agentAccount.User.Email;
			TxtChangePassword.Text = "";
			TxtConfirmChangePassword.Text = "";
			TxtpriceBand.Text = agentAccount.Priceband.text;
			lbRoles.DataSource = agentAccount.Roles;
			lbRoles.DataBind();
			chkUpdadateAccounts.Text = Xlt("  Apply to all my iQuote accounts.", agentAccount.Language);
		}

		if (UserIsAdmin(lid))
			TxtpriceBand.Enabled = true;

		LblInfo.Text = "AccountID:" + agentAccount.ID + " UserID:" + agentAccount.User.ID;

		CompareValidator1.ErrorMessage = Xlt("The passwords you supplied do not match", agentAccount.Language);
		lblRegex.Text = Xlt("Passwords must be at least 8 characters and include mixed case and a number", agentAccount.Language);
		lblRegex.Visible = false;
		// vldRegex.Visible = False
		requiredPasswordConfirm.ErrorMessage = CompareValidator1.ErrorMessage;

		h1HeaderContainer.InnerHtml = Xlt("Account Settings", agentAccount.Language);
	}


	protected void  // ERROR: Handles clauses are not supported in C#
BtnSave_Click(object sender, EventArgs e)
	{
		// CompareValidators don't raise errors if one of the fields is left blank (!), so we enable a
		// RequiredFieldValidator too if a new password has been entered
		if (TxtChangePassword.Text.Length > 0) {
			requiredPasswordConfirm.Enabled = true;
		} else {
			requiredPasswordConfirm.Enabled = false;
		}

		Page.Validate();

		List<string> errormessages = new List<string>();
		UInt64 lid = Request.QueryString("lid");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");

		RegularExpressions.Regex rg = new RegularExpressions.Regex("^((?=.*?[a-z])(?=.*?[A-Z])(?=.*?[^a-zA-Z]).{8,})$");


		if (TxtChangePassword.Text != "" && TxtChangePassword.Text == TxtConfirmChangePassword.Text) {
			if (rg.IsMatch(TxtChangePassword.Text)) {
				agentAccount.Password = simpleHash(TxtChangePassword.Text);
				if (chkUpdadateAccounts.Checked) {
					foreach ( ac in agentAccount.User.Accounts.Values) {
						ac.Password = simpleHash(TxtChangePassword.Text);
						ac.update(errormessages);
					}
				}
			} else {
				lblregex.Visible = true;
			}
		}

		agentAccount.Language = iq.Languages(DDLLanguage.SelectedValue);

		agentAccount.User.RealName = TxtFullName.Text;
		agentAccount.User.tel1 = new nullableString(TxtTelephone.Text);
		agentAccount.Priceband = iq.getPriceBand(TxtpriceBand.Text);
		agentAccount.Culture = iq.Cultures(ddlCulture.SelectedValue);

		agentAccount.User.update(errormessages);
		agentAccount.update(errormessages);

		if (errormessages.Count == 0) {
			LblInfo.Text = Xlt("Changes saved successfully.", agentAccount.Language);
		} else {
			object p = new Panel();
			OutputErrors(p.Controls, errormessages, true);
			LblInfo.Controls.Add(p);
		}



	}


}
