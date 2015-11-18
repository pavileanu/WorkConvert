Imports dataAccess

Public Class clsPrice

    'A price links a Buyer and a SkuVariant (which is a Seller, product, warehouse combo)

    Property ID As Integer
    ' Public Product As clsProduct            
    '    Property Seller As clsChannel 'note - there is no seller, op product here.. they come from the variant
    Property SKUVariant As clsVariant
    '    Property Buyer As clsChannel        'can be 'Everyone' (for list prices)
    Property PriceBand As clsPriceBand
    Property Price As nullablePrice  'contains the currency, and message
    Property Offers As Dictionary(Of Integer, clsOffer)  'Not implemented (a generalised offer framework to encompass Flex, Avalanche, Bundles and more)

    Property lastRequested As Date 'when it was last requested (webservice request fired off)
    Property lastUpdated As Date 'When it was last successfully updated (webservice result/update)

    Property Source As String

    Public tempID As Integer 'temporary negative ID used to allow late INSERTS (when the webservice returns a price)

    Public Function insert() As clsPrice

        Return New clsPrice(Me.SKUVariant, Me.PriceBand, Me.Price, Me.Source)

    End Function

    Public Function clone(SkuVariant As clsVariant) As clsPrice

        clone = New clsPrice(SkuVariant, Me.PriceBand, Me.Price, "cloned")

    End Function

    Public Sub New(buyeraccount As clsAccount, SKUvariant As clsVariant)

        'POA - NB: Does not add the price to the Product, or Insert to the database

        Me.ID = -1
        'Me.Product = Product
        Me.SKUVariant = SKUvariant
        Me.Price = New nullablePrice(buyeraccount.Currency)
        Me.priceband = buyeraccount.Priceband
        '       Me.Seller = buyeraccount.SellerChannel
        Me.Source = "Contact the seller for current pricing"
        Me.lastRequested = DateAdd(DateInterval.Day, -1, Now)
        Me.lastUpdated = Me.lastRequested
        Me.Offers = New Dictionary(Of Integer, clsOffer)

        If Me.SKUVariant IsNot Nothing Then
            Me.SKUVariant.i_prices.Add(buyeraccount.Priceband, New Dictionary(Of clsCurrency, clsPrice))
            Me.SKUVariant.i_prices(buyeraccount.Priceband).Add(buyeraccount.Currency, Me)
            Me.SKUVariant.prices.Add(Me.ID, Me)
        End If

    End Sub


    Public Sub New(BasePrice As clsPrice, Factor As Single)

        'creates a new price - from an exsisting (typically list) price - multiplied by the specified factor (margin based pricing

        Me.ID = -1
        Me.SKUVariant = BasePrice.SKUVariant
        Me.Price = New NullablePrice(BasePrice.Price.value, BasePrice.Price.currency, False)
        'Note - we do not preserve isList - as multiplying a list price by a factor... makes it no longer a list price ! (OK.. unless it's 1 - smart arse)

        Me.PriceBand = BasePrice.PriceBand
        Me.Source = "Margin based price"
        Me.Price.Message = "Estimated (Margin based) price"

        Me.Offers = New Dictionary(Of Integer, clsOffer)
        '                                                                                                   buyer
        'the vairant holds a dictionary of prices ..  Property prices As Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))  'TODO fill ??

        'this is a special constructore - we don't want to add marging based prices to the editable dictionary

        '     Me.SKUVariant.i_prices(Me.Buyer).Add(BasePrice.Price.currency, Me)
        '     Me.SKUVariant.prices.Add(Me.ID, Me)

    End Sub

    Public Sub New()

        Me.Offers = New Dictionary(Of Integer, clsOffer)

    End Sub


    'Public Sub New(product As clsProduct, SKUvariant As clsVariant, buyeraccount As clsAccount, source As String)

    '    'this particular oveload is *just* to make the temporary 'POA' ones


    '    Dim POAprice As clsPrice
    '    POAprice = New clsPrice(product, SKUvariant, buyeraccount.SellerChannel, buyeraccount.BuyerChannel, New nullablePrice(buyeraccount.Currency), "Webservice")
    '    Return POAprice

    '    'Me.ID = -1
    '    'Me.Product = product
    '    'Me.SKUVariant = SKUvariant
    '    'Me.Seller = buyeraccount.SellerChannel
    '    'Me.Price = New nullablePrice(buyeraccount.Currency)
    '    'Me.Buyer = Buyer
    '    'Me.Source = "Requesting Price . ."
    '    'Me.Price.Message = ""

    '    'Me.Offers = New Dictionary(Of Integer, clsOffer)

    'End Sub



    Public Shared Function temporaryID()

        'assigned the next avialble (and negative) temporary ID for a (in memory only for now) clsPrice
        'INSERTs which would normally yield an ID are very slow - and we don't actually want to persist a record until it has a price (back from the webserivce) anyway


        Dim lock As New Object

        SyncLock lock
            Static countdown As Integer
            countdown -= 1

            temporaryID = countdown
        End SyncLock



    End Function

    Public Sub New(SKUvariant As clsVariant, Priceband As clsPriceBand, price As NullablePrice, source As String, Optional ByRef writecache As DataTable = Nothing, Optional ByRef nextID As Integer = -1)

        'If buyer Is Nothing Then Stop

        Pmark("Price_NEW (db)")

        If writecache Is Nothing Then
            Dim sql$

            If price.sqlvalue = 0 Then

                Me.ID = temporaryID()
            Else

                sql$ = "INSERT INTO PRICE(fk_variant_id,priceband,price,fk_currency_id,datestamp,source) VALUES "
                sql$ &= "(" & SKUvariant.ID & "," & da.SqlEncode(Priceband.text) & "," & price.sqlvalue & "," & price.currency.ID & ",getdate()," & da.SqlEncode(source) & ");"

                Try
                    Me.ID = da.DBExecutesql(sql$, True)

                Catch ex As System.Exception

                    Logit(ex.Message.ToString)
                    If ex.InnerException IsNot Nothing Then
                        Logit(ex.InnerException.Message.ToString)
                    End If
                    Logit("In clsPrice_New " & sql$)
                    'Stop

                    'Beep()

                End Try
            End If


        Else

            Me.ID = nextID '
            nextID += 1
            Dim row As System.Data.DataRow
            row = writecache.NewRow()
            '  row("FK_product_id") = product.ID
            '  row("FK_Channel_id_seller") = seller.ID

            If nextID <> -1 Then row("ID") = nextID 'NEW

            row("FK_variant_id") = SKUvariant.ID

            row("Priceband") = Priceband.text
            'If Buyer Is Nothing Then
            '    row("FK_Channel_id_buyer") = DBNull.Value
            'Else
            '    row("FK_Channel_id_buyer") = buyer.ID
            'End If

            row("price") = price.value
            row("fk_currency_id") = price.currency.ID
            row("fk_variant_id") = SKUvariant.ID
            row("datestamp") = Now
            row("source") = source$ & "(bulk)"

            writecache.Rows.Add(row)

        End If

        Me.Price = price
        Me.SKUVariant = SKUvariant
        Me.lastUpdated = Now
        Me.lastRequested = Now

        Me.Source = source
        Me.PriceBand = Priceband

        'add myself to the master price list
        '  iq.Prices.Add(Me.ID, Me)

        'add into the products i_prices  - Obsoleted as the Products.Variants(sellerchannel) now provides a natural index
        'SKUvariant.Product.AddPrice(Me)

        Me.Offers = New Dictionary(Of Integer, clsOffer)
        If Not Me.SKUVariant.i_prices.ContainsKey(Priceband) Then
            Me.SKUVariant.i_prices.Add(Me.PriceBand, New Dictionary(Of clsCurrency, clsPrice))
        End If


        If Me.SKUVariant.i_prices(Me.PriceBand).ContainsKey(Me.Price.currency) Then
            Me.SKUVariant.i_prices(Me.PriceBand)(Me.Price.currency) = Me
        Else
            Me.SKUVariant.i_prices(Me.PriceBand).Add(Me.Price.currency, Me)
        End If

        If Me.ID = 0 Then Throw New Exception("attempted to add a price with an ID of 0")

        If Me.ID <> -1 Then 'for an insert WITHOUt the write cache - me.ID will be the ID generated by the SQL INSERT
            Me.SKUVariant.prices.Add(Me.ID, Me)
        End If

        If Me.ID <> -1 And da.DatabaseAlive Then 'TODO prices rely on DB
            iq.Prices.Add(Me.ID, Me)
        End If

        Pacc("Price_NEW (db)")

    End Sub

    Public Function Ui(buyeraccount As clsAccount, margin As Single, lid As UInt64) As Panel

        Dim errorMessages As List(Of String) = New List(Of String)

        Ui = New Panel

        'If Me.SKUVariant Is Nothing Or Me.SKUVariant.Product Is Nothing Then
        '    lbl.Text = "POA"
        '    Ui.BackColor = Drawing.Color.Red
        '    lbl.ToolTip = "Price " & Me.ID & " has no Product "
        'Else

        Ui.ID = "P_" & Me.ID
        Ui.CssClass = "P_" & Me.ID
        Ui.CssClass &= " Refresh"  'this class doesn't exist - but is used by the script to identify those elements to refresh - so DONT REMOVE IT !!!
        Ui.CssClass &= " PriceUI"

        If Me.MinutesOld > 60 Then Ui.CssClass &= " unconfirmed" Else Ui.CssClass &= " upToDate"

        Dim priceIncludingMargin As NullablePrice = Me.Price * margin
        'lbl.Text = Me.Price * margin.DisplayPrice(buyeraccount, errorMessages).Text

        Dim pp As Panel = priceIncludingMargin.DisplayPrice(buyeraccount, errorMessages)

        If False Then ''REINSTATE for price source info
            pp.ToolTip = Me.Source & vbCrLf
            pp.Attributes("Style") &= "background-color:blue;padding:5px;"

            With Me.SKUVariant
                Dim rcode As String = ""
                If .Region IsNot Nothing Then rcode = .Region.Code
                pp.ToolTip &= "VarID:" & .ID & " ProdID:" & .Product.ID & vbCrLf
                pp.ToolTip &= "DistiSKU:" & .DistiSku & " Warehouse:" & SKUVariant.Warehouse & " region:" & rcode & vbCrLf
                pp.ToolTip &= "SlrID:" & .sellerChannel.ID & " SlrCode:" & .sellerChannel.Code & vbCrLf
                pp.ToolTip &= "PriceBand:" & Me.PriceBand.text

            End With
        End If

        'Brazil Changes
        If buyeraccount.BuyerChannel.Region.Code = "BR" Then
            pp.ToolTip = iq.AddTranslation("For actual prices and stock levels, please set the Customer Context", English, "", 1, Nothing, 0, False).text(buyeraccount.Language)
        End If

        ' End If

        Ui.Controls.Add(pp)  'price panel
        OutputErrors(Ui.Controls, errorMessages, lid)

    End Function
    Public Function MinutesOld() As Integer
        Return DateDiff(DateInterval.Minute, Me.lastUpdated, Now)  'how old is it at the moment (when was it last *requested* ) 
    End Function

    Public Function Update() As clsPrice

        'If Me.ID = -1 Then Stop 'TODO REMOVE
        'If LCase(Me.Price.sqlvalue) = "null" Then Stop

        Me.lastUpdated = Now

        Dim sql$
        sql$ = "UPDATE PRICE"
        sql$ &= " SET price=" & Me.Price.sqlvalue & ","
        sql$ &= " datestamp=" & da.UniversalDate(Now)
        If Me.Source IsNot Nothing Then
            sql$ &= ",source=" & da.SqlEncode(Me.Source)
        End If
        sql$ &= " WHERE ID=" & Me.ID

        da.DBExecutesql(sql$)

        Return Me

    End Function

    Public Sub New(id As Integer, SKUvariant As clsVariant, priceband As clsPriceBand, price As Decimal, currency As clsCurrency, datestamp As Date, source As String)

        'If buyer Is Nothing Then Stop

        Me.ID = id
        Me.SKUVariant = SKUvariant
        ' Me.Buyer = buyer

        Dim islistPrice As Boolean = (SKUvariant.sellerChannel Is HP) And priceband.text = "" '=(buyer Is Everyone)

        Me.Price = New NullablePrice(price, currency, islistPrice)
        Me.lastUpdated = datestamp 'NOT Now (otherwise datestamps are not restored from the database properly)
        Me.lastRequested = datestamp
        Me.Source = source

        Me.Offers = New Dictionary(Of Integer, clsOffer)

        'add myself to the master price list
        iq.Prices.Add(Me.ID, Me)
        Me.PriceBand = priceband


        Me.SKUVariant.prices.Add(Me.ID, Me)
        If Not Me.SKUVariant.i_prices.ContainsKey(Me.PriceBand) Then Me.SKUVariant.i_prices.Add(Me.PriceBand, New Dictionary(Of clsCurrency, clsPrice))

        If Me.SKUVariant.i_prices(Me.PriceBand).ContainsKey(currency) Then
            ' Beep() 'ut oh - we already had a price for that buyer in that currency
            Dim a As Boolean = False
        Else
            Me.SKUVariant.i_prices(Me.PriceBand).Add(currency, Me)
        End If

    End Sub

End Class
