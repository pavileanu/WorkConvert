using System.Xml;
using System.Globalization;



public class clsFlatListItem
{

    public clsQuoteItem QuoteItem;
    public int Indent;
    public int Quantity;

    public clsFlatListItem(clsQuoteItem QuoteItem, int indent, int Quantity)
    {
        this.QuoteItem = QuoteItem;
        this.Quantity = Quantity;
        this.Indent = indent;

    }


    public TableRow HTMLTableRow(clsLanguage language, ref bool priceChanged, List<string> errorMessages)
    {

        TableRow tr = new TableRow();
        TableCell tc = new TableCell();

        clsProduct Product = this.QuoteItem.Branch.Product;

        clsQuote quote = default(clsQuote);
        quote = this.QuoteItem.quote; //Make a handy reference to the quote this line is part of

        //format per the seller channels regions culture (ulitmately there should be an account.culture)
        clsRegion region = quote.BuyerAccount.SellerChannel.Region;

        clsCulture culture = quote.BuyerAccount.Culture;

        Label lbl = default(Label);

        //SKU

        string PartNo = string.Empty;

        PartNo = System.Convert.ToString(Product.sku); //product.i_attributes_code("MfrSKU").text(s_lang)

        tc.Text = PartNo;
        tr.Cells.Add(tc);

        //VARIANT
        tc = new TableCell();
        if (!(this.QuoteItem.SKUVariant == null))
        {
            tc.Text = this.QuoteItem.SKUVariant.Code;
        }
        tr.Cells.Add(tc);

        //OPTTYPE
        tc = new TableCell();
        if (Product.i_Attributes_Code.ContainsKey("optType"))
        {
            string opt = System.Convert.ToString(Product.i_Attributes_Code("optType")[0].Translation.text(s_lang));
            tc.Text = opt;
        }
        tr.Cells.Add(tc);

        //DESCRIPTION
        tc = new TableCell();
        //If Product.i_attributes_code.containskey("Name") Then
        // tc.Text = Product.i_Attributes_Code("Name")(0).Translation.text(s_lang)
        // Else
        if (Product.i_Attributes_Code.ContainsKey("desc"))
        {
            tc.Text = Product.i_Attributes_Code("desc")[0].Translation.text(s_lang);
        }
        else if (Product.i_Attributes_Code.ContainsKey("Name"))
        {
            tc.Text = Product.i_Attributes_Code("Name")[0].Translation.text(s_lang);
        }
        else
        {
            tc.Text = "-";
        }

        //End If
        tr.Cells.Add(tc);

        //LISTPRICE
        tc = new TableCell();
        NullablePrice listprice = NullPrice(Product.ListPrice(quote.BuyerAccount), quote.BuyerAccount.Currency);
        NullablePrice listpriceNow = NullPrice(Product.ListPrice(quote.BuyerAccount), quote.BuyerAccount.Currency);
        NullablePrice ListpriceBefore = this.QuoteItem.ListPrice;
        //format per the seller channels regions culture (ulitmately there should be an account.culture)
        lbl = new Label();
        lbl.Text = listpriceNow.text(quote.BuyerAccount, errorMessages); // FormatPrice(listpriceNow.NumericValue, culture)
        tc.Controls.Add(lbl);

        //decorates the Tabel cell with a price increased/decreased graphic
        priceChangePics(tc, ListpriceBefore, listpriceNow, language);
        tr.Cells.Add(tc);

        //UNITPRICE (buyer specific)
        tc = new TableCell();
        tc.Text = this.QuoteItem.BasePrice.text(quote.BuyerAccount, errorMessages);
        tr.Cells.Add(tc);

        NullablePrice buyPriceNow = default(NullablePrice);
        NullablePrice BuyPriceBefore = default(NullablePrice);

        List<clsPrice> prices = default(List<clsPrice>);

        prices = Product.GetPrices(this.QuoteItem.quote.BuyerAccount, this.QuoteItem.quote.BuyerAccount.SellerChannel.priceConfig, this.QuoteItem.SKUVariant, errorMessages, true);
        if (prices.Count > 0)
        {
            buyPriceNow = (prices[0] != null) ? (prices[0].Price) : (new NullablePrice(this.QuoteItem.quote.Currency));
        }
        else
        {
            if (this.QuoteItem.IsPreInstalled)
            {
                // this is preinstalled and is a ZERO price
                buyPriceNow = new NullablePrice(0, this.QuoteItem.quote.Currency, false);
            }
            else
            {
                // POA or unknown price (price has been deleted since the quote was run)
                buyPriceNow = new NullablePrice(this.QuoteItem.quote.Currency);
            }
        }
        // 'VariantPrice(Me.QuoteItem.quote.BuyerAccount, Me.QuoteItem.SKUVariant)
        BuyPriceBefore = this.QuoteItem.BasePrice;

        //           If buyPriceNow.valid Then Stop

        //Price change
        tc = new TableCell();
        lbl = new Label();
        if (!BuyPriceBefore.isValid && buyPriceNow.isValid)
        {
            lbl.Text = Xlt("New price", quote.BuyerAccount.Language);

        }
        else if (!buyPriceNow.isValid)
        {
            lbl.Text = Xlt("POA", quote.BuyerAccount.Language);
        }
        else
        {
            float diff = 0;
            diff = System.Convert.ToSingle(buyPriceNow.NumericValue - BuyPriceBefore.NumericValue);
            if (diff != 0)
            {
                NullablePrice changedprice = new NullablePrice(Math.Abs(diff), quote.BuyerAccount.Currency, false);
                lbl.Text = (diff > 0 ? "+" : "-").ToString() + changedprice.text(quote.BuyerAccount, errorMessages);

                priceChanged = true;
            }
            else
            {
                lbl.Text = Xlt("none", quote.BuyerAccount.Language);
            }
        }
        tc.Controls.Add(lbl);

        //decorates the Tabel cell with a price increased/decreased graphic
        priceChangePics(tc, BuyPriceBefore, buyPriceNow, language);

        tr.Cells.Add(tc);

        //QUANTITY
        tc = new TableCell();
        tc.Text = this.Quantity.ToString();
        tr.Cells.Add(tc);

        //LINEPRICE
        tc = new TableCell();
        if (!this.QuoteItem.BasePrice.isValid)
        {
            tc.Text = "£POA"; //TODO generalise/multicurrency
        }
        else
        {
            NullablePrice linePrice = new NullablePrice(this.QuoteItem.BasePrice.value * this.QuoteItem.Quantity * this.QuoteItem.Margin, this.QuoteItem.quote.Currency, false);

            tc.Text = linePrice.text(quote.BuyerAccount, errorMessages);
        }
        tr.Cells.Add(tc);


        //STOCK
        tc = new TableCell();
        tc.Text = Product.CurrentStock(this.QuoteItem.quote.BuyerAccount, 0, this.QuoteItem.SKUVariant, errorMessages);
        tr.Cells.Add(tc);

        return tr;

    }

    public void priceChangePics(TableCell tc, NullablePrice priceBefore, NullablePrice priceNow, clsLanguage language)
    {

        Image PriceRise = default(Image);
        Image PriceDrop = default(Image);

        if (!priceBefore.isValid)
        {
            return; //if we didn't know the price before.. we can't say wether it's gone up or down
        }
        if (!priceNow.isValid)
        {
            return;
        }

        if (priceNow.NumericValue == priceBefore.NumericValue)
        {
            //Price hasn't changed
        }
        else if (priceNow.NumericValue > priceBefore.NumericValue)
        {
            PriceRise = new Image();
            PriceRise.AlternateText = Xlt("Pricing has risen on this item", language);
            PriceRise.ImageUrl = "../images/pricerise.png";
            tc.Controls.Add(PriceRise);
        }
        else
        {
            PriceDrop = new Image();
            PriceDrop.AlternateText = Xlt("Pricing has fallen on this item", language);
            PriceDrop.ImageUrl = "../images/pricedrop.png";
            tc.Controls.Add(PriceDrop);
        }


    }
    public XmlNode XMLLine(XmlDocument doc, clsQuote quote, bool IncludeIDs, List<string> errorMessages, List<string> productSkus, List<string> opgs)
    {
        XmlNode returnValue = default(XmlNode);

        clsLanguage language = quote.BuyerAccount.Language;
        //returns an XML Node representing this (possibly consolidated) Line
        //consolidated means all rows of this SKU added together (for the BOM (bill of materials) view)

        returnValue = doc.CreateElement("Line");

        if (IncludeIDs)
        {

            XmlNode qiid = doc.CreateElement("ID");
            qiid.InnerText = this.QuoteItem.ID.ToString();
            returnValue.AppendChild(qiid);

            XmlNode qipid = doc.CreateElement("Parent");
            qipid.InnerText = this.QuoteItem.Parent.ID.ToString();
            returnValue.AppendChild(qipid);

            XmlNode Preinstalled = doc.CreateElement("PreInstalled");
            Preinstalled.InnerText = this.QuoteItem.IsPreInstalled.ToString();
            returnValue.AppendChild(Preinstalled);

            XmlNode indent = doc.CreateElement("Indent");
            indent.InnerText = this.Indent.ToString();
            returnValue.AppendChild(indent);

        }

        clsProduct Product = default(clsProduct);
        Product = this.QuoteItem.Branch.Product;

        XmlNode mfrPartNum = doc.CreateElement("MfrSKU");
        string partNo = string.Empty;
        if (Product.SKU != "")
        {
            partNo = System.Convert.ToString(Product.SKU);
            mfrPartNum.InnerXml = xmlEncode(partNo);
        }

        returnValue.AppendChild(mfrPartNum);

        if (this.QuoteItem.Note.value != DBNull.Value)
        {
            XmlNode note = doc.CreateElement("DistiSKU");
            returnValue.AppendChild(note);
        }

        XmlNode skuVariant = doc.CreateElement("SKUVariant");
        if (!(this.QuoteItem.SKUVariant == null))
        {
            string skuVariantCode = System.Convert.ToString(xmlEncode(this.QuoteItem.SKUVariant.Code));
            if (skuVariantCode.ToUpper().Contains("LIST"))
            {
                skuVariant.InnerXml = string.Empty;
            }
            else
            {
                skuVariantCode = "#" + skuVariantCode;
            }
            XmlNode DistiSKU = doc.CreateElement("DistiSKU");
            DistiSKU.InnerXml = xmlEncode(this.QuoteItem.SKUVariant.DistiSku);
            returnValue.AppendChild(DistiSKU);
        }
        else
        {
            errorMessages.Add("skuvariant was nothing");
        }

        returnValue.AppendChild(skuVariant);


        //Dim SellerPartNum As XmlNode = doc.CreateElement("SellerSKU")
        // SellerPartNum.InnerText = iq.channelSKU(buyeraccount.SellerChannel)

        XmlNode OPTTYPE = doc.CreateElement("OptType"); //note Case difference (consistenet with other XML tags but DIFFERENT from the attribute name
        if (Product.i_Attributes_Code.ContainsKey("optType"))
        {
            string opt = System.Convert.ToString(this.QuoteItem.ShortName(language));
            //  opt$ = Product.i_Attributes_Code("optType")(0).Translation.text(language)
            OPTTYPE.InnerXml = xmlEncode(opt);
        }
        else if (Product.isSystem)
        {
            OPTTYPE.InnerXml = xmlEncode(Xlt("System unit", language));

        }
        returnValue.AppendChild(OPTTYPE);

        XmlNode qty = doc.CreateElement("Quantity");
        qty.InnerText = this.Quantity.ToString(); //not we do NOT use the quoteItems quantity - but the FlatListItems quantity (which may be a consolidation)
        returnValue.AppendChild(qty);

        XmlNode Desc = doc.CreateElement("Description");

        //from here - was innertext (for some reason)
        //        If Product.i_attributes_code.containskey("Name") Then
        // Desc.InnerXml = xmlEncode(Product.i_Attributes_Code("Name")(0).Translation.text(s_lang))
        // Else
        if (Product.i_Attributes_Code.ContainsKey("desc"))
        {
            Desc.InnerXml = xmlEncode(Product.i_Attributes_Code("desc")[0].Translation.text(language));
        }
        else if (Product.i_Attributes_Code.ContainsKey("Name"))
        {
            Desc.InnerXml = xmlEncode(Product.i_Attributes_Code("Name")[0].Translation.text(language));
        }
        else
        {
            Desc.InnerXml = string.Empty;
        }

        //End If
        returnValue.AppendChild(Desc);


        //NOTE UnitPrice and LinePrice and Multipleid by the (per item) margin (usually 1)
        XmlNode Price = doc.CreateElement("UnitPrice");
        Price.InnerXml = xmlEncode((this.QuoteItem.BasePrice.value * this.QuoteItem.Margin).ToString(CultureInfo.InvariantCulture));
        returnValue.AppendChild(Price);

        XmlNode linePrice = doc.CreateElement("LinePrice");
        if (!Information.IsDBNull(this.QuoteItem.BasePrice.value))
        {
            linePrice.InnerXml = xmlEncode((this.QuoteItem.BasePrice.value * this.Quantity * this.QuoteItem.Margin).ToString(CultureInfo.InvariantCulture)); //product.PriceVariants(buyeraccount).DisplayPrice().Text ' TODO - return price State/POA info

        }
        returnValue.AppendChild(linePrice);

        XmlNode lineRebate = doc.CreateElement("LineRebate");
        lineRebate.InnerXml = xmlEncode((this.QuoteItem.rebate).ToString(CultureInfo.InvariantCulture)); //mrp rebates are per line not per item.
        //lineRebate.InnerXml = xmlEncode((Me.QuoteItem.rebate * Me.Quantity).ToString(CultureInfo.InvariantCulture)) 'product.PriceVariants(buyeraccount).DisplayPrice().Text ' TODO - return price State/POA info

        returnValue.AppendChild(lineRebate);

        XmlNode linePromo = doc.CreateElement("OPG");
        linePromo.InnerXml = string.Empty;
        if (this.QuoteItem.OPG.value != null)
        {
            if (!Information.IsDBNull(this.QuoteItem.OPG.value))
            {
                linePromo.InnerXml = this.QuoteItem.OPG.value.ToString();
                if (opgs != null)
                {
                    opgs.Add(this.QuoteItem.OPG.value.ToString());
                }
            }
        }
        returnValue.AppendChild(linePromo);

        XmlNode listPrice = doc.CreateElement("ListPrice");
        //DisplayPrice(Me.QuoteItem.quote.BuyerAccount, errorMessages).Text)
        listPrice.InnerXml = xmlEncode(this.QuoteItem.ListPrice.value.ToString(CultureInfo.InvariantCulture));
        returnValue.AppendChild(listPrice);

        XmlNode stock = doc.CreateElement("Stock");
        string stockReturned = System.Convert.ToString(Product.CurrentStock(quote.BuyerAccount, 0, this.QuoteItem.SKUVariant, errorMessages));
        stock.InnerXml = xmlEncode(getStock(quote.BuyerAccount, long.Parse(stockReturned), true));
        returnValue.AppendChild(stock);




        if (QuoteItem.Branch.Product.isSystem)
        {
            foreach (var msg in this.QuoteItem.Msgs)
            {
                if (msg.message != null)
                {
                    if (msg.severity > EnumValidationSeverity.BlueInfo)
                    {
                        string textmsg = System.Convert.ToString(xmlEncode(msg.replaceVariables(msg.message.text(language), msg.variables)));
                        string[] variants = textmsg.Split(':');
                        string productSku = variants[0];
                        if (!productSkus.Contains(productSku))
                        {
                            productSkus.Add(productSku);
                            if (variants.Count() > 1)
                            {
                                textmsg = string.Format("{0}>>{1}", productSku, variants[1]);
                            }
                            else
                            {
                                textmsg = string.Format("{0}", variants);
                            }

                            XmlNode Advice = doc.CreateElement("Advice");
                            returnValue.AppendChild(Advice);
                            //the sku is a duplication of the lline sku - however, having it here makes the code much neater elsewhere (outptting the advice)
                            XmlNode SKU = doc.CreateElement("SKU");
                            SKU.InnerXml = this.QuoteItem.Branch.Product.sku;
                            Advice.AppendChild(SKU);
                            XmlNode severity = doc.CreateElement("Severity");
                            severity.InnerXml = msg.severity.ToString();
                            Advice.AppendChild(severity);

                            XmlNode adviceIcon = doc.CreateElement("AdviceIcon");
                            adviceIcon.InnerXml = msg.imagename;
                            Advice.AppendChild(adviceIcon);
                            XmlNode text = doc.CreateElement("Text");
                            text.InnerXml = textmsg;
                            Advice.AppendChild(text);

                        }


                    }
                }
            }
        }


        //Some of the external text
        if (this.QuoteItem.Branch.Product.i_Attributes_Code.ContainsKey("xText"))
        {
            List<ClsValidationMessage> vmsgs = Product.getXtext(this.QuoteItem.Path, this.QuoteItem.quote.AcknowledgedValidations);
            foreach (var msg in vmsgs)
            {
                if (msg.message != null)
                {
                    string textmsg = System.Convert.ToString(xmlEncode(clsIQ.CleanString(msg.message.text(quote.BuyerAccount.Language))));
                    string[] variants = textmsg.Split(':');
                    string productSku = variants[0];
                    if (!productSkus.Contains(productSku))
                    {
                        productSkus.Add(productSku);
                        if (variants.Count() > 1)
                        {
                            textmsg = string.Format("{0}>>{1}", productSku, variants[1]);
                        }
                        else
                        {
                            textmsg = string.Format("{0}", variants);
                        }
                        XmlNode Advice = doc.CreateElement("Advice");
                        returnValue.AppendChild(Advice);

                        XmlNode SKU = doc.CreateElement("SKU");
                        SKU.InnerXml = this.QuoteItem.Branch.Product.sku;
                        Advice.AppendChild(SKU);
                        XmlNode severity = doc.CreateElement("Severity");
                        severity.InnerXml = msg.severity.ToString();
                        Advice.AppendChild(severity);
                        XmlNode adviceIcon = doc.CreateElement("AdviceIcon");
                        adviceIcon.InnerXml = msg.imagename;
                        Advice.AppendChild(adviceIcon);
                        XmlNode text = doc.CreateElement("Text");
                        text.InnerXml = textmsg;
                        Advice.AppendChild(text);
                    }
                }
            }
        }


        if (this.QuoteItem.Note.value != DBNull.Value)
        {
            XmlNode Note = doc.CreateElement("Note");
            returnValue.AppendChild(Note);

            XmlNode SKU = doc.CreateElement("SKU");
            SKU.InnerXml = this.QuoteItem.Branch.Product.sku;
            Note.AppendChild(SKU);

            XmlNode text = doc.CreateElement("Text");
            text.InnerXml = xmlEncode(this.QuoteItem.Note.value.ToString());
            Note.AppendChild(text);
        }

        return returnValue;
    }
    /// <summary>
    /// Gets stock quantity or message in stock or out of stock for binarystock channels.
    /// </summary>
    /// <param name="account">an instance of clsAccount.</param>
    /// <param name="value">An integer value that represents the quantity of stock.</param>
    /// <param name="export">A boolean value that represents if export is being done.</param>
    /// <returns>A string object that represents the text or number to display in quote export.</returns>
    /// <remarks></remarks>
    private string getStock(clsAccount account, long value, bool export)
    {
        string result = string.Empty;
        if (account.SellerChannel.BinaryStock && value > 0)
        {
            result = System.Convert.ToString(InStock.text(account.Language));
        }
        else if (account.SellerChannel.BinaryStock && value <= 0)
        {
            result = System.Convert.ToString(OutOfStock.text(account.Language));
        }
        else if (!account.SellerChannel.BinaryStock && value > 0)
        {
            result = (value).ToString();
        }
        else if (!account.SellerChannel.BinaryStock && value <= 0)
        {
            if (export)
            {
                result = "0";
            }
            else
            {
                result = value.ToString();
            }
        }
        return result;
    }
    /// <summary>
    /// Returns a SmartQuote-format XML Node representing this line
    /// </summary>
    /// <param name="doc">The parent XMLDocument</param>
    /// <param name="quote">The quote</param>
    /// <param name="errorMessages">Error messages</param>
    /// <returns>The formatted XML Node</returns>
    /// <remarks></remarks>
    public XmlNode XMLSmartQuoteLine(XmlDocument doc, clsQuote quote, List<string> errorMessages)
    {
        XmlNode returnValue = default(XmlNode);

        clsLanguage language = quote.BuyerAccount.Language;

        returnValue = doc.CreateElement("EclipseLineItem");

        XmlAttribute attr = doc.CreateAttribute("TagText");
        attr.Value = "NA";
        returnValue.Attributes.Append(attr);

        attr = doc.CreateAttribute("ProductNumber");
        attr.Value = this.QuoteItem.SKUVariant.DistiSku;
        // Change any part numbers that end #xyz to use (tab)xyz
        //Dim s = Regex.Replace(Me.QuoteItem.SKUVariant.DistiSku, "^([a-zA-Z0-9]*)#([a-zA-Z0-9]{3})$", "$1{TAB}$2")
        //attr.Value = s.Replace("{TAB}", vbTab)
        returnValue.Attributes.Append(attr);

        attr = doc.CreateAttribute("Quantity");
        attr.Value = Quantity.ToString();
        returnValue.Attributes.Append(attr);

        attr = doc.CreateAttribute("Description");
        if (QuoteItem.Branch.Product != null)
        {
            attr.Value = xmlEncode(QuoteItem.Branch.Product.DisplayName(language, true));
        }
        else
        {
            attr.Value = string.Empty;
        }
        returnValue.Attributes.Append(attr);

        attr = doc.CreateAttribute("NetPrice");
        attr.Value = "0";
        returnValue.Attributes.Append(attr);

        attr = doc.CreateAttribute("IntegrationGroupCode");
        attr.Value = string.Empty;
        returnValue.Attributes.Append(attr);

        attr = doc.CreateAttribute("Level");
        attr.Value = (QuoteItem.Branch.Product.isSystem(QuoteItem.Path)) ? "1" : "2";
        returnValue.Attributes.Append(attr);

        return returnValue;
    }

}