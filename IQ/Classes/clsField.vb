'Option Strict On
Imports dataAccess
Imports System.Runtime.Serialization

Public Class clsField

    Implements i_Editable

    Public Property ID As Integer
    Public Property Screen As clsScreen  'The generic editor needs to 'see' this so it can populate it with its parent as a defualt.. otherwise, we cannot create instances of it
    Public Property propertyName As String 'which property does this field edit/display (on the instance of the object that its' screen edits)
    'Public Property PropertyClass As String 'what is the class of this property (for example clsUser)
    Public Property lookupOf As String     ' used for 1:1 relationships - the dictionary to look in - with optional (field=value) filter eg. threads.stautus might lookup staus(group=Threads)
    Public Property labelText As clsTranslation
    Public Property helpText As String
    Public Property Validation As clsValidation
    Public Property InputType As clsInputType
    Public Property length As Integer ' max character length
    Public Property order As Integer
    Public Property height As Single 'only applies in page/expanded mode
    Public Property defaultFilter As String 'carries a comma seperate list of the code of filtes which can be applied to this field
    Public Filters As Dictionary(Of Integer, clsFilter)
    Public Property defaultSort As String

    'need to make lookup fields look in a specific place (not necessarily Hygn's root dictionaries)
    ' Public Property EmbedScreen As clsScreen  'where a property is a list - defining a 1:M relationship .. manage the 'many' using this screen
    Public Property width As Single  'onscreen width in ems - only applies in list mode
    Public Property defaultValue As String
    Public Property visibleList As Boolean
    Public Property visiblePage As Boolean
    Public Property visibleSquare As Boolean
    Public Property Grow As Boolean
    Public Property DefaultFilterValues As String
    Public Property FilterVisible As Boolean

    Public Property priority As Integer 'How 'important' is this column - higher numbers are collapsed sooner

    'the 'quickfilter' is an optional, additional set of filtering UI which can appear on top of any matrix
    Public Property QuickFilterGroup As clsTranslation  'multiple fields are grouped to form a single set of radioButtons (or checkboxes) - this is both the title and the 'grouper'
    Public Property QuickFilterUItype As String  'How to presents this fields quickfilter UI  Check,Radio, with/without  '
    Public Property CanUserSelect As Boolean
    Public Property LinkedFieldID As Integer?
    Public Property ValidRegions As Dictionary(Of Integer, clsRegion)
    Public Property HMC_MutuallyExclusive As Boolean
    Public Property InvertFilterOrder As Boolean

    Public validationScript As String
    Dim oPropertyName As String

    Public Sub New()

        propertyName$ = ""
        'PropertyClass = ""
        lookupOf = ""
        Me.labelText = iq.AddTranslation("", English, "", 1, Nothing, 0, False)
        Me.helpText = ""
        Me.priority = 1

        oPropertyName = propertyName

    End Sub

    'Public Sub New(screen As clsScreen, ID As Integer, propertyname As String, lookupof As String, embedScreen As clsScreen, labeltext As String, helptext As String, validation As clsValidation, inputtype As clsInputType, length As Integer, order As Integer, width As Single, defaultvalue As String, visible As Boolean)
    'Public Sub New(screen As clsScreen, ID As Integer, propertyname As String, PropertyClass As String, lookupof As String, labeltext As String, helptext As String, validation As clsValidation, inputtype As clsInputType, length As Integer, order As Integer, width As Single, height As Single, defaultvalue As String, visibleList As Boolean, visiblePage As Boolean, defaultFilter As String, defaultSort As String)
    Public Sub New(screen As clsScreen, ID As Integer, propertyname As String, lookupof As String, labeltext As clsTranslation, helptext As String, validation As clsValidation, inputtype As clsInputType, length As Integer, order As Integer, width As Single, height As Single, defaultvalue As String, visibleList As Boolean, visiblePage As Boolean, visibleSquare As Boolean, defaultFilter As String, defaultSort As String, priority As Integer, quickFilterGroup As clsTranslation, quickFilterUIType As String, canUserSelect As Boolean, LinkedFieldID As Nullable(Of Integer), Grow As Boolean, DefaultFilterValues As String, FilterVisible As Boolean, HMC_MutuallyExclusive As Boolean, InvertFilterOrder As Boolean)

        screen.Fields.Add(ID, Me)

        Me.Screen = screen
        Me.ID = ID
        Me.propertyName = propertyname  'This is the property this field edits - it may be a simple Element, string, integer etc, 
        '                                another object (lookup of displayed as a dropdown list) .. or a dictionary - a collection of objects of another type
        '    Me.PropertyClass = PropertyClass  'what is the class of this property (for example clsUser)
        Me.lookupOf = lookupof
        'Me.EmbedScreen = embedScreen

        Me.labelText = labeltext
        Me.helpText = helptext
        Me.Validation = validation
        Me.InputType = inputtype
        Me.length = length
        Me.order = order
        Me.width = width
        Me.height = height
        Me.defaultValue = defaultvalue
        Me.visibleList = visibleList
        Me.visiblePage = visiblePage
        Me.defaultFilter = defaultFilter
        Me.defaultSort = defaultSort
        Me.priority = priority
        Me.QuickFilterGroup = quickFilterGroup
        Me.QuickFilterUItype = quickFilterUIType
        Me.CanUserSelect = canUserSelect
        Me.visibleSquare = visibleSquare
        Me.LinkedFieldID = LinkedFieldID
        Me.Grow = Grow
        Me.DefaultFilterValues = DefaultFilterValues
        Me.ValidRegions = New Dictionary(Of Integer, clsRegion)
        Me.FilterVisible = FilterVisible
        Me.HMC_MutuallyExclusive = HMC_MutuallyExclusive
        Me.InvertFilterOrder = InvertFilterOrder

        iq.Fields.Add(Me.ID, Me)
        If Not screen.i_field_property.ContainsKey(Me.propertyName) Then
            screen.i_field_property.Add(Me.propertyName, Me)
        Else
            ErrorLog.Add(New Exception("Screen " & screen.displayName(English) & " contains multiple references to " & Me.propertyName))
        End If

        oPropertyName = propertyname

    End Sub

    Public Function emptyCell(collapsed As Boolean, editor As Boolean) As Panel
        Return emptyCell(collapsed, Nothing, editor)
    End Function
    Public Function emptyCell(collapsed As Boolean, width As Double?, editor As Boolean) As Panel

        'the width of a cell is a bit of a red herring - 
        'becuase although the matrix cells are inline-block - they are positioned absolutely (to avoid wrapping)  - it would be nicer if they were in the flow (positioned relative)

        emptyCell = New Panel

        If editor Then
            emptyCell.CssClass = "editCell"
        Else
            emptyCell.CssClass = "matrixCell"
        End If

        Dim cellWidth As Single = If(width Is Nothing, Me.width, width)

        If collapsed Then cellWidth = collapsedColumnWidth
        emptyCell.Attributes("style") = "width:" & cellWidth & "em;"
        If If(width Is Nothing, Me.width, width) = 0 Then
            emptyCell.Attributes("style") &= "background-color:#ff9090;"
        End If

        'we must put *something* in the DIV or .net won't render it
        'Dim lt As Literal
        'lt = New Literal
        'lt.Text = "&nbsp;"
        'emptyCell.Controls.Add(lt)

    End Function

    Public Function NoPreferenceRadioButton(path As String, filters As List(Of clsFilter)) As Literal

        Dim rb As Literal = New Literal
        rb.Text = ""
        If filters.Count Then

            rb.Text = "<input type=~radio~ onclick=~{getBranches('path=" & path & "&cmd=removeFilter&filterParams=" & Me.ID & "|"
            rb.Text &= Join((From f In filters Select f.Code).ToArray, "|") & "')}~>"
            rb.Text &= "No preference</input>"
            rb.Text = rb.Text.Replace("~", Chr(34))

            Return rb
        End If


    End Function


    Public Function SortPriorityTextBox(sort$, ByRef Current As String) As TextBox 'DropDownList

        'builds a standard sort priority DDL, and Selects the current value in it for field
        'NB: Sort$ (the current sort parameters)  is module level

        SortPriorityTextBox = New TextBox 'DropDownList
        SortPriorityTextBox.CssClass = "sortPriorityTextBox"

        'are we currently sorting by this column (at all)
        Dim c As Integer = 0

        Current = "-"
        SortPriorityTextBox.Text = " -"
        For Each p In Split(sort$, ",")
            c = c + 1 'this is the sort priorty 1,2,3,4 or 5  (where it appears in the request.sort parameter comma seperated list)
            If InStr(p, "[" & Me.propertyName & "]") > 0 Then
                'yes we are sorting by this
                If InStr(p, "] ASC") > 0 Then
                    'SortPriority.SelectedValue = Trim$(c.ToString) & "A"
                    SortPriorityTextBox.Text = Trim$(c.ToString) & "⇧"
                    Current = Trim$(CStr(c)) & "A"  'we're currently soring price this column with priority C, ascending
                Else
                    'SortPriority.SelectedValue = Trim$(c.ToString) & "D"
                    SortPriorityTextBox.Text = Trim$(c.ToString) & "⇩"
                    Current = Trim$(CStr(c)) & "D"  'we're currently soring price this column with priority C, descending
                End If

            End If
        Next

    End Function

    Public Function OperatorDDL() As DropDownList

        'Returns a dropdown list filled with  approriate set of logical operators for filtering based on this inputType
        'Seletcts the current value in the list based on whats in the filter$ 

        'See IQ.loadfilters for a better undrestanding of how the filters work
        'eg. filters.Add("PM20", "[col]>=[filterValue]*.8 and [col]<=[filterValue]*1.2")

        OperatorDDL = New DropDownList

        With OperatorDDL


            ' .Items.Add(New ListItem("Equals", "EQ"))
            Select Case LCase(Me.InputType.code)

                'WATCH OUT WHEN USING ' (single quotes)   - you need to ESQ() them !!!

                Case Is = "string", "one", "nullstring", "translate"
                    'one's are treated as strings - but should ultimately become an 'is/in' filter

                    .Items.Add(New ListItem("Is", "EQ"))  'Note - the values (typed in the textboxes) get enclosed in single quotes
                    '.Items.Add(New ListItem("Ends with", "EW"))
                    '.Items.Add(New ListItem("Contains", "CN"))
                    '.Items.Add(New ListItem("Only", "ONLY"))
                    .Items.Add(New ListItem("Excluding", "EX"))

                Case Is = "many"
                    'for 'many' colums - we can only filter by the number of children presently)
                    'again, ultimately a 'has' filter - would be nice
                    .Items.Add(New ListItem("having n", "HN"))
                    .Items.Add(New ListItem("having n or more", "HNM"))
                    .Items.Add(New ListItem("having n  less ", "HNL"))
                Case Is = "int32", "nullint", "single", "customerprice", "nullprice"
                    .Items.Add(New ListItem("greater than or equal", "GE"))
                    .Items.Add(New ListItem("equals", "EQ"))
                    .Items.Add(New ListItem("less than or equal", "LE"))
                    .Items.Add(New ListItem("plus or minus 10%", "PM10"))
                    .Items.Add(New ListItem("plus or minus 20%", "PM20"))
                Case Is = "date"
                    .Items.Add(New ListItem("before", "B4"))
                    .Items.Add(New ListItem("after", "AFT"))
                    .Items.Add(New ListItem("on", "ON"))
                Case Is = "boolean"
                    .Items.Add(New ListItem("Ticked", "T"))
                    .Items.Add(New ListItem("UnTicked", "F"))
                Case Is = "icon"
                    .Items.Add(New ListItem("With", "WITH")) 'TODO with/without
                    .Items.Add(New ListItem("Without", "WITHOUT")) 'TODO with/without
                Case Else
                    Beep()
            End Select


        End With

    End Function



    'Public Sub New(screen As clsScreen, propertyName As String, lookupOf As String, embedScreen As clsScreen, labelText As String, helpText As String, validation As clsValidation, inputType As clsInputType, length As Integer, order As Integer, width As Single, defaultvalue As String, visible As Boolean)

    'Public Sub New(screen As clsScreen, propertyName As String, propertyClass As String, lookupOf As String, labelText As String, helpText As String, validation As clsValidation, inputType As clsInputType, _
    '                           length As Integer, order As Integer, width As Single, height As Single, defaultValue As String, visibleList As Boolean, visiblePage As Boolean, defaultFilter$, defaultSort$)
    Public Sub New(screen As clsScreen, propertyName As String, lookupOf As String, labelText As clsTranslation, helpText As String, validation As clsValidation, inputType As clsInputType, _
                                length As Integer, order As Integer, width As Single, height As Single, defaultValue As String, visibleList As Boolean, visiblePage As Boolean, defaultFilter$, defaultSort$, priority As Integer, quickFilterGroup As clsTranslation, quickFilterUIType As String, CanUserSelect As Boolean, LinkedFieldID As Integer?, FilterVisible As Boolean)
        Dim sql$

        '   sql$ = "INSERT INTO [field] (fk_screen_id,property,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],fk_screen_id_embed,[width],defaultValue,visible) "
        '   sql$ &= "VALUES (" & screen.ID & "," & da.SqlEncode(propertyName) & "," & da.SqlEncode(labelText) & "," & da.SqlEncode(helpText) & "," & NullID(validation) & ","
        '   sql$ &= SqlEncode(lookupOf) & "," & inputType.ID & "," & length & "," & order & "," & NullID(embedScreen) & "," & width & "," & da.SqlEncode(defaultvalue) & "," & IIf(visible, 1, 0) & ");"

        '    sql$ = "INSERT INTO [field] (fk_screen_id,property,propertyClass,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],[width],height,defaultValue,visibleList,VisiblePage,defaultfilter,defaultsort) "
        '    sql$ &= "VALUES (" & screen.ID & "," & da.SqlEncode(propertyName) & "," & da.SqlEncode(propertyClass) & "," & da.SqlEncode(labelText) & "," & da.SqlEncode(helpText) & "," & NullID(validation) & ","
        '    sql$ &= SqlEncode(lookupOf) & "," & inputType.ID & "," & length & "," & order & "," & width & "," & height & "," & da.SqlEncode(defaultValue) & "," & IIf(visibleList, "1", "0").ToString & "," & IIf(visiblePage, "1", "0").ToString & "," & da.SqlEncode(defaultFilter) & "," & da.SqlEncode(defaultSort) & ");"

        sql$ = "INSERT INTO [field] (fk_screen_id,property,fk_translation_key_label,helptext,fk_validation_id,lookupof,fk_inputtype_id,length,[order],[width],height,defaultValue,visibleList,VisiblePage,defaultfilter,defaultsort,[priority],FK_Translation_key_WidgetGroup,WidgetUI,CanUserSelect,VisibleSquare,Grows,DefaultFilterValues) "
        sql$ &= "VALUES (" & screen.ID & "," & da.SqlEncode(propertyName) & "," & labelText.Key & "," & da.SqlEncode(helpText) & "," & NullID(validation) & ","
        sql$ &= da.SqlEncode(lookupOf) & "," & inputType.ID & "," & length & "," & order & "," & width & "," & height & "," & da.SqlEncode(defaultValue) & "," & IIf(visibleList, "1", "0").ToString & "," & IIf(visiblePage, "1", "0").ToString & ","
        sql$ &= da.SqlEncode(defaultFilter) & "," & da.SqlEncode(defaultSort) & "," & priority & "," & TranslationKey(quickFilterGroup) & "," & da.SqlEncode(quickFilterUIType) & "," & da.SqlEncode(CanUserSelect) & "," & da.SqlEncode(visibleSquare) & "," & da.SqlEncode(Grow) & "," & da.SqlEncode(DefaultFilterValues) & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        screen.Fields.Add(Me.ID, Me)

        Me.Screen = screen
        Me.propertyName = propertyName 'This is the property (on the object) this field edits (or displays) - it may be a simple Element, string, integer etc, 
        '                                another object (lookup of displayed as a dropdown list) .. or a dictionary - a collection of objects of another type
        'Me.PropertyClass = propertyClass 'what is the class of this property (for example clsUser)

        Me.lookupOf = lookupOf
        'Me.EmbedScreen = embedScreen
        Me.labelText = labelText
        Me.helpText = helpText
        Me.Validation = validation
        Me.InputType = inputType
        Me.length = length
        Me.order = order
        Me.width = width
        Me.height = height
        Me.defaultValue = defaultValue
        Me.visibleList = visibleList
        Me.visiblePage = visiblePage
        Me.defaultFilter = defaultFilter
        Me.defaultSort = defaultSort
        Me.priority = priority
        Me.QuickFilterGroup = quickFilterGroup
        Me.QuickFilterUItype = quickFilterUIType
        Me.CanUserSelect = CanUserSelect
        Me.LinkedFieldID = LinkedFieldID
        Me.DefaultFilterValues = DefaultFilterValues
        Me.Grow = Grow
        Me.FilterVisible = FilterVisible

        iq.Fields.Add(Me.ID, Me)
        If Me.propertyName <> "" Then
            Me.Screen.i_field_property.Add(Me.propertyName, Me)
        End If
        'Me.Screen.Fields.Add(Me.ID, Me)

        oPropertyName = propertyName

    End Sub

    Public Function Insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert

        'Return New clsField(Me.Screen, Me.propertyName, Me.PropertyClass, Me.lookupOf, Me.labelText, Me.helpText, Me.Validation, Me.InputType, Me.length, Me.order, Me.width, Me.height, Me.defaultValue, Me.visibleList, Me.visiblePage, Me.defaultFilter, Me.defaultSort)
        Return New clsField(Me.Screen, Me.propertyName, Me.lookupOf, Me.labelText, Me.helpText, Me.Validation, Me.InputType, Me.length, Me.order, Me.width, Me.height, Me.defaultValue, Me.visibleList, Me.visiblePage, Me.defaultFilter, Me.defaultSort, Me.priority, Me.QuickFilterGroup, Me.QuickFilterUItype, Me.CanUserSelect, Me.LinkedFieldID, Me.FilterVisible)

    End Function

    Public Sub demote()

    End Sub

    ''' <summary>Returns a singe field value as Text (quoted for 'strings') suitable for a CSV export</summary>

    Public Function CSV(obj As Object, path As String, buyerAccount As clsAccount, language As clsLanguage, MatrixHeader As clsScreenHeader, collapsed As Boolean, foci As HashSet(Of String), ByRef errorMessages As List(Of String), lid As UInt64, width As Double?, Optional export As Boolean = False) As String

        'this has all become a little messy - Cellvalue could do with a refactor and some of these wrappers could be removed 

        Dim result As String = String.Empty
        Dim pnl As Panel = New Panel 'throwaway (in this case) panel for UI

        'this is populated                            \/ (byref)
        CellValue(obj, path, buyerAccount, language, result, pnl, collapsed, Nothing, Nothing, foci, errorMessages, lid, export)
        If Not String.IsNullOrWhiteSpace(result) Then
            result = result.Replace("&amp;", "&")
        End If
        Return result

    End Function

    Public Function CellUI(obj As Object, path$, buyerAccount As clsAccount, language As clsLanguage, MatrixHeader As clsScreenHeader, collapsed As Boolean, foci As HashSet(Of String), ByRef errorMessages As List(Of String), lid As UInt64, width As Double?) As Panel

        CellUI = Me.emptyCell(collapsed, width, False) 'New Panel

        Dim numericValue As String
        Dim csv As String 'throwaway (in this case) string for CSV export
        '                                                                  \/-  This gets POPULATED
        numericValue = CellValue(obj, path$, buyerAccount, language, csv, CellUI, collapsed, Nothing, Nothing, foci, errorMessages, lid)

        'If numericValue IsNot Nothing Then value = numericValue Else value = "\'" & value & "\'" 'Escape the quotes we require for filtering against literal strings

        'rdus - replaces dows with underscores
        CellUI.ID = rdus(path$) & "_" & Me.ID

        If Not collapsed Then

            Dim fbids As List(Of String) = New List(Of String) 'filter image button ID's
            If Me.defaultFilter <> "" Then
                For Each v In Split(Me.defaultFilter, ",")
                    fbids.Add("ctl00_FIB_" & v)
                Next
            End If
            'Dim omo$

            If fbids.Count > 0 Then

                'MatrixUI.Attributes("onmouseover") = omo$

                Dim omd$

                'TODO - replace whats below with this - (and a new JS function) omd$ = "showFilterButtons('" & bi.MatrixPath & "','" & Me.ID & "'," & numericValue.ToString & ",'" & Join(fbids.ToArray, ",") & "','" & MatrixUI.ID ');"

                'omd$ = "if(!onSpeechBubble){this.removeAttribute('Title');"
                'omd$ &= "filterPath='" & MatrixHeader.path & "';filterField=" & Me.ID & ";filterValue='" & numericValue.ToString & "';"
                'omd$ &= "showFilterButtons('" & Join(fbids.ToArray, ",") & "');display('ctl00_filterButtons','block');"
                'omd$ &= "$('#ctl00_filterButtons').position({my:'center top',at:'center bottom',of: '#" & CellUI.ID & "'})"
                'omd$ &= "};return false;"
                '      omd$ &= "var cellpos=$('#" & MatrixUI.ID & "').offset();$('#ctl00_filterButtons').css"

                '	<div id="target" onmousedown="$('#fbs').position({my:'center',at:'center',of:'#target'});"

                'MatrixUI.ToolTip = omd$
                CellUI.ToolTip = Xlt("Click to filter the list based on this value", language)
                CellUI.Attributes("onmousedown") = "arrowClick(onSpeechBubble,'" & MatrixHeader.path & "','" & Me.ID & "','" & numericValue.ToString & "','" & Join(fbids.ToArray, ",") & "','" & CellUI.ID & "',this);"

                CellUI.Attributes("class") &= "handPointer"

            End If
        End If

    End Function

    Public Function CellValue(obj As Object, path As String, buyerAccount As clsAccount, language As clsLanguage, ByRef csv As String, ByRef UI As Panel, collapsedColumn As Boolean, ByRef Translation As clsTranslation, ByRef Unit As clsUnit, foci As HashSet(Of String), ByRef errorMessages As List(Of String), lid As UInt64, Optional export As Boolean = False) As Object

        'in the customer facing UI the OBJ is a branch - but when used from the editor - 
        'it can be anything (an account, country, language etc,etc)

        'returns a piece of UI (label, claendar, checkbox, graph, textbox  etc) in a placeholder - and a numeric 'INT64' value for filtering and sorting

        csv = "Not Set" 'should *always* get overrridden

        Translation = Nothing 'For cells containing a translation - we return that too (for quickfilters)
        CellValue = Int64.MinValue 'Single.MinValue
        Dim IsBoolean As Boolean = False
        Dim propName As String = Me.propertyName
        If Len(Me.propertyName) > 2 AndAlso Right(Me.propertyName, 3) = "<B>" Then
            'This is a boolean field, yes has content or no doesn't only
            IsBoolean = True
            propName = Left(Me.propertyName, Len(Me.propertyName) - 3)
        End If
        Dim valueObject As Object = Nothing
        'If LCase(Me.propertyName).Contains("dmr") Then Stop

        If UI IsNot Nothing Then
            UI.CssClass &= " cls_" & Me.labelText.text(English).Replace(" ", "_")  'include the fields label text name as a CSS Class
        End If

        If SpecialColumn(Me, propName, obj, path, buyerAccount, language, UI, CellValue, Translation, csv, Unit, foci, errorMessages, lid, valueObject, export) Then
            If valueObject IsNot Nothing Then
                CellValue = valueObject
            End If
            'NB: CSV was set above (byRef) - and is CSV()'d already

            If collapsedColumn And UI IsNot Nothing Then
                UI.Controls.Clear()
                Dim lbl As Label = New Label
                lbl.Text = "-"
                UI.Controls.Add(lbl)
            End If

        Else
            'it WASN'T a 'special' column (price/stock/memory/display/supplychain)

            Dim lbl As Label
            lbl = New Label
            '            lbl.Style("width") = "100%"
            ' lbl.Style("whitespace") = "nowrap"
            lbl.Style("overflow") = "hidden"
            lbl.Style("word-wrap") = "break-word"
            If UI IsNot Nothing Then UI.Controls.Add(lbl)

            Dim tobj As Object 'the object at the end of the walk

            'Failing to get CP_DMR attribute
            tobj = Reflection.WalkPropertyValue(obj, propName, errorMessages)   'this is probably slow .. TODO consider short term cacheing at this level  (or possible better within walkproperty .. a cache of paths to recently walked values would speed up rows beyond the visible matrix too

            If TypeOf tobj Is String Then

                CellValue = tobj
                csv = Utility.CSV(tobj)
                lbl.Text = tobj
                lbl.Text = Replace(lbl.Text, " ", "&nbsp;")
                lbl.Text = Replace(lbl.Text, "-", "&#8209")

            ElseIf TypeOf tobj Is Integer Or TypeOf tobj Is Single Then

                csv = CInt(tobj) '.ToString("D") 'it's the SERVERS locale/regional settings here - so we shold get 'proper' decimal points and thousands sepearators

                CellValue = tobj 'CInt(Val(lbl.Text)) 'well, that was easy - strings probably need more careful hadling
                lbl.Text = Replace(lbl.Text, " ", "&nbsp;")
                lbl.Text = Replace(lbl.Text, "-", "&#8209")

            ElseIf TypeOf tobj Is clsTranslation Then
                Translation = CType(tobj, clsTranslation)
                lbl.Text = Translation.text(language)
                csv = Translation.text(language)
                CellValue = Translation.SortValue(language)

                lbl.Text = Replace(lbl.Text, " ", "&nbsp;")
                'lbl.Text = Replace(lbl.Text, "-", "&#8209")

            ElseIf TypeOf tobj Is clsProductAttribute Then  'the thing we've walked to is an attribute - like Disk RPM  - Also used for some product attributes which are presented as ICONS

                Dim prodatt As clsProductAttribute
                prodatt = CType(tobj, clsProductAttribute)


                '   If prodatt.Product.Attributes Then


                Translation = prodatt.Translation
                Unit = prodatt.Unit


                If Translation IsNot Nothing Then

                    'Any translation (and its ORDER will override any Numeric value (for the purposes of sorting and filtering)

                    CellValue = Translation.SortValue(language)  'some attributes (such as the RPM of an Drive have both text, and a numeric value)


                    Dim text As String = Translation.text(language)
                    csv = Utility.CSV(text)

                    If LCase(Me.InputType.code) = "sausage" Then 'values (productAttributes) to be displayed as sausage buttons have a translation which is their face text

                        CellValue = CInt(prodatt.NumericValue)
                        If UI IsNot Nothing Then
                            Dim wb As New Panel
                            wb.CssClass = "sausageButton"
                            If QuickFilterUItype <> "TKEY" AndAlso prodatt.NumericValue = 0 Then
                                wb.CssClass &= " greyed"
                            End If

                            lbl = New Label
                            lbl.Text = text
                            lbl.ToolTip = prodatt.Attribute.displayName(buyerAccount.Language) 'Use the full name of the productattribute as the tooltip

                            wb.Controls.Add(lbl)
                            UI.Controls.Add(wb)

                            '                            If prodatt.NumericValue <> 0 Then Stop

                            lbl.ToolTip &= " (" & CellValue.ToString & ")"

                        End If
                    Else
                        'this is a vanilla (non quickfilter UI)  ProducAttribute (WITH a translation) - we use its numericvalue if present
                        lbl.Text = text
                        lbl.ToolTip = Translation.text(language)
                        'If prodatt.NumericValue > 0 Then  'we want the translations sortvalue for product attributes with no numeric value '@@@
                        ' CellValue = CInt(prodatt.NumericValue)
                        'End If
                    End If
                Else
                    'numeric attribute (one where there is no text/translation) and units (and possibly conversions)
                    If InputType.code = "rounddecimal" Then
                        CellValue = Math.Round(CDec(prodatt.NumericValue), 2)
                    Else
                        CellValue = CInt(prodatt.NumericValue)
                    End If

                    ' Handle sausages
                    If LCase(Me.InputType.code) = "sausage" Then

                        If UI IsNot Nothing Then
                            Dim wb As New Panel
                            wb.CssClass = "sausageButton"
                            If QuickFilterUItype <> "TKEY" AndAlso prodatt.NumericValue = 0 Then
                                wb.CssClass &= " greyed"
                            End If

                            lbl = New Label
                            lbl.Text = CellValue.ToString()
                            lbl.ToolTip = prodatt.Attribute.displayName(buyerAccount.Language) 'Use the full name of the productattribute as the tooltip

                            wb.Controls.Add(lbl)
                            UI.Controls.Add(wb)

                            lbl.ToolTip &= " (" & CellValue.ToString & ")"
                        End If

                    Else
                        lbl.Text = CellValue.ToString
                    End If

                    csv = prodatt.NumericValue.ToString  'this is an integer (may need to change)

                    If prodatt.Unit.Code <> "txt" Then
                        lbl.Text &= " " & prodatt.Unit.Symbol   ' todo Symbol
                    End If

                    'Note - we don't need to check the 'value' of Icons - becuase just having it indicates a 'true'
                    If LCase(Me.InputType.code) = "icon" Then
                        If UI IsNot Nothing Then

                            Dim image As New Image
                            'We extract the code form the parentheses at the end of the propertname - to use as the image name
                            image.ImageUrl = imagebase & "/images/icons/icon_opttype_" & DeBracket(propName, "(", ")")(0) & ".png"

                            'the acutal value becomes a tooltip 
                            If lbl.Text = "-1" Then lbl.Text = Yes.text(language) 'Quick fix 
                            If lbl.Text = "0" Then lbl.Text = No.text(language)

                            image.ToolTip = lbl.Text
                            lbl.Text = ""
                            UI.Controls.Add(image)
                        End If
                    End If
                End If

            ElseIf Me.InputType.code = "one" Then
                lbl.Text = tobj.displayname(buyerAccount.Language)
                csv = Utility.CSV(lbl.Text)
                CellValue = tobj.id 'the value is the ID of the object referenced (the foreign key) - used for filtering in the editor

            ElseIf tobj Is Nothing Then
                csv = Utility.CSV("-")
                lbl.Text = "-"
                CellValue = Int64.MinValue 'Single.MinValue
            Else
                errorMessages.Add("unknown object type " & tobj.GetType.ToString & "|" & path & "|" & propName)
            End If
        End If

        If IsBoolean Then
            CellValue = If(CellValue = Int64.MinValue Or CellValue = "0", 0, 1)
            If UI IsNot Nothing Then
                Dim lit As Literal = New Literal
                Dim l As Literal = UI.Controls(0)
                'lit.Text = "<div>" + If(CellValue = Int64.MinValue Or CellValue = "0", _
                '                        No.text(language), _
                '                        Yes.text(language)) + "</div>"

                lit.Text = "<div>" + If(CellValue = Int64.MinValue Or CellValue = "0", _
                                       "<img src='images/navigation/cross.png'/>", _
                                        "<img src='images/navigation/cross.png'/>") + "</div>"


                csv = Utility.CSV(If(CellValue = Int64.MinValue Or CellValue = "0", _
                                        No.text(language), _
                                        Yes.text(language)))

                If UI IsNot Nothing Then
                    UI.Controls.Clear()
                    UI.Controls.Add(lit)
                End If
            End If
        End If

        If Not UI Is Nothing Then
            If collapsedColumn Then UI.Controls.Clear() : Dim lbl As Label = New Label : lbl.Text = "-" : UI.Controls.Add(lbl)
        End If


    End Function


    Public Function SpecialColumn(f As clsField, propName As String, obj As Object, path$, buyeraccount As clsAccount, language As clsLanguage, ByRef UI As Panel, ByRef Value As Int64, ByRef Translation As clsTranslation, ByRef csv As String, ByRef unit As clsUnit, foci As HashSet(Of String), ByRef errormessages As List(Of String), lid As UInt64, ByRef valueObject As Object, Optional export As Boolean = False) As Boolean

        'value will *very often* be a string representation of a 64bit Number

        'Populates the UI and a numeric value for soring and filtering
        'returns TRUE if is *is* a 'special' Column

        Dim lbl As Label = New Label
        lbl.Style("width") = "100%"
        lbl.Text = "-"

        Dim bp As String = Split(propName, "(")(0) ' find the first segment before any open parethesis

        SpecialColumn = True 'The CASE ELSE sets this to FALSE for 'non-speicial' columns

        unit = Nothing

        csv = Utility.CSV("unhandled 'special'column " & f.propertyName)

        Select Case Trim$(LCase(bp))

            Case Is = "slots"  'broken by the new major/minor slot type

                'looks on child branches for slots 'gives' of the specified type (becuase the slots are in the chassis/mobo)

                'Returns a dictionary of the number of slots by major type - recursing form the current branch - does not cross systems
                Dim majorSlots As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
                CType(obj, clsBranch).MajorSlots(majorSlots)  'Call the majorSlots (recursive) method on the branch object - to fill the dictionary

                Dim majorSlotType As String = DeBracket(propName, "(", ")")(0) 'extract the slot type from the brackets eg slots(CPU)

                If majorSlots.ContainsKey(majorSlotType) Then
                    lbl.Text = CStr(majorSlots(majorSlotType))
                    If UI IsNot Nothing Then UI.Controls.Add(lbl)
                    Value = majorSlots(majorSlotType).ToString
                    csv = Value
                Else
                    If UI IsNot Nothing Then UI.Controls.Add(lbl)
                    Value = Int64.MinValue.ToString
                    csv = Utility.CSV("-")
                End If


                unit = iq.i_unit_code("num")

                'Dim found As Boolean = False
                'For Each cb In obj.childbranches.values

                '    Dim ck$ 'build a compound key - telling us we want the 'gives' slots - no slot number
                '    Dim parms As List(Of String) = DeBracket(f.propertyName, "(", ")") 'extract the slot type from the brackets eg slots(CPU)

                '    ck$ = parms(0) & "_" & path & "." & Trim$(cb.id) & "_1_null"

                '    If cb.i_Slots.ContainsKey(ck$) Then
                '        value = cb.i_Slots(ck$).numSlots
                '        lbl.Text = value.ToString
                '        If UI IsNot Nothing Then UI.Controls.Add(lbl)
                '        found = True
                '        Exit For
                '    End If

                '    ck$ = parms(0) & "__1_null"

                '    If cb.i_Slots.ContainsKey(ck$) Then
                '        value = cb.i_Slots(ck$).numSlots
                '        lbl.Text = value.ToString
                '        If UI IsNot Nothing Then UI.Controls.Add(lbl)
                '        found = True
                '        Exit For
                '    End If
                'Next

                'If Not found Then
                '    value = CInt(0)
                '    If UI IsNot Nothing Then
                '        lbl.Text = "-"
                '        UI.Controls.Add(lbl)
                '    End If
                'End If

            Case Is = "xtext" 'external text (formely TextExternal)

                Dim product As clsProduct = CType(obj, clsBranch).Product
                'value = 0 ' we must return *something* (even if there was no xtext) to indicate this was a specialcolumn

                csv = ""

                Dim vmsgs As List(Of ClsValidationMessage) = product.getXtext(path, Nothing)
                If vmsgs.Count = 0 Then
                    If UI IsNot Nothing Then UI.Controls.Add(lbl)
                Else
                    For Each msg In vmsgs
                        If UI IsNot Nothing Then UI.Controls.Add(msg.CompactUI(language))
                        If msg.severity > Value Then
                            Value = msg.severity  'Return the max severity from the set of Xtexts as the (sortable) value
                        End If
                        csv &= msg.message.text(language) & vbCrLf
                    Next
                End If

                csv = Utility.CSV(csv)

                Translation = Nothing ' if we were going to sort xTexts by anything, it would be their Severity - not alphabetically

            Case Is = "scheme"
                Dim product As clsProduct
                product = CType(obj, clsBranch).Product

                Dim code As String = DeBracket(propName, "(", ")")(0) 'extract the scheme code

                If Not iq.i_scheme_code.ContainsKey(code) Then
                    If errormessages.Count < 10 Then
                        errormessages.Add("Unknown loyalty scheme code " & code)
                    End If
                Else
                    For Each scheme In iq.i_scheme_code(code)
                        If scheme.Region.Encompasses(buyeraccount.SellerChannel.Region) Then
                            If product.Points.ContainsKey(scheme) Then
                                Value = product.Points(scheme).ToString
                            Else
                                Value = "0"
                            End If

                            If Not UI Is Nothing Then
                                If CInt(Value) > 0 Then
                                    Dim lit As Literal = New Literal
                                    lit.Text = "<div class='loyaltyPoints'>" & Value.ToString & "</div>"
                                    UI.Controls.Add(lit)
                                End If
                            End If
                        End If
                    Next
                End If

                If Value = Int64.MinValue Then
                    csv = 0
                Else
                    csv = Value.ToString
                End If


                Translation = Nothing

            Case Is = "promo", "promos"

                Dim product As clsProduct
                Dim branch As clsBranch = CType(obj, clsBranch)
                product = CType(obj, clsBranch).Product

                Dim code As String = DeBracket(propName, "(", ")")(0) 'extract the scheme code

                csv = ""

                Select Case (code)

                    Case Is = "A"
                        If branch.hasAvalanche(buyeraccount.BuyerChannel, path) Then
                            Value = "1"
                            Dim lit As Literal = New Literal
                            lit.Text = "<div class='avalancheGrid'>AV</div>"
                            If UI IsNot Nothing Then
                                UI.Controls.Add(lit)
                            End If
                            Translation = iq.AddTranslation("Avalanche", English, "Promos", 0, Nothing, 0, False)
                            csv = Utility.CSV(code)
                        End If


                    Case Is = "F"

                        If branch.hasFlexAttach(buyeraccount, path$, foci, errormessages) Then
                            Value = "1"
                            Dim lit As Literal = New Literal
                            lit.Text = "<div class='flexF'>F</div>"
                            If UI IsNot Nothing Then
                                UI.Controls.Add(lit)
                            End If
                            Translation = iq.AddTranslation("Flex Attach", English, "Promos", 0, Nothing, 0, False)
                            csv = Utility.CSV(code)
                        End If

                    Case Is = "B"

                    Case Is = "R"
                        If product.hasPromo("R", buyeraccount.BuyerChannel.Region) Then
                            Value = "1"
                            Dim lit As Literal = New Literal
                            lit.Text = "<div class='recetaR'><span>&#x2713;</span></div>"
                            If UI IsNot Nothing Then
                                UI.Controls.Add(lit)
                            End If
                            unit = iq.i_unit_code("txt")
                            Translation = iq.AddTranslation("Receta", English, "Promos", 0, Nothing, 0, False)
                            csv = Utility.CSV(code)
                        End If
                    Case Else

                End Select


            Case Is = "customerprice"

                Dim product As clsProduct
                product = CType(obj, clsBranch).Product

                If product Is Nothing Then Return Nothing
                If product.SKU <> "" Then 'product.i_Attributes_Code.ContainsKey("MfrSKU") Then

                    Dim prices As List(Of clsPrice)
                    If product.SKU.StartsWith("###") Or product.SKU.ToUpper.StartsWith("FAKE") Then
                        prices = New List(Of clsPrice) 'no prices for fake products (especially DONT call the webservice!)
                    Else
                        'Dim withoutWebService As Integer = buyeraccount.SellerChannel.priceConfig And Not 8
                        prices = product.GetPrices(buyeraccount, buyeraccount.SellerChannel.priceConfig, iq.AllVariants, errormessages, True) 'this can return multiple prices (for multiple variants - different warehouses, localisations etc)
                    End If

                    If prices.Count = 0 OrElse prices(0) Is Nothing Then
                        Value = Int64.MinValue.ToString  'POA
                    Else

                        Dim lowest As clsPrice = Utility.LowestPrice(prices) 'they will all be in the same currency (that of the buyer account)

                        ' If UI IsNot Nothing Then UI.Controls.Add(CType(obj, clsBranch).BuyUI(buyeraccount, path))
                        'Dim quoteLocked As Boolean = False ' Check for if a quote was exported to remove the +button
                        'If iq.sesh(lid, "QuoteLocked") IsNot Nothing Then
                        '    quoteLocked = CBool(iq.sesh(lid, "QuoteLocked"))

                        'End If

                        If UI IsNot Nothing Then
                            If lowest.SKUVariant IsNot Nothing Then
                                Dim prp As Panel = lowest.Ui(buyeraccount, 1, lid) 'has its own width and inline-block - this panel (div)  is entirely replaced by the arriving (asynch) prices
                                UI.Controls.Add(prp)

                                ' If there is a current quote, work out whether it's HPI or HPE
                                Dim quote As clsQuote
                                Dim quoteSplit = Manufacturer.Unknown
                                If iq.sesh(lid, "QuoteID") IsNot Nothing Then
                                    If iq.sesh(lid, "AgentAccount") IsNot Nothing Then
                                        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
                                        quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"))
                                        quoteSplit = quote.QuoteSplit
                                    End If
                                End If

                                ' Work out whether adding is enabled according to the HPE/HPI split
                                Dim addEnabled As Boolean = True
                                If product.isSystem(path) Then
                                    If Not quoteSplit = Manufacturer.Unknown Then
                                        addEnabled = (quoteSplit = product.Manufacturer)
                                    End If
                                End If

                                ' Set up the message to display if the user attempts to create a mixed quote
                                Dim splitMessage As String = String.Empty
                                If Not addEnabled Then
                                    splitMessage = GetSplitMessage(quoteSplit, buyeraccount.Language)
                                End If

                                Dim TB_QTY As New TextBox
                                TB_QTY = New TextBox
                                TB_QTY.ID = "qtytxt." & path$
                                TB_QTY.Attributes("style") = "left:5em;width:1.5em;margin-left:.5em;height .9em;margin-top:.05em;border:solid silver 1px;" 'did have float:left
                                If addEnabled Then
                                    TB_QTY.CssClass = "qty"
                                    TB_QTY.Attributes.Add("onmousedown", "burstBubble(event);")
                                Else
                                    TB_QTY.CssClass = "qtyDisabled"
                                    TB_QTY.ReadOnly = True
                                    TB_QTY.Attributes.Add("onmousedown", String.Format("burstBubble(event); displayAddMsg('{0}', '{1}');", TB_QTY.ID, splitMessage))
                                End If

                                If Not lowest.SKUVariant.Deleted Then
                                    UI.Controls.Add(TB_QTY)

                                    UI.Controls.Add(TreeAddButton(TB_QTY, path$, obj, lowest.SKUVariant, buyeraccount.Language, addEnabled, splitMessage))
                                End If
                            End If
                        End If
                        Value = (lowest.Price.NumericValue * 100).ToString
                    End If

                    If Value = Int64.MinValue Then
                        csv = Utility.CSV("No Price")
                    Else
                        If export = False Then
                            csv = Utility.CSV((Value / 100).ToString("N2"))  'format as a number to two decimal places
                        Else
                            csv = Utility.CSV(buyeraccount.Currency.Symbol & (Value / 100).ToString("N2"))  'format as a number to two decimal places
                        End If
                    End If

                Else
                    'SKUless product/row
                    lbl.Text = "NO SKU"
                    If UI IsNot Nothing Then UI.Controls.Add(lbl)
                    Value = Int64.MinValue 'Single.MinValue
                End If

                    Translation = Nothing

            Case Is = "stock"

                csv = ""  'fix' for 'unhandled specialcoumn stock'

                Dim product As clsProduct
                product = CType(obj, clsBranch).Product
                If product IsNot Nothing Then
                    If product.hasSKU Then
                        Dim Disp As String
                        If UI Is Nothing Then  'we're just fetching a numeric value (for sorting)

                            Dim stockvalue As Integer 'TODO -decide if we should sum these take a max or what
                            Disp = product.CurrentStock(buyeraccount, stockvalue, iq.AllVariants, errormessages)  'SETS (numeric) value - passing nothing totalises the stock of all variant
                            Value = stockvalue
                            csv = GetStock(buyeraccount, stockvalue, export)

                        Else

                            'add a asynch-refreshabe stock number..
                            Dim prices As List(Of clsPrice)
                            prices = product.GetPrices(buyeraccount, buyeraccount.SellerChannel.priceConfig, iq.AllVariants, errormessages, True)

                            If prices.Count > 0 AndAlso prices(0) IsNot Nothing Then
                                UI.Controls.Add(prices(0).SKUVariant.StockUI(1, String.Empty, buyeraccount.Language, buyeraccount.SellerChannel))
                                Dim stockvalue As Integer
                                product.CurrentStock(buyeraccount, stockvalue, iq.AllVariants, errormessages)  'populate the return value with the numeric stock
                                Value = stockvalue
                                csv = GetStock(buyeraccount, stockvalue, export)


                            Else
                                lbl.Text = "-"
                                Value = Int64.MinValue.ToString 'Single.MinValue
                                UI.Controls.Add(lbl)
                                csv = ""

                            End If
                        End If
                    Else
                        'non skud
                        lbl.Text = "-"
                        If UI IsNot Nothing Then
                            UI.Controls.Add(lbl)
                        End If
                        Value = Int64.MinValue.ToString 'Single.MinValue

                        csv = String.Empty
                    End If
                End If

                Translation = Nothing

            Case Is = "memory"  'Need to re-import to get this to work

                '    Dim segs As Integer = Split(path$, ".").Length 'how many segements were there in the path to this point (becuase we will look in the 'next' segment for a branch called 'memory')
                Dim preinstalled = GetPreinstalled(lid, path$, obj, buyeraccount, errormessages)

                Dim mem As Integer = 0
                Dim bn As String

                For Each i In preinstalled
                    bn = i.Branch.Translation.text(English)
                    If i.Branch.Product IsNot Nothing Then
                        If i.Branch.Product.ProductType.Code.ToUpper() = "MEM" Then
                            If i.Branch.Product.i_Attributes_Code.ContainsKey("capacity") Then
                                mem = mem + (i.NumPreInstalled) * CInt(i.Branch.Product.i_Attributes_Code("capacity")(0).NumericValue)  'the capcity attribute of the DIMM
                            End If
                        End If
                    End If
                Next

                lbl.Text = mem & " GB"
                If UI IsNot Nothing Then UI.Controls.Add(lbl)

                Value = mem.ToString
                csv = Utility.CSV(lbl.Text.ToString)

                Translation = Nothing
                unit = iq.i_unit_code("Gbyte")

            Case Is = "drives"

                'Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
                Dim preinstalled = GetPreinstalled(lid, path$, obj, buyeraccount, errormessages)
                Dim drives As Integer = 0
                For Each i In preinstalled

                    If i.Branch.Product.ProductType.Code.ToUpper() = "HDD" Then
                        drives += 1
                    End If
                Next

                lbl.Text = drives
                If UI IsNot Nothing Then UI.Controls.Add(lbl)

                Value = drives.ToString
                csv = drives.ToString

                Translation = Nothing
                'unit = iq.i_unit_code("Gbyte")

            Case Is = "drivecapacity"

                'Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
                Dim preinstalled = GetPreinstalled(lid, path$, obj, buyeraccount, errormessages)

                Dim driveCapacity As Decimal = 0
                For Each i In preinstalled

                    If i.Branch.Product.ProductType.Code.ToUpper() = "HDD" Then

                        If (i.Branch.Product.i_Attributes_Code.ContainsKey("capacity")) Then
                            driveCapacity += i.Branch.Product.i_Attributes_Code("capacity")(0).NumericValue
                        End If
                    End If
                Next

                If driveCapacity = 0 Then
                    'Go check in attributes and see if its there.....
                    If CType(obj, clsBranch).Product.i_Attributes_Code.ContainsKey("capacity") Then
                        driveCapacity = CType(obj, clsBranch).Product.i_Attributes_Code("capacity")(0).NumericValue
                    End If
                End If
                If driveCapacity = 0 Then lbl.Text = "NA" Else lbl.Text = driveCapacity & " GB"
                If UI IsNot Nothing Then UI.Controls.Add(lbl)

                Value = driveCapacity
                csv = Value

                Translation = Nothing


                unit = iq.i_unit_code("Gbyte")


            Case Is = "supplychain"
                If obj.parent IsNot Nothing Then
                    With CType(obj, clsBranch).Parent 'The supply chain is this branches parent (systems 'live' within supply chains)
                        lbl.Text = .DisplayName(buyeraccount.Language).ToString
                        If UI IsNot Nothing Then UI.Controls.Add(lbl)
                        ' numericValue = .Translation.SortValue(buyeraccount.Language)
                        Translation = .Translation
                        csv = Utility.CSV(Translation.text(language))
                    End With
                End If


            Case Is = "display"

                Dim tl As clsTranslation = CType(obj, clsBranch).Product.i_Attributes_Code("Display")(0).Translation
                lbl.Text = tl.text(buyeraccount.Language)
                If UI IsNot Nothing Then UI.Controls.Add(lbl)
                Value = "0" 'tl.SortValue(buyeraccount.Language)
                csv = Utility.CSV(tl.text(language))

                'Case Is = "cpuspeed"

                '    lbl.Text = "-"
                '    Dim product As clsProduct = obj.Product

                '    Dim cpu As clsProduct
                '    Dim cpusku$
                '    If product.i_attributes_code.containskey("cpuSKU") Then
                '        cpusku = product.i_attributes_code("cpuSKU").Translation.text(English)
                '        If iq.i_SKU.ContainsKey(cpusku) Then
                '            cpu = iq.i_SKU(cpusku)
                '            If cpu.i_attributes_code.containskey("speed") Then
                '                lbl.Text = cpu.i_attributes_code("speed").NumericValue
                '            End If
                '        End If

                '        MatrixUI.Controls.Add(lbl)

                '    End If
            Case Is = "operatingsystem"

                'Dim preinstalled As List(Of clsQuantity) = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errormessages)
                Dim preinstalled = GetPreinstalled(lid, path$, obj, buyeraccount, errormessages)
                Dim tl As String
                For Each i In preinstalled

                    If i.Branch.Product.ProductType.Code.ToUpper() = "SOF1" Then
                        Dim r As Regex = New Regex("(Windows [A-z|0-9| ]+ [Foundation]*[Standard]*[Datacenter]*[Essentials]*)[ ]+")
                        tl = i.Branch.DisplayName(buyeraccount.Language)
                        Dim m As Match = r.Match(tl)
                        If m.Groups.Count > 1 Then
                            tl = m.Groups(1).Value
                        End If
                    End If
                Next

                lbl.Text = tl
                If UI IsNot Nothing Then UI.Controls.Add(lbl)
                csv = Utility.CSV(tl)

                Translation = iq.AddTranslation(tl, buyeraccount.Language, "", 0, Nothing, 0, False)
                'unit = iq.i_unit_code("Gbyte")

            Case Is = "portcountorradio"
                Dim attrs = CType(obj, clsBranch).Product.i_Attributes_Code
                If attrs.ContainsKey("PriConnectivity") Then
                    If attrs("PriConnectivity").First.Translation.text(English).Contains("802.1") Then
                        lbl.Text = attrs("PriConnectivity").First.Translation.text(English)
                        Translation = attrs("PriConnectivity").First.Translation
                        csv = lbl.Text
                    Else
                        If attrs.ContainsKey("PriPorts") Then
                            lbl.Text = attrs("PriPorts").First.NumericValue.ToString()
                            Value = attrs("PriPorts").First.NumericValue
                            Translation = iq.AddTranslation(lbl.Text, English, "", 0, Nothing, 0, False)
                            csv = attrs("PriPorts").First.NumericValue
                        End If
                    End If
                    If UI IsNot Nothing Then UI.Controls.Add(lbl)
                End If
            Case Is = "formfactorcompressed"
                Dim attrs = CType(obj, clsBranch).Product.i_Attributes_Code
                If attrs.ContainsKey("formFactor") Then
                    If attrs("formFactor").First.Translation.text(English).ToLower.Contains("tower") Then
                        Translation = iq.AddTranslation("Tower", English, "", 0, Nothing, 0, False)
                    Else
                        Translation = attrs("formFactor").First.Translation
                    End If
                    lbl.Text = Translation.text(language)
                    csv = Translation.text(language)
                    If UI IsNot Nothing Then UI.Controls.Add(lbl)
                End If
            Case Else
                SpecialColumn = False 'this wasn't a 'Special' Column
                Translation = Nothing

        End Select

    End Function
    ''' <summary>
    ''' Gets stock quantity or message in stock or out of stock for binarystock channels.
    ''' </summary>
    ''' <param name="account">an instance of clsAccount.</param>
    ''' <param name="value">An integer value that represents the quantity of stock.</param>
    ''' <param name="export">A boolean value that represents if export is being done.</param>
    ''' <returns>A string object that represents the text or number to display in quote export.</returns>
    ''' <remarks></remarks>
    Private Function getStock(account As clsAccount, value As Int64, export As Boolean) As String
        Dim result As String = String.Empty
        If account.SellerChannel.BinaryStock And value > 0 Then
            result = InStock.text(account.Language)
        ElseIf account.SellerChannel.BinaryStock And value <= 0 Then
            result = OutOfStock.text(account.Language)
        ElseIf Not account.SellerChannel.BinaryStock And value > 0 Then
            result = value
        ElseIf Not account.SellerChannel.BinaryStock And value <= 0 Then
            If export Then
                result = "0"
            Else
                result = value.ToString
            End If
        End If
        Return result
    End Function

    Function GetPreinstalled(lid As UInt64, path As String, ByRef obj As Object, ByRef buyeraccount As clsAccount, ByRef errorMessages As List(Of String)) As List(Of clsQuantity)

        'Return New List(Of clsQuantity)  '@@@ This was a test to see how expensive GetPreinstalledRecursive is when creating many matrixheaders/detailsquares
        'the answer is .. not very (not more  than a 10 % improvments when i returned an empty list and did none of the work.
        'Persisting the preInstalls dictionary looked attractive - but any gains would be small



        If iq.sesh(lid, "preInstalls") Is Nothing Then
            iq.sesh(lid, "preInstalls") = New Dictionary(Of String, List(Of clsQuantity))()
        End If
        Dim preinstalledDic As Dictionary(Of String, List(Of clsQuantity)) = iq.sesh(lid, "preInstalls")
        If preinstalledDic.ContainsKey(path) Then
            GetPreinstalled = preinstalledDic(path)
        Else
            GetPreinstalled = CType(obj, clsBranch).GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errorMessages)
            preinstalledDic(path) = GetPreinstalled
        End If
    End Function

    Public Function EditUI(obj As Object, path As String, RowPanel As Panel, enabled As Boolean, Request As Web.HttpRequest, language As clsLanguage, buyerAccount As clsAccount, PageMode As Boolean, ByRef errorMessages As List(Of String), translateLanguage As clsLanguage) As Panel

        EditUI = Me.emptyCell(Not Me.visibleList, True) 'New Panel - has an absolute width as defined in the field (ie. me.width - in ems)
        ' EditUI.Attributes("style") &= "overflow:visible;display:inline-block;" 'otherwise we don't see the dropdowns wich are absolutely positioned (taking them out of the flow)

        'returns the UI element for editing this fields property  of the supplied OBJ, using the interface defined in the field F
        'the 'enabled' flag enables the element - and is used for history at the moment (but will be useful for role/right stuff)
        'F may contain some straight propert of obj
        'e.g. DisplayName
        'or some derived property such as attributes(17).name

        Dim tb As TextBox
        Dim ddl As Panel 'DropDownList

        Dim em As UnitType
        em = UnitType.Em

        Select Case LCase(Me.InputType.code)

            Case "string", "int32", "single", "translate", "nullstring", "nullint", "nullprice"
                'simple textbox

                tb = New TextBox
                EditUI.Controls.Add(tb)

                tb.Style("width") = CStr(Me.width - 0.5) & "em"
                tb.Style("height") = "100%"  'for textAreas in paged mode
                tb.Style("text-align") = "top"
                tb.Style("display") = "inline-block"

                If PageMode Then
                    If Me.height > 2 Then
                        tb.TextMode = TextBoxMode.MultiLine
                    End If
                End If

                If Me.InputType.code = "translate" Then

                    Dim tobj As clsTranslation

                    tobj = CType(Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages), clsTranslation)
                    If tobj Is Nothing Then
                        tb.BackColor = Drawing.Color.Pink
                        tb.Text = ""
                        tb.ToolTip = Xlt("Missing text", language)
                    Else
                        If translateLanguage.Code IsNot Nothing Then
                            Dim lbl As Label = New Label
                            EditUI.Controls.Add(lbl)
                            lbl.Style("width") = CStr(Me.width - 0.5) & "em"
                            lbl.Style("height") = "100%"  'for textAreas in paged mode
                            lbl.Style("text-align") = "top"
                            lbl.Style("display") = "inline-block"
                            lbl.Text = tobj.text(language)
                            If tobj.textTranslation(translateLanguage).Length > 1 Then
                                tb.Text = tobj.textTranslation(translateLanguage)
                            Else
                                tb.BackColor = Drawing.Color.Pink
                                tb.Text = ""
                                tb.ToolTip = Xlt("Missing text", language)
                            End If
                        Else
                            tb.Text = tobj.text(language)
                        End If
                        'Add a button for a new translation
                        EditUI.Controls.Add(editor.MakeButton(True, "Nt", "New translation", "createNewTranslation('" & path & "','" & Me.propertyName & "');"))

                        '                  tb.BackColor = Drawing.Color.CornflowerBlue 'remove
                    End If

                ElseIf Me.InputType.code = "nullstring" Then
                    Dim ns As nullableString
                    ns = CType(Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages), nullableString)
                    tb.Text = ns.DisplayValue

                ElseIf Me.InputType.code = "nullint" Then
                    Dim ni As NullableInt
                    ni = CType(Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages), NullableInt)
                    tb.Text = ni.Displayvalue
                ElseIf Me.InputType.code = "nullprice" Then  'Nullable price
                    Dim np As NullablePrice
                    np = CType(Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages), NullablePrice)

                    'every price has a currency - this provides the currency symbol - however the number formatting is determined by buyers,channels, culture 
                    tb.Text = np.NumericValue.ToString  'DisplayPrice(buyerAccount, errorMessages).Text  'currency formatting is is the culture of the seller (the currency alone doesn't give us suffifient info at euro is pan Eueropean - but NL formats €1.000,00 and IE formats €1,000.00
                Else
                    'straight' text
                    Dim ao As Object = Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages)
                    If ao Is Nothing Then
                        tb.Text = ""
                    Else
                        tb.Text = ao.ToString
                    End If
                End If

                '    If LCase(f.propertyName) = "name" Then title = tb.Text
                tb.ID = "c_" & Trim$(Me.ID.ToString) & "_" & Trim$(obj.id.ToString)
                tb.CssClass = "input"  'This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS) - input carries no styling (neccessarily)
                tb.Enabled = enabled

                '      tb.ToolTip = tb.ID 'remove
                '        tb.Style("background-color") = "green"

                validationScript = ""
                If Not Me.Validation Is Nothing Then
                    ' passes a regEx and length to the JS to validate 
                    ' validate will disable all controls with a 'save' class, 
                    ' and make the textbox 'invalid' class (if it's invalid)
                    Dim msg$
                    msg$ = Me.Validation.ViolationMessage
                    msg$ = Replace(msg$, "'", "")

                    'must escape the backslashes in regex's (we're creating dynamically)
                    validationScript = "validate('" & Replace(Me.Validation.regEx, "\", "\\") & "','" & msg$ & "','" & tb.ID & "');"

                End If

                If Me.length > 0 Then
                    validationScript &= "validateLength(" & Me.length & ",'" & tb.ID & "');"
                End If

                tb.Attributes.Add("onKeyUp", validationScript)


            Case "boolean"
                Dim cb As WebControls.CheckBox
                cb = New CheckBox
                EditUI.Controls.Add(cb)
                'cb.Attributes("style") = "width:" & me.width & "em;"

                cb.Checked = CType(Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages), Boolean)

                '              \/ Note - checkboxes have different handing in the JS because (stupidly) they have a 'checked' property - and their "value" is always "on"
                cb.ID = "cb_" & Trim$(Me.ID.ToString) & "_" & Trim(obj.id.ToString)
                '       cb.CssClass = "input"  'This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS)
                cb.InputAttributes.Add("class", "input") 'the above doesn't work - becuase .NET takes it upon itseld to render the checkbox without the the class in a <span> with the class

                cb.Enabled = enabled

            Case Is = "one"
                Dim targetObj As Object
                targetObj = Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages) 'this object is the 'target' of the foregin key - and contains the selected value 

                Dim controlID As String
                controlID = "c_" & Trim(Me.ID.ToString) & "_" & obj.id

                ddl = FilledDDL(Me, targetObj, language, controlID, enabled, RowPanel, Request("depth"), errorMessages) 'Obj2 carries the ID
                EditUI.Controls.Add(ddl)

            Case Is = "many" 'this field holds a collection of things (a dictionary) - we render a button - which will embed editing for that dictionary

                'btn = New Button
                'btn.Style("width") = "100%"

                Dim dic As Object = Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages)
                Dim txt As String
                If dic Is Nothing Then
                    txt = "Add " & Me.labelText.text(language) 'propertyName
                Else
                    txt = dic.count.ToString & " " & Me.labelText.text(language) 'propertyName
                End If

                Dim td$ = path$ & "." & Me.propertyName ', subPanel.ID
                EditUI.Controls.Add(editor.MakeButton(True, txt, "Show/edit these", editor.EmbedScript(td, td, "", True, True)))

                Dim tp As Panel = New Panel : tp.ID = td$
                RowPanel.Controls.Add(tp)




                'btn.CssClass = "input"
                'btn.ID = "c_" & Trim$(Me.ID.ToString) & "_" & Trim$(obj.id.ToString)

                ''Dim descendantPanel = EmptyPanel(inPanel.ID & "." & Me.propertyName) '& "(" & obj.id & ")")
                ''inPanel.Controls.Add(descendantPanel) 'note, there will be one child panel for each 'many' field (editable dictionary)


                'If enabled Then
                '    'depth request(depth+1)

                '    btn.Attributes("onclick") = editor.EmbedScript(path$ & "." & Me.propertyName, subpanel, "depth," & Request("depth") + 1, False, False)
                '    btn.ToolTip = btn.Attributes("onclick")
                '    btn.Enabled = True
                'Else
                '    btn.Enabled = False
                'End If

                'If obj.id = -1 Then btn.Enabled = False : btn.ToolTip = "You must save before you can attach items"
                'EditUI.Controls.Add(btn)

            Case Is = "date"

                Dim txtbox As TextBox
                txtbox = New TextBox
                txtbox.ID = "c_" & Trim$(Me.ID.ToString) & "_" & Trim(obj.id.ToString)
                'This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS)
                txtbox.CssClass = "input"
                txtbox.Style.Add("width", "55%")

                'txtbox.Width = New Unit(f.width / 3 * 2, ut)

                Dim dt As Date
                dt = CType(Reflection.WalkPropertyValue(obj, Me.propertyName, errorMessages), Date)
                txtbox.Text = Format(dt, "yy-MM-dd")
                txtbox.Enabled = enabled

                Dim timebox As TextBox
                timebox = New TextBox
                timebox.Text = Format(dt, "HH:mm")
                timebox.ID = "c2_" & Trim$(Me.ID.ToString) & "_" & Trim$(obj.id.ToString)
                'This is vital - controls with the class of Input are those that carry data (and are manipulated by the JS)
                timebox.CssClass = "input"
                timebox.Style.Add("width", "38%")
                'timebox.Width = New Unit(f.width / 3, ut)

                timebox.Enabled = enabled

                'attach to an onload event of an image
                Dim img As New Image
                img.ImageUrl = eim$ & "resort.png"
                img.Attributes.Add("onload", "$(function() {$( ""#" & txtbox.ID & """ ).datepicker({ dateFormat: ""yy-mm-dd"" });});")
                '   img.Attributes.Add("onload", "$(function() {$( ""#" & txtbox.ID & """ ).datepicker({ dateFormat: ""yy-mm-dd"" });}$( ""#" & txtbox.ID & """ ).datepicker( ""setDate"" , '" & txtbox.Text & "' ));")

                img.Width = 1
                img.Height = 1
                EditUI.Controls.Add(img)

                EditUI.Controls.Add(txtbox)
                EditUI.Controls.Add(timebox)

                'Case Is = "customerprice"

                ' Dim product As clsProduct
                ' product = obj

                ' Dim lbl As New Label
                '    EditUI.Controls.Add(lbl)
                '    lbl.Text = product.GetPrices(buyerAccount, iq.StandardVariant)(0).Price.DisplayPrice.Text

            Case Else
                Beep()

        End Select



    End Function


    Public Sub promote(errorMessages As List(Of String))  '' DECREASES the [order] of a column (moving it left)

        Dim sf As SortedDictionary(Of Integer, clsField) = New SortedDictionary(Of Integer, clsField)

        For Each f In Me.Screen.Fields.Values
            sf.Add(f.order, f)
        Next

        'swap this fields order - with the one of the field before it
        Dim pf As clsField = Nothing 'previous field
        Dim pfo As Integer 'previous fields order (we need a 'spare' variable to perform the swap)

        For Each f In sf
            If f.Value Is Me Then
                If Not pf Is Nothing Then

                    pfo = pf.order
                    pf.order = f.Value.order
                    f.Value.order = pfo
                    f.Value.update(errorMessages)
                    pf.update(errorMessages)

                    Exit For
                End If
            End If
            pf = f.Value
        Next


    End Sub
    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$
        sql$ = "UPDATE [field] set "
        sql$ &= "fk_screen_id=" & Me.Screen.ID
        sql$ &= ",property=" & da.SqlEncode(Me.propertyName)
        'sql$ &= ",propertyClass=" & da.SqlEncode(Me.PropertyClass)
        sql$ &= ",fk_translation_key_label=" & Me.labelText.Key
        sql$ &= ",helptext=" & da.SqlEncode(Me.helpText)
        Dim vid As String
        If Me.Validation Is Nothing Then
            vid = "null"
        Else
            vid = Me.Validation.ID
        End If
        sql$ &= ",fk_validation_id=" & vid
        sql$ &= ",lookupof=" & da.SqlEncode(Me.lookupOf)
        sql$ &= ",fk_inputtype_id=" & Me.InputType.ID
        sql$ &= ",length=" & Me.length
        sql$ &= ",[order]=" & Me.order
        '    sql$ &= ",fk_screen_id_embed=" & NullID(Me.EmbedScreen)
        sql$ &= ",[width]=" & Me.width
        sql$ &= ",[height]=" & Me.height
        sql$ &= ",defaultvalue=" & da.SqlEncode(Me.defaultValue)
        sql$ &= ",visibleList=" & IIf(Me.visibleList, "1", "0").ToString
        sql$ &= ",visiblePage=" & IIf(Me.visiblePage, "1", "0").ToString
        sql$ &= ",defaultfilter=" & da.SqlEncode(Me.defaultFilter)
        sql$ &= ",defaultsort=" & da.SqlEncode(Me.defaultSort)
        sql$ &= ",priority=" & Me.priority
        sql$ &= ",fk_translation_key_widgetGroup=" & TranslationKey(Me.QuickFilterGroup) 'NB: this is a clsTranslation (there's an overload for sqlEncode)
        sql$ &= ",widgetUI=" & da.SqlEncode(Me.QuickFilterUItype)
        sql$ &= ",Grows=" & da.SqlEncode(Me.Grow)
        sql$ &= ",DefaultFilterValues=" & da.SqlEncode(Me.DefaultFilterValues)


        sql$ &= " WHERE ID=" & Me.ID

        da.DBExecutesql(sql$, False)


        Me.Screen.i_field_property.Remove(oPropertyName)
        Me.Screen.i_field_property.Add(Me.propertyName, Me)

        If Not Me.Screen.Fields.ContainsKey(Me.ID) Then Me.Screen.Fields.Add(Me.ID, Me) 'This is for when we've added one (using the New button)

        oPropertyName = propertyName


    End Sub

    Public Function LooksUp() As String

        Dim lu() As String
        lu = Split(Me.lookupOf, "(")
        Return lu(0)

    End Function

    Public Sub Delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Try
            Dim sql$
            sql$ = "DELETE FROM [field] WHERE ID=" & Me.ID

            da.DBExecutesql(sql$)

            Me.Screen.Fields.Remove(Me.ID)
            Me.Screen.i_field_property.Remove(oPropertyName)


        Catch ex As System.Exception
            errorMessages.Add("unable to delete " & ex.Message)
        End Try


    End Sub


    Public Function displayName(Language As clsLanguage) As String Implements i_Editable.displayName

        Return Me.propertyName

    End Function




End Class

