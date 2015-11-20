public class help : System.Web.UI.Page
{



	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		GK.OneTimeTokenClient cl;
		cl = new GK.OneTimeTokenClient();

		Table t = new Table();
		t.CellPadding = 3;
		//   t.BorderWidth = 1


		t.Attributes("class") = "wsHelpTable";
		//   t.Attributes("style") = "border-collapse:collapse;cellpadding:2px;"
		Form.Controls.Add(t);
		t.Controls.Add(MakeTHR("Name,Required,Notes,Example(value),min length,Max length,RegEx", "", ""));

		foreach ( i in cl.Help()) {
			t.Controls.Add(helpTableRow(i));
		}

	}

	public object helpTableRow(GK.clsName i)
	{

		helpTableRow = new TableRow();

		TableCell td = new TableCell();
		helpTableRow.controls.@add(td);
		td.Text = i.name;

		td = new TableCell();
		td.BorderStyle = BorderStyle.Solid;
		td.BorderWidth = 1;

		helpTableRow.controls.@add(td);
		td.Text = i.Required.ToString;

		td = new TableCell();
		td.BorderStyle = BorderStyle.Solid;
		td.BorderWidth = 1;

		helpTableRow.controls.@add(td);
		td.Text = i.Notes;

		td = new TableCell();
		helpTableRow.controls.@add(td);
		td.BorderStyle = BorderStyle.Solid;
		td.BorderWidth = 1;

		td.Text = i.Example;

		td = new TableCell();
		td.BorderStyle = BorderStyle.Solid;
		td.BorderWidth = 1;

		helpTableRow.controls.@add(td);
		td.Text = i.MinLength;

		td = new TableCell();
		td.BorderStyle = BorderStyle.Solid;
		td.BorderWidth = 1;

		helpTableRow.controls.@add(td);
		td.Text = i.MaxLength;

		td = new TableCell();
		td.BorderStyle = BorderStyle.Solid;
		td.BorderWidth = 1;

		helpTableRow.controls.@add(td);
		td.Text = i.RegEx;

	}

}
