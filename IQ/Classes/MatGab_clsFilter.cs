using dataAccess;
using System.Xml.Serialization;

public class clsFilter
{

	public int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	[XmlIgnore()]
	public clsTranslation DisplayText {
		get { return m_DisplayText; }
		set { m_DisplayText = Value; }
	}
	private clsTranslation m_DisplayText;
	public string Filter;

	public string Code;

	public clsFilter()
	{
	}

	//Public Sub New(screen As clsScreen, ID As Integer, propertyname As String, lookupof As String, embedScreen As clsScreen, labeltext As String, helptext As String, validation As clsValidation, inputtype As clsInputType, length As Integer, order As Integer, width As Single, defaultvalue As String, visible As Boolean)
	//Public Sub New(screen As clsScreen, ID As Integer, propertyname As String, PropertyClass As String, lookupof As String, labeltext As String, helptext As String, validation As clsValidation, inputtype As clsInputType, length As Integer, order As Integer, width As Single, height As Single, defaultvalue As String, visibleList As Boolean, visiblePage As Boolean, defaultFilter As String, defaultSort As String)

	public clsFilter(int id, string code, clsTranslation DisplayText, string filter)
	{
		this.ID = id;
		this.DisplayText = DisplayText;
		this.Filter = filter;
		this.Code = code;

		iq.Filters.Add(this.ID, this);
		iq.i_Filters_Code.Add(this.Code, this);

	}

	public bool compare(Int64 x, List<Int64> y, List<Int64> z = null)
	{
		switch (Code) {
			case "EQ":
				return y.Contains(x);
			case "GE":
				return y.Where(yy => yy <= x).Count > 0;
			case "LE":
				return y.Where(yy => yy >= x).Count > 0;
			case "LEGE":
				return y.Where(yy => yy >= x).Count > 0 && z.Where(yy => yy <= x).Count > 0;
			default:
				object a = 9;
		}
	}



	//Public Sub New(DisplayText As clsTranslation, filter As String, Code As String)
	// Dim sql$

	//sql$ = "INSERT INTO [filter] (fk_translation_id,filter,code) "
	//sql$ &= "VALUES (" & da.SqlEncode(filter) screen.ID & "," & da.SqlEncode(propertyName) & "," & da.SqlEncode(labelText) & "," & da.SqlEncode(helpText) & "," & NullID(validation) & ","
	//sql$ &= SqlEncode(lookupOf) & "," & inputType.ID & "," & length & "," & order & "," & width & "," & height & "," & da.SqlEncode(defaultValue) & "," & IIf(visibleList, "1", "0").ToString & "," & IIf(visiblePage, "1", "0").ToString & "," & da.SqlEncode(defaultFilter) & "," & da.SqlEncode(defaultSort) & ");"


	//Me.order = order
	//Me.width = width
	//Me.height = height
	//Me.defaultValue = defaultValue
	//Me.visibleList = visibleList
	//Me.visiblePage = visiblePage
	//Me.defaultFilter = defaultFilter
	//Me.defaultSort = defaultSort

	//iq.Fields.Add(Me.ID, Me)
	//Me.Screen.i_field_property.Add(Me.propertyName, Me)
	//'Me.Screen.Fields.Add(Me.ID, Me)

	//oPropertyName = propertyName

	//End Sub





	public void update()
	{
		//Dim sql$
		//sql$ = "UPDATE [field] set "
		//sql$ &= "fk_screen_id=" & Me.Screen.ID
		//sql$ &= ",property=" & da.SqlEncode(Me.propertyName)
		//'sql$ &= ",propertyClass=" & da.SqlEncode(Me.PropertyClass)
		//sql$ &= ",label=" & da.SqlEncode(Me.labelText)
		//sql$ &= ",helptext=" & da.SqlEncode(Me.helpText)
		//Dim vid As String
		//If Me.Validation Is Nothing Then
		//    vid = "null"
		//Else
		//    vid = Me.Validation.ID
		//End If
		//sql$ &= ",fk_validation_id=" & vid
		//sql$ &= ",lookupof=" & da.SqlEncode(Me.lookupOf)
		//sql$ &= ",fk_inputtype_id=" & Me.InputType.ID
		//sql$ &= ",length=" & Me.length
		//sql$ &= ",[order]=" & Me.order
		//'    sql$ &= ",fk_screen_id_embed=" & NullID(Me.EmbedScreen)
		//sql$ &= ",[width]=" & Me.width
		//sql$ &= ",[height]=" & Me.height
		//sql$ &= ",defaultvalue=" & da.SqlEncode(Me.defaultValue)
		//sql$ &= ",visibleList=" & IIf(Me.visibleList, "1", "0").ToString
		//sql$ &= ",visiblePage=" & IIf(Me.visiblePage, "1", "0").ToString
		//sql$ &= ",defaultfilter=" & da.SqlEncode(Me.defaultFilter)
		//sql$ &= ",defaultsort=" & da.SqlEncode(Me.defaultSort)

		//sql$ &= " WHERE ID=" & Me.ID

		//DBExecutesql(sql$, False)


		//Me.Screen.i_field_property.Remove(oPropertyName)
		//Me.Screen.i_field_property.Add(Me.propertyName, Me)

		//If Not Me.Screen.Fields.ContainsKey(Me.ID) Then Me.Screen.Fields.Add(Me.ID, Me) 'This is for when we've added one (using the New button)

		//oPropertyName = propertyName


	}



	public void Delete()
	{
		object sql;
		sql = "DELETE FROM [field] WHERE ID=" + this.ID;

		da.DBExecutesql(sql);

		//Me.Screen.Fields.Remove(Me.ID)
		// Me.Screen.i_field_property.Remove(oPropertyName)

	}



}
