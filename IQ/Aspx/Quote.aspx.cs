using System;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Xml.Serialization;
using System.Globalization;

//Option Strict On

public class quote : clsPageLogging
{

	public enum QuoteStyleEnum
	{
		Hierarchical,
		flat
	}

	private void  // ERROR: Handles clauses are not supported in C#
quote_Init(object sender, System.EventArgs e)
	{
		this.EnableViewState = false;
	}
	private clsQuote quote;
	private UInt64 lid;

	protected void  // ERROR: Handles clauses are not supported in C#
Page_Load(object sender, System.EventArgs e)
	{
		if (!clsIQ.IsLoaded){Response.Redirect("Loading.aspx", false);return;
}
		//vegas

		UInt64 lid = 0;
		if (!UInt64.TryParse(Request("lid"), lid))
			return;

		//Dim fi As WebControls.Image = Nothing 'Fetched Image (used to attach script to for the fetching preinstalled )
		int updateHandle = 0;
		//used to fetch pricing for the preinstalled parts
		bool displayContext = false;

		List<string> msgs = new List<string>();
		//  lid = Convert.ToUInt64(Request("lid"))

		if (iq.SeshAlive(lid)) {
			//called primarily from the flex() javascript in tree.aspx
			quote = null;
			int qty = 0;
			bool Absolute;
			int ItemID = 0;
			clsBranch Branch = null;
			//the last segment of the path IS the branch
			clsVariant SKUvariant = null;

			clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
			clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
			string ru = Request.RawUrl;

			int qiid = (int)Request("qiid");
			//quote item ID
			string path = Request("Path");

			int qid = 0;

			if (iq.SeshContains(lid, "QuoteID")) {
				qid = (int)iq.sesh(lid, "QuoteID");
				//use the session variable
			}
			if ((int)Request("QuoteID") != 0) {
				qid = (int)Request("QuoteID");
				//but override if it's specified as a request parameter... (from the QuoteList screen)
				iq.sesh(lid, "QuoteID") = qid;
			}
			Literal savedlabel = new Literal();
			string saveResult = "";
			//  If Request("QuoteView") <> "" Then iq.sesh(lid, "QuoteView") = Request("QuoteView") 'persist any change to the quote view type (breakdown or summary) - set when a header is clicked, by a call through setQuoteView
			if (qid != 0) {
				clsAccount ac = agentAccount;
				if (!ac.Quotes.ContainsKey(qid)) {
					ac.LoadQuotes(0);
				}
				//Should always be Agent account.quotes instead of iq.quotes
				quote = ac.Quotes(qid);
				//Commented the if statement as changes were not being reflected whent the quote refreshed 
				if (quote.RootItem.Children.Count == 0) {
					quote.LoadItems(errorMessages);
				}
			}

			if (Request("cmd") == "Upsell") {
				Pnlquote.Controls.Add(NewLit("!EndQuote"));
				if (quote != null) {
					//We get the VM's for the currently selected system
					if (quote.Cursor != null) {
						foreach ( m in quote.Cursor.Flattened(true, true, 0).items) {
							if (object.ReferenceEquals(m.QuoteItem, quote.Cursor)) {
								foreach ( vm in m.QuoteItem.AllChildMsgs) {
									if (vm.type == enumValidationMessageType.Upsell) {
										Pnlquote.Controls.Add(vm.UIExpanded(buyerAccount, agentAccount.Language, errorMessages, quote.ID));
									}
								}
							}
						}
					}
				}

				Pnlquote.Controls.Add(NewLit("!EndUpsells"));
			} else {
				if (!string.IsNullOrWhiteSpace(Request("itemID"))) {
					ItemID = (int)Request("itemID");
				}

				if (!string.IsNullOrWhiteSpace(Request("qty"))) {
					//We are editing this, if its not an active quote then create a new version (new quote workflow 16/12/14)

					qty = (int)Request("qty");
					Absolute = (Request("absolute") == "true");

					if (quote != null && quote.Locked) {
						quote = quote.CreateNextVersion(ItemID, qty, errorMessages);
						iq.sesh(lid, "QuoteID") = quote.ID;
						iq.sesh(lid, "quoteCursor") = quote.RootItem.Children(0).ID;
					} else {
						if (ItemID != 0) {
							quote.SetQtyByItemID(ItemID, qty, Absolute, quote.RootItem.Margin, errorMessages);
						//ItemID is a QUOTE item - so things are a little simpler (this is when twiddling an existing item in the quote)
						} else {
							if (Request("SKUvariantID") == "") {
								//Guess it?

								if (!quote == null && quote.RootItem.Children.Count > 0) {
									object rootVariant = quote.RootItem.Children.First.SKUVariant;
									//for debug/wathcing
									object pn = PathName(path);
									SKUvariant = iq.Branches(Split(path, ".").Last).Product.Variants.Values.Where(v => object.ReferenceEquals(v.sellerChannel, buyerAccount.SellerChannel) && v.Localisation == rootVariant.Localisation && v.Warehouse == rootVariant.Warehouse).FirstOrDefault;
									if (SKUvariant == null)
										SKUvariant = iq.Branches(Split(path, ".").Last).Product.Variants.Values.FirstOrDefault;
									clsPrice p;
									if (SKUvariant != null) {
										if (SKUvariant.prices != null && SKUvariant.prices.Count == 0) {
											p = new clsPrice(SKUvariant, iq.priceBands(""), new NullablePrice(buyerAccount.Currency), "CPQJIT");
										} else {
											p = SKUvariant.prices.Values.FirstOrDefault;
										}

									} else {
									}
								}

								if (UserIsAdmin(lid)) {
									errorMessages.Add("SkuvariantID was absent for SetQtyByPath");
								}

							} else {
								if (iq.Variants.ContainsKey((int)Request("SKUvariantID"))) {
									SKUvariant = iq.Variants((int)Request("SKUvariantID"));
									//  'Branch.Product.Variants(BuyerAccount.SellerChannel)(Request("SKUvariantID"))
								} else {
									SKUvariant = iq.AllVariants;
								}
							}
							if (SKUvariant != null) {
								if (Request("qty") == "") {
									errorMessages.Add("qty was '' or SetQtyByPath");
								} else if (SKUvariant.Product.Manufacturer != agentAccount.Manufacturer) {
									errorMessages.Add(GetSplitMessage(agentAccount.Manufacturer, agentAccount.Language));
								} else {
									if (quote == null) {
										//START A NEW QUOTE
										if (agentAccount.BuyerChannel.Region.Code == "BR" & iq.sesh(lid, "custContext") == null) {
											displayContext = true;
										}
										quote = new clsQuote(agentAccount, buyerAccount, null, Now, Now, 1, iq.i_state_GroupCode("QT-#NW"), new NullablePrice(buyerAccount.Currency), buyerAccount.Currency, false,
										false, false, string.Empty, new nullableString(), new nullableString(), 0);
										iq.sesh(lid, "QuoteID") = quote.ID;

									}
								}


								//was commented - and shouldnt have been !
								iq.sesh(lid, "Paradigm") = enumParadigm.configuringSystem;

								if (!quote == null && quote.Locked == false) {
									clsQuoteItem i = quote.setQtyByPath(Request("path"), SKUvariant, qty, Absolute, 1, errorMessages);
									if (i != null) {
										i.Margin = i.Parent.Margin;
										//Inherrit margin from the parent item Basecamp thread 044

										if (i.Branch.Product.isSystem(path)) {
											//Adds the preisntalled componentry and returns a handle to the webservice call which will get prices for them
											updateHandle = i.fetchPreinstalled(lid, buyerAccount, errorMessages);


										}
										//quote.MostRecent = i
										//quote.Cursor = i
										if (i.Branch.Product.isSystem(Request("path")))
											iq.sesh(lid, "quoteCursor") = i.ID;
									}
								}
								//set the quote cursor to the new Item *only* if its branch has children (is a (sub) system)

								//If quote.MostRecent.Quantity > 0 And quote.MostRecent.Branch.childBranches.Count > 0 Then
								// quote.Cursor = quote.MostRecent '.ID
								//End If
							}
						}
					}
				} else {
					// Quote is being reloaded 

				}


				if (quote != null) {
					string buttonCommand = Request("cmd");
					if (!(string.IsNullOrEmpty(buttonCommand))) {
						string quoteName = Request.QueryString("quoteName");

						switch (buttonCommand) {
							case "Save":
								saveResult = quote.Save(quoteName, lid);
							case "Email":
								if (Request("originalName") != null)
									quoteName += "|" + Request("originalName");
								saveResult = EmailQuote(quoteName, Request("email"));
							case "PDF":
								if (quote.PassesValidation(lid))
									saveResult = quote.ExportPDF(lid, MapPath("../drive.p12"), quoteName, errorMessages);
								else{Pnlquote.Controls.Add(NewLit("!Result!VF!!"));return;
}
							case "Excel":
								if (quote.PassesValidation(lid))
									saveResult = quote.ExportExcel(lid, quoteName, errorMessages, false);
								else{Pnlquote.Controls.Add(NewLit("!Result!VF!!"));return;
}
							case "XMLAdv":
								if (quote.PassesValidation(lid))
									saveResult = quote.ExportXMLAdv(lid, quoteName, errorMessages);
								else{Pnlquote.Controls.Add(NewLit("!Result!VF!!"));return;
}
							case "XML":
								if (quote.PassesValidation(lid))
									saveResult = quote.ExportXML(lid, quoteName, errorMessages);
								else{Pnlquote.Controls.Add(NewLit("!Result!VF!!"));return;
}
							case "XMLSmartQuote":
								if (quote.PassesValidation(lid))
									saveResult = quote.ExportXMLSmart(lid, quoteName, errorMessages);
								else{Pnlquote.Controls.Add(NewLit("!Result!VF!!"));return;
}
							case "Addbasket":
								saveResult = quote.Save(quoteName, lid);
								quote.Locked = true;
								quote.State = iq.i_state_GroupCode("QT-#WN");
								quote.Update();

								if (quote.AgentAccount.SellerChannel.Code.StartsWith("DSYUS") | quote.AgentAccount.SellerChannel.Code.StartsWith("DSYCA")) {
									// Dim sessionID As String = iq.sesh(lid, "GK_SessionID")
									if (iq.sesh(lid, "GK_BasketURL") != null) {
										string urlString = addtoBasketSynnex(lid);
										urlString = iq.sesh(lid, "GK_BasketURL").ToString() + urlString;
										saveResult = "SYNNEX|" + urlString;
									}

								} else if (quote.AgentAccount.SellerChannel.orderEmail != "") {
									string emailQuote__1 = addToBasket(lid);
									saveResult = EmailBasket(lid);



								} else {
									saveResult = addToBasket(lid);

								}
							case "MarkAsWon":
								if (quote.PassesValidation(lid))
									quote.MarkAsWon(lid);
								else{Pnlquote.Controls.Add(NewLit("!Result!VF!!"));return;
}
						}
					}



					string qc = Request("quoteCursor");
					if ((qc == null || qc == "undefined") && iq.seshDic(lid).ContainsKey("quoteCursor") && quote.RootItem.FindRecursive((int)iq.sesh(lid, "quoteCursor")) != null)
						qc = iq.sesh(lid, "quoteCursor");
					if (qc == null & quote.RootItem.Children.Count == 1 & qty == -1) {
						qc = quote.RootItem.Children(0).ID;
					}
					if (qc != "undefined" & qc != null) {
						//start at the quote cursor when attemting to find the item to flex
						//     Dim quotecursor As clsQuoteItem
						iq.sesh(lid, "quoteCursor") = qc;

						if ((int)qc != 0) {
							string cmd = Request("cmd");

							clsQuoteItem qi;
							qi = quote.RootItem.FindRecursive((int)qc);

							if (qi == null)
								System.Diagnostics.Debugger.Break();
							//temproaror

							//Don't Move quote cursor if theyr'e 'only' collapsing
							if (cmd != "collapse" & cmd != "closePanel") {
								quote.Cursor = qi;
								quote.MostRecent = qi;
								//new
							}

							if (cmd == "expand")
								qi.collapsed = false;
							if (cmd == "collapse")
								qi.collapsed = true;

							//tab switching with a tab=x command
							string[] bits = Split(Request("cmd"), "=");
							if (bits(0) == "openPanel")
								qi.ExpandedPanels.Add((panelEnum)bits(1));
							if (bits(0) == "closePanel")
								qi.ExpandedPanels.Remove((panelEnum)bits(1));

							if (bits(0) == "margin") {
								bool propagate = (bool)Request("propagate");
								float margin = bits(1);
								bool clamped = true;
								 // ERROR: Not supported in C#: WithStatement


								if (bits(2) == "R") {
									//retained margin
									qi.ApplyMargin(100 / (100 - margin), propagate);
									//eg * 1/.98

								} else if (bits(2) == "C") {
									//cost plus
									qi.ApplyMargin((100 + margin) / 100, propagate);
								} else {
									errorMessages.Add("Unknown margin type (not R or C):" + bits(2));
								}
								qi.updateRecursive();
							}
						}
					}
				}


				if (quote != null) {
					if (quote.Locked == true) {
						iq.sesh(lid, "QuoteLocked") = true;
					} else {
						iq.sesh(lid, "QuoteLocked") = false;
					}

					quote.Update();

				}


				//build a hashset from the CD list stored in the sesstion variable
				HashSet<string> foci = new HashSet<string>(Split(iq.sesh(lid, "foci"), ",").ToList);


				//If the 'inviisible/placeholding 'rootitem' has no children... there's nothing in the basket
				if (quote == null || quote.RootItem.Children.Count == 0) {
					Pnlquote.Controls.Add(EmptyQuote(buyerAccount, lid));
				} else {
					if (Request("cmd") == null || Request("cmd") != "Upsell")
						quote.validate(lid, buyerAccount, agentAccount, errorMessages);
					Pnlquote.Controls.Add(NewLit("!BeginQuote"));

					if (quote.RootItem.Children.Count == 1) {
						iq.sesh(lid, "lastbranch") = quote.RootItem.Children(0).Path;
					}
					//If fi IsNot Nothing Then
					// Pnlquote.Controls.Add(fi)
					// End If

					Pnlquote.Controls.Add(outputMessages(msgs));

					//Pnlquote.Controls.Add(MarginUI(quote))
					Pnlquote.Controls.Add(quote.UI(foci, lid));

				}

				//used for Export to Excel and pdf . The value triggers the js script to call streamer.aspx
				saveResult = "<input type = 'hidden' value = '" + saveResult + "' id='hdnMsgValue' />";
				Pnlquote.Controls.Add(NewLit(saveResult));
				OutputErrors(Pnlquote.Controls, errorMessages, lid, true);

				if (quote != null) {
					if (Request("qty") != "") {
						qty = (int)Request("qty");
						if (qty <= 0) {
							Literal lit = new Literal();
							lit.Text = "<input type=\"hidden\" name=\"previousPath\" value=\"" + quote.MostRecent.Path + "\"  />";
							Pnlquote.Controls.Add(lit);
						}
					}

					if (displayContext) {
						Literal lit = new Literal();
						lit.Text = "<input type=\"hidden\" id=\"wareHouseHidden\" value=\"True\"  />";
						Pnlquote.Controls.Add(lit);
					}

				}

				Pnlquote.Controls.Add(NewLit("!EndQuote"));


				if (quote != null) {
					//We get the VM's for the currently selected system
					if (quote.Cursor != null) {
						foreach ( m in quote.Cursor.Flattened(true, true, 0).items) {
							if (object.ReferenceEquals(m.QuoteItem, quote.Cursor)) {
								foreach ( vm in m.QuoteItem.AllChildMsgs) {
									if (vm.type == enumValidationMessageType.Upsell) {
										Pnlquote.Controls.Add(vm.UIExpanded(buyerAccount, agentAccount.Language, errorMessages, quote.ID));
									}
								}
							}
						}
					}
				}

				Pnlquote.Controls.Add(NewLit("!EndUpsells"));
				Pnlquote.Controls.Add(NewLit("!BeginUpdateHandle"));

				//well always write the updatehandle - which will me 0 if there's nothing to fetch
				//the JS will only check prices (and refresh the quote!) if there was an updatehandle (something to check !)
				Pnlquote.Controls.Add(NewLit(updateHandle + "!EndUpdateHandle"));

			}
		}

	}

	private PlaceHolder EmptyQuote(clsAccount agentaccount, UInt64 lidlocal = 0)
	{

		EmptyQuote = new PlaceHolder();
		Literal lit;

		lit = new Literal();
		lit.Text = "!BeginQuote<!-EmptyQuote-->";
		//Marker (for the JS) that the quote is empty and the export tools (pnlQuoteTools) should be hidden
		EmptyQuote.Controls.Add(lit);
		string oldpath = iq.sesh(lidlocal, "lastbranch");
		string[] pathArr = Split(oldpath, ".");
		string previousPath = string.Join(".", pathArr.Take(pathArr.Length - 2));
		lit = new Literal();

		if (oldpath != null) {
			iq.sesh(lidlocal, "path") = previousPath;
			lit.Text = "<input type=\"hidden\" name=\"previousPath\" value=\"" + previousPath + "\"  />";
			//Xlt("Your quote is empty", agentaccount.Language)
		} else {
			lit.Text = "";
			//Xlt("Your quote is empty", agentaccount.Language)
		}

		EmptyQuote.Controls.Add(lit);


		if (agentaccount.BuyerChannel.Region.Code == "BR" & iq.sesh(lidlocal, "Quote") != null) {
			lit = new Literal();
			lit.Text = "<input type=\"hidden\" id=\"wareHouseHidden\" value=\"True\"  />";
			// Pnlquote.Controls.Add(lit)
			iq.sesh(lidlocal, "custContext") = null;
			EmptyQuote.Controls.Add(lit);
		}
		lit = new Literal();
		lit.Text = "!EndQuote";
		//no id returned (we've not started a quote yet)
		EmptyQuote.Controls.Add(lit);

	}


	private string EmailQuote(string quoteName, string emailto)
	{

		object state = "";

		string[] splitNames = Split(quoteName, "|");

		UInt64 lid = Request.QueryString("lid");
		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");


		if (iq.sesh(lid, "QuoteID") == null) {
			errorMessages.Add(Xlt("You must add something to the quote first", agentAccount.Language));

		} else {
			state = "RQ";
			object fullpath;

			clsQuote QUOTE = agentAccount.Quotes(iq.sesh(lid, "QuoteID"));
			QUOTE.Locked = true;
			QUOTE.Saved = true;
			state = "UQ";
			if (Trim(splitNames(1)).Length > 0) {
				QUOTE.Name = new nullableString(Trim(splitNames(1)));
			} else if (QUOTE.Name.sqlValue == "null") {
				QUOTE.Name = new nullableString(agentAccount.User.RealName);

			}
			QUOTE.Update();
			QUOTE.ExportLogging("Email");

			state = "OQ";
			string errors = "";
			fullpath = ODS.OutputQuote(QUOTE, "Quotes", errorMessages);
			//the OutputQuote() function returns the full physical path the the file generated on the server

			state = "AR";


			object vPath = HttpContext.Current.Request.ApplicationPath;
			object pPath = HttpContext.Current.Request.MapPath(vPath) + "\\";


			state = "SR " + pPath;
			StreamReader tr = null;
			object b = "";
			try {
				tr = new StreamReader(pPath + "EMT/quote.htm");
				b = tr.ReadToEnd();
				tr.Close();
			} catch (System.Exception ex) {
				tr.Dispose();
			}

			//Tags are...
			//<subject>Welcome to Iqoute 2</subject>
			//<p>Dear <customerName/>,</p>
			//<p>Your <hostName/> iQuote quotation ID:<quoteID/> prepared for you by <agentName/> is shown below - You will also find an spreadsheet compatible version attached.
			//<quoteBody/>

			state = "RT ";
			Dictionary<string, string> tags = new Dictionary<string, string>();
			tags.Add("customerName", Split(buyerAccount.User.RealName, " ")(0));
			tags.Add("quoteID", QUOTE.RootQuote.ID + "-" + QUOTE.Version.ToString);
			tags.Add("hostName", buyerAccount.SellerChannel.DisplayName(buyerAccount.Language));
			tags.Add("agentName", agentAccount.User.RealName);
			tags.Add("agentEmail", agentAccount.User.Email);
			tags.Add("mfr", agentAccount.Manufacturer.ToString());

			NullablePrice runningTotal = new NullablePrice(buyerAccount.Currency);
			object qb = QUOTE.RootItem.EmailSummary(true, buyerAccount, agentAccount, errorMessages, runningTotal);
			tags.Add("quoteBody", qb);

			object to;
			// to$ = buyerAccount.User.Email
			to = splitNames(0);

			System.Net.Mail.Attachment attachment = null;
			if (errorMessages.Count == 0 & fullpath != "") {
				attachment = new System.Net.Mail.Attachment(fullpath);
				tags.Add("attachmentInfo", " You will find a spreadsheet compatible version attached.");
			} else {
				tags.Add("attachmentInfo", "The spreadsheet compatible attachment is not presently available - but please contact us if you require one.");
			}

			SendEmail(to, "quote.htm", tags, buyerAccount.Language, errorMessages, false, attachment);
			//agentAccount.User.Email

			if (errorMessages.Count > 0)
				SimpleEmail("Support@channelcentral.net", "iQuote2 - config issue", Join(errorMessages.ToArray, ","));


			//state$ = "IC "
			//Dim smtpclient As New System.Net.Mail.SmtpClient


			//msg = New MailMessage("support@channelcentral.net", to$, "Your iQuote 2 quotation" & QUOTE.RootQuote.ID & "-" & QUOTE.Version & " from " & buyerAccount.SellerChannel.DisplayName(buyerAccount.Language), b$)

			//msg.ReplyToList.Add(New MailAddress(AgentAccount.User.Email))
			//msg.CC.Add(New MailAddress("support@channelcentral.net"))
			//msg.CC.Add(New MailAddress(AgentAccount.User.Email))  'CC the agent

			if (errorMessages.Count == 0) {
				//LblSave.BackColor = Drawing.Color.Green
				//LblSave.ForeColor = Drawing.Color.White
				return Xlt("Mail sent successfully", agentAccount.Language);
			} else {
				//sendmail will have added errors if it failed (which will be output below)

			}
		}
		return string.Join(",", errorMessages.ToArray());
		//PnlErrors.Controls.Add(OutputErrors(errorMessages, lid))
	}

	private string addtoBasketSynnex()
	{

		//Returns the required parameters to the GET string 

		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");
		quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"));

		//the iq.Sesh(lid,"gk_BasketURL") should contain:-
		//http://ec.synnex.com/ecexpress/order/shoppingCart.do'

		object r = "?subAction=6&ref_id=" + iq.sesh(lid, "gk_token") + "&newBasket=" + quote.ID + "&quickAdd=";

		foreach ( flatListItem in quote.RootItem.Flattened(true, false, 0).items) {
			r += flatListItem.QuoteItem.SKUVariant.DistiSku + "+" + flatListItem.Quantity + ",";
		}

		//which it will be !
		if (Right(r, 1) == ",") {
			r = Left(r, Len(r) - 1);
			//remove the last comma
		}

		return r;

	}

	private string addToBasket(ulong lidLocal, bool ignoreCheck = false)
	{
		clsAccount agentAccount = (clsAccount)iq.sesh(lidLocal, "AgentAccount");

		if (quote != null) {
			string url = IIf(iq.sesh(lidLocal, "GK_BasketURL") == null, "", iq.sesh(lidLocal, "GK_BasketURL"));
			string xmlString = "";

			if (agentAccount.SellerChannel.basketMode == "FRM") {
				xmlString = quote.basketAsHiddenFields(lidLocal);

			} else {
				if (url.Length > 0 | quote.AgentAccount.SellerChannel.orderEmail != "") {
					//Generate the xml using the proxy class
					Data dt = new Data();
					dt.Quote = new DataQuote();
					dt.Quote.ID = quote.ID;
					dt.Quote.Name = quote.Name.value;
					dt.Quote.CreatedBy = quote.AgentAccount.User.RealName;
					dt.Quote.Supplier = quote.AgentAccount.SellerChannel.Name;
					//dt.Quote.URLProductImage = quote.RootItem.Note.value 'need to ask nick abt this
					List<DataQuoteProduct> products = new List<DataQuoteProduct>();
					DataQuoteProduct product;
					foreach ( flatListItem in quote.RootItem.Flattened(true, false, 0).items) {
						product = new DataQuoteProduct();
						product.Class = flatListItem.QuoteItem.Branch.Product.ProductType.Code;
						product.PartNum = flatListItem.QuoteItem.Branch.Product.SKU;
						product.SupplierPartNum = flatListItem.QuoteItem.SKUVariant.DistiSku;

						product.ListPrice = flatListItem.QuoteItem.ListPrice.value;
						product.Description = flatListItem.QuoteItem.Branch.DisplayName(quote.BuyerAccount.Language);
						product.Qty = flatListItem.Quantity;
						product.URLProductImage = flatListItem.QuoteItem.Branch.Picture;

						products.Add(product);

					}
					dt.Quote.Product = products.ToArray();

					xmlString = SerializeToString(dt);

				}
			}

			iq.sesh(lidLocal, "basketContent") = xmlString;

			Uri trueUri = new Uri(Request.Url.AbsoluteUri);
			string uri = "BasketPost.aspx";

			return uri;
		}
	}

	private string addtoBasketSynnex(UInt64 lidLocal)
	{

		//Returns the required parameters to the GET string 

		clsAccount agentAccount = (clsAccount)iq.sesh(lidLocal, "AgentAccount");
		quote = agentAccount.Quotes(iq.sesh(lidLocal, "QuoteID"));

		//the iq.Sesh(lid,"gk_BasketURL") should contain:-
		//http://ec.synnex.com/ecexpress/order/shoppingCart.do'

		object r = "?subAction=6&ref_id=" + iq.sesh(lidLocal, "gk_token") + "&newBasket=" + quote.ID + "&quickAdd=";

		foreach ( flatListItem in quote.RootItem.Flattened(true, false, 0).items) {
			r += flatListItem.QuoteItem.SKUVariant.DistiSku + "+" + flatListItem.Quantity + ",";
		}

		//which it will be !
		if (Right(r, 1) == ",") {
			r = Left(r, Len(r) - 1);
			//remove the last comma
		}

		return r;

	}

	private string EmailBasket(ulong lidlocal)
	{
		//find the virtual, and from that the physical path to the app folder

		clsAccount buyerAccount = (clsAccount)iq.sesh(lidlocal, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lidlocal, "AgentAccount");
		clsQuote QUOTE = agentAccount.Quotes(iq.sesh(lidlocal, "QuoteID"));
		object vPath = HttpContext.Current.Request.ApplicationPath;
		object pPath = HttpContext.Current.Request.MapPath(vPath) + "\\";

		string tf;
		string fn;
		fn = "Quotes\\" + QUOTE.RootQuote.ID + "-" + QUOTE.Version + ".txt";
		tf = pPath + fn;

		try {
			if (My.Computer.FileSystem.FileExists(tf))
				My.Computer.FileSystem.DeleteFile(tf);
		} catch (Exception ex) {
			ErrorLog.Add(ex);

		}

		System.IO.StreamWriter objWriter = new System.IO.StreamWriter(tf);
		objWriter.WriteLine(iq.sesh(lidlocal, "basketContent"));
		objWriter.Close();

		Dictionary<string, string> tags = new Dictionary<string, string>();
		tags.Add("customerName", Split(buyerAccount.User.RealName, " ")(0));
		tags.Add("quoteID", QUOTE.RootQuote.ID + "-" + QUOTE.Version.ToString);
		tags.Add("hostName", buyerAccount.SellerChannel.DisplayName(buyerAccount.Language));
		tags.Add("agentName", agentAccount.User.RealName);
		tags.Add("agentEmail", agentAccount.User.Email);
		tags.Add("mfr", agentAccount.Manufacturer.ToString());

		NullablePrice runningTotal = new NullablePrice(buyerAccount.Currency);
		object qb = QUOTE.RootItem.EmailSummary(true, buyerAccount, agentAccount, errorMessages, runningTotal);
		tags.Add("quoteBody", qb);

		string toEmail = agentAccount.SellerChannel.orderEmail;

		System.Net.Mail.Attachment attachment = null;
		if (errorMessages.Count == 0 & tf != "") {
			attachment = new System.Net.Mail.Attachment(tf);
			tags.Add("attachmentInfo", " You will find basket xml attached");
		} else {
			tags.Add("attachmentInfo", "Failed to generate attachment.");
		}

		SendEmail(toEmail, "quote.htm", tags, buyerAccount.Language, errorMessages, false, attachment);
		//agentAccount.User.Email

		if (errorMessages.Count > 0)
			SimpleEmail("Support@channelcentral.net", "iQuote2 - config issue", Join(errorMessages.ToArray, ","));

		if (errorMessages.Count == 0) {
			return Xlt("Mail sent successfully", agentAccount.Language);
		} else {
			//sendmail will have added errors if it failed (which will be output below)
			return Xlt("Failed to send email", agentAccount.Language);
		}

	}


}
