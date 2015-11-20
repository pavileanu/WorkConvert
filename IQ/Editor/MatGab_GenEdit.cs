public class GenEdit
{

	public string NullID(object obj)
	{
		if (obj == null) {
			return "null";
		} else {
			return obj.id.ToString;
		}

	}

	public string Plural(n)
	{

		if (LCase(Right(n, 1)) == "y") {
			Plural = Left(n, Len(n) - 1) + "ies";
		} else if (LCase(Right(n, 2) == "us")) {
			Plural = n + "es";
		} else {
			Plural = n + "s";
		}

	}

	public Panel EmptyPanel(string id)
	{

		Panel pnl = new Panel();
		pnl.ID = id;
		//& "." & Rnd(1).ToString  'TERRIBLE TODO CHANGE

		//Dim lbl As Label = New Label
		//lbl.ForeColor = Drawing.Color.White
		//lbl.BackColor = Drawing.Color.Blue
		//lbl.Text = pnl.ID
		//pnl.Controls.Add(lbl)

		return pnl;

	}

}
