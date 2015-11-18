Public Class GetPriceUIs
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'This page is called (from js fillprices() - to refresh the price AND STOCK labels after a webservice call is made
        'the sequence of events is.. a branch is opened, the list of products whose prices are out of date is compiled
        'A call is made to the 'UniTran' (universal translation) web service in DispatchUpdateRequest() to unitran.RequestStockPrices() - which will return a Handle (batch id) (and place the request in a global dictionary PendingRequests)
        'an image is rendered into the page with an onLoad script - which has a setTimeout - to execute the javascript fillPrices(path) after 5 seconds.
        'the JS fillPrices() assembles a list of the IDs of DIVs beneath the open branch (path) that contain stock and price UI to be refreshed .. there may be several becuase the user may have opened one or more branches during the 5 seconds
        'rExec calls this page (getPriceUIs.aspx), and upon completion calls back the javascript function PlacePrices()
        'which replaces the DIVs with revised content (which may or may not include the update - which may or may not have arrived yet)
        'if updates are still pending another JS timeout() is set

        'request("divIDs") contains a comma delimited list of the div IDs to refresh which are of the form  P or S (price or stock) _branchID_priceID


        Dim errorMessages As List(Of String) = New List(Of String)

        Dim lid As UInt64 = CType(Request.QueryString("lid"), UInt64)

        Dim divIDs As String = Request("divIDs")

        Dim ru$ = Request.RawUrl
        Dim lit As Literal = New Literal

        If Not IsNumeric(Request("handle")) Then
            errorMessages.Add("* GetpriceUIs invalid handle was " & Request("handle").ToString)
            OutputErrors(Me.Controls, errorMessages, lid) ' these just go to the audit log now
            lit.Text = "]^DONE"
        Else

            Dim handle As Integer = Request("handle")
            Dim response As wsconsumer.clsStockPriceResponse = Nothing

            If handle <> -1 Then

                'make a very fast call to the UniTran webservice to fetch the current status/results for the handle
                'it will return instantly  - with or without result lines
                response = callWsConsumer(handle, errorMessages)
                'update the OM with any completed lines in the response

                OutputErrors(Me.Controls, errorMessages, lid) ' these just go to the audit log now

                If response IsNot Nothing Then 'The webservice call can fail 
                    updateStockPriceFromResponse(handle, response, errorMessages)
                    outputUpdatedPriceUIs(lid, handle, response, divIDs, errorMessages)
                    

                    If PendingRequests.ContainsKey(handle) Then
                        If (response.completed) Or PendingRequests(handle).tryCount = 5 Then
                            If PendingRequests IsNot Nothing Then
                                Dim removed As clsQueuedRequest = Nothing
                                PendingRequests.TryRemove(handle, removed)
                            End If
                            lit.Text = "]DONE^" 'we have completed the fetches for all ID (they are all less than 5 minutes old)

                        Else
                            PendingRequests(handle).tryCount += 1
                            'if any of the prices are still pending.. set another timeout
                            lit.Text = "]" & Request("Path") & "^" & handle & "^"  'Some prices are still old - we add the path so a new setTimeout() can be created in the JS PlacePrices()
                        End If
                    End If

                End If
            End If
            End If

            Form.Controls.Add(lit)

    End Sub
    Private Sub outputUpdatedPriceUIs(lid As UInt64, handle As Integer, response As wsconsumer.clsStockPriceResponse, divIds As String, ByRef errormessages As List(Of String))


        If handle <> -1 Then

            Dim buyeraccount As clsAccount
            Dim queuedRequest As clsQueuedRequest = Nothing

            If PendingRequests.TryGetValue(handle, queuedRequest) Then

                buyeraccount = queuedRequest.BuyerAccount
                Dim lit As Literal
                If response.items.Count Then   'yey we have results

                    If divIds <> "" Then  'it's possibe we closed the DIV (whilst the request was pending)
                        Dim b()

                        Dim pnl As Panel
                        For Each ID As String In divIds.Split(",")  'each DIV id is of the form P_priceID (or S_Stockid)

                            If ID <> "" Then
                                lit = New Literal
                                lit.Text = "]" & ID & "^"  'This ASPX outputs ]DivID^replacamentContent  - which is merged back into the poage by the JS placePrices()
                                Form.Controls.Add(lit)

                                b = Split(ID, "_")
                                If b(0) = "P" Then
                                    'If b(1) <> "-1" Then  'todo - remove (to do with POA's and temporary variants  see getprices)
                                    '    minutesOld = 0 ' UI will RETURN the age of the price
                                    'pnl = iq.Products(b(1)).prices(b(2)).Ui 'Should go green
                                    If buyeraccount IsNot Nothing Then
                                        If Not iq.Prices.ContainsKey(b(1)) Then
                                            Dim jjj As Integer = 99

                                        Else


                                            Dim price = iq.Prices(b(1))
                                            price.lastUpdated = Now
                                            pnl = price.Ui(buyeraccount, 1, lid) 'Should go green


                                            'prices that have been created by late inserts - need their temporary references cleaned up
                                            If price.tempID < 0 Then
                                                If iq.Prices.ContainsKey(price.tempID) Then
                                                    iq.Prices.Remove(price.tempID)
                                                    price.tempID = 0
                                                Else
                                                    Dim kkk As Integer = 0
                                                End If
                                            End If
                                            End If

                                    Else
                                            errormessages.Add("* BuyerAccount was nothing in getPriceUIs")
                                            pnl = New Panel
                                    End If


                                    'minutesOld = iq.Prices(b(1)).MinutesOld
                                    'If minutesOld > 5 Then
                                    ' allDone = False
                                    ' End If

                                    Form.Controls.Add(pnl)
                                    ' Else
                                    '     Beep()
                                    ' Else
                                    '     Beep()
                                    ' End If

                                ElseIf b(0) = "S" Then
                                    ' the placeholder contains a panels (div) - which holds the stock UI  
                                    Dim ph As PlaceHolder = iq.Stock(b(1)).SKUvariant.StockUI(1, String.Empty, buyeraccount.Language, buyeraccount.SellerChannel)
                                    Form.Controls.Add(ph)
                                Else
                                    Beep()
                                End If
                            End If
                        Next
                    End If
                End If
            Else
                ' PendingRequests doesn't contain the handle - should never happen
                errormessages.Add(String.Format("* PendingRequests didn't contain expected handle {0}", handle))
            End If
        End If

    End Sub

    Private Sub updateStockPriceFromResponse(handle As Integer, response As wsconsumer.clsStockPriceResponse, ByRef errormessages As List(Of String))

        Dim buyeraccount As clsAccount

        If PendingRequests Is Nothing Then
            errormessages.Add("* Pending requests was nothing")
        Else
            If Not PendingRequests.ContainsKey(handle) Then
                errormessages.Add("* PendingRequests did not contain the handle:" & handle)
            Else
                For Each r In response.items


                    Try
                        Dim vs As IEnumerable(Of clsVariant) = (From rq In PendingRequests(handle).skuVariants Where rq.DistiSku = r.SKU)
                        If vs.Any Then
                            Dim v As clsVariant = vs.First
                            buyeraccount = PendingRequests(handle).BuyerAccount
                            updatePriceStock(buyeraccount, v, r)
                            If vs.Count > 1 Then errormessages.Add("* There were " & vs.Count & " rows returend for " & v.DistiSku & " expected 1")

                        Else
                            errormessages.Add("* The request contained no SKU:" & r.SKU)
                        End If
                    Catch
                        errormessages.Add("threading/multiple collection problem in updateStockpPiceFromResponse")
                    End Try

                Next
            End If
        End If



    End Sub


    Private Function callWsConsumer(handle As Integer, ByRef errorMessages As List(Of String)) As wsconsumer.clsStockPriceResponse

        Dim requester As wsconsumer.I_UniTranClient
        requester = New wsconsumer.I_UniTranClient

        requester.ClientCredentials.Windows.ClientCredential.Password = "iQuoteEXPERT"
        requester.ClientCredentials.Windows.ClientCredential.UserName = "DSVR016766\Nick.axworthy"

        Dim response As wsconsumer.clsStockPriceResponse
        Try
            'requester.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(1)
            'requester.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(1)
            'requester.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(5)

            response = requester.CheckStockPrices(handle, False, 0)  'Parameters : handle, isSyncronus, Timeout. Timeout valid for if isSyncronous set to true. This call is very fast - it returns the results sofar for the specified handle  (which may be an empty list).. and status   - TODO probably want to handle errors gracefully
            requester.Close()  'new
        Catch ex As System.Exception
            errorMessages.Add("*" & ex.Message)
            response = Nothing
        End Try

        Return response

    End Function

    Public Sub updatePriceStock(buyeraccount As clsAccount, v As clsVariant, item As wsconsumer.clsStockPriceItem)

        'Each Variant (product) is Seller-specific (but not Buyer specific) 
        Dim found As Boolean = False
        'each batch is a dictionary of HostPartnum > ProductVariant (a product-SKUvariant pair)
        'Each product has a dictioanry of sellers>variants>(arrival)dates>ClsStocks  - (containing a quantity, datestamp etc)


        With buyeraccount
            Dim price As clsPrice
            If v.i_prices.ContainsKey(buyeraccount.Priceband) Then

                Dim prices As Dictionary(Of clsCurrency, clsPrice) = v.i_prices(buyeraccount.Priceband)
                price = prices(.Currency)

                If item.status = "OK" Or item.status = "" Then

                    ' If item.SKU.Contains("3") Then Stop
                    If item.CustomerPrice > 0 Then
                        price.Price.value = item.CustomerPrice
                        price.Price.isValid = True  'Important ! (otherwise POA's remain 'invalid' event though they now have a value
                        price.Price.isList = False  'In case it was a (temporary) list price - (it is'nt now!)
                        price.Source = "Confirmed"

                    End If
                Else
                    If item.message Is Nothing Then
                        price.Source = item.status
                    Else
                        price.Source = item.message
                    End If
                End If

                If price.ID < 0 Then
                    Dim tempid As Integer = price.ID
                    'do the INSERT

                Else
                    price.Update()  'Expensive
                End If
            Else
                'should never happen ! - the webservice created a POA price in advance
                Dim newprice As clsPrice = New clsPrice(v, buyeraccount.Priceband, New NullablePrice(item.CustomerPrice, .Currency, False), "Wesbservice (updatePriceStock add)")
                price = newprice
            End If

            If price.Price.isValid Then
                If iq.sesh(Request.QueryString("lid"), "QuoteID") IsNot Nothing Then

                    Dim lid As UInt64 = CType(Request.QueryString("lid"), UInt64)
                    Dim quote As clsQuote = buyeraccount.Quotes(iq.sesh(lid, "QuoteID"))
                    quote.RootItem.updateQuotedPrice(v, price.Price)  'Recurses through every item in the quote - updating them IF they have this price
                End If
            End If
        End With

        If v.shipments Is Nothing Then
            Dim newstockrecord As clsstock = New clsstock(v, item.stock(0).quantity, item.stock(0).arrival, "New stock record (created by UpdatePriceStock()", True)
        End If

        'dates on which shipments of variants arrive
        For Each shipment As wsconsumer.clsShipment In item.stock

            If v.shipments.ContainsKey(shipment.arrival) Then  'update an exisiting shipment
                With v.shipments(shipment.arrival)
                    .quantity = shipment.quantity
                    .LastUpdated = Now()
                    .update()
                End With
            Else
                'make a new stock record for this shipment (will INSERT to the database and Update product.i_Stock
                '' This wasn't such a good idea - issues with the shipments ID changing when archived -  If shipment.arrival.Date = CDate("01/01/2000").Date Then v.ArchiveCurrentStock() 'removes it from the product.i_stock AND sets the archived flag

                'in this instance, there was no stock record - so there is no stockUI to replace - so the stock doesnt show... we need to refesh the whole branch instead

                Dim newStockRecord = New clsstock(v, shipment.quantity, shipment.arrival, "WS", shipment.isCurrent)
            End If
        Next

    End Sub

End Class