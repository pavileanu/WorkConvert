Option Strict On

Imports System.Web.UI.DataVisualization.Charting
Imports dataAccess
Imports System.Xml.Serialization
Imports System.IO

Public Class clsPriorityDirection
    <XmlIgnore>
    Public _column As clsField
    <XmlIgnore>
    Public Property column As clsField
        Get
            If _column Is Nothing AndAlso columnid <> 0 Then _column = iq.Fields(columnid)
            Return _column
        End Get
        Set(value As clsField)
            _column = value
        End Set
    End Property
    Public Priority As Integer  '1 is the highest priority (5 is lower)
    Public Direction As String  'ASC DESC

    Public columnid As Integer 'for serialization

    Public Sub New() 'for serialization

    End Sub
    Public Sub New(column As clsField, Priority As Integer, direction As String)

        Me.column = column
        Me.Priority = Priority
        Me.Direction = direction

    End Sub
    Public Sub New(l$) 'Special constructor makes one from something like 283,2D

        Dim bits() As String = Split(l$, ",")
        Me.column = iq.Fields(CInt(bits(0)))

        Dim pd As String = bits(1)
        Me.Priority = CInt(Left(pd, 1))
        Me.Direction = "A"
        If UCase(Right(pd, 1)) = "D" Then Me.Direction = "D"

    End Sub

    'Public Sub New(field As clsField, PD As String)
    '    Me.column = field
    '    Me.Priority = CInt(Left(PD, 1))
    '    Me.Direction = "ASC"
    '    If UCase(Right(PD, 1)) = "D" Then Me.Direction = "DESC"
    'End Sub

    Public Function UI(path$, language As clsLanguage) As Panel
        'returns an arrow button for flipping the sort order of this column from ascending to descending
        UI = New Panel
        UI.CssClass = "sort_" & Me.Direction & " pri_" & Me.Priority & " sortArrow"

        Dim lit As New Literal
        If Me.Direction = "A" Then
            UI.Attributes("onmousedown") = "getBranches('cmd=sort&path=" & path & "&colID=" & Me.column.ID & "&priority=" & Me.Priority & "&direction=D');" ' + sortFieldID + ',' + v);"
            lit.Text = "&nbsp;"  'the column shows an indication of how you are
            UI.Controls.Add(lit)
            UI.ToolTip = Xlt("switch to descending", language)
        ElseIf Me.Direction = "D" Then
            'It's currently Descending

            UI.Attributes("onmousedown") = "getBranches('cmd=sort&path=" & path & "&colID=" & Me.column.ID & "&priority=" & Me.Priority & "&direction=A');" ' + sortFieldID + ',' + v);"
            lit.Text = "&nbsp;" 'the column shows an indication of how you are
            UI.Controls.Add(lit)
            UI.ToolTip = Xlt("switch to ascending", language)
        Else
            Beep()
        End If

    End Function

End Class


Public Class clsScreen
    Implements i_Editable

    Public Property ID As Integer
    Public Property code As String
    Public Property title As String '(title)
    Public Property Obj As String 'what class of object does this screen edit  - NB: there is no reference to *which dictionary* this screen maintains, becuase it can maintain many (often the 'children' collections of objects
    Public Property Fields As Dictionary(Of Integer, clsField)

    Public i_field_property As Dictionary(Of String, clsField) ' which fied represents which property

    ' Public Property DicName As String '
    Public Auditable As Boolean 'Set during makescreen if the object has an AuditRoot property 

    Dim oCode As String
    Dim oTitle As String

    'Automatically collapses columns that won't fit in the available screen space (based on their priority)
    'priority is not necessarily the same as order - as you may want a high priorty column (such as price) to appear at the end of a row
    'Priority '1' is the highest priority


    Public Sub New()
        'the editor requires a parameterless constructor

    End Sub

    Public Function TotalWidth() As Single

        TotalWidth = 0
        For Each f In Fields.Values
            If f.visibleList Then
                TotalWidth += +f.width
            Else
                TotalWidth += collapsedColumnWidth
            End If
        Next

        TotalWidth += 3 'some space for margins gutters etc

    End Function


    Public Function displayName(langauge As clsLanguage) As String Implements i_Editable.displayName
        Return title & " (" & Obj & ")"
    End Function

    Public Function copy(ByRef errormessages As List(Of String)) As clsScreen

        copy = New clsScreen(Me.Obj, Me.code & "_2", "copy of " & Me.title, errormessages)

        Dim afield As clsField
        For Each f In Me.Fields.Values
            afield = New clsField(copy, f.propertyName, f.lookupOf, f.labelText, f.helpText, f.Validation, f.InputType, f.length, f.order, f.width, f.height, f.defaultValue, f.visibleList, f.visiblePage, f.defaultFilter, f.defaultSort, f.priority, f.QuickFilterGroup, f.QuickFilterUItype, f.CanUserSelect, f.LinkedFieldID, f.FilterVisible)
        Next

    End Function

    Public Function DefaultSorts() As Dictionary(Of Integer, clsPriorityDirection)

        DefaultSorts = New Dictionary(Of Integer, clsPriorityDirection)
        For Each f In Me.Fields.Values
            If f.defaultSort <> "" Then
                Dim dso As New clsPriorityDirection(f, CInt(Left(f.defaultSort, 1)), Right(f.defaultSort, 1))
                If Not DefaultSorts.ContainsKey(dso.Priority) Then  ' using a dictionary ensures that the priorites are unique - but we don't want to crash and burn if there's a dupe
                    DefaultSorts.Add(dso.Priority, dso)
                End If
            End If
        Next

    End Function


    Public Sub New(id As Integer, code As String, obj As String, Title As String)

        Me.ID = id
        Me.Obj = obj
        Me.code = code

        Me.title = Title
        '  Me.DicName = dicname
        Me.Fields = New Dictionary(Of Integer, clsField)
        iq.Screens.Add(Me.ID, Me)
        If Not iq.i_screens_title.ContainsKey(Me.title) Then
            iq.i_screens_title.Add(Me.title, Me)
        Else
            '  Beep()
        End If
        'iq.I_SCREENS_TYPE.Add(Me.code, Me)
        If Not iq.i_screens_code.ContainsKey(code) Then
            If code <> "" Then iq.i_screens_code.Add(code, Me)
        End If

        Me.i_field_property = New Dictionary(Of String, clsField)

        Me.oCode = Me.code
        Me.oTitle = Me.title

    End Sub

    Public Function EditTitles(EditHeader As clsEditHeader, language As clsLanguage) As Panel

        EditTitles = New Panel 'Labels are positioned absolutely within this panel
        EditTitles.CssClass = "editHeaderLabels" '"innerEditHeader"


        Dim vPath = HttpContext.Current.Request.ApplicationPath
        Dim pPath = HttpContext.Current.Request.MapPath(vPath)

        '        Dim img As Image

        '        Dim fields = (From f In Me.Fields.Values Where f.visibleList = True Order By f.order)
        Dim fields = (From f In Me.Fields.Values Order By f.order)

        EditTitles.Controls.Add(NewLit("<div class='efcSpacer'></div>"))
        For Each f In fields  'order by order

            Dim c As Panel = f.emptyCell(Not f.visibleList, True)

            EditTitles.Controls.Add(c)
            If f.visibleList Then
                Dim lbl As Label = New Label
                lbl.Text = f.displayName(language) 'displayName(language)
                c.Controls.Add(lbl)
            End If
        Next

        '' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"

    End Function

    Public Function MatrixHeaders(MatrixHeader As clsScreenHeader, bi As clsBranchInfo, language As clsLanguage) As Panel

        MatrixHeaders = New Panel() 'Labels are positioned absolutely within this panel
        MatrixHeaders.CssClass = "innerMatrixHeader"
        MatrixHeaders.ID = "innerMatrixHeader"

        Dim mychart As Chart

        'Dim matrix As clsScreen
        '        matrix = iq.Screens(Chart.Attributes("MatrixID"))
        'If Not matrix Is Nothing Then

        Dim x As Single '!! f*cked me over for 20 minutes

        x = 0  ' We start 2 ems accross - leaving room for the contrast/shortlist checkboxes

        Dim vPath = HttpContext.Current.Request.ApplicationPath
        Dim pPath = HttpContext.Current.Request.MapPath(vPath)

        ' Start further left on the options screens in basic user mode as no expand/collapse control is displayed
        If Not UserIsAdmin(bi.lid) AndAlso (bi.branch.rca.StartsWith("GTB") Or bi.branch.rca.Equals("G")) Then
            MatrixHeaders.Controls.Add(NewLit("<div class='LeftPad' style='display:inline-block;width:0.5em;'>&nbsp;</div>"))
        Else
            MatrixHeaders.Controls.Add(NewLit("<div class='LeftPad' style='display:inline-block;width:4.25em;'>&nbsp;</div>"))
        End If

        Dim img As Image

        'Dim fields = (From f In Me.Fields.Values Where f.visibleList = True Order By f.order)
        For Each f In MatrixHeader.EffectiveFields  'order by order
            Dim filename As String = f.labelText.text(language).Replace("/", "") & ".png"
            For Each c In IO.Path.GetInvalidFileNameChars
                filename = filename.Replace(c, "")
            Next
            Dim filePath As String = pPath & "\matrixLabels\" & filename

            If Not My.Computer.FileSystem.FileExists(filePath) Then
                mychart = New Chart
                '  mychart.BackGradientStyle = GradientStyle.TopBottom
                '  mychart.BackSecondaryColor = Drawing.Color.FromArgb(255, 220, 220, 255)

                mychart.Width = 100
                mychart.Height = 100

                mychart.BackColor = System.Drawing.Color.Transparent
                mychart.AntiAliasing = AntiAliasingStyles.All

                'mychart.Attributes.Add("MatrixID", Me.ID)
                mychart.Attributes.Add("Text", f.labelText.text(language))
                AddHandler mychart.PostPaint, AddressOf postpaint  'Renders the diagonal labels
                '     matrixHeader.Controls.Add(mychart)

                mychart.ToolTip = f.helpText
                '    mychart.Attributes("Style") = "position:absolute;left:" & x & "em;"
                mychart.Attributes("Style") = "width:" + MatrixHeader.FieldResultSet(f).GrownWidth.ToString() + "em;"
                mychart.SaveImage(filePath)
            End If

            Dim ph As Panel = New Panel()
            If MatrixHeader.ColIsCollapsed(f) Then
                ph.Width = New Unit(collapsedColumnWidth, UnitType.Em)
            Else
                ph.Width = New Unit(MatrixHeader.FieldResultSet(f).GrownWidth, UnitType.Em)
            End If


            img = New Image
            img.ID = f.ID.ToString()
            img.Width = Unit.Pixel(100)
            img.Height = Unit.Pixel(100)
            img.ImageUrl = "../matrixlabels/" & filename
            'img.Attributes("Style") = "position:relative;left:" & x & "em;"
            ph.Style("display") = "inline-block"
            ph.Controls.Add(img)
            MatrixHeaders.Controls.Add(ph)

            If MatrixHeader.ColIsCollapsed(f) Then
                x = x + collapsedColumnWidth
            Else
                x = x + MatrixHeader.FieldResultSet(f).GrownWidth 'in ems
            End If
        Next

        '' this has been moved onto the containg div - matrixHeader.CssClass = "matrixHeader"

    End Function



    Public Sub New(obj As String, Code As String, Title As String, errormessages As List(Of String))

        Dim sql$

        sql$ = "INSERT INTO [Screen] (object, code, title) values (" & da.SqlEncode(obj) & "," & da.SqlEncode(Code) & "," & da.SqlEncode$(Title) & ");"
        Me.ID = da.DBExecutesql(sql$, True)
        Me.Obj = obj
        Me.code = Code
        Me.title = Title
        ' Me.DicName = dicname

        Me.Fields = New Dictionary(Of Integer, clsField)
        iq.Screens.Add(Me.ID, Me)
        If iq.i_screens_title.ContainsKey(Title) Then
            errormessages.Add("There is more than one screen witht the title '" & Title & "'")
        Else
            iq.i_screens_title.Add(Me.title, Me)
        End If


        Me.i_field_property = New Dictionary(Of String, clsField)

        If Code <> "" Then
            If iq.i_screens_code.ContainsKey(Code) Then
                errormessages.Add("screen code '" & Code & "' is not unique !")
            Else
                iq.i_screens_code.Add(Code, Me)
            End If
        End If

        Me.oCode = Me.code
        Me.oTitle = Me.title

    End Sub

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsScreen(Me.Obj, Me.code, Me.title, errormessages)

    End Function

    Public Sub Update(ByRef errormessages As List(Of String)) Implements i_Editable.update

        Dim sql$
        sql$ = "UPDATE [screen] set object =" & da.SqlEncode(Me.Obj) & ",code=" & da.SqlEncode(Me.code) & ",title=" & da.SqlEncode(Me.title) & " WHERE ID=" & Me.ID

        iq.i_screens_title.Remove(oTitle) : iq.i_screens_title.Add(Me.title, Me) : Me.oTitle = Me.title
        iq.i_screens_code.Remove(oCode) : iq.i_screens_code.Add(Me.code, Me) : Me.oCode = Me.code


        da.DBExecutesql(sql$, False)

    End Sub

    Public Sub Delete(ByRef errormessages As List(Of String)) Implements i_Editable.delete


        Try
            'Kill all the fields with one delete (instead of deleting them individually)
            Dim sql$
            sql$ = "Delete FROM [Field] WHERE [FK_Screen_ID]=" & Me.ID
            da.DBExecutesql(sql$)

            For Each field In Me.Fields.Values
                iq.Fields.Remove(field.ID)
            Next

            iq.i_screens_title.Remove(Me.title)
            iq.i_screens_code.Remove(Me.code)
            iq.Screens.Remove(Me.ID)

            sql$ = "Delete FROM [screen] WHERE id=" & Me.ID
            da.DBExecutesql(sql$)



        Catch ex As System.Exception
            errormessages.Add(ex.Message)

        End Try


    End Sub

    

    Public Function MatrixRow(obj As Object, bi As clsBranchInfo, ByRef errorMessages As List(Of String), United As Boolean) As PlaceHolder
        'Obj would usually be a Product (but in theory - doesn't have to be one)

        Dim em As UnitType
        em = UnitType.Em 'Percentage

        Dim pnl As PlaceHolder 'Panel
        pnl = New PlaceHolder 'Panel

        Dim cell As Panel
        Dim script$ = ""

        'Note, the expand collapse butotn is acutally outside the matrix row

        Dim cb As Literal 'CheckBox

        'add the 'contrast/compare/shortlist column (of checkboxes)
        cell = New Panel
        cell.CssClass = "matrixCell hideOverflow"
        cell.Attributes("style") = "position:relative;width:1.5em;"
        ' col.Attributes("onmouseover") = "if(!lockedFbs){this.appendChild(ctl00_filterButtons);showFilterButtons('ctl00_btnContrast');};return false;"

        pnl.Controls.Add(cell)

        ' cell.Controls.Add(bi.Branch.PromoIndicators(bi))

        'compare/constrast/scales function - removed for now
        If False Then
            cb = New Literal 'CheckBox - DO NOT attempt to use checkbox controls !  .NET has a horrible habbit of wrapping checkboxes in span tags - and the complications that causes are not worth dealing with - so we use literals
            cb.Text = "<input type='checkbox'  class='sl' id='sl_" & bi.path$ & "'/>"  ' we need the full path to each branch we're comparing - as paths are required to evaluate (preinstalled) quanitites (in contrast.aspx).. they cant' be derived from just the branch ID's
            cell.Controls.Add(cb)
        End If

        Dim x As Single
        'use LINQ to order by order
        ' Dim fields = (From f In Me.Fields.Values Order By f.order)

        'x = 4
        Dim useHeader As clsScreenHeader
        If United Then
            Dim screenHeaders = CType(iq.sesh(bi.lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
            useHeader = screenHeaders(bi.rootPath)
        Else
            useHeader = bi.EffectiveHeader
        End If

        For Each f In useHeader.EffectiveFields ' screen.Fields.Values

            'If f.visibleList Then
            'matricpath was headerpath 
            Dim collapsed As Boolean = useHeader.ColIsCollapsed(f) '.isCollapsedAt(bi.lid, bi.MatrixHeader.pathEffectiveMatrixPath)
            cell = f.CellUI(obj, bi.path, bi.buyerAccount, bi.agentAccount.Language, useHeader, collapsed, bi.foci, errorMessages, bi.lid, useHeader.FieldResultSet(f).GrownWidth)  'adds the main UI element (dropdown, textbox, calendar tickbox etc) 

            ' cell.Style.Add("whitespace", "nowrap") - failed attemp to stop wordwrap in cells
            cell.Style.Add("position", "relative")  'positioning the elements explicitly is the only way (i could find) to stop them wrapping (they are inline-block, I tried whitespace:no wrap - and othe 'solutions' to no avail)
            '  cell.Style.Add("left", x & "em")
            pnl.Controls.Add(cell)

            If collapsed Then
                x = x + collapsedColumnWidth
            Else
                x = x + useHeader.FieldResultSet(f).GrownWidth
            End If

            '        script$ &= f.validationScript

            ' pnl.Controls.Add(Gutter) 'the gap between columns - a sperate div because of issues with em's not geing consistent accross input boxes and the header columns
            'Else
            'hidden column (set to 1 em so it can be reinstated via it's show button)
            'col = New Panel
            'col.Width = New Unit(1, em)
            'lit = New Literal
            'lit.Text = "&nbsp;" ' we have to put something in the column - or it's not rendered !
            'col.Controls.Add(lit)
            'pnl.Controls.Add(col)
            ' End If
        Next

        '   Dim clear As Panel
        '   clear = New Panel
        '   clear.Style.Add("clear", "both")
        '   pnl.Controls.Add(clear)

        Return pnl

    End Function


End Class


