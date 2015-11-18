Imports dataAccess

Public Class clsRegion

    Property ID As Integer
    Property Code As String
    Property Name As clsTranslation
    Property isCountry As Boolean
    Property Culture As clsCulture '.net culture code
    'note: currency does not appear here as there is no absolute link between geography and currency - currency is a function of the buyer account (but is defaulted from the culture)
    Property DefaultCurrency As clsCurrency
    Property DefaultLanguage As clsLanguage
    Property Notes As String
    Property isPlaceholder As Boolean  'Should NOT be used for localisation assignments (not an 'official' region)
    Property geoRegion As Integer
    Property Parent As clsRegion  'needed to recurse up (through wider regions)
    Property Children As Dictionary(Of Integer, clsRegion)

    Public quantitiesLoaded As Boolean 'Flag to say that the quanities (autoadds and increments)  have been loaded for this region
    Public slotsLoaded As Boolean 'Flag to say that the slots (Gives and takes)  have been loaded for this region

    Dim oParent As clsRegion

    Public Sub New()

    End Sub

    Public Shared Function getOrMake(parent As clsRegion, code As String, Name As String, isCountry As Boolean, isPlaceholder As Boolean, notes As String) As clsRegion

        'Returns the clsRegion with the specified code - making one if it doesn't exist

        If Not iq.i_region_code.ContainsKey(code) Then
            Dim aRegion As clsRegion = New clsRegion(parent, code, iq.AddTranslation(Name, English, "region", 0, Nothing, 0, False), isCountry, iq.i_culture_code("en-gb"), isPlaceholder, notes)
        End If

        Return iq.i_region_code(code)

    End Function

    ''' <summary>Returns a list of this region and all its ancestors</summary>
    ''' <returns></returns>
    ''' <remarks>e.g.  UK,GWE,EMEMA,XW</remarks>
    Public Function ancestors() As List(Of clsRegion)

        ancestors = New List(Of clsRegion)

        Dim a As clsRegion
        a = Me

        Do
            ancestors.Add(a)
            If a Is r_worldwide Then Exit Do
            a = a.Parent
        Loop

    End Function



    Public Shared Function containment() As Dictionary(Of String, List(Of String))

        containment = New Dictionary(Of String, List(Of String))
        For Each r As clsRegion In iq.Regions.Values
            If r.isCountry = False Then
                containment.Add(r.Code, r.Descendants(False))
            End If
        Next

    End Function

    Public Function treeNode() As WebControls.TreeNode

        treeNode = New WebControls.TreeNode(Me.Displayname(English))
        treeNode.Value = Me.ID

        For Each child In Me.Children.Values
            treeNode.ChildNodes.Add(child.treeNode)
        Next

    End Function

    Public Function Descendants(includeSelf As Boolean) As List(Of String)

        Descendants = New List(Of String)
        If includeSelf Then
            Descendants.Add(Me.Code)
        End If

        For Each child In Me.Children.Values
            Descendants.AddRange(child.Descendants(True))
        Next

    End Function

    Public Function Insert() As clsRegion
        Return New clsRegion(Me.Parent, Me.Code, Me.Name, Me.isCountry, Me.Culture, Me.isPlaceholder, Me.Notes)
    End Function

    Public ReadOnly Property Displayname(language As clsLanguage) As String

        Get
            Return Me.Code & "- " & Me.Name.text(language)
        End Get

    End Property

    ''' <summary>Determines wether this instance of a region contains the specified region (recursively)</summary>
    ''' <remarks>For example 'Europe' contains 'Cornwall' </remarks>
    Public Function Encompasses(region As clsRegion) As Boolean
        'This is called 'encompasses' (rather than contains) - to clearly differntiate from a dictioanry.contains

        Encompasses = False
        If Me Is region Then Encompasses = True : Exit Function 'A region encompasses itself - eg. FRANCE encompasses FRANCE

        For Each r In Me.Children.Values
            If r.Encompasses(region) Then Encompasses = True : Exit Function
        Next

    End Function

    Public Sub New(ByVal id As Integer, Parent As clsRegion, ByVal code As String, Name As clsTranslation, isCountry As Boolean, culture As clsCulture, isPlaceholder As Boolean, notes As String, geoRegionId As String)

        'This is an overriden constructor - becuase the ID *is* specified - it *knows* we' dont want to do a database insert
        Me.ID = id
        Me.Code = code
        Me.Name = Name
        Me.Culture = culture
        Me.isCountry = isCountry
        Me.Parent = Parent
        Me.isPlaceholder = isPlaceholder
        Me.Notes = notes
        If geoRegionId <> "" Then
            Me.geoRegion = CInt(geoRegionId)

        End If


        If Me.Parent IsNot Nothing Then
            Me.Parent.Children.Add(Me.ID, Me)
        End If

        iq.Regions.Add(Me.ID, Me)
        iq.i_region_code.Add(Me.Code, Me)

        Me.Children = New Dictionary(Of Integer, clsRegion)
        oParent = Parent

    End Sub

    Public Sub New(parent As clsRegion, code As String, ByVal Name As clsTranslation, isCountry As Boolean, culture As clsCulture, isPlaceholder As Boolean, Notes As String)

        'Creates a new (instance of the class cls)Language - populates its ID

        If code = "UK" Then Stop

        Dim pid$
        If parent Is Nothing Then
            pid$ = "null"
        Else
            pid$ = parent.ID
        End If

        Dim sql$
        sql$ = "INSERT INTO [Region] ([fk_region_id_parent],[Code],[fk_translation_key_Name],[iscountry],[culture],isplaceholder, notes) "
        sql$ &= "VALUES (" & pid & "," & da.SqlEncode(code) & "," & Name.Key & "," & IIf(isCountry, 1, 0) & "," & Me.Culture.ID & ","
        sql$ &= IIf(isPlaceholder, 1, 0) & "," & da.SqlEncode(Notes) & ");"

        Me.ID = da.DBExecutesql(sql$, True)
        Me.Code = code
        Me.Name = Name
        Me.Culture = culture
        Me.isCountry = isCountry
        Me.Parent = parent
        Me.isPlaceholder = isPlaceholder

        If Me.Parent IsNot Nothing Then
            Me.Parent.Children.Add(Me.ID, Me)
        End If


        iq.Regions.Add(Me.ID, Me)
        iq.i_region_code.Add(Me.Code, Me)

        oParent = parent

        Me.Children = New Dictionary(Of Integer, clsRegion)


    End Sub

    Public Sub Update()

        Dim sql$
        sql$ = "UPDATE [Region] set "

        sql$ &= "[Code]=" & da.SqlEncode(Me.Code) & ","
        sql$ &= "[fk_translation_key_name]=" & Me.Name.Key & ","
        If Me.Parent Is Nothing Then
            sql$ &= "[fk_region_id_parent]=null"
        Else
            sql$ &= "[fk_region_id_parent]=" & Me.Parent.ID
        End If
        sql$ &= ",isCountry=" & IIf(Me.isCountry, 1, 0)
        sql$ &= ",isPlaceHolder=" & IIf(Me.isPlaceholder, 1, 0)
        sql$ &= ",notes=" & da.SqlEncode(Me.Notes)
        sql$ &= ",[FK_Region_ID_Geo]=" & Me.geoRegion
        sql$ &= ",[FK_Culture_ID]=" & Me.Culture.ID
        sql$ &= " WHERE ID=" & Me.ID

        da.DBExecutesql(sql$, False)

        If Me.oParent IsNot Nothing Then
            Me.oParent.Children.Remove(Me.ID)
        End If

        If Me.Parent IsNot Nothing Then
            If Not Me.Parent.Children.ContainsKey(Me.ID) Then
                Me.Parent.Children.Add(Me.ID, Me)
            End If
        End If

        oParent = Parent

    End Sub

    Public Sub Remove()
        Dim sql As String = "Delete [Region] where ID=" & Me.ID
        da.DBExecutesql(sql$, False)
        If Me.oParent IsNot Nothing Then
            Me.oParent.Children.Remove(Me.ID)
        End If

    End Sub

End Class
