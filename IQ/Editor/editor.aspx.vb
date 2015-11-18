'Option Strict On
Option Explicit On

Imports System.Linq.Expressions
Imports System.Reflection
Imports System.IO
Imports dataAccess

Public Class editor
    Inherits System.Web.UI.Page

    Public Enum EnumViewType
        pageView = 1
        listView = 2
    End Enum

    Dim Sort$
    Dim Filter$

    Public ops As Object 'dictionary of integer,something
    Private lid As UInt64
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'Each (and every!) (ajax) call to editor.aspx generates a panel.. 
        'The path tells us what we're editing - and that content is placed into it's parent (the previous path segment)

        'This page edits either a Dictionary or an Object , ultimately belonging to the IQ object
        'it takes a querystring of the form..
        'editor.aspx?path= Channels(1).Users(5)
        'or
        'editor.aspx?path=Channels(1).Users
        'or 
        'products(1273).ProductAttributes(3892)
        'or
        'Channels(47).children(839).children(3940).users
        'The indices in all the above are ID's (keys in dictionaries)

        'The end item in the path is loaded into OBJ for editing - if it's a dictionary you'll get a list editing view,
        'if it's an object - you'll get a page editor

        'The information for which properties of the object(s) should be displayed, how, with what help, validation and default values - comes from a template [SCREEN] and a set of [FIELD]s relating to that screen
        'Those templates are initially generated via MakeScreen().. and can then be maintained using the generic editor itself.
        'Each screen edits one type of Object

        'Editor.aspx also accepts a URL parameter &Default - which contains as slash ('/') delimited list of name, value pairs to set defaults on an added object
        Dim j = Request.RawUrl

        Dim lid As UInt64 = 0

        If Not UInt64.TryParse(Request.QueryString("lid"), lid) Then Response.Redirect("/aspx/signin.aspx")

        Dim Obj As Object = iq 'Nothing              'this is the object we're editing - which *may* be a dictionary
        Dim ParentObj As Object = Nothing   'this is it's parent - used to set some defaults especially in recursive objects (eg, channels, threads)
        Dim screen As clsScreen = Nothing
        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)


        If agentAccount Is Nothing Then Response.Redirect("/aspx/signin.aspx")
        Dim language As clsLanguage = agentAccount.Language
        Dim cmd$ = Request("cmd")
        Dim translationlanguage As clsLanguage = New clsLanguage()
        ' This is very significant !
        Dim path$ = Request.QueryString("path")  'this INITIALLY the path passed in the GET URL - but subsequently that POSTED passed in the FORM (from embed()'s rexec )
        If Request.Form("path") IsNot Nothing Then path = Request.Form("path")
        If Request.QueryString("language") IsNot Nothing Then
            translationlanguage = iq.Languages(Request.QueryString("language"))
        End If


        Dim errorMessages As List(Of String) = New List(Of String)

        path$ = replaceSessionVariableTags(path$, lid, errorMessages)  'Things like UserID can be embedded in [brackets] and will be replaced with session variables

        'walk' the path to find the obejct and its parent (either of which *may* be dictionaries)
        ParsePath(path$, Obj, ParentObj, errorMessages)


        Dim MyPanel As Panel = New Panel

        If Obj Is Nothing Then
            errorMessages.Add("Path: " & path$ & " evaluates to nothing")
        Else

            'Find out what kind of screen to use to edit this kind of object
            screen = fetchScreenTemplate(Obj, ParentObj, errorMessages)

            'save any changes  (posted in the NVPs from Embed/rexec) - really need to see if this affects the view (filtering sorting)
            If cmd IsNot Nothing AndAlso cmd.Contains("saveTranslate") Then
                Dim arrayLang() As String = Split(cmd, "_")
                Dim saveLanguage As clsLanguage = iq.Languages(arrayLang(1))
                SaveChanges(screen, path, Obj, agentAccount, errorMessages)
                cmd = ""
            Else
                SaveChanges(screen, path, Obj, agentAccount, errorMessages)
            End If


            'The edit headers hold the state information for this list (in the editor) 
            'including Sort orders and Priorities, Column widths, Filtes, pagination info
            'They are similar to (& might ultimately get consolidated with) MatrixHeaders
            'The are indexed by the path to the dictionary they head - for example Products(3042).ProductAttributes

            'Dim divid As String = Request("DivId")

            Dim editheader As clsEditHeader = Nothing
            'If Reflection.IsDictionary(Obj) Then
            Dim editHeaders As Dictionary(Of String, clsEditHeader)
            editHeaders = iq.sesh(lid, "editHeaders")

            If editHeaders.ContainsKey(path) Then
                editheader = editHeaders(path)
            Else
                editheader = New clsEditHeader(path, Obj, screen, buyerAccount, agentAccount.Language, errorMessages, lid)
                editHeaders.Add(path, editheader)
            End If

            Dim expanded As Boolean = False 'Is this object pivoted into 'page' mode

            If ProcessCommand(lid, buyerAccount, language, screen, path$, Obj, ParentObj, cmd, editheader, errorMessages) Then
                OutputErrors(holder.Controls, errorMessages, lid, True)

                're-fetch the screen template - as processing the command may have altered it (eg. deletescreen)
                screen = fetchScreenTemplate(Obj, ParentObj, errorMessages)

                'Dim showheaders As Boolean = True  'Lists need headers


                If Reflection.IsDictionary(Obj) = False Then
                    If iq.SeshContains(lid, "expanded") AndAlso iq.sesh(lid, "expanded").contains(path) Then expanded = True
                End If
            End If

            'we're building a panel to edit OBJ - which might be a dictionary - or a single row (accoring to Path)
            'we *always* build a panel - even if it's closed

            If Split(cmd, ",")(0) <> "del" Then
                '  mypanel = BuildPanel(lid, editheader, path, Obj, divid, screen, expanded, errorMessages, cmd, editheader.translationLanguage)
                mypanel = BuildPanel(lid, editheader, path, Obj, path, screen, expanded, errorMessages, cmd, editheader.translationLanguage)
            Else
                Dim dl As Label = New Label : dl.Text = "deleted" : mypanel.Controls.Add(dl)
            End If
        End If

        holder.Controls.Add(MyPanel)
        holder.Controls.Add(editor.MakeButton(True, "Change Log", "View/undo recent changes made with the editor)", "window.open('/editor/editlog.aspx');"))

        OutputErrors(holder.Controls, errorMessages, lid, True)

    End Sub

    Private Function fetchScreenTemplate(obj As Object, parentobj As Object, ByRef errormessages As List(Of String)) As clsScreen

        'The 'right' screen to edit a given type of object - is determined from a compound of the parentOb's and Obj's 'type'
        'It then finds a screen with a TITLE clsParent.clsObj - for example clsChannel.clsAccount edits a channels buyerAccounts

        ' If Reflection.IsDictionary(obj) Then Stop
        If Reflection.IsDictionary(parentobj) Then Stop

        ' Dim isDictionary As Boolean = False
        ' isDictionary = Reflection.IsDictionary(obj)

        Dim pty As Type = Reflection.TypeOfDicOrObj(parentobj)
        Dim ty As Type = Reflection.TypeOfDicOrObj(obj)

        'Make a new screen template 'Just in time'
        Dim ck$ = pty.Name & "." & ty.Name
        If Not iq.i_screens_title.ContainsKey(ck$) Then
            'channel.buyeraccounts
            'user.accounts
            'clsChannel.clsAccount (buyeraccounts)
            'clsaccount.clsuser
            'clsUser.clsAccount (users accounts)

            If Not System.Type.GetType("IQ." & ty.Name, True, True) Is Nothing Then
                Dim code$
                code = ty.Name
                If Left(code, 3).ToLower = "cls" Then code = Mid(code, 4)
                MakeScreen(ck$, code, ty, parentobj.GetType, errormessages)
            Else
                errormessages.Add("Unrecognised type:IQ." & ty.Name)
                'unrecognised type
                ' Stop
            End If
        End If

        fetchScreenTemplate = iq.i_screens_title(ck$) ' Then  'which screen we use is based on the type of object we're going to edit

    End Function

    Private Function ProcessCommand(lid As UInt64, buyeraccount As clsAccount, language As clsLanguage, screen As clsScreen, path$, Obj As Object, ParentObj As Object, cmd As String, ByRef editHeader As clsEditHeader, ByRef errorMessages As List(Of String)) As Boolean

        'returns false is thhere is no requirement to render the panel - eg. close was pressed

        ProcessCommand = True

        If ParentObj Is Nothing Then ParentObj = Obj ' WHAT - WHY ? (pointing unparented threads to themselves - maybe?)

        Dim parts() = Split(cmd, ",")

        If InStr(cmd, "=") > 0 Then errorMessages.Add("Cmd should not contain = use , ")

        Select Case parts(0).ToLower

            Case Is = "widen", "narrow", "promote", "demote", "show", "hide"
                Dim fld As clsField
                fld = iq.Fields(CInt(parts(1)))
                editHeader.changeLayout(parts(0), fld, errorMessages)

            Case Is = "add"

                AddNew(lid, screen, ParentObj, editHeader, buyeraccount, language, errorMessages)

            Case Is = "deletescreen"

                Dim pty As Type = Reflection.TypeOfDicOrObj(ParentObj)
                Dim ty As Type = Reflection.TypeOfDicOrObj(Obj)

                'deindex it
                Dim ck$ = pty.Name & "." & ty.Name
                iq.i_screens_title.Remove(ck$)

                DeleteScreen(CInt(parts(1)), errorMessages) 'Deletes the current screen layout (such that it will be remade from the underlying sturcture)
                '  ProcessCommand = False 'closes the panel (as the old screen def is now invalid)

            Case Is = "del"  'the parameter on the del command is the object ID on dictionaries
                If Reflection.IsDictionary(Obj) Then
                    Dim id As Integer = CInt(parts(1))
                    If Obj.containskey(id) Then
                        Obj(id).delete()  'calls the delete method on the object to remove it from the database
                        Obj.remove(id) 'removed the object from its
                        Dim r As DataRow = editHeader.DT.Rows.Find(id)
                        r.Delete()
                    End If
                Else
                    If editHeader.DT IsNot Nothing Then
                        Dim r As DataRow = editHeader.DT.Rows.Find(Obj.ID)
                        r.Delete()
                    End If
                    Obj.delete(errorMessages)
                End If

            Case Is = "copy"
                'Dim ascreen As clsScreen = iq.Screens(Request("copyscreen")).copy
                'Dim theobj As Object = Obj(CInt(parts(1)))
                'theobj.copy()  'the object in question must expose a copy method (screens do !)
                Obj.copy()

            Case Is = "priority"
                Stop
                'see sort
                'editHeader.UpdateSorts(New clsPriorityDirection(parts(1)))

            Case Is = "changefilter"

                editHeader.UpdateFilters(parts(1), parts(2), parts(3))
                editHeader.addMissingColumns(Obj, buyeraccount, language, New HashSet(Of String), errorMessages, lid)
                'anything we're (now) sorting or filter by needs to be added to the dataview (from the dictionary)

            Case Is = "removefilter"

                editHeader.RemoveFilter(parts(1))

            Case Is = "from"
                editHeader.Fromindex = parts(1) 'Pagination

            Case Is = "hist"
                errorMessages.Add("Audit Trail/History is not currently supported")

            Case Is = "close"

                ProcessCommand = False 'don't build the panel
            Case Is = "expand"
                If Not iq.SeshContains(lid, "expanded") Then
                    iq.sesh(lid, "expanded") = New List(Of String)
                End If

                If path <> "" Then iq.sesh(lid, "expanded").add(path)

            Case Is = "collapse"
                If iq.sesh(lid, "expanded").contains(path) Then
                    iq.sesh(lid, "expanded").remove(path)
                End If

            Case Is = "language"
                Dim languageValue As String = Request("value")

                Dim translationlanguage As clsLanguage = New clsLanguage()
                translationlanguage = iq.Languages(Request.QueryString("value"))
                editHeader.translationLanguage = translationlanguage
            Case Else

                If cmd <> "" Then
                    errorMessages.Add("Unrecognised cmd parameter '" & cmd & "'")
                End If

        End Select

    End Function

    Private Function InLineAdd(lid As UInt64, parentobj As Object, prop As String, buyerAccount As clsAccount, language As clsLanguage, editHeader As clsEditHeader, ByRef errorMessages As List(Of String)) As Object

        'they've pressed the 'inline add' to add an object to a picklist
        'we create the last object on the path - and insert a panel to edit it
        Dim NewObj As Object
        Dim errorMessage As String = ""
        Dim typename$

        typename$ = Reflection.TypeOfProperty(parentobj, prop)

        Dim ty As System.Type
        ty = System.Type.GetType("IQ." & typename$)

        'Make a new screen template 'Just in time'
        If Not iq.i_screens_title.ContainsKey(typename.ToString) Then
            MakeScreen(Request("Path"), Left(ty.ToString, 5), ty, Nothing, errorMessages)
        End If

        Dim screen As clsScreen

        screen = iq.i_screens_title(typename$)  'which screen we use is based on the type of object we're going to edit

        NewObj = AddNew(lid, screen, parentobj, editHeader, buyerAccount, language, errorMessages)

        Return NewObj

    End Function


    Private Function PageButtons(Path$, divid As String, CurrentFromIndex As Integer, isFirstPage As Boolean, isLastPage As Boolean, perPage As Integer) As PlaceHolder

        Dim ph As PlaceHolder = New PlaceHolder

        If Not isFirstPage Then
            ph.Controls.Add(MakeButton(False, "◄", "Previous page", EmbedScript(Path$, divid, "From," & CStr(CurrentFromIndex - perPage), False, True)))
        End If

        If Not isLastPage Then
            ph.Controls.Add(MakeButton(False, "►", "Next page", EmbedScript(Path$, divid, "From," & CStr(CurrentFromIndex + perPage), False, True)))

        End If

        Return ph

    End Function


    Private Sub ExpandHistory(lid As UInt64, path$, screen As clsScreen, pnl As Panel, obj As Object, hp As String, language As clsLanguage, buyerAccount As clsAccount, translateLanguage As clsLanguage)

        'show only those members whos ...root matches (which will include the original) - but NOT the 'current' (which is the parent)
        Dim em As List(Of String) = New List(Of String)
        For Each row In obj.values
            If Reflection.WalkPropertyValue(row, "AuditRoot", em).id = hp Then
                pnl.Controls.Add(EditObj(screen, row, False, pnl, (obj.current), language, buyerAccount, True, path, False, em, translateLanguage))
            End If
        Next

    End Sub

    Private Function replaceSessionVariableTags(ByVal l$, lid As UInt64, ByRef errorMessages As List(Of String)) As String

        Dim o As Integer
        Dim c As Integer = 1
        Dim tags As List(Of String) = New List(Of String)

        Do
            o = InStr(c, l$, "[")
            If o = 0 Then Exit Do 'no more open's
            c = InStr(o + 1, l$, "]")
            If c = 0 Then
                errorMessages.Add(l$ & " - tag was unclosed")
                Exit Do 'tag was unclosed
            End If

            tags.Add(Mid$(l$, o + 1, c - o - 1))
        Loop

        For Each t In tags
            If iq.sesh(lid, t) Is Nothing Then
                errorMessages.Add("session variable " & lid & "(" & t & ") was nothing ")
            Else
                l$ = Replace(l$, "[" & t & "]", iq.sesh(lid, t))
            End If
        Next

        Return l$

    End Function

    ''' <summary>
    ''' 'The editor(.aspx) works by nesting many instances of itself to edit the descendant properties
    ''' </summary>
    ''' <param name="lid">Login id (key to session)</param>
    ''' <param name="path">Path in the object model to the thing (dictionary or object) to edit</param>
    ''' <param name="obj"></param>
    ''' <param name="screen"></param>
    ''' <returns></returns>
    ''' <remarks>Form_load (which is typically called via ajax).. processes any 'command' then builds a panel which is embeded</remarks>
    Private Function BuildPanel(lid As UInt64, editheader As clsEditHeader, path$, obj As Object, divID As String, screen As clsScreen, expanded As Boolean, ByRef errorMessages As List(Of String), cmd As String, translationLang As clsLanguage) As Panel
        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim language As clsLanguage = agentAccount.Language

        BuildPanel = New Panel
        BuildPanel.ID = divID

        '  Dim editheader As clsEditHeader = Nothing
        '  Dim editheaders As Dictionary(Of String, clsEditHeader)
        '  editheaders = CType(iq.sesh(lid, "editHeaders"), Dictionary(Of String, clsEditHeader))


        'build a hashset from the CD list stored in the sesstion variable
        Dim foci As HashSet(Of String) = New HashSet(Of String)(Split(iq.sesh(lid, "foci"), ",").ToList)


        '   Dim fromIndex As Integer = iq.sesh(lid, "from." & path$) 'Pagination
        '   Dim perpage As Integer = iq.sesh(lid, "perpage." & path$) 'Pagination

        Dim L As Integer
        L = 8 * Val(Request("depth"))

        If cmd <> "collapse" Then
            BuildPanel.CssClass = "shadow editPanel"
        End If

        Dim r, g, b As Integer
        r = 208 + L : If r > 255 Then r = 255
        g = 224 + L : If g > 255 Then g = 255
        b = 160 + L : If b > 255 Then b = 255

        ' BuildPanel.Style("background-color") = System.Drawing.ColorTranslator.ToHtml(Drawing.Color.FromArgb(r, g, b)) '"#d0e0A0"
        ' BuildPanel.Style("z-index") = CStr(40 + Val(Request("depth")))

        'BuildPanel.Style("position") = "relative"
        'If Request("popup") <> "" Then
        ' BuildPanel.Style("width") = "60%"
        ' Else
        ' BuildPanel.Style("width") = "95%"
        ' End If

        If Not expanded Then ' And IsDictionary(obj) Then 'no headers requires in 'expanded' (page)  mode

            BuildPanel.Controls.Add(closeButton(path, divID, buyerAccount.Language))
            BuildPanel.Controls.Add(EditScreenButton(screen, path)) 'Adds the 'edit & delete screen' button
            BuildPanel.Controls.Add(Title(Split(path, ".").Last, screen.title))

            BuildPanel.Controls.Add(editheader.UI(buyerAccount, agentAccount.Language, errorMessages, BuildPanel))
        End If

        'editing (ie saving) any object that is 'auditable' creates a new version of that object
        'the history button expands the object to reveal all versions of it.. which under normal circumstances would be locked for editing.
        'any auditable object must have a ownName_root, and a current property

        Dim isLastPage As Boolean = False
        Dim written As Integer = 0
        Dim rownum As Integer = 0

        Dim row As Object

        If Reflection.IsDictionary(obj) Then
            'list edit (a dictionary of objects)

            If editheader.PerPage = 0 Then editheader.PerPage = 50

            If editheader.VW.Count = 0 Then
                BuildPanel.Controls.Add(ErrorDymo("There are no records to display", lid))
            Else
                For i = editheader.Fromindex To editheader.Fromindex + editheader.PerPage
                    If i >= editheader.VW.Count Then isLastPage = True : Exit For 'needed for the last page (which we may not have enough rows to fill)

                    'we have to check becuase it may have been deleted (but still be in the view)
                    ' If obj.containskey(editheader.VW(i)("id")) Then
                    Dim id As Integer = editheader.VW(i)("ID")
                    If obj.containskey(id) Then
                        row = obj(id)  'obj is the dictionary - view contains ordered/filtered rows which carry an ID (which we use as the index for the OBJ dictionary)
                        rownum += 1

                        BuildPanel.Controls.Add(EditObj(screen, row, expanded, BuildPanel, True, language, buyerAccount, rownum, path$ & "(" & row.id & ")", False, errorMessages, editheader.translationLanguage))
                        OutputErrors(BuildPanel.Controls, errorMessages, lid, True)
                    End If
                Next i
            End If
        Else
            errorMessages = New List(Of String)
            BuildPanel.Controls.Add(EditObj(screen, obj, expanded, BuildPanel, True, language, buyerAccount, 0, path$, True, errorMessages, translationLang))
            OutputErrors(BuildPanel.Controls, errorMessages, lid, True)

            '   End If
        End If

        'make a panel directly under the exisiting rows - in which to add the new one (above the paging buttons)
        Dim pnlAdd As Panel = New Panel
        BuildPanel.Controls.Add(pnlAdd)
        If cmd <> "collapse" Then
            BuildPanel.Controls.Add(PageButtons(path$, divID, editheader.Fromindex, (editheader.Fromindex = 0), isLastPage, editheader.PerPage))

            If Not editheader.VW IsNot Nothing Then
                If editheader.DT IsNot Nothing Then
                    Dim lblcount As Label
                    lblcount = New Label
                    lblcount.Text = editheader.VW.Count & " of " & editheader.DT.Rows.Count & " unfiltered rows"
                    BuildPanel.Controls.Add(lblcount)
                End If
            End If

            If (translationLang.ID > 1) Then
                BuildPanel.Controls.Add(MakeButton(False, "Save", Xlt("Save changes", buyerAccount.Language), EmbedScript(path$, divID, "saveTranslate_" & translationLang.ID, False, True)))
            Else
                BuildPanel.Controls.Add(MakeButton(False, "Save", Xlt("Save changes", buyerAccount.Language), EmbedScript(path$, divID, "", False, True)))

            End If

            'in list mode - display an add button

            BuildPanel.Controls.Add(MakeButton(False, "Add", Xlt("Add a new ", buyerAccount.Language) & screen.title, EmbedScript(path$, divID, "add", False, True)))
        Else
            'BuildPanel = New Panel
            'BuildPanel.ID = divID
            'Dim row As Object
            'Dim id As Integer = editheader.VW(0)("ID")
            'row = obj(id)
            'Return EditObj(screen, row, expanded, BuildPanel, True, language, buyerAccount, 0, path$ & "(" & row.id & ")", False, errorMessages, editheader.translationLanguage)
        End If


    End Function
    'Private Function Title(screen As clsScreen) As Label
    Private Shadows Function Title(titleText As String, subtitle As String) As Panel

        Title = New Panel
        Dim lbl As Label
        lbl = New Label
        lbl.Text = titleText & "&nbsp;" 'Request("Title") & " " & screen.title
        lbl.Font.Size = 15
        lbl.Font.Bold = True
        Title.Controls.Add(lbl)

        Dim stlabel As Label = New Label
        stlabel.Text = subtitle
        stlabel.Font.Size = 11
        Title.Controls.Add(stlabel)

    End Function

    Private Function EditScreenButton(screen As clsScreen, path$) As Panel

        'edit screen button

        Dim pnl As Panel = New Panel
        pnl.ID = "Screens(" & screen.ID & ")"
        Dim lit As Literal = New Literal
        lit.Text = "&nbsp;"  'you must put *something* in the panel or it's not rendedred (and then the javascript can't access it !)
        pnl.Controls.Add(lit)

        pnl.Controls.Add(MakeButton(False, "Es", "Edit the title, layout, labeling, visible columns and and help for this screen.", EmbedScript("Screens(" & screen.ID & ")", pnl.ID, "", False, True)))

        pnl.Controls.Add(MakeButton(False, "Rs", _
                                    "Regenerate this screen layout (based on the underlying object - all help, validation, layout and labeling will be lost).", _
                                    EmbedScript(Path$, pnl.ID, "deleteScreen," & screen.ID, False, False)))

        Return pnl

    End Function

    Public Shared Function MakeButton(compact As Boolean, txt As String, tooltip As String, script As String) As Panel

        MakeButton = New Panel
        MakeButton.CssClass = "editorButton "
        If compact Then MakeButton.CssClass &= " compactButton"
        Dim lbl As Label = New Label
        lbl.Text = txt
        lbl.ToolTip = tooltip
        MakeButton.Controls.Add(lbl)
        MakeButton.Attributes("onclick") = script

    End Function
    Private Function closeButton(path$, divid As String, language As clsLanguage) As Panel

        closeButton = MakeButton(False, "✖", Xlt("Close (and save)", language), EmbedScript(path$, divid, "close", False, True))
        'closeButton.Attributes("style") = "position:absolute;top:8px;right:8px;"

    End Function

    Private Sub SaveChanges(screen As clsScreen, path As String, PathedObject As Object, agentaccount As clsAccount, ByRef errormessages As List(Of String))

        'The request (from embed() is POSTED - so the data is in the FORM variables (not the request.querystring) 
        'The variable names tell us which property, on which object they carry a value for
        'rk$ = "c" & col.ID & "_" & Row.id 'dk - the 'col.id' portion is the property, the row id is the id of the obejct in the dictionary

        'collect all the updates together by object, so we only need .update() each altered object once (and can also only create 1 additional entry for auditable objects)
        'make a dictionary of ObjectID to update > fieldID,value pair
        'many rows can be updated in a single 'save' operation - but only within the same dictionary
        Dim UpDic As Dictionary(Of Integer, Dictionary(Of Integer, String))
        UpDic = New Dictionary(Of Integer, Dictionary(Of Integer, String))

        For Each k As String In Request.QueryString.Keys ' 'Request.Form.Keys 'was request.form.keys BUT DOESNT WORK !!!

            ' Debug.Print(k)

            If (k.StartsWith("c_") Or k.StartsWith("cb_")) And InStr(3, k, "_") > 0 Then
                'rk$ = "c" & col.ID & "_" & Row.id 'dk

                Dim objId As Integer
                Dim bits() As String
                Dim FieldID As Integer

                bits = Split(k, "_")
                If bits.Length <> 3 Then
                    errormessages.Add("Expected 3 Bits - got " & bits.Length)
                Else

                    FieldID = CInt(bits(1))
                    objId = CInt(bits(2))

                    If Not UpDic.ContainsKey(objId) Then UpDic.Add(objId, New Dictionary(Of Integer, String))
                    UpDic(objId).Add(FieldID, Request(k))
                End If
            End If
        Next

        ' PathedObject is *typically* a dictionary (the one at the end of the path) - but can be a single object (for a page update)

        SaveGroupedChanges(screen, path, PathedObject, UpDic, agentaccount, False, errormessages)

        ' Next

    End Sub

    Private Sub SaveGroupedChanges(screen As clsScreen, path As String, obj As Object, dicUpdates As Dictionary(Of Integer, Dictionary(Of Integer, String)), agentaccount As clsAccount, auditable As Boolean, ByRef errorMessages As List(Of String))

        'obj is the object we're updating 

        Dim target As Object = Nothing
        Dim newrow As Object

        For Each objid In dicUpdates.Keys   'each 'row' in here has all the updated properties (indexed by fieldID) for single object (or row in a dictionary)

            target = Nothing
            If Reflection.IsDictionary(obj) Then
                If obj.containskey(objid) Then target = obj(objid) 'it's possible an object (in dicupdates) has been deleted
            Else
                target = obj
            End If

            If target Is Nothing Then
                errorMessages.Add("target was nothing ")
            Else

                If auditable Then
                    newrow = target.clone 'creates an exact copy - but with a new ID 
                    target.current = False 'this one is no longer current
                    target.update(errorMessages)
                    newrow.timeStamp = Now
                    target = newrow 'we'll change the columns (apply the edits) to the new row
                End If

                Dim fld As clsField
                For Each fieldkey In dicUpdates(objid).Keys 'the these keys are the fields in each updated object, 
                    fld = screen.Fields(fieldkey)

                    '     Dim oldvalue As String = ParsePath(path$ & "(" & objid & ")" & screen.Fields(fieldkey).propertyName, oldobj, Nothing, errormessgaes)
                    ' If col.lookupOf<>"" then oldvalue=

                    Dim oldValue As String = getPropertyString(target, fld.propertyName, agentaccount.Language)
                    Dim newValue As String = dicUpdates(objid)(fieldkey)

                    Dim pathToProp As String = path & "(" & objid & ")." & fld.propertyName

                    If dicUpdates(objid)(fieldkey) = "null" Then

                        setProperty(target, fld.propertyName, Nothing, Nothing, errorMessages, True)

                    Else
                        setPropertyFromString(fld, target, newvalue, agentaccount.Language, errorMessages)
                    End If
                    AuditLog.Instance.Add(AuditType.Editor, String.Format("{0}, Id:{1} updated to {2}", fld.propertyName, target.id, newvalue), "Editor", 0)
                    logEdit(agentaccount, "E", pathToProp, oldValue, newValue)
                Next
                target.update(errorMessages) 'call the update method on the object we changed to persist the changes 

            End If
        Next

        'If dicUpdates.Count Then sw.Close()

    End Sub

    Private Sub logEdit(agentaccount As clsAccount, action As String, path As String, oldValue As String, newValue As String)

        Dim sql$ = "INSERT INTO editLog (fk_account_id_agent,action,path,oldvalue,newvalue,timestamp,undone,comments) VALUES ("
        sql$ &= agentaccount.ID & ",'" & action & "'," & da.SqlEncode(path) & "," & da.SqlEncode(oldValue) & "," & da.SqlEncode(newValue) & ",getdate(),null,'')"

        da.DBExecutesql(sql$)

    End Sub

    Private Function EditObj(screen As clsScreen, obj As Object, expanded As Boolean, inPanel As Panel, enabled As Boolean, _
                             language As clsLanguage, buyerAccount As clsAccount, Rownum As Integer, _
                             path$, HideHistoryButton As Boolean, ByRef errormessages As List(Of String), translateLanguage As clsLanguage) As Panel

        'obj is always a single object (never a dictionary at this point)
        'If page is true = we present as a page, if page is false we present as a single row)
        'all rows have an expand/collapse button which maintains a session variable paths of expanded objects, 
        '(which will be presetted in 'page' mode.. overriding pararmenter - this is basically so we can expand entities in lists

        Dim objPnl As Panel 'this is what we Return
        objPnl = New Panel

        'Populate the Quote counts on Account objects - 'just in time'
        Dim act As Type
        act = System.Type.GetType("IQ.clsAccount")
        If obj.GetType Is act Then obj.user.countQuotesPerAccount()

        Dim g As Guid = New Guid
        Dim clear As Panel

        Dim DOTs As List(Of String) = DescendantObjectTypes(obj, screen, errormessages)

        If expanded Then
            'Dim lit As Literal = New Literal
            'lit.Text = "<br/>"
            'objPnl.Controls.Add(lit)
            objPnl = LayoutAsPage(path, obj, screen, True, language, buyerAccount, dots, errormessages, translateLanguage) 'returns the names on any descendant objects (so we know why the delete button is disabled)
        Else

            objPnl = LayoutAsRow(path, obj, screen, True, language, buyerAccount, DOTs, errormessages, translateLanguage)
        End If

        clear = New Panel
        clear.Style.Add("clear", "both")
        objPnl.Controls.Add(clear)

        Return objPnl

    End Function
    Private Function ObjButtons(inPanel As Panel, obj As Object, path As String, DOTs As List(Of String), expanded As Boolean) As PlaceHolder


        Dim ph As PlaceHolder = New PlaceHolder

        If DOTs.Count > 0 Then
            If obj.id.GetType() Is GetType(Dictionary(Of clsLanguage, Int32)) Then ' ml for translations obj.id is a dictionatry
                ph.Controls.Add(MakeButton(True, "∪", "Delete - You must delete the descendant " & Join(DOTs.ToArray, ",") & " first", EmbedScript(path, inPanel.ID, "del," & CType(obj.id, Dictionary(Of clsLanguage, Int32))(English), False, False))) 'don't save the object we're deleting
            Else
                ph.Controls.Add(MakeButton(True, "∪", "Delete - You must delete the descendant " & Join(DOTs.ToArray, ",") & " first", EmbedScript(path, inPanel.ID, "del," & obj.id, False, False))) 'don't save the object we're deleting
            End If

        Else
            ph.Controls.Add(MakeButton(True, "∪", "Delete- You must delete", EmbedScript(path, inPanel.ID, "del," & obj.id, False, False))) 'don't save the object we're deleting
        End If


        If obj.id.GetType() Is GetType(Dictionary(Of clsLanguage, Int32)) Then ' ml for translations obj.id is a dictionatry
            ph.Controls.Add(MakeButton(True, "∬", "Copy this object", EmbedScript(path$, inPanel.ID, "copy," & CType(obj.id, Dictionary(Of clsLanguage, Int32))(English), False, True)))
        Else
            ph.Controls.Add(MakeButton(True, "∬", "Copy this object", EmbedScript(path$, inPanel.ID, "copy," & obj.id, False, True)))
        End If



        If expanded Then
            ph.Controls.Add(MakeButton(False, "-", "Collapse to a row", EmbedScript(path, inPanel.ID, "collapse", False, True)))
        Else
            ph.Controls.Add(MakeButton(False, "+", "Expand to a page", EmbedScript(path, inPanel.ID, "expand", False, True)))
        End If


        'If screen.Auditable Then
        'If obj.current Then
        '    Dim btnhist As Button
        '    btnhist = New Button

        '    If Not HideHistoryButton Then
        '        btnhist.Text = "Show History"
        '        btnhist.OnClientClick = EmbedScript(path, "Hist," & obj.auditroot.id, False, True)
        '    End If

        '    btnhist.ToolTip = btnhist.OnClientClick
        '    objPnl.Controls.Add(btnhist)

        '    'only the current row has a delete button (not historical ones)
        '    objPnl.Controls.Add(btndelete)
        'End If
        'Else
        '       ph.Controls.Add(btndelete)
        '        End If

        Return ph

    End Function

    Private Function LayoutAsPage(path As String, obj As Object, screen As clsScreen, enabled As Boolean, language As clsLanguage, buyerAccount As clsAccount, DOTs As List(Of String), ByRef errorMessages As List(Of String), translateLanguage As clsLanguage) As Panel

        'Displays the object for editing as a page within Pnl, using the template screen

        'returns a list of an descendant object types (to become tooltip text on the dsiabled delete button)
        LayoutAsPage = New Panel

        Dim lit As Literal

        Dim em As UnitType
        em = UnitType.Em 'Percentage

        'Dim pnl As Panel = EmptyPanel("v." & obj.id)
        Dim pnl As Panel = EmptyPanel(path) '"v." & obj.id)
        'pnl.ID = "row." & obj.id & "." ') 'New Panel
        ' pnl.CssClass &= " editRow"

        Dim lblPanel As Panel
        Dim lbl As Label
        Dim col As Panel

        'use LINQ to order by order
        Dim fields = (From f In screen.Fields.Values Order By f.order)

        Dim script$ = ""
        Dim row As Panel

        For Each f In fields ' screen.Fields.Values

            row = New Panel
            pnl.Controls.Add(row)

            Dim subpanel As Panel = EmptyPanel("pg.sub." & f.propertyName)
            pnl.Controls.Add(subpanel)

            lblPanel = New Panel  'Leftmost panel carries the field labels
            With lblPanel
                .Style("width") = "10em"
                .Style("text-align") = "right"
                .Style("display") = "inline-block"
                .Style("margin-right") = ".75em"
            End With

            lbl = New Label
            lbl.Text = f.labelText.text(language)
            lbl.Font.Bold = True
            lblPanel.Controls.Add(lbl)
            row.Controls.Add(lblPanel)

            col = New Panel  'The central panel which carriues the acutal UI
            row.Controls.Add(col)
            col.Style("float") = "left"
            col.Style("margin-bottom") = ".75em"

            'OBSOLTE - Used to provide alist of dependent objects (to determin if it can be deleted) If f.InputType.code = "many" Then
            '    Dim adic As Object
            '    adic = WalkPropertyValue(obj, f.propertyName, errorMessages)
            '    If Not adic Is Nothing Then
            '        If WalkPropertyValue(obj, f.propertyName, errorMessages).Values.Count > 0 Then
            '            LayoutAsPage.Add(f.propertyName) 'yes, we have descendant (one of our 'many' properties - has objects in it
            '        End If
            '    End If
            'End If

            If f.visiblePage Then
                col = f.EditUI(obj, path, subpanel, enabled, Request, language, buyerAccount, True, errorMessages, translateLanguage)  'adds the main UI element (dropdown, textbox, calendar tickbox etc)
                col.Style("display") = "inline-block"
                col.Style("width") = "20em"
                script$ &= f.validationScript

                col.Style("height") = f.height & "em"
                col.Style("background-color") = "white"

            Else
                'hidden row (set to 1 em so it can be reinstated via it's show button)
                col = New Panel
                col.Style("display") = "inline-block"
                col.Style("height") = "1em"
                lit = New Literal
                lit.Text = ""
                col.Controls.Add(lit)
            End If
            row.Controls.Add(col)

            '3rd column, help text
            Dim helpcol As Panel
            helpcol = New Panel
            helpcol.Style("display") = "inline-block"
            row.Controls.Add(helpcol)

            lbl = New Label : lbl.Text = f.helpText
            lbl.Style("margin-left") = "1em"
            helpcol.Controls.Add(lbl)

        Next

        pnl.Controls.Add(ObjButtons(pnl, obj, path, DOTs, True))

        Dim img As New Image
        img.ImageUrl = eim$ & "resort.png"
        img.Width = 1
        img.Height = 1

        img.Attributes.Add("onload", script$)
        pnl.Controls.Add(img)

        Return pnl

    End Function

    Private Function DescendantObjectTypes(obj As Object, screen As clsScreen, ByRef errormessages As List(Of String)) As List(Of String)

        Dim DOTs As List(Of String) = New List(Of String)

        For Each f In screen.Fields.Values ' screen.Fields.Values

            If f.InputType.code = "many" Then
                Dim adic As Object
                adic = WalkPropertyValue(obj, f.propertyName, errormessages)
                If Not adic Is Nothing Then
                    If adic.Values.Count > 0 Then
                        '            pnl.Add(f.propertyName) 'yes, we have descendant (one of our 'many' properties - has objects in it) - return as a list (so we know what's preventing deletion)
                        DOTs.Add(f.propertyName)
                    End If
                End If
            End If
        Next

        Return dots

    End Function



    Private Function LayoutAsRow(path As String, obj As Object, screen As clsScreen, enabled As Boolean, language As clsLanguage, buyeraccount As clsAccount, DOTs As List(Of String), ByRef errormessages As List(Of String), translateLanguage As clsLanguage) As Panel
        'Displays the object for editing as a row within Pnl, using the template screen
        'returns a list of an descendant object types (to become the tooltip text on the disabled delete button) e.g. "You can't add an atttribute until you have some products"

        'Dim subpanel As Panel = EmptyPanel("sub.row." & path) ' 'we make this now - becuase we need to add things into it - but it's added to the controls collection later (so it appears below the row)
        'Dim subpanel As Panel = EmptyPanel("sub." & path) ' 'we make this now - becuase we need to add things into it - but it's added to the controls collection later (so it appears below the row)

        'Dim pnl = EmptyPanel("row." & obj.id & ".") 'New Panel
        Dim pnl = EmptyPanel(path)
        pnl.CssClass &= " editRow"
        pnl.Attributes.Add("onmousedown", "burstBubble(event);$(this).toggleClass('highlighted');")
        'The enabled parameter is a bit of a red herring becuase this needs to be done at a field level really (and based on permissions/roles)

        'use LINQ to order by order
        Dim fields = (From f In screen.Fields.Values Order By f.order)

        'Dim col As Panel
        Dim lit As Literal = Nothing



        Dim script$ = ""
        pnl.Controls.Add(ObjButtons(pnl, obj, path, DOTs, False))

        Dim underPanel As Panel = New Panel
        For Each f In fields ' screen.Fields.Values

            If f.visibleList Then
                pnl.Controls.Add(f.EditUI(obj, path, underPanel, enabled, Request, language, buyeraccount, False, errormessages, translateLanguage))  'adds the main UI element (dropdown, textbox, calendar tickbox etc)
            Else
                pnl.Controls.Add(f.emptyCell(True, True))
            End If
        Next

        pnl.Controls.Add(ObjButtons(pnl, obj, path, DOTs, False))

        pnl.Controls.Add(underPanel)

        '  pnl.Controls.Add(subpanel) 'This will host the editing of any descendants - we've already passed it to EditUI - but we *add* it here, UNDER the row in question

        Return pnl


    End Function





    Public Shared Function EmbedScript(Path$, TargetDiv As String, cmd$, append As Boolean, sendNVPS As Boolean) As String

        'Path IS The DIV id (and co-incidentally the object model path)

        'function embed(url, elementID, append, sendNVPs) 
        'DONT put bckslashes in here - they are a JS escape character !!!
        '  EmbedScript = "embed('../editor/editor.aspx?cmd=" & cmd$ & "&path=" & Path$ & "','" & TargetDiv & "'," & LCase(append.ToString) & "," & LCase(sendNVPS.ToString) & ");return false;"
        EmbedScript = "embed('../editor/editor.aspx?cmd=" & cmd$ & "&path=" & Path$ & "','" & Path & "'," & LCase(append.ToString) & "," & LCase(sendNVPS.ToString) & ");return false;"
        Return EmbedScript

    End Function

    'Private Sub deleteRow(b As Button, e As System.EventArgs)

    '    Dim obj As Object = iq
    '    ParsePath(Request("path"), obj, Nothing) 'Populate via reflection, the object we're about to edit (and it's parent)

    '    Dim key As Integer = b.Attributes("key")
    '    obj(key).delete()                                       'call the delete method on the object (for cascading deletes)
    '    obj.remove(key)                                         'remove from the dictionary
    '    'findRecursive(apanel, "row" & key).Visible = False   'remove from the screen

    'End Sub



    Private Function AddNew(lid As UInt64, Screen As clsScreen, ParentObj As Object, EditHeader As clsEditHeader, buyeraccount As clsAccount, language As clsLanguage, ByRef errorMessages As List(Of String)) As Object ', b As Button, e As System.EventArgs)

        'Create a new instance of whatever it is this screen edits

        'treepath is our current position in the 

        Dim type$ = Screen.Obj

        '              ugly()
        Dim ty As System.Type
        ty = System.Type.GetType("IQ." & type$)

        Dim Instance As Object = Activator.CreateInstance(ty)    'calls the parameterless constructor - making a 'temporary' object on which we can set default values

        If Instance Is Nothing Then
            errorMessages.Add("could not instance IQ." & type$ & "(activiator.createInstace returned nothing)")
        Else

            Dim em$ = ""

            'Set defaults - including and parent-child relationships for recursive objects
            SetDefaults(lid, Instance, ParentObj, Screen, ty, errorMessages)  'This is very important we must populate the correct parent object

            If em$ = "" Then
                Instance = Instance.insert(errorMessages) 'All generic edtor editable objects must expose an insert method -  their parmeterized constructor is called - adding them to their parents dictionary

                If Screen.Auditable Then
                    If Instance.auditroot Is Nothing Then errorMessages.Add("Auditroot was nothing") : Return Nothing
                End If

                'We must now add a row to the datatable/dataview - in the edit header - which is what is providing our filtering and sorting
                EditHeader.addRow(lid, Instance, Screen, buyeraccount, language, errorMessages)
                EditHeader.Fromindex = EditHeader.VW.Count - 10  'move to the End of the view
                If EditHeader.Fromindex < 0 Then EditHeader.Fromindex = 0 'these were 1 - (BUG: views are 0 based)
                If EditHeader.RowIndex(Instance.id) = -1 Then
                    errorMessages.Add("The row you just added is not visible becuase of the filters in effect")
                End If

                Return Instance
            Else
                Return Nothing
            End If
        End If

    End Function
    Private Sub SetDefaults(lid As UInt64, ByRef Instance As Object, parentObj As Object, screen As clsScreen, ty As Type, ByRef errorMessages As List(Of String))

        Dim language As clsLanguage = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Language
        Dim defaultvalue As Object

        Dim pType As Type = parentObj.GetType


        'IMPORTANT !! - the cssClass 'input' is what tags fields for value tracking/saving - DONT'T change/remove it

        For Each f In screen.Fields.Values

            If f.InputType.code = "nullstring" Then
                Dim nullstring As nullableString = New nullableString()
                Reflection.setProperty(Instance, f.propertyName, nullstring, Nothing, errorMessages, False)
            ElseIf f.InputType.code = "nullint" Then
                Dim nullint As NullableInt = New NullableInt()
                Reflection.setProperty(Instance, f.propertyName, nullint, Nothing, errorMessages, False)
            ElseIf f.InputType.code = "translate" Then
                Dim translation As clsTranslation = iq.AddTranslation("Edit me", language, "DM", True, Nothing, 0, False)
                Reflection.setProperty(Instance, f.propertyName, translation, Nothing, errorMessages, False)
            ElseIf f.InputType.code = "string" Then
                Reflection.setProperty(Instance, f.propertyName, "", Nothing, errorMessages, False) 'will be overriden by any explicit default
            Else
                'And dictionary properites are intialised in the individual parameterless constructors/Insert Methods
            End If

            If f.defaultValue <> "" Then
                defaultvalue = f.defaultValue

                If LCase(f.defaultValue).Contains("[parent]") Then
                    '   'eg, 'fields','screen' property
                    defaultvalue = parentObj
                ElseIf LCase(f.defaultValue).Contains("[seller]") Then
                    '   'eg, 'fields','screen' property
                    defaultvalue = CType(iq.sesh(lid, "BuyerAccount"), clsAccount).SellerChannel

                    'we don't need to add this object to the parents dictionary becuase..once we set the parent, and subsequently call Insert, the parmaterized construcot is called - which adds the object to it's parents children

                ElseIf LCase(f.defaultValue).Contains("[treepath]") Then
                    defaultvalue = iq.sesh(lid, "treepath")

                ElseIf LCase(f.defaultValue).Contains("[tree]") Then
                    If parentObj.GetType Is ty Then
                        defaultvalue = parentObj ' we're 'into' the tree now - set this objects parent
                    Else
                        defaultvalue = Nothing 'this is a top level object in the tree
                    End If
                ElseIf LCase(f.defaultValue) = "[now]" Then
                    defaultvalue = Now
                ElseIf LCase(f.defaultValue) = "[nothing]" Then
                    defaultvalue = Nothing
                Else
                    '    Stop
                End If

                Reflection.setProperty(Instance, f.propertyName, defaultvalue, Nothing, errorMessages, False) 'implement '[parent].id type defaults

            End If

            If f.InputType.code = "one" Then  'This field holds a reference to a dictionary (a foriegn key) - it's stored as an integer

                If f.defaultValue = "" Then 'If we have set some explict defailt already ( perhaps [Parent] or [tree] then DON'T override this by just picking the first

                    If System.Type.GetType("IQ." & f.propertyName) Is pType Then
                        'this field is of the same type at the parent obejct - this is (almost certainly) a back-reference)
                        Stop
                    End If

                    'exmine the 'field.lookupof'
                    'some drop down lists (and therefore defaults) are FILTERED 
                    'for example, threads.state(group=threads)

                    'find the root level dictionary this field looks up values in
                    Dim lu$
                    lu$ = f.LooksUp
                    'instance was IQ
                    ops = Reflection.WalkPropertyValue(iq, lu$, errorMessages) 'we now fetch from the INSTANCEs dictionary - not the root level dictionaries

                    'filter that dictionary by any name value pair (specified in parentesis after the .lookup)
                    Dim filterPVP$ = GetParenthesisValue(f.lookupOf) ' Gets any filter Property Value Pair e.g.  "group=TH"

                    Dim defaultTo As Object = Findmatch(ops, filterPVP, errorMessages)  ' returns the first match - we only need 1 entry
                    'Dim defaultTo As Object
                    '                    defaultTo = ops.values(0)

                    If defaultTo Is Nothing Then
                        If f.defaultValue <> "[tree]" Then
                            errorMessages.Add(" You can't add a " & screen.Obj & " until you have some " & f.LooksUp)
                        End If
                    Else
                        'get the first value (as the defualt)
                        Reflection.setProperty(Instance, f.propertyName, defaultTo, Nothing, errorMessages, False) 'implement '[parent].id type defaults                            
                    End If
                End If
            End If


            'For auditable items... set the _root element to point to itself... Only the root item is 'ADDed'.. subsequent edits create copies (but they don't comne through here)
            ' If LCase(f.propertyName) = "auditroot" Then
            'Reflection.setProperty(instance, f.propertyName, instance, Nothing)
            'End If
        Next

    End Sub

    Private Function findRecursive(c As Control, toFind As String) As Control

        findRecursive = Nothing
        If c.ID = toFind Then Return c

        Dim result As Control
        For Each child In c.Controls
            result = findRecursive(child, toFind)
            If Not result Is Nothing Then
                Return result
            End If

        Next

    End Function


End Class

