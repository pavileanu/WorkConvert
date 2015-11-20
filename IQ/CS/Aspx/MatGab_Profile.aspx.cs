public class Profile : clsPageLogging
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//Outputs the current code profile - genertaed by calls to Pmark() and pAcc()  - (profile, mark and profile accumulate)

		Table tb = new Table();
		Form.Controls.Add(tb);
		tb.BorderStyle = BorderStyle.Solid;
		tb.BorderWidth = 1;


		tb.Controls.Add(headerrow());

		foreach ( k in Profiling.Profile.Keys) {
			foreach ( r in Profiling.Profile(k).Results(k)) {
				tb.Controls.Add(r);
			}
		}

	}

	private TableHeaderRow HeaderRow()
	{

		HeaderRow = new TableHeaderRow();
		TableHeaderCell th;
		HeaderRow.BackColor = Drawing.Color.CornflowerBlue;
		th = new TableHeaderCell();
		th.Text = "Name/rDepth";
		HeaderRow.Controls.Add(th);

		th = new TableHeaderCell();
		th.Text = "Calls";
		HeaderRow.Controls.Add(th);

		th = new TableHeaderCell();
		th.Text = "Time (ms)";
		HeaderRow.Controls.Add(th);

		th = new TableHeaderCell();
		th.Text = "Avg Time (µs)";
		HeaderRow.Controls.Add(th);


		th = new TableHeaderCell();
		th.Text = "Min Time (µs)";
		HeaderRow.Controls.Add(th);

		th = new TableHeaderCell();
		th.Text = "Max Time (ms)";
		HeaderRow.Controls.Add(th);

	}


	protected void  // ERROR: Handles clauses are not supported in C#
Button1_Click(object sender, EventArgs e)
	{
		Profiling.Profile.Clear();

	}
}
