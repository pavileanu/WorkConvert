Imports dataAccess

Public Class clsVariant

    'Every product has one or more variants 
    'These are the disti specific versions of the the product and carry the disti Skus (hostPartNums) - amongst other things
    'Variants allow a product to have one or more stock level(s) and/or price(s) (although neither is required)
    'They're also used to allow (list) prices to be per region(country)
    'There must be a HP varant for each region (country)  for which there is a list price - as it is the variants that link Products, Prices and Regions.

    Property ID As Integer  ' there are at least as many variants as parts
    Property sellerChannel As clsChannel 'Seller channel (disti)
    Property DistiSku As String  'Distis internal part number - can be different for different variants of the same (HP) part (by localisation,  warehouse, code etc)
    Property Product As clsProduct
    Property Code As String   'Human friendly code such as 'b-grade'
    Property DisplayText As String 'Optional overriding text
    Property Warehouse As String  'disti warehouse code (3 chars, Human readable ?) – could be extended to an object – with GPS cords, and customer-warehouse restrictions
    Property Localisation As String  '#ABU, #ABA etc - this is just a text string - no fucntionality is driven from it
    Property Region As clsRegion  'Mostly so list prices can be defined per country - but potentially useful for international distis - Can be nothing.
    Property Deleted As Boolean

    'prices *can* be empty - the actual [Variant] merely joins a Seller, Product, Warehouse and DistiSKU

    'i_Prices (which is an index on THIS variants (Unique Disti wharehouse/product/part) ..Is indexed by PriceBand - eg. 'A' or a host AccountNumber - which can be thought of as a very 'narrow band'
    Public i_prices As Dictionary(Of clsPriceBand, Dictionary(Of clsCurrency, clsPrice))  'how to edit this (ie.. index by the objects).. would allow the iq.variants to be removed and the direct editing of prices
    Property prices As Dictionary(Of Integer, clsPrice)

    'listprices are under the 'everyone' buyer channel and are blended in in getprices()
    'Listprices are per currency/region

    Property shipments As SortedDictionary(Of DateTime, clsStock)
    Dim Ocode As String

    
    Public Function clone(Product As clsProduct) As clsVariant

        clone = New clsVariant(Me.Code, Product, Me.sellerChannel, Me.DistiSku, Me.DisplayText, Me.Warehouse, Me.Localisation, Me.Region, Me.Deleted)

        If Me.i_prices.ContainsKey(iq.getPriceBand("")) Then  ' blank band is the 'everyone' price
            For Each p In Me.i_prices(Everyone).Values
                p.clone(clone) 'clones the price list onto the newly cloned variant
            Next
        End If

    End Function
  
    Public Function priceFor(PriceBand As clsPriceBand, currency As clsCurrency) As clsPrice

        priceFor = Nothing

        If i_prices.ContainsKey(PriceBand) Then
            If i_prices(PriceBand).ContainsKey(currency) Then
                priceFor = i_prices(PriceBand)(currency)
            End If
        End If

    End Function

    ''' <summary>Returns the unique 'compound key' for this variant - which is the distiSKU|warehouse -OR HPSKU|region.code</summary>
    ''' <returns></returns>
    ''' <remarks>The compound key is much like a database index - and is used with the channel.i_variantCK to ensure uniqueness - and for efficient access to Variants - note, warehouse is often (generally) blank</remarks>
    Public Function CK() As String

        'For most Disti Channels - variants are indexed by (i.e. the unique key is DistiSKU|Warehouse
        'For HP - the compound key (and hp.i_variantCK) indexes ListPrice variants by Region.code
        'note... there can be more than one price for the same variant.. in different currencies

        If Me.sellerChannel Is HP Then
            Return Me.DistiSku & "|" & Me.Region.Code
        Else
            Return Me.DistiSku & "|" & Me.Warehouse
        End If

    End Function


    ''' <summary>returns UI for up to MaxShipment shipments of this productVariant</summary>
    Public Function StockUI(MaxShipments As Integer, style As String, language As clsLanguage, channel As clsChannel) As PlaceHolder 'Panel

        Dim lit As Literal
        Dim lbl As New Label

        StockUI = New PlaceHolder 'Panel
        'StockUI.Attributes("style") &= "display:inline-block"
        ' StockUI.Attributes("style") &= style

        '        If Me.shipments IsNot Nothing Then

        Dim c As Integer = 0
        'If Me.shipments.Count > 0 Then

        For Each s In (From x In Me.shipments.Values Where x.IsCurrent).ToList()

            Dim shipmentUI As Panel = New Panel
            shipmentUI.ID = "S_" & s.ID  'Shipment ID (for this arrival of stock of this product variant)
            shipmentUI.CssClass = "S_" & s.ID & " Refresh"
            shipmentUI.Attributes("style") &= "display:inline-block"

            c = c + 1
            lbl = New Label
            lbl.Text = getStock(channel, s.quantity, language)
            lbl.ToolTip = getLabelTooltipForStock(s.IsCurrent, channel, s.LastUpdated, s.Arrival, s.quantity, language)
            shipmentUI.Controls.Add(lbl)
            StockUI.Controls.Add(shipmentUI)
            If c = MaxShipments Then Exit For
            lit = New Literal
            lit.Text = "<br/>"
            StockUI.Controls.Add(lit)
        Next
        'Else
        ''we have a bootstrap problem here when there is no stock record . . .
        'lbl = New Label
        'lbl.Text = "X" 'there are no shipments 
        'lbl.ToolTip = Xlt("Unknown", language)
        'StockUI.Controls.Add(lbl)
        'End If
        '' End If

    End Function
    Private Function getLabelTooltipForStock(ByVal sIsCurrent As Boolean, ByVal channel As clsChannel, ByVal sLastupdated As Date, ByVal sArrival As Date, ByVal sQuantity As Integer, ByVal language As clsLanguage) As String
        Dim result As String = String.Empty
        If sIsCurrent Then
            If channel.BinaryStock And sQuantity > 0 Then
                result = InStock.text(language) & Xlt(" (at ", language) & sLastupdated.ToString & ")"
            ElseIf channel.BinaryStock And sQuantity <= 0 Then
                result = Xlt("arriving ", language) & sArrival
            Else
                result = sQuantity & Xlt(" in stock (at ", language) & sLastupdated.ToString & ")"
            End If
        Else
            result = Xlt("arriving ", language) & sArrival
        End If
        Return result
    End Function
    ''' <summary>
    ''' Gets stock quantity or message in stock or out of stock for binarystock channels.
    ''' </summary>
    ''' <param name="channel">an instance of clsChannel.</param>
    ''' <param name="value">An integer value that represents the quantity of stock.</param>
    ''' <param name="language">an instance of clsLanguage.</param>
    ''' <returns>A string object that represents the text or number to display.</returns>
    ''' <remarks></remarks>
    Private Function getStock(ByVal channel As clsChannel, ByVal value As Integer, language As clsLanguage) As String
        Dim result As String = String.Empty
        If channel.BinaryStock And value > 0 Then
            result = InStock.text(language)
        ElseIf channel.BinaryStock And value <= 0 Then
            result = OutOfStock.text(language)
        ElseIf Not channel.BinaryStock And value > 0 Then
            result = value.ToString
        ElseIf Not channel.BinaryStock And value <= 0 Then
            result = "0"
        End If
        Return result
    End Function

    Public Function Price(Buyeraccount As clsAccount) As NullablePrice 'clsPrice

        Price = New NullablePrice(Buyeraccount.Currency)

        If i_prices.ContainsKey(Buyeraccount.Priceband) Then
            If i_prices(Buyeraccount.Priceband).ContainsKey(Buyeraccount.Currency) Then
                Price = i_prices(Buyeraccount.Priceband)(Buyeraccount.Currency).Price
            End If
        End If

    End Function

    'Public Function listPrice(currency As clsCurrency) As nullablePrice

    '    'we (should) already be working with the HP specific variant 

    '    listPrice = New nullablePrice(currency)

    '    If Me.prices.ContainsKey(Everyone) Then
    '        If Me.prices(Everyone).ContainsKey(currency) Then
    '            listPrice = Me.prices(Everyone)(currency).Price
    '        End If
    '    End If

    'End Function

    ReadOnly Property displayName(language As clsLanguage) As String

        Get
            If Me.DisplayText = "" Then
                Return Me.Warehouse & " " & Me.DistiSku & " " & Me.Localisation ' & " " & Me.OPG Me.Code & " " &
            Else
                Return Me.DisplayText
            End If

        End Get
    End Property

    Public Sub New()
        '                            priceband/priceBand
        Me.i_prices = New Dictionary(Of clsPriceBand, Dictionary(Of clsCurrency, clsPrice))
        Me.prices = New Dictionary(Of Integer, clsPrice)
        Me.shipments = New SortedDictionary(Of DateTime, clsstock)
        ' Me.sellerChannel.i_variant_distisku.Add(Me.DistiSku, Me)

    End Sub

    Public Sub ArchiveCurrentStock()

        'Removes the current stock record from the product.i_stock AND sets clears the 'current' Flag
        'There should only ever be one current stock record for each variant - it's arrival may be in the past
        'There may be many (historical) records (in the pas but isCurrent = false) .. these are absolute stock levels at their DateStamp.. (and could be used to graph stock over time).. 

        Dim toDel As Date
        Dim IDtoArchive As Integer = 0

        For Each s In Me.shipments.Values
            If s.IsCurrent Then 'And kvp.Value.Arrival.Date = Dzero Then
                If IDtoArchive <> 0 Then Stop ' more than one current stock  'TODO remove
                toDel = s.Arrival
                IDtoArchive = s.ID
            End If
        Next

        If IDtoArchive = 0 Then Stop 'no current stock
        Me.shipments.Remove(toDel)

        'Remove it
        iq.Stock.Remove(IDtoArchive)

        'Remove them from the database (so they're not loaded next time)
        da.DBExecutesql("UPDATE stock SET [isCurrent]=0 WHERE ID =" & IDtoArchive)

    End Sub

    Public Sub New(ID As Integer, Code As String, Product As clsProduct, sellerChannel As clsChannel, DistiSku As String, DisplayText As String, Warehouse As String, Localisation As String, Region As clsRegion, deleted As Boolean, CreateIndex As Boolean) ', OPG As String)

        Me.ID = ID
        Me.Code = Code
        Me.Product = Product
        Me.sellerChannel = sellerChannel
        Me.DistiSku = DistiSku
        Me.DisplayText = DisplayText
        Me.Warehouse = Warehouse
        Me.Localisation = Localisation  'code like #ABU
        Me.Region = Region
        Me.Deleted = deleted

        '        Me.OPG = OPG

        'If Me.Product.i_Variants Is Nothing Then Me.Product.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
        If Not Me.Product.i_Variants.ContainsKey(Me.sellerChannel) Then Me.Product.i_Variants.Add(Me.sellerChannel, New List(Of clsVariant))

        If Not Me.Product.i_Variants(Me.sellerChannel).Contains(Me) Then
            Me.Product.i_Variants(Me.sellerChannel).Add(Me)
        End If

        If Not Product.Variants.ContainsKey(Me.ID) Then  'During the import, where we reload variants - it can already be in the product
            Me.Product.Variants.Add(Me.ID, Me)
        End If

        Me.i_prices = New Dictionary(Of clsPriceBand, Dictionary(Of clsCurrency, clsPrice)) 'note, the variants don't have a list price - only the products do
        Me.prices = New Dictionary(Of Integer, clsPrice)

        'a variant can exist with an empty set of shipments (but it will be populated as soon as a stock record is made)
        Me.shipments = New SortedDictionary(Of DateTime, clsstock)

        If ID <> -1 Then
            If Not iq.Variants.ContainsKey(Me.ID) Then 'Check is required beacuse a webservice call can have loaded variants 
                iq.Variants.Add(Me.ID, Me)
            End If
        End If

        Ocode = Me.Code

        If CreateIndex Then Me.sellerChannel.indexVariant(Me)


    End Sub

    Public Function Insert() As clsVariant

        Return New clsVariant(Me.Code, Me.Product, Me.sellerChannel, Me.DistiSku, Me.DisplayText, Me.Warehouse, Me.Localisation, Me.Region, 0) ', Me.OPG)

    End Function

    Public Sub Update()

        'this assumes we're not changing the product or the seller channel

        Dim rid As String = "null"
        If Me.Region IsNot Nothing Then rid = Me.Region.ID

        Dim sql$
        sql$ = "UPDATE [variant] set code=" & da.SqlEncode(Me.Code) & ",displaytext=" & da.SqlEncode(Me.DisplayText) & _
            ",Warehouse=" & da.SqlEncode(Me.Warehouse) & ",localisation=" & da.SqlEncode(Me.Localisation) & _
            ",fk_region_id=" & rid & ",Deleted=" & IIf(Me.Deleted, "1", "0") & " WHERE ID=" & Me.ID
        da.DBExecutesql(sql$)

    End Sub

    Public Sub Delete(errorMessages As List(Of String))

        'QuoteItems reference Variants - so we cannot delete them per se

        Dim sql$
        sql$ = "DELETE FROM stock WHERE fk_variant_id=" & Me.ID 'Deletes all stock (inlcuding archived)
        da.DBExecutesql(sql$)

        sql$ = "DELETE FROM price WHERE fk_variant_id=" & Me.ID 'Deletes all prices (inlcuding archived)
        da.DBExecutesql(sql$)

        sql$ = "UPDATE [VARIANT] set deleted = 1 where ID=" & Me.ID
        da.DBExecutesql(sql$)

        Me.Product.i_Variants(Me.sellerChannel).Remove(Me)
        If Me.Product.i_Variants(Me.sellerChannel).Count = 0 Then Me.Product.i_Variants.Remove(Me.sellerChannel)
        Me.sellerChannel.deIndexVariant(Me, errorMessages) 'we remove the REFERENCE from the index


    End Sub

    Public Function PriceExists(Priceband As clsPriceBand, currency As clsCurrency) As Boolean

        PriceExists = False

        If Me.i_prices.ContainsKey(Priceband) Then
            If Me.i_prices(Priceband).ContainsKey(currency) Then
                PriceExists = True
            End If
        End If

    End Function

    Public Function BasePrice(currency As clsCurrency) As NullablePrice

        'Returns a raw base price - NOT factored by any margin - typically this would be a cost price (it might just occasionally be a list price)

        BasePrice = New NullablePrice(currency)
        If Me.i_prices.ContainsKey(Everyone) Then
            If Me.i_prices(Everyone).ContainsKey(currency) Then
                BasePrice = Me.i_prices(Everyone)(currency).Price
            End If
        End If

    End Function

    Public Sub New(code As String, Product As clsProduct, sellerChannel As clsChannel, DistiSku As String, DisplayText As String, Warehouse As String, Localisation As String, Region As clsRegion, deleted As Boolean, Optional ByRef WriteCache As DataTable = Nothing, Optional ByRef nextId As Integer = -1)

        If WriteCache Is Nothing Then
            Dim sql$ = "INSERT INTO [Variant] (code,distisku,fk_channel_id_seller,fk_product_id,displayText,warehouse,localisation,fk_region_id,deleted) VALUES("
            Dim rid As String
            If Region Is Nothing Then rid = "null" Else rid = Region.ID
            sql$ &= da.SqlEncode(code) & "," & da.SqlEncode(DistiSku) & "," & sellerChannel.ID & "," & Product.ID & "," & da.SqlEncode(DisplayText) & "," & da.SqlEncode(Warehouse) & "," & da.SqlEncode(Localisation) & "," & rid & ",0);"
            Me.ID = da.DBExecutesql(sql$, True)
        Else

            Dim row As System.Data.DataRow
            row = WriteCache.NewRow()

            row("code") = code
            row("fk_product_id") = Product.ID
            row("fk_channel_id_seller") = sellerChannel.ID
            row("distisku") = DistiSku
            row("displaytext") = DisplayText
            row("warehouse") = Warehouse
            row("localisation") = Localisation
            If Region Is Nothing Then
                row("fk_region_id") = DBNull.Value
            Else
                row("fk_region_id") = Region.ID
            End If
            row("deleted") = deleted

            'row("opg") = opg
            If nextId <> -1 Then
                Me.ID = nextId
                row("id") = nextId
                nextId += 1
            End If

            WriteCache.Rows.Add(row)
        End If

        Me.Code = code
        Me.DistiSku = DistiSku
        Me.DisplayText = DisplayText
        Me.Warehouse = Warehouse
        Me.Localisation = Localisation
        'Me.OPG = opg
        Me.Product = Product
        Me.sellerChannel = sellerChannel
        Me.Region = Region
        Me.Deleted = deleted

        '                                                                                                              seller                        

        If Me.Product.i_Variants Is Nothing Then Me.Product.i_Variants = New Dictionary(Of clsChannel, List(Of clsVariant))
        If Not Me.Product.i_Variants.ContainsKey(Me.sellerChannel) Then Me.Product.i_Variants.Add(Me.sellerChannel, New List(Of clsVariant))

        Me.Product.i_Variants(Me.sellerChannel).Add(Me)
        Me.Product.Variants.Add(Me.ID, Me)

        Me.i_prices = New Dictionary(Of clsPriceBand, Dictionary(Of clsCurrency, clsPrice))
        Me.prices = New Dictionary(Of Integer, clsPrice)

        Me.shipments = New SortedDictionary(Of DateTime, clsstock)

        'iq.Variants.Add(ID, Me)
        'iq.i_variant_code.Add(Me.Code, Me)
        If Me.ID > 0 AndAlso Not iq.Variants.ContainsKey(Me.ID) Then
            iq.Variants.Add(Me.ID, Me)
        End If

        Me.sellerChannel.indexVariant(Me)

      

    End Sub

    Public Function HasListPrice(currency As clsCurrency) As Boolean

        HasListPrice = False

        If Me.i_prices.ContainsKey(Everyone) Then
            If Me.i_prices(Everyone).ContainsKey(currency) Then
                Return True
            End If
        End If

    End Function

End Class
