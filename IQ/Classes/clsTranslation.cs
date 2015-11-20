using System.Runtime.Serialization;
using dataAccess;


[DataContract()]
public class clsTranslation
{

    public clsTranslation()
    {

    }
    // Dim ID As Integer
    public int Key { get; set; }
    private Dictionary<clsLanguage, string> iText;
    public Dictionary<clsLanguage, int> ID { get; set; }

    public string Group { get; set; } //this translation belongs to a group of options - which would allow attributes to be picked from a list - instead of typed
    public int Order { get; set; } //this is the order of this option in that list

    public string compoundkey(clsLanguage language)
    {

        //compound key will be text^group^language
        return this.get_text(language) + "^" + this.Group + "^" + language.Code + "^" + System.Convert.ToString(this.Order);

    }

    public void addLanguage(clsLanguage language, string text, DataTable writecache)
    {
        if (!this.iText.ContainsKey(language))
        {
            this.iText.Add(language, text);
            if (writecache == null)
            {
                object sql = null;
                sql = "INSERT INTO [translation] ([key],[text],fk_language_id,[group],[order]) VALUES (";
                sql += this.Key + ",N" + da.SqlEncode(text) + "," + language.ID + "," + da.SqlEncode(Group) + "," + System.Convert.ToString(Order) + ");";
                //INserts the new row - and stores a copy of the ID in a dictioanry under the langauge
                this.ID.Add(language, da.DBExecutesql(sql, true));
            }
            else
            {
                System.Data.DataRow row = default(System.Data.DataRow);
                row = writecache.NewRow();
                row["key"] = this.Key;
                row["text"] = text;
                row["fk_language_id"] = language.ID;
                row["group"] = Group;
                row["order"] = Order;
                writecache.Rows.Add(row);
                //NB: this DOESNT increment the key - as were adding another language

            }
        }
        else
        {
            if (writecache == null)
            {
                object sql = null;
                sql = "UPDATE [translation] SET text = " + da.SqlEncode(text) + " WHERE [key] = " + System.Convert.ToString(this.Key) + " AND fk_language_id=" + language.ID + " AND [group]=" + da.SqlEncode(this.Group) + "";
                this.iText[language] = text;
                da.DBExecutesql(sql, false);
            }
        }

        clsIQ.IndexTL(this, language);
    }

    public void addLanguage(clsLanguage language, int id, string text)
    {
        if (!this.ID.ContainsKey(language))
        {
            this.ID.Add(language, id);
            this.iText.Add(language, text);
            clsIQ.IndexTL(this, language);
        }
    }
    public clsTranslation clone()
    {

        //todo .. doesn't do translations in other languages
        return new clsTranslation(this.iText.Keys(0), "Copy of " + this.iText.Values(0), this.Group, this.Order + 1, null, 0);

    }

    public Literal HTML(clsLanguage language)
    {
        Literal returnValue = default(Literal);

        returnValue = new Literal();
        if (this.iText.ContainsKey(language))
        {
            returnValue.Text = this.iText[language];
        }
        else
        {
            returnValue.Text = "<span class=\'missingTranslation\'>" + System.Convert.ToString(this.iText[English]) + "</span>";
        }

        return returnValue;
    }

    public long SortValue(clsLanguage language)
    {
        long returnValue = 0;

        if (this.Order > 0)
        {
            // If Order <> 1 Then Stop
            returnValue = Order;
        }
        else
        {

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

            returnValue = this.Key;

        }


        return returnValue;
    }

    public string get_text(clsLanguage language)
    {

        if (this.iText.ContainsKey(language))
        {
            if (language == English)
            {
                return (this.iText[language]);
            }
            else
            {
                return this.iText[language];
            }

        }
        else
        {
            if (this.iText.ContainsKey(English))
            {
                return this.iText[English]; //"*" & Me.iText(English)
            }
            else if (iq.i_language_Code.ContainsKey("KY") && this.iText.ContainsKey(iq.i_language_Code("KY"))) //ML Added check for KY existence as this was breaking the ?reload=1 on signin, master.aspx must not refer to anything which relies on the OM
            {
                return this.iText[iq.i_language_Code("KY")]; //(iq.i_language_Code("KY")) 'Return "**" & Me.iText(iq.i_language_Code("KY"))
            }
            else
            {
                return null; // should never happen
            }
        }

    }

    public void set_text(clsLanguage language, string value)
    {

        this.iText[language] = value;

    }

    public string get_textTranslation(clsLanguage language)
    {

        if (this.iText.ContainsKey(language))
        {
            return this.iText[language];
        }
        else
        {

            return "";

        }

    }

    public void set_textTranslation(clsLanguage language, string value)
    {

        this.iText[language] = value;

    }

    public void remove(clsLanguage language)
    {

        this.iText.Remove(language);

    }

    public static int NextKey()
    {
        int returnValue = 0;

        SqlClient.SqlDataReader reader = default(SqlClient.SqlDataReader);
        SqlClient.SqlConnection con = default(SqlClient.SqlConnection);
        con = da.OpenDatabase();

        reader = da.DBExecuteReader(con, "Select max([key])+1 as c from translation");
        if (reader.HasRows)
        {
            reader.Read();
            if (Information.IsDBNull(reader.Item(0)))
            {
                returnValue = 1;
            }
            else
            {
                returnValue = System.Convert.ToInt32(reader.Item(0));
            }
        }
        else
        {
            returnValue = 1;
        }

        reader.Close();
        con.Close();

        return returnValue;
    }

    public clsTranslation(clsLanguage language, string text, string group, int order, DataTable writecache, ref int nextkey)
    {

        //Creates a NEW translation


        this.iText = new Dictionary<clsLanguage, string>();
        this.ID = new Dictionary<clsLanguage, int>();

        this.iText.Add(language, text);
        this.Group = group;
        this.Order = order;

        if (nextkey != 0 && writecache == null)
        {
            Debugger.Break();
        }
        if (writecache != null && nextkey == 0)
        {
            Debugger.Break();
        }

        //Me.Key = iq.NextKey
        if (writecache == null)
        {
            object sql = null;
            this.Key = System.Convert.ToInt32(da.DBSelectFirst("select max([key])+1 from translation"));
            sql = "INSERT INTO [translation] ([key],[text],fk_language_id,[group],[order],deleted) VALUES (" + System.Convert.ToString(this.Key) + ",";
            sql += da.SqlEncode(text) + "," + language.ID + "," + da.SqlEncode(group) + "," + System.Convert.ToString(order) + ",0);";
            //INserts the new row - and stores a copy of the ID in a dictioanry under the langauge

            int id = System.Convert.ToInt32(da.DBExecutesql(sql, true));
            this.ID.Add(language, id); //NB: Translations have an array of ID's (one for each language)

            //now select back the ID


        }
        else
        {

            // Me.ID = -1
            this.Key = nextkey;
            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();
            row["key"] = this.Key;
            row["text"] = text;
            row["fk_language_id"] = language.ID;
            row["group"] = group;
            row["order"] = order;
            row["deleted"] = false;
            writecache.Rows.Add(row);

            //this isn't going to populate the ID's dictionary
            nextkey++;

        }

        clsIQ.IndexTL(this, language);

    }

    public clsTranslation(int key, clsLanguage language, string text, int id, string group = "", int order = 0)
    {

        //this constructor is called when reloading from the database.. it's slightly different from most (which have an ID)
        //tranlations have a KEY instead - becuase all the different (language) translations share tehen same KEY but have different ID's

        // Me.ID = id
        this.Key = key;
        //Me.Language = language
        //Me.Text = text
        if (this.iText == null)
        {
            this.iText = new Dictionary<clsLanguage, string>();
        }
        if (this.ID == null)
        {
            this.ID = new Dictionary<clsLanguage, int>();
        }
        this.Group = group;
        this.Order = order;

        this.ID.Add(language, id);
        this.iText.Add(language, text);
        clsIQ.IndexTL(this, language);

    }

    public bool delete(clsLanguage language)
    {

        if (this.ID.ContainsKey(language))
        {
            object sql = null;
            sql = "DELETE FROM translation WHERE id=" + this.ID(language) + ";";
            try
            {
                //it may not be possible to remove this translation if it still referenced by another object (RI)
                da.DBExecutesql(sql);


                clsIQ.deleteTL(this, language);

                return true;

            }
            catch
            {
                return false;
            }
        }


    }
    public void Update(clsLanguage language)
    {

        //        Stop

        object sql = null;
        sql = "Update translation set text=" + da.SqlEncode(get_text(language)) + ",[group]=" + da.SqlEncode(this.Group) + ",[order]=" + System.Convert.ToString(Order) + " where [key]=" + System.Convert.ToString(this.Key) + " and fk_language_id=" + language.ID;
        da.DBExecutesql(sql, false);

    }

} //clsTranslation