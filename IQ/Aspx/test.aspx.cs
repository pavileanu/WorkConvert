public class Test : System.Web.UI.Page
{


	private void  // ERROR: Handles clauses are not supported in C#
Page_Init(object sender, System.EventArgs e)
	{
		//Dim tb As New TextBox
		//tb.BackColor = Drawing.Color.Green
		//Form.Controls.Add(tb)


	}


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//  If Not IsPostBack Then
		TextBox tb = new TextBox();
		tb.BackColor = Drawing.Color.Green;
		Form.Controls.Add(tb);

		// End If

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button1_Click(object sender, EventArgs e)
	{
		Debug.Print(TextBox1.Text);

	}
}
