using System.Xml;
using dataAccess;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;




public class clsQuoteItem : ISqlBasedObject
{

    private const string PleaseCreateNewVersionToChangeQuote = "Please create new version to change quote.";
    public int ID { get; set; }
    public clsQuote quote;
    public int Indent { get; set; }

    public clsBranch Branch { get; set; }
    public clsVariant SKUVariant { get; set; }

    public string Path { get; set; } //The numeric (dotted) path to this item in the product tree
    public int Quantity { get; set; }
    public NullablePrice BasePrice { get; set; } //Pre margin 'quoted'  - price (from webserivce, or wherever)
    public NullablePrice ListPrice { get; set; } // contains a snapshot of  the list price at the time the quote was prepared

    public nullableString OPG { get; set; }
    public nullableString Bundle { get; set; } //an OPG can have many bundles
    public decimal rebate { get; set; } //amount of cash off this item for this OPG

    public float Margin { get; set; }

    public List<clsQuoteItem> Children { get; set; }
    public clsQuoteItem Parent { get; set; }

    public List<ClsValidationMessage> Msgs { get; set; } //String 'error (or other) message against this quote line item
    public bool IsPreInstalled { get; set; } //Means you cant remove it from the quote - or flex its quantities (adding will add a new quote item)
    public DateTime Created { get; set; }
    public bool validate { get; set; } //Whether to include in validation (or not)

    public nullableString Note { get; set; }

    public bool collapsed { get; set; }
    public HashSet<panelEnum> ExpandedPanels { get; set; } // panelEnum  'System = 1, Options = 2, Spec = 4, Validation = 8, Promo= 16
    public int order { get; set; } //this is not yet persited

    //Property price As clsPrice

    EnumHideButton FlexButtonState;
    private int ImportId;
    private bool allRulesQualified = false;
    public Panel specPanelOpen; //the slot summaries ('specs' are built at a system level 'outside' the quote items via quote.validate
    public Panel specPanelClosed;

    public Dictionary<clsSlotType, clsSlotSummary> dicslots { get; set; }

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

        if (this.SKUVariant == skuvariant && price.value >= 0)
        {

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

        foreach (var c in this.Children.ToArray) //iterate over an array (copy) of the list  (attempt to avoid a 'collection  modified enumeration may not execute' )
        {
            c.updateQuotedPrice(skuvariant, price);
        }

    }

    public List<clsQuoteItem> findSystemItems()
    {
        List<clsQuoteItem> returnValue = default(List<clsQuoteItem>);

        //returns a flat list of the 'system' items (which are indepentely validated) - things like a Chassis

        returnValue = new List<clsQuoteItem>();

        if (this.Parent != null && this.Parent != this.quote.RootItem)
        {
            return returnValue;
        }

        if (!(this.Branch == null)) //the root item has no branch (is just a placeholder)
        {
            if (this.Branch.Product.isSystem(this.Path))
            {
                returnValue.Add(this);
            }
        }



        foreach (var child in this.Children)
        {
            returnValue.AddRange(child.findSystemItems);
        }

        return returnValue;
    }


    public void ApplyMargin(float factor, bool propagate)
    {

        this.Margin = factor;
        if (propagate)
        {
            foreach (var c in this.Children)
            {
                c.ApplyMargin(factor, propagate);
            }
        }

        //     Me.Update()

    }

    public void doWarnings(Dictionary<clsSlotType, clsSlotSummary> dicslots)
	{
		
		//called for each top level item
		
		if (this.Branch.Product.isSystem(this.Path))
		{
			//this isn't pretty - but becuase of the generic nature of 'products' we don't know what they 'are' (Desktop, notebook, server, storage) - so we read *where* they are (from the first level of the tree) to derive their fundamental type
			//should probably just add a sysType attribute - but validation would be its only purpose (at present)
			string systype = "";
			object[] p = this.Path.Split('.');
			systype = Strings.LCase(System.Convert.ToString(iq.Branches(System.Convert.ToInt32(p[2])).Translation.text(English)));
			
			if (iq.ProductValidationsAssignment.ContainsKey(systype))
			{
				
				//build list of components
				object parts = this.listComponents(false);
				
				foreach (var ProdVal in iq.ProductValidationsAssignment(systype))
				{
					string pth = "";
					if (ProdVal.ValidationType == enumValidationType.MustHave)
					{
						//Split multiple opt types with a /
						bool pres = false;
						foreach (var ot in ProdVal.RequiredOptType.Split("/".ToArray))
						{
							if (string.IsNullOrEmpty(pth))
							{
								pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ot, true)); //only populate the path if it isnt already (for instances like PCI and MOD network cards where one system may have one one may have the other)
							}
							
							if (parts.Where(par => par.OptType, ot, CompareMethod.Binary).Count() == 0)
							{
								//Option type not present
							}
							else if (!string.IsNullOrEmpty(System.Convert.ToString(ProdVal.OptionFamily)) && parts.Where(pa => pa.hasAttribute("optFam", ProdVal.OptionFamily, true)).Count() == 0)
							{
								//OptionFamily specified but does not exist
								
							}
							else if (!string.IsNullOrEmpty(System.Convert.ToString(ProdVal.CheckAttributeValue)) && parts.Where(pa => pa.hasAttribute(ProdVal.CheckAttribute, ProdVal.CheckAttributeValue, true)).Count() == 0)
							{
								//Check attribute present but value not found
							}
							else
							{
								pres = true;
							}
						}
						
						if (!pres)
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, new[] {}, null, "", null, ProdVal));
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.MustHaveProperty)
					{
						bool found = false;
						if (this.Branch.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute) && ((this.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation != null && this.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation.text(English) == ProdVal.CheckAttributeValue) || this.Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.NumericValue.ToString() == ProdVal.CheckAttributeValue))
						{
							found = true;
						}
						foreach (var pa in parts)
						{
							clsBranch prod = iq.Branches(System.Convert.ToInt32(Strings.Split(System.Convert.ToString(pa.Path), ".").Last));
							
							if (prod.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute) && ((prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation != null && prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.Translation.text(English) == ProdVal.CheckAttributeValue) || prod.Product.i_Attributes_Code(ProdVal.CheckAttribute).First.NumericValue.ToString() == ProdVal.CheckAttributeValue))
							{
								found = true;
							}
						}
						if (!found)
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split(System.Convert.ToString(ProdVal.RequiredOptType.Split("/".ToArray).First), ",")));
						}
						//Not implemented yet
					} //redundant
					else if (ProdVal.ValidationType == enumValidationType.Slot)
					{
						System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dics = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
						
						if (dics.Value == null)
						{
							continue;
						}
						pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true));
						if (dics.Value.Given < dics.Value.taken)
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split(dics.Key.Translation.text(English) + "," + dics.Value.taken.ToString() + "," + dics.Value.Given.ToString(), ",")));
						}
						else
						{
							this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Strings.Split("")));
						}
					} //redundant
					else if (ProdVal.ValidationType == enumValidationType.CapacityOverload)
					{
						System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dics = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
						if (dics.Value == null)
						{
							continue;
						}
						if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Sum(par => FindRecursive(par.Path, true).Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute).First().NumericValue) > dics.Value.TotalCapacity)
						{
							pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true));
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split(System.Convert.ToString(dics.Key.Translation.text(English)), ",")));
						}
						else
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, ProdVal.Message, ProdVal.CorrectMessage, "", 0, 0, Strings.Split("")));
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.Dependancy)
					{
						if (!string.IsNullOrEmpty(System.Convert.ToString(ProdVal.DependantCheckAttribute)) || !string.IsNullOrEmpty(System.Convert.ToString(ProdVal.CheckAttribute)))
						{
							bool foundSource = false;
							bool foundTarget = false;
							foreach (var ot in ProdVal.DependantOptType.ToUpper.Split("/".ToArray))
							{
								foreach (var i in parts.ToList().Select(pa => pa.Path))
								{
									clsProduct part = FindRecursive(i, true).Branch.Product; //Get associated product
									if ((string.IsNullOrEmpty(System.Convert.ToString(ProdVal.CheckAttribute.ToUpper)) || part.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute.ToUpper)) && part.i_Attributes_Code("optType").Select(f => f.Translation.text(English).ToUpper).Where(ppp => ProdVal.RequiredOptType.ToUpper.StartsWith(ppp.ToUpper)).Count() > 0 && (string.IsNullOrEmpty(System.Convert.ToString(ProdVal.CheckAttribute)) || part.i_Attributes_Code(ProdVal.CheckAttribute.ToUpper).Select(f => (f.Translation != null ? (f.Translation.text(English).ToUpper) : (f.NumericValue.ToString()))).Contains(ProdVal.CheckAttributeValue.ToUpper)))
									{
										//We have a product of this type, find if we have the dependant one...
										foundSource = true;
									}
									if ((string.IsNullOrEmpty(System.Convert.ToString(ot)) || part.i_Attributes_Code("optType").Select(f => f.Translation.text(English).ToUpper).Where(ppp => ot.ToUpper.StartsWith(ppp.ToUpper)).Count() > 0) && part.i_Attributes_Code.ContainsKey(ProdVal.DependantCheckAttribute.ToUpper) && part.i_Attributes_Code(ProdVal.DependantCheckAttribute.ToUpper).Select(f => (f.Translation != null ? (f.Translation.text(English).ToUpper) : (f.NumericValue.ToString()))).Contains(ProdVal.DependantCheckAttributeValue.ToUpper))
									{
										foundTarget = true;
									}
								}
							}
							pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.DependantOptType, true));
							if (foundSource && !foundTarget)
							{
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split(""))); //Else Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
							}
						}
						else
						{
							if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Count() > 0 && parts.Where(par => par.OptType == ProdVal.DependantOptType).Count() == 0)
							{
								pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.DependantOptType, true));
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split("")));
							}
							else
							{
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Strings.Split("")));
							}
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.Mismatch)
					{
						if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Count() > 0)
						{
							List<clsProductAttribute> o1 = null;
							List<clsProductAttribute> o2 = null;
							foreach (var i in parts.ToList().Where(pa => pa.OptType == ProdVal.RequiredOptType).Select(pa => pa.Path))
							{
								o2 = (FindRecursive(i, true).Branch.Product.i_Attributes_Code.ContainsKey(ProdVal.CheckAttribute)) ? (FindRecursive(i, true).Branch.Product.i_Attributes_Code(ProdVal.CheckAttribute)) : null;
								if (o2 != null && o2[0].NumericValue > 0)
								{
									if (o1 != null && o1.Select(o1o => (o1o.Translation == null ? o1o.NumericValue.ToString() : (o1o.Translation.text(English)))).Intersect(o2.Select(o2o => (o2o.Translation == null ? o2o.NumericValue.ToString() : (o2o.Translation.text(English))))).Count() != o1.Count)
									{
										pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true));
										this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split(System.Convert.ToString(ProdVal.RequiredOptType), ",")));
										break;
									}
									o1 = o2;
									o2 = null;
								}
							}
						}
						else
						{
							//Me.Msgs.Add(New ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, Nothing, ProdVal.CorrectMessage, "", 0, 0, Split("")))
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.NotToppedUp)
					{
						System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dics = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
						if (dics.Value == null)
						{
							continue;
						}
						pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true));
						if (dics.Value.taken < dics.Value.Given)
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split(dics.Key.Translation.text(English) + "," + dics.Value.taken.ToString() + "," + dics.Value.Given.ToString(), ",")));
						}
						else
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Strings.Split("")));
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.MultipleRequred)
					{
						if (dicslots.Where(par => par.Key.MajorCode == ProdVal.RequiredOptType).Sum(ds => ds.Value.taken) % ProdVal.RequiredQuantity != 0)
						{
							pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true));
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, "".Split(',')));
						}
						else
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Strings.Split("")));
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.UpperWarning)
					{
						System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dics = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
						if (dics.Value == null)
						{
							continue;
						}
						if (dics.Value.taken > ProdVal.RequiredQuantity)
						{
							pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true));
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split(dics.Key.Translation.text(English) + "," + dics.Value.taken.ToString() + "," + dics.Value.Given.ToString(), ",")));
						}
						else
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Strings.Split("")));
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.Exists)
					{
						if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Count() > 0)
						{
							pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true));
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split("")));
						}
						else
						{
							this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, EnumValidationSeverity.greenTick, null, ProdVal.CorrectMessage, "", 0, 0, Strings.Split("")));
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.AtLeastSameQuantity)
					{
						pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ProdVal.RequiredOptType, true));
						if (parts.Where(par => par.OptType == ProdVal.RequiredOptType).Count() > 0 && parts.Where(par => par.OptType == ProdVal.DependantOptType).Count() > 0)
						{
							if (dicslots.Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Sum(ds => ds.Value.taken) > dicslots.Where(ds => ds.Key.MajorCode == ProdVal.DependantOptType).Sum(ds => ds.Value.taken))
							{
								this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, Strings.Split("")));
							}
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.Divisible)
					{
						System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dicsone = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.RequiredOptType).Select(ff => ff).FirstOrDefault();
						System.Collections.Generic.KeyValuePair<clsSlotType, clsSlotSummary> dicstwo = dicslots.ToList().Where(ds => ds.Key.MajorCode == ProdVal.DependantOptType).Select(ff => ff).FirstOrDefault();
						
						if (dicsone.Value == null || dicstwo.Value == null)
						{
							continue;
						}
						if (dicstwo.Value.taken != 0 && dicsone.Value.taken % dicstwo.Value.taken > 0)
						{
							this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, "", 0, 0, Strings.Split("")));
						}
					}
					else if (ProdVal.ValidationType == enumValidationType.SpecRequirement)
					{
						//Split multiple opt types with a /
						foreach (var ot in ProdVal.RequiredOptType.Split("/".ToArray))
						{
							if (string.IsNullOrEmpty(pth))
							{
								pth = System.Convert.ToString(this.Branch.findProductPathByAttributeValueRecursive(this.Path, "optType", ot, true)); //only populate the path if it isnt already (for instances like PCI and MOD network cards where one system may have one one may have the other)
							}
							
							if (parts.Where(par => par.OptType, ot, CompareMethod.Binary).Count() == 0)
							{
								//Option type not present
							}
							else if (!string.IsNullOrEmpty(System.Convert.ToString(ProdVal.OptionFamily)) && parts.Where(pa => pa.hasAttribute("optFam", ProdVal.OptionFamily, true)).Count() == 0)
							{
								//OptionFamily specified but does not exist
							}
							else
							{
								//We have one.
								foreach (var part in parts.Where((pa => pa.OptType), ot, CompareMethod.Binary) && (string.IsNullOrEmpty(System.Convert.ToString(ProdVal.OptionFamily)) || pa.hasAttribute("optFam", ProdVal.OptionFamily, true)))
								{
									if (part.Attributes.ContainsKey(ProdVal.CheckAttribute))
									{
										//Assume then that the translation of this attribute is an opttype and the number is the required (minimum in this case) value for it
										foreach (var attr in part.Attributes(ProdVal.CheckAttribute))
										{
											object targetOptType = attr.Translation.text(English);
											object targetValue = attr.NumericValue;
											if (dicslots.Where(st => st.Key.MajorCode == targetOptType).Sum(ds => ds.Value.TotalCapacity) < targetValue)
											{
												this.Msgs.Add(new ClsValidationMessage(ProdVal.ValidationMessageType, ProdVal.Severity, ProdVal.Message, ProdVal.CorrectMessage, pth, 0, 0, new[] {targetOptType, targetValue.ToString()}, null, "", null, ProdVal));
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
			
			if (blueinfo.Count > 0)
			{
				blueinfo.First.variables = new[] {string.Join("<br><br>", blueinfo.Select(ii => ii.message.text(this.quote.BuyerAccount.Language)))};
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

    public List<ClsValidationMessage> getXtext(string path)
    {
        List<ClsValidationMessage> returnValue = default(List<ClsValidationMessage>);
        returnValue = new List<ClsValidationMessage>();
        returnValue.AddRange(Branch.Product.getXtext(path, this.quote.AcknowledgedValidations));
        returnValue.AddRange(this.Children.Where(ci => !ci.IsPreInstalled).SelectMany(qi => qi.getXtext(qi.Path)));
        return returnValue;
    }


    public bool hasComponentOfType(object type, bool crossSystems)
    {
        bool returnValue = false;

        //crossystems is not yet implemented
        returnValue = false;

        if (this.Branch.Product.i_Attributes_Code.ContainsKey("optType"))
        {
            if (this.Branch.Product.i_Attributes_Code("optType")[0].Translation.text(English) == type)
            {
                returnValue = true;
                return returnValue;
            }
        }

        foreach (var c in this.Children)
        {
            if (c.hasComponentOfType(type, crossSystems))
            {
                returnValue = true;
            }
            return returnValue;
        }

        return returnValue;
    }
    public class clsSubComponent
    {
        public string OptType;
        public string Path;
        public Dictionary<string, List<clsProductAttribute>> Attributes;

        public bool hasAttribute(string code, object value, bool Wildcard)
        {
            decimal dec = new decimal();
            return Attributes.ContainsKey(code) && Attributes[code].Where(attr => decimal.TryParse(value.ToString(), out dec) && attr.NumericValue == dec || (attr.Translation != null && StringType.StrLike(attr.Translation.text(English), value.ToString(), CompareMethod.Binary))).Count() > 0;
        }
    }
    public List<clsSubComponent> listComponents(bool crossSystems)
    {
        List<clsSubComponent> returnValue = default(List<clsSubComponent>);
        returnValue = new List<clsSubComponent>();
        //crossystems is not yet implemented
        if (this.Branch.Product.i_Attributes_Code.ContainsKey("optType"))
        {
            returnValue.Add(new clsSubComponent() { Path = this.Path, OptType = this.Branch.Product.i_Attributes_Code("optType")[0].Translation.text(English), Attributes = this.Branch.Product.i_Attributes_Code });
        }

        foreach (var c in this.Children)
        {
            if (c.validate)
            {
                returnValue.AddRange(c.listComponents(crossSystems));
            }
        }

        return returnValue;
    }

    /// <summary>Counts products under each OPG, by product type - Recursively populates a dictionary of flexOPG>ProductType>clsMinMaxTotalUsed  - for this quoteItem(and it's descendants)</summary>
    ///<remarks>ClsMinMaxTotalUsed carries the 4 variables used later by SetFlexRebate()</remarks>
    int sysFlexID = 0;
    internal void QualifyFlex(Dictionary<clsProduct, Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>> qfdic, clsRegion region, bool flexQualifiedSystem, int sysFlexID)
    {

        if (!(this.Branch == null)) //The RootItem has no branch
        {

            if (Branch.Product.isSystem(this.Path) && Branch.Product.OPGflexLines.Count > 0) //Or qfdic.Count > 0 Then -- ML nobbled this as it says "qualifying system" but wasnt checking it was a system?
            {
                System.Boolean t = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.AppliesToRegion(region) && l.FlexOPG.OPGSysType == this.Branch.Product.ProductType.Code select l).ToList();
                if (t.Count > 0)
                {
                    clsTranslation tlqualifies = iq.AddTranslation("Qualifying System", English, "VM", 0, null, 0, false);
                    this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, null, tlqualifies, "", 0, 0, ",".Split(',')));
                    flexQualifiedSystem = true;
                    sysFlexID = System.Convert.ToInt32(t.First.FlexOPG.ID);
                }
            }

            System.Object op = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.AppliesToRegion(region) select l).ToList();
            if (op.Count > 1)
            {
                op = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.AppliesToRegion(region) && l.FlexOPG.ID == sysFlexID select l).ToList();
            }


            foreach (var flexLine in op)
            {

                //If Me.IsPreInstalled = False The
                clsProductType ProductType = this.Branch.Product.ProductType;
                if (flexLine.isCurrent && flexLine.FlexOPG.isCurrent)
                {
                    if (flexLine.FlexOPG.AppliesToRegion(region))
                    {
                        Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>> qflDic = default(Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>);
                        clsProduct sysProduct = new clsProduct();
                        if (this.Branch.Product.isSystem(this.Path))
                        {
                            sysProduct = this.Branch.Product;
                        }
                        else if (this.Parent != null && this.Parent.Branch != null && this.Parent.Branch.Product != null && this.Parent.Branch.Product.isSystem(this.Path))
                        {

                            sysProduct = this.Parent.Branch.Product;
                        }
                        if (flexQualifiedSystem)
                        {
                            if (!qfdic.ContainsKey(sysProduct))
                            {
                                qflDic = new Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>();
                                qfdic.Add(sysProduct, qflDic);
                            }
                            qflDic = qfdic(sysProduct);
                            if (!qflDic.ContainsKey(flexLine.FlexOPG))
                            {
                                Dictionary<clsProductType, clsMinMaxTotalUsed> ptdic = new Dictionary<clsProductType, clsMinMaxTotalUsed>();
                                qflDic.Add(flexLine.FlexOPG, ptdic);
                            }

                            if (this.IsPreInstalled == false)
                            {


                                if (!qflDic[flexLine.FlexOPG].ContainsKey(ProductType) && flexQualifiedSystem)
                                {
                                    clsFlexRule rule = flexLine.FlexOPG.getRule(ProductType);
                                    if (rule == null)
                                    {
                                        //No requried quantities on this Flexlines,Products,productType
                                        qflDic[flexLine.FlexOPG].Add(ProductType, new clsMinMaxTotalUsed(1, 9999, 0, 0, true));

                                    }
                                    else
                                    {
                                        qflDic[flexLine.FlexOPG].Add(ProductType, new clsMinMaxTotalUsed(rule.min, rule.max, 0, 0, rule.optionalRule));

                                    }
                                    dynamic with_1 = qflDic[flexLine.FlexOPG][ProductType];
                                    with_1.Total += this.DerivedQuantity(); //<This is important !
                                }

                            }
                        }
                    }
                }
                //End If
            }
        }

        foreach (var child in this.Children)
        {
            child.QualifyFlex(qfdic, region, flexQualifiedSystem, sysFlexID);
        }

    }

    internal void FlexCalculations(Dictionary<clsProduct, Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>> qfdic, clsRegion region, int sysFlexID, ref bool validationSuccess, Dictionary<clsProductType, ClsValidationMessage> qualifyingProductTypes)
    {
        bool flexQualifiedSystem;
        Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>> qflDic = default(Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>);
        if (!(this.Branch == null)) //Rootitem has no branch
        {
            //if the branch is a system then check if it has OPG flexline for the region
            if (Branch.Product.isSystem(this.Path) && Branch.Product.OPGflexLines.Count > 0)
            {
                System.Boolean t = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.AppliesToRegion(region) && l.FlexOPG.OPGSysType == this.Branch.Product.ProductType.Code select l).ToList();
                if (t.Count > 0)
                {
                    // The system qualifies for a flex
                    clsTranslation tlqualifies = iq.AddTranslation("Qualifying System", English, "VM", 0, null, 0, false);
                    this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, null, tlqualifies, "", 0, 0, ",".Split(',')));
                    flexQualifiedSystem = true;
                    sysFlexID = System.Convert.ToInt32(t.First.FlexOPG.ID);
                    clsFlexLine sysFlexline = t.First;
                    clsProduct sysProduct = this.Branch.Product;


                    if (!qfdic.ContainsKey(sysProduct))
                    {
                        qflDic = new Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>>();
                        qfdic.Add(sysProduct, qflDic);
                    }

                    qflDic = qfdic(sysProduct);
                    if (!qflDic.ContainsKey(sysFlexline.FlexOPG))
                    {
                        Dictionary<clsProductType, clsMinMaxTotalUsed> ptdic = new Dictionary<clsProductType, clsMinMaxTotalUsed>();
                        qflDic.Add(sysFlexline.FlexOPG, ptdic);
                    }

                    // Generate the system validation rules
                    foreach (var flexRules in sysFlexline.FlexOPG.Rules.Values)
                    {

                        if (!qflDic[sysFlexline.FlexOPG].ContainsKey(flexRules.ProductType))
                        {
                            qflDic[sysFlexline.FlexOPG].Add(flexRules.ProductType, new clsMinMaxTotalUsed(flexRules.min, flexRules.max, 0, 0, flexRules.optionalRule));
                        }
                    }
                    // Now check which item is missing from the quote
                    string strflexLineProductTypes = "";
                    getAllproductTypes(ref strflexLineProductTypes, System.Convert.ToInt32(sysFlexline.FlexOPG.ID));
                    List<clsProductType> includedproductTypes = new List<clsProductType>();
                    foreach (var productTypeRule in qflDic[sysFlexline.FlexOPG])
                    {
                        clsProductType productType = productTypeRule.Key;
                        clsMinMaxTotalUsed systemRule = productTypeRule.Value;
                        if (!systemRule.optionalRule)
                        {
                            if (!strflexLineProductTypes.Contains(System.Convert.ToString(productType.Code)))
                            {
                                if (productTypeRule.Value.Min > 0)
                                {
                                    string[] v = new string[2];
                                    v[0] = System.Convert.ToString(productType.Translation.text(English));
                                    v[1] = (productTypeRule.Value.Min).ToString();
                                    clsTranslation text = default(clsTranslation);
                                    text = iq.AddTranslation("No Qualifying %1 (Min required %2)", English, "VM", 0, null, 0, false);
                                    this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesntQualify, text, text, "", 0, 0, v));
                                    validationSuccess = false;
                                }
                            }
                            else
                            {
                                includedproductTypes.Add(productType);

                                // If we qualify on the warranty, explicitly display a summary message
                                if (productType.Code == "wty")
                                {
                                    string[] v = new string[2];
                                    v[0] = System.Convert.ToString(systemRule.Min.ToString());
                                    v[1] = System.Convert.ToString(productType.Translation.text(English));
                                    clsTranslation text = default(clsTranslation);
                                    text = iq.AddTranslation("%1 Qualifying %2", English, "VM", 0, null, 0, false);
                                    this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, text, text, "", 0, 0, v));
                                }

                            }
                        }
                    }


                }
                else
                {
                    // No system flex line

                }
                //
            }

            //get flex lines for a region

            System.Boolean op = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.ID == sysFlexID select l).ToList();

            if (op.Count == 1)
            {
                //There should be only one flexline per country per system type
                clsFlexLine flexLine = op.First;

                if (flexLine.isCurrent && flexLine.FlexOPG.isCurrent)
                {
                    if (this.IsPreInstalled == false)
                    {
                        //Need to get the system product from dictionary
                        clsProduct sysProduct = new clsProduct();
                        if (this.Branch.Product.isSystem(this.Path))
                        {
                            sysProduct = this.Branch.Product;
                        }
                        else if (this.Parent != null && this.Parent.Branch != null && this.Parent.Branch.Product != null && this.Parent.Branch.Product.isSystem(this.Parent.Path))
                        {
                            sysProduct = this.Parent.Branch.Product;
                        }

                        if ((!(sysProduct == null)) && qfdic.ContainsKey(sysProduct))
                        {
                            qflDic = qfdic(sysProduct);
                            clsProductType currentBranchProductType = this.Branch.Product.ProductType;
                            if (qflDic.ContainsKey(flexLine.FlexOPG))
                            {
                                //This opg dictionary should already be created with system opg .
                                // if the opg is different that means we shouln't count it for our flex calculations
                                if (!qflDic[flexLine.FlexOPG].ContainsKey(currentBranchProductType))
                                {
                                    clsFlexRule rule = flexLine.FlexOPG.getRule(currentBranchProductType);
                                    if (rule == null)
                                    {
                                        //No requried quantities on this Flexlines,Products,productType
                                        qflDic[flexLine.FlexOPG].Add(currentBranchProductType, new clsMinMaxTotalUsed(1, 9999, 0, 0, true));
                                    }
                                    else
                                    {
                                        qflDic[flexLine.FlexOPG].Add(currentBranchProductType, new clsMinMaxTotalUsed(rule.min, rule.max, 0, 0, rule.optionalRule));
                                    }

                                }
                                dynamic with_1 = qflDic[flexLine.FlexOPG][currentBranchProductType];
                                with_1.Total += this.DerivedQuantity(); //<This is important !
                                if (with_1.Total < with_1.Min)
                                {
                                    clsTranslation text = default(clsTranslation);
                                    text = iq.AddTranslation("%1 required for rebate", English, "VM", 0, null, 0, false);
                                    this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesntQualify, text, text, "", 0, 0, ((with_1.Min).ToString() + " * " + currentBranchProductType.Translation.text(English)).Split('*')));
                                    validationSuccess = false;
                                }
                                else
                                {
                                    if (!with_1.optionalRule && this.Branch.Product.isSystem(this.Path) == false && this.IsPreInstalled == false)
                                    {
                                        if (!qualifyingProductTypes.ContainsKey(currentBranchProductType))
                                        {
                                            clsTranslation text = default(clsTranslation);
                                            text = iq.AddTranslation("%1 Qualifying %2", English, "VM", 0, null, 0, false);
                                            ClsValidationMessage validationMessage = new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.DoesQualify, text, text, "", 0, 0, ((with_1.Total).ToString() + " * " + currentBranchProductType.Translation.text(English)).Split('*'));
                                            //Me.Msgs.Add(validationMessage)
                                            qualifyingProductTypes.Add(currentBranchProductType, validationMessage);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        foreach (var child in this.Children)
        {
            child.FlexCalculations(qfdic, region, sysFlexID, validationSuccess, qualifyingProductTypes);
        }

    }

    /// <summary>Counts qualifying options under each system in the basket, by OPG - Recursively populates a dictionary of System>OPGRef>Qty  </summary>
    public void QualifyAvalanche(ref clsProduct system, ref Dictionary<clsProduct, Dictionary<ClsAvalancheOPG, int>> qdic, clsRegion region)
    {

        if (!(this.Branch == null)) //The RootItem has no branch
        {
            if (this.Branch.Product.isSystem(this.Path))
            {
                system = this.Branch.Product;
                if (!qdic.ContainsKey(system))
                {
                    qdic.Add(system, new Dictionary<ClsAvalancheOPG, int>());
                }
            }
            else
            {
                if (this.IsPreInstalled == false)
                {
                    clsProduct opt = this.Branch.Product;
                    if (system != null) //need to check if we're inside a system yet (as options can be orphans)
                    {
                        foreach (var avOPG in system.AvalancheOPGs.Values)
                        {
                            string prodRef = "";
                            clsProductAttribute pra = default(clsProductAttribute);
                            if (opt.i_Attributes_Code.ContainsKey("ProdRef")) //only check options with a prodref
                            {
                                pra = opt.i_Attributes_Code("ProdRef")[0];
                                prodRef = System.Convert.ToString(pra.Translation.text(English));
                                if (!qdic(system).ContainsKey(avOPG))
                                {
                                    qdic(system).Add(avOPG, 0);
                                }
                                if (avOPG.getAvalancheOptions(prodRef, 0, DateTime.Now, region).Count() > 0) //returns the avalancheOptions (containing % rebate information)  is for the sepcified prodref,qty..etc (which are all optional)
                                {
                                    qdic(system)[avOPG] += this.DerivedQuantity();
                                }
                            }
                        }
                    }
                }

            }
        }

        foreach (var child in this.Children)
        {
            child.QualifyAvalanche(system, qdic, region);
        }

    }

    /// <summary>recursively sets the rebate (and OPG) on quoteitems holding qualifying options - according to the avalanche offers available on the system in which they reside</summary>
    ///<remarks>qDic contains a the number of qualifying options,  by opg, by systems (in the basket) - and was built by QualifyAvalanche()</remarks>
    public void SetAvalancheRebate(clsProduct system, Dictionary<clsProduct, Dictionary<ClsAvalancheOPG, int>> qDic)
    {

        string prodRef = "";
        if (!(this.Branch == null)) //the quote's root item has no branch (and is the only quote item like it)
        {
            if (this.Branch.Product.isSystem(this.Path))
            {
                this.rebate = 0;
                system = this.Branch.Product;
            }
            else
            {
                //i'm an option
                if (!this.IsPreInstalled)
                {
                    this.rebate = 0;
                    if (this.Branch.Product.i_Attributes_Code.ContainsKey("ProdRef"))
                    {
                        prodRef = System.Convert.ToString(this.Branch.Product.i_Attributes_Code("ProdRef")[0].Translation.text(English));

                        this.OPG = new nullableString();
                        if (system != null) //we may not have 'hit' a system yet (options can be orhpans!)
                        {
                            foreach (var Av in system.AvalancheOPGs.Values) //these are the offers
                            {
                                List<clsAvalancheOption> opt = Av.getAvalancheOptions(prodRef, qDic(system)[Av], DateTime.Now, this.quote.BuyerAccount.SellerChannel.Region);
                                if (opt.Count > 0)
                                {
                                    clsPrice listprice = this.Branch.Product.ListPrice(this.quote.BuyerAccount);

                                    if (listprice == null)
                                    {
                                        this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("No list price available - to calculate Avalanche Rebate", English, "VM", 0, null, 0, false), iq.AddTranslation("%1 - No list price", English, "VM", 0, null, 0, false), "", 0, 0, Strings.Split("")));
                                    }
                                    else if (listprice.Price.value == 0)
                                    {
                                        this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("list price was 0 unable - to calculate Avalanche Rebate", English, "VM", 0, null, 0, false), iq.AddTranslation("%1 - Zero list price", English, "VM", 0, null, 0, false), "", 0, 0, Strings.Split("")));
                                    }
                                    else
                                    {
                                        this.rebate = System.Convert.ToDecimal((System.Convert.ToDecimal(opt.First.LPDiscountPercent)) / 100 * listprice.Price.value);
                                        if (this.rebate == 0)
                                        {
                                            this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Flex, EnumValidationSeverity.amberalert, iq.AddTranslation("Avalanche Rebate was 0", English, "VM", 0, null, 0, false), iq.AddTranslation("%1 - Zero Avalanche rebate", English, "VM", 0, null, 0, false), "", 0, 0, Strings.Split("")));
                                        }
                                        else
                                        {
                                            this.OPG = new nullableString(Av.OPGref);
                                        }
                                    }
                                }
                                else
                                {

                                }
                            }
                        }
                    }
                }
            }
        }

        //and recurse... for all children
        foreach (var child in this.Children)
        {
            child.SetAvalancheRebate(system, qDic);
        }

    }



    /// <summary>recursively sets the rebate (and OPG) on quoteItems holding qualifying products - according to the flexOPG offers available</summary>
    internal void SetFlexRebate(Dictionary<clsFlexOPG, Dictionary<clsProductType, clsMinMaxTotalUsed>> qfdic, bool rulesQualified, int systemopgid, bool systemTotalQualified, ref decimal totalrebate)
    {

        //qfDic contains the number of total number of products in the basket,  by opg, by ProductType and was built by QualifyFlex()
        //each instance of clsMinMaxTotalUsed - tells us the Total in the baseket, min required ,max, rebatable of each product type under each OPG

        clsProduct product = default(clsProduct);

        clsRegion region = this.quote.BuyerAccount.BuyerChannel.Region;
        if (!(this.Branch == null)) //the quote's root item has no branch (and is the only quote item like it)
        {
            if (!this.IsPreInstalled)
            {

                this.OPG = new nullableString();
                product = this.Branch.Product;
                if (product.isSystem(this.Path) && product.OPGflexLines.Count > 0)
                {
                    rulesQualified = true;
                    //Now add all the rules associated with the system
                    if (product.OPGflexLines.Values.Count > 0)
                    {
                        System.Boolean t = (from l in product.OPGflexLines.Values where l.FlexOPG.AppliesToRegion(region) && l.FlexOPG.OPGSysType == product.ProductType.Code select l).ToList();
                        clsFlexLine sysFlexline = product.OPGflexLines.Values.First;
                        if (t.Count > 0)
                        {
                            sysFlexline = t.First;
                        }
                        systemopgid = System.Convert.ToInt32(sysFlexline.FlexOPG.ID);

                    }

                }

                if (systemTotalQualified)
                {

                    System.Object op = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.AppliesToRegion(this.quote.BuyerAccount.BuyerChannel.Region) select l).ToList();
                    if (op.Count > 1)
                    {
                        op = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.AppliesToRegion(this.quote.BuyerAccount.BuyerChannel.Region) && l.FlexOPG.ID == systemopgid select l).ToList();
                    }

                    foreach (var flexLine in op)
                    {

                        // If Not (flexLine.Product.isSystem) Then
                        //If qfdic.ContainsKey(flexLine.FlexOPG) Then 'we may have removed the OPG becuase we didnt have enough options

                        if (flexLine.FlexOPG.AppliesToRegion(this.quote.BuyerAccount.BuyerChannel.Region))
                        {

                            if (flexLine.isCurrent && flexLine.FlexOPG.isCurrent)
                            {
                                //does the basket contain a product of this flexlines,products type
                                clsMinMaxTotalUsed q = default(clsMinMaxTotalUsed);
                                if (systemopgid == flexLine.FlexOPG.ID)
                                {
                                    if (qfdic.ContainsKey(flexLine.FlexOPG) && qfdic(flexLine.FlexOPG).ContainsKey(flexLine.Product.ProductType))
                                    {
                                        q = qfdic(flexLine.FlexOPG)[flexLine.Product.ProductType];
                                        clsFlexRule r = null;
                                        r = flexLine.FlexOPG.getRule(flexLine.Product.ProductType);
                                        if (r != null)
                                        {
                                            // Make sure that the values are from rules if it exists
                                            q.Min = r.min;
                                            q.Max = r.max;
                                        }
                                    }
                                    else
                                    {
                                        //we don't have a product of this flexlines product type  in the basket yet
                                        clsFlexRule r = null;
                                        r = flexLine.FlexOPG.getRule(flexLine.Product.ProductType);
                                        if (r == null)
                                        {
                                            q = new clsMinMaxTotalUsed(1, 9999, 0, 0, true);
                                        }
                                        else
                                        {
                                            q = new clsMinMaxTotalUsed(r.min, r.max, 0, 0, r.optionalRule);
                                        }
                                    }
                                    if (q.Total >= q.Min)
                                    {

                                        // If Me.OPG.value IsNot DBNull.Value Then Stop 'TODO - report an error that a products is qualifying for more than One opg
                                        this.OPG = new nullableString(flexLine.FlexOPG.OPGRef);
                                        int remainingQuota = System.Convert.ToInt32(q.Max - q.Used); //how many more units of this product can attract the rebate

                                        int dq = this.DerivedQuantity();
                                        if (dq < remainingQuota)
                                        {
                                            this.rebate = dq * flexLine.rebate;
                                        }
                                        else
                                        {
                                            this.rebate = remainingQuota * flexLine.rebate;
                                        }
                                        if (!rulesQualified)
                                        {
                                            this.rebate = 0;
                                        }
                                        if (this.rebate > 0 && !(this.Parent == null))
                                        {
                                            Update(false);
                                            totalrebate += this.rebate;
                                        }
                                    }
                                    else
                                    {

                                        this.rebate = 0; //IMPORTANT (otherwise rebates stay on the item) when it 'unqualifies'
                                    }
                                }

                            }
                        }
                        //  End If
                    }
                }
            } //new
        }

        //and recurse... for all children
        foreach (var child in this.Children)
        {
            child.SetFlexRebate(qfdic, rulesQualified, systemopgid, systemTotalQualified, totalrebate);
        }

    }


    public void Flex(int qty, bool absolute)
    {

        //branch As clsBranch, ByVal path As String, ByVal quote As clsquote,
        //if absolute is true - it sets the quantity to (otherwise changes it by qty)

        //The quote contained an item of this product - not preinstalled (posisbly another instance of a product that was originally preinstalled)
        if (absolute)
        {
            //Dim preInstalledOfThis As clsquoteItem = quote.RootItem.FindRecursive(path$, True) 'dangerous
            //Dim subtract As Integer
            //If preInstalledOfThis.IsPreInstalled Then subtract = preInstalledOfThis.Quantity Else subtract = 0
            if (this.IsPreInstalled)
            {
                Interaction.Beep();
            }
            this.Quantity = qty; //- subtract
        }
        else
        {
            object quan = this.Branch.Quantities.Where(q => this.quote.BuyerAccount.BuyerChannel.Region.Encompasses(q.Value.Region)).FirstOrDefault;
            if (quan.Value != null && quan.Value.MinIncrement != 0)
            {
                this.Quantity += System.Convert.ToInt32(quan.Value.MinIncrement);
            }
            else
            {
                this.Quantity += qty;
            }

            if (this.Quantity < 0)
            {
                this.Quantity = 0;
            }
        }

        //Remove this item if we just flexed/set its quantity to zero
        if (this.Quantity < 0)
        {
            Interaction.Beep();
        }

        if (this.Quantity == 0)
        {
            if (this == this.quote.RootItem)
            {
                Interaction.Beep();
            }
            else
            {
                this.Parent.Children.Remove(this);
            }
        }

    }

    private PlaceHolder FlexButtons(bool AllowFlexUp)
    {

        PlaceHolder ph = new PlaceHolder();

        //Javascript function declaration function flex(branchID, path, qty){

        //Flex Up button
        if (this.FlexButtonState != EnumHideButton.Up && this.FlexButtonState != EnumHideButton.Both)
        {
            if (AllowFlexUp)
            {
                ph.Controls.Add(this.FlexButton("quoteTreeFlexUp", System.Convert.ToInt32(+1), System.Convert.ToString(Xlt("Add one", quote.BuyerAccount.Language)), "../images/navigation/plus.png"));
            }
        }

        //Flex down button
        if (this.FlexButtonState != EnumHideButton.Down && this.FlexButtonState != EnumHideButton.Both)
        {
            ph.Controls.Add(this.FlexButton("quoteTreeFlexDown", -1, System.Convert.ToString(Xlt("Remove one", quote.BuyerAccount.Language)), "../images/navigation/minus.png"));
        }

        //Remove from/add back to Validation
        if (this.IsPreInstalled)
        {
            if (this.validate)
            {
                ph.Controls.Add(this.FlexButton("quoteTreeFlexDown", -1, System.Convert.ToString(Xlt("Remove from validation", quote.BuyerAccount.Language)), "../images/navigation/cross.png"));
            }
            else
            {
                ph.Controls.Add(this.FlexButton("quoteTreeFlexUp", System.Convert.ToInt32(+1), System.Convert.ToString(Xlt("Include in validation", quote.BuyerAccount.Language)), "../images/navigation/tick.png"));
            }
        }

        return ph;

    }

    private Image FlexButton(string cssClass, int qty, string toolTip, string imageURL)
    {
        Image returnValue = default(Image);

        //returns an individual flex button (+ or -) - or a a 'remvove from/add to validation' button (on preinstalled options)
        returnValue = new Image();

        returnValue.ImageUrl = imageURL;
        returnValue.ToolTip = toolTip;
        returnValue.CssClass = cssClass + " quoteTreeButton";

        if (!this.quote.Locked)
        {
            returnValue.Attributes("onclick") = "burstBubble(event);flex(\'" + this.Path + "\'," + (qty).ToString() + "," + System.Convert.ToString(this.ID) + "," + this.SKUVariant.ID + ");";
            returnValue.ToolTip = toolTip;
        }
        else
        {
            string text = System.Convert.ToString(Xlt(PleaseCreateNewVersionToChangeQuote, quote.BuyerAccount.Language));
            returnValue.Attributes.Add("onmousedown", string.Format("burstBubble(event); setupanddisplaymsg(\'{0}\',\'{1}\');return(false);", text, this.Path));



        }

        return returnValue;
    }
    public bool Compatible(object path)
    {

        //checks wether the product with the path - path$ - is compatible with (an option for) this item

        if ((string)path == this.Path)
        {
            return false; //explicitly stop things being compatible with themselves ! (otherwise systems add as a nested cascase)
        }

        if (Strings.Left(System.Convert.ToString(path), this.Path.Length) == this.Path)
        {
            return true;
        }
        else
        {
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

    public void ValidateSlots2(Dictionary<clsSlotType, clsSlotSummary> dicSlots, bool ForGives) //pair contains the running used total, and the total available to this point
		{
			
			//Validates this item and all of its children
			//by recursing the tree of the quoteitems
			//Populates and then manipulates the dicSlots dicitonary as the tree (of quote items) is walked
			//The dictionary therefore contains the slots available (of each type) at each point in the tree - wherever it goes negative - there are isufficient slots and validation fails.
			
			if (this.validate) //am i excluded from validation ? (pre-installed items can be removed from validation in the UI)
			{
				if (this.IsPreInstalled)
				{
					this.FlexButtonState = EnumHideButton.Down;
				}
				else
				{
					this.FlexButtonState = EnumHideButton.Neither;
				}
				int qs = 0;
				if (!(this.Branch == null)) //skip the rootitem - it's just a placeholder for the top level items (typically systems)
				{
					List<clsSlot> sif;
					
					foreach (var slot in this.Branch.slotsInForce(Path))
					{
						object pn = this.Branch.Product.DisplayName(English); //For watching/debugging
						if (slot.NonStrictType.MajorCode.ToLower() == "wty")
						{
							int a = 9;
						}
						if (!dicslots.ContainsKey(slot.NonStrictType))
						{
							dicslots.Add(slot.NonStrictType, new clsSlotSummary(0, 0, 0, 0));
						}
						
						int dq = this.DerivedQuantity();
						
						//GREG OVERRIDE - COUNT AND VALIDATE SLOTS PER SERVER - REMOVE TO GO BACK TO MULTIPLIED.
						if (this.Branch.Product.isSystem(this.Path))
						{
							dq = 1;
						}
						else
						{
							dq = this.Quantity;
						}
						//</gregness>
						
						qs = System.Convert.ToInt32(slot.numSlots * dq);
						if (qs > 0)
						{
							if (ForGives)
							{
								dicSlots(slot.NonStrictType).Given += qs;
								
							}
						}
						else
						{
							if (!ForGives)
							{
								//Find if there is a fallback WITH SPACE
								int toAllocate = System.Convert.ToInt32(- qs);
								if (slot.NonStrictType.Fallback != null && slot.NonStrictType.Fallback.Count > 0)
								{
									foreach (var fbs in slot.NonStrictType.Fallback.Values)
									{
										if (dicslots.ContainsKey(fbs))
										{
											if (dicSlots(fbs).Given > dicSlots(fbs).taken)
											{
												System.Int32 theseslots = toAllocate;
												if (toAllocate > (dicSlots(fbs).Given - dicSlots(fbs).taken))
												{
													theseslots = System.Convert.ToInt32(dicSlots(fbs).Given - dicSlots(fbs).taken);
												}
												dicSlots(fbs).taken += theseslots;
												dicSlots(slot.NonStrictType).taken += theseslots; //These two lines are simply in to get the count correct in the error message...
												dicSlots(slot.NonStrictType).Given += theseslots;
												if (this.IsPreInstalled)
												{
													dicSlots(fbs).PreInstalledTaken += theseslots;
												}
												toAllocate = toAllocate - theseslots;
												if (toAllocate == 0)
												{
													break;
												}
											}
										}
									}
								}
								if (toAllocate > 0)
								{
									dicSlots(slot.NonStrictType).taken += toAllocate; //takes slots are negative - so we subtract (to add)
									if (this.IsPreInstalled)
									{
										dicSlots(slot.NonStrictType).PreInstalledTaken += toAllocate;
									}
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
						
						if (slot.numSlots < 0 && !ForGives) //this option TAKES slots'
						{
                            string[] majorCodes =  {"MEM", "HDD", "CPU", "PSU"};
							if (this.Branch.Product.i_Attributes_Code.ContainsKey("capacity") && majorCodes.Contains(slot.NonStrictType.MajorCode.ToUpper()))
							{
								
								//options like HDD's or MEM have a Capacity ...
								// (say 100Mb... which has nothing really to do with slots - but we want to sum the total capacities (for storage, memory etc)
								float capacity = System.Convert.ToSingle(this.Branch.Product.i_Attributes_Code("capacity")[0].NumericValue);
								dicSlots(slot.NonStrictType).TotalCapacity += this.Quantity * capacity;
								//dicSlots(slot.NonStrictType).TotalCapacity += dq * capacity
								if (slot.NonStrictType.MajorCode.ToUpper() == "CPU")
								{
									dicSlots(slot.NonStrictType).TotalCapacity = capacity;
								}
								if (slot.NonStrictType.MajorCode.ToUpper() == "PSU")
								{
									dicSlots(slot.NonStrictType).TotalRedundantCapacity = capacity * (dicSlots(slot.NonStrictType).taken - 1); //PSU's are n+1 and always have to be the same power...
								}
								if (slot.NonStrictType.MajorCode.ToUpper() == "PSU")
								{
									dicSlots(slot.NonStrictType).TotalCapacity = capacity;
								}
								
								//OK this is really ugly - we need to know the capacity Unit Gb/MB/Watt etc
								//options take slots - and have the (somehwat legacy) capacity
								dicSlots(slot.NonStrictType).CapacityUnit = this.Branch.Product.i_Attributes_Code("capacity")[0].Unit;
								if (slot.NonStrictType.MinorCode == "W")
								{
									dicSlots(slot.NonStrictType).CapacityUnit = iq.i_unit_code("W");
								}
							}
						}
					}
					
				}
				
				foreach (var item in this.Children) //These are the child quote items - NOT the children of the branch
				{
					object nm = item.Branch.Product.DisplayName(English);
					item.ValidateSlots2(dicSlots, ForGives); //Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
				}
				
			} //move this End If up (to above the for each) if you want to still validate sub options even though their parent is excluded from validation
			
		}



    public void OldValidateSlots(ref Dictionary<clsSlotType, clsSlotSummary> dicSlots) //pair contains the running used total, and the total available to this point
		{
			
			//Validates this item and all of its children
			//by recursing the tree of the quoteitems
			//Populates and then manipulates the dicSlots dicitonary as the tree (of quote items) is walked
			//The dictionary therefore contains the slots available (of each type) at each point in the tree - wherever it goes negative - there are isufficient slots and validation fails.
			
			//adding a processor might enable (give) 9 UDIMM *OR* 6 RDIMM ports
			//We need these to be multiplied by the number of processors
			//because processors are encountered before memory - the slots are 'given' before the memory 'takes' them
			//and everything works
			
			if (this.validate)
			{
				if (this.IsPreInstalled)
				{
					this.FlexButtonState = EnumHideButton.Down;
				}
				else
				{
					this.FlexButtonState = EnumHideButton.Neither;
				}
				int qs = 0;
				if (!(this.Branch == null)) //skip the rootitem - it's just a placeholder for the top level items (typically systems)
				{
					
					//Combined system and chassis slots (union)
					List<clsSlot> combinedslots = new List<clsSlot>();
					
					foreach (var slot in this.Branch.slots.Values) //a branch may have slots of more than one type - It might take Watts and Give USB's
					{
						if (slot.path == this.Path) //a
						{
							combinedslots.Add(slot);
						}
					}
					combinedslots.AddRange(this.Branch.slots.Values.ToList); //system
					
					//UGLY fix to include the (generally GIVES) slots defined on the chassis branch
					//If Me.Branch.Product.isSystem Then
					string chassispath = "";
					foreach (var cb in Branch.childBranches.Values)
					{
						if (cb.Product != null && cb.Product.ProductType.Code == "CHAS")
						{
							combinedslots.AddRange(cb.slots.Values.ToList); //chassis
							chassispath = Path +"." + cb.ID;
							break;
						}
					}
					
					foreach (var slot in combinedslots)
					{
						if (slot.path == this.Path || slot.path == chassispath || slot.path == "")
						{
							// If slot.path <> "" Then Stop
							if (!dicslots.ContainsKey(slot.Type))
							{
								// qs = slot.numSlots * Me.DerivedQuantity
								dicslots.Add(slot.Type, new clsSlotSummary(0, 0, 0, 0));
							}
							
							int dq = this.DerivedQuantity();
							qs = System.Convert.ToInt32(slot.numSlots * dq);
							if (qs > 0)
							{
								dicSlots(slot.Type).Given += qs;
							}
							else
							{
								dicSlots(slot.Type).taken -= qs; //takes slots are negative - so we subtract (to add)
								if (this.IsPreInstalled)
								{
									dicSlots(slot.Type).PreInstalledTaken -= qs;
								}
							}
							
							//If qs > 0 Then dicslots(slot.Type).Total += qs
							
							if (slot.numSlots < 0) //this option TAKES slots'
							{
                                string[] majorCodes = {"PWR", "MEM", "HDD"};
								if (this.Branch.Product.i_Attributes_Code.ContainsKey("capacity") && majorCodes.Contains(slot.Type.MajorCode.ToUpper()))
								{
									
									//options like HDD's or MEM have a Capacity ...
									// (say 100Mb... which has nothing really to do with slots - but we want to sum the total capacities (for storage, memory etc)
									float capacity = System.Convert.ToSingle(this.Branch.Product.i_Attributes_Code("capacity")[0].NumericValue);
									if (slot.Type.MajorCode == "CPU")
									{
										dicSlots(slot.Type).TotalCapacity += this.Quantity * capacity;
									}
									else
									{
										dicSlots(slot.Type).TotalCapacity = capacity;
									}
									
									//OK this is really ugly - we need to know the capacity Unit Gb/MB/Watt etc
									//options take slots - and have the (somehwat legacy) capacity
									dicSlots(slot.Type).CapacityUnit = this.Branch.Product.i_Attributes_Code("capacity")[0].Unit;
									if (slot.Type.MinorCode == "W")
									{
										dicSlots(slot.Type).CapacityUnit = iq.i_unit_code("W");
									}
									
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
				
				foreach (var item in this.Children)
				{
					//      If Not item.Branch.Product.isSystem Then 'don't cross system boudaries
					object nm = item.Branch.Product.DisplayName(English);
					item.OldValidateSlots(dicSlots); //Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
					//   End If
				}
			} //move this End If up (to above the for each) if you want to still validate sub options even though their parent is excluded from validation
			
		}

    public clsQuoteItem SystemItem()
    {

        if (this == this.quote.RootItem)
        {
            return null;
        }
        else
        {
            if (this.Branch.Product.isSystem(this.Path))
            {
                return this;
            }
            else
            {
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
    public void resolveOverFlows(clsSlotType slotType, int shortfall, clsAccount buyeraccount, ClsValidationMessage msg, List<string> errormessages, UInt64 lid, Dictionary<clsSlotType, clsSlotSummary> dicSlots)
    {

        //adds validationmessages - to resolve slot overflows

        clsTranslation tl = iq.AddTranslation("Resolve with ", English, "VM", 0, null, 0, false);

        clsBranch branch = null;
        int rq; //number of units (of the partnumber) required to resove

        clsQuoteItem systemItem = this.SystemItem();
        if (systemItem == null)
        {
            return;
        }
        Dictionary<string, int> gives = systemItem.Branch.findSlotGivers(Path, slotType, false); //the 'false' stops it incuding another system unit as a slot donor
        foreach (var pth in gives.OrderByDescending(g => g.Value).Select(j => j.Key))
        {
            branch = iq.Branches(System.Convert.ToInt32(Strings.Split(System.Convert.ToString(pth), ".").Last));
            HashSet<string> foci = new HashSet<string>(iq.sesh(lid, "foci").ToString().Split(",".ToArray));
            if (foci == null)
            {
                foci = new HashSet<string>();
            }
            if (branch.ReasonsForHide(buyeraccount, foci, Path, buyeraccount.SellerChannel.priceConfig, false, errormessages).Count() == 0 && !branch.Product.SKU.StartsWith("###"))
            {
                int remainingtakes = -1;

                foreach (var slot in branch.slots)
                {
                    //important must be slots.numSlots must be negative.
                    if (slot.Value.numSlots >= 0)
                    {
                        continue;
                    }
                    //Important if clsSlotType of NonStrictType exists.
                    if (!dicslots.ContainsKey(slot.Value.NonStrictType))
                    {
                        continue;
                    }
                    clsSlotSummary slotTypeNoneStrict = dicSlots(slot.Value.NonStrictType);
                    //Important if these checks are not in place not the incorrect path and the incorrect message is displayed
                    if ((int)(slotTypeNoneStrict.taken - slotTypeNoneStrict.Given) == shortfall)
                    {
                        double need = System.Convert.ToDouble(-((slotTypeNoneStrict.Given - slotTypeNoneStrict.taken) / slot.Value.numSlots));
                        if (need < remainingtakes | remainingtakes == -1)
                        {
                            remainingtakes = System.Convert.ToInt32(Convert.ToInt32(need));
                        }
                    }

                }
                //Could tell them exactly what they need here?? for now lets just suggest the first thing to get them started.
                rq = (Convert.ToInt32(remainingtakes * gives[pth]) > shortfall) ? (Convert.ToInt32(remainingtakes * gives[pth])) : shortfall;
                ClsValidationMessage with_2 = msg;
                with_2.ResolvePath = pth;
                with_2.ResolverGives = gives[pth] * remainingtakes;
                with_2.ResolvingQty = remainingtakes;
                with_2.resolutionMessage = tl;
                if (remainingtakes > 0)
                {
                    break; //Remove this when counting more than the best item
                }
            }
        }

    }

    public void ValidateFill(clsAccount agentAccount)
    {

        //checks that enough slots of each type are filled (accoring to the slot.requiredFill property

        if (this.Branch != null) //the root QuoteItem has no branch (it's just a placholder for the top level items in the Quote)
        {
            foreach (var slot in this.Branch.slots.Values)
            {
                if (Strings.LCase(System.Convert.ToString(slot.path)) == this.Path.ToLower() || slot.path == "")
                {
                    if (slot.requiredFill > 0)
                    {
                        if (this.CountFilledDescendants(slot.Type) < slot.requiredFill)
                        {
                            clsTranslation tl = default(clsTranslation);
                            tl = iq.AddTranslation("You must have at least %1", agentAccount.Language, "VM", 0, null, 0, false);
                            this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.amberalert, tl, tl, "", 0, 0, Strings.Split(slot.requiredFill + " " + slot.Type.Translation.text(agentAccount.Language), ",")));

                        }
                    }
                }
            }
        }

        foreach (var item in this.Children)
        {
            item.ValidateFill(agentAccount); // (agentAccount)  'Recurse (validate each child) - dicSlots is passed BYREF - so it accumulates changes made by the item.validateslots
        }

    }

    public int CountFilledDescendants(clsSlotType slottype)
    {

        int count = 0;

        foreach (var slot in this.Branch.slots.Values)
        {
            if (slot.Type == slottype)
            {
                count++;
            }
        }

        foreach (var item in this.Children)
        {
            count += System.Convert.ToInt32(item.CountFilledDescendants(slottype));
        }

        return (count);

    }

    /// <summary>Returns this Items quantity - multiplied by that of all its ancesetors </summary>
    /// <remarks>Handles the 'nestedness' of quotes - the fact hat you might be buying 2 racks  each containing 3 servers each containing 4 Drives - For the drives, this fuction gives you the 2*3*4 </remarks>
    private int DerivedQuantity()
    {
        int returnValue = 0;

        returnValue = this.Quantity;

        clsQuoteItem item = default(clsQuoteItem);
        item = this;
        while (!(item.Parent == null))
        {
            item = item.Parent;
            returnValue *= item.Quantity;
        }


        return returnValue;
    }
    public void validateExclusivity()
    {

        List<clsQuoteItem> incompatible = default(List<clsQuoteItem>);
        foreach (var ex in iq.Excludes.Values) //There is typically one exclude per family - so a few hundred at most
        {
            foreach (var o in this.Children)
            {
                // If o.validate Then  'is it removed from validation !
                if (ex.havingAnyOf.Contains(o.Branch))
                {
                    incompatible = o.siblingsBranchesIn(ex.excludesAllOf);
                    if (incompatible.Count > 0)
                    {
                        o.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.RedCross, iq.AddTranslation("Incompatbile with %1", English, "VM", 0, null, 0, false), iq.AddTranslation("%1 - Incompatbile", English, "VM", 0, null, 0, false), "", 0, 0, Strings.Split(System.Convert.ToString(incompatible[0].Branch.DisplayName(English)), ",")));
                    }
                }
                // End If
            }
        }

    }



    /// <summary>Returns any sibling QuoteItem whos branches are contained in the specified list </summary>
    public List<clsQuoteItem> siblingsBranchesIn(List<clsBranch> L)
    {
        List<clsQuoteItem> returnValue = default(List<clsQuoteItem>);

        returnValue = new List<clsQuoteItem>();

        foreach (var i in this.Parent.Children)
        {
            if (i != this)
            {
                if (i.validate) //is it removed from validation (preinstalled items can be - by minusing them in the basket)
                {
                    if (L.Contains(i.Branch))
                    {
                        returnValue.Add(i);
                    }
                }
            }
        }

        return returnValue;
    }

    public void ValidateIncrements(Dictionary<string, int> dicItemCounts, clsAccount agentaccount, List<string> errorMessages)
    {

        //Validates this items quantity (against is Min and Preferred increments)
        //recurses for all children
        clsLanguage translationLanguage = agentaccount.Language;
        if (translationLanguage == null)
        {
            translationLanguage = English;
        }

        //translationLanguage = English

        if (this.validate)
        {
            clsRegion sellerRegion = this.quote.BuyerAccount.SellerChannel.Region;

            clsQuantity quantity = null;

            if (!(this.Branch == null)) //skip the rootitem
            {
                quantity = this.Branch.LocalisedQuantity(sellerRegion, this.Path, errorMessages);

                if (quantity == null)
                {

                    Logit("No quantity limits for " + Branch.Translation.text(translationLanguage));
                    return;
                }

                int qty = 0;
                if (dicItemCounts.ContainsKey(this.Path))
                {
                    qty = dicItemCounts(this.Path); //this is the TOTAL quantity of this branch.product in the quote

                    if (quantity.PreferredIncrement != 0)
                    {
                        if (qty % quantity.PreferredIncrement != 0)
                        {

                            clsTranslation tl = default(clsTranslation);
                            tl = iq.AddTranslation("Optimum performance is achieved when %1 is installed in multiples of %2 modules %3 selected", English, "VM", 0, null, 0, false);
                            this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Upsell, EnumValidationSeverity.Exclamation, tl, iq.AddTranslation(string.Format("{0} Optimisation", quantity.Branch.Product.ProductType.Translation.text(English)), English, "", 0, null, 0, false), this.Path, 1 - (qty % quantity.PreferredIncrement) * quantity.PreferredIncrement, 0, new[] { quantity.Branch.Product.ProductType.Translation.text(translationLanguage), quantity.PreferredIncrement.ToString(), qty.ToString() }));
                        }
                    }

                    if (this.Quantity > 0 & this.Quantity < quantity.MinIncrement)
                    {
                        this.Quantity = System.Convert.ToInt32(quantity.MinIncrement);
                        clsTranslation tl = default(clsTranslation);
                        tl = iq.AddTranslation("Quantity adjusted to meet minimum of %1", English, "VM", 0, null, 0, false);
                        this.Msgs.Add(new ClsValidationMessage(enumValidationMessageType.Validation, EnumValidationSeverity.BlueInfo, tl, tl, "", 0, 0, ((quantity.MinIncrement).ToString()).Split(',')));
                    }
                }
            }

            foreach (var item in this.Children)
            {
                item.ValidateIncrements(dicItemCounts, agentaccount, errorMessages); //RECURSE
            }
        }

    }


    public void clearMessage()
    {

        //Nice easy one.. clears the (warning) message on this quoute item - and recurses for all children.
        this.Msgs = new List<ClsValidationMessage>();
        foreach (var item in this.Children)
        {
            item.clearMessage();
        }

    }

    public bool HasProduct(clsProduct Product)
    {

        if (this.Branch != null)
        {
            if (this.Branch.Product == Product)
            {
                return true;
            }

        }

        foreach (var item in this.Children)
        {
            if (item.HasProduct(Product))
            {
                return true;
            }
        }

    }


    public bool HasProductType(clsProductType ProductType)
    {
        if (this.Branch != null)
        {

            if (this.Branch.Product.ProductType == ProductType)
            {
                return true;

            }

        }
        foreach (var item in this.Children)
        {
            if (item.HasProductType(ProductType))
            {
                return true;
            }
        }
    }


    public void countSystems(ref int systems, ref int options)
    {

        if (this.Branch != null)
        {
            if (this.Branch.Product.isSystem(this.Path))
            {
                systems += this.Quantity;
            }
            else
            {
                if (!this.IsPreInstalled)
                {
                    options += this.Quantity;
                }
            }
        }

        foreach (var c in this.Children)
        {
            c.countSystems(systems, options);
        }

    }
    public void CountItems(ref Dictionary<string, int> dicItems)
    {

        //recursively (used from the rootitem)
        //builds a dictionary keyed by branch path - of the counts of every item (so we can validate the total number of items (preinstalled and added) agains their minimum/preferred increments
        //because each path is unique (but the same wether an option is pre-installed or user selected) , the validation will happen per item (sku)

        //Note: dicItems is passed BYREF.. so it will accumulate a count of all items

        if (!(this.Branch == null)) //skip the rootitem
        {

            int qty = this.Quantity;
            if (!this.validate)
            {
                qty = 0; //NOTE: items excluded from validation are not counted !
            }

            if (!dicItems.ContainsKey(this.Path))
            {
                dicItems.Add(this.Path, qty);
            }
            else
            {
                dicItems(this.Path) += qty;
            }

        }

        foreach (var item in this.Children)
        {
            item.CountItems(dicItems);
        }

    }

    public void Totalise(ref NullablePrice runningTotal, ref decimal runningRebate, bool includeMargin) // As nullablePrice
    {

        //for each item - add myself to the running total - and recurse for all children
        //TotalPrice = New nullablePrice(Me.quote.Currency)

        if (!(this == this.quote.RootItem)) //the ROOT item has no price
        {
            if (!this.BasePrice.isValid && !this.IsPreInstalled) // Also check if the item is pre installed
            {
                runningTotal.isValid = false;
                //   Exit Sub
            }
            if (this.BasePrice.isList && !this.IsPreInstalled)
            {
                runningTotal.isList = true;
            }
        }

        float mgf = 1; //Margin Factor
        if (this.Margin == 0)
        {
            this.Margin = 1;
        }
        if (includeMargin)
        {
            mgf = this.Margin;
        }


        //NB this uses the * operator overload on clsNullablePrice - which preserves the IsValid/IsList
        if (!this.IsPreInstalled && !(this == this.quote.RootItem)) // Fix for recalculation bug ignore pre installed items
        {
            runningTotal += this.BasePrice * this.DerivedQuantity() * mgf;
        }

        if (!(this == this.quote.RootItem))
        {
            if (this.Branch.Product.hasSKU) //Chasis are from HP but not List price !
            {
                if (this.SKUVariant.sellerChannel == HP && !this.IsPreInstalled)
                {
                    runningTotal.isList = true;
                }
            }
        }

        int dq = this.DerivedQuantity();
        if (this.Branch != null)
        {
            if (this.Branch.Product.isSystem(this.Path))
            {
                runningRebate = 0; //NEW - IMPORTANT
            }
        }

        runningRebate += this.rebate; //* dq  - NB: Rebates are per line (are already multiplied by the permisssible quantity)
        //  If runningRebate > 0 Then Stop

        foreach (var item in this.Children)
        {
            item.Totalise(runningTotal, runningRebate, includeMargin);
            //If runningTotal.valid = False Then Exit Sub  - keep going becuase we may still be able to give a valid total rebate

            //If item.Price.valid = False Then TotalPrice.valid = False : Exit Function
            // TotalPrice = New nullablePrice(TotalPrice.NumericValue + item.TotalPrice.NumericValue, Me.quote.BuyerAccount.Currency)
        }


    }

    public clsQuoteItem FindRecursive(int itemID)
    {
        clsQuoteItem returnValue = default(clsQuoteItem);

        //Recursively locates and returns an item from the quote by ID
        returnValue = null;

        if (this.ID == itemID)
        {
            return this;
        }

        clsQuoteItem result = default(clsQuoteItem);
        foreach (clsQuoteItem item in this.Children)
        {
            result = item.FindRecursive(itemID);
            if (!(result == null))
            {
                return result;
            }
        }

        return returnValue;
    }

    public clsQuoteItem FindRecursive(object path, bool includepreinstalled) //, IncludePreinstalled As Boolean) As clsquoteItem
    {
        clsQuoteItem returnValue = default(clsQuoteItem);

        //Checks wether a quote item has the specified path - if so, returns the item
        //works *backwards* through the children for LIFO behaviour http://en.wikipedia.org/wiki/LIFO_(computing)
        returnValue = null;

        if (this.Path.ToLower() == Strings.LCase(System.Convert.ToString(path)))
        {
            //If Me.IsPreInstalled = includepreinstalled Then Return Me - This ISNT the same as the two lines below
            if (this.IsPreInstalled && includepreinstalled)
            {
                return this;
            }
            if (this.IsPreInstalled == false)
            {
                return this;
            }
        }

        clsQuoteItem result = default(clsQuoteItem);
        //For Each item As clsquoteItem In Me.Children
        for (i = this.Children.Count - 1; i >= 0; i--) //We want to go backwards to find the last item first (when looking by path... primarliy for decrementing via the product tree - not the quote - so we end up with LIFO behaviour
        {
            result = this.Children(i).FindRecursive(path, includepreinstalled);
            if (!(result == null))
            {
                return result;
            }
        }
        //Next

        return returnValue;
    }

    public string summarise(ref int options)
    {
        string returnValue = "";

        //Gets the name of every system, and a count of its options - recursively

        returnValue = "";
        if (!(this.Branch == null)) // the quote.rootitem has no branch defined (so we must check)
        {
            dynamic with_1 = this.Branch.Product;
            if (with_1.isSystem(this.Path))
            {
                returnValue = System.Convert.ToString(with_1.SKU);
                options = 0;
            }
            else
            {
                options++;
            }
        }

        object so = null;
        foreach (var item in this.Children.OrderBy(x => x.order))
        {
            so = item.summarise(options) + "(+" + System.Convert.ToString(options) + " options)" + ","; //we must always recurse
            if (item.Branch.Product.isSystem(this.Path))
            {
                returnValue += System.Convert.ToString(so); //but only add the result for systems
            }
        }

        return returnValue;
    }

    public List<clsQuoteItem> Descendants()
    {
        List<clsQuoteItem> returnValue = default(List<clsQuoteItem>);

        returnValue = new List<clsQuoteItem>();
        returnValue.Add(this);

        foreach (var c in this.Children.OrderBy(x => x.order))
        {
            returnValue.AddRange(c.Descendants);
        }

        return returnValue;
    }

    /// <summary>
    ///returns a list of product/SKUvariant/quantity (so it doesn't matter where they pick an option from in the tree - they will be consolidated)
    /// </summary>
    /// <param name="Consolidate">Wether to consolidate identical parts (or list them as seperate quantity/partnos)</param>
    /// <param name="IncludePreinstalled"></param>
    /// <param name="Indent"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public clsFlatList Flattened(bool Consolidate, bool IncludePreinstalled, int Indent, bool quote = false) //
    {
        clsFlatList returnValue = default(clsFlatList);

        Indent++;
        returnValue = new clsFlatList();

        if (!this.IsPreInstalled || IncludePreinstalled) //We don't include preinstalled items on the 'flat' quote (Bill Of Materials) view
        {
            if (!(this.Branch == null)) //the root item (placeholder)
            {
                returnValue.items.Add(new clsFlatListItem(this, Indent, Consolidate ? this.DerivedQuantity() : this.Quantity)); //NOT quantity
                this.Indent = Indent;
            }
        }

        foreach (var item in this.Children.OrderBy(x => x.order))
        {
            //merge
            if (quote)
            {
                returnValue = MergeItems(returnValue, item.Flattened(Consolidate, IncludePreinstalled, Indent, quote), Consolidate, IncludePreinstalled);
            }
            else
            {
                returnValue = MergeItems(returnValue, item.Flattened(Consolidate, IncludePreinstalled, Indent));
            }
        }

        return returnValue;
    }

    private clsFlatList MergeItems(clsFlatList a, clsFlatList b)
    {

        //used to merge the lists of child items (of different parent systems)  together into one big, flat shopping list
        //appends/merges Dictionary b to a - extending a

        clsFlatListItem existing = default(clsFlatListItem);
        foreach (var i in b.items)
        {
            existing = a.PSV(i.QuoteItem.Branch.Product, i.QuoteItem.SKUVariant);
            if (existing == null) //the flat list didnt have one of these yet so add the product and its quantity Then
            {
                a.items.Add(i);
            }
            else
            {
                existing.Quantity += i.Quantity; //add the quantity in b to the exisiting quoute item in a (for this product)
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
    private clsFlatList MergeItems(clsFlatList a, clsFlatList b, bool consolidate, bool includePreinstalled)
    {

        //used to merge the lists of child items (of different parent systems)  together into one big, flat shopping list
        //appends/merges Dictionary b to a - extending a

        clsFlatListItem existing = default(clsFlatListItem);
        foreach (var i in b.items)
        {
            existing = a.PSV(i.QuoteItem.Branch.Product, i.QuoteItem.SKUVariant);
            if (existing == null)
            {
                a.items.Add(i);
            }
            else if (includePreinstalled && consolidate == false && i.QuoteItem.IsPreInstalled == false) //the flat list didnt have one of these yet so add the product and its quantity Then
            {
                if (a.DoesNoneInstalledExist(i.QuoteItem) == false)
                {
                    a.items.Add(i);
                }
                else
                {
                    existing.Quantity += i.Quantity; //add the quantity in b to the exisiting quoute item in a (for this product)
                }
            }
            else
            {
                existing.Quantity += i.Quantity;
            }
        }
        return a;

    }
    public string FlatListTXT(clsAccount buyeraccount)
    {
        string returnValue = "";

        //Returns a SKU tab Qty CRLF  delimited 'Bill of materials' type list (for Ingram Copy to ClipBoard)
        clsProduct Product = default(clsProduct);

        returnValue = "";
        foreach (var lineitem in quote.RootItem.Flattened(true, false, 0).items) //see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote
        {

            Product = lineitem.QuoteItem.Branch.Product;
            returnValue += Product.SKU + "\t" + lineitem.Quantity + "\r\n";

        }

        return returnValue;
    }

    public string EmailSummary(bool includePreinstalled, clsAccount BuyerAccount, clsAccount agentAccount, List<string> errorMessages, ref NullablePrice runningtotal)
    {
        string returnValue = "";

        //Returns this QuoteItem (as simple HTML div with a left margin (indent) ..and recurses for all child items (nesting Divs)

        clsProduct product = default(clsProduct);

        returnValue = "";
        clsLanguage translationLanguage = agentAccount.Language;
        if (translationLanguage == null)
        {
            translationLanguage = English;
        }

        //translationLanguage = English


        float mgf = 1; //Margin Factor
        //NB this uses the * operator overload on clsNullablePrice - which preserves the IsValid/IsList
        if (!this.IsPreInstalled) // Fix for recalculation bug ignore pre installed items
        {
            int dq = this.DerivedQuantity(); //autoadded things are not preinstalled - presinstalle din synonymous with "FIO"
            runningtotal += this.BasePrice * dq * mgf; //this total ALWAYS includes any margin applied
        }


        string th = "<th style=\'background-color:#0096D6;text-align:left;\'>";
        if (this.Branch == null) //This is the root item (it has no branch!)
        {

            returnValue += "<table cellpadding=\'3px\' style=\'font-family:arial;font-size:10pt;border-collapse:collapse;border:solid gray 1px;\'>";
            returnValue += "<tr>";
            returnValue += th + Xlt("Product type", translationLanguage) + "</th>";
            returnValue += th + Xlt("Part Number", translationLanguage) + "</th>";
            returnValue += th + Xlt("Description", translationLanguage) + "</th>";
            returnValue += th + Xlt("Quantity", translationLanguage) + "</th>";
            returnValue += th + Xlt("Unit Price", translationLanguage) + "</th>";
            returnValue += th + Xlt("Note", translationLanguage) + "</th>";

        }
        else
        {

            product = this.Branch.Product;

            //EmailSummary = "<div style='margin-left:3em;" & IIf(product.isSystem, "font-weight:bold;", "font-weight:normal;") & IIf(Me.IsPreInstalled, "font-style:italic;", "font-style:normal;") & "clear:both;'>" & vbCrLf


            //Dim border$ = "border:1px solid black;"
            //            Dim display$ = "float:left;" '"display:inline-block;"

            //If Not Me.validate Then EmailSummary &= "* "
            if (!this.IsPreInstalled || (includePreinstalled && this.IsPreInstalled))
            {

                returnValue += "<tr>";

                object ct = null; //cell type/end cell type
                object ect = null;
                ct = "<td style=\'padding-left:10px;\'>";
                ect = "</td>";
                if (product.isSystem(this.Path))
                {
                    ct = "<th style=\'text-align:left;\'>";
                }
                ect = "</th>";

                //    If Me.IsPreInstalled Then
                // EmailSummary &= "preInstalled"
                // End If

                //Product type NoteBook/Server/HardDisk Drive Etc.
                //EmailSummary &= "<div style='width:15em;" & display$ & border$ & "'>" & product.ProductType.Translation.text(s_lang) & "</div>" & vbCrLf
                returnValue += ct + product.ProductType.Translation.text(translationLanguage) + System.Convert.ToString(ect);

                //EmailSummary &= "<div style='width:10em;" & display$ & border$ & "'>" & product.sku & "</div>" & vbCrLf
                if (product.SKU.StartsWith("###"))
                {
                    returnValue += ct + "built in" + System.Convert.ToString(ect);
                }
                else
                {
                    returnValue += ct + product.SKU + System.Convert.ToString(ect);
                }


                string desc = "No description available";

                if (product.i_Attributes_Code.ContainsKey("desc"))
                {
                    desc = System.Convert.ToString(product.i_Attributes_Code("desc")[0].Translation.text(translationLanguage));
                }

                //EmailSummary &= "<div style='width:30em;" & display$ & border$ & "'>" & desc$ & "</div>" & vbCrLf
                returnValue += ct + desc + System.Convert.ToString(ect);

                //Quantity
                //EmailSummary &= "<div style='width:2em;" & display$ & border$ & "'>" & Me.Quantity & "</div>" & vbCrLf
                returnValue += ct + System.Convert.ToString(this.Quantity) + System.Convert.ToString(ect);

                //Price
                //EmailSummary &= "<div style='width:8em;" & display$ & border$ & "'>" & Me.QuotedPrice.DisplayPrice(quote.BuyerAccount).Text & "</div>" & vbCrLf

                if (this.IsPreInstalled)
                {
                    returnValue += ct + "-" + System.Convert.ToString(ect);
                }
                else
                {
                    NullablePrice PriceIncludingAnyMargin = this.BasePrice * this.Margin;
                    returnValue += ct + PriceIncludingAnyMargin.text(quote.BuyerAccount, errorMessages) + System.Convert.ToString(ect);
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
                returnValue += System.Convert.ToString(ct);
                if (this.Note.value != DBNull.Value)
                {
                    returnValue += System.Convert.ToString(this.Note.DisplayValue);
                }
                //EmailSummary &= "</div>" & vbCrLf
                returnValue += System.Convert.ToString(ect);

                returnValue += "</tr>";

                //For Each vm As ClsValidationMessage In Me.Msgs
                // UI.Controls.Add(vm.UI(BuyerAccount, agentAccount.Language))
                // Next

            }
        }

        //we *always* recurse (otherwise we'd lose options in preinstalled options) .. but only append the HTML for preinstalled items if includePreinstalled=true

        foreach (var item in this.Children.OrderBy(x => x.order))
        {
            if (!this.IsPreInstalled || includePreinstalled)
            {
                returnValue += System.Convert.ToString(item.EmailSummary(includePreinstalled, BuyerAccount, agentAccount, errorMessages, runningtotal));
            }
        }

        //EmailSummary &= "</div>" & vbCrLf
        if (this.Branch == null)
        {

            //Grand total 'may have a * for contains list price elements
            returnValue += "<tr><td></td><td></td><td></td><td></td><td> " + Xlt("TOTAL", translationLanguage) + " " + runningtotal.text(BuyerAccount, errorMessages) + (runningtotal.isList ? (" " + Xlt("Contains list price elements", translationLanguage)) : "").ToString() + "</td><td></td><tr>";
            returnValue += "</table>";
        }



        return returnValue;
    }


    public Panel FlatList(clsAccount buyerAccount, List<string> errorMessages)
    {
        Panel returnValue = default(Panel);

        //Returns a HTML Panel 'bill of materials' type consolidated view of the
        //Flatlist is called on the quotes 'root' item - it
        //Calls the consolidated() function - which recurses through all quoteitems to return a dictionary of the counts of parts, indexed by Product.

        clsLanguage translationLanguage = this.quote.AgentAccount.Language;
        if (translationLanguage == null)
        {
            translationLanguage = English;
        }

        //translationLanguage = English


        returnValue = new Panel();
        returnValue.CssClass = "flatQuotePanel";

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

        clsProduct Product = default(clsProduct);

        Label lbl = default(Label);
        Panel line = default(Panel);


        //For Each lineitem In quote.RootItem.Flattened(True, False, 0).items  'see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote
        foreach (var lineitem in this.Flattened(true, false, 0).items) //see consolidated() - recurses to provide a 'flattened', consolidated (by Product/SKU varaint) view of the quote
        {


            Product = lineitem.QuoteItem.Branch.Product;
            line = new Panel();
            returnValue.Controls.Add(line);
            line.CssClass = "flatLine";
            if (Product.isSystem(this.Path))
            {
                line.CssClass += " isSystem";
            }

            //tr = New TableRow
            //tbl.Controls.Add(tr)

            //td = New TableCell
            //tr.Controls.Add(td)
            lbl = new Label();
            lbl.CssClass = "quoteFlatQty";
            lbl.Text = lineitem.Quantity.ToString() + " "; //note.. this can be a consolidated quantity (from more than one quote line)
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

            if (Product.i_Attributes_Code.ContainsKey("MfrSKU"))
            {
                lbl.Text = Product.SKU;
            }

            if (Product.i_Attributes_Code.ContainsKey("desc"))
            {
                lbl.ToolTip = Product.i_Attributes_Code("desc")[0].Translation.text(translationLanguage);
            }

            //calls NEW internally (to create a new label)

            //td = New TableCell
            //tr.Controls.Add(td)
            NullablePrice PriceIncludingMargin = lineitem.QuoteItem.BasePrice * lineitem.QuoteItem.Margin;

            Panel pp = PriceIncludingMargin.DisplayPrice(quote.BuyerAccount, errorMessages);

            pp.CssClass += " quoteFlatPrice";
            line.Controls.Add(pp);
        }

        return returnValue;
    }

    public Panel UI(bool includePreinstalled, clsAccount BuyerAccount, clsAccount agentAccount, HashSet<string> foci, ref List<string> errorMessages, bool validationMode, UInt64 lid)
    {
        Panel returnValue = default(Panel);

        //Returns this QuoteItem (as a panel) ..and recurses for all child items (nesting panels)

        returnValue = new Panel();

        if (this.Branch != null)
        {
            if (this.Branch.Hidden)
            {
                return returnValue; //return an entirely emptt panel from hidden (chassis) branches
            }
        }

        returnValue.CssClass = "quoteItemTree";
        if (!this.validate)
        {
            returnValue.CssClass += " exVal";
        }
        if (this == quote.MostRecent)
        {
            returnValue.CssClass += " mostRecent"; //used to target the flying frame animation '~~~
        }
        if (this == quote.Cursor)
        {
            returnValue.CssClass += " quoteCursor"; //used to target the flying frame animation
        }

        returnValue.ID = "QI" + System.Convert.ToString(this.ID);
        clsProduct product = default(clsProduct);
        bool issystem = false;

        if (this == this.quote.RootItem) //AKA 'Parts bin
        {
            //this is the outermost 'root' item (where we *can* add (orphaned) options)
            //When we click on - OR THE EVENT BUBBLING REACHES the root item - we 'unlock' the cursor
            //                                                                                   note this is OUTside the IF
            //was omd
            returnValue.CssClass += " quoteRoot";


            //'THIS WAS THE 'PARTS BIN' - Which was disbaled at Gregs request /01/01/2015 - UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.ID & ",'');}"
            //The root item

            Panel qh = new Panel();
            qh.CssClass = "quoteHeader";



            Panel namepanel = new Panel();

            namepanel.Controls.Add(NewLit(Xlt("Quote", agentAccount.Language) + " " + this.quote.RootQuote.ID + "-" + quote.Version + (quote.Saved ? ("<span class=\'saved\'>(" + Xlt("saved", BuyerAccount.Language)) : ("<span class=\'draft\'>(" + Xlt("draft", BuyerAccount.Language))) + ")</span>"));
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

            if (criticalMsgs.Count == 0) // need to sort out the images for
            {
                //"<div class='q_outputs'><div class='q_save' onclick = ""burstBubble(event); quoteEvent('Save'); return false;"" title ='Save'></div> " & _
                butts.Text = butts.Text + "<div title =\'" + Xlt("Export", agentAccount.Language) + "\' class=\'q_export \' onclick = \"burstBubble(event); showMenu(" + this.quote.ID.ToString() + "); return false;\"><div id = \"exportMenu" + this.quote.ID.ToString() + "\"  class = \"submenu\" > " + "<a class=\"account\"> " + Xlt("Export Option", agentAccount.Language) + "s</a> <ul class=\"root\" >" + "<li><a onclick = \"burstBubble(event); quoteEvent(\'Excel\'); return false;\" href=\"#\">" + Xlt("Excel", agentAccount.Language) + "</a></li>" + "<li><a onclick = \"burstBubble(event);  quoteEvent(\'PDF\'); return false;\" href=\"#\">" + Xlt("PDF", agentAccount.Language) + "</a></li>" + "<li><a onclick = \"burstBubble(event);  quoteEvent(\'XML\'); return false;\" href=\"#\">" + Xlt("XML", agentAccount.Language) + "</a></li>" + "<li><a href=\"#\" onclick = \"burstBubble(event);  quoteEvent(\'XMLAdv\'); return false;\">" + Xlt("XML Advanced", agentAccount.Language) + "</a></li>" + "<li><a href=\"#\" onclick = \"burstBubble(event);  quoteEvent(\'XMLSmartQuote\'); return false;\">" + Xlt("XML SmartQuote", agentAccount.Language) + "</a></li></ul>" + " </div></div> " + "<div title =\'" + Xlt("Email", agentAccount.Language) + "\' class=\'q_email\' onclick = \"burstBubble(event); quoteEvent(\'Email\'); return false;\"></div> ";
                //& "<div class='q_excel' onclick = ""burstBubble(event); saveNote(); quoteEvent('Excel'); return false;""></div> " _
                //& "<div class='q_xml' onclick = ""burstBubble(event); saveNote(); quoteEvent('XML'); return false;""></div></div>"
            }
            else
            {
                butts.Text = butts.Text + "<div title =\'" + Xlt("Export", agentAccount.Language) + "\' class=\'q_export \' onclick = \"burstBubble(event); displayMsg(\'" + Xlt("Export not available due to validation errors", BuyerAccount.Language) + "\'); return false;\"></div>";
            }
            if (quote.Locked)
            {
                butts.Text = butts.Text + "<div title =\'" + Xlt("Create Next Version", agentAccount.Language) + "\' class=\'q_newVlocked\' onclick = \"burstBubble(event); displayMsg(\'Next version created\');  rExec(\'Manipulation.aspx?command=createNextVersion&quoteId=" + quote.ID + "\', showQuote); return false;\"></div> ";
            }
            else
            {
                if (quote.Saved)
                {
                    butts.Text = butts.Text + "<div  title =\'" + Xlt("Create Next Version", agentAccount.Language) + "\' class=\'q_newVunlocked\' onclick = \"burstBubble(event);displayMsg(\'Next version created\');  rExec(\'Manipulation.aspx?command=createNextVersion&quoteId=" + quote.ID + "\', showQuote); return false;\"></div>";
                }

                //Dim btnNextVersion As New Button
                //btnNextVersion.Text = Xlt("Create next version", agentAccount.Language)
                //btnNextVersion.ToolTip = Xlt("Creates a copy leaving the original quote intact", agentAccount.Language)
                //btnNextVersion.OnClientClick = "rExec('Manipulation.aspx?command=createNextVersion&quoteId=" & quote.ID & "', gotoTree);return false;"
                //UI.Controls.Add(btnNextVersion)

            }

            if (quote != null)
            {

                if (criticalMsgs.Count == 0 && (iq.sesh(lid, "GK_BasketURL") != null || agentAccount.SellerChannel.orderEmail != ""))
                {
                    // Dim litAddtobasket = New Literal
                    //butts.Text = butts.Text & "<div class='hpBlueButton smallfont ib'  onclick = ""burstBubble(event); saveNote(false); quoteEvent('Addbasket'); return false;"">" & Xlt("Place Order", agentAccount.Language) & "</div></div> "

                    butts.Text = butts.Text + "<div class=\'hpOrangeButton q_basket smallfont\'  onclick = \"burstBubble(event); saveNote(false); quoteEvent(\'Addbasket" + quote.Saved + "\'); return false;\">&nbsp;</div>";
                }
                else
                {
                    //butts.Text = butts.Text & "</div> "
                }

                //Dim addToBasket As Button = New Button()
                //addToBasket.Text = Xlt("Add to Basket", quote.BuyerAccount.Language)
                //addToBasket.ID = "btnAddToBasket"
                //AddHandler addToBasket.Click, AddressOf Me.addToBasket_Click

                //   qh.Controls.Add(litAddtobasket)
            }
            else
            {
                butts.Text = butts.Text + "</div>";
            }

            namepanel.Controls.Add(butts);

            Panel ih = new Panel();

            ih.CssClass = "innerHeader";


            //quoteName/Customer - Sams pop open panel - part of the export tools
            Panel qnp = new Panel();
            qnp.ID = "quotepanel";


            object qn = this.quote.Name.DisplayValue;
            if (Strings.Trim(System.Convert.ToString(qn)) == "" || Strings.Trim(System.Convert.ToString(qn)) == "-")
            {
                qn = this.quote.BuyerAccount.BuyerChannel.Name;
            }


            qnp.Controls.Add(NewLit("<div id = \'quoteText\' ><input id=\'saveQuoteName\'" + (quote.Saved ? " value=\'" + System.Convert.ToString(qn) + "\'" : "") + " type=\'text\' placeholder=\'" + Xlt("enter quote name", BuyerAccount.Language) + "\' onclick= \'burstBubble(event); return false;\'   onkeydown = \'var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode==13){return false;}\'/> " + "<input id = \'hiddenType\' type=\'hidden\' value=\'Save\' /><input id = \'hiddenName\' type=\'hidden\' value=\'" + System.Convert.ToString(qn) + "\' /><input id =\'hdnEmail\' type = \'hidden\' value =\'" + this.quote.BuyerAccount.User.Email + "\'/><input id = \'continueBtn\' type=\'button\' class=\"hpBlueButton smallfont\" style=\"margin-top:-0.75em; margin-left:0.6em;\" value = \'" + Xlt("Save", agentAccount.Language) + "\' onClick =\'burstBubble(event); continueClick();\'/><input id = \'cancelBtn\' type=\'button\' onclick=\"burstBubble(event); $(\'#saveQuoteName\').val($(\'#hiddenName\').val());$(\'#continueBtn\').val($(\'#hiddenSaveTrans\').val());$(\'#cancelBtn\').hide();$(\'#quoteText\').show();$(\'#hiddenType\').val(\'Save\');return false;\" class=\"hpBlueButton smallfont\" style=\'display:none;margin-top:-0.70em; margin-left:0.6em;\' value =\'" + Xlt("Cancel", agentAccount.Language) + "\'  />" + "<input id = \'hiddenEmailTrans\' type=\'hidden\' value =\'" + Xlt("Send Email", agentAccount.Language) + "\'  /><input id = \'hiddenSaveTrans\' type=\'hidden\' value =\'" + Xlt("Save", agentAccount.Language) + "\'  /></div>")); // removed 19/01 <input id = 'cancelBtn' type='button' class=""hpGreyButton  smallfont"" value = '" & Xlt("Cancel", agentAccount.Language) & "' style=""margin-top:-0.75em; margin-left:0.6em;"" onClick ='burstBubble(event); quoteCancel();'/>


            //Systems Options summary + validation rollup

            Panel sysOptSum = new Panel();
            sysOptSum.ID = "sysOptSumm";

            int systems = 0;
            int options = 0;
            this.countSystems(ref systems, ref options);

            string syss = "";
            if (systems > 1 | systems == 0)
            {
                syss = "systems";
            }
            else
            {
                syss = "system";
            }
            string opts = "";
            if (options > 1 | options == 0)
            {
                opts = "options";
            }
            else
            {
                opts = "option";
            }
            syss = System.Convert.ToString(Xlt(syss, agentAccount.Language));
            opts = System.Convert.ToString(Xlt(opts, agentAccount.Language));

            sysOptSum.Controls.Add(NewLit("<span class=\'sysOptCount\'>" + System.Convert.ToString(systems) + " " + syss + ", " + System.Convert.ToString(options) + " " + opts + "</span>"));

            Panel vdRollup = new Panel();
            vdRollup.CssClass = "validationRollup";
            sysOptSum.Controls.Add(this.MessageCounts(new[] { { enumValidationMessageType.Validation }, { }, { EnumValidationSeverity.DoesQualify, EnumValidationSeverity.DoesntQualify }, true, true })); //exclude flex qualification messages from the roll up

            Label lblSpace = new Label();
            lblSpace.Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
            sysOptSum.Controls.Add(lblSpace);

            Dictionary<clsScheme, int> dlp = new Dictionary<clsScheme, int>();
            this.LoyaltyPoints(ref dlp);

            int BCTotal = 0;
            string toolTip = "";

            if (iq.i_scheme_code.ContainsKey("BC"))
            {
                foreach (var scheme in iq.i_scheme_code("BC"))
                {
                    if (scheme.Region.Encompasses(agentAccount.SellerChannel.Region))
                    {
                        //We have an active region for this account
                        if (dlp.ContainsKey(scheme))
                        {
                            BCTotal += dlp[scheme];
                        }
                        toolTip = System.Convert.ToString(scheme.displayName(agentAccount.Language));
                    }
                }
            }

            if (BCTotal > 0)
            {

                string points = System.Convert.ToString(Xlt("Points", agentAccount.Language));

                Label lblpointsTitle = new Label();
                lblpointsTitle.CssClass = "BlueCarpetTitle";
                lblpointsTitle.Text = points + ": ";
                lblpointsTitle.ToolTip = points;
                sysOptSum.Controls.Add(lblpointsTitle);

                Label lblpoints = default(Label);
                lblpoints = new Label();
                lblpoints.CssClass = "BlueCarpet";
                lblpoints.Text = BCTotal.ToString();
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
            if (this.quote.RootItem.Children.Count > 1 && !(agentAccount.SellerChannel.marginMin == 0 && agentAccount.SellerChannel.marginMax == 0))
            {
                PnlGrandTotal.Controls.Add(this.MarginUI(true, System.Convert.ToBoolean(this.quote.Locked))); //whole quote margin - goes inside the header at Dans )



                ih.Controls.Add(qnp);
                ih.Controls.Add(PnlGrandTotal);
                ih.Controls.Add(sysOptSum);
                qh.Controls.Add(ih);
                returnValue.Controls.Add(qh);
            }
            else
            {
                product = this.Branch.Product;

                if (product != null) //skip chassis (and other invisible) branches
                {
                    if (this.Branch.childBranches.Count > 0)
                    {
                        //SYSTEM
                        issystem = true;
                        //this quote items' product branch has sub items and so can be targetted for options (it's a 'system')
                        returnValue.CssClass += " quoteSystem";
                        returnValue.Controls.Add(this.SystemHeader(agentAccount, BuyerAccount, ref errorMessages)); //the virtual system/rollup/total header (also contains the expand/collapse button
                        returnValue.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + System.Convert.ToString(this.ID) + ");getBranches(\'cmd=open&path=" + this.Path + "&into=tree&Paradigm=C\')};";

                    }
                    else
                    {
                        //It's an OPTION - show it in the tree (and highlight it if its clicked)
                        returnValue.CssClass += " quoteOption";

                        string From = "tree." + iq.RootBranch.ID; //Default to 'from' the root
                        if (this.SystemItem() != null)
                        {
                            From = this.SystemItem().Path; //Override with the system if its there
                        }
                        returnValue.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + System.Convert.ToString(this.Parent.ID) + ");getBranches(\'cmd=open&path=" + From + "&to=" + this.Path + "&into=tree&Paradigm=C\')};";

                    }
                    //          UI.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" & Me.Parent.ID & ");getBranches('cmd=open&path=" & from & "&to=" & Me.Path & "&into=tree')};"
                }

                if (!this.collapsed)
                {

                    //note - for Pauls benefit if you have the DIAGVIEW role .. you see ALL products int he basket

                    if (this.ShouldShowInBasket(includePreinstalled, BuyerAccount, foci))
                    {
                        returnValue.Controls.Add(this.basketLine(agentAccount, BuyerAccount, product, foci, ref errorMessages, lid));
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
            if (!this.collapsed)
            {

                Panel options = new Panel();
                Panel systems = new Panel();

                Panel addto = default(Panel);
                returnValue.Controls.Add(systems);
                returnValue.Controls.Add(options);

                string prevDisplayText = "";
                ArrayList itemsToRemove = new ArrayList();
                bool isPrevPreInstalled = false;
                bool isPreInstalled = false;
                int prevItemQuantity = 0;
                int itemQuantity;
                string path = "";
                bool isASystem;

                ArrayList prevItemQuantities = new ArrayList();
                ArrayList prevDisplayTexts = new ArrayList();
                IQ.clsQuoteItem prevItem = null;

                //' This avoids systems and only uses options. It also orders by the option name.
                foreach (var item in from c in this.Children where c.order > 2 orderby c.SKUVariant.DistiSku select c)
                {

                    path = System.Convert.ToString(item.Path);
                    isASystem = System.Convert.ToBoolean(item.Branch.Product.isSystem(path));
                    if (isASystem == false)
                    {
                        string displayText = System.Convert.ToString(item.SKUVariant.DistiSku);
                        isPreInstalled = System.Convert.ToBoolean(item.IsPreInstalled);

                        itemQuantity = System.Convert.ToInt32(item.Quantity);
                        if ((displayText == prevDisplayText) && (isPreInstalled == false) && (isPrevPreInstalled == false))
                        {
                            item.Quantity = prevItemQuantity + item.Quantity;
                            if (!(prevItem == null))
                            {
                                this.Children.Remove(prevItem);
                            }
                        }
                        prevDisplayText = displayText;
                        isPrevPreInstalled = isPreInstalled;
                        prevItemQuantity = System.Convert.ToInt32(item.Quantity);
                        prevItem = item;
                    }
                }

                //order systems first then options - so that we can group the root level options into a parts bin (single div) we can draw a box around

                foreach (var item in this.Children.Where(ch => ch.ShouldShowInBasket(includePreinstalled, BuyerAccount, foci)).GroupBy(ch => ch.Branch.Product.isSystem(ch.Path)))
                {
                    if (item.Key)
                    {
                        addto = systems;
                    }
                    else
                    {
                        addto = options;
                    }

                    //Ruined below temp with andalso False until this is ok'd
                    foreach (var i in item.GroupBy(ch => ch.Branch.Product.ProductType.Translation.text(English) + (ch.IsPreInstalled && false ? 0 : ch.ID).ToString()).OrderByDescending(ch => ch.First.Branch.Product.ProductType.Order).OrderByDescending(ch => ch.First.IsPreInstalled))
                    {
                        if (i.Count > 1 && i.First.IsPreInstalled)
                        {
                            //Add header
                            Panel panel = new Panel();
                            panel.CssClass = "quoteGroup";
                            panel.Attributes("OnClick") = "burstBubble(event);";
                            panel.Controls.Add(NewLit("<h3 style=\"outline-color:white;background:white;\">" + i.First.Branch.Product.ProductType.Translation.text(BuyerAccount.Language) + "</h3>"));
                            panel panel2 = new Panel();
                            addto.Controls.Add(panel);
                            panel.Controls.Add(panel2);

                            foreach (var qi in i)
                            {
                                if (qi.Branch != null)
                                {
                                    panel2.Controls.Add(qi.UI(includePreinstalled, BuyerAccount, agentAccount, foci, errorMessages, validationMode, lid));
                                }
                            }
                        }
                        else
                        {
                            if (!i.First.IsPreInstalled || includePreinstalled) //was me.preinstalled - which was a bug (i think) NA 12/06/2014
                            {
                                //and... recurse
                                if (i.First.Branch != null)
                                {
                                    if (!i.First.Branch.Product.isSystem(i.First.Path))
                                    {
                                        if (this == quote.RootItem)
                                        {
                                            if (this == quote.RootItem)
                                            {
                                                options.Attributes("class") = "partsBin";
                                            }
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

                if (issystem)
                {

                    returnValue.Controls.Add(this.SystemFooter(BuyerAccount, agentAccount, ref errorMessages)); //Includes flex checklist

                    if (this.ExpandedPanels.Contains(panelEnum.Spec))
                    {
                        returnValue.Controls.Add(this.specPanelOpen);
                    }
                    else
                    {
                        if (specPanelClosed != null)
                        {
                            returnValue.Controls.Add(this.specPanelClosed);
                        }
                        //UI.Controls.Add(Me.validationpanel)
                        //UI.Controls.Add(Me.PromosPanel)
                    }

                    returnValue.Controls.Add(this.ValidationPanel(BuyerAccount, agentAccount, ref errorMessages));
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

            return returnValue;
        }
    }

    //Private Shared Function Flatten(source As IEnumerable(Of clsQuoteItem)) As IEnumerable(Of clsQuoteItem)
    //    Return source.Concat(source.SelectMany(Function(p) Children.Flatten()))
    //End Function

    public IEnumerable<clsQuoteItem> GetFamily(clsQuoteItem parent)
    {
        yield return parent;
        foreach (clsQuoteItem child in parent.Children)
        {
            // check null if you must
            foreach (clsQuoteItem relative in GetFamily(child))
            {
                yield return relative;
            }
        }
    }


    private TextBox QuantityBox(clsAccount buyerAccount, clsLanguage language, List<string> errorMessages)
    {

        //Quantity
        TextBox txtqty = default(TextBox);
        txtqty = new TextBox();
        txtqty.Text = this.Quantity.ToString().Trim();
        txtqty.ID = "Q" + System.Convert.ToString(this.ID);
        txtqty.CssClass = "quoteTreeQty";
        int stk = 0;
        List<clsPrice> prices = this.Branch.Product.GetPrices(buyerAccount, buyerAccount.SellerChannel.priceConfig, iq.AllVariants, errorMessages, true);

        string stock = System.Convert.ToString(this.Branch.Product.CurrentStock(buyerAccount, stk, this.SKUVariant, errorMessages));
        if (stk < this.Quantity && !this.IsPreInstalled)
        {
            txtqty.CssClass += " outOfStock";
            txtqty.ToolTip = string.Format(Xlt("Warning: {0} in stock", language), stock);
        }

        if (this.IsPreInstalled || this.Branch.Product.hasSKU == false)
        {
            txtqty.Enabled = false;
        }
        else
        {
            if (!this.quote.Locked)
            {
                txtqty.Attributes("onclick") = "burstBubble(event); return false;";
                txtqty.Attributes("onblur") = "burstBubble(event); changeQty(\'" + txtqty.ID + "\',\'" + System.Convert.ToString(this.ID) + "\',\'\',\'\',true);return false;";
                txtqty.Attributes("onkeypress") = "Javascript: if (event.keyCode==13) {burstBubble(event); changeQty(\'" + txtqty.ID + "\',\'" + System.Convert.ToString(this.ID) + "\',\'\',\'\',true);return false; }";
            }
            else
            {
                txtqty.Enabled = false;
            }
        }
        return txtqty;

    }

    public Panel basketLine(clsAccount agentAccount, clsAccount buyerAccount, clsProduct product, HashSet<string> foci, ref List<string> errormessages, UInt64 lid)
    {

        Panel bl = new Panel();

        bool priceChange = false;
        //Dim translationLanguage As clsLanguage = agentAccount.Language
        //If translationLanguage Is Nothing Then translationLanguage = English
        //translationLanguage = English

        bl.CssClass = "basketLine";
        if (this.IsPreInstalled)
        {
            bl.CssClass += " preInstalled";
        }
        else if (!this.Branch.Product.isSystem(this.Path))
        {
            bl.CssClass += " addonItem";
        }


        //System/option name label
        Label lbl = new Label();
        if (this.Branch.Product.isSystem(this.Path))
        {
            lbl.Text = Xlt("System unit", agentAccount.Language);
            bl.CssClass += " systemLine";
        }
        else
        {
            lbl.Text = this.ShortName(agentAccount.Language, true);
            lbl.ToolTip = this.ShortName(agentAccount.Language, true);
        }


        //lbl.Text = product.ProductType.Translation.text(s_lang) & " "
        lbl.CssClass = "quoteTreeType";

        if (product.isSystem(this.Path))
        {
            lbl.Font.Bold = true;
        }
        bl.Controls.Add(lbl);

        //SKU
        lbl = new Label();

        if (Strings.Left(System.Convert.ToString(product.SKU), 3) == "###")
        {
            lbl.Text = decodeTrebbleHash(product.SKU);
        }
        else
        {
            lbl.Text = product.SKU;
            if (this.IsPreInstalled && this.Branch.Product.ProductType.Code.ToLower == "cpu")
            {
                string altSKU = System.Convert.ToString(this.Branch.Product.FirstAttributeEnglishText("altSKU"));
                if (!string.IsNullOrEmpty(altSKU)) //IF an FIO CPU has an altSKU we display it as that
                {
                    lbl.Text = altSKU;
                }
            }

            if (!string.IsNullOrEmpty(System.Convert.ToString(this.SKUVariant.Code)) && (!this.SKUVariant.Code.Equals("list", StringComparison.InvariantCultureIgnoreCase)))
            {
                lbl.Text += "#" + this.SKUVariant.Code;
            }
        }

        lbl.CssClass = "quoteTreeSKU";

        if (product.isSystem(this.Path))
        {
            lbl.CssClass += " isSystem";
        }

        //With tooltip description
        if (product.i_Attributes_Code.ContainsKey("desc"))
        {
            lbl.ToolTip = product.i_Attributes_Code("desc")[0].Translation.text(agentAccount.Language);
        }

        if (this.SKUVariant.DistiSku != product.SKU)
        {
            lbl.ToolTip += " (" + this.SKUVariant.DistiSku + ")";
        }

        if (this.Branch.Product.isFIO)
        {
            lbl.ToolTip += " *FIO";
        }

        //for debugging Watts Slots (aka powersizing)
        if (AccountHasRight(lid, "DIAGVIEW"))
        {
            Label wlbl = default(Label);
            wlbl = new Label();
            wlbl.Text = "W";
            string tt = "";
            foreach (var slot in Branch.slots.Values)
            {

                if (slot.Type.MinorCode == "W")
                {
                    if (slot.path == "" || Strings.LCase(System.Convert.ToString(slot.path)) == this.Path.ToLower())
                    {
                        if (Strings.LCase(System.Convert.ToString(slot.path)) == this.Path.ToLower() && this.Path != "")
                        {
                            tt += "*" + slot.numSlots.ToString() + "W*" + "\r\n" + tt;
                        }
                        else
                        {
                            tt += slot.numSlots.ToString() + "W " + "\r\n" + tt;
                        }
                    }
                }
            }
            wlbl.ToolTip = tt; //No translation required - this is an admin tool
            bl.Controls.Add(wlbl); //adds a 'W' in forn of the part number with a tooltip gicing wattage
        }


        bl.Controls.Add(lbl);

        //new - nick23/03/2015 - more consistent with how the visibility of BuyUI is determined elesewhere
        //Dim pc As Integer = buyerAccount.SellerChannel.priceConfig
        //Dim prices As List(Of clsPrice) = Me.Branch.Product.GetPrices(buyerAccount, pc, Me.SKUVariant, errormessages, False)


        //system unit quantities have been moved 'up' to become the 'multiplier' in the systemheader
        //which is clearer - but won't work great for racks/options for options
        bool allowflexup = false;
        if (!this.Branch.Product.isSystem(this.Path))
        {

            bl.Controls.Add(this.QuantityBox(buyerAccount, agentAccount.Language, errormessages));

            //In quote Quantity Flex Buttons - also calls validate for every item (which is expensive)
            if (this.Branch.Product.hasSKU)
            {
                //                  DONT insist it's in the feed anymore - (there's probably a list price)
                //                  it shouldn't be in the basket in the first place if there isnt !
                allowflexup = true; //Me.Branch.Product.inFeed(buyerAccount.SellerChannel)
                if (this.Branch.Product.isFIO)
                {
                    allowflexup = false; //Factory Installed Options (mostly ###'s) cannot be flexed
                }
                //If slots are maxed then stop flex up
                if (!this.SystemItem().hasRoomFor(this) && !this.Branch.Product.isSystem(this.Path))
                {
                    allowflexup = false;
                }
                bl.Controls.Add(this.FlexButtons(allowflexup));
            }

        }

        //price
        //lbl = Me.QuotedPrice.DisplayPrice(quote.BuyerAccount, errormessages)
        //lbl.CssClass = "quoteTreePrice"
        //If product.isSystem Then lbl.CssClass &= " isSystem"

        //'dislpay prices that have been confirmed in the last hour as solid or green or something
        //If Me.upToDate Then lbl.CssClass &= " upToDate" Else lbl.CssClass &= " unconfirmed"

        //this is an AJAX updateable Price (which will be updated by placePrices - such that we don't need to refresh the basket anymore !

        //Dim price As List(Of clsPrice) = New List(Of clsPrice)
        //price.Add(

        if ((allowflexup) || (this.Branch.Product.isSystem(this.Path)) || (!this.IsPreInstalled))
        {
            List<clsPrice> prices = SKUVariant.Product.Prices(buyerAccount, SKUVariant);
            // Me.Branch.Product.Prices(buyerAccount, Me.SKUVariant)
            NullablePrice oldPrice = this.BasePrice;
            Panel updateablePrice = new Panel();
            if (prices.Count == 0 && SKUVariant.Product.ListPrice(buyerAccount) != null)
            {
                //fetch HPs list price according to the buyers region and currency - NB list pricing is per country at the moment (ie. there is no EMEA list price)
                prices.Add(SKUVariant.Product.ListPrice(buyerAccount));
            }
            if (prices.Count == 0 || prices[0] == null)
            {
                System.Char m = string.Format(System.Convert.ToString(Xlt("* No price available for {0} ( {1}  variant)", agentAccount.Language)), this.Branch.Product.SKU, SKUVariant.Code);
                errormessages.Add(m);

            }
            else
            {
                Panel container = new Panel();
                container.CssClass = "quoteTreePrice";
                //'what if a price gets ajax'd in AFTER we've applied margin  ?
                //Check for Items price with the latest price and flag of price change
                List<clsPrice> currentPrice = (from p in prices where p.Price.value > 0 select p).ToList();
                if (currentPrice.Count > 0)
                {
                    foreach (var newPrice in currentPrice)
                    {
                        if (newPrice.Price.value != oldPrice.value)
                        {
                            priceChange = true;
                        }
                    }
                }

                if (priceChange)
                {
                    Literal litPriceChange = new Literal();
                    litPriceChange.Text = "<input type=\"hidden\" class =\"pricechangeitem\" id =\"prchange" + product.ID + "\"  value =\"true\" />";
                    bl.Controls.Add(litPriceChange);
                }

                if (currentPrice.Count == 0)
                {
                    updateablePrice = oldPrice.DisplayPrice(buyerAccount, errormessages);
                }
                else
                {
                    updateablePrice = prices[0].Ui(buyerAccount, this.Margin, lid);
                }
                if (this.ID == 22508)
                {
                    Literal litPriceChange = new Literal();
                    litPriceChange.Text = "<input type=\"hidden\" class =\"pricechangeitem\" id =\"prchange" + product.ID + "\"  value =\"true\" />";
                    bl.Controls.Add(litPriceChange);
                }


                container.Controls.Add(updateablePrice);
                bl.Controls.Add(container);
            }
        }

        //Preinstalled Items should not showPromo markers (flex Attach or  Blue carpet)
        if (!this.IsPreInstalled)
        {
            PlaceHolder promoMarkers = this.Branch.PromoIndicators(buyerAccount, agentAccount, this.Path, foci, true, this.IsPreInstalled, errormessages, null);
            bl.Controls.Add(promoMarkers);
        }

        if (this.rebate != 0)
        {
            Label avlabel = new Label();
            avlabel.Text = "✔";
            avlabel.CssClass += "basketAvalancheTick"; //probably needs a slight change (add a seperate *)

            NullablePrice saving = new NullablePrice(this.rebate / this.DerivedQuantity(), this.quote.Currency, false); //making it into a nullableprice allows us to get this displaprice (lable) - which does the currency/culture formatting
            avlabel.ToolTip = Xlt("Qualifies - Saving ", agentAccount.Language) + saving.text(quote.BuyerAccount, errormessages) + Xlt(" per item ", agentAccount.Language); //(" & CStr(Me.OPG.value) & ")"

            bl.Controls.Add(avlabel);
        }


        //flex buttons were here
        if (!(this == this.quote.RootItem))
        {
            bl.Controls.Add(this.AddNoteButton());
        }


        if (this.Parent.Margin != 1 | this.Margin != 1)
        {
            if (!this.IsPreInstalled && !(agentAccount.SellerChannel.marginMin == 0 && agentAccount.SellerChannel.marginMax == 0))
            {

                bl.Controls.Add(this.MarginUI(false, System.Convert.ToBoolean(this.quote.Locked)));
            }
        }


        if (!Information.IsDBNull(this.Note.value))
        {
            Panel notePanel = new Panel();
            bl.Controls.Add(notePanel);
            TextBox tb = new TextBox();
            tb.ID = "note" + System.Convert.ToString(this.ID);
            tb.Attributes("style") = "position:relative;left:1.25em;top:-.25em;background-color:#FCF0AD;color:black;width:23em;border:none;";
            tb.Attributes("onclick") = "burstBubble(event);"; //stops the event propagation
            tb.Attributes("onfocus") = "currentNote=\'" + tb.ID + "\';";
            tb.Attributes("onblur") = "saveNote(false);"; //uses the currentNote JS variable

            tb.Attributes("onkeydown") = "var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode==13){return false;}";

            //var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode!=13){//code goes here}

            tb.AutoPostBack = false;
            notePanel.Controls.Add(tb);
            tb.Text = this.Note.DisplayValue;
            bl.Controls.Add(notePanel);

            Image img = new Image();
            img.ImageUrl = "../images/navigation/trash.png";

            notePanel.Controls.Add(img);
            img.Attributes("onclick") = "burstBubble(event);delNote(" + System.Convert.ToString(this.ID) + ");";
            img.Attributes("style") = "width:1.25em;height:1.25em;position:relative;left:1.75em;top:-.15em;";
            notePanel.Controls.Add(img);
            img.ToolTip = Xlt("Delete this note", agentAccount.Language);
        }

        return bl;


    }

    public int fetchPreinstalled(UInt64 lid, clsAccount buyeraccount, List<string> errormessages) //WebControls.Image previoulsy a fetcheimage - with onloadscript
    {


        //DON'T DO This if there's no webservice !
        if ((quote.BuyerAccount.SellerChannel.priceConfig & 8) == 0)
        {
            return 0;
        }

        List<clsVariant> toget = additionalUpdates(this.Branch, buyeraccount, this.Path, errormessages);

        int handle = 0;
        handle = System.Convert.ToInt32(ModUniTran.DispatchUpdateRequest(lid, toget, "", errormessages));

        //pbi.path$ - tree.1 was pbi.path - but there's no real reason (apart from perhaps swift) not to placeprices across the whole tree
        if (handle == 0)
        {
            errormessages.Add("*" + Xlt("Could not dispatch web request (handle was 0)", this.quote.AgentAccount.Language));
        }
        else
        {
            //inserts an image with an onload script which calls the js FillPrices() after 5 seconds
            //refreshing the prices within the quotouter dost update totals
            //    Return fetcherImage("quote", handle, toget)
            return handle;
        }

    }

    private Panel SystemFooter(clsAccount buyeraccount, clsAccount agentaccount, ref List<string> errorMessages)
		{
			Panel returnValue = default(Panel);
			
			//contains the loyalty points and flex discount summary per system -
			
			Dictionary<clsScheme, int> dicPoints = new Dictionary<clsScheme, int>();
			
			this.LoyaltyPoints(ref dicPoints);
			
			returnValue = new Panel();
			returnValue.Attributes("class") = "flexChecklist";
			
			//If dicPoints.Count > 0 Then
			//    For Each scheme In dicPoints.Keys
			//        Dim lps As String = "<div class='pointsScheme'>" & scheme.Name.text(buyeraccount.Language) & "</div>"
			//        lps &= "<div class='pointsValue'>" & dicPoints(scheme).ToString & "</div><br/>"
			//        SystemFooter.Controls.Add(NewLit(lps))
			//    Next
			//End If
			
			object region = agentaccount.BuyerChannel.Region;
			System.Object t = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.AppliesToRegion(region) select l).ToList();
			
			int flexConditionCount = 0;
			PlaceHolder flexDoesQualifyPlaceholder = outputValidations(new[] { {enumValidationMessageType.Flex}, {EnumValidationSeverity.DoesQualify}, {}, buyeraccount, agentaccount, errorMessages, flexConditionCount});
			PlaceHolder flexDoesntQualifyPlaceholder = outputValidations(new[] {{enumValidationMessageType.Flex}, {EnumValidationSeverity.DoesntQualify}, {}, buyeraccount, agentaccount, errorMessages, flexConditionCount});
			
			if (flexConditionCount > 0)
			{
				
				//Ouput the Flex Qualifiaction messages
				Literal litFlexH3 = new Literal();
				if (t.Count > 0)
				{
					litFlexH3.Text = "<h3>" + Xlt("HP FlexAttach Requirements", agentaccount.Language) + "</h3>";
				}
				returnValue.Controls.Add(litFlexH3);
				
				returnValue.Controls.Add(flexDoesQualifyPlaceholder);
				returnValue.Controls.Add(flexDoesntQualifyPlaceholder);
				
				//need to totalise the rebate acrros 'me' (the system) and 'my' options
				decimal rr = new decimal(); //running rebate
				NullablePrice temp_runningTotal = new NullablePrice(agentaccount.Currency);
				this.Totalise(ref temp_runningTotal, ref rr, false);
				
				Literal flexRebate = new Literal();
				if (rr != 0)
				{
					
					flexRebate.Text = "<div id=\"flexDis\" class= \"flexDiscountDiv\"> " + Xlt("HP FlexAttach Saving", agentaccount.Language) + "&nbsp;&nbsp;" + this.quote.Currency.format(rr, buyeraccount.Culture.Code, errorMessages, 2) + "</div>";
					//  flexRebate.BackColor = System.Drawing.ColorTranslator.FromHtml("#E1F0E1")
					
				}
				else
				{
					if (this.Branch.Product.isSystem(this.Path) && this.Branch.Product.OPGflexLines.Count > 0)
					{
						
						if (t.Count > 0)
						{
							flexRebate.Text = "<div id=\"flexDis\" class= \"flexDiscountDiv\">" + Xlt("HP FlexAttach Saving None", agentaccount.Language) + "</div>";
						}
						
					}
					
				}
				returnValue.Controls.Add(flexRebate);
				
			}
			
			
			return returnValue;
		}


    private Panel SystemHeader(clsAccount Agentaccount, clsAccount buyeraccount, ref List<string> errorMessages)
		{
			
			Panel header = new Panel();
			
			string script = "";
			if (this.collapsed)
			{
				header.Controls.Add(this.ExpandCollapseButton(false, ref script));
			}
			else
			{
				header.Controls.Add(this.ExpandCollapseButton(true, ref script));
			}
			
			//add the 'virtual' header row (with the rolled up price)
			Label lbl = new Label();
			lbl.Text = this.ShortName(buyeraccount.Language);
			
			
			
			//second m.path is the div to scroll to top
			header.Attributes("onclick") = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + System.Convert.ToString(this.ID) + ",\'\');getBranches(\'cmd=open&path=" + this.Path + "&to=" + this.Path + "&into=tree&Paradigm=C\');return(false);}";
			//was on mousedown -= bad (runs 2 threads)
			header.Attributes("class") += " systemHeader";
			
			lbl.Font.Bold = true;
			header.Controls.Add(lbl);
			if (!(Agentaccount.SellerChannel.marginMin == 0 && Agentaccount.SellerChannel.marginMax == 0))
			{
				Literal br = new Literal();
				br.Text = "<br/>";
				header.Controls.Add(br);
				header.Controls.Add(this.MarginUI(true, System.Convert.ToBoolean(this.quote.Locked)));
			}
			
			NullablePrice tot = new NullablePrice(0, buyeraccount.Currency, false);
			decimal rr = new decimal();
			this.Totalise(ref tot, ref rr, true); //the act of totalising may make this include a list price element
			
			tot.value -= rr; //Subtract the rebate in the sysytem header too
			
			Panel sysTot = new Panel();
			
			sysTot.CssClass = "h_sysTotal";
			Panel totPnl = tot.DisplayPrice(buyeraccount, errorMessages);
			sysTot.Controls.Add(totPnl);
			header.Controls.Add(sysTot);
			
			//MULTIPLIER
			Panel MultiPanel = new Panel();
			MultiPanel.CssClass = "multiDiv";
			header.Controls.Add(MultiPanel);
			
			Literal Mlabel = NewLit("<span class=\'multiLabel\'>" + Xlt("Multiplier", Agentaccount.Language) + "</span>");
			MultiPanel.Controls.Add(Mlabel);
			
			Panel multiFlex = new Panel();
			multiFlex.CssClass = "multiFlex";
			
			//In quote Quantity Flex Buttons - also calls validate for every item (which is expensive)
			if (this.Branch.Product.hasSKU)
			{
				bool allowflexup = true; // Me.Branch.Product.inFeed(buyeraccount.SellerChannel)
				if (this.Branch.Product.isFIO)
				{
					allowflexup = false; //Factory Installed Options cannot be flexed
				}
				multiFlex.Controls.Add(this.FlexButtons(allowflexup));
			}
			
			multiFlex.Controls.Add(this.QuantityBox(buyeraccount, Agentaccount.Language, errorMessages));
			
			MultiPanel.Controls.Add(multiFlex);
			
			////END of Multiplier
			
			if (this.collapsed)
			{
				header.Controls.Add(this.MessageCounts(new[] {{enumValidationMessageType.Validation}, {}, {EnumValidationSeverity.DoesQualify, EnumValidationSeverity.DoesntQualify}, false, true}));
			}
			
			if (this.ID == 0)
			{
				errorMessages.Add("QuoteItemID was 0");
			}
			List<string> marginsAvailable = new List<string>();
			CheckMargin(this, ref marginsAvailable);
			if (marginsAvailable.Count > 0)
			{
				//your (price before margin)
				Literal lit = new Literal();
				NullablePrice baseprice = new NullablePrice(0, this.quote.Currency, false);
				baseprice.isValid = true;
				System.Int32 temp_runningRebate = 0;
				this.Totalise(ref baseprice, ref temp_runningRebate, false);
				lit.Text = string.Format("<div class=\'basePrice\' title=\'{0}\'>{1}</div>", Xlt("Price before margin", Agentaccount.Language), Xlt("Base price:" + baseprice.text(buyeraccount, errorMessages), buyeraccount.Language));
				header.Controls.Add(lit);
			}
			
			return header;
			
		}

    private Panel QuoteItemTabs()
    {

        //OBSOLETED (before it ever saw the light of day) - surplanted by panels -

        Panel tabstrip = default(Panel);
        tabstrip = new Panel();

        tabstrip.ID = "quoteBasketTabStrip";

        //tabstrip.Controls.Add(MakeTab("Breakdown", viewTypeEnum.Breakdown))
        //tabstrip.Controls.Add(MakeTab("Summary", viewTypeEnum.Summary))
        //tabstrip.Controls.Add(MakeTab("Validation", viewTypeEnum.Validation))

        return tabstrip;

    }

    public string ShortName(clsLanguage language, bool forQuotes = false)
    {
        string returnValue = "";
        //Gives the familyname for a system unit

        if (this.Branch.Product.i_Attributes_Code.ContainsKey("FamMajor"))
        {
            if (new[] { "NBK", "SVR", "DTO", "SWD", "HPN" }.Contains(this.Branch.Product.ProductType.Code)) //need to change this for a flag
            {
                //new for greg
                //changes added for overlapping text in quotes
                if (forQuotes)
                {
                    returnValue = System.Convert.ToString(this.Branch.Parent.Translation.text(language)); //
                }
                else
                {
                    returnValue = System.Convert.ToString(this.Branch.Parent.Translation.text(language)); // Product.i_Attributes_Code("FamMajor")(0).displayName(English)
                    returnValue += " " + this.Branch.Product.ProductType.Translation.text(language);
                }
            }
            else
            {
                returnValue = System.Convert.ToString(this.Branch.Product.i_Attributes_Code("FamMajor")[0].displayName(language));
            }
        }
        else
        {
            //Lets mix this up a little, this was far to simple and neat... so we now go and look for any slots on the product and use the minor translation if its available, specifically for Hardware Kit as this just repeats in the basket
            object st = this.Branch.slots.Where(s => s.Value.Type.MajorCode.ToLower == this.Branch.Product.ProductType.Code.ToLower).FirstOrDefault;
            if (st.Value != null && st.Value.NonStrictType.TranslationShort != null)
            {
                returnValue = System.Convert.ToString(st.Value.NonStrictType.TranslationShort.text(language));
            }
            else
            {
                returnValue = System.Convert.ToString(this.Branch.Product.ProductType.Translation.text(language));
            }
        }

        return returnValue;
    }


    private Panel ExpandCollapseButton(bool open, ref string script)
    {
        Panel Btn = new Panel(); // - new panel 'expandCollapsebutton As WebControls.Image = New WebControls.Image
        Btn.CssClass = "expandContract";

        if (open)
        {
            //do not 'ShowInTree' when collapsing (Basecamp 054 bascamp 54)
            script = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + System.Convert.ToString(this.ID) + ",\'collapse\');};return false;";
        }
        else
        {
            Btn.CssClass += " collapsed";
            script = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + System.Convert.ToString(this.ID) + ",\'expand\');getBranches(\'cmd=open&path=" + this.Path + "&into=tree&Paradigm=C\')}";

        }

        Btn.Attributes("onclick") = script;

        return Btn;
    }


    private Image AddNoteButton()
    {
        Image returnValue = default(Image);

        returnValue = new Image();
        Image with_1 = returnValue;
        with_1.ImageUrl = "../images/navigation/pencil.png";
        with_1.CssClass = "quoteAddNote quoteTreeButton";
        with_1.ToolTip = Xlt("Add a note", this.quote.BuyerAccount.Language);
        if (this.Note.value == DBNull.Value)
        {
            with_1.Attributes("onclick") = "burstBubble(event);addNote(" + System.Convert.ToString(this.ID) + ");";
        }

        return returnValue;
    }


    public clsQuoteItem(clsQuote quote)
    {

        //This constructor is used to create the root quoteitem only - which just holds a set of children - the top level items (typically systems) in a quote - but also 'loose ' parts
        this.quote = quote;

        this.ID = -99; //NB: the 'virtual' root item never exits on disk - first level nodes have a
        this.Parent = null;
        this.Children = new List<clsQuoteItem>();
        this.Quantity = 1; //This is important (as all derived quanities are multiplied by it !)
        this.BasePrice = new NullablePrice(quote.Currency);
        this.validate = true;
        this.Msgs = new List<ClsValidationMessage>(); //String 'error (or other) message against this quote line item
        this.Note = new nullableString();
        this.Created = DateTime.Now;
        this.ExpandedPanels = new HashSet<panelEnum>(); //IMPORTANT (that the root item with options 'open' 'NB Viewtype uses the viewType enum BITWISE  (to compbile multiple states)
        this.Margin = 1; //default margin for the root item to 1
        this.order = 0;

    }


    public clsQuoteItem(int ID, clsQuote quote, clsBranch Branch, clsVariant SKUVariant, object path, int Quantity, NullablePrice basePrice, NullablePrice listprice, bool isPreInstalled, clsQuoteItem parent, nullableString Opg, nullableString bundle, decimal rebate, DateTime created, float margin, nullableString note, int order, bool validate)
    {

        //this constructor is used when recreating a record from the database

        this.ID = ID;
        this.quote = quote;
        this.Branch = Branch;

        this.SKUVariant = SKUVariant;
        this.Path = System.Convert.ToString(path);
        this.Quantity = Quantity;
        this.BasePrice = basePrice; //this isn't a good name - becuase it's BEFORE margin (so wasn't actually the price quoted)
        this.ListPrice = listprice;
        this.Bundle = bundle;
        this.Margin = margin; //TODO
        this.Note = note;

        this.OPG = Opg;
        this.rebate = rebate;
        this.Created = created;

        //Me.quote.Items.Add(Me.ID, Me) 'add to the flat list
        this.IsPreInstalled = isPreInstalled;

        this.Parent = parent;
        if (!(this.Parent == null))
        {
            //If Me.Parent.Children Is Nothing Then Me.Parent.Children = New List(Of clsquoteItem)
            this.Parent.Children.Add(this);
        }
        this.Children = new List<clsQuoteItem>();

        this.quote.UpdateDescAndPrice();

        this.validate = validate;

        this.Msgs = new List<ClsValidationMessage>(); //String 'error (or other) message against this quote line item
        this.Note = note;

        this.ExpandedPanels = new HashSet<panelEnum>();
        this.ExpandedPanels.Add(panelEnum.Options);
        this.order = order;

    }

    public clsQuoteItem(clsQuote quote, clsBranch Branch, clsVariant SKUvariant, object path, int Quantity, NullablePrice basePrice, NullablePrice listprice, bool isPreInstalled, clsQuoteItem parent, nullableString Opg, nullableString bundle, decimal rebate, float margin, nullableString note, int order, DataTable writecache = null, int importID = 0)
    {

        //Note - Bundle, OPG and Margin are nullable and/or have a default value

        //Top level items sit under the virtual root item (which doesnt exist in the database) - their parent pointers are null
        //Having a real root item might be neater - but we'd have to hide it - and it would have no branch or product

        this.ImportId = importID;

        // If quote.ID > 3000 And quote.ID < 3005 Then Stop 'TODO remove

        this.Created = DateTime.Now;

        this.quote = quote;
        this.Branch = Branch;
        this.SKUVariant = SKUvariant;

        this.Path = System.Convert.ToString(path);
        this.Quantity = Quantity;
        this.BasePrice = basePrice;
        this.ListPrice = listprice;
        this.Bundle = null;
        this.Margin = margin;
        this.Note = new nullableString();

        this.OPG = Opg;
        this.Bundle = bundle;
        this.rebate = rebate;
        //  Me.Created = Now

        this.order = order;

        //Me.quote.Items.Add(Me.ID, Me) 'add to the flat list
        this.IsPreInstalled = isPreInstalled;

        this.Parent = parent;
        if (!(this.Parent == null))
        {
            //If Me.Parent.Children Is Nothing Then Me.Parent.Children = New List(Of clsquoteItem)
            if (this.Parent == this.quote.RootItem)
            {
                this.Parent.Children.Insert(0, this);
            }
            else
            {
                this.Parent.Children.Add(this);
            }
        }

        this.Children = new List<clsQuoteItem>();

        this.validate = true;
        this.Msgs = new List<ClsValidationMessage>(); //String 'error (or other) message against this quote line item

        this.ExpandedPanels = new HashSet<panelEnum>();
        //   ExpandedPanels.Add(panelEnum.Spec) - greg/dan didnt want this open by default 20/07/2014



        if (writecache == null)
        {
            Pmark("New QuoteItem (INSERT)");

            this.ID = SQLInsert();


            Pacc("New QuoteItem (INSERT)");

        }
        else
        {

            Pmark("New QuoteItem (Writecache)");

            this.ID = -1; //they will get their true ID's next time they're loaded
            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();
            row["FK_Quote_id"] = quote.ID;
            row["FK_Branch_id"] = Branch.ID;
            row["path"] = path;
            row["Quantity"] = Quantity;
            row["price"] = BasePrice.value;
            row["listprice"] = ListPrice.value;
            row["fk_variant_id"] = SKUVariant.ID;
            row["created"] = Created;
            row["IsPreinstalled"] = isPreInstalled;
            row["Margin"] = margin;
            row["rebate"] = rebate;
            row["fk_import_id"] = importID; //todo !
            row["opg"] = OPG.value;
            row["bundle"] = Bundle.value;
            row["validate"] = true;

            row["note"] = Note.value;
            row["order"] = this.order;

            if (parent == null)
            {
                row["fk_quoteItem_id_parent"] = DBNull.Value;
            }
            else
            {
                row["fk_quoteItem_id_parent"] = parent.ID;
            }

            writecache.Rows.Add(row);

            Pacc("New QuoteItem (Writecache)");

        }



    }

    /// <summary>Writes the quote Item(s) to the database</summary>
    /// <remarks></remarks>
    public void updateRecursive()
    {

        if (!(this == this.quote.RootItem)) //the root item is 'virtual' (not on disk) and need not be updated (top level items in the quote have no parent)
        {
            this.Update();
        }

        foreach (var child in this.Children)
        {
            child.Update();
        }

    }

    /// <summary>Populates the dictionary of LoyaltyScheme>Points - and recurses for all child Items</summary>
    /// <remarks>Fills this Dictionary - giving total points per loyalty scheme for the entire 'basket' (accouting for Item quantities)</remarks>
    public void LoyaltyPoints(ref Dictionary<clsScheme, int> Dic)
    {

        if (this.Branch != null) //(the root quoteItem is a placeholder and has no branch/product)
        {

            if (!this.IsPreInstalled) // Don't include Pre-installed items in Loyalty Scheme calculations
            {

                foreach (var scheme in this.Branch.Product.Points.Keys)
                {
                    if (scheme.Region.Encompasses(this.quote.AgentAccount.SellerChannel.Region)) //Is the agent (who we presume is the one earning points) in this schemes' region
                    {
                        if (DateTime.Now > scheme.StartDate && DateTime.Now < scheme.EndDate) //is the scheme active
                        {
                            int dq = this.DerivedQuantity();
                            if (Dic.ContainsKey(scheme))
                            {
                                Dic(scheme) += dq * this.Branch.Product.Points(scheme);
                            }
                            else
                            {
                                Dic.Add(scheme, dq * this.Branch.Product.Points(scheme));
                            }
                        }
                    }
                }

            }
        }


        foreach (var child in this.Children)
        {
            child.LoyaltyPoints(Dic);
        }

    }

    public void Update(bool updatePrice = true)
    {
        if (updatePrice)
        {
            this.quote.UpdateDescAndPrice();
        }
        this.SQLUpdate();


        // Return Me

    }

    private Panel MarginUI(bool propagate, bool quoteLocked)
    {
        Panel returnValue = default(Panel);

        //hello

        //returns a margin button/indicator of the current margin per quote item
        //propagate determines wether all child items will inherrit this margin is it's applied
        //System headers propagate margins to all chlidren (sytems and options)
        //but systems themslves are 'divorced' from their otions

        returnValue = new Panel();
        returnValue.CssClass = "marginHolder";

        //A unique ID is needed for the two versions of the system unit (The header which propagates and the actual SU which does not)
        string uid = this.ID.ToString() + (propagate ? "p" : "").ToString();

        bool isRetainedMargin = true;
        Panel showbutton = new Panel();
        Literal lit = new Literal();
        lit.Text = "%";
        double mp = 0; //margin as a percentage
        mp = (this.Margin * 100) - 100;




        //see bug 1094
        //If mp <> 0 Then

        //    Dim e As Double = Math.Round(mp * 1000) - (mp * 1000)
        //    If Math.Abs(e) < 0.01 Then
        //        'its an 'exact' margin (so it's cost plus)
        //        mp = Math.Round(mp * 1000) / 1000

        //        lit.Text = mp.ToString & "%"
        //        isRetainedMargin = False
        //    Else
        //it's some strange precentage (1.02038503) - so it's 'retained'


        //need to add a retained/costplus property to the channel
        //The above was not robust (at all!) .. 20% RM is *exactly* 25% costplus see bug 1094

        mp = 1 - 1 / this.Margin; //work out what the RM was (although there will be a v.small rounding error)
        mp = System.Convert.ToDouble(Math.Round(mp * 1000) / 10); //round to the nearest 1000'th of a % (and multiply by 100)
        lit.Text = mp.ToString() + "%"; // ("0.0") & "%"
        //     End If
        //  End If

        //        If Me.Margin <> 1 Then
        showbutton.CssClass = "textButton addMarginButton";
        showbutton.ID = "amb" + uid;
        if (!quoteLocked)
        {
            showbutton.Attributes("onclick") = "burstBubble(event);display(\'" + showbutton.ID + "\',\'none\');display(\'mg" + uid + "\',\'block\');";
        }
        showbutton.Controls.Add(lit);

        returnValue.Controls.Add(showbutton);

        Panel marginGuts = new Panel();
        returnValue.Controls.Add(marginGuts);
        marginGuts.ID = "mg" + uid;
        marginGuts.Attributes("style") += " display:none";

        //'Insert the (initially hidden) UI for the margin

        TextBox tb = new TextBox();
        tb.Text = mp.ToString();
        tb.MaxLength = 5;
        tb.ID = "mv" + uid;
        tb.CssClass = "marginTextBox";
        tb.Attributes("onclick") = "burstBubble(event);";
        tb.Attributes("onkeydown") = "var e=event||window.event; var keyCode=e.keyCode||e.which; if (keyCode==13){return false;}";


        marginGuts.Controls.Add(tb);

        Literal ms = new Literal();
        ms.Text = "<select class=\'cprt\' id=\'mt" + uid + "\' onclick=\'burstBubble(event);\'>";
        ms.Text += "<option value=\'R\'" + (isRetainedMargin ? "selected" : "").ToString() + ">Retained margin</option>"; //eg * 1/.98
        ms.Text += "<option value=\'C\'" + (isRetainedMargin ? "" : "selected").ToString() + ">Cost Plus</option>"; //eg *1.02
        ms.Text += "</select>";
        marginGuts.Controls.Add(ms);

        Literal dib = new Literal(); //Do It Button
        marginGuts.Controls.Add(dib);
        dib.Text = "<div id=\'applyMarginButton\' class=\'amb textButton\' onclick=\'burstBubble(event);";
        dib.Text += "var ddl=document.getElementById(|mt" + uid + "|);"; //Marging Type (retained/cost plus)
        dib.Text += "var txt=document.getElementById(|mv" + uid + "|); if (marginValue(txt.value)) {  ";
        dib.Text += "saveNote(false);var url;url=|quote.aspx?cmd=margin=|+txt.value +|=|+ ddl.value+|&quoteCursor=" + System.Convert.ToString(this.ID) + "&propagate=" + (propagate ? "1" : "0").ToString() + "|;";
        dib.Text += "rExec(url, displayQuote);} else { $(\"#errmsg\").show(); } \'>Apply</div><div id \'errmsg\' style=\'display: none;\' >Please enter a numeric value between -20 to 40</div>";
        dib.Text = dib.Text.Replace("|", '\u0022'); //to use quotes in script . . .
        //End If

        return returnValue;
    }


    public Panel ValidationPanel(clsAccount buyeraccount, clsAccount agentaccount, ref List<string> ErrorMessages)
		{
			Panel returnValue = default(Panel);
			
			//dicslots is  by 'minor' type - slot types is the list of 'categories' we're validating by W,MEM,HDD etc
			returnValue = new Panel();
			returnValue.CssClass = "panelOuter";
			
			Panel vHeader = new Panel();
			vHeader.CssClass = "panelHeader";
			returnValue.Controls.Add(vHeader);
			
			string script = ""; //passed byref and POPULATES the script (which goes onto the WHOLE panel (not just the button)
			vHeader.Controls.Add(this.PanelButton(panelEnum.Validation, System.Convert.ToBoolean(this.ExpandedPanels.Contains(panelEnum.Validation)), ref script));
			vHeader.Attributes("onclick") = script;
			
			Literal title = new Literal();
			title.Text = "<div class=\'panelTitle\'>" + Xlt("Validation", agentaccount.Language) + "</div>";
			vHeader.Controls.Add(title);
			
			
			bool critOutstanding = true;
			Panel vdRollup = new Panel();
			vdRollup.CssClass = "validationRollup";
			vHeader.Controls.Add(this.MessageCounts(new[] {{enumValidationMessageType.Validation}, {}, {EnumValidationSeverity.DoesQualify, EnumValidationSeverity.DoesntQualify}, false, critOutstanding})); //exclude felx qualification messages from the roll up
			
			if (!critOutstanding)
			{
				vHeader.Controls.AddAt(2, NewLit("<img class=\'headerValidationIcon\'  src=\'/images/navigation/ICON_CIRCLE_tick.png\'><span class=\'iconX\'></span>"));
			}
			
			Label lblSpace = new Label();
			lblSpace.Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
			vHeader.Controls.Add(lblSpace);
			
			if (this.AllChildMsgs.Where(df => df.type == enumValidationMessageType.Upsell).Count() > 0)
			{
				vHeader.Controls.Add(NewLit("<img class=\'headerUpsellIcon\'  src=\'/images/navigation/ICON_IQ2_UPSELL.png\'><span class=\'iconX\'></span>"));
			}
			
			if (this.ExpandedPanels.Contains(panelEnum.Validation))
			{
				//Output ALL validation messsages - EXCEPT flex qualification - which greg wants in the systemfooter
				int count = 0;
				returnValue.Controls.Add(this.outputValidations(new[] {{enumValidationMessageType.Validation}, {}, {EnumValidationSeverity.DoesntQualify, EnumValidationSeverity.DoesQualify}, buyeraccount, agentaccount, ErrorMessages, count}));
				
			}
			
			return returnValue;
		}

    public PlaceHolder outputValidations(enumValidationMessageType[] onlytype, EnumValidationSeverity[] only, EnumValidationSeverity[] Except, clsAccount buyeraccount, clsAccount agentaccount, List<string> errorMessages, ref int count)
    {

        //Returns a placeholder filled with (some of) the validation messages associated with the quote item
        PlaceHolder ph = new PlaceHolder();

        if (this.AllChildMsgs.Where(df => df.type == enumValidationMessageType.Upsell).Count() > 0 && !onlytype.Contains(enumValidationMessageType.Flex))
        {
            ph.Controls.Add(new ClsValidationMessage(enumValidationMessageType.UpsellHolder, EnumValidationSeverity.Upsell, null, iq.AddTranslation("Upsell Opportunities Available", English, "", 0, null, 0, false), "", 0, 0, new[] { }).UI(buyeraccount, agentaccount.Language, errorMessages, this.quote.ID));
            count++;
        }

        foreach (ClsValidationMessage vm in this.AllChildMsgs.Distinct())
        {
            if (vm != null && !Except.Contains(vm.severity) && (only.Length == 0 || only.Contains(vm.severity)) && (onlytype.Contains(vm.type) || onlytype.Length == 0)) //TODO remove - some 'nothings' are getting in
            {
                ph.Controls.Add(vm.UI(buyeraccount, agentaccount.Language, errorMessages, this.quote.ID));
                count++;
            }
        }

        return ph;

    }
    public List<ClsValidationMessage> AllChildMsgs
    {
        get
        {
            return this.Msgs.Union(this.Children.SelectMany(ch => ch.AllChildMsgs)).ToList();
        }
    }
    private string Initials(string phrase)
    {
        string returnValue = "";

        returnValue = "";
        foreach (var w in phrase.Split(" ".ToArray))
        {
            returnValue += System.Convert.ToString(w.First);
        }

        return returnValue;
    }

    public Panel PromosRollup(Dictionary<clsScheme, int> dicPoints, clsLanguage language)
    {
        Panel returnValue = default(Panel);

        //will also want total rebate

        returnValue = new Panel();
        returnValue.CssClass = "promosRollup";

        foreach (var scheme in dicPoints.Keys)
        {
            Literal lit = new Literal();
            lit.Text = "<div class = promoRollUpItem>" + Initials(System.Convert.ToString(scheme.displayName(language))) + ":" + System.Convert.ToString(dicPoints(scheme)) + "</div>";
            returnValue.Controls.Add(lit);
        }


        //'group and count points by scheme to make a compact header
        //Dim counts As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
        //For Each m In Me.Msgs
        //    If Not counts.ContainsKey(m.imagename) Then
        //        counts.Add(m.imagename, 1)
        //    Else
        //        counts(m.imagename) += 1
        //    End If
        //Next

        //For Each j In counts.Keys
        //    Dim i As WebControls.Image = New WebControls.Image
        //    i.ImageUrl = "/images/navigation/" & j
        //    i.CssClass = "headerValidationIcon"
        //    MessageCounts.Controls.Add(i)
        //    Dim lit As Literal = New Literal
        //    lit.Text = "<span class='iconX'>x" & counts(j).ToString.Trim & "</span>"
        //    MessageCounts.Controls.Add(lit)
        //Next

        return returnValue;
    }


    public Panel MessageCounts(enumValidationMessageType[] includetype, EnumValidationSeverity[] include, EnumValidationSeverity[] exclude, bool allSystems, ref bool critOutstanding)
    {
        Panel returnValue = default(Panel);

        returnValue = new Panel();
        returnValue.CssClass = "validationRollup";

        object combinedValidations = (allSystems ? (this.quote.RootItem.Children.SelectMany(sys => sys.Msgs).ToList()) : this.Msgs.ToList).Where(ch => (include.Length == 0 || include.Contains(ch.severity)) && (exclude.Length == 0 || !exclude.Contains(ch.severity)) && (includetype.Length == 0 || includetype.Contains(ch.type)));

        foreach (var p in combinedValidations.OrderByDescending(val => val.severity).GroupBy(sm => sm.imagename).Select(msg => NewLit("<img class=\'headerValidationIcon\' src=\'/images/navigation/" + msg.Key + "\' style=\'border-width:0px;\'><span class=\'iconX\'>x" + msg.Count.ToString() + "</span>")))
        {
            returnValue.Controls.Add(p);
        }

        if (combinedValidations.Count == 0 || combinedValidations.Max(f => f.severity) <= EnumValidationSeverity.BlueInfo)
        {
            critOutstanding = false;
        }


        return returnValue;

        //'ground and count each type of warning (by image name)
        //Dim counts As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)

        //Dim worsterror As Integer = 0


        //If allSystems Then  'Ugly - but this whole thing got compromised by the 'two level' approach (messages are not on their items anymore :o( )
        //    For Each System As clsQuoteItem In Me.quote.RootItem.Children
        //        For Each m In From r In System.Msgs Where r IsNot Nothing
        //            If m.severity <> exclude And m.severity <> exclude2 Then
        //                If m.severity > worsterror Then worsterror = m.severity
        //                If Not counts.ContainsKey(m.imagename) Then
        //                    counts.Add(m.imagename, 1)
        //                Else
        //                    counts(m.imagename) += 1
        //                End If
        //            End If
        //        Next
        //    Next
        //Else
        //    For Each m In From r In Me.Msgs Where r IsNot Nothing
        //        If m.severity <> exclude And m.severity <> exclude2 Then
        //            If m.severity > worsterror Then worsterror = m.severity
        //            If Not counts.ContainsKey(m.imagename) Then
        //                counts.Add(m.imagename, 1)
        //            Else
        //                counts(m.imagename) += 1
        //            End If
        //        End If
        //    Next
        //End If

        //For Each j In counts.Keys
        //    Dim i As WebControls.Image = New WebControls.Image
        //    i.ImageUrl = "/images/navigation/" & j
        //    i.CssClass = "headerValidationIcon"
        //    MessageCounts.Controls.Add(i)
        //    Dim lit As Literal = New Literal
        //    lit.Text = "<span class='iconX'>x" & counts(j).ToString.Trim & "</span>"
        //    MessageCounts.Controls.Add(lit)
        //Next

        //'Last part of the code executed if there are no errors and shows green tick
        //If worsterror <= EnumValidationSeverity.BlueInfo Then
        //    Dim i As WebControls.Image = New WebControls.Image
        //    i.ImageUrl = "/images/navigation/ICON_CIRCLE_tick.png" 'bodge
        //    i.CssClass = "headerValidationIcon"
        //    MessageCounts.Controls.Add(i)
        //    Dim lit As Literal = New Literal
        //    lit.Text = "<span class='iconX'></span>"
        //    MessageCounts.Controls.Add(lit)
        //End If

    }

    public Panel PanelButton(panelEnum paneltype, bool open, ref string script)
    {

        Panel Btn = new Panel(); // - new panel 'expandCollapsebutton As WebControls.Image = New WebControls.Image
        Btn.CssClass = "expandContract";

        //With (expandCollapsebutton)
        // .CssClass = "panelButton quoteTreeButton"
        if (open)
        {
            //.ImageUrl = "../images/navigation/minus.png"
            script = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + System.Convert.ToString(this.ID) + ",\'closePanel=" + System.Convert.ToString(paneltype) + "\');}";
            //.ToolTip = "Click to collapse this panel"
        }
        else
        {
            Btn.CssClass += " collapsed";
            //.ImageUrl = "../images/navigation/plus.png"
            script = "burstBubble(event);if(!ajaxing){setQuoteCursor(" + System.Convert.ToString(this.ID) + ",\'openPanel=" + System.Convert.ToString(paneltype) + "\');}";
            //.ToolTip = "click to expand this panel"
            //End If
        }

        return Btn;

    }

    public int SQLInsert(bool BootStrap = false)
    {
        int returnValue = 0;
        string parentid = "";
        if (Parent == null)
        {
            parentid = "null";
        }
        else
        {
            if (Parent == quote.RootItem || Parent.Branch == null) //the virtual root has no branch - wh need this check when cloning it (to make a new quote)
            {
                parentid = "null";
            }
            else
            {
                parentid = Parent.ID.ToString();
            }
        }

        object Sql = null;
        Sql = "INSERT INTO QuoteItem (fk_quote_id,fk_branch_id,path,quantity,price,listprice,fk_variant_id,created,ispreinstalled,fk_quoteitem_id_parent,margin,fk_import_id,opg,bundle,note,[order]) ";
        Sql += " values (" + quote.ID + "," + Branch.ID + ",\'" + Path + "\'," + System.Convert.ToString(Quantity) + "," + BasePrice.sqlvalue + "," + ListPrice.sqlvalue + "," + SKUVariant.ID + ",";
        Sql += da.UniversalDate(Created) + "," + (IsPreInstalled ? "1" : "0").ToString() + "," + parentid + "," + System.Convert.ToString(Margin) + "," + System.Convert.ToString(this.ImportId) + "," + OPG.sqlValue + "," + Bundle.sqlValue + "," + Note.sqlValue + "," + order.ToString() + ");";

        returnValue = System.Convert.ToInt32(da.DBExecutesql(Sql, true, this));
        return returnValue;
    }
    public void SQLUpdate()
    {
        StringBuilder sql = new StringBuilder(string.Empty);

        if (this.Quantity > 0)
        {
            sql.Append(string.Format("{0}{1}", "UPDATE QuoteItem SET Quantity = ", this.Quantity));
            sql.Append(string.Format("{0}{1}", ",price = ", this.BasePrice.sqlvalue));
            sql.Append(string.Format("{0}{1}", ",listPrice = ", this.ListPrice.sqlvalue));
            sql.Append(string.Format("{0}{1}", ",OPG = ", OPG.sqlValue));
            sql.Append(string.Format("{0}{1}", ",margin = ", this.Margin));
            sql.Append(string.Format("{0}{1}", ",bundle = ", this.Bundle.sqlValue));
            sql.Append(string.Format("{0}{1}", ",validate = ", (this.validate ? "1" : "0").ToString()));
            sql.Append(string.Format("{0}{1}", ",note = ", Note.sqlValue));
            sql.Append(string.Format("{0}{1}", ",Rebate = ", this.rebate));
            sql.Append(string.Format("{0}{1}", " WHERE ID = ", this.ID));

            try
            {
                da.DBExecutesql(sql.ToString(), false, this);
            }
            catch (System.Exception ex)
            {
                throw (new Exception("***" + ex.Message + "SQL was:" + sql.ToString()));
            }

        }
        else
        {

            // SK - updates wrapped in a single DB transaction to protect against multiple UI thread deadlocks
            sql.Append("BEGIN TRAN;");
            sql.Append(string.Format("DELETE FROM QuoteItem WHERE fk_quoteitem_id_parent = {0};", this.ID)); //delete any children first (including Chassis!)
            sql.Append(string.Format("DELETE FROM QuoteItem WHERE ID = {0};", this.ID));
            sql.Append("COMMIT TRAN;");

            da.DBExecutesql(sql.ToString(), false, this);

            if (this.quote.Cursor == this)
            {
                this.quote.Cursor = quote.RootItem; //fix for bug 721 (adding options to a basket you have removed the system from causes a crash)
            }

            this.Parent.Children.Remove(this); //THIS IS REALLY IMPORTANT

        }

    }
    public void UpdateSelfAfterIdChange(int NewId)
    {
        //Update OM
        //no need for quote item...
        this.ID = NewId;

    }

    //Private Sub addToBasket_Click(sender As Object, e As EventArgs)
    //    If quote IsNot Nothing Then
    //        Dim url As String = iq.sesh(Me., "GK_BasketURL")
    //        If url.Length > 0 Then

    //            Dim req As HttpWebRequest = WebRequest.Create(New Uri(url))
    //            req.ContentType = "text/xml; charset=utf-8"
    //            req.Method = "POST"
    //            req.Accept = "text/xml"

    //            'Generate the xml using the proxy class
    //            Dim dt As Data = New Data()
    //            dt.Quote.ID = quote.ID
    //            dt.Quote.Name = quote.Name.ToString
    //            dt.Quote.CreatedBy = quote.AgentAccount.User.RealName
    //            dt.Quote.Supplier = quote.AgentAccount.SellerChannel.Name
    //            dt.Quote.URLProductImage = quote.RootItem.Note.value 'need to ask nick abt this
    //            Dim products As List(Of DataQuoteProduct) = New List(Of DataQuoteProduct)
    //            Dim product As DataQuoteProduct
    //            For Each flatListItem In quote.RootItem.Flattened(True, False, 0).items
    //                product = New DataQuoteProduct()
    //                product.Class = flatListItem.QuoteItem.Branch.Product.ProductType.Code
    //                product.PartNum = "" 'flatListItem.QuoteItem.Branch.Product.i_Attributes_Code("MfrSKU").
    //                product.SupplierPartNum = flatListItem.QuoteItem.SKUVariant.DistiSku

    //                product.ListPrice = flatListItem.QuoteItem.ListPrice.value
    //                product.Description = flatListItem.QuoteItem.Branch.DisplayName(quote.BuyerAccount.Language)
    //                product.Qty = flatListItem.QuoteItem.Quantity
    //                product.URLProductImage = flatListItem.QuoteItem.Branch.Picture
    //                ' product.URLProductSpecs =
    //                products.Add(product)

    //            Next
    //            dt.Quote.Product = products.ToArray()


    //            Dim xmlString As String = SerializeToString(dt)
    //            iq.sesh(lid, "basketContent") = xmlString

    //            Dim trueUri As Uri = New Uri(Request.Url.AbsoluteUri)
    //            Dim uri As String = trueUri.Scheme + "://"
    //            uri = uri & Request.Url.Host.ToString()
    //            uri = uri & Page.ResolveUrl("~/BasketPost.aspx?lid=" & lid)

    //            Response.Redirect(uri)

    //        End If
    //    End If
    //End Sub


    //Private Sub FindValidation(ByRef selectedMsgs As List(Of ClsValidationMessage), root As clsQuoteItem)
    //    If root IsNot Nothing Then
    //        For Each msg In root.Msgs
    //            If msg.severity <= EnumValidationSeverity.RedCross Then
    //                selectedMsgs.Add(msg)
    //            End If
    //        Next
    //        For Each child In root.Children
    //            FindValidation(selectedMsgs, child)
    //        Next
    //    End If


    public List<ClsValidationMessage> ValidationsGreaterThanEqualTo(EnumValidationSeverity vmin)
    {
        List<ClsValidationMessage> returnValue = default(List<ClsValidationMessage>);

        returnValue = new List<ClsValidationMessage>();

    _10:
        foreach (var msg in this.Msgs)
        {
        _15:
            if (msg.severity >= vmin)
            {
                returnValue.Add(msg);
            _17:
                1.GetHashCode(); //VBConversions note: C# requires an executable line here, so a dummy line was added.
            }
        }
    _20:
        foreach (var child in this.Children)
        {
        _30:
            returnValue.AddRange(child.ValidationsGreaterThanEqualTo(vmin));
        }


        return returnValue;
    }


    private void CheckMargin(clsQuoteItem quoteItem, ref List<string> margin)
    {
        if (quoteItem.Margin != 1 && quoteItem.IsPreInstalled == false)
        {
            Margin.Add(quoteItem.ID.ToString());
        }
        else
        {
            foreach (var child in quoteItem.Children)
            {
                CheckMargin(child, ref margin);

            }
        }

    }

    private void getAllproductTypes(ref string alltypes, int flexOPGID)
    {

        System.Boolean sysFlexLine = (from l in this.Branch.Product.OPGflexLines.Values where l.FlexOPG.ID == flexOPGID select l).ToList();
        if (sysFlexLine.Count > 0)
        {
            alltypes += this.Branch.Product.ProductType.Code + " , ";
        }
        else
        {
            // If there's a warranty, always include it (even if not set up for flex)
            if (this.Branch.Product.ProductType.Code == "wty")
            {
                alltypes += this.Branch.Product.ProductType.Code + " , ";
            }
        }

        foreach (var child in this.Children)
        {
            child.getAllproductTypes(alltypes, flexOPGID);
        }

    }
    public void getQuoteVariant(List<clsVariant> variants, bool includePreinstalled = false)
    {
        if (this.SKUVariant != null)
        {
            variants.Add(this.SKUVariant);
        }
        foreach (var child in this.Children)
        {
            if (!child.IsPreInstalled || includePreinstalled)
            {
                child.getQuoteVariant(variants, includePreinstalled);
            }
        }
    }

    public bool hasRoomFor(clsQuoteItem qi)
    {
        foreach (var slot in qi.Branch.slots.Values)
        {

            if (this.dicslots.ContainsKey(slot.NonStrictType))
            {

                //does this slot apply here
                if (string.IsNullOrEmpty(System.Convert.ToString(slot.path)) || slot.path.Contains(this.Path))
                {

                    if (slot.numSlots < 0)
                    {

                        //it's a 'takes' slot (occupies slots) - typically an option
                        object slotsLeft = this.dicslots(slot.NonStrictType).Given - this.dicslots(slot.NonStrictType).taken;

                        if (slotsLeft <= 0)
                        {
                            return false;
                        }
                    }
                }

            }
        }
        return true;
    }

    public bool ShouldShowInBasket(bool includePreInstalled, clsAccount BuyerAccount, HashSet<string> foci)
    {
        //Can you purchase one of me
        //Ok, so new logic as of 16/03/2015 based on Dan's input on what should or should not show in the basket
        //Rules are
        //1. Honour the is preinstalled flag (not even sure if this is used, maybe flat list?)
        //2. Does this item have any slots, therefore is there any point in showing this so that it can be disabled from validation
        //3. Always show systems (of course!)
        //4. Its not a fake part (controversial)
        //5. Can you buy it?  If you can't select a replacement, why show it
        //***ML- I HAVE NOT IMPLEMENTED THIS*** 6. Are there any slots left for it? - debatable logic here I think, the point is surely that you can remove them from validation and MAKE slots available for a replacement

        if (BuyerAccount.HasRight("DIAGVIEW"))
        {
            return true;
        }

        //Return Not Me.Branch.Product.ProductType.Code.ToUpper = "EMB" AndAlso Not Me.Branch.Product.isFakePart AndAlso
        //           (((Me.IsPreInstalled OrElse (includePreInstalled And Not Me.IsPreInstalled)) AndAlso Me.Branch.slots.Count > 0) Or (Me.Branch.Product.isSystem(Me.Path) = False And Me.Branch.slots.Count = 0)) AndAlso
        //           (Me.SystemItem.Branch.findChildByProductType(Me.Path, Me.Branch.Product.ProductType, BuyerAccount, foci) IsNot Nothing Or Not Me.IsPreInstalled)

        // SNK - Conditional logic split up so things are hopefully a little clearer...
        if (this.Branch.Product.ProductType.Code.ToUpper == "EMB")
        {
            return false;
        }
        if (this.Branch.Product.isFakePart)
        {
            return false;
        }

        // Always show systems
        if (this.Branch.Product.isSystem(this.Path))
        {
            return true;
        }

        bool slotsOK = false;


        if (this.Branch.slots.Count == 0)
        {
            slotsOK = false;
        }
        else
        {
            slotsOK = this.IsPreInstalled || (includePreInstalled && !this.IsPreInstalled);
        }


        clsBranch branch = this.SystemItem().Branch.findChildByProductType(this.Path, this.Branch.Product.ProductType, BuyerAccount, foci);
        bool branchOK = branch != null || !this.IsPreInstalled;

        return slotsOK && branchOK;

    }
}


//End of clsQuoteItem