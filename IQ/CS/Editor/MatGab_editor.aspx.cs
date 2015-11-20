//Option Strict On

using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using dataAccess;

public class editor : System.Web.UI.Page
{

	public enum EnumViewType
	{
		pageView = 1,
		listView = 2
	}

	 Sort;

	 Filter;
		//dictionary of integer,something
	public object ops;
	private UInt64 lid;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		//Each (and every!) (ajax) call to editor.aspx generates a panel.. 
		//The path tells us what we're editing - and that content is placed into it's parent (the previous path segment)

		//This page edits either a Dictionary or an Object , ultimately belonging to the IQ object
		//it takes a querystring of the form..
		//editor.aspx?path= Channels(1).Users(5)
		//or
		//editor.aspx?path=Channels(1).Users
		//or 
		//products(1273).ProductAttributes(3892)
		//or
		//Channels(47).children(839).children(3940).users
		//The indices in all the above are ID's (keys in dictionaries)

		//The end item in the path is loaded into OBJ for editing - if it's a dictionary you'll get a list editing view,
		//if it's an object - you'll get a page editor

		//The information for which properties of the object(s) should be displayed, how, with what help, validation and default values - comes from a template [SCREEN] and a set of [FIELD]s relating to that screen
		//Those templates are initially generated via MakeScreen().. and can then be maintained using the generic editor itself.
		//Each screen edits one type of Object

		//Editor.aspx also accepts a URL parameter &Default - which contains as slash ('/') delimited list of name, value pairs to set defaults on an added object
		object j = Request.RawUrl;

		UInt64 lid = 0;

		if (!UInt64.TryParse(Request.QueryString("lid"), lid))
			Response.Redirect("/aspx/signin.aspx");

		object Obj = iq;
		//Nothing              'this is the object we're editing - which *may* be a dictionary
		object ParentObj = null;
		//this is it's parent - used to set some defaults especially in recursive objects (eg, channels, threads)
		clsScreen screen = null;
		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");


		if (agentAccount == null)
			Response.Redirect("/aspx/signin.aspx");
		clsLanguage language = agentAccount.Language;
		object cmd = Request("cmd");
		clsLanguage translationlanguage = new clsLanguage();
		// This is very significant !
		object path = Request.QueryString("path");
		//this INITIALLY the path passed in the GET URL - but subsequently that POSTED passed in the FORM (from embed()'s rexec )
		if (Request.Form("path") != null)
			path = Request.Form("path");
		if (Request.QueryString("language") != null) {
			translationlanguage = iq.Languages(Request.QueryString("language"));
		}


		List<string> errorMessages = new List<string>();

		path = replaceSessionVariableTags(path, lid, errorMessages);
		//Things like UserID can be embedded in [brackets] and will be replaced with session variables

		//walk' the path to find the obejct and its parent (either of which *may* be dictionaries)
		ParsePath(path, Obj, ParentObj, errorMessages);


		Panel MyPanel__1 = new Panel();

		if (Obj == null) {
			errorMessages.Add("Path: " + path + " evaluates to nothing");

		} else {
			//Find out what kind of screen to use to edit this kind of object
			screen = fetchScreenTemplate(Obj, ParentObj, errorMessages);

			//save any changes  (posted in the NVPs from Embed/rexec) - really need to see if this affects the view (filtering sorting)
			if (cmd != null && cmd.Contains("saveTranslate")) {
				string[] arrayLang = Split(cmd, "_");
				clsLanguage saveLanguage = iq.Languages(arrayLang(1));
				SaveChanges(screen, path, Obj, agentAccount, errorMessages);
				cmd = "";
			} else {
				SaveChanges(screen, path, Obj, agentAccount, errorMessages);
			}


			//The edit headers hold the state information for this list (in the editor) 
			//including Sort orders and Priorities, Column widths, Filtes, pagination info
			//They are similar to (& might ultimately get consolidated with) MatrixHeaders
			//The are indexed by the path to the dictionary they head - for example Products(3042).ProductAttributes

			//Dim divid As String = Request("DivId")

			clsEditHeader editheader = null;
			//If Reflection.IsDictionary(Obj) Then
			Dictionary<string, clsEditHeader> editHeaders;
			editHeaders = iq.sesh(lid, "editHeaders");

			if (editHeaders.ContainsKey(path)) {
				editheader = editHeaders(path);
			} else {
				editheader = new clsEditHeader(path, Obj, screen, buyerAccount, agentAccount.Language, errorMessages, lid);
				editHeaders.Add(path, editheader);
			}

			bool expanded = false;
			//Is this object pivoted into 'page' mode

			if (ProcessCommand(lid, buyerAccount, language, screen, path, Obj, ParentObj, cmd, editheader, errorMessages)) {
				OutputErrors(holder.Controls, errorMessages, lid, true);

				//re-fetch the screen template - as processing the command may have altered it (eg. deletescreen)
				screen = fetchScreenTemplate(Obj, ParentObj, errorMessages);

				//Dim showheaders As Boolean = True  'Lists need headers


				if (Reflection.IsDictionary(Obj) == false) {
					if (iq.SeshContains(lid, "expanded") && iq.sesh(lid, "expanded").contains(path))
						expanded = true;
				}
			}

			//we're building a panel to edit OBJ - which might be a dictionary - or a single row (accoring to Path)
			//we *always* build a panel - even if it's closed

			if (Split(cmd, ",")(0) != "del") {
				//  mypanel = BuildPanel(lid, editheader, path, Obj, divid, screen, expanded, errorMessages, cmd, editheader.translationLanguage)
				mypanel = BuildPanel(lid, editheader, path, Obj, path, screen, expanded, errorMessages, cmd, editheader.translationLanguage);
			} else {
				Label dl = new Label();
				dl.Text = "deleted";
				mypanel.Controls.Add(dl);
			}
		}

		holder.Controls.Add(MyPanel__1);
		holder.Controls.Add(editor.MakeButton(true, "Change Log", "View/undo recent changes made with the editor)", "window.open('/editor/editlog.aspx');"));

		OutputErrors(holder.Controls, errorMessages, lid, true);

	}

	private clsScreen fetchScreenTemplate(object obj, object parentobj, ref List<string> errormessages)
	{

		//The 'right' screen to edit a given type of object - is determined from a compound of the parentOb's and Obj's 'type'
		//It then finds a screen with a TITLE clsParent.clsObj - for example clsChannel.clsAccount edits a channels buyerAccounts

		// If Reflection.IsDictionary(obj) Then Stop
		if (Reflection.IsDictionary(parentobj))
			System.Diagnostics.Debugger.Break();

		// Dim isDictionary As Boolean = False
		// isDictionary = Reflection.IsDictionary(obj)

		Type pty = Reflection.TypeOfDicOrObj(parentobj);
		Type ty = Reflection.TypeOfDicOrObj(obj);

		//Make a new screen template 'Just in time'
		object ck = pty.Name + "." + ty.Name;
		if (!iq.i_screens_title.ContainsKey(ck)) {
			//channel.buyeraccounts
			//user.accounts
			//clsChannel.clsAccount (buyeraccounts)
			//clsaccount.clsuser
			//clsUser.clsAccount (users accounts)

			if (!System.Type.GetType("IQ." + ty.Name, true, true) == null) {
				object code;
				code = ty.Name;
				if (Left(code, 3).ToLower == "cls")
					code = Mid(code, 4);
				MakeScreen(ck, code, ty, parentobj.GetType, errormessages);
			} else {
				errormessages.Add("Unrecognised type:IQ." + ty.Name);
				//unrecognised type
				// Stop
			}
		}

		fetchScreenTemplate = iq.i_screens_title(ck);
		// Then  'which screen we use is based on the type of object we're going to edit

	}

	private bool ProcessCommand(UInt64 lid, clsAccount buyeraccount, clsLanguage language, clsScreen screen, path, object Obj, object ParentObj, string cmd, ref clsEditHeader editHeader, ref List<string> errorMessages)
	{

		//returns false is thhere is no requirement to render the panel - eg. close was pressed

		ProcessCommand = true;

		if (ParentObj == null)
			ParentObj = Obj;
		// WHAT - WHY ? (pointing unparented threads to themselves - maybe?)

		[] parts = Split(cmd, ",");

		if (InStr(cmd, "=") > 0)
			errorMessages.Add("Cmd should not contain = use , ");

		switch (parts(0).ToLower) {

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"widen":
			case "narrow":
			case "promote":
			case "demote":
			case "show":
			case "hide":
				clsField fld;
				fld = iq.Fields((int)parts(1));

				editHeader.changeLayout(parts(0), fld, errorMessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"add":


				AddNew(lid, screen, ParentObj, editHeader, buyeraccount, language, errorMessages);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"deletescreen":

				Type pty = Reflection.TypeOfDicOrObj(ParentObj);
				Type ty = Reflection.TypeOfDicOrObj(Obj);

				//deindex it
				object ck = pty.Name + "." + ty.Name;
				iq.i_screens_title.Remove(ck);

				//Deletes the current screen layout (such that it will be remade from the underlying sturcture)
				DeleteScreen((int)parts(1), errorMessages);
			//  ProcessCommand = False 'closes the panel (as the old screen def is now invalid)

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"del":
				//the parameter on the del command is the object ID on dictionaries
				if (Reflection.IsDictionary(Obj)) {
					int id = (int)parts(1);
					if (Obj.containskey(id)) {
						Obj(id).delete();
						//calls the delete method on the object to remove it from the database
						Obj.@remove(id);
						//removed the object from its
						DataRow r = editHeader.DT.Rows.Find(id);
						r.Delete();
					}
				} else {
					if (editHeader.DT != null) {
						DataRow r = editHeader.DT.Rows.Find(Obj.ID);
						r.Delete();
					}
					Obj.delete(errorMessages);

				}
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"copy":
				//Dim ascreen As clsScreen = iq.Screens(Request("copyscreen")).copy
				//Dim theobj As Object = Obj(CInt(parts(1)))
				//theobj.copy()  'the object in question must expose a copy method (screens do !)

				Obj.copy();
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"priority":
				System.Diagnostics.Debugger.Break();
			//see sort
			//editHeader.UpdateSorts(New clsPriorityDirection(parts(1)))

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"changefilter":

				editHeader.UpdateFilters(parts(1), parts(2), parts(3));
				editHeader.addMissingColumns(Obj, buyeraccount, language, new HashSet<string>(), errorMessages, lid);
			//anything we're (now) sorting or filter by needs to be added to the dataview (from the dictionary)

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"removefilter":


				editHeader.RemoveFilter(parts(1));
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"from":
				//Pagination
				editHeader.Fromindex = parts(1);

			case  // ERROR: Case labels with binary operators are unsupported : Equality
"hist":

				errorMessages.Add("Audit Trail/History is not currently supported");
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"close":

				//don't build the panel
				ProcessCommand = false;
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"expand":
				if (!iq.SeshContains(lid, "expanded")) {
					iq.sesh(lid, "expanded") = new List<string>();
				}

				if (path != "")

					iq.sesh(lid, "expanded").@add(path);
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"collapse":
				if (iq.sesh(lid, "expanded").contains(path)) {
					iq.sesh(lid, "expanded").@remove(path);

				}
			case  // ERROR: Case labels with binary operators are unsupported : Equality
"language":
				string languageValue = Request("value");

				clsLanguage translationlanguage = new clsLanguage();
				translationlanguage = iq.Languages(Request.QueryString("value"));
				editHeader.translationLanguage = translationlanguage;
			default:

				if (cmd != "") {
					errorMessages.Add("Unrecognised cmd parameter '" + cmd + "'");

				}
		}

	}

	private object InLineAdd(UInt64 lid, object parentobj, string prop, clsAccount buyerAccount, clsLanguage language, clsEditHeader editHeader, ref List<string> errorMessages)
	{

		//they've pressed the 'inline add' to add an object to a picklist
		//we create the last object on the path - and insert a panel to edit it
		object NewObj;
		string errorMessage = "";
		object typename;

		typename = Reflection.TypeOfProperty(parentobj, prop);

		System.Type ty;
		ty = System.Type.GetType("IQ." + typename);

		//Make a new screen template 'Just in time'
		if (!iq.i_screens_title.ContainsKey(typename.ToString)) {
			MakeScreen(Request("Path"), Left(ty.ToString, 5), ty, null, errorMessages);
		}

		clsScreen screen;

		screen = iq.i_screens_title(typename);
		//which screen we use is based on the type of object we're going to edit

		NewObj = AddNew(lid, screen, parentobj, editHeader, buyerAccount, language, errorMessages);

		return NewObj;

	}


	private PlaceHolder PageButtons(Path, string divid, int CurrentFromIndex, bool isFirstPage, bool isLastPage, int perPage)
	{

		PlaceHolder ph = new PlaceHolder();

		if (!isFirstPage) {
			ph.Controls.Add(MakeButton(false, "◄", "Previous page", EmbedScript(Path, divid, "From," + (string)CurrentFromIndex - perPage, false, true)));
		}

		if (!isLastPage) {
			ph.Controls.Add(MakeButton(false, "►", "Next page", EmbedScript(Path, divid, "From," + (string)CurrentFromIndex + perPage, false, true)));

		}

		return ph;

	}



	private void ExpandHistory(UInt64 lid, path, clsScreen screen, Panel pnl, object obj, string hp, clsLanguage language, clsAccount buyerAccount, clsLanguage translateLanguage)
	{
		//show only those members whos ...root matches (which will include the original) - but NOT the 'current' (which is the parent)
		List<string> em = new List<string>();
		foreach ( row in obj.values) {
			if (Reflection.WalkPropertyValue(row, "AuditRoot", em).id == hp) {
				pnl.Controls.Add(EditObj(screen, row, false, pnl, (obj.current), language, buyerAccount, true, path, false,
				em, translateLanguage));
			}
		}

	}

	private string replaceSessionVariableTags(l, UInt64 lid, ref List<string> errorMessages)
	{

		int o;
		int c = 1;
		List<string> tags = new List<string>();

		do {
			o = InStr(c, l, "[");
			if (o == 0)
				break; // TODO: might not be correct. Was : Exit Do
			//no more open's
			c = InStr(o + 1, l, "]");
			if (c == 0) {
				errorMessages.Add(l + " - tag was unclosed");
				break; // TODO: might not be correct. Was : Exit Do
				//tag was unclosed
			}

			tags.Add(Mid(l, o + 1, c - o - 1));
		} while (true);

		foreach ( t in tags) {
			if (iq.sesh(lid, t) == null) {
				errorMessages.Add("session variable " + lid + "(" + t + ") was nothing ");
			} else {
				l = Replace(l, "[" + t + "]", iq.sesh(lid, t));
			}
		}

		return l;

	}

	/// <summary>
	/// 'The editor(.aspx) works by nesting many instances of itself to edit the descendant properties
	/// </summary>
	/// <param name="lid">Login id (key to session)</param>
	/// <param name="path">Path in the object model to the thing (dictionary or object) to edit</param>
	/// <param name="obj"></param>
	/// <param name="screen"></param>
	/// <returns></returns>
	/// <remarks>Form_load (which is typically called via ajax).. processes any 'command' then builds a panel which is embeded</remarks>
	private Panel BuildPanel(UInt64 lid, clsEditHeader editheader, path, object obj, string divID, clsScreen screen, bool expanded, ref List<string> errorMessages, string cmd, clsLanguage translationLang)
	{
		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		clsLanguage language = agentAccount.Language;

		BuildPanel = new Panel();
		BuildPanel.ID = divID;

		//  Dim editheader As clsEditHeader = Nothing
		//  Dim editheaders As Dictionary(Of String, clsEditHeader)
		//  editheaders = CType(iq.sesh(lid, "editHeaders"), Dictionary(Of String, clsEditHeader))


		//build a hashset from the CD list stored in the sesstion variable
		HashSet<string> foci = new HashSet<string>(Split(iq.sesh(lid, "foci"), ",").ToList);


		//   Dim fromIndex As Integer = iq.sesh(lid, "from." & path$) 'Pagination
		//   Dim perpage As Integer = iq.sesh(lid, "perpage." & path$) 'Pagination

		int L;
		L = 8 * Val(Request("depth"));

		if (cmd != "collapse") {
			BuildPanel.CssClass = "shadow editPanel";
		}

		int r;
		int g;
		int b;
		r = 208 + L;
		if (r > 255)
			r = 255;
		g = 224 + L;
		if (g > 255)
			g = 255;
		b = 160 + L;
		if (b > 255)
			b = 255;

		// BuildPanel.Style("background-color") = System.Drawing.ColorTranslator.ToHtml(Drawing.Color.FromArgb(r, g, b)) '"#d0e0A0"
		// BuildPanel.Style("z-index") = CStr(40 + Val(Request("depth")))

		//BuildPanel.Style("position") = "relative"
		//If Request("popup") <> "" Then
		// BuildPanel.Style("width") = "60%"
		// Else
		// BuildPanel.Style("width") = "95%"
		// End If

		// And IsDictionary(obj) Then 'no headers requires in 'expanded' (page)  mode
		if (!expanded) {

			BuildPanel.Controls.Add(closeButton(path, divID, buyerAccount.Language));
			BuildPanel.Controls.Add(EditScreenButton(screen, path));
			//Adds the 'edit & delete screen' button
			BuildPanel.Controls.Add(Title(Split(path, ".").Last, screen.title));

			BuildPanel.Controls.Add(editheader.UI(buyerAccount, agentAccount.Language, errorMessages, BuildPanel));
		}

		//editing (ie saving) any object that is 'auditable' creates a new version of that object
		//the history button expands the object to reveal all versions of it.. which under normal circumstances would be locked for editing.
		//any auditable object must have a ownName_root, and a current property

		bool isLastPage = false;
		int written = 0;
		int rownum = 0;

		object row;

		if (Reflection.IsDictionary(obj)) {
			//list edit (a dictionary of objects)

			if (editheader.PerPage == 0)
				editheader.PerPage = 50;

			if (editheader.VW.Count == 0) {
				BuildPanel.Controls.Add(ErrorDymo("There are no records to display", lid));
			} else {
				for (i = editheader.Fromindex; i <= editheader.Fromindex + editheader.PerPage; i++) {
					if (i >= editheader.VW.Count){isLastPage = true;break; // TODO: might not be correct. Was : Exit For
}
					//needed for the last page (which we may not have enough rows to fill)

					//we have to check becuase it may have been deleted (but still be in the view)
					// If obj.containskey(editheader.VW(i)("id")) Then
					int id = editheader.VW(i)("ID");
					if (obj.containskey(id)) {
						row = obj(id);
						//obj is the dictionary - view contains ordered/filtered rows which carry an ID (which we use as the index for the OBJ dictionary)
						rownum += 1;

						BuildPanel.Controls.Add(EditObj(screen, row, expanded, BuildPanel, true, language, buyerAccount, rownum, path + "(" + row.id + ")", false,
						errorMessages, editheader.translationLanguage));
						OutputErrors(BuildPanel.Controls, errorMessages, lid, true);
					}
				}
			}
		} else {
			errorMessages = new List<string>();
			BuildPanel.Controls.Add(EditObj(screen, obj, expanded, BuildPanel, true, language, buyerAccount, 0, path, true,
			errorMessages, translationLang));
			OutputErrors(BuildPanel.Controls, errorMessages, lid, true);

			//   End If
		}

		//make a panel directly under the exisiting rows - in which to add the new one (above the paging buttons)
		Panel pnlAdd = new Panel();
		BuildPanel.Controls.Add(pnlAdd);
		if (cmd != "collapse") {
			BuildPanel.Controls.Add(PageButtons(path, divID, editheader.Fromindex, (editheader.Fromindex == 0), isLastPage, editheader.PerPage));

			if (!editheader.VW != null) {
				if (editheader.DT != null) {
					Label lblcount;
					lblcount = new Label();
					lblcount.Text = editheader.VW.Count + " of " + editheader.DT.Rows.Count + " unfiltered rows";
					BuildPanel.Controls.Add(lblcount);
				}
			}

			if ((translationLang.ID > 1)) {
				BuildPanel.Controls.Add(MakeButton(false, "Save", Xlt("Save changes", buyerAccount.Language), EmbedScript(path, divID, "saveTranslate_" + translationLang.ID, false, true)));
			} else {
				BuildPanel.Controls.Add(MakeButton(false, "Save", Xlt("Save changes", buyerAccount.Language), EmbedScript(path, divID, "", false, true)));

			}

			//in list mode - display an add button

			BuildPanel.Controls.Add(MakeButton(false, "Add", Xlt("Add a new ", buyerAccount.Language) + screen.title, EmbedScript(path, divID, "add", false, true)));
		} else {
			//BuildPanel = New Panel
			//BuildPanel.ID = divID
			//Dim row As Object
			//Dim id As Integer = editheader.VW(0)("ID")
			//row = obj(id)
			//Return EditObj(screen, row, expanded, BuildPanel, True, language, buyerAccount, 0, path$ & "(" & row.id & ")", False, errorMessages, editheader.translationLanguage)
		}


	}
	//Private Function Title(screen As clsScreen) As Label
	private new Panel Title(string titleText, string subtitle)
	{

		Title = new Panel();
		Label lbl;
		lbl = new Label();
		lbl.Text = titleText + "&nbsp;";
		//Request("Title") & " " & screen.title
		lbl.Font.Size = 15;
		lbl.Font.Bold = true;
		Title.Controls.Add(lbl);

		Label stlabel = new Label();
		stlabel.Text = subtitle;
		stlabel.Font.Size = 11;
		Title.Controls.Add(stlabel);

	}

	private Panel EditScreenButton(clsScreen screen, path__1)
	{

		//edit screen button

		Panel pnl = new Panel();
		pnl.ID = "Screens(" + screen.ID + ")";
		Literal lit = new Literal();
		lit.Text = "&nbsp;";
		//you must put *something* in the panel or it's not rendedred (and then the javascript can't access it !)
		pnl.Controls.Add(lit);

		pnl.Controls.Add(MakeButton(false, "Es", "Edit the title, layout, labeling, visible columns and and help for this screen.", EmbedScript("Screens(" + screen.ID + ")", pnl.ID, "", false, true)));

		pnl.Controls.Add(MakeButton(false, "Rs", "Regenerate this screen layout (based on the underlying object - all help, validation, layout and labeling will be lost).", EmbedScript(Path, pnl.ID, "deleteScreen," + screen.ID, false, false)));

		return pnl;

	}

	public static Panel MakeButton(bool compact, string txt, string tooltip, string script)
	{

		MakeButton = new Panel();
		MakeButton.CssClass = "editorButton ";
		if (compact)
			MakeButton.CssClass += " compactButton";
		Label lbl = new Label();
		lbl.Text = txt;
		lbl.ToolTip = tooltip;
		MakeButton.Controls.Add(lbl);
		MakeButton.Attributes("onclick") = script;

	}
	private Panel closeButton(path, string divid, clsLanguage language)
	{

		closeButton = MakeButton(false, "✖", Xlt("Close (and save)", language), EmbedScript(path, divid, "close", false, true));
		//closeButton.Attributes("style") = "position:absolute;top:8px;right:8px;"

	}


	private void SaveChanges(clsScreen screen, string path, object PathedObject, clsAccount agentaccount, ref List<string> errormessages)
	{
		//The request (from embed() is POSTED - so the data is in the FORM variables (not the request.querystring) 
		//The variable names tell us which property, on which object they carry a value for
		//rk$ = "c" & col.ID & "_" & Row.id 'dk - the 'col.id' portion is the property, the row id is the id of the obejct in the dictionary

		//collect all the updates together by object, so we only need .update() each altered object once (and can also only create 1 additional entry for auditable objects)
		//make a dictionary of ObjectID to update > fieldID,value pair
		//many rows can be updated in a single 'save' operation - but only within the same dictionary
		Dictionary<int, Dictionary<int, string>> UpDic;
		UpDic = new Dictionary<int, Dictionary<int, string>>();

		// 'Request.Form.Keys 'was request.form.keys BUT DOESNT WORK !!!
		foreach (string k in Request.QueryString.Keys) {

			// Debug.Print(k)

			if ((k.StartsWith("c_") | k.StartsWith("cb_")) & InStr(3, k, "_") > 0) {
				//rk$ = "c" & col.ID & "_" & Row.id 'dk

				int objId;
				string[] bits;
				int FieldID;

				bits = Split(k, "_");
				if (bits.Length != 3) {
					errormessages.Add("Expected 3 Bits - got " + bits.Length);

				} else {
					FieldID = (int)bits(1);
					objId = (int)bits(2);

					if (!UpDic.ContainsKey(objId))
						UpDic.Add(objId, new Dictionary<int, string>());
					UpDic(objId).Add(FieldID, Request(k));
				}
			}
		}

		// PathedObject is *typically* a dictionary (the one at the end of the path) - but can be a single object (for a page update)

		SaveGroupedChanges(screen, path, PathedObject, UpDic, agentaccount, false, errormessages);

		// Next

	}


	private void SaveGroupedChanges(clsScreen screen, string path, object obj, Dictionary<int, Dictionary<int, string>> dicUpdates, clsAccount agentaccount, bool auditable, ref List<string> errorMessages)
	{
		//obj is the object we're updating 

		object target = null;
		object newrow;

		//each 'row' in here has all the updated properties (indexed by fieldID) for single object (or row in a dictionary)
		foreach ( objid in dicUpdates.Keys) {

			target = null;
			if (Reflection.IsDictionary(obj)) {
				if (obj.containskey(objid))
					target = obj(objid);
				//it's possible an object (in dicupdates) has been deleted
			} else {
				target = obj;
			}

			if (target == null) {
				errorMessages.Add("target was nothing ");

			} else {
				if (auditable) {
					newrow = target.clone;
					//creates an exact copy - but with a new ID 
					target.current = false;
					//this one is no longer current
					target.update(errorMessages);
					newrow.timeStamp = Now;
					target = newrow;
					//we'll change the columns (apply the edits) to the new row
				}

				clsField fld;
				//the these keys are the fields in each updated object, 
				foreach ( fieldkey in dicUpdates(objid).Keys) {
					fld = screen.Fields(fieldkey);

					//     Dim oldvalue As String = ParsePath(path$ & "(" & objid & ")" & screen.Fields(fieldkey).propertyName, oldobj, Nothing, errormessgaes)
					// If col.lookupOf<>"" then oldvalue=

					string oldValue = getPropertyString(target, fld.propertyName, agentaccount.Language);
					string newValue__1 = dicUpdates(objid)(fieldkey);

					string pathToProp = path + "(" + objid + ")." + fld.propertyName;


					if (dicUpdates(objid)(fieldkey) == "null") {
						setProperty(target, fld.propertyName, null, null, errorMessages, true);

					} else {
						setPropertyFromString(fld, target, newvalue, agentaccount.Language, errorMessages);
					}
					AuditLog.Instance.Add(AuditType.Editor, string.Format("{0}, Id:{1} updated to {2}", fld.propertyName, target.id, newvalue), "Editor", 0);
					logEdit(agentaccount, "E", pathToProp, oldValue, newValue__1);
				}
				target.update(errorMessages);
				//call the update method on the object we changed to persist the changes 

			}
		}

		//If dicUpdates.Count Then sw.Close()

	}


	private void logEdit(clsAccount agentaccount, string action, string path, string oldValue, string newValue)
	{
		object sql = "INSERT INTO editLog (fk_account_id_agent,action,path,oldvalue,newvalue,timestamp,undone,comments) VALUES (";
		sql += agentaccount.ID + ",'" + action + "'," + da.SqlEncode(path) + "," + da.SqlEncode(oldValue) + "," + da.SqlEncode(newValue) + ",getdate(),null,'')";

		da.DBExecutesql(sql);

	}

	private Panel EditObj(clsScreen screen, object obj, bool expanded, Panel inPanel, bool enabled, clsLanguage language, clsAccount buyerAccount, int Rownum, path, bool HideHistoryButton,
	ref List<string> errormessages, clsLanguage translateLanguage)
	{

		//obj is always a single object (never a dictionary at this point)
		//If page is true = we present as a page, if page is false we present as a single row)
		//all rows have an expand/collapse button which maintains a session variable paths of expanded objects, 
		//(which will be presetted in 'page' mode.. overriding pararmenter - this is basically so we can expand entities in lists

		Panel objPnl;
		//this is what we Return
		objPnl = new Panel();

		//Populate the Quote counts on Account objects - 'just in time'
		Type act;
		act = System.Type.GetType("IQ.clsAccount");
		if (object.ReferenceEquals(obj.GetType, act))
			obj.user.countQuotesPerAccount();

		Guid g = new Guid();
		Panel clear;

		List<string> DOTs__1 = DescendantObjectTypes(obj, screen, errormessages);

		if (expanded) {
			//Dim lit As Literal = New Literal
			//lit.Text = "<br/>"
			//objPnl.Controls.Add(lit)
			objPnl = LayoutAsPage(path, obj, screen, true, language, buyerAccount, dots, errormessages, translateLanguage);
			//returns the names on any descendant objects (so we know why the delete button is disabled)

		} else {
			objPnl = LayoutAsRow(path, obj, screen, true, language, buyerAccount, DOTs__1, errormessages, translateLanguage);
		}

		clear = new Panel();
		clear.Style.Add("clear", "both");
		objPnl.Controls.Add(clear);

		return objPnl;

	}
	private PlaceHolder ObjButtons(Panel inPanel, object obj, string path, List<string> DOTs, bool expanded)
	{


		PlaceHolder ph = new PlaceHolder();

		if (DOTs.Count > 0) {
			// ml for translations obj.id is a dictionatry
			if (object.ReferenceEquals(obj.id.GetType(), typeof(Dictionary<clsLanguage, Int32>))) {
				ph.Controls.Add(MakeButton(true, "∪", "Delete - You must delete the descendant " + Join(DOTs.ToArray, ",") + " first", EmbedScript(path, inPanel.ID, "del," + (Dictionary<clsLanguage, Int32>)obj.id(English), false, false)));
				//don't save the object we're deleting
			} else {
				ph.Controls.Add(MakeButton(true, "∪", "Delete - You must delete the descendant " + Join(DOTs.ToArray, ",") + " first", EmbedScript(path, inPanel.ID, "del," + obj.id, false, false)));
				//don't save the object we're deleting
			}

		} else {
			ph.Controls.Add(MakeButton(true, "∪", "Delete- You must delete", EmbedScript(path, inPanel.ID, "del," + obj.id, false, false)));
			//don't save the object we're deleting
		}


		// ml for translations obj.id is a dictionatry
		if (object.ReferenceEquals(obj.id.GetType(), typeof(Dictionary<clsLanguage, Int32>))) {
			ph.Controls.Add(MakeButton(true, "∬", "Copy this object", EmbedScript(path, inPanel.ID, "copy," + (Dictionary<clsLanguage, Int32>)obj.id(English), false, true)));
		} else {
			ph.Controls.Add(MakeButton(true, "∬", "Copy this object", EmbedScript(path, inPanel.ID, "copy," + obj.id, false, true)));
		}



		if (expanded) {
			ph.Controls.Add(MakeButton(false, "-", "Collapse to a row", EmbedScript(path, inPanel.ID, "collapse", false, true)));
		} else {
			ph.Controls.Add(MakeButton(false, "+", "Expand to a page", EmbedScript(path, inPanel.ID, "expand", false, true)));
		}


		//If screen.Auditable Then
		//If obj.current Then
		//    Dim btnhist As Button
		//    btnhist = New Button

		//    If Not HideHistoryButton Then
		//        btnhist.Text = "Show History"
		//        btnhist.OnClientClick = EmbedScript(path, "Hist," & obj.auditroot.id, False, True)
		//    End If

		//    btnhist.ToolTip = btnhist.OnClientClick
		//    objPnl.Controls.Add(btnhist)

		//    'only the current row has a delete button (not historical ones)
		//    objPnl.Controls.Add(btndelete)
		//End If
		//Else
		//       ph.Controls.Add(btndelete)
		//        End If

		return ph;

	}

	private Panel LayoutAsPage(string path, object obj, clsScreen screen, bool enabled, clsLanguage language, clsAccount buyerAccount, List<string> DOTs, ref List<string> errorMessages, clsLanguage translateLanguage)
	{

		//Displays the object for editing as a page within Pnl, using the template screen

		//returns a list of an descendant object types (to become tooltip text on the dsiabled delete button)
		LayoutAsPage = new Panel();

		Literal lit;

		UnitType em;
		em = UnitType.Em;
		//Percentage

		//Dim pnl As Panel = EmptyPanel("v." & obj.id)
		Panel pnl = EmptyPanel(path);
		//"v." & obj.id)
		//pnl.ID = "row." & obj.id & "." ') 'New Panel
		// pnl.CssClass &= " editRow"

		Panel lblPanel;
		Label lbl;
		Panel col;

		//use LINQ to order by order
		object fields = (from f in screen.Fields.Valuesorderby f.order);

		object script = "";
		Panel row;

		// screen.Fields.Values
		foreach ( f in fields) {

			row = new Panel();
			pnl.Controls.Add(row);

			Panel subpanel = EmptyPanel("pg.sub." + f.propertyName);
			pnl.Controls.Add(subpanel);

			lblPanel = new Panel();
			//Leftmost panel carries the field labels
			 // ERROR: Not supported in C#: WithStatement


			lbl = new Label();
			lbl.Text = f.labelText.text(language);
			lbl.Font.Bold = true;
			lblPanel.Controls.Add(lbl);
			row.Controls.Add(lblPanel);

			col = new Panel();
			//The central panel which carriues the acutal UI
			row.Controls.Add(col);
			col.Style("float") = "left";
			col.Style("margin-bottom") = ".75em";

			//OBSOLTE - Used to provide alist of dependent objects (to determin if it can be deleted) If f.InputType.code = "many" Then
			//    Dim adic As Object
			//    adic = WalkPropertyValue(obj, f.propertyName, errorMessages)
			//    If Not adic Is Nothing Then
			//        If WalkPropertyValue(obj, f.propertyName, errorMessages).Values.Count > 0 Then
			//            LayoutAsPage.Add(f.propertyName) 'yes, we have descendant (one of our 'many' properties - has objects in it
			//        End If
			//    End If
			//End If

			if (f.visiblePage) {
				col = f.EditUI(obj, path, subpanel, enabled, Request, language, buyerAccount, true, errorMessages, translateLanguage);
				//adds the main UI element (dropdown, textbox, calendar tickbox etc)
				col.Style("display") = "inline-block";
				col.Style("width") = "20em";
				script += f.validationScript;

				col.Style("height") = f.height + "em";
				col.Style("background-color") = "white";

			} else {
				//hidden row (set to 1 em so it can be reinstated via it's show button)
				col = new Panel();
				col.Style("display") = "inline-block";
				col.Style("height") = "1em";
				lit = new Literal();
				lit.Text = "";
				col.Controls.Add(lit);
			}
			row.Controls.Add(col);

			//3rd column, help text
			Panel helpcol;
			helpcol = new Panel();
			helpcol.Style("display") = "inline-block";
			row.Controls.Add(helpcol);

			lbl = new Label();
			lbl.Text = f.helpText;
			lbl.Style("margin-left") = "1em";
			helpcol.Controls.Add(lbl);

		}

		pnl.Controls.Add(ObjButtons(pnl, obj, path, DOTs, true));

		Image img = new Image();
		img.ImageUrl = eim + "resort.png";
		img.Width = 1;
		img.Height = 1;

		img.Attributes.Add("onload", script);
		pnl.Controls.Add(img);

		return pnl;

	}

	private List<string> DescendantObjectTypes(object obj, clsScreen screen, ref List<string> errormessages)
	{

		List<string> DOTs__1 = new List<string>();

		// screen.Fields.Values
		foreach ( f in screen.Fields.Values) {

			if (f.InputType.code == "many") {
				object adic;
				adic = WalkPropertyValue(obj, f.propertyName, errormessages);
				if (!adic == null) {
					if (adic.Values.Count > 0) {
						//            pnl.Add(f.propertyName) 'yes, we have descendant (one of our 'many' properties - has objects in it) - return as a list (so we know what's preventing deletion)
						DOTs__1.Add(f.propertyName);
					}
				}
			}
		}

		return dots;

	}



	private Panel LayoutAsRow(string path, object obj, clsScreen screen, bool enabled, clsLanguage language, clsAccount buyeraccount, List<string> DOTs, ref List<string> errormessages, clsLanguage translateLanguage)
	{
		//Displays the object for editing as a row within Pnl, using the template screen
		//returns a list of an descendant object types (to become the tooltip text on the disabled delete button) e.g. "You can't add an atttribute until you have some products"

		//Dim subpanel As Panel = EmptyPanel("sub.row." & path) ' 'we make this now - becuase we need to add things into it - but it's added to the controls collection later (so it appears below the row)
		//Dim subpanel As Panel = EmptyPanel("sub." & path) ' 'we make this now - becuase we need to add things into it - but it's added to the controls collection later (so it appears below the row)

		//Dim pnl = EmptyPanel("row." & obj.id & ".") 'New Panel
		object pnl = EmptyPanel(path);
		pnl.CssClass += " editRow";
		pnl.Attributes.Add("onmousedown", "burstBubble(event);$(this).toggleClass('highlighted');");
		//The enabled parameter is a bit of a red herring becuase this needs to be done at a field level really (and based on permissions/roles)

		//use LINQ to order by order
		object fields = (from f in screen.Fields.Valuesorderby f.order);

		//Dim col As Panel
		Literal lit = null;



		object script = "";
		pnl.Controls.Add(ObjButtons(pnl, obj, path, DOTs, false));

		Panel underPanel = new Panel();
		// screen.Fields.Values
		foreach ( f in fields) {

			if (f.visibleList) {
				pnl.Controls.Add(f.EditUI(obj, path, underPanel, enabled, Request, language, buyeraccount, false, errormessages, translateLanguage));
				//adds the main UI element (dropdown, textbox, calendar tickbox etc)
			} else {
				pnl.Controls.Add(f.emptyCell(true, true));
			}
		}

		pnl.Controls.Add(ObjButtons(pnl, obj, path, DOTs, false));

		pnl.Controls.Add(underPanel);

		//  pnl.Controls.Add(subpanel) 'This will host the editing of any descendants - we've already passed it to EditUI - but we *add* it here, UNDER the row in question

		return pnl;


	}





	public static string EmbedScript(Path, string TargetDiv, cmd, bool append, bool sendNVPS)
	{

		//Path IS The DIV id (and co-incidentally the object model path)

		//function embed(url, elementID, append, sendNVPs) 
		//DONT put bckslashes in here - they are a JS escape character !!!
		//  EmbedScript = "embed('../editor/editor.aspx?cmd=" & cmd$ & "&path=" & Path$ & "','" & TargetDiv & "'," & LCase(append.ToString) & "," & LCase(sendNVPS.ToString) & ");return false;"
		EmbedScript = "embed('../editor/editor.aspx?cmd=" + cmd + "&path=" + Path + "','" + Path + "'," + LCase(append.ToString) + "," + LCase(sendNVPS.ToString) + ");return false;";
		return EmbedScript;

	}

	//Private Sub deleteRow(b As Button, e As System.EventArgs)

	//    Dim obj As Object = iq
	//    ParsePath(Request("path"), obj, Nothing) 'Populate via reflection, the object we're about to edit (and it's parent)

	//    Dim key As Integer = b.Attributes("key")
	//    obj(key).delete()                                       'call the delete method on the object (for cascading deletes)
	//    obj.remove(key)                                         'remove from the dictionary
	//    'findRecursive(apanel, "row" & key).Visible = False   'remove from the screen

	//End Sub



	private object AddNew(UInt64 lid, clsScreen Screen, object ParentObj, clsEditHeader EditHeader, clsAccount buyeraccount, clsLanguage language, ref List<string> errorMessages)
	{
		//, b As Button, e As System.EventArgs)

		//Create a new instance of whatever it is this screen edits

		//treepath is our current position in the 

		object type = Screen.Obj;

		//              ugly()
		System.Type ty;
		ty = System.Type.GetType("IQ." + type);

		object Instance = Activator.CreateInstance(ty);
		//calls the parameterless constructor - making a 'temporary' object on which we can set default values

		if (Instance == null) {
			errorMessages.Add("could not instance IQ." + type + "(activiator.createInstace returned nothing)");

		} else {
			object em = "";

			//Set defaults - including and parent-child relationships for recursive objects
			SetDefaults(lid, Instance, ParentObj, Screen, ty, errorMessages);
			//This is very important we must populate the correct parent object

			if (em == "") {
				Instance = Instance.insert(errorMessages);
				//All generic edtor editable objects must expose an insert method -  their parmeterized constructor is called - adding them to their parents dictionary

				if (Screen.Auditable) {
					if (Instance.auditroot == null){errorMessages.Add("Auditroot was nothing");return null;}
				}

				//We must now add a row to the datatable/dataview - in the edit header - which is what is providing our filtering and sorting
				EditHeader.addRow(lid, Instance, Screen, buyeraccount, language, errorMessages);
				EditHeader.Fromindex = EditHeader.VW.Count - 10;
				//move to the End of the view
				if (EditHeader.Fromindex < 0)
					EditHeader.Fromindex = 0;
				//these were 1 - (BUG: views are 0 based)
				if (EditHeader.RowIndex(Instance.id) == -1) {
					errorMessages.Add("The row you just added is not visible becuase of the filters in effect");
				}

				return Instance;
			} else {
				return null;
			}
		}

	}

	private void SetDefaults(UInt64 lid, ref object Instance, object parentObj, clsScreen screen, Type ty, ref List<string> errorMessages)
	{
		clsLanguage language = ((clsAccount)iq.sesh(lid, "AgentAccount")).Language;
		object defaultvalue;

		Type pType = parentObj.GetType;


		//IMPORTANT !! - the cssClass 'input' is what tags fields for value tracking/saving - DONT'T change/remove it


		foreach ( f in screen.Fields.Values) {
			if (f.InputType.code == "nullstring") {
				nullableString nullstring = new nullableString();
				Reflection.setProperty(Instance, f.propertyName, nullstring, null, errorMessages, false);
			} else if (f.InputType.code == "nullint") {
				NullableInt nullint = new NullableInt();
				Reflection.setProperty(Instance, f.propertyName, nullint, null, errorMessages, false);
			} else if (f.InputType.code == "translate") {
				clsTranslation translation = iq.AddTranslation("Edit me", language, "DM", true, null, 0, false);
				Reflection.setProperty(Instance, f.propertyName, translation, null, errorMessages, false);
			} else if (f.InputType.code == "string") {
				Reflection.setProperty(Instance, f.propertyName, "", null, errorMessages, false);
				//will be overriden by any explicit default
			} else {
				//And dictionary properites are intialised in the individual parameterless constructors/Insert Methods
			}

			if (f.defaultValue != "") {
				defaultvalue = f.defaultValue;

				if (LCase(f.defaultValue).Contains("[parent]")) {
					//   'eg, 'fields','screen' property
					defaultvalue = parentObj;
				} else if (LCase(f.defaultValue).Contains("[seller]")) {
					//   'eg, 'fields','screen' property
					defaultvalue = ((clsAccount)iq.sesh(lid, "BuyerAccount")).SellerChannel;

				//we don't need to add this object to the parents dictionary becuase..once we set the parent, and subsequently call Insert, the parmaterized construcot is called - which adds the object to it's parents children

				} else if (LCase(f.defaultValue).Contains("[treepath]")) {
					defaultvalue = iq.sesh(lid, "treepath");

				} else if (LCase(f.defaultValue).Contains("[tree]")) {
					if (object.ReferenceEquals(parentObj.GetType, ty)) {
						defaultvalue = parentObj;
						// we're 'into' the tree now - set this objects parent
					} else {
						defaultvalue = null;
						//this is a top level object in the tree
					}
				} else if (LCase(f.defaultValue) == "[now]") {
					defaultvalue = Now;
				} else if (LCase(f.defaultValue) == "[nothing]") {
					defaultvalue = null;
				} else {
					//    Stop
				}

				Reflection.setProperty(Instance, f.propertyName, defaultvalue, null, errorMessages, false);
				//implement '[parent].id type defaults

			}

			//This field holds a reference to a dictionary (a foriegn key) - it's stored as an integer
			if (f.InputType.code == "one") {

				//If we have set some explict defailt already ( perhaps [Parent] or [tree] then DON'T override this by just picking the first
				if (f.defaultValue == "") {

					if (object.ReferenceEquals(System.Type.GetType("IQ." + f.propertyName), pType)) {
						//this field is of the same type at the parent obejct - this is (almost certainly) a back-reference)
						System.Diagnostics.Debugger.Break();
					}

					//exmine the 'field.lookupof'
					//some drop down lists (and therefore defaults) are FILTERED 
					//for example, threads.state(group=threads)

					//find the root level dictionary this field looks up values in
					object lu;
					lu = f.LooksUp;
					//instance was IQ
					ops = Reflection.WalkPropertyValue(iq, lu, errorMessages);
					//we now fetch from the INSTANCEs dictionary - not the root level dictionaries

					//filter that dictionary by any name value pair (specified in parentesis after the .lookup)
					object filterPVP = GetParenthesisValue(f.lookupOf);
					// Gets any filter Property Value Pair e.g.  "group=TH"

					object defaultTo = Findmatch(ops, filterPVP, errorMessages);
					// returns the first match - we only need 1 entry
					//Dim defaultTo As Object
					//                    defaultTo = ops.values(0)

					if (defaultTo == null) {
						if (f.defaultValue != "[tree]") {
							errorMessages.Add(" You can't add a " + screen.Obj + " until you have some " + f.LooksUp);
						}
					} else {
						//get the first value (as the defualt)
						Reflection.setProperty(Instance, f.propertyName, defaultTo, null, errorMessages, false);
						//implement '[parent].id type defaults                            
					}
				}
			}


			//For auditable items... set the _root element to point to itself... Only the root item is 'ADDed'.. subsequent edits create copies (but they don't comne through here)
			// If LCase(f.propertyName) = "auditroot" Then
			//Reflection.setProperty(instance, f.propertyName, instance, Nothing)
			//End If
		}

	}

	private Control findRecursive(Control c, string toFind)
	{

		findRecursive = null;
		if (c.ID == toFind)
			return c;

		Control result;
		foreach ( child in c.Controls) {
			result = findRecursive(child, toFind);
			if (!result == null) {
				return result;
			}

		}

	}


}

