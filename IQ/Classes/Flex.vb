Imports dataAccess
Imports System.IO

Public Class clsFlexOPG

    Public Property ID As Integer
    Public Property OPGRef As String
    Public Property Description As String
    Public Property ValidFrom As Date
    Public Property ValidTo As Date
    Public Property Currency As clsCurrency
    Public MinOptions As Integer
    Public MaxOptions As Integer
    Public Property OPGSysType As String
    Public Rules As Dictionary(Of Integer, clsFlexRule)
    Public Lines As Dictionary(Of Integer, clsFlexLine)
    'Public Regions As Dictionary(Of Integer, clsFlexRegion) 'NOTE - this is NOT a dictionary of clsRegions! (the clsFlexRegion allows the required many:many relationship - mostly for editing)
    Public regions As Dictionary(Of Integer, clsRegion)

    'Public Sub serialize(Sw As streamwriter)

    '    'for any object - you only need write the ID
    '    'For any dictionary - you write the IDs

    'End Sub

    'Public Sub deSerialize(sr As streamreader)

    'End Sub

    Public Sub New(ID As Integer, OPGref As String, Description As String, validFrom As Date, validTo As Date, Currency As clsCurrency, minOptions As Integer, maxOptions As Integer, OPGSysType As String)

        Me.ID = ID
        Me.OPGRef = OPGref
        Me.Description = Description
        Me.ValidFrom = validFrom
        Me.ValidTo = validTo
        Me.Currency = Currency
        Me.MinOptions = minOptions
        Me.MaxOptions = maxOptions
        Me.OPGSysType = OPGSysType

        Me.Rules = New Dictionary(Of Integer, clsFlexRule)
        Me.Lines = New Dictionary(Of Integer, clsFlexLine)
        Me.regions = New Dictionary(Of Integer, clsRegion) 'clsFlexRegion)
        iq.FlexOPGs.Add(Me.ID, Me)

    End Sub

    Public Sub New(OPGref As String, Description As String, validFrom As Date, validTo As Date, Currency As clsCurrency, MinOptions As Integer, maxOptions As Integer, OPGSysType As String, Optional dt As DataTable = Nothing)

        Me.ID = ID
        Me.OPGRef = OPGref
        Me.Description = Description
        Me.ValidFrom = validFrom
        Me.ValidTo = validTo
        Me.Currency = Currency
        Me.MinOptions = MinOptions
        Me.MaxOptions = maxOptions
        Me.OPGSysType = OPGSysType

        Me.Rules = New Dictionary(Of Integer, clsFlexRule)
        Me.Lines = New Dictionary(Of Integer, clsFlexLine)
        Me.regions = New Dictionary(Of Integer, clsRegion) ' clsFlexRegion)

        If dt Is Nothing Then
            Dim Sql$ = " INSERT INTO Flex (OPGref,description,validFrom,validTo,FK_currency_ID,minoptions,maxoptions,OPGSysType) "
            Sql$ &= "values (" & Me.OPGRef & "," & da.SqlEncode(Description) & "," & da.UniversalDate(Me.ValidFrom) & "," & da.UniversalDate(Me.ValidTo) & ","
            Sql$ &= Me.Currency.ID & "," & Me.MinOptions & "," & Me.MaxOptions & "," & da.SqlEncode(Me.OPGSysType) & ");"
            Me.ID = da.DBExecutesql(Sql$, True)

            iq.FlexOPGs.Add(Me.ID, Me)
        Else
            Dim dr As DataRow
            dr = dt.NewRow
            dr.Item("OPGRef") = Me.OPGRef
            dr.Item("Description") = Me.Description
            dr.Item("validFrom") = Me.ValidFrom
            dr.Item("validTo") = Me.ValidTo
            dr.Item("FK_Currency_ID") = Me.Currency.ID
            dr.Item("MinOptions") = Me.MinOptions
            dr.Item("MaxOptions") = Me.MaxOptions
            dr.Item("OPGSysType") = Me.OPGSysType

            dt.Rows.Add(dr)
            Me.ID = -1
        End If
    End Sub
    Public Function getRule(ProductType As clsProductType) As clsFlexRule

        Dim r = From v In Me.Rules.Values Where v.ProductType Is ProductType
        If r.Any Then getRule = r.First Else getRule = Nothing

    End Function
    Public Function isCurrent() As Boolean

        isCurrent = False
        If Me.validFrom < Now And Me.validTo > Now Then isCurrent = True

    End Function

    Public Function AppliesToRegion(region As clsRegion) As Boolean

        AppliesToRegion = False

        '    If Me.Regions.Values.Count = 0 Then Stop

        For Each r In Me.Regions.Values  'Each flexOPG potentially applies in many regions (becuase although regions are a hierarchy, a single region doesn't quite cut it (eg. Peru & Mexico)
            If r.Encompasses(region) Then
                AppliesToRegion = True : Exit For
            End If
        Next

    End Function

    ''' <summary>Returns the FlexLines from this FlexOPG which match the supllied critera</summary>
    Public Function MatchingFlexLines(Optional product As clsProduct = Nothing, Optional qty As Integer = 0, Optional dateTime As Object = Nothing, Optional region As clsRegion = Nothing) As List(Of clsFlexLine)

        Pmark("matchingFlexLines")
        'returns the FlexLines (containing rebate information)  is for the sepcified prodType,qty..etc (which are all optional)

        MatchingFlexLines = New List(Of clsFlexLine)

        Dim dateValid As Boolean = False
        If dateTime Is Nothing Then
            dateValid = True
        Else
            If dateTime > Me.ValidFrom And dateTime < Me.ValidTo Then
                dateValid = True
            End If
        End If

        Dim regionValid As Boolean = False
        If region Is Nothing Then
            regionValid = True
        Else
            For Each r In Me.Regions.Values  'Each flexOPG potentially applies in many regions (becuase although regions are a hierarchy, a single region doesn't quite cut it (eg. Peru & Mexico)
                If r.Encompasses(region) Then regionValid = True : Exit For
            Next
        End If

        If dateValid And regionValid Then

            'build an index to look up rules for each product type quickly (this should probably go in the clsFlex object for speed)
            Dim i_rules_producttype As Dictionary(Of clsProductType, clsFlexRule) = New Dictionary(Of clsProductType, clsFlexRule)
            For Each r In Me.Rules.Values
                i_rules_producttype.Add(r.ProductType, r)
            Next

            For Each flexline In Me.Lines.Values

                If flexline.Product Is product Or product Is Nothing Then

                    Dim rule As clsFlexRule = Nothing
                    If i_rules_producttype.ContainsKey(flexline.Product.ProductType) Then
                        rule = i_rules_producttype(flexline.Product.ProductType)
                    End If

                    Dim rulevalid As Boolean = False
                    If rule Is Nothing Then
                        rulevalid = True
                    Else
                        If (qty >= rule.min And qty <= rule.max) Or qty = 0 Then rulevalid = True
                    End If

                    If rulevalid Then MatchingFlexLines.Add(flexline)
                End If
            Next
        End If

        Pacc("matchingFlexLines")

    End Function


End Class


Public Class clsFlexLine
    Public Property ID As Integer
    Public Property FlexOPG As clsFlexOPG
    Public Property Product As clsProduct
    Public Property rebate As Decimal
    Public Property validFrom As DateTime
    Public Property validTo As DateTime

    Public Sub New(ID As Integer, FlexOPG As clsFlexOPG, Product As clsProduct, Rebate As Single, validFrom As Date, validTo As Date)

        Me.ID = ID
        Me.FlexOPG = FlexOPG
        Me.Product = Product
        Me.rebate = CDec(Rebate)
        Me.validFrom = validFrom
        Me.validTo = validTo

        Product.OPGflexLines.Add(Me.ID, Me)
        FlexOPG.Lines.Add(Me.ID, Me)

    End Sub

    Public Sub New(FlexOPG As clsFlexOPG, Product As clsProduct, rebate As Single, validFrom As Date, validTo As Date, Optional dt As DataTable = Nothing)

        Me.ID = ID
        Me.FlexOPG = FlexOPG
        Me.Product = Product
        Me.rebate = CDec(rebate)
        Me.validFrom = validFrom
        Me.validTo = validTo

        If dt IsNot Nothing Then


            Dim dr As DataRow = dt.NewRow
            dr("fk_flex_id") = FlexOPG.ID
            dr("FK_Product_id") = Product.ID
            dr("rebate") = rebate
            dr("validFrom") = validFrom
            dr("validTo") = validTo
            dt.Rows.Add(dr)
            Me.ID = -1
        Else
            Dim sql$ = "INSERT INTO FlexLine (FK_Product_ID,rebate,FK_Flex_ID,validFrom,ValidTo) VALUES "
            sql$ &= "(" & Me.Product.ID & "," & Me.rebate & "," & Me.FlexOPG.ID & "," & da.UniversalDate(Me.validFrom) & "," & da.UniversalDate(Me.validTo) & ");"
            Me.ID = da.DBExecutesql(sql$, True)
            FlexOPG.Lines.Add(Me.ID, Me)
            Product.OPGflexLines.Add(Me.ID, Me)

        End If


    End Sub

    Public Function isCurrent() As Boolean

        isCurrent = False
        If Me.validFrom < Now And Me.validTo > Now Then isCurrent = True

    End Function


End Class

Public Class clsFlexRule
    Public Property ID As Integer
    Public Property ProductType As clsProductType
    Public Property min As Integer 'NullableInt
    Public Property max As Integer
    Public Property optionalRule As Boolean
    Public Property flexOPG As clsFlexOPG

    Public Sub New(ID As Integer, flexOPG As clsFlexOPG, ProductType As clsProductType, min As Integer, max As Integer, optionalRule As Boolean)

        Me.ID = ID
        Me.ProductType = ProductType
        Me.min = min
        Me.max = max
        Me.optionalRule = optionalRule
        flexOPG.Rules.Add(Me.ID, Me)

    End Sub

    Public Sub New(FlexOPG As clsFlexOPG, ProductType As clsProductType, min As Integer, max As Integer, optionalRule As Boolean, dt As DataTable)

        Me.flexOPG = FlexOPG
        Me.ProductType = ProductType
        Me.min = min
        Me.max = max
        Me.optionalRule = optionalRule

        If dt IsNot Nothing Then
            Dim dr As DataRow = dt.NewRow
            dr("FK_Flex_id") = Me.flexOPG.ID
            dr("FK_ProductType_id") = Me.ProductType.ID
            dr("min") = Me.min
            dr("max") = Me.max
            dr("optionalRule") = Me.optionalRule
            dt.Rows.Add(dr)
            Me.ID = -1
        Else
            Dim sql$ = "INSERT INTO FlexRule (FK_Flex_ID,FK_ProductType_ID,[min],[max],[optionalRule]) VALUES "
            sql$ &= "(" & Me.flexOPG.ID & "," & Me.ProductType.ID & "," & Me.min & "," & Me.max & "," & Me.optionalRule & ");"
            Me.ID = da.DBExecutesql(sql$, True)
            FlexOPG.Rules.Add(Me.ID, Me)
        End If

    End Sub
End Class

Public Class clsFlexRegion  'Should Allow an opgs regions to be edited (and presisted) - The editor can get you edit the FlexOPG's regions - but can't 'store' them
    Public Property ID As Integer
    Public Property FlexOPG As clsFlexOPG
    Public Property Region As clsRegion

    Public Sub New(ID As Integer, flexOPG As clsFlexOPG, Region As clsRegion)

        Me.ID = ID
        Me.FlexOPG = flexOPG
        Me.Region = Region
        flexOPG.regions.Add(Me.Region.ID, Me.Region)  'ME

    End Sub

    Public Sub New(FlexOPG As clsFlexOPG, region As clsRegion, Optional dt As DataTable = Nothing)

        Me.FlexOPG = FlexOPG
        Me.Region = region


        If dt IsNot Nothing Then
            Dim dr As DataRow = dt.NewRow
            dr("FK_Flex_id") = Me.FlexOPG.ID
            dr("FK_region_id") = Me.Region.ID
            dt.Rows.Add(dr)
            Me.ID = -1
        Else
            Dim sql$ = "INSERT INTO FlexRegion(FK_Flex_ID,FK_region_ID,[min],[max]) VALUES "
            sql$ &= "(" & Me.FlexOPG.ID & "," & Me.Region.ID & ");"
            Me.ID = da.DBExecutesql(sql$, True)
            Me.FlexOPG.regions.Add(Me.Region.ID, Me.Region)
        End If



    End Sub

End Class
