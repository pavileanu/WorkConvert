public class teams : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//returns a delimited set of ID^Team] s matching the Channel request parameter

		Literal lit;
		lit = new Literal();
		lit.Text = "!Begin";
		Form.Controls.Add(lit);

		clsTeam t;
		foreach ( t in iq.Channels(Request("channel")).Teams.Values) {
			lit = new Literal();
			lit.Text = t.ID + "^" + t.Name + "]";
			Form.Controls.Add(lit);
		}

		lit = new Literal();
		lit.Text = "!End";
		Form.Controls.Add(lit);

	}

}
