
using dataAccess;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Globalization;


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
		//The virtual root item in this quote (has no branch.. but has children)
	public clsQuoteItem RootItem;
		//Integer 'This is the 'original' quote (this quote is a version of) - starts out as itself
	public clsQuote RootQuote;
	public int Version;
	public clsCurrency Currency;
		//Used during the import .. possiby useful to expose as a customers reference in future
	public string Reference;

	public nullableString Description;
	//Public NumOptions As Integer
	public int NumAlternatives;

	public string keyword;
		// don't use  !! margins are per QuoteItem now
	public float TEMP_IMPORT_MARGIN;
		//The 'sytems' (multiplier) when the quote was exported .. we need to multiple all QuoteItemQuantites by this
	public int TEMP_IMPORT_MULTIPLIER;
		//Total value of the WITH Margin - also hold a 'valid' member to say wether the quote includes any POA items, and the CURRENCY of the quote
	public NullablePrice QuotedPrice;
		//Single
	public decimal TotalRebate;

		//the last item flexed (up) or added - used to set a cssClass on the div, used in trun for the zoomer animation
	public clsQuoteItem MostRecent;

		//stored on the root quote - (primarily so we know if there's more than one quote and wether to display the 'show all versions' button in the list of quotes)
	public int maxVersion;
		//the thing (system) into which we're adding things (options)
	public clsQuoteItem Cursor;



	private int FK_Import_Id;
	//Public Editable As Boolean = False


	public List<string> AcknowledgedValidations = new List<string>();

	public static bool CurrentQuoteContains(UInt64 lid, clsProduct product)
	{

		CurrentQuoteContains = false;

		if (iq.SeshContains(lid, "QuoteID") && iq.sesh(lid, "QuoteID") != null) {
			int qid;
			qid = (int)iq.sesh(lid, "QuoteID");
			//use the session variable
			if (qid != 0) {
				clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
				clsQuote quote = agentAccount.Quotes(qid);
				if (quote.RootItem.HasProduct(product))
					return true;
			}
		}


	}




	public void delete()
	{
		this.BuyerAccount.Quotes.Remove(this.ID);
		//we dont have to remove the quote items - becuase removing the only refernece to the quote (here) - destroys all items

		object sql;
		sql = "DELETE FROM [QuoteItem] WHERE fk_quote_id=" + this.ID;
		da.DBExecutesql(sql, false);


		sql = "DELETE FROM [Quote] WHERE ID=" + this.ID;
		da.DBExecutesql(sql, false);


	}


	public void ExportLogging(string type)
	{
		object sql;
		sql = "INSERT INTO [dbo].[QuoteExport] ([FK_Quote_ID],[Type])  VALUES(" + this.ID + ",'" + type + "')";
		da.DBExecutesql(sql, false);

	}


	public clsQuoteItem SetQtyByItemID(int itemID, int qty, bool Absolute, float MarginForNew, ref List<string> ErrorMessages)
	{

		//can probably be largely consolidated with SetQtyByPath.. but no time right now

		clsQuoteItem Item = null;
		this.RootItem.clearMessage();
		//Clear all validation messages (recursively)

		Item = this.RootItem.FindRecursive(itemID);
		if (Item == null) {
			ErrorMessages.Add("Could not find quote item " + itemID + " in " + this.ID);
		} else {
			if (Item.IsPreInstalled) {
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
				clsQuoteItem existing;
				existing = this.Cursor.FindRecursive(Item.Path, false);


				if (Item.validate) {
					if (qty < 0) {
						Item.validate = false;
					} else {

						if (existing == null) {
							NullablePrice price;
							NullablePrice listprice;
							price = Item.SKUVariant.Price(BuyerAccount);
							//.Price

							listprice = NullPrice(Item.Branch.Product.ListPrice(BuyerAccount), BuyerAccount.Currency);

							//this is a fix for universal/Synnex demo - flexing basket itmes worked on the assumption the variant would have a price already - but  with Universal it's not necessarily so !
							//it may be dangerous - (where prices are 'vivalid' fomr some other reason)
							if (!price.isValid)
								price = listprice;

							//give these an order
							int order;
							if (Item.Branch.Product.isSystem) {
								order = 2;
							} else {
								order = 4;
							}

							Item = new clsQuoteItem(this, Item.Branch, Item.SKUVariant, Item.Path, 1, price, listprice, false, Item.Parent, new nullableString(),
							new nullableString(), 0, MarginForNew, new nullableString(), order);
							//Item.order)
						} else {
							existing.Quantity += qty;
						}
					}
				} else {
					if (qty > 0) {
						Item.validate = true;
						//place a preinstalled item back into validation
					} else {
						ErrorMessages.Add("* preinstalled unvalidated item qty<0");
					}
				}
			} else {
				if (Absolute) {
					Item.Quantity = qty;
				} else {
					object quan = Item.Branch.Quantities.Where(q => q.Value.Region.Encompasses(this.BuyerAccount.BuyerChannel.Region)).FirstOrDefault;
					if (quan.Value != null && quan.Value.MinIncrement != 0) {
						Item.Quantity += (quan.Value.MinIncrement * qty);
					} else {
						Item.Quantity += qty;
					}
				}

				if (Item.Quantity == 0) {
					Item.Parent.Children.Remove(Item);

					//fix for removing items and cursor focus
					if (object.ReferenceEquals(Item, this.Cursor)) {
						if (Item.Parent.Children.Count > 0) {
							this.Cursor = Item.Parent.Children.First;
							this.MostRecent = Item.Parent.Children.First;
						} else {
							this.Cursor = null;
						}
					}
				}

				Item.Update();
				if (Item.Quantity > 0) {
					this.MostRecent = Item;
				}
			}
		}

		return Item;


	}

	public clsQuoteItem setQtyByPath(path, clsVariant SKUvariant, int qty, bool Absolute, float margin, ref List<string> errormessages)
	{

		this.RootItem.clearMessage();
		//Clear all validation messages (recursively)
		NullablePrice Price;
		// = branch.Product.VariantPrice(BuyerAccount, SKUvariant)
		//this gets the list price for the a first variant

		clsBranch branch = iq.Branches((int)Split(path, ".").Last);

		//NB:- List Price can return NOTHING
		//Dim lp As clsPrice = branch.Product.ListPrice(BuyerAccount) 'branch.Product.listPrices(BuyerAccount.Currency)(0).Price

		//fetch the list price if there is one - or constuct a POA in the correct currency
		NullablePrice listprice = NullPrice(branch.Product.ListPrice(BuyerAccount), BuyerAccount.Currency);

		clsQuoteItem item = null;

		//Important - we flex quantities of exisiting OPTIONS under the currenct systen) BUT we add new SYSTEMS to the basket (when flexing by path)'
		//this means that the add buttons in the tree will add *instances* of systems (that can be configured individually)

		bool addingOption = (branch.Product.isSystem(path) == false);
		if (this.Cursor != null & addingOption) {
			item = this.Cursor.FindRecursive(path, true);
			//see if the Current (cursored) quote item (system) already has (as a descendant) 
			//                                              one of the items (by path) we're trying to set the quantity of...
		}

		List<clsPrice> prices;
		prices = branch.Product.GetPrices(BuyerAccount, BuyerAccount.SellerChannel.priceConfig, SKUvariant, errormessages, true);

		bool blnAddSystem = true;

		if (!addingOption & this.RootItem.Children.Count > 0) {
			clsBranch firstSystemBranch = this.RootItem.Children(0).Branch;
			if (branch.Product.Manufacturer != firstSystemBranch.Product.Manufacturer) {
				blnAddSystem = false;
			}

		}



		if (blnAddSystem) {
			if (prices.Count == 0 || prices(0) == null) {
				errormessages.Add("No Price available");
			} else {
				Price = prices(0).Price;

				if (Price.NumericValue > listprice.NumericValue)
					errormessages.Add("* ! Customer price exceeds list price for " + SKUvariant.Product.sku + "(" + SKUvariant.DistiSku + ")");

				if (Price.NumericValue == 0 & Price.isValid == true)
					errormessages.Add("* Price was valid but  0");

				if (item == null) {
					//we didn't have one (under the cursor) in the quote - so we add one

					//this is a fix for universal/Synnex demo - flexing basket itmes worked on the assumption the variant would have a price already - but  with Universal it's not necessarily so !
					//it may be dangerous - (where prices are 'invalid' fomr some other reason)

					//REMOVED by nick - listprice could be nothing (and should already have been returned if it existed)
					//If Not Price.isValid Then Price = listprice
					///'''


					if (this.Cursor == null) {
						item = this.Additem(branch, SKUvariant, path, qty, Price, listprice, this.RootItem, margin, true, errormessages);
					} else {
						//is the thing we're adding/flexing compatible with (appears as a option under) the thing  at the quote cursor

						if (Cursor.Compatible(path)) {
							if (this.Cursor.ID == -99)
								this.Cursor = this.RootItem;
							//part of the fix for bug 712 (may be redundant - but be carefull - harmless !!)
							item = this.Additem(branch, SKUvariant, path, qty, Price, listprice, this.Cursor, margin, true, errormessages);
						} else {
							item = this.Additem(branch, SKUvariant, path, qty, Price, listprice, this.RootItem, margin, true, errormessages);
							item.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.amberalert, iq.AddTranslation("Not compatible with the currently selected system", English, "VM", 0, null, 0, false), null, "", 0, 0, Split("")));
						}
					}

				} else {
					//yes, we had one
					if (item.IsPreInstalled) {
						//we've attempted to flex a preinstalled option 
						//see if we have a 'non-preinstalled' one too already

						clsQuoteItem npItem;
						npItem = this.Cursor.FindRecursive(path, false);
						int toadd;
						if (Absolute)
							toadd = qty - item.Quantity;
						else
							toadd = qty;
						//for absolute.. make up to the desired quantity

						//flexing a preinstalled item down (remove from validation)
						if (toadd < 0) {
							if (item.validate) {
								item.validate = false;
							} else {
								errormessages.Add("* Tried to flex down an item removed from validation");
							}
						//flexing up a preinstalled item
						} else if (toadd > 0) {
							//this preinstalled item included in vaidation 
							if (item.validate) {
								if (npItem == null) {
									//no - make the non-preinstalled version (including *it's* preinstalled options                    
									npItem = this.Additem(branch, SKUvariant, path, toadd, Price, listprice, this.Cursor, margin, true, errormessages);
								} else {
									//yes - flex the non-preinstalled version
									npItem.Quantity += toadd;
								}
							} else {
								item.validate = true;
								//bring a pre-installed item back into validation
							}
						} else {
							errormessages.Add("* toadd was 0 in setQtyByPath");
						}
					} else {
						if (Absolute)
							item.Quantity = qty;
						else
							item.Quantity += qty;

						if (item.Quantity == 0) {
							item.Parent.Children.Remove(item);
						}
					}


				}
				if (item.Branch.Product.isSystem(path)) {
					this.MostRecent = item;
					this.Cursor = item;
				}
			}
			if (item != null) {
				item.Update();
			}
		}

		return item;

	}


	private XmlNode FilledXMLHeader(XmlDocument doc, ref List<string> errormessages)
	{

		//Returns a populated header section for the XML quote export 

		XmlNode h;
		h = doc.CreateElement("Header");

		XmlNode dateNode = doc.CreateElement("Date");
		dateNode.InnerText = Now.ToString("dd MMM yyyy");
		h.AppendChild(dateNode);

		XmlNode Qid = doc.CreateElement("QuoteID");
		Qid.InnerText = this.RootQuote.ID.ToString;
		h.AppendChild(Qid);

		XmlNode QuoteName;
		QuoteName = doc.CreateElement("QuoteName");
		if (this.Name.value.ToString().Trim().Length == 0) {
			QuoteName.InnerText = this.RootQuote.ID.ToString;
		} else {
			QuoteName.InnerText = this.Name.sqlValue;
		}

		h.AppendChild(QuoteName);

		XmlNode Version = doc.CreateElement("Version");
		Version.InnerText = this.Version.ToString;
		h.AppendChild(Version);

		XmlNode currencyCode;
		currencyCode = h.AppendChild(doc.CreateElement("CurrencyCode"));
		currencyCode.InnerText = this.BuyerAccount.Currency.Code;

		XmlNode currencySymbol;
		currencySymbol = h.AppendChild(doc.CreateElement("CurrencySymbol"));
		currencySymbol.InnerText = this.BuyerAccount.Currency.Symbol;


		XmlNode Total = doc.CreateElement("Total");
		//Total.InnerText = Me.Currency.Symbol & Me.TotalPrice.sqlvalue 'this is the sum of all prices BEFORE margin

		NullablePrice tpim = new NullablePrice(this.Currency);
		//Total Price Including Margin

		this.RootItem.Totalise(tpim, TotalRebate, true);
		//fetch the total price INCLUDING margin
		string listPriceIndicator = string.Empty;
		if ((tpim.isList)) {
			listPriceIndicator = " *";
		}
		Total.InnerText = ((decimal)tpim.sqlvalue).ToString(CultureInfo.InvariantCulture);
		//tpim.text(Me.BuyerAccount, errormessages)

		h.AppendChild(Total);

		XmlNode TR = doc.CreateElement("TotalRebate");
		TR.InnerText = this.TotalRebate.ToString(CultureInfo.InvariantCulture);
		h.AppendChild(TR);

		XmlNode GT = doc.CreateElement("QuoteTotal");
		//GT.InnerText = Me.Currency.Symbol & Me.TotalPrice.sqlvalue - Me.TotalRebate
		GT.InnerText = ((decimal)tpim.sqlvalue - this.TotalRebate).ToString(CultureInfo.InvariantCulture);
		h.AppendChild(GT);

		XmlNode buyer;

		buyer = h.AppendChild(doc.CreateElement("Buyer"));

		buyer.AppendChild(doc.CreateElement("BuyerCompanyName")).InnerText = string.Empty;
		// Me.BuyerAccount.BuyerChannel.Name
		buyer.AppendChild(doc.CreateElement("BuyerCompanyID")).InnerText = string.Empty;
		//Me.BuyerAccount.BuyerChannel.ID.ToString
		buyer.AppendChild(doc.CreateElement("BuyerPersonName")).InnerText = string.Empty;
		//Me.BuyerAccount.User.RealName
		buyer.AppendChild(doc.CreateElement("BuyerPersonEmail")).InnerText = string.Empty;
		// Me.BuyerAccount.User.Email
		buyer.AppendChild(doc.CreateElement("BuyerPersonTelephone")).InnerText = string.Empty;
		// If(Me.BuyerAccount.User.tel1.DisplayValue = "IQ.nullableString", "", Me.BuyerAccount.User.tel1.DisplayValue)


		XmlNode Seller;
		Seller = h.AppendChild(doc.CreateElement("Seller"));
		Seller.AppendChild(doc.CreateElement("SellerPersonName")).InnerText = string.Format("{0}, {1} - {2}", this.AgentAccount.User.RealName, this.AgentAccount.User.Channel.Name, this.AgentAccount.User.Email);
		//'String.Empty ' Me.AgentAccount.User.RealName
		Seller.AppendChild(doc.CreateElement("SellerCompanyName")).InnerText = string.Empty;
		//String.Format("{0}", Me.AgentAccount.User.RealName)
		Seller.AppendChild(doc.CreateElement("SellerCompanyID")).InnerText = this.BuyerAccount.SellerChannel.ID.ToString;

		Seller.AppendChild(doc.CreateElement("SellerLogo")).InnerText = (string)this.BuyerAccount.SellerChannel.pic1.value;
		//contains only the subpath and filename (for example /dist/LOGO_DABPL03228.jpg)

		Seller.AppendChild(doc.CreateElement("SellerLogoShort")).InnerText = Filename((string)this.BuyerAccount.SellerChannel.pic2.value);
		//Just the filename parsed out... so we can get it from the media folder

		//'Seller.AppendChild(doc.CreateElement("SellerPersonName")).InnerText = String.Empty ' Me.AgentAccount.User.RealName
		Seller.AppendChild(doc.CreateElement("SellerPersonEmail")).InnerText = string.Empty;
		//Me.AgentAccount.User.Email
		Seller.AppendChild(doc.CreateElement("SellerPersonTelephone")).InnerText = string.Empty;
		//If(Me.AgentAccount.User.tel1.DisplayValue = "IQ.nullableString", "", Me.AgentAccount.User.tel1.DisplayValue)



		XmlNode Language;
		Language = h.AppendChild(doc.CreateElement("Language"));
		Language.InnerText = this.AgentAccount.Language.Code;

		return h;

	}

	private XmlNode FilledXMLSmartQuoteHeader(XmlDocument doc, clsQuoteItem quoteItem, ref List<string> errormessages)
	{

		//Returns a populated header section for the XML SmartQuote export 

		clsLanguage enLanguage = (from l in iq.Languages.Valueswhere l.Code == "EN").First;
		string supplyChainCode = string.Empty;

		foreach ( productAttribute in quoteItem.Branch.Product.Attributes.Values) {
			if ((productAttribute.Translation != null) && (productAttribute.Translation.Group != null) && (productAttribute.Translation.Group == "SCC")) {
				supplyChainCode = productAttribute.Translation.text(enLanguage);
				break; // TODO: might not be correct. Was : Exit For
			}
		}

		XmlNode h;
		h = doc.CreateElement("EclipseHeader");

		XmlAttribute attr = doc.CreateAttribute("ConfigName");
		if (this.Name.v != null) {
			attr.Value = xmlEncode(this.Name.DisplayValue());
		} else {
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

		//returns a summary table of the quote - if onlychanged is passed at true - only quote lines whos prices have changed (stock is now at a different price) will be returned.

		HtmlSummary = new PlaceHolder();

		clsFlatList fl = new clsFlatList();
		Table tbl = new Table();
		tbl.CssClass = "quotePreview";

		List<clsVariant> toget = new List<clsVariant>();
		this.getPrice(toget, lid, errorMessages);

		//flatten the quote in summary form (without preinstalled options)
		//    For Each Li In Me.RootItem.Flattened(Consolidated, includePreinstalled, -1).items  'Flattened() returns a flat list of clsFlatListItems
		bool priceChanged = false;

		//If priceChanged Then

		//End If



		tbl.Rows.Add(summaryHeaders(language));
		bool linePriceChanged;
		TableRow tr;
		foreach ( flatListItem in this.RootItem.Flattened(false, false, 0).items) {
			linePriceChanged = false;
			tr = flatListItem.HTMLTableRow(language, linePriceChanged, errorMessages);
			if ((OnlyChanged & linePriceChanged) | (!OnlyChanged)) {
				tbl.Rows.Add(tr);
			}
			if (linePriceChanged)
				priceChanges = true;
		}


		HtmlSummary.Controls.Add(tbl);

		//If we asked for only changed rows - and only have the one (tableheaders) row... return nothing
		if (OnlyChanged & tbl.Rows.Count == 1) {
			return null;
		}

	}

	public string SellerLogo()
	{

		return this.BuyerAccount.SellerChannel.pic2.value.ToString;

	}

	public TableHeaderRow summaryHeaders(clsLanguage language)
	{

		TableHeaderCell thc;
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

	public XmlDocument XMLDoc(ref List<string> errorMessages, ref List<string> opgs = null)
	{

		XmlDeclaration dec;
		XmlElement DocRoot;
		XmlNode header;
		XmlNode Body;
		XmlNode Footer;

		XMLDoc = new XmlDocument();
		dec = XMLDoc.CreateXmlDeclaration("1.0", null, null);
		XMLDoc.AppendChild(dec);
		DocRoot = XMLDoc.CreateElement("Quote");
		XMLDoc.AppendChild(DocRoot);

		header = this.FilledXMLHeader(XMLDoc, errorMessages);
		DocRoot.AppendChild(header);

		Body = DocRoot.AppendChild(XMLDoc.CreateElement("Body"));
		List<string> productSkus = new List<string>();

		//Flat - Bill Of materiales  (consolidated) version - doesn't include FIO's 
		Body.AppendChild(this.XMLFlatList("FlatQuote", XMLDoc, true, false, errorMessages, productSkus, opgs));
		productSkus.Clear();
		//Tree version can have many rows with the same part number (ie. the same otion installed in several systems) and displays full structure of the quote
		Body.AppendChild(this.XMLFlatList("TreeQuote", XMLDoc, false, true, errorMessages, productSkus, opgs, true));

		// Body.AppendChild(Me.RootItem.XMLTreeList(Doc))
		Footer = DocRoot.AppendChild(XMLDoc.CreateElement("Footer"));

		XmlNode note = XMLDoc.CreateElement("Note");
		Footer.AppendChild(note);
		if (!this.Notes == null) {
			note.InnerText = this.Notes.DisplayValue;
		}

		XmlNode advice = XMLDoc.CreateElement("Advice");


	}

	public XmlNode XMLFlatList(string NodeName, XmlDocument doc, bool Consolidate, bool includePreinstalled, ref List<string> errorMessages, ref List<string> productSkus, ref List<string> opgs = null, bool quote = false)
	{

		//Returns an XML Node - cotaining all quote lines
		//If 'consolidate' is set to true, All quote lines of the same SKU (and variant) are consolidated into one line (with a larger quantity)
		//If consolidate is false, A node st added for every quote line, with its quantity - and additional Nodes for ID and ParentID (which allow the actual herirachy to be reconstructed from the XML - should that ever prove necessary

		XMLFlatList = doc.CreateElement(NodeName);
		if (quote) {
			//Flattened() returns a flat list of clsFlatListItems
			foreach ( Li in this.RootItem.Flattened(Consolidate, includePreinstalled, -1, quote).items) {
				XMLFlatList.AppendChild(Li.XMLLine(doc, this, !Consolidate, errorMessages, productSkus, opgs));
			}
		} else {
			//Flattened() returns a flat list of clsFlatListItems
			foreach ( Li in this.RootItem.Flattened(Consolidate, includePreinstalled, -1).items) {
				XMLFlatList.AppendChild(Li.XMLLine(doc, this, !Consolidate, errorMessages, productSkus, opgs));
			}
		}


	}

	public XmlDocument XMLDocSmartQuote(ref List<string> errorMessages)
	{

		XmlDeclaration xmlDeclaration;
		XmlElement root;
		XmlNode eclipseHeader;
		XmlNode eclipseLineItems;

		XMLDocSmartQuote = new XmlDocument();
		xmlDeclaration = XMLDocSmartQuote.CreateXmlDeclaration("1.0", "utf-8", null);
		XMLDocSmartQuote.AppendChild(xmlDeclaration);

		root = XMLDocSmartQuote.CreateElement("EclipseHeaders");
		XMLDocSmartQuote.AppendChild(root);


		foreach ( quoteItem in RootItem.Children) {
			// Create an EclipseHeader for each system in the quote

			if (quoteItem.Branch.Product != null) {

				if (quoteItem.Branch.Product.isSystem(quoteItem.Path)) {
					eclipseHeader = FilledXMLSmartQuoteHeader(XMLDocSmartQuote, quoteItem, errorMessages);
					root.AppendChild(eclipseHeader);

					eclipseLineItems = eclipseHeader.AppendChild(XMLDocSmartQuote.CreateElement("EclipseLineItems"));

					foreach (clsFlatListItem flatListItem in quoteItem.Flattened(true, false, -1).items) {
						eclipseLineItems.AppendChild(flatListItem.XMLSmartQuoteLine(XMLDocSmartQuote, this, errorMessages));
					}

				}
			}
		}

	}

	public clsQuote CreateNextVersion(List<string> errorMessages)
	{
		return CreateNextVersion(null, 0, errorMessages);
	}

}

//Return New clsquote(Me) 'calls the special constructor which bases one (cloned) quote upon another - incrementing the version - and updating the pricing (to what is current for the quote buyeraccount)


//NB it's the AGENT that holds the quotes (not the buyer)
