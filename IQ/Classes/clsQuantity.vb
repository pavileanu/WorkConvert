Imports dataAccess

'Quantities define AutoAdds, preferred increments, and pre-installed options (the same thing)

Public Class clsQuantity
    Implements i_Editable
    Property ID As Integer
    Property Region As clsRegion         'Certain things (carepacks, warranties O/S's are only added in certain countries) - furthermore - certain systems are only available in certain countries
    Property Path As String                'OPTIONAL specific tree node (because of grafts - a branch id is not unique)
    Property Branch As clsBranch

    Property NumPreInstalled As Integer   'number of this component fitted by default - in this context
    Property MinIncrement As Integer
    Property PreferredIncrement As Integer
    Property FOC As Boolean               'Free of charge (or not)

    Public oBranch As clsBranch ' holds a reference the the branch this quantity was originally on when it was created (for editing).. such that the quantity can be 'reparented'
    Public oRegion As clsRegion 'similar to the above..
    Public oPath As String
    Public deleted As Boolean

    '   Public i_Quantities As Dictionary(Of clsRegion, Dictionary(Of String, clsBranch))

    Public Sub New()

    End Sub

    Public ReadOnly Property IsAutoAdd As Boolean
        Get
            Return (NumPreInstalled > 0)
        End Get
    End Property
    Public Function clone(newpath$) As clsQuantity

        clone = New clsQuantity(Me.Region, newpath$, Me.Branch, Me.NumPreInstalled, Me.MinIncrement, Me.PreferredIncrement, Me.FOC)

    End Function
    Public Function displayName(language As clsLanguage) As String Implements i_Editable.displayName
        Return "rgn:" & Me.Region.Code & " path:" & Me.Path & " preInst:" & Me.NumPreInstalled & " minIncr:" & Me.MinIncrement & " prfIncr:" & Me.PreferredIncrement & " foc:" & Me.FOC
    End Function
    Public Function XML() As String

        With Me
            Return String.Format("<quantity id='{0}' region='{1}' path='{2}' number='{3}' minIncr='{4}' prefIncr='{5}' freeOfCharge='{6}'/>", _
                                 .ID, .Region.Code, .Path, .NumPreInstalled, MinIncrement, PreferredIncrement, FOC)
        End With

    End Function

    Public Function Insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert

        'Return New clsQuantity(Me.Region, Me.Path, Me.Branch, Me.SKUVariant, Me.NumPreInstalled, Me.MinIncrement, Me.PreferredIncrement, Me.FOC)
        Return New clsQuantity(Me.Region, Me.Path, Me.Branch, Me.NumPreInstalled, Me.MinIncrement, Me.PreferredIncrement, Me.FOC)
    End Function

    Public Function adminTableRow(bi As clsBranchInfo) As TableRow

        Dim tr As TableRow
        Dim td As TableCell

        Dim isPruned As Boolean = False
        tr = New TableRow

        If Not String.IsNullOrWhiteSpace(Me.Path) Then
            If Me.Branch.PruneInForce(Me.Path, bi.buyerAccount.SellerChannel) > 0 Then
                isPruned = True
            End If
        End If

        If isPruned Then tr.Attributes.Add("style", "text-decoration:line-through;") : tr.Attributes.Add("title", "the quanity sits on a pruned bracnh in this context - and is not in force")

        If Me.deleted Then tr.CssClass &= " deletedRow"

        td = New TableCell
        td.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit this quantity", bi.agentAccount.Language), _
"window.open('edit.aspx?path=Quantities(" & Me.ID & ")&TreePath=" & bi.path & "&lid=" & bi.lid.ToString & "');return(false);", _
"", "width:25px;height:25px;", bi.buyerAccount.Language))
        tr.Controls.Add(td)


        td = New TableCell
        tr.Controls.Add(td)
        td.Text = Me.Path
        tr.Controls.Add(td)
        td.ToolTip = Utility.PathName(Me.Path)

        td = New TableCell
        tr.Controls.Add(td)
        td.Text = Me.NumPreInstalled.ToString.Trim
        tr.Controls.Add(td)

        td = New TableCell
        tr.Controls.Add(td)
        td.Text = Me.MinIncrement.ToString.Trim
        tr.Controls.Add(td)

        td = New TableCell
        tr.Controls.Add(td)
        td.Text = Me.PreferredIncrement.ToString.Trim
        tr.Controls.Add(td)

        ' td = New TableCell
        ' td.Text = Me.Branch.Product.DisplayName(bi.buyerAccount.Language) & " (" & Me.Branch.Product.ID & ")"
        ' tr.Controls.Add(td)

        td = New TableCell
        td.Text = Me.Branch.Product.ProductType.DisplayName(bi.buyerAccount.Language)
        td.ToolTip = Me.Branch.Product.DisplayName(bi.buyerAccount.Language) & " (" & Me.Branch.Product.ID & ")"
        tr.Controls.Add(td)

        td = New TableCell
        td.Text = Me.FOC.ToString
        tr.Controls.Add(td)

        td = New TableCell
        tr.Controls.Add(td)
        td.Text = Me.Region.Code
        tr.Controls.Add(td)

        td = New TableCell
        tr.Controls.Add(td)
        If Not String.IsNullOrWhiteSpace(Me.Path) Then
            td.Text = Me.Branch.PruneInForce(Me.Path, bi.buyerAccount.SellerChannel)
        End If
        tr.Controls.Add(td)

        td = New TableCell
        tr.Controls.Add(td)
        If Me.deleted Then
            Dim lt As Literal = FunctionButton(bi.path, Me.ID, "unDeleteQuantity", "unDEL", "unDelete this quantity")
            td.Controls.Add(lt)

        Else

            Dim lt2 As Literal = FunctionButton(bi.path, Me.ID, "deleteQuantity", "DEL", "Delete this quantity")
            td.Controls.Add(lt2)

        End If

        Return tr

    End Function


    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$
        sql$ = "UPDATE [Quantity] SET "
        sql$ &= " Preinstalled=" & Me.NumPreInstalled
        sql$ &= ",MinIncrement=" & Me.MinIncrement
        sql$ &= ",PreferredIncrement=" & Me.PreferredIncrement
        sql$ &= ",FK_Region_ID=" & Me.Region.ID
        ' sql$ &= ",FK_Variant_ID=" & Me.SKUVariant.ID
        sql$ &= ",Foc=" & IIf(Me.FOC, 1, 0)
        sql$ &= ",deleted =" & IIf(Me.deleted, 1, 0)

        sql$ &= " WHERE ID= " & Me.ID
        da.DBExecutesql(sql, False)

        Me.oBranch.Quantities.Remove(Me.ID)
        Me.Branch.Quantities.Add(Me.ID, Me)
        'Me.oBranch.i_Quantities(Me.oRegion).Remove(Me.oPath)
        'Me.Branch.i_Quantities(Me.Region).Add(Me.Path, Me)

        oBranch = Me.Branch
        oRegion = Me.Region
        oPath = Me.Path

        'Return Me

    End Sub

    Public Sub Delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Dim sql$
        sql$ = "DELETE FROM [Quantity] WHERE ID=" & Me.ID
        da.DBExecutesql(sql$, False)

        Me.oBranch.Quantities.Remove(Me.ID)
        'Me.oBranch.i_Quantities(Me.oCountry).Remove(Me.oPath)
        iq.Quantities.Remove(Me.ID)

    End Sub
    'Public Function compoundKey() As String
    '    'used to make a lookup in the branches slot sorteddictionary - having them in a sorted dictionary means they can be presented in a sensible order
    '    Return Me.Country.ID & "_" & Me.Path
    'End Function

    '   Public Sub New(region As clsRegion, ByVal Path As String, ByVal Branch As clsBranch, SKUvariant As clsVariant, ByVal numPreInstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean, Optional Writecache As DataTable = Nothing)
    Public Sub New(region As clsRegion, ByVal Path As String, ByVal branch As clsBranch, ByVal numPreInstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean, Optional Writecache As DataTable = Nothing)

        'Note:- Quanitites do not have a clsVariant - the 'best' variant to autoadd/preinstall is determined from the system, warehouse and localisation

        ' If numPreInstalled > 0 And FOC = False Then Stop 'this shold be a carepack or OS.. but not an FIO

        Me.Path = Path
        Me.Branch = branch ' iq.Branches(Split(Path, ".").Last)

        Me.NumPreInstalled = numPreInstalled
        Me.Region = region
        Me.FOC = freeOfCharge

        If numPreInstalled < 0 Then Stop

        Me.MinIncrement = MinIncrement
        Me.PreferredIncrement = PreferredIncrement

        ' If Branch.i_Quantities Is Nothing Then Branch.i_Quantities = New Dictionary(Of clsRegion, Dictionary(Of String, clsQuantity))

        '        DeDupeCountryPath()

        If Writecache Is Nothing Then
            Dim sql$
            sql$ = "INSERT INTO [Quantity] (fk_branch_id,fk_region_id,path,Preinstalled,MinIncrement,PreferredIncrement,foc) "
            sql$ &= "VALUES (" & branch.ID & "," & region.ID & "," & da.SqlEncode(Path) & "," & numPreInstalled & "," & MinIncrement & "," & PreferredIncrement & "," & IIf(Me.FOC, "1", "0") & ");"

            Me.ID = da.DBExecutesql(sql, True)

            If branch.Quantities Is Nothing Then branch.Quantities = New Dictionary(Of Integer, clsQuantity)
            If Not branch.Quantities.ContainsKey(Me.ID) Then branch.Quantities.Add(Me.ID, Me)
            iq.Quantities.Add(Me.ID, Me)

        Else

            Dim row As System.Data.DataRow
            row = Writecache.NewRow()
            row("FK_Branch_ID") = branch.ID
            row("FK_Region_id") = region.ID
            row("Path") = Path
            row("Preinstalled") = numPreInstalled
            row("Minincrement") = MinIncrement
            row("PreferredIncrement") = PreferredIncrement
            row("FOC") = IIf(FOC, 1, 0)  'free of charge
            row("deleted") = 0


            Writecache.Rows.Add(row)

        End If

        '    If Not branch.i_Quantities.ContainsKey(region) Then branch.i_Quantities.Add(region, New Dictionary(Of String, clsQuantity))
        'If Not Branch.i_Quantities.ContainsKey(country) Then Branch.i_Quantities.Add(country, New Dictionary(Of String, clsQuantity))
        'Branch.i_Quantities(country).Add(Path, Me)


        oBranch = branch
        oRegion = region
        oPath = Path

    End Sub

    'Public Sub DeDupeCountryPath()

    '    'Tweeks the ME variables to make sure we're not inserting a duplicate
    '    'If Not Me.Branch.i_Quantities.ContainsKey(Me.Country) Then Exit Sub ' no conflict

    '    Dim dic As Dictionary(Of String, clsQuantity)
    '    dic = Me.Branch.i_Quantities(Me.Country) 'get the dictionary of paths>quanities (by country)
    '    If Not dic.ContainsKey(Me.Path) Then Exit Sub

    '    If Me.Path <> "" Then Me.Path = "" 'a local, specific quantity already existed - try a global
    '    If Not dic.ContainsKey(Me.Path) Then Exit Sub ' resolved.. we'll use a global

    '    For Each C In iq.Countries.Values
    '        If Not Me.Branch.i_Quantities.ContainsKey(C) Then Me.Country = C : Exit Sub 'resolved (using a different country)
    '    Next

    'End Sub

    'Public Sub New(ByVal id As Integer, Region As clsRegion, ByVal Path As String, ByVal Branch As clsBranch, skuvariant As clsVariant, ByVal numPreinstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean)
    Public Sub New(ByVal id As Integer, Region As clsRegion, ByVal Path As String, ByVal Branch As clsBranch, ByVal numPreinstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean)

        Me.ID = id
        Me.Region = Region
        Me.Path = Path
        Me.Branch = Branch
        ' Me.SKUVariant = skuvariant
        Me.NumPreInstalled = numPreinstalled
        Me.MinIncrement = MinIncrement
        Me.PreferredIncrement = PreferredIncrement
        Me.FOC = freeOfCharge

        'iq.Quantities.Add(Path, Me)
        'If Branch.i_Quantities Is Nothing Then Branch.i_Quantities = New Dictionary(Of clsRegion, Dictionary(Of String, clsQuantity))
        'If Not Branch.i_Quantities.ContainsKey(country) Then Branch.i_Quantities.Add(country, New Dictionary(Of String, clsQuantity))

        'If Not Branch.i_Quantities(country).ContainsKey(Path) Then  'Remove this IF - i jsut had some screwy data
        ' Branch.i_Quantities(country).Add(Path, Me)
        ' End If

        If Branch.Quantities Is Nothing Then Branch.Quantities = New Dictionary(Of Integer, clsQuantity)
        'If Not Branch.Quantities.ContainsKey(Me.ID) Then Branch.Quantities.Add(Me.ID, Me)
        Branch.Quantities.Add(Me.ID, Me)
        iq.Quantities.Add(Me.ID, Me)

        oBranch = Branch
        oRegion = Region
        oPath = Path

    End Sub

End Class
