Option Strict On

Imports System.ServiceModel
Imports System.ServiceModel.Activation
Imports System.Runtime.Serialization
Imports dataAccess

'Namespace iQuote  'a namespace is a bit like a stub class.. under which we can define a set classes implementing and exposing methods

<ServiceContract()>
Public Interface i_PnA

    <OperationContract()>
    Function SetStock(SessionID As String, items() As clsStockPriceSvc.clsStockItem) As clsStockPriceSvc.clsResult()
    <OperationContract()>
    Function SetPrices(SessionID As String, Currency As String, prices() As clsStockPriceSvc.clsPrice) As clsStockPriceSvc.clsResult()
    <OperationContract()>
    Function SetMargins(SessionID As String, margins() As clsStockPriceSvc.clsMargin) As clsStockPriceSvc.clsResult()
    <OperationContract()>
    Function SetVariants(SessionID As String, variants() As clsStockPriceSvc.clsVariant) As clsStockPriceSvc.clsResult()
    <OperationContract()>
    Function DeleteVariants(SessionID As String) As String
    <OperationContract()>
    Function SKUS(systems As Boolean, options As Boolean) As HashSet(Of String)

    'techdata (and others) will requires somethgin along these lines
    'the productinfo class might contain 'Recognised' - regionalisation info, ListPrice info (in currencies), HP descriptions - etc.
    '<OperationContract()>
    'Function ProductInfo(SessionID As String, skus As List(Of String)) As List(Of clsStockPriceService.clsProductInfo)

End Interface

'<AspNetCompatibilityRequirements(RequirementsMode:=AspNetCompatibilityRequirementsMode.Allowed)>

' IQ.clsStockPriceSvc is not attributed with ServiceContractAttribute
'<ServiceContract()>
Public Class clsStockPriceSvc

    Implements i_PnA

    'The clsPrice is used (in combination with the hosts Token (which yields their ID) to locate this right variant

    <DataContractAttribute()>
    Class clsPrice

        <DataMemberAttribute()>
        Public MfrSKU As String 'HP part number (strictly this is redundant but it helps us look up the variant much quciker)
        <DataMemberAttribute()>
        Public HostSKU As String 'a unique identifier for each distinct variant of the product (most likely thier internal part nmber) - or a compound key of Warehouse/PartnNo
        <DataMemberAttribute()>
        Public Price As Decimal  'The currency and buyer channel are  specified in the setPrices Method
        <DataMemberAttribute()>
        Public Warehouse As String  '
        <DataMemberAttribute()>
        Public PriceBand As String  '

    End Class

    <DataContractAttribute()> _
    Class clsVariant
        <DataMemberAttribute()>
        Public MfrSKU As String
        <DataMemberAttribute()>
        Public HostSKU As String
        <DataMemberAttribute()>
        Public WareHouse As String
        <DataMemberAttribute()>
        Public DisplayText As String  'an overriding/variant specific description

    End Class

    Class clsMargin

        Public AccountNum As String
        Public ProductType As String
        Public Margin As Single

    End Class

    Class clsstock
        Public Quantity As Integer
        Public Arrival As Date
        Public isCurrent As Boolean
    End Class

    Class clsStockItem

        Public MfrSKU As String
        Public HostSKU As String
        Public Warehouse As String  ''a unique identifier for each distinct variant of the product (most likely thier internal part nmber) - or a compound key of Warehouse/PartnNo
        Public Shipments() As clsstock

    End Class

    <DataContractAttribute()>
    Class clsResult
        <DataMemberAttribute()>
        Public Success As Boolean
        <DataMemberAttribute()>
        Public Message As String
        <DataMemberAttribute()>
        Public ErrorCode As Integer

        Public Sub New(Success As Boolean, Message As String, errorCode As Integer)
            Me.Success = Success
            Me.Message = Message
            Me.ErrorCode = errorCode
        End Sub

    End Class

    Public Function BuyerChannelFrompriceBand(priceBand As clsPriceBand) As clsChannel

        Dim j = From a In iq.Accounts.Values Where a.Priceband Is priceBand
        If j.Any Then Return j.First.BuyerChannel Else Return Nothing

    End Function

    Public Function channelFromToken(WebToken As String) As clsChannel

        Dim j = From c In iq.Channels.Values Where UCase(c.WebToken) = UCase(WebToken) Or UCase(c.Code) = UCase(WebToken) 'todo REMOve THE CHECK ON cHANNEL.CODE AND WORK OFF (SECRET) TOKENS

        If j.Any Then
            Return j.First
        Else
            Return Nothing
        End If

    End Function

    Public Function SetMargins(webtoken As String, Margins() As clsStockPriceSvc.clsMargin) As clsResult() Implements i_PnA.SetMargins

        Dim results() As clsStockPriceSvc.clsResult

        If iq.PNAdown Then
            ReDim results(0)
            results(0) = New clsResult(False, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1)
            Return results '<<<<<<<<<< EXIT
        End If

        ReDim results(Margins.Count - 1)
        Return results

    End Function
    Public Function DeleteVariants(webtoken As String) As String Implements i_PnA.DeleteVariants

        'delete all variants (that are not referenced by a quoteitem)

        Dim rt$ = "" 'result text

        Try

            Dim con As SqlClient.SqlConnection = da.OpenDatabase()

            Dim seller As clsChannel
            seller = channelFromToken(webtoken$)



            If seller Is Nothing Then rt$ = webtoken & " is not valid (no seller channel could be located)" : Return rt$

            iq.PNAdown = True

            Dim sql$ = "SELECT id FROM variant WHERE fk_channel_id_seller=" & seller.ID
            Dim vlist As List(Of String) = New List(Of String)
            Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)
            While rdr.Read
                vlist.Add(rdr.Item("ID").ToString.Trim)
            End While
            rdr.Close()

            If vlist.Count = 0 Then
                rt$ = " There are no variants for " & seller.ID & " (" & webtoken & ")"
            Else

                rt$ &= vlist.Count & " Variants " & vbCrLf

                sql$ = "DELETE FROM PRICE where fk_variant_id in (" & Join(vlist.ToArray, ",") & ")"
                rt$ &= "Deleted " & LongSQL(sql$) & " prices " & vbCrLf

                sql$ = "DELETE FROM Stock where fk_variant_id in (Select id from variant where fk_channel_id_seller=" & seller.ID & ")"
                rt$ &= "Deleted " & LongSQL(sql) & " stock " & vbCrLf

                For Each s In iq.Stock.Values.ToArray
                    If vlist.Contains(s.SKUvariant.ID.ToString.Trim) Then
                        iq.Stock.Remove(s.ID)
                    End If
                Next

                'prices live 'in' the products - actually variants live in the products - prices  line under the variants

                'For Each product In iq.Products.Values
                '    For Each ba In product.

                '    Next
                'Next

                'These are the variants referenced by quoteitems (we will not be able to delete)
                sql$ = "SELECT v.id as ID FROM quote q "
                sql$ &= "JOIN quoteitem qi ON qi.fk_quote_id=q.id "
                sql$ &= "JOIN variant v ON qi.fk_variant_id=v.id "
                sql$ &= "JOIN account buyerAccount on buyeraccount.id=q.fk_account_id_buyer "
                sql$ &= "WHERE buyeraccount.fk_channel_id_seller = " & seller.ID

                rdr = da.DBExecuteReader(con, sql$)
                Dim refed As Integer = 0
                While rdr.Read
                    vlist.Remove(CStr(rdr.Item("id")))
                    refed += 1
                End While
                rdr.Close()
                con.Close()

                rt$ &= refed & " variants are referenced by quote items and cannot be (physically) deleted" & vbCrLf

                Dim removed As Integer = 0
                For Each v In iq.Variants.Values.ToArray
                    If vlist.Contains(v.ID.ToString.Trim) Then
                        iq.Variants.Remove(v.ID)
                        removed += 1
                    End If
                Next v
                rt$ &= removed & " variants were removed from the OM (iq.variants)." & vbCrLf

                removed = 0
                Dim sremoved As Integer = 0
                'remove the variants (holding the prices) from the products
                For Each p In iq.Products.Values.ToArray
                    If p.i_Variants.ContainsKey(seller) Then
                        p.i_Variants.Remove(seller) 'each product has an index of variants by seller channel - we need to remove those references - Public i_Variants As Dictionary(Of clsChannel, List(Of clsVariant))
                        sremoved += 1
                    End If

                    For Each v In p.Variants.Values.ToArray
                        If vlist.Contains(v.ID.ToString.Trim) Then
                            If Not p.Variants.ContainsKey(v.ID) Then Stop

                            p.Variants.Remove(v.ID)
                            removed += 1
                        End If
                    Next
                Next

                rt$ &= removed & " variants were removed from products.variants" & vbCrLf
                rt$ &= sremoved & " products had the seller (variant set) removed" & vbCrLf


                'Break into chunks of 1000 or the deletions time out
                Dim chunk As List(Of String) = New List(Of String)
                For Each vid In vlist
                    chunk.Add(vid)
                    If chunk.Count = 200 Or vid = vlist.Last Then
                        sql$ = "DELETE FROM VARIANT where id in (" & Join(chunk.ToArray, ",") & ")"
                        rt$ &= LongSQL(sql$) & " variants DELETED from the database"
                        chunk = New List(Of String)
                    End If
                Next
            End If

            Return rt$

        Catch ex As Exception

            Return "An error coccured " & ex.Message & vbCrLf & rt$
            ErrorLog.Add(ex)

        Finally

            iq.PNAdown = False

        End Try

    End Function


    Public Function SetVariants(webtoken As String, variants() As clsStockPriceSvc.clsVariant) As clsStockPriceSvc.clsResult() Implements i_PnA.SetVariants

        'Called by the feedReaded to push variants into IQ2 - should be obsoleted by the JIT call to AllProducts

        'For each HP sku a disti can have multiple variants 
        'prices (and stock) then apply to these variants (for specific customers)

        Try

            Dim Results(variants.Count - 1) As clsStockPriceSvc.clsResult

            If iq Is Nothing Then
                ReDim Results(0)
                Results(0) = New clsResult(False, "The object model is not currently loaded - please try later", 1)
                Return Results '<<<<<<<<<< EXIT
            End If

            If iq.PNAdown Then
                ReDim Results(0)
                Results(0) = New clsResult(False, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1)
                Return Results '<<<<<<<<<< EXIT
            End If

            Dim sellerChannel As clsChannel
            sellerChannel = channelFromToken(webtoken$)

            If sellerChannel Is Nothing Then
                ReDim Results(0)
                Results(0) = New clsResult(False, "Invalid or unrecognised Security Token '" & webtoken & "' your webservice security token is a lowercase 32 Digit, hyphenated, Hexadecimal GUID, supplied by ChannelCentral.net - Contact support@channelcentral.net for assistance", 1)
                Return Results '<<<<<<<<<< EXIT
            End If

            Dim errormessages As List(Of String) = New List(Of String)
            If sellerChannel.variantsLoadedAt = Nothing Then
                sellerChannel.LoadVariants(errormessages, 0.1) 'generally this will mean there is 
            End If

            Dim skuvariant As IQ.clsVariant = Nothing
            Dim result As clsResult = Nothing

            Dim i As Integer = 0

            'need a locking mechanism - to make these robust
            iq.PNAdown = True

            Dim con As SqlClient.SqlConnection = da.OpenDatabase()


            'find the next available variant ID (so we can bulk write)

            Dim nextid As Integer
            Dim wc As DataTable = da.MakeWriteCacheFor(con, "variant", nextid, True)
            con.Close()

            ' Dim nextID As Integer = (From j In iq.Variants.Values Select j.ID).Max + 1  'LINQ

            ReDim Results(variants.Count)
            For Each v In variants

                'If v.HostSKU.Contains("#") Then Stop

                If v.DisplayText Is Nothing Then v.DisplayText = ""

                Dim bpn$ = Split(v.MfrSKU, "#")(0)
                If v.MfrSKU = "" Then
                    Results(i) = New clsResult(False, "Empty manufacturer SKU for :" & v.HostSKU, 21)
                ElseIf Not iq.i_SKU.ContainsKey(bpn$) Then 'Check the part before any hash present
                    Results(i) = New clsResult(False, "Unrecognised manufacturer part number (in Heirarchy - but not in iQuote) " & v.MfrSKU, 66)
                Else

                    If Not sellerChannel.findVariant(v.HostSKU, v.WareHouse, result, skuvariant) Then
                        Select Case result.ErrorCode
                            Case 23, 56, 57 'No (existing) variant
                                'add the variant

                                'THIS IS SIGNIFICANT - Products are hashless - sellers have variants wich carry an #SUFFIX (of the version they choose to sell), along with their (internal) part number
                                'If a disti pushes as #ABU type variant to us we will make a variant .. (using the same maechanism as warehouses)
                                'this allows them to sell multiple versions of the same 
                                Dim basePartNo As String = Split(v.MfrSKU, "#").First
                                Dim suffix = ""
                                If v.MfrSKU.Contains("#") Then
                                    suffix = Split(v.MfrSKU, "#").Last ' set the CODE of this variant to any #ABU type suffix in the HostMfrPartNum - 
                                End If

                                'Every variant has a code.. for a 123456#ABU part - it woudl be ABU - generally it's blank/empty

                                skuvariant = New IQ.clsVariant(suffix, iq.i_SKU(basePartNo), sellerChannel, v.HostSKU, v.DisplayText, v.WareHouse.Trim, "", r_worldwide, False, wc, nextid)
                                Results(i) = New clsResult(True, "Added variant " & v.MfrSKU & "/" & v.HostSKU & " " & v.WareHouse, 0)
                            Case Else
                                Results(i) = result
                        End Select
                    Else
                        'we found the variant in question

                        Dim A As Boolean
                        Dim b As Boolean

                        A = (v.DisplayText <> skuvariant.DisplayText) 'description has changed
                        b = (v.HostSKU <> skuvariant.DistiSku)

                        If A Or b Then
                            skuvariant.DisplayText = v.DisplayText
                            skuvariant.DistiSku = v.HostSKU ' TODO warehouse, localisations etc
                            skuvariant.Update()
                        End If

                        Results(i) = result 'OK  (returned by findvariant)

                    End If

                End If
                i += 1
            Next

            If wc.Rows.Count > 0 Then
                con = da.OpenDatabase()
                da.BulkWrite(con, wc, "variant", 1000, True)
                con.Close()
            End If

            Return Results

        Catch ex As System.Exception

            ErrorLog.Add(ex)
        Finally

            iq.PNAdown = False

        End Try


    End Function

    ''' <summary>Webservice method Called by external applications (feed reader) to directly inject stock levels to the iquote2 database/OM </summary>
    ''' <remarks>Does not update the [pricing] database - which in the LONG term will not exist </remarks>
    Public Function SetStock(webtoken As String, Items() As clsStockItem) As clsResult() Implements i_PnA.SetStock


        Try

            Dim results() As clsStockPriceSvc.clsResult
            ReDim results(Items.Count - 1)

            If iq Is Nothing Then
                ReDim results(0)
                results(0) = New clsResult(False, "The object model is not currently loaded - please try later", 1)
                Return results '<<<<<<<<<< EXIT
            End If


            If iq.PNAdown Then
                ReDim results(0)
                results(0) = New clsResult(False, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1)
                Return results '<<<<<<<<<< EXIT
            End If

            Dim sellerChannel As clsChannel
            sellerChannel = channelFromToken(webtoken$)

            If sellerChannel Is Nothing Then
                ReDim results(0)
                results(0) = New clsResult(False, "Invalid or unrecognised Security Token '" & webtoken & "' your webservice security token is a lowercase 32 Digit, hyphenated, Hexadecimal GUID, supplied by ChannelCentral.net - Contact support@channelcentral.net for assistance", 1)
                Return results '<<<<<<<<<< EXIT
            End If

            If iq.PNAdown Then
                ReDim results(0)
                results(0) = New clsResult(False, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1)
                Return results '<<<<<<<<<< EXIT
            End If

            iq.PNAdown = True

            Dim errormessages As List(Of String) = New List(Of String)

            'load up - JIT
            sellerChannel.LoadVariants(errormessages, 0.1)

            If Not sellerChannel.stockLoaded Then
                sellerChannel.LoadStock()
            End If

            '        Dim con As SqlClient.SqlConnection
            '        Dim swc As DataTable = da.MakeWriteCacheFor(con,"stock",nsid,

            Dim DeleteShipments As List(Of Integer)
            DeleteShipments = New List(Of Integer)  'Maintain a list of unmatched shipments...ones which are in the database for which the caller is not providing a updated version - they're removed from the OM as we go - but removed from the DB en-masse at the end of the batch for performance reasons

            Dim i As Integer
            For Each item In Items  'Products - stock lines (each, potentially containing multiple shipments)

                Dim skuvariant As IQ.clsVariant = Nothing
                Dim result As clsResult = Nothing


                'DistiSKU|Warehouse form a unique key
                If Not sellerChannel.findVariant(item.HostSKU, item.Warehouse, result, skuvariant) Then
                    results(i) = result
                Else
                    results(i) = New clsResult(True, "OK", 0)

                    'Each stock items has many shipments - most of which are in the future
                    'the isCurrent flag tells us which is the current stock level indicator (becuase the datestamps of what were once future shipments, may may now be in the past)
                    'for each shipment - see if we have an existing match (on variant and date) in the OM - if so, update it (if the quantity has changed)

                    Dim match As IQ.clsstock
                    Dim ashipment As IQ.clsStockPriceSvc.clsstock
                    Dim matched As List(Of IQ.clsstock) ' store a list of all the shipments we *have* matched
                    matched = New List(Of IQ.clsstock)
                    For Each shipment As IQ.clsStockPriceSvc.clsstock In item.Shipments
                        ashipment = shipment 'apparently we need to do this (rather than look at the iterator)

                        If skuvariant.shipments Is Nothing Then skuvariant.shipments = New SortedDictionary(Of DateTime, IQ.clsstock)
                        If skuvariant.shipments.ContainsKey(ashipment.Arrival) Then
                            match = skuvariant.shipments(ashipment.Arrival)
                            match.LastUpdated = Date.UtcNow 'update the timestamp
                            match.quantity = shipment.Quantity
                            matched.Add(match)

                            If match.quantity <> shipment.Quantity Then
                                'the (anticipated) shipment quantity has changed
                            Else

                            End If
                        Else
                            'INSERT
                            Dim newshipment As IQ.clsstock
                            newshipment = New IQ.clsstock(skuvariant, shipment.Quantity, shipment.Arrival, "PNA webservice", shipment.isCurrent)
                        End If
                    Next

                    'remove unmatched shipments from the OM - and queue them for deletion for the DB
                    If skuvariant.shipments IsNot Nothing Then
                        For Each shipment In skuvariant.shipments.Values
                            If Not matched.Contains(shipment) Then
                                DeleteShipments.Add(shipment.ID)
                            End If
                        Next

                        'Mark all stock shipments of this variant as non-current (in the object model)
                        For Each shipment In skuvariant.shipments.Values
                            shipment.IsCurrent = False
                        Next
                    End If

                    'And do the same in the database (this is more efficient than doing many clsStock.update calls)
                    Dim sql$
                    sql$ = "UPDATE [Stock] SET isCurrent=0 WHERE fk_variant_id=" & skuvariant.ID  'The SKUVariant is unique to the seller, product and warehouse - we're 'archving' stock for this variant
                    da.DBExecutesql(sql$)

                    Dim gotcurrent As Boolean = False
                    For Each Shipment In item.Shipments
                        'create the new (and Current) stock shipments for this SKU (which will add them to the product, and INSERT them in the database STOCK table)

                        Dim stock As New IQ.clsstock(skuvariant, Shipment.Quantity, Shipment.Arrival, "PNA Webservice", Shipment.isCurrent)

                        If Shipment.Arrival < DateAdd(DateInterval.Second, 1, DateTime.UtcNow) Then
                            If gotcurrent Then
                                results(i) = New clsResult(False, "You have provided more than one current stock level - only one shipment with a date in the past is allowed (which should contain all of your current stock)", 5)
                            Else
                                stock.IsCurrent = True
                                gotcurrent = True
                            End If
                        End If
                    Next
                End If

                i += 1
            Next

            iq.PNAdown = False

            Return results

        Catch ex As System.Exception

            ErrorLog.Add(ex)
        Finally

            iq.PNAdown = False

        End Try


    End Function

    Private Function FindCurrency(code As String) As clsCurrency

        'uses LINQ to locate the currency by code
        If (From c In iq.Currencies.Values Where c.Code = code).Count > 0 Then
            Return (From c In iq.Currencies.Values Where c.Code = code).First
        Else
            Return Nothing
        End If

    End Function

    ''' <summary>Returns a list of SKUs known to iQuote2</summary>
    ''' <param name="includeSystems">whether to include systems SKU in the list</param>
    ''' <param name="includeOptions"></param>
    
    Public Function Skus(includeSystems As Boolean, includeOptions As Boolean) As HashSet(Of String) Implements i_PnA.SKUS

        Skus = New HashSet(Of String)  'hashsets are fast and enforce uniqueness
        For Each kvp In iq.i_SKU
            If (kvp.Value.isSystem And includeSystems) Or ((Not kvp.Value.isSystem) And includeOptions) Then
                Skus.Add(kvp.Key)
            End If
        Next

    End Function

    Public Function SetPrices(webtoken As String, CurrencyCode As String, prices() As clsPrice) As clsResult() Implements i_PnA.SetPrices

        Try

            Dim errormessages As List(Of String) = New List(Of String)

            Dim results() As clsStockPriceSvc.clsResult
            ReDim results(prices.Count - 1)

            If iq Is Nothing Then
                ReDim results(0)
                results(0) = New clsResult(False, "The system is currently unavailable (the object model is not loaded)", 99)
                Return results
            End If

            Dim sellerChannel As clsChannel
            sellerChannel = channelFromToken(webtoken$)

            If sellerChannel Is Nothing Then
                ReDim results(0)
                results(0) = New clsResult(False, "Invalid or unrecognised Security Token '" & webtoken & "' your webservice security token is a lowercase 32 Digit, hyphenated, Hexadecimal GUID, supplied by ChannelCentral.net - Contact support@channelcentral.net for assistance", 1)
                Return results '<<<<<<<<<< EXIT
            End If

            If iq.PNAdown Then
                ReDim results(0)
                results(0) = New clsResult(False, "The service is temporarily unavailable (typically only for a few seconds) - please try again", 1)
                Return results '<<<<<<<<<< EXIT
            End If

            Dim buyer As clsChannel = Nothing

            'If priceBand = "" Then
            '    buyer = Everyone
            'Else
            '    buyer = BuyerChannelFrompriceBand(priceBand) 'channelFromToken(BuyerChannelToken)
            '    If buyer Is Nothing Then
            '        ReDim results(0)
            '        results(0) = New clsResult(False, "Invalid host account number " & priceBand, 2)
            '        Return results '<<<<<<<<<< EXIT
            '    End If
            'End If

            'load/freshen variants - JIT
            sellerChannel.LoadVariants(errormessages, 0.1)

            'load prices - JIT

            Dim i As Integer = 0
            Dim currency As clsCurrency
            Dim CurrentCurrencyCode As String = ""

            If iq.i_currency_code.ContainsKey(CurrencyCode) Then
                currency = iq.i_currency_code(CurrencyCode)
            Else
                ReDim results(0)
                results(0) = New clsResult(False, "Invalid currency code:" & CurrencyCode, 78)
                Return results
            End If

            'results(0) = New clsResult(False, "Hello:" & CurrencyCode, 78)
            ' Return results

            'Dim con As SqlClient.SqlConnection
            'con = da.OpenDatabase
            'Dim writecache As DataTable = da.MakeWriteCacheFor(con, "price")
            'con.Close()

            'Dim nextID As Integer = (From j In iq.Prices.Values Select j.ID).Max + 1  'LINQ  - the prices aren't all loaded any more so we have to go back to the DB for the MAX

            'Dim nextid As Integer
            'Dim con As SqlClient.SqlConnection = da.OpenDatabase()
            'Dim reader = da.DBExecuteReader(con, "Select max([price])+1 as c from Price")
            'reader.Read()
            'Nextid = reader.Item(0)
            'reader.Close()
            'con.Close()


            Dim aprice As clsPrice
            For Each price In prices
                aprice = price

                Dim result As clsResult = Nothing
                Dim SKUvariant As IQ.clsVariant = Nothing
                If Not sellerChannel.findVariant(price.HostSKU, price.Warehouse, result, SKUvariant) Then
                    results(i) = result 'Failed to find the variant
                Else
                    'Look at the prices in the price band

                    If Not sellerChannel.pricesLoadedFor.ContainsKey(iq.getPriceBand(price.PriceBand)) OrElse sellerChannel.pricesLoadedFor(iq.getPriceBand(price.PriceBand)) = 0 Then  'load them Just in time (the check is trivial)
                        sellerChannel.LoadPrices(iq.getPriceBand(price.PriceBand), errormessages)
                    End If

                    Dim pl As List(Of IQ.clsPrice) = SKUvariant.Product.Prices(sellerChannel, buyer, iq.getPriceBand(price.PriceBand), currency, SKUvariant)

                    Dim AddPrice As Boolean = False
                    If pl Is Nothing Then
                        AddPrice = True
                    Else
                        If pl.Count = 0 Then AddPrice = True
                    End If
                    If AddPrice Then
                        Dim newprice As IQ.clsPrice
                        newprice = New IQ.clsPrice(SKUvariant, iq.getPriceBand(price.PriceBand), New NullablePrice(price.Price, currency, False), "PNA webservice")  'match the IQ distiSku with the stockPriceSVC skuvariant 
                        results(i) = New clsResult(True, "Added", 0)
                    ElseIf pl.Count = 1 Then
                        If pl(0).Price.NumericValue <> price.Price Then
                            pl(0).Price = New NullablePrice(price.Price, currency, False)
                            pl(0).PriceBand = iq.getPriceBand(price.PriceBand)
                            pl(0).Update()
                            results(i) = New clsResult(True, "Updated", 0)
                        Else
                            results(i) = New clsResult(True, "Unchanged (shouldn't happen)", 0)
                        End If
                    Else
                        results(i) = New clsResult(False, "more than one variant matches " & aprice.HostSKU, 18)
                    End If
                End If
                i += 1
            Next

            ' If writecache.Rows.Count > 0 Then
            ' con = da.OpenDatabase
            ' da.BulkWrite(con, writecache, "Price")
            ' con.Close()
            ' End If
            iq.PNAdown = False
            Return results

        Catch ex As System.Exception

            ErrorLog.Add(ex)
        Finally

            iq.PNAdown = False

        End Try


    End Function

End Class

'End Namespace
