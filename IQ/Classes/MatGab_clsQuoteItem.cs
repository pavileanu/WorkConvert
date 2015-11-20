
using System.Xml;
using dataAccess;
using System.Globalization;



public class clsQuoteItem : ISqlBasedObject
{
	private const string PleaseCreateNewVersionToChangeQuote = "Please create new version to change quote.";
	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	public clsQuote quote;
	private int Indent {
		get { return m_Indent; }
		set { m_Indent = Value; }
	}
	private int m_Indent;

	private clsBranch Branch {
		get { return m_Branch; }
		set { m_Branch = Value; }
	}
	private clsBranch m_Branch;
	private clsVariant SKUVariant {
		get { return m_SKUVariant; }
		set { m_SKUVariant = Value; }
	}
	private clsVariant m_SKUVariant;

	private string Path {
		get { return m_Path; }
		set { m_Path = Value; }
	}
	private string m_Path;
	//The numeric (dotted) path to this item in the product tree
	private int Quantity {
		get { return m_Quantity; }
		set { m_Quantity = Value; }
	}
	private int m_Quantity;
	private NullablePrice BasePrice {
		get { return m_BasePrice; }
		set { m_BasePrice = Value; }
	}
	private NullablePrice m_BasePrice;
	//Pre margin 'quoted'  - price (from webserivce, or wherever)
	private NullablePrice ListPrice {
		get { return m_ListPrice; }
		set { m_ListPrice = Value; }
	}
	private NullablePrice m_ListPrice;
	// contains a snapshot of  the list price at the time the quote was prepared

	private nullableString OPG {
		get { return m_OPG; }
		set { m_OPG = Value; }
	}
	private nullableString m_OPG;
	private nullableString Bundle {
		get { return m_Bundle; }
		set { m_Bundle = Value; }
	}
	private nullableString m_Bundle;
	//an OPG can have many bundles
	private decimal rebate {
		get { return m_rebate; }
		set { m_rebate = Value; }
	}
	private decimal m_rebate;
	//amount of cash off this item for this OPG

	private float Margin {
		get { return m_Margin; }
		set { m_Margin = Value; }
	}
	private float m_Margin;

	private List<clsQuoteItem> Children {
		get { return m_Children; }
		set { m_Children = Value; }
	}
	private List<clsQuoteItem> m_Children;
	private clsQuoteItem Parent {
		get { return m_Parent; }
		set { m_Parent = Value; }
	}
	private clsQuoteItem m_Parent;

	private List<ClsValidationMessage> Msgs {
		get { return m_Msgs; }
		set { m_Msgs = Value; }
	}
	private List<ClsValidationMessage> m_Msgs;
	//String 'error (or other) message against this quote line item
	private bool IsPreInstalled {
		get { return m_IsPreInstalled; }
		set { m_IsPreInstalled = Value; }
	}
	private bool m_IsPreInstalled;
	//Means you cant remove it from the quote - or flex its quantities (adding will add a new quote item)
	private DateTime Created {
		get { return m_Created; }
		set { m_Created = Value; }
	}
	private DateTime m_Created;
	private bool validate {
		get { return m_validate; }
		set { m_validate = Value; }
	}
	private bool m_validate;
	//Whether to include in validation (or not)

	private nullableString Note {
		get { return m_Note; }
		set { m_Note = Value; }
	}
	private nullableString m_Note;

	private bool collapsed {
		get { return m_collapsed; }
		set { m_collapsed = Value; }
	}
	private bool m_collapsed;
	private HashSet<panelEnum> ExpandedPanels {
		get { return m_ExpandedPanels; }
		set { m_ExpandedPanels = Value; }
	}
	private HashSet<panelEnum> m_ExpandedPanels;
	// panelEnum  'System = 1, Options = 2, Spec = 4, Validation = 8, Promo= 16
	private int order {
		get { return m_order; }
		set { m_order = Value; }
	}
	private int m_order;
	//this is not yet persited

	//Property price As clsPrice

	EnumHideButton FlexButtonState;
	private int ImportId;
	private bool allRulesQualified = false;
		//the slot summaries ('specs' are built at a system level 'outside' the quote items via quote.validate
	public Panel specPanelOpen;

	public Panel specPanelClosed;
	private Dictionary<clsSlotType, clsSlotSummary> dicslots {
		get { return m_dicslots; }
		set { m_dicslots = Value; }
	}
	private Dictionary<clsSlotType, clsSlotSummary> m_dicslots;

	//Public Sub Clone(onToQuote As clsquote)

	//    'recursively clones this quote item and all it's children - creating copies with new ID's

	//    Dim anitem As clsquoteItem = Nothing

	//    'If Me IsNot Me.quote.RootItem Then
	//    'we don't clone the root item as the new (empty) quote already has one
	//    'makes a new copy of me (the quote item) onto 'ontqoquote'

	//    'this constructor is recursive ! - it makes a whole set of itesm 0 attached to the quote
	//    anitem = New clsquoteItem(Me, onToQuote)
	//    'End If

	//    '     For Each item In Me.Children
	//    '     item.Clone(onToQuote)
	//    '     Next

	//End Sub
	//bulid a dictionary of quanties,by prodRef, by system


	/// <summary>
	/// This method is called on the quote.rootitem after price updates for a variant have arrived - it recurses the entire quote updating the price on any QuoteItems which refer to this skuvariant
	/// </summary>
	/// <param name="skuvariant"></param>
	/// <param name="price"></param>
	/// <remarks></remarks>

	public void updateQuotedPrice(clsVariant skuvariant, NullablePrice price)
	{

		if (object.ReferenceEquals(this.SKUVariant, skuvariant) && price.value >= 0) {
			this.BasePrice = price;

			//With Me.BasePrice
			//    .value = price
			//    .isValid = price.isValid
			//    .isList = price.isList
			//End With
			//save this quoteItem
			this.Update();
			//and continute to recures (as the same item may appear more than once in the quote

		}

		//iterate over an array (copy) of the list  (attempt to avoid a 'collection  modified enumeration may not execute' )
		foreach ( c in this.Children.ToArray) {
			c.updateQuotedPrice(skuvariant, price);
		}

	}

	public List<clsQuoteItem> findSystemItems()
	{

		//returns a flat list of the 'system' items (which are indepentely validated) - things like a Chassis

		findSystemItems = new List<clsQuoteItem>();

		if (this.Parent != null && !object.ReferenceEquals(this.Parent, this.quote.RootItem))
			return;

		//the root item has no branch (is just a placeholder)
		if (!this.Branch == null) {
			if (this.Branch.Product.isSystem(this.Path))
				findSystemItems.Add(this);
		}



		foreach ( child in this.Children) {
			findSystemItems.AddRange(child.findSystemItems);
		}

	}



	public void ApplyMargin(float factor, bool propagate)
	{
		this.Margin = factor;
		if (propagate) {
			foreach ( c in this.Children) {
				c.ApplyMargin(factor, propagate);
			}
		}

		//     Me.Update()

	}


	public void doWarnings(Dictionary<clsSlotType, clsSlotSummary> dicslots)
	{
		//called for each top level item

		if (this.Branch.Product.isSystem(this.Path)) {
			//this isn't pretty - but becuase of the generic nature of 'products' we don't know what they 'are' (Desktop, notebook, server, storage) - so we read *where* they are (from the first level of the tree) to derive their fundamental type
			//should probably just add a sysType attribute - but validation would be its only purpose (at present)
			string systype;
			[] p = Split(this.Path, ".");
			systype = LCase(iq.Branches((int)p(2)).Translation.text(English));


			if (iq.ProductValidationsAssignment.ContainsKey(systype)) {
				//build list of components
				object parts = this.listComponents(false);

				foreach ( ProdVal in iq.ProductValidationsAssignment(systype)) {
					string pth = "";
					switch (ProdVal.ValidationType) {
						case enumValidationType.MustHave:
							//Split multiple opt types with a /
							bool pres = false;
							foreach ( ot in ProdVal.RequiredOptType.Split("/".ToArray)) {
								if (string.IsNullOrEmpty(pth))
									pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ot, true);
								//only populate the path if it isnt already (for instances like PCI and MOD network cards where one system may have one one may have the other)

								if (parts.Where(par => par.OptType // ERROR: Unknown binary operator Like
).Count == 0) {
								//Option type not present
								} else if (!string.IsNullOrEmpty(ProdVal.OptionFamily) && parts.Where(pa => pa.hasAttribute("optFam", ProdVal.OptionFamily, true)).Count == 0) {
								//OptionFamily specified but does not exist

								} else if (!string.IsNullOrEmpty(ProdVal.CheckAttributeValue) && parts.Where(pa => pa.hasAttribute(ProdVal.CheckAttribute, ProdVal.CheckAttributeValue, true)).Count == 0) {
								//Check attribute present but value not found
								} else {
									pres = true;
								}
							}


							if (!pres)
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, {
									
								}, null, "",
								null, ProdVal));
						case enumValidationType.MustHaveProperty:
							bool found = false;
							if (this.Branch.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute) && ((this.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation != null && this.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation.text(English) == ProdVal.CheckAttributeValue) | this.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.NumericValue.ToString == ProdVal.CheckAttributeValue))
								found = true;
							foreach ( pa in parts) {
								clsBranch prod = iq.Branches((int)Split(pa.Path, ".").Last);

								if (prod.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute) && ((prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation != null && prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation.text(English) == ProdVal.CheckAttributeValue) | prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.NumericValue.ToString == ProdVal.CheckAttributeValue))
									found = true;
							}

							if (!found)
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(ProdVal.RequiredOptType.Split("/".ToArray).First, ",")));
						//Not implemented yet
						case enumValidationType.Slot:
							//redundant
							System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dics = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();

							if (dics.Value == null)
								continue;
							pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true);
							if (dics.Value.Given < dics.Value.taken)
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(dics.Key.Translation.text(English) + "," + dics.Value.taken.ToString + "," + dics.Value.Given.ToString, ",")));
							else
								this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Split("")));
						case enumValidationType.CapacityOverload:
							//redundant
							System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dics = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
							if (dics.Value == null)
								continue;
							if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Sum(par => FindRecursive(par.Path, true).Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First().NumericValue) > dics.Value.TotalCapacity) {
								pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true);
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(dics.Key.Translation.text(English), ",")));
							} else {
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, ProdVal.Message, ProdVal.CorrectMessage, "", 0, 0, Split("")));
							}
						case enumValidationType.Dependancy:
							if (!string.IsNullOrEmpty(ProdVal.DependantCheckAttribute) || !string.IsNullOrEmpty(ProdVal.CheckAttribute)) {
								bool foundSource = false;
								bool foundTarget = false;
								foreach ( ot in ProdVal.DependantOptType.ToUpper.Split("/".ToArray)) {
									foreach ( i in parts.ToList().Select(pa => pa.Path)) {
										clsProduct part = FindRecursive(i, true).Branch.Product;
										//Get associated product
										if ((string.IsNullOrEmpty(ProdVal.CheckAttribute.ToUpper) || part.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute.ToUpper)) & part.i_Attributes_Code("optType").Select(f => f.Translation.text(English).ToUpper).Where(ppp => ProdVal.RequiredOptType.ToUpper.StartsWith(ppp.ToUpper)).Count > 0 && (string.IsNullOrEmpty(ProdVal.CheckAttribute) || part.i_Attributes_Code(ProdVal.CheckAttribute.ToUpper).Select(f => f.Translation != null ? f.Translation.text(English).ToUpper : f.NumericValue.ToString()).Contains(ProdVal.CheckAttributeValue.ToUpper))) {
											//We have a product of this type, find if we have the dependant one...
											foundSource = true;
										}
										if ((string.IsNullOrEmpty(ot) || part.i_Attributes_Code("optType").Select(f => f.Translation.text(English).ToUpper).Where(ppp => ot.ToUpper.StartsWith(ppp.ToUpper)).Count > 0) && part.i_Attributes_Code.ContainsKey(ProdVal.DependantCheckAttribute.ToUpper) && part.i_Attributes_Code(ProdVal.DependantCheckAttribute.ToUpper).Select(f => f.Translation != null ? f.Translation.text(English).ToUpper : f.NumericValue.ToString()).Contains(ProdVal.DependantCheckAttributeValue.ToUpper)) {
											foundTarget = true;
										}
									}
								}
								pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.DependantOptType, true);
								if (foundSource && !foundTarget)
									this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("")));
								//Else Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
							} else {
								if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Count > 0 && parts.Where(par => par.OptType == ProdVal.DependantOptType).Count == 0) {
									pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.DependantOptType, true);
									this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("")));
								} else {
									this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Split("")));
								}
							}
						case enumValidationType.Mismatch:
							if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Count > 0) {
								List<clsProductAttribute> o1 = null;
								List<clsProductAttribute> o2 = null;
								foreach ( i in parts.ToList().Where(pa => pa.OptType == ProdVal.RequiredOptType).Select(pa => pa.Path)) {
									o2 = FindRecursive(i, true).Branch.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute) ? FindRecursive(i, true).Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute) : null;
									if (o2 != null && o2(0).NumericValue > 0) {
										if (o1 != null && o1.Select(o1o => o1o.Translation == null ? o1o.NumericValue.ToString : o1o.Translation.text(English)).Intersect(o2.Select(o2o => o2o.Translation == null ? o2o.NumericValue.ToString : o2o.Translation.text(English))).Count != o1.Count) {
											pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true);
											this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(ProdVal.RequiredOptType, ",")));
											break; // TODO: might not be correct. Was : Exit For
										}
										o1 = o2;
										o2 = null;
									}
								}
							} else {
								//Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
							}
						case enumValidationType.NotToppedUp:
							System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dics = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
							if (dics.Value == null)
								continue;
							pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true);
							if (dics.Value.taken < dics.Value.Given)
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(dics.Key.Translation.text(English) + "," + dics.Value.taken.ToString + "," + dics.Value.Given.ToString, ",")));
							else
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Split("")));
						case enumValidationType.MultipleRequred:
							if (dicslots.Where(par => par.Key.MajorCode == ProdVal.RequiredOptType).Sum(ds => ds.Value.taken) % ProdVal.RequiredQuantity != 0) {
								pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true);
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("", ",")));
							} else {
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Split("")));
							}
						case enumValidationType.UpperWarning:
							System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dics = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
							if (dics.Value == null)
								continue;
							if (dics.Value.taken > ProdVal.RequiredQuantity) {
								pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true);
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split(dics.Key.Translation.text(English) + "," + dics.Value.taken.ToString + "," + dics.Value.Given.ToString, ",")));
							} else {
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Split("")));
							}
						case enumValidationType.Exists:
							if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Count > 0) {
								pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true);
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("")));
							} else {
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Split("")));
							}
						case enumValidationType.AtLeastSameQuantity:
							pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true);
							if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Count > 0 && parts.Where(par => par.OptType == ProdVal.DependantOptType).Count > 0) {
								if (dicslots.Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Sum(ds => ds.Value.taken) > dicslots.Where(ds => ds.Key.MajorCode == ProdVal.DependantOptType).Sum(ds => ds.Value.taken)) {
									this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Split("")));
								}
							}
						case enumValidationType.Divisible:
							System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dicsone = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
							System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dicstwo = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.DependantOptType).Select(ff => ff).FirstOrDefault();

							if (dicsone.Value == null || dicstwo.Value == null)
								continue;
							if (dicstwo.Value.taken != 0 && dicsone.Value.taken % dicstwo.Value.taken > 0)
								this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, "", 0, 0, Split("")));
						case enumValidationType.SpecRequirement:
							//Split multiple opt types with a /
							foreach ( ot in ProdVal.RequiredOptType.Split("/".ToArray)) {
								if (string.IsNullOrEmpty(pth))
									pth = this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ot, true);
								//only populate the path if it isnt already (for instances like PCI and MOD network cards where one system may have one one may have the other)

								if (parts.Where(par => par.OptType // ERROR: Unknown binary operator Like
).Count == 0) {
								//Option type not present
								} else if (!string.IsNullOrEmpty(ProdVal.OptionFamily) && parts.Where(pa => pa.hasAttribute("optFam", ProdVal.OptionFamily, true)).Count == 0) {
								//OptionFamily specified but does not exist
								} else {
									//We have one.
									foreach ( part in parts.Where(pa => pa.OptType // ERROR: Unknown binary operator Like
 && (string.IsNullOrEmpty(ProdVal.OptionFamily) || pa.hasAttribute("optFam", ProdVal.OptionFamily, true)))) {
										if (part.Attributes.ContainsKey(ProdVal.CheckAttribute)) {
											//Assume then that the translation of this attribute is an opttype and the number is the required (minimum in this case) value for it
											foreach ( attr in part.Attributes(ProdVal.CheckAttribute)) {
												object targetOptType = attr.Translation.text(English);
												object targetValue = attr.NumericValue;
												if (dicslots.Where(st => st.Key.MajorCode == targetOptType).Sum(ds => ds.Value.TotalCapacity) < targetValue) {
													this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, {
														targetOptType,
														targetValue.ToString
													}, null, "",
													null, ProdVal));
												}
											}
										}
									}
								}
							}

					}

				}
			}

			//Ok, lets find any xText's - recurse to find any option xTexts

			//Change to this "Important Information" needs to be consolidated...
			//'Me.Msgs.AddRange(Me.getXtext(Me.Path))

			object impinfo = this.getXtext(this.Path);

			object ambinfo = impinfo.Where(ai => ai.severity == EnumValidationSeverity.amberalert);
			object blueinfo = impinfo.Where(ai => ai.severity == EnumValidationSeverity.BlueInfo);

			if (blueinfo.Count > 0) {
				blueinfo.First.variables = { string.Join("<br><br>", blueinfo.Select(ii => ii.message.text(this.quote.BuyerAccount.Language))) };
				blueinfo.First.message = iq.AddTranslation("%1", English, "", 0, null, 0, false);
				this.Msgs.Add(blueinfo.First);

			}

			this.Msgs.AddRange(ambinfo);


			//    For Each a In iq.ProductValidationsAssignment(systype).Where(Function(f) f.ValidationType = enumValidationType.Quantity AndAlso Not parts.Contains(f.RequiredOptType))
			// Dim pth As String = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", a.RequiredOptType)
			// Me.Msgs.Add(New ClsValidationMessage(a.Severity, a.Message, pth, 0, 0, Split("")))
			// Next

			//For Each a In iq.ProductValidationsAssignment(systype).Where(Function(f) f.ValidationType = enumValidationType.Slot)
			// Dim dics As System.Collections.Generic.KeyValuePair(Of clsSlotType, clsSlotSummary)? = dicslots.ToList().Where(Function(ds) ds.Key.MajorCode = a.RequiredOptType).Select(Function(ff) ff).FirstOrDefault()
			// If dics Is Nothing Then Continue For
			// Dim pth As String = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", a.RequiredOptType)
			// If dics.Value.Value.Given < dics.Value.Value.taken Then Me.Msgs.Add(New ClsValidationMessage(a.Severity, a.Message, pth, 0, 0, Split(dics.Value.Key.Translation.text(English) & "," & dics.Value.Value.taken.ToString & "," & dics.Value.Value.Given.ToString, ",")))
			// Next
		}
		//    Dim pth$
		//    If systype.Contains("notebook") Then

		//    ElseIf systype.Contains("workstation") Then

		//    ElseIf systype.Contains("server") Then
		//        If Not Me.hasComponentOfType("KVM", False) Then
		//            pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "KVM")
		//            Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("We recommend you purchase a KVM adapter with this server", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
		//        End If
		//        If Not Me.hasComponentOfType("HDD", False) Then
		//            'Me.Msg &= " You should install at least one Hard Disk"
		//            pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "HDD")
		//            Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.amberalert, iq.AddTranslation("You should install at least one Hard Disk", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
		//        End If
		//        If Not Me.hasComponentOfType("CPU", False) Then
		//            'Me.Msg &= " You should install at least one Hard Disk"
		//            pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "CPU")
		//            Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.amberalert, iq.AddTranslation("You should install at least one CPU", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
		//        End If
		//        If Not Me.hasComponentOfType("ILO", False) Then
		//            'Me.Msg &= " You should install at least one Hard Disk"
		//            pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "ILO")
		//            Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("HP recomends ILO License", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
		//        End If
		//        For Each st In dicslots.Keys

		//            If st.MajorCode = "CPU" Then
		//                With dicslots(st)
		//                    If .taken < .Given Then
		//                        pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "CPU")
		//                        Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("CPU slots " & .taken & " used  " & (.Given - .taken) & " available", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
		//                    End If
		//                End With
		//            End If

		//            If st.MajorCode = "MEM" Then
		//                With dicslots(st)
		//                    If .taken < .Given Then
		//                        pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "MEM")
		//                        Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("Memory Optimisation " & .taken & " used  " & (.Given - .taken) & " available", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
		//                    End If
		//                End With
		//            End If

		//            Debug.WriteLine(st.MajorCode)
		//        Next

		//    ElseIf systype.Contains("storage") Then

		//    ElseIf systype.Contains("network") Then

		//    ElseIf systype.Contains("desktops") Then

		//    ElseIf systype.Contains("laptops") Then
		//        For Each st In dicslots.Keys
		//            If st.MajorCode = "MEM" Then
		//                With dicslots(st)
		//                    If .taken < .Given Then
		//                        pth$ = Me.Branch.findProductPathByAttributeValueRecursive(Me.Path, "optType", "MEM")
		//                        Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.BlueInfo, iq.AddTranslation("Memory Optimisation " & .taken & " used  " & (.Given - .taken) & " available", English, "VM", 0, Nothing, 0, False), pth, 0, 0, Split("")))
		//                    End If
		//                End With
		//            End If
		//        Next

		//    Else

		//        Beep()

		//    End If
		//End If

	}

	private List<ClsValidationMessage> getXtext(string path)
	{
		getXtext = new List<ClsValidationMessage>();
		getXtext.AddRange(Branch.Product.getXtext(path, this.quote.AcknowledgedValidations));
		getXtext.AddRange(this.Children.Where(ci => !ci.IsPreInstalled).SelectMany(qi => qi.getXtext(qi.Path)));
	}


	public bool hasComponentOfType(type, bool crossSystems)
	{

		//crossystems is not yet implemented
		hasComponentOfType = null;

		if (this.Branch.Product.i_Attributes_Code.ContainsKey("optType")) {
			if (this.Branch.Product.i_Attributes_Code("optType")(0).Translation.text(English) == type) {
				hasComponentOfType = true;
				return;
			}
		}

		foreach ( c in this.Children) {
			if (c.hasComponentOfType(type, crossSystems)){hasComponentOfType = true;return;
}
		}

	}
	public class clsSubComponent
	{
		public string OptType;
		public string Path;

		public Dictionary<string, List<clsProductAttribute>> Attributes;
		public bool hasAttribute(string code, object value, bool Wildcard)
		{
			decimal dec;
			return Attributes.ContainsKey(code) && Attributes(code).Where(attr => decimal.TryParse(value.ToString, dec) && attr.NumericValue == dec || (attr.Translation != null && attr.Translation.text(English) // ERROR: Unknown binary operator Like
)).Count > 0;
		}
	}
	public List<clsSubComponent> listComponents(bool crossSystems)
	{
		listComponents = new List<clsSubComponent>();
		//crossystems is not yet implemented
		if (this.Branch.Product.i_Attributes_Code.ContainsKey("optType")) {
			listComponents.Add(new clsSubComponent {
				Path = this.Path,
				OptType = this.Branch.Product.i_Attributes_Code("optType")(0).Translation.text(English),
				Attributes = this.Branch.Product.i_Attributes_Code
			});
		}

		foreach ( c in this.Children) {
			if (c.validate)
				listComponents.AddRange(c.listComponents(crossSystems));
		}

	}

	/// <summary>Counts products under each OPG, by product type - Recursively populates a dictionary of flexOPG>ProductType>clsMinMaxTotalUsed  - for this quoteItem(and it's descendants)</summary>
	///<remarks>ClsMinMaxTotalUsed carries the 4 variables used later by SetFlexRebate()</remarks>
	int sysFlexID = 0;

	internal void QualifyFlex(ref Dictionary<clsProduct, Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>> qfdic, clsRegion region, bool flexQualifiedSystem, int sysFlexID)
	{
		//The RootItem has no branch
		if (!this.Branch == null) {

			//Or qfdic.Count > 0 Then -- ML nobbled this as it says "qualifying system" but wasnt checking it was a system??
			if ((Branch.Product.isSystem(this.Path) & Branch.Product.OPGflexLines.Count > 0)) {
				object t = (from l in this.Branch.Product.OPGflexLines.Valueswhere l.FlexOPG.AppliesToRegion(region) & l.FlexOPG.OPGSysType == this.Branch.Product.ProductType.Code).ToList();
				if (t.Count > 0) {
					clsTranslation tlqualifies = iq.AddTranslation("Qualifying System", English, "VM", 0, null, 0, false);
					this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, null, tlqualifies, "", 0, 0, Split(",", ",")));
					flexQualifiedSystem = true;
					sysFlexID = t.First.FlexOPG.ID;
				}
			}

			object op = (from l in this.Branch.Product.OPGflexLines.Valueswhere l.FlexOPG.AppliesToRegion(region)).ToList();
			if (op.Count > 1) {
				op = (from l in this.Branch.Product.OPGflexLines.Valueswhere l.FlexOPG.AppliesToRegion(region) & l.FlexOPG.ID == sysFlexID).ToList();
			}



			foreach ( flexLine in op) {
				//If Me.IsPreInstalled = False The
				clsProductType ProductType = this.Branch.Product.ProductType;
				if (flexLine.isCurrent & flexLine.FlexOPG.isCurrent) {
					if (flexLine.FlexOPG.AppliesToRegion(region)) {
						Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>> qflDic;
						clsProduct sysProduct = new clsProduct();
						if (this.Branch.Product.isSystem(this.Path)) {
							sysProduct = this.Branch.Product;

						} else if (this.Parent != null && this.Parent.Branch != null && this.Parent.Branch.Product != null && this.Parent.Branch.Product.isSystem(this.Path)) {
							sysProduct = this.Parent.Branch.Product;
						}
						if (flexQualifiedSystem) {
							if (!qfdic.ContainsKey(sysProduct)) {
								qflDic = new Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>();
								qfdic.Add(sysProduct, qflDic);
							}
							qflDic = qfdic(sysProduct);
							if (!qflDic.ContainsKey(flexLine.FlexOPG)) {
								Dictionary<clsProductType, clsMinMaxTotalUsed> ptdic = new Dictionary<clsProductType, clsMinMaxTotalUsed>();
								qflDic.Add(flexLine.FlexOPG, ptdic);
							}


							if (this.IsPreInstalled == false) {

								if (!qflDic(flexLine.FlexOPG).ContainsKey(ProductType) & flexQualifiedSystem) {
									clsFlexRule rule = flexLine.FlexOPG.getRule(ProductType);
									if (rule == null) {
										//No requried quantities on this Flexlines,Products,productType 
										qflDic(flexLine.FlexOPG).Add(ProductType, new clsMinMaxTotalUsed(1, 9999, 0, 0, true));

									} else {
										qflDic(flexLine.FlexOPG).Add(ProductType, new clsMinMaxTotalUsed(rule.min, rule.max, 0, 0, rule.optionalRule));

									}
										//<This is important !
									 // ERROR: Not supported in C#: WithStatement

								}

							}
						}
					}
				}
				//End If
			}
		}

		foreach ( child in this.Children) {
			child.QualifyFlex(qfdic, region, flexQualifiedSystem, sysFlexID);
		}

	}

	internal void FlexCalculations(ref Dictionary<clsProduct, Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>> qfdic, clsRegion region, int sysFlexID, ref bool validationSuccess, Dictionary<clsProductType, ClsValidationMessage> qualifyingProductTypes)
	{
		bool flexQualifiedSystem;
		Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>> qflDic;
		//Rootitem has no branch
		if (!this.Branch == null) {
			//if the branch is a system then check if it has OPG flexline for the region 
			if ((Branch.Product.isSystem(this.Path) & Branch.Product.OPGflexLines.Count > 0)) {
				object t = (from l in this.Branch.Product.OPGflexLines.Valueswhere l.FlexOPG.AppliesToRegion(region) & l.FlexOPG.OPGSysType == this.Branch.Product.ProductType.Code).ToList();
				if (t.Count > 0) {
					// The system qualifies for a flex 
					clsTranslation tlqualifies = iq.AddTranslation("Qualifying System", English, "VM", 0, null, 0, false);
					this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, null, tlqualifies, "", 0, 0, Split(",", ",")));
					flexQualifiedSystem = true;
					sysFlexID = t.First.FlexOPG.ID;
					clsFlexLine sysFlexline = t.First;
					clsProduct sysProduct = this.Branch.Product;


					if (!qfdic.ContainsKey(sysProduct)) {
						qflDic = new Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>();
						qfdic.Add(sysProduct, qflDic);
					}

					qflDic = qfdic(sysProduct);
					if (!qflDic.ContainsKey(sysFlexline.FlexOPG)) {
						Dictionary<clsProductType, clsMinMaxTotalUsed> ptdic = new Dictionary<clsProductType, clsMinMaxTotalUsed>();
						qflDic.Add(sysFlexline.FlexOPG, ptdic);
					}

					// Generate the system validation rules 

					foreach ( flexRules in sysFlexline.FlexOPG.Rules.Values) {
						if (!qflDic(sysFlexline.FlexOPG).ContainsKey(flexRules.ProductType)) {
							qflDic(sysFlexline.FlexOPG).Add(flexRules.ProductType, new clsMinMaxTotalUsed(flexRules.min, flexRules.max, 0, 0, flexRules.optionalRule));
						}
					}
					// Now check which item is missing from the quote
					string strflexLineProductTypes = "";
					getAllproductTypes(strflexLineProductTypes, sysFlexline.FlexOPG.ID);
					List<clsProductType> includedproductTypes = new List<clsProductType>();
					foreach ( productTypeRule in qflDic(sysFlexline.FlexOPG)) {
						clsProductType productType = productTypeRule.Key;
						clsMinMaxTotalUsed systemRule = productTypeRule.Value;
						if (!systemRule.optionalRule) {
							if (!strflexLineProductTypes.Contains(productType.Code)) {
								if (productTypeRule.Value.Min > 0) {
									string[] v = new string[0];
									v(0) = productType.Translation.text(English);
									v(1) = (string)productTypeRule.Value.Min;
									clsTranslation text;
									text = iq.AddTranslation("No Qualifying %1 (Min required %2)", English, "VM", 0, null, 0, false);
									this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesntQualify, text, text, "", 0, 0, v));
									validationSuccess = false;
								}
							} else {
								includedproductTypes.Add(productType);

								// If we qualify on the warranty, explicitly display a summary message
								if (productType.Code == "wty") {
									string[] v = new string[0];
									v(0) = systemRule.Min.ToString();
									v(1) = productType.Translation.text(English);
									clsTranslation text;
									text = iq.AddTranslation("%1 Qualifying %2", English, "VM", 0, null, 0, false);
									this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, text, text, "", 0, 0, v));
								}

							}
						}
					}


				} else {
					// No system flex line 

				}
				//
			}

			//get flex lines for a region 

			object op = (from l in this.Branch.Product.OPGflexLines.Valueswhere l.FlexOPG.ID == sysFlexID).ToList();

			if (op.Count == 1) {
				//There should be only one flexline per country per system type 
				clsFlexLine flexLine = op.First;

				if (flexLine.isCurrent & flexLine.FlexOPG.isCurrent) {
					if (this.IsPreInstalled == false) {
						//Need to get the system product from dictionary
						clsProduct sysProduct = new clsProduct();
						if (this.Branch.Product.isSystem(this.Path)) {
							sysProduct = this.Branch.Product;
						} else if (this.Parent != null && this.Parent.Branch != null && this.Parent.Branch.Product != null && this.Parent.Branch.Product.isSystem(this.Parent.Path)) {
							sysProduct = this.Parent.Branch.Product;
						}

						if ((!sysProduct == null) && qfdic.ContainsKey(sysProduct)) {
							qflDic = qfdic(sysProduct);
							clsProductType currentBranchProductType = this.Branch.Product.ProductType;
							if (qflDic.ContainsKey(flexLine.FlexOPG)) {
								//This opg dictionary should already be created with system opg . 
								// if the opg is different that means we shouln't count it for our flex calculations
								if (!qflDic(flexLine.FlexOPG).ContainsKey(currentBranchProductType)) {
									clsFlexRule rule = flexLine.FlexOPG.getRule(currentBranchProductType);
									if (rule == null) {
										//No requried quantities on this Flexlines,Products,productType 
										qflDic(flexLine.FlexOPG).Add(currentBranchProductType, new clsMinMaxTotalUsed(1, 9999, 0, 0, true));
									} else {
										qflDic(flexLine.FlexOPG).Add(currentBranchProductType, new clsMinMaxTotalUsed(rule.min, rule.max, 0, 0, rule.optionalRule));
									}

								}
									//<This is important !
									//Me.Msgs.Add(validationMessage)
								 // ERROR: Not supported in C#: WithStatement

							}
						}
					}
				}
			}
		}
		foreach ( child in this.Children) {
			child.FlexCalculations(qfdic, region, sysFlexID, validationSuccess, qualifyingProductTypes);
		}

	}

	/// <summary>Counts qualifying options under each system in the basket, by OPG - Recursively populates a dictionary of System>OPGRef>Qty  </summary>

	public void QualifyAvalanche(ref clsProduct system, ref Dictionary<clsProduct, Dictionary<ClsAvalancheOPG, int>> qdic, clsRegion region)
	{
		//The RootItem has no branch
		if (!this.Branch == null) {
			if (this.Branch.Product.isSystem(this.Path)) {
				system = this.Branch.Product;
				if (!qdic.ContainsKey(system)) {
					qdic.Add(system, new Dictionary<ClsAvalancheOPG, int>());
				}
			} else {
				if (this.IsPreInstalled == false) {
					clsProduct opt = this.Branch.Product;
					//need to check if we're inside a system yet (as options can be orphans)
					if (system != null) {
						foreach ( avOPG in system.AvalancheOPGs.Values) {
							string prodRef;
							clsProductAttribute pra;
							//only check options with a prodref
							if (opt.i_Attributes_Code.ContainsKey("ProdRef")) {
								pra = opt.i_Attributes_Code("ProdRef")(0);
								prodRef = pra.Translation.text(English);
								if (!qdic(system).ContainsKey(avOPG)) {
									qdic(system).Add(avOPG, 0);
								}
								//returns the avalancheOptions (containing % rebate information)  is for the sepcified prodref,qty..etc (which are all optional)
								if (avOPG.getAvalancheOptions(prodRef, 0, Now, region).Count > 0) {
									qdic(system)(avOPG) += this.DerivedQuantity;
								}
							}
						}
					}
				}

			}
		}

		foreach ( child in this.Children) {
			child.QualifyAvalanche(system, qdic, region);
		}

	}

	/// <summary>recursively sets the rebate (and OPG) on quoteitems holding qualifying options - according to the avalanche offers available on the system in which they reside</summary>
	///<remarks>qDic contains a the number of qualifying options,  by opg, by systems (in the basket) - and was built by QualifyAvalanche()</remarks>

	public void SetAvalancheRebate(clsProduct system, Dictionary<clsProduct, Dictionary<ClsAvalancheOPG, int>> qDic)
	{
		string prodRef;
		//the quote's root item has no branch (and is the only quote item like it)
		if (!this.Branch == null) {
			if (this.Branch.Product.isSystem(this.Path)) {
				this.rebate = 0;
				system = this.Branch.Product;
			} else {
				//i'm an option
				if (!this.IsPreInstalled) {
					this.rebate = 0;
					if (this.Branch.Product.i_Attributes_Code.ContainsKey("ProdRef")) {
						prodRef = this.Branch.Product.i_Attributes_Code("ProdRef")(0).Translation.text(English);

						this.OPG = new nullableString();
						//we may not have 'hit' a system yet (options can be orhpans!)
						if (system != null) {
							//these are the offers
							foreach ( Av in system.AvalancheOPGs.Values) {
								List<clsAvalancheOption> opt = Av.getAvalancheOptions(prodRef, qDic(system)(Av), Now, this.quote.BuyerAccount.SellerChannel.Region);
								if (opt.Count > 0) {
									clsPrice listprice = this.Branch.Product.ListPrice(this.quote.BuyerAccount);

									if (listprice == null) {
										this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("No list price available - to calculate Avalanche Rebate", English, "VM", 0, null, 0, false), iq.AddTranslation("%1 - No list price", English, "VM", 0, null, 0, false), "", 0, 0, Split("")));
									} else if (listprice.Price.value == 0) {
										this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("list price was 0 unable - to calculate Avalanche Rebate", English, "VM", 0, null, 0, false), iq.AddTranslation("%1 - Zero list price", English, "VM", 0, null, 0, false), "", 0, 0, Split("")));
									} else {
										this.rebate = (decimal)opt.First.LPDiscountPercent / 100 * listprice.Price.value;
										if (this.rebate == 0) {
											this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("Avalanche Rebate was 0", English, "VM", 0, null, 0, false), iq.AddTranslation("%1 - Zero Avalanche rebate", English, "VM", 0, null, 0, false), "", 0, 0, Split("")));
										} else {
											this.OPG = new nullableString(Av.OPGref);
										}
									}

								} else {
								}
							}
						}
					}
				}
			}
		}

		//and recurse... for all children
		foreach ( child in this.Children) {
			child.SetAvalancheRebate(system, qDic);
		}

	}



	/// <summary>recursively sets the rebate (and OPG) on quoteItems holding qualifying products - according to the flexOPG offers available</summary>

	internal void SetFlexRebate(Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>> qfdic, bool rulesQualified, int systemopgid, bool systemTotalQualified, ref decimal totalrebate)
	{
		//qfDic contains the number of total number of products in the basket,  by opg, by ProductType and was built by QualifyFlex() 
		//each instance of clsMinMaxTotalUsed - tells us the Total in the baseket, min required ,max, rebatable of each product type under each OPG

		clsProduct product;

		clsRegion region = this.quote.BuyerAccount.BuyerChannel.Region;
		//the quote's root item has no branch (and is the only quote item like it)
		if (!this.Branch == null) {

			if (!this.IsPreInstalled) {
				this.OPG = new nullableString();
				product = this.Branch.Product;
				if (product.isSystem(this.Path) && product.OPGflexLines.Count > 0) {
					rulesQualified = true;
					//Now add all the rules associated with the system                   
					if (product.OPGflexLines.Values.Count > 0) {
						object t = (from l in product.OPGflexLines.Valueswhere l.FlexOPG.AppliesToRegion(region) & l.FlexOPG.OPGSysType == product.ProductType.Code).ToList();
						clsFlexLine sysFlexline = product.OPGflexLines.Values.First;
						if (t.Count > 0) {
							sysFlexline = t.First;
						}
						systemopgid = sysFlexline.FlexOPG.ID;

					}

				}


				if (systemTotalQualified) {
					object op = (from l in this.Branch.Product.OPGflexLines.Valueswhere l.FlexOPG.AppliesToRegion(this.quote.BuyerAccount.BuyerChannel.Region)).ToList();
					if (op.Count > 1) {
						op = (from l in this.Branch.Product.OPGflexLines.Valueswhere l.FlexOPG.AppliesToRegion(this.quote.BuyerAccount.BuyerChannel.Region) & l.FlexOPG.ID == systemopgid).ToList();
					}


					foreach ( flexLine in op) {
						// If Not (flexLine.Product.isSystem) Then
						//If qfdic.ContainsKey(flexLine.FlexOPG) Then 'we may have removed the OPG becuase we didnt have enough options


						if (flexLine.FlexOPG.AppliesToRegion(this.quote.BuyerAccount.BuyerChannel.Region)) {
							if (flexLine.isCurrent & flexLine.FlexOPG.isCurrent) {
								//does the basket contain a product of this flexlines,products type
								clsMinMaxTotalUsed q;
								if (systemopgid == flexLine.FlexOPG.ID) {
									if (qfdic.ContainsKey(flexLine.FlexOPG) && qfdic(flexLine.FlexOPG).ContainsKey(flexLine.Product.ProductType)) {
										q = qfdic(flexLine.FlexOPG)(flexLine.Product.ProductType);
										clsFlexRule r = null;
										r = flexLine.FlexOPG.getRule(flexLine.Product.ProductType);
										if (r != null) {
											// Make sure that the values are from rules if it exists
											q.Min = r.min;
											q.Max = r.max;
										}
									} else {
										//we don't have a product of this flexlines product type  in the basket yet
										clsFlexRule r = null;
										r = flexLine.FlexOPG.getRule(flexLine.Product.ProductType);
										if (r == null) {
											q = new clsMinMaxTotalUsed(1, 9999, 0, 0, true);
										} else {
											q = new clsMinMaxTotalUsed(r.min, r.max, 0, 0, r.optionalRule);
										}
									}

									if (q.Total >= q.Min) {
										// If Me.OPG.value IsNot DBNull.Value Then Stop 'TODO - report an error that a products is qualifying for more than One opg
										this.OPG = new nullableString(flexLine.FlexOPG.OPGRef);
										int remainingQuota = q.Max - q.Used;
										//how many more units of this product can attract the rebate

										int dq = this.DerivedQuantity;
										if (dq < remainingQuota) {
											this.rebate = dq * flexLine.rebate;
										} else {
											this.rebate = remainingQuota * flexLine.rebate;
										}
										if (!rulesQualified) {
											this.rebate = 0;
										}
										if (this.rebate > 0 & !this.Parent == null) {
											Update(false);
											totalrebate += this.rebate;
										}

									} else {
										this.rebate = 0;
										//IMPORTANT (otherwise rebates stay on the item) when it 'unqualifies'
									}
								}

							}
						}
						//  End If
					}
				}
			}
			//new
		}

		//and recurse... for all children
		foreach ( child in this.Children) {
			child.SetFlexRebate(qfdic, rulesQualified, systemopgid, systemTotalQualified, totalrebate);
		}

	}



	public void Flex(int qty, bool absolute)
	{
		//branch As clsBranch, ByVal path As String, ByVal quote As clsquote,
		//if absolute is true - it sets the quantity to (otherwise changes it by qty)

		//The quote contained an item of this product - not preinstalled (posisbly another instance of a product that was originally preinstalled)
		if (absolute) {
			//Dim preInstalledOfThis As clsquoteItem = quote.RootItem.FindRecursive(path$, True) 'dangerous ?
			//Dim subtract As Integer
			//If preInstalledOfThis.IsPreInstalled Then subtract = preInstalledOfThis.Quantity Else subtract = 0
			if (this.IsPreInstalled) {
				Beep();
			}
			this.Quantity = qty;
			//- subtract
		} else {
			object quan = this.Branch.Quantities.Where(q => this.quote.BuyerAccount.BuyerChannel.Region.Encompasses(q.Value.Region)).FirstOrDefault;
			if (quan.Value != null && quan.Value.MinIncrement != 0) {
				this.Quantity += quan.Value.MinIncrement;
			} else {
				this.Quantity += qty;
			}

			if (this.Quantity < 0)
				this.Quantity = 0;
		}

		//Remove this item if we just flexed/set its quantity to zero
		if (this.Quantity < 0) {
			Beep();
		}

		if (this.Quantity == 0) {
			if (object.ReferenceEquals(this, this.quote.RootItem)) {
				Beep();
			} else {
				this.Parent.Children.Remove(this);
			}
		}

	}

	private PlaceHolder FlexButtons(bool AllowFlexUp)
	{

		PlaceHolder ph = new PlaceHolder();

		//Javascript function declaration function flex(branchID, path, qty){

		//Flex Up button
		if (this.FlexButtonState != EnumHideButton.Up & this.FlexButtonState != EnumHideButton.Both) {
			if (AllowFlexUp) {
				ph.Controls.Add(this.FlexButton("quoteTreeFlexUp", +1, Xlt("Add one", quote.BuyerAccount.Language), "../images/navigation/plus.png"));
			}
		}

		//Flex down button
		if (this.FlexButtonState != EnumHideButton.Down & this.FlexButtonState != EnumHideButton.Both) {
			ph.Controls.Add(this.FlexButton("quoteTreeFlexDown", -1, Xlt("Remove one", quote.BuyerAccount.Language), "../images/navigation/minus.png"));
		}

		//Remove from/add back to Validation
		if (this.IsPreInstalled) {
			if (this.validate) {
				ph.Controls.Add(this.FlexButton("quoteTreeFlexDown", -1, Xlt("Remove from validation", quote.BuyerAccount.Language), "../images/navigation/cross.png"));
			} else {
				ph.Controls.Add(this.FlexButton("quoteTreeFlexUp", +1, Xlt("Include in validation", quote.BuyerAccount.Language), "../images/navigation/tick.png"));
			}
		}

		return ph;

	}

	private Image FlexButton(string cssClass, int qty, string toolTip, string imageURL)
	{

		//returns an individual flex button (+ or -) - or a a 'remvove from/add to validation' button (on preinstalled options)
		FlexButton = new Image();

		FlexButton.ImageUrl = imageURL;
		FlexButton.ToolTip = toolTip;
		FlexButton.CssClass = cssClass + " quoteTreeButton";

		if (!this.quote.Locked) {
			FlexButton.Attributes("onclick") = "burstBubble(event);flex('" + this.Path + "'," + (string)qty + "," + this.ID + "," + this.SKUVariant.ID + ");";
			FlexButton.ToolTip = toolTip;
		} else {
			string text = Xlt(PleaseCreateNewVersionToChangeQuote, quote.BuyerAccount.Language);
			FlexButton.Attributes.Add("onmousedown", string.Format("burstBubble(event); setupanddisplaymsg('{0}','{1}');return(false);", text, this.Path));



		}

	}
	public bool Compatible(path)
	{

		//checks wether the product with the path - path$ - is compatible with (an option for) this item

		if (path == this.Path)
			return false;
		//explicitly stop things being compatible with themselves ! (otherwise systems add as a nested cascase)

		if (Left(path, Len(this.Path)) == this.Path) {
			return true;
		} else {
			return false;
		}

	}

	//Private Function NOTSYSDerivedQuantity() As Integer

	//    NOTSYSDerivedQuantity = Me.Quantity
	//    Dim item As clsQuoteItem
	//    item = Me
	//    While Not item.Parent Is Nothing
	//        item = item.Parent
	//        If item.Branch IsNot Nothing Then
	//            If Not item.Branch.Product.isSystem Then
	//                NOTSYSDerivedQuantity *= item.Quantity
	//            End If
	//        End If
	//    End While
	//End Function

	//pair contains the running used total, and the total available to this point
	public void ValidateSlots2(Dictionary<clsSlotType, clsSlotSummary> dicSlots, bool ForGives)
	{

		//Validates this item and all of its children
		//by recursing the tree of the quoteitems 
		//Populates and then manipulates the dicSlots dicitonary as the tree (of quote items) is walked
		//The dictionary therefore contains the slots available (of each type) at each point in the tree - wherever it goes negative - there are isufficient slots and validation fails.

		//am i excluded from validation ? (pre-installed items can be removed from validation in the UI)
		if (this.validate) {
			if (this.IsPreInstalled) {
				this.FlexButtonState = EnumHideButton.Down;
			} else {
				this.FlexButtonState = EnumHideButton.Neither;
			}
			int qs;
			//skip the rootitem - it's just a placeholder for the top level items (typically systems)
			if (!this.Branch == null) {
				List<clsSlot> sif;

				foreach ( slot in this.Branch.slotsInForce(Path)) {
					object pn = this.Branch.Product.DisplayName(English);
					//For watching/debugging
					if (slot.NonStrictType.MajorCode.ToLower() == "wty") {
						object a = 9;
					}
					if (!dicSlots.ContainsKey(slot.NonStrictType))
						dicSlots.Add(slot.NonStrictType, new clsSlotSummary(0, 0, 0, 0));

					int dq = this.DerivedQuantity;

					//GREG OVERRIDE - COUNT AND VALIDATE SLOTS PER SERVER - REMOVE TO GO BACK TO MULTIPLIED.
					if (this.Branch.Product.isSystem(this.Path)) {
						dq = 1;
					} else {
						dq = this.Quantity;
					}
					//</gregness>

					qs = slot.numSlots * dq;
					if (qs > 0) {
						if (ForGives) {
							dicSlots(slot.NonStrictType).Given += qs;

						}
					} else {
						if (!ForGives) {
							//Find if there is a fallback WITH SPACE
							int toAllocate = -qs;
							if (slot.NonStrictType.Fallback != null && slot.NonStrictType.Fallback.Count > 0) {
								foreach ( fbs in slot.NonStrictType.Fallback.Values) {
									if (dicSlots.ContainsKey(fbs)) {
										if (dicSlots(fbs).Given > dicSlots(fbs).taken) {
											object theseslots = toAllocate;
											if (toAllocate > (dicSlots(fbs).Given - dicSlots(fbs).taken))
												theseslots = (dicSlots(fbs).Given - dicSlots(fbs).taken);
											dicSlots(fbs).taken += theseslots;
											dicSlots(slot.NonStrictType).taken += theseslots;
											//These two lines are simply in to get the count correct in the error message...
											dicSlots(slot.NonStrictType).Given += theseslots;
											if (this.IsPreInstalled)
												dicSlots(fbs).PreInstalledTaken += theseslots;
											toAllocate = toAllocate - theseslots;
											if (toAllocate == 0)
												break; // TODO: might not be correct. Was : Exit For
										}
									}
								}
							}
							if (toAllocate > 0) {
								dicSlots(slot.NonStrictType).taken += toAllocate;
								//takes slots are negative - so we subtract (to add)
								if (this.IsPreInstalled)
									dicSlots(slot.NonStrictType).PreInstalledTaken += toAllocate;
							}
						}
					}

					//ML - added this for the n-1 PSU functionality, after speaking with Paul aparently this doesnt need to happen... leaving it here incase it suddenly does again.
					//If slot.Branch.Product.ProductType.Code.ToLower = "psu" AndAlso slot.NonStrictType.MajorCode.ToLower = "psu" AndAlso qs < 0 Then
					//    'n-1 for power, do it on the slots so that it effects everywhere, validation ,specs, etc
					//    Dim noPSUs = dicSlots.Where(Function(ds) ds.Key.MajorCode.ToLower = "psu" AndAlso Branch.ID = slot.Branch.ID).Sum(Function(ds) ds.Value.taken)
					//    Dim noWatts = Me.Branch.slotsInForce(Path).Where(Function(ds) ds.Type.MajorCode.ToLower = "pwr").Max(Function(ds) ds.numSlots)
					//    Dim wattSlot = dicSlots.Where(Function(ds) ds.Key.MajorCode.ToLower = "pwr").FirstOrDefault
					//    If wattSlot.Value IsNot Nothing Then wattSlot.Value.Given = If(noPSUs > 1, noWatts * (noPSUs - 1), noPSUs)
					//End If

					//this option TAKES slots'
					if (slot.numSlots < 0 && !ForGives) {
						if (this.Branch.Product.i_Attributes_Code.ContainsKey("capacity") && {
							"MEM",
							"HDD",
							"CPU",
							"PSU"

						}.Contains(slot.NonStrictType.MajorCode.ToUpper())) {
							//options like HDD's or MEM have a Capacity ...
							// (say 100Mb... which has nothing really to do with slots - but we want to sum the total capacities (for storage, memory etc)
							float capacity = this.Branch.Product.i_Attributes_Code("capacity")(0).NumericValue;
							dicSlots(slot.NonStrictType).TotalCapacity += this.Quantity * capacity;
							//dicSlots(slot.NonStrictType).TotalCapacity += dq * capacity
							if (slot.NonStrictType.MajorCode.ToUpper() == "CPU")
								dicSlots(slot.NonStrictType).TotalCapacity = capacity;
							if (slot.NonStrictType.MajorCode.ToUpper() == "PSU")
								dicSlots(slot.NonStrictType).TotalRedundantCapacity = capacity * (dicSlots(slot.NonStrictType).taken - 1);
							//PSU's are n+1 and always have to be the same power...
							if (slot.NonStrictType.MajorCode.ToUpper() == "PSU")
								dicSlots(slot.NonStrictType).TotalCapacity = capacity;

							//OK this is really ugly - we need to know the capacity Unit Gb/MB/Watt etc
							//options take slots - and have the (somehwat legacy) capacity
							dicSlots(slot.NonStrictType).CapacityUnit = this.Branch.Product.i_Attributes_Code("capacity")(0).Unit;
							if (slot.NonStrictType.MinorCode == "W")
								dicSlots(slot.NonStrictType).CapacityUnit = iq.i_unit_code("W");
						}
					}
				}

			}

			//These are the child quote items - NOT the children of the branch
			foreach ( item in this.Children) {
				object nm = item.Branch.Product.DisplayName(English);
				item.ValidateSlots2(dicSlots, ForGives);
				//Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
			}

		}
		//move this End If up (to above the for each) if you want to still validate sub options even though their parent is excluded from validation

	}



	//pair contains the running used total, and the total available to this point
	public void OldValidateSlots(ref Dictionary<clsSlotType, clsSlotSummary> dicSlots)
	{

		//Validates this item and all of its children
		//by recursing the tree of the quoteitems 
		//Populates and then manipulates the dicSlots dicitonary as the tree (of quote items) is walked
		//The dictionary therefore contains the slots available (of each type) at each point in the tree - wherever it goes negative - there are isufficient slots and validation fails.

		//adding a processor might enable (give) 9 UDIMM *OR* 6 RDIMM ports
		//We need these to be multiplied by the number of processors
		//because processors are encountered before memory - the slots are 'given' before the memory 'takes' them
		//and everything works

		if (this.validate) {
			if (this.IsPreInstalled) {
				this.FlexButtonState = EnumHideButton.Down;
			} else {
				this.FlexButtonState = EnumHideButton.Neither;
			}
			int qs;
			//skip the rootitem - it's just a placeholder for the top level items (typically systems)
			if (!this.Branch == null) {

				//Combined system and chassis slots (union)
				List<clsSlot> combinedslots = new List<clsSlot>();

				//a branch may have slots of more than one type - It might take Watts and Give USB's
				foreach ( slot in this.Branch.slots.Values) {
					//a
					if (slot.path == this.Path) {
						combinedslots.Add(slot);
					}
				}
				combinedslots.AddRange(this.Branch.slots.Values.ToList);
				//system

				//UGLY fix to include the (generally GIVES) slots defined on the chassis branch 
				//If Me.Branch.Product.isSystem Then
				string chassispath = "";
				foreach ( cb in Branch.childBranches.Values) {
					if (cb.Product != null && cb.Product.ProductType.Code == "CHAS") {
						combinedslots.AddRange(cb.slots.Values.ToList);
						//chassis
						chassispath = Path + "." + cb.ID;
						break; // TODO: might not be correct. Was : Exit For
					}
				}

				foreach ( slot in combinedslots) {
					if (slot.path == this.Path | slot.path == chassispath | slot.path == "") {
						// If slot.path <> "" Then Stop
						if (!dicSlots.ContainsKey(slot.Type)) {
							// qs = slot.numSlots * Me.DerivedQuantity
							dicSlots.Add(slot.Type, new clsSlotSummary(0, 0, 0, 0));
						}

						int dq = this.DerivedQuantity;
						qs = slot.numSlots * dq;
						if (qs > 0) {
							dicSlots(slot.Type).Given += qs;
						} else {
							dicSlots(slot.Type).taken -= qs;
							//takes slots are negative - so we subtract (to add)
							if (this.IsPreInstalled)
								dicSlots(slot.Type).PreInstalledTaken -= qs;
						}

						//If qs > 0 Then dicslots(slot.Type).Total += qs

						//this option TAKES slots'
						if (slot.numSlots < 0) {
							if (this.Branch.Product.i_Attributes_Code.ContainsKey("capacity") && {
								"PWR",
								"MEM",
								"HDD"

							}.Contains(slot.Type.MajorCode.ToUpper())) {
								//options like HDD's or MEM have a Capacity ...
								// (say 100Mb... which has nothing really to do with slots - but we want to sum the total capacities (for storage, memory etc)
								float capacity = this.Branch.Product.i_Attributes_Code("capacity")(0).NumericValue;
								if (slot.Type.MajorCode == "CPU")
									dicSlots(slot.Type).TotalCapacity += this.Quantity * capacity;
								else
									dicSlots(slot.Type).TotalCapacity = capacity;

								//OK this is really ugly - we need to know the capacity Unit Gb/MB/Watt etc
								//options take slots - and have the (somehwat legacy) capacity
								dicSlots(slot.Type).CapacityUnit = this.Branch.Product.i_Attributes_Code("capacity")(0).Unit;
								if (slot.Type.MinorCode == "W")
									dicSlots(slot.Type).CapacityUnit = iq.i_unit_code("W");

							}
						}

						//If dicslots(slot.Type).Available < 0 Then
						//    If Me.Parent IsNot Me.quote.RootItem Then  'Don't require sufficient slots on orphaned (root level) options

						//        Dim tl As clsTranslation
						//        tl = iq.AddTranslation("Not enough " & slot.Type.displayName(English) & " slots available", English)
						//        Me.Msgs.Add(New ClsValidationMessage(EnumValidationSeverity.RedCross, tl, "", 0, 0))
						//        Me.FlexButtonState = EnumHideButton.Up  'Stop them attempting to add more
						//        'look through every option on the system to find those that would offer more slots of this type
						//        resolveOverFlows(slot.Type, Math.Abs(dicslots(slot.Type).Available))
						//    End If
						//End If
					}
				}
			}

			foreach ( item in this.Children) {
				//      If Not item.Branch.Product.isSystem Then 'don't cross system boudaries
				object nm = item.Branch.Product.DisplayName(English);
				item.OldValidateSlots(dicSlots);
				//Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
				//   End If
			}
		}
		//move this End If up (to above the for each) if you want to still validate sub options even though their parent is excluded from validation

	}

	private clsQuoteItem SystemItem()
	{

		if (object.ReferenceEquals(this, this.quote.RootItem)) {
			return null;
		} else {
			if (this.Branch.Product.isSystem(this.Path)) {
				return this;
			} else {
				return this.Parent.SystemItem();
			}
		}

		//recursively finds the items parent system (within the tree of quote items)


	}
	/// <summary>
	/// This is for item where the slots number of slots is less than 0 thte slottype amd Nostricttype  of dicSlots create innstances of slotsummary where the taken - the given is equal to the short fall.  
	/// </summary>
	/// <param name="slotType">An instance clsSlotType.  </param>
	/// <param name="shortfall">An integer value representing the shortfall</param>
	/// <param name="buyeraccount">An instance of ClsAccount.</param>
	/// <param name="msg">An instance of ClsValidationMessage.</param>
	/// <param name="errormessages">An intsance of list of type string that represent any errror messages recorded.</param>
	/// <param name="lid">A Uint64 value that represent the logon ID</param>
	/// <param name="dicSlots">An instance of Dictionary of Type of ClsSlotType , clsSlotSummary. </param>
	/// <remarks></remarks>

	public void resolveOverFlows(clsSlotType slotType, int shortfall, clsAccount buyeraccount, ref ClsValidationMessage msg, ref List<string> errormessages, UInt64 lid, Dictionary<clsSlotType, clsSlotSummary> dicSlots)
	{
		//adds validationmessages - to resolve slot overflows

		clsTranslation tl = iq.AddTranslation("Resolve with ", English, "VM", 0, null, 0, false);

		clsBranch branch = null;
		int rq;
		//number of units (of the partnumber) required to resove

		clsQuoteItem systemItem = this.SystemItem;
		if (systemItem == null)
			return;
		Dictionary<string, int> gives = systemItem.Branch.findSlotGivers(Path, slotType, false);
		//the 'false' stops it incuding another system unit as a slot donor
		foreach ( pth in gives.OrderByDescending(g => g.Value).Select(j => j.Key)) {
			branch = iq.Branches((int)Split(pth, ".").Last);
			HashSet<string> foci = new HashSet<string>(iq.sesh(lid, "foci").ToString.Split(",".ToArray));
			if (foci == null)
				foci = new HashSet<string>();
			if (branch.ReasonsForHide(buyeraccount, foci, Path, buyeraccount.SellerChannel.priceConfig, false, errormessages).Count == 0 && !branch.Product.SKU.StartsWith("###")) {
				object remainingtakes = -1;

				foreach ( slot in branch.slots) {
					//important must be slots.numSlots must be negative.
					if (slot.Value.numSlots >= 0)
						continue;
					//Important if clsSlotType of NonStrictType exists.
					if (!dicSlots.ContainsKey(slot.Value.NonStrictType))
						continue;
					clsSlotSummary slotTypeNoneStrict = dicSlots(slot.Value.NonStrictType);
						//Important if these checks are not in place not the incorrect path and the incorrect message is displayed 
					 // ERROR: Not supported in C#: WithStatement


				}
				//Could tell them exactly what they need here?? for now lets just suggest the first thing to get them started.
				rq = Convert.ToInt32(remainingtakes * gives(pth)) > shortfall ? Convert.ToInt32(remainingtakes * gives(pth)) : shortfall;
				 // ERROR: Not supported in C#: WithStatement

				if (remainingtakes > 0)
					break; // TODO: might not be correct. Was : Exit For
				//Remove this when counting more than the best item
			}
		}

	}


	public void ValidateFill(clsAccount agentAccount)
	{
		//checks that enough slots of each type are filled (accoring to the slot.requiredFill property

		//the root QuoteItem has no branch (it's just a placholder for the top level items in the Quote)
		if (this.Branch != null) {
			foreach ( slot in this.Branch.slots.Values) {
				if (LCase(slot.path) == LCase(this.Path) | slot.path == "") {
					if (slot.requiredFill > 0) {
						if (this.CountFilledDescendants(slot.Type) < slot.requiredFill) {
							clsTranslation tl;
							tl = iq.AddTranslation("You must have at least %1", agentAccount.Language, "VM", 0, null, 0, false);
							this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.amberalert, tl, tl, "", 0, 0, Split(slot.requiredFill + " " + slot.Type.Translation.text(agentAccount.Language), ",")));

						}
					}
				}
			}
		}

		foreach ( item in this.Children) {
			item.ValidateFill(agentAccount);
			// (agentAccount)  'Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
		}

	}

	public int CountFilledDescendants(clsSlotType slottype)
	{

		int count;

		foreach ( slot in this.Branch.slots.Values) {
			if (object.ReferenceEquals(slot.Type, slottype)) {
				count += 1;
			}
		}

		foreach ( item in this.Children) {
			count += item.CountFilledDescendants(slottype);
		}

		return (count);

	}

	/// <summary>Returns this Items quantity - multiplied by that of all its ancesetors </summary>
	/// <remarks>Handles the 'nestedness' of quotes - the fact hat you might be buying 2 racks  each containing 3 servers each containing 4 Drives - For the drives, this fuction gives you the 2*3*4 </remarks>
	private int DerivedQuantity()
	{

		DerivedQuantity = this.Quantity;

		clsQuoteItem item;
		item = this;
		while (!item.Parent == null) {
			item = item.Parent;
			DerivedQuantity *= item.Quantity;
		}


	}

	public void validateExclusivity()
	{
		List<clsQuoteItem> incompatible;
		//There is typically one exclude per family - so a few hundred at most
		foreach ( ex in iq.Excludes.Values) {
			foreach ( o in this.Children) {
				// If o.validate Then  'is it removed from validation ! ?
				if (ex.havingAnyOf.Contains(o.Branch)) {
					incompatible = o.siblingsBranchesIn(ex.excludesAllOf);
					if (incompatible.Count > 0) {
						o.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.RedCross, iq.AddTranslation("Incompatbile with %1", English, "VM", 0, null, 0, false), iq.AddTranslation("%1 - Incompatbile", English, "VM", 0, null, 0, false), "", 0, 0, Split(incompatible(0).Branch.DisplayName(English), ",")));
					}
				}
				// End If
			}
		}

	}



	/// <summary>Returns any sibling QuoteItem whos branches are contained in the specified list </summary>
	public List<clsQuoteItem> siblingsBranchesIn(List<clsBranch> L)
	{

		siblingsBranchesIn = new List<clsQuoteItem>();

		foreach ( i in this.Parent.Children) {
			if (!object.ReferenceEquals(i, this)) {
				//is it removed from validation (preinstalled items can be - by minusing them in the basket)
				if (i.validate) {
					if (L.Contains(i.Branch)) {
						siblingsBranchesIn.Add(i);
					}
				}
			}
		}

	}


	public void ValidateIncrements(Dictionary<string, int> dicItemCounts, clsAccount agentaccount, ref List<string> errorMessages)
	{
		//Validates this items quantity (against is Min and Preferred increments) 
		//recurses for all children
		clsLanguage translationLanguage = agentaccount.Language;
		if (translationLanguage == null)
			translationLanguage = English;

		//translationLanguage = English

		if (this.validate) {
			clsRegion sellerRegion = this.quote.BuyerAccount.SellerChannel.Region;

			clsQuantity quantity = null;

			//skip the rootitem
			if (!this.Branch == null) {
				quantity = this.Branch.LocalisedQuantity(sellerRegion, this.Path, errorMessages);


				if (quantity == null) {
					Logit("No quantity limits for " + Branch.Translation.text(translationLanguage));
					return;
				}

				int qty;
				if (dicItemCounts.ContainsKey(this.Path)) {
					qty = dicItemCounts(this.Path);
					//this is the TOTAL quantity of this branch.product in the quote

					if (quantity.PreferredIncrement != 0) {

						if (qty % quantity.PreferredIncrement != 0) {
							clsTranslation tl;
							tl = iq.AddTranslation("Optimum performance is achieved when %1 is installed in multiples of %2 modules %3 selected", English, "VM", 0, null, 0, false);
							this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Upsell, EnumValidationSeverity.Exclamation, tl, iq.AddTranslation(string.Format("{0} Optimisation", quantity.Branch.Product.ProductType.Translation.text(English)), English, "", 0, null, 0, false), this.Path, 1 - (qty % quantity.PreferredIncrement) * quantity.PreferredIncrement, 0, {
								quantity.Branch.Product.ProductType.Translation.text(translationLanguage),
								quantity.PreferredIncrement.ToString(),
								qty.ToString()
							}));
						}
					}

					if (this.Quantity > 0 & this.Quantity < quantity.MinIncrement) {
						this.Quantity = quantity.MinIncrement;
						clsTranslation tl;
						tl = iq.AddTranslation("Quantity adjusted to meet minimum of %1", English, "VM", 0, null, 0, false);
						this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.BlueInfo, tl, tl, "", 0, 0, Split((string)quantity.MinIncrement, ",")));
					}
				}
			}

			foreach ( item in this.Children) {
				item.ValidateIncrements(dicItemCounts, agentaccount, errorMessages);
				//RECURSE
			}
		}

	}



	public void clearMessage()
	{
		//Nice easy one.. clears the (warning) message on this quoute item - and recurses for all children.
		this.Msgs = new List<ClsValidationMessage>();
		foreach ( item in this.Children) {
			item.clearMessage();
		}

	}

	public bool HasProduct(clsProduct Product)
	{

		if (this.Branch != null) {
			if (object.ReferenceEquals(this.Branch.Product, Product)) {
				return true;
			}

		}

		foreach ( item in this.Children) {
			if (item.HasProduct(Product))
				return true;
		}

	}


	public bool HasProductType(clsProductType ProductType)
	{

		if (this.Branch != null) {
			if (object.ReferenceEquals(this.Branch.Product.ProductType, ProductType)) {
				return true;

			}

		}
		foreach ( item in this.Children) {
			if (item.HasProductType(ProductType))
				return true;
		}
	}



	public void countSystems(ref int systems, ref int options)
	{
		if (this.Branch != null) {
			if (this.Branch.Product.isSystem(this.Path)) {
				systems += this.Quantity;
			} else {
				if (!this.IsPreInstalled) {
					options += this.Quantity;
				}
			}
		}

		foreach ( c in this.Children) {
			c.countSystems(systems, options);
		}

	}

	public void CountItems(ref Dictionary<string, int> dicItems)
	{
		//recursively (used from the rootitem)
		//builds a dictionary keyed by branch path - of the counts of every item (so we can validate the total number of items (preinstalled and added) agains their minimum/preferred increments
		//because each path is unique (but the same wether an option is pre-installed or user selected) , the validation will happen per item (sku)

		//Note: dicItems is passed BYREF.. so it will accumulate a count of all items

		//skip the rootitem
		if (!this.Branch == null) {

			int qty = this.Quantity;
			if (!this.validate)
				qty = 0;
			//NOTE: items excluded from validation are not counted !

			if (!dicItems.ContainsKey(this.Path)) {
				dicItems.Add(this.Path, qty);
			} else {
				dicItems(this.Path) += qty;
			}

		}

		foreach ( item in this.Children) {
			item.CountItems(dicItems);
		}

	}

	// As nullablePrice
	public void Totalise(ref NullablePrice runningTotal, ref decimal runningRebate, bool includeMargin)
	{

		//for each item - add myself to the running total - and recurse for all children
		//TotalPrice = New nullablePrice(Me.quote.Currency)

		//the ROOT item has no price
		if (!object.ReferenceEquals(this, this.quote.RootItem)) {
			// Also check if the item is pre installed 
			if (!this.BasePrice.isValid & !this.IsPreInstalled) {
				runningTotal.isValid = false;
				//   Exit Sub
			}
			if (this.BasePrice.isList & !this.IsPreInstalled)
				runningTotal.isList = true;
		}

		float mgf = 1;
		//Margin Factor
		if (this.Margin == 0)
			this.Margin = 1;
		if (includeMargin)
			mgf = this.Margin;


		//NB this uses the * operator overload on clsNullablePrice - which preserves the IsValid/IsList
		// Fix for recalculation bug ignore pre installed items
		if (!this.IsPreInstalled & !object.ReferenceEquals(this, this.quote.RootItem)) {
			runningTotal += this.BasePrice * (float)this.DerivedQuantity * mgf;
		}

		if (!object.ReferenceEquals(this, this.quote.RootItem)) {
			//Chasis are from HP but not List price !
			if (this.Branch.Product.hasSKU) {
				if (object.ReferenceEquals(this.SKUVariant.sellerChannel, HP) && !this.IsPreInstalled)
					runningTotal.isList = true;
			}
		}

		int dq = this.DerivedQuantity;
		if (this.Branch != null) {
			if (this.Branch.Product.isSystem(this.Path))
				runningRebate = 0;
			//NEW - IMPORTANT
		}

		runningRebate += this.rebate;
		//* dq  - NB: Rebates are per line (are already multiplied by the permisssible quantity)
		//  If runningRebate > 0 Then Stop

		foreach ( item in this.Children) {
			item.Totalise(runningTotal, runningRebate, includeMargin);
			//If runningTotal.valid = False Then Exit Sub  - keep going becuase we may still be able to give a valid total rebate

			//If item.Price.valid = False Then TotalPrice.valid = False : Exit Function
			// TotalPrice = New nullablePrice(TotalPrice.NumericValue + item.TotalPrice.NumericValue, Me.quote.BuyerAccount.Currency)
		}


	}

	public clsQuoteItem FindRecursive(int itemID)
	{

		//Recursively locates and returns an item from the quote by ID
		FindRecursive = null;

		if (this.ID == itemID) {
			return this;
		}

		clsQuoteItem result;
		foreach (clsQuoteItem item in this.Children) {
			result = item.FindRecursive(itemID);
			if (!result == null) {
				return result;
			}
		}

	}

	public clsQuoteItem FindRecursive(path, bool includepreinstalled)
	{
		//, IncludePreinstalled As Boolean) As clsquoteItem

		//Checks wether a quote item has the specified path - if so, returns the item
		//works *backwards* through the children for LIFO behaviour http://en.wikipedia.org/wiki/LIFO_(computing)
		FindRecursive = null;

		if (LCase(this.Path) == LCase(path)) {
			//If Me.IsPreInstalled = includepreinstalled Then Return Me - This ISNT the same as the two lines below
			if (this.IsPreInstalled & includepreinstalled)
				return this;
			if (this.IsPreInstalled == false)
				return this;
		}

		clsQuoteItem result;
		//For Each item As clsquoteItem In Me.Children 
		//We want to go backwards to find the last item first (when looking by path... primarliy for decrementing via the product tree - not the quote - so we end up with LIFO behaviour
		for (i = this.Children.Count - 1; i >= 0; i += -1) {
			result = this.Children(i).FindRecursive(path, includepreinstalled);
			if (!result == null) {
				return result;
			}
		}
		//Next

	}

	public string summarise(ref int options)
	{

		//Gets the name of every system, and a count of its options - recursively 

		summarise = "";
		// the quote.rootitem has no branch defined (so we must check)
		if (!this.Branch == null) {
			 // ERROR: Not supported in C#: WithStatement

		}

		object so;
		foreach ( item in this.Children.OrderBy(x => x.order)) {
			so = item.summarise(options) + "(+" + options + " options)" + ",";
			//we must always recurse
			if (item.Branch.Product.isSystem(this.Path))
				summarise += so;
			//but only add the result for systems
		}

	}

	public List<clsQuoteItem> Descendants()
	{

		Descendants = new List<clsQuoteItem>();
		Descendants.Add(this);

		foreach ( c in this.Children.OrderBy(x => x.order)) {
			Descendants.AddRange(c.Descendants);
		}

	}

	/// <summary>
	///returns a list of product/SKUvariant/quantity (so it doesn't matter where they pick an option from in the tree - they will be consolidated) 
	/// </summary>
	/// <param name="Consolidate">Wether to consolidate identical parts (or list them as seperate quantity/partnos)</param>
	/// <param name="IncludePreinstalled"></param>
	/// <param name="Indent"></param>
	/// <returns></returns>
	/// <remarks></remarks>
	public clsFlatList Flattened(bool Consolidate, bool IncludePreinstalled, int Indent, bool quote = false)
	{
		//

		Indent += 1;
		Flattened = new clsFlatList();

		//We don't include preinstalled items on the 'flat' quote (Bill Of Materials) view
		if (!this.IsPreInstalled | IncludePreinstalled) {
			//the root item (placeholder)
			if (!this.Branch == null) {
				Flattened.items.Add(new clsFlatListItem(this, Indent, Consolidate ? this.DerivedQuantity : this.Quantity));
				//NOT quantity
				this.Indent = Indent;
			}
		}

		foreach ( item in this.Children.OrderBy(x => x.order)) {
			//merge
			if (quote) {
				Flattened = MergeItems(Flattened, item.Flattened(Consolidate, IncludePreinstalled, Indent, quote), Consolidate, IncludePreinstalled);
			} else {
				Flattened = MergeItems(Flattened, item.Flattened(Consolidate, IncludePreinstalled, Indent));
			}
		}

	}

	private clsFlatList MergeItems(ref clsFlatList a, clsFlatList b)
	{

		//used to merge the lists of child items (of different parent systems)  together into one big, flat shopping list
		//appends/merges Dictionary b to a - extending a

		clsFlatListItem existing;
		foreach ( i in b.items) {
			existing = a.PSV(i.QuoteItem.Branch.Product, i.QuoteItem.SKUVariant);
			//the flat list didnt have one of these yet so add the product and its quantity Then
			if (existing == null) {
				a.items.Add(i);
			} else {
				existing.Quantity += i.Quantity;
				//add the quantity in b to the exisiting quoute item in a (for this product)
			}
		}
		return a;

	}
	/// <summary>
	/// This is for reports only so we can have the sperate items with in the system break down of report.
	/// </summary>
	/// <param name="a">An instance of clsFlatList</param>
	/// <param name="b"></param>
	/// <param name="consolidate">A boolean value true/ false that represents whether to consolidate the quote.</param>
	/// <param name="includePreinstalled">A boolean value true/ false and represents to inclued preinstalled items.</param>
	/// <returns>An instance of clsFlatList.</returns>
	/// <remarks></remarks>
	private clsFlatList MergeItems(ref clsFlatList a, clsFlatList b, bool consolidate, bool includePreinstalled)
	{

		//used to merge the lists of child items (of different parent systems)  together into one big, flat shopping list
		//appends/merges Dictionary b to a - extending a

		clsFlatListItem existing;
		foreach ( i in b.items) {
			existing = a.PSV(i.QuoteItem.Branch.Product, i.QuoteItem.SKUVariant);
			if ((existing == null)) {
				a.items.Add(i);
			//the flat list didnt have one of these yet so add the product and its quantity Then
			} else if (includePreinstalled & consolidate == false & i.QuoteItem.IsPreInstalled == false) {
				if (a.DoesNoneInstalledExist(i.QuoteItem) == false) {
					a.items.Add(i);
				} else {
					existing.Quantity += i.Quantity;
					//add the quantity in b to the exisiting quoute item in a (for this product)
				}
			} else {
				existing.Quantity += i.Quantity;
			}
		}
		return a;

	}
	public string FlatListTXT(clsAccount buyeraccount)
	{

		//Returns a SKU tab Qty CRLF  delimited 'Bill of materials' type list (for Ingram Copy to ClipBoard)
		clsProduct Product;

		FlatListTXT = "";
		//see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote
		foreach ( lineitem in quote.RootItem.Flattened(true, false, 0).items) {

			Product = lineitem.QuoteItem.Branch.Product;
			FlatListTXT += Product.SKU + vbTab + lineitem.Quantity + vbCrLf;

		}

	}

	public string EmailSummary(bool includePreinstalled, clsAccount BuyerAccount, clsAccount agentAccount, ref List<string> errorMessages, ref NullablePrice runningtotal)
	{

		//Returns this QuoteItem (as simple HTML div with a left margin (indent) ..and recurses for all child items (nesting Divs)

		clsProduct product;

		EmailSummary = "";
		clsLanguage translationLanguage = agentAccount.Language;
		if (translationLanguage == null)
			translationLanguage = English;

		//translationLanguage = English


		float mgf = 1;
		//Margin Factor
		//NB this uses the * operator overload on clsNullablePrice - which preserves the IsValid/IsList
		// Fix for recalculation bug ignore pre installed items
		if (!this.IsPreInstalled) {
			int dq = this.DerivedQuantity;
			//autoadded things are not preinstalled - presinstalle din synonymous with "FIO"
			runningtotal += this.BasePrice * dq * mgf;
			//this total ALWAYS includes any margin applied
		}


		object th = "<th style='background-color:#0096D6;text-align:left;'>";
		//This is the root item (it has no branch!)
		if (this.Branch == null) {

			EmailSummary += "<table cellpadding='3px' style='font-family:arial;font-size:10pt;border-collapse:collapse;border:solid gray 1px;'>";
			EmailSummary += "<tr>";
			EmailSummary += th + Xlt("Product type", translationLanguage) + "</th>";
			EmailSummary += th + Xlt("Part Number", translationLanguage) + "</th>";
			EmailSummary += th + Xlt("Description", translationLanguage) + "</th>";
			EmailSummary += th + Xlt("Quantity", translationLanguage) + "</th>";
			EmailSummary += th + Xlt("Unit Price", translationLanguage) + "</th>";
			EmailSummary += th + Xlt("Note", translationLanguage) + "</th>";


		} else {
			product = this.Branch.Product;

			//EmailSummary = "<div style='margin-left:3em;" & IIf(product.isSystem, "font-weight:bold;", "font-weight:normal;") & IIf(Me.IsPreInstalled, "font-style:italic;", "font-style:normal;") & "clear:both;'>" & vbCrLf


			//Dim border$ = "border:1px solid black;"
			//            Dim display$ = "float:left;" '"display:inline-block;"

			//If Not Me.validate Then EmailSummary &= "* "

			if (!this.IsPreInstalled | (includePreinstalled & this.IsPreInstalled)) {
				EmailSummary += "<tr>";

				object ct, ect;
				//cell type/end cell type
				ct = "<td style='padding-left:10px;'>";
				ect = "</td>";
				if (product.isSystem(this.Path)){ct = "<th style='text-align:left;'>";ect = "</th>";}

				//    If Me.IsPreInstalled Then
				// EmailSummary &= "preInstalled"
				// End If

				//Product type NoteBook/Server/HardDisk Drive Etc.
				//EmailSummary &= "<div style='width:15em;" & display$ & border$ & "'>" & product.ProductType.Translation.text(s_lang) & "</div>" & vbCrLf
				EmailSummary += ct + product.ProductType.Translation.text(translationLanguage) + ect;

				//EmailSummary &= "<div style='width:10em;" & display$ & border$ & "'>" & product.sku & "</div>" & vbCrLf
				if (product.SKU.StartsWith("###")) {
					EmailSummary += ct + "built in" + ect;
				} else {
					EmailSummary += ct + product.SKU + ect;
				}


				object desc = "No description available";

				if (product.i_Attributes_Code.ContainsKey("desc")) {
					desc = product.i_Attributes_Code("desc")(0).Translation.text(translationLanguage);
				}

				//EmailSummary &= "<div style='width:30em;" & display$ & border$ & "'>" & desc$ & "</div>" & vbCrLf
				EmailSummary += ct + desc + ect;

				//Quantity
				//EmailSummary &= "<div style='width:2em;" & display$ & border$ & "'>" & Me.Quantity & "</div>" & vbCrLf
				EmailSummary += ct + this.Quantity + ect;

				//Price
				//EmailSummary &= "<div style='width:8em;" & display$ & border$ & "'>" & Me.QuotedPrice.DisplayPrice(quote.BuyerAccount).Text & "</div>" & vbCrLf

				if (this.IsPreInstalled) {
					EmailSummary += ct + "-" + ect;
				} else {
					NullablePrice PriceIncludingAnyMargin = this.BasePrice * this.Margin;
					EmailSummary += ct + PriceIncludingAnyMargin.text(quote.BuyerAccount, errorMessages) + ect;
				}

				//TODO (avalanche/rebates)
				//Dim promoMarkers As PlaceHolder = Me.Branch.PromoIndicators(BuyerAccount, Me.Path, "quoteTreeAvalancheStar")
				//UI.Controls.Add(promoMarkers)
				//If Me.rebate <> 0 Then
				//    Dim avlabel As Label = New Label
				//    avlabel.Text = "✔"
				//    avlabel.CssClass &= "quoteTreeAvalancheTick"  'probably needs a slight change (add a seperate *)

				//    Dim saving As nullablePrice = New nullablePrice(Me.rebate, Me.quote.Currency) 'making it into a nullableprice allows us to get this displaprice (lable) - which does the currency/culture formatting
				//    avlabel.ToolTip = "Qualifies - Saving  " & saving.DisplayPrice(quote.BuyerAccount).Text & " per item (" & Me.OPG.value & ")"

				//    UI.Controls.Add(avlabel)
				//End If

				//EmailSummary &= "<div style=width:20em;" & display$ & border$ & "'>"
				EmailSummary += ct;
				if (!object.ReferenceEquals(this.Note.value, DBNull.Value)) {
					EmailSummary += this.Note.DisplayValue;
				}
				//EmailSummary &= "</div>" & vbCrLf
				EmailSummary += ect;

				EmailSummary += "</tr>";

				//For Each vm As ClsValidationMessage In Me.Msgs
				// UI.Controls.Add(vm.UI(BuyerAccount, agentAccount.Language))
				// Next

			}
		}

		//we *always* recurse (otherwise we'd lose options in preinstalled options) .. but only append the HTML for preinstalled items if includePreinstalled=true

		foreach ( item in this.Children.OrderBy(x => x.order)) {
			if (!this.IsPreInstalled | includePreinstalled) {
				EmailSummary += item.EmailSummary(includePreinstalled, BuyerAccount, agentAccount, errorMessages, runningtotal);
			}
		}

		//EmailSummary &= "</div>" & vbCrLf

		if (this.Branch == null) {
			//Grand total 'may have a * for contains list price elements
			EmailSummary += "<tr><td></td><td></td><td></td><td></td><td> " + Xlt("TOTAL", translationLanguage) + " " + runningtotal.text(BuyerAccount, errorMessages) + (string)IIf(runningtotal.isList, " " + Xlt("Contains list price elements", translationLanguage), "") + "</td><td></td><tr>";
			EmailSummary += "</table>";
		}



	}


	public Panel FlatList(clsAccount buyerAccount, ref List<string> errorMessages)
	{

		//Returns a HTML Panel 'bill of materials' type consolidated view of the 
		//Flatlist is called on the quotes 'root' item - it 
		//Calls the consolidated() function - which recurses through all quoteitems to return a dictionary of the counts of parts, indexed by Product.

		clsLanguage translationLanguage = this.quote.AgentAccount.Language;
		if (translationLanguage == null)
			translationLanguage = English;

		//translationLanguage = English


		FlatList = new Panel();
		FlatList.CssClass = "flatQuotePanel";

		//Dim tbl As Table = New Table
		//FlatList.Controls.Add(tbl)

		//Dim thr As TableHeaderRow = New TableHeaderRow
		//tbl.Controls.Add(thr)

		//Dim thc As TableHeaderCell
		//thc = New TableHeaderCell
		//thr.Controls.Add(thc)
		//thc.Text = "Qty"

		//thc = New TableHeaderCell
		//thr.Controls.Add(thc)

		//thc = New TableHeaderCell
		//thr.Controls.Add(thc)
		//thc.Text = "Part#"

		//thc = New TableHeaderCell
		//thr.Controls.Add(thc)
		//thc.Text = "Price"

		//Dim tr As TableRow
		//Dim td As TableCell

		clsProduct Product;

		Label lbl;
		Panel line;


		//For Each lineitem In quote.RootItem.Flattened(True, False, 0).items  'see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote
		//see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote
		foreach ( lineitem in this.Flattened(true, false, 0).items) {


			Product = lineitem.QuoteItem.Branch.Product;
			line = new Panel();
			FlatList.Controls.Add(line);
			line.CssClass = "flatLine";
			if (Product.isSystem(this.Path))
				line.CssClass += " isSystem";

			//tr = New TableRow
			//tbl.Controls.Add(tr)

			//td = New TableCell
			//tr.Controls.Add(td)
			lbl = new Label();
			lbl.CssClass = "quoteFlatQty";
			lbl.Text = lineitem.Quantity.ToString + " ";
			//note.. this can be a consolidated quantity (from more than one quote line) 
			line.Controls.Add(lbl);

			//td = New TableCell
			//tr.Controls.Add(td)
			lbl = new Label();
			lbl.CssClass = "quoteFlatType";
			lbl.Text = Product.ProductType.Translation.text(translationLanguage);
			line.Controls.Add(lbl);


			//td = New TableCell
			//tr.Controls.Add(td)
			lbl = new Label();
			lbl.CssClass = "quoteFlatSKU";
			line.Controls.Add(lbl);

			if (Product.i_Attributes_Code.ContainsKey("MfrSKU")) {
				lbl.Text = Product.SKU;
			}

			if (Product.i_Attributes_Code.ContainsKey("desc")) {
				lbl.ToolTip = Product.i_Attributes_Code("desc")(0).Translation.text(translationLanguage);
			}

			//calls NEW internally (to create a new label)

			//td = New TableCell
			//tr.Controls.Add(td)
			NullablePrice PriceIncludingMargin = lineitem.QuoteItem.BasePrice * lineitem.QuoteItem.Margin;

			Panel pp = PriceIncludingMargin.DisplayPrice(quote.BuyerAccount, errorMessages);

			pp.CssClass += " quoteFlatPrice";
			line.Controls.Add(pp);
		}

	}

	public Panel UI(bool includePreinstalled, clsAccount BuyerAccount, clsAccount agentAccount, HashSet<string> foci, ref List<string> errorMessages, bool validationMode, UInt64 lid)
	{

		//Returns this QuoteItem (as a panel) ..and recurses for all child items (nesting panels)

		UI = new Panel();

		if (this.Branch != null) {
			if (this.Branch.Hidden)
				return;
			//return an entirely emptt panel from hidden (chassis) branches
		}

		UI.CssClass = "quoteItemTree";
		if (!this.validate)
			UI.CssClass += " exVal";
		if (object.ReferenceEquals(this, quote.MostRecent))
			UI.CssClass += " mostRecent";
		//used to target the flying frame animation '~~~
		if (object.ReferenceEquals(this, quote.Cursor))
			UI.CssClass += " quoteCursor";
		//used to target the flying frame animation

		UI.ID = "QI" + this.ID;
		clsProduct product;
		bool issystem = false;

		//AKA 'Parts bin
		if (object.ReferenceEquals(this, this.quote.RootItem)) {
			//this is the outermost 'root' item (where we *can* add (orphaned) options)
			//When we click on - OR THE EVENT BUBBLING REACHES the root item - we 'unlock' the cursor
			//                                                                                   note this is OUTside the IF
			//was omd
			UI.CssClass += " quoteRoot";


			//'THIS WAS THE 'PARTS BIN' - Which was disbaled at Gregs request /01/01/2015 - UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ",'');}"
			//The root item

			Panel qh = new Panel();
			qh.CssClass = "quoteHeader";



			Panel namepanel = new Panel();

			namepanel.Controls.Add(NewLit(Xlt("Quote", agentAccount.Language) + " " + this.quote.RootQuote.ID + "-" + quote.Version + quote.Saved ? "<span class='saved'>(" + Xlt("saved", BuyerAccount.Language) : "<span class='draft'>(" + Xlt("draft", BuyerAccount.Language) + ")</span>"));
			qh.Controls.Add(namepanel);
			//action buttons
			Literal butts = new Literal();
			List<ClsValidationMessage> criticalMsgs = this.ValidationsGreaterThanEqualTo(EnumValidationSeverity.RedCross);
			//SAMS version FindValidation(selectedMsgs, Me)

			//       If Me.quote.Saved Then
			// 'butts.Visible = False
			//butts.Text = "<div class='q_outputs'><div class='q_saved' onclick = ""burstBubble(event); quoteEvent('Save'); return false;"" title ='" & Xlt("Save", agentAccount.Language) & "'></div> "
			//Else
			//            butts.Text = "<div class='q_outputs'><div class='q_save' onclick = ""burstBubble(event); quoteEvent('Save'); return false;"" title ='" & Xlt("Save", agentAccount.Language) & "'></div> "
			// End If

			// need to sort out the images for 
			if (criticalMsgs.Count == 0) {
				//"<div class='q_outputs'><div class='q_save' onclick = ""burstBubble(event); quoteEvent('Save'); return false;"" title ='Save'></div> " & _
				butts.Text = butts.Text + "<div title ='" + Xlt("Export", agentAccount.Language) + "' class='q_export ' onclick = \"burstBubble(event); showMenu(" + this.quote.ID.ToString + "); return false;\"><div id = \"exportMenu" + this.quote.ID.ToString + "\"  class = \"submenu\" > " + "<a class=\"account\"> " + Xlt("Export Option", agentAccount.Language) + "s</a> <ul class=\"root\" >" + "<li><a onclick = \"burstBubble(event); quoteEvent('Excel'); return false;\" href=\"#\">" + Xlt("Excel", agentAccount.Language) + "</a></li>" + "<li><a onclick = \"burstBubble(event);  quoteEvent('PDF'); return false;\" href=\"#\">" + Xlt("PDF", agentAccount.Language) + "</a></li>" + "<li><a onclick = \"burstBubble(event);  quoteEvent('XML'); return false;\" href=\"#\">" + Xlt("XML", agentAccount.Language) + "</a></li>" + "<li><a href=\"#\" onclick = \"burstBubble(event);  quoteEvent('XMLAdv'); return false;\">" + Xlt("XML Advanced", agentAccount.Language) + "</a></li>" + "<li><a href=\"#\" onclick = \"burstBubble(event);  quoteEvent('XMLSmartQuote'); return false;\">" + Xlt("XML SmartQuote", agentAccount.Language) + "</a></li></ul>" + " </div></div> " + "<div title ='" + Xlt("Email", agentAccount.Language) + "' class='q_email' onclick = \"burstBubble(event); quoteEvent('Email'); return false;\"></div> ";
			//& "<div class='q_excel' onclick = ""burstBubble(event); saveNote(); quoteEvent('Excel'); return false;""></div> " _
			//& "<div class='q_xml' onclick = ""burstBubble(event); saveNote(); quoteEvent('XML'); return false;""></div></div>"
			} else {
				butts.Text = butts.Text + "<div title ='" + Xlt("Export", agentAccount.Language) + "' class='q_export ' onclick = \"burstBubble(event); displayMsg('" + Xlt("Export not available due to validation errors", BuyerAccount.Language) + "'); return false;\"></div>";
			}
			if (quote.Locked) {
				butts.Text = butts.Text + "<div title ='" + Xlt("Create Next Version", agentAccount.Language) + "' class='q_newVlocked' onclick = \"burstBubble(event); displayMsg('Next version created');  rExec('Manipulation.aspx?command=createNextVersion&quoteId=" + quote.ID + "', showQuote); return false;\"></div> ";
			} else {
				if (quote.Saved) {
					butts.Text = butts.Text + "<div  title ='" + Xlt("Create Next Version", agentAccount.Language) + "' class='q_newVunlocked' onclick = \"burstBubble(event);displayMsg('Next version created');  rExec('Manipulation.aspx?command=createNextVersion&quoteId=" + quote.ID + "', showQuote); return false;\"></div>";
				}

				//Dim btnNextVersion As New Button
				//btnNextVersion.Text = Xlt("Create next version", agentAccount.Language)
				//btnNextVersion.ToolTip = Xlt("Creates a copy leaving the original quote intact", agentAccount.Language)
				//btnNextVersion.OnClientClick = "rExec('Manipulation.aspx?command=createNextVersion&quoteId=" & quote.ID & "', gotoTree);return false;"
				//UI.Controls.Add(btnNextVersion)

			}


			if (quote != null) {
				if (criticalMsgs.Count == 0 & (iq.sesh(lid, "GK_BasketURL") != null | agentAccount.SellerChannel.orderEmail != "")) {
					// Dim litAddtobasket = New Literal
					//butts.Text = butts.Text & "<div class='hpBlueButton smallfont ib'  onclick = ""burstBubble(event); saveNote(false); quoteEvent('Addbasket'); return false;"">" & Xlt("Place Order", agentAccount.Language) & "</div></div> "

					butts.Text = butts.Text + "<div class='hpOrangeButton q_basket smallfont'  onclick = \"burstBubble(event); saveNote(false); quoteEvent('Addbasket" + quote.Saved + "'); return false;\">&nbsp;</div>";
				} else {
					//butts.Text = butts.Text & "</div> "
				}

			//Dim addToBasket As Button = New Button()
			//addToBasket.Text = Xlt("Add to Basket", quote.BuyerAccount.Language)
			//addToBasket.ID = "btnAddToBasket"
			//AddHandler addToBasket.Click, AddressOf Me.addToBasket_Click

			//   qh.Controls.Add(litAddtobasket)
			} else {
				butts.Text = butts.Text + "</div>";
			}

			namepanel.Controls.Add(butts);

			Panel ih = new Panel();

			ih.CssClass = "innerHeader";


			//quoteName/Customer - Sams pop open panel - part of the export tools
			Panel qnp = new Panel();
			qnp.ID = "quotepanel";


			object qn = this.quote.Name.DisplayValue;
			if (Trim(qn) == "" | Trim(qn) == "-")
				qn = this.quote.BuyerAccount.BuyerChannel.Name;


			qnp.Controls.Add(NewLit("<div id = 'quoteText' ><input id='saveQuoteName'" + quote.Saved ? " value='" + qn + "'" : "" + " type='text' placeholder='" + Xlt("enter quote name", BuyerAccount.Language) + "' onclick= 'burstBubble(event); return false;'   onkeydown = 'var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode==13){return false;}'/> " + "<input id = 'hiddenType' type='hidden' value='Save' /><input id = 'hiddenName' type='hidden' value='" + qn + "' /><input id ='hdnEmail' type = 'hidden' value ='" + this.quote.BuyerAccount.User.Email + "'/><input id = 'continueBtn' type='button' class=\"hpBlueButton smallfont\" style=\"margin-top:-0.75em; margin-left:0.6em;\" value = '" + Xlt("Save", agentAccount.Language) + "' onClick ='burstBubble(event); continueClick();'/><input id = 'cancelBtn' type='button' onclick=\"burstBubble(event); $('#saveQuoteName').val($('#hiddenName').val());$('#continueBtn').val($('#hiddenSaveTrans').val());$('#cancelBtn').hide();$('#quoteText').show();$('#hiddenType').val('Save');return false;\" class=\"hpBlueButton smallfont\" style='display:none;margin-top:-0.70em; margin-left:0.6em;' value ='" + Xlt("Cancel", agentAccount.Language) + "'  />" + "<input id = 'hiddenEmailTrans' type='hidden' value ='" + Xlt("Send Email", agentAccount.Language) + "'  /><input id = 'hiddenSaveTrans' type='hidden' value ='" + Xlt("Save", agentAccount.Language) + "'  /></div>"));
			// removed 19/01 <input id = 'cancelBtn' type='button' class=""hpGreyButton  smallfont"" value = '" & Xlt("Cancel", agentAccount.Language) & "' style=""margin-top:-0.75em; margin-left:0.6em;"" onClick ='burstBubble(event); quoteCancel();'/>


			//Systems Options summary + validation rollup

			Panel sysOptSum = new Panel();
			sysOptSum.ID = "sysOptSumm";

			int systems;
			int options;
			this.countSystems(systems, options);

			string syss;
			if (systems > 1 | systems == 0)
				syss = "systems";
			else
				syss = "system";
			string opts;
			if (options > 1 | options == 0)
				opts = "options";
			else
				opts = "option";
			syss = Xlt(syss, agentAccount.Language);
			opts = Xlt(opts, agentAccount.Language);

			sysOptSum.Controls.Add(NewLit("<span class='sysOptCount'>" + systems + " " + syss + ", " + options + " " + opts + "</span>"));

			Panel vdRollup = new Panel();
			vdRollup.CssClass = "validationRollup";
			sysOptSum.Controls.Add(this.MessageCounts({ enumValidationMessageType.Validation }, {
				
			}, {
				EnumValidationSeverity.DoesQualify,
				EnumValidationSeverity.DoesntQualify
			}, true));
			//exclude flex qualification messages from the roll up

			Label lblSpace = new Label();
			lblSpace.Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
			sysOptSum.Controls.Add(lblSpace);

			object dlp = new Dictionary<clsScheme, int>();
			this.LoyaltyPoints(dlp);

			object BCTotal = 0;
			string toolTip = "";

			if (iq.i_scheme_code.ContainsKey("BC")) {
				foreach ( scheme in iq.i_scheme_code("BC")) {
					if (scheme.Region.Encompasses(agentAccount.SellerChannel.Region)) {
						//We have an active region for this account
						if (dlp.ContainsKey(scheme))
							BCTotal += dlp(scheme);
						toolTip = scheme.displayName(agentAccount.Language);
					}
				}
			}


			if (BCTotal > 0) {
				string points = Xlt("Points", agentAccount.Language);

				Label lblpointsTitle = new Label();
				lblpointsTitle.CssClass = "BlueCarpetTitle";
				lblpointsTitle.Text = points + ": ";
				lblpointsTitle.ToolTip = points;
				sysOptSum.Controls.Add(lblpointsTitle);

				Label lblpoints;
				lblpoints = new Label();
				lblpoints.CssClass = "BlueCarpet";
				lblpoints.Text = BCTotal.ToString;
				lblpoints.ToolTip = points;
				sysOptSum.Controls.Add(lblpoints);

				Label lblSpace2 = new Label();
				lblSpace2.Text = lblSpace.Text;
				sysOptSum.Controls.Add(lblSpace2);

				LinkButton lnkBtn = new LinkButton();
				lnkBtn.Attributes.Add("onClick", "return false;");

				lnkBtn.Text = Xlt("Learn More", BuyerAccount.Language);
				lnkBtn.OnClientClick = "LearnMoreClick();";

				sysOptSum.Controls.Add(lnkBtn);
			}

			//grand total
			//If Me.quote.QuotedPrice.isValid Then
			Panel PnlGrandTotal = new Panel();
			PnlGrandTotal.CssClass = "grandTotal";
			PnlGrandTotal.Controls.Add(NewLit(Xlt("Total", agentAccount.Language) + " "));

			//price panel
			NullablePrice finalprice = new NullablePrice(this.quote.QuotedPrice.NumericValue - this.quote.TotalRebate, this.quote.Currency, this.quote.QuotedPrice.isList);
			finalprice.isTotal = true;
			Panel pp = finalprice.DisplayPrice(BuyerAccount, errorMessages);
			pp.CssClass += " finalPrice";
			PnlGrandTotal.Controls.Add(pp);

			//show the 'quotewide',propogating margin only once there is more than one item at the root level
			if (this.quote.RootItem.Children.Count > 1 & !(agentAccount.SellerChannel.marginMin == 0 & agentAccount.SellerChannel.marginMax == 0)) {
				PnlGrandTotal.Controls.Add(this.MarginUI(true, this.quote.Locked));
				//whole quote margin - goes inside the header at Dans )
			}


			ih.Controls.Add(qnp);
			ih.Controls.Add(PnlGrandTotal);
			ih.Controls.Add(sysOptSum);
			qh.Controls.Add(ih);
			UI.Controls.Add(qh);
		} else {
			product = this.Branch.Product;

			//skip chassis (and other invisible) branches
			if (product != null) {
				if (this.Branch.childBranches.Count > 0) {
					//SYSTEM
					issystem = true;
					//this quote items' product branch has sub items and so can be targetted for options (it's a 'system')
					UI.CssClass += " quoteSystem";
					UI.Controls.Add(this.SystemHeader(agentAccount, BuyerAccount, errorMessages));
					//the virtual system/rollup/total header (also contains the expand/collapse button
					UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + this.ID + ");getBranches('cmd=open&path=" + this.Path + "&into=tree&Paradigm=C')};";

				} else {
					//It's an OPTION - show it in the tree (and highlight it if its clicked)
					UI.CssClass += " quoteOption";

					string @from = "tree." + iq.RootBranch.ID;
					//Default to 'from' the root
					if (this.SystemItem != null)
						@from = this.SystemItem.Path;
					//Override with the system if its there
					UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + this.Parent.ID + ");getBranches('cmd=open&path=" + @from + "&to=" + this.Path + "&into=tree&Paradigm=C')};";

				}
				//          UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.Parent.ID & ");getBranches('cmd=open&path=" & from & "&to=" & Me.Path & "&into=tree')};"
			}


			if (!this.collapsed) {
				//note - for Pauls benefit if you have the DIAGVIEW role .. you see ALL products int he basket

				if (this.ShouldShowInBasket(includePreinstalled, BuyerAccount, foci)) {
					UI.Controls.Add(this.basketLine(agentAccount, BuyerAccount, product, foci, errorMessages, lid));
				}

				//Dim vms As Panel = New Panel
				//UI.Controls.Add(vms)
				//vms.CssClass = "validationMessages"
				//For Each vm As ClsValidationMessage In Me.Msgs
				//    If vm IsNot Nothing Then  'TODO remove - some 'nothings' are getting in
				//        vms.Controls.Add(vm.UI(BuyerAccount, agentAccount.Language, errorMessages))
				//    End If
				//Next


			}
		}


		//we *always* recurse (Through invisible options) otherwise we'd lose options in preinstalled options) .. but only append the HTML for preinstalled items if includePreinstalled=true

		//dont recurse into collapse items

		if (!this.collapsed) {
			Panel options = new Panel();
			Panel systems = new Panel();

			Panel addto;
			UI.Controls.Add(systems);
			UI.Controls.Add(options);

			string prevDisplayText = "";
			ArrayList itemsToRemove = new ArrayList();
			bool isPrevPreInstalled;
			bool isPreInstalled;
			int prevItemQuantity;
			int itemQuantity;
			string path;
			bool isASystem;

			ArrayList prevItemQuantities = new ArrayList();
			ArrayList prevDisplayTexts = new ArrayList();
			IQ.clsQuoteItem prevItem = null;

			//' This avoids systems and only uses options. It also orders by the option name.

			foreach ( item in from c in this.Childrenwhere c.order > 2orderby c.SKUVariant.DistiSku) {
				path = item.Path;
				isASystem = item.Branch.Product.isSystem(path);
				if ((isASystem == false)) {
					string displayText = item.SKUVariant.DistiSku;
					isPreInstalled = item.IsPreInstalled;

					itemQuantity = item.Quantity;
					if (((displayText == prevDisplayText) & (isPreInstalled == false) & (isPrevPreInstalled == false))) {
						item.Quantity = prevItemQuantity + item.Quantity;
						if ((!prevItem == null)) {
							this.Children.Remove(prevItem);
						}
					}
					prevDisplayText = displayText;
					isPrevPreInstalled = isPreInstalled;
					prevItemQuantity = item.Quantity;
					prevItem = item;
				}
			}

			//order systems first then options - so that we can group the root level options into a parts bin (single div) we can draw a box around

			foreach ( item in this.Children.Where(ch => ch.ShouldShowInBasket(includePreinstalled, BuyerAccount, foci)).GroupBy(ch => ch.Branch.Product.isSystem(ch.Path))) {
				if (item.Key)
					addto = systems;
				else
					addto = options;

				//Ruined below temp with andalso False until this is ok'd
				foreach ( i in item.GroupBy(ch => ch.Branch.Product.ProductType.Translation.text(English) + ch.IsPreInstalled && false ? 0 : ch.ID.ToString).OrderByDescending(ch => ch.First.Branch.Product.ProductType.Order).OrderByDescending(ch => ch.First.IsPreInstalled)) {
					if (i.Count > 1 & i.First.IsPreInstalled) {
						//Add header
						Panel panel = new Panel();
						panel.CssClass = "quoteGroup";
						panel.Attributes("OnClick") = "burstBubble(event);";
						panel.Controls.Add(NewLit("<h3 style=\"outline-color:white;background:white;\">" + i.First.Branch.Product.ProductType.Translation.text(BuyerAccount.Language) + "</h3>"));
						Panel panel2 = new Panel();
						addto.Controls.Add(panel);
						panel.Controls.Add(panel2);

						foreach ( qi in i) {
							if (qi.Branch != null)
								panel2.Controls.Add(qi.UI(includePreinstalled, BuyerAccount, agentAccount, foci, errorMessages, validationMode, lid));
						}
					} else {
						//was me.preinstalled - which was a bug (i think) NA 12/06/2014
						if (!i.First.IsPreInstalled | includePreinstalled) {
							//and... recurse
							if (i.First.Branch != null) {
								if (!i.First.Branch.Product.isSystem(i.First.Path)) {
									if (object.ReferenceEquals(this, quote.RootItem)) {
										if (object.ReferenceEquals(this, quote.RootItem))
											options.Attributes("class") = "partsBin";
									}
								}
							}

							//formerly UI.controls.add 

							addto.Controls.Add(i.First.UI(includePreinstalled, BuyerAccount, agentAccount, foci, errorMessages, validationMode, lid));
						}
					}
				}
			}



			//    Case Is = viewTypeEnum.Summary
			//this was the old summary/BOM view - which has been obsoleted (before it ever made it out) 'is essentially the same - potentially consolidates some items
			// UI.Controls.Add(Me.FlatList(BuyerAccount, errorMessages))

			//        If viewType and viewTypeEnum).validation then 
			//Case Is = viewTypeEnum.validation


			if (issystem) {
				UI.Controls.Add(this.SystemFooter(BuyerAccount, agentAccount, errorMessages));
				//Includes flex checklist

				if (this.ExpandedPanels.Contains(panelEnum.Spec)) {
					UI.Controls.Add(this.specPanelOpen);
				} else {
					if (specPanelClosed != null) {
						UI.Controls.Add(this.specPanelClosed);
					}
					//UI.Controls.Add(Me.validationpanel)
					//UI.Controls.Add(Me.PromosPanel)
				}

				UI.Controls.Add(this.ValidationPanel(BuyerAccount, agentAccount, errorMessages));
				// OBSOLETED      UI.Controls.Add(Me.PromosPanel(agentAccount)) ' Total rebate, Loyalty point by schecme, Bundle savings

			}

			//'output each items validation messages *HERE* to get them in contexct
			//Dim vms As Panel = New Panel
			//UI.Controls.Add(vms)
			//vms.CssClass = "validationMessages"
			//For Each vm As ClsValidationMessage In Me.Msgs
			//    If vm IsNot Nothing Then  'TODO remove - some 'nothings' are getting in
			//        vms.Controls.Add(vm.UI(BuyerAccount, agentAccount.Language, errorMessages))
			//    End If
			//Next

		}

	}

	//Private Shared Function Flatten(source As IEnumerable(Of clsQuoteItem)) As IEnumerable(Of clsQuoteItem)
	//    Return source.Concat(source.SelectMany(Function(p) Children.Flatten()))
	//End Function

	public  Iterator;
	 Yield;
}
// check null if you must
