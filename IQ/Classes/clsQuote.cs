using dataAccess;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;
using System.Threading;




public class clsQuote : ISqlBasedObject
{

    //Thoughts on SBSO
    //The root quote could be HP's version at list price
    //Each disti then 'picks it up' - makes their own version (updating pricing) - which contains *their* (customer specific) pricing - this copy get marked lost/won etc

    public int ID;
    public clsAccount BuyerAccount;
    public clsAccount AgentAccount;
    public nullableString Name;
    public nullableString Notes;
    public DateTime Created;
    public DateTime Updated;
    public bool Hidden;
    public bool Locked;
    public bool Saved;
    public clsState State;
    public clsQuoteItem RootItem; //The virtual root item in this quote (has no branch.. but has children)
    public clsQuote RootQuote; //Integer 'This is the 'original' quote (this quote is a version of) - starts out as itself
    public int Version;
    public clsCurrency Currency;
    public string Reference; //Used during the import .. possiby useful to expose as a customers reference in future

    public nullableString Description;
    //Public NumOptions As Integer
    public int NumAlternatives;
    public string keyword;

    public float TEMP_IMPORT_MARGIN; // don't use  !! margins are per QuoteItem now
    public int TEMP_IMPORT_MULTIPLIER; //The 'sytems' (multiplier) when the quote was exported .. we need to multiple all QuoteItemQuantites by this
    public NullablePrice QuotedPrice; //Total value of the WITH Margin - also hold a 'valid' member to say wether the quote includes any POA items, and the CURRENCY of the quote
    public decimal TotalRebate; //Single

    public clsQuoteItem MostRecent; //the last item flexed (up) or added - used to set a cssClass on the div, used in trun for the zoomer animation

    public int maxVersion; //stored on the root quote - (primarily so we know if there's more than one quote and wether to display the 'show all versions' button in the list of quotes)
    public clsQuoteItem Cursor; //the thing (system) into which we're adding things (options)


    private int FK_Import_Id;

    //Public Editable As Boolean = False

    public List<string> AcknowledgedValidations; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.


    public static bool CurrentQuoteContains(UInt64 lid, clsProduct product)
    {
        bool returnValue = false;

        returnValue = false;

        if (iq.SeshContains(lid, "QuoteID") && iq.sesh(lid, "QuoteID") != null)
        {
            int qid = 0;
            qid = System.Convert.ToInt32(iq.sesh(lid, "QuoteID")); //use the session variable
            if (qid != 0)
            {
                clsAccount agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));
                clsQuote quote = agentAccount.Quotes(qid);
                if (quote.RootItem.HasProduct(product))
                {
                    return true;
                }
            }
        }


        return returnValue;
    }



    public void delete()
    {

        this.BuyerAccount.Quotes.Remove(this.ID); //we dont have to remove the quote items - becuase removing the only refernece to the quote (here) - destroys all items

        object sql = null;
        sql = "DELETE FROM [QuoteItem] WHERE fk_quote_id=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);


        sql = "DELETE FROM [Quote] WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);


    }

    public void ExportLogging(string type)
    {

        object sql = null;
        sql = "INSERT INTO [dbo].[QuoteExport] ([FK_Quote_ID],[Type])  VALUES(" + System.Convert.ToString(this.ID) + ",\'" + type + "\')";
        da.DBExecutesql(sql, false);

    }


    public clsQuoteItem SetQtyByItemID(int itemID, int qty, bool Absolute, float MarginForNew, List<string> ErrorMessages)
    {

        //can probably be largely consolidated with SetQtyByPath.. but no time right now

        clsQuoteItem Item = null;
        this.RootItem.clearMessage(); //Clear all validation messages (recursively)

        Item = this.RootItem.FindRecursive(itemID);
        if (Item == null)
        {
            ErrorMessages.Add("Could not find quote item " + System.Convert.ToString(itemID) + " in " + System.Convert.ToString(this.ID));
        }
        else
        {
            if (Item.IsPreInstalled)
            {
                //we tried to flex a preinstalled item - you can't enter absolute values against presinstalled items in the quote


                //New - Handles -L21 > - B21 FIO's
                //  Dim altsku As String = Item.Branch.Product.FirstAttributeEnglishText("altsku")

                //if this preinstalled item has an altSKU - add one of those instead !
                //If altsku <> "" Then
                //    Dim atpath As String = Item.Parent.Path
                //    Dim altskubranch = Item.Parent.Branch.findChildBySKU2(Item.Parent.Path & "", altsku, atpath)
                //    Dim price As clsPrice = altskubranch.Product.GetPrices(BuyerAccount, BuyerAccount.SellerChannel.priceConfig, iq.AllVariants, ErrorMessages, True).First
                //    Return setQtyByPath(atpath, price.SKUVariant, 1, False, 1, ErrorMessages)
                //End If

                //see if there's an existing - non-preInstalled sibling
                clsQuoteItem existing = default(clsQuoteItem);
                existing = this.Cursor.FindRecursive(Item.Path, false);


                if (Item.validate)
                {
                    if (qty < 0)
                    {
                        Item.validate = false;
                    }
                    else
                    {
                        if (existing == null)
                        {

                            NullablePrice price = default(NullablePrice);
                            NullablePrice listprice = default(NullablePrice);
                            price = Item.SKUVariant.Price(BuyerAccount); //.Price

                            listprice = NullPrice(Item.Branch.Product.ListPrice(BuyerAccount), BuyerAccount.Currency);

                            //this is a fix for universal/Synnex demo - flexing basket itmes worked on the assumption the variant would have a price already - but  with Universal it's not necessarily so !
                            //it may be dangerous - (where prices are 'vivalid' fomr some other reason)
                            if (!price.isValid)
                            {
                                price = listprice;
                            }

                            //give these an order
                            int order = 0;
                            if (Item.Branch.Product.isSystem)
                            {
                                order = 2;
                            }
                            else
                            {
                                order = 4;
                            }

                            Item = new clsQuoteItem(this, Item.Branch, Item.SKUVariant, Item.Path, 1, price, listprice, false, Item.Parent, new nullableString(), new nullableString(), 0, MarginForNew, new nullableString(), order); //Item.order)
                        }
                        else
                        {
                            existing.Quantity += qty;
                        }
                    }
                }
                else
                {
                    if (qty > 0)
                    {
                        Item.validate = true; //place a preinstalled item back into validation
                    }
                    else
                    {
                        ErrorMessages.Add("* preinstalled unvalidated item qty<0");
                    }
                }
            }
            else
            {
                if (Absolute)
                {
                    Item.Quantity = qty;
                }
                else
                {
                    object quan = Item.Branch.Quantities.Where(q => q.Value.Region.Encompasses(this.BuyerAccount.BuyerChannel.Region)).FirstOrDefault;
                    if (quan.Value != null && quan.Value.MinIncrement != 0)
                    {
                        Item.Quantity += quan.Value.MinIncrement * qty;
                    }
                    else
                    {
                        Item.Quantity += qty;
                    }
                }

                if (Item.Quantity == 0)
                {
                    Item.Parent.Children.Remove(Item);

                    //fix for removing items and cursor focus
                    if (Item == this.Cursor)
                    {
                        if (Item.Parent.Children.Count > 0)
                        {
                            this.Cursor = Item.Parent.Children.First;
                            this.MostRecent = Item.Parent.Children.First;
                        }
                        else
                        {
                            this.Cursor = null;
                        }
                    }
                }

                Item.Update();
                if (Item.Quantity > 0)
                {
                    this.MostRecent = Item;
                }
            }
        }

        return Item;


    }

    public clsQuoteItem setQtyByPath(object path, clsVariant SKUvariant, int qty, bool Absolute, float margin, ref List<string> errormessages)
    {

        this.RootItem.clearMessage(); //Clear all validation messages (recursively)
        NullablePrice Price = default(NullablePrice); // = branch.Product.VariantPrice(BuyerAccount, SKUvariant)
        //this gets the list price for the a first variant

        clsBranch branch = iq.Branches(System.Convert.ToInt32(Strings.Split(System.Convert.ToString(path), ".").Last));

        //NB:- List Price can return NOTHING
        //Dim lp As clsPrice = branch.Product.ListPrice(BuyerAccount) 'branch.Product.listPrices(BuyerAccount.Currency)(0).Price

        //fetch the list price if there is one - or constuct a POA in the correct currency
        NullablePrice listprice = NullPrice(branch.Product.ListPrice(BuyerAccount), BuyerAccount.Currency);

        clsQuoteItem item = null;

        //Important - we flex quantities of exisiting OPTIONS under the currenct systen) BUT we add new SYSTEMS to the basket (when flexing by path)'
        //this means that the add buttons in the tree will add *instances* of systems (that can be configured individually)

        bool addingOption = branch.Product.isSystem(path) == false;
        if (this.Cursor != null && addingOption)
        {
            item = this.Cursor.FindRecursive(path, true); //see if the Current (cursored) quote item (system) already has (as a descendant)
            //                                              one of the items (by path) we're trying to set the quantity of...
        }

        List<clsPrice> prices = default(List<clsPrice>);
        prices = branch.Product.GetPrices(BuyerAccount, BuyerAccount.SellerChannel.priceConfig, SKUvariant, errormessages, true);

        bool blnAddSystem = true;

        if (!addingOption && this.RootItem.Children.Count > 0)
        {
            clsBranch firstSystemBranch = this.RootItem.Children(0).Branch;
            if (branch.Product.Manufacturer != firstSystemBranch.Product.Manufacturer)
            {
                blnAddSystem = false;
            }

        }


        if (blnAddSystem)
        {

            if (prices.Count == 0 || prices[0] == null)
            {
                errormessages.Add("No Price available");
            }
            else
            {
                Price = prices[0].Price;

                if (Price.NumericValue > listprice.NumericValue)
                {
                    errormessages.Add("* ! Customer price exceeds list price for " + SKUvariant.Product.sku + "(" + SKUvariant.DistiSku + ")");
                }

                if (Price.NumericValue == 0 && Price.isValid == true)
                {
                    errormessages.Add("* Price was valid but  0");
                }

                if (item == null)
                {
                    //we didn't have one (under the cursor) in the quote - so we add one

                    //this is a fix for universal/Synnex demo - flexing basket itmes worked on the assumption the variant would have a price already - but  with Universal it's not necessarily so !
                    //it may be dangerous - (where prices are 'invalid' fomr some other reason)

                    //REMOVED by nick - listprice could be nothing (and should already have been returned if it existed)
                    //If Not Price.isValid Then Price = listprice
                    ///'''


                    if (this.Cursor == null)
                    {
                        item = this.Additem(branch, SKUvariant, System.Convert.ToString(path), qty, Price, listprice, this.RootItem, margin, true, ref errormessages);
                    }
                    else
                    {
                        //is the thing we're adding/flexing compatible with (appears as a option under) the thing  at the quote cursor
                        if (Cursor.Compatible(path))
                        {

                            if (this.Cursor.ID == -99)
                            {
                                this.Cursor = this.RootItem; //part of the fix for bug 712 (may be redundant - but be carefull - harmless !!)
                            }
                            item = this.Additem(branch, SKUvariant, System.Convert.ToString(path), qty, Price, listprice, this.Cursor, margin, true, ref errormessages);
                        }
                        else
                        {
                            item = this.Additem(branch, SKUvariant, System.Convert.ToString(path), qty, Price, listprice, this.RootItem, margin, true, ref errormessages);
                            item.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.amberalert, iq.AddTranslation("Not compatible with the currently selected system", English, "VM", 0, null, 0, false), null, "", 0, 0, Strings.Split("")));
                        }
                    }

                }
                else
                {
                    //yes, we had one
                    if (item.IsPreInstalled)
                    {
                        //we've attempted to flex a preinstalled option
                        //see if we have a 'non-preinstalled' one too already

                        clsQuoteItem npItem = default(clsQuoteItem);
                        npItem = this.Cursor.FindRecursive(path, false);
                        int toadd = 0;
                        if (Absolute)
                        {
                            toadd = qty - item.Quantity;
                        }
                        else
                        {
                            toadd = qty; //for absolute.. make up to the desired quantity
                        }

                        if (toadd < 0) //flexing a preinstalled item down (remove from validation)
                        {
                            if (item.validate)
                            {
                                item.validate = false;
                            }
                            else
                            {
                                errormessages.Add("* Tried to flex down an item removed from validation");
                            }
                        }
                        else if (toadd > 0) //flexing up a preinstalled item
                        {
                            if (item.validate) //this preinstalled item included in vaidation
                            {
                                if (npItem == null)
                                {
                                    //no - make the non-preinstalled version (including *it's* preinstalled options
                                    npItem = this.Additem(branch, SKUvariant, System.Convert.ToString(path), toadd, Price, listprice, this.Cursor, margin, true, ref errormessages);
                                }
                                else
                                {
                                    //yes - flex the non-preinstalled version
                                    npItem.Quantity += toadd;
                                }
                            }
                            else
                            {
                                item.validate = true; //bring a pre-installed item back into validation
                            }
                        }
                        else
                        {
                            errormessages.Add("* toadd was 0 in setQtyByPath");
                        }
                    }
                    else
                    {
                        if (Absolute)
                        {
                            item.Quantity = qty;
                        }
                        else
                        {
                            item.Quantity += qty;
                        }

                        if (item.Quantity == 0)
                        {
                            item.Parent.Children.Remove(item);
                        }
                    }


                }
                if (item.Branch.Product.isSystem(path))
                {
                    this.MostRecent = item;
                    this.Cursor = item;
                }
            }
            if (item != null)
            {
                item.Update();
            }
        }

        return item;

    }


    private XmlNode FilledXMLHeader(XmlDocument doc, List<string> errormessages)
    {

        //Returns a populated header section for the XML quote export

        XmlNode h = default(XmlNode);
        h = doc.CreateElement("Header");

        XmlNode dateNode = doc.CreateElement("Date");
        dateNode.InnerText = DateTime.Now.ToString("dd MMM yyyy");
        h.AppendChild(dateNode);

        XmlNode Qid = doc.CreateElement("QuoteID");
        Qid.InnerText = this.RootQuote.ID.ToString();
        h.AppendChild(Qid);

        XmlNode QuoteName = default(XmlNode);
        QuoteName = doc.CreateElement("QuoteName");
        if (this.Name.value.ToString().Trim().Length == 0)
        {
            QuoteName.InnerText = this.RootQuote.ID.ToString();
        }
        else
        {
            QuoteName.InnerText = this.Name.sqlValue;
        }

        h.AppendChild(QuoteName);

        XmlNode Version = doc.CreateElement("Version");
        Version.InnerText = this.Version.ToString();
        h.AppendChild(Version);

        XmlNode currencyCode;
        currencyCode = h.AppendChild(doc.CreateElement("CurrencyCode"));
        currencyCode.InnerText = this.BuyerAccount.Currency.Code;

        XmlNode currencySymbol;
        currencySymbol = h.AppendChild(doc.CreateElement("CurrencySymbol"));
        currencySymbol.InnerText = this.BuyerAccount.Currency.Symbol;


        XmlNode Total = doc.CreateElement("Total");
        //Total.InnerText = Me.Currency.Symbol & Me.TotalPrice.sqlvalue 'this is the sum of all prices BEFORE margin

        NullablePrice tpim = new NullablePrice(this.Currency); //Total Price Including Margin

        this.RootItem.Totalise(tpim, TotalRebate, true); //fetch the total price INCLUDING margin
        string listPriceIndicator = string.Empty;
        if (tpim.isList)
        {
            listPriceIndicator = " *";
        }
        Total.InnerText = (System.Convert.ToDecimal(tpim.sqlvalue)).ToString(CultureInfo.InvariantCulture); //tpim.text(Me.BuyerAccount, errormessages)

        h.AppendChild(Total);

        XmlNode TR = doc.CreateElement("TotalRebate");
        TR.InnerText = this.TotalRebate.ToString(CultureInfo.InvariantCulture);
        h.AppendChild(TR);

        XmlNode GT = doc.CreateElement("QuoteTotal");
        //GT.InnerText = Me.Currency.Symbol & Me.TotalPrice.sqlvalue - Me.TotalRebate
        GT.InnerText = ((System.Convert.ToDecimal(tpim.sqlvalue)) - this.TotalRebate).ToString(CultureInfo.InvariantCulture);
        h.AppendChild(GT);

        XmlNode buyer = default(XmlNode);

        buyer = h.AppendChild(doc.CreateElement("Buyer"));

        buyer.AppendChild(doc.CreateElement("BuyerCompanyName")).InnerText = string.Empty; // Me.BuyerAccount.BuyerChannel.Name
        buyer.AppendChild(doc.CreateElement("BuyerCompanyID")).InnerText = string.Empty; //Me.BuyerAccount.BuyerChannel.ID.ToString
        buyer.AppendChild(doc.CreateElement("BuyerPersonName")).InnerText = string.Empty; //Me.BuyerAccount.User.RealName
        buyer.AppendChild(doc.CreateElement("BuyerPersonEmail")).InnerText = string.Empty; // Me.BuyerAccount.User.Email
        buyer.AppendChild(doc.CreateElement("BuyerPersonTelephone")).InnerText = string.Empty; // If(Me.BuyerAccount.User.tel1.DisplayValue = "IQ.nullableString", "", Me.BuyerAccount.User.tel1.DisplayValue)


        XmlNode Seller = default(XmlNode);
        Seller = h.AppendChild(doc.CreateElement("Seller"));
        Seller.AppendChild(doc.CreateElement("SellerPersonName")).InnerText = string.Format("{0}, {1} - {2}", this.AgentAccount.User.RealName, this.AgentAccount.User.Channel.Name, this.AgentAccount.User.Email); //'String.Empty ' Me.AgentAccount.User.RealName
        Seller.AppendChild(doc.CreateElement("SellerCompanyName")).InnerText = string.Empty; //String.Format("{0}", Me.AgentAccount.User.RealName)
        Seller.AppendChild(doc.CreateElement("SellerCompanyID")).InnerText = this.BuyerAccount.SellerChannel.ID.ToString();

        Seller.AppendChild(doc.CreateElement("SellerLogo")).InnerText = (this.BuyerAccount.SellerChannel.pic1.value).ToString(); //contains only the subpath and filename (for example /dist/LOGO_DABPL03228.jpg)

        Seller.AppendChild(doc.CreateElement("SellerLogoShort")).InnerText = Filename((this.BuyerAccount.SellerChannel.pic2.value).ToString()); //Just the filename parsed out... so we can get it from the media folder

        //'Seller.AppendChild(doc.CreateElement("SellerPersonName")).InnerText = String.Empty ' Me.AgentAccount.User.RealName
        Seller.AppendChild(doc.CreateElement("SellerPersonEmail")).InnerText = string.Empty; //Me.AgentAccount.User.Email
        Seller.AppendChild(doc.CreateElement("SellerPersonTelephone")).InnerText = string.Empty; //If(Me.AgentAccount.User.tel1.DisplayValue = "IQ.nullableString", "", Me.AgentAccount.User.tel1.DisplayValue)



        XmlNode Language;
        Language = h.AppendChild(doc.CreateElement("Language"));
        Language.InnerText = this.AgentAccount.Language.Code;

        return h;

    }

    private XmlNode FilledXMLSmartQuoteHeader(XmlDocument doc, clsQuoteItem quoteItem, List<string> errormessages)
    {

        //Returns a populated header section for the XML SmartQuote export

        clsLanguage enLanguage = (from l in iq.Languages.Values where l.Code == "EN" select l).First;
        string supplyChainCode = string.Empty;

        foreach (var productAttribute in quoteItem.Branch.Product.Attributes.Values)
        {
            if ((productAttribute.Translation != null) && (productAttribute.Translation.Group != null) && (productAttribute.Translation.Group == "SCC"))
            {
                supplyChainCode = System.Convert.ToString(productAttribute.Translation.text(enLanguage));
                break;
            }
        }

        XmlNode h = default(XmlNode);
        h = doc.CreateElement("EclipseHeader");

        XmlAttribute attr = doc.CreateAttribute("ConfigName");
        if (this.Name.v != null)
        {
            attr.Value = xmlEncode(this.Name.DisplayValue());
        }
        else
        {
            attr.Value = string.Empty;
        }
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("NetPrice");
        attr.Value = "0";
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("SKU");
        attr.Value = "TBD";
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("ConfigID");
        attr.Value = this.ID.ToString();
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("SupplyChain");
        attr.Value = supplyChainCode;
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("OrigApplication");
        attr.Value = "IQUOTE";
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("Country");
        attr.Value = this.BuyerAccount.SellerChannel.Region.Code;
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("PriceTerm");
        attr.Value = "DP";
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("CurrencyCode");
        attr.Value = this.BuyerAccount.Currency.Code_HP;
        h.Attributes.Append(attr);

        attr = doc.CreateAttribute("OpportunityID");
        attr.Value = string.Empty;
        h.Attributes.Append(attr);

        return h;

    }


    public PlaceHolder HtmlSummary(clsLanguage language, bool OnlyChanged, UInt64 lid, ref bool priceChanges, ref List<string> errorMessages)
    {
        PlaceHolder returnValue = default(PlaceHolder);

        //returns a summary table of the quote - if onlychanged is passed at true - only quote lines whos prices have changed (stock is now at a different price) will be returned.

        returnValue = new PlaceHolder();

        clsFlatList fl = new clsFlatList();
        Table tbl = new Table();
        tbl.CssClass = "quotePreview";

        List<clsVariant> toget = new List<clsVariant>();
        this.getPrice(toget, lid, ref errorMessages);

        //flatten the quote in summary form (without preinstalled options)
        //    For Each Li In Me.RootItem.Flattened(Consolidated, includePreinstalled, -1).items  'Flattened() returns a flat list of clsFlatListItems
        bool priceChanged = false;

        //If priceChanged Then

        //End If



        tbl.Rows.Add(summaryHeaders(language));
        bool linePriceChanged = false;
        TableRow tr = default(TableRow);
        foreach (var flatListItem in this.RootItem.Flattened(false, false, 0).items)
        {
            linePriceChanged = false;
            tr = flatListItem.HTMLTableRow(language, linePriceChanged, errorMessages);
            if ((OnlyChanged && linePriceChanged) || (!OnlyChanged))
            {
                tbl.Rows.Add(tr);
            }
            if (linePriceChanged)
            {
                priceChanges = true;
            }
        }


        returnValue.Controls.Add(tbl);

        //If we asked for only changed rows - and only have the one (tableheaders) row... return nothing
        if (OnlyChanged && tbl.Rows.Count == 1)
        {
            return null;
        }

        return returnValue;
    }

    public string SellerLogo()
    {

        return this.BuyerAccount.SellerChannel.pic2.value.ToString();

    }

    public TableHeaderRow summaryHeaders(clsLanguage language)
    {

        TableHeaderCell thc = default(TableHeaderCell);
        TableHeaderRow thr = new TableHeaderRow();


        //SKU
        thc = new TableHeaderCell();
        thc.Text = Xlt("Mfr Part No", language);
        thr.Cells.Add(thc);

        //Variant
        thc = new TableHeaderCell();
        thc.Text = Xlt("Variant", language);
        thr.Cells.Add(thc);

        //Opttype (no label required)
        thc = new TableHeaderCell();
        thr.Cells.Add(thc);


        //DESCRIPTION
        thc = new TableHeaderCell();
        thr.Cells.Add(thc);
        thc.Text = Xlt("Description", language);

        //LISTPRICE
        thc = new TableHeaderCell();
        thc.Text = Xlt("List Price", language);
        thr.Cells.Add(thc);


        //UNITPRICE
        thc = new TableHeaderCell();
        thc.Text = Xlt("Unit Price", language);
        thr.Cells.Add(thc);


        //PRICE CHANGE
        thc = new TableHeaderCell();
        thc.Text = Xlt("Price Change", language);
        thr.Cells.Add(thc);


        //QUANTITY
        thc = new TableHeaderCell();
        thc.Text = Xlt("Quantity", language);
        thr.Cells.Add(thc);


        //LINEPRICE
        thc = new TableHeaderCell();
        thc.Text = Xlt("Line Price", language);
        thr.Cells.Add(thc);


        //STOCK
        thc = new TableHeaderCell();
        thc.Text = Xlt("Stock", language);
        thr.Cells.Add(thc);

        return thr;


    }

    public XmlDocument XMLDoc(ref List<string> errorMessages, ref List<string> opgs)
    {
        XmlDocument returnValue = default(XmlDocument);

        XmlDeclaration dec = default(XmlDeclaration);
        XmlElement DocRoot = default(XmlElement);
        XmlNode header = default(XmlNode);
        XmlNode Body = default(XmlNode);
        XmlNode Footer = default(XmlNode);

        returnValue = new XmlDocument();
        dec = returnValue.CreateXmlDeclaration("1.0", null, null);
        returnValue.AppendChild(dec);
        DocRoot = returnValue.CreateElement("Quote");
        returnValue.AppendChild(DocRoot);

        header = this.FilledXMLHeader(returnValue, errorMessages);
        DocRoot.AppendChild(header);

        Body = DocRoot.AppendChild(returnValue.CreateElement("Body"));
        List<string> productSkus = new List<string>();

        //Flat - Bill Of materiales  (consolidated) version - doesn't include FIO's
        Body.AppendChild(this.XMLFlatList("FlatQuote", returnValue, true, false, errorMessages, productSkus, opgs));
        productSkus.Clear();
        //Tree version can have many rows with the same part number (ie. the same otion installed in several systems) and displays full structure of the quote
        Body.AppendChild(this.XMLFlatList("TreeQuote", returnValue, false, true, errorMessages, productSkus, opgs, true));

        // Body.AppendChild(Me.RootItem.XMLTreeList(Doc))
        Footer = DocRoot.AppendChild(returnValue.CreateElement("Footer"));

        XmlNode note = returnValue.CreateElement("Note");
        Footer.AppendChild(note);
        if (!(this.Notes == null))
        {
            note.InnerText = this.Notes.DisplayValue;
        }

        XmlNode advice = returnValue.CreateElement("Advice");


        return returnValue;
    }

    public XmlNode XMLFlatList(string NodeName, XmlDocument doc, bool Consolidate, bool includePreinstalled, List<string> errorMessages, List<string> productSkus, List<string> opgs, bool quote = false)
    {
        XmlNode returnValue = default(XmlNode);

        //Returns an XML Node - cotaining all quote lines
        //If 'consolidate' is set to true, All quote lines of the same SKU (and variant) are consolidated into one line (with a larger quantity)
        //If consolidate is false, A node st added for every quote line, with its quantity - and additional Nodes for ID and ParentID (which allow the actual herirachy to be reconstructed from the XML - should that ever prove necessary

        returnValue = doc.CreateElement(NodeName);
        if (quote)
        {
            foreach (var Li in this.RootItem.Flattened(Consolidate, includePreinstalled, -1, quote).items) //Flattened() returns a flat list of clsFlatListItems
            {
                returnValue.AppendChild(Li.XMLLine(doc, this, !Consolidate, errorMessages, productSkus, opgs));
            }
        }
        else
        {
            foreach (var Li in this.RootItem.Flattened(Consolidate, includePreinstalled, -1).items) //Flattened() returns a flat list of clsFlatListItems
            {
                returnValue.AppendChild(Li.XMLLine(doc, this, !Consolidate, errorMessages, productSkus, opgs));
            }
        }


        return returnValue;
    }

    public XmlDocument XMLDocSmartQuote(ref List<string> errorMessages)
    {
        XmlDocument returnValue = default(XmlDocument);

        XmlDeclaration xmlDeclaration = default(XmlDeclaration);
        XmlElement root = default(XmlElement);
        XmlNode eclipseHeader = default(XmlNode);
        XmlNode eclipseLineItems = default(XmlNode);

        returnValue = new XmlDocument();
        xmlDeclaration = returnValue.CreateXmlDeclaration("1.0", "utf-8", null);
        returnValue.AppendChild(xmlDeclaration);

        root = returnValue.CreateElement("EclipseHeaders");
        returnValue.AppendChild(root);

        foreach (var quoteItem in RootItem.Children)
        {

            // Create an EclipseHeader for each system in the quote
            if (quoteItem.Branch.Product != null)
            {

                if (quoteItem.Branch.Product.isSystem(quoteItem.Path))
                {

                    eclipseHeader = FilledXMLSmartQuoteHeader(returnValue, quoteItem, errorMessages);
                    root.AppendChild(eclipseHeader);

                    eclipseLineItems = eclipseHeader.AppendChild(returnValue.CreateElement("EclipseLineItems"));

                    foreach (clsFlatListItem flatListItem in quoteItem.Flattened(true, false, -1).items)
                    {
                        eclipseLineItems.AppendChild(flatListItem.XMLSmartQuoteLine(returnValue, this, errorMessages));
                    }

                }
            }
        }

        return returnValue;
    }

    public clsQuote CreateNextVersion(List<string> errorMessages)
    {
        return CreateNextVersion(null, 0, ref errorMessages);
    }

    public clsQuote CreateNextVersion(int? flexUpItem, int flexUpQuantity, ref List<string> errormessages)
    {

        //Return New clsquote(Me) 'calls the special constructor which bases one (cloned) quote upon another - incrementing the version - and updating the pricing (to what is current for the quote buyeraccount)


        //NB it's the AGENT that holds the quotes (not the buyer)
        int nextVersion = System.Convert.ToInt32(this.AgentAccount.MaxQuoteVersion(this.RootQuote) + 1);
        if (nextVersion == 1)
        {
            Debugger.Break(); //quote version must have been zero (ie - it wasnt' found under the buyeraccount)
        }

        clsQuote newQuote = new clsQuote(this.BuyerAccount, this.AgentAccount, this.RootQuote, DateTime.Now, DateTime.Now, nextVersion, iq.i_state_GroupCode("QT-#NW"), this.QuotedPrice, this.Currency, false, this.Hidden, true, this.Reference, this.Name, this.Description, this.TotalRebate);

        DeepCopy(newQuote, flexUpItem, flexUpQuantity, errormessages);

        return newQuote;
    }

    public clsQuote Copy(int? flexUpItem, int flexUpQuantity, ref List<string> errormessages)
    {
        if (this.RootItem.Children.Count == 0)
        {
            LoadItems(errormessages);
        }
        clsQuote newQuote = new clsQuote(this.BuyerAccount, this.AgentAccount, null, DateTime.Now, DateTime.Now, Version, iq.i_state_GroupCode("QT-#NW"), this.QuotedPrice, this.Currency, false, this.Hidden, false, string.Empty, new nullableString(), new nullableString(), this.TotalRebate);
        DeepCopy(newQuote, flexUpItem, flexUpQuantity, errormessages);

        return newQuote;
    }

    public void DeepCopy(clsQuote newQuote, int? flexUpItem, int flexUpQuantity, List<string> errormessages)
    {

        List<clsQuoteItem> descendants = this.RootItem.Descendants; //includes the root item itself

        //                                               original                copy
        Dictionary<clsQuoteItem, clsQuoteItem> copies = new Dictionary<clsQuoteItem, clsQuoteItem>();
        copies.Add(this.RootItem, newQuote.RootItem);

        clsQuoteItem parent = default(clsQuoteItem);

        foreach (var d in descendants)
        {
            if (d != this.RootItem)
            {
                parent = copies[d.Parent];

                NullablePrice buyPriceNow = default(NullablePrice);

                //unused - but for reference...
                NullablePrice BuyPriceBefore;
                BuyPriceBefore = d.BasePrice;

                List<clsPrice> pricesNow = d.Branch.Product.GetPrices(this.BuyerAccount, this.BuyerAccount.SellerChannel.priceConfig, d.SKUVariant, errormessages, true);
                if (pricesNow.Count > 0) //Some virtual products (CHASSIS brnches specifically) have no pricing (are free)
                {
                    buyPriceNow = pricesNow[0].Price; // 'VariantPrice(Me.QuoteItem.quote.BuyerAccount, Me.QuoteItem.SKUVariant)
                }
                else
                {
                    buyPriceNow = d.BasePrice; //will be O (chassis branches)
                }


                clsQuoteItem copy = new clsQuoteItem(newQuote, d.Branch, d.SKUVariant, d.Path, d.Quantity, buyPriceNow, d.ListPrice, d.IsPreInstalled, parent, d.OPG, d.Bundle, d.rebate, d.Margin, d.Note, d.order);
                if (flexUpItem == d.ID)
                {
                    copy.Quantity += flexUpQuantity;
                }
                copy.Update();
                copies.Add(d, copy);
            }
        }


    }

    //Public Sub New(QuoteToCopy As clsquote)

    //    'Clones the specified quote - to create a new one - with a new ID, version number, created and updated dates - but otherwise identical

    //    With QuoteToCopy

    //    End With
    //    Dim aquote As New clsquote(
    //    'Clone my rootitem (and, recursively all my child items) onto the new (cloned) quote
    //    Me.RootItem.Clone(aquote)

    //End Sub

    public clsQuote(clsAccount BuyerAccount, clsAccount AgentAccount, clsQuote RootQuote, DateTime Created, DateTime updated, int Version, clsState state, NullablePrice price, clsCurrency Currency, bool locked, bool hidden, bool saved, string reference, nullableString name, nullableString description, decimal totalRebate, bool BootStrap = false, DataTable writecache = null, int fk_Import_ID = 0)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        AcknowledgedValidations = new List<string>();


        //Only this overload features the importID which forms no part of the quote object - it's just written to the table to allow only those rows added in the latest import to be updated
        //for better import performance (see import.quotes())

        //Me.Editable = True 'Make this quote editable as its brand new, if its ever loaded again then it wont be editable
        if (Version == 0)
        {
            Debugger.Break();
        }

        if (RootQuote == null)
        {
            this.RootQuote = this;
        }
        else
        {
            this.RootQuote = RootQuote;
        }

        this.QuotedPrice = price; //total for quote including applied margin(s)
        this.FK_Import_Id = fk_Import_ID;

        this.BuyerAccount = BuyerAccount;
        this.AgentAccount = AgentAccount;
        this.Created = Created;
        this.Updated = Created;
        this.Locked = locked;
        this.Hidden = hidden;
        this.Saved = saved;
        this.Notes = Notes;
        this.TotalRebate = totalRebate;
        this.Version = Version;
        this.State = state;
        this.Name = name;
        this.Description = description;
        this.Currency = Currency;
        this.RootItem = new clsQuoteItem(this); //Each quote has a virtual root quoteItem with an ID of 0 placeholder (see special constructor)

        this.Reference = reference;
        this.Cursor = this.RootItem;
        this.MostRecent = this.RootItem; //becomes the target of the flying frames


        if ((!(writecache == null)) && BootStrap == false)
        {

            this.ID = -1; //they will get their true ID's next time they're loaded
            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();
            //note - we do not set the ID's - theyre auto generated
            row["FK_Account_ID_agent"] = AgentAccount.ID;
            row["FK_Account_ID_buyer"] = BuyerAccount.ID;
            row["Hidden"] = hidden;
            row["Locked"] = locked;
            row["Created"] = Created;
            row["Updated"] = DateTime.Now;
            row["Saved"] = saved;
            row["FK_quote_id_root"] = 1; //!!!!!!!!!!!!!!!!!!!!!!!!! All quotes are initially bulk imported pointing to quote #1 - we MUST (and do) subsequently update.
            row["FK_State_id"] = State.ID;
            row["Price"] = price.value; //This is the total quoted price Including margin (at differing rates on all the items)
            row["FK_currency_id"] = Currency.ID;
            row["version"] = Version;
            row["reference"] = reference;
            row["FK_import_ID"] = fk_Import_ID;
            row["totalrebate"] = totalRebate;

            writecache.Rows.Add(row);

        }
        else
        {
            this.ID = SQLInsert(BootStrap);

        }


        if (this.ID != -1) //note - bulk insered quotes won't be available via the agent account unti the OM is re-loaded
        {
            AgentAccount.Quotes.Add(this.ID, this);
            if (!iq.Quotes.ContainsKey(this.ID))
            {
                iq.Quotes.Add(this.ID, this);
            }
        }

    }

    public clsQuote(int ID, clsAccount BuyerAccount, clsAccount AgentAccount, clsQuote RootQuote, DateTime Created, DateTime updated, int Version, clsState state, NullablePrice price, clsCurrency Currency, bool locked, bool hidden, bool saved, string reference, nullableString name, nullableString description, decimal totalRebate)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        AcknowledgedValidations = new List<string>();



        this.ID = ID;
        this.BuyerAccount = BuyerAccount;
        this.AgentAccount = AgentAccount;
        this.Created = Created;
        this.Updated = updated;
        this.Locked = locked;
        this.Hidden = hidden;
        this.Saved = saved;
        this.Notes = Notes;
        this.Version = Version;
        this.TotalRebate = totalRebate;
        if (RootQuote == null)
        {
            this.RootQuote = this;
        }
        else
        {
            this.RootQuote = RootQuote;
        }


        this.State = state;
        this.QuotedPrice = price;

        this.Name = name;
        this.Description = description;
        this.Currency = Currency;
        this.Reference = reference;

        this.RootItem = new clsQuoteItem(this); //the placeholder  (see special constructor )
        this.Cursor = this.RootItem;
        this.MostRecent = this.RootItem; //becomes the target of the flying frames

        AgentAccount.Quotes.Add(this.ID, this);

        if (!iq.Quotes.ContainsKey(this.ID))
        {
            iq.Quotes.Add(this.ID, this); //put it in the root level dictionary too (for generic editing)
        }

    }

    public string ReplaceTags(string l, ref XmlDocument doc, ref List<string> errorMessages, clsLanguage language)
    {

        //returns a copy of l$ with the !TAGS! replaced with values from this quotes corresponding XML <tags> of the quotes XMLDoc()

        List<string> opgs = new List<string>();

        StringBuilder sb = new StringBuilder(l);
        doc = this.XMLDoc(ref errorMessages, ref opgs); //genertae and fetch the XML of this quote

        Regex regex = new Regex("\\|[A-z\\ 0-9]+\\|"); //ML - translate anything between |'s
        object matches = regex.Matches(l);
        foreach (Match m in matches)
        {
            sb.Replace(System.Convert.ToString(m.Value), System.Convert.ToString(iq.AddTranslation(m.Value.Trim("|".ToArray()), English, "Export", 0, null, 0, false).text(language)));
        }
        XmlNode header = doc.GetElementsByTagName("Header")[0]; //Contains the <QuoteID>,<Version>,<Date> and <Total> tags - whos contents will replace the respective !QuoteID!,!Date! and !Total! tags
        sb = setHeaderTags(sb, header, opgs);
        if (sb.ToString().Contains("Prepared for:-"))
        {
            sb.Replace("Prepared for:-", string.Empty);
        }
        if (sb.ToString().Contains("Opt Type"))
        {
            sb.Replace("Opt Type", string.Empty);
        }

        XmlNode Buyer = default(XmlNode);
        Buyer = doc.GetElementsByTagName("Buyer")[0];
        foreach (XmlNode item in Buyer.ChildNodes)
        {
            sb.Replace("!" + item.Name + "!", System.Convert.ToString(item.InnerText));
        }

        XmlNode Seller = default(XmlNode);
        Seller = doc.GetElementsByTagName("Seller")[0];
        foreach (XmlNode item in Seller.ChildNodes)
        {
            sb.Replace("!" + item.Name + "!", System.Convert.ToString(item.InnerText));
        }

        sb.Replace("!cols!", "20");
        sb.Replace("!cols-rep!", "11");

        sb.Replace("!TC!", System.Convert.ToString(this.AgentAccount.SellerChannel.Legal != null ? (HttpUtility.HtmlEncode(clsIQ.CleanString(this.AgentAccount.SellerChannel.Legal))) : string.Empty));
        string currencySetting = string.Format("{0}{1}{2}{3}{4}{5}{6}", "<number:currency-symbol number:language=\"", this.AgentAccount.Language.Code, "\" number:country=\"", this.AgentAccount.SellerChannel.Region.Code, "\">", this.Currency.Symbol, "</number:currency-symbol>");
        sb.Replace("!Currency!", currencySetting);
        //"
        //The following lines use (some very simple) XPATH to select nodes from the document - see http://www.w3schools.com/xpath/xpath_syntax.asp
        //the // prefix selects nodes from anywhere in the document


        XmlNode currencySymbol = doc.SelectSingleNode("Quote/Header/CurrencySymbol");
        XmlNodeList flatlines = doc.SelectSingleNode("//FlatQuote").SelectNodes("Line");
        flatlines.Item(0).ParentNode.AppendChild(currencySymbol); //add the currency from the root level of the quote into the nodelist so it can be used as a tag

        //Do it again for sheet 2 (but this time with indents)
        XmlNodeList treeLines = doc.SelectSingleNode("//TreeQuote").SelectNodes("Line");
        treeLines.Item(0).ParentNode.AppendChild(currencySymbol); //add the currency from the root level of the quote into the nodelist so it can be used as a tag

        sb = ReplaceTagsInRowContaining("!OptType!", sb, flatlines, false, opgs);
        sb = ReplaceTagsInRowContaining("!AdviceIcon!", sb, doc.SelectNodes("//TreeQuote/Line/Advice"), false, opgs);
        //Select *all* the advice tags under the TreeView of the quote
        sb = ReplaceTagsInRowContaining("!OptType!", sb, treeLines, true, opgs);
        sb = ReplaceTagsInRowContaining("!AdviceIcon!", sb, doc.SelectNodes("//TreeQuote/Line/Advice"), false, opgs);
        sb = ReplaceTagsInRowContaining("!Note!", sb, doc.SelectNodes("//TreeQuote/Line/Note"), false, opgs);

        XmlNodeList adviceNodes = doc.SelectSingleNode("//TreeQuote").SelectNodes("//Advice");
        sb = RemoveLabelByNodeCount(adviceNodes, "Advisory Notes", sb);

        XmlNodeList notes = doc.SelectSingleNode("//TreeQuote").SelectNodes("//Note");
        sb = RemoveLabelByNodeCount(notes, "Agent Notes", sb);
        if (opgs.Count == 0)
        {
            sb = setLablelsWhenOPGCountZero(doc, sb);
        }
        if (this.AgentAccount.SellerChannel.Universal)
        {
            sb = setUniversalLabels(doc, sb);
        }

        return sb.ToString();

    }
    /// <summary>
    ///  Set labels to hide when ppg count is zero.
    /// </summary>
    /// <param name="doc">An instance of XMLDocument.</param>
    /// <param name="sb">An instance of StringBuilder.</param>
    /// <returns>An instance of StringBuilder.</returns>
    /// <remarks></remarks>
    private StringBuilder setLablelsWhenOPGCountZero(XmlDocument doc, StringBuilder sb)
    {
        XmlNodeList opg = doc.SelectSingleNode("//TreeQuote").SelectNodes("//OPG");
        sb = RemoveLabelByNodeCount(opg, "OPG", sb, true);
        XmlNodeList lineRebate = doc.SelectSingleNode("//TreeQuote").SelectNodes("//LineRebate");
        sb = RemoveLabelByNodeCount(lineRebate, "Rebate", sb, true);
        XmlNodeList totalRebate = doc.SelectSingleNode("//TreeQuote").SelectNodes("//TotalRebate");
        sb = RemoveLabelByNodeCount(totalRebate, "Savings:", sb, true);
        XmlNodeList quoteTotal = doc.SelectSingleNode("//TreeQuote").SelectNodes("//QuoteTotal");
        sb = RemoveLabelByNodeCount(quoteTotal, "Quote Total:", sb, true);
        return sb;
    }

    /// <summary>
    /// Set labels to hide when uninversal.
    /// </summary>
    /// <param name="doc">An instance of XMLDocument.</param>
    /// <param name="sb">An instance of StringBuilder.</param>
    /// <returns>An instance of StringBuilder.</returns>
    /// <remarks></remarks>
    private StringBuilder setUniversalLabels(XmlDocument doc, StringBuilder sb)
    {
        XmlNodeList listPrice = doc.SelectSingleNode("//TreeQuote").SelectNodes("//ListPrice");
        XmlNodeList stock = doc.SelectSingleNode("//TreeQuote").SelectNodes("//Stock");
        XmlNodeList opg = doc.SelectSingleNode("//TreeQuote").SelectNodes("//OPG");
        XmlNodeList lineRebate = doc.SelectSingleNode("//TreeQuote").SelectNodes("//LineRebate");

        sb = RemoveLabelByNodeCount(listPrice, "List Price", sb, true);
        sb = RemoveLabelByNodeCount(stock, "Stock", sb, true);
        sb = RemoveLabelByNodeCount(opg, "OPG", sb, true);
        sb = RemoveLabelByNodeCount(lineRebate, "Rebate", sb, true);
        return sb;
    }
    /// <summary>
    /// Update header and total tags
    /// </summary>
    /// <param name="sb">An instance of StringBuilder.</param>
    /// <param name="header">An instance of XmlNode representing header.</param>
    /// <param name="opgs">An isntance of List of type string represntinng opgs on count.</param>
    /// <returns></returns>
    /// <remarks></remarks>
    private StringBuilder setHeaderTags(StringBuilder sb, XmlNode header, List<string> opgs)
    {
        string it = string.Empty;
        foreach (XmlNode item in header.ChildNodes)
        {
            it = System.Convert.ToString(item.InnerText);
            if ((item.Name.ToUpper == "TOTALREBATE" || item.Name.ToUpper == "QUOTETOTAL") && opgs.Count == 0)
            {
                it = " ";
            }
            sb.Replace("!" + item.Name + "!", it);
        }
        return sb;
    }
    /// <summary>
    /// Replaces label if count of nodes is 0 or forced by optional parameter removeheader.
    /// </summary>
    /// <param name="xmlNodelist">An instance of XMLNodeList. </param>
    /// <param name="label">A string object that represents the name of the label in the xml. </param>
    /// <param name="sb">An instance an StringBuilder.</param>
    /// <param name="removeHeader">a boolean value optional parameter default is true so will always remove label unless passedin as false.</param>
    /// <returns></returns>
    /// <remarks></remarks>
    private StringBuilder RemoveLabelByNodeCount(XmlNodeList xmlNodelist, string label, StringBuilder sb, bool removeHeader = true)
    {
        int i = 0;
        if (removeHeader == false)
        {
            foreach (XmlNode node in xmlNodelist)
            {
                i++;
                break;
            }
        }
        if (i == 0)
        {
            sb.Replace(label, string.Empty);
        }
        return sb;
    }
    //Private Function DuplicateAndReplaceTagsInRowContaining(tag$, ByVal l$, NodeList As XmlNodeList, WithIndents As Boolean) As String

    //    'extracts the  complete row containing tag$ - and duplicates it for each child  of node, replacing !tags! (in that row) with their corresponding xml nodes (children of Node)
    //    'For example - replaces the row with the !OptType! tag (a single row in the quote) with the flat quotes  child items (ie. all the rows in the quote)
    //    'also used to replace the !advice! tage with all advice lines

    //    'get a copy of the row that contains the original tag
    //    Dim rm As Integer = InStr(l$, tag)   'Find the position of the opttype tag - the row we will repeat many times
    //    Dim br As Integer = InStrRev(Left$(l$, rm), "<table:table-row")
    //    Dim er As Integer = InStr(br, l$, "</table:table-row")
    //    er = InStr(er, l$, ">")
    //    Dim il$ = Mid$(l$, br, er - br + 1)

    //    Dim nl$ = ""
    //    Dim qb$ = "" 'quote body

    //    Dim indent As Integer = 0
    //    Dim columnsToHide = "OPG LINEREBATE STOCK"
    //    For Each row As XmlNode In NodeList 'iterate the direct children for the <FlatQuote> (<line>s)
    //        nl$ = il$ 'make a copy of the unmolested (template) Item line (with the tags in)
    //        'replace each !TAG! with the contents of the correspondingly named child element

    //        If row.ChildNodes.Count > 0 Then  'don't process empt elements (such as empty advice tags)
    //            Dim iXML$
    //            For Each col As XmlNode In row.ChildNodes  'the cols are the things we're replacing in each row, EG SKU, Price, Description - or (for advice, AdviceIcon, Text)
    //                If WithIndents Then
    //                    '                                      was innertext (but DVD_+/- drives broke
    //                    If col.Name = "Indent" Then indent = CInt(col.InnerXml) 'on the 'breakdown' (non-bill of materials) view - the heirarchy of the quote is preserved/displayed - each row has an indent level
    //                End If


    //                '    txt = col.InnerText
    //                iXML = col.InnerXml  'We MUST use the innerXML

    //                'If InStr(txt, "&") Then Stop

    //                'If indent <> 0 And col.Name = "Description" Then txt$ = Strings.StrDup(indent - 1, "---") & txt$
    //                If indent <> 0 And col.Name = "Description" Then iXML = Strings.StrDup(indent - 1, "---") & iXML
    //                If Not (Me.AgentAccount.SellerChannel.Code = "HP" And columnsToHide.Contains(col.Name.ToUpper)) Then
    //                    nl$ = Replace(nl$, "!" & col.Name & "!", iXML)
    //                Else
    //                    nl$ = Replace(nl$, "!" & col.Name & "!", "")
    //                End If
    //            Next
    //            qb$ &= nl$
    //        End If
    //    Next

    //    Dim b$, e$
    //    b$ = Left$(l$, br - 1)
    //    e$ = Mid$(l$, er + 1)
    //    l$ = b$ & qb$ & e$

    //    Return l$

    //End Function
    private StringBuilder ReplaceTagsInRowContaining(string tag, StringBuilder l, XmlNodeList NodeList, bool WithIndents, List<string> opgs)
    {

        //extracts the  complete row containing tag$ - and duplicates it for each child  of node, replacing !tags! (in that row) with their corresponding xml nodes (children of Node)
        //For example - replaces the row with the !OptType! tag (a single row in the quote) with the flat quotes  child items (ie. all the rows in the quote)
        //also used to replace the !advice! tage with all advice lines

        //get a copy of the row that contains the original tag
        int br = 0;
        int er = 0;
        string il = GetTag(l, tag, ref er, ref br);
        StringBuilder nl = new StringBuilder(string.Empty);
        StringBuilder qb = new StringBuilder(string.Empty); //quote body

        int indent = 0;

        string columnsToHide = "OPG LINEREBATE STOCK LISTPRICE";
        foreach (XmlNode row in NodeList) //iterate the direct children for the <FlatQuote> (<line>s)
        {
            //make a copy of the unmolested (template) Item line (with the tags in)
            //replace each !TAG! with the contents of the correspondingly named child element
            nl = new StringBuilder(il);
            if (row.ChildNodes.Count > 0) //don't process empt elements (such as empty advice tags)
            {
                string iXML = "";
                foreach (XmlNode col in row.ChildNodes) //the cols are the things we're replacing in each row, EG SKU, Price, Description - or (for advice, AdviceIcon, Text)
                {
                    if (WithIndents)
                    {
                        // was innertext (but DVD_+/- drives broke
                        if (col.Name == "Indent")
                        {
                            indent = int.Parse(System.Convert.ToString(col.InnerXml)); //on the 'breakdown' (non-bill of materials) view - the heirarchy of the quote is preserved/displayed - each row has an indent level
                        }
                    }
                    iXML = System.Convert.ToString(col.InnerXml); //We MUST use the innerXML

                    if (!(this.AgentAccount.SellerChannel.Universal && columnsToHide.Contains(System.Convert.ToString(col.Name.ToUpper))))
                    {
                        nl = GetMainReplacements(nl, System.Convert.ToString(col.Name), iXML, opgs);
                    }
                    else
                    {
                        //Because the ODS file has formated columns currency must be set to space and replace float to string and then replace value with space.
                        nl = SetStockToFloatFromStringWhenHiding(nl, System.Convert.ToString(col.Name), false, true);
                        nl.Replace("!" + col.Name + "!", " ");
                    }
                }
                qb.Append(nl);
            }
        }


        return GetResultXmlString(br, er, qb.ToString(), l);

    }
    /// <summary>
    /// Gets replacements for stringh builder
    /// </summary>
    /// <param name="nl">An instance of StringBuilder that represents part of the xml.</param>
    /// <param name="colname">An instance of String that represents the Column Name to check or value to be changed</param>
    /// <param name="value">A string object that the value for the column at that point. </param>
    /// <param name="opgs">an instance of List of type string.</param>
    /// <returns>An instance of StringBuilder</returns>
    /// <remarks></remarks>
    private StringBuilder GetMainReplacements(StringBuilder nl, string colname, string value, List<string> opgs)
    {
        if (this.AgentAccount.SellerChannel.BinaryStock)
        {
            nl = SetStockToFloatFromStringWhenHiding(nl, colname, true, false);
        }
        if (opgs.Count == 0)
        {
            nl = SetLineRebateWhenNoOPGSwhenHiding(nl, colname);
        }
        else
        {
            nl.Replace("!" + colname + "!", value);
        }
        nl.Replace("!" + colname + "!", value);
        return nl;
    }
    /// <summary>
    /// This is to replace float with string in the attributes in the line of XML
    /// </summary>
    /// <param name="nl">An instance of StringBuilder that represents the part of the xml.</param>
    /// <param name="colname">An instance of String that represents the Column Name to check.</param>
    /// <param name="binarystock">A boolean value that represents whether the channel uses binarystock.</param>
    /// <param name="universal">A boolean value that represents whether the channel is universal.</param>
    /// <returns>An instance of an StringBuilder.</returns>
    /// <remarks>This need to be done as if string empty or space is replaced then zero will be dipslayed.</remarks>
    private StringBuilder SetStockToFloatFromStringWhenHiding(StringBuilder nl, string colname, bool binarystock, bool universal)
    {
        if (colname.ToUpper() == "STOCK")
        {
            //<table:table-cell office:value-type="float" office:value="!Stock!" table:style-name="ce-right">
            string x = " office:value-type=" + "\u0022" + "float" + "\u0022" + " office:value=" + "\u0022" + "!" + colname + "!" + "\u0022" + " table:style-name=" + "\u0022" + "ce-right" + "\u0022" + ">";
            int i = nl.ToString().IndexOf(x);
            if (i > 0)
            {
                if (binarystock || universal)
                {
                    string y = x.Replace("float", "string");
                    nl.Replace(x, y);
                    if (binarystock)
                    {
                        string z = y.Replace(" table:style-name=" + "\u0022" + "ce-right" + "\u0022", string.Empty);
                        nl.Replace(y, z);
                    }

                }
            }
        }
        return nl;
    }
    private StringBuilder SetLineRebateWhenNoOPGSwhenHiding(StringBuilder nl, string colname)
    {
        if (colname.ToUpper() == "LINEREBATE")
        {
            //'<table:table-cell office:value-type="currency" table:style-name="ce-currency" office:value="!LineRebate!">
            string x = " office:value-type=" + "\u0022" + "currency" + "\u0022" + " table:style-name=" + "\u0022" + "ce-currency" + "\u0022" + " office:value=" + "\u0022" + "!" + colname + "!" + "\u0022" + ">";
            int i = nl.ToString().IndexOf(x);
            if (i > 0)
            {

                string y = x.Replace("currency", "string");
                nl.Replace(x, y);
                string z = y.Replace(" table:style-name=" + "\u0022" + "ce-string" + "\u0022", string.Empty);
                nl.Replace(y, z);
                nl.Replace("!" + colname + "!", " ");

            }
        }
        return nl;
    }
    /// <summary>
    /// Puts all the xml back together.
    /// </summary>
    /// <param name="br">An integer representing a postion in the oringinal XML.</param>
    /// <param name="er">An integer representing a postion in the oringinal XML.</param>
    /// <param name="qb">An instance of String that represents the new parts of the xml.</param>
    /// <param name="l">An instance of StringBuilder that represents the whole of the xml.</param>
    /// <returns>An instance of an StringBuilder.</returns>
    /// <remarks></remarks>
    private StringBuilder GetResultXmlString(int br, int er, string qb, StringBuilder l)
    {
        string b = string.Empty;
        string e = string.Empty;
        StringBuilder sb = new StringBuilder(string.Empty);
        b = l.ToString().Substring(0, br - 1);
        e = l.ToString().Substring(er + 1);
        sb.Append(b + qb + e);
        return sb;
    }
    /// <summary>
    /// Gets string from stringbuilder with tag to look for.
    /// </summary>
    /// <param name="sb">An instance of a stringBuilder.</param>
    /// <param name="tag">An instance of a string. </param>
    /// <returns>An instance of an string.</returns>
    /// <remarks></remarks>
    private string GetTag(StringBuilder sb, string tag, ref int er, ref int br)
    {
        int rm = sb.ToString().IndexOf(tag); //Find the position of the opttype tag - the row we will repeat many times
        br = System.Convert.ToInt32(sb.ToString().Substring(0, rm).LastIndexOf("<table:table-row"));
        er = sb.ToString().IndexOf("</table:table-row", br);
        er = sb.ToString().IndexOf(">", er);
        string il = sb.ToString().Substring(br, er - br + 1);
        return il;
    }

    public clsQuoteItem Additem(clsBranch branch, clsVariant SKUVariant, string path, int qty, NullablePrice price, NullablePrice listPrice, clsQuoteItem ParentItem, float margin, bool withAutoAdds, ref List<string> errorMessages)
    {

        //Makes a new (hierarchical) quote item - complete with a set of preinstalled sub items! - AND adds it to the quote
        //under the right parent item (in the quote) for this option (NB: - it's direct parent (in the product tree) - eg 'Hard Disk Drives' may not be in the quote)

        clsQuoteItem item = default(clsQuoteItem);

        if (ParentItem == null)
        {
            Debugger.Break();
        }



        // If price.NumericValue > listPrice.NumericValue Then Stop 'todo remove


        //systems go at the begining - options go on the end  ' this makes the basket act like a stack
        int ord = 0;
        if (ParentItem.Children.Count == 0)
        {
            ord = 100;
        }
        else
        {
            if (branch.Product.isSystem && path.Split('.').Length < 6)
            {
                //systems go on the top
                ord = System.Convert.ToInt32((from ch in ParentItem.Children select ch.order).Min - 10);
            }
            else
            {
                //options go on the bottom
                ord = System.Convert.ToInt32((from ch in ParentItem.Children select ch.order).Max + 10);
            }
        }

        //synnex demo /universal  'fix'
        if (!price.isValid)
        {
            price = listPrice;
        }


        item = new clsQuoteItem(this, branch, SKUVariant, path, qty, price, listPrice, false, ParentItem, new nullableString(), new nullableString(), 0, margin, new nullableString(), ord);

        // Debug.Print("Additem")
        //we do not pass a quantity to AddPreinstalled - those are determined by the quantity records
        addPreinstalledRecursive(item, item.Branch, System.Convert.ToString(item.Path), withAutoAdds, ref errorMessages);



        // If Me.RootItem Is Nothing Then Me.RootItem = item

        return item;

    }


    /// <summary>
    /// 'Returns a placholder containing the entyire quotes UI
    /// </summary>
    /// <remarks>Largely by calling Rootitem.UI</remarks>
    /// <returns>A big lump of HTML</returns>

    public PlaceHolder UI(HashSet<string> foci, UInt64 lid)
    {
        PlaceHolder returnValue = default(PlaceHolder);

        List<string> errorMessages = new List<string>();

        returnValue = new PlaceHolder();
        // Dim ph As New Panel

        //Dim tabstrip As Panel
        //tabstrip = New Panel

        //tabstrip.ID = "quoteBasketTabStrip"

        // tabstrip.Controls.Add(MakeTab("Breakdown", viewtype))
        //tabstrip.Controls.Add(MakeTab("Summary", viewtype))
        //tabstrip.Controls.Add(MakeTab("Validation", viewtype))

        //OutputHTML.Controls.Add(tabstrip)

        //Focus is a list of the product sets we're viewing (eg. Receta)
        //If viewtype = "Breakdown" Then

        //Quote Totals
        UpdateDescAndPrice();

        Panel subheader = new Panel();
        subheader.ID = "quouteSubHeader";
        returnValue.Controls.Add(subheader);

        //the UI of the rootItem includes the quoteHeader

        returnValue.Controls.Add(this.RootItem.UI(true, BuyerAccount, AgentAccount, foci, errorMessages, false, lid)); //, iq.sesh(lid,"quoteCursor"))
        OutputErrors(returnValue.Controls, errorMessages, lid);


        TextBox txtPartsList = new TextBox();
        txtPartsList.TextMode = TextBoxMode.MultiLine;
        txtPartsList.Attributes("style") += " height:0px;"; //this hides it - but still renders it into the page (if you set visible to false - the control isn't rendered into the page !)

        returnValue.Controls.Add(txtPartsList);
        txtPartsList.ID = "txtPartsList";

        txtPartsList.Text = this.RootItem.FlatListTXT(BuyerAccount); //for copy to clipboard we output the equivilent of the BOM view (ie. installe doption qunaities are multiplied by the system quantities)
        returnValue.Controls.Add(txtPartsList);

        //the copy to clipboard button already exists a a static control in the page - and looks like this
        //<asp:Button ID="BtnCopy" runat="server" Text="Copy" cssclass="textButton" ToolTip="Copy the quote parts list to the ClipBoard" onmousedown="copyToClipBoard(txtPartsList);return false;"/>

        // OutputHTML.Controls.Add(lit) <may have missed something here

        return returnValue;
    }

    /// <summary>
    /// Builds a hierarchical quote from a flat, delimited list.
    /// </summary>
    /// <param name="lid"></param>
    /// <param name="txt"></param>
    /// <param name="errorMessages"></param>
    /// <returns>A list of warnings (not to be confused with errrorMessages) (unrecognised/disallowed parts)</returns>
    /// <remarks></remarks>
    public static List<string> FromShoppingList(UInt64 lid, clsAccount AgentAccount, clsAccount BuyerAccount, string txt, ref List<string> errorMessages, ref string FirstSysPath)
    {

        List<string> msgs = new List<string>(); //warinings/exceptions to show the user

        if (txt.IndexOf(Constants.vbCr) + 1 > 0)
        {
            Interaction.Beep();
        }
        txt = txt.Replace("\r\n", Constants.vbCr); //Switch all delimiters to CR's
        txt = txt.Replace(",", Constants.vbCr);
        txt = txt.Replace(";", Constants.vbCr);

        string[] p = txt.Split(Constants.vbCr.ToCharArray()[0]);
        string[] b = null;

        int qty = 0;
        string partno = "";

        int ln = 0;

        clsProduct product = null;
        clsProduct currentSystem = null;

        //We defualt these to the root - for option searches (ie, where the first part in the list is an option, not a system)
        clsBranch systemBranch = iq.RootBranch;
        string systemPath = "tree.1";

        clsBranch optionBranch = null;
        string optionPath = "";
        clsQuoteItem SystemQuoteItem = null;

        //build a hashset from the CD list stored in the sesstion variable
        HashSet<string> foci = new HashSet<string>(Strings.Split(System.Convert.ToString(iq.sesh(lid, "foci")), ",").ToList);


        clsQuote quote = null;
        if (iq.SeshContains(lid, "QuoteID") && System.Convert.ToInt32(iq.sesh(lid, "QuoteID")) != 0)
        {

            //if we have a quote on the go, It will add to it - may want to change this behaviour (discussed 06/06/2013 with Dan)
            int qid = System.Convert.ToInt32(iq.sesh(lid, "QuoteID"));
            quote = AgentAccount.Quotes(qid);
        }
        else
        {


            //moved to only create the (new) quote when we hit the virst valid item
            //Dim nullprice As NullablePrice 'The quote will start life with an unknown price
            //nullprice = New NullablePrice(bi.buyerAccount.Currency)
            //quote = New clsQuote(bi.BuyerAccount, bi.AgentAccount, Nothing, Now, Now, CInt(1), iq.i_state_GroupCode("QT-#NW"), nullprice, bi.BuyerAccount.Currency, False, False, False, "", New nullableString(), New nullableString())
            //iq.sesh(bi.lid, "QuoteID") = quote.ID

        }

        // Resolve the list of products first so we can check for HPE/HPI splits before adding anything to the quote
        List<clsShoppingListItem> products = new List<clsShoppingListItem>();
        int hpeCount = 0;
        int hpiCount = 0;
        bool splitFound = false;
        foreach (var QtyPartno in p)
        {

            ln++;

            b = QtyPartno.Split('*');
            partno = string.Empty;
            product = null;

            try
            {
                if (b.Count() == 2)
                {
                    if (string.IsNullOrWhiteSpace(b[1]))
                    {
                        b[1] = "1";
                    }
                    if (iq.i_SKU.ContainsKey(b[0].Trim()) && Val(b[1]) > 0 && Val(b[1]) < 9999)
                    {
                        partno = b[0].Trim();
                        qty = int.Parse(b[1]);
                    }
                    else if (iq.i_SKU.ContainsKey(b[1].Trim()) && Val(b[0]) > 0 && Val(b[0]) < 9999)
                    {
                        partno = b[1].Trim();
                        qty = int.Parse(b[0]);
                    }
                    else
                    {
                        msgs.Add(Xlt("Line", AgentAccount.Language) + ln + " is invalid:" + QtyPartno + " is unrecognised");
                    }
                }
                else if (b.Count() == 1)
                {
                    //quantityless line
                    if (b[0].Trim() != "")
                    {
                        if (iq.i_SKU.ContainsKey(b[0].Trim()))
                        {
                            qty = 1;
                            partno = b[0].Trim();
                        }
                        else
                        {
                            msgs.Add(Xlt("Line ", AgentAccount.Language) + ln + Xlt(" is invalid: ", AgentAccount.Language) + QtyPartno + Xlt(" unrecognised", AgentAccount.Language));
                        }
                    }
                }
            }
            catch (Exception)
            {
                msgs.Add(Xlt("Line ", AgentAccount.Language) + ln + Xlt(" is invalid: ", AgentAccount.Language) + QtyPartno + Xlt(" unrecognised", AgentAccount.Language));
            }

            if (msgs.Count > 0)
            {
                return (msgs);
            }

            if (!string.IsNullOrWhiteSpace(partno))
            {
                product = iq.i_SKU(partno);
            }

            if (!(product == null))
            {

                if (product.Manufacturer != Manufacturer.Unknown && product.Manufacturer != AgentAccount.Manufacturer)
                {

                    msgs.Add(Xlt("Line ", AgentAccount.Language) + ln + Xlt(" is invalid: ", AgentAccount.Language) + QtyPartno + Xlt(" unrecognised", AgentAccount.Language));
                    splitFound = true;
                    break;
                }


                //If product.Manufacturer = Manufacturer.HPE Then
                //    hpeCount += 1
                //ElseIf product.Manufacturer = Manufacturer.HPI Then
                //    hpiCount += 1
                //End If

                //If hpiCount > 0 AndAlso hpeCount > 0 Then
                //    ' HPE/HPI split found - reject the shopping list
                //    msgs.Add(Xlt("Mixed quotes for PPS and EG detected. Please select which product set you wish to carry through to quote.", AgentAccount.Language))
                //    splitFound = True
                //    Exit For
                //End If

                products.Add(new clsShoppingListItem(QtyPartno, partno, qty, product));

            }

        }
        if (splitFound)
        {
            return (msgs);
        }

        NullablePrice Price = null;
        NullablePrice listPrice = default(NullablePrice);
        List<clsPrice> prices = default(List<clsPrice>);
        clsPrice customerPrice = default(clsPrice); // nullablePrice
        string sysProduct = string.Empty;
        string optionProduct = string.Empty;
        int sysQty = 0;
        int optQty = 0;
        Dictionary<string, clsBranch> locations = default(Dictionary<string, clsBranch>);
        ln = 0;
        foreach (var shoppingListItem in products)
        {

            ln++;

            object QtyPartno = shoppingListItem.QtyPartNo;
            product = shoppingListItem.Product;
            partno = System.Convert.ToString(shoppingListItem.PartNo);
            qty = System.Convert.ToInt32(shoppingListItem.Quantity);

            if (product.isSystem)
            {
                sysProduct = partno;
                sysQty = qty;
                optQty = 0;
            }
            else
            {
                optionProduct = partno;
                optQty = qty;
            }

            if (sysQty > 0 & optQty > 0)
            {

                if (sysQty > optQty || (optQty % sysQty) != 0)
                {
                    msgs.Add(Xlt("Line ", AgentAccount.Language) + ln + Xlt(" There is a mismatch between system quantity and option quantity. ", AgentAccount.Language) + System.Convert.ToString(QtyPartno) + Xlt(" with ", AgentAccount.Language) + sysProduct);
                    if (quote != null)
                    {
                        quote.delete();
                        quote = null;
                    }
                    iq.sesh(lid, "QuoteID") = null;
                }

            }

            if (msgs.Count == 0)
            {
                //    listPrice = product.ListPrice(buyeraccount).Price
                //fetch the list price if there is one - or constuct a POA in the correct currency
                listPrice = NullPrice(product.ListPrice(BuyerAccount), BuyerAccount.Currency);

                prices = product.GetPrices(BuyerAccount, BuyerAccount.SellerChannel.priceConfig, iq.AllVariants, errorMessages, true);

                if (prices.Count == 0 || prices[0] == null)
                {
                    msgs.Add(Xlt("Line", AgentAccount.Language) + ln + " " + partno + Xlt("No customer specific or list price variant available - item cannot be added", AgentAccount.Language));
                }
                else
                {
                    customerPrice = Utility.LowestPrice(prices);

                    if (customerPrice.SKUVariant == null)
                    {
                        //A product for which we have no disti variant or HP 'standard'/list price
                        msgs.Add(Xlt("Line", AgentAccount.Language) + ln + " " + partno + Xlt("No customer specific or list price variant available - item cannot be added", AgentAccount.Language));
                    }
                    else
                    {

                        //OK there's something vialid in the shopping list - NOW create a quote
                        if (quote == null)
                        {
                            NullablePrice nullprice = default(NullablePrice); //The quote will start life with an unknown price
                            nullprice = new NullablePrice(BuyerAccount.Currency);
                            quote = new clsQuote(BuyerAccount, AgentAccount, null, DateTime.Now, DateTime.Now, 1, iq.i_state_GroupCode("QT-#NW"), nullprice, BuyerAccount.Currency, false, false, false, string.Empty, new nullableString(), new nullableString(), 0);
                            iq.sesh(lid, "QuoteID") = quote.ID;

                            iq.sesh(lid, "paradigm") = enumParadigm.configuringSystem;
                            //bi.Paradigm = enumParadigm.configuringSystem - ML Removed, orry if this breaks it but I dont think it will, needed to remove the dependancy on bi for the solution store system
                        }

                        if (product.isSystem)
                        {
                            HashSet<clsBranch> fruitlessGrafts = new HashSet<clsBranch>();
                            locations = iq.RootBranch.findProductBranches("tree.1", BuyerAccount.SellerChannel, product, false, fruitlessGrafts, true);
                            currentSystem = product;
                            systemPath = System.Convert.ToString(locations.Keys(0));
                            systemBranch = locations.Values(0);

                            if (string.IsNullOrEmpty(FirstSysPath))
                            {
                                FirstSysPath = systemPath;
                            }

                            List<string> hrs = systemBranch.ReasonsForHide(BuyerAccount, foci, systemPath, BuyerAccount.SellerChannel.priceConfig, false, errorMessages);
                            if (hrs.Count > 0)
                            {
                                msgs.Add("Line " + System.Convert.ToString(ln) + " System " + partno + " is not available to you (" + string.Join(",", hrs.ToArray) + ")");
                            }
                            else
                            {
                                //HP Split changes
                                // SK - This is (one instance of?) the backstop "basket interceptor" for split; It shouldn't be possible to get here,
                                // but previously this logic was being driven by the MFR of the first product into the basket, not the account's MFR
                                if (product.Manufacturer == AgentAccount.Manufacturer)
                                {
                                    SystemQuoteItem = quote.Additem(systemBranch, customerPrice.SKUVariant, systemPath, qty, customerPrice.Price, listPrice, quote.RootItem, 1, false, ref errorMessages);
                                }
                                else
                                {
                                    msgs.Add("Line " + System.Convert.ToString(ln) + " System " + partno + " can\'t be added as this would create a mixed quote and PPS/EG products need to be quoted separately.");
                                }
                            }
                        }
                        else //it's an option
                        {
                            //If an option is required outside of a system searching the entire tree for it is not feasible (searching grafts might be)
                            HashSet<clsBranch> fruitlessGrafts = new HashSet<clsBranch>(); //used to (massively) accelerate the search (by searching each non fruitful branch only once)
                            if (systemPath == "tree.1")
                            {


                                errorMessages.Add("Please make sure your list starts with a system SKU");
                            }
                            else
                            {
                                locations = systemBranch.findProductBranches(systemPath, BuyerAccount.SellerChannel, product, true, fruitlessGrafts, true); //will cross subsystems
                                if (locations.Count == 0)
                                {
                                    msgs.Add("Line " + System.Convert.ToString(ln) + " " + partno + " is not a compatible option for system " + currentSystem.SKU);
                                }
                                else
                                {
                                    optionPath = System.Convert.ToString(locations.Keys(0));
                                    optionBranch = locations.Values(0);
                                    List<string> hrs = optionBranch.ReasonsForHide(BuyerAccount, foci, optionPath, BuyerAccount.SellerChannel.priceConfig, false, errorMessages);
                                    if (hrs.Count > 0)
                                    {
                                        msgs.Add("Line " + System.Convert.ToString(ln) + " Option " + partno + " is not available to you (" + string.Join(",", hrs.ToArray) + ")");
                                    }
                                    else
                                    {
                                        clsQuoteItem addto = default(clsQuoteItem);
                                        if (SystemQuoteItem != null)
                                        {
                                            addto = SystemQuoteItem;
                                        }
                                        else
                                        {
                                            addto = quote.RootItem;
                                        }
                                        qty = (int)((double)qty / sysQty);
                                        quote.Additem(optionBranch, customerPrice.SKUVariant, optionPath, qty, customerPrice.Price, listPrice, addto, 1, false, ref errorMessages);
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }


        if (quote != null && quote.RootItem.Children.Count > 0)
        {
            iq.sesh(lid, "quoteCursor") = quote.RootItem.Children(0).ID;
            List<string> err2 = new List<string>();
            List<clsVariant> toget = new List<clsVariant>();

            //only do this if there's a webservice to call
            if (System.Convert.ToBoolean(BuyerAccount.SellerChannel.priceConfig & 8))
            {
                quote.getPrice(toget, lid, ref err2);
            }

        }



        return msgs;

    }

    private string @lock = "";

    /// <summary>Performs all validations  on an entrie quote</summary>
    /// <param name="buyerAccount">The account for which pricing should be displayed</param>
    /// <param name="agentAccount">The agent doing the quote (determines the language of the UI)</param>
    /// <remarks></remarks>
    public void validate(UInt64 lid, clsAccount buyerAccount, clsAccount agentAccount, List<string> errorMessages, bool calcTotalRebate = true)
    {

        //Validation (of the whole quote - sets/limits the flexbuttons and adds per item messages)
        //Validate slot usage

        //We validate each root item independently
        //this is to deal with he UDIMM/RDIMM thing and allows slots (UDIMM/RDIMM for example) to be 'given' by componenents earlier in the walk,
        //and 'taken' by non descendant items later in the walk

        //we should store/restore the dicslots as we recurse past systems as, at present there is a potential for a subSYSTEM to 'give' slots 'up' to a parent system
        //whilst the resulting configuration would be valid (in terms of there would be enough slots in the complete configuration to accommodate all the otpions)
        //The quote hierarchy would not reflect how you would actually have to plug it together - ie. you may show 10 drives in a slave device with only 8 bays.


        lock (@lock) //don't validate twice, this was causing double ups on the validation messages
        {

            this.RootItem.clearMessage(); //recursively clears the warning messages on every quote item (from the root)

            this.RootItem.ValidateFill(agentAccount); //(recursively) Checks the required fill level is satisfied for each slot

            List<clsQuoteItem> systemItems = default(List<clsQuoteItem>);
            systemItems = this.RootItem.findSystemItems;

            foreach (clsQuoteItem I in systemItems) //Validate each "system" independently - don't cross system boundaries (don't recurse into other systems)
            {

                Dictionary<clsSlotType, clsSlotSummary> dicslots = new Dictionary<clsSlotType, clsSlotSummary>();

                I.ValidateSlots2(dicslots, true); //Recursive ! - compiles (and uses internally the quotes dicslots) =- Gives
                I.ValidateSlots2(dicslots, false); //Now for takes, to fill fallbacks

                string summary = "";

                //for debug only
                foreach (var kvp in dicslots)
                {
                    summary += "Mj:" + kvp.Key.MajorCode + " Mn:" + kvp.Key.MinorCode + " Gvn:" + kvp.Value.Given + " Tkn:" + kvp.Value.taken + "\r\n";
                }

                I.dicslots = dicslots;
                ClsValidationMessage message = default(ClsValidationMessage);
                foreach (var slotType in dicslots.Keys)
                {
                    Dictionary with_1 = dicslots[slotType];


                    //So are we validating this??
                    object vi = iq.ValidationInclusions.Where(vai => vai.Value.MajorCode.ToLower == slotType.MajorCode.ToLower).FirstOrDefault;
                    if (!(vi.Value == null))
                    {


                        string sum = slotType.Translation.text(AgentAccount.Language) + Xlt(" Given:", AgentAccount.Language) + with_1.Given + Xlt(" Taken:", AgentAccount.Language) + with_1.taken;
                        if (with_1.taken > with_1.Given)
                        {
                            clsTranslation tl = default(clsTranslation);
                            //TLissue

                            //Me.FlexButtonState = EnumHideButton.Up  'Stop them attempting to add more
                            //            'look through every option in the product tree under the system to find those that would offer more slots of this type
                            //(adds ClsValidationMessages to the item)
                            //Do we have any preinstalled slots taken

                            EnumValidationSeverity severity = EnumValidationSeverity.RedCross;
                            if (vi.Value.InclusionType == enumInclusionType.Unvalidated) //This one is from paul, a CHK item appears to mean chassis hardware kit which is unvalidated and should result in disclaimer warning....
                            {
                                if (I.IsPreInstalled)
                                {
                                    continue;
                                }
                                tl = iq.AddTranslation("Some elements of this quote are unvalidated, please check carefully", English, string.Empty, 0, null, 0, false);
                                severity = EnumValidationSeverity.Question;
                                //m = New ClsValidationMessage(EnumValidationSeverity.Question, tl, iq.AddTranslation("Unvalidated", English, "ISSUES", 0, Nothing, 0, False), "", 0, 0, {}, st.MajorCode)
                            }
                            else
                            {

                                //Sadly, treat power differently
                                if (slotType.MajorCode == "PWR")
                                {
                                    if (I.SystemItem.Branch.Product.ProductType.Code == "NBK")
                                    {
                                        tl = iq.AddTranslation("Power usage", English, "", 0, null, 0, false);
                                    }
                                    else
                                    {
                                        tl = iq.AddTranslation("Maximum load reached. Upgrade your power supply or remove options", English, string.Empty, 0, null, 0, false);
                                    }
                                }
                                else
                                {
                                    if (with_1.PreInstalledTaken > 0)
                                    {
                                        if (slotType.MajorCode == "MEM" && dicslots.Where(ds => ds.Key.MajorCode == "CPU").Count() > 0 && dicslots.Where(ds => ds.Key.MajorCode == "CPU").Sum(ds => ds.Value.Given) > 1)
                                        {
                                            tl = iq.AddTranslation("Not enough %1 slots available Max: %3 (%2 Used), Add more CPU\'s to enable more memory slots", English, "ISSUES", 0, null, 0, false);
                                        }
                                        else
                                        {
                                            tl = iq.AddTranslation("Not enough %1 slots available Max: %3 (%2 Used), Remove preinstalled to free more slots", English, "ISSUES", 0, null, 0, false);
                                        }
                                    }
                                    else
                                    {
                                        tl = iq.AddTranslation("Not enough %1 slots available Max: %3 (%2 Used)", English, "ISSUES", 0, null, 0, false);
                                    }
                                }
                            }
                            //Do we display capacity or slots
                            string display = "";
                            if (slotType.MajorCode.ToLower == "pwr")
                            {
                                //Special case for power as its a hybrid :(
                                display = string.Format("{0}W", dicslots[slotType].taken.ToString());
                            }
                            else if (dicslots[slotType].TotalCapacity != 0)
                            {
                                //Show capacity and units
                                display = string.Format("{0} {1}", dicslots[slotType].TotalCapacity.ToString(), (dicslots[slotType].CapacityUnit != null) ? (dicslots[slotType].CapacityUnit.Code) : "");
                            }
                            else
                            {
                                //Show quantity
                                display = string.Format("x {0}", dicslots[slotType].taken);
                            }

                            //Do we need to validate power.  If there is no power supply installed and there are no power supplied available under all options then it seems stupid to enforce this, we are talking about blade servers or all in one desktops mainly

                            if (slotType.MajorCode.ToUpper == "PWR" && (I.SystemItem.Branch.Product.ProductType.Code == "NBK" || I.SystemItem.Branch.findSlotGivers(I.SystemItem.Path, iq.i_slotType_Code("PWR").First.Value, false).Count() == 0))
                            {
                                severity = EnumValidationSeverity.BlueInfo;
                                tl = iq.AddTranslation("Power usage", English, string.Empty, 0, null, 0, false);
                            }
                            //Not(slotType.MajorCode.ToUpper = "PWR" AndAlso I.Branch.Product.ProductType.Code <> "SVR")
                            bool skip = slotType.MajorCode.ToUpper == "PWR" && I.Branch.Product.ProductType.Code != "SVR"; //oh god more hacking, PWR validation should only be done on servers SVR
                            if (!skip)
                            {
                                message = new ClsValidationMessage(enumValidationMessageType.Validation, severity, tl, iq.AddTranslation("%1 %4", English, "ISSUES", 0, null, 0, false), string.Empty, 0, 0, new[] { slotType.shortDisplayName(AgentAccount.Language), dicslots[slotType].taken.ToString(), dicslots[slotType].Given.ToString(), display }, slotType.MajorCode);
                                int shortFall = System.Convert.ToInt32(with_1.taken - with_1.Given);
                                I.resolveOverFlows(slotType, shortFall, buyerAccount, message, errorMessages, lid, dicslots);
                                I.Msgs.Add(message);
                            }
                        }
                    }
                }

                //

                //Validate minimum/preferred increments...
                //firstly - get a consolidated count of all items by branch (mostly to group preinstalled and user selected items together)
                Dictionary<string, int> dicItemCount = default(Dictionary<string, int>);
                dicItemCount = new Dictionary<string, int>();

                I.CountItems(dicItemCount); //NB: Items excluded from validation are not counted !
                //           I.CompatibleSiblings()

                //now validate all branches - using the (branch) counts in the dictionary
                I.ValidateIncrements(dicItemCount, agentAccount, errorMessages);
                I.validateExclusivity();
                I.doWarnings(dicslots);

                //Mop up adding pre-installed which are ok - Removed, spec is being redesigned ML
                //For Each p In I.Children ' Branch.preInstalled(buyerAccount, I.Path, errorMessages)
                //    If I.Msgs.Where(Function(msg) msg.slotTypeMajor IsNot Nothing AndAlso msg.slotTypeMajor.ToLower() = p.Branch.Product.ProductType.Code.ToLower()).Count = 0 Then
                //        If If(p.validate, p.Quantity, 0) > 0 Then
                //            I.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.greenTick, Nothing, iq.AddTranslation("%1 x %2", English, "ISSUES", 0, Nothing, 0, False), "", 0, 0, Split(p.Branch.Product.ProductType.Translation.text(agentAccount.Language) & "," & If(dicslots.Where(Function(ds) ds.Key.MajorCode.ToLower() = p.Branch.Product.ProductType.Code.ToLower()).Count > 0, dicslots.Where(Function(ds) ds.Key.MajorCode.ToLower() = p.Branch.Product.ProductType.Code.ToLower()).First().Value.taken.ToString(), p.Quantity.ToString()), ",")))
                //        End If

                //    End If
                //Next

                //Add Power
                //If I.Msgs.Where(Function(msg) msg.slotTypeMajor IsNot Nothing AndAlso msg.slotTypeMajor.ToLower() = "pwr").Count = 0 Then I.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.greenTick, Nothing, New clsTranslation(English, "Power Consumption " & dicslots(iq.i_slotType_MinorCode("W")).taken.ToString & "W."), "", 0, 0, {}))

                // DictSlots is keyed by instances of clsSlotType - which are the 'minor' (more granular) type
                I.specPanelOpen = SpecUI(lid, I, dicslots, "PWR,MEM,CPU,HDD,OPT,PSU".Split(',').ToList, true, BuyerAccount.Language); //shows the amount of memoy, Watts, CPU's and HDD capacity
                I.specPanelClosed = SpecUI(lid, I, dicslots, "PWR,MEM,CPU,HDD,OPT,PSU".Split(',').ToList, false, BuyerAccount.Language); //shows the amount of memoy, Watts, CPU's and HDD capacity

            }



            //Avalanche.. first step is to populate a dictionary of Systems>OPGRef>Qty  (count qualifying options by OPG under each system)
            Dictionary<clsProduct, Dictionary<ClsAvalancheOPG, int>> Qdic = default(Dictionary<clsProduct, Dictionary<ClsAvalancheOPG, int>>);
            Qdic = new Dictionary<clsProduct, Dictionary<ClsAvalancheOPG, int>>();
            this.RootItem.QualifyAvalanche(null, Qdic, this.BuyerAccount.SellerChannel.Region);
            //now calculate rebates on qualifying items (starting at the placeholding, branchless quote .rootitem
            this.RootItem.SetAvalancheRebate(null, Qdic);

            //Flex Attach
            Dictionary<clsProduct, Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>> QFDic = new Dictionary<clsProduct, Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>>(); //count the products by type, under each OPG
            //Me.RootItem.QualifyFlex(QFDic, Me.BuyerAccount.SellerChannel.Region, False, 0)
            bool systemValidationCheck = true;

            Dictionary<clsProductType, ClsValidationMessage> qualifyingProductTypes = new Dictionary<clsProductType, ClsValidationMessage>(); // Used to prevent multiple validation messages appearing for the same product type
            this.RootItem.FlexCalculations(QFDic, this.BuyerAccount.SellerChannel.Region, 0, systemValidationCheck, qualifyingProductTypes);
            //QFDIC now contains the number of products of each product type -
            // required (to qualify), present in the basket, and allowed to be discounted
            // - plus a 'used' 'working variable'..


            List<clsFlexOPG> io = new List<clsFlexOPG>(); //insuffient options
            foreach (var item in this.RootItem.Children)
            {
                if (QFDic.ContainsKey(item.Branch.Product))
                {
                    System.Object QFLDic = QFDic[item.Branch.Product];

                    foreach (var opg in QFLDic.Keys.ToList)
                    {
                        int options = 0;

                        bool hassystem = false;
                        bool hasCarePack = false;
                        foreach (var pt in QFLDic[opg].Keys)
                        {
                            if (pt.Code == "SVR" || pt.Code == "SWD") //This needs to be changed to include laptops, desktops and other SYSTEMS
                            {
                                hassystem = true;
                            }
                            else
                            {
                                //Don't count servers as options !
                                options += System.Convert.ToInt32(QFLDic[opg][pt].Total);
                            }
                        }

                        if (!hassystem)
                        {
                            QFLDic.Remove(opg);
                        }
                        else
                        {
                            if (options < opg.MinOptions)
                            {
                                string[] v = new string[2];
                                v[0] = (opg.MinOptions).ToString();
                                v[1] = (options).ToString();
                                clsTranslation vmtl = default(clsTranslation);
                                if (options == 0)
                                {
                                    vmtl = iq.AddTranslation("%1 Qualifying options", English, "VM", 0, null, 0, false);
                                }
                                else
                                {

                                    vmtl = iq.AddTranslation("%1 Qualifying options (%2 selected) ", English, "VM", 0, null, 0, false);
                                }

                                item.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesntQualify, null, vmtl, string.Empty, 0, 0, v));
                                systemValidationCheck = false;
                            }
                            else
                            {
                                string[] v = new string[2];
                                v[0] = (opg.MinOptions).ToString();
                                v[1] = (options).ToString();
                                clsTranslation vmtl = default(clsTranslation);

                                vmtl = iq.AddTranslation("%1 Qualifying options", English, "VM", 0, null, 0, false);
                                item.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, null, vmtl, "", 0, 0, v));

                            }
                        }
                    }
                }
            }

            //remove those OPG's for which we don't have sufficient options
            //For Each opg In io  'insuffient options
            // QFDic.Remove(opg)
            // Next
            foreach (var item in this.RootItem.Children)
            {
                if (item.Branch.Product.isSystem)
                {
                    if (QFDic.ContainsKey(item.Branch.Product))
                    {
                        if (calcTotalRebate)
                        {
                            item.SetFlexRebate(QFDic[item.Branch.Product], false, 0, systemValidationCheck, this.TotalRebate);
                        }
                        else
                        {
                            item.SetFlexRebate(QFDic[item.Branch.Product], false, 0, systemValidationCheck, 0);
                        }
                    }
                }
            }
            //  Me.RootItem.SetFlexRebate(QFDic)
        }

    }


    public void addPreinstalledRecursive(clsQuoteItem quoteItem, clsBranch branch, string path, bool withAutoAdds, ref List<string> errorMessages)
    {

        //This is one of the trickiest subs to get your head around.
        //It adds 'sub' items (to the quoteitem (typically, but not neccessarily a system) with their pre-installed quanities

        //AutoAdds are *chargable* (non Free Of Charge) items

        //quoteItem is the Item (typically system) we just added to the quote
        //Recursively creates child QuoteItems according to the default Quantities set out in the descendant branches of the (supplied) branch at the (supplied) path
        //(remember branches are not unique or distinct in the tree - only the path fully qualifies the quantity record (of the branch) we need to use.
        //NOTE: - this sub DOES NOT add a preinstalled quanitity to the quote Item - It adds 'sub' items (to the item) with pre-installed quanities

        //Also - don't be tempted to try and rewrite this to be a method on the quoteitem - you cannot recurse child items that do not (yet) exist.. so the 'obvious' structure doesn't work.

        string iPath = "";
        clsQuantity q = default(clsQuantity);
        clsQuoteItem childitem = default(clsQuoteItem);
        bool addedItem = false;

        bool finished = false;

        foreach (var b in branch.childBranches.Values) //These are the child branches of the BRANCH in the product catalogue (not the quoteItem!)
        {
            iPath = path + "." + Strings.Trim(System.Convert.ToString(b.ID.ToString()));

            //new nick 26/03/2015

            //  If b.deleted Then Exit Sub

            bool recurse = true;
            if (b.PruneInForce(iPath, quoteItem.quote.BuyerAccount.SellerChannel) > 0)
            {
                recurse = false; //  Exit Sub
            }




            //  Debug.Print(iPath)

            // was just 'region' (which wasn't valid)
            q = b.LocalisedQuantity(BuyerAccount.SellerChannel.Region, iPath, errorMessages); //returns the 'best' quantity record for this user - ie, the deepest, narrowest match

            //If branch.Product IsNot Nothing Then
            //    If branch.Product.sku = "748922-B21" Then Stop
            //End If

            // If q IsNot Nothing Then
            // If q.NumPreInstalled > 0 Then Stop
            // End If

            if (b.Product == null && !(q == null))
            {

                errorMessages.Add("* A (preinstalled) quantity record for the branch " + b.DisplayName(English) + " at path " + iPath + " was found - but this is a product-less branch !");
            }
            else
            {
                // :BUG - InsertItemPosition is using quanity record that don't matcc on path

                bool isPreInstalled = false;
                addedItem = false;
                childitem = quoteItem; // by default, we'll pass the same quoteitem down - until a new quoteitem is created
                if (!(q == null))
                {
                    clsVariant skuvariant = null;
                    NullablePrice listPrice = default(NullablePrice);
                    NullablePrice price = default(NullablePrice);
                    // If q.FOC Or withAutoAdds Then

                    //For the options we're autoadding - locate the best match against the quotitem (system) we're adding it to
                    //for example, get a french keyboard, from the same warehouse asys the sytem unit - for systems in france
                    skuvariant = b.Product.MatchingVariant(quoteItem.SKUVariant, BuyerAccount.SellerChannel);

                    //If skuvariant Is Nothing Then Stop - if there's no list price for something - skuvariant will be nothing
                    if (skuvariant == null)
                    {
                        price = new NullablePrice(BuyerAccount.Currency); //
                        listPrice = new NullablePrice(BuyerAccount.Currency); //
                        // errorMessages.Add("Preinstalled SKU variant was nothing (no list price )")
                    }
                    else
                    {
                        listPrice = NullPrice(skuvariant.Product.ListPrice(BuyerAccount), BuyerAccount.Currency);
                        price = skuvariant.Price(BuyerAccount); //.Price
                    }

                    //Else
                    //    listPrice = New nullablePrice(BuyerAccount.Currency)
                    // End If


                    if (q.NumPreInstalled > 0)
                    {
                        //creates the new child item and adds it to the item
                        // Preinstalled items are FOC (free of charge)
                        //Dim skuvariant As clsVariant = iq.i_variant_code("") 'TODO - May need further work - Installs only 'standard' FIO's

                        clsAccount buyerAccount = default(clsAccount);
                        buyerAccount = quoteItem.quote.BuyerAccount;

                        string g = System.Convert.ToString(b.Product.DisplayName(English));
                        if (q.FOC) //AndAlso skuvariant IsNot Nothing Then
                        {
                            if (skuvariant == null)
                            {
                                skuvariant = new clsVariant("", b.Product, buyerAccount.SellerChannel, b.Product.sku, "", "", "", iq.i_region_code("XW"), false); // iq.AllVariants ' New clsVariant() With {.DistiSku = "1234", .Product = b.Product, .Region = iq.i_region_code("XW"), .ID = -1, .i_prices = New Dictionary(Of clsChannel, Dictionary(Of clsCurrency, clsPrice))()}
                            }
                            // Dim zeroPrice As NullablePrice = New NullablePrice(CSng(0), quoteItem.quote.BuyerAccount.Currency, False)
                            if (skuvariant != null)
                            {
                                price = skuvariant.Price(buyerAccount);
                            }
                            isPreInstalled = true; //Free items are preinstalled (in the sense that they cannot be removed from the basket)
                        }
                    }
                    else
                    {
                        isPreInstalled = false; //non free items - are allowed to be removed

                        //for this quote item - this childbranch has a preinstalled quantity - find the best variant match
                        //so that, for example if you pick a french server with an autoadded keyboard, it will add a french keyboard
                        //and/or so that any (non-free) auto adds come from the same warehouse
                        if (skuvariant != null)
                        {
                            price = skuvariant.Price(BuyerAccount); //.Price
                        }
                    }


                    //synnex fix
                    if (!price.isValid)
                    {
                        price = listPrice;
                    }

                    if (skuvariant == null && !b.Translation.text(English).StartsWith("###"))
                    {
                        //could not locate a variant to auto add (no list price)
                        //   errorMessages.Add("* Could not locate a variant to auto add (no list price?)")
                    }
                    else
                    {
                        if (b.Product.ProductType.Code.ToUpper == "WTY")
                        {
                            int a = 8;
                        }
                        if (q.NumPreInstalled > 0) //Important (becuase the quantity may only specify max's and limits and NOT be an 'auto-add' per-se
                        {
                            int order = 3;
                            if (isPreInstalled)
                            {
                                order = 2; //FIOS should some above autoaddes
                            }
                            if (isPreInstalled || withAutoAdds)
                            {
                                childitem = new clsQuoteItem(quoteItem.quote, b, skuvariant, iPath, q.NumPreInstalled, price, listPrice, isPreInstalled, quoteItem, new nullableString(), new nullableString(), 0, 1, new nullableString(), order);
                            }

                        }
                    }

                }

                //childitem = New clsQuoteItem(quoteItem.quote, b, skuvariant, iPath, q.NumPreInstalled * qty, zeroprice, zeroprice, True, quoteItem, New NullableInt(), 0, Created, 0)
                //pass the new child down, with the next branch

                if (recurse)
                {
                    if (childitem == null)
                    {
                        errorMessages.Add("* Child item was nothing in AddPreinstalledRecursive");
                    }
                    else
                    {
                        addPreinstalledRecursive(childitem, b, iPath, withAutoAdds, ref errorMessages);
                        addedItem = true;
                    }

                    if (!addedItem) //pass myself down (for the attachment of subsequent children), with the next(deeper) branch
                    {
                        addPreinstalledRecursive(quoteItem, b, iPath, withAutoAdds, ref errorMessages);
                    }
                }
            }
        }

    }
    /// <summary>
    /// added in Me.TotalRebate check  because rebate is not take off price,  have now called validate, before this so rebate is worked out.The rebate is stored indivdually against each item in the quoteitem table when qulaifyling rules apply.
    /// </summary>
    /// <param name="recalculatePrice">optional boolean value true/ false.</param>
    /// <remarks></remarks>
    public void UpdateDescAndPrice(bool recalculatePrice = true)
    {

        if (!(this.Saved && this.Locked))
        {
            Pmark("Quote.updateDescAndPrice");

            //creates the rolled up description/headline value for the Quote - descriptions
            //needs to recurse
            if (!(this.RootItem == null))
            {
                string summary = "";
                summary = System.Convert.ToString(this.RootItem.summarise(0));
                //replace the trailing comma with a full stop.
                if (summary.Length > 0)
                {
                    StringType.MidStmtStr(ref summary, summary.Length, 1, ".");
                }
                this.Description = new nullableString(summary);
            }

            //Me.Value is a NullablePrice
            if (recalculatePrice)
            {
                this.QuotedPrice = new NullablePrice(this.Currency);
                this.QuotedPrice.isValid = true;
                this.TotalRebate = 0;

                NullablePrice tPrice = new NullablePrice(this.Currency);
                tPrice.isValid = true;
                //Me.RootItem.Totalise(Me.QuotedPrice, CDec(Me.TotalRebate), True) ' recurses all items to find a total cost - which also has a 'valid' member 'and isList' memeber
                this.RootItem.Totalise(tPrice, this.TotalRebate, true); // recurses all items to find a total cost - which also has a 'valid' member 'and isList' memeber
                if (tPrice.isTotal)
                {
                    tPrice.isValid = true;
                }

                this.QuotedPrice = tPrice;
                if (this.QuotedPrice.isList)
                {
                    this.QuotedPrice.isTotal = true; // Change the tooltip to stay 'includes list price elements'
                }
            }

            Pacc("Quote.updateDescAndPrice");
        }


    }

    public void Update(SqlClient.SqlConnection con = null, bool recalculatePrice = true)
    {

        UpdateDescAndPrice(recalculatePrice);

        //note: we don't need to sqlencode then nullablestring types (it's done in their SqlValue method)
        //note: the quote.price can be null (when there are no quote items)
        SQLUpdate();

    }


    public int LoadItems(List<string> errormessages)
    {

        //(re) creates the quotes RootItem - and then loads and attaches the heriarchy of quote items

        //the QuoteItems created timestamp is used to order them - so that when we re-constitute the tree - the parent items will alway be there
        string sql = "SELECT id,fk_branch_id,path,quantity,opg,bundle,rebate,fk_QuoteItem_Id_parent,price,listprice,ispreinstalled,fk_variant_id,created,margin,note,[order],validate";
        sql += " FROM quoteItem WHERE FK_Quote_ID=" + System.Convert.ToString(this.ID) + " order by Created";

        int count = 0;

        using (SqlClient.SqlConnection con = da.OpenDatabase())
        {

            using (SqlClient.SqlDataReader rdr = da.DBExecuteReader(con, sql))
            {

                int QiID;
                clsBranch Branch = default(clsBranch);
                string path = "";
                int Qty = 0;
                NullablePrice price = default(NullablePrice);
                NullablePrice listprice = default(NullablePrice);

                bool IsPreinstalled = false;
                clsQuoteItem Parent = null;
                nullableString opg = default(nullableString);

                DateTime created = default(DateTime);

                this.RootItem = new clsQuoteItem(this);

                clsQuoteItem anitem = default(clsQuoteItem);

                Dictionary<int, clsQuoteItem> flatitems = default(Dictionary<int, clsQuoteItem>);
                flatitems = new Dictionary<int, clsQuoteItem>();

                nullableString bundle = default(nullableString); //this and OPG are just (optional) text fields which appea on the quote
                if (rdr.HasRows)
                {
                    this.TotalRebate = 0;
                }

                while (rdr.Read)
                {

                    QiID = System.Convert.ToInt32(rdr.Item("id"));
                    Branch = iq.Branches(System.Convert.ToInt32(rdr.Item("fk_branch_id")));
                    path = (rdr.Item("Path")).ToString();
                    Qty = System.Convert.ToInt32(rdr.Item("Quantity"));

                    clsVariant skuVariant = null;

                    int vid = System.Convert.ToInt32(rdr.Item("fk_variant_id"));
                    if (iq.Variants.ContainsKey(vid))
                    {
                        skuVariant = iq.Variants(vid); //   Branch.Product.i_Variants(Me.BuyerAccount.SellerChannel)(rdr.Item("fk_variant_id"))

                        //recreate the quote items in the same currency as the quote
                        bool isListPrice = skuVariant.sellerChannel == HP;
                        price = new NullablePrice(rdr.Item("price"), this.Currency, isListPrice);
                        listprice = new NullablePrice(rdr.Item("listprice"), this.Currency, true);
                        created = System.Convert.ToDateTime(rdr.Item("created"));

                        opg = new nullableString(rdr.Item("opg"));
                        bundle = new nullableString(rdr.Item("bundle"));

                        decimal rebate = System.Convert.ToDecimal(rdr.Item("rebate"));
                        this.TotalRebate += rebate;
                        IsPreinstalled = System.Convert.ToBoolean(rdr.Item("isPreinstalled"));

                        if (Information.IsDBNull(rdr.Item("fk_quoteItem_id_parent")))
                        {
                            Parent = this.RootItem;
                        }
                        else
                        {
                            if (flatitems.ContainsKey(System.Convert.ToInt32(rdr.Item("fk_quoteitem_id_parent"))))
                            {
                                Parent = flatitems[System.Convert.ToInt32(rdr.Item("fk_quoteitem_id_parent"))];
                            }
                            else
                            {
                                Interaction.Beep();
                            }
                        }

                        anitem = new clsQuoteItem(System.Convert.ToInt32(rdr.Item("id")), this, Branch, skuVariant, path, Qty, price, listprice, IsPreinstalled, Parent, opg, bundle, rebate, created, System.Convert.ToSingle(rdr.Item("margin")), new nullableString(rdr.Item("note")), System.Convert.ToInt32(rdr.Item("order")), System.Convert.ToBoolean(rdr.Item("validate")));

                        flatitems.Add(System.Convert.ToInt32(rdr.Item("id")), anitem);
                        count++;

                    }
                    else
                    {
                        errormessages.Add("* Variant " + System.Convert.ToString(vid) + " was missing or not loaded");

                    }

                }

                this.UpdateDescAndPrice();

                rdr.Close();
            }

            con.Close();
        }


        return count;

    }


    public TableRow ListRow(clsQuote Quote, string[] css, UInt64 lid, bool islatest, List<string> errorMessages)
    {

        System.Object buyerAccount = (clsAccount)(iq.sesh(lid, "BuyerAccount"));
        System.Object language = ((clsAccount)(iq.sesh(lid, "AgentAccount"))).Language;
        //Quote.validate(lid, buyerAccount, AgentAccount, errorMessages)
        if (!Quote.QuotedPrice.isValid || Quote.QuotedPrice.NumericValue == 0)
        {
            Quote.UpdateDescAndPrice();
        }


        Panel previewpanel = new Panel();
        previewpanel.ID = "pv" + System.Convert.ToString(Quote.ID) + "-" + System.Convert.ToString(Quote.Version);

        TableRow tr = new TableRow();
        tr.Attributes("onclick") = "suck(\'" + "quotesummary.aspx?quoteid=" + System.Convert.ToString(Quote.ID) + "\',\'" + previewpanel.ID + "\');  popDialog(\'" + previewpanel.ID + "\', \'Quote " + System.Convert.ToString(Quote.RootQuote.ID) + " Version " + System.Convert.ToString(Quote.Version) + "\');";
        tr.CssClass = "quotesTableRow ";
        tr.ID = "Q" + this.ID.ToString();

        TableCell tc = new TableCell();

        bool isExpanded = false;
        if (iq.SeshContains(lid, "expandedQuotes"))
        {
            //If Me.AgentAccount.Quotes.ContainsKey(Me.RootQuote.ID) Then
            if (((List<int>)(iq.sesh(lid, "expandedQuotes"))).Contains(this.RootQuote.ID))
            {
                isExpanded = true;
            }
            // End If
        }
        tc = new TableCell();
        if (islatest)
        {
            if (isExpanded)
            {
                tc.Controls.Add(MakeRoundButton("minus.png", "Show only the latest version of this quote", "burstBubble(event);var savedP=false;  savedP= $(\'#SavedPanel\').attr(\'aria-expanded\'); showVersion(\'manipulation.aspx?command=CollapseQuoteVersions&RQID=" + System.Convert.ToString(Quote.RootQuote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
            }
            else
            {
                if (this.AgentAccount.MaxQuoteVersion(RootQuote) > 1)
                {
                    tc.Controls.Add(MakeRoundButton("plus.png", "Show all versions of this quote", "burstBubble(event);var savedP=false;  savedP= $(\'#SavedPanel\').attr(\'aria-expanded\');showVersion(\'manipulation.aspx?command=ExpandQuoteVersions&RQID=" + System.Convert.ToString(Quote.RootQuote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
                }
            }
            tr.CssClass += "latestVersion";
        }
        else
        {
            tr.CssClass += "oldVersion";
        }
        tr.Cells.Add(tc);

        if (!tr.CssClass.Contains("oldVersion"))
        {
            tr.Cells.Add(new TableCell() { CssClass = "quotesTableCell center", Text = Quote.RootQuote.ID.ToString() });
        }
        else
        {
            tr.Cells.Add(new TableCell());
        }

        tc = new TableCell() { CssClass = "quotesTableCell" };
        tr.Cells.Add(tc);
        tr.Cells(tr.Cells.Count - 1).Controls.Add(NewLit(Quote.Version.ToString() + (Quote.Locked ? "<div class=\'lockIcon\'/>" : "")));

        if (this.Saved)
        {
            tr.Cells.Add(new TableCell() { ID = "quotesList1Col-Name", CssClass = "quotesTableCell", Text = Quote.Name.DisplayValue() });
        }

        if (this.Saved)
        {
            tr.Cells.Add(new TableCell() { CssClass = "quotesTableCell", Text = "<img class=\'unpressedButton\' alt=\'" + Xlt("Rename", language) + "\' title=\'" + Xlt("Rename", language) + "\' src=\'/images/navigation/pencil.png\' onclick=\'burstBubble(event);renameQuote(" + System.Convert.ToString(this.ID) + ");return false;\'/>" });
        }

        //Console.WriteLine(myDateTime.ToString("d", CultureInfo.CreateSpecificCulture("ro-RO")))

        //This sulture should move to the account really
        tr.Cells.Add(new TableCell() { CssClass = "quotesTableCell", Text = Quote.Updated.ToString("d", CultureInfo.CreateSpecificCulture(System.Convert.ToString(buyerAccount.Culture.Code))) });
        //tr.Cells.Add(New TableCell() With {.CssClass = "quotesTableCell", .Text = Quote.Updated.ToShortDateString()})

        if (this.Saved)
        {
            tr.Cells.Add(new TableCell() { CssClass = "quotesTableCell", Text = Quote.State.Displayname(language) });
        }

        if (this.Saved)
        {
            tr.Cells.Add(new TableCell() { CssClass = "quotesTableCell center", Text = Quote.ExportHistory(true).Count.ToString() });
        }
        if (this.Saved)
        {
            tr.Cells(tr.Cells.Count - 1).Controls.Add(NewLit((Quote.ExportHistory(true).Count() > 0) ? ("<span class=\'spanLink\' onclick=\"burstBubble(event);showDialog(\'ExportedQuotes.aspx?quoteID=" + System.Convert.ToString(this.ID) + "&lid=" + System.Convert.ToString(lid) + "\');\">" + Quote.ExportHistory(true).Count.ToString() + "</span>") : (Quote.ExportHistory(true).Count.ToString())));
        }
        //New Nullable price with Quote.Quotedprice - quote.TotalRebate
        NullablePrice finalprice = new NullablePrice(Quote.QuotedPrice.NumericValue - Quote.TotalRebate, Quote.Currency, false); // destroy blue price with false on purpose.
        finalprice.isTotal = true;

        tc = new TableCell() { CssClass = "quotesTableCell" };
        tc.Controls.Add(finalprice.DisplayPrice(buyerAccount, errorMessages)); //if valid only --- need to add
        tr.Cells.Add(tc);

        //Add tool buttons....

        tc = new TableCell();
        if (Quote.State.code != "#WN")
        {
            tc.Controls.Add(MakeRoundButton("tick.png", Xlt("Mark as won", language), "burstBubble(event);var savedP=false;  savedP= $(\'#SavedPanel\').attr(\'aria-expanded\');showVersion(\'manipulation.aspx?command=MarkAsWon&QID=" + System.Convert.ToString(Quote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
        }
        tr.Cells.Add(tc);


        tc = new TableCell();
        if (Quote.State.code != "#WN")
        {
            tc.Controls.Add(MakeRoundButton("close.png", Xlt("Discard this quote", language), "burstBubble(event); savedP= $(\'#SavedPanel\').attr(\'aria-expanded\');showVersion(\'manipulation.aspx?command=DiscardQuote&QID=" + System.Convert.ToString(Quote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
        }
        tr.Cells.Add(tc);

        tc = new TableCell();
        tc.Controls.Add(MakeRoundButton("excl.png", Xlt("Export", language), "burstBubble(event);showMenu(" + this.ID.ToString() + ");return false;", " quoteButton", "", Quote.AgentAccount.Language));
        tc.Controls.Add(NewLit("<div onclick=\"burstBubble();return false;\">" + "<div id = \"exportMenu" + this.ID.ToString() + "\"  class = \"submenu\" > " + "<a class=\"account\"> " + Xlt("Export Option", AgentAccount.Language) + "s</a> <ul class=\"root\" >" + "<li><a onclick = \"burstBubble(event);$(\'.submenu\').hide();$(\'#spinnerContainer\').show() ;rExec(\'Quote.aspx?cmd=Excel&QuoteId=" + System.Convert.ToString(this.ID) + "&quoteName=" + this.Name.value.ToString() + "\',  downloadFile); return false;\" href=\"#\">" + Xlt("Excel", AgentAccount.Language) + "</a></li>" + "<li><a onclick = \"burstBubble(event); $(\'.submenu\').hide();$(\'#spinnerContainer\').show();rExec(\'Quote.aspx?cmd=PDF&QuoteId=" + System.Convert.ToString(this.ID) + "&quoteName=" + this.Name.value.ToString() + "\',  downloadFile); return false;\" href=\"#\">" + Xlt("PDF", AgentAccount.Language) + "</a></li>" + "<li><a href=\"#\" onclick = \"burstBubble(event); $(\'.submenu\').hide();$(\'#spinnerContainer\').show() ;rExec(\'Quote.aspx?cmd=XML&QuoteId=" + System.Convert.ToString(this.ID) + "&quoteName=" + this.Name.value.ToString() + "\',  downloadFile); return false;\">" + Xlt("XML", AgentAccount.Language) + "</a></li>" + "<li><a href=\"#\" onclick = \"burstBubble(event); $(\'.submenu\').hide();$(\'#spinnerContainer\').show() ;rExec(\'Quote.aspx?cmd=XMLAdv&QuoteId=" + System.Convert.ToString(this.ID) + "&quoteName=" + this.Name.value.ToString() + "\',  downloadFile); return false;\">" + Xlt("XML Advanced", AgentAccount.Language) + "</a></li>" + "<li><a href=\"#\" onclick = \"burstBubble(event); $(\'.submenu\').hide();$(\'#spinnerContainer\').show() ;rExec(\'Quote.aspx?cmd=XMLSmartQuote&QuoteId=" + System.Convert.ToString(this.ID) + "&quoteName=" + this.Name.value.ToString() + "\',  downloadFile); return false;\">" + Xlt("XML SmartQuote", AgentAccount.Language) + "</a></li></ul>" + " </div></div> "));

        //tc.Controls.Add(MakeRoundButton("excl.png", "Export to PDF", "burstBubble(event);rExec('Quote.aspx?cmd=PDF&QuoteId=" & Me.ID & "&quoteName=" & Me.Name.value.ToString() & "',  downloadFile);return false;", "", "", Quote.AgentAccount.Language))
        tr.Cells.Add(tc);

        //Not nice, would like to redo this....

        tc = new TableCell();
        tc.Controls.Add(previewpanel);
        tr.Cells.Add(tc);

        return tr;
    }

    public void dummy(clsQuote Quote, string[] css, UInt64 lid, bool islatest, List<string> errorMessages)
    {
        Panel tr = default(Panel);
        Panel tc = default(Panel);
        Label lbl = default(Label);

        Panel buttons = default(Panel);

        tr = new Panel();
        tr.ID = "Q" + System.Convert.ToString(Quote.ID); //give the row an id so we can hide it (when discarding)

        //Dim H$ = "ID,Version,Name,Customer,Supplier,Systems,Updated,Status,Value!"

        //ID
        tc = new Panel();
        tc.CssClass = css[0];
        lbl = new Label();
        tc.Controls.Add(lbl);
        lbl.Text = Quote.RootQuote.ID.ToString();
        tr.Controls.Add(tc);

        //Version
        tc = new Panel();
        lbl = new Label();
        tc.Controls.Add(lbl);
        tc.CssClass = css[1];
        lbl.Text = Quote.Version.ToString();
        tr.Controls.Add(tc);

        //Name
        tc = new Panel();
        tc.CssClass = css[2];
        //Dim aLit As Literal
        // aLit = New Literal
        // aLit.Text = "<span onClick=""alert('hello';)>"""
        // aLit.Text &= "NM:" & Quote.Name.DisplayValue
        // aLit.Text &= "</span>"
        // tc.Controls.Add(aLit)

        lbl = new Label();
        //       If Not Quote.Name.value Is DBNull.Value Then Stop

        lbl.Text = Quote.Name.DisplayValue + (Quote.Locked ? "*" : "").ToString() + "<img class=\'unpressedButton\' style=\'vertical-align:center;width:1em;height:1em;\' alt=\'Rename\' title=\'Rename\' src=\'/images/navigation/pencil.png\' onclick=\'burstBubble(event);renameQuote(" + System.Convert.ToString(this.ID) + ");return false;\'/>";
        tc.Controls.Add(lbl);

        //   lbl.Attributes("onClick") = "alert('hello';)>"""
        tr.Controls.Add(tc);


        //Customer
        tc = new Panel();
        tc.CssClass = css[3];
        lbl = new Label();
        tc.Controls.Add(lbl);
        lbl.Text = Quote.BuyerAccount.BuyerChannel.Name;
        tr.Controls.Add(tc);

        //Supplier
        tc = new Panel();
        tc.CssClass = css[4];
        lbl = new Label();
        tc.Controls.Add(lbl);
        lbl.Text = Quote.BuyerAccount.SellerChannel.Name;
        tr.Controls.Add(tc);

        //'Systems
        //tc = New Panel
        //tc.CssClass = css(5)
        //lbl = New Label
        //tc.Controls.Add(lbl)
        //lbl.Text = Xlt("sys:" & Quote.Description.DisplayValue, Quote.BuyerAccount.Language)
        //tr.Controls.Add(tc)

        //'Options
        //tc = New Panel
        //tc.CssClass = css(6)
        //lbl = New Label
        //tc.Controls.Add(lbl)
        //lbl.Text = Quote.NumOptions
        //tr.Controls.Add(tc)

        //Updated
        tc = new Panel();
        tc.CssClass = css[5];
        lbl = new Label();
        tc.Controls.Add(lbl);
        lbl.Text = Quote.Created.ToShortDateString(); // & " " & Quote.Created.ToShortTimeString
        tr.Controls.Add(tc);

        //Status
        tc = new Panel();
        tc.CssClass = css[6];
        lbl = new Label();
        lbl.Text = Quote.State.code;
        tc.Controls.Add(lbl);
        tr.Controls.Add(tc);

        //Value
        tc = new Panel();
        tc.CssClass = css[7];
        if (!Quote.QuotedPrice.isValid || Quote.QuotedPrice.NumericValue == 0)
        {
            Quote.UpdateDescAndPrice();
        }
        if (!(Quote.QuotedPrice == null))
        {
            tc.Controls.Add(Quote.QuotedPrice.DisplayPrice(Quote.BuyerAccount, errorMessages)); //NB: the price has a currency but the buyeraccounts culture telss us how to format the number
            tr.Controls.Add(tc);
        }

        //!Buttons"
        buttons = new Panel();
        buttons.CssClass = "quotesListCol-Buttons"; // {width:7em;float:left;min-height:1px;}
        buttons.Controls.Add(MakeRoundButton("tick.png", "Mark as won", "var savedP=false;  savedP= $(\'#SavedPanel\').attr(\'aria-expanded\');showVersion(\'manipulation.aspx?command=MarkAsWon&QID=" + System.Convert.ToString(Quote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
        buttons.Controls.Add(MakeRoundButton("close.png", "Discard this quote", "var savedP=false;  savedP= $(\'#SavedPanel\').attr(\'aria-expanded\');showVersion(\'manipulation.aspx?command=DiscardQuote&QID=" + System.Convert.ToString(Quote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
        buttons.Controls.Add(MakeRoundButton("copy.png", "Create a new quote using this template", "var savedP=false;  savedP= $(\'#SavedPanel\').attr(\'aria-expanded\');showVersion(\'manipulation.aspx?command=CopyQuote&QID=" + System.Convert.ToString(Quote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
        buttons.Controls.Add(MakeRoundButton("excl.png", "Export to PDF", "burstBubble(event);rExec(\'Quote.aspx?cmd=PDF&QuoteId=" + System.Convert.ToString(this.ID) + "&quoteName=" + this.Name.value.ToString() + "\',  downloadFile);return false;", "", "", Quote.AgentAccount.Language));


        Panel previewpanel = new Panel();
        previewpanel.ID = "pv" + System.Convert.ToString(Quote.ID) + "-" + System.Convert.ToString(Quote.Version);
        //ctl00_MainContent_
        buttons.Controls.Add(MakeRoundButton("pencil.png", " Edit this quote", "suck(\'" + "quotesummary.aspx?quoteid=" + System.Convert.ToString(Quote.ID) + "\',\'" + previewpanel.ID + "\');  popDialog(\'" + previewpanel.ID + "\');", "", "", Quote.AgentAccount.Language));

        bool isExpanded = false;
        if (iq.SeshContains(lid, "expandedQuotes"))
        {
            //If Me.AgentAccount.Quotes.ContainsKey(Me.RootQuote.ID) Then
            if (((List<int>)(iq.sesh(lid, "expandedQuotes"))).Contains(this.RootQuote.ID))
            {
                isExpanded = true;
            }
            // End If
        }

        if (islatest)
        {
            if (isExpanded)
            {
                buttons.Controls.Add(MakeRoundButton("minus.png", "Show only the latest version of this quote", "var savedP=false;  savedP= $(\'#SavedPanel\').attr(\'aria-expanded\'); showVersion(\'manipulation.aspx?command=CollapseQuoteVersions&RQID=" + System.Convert.ToString(Quote.RootQuote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
            }
            else
            {
                if (this.AgentAccount.MaxQuoteVersion(RootQuote) > 1)
                {
                    buttons.Controls.Add(MakeRoundButton("plus.png", "Show all versions of this quote", "var savedP=false;  savedP= $(\'#SavedPanel\').attr(\'aria-expanded\');showVersion(\'manipulation.aspx?command=ExpandQuoteVersions&RQID=" + System.Convert.ToString(Quote.RootQuote.ID) + "\', savedP);", "", "", Quote.AgentAccount.Language));
                }
            }
            tr.CssClass += "latestVersion";
        }
        else
        {
            tr.CssClass += "oldVersion";
        }
        buttons.Controls.Add(MakeRoundButton("eye.png", "Show history of quote export", "showDialog(\'ExportedQuotes.aspx?quoteRootID=" + System.Convert.ToString(this.RootQuote.ID) + "&lid=" + System.Convert.ToString(lid) + "\' )", "", "", Quote.AgentAccount.Language));

        tr.Controls.Add(buttons);
        tr.Controls.Add(previewpanel);

    }

    public int SQLInsert(bool BootStrap = false)
    {
        int returnValue = 0;
        StringBuilder sql = new StringBuilder(string.Empty);
        if (BootStrap) //the very first quote
        {
            sql.AppendFormat("{0}", "set identity_insert [quote] ON;");
            sql.AppendFormat("{0}", "INSERT INTO QUOTE (id,FK_Account_Id_agent,FK_Account_id_buyer,fk_quote_id_root,created,updated,version,fk_state_id,price,fk_currency_id,locked,hidden,saved,reference,fk_import_id,totalrebate) ");
            sql.AppendFormat("{0}", " VALUES (1,");
            sql.AppendFormat("{0},", AgentAccount.ID);
            sql.AppendFormat("{0},", BuyerAccount.ID);
            sql.AppendFormat("{0},", 1);
            sql.AppendFormat("{0},", da.UniversalDate(Created));
            sql.AppendFormat("{0},", da.UniversalDate(Updated));
            sql.AppendFormat("{0},", Version);
            sql.AppendFormat("{0},", State.ID);
            sql.AppendFormat("{0},", this.QuotedPrice.sqlvalue);
            sql.AppendFormat("{0},", Currency.ID);
            sql.AppendFormat("{0},", Locked ? "1" : "0");
            sql.AppendFormat("{0},", Hidden ? "1" : "0");
            sql.AppendFormat("{0},", Saved ? "1" : "0");
            sql.AppendFormat("{0},", da.SqlEncode(Reference));
            sql.AppendFormat("{0},", FK_Import_Id);
            sql.AppendFormat("{0}", this.TotalRebate);
            sql.AppendFormat("{0}", "); set identity_insert [quote] OFF;");
        }
        else
        {
            //a 'normal' (non - bulk inserted) quote

            sql.AppendFormat("{0}", "INSERT INTO QUOTE (FK_Account_Id_agent,FK_Account_id_buyer,fk_quote_id_root,created,updated,version,name,fk_state_id,price,fk_currency_id,locked,hidden,saved,reference,fk_import_ID,totalrebate) ");
            sql.AppendFormat("{0}", "VALUES (");
            sql.AppendFormat("{0},", AgentAccount.ID);
            sql.AppendFormat("{0},", BuyerAccount.ID);
            sql.AppendFormat("{0},", "(SELECT IDENT_CURRENT (\'Quote\'))");
            sql.AppendFormat("{0},", da.UniversalDate(Created));
            sql.AppendFormat("{0},", da.UniversalDate(Updated));
            sql.AppendFormat("{0},", Version);
            sql.AppendFormat("{0},", Name.sqlValue);
            sql.AppendFormat("{0},", State.ID);
            sql.AppendFormat("{0},", this.QuotedPrice.sqlvalue);
            sql.AppendFormat("{0},", Currency.ID);
            sql.AppendFormat("{0},", Locked ? "1" : "0");
            sql.AppendFormat("{0},", Hidden ? "1" : "0");
            sql.AppendFormat("{0},", Saved ? "1" : "0");
            sql.AppendFormat("{0},", da.SqlEncode(Reference));
            sql.AppendFormat("{0},", FK_Import_Id);
            sql.AppendFormat("{0}", this.TotalRebate);
            sql.AppendFormat("{0}", ")");
        }
        returnValue = System.Convert.ToInt32(da.DBExecutesql(sql.ToString(), true, this));

        //        sql$ = "UPDATE quote SET fk_quote_id_root=" & Me.RootQuote.ID & ",version=" & Version.ToString & " WHERE ID=" & Me.ID
        //       da.DBExecutesql(sql$, False, Me)
        return returnValue;
    }
    public void SQLUpdate()
    {

        string desc = System.Convert.ToString(this.Description.DisplayValue);
        if (desc.Length > 450)
        {
            desc = desc.Substring(0, 450) + "...(truncated)";
        }

        StringBuilder sql = new StringBuilder(string.Empty);
        sql.AppendFormat("{0}{1}", "UPDATE Quote SET FK_Account_Id_agent=", AgentAccount.ID);
        sql.AppendFormat("{0}{1}", ",FK_Account_id_buyer=", BuyerAccount.ID);
        sql.AppendFormat("{0}{1}", ",FK_State_ID=", State.ID);
        sql.AppendFormat("{0}{1}", ",created=", da.UniversalDate(this.Created));
        sql.AppendFormat("{0}{1}", ",hidden=", Hidden ? "1" : "0");
        sql.AppendFormat("{0}{1}", ",locked=", Locked ? "1" : "0");
        sql.AppendFormat("{0}{1}", ",saved=", Saved ? "1" : "0");
        sql.AppendFormat("{0}{1}", ",description=", da.SqlEncode(desc));
        sql.AppendFormat("{0}{1}", ",price=", this.QuotedPrice.sqlvalue);
        sql.AppendFormat("{0}{1}", ",name=", this.Name.sqlValue);
        sql.AppendFormat("{0}{1}", ",fk_quote_id_root=", this.RootQuote.ID);
        sql.AppendFormat("{0}{1}", ",totalrebate=", this.TotalRebate);
        sql.AppendFormat("{0}{1}", " WHERE ID=", this.ID);
        da.DBExecutesql(sql.ToString(), false, this);
    }
    public void UpdateSelfAfterIdChange(int NewId)
    {
        //Update OM
        iq.Quotes.Remove(this.ID);
        this.ID = NewId;
        iq.Quotes.Add(NewId, this);

        foreach (var c in this.RootItem.Children)
        {
        }


    }

    public bool KeywordExists(string search)
    {



        List<string> errorMessages = new List<string>();
        this.LoadItems(errorMessages);
        clsQuoteItem item = default(clsQuoteItem);

        foreach (clsQuoteItem tempLoopVar_item in this.RootItem.Children)
        {
            item = tempLoopVar_item;
            this.keyword += item.Branch.Product.sku + " , ";
            if (item.Children.Count > 0)
            {
                foreach (var child in item.Children)
                {
                    this.keyword += child.Branch.Product.sku + ",";
                }
            }
        }
        this.keyword = this.keyword.ToUpper();
        if (this.keyword.Contains(search.ToUpper()))
        {
            return true;
        }
        else
        {
            return false;
        }



    }


    #region QuoteFunctions
    public List<string> MarkAsWon(UInt64 lid)
    {
        List<string> returnValue = default(List<string>);
        returnValue = new List<string>();
        if (!this.Saved)
        {
            this.Save(DefaultName, lid);
        }
        this.LoadItems(returnValue);
        this.Locked = true;
        this.State = iq.i_state_GroupCode("QT-#WN"); //mark as Won
        this.Update();
        return returnValue;
    }
    public string DefaultName
    {
        get
        {
            return this.AgentAccount.displayName(English) + DateTime.Today.ToShortDateString();
        }
    }


    public string Save(string quoteName, UInt64 lid)
    {

        //should be invisible really (until we have a quote)
        //If iq.sesh(lid, "QuoteID") IsNot Nothing Then

        //Dim quote As clsQuote
        clsAccount agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));

        //quote = agentAccount.Quotes(CInt(iq.sesh(lid, "QuoteID")))

        if (quoteName.Trim() == "" || quoteName.Trim() == "-")
        {
            this.Name = new nullableString(BuyerAccount.BuyerChannel.Name);
        }
        else
        {
            this.Name = new nullableString(quoteName);
        }

        this.Saved = true;
        this.Update();
        this.RootItem.updateRecursive();

        return Xlt("Version " + System.Convert.ToString(this.Version) + " saved.", agentAccount.Language); // ,version " & NewQuote.Version & " created"

        //  Else
        //    Return String.Empty
        //   End If
    }

    public List<clsQuoteHistoryItem> ExportHistory(bool onlyThisVersion)
    {
        List<clsQuoteHistoryItem> returnValue = default(List<clsQuoteHistoryItem>);
        returnValue = new List<clsQuoteHistoryItem>();
        SqlClient.SqlConnection con = da.OpenDatabase();
        string sqlQuery = "";
        if (onlyThisVersion)
        {
            sqlQuery = "SELECT  qe.[ID],[FK_Quote_ID],[Type],[TimeStamp] , [Version] FROM [QuoteExport] qe inner join Quote q on [FK_Quote_ID] = q.ID where q.id= " + System.Convert.ToString(this.ID);
        }
        else
        {
            sqlQuery = "SELECT  qe.[ID],[FK_Quote_ID],[Type],[TimeStamp] , [Version] FROM [QuoteExport] qe inner join Quote q on [FK_Quote_ID] = q.ID where FK_Quote_ID_Root = " + System.Convert.ToString(this.RootQuote.ID);
        }
        SqlClient.SqlDataReader r = da.DBExecuteReader(con, sqlQuery);
        DataTable dt = new DataTable();
        dt.Load(r);

        foreach (DataRow rd in dt.Rows)
        {
            returnValue.Add(new clsQuoteHistoryItem() { ExportType = rd.Item("Type").ToString(), Timestamp = DateTime.Parse(rd.Item("TimeStamp").ToString()), Version = System.Convert.ToInt32(rd.Item("Version")) });
        }
        con.Close();
        con.Dispose();
        return returnValue;
    }

    #endregion



    public bool ExportPDF(UInt64 lid, string certPath, string quoteName, ref List<string> errormessages)
    {
        //We are exporting so lock and save this...
        Locked = true;
        Save(DefaultName, lid);

        if (ExportExcel(lid, quoteName, errormessages, true))
        {
            ExportLogging("PDF");
            string filepath = System.Convert.ToString(iq.sesh(lid, "tostream"));
            string pdfFile = System.Convert.ToString(IQDrive.uploadFile(filepath, certPath));
            if (pdfFile.Length > 0)
            {
                iq.sesh(lid, "tostream") = pdfFile;
                iq.sesh(lid, "streamcontent-type") = "application/pdf";
                iq.sesh(lid, "DeleteStreamed") = true;
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    public bool ExportExcel(UInt64 lid, string quoteName, List<string> errormessages, bool fromPDF)
    {
        try
        {
            //We are exporting so lock and save this...
            Locked = true;
            Save(DefaultName, lid);

            //should be invisible really (until we have a quote)
            if (iq.sesh(lid, "QuoteID") == null)
            {
                errormessages.Add(Xlt("You need to add some items to the quote first", ((clsAccount)(iq.sesh(lid, "AgentAccount"))).Language));
                return false;
            }
            else
            {
                string fullpath = "";
                this.Name = new nullableString(quoteName);
                this.Saved = true;
                this.Locked = true;
                this.Update();
                if (!fromPDF)
                {
                    this.ExportLogging("Excel");
                }
                fullpath = System.Convert.ToString(ODS.OutputQuote(this, "Quotes", errormessages)); //the OutputQuote() function returns the full physical path the the file generated on the server

                // If errormessages.Count = 0 Then
                string filepath = "/Quotes/" + System.Convert.ToString(this.RootQuote.ID) + "-" + System.Convert.ToString(this.Version) + ".ods";

                iq.sesh(lid, "tostream") = fullpath;
                iq.sesh(lid, "streamcontent-type") = "application/vnd.ms-excel;charset=UTF-8";
                iq.sesh(lid, "DeleteStreamed") = true;
                //Response.Redirect("streamer.aspx?lid=" & lid)
                return true;
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return false;
        }
    }
    public bool ExportXMLAdv(UInt64 lid, string quotenNme, List<string> errorMessages)
    {
        try
        {
            //We are exporting so lock and save this...
            Locked = true;
            Save(DefaultName, lid);
            string outputMessage = string.Empty;
            //should be invisible really (until we have a quote)
            if (iq.sesh(lid, "QuoteID") != null)
            {

                this.Name = new nullableString(quotenNme);
                this.Saved = true;
                this.Locked = true;
                this.Update();
                this.ExportLogging("XML");
                List<string> null_List = null;
                string fullpath = System.Convert.ToString(SaveXML(XMLDoc(ref errorMessages, ref null_List), "/quotes/" + System.Convert.ToString(RootQuote.ID) + "-" + System.Convert.ToString(Version) + ".xml", outputMessage));

                //PnlErrors.Controls.Add(OutputErrors(errorMessages, lid))

                if (fullpath != "FAIL")
                {
                    iq.sesh(lid, "tostream") = fullpath;
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return false;
        }
    }
    public bool ExportXML(UInt64 lid, string quotenNme, List<string> errorMessages)
    {
        try
        {
            //We are exporting so lock and save this...
            Locked = true;
            Save(DefaultName, lid);
            object vPath = HttpContext.Current.Request.ApplicationPath;
            object pPath = HttpContext.Current.Request.MapPath(vPath) + "\\";

            string outputMessage = string.Empty;
            //should be invisible really (until we have a quote)
            if (iq.sesh(lid, "QuoteID") != null)
            {
                this.Name = new nullableString(quotenNme);
                this.Saved = true;
                this.Locked = true;
                this.Update();
                this.ExportLogging("XML");

                string tf = "";
                string fn = "";
                fn = "Quotes\\" + System.Convert.ToString(this.RootQuote.ID) + "-" + System.Convert.ToString(this.Version) + ".xml";
                tf = pPath + fn;

                try
                {
                    if ((new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.FileExists(tf))
                    {
                        (new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.DeleteFile(tf);
                    }


                    System.IO.StreamWriter objWriter = new System.IO.StreamWriter(tf);
                    object xmlstring = this.basketAsXML(lid);
                    objWriter.WriteLine(xmlstring);
                    objWriter.Close();


                    //PnlErrors.Controls.Add(OutputErrors(errorMessages, lid))


                    iq.sesh(lid, "tostream") = tf;

                }
                catch (Exception ex)
                {
                    ErrorLog.Add(ex);

                }
                return true;
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return false;
        }
    }
    public bool ExportXMLSmart(UInt64 lid, string quotenNme, List<string> errorMessages)
    {

        try
        {
            //We are exporting so lock and save this...
            Locked = true;
            Save(DefaultName, lid);

            string outputMessage = string.Empty;
            //should be invisible really (until we have a quote)
            if (iq.sesh(lid, "QuoteID") != null)
            {
                this.Name = new nullableString(quotenNme);
                this.Saved = true;
                this.Locked = true;
                this.Update();
                this.ExportLogging("XML");
                string fullpath = System.Convert.ToString(SaveXML(XMLDocSmartQuote(ref errorMessages), "/quotes/" + System.Convert.ToString(RootQuote.ID) + "-" + System.Convert.ToString(Version) + ".xml", outputMessage));

                //PnlErrors.Controls.Add(OutputErrors(errorMessages, lid))

                if (fullpath != "FAIL")
                {
                    iq.sesh(lid, "tostream") = fullpath;
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return false;
        }
    }
    public bool PassesValidation(UInt64 lid) //lid As UInt64) As Boolean
    {
        //Dim buyeraccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        //Dim agentaccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
        List<string> errorMessages = new List<string>();
        this.LoadItems(errorMessages);
        this.validate(lid, BuyerAccount, AgentAccount, errorMessages, false);
        return this.RootItem.AllChildMsgs.Where(m => m.severity > EnumValidationSeverity.amberalert && m.type == enumValidationMessageType.Validation).Count() == 0;

    }
    public void getPrice(List<clsVariant> toget, UInt64 lid, ref List<string> errorMessages)
    {
        if ((this.AgentAccount.SellerChannel.priceConfig & 8) != 0)
        {
            this.RootItem.getQuoteVariant(toget, true);

            int handle = 0;
            handle = System.Convert.ToInt32(ModUniTran.DispatchUpdateRequest(lid, toget, "", errorMessages));

            wsconsumer.clsStockPriceResponse response = null;
            response = callWsConsumer(handle, errorMessages);
            //   OutputErrors(Me.Controls, errorMessages, lid) ' these just go to the audit log now

            if (response != null) //The webservice call can fail
            {
                updateStockPriceFromResponse(handle, response, lid, errorMessages);
                //  outputUpdatedPriceUIs(lid, handle, response, "", errorMessages)
                Literal lit = new Literal();
                if (PendingRequests != null && PendingRequests.ContainsKey(handle))
                {


                    if (response.completed || PendingRequests(handle).tryCount == 5)
                    {
                        clsQueuedRequest removed = null;
                        PendingRequests.TryRemove(handle, removed);

                        lit.Text = "]DONE^"; //we have completed the fetches for all ID (they are all less than 5 minutes old)
                    }
                    else
                    {
                        PendingRequests(handle).tryCount += 1;
                    }

                    //if any of the prices are still pending.. set another timeout
                    //   lit.Text = "]" & Request("Path") & "^" & handle & "^"  'Some prices are still old - we add the path so a new setTimeout() can be created in the JS PlacePrices()
                }

            }
        }
    }
    private wsconsumer.clsStockPriceResponse callWsConsumer(int handle, List<string> errorMessages)
    {

        wsconsumer.I_UniTranClient requester = default(wsconsumer.I_UniTranClient);
        requester = new wsconsumer.I_UniTranClient();

        requester.ClientCredentials.Windows.ClientCredential.Password = "iQuoteEXPERT";
        requester.ClientCredentials.Windows.ClientCredential.UserName = "DSVR016766\\Nick.axworthy";

        wsconsumer.clsStockPriceResponse response = default(wsconsumer.clsStockPriceResponse);
        try
        {
            response = requester.CheckStockPrices(handle, true, 30); //this call is very fast - it returns the results sofar for the specified handle  (which may be an empty list).. and status   - TODO probably want to set a nice short timeout in the binding, and handle errors gracefully
            requester.Close(); //new
        }
        catch (System.Exception ex)
        {
            errorMessages.Add("*" + ex.Message);
            response = null;
        }

        return response;

    }
    private void updateStockPriceFromResponse(int handle, wsconsumer.clsStockPriceResponse response, UInt64 lid, List<string> errormessages)
    {

        clsAccount buyeraccount = default(clsAccount);

        if (PendingRequests == null)
        {
            errormessages.Add("* Pending requests was nothing");
        }
        else
        {
            if (!PendingRequests.ContainsKey(handle))
            {
                errormessages.Add("* PendingRequests did not contain the handle:" + System.Convert.ToString(handle));
            }
            else
            {

                foreach (var r in response.items)
                {

                    IEnumerable<clsVariant> vs = from rq in PendingRequests(handle).skuVariants where rq.DistiSku == r.SKU select rq;
                    if (vs.Any)
                    {
                        clsVariant v = vs.First;
                        buyeraccount = PendingRequests(handle).BuyerAccount;
                        updatePriceStock(buyeraccount, v, r, lid);
                        if (vs.Count > 1)
                        {
                            errormessages.Add("* There were " + vs.Count + " rows returend for " + v.DistiSku + " expected 1");
                        }

                    }
                    else
                    {
                        errormessages.Add("* The request contained no SKU:" + r.SKU);
                    }
                }
            }
        }



    }
    public void updatePriceStock(clsAccount buyeraccount, clsVariant v, wsconsumer.clsStockPriceItem item, UInt64 lid)
    {

        //Each Variant (product) is Seller-specific (but not Buyer specific)
        bool found = false;
        //each batch is a dictionary of HostPartnum > ProductVariant (a product-SKUvariant pair)
        //Each product has a dictioanry of sellers>variants>(arrival)dates>ClsStocks  - (containing a quantity, datestamp etc)


        clsAccount with_1 = buyeraccount;
        clsPrice price = default(clsPrice);
        if (v.i_prices.ContainsKey(BuyerAccount.Priceband))
        {

            Dictionary<clsCurrency, clsPrice> prices = v.i_prices(BuyerAccount.Priceband);
            price = prices[with_1.Currency];

            if (item.status == "OK" || item.status == "")
            {

                // If item.SKU.Contains("3") Then Stop
                if (item.CustomerPrice > 0)
                {
                    price.Price.value = System.Convert.ToDecimal(item.CustomerPrice);
                    price.Price.isValid = true; //Important ! (otherwise POA's remain 'invalid' event though they now have a value
                    price.Price.isList = false; //In case it was a (temporary) list price - (it is'nt now!)
                    price.Source = "Confirmed";

                }
            }
            else
            {
                if (item.message == null)
                {
                    price.Source = item.status;
                }
                else
                {
                    price.Source = item.message;
                }
            }

            price.Update(); //Expensive
        }
        else
        {
            //should never happen ! - the webservice created a POA price in advance
            clsPrice newprice = new clsPrice(v, BuyerAccount.Priceband, new NullablePrice(item.CustomerPrice, with_1.Currency, false), "Wesbservice (updatePriceStock add)");
            price = newprice;
        }

        if (price.Price.isValid)
        {
            if (iq.sesh(lid, "QuoteID") != null)
            {
                clsQuote quote = BuyerAccount.Quotes(System.Convert.ToInt32(iq.sesh(lid, "QuoteID")));
                quote.RootItem.updateQuotedPrice(v, price.Price); //Recurses through every item in the quote - updating them IF they have this price
            }
        }

        if (v.shipments == null)
        {
            clsstock newstockrecord = new clsstock(v, item.stock(0).quantity, item.stock(0).arrival, "New stock record (created by UpdatePriceStock()", true);
        }

        //dates on which shipments of variants arrive
        foreach (wsconsumer.clsShipment shipment in item.stock)
        {

            if (v.shipments.ContainsKey(shipment.arrival)) //update an exisiting shipment
            {
                dynamic with_2 = v.shipments(shipment.arrival);
                with_2.quantity = shipment.quantity;
                with_2.LastUpdated = DateTime.Now;
                with_2.update();
            }
            else
            {
                //make a new stock record for this shipment (will INSERT to the database and Update product.i_Stock
                //' This wasn't such a good idea - issues with the shipments ID changing when archived -  If shipment.arrival.Date = CDate("01/01/2000").Date Then v.ArchiveCurrentStock() 'removes it from the product.i_stock AND sets the archived flag

                //in this instance, there was no stock record - so there is no stockUI to replace - so the stock doesnt show... we need to refesh the whole branch instead

                clsstock newStockRecord = new clsstock(v, shipment.quantity, shipment.arrival, "WS", shipment.isCurrent);
            }
        }

    }
    private void outputUpdatedPriceUIs(UInt64 lid, int handle, wsconsumer.clsStockPriceResponse response, string divIds, List<string> errormessages)
    {


        if (handle != -1)
        {
            clsAccount buyeraccount = PendingRequests(handle).BuyerAccount;
            Literal lit;
            if (response.items.Count > 0) //yey we have results
            {

                if (divIds != "") //it's possibe we closed the DIV (whilst the request was pending)
                {
                    string[] b;

                    Panel pnl;
                    foreach (string ID in divIds.Split(',')) //each DIV id is of the form P_priceID (or S_Stockid)
                    {

                        //If ID <> "" Then
                        //    lit = New Literal
                        //    lit.Text = "]" & ID & "^"  'This ASPX outputs ]DivID^replacamentContent  - which is merged back into the poage by the JS placePrices()
                        //    Form.Controls.Add(lit)

                        //    b = Split(ID, "_")
                        //    If b(0) = "P" Then
                        //        If b(1) <> "-1" Then  'todo - remove (to do with POA's and temporary variants  see getprices)
                        //            '    minutesOld = 0 ' UI will RETURN the age of the price
                        //            'pnl = iq.Products(b(1)).prices(b(2)).Ui 'Should go green
                        //            If buyeraccount IsNot Nothing Then
                        //                pnl = iq.Prices(b(1)).Ui(buyeraccount, 1, lid) 'Should go green
                        //            Else
                        //                errormessages.Add("* BuyerAccount was nothing in getPriceUIs")
                        //                pnl = New Panel
                        //            End If

                        //            'minutesOld = iq.Prices(b(1)).MinutesOld
                        //            'If minutesOld > 5 Then
                        //            ' allDone = False
                        //            ' End If

                        //            Form.Controls.Add(pnl)
                        //            ' Else
                        //            '     Beep()
                        //        Else
                        //            Beep()
                        //        End If

                        //    ElseIf b(0) = "S" Then
                        //        ' the placeholder contains a panels (div) - which holds the stock UI
                        //        Dim ph As PlaceHolder = iq.Stock(b(1)).SKUvariant.StockUI(1, "", buyeraccount.Language)
                        //        Form.Controls.Add(ph)
                        //    Else
                        //        Beep()
                        //    End If
                        //End If
                    }
                }
            }
        }

    }
    public string basketAsXML(UInt64 lid)
    {
        clsAccount agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));

        if (this != null)
        {
            //Generate the xml using the proxy class
            Data dt = new Data();
            dt.Quote = new DataQuote();
            dt.Quote.ID = this.ID;
            dt.Quote.Name = this.Name.DisplayValue;
            dt.Quote.CreatedBy = this.AgentAccount.User.RealName;
            dt.Quote.Supplier = this.AgentAccount.SellerChannel.Name;

            List<DataQuoteProduct> products = new List<DataQuoteProduct>();
            DataQuoteProduct product = default(DataQuoteProduct);
            foreach (var flatListItem in this.RootItem.Flattened(true, false, 0).items)
            {
                product = new DataQuoteProduct();
                product.Class = flatListItem.QuoteItem.Branch.Product.ProductType.Code;
                product.PartNum = flatListItem.QuoteItem.Branch.Product.SKU;
                product.SupplierPartNum = flatListItem.QuoteItem.SKUVariant.DistiSku;

                product.ListPrice = flatListItem.QuoteItem.ListPrice.value;
                product.Description = flatListItem.QuoteItem.Branch.DisplayName(this.BuyerAccount.Language);
                product.Qty = flatListItem.Quantity.ToString();
                product.URLProductImage = flatListItem.QuoteItem.Branch.Picture;
                product.OPGref = ((flatListItem.QuoteItem.OPG.DisplayValue() == "-") ? "" : (flatListItem.QuoteItem.OPG.DisplayValue())).ToString();
                product.RebateValue = flatListItem.QuoteItem.rebate;
                product.RebateValueSpecified = true;
                products.Add(product);

            }
            dt.Quote.Product = products.ToArray();


            string xmlString = System.Convert.ToString(SerializeToString(dt));

            return xmlString;

        }
    }
    public string basketAsHiddenFields(UInt64 lid)
    {
        //form based basket

        clsAccount agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));
        StringBuilder hdnbasket = new StringBuilder();
        string templateString = "<input type=\"hidden\" name=\"{0}\" id=\"{0}\" value=\"{1}\" /> ";
        if (this != null)
        {
            hdnbasket.Append("\r\n");
            hdnbasket.Append(string.Format(templateString, "SesID", iq.sesh(lid, "GK_SessionID")));
            hdnbasket.Append("\r\n");
            hdnbasket.Append(string.Format(templateString, "UserID", iq.sesh(lid, "GK_uEmail")));
            hdnbasket.Append("\r\n");
            hdnbasket.Append(string.Format(templateString, "Account", iq.sesh(lid, "GK_cAccountNum")));
            hdnbasket.Append("\r\n");
            hdnbasket.Append(string.Format(templateString, "cPriceBand", iq.sesh(lid, "GK_cPriceBand")));
            hdnbasket.Append("\r\n");
            hdnbasket.Append(string.Format(templateString, "QuoteID", this.ID));
            hdnbasket.Append("\r\n");
            hdnbasket.Append(string.Format(templateString, "Account", agentAccount.ID));
            hdnbasket.Append("\r\n");
            hdnbasket.Append(string.Format(templateString, "Grouped", ""));
            hdnbasket.Append("\r\n");
            hdnbasket.Append(string.Format(templateString, "OrdType", "IQ"));
            hdnbasket.Append("\r\n");
            int Productnum = 0;


            foreach (var flatListItem in this.RootItem.Flattened(true, false, 0).items)
            {
                if (!flatListItem.QuoteItem.SKUVariant.DistiSku.Contains("###"))
                {
                    Productnum++;
                    string prodId = "Item[Prod" + Productnum.ToString() + "][";

                    string pn = System.Convert.ToString(flatListItem.QuoteItem.Branch.Product.SKU);
                    if (!string.IsNullOrEmpty(System.Convert.ToString(flatListItem.QuoteItem.SKUVariant.Code)))
                    {
                        pn += "#" + flatListItem.QuoteItem.SKUVariant.Code;
                    }
                    hdnbasket.Append(string.Format(templateString, prodId + "PN]", pn));

                    hdnbasket.Append("\r\n");
                    hdnbasket.Append(string.Format(templateString, prodId + "SKU]", flatListItem.QuoteItem.SKUVariant.DistiSku));
                    hdnbasket.Append("\r\n");
                    hdnbasket.Append(string.Format(templateString, prodId + "Qty]", flatListItem.Quantity.ToString()));
                    hdnbasket.Append("\r\n");
                    hdnbasket.Append(string.Format(templateString, prodId + "Description]", flatListItem.QuoteItem.Branch.Product.DisplayName(this.BuyerAccount.Language)));
                    hdnbasket.Append("\r\n");
                    hdnbasket.Append(string.Format(templateString, prodId + "Price]", flatListItem.QuoteItem.BasePrice.sqlvalue));
                    hdnbasket.Append("\r\n");
                }
            }

            hdnbasket.Append(string.Format(templateString, "Multiplier", "1"));
            hdnbasket.Append("\r\n");
            return hdnbasket.ToString();
        }
        else
        {
            return string.Empty;
        }
    }

    public Manufacturer QuoteSplit
    {

        get
        {
            Manufacturer returnValue = default(Manufacturer);

            returnValue = Manufacturer.Unknown;

            if ((RootItem != null) && (RootItem.Children != null) && (RootItem.Children.Count > 0))
            {
                object root = RootItem.Children(0);
                if (root.Branch.Product != null)
                {
                    returnValue = RootItem.Children(0).Branch.Product.Manufacturer;
                }
            }

            return returnValue;
        }

    }

} //End of class clsQuote
public class clsQuoteHistoryItem
{
    public DateTime Timestamp { get; set; }
    public int Version { get; set; }
    public string ExportType { get; set; }
}