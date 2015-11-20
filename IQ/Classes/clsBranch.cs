using dataAccess;
using IQ.clsBranchState;
using System.IO;
using System.Xml;
using log4net;
using Microsoft.VisualBasic.CompilerServices;


//Option Strict On


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


    public int ID { get; set; }
    public clsBranch Parent { get; set; }

    //many branches (roots of grafts, have no parent... a branch can have many parents, it can be grafted in many places ie. it can be the child of many branches)
    public clsProduct Product { get; set; }
    public clsTranslation Translation { get; set; } //the branch text (if no product is present)
    public Dictionary<int, clsBranch> childBranches { get; set; }

    public string Picture { get; set; }
    public Dictionary<int, clsQuantity> Quantities { get; set; } //A 'flat' dictionary by ID for the generic editor - would be nice if we could edit the more complex multi-dimensional dictionaries.. but that's a bridge too far at the moment
    public Dictionary<int, clsBranch> AllParents { get; set; }

    //                                                                                                  path
    //    Public i_Quantities As Dictionary(Of clsRegion, Dictionary(Of String, clsQuantity))  'Quantity preInstalled, minimum and preferred increments (no maximum because that is handled by slots)
    //                                                                                   there can be more than one because they *may* have a path  (and only apply in that context)
    //                                                                                   Preinstalled quantities by country is what handles carepack auto-adds (which differ by country - yuck)

    //Property Matches As Integer  'a set of bitwise flags for WHETHER this branch featues each of the current keywords
    // Property Points As Integer 'the total number of matches (including multiple matches on the same keyword)

    public Dictionary<string, clsSlot> i_Slots; //contains a compound key of slottype, path, and wether it is a + (give) or - (take)
    public Dictionary<int, clsSlot> slots { get; set; } //containts gives (+) and takes (-) slot info

    public clsTranslation CollectiveNoun { get; set; } // the key to the translation containing the collective noun eg. "Options" for items under this branch (used in the branch child counts - blue numbers in brackets)
    public clsTranslation collectiveNounSingular { get; set; } //eg. "Option"
    public clsScreen Matrix { get; set; } //which screen (set of fields) is used to display the matrix in the front end
    public Dictionary<int, clsPrune> Prunes { get; set; } //List(Of String) ' a list of the paths at which this branch is pruned
    public int order { get; set; }
    public bool Hidden { get; set; } //For Chassis.Mobos etc
    public bool locked { get; set; }
    public string rca { get; set; } //A string containing BSGT 'Branches Squares Grid Tabs

    public string Tag { get; set; } //used temporarily during import, not persisted
    public bool deleted { get; set; } //soft' deleted (will not be loaded into the OM next time)

    public bool HasGrafts;
    public List<string> GraftedOnAt { get; set; } //SOME branches have grafts that only work at specific locations (used for CPUs)
    public bool unSearchable { get; set; }

    // Private log As ILog = LogManager.GetLogger("IQDebug")

    //This is only used for the processor import, generally - surfing branches by name is a BAD idea (Names are case and spacing senstitive, language specific and not unique)
    public bool NameSurf(ref object path, object nm)
    {

        System.String nseg = Strings.Split(System.Convert.ToString(nm), "/");

        foreach (var b in this.childBranches.Values)
        {
            System.Char bn = Strings.LCase(System.Convert.ToString(b.Translation.text(English)));
            if (bn.Contains(nseg[0].ToLower()))
            {
                path += "." + b.ID.ToString().Trim;
                if (nseg.Count() == 1)
                {
                    return true;
                }
                else
                {
                    if (b.NameSurf(path, Strings.Mid(System.Convert.ToString(nm), System.Convert.ToInt32(nm.ToString().IndexOf("/") + 2))))
                    {
                        return true;
                    }
                }
                // Else
                //    Return True
            }
        }

    }

    public dynamic descendantQuantities(Dictionary<string, HashSet<clsQuantity>> dic)
    {


        //for each FIO sku there may be multiple quanities (localistations)
        //no skuless branch should have a quantity
        //the same sku should not appear more than once under the same system (at the moment)

        string optSKU = "";

        if (this.HasSKU()) // And Me.isOption Then
        {
            optSKU = System.Convert.ToString(this.Product.SKU);

            if (this.Quantities.Count)
            {
                if (!dic.ContainsKey(optSKU))
                {
                    if (optSKU == "A8007B")
                    {
                        Debugger.Break();
                    }
                    dic.Add(optSKU, new HashSet<clsQuantity>());
                }

                foreach (var q in this.Quantities.Values)
                {
                    dic(optSKU).Add(q);
                }
            }

        }


        foreach (var c in this.childBranches.Values)
        {
            c.descendantQuantities(dic);
        }

    }


    //aobranch.compareagainst(validPaths, aobranch)
    public dynamic compareAgainst(HashSet<string> validSkus, ref int kept, HashSet<string> delList)
    {

        if (!this.deleted)
        {
            if (this.Product != null)
            {
                if (this.Product.hasSKU)
                {
                    if (validSkus.Contains(Product.SKU))
                    {
                        kept++;
                    }
                    else
                    {
                        delList.Add(this.ID);
                        this.deleted = true;
                    }

                }
            }

            foreach (var c in this.childBranches.Values)
            {
                c.compareAgainst(validSkus, kept, delList);
            }
        }

    }


    public dynamic flagAsUnsearchable(ref int count)
    {

        this.unSearchable = true;
        count++;

        foreach (var c in this.childBranches.Values)
        {
            c.flagAsUnsearchable(count);
        }

    }


    public string message; //used for 'one shot' (see branch.title) messages for some editor operations (notably) 'shred' there is a (vanishingly) small chance the message will be displayed to the wrong user - so this wan'ts improving at some point (along with the whole ProcesCommand 'cycle' - to remove all the stuff !tagged! on the end)

    /// <summary>Returns the distinct slots by type, with Path matches overringing empty paths    ''' </summary>
    /// <returns></returns>
    public List<clsSlot> slotsInForce(object path)
    {

        //ML Change to a list so we can have multiple slots,
        //problem with mod'ing the qty using i_slots (first idea) is they are a reference so you mod the qty and the object changes everywhere, not what we want

        List<clsSlot> Dic = new List<clsSlot>();

        if (!this.deleted) //If the branch is soft deleted - it's slots are no longer in effect)
        {

            foreach (var slot in this.slots.Values)
            {
                if (!slot.deleted)
                {
                    if (Strings.LCase(System.Convert.ToString(slot.path)) == Strings.LCase(System.Convert.ToString(path)) || slot.path == "")
                    {
                        if (!Dic.Exists(f => f.Type == slot.Type && Math.Sign(f.numSlots) == Math.Sign(slot.numSlots) && f.slotNum.Equals(slot.slotNum)))
                        {
                            Dic.Add(slot);
                        }
                        else
                        {
                            //Get a list of slots already there
                            object sls = Dic.Where(f => f.Type == slot.Type && Math.Sign(f.numSlots) == Math.Sign(slot.numSlots) && f.slotNum.Equals(slot.slotNum));
                            if (slot.path != "")
                            {
                                if (sls.Count == 1 && string.IsNullOrEmpty(System.Convert.ToString(sls.First().path)))
                                {
                                    Dic.Remove(Dic.Where(f => f.Type == slot.Type).First());
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
            if (this.Product == null || this.Product.isSystem)
            {
                foreach (var b in this.childBranches.Values)
                {
                    // If Not b.Product Is Nothing Then
                    // If b.Product.ProductType.Code = "CHAS" Then
                    if (b.slots.Count) //this will be the chassis branch
                    {
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

    public bool hasQuantity(object path, clsRegion region)
    {

        foreach (var q in this.Quantities.Values)
        {
            if (q.Path == path && q.Region == region)
            {
                return true;
            }
        }

        return false;

    }

    /// <summary>
    /// Helper function - recursively populates a dictionary of option products below the branch, and their paths
    /// </summary>
    public void optionsBelow(object path, Dictionary<clsProduct, string> options)
    {

        if (this.DisplayName(English).ToLower() != "fios")
        {
            if (this.Product != null)
            {
                if (this.Product.isOption)
                {
                    if (options.ContainsKey(this.Product))
                    {
                        if (this.Product.ProductType.Code == "MEM" || this.Product.ProductType.Code == "CPU")
                        {
                            Debugger.Break();
                        }
                        string x = PathName(path) + " is a duplicate of " + PathName(options(this.Product));
                        // Beep()
                    }
                    else
                    {
                        options.Add(this.Product, path);
                    }

                }
            }

            foreach (var cb in this.childBranches.Values)
            {
                cb.optionsBelow(path + "." + cb.ID, options);
            }
        }
        else
        {
            // Beep()
        }

    }


    public bool HasSiblingWithSameProduct()
    {

        if (this.Product != null)
        {
            if (this.Parent != null)
            {
                foreach (var b in this.Parent.childBranches.Values)
                {
                    if (!(b == this))
                    {
                        if (this.Product == b.Product)
                        {
                            return true;
                        }
                    }
                }
            }
        }

    }

    public void OptionsPersystem(string systemSKU, HashSet<string> opts, string path, ref int prunes, ref int dupes, StreamWriter sw, HashSet<string> inSkus)
    {

        object bn = this.Translation.text(English).ToLower;
        if (bn.Contains("accessories and"))
        {
            return;
        }

        //  Dim systems As HashSet(Of String)

        // If Me.Product IsNot Nothing AndAlso Product.SKU = "QK765A" Then Stop

        if (this.PruneInForce(path, HP) != 0)
        {
            prunes++;
        }
        else
        {
            if (this.deleted)
            {
                return;
            }
            else
            {
                if (this.Product != null)
                {
                    // If Me.Product.deleted Then Exit Sub
                    if (this.Product.hasSKU)
                    {
                        //If Product.SKU = "QK765A" Then Stop
                        if (Product.isSystem) //And Me.childBranches.Count > 0 Then
                        {
                            //  If Product.Publish = False Or Product.Active = False Then Exit Sub 'dont recurse throuh unpublished systems
                            systemSKU = System.Convert.ToString(Product.SKU);
                        }

                        if (this.Product.isOption)
                        {
                            //If Me.Product.SKU = "AN975A" And systemSKU = "671163-425" Then Stop

                            //        Dim pn As String = PathName(path)
                            if (Product.Active && Product.Publish)
                            {
                                if (inSkus.Count == 0 || inSkus.Contains(systemSKU))
                                {
                                    System.String ck = systemSKU + "^" + this.Product.SKU;
                                    if (opts.Contains(ck))
                                    {
                                        dupes++;
                                        // sw.WriteLine(systemSKU & " " & Me.Product.SKU & " " & PathName(path$))
                                    }
                                    else
                                    {
                                        opts.Add(ck);

                                    }
                                }
                            }
                        }
                    }
                    if (this.Product.isSystem == false && this.HasSKU() && this.childBranches.Count > 0)
                    {
                        Debugger.Break();
                    }

                }


                foreach (var c in this.childBranches.Values)
                {
                    c.OptionsPersystem(systemSKU, opts, path + "." + c.ID, prunes, dupes, sw, inSkus);
                }
            }

        }


    }

    public void DistinctOptionsRecursive(string ty, string fam, Dictionary<string, clsProduct> opts)
    {

        //famMajor^optsku

        if (this.Product != null)
        {
            if (this.Product.i_Attributes_Code.ContainsKey("FamMajor"))
            {
                fam = System.Convert.ToString(this.Product.i_Attributes_Code("famMajor")[0].Translation.text(English));
                ty = System.Convert.ToString(this.Product.ProductType.Code); //system level product type SVR,SWD,DTO,NBK etc
            }

            //  If Me.Product.SKU = "202997-001" Then Stop

            if (this.Product.isOption == true)
            {
                if (this.Product.hasSKU)
                {
                    if (this.Product.Active && !this.Product.EOL)
                    {
                        System.String ck = ty + "^" + fam + "^" + this.Product.SKU;
                        if (!opts.ContainsKey(ck))
                        {
                            opts.Add(ck, this.Product);
                        }
                    }
                }
            }
        }

        foreach (var b in this.childBranches.Values)
        {
            b.DistinctOptionsRecursive(ty, fam, opts);
        }

    }

    public void systemsBelow(HashSet<clsProduct> lst)
    {

        //only used by the SNAP code (probably a duplicate of something)

        if (this.HasSKU() && !this.Product.isSystem)
        {
            return; //don't recurse into options
        }


        if (this.HasSKU() && this.Product.isSystem)
        {
            //            If Me.Product.EOL Then Stop
            if (this.Product.Active == true && this.Product.EOL == false)
            {
                lst.Add(this.Product);
            }
        }

        foreach (var b in this.childBranches.Values)
        {
            b.systemsBelow(lst);
        }

    }

    public void serializeRecursive(clsBranchInfo bi, int depth, string path, XmlTextWriter sw, bool crossSKUs, List<string> errormessages)
    {

        string indent = ""; //As String = StrDup(depth, ChrW(9))
        object l = null;

        List<string> nogos = new List<string>();
        nogos.Add("Upsell Opportunities");
        nogos.Add("Top Recommended");
        //nogos.Add("All Options")

        if (nogos.Contains(this.Translation.text(English)))
        {
            return;
        }

        //Hide the chassis branches for now (longer term we may need to expose)
        if (this.Translation.text(English).EndsWith(" chassis"))
        {
            return;
        }

        //do not write out (at all) inactive or end of life products
        if (this.HasSKU() && (this.Product.EOL || !this.Product.Active))
        {
            return;
        }

        if (crossSKUs == false)
        {
            if (this.Product != null && !this.Product.isSystem && this.Product.hasSKU)
            {
                return; //don't recurse into otpions
            }
        }

        sw.WriteStartElement("branch");
        sw.WriteStartAttribute("path");
        sw.WriteString(path);
        sw.WriteEndAttribute();

        if (this.Translation != null)
        {
            sw.WriteStartAttribute("text");
            sw.WriteString(this.Translation.text(English));
            sw.WriteEndAttribute();
        }

        List<string> hr = this.ReasonsForHide(bi.buyerAccount, bi.foci, path, System.Convert.ToInt32(bi.buyerAccount.SellerChannel.priceConfig), false, ref errormessages);

        if (hr.Any)
        {
            sw.WriteStartElement("hideReasons");
            foreach (object tempLoopVar_l in hr)
            {
                l = tempLoopVar_l;
                sw.WriteStartElement("reason");
                sw.WriteStartAttribute("text");
                sw.WriteString(l);
                sw.WriteEndAttribute();
                sw.WriteEndElement(); ///reason
            }
            sw.WriteEndElement(); ///hideReasons
        }
        else
        {
            if (this.Product != null)
            {
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

                if (this.Product.Attributes.Any)
                {
                    sw.WriteStartElement("productAttributes");
                    foreach (var pa in this.Product.Attributes.Values)
                    {
                        pa.writeXML(sw); // sw.WriteRaw(pa.XML)
                    }
                    sw.WriteEndElement(); ///productAttributes
                }

                //count slots by major type
                if (this.Product.isSystem)
                {
                    Dictionary<string, int> slotsummary = new Dictionary<string, int>();
                    this.summariseSlots(slotsummary);
                    foreach (var c in this.childBranches.Values)
                    {
                        c.summariseSlots(slotsummary);
                    }

                    sw.WriteStartElement("slotSummary");
                    foreach (var kvp in slotsummary)
                    {
                        sw.WriteStartElement(kvp.Key);
                        sw.WriteStartAttribute("number");
                        sw.WriteString(kvp.Value);
                        sw.WriteEndAttribute();
                        sw.WriteEndElement();
                    }
                    sw.WriteEndElement();

                }


                if (this.slots.Any)
                {
                    sw.WriteStartElement("slots");
                    foreach (var slot in this.slots.Values)
                    {
                        slot.writeXml(sw);
                    }
                    sw.WriteEndElement(); ///slots
                }

                if (this.Quantities.Any)
                {
                    sw.WriteStartElement("quantities");
                    foreach (var q in this.Quantities.Values)
                    {
                        sw.WriteRaw(q.XML);
                    }
                    sw.WriteEndElement(); // /quantities
                }

                sw.WriteEndElement(); // /product
            }

            foreach (var b in this.childBranches.Values)
            {
                b.serializeRecursive(bi, depth + 1, path + "." + b.ID, sw, crossSKUs, errormessages);
            }

        }

        sw.WriteEndElement(); ///branch


    }

    public dynamic summariseSlots(Dictionary<string, int> slotSummary)
    {

        //NOT recursive - only used by the XML SNAP/export - typically called for a system branch and all its children (to get the chassis branch)

        foreach (var s in this.slots.Values)
        {
            if (!slotSummary.ContainsKey(s.Type.MajorCode))
            {
                slotSummary.Add(s.Type.MajorCode, 0);
            }
            slotSummary(s.Type.MajorCode) += s.numSlots;
        }

    }



    public void DoPrunes(DataTable pwc, int npid, object path, object famMinor, Dictionary<string, Dictionary<string, string>> dic, ref int kept, ref int pruned)
    {
        //Walks the entire tree - checking the compatibilty of options, by their technology againstagainst the family under which they are appearing
        //pruning off incompatibles on the way

        if (this.DisplayName(English).ToLower().Contains("accessories"))
        {
            return; //Do NOT stumble into the Accessories catalogue
        }


        if (this.Product != null)
        {

            //If Product.isSystem Then Stop

            //the fammino attribute appears on both the family and system branches... neither of which are what we're pruning (which is options!)
            if (this.Product.i_Attributes_Code.ContainsKey("famMinor"))
            {
                famMinor = this.Product.i_Attributes_Code("famMinor")[0].Translation.text(English);

                if (Product.SKU == "803860-B21")
                {
                    int b = 0;
                }
                //If LCase(Left(famMinor, 3)) = "dl3" Then Stop
                //  If Me.Product.isSystem = False Then Stop
                //  If famMinor = "" Then Stop
                //       opttype = Me.Product.i_Attributes_Code("OptType")(0).Translation.text(English)
            }
            else
            {
                if (!this.Product.isSystem) //only for options...
                {
                    if (famMinor != "")
                    {
                        object sku = this.SKU();
                        if (this.Product.ProductType.Code.ToLower == "hdd")
                        {
                            if (this.Product.i_Attributes_Code.ContainsKey("desc"))
                            {
                                object desc = this.Product.i_Attributes_Code("desc")[0].Translation.text(English);
                                if (desc.Contains("3.5"))
                                {
                                    int a = 0;
                                }
                            }
                        }

                        if (sku() != "") //NB: ### SKUS return an empty string !!!
                        {

                            //if the minor option type (eg NHLLFF35 is not right right for this subfamiles 'tech' .. prune it
                            if (this.Product.i_Attributes_Code.ContainsKey("optFamily"))
                            {
                                string optfam = System.Convert.ToString(this.Product.i_Attributes_Code("optFamily")[0].Translation.text(English));
                                string opttype = System.Convert.ToString(this.Product.i_Attributes_Code("optType")[0].Translation.text(English));
                                if (dic(famMinor).ContainsKey(opttype))
                                {
                                    if (dic(famMinor)[opttype] != optfam)
                                    {
                                        clsPrune aprune = new clsPrune(path, new NullableInt(), "DoPrunes Button", pwc, npid);
                                        pruned++;
                                    }
                                    else
                                    {
                                        kept++;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {

                    //Stop 'reached a system - keep going, we want options...
                }
            }
        }


        foreach (var child in this.childBranches.Values)
        {
            //If famMinor <> "" Then Stop
            child.DoPrunes(pwc, npid, path + "." + child.ID, famMinor, dic, kept, pruned);
        }

    }


    public dynamic toDisk(StreamWriter sw, int depth, object path)
    {

        string sku = "";
        if (this.HasSKU())
        {
            sku = this.SKU();
        }

        System.Char l = Strings.Space(depth * 2) + this.DisplayName(English);
        if (!string.IsNullOrEmpty(sku))
        {
            l += " - " + sku;
        }
        if (this.PruneInForce(path, RootChannel) != 0)
        {
            l = "X " + System.Convert.ToString(l) + " X PRUNED";
        }

        sw.WriteLine(l);

        foreach (var c in this.childBranches.Values)
        {
            c.toDisk(sw, depth + 1, path + "." + c.ID);
        }

    }

    /// <summary>Used for import only - OptFamily 'holder' branches are tagged with the optfamily code - such that options with the wrong optfamily for the familyPriStor can be pruned</summary>
    /// <returns>Dictionary tag>path</returns>
    public Dictionary<string, string> TaggedPaths(object path)
    {

        //will fail if the same tag appears more than once under the branch the methos id called on (which is a good thing!)

        Dictionary<string, string> idic = new Dictionary<string, string>();
        if (this.Tag != "")
        {
            idic.Add(this.Tag, path);
        }
        else
        {
            foreach (var c in this.childBranches.Values)
            {
                AppendDic(idic, c.TaggedPaths(path + "." + c.ID.ToString()));
            }
        }

        return idic;

    }

    public Dictionary<string, string> OptionPaths(object Path) //Flattens
    {

        Dictionary<string, string> idic = new Dictionary<string, string>();
        if (this.HasSKU())
        {
            idic.Add(this.SKU(), Path);
        }
        else
        {
            foreach (var c in this.childBranches.Values)
            {
                AppendDic(idic, c.OptionPaths(Path + "." + c.ID.ToString()));
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
        {
            return false;
        }

        int b = System.Convert.ToInt32(vw.Table.Rows(0).Item("Id"));
        string tl = System.Convert.ToString(iq.Branches(b).Translation.text(English));

        if (vw.Find(this.ID) != -1)
        {
            return true;
        }

        if (!(this.Product != null && this.Product.isSystem)) //Stop recusing at SKUs (don't cross systems)
        {
            foreach (var child in this.childBranches.Values)
            {

                if (child.isInOrHasDescendantIn(vw))
                {
                    return true;
                }

            }
        }

    }

    public WebControls.TreeNode treeNode()
    {
        WebControls.TreeNode returnValue = default(WebControls.TreeNode);

        returnValue = new WebControls.TreeNode(this.DisplayName(English));
        returnValue.Value = this.ID;

        foreach (var child in this.childBranches.Values)
        {
            returnValue.ChildNodes.Add(child.treeNode);
        }

        return returnValue;
    }


    private void ensurePath(object pth)
    {

        //makes sure a child named with the first section in the path exists and then recurses

        string[] psegs = Strings.Split(System.Convert.ToString(pth), "/");

        System.Char nm = psegs[0];
        if (this.ChildNamed(nm) == null)
        {
            clsBranch newbranch = new clsBranch(null, this, iq.AddTranslation(nm, English, "HWCP", 0, null, -1, false), "", null, null, iq.Screens(719), 0, false, "B", null, -1);
        }


        //if there was more than one / delimited segment - recurse on the child we have just created
        if (psegs.Count() > 1)
        {
            this.ChildNamed(nm).ensurePath(Strings.Mid(System.Convert.ToString(pth), pth.ToString().IndexOf("/") + 2));
        }


    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="path"></param>
    /// <param name="buyeraccount"></param>
    /// <remarks></remarks>
    public void createCarePacks(object path, clsAccount buyeraccount)
    {

        //Create carepacks - just in time for the system on this branch

        //Exit Sub

        //If Me.arent.childBranches.ContainsKey(-1) Then Exit Sub 'we already created them !

        //If Me Then


        this.ensurePath("All Options/Services/HW support");

        clsBranch cpholder = default(clsBranch);
        if (this.NameSurf(ref path, "All Options/Services/HW support")) //never planned to use NameSurf this way - it will probably bite us in the arse
        {
            cpholder = iq.Branches(System.Convert.ToInt32(Strings.Split(System.Convert.ToString(path), ".").Last));


            object systemSku = this.Product.SKU;
            clsRegion country = buyeraccount.SellerChannel.Region;

            clsTranslation cptl = default(clsTranslation);
            clsTranslation cpstl = default(clsTranslation);
            cptl = iq.AddTranslation("Carepack", English, "collect", 0, null, 0, false);
            cpstl = iq.AddTranslation("Carepacks", English, "collect", 0, null, 0, false);

            //    Dim packsHolder As clsBranch = New clsBranch(-1, Nothing, Me, cptl, "", cpstl, cptl, iq.i_screens_code("base"), 100, False, "GB")

            SqlClient.SqlConnection H1con = da.OpenDatabase("Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;"); //change this
            string countryCode = string.Empty;
            countryCode = System.Convert.ToString(country.Code == "UK" ? "GB" : country.Code);
            //open the recordset here - to get carepacks for this systemSku$, for this country.code
            string sql = "select CountryCode	,HWpartnum,	CPKpartnum,	txtStartDate,	txtEndDate,	sortorder from DataStore.products.CarePacks  where HWpartnum = \'" + Strings.Trim(System.Convert.ToString(systemSku)) + "\'  and CountryCode = \'" + countryCode + "\'"; //and this

            SqlClient.SqlDataReader rdr = da.DBExecuteReader(H1con, sql);

            clsBranch cpkBranch;

            //use an ever decreasing negative branch number - so we can create branches with a unique ID, using the correct consturtor (ie.. withou them being persisted to the database)!
            int nextid = -2;

            List<string> l = new List<string>();

            if (rdr.HasRows)
            {
                cpholder.childBranches.Clear(); //Blow away anything thats defined (or already loaded) IF there are products.carepacks
            }

            while (rdr.Read)
            {
                l.Add(rdr.Item("CPKpartnum"));
                // Continue While

                string cpkSKU = System.Convert.ToString(rdr.Item("CPKpartnum"));

                // there's a lot of junk in here - post warranty carepacks etc.
                //NB**  If some systems *only* have junk - this will cause problems
                if (iq.i_SKU.ContainsKey(cpkSKU))
                {
                    //NB the screen here doesn't matter - it's the screen on the holding branch (parent) thats important !
                    cpkBranch = new clsBranch(nextid, iq.i_SKU(cpkSKU), cpholder, cptl, "", cpstl, cptl, null, 0, false, false, "");
                    nextid--; //decrement
                }

            }
            rdr.Close();
            H1con.Close();

        }
        else
        {
            Interaction.Beep();

        }

        //Alternative method (where ALLL carepacks) are grafted pruneCarePacks(path, l, buyeraccount.BuyerChannel)

    }
    public void pruneCarePacks(string Path, List<string> l, clsChannel channel)
    {
        if (Path.Split('.').Last() != this.ID)
        {
            Path = Path + "." + System.Convert.ToString(this.ID);
        }
        foreach (var c in this.childBranches)
        {
            if (c.Value.HasSKU() && c.Value.Product.i_Attributes_Code.ContainsKey("optFam") && c.Value.Product.i_Attributes_Code("optFam")[0].Translation != null && c.Value.Product.i_Attributes_Code("optFam")[0].Translation.text(English) == "CAREPACK")
            {
                if (!l.Contains(c.Value.Product.SKU))
                {

                    string Path2 = Path + "." + c.Value.ID;
                    if (c.Value.PruneInForce(Path2, channel) == 0)
                    {
                        clsPrune p = new clsPrune(Path2, new NullableInt(channel.ID), "AutoCarePack");
                    }
                }
            }
            c.Value.pruneCarePacks(Path, l, channel);
        }
    }


    public void index2(Dictionary<string, clsBranch> famPaths, int depth, object path)
    {

        string[] seg = null;
        int segs = 0;
        string sc = "";
        string famName = "";

        //root/sector/family
        if (depth == 3)
        {
            seg = Strings.Split(System.Convert.ToString(path), ".");
            segs = seg.Count();
            //this is the un-'abreviated' PK
            int bid = int.Parse(seg[segs - 1]);

            if (iq.Branches(bid).Product != null)
            {
                famName = System.Convert.ToString(iq.Branches(bid).Product.i_Attributes_Code("FamMajor")[0].Translation.text(English));
                //This needs to be family CODE not the reanslation therof
                //famName = LCase(iq.Branches(CInt(seg(segs - 2))).famname) 'Translation.text(English))
                //famName = Replace(famName, " family", "")
                sc = Strings.LCase(System.Convert.ToString(iq.Branches(System.Convert.ToInt32(seg.Last)).DisplayName(English)));
                famPaths.Add(famName + "|" + sc, this);
            }

        }

        if (depth < 3)
        {
            foreach (var child in this.childBranches.Values)
            {
                child.index2(famPaths, depth + 1, path + "." + child.ID);
            }
        }

    }


    public void OrderFamilies(int depth, ref List<string> errormessages)
    {

        if (this.childBranches.Count > 0)
        {
            if (depth == 3)
            {

                //NOTE DOUBLE CHILDBRANCHES HERE BECAUSE OFF SUPPLY CHAIN - REMOVE
                if (this.childBranches.Values(0).childBranches.Values(0).Product != null)
                {
                    if (this.childBranches.Values(0).childBranches.Values(0).Product.i_Attributes_Code.ContainsKey("formFactor"))
                    {
                        object ff = this.childBranches.Values(0).childBranches.Values(0).Product.i_Attributes_Code("formFactor")[0];
                        string fft = System.Convert.ToString(ff.Translation.text(English));

                        this.order = ("Rack Mount>SMALL FORM FACTOR RACK-MOUNT>Tower>MicroTower>ultra micro tower>Blade>Desktop Mini>Desktops>All in one>Rackable MiniTower>Convertible mini tower>small form factor>Thin Client>horizontally mounted / desktop>wall outlet box>ceiling mount only>WALL/CEILING/DESKTOP/UNDER-TABLE>WALL/DESKTOP/UNDER-TABLE MOUNT>elitebook>laptops>Probook>elitebook mobile workstation>ultrabook>tablet pc>mini-notebook>RACK MOUNT - LARGE FORM FACTOR DISKS>3U rack Unit>RACK-MOUNT MODULAR CHASSIS".ToLower()).IndexOf(fft.ToLower()) + 1;
                        //                        If Me.order = 0 Then Stop
                        this.Update(ref errormessages);

                        return;
                    }

                }
            }
            else
            {
                foreach (var b in this.childBranches.Values.ToArray)
                {
                    b.OrderFamilies(depth + 1, errormessages);
                }
            }
        }



    }


    public void setRCA(int depth, clsBranch parent, Dictionary<string, List<int>> How, List<string> errormessages)
    {

        string rca = "";




        if (depth == 1)
        {
            rca = "S"; //root branch renders its children (sectors) as squares
        }
        else if (depth == 2)
        {
            rca = "DGB"; //sector children (families) are rendered as squares - with the option of (united) grid, or branches
        }
        else if (depth == 3)
        {
            rca = "BG"; //families children (supply chains) are rendered as open branches - with th eoption of a Unitied) grid
        }
        else if (depth == 4)
        {
            rca = "K"; //systems children (TRO, Upsell and All Options)  render as hyperlinks (not tabs)
        }
        else if (depth == 5)
        {
            if (this.Picture == "hptop")
            {
                rca = "H"; //TROs children render as TRO headers
            }
            else if (this.Picture == "upsell")
            {
                rca = "U"; //up-sells don't have any real children
            }
            else if (this.Picture != "")
            {
                //Stop
            }
            else if (this.Translation.text(English).ToLower.Contains("chassis"))
            {
                rca = "B";
            }
            else
            {
                rca = "TGB"; //all options - renders its children (opt cats) as tabs
            }
        }
        else if (depth == 6)
        {
            if (parent.rca == "H")
            {
                rca = "I"; //TRO items
            }
            else
            {
                rca = "YTGB"; //all options - renders its children (opt cats) as HYPERLINKS (not tabs)
            }
        }
        else if (depth >= 7)
        {
            if (this.childBranches.Count > 0 && this.childBranches.First.Value.Translation.Group == "OL3")
            {
                rca = "B";
            }
            else
            {
                rca = "GB"; //systems children render as tabs
            }
        }

        if (this.rca != rca)
        {
            this.rca = rca;
            if (!How.ContainsKey(rca))
            {
                How.Add(rca, new List<int>());
            }
            How(rca).Add(this.ID); //Me.Update(errormessages) - this was very slow - moved to a dictionary of similar updates
        }

        int ord = 0;
        foreach (var child in from j in this.childBranches.Values.ToList orderby j.order select j)
        {
            if (child.DisplayName(English) != "Accessories")
            {
                child.setRCA(depth + 1, this, How, errormessages);
            }
            else
            {
                Interaction.Beep();
            }

        }

    }


    public string findProductPathByAttributeValueRecursive(object Path, string attributeCode, string value, bool useWildcard)
    {
        string returnValue = "";

        returnValue = "";

        if (this.Product != null)
        {
            if (this.Product.i_Attributes_Code.ContainsKey(attributeCode))
            {
                if (this.Product.i_Attributes_Code(attributeCode)[0].Translation != null)
                {
                    if (useWildcard)
                    {
                        if (StringType.StrLike(this.Product.i_Attributes_Code(attributeCode)[0].Translation.text(English), value, CompareMethod.Binary))
                        {
                            return System.Convert.ToString(Path);
                        }
                    }
                    else
                    {
                        if (this.Product.i_Attributes_Code(attributeCode)[0].Translation.text(English) == value)
                        {
                            return System.Convert.ToString(Path);
                        }
                    }
                }
                else
                {
                    if (this.Product.i_Attributes_Code(attributeCode)[0].NumericValue == value)
                    {
                        return System.Convert.ToString(Path);
                    }
                }
            }

        }

        foreach (var child in this.childBranches.Values)
        {
            object pth = child.findProductPathByAttributeValueRecursive(Path + "." + (child.ID).ToString().Trim(), attributeCode, value, useWildcard);
            if (pth != "")
            {
                return System.Convert.ToString(pth);
            }
        }

        return returnValue;
    }



    public List<string> findAllProductPathsByAttributeValueRecursive(object Path, string attributeCode, string value, bool useWildcard, clsAccount bi)
    {
        List<string> returnValue = default(List<string>);
        List<string> errorMessages = new List<string>();
        returnValue = new List<string>();

        //If Me.Product IsNot Nothing Then
        //    If Me.Product.i_Attributes_Code.ContainsKey(attributeCode) Then
        //        If Me.Product.i_Attributes_Code(attributeCode)(0).Translation IsNot Nothing Then
        //            If useWildcard Then
        //                If Me.Product.i_Attributes_Code(attributeCode)(0).Translation.text(English).ToUpper Like value.ToUpper Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
        //            Else
        //                If Me.Product.i_Attributes_Code(attributeCode)(0).Translation.text(English).ToUpper = value.ToUpper Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
        //            End If
        //        Else
        //            If Me.Product.i_Attributes_Code(attributeCode)(0).NumericValue = value Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
        //        End If
        //    End If

        //End If

        foreach (var child in this.childBranches.Values.ToArray)
        {
            if (child.Translation.text(English) == "Top Recommended")
            {
                continue; //Special exception for TRO's as we dont want them showing up
            }
            if (child.Product != null && child.Hidden == false && child.ReasonsForHide(bi, new HashSet<string>(Strings.Split(System.Convert.ToString(bi.BuyerChannel.Focus), ",")), Path, bi.SellerChannel.priceConfig, false, errorMessages).Count() == 0)
            {
                if (child.Product.i_Attributes_Code.ContainsKey(attributeCode))
                {
                    if (child.Product.i_Attributes_Code(attributeCode)[0].Translation != null)
                    {
                        if (useWildcard)
                        {
                            if (StringType.StrLike(child.Product.i_Attributes_Code(attributeCode)[0].Translation.text(English).ToUpper, value.ToUpper(), CompareMethod.Binary))
                            {
                                returnValue.Add(Path);
                            }
                            return returnValue;
                        }
                        else
                        {
                            if (child.Product.i_Attributes_Code(attributeCode)[0].Translation.text(English).ToUpper == value.ToUpper())
                            {
                                returnValue.Add(Path);
                            }
                            return returnValue;
                        }
                    }
                    else
                    {
                        if (child.Product.i_Attributes_Code(attributeCode)[0].NumericValue == value)
                        {
                            returnValue.Add(Path);
                        }
                        return returnValue;
                    }
                }

            }
            returnValue.AddRange(child.findAllProductPathsByAttributeValueRecursive(Path + "." + (child.ID).ToString().Trim(), attributeCode, value, useWildcard, bi));
        }

        return returnValue;
    }

    public string EnglishName()
    {

        return this.DisplayName(English);

    }

    clsBranch oParent;

    public clsBranch()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        AllParents = new Dictionary<int, clsBranch>();


        Prunes = new Dictionary<int, clsPrune>();
        GraftedOnAt = new List<string>();

    }

    public bool HasSystem()
    {

        if (this.Product == null)
        {
            return false;
        }
        if (this.Product.isSystem)
        {
            return true;
        }

    }
    public bool HasSKU()
    {
        bool returnValue = false;

        returnValue = false;
        if (this.Product == null)
        {
            return false;
        }
        if (!string.IsNullOrEmpty(System.Convert.ToString(Product.SKU)))
        {
            return true;
        }
        if (this.Product.i_Attributes_Code.ContainsKey("MfrSKU"))
        {
            Debugger.Break(); //HasSKU = True
        }

        return returnValue;
    }

    public clsBranch clone(object path, List<string> errormessages)
    {
        clsBranch returnValue = default(clsBranch);

        //returns an independent copy of the branch and it's product - not (yet) designed to be used recursively

        returnValue = new clsBranch(this.Product.clone, this.Parent, this.Translation.clone, this.Picture, this.CollectiveNoun, this.collectiveNounSingular, this.Matrix, this.order + 1, this.Hidden, this.rca, null, -1);

        //copy the child branches (chassis and option categories)  as grafts (shallow copy)
        foreach (var c in this.childBranches.Values)
        {
            // If c.Parent Is Nothing Then  'grafted branches have no parent
            returnValue.Graft(c, "grafted during deep copy", "", errormessages);
            // End If
        }

        //we only need to copy those slots and quantities that have a specific path - as the others will 'work' anyway (on grafts)
        List<clsQuantity> lq = this.PathedQuantities(); //<this is recursive

        clsQuantity qty;
        foreach (var q in lq)
        {
            string newpath = System.Convert.ToString(Utility.ReplaceSegment(q.Path, this.ID, returnValue.ID));
            qty = q.clone(newpath);
        }

        List<clsSlot> ls = this.PathedSlots();

        clsSlot slt;
        foreach (var slot in ls)
        {
            string newpath = System.Convert.ToString(Utility.ReplaceSegment(slot.path, this.ID, returnValue.ID));
            slt = slot.clone(newpath);
        }

        //Dim ns As clsSlot
        //For Each s In Me.slots.Values
        // If s.path = "" Then
        // ns = New clsSlot(s.Type, clone, s.path, s.numSlots, s.notes, s.slotNum, s.requiredFill, s.advisedFill)
        // End If
        // Next

        return returnValue;
    }


    //Public Sub IndexProductPaths(path$, ByRef monsterIndex As Dictionary(Of clsProduct, List(Of String)), systems As Boolean, options As Boolean, ProductsToFind As List(Of clsProduct))
    //    'THIS ONLY RETURNS THE FIRST OCCURANCE OF THE PRODUCT
    //    'IT *DOES NOT* INDEX PATHS

    //    If Me.Product IsNot Nothing Then
    //        If Me.Product.hasSKU Then
    //            If ProductsToFind Is Nothing OrElse ProductsToFind.Contains(Me.Product) Then
    //                If (Me.Product.isSystem And systems) Or (Not Me.Product.isSystem And options) Then
    //                    If Not monsterIndex.ContainsKey(Me.Product) Then
    //                        monsterIndex.Add(Me.Product, New List(Of String))
    //                    End If
    //                    monsterIndex(Me.Product).Add(path$)
    //                End If
    //            End If
    //        End If
    //    End If

    //    For Each child In Me.childBranches.Values
    //        child.IndexProductPaths(path$ & "." & Trim$(CStr(child.ID)), monsterIndex, systems, options, ProductsToFind)
    //    Next

    //End Sub
    public List<clsQuantity> PathedQuantities()
    {
        List<clsQuantity> returnValue = default(List<clsQuantity>);

        returnValue = new List<clsQuantity>();

        foreach (var b in this.childBranches.Values)
        {
            foreach (var q in b.Quantities.Values)
            {
                if (q.Path != "")
                {
                    returnValue.Add(q);
                }
            }

            returnValue.AddRange(b.PathedQuantities()); //<recurse
        }


        return returnValue;
    }

    public List<clsSlot> PathedSlots()
    {
        List<clsSlot> returnValue = default(List<clsSlot>);

        returnValue = new List<clsSlot>();

        foreach (var b in this.childBranches.Values)
        {
            foreach (var s in b.slots.Values)
            {
                if (s.path != "")
                {
                    returnValue.Add(s);
                }
            }
            returnValue.AddRange(b.PathedSlots()); // < recurse
        }

        return returnValue;
    }

    public List<clsQuantity> preInstalled(clsAccount buyeraccount, object path, List<string> errormessages)
    {
        List<clsQuantity> returnValue = default(List<clsQuantity>);

        //Recursive - not to be confused with AddPreinstalledRecursive which is used when adding items to quotes
        //Returns a list of all the preinstalled quantities under the this Branch - (when at the path specified).
        //pass the path to a system - you'll get all the preinstalled FOC options
        //used by Contrast.ASPX - for comparing the preinstalled options in systems

        returnValue = new List<clsQuantity>();

        clsRegion Region = buyeraccount.SellerChannel.Region;
        object ipath = null;
        clsQuantity q = default(clsQuantity);

        foreach (var b in this.childBranches.Values)
        {
            if (!b.deleted)
            {
                ipath = path + "." + b.ID;
                q = b.LocalisedQuantity(Region, ipath, errormessages); //returns the 'best' quantity record for this user - ie, the deepest, narrowest match
                if (!(q == null))
                {
                    if (q.NumPreInstalled > 0)
                    {
                        if (q.FOC)
                        {
                            returnValue.Add(q);
                        }
                    }
                }
                returnValue.AddRange(b.preInstalled(buyeraccount, ipath, errormessages));
            }
        }

        return returnValue;
    }

    public List<clsVariant> StalePrices(clsAccount buyerAccount, List<string> errorMessages) //Dictionary(Of String, clsProductVariant)   'only a combination of Product and Variant is unique
    {
        List<clsVariant> returnValue = default(List<clsVariant>);

        //returns a a list of the variants whose prices are 'stale'
        //The variants include the distiSKU

        returnValue = new List<clsVariant>(); //Dictionary(Of String, ClsProductVariant)  'distiSKUs >ProductVariants

        List<IQ.clsPrice> prices = default(List<IQ.clsPrice>);

        //Getprices won't actually queue or make any webservice request
        //        If Me.Product.inFeed(buyerAccount.SellerChannel) Then
        //If Me.Product.i_Variants IsNot Nothing Then 'Some (inactive typically) products have no variants - becuase nobody stocks or has prices for them)
        // If Me.Product.i_Variants.ContainsKey(buyerAccount.SellerChannel) Then 'it it in the sellers feed

        //9 will return Everyone Prices, customer specific prices plus POA's for those that don't exist (regardless of the channels priceconfig)

        prices = this.Product.GetPrices(buyerAccount, 9, iq.AllVariants, errorMessages, true);

        foreach (var p in prices)
        {
            if (p != null) //POA's are 'nothings' in the list of retrieved prices
            {
                if (p != null) //~~
                {
                    if (!p.SKUVariant.DistiSku.Contains("FAKE") && !p.SKUVariant.DistiSku.Contains("###"))
                    {
                        //fetch a new price for all 'old' prices, POA's and temporary clones of listprices
                        long minutesold = DateAndTime.DateDiff(DateInterval.Minute, System.Convert.ToDateTime(p.lastRequested), DateTime.Now);
                        if (minutesold > 60 || p.Price.isValid == false)
                        {
                            returnValue.Add(p.SKUVariant);
                            p.lastRequested = DateTime.Now;
                        }
                    }
                }
            }
        }

        return returnValue;
    }

    public Panel RenderTabHeads(clsBranchInfo pbi, clsBranchState pbs, Dictionary<clsBranch, clsVisibility> tabs, List<string> errorMessages)
    {
        Panel returnValue = default(Panel);

        //Renders this branches visible children as a set of tab heads

        returnValue = new Panel();
        returnValue.CssClass = "tabStrip";
        returnValue.ID = "tabStrip" + pbi.path.Split(".").Length.ToString();

        Panel tab = default(Panel);
        bool autoOpen = true;
        clsLanguage langEN = (from l in iq.Languages.Values where l.Code == "EN" select l).First;

        foreach (var vis in from k in tabs.Values select k) //Needs optimizing so we dont get branchinto twice in here, quick fix - ML
        {
            clsBranchInfo cbi = new clsBranchInfo(pbi.lid, pbi.path + "." + vis.branch.ID.ToString().Trim, null, pbi.treeWidth, pbi.Paradigm, errorMessages);
            clsBranchState bs = getbranchstate(cbi.lid, vis.path);

            if (bs != null)
            {
                autoOpen = false;
            }
        }
        int intFirstItem = 0;

        foreach (var vis in from k in tabs.Values orderby k.branch.order select k) //Me.childBranches.Values 'was me.childbranches
        {
            if (vis.branch.Hidden && !pbi.showAll)
            {
                continue;
            }
            tab = new Panel();
            returnValue.Controls.Add(tab);

            clsBranchInfo cbi = new clsBranchInfo(pbi.lid, pbi.path + "." + vis.branch.ID.ToString().Trim, null, pbi.treeWidth, pbi.Paradigm, errorMessages);
            clsBranchState bs = getbranchstate(cbi.lid, vis.path);

            // Auto Open the first big hyperlink (or one with an order of 10)
            //     If bs Is Nothing AndAlso vis.branch.order = 10 AndAlso pbs.rca = enumBt.bighyperlinK AndAlso autoOpen Then
            if (bs == null && intFirstItem == 0 && (pbs.rca == enumBt.bighyperlinK || pbs.rca == enumBt.Tab) && autoOpen)
            {

                enumBt bt = (enumBt)(BTchar.IndexOf(vis.branch.rca.First));
                bs = new clsBranchState(pbi.lid, cbi.path, bt, false, 0, 100);

            }
            intFirstItem++;
            Panel title = vis.branch.Title(cbi, false, true, false, 0, 0, vis.hideReasonList, errorMessages, pbs, bs);
            //If vis.branch.childBranches.Count > 0 Then
            tab.Controls.Add(title);
            //End If
            tab.Controls.Add(vis.branch.PromoIndicators(cbi, errorMessages));
            tab.ID = vis.path + ".tab";

            string Func = "";
            System.Char q = '\u0022';
            //NB: there is no way to close a tab per se, (you just select another)


            string pth = System.Convert.ToString(vis.path); //tabs(branch).path

            Func += "burstBubble(event);";

            if ((!(langEN == null)) && (vis.branch.Translation.text(langEN) == "HW Support"))
            {
                Func += "getBranches(\'cmd=openFiltered&path=" + pth + "\');";
            }
            else
            {
                Func += "getBranches(\'cmd=openTab&path=" + pth + "\');";
            }

            if (Strings.Trim(System.Convert.ToString(vis.branch.rca)) == "U")
            {
                //func$ &= "setTimeout(function(){showQuote()},200);" 'Selecting the upsell opportunities tab needs to refresh the quote (to generate the VM's and update the div)
                tab.CssClass += " upsell";
            }
            else if (Strings.LCase(System.Convert.ToString(vis.branch.Picture == "hptop")))
            {
                tab.CssClass += " hpTopRecommended";
            }

            Func += "return false;";
            tab.Attributes("onclick") = Func; //was omd

            if (pbs.rca == enumBt.hYperlink || pbs.rca == enumBt.bighyperlinK)
            {
                tab.CssClass += " ib";
                if (pbs.rca == enumBt.hYperlink)
                {
                    tab.CssClass += " optionsLink"; //this is temporary
                }
                else if (pbs.rca == enumBt.bighyperlinK)
                {
                    tab.CssClass += " bigLink"; //this is temporary
                }

                if (bs == null || bs.rca == enumBt.Hidden)
                {
                    tab.CssClass += " inActiveLink";
                }
                else
                {
                    tab.CssClass += " ActiveLink hpOrange";
                }
            }
            else
            {
                if (bs == null || bs.rca == enumBt.Hidden)
                {
                    tab.CssClass = "inActiveTab";
                }
                else
                {

                    tab.CssClass = "activeTab";
                    //AutoOpen the active tab
                    //  Dim bt As enumBt = CType(BTchar.IndexOf(vis.branch.rca.First), enumBt)
                    //  Dim bss As clsBranchState = New clsBranchState(pbi.lid, pbi.path & "." & vis.branch.ID.ToString, bt, 1, 0, 100)
                    //put the switcher in the active tab

                    // tab.Controls.Add(NewLit("<div class='switcherGap'>&nbsp;</div>"))
                    //tab.Controls.Add(Switcher(cbi, bs, False, vis.branch.rca))

                }
            }
            //tab.Controls.Add(NewLit(func$))
        }


        //****Options search Hyperlink/tab ***
        if (pbi.branch.Product != null)
        {
            if (pbi.branch.Product.isSystem)
            {

                tab = new Panel();
                returnValue.Controls.Add(tab);

                Panel ttlpnl = new Panel();
                tab.CssClass += "ib";
                tab.Controls.Add(ttlpnl);

                Label lbl = new Label();
                lbl.Text = Xlt("Search", pbi.agentAccount.Language);
                ttlpnl.Controls.Add(lbl);
                ttlpnl.CssClass += "bigLink";


                tab.Attributes("onclick") = "burstBubble(event);$(\'#optionsSearch\').show();$(\'#systemsSearch\').hide();searchClick(\'" + pbi.path + "\');return false;";

            }
        }


        //tab.Controls.Add(vis.branch.PromoIndicators(cbi, errorMessages))
        //tab.ID = vis.path & ".tab"

        //Dim c As Literal
        //c = New Literal
        //RenderTabHeads.Controls.Add(c)
        //c.Text = "<div style='clear:both;'></div>"

        return returnValue;
    }
    public clsPrune getPrune(object path, clsChannel sellerchannel)
    {
        clsPrune returnValue = default(clsPrune);

        returnValue = null;
        IEnumerable<clsPrune> c = from j in this.Prunes.Values where Strings.LCase(System.Convert.ToString(j.path)) == Strings.LCase(System.Convert.ToString(path)) && (j.ChannelID.value == DBNull.Value || System.Convert.ToInt32(j.ChannelID.value) == sellerchannel.ID) select j;
        if (c.Count > 0)
        {
            returnValue = c.First;
        }

        return returnValue;
    }

    public int PruneInForce(object path, clsChannel sellerchannel)
    {
        int returnValue = 0;

        //returns the ID of any prune

        returnValue = 0;
        if (this.Prunes.Count)
        {
            System.Boolean p = from j in this.Prunes.Values where Strings.LCase(System.Convert.ToString(j.path)) == Strings.LCase(System.Convert.ToString(path)) && (j.ChannelID.value == DBNull.Value || System.Convert.ToInt32(j.ChannelID.value) == sellerchannel.ID) select j;
            if (p.Count > 0)
            {
                returnValue = System.Convert.ToInt32(p.First.ID);
                return returnValue;
            }
        }


        //(cpu) Branches are 'virtually pruned' if they're at the 'wrong' location   - this is NOT obvious
        if (this.GraftedOnAt.Count > 0)
        {
            // Dim j$ = PathName(path$)
            // Dim k$ = PathName(GraftedOnAt(0))

            string spath = Strings.Left(System.Convert.ToString(path), Strings.InStrRev(System.Convert.ToString(path), ".") - 1);
            if (this.GraftedOnAt.Contains(spath))
            {
                returnValue = 0;
            }
            else
            {
                returnValue = 1000000;
            }
        }

        //If path.Contains("136780") Then
        //    Dim test As String = ""
        //End If
        //life would be easier (and faster) if the prunes were indexed by path - but this would make them tricky to edit (editiong a dictionary keyed by a path is not yet supported)



        return returnValue;
    }

    public PlaceHolder renderChildren(clsBranchInfo pbi, clsBranchState pbs, bool isGrid, ref string EndPath, Dictionary<clsBranch, clsVisibility> descendants, ref List<string> errorMessages, Panel Into) //Panel
	{
		
		//NB - descendants contains Branches which may not ultimately display in 'normal' (non-admin) mode
		//descendants is also already ordered, and have been filtered by a view
		//see HideReason
		System.Object language = ((clsAccount) (iq.sesh(pbi.lid, "BuyerAccount"))).Language;
		
		if (this != pbi.branch)
		{
			errorMessages.Add("wrong branch in PBI"); //: Return childrenPanel
		}
		
		int priceconfig = System.Convert.ToInt32(pbi.buyerAccount.SellerChannel.priceConfig);
		if (Strings.Left(System.Convert.ToString(pbi.buyerAccount.SellerChannel.Code), 3) == "MHP")
		{
			priceconfig = priceconfig & !8 != 0; //HP (universal instances)  dont have a webservice (temporary hack)
		}
		
		if ((priceconfig & 8) > 0) //customer specific (webservice) pricing
		{
            EmbedUpdateRequest(pbi, descendants, into, errorMessages);
		}
		
		if ((!(pbi == null)) && (!(pbi.ScreenHeader == null)) && (!(pbi.ScreenHeader.screen == null)) && (pbi.ScreenHeader.screen.code == "hmcSOFOS") && (Descendants().Count > 0))
		{
			object rok = RenderROK(pbi, descendants);
			if (!(rok == null))
			{
				Into.Controls.Add(rok);
			}
		}
		
		int showOnly = System.Convert.ToInt32(iq.sesh(pbi.lid, "showOnly")); //Keyword search results - supress systems siblings thing
		
		
		int numrows = 0;
		int rendered = 0;
		
		foreach (var kvp in descendants) //these are already ordered
		{
			clsBranch branch = kvp.Value.branch;
			clsVisibility visibility = kvp.Value;
			
			if (visibility.hideReasonList.Count == 0 || pbi.showAll)
			{
				if (rendered < pbs.maxChildren)
				{
					
					clsBranchInfo cbi = default(clsBranchInfo); //note Branchinfo is not persisted in the session (thats branchstate)
					cbi = new clsBranchInfo(pbi.lid, visibility.path, pbi.lblMatches, pbi.treeWidth, pbi.Paradigm, errorMessages, pbs.United ? pbi.path : null); //for closed branches the branchinfo.branchSTATE will be NOTHING
					
					rendered++;
					//Dim pnl As Panel = branch.UI(cbi, EndPath, errorMessages)
					
					bool supressed = false;
					if (showOnly != 0) //we're showing just one system (a system from the keyword search results)
					{
						if (branch.HasSKU() && branch.Product.isSystem)
						{
							if (branch.ID != showOnly)
							{
								supressed = true;
							}
						}
					}
					
					if (!supressed)
					{
						Into.Controls.Add(branch.UI(cbi, ref EndPath, ref errorMessages));
					}
					
					if (false && (pbs.rca == enumBt.DetailSquare || pbs.rca == enumBt.Square))
					{
						//throw in an advert
						if (VBMath.Rnd(1) > 100) //NB this is NEVER turn (effective comment)
						{
							Literal newlit = new Literal();
							newlit.Text = "<div class=\'squareAdvert\'>Banner</div>";
							Into.Controls.Add(newlit);
						}
					}
				}
				numrows++;
			}
		}
		
		if (pbs.rca == enumBt.Square && EndPath == "tree.1")
		{
			
			
			//Assume that this is only the top page...
			//Could ultimately do with a type of "linksquare" or something but it wouldnt fit with renderchildrenas, would have to be renderself...
			object agentAccount = iq.seshTyped<clsAccount>(pbi.lid, "AgentAccount");
			
			if (agentAccount.Manufacturer == Manufacturer.HPE)
			{
				Into.Controls.Add(NewLit("<div class=\"square dropShadow ib\" onclick=\"burstBubble(event);ShowSolutionStore(\'" + pbi.lid + "\',\'" + agentAccount.User.Email + "\',\'" + agentAccount.SellerChannel.Code + "\',\'" + agentAccount.mfrCode + "\');return false;\">" + "<div class=\"branchTitle\"><span>" + Xlt("Solution Store", English) + "</span></div><div class=\"hpBlue\" style=\"margin-top:1.5em;margin-left:1.3em;text-align:left;font-size:1.5em;width: 60%;\">" + Xlt("Flex-Bundle Solutions", English) + "</div><div style=\"margin-top:1.2em;margin-left: 2em;width:70%;text-align:left;\">" + Xlt("Workload optimized solutions including servers, storage, networking and services", English) + "</div>" + "</div>"));
			}
		}
		
		object Func;
		
		//End If
		
		//If pbs.rca = enumBt.TROitem AndAlso (descendants.Count - 1 = numrows Or rendered = pbs.maxChildren - 1) Then 'might need a hack to only show on carepacks for now
		//Add help me choose in here
		//Find this category in all options....
		//OptionPaths()
		if (pbs.rca == enumBt.TROitem)
		{
			Dictionary<string, List<string>> d = new Dictionary<string, List<string>>();
			iq.Branches(Strings.Split(System.Convert.ToString(pathToSystem(pbi.path)), ".").Last).SkuPaths(d, pathToSystem(pbi.path), true);
			string s = System.Convert.ToString((from sk in Descendants().Values where sk.branch.Product != null select sk.branch.Product.SKU).FirstOrDefault);
			if (d.ContainsKey(SKU()))
			{
				foreach (var p in d[SKU()])
				{
					if (FindBranchByName(p, "All Options") != null && this.Translation.text(English) == "Care Pack" || this.Translation.text(English).Contains("Microsoft")) //yuuuck, this must be taken out as it relies on the text, needed to get it in quick
					{
						p = p.Substring(0, p.Length - Strings.Split(System.Convert.ToString(p), ".").Last.Length - 1);
						Into.Controls.Add(NewLit("<button class=\'hpBlueButton smallfont\' onclick=\'getBranches(\"cmd=defFilterOn&path=" + pathToSystem(pbi.path) + "&to=" + p + "&into=tree\");return false;\'>" + Xlt("Help Me Choose", language) + "</button>"));
						break;
					}
				}
			}
			
		}
		
		if (isGrid)
		{
			
			string q = "\'";
			if (!(pbi.lblMatches == null))
			{
				//If cbi IsNot Nothing Then
				if (pbi.ScreenHeader.Vw.Count == 1)
				{
					pbi.lblMatches.Text = "1 match "; //e  'This isn't *quite* right
				}
				else
				{
					if (pbi.ScreenHeader.Vw.Count > 0)
					{
						pbi.lblMatches.Text = pbi.ScreenHeader.Vw.Count + " matches "; // & cbi.CollectivePlural 'This isn't *quite* right
					}
				}
			}
			
			
			
			object occ = null;
			
			//Compare/Constrast (scales) has been removed until it can get some more attention
			//  occ$ = "contrast('" & pbi.path$ & "');return false;"
			//  into.Controls.Add(MakeRoundButton("scales.png", "Compare selected systems", occ$, "", "contrast", pbi.AgentAccount.Language))  'positioned 2 ems 'over' (relatively) so it falls nicely under the the checkboxes and remains in the flow
			
			//call showchildren.aspx ..
			occ = "exportGridAsCSV(\'" + pbi.path + "\');";
			
			Into.Controls.Add(MakeRoundButton("excl.png", "Export Grid as CSV", occ, "", "contrast ib", pbi.agentAccount.Language)); //positioned 2 ems 'over' (relatively) so it falls nicely under the the checkboxes and remains in the flow
			
			
		}
		
		
		
		
		//contrast/compare/scales functionality disbabled for now
		if (false)
		{
			Panel contrastpanel = new Panel();
			contrastpanel.ID = "contrast." + pbi.path;
			contrastpanel.CssClass += " compareTablePanel";
			Literal lit = default(Literal);
			lit = new Literal();
			lit.Text = "&nbsp;";
			
			contrastpanel.Controls.Add(lit);
			Into.Controls.Add(contrastpanel);
		}
		
		
		if (numrows > 100)
		{
			
			HtmlGenericControl btnShowall = new HtmlGenericControl("button");
			
			Into.Controls.Add(btnShowall);
			if (pbs.maxChildren == 1000)
			{
				btnShowall.InnerHtml = Xlt("Show first 100 items only", pbi.agentAccount.Language); //note - the page doesnt post back to this isn't actually what changes the button ! - see recaption script
				Func = ButtonScript("path=" + pbi.path + "&cmd=maxrows&rows=100");
			}
			else
			{
				if (numrows <= 1000)
				{
					btnShowall.InnerHtml = string.Format(Xlt("Show all {0} items", pbi.agentAccount.Language), System.Convert.ToString(numrows));
					Func = ButtonScript("path=" + pbi.path + "&cmd=maxrows&rows=1000");
				}
				else
				{
					btnShowall.InnerHtml = Xlt("Show first 1,000 items", pbi.agentAccount.Language);
					Func = ButtonScript("path=" + pbi.path + "&cmd=maxrows&rows=1000");
					Literal slowlit = default(Literal);
					slowlit = new Literal();
					slowlit.Text = "<div class=\'perfNote\'>This may be slow !, For performance reasons - we never show more than 1000 rows on a page </div>";
					
					Into.Controls.Add(slowlit);
				}
			}
			
			btnShowall.Attributes("onclick") = Func;
			btnShowall.Attributes("class") = "textButton";
			btnShowall.Attributes.Add("style", "display:block;clear:both"); //the 'button' style places things 'inline' - which we dont actually want here (so we override it)
			
		}
		
		Pacc("Branch.renderChildren");
		//   Return childrenPanel
		
	}

    /// <summary>
    /// ROK - Display any OS extra information :
    /// Look through all the descendent products and see if all share the same Windows edition.
    /// If they do, display extra OS information
    /// </summary>
    /// <param name="pbi"></param>
    /// <param name="descendants"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    private Literal RenderROK(clsBranchInfo pbi, Dictionary<clsBranch, clsVisibility> descendants)
    {
        Literal returnValue = default(Literal);

        clsLanguage kyLanguage = (from l in iq.Languages.Values where l.Code == "KY" select l).First;
        string osTitle = null;
        string osEdition = null;
        string osCategory = null;
        string osKey = null;

        foreach (var branch in Descendants().Keys)
        {
            if (!(branch.Product == null))
            {
                if (branch.Product.i_Attributes_Code.ContainsKey("Category") && branch.Product.i_Attributes_Code.ContainsKey("edition"))
                {
                    object category = branch.Product.i_Attributes_Code("Category")[0].Translation.text(kyLanguage);
                    object edition = branch.Product.i_Attributes_Code("edition")[0].Translation.text(kyLanguage);

                    string key1 = null;
                    string key2 = null;

                    if (string.Equals(System.Convert.ToString(category), "Windows Server 2012", StringComparison.InvariantCultureIgnoreCase))
                    {
                        key1 = "W2012";
                    }
                    else if (string.Equals(System.Convert.ToString(category), "Windows Server 2012 R2", StringComparison.InvariantCultureIgnoreCase))
                    {
                        key1 = "W2012R2";
                    }

                    if (edition.ToLower().StartsWith("standard"))
                    {
                        key2 = "STD";
                    }
                    else if (edition.ToLower().StartsWith("essentials"))
                    {
                        key2 = "ESS";
                    }
                    else if (edition.ToLower().StartsWith("datacenter"))
                    {
                        key2 = "DAT";
                    }

                    if (!(key1 == null) && !(key2 == null))
                    {
                        System.Char key = string.Format("{0}_{1}", key1, key2);

                        if (osEdition == null || osCategory == null)
                        {
                            osEdition = System.Convert.ToString(edition);
                            osCategory = System.Convert.ToString(category);
                            osKey = key.ToString();
                        }
                        else
                        {
                            if (string.Compare(key.ToString(), osKey, true) != 0)
                            {
                                osEdition = null; // No common edition info can be displayed
                                osCategory = null;
                                osKey = null;
                                break;
                            }
                        }
                    }

                }
            }
        }

        if (!string.IsNullOrEmpty(osKey))
        {

            if (iq.ROKAttributes.ContainsKey(osKey))
            {

                object attributes = iq.ROKAttributes(osKey);

                object attrLicence = attributes.Where(a => a.Code == "licences").FirstOrDefault();
                object attrVirt = attributes.Where(a => a.Code == "virtualisation").FirstOrDefault();
                clsROKAttribute attrCals = attributes.Where(a => a.Code == "cals").FirstOrDefault();
                clsROKAttribute attrCpus = attributes.Where(a => a.Code == "maxcpus").FirstOrDefault();
                clsROKAttribute attrUsers = attributes.Where(a => a.Code == "maxusers").FirstOrDefault();
                clsROKAttribute attrRam = attributes.Where(a => a.Code == "maxram").FirstOrDefault();

                object lang = pbi.buyerAccount.Language;

                string licence = string.Empty;
                string virt = string.Empty;
                string cals = string.Empty;
                string maxCpus = string.Empty;
                string maxUsers = string.Empty;
                string maxRam = string.Empty;

                if (!(attrLicence == null))
                {
                    licence = System.Convert.ToString(attrLicence.Translation.textTranslation(lang));
                }
                if (!(attrVirt == null))
                {
                    virt = System.Convert.ToString(attrVirt.Translation.textTranslation(lang));
                }
                if (!(attrCals == null))
                {
                    cals = System.Convert.ToString(attrCals.Translation.textTranslation(lang));
                }
                if (!(attrCpus == null))
                {
                    maxCpus = System.Convert.ToString(attrCpus.Translation.textTranslation(lang));
                }
                if (!(attrUsers == null))
                {
                    maxUsers = System.Convert.ToString(attrUsers.Translation.textTranslation(lang));
                }
                if (!(attrRam == null))
                {
                    maxRam = System.Convert.ToString(attrRam.Translation.textTranslation(lang));
                }

                string title = osCategory + " " + osEdition;
                string table = null;
                table = string.Format("<span class=\'leftcol\'><span class=\'subtitle\'>{0}: </span>{1}</span><span class=\'rightcol\'><span class=\'subtitle\'>{2}: </span>{3}</span><br/>",
                    Xlt("Licenses", lang), licence, Xlt("Virtualisation", lang), virt);
                table += string.Format("<span class=\'leftcol\'><span class=\'subtitle\'>{0}: </span>{1}</span><span class=\'rightcol\'><span class=\'subtitle\'>{2}: </span>{3}</span><br/>",
                    Xlt("CALs", lang), cals, Xlt("Max. CPUs", lang), maxCpus);
                table += string.Format("<span class=\'leftcol\'><span class=\'subtitle\'>{0}: </span>{1}</span><span class=\'rightcol\'><span class=\'subtitle\'>{2}: </span>{3}</span><br/>",
                    Xlt("Max. Users", lang), maxUsers, Xlt("Max. RAM", lang), maxRam);

                Literal infoDisplay = new Literal();
                infoDisplay.Text = string.Format("<div class=\'quickFilterInfo\'><span class=\'title\'>{0}</span><br/><br/>{1}</div><br/>", title, table);

                returnValue = infoDisplay;

            }
        }

        return returnValue;
    }

    public dynamic Insert(ref List<string> errormessages)
    {

        return new clsBranch(this.Product, this.Parent, this.Translation, this.Picture, this.CollectiveNoun, this.collectiveNounSingular, this.Matrix, this.order, this.Hidden, this.rca, null, -1);

    }

    public string AsTextRecursive(int level)
    {
        string returnValue = "";

        returnValue = ""; //new (possibly not requeired)
        returnValue += Strings.StrDup(level, "    ") + this.Translation.text(English) + "\r\n";

        foreach (var c in (from cb in childBranches.Values orderby cb.order select cb)) //Me.childBranches.Values
        {
            returnValue += System.Convert.ToString(c.AsTextRecursive(level + 1));
        }

        return returnValue;
    }

    public clsQuantity LocalisedQuantity(clsRegion region, object path, List<string> errorMessages)
    {
        clsQuantity returnValue = default(clsQuantity);

        //    Pmark("LocalisedQuantity")
        //        Try

        returnValue = null;

        //this could be speeded up by adding a dictioanary of region to path/quanity - but it's probably unnecessary

        clsRegion bubbleRegion = region;

        //IF there are ONLY pathed Quanitites and none of them apply - return NOTHING - NOT! an

        do
        {
            if (this.Quantities != null)
            {
                foreach (var qty in this.Quantities.Values) //branches now carry quite a small number of quanities.. becuase of the neat way we do regions
                {
                    if (!qty.deleted)
                    {
                        //   If Not qty.IsAutoAdd Then  'ClsQuantity handes autoadds AND regionsalisation - but a branch with an autoadd is NOT necessarilt restircted to only that region
                        if (qty.Region == bubbleRegion)
                        {
                            //   If qty.NumPreInstalled <> 0 Then Stop
                            if (Strings.LCase(System.Convert.ToString(qty.Path)) == Strings.LCase(System.Convert.ToString(path)) || qty.Path == "")
                            {
                                //If qty.Path = path$ Then
                                //  If qty.Path <> "" Then Stop
                                // If Me.Product.ProductType.Code <> "SVR" Then Stop
                                if (qty.Path != "" || returnValue == null) //don't think this is qute right - because we want an explicit match to override a blank path
                                {
                                    returnValue = qty;
                                    if (qty.Path != "")
                                    {
                                        goto endOfDoLoop; //we're done (becuase this item has specific scope (and overrdies any quantity present with an empty path (global scope)))
                                    }
                                }
                            }
                            else if (qty.Path != "")
                            {
                                //   Beep()
                                // qty's on the branch but whos path does not match . .
                                //  Dim nmq$ = PathName(qty.Path)
                                //   nmq$ = ""

                                //   Debug.Print(qty.Path & iq.Branches(Split(qty.Path, ".").Last).DisplayName(English))
                            }
                        }
                    }
                    //  End If
                }
            }
            else
            {
                string test = "";
            }

            if (bubbleRegion == r_worldwide)
            {
                break; //we've reached the top
            }

            if (bubbleRegion.Parent == null)
            {
                errorMessages.Add("* region " + bubbleRegion.Name.text(English) + "(" + bubbleRegion.Code + ") is detached (not connected to XW (r_worldwide) - See LocalisedQuantity())");
                break; //some region is detached - we couldn't reach the root from it
            }

            bubbleRegion = bubbleRegion.Parent;

        } while (returnValue == null);
    endOfDoLoop:
        1.GetHashCode(); //VBConversions note: C# requires an executable line here, so a dummy line was added.

        //Catch ex As Exception
        //    Beep()
        // Finally

        //   Pacc("LocalisedQuantity")
        // End Try

        return returnValue;
    }

    /// <summary>
    ///  Determines branch visiblity (at this path) for the buyer account - based on Focus, Geography, Available pricing, Active Dates, EOL, Presence in the feed, "AlsoHost" etc.
    /// </summary>
    /// <remarks>Does NOT call the webservice
    /// </remarks>
    public List<string> ReasonsForHide(clsAccount buyeraccount, HashSet<string> foci, object path, int priceconfig, bool exitEarly, ref List<string> errorMessages)
    {
        List<string> returnValue = default(List<string>);

        //TODO if we're not in showall mode.. exit asap (as soon as we have a reason)
        //This function never calls the webservice

        returnValue = new List<string>();

        if (this.Product == null)
        {
            return returnValue;
        }

        if (this.deleted)
        {
            returnValue.Add("Branch is deleted");
        }

        if (!this.Product.Active)
        {
            returnValue.Add("Product is not Active");
        }
        if (!this.Product.Publish)
        {
            returnValue.Add("Product is not Published (was AAonly)");
        }

        if (DateTime.Now < this.Product.activeFrom)
        {
            returnValue.Add("Product is NOT YET active - activeFrom " + this.Product.activeFrom);
        }
        if (DateTime.Now > this.Product.activeTo)
        {
            returnValue.Add("Product is NO LONGER active - activeTo " + this.Product.activeTo);
        }

        if (exitEarly && returnValue.Count > 0)
        {
            return returnValue;
        }

        if (!this.HasSKU())
        {
            return returnValue; //some placeholding branches (for example families) have products but not SKUS
        }
        else
        {

            //***** temporary - default to HPE for Pre-Split (undefined) accounts - REMOVE
            if (string.IsNullOrEmpty(System.Convert.ToString(buyeraccount.mfrCode)))
            {
                buyeraccount.mfrCode = "HPE";
            }
            //*******************

            if (!string.IsNullOrEmpty(System.Convert.ToString(Product.mfrCode)))
            {
                if (!(Product.Manufacturer == buyeraccount.Manufacturer))
                {
                    returnValue.Add("Wrong Company (HPI/E)");
                }
            }

            List<string> rh = this.inFocus(foci);
            //If rh.Count > 0 Then Stop
            returnValue.AddRange(rh);

            if (exitEarly && returnValue.Count > 0)
            {
                return returnValue;
            }

            //Dim channelSKU$
            //channelSKU$ = buyeraccount.SellerChannel.ChannelSKU(Me.Product, iq.StandardVariant)
            //channelSKU$ = Product.i_variants(buyeraccount.SellerChannel.ChannelSKU(Me.Product, iq.StandardVariant)
            //  If (buyeraccount.SellerChannel.priceConfig And 2) = 0 Then ' if They don't show list prices .. require it to be in their feed
            if (Product.i_Variants == null)
            {
                returnValue.Add("No Variants (not in anyones feed ?)");
            }
            if (!Product.i_Variants.ContainsKey(buyeraccount.SellerChannel.IsCloneOf))
            {
                //this seller channel has no variant of this product - is there a list price
                if (!Product.i_Variants.ContainsKey(HP))
                {
                    returnValue.Add("Not in the feed - AND *no* list prices (no Disti or HP variants)");
                }
                else
                {
                    bool haveListPriceForRegion = false;
                    foreach (var v in Product.i_Variants(HP))
                    {
                        if (v.Region.Encompasses(buyeraccount.SellerChannel.IsCloneOf.Region))
                        {
                            haveListPriceForRegion = true;
                        }
                        break;
                    }
                    if (!haveListPriceForRegion)
                    {
                        returnValue.Add("No list price in force for disti region (" + buyeraccount.SellerChannel.IsCloneOf.Region.Code + ")");
                    }
                }

            }
            else
            {
                if (Product.i_Variants(buyeraccount.SellerChannel.IsCloneOf).Count() == 0)
                {
                    returnValue.Add("Product is not in the feed (No ChannelSKU)");
                    if (exitEarly && returnValue.Count > 0)
                    {
                        return returnValue;
                    }

                    //  Exit Function
                }
                //End If
            }
        }

        if (this.Product.EOL && !this.Product.anyStock(buyeraccount.SellerChannel))
        {
            returnValue.Add("Product is End Of Life AND not in stock");
        }

        if (exitEarly && returnValue.Count > 0)
        {
            return returnValue;
        }

        //NB this adds to the list of reasons for hiding ...
        returnValue.AddRange(this.AvailableInRegion(buyeraccount, System.Convert.ToString(path), ref errorMessages));

        if (exitEarly && returnValue.Count > 0)
        {
            return returnValue;
        }

        if (returnValue.Count > 0)
        {
            return returnValue;
        }

        //IF you have a webservice then (potential) visbility is ONLY a function of 'infeed'
        if ((priceconfig & 8) > 0)
        {
            if (Product.inFeed(buyeraccount.SellerChannel.IsCloneOf))
            {
                return returnValue;
            }
        }
        else
        {
            List<clsPrice> prices = this.Product.GetPrices(buyeraccount, priceconfig, iq.AllVariants, errorMessages, false);
            if (prices.Count == 0)
            {
                returnValue.Add("No price for Product - Priceconfig:" + buyeraccount.SellerChannel.priceConfig);
            }
        }

        return returnValue;
    }
    /// <summary>Checks if a branch is avbailable (by region) (ie. is not restricted for this sellerchannels region.</summary>
    /// <returns>"" if the branch should be displayed (or a HideReaon of the branch should be supressed</returns>
    /// <remarks>Also checks the products set of ALSOHOST attributes (which overried geographical restrictions)</remarks>
    public List<string> AvailableInRegion(clsAccount buyeraccount, string path, ref List<string> errorMessages)
    {
        List<string> returnValue = default(List<string>);

        returnValue = new List<string>(); //this is the list of regionalisation reasons NOT to display a product
        bool skipGeography = false;
        if (this.Product.i_Attributes_Code.ContainsKey("alsoHost"))
        {
            System.Boolean j = from h in this.Product.i_Attributes_Code("alsoHost") where h.Translation.text(English) == buyeraccount.SellerChannel.Code select h;
            if (j != null)
            {
                return returnValue; //This product *is* visible becuase of AlsoHost
            }
        }

        bool geoRestrictions = false; //Are there any purely geographic restrictions (ie. non auto adds)
        foreach (var q in this.Quantities.Values)
        {
            if (q.NumPreInstalled == 0)
            {
                geoRestrictions = true;
            }
        }

        //The seller wasn't listed in AlsoHosts)
        //Quantity records should be thought of as restrictions (of minimum installed, Min Increment, Preferred increment etc.. if none is present there is no limitation !
        clsQuantity qty = default(clsQuantity);
        if (!geoRestrictions) //Me.Quantities.Count = 0 Then
        {
            //There are *no* quantity restrictions (most branches have none)
            return returnValue;
        }
        else
        {
            // If Me.Product IsNot Nothing And Me.Product.ProductType.Code = "wty" Then Stop
            //gets the most appropriate quantity record for this sellers region/country
            qty = this.LocalisedQuantity(buyeraccount.SellerChannel.Region, path, errorMessages);
            if (qty == null)
            {
                //this branch has quantities - but none appropriate for this sellers region
                returnValue.Add("Product will not appear in this REGION " + buyeraccount.SellerChannel.Region.Code + " (no localised Quantity record) - " + buyeraccount.SellerChannel.Region.Displayname(English) + " Path:" + path);
                //For Each q In Me.Quantities.Values
                // AvailableInRegion.Add("Region:" & q.Region.Name.text(English) & " (" & q.Region.Code & ")")
                // Next
            }
            else
            {
                //A minIncrement of 0 in a most appropriate localised quantity disables the product (for that region)
                if (qty.MinIncrement == 0)
                {
                    returnValue.Add("Product is explicitly disallowed in this region by a minIncrement of  0 at the " + qty.Region.Code + " level. " + buyeraccount.SellerChannel.Region.Displayname(English) + "Branchid:" + System.Convert.ToString(this.ID) + " Path:" + path);
                }
                else
                {
                    return returnValue;
                }
            }
        }

        return returnValue;
    }

    public List<clsQuantity> GetPreInstalledRecursive(clsRegion region, object path, ref List<string> errorMessages)
    {
        List<clsQuantity> returnValue = default(List<clsQuantity>);

        //Returns a dictionary of the (relative) paths of all descendant, pre-installed quantities - with quanities thereof
        //Remember a branch can be grafted in multiple places - and the quantities have a path (which must match, or be blank)
        //Each branch will carry (typically) many quanities... most of which will be irrelevant becuase their paths wont match

        // If (iq.Cache.ContainsKey(region.ID) AndAlso iq.Cache(region.ID).ContainsKey(path$)) Then Return iq.Cache(region.ID)(path$)


        returnValue = new List<clsQuantity>();

        clsQuantity q = default(clsQuantity);

        //this branch has many quanity records - find the single 'best' (geographically narrowest)
        if (region == null)
        {
            //we'e just compiling a list of all quanities (for the PreINstalled table for debugging/product maintenance)
            //the child rbanches are grafted in many places and so carry lot of quanitites that are not relevant to this location (path)
            foreach (var l in this.Quantities.Values)
            {
                if (!l.deleted)
                {
                    if (l.Path == path || l.Path == "") //this was mysteriously commented out - nick reinsttated
                    {
                        returnValue.Add(l);
                    }
                }
            }

        }
        else
        {
            q = this.LocalisedQuantity(region, path, errorMessages); //returns the 'best' quantity record for this user - ie, the deepest, narrowest match
            if (!(q == null))
            {

                if (Strings.LCase(System.Convert.ToString(path)) == Strings.LCase(System.Convert.ToString(q.Path)) || q.Path == "") //@@@
                {
                    //     If q.NumPreInstalled > 0 Then
                    //If Not q.deleted Then
                    //WE DO want to include deleted items in the preinstalled table (otherwise we have no way to undelete them !)
                    returnValue.Add(q);
                    //End If
                    //End If
                }
            }
        }


        //For Each q In Me.Quantities.Values
        //If q.Region Is region Then



        foreach (var child in this.childBranches.Values)
        {
            //Dim j As List(Of clsQuantity)

            returnValue.AddRange(child.GetPreInstalledRecursive(region, path + "." + (child.ID).ToString().Trim(), errorMessages)); //recursively find the child branches preinstalled options - and append them to the dictionary that is ultimately returned

            //    j = branch.GetPreInstalledRecursive(region, path$ & "." & Trim$(CStr(branch.ID)), errormessages) 'recursively find the child branches preinstalled options - and append them to the dictionary that is ultimately returned
            //For Each v In j
            // GetPreInstalledRecursive.Add(v)
            // Next
        }

        //If Not iq.Cache.ContainsKey(region.ID) Then iq.Cache.Add(region.ID, New Dictionary(Of String, List(Of clsQuantity)))
        //If Not iq.Cache(region.ID).ContainsKey(path$) Then iq.Cache(region.ID).Add(path$, GetPreInstalledRecursive)

        return returnValue;
    }

    /// <summary>
    /// Returns "" if its OK to show the product (it's in focus) - otherwise return the HideReason
    /// </summary>
    /// <param name="foci"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public List<string> inFocus(HashSet<string> foci)
    {
        List<string> returnValue = default(List<string>);

        returnValue = new List<string>(); //list of reasons NOT to display

        if (foci.Count == 0)
        {
            return returnValue; //We're not focussing on anything (in particular)
        }
        else
        {
            if (this.Product.i_Attributes_Code.ContainsKey("focus"))
            {
                foreach (var focus in this.Product.i_Attributes_Code("focus"))
                {
                    string ft = System.Convert.ToString(focus.Translation.text(English));

                    if (!foci.Contains(ft))
                    {
                        returnValue.Add("You are not focussing on " + ft);
                    }
                }
            }
        }

        //'NB this is a list so we can look at SmartBuy AND receta (at the same time) for example
        //If Not Me.Product.i_Attributes_Code.ContainsKey("focus") Then
        //    inFocus.Add("Product does not have a focus attribute (receta etc.) - you're currenctly focusing on:" & Join(foci.ToArray, ","))
        //    Exit Function
        //End If

        //Dim j = From v In Me.Product.i_Attributes_Code("focus") Select v.Translation.text(English)
        //Dim l As List(Of String) = j.ToList

        //If l.Intersect(foci).Count > 0 Then
        //    'all good - one focus of the product matches that of the session
        //    Exit Function
        //Else
        //    inFocus.Add("Product not in focus -  Product foci:" & Join(l.ToArray, ",") & " current foci:" & Join(foci.ToArray, ","))
        //End If
        //        End If

        return returnValue;
    }

    public string displayName(clsLanguage language)
    {

        return this.Translation.text(language);

    }

    //Public Function HasPriceRecursive(buyerAccount As clsAccount) As Integer


    //    'Returns 1 if any descendant branch carries a product which has a price (according to priceconfig)
    //    'returns -1 if if hits a system for which we have no price
    //    'returns 0 (or keeps processing) if there is no price

    //    'NOTE: - this stops recursing at any system for which there is no price to prevent higher level branches
    //    '        (families, supply chains, systems) from being visible where an option shared with anouther system *does* have a price

    //    '      If Me.HasSKU = False Then Return False 'todo - remove (was needed as some placehol;ders had acquired pricing)

    //    HasPriceRecursive = False

    //    If Not Me.Product Is Nothing Then
    //        If Me.Product.GetPrices(buyerAccount, buyerAccount.SellerChannel.priceConfig, iq.AllVariants).Count > 0 Then
    //            Return 1
    //        Else
    //            'Return False 'we dont recurse through non priced items (yet)
    //            If Me.Product.isSystem Then Return -1 ' see NOTE - we need to toatally bail here (somehow)
    //        End If
    //    End If

    //    For Each child In Me.childBranches.Values

    //        Dim r As Integer = child.HasPriceRecursive(buyerAccount)
    //        If r <> 0 Then Return r

    //    Next

    //End Function


    public string minorKeywords(clsLanguage language)
    {
        string returnValue = "";

        //Minor Keywords come from the branch.product
        returnValue = "";

        if (!(this.Product == null))
        {

            if (this.Product.i_Attributes_Code.ContainsKey("subTitle"))
            {
                returnValue += System.Convert.ToString(this.Product.i_Attributes_Code("subTitle")[0].Translation.text(language));
            }

            if (this.Product.i_Attributes_Code.ContainsKey("desc"))
            {
                if (this.Product.i_Attributes_Code("desc")[0].Translation != null)
                {
                    returnValue += " " + this.Product.i_Attributes_Code("desc")[0].Translation.text(language);
                }

            }

            returnValue += " " + this.Product.ProductType.Code; //Allow searhing by things like SSD and HDD

        }

        return returnValue;
    }

    public string Majorkeywords(clsLanguage language)
    {
        string returnValue = "";

        //Indexes the branch.translation - which it either a category/family name or a SKU
        returnValue = System.Convert.ToString(this.Translation.text(language));

        return returnValue;
    }

    public Panel BuyUI(clsAccount buyeraccount, object Path, UInt64 lid, bool searchResults = false) //  , skud As Boolean, matrix As clsBranch, filter As String, sort As String) As PlaceHolder
    {

        List<string> errorMessages = new List<string>();
        //returns customer facing UI for price and stock (including flex buttons),
        // Pricing and stock for ALL variants of a SKU

        Panel ui = new Panel();


        //Print("hello")
        //ui.ID = "Prices_" & Me.Product.ID
        ui.ID = "buyUI_" + System.Convert.ToString(Path); //this may not be distinct enough
        if (searchResults)
        {
            ui.CssClass = "buyUISearch";
        }
        else
        {
            ui.CssClass = "buyUI";
        }

        //ui.Attributes("style") = "" 'we have to explicitly position relative - so we can subsequently specify a left postion for child elements

        List<clsPrice> prices = this.Product.GetPrices(buyeraccount, buyeraccount.SellerChannel.IsCloneOf.priceConfig, iq.AllVariants, errorMessages, true);

        if (prices == null)
        {
            errorMessages.Add("* Missing prices");
            ui.Controls.Add(NewLit("wait"));

        }
        else
        {
            TextBox tb_qty = default(TextBox);

            Panel vpanel = default(Panel); //a panel for each variant
            Panel vnp; //within that - a panel for each variant name
            Panel pp = default(Panel); // a price panel
            Panel sp = default(Panel);
            Panel qp = default(Panel);

            int vn = 0;

            Label vnl = default(Label);
            foreach (var price in prices) //For this one product there can be more than one price/stock (one per variant!)
            {

                //supress the test variants for any non admin user
                if (!(price == null)) //POAs are 'NOthings' in the list of prices
                {
                    if (price.SKUVariant.Code != "TST" || (price.SKUVariant.Code == "TST" && buyeraccount.HasRight("SEETEST")))
                    {

                        vpanel = new Panel();
                        vpanel.ID = "v_" + System.Convert.ToString(vn) + "." + System.Convert.ToString(Path);
                        vpanel.CssClass = "buyUIvariant";
                        // vpanel.Attributes("style") = "left:" & vn * 20 & "em;"
                        ui.Controls.Add(vpanel);

                        vn++;
                        if (prices.Count > 1)
                        {
                            //vnp = New Panel
                            vnl = new Label();
                            //vnp.Controls.Add(vnl)
                            vpanel.Controls.Add(vnl);
                            vnl.Text = price.SKUVariant.Code; //Variant
                            vnl.BackColor = Drawing.Color.Blue;
                            vnl.ForeColor = Drawing.Color.White;
                            vnl.ToolTip = price.SKUVariant.displayName(buyeraccount.Language);
                        }

                        //PRICE  - TODO - supress/display differently prices whose variants are deleted ??? makes no sense
                        pp = new Panel();
                        sp = new Panel();
                        vpanel.Controls.Add(pp);
                        if (searchResults)
                        {
                            pp.Attributes("class") = "buyUIprSearch"; //float:left
                            sp.CssClass = "buyUIstSearch";
                        }
                        else
                        {
                            pp.Attributes("class") = "buyUIprice"; //float:left
                            sp.CssClass = "buyUIstock";
                        }


                        //returns a DIV with the ID P_Product.id_Price.ID,  containing a label with tooltip info -
                        //NB: = it has the cssClass Refresh, so that it can be updated after a webservie call (see FillPrices)
                        pp.Controls.Add(price.Ui(buyeraccount, 1, lid));

                        //STOCK - we show the stock of the variant we showed the price of - note buyUi may return many panels (one for each variant)

                        vpanel.Controls.Add(sp);
                        if (price.SKUVariant == null)
                        {
                            errorMessages.Add("* Price " + price.ID + " SkuVariant was nothing");
                        }
                        else
                        {
                            if (price.SKUVariant.Product == null)
                            {
                                errorMessages.Add("* Price " + price.ID + " SKUVariants product was nothing");
                            }
                            else
                            {
                                if (price.SKUVariant.shipments.Count == 0)
                                {
                                    //dont' show any indication of stock is these are no shipments at all

                                }
                                else
                                {
                                    Label sl = new Label();


                                    sp.Controls.Add(price.SKUVariant.StockUI(1, string.Empty, buyeraccount.Language, buyeraccount.SellerChannel)); //returns a DIV with the ID S_ Price.ID,  containing a label with tooltip info
                                    if (!buyeraccount.SellerChannel.BinaryStock)
                                    {
                                        sl.Text = "&nbsp; " + Xlt("in stock", buyeraccount.Language);
                                    }
                                    else
                                    {
                                        sl.Text = "&nbsp; ";
                                    }
                                    sp.Controls.Add(sl);
                                }
                            }
                        }

                        bool quoteLocked = false;
                        if (iq.sesh(lid, "QuoteLocked") != null)
                        {
                            quoteLocked = System.Convert.ToBoolean(iq.sesh(lid, "QuoteLocked"));
                        }

                        if (!price.SKUVariant.Deleted)
                        {

                            // If there is a current quote, work out whether it's HPI or HPE
                            clsQuote quote = default(clsQuote);
                            object quoteSplit = Manufacturer.Unknown;
                            if (iq.sesh(lid, "QuoteID") != null)
                            {
                                if (iq.sesh(lid, "AgentAccount") != null)
                                {
                                    clsAccount agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));
                                    quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"));
                                    quoteSplit = quote.QuoteSplit;
                                }
                            }

                            // Work out whether adding is enabled according to the HPE/HPI split
                            bool addEnabled = true;
                            if (Product.isSystem(Path))
                            {
                                if (!(quoteSplit == Manufacturer.Unknown))
                                {
                                    addEnabled = quoteSplit == Product.Manufacturer;
                                }
                            }

                            // Set up the message to display if the user attempts to create a mixed quote
                            string splitMessage = string.Empty;
                            if (!addEnabled)
                            {
                                splitMessage = System.Convert.ToString(GetSplitMessage(quoteSplit, buyeraccount.Language));
                            }

                            qp = new Panel();
                            qp.Attributes("class") = "buyUIqty";
                            vpanel.Controls.Add(qp);
                            tb_qty = new TextBox();
                            tb_qty.ID = "qtytxt." + System.Convert.ToString(Path);
                            if (addEnabled)
                            {
                                tb_qty.CssClass = "qty UI";
                                tb_qty.Attributes.Add("onmousedown", "burstBubble(event);");
                            }
                            else
                            {
                                tb_qty.CssClass = "qtyDisabled UI";
                                tb_qty.ReadOnly = true;
                                tb_qty.Attributes.Add("onmousedown", string.Format("burstBubble(event); displayAddMsg(\'{0}\', \'{1}\');", tb_qty.ID, splitMessage));
                            }
                            qp.Controls.Add(tb_qty);

                            qp.Controls.Add(TreeAddButton(tb_qty, Path, this, price.SKUVariant, buyeraccount.Language, addEnabled, splitMessage));

                            if (UserIsAdmin(lid))
                            {
                                Literal lt = FunctionButton(Path, price.SKUVariant.ID, "deleteVariant&into=tree", "DEL", "Removes this variant from the feed" + "\r\n" + "(until it`s next loaded/refreshed)" + "\r\n" + "Generally not something you want to be doing!\'");
                                qp.Controls.Add(lt);
                            }
                        }
                    }
                }
            }

            OutputErrors(ui.Controls, errorMessages, lid);

            return ui;

        }

    }


    public clsBranch ChildNamed(object nm)
    {
        clsBranch returnValue = default(clsBranch);

        //NOTE: - its generally a very bad idea to navigate banches by name - as their names may change
        //This is used in CreateCarePacks (and called from ensurePath)

        returnValue = null;
        foreach (var child in this.childBranches.Values)
        {
            if (child.Translation.text(English) == nm)
            {
                return child;
            }
        }

        return returnValue;
    }

    public string SKU()
    {
        string returnValue = "";

        //could be speeded up by pre-loading into a string property

        returnValue = "";
        if (!(this.Product == null))
        {
            if (this.Product.SKU != "")
            {
                returnValue = System.Convert.ToString(this.Product.SKU);
            }
        }

        return returnValue;
    }



    //Public ReadOnly Property Name As String
    //    Get
    //        If Me.Product IsNot Nothing Then
    //            If Me.Product.i_Attributes_Code.ContainsKey("~ame") Then
    //                Name = Me.Product.i_Attributes_Code("Name")(0).Translation.text(s_lang)
    //            Else
    //                Name = "branches product has no name"
    //            End If
    //        End If
    //    End Get
    //End Property


    public Table SlotsTable(clsBranchInfo bi)
    {

        //returns HTML UI showing slots
        List<string> errormessages = new List<string>();
        //Got a quote
        clsQuote quote = default(clsQuote);
        if (iq.sesh(bi.lid, "QuoteID") != null)
        {
            quote = bi.agentAccount.Quotes(iq.sesh(bi.lid, "QuoteID"));
            if (quote.RootItem.Children.Count == 0)
            {
                quote.LoadItems(errormessages);
            }
        }

        //Show any slots
        Table atable = new Table();
        atable.CssClass = "adminTable";

        if (this.slots.Values.Count > 0)
        {

            string help = "Broad slot type (many validations are only performed agains this)|";
            help += "Narrow slot type (some PCI validations are performed against this)|";
            help += "description|";
            help += "Number of slots (+given/-taken)|";
            help += "specific slot number (used for *some* options which only go in specific slots in a given system)|";
            help += "path at which this slot is \'active\' (or empty if it applies everywhere)|";
            help += "Notes (to user)|";
            help += "User MUST fill this many of this slot|";
            help += "You may \'soft delete\' slots (and undelete them if you change your mind)";

            TableHeaderRow thr = MakeTHR("Major,Minor,Description,NumSlots,SlotNum,path,notes,RequiredFill,Del", help, "");
            atable.Controls.Add(thr);


            TableRow tr = default(TableRow);
            TableCell tc = default(TableCell);
            foreach (var slot in this.slots.Values)
            {

                tr = new TableRow();

                atable.Rows.Add(tr);
                if (slot.deleted)
                {
                    tr.CssClass += " deletedRow";
                }
                if (slot.path != "" && !bi.path.Contains(slot.path))
                {
                    tr.Attributes.Add("style", "text-decoration:line-through;");
                }

                tc = new TableCell();
                tc.Text = slot.Type.MajorCode;
                tr.Controls.Add(tc);

                tc = new TableCell();
                tc.Text = slot.Type.MinorCode;
                tr.Controls.Add(tc);

                tc = new TableCell();
                tc.Text = slot.Type.Translation.text(s_lang);
                tr.Controls.Add(tc);

                tc = new TableCell();
                tc.Text = (slot.numSlots).ToString();
                tr.Controls.Add(tc);

                tc = new TableCell();
                if (slot.slotNum == null)
                {
                    tc.Text = "Undefined";
                }
                else
                {
                    tc.Text = slot.slotNum.sqlvalue;
                }
                tr.Controls.Add(tc);

                tc = new TableCell();

                if (slot.path == bi.path)
                {
                    tc.Text = "Only here";
                }
                else if (slot.path == "")
                {
                    tc.Text = "Everywhere";
                }
                else
                {
                    tc.Text = "Not here";
                }
                tc.ToolTip = slot.path;
                tr.Controls.Add(tc);

                tc = new TableCell();
                if (!(slot.notes == null))
                {
                    tc.Text = slot.notes.text(s_lang);
                }
                tr.Controls.Add(tc);

                tc = new TableCell();
                tc.Text = (slot.requiredFill).ToString();

                tr.Controls.Add(tc);

                //If quote IsNot Nothing Then
                //    tc = New TableCell
                //    If quote.RootItem IsNot Nothing AndAlso quote.RootItem.Descendants IsNot Nothing Then
                //        For Each quoteItem In quote.RootItem.Descendants
                //            If quoteItem.dicslots IsNot Nothing Then
                //                If quoteItem.dicslots.ContainsKey(slot.NonStrictType) Then
                //                    tc.Text = quoteItem.dicslots(slot.NonStrictType).taken
                //                    Exit For
                //                End If
                //            End If
                //        Next
                //    End If
                //    tr.Controls.Add(tc)
                //End If

                tc = new TableCell();

                if (!slot.deleted)
                {
                    Literal lt = FunctionButton(bi.path, slot.ID, "deleteSlot", "DEL", "Delete this slot");
                    tc.Controls.Add(lt);
                }
                else
                {
                    Literal lt = FunctionButton(bi.path, slot.ID, "unDeleteSlot", "UNDEL", "Un-Delete this slot");
                    tc.Controls.Add(lt);
                }


                tr.Controls.Add(tc);


            }

            tr = new TableRow();
            atable.Rows.Add(tr);
            tc = new TableCell();
            tc.CssClass = "slotCell";
            tr.Controls.Add(tc);

            tc.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit these slots", bi.agentAccount.Language), "window.open(\'edit.aspx?path=Branches(" + System.Convert.ToString(this.ID) + ").slots&TreePath=" + bi.path + "&lid=" + bi.lid.ToString() + "\');return(false);", "", "width:25px;height:25px;", bi.buyerAccount.Language));


        }
        return atable;

    }

    //ByVal path As List(Of Integer)
    internal void Score(Dictionary<int, clsKwScore> branchScores, ref int[] path, int majorMatchedBits, int minorMatchedbits, int majorMatchCount, int minorMatchCount, ref int worst, int depth, ref Dictionary<string, int> PathScores, clsAccount buyeraccount, string searchType, bool crossSystems, UInt64 lid, int searchid, ref bool abandon, ref int cyclecount, object pth)
    {

        if (depth > 14)
        {
            return; //Dim l$ = PathName(pth$) : Stop
        }

        pth += this.ID + ".";


        //Each branch has been (pre) tested to see if it contains each of up to 32 keywords,
        //Each of which is represented by a single bit in the 'branchscores.MatchedBits property
        //BranchScores is an INPUT to this fuction, and never changes
        //PathScore is populated (through recursion) and contains a Path>Score

        //MinorMatches are in descriptions - Majors are in Names


        //matchedBits is the 'working variable' and has its bits set for the 'current' position in the tree
        //we keep track of the recursion depth and use an BYREF array of integers to track path to this point

        //This is a speedup - but at the cost of possbile
        // If PathScores.Count > 1000 Then Exit Sub

        path[depth] = this.ID;
        //matchedBits = matchedBits Or Me.Matches 'Bitwise logical OR mask with the matches of the branch
        // points = points + Me.Points

        if (abandon)
        {
            return;
        }

        object bn = this.Translation.text(English).ToLower;
        if (bn.Contains("accessories and"))
        {
            return;
            //Beep()
        }

        cyclecount++;
        if (cyclecount == 100)
        {
            int ssid = System.Convert.ToInt32(iq.sesh(lid, "searchID"));
            if (ssid != searchid) //abandon
            {
                abandon = true;
                return;
            }
            cyclecount = 0;
        }

        if (branchScores.ContainsKey(this.ID))
        {

            //Bitwise logical OR mask with the all the matches of the branch
            majorMatchedBits = majorMatchedBits | branchScores(this.ID).majorMatchBits;
            minorMatchedbits = minorMatchedbits | branchScores(this.ID).minorMatchbits;

            majorMatchCount += System.Convert.ToInt32(branchScores(this.ID).MajorMatchCount); //sum the points (total number of matches) of each branch
            minorMatchCount += System.Convert.ToInt32(branchScores(this.ID).MinorMatchCount); //sum the points (total number of matches) of each branch

            //matechedbits carries matches of frags 'down' to this point in the tree - so *everything* under a 'SSD' category for example
            //Scores a 1 for SSD (even if it itself doesnt have SSD in its (own) .Keyswords)

            // Dim bc As Integer
            // bc = BitCount(majorMatchedBits)  'this is the matched bits down to this point in the tree
            int score = 0;
            score = System.Convert.ToInt32(BitCount(majorMatchedBits) * 200 / (depth + 1) + majorMatchCount * 3);
            score += System.Convert.ToInt32(BitCount(minorMatchedbits) * 100 / (depth + 1) + minorMatchCount);

            //If (score >= worst) Or (score = worst And results.Count < 20) Then
            if (score > 0)
            {
                string abc = "";
            }

            //worst' is initially 0 so only things with *some* score will get added

            if (score > 0 & score >= worst || PathScores.Count < 1000)
            {

                string rp = "";
                for (i = 1; i <= depth; i++)
                {
                    rp += "." + path[i].ToString();
                }

                PathScores.Add(rp, score);

                System.Object topPathScores = from r in PathScores orderby r.Value descending select r;
                if (PathScores.Count > 1024) //64 Then  'if we get to more than 64 results .. keep only the 32 'best'
                {

                    Dictionary<string, int> sorted = new Dictionary<string, int>();
                    foreach (var kvp in topPathScores.Take(32))
                    {
                        sorted.Add(kvp.Key, kvp.Value); //values are scores
                    }
                    worst = System.Convert.ToInt32(sorted.Values.Last); //the worst of the current scorers (futures scores must be better than this to get added)
                    PathScores.Clear();
                    PathScores = sorted;
                }
            }
        }
        else
        {
            //many branches are not in branchscores (for example hidden chassis branches)
        }


        //@@@ - systems only search (don't recurse through systems)
        if (!crossSystems)
        {
            if (this.Product != null && this.Product.isSystem)
            {
                return;
            }
        }

        //Branches are unfiltered at this point - there's a very real danger that none of our 64 results are in the distis feed
        foreach (var branch in this.childBranches.Values)
        {
            if (branch == this)
            {
                Debugger.Break(); //circular refernece - very bad
            }
            bool include = true;
            if (branch.Product != null)
            {

                if (branch.Product.isSystem) //If the system is not in the feed we do not recurse into the otpions
                {

                    if (searchType == "priced")
                    {

                        if ((!branch.Product.inFeed(buyeraccount.SellerChannel)) && (!branch.Product.HasListPrice(buyeraccount)))
                        {
                            include = false;
                        }

                    }
                }
            }

            if (include)
            {

                bool dnr = false;
                for (i = 1; i <= depth + 1; i++)
                {
                    if (path[i] == branch.ID)
                    {
                        object l = PathName(pth + branch.ID); //Ut oh - there's a loop in the tree
                        dnr = true; //Don not recurse
                    }
                }

                if (!dnr)
                {
                    branch.Score(branchScores, path, majorMatchedBits, minorMatchedbits, majorMatchCount, minorMatchCount, worst, depth + 1, PathScores, buyeraccount, searchType, crossSystems, lid, searchid, abandon, cyclecount, pth);
                }

            }
            if (abandon)
            {
                break;
            }
        }

    }

    /// <summary>Grafts the supplied source branch onto this instance (the target branch) (making it a child thereof)</summary>
    /// <param name="sourceBranch">The branch being grafted (stuck on)</param>
    /// <param name="Source">Audit trail/data source text (Who made this graft)</param>
    /// <param name="writecache"></param>
    /// <returns></returns>
    /// <remarks>Does *not* make this branch the parent of the source branch..(otherwise a branch would need multiple parents..The parent chain cannot be navigated through grafts )</remarks>
    public bool Graft(clsBranch sourceBranch, string Source, string path, List<string> errorMessages, DataTable writecache = null)
    {
        bool returnValue = false;

        //make sure the target is not already a descendant of the source  (circular reference)

        returnValue = false;

        List<clsBranch> descendants = default(List<clsBranch>);
        if (writecache == null) //For imports, where we use bulk write, Don't check for ciruclar references (becuase it's expensive)
        {
            descendants = sourceBranch.Descendants();
        }
        else
        {
            descendants = new List<clsBranch>();
        }

        //if any of the target branches children have a SKU and the source branch doesn't
        // If childBranches.Where(Function(a) a.Value.HasSKU() <> sourceBranch.HasSKU()).Count() > 0 Then
        // errorMessages.Add("!Mixed Branch Error - You can't mix the type, SKU and Category under this branch")
        // Exit Function
        // End If

        if (descendants.Contains(this))
        {
            errorMessages.Add("!Circular Reference Error - You can\'t graft onto a branch that is contained within the branch you are grafting (or very bad things would happen)");
            return returnValue;
        }
        else
        {

            if (path == "") //we allow multiple grafts of the same source branch IF a path is specified
            {
                if (this.childBranches.Values.Contains(sourceBranch))
                {
                    errorMessages.Add("!Error - The branch you\'re trying to graft is already a child of this branch - you probably want to make a copy instead");
                    return returnValue;
                }
            }


            if (!sourceBranch.AllParents.ContainsKey(this.ID))
            {
                sourceBranch.AllParents.Add(this.ID, this);
            }
            bool blnAddRecords = false;

            if (!this.childBranches.ContainsKey(sourceBranch.ID))
            {
                this.childBranches.Add(sourceBranch.ID, sourceBranch);
                this.HasGrafts = true;
                blnAddRecords = true;
                if (path != "")
                {
                    sourceBranch.GraftedOnAt.Add(path);
                }
            }
            else if (path != "" && !sourceBranch.GraftedOnAt.Contains(path))
            {
                this.HasGrafts = true;
                sourceBranch.GraftedOnAt.Add(path);
                blnAddRecords = true;
            }
            if (blnAddRecords)
            {
                if (writecache == null)
                {
                    object sql = null;
                    sql = "INSERT INTO [graft] (fk_branch_id_target,fk_branch_id_source,created,source,path) VALUES (" + System.Convert.ToString(this.ID) + "," + System.Convert.ToString(sourceBranch.ID) + ",getdate()," + da.SqlEncode(Source) + "," + da.SqlEncode(path) + ");";
                    da.DBExecutesql(sql, true); //return the ID of the graft record
                }
                else
                {

                    System.Data.DataRow row = default(System.Data.DataRow);
                    row = writecache.NewRow();
                    row["FK_Branch_ID_Target"] = this.ID;
                    row["FK_Branch_ID_Source"] = sourceBranch.ID;
                    row["Path"] = path;

                    row["Created"] = DateTime.Now;
                    row["Source"] = Source;
                    row["marginsystems"] = "1";
                    row["marginoptions"] = "1";

                    writecache.Rows.Add(row);
                }
            }

        }

        return true;

    }

    public bool DeleteGraftedOnBranch(int sourceId)
    {

        da.DBExecutesql(string.Format("DELETE FROM [graft] WHERE [FK_Branch_ID_Source] = {0} AND [FK_Branch_ID_Target] = {1};", sourceId, this.ID));

    }


    public void IndexPaths(SqlClient.SqlConnection con, object path, object pathname, DataTable dt, DataTable segcache, int depth, ref int cc)
    {

        //Recurses all branches, adding the product ID and path - populates the datatable DT (ready for a fast bulk insert to SQL via an SP)
        //creates the Path table - allowing us to quickly find every occurence of a product in the tree

        path += "." + (this.ID).ToString().Trim();

        //do not recurse pruned branches !
        //If Not iq.Prunes.ContainsKey(path$) Then

        DataRow row = default(DataRow);
        //pathname$ &= " / " & Me.displayname


        //If Not Me.Product Is Nothing Then 'Not all branches carry a product
        row = dt.NewRow();
        row["Path"] = path;
        if (this.Product == null)
        {
            row["fk_product_id"] = DBNull.Value;
        }
        else
        {
            row["fk_product_id"] = this.Product.ID;
        }

        cc++; //this will tally with the path ID
        row["cc"] = cc;

        dt.Rows.Add(row);
        if (dt.Rows.Count % 50000 == 0)
        {
            Interaction.Beep();
            da.BulkWrite(con, segcache, "PathSegment");
            segcache.Rows.Clear();

            da.BulkWrite(con, dt, "Path");
            dt.Rows.Clear();
            Interaction.Beep();
        }


        string[] j = Strings.Split(System.Convert.ToString(path), ".");

        //the 0th element contained the literal 'tree'
        for (i = 1; i <= (j.Length - 1); i++)
        {
            row = segcache.NewRow;
            row["fk_path_id"] = cc;
            row["fk_branch_id"] = j[i];
            row["fk_translation_key"] = iq.Branches(int.Parse(j[i])).Translation.Key;
            row["order"] = i;
            segcache.Rows.Add(row);
        }

        //add any attributes of the product we want to index

        //however we recurse them all
        foreach (var child in this.childBranches.Values)
        {
            child.IndexPaths(con, path, pathname, dt, segcache, depth + 1, cc);
        }

        //Else

        //End If

    }

    public Dictionary<string, clsBranch> findSystemBranches(object path)
    {
        Dictionary<string, clsBranch> returnValue = default(Dictionary<string, clsBranch>);

        returnValue = new Dictionary<string, clsBranch>();

        if (!(this.Product == null))
        {
            if (this.Product.isSystem)
            {
                returnValue.Add(path, this);
            }
            return returnValue;
        }
        foreach (var child in this.childBranches.Values)
        {
            AppendDic((Dictionary<string, clsBranch>)returnValue, child.findSystemBranches(path + "." + (child.ID).ToString().Trim()));
        }

        return returnValue;
    }

    public Dictionary<string, Pair> findFamilyBranches(object Path) //(Of String, clsBranch))
    {
        Dictionary<string, Pair> returnValue = default(Dictionary<string, Pair>);

        //Only used during the power sizing import - generally NOT robust.
        //return a dictionary of SysFamily codes to a weakly typed pair of Path,Branch

        returnValue = new Dictionary<string, Pair>(StringComparer.CurrentCultureIgnoreCase);


        if (this.Product != null)
        {
            if (this.Product.i_Attributes_Code.ContainsKey("FamMajor"))
            {
                string famcode = System.Convert.ToString(this.Product.i_Attributes_Code("FamMajor")[0].Translation.text(English));
                returnValue.Add(famcode, new Pair(Path, this));
            }

            return returnValue;
        }

        foreach (var child in this.childBranches.Values)
        {
            AppendDic((Dictionary<string, Pair>)returnValue, child.findFamilyBranches(Path + "." + (child.ID).ToString().Trim()));
        }

        return returnValue;
    }


    /// <returns>The number of slots (on balance - ie. Gives-Takes) of a specified slotType</returns>
    public int slotsGiven(object path, clsSlotType slotType)
    {
        int returnValue = 0;

        //on balance!
        returnValue = 0;

        if (this.HasSKU()) //Only things with a SKU can give slots (otherwise the virtual chassis gets suggested to solve all sorts of slot shortfalls! )
        {
            string min = "";
            if (iq.StrictSlotValidation)
            {
                min = System.Convert.ToString(slotType.MinorCode);
            }

            int gives = 0;
            int takes = 0;

            //This is to 'fake' a dictionary with less strict slot types using NonStrictType from the slot, logic then remains the same so it can be switched on / off easily
            object ist = iq.StrictSlotValidation ? this.i_Slots : (this.slots.Values.ToList().GroupBy(iss => iss.NonStrictCompoundKey).ToDictionary(dk => dk.Key, dk => new clsSlot() { Type = dk.First.NonStrictType, numSlots = dk.Sum(dkchild => dkchild.numSlots) }));

            if (ist.ContainsKey(slotType.MajorCode + "_" + min + "_" + System.Convert.ToString(path) + "_1_null"))
            {
                gives = System.Convert.ToInt32(ist(slotType.MajorCode + "_" + min + "_" + System.Convert.ToString(path) + "_1_null").numSlots); //'find the 'gives ' slots -SPECIFICALY at this path (there can only be one of each type)
            }
            else if (ist.ContainsKey(slotType.MajorCode + "_" + min + "__1_null")) //look for Globally scoped gives slots (without a path)
            {
                gives = System.Convert.ToInt32(ist(slotType.MajorCode + "_" + min + "__1_null").numSlots);
            }

            if (ist.ContainsKey(slotType.MajorCode + "_" + min + "_" + System.Convert.ToString(path) + "_-1_null"))
            {
                takes = System.Convert.ToInt32(ist(slotType.MajorCode + "_" + min + "_" + System.Convert.ToString(path) + "_-1_null").numSlots); //'find the 'takes' slots -SPECIFICALY at this path (there can only be one of each type)
            }
            else if (ist.ContainsKey(slotType.MajorCode + "_" + min + "__-1_null")) //look for Globally scoped takes slots (without a path)
            {
                takes = System.Convert.ToInt32(ist(slotType.MajorCode + "_" + min + "__-1_null").numSlots);
            }

            returnValue = gives + takes; //NB: Takes are alread NEGATIVE so we ADD them (to find the number of slots given on balance)

        }

        return returnValue;
    }

    public Dictionary<string, int> findSlotGivers(object path, clsSlotType slotType, bool include = true)
    {
        Dictionary<string, int> returnValue = default(Dictionary<string, int>);

        //recusively builds a dictionary of path>NumSlots(given) for those systems/options giving slots of the specified type (the branch can be determined easiy from the last segment of the path)
        //Include' is used to supress the slot of the system unit itself (so we can easily find options that give slots - otherwsie it suggests we buy another system unit when we're out of slots !

        //TODO - we could build a 3d dictionary on the system branch when it's added to the basket of Path>SlotType>NumberGiven - which would allow faster resolution of slot overflows

        returnValue = new Dictionary<string, int>();
        if (include)
        {
            int slotsGiven = this.slotsGiven(path, slotType);
            if (slotsGiven > 0)
            {
                returnValue.Add(path, slotsGiven);
            }
        }

        //and recurse
        foreach (var child in this.childBranches.Values.ToArray)
        {
            AppendDic((Dictionary<string, int>)returnValue, child.findSlotGivers(path + "." + (child.ID).ToString().Trim(), slotType));
        }

        return returnValue;
    }


    public Dictionary<string, int> findSlotTakers(object path, clsSlotType slotType, bool include = true)
    {
        Dictionary<string, int> returnValue = default(Dictionary<string, int>);

        //recusively builds a dictionary of path>NumSlots(given) for those systems/options taking slots of the specified type (the branch can be determined easiy from the last segment of the path)
        //Include' is used to supress the slot of the system unit itself (so we can easily find options that give slots - otherwsie it suggests we buy another system unit when we're out of slots !

        //TODO - we could build a 3d dictionary on the system branch when it's added to the basket of Path>SlotType>NumberGiven - which would allow faster resolution of slot overflows

        returnValue = new Dictionary<string, int>();
        if (include)
        {
            int slotsTaken = this.slotsGiven(path, slotType);
            if (slotsTaken < 0)
            {
                returnValue.Add(path, slotsTaken);
            }
        }

        //and recurse
        foreach (var child in this.childBranches.Values)
        {
            AppendDic((Dictionary<string, int>)returnValue, child.findSlotTakers(path + "." + (child.ID).ToString().Trim(), slotType));
        }

        return returnValue;
    }

    /// <summary>Builds a dictionary of the path>branch of all the branches under this one which carry the specified product.</summary>
    /// <remarks>call it (for example) on the root branch to find all the locations of a system, or on a system branch to find the location(s) of an option</remarks>
    public Dictionary<string, clsBranch> findProductBranches(object path, clsChannel sellerchannel, clsProduct ProductToFind, bool crossSystems, HashSet<clsBranch> fruitlessGrafts, bool ExitOnFound)
    {
        Dictionary<string, clsBranch> returnValue = default(Dictionary<string, clsBranch>);

        //Note: a HashSet is very like a list - only it is MUCH faster (binary chop vs linear lookup)
        //hashsets only contain unique values !

        //Pmark("findProductBranches")
        //CheckedBranches contains a built list of the branches (we have checked) under which we *know* the product *doesn't* appear (this stops us from recursing grafts and provides a tenfold speed up)

        returnValue = new Dictionary<string, clsBranch>(); //this is the return value of the function
        if (this.PruneInForce(path, sellerchannel) == 0)
        {

            if (this.Product == ProductToFind)
            {
                returnValue.Add(path, this);
            }

            bool keepGoing = true;
            if (!(this.Product == null))
            {
                if (this.Product.isSystem && !crossSystems)
                {
                    keepGoing = false;
                }
            }

            if (keepGoing)
            {
                foreach (var child in this.childBranches.Values)
                {
                    //    If InStr(path$ & ".", "." & Trim(child.ID) & ".") Then Stop 'Circular reference (this branch already appeared on the path) - todo remove for spped
                    if (!fruitlessGrafts.Contains(child)) //did we already (fruitlessly) check this (grafted) branch (it's grafted in many places - but its children (notwithstanding prunes) are always the same)
                    {
                        Dictionary<string, clsBranch> locations = child.findProductBranches(path + "." + (child.ID).ToString().Trim(), sellerchannel, ProductToFind, crossSystems, fruitlessGrafts, ExitOnFound); //recurse
                        if (locations.Count == 0)
                        {
                            if (child.HasGrafts)
                            {
                                fruitlessGrafts.Add(child); //Checked branches contains a list of the branches under which we *know* the product *doesn't* appear
                            }
                        }
                        else
                        {
                            AppendDic((Dictionary<string, clsBranch>)returnValue, locations);
                            if (ExitOnFound)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
        // Pacc("findProductBranches")

        return returnValue;
    }

    public class clsSKUBranchPathIndexEntry
    {
        public string FirstPath;
        public int BranchId;
        public string SKU;
    }

    /// <summary>Builds a dictionary of the path>branch of all the branches under this one which carry the specified product.</summary>
    /// <remarks>call it (for example) on the root branch to find all the locations of a system, or on a system branch to find the location(s) of an option</remarks>
    public void indexProductBranchesByPath(string path, bool crossSystems, ref Dictionary<int, string> lb)
    {
        if (this.ID == 10093)
        {
            return; //Remove this, its to discount the accessories branch..
        }
        if (this.ID == 10443)
        {
            int d = 9;
        }
        if (this.Product != null && lb.ContainsKey(this.ID))
        {
            lb(this.ID) = path;
            return;
        }

        foreach (var c in this.childBranches.Values)
        {
            c.indexProductBranchesByPath(path + "." + this.ID.ToString(), true, lb);
        }

    }

    public dynamic FindBranchByNameBelow(string name, string fioPath, bool appendPath, int stopAtDepth, ref string outPath)
    {
        if (appendPath)
        {
            fioPath = fioPath + "." + System.Convert.ToString(this.ID);
        }
        if (stopAtDepth != 0 && fioPath.Split('.').Length > stopAtDepth)
        {
            return null;
        }
        if (this.Translation != null && this.Translation.text(English) == name)
        {
            outPath = fioPath;
        }
        return this;

        //		foreach (var child in this.childBranches.Values)
        //		{
        //			object p = child.FindBranchByNameBelow(name, fioPath, true, stopAtDepth, outPath);
        //			if (!(p == null))
        //			{
        //				if (outPath != null)
        //				{
        //					outPath = outPath;
        //					}
        //					return p;
        //					}
        //					}
        //					return null;
    }

    //fammajor ..


    public void FindFamilyBranchesBelow(string Path, int stopAtDepth, Dictionary<string, string> dic)
    {

        List<string> retval = new List<string>();

        // If appendPath Then Path = Path & "." & Me.ID
        //  Path = Path & "." & Me.ID
        if (Path.Split('.').Length > stopAtDepth)
        {
            return;
        }
        if (this.Product != null && this.Product.i_Attributes_Code.ContainsKey("FamMajor"))
        {

            //If Me.Product.i_Attributes_Code("FamMajor").Count > 1 Then Stop

            object fn = this.Product.i_Attributes_Code("FamMajor")[0].Translation.text(English);
            if (dic.ContainsKey(fn))
            {
                return;
            }
            dic.Add(fn, Path);
            //If Me.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English).ToLower = famname.ToLower Then
            // retval.Add(Path)
            //End If
        }

        foreach (var child in this.childBranches.Values)
        {
            child.FindFamilyBranchesBelow(Path + "." + child.ID, stopAtDepth, dic);

        }

    }


    public void findChildBySKU(object path, object pathname, ref clsBranch branch, string sku, clsLanguage language)
    {

        //(Slow (sepecially if context is unknown) ..   for a faster version which finds all occurences - see FindSkuPaths (which uses the [SKUIndex], generated by indexSKUs()
        //Recurses down the branches until it encounters a branch with a product with a SKU = sku
        //Adds the branch ID's to the path$ - such that the final result path$ is correct for the branch located

        //    If Not iq.Prunes.ContainsKey(path$) Then 'dont recurse pruned branches
        if (this.Product != null)
        {
            path += "." + (this.ID).ToString().Trim();
            pathname += " / " + this.displayName(language);
            string mysku;

            if (this.Product.SKU != "")
            {
                mysku = System.Convert.ToString(this.Product.SKU);
                if (mysku == sku)
                {
                    branch = this;
                    return;
                }
            }
        }
        foreach (var child in this.childBranches.Values)
        {
            child.findChildBySKU(path, pathname, branch, sku, language);
            if (!(branch == null))
            {
                return; //found somehting - climb back out
            }
        }
        //        End If


    }

    public clsBranch findChildByProductType(string address, clsProductType producttype, clsAccount account, HashSet<string> foci)
    {
        List<string> errormessages = new List<string>();
        if (this.Product != null && !this.Product.SKU.StartsWith("###") && this.Product.ProductType == producttype)
        {
            return this;
        }

        foreach (var child in this.childBranches.Values)
        {
            if (child.ReasonsForHide(account, foci, address, account.SellerChannel.priceConfig, false, errormessages).Count() == 0)
            {
                object res = child.findChildByProductType(address + "." + child.ID, producttype, account, foci);
                if (res != null)
                {
                    return res;
                }
            }
        }
        return null;
    }


    public int FastfindChildBySKU2(string findsku, string[] segs, int j)
    {
        int returnValue = 0;

        //(Slow (sepecially if context is unknown) ..   for a faster version which finds all occurences - see FindSkuPaths (which uses the [SKUIndex], generated by indexSKUs()
        //Recurses down the branches until it encounters a branch with a product with a SKU = sku
        //Adds the branch ID's to the path$ - such that the final result path$ is correct for the branch located

        //  If Me.GraftedOnAt.Count > 0 AndAlso Not Me.GraftedOnAt.Contains(address) Then Return Nothing


        returnValue = 0;

        // If Not iq.Prunes.ContainsKey(address$) Then 'dont recurse pruned branches

        string mysku;

        if (!(this.Product == null)) //Some placeholder branches (families, supply chains etc - have no product.. so we recurse on down)
        {
            if (this.Product.SKU == findsku)
            {

                return j;

            }
            if (this.Product.isOption)
            {
                return returnValue; //we found an option - but it's not 'the one' - exit -so as not to recurse through tape drives etc
            }
        }

        int result = 0;
        if (j > 20)
        {
            Debugger.Break();
        }
        foreach (var child in this.childBranches.Values)
        {
            segs[j] = System.Convert.ToString(child.ID);
            result = System.Convert.ToInt32(child.FastfindChildBySKU2(findsku, segs, j + 1));
            if (result != 0)
            {
                return result;
            }
        }

        return returnValue;
    }


    public clsBranch findChildBySKU2(string address, string findsku, ref object resultpath, bool crossystems = true)
    {
        clsBranch returnValue = default(clsBranch);

        //(Slow (sepecially if context is unknown) ..   for a faster version which finds all occurences - see FindSkuPaths (which uses the [SKUIndex], generated by indexSKUs()
        //Recurses down the branches until it encounters a branch with a product with a SKU = sku
        //Adds the branch ID's to the path$ - such that the final result path$ is correct for the branch located

        //  If Me.GraftedOnAt.Count > 0 AndAlso Not Me.GraftedOnAt.Contains(address) Then Return Nothing


        returnValue = null;

        // If Not iq.Prunes.ContainsKey(address$) Then 'dont recurse pruned branches

        string mysku;

        if (!(this.Product == null)) //Some placeholder branches (families, supply chains etc - have no product.. so we recurse on down)
        {
            if (this.Product.SKU == findsku)
            {
                resultpath = address;
                return this;

            }

            if (this.Product.isSystem && !crossystems)
            {
                return returnValue; //early exit/speedup
            }
        }


        clsBranch result = default(clsBranch);

        foreach (var child in this.childBranches.Values)
        {

            result = child.findChildBySKU2(address + "." + (child.ID).ToString().Trim(), findsku, resultpath);
            if (!(result == null))
            {
                return result;
            }
        }

        return returnValue;
    }

    /// <summary>Returns a list of paths of all the higest level branches that can be pruned whilst the specified those branches to keep</summary>
    /// <param name="Path">The path of the branch you're starting at</param>
    /// <remarks>Efficienlty prunes as 'far back' as possible - maintainting only the </remarks>
    public List<string> SeverePrune(object Path, Dictionary<string, clsBranch> Keep)
    {
        List<string> returnValue = default(List<string>);

        returnValue = new List<string>();

        Dictionary<string, clsBranch> pd = new Dictionary<string, clsBranch>();
        if (this.childBranches.Count > 0)
        {
            pd = this.PathedDescendants(Path); //Get a dictioanry of Path>Branch for ALL by descendants SLOW
        }
        if (pd.Intersect(Keep).Count() == 0) //does is it contain any of the branches we want to keep
        {
            if (this.Product != null)
            {
                if (this.Product.isSystem)
                {
                    Debugger.Break(); //A receta system with no (receta) options
                }
            }
            returnValue.Add(Path); //nope - prune it and stop recursing
        }
        else
        {
            foreach (var child in this.childBranches.Values)
            {
                returnValue.AddRange(child.SeverePrune(Path + "." + (child.ID).ToString().Trim(), Keep)); //Recurse
            }
        }

        return returnValue;
    }


    public Dictionary<string, clsBranch> PathedDescendants(object Path)
    {
        Dictionary<string, clsBranch> returnValue = default(Dictionary<string, clsBranch>);

        returnValue = new Dictionary<string, clsBranch>();
        returnValue.Add(Path, this);

        foreach (var child in this.childBranches.Values)
        {
            if (child.childBranches.Count > 0)
            {
                Dictionary<string, clsBranch> cpd = child.PathedDescendants(Path + "." + (child.ID).ToString().Trim());
                // If cpd Is Nothing Then Stop
                AppendDic((Dictionary<string, clsBranch>)returnValue, cpd);
            }
        }

        return returnValue;
    }

    public List<clsBranch> Descendants()
    {
        List<clsBranch> returnValue = default(List<clsBranch>);

        returnValue = new List<clsBranch>();
        returnValue.Add(this);

        foreach (var branch in childBranches.Values)
        {
            returnValue.AddRange(branch.Descendants);
        }

        return returnValue;
    }


    public clsBranch(int id, clsProduct Product, clsBranch ParentBranch, clsTranslation translation, string picture, clsTranslation collectiveNoun, clsTranslation collectiveNounSingular, clsScreen matrix, int order, bool hidden, bool locked, string rca)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        AllParents = new Dictionary<int, clsBranch>();


        //This particular constructor is most often called when (re)constructing from the database

        //Branches carry a reference to a product - and although each branch exists under (exactly) 1 parent branch
        //there can be many branches referencing the same product (in the 'master list' of products).. each with a different parent
        GraftedOnAt = new List<string>();

        this.ID = id;
        this.Product = Product;
        this.Quantities = new Dictionary<int, clsQuantity>();
        //Me.i_Quantities = Nothing
        this.i_Slots = new Dictionary<string, clsSlot>();
        this.slots = new Dictionary<int, clsSlot>();

        this.Translation = translation;
        this.CollectiveNoun = collectiveNoun;
        this.collectiveNounSingular = collectiveNounSingular;
        this.Picture = picture;
        this.Matrix = matrix; //Which screen to use in the front end Matrix
        this.order = order;
        this.Hidden = hidden;
        this.locked = locked;
        this.rca = rca;

        //i_childbranches = New List(Of clsBranch)
        childBranches = new Dictionary<int, clsBranch>();

        if (!(ParentBranch == null))
        {

            ParentBranch.childBranches.Add(this.ID, this);
            this.Parent = ParentBranch;
            this.AllParents.Add(ParentBranch.ID, ParentBranch);
        }
        else
        {

        }
        if (!iq.Branches.ContainsKey(this.ID))
        {
            iq.Branches.Add(this.ID, this);
        }

        if (Product != null)
        {
            Product.Branches.Add(this);
        }

        if (iq.Branches.Count == 1)
        {
            iq.RootBranch = this; //The first branch we (EVER) add - becomes the root
        }

        oParent = this.Parent;
        this.Prunes = new Dictionary<int, clsPrune>();

    }

    public clsBranch(clsProduct Product, clsBranch ParentBranch, clsTranslation translation, string picture, clsTranslation CollectiveNoun, clsTranslation CollectiveNounsingular, clsScreen matrix, int order, bool hidden, string rca, DataTable writecache, ref int nextID)
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        AllParents = new Dictionary<int, clsBranch>();




        this.Product = Product;
        this.Picture = picture;
        this.CollectiveNoun = CollectiveNoun;
        this.collectiveNounSingular = CollectiveNounsingular;
        //Me.i_Quantities = Nothing
        this.Quantities = new Dictionary<int, clsQuantity>();
        this.slots = new Dictionary<int, clsSlot>();
        this.i_Slots = new Dictionary<string, clsSlot>();

        this.Matrix = matrix;
        this.order = order;
        this.Hidden = hidden;
        this.locked = locked;
        this.rca = rca;

        object sql = null;
        string PBID = "";

        if (ParentBranch == null)
        {
            PBID = "null";
        }
        else
        {
            PBID = (ParentBranch.ID).ToString();
        }

        string prodID = ""; //product id
        if (Product == null)
        {
            prodID = "null";
        }
        else
        {
            prodID = (Product.ID).ToString();

        }

        string matrixID = "";
        if (this.Matrix == null)
        {
            matrixID = "null";
        }
        else
        {
            matrixID = (this.Matrix.ID).ToString();
        }

        this.Translation = translation;

        if (PBID != "null")
        {
            if (int.Parse(PBID) <= 0)
            {
                Debugger.Break();
            }
        }

        if (writecache == null)
        {
            sql = "INSERT INTO BRANCH(fk_Product_ID,FK_branch_id_parent,fk_translation_key,picture,fk_translation_key_collective,fk_translation_key_collectiveSingular,fk_screen_id_matrix,[order],hidden,locked,rca) ";
            sql += "VALUES (" + prodID + ", " + PBID + "," + this.Translation.Key + ",\'" + picture + "\'," + this.CollectiveNoun.Key + "," + this.collectiveNounSingular.Key + "," + matrixID + "," + System.Convert.ToString(order) + "," + (hidden ? "1" : "0").ToString() + "," + (hidden ? "1" : "0").ToString() + "," + da.SqlEncode(this.rca) + ");";

            this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
            if (PBID != "null")
            {
                if (int.Parse(PBID) > this.ID)
                {
                    if (int.Parse(PBID) > nextID)
                    {
                        Debugger.Break();
                    }

                }
            }
        }
        else
        {
            if (nextID == -1)
            {
                Interaction.Beep();
            }

            //  If CInt(PBID) > nextID Then Stop
            if (iq.Branches.ContainsKey(nextID))
            {
                nextID = System.Convert.ToInt32(iq.Branches.ToList.Max(m => m.Value.ID) + 1);
            }
            this.ID = nextID;
            nextID++;


            System.Data.DataRow row = default(System.Data.DataRow);
            row = writecache.NewRow();
            writecache.Rows.Add(row);
            row["ID"] = this.ID; //- we EXPLICITLY set ids on branches

            row["fk_product_id"] = prodID == "null" ? DBNull.Value : prodID;
            row["fk_branch_id_parent"] = PBID == "null" ? DBNull.Value : PBID;

            row["fk_translation_key"] = this.Translation.Key;
            row["picture"] = picture;
            row["fk_translation_key_collective"] = this.CollectiveNoun.Key;
            row["fk_translation_key_collectiveSingular"] = this.collectiveNounSingular.Key;
            row["fk_screen_id_matrix"] = matrixID == "null" ? DBNull.Value : matrixID;
            row["order"] = this.order;
            row["hidden"] = hidden ? 1 : 0;
            row["locked"] = hidden ? 1 : 0;
            row["rca"] = this.rca;
            row["deleted"] = false;
            if (PBID != "null")
            {
                if (int.Parse(PBID) > this.ID)
                {
                    Interaction.Beep(); //my parent is further 'down' the tree than me - which is weird
                }
            }
        }

        if (childBranches == null)
        {
            childBranches = new Dictionary<int, clsBranch>();
        }

        if (!(ParentBranch == null)) //The root node has no parents (so we dont add this child)
        {
            //If ParentBranch.Branches Is Nothing Then ParentBranch.Branches = New List(Of clsBranch)
            ParentBranch.childBranches.Add(this.ID, this);
            this.Parent = ParentBranch;

        }

        if (Product != null)
        {
            Product.Branches.Add(this);
        }
        // If Not iq.Branches.ContainsKey(Me.ID) Then

        iq.Branches.Add(this.ID, this);

        //End If

        if (iq.Branches.Count == 1)
        {
            iq.RootBranch = this; //The first branch we add - becomes the root
        }

        oParent = this.Parent;
        this.Prunes = new Dictionary<int, clsPrune>();
        this.GraftedOnAt = new List<string>();

    }
    public void SetParent(clsBranch ParentBranch)
    {

        if (!(ParentBranch == null))
        {

            ParentBranch.childBranches.Add(this.ID, this);
            this.Parent = ParentBranch;
            this.AllParents.Add(ParentBranch.ID, ParentBranch);
        }
        else
        {

        }
    }

    public void update(ref List<string> errorMessages) //As clsBranch
    {

        //todo - allow reparenting

        object sql = null;
        object pid = null;
        string matrixid = "";
        if (this.Parent == null)
        {
            pid = "null";
        }
        else
        {
            pid = (this.Parent.ID).ToString();
        }
        if (this.Matrix == null)
        {
            matrixid = "null";
        }
        else
        {
            matrixid = (this.Matrix.ID).ToString();
        }

        sql = "UPDATE branch SET fk_branch_id_parent=" + System.Convert.ToString(pid) + ",picture=" + da.SqlEncode(this.Picture) + ",fk_translation_key=" + this.Translation.Key.ToString() + ",fk_screen_id_matrix=" + matrixid + ",[order]=" + System.Convert.ToString(this.order) + ",rca=" + da.SqlEncode(this.rca) + ",FK_PRODUCT_ID=" + (this.Product != null ? this.Product.ID : "null") + ",fk_translation_key_collective=" + this.CollectiveNoun.Key + ",fk_translation_key_collectiveSingular=" + this.collectiveNounSingular.Key + ",deleted=" + System.Convert.ToString(this.deleted ? "1" : "0") + ",hidden=" + System.Convert.ToString(this.Hidden ? "1" : "0") + ",locked=" + System.Convert.ToString(this.deleted ? "1" : "0") + " WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);


        if (!(oParent == null))
        {
            oParent.childBranches.Remove(this.ID);
        }

        if (!(this.Parent == null) && !this.Parent.childBranches.ContainsKey(this.ID))
        {
            this.Parent.childBranches.Add(this.ID, this);
        }

        oParent = this.Parent;

        //  Return Me

    }


    /// <summary>
    /// Performs a hard (and cascading) delete from the branch - including its referencing quantities, slots and quotItems (and any quotes featuring those quoteitems)
    /// </summary>
    /// <param name="errorMessages"></param>
    /// <param name="summary">verbose, indented (tabbed), textual summary of the atomic operation(s) performed</param>
    /// <param name="depth">Pass 0, used to foramt the summary</param>
    /// <remarks></remarks>
    public void HardDelete(List<string> errorMessages, ref string summary, int depth, bool DoIT, Dictionary<string, int> counts)
    {

        int found = 0;
        List<clsBranch> descendants = this.Descendants(); //getSKUdDescendants(True, bi.path, bi, True, 100000, found, errorMessages)

        List<string> j = new List<string>();
        foreach (var b in descendants)
        {
            j.Add(b.ID);
        }

        string bids = string.Join(",", j.ToArray); //Construct a comma seperated list of the descendant branch ID's (regardless of visibility !)

        //If Me.childBranches.Count Then
        //    For Each c In Me.childBranches.Values.ToList
        //        c.HardDelete(errorMessages, summary, depth + 1, DoIT, counts)
        //    Next
        //Else
        //    'ok to delete 'me' now (i have no childbranches)

        object sql = null;

        sql = "FROM quoteItem where fk_branch_id in (" + bids + ") ";
        summary += DeleteOrcount(DoIT, sql, depth, counts, "QuoteItems") + " QuoteItems<br>";

        sql = "FROM quote where id in (select fk_quote_id from quoteitem where fk_branch_id in (" + bids + "))";
        summary += DeleteOrcount(DoIT, sql, depth, counts, "Quotes") + " Quotes<br>";

        sql = "FROM graft where fk_branch_id_source in (" + bids + ")";
        summary += DeleteOrcount(DoIT, sql, depth, counts, "GraftSource") + " Grafts (sources)<br>";

        sql = "FROM graft where fk_branch_id_target in (" + bids + ")";
        summary += DeleteOrcount(DoIT, sql, depth, counts, "GraftTarget") + " Grafts (target)<br>";

        sql = "FROM quantity where fk_branch_id in (" + bids + ")";
        summary += DeleteOrcount(DoIT, sql, depth, counts, "Quantities") + " quantities<br>";

        sql = "FROM slot where fk_branch_id in (" + bids + ")";
        summary += DeleteOrcount(DoIT, sql, depth, counts, "Slots") + " slots<br>";

        sql = "FROM [Branch] WHERE ID in (" + bids + ")";
        // summary$ &= DeleteOrcount(DoIT, sql$, depth, counts, "Branches") & " branches<br>"

        da.DBExecutesql("update branch set deleted=1 where id in (" + bids + ")");


        if (DoIT)
        {
            if (this.Parent != null)
            {
                this.Parent.childBranches.Remove(this.ID);
            }
        }
        // End If

    }

    public dynamic QuoteAllSystemsBelow(UInt64 lid, object Path, clsQuote quote, List<string> errorMessages, List<string> results)
    {


        if (this.Product != null && this.Product.isSystem(Path))
        {

            //add this system to a quote
            List<clsPrice> prices = this.Product.GetPrices(quote.BuyerAccount, quote.BuyerAccount.SellerChannel.IsCloneOf.priceConfig, iq.AllVariants, errorMessages, true);

            if (prices == null)
            {
                results.Add(Product.SKU + " **NO PRICES**");
            }
            else
            {
                foreach (var price in prices) //For this one product there can be more than one price/stock (one per variant!)
                {

                    //supress the test variants for any non admin user
                    if (!(price == null)) //POAs are 'NOthings' in the list of prices
                    {

                        clsQuoteItem qi = quote.setQtyByPath(Path, prices.First.SKUVariant, 1, true, 1, errorMessages);

                        if (qi != null)
                        {
                            qi.fetchPreinstalled(lid, quote.BuyerAccount, errorMessages);

                            if (quote.PassesValidation(lid))
                            {
                                results.Add(Product.SKU + " **PASS**");
                            }
                            else
                            {
                                results.Add(Product.SKU + " **FAIL**");
                            }

                            //remove the system ((ready to quote the next one)
                            quote.SetQtyByItemID(qi.ID, 0, true, 1, errorMessages);
                            //qi.Update()

                        }

                        break; //we quote on the first variant we come accorss
                    }
                }
            }
        }

        foreach (var child in this.childBranches.Values)
        {
            child.QuoteAllSystemsBelow(lid, Path + "." + child.ID, quote, errorMessages, results);
        }


    }

    private string DeleteOrcount(bool doit, object fromsql, int depth, Dictionary<string, int> counts, string entity)
    {

        if (!counts.ContainsKey(entity))
        {
            counts.Add(entity, 0);
        }


        int num = 0;
        if (doit)
        {
            num = System.Convert.ToInt32(da.DBExecutesql("delete " + System.Convert.ToString(fromsql)));
            counts(entity) += num;
            return "Deleted " + num.ToString();
        }
        else
        {

            num = System.Convert.ToInt32(da.DBSelectFirst("select count(*) " + System.Convert.ToString(fromsql)));
            counts(entity) += num;
            return "Would delete - " + System.Convert.ToString(num);
        }

    }


    public void delete(ref List<string> errorMessages)
    {

        //todo - remove all grafts (and prunes) of this branch
        //deleting a branch can orphan products, but that's OK

        object sql = null;

        sql = "delete from graft where fk_branch_id_source=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

        sql = "delete from graft where fk_branch_id_target=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);


        //this is not robust or complete and will not cope with deleteting branches with more than one level of descendants
        sql = "delete from branch where fk_branch_id_parent=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql);

        if (oParent != null)
        {
            oParent.childBranches.Remove(this.ID);
        }

        if (this.Parent != null)
        {
            this.Parent.childBranches.Remove(this.ID);
        }


        sql = "DELETE FROM [Branch] WHERE ID=" + System.Convert.ToString(this.ID);
        da.DBExecutesql(sql, false);


    }


    /// <summary>
    /// Recursivley returns a list of the paths of descendant branches named (with the unique/specific trasnlation) TL
    /// </summary>
    /// <remarks>Note - Walking branches by name is rarely a good idea - It's possible there is more than one tranlation with the same text </remarks>
    public List<string> DescendantsNamed(object path, clsTranslation tl)
    {
        List<string> returnValue = default(List<string>);

        returnValue = new List<string>();
        if (this.Translation == tl)
        {
            returnValue.Add(path);
            return returnValue; //speedup
        }

        foreach (var child in this.childBranches.Values)
        {
            returnValue.AddRange(child.DescendantsNamed(path + "." + child.ID.ToString().Trim, tl));
            if (returnValue.Count > 0)
            {
                return returnValue; //Speedup
            }
        }

        return returnValue;
    }

    ///<summary>Returns the direct children of this branch</summary>
    /// <param name="bi">Includes the SHOWALL paramter - if true, branches that would normally be hidden are also returned (with their HideReasons())</param>
    /// <param name="errormessages"></param>
    /// <remarks> SKUless (category) branches are only returned if the have visible descdants (ones with NO hidereasons)</remarks>
    public Dictionary<clsBranch, clsVisibility> getVisibleChildren(clsBranchInfo bi, List<string> errormessages, ref int skus, ref int cats)
    {
        Dictionary<clsBranch, clsVisibility> returnValue = default(Dictionary<clsBranch, clsVisibility>);

        returnValue = new Dictionary<clsBranch, clsVisibility>();
        HashSet<string> RememberSet = new HashSet<string>();
        //LogMessage("Parent Branch ID : " & Me.ID & "-  Name : " & Me.Translation.text(English))
        //If Me.Translation.text(English) = "HW Support" Then
        //    Dim a = 8
        //End If
        foreach (var child in this.childBranches.Values.ToList().OrderBy(cb => cb.order))
        {
            //If child.Translation.text(English) = "HW Support" Then
            //    Dim a = 8
            //End If
            //LogMessage("Child Branch ID : " & child.ID & "-  Name : " & child.Translation.text(English))
            if (child.Product != null)
            {
                //LogMessage("Child product ID : " & child.Product.ID & "-  Name : " & child.Product.DisplayName(English) & " Type : " & child.Product.ProductType.Translation.text(English))
            }
            // Debug.WriteLine(child.Translation.text(English) & " : " & child.ID)
            object cpath = bi.path + "." + child.ID;
            if (bi.showAll == true || (child.PruneInForce(bi.path + "." + child.ID, bi.buyerAccount.SellerChannel.IsCloneOf) == 0))
            {
                if (child.HasSKU)
                {

                    List<string> hrs = child.ReasonsForHide(bi.buyerAccount, bi.foci, cpath, bi.buyerAccount.SellerChannel.priceConfig, false, errormessages);
                    if ((hrs.Count) == 0 || (bi.showAll == true))
                    {
                        skus++;
                        returnValue.Add(child, new clsVisibility(child, cpath, hrs));
                    }
                }
                else
                {
                    //Always include the upsell opportunities branch (even thouh it has no children!)
                    if (child.rca == "U" && !bi.showAll)
                    {
                        returnValue.Add(child, new clsVisibility(child, cpath, new List<string>()));
                    }

                    //determine the visibility of the SKUless (category) branches (Based on whether they have a visible descendant)
                    Dictionary<clsBranch, clsVisibility> vCats = default(Dictionary<clsBranch, clsVisibility>);
                    vCats = child.getSKUdDescendants(true, cpath, bi, false, 1, 0, errormessages);
                    // If child.rca = "U" Then getVisibleChildren.Add(child, New clsVisibility(child, cpath, New List(Of String)))
                    if ((from j in vCats.Values where j.hideReasonList.Count == 0 select j).Any || bi.showAll)
                    {
                        //   For Each j In (From v In vCats.Values Order By v.branch.order)  'Order category branches by their bracnch.order
                        //If j.hideReasonList.Count = 0 Or bi.showAll Then
                        cats++;
                        if (RememberSet.Add(child.Translation.text(English)))
                        {
                            returnValue.Add(child, new clsVisibility(child, cpath, new List<string>()));
                        }
                        //end if
                        //   Next
                    }
                }
            }
        }



        return returnValue;
    }

    public void BranchFirstPaths(Dictionary<clsBranch, string> Locations, object path, List<clsBranch> forBranches)
    {

        //builds a dictionary of clsBraanch>Paths at which it first occurs under this branch

        if (forBranches.Contains(this))
        {
            Locations.Add(this, path);
            return; //Assume a branch won't appear again under itself !
        }

        if (this.childBranches != null)
        {
            foreach (var child in this.childBranches.Values)
            {
                child.BranchFirstPaths(Locations, path + "." + child.ID, forBranches);
            }
        }

    }

    public void SkuPaths(Dictionary<string, List<string>> Dic, object path, bool CrossSKUs)
    {

        //builds a dictionary of SKU>List of Paths at which it occurs under this branch (which happens a lot for things like all the occurances of an option in a family)

        if (this.HasSKU())
        {
            if (!Dic.ContainsKey(this.Product.SKU))
            {
                Dic.Add(this.Product.SKU, new List<string>());
            }
            Dic(this.Product.SKU).Add(path);

            if (!CrossSKUs)
            {
                return; //stop recursing if deep is false and we have just found a SKUd part (siblings will still be processed)
            }
        }

        string cpath = "";
        if (this.childBranches != null)
        {
            foreach (var child in this.childBranches.Values)
            {

                cpath = path + "." + (child.ID).ToString().Trim();
                child.SkuPaths(Dic, cpath, CrossSKUs);

            }
        }

    }

    public Dictionary<clsBranch, clsVisibility> getSKUdDescendants(bool includeself, object path, clsBranchInfo bi, bool goDeep, int maxSKUs, ref int skusFound, ref List<string> errorMessages)
    {
        Dictionary<clsBranch, clsVisibility> returnValue = default(Dictionary<clsBranch, clsVisibility>);

        returnValue = new Dictionary<clsBranch, clsVisibility>();
        // Debug.WriteLine(Me.Translation.text(English).ToUpper())
        //If Me.Translation.text(English).ToUpper() = "TOP RECOMMENDED" Then
        //    Dim a = 9
        //End If

        if (bi.buyerAccount == null || bi.showAll || this.PruneInForce(path, bi.buyerAccount.SellerChannel) == 0) //Prunes are not host specific (although that will almost certainly become a requirement)
        {
            if (includeself)
            {
                // NOTE this WAS BI .path which doesn't track the recursion properly and so didnt work !

                if (this.HasSKU())
                {

                    //IMPORTANT We need to make this call to get the hidereason populated - Generally it will be fast for hidden products anyway
                    //branches with a hidereason are not displayed in 'normal' operation

                    List<string> hrs = default(List<string>);

                    if (bi.buyerAccount == null) //Descendants can be called with no buyeraccount (if showall is true)
                    {
                        hrs = new List<string>();
                    }
                    else
                    {
                        int pc = System.Convert.ToInt32(bi.buyerAccount.SellerChannel.priceConfig);
                        hrs = this.ReasonsForHide(bi.buyerAccount, bi.foci, path, pc, false, ref errorMessages); //Calls GetPrices

                    }

                    if (this.PruneInForce(path, bi.buyerAccount.SellerChannel) != 0)
                    {
                        hrs.Add("Branch is PRUNED at this location ");
                        // If Not bi.showAll Then Exit Function
                    }
                    if (this.SKU().StartsWith("###"))
                    {
                        hrs.Add("Fake/Soldered Part");
                    }

                    if ((hrs.Count == 0) || (bi.showAll == true))
                    {
                        skusFound++;
                        returnValue.Add(this, new clsVisibility(this, path, hrs)); //buyeraccount, focus, Path$)
                    }

                    if (!goDeep)
                    {
                        return returnValue;
                    }
                    if (skusFound > maxSKUs)
                    {
                        return returnValue;
                    }
                }
            }
            string cpath = "";
            foreach (var child in this.childBranches.Values)
            {
                cpath = path + "." + (child.ID).ToString().Trim();
                AppendDic(returnValue, child.getSKUdDescendants(true, cpath, bi, goDeep, maxSKUs, skusFound, errorMessages));
                if (skusFound >= maxSKUs)
                {
                    return returnValue;
                }
            }
        }

        return returnValue;
    }

    public void getSKUdParents(object path, clsBranchInfo bi, ref List<string> errorMessages, List<string> rs)
    {

        string[] pathArray = Strings.Split(System.Convert.ToString(path), ".");
        string brID = "";
        List<string> hrs = default(List<string>);
        foreach (string tempLoopVar_brID in pathArray)
        {
            brID = tempLoopVar_brID;
            if (Information.IsNumeric(brID))
            {
                clsBranch currentBranch = iq.Branches(int.Parse(brID));
                if (currentBranch.HasSKU())
                {
                    int pc = System.Convert.ToInt32(bi.buyerAccount.SellerChannel.priceConfig);
                    hrs = currentBranch.ReasonsForHide(bi.buyerAccount, bi.foci, path, pc, false, ref errorMessages);
                    if (hrs.Count > 0)
                    {
                        foreach (var hr in hrs)
                        {
                            rs.Add(hr);
                        }
                    }
                }
            }
        }
    }
    public List<clsBranch> CheckBundles(clsAccount buyeraccount, HashSet<string> foci, string path, bool Showall, short priceconfig, ref List<string> Errormessages)
    {
        List<clsBranch> returnValue = default(List<clsBranch>);

        returnValue = null;

        Pmark("CheckBundles");

        List<clsBranch> retval = new List<clsBranch>();
        bool reachedSystem = false;

        try
        {
            //bundles only exist on systems
            if (!(this.Product == null))
            {
                if (this.Product.isSystem)
                {
                    reachedSystem = true;
                    if (!(this.Product.Bundles == null))
                    {
                        if ((this.ReasonsForHide(buyeraccount, foci, path, priceconfig | 1, true, ref Errormessages).Count() == 0) || Showall)
                        {
                            foreach (var bundle in this.Product.Bundles.Values)
                            {
                                if (buyeraccount.SellerChannel.Region.Encompasses(bundle.Region))
                                {
                                    //does this system have any current bundles for this (sellers) region
                                    if (DateTime.Now > bundle.validFrom && DateTime.Now < bundle.validTo) //getBundle("", 0, Now, region) IsNot Nothing Then
                                    {
                                        retval.Add(this);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //and recurse.. for each child

            if (!reachedSystem) //we don't recurse beyond systems  (presently - for speed - we may need to in future (for sub-system) - and will probably need a table of BundleBranches for speed at that point)
            {
                List<clsBranch> descendants = default(List<clsBranch>);
                foreach (var b in this.childBranches.Values)
                {

                    descendants = b.CheckBundles(buyeraccount, foci, path + "." + (this.ID).ToString().Trim(), Showall, priceconfig, Errormessages);
                    if (descendants.Count > 0)
                    {
                        retval.AddRange(descendants);
                        retval.Add(this); //I have descendants who have bundles , so add me too.. (to show where those descendants sit... so the root branch, servers branch, family branch, top value branch etc get their bundle circles)
                    }
                }
            }

            return retval;

        }
        catch
        {

        }
        finally
        {
            Pacc("CheckBundles");
        }

        return returnValue;
    }

    public List<clsBranch> checkAvalanche(clsProduct system, clsAccount buyeraccount, HashSet<string> foci, string path, bool showAll, int priceconfig, ref List<string> errorMessages)
    {

        // Pmark("CheckAvalanche")

        //called once at startup on the root branch
        //recurses the full tree, creating a list of which branches feature avalanche offers for the specified buyer account
        //this is necessary as different products are visible (see branch.productvisible) to different buyers (by virtue of having a price or not, regional restrictions etc.)
        //and recursing branches in realtime to look for

        List<clsBranch> retval = new List<clsBranch>();
        string prodRef = "";

        //Try
        if (!(this.Product == null))
        {
            if (this.Product.isSystem)
            {
                List<string> hr = this.ReasonsForHide(buyeraccount, foci, path, priceconfig | 1, true, ref errorMessages);
                if ((hr.Count == 0) || showAll)
                {
                    system = this.Product;

                    //if this system has no avalancheOPGs we don't need to recurse into its options - WILL NEED TO BE REMOVED FOR SYSTEMS WITHIN SYSTEMS
                    if (this.Product.AvalancheOPGs.Count == 0)
                    {
                        return retval;
                    }
                    return default(List<clsBranch>);

                    //								foreach (var av in this.Product.AvalancheOPGs.Values)
                    //								{
                    //does this system have any current avalanche options for this (sellers) region
                    //									if (av.getAvalancheOptions("", 0, DateTime.Now, buyeraccount.SellerChannel.Region).Count() > 0)
                    //									{
                    //iq.avalancheBranches(buyeraccount.BuyerChannel).Add(Me)
                    //										retval.Add(this);
                    //										break;
                    //										}
                    //										}
                }
                else
                {
                    //the system is not visible.. stop recursing
                    return retval;
                }
            }
            else
            {
                //you're an option (or a placeholder product) - do you have a refcode, and is it valid for one of your systems' avalanches
                if ((this.ReasonsForHide(buyeraccount, foci, path, System.Convert.ToInt32(buyeraccount.SellerChannel.priceConfig), true, ref errorMessages).Count() == 0) || showAll)
                {
                    if (this.Product.i_Attributes_Code.ContainsKey("ProdRef"))
                    {
                        prodRef = System.Convert.ToString(this.Product.i_Attributes_Code("ProdRef")[0].Translation.text(English));
                        if (system != null)
                        {
                            foreach (var av in system.AvalancheOPGs.Values)
                            {
                                if (av.getAvalancheOptions(prodRef, 0, DateTime.Now, null).Count() > 0)
                                {
                                    //iq.avalancheBranches(buyeraccount.BuyerChannel).Add(Me)
                                    retval.Add(this);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        //and recurse.. for each child
        List<clsBranch> descendants = default(List<clsBranch>);
        foreach (var b in this.childBranches.Values)
        {
            if (b.PruneInForce(path + "." + b.ID.ToString().Trim, buyeraccount.SellerChannel) == 0) //respect the Prunes man !
            {
                descendants = b.checkAvalanche(system, buyeraccount, foci, path + "." + (b.ID).ToString().Trim(), showAll, priceconfig, errorMessages);
                if (descendants.Count > 0)
                {
                    retval.AddRange(descendants);
                    retval.Add(this); //I have descendants who have avalanche offers, so add me too.. (to show where those descendants sit... so the root branch, servers branch, family branch, top value branch etc get their avalanche stars
                }
            }
            else
            {
                //    Beep()
            }
        }

        //Catch ex As Exception

        //Finally
        //    Pacc("CheckAvalanche")

        //End Try

        return retval;

    }

    public void checkFlex(clsAccount buyeraccount, HashSet<string> foci, string path, short PriceConfig, List<clsBranch> branches, ref List<string> errormessages)
    {

        //called once at startup on the root branch
        //recurses the full tree, creating a list of which branches feature (descendant) offers for the specified buyer account
        //this is necessary as different products are visible to different buyers (by virtue of having a price or not, regional restrictions etc.)
        // Dim retval As List(Of clsBranch) = New List(Of clsBranch)

        //Try
        if (!buyeraccount.BuyerChannel.SchemeEnabled("F"))
        {
            return; //Disable this if the user cannot see flex...
        }

        //NB priceconfig at this point has had is's 8 bit ANDEd out - so we will NOT check the webservice (becuase we'd be making a call for far too many products)
        if (this.Product != null)
        {

            List<string> rh = this.ReasonsForHide(buyeraccount, foci, path, PriceConfig, true, ref errormessages);
            if (rh.Count == 0)
            {

                //If isPrunedAt(path$) And Not showAll Then Return retval 'PRUNES ARE NOT HANDLED HERE (there are too many paths)


                //IMPORTANT
                if (Product.isSystem && Product.OPGflexLines.Count == 0)
                {
                    return; //Return retval
                }

                bool oneRegionHasFlex = false;
                foreach (var FlexLine in this.Product.OPGflexLines.Values) //Flexlines contain a rebate on a product, valid between certain dates, under a certain OPG
                {

                    if (FlexLine.FlexOPG.AppliesToRegion(buyeraccount.BuyerChannel.Region))
                    {
                        if (FlexLine.isCurrent && FlexLine.FlexOPG.isCurrent)
                        {
                            foreach (var b in path.Split('.'))
                            {
                                if (b != "tree")
                                {
                                    if (!branches.Contains(iq.Branches(System.Convert.ToInt32(b))))
                                    {
                                        branches.Add(iq.Branches(System.Convert.ToInt32(b)));
                                    }
                                }
                            }
                            // retval.Add(Me) 'add me (to the list of branches which have flex on them)
                            //Exit For ' was exit for
                            //        Return retval
                            oneRegionHasFlex = true;
                        }

                    }
                    else
                    {

                        //STOP recursing if you hit a system then does not qualify regionally !
                        //otherwise we recurse to options that migh be part of a DIFFERENT flex
                    }
                }
                if (Product.isSystem && !oneRegionHasFlex)
                {
                    return;
                }
            }
            else
            {
                //the product is not visible.. (so neither are any of its descendants !) - stop recursing
                //Return retval
                return;
            }

        }

        //and recurse.. for each child
        //Dim descendants As List(Of clsBranch)
        foreach (var b in this.childBranches.Values)
        {

            if (b.PruneInForce(path + "." + (b.ID).ToString().Trim(), buyeraccount.SellerChannel) == 0) //respect the Prunes man !
            {
                b.checkFlex(buyeraccount, foci, path + "." + b.ID.ToString().Trim, PriceConfig, branches, errormessages);

                // For Each d In descendants
                // If Not retval.Contains(d) Then retval.Add(d)
                // Next
            }
        }

        //Return retval

    }

    public bool HasMajorSlot(string majorCode)
    {
        bool returnValue = false;

        returnValue = false;

        foreach (var slot in this.slots.Values)
        {
            if (!slot.deleted)
            {
                if (Strings.LCase(System.Convert.ToString(slot.Type.MajorCode)) == majorCode.ToLower())
                {
                    return true;
                }
            }
        }

        return returnValue;
    }


    public bool hasSlot(clsSlotType SlotType)
    {
        bool returnValue = false;


        returnValue = false;
        foreach (var slot in this.slots.Values)
        {
            if (slot.Type == SlotType && !slot.deleted)
            {
                return true;
            }
        }

        return returnValue;
    }

    public void MajorSlots(ref Dictionary<string, int> dic)
    {

        //Returns the number of (gives) slots of each of the major types - recursing branches - but not crossing systems

        foreach (var t in this.slots.Values)
        {
            if (!t.deleted)
            {
                if (t.numSlots > 0)
                {
                    //If t.Type.MajorCode = "CPU" Then Stop
                    if (!dic.ContainsKey(t.Type.MajorCode))
                    {
                        dic.Add(t.Type.MajorCode, 0);
                    }
                    dic(t.Type.MajorCode) += t.numSlots;
                }
            }
        }

        bool isSystem = false;
        foreach (var c in this.childBranches.Values)
        {

            if (c.Product != null)
            {
                isSystem = System.Convert.ToBoolean(c.Product.isSystem);
            }

            if (!isSystem)
            {
                c.MajorSlots(dic); //recurse (if its not a system)
            }
        }

    }

    private PlaceHolder BranchUI(clsBranchInfo bi, int numSKUs, int numCats, List<string> hideReasons, ref List<string> errorMessages, UInt64 lid)
    {
        PlaceHolder returnValue = default(PlaceHolder);

        //Called for branches being rendered as a BRANCH (bt.branch) .. ie. not a square, matrix row, etc
        //DO NOT CONFUSE WITH  clsBranch.UI()

        returnValue = new PlaceHolder();
        bool imageAdded = false;
        if (iq.sesh(bi.lid, "Paradigm") == enumParadigm.configuringSystem)
        {
            if (this.Picture != "")
            {
                WebControls.Image img = new WebControls.Image();
                img.ImageUrl = "http://www.channelcentral.net" + this.Picture;
                img.CssClass = "prodPhoto";
                returnValue.Controls.Add(img);
                imageAdded = true;

            }
        }

        bool showcounts = true;
        if (numSKUs < 0)
        {
            showcounts = false;
        }

        returnValue.Controls.Add(this.Title(bi, showcounts, false, false, numSKUs, numCats, hideReasons, errorMessages));
        returnValue.Controls.Add(this.PromoIndicators(bi, ref errorMessages));


        if (!(this.Product == null))
        {

        }

        if (this.HasSKU())
        {
            returnValue.Controls.Add(this.BuyUI(bi.buyerAccount, bi.path, lid));
        }


        Literal lit = new Literal();
        if (this.Product != null)
        {
            //Show the HighPerformace and energy star attributes if present
            List<clsProductAttribute> perf = null;

            if (Product.i_Attributes_Code.TryGetValue("Perf", perf))
            {
                lit = new Literal();
                lit.Text = "<div class=\'highPerf\'><img class=\'highPerformance\' src=\'" + imagebase + "/images/icons/icon_iq2_highp.png\' title=\'" + perf[0].Attribute.Translation.text(bi.buyerAccount.Language) + "\'/></div>";
                returnValue.Controls.Add(lit);
            }

            List<clsProductAttribute> eStar = null;
            if (Product.i_Attributes_Code.TryGetValue("eStar", eStar))
            {
                lit = new Literal();
                //http://iquote2.channelcentral.net/sandbox/daisyimages//images/logo/logo_energystar.jpg'
                lit.Text = "<div class=\'eStar\'><img class = \'energyStar\' src=\'" + imagebase + "/images/logo/logo_energystar.jpg\' title=\'" + eStar[0].Attribute.Translation.text(bi.buyerAccount.Language) + "\'/></div>";

                returnValue.Controls.Add(lit);
            }

            List<clsProductAttribute> sc = null;
            if (Product.i_Attributes_Code.TryGetValue("SC", sc))
            {
                lit = new Literal();
                string sct = Strings.Replace(System.Convert.ToString(sc[0].Translation.text(English)), " ", "_");
                lit.Text = "<div class=\'supC\'><img class = \'supplyChain\' src=\'" + imagebase + "images/logo/logo_" + sct + ".png\' title=\'" + sct.Replace("_", " ") + "\'/></div>";

                returnValue.Controls.Add(lit);
            }

        }


        //BranchUI.Controls.Add(NewLit("<div>&nbsp;</div>"))
        if (this.Picture != "" && !bi.treeMode && imageAdded == false)
        {

            //supress the picture if the same one exists 'above'
            //Supress the photo if it appears on the path - but always show the photo on 'configuringsystem' mode
            if ((!PictureOnPath(oneAbove(bi.path), this.Picture)) && bi.Paradigm == enumParadigm.configuringSystem)
            {
                Panel picpanel = new Panel();
                picpanel.CssClass = "photoDiv";
                returnValue.Controls.Add(picpanel);

                WebControls.Image img = new WebControls.Image();
                img.ImageUrl = imagebase + this.Picture;
                img.CssClass = "prodPhoto";
                //BranchUI.Controls.Add(img)
                picpanel.Controls.Add(img);
            }
        }

        if (bi.treeMode == true)
        {

            Panel ipanel = new Panel();
            returnValue.Controls.Add(ipanel);
            ipanel.CssClass = "ib";
            Panel toolsPanel = ExpandablePanel(ipanel, "Tools", "adm", bi);

            if (toolsPanel != null)
            {
                toolsPanel.Controls.Add(adminControls(bi, System.Convert.ToString(bi.path), bi.path));
            }
            toolsPanel.CssClass += " ib";
            //one shot messages (not robust - extend/re-use with caution - if somebody else happens to render the same branch before you - they will get your message (and you won't))
            if (!string.IsNullOrEmpty(this.message))
            {
                returnValue.Controls.Add(NewLit(this.message));
                this.message = "";
            }

        }


        //If Me.Product IsNot Nothing Then
        //    Dim lblID As New Label
        //    lblID.Text = "ID:" & Me.Product.ID
        //    BranchUI.Controls.Add(lblID)
        //End If
        //BranchUI.Controls.Add(SellerSkus(Bi))

        //listprice
        //If Me.HasSKU Then
        //    Dim pr As clsPrice = Me.Product.ListPrice(Bi.BuyerAccount)
        //    If pr IsNot Nothing Then
        //        Dim lp As Label = pr.Price.DisplayPrice(Bi.BuyerAccount, errorMessages)
        //        BranchUI.Controls.Add(lp)
        //    End If
        //End If


        //Subtitle and description are mutually exclusive at the moment


        if (Product != null)
        {
            if (Product.i_Attributes_Code.ContainsKey("subTitle")) //The subtitle is shown on closed branches - open branches also show the proddesc (see ProductInfo)
            {
                //lit = New Literal
                //lit.Text = "<div class='ProdSubTitle'>" & Product.i_Attributes_Code("subTitle")(0).Translation.text(Bi.buyerAccount.Language) & "</div>"
                //BranchUI.Controls.Add(lit)
            }
            else if (Product.i_Attributes_Code.ContainsKey("desc"))
            {
                lit = new Literal();
                lit.Text = "<span class=\'prodDesc\'>" + Product.i_Attributes_Code("desc")[0].Translation.text(bi.buyerAccount.Language) + "</span>";
                returnValue.Controls.Add(lit);
            }

            if (this.PruneInForce(bi.path, bi.buyerAccount.SellerChannel))
            {
                //Beep()
                returnValue.Controls.Add(ErrorDymo("Branch is PRUNED here"));
            }


            if (hideReasons.Count > 0)
            {
                //OK this is it
                //tl.ToolTip = Join(hideReasons.ToArray, ",")
                Panel hrs = new Panel();
                hrs.CssClass = "hidereasons";
                returnValue.Controls.Add(hrs);
                foreach (var reason in hideReasons)
                {
                    if (reason.ToLower.Contains("pruned"))
                    {
                        //     Beep()
                    }
                    hrs.Controls.Add(ErrorDymo(reason));
                }
            }

        }

        //'iterate the slots - output the HDD bay type and count
        //For Each b In Me.childBranches  'the slots are in the chassis branch!
        //    For Each s In Me.slots.Values
        //        If s.path = Bi.path Or s.path = "" Then  'slots *may* have paths  .. for example, some card might give 4 slots on one machine, but only two in another - due to some physical or electrical constraint - and pathed version takes precedence
        //            If s.Type.MajorCode = "HDD" Then

        //                Dim DriveType As Panel = New Panel
        //                DriveType.CssClass = "DriveType"
        //                DriveType.Controls.Add(iq.EnglishIndex("Hard Drives:").HTML(Bi.AgentAccount.Language))
        //                DriveType.Controls.Add(s.Type.Translation.HTML(Bi.AgentAccount.Language))  ' Eg  "Hot Plug 2.5inch smart Carrier"
        //                BranchUI.Controls.Add(DriveType)

        //                Dim Bays As Panel = New Panel
        //                Bays.CssClass = "Bays"
        //                lit = New Literal : lit.Text = "Drive bays:" 'fixed' in a hurry for vegas - needs trasnaltiosn
        //                Bays.Controls.Add(lit) 'iq.EnglishIndex("Drive Bays:").HTML(Bi.AgentAccount.Language))
        //                lit = New Literal
        //                lit.Text = s.numSlots.ToString
        //                Bays.Controls.Add(lit)  ' Number of slots (bays) - eg 8
        //                BranchUI.Controls.Add(Bays)

        //            End If
        //        End If
        //    Next
        //Next

        //'Conditions for showing quickfilter button need work
        //If numSKUs > 4 Then
        //If Split(Bi.path, ".").Count > 3 Then
        //End If


        return returnValue;
    }

    private PlaceHolder SellerSkus(clsBranchInfo bi)
    {
        PlaceHolder returnValue = default(PlaceHolder);

        returnValue = new PlaceHolder();

        Literal lit = default(Literal);
        lit = new Literal();
        if (this.HasSKU())
        {
            if (this.Product.i_Variants != null)
            {
                lit.Text = "(" + bi.buyerAccount.SellerChannel.DisplayName(English) + "SKUs:";
                if (this.Product.i_Variants.ContainsKey(bi.buyerAccount.SellerChannel))
                {
                    foreach (var v in this.Product.i_Variants(bi.buyerAccount.SellerChannel))
                    {
                        lit.Text += "SKU:" + v.DistiSku;
                    }
                    returnValue.Controls.Add(lit);
                }
            }
        }

        return returnValue;
    }

    /// <summary>
    /// Typically display a title and some Expand Buttons -
    /// It may be a branch, square, tab, or matrix row (or soemething else as we add new ways to render branches)
    /// </summary>
    /// <remarks>The BranchHeader is the bit you see when the branch is closed (you typically still see it when its open)</remarks>
    public Panel BranchHeader(clsBranchInfo bi, ref clsBranchState bs, clsBranchState pbs, List<string> hideReasons, int skus, int cats, ref List<string> errorMessages, UInt64 lid)
    {
        Panel returnValue = default(Panel);

        returnValue = new Panel();
        returnValue.ID = bi.path;

        //BranchHeader.Controls.Add(NewLit("<p>DIV:" & bi.path & "</p>"))

        //sd is the SKUd descendant(s).. all these branches will have a product
        //dd is the direct descendant(s) .. may contain categories (product-less branches)

        if (bi.treeMode)
        {
            pbs.rca = enumBt.Branch;
            int PruneID = System.Convert.ToInt32(bi.branch.PruneInForce(bi.path, bi.buyerAccount.SellerChannel));
            if (PruneID != 0)
            {
                //BranchHeader.Controls.Add(NewLit("PRUNED"))
                returnValue.Controls.Add(FunctionButton(bi.path, PruneID, "unprune", "unprune", "reinstates/unprunes this branch"));
            }
        }


        if (pbs.rca != enumBt.Hidden && pbs.rca != enumBt.OpenSquare && AccountHasRight(lid, "EDITTREE") && bi.treeMode == true)
        {
            //Dim BtnEdit As New HyperLink
            //BtnEdit.Text = Xlt("Edit", bi.buyerAccount.Language)
            //BtnEdit.ToolTip = Xlt("Edit - Edits this branch (and the product, slots, quantities etc. attached to it) " & bi.path, bi.buyerAccount.Language)
            //BtnEdit.ImageUrl = "/images/navigation/pencil.png"
            //Dim url$
            //url$ = "edit.aspx?cmd=expand&path=Branches(" & Me.ID & ")&TreePath=" & bi.path$ & "&lid=" & bi.lid

            //BtnEdit.NavigateUrl = url$
            //BtnEdit.Attributes("target") = "_blank"
            //BranchHeader.Controls.Add(BtnEdit)
        }

        //   If bi.treeMode = True Then
        // BranchHeader.Controls.Add(adminControls(bi, bi.path, bi.path$))
        // End If

        //bi.branchState.renderAs     - How a branch is rendered is determined by how a it's parent renders its children

        //Case bt.hidden
        //don't need to render *anything*
        if (pbs.rca == enumBt.OpenSquare)
        {

            //an open square renders nothing ... except its children

            if (bs != null && !(pbs.rca == enumBt.OpenSquare)) //only (hidden) openSquares that are themselves open render their children
            {
                returnValue.CssClass += " branch";
                returnValue.Controls.Add(this.BranchUI(bi, skus, cats, hideReasons, ref errorMessages, lid));
            }
        }
        else if ((pbs.rca == enumBt.Branch) || (pbs.rca == enumBt.OpenBranch))
        {

            if (pbs.rca == enumBt.OpenBranch) //AutoOpen the branch
            {
                if (bs != null) //AM I OPEN ?  otherwise you cant (ever) close auto opening branches
                {
                    enumBt bt = (enumBt)(BTchar.IndexOf(bi.branch.rca.First)); //rember an OpenBranch doesnt neccessarily render its children as branches
                    bs = new clsBranchState(bi.lid, bi.path, bt, bt == enumBt.gridrow, 0, 100);
                }
            }

            // If bi.Paradigm <> enumParadigm.configuringSystem Then

            if ((this.Product == null || (this.Product != null && (!this.Product.isSystem(bi.path) || bi.Paradigm == enumParadigm.AddingSystem))) || AccountHasRight(lid, "DIAGVIEW"))
            {
                returnValue.Controls.Add(this.ExpandCollapseButton(pbs, bi, bs, errorMessages));
            }

            returnValue.CssClass += " branch";
            // a branch renders pratically the same whether it is open or closed - the only singinifcant difference is that an open branch renders its children
            returnValue.Controls.Add(this.BranchUI(bi, skus, cats, hideReasons, ref errorMessages, lid));

        }
        else if ((pbs.rca == enumBt.Square) || (pbs.rca == enumBt.DetailSquare))
        {

            if (bs != null)
            {
                //this is an open sqaure
                //we render nothing (but the children)
            }
            else
            {
                //The header IS the square
                returnValue.CssClass += " square dropShadow ib";
                //BranchHeader.Attributes("onclick") = ButtonScript(bi.path, "open", "bcHolder")
                returnValue.Attributes("onclick") = ButtonScript("cmd=openSquare&path=" + bi.path + "&into=tree");
                bool IsMin = false;
                returnValue.Controls.Add(this.SquareUI(bi, ref hideReasons, skus, cats, lid, null, ref IsMin));

                foreach (var h in hideReasons)
                {
                    returnValue.Controls.Add(ErrorDymo(h));
                }
                if (IsMin)
                {
                    returnValue.CssClass = " squareHidden dropShadow ib";
                }
                //               BranchHeader.ToolTip = BranchHeader.Attributes("onclick")
            }
        }
        else if (((pbs.rca == enumBt.Tab) || (pbs.rca == enumBt.hYperlink)) || (pbs.rca == enumBt.bighyperlinK))
        {

            //note this is the (empty) panel which will hold the tabstrip
            //see also renderTabHeads()I
            //every tab has a panel (the branhcheader) - but only the open one gets any content
            //          BranchHeader.CssClass &= " isTabPanel" 'just for identification in the page source its not a used style
            //   If bi.branchState.state = oc.open Then 'only the open tab (body) displays anything
            //                BranchHeader.CssClass &= " tabBody dropShadow"
            // End If
        } //Remember the gridrow IS the branchHeader.. (it can be opened !)
        else if (pbs.rca == enumBt.gridrow)
        {
            if (bs != null && bs.rca == enumBt.gridrow)
            {
                returnValue.CssClass += " highlighted";
            }
            returnValue.CssClass += " isMatrixRow hideOverflow"; //just for identification in the page source its not a used style

            if (System.Convert.ToBoolean(bi.rownum & 1))
            {
                returnValue.CssClass += " matrixRowOdd";
            }
            else
            {
                returnValue.CssClass += " matrixRowEven"; //alternating stripes
            }

            Panel panel = new Panel();
            panel.CssClass = "matrixRowColumns";
            foreach (var b in returnValue.Controls)
            {
                panel.Controls.Add(b);
                returnValue.Controls.Remove(b);
            }

            if (this.HasSKU())
            {
                if (!this.isOptionOrOptionHolder(System.Convert.ToString(bi.path)) || AccountHasRight(lid, "DIAGVIEW"))
                {
                    //Bodge to hide the expand description in "simple" option mode, request from Greg via DM/JN
                    panel.Controls.Add(this.ExpandCollapseButton(pbs, bi, bs, errorMessages));
                }

                //panel.Controls.Add(Me.PromoIndicators(bi, errorMessages)) 'new
                if (bi.EffectiveHeader == null && !pbs.United)
                {
                    //Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(bi.lid, "matrixHeaders"), Dictionary(Of String, clsScreenHeader))
                    //    If (matrixHeaders.ContainsKey(bi.path)) Then
                    //        bi.matrixHeader = matrixHeaders(bi.path)
                    //    Else

                    Dictionary<clsBranch, clsVisibility> descendants = bi.visibleChildren(errorMessages, true, 0, 0, false, false);
                    bi.CreateMatrixHeader(descendants);
                }
                //    'bi.EffectiveHeader = bi.matrixHeader 'new - ML Removed
                //End If


                foreach (var hr in hideReasons)
                {
                    Literal lt = new Literal();
                    lt.Text = "<span style=\'color:white;background-color:red;\'>" + hr + "</span>";
                    panel.Controls.Add(lt);
                }

                if (pbs.United && (bs == null || pbs.rca == enumBt.gridrow))
                {
                    //Must be a header for the rendering grid, find it
                    System.Object screenHeaders = (Dictionary<string, clsScreenHeader>)(iq.sesh(lid, "screenHeaders"));
                    if (bi.rootPath == null)
                    {
                        //Somethings wrong, we have no rootpath, maybe rendering directly to a div rather than the whole tree, lets find a matrix above this to render as there must be one
                        bi.rootPath = matrixHeaderAbove(lid, bi.path, errorMessages).Path;
                    }
                    if (screenHeaders.ContainsKey(bi.rootPath))
                    {
                        panel.Controls.Add(screenHeaders[bi.rootPath].screen.MatrixRow(this, bi, errorMessages, true));
                    }
                    else
                    {
                        panel.Controls.Add(bi.EffectiveMatrix.MatrixRow(this, bi, errorMessages, false));
                    }
                }
                else
                {
                    if (bi.EffectiveMatrix != null)
                    {
                        panel.Controls.Add(bi.EffectiveMatrix.MatrixRow(this, bi, errorMessages, false));
                    }

                }

                returnValue.Controls.Add(panel);

                if (bi.branch.Product.i_Attributes_Code.ContainsKey("desc") && this.Product.isSystem(bi.path) && (!this.isOptionOrOptionHolder(System.Convert.ToString(bi.path)) || AccountHasRight(lid, "DIAGVIEW")))
                {
                    Literal lit = new Literal();
                    string txt = System.Convert.ToString(bi.branch.Product.i_Attributes_Code("desc")[0].Translation.text(bi.buyerAccount.Language));
                    lit.Text = "<div class=\'inGridDescription\'>" + txt + "</div>";
                    returnValue.Controls.Add(lit);
                }


            }
            else
            {
                returnValue.Controls.Add(NewLit("SKUless branch in grid " + this.EnglishName()));
            }
            //     Case Is = enumBt.headless  'System branches in 'configure' paradigm render as this
            //         BranchHeader.Controls.Add(Me.BranchUI(bi, -1, -1, False, hideReasons, errorMessages, lid))
        }
        else if (pbs.rca == enumBt.TROhead)
        {
            Panel p = new Panel();
            p.CssClass = "troSectionImage";
            p.ID = "troSectionImage";
            returnValue.Controls.Add(p);

            if (this.Picture != "")
            {
                WebControls.Image img = new WebControls.Image();
                img.ImageUrl = "http://www.channelcentral.net" + this.Picture;
                p.Controls.Add(img);
            }

            //auto open tro headers
            // If bs IsNot Nothing Then  'otherwise you cant (ever) close auto opening branches
            enumBt bt = (enumBt)(BTchar.IndexOf(bi.branch.rca.First));
            bs = new clsBranchState(bi.lid, bi.path, bt, bt == enumBt.gridrow, 0, 100);
        }
        else if (pbs.rca == enumBt.TROitem)
        {
            returnValue.Controls.Add(this.TROitem(bi, errorMessages));
        }
        else if (pbs.rca == enumBt.helpMechoose)
        {
            Panel p = new Panel();
            p.CssClass = "hmcSectionHeader";
            p.ID = "troSectionImage";
            returnValue.Controls.Add(p);

            p.Controls.Add(NewLit("<span class=\'hmcHead\'>" + this.Translation.text(bi.buyerAccount.Language) + "</span>"));

            //auto open tro headers
            // If bs IsNot Nothing Then  'otherwise you cant (ever) close auto opening branches
            enumBt bt = (enumBt)(BTchar.IndexOf(bi.branch.rca.First));
            bs = new clsBranchState(bi.lid, bi.path, enumBt.TROitem, bt == enumBt.gridrow, 0, 100);
        }
        else if (pbs.rca == enumBt.Hidden)
        {
            //render nothing
            Interaction.Beep();
        }
        else
        {
            errorMessages.Add("* Parent Branch RCA not set/unhandled (" + pbs.rca + ")");
        }





        if (bs != null) //switchers only appear on open branches!
        {
            if (bs.rca != enumBt.Tab)
            {
                if (bs.rca == enumBt.helpMechoose || (!(pbs.rca == enumBt.Tab && bs.rca == enumBt.gridrow) && (!this.isOptionOrOptionHolder(System.Convert.ToString(bi.path)) || AccountHasRight(lid, "DIAGVIEW")))) //supress switcher on grids hosted in tabs
                {
                    returnValue.Controls.Add(Switcher(bi, bs, false, this.rca));
                }
            }
        }


        //'Quick filters
        //If bs IsNot Nothing And Not Me.HasSKU Then
        //    'this is an 'open','category' branch (eligible for quick filters)

        //    'this is the 'show filters' button the hide filters is in the matrixheader itself
        //    If bi.MatrixHeader Is Nothing OrElse bi.MatrixHeader.QuickFiltersVisible = False Then

        //        If bi.Morethan(5) Then
        //            Dim bid As String = "hmcb." & bi.path  'just needs a unique DIV id (serves no other purpose)
        //            Dim lit As New Literal
        //            lit.Text = Replace("<div id=|" & bid & "| class=|hmc hpBlueButton ib showHMC| onclick=|getBranches('cmd=quickFilter&path=" & bi.path & "');return false|> Quick Filter </div>", "|", Chr(34))
        //            BranchHeader.Controls.Add(lit)
        //        End If
        //    End If

        //End If

        return returnValue;
    }

    private Panel TROitem(clsBranchInfo bi, List<string> errormessages)
    {
        Panel returnValue = default(Panel);

        returnValue = new Panel();
        returnValue.CssClass = "TROitem";

        Literal lt = new Literal();
        string desc = "";
        if (this.Product == null)
        {
            errormessages.Add("* Cannot render a productless branch as a TRO Item (I)");
        }
        else
        {
            if (this.Product.i_Attributes_Code.ContainsKey("desc"))
            {
                clsProductAttribute da;
                da = this.Product.i_Attributes_Code("desc")[0];
            }

            clsAttribute attribute = iq.i_attribute_code("desc");

            clsProductAttribute att = (from at in this.Product.Attributes.Values where at.attribute == attribute select at).FirstOrDefault;

            string cpkDesc = "";

            if (att != null && att.Translation != null && att.Translation.text(bi.buyerAccount.Language) != null)
            {
                cpkDesc = System.Convert.ToString(att.Translation.text(bi.buyerAccount.Language));
            }
            else
            {
                cpkDesc = System.Convert.ToString(this.Product.DisplayName(bi.buyerAccount.Language, true));

            }

            lt.Text = "<span class=\'TROpartNum\'>" + this.SKU() + "</span>&nbsp;<span class=\'TROdesc\'>" + cpkDesc + "</span>";
            // lt.Text = "<span class='TROdesc'>" & Me.Product.DisplayName(bi.BuyerAccount.Language) & "</span>"
            returnValue.Controls.Add(lt);

            returnValue.Controls.Add(this.BuyUI(bi.buyerAccount, bi.path, bi.lid));

            // Code commented as a bug has been raised by Gregg
            //If UserIsAdmin(bi.lid) Then

            //    For Each q In bi.branch.Quantities.Values
            //        Dim lit As Literal = New Literal
            //        lit.Text = "<Span>(" & q.Region.Code & ")</span>"
            //        TROitem.Controls.Add(lit)
            //    Next
            //End If

        }


        return returnValue;
    }


    public static Literal Breadcrumbs(UInt64 lid, string path, clsLanguage language, List<string> errormessages)
    {
        Literal returnValue = default(Literal);

        //for everything on the path above here that is a breadcrumb, draw one (until we reach a non breadcrumb)

        // Dim branchStates As Dictionary(Of String, clsBranchState) = CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))

        enumParadigm paradigm = (enumParadigm)(iq.sesh(lid, "Paradigm"));

        string l = "<div id=\'bcHolder\'>";
        string pth = "";
        clsAccount agentAccount = iq.sesh(lid, "AgentAccount");




        string[] segs = path.Split('.');

        string slash = " ► ";

        //Dim abovesystem As Boolean = True
        foreach (var seg in segs)
        {
            if (seg == "1" && segs.Length == 2)
            {
                break;
            }
            //            If seg <> segs.Last Then

            // If paradigm = enumParadigm.configuringSystem And iq.Branches(CInt(seg)).hassystem Then l$ &= "<div class>"

            pth += seg;
            if (pth != "tree")
            {
                if (iq.Branches(seg).Product != null)
                {
                    if (iq.Branches(seg).Product.isSystem)
                    {
                        break;
                    }
                }
                string bs = "cmd=open&path=" + pth + "&configuration=0&Paradigm=B";
                bs += "&into=tree";
                l += System.Convert.ToString("<div class=\'breadcrumbs\' onclick=|" + ButtonScript(bs) + "|>&nbsp;" + System.Convert.ToString((seg == segs[1]) ? string.Empty : slash) + iq.Branches(int.Parse(seg)).Translation.text(language).Replace("[mfr]", agentAccount.mfrCode) + "</div>".Replace("|", '\u0022'));

            }

            pth += ".";



            // End If
        }

        returnValue = new Literal();
        returnValue.Text = l + "</div>";


        return returnValue;
    }

    /// <summary>
    /// Returns the prefix plus first segment of the path - eg tree.1
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public static string rootOf(object path, List<string> errorMessages)
    {

        string[] segs = Strings.Split(System.Convert.ToString(path), ".");
        if (segs.Count() >= 2)
        {
            return segs[0] + "." + segs[1];
        }
        else
        {
            errorMessages.Add("could not find rootOf \'" + System.Convert.ToString(path) + "\'");
            return "tree.1";
        }

    }

    private static clsBranchState StateOfFirstDescendantPresent(UInt64 lid, IEnumerable<clsVisibility> values)
    {
        clsBranchState returnValue = default(clsBranchState);

        //This is not very intuitive - but, The situation arises where we switch an already opened page/branch to 'show all' (admin mode)
        //whereupon we must determine it's 'mode' (from how its first child has been rendered)
        //only (in this scenario) it's first child *was (potentially) never* rendered (as it was previously hidden by some restriction (geography, not in feed etc)
        //Because it wasn't rendered - no session variable for it (it's branchstate at that path) exists
        //so we need to iterate until we find a branch that *was* renedered (and would prior to the switch to admin mode) have been the one on display

        returnValue = null;

        Dictionary<string, clsBranchState> branchStates = (Dictionary<string, clsBranchState>)(iq.sesh(lid, "branchStates"));
        foreach (var c in values)
        {
            if (branchStates.ContainsKey(c.path))
            {
                return branchStates[c.path];
            }
        }

        return returnValue;
    }


    private List<int> skudDescendantIDs()
    {
        List<int> returnValue = default(List<int>);

        returnValue = new List<int>();
        if (this.HasSKU())
        {
            returnValue.Add(this.ID);
        }
        return returnValue; //We've hit a SKU stop recursing

        //							foreach (var child in this.childBranches.Values)
        //							{
        //								returnValue.AddRange(child.skudDescendantIDs);
        //								}

        return returnValue;
    }

    public PlaceHolder SquareUI(clsBranchInfo bi, ref List<string> hideReasons, int numSKUs, int numCats, UInt64 lid, DataView view, ref bool IsMinimised)
    {
        PlaceHolder returnValue = default(PlaceHolder);



        List<string> errormessages = new List<string>();
        clsAccount acct = iq.sesh(lid, "BuyerAccount");

        returnValue = new PlaceHolder();
        Panel tp = default(Panel);
        tp = this.Title(bi, true, false, false, numSKUs, numCats, hideReasons, errormessages);

        tp.Controls.Add(this.PromoIndicators(bi, ref errormessages)); //Put the promo indicators on squares too

        returnValue.Controls.Add(tp);

        //        Dim spacer As Panel = New Panel
        //       spacer.Attributes("style") = "height:.7em;"
        //      SquareUI.Controls.Add(spacer)

        //extract from productinfo

        Panel ppnl = new Panel(); //add the image in a DIV (so its forced onto a new line)
        if (this.Picture != "")
        {

            //; ppnl.BackColor = Drawing.Color.Aquamarine

            WebControls.Image img = new WebControls.Image();
            img.ImageUrl = imagebase + this.Picture;
            img.CssClass = "squarePhoto";
            img.Attributes.Add("onerror", "this.src=\'/images/navigation/redBlob.png\';");

            ppnl.Controls.Add(img);
            //Don't add this until later incase we have family counts turned on (complex square)

        }

        //familyFinder/HeaderSquares roll up (of what's in the view)
        Literal counts = default(Literal);
        List<Literal> attb = new List<Literal>();
        Panel promopanel = new Panel();
        clsBranchState pbs = getBranchStateAbove(bi.lid, bi.path, errormessages);
        if (pbs != null && pbs.rca == enumBt.DetailSquare)
        {
            List<int> descendants = this.skudDescendantIDs(); //this is a very simple (and fast) list of ALL the (first) SKUd descendants of this branch - it is intersected (ANDed) with the Matrix's Views Datatable - which has had more robust visibility checking

            //used to find the range (min-max) of each of the colums we want to roll up
            Dictionary<string, clsRange> ranges = new Dictionary<string, clsRange>(); //lazy re-use of a structure used elsewhere
            //Dim descendantsobj As Dictionary(Of clsBranch, clsVisibility) = bi.visibleChildren(errormessages, True, 0, 0, True)
            //bi.CreateMatrixHeader(descendantsobj, True) 'this creates the clsmatrix header AND stores it in the users session

            if (bi.EffectiveHeader != null)
            {

                foreach (var col in bi.EffectiveHeader.FieldResultSet.Keys.Where(d => d.visibleSquare))
                {
                    //If col.visibleList Then
                    ranges.Add(col.propertyName, new clsRange()); //Int64.MaxValue, Int64.MinValue))
                    // End If
                }

                //The instersection of the (unfiltered) datatable, and the descendants - tell us the number of (unfiltered, visible products)
                //The intersection of the (filtered) VIEW and the Descendant branches - tells us the number of matches

                int Count = 0;
                int matches = 0;

                //Scan ALL the UNFILTERED rows to get the of unfiltered attributes
                foreach (DataRow dr in bi.EffectiveHeader.Vw.Table.Rows) //. Item(ID)   'makes more sense to iterate over the (filtered) VIEW - iq.Translations(dr.Item(colname)).text(acct.Language) + If(col.LinkedFieldID IsNot Nothing, If(dr.Item(iq.Fields(col.LinkedFieldID).propertyName) <> UInt64.MinValue, String.Format("({0})", dr.Item(iq.Fields(col.LinkedFieldID).propertyName)), String.Empty), String.Empty) = ""ONCE than run many datatable.select min(),Max() queries
                {
                    if (descendants.Contains(System.Convert.ToInt32(dr["id"]))) //Does this row descend from this squares branch ..
                    {
                        Count++;
                        foreach (var colname in ranges.Keys)
                        {
                            clsField col = bi.EffectiveHeader.screen.i_field_property(colname);

                            if (bi.EffectiveHeader.Vw.Table.Columns.Contains(colname) && (!Information.IsDBNull(dr.Item(colname)) && dr.Item(colname) != long.MaxValue && dr.Item(colname) != long.MinValue))
                            {

                                ranges[colname].stretch(bi.EffectiveHeader.FieldResultSet(col).ConvertValueToUnit(dr.Item(colname), (dr.Item(colname + "UNIT") == DBNull.Value) ? null : (System.Convert.ToDouble(dr.Item(colname + "UNIT"))))); //updates the min and max of the range pertaining to this column
                                ranges[colname].UnitText = (dr.Item(colname + "UNIT") == DBNull.Value) ? string.Empty : (iq.Units(dr.Item(colname + "UNIT")).Symbol);
                                if (col.InputType.code != "int32" && col.InputType.code != "single" && col.InputType.code != "nullint")
                                {
                                    if (ranges[colname].TextRepresentation == null || ranges[colname].TextRepresentation == iq.Translations(dr.Item(colname)).text(acct.Language) + (col.LinkedFieldID != null ? ((System.Convert.ToInt64(dr.Item(iq.Fields(col.LinkedFieldID).propertyName)) != long.MinValue) ? (string.Format(" ({0}{1})", dr.Item(iq.Fields(col.LinkedFieldID).propertyName), iq.Units(dr.Item(iq.Fields(col.LinkedFieldID).propertyName + "UNIT")).Symbol)) : string.Empty) : string.Empty))
                                    {
                                        ranges[colname].TextRepresentation = iq.Translations(dr.Item(colname)).text(acct.Language) + (col.LinkedFieldID != null ? ((System.Convert.ToInt64(dr.Item(iq.Fields(col.LinkedFieldID).propertyName)) != long.MinValue) ? (string.Format(" ({0}{1})", dr.Item(iq.Fields(col.LinkedFieldID).propertyName), iq.Units(dr.Item(iq.Fields(col.LinkedFieldID).propertyName + "UNIT")).Symbol)) : string.Empty) : string.Empty);
                                    }
                                    else
                                    {
                                        ranges[colname].IsMixed = true;
                                        ranges[colname].TextRepresentation = null;
                                    }
                                }
                            }
                        }
                    }
                }

                //count the matches (survivors in this family)
                foreach (DataRowView dr in bi.EffectiveHeader.Vw) //
                {
                    if (descendants.Contains(System.Convert.ToInt32(dr["id"])))
                    {
                        matches++;
                    }
                }

                if (matches == 0)
                {
                    IsMinimised = true;
                }

                counts = NewLit("<div style=\"text-align:center;\"><div class=\'familyCount\'>" + System.Convert.ToString(matches) + " of " + System.Convert.ToString(Count) + " " + Xlt("match", bi.agentAccount.Language) + "</div></div>");

                if (matches == Count)
                {
                    counts = null; //remove counts on matches
                }

                attb.Add(NewLit("<div style=\"padding-top:2px;\">"));
                //output the range of each attibute
                foreach (var colname in ranges.Keys)
                {
                    if (ranges[colname].max == long.MinValue || ranges[colname].min == long.MaxValue)
                    {
                        continue;
                    }

                    clsField col = bi.EffectiveHeader.screen.i_field_property(colname);

                    if (col.propertyName.ToLower == "customerprice")
                    {
                        string MinP = "";
                        string MaxP = "";
                        MinP = new NullablePrice(ranges[colname].min / 100, bi.buyerAccount.Currency, false).text(bi.buyerAccount, errormessages);
                        MaxP = new NullablePrice(ranges[colname].max / 100, bi.buyerAccount.Currency, false).text(bi.buyerAccount, errormessages);
                        if (ranges[colname].min != long.MinValue && ranges[colname].max != long.MaxValue)
                        {
                            if (ranges[colname].min == ranges[colname].max)
                            {
                                attb.Add(NewLit("<p><span class=\'bold\'>" + col.labelText.text(bi.agentAccount.Language) + " :</span> " + MinP + "</p>"));
                            }
                            else
                            {
                                attb.Add(NewLit("<p><span class=\'bold\'>" + col.labelText.text(bi.agentAccount.Language) + " :</span> " + MinP + " to " + MaxP + "</p>"));
                            }

                        }
                    }
                    else if (col.InputType.code == "int32" || col.InputType.code == "single" || col.InputType.code == "nullint")
                    {
                        if (ranges[colname].min != long.MinValue && ranges[colname].max != long.MaxValue)
                        {
                            if (ranges[colname].min == ranges[colname].max)
                            {
                                attb.Add(NewLit("<p><span class=\'bold\'>" + col.labelText.text(bi.agentAccount.Language) + " :</span> " + ranges[colname].min + ranges[colname].UnitText + "</p>"));
                            }
                            else
                            {
                                if (ranges[colname].min == 0)
                                {
                                    attb.Add(NewLit("<p><span class=\'bold\'>" + col.labelText.text(bi.agentAccount.Language) + " :</span> up to " + ranges[colname].max + ranges[colname].UnitText + "</p>"));
                                }
                                else
                                {
                                    //attb.Add(NewLit("<p><b>" & col.labelTextLanguage & " :</b> " & ranges(colname).min & bi.EffectiveHeader.FieldResultSet(col).DisplayUnitSymbol & " to " & ranges(colname).max & bi.EffectiveHeader.FieldResultSet(col).DisplayUnitSymbol & "</p>"))
                                    attb.Add(NewLit("<p><span class=\'bold\'>" + col.labelText.text(bi.agentAccount.Language) + " :</span> " + ranges[colname].min + ranges[colname].UnitText + " to " + ranges[colname].max + ranges[colname].UnitText + "</p>"));
                                }

                            }
                        }
                    }
                    else
                    {
                        if (!ranges[colname].IsMixed)
                        {
                            attb.Add(NewLit("<p><span class=\'bold\'>" + col.labelText.text(bi.agentAccount.Language) + "</span> : " + ranges[colname].TextRepresentation + " </p>"));
                        }
                        else
                        {
                            attb.Add(NewLit("<p><span class=\'bold\'>" + col.labelText.text(bi.agentAccount.Language) + "</span> : " + Xlt("Mixed", acct.Language) + "</p>"));
                        }
                    }
                }

                attb.Add(NewLit("</div>"));

            }
        }
        else
        {
            //Simple Square, add promos
            if (iq.i_PromoRegions.ContainsKey(bi.buyerAccount.BuyerChannel.Region))
            {
                promopanel.CssClass = "square_promoPanel";
                string t = "";
                foreach (var promo in iq.i_PromoRegions(bi.buyerAccount.BuyerChannel.Region))
                {
                    if (iq.i_PromoSystemTypes.ContainsKey(promo) && iq.i_PromoSystemTypes(promo).Contains(this.Translation.text(English)))
                    {
                        t += "<li onclick=\"burstBubble(event);getBranches(\'cmd=openSquare&path=" + bi.path + "&into=tree&promoLink=" + promo.Id + "\');\" class=\'square_promo square_promo_" + promo.Code + "\'>" + promo.displayName(bi.buyerAccount.Language) + "</li>";
                    }
                }
                if (!string.IsNullOrEmpty(t))
                {
                    t = "<div class=\'square_promo_Header\'><span>Promotions</span></div><ul>" + t;
                    promopanel.Controls.Add(NewLit(t + "</ul>"));
                }

            }
        }
        if (counts != null && bi.ScreenHeader != null && bi.ScreenHeader.hasQuickFilters())
        {
            returnValue.Controls.Add(counts); //add the counts into the title panel
        }

        returnValue.Controls.Add(ppnl);
        returnValue.Controls.Add(promopanel);

        Literal lit = default(Literal);
        if (Product != null && (pbs == null || pbs.rca != enumBt.DetailSquare))
        {
            if (Product.i_Attributes_Code.ContainsKey("subTitle"))
            {
                lit = new Literal();
                lit.Text = "<div class=\'ProdSubTitle liftBottom\' style=\'clear:both\'>" + Product.i_Attributes_Code("subTitle")[0].Translation.text(s_lang) + "</div>";
                returnValue.Controls.Add(lit);
            }

            if (Product.i_Attributes_Code.ContainsKey("xNote")) //ProductNote from Products_UnionSytsems
            {
                lit = new Literal();
                lit.Text = "<div class=\'ProdSubTitle\' style = \'clear:both\'>" + Product.i_Attributes_Code("xText")[0].Translation.text(s_lang) + "</div>";
                returnValue.Controls.Add(lit);
            }
        }

        if (attb.Count > 0)
        {
            Panel txt = new Panel();
            txt.CssClass = "SquareAttributePanel";
            txt.Style.Item("overflow") = "hidden";
            foreach (Literal l in attb)
            {
                txt.Controls.Add(l);
            }
            returnValue.Controls.Add(txt);
        }

        // SquareUI.Controls.Add(Me.PromoIndicators(bi, errormessages))
        if (this.HasSKU())
        {
            returnValue.Controls.Add(this.BuyUI(bi.buyerAccount, bi.path, lid));
        }

        OutputErrors(returnValue.Controls, errormessages, bi.lid);

        return returnValue;
    }


    /// <summary>
    /// Renders the UI for a branch, and recurses for those (open) children with state
    /// </summary>
    /// <param name="bi"></param>
    /// <param name="errorMessages"></param>
    /// <returns></returns>
    /// <remarks>State is stored in the branchstates dictionary of the users session</remarks>
    public Panel UI(clsBranchInfo bi, ref string EndPath, ref List<string> errorMessages)
    {
        Panel returnValue = default(Panel);

        clsBranchState pbs = getBranchStateAbove(bi.lid, bi.path, errorMessages);
        clsBranchState bs = getbranchstate(bi.lid, bi.path); //this is my branchstae - !! which will be NOTHING if I am closed !!

        List<string> hidereasons = this.ReasonsForHide(bi.buyerAccount, bi.foci, bi.path, System.Convert.ToInt32(bi.buyerAccount.SellerChannel.priceConfig), false, ref errorMessages); //Calls GetPrices
        //   UI = New Panel

        if (hidereasons.Count > 0 && bi.showAll == false)
        {

            returnValue = new Panel();
            //UI.Controls.Add(NewLit("Hidden")) '*KW
            //UI.Controls.Add(outputMessages(hidereasons)) '*KW
            return returnValue;

        }

        //Dim maxFind As Integer
        //maxFind = 100 'If bi.branchState.state = oc.open Then maxFind = 10000 Else maxFind = 100
        //If pbs.rca = enumBt.gridrow Then maxFind = 1

        string[] segs = Strings.Split(System.Convert.ToString(bi.path), ".");
        //If segs.Count < 3 And pbs.United = False Then maxFind = 1 'Above families we do not need counts (or to recurse down to systems - UNLESS we're 'united')

        if (this.Hidden && !bi.showAll)
        {
            //a hidden branch (such as a chassis) will return an empty panel (which will not be rendered - becuase it's empty)
            returnValue = new Panel();
            if (bi.showAll)
            {
                returnValue.Controls.Add(ErrorDymo(this.displayName(English)));
            }
        }
        else
        {

            Dictionary<clsBranch, clsVisibility> descendants = new Dictionary<clsBranch, clsVisibility>();

            //Dim united As Boolean = False
            //If bs IsNot Nothing AndAlso bs.United Then united = True

            int skus = 0;
            int cats = 0;

            //descendants = bi.visibleChildren(errorMessages, united, skus, cats, fbv) '<<<THIS is where most of the action happens - if we dont need counts, we could do it only if the branch is open

            returnValue = this.BranchHeader(bi, ref bs, pbs, hidereasons, skus, cats, ref errorMessages, bi.lid);

            if (bs != null) //any branch with state is OPEN we never render the body of closed branches
            {

                //This filters the visible children against the view in effect (and returns an ordered list)
                bool fbv = true;
                if (this.HasSKU())
                {
                    fbv = false; //stop filtering by view when we're 'at' a system
                }
                //If Me.Product IsNot Nothing AndAlso Me.Product.isSystem Then fbv = False 'stop filtering by view when we're 'at' a system
                if (bi.treeMode)
                {
                    fbv = false;
                }
                descendants = bi.visibleChildren(errorMessages, bs.United, skus, cats, fbv, true); //<<<THIS is where most of the action happens - if we dont need counts, we could do it only if the branch is open

                //render the contents of this - will include it's children
                //If this is tro then nest into the header for layout....
                if (bs.rca == enumBt.TROitem)
                {
                    Dictionary<Control, Control> toadd = new Dictionary<Control, Control>();
                    Control[] ctlarr = new Control[returnValue.Controls.Count - 1 + 1];
                    returnValue.Controls.CopyTo(ctlarr, 0);
                    foreach (var c in ctlarr)
                    {
                        if (c.ID == "troSectionImage")
                        {
                            toadd.Add(c, this.BranchBody(bi, bs, pbs, returnValue, ref EndPath, ref descendants, ref errorMessages));

                        }
                    }
                    foreach (var t in toadd)
                    {
                        t.Key.Controls.Add(t.Value);
                    }
                }
                else
                {
                    returnValue.Controls.Add(this.BranchBody(bi, bs, pbs, returnValue, ref EndPath, ref descendants, ref errorMessages)); //BranchBody renders open sub branches recursively (according to thier 'renderas')
                }
            }
        }

        //UI.Controls.Add(NewLit("<span style='background-color:green;color:white;'>" & CoreCode.iicalls & "</span>"))

        OutputErrors(returnValue.Controls, errorMessages, bi.lid);
        errorMessages.Clear();

        if (bi.isTreeCursor) //And bs IsNot Nothing AndAlso bs.rca <> enumBt.Tab Then
        {
            returnValue.CssClass += " treeCursor";
        }

        return returnValue;
    }

    private dynamic rendertest(Control Panel)
    {
        StringWriter s = new StringWriter();
        HtmlTextWriter h = new HtmlTextWriter(s);
        Panel.RenderControl(h);
        s.ToString();
        h.Close();
        s.Close();
        h.Dispose();
        s.Dispose();
    }

    /// <summary>Returns a panel for an open branch - contains the product info and the children (themselves either open or closed) for the branch
    /// </summary>
    /// <param name="bi"></param>
    /// <returns></returns>
    /// <remarks>A panel populated with UI (based on the type and state of the branch)</remarks>
    private Panel BranchBody(clsBranchInfo bi, clsBranchState bs, clsBranchState pbs, Panel parentPanel, ref string EndPath, ref Dictionary<clsBranch, clsVisibility> descendants, ref List<string> errorMessages)
    {
        Panel returnValue = default(Panel);


        EndPath = System.Convert.ToString(bi.path);
        returnValue = new Panel();

        if (bs.rca == enumBt.Hidden)
        {
            return returnValue;
        }

        if (bi.treeMode)
        {
            bs.rca = enumBt.Branch; // Force tree mode to branch, would be nicer to do this on .open methods so the switcher can be used but that will take more time (and requires that the branch as B in its rca)  - ML
        }

        returnValue.ID = bi.path + ".body"; //this is a matrix (or set of child branches)

        if (bs.rca == enumBt.Upsell)
        {
            returnValue.CssClass += " tabIndent tabBody dropShadow ib upsellBody"; //Yuck
        }
        else if (bs.rca != enumBt.OpenSquare)
        {

            //need to create a matrix?

            Dictionary<string, clsScreenHeader> mh = iq.sesh(bi.lid, "screenHeaders");

            if (!mh.ContainsKey(bi.path))
            {
                int skus = 0;
                descendants = bi.visibleChildren(errorMessages, bs.United, skus, 0, false, true); //get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                if (skus > 0 && MatrixAbove(bi.lid, bi.path) != null)
                {
                    bi.CreateMatrixHeader(descendants, true); //this creates the clsmatrix header AND stores it in the users session
                }
            }
            if (mh.ContainsKey(bi.path.Substring(0, bi.path.LastIndexOf("."))))
            {
                //If mh(bi.path.substring(0,bi.path.LastindexOf("."))).hasQuickFilters() Filters.Count 0  Then
                //create matrix
                //descendants = bi.visibleChildren(errorMessages, bs.United, 0, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                //If Not mh.ContainsKey(bi.path) AndAlso (bs.United Or (descendants.Count > 0 AndAlso descendants.First().Value.branch.HasSKU())) Then ' only generate a mh if there are any products to show
                // bi.CreateMatrixHeader(descendants, True) 'this creates the clsmatrix header AND stores it in the users session
                // bi.ScreenHeader.QuickFiltersVisible = False
                //End If

                //End If
            }

            //Quick filters
            if (bs != null && !this.HasSKU() && bi.PathLevel != 1 && (!(bi.branch.Product == null)) && (!bi.branch.Product.isSystem(bi.path)))
            {
                //this is an 'open','category' branch (eligible for quick filters)

                //this is the 'show filters' button the hide filters is in the matrixheader itself
                if (bi.ScreenHeader == null)
                {
                    //do or can we have filters here
                    if (this.Matrix != null && this.Matrix.Fields.ToList().Where(f => f.Value.QuickFilterGroup != null).Count() > 0)
                    {
                        if (bi.MoreThanXskus(5)) //need to remove blank filters if posible
                        {

                            string bid = "hmcb." + bi.path; //just needs a unique DIV id (serves no other purpose)
                            Literal lit = new Literal();
                            lit.Text = "<div class=|quickFilterGroupHolder|><div id=|" + bid + "| class=|hmc hpBlueButton ib showHMC| onclick=|getBranches(\'cmd=quickFilter&path=" + bi.path + "\');return false|> " + Xlt("Filter", bi.buyerAccount.Language) + "</div></div>".Replace("|", '\u0022'); // ML Removed <div class='clear'></div> as this was adding a huge space in scenaro, add quote click on earlier breadcrumb
                            returnValue.Controls.Add(lit);
                        }
                    }
                }
            }


            if (pbs.rca == enumBt.Tab)
            {
                bi.treeWidth--;
                returnValue.CssClass += " tabIndent tabBody dropShadow ib";
            }
            else if (pbs.rca == enumBt.Branch)
            {
                bi.treeWidth = bi.treeWidth - 2.25;
                returnValue.CssClass += " treeIndent";
            }

            if (bi.ScreenHeader != null && !this.isOption(System.Convert.ToString(bi.path)))
            {
                Panel pnlMatrixHeader = new Panel();

                //Dim tw As Label = New Label
                //tw.Text = bi.treeWidth
                //tw.BackColor = Drawing.Color.Green
                //tw.ForeColor = Drawing.Color.White
                //pnlMatrixHeader.Controls.Add(tw)
                returnValue.Controls.Add(pnlMatrixHeader);

                bi.ScreenHeader.CollapseColumns(bi.treeWidth, errorMessages);

                pnlMatrixHeader.Controls.Add(bi.ScreenHeader.UI(bi, errorMessages, bi.lid));
            }
        }
        //BranchBody.Controls.Add(NewLit("<span style='background-color:yellow;'>P:" + (bi.treeWidth * 12).ToString() + ",E:" + bi.treeWidth.ToString() + "</span>"))
        if (!this.isOption(System.Convert.ToString(bi.path)) || AccountHasRight(bi.lid, "DIAGVIEW"))
        {

            if (this.Product != null)
            {
                //was branchbody
                if (Product.hasSKU)
                {
                    parentPanel.Controls.Add(ProductInfo(bi, ref errorMessages)); //.BuyerAccount, bi.path$, bi.AgentAccount.Language, True, bi.lid)) '   skud, matrixBranch, filters, sorts, showPriceInBody))
                }

                if (this.Product.isSystem(bi.path))
                {
                    if (showOptions(bi) == false)
                    {
                        return returnValue;
                    }
                }

                // BranchBody.Controls.Add(Me.SlotsTable)
            }
            else
            {
                //If Me.slots.Count Then Stop
                //If Me.Quantities.Count Then Stop
            }

            if (!(this.Product == null))
            {
                if (!(this.Product.Bundles == null))
                {
                    foreach (var bundle in this.Product.Bundles.Values)
                    {
                        if (bundle.Region.Encompasses(bi.buyerAccount.SellerChannel.Region))
                        {
                            returnValue.Controls.Add(bundle.UI);
                        }
                    }
                }
            }

            //If CType(iq.sesh(bi.lid, "paradigm"), enumParadigm) = enumParadigm.configuringSystem Or (iq.sesh(bi.lid, "configuring") IsNot Nothing AndAlso bi.PathLevel = iq.sesh(bi.lid, "configuring").ToString().Split(".").Length - 2) Then
            //    For Each d In descendants.ToList()
            //        If d.Value.path <> bi.buyerAccount.Quotes(iq.sesh(bi.lid, "QuoteID")).RootItem.Children(0).Path Then descendants.Remove(d.Key) 'iq.sesh(bi.lid, "configuring") Then descendants.Remove(d.Key)
            //    Next
            //End If

            foreach (var d in Descendants().ToList())
            {
                if (((enumParadigm)(iq.sesh(bi.lid, "Paradigm"))) == enumParadigm.configuringSystem)
                {
                    if (bs.rca == enumBt.invisibLe)
                    {
                        Descendants().Remove(d.Key);
                    }
                }
            }

            if (bs.rca == enumBt.gridrow)
            {
                parentPanel.CssClass += " openGrid dropShadow faintBorder ib";
            }

        }
        if (Descendants().Count == 0)
        {
            if (!this.isOptionOrOptionHolder(System.Convert.ToString(bi.path)))
            {
                returnValue.Controls.Add((UserIsAdmin(bi.lid)) ? (NewLit("No visible descendants")) : (NewLit(Xlt("No Results", bi.buyerAccount.Language))));
            }
            else
            {
                returnValue.Controls.Add(NewLit(""));
            }
        }
        else
        {
            //we're about to render a set of descendant branches - based on the type of the first branch we may need a header
            //(matrix view and tabs view being the main cases)
            if (bs.rca == enumBt.gridrow)
            {
                //BranchBody.CssClass &= " dropShadow faintBorder matrix"
                returnValue.CssClass += " matrix";
            }
            else if (((bs.rca == enumBt.Tab) || (bs.rca == enumBt.hYperlink)) || (bs.rca == enumBt.bighyperlinK))
            {
                if (bs.rca == enumBt.Hidden || bi.treeMode) //Me.Product IsNot Nothing AndAlso Me.Product.isSystem AndAlso bi.Paradigm = enumParadigm.AddingSystem Then
                {
                    //we don't show tabs on systems in addingsystem mode
                }
                else
                {
                    returnValue.Controls.Add(this.RenderTabHeads(bi, bs, descendants, errorMessages));
                }
            }

            if (Descendants().Count)
            {
                this.renderChildren(bi, bs, bs.rca == enumBt.gridrow, ref EndPath, descendants, ref errorMessages, returnValue); //, pnlHeadsquares)
            }
        }

        return returnValue;
    }
    /// <summary>
    /// This function is important!! If the if the product is in the basket and the paradigm is not equal to addsystem or branchinfo.paradigm = to configuringSystem or BranchInfo.treemode = true want to show options otherwise dont and return false.
    /// </summary>
    /// <param name="bi">An instance of BranchInfo.</param>
    /// <returns>A boolean value.</returns>
    /// <remarks></remarks>
    private bool showOptions(clsBranchInfo bi)
    {

        if ((clsQuote.CurrentQuoteContains(bi.lid, this.Product) && bi.Paradigm != enumParadigm.AddingSystem) || (bi.Paradigm == enumParadigm.configuringSystem || bi.treeMode))
        {
            return true;
        }
        return false;

    }

    public List<string> SystemOPGsProdRefs(object path)
    {
        List<string> returnValue = default(List<string>);

        returnValue = new List<string>();
        object[] segs = Strings.Split(System.Convert.ToString(path), ".");
        clsBranch branch = default(clsBranch);
        for (i = (segs.Length - 1); i >= 1; i--)
        {
            branch = iq.Branches(System.Convert.ToInt32(segs[i]));
            if (branch.Product != null)
            {
                if (branch.Product.isSystem)
                {
                    foreach (var av in branch.Product.AvalancheOPGs.Values)
                    {
                        foreach (var o in av.getAvalancheOptions)
                        {
                            returnValue.Add(o.ProdRef);
                        }
                    }
                }
            }
        }

        return returnValue;
    }

    public bool hasFlexAttach(clsAccount buyeraccount, object path, HashSet<string> foci, ref List<string> errormessages)
    {
        bool returnValue = false;

        int priceconfig = System.Convert.ToInt32(buyeraccount.SellerChannel.priceConfig && !8 != 0); //DON'T Check the webservice for a price when checking for flew attach

        returnValue = false;
        if (iq.PromoBranches.ContainsKey(buyeraccount.BuyerChannel) && iq.PromoBranches(buyeraccount.BuyerChannel).ContainsKey("F") && iq.PromoBranches(buyeraccount.BuyerChannel)["F"].Contains(this))
        {
            //It's possible the actual promo branches are pruned (or 'defocussed' eg.recetta view)  in this context - so we must recurse/check
            string newpath = "";
            object sysbranch = this.FindSystemAbove2(path, ref newpath);
            if (sysbranch != null && !sysbranch.hasFlexAttach(buyeraccount, newpath, foci, ref errormessages))
            {
                return false;
            }
            if (this.checkPromo(buyeraccount, path, foci, "F", priceconfig, ref errormessages))
            {
                returnValue = true;
            }
        }

        return returnValue;
    }

    public bool hasAvalanche(clsChannel buyerchannel, object path)
    {
        bool returnValue = false;

        returnValue = false;
        if (iq.PromoBranches.ContainsKey(buyerchannel))
        {
            if (iq.PromoBranches(buyerchannel).ContainsKey("A"))
            {
                if (iq.PromoBranches(buyerchannel)["A"].Contains(this))
                {
                    if (!ContainsSystem(path))
                    {
                        returnValue = true; //Above the system level - we can work based on the precalculated promobranches - below we need to worry about grafts
                    }
                    else
                    {
                        if (this.Product != null)
                        {
                            if (this.Product.isSystem)
                            {
                                returnValue = true;
                            }
                        }
                        if (returnValue == false)
                        {
                            List<string> Prodrefs = SystemOPGsProdRefs(path); //walks up the path to the system.. returns a list of the prodrefs for qualifying OPG options
                            returnValue = this.DescendantProductHasProdrefIn(Prodrefs);
                        }
                    }
                }
            }
        }

        return returnValue;
    }

    /// <summary>Returns a placeholder with UI (Letter indicators with tooldtips) for Promtions available under this branch</summary>
    public PlaceHolder PromoIndicators(clsAccount Buyeraccount, clsAccount Agentaccount, object path, HashSet<string> foci, bool inBasket, bool greyed, ref List<string> errorMessages, clsBranchInfo bi)
    {
        PlaceHolder returnValue = default(PlaceHolder);

        returnValue = new PlaceHolder();
        Label lblAvalanche = null;
        Label lblBundle = null;
        Label lblFlex = null;


        if (this.hasAvalanche(Buyeraccount.BuyerChannel, path))
        {
            lblAvalanche = new Label();
            lblAvalanche.Text = "*";
            lblAvalanche.ToolTip = Xlt("Avalanche rebates available", Buyeraccount.Language);
            lblAvalanche.CssClass = "OrangeStar";
            if (bi != null && bi.PathLevel < 4)
            {
                lblAvalanche.Attributes.Add("onclick", "burstBubble(event);getBranches(\'cmd=promofilter&path=" + path + "&promoType=*&into=tree\');return false;");
            }
            if (inBasket)
            {
                lblAvalanche.CssClass += " basketAvalanche";
            }
            if (greyed)
            {
                lblAvalanche.CssClass += " greyedPromo";
            }
            returnValue.Controls.Add(lblAvalanche);
        }

        if (iq.PromoBranches.ContainsKey(Buyeraccount.BuyerChannel) && iq.PromoBranches(Buyeraccount.BuyerChannel).ContainsKey("B") && iq.PromoBranches(Buyeraccount.BuyerChannel)["B"].Contains(this))
        {
            lblBundle = new Label();
            lblBundle.Text = "O";
            lblBundle.ToolTip = Xlt("Promotional bundles available", Buyeraccount.Language);
            lblBundle.CssClass = "bundleCircle ";
            if (bi != null && bi.PathLevel < 4)
            {
                lblBundle.Attributes.Add("onclick", "burstBubble(event);getBranches(\'cmd=promofilter&path=" + path + "&promoType=O&into=tree\');return false;");
            }
            returnValue.Controls.Add(lblBundle);
            if (inBasket)
            {
                lblBundle.CssClass += " basketBundleCircle";
            }
            if (greyed)
            {
                lblBundle.CssClass += " greyedPromo";
            }

        }

        if (this.hasFlexAttach(Buyeraccount, path, foci, ref errorMessages) && this.rca != "DGB")
        {
            lblFlex = new Label();
            lblFlex.Text = "F";
            lblFlex.ToolTip = Xlt("Flex rebates available", Buyeraccount.Language);
            lblFlex.CssClass = "flexF";
            if (bi != null && bi.PathLevel < 4)
            {
                lblFlex.Attributes.Add("onclick", "burstBubble(event);getBranches(\'cmd=promofilter&path=" + path + "&promoType=F&into=tree\');return false;");
            }
            if (inBasket)
            {
                lblFlex.CssClass += " basketFlexF";
            }
            if (greyed)
            {
                lblFlex.CssClass += " greyedPromo";
            }
            returnValue.Controls.Add(lblFlex);

        }

        //Loyalty Points

        // Loyalty points have been commented out because these are now shown
        // at the top of the quote section.

        //If Me.Product IsNot Nothing Then
        //    If Me.Product.Points.Count > 0 Then
        //        For Each scheme In Me.Product.Points.Keys
        //            If scheme.Region.Encompasses(Agentaccount.SellerChannel.Region) Then
        //                Dim lblpoints As Label
        //                lblpoints = New Label
        //                lblpoints.BackColor = Drawing.Color.HotPink
        //                lblpoints.ForeColor = Drawing.Color.White
        //                lblpoints.Text = Product.Points(scheme).ToString.Trim
        //                lblpoints.ToolTip = scheme.displayName(Agentaccount.Language) & " points"
        //                PromoIndicators.Controls.Add(lblpoints)
        //                Dim lit As Literal = New Literal
        //                lit.Text = "&nbsp;"
        //                PromoIndicators.Controls.Add(lit)
        //            End If
        //        Next
        //    End If
        //End If


        return returnValue;
    }
    /// <summary>'double' checks that there is a descendant (NOT pruned) promotional branch of the specified type)</summary>
    /// <param name="path"></param>
    /// <param name="Type">The type of promotion</param>
    /// <returns></returns>
    /// <remarks>Having tagged the promoBranches, we now only need recurse the tagged ones which *might* (probably) have a descendant promo branch (unless its pruned in this context)</remarks>
    public bool checkPromo(clsAccount buyeraccount, object path, HashSet<string> foci, string Type, int priceconfig, ref List<string> errormessages)
    {
        bool returnValue = false;

        returnValue = false;
        if (this.PruneInForce(path, buyeraccount.SellerChannel) == 0)
        {

            bool recurse = false;
            if (this.Product == null)
            {
                recurse = true; //recurse
            }
            else
            {
                //if there's a Price... (the product is visible to this user)
                recurse = this.ReasonsForHide(buyeraccount, foci, path, priceconfig, true, ref errormessages).Count() == 0; //IMPORTANT don't call the webservice
            }

            if (recurse)
            {
                if (iq.PromoBranches(buyeraccount.BuyerChannel)[Type].Contains(this) && !(this.Product == null))
                {
                    returnValue = true;
                    return returnValue;
                }
                else
                {
                    //this branch does not have a promo (of this type) on it , we don't recurse through a SKUD part (ie, we stop at the system, or the first option encountered)
                    if (this.Product != null)
                    {
                        if (this.Product.hasSKU)
                        {
                            returnValue = false;
                            return returnValue;
                        }
                    }
                }

                foreach (var ch in this.childBranches.Values)
                {
                    if (iq.PromoBranches(buyeraccount.BuyerChannel)[Type].Contains(ch)) //trivially check this branch *might* have a promo of the requisite type before recursing into it
                    {
                        if (ch.checkPromo(buyeraccount, path + "." + ch.ID, foci, Type, priceconfig, errormessages))
                        {
                            returnValue = true;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            returnValue = false;
        }

        return returnValue;
    }
    public PlaceHolder PromoIndicators(clsBranchInfo Bi, ref List<string> errormessages)
    {

        return PromoIndicators(Bi.buyerAccount, Bi.agentAccount, Bi.path, Bi.foci, false, false, ref errormessages, Bi);

    }

    public bool ContainsSystem(object path)
    {
        bool returnValue = false;

        returnValue = false;
        string[] segs = Strings.Split(System.Convert.ToString(path), ".");
        foreach (var seg in segs)
        {
            if (Val(seg) > 0)
            {
                if (iq.Branches(int.Parse(seg)).Product != null)
                {
                    if (iq.Branches(int.Parse(seg)).Product.isSystem)
                    {
                        return true;
                    }
                }
            }
        }

        return returnValue;
    }


    public clsBranch FindSystemAbove(object path, ref object newpath)
    {
        newpath = path;
        string[] segs = Strings.Split(System.Convert.ToString(path), ".");
        for (i = segs.Count() - 1; i >= 0; i--)
        {
            //For Each seg In segs
            if (Val(segs[i]) > 0)
            {

                if (iq.Branches(int.Parse(segs[i])).Product != null)
                {
                    if (iq.Branches(int.Parse(segs[i])).Product.isSystem)
                    {
                        return iq.Branches(int.Parse(segs[i]));
                    }
                }
                newpath = Strings.Left(System.Convert.ToString(newpath), Strings.InStrRev(System.Convert.ToString(newpath), ".") - 1);
            }
        }
        return null;
    }

    public clsBranch FindSystemAbove2(object path, ref object newpath)
    {
        newpath = path;
        string[] segs = Strings.Split(System.Convert.ToString(path), ".");
        for (i = segs.Count() - 1; i >= 0; i--)
        {
            //For Each seg In segs
            if (Val(segs[i]) > 0)
            {

                newpath = Strings.Left(System.Convert.ToString(newpath), Strings.InStrRev(System.Convert.ToString(newpath), ".") - 1);

                if (iq.Branches(int.Parse(segs[i])).Product != null)
                {
                    if (iq.Branches(int.Parse(segs[i])).Product.isSystem)
                    {
                        return iq.Branches(int.Parse(segs[i]));
                    }
                }
            }
        }
        return null;
    }

    public bool DescendantProductHasProdrefIn(List<string> ProdRefs)
    {
        bool returnValue = false;

        //Recursivley checks wether any descendendant product of this branch has a ProdRef attribute value in the supplied list

        returnValue = false;

        if (!(this.Product == null))
        {
            if (this.Product.i_Attributes_Code.ContainsKey("ProdRef"))
            {
                if (ProdRefs.Contains(this.Product.i_Attributes_Code("ProdRef")[0].Translation.text(English)))
                {
                    return true;
                }
            }
        }

        foreach (var b in this.childBranches.Values)
        {
            //todo - don't recurse branches that have no avalanche optiosn on them
            if (b.DescendantProductHasProdrefIn(ProdRefs))
            {
                return true;
            }
        }

        return returnValue;
    }


    //Private Function ChildOpen(lid As UInt64, ByVal path$) As Boolean

    //    'Returns true if a child of the specified path is open

    //    ChildOpen = False

    //    Dim branchStates As Dictionary(Of String, clsBranchState) = CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
    //    For Each p In branchStates.Keys
    //        If Left(p, Len(path$) + 1) = path$ & "." Then
    //            If branchStates(p).state = oc.open Then
    //                ChildOpen = True : Exit Function
    //            End If
    //        End If
    //    Next
    //End Function

    private Panel ProductInfo(clsBranchInfo bi, ref List<string> errorMessages) //buyeraccount As clsAccount, path$, language As clsLanguage, showprice As Boolean, lid As UInt64) As Panel  ', skud As Boolean, matrixBranch As clsBranch, filter$, sort$, showprice As Boolean) As PlaceHolder
							{
								
								Panel ui = new Panel();
								//ui.Attributes("style") = "width:100%;margin-bottom:.5em;"
								ui.CssClass = "prodInfo";
								// ui.CssClass = "treeIndent"
								
								Literal lit = default(Literal);
								
								
								//If Bi.branchState.state = oc.open Then
								
								//If Product.i_Attributes_Code.ContainsKey("subTitle") Then
								//    lit = New Literal
								//    lit.Text = "<div class='ProdSubTitle'>" & Product.i_Attributes_Code("subTitle")(0).Translation.text(s_lang) & "</div>"
								//    ui.Controls.Add(lit)
								//End If
								
								if (Product.i_Attributes_Code.ContainsKey("xNote")) //ProductNote from Products_UnionSytsems
								{
									lit = new Literal();
									lit.Text = "<div class=\'ProdSubTitle xXote\'>" + Product.i_Attributes_Code("xText")[0].Translation.text(s_lang) + "</div>";
									ui.Controls.Add(lit);
								}
								
								
								//the description is on the branch header- so not needed here (see gregs powerpoint)
								
								//If Product.i_Attributes_Code.ContainsKey("desc") Then
								//    lit = New Literal
								//    lit.Text = "<div class='prodDesc desc'>" & Product.i_Attributes_Code("desc")(0).Translation.text(s_lang) & "</div>"
								//    ui.Controls.Add(lit)
								//End If
								
								//the product photo is floated left of the desctiption and subtitle - so we need to clear the float
								// lit = New Literal
								// lit.Text = "<div style='height:0px;clear:both';>&nbsp;</div>"
								// ui.Controls.Add(lit)
								
								if (this.Product.hasSKU) //we ONLY display prices on those products with a SKU (some products are placeholders)
								{
									if (this.Product.isSystem(bi.path))
									{
										ui.CssClass += " isSystem";
									}
									
									//  If showprice Then
									//          ui.Controls.Add(Me.PriceUI(buyeraccount, path$)) ' , skud, matrixBranch, filter, sort))
									//  End If
									List<clsQuantity> preinstalled = bi.branch.GetPreInstalledRecursive(bi.buyerAccount.SellerChannel.Region, bi.path, errorMessages);
									
									
									Panel st = ExpandablePanel(ui, "Specification", "st", bi);
									if (st != null)
									{
										st.Controls.Add(this.Product.Spectable(bi.buyerAccount.Language, preinstalled, bi.branch, bi.path, bi.showAll));
									}
									//Dim spectable As Panel = Me.Product.Spectable(bi.buyerAccount.Language, preinstalled, bi.branch, bi.path)
									//spectable.ID = "spec." & bi.path
									
									//'spectable starts collapsed - so the collapse button is initially visible
									//Dim SpecHeader = New Panel()
									//SpecHeader.CssClass = "specHeader"
									//Dim btnCollapse = New Literal  ' when the collapseButton is pressed we will . . .
									//Dim omd$ = "$(this).toggleClass('collapsed');" 'toggle the + and -
									//omd$ &= "$(document.getElementById('spec." & bi.path$ & "')).toggle();"   'show the expand button
									//omd$ &= "return false;"  'supress the postback
									//btnCollapse.Text = Replace("<div id=|collapseSpec." & bi.path & "| class=|expandContract collapsed| onclick=|" & omd$ & "|>&nbsp;</div> ", "|", Chr(34))
									//SpecHeader.Controls.Add(btnCollapse)
									
									//SpecHeader.Controls.Add(NewLit("<span class='specHeader'>Specification</span>"))
									
									//ui.Controls.Add(SpecHeader)
									
									//ui.Controls.Add(spectable)
									
								}
								
								//Dim hl As New HyperLink
								//hl.Target = "new"
								//hl.NavigateUrl = "edit.aspx?path=Products(" & Me.Product.ID & ").i_variants(Channels(" & buyeraccount.SellerChannel.ID & "))" & "&lid=" & lid
								//hl.Text = "Edit Price Variants"
								//ui.Controls.Add(hl)
								
								//     ui.Controls.Add(NewLit("<p>" & bi.path & "</p>"))
								
								// If False Then
								
								if (AccountHasRight(bi.lid, "DIAGVIEW"))
								{
									
									//Dim pth As Literal = New Literal
									//pth.Text = "<div style='background-color:magenta;color:white;'>" & bi.path & "</div>"
									//pth.Text &= "<div style='background-color:cyan;color:blue;'>" & PathName(bi.path) & "</div>"
									//ui.Controls.Add(pth)
									
									
									string ttl = "Slots";
									if (this.Product != null && this.Product.isSystem)
									{
										ttl = "System Slots";
									}
									Panel st = ExpandablePanel(ui, "Slots", "ss", bi);
									
									if (st != null)
									{
										st.Controls.Add(NewLit("<div class=\'adminHelp\'>Slots are attached to system and option branches - \'gives\' slots are positive numbers - and generally appear on systems, \'takes\' slots are negative numbers and (generally) appear against options.</div>"));
										st.Controls.Add(this.SlotsTable(bi));
									}
									
									//don't show 'chassis slots link (at all) on options
									if (this.Product != null && this.Product.isSystem)
									{
										
										//locate the chassis branch
										System.Boolean cbl = from b in this.childBranches.Values where b.slots.Count > 0 select b; //Where b.EnglishName.ToLower.Contains("chassis")
										
										if (cbl.Any) //is there a chassis branch
										{
											foreach (var b in cbl) // there shouls only be  1 !! 'Dim b As clsBranch = cbl.First
											{
												Panel cp = ExpandablePanel(ui, b.displayName(English) + " Slots (" + System.Convert.ToString(b.countGrafts()) + " models)", "cs" + System.Convert.ToString(b.ID), bi);
												if (cp != null)
												{
													cp.Controls.Add(NewLit("<div class=\'adminHelp\'>Slots which are common to a number of machines in a family are defined on the (sub) chassis</div>"));
													clsBranchInfo cbi = new clsBranchInfo(bi.lid, bi.path +"." + b.ID.ToString(), bi.lblMatches, bi.treeWidth, bi.Paradigm, errorMessages);
													cp.Controls.Add(b.SlotsTable(cbi));
													//    Exit For 'there should be only 1 !
												}
												//  Beep()
											}
											
										}
									}
									
									Panel qp = ExpandablePanel(ui, "Quantities", "qt", bi);
									if (qp != null)
									{
										qp.Controls.Add(NewLit("<div class=\'adminHelp\'>Quantities control, Regionalisaion, Pre-installed componentry and AutoAdds</div>"));
										qp.Controls.Add(this.QuantitiesTable(bi.buyerAccount, bi, ref errorMessages));
									}
									
									Panel ap = ExpandablePanel(ui, "Attributes", "at", bi);
									if (ap != null)
									{
										ap.Controls.Add(NewLit("<div class=\'adminHelp\'>Attributes hold core product information and form much of the \'spec table\'</div>"));
										ap.Controls.Add(this.AttributeTable(bi.buyerAccount, bi, errorMessages));
									}
									
									Panel bp = ExpandablePanel(ui, "Branches", "tl", bi);
									if (bp != null)
									{
										bp.Controls.Add(NewLit("<div class=\'adminHelp\'>Shows other branches with this SKU/Product</div>"));
										bp.Controls.Add(this.BranchesTable(bi)); //AttributeTable(bi.buyerAccount, bi.path, errorMessages))
									}
									
									//ui.Controls.Add(NewLit("<span class=""expandContract collapsed specHeader"" onclick=""burstBubble(event);$(this).next().next().toggle();$(this).toggleClass('collapsed');"">&nbsp;</span>"))
									//ui.Controls.Add(NewLit("<span class=""specHeader"">System Slots</span>"))
									//Dim t As Table = Me.SlotsTable(bi)
									//t.Style("display") = "none"
									//ui.Controls.Add(t)
									
									//output the slots of the chassis too
									//ui.Controls.Add(NewLit("<span class=""expandContract collapsed specHeader"" onclick=""burstBubble(event);$(this).next().next().toggle();$(this).toggleClass('collapsed');"">&nbsp;</span>"))
									//ui.Controls.Add(NewLit("<span class=""specHeader"">Chassis Slots</span>"))
									//Dim p As Panel = New Panel()
									//p.Style("display") = "none"
									//For Each b In Me.childBranches.Values
									//    Dim cbi As New clsBranchInfo(bi.lid, bi.path & "." & b.ID.ToString, bi.lblMatches, bi.treeWidth, bi.Paradigm, errorMessages)
									//    p.Controls.Add(b.SlotsTable(cbi))
									//Next
									//ui.Controls.Add(p)
									
									//            ui.Controls.Add(NewLit("<span class=""expandContract collapsed specHeader"" onclick=""burstBubble(event);$(this).next().next().toggle();$(this).toggleClass('collapsed');"">&nbsp;</span>"))
									//           ui.Controls.Add(NewLit("<span class=""specHeader"">Preinstalled, quantityrestrictions & localisations)</span>"))
									//          ui.Controls.Add(Me.PreinstalledTable(bi.buyerAccount, bi.path, errorMessages))
									
									//ui.Controls.Add(NewLit("<span class=""expandContract collapsed specHeader"" onclick=""burstBubble(event);$(this).next().next().toggle();$(this).toggleClass('collapsed');"">&nbsp;</span>"))
									//ui.Controls.Add(NewLit("<span class=""specHeader"">Attributes</span>"))
									//ui.Controls.Add(Me.AttributeTable(bi.buyerAccount, bi.path, errorMessages))
								}
								
								return ui;
								
							}
    /// <summary>
    /// Renders an expandable panel - who's (expanded/collapsed) state is maintained server side under the session LID
    /// </summary>
    /// <param name="addTo">Into which div should we place this expandadable panel</param>
    /// <param name="title">Display title</param>
    /// <param name="uniquizer">Short code, used along with bi.path to create a unique</param>
    /// <param name="bi">the BI.LID and BI.Path are used internally</param>
    /// <returns>A reference to the (empty) content panel - for you to add content to IF it's expanded or nothing</returns>



    private Panel ExpandablePanel(Panel addTo, string title, string uniquizer, clsBranchInfo bi)
    {

        Panel outerPanel = new Panel();
        //     outerPanel.CssClass = "ib"
        Panel contentPanel = default(Panel);
        addTo.Controls.Add(outerPanel);

        string css = "";
        object oc = null;
        string ky = "expanded_" + uniquizer + "_" + bi.path;
        //determines whether a + or a - button shows
        if (iq.SeshContains(bi.lid, ky))
        {
            //we are currently expanded
            css = "expandContract specHeader";
            oc = ButtonScript("path=" + bi.path + "&cmd=collapsepanel&key=" + ky);
            contentPanel = new Panel();

        }
        else
        {
            //we are currently collapsed
            css = "expandContract collapsed specHeader";
            oc = ButtonScript("path=" + bi.path + "&cmd=expandpanel&key=" + ky);
            contentPanel = null;
        }

        outerPanel.Controls.Add(NewLit("<span class=\"" + css + "\" onclick=\"" + System.Convert.ToString(oc) + "\">&nbsp;</span>"));
        outerPanel.Controls.Add(NewLit("<span class=\'specHeader\'>" + title + "</span>"));

        if (contentPanel != null)
        {
            outerPanel.Controls.Add(contentPanel);
        }

        return contentPanel;

    }

    private Table AttributeTable(clsAccount buyeraccount, clsBranchInfo bi, List<string> errormessages)
    {
        Table returnValue = default(Table);

        returnValue = new Table();
        returnValue.CssClass = "adminTable";

        TableHeaderRow thr = MakeTHR("Name,Value,Text,Unit,Delete", "", "");
        returnValue.Controls.Add(thr);

        if (this.Product != null)
        {
            TableRow tr = default(TableRow);
            TableCell td = default(TableCell);
            foreach (var pa in this.Product.Attributes.Values)
            {
                tr = new TableRow();
                returnValue.Controls.Add(tr);
                if (pa.deleted)
                {
                    tr.CssClass += " deletedRow";
                }

                td = new TableCell();
                tr.Controls.Add(td);
                td.Text = pa.Attribute.Translation.text(buyeraccount.Language);
                tr.Controls.Add(td);

                td = new TableCell();
                tr.Controls.Add(td);
                td.Text = pa.NumericValue.ToString();
                tr.Controls.Add(td);

                td = new TableCell();
                tr.Controls.Add(td);
                td.Text = pa.Translation != null ? (pa.Translation.text(buyeraccount.Language)) : "";
                tr.Controls.Add(td);

                td = new TableCell();
                tr.Controls.Add(td);
                td.Text = pa.Unit != null ? pa.Unit.Symbol : "";
                tr.Controls.Add(td);

                td = new TableCell();
                tr.Controls.Add(td);
                if (!pa.deleted)
                {
                    Literal lt = FunctionButton(bi.path, pa.Product.ID, "deleteProductAttribute&PAID=" + pa.ID, "DEL", "Delete this product attribute");
                    td.Controls.Add(lt);
                }
                else
                {
                    Literal lt = FunctionButton(bi.path, pa.Product.ID, "unDeleteProductAttribute&PAID=" + pa.ID, "unDEL", "Undelete this product attribute");
                    td.Controls.Add(lt);
                }
            }

            tr = new TableRow();
            returnValue.Controls.Add(tr);
            td = new TableCell();
            tr.Controls.Add(td);
            td.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit these attributes", bi.agentAccount.Language), "window.open(\'edit.aspx?path=Products(" + this.Product.ID + ").Attributes&TreePath=" + bi.path + "&lid=" + bi.lid.ToString() + "\');return(false);", "", "width:25px;height:25px;", bi.buyerAccount.Language));

        }
        return returnValue;
    }


    private int countGrafts()
    {
        int returnValue = 0;

        //return the number of grafts of this branch

        SqlClient.SqlConnection con = da.OpenDatabase;
        returnValue = System.Convert.ToInt32(da.DBSelectFirst("SELECT COUNT(*) AS c FROM [graft] WHERE fk_branch_id_source=" + System.Convert.ToString(this.ID)));
        con.Close();

        return returnValue;
    }
    public dynamic findSKUpaths(string FindSku, object path, List<string> Paths, bool crossSkus)
    {

        //adds all the paths a sku appears at (below this branch) to the list

        if (this.Product != null)
        {
            if (Product.SKU == FindSku)
            {
                Paths.Add(path);
            }
            if (crossSkus == false && this.Product.hasSKU)
            {
                return default(dynamic);
            }
        }


        foreach (var b in this.childBranches.Values)
        {
            b.findSKUpaths(FindSku, path + "." + b.ID, Paths, crossSkus);
        }

    }


    private Table BranchesTable(clsBranchInfo bi)
    {

        Table tbl = new Table();
        tbl.Attributes("class") = "adminTable";

        TableHeaderRow thr = MakeTHR("path", "", "");
        tbl.Controls.Add(thr);

        TableRow tr = default(TableRow);
        TableCell td = default(TableCell);

        //iq.RootBranch.SkuPaths(

        List<string> paths = new List<string>();
        iq.RootBranch.findSKUpaths(this.Product.SKU, "tree." + iq.RootBranch.ID, paths, !this.Product.isSystem);

        foreach (var pth in paths)
        {

            tr = new TableRow();
            tbl.Controls.Add(tr);

            td = new TableCell();
            tr.Controls.Add(td);
            td.Controls.Add(KWbreadcrumbs(bi.lid, pth, English, true, false, "", true));

            //td = New TableCell
            //tr.Controls.Add(td)

        }

        return tbl;

    }
    internal dynamic ancestorMinorFamilies(List<string> all)
    {

        //    Dim fams As List(Of String) = New List(Of String)
        if (this.Product != null && this.Product.i_Attributes_Code.ContainsKey("FamMinor"))
        {
            string fm = System.Convert.ToString(this.Product.i_Attributes_Code("FamMinor")[0].Translation.text(English));
            if (!all.Contains(fm))
            {
                all.Add(fm);
            }

            return default(dynamic);
            //Return Me.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English)
        }

        //If Me.AllParents.Values.Count = 0 Then Return "Undetermined"
        // all.AddRange(fams)
        foreach (var p in this.AllParents.Values)
        {
            p.ancestorMinorFamilies(all);
        }

    }

    private dynamic SystemLocalisationTable()
    {

    }

    private Panel QuantitiesTable(clsAccount buyeraccount, clsBranchInfo bi, ref List<string> errormessages)
    {

        Panel pnl = new Panel();

        clsRegion region = default(clsRegion);
        //Presintalled options
        if (!(this.Product == null))
        {


            //todo Title
            object lt;

            //localisations
            //(heavily simplifived version of the branches quantities - which can be edited more fully in the editor)
            foreach (var q in this.Quantities.Values)
            {
                if (q.Path == bi.path || q.Path == "")
                {
                    pnl.Controls.Add(NewLit("<span style=\'background-color:#004040;color:white;\'>" + q.Region.Code + (q.NumPreInstalled ? "(" + q.NumPreInstalled + ")" : "") + "</span>&nbsp;"));
                }
            }

            //Edit localistaiotns button
            pnl.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit these quantity rows", bi.agentAccount.Language), "window.open(\'edit.aspx?path=Branches(" + System.Convert.ToString(this.ID) + ").Quantities&TreePath=" + bi.path + "&lid=" + bi.lid.ToString() + "\');return(false);", "", "width:25px;height:25px;", bi.buyerAccount.Language));


            region = buyeraccount.SellerChannel.Region;

            //get the qtys that apply to my region (wider regions will tend to have LESS qtys) (think about it!)
            List<clsQuantity> preinstalled = null;
            if (this.Product != null && this.Product.isSystem)
            {
                preinstalled = this.GetPreInstalledRecursive(region, bi.path, ref errormessages);
            }

            if (preinstalled != null)
            {

                Table tbl = new Table();
                pnl.Controls.Add(tbl);
                tbl.CssClass = "adminTable";
                TableRow tr = default(TableRow);
                TableCell td = default(TableCell);


                string help = "Use the buttons to edit individual quantity rows|";
                help += "Path at which the quantity specifically only works (or blank for everywhere)|";
                help += "Number Preinstalled|";
                help += "Minimum increment (users must add this many at a time to a system)|,";
                help += "Preferred increment (users are *recommended* to add this many at a time (e.g. memory modules the perfom best in threes)|";
                help += "What is the product type of the product, of the branch, that this quanity is attached to|";
                help += "Is this (auto-added) quanitity FREE OF CHARGE (ie. \'preinstalled\')|";
                help += "In which region/county does this quantity specifically apply|";
                help += "This quanity is attached to a branch which appears in many locations - is it pruned here|";
                help += "You can \'soft delete\' quantities (and undelete them if you change your mind)";

                TableHeaderRow thr = MakeTHR("Edit,Path,Num,MinIncr,PrefIncr,ProdType,FOC,Region,Pruned,Delete", help, "");

                tbl.Controls.Add(thr);
                tr = new TableRow();
                tbl.Controls.Add(tr);
                td = new TableCell();
                tr.Controls.Add(td);
                td.Text = "Pre Installed (Quantities on descendant branches)";

                foreach (var i in preinstalled)
                {
                    //we don't render ALL the descendant quanitites (many are regionalisations for options under this system) -the hae a numinstalled of zero and 1,1 for min and pref increments .. but (importantly) a region.
                    //If i.Path.Contains(bi.path) Or i.Path = "" Then
                    tbl.Controls.Add(i.adminTableRow(bi));
                    //End If
                }
            }
        }

        return pnl;

    }
    private Panel adminControls(clsBranchInfo bi, string PanelId, object path)
    {

        object url = null;

        Panel outerpanel = new Panel(); //holds the checkbox
        outerpanel.CssClass = "ib";
        Panel adminPanel = new Panel();
        outerpanel.Controls.Add(adminPanel);

        adminPanel.ID = "admin_" + System.Convert.ToString(path);

        //  adminPanel.Attributes("class") &= "admin_collapsed"
        //adminPanel.Attributes("onclick") = "this.style.width='230px';this.style.height='auto';"

        //toggle between expanded and collapsed (ie.. if you are currently collapsed (when clicked) then switch to then admin_expanded class
        //    Dim collapserOnClickScript As String = _
        //    "burstBubble(event);var ex;ex=document.getElementById('admin_" & path$ & "');" & _
        //      "if (ex.className=='admin_collapsed'){ex.className='admin_expanded'} " & _
        //      "else {ex.className='admin_collapsed'};return(false)"

        //this DIV is just a 'spacer', the expand/collapse image is actually in the background of the admintools
        //(so we can change the appearance of it client side - with no server side knowledge of the exapnd/collapsed state))
        //adminPanel.Controls.Add(NewLit("<div style='width:30px;height:30px;display:inline-block;' onclick=" & Chr(34) & collapserOnClickScript & Chr(34) & "></div>"))



        adminPanel.Controls.Add(NewLit("&nbsp;"));

        //todo - implement commands - plus button graphics
        if (bi.branch.DisplayName(English).ToLower == "all options")
        {
            if (bi.branch.locked)
            {
                adminPanel.Controls.Add(MakeRoundButton("lock.png", Xlt("click to unlock (allow imports to overwrite)", bi.agentAccount.Language), ButtonScript("cmd=unlock&path=" + bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language));
            }
            else
            {
                adminPanel.Controls.Add(MakeRoundButton("unlock.png", Xlt("click to lock (prevent imports overwriting)", bi.agentAccount.Language), ButtonScript("cmd=lock&path=" + bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language));
            }
            adminPanel.Controls.Add(NewLit("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp"));
        }

        adminPanel.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit - Edits this branch (and the product, slots, quantities etc. attached to it))", bi.agentAccount.Language), "window.open(\'edit.aspx?path=Branches(" + System.Convert.ToString(this.ID) + ")&TreePath=" + System.Convert.ToString(path) + "&lid=" + bi.lid.ToString() + "\');", "", "width:25px;height:25px;", bi.buyerAccount.Language));

        string tt = "";
        if (this.deleted)
        {
            tt = System.Convert.ToString(Xlt("Undeletes (reinstates) this branch everywhere))", bi.agentAccount.Language));
            adminPanel.Controls.Add(MakeRoundButton("undelete.png", tt, ButtonScript("cmd=unDeleteBranch&path=" + bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language));
        }
        else
        {
            //only put delete buttons on options for now .. (Systems and categories will need cascading deletes - which will need more thought)

            tt = System.Convert.ToString(Xlt("Marks the branch as deleted (everywhere it appears) " + bi.branch.AllPaths.Count + " Locations", bi.agentAccount.Language));
            adminPanel.Controls.Add(MakeRoundButton("delete.png", tt, ButtonScript("cmd=deleteBranch&path=" + bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language));
        }

        //adminPanel.Controls.Add(MakeRoundButton("copy.png", Xlt("Copy - Copy - Mark this branch for a subsequent graft/adopt operation", bi.agentAccount.Language), "copyBranch(" & Me.ID.ToString.Trim & ");", "", "width:25px;height:25px;", bi.buyerAccount.Language))
        adminPanel.Controls.Add(MakeRoundButton("Graft.png", Xlt("Graft - attaches the branch you have previously copied or pruned, to this branch as a new child", bi.agentAccount.Language), "pasteBranch(" + this.ID.ToString().Trim() + ",\'" + PanelId.Trim() + "\');", "", "width:25px;height:25px;", bi.buyerAccount.Language));
        adminPanel.Controls.Add(MakeRoundButton("Prune.png", Xlt("Prune - (deletes) this specific occurance of the branch (the same branch may still appear elsewhere)", bi.buyerAccount.Language), "pruneBranch(\'" + System.Convert.ToString(path) + "\');", "", "width:25px;height:25px;", bi.buyerAccount.Language));

        tt = System.Convert.ToString(Xlt("Shred preview (shows the impact of shredding (completely destroying this branch and all its descendants and dependecies would be)", bi.agentAccount.Language));
        adminPanel.Controls.Add(MakeRoundButton("ShredPreview.png", tt, ButtonScript("cmd=previewShredBranch&path=" + bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language));

        object ttt = Xlt("Validate - Runs a test quote/Validation on every system under this branch.", bi.agentAccount.Language);
        adminPanel.Controls.Add(MakeRoundButton("tick.png", ttt, ButtonScript("cmd=quoteAll&path=" + bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language));


        if (!(this.Parent == null))
        {
            adminPanel.Controls.Add(MakeRoundButton("Retract.png", Xlt("Retract - removes this branch and promotes all of its children to its level (useful for collapsing redundant categories)", bi.agentAccount.Language), "retractBranch(\'" + System.Convert.ToString(path) + "\');", "", "width:25px;height:25px;", bi.buyerAccount.Language));
        }
        adminPanel.Controls.Add(MakeRoundButton("genericFilter.png", Xlt("Clone, makes an independent \'deep\' copy of this branch (as a sibling) - use to add a model to a family", bi.agentAccount.Language), "clone(\'" + System.Convert.ToString(path) + "\');", "", "width:25px;height:25px;", bi.buyerAccount.Language));
        adminPanel.Controls.Add(MakeRoundButton("adopt.png", Xlt("Adopt - Makes this branch the new parent of the Selected branches ", bi.agentAccount.Language), "adopt(\'" + System.Convert.ToString(path) + "\');", "", "width:25px;height:25px;", bi.buyerAccount.Language));

        string bs = ButtonScript("cmd=snap&=path=" + bi.path);
        adminPanel.Controls.Add(MakeRoundButton("hierarchy.png", Xlt("XML Snapshot from this point", bi.agentAccount.Language), bs, "", "width:25px;height:25px;", bi.buyerAccount.Language));


        //different approach (ML) - haven't attempted topring ito the fold - i think he hads an event handelr with jquery
        if (this.Matrix != null)
        {
            adminPanel.Controls.Add(NewLit("<div title=\'" + this.Matrix.ID.ToString() + "\' class=\"hasScreen\"></div>"));
        }


        // adminPanel.Controls.Add(MakeRoundButton("cross.png", Xlt("Having - Marks this branch (and all its descendants) as being Incompatible with ...", bi.agentAccount.Language), "setHaving('" & path & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))
        // adminPanel.Controls.Add(MakeRoundButton("excl.png", Xlt("Excludes - Excludes all items under this branch (in combintaion with the having button)", bi.agentAccount.Language), "makeExclude('" & path & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))

        // Dim excludedBy As List(Of clsExclude) = Me.isExcludedBy
        foreach (var eb in this.ExcludedBy()) // I' can  be excluded by more than one branch
        {

            string tooltip = "EDIT - This branch is excluded by Having " + eb.havingAnyOf.First.EnglishName + "....(" + eb.Reason + ")";
            url = "edit.aspx?path=Excludes(" + eb.ID + ")&TreePath=" + System.Convert.ToString(path);
            adminPanel.Controls.Add(MakeLinkButton("isexcl.png", tooltip, url, bi.buyerAccount.Language));
        }

        foreach (var e in this.iExclude())
        {

            HyperLink btn = new HyperLink();
            string ToolTip = "EDIT - Having anything under this branch excludes excludes " + e.excludesAllOf.First.EnglishName + "....(" + e.Reason + ")";
            url = "edit.aspx?path=Excludes(" + e.ID + ")&TreePath=" + System.Convert.ToString(path);

            adminPanel.Controls.Add(MakeLinkButton("excl.png", ToolTip, url, bi.buyerAccount.Language));
        }


        //the ecb  (editor check box) class has no styling effect but is used in the JS to GetElementsByClassName
        adminPanel.Controls.Add(NewLit("&nbsp;<Input title=\'used to select multiple branches for Adopt operation\' type=\'checkbox\' style=\'vertical-align:top\' class=\'ecb ib\' id=\'cb" + bi.path + "\'></input>&nbsp;"));


        return outerpanel;

    }

    private List<clsExclude> iExclude()
    {
        List<clsExclude> returnValue = default(List<clsExclude>);

        returnValue = new List<clsExclude>();

        foreach (var exclude in iq.Excludes.Values)
        {
            if (exclude.havingAnyOf.Contains(this))
            {
                returnValue.Add(exclude);
            }
        }

        return returnValue;
    }

    private List<clsExclude> ExcludedBy()
    {
        List<clsExclude> returnValue = default(List<clsExclude>);

        returnValue = new List<clsExclude>();
        foreach (var exclude in iq.Excludes.Values)
        {
            if (exclude.excludesAllOf.Contains(this))
            {
                returnValue.Add(exclude);
            }
        }

        return returnValue;
    }

    //Private Function CountLabel(language As clsLanguage, buyerAccount As clsAccount, Path$) As Literal
    //    'add the count of sub products (or families - or options or whatever - with the appropriate wording - including singulars)
    //    Return lit
    //End Function

    private dynamic Title(clsBranchInfo bi, bool ShowCount, bool Previewchildren, bool offsetCount, int numSKUs, int numCats, List<string> hideReasons, List<string> errorMessages, clsBranchState pbs = null, clsBranchState bs = null)
    {
        dynamic returnValue = default(dynamic);

        //Clickable section title (will jump the TreeCursor)

        returnValue = new Panel();



        //hyperlinks (replace some of the options tabs)
        if (pbs != null && (pbs.rca == enumBt.hYperlink || pbs.rca == enumBt.bighyperlinK))
        {
            //If pbs.rca = enumBt.bighyperlinK Then
            //    Title.cssclass = "bigLink "
            //Else
            //    Title.cssclass = "link optionsLink"
            //End If

            if (bs != null)
            {
                returnValue.CssClass += " visited";
            }
            if (this.deleted)
            {
                returnValue.cssclass += " strikethru";
            }
        }
        else
        {
            returnValue.cssclass = "branchTitle";
            if (this.deleted)
            {
                returnValue.cssclass += " strikethru";
            }

        }

        if (hideReasons.Count > 0)
        {
            returnValue.CssClass += " HiddenProduct";
            foreach (var h in hideReasons)
            {
                returnValue.tooltip += h;
            }
        }


        Label tl = new Label();
        returnValue.Controls.Add(tl);

        // Dim lbl As Label = New Label
        // lbl.Text = " " & bi.path
        // Title.controls.add(lbl)

        //If iq.sesh(bi.lid, "configuring") IsNot Nothing AndAlso Me.Product IsNot Nothing AndAlso Me.Parent.Translation.text(English) IsNot Nothing Then
        //    tl.Text = Me.Parent.Translation.text(English) + " - "
        //    If Me.Product IsNot Nothing AndAlso Me.Product.i_Attributes_Code.ContainsKey("SC") Then
        //        tl.Text &= Me.Product.i_Attributes_Code("SC")(0).displayName(English) + " - "
        //    End If
        //Else
        //    tl.Text = String.Empty
        //End If

        tl.Text += FormatName(bi.agentAccount, System.Convert.ToString(this.Translation.text(bi.buyerAccount.Language)));

        //If Not Me.Translation Is Nothing Then
        //    tl.Text &= "-" & Me.Translation.text(bi.BuyerAccount.Language)
        //    'For the System Title in 'Configuring' mode - prepend the Family name and Supply chain
        //    If iq.sesh(bi.lid, "configuring") IsNot Nothing AndAlso Me.Product IsNot Nothing Then

        //        'the family name is the systems parent branches name (this is the correctly 'unabreviated' one)
        //        tl.Text = Me.Parent.Translation.text(English)

        //        If Me.Product IsNot Nothing Then
        //            Dim supplychain As String = ""
        //            If Me.Product.i_Attributes_Code.ContainsKey("SC") Then
        //                supplychain = Product.i_Attributes_Code("SC")(0).Translation.text(bi.BuyerAccount.Language)
        //            End If

        //            If supplychain <> "" Then
        //                tl.Text &= " - " & supplychain
        //            End If
        //            tl.Text &= "-" & Me.Translation.text(bi.BuyerAccount.Language) ' My branch name (the system)
        //        Else
        //            tl.Text = "HP" 'not sure what this is for - I may have stuffed it up when altering martrins code
        //        End If
        //    End If
        //End If



        //Active filters
        Dictionary<string, clsScreenHeader> mh = iq.sesh(bi.lid, "matrixHeaders");

        //Removed for Greg, now at filterui level
        //If mh IsNot Nothing Then
        //    Dim pth As String = ""
        //    For Each seg In Split(bi.path, ".")
        //        pth &= seg

        //        If mh.ContainsKey(pth) Then
        //            If mh(pth).Vw IsNot Nothing Then
        //                If mh(pth).Vw.RowFilter <> "" Then
        //                    'find the first thing whos rca is NOT opensquares .. render from and into there
        //                    Dim fe As String = "<span title='Filters are in effect (click to remove) " & mh(pth).Vw.RowFilter & _
        //                        "' onclick=|" & ButtonScript("cmd=removeFilters&path=" + bi.path & "&into=" + pth + "&filterPath=" & pth) & "|>*</span>"
        //                    fe = Replace(fe, "|", Chr(34))
        //                    Title.Controls.Add(NewLit(fe))
        //                End If
        //            End If
        //        End If
        //        pth &= "."
        //    Next
        //End If

        if (Previewchildren) //this Is to see what will be on tabs (as tooltips) - there's no 'easy' was - as we have open
        {
            List<string> bn = new List<string>();
            foreach (var c in (from cb in childBranches.Values orderby cb.order select cb)) //Me.childBranches.Values
            {
                //add it to the preview if it is a (or has a descendant) visible,SKUd product
                //         If c.Descendants(True, bi.BuyerAccount, bi.Foci, bi.path$ & "." & Trim$(CStr(c.ID)), False, False, 1, True, errorMessages, False).Count > 0 Then
                // bn.Add(c.DisplayName( bi.BuyerAccount.Language))
                // End If
            }
            if (bn.Count > 0)
            {
                tl.ToolTip = string.Join(",", bn.ToArray) + ".";
            }
        }

        //  tl.ToolTip &= "branchID:" & Me.ID & " " & Me.childBranches.Count & " children"

        object occ = null;
        //moveTreeCursor - jumps the tree cursor to this section without altering the open/closed state of the branch - calls maniupulation.aspx?cursor=..
        occ = "moveTreeCursor(\'" + bi.path + "\');return false;";

        returnValue.Attributes("onclick") = occ;

        if (ShowCount)
        {
            Literal lit = new Literal();

            //Child branch count (number in parenthesis at the end of each line)
            if (this.childBranches.Count > 0)
            {

                //This is the count of skud products
                //Which at various points we refer to as different things (systems, option, drives, modules etc.)

                //Dim childProducts As Integer = Me.ChildProductCount(bi.BuyerAccount, bi.Foci, bi.path, False, bi.ShowAll)

                object ProdWord = null;

                if (plen(bi.path) < 3)
                {

                    string cnoun = "";
                    //If numCats = 1 Then
                    //    cnoun = Me.collectiveNounSingular.text(s_lang)
                    //Else
                    if (this.CollectiveNoun == null)
                    {
                        cnoun = "products";
                    }
                    else
                    {
                        cnoun = System.Convert.ToString(this.CollectiveNoun.text(s_lang));
                    }
                    //End If

                    //Dim skus As String = ""
                    //If numSKUs >= 100 Then
                    //    skus = "100+"
                    //Else
                    //    skus = numSKUs.ToString
                    //End If

                    //in the 'Squares' mode, we break the count label out of the flow and put it under the title (becuase horizontal space is tight)
                    if (numSKUs == 0 & numCats > 1)
                    {
                        lit.Text = "<div class=\'childCount" + (offsetCount ? " squareCount" : "").ToString() + "\'>(" + System.Convert.ToString(numCats) + " " + cnoun;
                        lit.Text += ")</div>";
                    }
                    else
                    {

                        if (numSKUs > 2)
                        {
                            if (Strings.Split(System.Convert.ToString(bi.path), ".").Count() > 5)
                            {
                                ProdWord = "options";
                            }
                            else
                            {
                                ProdWord = "systems";
                            }

                            lit.Text = "<div class=\'childCount" + (offsetCount ? " squareCount" : "").ToString() + "\'>(" + System.Convert.ToString(numSKUs) + " " + System.Convert.ToString(ProdWord);

                            //If numCats = 1 Then
                            //    lit.Text &= ")</div>"
                            //Else
                            //    If False Then 'bi.branchState.United Or Split(bi.path, ".").Count < 5 Then   -Nobbled at greg/dans request
                            //        lit.Text &= " in " & numCats & "&nbsp;" & cnoun & ")</div>"
                            //    Else
                            lit.Text += ")</div>";
                            //  End If
                        }
                    }
                }
                returnValue.Controls.Add(lit);
            }
        }


        return returnValue;
    }

    private string FormatName(clsAccount agentAccount, string title)
    {
        string returnValue = "";

        if (title.IndexOf("[") < 0)
        {
            return title; // Bale out if there are no substitutions
        }

        returnValue = title.Replace("[mfr]", System.Convert.ToString(agentAccount.mfrCode));

        return returnValue;
    }

    private PlaceHolder ExpandCollapseButton(clsBranchState pbs, clsBranchInfo bi, clsBranchState bs, List<string> errorMessages)
    {
        PlaceHolder returnValue = default(PlaceHolder);

        returnValue = new PlaceHolder();

        string visclass = string.Empty;

        bool diagnostic = System.Convert.ToBoolean(AccountHasRight(bi.lid, "DIAGVIEW"));
        if (!diagnostic)
        {
            //reasons NOT to display an open/close button
            //If Me.isOptionOrOptionHolder Then Exit Function    ' SK - OptionHolders allowed through to show the expander
            if (this.isOption())
            {
                return returnValue;
            }
            if (this.PruneInForce(bi.path, bi.buyerAccount.SellerChannel) != 0)
            {
                return returnValue;
            }
        }

        Literal lit = new Literal();
        if (bs == null)
        {
            //No Branch State - We are closed - Add an expand (+) button
            lit.Text = "<div class=\'expandContract collapsed\' title=\'" + Xlt("Click to expand", bi.buyerAccount.Language) + "\' onclick=|" + ButtonScript("cmd=open&path=" + bi.path) + "|>&nbsp;</div>";
            lit.Text = Strings.Replace(System.Convert.ToString(lit.Text), "|", System.Convert.ToString('\u0022'));
            returnValue.Controls.Add(lit);
        }
        else
        {
            //there is branchstate (we are open) - Add a collapse (-) button
            lit.Text = "<div class=\'expandContract" + visclass + "\'  onclick=|" + ButtonScript("cmd=close&path=" + bi.path) + "|>&nbsp;</div>";
            lit.Text = Strings.Replace(System.Convert.ToString(lit.Text), "|", System.Convert.ToString('\u0022'));
            returnValue.Controls.Add(lit);
        }

        return returnValue;
    }

    private PlaceHolder oldExpandCollapseButton(clsBranchState pbs, clsBranchInfo bi, clsBranchState bs, List<string> errorMessages)
    {
        PlaceHolder returnValue = default(PlaceHolder);

        returnValue = new PlaceHolder();

        //Dim visible As Boolean = True
        //If Not bi.ShowAll Then  'Show all is 'admin mode' - where we wall draw all branches (regardless of Geographic restrictions and prunes)
        //    If Not Me.Product Is Nothing Then
        //        Dim rh As List(Of String) = Me.ReasonsForHide(bi.BuyerAccount, bi.Foci, bi.path$, bi.BuyerAccount.SellerChannel.priceConfig, True, errorMessages)
        //        If rh.Count <> 0 Or bi.ShowAll Then
        //            visible = False
        //        End If
        //    End If
        //End If

        string visclass = string.Empty;
        //If visible Then visclass = " greyed" 'this product Isnt visibisible (we must be in 'show all' mode)

        if (this.PruneInForce(bi.path, bi.buyerAccount.SellerChannel) != 0)
        {
            //this branch is pruned here
            return returnValue;

        }
        else
        {
            //We make a close button for all open branches, ... individual branches we opened and then closed (at any given level) have their RCA set to hidden
            if (this.Product != null)
            {
                //where we have used 'openttreeto' (primarily show in tree buttons in the basket) - we render the branches with + signs (as expandable)
                if (bs != null && this.Product.isSystem(bi.path) && (!this.isOptionOrOptionHolder(System.Convert.ToString(bi.path)) || pbs.rca == enumBt.Branch || AccountHasRight(bi.lid, "DIAGVIEW")))
                {
                    //add the close (-) button . .
                    Literal lit = new Literal();
                    lit.Text = "<div class=\'expandContract" + visclass + "\'  onclick=|" + ButtonScript("cmd=close&path=" + bi.path) + "|>&nbsp;</div>"; //-
                    lit.Text = Strings.Replace(System.Convert.ToString(lit.Text), "|", System.Convert.ToString('\u0022'));
                    returnValue.Controls.Add(lit);
                }
            }
        }

        if (bs == null && (!this.isOptionOrOptionHolder(System.Convert.ToString(bi.path)) || pbs.rca == enumBt.Branch || AccountHasRight(bi.lid, "DIAGVIEW")))
        {

            // Don't display an expand/collapse box on systems if they are being displayed as components of another system
            if (this.Product == null || (string.IsNullOrEmpty(System.Convert.ToString(this.Product.SKU)) || this.Product.isSystem(bi.path) || AccountHasRight(bi.lid, "DIAGVIEW")))
            {
                if (bs == null)
                {
                    //No Branch State - We are closed - Add an expand button
                    Literal lit = new Literal();
                    lit.Text = "<div class=\'expandContract collapsed\' title=\'" + Xlt("Click to expand", bi.buyerAccount.Language) + "\' onclick=|" + ButtonScript("cmd=open&path=" + bi.path) + "|>&nbsp;</div>";
                    lit.Text = Strings.Replace(System.Convert.ToString(lit.Text), "|", System.Convert.ToString('\u0022'));
                    returnValue.Controls.Add(lit);
                }
                else
                {
                    //there is branchstate (we are open) - Add a collapse button
                    Literal lit = new Literal();
                    lit.Text = "<div class=\'expandContract" + visclass + "\'  onclick=|" + ButtonScript("cmd=close&path=" + bi.path) + "|>&nbsp;</div>"; //-
                    lit.Text = Strings.Replace(System.Convert.ToString(lit.Text), "|", System.Convert.ToString('\u0022'));
                    returnValue.Controls.Add(lit);
                }
            }
        }
        return returnValue;
    }

    private Panel Switcher(clsBranchInfo bi, clsBranchState bs, bool atSkus, string RCAs) //View switcher
    {
        Panel returnValue = default(Panel);

        bool allButtons = false;
        returnValue = new Panel();
        returnValue.CssClass = "switcher";

        //Dim q$ = Chr(34)

        //Dim css$ = ""
        //If current = bt.Branch Then css$ = "selected" Else css$ = ""
        //SwitchViewButtons.Controls.Add(MakeRoundButton("tree.png", "View in a tree", ButtonScript(bi, "branches"), css, ""))

        //If current = bt.gridrow Then css$ = "selected" Else css$ = ""
        //SwitchViewButtons.Controls.Add(MakeRoundButton("matrix.png", "View/compare in a grid", ButtonScript(bi, "grid"), css, ""))

        //'Squares button
        //If current = bt.Square Then css$ = "selected" Else css$ = ""
        //SwitchViewButtons.Controls.Add(MakeRoundButton("squares.png", "View as tiles", ButtonScript(bi, "squares"), css, ""))

        //'Tabs button
        //If current = bt.Tab Then css$ = "selected" Else css$ = ""
        //SwitchViewButtons.Controls.Add(MakeRoundButton("tabs.png", "View on tabs", ButtonScript(bi, "tabs"), css, ""))


        if (rca.Trim().Length <= 1)
        {
            return returnValue; //Only one choice - so we dont render a switcher
        }

        Dictionary<enumBt, string> vt = new Dictionary<enumBt, string>();
        //these will need translating
        vt.Add(enumBt.Branch, Xlt("Branches", bi.agentAccount.Language));
        vt.Add(enumBt.gridrow, Xlt("Grid", bi.agentAccount.Language));
        vt.Add(enumBt.Square, Xlt("Squares", bi.agentAccount.Language));
        vt.Add(enumBt.Tab, Xlt("Tabs", bi.agentAccount.Language));
        vt.Add(enumBt.OpenBranch, Xlt("Branches (open)", bi.agentAccount.Language));
        vt.Add(enumBt.DetailSquare, Xlt("Squares", bi.agentAccount.Language));
        vt.Add(enumBt.helpMechoose, Xlt("Help Me Choose", bi.agentAccount.Language));


        foreach (var t in vt.Keys.ToArray)
        {
            if (!RCAs.Contains(System.Convert.ToString(BTchar(t))))
            {
                vt.Remove(t);
            }
        }

        Dictionary<enumBt, string> dicCss = new Dictionary<enumBt, string>();
        dicCss.Add(enumBt.Branch, "v_Branch");
        dicCss.Add(enumBt.gridrow, "v_Grid");
        dicCss.Add(enumBt.Square, "v_Square");
        dicCss.Add(enumBt.Tab, "v_Tab");
        // dicCss.Add(enumBt.BreadCrumb, "v_Breadcrumb")
        dicCss.Add(enumBt.OpenBranch, "v_Branch");
        dicCss.Add(enumBt.DetailSquare, "v_CompSquare");
        dicCss.Add(enumBt.helpMechoose, "v_HelpMeChoose");

        Literal lit = default(Literal);
        if (dicCss.ContainsKey(bs.rca) && vt.ContainsKey(bs.rca)) //(branches may be hidden))
        {

            lit = new Literal();

            lit.Text = "\r\n" + "<!--View Switch DropDown-->" + "\r\n" + "<div id=\'outer." + bi.path + "\' class=\'dd_form dd_closed\'>";
            // lit.Text &= "<div class='dd_wrap'>"
            lit.Text += "<div id=\'ddh." + bi.path + "\' class=\'dd_head " + System.Convert.ToString(dicCss[bs.rca]) + "\'";
            lit.Text += " onmousedown=|burstBubble(event);";
            lit.Text += "displayDropDown(\'" + bi.path + "\') |";
            lit.Text += " style=\'z-index: 3;\'>"; ///*downside is the 'thin' bottom border and should be added when it's expanded
            lit.Text = lit.Text.Replace("|", '\u0022');
            //lit.Text &= "<span class='dd_label_text'><span class='js_dd_input_value dd_input_value dd_selectedOption'>" & vt(current) & "</span><span class='dd_icn_container'><span class='dd_icn'>&nbsp;</span></span></span></a>"
            lit.Text += "<span>&nbsp;</span><span class=\'dd_icn_container\'></span><span id=\'txt." + bi.path + "\' style=\'display:none\'>" + System.Convert.ToString(vt[bs.rca]) + "</span>";
            lit.Text += "</div>";

            lit.Text += "\r\n" + "<!--DropDownBody-->" + "\r\n";
            lit.Text += "<div id=\'ddb." + bi.path + "\' class=\'dd_thinBottom dd_form\' style=\'visibility:visible;display: none;\'>"; //block
            //        lit.Text &= "<div class='js_dd_list_items dd_list_items h150'>"


            foreach (var nonselected in from j in vt select j) //Where j.Key <> current
            {
                //NB the buttonscript needs an ENGLISH version *tabs,squares,branches or grid*
                lit.Text += "<div class=\'dd_item " + System.Convert.ToString(dicCss[nonselected.Key]) + "\' onmousedown=|" + ButtonScript("cmd=switchTo&bt=" + BTchar(nonselected.Key) + "&path=" + bi.path) + ";|>";
                lit.Text += "<span>" + nonselected.Value + "</span>";
                lit.Text += "</div>" + "\r\n";
                lit.Text = lit.Text.Replace("|", '\u0022');
            }

            //        lit.Text &= "</div>"
            // lit.Text &= "</div>"
            lit.Text += "</div> <!--/DropDrownBody-->" + "\r\n";
            lit.Text += "</div><!--/Outer-->";

            returnValue.Controls.Add(lit);

        }

        object css = null;

        bool vegas = true;

        if (!vegas)
        {
            if (!atSkus)
            {

                //Unite
                if (bs.United)
                {
                    css = "selected";
                }
                else
                {
                    css = "";
                }
                returnValue.Controls.Add(MakeRoundButton("unite.png", "Unite (View all products)", ButtonScript("cmd=unite&path=" + bi.path), css, "", bi.buyerAccount.Language));

                if (!bs.United)
                {
                    css = "selected";
                }
                else
                {
                    css = ""; //divided
                }
                returnValue.Controls.Add(MakeRoundButton("divide.png", "Divide (View categories)", ButtonScript("cmd=divide&path=" + bi.path), css, "", bi.buyerAccount.Language));

            }
        }

        return returnValue;
    }


    public static string ButtonScript(string cmd)
    {
        string returnValue = "";

        //Builds the JS for a getBranches

        //burstbubble stops even propagation (the event firing on ancestor element)
        //return false - stops the 'default' (submit/postback) action on button elements

        if (cmd.IndexOf("path=") + 1 == 0)
        {
            Debugger.Break();
        }
        returnValue = "burstBubble(event);$(this).attr(\'onclick\',\'\');getBranches(\'" + cmd + "\');return false;";

        return returnValue;
    }


    public List<clsProduct> SystemsThatTake(clsProduct system, ref List<clsProduct> mustHost, clsBundle bundle)
    {

        //recurses, typically from ther root branch to return a list of all the systems for which all the items in the bundle are an option.
        //used only during import to implement Chris's bright idea of allowing NULLs on the bundleIndex_ISSRebates.systems (to mean appy to all systems)

        //This is not an easy function to understand.. mustHost is a set of options - copied from the bundle every time we encounter a system
        //options (non-system products) are removed from this 'musthost' checklist as they are encountered - once we have them all - we add the system to the list
        // all the lists are concatenated by the addrange... the whole thing is recursive

        List<clsProduct> retval = new List<clsProduct>();

        if (!(this.Product == null))
        {
            if (this.Product.isSystem)
            {
                system = this.Product;
                if (mustHost == null)
                {
                    mustHost = new List<clsProduct>();
                }
                mustHost.Clear();
                foreach (var i in bundle.Items.Values)
                {
                    mustHost.Add(i.Product);
                }
            }
            else
            {
                if (!(system == null)) //have we traveresed a system yet
                {
                    if (mustHost.Contains(this.Product))
                    {
                        mustHost.Remove(this.Product);
                    }
                    if (mustHost.Count == 0)
                    {
                        retval.Add(system);
                        return retval;
                    }
                }
            }
        }

        List<clsProduct> sys = default(List<clsProduct>);
        foreach (var b in this.childBranches.Values)
        {
            sys = b.SystemsThatTake(system, mustHost, bundle);
            if (sys.Count > 0)
            {
                retval.AddRange(sys);
                return retval;
            }
        }

        return retval;

    }

    [Obsolete("Based on flawed logic - ml")]
    private int OptLevel()
    {

        if (this.Translation != null && this.Translation.Group.StartsWith("OL"))
        {
            return int.Parse(System.Convert.ToString(this.Translation.Group.Substring(this.Translation.Group.Length - 1, 1)));
        }
        if (this.Parent != null && this.Parent.Translation != null && this.Parent.Translation.Group.StartsWith("OL"))
        {
            return int.Parse(System.Convert.ToString(this.Parent.Translation.Group.Substring(this.Parent.Translation.Group.Length - 1, 1))) + 1;
        }
        if (this.Parent != null && this.Parent.Parent != null && this.Parent.Parent.Translation != null && this.Parent.Parent.Translation.Group.StartsWith("OL"))
        {
            return int.Parse(System.Convert.ToString(this.Parent.Parent.Translation.Group.Substring(this.Parent.Parent.Translation.Group.Length - 1, 1))) + 2;
        }


    }

    /// <summary>Returns True If the Branch is an option, or hold options</summary>
    /// <param name="lid"></param>
    /// <returns></returns>
    /// <remarks>Userd to supress switchers</remarks>
    private bool isOptionOrOptionHolder(string path = "")
    {

        //        If UserIsAdmin(lid) Then Return True 'FOR ADMINS - all branches should  be expandable

        return (
            this.Product != null && this.Product.hasSKU() && !this.Product.isSystem(path)) || (this.childBranches.Count > 0 && this.childBranches.First.Value.Product != null && this.childBranches.First.Value.Product.hasSKU() && !this.childBranches.First.Value.Product.isSystem(path));

    }

    private bool isOption(string path = "")
    {
        return (this.Product != null && this.Product.hasSKU() && !this.Product.isSystem(path));
    }

    public void BuildPathDic(string ck, Dictionary<string, clsBranch> lDic, bool useMe)
    {
        if (this.HasSKU())
        {
            return;
        }
        if (useMe && this.Translation != null && Information.IsNumeric(Strings.Right(System.Convert.ToString(this.Translation.Group), 1)) && Strings.Right(System.Convert.ToString(this.Translation.Group), 1) > ck.Split('^').Length + 1)
        {
            ck = ck + "^";
        }
        if (useMe)
        {
            ck = ck + ((string.IsNullOrEmpty(ck)) ? "" : "^") + Translation.text(English);
            if (!lDic.ContainsKey(ck))
            {
                lDic.Add(ck, this);
            }
        }
        foreach (var child in childBranches)
        {
            child.Value.BuildPathDic(ck, lDic, true);
        }
    }

    public List<string> AllPaths()
    {
        List<string> returnValue = default(List<string>);
        if (this.ID == 1)
        {
            return new List<string>() { "tree.1" };
        }
        returnValue = new List<string>();
        foreach (var p in AllParents.Values)
        {
            returnValue.AddRange(p.AllPaths().Select(f => f + "." + System.Convert.ToString(this.ID)));
        }
        return returnValue;
    }

    //Private Sub LogMessage(message As String)

    //    If (Not log4net.LogManager.GetRepository().Configured) Then
    //        Config.XmlConfigurator.Configure()
    //    End If
    //    log.Info(message)

    //End Sub
} //clsbranch