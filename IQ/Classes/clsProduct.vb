
Imports dataAccess

Public Class clsProduct

    Implements i_Editable

    Property ID As Integer
    'For each product - each seller offers prices to each buyer in each currency
    'Public pricekeys As Dictionary(Of Integer, Dictionary(Of Integer, Dictionary(Of Integer, Single)))

    Property Sector As clsSector
    Property ProductType As clsProductType
    Property Branches As New HashSet(Of clsBranch) 'Dictionary(Of Integer, clsBranch) = New Dictionary(Of Integer, clsBranch)

    'this has been  made private so as to force acces to prices via the clearer Baseprice(),NullablePrice() and listprice() functions
    'these are the base prices - the channels (not the accounts) contain margins 
    '                                                     seller                  null/buyer
    'Private i_Prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, Dictionary(Of clsVariant, clsPrice))))
    'Private i_Prices As Dictionary(Of clsChannel, Dictionary(Of clsVariant, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))))

    'Seller,Variant,ArrivalDate,Stock(contains variant)
    'Public i_Stock As Dictionary(Of clsChannel, Dictionary(Of clsVariant, SortedDictionary(Of Date, clsStock)))

    '  Property stock As Dictionary(Of Integer, clsStock)  'it (still) makes sense to have'global' stock and price held in the product
    'Property prices As Dictionary(Of Integer, clsPrice)
    Property activeFrom As DateTime        'Product will only display between these dates
    Property activeTo As DateTime          '
    Property Active As Boolean             'Wether the product shows (at all) or not
    Property EOL As Boolean                'End of life - product will only show if it has stock
    Property Publish As Boolean            'Only admin users see unpublished products
    Property AvalancheOPGs As Dictionary(Of Integer, ClsAvalancheOPG)
    Property OPGflexLines As Dictionary(Of Integer, clsFlexLine)
    Property Variants As Dictionary(Of Integer, clsVariant) 'a flat dictionary referencing the variants - to allow editing (would be nicer if the generic edito could evaluate more complex paths and edit lists (aswell as dictionaries)
    Public Bundles As Dictionary(Of Integer, clsBundle)  'this isn't a property - so it's not exposed in the editor we edit iq.bundles
    Public Points As Dictionary(Of clsScheme, Integer) ' how many points does this product have under each scheme
    Property Promos As Dictionary(Of String, List(Of clsRegion))

    'New for HP Split
    Property mfrCode As String
    Property plCode As String
    Property buCode As String
    Property SKU As String


    '                                           seller                           
    '' <summary>Provides access to a List of sellerChannel specific variants of the product (which in turn have prices) </summary>
    Public i_Variants As Dictionary(Of clsChannel, List(Of clsVariant))

    'Geographical visibility is controlled by Quantity records - which relate products to regions... see also

    Property Attributes As Dictionary(Of Integer, clsProductAttribute) ' this is a 'flattened' dictionary for the editor (a product can have more than one attribute of the same type - i_Attributes_code groups them by type and makes thank indexable (e.g. SKU(0)) 
    'This MUST be a property as we use reflection to access productattributes in clsFields defined in a clsScreen (for the editor/matrix views)
    Property i_Attributes_Code As Dictionary(Of String, List(Of clsProductAttribute)) 'An index of the attributes by code, - NOTE: because we can have more than one attribute of the same type (eg, more than one xText, or description) this is an index to a LIST of attributes 
    Private _isSystem As Boolean
    Private _isOption As Boolean
    Property isDeleted As Boolean

    ''' <summary>Returns the HP/Everyone Price of the Variant that matches the buyerAccounts Region and Currency</summary>
    Public Function ListPrice(buyeraccount As clsAccount) As clsPrice

        ListPrice = Nothing
        Dim hplist As clsPriceBand = iq.getPriceBand("") 'Hplist -dosnt need a 'special' band - becuase it's the 'everyone' band on the HP sellerChannel

        If Me.i_Variants IsNot Nothing Then
            If Me.i_Variants.ContainsKey(hp) Then
                For Each v In Me.i_Variants(HP)  'Not wildly happy with this - walking over (say) 50 list prices pre product could be slow
                    'Making i_variants a compound key SellerChannel|Region would be much faster
                    If v.Region.Code <> "US" Then
                        Dim a = 0
                    End If
                    If v.Region Is buyeraccount.SellerChannel.Region Then 'Base LIST PRICES on SELLERS REGION (not buyers) - ultimately the account should have a region
                        If v.i_prices.ContainsKey(hplist) Then
                            If v.i_prices(hplist).ContainsKey(buyeraccount.Currency) Then
                                ListPrice = v.i_prices(hplist)(buyeraccount.Currency)
                                Exit Function
                            End If
                        End If
                    End If
                Next
            End If
        End If
        'ListPrice = Me.prices(Everyone)(buyeraccount.Currency)

    End Function

    Public Function clone(newsku As String)

        With Me
            Return New clsProduct(newsku, .isSystem, .isOption, .Sector, .ProductType, .activeFrom, .activeTo, .Active, .EOL, .Publish, .mfrCode, .buCode, .plCode)
        End With

    End Function

    Public Function FirstAttributeEnglishText(code) As String

        If Me.i_Attributes_Code.ContainsKey(code) Then
            Return Me.i_Attributes_Code(code)(0).Translation.text(English)
        End If

        Return ""

    End Function

    Public ReadOnly Property isFIO() As Boolean

        'NOT to be confused with preinstalled parts !!

        'FIOs (Factory Installed Options) - ahve part numbers - but can't be 'bought' - and therefore be flexed
        '(in practise there is probably an equevilent (often identical) part - but it has a different (and unknwon to us) SKU

        Get
            If Me.i_Attributes_Code.ContainsKey("focus") Then
                For Each f In Me.i_Attributes_Code("focus")
                    If f.Translation.text(English) = ("FIO") Then
                        Return True
                    End If
                Next
            End If
        End Get

    End Property

    Property isSystem(Optional path As String = "") As Boolean
        Get
            If path <> "" And _isSystem Then
                If Split(path, ".").Length < 6 Then
                    Return _isSystem
                Else
                    Return False
                End If
            Else
                Return _isSystem
            End If

        End Get
        Set(value As Boolean)
            _isSystem = value
        End Set
    End Property

    Property isOption() As Boolean
        Get
            Return _isOption
        End Get
        Set(value As Boolean)
            _isOption = value
        End Set
    End Property

    Public ReadOnly Property Manufacturer As Manufacturer

        Get

            Manufacturer = Manufacturer.Unknown

            If String.IsNullOrEmpty(Me.mfrCode) Then

                ' No mfrCode found - make the decsion based on product types
                If Me.isSystem Then
                    If Me.ProductType.Code = "DTO" OrElse Me.ProductType.Code = "NBK" Then
                        Manufacturer = Manufacturer.HPI
                    Else
                        Manufacturer = Manufacturer.HPE
                    End If
                End If

            Else

                ' mfrCode set up - use it to work out the manufacturer
                If String.Equals(Me.mfrCode, "HPI", StringComparison.InvariantCultureIgnoreCase) Then
                    Manufacturer = Manufacturer.HPI
                ElseIf String.Equals(Me.mfrCode, "HPE", StringComparison.InvariantCultureIgnoreCase) Then
                    Manufacturer = Manufacturer.HPE
                End If

            End If

        End Get

    End Property

    'Public ReadOnly Property sku() As String
    '    Get
    '        If Me.i_Attributes_Code.ContainsKey("MfrSKU") Then
    '            Return Me.i_Attributes_Code("MfrSKU")(0).displayName(English)
    '        Else
    '            Return ""
    '        End If
    '    End Get
    'End Property

    Public Function inFeed(sellerchannel As clsChannel) As Boolean

        If Left(sellerchannel.Code, 3) = "MHP" Then Return True 'Temporary hack becuase otherwise you cannot flex up in Universal

        If Me.i_Variants Is Nothing Then Return False '*Nobody* stocks (or has a price for me)
        If Not Me.i_Variants.ContainsKey(sellerchannel) Then Return False
        Return True

    End Function


    Public Function MatchingVariant(MatchWith As clsVariant, sellerchannel As clsChannel) As clsVariant

        'looks thtough all the variants on this Product ie. me
        'for the one which most closely matches 'matchwith'

        '    Product.matchingvariant(quoteItem.SKUVariant, buyerAccount.SellerChannel)

        'niave - but clear - hopefully

        MatchingVariant = Nothing

        Dim bestscore As Integer = 0
        Dim score As Integer
        Dim channel As clsChannel

        If Me.i_Variants IsNot Nothing Then

            If Me.i_Variants.ContainsKey(sellerchannel) Then channel = sellerchannel Else channel = HP

            If Me.i_Variants.ContainsKey(channel) Then
                For Each v In Me.i_Variants(channel)
                    score = 0
                    If v.Warehouse = MatchWith.Warehouse Then score = score + 1
                    If v.Localisation = MatchWith.Localisation Then score = score + 1
                    If v.Code = MatchWith.Code Then score = score + 1
                    If v.Region Is MatchWith.Region Then score += 1
                    If score > bestscore Then MatchingVariant = v : bestscore = score
                Next v
            End If

        End If

    End Function


    Public Function clone() As clsProduct

        clone = New clsProduct(Me.SKU, Me.isSystem, Me.isOption, Me.Sector, Me.ProductType, Me.activeFrom, Me.activeTo, Me.Active, Me.EOL, Me.Publish, Me.mfrCode, Me.buCode, Me.plCode)

        'clone the HP variant(s)  (which internally clone the prices)
        Dim cv As clsVariant

        If Me.i_Variants.ContainsKey(HP) Then
            For Each v In Me.i_Variants(HP)
                cv = v.clone(clone)
            Next
        End If

        Dim pa As clsProductAttribute
        For Each pa In Me.Attributes.Values
            pa.Clone(clone)  'clone my product attributes onto the new (cloned) product
        Next


    End Function

    Public Function anyStock(SellerChannel As clsChannel) As Boolean

        'returns wether there is any present or future stock for any variant
        anyStock = False

        'Each variant will have many shipments - any before 'now' are absolute stock values at that date and should have current = false
        'there should only be 1 after 'now' who's 'current=true' - others after now are (relative) shipments
        If Me.i_Variants.ContainsKey(SellerChannel) Then
            For Each v In Me.i_Variants(SellerChannel)
                For Each s In v.shipments.Values
                    If s.IsCurrent Or s.Arrival > Now Then
                        If s.quantity > 0 Then Return True 'yey ! - some stock
                    End If
                Next
            Next
        End If

    End Function

    Public Sub New()

        AvalancheOPGs = New Dictionary(Of Integer, ClsAvalancheOPG)
        OPGflexLines = New Dictionary(Of Integer, clsFlexLine)
        Me.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
        Me.Variants = New Dictionary(Of Integer, clsVariant)
        Me.Points = New Dictionary(Of clsScheme, Integer) 'number of points this product is worth under each scheme
        Me.Promos = New Dictionary(Of String, List(Of clsRegion))()
        Me.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
    End Sub

    Public Function insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsProduct(Me.SKU, Me.isSystem, Me.isOption, Me.Sector, Me.ProductType, Me.activeFrom, Me.activeTo, Me.Active, Me.EOL, Me.Publish, Me.mfrCode, Me.buCode, Me.plCode)

    End Function

    Public Sub update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        If Me.ID = 0 Then Stop

        Dim sql$
        sql$ = "UPDATE [Product] SET sku='" & Me.SKU & "', isSystem=" & IIf(Me.isSystem, 1, 0) & ", isOption=" & IIf(Me.isOption, 1, 0) & ",fk_producttype_id=" & Me.ProductType.ID & ",fk_sector_id=" & Me.Sector.ID
        sql$ &= ",activefrom=" & da.UniversalDate(Me.activeFrom) & ",activeTo=" & da.UniversalDate(Me.activeTo) & ",active=" & IIf(Me.Active, 1, 0) & ",eol=" & IIf(Me.EOL, 1, 0) & ",publish=" & IIf(Me.Publish, 1, 0)
        sql$ &= ",mfrCode=" & da.SqlEncode(Me.mfrCode) & ",buCode=" & da.SqlEncode(Me.buCode) & ", deleted=" & IIf(Me.isDeleted, 1, 0) & ",plCode=" & da.SqlEncode(Me.plCode)
        sql$ &= " WHERE ID=" & Me.ID
        da.DBExecutesql(sql$)

    End Sub

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        Try
            da.DBExecutesql("DELETE  FROM PRODUCT WHERE ID=" & Me.ID)  'will often fail due to RI (expose this error through the editor)
            iq.i_SKU.Remove(Me.sku)
            iq.Products.Remove(Me.ID)

        Catch ex As Exception
            errorMessages.Add(ex.Message.ToString)

        End Try

    End Sub

    Public Function DisplayName(clsLanguage As clsLanguage, Optional StripSKU As Boolean = False) As String
        'Show the description - falling back to SKU (if absent)

        DisplayName = Me.sku

        If Me.i_Attributes_Code.ContainsKey("desc") Then
            DisplayName = If(Not StripSKU, Me.sku & " - ", "") & Me.i_Attributes_Code("desc")(0).Translation.text(English)

        End If
    End Function
    Public Function displayName(clsLanguage As clsLanguage) As String Implements i_Editable.displayName

        Return DisplayName(clsLanguage, False)

    End Function


    Public Function hasSKU() As Boolean

        'Returns true if the product has a 'real' (non ### SKU)

        hasSKU = False
        If Me.SKU <> "" Then Return True

        Return False

        'this is obsolete as product will have their sku set at load time (it if is empty),
        ' from the product attribute if it is present.

        If Me.SKU <> "" Then
            Dim sku$
            sku = Me.SKU
            'If Left$(sku$, 3) <> "###" Then
            'End If
            Return True
        End If

    End Function
    'Public Function SKU() As String

    '    If Not Me.i_Attributes Is Nothing Then
    '        If Me.i_attributes_code.containskey("MfrSKU") Then
    '            SKU = Me.i_attributes_code("MfrSKU").Translation.Text(s_lang)
    '            If Left$(SKU$, 3) = "###" Then
    '                SKU$ = "-"
    '            End If
    '        End If
    '    End If

    'End Function

    ''' <summary>Returns the 'HP','Everyone' price for the variant with the specified Region and Currency (which will be a list price)
    ''' 
    ''' </summary>
    ''' <param name="skuvariant"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>

    Function GetPrices(Buyeraccount As clsAccount, PriceConfig As Integer, SKUvariant As clsVariant, ByRef errormessages As List(Of String), callWebservice As Boolean) As List(Of clsPrice)

        GetPrices = New List(Of clsPrice) 'fail safe (return SOMETHING)
        If SKUvariant Is Nothing Then errormessages.Add("* SkuVariant was Nothing in getprices) - Use iq.allvariant") : Exit Function

        'Called form StalePrices()
        'Returns a list of prices for the buyer account based on the PriceConfig of the sellerchannel 
        'Pricing may be customer specific,margin based, may queue a webservice based price/stock update or may be HP list price for the buyeraccounts region.
        'Pricing will only be of one type for a single call - ie. it will not return List and CustomerSpecific prices in the same list

        'an empty list is returned if the are no prices - getPrices never returns Nothing

        'PriceConfig  AND 8 Inclide customer specific prices
        'PriceConfig  AND 4 Use Price bands
        'PriceConfig  AND 2 Show List price (in the absence of any other price)
        'PriceConfig  AND 1 Show POA (for products for which we have no price (as opposed to not showing the product at all if there is no price at all)

        Dim sku$
        sku$ = Me.SKU

        Dim SpecificPrices As List(Of clsPrice) = New List(Of clsPrice)
        Dim BasePrices As List(Of clsPrice) = New List(Of clsPrice)
        Dim ListPrices As List(Of clsPrice) = New List(Of clsPrice)

        'returns customer specific prices - Includng potenitally outstanding web request  POA 'Requesting prices' prices
        'also does margin based pricing ! - note, you can't add a margin to a webservice price
        SpecificPrices = Me.Prices(Buyeraccount, SKUvariant)

        'This IS DELIBERATE - DON'T undo it         \/-------------------------------\/ - the base price is the sellers price to themself
        'BasePrices = Me.Prices(Buyeraccount.SellerChannel, Buyeraccount.SellerChannel, Buyeraccount.Currency, SKUvariant)

        'With Buyeraccount.SellerChannel

        'if we're using a webserive - we return the (last known) specific price
        '8 = Customer specific prices

        'If (PriceConfig And 8) And SpecificPrices.Count > 0 Then
        '    'Some prices are loaded with 0's - invalidate them (so the basket will update correctly)
        '    If SpecificPrices(0).Price.NumericValue = 0 And SpecificPrices(0).Price.valid = True Then
        '        SpecificPrices(0).Price.valid = False
        '        SpecificPrices(0).Price.Message = "Was 0.. checking with webservice"
        '    End If
        '    Return SpecificPrices
        'End If

        'ListPrices = New List(Of clsPrice)
        'If Me.ListPrice(Buyeraccount) IsNot Nothing Then
        '    ListPrices.Add(Me.ListPrice(Buyeraccount))
        'Return ListPrices
        'End If

        'mask off the webservice BIT on univeral instances
        If Left$(Buyeraccount.SellerChannel.Code, 3) = "MHP" Then PriceConfig = PriceConfig And Not 8 'HP (universal instances)  dont have a webservice (temporary hack)

        If (PriceConfig And 8) And callWebservice Then 'And (PriceConfig And 16) Then
            'we're using a webservice.. and there was no specific price YET..
            'IF this poduct is in the feed (we have a variant) return a 'pending' price (and DO show the product)


            'BRAZIL (see clsAccount.warehousefilter)
            If Buyeraccount.wareHouseFilter.ToUpper <> "NONE" Then
                If Me.inFeed(Buyeraccount.SellerChannel.IsCloneOf) Then 'ChannelSKU(Me, SKUvariant, Buyeraccount.SellerChannel) <> "" Then
                    Dim poa As List(Of clsPrice)
                    poa = New List(Of clsPrice)

                    'will make a new price (for every variant!) - A COPY of the HP list price  ''storing a POA, until the webservice returns a real price (for the first time)

                    Dim aPrice As clsPrice
                    Dim aStock As clsstock ' Stock is really just a collection of all stock positions

                    'these are the disti variants - they're never going to be HP list price ones
                    For Each v In Me.i_Variants(Buyeraccount.SellerChannel.IsCloneOf).ToList  'tolist is new (was getting a collection has been modified enumeration may not execute)
                        If Buyeraccount.wareHouseFilter = "" Or String.Equals(Buyeraccount.wareHouseFilter, v.Code, StringComparison.CurrentCultureIgnoreCase) Then
                            If Not v.Deleted Then

                                'need to see if there is a customer specific price first - if not, clone the list price if these is one,
                                'otherwise make a POA

                                If v Is SKUvariant Or SKUvariant Is iq.AllVariants Then

                                    aPrice = v.priceFor(Buyeraccount.Priceband, Buyeraccount.Currency)

                                    If aPrice Is Nothing Then
                                        'we have no price customer specific price

                                        Dim lp As clsPrice = ListPrice(Buyeraccount)
                                        If lp Is Nothing Then

                                            aPrice = New clsPrice(v, Buyeraccount.Priceband, New NullablePrice(Buyeraccount.Currency), "Requesting price.." & Format(Now, "ddd hh:nn"))

                                            'aPrice.Price = New NullablePrice(Buyeraccount.Currency)
                                            'aPrice.SKUVariant = v
                                            'aPrice.PriceBand = Buyeraccount.Priceband
                                            'aPrice.Price.isValid = False
                                        Else
                                            'Clone the HP list PRICE (not variant) into a new customer specific record PRICE
                                            '(attached to the exsiting disti Variant)
                                            'prior to calling the webservice for a 'real' (Updated, customer specific) price

                                            aPrice = New clsPrice(v, Buyeraccount.Priceband, lp.Price, "LP clone")
                                            aPrice.Price.isList = True
                                            'force it into the stale list
                                            aPrice.lastRequested = DateAdd(DateInterval.Minute, -100, Now)

                                        End If
                                    End If

                                    If v.shipments.Count = 0 Then
                                        'similarly, we need to make a stock record - so we can render a DIV with a valid S_Id - to be replaced when the webservice returns - without this wee see 'X'  stock when first using the system
                                        'If aPrice.ID <> -1 Then 'not a POA
                                        aStock = New clsstock(v, -1, Now, "initial", True)
                                        ' End If
                                    End If
                                    'End If
                                    poa.Add(aPrice)
                                End If

                            End If
                        End If
                    Next v

                    Return poa
                Else
                    'Changes to Display CarePack Blank Prices
                    If Me.ListPrice(Buyeraccount) IsNot Nothing Then
                        'fetch HPs list price according to the buyers region and currency - NB list pricing is per country at the moment (ie. there is no EMEA list price)
                        ListPrices.Add(Me.ListPrice(Buyeraccount))
                        Return ListPrices
                    Else
                        ' Stop
                    End If
                    'not in their feed
                    ' return an EMPTY list - which supresses display (feed only skus)
                    Return New List(Of clsPrice) 'return a list of 0 prices

                End If
            End If


            'ElseIf (PriceConfig And 8) And Not callWebservice Then
            '    'this is the code path for checeks of whether a product *could* have a price (from the webservice)
            '    'these tend to be the big, expensige resurvice ones for working out whether to display squares etc.
            '    Dim couldhave As List(Of clsPrice) = New List(Of clsPrice)
            '    If Me.inFeed(Buyeraccount.SellerChannel) Then 'ChannelSKU(Me, SKUvariant, Buyeraccount.SellerChannel) <> "" Then
            '        couldhave.Add(Nothing) 'this is a POA
            '    End If
            '    Return couldhave
        End If

        'If Not Me.inFeed(Buyeraccount.SellerChannel) Then Return New List(Of clsPrice) 'return a list of 0 prices

        'Everyone' IS the Base Channel - this is loosely equivilent to the IQ1 'external' price - for implementations with a webservice
        If (PriceConfig And 4) Then  'use price bands
            BasePrices = Me.Prices(Buyeraccount, SKUvariant) '.Priceband, Everyone, Buyeraccount.Currency) ', SKUvariant)
            If BasePrices.Count > 0 Then Return BasePrices
        End If

        '   If PriceConfig And 2 Then
        ' Buyeraccount.Currency = iq.i_currency_code("USD")


        If Me.ListPrice(Buyeraccount) IsNot Nothing Then
            'fetch HPs list price according to the buyers region and currency - NB list pricing is per country at the moment (ie. there is no EMEA list price)
            ListPrices.Add(Me.ListPrice(Buyeraccount))
            Return ListPrices
        Else
            ' Stop
        End If




        '    End If

        ''this is effectively a 'show everything' flag - becuase it will make a price on products that are geographically out of scope
        If (PriceConfig And 1) <> 0 Then

            Dim poa As List(Of clsPrice)
            poa = New List(Of clsPrice)

            'If Me.i_Variants(sellerchannel).
            'Dim aprice As clsPrice
            'aprice = New clsPrice(Buyeraccount, New clsVariant(-1, "POA", Me, Buyeraccount.SellerChannel, "", "", "", "", r_worldwide, False, False))

            poa.Add(Nothing) 'return a list containing a single nothing ' This is a POA
            Return poa
        End If

        'if ALL else fails - we wont show the product  return an EMPTY list
        Return New List(Of clsPrice) 'return a list of 0 prices

    End Function


    Public Sub New(SKu As String, ByVal Name As String, isSystem As Boolean, isOption As Boolean, sector As clsSector, productType As clsProductType, activeFrom As DateTime, ActiveTo As DateTime, Active As Boolean, EOL As Boolean, Publish As Boolean, mfrCode As String, buCode As String, plCode As String)

        Me.New(SKu, isSystem, isOption, sector, productType, activeFrom, ActiveTo, Active, EOL, Publish, mfrCode, buCode, plCode) 'call the 'normal' constructor to make an instance and populate me.id

        'This is a 'quick' method to create a product with a single attribute (of Name).. and to fill that attribute with a new text object in English carrying the description
        'you *generally* dont want to be doing that - but it's useful for creating some of the metadata

        If SKu <> "" Then
            iq.i_SKU.Add(SKu, Me)
        End If

        Dim desc As clsProductAttribute
        desc = New clsProductAttribute(Me, iq.i_attribute_code("Name"), 0, iq.i_unit_code("txt"), iq.AddTranslation(Name$, s_lang, Nothing, 0, Nothing, 0, True))

        AvalancheOPGs = New Dictionary(Of Integer, ClsAvalancheOPG)
        OPGflexLines = New Dictionary(Of Integer, clsFlexLine)
        Me.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
        Me.Variants = New Dictionary(Of Integer, clsVariant)
        Me.i_Attributes_Code = New Dictionary(Of String, List(Of clsProductAttribute))(StringComparer.CurrentCultureIgnoreCase)
        Me.Promos = New Dictionary(Of String, List(Of clsRegion))()

    End Sub
    Public Sub New(sku As String, IsSystem As Boolean, IsOption As Boolean, Sector As clsSector, ProductType As clsProductType, activeFrom As DateTime, _
                   ActiveTo As DateTime, Active As Boolean, EOL As Boolean, Publish As Boolean, _
                   mfrCode As String, buCode As String, plCode As String, Optional wc As DataTable = Nothing, Optional ByRef nextid As Integer = -1, Optional InMemoryOnly As Boolean = False)

        Me.Attributes = New Dictionary(Of Integer, clsProductAttribute)
        Me.i_Attributes_Code = New Dictionary(Of String, List(Of clsProductAttribute))(StringComparer.CurrentCultureIgnoreCase)


        If sku = "" And (IsSystem Or IsOption) Then Stop

        If IsSystem = 0 And IsOption = 0 Then
            Dim jjj = 0
        End If


        Me.SKU = sku
        Me.isSystem = IsSystem
        Me.isOption = IsOption
        Me.Sector = Sector
        Me.ProductType = ProductType
        Me.activeFrom = activeFrom
        Me.activeTo = ActiveTo
        Me.Active = Active
        Me.EOL = EOL
        Me.Publish = Publish

        AvalancheOPGs = New Dictionary(Of Integer, ClsAvalancheOPG)
        OPGflexLines = New Dictionary(Of Integer, clsFlexLine)
        Me.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
        Me.Variants = New Dictionary(Of Integer, clsVariant)
        Me.Points = New Dictionary(Of clsScheme, Integer) 'number of points this product is worth under each scheme
        Me.Promos = New Dictionary(Of String, List(Of clsRegion))()

        Me.mfrCode = mfrCode  'broad
        Me.buCode = buCode    'narrow
        Me.plCode = plCode    'narrower

        If Not InMemoryOnly Then
            If sku <> "" Then
                iq.i_SKU.Add(sku, Me)
            End If

            'add myself to the master list
            If wc IsNot Nothing Then
                Me.ID = nextid
                nextid += 1

                Dim row As System.Data.DataRow
                row = wc.NewRow()
                row("ID") = Me.ID '- we EXPLICITLY set ids ?
                row("sku") = Me.SKU
                row("issystem") = Me.isSystem
                row("isoption") = Me.isOption
                row("fk_producttype_id") = Me.ProductType.ID
                row("fk_sector_id") = Me.Sector.ID
                row("activefrom") = Me.activeFrom
                row("activeTo") = Me.activeTo
                row("active") = Me.Active
                row("eol") = Me.EOL
                row("publish") = Me.Publish
                row("mfrCode") = Me.mfrCode
                row("buCode") = Me.buCode
                row("plCode") = Me.plCode
                row("deleted") = False

                wc.Rows.Add(row)

            Else

                Dim sql$
                sql$ = "INSERT INTO PRODUCT (sku,issystem,isoption,fk_producttype_id,fk_sector_id,activefrom,activeto,active,eol,publish,mfrCode,buCode,plCode )"
                sql$ &= " VALUES (" & da.SqlEncode(sku) & "," & IIf(IsSystem, 1, 0) & "," & IIf(IsOption, 1, 0) & "," & ProductType.ID & "," & Sector.ID & "," & da.UniversalDate(activeFrom) & "," & da.UniversalDate(ActiveTo) & "," & IIf(Active, 1, 0) & "," & IIf(EOL, 1, 0) & "," & IIf(Publish, 1, 0) & ","
                sql$ &= da.SqlEncode(mfrCode) & "," & da.SqlEncode(buCode) & "," & da.SqlEncode(plCode) & ");"

                Me.ID = da.DBExecutesql(sql$, True)
            End If

            iq.Products.Add(Me.ID, Me)
        End If

        'Prices = New Dictionary(Of clsChannel, Dictionary(Of clsBuyerGroup, Dictionary(Of clsCurrency, clsPrice)))
        'i_Prices = New Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, Dictionary(Of clsVariant, clsPrice))))
        'i_Prices = New Dictionary(Of clsChannel, Dictionary(Of clsVariant, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))))
        'i_Stock = New Dictionary(Of clsChannel, Dictionary(Of clsVariant, SortedDictionary(Of Date, clsStock)))
        ' Stock = New Dictionary(Of Integer, clsStock)
        ' Prices = New Dictionary(Of Integer, clsPrice)

    End Sub


    Public Sub New(ByVal id As Integer, sku As String, isSystem As Boolean, isOption As Boolean, sector As clsSector, ProductType As clsProductType, activeFrom As DateTime, ActiveTo As DateTime, Active As Boolean, EOL As Boolean, Publish As Boolean, mfrCode As String, buCode As String, plCode As String)

        'If sku <> "" And (isSystem Or isOption) Then

        Me.ID = id

        Me.SKU = sku
        Me.Attributes = New Dictionary(Of Integer, clsProductAttribute)
        Me.i_Attributes_Code = New Dictionary(Of String, List(Of clsProductAttribute))(StringComparer.CurrentCultureIgnoreCase)
        Me.Sector = sector
        Me.ProductType = ProductType

        If sku <> "" Then
            iq.i_SKU.Add(sku, Me)
        End If

        'i_Prices = New Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, Dictionary(Of clsVariant, clsPrice))))
        'i_Prices = New Dictionary(Of clsChannel, Dictionary(Of clsVariant, Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))))
        'i_Stock = New Dictionary(Of clsChannel, Dictionary(Of clsVariant, SortedDictionary(Of Date, clsStock)))
        ' Stock = New Dictionary(Of Integer, clsStock)
        ' Prices = New Dictionary(Of Integer, clsPrice)
        Me.AvalancheOPGs = New Dictionary(Of Integer, ClsAvalancheOPG)
        Me.OPGflexLines = New Dictionary(Of Integer, clsFlexLine)

        Me.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
        Me.Variants = New Dictionary(Of Integer, clsVariant)
        Me.Points = New Dictionary(Of clsScheme, Integer) 'number of points this product is worth under each scheme

        Me.activeFrom = activeFrom
        Me.activeTo = ActiveTo
        Me.Active = Active
        Me.EOL = EOL
        Me.Publish = Publish
        Me.isSystem = isSystem
        Me.isOption = isOption
        Me.Promos = New Dictionary(Of String, List(Of clsRegion))()

        Me.mfrCode = mfrCode
        Me.buCode = buCode
        Me.plCode = plCode
        'End If

    End Sub

    'Public Function HasPrices() As Boolean

    '    HasPrices = Me.i_Prices.Count > 0

    'End Function

    'Public Function VariantPrice(BuyerAccount As clsAccount, SKUvariant As clsVariant, ByRef errorMessages As List(Of String)) As NullablePrice

    '    'fetches the single price for a speficied product variant  for a specified account
    '    'It will be either specific, margin based, list or POA.. all dependent upon the seller channels [priceConfig]

    '    Dim OnePrice As List(Of clsPrice)
    '    OnePrice = Me.Prices(BuyerAccount, SKUvariant) 'get the one matching price
    '    If OnePrice Is Nothing Then
    '        Return New NullablePrice(BuyerAccount.Currency)
    '    ElseIf OnePrice.Count = 1 Then
    '        Return OnePrice(0).Price
    '    Else
    '        Return OnePrice(0).Price
    '        errorMessages.Add("* more then one price for the same variant !")
    '    End If
    'End Function


    ''' <summary>
    ''' Returns a string, representing the amount of (current) stock of the specified variant - or the total stock of all variants if skuvariant is ommitted
    ''' POPULATES the NumericValue supplied - with 
    ''' </summary>
    Public Function CurrentStock(buyeraccount As clsAccount, ByRef numericValue As Integer, whichVariant As clsVariant, ByRef errorMessages As List(Of String)) As String

        If whichVariant Is Nothing Then
            errorMessages.Add("* Whichvariant was Nothing for CurrentStock()")
            numericValue = -10
            CurrentStock = "-"
        Else

            'NB: We fetch stock from the sellerchannels 'isCloneOf' - which for non-clones, is itself.
            If Me.i_Variants.ContainsKey(buyeraccount.SellerChannel.IsCloneOf) Then
                CurrentStock = "-" 'iq.EnglishIndex("Unknown").text(buyeraccount.Language) 'should be agentaccount.language really
                For Each sv In Me.i_Variants(buyeraccount.SellerChannel.IsCloneOf)
                    'If Me.i_Stock(buyeraccount.SellerChannel.IsCloneOf).ContainsKey(skuVariant) Then
                    If sv Is whichVariant Or whichVariant Is iq.AllVariants Then
                        For Each shipment In sv.shipments.Values
                            If shipment.IsCurrent And shipment.Arrival < Now Then
                                numericValue += shipment.quantity
                                If whichVariant IsNot Nothing Then CurrentStock = numericValue.ToString : Exit Function 'we're done
                            End If
                        Next
                        CurrentStock = numericValue.ToString : Exit Function 'we're done (totaled for all skuvariants)
                    Else
                        CurrentStock = "-" '  iq.EnglishIndex("Unstocked Variant").text(buyeraccount.Language)
                        numericValue = -1
                    End If
                Next
            Else
                CurrentStock = "no record" 'iq.EnglishIndex("Unstocked").text(buyeraccount.Language)
                numericValue = -2
            End If

            If numericValue < 0 Then CurrentStock = numericValue.ToString
        End If

    End Function
    Function BasePrices(buyerAccount As clsAccount, whichVariant As clsVariant) As List(Of clsPrice)

        'Returns all of a seller channels prices for this product in the buyerAccounts currency - there may be many variants 
        'If SKUVariant is is suppled - only that variant is returned

        BasePrices = Nothing
        Dim sellerChannel As clsChannel = buyerAccount.SellerChannel

        If Me.i_Variants.ContainsKey(sellerChannel) Then
            For Each v In Me.i_Variants(sellerChannel)
                If v Is whichVariant Or whichVariant Is iq.AllVariants Then
                    If BasePrices Is Nothing Then BasePrices = New List(Of clsPrice)
                    BasePrices.Add(v.i_prices(iq.getPriceBand(""))(buyerAccount.Currency))
                End If
            Next
        End If

    End Function

    'Public Function listPrice(country As clsRegion) 'Currency As clsCurrency, Optional SKUVariant As clsVariant = Nothing) As List(Of clsPrice)

    '    'returns the HP list price for every SKUvariant of the product available in the specified currency
    '    'optionally returns List Price for only One (specified) variant
    '    listPrices = Nothing

    '    Dim hp As clsChannel
    '    hp = iq.i_channel_code("HP")
    '    If Me.i_variants IsNot Nothing Then
    '        If Me.i_variants.ContainsKey(hp) Then  'the first dimension of the product.i_variants is the seller channel
    '            For Each sv In Me.i_variants(hp) 'each of those contains a LIST of clsVariant
    '                If SKUVariant Is Nothing Or sv Is SKUVariant Then
    '                    If sv.prices.ContainsKey(Everyone) Then
    '                        If sv.prices(Everyone).ContainsKey(Currency) Then
    '                            If listPrices Is Nothing Then listPrices = New List(Of clsPrice)
    '                            listPrices.Add(sv.prices(Everyone)(Currency))
    '                        End If
    '                    End If
    '                End If
    '            Next
    '        End If
    '    End If

    'End Function


    'Public Function ListPrice(Currency As clsCurrency) As clsPrice

    '    'list pricing is the price of HP (the seller) to the everyone channel - for the first variant (there shoudl only be one!)

    '    Dim hp As clsChannel
    '    hp = iq.i_channel_code("HP") 'If this is missing - it's because you havent imported list prices (see Default.aspx !)

    '    'ListPrice = New clsPrice() : ListPrice.Seller = hp : ListPrice.Buyer = Everyone : ListPrice.Price = New nullablePrice(Currency) : ListPrice.ID = -1 : ListPrice.DateStamp = Now : ListPrice.SKUVariant = skuvariant : ListPrice.Source = ""
    '    '    Property Variants As Dictionary(Of clsChannel, Dictionary(Of Integer, clsVariant))

    '    If Me.i_variants.ContainsKey(hp) Then
    '        If Me.i_variants(hp).ContainsKey(Everyone) Then
    '            If Me.i_variants(hp)(Everyone).prices.ContainsKey(Currency) Then
    '            End If
    '        End If
    '        If Not Me.i_Prices Is Nothing Then
    '            If Me.i_Prices.ContainsKey(hp) Then
    '                If Me.i_Prices(hp).Count = 1 Then
    '                    'the first (0'th) variant
    '                    If Me.i_Prices(hp).Values(0).ContainsKey(Everyone) Then  'is there a price in this accounts currency
    '                        If Me.i_Prices(hp).Values(0)(Everyone).ContainsKey(Currency) Then
    '                            ListPrice = i_Prices(hp).Values(0)(Everyone)(Currency) '.Price
    '                            ListPrice.Price.Message = "List price"
    '                        Else
    '                            Dim aprice As clsPrice = New clsPrice(Me.i_variants(hp).Values(0), Everyone, New nullablePrice(Currency), "No list price available in the currency")
    '                            aprice.Price.valid = False
    '                            Return aprice
    '                        End If
    '                    Else
    '                        Stop
    '                    End If
    '                Else
    '                    'ut oh.. more than one list price variant !
    '                    Stop
    '                End If
    '            End If
    '        End If
    'End Function

    Public Function Prices(buyeraccount As clsAccount, whichVariant As clsVariant) As List(Of clsPrice)

        With buyeraccount
            'call the other (more granular) overload
            Return Prices(.SellerChannel, .BuyerChannel, .Priceband, .Currency, whichVariant)
        End With

    End Function

    Public Function Prices(sellerchannel As clsChannel, buyerchannel As clsChannel, priceband As clsPriceBand, currency As clsCurrency, whichvariant As clsVariant) As List(Of clsPrice)

        'returns The Prices for all (or the specified) SKUVariant(s) for the specified buyer, - at the correct margin/multiplier for the buyer/seller combo
        'will return an empty list if there is no price (POA)

        Dim ret As List(Of clsPrice) = New List(Of clsPrice)
        Dim margin As clsMargin

        'NB: We fetch pricing from the sellerchannels 'isCloneOf' - which for non-clones, is itself.
        Dim sourceChannel As clsChannel = sellerchannel.IsCloneOf

        'If we're getting prices for a clone..
        'There's an important distinction between the SourceChannel and the SellerChannel - the sellerchannel (which may be a clone) carries the margin for the buyer..
        'whereas the sourceChannel (the clones 'parent') has the base price

        Dim done As List(Of clsVariant)
        done = New List(Of clsVariant)

        'First get SPECIFIC (overriding)prices
        If Me.i_Variants IsNot Nothing Then
            If Me.i_Variants.ContainsKey(sourceChannel) Then 'does the SOURCE channel seller have a (base) price for this product            
                For Each v In Me.i_Variants(sourceChannel)     'The variants are *not* buyer specific - there are a small number of variants (and a large number of prices)
                    If v Is whichvariant Or whichvariant Is iq.AllVariants Then
                        'there are *some* specific prices for this product - for this buyer (perhaps not for the right variants or currencies
                        If v.i_prices.ContainsKey(priceband) Then
                            If v.i_prices(priceband).ContainsKey(currency) Then
                                ret.Add(v.i_prices(priceband)(currency))  'we've found a specifc price - to which we should NOT apply margin !
                                done.Add(v)
                            End If
                        End If
                    End If
                Next
            End If

            'for any remaining unpriced - do margin based pricing
            If buyerchannel IsNot Nothing Then
                If sellerchannel.Margin.ContainsKey(buyerchannel) Then 'does this seller have ANY margin specified for this buyer
                    If sellerchannel.Margin(buyerchannel).ContainsKey(Me.Sector) Then 'is there a margin specified for products within this sector (BU) .. PSG/ISS . . .
                        If Me.i_Variants.ContainsKey(sourceChannel) Then
                            Dim vpc As List(Of clsVariant) = Me.i_Variants(sourceChannel) 'seller'  
                            If vpc.Count > 30 Then
                                Dim a = 0
                            End If
                            For Each SV In vpc
                                If Not done.Contains(SV) Then
                                    '  If SV Is WhichVariant Or WhichVariant Is iq.AllVariants Then  'was variantmatch
                                    margin = sellerchannel.Margin(buyerchannel)(Me.Sector)
                                    'baseprice is their price for themselves (or maybe 'everyone' - check)
                                    If SV.i_prices.ContainsKey(iq.getPriceBand(margin.PriceBand)) Then
                                        Dim baseprice As IQ.clsPrice
                                        baseprice = SV.i_prices(iq.getPriceBand(margin.PriceBand))(currency)
                                        ret.Add(New IQ.clsPrice(baseprice, margin.Factor))  'Return the Factored price from the SOURCE channel
                                        done.Add(SV)
                                    Else
                                        ' errormessages.Add("There is no (" & margin.PriceBand & ") price for " & sku)
                                    End If
                                    'End If
                                End If
                            Next
                        End If
                    End If
                End If
            End If
        End If

        Return (ret)

    End Function


    'Public Function PriceVariants(sellerchannel As clsChannel, buyerchannel As clsChannel, currency As clsCurrency, Optional SKUVariant As clsVariant = Nothing) As List(Of clsPrice)

    '    'returns The Prices for all (or the specified) SKUVariant(s) the specified buyer, - at the correct margin/multiplier for the buyer/seller combo
    '    'will return an empty list if there is no price (POA)

    '    PriceVariants = New List(Of clsPrice)
    '    Dim margin As Single

    '    'NB: We fetch pricing from the sellerchannels 'isCloneOf' - which for non-clones, is itself.
    '    Dim sourceChannel As clsChannel = sellerchannel.IsCloneOf

    '    'If we're getting prices for a clone..
    '    'There's an important distinction between the SourceChannel and the SellerChannel - the sellerchannel (which may be a clone) carries the margin for the buyer..
    '    'whereas the sourceChannel (the clones 'parent') has the base price

    '    Dim done As List(Of clsVariant)
    '    done = New List(Of clsVariant)

    '    'First get SPECIFIC (overriding)prices
    '    If i_Prices.ContainsKey(sourceChannel) Then 'does the SOURCE channel seller have a (base) price for this product            
    '        For Each v In i_Prices(sourceChannel).Keys 'variants
    '            If v Is SKUVariant Or SKUVariant Is Nothing Then
    '                'there are *some* specific prices for this product - for this buyer (perhaps not for the right variants or currencies
    '                If i_Prices(sourceChannel)(v).ContainsKey(buyerchannel) Then
    '                    If i_Prices(sourceChannel)(v)(buyerchannel).ContainsKey(currency) Then
    '                        PriceVariants.Add(i_Prices(sourceChannel)(v)(buyerchannel)(currency))  'we've found a specifc price - to which we should NOT apply margin !
    '                        done.Add(v)
    '                    End If
    '                End If
    '            End If
    '        Next
    '    End If

    '    'for any remaining unpriced - do margin based pricing
    '    If sellerchannel.Margin.ContainsKey(buyerchannel) Then 'does this seller have ANY margin specified for this buyer
    '        If sellerchannel.Margin(buyerchannel).ContainsKey(Me.Sector) Then 'is there a margin specified for products within this sector (BU) .. PSG/ISS . . .
    '            If sellerchannel.Margin(buyerchannel)(Me.Sector).ContainsKey(Me.ProductType) Then  'does this seller have a margin specified for this products productType (within this sector)
    '                For Each SV In i_Prices(sourceChannel).Keys
    '                    If Not done.Contains(SV) Then
    '                        If SV Is SKUVariant Or SKUVariant Is Nothing Then  'was variantmatch
    '                            margin = sellerchannel.Margin(buyerchannel)(Me.Sector)(Me.ProductType).Factor
    '                            PriceVariants.Add(New clsPrice(i_Prices(sourceChannel)(SV)(sellerchannel)(currency), margin))  'Return the Factored price from the SOURCE channel
    '                            done.Add(SV)
    '                        End If
    '                    End If
    '                Next
    '            End If
    '        End If
    '    End If

    '    If PriceVariants.Count = 0 Then PriceVariants = Nothing

    'End Function

    'Private Function VariantMatch(skuvariant As clsVariant, matchWith As clsVariant) As Boolean

    '    VariantMatch = False
    '    If matchWith Is Nothing Then Return True
    '    If skuvariant Is matchWith Then Return True
    'End Function

    Public Function getXtext(path$, Acknowledged As List(Of String)) As List(Of ClsValidationMessage)
        If Me.isFIO Then Return New List(Of ClsValidationMessage) 'Dont include messages from Pre-installed

        getXtext = New List(Of ClsValidationMessage)
        If Me.i_Attributes_Code.ContainsKey("xText") Then

            Dim i As Integer
            Dim xtext As clsProductAttribute
            Dim showOnlyInFamilies As String = ""
            Dim hideInFamilies As String = ""

            Dim myfamily As String = LCase(Trim$(findFamily(path$)))
            For i = 0 To Me.i_Attributes_Code("xText").Count - 1

                xtext = Me.i_Attributes_Code("xText")(i)

                If Me.i_Attributes_Code.ContainsKey("ShowF") Then
                    showOnlyInFamilies = Trim$(LCase(Me.i_Attributes_Code("ShowF")(i).Translation.text(English)))
                End If

                If Me.i_Attributes_Code.ContainsKey("HideF") Then
                    hideInFamilies = Trim$(LCase(Me.i_Attributes_Code("HideF")(i).Translation.text(English)))
                End If


                'Exit if this external text should NOT be visible
                If showOnlyInFamilies <> "" AndAlso Not Split(showOnlyInFamilies, ",").Contains(myfamily) Then Continue For
                If hideInFamilies <> "" AndAlso Split(hideInFamilies, ",").Contains(myfamily) Then Continue For

                Dim ib As ImageButton = New ImageButton
                Dim msg As ClsValidationMessage

                msg = New ClsValidationMessage(enumValidationMessageType.Validation, If(Acknowledged IsNot Nothing AndAlso Acknowledged.Contains(path & "." & i), EnumValidationSeverity.greenTick, CType(XText.NumericValue, EnumValidationSeverity)), iq.AddTranslation(Me.sku & ":" & XText.Translation.text(English), English, "ISSU", 0, Nothing, 0, False), iq.AddTranslation("Important Information", English, "ISSU", 0, Nothing, 0, False), "", 0, 0, Split(""), "", path & "." & i)
                If Acknowledged IsNot Nothing AndAlso Acknowledged.Contains(path & "." & i) Then msg.Acknowledged = True
                getXtext.Add(msg)
            Next

        End If

    End Function
    Public Class clsSpecTableEntry



        Private _title As String
        Public Property Title As String
            Get
                If (Code <> "hdd" And Code <> "opt") Then
                    If Code2 IsNot Nothing AndAlso (Code2.Contains("SATA") Or Code2.Contains("SAS")) Then
                        Return "Disk Controller"
                    Else
                        Return _title
                    End If
                Else
                    Return _title
                End If

                '   Return If(Code <> "hdd" AndAlso Code2 IsNot Nothing AndAlso (Code2.Contains("SATA") Or Code2.Contains("SAS")), "Disk Controller", _title)
            End Get
            Set(value As String)
                _title = value
            End Set
        End Property
        Public Property Value As clsTranslation
        Public Property Extra As String
        Public Property Code As String
        Public Property Code2 As String
        Public Property Max As Integer
        Public Property Type As String
        Public Property ProdType As String
        Public Property Params As String()
        Public ReadOnly Property Order As Int32 'This gives the combined order for preinstalled, attributes, etc
            Get
                Select Case ProdType
                    Case "SVR", "DTO", "NBK"
                        Select Case Code.ToLower()
                            Case "mfrsku"
                                Return 10
                            Case "formfactor"
                                Return 20
                            Case "cpu"
                                Return If(Type = "pre", 30, 0)
                            Case "mem"
                                Return If(Type <> "slot", 40, 0)
                            Case "graphics"
                                Return 50
                            Case "display"
                                Return 60
                            Case "networking"
                                Return 70
                            Case "hdd"
                                Return 80
                            Case "raid"
                                Return If(Type = "pre", 90, 0)
                            Case "opt"
                                Return 100
                            Case "pci"
                                Return 110
                            Case "psu"
                                Return If(Type <> "slot", 120, 0)
                            Case "mgt"
                                Return 130
                            Case "warrantycode"
                                Return 140
                            Case "man3"
                                Return 150
                            Case "software"
                                Return 155
                            Case "document links"
                                Return 190
                            Case "also included"
                                Return 160
                            Case "options"
                                Return 180
                            Case Else
                                Return 0
                        End Select
                    Case "SWD"
                        Select Case Code.ToLower
                            Case "mfrsku"
                                Return 10
                            Case "formfactor"
                                Return 20
                            Case "ioc", "cpu"
                                If Type = "pre" Then Return If(Code2 IsNot Nothing AndAlso (Code2.Contains("SATA") Or Code2.Contains("SAS")), 90, 30) Else Return 0
                            Case "mem"
                                Return If(Type <> "slot", 40, 0)
                            Case "networking"
                                Return 45
                            Case "priconnectivity"
                                Return 50
                            Case "management"
                                Return 40
                            Case "poe"
                                Return 60
                            Case "poepower"
                                Return 70
                            Case "hdd"
                                Return 80
                            Case "raid"
                                Return If(Type = "pre", 90, 0)
                            Case "opt"
                                Return 100
                            Case "pci"
                                Return 110
                            Case "mgt"
                                Return 130
                            Case "psu"
                                Return If(Type <> "slot", 120, 0)
                            Case "man3"
                                Return 150
                            Case "warrantycode"
                                Return 140
                            Case "software"
                                Return 155
                            Case "document links"
                                Return 190
                            Case "also included"
                                Return 160
                            Case "options"
                                Return 180
                            Case Else
                                Return 0
                        End Select
                    Case "HPN"
                        Select Case Code.ToLower
                            Case "mfrsku"
                                Return 10
                            Case "formfactor"
                                Return 20
                            Case "ioc", "cpu"
                                If Type = "pre" Then Return If(Code2 IsNot Nothing AndAlso (Code2.Contains("SATA") Or Code2.Contains("SAS")), 90, 30) Else Return 0
                            Case "mem"
                                Return If(Type <> "slot", 40, 0)
                            Case "priconnectivity"
                                Return 50
                            Case "management"
                                Return 40
                            Case "poe"
                                Return 60
                            Case "poepower"
                                Return 70
                            Case "upconnectivity"
                                Return 80
                            Case "psu"
                                Return If(Type = "pre", 100, 0)
                            Case "mgt"
                                Return 130
                            Case "warrantycode"
                                Return 140
                            Case "document links"
                                Return 190
                            Case "also included"
                                Return 160
                            Case "options"
                                Return 180
                            Case Else
                                Return 0
                        End Select
                End Select
            End Get
        End Property

        Public Sub New()
            Params = {}
        End Sub
    End Class

    Public Function Spectable(language As clsLanguage, preinstalled As List(Of clsQuantity), branch As clsBranch, sysPath As String, showall As Boolean) As Panel

        Dim SpectablePanel = New Panel
        SpectablePanel.CssClass = "specTable"

        Dim familyName As String = String.Empty
        If branch.Parent IsNot Nothing Then familyName = branch.Parent.Translation.text(English)

        Dim specTableProps As List(Of clsSpecTableEntry) = New List(Of clsSpecTableEntry)()

        Dim orderedAttributeList As IEnumerable(Of clsProductAttribute) = From v In Me.Attributes.Values Order By v.Attribute.Order

        'For Each a In Me.Attributes.Values
        '    If a.Attribute.Code.ToLower = "also included" Then
        '        Beep()
        '    End If
        'Next


        'This summarises the gives and takes slots accrooss the system, chassis, FIOs and all Preinstalled componentry -
        'so that we can render the 'Max 8' type slot info in the spec table
        Dim allslots As List(Of clsSlot)
        allslots = branch.slots.Values.ToList

        'Martins OLD code
        'allslots = branch.slots.values.Union(branch.childBranches.
        ' SelectMany(Function(s) s.Value.slots.Values.
        '   Select(Function(ppp) New clsSlot() With {.path = ppp.path, .numSlots = ppp.numSlots, .Type = ppp.Type})))
        '.Union(preinstalled.SelectMany(Function(p) p.Branch.slots.
        'Select(Function(ppp) New clsSlot() With {.path = ppp.Value.path, .numSlots = p.NumPreInstalled * ppp.Value.numSlots, .Type = ppp.Value.Type}
        ').Where(Function(sl) String.IsNullOrEmpty(sl.path) OrElse sl.path.Contains(path)))) 'Need to consider path in this stuff? yes you do ML


        'The systems child branches includde the chassis branch (which carries slots accross many common systems) 
        'and the FIOs branch which carries a number of 'fake' parts
        For Each b In branch.childBranches.Values
            If Not b.deleted Then
                If b.EnglishName.ToLower.Contains("fios") Then Stop 'contains the ###iO controlers, ###CPUS etc
                If b.EnglishName.ToLower.Contains("chassis") And b.Hidden Then
                    For Each slot In b.slots.Values
                        If Not slot.deleted Then
                            If slot.path.Contains(sysPath) Or slot.path = "" Then  'Presinstalled contains some quantities which DO NOT apply (wrong paths)
                                allslots.Add(slot)
                            End If
                        End If
                    Next
                    Exit For 'there should be only 1 chassis branch
                End If
            End If
        Next b

        For Each pic In preinstalled  'preinstalled component (clsQuantities)
            If pic.Path = "" OrElse pic.Path.StartsWith(sysPath) Then
                'Dim bn As String = pic.Branch.DisplayName(English)
                'Dim picProd As String = pic.Branch.Product.i_Attributes_Code("desc")(0).Translation.text(English)
                'Dim sp As String = PathName(path) 'sys path
                'Dim p As String = PathName(pic.Path)
                For Each picSlot In pic.Branch.slots.Values
                    If Not picSlot.deleted Then
                        '  If picSlot.Type.MajorCode = "MEM" Then Stop
                        If picSlot.path.StartsWith(sysPath) Or picSlot.path = "" Then  'Presinstalled contains some quantities which DO NOT apply (wrong paths)
                            Dim slt As New clsSlot
                            slt.path = pic.Path
                            slt.numSlots = pic.NumPreInstalled * picSlot.numSlots
                            slt.Type = picSlot.Type
                            allslots.Add(slt)
                        End If
                    End If
                Next
                'Else
                'Beep()
            End If
        Next
        '/ end of 'allslots' creation


        'Add all attributes
        'specTableProps.AddRange(orderedAttributeList.Where(Function(oal) oal.Attribute.Order And oal.deleted = False > 0).Select(Function(v) New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "atr", .Code = v.Attribute.Code, .Title = v.Attribute.displayName(language), .Value = iq.AddTranslation(If(v.Attribute.Code.ToLower() = "formfactor" AndAlso orderedAttributeList.Where(Function(at) at.Attribute.Code = "U").Count() > 0, v.displayNameNoCode(language) + " (" + orderedAttributeList.Where(Function(at) at.Attribute.Code = "U").First().NumericValue.ToString() + "U)", v.displayNameNoCode(language)), English, "", 0, Nothing, 0, False)})) ' horrid hack to put U after form factor
        specTableProps.AddRange(orderedAttributeList.Where(Function(oal) oal.deleted = False).Select(Function(v) New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "atr", .Code = v.Attribute.Code, .Title = v.Attribute.displayName(language), .Value = iq.AddTranslation(If(v.Attribute.Code.ToLower() = "formfactor" AndAlso orderedAttributeList.Where(Function(at) at.Attribute.Code = "U").Count() > 0, v.displayNameNoCode(language) + " (" + orderedAttributeList.Where(Function(at) at.Attribute.Code = "U").First().NumericValue.ToString() + "U)", v.displayNameNoCode(language)), English, "", 0, Nothing, 0, False)})) ' horrid hack to put U after form factor

        'Add all preinstalled products
        For Each p In preinstalled.Where(Function(pi) pi.FOC).GroupBy(Function(pi) pi.Branch)
            Dim productDisplay As String = String.Empty
            If p.Key.Product.i_Attributes_Code.ContainsKey("Name") Then
                Dim productName = p.Key.Product.i_Attributes_Code("Name")(0).Translation.text(s_lang)
                productDisplay = productName
            ElseIf p.Key.Product.i_Attributes_Code.ContainsKey("Description") Then
                productDisplay = p.Key.Product.i_Attributes_Code("Description")(0).Translation.text(s_lang)
            ElseIf p.Key.Product.i_Attributes_Code.ContainsKey("Desc") Then
                productDisplay = p.Key.Product.i_Attributes_Code("Desc")(0).Translation.text(s_lang)
            Else
                productDisplay = p.Key.Translation.text(s_lang)
            End If
            'do we have a max slot?
            ' Dim test =AllSlots.Where(Function(x) x.slotNum)
            Dim slo = allslots.Where(Function(als) als.deleted = False AndAlso p.Key.Product.ProductType.Code.ToLower() = als.Type.MajorCode.ToLower() AndAlso als.numSlots > 0).Sum(Function(s) s.numSlots)

            'Horrrid if statement below as Raid controllers are embeded devices, this is going to change according to Paul so this is temp, yay!
            Dim specEntry As clsSpecTableEntry = New clsSpecTableEntry()

            specEntry.ProdType = Me.ProductType.Code
            If p.Key.Product.i_Attributes_Code.ContainsKey("technology") Then

                specEntry.Code2 = p.Key.Product.i_Attributes_Code("technology").First.Translation.text(English)
            Else
                specEntry.Code2 = Nothing
            End If

            specEntry.Type = "pre"

            If p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") AndAlso p.Key.Product.i_Attributes_Code("optFamily")(0).Translation.text(English).ToUpper = "RAID_CONTROLLERS" Then
                specEntry.Code = "RAID"
                specEntry.Title = "RAID Controller"
            Else
                specEntry.Code = (p.Key.Product.ProductType.Code)
                specEntry.Title = p.Key.Product.ProductType.Translation.text(language)
            End If


            specEntry.Value = iq.AddTranslation("{0} x {1}", English, "SpecValues", 0, Nothing, 0, False)
            If specEntry.Code <> "ioc" Then
                specEntry.Max = slo
            End If
            specEntry.Params = {p.Sum(Function(pp) pp.NumPreInstalled).ToString, productDisplay}

            '   specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Code2 = If(p.Key.Product.i_Attributes_Code.ContainsKey("technology"), p.Key.Product.i_Attributes_Code("technology").First.Translation.text(English), Nothing), .Type = "pre", .Code = If(p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") AndAlso p.Key.Product.i_Attributes_Code("optFamily")(0).Translation.text(English).ToUpper = "RAID_CONTROLLERS", "RAID", p.Key.Product.ProductType.Code), .Title = If(p.Key.Product.i_Attributes_Code.ContainsKey("optFamily") AndAlso p.Key.Product.i_Attributes_Code("optFamily")(0).Translation.text(English).ToUpper = "RAID_CONTROLLERS", "RAID Controller", p.Key.Product.ProductType.Translation.text(language)), .Value = iq.AddTranslation("{0} x {1}", English, "SpecValues", 0, Nothing, 0, False), .Max = slo, .Params = {p.Sum(Function(pp) pp.NumPreInstalled).ToString, productDisplay}})
            specTableProps.Add(specEntry)
        Next

        If {"SVR", "SWD"}.Contains(Me.ProductType.Code) Then
            'Populate Management row, do we have an insight or oneview licence here?
            Dim t2 As String = "No OneView or Insight Control"
            Dim t1 As String = "No Licence"

            If preinstalled.Where(Function(pi) pi.Branch.Product.ProductType.Code.ToLower() = "man1").Count() > 0 Then
                Dim pr = preinstalled.Where(Function(pi) pi.Branch.Product.ProductType.Code.ToLower() = "man1").FirstOrDefault()
                If pr.IsAutoAdd Then
                    t2 = pr.Branch.Product.i_Attributes_Code("desc")(0).Translation.text(language)
                    Dim includedMajorCode As String = "ILO MAN1"
                    If pr.Branch.slots.Where(Function(sl) includedMajorCode.Contains(sl.Value.Type.MajorCode)).Count() > 0 Then t1 = "Advanced"
                End If

            End If
            Dim sts As String = If(Me.i_Attributes_Code.ContainsKey("ILOHARDWARE"), "{0} ({1}) / {2}", "No Management")
            Dim specEntry As clsSpecTableEntry = New clsSpecTableEntry()
            With specEntry
                .ProdType = Me.ProductType.Code
                .Type = "pre"
                .Code = "MGT"
                .Title = "Management"
                .Value = iq.AddTranslation(sts, English, "", 0, Nothing, 0, False)
                If Me.i_Attributes_Code.ContainsKey("ILOHARDWARE") Then
                    .Params = {Me.i_Attributes_Code("ILOHARDWARE")(0).Translation.text(language), t1, t2}
                Else
                    .Params = {}
                End If


            End With
            specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "pre", .Code = "MGT", .Title = "Management", .Value = iq.AddTranslation(sts, English, "", 0, Nothing, 0, False), .Params = If(Me.i_Attributes_Code.ContainsKey("ILOHARDWARE"), {Me.i_Attributes_Code("ILOHARDWARE")(0).Translation.text(language), t1, t2}, {})})
        End If

        If {"SVR", "NBK", "DTO", "SWD"}.Contains(Me.ProductType.Code) Then
            'Special case for HDD and OPT as we need a line when they are NOT present
            Dim allHDDslots = From p In preinstalled Where p.Branch.Product.ProductType.Code.ToUpper() = "HDD"
            If allHDDslots.Count > 0 Then
                Dim preinstalledHDD = From p In allHDDslots Where p.NumPreInstalled > 0
                If preinstalledHDD.Count = 0 Then
                    specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "pre", .Code = "HDD", .Title = "Hard Disk Drive", .Value = iq.AddTranslation("None Installed", English, "", 0, Nothing, 0, False), .Max = allslots.Where(Function(als) als.deleted = False AndAlso "hdd" = als.Type.MajorCode.ToLower() AndAlso als.numSlots > 0).Sum(Function(s) s.numSlots)})
                End If
            End If

            If preinstalled.Where(Function(p) p.Branch.Product.ProductType.Code.ToUpper() = "HDD").Count() = 0 Then specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "pre", .Code = "HDD", .Title = "Hard Disk Drive", .Value = iq.AddTranslation("None Installed", English, "", 0, Nothing, 0, False), .Max = allslots.Where(Function(als) "hdd" = als.Type.MajorCode.ToLower() AndAlso als.deleted = False AndAlso als.numSlots > 0).Sum(Function(s) s.numSlots)})
            If preinstalled.Where(Function(p) p.Branch.Product.ProductType.Code.ToUpper() = "OPT").Count() = 0 Then specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "pre", .Code = "OPT", .Title = "Optical Drive", .Value = iq.AddTranslation("None Installed", English, "", 0, Nothing, 0, False), .Max = allslots.Where(Function(als) "opt" = als.Type.MajorCode.ToLower() AndAlso als.deleted = False AndAlso als.numSlots > 0).Sum(Function(s) s.numSlots)})
        End If

        'Add a slot summary for PCI, grouped by the short description of the slot type
        Dim listofInterfaceCards = {"PCIF", "PCIC", "PCID", "PCIE", "PCIG", "PCIX", "PCI", "MODA", "MODL", "MODI", "MODB", "RISER", "MODE", "MODM"}
        Dim excludefromsummary = {"MODM", "MODE", "MODL", "MODI", "RISER"}
        Dim d = String.Join("<br>", _
        branch.slots.Values.Union(branch.childBranches.SelectMany(Function(s) s.Value.slots.Values)). _
        Where(Function(s) listofInterfaceCards.Contains(s.Type.MajorCode.ToUpper) _
                  And s.numSlots > 0 _
                  ). _
                  OrderBy( _
                      Function(sl) _
                      IIf(sl.HasSlotNum, sl.slotNum.value, 200) _
                      ) _
                  .Select(Function(s) String.Format("{0}: {1}", _
                        If(s.HasSlotNum, _
                            Chr(64 + s.slotNum.value), ""), _
                            If(s.Type.MajorCode.ToUpper.StartsWith("MOD"), _
                                s.Type.shortDisplayName(language), _
                                s.Type.Translation.text(language) _
            ))).ToList())


        'branch.slots.Values.Union(branch.childBranches.SelectMany(Function(s) s.Value.slots.Values)). _
        'Where(Function(s) listofInterfaceCards.Contains(s.Type.MajorCode.ToUpper) _
        '          And s.numSlots > 0 _
        '          AndAlso s.slotNum.value IsNot Nothing _
        '          AndAlso Not IsDBNull(s.slotNum.value)). _
        '          OrderBy( _
        '              Function(sl) _
        '              IIf(sl.slotNum.value Is Nothing OrElse IsDBNull(sl.slotNum.value), 200, sl.slotNum.value) _
        '              ) _
        '          .Select(Function(s) String.Format("{0}: {1}", _
        '                IIf(s.slotNum IsNot Nothing AndAlso s.slotNum.value IsNot Nothing AndAlso Not IsDBNull(s.slotNum.value), _
        '                    Chr(64 + s.slotNum.value), ""), _
        '                    IIf(s.Type.MajorCode.ToUpper.StartsWith("MOD"), _
        '                        s.Type.shortDisplayName(language), _
        '                        s.Type.Translation.text(language) _
        '    ))).ToList())




        Dim cn = 0
        Dim op As String = ""
        Dim params As List(Of String) = New List(Of String)()

        branch.slots.Values.Union(branch.childBranches.SelectMany(Function(s) s.Value.slots.Values)). _
        Where(Function(s) listofInterfaceCards.Contains(s.Type.MajorCode) _
                  AndAlso Not excludefromsummary.Contains(s.Type.MajorCode) _
                  AndAlso s.numSlots > 0). _
              GroupBy(Function(s) s.Type.shortDisplayName(language)). _
              Select(Function(s)
                         op &= " {" & cn & "}: {" & cn + 1 & "}"
                         cn = cn + 2
                         params.Add(s.Key)
                         params.Add(s.Sum(Function(sss) sss.numSlots))
                     End Function).ToList()

        If params.Count > 0 Then specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "pre", .Code = "PCI", .Title = "Interface Slots", .Value = iq.AddTranslation(op, English, "", 0, Nothing, 0, False), .Extra = d, .Params = params.ToArray})

        'Add any information pertinent to slots, so anything which gives a slot to the system and isn't otherwise already added
        For Each slot In branch.slots.Values.Union(branch.childBranches.SelectMany(Function(s) s.Value.slots.Values)).Where(Function(s) Not listofInterfaceCards.Contains(s.Type.MajorCode) And s.numSlots > 0)
            If slot.path = sysPath Or slot.path = "" Then
                If slot.Type.MajorCode = "RJ45" Then
                    Dim spt = specTableProps.Where(Function(stp) stp.Type = "atr" AndAlso stp.Code = "PriConnectivity").FirstOrDefault()
                    If spt IsNot Nothing Then
                        spt.Max = slot.numSlots
                    End If
                End If
                specTableProps.Add(New clsSpecTableEntry() With {.ProdType = Me.ProductType.Code, .Type = "slot", .Code = slot.Type.MajorCode, .Title = Xlt(slotMajorTranslations(slot.Type.MajorCode), language), .Value = slot.Type.Translation})
            End If
        Next

        'Render all of the above in a predefined order, nasty way of ordering needs to go in the db somewhere but for now hardcoded in the object
        For Each sp In specTableProps.OrderBy(Function(s) s.Order)

            If sp.Order > 0 Or showall Then
                Dim l = Nothing
                If sp.Code.ToUpper() = "PCI" Then
                    l = NewLit("<div style='display:none;' id='" + sysPath + "." + "ttPCIslots'>" + sp.Extra + "</div><span onclick=""TagToTip('" + sysPath + "." + "ttPCIslots', TITLE, 'Interface Card Slots', CLICKSTICKY, true, CLICKCLOSE, false, CLOSEBTN, true, COPYCONTENT, false, DELAY, 400, BORDERWIDTH, 1, BORDERCOLOR, '#2F7BD1', PADDING, 2, FIX, [624, 372])"" style='cursor:help;float:right;height:15px;width:15px;'><img src='../images/Navigation/ICON_CIRCLE_info.png'/></span>")
                End If
                'Ok another hacky bit, would like to put a param in the DB to say if the field might contain a part number
                'Scan for Part Numbers
                If sp.Code.ToUpper <> "MFRSKU" Then
                    Dim newValue As String = ""
                    params = New List(Of String)
                    Dim vc = 0
                    For Each spl In sp.Value.text(English).Split(",")
                        If iq.i_SKU.ContainsKey(spl) Then
                            newValue &= "{" & vc & "} , "
                            params.Add(iq.i_SKU(spl).DisplayName(language))
                            vc = vc + 1
                        End If
                    Next
                    If Not String.IsNullOrEmpty(newValue) Then
                        sp.Value = iq.AddTranslation(Left(newValue, Len(newValue) - 2), English, "", 0, Nothing, 0, False)
                        sp.Params = params.ToArray
                    End If

                End If

                specTableRow(IIf(sp.Order = 0, "HIDDEN ", "") & Xlt(sp.Title, language), Replace(Replace(String.Format(sp.Value.text(language), sp.Params), familyName & " ", ""), "HP ", ""), SpectablePanel, sp.Max, l, language)
            Else

                Dim ss As String = sp.Title & ":" & sp.Value.text(language)

            End If


        Next

        Return SpectablePanel
    End Function
    Sub specTableRow(headerText As String, valueText As String, ByRef specTable As Panel, max As Integer, xtra As Literal, language As clsLanguage)
        Dim p As Panel = New Panel
        p.CssClass = "specRow"
        specTable.Controls.Add(p)

        Dim panel = New Panel
        panel.CssClass = "specLeft"
        Dim lbl As Literal = New Literal
        lbl.Mode = LiteralMode.Transform
        lbl.Text = headerText
        panel.Controls.Add(lbl)
        p.Controls.Add(panel)

        panel = New Panel
        panel.CssClass = "specRight"
        lbl = New Literal
        lbl.Mode = LiteralMode.Transform
        lbl.Text = If(valueText IsNot Nothing, valueText.Replace("&NBSP;", "&nbsp;"), "")
        panel.Controls.Add(lbl)
        If max <> 0 Then
            Dim panel2 = New Panel
            panel2.CssClass = "specRightMax"
            Dim lbl2 = New Literal
            lbl2.Text = Xlt("max", language) & ": " & max.ToString()
            panel2.Controls.Add(lbl2)
            panel.Controls.Add(panel2)
        End If

        If xtra IsNot Nothing Then panel.Controls.Add(xtra)

        p.Controls.Add(panel)

        panel = New Panel
        panel.CssClass = "specBreak"
        p.Controls.Add(panel)
        lbl = New Literal
        lbl.Text = "&nbsp;"
    End Sub

    Function slotMajorTranslations(type As String) As String
        Select Case type
            Case "HDD"
                Return "Disk Storage Backplane"
            Case "PCI"
                Return "Interface Card Slots"
            Case "FAN"
                Return "Fan Slots"
            Case "MEM"
                Return "Memory Slots"
            Case "OPT"
                Return "Optical Slots"
            Case "PSU"
                Return "Power Supply Slots"
        End Select
        Return type
    End Function

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Public Function hasPromo(promoCode As String, region As clsRegion) As Boolean
        If Not Promos.ContainsKey(promoCode) Then Return False
        For Each p In Promos(promoCode)
            If p.Encompasses(region) Then Return True
        Next
        Return False
    End Function

    ' Returns whether this product has a valid list price
    Public Function HasListPrice(buyerAccount As clsAccount) As Boolean

        Return ListPrice(buyerAccount) IsNot Nothing

    End Function

    Public Function isFakePart() As Boolean
        If Not Me.hasSKU Then Return True
        If Me.SKU.StartsWith("###") Then Return True
        Return False
    End Function
End Class

