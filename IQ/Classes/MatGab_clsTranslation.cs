using System.Runtime.Serialization;
using dataAccess;

[DataContract()]
public class clsTranslation
{


	public clsTranslation()
	{
	}
	// Dim ID As Integer
	private int Key {
		get { return m_Key; }
		set { m_Key = Value; }
	}
	private int m_Key;
	private Dictionary<clsLanguage, string> iText;
	private Dictionary<clsLanguage, int> ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private Dictionary<clsLanguage, int> m_ID;

	private string Group {
		get { return m_Group; }
		set { m_Group = Value; }
	}
	private string m_Group;
	//this translation belongs to a group of options - which would allow attributes to be picked from a list - instead of typed
	private int Order {
		get { return m_Order; }
		set { m_Order = Value; }
	}
	private int m_Order;
	//this is the order of this option in that list

	public string compoundkey(clsLanguage language)
	{

		//compound key will be text^group^language?
		return this.text(language) + "^" + this.Group + "^" + language.Code + "^" + this.Order;

	}

	public void addLanguage(clsLanguage language, string text, DataTable writecache)
	{
		if (!this.iText.ContainsKey(language)) {
			this.iText.Add(language, text);
			if (writecache == null) {
				object sql;
				sql = "INSERT INTO [translation] ([key],[text],fk_language_id,[group],[order]) VALUES (";
				sql += this.Key + ",N" + da.SqlEncode(text) + "," + language.ID + "," + da.SqlEncode(Group) + "," + Order + ");";
				//INserts the new row - and stores a copy of the ID in a dictioanry under the langauge
				this.ID.Add(language, da.DBExecutesql(sql, true));
			} else {
				System.Data.DataRow row;
				row = writecache.NewRow();
				row("key") = this.Key;
				row("text") = text;
				row("fk_language_id") = language.ID;
				row("group") = Group;
				row("order") = Order;
				writecache.Rows.Add(row);
				//NB: this DOESNT increment the key - as were adding another language

			}
		} else {
			if (writecache == null) {
				object sql;
				sql = "UPDATE [translation] SET text = " + da.SqlEncode(text) + " WHERE [key] = " + this.Key + " AND fk_language_id=" + language.ID + " AND [group]=" + da.SqlEncode(this.Group) + "";
				this.iText(language) = text;
				da.DBExecutesql(sql, false);
			}
		}

		clsIQ.IndexTL(this, language);
	}

	public void addLanguage(clsLanguage language, Int32 id, string text)
	{
		if (!this.ID.ContainsKey(language)) {
			this.ID.Add(language, id);
			this.iText.Add(language, text);
			clsIQ.IndexTL(this, language);
		}
	}
	public clsTranslation clone()
	{

		//todo .. doesn't do translations in other languages
		return new clsTranslation(this.iText.Keys(0), "Copy of " + this.iText.Values(0), this.Group, this.Order + 1);

	}

	public Literal HTML(clsLanguage language)
	{

		HTML = new Literal();
		if (this.iText.ContainsKey(language)) {
			HTML.Text = this.iText(language);
		} else {
			HTML.Text = "<span class='missingTranslation'>" + this.iText(English) + "</span>";
		}

	}

	public Int64 SortValue(clsLanguage language)
	{

		if (this.Order > 0) {
			// If Order <> 1 Then Stop
			SortValue = Order;

		} else {
			//there isn't reall any use for an alphabetical sort

			//Dim words() As String = Split(Trim(Me.iText(language)))
			//Dim pwr As Int64 = 1
			//SortValue = 0
			//For Each w In words.Take(3)
			//    If w <> "" Then  'phrases with a double space hav cause this to be empty
			//        If Len(w) > 1 Then
			//            SortValue += CSng(Asc(Mid(w, 2, 1))) * pwr : pwr = pwr * 64
			//        End If
			//        SortValue += Asc(Left(w, 1)) * pwr : pwr = pwr * 64
			//        ' SortValue += Asc(Left(w, 1)) * pwr : pwr = pwr * 256 - gets too big (and an exponent)
			//    End If
			//Next

			SortValue = this.Key;

		}


	}

	public string text {

		get {
			if (this.iText.ContainsKey(language)) {
				if (object.ReferenceEquals(language, English)) {
					return (this.iText(language));
				} else {
					return this.iText(language);
				}

			} else {
				if (this.iText.ContainsKey(English)) {
					return this.iText(English);
					//"*" & Me.iText(English)
				//ML Added check for KY existence as this was breaking the ?reload=1 on signin, master.aspx must not refer to anything which relies on the OM
				} else if (iq.i_language_Code.ContainsKey("KY") && this.iText.ContainsKey(iq.i_language_Code("KY"))) {
					return this.iText(iq.i_language_Code("KY"));
					//(iq.i_language_Code("KY")) 'Return "**" & Me.iText(iq.i_language_Code("KY"))
				} else {
					return null;
					// should never happen
				}
			}

		}



		set { this.iText(language) = value; }
	}

	public string textTranslation {

		get {
			if (this.iText.ContainsKey(language)) {
				return this.iText(language);

			} else {
				return "";

			}

		}



		set { this.iText(language) = value; }
	}


	public void @remove(clsLanguage language)
	{
		this.iText.Remove(language);

	}

	public static int NextKey()
	{

		SqlClient.SqlDataReader reader;
		SqlClient.SqlConnection con;
		con = da.OpenDatabase();

		reader = da.DBExecuteReader(con, "Select max([key])+1 as c from translation");
		if (reader.HasRows) {
			reader.Read();
			if (IsDBNull(reader.Item(0))) {
				NextKey = 1;
			} else {
				NextKey = reader.Item(0);
			}
		} else {
			NextKey = 1;
		}

		reader.Close();
		con.Close();

	}


	public clsTranslation(clsLanguage language, string text, string @group = "", int order = 0, DataTable writecache = null, ref int nextkey = 0)
	{
		//Creates a NEW translation


		this.iText = new Dictionary<clsLanguage, string>();
		this.ID = new Dictionary<clsLanguage, int>();

		this.iText.Add(language, text);
		this.Group = @group;
		this.Order = order;

		if (nextkey != 0 & writecache == null)
			System.Diagnostics.Debugger.Break();
		if (writecache != null & nextkey == 0)
			System.Diagnostics.Debugger.Break();

		//Me.Key = iq.NextKey
		if (writecache == null) {
			object sql;
			this.Key = da.DBSelectFirst("select max([key])+1 from translation");
			sql = "INSERT INTO [translation] ([key],[text],fk_language_id,[group],[order],deleted) VALUES (" + this.Key + ",";
			sql += da.SqlEncode(text) + "," + language.ID + "," + da.SqlEncode(@group) + "," + order + ",0);";
			//INserts the new row - and stores a copy of the ID in a dictioanry under the langauge

			int id = da.DBExecutesql(sql, true);
			this.ID.Add(language, id);
			//NB: Translations have an array of ID's (one for each language)

		//now select back the ID



		} else {
			// Me.ID = -1
			this.Key = nextkey;
			System.Data.DataRow row;
			row = writecache.NewRow();
			row("key") = this.Key;
			row("text") = text;
			row("fk_language_id") = language.ID;
			row("group") = @group;
			row("order") = order;
			row("deleted") = false;
			writecache.Rows.Add(row);

			//this isn't going to populate the ID's dictionary
			nextkey += 1;

		}

		clsIQ.IndexTL(this, language);

	}


	public clsTranslation(int key, clsLanguage language, string text, Int32 id, string @group = "", int order = 0)
	{
		//this constructor is called when reloading from the database.. it's slightly different from most (which have an ID)
		//tranlations have a KEY instead - becuase all the different (language) translations share tehen same KEY but have different ID's

		// Me.ID = id
		this.Key = key;
		//Me.Language = language
		//Me.Text = text
		if (this.iText == null)
			this.iText = new Dictionary<clsLanguage, string>();
		if (this.ID == null)
			this.ID = new Dictionary<clsLanguage, int>();
		this.Group = @group;
		this.Order = order;

		this.ID.Add(language, id);
		this.iText.Add(language, text);
		clsIQ.IndexTL(this, language);

	}

	public bool delete(clsLanguage language)
	{

		if (this.ID.ContainsKey(language)) {
			object sql;
			sql = "DELETE FROM translation WHERE id=" + this.ID(language) + ";";
			try {
				//it may not be possible to remove this translation if it still referenced by another object (RI)
				da.DBExecutesql(sql);


				clsIQ.deleteTL(this, language);

				return true;

			} catch {
				return false;
			}
		}


	}

	public void Update(clsLanguage language)
	{
		//        Stop

		object sql;
		sql = "Update translation set text=" + da.SqlEncode(text(language)) + ",[group]=" + da.SqlEncode(this.Group) + ",[order]=" + Order + " where [key]=" + this.Key + " and fk_language_id=" + language.ID;
		da.DBExecutesql(sql, false);

	}

}
//clsTranslation



