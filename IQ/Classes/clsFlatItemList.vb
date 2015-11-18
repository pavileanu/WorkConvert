Imports System.Xml
Imports System.Globalization


Public Class clsFlatListItem

    Public QuoteItem As clsQuoteItem
    Public Indent As Integer
    Public Quantity As Integer

    Public Sub New(QuoteItem As clsQuoteItem, indent As Integer, Quantity As Integer)
        Me.QuoteItem = QuoteItem
        Me.Quantity = Quantity
        Me.Indent = indent

    End Sub


    Public Function HTMLTableRow(language As clsLanguage, ByRef priceChanged As Boolean, ByRef errorMessages As List(Of String)) As TableRow

        Dim tr As New TableRow
        Dim tc As New TableCell

        Dim Product As clsProduct = Me.QuoteItem.Branch.Product

        Dim quote As clsQuote
        quote = Me.QuoteItem.quote 'Make a handy reference to the quote this line is part of

        'format per the seller channels regions culture (ulitmately there should be an account.culture)
        Dim region As clsRegion = quote.BuyerAccount.SellerChannel.Region

        Dim culture As clsCulture = quote.BuyerAccount.Culture

        Dim lbl As Label

        'SKU

        Dim PartNo As String = String.Empty

        PartNo = Product.sku 'product.i_attributes_code("MfrSKU").text(s_lang)

        tc.Text = PartNo
        tr.Cells.Add(tc)

        'VARIANT
        tc = New TableCell
        If Not Me.QuoteItem.SKUVariant Is Nothing Then
            tc.Text = Me.QuoteItem.SKUVariant.Code
        End If
        tr.Cells.Add(tc)

        'OPTTYPE
        tc = New TableCell
        If Product.i_Attributes_Code.ContainsKey("optType") Then
            Dim opt As String = Product.i_Attributes_Code("optType")(0).Translation.text(s_lang)
            tc.Text = opt
        End If
        tr.Cells.Add(tc)

        'DESCRIPTION
        tc = New TableCell
        'If Product.i_attributes_code.containskey("Name") Then
        ' tc.Text = Product.i_Attributes_Code("Name")(0).Translation.text(s_lang)
        ' Else
        If Product.i_Attributes_Code.ContainsKey("desc") Then
            tc.Text = Product.i_Attributes_Code("desc")(0).Translation.text(s_lang)
        ElseIf Product.i_Attributes_Code.ContainsKey("Name") Then
            tc.Text = Product.i_Attributes_Code("Name")(0).Translation.text(s_lang)
        Else
            tc.Text = "-"
        End If

        'End If
        tr.Cells.Add(tc)

        'LISTPRICE
        tc = New TableCell
        Dim listprice As NullablePrice = NullPrice(Product.ListPrice(quote.BuyerAccount), quote.BuyerAccount.Currency)
        Dim listpriceNow As NullablePrice = NullPrice(Product.ListPrice(quote.BuyerAccount), quote.BuyerAccount.Currency)
        Dim ListpriceBefore As NullablePrice = Me.QuoteItem.ListPrice
        'format per the seller channels regions culture (ulitmately there should be an account.culture)
        lbl = New Label
        lbl.Text = listpriceNow.text(quote.BuyerAccount, errorMessages) ' FormatPrice(listpriceNow.NumericValue, culture)
        tc.Controls.Add(lbl)

        'decorates the Tabel cell with a price increased/decreased graphic
        priceChangePics(tc, ListpriceBefore, listpriceNow, language)
        tr.Cells.Add(tc)

        'UNITPRICE (buyer specific)
        tc = New TableCell
        tc.Text = Me.QuoteItem.BasePrice.text(quote.BuyerAccount, errorMessages)
        tr.Cells.Add(tc)

        Dim buyPriceNow As NullablePrice
        Dim BuyPriceBefore As NullablePrice

        Dim prices As List(Of clsPrice)

        prices = Product.GetPrices(Me.QuoteItem.quote.BuyerAccount, Me.QuoteItem.quote.BuyerAccount.SellerChannel.priceConfig, Me.QuoteItem.SKUVariant, errorMessages, True)
        If prices.Count > 0 Then
            buyPriceNow = If(prices(0) IsNot Nothing, prices(0).Price, New NullablePrice(Me.QuoteItem.quote.Currency))
        Else
            If Me.QuoteItem.IsPreInstalled Then
                ' this is preinstalled and is a ZERO price 
                buyPriceNow = New NullablePrice(0, Me.QuoteItem.quote.Currency, False)
            Else
                ' POA or unknown price (price has been deleted since the quote was run)
                buyPriceNow = New NullablePrice(Me.QuoteItem.quote.Currency)
            End If
        End If
        ' 'VariantPrice(Me.QuoteItem.quote.BuyerAccount, Me.QuoteItem.SKUVariant)
        BuyPriceBefore = Me.QuoteItem.BasePrice

        '           If buyPriceNow.valid Then Stop

        'Price change
        tc = New TableCell
        lbl = New Label
        If Not BuyPriceBefore.isValid And buyPriceNow.isValid Then
            lbl.Text = Xlt("New price", quote.BuyerAccount.Language)

        ElseIf Not buyPriceNow.isValid Then
            lbl.Text = Xlt("POA", quote.BuyerAccount.Language)
        Else
            Dim diff As Single
            diff = buyPriceNow.NumericValue - BuyPriceBefore.NumericValue
            If diff <> 0 Then
                Dim changedprice As NullablePrice = New NullablePrice(Math.Abs(diff), quote.BuyerAccount.Currency, False)
                lbl.Text = CStr(IIf(diff > 0, "+", "-")) & changedprice.text(quote.BuyerAccount, errorMessages)

                priceChanged = True
            Else
                lbl.Text = Xlt("none", quote.BuyerAccount.Language)
            End If
        End If
        tc.Controls.Add(lbl)

        'decorates the Tabel cell with a price increased/decreased graphic
        priceChangePics(tc, BuyPriceBefore, buyPriceNow, language)

        tr.Cells.Add(tc)

        'QUANTITY
        tc = New TableCell
        tc.Text = Me.Quantity.ToString
        tr.Cells.Add(tc)

        'LINEPRICE
        tc = New TableCell
        If Not Me.QuoteItem.BasePrice.isValid Then
            tc.Text = "£POA" 'TODO generalise/multicurrency
        Else
            Dim linePrice As NullablePrice = New NullablePrice(Me.QuoteItem.BasePrice.value * Me.QuoteItem.Quantity * Me.QuoteItem.Margin, Me.QuoteItem.quote.Currency, False)

            tc.Text = linePrice.text(quote.BuyerAccount, errorMessages)
        End If
        tr.Cells.Add(tc)


        'STOCK
        tc = New TableCell
        tc.Text = Product.CurrentStock(Me.QuoteItem.quote.BuyerAccount, 0, Me.QuoteItem.SKUVariant, errorMessages)
        tr.Cells.Add(tc)

        Return tr

    End Function

    Public Sub priceChangePics(tc As TableCell, priceBefore As NullablePrice, priceNow As NullablePrice, language As clsLanguage)

        Dim PriceRise As Image
        Dim PriceDrop As Image

        If Not priceBefore.isValid Then Exit Sub 'if we didn't know the price before.. we can't say wether it's gone up or down
        If Not priceNow.isValid Then Exit Sub

        If priceNow.NumericValue = priceBefore.NumericValue Then
            'Price hasn't changed
        ElseIf priceNow.NumericValue > priceBefore.NumericValue Then
            PriceRise = New Image
            PriceRise.AlternateText = Xlt("Pricing has risen on this item", language)
            PriceRise.ImageUrl = "../images/pricerise.png"
            tc.Controls.Add(PriceRise)
        Else
            PriceDrop = New Image
            PriceDrop.AlternateText = Xlt("Pricing has fallen on this item", language)
            PriceDrop.ImageUrl = "../images/pricedrop.png"
            tc.Controls.Add(PriceDrop)
        End If


    End Sub
    Public Function XMLLine(doc As XmlDocument, quote As clsQuote, IncludeIDs As Boolean, ByRef errorMessages As List(Of String), ByRef productSkus As List(Of String), ByRef opgs As List(Of String)) As XmlNode

        Dim language As clsLanguage = quote.BuyerAccount.Language
        'returns an XML Node representing this (possibly consolidated) Line
        'consolidated means all rows of this SKU added together (for the BOM (bill of materials) view)

        XMLLine = doc.CreateElement("Line")

        If IncludeIDs Then

            Dim qiid As XmlNode = doc.CreateElement("ID")
            qiid.InnerText = Me.QuoteItem.ID.ToString
            XMLLine.AppendChild(qiid)

            Dim qipid As XmlNode = doc.CreateElement("Parent")
            qipid.InnerText = Me.QuoteItem.Parent.ID.ToString
            XMLLine.AppendChild(qipid)

            Dim Preinstalled As XmlNode = doc.CreateElement("PreInstalled")
            Preinstalled.InnerText = Me.QuoteItem.IsPreInstalled.ToString
            XMLLine.AppendChild(Preinstalled)

            Dim indent As XmlNode = doc.CreateElement("Indent")
            indent.InnerText = Me.Indent.ToString
            XMLLine.AppendChild(indent)

        End If

        Dim Product As clsProduct
        Product = Me.QuoteItem.Branch.Product

        Dim mfrPartNum As XmlNode = doc.CreateElement("MfrSKU")
        Dim partNo As String = String.Empty
        If Product.SKU <> "" Then
            partNo = Product.SKU
            mfrPartNum.InnerXml = xmlEncode(partNo)
        End If

        XMLLine.AppendChild(mfrPartNum)

        If Me.QuoteItem.Note.value IsNot DBNull.Value Then
            Dim note As XmlNode = doc.CreateElement("DistiSKU")
            XMLLine.AppendChild(note)
        End If

        Dim skuVariant As XmlNode = doc.CreateElement("SKUVariant")
        If Not Me.QuoteItem.SKUVariant Is Nothing Then
            Dim skuVariantCode As String = xmlEncode(Me.QuoteItem.SKUVariant.Code)
            If skuVariantCode.ToUpper.Contains("LIST") Then
                skuVariant.InnerXml = String.Empty
            Else
                skuVariantCode = "#" & skuVariantCode
            End If
            Dim DistiSKU As XmlNode = doc.CreateElement("DistiSKU")
            DistiSKU.InnerXml = xmlEncode(Me.QuoteItem.SKUVariant.DistiSku)
            XMLLine.AppendChild(DistiSKU)
        Else
            errorMessages.Add("skuvariant was nothing")
        End If

        XMLLine.AppendChild(skuVariant)


        'Dim SellerPartNum As XmlNode = doc.CreateElement("SellerSKU")
        ' SellerPartNum.InnerText = iq.channelSKU(buyeraccount.SellerChannel)

        Dim OPTTYPE As XmlNode = doc.CreateElement("OptType") 'note Case difference (consistenet with other XML tags but DIFFERENT from the attribute name
        If Product.i_Attributes_Code.ContainsKey("optType") Then
            Dim opt As String = Me.QuoteItem.ShortName(language)
            '  opt$ = Product.i_Attributes_Code("optType")(0).Translation.text(language)
            OPTTYPE.InnerXml = xmlEncode(opt)
        ElseIf Product.isSystem Then
            OPTTYPE.InnerXml = xmlEncode(Xlt("System unit", language))

        End If
        XMLLine.AppendChild(OPTTYPE)

        Dim qty As XmlNode = doc.CreateElement("Quantity")
        qty.InnerText = Me.Quantity.ToString   'not we do NOT use the quoteItems quantity - but the FlatListItems quantity (which may be a consolidation)
        XMLLine.AppendChild(qty)

        Dim Desc As XmlNode = doc.CreateElement("Description")

        'from here - was innertext (for some reason) 
        '        If Product.i_attributes_code.containskey("Name") Then
        ' Desc.InnerXml = xmlEncode(Product.i_Attributes_Code("Name")(0).Translation.text(s_lang))
        ' Else
        If Product.i_Attributes_Code.ContainsKey("desc") Then
            Desc.InnerXml = xmlEncode(Product.i_Attributes_Code("desc")(0).Translation.text(language))
        ElseIf Product.i_Attributes_Code.ContainsKey("Name") Then
            Desc.InnerXml = xmlEncode(Product.i_Attributes_Code("Name")(0).Translation.text(language))
        Else
            Desc.InnerXml = String.Empty
        End If

        'End If
        XMLLine.AppendChild(Desc)


        'NOTE UnitPrice and LinePrice and Multipleid by the (per item) margin (usually 1)
        Dim Price As XmlNode = doc.CreateElement("UnitPrice")
        Price.InnerXml = xmlEncode((Me.QuoteItem.BasePrice.value * Me.QuoteItem.Margin).ToString(CultureInfo.InvariantCulture))
        XMLLine.AppendChild(Price)

        Dim linePrice As XmlNode = doc.CreateElement("LinePrice")
        If Not IsDBNull(Me.QuoteItem.BasePrice.value) Then
            linePrice.InnerXml = xmlEncode((Me.QuoteItem.BasePrice.value * Me.Quantity * Me.QuoteItem.Margin).ToString(CultureInfo.InvariantCulture)) 'product.PriceVariants(buyeraccount).DisplayPrice().Text ' TODO - return price State/POA info

        End If
        XMLLine.AppendChild(linePrice)

        Dim lineRebate As XmlNode = doc.CreateElement("LineRebate")
        lineRebate.InnerXml = xmlEncode((Me.QuoteItem.rebate).ToString(CultureInfo.InvariantCulture)) 'mrp rebates are per line not per item.
        'lineRebate.InnerXml = xmlEncode((Me.QuoteItem.rebate * Me.Quantity).ToString(CultureInfo.InvariantCulture)) 'product.PriceVariants(buyeraccount).DisplayPrice().Text ' TODO - return price State/POA info

        XMLLine.AppendChild(lineRebate)

        Dim linePromo As XmlNode = doc.CreateElement("OPG")
        linePromo.InnerXml = String.Empty
        If Me.QuoteItem.OPG.value IsNot Nothing Then
            If Not IsDBNull(Me.QuoteItem.OPG.value) Then
                linePromo.InnerXml = Me.QuoteItem.OPG.value.ToString
                If opgs IsNot Nothing Then
                    opgs.Add(Me.QuoteItem.OPG.value.ToString)
                End If
            End If
        End If
        XMLLine.AppendChild(linePromo)

        Dim listPrice As XmlNode = doc.CreateElement("ListPrice")
        'DisplayPrice(Me.QuoteItem.quote.BuyerAccount, errorMessages).Text)
        listPrice.InnerXml = xmlEncode(Me.QuoteItem.ListPrice.value.ToString(CultureInfo.InvariantCulture))
        XMLLine.AppendChild(listPrice)

        Dim stock As XmlNode = doc.CreateElement("Stock")
        Dim stockReturned As String = Product.CurrentStock(quote.BuyerAccount, 0, Me.QuoteItem.SKUVariant, errorMessages)
        stock.InnerXml = xmlEncode(getStock(quote.BuyerAccount, stockReturned, True))
        XMLLine.AppendChild(stock)




        If QuoteItem.Branch.Product.isSystem Then
            For Each msg In Me.QuoteItem.Msgs
                If msg.message IsNot Nothing Then
                    If msg.severity > EnumValidationSeverity.BlueInfo Then
                        Dim textmsg As String = xmlEncode(msg.replaceVariables(msg.message.text(language), msg.variables))
                        Dim variants As String() = textmsg.Split(":")
                        Dim productSku As String = variants(0)
                        If Not productSkus.Contains(productSku) Then
                            productSkus.Add(productSku)
                            If variants.Count > 1 Then
                                textmsg = String.Format("{0}>>{1}", productSku, variants(1))
                            Else
                                textmsg = String.Format("{0}", variants)
                            End If

                            Dim Advice As XmlNode = doc.CreateElement("Advice")
                            XMLLine.AppendChild(Advice)
                            'the sku is a duplication of the lline sku - however, having it here makes the code much neater elsewhere (outptting the advice)
                            Dim SKU As XmlNode = doc.CreateElement("SKU")
                            SKU.InnerXml = Me.QuoteItem.Branch.Product.sku
                            Advice.AppendChild(SKU)
                            Dim severity As XmlNode = doc.CreateElement("Severity")
                            severity.InnerXml = msg.severity.ToString
                            Advice.AppendChild(severity)

                            Dim adviceIcon As XmlNode = doc.CreateElement("AdviceIcon")
                            adviceIcon.InnerXml = msg.imagename
                            Advice.AppendChild(adviceIcon)
                            Dim text As XmlNode = doc.CreateElement("Text")
                            text.InnerXml = textmsg
                            Advice.AppendChild(text)

                        End If


                    End If
                End If
            Next
        End If


        'Some of the external text
        If Me.QuoteItem.Branch.Product.i_Attributes_Code.ContainsKey("xText") Then
            Dim vmsgs As List(Of ClsValidationMessage) = Product.getXtext(Me.QuoteItem.Path, Me.QuoteItem.quote.AcknowledgedValidations)
            For Each msg In vmsgs
                If msg.message IsNot Nothing Then
                    Dim textmsg As String = xmlEncode(clsIQ.CleanString(msg.message.text(quote.BuyerAccount.Language)))
                    Dim variants As String() = textmsg.Split(":")
                    Dim productSku As String = variants(0)
                    If Not productSkus.Contains(productSku) Then
                        productSkus.Add(productSku)
                        If variants.Count > 1 Then
                            textmsg = String.Format("{0}>>{1}", productSku, variants(1))
                        Else
                            textmsg = String.Format("{0}", variants)
                        End If
                        Dim Advice As XmlNode = doc.CreateElement("Advice")
                        XMLLine.AppendChild(Advice)

                        Dim SKU As XmlNode = doc.CreateElement("SKU")
                        SKU.InnerXml = Me.QuoteItem.Branch.Product.sku
                        Advice.AppendChild(SKU)
                        Dim severity As XmlNode = doc.CreateElement("Severity")
                        severity.InnerXml = msg.severity.ToString
                        Advice.AppendChild(severity)
                        Dim adviceIcon As XmlNode = doc.CreateElement("AdviceIcon")
                        adviceIcon.InnerXml = msg.imagename
                        Advice.AppendChild(adviceIcon)
                        Dim text As XmlNode = doc.CreateElement("Text")
                        text.InnerXml = textmsg
                        Advice.AppendChild(text)
                    End If
                End If
            Next
        End If


        If Me.QuoteItem.Note.value IsNot DBNull.Value Then
            Dim Note As XmlNode = doc.CreateElement("Note")
            XMLLine.AppendChild(Note)

            Dim SKU As XmlNode = doc.CreateElement("SKU")
            SKU.InnerXml = Me.QuoteItem.Branch.Product.sku
            Note.AppendChild(SKU)

            Dim text As XmlNode = doc.CreateElement("Text")
            text.InnerXml = xmlEncode(Me.QuoteItem.Note.value.ToString)
            Note.AppendChild(text)
        End If

    End Function
    ''' <summary>
    ''' Gets stock quantity or message in stock or out of stock for binarystock channels.
    ''' </summary>
    ''' <param name="account">an instance of clsAccount.</param>
    ''' <param name="value">An integer value that represents the quantity of stock.</param>
    ''' <param name="export">A boolean value that represents if export is being done.</param>
    ''' <returns>A string object that represents the text or number to display in quote export.</returns>
    ''' <remarks></remarks>
    Private Function getStock(account As clsAccount, value As Int64, export As Boolean) As String
        Dim result As String = String.Empty
        If account.SellerChannel.BinaryStock And value > 0 Then
            result = InStock.text(account.Language)
        ElseIf account.SellerChannel.BinaryStock And value <= 0 Then
            result = OutOfStock.text(account.Language)
        ElseIf Not account.SellerChannel.BinaryStock And value > 0 Then
            result = value
        ElseIf Not account.SellerChannel.BinaryStock And value <= 0 Then
            If export Then
                result = "0"
            Else
                result = value.ToString
            End If
        End If
        Return result
    End Function
    ''' <summary>
    ''' Returns a SmartQuote-format XML Node representing this line
    ''' </summary>
    ''' <param name="doc">The parent XMLDocument</param>
    ''' <param name="quote">The quote</param>
    ''' <param name="errorMessages">Error messages</param>
    ''' <returns>The formatted XML Node</returns>
    ''' <remarks></remarks>
    Public Function XMLSmartQuoteLine(doc As XmlDocument, quote As clsQuote, ByRef errorMessages As List(Of String)) As XmlNode

        Dim language As clsLanguage = quote.BuyerAccount.Language

        XMLSmartQuoteLine = doc.CreateElement("EclipseLineItem")

        Dim attr As XmlAttribute = doc.CreateAttribute("TagText")
        attr.Value = "NA"
        XMLSmartQuoteLine.Attributes.Append(attr)

        attr = doc.CreateAttribute("ProductNumber")
        attr.Value = Me.QuoteItem.SKUVariant.DistiSku
        ' Change any part numbers that end #xyz to use (tab)xyz
        'Dim s = Regex.Replace(Me.QuoteItem.SKUVariant.DistiSku, "^([a-zA-Z0-9]*)#([a-zA-Z0-9]{3})$", "$1{TAB}$2")
        'attr.Value = s.Replace("{TAB}", vbTab)
        XMLSmartQuoteLine.Attributes.Append(attr)

        attr = doc.CreateAttribute("Quantity")
        attr.Value = Quantity.ToString()
        XMLSmartQuoteLine.Attributes.Append(attr)

        attr = doc.CreateAttribute("Description")
        If QuoteItem.Branch.Product IsNot Nothing Then
            attr.Value = xmlEncode(QuoteItem.Branch.Product.DisplayName(language, True))
        Else
            attr.Value = String.Empty
        End If
        XMLSmartQuoteLine.Attributes.Append(attr)

        attr = doc.CreateAttribute("NetPrice")
        attr.Value = "0"
        XMLSmartQuoteLine.Attributes.Append(attr)

        attr = doc.CreateAttribute("IntegrationGroupCode")
        attr.Value = String.Empty
        XMLSmartQuoteLine.Attributes.Append(attr)

        attr = doc.CreateAttribute("Level")
        attr.Value = If(QuoteItem.Branch.Product.isSystem(QuoteItem.Path), "1", "2")
        XMLSmartQuoteLine.Attributes.Append(attr)

    End Function

End Class

