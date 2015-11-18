Option Explicit On
Option Strict On

Imports dataAccess
Imports System.Linq
Imports System.Xml
Imports System.Xml.Linq
Imports System.Text
Imports System.Globalization


Public Class clsQuote
    Implements ISqlBasedObject

    'Thoughts on SBSO
    'The root quote could be HP's version at list price
    'Each disti then 'picks it up' - makes their own version (updating pricing) - which contains *their* (customer specific) pricing - this copy get marked lost/won etc

    Public ID As Integer
    Public BuyerAccount As clsAccount
    Public AgentAccount As clsAccount
    Public Name As nullableString
    Public Notes As nullableString
    Public Created As DateTime
    Public Updated As DateTime
    Public Hidden As Boolean
    Public Locked As Boolean
    Public Saved As Boolean
    Public State As clsState
    Public RootItem As clsQuoteItem  'The virtual root item in this quote (has no branch.. but has children)
    Public RootQuote As clsQuote 'Integer 'This is the 'original' quote (this quote is a version of) - starts out as itself
    Public Version As Integer
    Public Currency As clsCurrency
    Public Reference As String 'Used during the import .. possiby useful to expose as a customers reference in future

    Public Description As nullableString
    'Public NumOptions As Integer
    Public NumAlternatives As Integer
    Public keyword As String

    Public TEMP_IMPORT_MARGIN As Single ' don't use  !! margins are per QuoteItem now
    Public TEMP_IMPORT_MULTIPLIER As Integer 'The 'sytems' (multiplier) when the quote was exported .. we need to multiple all QuoteItemQuantites by this
    Public QuotedPrice As NullablePrice 'Total value of the WITH Margin - also hold a 'valid' member to say wether the quote includes any POA items, and the CURRENCY of the quote
    Public TotalRebate As Decimal 'Single

    Public MostRecent As clsQuoteItem 'the last item flexed (up) or added - used to set a cssClass on the div, used in trun for the zoomer animation

    Public maxVersion As Integer 'stored on the root quote - (primarily so we know if there's more than one quote and wether to display the 'show all versions' button in the list of quotes)
    Public Cursor As clsQuoteItem 'the thing (system) into which we're adding things (options)


    Private FK_Import_Id As Integer

    'Public Editable As Boolean = False

    Public AcknowledgedValidations As List(Of String) = New List(Of String)()


    Public Shared Function CurrentQuoteContains(lid As UInt64, product As clsProduct) As Boolean

        CurrentQuoteContains = False

        If iq.SeshContains(lid, "QuoteID") AndAlso iq.sesh(lid, "QuoteID") IsNot Nothing Then
            Dim qid As Integer
            qid = CInt(iq.sesh(lid, "QuoteID"))  'use the session variable
            If qid <> 0 Then
                Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
                Dim quote As clsQuote = agentAccount.Quotes(qid)
                If quote.RootItem.HasProduct(product) Then Return True
            End If
        End If


    End Function



    Public Sub delete()

        Me.BuyerAccount.Quotes.Remove(Me.ID)  'we dont have to remove the quote items - becuase removing the only refernece to the quote (here) - destroys all items

        Dim sql$
        sql$ = "DELETE FROM [QuoteItem] WHERE fk_quote_id=" & Me.ID
        da.DBExecutesql(sql$, False)


        sql$ = "DELETE FROM [Quote] WHERE ID=" & Me.ID
        da.DBExecutesql(sql$, False)


    End Sub

    Public Sub ExportLogging(type As String)

        Dim sql$
        sql$ = "INSERT INTO [dbo].[QuoteExport] ([FK_Quote_ID],[Type])  VALUES(" & Me.ID & ",'" & type & "')"
        da.DBExecutesql(sql$, False)

    End Sub


    Public Function SetQtyByItemID(itemID As Integer, qty As Integer, Absolute As Boolean, MarginForNew As Single, ByRef ErrorMessages As List(Of String)) As clsQuoteItem

        'can probably be largely consolidated with SetQtyByPath.. but no time right now

        Dim Item As clsQuoteItem = Nothing
        Me.RootItem.clearMessage() 'Clear all validation messages (recursively)

        Item = Me.RootItem.FindRecursive(itemID)
        If Item Is Nothing Then
            ErrorMessages.Add("Could not find quote item " & itemID & " in " & Me.ID)
        Else
            If Item.IsPreInstalled Then
                'we tried to flex a preinstalled item - you can't enter absolute values against presinstalled items in the quote


                'New - Handles -L21 > - B21 FIO's
                '  Dim altsku As String = Item.Branch.Product.FirstAttributeEnglishText("altsku")

                'if this preinstalled item has an altSKU - add one of those instead !
                'If altsku <> "" Then
                '    Dim atpath As String = Item.Parent.Path
                '    Dim altskubranch = Item.Parent.Branch.findChildBySKU2(Item.Parent.Path & "", altsku, atpath)
                '    Dim price As clsPrice = altskubranch.Product.GetPrices(BuyerAccount, BuyerAccount.SellerChannel.priceConfig, iq.AllVariants, ErrorMessages, True).First
                '    Return setQtyByPath(atpath, price.SKUVariant, 1, False, 1, ErrorMessages)
                'End If

                'see if there's an existing - non-preInstalled sibling
                Dim existing As clsQuoteItem
                existing = Me.Cursor.FindRecursive(Item.Path, False)


            If Item.validate Then
                If qty < 0 Then
                    Item.validate = False
                Else
                    If existing Is Nothing Then

                        Dim price As NullablePrice
                        Dim listprice As NullablePrice
                        price = Item.SKUVariant.Price(BuyerAccount) '.Price

                        listprice = NullPrice(Item.Branch.Product.ListPrice(BuyerAccount), BuyerAccount.Currency)

                        'this is a fix for universal/Synnex demo - flexing basket itmes worked on the assumption the variant would have a price already - but  with Universal it's not necessarily so !
                        'it may be dangerous - (where prices are 'vivalid' fomr some other reason)
                        If Not price.isValid Then price = listprice

                        'give these an order
                        Dim order As Integer
                        If Item.Branch.Product.isSystem Then
                            order = 2
                        Else
                            order = 4
                        End If

                        Item = New clsQuoteItem(Me, Item.Branch, Item.SKUVariant, Item.Path, 1, price, listprice, False, Item.Parent, New nullableString, New nullableString, 0, MarginForNew, New nullableString, order) 'Item.order)
                    Else
                        existing.Quantity += qty
                    End If
                End If
            Else
                If qty > 0 Then
                    Item.validate = True 'place a preinstalled item back into validation
                Else
                    ErrorMessages.Add("* preinstalled unvalidated item qty<0")
                End If
            End If
            Else
            If Absolute Then
                Item.Quantity = qty
            Else
                Dim quan = Item.Branch.Quantities.Where(Function(q) q.Value.Region.Encompasses(Me.BuyerAccount.BuyerChannel.Region)).FirstOrDefault
                If quan.Value IsNot Nothing AndAlso quan.Value.MinIncrement <> 0 Then
                    Item.Quantity += (quan.Value.MinIncrement * qty)
                Else
                    Item.Quantity += qty
                End If
            End If

            If Item.Quantity = 0 Then
                Item.Parent.Children.Remove(Item)

                'fix for removing items and cursor focus
                If Item Is Me.Cursor Then
                    If Item.Parent.Children.Count > 0 Then
                        Me.Cursor = Item.Parent.Children.First
                        Me.MostRecent = Item.Parent.Children.First
                    Else
                        Me.Cursor = Nothing
                    End If
                End If
            End If

            Item.Update()
            If Item.Quantity > 0 Then
                Me.MostRecent = Item
            End If
        End If
        End If

        Return Item


    End Function

    Public Function setQtyByPath(path$, SKUvariant As clsVariant, qty As Integer, Absolute As Boolean, margin As Single, ByRef errormessages As List(Of String)) As clsQuoteItem

        Me.RootItem.clearMessage() 'Clear all validation messages (recursively)
        Dim Price As NullablePrice ' = branch.Product.VariantPrice(BuyerAccount, SKUvariant)
        'this gets the list price for the a first variant

        Dim branch As clsBranch = iq.Branches(CInt(Split(path$, ".").Last))

        'NB:- List Price can return NOTHING
        'Dim lp As clsPrice = branch.Product.ListPrice(BuyerAccount) 'branch.Product.listPrices(BuyerAccount.Currency)(0).Price

        'fetch the list price if there is one - or constuct a POA in the correct currency
        Dim listprice As NullablePrice = NullPrice(branch.Product.ListPrice(BuyerAccount), BuyerAccount.Currency)

        Dim item As clsQuoteItem = Nothing

        'Important - we flex quantities of exisiting OPTIONS under the currenct systen) BUT we add new SYSTEMS to the basket (when flexing by path)'
        'this means that the add buttons in the tree will add *instances* of systems (that can be configured individually)

        Dim addingOption As Boolean = (branch.Product.isSystem(path) = False)
        If Me.Cursor IsNot Nothing And addingOption Then
            item = Me.Cursor.FindRecursive(path$, True) 'see if the Current (cursored) quote item (system) already has (as a descendant) 
            '                                              one of the items (by path) we're trying to set the quantity of...
        End If

        Dim prices As List(Of clsPrice)
        prices = branch.Product.GetPrices(BuyerAccount, BuyerAccount.SellerChannel.priceConfig, SKUvariant, errormessages, True)

        Dim blnAddSystem As Boolean = True

        If Not addingOption And Me.RootItem.Children.Count > 0 Then
            Dim firstSystemBranch As clsBranch = Me.RootItem.Children(0).Branch
            If branch.Product.Manufacturer <> firstSystemBranch.Product.Manufacturer Then
                blnAddSystem = False
            End If

        End If


        If blnAddSystem Then

            If prices.Count = 0 OrElse prices(0) Is Nothing Then
                errormessages.Add("No Price available")
            Else
                Price = prices(0).Price

                If Price.NumericValue > listprice.NumericValue Then errormessages.Add("* ! Customer price exceeds list price for " & SKUvariant.Product.sku & "(" & SKUvariant.DistiSku & ")")

                If Price.NumericValue = 0 And Price.isValid = True Then errormessages.Add("* Price was valid but  0")

                If item Is Nothing Then
                    'we didn't have one (under the cursor) in the quote - so we add one

                    'this is a fix for universal/Synnex demo - flexing basket itmes worked on the assumption the variant would have a price already - but  with Universal it's not necessarily so !
                    'it may be dangerous - (where prices are 'invalid' fomr some other reason)

                    'REMOVED by nick - listprice could be nothing (and should already have been returned if it existed)
                    'If Not Price.isValid Then Price = listprice
                    ''''''


                    If Me.Cursor Is Nothing Then
                        item = Me.Additem(branch, SKUvariant, path$, qty, Price, listprice, Me.RootItem, margin, True, errormessages)
                    Else
                        'is the thing we're adding/flexing compatible with (appears as a option under) the thing  at the quote cursor
                        If Cursor.Compatible(path$) Then

                            If Me.Cursor.ID = -99 Then Me.Cursor = Me.RootItem 'part of the fix for bug 712 (may be redundant - but be carefull - harmless !!)
                            item = Me.Additem(branch, SKUvariant, path$, qty, Price, listprice, Me.Cursor, margin, True, errormessages)
                        Else
                            item = Me.Additem(branch, SKUvariant, path$, qty, Price, listprice, Me.RootItem, margin, True, errormessages)
                            item.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.amberalert, iq.AddTranslation("Not compatible with the currently selected system", English, "VM", 0, Nothing, 0, False), Nothing, "", 0, 0, Split("")))
                        End If
                    End If

                Else
                    'yes, we had one
                    If item.IsPreInstalled Then
                        'we've attempted to flex a preinstalled option 
                        'see if we have a 'non-preinstalled' one too already

                        Dim npItem As clsQuoteItem
                        npItem = Me.Cursor.FindRecursive(path$, False)
                        Dim toadd As Integer
                        If Absolute Then toadd = qty - item.Quantity Else toadd = qty 'for absolute.. make up to the desired quantity

                        If toadd < 0 Then 'flexing a preinstalled item down (remove from validation)
                            If item.validate Then
                                item.validate = False
                            Else
                                errormessages.Add("* Tried to flex down an item removed from validation")
                            End If
                        ElseIf toadd > 0 Then 'flexing up a preinstalled item
                            If item.validate Then  'this preinstalled item included in vaidation 
                                If npItem Is Nothing Then
                                    'no - make the non-preinstalled version (including *it's* preinstalled options                    
                                    npItem = Me.Additem(branch, SKUvariant, path$, toadd, Price, listprice, Me.Cursor, margin, True, errormessages)
                                Else
                                    'yes - flex the non-preinstalled version
                                    npItem.Quantity += toadd
                                End If
                            Else
                                item.validate = True 'bring a pre-installed item back into validation
                            End If
                        Else
                            errormessages.Add("* toadd was 0 in setQtyByPath")
                        End If
                    Else
                        If Absolute Then item.Quantity = qty Else item.Quantity += qty

                        If item.Quantity = 0 Then
                            item.Parent.Children.Remove(item)
                        End If
                    End If


                End If
                If item.Branch.Product.isSystem(path) Then
                    Me.MostRecent = item
                    Me.Cursor = item
                End If
            End If
            If item IsNot Nothing Then
                item.Update()
            End If
        End If

        Return item

    End Function


    Private Function FilledXMLHeader(doc As XmlDocument, ByRef errormessages As List(Of String)) As XmlNode

        'Returns a populated header section for the XML quote export 

        Dim h As XmlNode
        h = doc.CreateElement("Header")

        Dim dateNode As XmlNode = doc.CreateElement("Date")
        dateNode.InnerText = Now.ToString("dd MMM yyyy")
        h.AppendChild(dateNode)

        Dim Qid As XmlNode = doc.CreateElement("QuoteID")
        Qid.InnerText = Me.RootQuote.ID.ToString
        h.AppendChild(Qid)

        Dim QuoteName As XmlNode
        QuoteName = doc.CreateElement("QuoteName")
        If Me.Name.value.ToString().Trim().Length = 0 Then
            QuoteName.InnerText = Me.RootQuote.ID.ToString
        Else
            QuoteName.InnerText = Me.Name.sqlValue
        End If

        h.AppendChild(QuoteName)

        Dim Version As XmlNode = doc.CreateElement("Version")
        Version.InnerText = Me.Version.ToString
        h.AppendChild(Version)

        Dim currencyCode As XmlNode
        currencyCode = h.AppendChild(doc.CreateElement("CurrencyCode"))
        currencyCode.InnerText = Me.BuyerAccount.Currency.Code

        Dim currencySymbol As XmlNode
        currencySymbol = h.AppendChild(doc.CreateElement("CurrencySymbol"))
        currencySymbol.InnerText = Me.BuyerAccount.Currency.Symbol


        Dim Total As XmlNode = doc.CreateElement("Total")
        'Total.InnerText = Me.Currency.Symbol & Me.TotalPrice.sqlvalue 'this is the sum of all prices BEFORE margin

        Dim tpim As NullablePrice = New NullablePrice(Me.Currency) 'Total Price Including Margin

        Me.RootItem.Totalise(tpim, TotalRebate, True) 'fetch the total price INCLUDING margin
        Dim listPriceIndicator As String = String.Empty
        If (tpim.isList) Then
            listPriceIndicator = " *"
        End If
        Total.InnerText = CDec(tpim.sqlvalue).ToString(CultureInfo.InvariantCulture) 'tpim.text(Me.BuyerAccount, errormessages)

        h.AppendChild(Total)

        Dim TR As XmlNode = doc.CreateElement("TotalRebate")
        TR.InnerText = Me.TotalRebate.ToString(CultureInfo.InvariantCulture)
        h.AppendChild(TR)

        Dim GT As XmlNode = doc.CreateElement("QuoteTotal")
        'GT.InnerText = Me.Currency.Symbol & Me.TotalPrice.sqlvalue - Me.TotalRebate
        GT.InnerText = (CDec(tpim.sqlvalue) - Me.TotalRebate).ToString(CultureInfo.InvariantCulture)
        h.AppendChild(GT)

        Dim buyer As XmlNode

        buyer = h.AppendChild(doc.CreateElement("Buyer"))

        buyer.AppendChild(doc.CreateElement("BuyerCompanyName")).InnerText = String.Empty ' Me.BuyerAccount.BuyerChannel.Name
        buyer.AppendChild(doc.CreateElement("BuyerCompanyID")).InnerText = String.Empty 'Me.BuyerAccount.BuyerChannel.ID.ToString
        buyer.AppendChild(doc.CreateElement("BuyerPersonName")).InnerText = String.Empty 'Me.BuyerAccount.User.RealName
        buyer.AppendChild(doc.CreateElement("BuyerPersonEmail")).InnerText = String.Empty ' Me.BuyerAccount.User.Email
        buyer.AppendChild(doc.CreateElement("BuyerPersonTelephone")).InnerText = String.Empty ' If(Me.BuyerAccount.User.tel1.DisplayValue = "IQ.nullableString", "", Me.BuyerAccount.User.tel1.DisplayValue)


        Dim Seller As XmlNode
        Seller = h.AppendChild(doc.CreateElement("Seller"))
        Seller.AppendChild(doc.CreateElement("SellerPersonName")).InnerText = String.Format("{0}, {1} - {2}", Me.AgentAccount.User.RealName, Me.AgentAccount.User.Channel.Name, Me.AgentAccount.User.Email) ''String.Empty ' Me.AgentAccount.User.RealName
        Seller.AppendChild(doc.CreateElement("SellerCompanyName")).InnerText = String.Empty 'String.Format("{0}", Me.AgentAccount.User.RealName)
        Seller.AppendChild(doc.CreateElement("SellerCompanyID")).InnerText = Me.BuyerAccount.SellerChannel.ID.ToString

        Seller.AppendChild(doc.CreateElement("SellerLogo")).InnerText = CStr(Me.BuyerAccount.SellerChannel.pic1.value)  'contains only the subpath and filename (for example /dist/LOGO_DABPL03228.jpg)

        Seller.AppendChild(doc.CreateElement("SellerLogoShort")).InnerText = Filename(CStr(Me.BuyerAccount.SellerChannel.pic2.value)) 'Just the filename parsed out... so we can get it from the media folder

        ''Seller.AppendChild(doc.CreateElement("SellerPersonName")).InnerText = String.Empty ' Me.AgentAccount.User.RealName
        Seller.AppendChild(doc.CreateElement("SellerPersonEmail")).InnerText = String.Empty 'Me.AgentAccount.User.Email
        Seller.AppendChild(doc.CreateElement("SellerPersonTelephone")).InnerText = String.Empty 'If(Me.AgentAccount.User.tel1.DisplayValue = "IQ.nullableString", "", Me.AgentAccount.User.tel1.DisplayValue)



        Dim Language As XmlNode
        Language = h.AppendChild(doc.CreateElement("Language"))
        Language.InnerText = Me.AgentAccount.Language.Code

        Return h

    End Function

    Private Function FilledXMLSmartQuoteHeader(doc As XmlDocument, quoteItem As clsQuoteItem, ByRef errormessages As List(Of String)) As XmlNode

        'Returns a populated header section for the XML SmartQuote export 

        Dim enLanguage As clsLanguage = (From l In iq.Languages.Values Where l.Code = "EN").First
        Dim supplyChainCode As String = String.Empty

        For Each productAttribute In quoteItem.Branch.Product.Attributes.Values
            If (productAttribute.Translation IsNot Nothing) AndAlso (productAttribute.Translation.Group IsNot Nothing) AndAlso (productAttribute.Translation.Group = "SCC") Then
                supplyChainCode = productAttribute.Translation.text(enLanguage)
                Exit For
            End If
        Next

        Dim h As XmlNode
        h = doc.CreateElement("EclipseHeader")

        Dim attr As XmlAttribute = doc.CreateAttribute("ConfigName")
        If Me.Name.v IsNot Nothing Then
            attr.Value = xmlEncode(Me.Name.DisplayValue())
        Else
            attr.Value = String.Empty
        End If
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("NetPrice")
        attr.Value = "0"
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("SKU")
        attr.Value = "TBD"
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("ConfigID")
        attr.Value = Me.ID.ToString()
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("SupplyChain")
        attr.Value = supplyChainCode
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("OrigApplication")
        attr.Value = "IQUOTE"
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("Country")
        attr.Value = Me.BuyerAccount.SellerChannel.Region.Code
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("PriceTerm")
        attr.Value = "DP"
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("CurrencyCode")
        attr.Value = Me.BuyerAccount.Currency.Code_HP
        h.Attributes.Append(attr)

        attr = doc.CreateAttribute("OpportunityID")
        attr.Value = String.Empty
        h.Attributes.Append(attr)

        Return h

    End Function


    Public Function HtmlSummary(language As clsLanguage, OnlyChanged As Boolean, lid As UInt64, ByRef priceChanges As Boolean, ByRef errorMessages As List(Of String)) As PlaceHolder

        'returns a summary table of the quote - if onlychanged is passed at true - only quote lines whos prices have changed (stock is now at a different price) will be returned.

        HtmlSummary = New PlaceHolder

        Dim fl As New clsFlatList
        Dim tbl As New Table
        tbl.CssClass = "quotePreview"

        Dim toget As List(Of clsVariant) = New List(Of clsVariant)
        Me.getPrice(toget, lid, errorMessages)

        'flatten the quote in summary form (without preinstalled options)
        '    For Each Li In Me.RootItem.Flattened(Consolidated, includePreinstalled, -1).items  'Flattened() returns a flat list of clsFlatListItems
        Dim priceChanged As Boolean = False

        'If priceChanged Then

        'End If



        tbl.Rows.Add(summaryHeaders(language))
        Dim linePriceChanged As Boolean
        Dim tr As TableRow
        For Each flatListItem In Me.RootItem.Flattened(False, False, 0).items
            linePriceChanged = False
            tr = flatListItem.HTMLTableRow(language, linePriceChanged, errorMessages)
            If (OnlyChanged And linePriceChanged) Or (Not OnlyChanged) Then
                tbl.Rows.Add(tr)
            End If
            If linePriceChanged Then priceChanges = True
        Next


        HtmlSummary.Controls.Add(tbl)

        'If we asked for only changed rows - and only have the one (tableheaders) row... return nothing
        If OnlyChanged And tbl.Rows.Count = 1 Then
            Return Nothing
        End If

    End Function

    Public Function SellerLogo() As String

        Return Me.BuyerAccount.SellerChannel.pic2.value.ToString

    End Function

    Public Function summaryHeaders(language As clsLanguage) As TableHeaderRow

        Dim thc As TableHeaderCell
        Dim thr As New TableHeaderRow


        'SKU
        thc = New TableHeaderCell
        thc.Text = Xlt("Mfr Part No", language)
        thr.Cells.Add(thc)

        'Variant
        thc = New TableHeaderCell
        thc.Text = Xlt("Variant", language)
        thr.Cells.Add(thc)

        'Opttype (no label required)
        thc = New TableHeaderCell
        thr.Cells.Add(thc)


        'DESCRIPTION
        thc = New TableHeaderCell
        thr.Cells.Add(thc)
        thc.Text = Xlt("Description", language)

        'LISTPRICE
        thc = New TableHeaderCell
        thc.Text = Xlt("List Price", language)
        thr.Cells.Add(thc)


        'UNITPRICE
        thc = New TableHeaderCell
        thc.Text = Xlt("Unit Price", language)
        thr.Cells.Add(thc)


        'PRICE CHANGE
        thc = New TableHeaderCell
        thc.Text = Xlt("Price Change", language)
        thr.Cells.Add(thc)


        'QUANTITY
        thc = New TableHeaderCell
        thc.Text = Xlt("Quantity", language)
        thr.Cells.Add(thc)


        'LINEPRICE
        thc = New TableHeaderCell
        thc.Text = Xlt("Line Price", language)
        thr.Cells.Add(thc)


        'STOCK
        thc = New TableHeaderCell
        thc.Text = Xlt("Stock", language)
        thr.Cells.Add(thc)

        Return thr


    End Function

    Public Function XMLDoc(ByRef errorMessages As List(Of String), Optional ByRef opgs As List(Of String) = Nothing) As XmlDocument

        Dim dec As XmlDeclaration
        Dim DocRoot As XmlElement
        Dim header As XmlNode
        Dim Body As XmlNode
        Dim Footer As XmlNode

        XMLDoc = New XmlDocument
        dec = XMLDoc.CreateXmlDeclaration("1.0", Nothing, Nothing)
        XMLDoc.AppendChild(dec)
        DocRoot = XMLDoc.CreateElement("Quote")
        XMLDoc.AppendChild(DocRoot)

        header = Me.FilledXMLHeader(XMLDoc, errorMessages)
        DocRoot.AppendChild(header)

        Body = DocRoot.AppendChild(XMLDoc.CreateElement("Body"))
        Dim productSkus As New List(Of String)

        'Flat - Bill Of materiales  (consolidated) version - doesn't include FIO's 
        Body.AppendChild(Me.XMLFlatList("FlatQuote", XMLDoc, True, False, errorMessages, productSkus, opgs))
        productSkus.Clear()
        'Tree version can have many rows with the same part number (ie. the same otion installed in several systems) and displays full structure of the quote
        Body.AppendChild(Me.XMLFlatList("TreeQuote", XMLDoc, False, True, errorMessages, productSkus, opgs, True))

        ' Body.AppendChild(Me.RootItem.XMLTreeList(Doc))
        Footer = DocRoot.AppendChild(XMLDoc.CreateElement("Footer"))

        Dim note As XmlNode = XMLDoc.CreateElement("Note")
        Footer.AppendChild(note)
        If Not Me.Notes Is Nothing Then
            note.InnerText = Me.Notes.DisplayValue
        End If

        Dim advice As XmlNode = XMLDoc.CreateElement("Advice")


    End Function

    Public Function XMLFlatList(NodeName As String, doc As XmlDocument, Consolidate As Boolean, includePreinstalled As Boolean, ByRef errorMessages As List(Of String), ByRef productSkus As List(Of String), Optional ByRef opgs As List(Of String) = Nothing, Optional quote As Boolean = False) As XmlNode

        'Returns an XML Node - cotaining all quote lines
        'If 'consolidate' is set to true, All quote lines of the same SKU (and variant) are consolidated into one line (with a larger quantity)
        'If consolidate is false, A node st added for every quote line, with its quantity - and additional Nodes for ID and ParentID (which allow the actual herirachy to be reconstructed from the XML - should that ever prove necessary

        XMLFlatList = doc.CreateElement(NodeName)
        If quote Then
            For Each Li In Me.RootItem.Flattened(Consolidate, includePreinstalled, -1, quote).items  'Flattened() returns a flat list of clsFlatListItems
                XMLFlatList.AppendChild(Li.XMLLine(doc, Me, Not Consolidate, errorMessages, productSkus, opgs))
            Next
        Else
            For Each Li In Me.RootItem.Flattened(Consolidate, includePreinstalled, -1).items  'Flattened() returns a flat list of clsFlatListItems
                XMLFlatList.AppendChild(Li.XMLLine(doc, Me, Not Consolidate, errorMessages, productSkus, opgs))
            Next
        End If


    End Function

    Public Function XMLDocSmartQuote(ByRef errorMessages As List(Of String)) As XmlDocument

        Dim xmlDeclaration As XmlDeclaration
        Dim root As XmlElement
        Dim eclipseHeader As XmlNode
        Dim eclipseLineItems As XmlNode

        XMLDocSmartQuote = New XmlDocument
        xmlDeclaration = XMLDocSmartQuote.CreateXmlDeclaration("1.0", "utf-8", Nothing)
        XMLDocSmartQuote.AppendChild(xmlDeclaration)

        root = XMLDocSmartQuote.CreateElement("EclipseHeaders")
        XMLDocSmartQuote.AppendChild(root)

        For Each quoteItem In RootItem.Children

            ' Create an EclipseHeader for each system in the quote
            If quoteItem.Branch.Product IsNot Nothing Then

                If quoteItem.Branch.Product.isSystem(quoteItem.Path) Then

                    eclipseHeader = FilledXMLSmartQuoteHeader(XMLDocSmartQuote, quoteItem, errorMessages)
                    root.AppendChild(eclipseHeader)

                    eclipseLineItems = eclipseHeader.AppendChild(XMLDocSmartQuote.CreateElement("EclipseLineItems"))

                    For Each flatListItem As clsFlatListItem In quoteItem.Flattened(True, False, -1).items
                        eclipseLineItems.AppendChild(flatListItem.XMLSmartQuoteLine(XMLDocSmartQuote, Me, errorMessages))
                    Next

                End If
            End If
        Next

    End Function

    Public Function CreateNextVersion(errorMessages As List(Of String)) As clsQuote
        Return CreateNextVersion(Nothing, 0, errorMessages)
    End Function

    Public Function CreateNextVersion(flexUpItem As Int32?, flexUpQuantity As Int32, ByRef errormessages As List(Of String)) As clsQuote

        'Return New clsquote(Me) 'calls the special constructor which bases one (cloned) quote upon another - incrementing the version - and updating the pricing (to what is current for the quote buyeraccount)


        'NB it's the AGENT that holds the quotes (not the buyer)
        Dim nextVersion As Integer = Me.AgentAccount.MaxQuoteVersion(Me.RootQuote) + 1
        If nextVersion = 1 Then Stop 'quote version must have been zero (ie - it wasnt' found under the buyeraccount)

        Dim newQuote As clsQuote = New clsQuote(Me.BuyerAccount, Me.AgentAccount, Me.RootQuote, Now, Now, nextVersion, iq.i_state_GroupCode("QT-#NW"), Me.QuotedPrice, Me.Currency, False, Me.Hidden, True, Me.Reference, Me.Name, Me.Description, Me.TotalRebate)

        DeepCopy(newQuote, flexUpItem, flexUpQuantity, errormessages)

        Return newQuote
    End Function

    Public Function Copy(flexUpItem As Int32?, flexUpQuantity As Int32, ByRef errormessages As List(Of String)) As clsQuote
        If Me.RootItem.Children.Count = 0 Then LoadItems(errormessages)
        Dim newQuote As clsQuote = New clsQuote(Me.BuyerAccount, Me.AgentAccount, Nothing, Now, Now, Version, iq.i_state_GroupCode("QT-#NW"), Me.QuotedPrice, Me.Currency, False, Me.Hidden, False, String.Empty, New nullableString(), New nullableString(), Me.TotalRebate)
        DeepCopy(newQuote, flexUpItem, flexUpQuantity, errormessages)

        Return newQuote
    End Function

    Public Sub DeepCopy(ByRef newQuote As clsQuote, flexUpItem As Int32?, flexUpQuantity As Int32, ByRef errormessages As List(Of String))

        Dim descendants As List(Of clsQuoteItem) = Me.RootItem.Descendants  'includes the root item itself

        '                                               original                copy
        Dim copies As New Dictionary(Of clsQuoteItem, clsQuoteItem)
        copies.Add(Me.RootItem, newQuote.RootItem)

        Dim parent As clsQuoteItem

        For Each d In descendants
            If d IsNot Me.RootItem Then
                parent = copies(d.Parent)

                Dim buyPriceNow As NullablePrice

                'unused - but for reference...
                Dim BuyPriceBefore As NullablePrice
                BuyPriceBefore = d.BasePrice

                Dim pricesNow As List(Of clsPrice) = d.Branch.Product.GetPrices(Me.BuyerAccount, Me.BuyerAccount.SellerChannel.priceConfig, d.SKUVariant, errormessages, True)
                If pricesNow.Count > 0 Then 'Some virtual products (CHASSIS brnches specifically) have no pricing (are free)
                    buyPriceNow = pricesNow(0).Price ' 'VariantPrice(Me.QuoteItem.quote.BuyerAccount, Me.QuoteItem.SKUVariant)
                Else
                    buyPriceNow = d.BasePrice 'will be O (chassis branches)
                End If


                Dim copy As clsQuoteItem = New clsQuoteItem(newQuote, d.Branch, d.SKUVariant, d.Path, d.Quantity, buyPriceNow, d.ListPrice, d.IsPreInstalled, parent, d.OPG, d.Bundle, d.rebate, d.Margin, d.Note, d.order)
                If flexUpItem = d.ID Then copy.Quantity += flexUpQuantity : copy.Update()
                copies.Add(d, copy)
            End If
        Next


    End Sub

    'Public Sub New(QuoteToCopy As clsquote)

    '    'Clones the specified quote - to create a new one - with a new ID, version number, created and updated dates - but otherwise identical

    '    With QuoteToCopy

    '    End With
    '    Dim aquote As New clsquote(
    '    'Clone my rootitem (and, recursively all my child items) onto the new (cloned) quote
    '    Me.RootItem.Clone(aquote)

    'End Sub

    Public Sub New(ByVal BuyerAccount As clsAccount, AgentAccount As clsAccount, RootQuote As clsQuote, Created As DateTime, updated As DateTime, Version As Integer, state As clsState, price As NullablePrice, Currency As clsCurrency, _
                    locked As Boolean, hidden As Boolean, saved As Boolean, reference As String, name As nullableString, description As nullableString, totalRebate As Decimal, Optional BootStrap As Boolean = False, Optional writecache As DataTable = Nothing, Optional fk_Import_ID As Integer = 0)

        'Only this overload features the importID which forms no part of the quote object - it's just written to the table to allow only those rows added in the latest import to be updated
        'for better import performance (see import.quotes())

        'Me.Editable = True 'Make this quote editable as its brand new, if its ever loaded again then it wont be editable
        If Version = 0 Then Stop

        If RootQuote Is Nothing Then
            Me.RootQuote = Me
        Else
            Me.RootQuote = RootQuote
        End If

        Me.QuotedPrice = price 'total for quote including applied margin(s)
        Me.FK_Import_Id = fk_Import_ID

        Me.BuyerAccount = BuyerAccount
        Me.AgentAccount = AgentAccount
        Me.Created = Created
        Me.Updated = Created
        Me.Locked = locked
        Me.Hidden = hidden
        Me.Saved = saved
        Me.Notes = Notes
        Me.TotalRebate = totalRebate
        Me.Version = Version
        Me.State = state
        Me.Name = name
        Me.Description = description
        Me.Currency = Currency
        Me.RootItem = New clsQuoteItem(Me) 'Each quote has a virtual root quoteItem with an ID of 0 placeholder (see special constructor)

        Me.Reference = reference
        Me.Cursor = Me.RootItem
        Me.MostRecent = Me.RootItem  'becomes the target of the flying frames


        If (Not writecache Is Nothing) And BootStrap = False Then

            Me.ID = -1  'they will get their true ID's next time they're loaded
            Dim row As System.Data.DataRow
            row = writecache.NewRow()
            'note - we do not set the ID's - theyre auto generated
            row("FK_Account_ID_agent") = AgentAccount.ID
            row("FK_Account_ID_buyer") = BuyerAccount.ID
            row("Hidden") = hidden
            row("Locked") = locked
            row("Created") = Created
            row("Updated") = Now
            row("Saved") = saved
            row("FK_quote_id_root") = 1 '!!!!!!!!!!!!!!!!!!!!!!!!! All quotes are initially bulk imported pointing to quote #1 - we MUST (and do) subsequently update.
            row("FK_State_id") = state.ID
            row("Price") = price.value  'This is the total quoted price Including margin (at differing rates on all the items)
            row("FK_currency_id") = Currency.ID
            row("version") = Version
            row("reference") = reference
            row("FK_import_ID") = fk_Import_ID
            row("totalrebate") = totalRebate

            writecache.Rows.Add(row)

        Else
            Me.ID = SQLInsert(BootStrap)

        End If


        If Me.ID <> -1 Then 'note - bulk insered quotes won't be available via the agent account unti the OM is re-loaded
            AgentAccount.Quotes.Add(Me.ID, Me)
            If Not iq.Quotes.ContainsKey(Me.ID) Then iq.Quotes.Add(Me.ID, Me)
        End If

    End Sub

    Public Sub New(ByVal ID As Integer, ByVal BuyerAccount As clsAccount, AgentAccount As clsAccount, RootQuote As clsQuote, Created As DateTime, updated As DateTime, Version As Integer, state As clsState, price As NullablePrice, Currency As clsCurrency, _
                    locked As Boolean, hidden As Boolean, saved As Boolean, reference As String, name As nullableString, description As nullableString, ByVal totalRebate As Decimal)


        Me.ID = ID
        Me.BuyerAccount = BuyerAccount
        Me.AgentAccount = AgentAccount
        Me.Created = Created
        Me.Updated = updated
        Me.Locked = locked
        Me.Hidden = hidden
        Me.Saved = saved
        Me.Notes = Notes
        Me.Version = Version
        Me.TotalRebate = totalRebate
        If RootQuote Is Nothing Then
            Me.RootQuote = Me
        Else
            Me.RootQuote = RootQuote
        End If


        Me.State = state
        Me.QuotedPrice = price

        Me.Name = name
        Me.Description = description
        Me.Currency = Currency
        Me.Reference = reference

        Me.RootItem = New clsQuoteItem(Me)    'the placeholder  (see special constructor )
        Me.Cursor = Me.RootItem
        Me.MostRecent = Me.RootItem  'becomes the target of the flying frames

        AgentAccount.Quotes.Add(Me.ID, Me)

        If Not iq.Quotes.ContainsKey(Me.ID) Then
            iq.Quotes.Add(Me.ID, Me)  'put it in the root level dictionary too (for generic editing)
        End If

    End Sub

    Public Function ReplaceTags(l As String, ByRef doc As XmlDocument, ByRef errorMessages As List(Of String), language As clsLanguage) As String

        'returns a copy of l$ with the !TAGS! replaced with values from this quotes corresponding XML <tags> of the quotes XMLDoc()

        Dim opgs As New List(Of String)

        Dim sb As New StringBuilder(l)
        doc = Me.XMLDoc(errorMessages, opgs) 'genertae and fetch the XML of this quote

        Dim regex As Regex = New Regex("\|[A-z\ 0-9]+\|") 'ML - translate anything between |'s
        Dim matches = regex.Matches(l)
        For Each m As Match In matches
            sb.Replace(m.Value, iq.AddTranslation(m.Value.Trim("|".ToArray()), English, "Export", 0, Nothing, 0, False).text(language))
        Next
        Dim header As XmlNode = doc.GetElementsByTagName("Header")(0)   'Contains the <QuoteID>,<Version>,<Date> and <Total> tags - whos contents will replace the respective !QuoteID!,!Date! and !Total! tags
        sb = setHeaderTags(sb, header, opgs)
        If sb.ToString.Contains("Prepared for:-") Then
            sb.Replace("Prepared for:-", String.Empty)
        End If
        If sb.ToString.Contains("Opt Type") Then
            sb.Replace("Opt Type", String.Empty)
        End If

        Dim Buyer As XmlNode
        Buyer = doc.GetElementsByTagName("Buyer")(0)
        For Each item As XmlNode In Buyer.ChildNodes
            sb.Replace("!" & item.Name & "!", item.InnerText)
        Next

        Dim Seller As XmlNode
        Seller = doc.GetElementsByTagName("Seller")(0)
        For Each item As XmlNode In Seller.ChildNodes
            sb.Replace("!" & item.Name & "!", item.InnerText)
        Next

        sb.Replace("!cols!", "20")
        sb.Replace("!cols-rep!", "11")

        sb.Replace("!TC!", If(Me.AgentAccount.SellerChannel.Legal IsNot Nothing, HttpUtility.HtmlEncode(clsIQ.CleanString(Me.AgentAccount.SellerChannel.Legal)), String.Empty))
        Dim currencySetting As String = String.Format("{0}{1}{2}{3}{4}{5}{6}", "<number:currency-symbol number:language=""", Me.AgentAccount.Language.Code, """ number:country=""", Me.AgentAccount.SellerChannel.Region.Code, """>", Me.Currency.Symbol, "</number:currency-symbol>")
        sb.Replace("!Currency!", currencySetting)
        '"
        'The following lines use (some very simple) XPATH to select nodes from the document - see http://www.w3schools.com/xpath/xpath_syntax.asp
        'the // prefix selects nodes from anywhere in the document


        Dim currencySymbol As XmlNode = doc.SelectSingleNode("Quote/Header/CurrencySymbol")
        Dim flatlines As XmlNodeList = doc.SelectSingleNode("//FlatQuote").SelectNodes("Line")
        flatlines.Item(0).ParentNode.AppendChild(currencySymbol)  'add the currency from the root level of the quote into the nodelist so it can be used as a tag

        'Do it again for sheet 2 (but this time with indents)
        Dim treeLines As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("Line")
        treeLines.Item(0).ParentNode.AppendChild(currencySymbol)  'add the currency from the root level of the quote into the nodelist so it can be used as a tag

        sb = ReplaceTagsInRowContaining("!OptType!", sb, flatlines, False, opgs)
        sb = ReplaceTagsInRowContaining("!AdviceIcon!", sb, doc.SelectNodes("//TreeQuote/Line/Advice"), False, opgs)
        'Select *all* the advice tags under the TreeView of the quote
        sb = ReplaceTagsInRowContaining("!OptType!", sb, treeLines, True, opgs)
        sb = ReplaceTagsInRowContaining("!AdviceIcon!", sb, doc.SelectNodes("//TreeQuote/Line/Advice"), False, opgs)
        sb = ReplaceTagsInRowContaining("!Note!", sb, doc.SelectNodes("//TreeQuote/Line/Note"), False, opgs)

        Dim adviceNodes As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//Advice")
        sb = RemoveLabelByNodeCount(adviceNodes, "Advisory Notes", sb)

        Dim notes As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//Note")
        sb = RemoveLabelByNodeCount(notes, "Agent Notes", sb)
        If opgs.Count = 0 Then
            sb = setLablelsWhenOPGCountZero(doc, sb)
        End If
        If Me.AgentAccount.SellerChannel.Universal Then
            sb = setUniversalLabels(doc, sb)
        End If

        Return sb.ToString

    End Function
    ''' <summary>
    '''  Set labels to hide when ppg count is zero.
    ''' </summary>
    ''' <param name="doc">An instance of XMLDocument.</param>
    ''' <param name="sb">An instance of StringBuilder.</param>
    ''' <returns>An instance of StringBuilder.</returns>
    ''' <remarks></remarks>
    Private Function setLablelsWhenOPGCountZero(ByRef doc As XmlDocument, sb As StringBuilder) As StringBuilder
        Dim opg As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//OPG")
        sb = RemoveLabelByNodeCount(opg, "OPG", sb, True)
        Dim lineRebate As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//LineRebate")
        sb = RemoveLabelByNodeCount(lineRebate, "Rebate", sb, True)
        Dim totalRebate As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//TotalRebate")
        sb = RemoveLabelByNodeCount(totalRebate, "Savings:", sb, True)
        Dim quoteTotal As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//QuoteTotal")
        sb = RemoveLabelByNodeCount(quoteTotal, "Quote Total:", sb, True)
        Return sb
    End Function

    ''' <summary>
    ''' Set labels to hide when uninversal.
    ''' </summary>
    ''' <param name="doc">An instance of XMLDocument.</param>
    ''' <param name="sb">An instance of StringBuilder.</param>
    ''' <returns>An instance of StringBuilder.</returns>
    ''' <remarks></remarks>
    Private Function setUniversalLabels(ByRef doc As XmlDocument, sb As StringBuilder) As StringBuilder
        Dim listPrice As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//ListPrice")
        Dim stock As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//Stock")
        Dim opg As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//OPG")
        Dim lineRebate As XmlNodeList = doc.SelectSingleNode("//TreeQuote").SelectNodes("//LineRebate")

        sb = RemoveLabelByNodeCount(listPrice, "List Price", sb, True)
        sb = RemoveLabelByNodeCount(stock, "Stock", sb, True)
        sb = RemoveLabelByNodeCount(opg, "OPG", sb, True)
        sb = RemoveLabelByNodeCount(lineRebate, "Rebate", sb, True)
        Return sb
    End Function
    ''' <summary>
    ''' Update header and total tags
    ''' </summary>
    ''' <param name="sb">An instance of StringBuilder.</param>
    ''' <param name="header">An instance of XmlNode representing header.</param>
    ''' <param name="opgs">An isntance of List of type string represntinng opgs on count.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function setHeaderTags(sb As StringBuilder, header As XmlNode, opgs As List(Of String)) As StringBuilder
        Dim it As String = String.Empty
        For Each item As XmlNode In header.ChildNodes
            it = item.InnerText
            If (item.Name.ToUpper = "TOTALREBATE" Or item.Name.ToUpper = "QUOTETOTAL") And opgs.Count = 0 Then
                it = " "
            End If
            sb.Replace("!" & item.Name & "!", it)
        Next
        Return sb
    End Function
    ''' <summary>
    ''' Replaces label if count of nodes is 0 or forced by optional parameter removeheader.
    ''' </summary>
    ''' <param name="xmlNodelist">An instance of XMLNodeList. </param>
    ''' <param name="label">A string object that represents the name of the label in the xml. </param>
    ''' <param name="sb">An instance an StringBuilder.</param>
    ''' <param name="removeHeader">a boolean value optional parameter default is true so will always remove label unless passedin as false.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function RemoveLabelByNodeCount(xmlNodelist As XmlNodeList, label As String, sb As StringBuilder, Optional removeHeader As Boolean = True) As StringBuilder
        Dim i As Integer = 0
        If removeHeader = False Then
            For Each node As XmlNode In xmlNodelist
                i += 1
                Exit For
            Next
        End If
        If i = 0 Then
            sb.Replace(label, String.Empty)
        End If
        Return sb
    End Function
    'Private Function DuplicateAndReplaceTagsInRowContaining(tag$, ByVal l$, NodeList As XmlNodeList, WithIndents As Boolean) As String

    '    'extracts the  complete row containing tag$ - and duplicates it for each child  of node, replacing !tags! (in that row) with their corresponding xml nodes (children of Node)
    '    'For example - replaces the row with the !OptType! tag (a single row in the quote) with the flat quotes  child items (ie. all the rows in the quote)
    '    'also used to replace the !advice! tage with all advice lines

    '    'get a copy of the row that contains the original tag
    '    Dim rm As Integer = InStr(l$, tag)   'Find the position of the opttype tag - the row we will repeat many times
    '    Dim br As Integer = InStrRev(Left$(l$, rm), "<table:table-row")
    '    Dim er As Integer = InStr(br, l$, "</table:table-row")
    '    er = InStr(er, l$, ">")
    '    Dim il$ = Mid$(l$, br, er - br + 1)

    '    Dim nl$ = ""
    '    Dim qb$ = "" 'quote body

    '    Dim indent As Integer = 0
    '    Dim columnsToHide = "OPG LINEREBATE STOCK"
    '    For Each row As XmlNode In NodeList 'iterate the direct children for the <FlatQuote> (<line>s)
    '        nl$ = il$ 'make a copy of the unmolested (template) Item line (with the tags in) 
    '        'replace each !TAG! with the contents of the correspondingly named child element 

    '        If row.ChildNodes.Count > 0 Then  'don't process empt elements (such as empty advice tags)
    '            Dim iXML$
    '            For Each col As XmlNode In row.ChildNodes  'the cols are the things we're replacing in each row, EG SKU, Price, Description - or (for advice, AdviceIcon, Text)
    '                If WithIndents Then
    '                    '                                      was innertext (but DVD_+/- drives broke
    '                    If col.Name = "Indent" Then indent = CInt(col.InnerXml) 'on the 'breakdown' (non-bill of materials) view - the heirarchy of the quote is preserved/displayed - each row has an indent level
    '                End If


    '                '    txt = col.InnerText
    '                iXML = col.InnerXml  'We MUST use the innerXML 

    '                'If InStr(txt, "&") Then Stop

    '                'If indent <> 0 And col.Name = "Description" Then txt$ = Strings.StrDup(indent - 1, "---") & txt$
    '                If indent <> 0 And col.Name = "Description" Then iXML = Strings.StrDup(indent - 1, "---") & iXML
    '                If Not (Me.AgentAccount.SellerChannel.Code = "HP" And columnsToHide.Contains(col.Name.ToUpper)) Then
    '                    nl$ = Replace(nl$, "!" & col.Name & "!", iXML)
    '                Else
    '                    nl$ = Replace(nl$, "!" & col.Name & "!", "")
    '                End If
    '            Next
    '            qb$ &= nl$
    '        End If
    '    Next

    '    Dim b$, e$
    '    b$ = Left$(l$, br - 1)
    '    e$ = Mid$(l$, er + 1)
    '    l$ = b$ & qb$ & e$

    '    Return l$

    'End Function
    Private Function ReplaceTagsInRowContaining(tag As String, ByVal l As StringBuilder, NodeList As XmlNodeList, WithIndents As Boolean, opgs As List(Of String)) As StringBuilder

        'extracts the  complete row containing tag$ - and duplicates it for each child  of node, replacing !tags! (in that row) with their corresponding xml nodes (children of Node)
        'For example - replaces the row with the !OptType! tag (a single row in the quote) with the flat quotes  child items (ie. all the rows in the quote)
        'also used to replace the !advice! tage with all advice lines

        'get a copy of the row that contains the original tag
        Dim br As Integer = 0
        Dim er As Integer = 0
        Dim il As String = GetTag(l, tag, er, br)
        Dim nl As New StringBuilder(String.Empty)
        Dim qb As New StringBuilder(String.Empty) 'quote body

        Dim indent As Integer = 0

        Dim columnsToHide = "OPG LINEREBATE STOCK LISTPRICE"
        For Each row As XmlNode In NodeList 'iterate the direct children for the <FlatQuote> (<line>s)
            'make a copy of the unmolested (template) Item line (with the tags in) 
            'replace each !TAG! with the contents of the correspondingly named child element 
            nl = New StringBuilder(il)
            If row.ChildNodes.Count > 0 Then  'don't process empt elements (such as empty advice tags)
                Dim iXML As String
                For Each col As XmlNode In row.ChildNodes  'the cols are the things we're replacing in each row, EG SKU, Price, Description - or (for advice, AdviceIcon, Text)
                    If WithIndents Then
                        ' was innertext (but DVD_+/- drives broke
                        If col.Name = "Indent" Then
                            indent = Integer.Parse(col.InnerXml) 'on the 'breakdown' (non-bill of materials) view - the heirarchy of the quote is preserved/displayed - each row has an indent level
                        End If
                    End If
                    iXML = col.InnerXml  'We MUST use the innerXML 

                    If Not (Me.AgentAccount.SellerChannel.Universal And columnsToHide.Contains(col.Name.ToUpper)) Then
                        nl = GetMainReplacements(nl, col.Name, iXML, opgs)
                    Else
                        'Because the ODS file has formated columns currency must be set to space and replace float to string and then replace value with space.
                        nl = SetStockToFloatFromStringWhenHiding(nl, col.Name, False, True)
                        nl.Replace("!" & col.Name & "!", " ")
                    End If
                Next
                qb.Append(nl)
            End If
        Next


        Return GetResultXmlString(br, er, qb.ToString, l)

    End Function
    ''' <summary>
    ''' Gets replacements for stringh builder 
    ''' </summary>
    ''' <param name="nl">An instance of StringBuilder that represents part of the xml.</param>
    ''' <param name="colname">An instance of String that represents the Column Name to check or value to be changed</param>
    ''' <param name="value">A string object that the value for the column at that point. </param>
    ''' <param name="opgs">an instance of List of type string.</param>
    ''' <returns>An instance of StringBuilder</returns>
    ''' <remarks></remarks>
    Private Function GetMainReplacements(nl As StringBuilder, colname As String, value As String, opgs As List(Of String)) As StringBuilder
        If Me.AgentAccount.SellerChannel.BinaryStock Then
            nl = SetStockToFloatFromStringWhenHiding(nl, colname, True, False)
        End If
        If opgs.Count = 0 Then
            nl = SetLineRebateWhenNoOPGSwhenHiding(nl, colname)
        Else
            nl.Replace("!" & colname & "!", value)
        End If
        nl.Replace("!" & colname & "!", value)
        Return nl
    End Function
    ''' <summary>
    ''' This is to replace float with string in the attributes in the line of XML
    ''' </summary>
    ''' <param name="nl">An instance of StringBuilder that represents the part of the xml.</param>
    ''' <param name="colname">An instance of String that represents the Column Name to check.</param>
    ''' <param name="binarystock">A boolean value that represents whether the channel uses binarystock.</param>
    ''' <param name="universal">A boolean value that represents whether the channel is universal.</param>
    ''' <returns>An instance of an StringBuilder.</returns>
    ''' <remarks>This need to be done as if string empty or space is replaced then zero will be dipslayed.</remarks>
    Private Function SetStockToFloatFromStringWhenHiding(nl As StringBuilder, colname As String, binarystock As Boolean, ByVal universal As Boolean) As StringBuilder
        If colname.ToUpper = "STOCK" Then
            '<table:table-cell office:value-type="float" office:value="!Stock!" table:style-name="ce-right">
            Dim x As String = " office:value-type=" & Chr(34) & "float" & Chr(34) & " office:value=" & Chr(34) & "!" & colname & "!" & Chr(34) & " table:style-name=" & Chr(34) & "ce-right" & Chr(34) & ">"
            Dim i As Integer = nl.ToString.IndexOf(x)
            If i > 0 Then
                If binarystock Or universal Then
                    Dim y As String = x.Replace("float", "string")
                    nl.Replace(x, y)
                    If binarystock Then
                        Dim z As String = y.Replace(" table:style-name=" & Chr(34) & "ce-right" & Chr(34), String.Empty)
                        nl.Replace(y, z)
                    End If

                End If
            End If
        End If
        Return nl
    End Function
    Private Function SetLineRebateWhenNoOPGSwhenHiding(nl As StringBuilder, colname As String) As StringBuilder
        If colname.ToUpper = "LINEREBATE" Then
            ''<table:table-cell office:value-type="currency" table:style-name="ce-currency" office:value="!LineRebate!">
            Dim x As String = " office:value-type=" & Chr(34) & "currency" & Chr(34) & " table:style-name=" & Chr(34) & "ce-currency" & Chr(34) & " office:value=" & Chr(34) & "!" & colname & "!" & Chr(34) & ">"
            Dim i As Integer = nl.ToString.IndexOf(x)
            If i > 0 Then

                Dim y As String = x.Replace("currency", "string")
                nl.Replace(x, y)
                Dim z As String = y.Replace(" table:style-name=" & Chr(34) & "ce-string" & Chr(34), String.Empty)
                nl.Replace(y, z)
                nl.Replace("!" & colname & "!", " ")

            End If
        End If
        Return nl
    End Function
    ''' <summary>
    ''' Puts all the xml back together.
    ''' </summary>
    ''' <param name="br">An integer representing a postion in the oringinal XML.</param>
    ''' <param name="er">An integer representing a postion in the oringinal XML.</param>
    ''' <param name="qb">An instance of String that represents the new parts of the xml.</param>
    ''' <param name="l">An instance of StringBuilder that represents the whole of the xml.</param>
    ''' <returns>An instance of an StringBuilder.</returns>
    ''' <remarks></remarks>
    Private Function GetResultXmlString(ByVal br As Integer, ByVal er As Integer, qb As String, l As StringBuilder) As StringBuilder
        Dim b As String = String.Empty
        Dim e As String = String.Empty
        Dim sb As New StringBuilder(String.Empty)
        b = l.ToString.Substring(0, br - 1)
        e = l.ToString.Substring(er + 1)
        sb.Append(b & qb & e)
        Return sb
    End Function
    ''' <summary>
    ''' Gets string from stringbuilder with tag to look for.
    ''' </summary>
    ''' <param name="sb">An instance of a stringBuilder.</param>
    ''' <param name="tag">An instance of a string. </param>
    ''' <returns>An instance of an string.</returns>
    ''' <remarks></remarks>
    Private Function GetTag(ByVal sb As StringBuilder, ByVal tag As String, ByRef er As Integer, ByRef br As Integer) As String
        Dim rm As Integer = sb.ToString.IndexOf(tag)   'Find the position of the opttype tag - the row we will repeat many times
        br = sb.ToString.Substring(0, rm).LastIndexOf("<table:table-row")
        er = sb.ToString.IndexOf("</table:table-row", br)
        er = sb.ToString.IndexOf(">", er)
        Dim il As String = sb.ToString.Substring(br, er - br + 1)
        Return il
    End Function

    Public Function Additem(branch As clsBranch, SKUVariant As clsVariant, path As String, qty As Integer, price As NullablePrice, listPrice As NullablePrice, ParentItem As clsQuoteItem, margin As Single, withAutoAdds As Boolean, ByRef errorMessages As List(Of String)) As clsQuoteItem

        'Makes a new (hierarchical) quote item - complete with a set of preinstalled sub items! - AND adds it to the quote
        'under the right parent item (in the quote) for this option (NB: - it's direct parent (in the product tree) - eg 'Hard Disk Drives' may not be in the quote)

        Dim item As clsQuoteItem

        If ParentItem Is Nothing Then Stop



        ' If price.NumericValue > listPrice.NumericValue Then Stop 'todo remove


        'systems go at the begining - options go on the end  ' this makes the basket act like a stack
        Dim ord As Integer
        If ParentItem.Children.Count = 0 Then
            ord = 100
        Else
            If branch.Product.isSystem And Split(path, ".").Length < 6 Then
                'systems go on the top
                ord = (From ch In ParentItem.Children Select ch.order).Min - 10
            Else
                'options go on the bottom
                ord = (From ch In ParentItem.Children Select ch.order).Max + 10
            End If
        End If

        'synnex demo /universal  'fix'
        If Not price.isValid Then
            price = listPrice
        End If


        item = New clsQuoteItem(Me, branch, SKUVariant, path, qty, price, listPrice, False, ParentItem, New nullableString, New nullableString, 0, margin, New nullableString(), ord)

        ' Debug.Print("Additem")
        'we do not pass a quantity to AddPreinstalled - those are determined by the quantity records
        addPreinstalledRecursive(item, item.Branch, item.Path, withAutoAdds, errorMessages)



        ' If Me.RootItem Is Nothing Then Me.RootItem = item

        Return item

    End Function


    ''' <summary> 
    ''' 'Returns a placholder containing the entyire quotes UI 
    ''' </summary> 
    ''' <remarks>Largely by calling Rootitem.UI</remarks> 
    ''' <returns>A big lump of HTML</returns>

    Public Function UI(foci As HashSet(Of String), lid As UInt64) As PlaceHolder

        Dim errorMessages As List(Of String) = New List(Of String)

        UI = New PlaceHolder
        ' Dim ph As New Panel

        'Dim tabstrip As Panel
        'tabstrip = New Panel

        'tabstrip.ID = "quoteBasketTabStrip"

        ' tabstrip.Controls.Add(MakeTab("Breakdown", viewtype))
        'tabstrip.Controls.Add(MakeTab("Summary", viewtype))
        'tabstrip.Controls.Add(MakeTab("Validation", viewtype))

        'OutputHTML.Controls.Add(tabstrip)

        'Focus is a list of the product sets we're viewing (eg. Receta)
        'If viewtype = "Breakdown" Then

        'Quote Totals
        UpdateDescAndPrice()

        Dim subheader As Panel = New Panel
        subheader.ID = "quouteSubHeader"
        UI.Controls.Add(subheader)

        'the UI of the rootItem includes the quoteHeader

        UI.Controls.Add(Me.RootItem.UI(True, BuyerAccount, AgentAccount, foci, errorMessages, False, lid)) ', iq.sesh(lid,"quoteCursor"))
        OutputErrors(UI.Controls, errorMessages, lid)


        Dim txtPartsList As TextBox = New TextBox
        txtPartsList.TextMode = TextBoxMode.MultiLine
        txtPartsList.Attributes("style") &= " height:0px;"  'this hides it - but still renders it into the page (if you set visible to false - the control isn't rendered into the page !)

        UI.Controls.Add(txtPartsList)
        txtPartsList.ID = "txtPartsList"

        txtPartsList.Text = Me.RootItem.FlatListTXT(BuyerAccount)  'for copy to clipboard we output the equivilent of the BOM view (ie. installe doption qunaities are multiplied by the system quantities)
        UI.Controls.Add(txtPartsList)

        'the copy to clipboard button already exists a a static control in the page - and looks like this
        '<asp:Button ID="BtnCopy" runat="server" Text="Copy" cssclass="textButton" ToolTip="Copy the quote parts list to the ClipBoard" onmousedown="copyToClipBoard(txtPartsList);return false;"/>

        ' OutputHTML.Controls.Add(lit) <may have missed something here

    End Function

    ''' <summary>
    ''' Builds a hierarchical quote from a flat, delimited list. 
    ''' </summary>
    ''' <param name="lid"></param>
    ''' <param name="txt"></param>
    ''' <param name="errorMessages"></param>
    ''' <returns>A list of warnings (not to be confused with errrorMessages) (unrecognised/disallowed parts)</returns>
    ''' <remarks></remarks>
    Public Shared Function FromShoppingList(lid As UInt64, AgentAccount As clsAccount, BuyerAccount As clsAccount, txt As String, ByRef errorMessages As List(Of String), Optional ByRef FirstSysPath As String = "") As List(Of String)

        Dim msgs As List(Of String) = New List(Of String)  'warinings/exceptions to show the user

        If InStr(txt, vbCr) > 0 Then
            Beep()
        End If
        txt$ = Replace(txt, vbCrLf, vbCr)  'Switch all delimiters to CR's
        txt$ = Replace(txt$, ",", vbCr)
        txt$ = Replace$(txt$, ";", vbCr)

        Dim p() As String = Split(txt$, vbCr)
        Dim b() As String

        Dim qty As Integer
        Dim partno As String

        Dim ln As Integer = 0

        Dim product As clsProduct = Nothing
        Dim currentSystem As clsProduct = Nothing

        'We defualt these to the root - for option searches (ie, where the first part in the list is an option, not a system)
        Dim systemBranch As clsBranch = iq.RootBranch
        Dim systemPath$ = "tree.1"

        Dim optionBranch As clsBranch = Nothing
        Dim optionPath As String = ""
        Dim SystemQuoteItem As clsQuoteItem = Nothing

        'build a hashset from the CD list stored in the sesstion variable
        Dim foci As HashSet(Of String) = New HashSet(Of String)(Split(CType(iq.sesh(lid, "foci"), String), ",").ToList)


        Dim quote As clsQuote = Nothing
        If iq.SeshContains(lid, "QuoteID") AndAlso CInt(iq.sesh(lid, "QuoteID")) <> 0 Then

            'if we have a quote on the go, It will add to it - may want to change this behaviour (discussed 06/06/2013 with Dan)
            Dim qid As Integer = CInt(iq.sesh(lid, "QuoteID"))
            quote = AgentAccount.Quotes(qid)
        Else


            'moved to only create the (new) quote when we hit the virst valid item
            'Dim nullprice As NullablePrice 'The quote will start life with an unknown price
            'nullprice = New NullablePrice(bi.buyerAccount.Currency)
            'quote = New clsQuote(bi.BuyerAccount, bi.AgentAccount, Nothing, Now, Now, CInt(1), iq.i_state_GroupCode("QT-#NW"), nullprice, bi.BuyerAccount.Currency, False, False, False, "", New nullableString(), New nullableString())
            'iq.sesh(bi.lid, "QuoteID") = quote.ID

        End If

        ' Resolve the list of products first so we can check for HPE/HPI splits before adding anything to the quote
        Dim products As List(Of clsShoppingListItem) = New List(Of clsShoppingListItem)()
        Dim hpeCount As Integer = 0
        Dim hpiCount As Integer = 0
        Dim splitFound As Boolean = False
        For Each QtyPartno In p

            ln += 1

            b = Split(QtyPartno, "*")
            partno = String.Empty
            product = Nothing

            Try
                If b.Count = 2 Then
                    If String.IsNullOrWhiteSpace(b(1)) Then
                        b(1) = "1"
                    End If
                    If iq.i_SKU.ContainsKey(Trim(b(0))) And Val(b(1)) > 0 And Val(b(1)) < 9999 Then
                        partno = Trim(b(0))
                        qty = CInt(b(1))
                    ElseIf iq.i_SKU.ContainsKey(Trim(b(1))) And Val(b(0)) > 0 And Val(b(0)) < 9999 Then
                        partno = Trim(b(1))
                        qty = CInt(b(0))
                    Else
                        msgs.Add(Xlt("Line", AgentAccount.Language) & ln & " is invalid:" & QtyPartno & " is unrecognised")
                    End If
                ElseIf b.Count = 1 Then
                    'quantityless line
                    If Trim(b(0)) <> "" Then
                        If iq.i_SKU.ContainsKey(Trim(b(0))) Then
                            qty = 1
                            partno = Trim(b(0))
                        Else
                            msgs.Add(Xlt("Line ", AgentAccount.Language) & ln & Xlt(" is invalid: ", AgentAccount.Language) & QtyPartno & Xlt(" unrecognised", AgentAccount.Language))
                        End If
                    End If
                End If
            Catch ex As Exception
                msgs.Add(Xlt("Line ", AgentAccount.Language) & ln & Xlt(" is invalid: ", AgentAccount.Language) & QtyPartno & Xlt(" unrecognised", AgentAccount.Language))
            End Try

            If msgs.Count > 0 Then Return (msgs)

            If Not String.IsNullOrWhiteSpace(partno) Then
                product = iq.i_SKU(partno)
            End If

            If Not product Is Nothing Then

                If product.Manufacturer <> Manufacturer.Unknown AndAlso product.Manufacturer <> AgentAccount.Manufacturer Then

                    msgs.Add(Xlt("Line ", AgentAccount.Language) & ln & Xlt(" is invalid: ", AgentAccount.Language) & QtyPartno & Xlt(" unrecognised", AgentAccount.Language))
                    splitFound = True
                    Exit For
                End If


                'If product.Manufacturer = Manufacturer.HPE Then
                '    hpeCount += 1
                'ElseIf product.Manufacturer = Manufacturer.HPI Then
                '    hpiCount += 1
                'End If

                'If hpiCount > 0 AndAlso hpeCount > 0 Then
                '    ' HPE/HPI split found - reject the shopping list
                '    msgs.Add(Xlt("Mixed quotes for PPS and EG detected. Please select which product set you wish to carry through to quote.", AgentAccount.Language))
                '    splitFound = True
                '    Exit For
                'End If

                products.Add(New clsShoppingListItem(QtyPartno, partno, qty, product))

            End If

        Next
        If splitFound Then Return (msgs)

        Dim Price As NullablePrice = Nothing
        Dim listPrice As NullablePrice
        Dim prices As List(Of clsPrice)
        Dim customerPrice As clsPrice ' nullablePrice
        Dim sysProduct As String = String.Empty
        Dim optionProduct As String = String.Empty
        Dim sysQty As Integer = 0
        Dim optQty As Integer = 0
        Dim locations As Dictionary(Of String, clsBranch)
        ln = 0
        For Each shoppingListItem In products

            ln += 1

            Dim QtyPartno = shoppingListItem.QtyPartNo
            product = shoppingListItem.Product
            partno = shoppingListItem.PartNo
            qty = shoppingListItem.Quantity

            If product.isSystem Then
                sysProduct = partno
                sysQty = qty
                optQty = 0
            Else
                optionProduct = partno
                optQty = qty
            End If

            If sysQty > 0 And optQty > 0 Then

                If sysQty > optQty Or (optQty Mod sysQty) <> 0 Then
                    msgs.Add(Xlt("Line ", AgentAccount.Language) & ln & Xlt(" There is a mismatch between system quantity and option quantity. ", AgentAccount.Language) & QtyPartno & Xlt(" with ", AgentAccount.Language) & sysProduct)
                    If quote IsNot Nothing Then
                        quote.delete()
                        quote = Nothing
                    End If
                    iq.sesh(lid, "QuoteID") = Nothing
                End If

            End If

            If msgs.Count = 0 Then
                '    listPrice = product.ListPrice(buyeraccount).Price
                'fetch the list price if there is one - or constuct a POA in the correct currency
                listPrice = NullPrice(product.ListPrice(BuyerAccount), BuyerAccount.Currency)

                prices = product.GetPrices(BuyerAccount, BuyerAccount.SellerChannel.priceConfig, iq.AllVariants, errorMessages, True)

                If prices.Count = 0 OrElse prices(0) Is Nothing Then
                    msgs.Add(Xlt("Line", AgentAccount.Language) & ln & " " & partno & Xlt("No customer specific or list price variant available - item cannot be added", AgentAccount.Language))
                Else
                    customerPrice = Utility.LowestPrice(prices)

                    If customerPrice.SKUVariant Is Nothing Then
                        'A product for which we have no disti variant or HP 'standard'/list price
                        msgs.Add(Xlt("Line", AgentAccount.Language) & ln & " " & partno & Xlt("No customer specific or list price variant available - item cannot be added", AgentAccount.Language))
                    Else

                        'OK there's something vialid in the shopping list - NOW create a quote
                        If quote Is Nothing Then
                            Dim nullprice As NullablePrice 'The quote will start life with an unknown price
                            nullprice = New NullablePrice(BuyerAccount.Currency)
                            quote = New clsQuote(BuyerAccount, AgentAccount, Nothing, Now, Now, CInt(1), iq.i_state_GroupCode("QT-#NW"), nullprice, BuyerAccount.Currency, False, False, False, String.Empty, New nullableString(), New nullableString(), 0)
                            iq.sesh(lid, "QuoteID") = quote.ID

                            iq.sesh(lid, "paradigm") = enumParadigm.configuringSystem
                            'bi.Paradigm = enumParadigm.configuringSystem - ML Removed, orry if this breaks it but I dont think it will, needed to remove the dependancy on bi for the solution store system
                        End If

                        If product.isSystem Then
                            Dim fruitlessGrafts As HashSet(Of clsBranch) = New HashSet(Of clsBranch)
                            locations = iq.RootBranch.findProductBranches("tree.1", BuyerAccount.SellerChannel, product, False, fruitlessGrafts, True)
                            currentSystem = product
                            systemPath$ = locations.Keys(0)
                            systemBranch = locations.Values(0)

                            If String.IsNullOrEmpty(FirstSysPath) Then FirstSysPath = systemPath

                            Dim hrs As List(Of String) = systemBranch.ReasonsForHide(BuyerAccount, foci, systemPath, BuyerAccount.SellerChannel.priceConfig, False, errorMessages)
                            If hrs.Count > 0 Then
                                msgs.Add("Line " & ln & " System " & partno & " is not available to you (" & Join(hrs.ToArray, ",") & ")")
                            Else
                                'HP Split changes
                                ' SK - This is (one instance of?) the backstop "basket interceptor" for split; It shouldn't be possible to get here,
                                ' but previously this logic was being driven by the MFR of the first product into the basket, not the account's MFR
                                If product.Manufacturer = AgentAccount.Manufacturer Then
                                    SystemQuoteItem = quote.Additem(systemBranch, customerPrice.SKUVariant, systemPath$, qty, customerPrice.Price, listPrice, quote.RootItem, 1, False, errorMessages)
                                Else
                                    msgs.Add("Line " & ln & " System " & partno & " can't be added as this would create a mixed quote and PPS/EG products need to be quoted separately.")
                                End If
                            End If
                        Else 'it's an option
                            'If an option is required outside of a system searching the entire tree for it is not feasible (searching grafts might be)
                            Dim fruitlessGrafts As HashSet(Of clsBranch) = New HashSet(Of clsBranch) 'used to (massively) accelerate the search (by searching each non fruitful branch only once)
                            If systemPath$ = "tree.1" Then


                                errorMessages.Add("Please make sure your list starts with a system SKU")
                            Else
                                locations = systemBranch.findProductBranches(systemPath$, BuyerAccount.SellerChannel, product, True, fruitlessGrafts, True) 'will cross subsystems
                                If locations.Count = 0 Then
                                    msgs.Add("Line " & ln & " " & partno & " is not a compatible option for system " & currentSystem.SKU)
                                Else
                                    optionPath = locations.Keys(0)
                                    optionBranch = locations.Values(0)
                                    Dim hrs As List(Of String) = optionBranch.ReasonsForHide(BuyerAccount, foci, optionPath, BuyerAccount.SellerChannel.priceConfig, False, errorMessages)
                                    If hrs.Count > 0 Then
                                        msgs.Add("Line " & ln & " Option " & partno & " is not available to you (" & Join(hrs.ToArray, ",") & ")")
                                    Else
                                        Dim addto As clsQuoteItem
                                        If SystemQuoteItem IsNot Nothing Then
                                            addto = SystemQuoteItem
                                        Else
                                            addto = quote.RootItem
                                        End If
                                        qty = CInt(qty / sysQty)
                                        quote.Additem(optionBranch, customerPrice.SKUVariant, optionPath$, qty, customerPrice.Price, listPrice, addto, 1, False, errorMessages)
                                    End If
                                End If
                            End If
                        End If
                    End If

                End If
            End If
        Next


        If quote IsNot Nothing AndAlso quote.RootItem.Children.Count > 0 Then
            iq.sesh(lid, "quoteCursor") = quote.RootItem.Children(0).ID
            Dim err2 As List(Of String) = New List(Of String)
            Dim toget As List(Of clsVariant) = New List(Of clsVariant)

            'only do this if there's a webservice to call
            If CBool(BuyerAccount.SellerChannel.priceConfig And 8) Then
                quote.getPrice(toget, lid, err2)
            End If

        End If



        Return msgs

    End Function

    Private lock As String = ""

    ''' <summary>Performs all validations  on an entrie quote</summary> 
    ''' <param name="buyerAccount">The account for which pricing should be displayed</param> 
    ''' <param name="agentAccount">The agent doing the quote (determines the language of the UI)</param> 
    ''' <remarks></remarks> 
    Public Sub validate(lid As UInt64, buyerAccount As clsAccount, agentAccount As clsAccount, ByRef errorMessages As List(Of String), Optional calcTotalRebate As Boolean = True)

        'Validation (of the whole quote - sets/limits the flexbuttons and adds per item messages)
        'Validate slot usage

        'We validate each root item independently 
        'this is to deal with he UDIMM/RDIMM thing and allows slots (UDIMM/RDIMM for example) to be 'given' by componenents earlier in the walk, 
        'and 'taken' by non descendant items later in the walk

        'we should store/restore the dicslots as we recurse past systems as, at present there is a potential for a subSYSTEM to 'give' slots 'up' to a parent system
        'whilst the resulting configuration would be valid (in terms of there would be enough slots in the complete configuration to accommodate all the otpions)
        'The quote hierarchy would not reflect how you would actually have to plug it together - ie. you may show 10 drives in a slave device with only 8 bays.


        SyncLock lock 'don't validate twice, this was causing double ups on the validation messages

            Me.RootItem.clearMessage()  'recursively clears the warning messages on every quote item (from the root)

            Me.RootItem.ValidateFill(agentAccount) '(recursively) Checks the required fill level is satisfied for each slot

            Dim systemItems As List(Of clsQuoteItem)
            systemItems = Me.RootItem.findSystemItems

            For Each I As clsQuoteItem In systemItems  'Validate each "system" independently - don't cross system boundaries (don't recurse into other systems)

                Dim dicslots As Dictionary(Of clsSlotType, clsSlotSummary) = New Dictionary(Of clsSlotType, clsSlotSummary)()

                I.ValidateSlots2(dicslots, True) 'Recursive ! - compiles (and uses internally the quotes dicslots) =- Gives
                I.ValidateSlots2(dicslots, False) 'Now for takes, to fill fallbacks

                Dim summary As String = ""

                'for debug only
                For Each kvp In dicslots
                    summary &= "Mj:" & kvp.Key.MajorCode & " Mn:" & kvp.Key.MinorCode & " Gvn:" & kvp.Value.Given & " Tkn:" & kvp.Value.taken & vbCrLf
                Next

                    I.dicslots = dicslots
                    Dim message As ClsValidationMessage
                    For Each slotType In dicslots.Keys
                    With dicslots(slotType)


                        'So are we validating this???
                        Dim vi = iq.ValidationInclusions.Where(Function(vai) vai.Value.MajorCode.ToLower = slotType.MajorCode.ToLower).FirstOrDefault
                        If Not vi.Value Is Nothing Then


                            Dim sum As String = slotType.Translation.text(agentAccount.Language) & Xlt(" Given:", agentAccount.Language) & .Given & Xlt(" Taken:", agentAccount.Language) & .taken
                            If .taken > .Given Then
                                Dim tl As clsTranslation
                                'TLissue

                                'Me.FlexButtonState = EnumHideButton.Up  'Stop them attempting to add more
                                '            'look through every option in the product tree under the system to find those that would offer more slots of this type
                                '(adds ClsValidationMessages to the item)
                                'Do we have any preinstalled slots taken?

                                Dim severity As EnumValidationSeverity = EnumValidationSeverity.RedCross
                                If vi.Value.InclusionType = enumInclusionType.Unvalidated Then 'This one is from paul, a CHK item appears to mean chassis hardware kit which is unvalidated and should result in disclaimer warning....
                                    If I.IsPreInstalled Then Continue For
                                    tl = iq.AddTranslation("Some elements of this quote are unvalidated, please check carefully", English, String.Empty, 0, Nothing, 0, False)
                                    severity = EnumValidationSeverity.Question
                                    'm = New ClsValidationMessage(EnumValidationSeverity.Question, tl, iq.AddTranslation("Unvalidated", English, "ISSUES", 0, Nothing, 0, False), "", 0, 0, {}, st.MajorCode)
                                Else

                                    'Sadly, treat power differently
                                    If slotType.MajorCode = "PWR" Then
                                        If I.SystemItem.Branch.Product.ProductType.Code = "NBK" Then
                                            tl = iq.AddTranslation("Power usage", English, "", 0, Nothing, 0, False)
                                        Else
                                            tl = iq.AddTranslation("Maximum load reached. Upgrade your power supply or remove options", English, String.Empty, 0, Nothing, 0, False)
                                        End If
                                    Else
                                        If .PreInstalledTaken > 0 Then
                                            If slotType.MajorCode = "MEM" AndAlso dicslots.Where(Function(ds) ds.Key.MajorCode = "CPU").Count > 0 AndAlso dicslots.Where(Function(ds) ds.Key.MajorCode = "CPU").Sum(Function(ds) ds.Value.Given) > 1 Then
                                                tl = iq.AddTranslation("Not enough %1 slots available Max: %3 (%2 Used), Add more CPU's to enable more memory slots", English, "ISSUES", 0, Nothing, 0, False)
                                            Else
                                                tl = iq.AddTranslation("Not enough %1 slots available Max: %3 (%2 Used), Remove preinstalled to free more slots", English, "ISSUES", 0, Nothing, 0, False)
                                            End If
                                        Else
                                            tl = iq.AddTranslation("Not enough %1 slots available Max: %3 (%2 Used)", English, "ISSUES", 0, Nothing, 0, False)
                                        End If
                                    End If
                                End If
                                'Do we display capacity or slots?
                                Dim display As String
                                If slotType.MajorCode.ToLower = "pwr" Then
                                    'Special case for power as its a hybrid :(
                                    display = String.Format("{0}W", dicslots(slotType).taken.ToString)
                                ElseIf dicslots(slotType).TotalCapacity <> 0 Then
                                    'Show capacity and units
                                    display = String.Format("{0} {1}", dicslots(slotType).TotalCapacity.ToString(), If(dicslots(slotType).CapacityUnit IsNot Nothing, dicslots(slotType).CapacityUnit.Code, ""))
                                Else
                                    'Show quantity
                                    display = String.Format("x {0}", dicslots(slotType).taken)
                                End If

                                'Do we need to validate power.  If there is no power supply installed and there are no power supplied available under all options then it seems stupid to enforce this, we are talking about blade servers or all in one desktops mainly

                                If slotType.MajorCode.ToUpper = "PWR" AndAlso (I.SystemItem.Branch.Product.ProductType.Code = "NBK" OrElse I.SystemItem.Branch.findSlotGivers(I.SystemItem.Path, iq.i_slotType_Code("PWR").First.Value, False).Count = 0) Then
                                    severity = EnumValidationSeverity.BlueInfo
                                    tl = iq.AddTranslation("Power usage", English, String.Empty, 0, Nothing, 0, False)
                                End If
                                'Not(slotType.MajorCode.ToUpper = "PWR" AndAlso I.Branch.Product.ProductType.Code <> "SVR")
                                Dim skip As Boolean = (slotType.MajorCode.ToUpper = "PWR" AndAlso I.Branch.Product.ProductType.Code <> "SVR") 'oh god more hacking, PWR validation should only be done on servers SVR
                                If Not skip Then
                                    message = New ClsValidationMessage(enumValidationMessageType.Validation, severity, tl, iq.AddTranslation("%1 %4", English, "ISSUES", 0, Nothing, 0, False), String.Empty, 0, 0, {slotType.shortDisplayName(agentAccount.Language), dicslots(slotType).taken.ToString, dicslots(slotType).Given.ToString, display}, slotType.MajorCode)
                                    Dim shortFall As Integer = .taken - .Given
                                    I.resolveOverFlows(slotType, shortFall, buyerAccount, message, errorMessages, lid, dicslots)
                                    I.Msgs.Add(message)
                                End If
                            End If
                        End If
                    End With
                    Next

                    '

                    'Validate minimum/preferred increments...
                    'firstly - get a consolidated count of all items by branch (mostly to group preinstalled and user selected items together)
                    Dim dicItemCount As Dictionary(Of String, Integer)
                    dicItemCount = New Dictionary(Of String, Integer)

                    I.CountItems(dicItemCount)  'NB: Items excluded from validation are not counted !
                    '           I.CompatibleSiblings()

                    'now validate all branches - using the (branch) counts in the dictionary
                    I.ValidateIncrements(dicItemCount, agentAccount, errorMessages)
                    I.validateExclusivity()
                    I.doWarnings(dicslots)

                    'Mop up adding pre-installed which are ok - Removed, spec is being redesigned ML
                    'For Each p In I.Children ' Branch.preInstalled(buyerAccount, I.Path, errorMessages)
                    '    If I.Msgs.Where(Function(msg) msg.slotTypeMajor IsNot Nothing AndAlso msg.slotTypeMajor.ToLower() = p.Branch.Product.ProductType.Code.ToLower()).Count = 0 Then
                    '        If If(p.validate, p.Quantity, 0) > 0 Then
                    '            I.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.greenTick, Nothing, iq.AddTranslation("%1 x %2", English, "ISSUES", 0, Nothing, 0, False), "", 0, 0, Split(p.Branch.Product.ProductType.Translation.text(agentAccount.Language) & "," & If(dicslots.Where(Function(ds) ds.Key.MajorCode.ToLower() = p.Branch.Product.ProductType.Code.ToLower()).Count > 0, dicslots.Where(Function(ds) ds.Key.MajorCode.ToLower() = p.Branch.Product.ProductType.Code.ToLower()).First().Value.taken.ToString(), p.Quantity.ToString()), ",")))
                    '        End If

                    '    End If
                    'Next

                    'Add Power
                    'If I.Msgs.Where(Function(msg) msg.slotTypeMajor IsNot Nothing AndAlso msg.slotTypeMajor.ToLower() = "pwr").Count = 0 Then I.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.greenTick, Nothing, New clsTranslation(English, "Power Consumption " & dicslots(iq.i_slotType_MinorCode("W")).taken.ToString & "W."), "", 0, 0, {}))

                    ' DictSlots is keyed by instances of clsSlotType - which are the 'minor' (more granular) type
                    I.specPanelOpen = SpecUI(lid, I, dicslots, Split("PWR,MEM,CPU,HDD,OPT,PSU", ",").ToList, True, buyerAccount.Language) 'shows the amount of memoy, Watts, CPU's and HDD capacity
                    I.specPanelClosed = SpecUI(lid, I, dicslots, Split("PWR,MEM,CPU,HDD,OPT,PSU", ",").ToList, False, buyerAccount.Language) 'shows the amount of memoy, Watts, CPU's and HDD capacity

                Next



            'Avalanche.. first step is to populate a dictionary of Systems>OPGRef>Qty  (count qualifying options by OPG under each system)
            Dim Qdic As Dictionary(Of clsProduct, Dictionary(Of ClsAvalancheOPG, Integer))
            Qdic = New Dictionary(Of clsProduct, Dictionary(Of ClsAvalancheOPG, Integer))
            Me.RootItem.QualifyAvalanche(Nothing, Qdic, Me.BuyerAccount.SellerChannel.Region)
            'now calculate rebates on qualifying items (starting at the placeholding, branchless quote .rootitem
            Me.RootItem.SetAvalancheRebate(Nothing, Qdic)

            'Flex Attach
            Dim QFDic As Dictionary(Of clsProduct, Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed))) = New Dictionary(Of clsProduct, Dictionary(Of clsFlexOPG, Dictionary(Of clsProductType, clsMinMaxTotalUsed))) 'count the products by type, under each OPG
            'Me.RootItem.QualifyFlex(QFDic, Me.BuyerAccount.SellerChannel.Region, False, 0)
            Dim systemValidationCheck As Boolean = True

            Dim qualifyingProductTypes As Dictionary(Of clsProductType, ClsValidationMessage) = New Dictionary(Of clsProductType, ClsValidationMessage) ' Used to prevent multiple validation messages appearing for the same product type
            Me.RootItem.FlexCalculations(QFDic, Me.BuyerAccount.SellerChannel.Region, 0, systemValidationCheck, qualifyingProductTypes)
            'QFDIC now contains the number of products of each product type -
            ' required (to qualify), present in the basket, and allowed to be discounted  
            ' - plus a 'used' 'working variable'..


            Dim io As List(Of clsFlexOPG) = New List(Of clsFlexOPG) 'insuffient options 
            For Each item In Me.RootItem.Children
                If QFDic.ContainsKey(item.Branch.Product) Then
                    Dim QFLDic = QFDic(item.Branch.Product)

                    For Each opg In QFLDic.Keys.ToList
                        Dim options As Integer = 0

                        Dim hassystem As Boolean = False
                        Dim hasCarePack As Boolean = False
                        For Each pt In QFLDic(opg).Keys
                            If pt.Code = "SVR" Or pt.Code = "SWD" Then  'This needs to be changed to include laptops, desktops and other SYSTEMS
                                hassystem = True
                            Else
                                'Don't count servers as options !
                                options += QFLDic(opg)(pt).Total
                            End If
                        Next

                        If Not hassystem Then
                            QFLDic.Remove(opg)
                        Else
                            If options < opg.MinOptions Then
                                Dim v(1) As String
                                v(0) = CStr(opg.MinOptions)
                                v(1) = CStr(options)
                                Dim vmtl As clsTranslation
                                If options = 0 Then
                                    vmtl = iq.AddTranslation("%1 Qualifying options", English, "VM", 0, Nothing, 0, False)
                                Else

                                    vmtl = iq.AddTranslation("%1 Qualifying options (%2 selected) ", English, "VM", 0, Nothing, 0, False)
                                End If

                                item.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesntQualify, Nothing, vmtl, String.Empty, 0, 0, v))
                                systemValidationCheck = False
                            Else
                                Dim v(1) As String
                                v(0) = CStr(opg.MinOptions)
                                v(1) = CStr(options)
                                Dim vmtl As clsTranslation

                                vmtl = iq.AddTranslation("%1 Qualifying options", English, "VM", 0, Nothing, 0, False)
                                item.Msgs.Add(New ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, Nothing, vmtl, "", 0, 0, v))

                            End If
                        End If
                    Next
                End If
            Next

            'remove those OPG's for which we don't have sufficient options
            'For Each opg In io  'insuffient options 
            ' QFDic.Remove(opg)
            ' Next
            For Each item In Me.RootItem.Children
                If item.Branch.Product.isSystem Then
                    If QFDic.ContainsKey(item.Branch.Product) Then
                        If calcTotalRebate Then
                            item.SetFlexRebate(QFDic(item.Branch.Product), False, 0, systemValidationCheck, Me.TotalRebate)
                        Else
                            item.SetFlexRebate(QFDic(item.Branch.Product), False, 0, systemValidationCheck, 0)
                        End If
                    End If
                End If
            Next
            '  Me.RootItem.SetFlexRebate(QFDic)
        End SyncLock

    End Sub


    Public Sub addPreinstalledRecursive(ByVal quoteItem As clsQuoteItem, ByVal branch As clsBranch, ByVal path As String, withAutoAdds As Boolean, ByRef errorMessages As List(Of String))

        'This is one of the trickiest subs to get your head around.
        'It adds 'sub' items (to the quoteitem (typically, but not neccessarily a system) with their pre-installed quanities

        'AutoAdds are *chargable* (non Free Of Charge) items

        'quoteItem is the Item (typically system) we just added to the quote
        'Recursively creates child QuoteItems according to the default Quantities set out in the descendant branches of the (supplied) branch at the (supplied) path
        '(remember branches are not unique or distinct in the tree - only the path fully qualifies the quantity record (of the branch) we need to use.
        'NOTE: - this sub DOES NOT add a preinstalled quanitity to the quote Item - It adds 'sub' items (to the item) with pre-installed quanities

        'Also - don't be tempted to try and rewrite this to be a method on the quoteitem - you cannot recurse child items that do not (yet) exist.. so the 'obvious' structure doesn't work.

        Dim iPath As String
        Dim q As clsQuantity
        Dim childitem As clsQuoteItem
        Dim addedItem As Boolean = False

        Dim finished As Boolean = False

        For Each b In branch.childBranches.Values  'These are the child branches of the BRANCH in the product catalogue (not the quoteItem!)
            iPath = path & "." & Trim$(b.ID.ToString)

            'new nick 26/03/2015

            '  If b.deleted Then Exit Sub

            Dim recurse As Boolean = True
            If b.PruneInForce(iPath, quoteItem.quote.BuyerAccount.SellerChannel) > 0 Then
                recurse = False '  Exit Sub
            End If




            '  Debug.Print(iPath)

            ' was just 'region' (which wasn't valid)
            q = b.LocalisedQuantity(BuyerAccount.SellerChannel.Region, iPath$, errorMessages) 'returns the 'best' quantity record for this user - ie, the deepest, narrowest match

            'If branch.Product IsNot Nothing Then
            '    If branch.Product.sku = "748922-B21" Then Stop
            'End If

            ' If q IsNot Nothing Then
            ' If q.NumPreInstalled > 0 Then Stop
            ' End If

            If b.Product Is Nothing And Not q Is Nothing Then

                errorMessages.Add("* A (preinstalled) quantity record for the branch " & b.DisplayName(English) & " at path " & iPath & " was found - but this is a product-less branch !")
            Else
                ' :BUG - InsertItemPosition is using quanity record that don't matcc on path

                Dim isPreInstalled As Boolean = False
                addedItem = False
                childitem = quoteItem ' by default, we'll pass the same quoteitem down - until a new quoteitem is created
                If Not q Is Nothing Then
                    Dim skuvariant As clsVariant = Nothing
                    Dim listPrice As NullablePrice
                    Dim price As NullablePrice
                    ' If q.FOC Or withAutoAdds Then

                    'For the options we're autoadding - locate the best match against the quotitem (system) we're adding it to
                    'for example, get a french keyboard, from the same warehouse asys the sytem unit - for systems in france
                    skuvariant = b.Product.MatchingVariant(quoteItem.SKUVariant, BuyerAccount.SellerChannel)

                    'If skuvariant Is Nothing Then Stop - if there's no list price for something - skuvariant will be nothing
                    If skuvariant Is Nothing Then
                        price = New NullablePrice(BuyerAccount.Currency) ' 
                        listPrice = New NullablePrice(BuyerAccount.Currency) ' 
                        ' errorMessages.Add("Preinstalled SKU variant was nothing (no list price )")
                    Else
                        listPrice = NullPrice(skuvariant.Product.ListPrice(BuyerAccount), BuyerAccount.Currency)
                        price = skuvariant.Price(BuyerAccount) '.Price
                    End If

                    'Else
                    '    listPrice = New nullablePrice(BuyerAccount.Currency)
                    ' End If


                    If q.NumPreInstalled > 0 Then
                        'creates the new child item and adds it to the item
                        ' Preinstalled items are FOC (free of charge)
                        'Dim skuvariant As clsVariant = iq.i_variant_code("") 'TODO - May need further work - Installs only 'standard' FIO's 

                        Dim buyerAccount As clsAccount
                        buyerAccount = quoteItem.quote.BuyerAccount

                        Dim g As String = b.Product.DisplayName(English)
                        If q.FOC Then 'AndAlso skuvariant IsNot Nothing Then
                            If skuvariant Is Nothing Then
                                skuvariant = New clsVariant("", b.Product, buyerAccount.SellerChannel, b.Product.sku, "", "", "", iq.i_region_code("XW"), False) ' iq.AllVariants ' New clsVariant() With {.DistiSku = "1234", .Product = b.Product, .Region = iq.i_region_code("XW"), .ID = -1, .i_prices = New Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))()}
                            End If
                            ' Dim zeroPrice As NullablePrice = New NullablePrice(CSng(0), quoteItem.quote.BuyerAccount.Currency, False)
                            If skuvariant IsNot Nothing Then price = skuvariant.Price(buyerAccount)
                            isPreInstalled = True  'Free items are preinstalled (in the sense that they cannot be removed from the basket)
                        End If
                    Else
                        isPreInstalled = False 'non free items - are allowed to be removed

                        'for this quote item - this childbranch has a preinstalled quantity - find the best variant match
                        'so that, for example if you pick a french server with an autoadded keyboard, it will add a french keyboard
                        'and/or so that any (non-free) auto adds come from the same warehouse
                        If skuvariant IsNot Nothing Then
                            price = skuvariant.Price(BuyerAccount) '.Price
                        End If
                    End If


                    'synnex fix
                    If Not price.isValid Then
                        price = listPrice
                    End If

                    If skuvariant Is Nothing AndAlso Not b.Translation.text(English).StartsWith("###") Then
                        'could not locate a variant to auto add (no list price)
                        '   errorMessages.Add("* Could not locate a variant to auto add (no list price?)")
                    Else
                        If b.Product.ProductType.Code.ToUpper = "WTY" Then
                            Dim a = 8
                        End If
                        If q.NumPreInstalled > 0 Then  'Important (becuase the quantity may only specify max's and limits and NOT be an 'auto-add' per-se
                            Dim order As Integer = 3
                            If isPreInstalled Then order = 2 'FIOS should some above autoaddes
                            If isPreInstalled Or withAutoAdds Then
                                childitem = New clsQuoteItem(quoteItem.quote, b, skuvariant, iPath, q.NumPreInstalled, price, listPrice, isPreInstalled, quoteItem, New nullableString(), New nullableString(), 0, 1, New nullableString, order)
                            End If

                        End If
                    End If

                End If

                'childitem = New clsQuoteItem(quoteItem.quote, b, skuvariant, iPath, q.NumPreInstalled * qty, zeroprice, zeroprice, True, quoteItem, New NullableInt(), 0, Created, 0)
                'pass the new child down, with the next branch

                If recurse Then
                    If childitem Is Nothing Then
                        errorMessages.Add("* Child item was nothing in AddPreinstalledRecursive")
                    Else
                        addPreinstalledRecursive(childitem, b, iPath, withAutoAdds, errorMessages)
                        addedItem = True
                    End If

                    If Not addedItem Then                'pass myself down (for the attachment of subsequent children), with the next(deeper) branch
                        addPreinstalledRecursive(quoteItem, b, iPath, withAutoAdds, errorMessages)
                    End If
                End If
            End If
        Next

    End Sub
    ''' <summary>
    ''' added in Me.TotalRebate check  because rebate is not take off price,  have now called validate, before this so rebate is worked out.The rebate is stored indivdually against each item in the quoteitem table when qulaifyling rules apply. 
    ''' </summary>
    ''' <param name="recalculatePrice">optional boolean value true/ false.</param>
    ''' <remarks></remarks>
    Public Sub UpdateDescAndPrice(Optional recalculatePrice As Boolean = True)

        If Not (Me.Saved And Me.Locked) Then
            Pmark("Quote.updateDescAndPrice")

            'creates the rolled up description/headline value for the Quote - descriptions
            'needs to recurse
            If Not Me.RootItem Is Nothing Then
                Dim summary As String
                summary = Me.RootItem.summarise(0)
                'replace the trailing comma with a full stop.
                If Len(summary) > 0 Then Mid$(summary, Len(summary), 1) = "."
                Me.Description = New nullableString(summary)
            End If

            'Me.Value is a NullablePrice 
            If recalculatePrice Then
                Me.QuotedPrice = New NullablePrice(Me.Currency)
                Me.QuotedPrice.isValid = True
                Me.TotalRebate = 0

                Dim tPrice As New NullablePrice(Me.Currency)
                tPrice.isValid = True
                'Me.RootItem.Totalise(Me.QuotedPrice, CDec(Me.TotalRebate), True) ' recurses all items to find a total cost - which also has a 'valid' member 'and isList' memeber
                Me.RootItem.Totalise(tPrice, Me.TotalRebate, True) ' recurses all items to find a total cost - which also has a 'valid' member 'and isList' memeber
                If tPrice.isTotal Then
                    tPrice.isValid = True
                End If

                Me.QuotedPrice = tPrice
                If Me.QuotedPrice.isList Then Me.QuotedPrice.isTotal = True ' Change the tooltip to stay 'includes list price elements'
            End If

            Pacc("Quote.updateDescAndPrice")
        End If


    End Sub

    Public Sub Update(Optional con As SqlClient.SqlConnection = Nothing, Optional recalculatePrice As Boolean = True)

        UpdateDescAndPrice(recalculatePrice)

        'note: we don't need to sqlencode then nullablestring types (it's done in their SqlValue method)
        'note: the quote.price can be null (when there are no quote items)
        SQLUpdate()

    End Sub


    Public Function LoadItems(ByRef errormessages As List(Of String)) As Integer

        '(re) creates the quotes RootItem - and then loads and attaches the heriarchy of quote items

        'the QuoteItems created timestamp is used to order them - so that when we re-constitute the tree - the parent items will alway be there
        Dim sql As String = "SELECT id,fk_branch_id,path,quantity,opg,bundle,rebate,fk_QuoteItem_Id_parent,price,listprice,ispreinstalled,fk_variant_id,created,margin,note,[order],validate"
        sql &= " FROM quoteItem WHERE FK_Quote_ID=" & Me.ID & " order by Created"

        Dim count As Integer

        Using con As SqlClient.SqlConnection = da.OpenDatabase()

            Using rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)

                Dim QiID As Integer
                Dim Branch As clsBranch
                Dim path As String
                Dim Qty As Integer
                Dim price As NullablePrice
                Dim listprice As NullablePrice

                Dim IsPreinstalled As Boolean
                Dim Parent As clsQuoteItem = Nothing
                Dim opg As nullableString

                Dim created As DateTime

                Me.RootItem = New clsQuoteItem(Me)

                Dim anitem As clsQuoteItem

                Dim flatitems As Dictionary(Of Integer, clsQuoteItem)
                flatitems = New Dictionary(Of Integer, clsQuoteItem)

                Dim bundle As nullableString 'this and OPG are just (optional) text fields which appea on the quote
                If rdr.HasRows Then
                    Me.TotalRebate = 0
                End If

                While rdr.Read

                    QiID = CInt(rdr.Item("id"))
                    Branch = iq.Branches(CInt(rdr.Item("fk_branch_id")))
                    path = CStr(rdr.Item("Path"))
                    Qty = CInt(rdr.Item("Quantity"))

                    Dim skuVariant As clsVariant = Nothing

                    Dim vid As Integer = CInt(rdr.Item("fk_variant_id"))
                    If iq.Variants.ContainsKey(vid) Then
                        skuVariant = iq.Variants(vid) '   Branch.Product.i_Variants(Me.BuyerAccount.SellerChannel)(rdr.Item("fk_variant_id"))

                        'recreate the quote items in the same currency as the quote
                        Dim isListPrice As Boolean = (skuVariant.sellerChannel Is HP)
                        price = New NullablePrice(rdr.Item("price"), Me.Currency, isListPrice)
                        listprice = New NullablePrice(rdr.Item("listprice"), Me.Currency, True)
                        created = CDate(rdr.Item("created"))

                        opg = New nullableString(rdr.Item("opg"))
                        bundle = New nullableString(rdr.Item("bundle"))

                        Dim rebate As Decimal = CDec(rdr.Item("rebate"))
                        Me.TotalRebate += rebate
                        IsPreinstalled = CBool(rdr.Item("isPreinstalled"))

                        If IsDBNull(rdr.Item("fk_quoteItem_id_parent")) Then
                            Parent = Me.RootItem
                        Else
                            If flatitems.ContainsKey(CInt(rdr.Item("fk_quoteitem_id_parent"))) Then
                                Parent = flatitems(CInt(rdr.Item("fk_quoteitem_id_parent")))
                            Else
                                Beep()
                            End If
                        End If

                        anitem = New clsQuoteItem(CInt(rdr.Item("id")), Me, Branch, skuVariant, path$, Qty, price, listprice, IsPreinstalled, Parent, opg, bundle, rebate, created, CSng(rdr.Item("margin")), New nullableString(rdr.Item("note")), CInt(rdr.Item("order")), CBool(rdr.Item("validate")))

                        flatitems.Add(CInt(rdr.Item("id")), anitem)
                        count += 1

                    Else
                        errormessages.Add("* Variant " & vid & " was missing or not loaded")

                    End If

                End While

                Me.UpdateDescAndPrice()

                rdr.Close()
            End Using
            con.Close()
        End Using

        Return count

    End Function


    Public Function ListRow(Quote As clsQuote, css() As String, lid As UInt64, islatest As Boolean, ByRef errorMessages As List(Of String)) As TableRow

        Dim buyerAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim language = CType(iq.sesh(lid, "AgentAccount"), clsAccount).Language
        'Quote.validate(lid, buyerAccount, AgentAccount, errorMessages)
        If Not Quote.QuotedPrice.isValid Or Quote.QuotedPrice.NumericValue = 0 Then
            Quote.UpdateDescAndPrice()
        End If


        Dim previewpanel As Panel = New Panel
        previewpanel.ID = "pv" & Quote.ID & "-" & Quote.Version

        Dim tr As TableRow = New TableRow()
        tr.Attributes("onclick") = "suck('" & "quotesummary.aspx?quoteid=" & Quote.ID & "','" & previewpanel.ID & "');  popDialog('" & previewpanel.ID & "', 'Quote " & Quote.RootQuote.ID & " Version " & Quote.Version & "');"
        tr.CssClass = "quotesTableRow "
        tr.ID = "Q" & Me.ID.ToString

        Dim tc = New TableCell()

        Dim isExpanded As Boolean = False
        If iq.SeshContains(lid, "expandedQuotes") Then
            'If Me.AgentAccount.Quotes.ContainsKey(Me.RootQuote.ID) Then
            If CType(iq.sesh(lid, "expandedQuotes"), List(Of Integer)).Contains(Me.RootQuote.ID) Then isExpanded = True
            ' End If
        End If
        tc = New TableCell()
        If islatest Then
            If isExpanded Then
                tc.Controls.Add(MakeRoundButton("minus.png", "Show only the latest version of this quote", "burstBubble(event);var savedP=false;  savedP= $('#SavedPanel').attr('aria-expanded'); showVersion('manipulation.aspx?command=CollapseQuoteVersions&RQID=" & Quote.RootQuote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
            Else
                If Me.AgentAccount.MaxQuoteVersion(RootQuote) > 1 Then
                    tc.Controls.Add(MakeRoundButton("plus.png", "Show all versions of this quote", "burstBubble(event);var savedP=false;  savedP= $('#SavedPanel').attr('aria-expanded');showVersion('manipulation.aspx?command=ExpandQuoteVersions&RQID=" & Quote.RootQuote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
                End If
            End If
            tr.CssClass &= "latestVersion"
        Else
            tr.CssClass &= "oldVersion"
        End If
        tr.Cells.Add(tc)

        If Not tr.CssClass.Contains("oldVersion") Then tr.Cells.Add(New TableCell() With {.CssClass = "quotesTableCell center", .Text = Quote.RootQuote.ID.ToString}) Else tr.Cells.Add(New TableCell())

        tc = New TableCell() With {.CssClass = "quotesTableCell"}
        tr.Cells.Add(tc)
        tr.Cells(tr.Cells.Count - 1).Controls.Add(NewLit(Quote.Version.ToString & If(Quote.Locked, "<div class='lockIcon'/>", "")))

        If Me.Saved Then tr.Cells.Add(New TableCell() With {.ID = "quotesList1Col-Name", .CssClass = "quotesTableCell", .Text = Quote.Name.DisplayValue()})

        If Me.Saved Then tr.Cells.Add(New TableCell() With {.CssClass = "quotesTableCell", .Text = "<img class='unpressedButton' alt='" & Xlt("Rename", language) & "' title='" & Xlt("Rename", language) & "' src='/images/navigation/pencil.png' onclick='burstBubble(event);renameQuote(" & Me.ID & ");return false;'/>"})

        'Console.WriteLine(myDateTime.ToString("d", CultureInfo.CreateSpecificCulture("ro-RO")))

        'This sulture should move to the account really
        tr.Cells.Add(New TableCell() With {.CssClass = "quotesTableCell", .Text = Quote.Updated.ToString("d", CultureInfo.CreateSpecificCulture(buyerAccount.Culture.Code))})
        'tr.Cells.Add(New TableCell() With {.CssClass = "quotesTableCell", .Text = Quote.Updated.ToShortDateString()})

        If Me.Saved Then tr.Cells.Add(New TableCell() With {.CssClass = "quotesTableCell", .Text = Quote.State.Displayname(language)})

        If Me.Saved Then tr.Cells.Add(New TableCell() With {.CssClass = "quotesTableCell center", .Text = Quote.ExportHistory(True).Count.ToString})
        If Me.Saved Then tr.Cells(tr.Cells.Count - 1).Controls.Add(NewLit(If(Quote.ExportHistory(True).Count > 0, "<span class='spanLink' onclick=""burstBubble(event);showDialog('ExportedQuotes.aspx?quoteID=" & Me.ID & "&lid=" & lid & "');"">" & Quote.ExportHistory(True).Count.ToString & "</span>", Quote.ExportHistory(True).Count.ToString)))
        'New Nullable price with Quote.Quotedprice - quote.TotalRebate
        Dim finalprice As NullablePrice = New NullablePrice(Quote.QuotedPrice.NumericValue - Quote.TotalRebate, Quote.Currency, False) ' destroy blue price with false on purpose.
        finalprice.isTotal = True

        tc = New TableCell() With {.CssClass = "quotesTableCell"}
        tc.Controls.Add(finalprice.DisplayPrice(buyerAccount, errorMessages)) 'if valid only --- need to add
        tr.Cells.Add(tc)

        'Add tool buttons....

        tc = New TableCell()
        If Quote.State.code <> "#WN" Then tc.Controls.Add(MakeRoundButton("tick.png", Xlt("Mark as won", language), "burstBubble(event);var savedP=false;  savedP= $('#SavedPanel').attr('aria-expanded');showVersion('manipulation.aspx?command=MarkAsWon&QID=" & Quote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
        tr.Cells.Add(tc)


        tc = New TableCell()
        If Quote.State.code <> "#WN" Then tc.Controls.Add(MakeRoundButton("close.png", Xlt("Discard this quote", language), "burstBubble(event); savedP= $('#SavedPanel').attr('aria-expanded');showVersion('manipulation.aspx?command=DiscardQuote&QID=" & Quote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
        tr.Cells.Add(tc)

        tc = New TableCell()
        tc.Controls.Add(MakeRoundButton("excl.png", Xlt("Export", language), "burstBubble(event);showMenu(" & Me.ID.ToString & ");return false;", " quoteButton", "", Quote.AgentAccount.Language))
        tc.Controls.Add(NewLit("<div onclick=""burstBubble();return false;"">" & "<div id = ""exportMenu" & Me.ID.ToString & """  class = ""submenu"" > " & _
                 "<a class=""account""> " & Xlt("Export Option", AgentAccount.Language) & "s</a> <ul class=""root"" >" & _
                 "<li><a onclick = ""burstBubble(event);$('.submenu').hide();$('#spinnerContainer').show() ;rExec('Quote.aspx?cmd=Excel&QuoteId=" & Me.ID & "&quoteName=" & Me.Name.value.ToString() & "',  downloadFile); return false;"" href=""#"">" & Xlt("Excel", AgentAccount.Language) & "</a></li>" & _
                 "<li><a onclick = ""burstBubble(event); $('.submenu').hide();$('#spinnerContainer').show();rExec('Quote.aspx?cmd=PDF&QuoteId=" & Me.ID & "&quoteName=" & Me.Name.value.ToString() & "',  downloadFile); return false;"" href=""#"">" & Xlt("PDF", AgentAccount.Language) & "</a></li>" & _
                 "<li><a href=""#"" onclick = ""burstBubble(event); $('.submenu').hide();$('#spinnerContainer').show() ;rExec('Quote.aspx?cmd=XML&QuoteId=" & Me.ID & "&quoteName=" & Me.Name.value.ToString() & "',  downloadFile); return false;"">" & Xlt("XML", AgentAccount.Language) & "</a></li>" & _
                 "<li><a href=""#"" onclick = ""burstBubble(event); $('.submenu').hide();$('#spinnerContainer').show() ;rExec('Quote.aspx?cmd=XMLAdv&QuoteId=" & Me.ID & "&quoteName=" & Me.Name.value.ToString() & "',  downloadFile); return false;"">" & Xlt("XML Advanced", AgentAccount.Language) & "</a></li>" & _
                 "<li><a href=""#"" onclick = ""burstBubble(event); $('.submenu').hide();$('#spinnerContainer').show() ;rExec('Quote.aspx?cmd=XMLSmartQuote&QuoteId=" & Me.ID & "&quoteName=" & Me.Name.value.ToString() & "',  downloadFile); return false;"">" & Xlt("XML SmartQuote", AgentAccount.Language) & "</a></li></ul>" & _
                 " </div></div> "))

        'tc.Controls.Add(MakeRoundButton("excl.png", "Export to PDF", "burstBubble(event);rExec('Quote.aspx?cmd=PDF&QuoteId=" & Me.ID & "&quoteName=" & Me.Name.value.ToString() & "',  downloadFile);return false;", "", "", Quote.AgentAccount.Language))
        tr.Cells.Add(tc)

        'Not nice, would like to redo this....

        tc = New TableCell()
        tc.Controls.Add(previewpanel)
        tr.Cells.Add(tc)

        Return tr
    End Function

    Sub dummy(Quote As clsQuote, css() As String, lid As UInt64, islatest As Boolean, ByRef errorMessages As List(Of String))
        Dim tr As Panel
        Dim tc As Panel
        Dim lbl As Label

        Dim buttons As Panel

        tr = New Panel
        tr.ID = "Q" & Quote.ID 'give the row an id so we can hide it (when discarding)

        'Dim H$ = "ID,Version,Name,Customer,Supplier,Systems,Updated,Status,Value!"

        'ID
        tc = New Panel
        tc.CssClass = css(0)
        lbl = New Label
        tc.Controls.Add(lbl)
        lbl.Text = Quote.RootQuote.ID.ToString
        tr.Controls.Add(tc)

        'Version
        tc = New Panel
        lbl = New Label
        tc.Controls.Add(lbl)
        tc.CssClass = css(1)
        lbl.Text = Quote.Version.ToString
        tr.Controls.Add(tc)

        'Name
        tc = New Panel
        tc.CssClass = css(2)
        'Dim aLit As Literal
        ' aLit = New Literal
        ' aLit.Text = "<span onClick=""alert('hello';)>"""
        ' aLit.Text &= "NM:" & Quote.Name.DisplayValue
        ' aLit.Text &= "</span>"
        ' tc.Controls.Add(aLit)

        lbl = New Label
        '       If Not Quote.Name.value Is DBNull.Value Then Stop

        lbl.Text = Quote.Name.DisplayValue & IIf(Quote.Locked, "*", "").ToString() & "<img class='unpressedButton' style='vertical-align:center;width:1em;height:1em;' alt='Rename' title='Rename' src='/images/navigation/pencil.png' onclick='burstBubble(event);renameQuote(" & Me.ID & ");return false;'/>"
        tc.Controls.Add(lbl)

        '   lbl.Attributes("onClick") = "alert('hello';)>"""
        tr.Controls.Add(tc)


        'Customer
        tc = New Panel
        tc.CssClass = css(3)
        lbl = New Label
        tc.Controls.Add(lbl)
        lbl.Text = Quote.BuyerAccount.BuyerChannel.Name
        tr.Controls.Add(tc)

        'Supplier
        tc = New Panel
        tc.CssClass = css(4)
        lbl = New Label
        tc.Controls.Add(lbl)
        lbl.Text = Quote.BuyerAccount.SellerChannel.Name
        tr.Controls.Add(tc)

        ''Systems
        'tc = New Panel
        'tc.CssClass = css(5)
        'lbl = New Label
        'tc.Controls.Add(lbl)
        'lbl.Text = Xlt("sys:" & Quote.Description.DisplayValue, Quote.BuyerAccount.Language)
        'tr.Controls.Add(tc)

        ''Options
        'tc = New Panel
        'tc.CssClass = css(6)
        'lbl = New Label
        'tc.Controls.Add(lbl)
        'lbl.Text = Quote.NumOptions
        'tr.Controls.Add(tc)

        'Updated
        tc = New Panel
        tc.CssClass = css(5)
        lbl = New Label
        tc.Controls.Add(lbl)
        lbl.Text = Quote.Created.ToShortDateString ' & " " & Quote.Created.ToShortTimeString
        tr.Controls.Add(tc)

        'Status
        tc = New Panel
        tc.CssClass = css(6)
        lbl = New Label
        lbl.Text = Quote.State.code
        tc.Controls.Add(lbl)
        tr.Controls.Add(tc)

        'Value
        tc = New Panel
        tc.CssClass = css(7)
        If Not Quote.QuotedPrice.isValid Or Quote.QuotedPrice.NumericValue = 0 Then Quote.UpdateDescAndPrice()
        If Not Quote.QuotedPrice Is Nothing Then
            tc.Controls.Add(Quote.QuotedPrice.DisplayPrice(Quote.BuyerAccount, errorMessages)) 'NB: the price has a currency but the buyeraccounts culture telss us how to format the number
            tr.Controls.Add(tc)
        End If

        '!Buttons"
        buttons = New Panel
        buttons.CssClass = "quotesListCol-Buttons"  ' {width:7em;float:left;min-height:1px;}
        buttons.Controls.Add(MakeRoundButton("tick.png", "Mark as won", "var savedP=false;  savedP= $('#SavedPanel').attr('aria-expanded');showVersion('manipulation.aspx?command=MarkAsWon&QID=" & Quote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
        buttons.Controls.Add(MakeRoundButton("close.png", "Discard this quote", "var savedP=false;  savedP= $('#SavedPanel').attr('aria-expanded');showVersion('manipulation.aspx?command=DiscardQuote&QID=" & Quote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
        buttons.Controls.Add(MakeRoundButton("copy.png", "Create a new quote using this template", "var savedP=false;  savedP= $('#SavedPanel').attr('aria-expanded');showVersion('manipulation.aspx?command=CopyQuote&QID=" & Quote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
        buttons.Controls.Add(MakeRoundButton("excl.png", "Export to PDF", "burstBubble(event);rExec('Quote.aspx?cmd=PDF&QuoteId=" & Me.ID & "&quoteName=" & Me.Name.value.ToString() & "',  downloadFile);return false;", "", "", Quote.AgentAccount.Language))


        Dim previewpanel As Panel = New Panel
        previewpanel.ID = "pv" & Quote.ID & "-" & Quote.Version
        'ctl00_MainContent_
        buttons.Controls.Add(MakeRoundButton("pencil.png", " Edit this quote", "suck('" & "quotesummary.aspx?quoteid=" & Quote.ID & "','" & previewpanel.ID & "');  popDialog('" & previewpanel.ID & "');", "", "", Quote.AgentAccount.Language))

        Dim isExpanded As Boolean = False
        If iq.SeshContains(lid, "expandedQuotes") Then
            'If Me.AgentAccount.Quotes.ContainsKey(Me.RootQuote.ID) Then
            If CType(iq.sesh(lid, "expandedQuotes"), List(Of Integer)).Contains(Me.RootQuote.ID) Then isExpanded = True
            ' End If
        End If

        If islatest Then
            If isExpanded Then
                buttons.Controls.Add(MakeRoundButton("minus.png", "Show only the latest version of this quote", "var savedP=false;  savedP= $('#SavedPanel').attr('aria-expanded'); showVersion('manipulation.aspx?command=CollapseQuoteVersions&RQID=" & Quote.RootQuote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
            Else
                If Me.AgentAccount.MaxQuoteVersion(RootQuote) > 1 Then
                    buttons.Controls.Add(MakeRoundButton("plus.png", "Show all versions of this quote", "var savedP=false;  savedP= $('#SavedPanel').attr('aria-expanded');showVersion('manipulation.aspx?command=ExpandQuoteVersions&RQID=" & Quote.RootQuote.ID & "', savedP);", "", "", Quote.AgentAccount.Language))
                End If
            End If
            tr.CssClass &= "latestVersion"
        Else
            tr.CssClass &= "oldVersion"
        End If
        buttons.Controls.Add(MakeRoundButton("eye.png", "Show history of quote export", "showDialog('ExportedQuotes.aspx?quoteRootID=" & Me.RootQuote.ID & "&lid=" & lid & "' )", "", "", Quote.AgentAccount.Language))

        tr.Controls.Add(buttons)
        tr.Controls.Add(previewpanel)

    End Sub

    Public Function SQLInsert(Optional BootStrap As Boolean = False) As Integer Implements ISqlBasedObject.SQLInsert
        Dim sql As New StringBuilder(String.Empty)
        If BootStrap Then 'the very first quote
            sql.AppendFormat("{0}", "set identity_insert [quote] ON;")
            sql.AppendFormat("{0}", "INSERT INTO QUOTE (id,FK_Account_Id_agent,FK_Account_id_buyer,fk_quote_id_root,created,updated,version,fk_state_id,price,fk_currency_id,locked,hidden,saved,reference,fk_import_id,totalrebate) ")
            sql.AppendFormat("{0}", " VALUES (1,")
            sql.AppendFormat("{0},", AgentAccount.ID)
            sql.AppendFormat("{0},", BuyerAccount.ID)
            sql.AppendFormat("{0},", 1)
            sql.AppendFormat("{0},", da.UniversalDate(Created))
            sql.AppendFormat("{0},", da.UniversalDate(Updated))
            sql.AppendFormat("{0},", Version)
            sql.AppendFormat("{0},", State.ID)
            sql.AppendFormat("{0},", Me.QuotedPrice.sqlvalue)
            sql.AppendFormat("{0},", Currency.ID)
            sql.AppendFormat("{0},", If(Locked, "1", "0"))
            sql.AppendFormat("{0},", If(Hidden, "1", "0"))
            sql.AppendFormat("{0},", If(Saved, "1", "0"))
            sql.AppendFormat("{0},", da.SqlEncode(Reference))
            sql.AppendFormat("{0},", FK_Import_Id)
            sql.AppendFormat("{0}", Me.TotalRebate)
            sql.AppendFormat("{0}", "); set identity_insert [quote] OFF;")
        Else
            'a 'normal' (non - bulk inserted) quote

            sql.AppendFormat("{0}", "INSERT INTO QUOTE (FK_Account_Id_agent,FK_Account_id_buyer,fk_quote_id_root,created,updated,version,name,fk_state_id,price,fk_currency_id,locked,hidden,saved,reference,fk_import_ID,totalrebate) ")
            sql.AppendFormat("{0}", "VALUES (")
            sql.AppendFormat("{0},", AgentAccount.ID)
            sql.AppendFormat("{0},", BuyerAccount.ID)
            sql.AppendFormat("{0},", "(SELECT IDENT_CURRENT ('Quote'))")
            sql.AppendFormat("{0},", da.UniversalDate(Created))
            sql.AppendFormat("{0},", da.UniversalDate(Updated))
            sql.AppendFormat("{0},", Version)
            sql.AppendFormat("{0},", Name.sqlValue)
            sql.AppendFormat("{0},", State.ID)
            sql.AppendFormat("{0},", Me.QuotedPrice.sqlvalue)
            sql.AppendFormat("{0},", Currency.ID)
            sql.AppendFormat("{0},", If(Locked, "1", "0"))
            sql.AppendFormat("{0},", If(Hidden, "1", "0"))
            sql.AppendFormat("{0},", If(Saved, "1", "0"))
            sql.AppendFormat("{0},", da.SqlEncode(Reference))
            sql.AppendFormat("{0},", FK_Import_Id)
            sql.AppendFormat("{0}", Me.TotalRebate)
            sql.AppendFormat("{0}", ")")
        End If
        SQLInsert = da.DBExecutesql(sql.ToString, True, Me)

        '        sql$ = "UPDATE quote SET fk_quote_id_root=" & Me.RootQuote.ID & ",version=" & Version.ToString & " WHERE ID=" & Me.ID
        '       da.DBExecutesql(sql$, False, Me)
    End Function
    Public Sub SQLUpdate() Implements ISqlBasedObject.SQLUpdate

        Dim desc As String = Me.Description.DisplayValue
        If Len(desc) > 450 Then desc = Left(desc, 450) & "...(truncated)"

        Dim sql As New StringBuilder(String.Empty)
        sql.AppendFormat("{0}{1}", "UPDATE Quote SET FK_Account_Id_agent=", AgentAccount.ID)
        sql.AppendFormat("{0}{1}", ",FK_Account_id_buyer=", BuyerAccount.ID)
        sql.AppendFormat("{0}{1}", ",FK_State_ID=", State.ID)
        sql.AppendFormat("{0}{1}", ",created=", da.UniversalDate(Me.Created))
        sql.AppendFormat("{0}{1}", ",hidden=", If(Hidden, "1", "0"))
        sql.AppendFormat("{0}{1}", ",locked=", If(Locked, "1", "0"))
        sql.AppendFormat("{0}{1}", ",saved=", If(Saved, "1", "0"))
        sql.AppendFormat("{0}{1}", ",description=", da.SqlEncode(desc))
        sql.AppendFormat("{0}{1}", ",price=", Me.QuotedPrice.sqlvalue)
        sql.AppendFormat("{0}{1}", ",name=", Me.Name.sqlValue)
        sql.AppendFormat("{0}{1}", ",fk_quote_id_root=", Me.RootQuote.ID)
        sql.AppendFormat("{0}{1}", ",totalrebate=", Me.TotalRebate)
        sql.AppendFormat("{0}{1}", " WHERE ID=", Me.ID)
        da.DBExecutesql(sql.ToString, False, Me)
    End Sub
    Public Sub UpdateSelfAfterIdChange(NewId As Integer) Implements ISqlBasedObject.UpdateSelfAfterIdChange
        'Update OM
        iq.Quotes.Remove(Me.ID)
        Me.ID = NewId
        iq.Quotes.Add(NewId, Me)

        For Each c In Me.RootItem.Children
        Next


    End Sub

    Public Function KeywordExists(search As String) As Boolean



        Dim errorMessages As List(Of String) = New List(Of String)
        Me.LoadItems(errorMessages)
        Dim item As clsQuoteItem

        For Each item In Me.RootItem.Children
            Me.keyword += item.Branch.Product.sku & " , "
            If item.Children.Count > 0 Then
                For Each child In item.Children
                    Me.keyword += child.Branch.Product.sku & ","
                Next
            End If
        Next
        Me.keyword = UCase(Me.keyword)
        If Me.keyword.Contains(UCase(search)) Then
            Return True
        Else
            Return False
        End If



    End Function


#Region "QuoteFunctions"
    Public Function MarkAsWon(lid As UInt64) As List(Of String)
        MarkAsWon = New List(Of String)()
        If Not Me.Saved Then
            Me.Save(DefaultName, lid)
        End If
        Me.LoadItems(MarkAsWon)
        Me.Locked = True
        Me.State = iq.i_state_GroupCode("QT-#WN")  'mark as Won
        Me.Update()
    End Function
    Public ReadOnly Property DefaultName As String
        Get
            Return Me.AgentAccount.displayName(English) + DateTime.Today.ToShortDateString()
        End Get
    End Property


    Public Function Save(quoteName As String, lid As UInt64) As String

        'should be invisible really (until we have a quote)
        'If iq.sesh(lid, "QuoteID") IsNot Nothing Then

        'Dim quote As clsQuote
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        'quote = agentAccount.Quotes(CInt(iq.sesh(lid, "QuoteID")))

        If Trim$(quoteName) = "" Or Trim$(quoteName) = "-" Then
            Me.Name = New nullableString(BuyerAccount.BuyerChannel.Name)
        Else
            Me.Name = New nullableString(quoteName)
        End If

        Me.Saved = True
        Me.Update()
        Me.RootItem.updateRecursive()

        Return Xlt("Version " & Me.Version & " saved.", agentAccount.Language) ' ,version " & NewQuote.Version & " created"

        '  Else
        '    Return String.Empty
        '   End If
    End Function

    Function ExportHistory(onlyThisVersion As Boolean) As List(Of clsQuoteHistoryItem)
        ExportHistory = New List(Of clsQuoteHistoryItem)()
        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim sqlQuery As String
        If onlyThisVersion Then
            sqlQuery = "SELECT  qe.[ID],[FK_Quote_ID],[Type],[TimeStamp] , [Version] FROM [QuoteExport] qe inner join Quote q on [FK_Quote_ID] = q.ID where q.id= " & Me.ID
        Else
            sqlQuery = "SELECT  qe.[ID],[FK_Quote_ID],[Type],[TimeStamp] , [Version] FROM [QuoteExport] qe inner join Quote q on [FK_Quote_ID] = q.ID where FK_Quote_ID_Root = " & Me.RootQuote.ID
        End If
        Dim r As SqlClient.SqlDataReader = da.DBExecuteReader(con, sqlQuery)
        Dim dt As DataTable = New DataTable()
        dt.Load(r)

        For Each rd As DataRow In dt.Rows
            ExportHistory.Add(New clsQuoteHistoryItem() With {.ExportType = rd.Item("Type").ToString, .Timestamp = CDate(rd.Item("TimeStamp").ToString), .Version = CInt(rd.Item("Version"))})
        Next
        con.Close()
        con.Dispose()
    End Function

#End Region



    Public Function ExportPDF(lid As UInt64, certPath As String, quoteName As String, ByRef errormessages As List(Of String)) As Boolean
        'We are exporting so lock and save this...
        Locked = True
        Save(DefaultName, lid)

        If ExportExcel(lid, quoteName, errormessages, True) Then
            ExportLogging("PDF")
            Dim filepath As String = CType(iq.sesh(lid, "tostream"), String)
            Dim pdfFile As String = IQDrive.uploadFile(filepath, certPath)
            If pdfFile.Length > 0 Then
                iq.sesh(lid, "tostream") = pdfFile
                iq.sesh(lid, "streamcontent-type") = "application/pdf"
                iq.sesh(lid, "DeleteStreamed") = True
                Return True
            End If
        Else
            Return False
        End If
    End Function

    Public Function ExportExcel(lid As UInt64, quoteName As String, ByRef errormessages As List(Of String), fromPDF As Boolean) As Boolean
        Try
            'We are exporting so lock and save this...
            Locked = True
            Save(DefaultName, lid)

            'should be invisible really (until we have a quote)
            If iq.sesh(lid, "QuoteID") Is Nothing Then
                errormessages.Add(Xlt("You need to add some items to the quote first", CType(iq.sesh(lid, "AgentAccount"), clsAccount).Language))
                Return False
            Else
                Dim fullpath As String
                Me.Name = New nullableString(quoteName)
                Me.Saved = True
                Me.Locked = True
                Me.Update()
                If Not fromPDF Then Me.ExportLogging("Excel")
                fullpath = ODS.OutputQuote(Me, "Quotes", errormessages) 'the OutputQuote() function returns the full physical path the the file generated on the server

                ' If errormessages.Count = 0 Then
                Dim filepath As String = "/Quotes/" & Me.RootQuote.ID & "-" & Me.Version & ".ods"

                iq.sesh(lid, "tostream") = fullpath$
                iq.sesh(lid, "streamcontent-type") = "application/vnd.ms-excel;charset=UTF-8"
                iq.sesh(lid, "DeleteStreamed") = True
                'Response.Redirect("streamer.aspx?lid=" & lid)
                Return True
            End If
        Catch ex As Exception
            ErrorLog.Add(ex)
            Return False
        End Try
    End Function
    Public Function ExportXMLAdv(lid As UInt64, quotenNme As String, errorMessages As List(Of String)) As Boolean
        Try
            'We are exporting so lock and save this...
            Locked = True
            Save(DefaultName, lid)
            Dim outputMessage As String = String.Empty
            'should be invisible really (until we have a quote)
            If iq.sesh(lid, "QuoteID") IsNot Nothing Then

                Me.Name = New nullableString(quotenNme)
                Me.Saved = True
                Me.Locked = True
                Me.Update()
                Me.ExportLogging("XML")
                Dim fullpath As String = SaveXML(XMLDoc(errorMessages), "/quotes/" & RootQuote.ID & "-" & Version & ".xml", outputMessage)

                'PnlErrors.Controls.Add(OutputErrors(errorMessages, lid))

                If fullpath <> "FAIL" Then
                    iq.sesh(lid, "tostream") = fullpath$
                End If
                Return True
            End If
        Catch ex As Exception
            ErrorLog.Add(ex)
            Return False
        End Try
    End Function
    Public Function ExportXML(lid As UInt64, quotenNme As String, errorMessages As List(Of String)) As Boolean
        Try
            'We are exporting so lock and save this...
            Locked = True
            Save(DefaultName, lid)
            Dim vPath = HttpContext.Current.Request.ApplicationPath
            Dim pPath = HttpContext.Current.Request.MapPath(vPath) & "\"

            Dim outputMessage As String = String.Empty
            'should be invisible really (until we have a quote)
            If iq.sesh(lid, "QuoteID") IsNot Nothing Then
                Me.Name = New nullableString(quotenNme)
                Me.Saved = True
                Me.Locked = True
                Me.Update()
                Me.ExportLogging("XML")

                Dim tf As String
                Dim fn As String
                fn = "Quotes\" & Me.RootQuote.ID & "-" & Me.Version & ".xml"
                tf = pPath & fn

                Try
                    If My.Computer.FileSystem.FileExists(tf) Then My.Computer.FileSystem.DeleteFile(tf)


                    Dim objWriter As New System.IO.StreamWriter(tf)
                    Dim xmlstring = Me.basketAsXML(lid)
                    objWriter.WriteLine(xmlstring)
                    objWriter.Close()


                    'PnlErrors.Controls.Add(OutputErrors(errorMessages, lid))


                    iq.sesh(lid, "tostream") = tf

                Catch ex As Exception
                    ErrorLog.Add(ex)

                End Try
                Return True
            End If
        Catch ex As Exception
            ErrorLog.Add(ex)
            Return False
        End Try
    End Function
    Public Function ExportXMLSmart(lid As UInt64, quotenNme As String, errorMessages As List(Of String)) As Boolean

        Try
            'We are exporting so lock and save this...
            Locked = True
            Save(DefaultName, lid)

            Dim outputMessage As String = String.Empty
            'should be invisible really (until we have a quote)
            If iq.sesh(lid, "QuoteID") IsNot Nothing Then
                Me.Name = New nullableString(quotenNme)
                Me.Saved = True
                Me.Locked = True
                Me.Update()
                Me.ExportLogging("XML")
                Dim fullpath As String = SaveXML(XMLDocSmartQuote(errorMessages), "/quotes/" & RootQuote.ID & "-" & Version & ".xml", outputMessage)

                'PnlErrors.Controls.Add(OutputErrors(errorMessages, lid))

                If fullpath <> "FAIL" Then
                    iq.sesh(lid, "tostream") = fullpath$
                End If
                Return True
            End If
        Catch ex As Exception
            ErrorLog.Add(ex)
            Return False
        End Try
    End Function
    Public Function PassesValidation(lid As UInt64) As Boolean 'lid As UInt64) As Boolean
        'Dim buyeraccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        'Dim agentaccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim errorMessages As List(Of String) = New List(Of String)()
        Me.LoadItems(errorMessages)
        Me.validate(lid, BuyerAccount, AgentAccount, errorMessages, False)
        Return Me.RootItem.AllChildMsgs.Where(Function(m) m.severity > EnumValidationSeverity.amberalert AndAlso m.type = enumValidationMessageType.Validation).Count = 0

    End Function
    Public Sub getPrice(toget As List(Of clsVariant), lid As UInt64, ByRef errorMessages As List(Of String))
        If (Me.AgentAccount.SellerChannel.priceConfig And 8) <> 0 Then
            Me.RootItem.getQuoteVariant(toget, True)

            Dim handle As Integer
            handle = ModUniTran.DispatchUpdateRequest(lid, toget, "", errorMessages)

            Dim response As wsconsumer.clsStockPriceResponse = Nothing
            response = callWsConsumer(handle, errorMessages)
            '   OutputErrors(Me.Controls, errorMessages, lid) ' these just go to the audit log now

            If response IsNot Nothing Then 'The webservice call can fail 
                updateStockPriceFromResponse(handle, response, lid, errorMessages)
                '  outputUpdatedPriceUIs(lid, handle, response, "", errorMessages)
                Dim lit As Literal = New Literal
                If PendingRequests IsNot Nothing AndAlso PendingRequests.ContainsKey(handle) Then


                    If response.completed Or PendingRequests(handle).tryCount = 5 Then
                        Dim removed As clsQueuedRequest = Nothing
                        PendingRequests.TryRemove(handle, removed)

                        lit.Text = "]DONE^" 'we have completed the fetches for all ID (they are all less than 5 minutes old)
                    Else
                        PendingRequests(handle).tryCount += 1
                    End If

                    'if any of the prices are still pending.. set another timeout
                    '   lit.Text = "]" & Request("Path") & "^" & handle & "^"  'Some prices are still old - we add the path so a new setTimeout() can be created in the JS PlacePrices()
                End If

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
            response = requester.CheckStockPrices(handle, True, 30)  'this call is very fast - it returns the results sofar for the specified handle  (which may be an empty list).. and status   - TODO probably want to set a nice short timeout in the binding, and handle errors gracefully
            requester.Close()  'new
        Catch ex As System.Exception
            errorMessages.Add("*" & ex.Message)
            response = Nothing
        End Try

        Return response

    End Function
    Private Sub updateStockPriceFromResponse(handle As Integer, response As wsconsumer.clsStockPriceResponse, lid As UInt64, ByRef errormessages As List(Of String))

        Dim buyeraccount As clsAccount

        If PendingRequests Is Nothing Then
            errormessages.Add("* Pending requests was nothing")
        Else
            If Not PendingRequests.ContainsKey(handle) Then
                errormessages.Add("* PendingRequests did not contain the handle:" & handle)
            Else

                For Each r In response.items

                    Dim vs As IEnumerable(Of clsVariant) = (From rq In PendingRequests(handle).skuVariants Where rq.DistiSku = r.SKU)
                    If vs.Any Then
                        Dim v As clsVariant = vs.First
                        buyeraccount = PendingRequests(handle).BuyerAccount
                        updatePriceStock(buyeraccount, v, r, lid)
                        If vs.Count > 1 Then errormessages.Add("* There were " & vs.Count & " rows returend for " & v.DistiSku & " expected 1")

                    Else
                        errormessages.Add("* The request contained no SKU:" & r.SKU)
                    End If
                Next
            End If
        End If



    End Sub
    Public Sub updatePriceStock(buyeraccount As clsAccount, v As clsVariant, item As wsconsumer.clsStockPriceItem, lid As UInt64)

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
                        price.Price.value = CDec(item.CustomerPrice)
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

                price.Update()  'Expensive
            Else
                'should never happen ! - the webservice created a POA price in advance
                Dim newprice As clsPrice = New clsPrice(v, buyeraccount.Priceband, New NullablePrice(item.CustomerPrice, .Currency, False), "Wesbservice (updatePriceStock add)")
                price = newprice
            End If

            If price.Price.isValid Then
                If iq.sesh(lid, "QuoteID") IsNot Nothing Then
                    Dim quote As clsQuote = buyeraccount.Quotes(CInt(iq.sesh(lid, "QuoteID")))
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
    Private Sub outputUpdatedPriceUIs(lid As UInt64, handle As Integer, response As wsconsumer.clsStockPriceResponse, divIds As String, ByRef errormessages As List(Of String))


        If handle <> -1 Then
            Dim buyeraccount As clsAccount = PendingRequests(handle).BuyerAccount
            Dim lit As Literal
            If response.items.Count > 0 Then   'yey we have results

                If divIds <> "" Then  'it's possibe we closed the DIV (whilst the request was pending)
                    Dim b() As String

                    Dim pnl As Panel
                    For Each ID As String In Split(divIds, ",")  'each DIV id is of the form P_priceID (or S_Stockid)

                        'If ID <> "" Then
                        '    lit = New Literal
                        '    lit.Text = "]" & ID & "^"  'This ASPX outputs ]DivID^replacamentContent  - which is merged back into the poage by the JS placePrices()
                        '    Form.Controls.Add(lit)

                        '    b = Split(ID, "_")
                        '    If b(0) = "P" Then
                        '        If b(1) <> "-1" Then  'todo - remove (to do with POA's and temporary variants  see getprices)
                        '            '    minutesOld = 0 ' UI will RETURN the age of the price
                        '            'pnl = iq.Products(b(1)).prices(b(2)).Ui 'Should go green
                        '            If buyeraccount IsNot Nothing Then
                        '                pnl = iq.Prices(b(1)).Ui(buyeraccount, 1, lid) 'Should go green
                        '            Else
                        '                errormessages.Add("* BuyerAccount was nothing in getPriceUIs")
                        '                pnl = New Panel
                        '            End If

                        '            'minutesOld = iq.Prices(b(1)).MinutesOld
                        '            'If minutesOld > 5 Then
                        '            ' allDone = False
                        '            ' End If

                        '            Form.Controls.Add(pnl)
                        '            ' Else
                        '            '     Beep()
                        '        Else
                        '            Beep()
                        '        End If

                        '    ElseIf b(0) = "S" Then
                        '        ' the placeholder contains a panels (div) - which holds the stock UI  
                        '        Dim ph As PlaceHolder = iq.Stock(b(1)).SKUvariant.StockUI(1, "", buyeraccount.Language)
                        '        Form.Controls.Add(ph)
                        '    Else
                        '        Beep()
                        '    End If
                        'End If
                    Next
                End If
            End If
        End If

    End Sub
    Public Function basketAsXML(lid As UInt64) As String
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        If Me IsNot Nothing Then
            'Generate the xml using the proxy class
            Dim dt As Data = New Data()
            dt.Quote = New DataQuote()
            dt.Quote.ID = Me.ID
            dt.Quote.Name = Me.Name.DisplayValue
            dt.Quote.CreatedBy = Me.AgentAccount.User.RealName
            dt.Quote.Supplier = Me.AgentAccount.SellerChannel.Name

            Dim products As List(Of DataQuoteProduct) = New List(Of DataQuoteProduct)
            Dim product As DataQuoteProduct
            For Each flatListItem In Me.RootItem.Flattened(True, False, 0).items
                product = New DataQuoteProduct()
                product.Class = flatListItem.QuoteItem.Branch.Product.ProductType.Code
                product.PartNum = flatListItem.QuoteItem.Branch.Product.SKU
                product.SupplierPartNum = flatListItem.QuoteItem.SKUVariant.DistiSku

                product.ListPrice = flatListItem.QuoteItem.ListPrice.value
                product.Description = flatListItem.QuoteItem.Branch.DisplayName(Me.BuyerAccount.Language)
                product.Qty = flatListItem.Quantity.ToString()
                product.URLProductImage = flatListItem.QuoteItem.Branch.Picture
                product.OPGref = IIf(flatListItem.QuoteItem.OPG.DisplayValue() = "-", "", flatListItem.QuoteItem.OPG.DisplayValue()).ToString()
                product.RebateValue = flatListItem.QuoteItem.rebate
                product.RebateValueSpecified = True
                products.Add(product)

            Next
            dt.Quote.Product = products.ToArray()


            Dim xmlString As String = SerializeToString(dt)

            Return xmlString

        End If
    End Function
    Public Function basketAsHiddenFields(lid As UInt64) As String
        'form based basket

        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        Dim hdnbasket As StringBuilder = New StringBuilder()
        Dim templateString As String = "<input type=""hidden"" name=""{0}"" id=""{0}"" value=""{1}"" /> "
        If Me IsNot Nothing Then
            hdnbasket.Append(vbCrLf)
            hdnbasket.Append(String.Format(templateString, "SesID", iq.sesh(lid, "GK_SessionID")))
            hdnbasket.Append(vbCrLf)
            hdnbasket.Append(String.Format(templateString, "UserID", iq.sesh(lid, "GK_uEmail")))
            hdnbasket.Append(vbCrLf)
            hdnbasket.Append(String.Format(templateString, "Account", iq.sesh(lid, "GK_cAccountNum")))
            hdnbasket.Append(vbCrLf)
            hdnbasket.Append(String.Format(templateString, "cPriceBand", iq.sesh(lid, "GK_cPriceBand")))
            hdnbasket.Append(vbCrLf)
            hdnbasket.Append(String.Format(templateString, "QuoteID", Me.ID))
            hdnbasket.Append(vbCrLf)
            hdnbasket.Append(String.Format(templateString, "Account", agentAccount.ID))
            hdnbasket.Append(vbCrLf)
            hdnbasket.Append(String.Format(templateString, "Grouped", ""))
            hdnbasket.Append(vbCrLf)
            hdnbasket.Append(String.Format(templateString, "OrdType", "IQ"))
            hdnbasket.Append(vbCrLf)
            Dim Productnum As Integer = 0


            For Each flatListItem In Me.RootItem.Flattened(True, False, 0).items
                If Not flatListItem.QuoteItem.SKUVariant.DistiSku.Contains("###") Then
                    Productnum += 1
                    Dim prodId As String = "Item[Prod" & Productnum.ToString() & "]["

                    Dim pn As String = flatListItem.QuoteItem.Branch.Product.SKU
                    If Not String.IsNullOrEmpty(flatListItem.QuoteItem.SKUVariant.Code) Then
                        pn += "#" & flatListItem.QuoteItem.SKUVariant.Code
                    End If
                    hdnbasket.Append(String.Format(templateString, prodId & "PN]", pn))

                    hdnbasket.Append(vbCrLf)
                    hdnbasket.Append(String.Format(templateString, prodId & "SKU]", flatListItem.QuoteItem.SKUVariant.DistiSku))
                    hdnbasket.Append(vbCrLf)
                    hdnbasket.Append(String.Format(templateString, prodId & "Qty]", flatListItem.Quantity.ToString()))
                    hdnbasket.Append(vbCrLf)
                    hdnbasket.Append(String.Format(templateString, prodId & "Description]", flatListItem.QuoteItem.Branch.Product.DisplayName(Me.BuyerAccount.Language)))
                    hdnbasket.Append(vbCrLf)
                    hdnbasket.Append(String.Format(templateString, prodId & "Price]", flatListItem.QuoteItem.BasePrice.sqlvalue))
                    hdnbasket.Append(vbCrLf)
                End If
            Next

            hdnbasket.Append(String.Format(templateString, "Multiplier", "1"))
            hdnbasket.Append(vbCrLf)
            Return hdnbasket.ToString()
        Else
            Return String.Empty
        End If
    End Function

    Public ReadOnly Property QuoteSplit As Manufacturer

        Get

            QuoteSplit = Manufacturer.Unknown

            If (RootItem IsNot Nothing) AndAlso (RootItem.Children IsNot Nothing) AndAlso (RootItem.Children.Count > 0) Then
                Dim root = RootItem.Children(0)
                If root.Branch.Product IsNot Nothing Then
                    QuoteSplit = RootItem.Children(0).Branch.Product.Manufacturer
                End If
            End If

        End Get

    End Property

End Class 'End of class clsQuote
Public Class clsQuoteHistoryItem
    Public Property Timestamp As DateTime
    Public Property Version As Int32
    Public Property ExportType As String
End Class