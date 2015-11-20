using dataAccess;

public class editlog : System.Web.UI.Page
{


	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		showEditsTable();

	}

	private TableRow MakeLogTableRow(SqlClient.SqlDataReader rdr)
	{

		TableRow tr = new TableRow();

		foreach ( column in Split("id,Action,Path,oldValue,NewValue,timestamp,fk_account_id_agent,comments,undone", ",")) {
			TableCell td = new TableCell();
			tr.Controls.Add(td);

			string v;
			if (column.ToLower == "id") {
				v = "";
				Button btn = new Button();
				btn.Text = "undo";
				btn.Attributes("rowID") = rdr.Item("id");
				btn.Click += undo;
				td.Controls.Add(btn);

			} else if (column.ToLower == "fk_account_id_agent") {
				v = iq.Accounts(rdr.Item(column)).User.RealName;
				td.Text = v;
			} else {
				v = rdr.Item(column).ToString;
				td.Text = v;
			}
		}

		return tr;

	}

	public object showEditsTable()
	{


		form1.Controls.Clear();
		form1.Controls.Add(NewLit("<p>Most recent activity is at the top</p><p>"));
		Table t = new Table();
		t.CssClass = "adminTable";
		Form.Controls.Add(t);
		TableHeaderRow thr = MakeTHR("id,Action,path,oldvalue,newvalue,timestamp,by,comments,undone", "Edit, Upate, Add or Delete|The location (in the object model) of the item changed|previous data value|New (current) data value,When the change was made|Who made the change,Editable Comments|When (if) this change was undone.", "");
		t.Controls.Add(thr);

		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, "Select top 50 * from editlog order by id desc");


		while (rdr.Read) {
			TableRow tr = MakeLogTableRow(rdr);
			t.Controls.Add(tr);

		}

		rdr.Close();
		con.Close();

	}

	public object undo(object obj, EventArgs e)
	{

		Button btn = obj;

		object sql;
		sql = "SELECT oldvalue,path,action from Editlog where id =" + btn.Attributes("rowID");
		SqlClient.SqlConnection con = da.OpenDatabase;
		SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql);
		rdr.Read();

		undoLogEntry(rdr.Item("action"), rdr.Item("path"), rdr.Item("oldvalue"));

		sql = "update editlog set undone=getdate() where id=" + btn.Attributes("rowID");

		da.DBExecutesql(sql);

		showEditsTable();

	}

	private object undoLogEntry(string action, string path, string oldvalue)
	{

		object obj;
		object pobj;

		List<string> errormessages = new List<string>();

		string[] bits = Split(path, ".");
		path = Left(path, InStrRev(path, ".") - 1);

		Reflection.ParsePath(path, obj, pobj, errormessages);

		if (action == "E") {
			setProperty(obj, bits.Last, oldvalue, 0, errormessages, false);

		}

		foreach ( e in errormessages) {
			Form.Controls.Add(ErrorDymo(e));
		}

	}

}
