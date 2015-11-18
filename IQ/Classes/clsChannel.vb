Imports dataAccess
Imports System.Globalization
Imports System.Collections.Concurrent
Imports System.Threading

<Serializable>
Public Class clsChannel
    Implements i_Editable

    'NB:-ALL channels are clones ! - they have an IsCloneOf member - it's just that 'non clones' are clones of themselves.
    'This way we can *always* present pricing as that of the channels clone (wether it's a clone or not)

    Property ID As Integer
    Property Name As String
    Property BusinessName As String
    Property Address As String
    Property Code As String 'IQ1 HostID
    'Public ChannelAcID As String ' buyer account number
    Property Users As Dictionary(Of Integer, clsUser) 'users (who work at this channel - and are generally buyers at another channel, or sales agents at this one)
    Property Region As clsRegion 'Country As clsCountry - previously country - but countries *are* now regions
    Property Teams As Dictionary(Of Integer, clsTeam)
    Property CustomerAccounts As Dictionary(Of Integer, clsAccount) 'the people this channel sells to
    Property WebToken As String 'Used as a unique (and 'unguessable') token (instead of a username/password) for webservice operations
    '                              buyer                   sector          
    Property Margin As Dictionary(Of clsChannel, Dictionary(Of clsSector, clsMargin))
    Property IsCloneOf As clsChannel
    Property Parent As clsChannel   'Channels are placed in a heirarchy for organisational/display puproses.. much like threads
    Property Children As Dictionary(Of Integer, clsChannel)  'we use a dictionary - rather than a list, so that indiviual elements can be addressed for editing

    Property pic1 As nullableString
    Property pic2 As nullableString
    Property URL As nullableString

    Property TreePath As String
    Property Focus As String  'Recta,smartbuy etc. (intinital filter against the 'Focus' Attributte of products. (can be a CD list)  'iq.dbo.countries.hpreceta
    Property Domains As List(Of String)
    Property Campaigns As Dictionary(Of Integer, clsCampaign)
    Property marginMin As Single 'Most negative permissable margin (negative margin is reducing the cost)
    Property marginMax As Single 'largest allowable margin (markup)
    Property marginType As String 'R' or 'C' for  Retained or 'CostPlus'
    Property Legal As String 'host specific terms and conditions 
    Property SchemeOverride As String 'Host specific loyalty points scheme codes (comma delimited list) - having an entry here will override the usual (regionalised) Loyalty schems
    Property DefaultCurrency As clsCurrency
    Property Universal As Boolean
    Property orderEmail As String
    Property basketMode As String
    Property basketURL As String

    '    Public Variants As Dictionary(Of clsProduct, List(Of clsVariant))

    Property priceConfig As Int16 'eger ' contains a set of bitwise flags controlling which prices (and therefore products) are displayed - Per SELLER channel (at the moment - could be moved to Buyer channel, or even account without great difficulty)


    'Public variantsLoaded As Integer 'used on the seller channel - to indicate that the variants (containing host partnumbers, and indexing prices) are loaded
    Public variantsLoadedAt As DateTime
    Public pricesLoadedFor As Dictionary(Of clsPriceBand, Integer) 'used on sellerchannel - to indicate how many prices have been loaded for each priceband
    Public listPricesLoadedFor As Dictionary(Of clsRegion, Integer) 'Specific to the HP channel - and used to know whether to load list prices for the users region at signin
    Public stockLoaded As Boolean


    'DistiSKU|wharehousecode>clsVariant - compound key (NB warehouse will *often* be blank
    Private i_variantCK As Dictionary(Of String, clsVariant)  'use the FindVariant public helper function to get to his
    'Private SKUs As Dictionary(Of clsProduct, List(Of clsVariant)) 'the variant(s) contains the DistsSKU(s) (or blank if they don't have one ..ie they use the HP partNumber

    Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName
        DisplayName = Name & " (" & Region.Name.text(language) & ")"
    End Function


    ''' <summary>
    ''' If the channel has no teams, makes one (an Everyone) .. and assigns all existintg users to it
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub fixteams(ByRef errormessages As List(Of String))
        If (Me.Teams.Values.Count < 1) Then

            Dim newTeam As clsTeam = New clsTeam(Me, "Everyone")
            Dim existingUser As clsUser = New clsUser()

            For Each existingUser In Me.Users.Values
                'If existingUser.Accounts.Count = 0 Then
                '    Dim newAccount As clsAccount = New clsAccount(existingUser, )
                '    existingUser.Accounts.Add(agentaccount.SellerChannel.ID, agentaccount)
                '    existingUser.update()
                'End If

                Dim userAccounts = From j In existingUser.Accounts.Values Where j.SellerChannel Is Me
                If userAccounts.Any Then
                    userAccounts.First.Team = newTeam
                    userAccounts.First.Team.update(errormessages)
                End If
            Next
        End If
    End Sub

    Public Function countVariants() As Integer
        Return Me.i_variantCK.Count
    End Function

    Public Function Insert(ByRef errorMessages As List(Of String)) As Object Implements i_Editable.Insert

        Dim achannel As clsChannel = New clsChannel(Me.Parent, Me.Name, Me.BusinessName, Me.Address, Me.Code, Me.Region, Me.pic1, Me.pic2, Me.URL, Me.priceConfig, Me.TreePath, Me.Focus, Me.marginMin, Me.marginMax, Me.marginType, Me.SchemeOverride, Me.Legal, Nothing, Me.Universal, Me.orderEmail, Me.basketMode, Me.basketURL)
        Return achannel

    End Function

    Public Function deIndexVariant(v As clsVariant, errorMessages As List(Of String)) As Boolean

        If i_variantCK.ContainsKey(v.CK) Then
            i_variantCK.Remove(v.CK)
            Return True
        Else
            errorMessages.Add("Could not locate " & v.CK & "")
            Return False
        End If

    End Function

    Public Sub indexVariant(v As clsVariant)
        If Not Me.i_variantCK.ContainsKey(v.CK) Then Me.i_variantCK.Add(v.CK, v)
    End Sub


    ''' <summary>Returns a specific variant (matching DistiSku) - Variants effectively join, sellers, products and warehouses - allowing us to store different price and stock per variant/buyer. </summary>
    Public Function findVariant(DistiSKU As String, warehouse As String, ByRef result As clsStockPriceSvc.clsResult, ByRef SKUvariant As IQ.clsVariant) As Boolean

        'mfsrSKU is the hostManufacturer part number and may contain a #

        findVariant = False
        result = Nothing  'Any error msg
        ' Dim product As clsProduct

        'mfrsku = Split(mfrsku, "#")(0)  'Not sure about this - all IQ2 Mfrpartnums have no #
        ' Dim stub As String = Split(MfrSku, "#")(0)

        Dim ck As String = DistiSKU & "|" & warehouse
        If Not Me.i_variantCK.ContainsKey(ck) Then
            result = New clsStockPriceSvc.clsResult(False, Me.i_variantCK.Count & " variants - none with the host SKU|warehouse combo  " & ck & " use AddVariant to add new variants.", 56)
        Else
            result = New clsStockPriceSvc.clsResult(True, "OK", 0)
            SKUvariant = Me.i_variantCK(ck)
            findVariant = True
        End If

    End Function


    ''' <summary>Maintains a Distis portfolio - Calls the allProducts method on wsConsumer - which proxies the Distis AllProducts Method / OR returns variants based on the pricing database</summary>
    ''' <remarks></remarks>
    Public Function freshenVariants(errorMessages As List(Of String)) As String

        'Note this is called *after* the variants are loaded
        'called on the seller channel

        Dim cl As wsconsumer.I_UniTranClient = New wsconsumer.I_UniTranClient()
        cl.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(10)
        cl.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(10)


        Try
            Dim SKUlist() As String = cl.AllProducts(Me.Code)

            If SKUlist.Count < 10 Then

                errorMessages.Add("Crazy small feed ! - nothing freshened")

                Return "Refresh failed"

            Else

                Dim FeedCKs As HashSet(Of String) 'we construct a list of all the CK's in the response (to check for dupes) - Note a HashSet is much faster than a list for Contians ops.
                FeedCKs = New HashSet(Of String)
                Dim added As Integer = 0
                Dim deleted As Integer = 0
                Dim existed As Integer = 0

                Dim con As SqlClient.SqlConnection = da.OpenDatabase

                Dim nvid As Integer = 0
                Dim vwc As DataTable = da.MakeWriteCacheFor(con, "variant", nvid, True)

                'errorMessages.Clear()
                'HP renew parts don't have list prices !! - really not sure what the fix is

                FeedCKs.Clear()
                For Each line In SKUlist

                    If Trim(line) = "" Then
                        errorMessages.Add("blank line in AllProducts response")
                    End If

                    'MrfSKU|DistisSKu|Warehouse
                    Dim bits() = Split(line, "|")
                    Dim mfrSKU = bits(0)
                    Dim distisku = bits(1)

                    If bits.Count > 3 Then
                        errorMessages.Add(line & " in AllProducts response contained too many segments.")
                    Else
                        Dim warehouse As String = ""
                        If bits.Count = 3 Then warehouse = bits(3)

                        Dim ck As String = distisku & "|" & warehouse
                        If FeedCKs.Contains(ck) Then
                            'Duplicate (By DisitSKU|Warwhouse)
                            errorMessages.Add("Duplicated line " & line)
                        Else
                            FeedCKs.Add(ck)
                            If Me.i_variantCK.ContainsKey(ck) Then  'warehouse
                                'all good - the variant exists
                                existed += 1
                            Else
                                'Need to create a new variant
                                Dim product As clsProduct
                                If iq.i_SKU.ContainsKey(mfrSKU) Then
                                    product = iq.i_SKU(mfrSKU)
                                    'this could be *much* faster with a 'writecahe' (but additions should 
                                    Dim newVariant As clsVariant = New clsVariant("", product, Me, distisku, "", warehouse, "", Nothing, False, vwc, nvid)
                                    added += 1
                                Else
                                    If errorMessages.Count < 100 Then
                                        errorMessages.Add("Unrecognised part " & mfrSKU & " could not create variant")
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next

                da.BulkWrite(con, vwc, "variant", batchsize:=1000, writeIDs:=True)
                con.Close()

                For Each ck In Me.i_variantCK.Keys.ToArray
                    If Not ck.Contains("FAKE") Then 'Pauls fake parts for the Unhosted instance (so he can add anything to a basket)
                        If Not FeedCKs.Contains(ck) AndAlso Not Me.i_variantCK(ck).DistiSku.StartsWith("###") AndAlso Not Me.i_variantCK(ck).Deleted Then
                            'variant no longer in the feed - 'delete' it
                            deleted += 1
                            Me.i_variantCK(ck).Delete(errorMessages) 'NOTE - We NEVER actually delete variants - becuase they're referenced by quote items -the are flagged as deleted - and removed from the indicies
                        End If
                    End If
                Next

                Return Me.Name & "(" & Me.Code & ") - FreshenVariants - Existed:" & existed & " Added:" & added & " Deleted:" & deleted

            End If
        Catch ex As System.Exception

            ErrorLog.Add(ex)
        End Try


    End Function

    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        errorMessages.Add("Delete is not yet Implemented on the Channel object")

    End Sub

    Public Function LoadStock() As String


        Dim errorMessages As List(Of String) = New List(Of String)
        Dim ts As Double = Stopwatch.GetTimestamp

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim r As SqlClient.SqlDataReader

        Dim dzero As String = da.UniversalDate(CDate("01/01/2000"))

        Dim sql$
        sql$ = "SELECT stock.ID,fk_variant_id,v.fk_product_id,v.fk_channel_id_seller,quantity,Arrival,datestamp,iscurrent "
        sql$ &= "FROM [stock] "
        sql$ &= "JOIN [variant] AS v ON v.id=fk_variant_id  "
        sql$ &= " WHERE fk_channel_id_seller=" & Me.ID & " AND arrival = " & dzero & " AND isCurrent = 1 Or Arrival > " & da.UniversalDate(DateAdd(DateInterval.Day, -30, Now))

        r = da.DBExecuteReader(con, sql$)

        Dim count As Integer
        Dim Product As clsProduct
        Dim Seller As clsChannel
        Dim stock As clsstock
        Dim SKUvariant As clsVariant
        Dim duds As Integer = 0

        If r.HasRows Then
            While r.Read

                Seller = iq.Channels(r.Item("fk_channel_id_seller"))

                Dim pid As Integer = r.Item("fk_product_id")
                If Not iq.Products.ContainsKey(pid) Then
                    If duds < 10 Then
                        Logit(Me.DisplayName(English) & " LoadStock referenced product " & pid & " which is not in the OM of " & iq.Products.Count & " products")
                        duds += 1
                    End If

                Else

                    If iq.Products.ContainsKey(pid) Then
                        Product = iq.Products(pid)
                    Else
                        Product = iq.REMAPS(pid)
                    End If

                    Dim vid As Integer = r.Item("fk_variant_id")
                    If Not iq.Variants.ContainsKey(vid) Then
                        'Logit(Me.DisplayName(English) & " LoadStock referenced variant " & vid & " which is not in the OM")
                    Else
                        SKUvariant = iq.Variants(vid)
                        'just creating the stock adds it to the product AND iq.stock (the 'flat' list used for import
                        stock = New clsstock(r.Item("ID"), SKUvariant, r.Item("quantity"), r.Item("arrival"), r.Item("datestamp"), r.Item("isCurrent"), errorMessages)
                        count += 1
                    End If
                End If
            End While
        End If
        r.Close()
        con.Close()
        con.Dispose()

        Me.stockLoaded = True

        Dim v$ = "Loaded " & count & " Stock records in " & TimeSince(ts) & "<br/>"

        For Each e In errorMessages
            v$ &= "<p>" & e & "</p>"
            If Len(v$) > 1000 Then Exit For
        Next

        Return v$

    End Function


    ''' <summary>Loads (from the DB) the variants for this channel - and 'freshens' them via a webservice call if necessary</summary>
    ''' <param name="errormessages"></param>
    ''' <param name="maxAgeHrs">Variants will not be re-loaded if they are 'fresher' than this</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function LoadVariants(ByRef errormessages As List(Of String), maxAgeHrs As Single) As String

        'Called on the sellerchannel (when an account is selected at login) - to load the SKUvariants - (loosely equivilent to IQ1 tbhostPartnums)
        'Doing this 'just in time' (per channel) like this saves around 400MB and 5 seconds from the OBject model and its startup time

        Dim ts As Single = Stopwatch.GetTimestamp
        Dim count As Integer = 0
        Dim dupes As Integer = 0
        Dim bad As Integer = 0

        If Me.variantsLoadedAt = Nothing Then

            'Read them from the IQ2 Database

            Dim seller As clsChannel = Me 'just for clarity
            Dim con As SqlClient.SqlConnection = da.OpenDatabase
            Dim r As SqlClient.SqlDataReader


            'NB: - we don't load deleted variants
            r = da.DBExecuteReader(con, "SELECT v.id,code,distiSKU,fk_channel_id_seller,fk_product_id,displaytext,warehouse,localisation,fk_region_id,v.deleted  " & _
                                   "FROM [Variant] v inner join Product p on p.id = fk_product_id WHERE p.deleted = 0 and  fk_channel_id_seller=" & Me.ID)

            Dim v As clsVariant
            Dim product As clsProduct
            Dim region As clsRegion
            Dim warehouse As String
            Dim distiSKU As String

            Me.i_variantCK.Clear()

            While r.Read
                'iq.Channels(r.Item("fk_channel_id")).addSKU(iq.Products(r.Item("fk_product_id")), iq.Variants(r.Item("fk_variant_id")), r.Item("channelSKU"))

                distiSKU = r.Item("distiSKU")

                Dim pid As Integer = r.Item("fk_product_id")
                If iq.Products.ContainsKey(pid) Then
                    product = iq.Products(pid)
                Else
                    bad += 1
                    Continue While
                    product = iq.REMAPS(pid)
                End If

                '    seller = iq.Channels(r.Item("fk_channel_id_seller")) this IS ME
                region = Nothing
                If r.Item("fk_region_id") IsNot DBNull.Value Then region = iq.Regions(r.Item("fk_region_id"))
                warehouse = r.Item("warehouse")


                If region IsNot Nothing AndAlso region.Code = "CO" Then
                    Dim a = 0
                End If

                If distiSKU = "" Then
                    If errormessages.Count < 10 Then
                        errormessages.Add("distiSKU was blank for variant " & r.Item("ID").ToString)
                    End If
                Else
                    If Me Is HP AndAlso i_variantCK.ContainsKey(distiSKU & "|" & region.Code) Then
                        If errormessages.Count < 10 Then
                            errormessages.Add("Duplicate HP variant " & distiSKU & "|" & region.Code)
                        End If

                        dupes += 1
                    ElseIf i_variantCK.ContainsKey(distiSKU & "|" & warehouse) Then
                        v = New clsVariant(CInt(r.Item("id")), r.Item("code"), product, Me, distiSKU, r.Item("displaytext"), warehouse, r.Item("Localisation"), region, r.Item("deleted"), False)
                        If errormessages.Count < 10 Then
                            errormessages.Add("Duplicate variant " & distiSKU & "|" & warehouse & " for " & Me.Code & "(" & Me.Name & ")")
                        End If
                        dupes += 1
                    Else
                        v = New clsVariant(CInt(r.Item("id")), r.Item("code"), product, Me, distiSKU, r.Item("displaytext"), warehouse, r.Item("Localisation"), region, r.Item("deleted"), True)
                        count += 1
                    End If
                End If
            End While

            r.Close()
            con.Close()
            con.Dispose()

            Me.variantsLoadedAt = DateAdd(DateInterval.Hour, -100, Now) 'force a freshen

        End If

        If (Math.Abs(DateDiff(DateInterval.Hour, Now, Me.variantsLoadedAt))) > maxAgeHrs Then
            'Freshen' them from the webservice (this may add and delete variants!)

            Me.variantsLoadedAt = Now 'we set this 'early' so that it's not called twice for multiple users logging int simultaneousness
            Dim j = Me.freshenVariants(errormessages)

        End If

        Return "Loaded " & count & " variants SKUs in " & TimeSince(ts) & " skipped " & bad & " bad (deleted products)<br/>"

    End Function


    Public Function LoadPrices(PriceBand As clsPriceBand, ByRef errorMessages As List(Of String), Optional region As clsRegion = Nothing) As String
        ' If Environment.MachineName = "LINGM-LAPTOP" Then Exit Function
        'Called on the sellerchannel, passing the buyerchannel to load the prices for/of the buyerchannel

        Dim ts As Single = Stopwatch.GetTimestamp

        Dim already As Integer = 0

        Dim Seller As clsChannel = Me

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim r As SqlClient.SqlDataReader

        'r = da.dbexecuteReader(con, "SELECT Id,fk_product_id,fk_variant_id,fk_channel_id_seller,fk_channel_id_buyer,price,fk_currency_id,datestamp,source from [Price]")
        Dim sql$

        sql$ = "SELECT Price.Id as priceID,V.FK_PRODUCT_ID,fk_variant_id,v.fk_channel_id_seller,priceband,price,fk_currency_id,datestamp,source "
        sql$ &= "FROM [Price]"
        sql$ &= "JOIN [Variant] AS v on v.id= fk_variant_id "
        sql$ &= "JOIN [product] AS p on p.id= v.fk_product_id "
        sql$ &= "WHERE fk_channel_id_seller=" & Seller.ID & " AND priceband='" & PriceBand.text & "'"
        sql$ &= "AND p.deleted = 0 and v.deleted = 0"

        If region IsNot Nothing Then
            sql$ &= " AND fk_region_id=" & region.ID
        End If

        r = da.DBExecuteReader(con, sql$)

        Dim count As Integer
        Dim aPrice As clsPrice
        Dim Product As clsProduct
        'Dim buyer As clsChannel = buyerchannel
        Dim Currency As clsCurrency
        Dim price As Decimal
        Dim SKUvariant As clsVariant

        If r.HasRows Then
            While r.Read

                Dim vid As Integer = r.Item("fk_variant_id")

                If iq.Variants.ContainsKey(vid) Then
                    SKUvariant = iq.Variants(vid)

                    If Not SKUvariant.prices.ContainsKey(r.Item("PriceID")) Then 'check its not already loaded

                        Dim pid As Integer = r.Item("fk_product_id")

                        If iq.Products.ContainsKey(pid) Then  'SHOULD mbe removed (but without it it crashes!)
                            Product = iq.Products(pid)
                        Else
                            Product = iq.REMAPS(pid)
                        End If

                        Currency = iq.Currencies(r.Item("fk_currency_id"))
                        price = r.Item("price")

                        'will add the price into both the master price list - and into the product.price(seller)(buyer)(currency)
                        Dim datestamp As DateTime
                        If IsDBNull(r.Item("datestamp")) Then
                            datestamp = Now
                        Else
                            datestamp = r.Item("datestamp")
                        End If

                        'WE DONT WANT ZERO PRICES (for now)
                        If price <> 0 Then
                            If r.Item("priceid") < 1 Then errorMessages.Add("a price has an id <1")
                            aPrice = New clsPrice(r.Item("priceid"), SKUvariant, iq.getPriceBand(r.Item("Priceband")), price, Currency, datestamp, r.Item("source"))
                        End If

                        count += 1

                    Else
                        already += 1
                    End If
                Else
                    'missing variant ??
                End If

            End While
        End If
        r.Close()
        r.Close()
        con.Close()
        con.Dispose()

        If Not Me.pricesLoadedFor.ContainsKey(PriceBand) Then pricesLoadedFor.Add(PriceBand, 0)
        Me.pricesLoadedFor(PriceBand) = CInt(count + already)

        'only list prices are region specific... we track how many were loaded to know wther we need to load them for the users country at logon
        If region IsNot Nothing Then
            If Not Me.listPricesLoadedFor.ContainsKey(region) Then Me.listPricesLoadedFor.Add(region, 0)
            Me.listPricesLoadedFor(region) = count + already
        End If

        Return "Loaded " & count & " Prices in " & TimeSince(ts) & " " & already & " were already loaded<br/>"

    End Function


    Public Sub Update(ByRef errorMessages As List(Of String)) Implements i_Editable.update

        Dim sql$
        sql$ = "UPDATE CHANNEL SET "
        sql$ &= "FK_Channel_id_cloneof=" & Me.IsCloneOf.ID & ","
        If Me.Parent Is Nothing Then
            sql$ &= "FK_Channel_id_parent=null,"
        Else
            sql$ &= "FK_Channel_id_parent=" & Me.Parent.ID & ","
        End If

        sql$ &= "Name=" & da.SqlEncode(Me.Name) & ","
        sql$ &= "Address=" & da.SqlEncode(Me.Address) & ","
        sql$ &= "FK_Region_ID=" & Me.Region.ID & ","
        sql$ &= "webtoken=" & da.SqlEncode(Me.WebToken) & ","
        sql$ &= "code=" & da.SqlEncode(Me.Code) & ","
        sql$ &= "pic1=" & Me.pic1.sqlValue & ","
        sql$ &= "pic2=" & Me.pic2.sqlValue & ","
        sql$ &= "URL=" & Me.URL.sqlValue & ","
        sql$ &= "priceconfig=" & Me.priceConfig & ","
        sql$ &= "focus=" & da.SqlEncode(Me.Focus) & ","
        sql$ &= "treepath=" & da.SqlEncode(Me.TreePath) & ","
        sql$ &= "marginMin=" & Me.marginMin & ","
        sql$ &= "marginMax=" & Me.marginMax & ","
        sql$ &= "marginType=" & da.SqlEncode(Me.marginType) & ","
        sql$ &= "schemeOverride=" & da.SqlEncode(Me.SchemeOverride) & ","
        sql$ &= "legal=" & da.SqlEncode(Me.Legal) & ","
        sql$ &= "fk_currency_id_default="
        If Me.DefaultCurrency Is Nothing Then
            sql$ &= "null,"
        Else
            sql$ &= Me.DefaultCurrency.ID & ","
        End If
        sql$ &= "universal=" & IIf(Me.Universal, "1", "0") & ","
        sql$ &= "orderEmail=" & da.SqlEncode(Me.orderEmail) & ","
        sql$ &= "basketMode=" & da.SqlEncode(Me.basketMode) & ","
        sql$ &= "basketURL=" & da.SqlEncode(Me.basketURL)

        sql$ &= " WHERE ID = " & Me.ID

        da.DBExecutesql(sql)

    End Sub

    'Public Function addSKU(Product As clsProduct, SKUvariant As clsVariant)

    '    If Not Me.SKUs.ContainsKey(Product) Then Me.SKUs.Add(Product, New List(Of clsVariant))
    '    Me.SKUs(Product).Add(SKUvariant)

    'End Function

    'Public Function ChannelSKUs(product) As Dictionary(Of clsVariant, String)

    '    'returns a dictionary of all the variants>DistiSKUs - for the sepcifed product

    '    ChannelSKUs = New Dictionary(Of clsVariant, String) 'return an empty dictionary by default
    '    If SKUs.ContainsKey(product) Then
    '        Return SKUs(product)
    '    End If

    'End Function

    'Public Function ChannelSKU(product As clsProduct, skuvariant As clsVariant) As String

    '    ChannelSKU = ""
    '    If SKUs.ContainsKey(product) Then
    '        If SKUs(product).ContainsKey(skuvariant) Then
    '            Return SKUs(product)(skuvariant)
    '        End If
    '    End If
    'End Function

    Public Sub New(Parent As clsChannel, ByVal Name As String, ByVal BusinessName As String, ByVal Address As String, code As String, ByVal Region As clsRegion, pic1 As nullableString, pic2 As nullableString, url As nullableString, priceConfig As Integer, treepath As String, focus As String, marginMin As Single, MarginMax As Single, MarginType As String, SchemeOverride As String, Legal As String, DefaultCurrency As clsCurrency, universal As Boolean, orderEmail As String, basketMode As String, basketURL As String, Optional writecache As DataTable = Nothing, Optional ByRef nextID As Integer = -1)

        'EVERY channel is created AS A CLONE, AND PARENT OF ITSELF
        'Those that are actually clones of something else are subsequenty UPDATED

        Dim aguid As New Guid
        Me.WebToken = aguid.ToString("D")

        'This bit is important to understand - where isCloneOf is passed as nothing .. it will insert a row that refers to itself

        Dim pid As String
        If Parent Is Nothing Then pid = "null" Else pid = Parent.ID.ToString

        Me.Name = Name
        Me.BusinessName = BusinessName
        Me.Address = Address
        Me.IsCloneOf = Me

        Me.Code = Trim$(code)
        Me.Region = Region

        If (DefaultCurrency Is Nothing) AndAlso (Not iq.DefaultCurrencies Is Nothing) AndAlso (iq.DefaultCurrencies.ContainsKey("USD")) Then
            DefaultCurrency = iq.DefaultCurrencies("USD")
        End If

        Me.Users = New Dictionary(Of Integer, clsUser)
        Me.Teams = New Dictionary(Of Integer, clsTeam)

        Me.pic1 = pic1
        Me.pic2 = pic2
        Me.URL = url
        Me.priceConfig = priceConfig
        Me.Children = New Dictionary(Of Integer, clsChannel)
        Me.TreePath = treepath
        Me.Focus = focus
        Me.Domains = New List(Of String)
        Me.marginMax = MarginMax  'These are the margins applied via buttons in the basket
        Me.marginMin = marginMin
        Me.marginType = MarginType
        Me.SchemeOverride = SchemeOverride
        Me.Legal = Legal
        Me.DefaultCurrency = DefaultCurrency
        Me.Universal = universal
        Me.orderEmail = orderEmail
        Me.basketMode = basketMode
        Me.basketURL = basketURL

        If writecache Is Nothing Then

            Dim sql$
            sql$ = "INSERT INTO Channel (fk_channel_id_parent,Name,BusinessName,Address,fk_region_id,webtoken,code,pic1,pic2,url,FK_Channel_ID_CloneOf,priceconfig,treepath,focus,marginMax,marginMin,marginType,SchemeOverride,legal,FK_Currency_ID_Default,Universal,orderEmail,basketMode,basketURL) "
            sql$ &= "VALUES (" & pid & "," & da.SqlEncode(Name) & "," & da.SqlEncode(BusinessName) & "," & da.SqlEncode(Address) & "," & Region.ID & ",'" & Me.WebToken & "'," & da.SqlEncode(code) & "," & pic1.sqlValue & "," & pic2.sqlValue & "," & url.sqlValue & ","
            sql$ &= "IDENT_CURRENT('Channel')," & priceConfig & "," & da.SqlEncode(treepath) & "," & da.SqlEncode("focus") & ","
            sql$ &= MarginMax & "," & marginMin & "," & da.SqlEncode(MarginType) & "," & da.SqlEncode(SchemeOverride) & "," & da.SqlEncode(Legal) & "," & DefaultCurrency.ID & "," & IIf(universal, "1", "0") & "," & da.SqlEncode(orderEmail) & "," & da.SqlEncode(basketMode) & "," & da.SqlEncode(basketURL)
            sql$ &= ");"  '<<COID

            Me.ID = da.DBExecutesql(sql$, True)
        Else

            If nextID = -1 Then Stop

            Dim row As System.Data.DataRow
            row = writecache.NewRow()

            Me.ID = nextID

            '            row("ID") = Me.ID - there are 'autonumbers'

            row("FK_channel_id_parent") = IIf(pid = "null", DBNull.Value, pid)
            row("Name") = Name
            row("BusinessName") = BusinessName
            row("Address") = Address
            row("fk_region_id") = Region.ID
            row("webtoken") = Me.WebToken
            row("code") = code
            row("pic1") = pic1.value
            row("pic2") = pic2.value
            row("url") = url.value
            row("FK_Channel_ID_cloneof") = nextID '  coid - clone of self
            row("priceconfig") = priceConfig
            row("treepath") = treepath
            row("focus") = focus
            row("MarginMax") = MarginMax
            row("MarginMin") = marginMin
            row("MarginType") = MarginType
            row("schemeOverride") = MarginType
            row("Legal") = Legal
            row("FK_Currency_ID_Default") = DefaultCurrency.ID
            row("universal") = universal
            row("orderemail") = orderEmail
            row("basketMode") = basketMode
            row("basketURL") = basketURL

            nextID += 1
            writecache.Rows.Add(row)

        End If

        iq.Channels.Add(Me.ID, Me)

        ' If Not iq.i_channel_code.ContainsKey(Me.Code) Then  'this shouldn't be needed and imples a problem
        iq.i_channel_code.Add(Me.Code, Me)
        '   End If

        If iq.Channels.Count = 1 Then
            If Not Me.Parent Is Nothing Then Stop 'The root channel should not have a parent
            iq.RootChannel = Me
        End If

        If Not Me.Parent Is Nothing Then
            Me.Parent.Children.Add(Me.ID, Me)  'add me to my parents children (to create the heirarchy)
        End If

        Me.CustomerAccounts = New Dictionary(Of Integer, clsAccount)
        Me.Campaigns = New Dictionary(Of Integer, clsCampaign)


        Me.Margin = New Dictionary(Of clsChannel, Dictionary(Of clsSector, clsMargin))
        '        SKUs = New Dictionary(Of clsProduct, clsVariant)

        Me.pricesLoadedFor = New Dictionary(Of clsPriceBand, Integer) 'used on sellerchannel - to indicate which buyers prices have been loaded for 
        Me.listPricesLoadedFor = New Dictionary(Of clsRegion, Integer)
        Me.i_variantCK = New Dictionary(Of String, clsVariant)  'DistiSKU|Warehouse>Variant


    End Sub
    Public Sub New()

        Me.ID = -1
        Me.Parent = Nothing

        Me.Users = New Dictionary(Of Integer, clsUser)

        Me.Region = iq.Regions.Values(0)
        Me.Teams = New Dictionary(Of Integer, clsTeam)
        Me.CustomerAccounts = New Dictionary(Of Integer, clsAccount) 'the people this channel sells to

        Dim aguid As New Guid
        Me.WebToken = aguid.ToString("D")

        '                              buyer                   sector       margin
        Margin = New Dictionary(Of clsChannel, Dictionary(Of clsSector, clsMargin))

        Me.Children = New Dictionary(Of Integer, clsChannel)
        Me.pic1 = New nullableString
        Me.pic2 = New nullableString
        Me.URL = New nullableString
        Me.TreePath = ""
        Me.Focus = ""
        Me.Domains = New List(Of String)
        Me.marginMax = 0
        Me.marginMin = 0
        Me.marginType = "R"
        Me.Legal = ""
        Me.SchemeOverride = ""
        Me.DefaultCurrency = Nothing
        Me.Universal = False
        Me.orderEmail = ""
        Me.basketMode = ""
        Me.basketURL = ""


        'SKUs = New Dictionary(Of clsProduct, Dictionary(Of clsVariant, String)) ' This channels 'internal' SKU for each product (they sell) - the first dimension contains a compound key of Product.id+Variant.id

        Me.pricesLoadedFor = New Dictionary(Of clsPriceBand, Integer) 'used on sellerchannel - to indicate how many prices have  been loaded for each buyer
        Me.listPricesLoadedFor = New Dictionary(Of clsRegion, Integer)

        Me.Campaigns = New Dictionary(Of Integer, clsCampaign)
        Me.i_variantCK = New Dictionary(Of String, clsVariant)

    End Sub

    Public Sub New(ByVal ID As Integer, Parent As clsChannel, ByVal Name As String, ByVal BusinessName As String, CloneOf As clsChannel, ByVal Address As String, code As String, ByVal region As clsRegion, webtoken As String, pic1 As nullableString, pic2 As nullableString, url As nullableString, priceConfig As Integer, treepath As String, focus As String, marginMin As Single, MarginMax As Single, MarginType As String, SchemeOverride As String, Legal As String, DefaultCurrency As clsCurrency, universal As Boolean, orderEmail As String, basketMode As String, basketURL As String)

        Me.ID = ID
        Me.Parent = Parent
        Me.Name = Name
        Me.BusinessName = BusinessName
        Me.IsCloneOf = IsCloneOf
        Me.Address = Address
        Me.Region = region
        Me.Code = Trim$(code)
        Me.Users = New Dictionary(Of Integer, clsUser)
        Me.Teams = New Dictionary(Of Integer, clsTeam)
        Me.WebToken = webtoken
        Me.pic1 = pic1
        Me.pic2 = pic2
        Me.URL = url
        Me.priceConfig = priceConfig
        Me.TreePath = treepath
        Me.Focus = focus
        Me.Domains = New List(Of String)
        Me.marginMax = MarginMax  'These are the margins applied via buttons in the basket
        Me.marginMin = marginMin
        Me.marginType = MarginType
        Me.SchemeOverride = SchemeOverride
        Me.Legal = Legal
        Me.DefaultCurrency = DefaultCurrency
        Me.Universal = universal
        Me.orderEmail = orderEmail
        Me.basketMode = basketMode
        Me.basketURL = basketURL

        If Me.IsCloneOf Is Nothing Then Me.IsCloneOf = Me '!IMPORTANT  (when we're loding channels from the DB they're not yet in the dictionary - so we cant point them to themselves - this work's around that
        Me.Children = New Dictionary(Of Integer, clsChannel)

        iq.Channels.Add(Me.ID, Me)
        iq.i_channel_code.Add(Me.Code, Me)

        If iq.Channels.Count = 1 Then
            If Not Me.Parent Is Nothing Then Stop 'The root channel should not have a parent
            iq.RootChannel = Me
        End If

        If Not Me.Parent Is Nothing Then
            Me.Parent.Children.Add(Me.ID, Me)   'add me to my parents children (to create the heirarchy)
        End If

        CustomerAccounts = New Dictionary(Of Integer, clsAccount)
        Margin = New Dictionary(Of clsChannel, Dictionary(Of clsSector, clsMargin))
        Me.pricesLoadedFor = New Dictionary(Of clsPriceBand, Integer) 'used on sellerchannel - how many prices loaded for each buyer
        Me.listPricesLoadedFor = New Dictionary(Of clsRegion, Integer)

        Me.Campaigns = New Dictionary(Of Integer, clsCampaign)
        Me.i_variantCK = New Dictionary(Of String, clsVariant)

        'moved into Product.variants
        'SKUs = New Dictionary(Of clsProduct, Dictionary(Of clsVariant, String)) ' This channels 'internal' SKU for each product (they sell) - the first dimension contains a compound key of Product.id+Variant.id

    End Sub

    Public Function IsUniversal() As Boolean
        Return Me.Code.EndsWith("U") 'i_variant_distisku.Count = 0 'TODO ML have no idea if this is a measure of if they are univeral, need some input on this one...
    End Function

    ''' <summary>
    ''' Check if a particular scheme is enabled for this account and region
    ''' </summary>
    ''' <param name="scheme">The char denoting the scheme to check (F,A, etc)</param>
    ''' <returns>...</returns>
    ''' <remarks></remarks>
    Public Function SchemeEnabled(scheme As Char) As Boolean
        Select Case scheme
            Case Is = "F"
                If IsUniversal() AndAlso Not SchemeOverride.Split(",").Contains("F") Then Return False Else Return True
        End Select
    End Function

    'Private _attributeDataTable As ConcurrentDictionary(Of clsChannel, DataTable)
    'Private _attributeDataTableAge As ConcurrentDictionary(Of clsChannel, DateTime)
    'Private _dataPathsLoaded As ConcurrentDictionary(Of clsChannel, List(Of String)) = New ConcurrentDictionary(Of clsChannel, List(Of String))()
    'Public DataTableMutexLock As ConcurrentDictionary(Of clsChannel, Mutex) = New ConcurrentDictionary(Of clsChannel, Mutex)()

    'Public Function AttributeDataTable(buyerChannel As clsChannel) As DataTable
    '    If _attributeDataTable Is Nothing Then _attributeDataTable = New ConcurrentDictionary(Of clsChannel, DataTable)()
    '    If _attributeDataTableAge Is Nothing Then _attributeDataTableAge = New ConcurrentDictionary(Of clsChannel, DateTime)()
    '    If DataTableMutexLock Is Nothing Then DataTableMutexLock = New ConcurrentDictionary(Of clsChannel, Mutex)()

    '    If Not _attributeDataTable.ContainsKey(buyerChannel) Then

    '        _attributeDataTable.TryAdd(buyerChannel, New DataTable() With {.Locale = New CultureInfo(If(Region.Culture IsNot Nothing, Region.Culture, "En-gb"))})
    '        _attributeDataTableAge.TryAdd(buyerChannel, DateTime.Now)
    '        DataTableMutexLock.TryAdd(buyerChannel, New Mutex())
    '        'Get or create the lid's root data table

    '        Dim col As DataColumn
    '        col = New DataColumn("ID", GetType(Int32))
    '        _attributeDataTable(buyerChannel).Columns.Add(col)

    '        'Populate it with all id's in descendants
    '        Dim c(0) As Object 'ID column in the data table
    '        Dim dv = _attributeDataTable(buyerChannel).AsDataView()
    '        dv.Sort = "[ID]"

    '    End If
    '    AttributeDataTable = _attributeDataTable(buyerChannel)
    '    If DateDiff(DateInterval.Minute, _attributeDataTableAge(buyerChannel), DateTime.Now) > If(ConfigurationManager.AppSettings("MaxDataTableAge") Is Nothing, 15, ConfigurationManager.AppSettings("MaxDataTableAge")) Then RegenerateTable(buyerChannel)
    'End Function

    'Private Sub RegenerateTable(buyerChannel As clsChannel)
    '    _attributeDataTable.TryRemove(buyerChannel, Nothing)
    '    _attributeDataTableAge.TryRemove(buyerChannel, Nothing)
    '    DataTableMutexLock.TryRemove(buyerChannel, Nothing)
    'End Sub

    'Public Function DataPathLoaded(buyerChannel As clsChannel, path As String) As Boolean
    '    Dim dpl = _dataPathsLoaded.GetOrAdd(buyerChannel, New List(Of String))
    '    If dpl.Contains(path) Then Return True Else dpl.Add(path)
    '    Return False
    'End Function

    ''' <summary>
    ''' Gets whether channel uses BinaryStock 
    ''' </summary>
    ''' <returns>A boolean value.</returns>
    ''' <remarks></remarks>
    Public Function BinaryStock() As Boolean
        If (Me.priceConfig And 16) <> 0 Then Return True
        Return False
    End Function
    Function DecodedPriceConfig()
        If priceConfig And 8 Then Return "Web Service"
        If priceConfig And 4 Then Return "Brice Band/Feed File"
        If priceConfig And 2 Then Return "List Price"
        If priceConfig And 1 Then Return "Show products with no price with '...' (Don't Hide them completely)"
    End Function

    Public ReadOnly Property CompoundDisplayName() As String
        Get
            Return Name & " - " & Me.Code
        End Get
    End Property
End Class 'clsChannel



