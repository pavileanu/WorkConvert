
//Option Strict On

using dataAccess;
using IQ.clsBranchState;
using System.IO;
using System.Xml;
using log4net;

//This class represents branches of the product tree - each instance is a branch.
//however - each branch may be grafted in many places in the tree - updating any of these grafts of the brach will update them all.
//Each branch references a single product
//More than one branch can reference the same product - allowing the same (underlying) product to appear in different places in the tree

//Each branch has a collection of childbranches
//and *usually* a parent branch - except when it's a graft
//Grafts effectively mean that a branch can have more than one parent - hence you cannot reliably recurse 'backwards' by parent
//To get arround this - the front end renders and mantains (as each branch is opened) an address or 'path' to every branch - typically into the ID of a DIV
//things like Quanty limits can then be 'scoped' with these unique branch addresses - such that they can apply only locally (in that context) if neccessary.


public class clsBranch : i_Editable
{

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private clsBranch Parent {
		get { return m_Parent; }
		set { m_Parent = Value; }
	}
	private clsBranch m_Parent;

	//many branches (roots of grafts, have no parent... a branch can have many parents, it can be grafted in many places ie. it can be the child of many branches)
	private clsProduct Product {
		get { return m_Product; }
		set { m_Product = Value; }
	}
	private clsProduct m_Product;
	private clsTranslation Translation {
		get { return m_Translation; }
		set { m_Translation = Value; }
	}
	private clsTranslation m_Translation;
	//the branch text (if no product is present)
	private Dictionary<int, clsBranch> childBranches {
		get { return m_childBranches; }
		set { m_childBranches = Value; }
	}
	private Dictionary<int, clsBranch> m_childBranches;

	private string Picture {
		get { return m_Picture; }
		set { m_Picture = Value; }
	}
	private string m_Picture;
	private Dictionary<int, clsQuantity> Quantities {
		get { return m_Quantities; }
		set { m_Quantities = Value; }
	}
	private Dictionary<int, clsQuantity> m_Quantities;
	//A 'flat' dictionary by ID for the generic editor - would be nice if we could edit the more complex multi-dimensional dictionaries.. but that's a bridge too far at the moment
	private Dictionary<int, clsBranch> AllParents {
		get { return m_AllParents; }
		set { m_AllParents = Value; }
	}
	private Dictionary<int, clsBranch> m_AllParents;

	//                                                                                                  path
	//    Public i_Quantities As Dictionary(Of clsRegion, Dictionary(Of String, clsQuantity))  'Quantity preInstalled, minimum and preferred increments (no maximum because that is handled by slots)
	//                                                                                   there can be more than one because they *may* have a path  (and only apply in that context)
	//                                                                                   Preinstalled quantities by country is what handles carepack auto-adds (which differ by country - yuck)

	//Property Matches As Integer  'a set of bitwise flags for WHETHER this branch featues each of the current keywords
	// Property Points As Integer 'the total number of matches (including multiple matches on the same keyword)

		//contains a compound key of slottype, path, and wether it is a + (give) or - (take)
	public Dictionary<string, clsSlot> i_Slots;
	private Dictionary<int, clsSlot> slots {
		get { return m_slots; }
		set { m_slots = Value; }
	}
	private Dictionary<int, clsSlot> m_slots;
	//containts gives (+) and takes (-) slot info

	private clsTranslation CollectiveNoun {
		get { return m_CollectiveNoun; }
		set { m_CollectiveNoun = Value; }
	}
	private clsTranslation m_CollectiveNoun;
	// the key to the translation containing the collective noun eg. "Options" for items under this branch (used in the branch child counts - blue numbers in brackets)
	private clsTranslation collectiveNounSingular {
		get { return m_collectiveNounSingular; }
		set { m_collectiveNounSingular = Value; }
	}
	private clsTranslation m_collectiveNounSingular;
	//eg. "Option"
	private clsScreen Matrix {
		get { return m_Matrix; }
		set { m_Matrix = Value; }
	}
	private clsScreen m_Matrix;
	//which screen (set of fields) is used to display the matrix in the front end
	private Dictionary<int, clsPrune> Prunes {
		get { return m_Prunes; }
		set { m_Prunes = Value; }
	}
	private Dictionary<int, clsPrune> m_Prunes;
	//List(Of String) ' a list of the paths at which this branch is pruned
	private int order {
		get { return m_order; }
		set { m_order = Value; }
	}
	private int m_order;
	private bool Hidden {
		get { return m_Hidden; }
		set { m_Hidden = Value; }
	}
	private bool m_Hidden;
	//For Chassis.Mobos etc
	private bool locked {
		get { return m_locked; }
		set { m_locked = Value; }
	}
	private bool m_locked;
	private string rca {
		get { return m_rca; }
		set { m_rca = Value; }
	}
	private string m_rca;
	//A string containing BSGT 'Branches Squares Grid Tabs

	private string Tag {
		get { return m_Tag; }
		set { m_Tag = Value; }
	}
	private string m_Tag;
	//used temporarily during import, not persisted
	private bool deleted {
		get { return m_deleted; }
		set { m_deleted = Value; }
	}
	private bool m_deleted;
	//soft' deleted (will not be loaded into the OM next time)

	public bool HasGrafts;
	private List<string> GraftedOnAt {
		get { return m_GraftedOnAt; }
		set { m_GraftedOnAt = Value; }
	}
	private List<string> m_GraftedOnAt;
	//SOME branches have grafts that only work at specific locations (used for CPUs)
	private bool unSearchable {
		get { return m_unSearchable; }
		set { m_unSearchable = Value; }
	}
	private bool m_unSearchable;

	// Private log As ILog = LogManager.GetLogger("IQDebug")

	//This is only used for the processor import, generally - surfing branches by name is a BAD idea (Names are case and spacing senstitive, language specific and not unique)
	public bool NameSurf(ref path, nm)
	{

		object nseg = Split(nm, "/");

		foreach ( b in this.childBranches.Values) {
			object bn = LCase(b.Translation.text(English));
			if (bn.Contains(LCase(nseg(0)))) {
				path += "." + b.ID.ToString.Trim;
				if (nseg.Count == 1) {
					return true;
				} else {
					if (b.NameSurf(path, Mid(nm, InStr(nm, "/") + 1))) {
						return true;
					}
				}
				// Else
				//    Return True
			}
		}

	}

	public object descendantQuantities(ref Dictionary<string, HashSet<clsQuantity>> dic)
	{


		//for each FIO sku there may be multiple quanities (localistations)
		//no skuless branch should have a quantity
		//the same sku should not appear more than once under the same system (at the moment)

		string optSKU;

		// And Me.isOption Then
		if (this.HasSKU) {
			optSKU = this.Product.SKU;

			if (this.Quantities.Count) {
				if (!dic.ContainsKey(optSKU)) {
					if (optSKU == "A8007B")
						System.Diagnostics.Debugger.Break();
					dic.Add(optSKU, new HashSet<clsQuantity>());
				}

				foreach ( q in this.Quantities.Values) {
					dic(optSKU).Add(q);
				}
			}

		}


		foreach ( c in this.childBranches.Values) {
			c.descendantQuantities(dic);
		}

	}


	//aobranch.compareagainst(validPaths, aobranch)
	public object compareAgainst(HashSet<string> validSkus, ref int kept, ref HashSet<string> delList)
	{

		if (!this.deleted) {
			if (this.Product != null) {
				if (this.Product.hasSKU) {
					if (validSkus.Contains(Product.SKU)) {
						kept += 1;
					} else {
						delList.Add(this.ID);
						this.deleted = true;
					}

				}
			}

			foreach ( c in this.childBranches.Values) {
				c.compareAgainst(validSkus, kept, delList);
			}
		}

	}


	public object flagAsUnsearchable(ref int count)
	{

		this.unSearchable = true;
		count += 1;

		foreach ( c in this.childBranches.Values) {
			c.flagAsUnsearchable(count);
		}

	}


		//used for 'one shot' (see branch.title) messages for some editor operations (notably) 'shred' there is a (vanishingly) small chance the message will be displayed to the wrong user - so this wan'ts improving at some point (along with the whole ProcesCommand 'cycle' - to remove all the stuff !tagged! on the end)
	public string message;

	/// <summary>Returns the distinct slots by type, with Path matches overringing empty paths    ''' </summary>
	/// <returns></returns>
	public List<clsSlot> slotsInForce(path)
	{

		//ML Change to a list so we can have multiple slots, 
		//problem with mod'ing the qty using i_slots (first idea) is they are a reference so you mod the qty and the object changes everywhere, not what we want

		List<clsSlot> Dic = new List<clsSlot>();

		//If the branch is soft deleted - it's slots are no longer in effect)
		if (!this.deleted) {

			foreach ( slot in this.slots.Values) {
				if (!slot.deleted) {
					if (LCase(slot.path) == LCase(path) | slot.path == "") {
						if (!Dic.Exists(f => object.ReferenceEquals(f.Type, slot.Type) && Math.Sign(f.numSlots) == Math.Sign(slot.numSlots) && f.slotNum.Equals(slot.slotNum))) {
							Dic.Add(slot);
						} else {
							//Get a list of slots already there
							object sls = Dic.Where(f => object.ReferenceEquals(f.Type, slot.Type) && Math.Sign(f.numSlots) == Math.Sign(slot.numSlots) && f.slotNum.Equals(slot.slotNum));
							if (slot.path != "") {
								if (sls.Count == 1 && string.IsNullOrEmpty(sls.First().path)) {
									Dic.Remove(Dic.Where(f => object.ReferenceEquals(f.Type, slot.Type)).First());
								}
								Dic.Add(slot);
							}
						}
					}
				}
			}

			//Note - memory slots *may* be on the CPU - but there should be a corrseponding quoteitem - so this will just work.

			//Not proud of this - recurses to include the slots off the chassis in the system
			//NB: - there is no quoteItem for the chassis - so we move the slots 'up' - the alternative - of a hidden, chassis quoteitem is (arguably) even uglier
			if (this.Product == null || this.Product.isSystem) {
				foreach ( b in this.childBranches.Values) {
					// If Not b.Product Is Nothing Then
					// If b.Product.ProductType.Code = "CHAS" Then
					//this will be the chassis branch
					if (b.slots.Count) {
						List<clsSlot> cs = b.slotsInForce(path + "." + b.ID);
						Dic.AddRange(cs);
					}
					//  Exit For
					//               End If
					//    End If
				}
			}
		}

		return Dic;

	}

	public bool hasQuantity(path, clsRegion region)
	{

		foreach ( q in this.Quantities.Values) {
			if (q.Path == path & object.ReferenceEquals(q.Region, region))
				return true;
		}

		return false;

	}

	/// <summary>
	/// Helper function - recursively populates a dictionary of option products below the branch, and their paths
	/// </summary>

	public void optionsBelow(path, ref Dictionary<clsProduct, string> options)
	{
		if (this.DisplayName(English).ToLower != "fios") {
			if (this.Product != null) {
				if (this.Product.isOption) {
					if (options.ContainsKey(this.Product)) {
						if (this.Product.ProductType.Code == "MEM" | this.Product.ProductType.Code == "CPU")
							System.Diagnostics.Debugger.Break();
						string x = PathName(path) + " is a duplicate of " + PathName(options(this.Product));
					// Beep()
					} else {
						options.Add(this.Product, path);
					}

				}
			}

			foreach ( cb in this.childBranches.Values) {
				cb.optionsBelow(path + "." + cb.ID, options);
			}
		} else {
			// Beep()
		}

	}


	public bool HasSiblingWithSameProduct()
	{

		if (this.Product != null) {
			if (this.Parent != null) {
				foreach ( b in this.Parent.childBranches.Values) {
					if (!object.ReferenceEquals(b, this)) {
						if (object.ReferenceEquals(this.Product, b.Product)) {
							return true;
						}
					}
				}
			}
		}

	}


	public void OptionsPersystem(string systemSKU, ref HashSet<string> opts, string path, ref int prunes, ref int dupes, StreamWriter sw, HashSet<string> inSkus)
	{
		object bn = this.Translation.text(English).ToLower;
		if (bn.Contains("accessories and"))
			return;

		//  Dim systems As HashSet(Of String)

		// If Me.Product IsNot Nothing AndAlso Product.SKU = "QK765A" Then Stop

		if (this.PruneInForce(path, HP) != 0) {
			prunes += 1;
		} else {
			if (this.deleted) {
				return;
			} else {
				if (this.Product != null) {
					// If Me.Product.deleted Then Exit Sub
					if (this.Product.hasSKU) {
						//If Product.SKU = "QK765A" Then Stop
						//And Me.childBranches.Count > 0 Then
						if (Product.isSystem) {
							//  If Product.Publish = False Or Product.Active = False Then Exit Sub 'dont recurse throuh unpublished systems
							systemSKU = Product.SKU;
						}

						if (this.Product.isOption) {
							//If Me.Product.SKU = "AN975A" And systemSKU = "671163-425" Then Stop

							//        Dim pn As String = PathName(path)
							if (Product.Active & Product.Publish) {
								if (inSkus.Count == 0 || inSkus.Contains(systemSKU)) {
									object ck = systemSKU + "^" + this.Product.SKU;
									if (opts.Contains(ck)) {
										dupes += 1;
									// sw.WriteLine(systemSKU & " " & Me.Product.SKU & " " & PathName(path$))
									} else {
										opts.Add(ck);

									}
								}
							}
						}
					}
					if (this.Product.isSystem == false & this.HasSKU & this.childBranches.Count > 0)
						System.Diagnostics.Debugger.Break();

				}


				foreach ( c in this.childBranches.Values) {
					c.OptionsPersystem(systemSKU, opts, path + "." + c.ID, prunes, dupes, sw, inSkus);
				}
			}

		}


	}


	public void DistinctOptionsRecursive(string ty, string fam, Dictionary<string, clsProduct> opts)
	{
		//famMajor^optsku

		if (this.Product != null) {
			if (this.Product.i_Attributes_Code.ContainsKey("FamMajor")) {
				fam = this.Product.i_Attributes_Code("famMajor")(0).Translation.text(English);
				ty = this.Product.ProductType.Code;
				//system level product type SVR,SWD,DTO,NBK etc
			}

			//  If Me.Product.SKU = "202997-001" Then Stop

			if (this.Product.isOption == true) {
				if (this.Product.hasSKU) {
					if (this.Product.Active & !this.Product.EOL) {
						object ck = ty + "^" + fam + "^" + this.Product.SKU;
						if (!opts.ContainsKey(ck))
							opts.Add(ck, this.Product);
					}
				}
			}
		}

		foreach ( b in this.childBranches.Values) {
			b.DistinctOptionsRecursive(ty, fam, opts);
		}

	}


	public void systemsBelow(HashSet<clsProduct> lst)
	{
		//only used by the SNAP code (probably a duplicate of something)

		if (this.HasSKU && !this.Product.isSystem)
			return;
		//don't recurse into options


		if (this.HasSKU && this.Product.isSystem) {
			//            If Me.Product.EOL Then Stop
			if (this.Product.Active == true & this.Product.EOL == false) {
				lst.Add(this.Product);
			}
		}

		foreach ( b in this.childBranches.Values) {
			b.systemsBelow(lst);
		}

	}


	public void serializeRecursive(clsBranchInfo bi, int depth, string path, XmlTextWriter sw, bool crossSKUs, List<string> errormessages)
	{
		string indent = "";
		//As String = StrDup(depth, ChrW(9))
		object l;

		List<string> nogos = new List<string>();
		nogos.Add("Upsell Opportunities");
		nogos.Add("Top Recommended");
		//nogos.Add("All Options")

		if (nogos.Contains(this.Translation.text(English))) {
			return;
		}

		//Hide the chassis branches for now (longer term we may need to expose)
		if (this.Translation.text(English).EndsWith(" chassis"))
			return;

		//do not write out (at all) inactive or end of life products
		if (this.HasSKU && (this.Product.EOL | !this.Product.Active))
			return;

		if (crossSKUs == false) {
			if (this.Product != null && !this.Product.isSystem && this.Product.hasSKU) {
				return;
				//don't recurse into otpions
			}
		}

		sw.WriteStartElement("branch");
		sw.WriteStartAttribute("path");
		sw.WriteString(path);
		sw.WriteEndAttribute();

		if (this.Translation != null) {
			sw.WriteStartAttribute("text");
			sw.WriteString(this.Translation.text(English));
			sw.WriteEndAttribute();
		}

		List<string> hr = this.ReasonsForHide(bi.buyerAccount, bi.foci, path, bi.buyerAccount.SellerChannel.priceConfig, false, errormessages);

		if (hr.Any) {
			sw.WriteStartElement("hideReasons");
			foreach ( l in hr) {
				sw.WriteStartElement("reason");
				sw.WriteStartAttribute("text");
				sw.WriteString(l);
				sw.WriteEndAttribute();
				sw.WriteEndElement();
				///reason
			}
			sw.WriteEndElement();
			///hideReasons
		} else {
			if (this.Product != null) {
				sw.WriteStartElement("product");

				sw.WriteStartAttribute("id");
				sw.WriteString(this.Product.ID);
				sw.WriteEndAttribute();

				sw.WriteStartAttribute("SKU");
				sw.WriteString(this.Product.SKU);
				sw.WriteEndAttribute();

				sw.WriteStartAttribute("mfr");
				sw.WriteString(this.Product.mfrCode);
				sw.WriteEndAttribute();

				if (this.Product.Attributes.Any) {
					sw.WriteStartElement("productAttributes");
					foreach ( pa in this.Product.Attributes.Values) {
						pa.writeXML(sw);
						// sw.WriteRaw(pa.XML)
					}
					sw.WriteEndElement();
					///productAttributes
				}

				//count slots by major type
				if (this.Product.isSystem) {
					Dictionary<string, int> slotsummary = new Dictionary<string, int>();
					this.summariseSlots(slotsummary);
					foreach ( c in this.childBranches.Values) {
						c.summariseSlots(slotsummary);
					}

					sw.WriteStartElement("slotSummary");
					foreach ( kvp in slotsummary) {
						sw.WriteStartElement(kvp.Key);
						sw.WriteStartAttribute("number");
						sw.WriteString(kvp.Value);
						sw.WriteEndAttribute();
						sw.WriteEndElement();
					}
					sw.WriteEndElement();

				}


				if (this.slots.Any) {
					sw.WriteStartElement("slots");
					foreach ( slot in this.slots.Values) {
						slot.writeXml(sw);
					}
					sw.WriteEndElement();
					///slots
				}

				if (this.Quantities.Any) {
					sw.WriteStartElement("quantities");
					foreach ( q in this.Quantities.Values) {
						sw.WriteRaw(q.XML);
					}
					sw.WriteEndElement();
					// /quantities
				}

				sw.WriteEndElement();
				// /product
			}

			foreach ( b in this.childBranches.Values) {
				b.serializeRecursive(bi, depth + 1, path + "." + b.ID, sw, crossSKUs, errormessages);
			}

		}

		sw.WriteEndElement();
		///branch


	}

	public object summariseSlots(Dictionary<string, int> slotSummary)
	{

		//NOT recursive - only used by the XML SNAP/export - typically called for a system branch and all its children (to get the chassis branch)

		foreach ( s in this.slots.Values) {
			if (!slotSummary.ContainsKey(s.Type.MajorCode))
				slotSummary.Add(s.Type.MajorCode, 0);
			slotSummary(s.Type.MajorCode) += s.numSlots;
		}

	}



	public void DoPrunes(ref DataTable pwc, ref int npid, path, famMinor, Dictionary<string, Dictionary<string, string>> dic, ref int kept, ref int pruned)
	{
		//Walks the entire tree - checking the compatibilty of options, by their technology againstagainst the family under which they are appearing
		//pruning off incompatibles on the way

		if (this.DisplayName(English).ToLower.Contains("accessories")) {
			return;
			//Do NOT stumble into the Accessories catalogue
		}



		if (this.Product != null) {
			//If Product.isSystem Then Stop

			//the fammino attribute appears on both the family and system branches... neither of which are what we're pruning (which is options!)
			if (this.Product.i_Attributes_Code.ContainsKey("famMinor")) {
				famMinor = this.Product.i_Attributes_Code("famMinor")(0).Translation.text(English);

				if (Product.SKU == "803860-B21") {
					object b = 0;
				}
			//If LCase(Left(famMinor, 3)) = "dl3" Then Stop
			//  If Me.Product.isSystem = False Then Stop
			//  If famMinor = "" Then Stop
			//       opttype = Me.Product.i_Attributes_Code("OptType")(0).Translation.text(English)
			} else {
				//only for options...
				if (!this.Product.isSystem) {
					if (famMinor != "") {
						object sku = this.SKU;
						if (this.Product.ProductType.Code.ToLower == "hdd") {
							if (this.Product.i_Attributes_Code.ContainsKey("desc")) {
								object desc = this.Product.i_Attributes_Code("desc")(0).Translation.text(English);
								if (desc.Contains("3.5")) {
									object a = 0;
								}
							}
						}

						//NB: ### SKUS return an empty string !!!
						if (sku != "") {

							//if the minor option type (eg NHLLFF35 is not right right for this subfamiles 'tech' .. prune it
							if (this.Product.i_Attributes_Code.ContainsKey("optFamily")) {
								string optfam = this.Product.i_Attributes_Code("optFamily")(0).Translation.text(English);
								string opttype = this.Product.i_Attributes_Code("optType")(0).Translation.text(English);
								if (dic(famMinor).ContainsKey(opttype)) {
									if (dic(famMinor)(opttype) != optfam) {
										object aprune = new clsPrune(path, new NullableInt(), "DoPrunes Button", pwc, npid);
										pruned += 1;
									} else {
										kept += 1;
									}
								}
							}
						}
					}

				} else {
					//Stop 'reached a system - keep going, we want options...
				}
			}
		}


		foreach ( child in this.childBranches.Values) {
			//If famMinor <> "" Then Stop
			child.DoPrunes(pwc, npid, path + "." + child.ID, famMinor, dic, kept, pruned);
		}

	}


	public object toDisk(StreamWriter sw, int depth, path)
	{

		string sku = "";
		if (this.HasSKU)
			sku = this.SKU;

		object l = Space(depth * 2) + this.DisplayName(English);
		if (sku != "")
			l += " - " + sku;
		if (this.PruneInForce(path, RootChannel) != 0) {
			l = "X " + l + " X PRUNED";
		}

		sw.WriteLine(l);

		foreach ( c in this.childBranches.Values) {
			c.toDisk(sw, depth + 1, path + "." + c.ID);
		}

	}

	/// <summary>Used for import only - OptFamily 'holder' branches are tagged with the optfamily code - such that options with the wrong optfamily for the familyPriStor can be pruned</summary>
	/// <returns>Dictionary tag>path</returns>
	public Dictionary<string, string> TaggedPaths(path)
	{

		//will fail if the same tag appears more than once under the branch the methos id called on (which is a good thing!)

		Dictionary<string, string> idic = new Dictionary<string, string>();
		if (this.Tag != "") {
			idic.Add(this.Tag, path);
		} else {
			foreach ( c in this.childBranches.Values) {
				AppendDic(idic, c.TaggedPaths(path + "." + c.ID.ToString));
			}
		}

		return idic;

	}

	public Dictionary<string, string> OptionPaths(Path)
	{
		//Flattens

		Dictionary<string, string> idic = new Dictionary<string, string>();
		if (this.HasSKU) {
			idic.Add(this.SKU, Path);
		} else {
			foreach ( c in this.childBranches.Values) {
				AppendDic(idic, c.OptionPaths(Path + "." + c.ID.ToString));
			}

		}

		return idic;

	}


	/// <summary>
	/// recurses until it finds a descendant branch which appears in the view
	/// </summary>
	/// <param name="vw"></param>
	/// <returns></returns>
	/// <remarks></remarks>
	public bool isInOrHasDescendantIn(DataView vw)
	{


		iicalls += 1;

		// If vw.Sort <> "ID" Then Stop
		if (vw.Count == 0)
			return false;

		int b = vw.Table.Rows(0).Item("Id");
		string tl = iq.Branches(b).Translation.text(English);

		if (vw.Find(this.ID) != -1) {
			return true;
		}

		//Stop recusing at SKUs (don't cross systems)
		if (!(this.Product != null && this.Product.isSystem)) {

			foreach ( child in this.childBranches.Values) {
				if (child.isInOrHasDescendantIn(vw))
					return true;

			}
		}

	}

	public WebControls.TreeNode treeNode()
	{

		treeNode = new WebControls.TreeNode(this.DisplayName(English));
		treeNode.Value = this.ID;

		foreach ( child in this.childBranches.Values) {
			treeNode.ChildNodes.Add(child.treeNode);
		}

	}



	private void ensurePath(pth)
	{
		//makes sure a child named with the first section in the path exists and then recurses

		string[] psegs = Split(pth, "/");

		object nm = psegs(0);
		if (this.ChildNamed(nm) == null) {
			clsBranch newbranch = new clsBranch(null, this, iq.AddTranslation(nm, English, "HWCP", 0, null, -1, false), "", null, null, iq.Screens(719), 0, false, "B");
		}


		//if there was more than one / delimited segment - recurse on the child we have just created 
		if (psegs.Count > 1) {
			this.ChildNamed(nm).ensurePath(Mid(pth, InStr(pth, "/") + 1));
		}


	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="path"></param>
	/// <param name="buyeraccount"></param>
	/// <remarks></remarks>

	public void createCarePacks(path, clsAccount buyeraccount)
	{
		//Create carepacks - just in time for the system on this branch

		//Exit Sub

		//If Me.arent.childBranches.ContainsKey(-1) Then Exit Sub 'we already created them !

		//If Me Then


		this.ensurePath("All Options/Services/HW support");

		clsBranch cpholder;
		//never planned to use NameSurf this way - it will probably bite us in the arse
		if (this.NameSurf(path, "All Options/Services/HW support")) {
			cpholder = iq.Branches((int)Split(path, ".").Last);


			object systemSku = this.Product.SKU;
			clsRegion country = buyeraccount.SellerChannel.Region;

			clsTranslation cptl;
			clsTranslation cpstl;
			cptl = iq.AddTranslation("Carepack", English, "collect", 0, null, 0, false);
			cpstl = iq.AddTranslation("Carepacks", English, "collect", 0, null, 0, false);

			//    Dim packsHolder As clsBranch = New clsBranch(-1, Nothing, Me, cptl, "", cpstl, cptl, iq.i_screens_code("base"), 100, False, "GB")

			SqlClient.SqlConnection H1con = da.OpenDatabase("Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;");
			//change this
			string countryCode = string.Empty;
			countryCode = IIf(country.Code == "UK", "GB", country.Code);
			//open the recordset here - to get carepacks for this systemSku$, for this country.code
			object sql = "select CountryCode\t,HWpartnum,\tCPKpartnum,\ttxtStartDate,\ttxtEndDate,\tsortorder from DataStore.products.CarePacks  where HWpartnum = '" + Trim(systemSku) + "'  and CountryCode = '" + countryCode + "'";
			//and this

			SqlClient.SqlDataReader rdr = da.DBExecuteReader(H1con, sql);

			clsBranch cpkBranch;

			//use an ever decreasing negative branch number - so we can create branches with a unique ID, using the correct consturtor (ie.. withou them being persisted to the database)!
			int nextid = -2;

			List<string> l = new List<string>();

			if (rdr.HasRows)
				cpholder.childBranches.Clear();
			//Blow away anything thats defined (or already loaded) IF there are products.carepacks

			while (rdr.Read) {
				l.Add(rdr.Item("CPKpartnum"));
				// Continue While

				string cpkSKU = rdr.Item("CPKpartnum");

				// there's a lot of junk in here - post warranty carepacks etc.
				//NB**  If some systems *only* have junk - this will cause problems
				if (iq.i_SKU.ContainsKey(cpkSKU)) {
					//NB the screen here doesn't matter - it's the screen on the holding branch (parent) thats important !
					cpkBranch = new clsBranch(nextid, iq.i_SKU(cpkSKU), cpholder, cptl, "", cpstl, cptl, null, 0, false,
					false, "");
					nextid -= 1;
					//decrement
				}

			}
			rdr.Close();
			H1con.Close();

		} else {
			Beep();

		}

		//Alternative method (where ALLL carepacks) are grafted pruneCarePacks(path, l, buyeraccount.BuyerChannel)

	}
	public void pruneCarePacks(string Path, ref List<string> l, ref clsChannel channel)
	{
		if (Path.Split(".").Last() != this.ID)
			Path = Path + "." + this.ID;
		foreach ( c in this.childBranches) {
			if (c.Value.HasSKU() && c.Value.Product.i_Attributes_Code.ContainsKey("optFam") && c.Value.Product.i_Attributes_Code("optFam")(0).Translation != null && c.Value.Product.i_Attributes_Code("optFam")(0).Translation.text(English) == "CAREPACK") {

				if (!l.Contains(c.Value.Product.SKU)) {
					string Path2 = Path + "." + c.Value.ID;
					if (c.Value.PruneInForce(Path2, channel) == 0) {
						object p = new clsPrune(Path2, new NullableInt(channel.ID), "AutoCarePack");
					}
				}
			}
			c.Value.pruneCarePacks(Path, l, channel);
		}
	}



	public void index2(ref Dictionary<string, clsBranch> famPaths, int depth, path)
	{
		string[] seg;
		int segs;
		object sc = "";
		object famName = "";

		//root/sector/family
		if (depth == 3) {
			seg = Split(path, ".");
			segs = seg.Count;
			//this is the un-'abreviated' PK 
			int bid = (int)seg(segs - 1);

			if (iq.Branches(bid).Product != null) {
				famName = iq.Branches(bid).Product.i_Attributes_Code("FamMajor")(0).Translation.text(English);
				//This needs to be family CODE not the reanslation therof
				//famName = LCase(iq.Branches(CInt(seg(segs - 2))).famname) 'Translation.text(English))
				//famName = Replace(famName, " family", "")
				sc = LCase(iq.Branches((int)seg.Last).DisplayName(English));
				famPaths.Add(famName + "|" + sc, this);
			}

		}

		if (depth < 3) {
			foreach ( child in this.childBranches.Values) {
				child.index2(famPaths, depth + 1, path + "." + child.ID);
			}
		}

	}



	public void OrderFamilies(int depth, ref List<string> errormessages)
	{
		if (this.childBranches.Count > 0) {

			if (depth == 3) {
				//NOTE DOUBLE CHILDBRANCHES HERE BECAUSE OFF SUPPLY CHAIN - REMOVE
				if (this.childBranches.Values(0).childBranches.Values(0).Product != null) {
					if (this.childBranches.Values(0).childBranches.Values(0).Product.i_Attributes_Code.ContainsKey("formFactor")) {
						object ff = this.childBranches.Values(0).childBranches.Values(0).Product.i_Attributes_Code("formFactor")(0);
						string fft = ff.Translation.text(English);

						this.order = InStr(LCase("Rack Mount>SMALL FORM FACTOR RACK-MOUNT>Tower>MicroTower>ultra micro tower>Blade>Desktop Mini>Desktops>All in one>Rackable MiniTower>Convertible mini tower>small form factor>Thin Client>horizontally mounted / desktop>wall outlet box>ceiling mount only>WALL/CEILING/DESKTOP/UNDER-TABLE>WALL/DESKTOP/UNDER-TABLE MOUNT>elitebook>laptops>Probook>elitebook mobile workstation>ultrabook>tablet pc>mini-notebook>RACK MOUNT - LARGE FORM FACTOR DISKS>3U rack Unit>RACK-MOUNT MODULAR CHASSIS"), LCase(fft));
						//                        If Me.order = 0 Then Stop
						this.Update(errormessages);

						return;
					}

				}
			} else {
				foreach ( b in this.childBranches.Values.ToArray) {
					b.OrderFamilies(depth + 1, errormessages);
				}
			}
		}



	}



	public void setRCA(int depth, clsBranch parent, ref Dictionary<string, List<int>> How, ref List<string> errormessages)
	{
		string rca = "";




		switch (depth) {
			case  // ERROR: Case labels with binary operators are unsupported : Equality
1:
				//root branch renders its children (sectors) as squares
				rca = "S";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
2:
				//sector children (families) are rendered as squares - with the option of (united) grid, or branches
				rca = "DGB";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
3:
				//families children (supply chains) are rendered as open branches - with th eoption of a Unitied) grid
				rca = "BG";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
4:
				//systems children (TRO, Upsell and All Options)  render as hyperlinks (not tabs)
				rca = "K";
			case  // ERROR: Case labels with binary operators are unsupported : Equality
5:
				if (this.Picture == "hptop") {
					rca = "H";
					//TROs children render as TRO headers
				} else if (this.Picture == "upsell") {
					rca = "U";
					//up-sells don't have any real children
				} else if (this.Picture != "") {
				//Stop
				} else if (this.Translation.text(English).ToLower.Contains("chassis")) {
					rca = "B";
				} else {
					rca = "TGB";
					//all options - renders its children (opt cats) as tabs                
				}
			case  // ERROR: Case labels with binary operators are unsupported : Equality
6:
				if (parent.rca == "H") {
					rca = "I";
					//TRO items
				} else {
					rca = "YTGB";
					//all options - renders its children (opt cats) as HYPERLINKS (not tabs)

				}
			case  // ERROR: Case labels with binary operators are unsupported : GreaterThanOrEqual
7:
				if (this.childBranches.Count > 0 && this.childBranches.First.Value.Translation.Group == "OL3")
					rca = "B";
				else
				//systems children render as tabs
					rca = "GB";

		}

		if (this.rca != rca) {
			this.rca = rca;
			if (!How.ContainsKey(rca))
				How.Add(rca, new List<int>());
			How(rca).Add(this.ID);
			//Me.Update(errormessages) - this was very slow - moved to a dictionary of similar updates
		}

		int ord = 0;
		foreach ( child in from j in this.childBranches.Values.ToListorderby j.order) {
			child.setRCA(depth + 1, this, How, errormessages);
		}
		Beep();
	}

}

