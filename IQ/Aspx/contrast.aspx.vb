Option Explicit On
Option Strict On

Public Class compare
    Inherits clsPageLogging

    Private Function FetchBranchesToCompare(checkboxIds As String) As Dictionary(Of String, clsBranch)

        FetchBranchesToCompare = New Dictionary(Of String, clsBranch) 'path>branch

        'We've been supplied a comma seperated list of checkbox control id's of the form 
        'sl_tree.1.5.1920.8292.63627,sl_tree.1.5.1920.12939.39939 . delimited paths
        For Each I As String In Split(checkboxIds, ",").ToList
            If I <> "" Then 'there's a trailing comma - which is easier to ignore here than faff about with in JS
                FetchBranchesToCompare.Add(Split(I, "_")(1), iq.Branches(CInt(Split(I, ".").Last))) '
            End If
        Next

    End Function


    Private Function BuildCompareMatrix(lid As UInt64, BranchesToCompare As Dictionary(Of String, clsBranch), ByRef errorMessages As List(Of String)) As Dictionary(Of String, Dictionary(Of clsBranch, String))

        'The display data is assembled in a dictionary of dictioanries 
        'The first index being a string representation of the attribute title "Size", "PowerConsumprion", "SKU" etc
        'each row in this outer dictionary contains a dictionary - of BRANCH>display value (for each attribute)

        Dim cm As Dictionary(Of String, Dictionary(Of clsBranch, String)) = New Dictionary(Of String, Dictionary(Of clsBranch, String))
        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        Dim q As clsQuantity

        'pass 1, create a matrix - with one row for each attribute/quantity/slot for each branch being compared

        Dim ky$

        Dim branch As clsBranch ' each branch forms one column in the comparison matrix
        For Each pth In BranchesToCompare.Keys 'the keys are the paths, the values are the terminal branches (systems we're comparing)

            'product attributes
            branch = BranchesToCompare(pth)
            For Each pa In BranchesToCompare(pth).Product.Attributes.Values
                If pa.Attribute.Order = 0 Then Continue For 'Ignore anything which is order 0

                If InStr("mfrsku,", LCase(pa.Attribute.Code)) = 0 Then  'supress some attributes here

                    ky$ = pa.Attribute.displayName(agentAccount.Language)
                    If Not cm.ContainsKey(ky$) Then
                        cm.Add(ky$, New Dictionary(Of clsBranch, String))
                    End If
                    If Not cm(ky).ContainsKey(branch) Then
                        cm(ky).Add(branch, pa.displayName(agentAccount.Language))  'this is the display value of the product attribute EG. 16gb - NOT the attribute (eg. "memory")
                    End If
                End If
            Next

            'Preinstalled options
            For Each q In branch.preInstalled(buyerAccount, pth, errormessages) 'path$ & "." & Trim$(branch.ID))
                ky$ = q.Branch.Product.ProductType.DisplayName(agentAccount.Language) 'q.Branch.Product.DisplayName(agentAccount.Language)
                If Not cm.ContainsKey(ky$) Then
                    cm.Add(ky$, New Dictionary(Of clsBranch, String))
                End If
                Dim v$ = q.Branch.Product.DisplayName(agentAccount.Language)
                If Not cm(ky).ContainsKey(branch) Then cm(ky$).Add(branch, IIf(q.NumPreInstalled > 1, q.NumPreInstalled & " x " & v$, v$).ToString)
            Next

            'consolidate the slot info (count up the number of PCI slots of each type)
            Dim slottypes As Dictionary(Of clsSlotType, Integer) 'slottype > count
            slottypes = New Dictionary(Of clsSlotType, Integer)

            For Each s In BranchesToCompare(pth).slots.Values
                If s.path = pth Or s.path$ = "" Then
                    If s.slotNum.value Is DBNull.Value Then    'normal' slots
                        slottypes.Add(s.Type, s.numSlots)
                    Else
                        'numbered, PCI slots
                        If s.numSlots <> 1 Then Stop
                        If Not slottypes.ContainsKey(s.Type) Then
                            slottypes.Add(s.Type, 1)
                        Else
                            slottypes(s.Type) += 1 'add another (of this type of PCI slot)
                        End If
                    End If
                End If
            Next

            Dim stn$ 'slot type name
            For Each s In slottypes.Keys
                stn = s.displayName(agentAccount.Language)
                If Not cm.ContainsKey(stn) Then
                    cm.Add(stn, New Dictionary(Of clsBranch, String))
                End If
                cm(stn)(branch) = slottypes(s).ToString 'set the value to the number of slots of this type                
            Next
        Next

        Return cm

    End Function

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'this ASPX is called AJAX'd in from the JS contrast() function 
        'thus:-
        'rExec("contrast.aspx?BPs=" + elids + '&path=' + path, placeContrast); 

        'We render a column (in the matrix) for each BRANCH being compared

        Dim lid As UInt64 = CType(Request.QueryString("lid"), UInt64)
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)

        Dim BranchesToCompare As Dictionary(Of String, clsBranch) = _
        FetchBranchesToCompare(Request("BPs"))

        Dim errorMessages As List(Of String) = New List(Of String)
        Dim cm As Dictionary(Of String, Dictionary(Of clsBranch, String)) = _
        BuildCompareMatrix(lid, BranchesToCompare, errormessages)

        Pnl.Controls.Add(outputTable(cm, BranchesToCompare, Request("path"), agentAccount, buyerAccount, lid))

        OutputErrors(Pnl.Controls, errorMessages, lid, True)

    End Sub

    Private Function outputTable(cm As Dictionary(Of String, Dictionary(Of clsBranch, String)), branches As Dictionary(Of String, clsBranch), path$, AgentAccount As clsAccount, BuyerAccount As clsAccount, lid As UInt64) As Table

        Dim t As Table = New Table
        Pnl.Controls.Add(t)
        t.CssClass = "compareTable"

        'make the headers
        Dim tr As TableRow
        tr = New TableRow

        'top left grid cell placeholder
        t.Controls.Add(tr)

        Dim topLeft As TableCell = ContrastCell("&nbsp;", "compareTopLeftCell", branches.Count + 1, 30)
        tr.Controls.Add(topLeft)

        Dim occ$
        occ$ = "document.getElementById('contrast." & path$ & "').innerHTML='';return false;"

        'Dim btnclose As ImageButton = New ImageButton
        ' btnclose.Attributes("onmousedown") = occ$
        ' btnclose.ID = "btnCloseContrast"
        ' btnclose.OnClientClick = "return false;" 'stop it from posting back
        ' btnclose.ImageUrl = "/images/navigation/close.png"

        'topLeft.Controls.Add(MakeRoundButton("close.png", "Finish comparing (close)", occ$, "", "", AgentAccount.Language))

        Dim colheader As TableCell 'Panel
        For Each k In branches.Keys 'these are the paths
            'branches are the things we're comparing - so branches.count is the number of columns (+1 for the row heads)
            colheader = ContrastCell(branches(k).Product.DisplayName(AgentAccount.Language), "compareColumnHeads", branches.Count + 1, 0)
            Dim lit As Literal = New Literal
            lit.Text = "<br/>"
            colheader.Controls.Add(lit)
            colheader.Controls.Add(branches(k).BuyUI(BuyerAccount, k, lid))
            CType(colheader.Controls(colheader.Controls.Count - 1), Panel).Style("text-align") = "center;"
            tr.Controls.Add(colheader)
        Next

        'headerRow.Controls.Add(UnFloat)

        'key (attribute name/title) >Distinct column count
        Dim distinctCount As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
        Dim distinct As List(Of String)

        For Each K In cm.Keys 'the keys are the attribute names/titles (row headers - column 0)


            distinct = New List(Of String)
            If cm(K).Values.Count < branches.Count Then
                distinct.Add("") 'we have at least one 'null' (how many - doesnt actually matter)
            End If

            For Each v In cm(K).Values

                If Not distinct.Contains(v) Then distinct.Add(v)
            Next

            distinctCount.Add(K, distinct.Count) ' The number of matching columns (columns that carry the same value) is the number of columns (branches we are contrasting)minus the number of distinct ones
        Next

        Dim css$

        'LINQ
        Dim iter = (From kvp In distinctCount Order By kvp.Value Descending)  'Use linq to create an iterator object which steps through the keys in desceding order of the number of distinct values

        Dim value$

        Dim row As TableRow 'Panel
        Dim headers As List(Of String) = New List(Of String)

        For Each kvp In iter


            Dim h$ = "Differences"
            If kvp.Value > 1 And Not headers.Contains(h$) Then
                t.Controls.Add(Sectionheader(h$, branches.Count))
                headers.Add(h$)
            Else
                h$ = "Similarities"
                If kvp.Value = 1 And Not headers.Contains(h$) Then
                    t.Controls.Add(Sectionheader(h$, branches.Count))
                    headers.Add(h$)
                End If
            End If

            row = New TableRow
            t.Controls.Add(row)

            'row header (attribute name)
            '                                                                            %width
            row.Controls.Add(ContrastCell(kvp.Key, "compareRowHeads", branches.Count + 1, 30))

            distinct = New List(Of String) 'used when rendering each row 
            For Each branch In branches.Values

                If cm(kvp.Key).ContainsKey(branch) Then
                    value = cm(kvp.Key)(branch)

                    If distinctCount(kvp.Key) = 1 Then 'they're all the same
                        css = "compareSetAllSame"
                    Else
                        If Not distinct.Contains(value) Then distinct.Add(value)
                        css = "compareSet" & Trim$(distinct.IndexOf(value).ToString) 'render each disitnct group in a different colour
                    End If
                    row.Controls.Add(ContrastCell(value, css, branches.Count + 1, 0))
                Else
                    row.Controls.Add(ContrastCell("-", "compareWithout", branches.Count + 1, 0))
                End If
            Next
            'row.Controls.Add(UnFloat())
        Next kvp

        Return t

    End Function

    Private Function Sectionheader(h$, cols As Integer) As TableRow

        Sectionheader = New TableRow
        Dim td As TableHeaderCell = New TableHeaderCell
        td.CssClass = "sectionHead"
        td.ColumnSpan = cols + 1
        Sectionheader.Controls.Add(td)
        Dim lit As New Literal
        lit.Text = "<span>" & h$ & "</span>"
        td.Controls.Add(lit)

    End Function

    Private Function UnFloat() As Literal
        Dim lit As Literal = New Literal
        lit.Text = "<div style='float:left;clear:both;'>  &nbsp; </div>"
        Return lit

    End Function
    Private Function ContrastCell(text$, css$, cols As Integer, width As Integer) As TableCell

        Dim tc As TableCell = New TableCell
        Dim lit As Literal
        'hd.Attributes("style") = "height:inherit;width:" & Int(99 / cols) & "%;float:left;color:white;background-color:" & css & ";text-align:" & textAlign & ";border-style:solid;border-width:0px;border-right-width:1px;border-bottom-width:1px;"
        'tc.Attributes("style") = "color:white;background-color:" & css & ";text-align:" & textAlign & ";"
        tc.CssClass = css$
        If width <> 0 Then tc.Attributes("style") &= "width:" & width & "em;"
        lit = New Literal
        lit.Text = text$
        tc.Controls.Add(lit)

        Return tc

    End Function

End Class