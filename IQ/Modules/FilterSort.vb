
Imports System.Web.Caching
Imports System.Globalization

Module FilterSort


    Public Function ContrastSpacer() As Panel

        'contrast spacer
        Dim pnl As Panel = New Panel
        pnl.CssClass = "matrixCell"
        pnl.Attributes("Style") = "width:3em;"

        Dim lit As Literal = New Literal  'we have to have *something* in the panel - or it isn't rendered
        lit.Text = "&nbsp;"
        pnl.Controls.Add(lit)

        Return pnl

    End Function



    ''' <summary>
    ''' The sub datatable has the Branch (or object) ID's - Filtering and Sorting are achieved with Dataviews onto a datatables which carry extracted Int64 numeric data
    ''' </summary>
    ''' <param name="bi"></param>
    ''' <param name="dic"></param>
    ''' <param name="errors"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>


    Private Function FilterFieldIDs(filter As String) As List(Of Integer)

        'Note filter$ is a module level variable containing 
        'a ^ (circumflex)  delimited list of filters each of which has 3 | (pipe) delimited segements,  FieldID|Operator|Value
        'ultimately the filter as applied to the dataview

        FilterFieldIDs = Nothing
        If filter <> "" Then
            FilterFieldIDs = New List(Of Integer)

            Dim f As List(Of String) = Split$(filter, "^").ToList
            For Each i In f
                If Split(i, "|").Count = 3 Then  'only return those filters that have an operand
                    FilterFieldIDs.Add(Split(i, "|")(0))
                End If
            Next
        End If

    End Function


    Public Function Gutter() As Panel

        Gutter = New Panel
        Gutter.Style("width") = ".75em"
        Gutter.Style("float") = "left"
        Dim lit As Literal = New Literal
        lit.Text = "&nbsp;" ' we have to put *something* in a div.. or it isn't rendered by ASP.NET
        Gutter.Controls.Add(lit)

    End Function

    Public Function FilledDDL(f As clsField, selectedValue As Object, language As clsLanguage, controlid As String, enabled As Boolean, rowPanel As Panel, depth As Integer, ByRef errorMessages As List(Of String)) As Panel ' DropDownList

        'Returns a panel conaining a TextBox and a filled DropDown list with script for autosuggest attached

        FilledDDL = New Panel

        FilledDDL.Attributes("style") = "overflow:visible;display:inline-block;"  'Overflow so the DDL can hang oout of the div

        Dim TypedTxt As TextBox = New TextBox 'caries the selected/typed text
        Dim txtObjID As New TextBox  'carries the ID of the selected item

        With TypedTxt
            .ID = controlid & "_txt"
            .Enabled = enabled
            .Style("background-color") = "#a0a0ff" 'light blue
            .CssClass = "TypedText"
            'INPUT elements do not behave well with inline-block
            .Attributes("Style") &= " Width:" & CStr(f.width - 2) & "em;"  'leave room for the buttons - there is no good way yo do this - this is the best way i can find  http://coding.smashingmagazine.com/2013/02/27/css-form-elements-problem/             
        End With

        With txtObjID
            .ID = controlid
            '.CssClass = "SelectedID input" 'The input class is vital it tags it a a data carrier
            .CssClass = "input" 'The input class is vital it tags it a a data carrier
            .Attributes("Style") = "display:none" 'this hidden ctextBox carries the ID of the selected item
        End With

        FilledDDL.Controls.Add(TypedTxt)
        FilledDDL.Controls.Add(txtObjID) 'invisible

        'Edit this list button
        If rowPanel IsNot Nothing Then
            Dim Elpnl As Panel = New Panel : rowPanel.Controls.Add(Elpnl) : Elpnl.ID = f.lookupOf
            FilledDDL.Controls.Add(editor.MakeButton(True, "El", "Edit this list", editor.EmbedScript(f.lookupOf, f.lookupOf, "" & depth + 1, False, True)))


            'Edit the target button
            Dim etpnl As Panel = New Panel : rowPanel.Controls.Add(etpnl) : etpnl.ID = f.lookupOf
            If selectedValue IsNot Nothing Then
                etpnl.ID = f.lookupOf & "(" & selectedValue.id & ")"
                Dim targetPath$ = f.lookupOf & "(" & selectedValue.id & ")"
                FilledDDL.Controls.Add(editor.MakeButton(True, "Et", "Edit the target " & f.labelText.text(language), editor.EmbedScript(targetPath, targetPath, "", False, True)))
            End If

        End If

        Dim ddh As Panel = New Panel 'Drop down holder.. (for the actual DDL)
        ddh.CssClass = "dropdownHolder"
        FilledDDL.Controls.Add(ddh)

        'ddh.Attributes("style") = "overflow:visible;min-height:100px;"
        'ddh.Attributes("z-index") = 100
        ddh.ID = "ddh_" & controlid

        Dim dic As Object = Nothing
        Dim bits() As String = Split(f.lookupOf, "(") 'The 'lookupof' a a fied may have a (field=value) filter on the end e.g.  States(group=TH) - returns only states whos group = TH

        Dim luObj As String = bits(0)

        dic = Reflection.WalkPropertyValue(iq, luObj, errorMessages)  'look in a root level dictionary

        If selectedValue IsNot Nothing Then
            TypedTxt.Text = selectedValue.displayname(language)  'IMPORTANT - show the selected value
            If dic Is iq.States Then
                TypedTxt.Style("background-color") = selectedValue.colour  'Colour code the textbox - if it's a state from the states dictionary
            End If
        End If

        'function suggest(textBoxID,valueBoxID, divID, dicName) //Used by the editor
        If (f.InputType.code = "translate") Then
            TypedTxt.Attributes("onkeyup") = "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','translation');"  '<any filter is passed here (through to suggest.aspx)  - call the autosuggest on every keyUp
        Else
            TypedTxt.Attributes("onkeyup") = "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','" & f.lookupOf & "');"  '<any filter is passed here (through to suggest.aspx)  - call the autosuggest on every keyUp
        End If

        'select all the text in the textbox when it's clicked (via Jquesry #id selector) 
        '- display the list, and populate with ALL matches (to a blank string)

        Dim oc$ 'On clicking the typed text box . . 
        oc$ = "$(document.getElementById('" & TypedTxt.ID & "')).select();"
        oc$ &= "display('" & ddh.ID & "','inline-block');"
        If (f.InputType.code = "translate") Then
            oc$ &= "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','translation');"
        Else
            oc$ &= "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','" & f.lookupOf & "');"
        End If

        'oc$ &= "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','" & PathName & "." & f.propertyName "');"

        '";"
        TypedTxt.Attributes("onclick") = oc$
        'setTimeout(function(){a(d
        TypedTxt.Attributes("onblur") = "setTimeout(function(){display('" & ddh.ID & "','none')},500);" 'hide the DDL when we move off the textbox - BUT GIVE IT long enough to accept a selection !!!

    End Function
    Public Function FilledTranslation(f As clsField, selectedValue As clsTranslation, language As clsLanguage, controlid As String, enabled As Boolean, subPanel As Panel, depth As Integer, ByRef errorMessages As List(Of String)) As Panel ' DropDownList

        'Returns a panel conaining a TextBox and a filled DropDown list with script for autosuggest attached


        FilledTranslation = New Panel

        FilledTranslation.Attributes("style") = "overflow:visible;display:inline-block;"  'Overflow so the DDL can hang oout of the div

        Dim TypedTxt As TextBox = New TextBox 'caries the selected/typed text
        Dim txtObjID As New TextBox  'carries the ID of the selected item

        With TypedTxt
            .ID = controlid & "_txt"
            .Enabled = enabled
            .Style("background-color") = "#a0a0ff" 'light blue
            .CssClass = "TypedText"
            'INPUT elements do not behave well with inline-block
            .Attributes("Style") &= " Width:" & CStr(f.width - 2) & "em;"  'leave room for the buttons - there is no good way yo do this - this is the best way i can find  http://coding.smashingmagazine.com/2013/02/27/css-form-elements-problem/             
        End With

        With txtObjID
            .ID = controlid
            '.CssClass = "SelectedID input" 'The input class is vital it tags it a a data carrier
            .CssClass = "input" 'The input class is vital it tags it a a data carrier
            .Attributes("Style") = "display:none" 'this hidden ctextBox carries the ID of the selected item
        End With

        FilledTranslation.Controls.Add(TypedTxt)
        FilledTranslation.Controls.Add(txtObjID) 'invisible

        If subPanel IsNot Nothing Then
            'Edit this list button
            FilledTranslation.Controls.Add(editor.MakeButton(True, "El", "Edit this list", editor.EmbedScript(f.lookupOf, subPanel.ID, "" & depth + 1, False, True)))

            'Edit the target button
            'If selectedValue IsNot Nothing Then
            '    Dim targetPath$ = f.lookupOf & "(" & selectedValue.id & ")"
            '    FilledTranslation.Controls.Add(editor.MakeButton(True, "Et", "Edit the target " & f.labelText, editor.EmbedScript(targetPath, subPanel.ID, "", False, True)))
            'End If
        End If

        Dim ddh As Panel = New Panel 'Drop down holder.. (for the actual DDL)
        ddh.CssClass = "dropdownHolder"
        FilledTranslation.Controls.Add(ddh)

        'ddh.Attributes("style") = "overflow:visible;min-height:100px;"
        'ddh.Attributes("z-index") = 100
        ddh.ID = "ddh_" & controlid

        Dim dic As Object = Nothing
        Dim bits() As String = Split(f.lookupOf, "(") 'The 'lookupof' a a fied may have a (field=value) filter on the end e.g.  States(group=TH) - returns only states whos group = TH

        Dim luObj As String = bits(0)

        dic = Reflection.WalkPropertyValue(iq, luObj, errorMessages)  'look in a root level dictionary

        If selectedValue IsNot Nothing Then
            TypedTxt.Text = selectedValue.textTranslation(language) 'IMPORTANT - show the selected value
            'If dic Is iq.States Then
            '    TypedTxt.Style("background-color") = selectedValue.colour  'Colour code the textbox - if it's a state from the states dictionary
            'End If
        End If

        'function suggest(textBoxID,valueBoxID, divID, dicName) //Used by the editor
        If (f.InputType.code = "translate") Then
            TypedTxt.Attributes("onkeyup") = "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','translation');"  '<any filter is passed here (through to suggest.aspx)  - call the autosuggest on every keyUp
        Else
            TypedTxt.Attributes("onkeyup") = "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','" & f.lookupOf & "');"  '<any filter is passed here (through to suggest.aspx)  - call the autosuggest on every keyUp
        End If

        'select all the text in the textbox when it's clicked (via Jquesry #id selector) 
        '- display the list, and populate with ALL matches (to a blank string)

        Dim oc$ 'On clicking the typed text box . . 
        oc$ = "$(document.getElementById('" & TypedTxt.ID & "')).select();"
        oc$ &= "display('" & ddh.ID & "','inline-block');"
        If (f.InputType.code = "translate") Then
            oc$ &= "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','translation');"
        Else
            oc$ &= "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','" & f.lookupOf & "');"
        End If

        'oc$ &= "suggest('" & TypedTxt.ID & "','" & txtObjID.ID & "','" & ddh.ID & "','" & PathName & "." & f.propertyName "');"

        '";"
        TypedTxt.Attributes("onclick") = oc$
        'setTimeout(function(){a(d
        TypedTxt.Attributes("onblur") = "setTimeout(function(){display('" & ddh.ID & "','none')},500);" 'hide the DDL when we move off the textbox - BUT GIVE IT long enough to accept a selection !!!

    End Function
    Public Function RemoveFilter(filters$, toRemove As String) As String

        If filters$ = "" Then
            Beep() 'this should never happen (becuase remove filter buttons are dynamically disabled
        End If


        Dim l As List(Of String) = Split(filters, "^").ToList

        Dim output As List(Of String) = New List(Of String)

        For Each i In l
            Dim p$() = Split(i, "|")
            If Trim$(p(0)) <> Trim$(toRemove) Then
                output.Add(i) 'Add all but the one we're deleting
            End If
        Next

        Return Join(output.ToArray, "^") 'join all the | delimited filters back together

    End Function


    Public Function sortValue(myString As String) As Int64

        'Computes a sortable vaue from the multiword Input string
        'Uses 6 bits per character and encodes into a INT64 (8 bytes/64 bits)

        sortValue = 0

        myString = Replace(myString, "  ", " ") ' remove any double spaces
        Dim words() As String = Split(myString)

        Dim chars As Integer = 6
        If words.Count >= 3 Then
            chars = 2
        ElseIf words.Count = 2 Then
            chars = 3
        ElseIf words.Count = 1 Then
            chars = 6
        Else
            Stop 'this should never happen
        End If

        Dim pwr As Int64 = Int64.MaxValue
        pwr = pwr \ 64  'Integer divide by 64 'for the most significiant '

        For Each w In words.Take(3)

            For c = 1 To chars
                If c <= Len(w) Then
                    Dim cv As Integer = Asc(Mid(w, c, 1)) - 64 'the 'ascii' values start around 64 for an @ A=65 (this doesn't really handle more elaborate non-western encodings - and will ultimately require a (fairly big) rethink
                    If cv < 0 Then cv = 0
                    If cv > 63 Then cv = 63

                    sortValue += pwr * cv
                End If

                pwr = pwr \ 64
                If pwr <= 0 Then Stop
            Next c
        Next

    End Function



    Public Function basetype(ty$) As String

        basetype = "System." & ty$

        If ty$ = "translate" Or ty$ = "one" Or ty$ = "nullstring" Then
            basetype = "System.string"
        ElseIf ty$ = "many" Then
            basetype = "System.Int32" ' we sort by the number of attached items
        ElseIf ty$ = "customerprice" Or ty$ = "nullprice" Then
            basetype = "System.Single"
        ElseIf ty$ = "xnote" Then
            basetype = "System.string"

        ElseIf ty$ = "icon" Then
            basetype = "System.string"

        End If

    End Function

    Function textRep(activeFilters As Dictionary(Of clsField, Dictionary(Of clsFilter, String))) As String

        'returns a text representation of the supplied set of filter values - used as a key to the cache of views (you can't use a view unless the filters are the same)

        textRep$ = ""
        If activeFilters IsNot Nothing Then
            For Each field In activeFilters.Keys
                textRep$ &= Trim$(field.ID) & "^"
                For Each flt In activeFilters(field).Keys
                    textRep$ &= Trim$(flt.ID) & "^" & activeFilters(field)(flt) & "^" 'final key value pair
                Next
            Next
        End If

    End Function


    Public Function DeBracket(ByVal l$, Optional ob$ = "[", Optional cb$ = "]") As List(Of String)

        'returns a list of all the items within [brackets] in l$

        Dim o As Integer = 1
        Dim c As Integer = 1

        DeBracket = New List(Of String)

        For i = 1 To 1000 'use a for loop so it can never lock up 
            o = InStr(c, l$, ob$)
            If o = 0 Then Exit For
            c = InStr(o, l$, cb$)

            DeBracket.Add(Mid$(l$, o + 1, (c - o - 1)))
        Next i

    End Function



End Module
