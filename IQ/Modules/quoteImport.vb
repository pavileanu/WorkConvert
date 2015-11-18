Imports System.Data
Imports dataAccess

Module quoteImport

    Public Function all(con As SqlClient.SqlConnection, Optional HostID As String = Nothing) As String



        Dim dicSystems As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "system")
        Dim dicAccounts As Dictionary(Of String, clsAccount) = loadDic(con, iq.Accounts, "account")
        Dim DicRegionCurrency As Dictionary(Of String, clsCurrency) = loadDic(con, iq.Currencies, "coCurr")

        'load *all* known quotes 
        'the string key is the ID-Version (a compund key)

        Dim dicOneQUOTE As Dictionary(Of Integer, clsQuote)
        dicOneQUOTE = New Dictionary(Of Integer, clsQuote)
        Dim dicAllQuotes As Dictionary(Of String, clsQuote) = loadDic(con, dicOneQUOTE, "quote")


        'quotes (around 1:40)
        'Makes the basic 'stub' quotes - to which all the quoted sytems, and subsequently options will be attached
        'we import every version (export) of the quotes - but only attach the quoteItems to the last(most up to date) version held in dicquotes

        ' Dim importid As Integer
        ' importid = ' DBExecutesql("INSERT INTO IMPORTS (timestamp) values(getdate())", True)


        'Dans ID:Final quote version/export
        Dim dicLastquotes As Dictionary(Of String, clsQuote) = New Dictionary(Of String, clsQuote)
        Try
            dicLastquotes = loadDic(con, dicAllQuotes, "lastquote")  'NB: there is no 'master list' of quotes on the object model (as it would be very large) - an accounts quotes are loaded dynamically

        Catch ex As Exception

        End Try


        'somwehere around 10 seconds for 10,000 quotes
        quoteImport.quotes(con, dicLastquotes, dicAllQuotes, dicAccounts, DicRegionCurrency, 0, HostID)

        con.Close()
        con.Dispose()
        con = da.OpenDatabase()

        saveDic(con, dicLastquotes, "lastquote") 'these are the final quotes (to which we will attach QuoteItems)
        saveDic(con, dicAllQuotes, "lastquote")

        dicAllQuotes = Nothing 'get rid of this ASAP (as it's very large!) - we still have dicLastQuotes - the final versions

        'Get all the systems on quotes - create those line items first (because they will be the parents of all the options)
        Dim Qsample As Dictionary(Of Integer, clsQuoteItem) = New Dictionary(Of Integer, clsQuoteItem)
        Dim dicQuoteItems As Dictionary(Of String, clsQuoteItem) = loadDic(con, Qsample, "QIsystem") 'the dictionary of Quote system items we've already (previously) imported
        'around 3 seconds
        Dim errorMessage As List(Of String) = New List(Of String)
        QuoteSystemItems(con, dicQuoteItems, dicLastquotes, dicSystems, errorMessage)

        saveDic(con, dicQuoteItems, "QIsystem")
        '  Logit("Imported " & added & " quote system items in " & TimeSince(LastMilestone))

        'Important to empty this
        Dim qiSample As Dictionary(Of Integer, clsQuoteItem) = New Dictionary(Of Integer, clsQuoteItem)
        dicQuoteItems = loadDic(con, qiSample, "QIoption") 'now loadUp the dictionary with previously imported quote options

        'get all the options - hook them up to the system quoteItems
        'around 7 secs
        QuoteOptionItems(con, dicLastquotes, dicQuoteItems, errorMessage)
        'recalculate/sanity check the totals - and generate the headline descriptions

        'around 11 secs
        Import.updateQuoteDescriptionsAndTotals(con, dicLastquotes)

        Return String.Empty

    End Function
    Public Function QuotesByHostID(con As SqlClient.SqlConnection, hostID As String, ByRef errorMessages As List(Of String)) As Boolean

        'Retrieve all the quotes based on HostID
        Dim rdr As SqlClient.SqlDataReader
        Dim anaccount As clsAccount
        Dim dicRegionCurrencies As Dictionary(Of String, clsCurrency) = New Dictionary(Of String, clsCurrency) 'loadDic(con, iq.Currencies, "coCurr")
        Dim dicAccounts As Dictionary(Of String, clsAccount) = New Dictionary(Of String, clsAccount) ' loadDic(con, iq.Accounts, "account")

        Dim dicAllQuotes As Dictionary(Of String, clsQuote) = New Dictionary(Of String, clsQuote)
        'Dim dicLastQuotes As Dictionary(Of String, clsQuote) = New Dictionary(Of String, clsQuote)
        Dim sqlQuery As String = String.Empty
        Dim aquote As clsQuote = Nothing
        Dim QuoteWriteCache As DataTable
        Dim quotesCount As Integer = 0
        Dim con2 As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        con.Close()
        con = da.OpenDatabase()
        da.DBExecutesql(con, "IF EXISTS(SELECT * FROM sys.indexes WHERE object_id = object_id('dbo.quote')and  NAME ='ix_quote') DROP INDEX quote.ix_quote;")

        QuoteWriteCache = da.MakeWriteCacheFor(con, "Quote")

        sqlQuery = "select distinct CountryCode,currency from [iq].dbo.countries"
        rdr = da.DBExecuteReader(con2, sqlQuery)
        While rdr.Read
            If Not IsDBNull(rdr.Item("countrycode")) Then
                If Not IsDBNull(rdr.Item("currency")) Then
                    dicRegionCurrencies.Add(rdr.Item("countrycode"), iq.i_currency_code(rdr.Item("currency")))
                End If
            End If
        End While
        rdr.Close()

        Dim allAccounts As IEnumerable(Of clsAccount) = From a In iq.Accounts.Values Where a.SellerChannel.Code = hostID
        For Each act In allAccounts
            dicAccounts.Add(act.User.Email, act)
        Next
        'clear out string builder

        ' made so that this is the basis of the select statement
        ' see function getQuotesfromerver 
        sqlQuery = getQuotesFromServer(hostID, False)


        rdr = da.DBExecuteReader(con2, sqlQuery)

        Dim oid As Integer
        Dim dicOPG As New Dictionary(Of Integer, Object)
        Dim dicBundle As New Dictionary(Of Integer, Object)
        Dim dicMargin As New Dictionary(Of Integer, Single)
        Dim bootstrap As Boolean = True  'we must INSERT the very first quote (it can't be bulk inserted) as we need *something* to point all quotes fk_quote_id_root at
        ' If dicAllQuotes.Count > 0 Then bootstrap = False

        If da.DBSelectFirst("select count(*) from quote where id=1") = 1 Then bootstrap = False


        Dim qc As Integer = 0 'quote count


        While rdr.Read
            If dicAccounts.ContainsKey(rdr.Item("email")) Then
                anaccount = dicAccounts(rdr.Item("email")) 'buyer

                If anaccount.SellerChannel.Region.Code <> "AA" Then
                    Dim currency As clsCurrency

                    currency = dicRegionCurrencies(anaccount.SellerChannel.Region.Code)

                    If rdr.Item("totalvalue") IsNot DBNull.Value Then
                        'we make a quote for every version/export
                        Dim importquoteID As String = rdr.Item("id")

                        Dim quoteName As nullableString = New nullableString(rdr.Item("listname") & "-IQ1[" & importquoteID & "]")
                        Dim totalRebate As Decimal = CDec(rdr("totalrebate"))

                        '                                                                                                 Version \/
                        aquote = New clsQuote(anaccount, anaccount, Nothing, rdr.Item("qcreated"), rdr.Item("updated"), rdr.Item("exports"), iq.i_state_GroupCode("QT-" & rdr.Item("quotestatus")), _
                                                 New NullablePrice(rdr.Item("totalvalue"), currency, False), currency, 0, 0, 0, importquoteID, quoteName, New nullableString(), totalRebate, bootstrap, QuoteWriteCache, 25)
                        'put check so that just in case key is already dicAllQuotes.
                        '*******************************************************************************************************************
                        If Not dicAllQuotes.ContainsKey(importquoteID) Then ' kept falling over so put this check so could test this need looking into for the cause only 99 records are export why!!!!
                            dicAllQuotes.Add(importquoteID, aquote)
                        Else
                            Stop ' break dont want to run if not importing all quotes.
                        End If
                        '********************************************************************************************************************
                        quotesCount += 1

                        aquote.TEMP_IMPORT_MARGIN = If(rdr.Item("quotemargin") < 1, 1, rdr.Item("quotemargin"))
                        If IsDBNull(rdr.Item("multiplier")) Then
                            aquote.TEMP_IMPORT_MULTIPLIER = 1         'One quote had a null 'systems'
                        Else
                            aquote.TEMP_IMPORT_MULTIPLIER = rdr.Item("multiplier")
                        End If

                        bootstrap = False 'ok - we can bulk insert the remainder

                        'put the latest version of the quote in the dictionary
                        ' Dim id$
                        With aquote
                            oid = rdr.Item("ID")

                            ' .Name = New nullableString(rdr.Item("listname"))
                            If IsDBNull(rdr.Item("qCreated")) Then
                                .Created = rdr.Item("updated")
                            Else
                                .Created = rdr.Item("qCreated")
                            End If
                            .State = iq.i_state_GroupCode("QT-" & rdr.Item("Quotestatus"))
                            .Locked = rdr.Item("locked")
                            .Hidden = rdr.Item("hidden")
                        End With
                        qc += 1
                    End If
                
                End If
            End If

        End While
        rdr.Close()
        If qc > 0 Then
            da.BulkWrite(con, QuoteWriteCache, "Quote")
            QuoteWriteCache = Nothing

            'fix the quote root pointers - (now the quotes have their ID's)
            da.DBExecutesql(con, "Update quote set fk_quote_id_root=quote.id ")

            sqlQuery = "CREATE UNIQUE NONCLUSTERED INDEX [IX_Quote] ON [dbo].[Quote] ([Version] ASC,[FK_Quote_ID_Root] Asc) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"

            da.DBExecutesql(con, sqlQuery)

            'After the bulk insert - and before we can attach quote items (to the LAST) quote version,
            'we need to give every quote (in the dictionary) its' correct ID (so that quoteitems can have a valid fk_quote_id)
            'we do this only for th quotes we import in this batch

            Dim allQuoteKeys() As String = dicAllQuotes.Keys.ToArray()
            'For i As Integer = 0 To allQuoteKeys.Count - 1
            '    allQuoteKeys(i) = "'" & allQuoteKeys(i) & "'"
            'Next

            Dim allQuoteKeysString As String = Join(allQuoteKeys, "','")
            allQuoteKeysString = "('" & allQuoteKeysString & "')"

            Dim sqlQuery2 As String = "Select ID,reference from Quote where reference in " & allQuoteKeysString
            Dim rdr2 As SqlClient.SqlDataReader
            rdr2 = da.DBExecuteReader(con, sqlQuery2)
            Dim quote2 As clsQuote
            While rdr2.Read
                If dicAllQuotes.ContainsKey(rdr2("reference")) Then
                    quote2 = dicAllQuotes(rdr2("reference"))
                    quote2.ID = rdr2("ID")

                End If
            End While


            'Get all the systems on quotes - create those line items first (because they will be the parents of all the options)
            Dim Qsample As Dictionary(Of Integer, clsQuoteItem) = New Dictionary(Of Integer, clsQuoteItem)
            Dim dicQuoteItems As Dictionary(Of String, clsQuoteItem) = New Dictionary(Of String, clsQuoteItem) 'loadDic(con, Qsample, "QIsystem")
            Dim dicSystems As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch) 'loadDic(con, iq.Branches, "system")

            Dim allSystems = From s In iq.Branches.Values Where Not IsNothing(s.Product) AndAlso s.Product.isSystem

            For Each prodSys In allSystems
                If Not dicSystems.ContainsKey(prodSys.Product.sku) Then
                    dicSystems.Add(prodSys.Product.sku, prodSys)
                End If
            Next
            'the dictionary of Quote system items we've already (previously) imported
            'around 3 seconds
            QuoteSystemItems(con, dicQuoteItems, dicAllQuotes, dicSystems, errorMessages, con2)

            '  saveDic(con, dicQuoteItems, "QIsystem")
            '  Logit("Imported " & added & " quote system items in " & TimeSince(LastMilestone))

            'Important to empty this
            Dim qiSample As Dictionary(Of Integer, clsQuoteItem) = New Dictionary(Of Integer, clsQuoteItem)
            ' dicQuoteItems = loadDic(con, qiSample, "QIoption") 'now loadUp the dictionary with previously imported quote options

            'get all the options - hook them up to the system quoteItems
            'around 7 secs
            QuoteOptionItems(con, dicAllQuotes, dicQuoteItems, errorMessages, con2)
            'recalculate/sanity check the totals - and generate the headline descriptions

            'around 11 secs
            Import.updateQuoteDescriptionsAndTotals(con, dicAllQuotes)

        Else
            
            errorMessages.Add("No Quotes to import")

        End If

        Return False
    End Function

    Public Function QuoteSystemItems(Con As SqlClient.SqlConnection, ByRef dicQuoteItems As Dictionary(Of String, clsQuoteItem),
                                     dicQuotes As Dictionary(Of String, clsQuote), dicSystems As Dictionary(Of String, clsBranch), ByRef errorMessage As List(Of String),
                                     Optional Con2 As SqlClient.SqlConnection = Nothing
                                      ) As Integer


        'dicQuoteItems uses listid^mfrpartnum as a unique key
        QuoteSystemItems = 0

        Dim dicNewQuoteItems As Dictionary(Of String, clsQuoteItem) = New Dictionary(Of String, clsQuoteItem)

        Dim sql As String = String.Empty
        Dim rdr As SqlClient.SqlDataReader
        Dim quote As clsQuote = Nothing

        Dim swc As New DataTable 'systems write cache
        swc = da.MakeWriteCacheFor(Con, "quoteItem")

        Dim anItem As clsQuoteItem
        'sql$ = "SELECT ListID,mfrpartnum,qty,savedprice,opttype from " & server$ & "[iq].quote.quotestore where ListID>3000 order by listid,case when rtrim(opttype)='sys' then 1 else 2 end"
        Dim allkeys = dicQuotes.Keys.ToArray
        Dim allQuteIDs As String = String.Empty
        If allkeys(0).Contains("-") Then
            For Each key In allkeys
                allQuteIDs &= Split(key, "-")(0) & ","
            Next
        Else
            allQuteIDs = Join(allkeys, ",")
        End If

        sql = "SELECT ListID,mfrpartnum,qty,savedprice,opttype from [iq].quote.quotestore where ListID in ( " & allQuteIDs & " ) and opttype = 'sys'"
        rdr = da.DBExecuteReader(Con2, sql)

        Dim sysBranch As clsBranch = Nothing 'Each quote (should) have exactly one system - it will become a root level item in the new quote
        Dim systemPath As String = String.Empty 'path to the system unit - in the product tree

        Dim branch As clsBranch = Nothing 'branch used for constructing the additional quote items (options)
        Dim SKU As String = String.Empty

        'need to load the branches (so that grafts are done !) before we can recurse throught eh product tree to find options by SKU

        Dim qs As Integer = 0  'quote systems
        Dim qo As Integer = 0  'quote options

        Dim sysSKU As String = String.Empty
        Dim oi As Integer = 0 'orphaned items

        Dim skipquoteoptions As Boolean = False 'used to skip options where the system is missing

        'If dicQuotes.Count = 0 Then Stop

        quote = Nothing

        Dim ck As String = String.Empty
        Dim mfr As String = String.Empty
        Dim rowCount As Integer = 0
        While rdr.Read
            rowCount += 1
            systemPath = String.Empty
            mfr = Trim(rdr.Item("mfrpartnum"))
            ck = rdr.Item("listid") & "-" & mfr  'compound key (uniquely identifies a quote item)
            If dicQuoteItems.ContainsKey(ck) Then
                'already imported
            Else
                If dicSystems.ContainsKey(mfr) Then
                    sysBranch = dicSystems(mfr) 'a system - the (new) quotes root item
                    sysSKU = mfr 'locate this option part number in the new product catalogue

                    'systemPath$ = "tree." & Trim$(iq.RootBranch.ID)
                    'systemPath$ &= "." & Trim$(sysBranch.Parent.Parent.Parent.ID) 'System type (desktops/notebooks/servers)
                    'systemPath$ &= "." & Trim$(sysBranch.Parent.Parent.ID) 'Family
                    'systemPath$ &= "." & Trim$(sysBranch.Parent.ID) 'supply chain (Smart Buy/Top Value/ regular)
                    'systemPath$ &= "." & Trim$(sysBranch.ID) 'system
                    GetFullPath(sysBranch, systemPath)
                    systemPath = "tree" & systemPath



                    If Not dicQuotes.ContainsKey(rdr.Item("listid")) Then
                        Logit("Quote " & rdr.Item("listid") & " does not exsit.")
                        oi += 1 'orphaned items
                    Else
                        quote = dicQuotes(rdr.Item("listid"))

                        'when creating the quote line items we must multily the quantity by the 'systems' multiplier from the original export

                        Dim skuvariant As clsVariant
                        If sysBranch.Product.i_Variants Is Nothing Then
                            Dim str As String = ""
                            'legacy part (no variant)
                        Else

                            If sysBranch.Product.i_Variants.ContainsKey(quote.BuyerAccount.SellerChannel) Then
                                skuvariant = sysBranch.Product.i_Variants(quote.BuyerAccount.SellerChannel)(0)
                                Dim islist As Boolean = CBool(skuvariant.sellerChannel Is HP)
                                anItem = New clsQuoteItem(quote, sysBranch, skuvariant, systemPath, rdr.Item("qty") * quote.TEMP_IMPORT_MULTIPLIER, _
                                                          New NullablePrice(rdr.Item("savedprice"), quote.Currency, islist), New NullablePrice(quote.Currency), False, Nothing, _
                                                          New nullableString, New nullableString, 0, quote.TEMP_IMPORT_MARGIN, New nullableString, 10, swc)

                                dicQuoteItems.Add(ck, anItem)
                                dicNewQuoteItems.Add(quote.Reference, anItem)
                                quote.RootItem.Children.Add(anItem)
                                QuoteSystemItems += 1
                            Else
                                Dim str As String = ""
                                'legacy part (no variant)
                            End If
                        End If
                    End If
                Else
                    Logit("System " & rdr.Item("mfrpartnum") & " does not exist (so the quote could not be imported)")
                    skipquoteoptions = True
                End If
            End If
        End While

        rdr.Close()
        If QuoteSystemItems > 0 Then
            da.BulkWrite(Con, swc, "QuoteItem")
            swc = Nothing

            Dim quoteIDs() As String = (From q In dicNewQuoteItems.Values Select q.quote.ID).ToArray().Select(Function(x) x.ToString()).ToArray()
            'Stop
            Dim quoteKeys() As String = dicNewQuoteItems.Keys.ToArray()
            Dim allimportedQuoteItems As String = Join(quoteIDs, ",")
            'Read in the ID's onto the QuoteItems we've created thus far (via the bulk write) (because they need to be valid parents)
            sql = "SELECT q.reference,qi.id from quoteitem qi join quote as q on qi.fk_quote_id=q.id WHERE qi.fk_quote_id in (" & allimportedQuoteItems & ")" ' & parentEvent.ID WORK NEEDED HERE
            rdr = da.DBExecuteReader(Con, sql)
            Dim quc As Integer = 0
            Dim errorMessages As List(Of String) = New List(Of String)
            While rdr.Read
                Dim aquote As clsQuote = dicQuotes(rdr.Item("reference"))
                aquote.RootItem.Children(0).ID = rdr.Item("id")
                Dim sysItem As clsQuoteItem = aquote.RootItem.Children(0)
                aquote.addPreinstalledRecursive(sysItem, sysItem.Branch, sysItem.Path, False, errorMessages)
                aquote.Update()
                quc += 1
            End While
            rdr.Close()

            Logit("Stamped " & quc & " IDs onto system quoteitems")
        Else
           
            errorMessage.Add("Failed to add system Items")
        End If

    End Function

    Public Function quotes(con As SqlClient.SqlConnection, dicLastQuotes As Dictionary(Of String, clsQuote), dicAllQuotes As Dictionary(Of String, clsQuote), dicAccounts As Dictionary(Of String, clsAccount), dicRegionCurrencies As Dictionary(Of String, clsCurrency), importID As Integer, hostID As String) As Integer


        Dim rdr As SqlClient.SqlDataReader
        Dim anaccount As clsAccount

        con.Close()
        con = da.OpenDatabase()

        Dim QuoteWriteCache As DataTable
        QuoteWriteCache = da.MakeWriteCacheFor(con, "Quote")

        quotes = 0

        Dim sql As String = String.Empty

        Dim aquote As clsQuote = Nothing

        'NB: margin is not on the quote in the new model.. it's on every item in the quote

        ' sql$ = "dELETE FROM QUOTE"
        ' DBExecutesql(con, sql$)

        ' For Each ACCOUNT In iq.Accounts.Values
        ' ACCOUNT.quotes.Clear()
        ' Next

        'temporarily remove the unique index on FK_quoute_id_root, version  (which stops us creating two versions of the quote with same version number)
        'so we can do the bulk insert

        ' da.DBExecutesql(con, "drop index quote.ix_quote")

        sql = "select CountryCode,currency from " & server & "[iq].dbo.countries"
        rdr = da.DBExecuteReader(con, sql)
        While rdr.Read
            If Not IsDBNull(rdr.Item("countrycode")) Then
                If Not IsDBNull(rdr.Item("currency")) Then
                    dicRegionCurrencies.Add(rdr.Item("countrycode"), iq.i_currency_code(rdr.Item("currency")))
                End If
            End If
        End While
        rdr.Close()

        'REMOVE the TOP 100 !

        'There may be more than one quote (export) with the same ID (they will have different versions)
        ' this could be ported to getQuotesfromServer.
        sql = getQuotesFromServer(hostID, True)
        'If hostID Is Nothing Then
        '    Dim daysOld As Integer = 14
        '    sql = "Select top 100 ID,username,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
        '    sql &= "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex "
        '    sql &= " join (SELECT QuoteID,RANK() OVER (PARTITION bY QuoteID ORDER BY timestamp ) as version,Margin,OPG,Rebate,TotalValue,Systems,Options,timestamp FROM " & server & "iq.quote.vExportsDistinct where timestamp>getdate()-" & daysOld & ") as qc on qc.quoteid=id "
        '    sql &= " join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null"
        'Else
        '    Dim daysOld As Integer = 120
        '    sql = "Select top 100 ID,username,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
        '    sql &= "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex "
        '    sql &= " join (SELECT QuoteID,RANK() OVER (PARTITION bY QuoteID ORDER BY timestamp ) as version,Margin,OPG,Rebate,TotalValue,Systems,Options,timestamp FROM " & server & "iq.quote.vExportsDistinct where timestamp>getdate()-" & daysOld & ") as qc on qc.quoteid=id "
        '    sql &= " join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null and u.ChanID = '" & hostID & "'"
        'End If
        rdr = da.DBExecuteReader(con, sql)

        Dim oid As Integer
        Dim dicOPG As New Dictionary(Of Integer, Object)
        Dim dicBundle As New Dictionary(Of Integer, Object)
        Dim dicMargin As New Dictionary(Of Integer, Single)

        Dim bootstrap As Boolean = True  'we must INSERT the very first quote (it can't be bulk inserted) as we need *something* to point all quotes fk_quote_id_root at
        If dicAllQuotes.Count > 0 Then bootstrap = False

        If da.DBSelectFirst("select count(*) from quote where id=1") = 1 Then bootstrap = False


        Dim qc As Integer = 0 'quote count


        While rdr.Read
            If dicAccounts.ContainsKey(rdr.Item("username")) Then
                anaccount = dicAccounts(rdr.Item("username")) 'buyer

                If anaccount.SellerChannel Is Nothing Then
                    Dim an$
                    an$ = rdr.Item("username")
                    'anevent = New clsEvent(QuotesEvent, "account " & an$ & " has no seller channel", ev_Warning)

                Else
                    If anaccount.SellerChannel.Region.Code <> "AA" Then
                        Dim currency As clsCurrency

                        currency = dicRegionCurrencies(anaccount.SellerChannel.Region.Code)
                        Dim ck As String = rdr.Item("id") & "-" & rdr.Item("exports")
                        If dicAllQuotes.ContainsKey(ck) Then
                            'already imported
                        Else

                            If rdr.Item("totalvalue") IsNot DBNull.Value Then
                                'we make a quote for every version/export
                                '                                                                                             Version \/
                                aquote = New clsQuote(anaccount, anaccount, Nothing, rdr.Item("qcreated"), rdr.Item("updated"), rdr.Item("exports"), iq.i_state_GroupCode("QT-" & rdr.Item("quotestatus")), _
                                                         New NullablePrice(rdr.Item("totalvalue"), currency, False), currency, 0, 0, 0, rdr.Item("ID"), New nullableString(), New nullableString(), CDec(rdr("totalrebate")), bootstrap, QuoteWriteCache, importID)
                                dicAllQuotes.Add(Trim$(rdr.Item("id")) & "-" & rdr.Item("exports"), aquote)
                                quotes += 1

                                aquote.TEMP_IMPORT_MARGIN = rdr.Item("quotemargin")
                                If IsDBNull(rdr.Item("multiplier")) Then
                                    aquote.TEMP_IMPORT_MULTIPLIER = 1         'One quote had a null 'systems'
                                Else
                                    aquote.TEMP_IMPORT_MULTIPLIER = rdr.Item("multiplier")
                                End If

                                bootstrap = False 'ok - we can bulk insert the remainder

                                'put the latest version of the quote in the dictionary
                                Dim id As String = rdr.Item("id")
                                If dicLastQuotes.ContainsKey(id) Then
                                    If aquote.Created > dicLastQuotes(id).Created Then
                                        dicLastQuotes(id) = aquote
                                    End If
                                Else
                                    dicLastQuotes.Add(rdr.Item("ID"), aquote) 'these are Dan's, iQuote1 -  Quote (list ID's) 
                                End If

                                With aquote
                                    oid = rdr.Item("ID")

                                    .Name = New nullableString(rdr.Item("listname"))
                                    If IsDBNull(rdr.Item("qCreated")) Then
                                        .Created = rdr.Item("updated")
                                    Else
                                        .Created = rdr.Item("qCreated")
                                    End If

                                    '.Updated = rdr.Item("Updated")
                                    'margin, VoucherCodes, budlerefs apply (now) to ITEMS not Quotes
                                    '  .margin = rdr.Item("quotemargin")
                                    '  dicOPG.Add(oid, rdr.Item("quoteOPG")) '
                                    '  dicBundle.Add(oid, rdr.Item("bundleRef"))

                                    .State = iq.i_state_GroupCode("QT-" & rdr.Item("Quotestatus"))
                                    .Locked = rdr.Item("locked")
                                    .Hidden = rdr.Item("hidden")

                                    ' .Update() 'IMPORTANT (makes descriptions etc) - but cant do it till we have some items

                                End With
                                qc += 1
                            End If
                        End If
                    End If
                End If

                '            Else
                '    Debug.Print("Skipped orphaned quote for " & rdr.Item("username"))
            End If

        End While
        rdr.Close()

        da.BulkWrite(con, QuoteWriteCache, "Quote")
        QuoteWriteCache = Nothing

        'fix the quote root pointers - (now the quotes have their ID's)
        da.DBExecutesql(con, "Update quote set fk_quote_id_root=quote.id")

        'sql$ = "CREATE UNIQUE NONCLUSTERED INDEX [IX_Quote] ON [dbo].[Quote]"
        'sql$ &= "([Version] ASC,[FK_Quote_ID_Root] Asc) "
        'sql$ &= " WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"

        'da.DBExecutesql(con, sql$)

        'After the bulk insert - and before we can attach quote items (to the LAST) quote version,
        'we need to give every quote (in the dictionary) its' correct ID (so that quoteitems can have a valid fk_quote_id)
        'we do this only for th quotes we import in this batch
        rdr = da.DBExecuteReader(con, "SELECT id,reference FROM quote WHERE fk_import_id=" & importID & ";")
        While rdr.Read
            dicLastQuotes(rdr.Item("reference")).ID = rdr.Item("Id")
        End While
        rdr.Close()




    End Function
    'host is nothing
    ' sqlQuery = "Select top 100 ID,username,email,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
    ' sqlQuery &= "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex"
    ' sqlQuery &= " join (SELECT e1.QuoteID,RANK() OVER (PARTITION bY e1.QuoteID ORDER BY timestamp ) as version,e1.Margin,e1.OPG,e1.Rebate,e1.TotalValue,e1.Systems,e1.Options,e1.timestamp 
    '  FROM " & server & "iq.quote.vExportsDistinct e1 
    '  where e1.timestamp>getdate()-" & daysOld & ") as qc on qc.quoteid=id "
    ' sqlQuery &= " join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null"
    '~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 
    'Host is not nothing
    ' sqlQuery = "Select top 100 ID,username,email,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
    ' sqlQuery &= "quotemargin,quoteopg,quotenotes,qcreated,qc.MostRecently as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex "
    ' sqlQuery &= " join  (SELECT * FROM ( SELECT e1.QuoteID,RANK() OVER (PARTITION bY e1.QuoteID ORDER BY timestamp DESC )"
    ' sqlQuery &= " as MostRecently,e1.Margin,e1.OPG,e1.Rebate,e1.TotalValue,e1.Systems,e1.Options,e1.timestamp FROM " & server & "[iq].quote.vExportsDistinct e1  WHERE e1.timestamp>getdate()-" & daysOld & ")"
    ' sqlQuery &= " compiled  WHERE compiled.MostRecently=1) as qc on qc.quoteid=id join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid"
    ' sqlQuery &= "  where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null and u.userHostID = '" & hostID & "'"
    '~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    ' ALL SQL
    ' sql = "Select top 100 ID,username,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,"
    ' sql &= "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from " & server & "[iq].quote.quoteindex "
    ' sql &= " join (SELECT QuoteID,RANK() OVER (PARTITION bY QuoteID ORDER BY timestamp ) as version,Margin,OPG,Rebate,TotalValue,Systems,Options,timestamp FROM " & server & "iq.quote.vExportsDistinct where timestamp>getdate()-" & daysOld & ") as qc on qc.quoteid=id "
    ' sql &= " join " & server & "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-" & daysOld & " and totalvalue is not null and u.ChanID = '" & hostID & "'"
    '~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    ''' <summary> ' These sql statements where being used in  quotesbyhostid and all.</summary>
    ''' <param name="hostid">A string object that represents the channel ID. can be null.</param>
    ''' <returns>A string object that represents a string.</returns>
    ''' <remarks> 
    ''' </remarks>
    Private Function getQuotesFromServer(hostid As String, all As Boolean) As String
        Dim sqlQuery As New StringBuilder(String.Empty)
        Dim daysOld As Integer = If(String.IsNullOrWhiteSpace(hostid), 14, 120)
        If all Then

        End If
        If String.IsNullOrWhiteSpace(hostid) Then
            sqlQuery.AppendFormat("{0}", "Select top 100 ID,username,email,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,")
            sqlQuery.AppendFormat("{0}{1}{2}", "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from ", server, "[iq].quote.quoteindex")
            sqlQuery.AppendFormat("{0}", " join (SELECT e1.QuoteID,RANK() OVER (PARTITION bY e1.QuoteID ORDER BY timestamp ) as version,e1.Margin,e1.OPG,e1.Rebate,e1.TotalValue,e1.Systems,e1.Options,e1.timestamp")
            sqlQuery.AppendFormat("{0}{1}{2}", " FROM ", server, "iq.quote.vExportsDistinct e1")
            sqlQuery.AppendFormat("{0}{1}{2}", " where e1.timestamp>getdate()-", daysOld, ") as qc on qc.quoteid=id ")
            sqlQuery.AppendFormat("{0}{1}{2}{3}{4}", " join ", server, "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-", daysOld, " and totalvalue is not null")

        Else
            If all Then
                sqlQuery.AppendFormat("{0}", "Select top 100 ID,username,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,")
                sqlQuery.AppendFormat("{0}{1}{2}", "quotemargin,quoteopg,quotenotes,qcreated,qc.version as exports,totalvalue,rebate as totalrebate from ", server, "[iq].quote.quoteindex ")
                sqlQuery.AppendFormat("{0}{1}{2}{3}{4}", " join (SELECT QuoteID,RANK() OVER (PARTITION bY QuoteID ORDER BY timestamp ) as version,Margin,OPG,Rebate,TotalValue,Systems,Options,timestamp FROM ", server, "iq.quote.vExportsDistinct where timestamp>getdate()-", daysOld, ") as qc on qc.quoteid=id ")
                sqlQuery.AppendFormat("{0}{1}{2}{3}{4}{5}{6}", " join ", server, "channelcentral.customers.users u on u.upkid =  quoteindex.upkid where id>3000 and updated>getdate()-", daysOld, " and totalvalue is not null and u.ChanID = '", hostid, "'")
            Else
                sqlQuery.AppendFormat("{0}", "Select top 100 ID,username,email,listname,updated,hidden,bundleref,locked,quotestatus,qc.systems as multiplier,")
                sqlQuery.AppendFormat("{0}{1}{2}", "quotemargin,quoteopg,quotenotes,qcreated,qc.MostRecently as exports,totalvalue,rebate as totalrebate from ", server, "[iq].quote.quoteindex ")
                sqlQuery.AppendFormat("{0}", " join  (SELECT * FROM ( SELECT e1.QuoteID,RANK() OVER (PARTITION bY e1.QuoteID ORDER BY timestamp DESC )")
                sqlQuery.AppendFormat("{0}{1}{2}{3}{4}", " as MostRecently,e1.Margin,e1.OPG,e1.Rebate,e1.TotalValue,e1.Systems,e1.Options,e1.timestamp FROM ", server, "[iq].quote.vExportsDistinct e1  WHERE e1.timestamp>getdate()-", daysOld, ")")
                sqlQuery.AppendFormat("{0}{1}{2}", " compiled  WHERE compiled.MostRecently=1) as qc on qc.quoteid=id join ", server, "channelcentral.customers.users u on u.upkid =  quoteindex.upkid")
                sqlQuery.AppendFormat("{0}{1}{2}{3}{4}", " where id>3000 and updated>getdate()-", daysOld, " and totalvalue is not null and u.userHostID = '", hostid, "'")
            End If
        End If
        Return sqlQuery.ToString
    End Function
    Public Function QuoteOptionItems(con As SqlClient.SqlConnection, dicLastquotes As Dictionary(Of String, clsQuote), ByRef dicQuoteOptions_pi As Dictionary(Of String, clsQuoteItem),
                                     ByRef errorMessages As List(Of String), Optional con2 As SqlClient.SqlConnection = Nothing) As Integer

        'Creates Quote Line items for the all the options (not the systems) on a quote

        QuoteOptionItems = 0

        Dim qo As Integer               ' counter
        Dim owc As DataTable            ' options write cache
        Dim sql As String = String.Empty
        Dim rdr As SqlClient.SqlDataReader
        Dim quote As clsQuote           ' reference to the quote
        Dim newItem As clsQuoteItem     'for constructing the new items
        Dim sysItem As clsQuoteItem     'THE item containing THE system on this quote
        Dim sku As String
        Dim oi As Integer

        owc = da.MakeWriteCacheFor(con, "QuoteItem")


        Dim iqQuoteIds As String = Join((From x In dicLastquotes.Values Select x.Reference).ToArray(), ",")



        'get an ordered list of non system options,ordered by the system they were attached to
        'allows us to cache the tree paths (of the options under its system).. making the import an order of magnitude faster
        sql = "SELECT qs.ListID,qs.mfrpartnum,sysunits.MfrPartNum AS SU,qty,savedprice,optType "
        sql &= "from [iq].quote.quotestore AS qs "
        sql &= "JOIN (SELECT mfrpartnum,listid from [iq].quote.quotestore WHERE optType='sys') AS sysunits ON sysunits.listid = qs.listid "
        sql &= "WHERE  optType <> 'sys' and qs.listID in ( " & iqQuoteIds & ")"
        sql &= "ORDER BY qs.listID,SU,mfrpartnum"

        rdr = da.DBExecuteReader(con2, sql)


        'we will clear this each time the system unit changes
        'and check/add to it for each option - saves a LOT of recursive looking up of child branches 
        '(basically each (option)branch is looked up ONCE under each system - instead of it having to be looked up EVERY time it appears)
        Dim OptionPath As String = String.Empty
        OptionPath = Nothing

        Dim optionBranch As clsBranch = Nothing
        sysItem = Nothing


        Dim nosystem As Integer = 0

        Dim su As String = String.Empty
        Dim startat As String = String.Empty

        Dim ck As String = String.Empty 'compound key


        While rdr.Read

            ck = rdr.Item("listid") & "^" & Trim(rdr.Item("mfrpartnum"))
            If Not dicQuoteOptions_pi.ContainsKey(ck) Then
                If rdr.Item("listid") = 328145 Then

                    Dim str1 As String = ""
                End If

                If Not dicLastquotes.ContainsKey(rdr.Item("listid")) Then
                    'this items (parent) quote no longer exists
                    oi += 1 'orphaned items
                Else
                    'make this item as a child of the root item (system)
                    quote = dicLastquotes(rdr.Item("listid"))
                    If quote.RootItem.Children.Count = 0 Then
                        Logit("Quote " & quote.Reference & " has no system on it ??")
                        nosystem += 1
                    Else
                        sysItem = quote.RootItem.Children(0)  'The first child of the quotes root item - IS the system
                        sku = Trim(rdr.Item("mfrpartnum")) 'locate this option part number in the new product catalogue (under the system)

                        'If quote.RootItem.Children.Count = 0 Then/
                        '    Logit("quote " & rdr.Item("listID") & " didn't appear to have a system on it ?")
                        '    nosystem += 1
                        'Else
                        ' If rdr.Item("SU") <> su Then
                        su = Trim(rdr.Item("su"))
                        startat = sysItem.Path
                        OptionPath = String.Empty
                        Pmark("FindChildBySku")
                        optionBranch = Nothing
                        optionBranch = sysItem.Branch.findChildBySKU2(startat, sku, OptionPath) 'staring at this branch/path - recurse down until you find the sku - returns branch and its address 
                        Pacc("FindChildBySku")
                        'End If

                        If optionBranch Is Nothing Then
                            ' Stop 'the option SKU wasn't found under the system
                            Logit(sku & " is not an option for " & rdr.Item("su"))
                        Else

                            'If Not systemItem Is dicquotes(rdr.Item("listid")) Then Stop
                            Dim listprice As NullablePrice = New NullablePrice(quote.QuotedPrice.currency)
                            Dim price As NullablePrice
                            If rdr.Item("savedPrice") Is DBNull.Value Then
                                price = New NullablePrice(rdr.Item("savedPrice"), quote.Currency, False)
                            Else
                                price = New NullablePrice(rdr.Item("savedPrice"), quote.Currency, False)
                            End If


                            'If sysItem Is Nothing Then Stop
                            'If sysItem.ID = -1 Then Stop

                            'when creating the quote line items we must multiply the quantity by the 'systems' multiplier from the original export

                            'NB:- We do NOT multiplty the option quantities by * quote.TEMP_IMPORT_MULTIPLIER - becuase the system (quoteitem) has been (multiplied already)

                            Dim opg As nullableString = New nullableString
                            Dim bundle As nullableString = New nullableString

                            If optionBranch.Product.i_Variants Is Nothing Then
                                Dim strtes As String = ""
                                'legacy option (no variant)
                            Else
                                If Not optionBranch.Product.i_Variants.ContainsKey(quote.BuyerAccount.SellerChannel) Then
                                    'legacy option (no variant)
                                Else
                                    Dim SKUvariant As clsVariant = optionBranch.Product.i_Variants(quote.BuyerAccount.SellerChannel)(0)
                                    newItem = New clsQuoteItem(quote, optionBranch, SKUvariant, OptionPath, rdr.Item("qty"), price, listprice, False, sysItem, opg, bundle, 0, quote.TEMP_IMPORT_MARGIN, New nullableString, 10, owc)
                                    QuoteOptionItems += 1 'how many we've added (function return value)
                                End If
                            End If
                            'sysItem.Children.Add(anItem) - DONT need to do this (specifying sysitem as the parent does it)
                        End If
                        qo += 1 ' quote options (count)
                        ' End If
                    End If
                End If
            End If

        End While
        rdr.Close()

        If QuoteOptionItems > 0 Then
            Dim rows As Integer = owc.Rows.Count
            da.BulkWrite(con, owc, "QuoteItem")
            owc = Nothing
        Else
            errorMessages.Add("Failed to add any options")
        End If

        'QOIEvent.update(qo & "option quote items")

    End Function




    Private Sub GetFullPath(sysBranch As clsBranch, ByRef fullpath As String)
        fullpath = "." & Trim(sysBranch.ID) & fullpath

        If Not IsNothing(sysBranch.Parent) Then
            GetFullPath(sysBranch.Parent, fullpath)
        End If


    End Sub



End Module
