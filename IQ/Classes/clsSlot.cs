using dataAccess;
using System.Xml;


public class clsSlot : i_Editable
{

    //Physical slots in a machine

    public int ID { get; set; }
    public string path { get; set; } //OPTIONAL where (the context in which) this 'gives' works  - leave blank for it to work wherever it is grafted
    public clsBranch Branch { get; set; } //the branch (which may appear in many locations in the tree) to which these slots apply - single branch can have many of the same slot type with different paths (one for each of the positiosn it's grafted at)
    public clsSlotType Type { get; set; }

    public bool deleted { get; set; }

    public clsSlotType NonStrictType
    {
        get
        {
            if (iq.StrictSlotValidation || Type.EnforceMinorCode)
            {
                return Type;
            }
            else
            {
                clsSlotType st = default(clsSlotType);
                if (iq.i_slotType_Code(Type.MajorCode).ContainsKey(""))
                {
                    st = iq.i_slotType_Code(Type.MajorCode)[""];
                }
                else
                {
                    st = new clsSlotType(Type.MajorCode, "") { Translation = (Type.TranslationShort != null ? Type.TranslationShort : Type.Translation), TranslationShort = Type.TranslationShort };
                }
                //Dim st = iq.SlotTypes.Where(Function(slt) slt.Value.MajorCode = Type.MajorCode AndAlso slt.Value.MinorCode = If(Type.TranslationShort IsNot Nothing, Type.TranslationShort.Key.ToString, "")).FirstOrDefault

                //Return If(st.Value Is Nothing, , st.Value)
                return st;
            }
        }
    }

    public int numSlots { get; set; } //slots given + / - taken (per item)
    public clsTranslation notes { get; set; }
    public IQ.NullableInt slotNum { get; set; } // for 'gives' slots you *can* specify the slot number
    //                               (and must do so, if specifying more than one slot of the same type in the same product)
    //                               numslots MUST be 1 if slotNum is specified
    //                               slotnum MUST be null for 'takes' slots (there is no functionality to specify that a particular card must go in a particular slot)

    public int requiredFill { get; set; } // the number of "given" slots (of this type) that *must* be filled - eg.. you MUST have a PSU in certain servers
    public int advisedFill { get; set; }

    public string CurrentCompoundKey;

    public clsSlot clone(object newpath)
    {

        return new clsSlot(this.Type, this.Branch, System.Convert.ToString(newpath), this.numSlots, this.notes, this.slotNum, this.requiredFill, this.advisedFill);

    }

    public dynamic writeXml(xmltextwriter W)
    {

        clsSlot with_1 = this;

        W.WriteStartElement("slot");

        W.WriteStartAttribute("id");
        W.WriteString(with_1.ID.ToString());
        W.WriteEndAttribute();

        W.WriteStartAttribute("majorCode");
        W.WriteString(with_1.Type.MajorCode.ToString());
        W.WriteEndAttribute();

        W.WriteStartAttribute("minorCode");
        W.WriteString(with_1.Type.MinorCode.ToString());
        W.WriteEndAttribute();

        W.WriteStartAttribute("numSlots");
        W.WriteString(with_1.numSlots.ToString());
        W.WriteEndAttribute();

        if (with_1.notes != null)
        {
            W.WriteStartAttribute("notes");
            W.WriteString(with_1.notes.text(English));
            W.WriteEndAttribute();
        }
        if (with_1.slotNum.sqlvalue != "null")
        {
            W.WriteStartAttribute("slotNum");
            W.WriteString(with_1.slotNum.sqlvalue);
            W.WriteEndAttribute();
        }

        W.WriteEndElement(); ///slot


        //Return String.Format("<slot id='{0}' majorType='{1}' minorType='{2}' numSlots='{3}' notes='{4}' slotNum'{5}'/>" _
        //                              , .ID, _
        //                              xmlEncode(.Type.MajorCode), _
        //xmlEncode(.Type.MinorCode), _
        //                          .numSlots, _
        //                        If(.notes Is Nothing, "", xmlEncode(.notes.text(English))), _
        //                      .slotNum.sqlvalue)

    }

    public clsSlot(clsSlotType type, clsBranch Branch, string Path, int numslots, clsTranslation notes, IQ.NullableInt slotnum, int requiredfill, int advisedFill, DataTable writecache = null)
    {

        this.path = Path;
        this.Branch = Branch;
        this.Type = type;
        this.numSlots = numslots;
        this.notes = notes;
        this.slotNum = slotnum;
        this.requiredFill = requiredfill;
        this.advisedFill = advisedFill;


        if (Type.ID <= 0)
        {
            Debugger.Break();
        }

        //        If Val(slotnum.sqlvalue) > 100 Then Stop

        if (Branch != null && Branch.ID > 0) //A temproary (branchless) slot is created during import - just so we can constuct/access a compound key
        {

            object nk = null;
            if (notes == null)
            {
                nk = "null";
            }
            else
            {
                nk = notes.Key;
            }

            if (Branch.i_Slots == null)
            {
                Branch.i_Slots = new Dictionary<string, clsSlot>();
            }

            CurrentCompoundKey = this.compoundKey();
            if (Branch.slots == null)
            {
                Branch.slots = new Dictionary<int, clsSlot>();
            }

            if (writecache == null)
            {
                object sql = null;
                sql = "INSERT INTO [slot] (path,fk_branch_id,fk_slottype_id,numslots,slotnum,fk_translation_key_notes,requiredFill,advisedFill) ";
                sql += "VALUES (" + da.SqlEncode(Path) + "," + Branch.ID + "," + Type.ID + "," + System.Convert.ToString(numslots) + "," + slotNum.sqlvalue + "," + System.Convert.ToString(nk) + "," + System.Convert.ToString(requiredfill) + "," + System.Convert.ToString(advisedFill) + ");";

                try
                {

                    this.ID = System.Convert.ToInt32(da.DBExecutesql(sql, true));
                }
                catch
                {
                    Interaction.Beep();
                }


                Branch.slots.Add(this.ID, this);
                //    If Not Branch.i_Slots.ContainsKey(CurrentCompoundKey) Then
                Branch.i_Slots.Add(CurrentCompoundKey, this);
                //End If

            }
            else
            {

                if (Branch.i_Slots.ContainsKey(CurrentCompoundKey))
                {
                    // Beep()
                    //no biggie (the same brachs is master into many FAMILIES
                    //Logit("Duplicate branch slot key " & CurrentCompoundKey)
                }
                else
                {

                    // Me.ID = -1
                    System.Data.DataRow row = default(System.Data.DataRow);
                    row = writecache.NewRow();
                    row["path"] = this.path;
                    row["fk_branch_id"] = this.Branch.ID;
                    row["fk_slottype_id"] = this.Type.ID;
                    row["numslots"] = this.numSlots;

                    if (this.slotNum.sqlvalue == "null")
                    {
                        row["slotnum"] = DBNull.Value;
                    }
                    else
                    {
                        row["slotnum"] = this.slotNum.sqlvalue;
                    }

                    if ((string)nk == "null")
                    {
                        row["fk_translation_key_notes"] = DBNull.Value;
                    }
                    else
                    {
                        row["fk_translation_key_notes"] = nk;
                    }

                    row["requiredFill"] = requiredfill;
                    row["advisedFill"] = advisedFill;
                    row["deleted"] = false;
                    writecache.Rows.Add(row);
                    Branch.i_Slots.Add(CurrentCompoundKey, this);

                    //  Branch.slots.Add(Me.ID, Me) 'new
                }

            }

            //Note - when created wth a writecache - the slots are not added to the branches (becuase they have no ID's yet)

        }



    }

    public bool HasSlotNum()
    {
        bool returnValue = false;

        returnValue = false;

        if (this.slotNum != null)
        {
            if (this.slotNum.value != null)
            {
                if (!(this.slotNum.value == DBNull.Value))
                {
                    returnValue = true;
                }
            }
        }

        return returnValue;
    }

    public string compoundKey()
    {

        //used to make a lookup in the branches slot sorteddictionary - having them in a sorted dictionary means they can be presented in a sensible order
        return this.Type.MajorCode + "_" + this.Type.MinorCode + "_" + this.path + "_" + Math.Sign(this.numSlots) + "_" + this.slotNum.sqlvalue;

    }

    public string NonStrictCompoundKey()
    {

        //used to make a lookup in the branches slot sorteddictionary - having them in a sorted dictionary means they can be presented in a sensible order
        return this.NonStrictType.MajorCode + "_" + this.NonStrictType.MinorCode + "_" + this.path + "_" + Math.Sign(this.numSlots) + "_" + this.slotNum.sqlvalue;

    }


    public dynamic update(clsBranch newbranch, string newpath)
    {


        //this is a littel delicat e- we have to maintain the index of slits on the branch carefully.
        this.Branch.i_Slots.Remove(this.compoundKey());

        this.Branch = newbranch;
        this.path = newpath;

        object sql = null;
        sql = "UPDATE [slot] set ";
        sql += "path=" + da.SqlEncode(this.path) + ",fk_branch_id=" + this.Branch.ID;
        sql += " WHERE ID=" + System.Convert.ToString(this.ID);

        da.DBExecutesql(sql, false);

        this.CurrentCompoundKey = this.compoundKey();
        this.Branch.i_Slots.Add(this.CurrentCompoundKey, this);


    }

    public void update(ref List<string> Errormessages)
    {

        try
        {

            if (this.Type.ID <= 0)
            {
                Debugger.Break();
            }

            object sql = null;
            sql = "UPDATE [slot] set ";
            sql += "path=" + da.SqlEncode(this.path) + ",fk_branch_id=" + this.Branch.ID;
            sql += ",fk_slottype_id=" + this.Type.ID + ",numslots=" + System.Convert.ToString(this.numSlots);
            sql += ",requiredFill=" + System.Convert.ToString(this.requiredFill);
            sql += ",advisedfill=" + System.Convert.ToString(this.advisedFill);
            if (this.notes != null)
            {
                sql += ",fk_translation_key_notes=" + this.notes.Key;
            }
            else
            {
                sql += ",fk_translation_key_notes=null";
            }
            sql += ",deleted=" + System.Convert.ToString(this.deleted ? 1 : 0);


            sql += " WHERE ID=" + System.Convert.ToString(this.ID);

            da.DBExecutesql(sql, false);


            if (Branch.i_Slots.ContainsKey(CurrentCompoundKey))
            {
                this.Branch.i_Slots.Remove(CurrentCompoundKey);
            }

            if (!this.deleted)
            {
                CurrentCompoundKey = compoundKey();
                this.Branch.i_Slots.Add(CurrentCompoundKey, this);
            }


        }
        catch (System.Exception ex)
        {

            Errormessages.Add(ex.Message);
        }

    }

    public clsSlot()
    {

        //needs to add it to the parent products distionary of slots

    }

    public dynamic Insert(ref List<string> Errormessages)
    {
        if (this.Branch == null)
        {
            this.Branch = iq.RootBranch; //temporary for editor, always gets updated...
        }
        this.Type = iq.SlotTypes.First.Value;

        return new clsSlot(this.Type, this.Branch, this.path, this.numSlots, this.notes, this.slotNum, this.requiredFill, this.advisedFill);

    }

    public clsSlot(int ID, clsSlotType type, clsBranch Branch, string Path, int numSlots, clsTranslation notes, IQ.NullableInt slotnum, int requiredFill, int advisedFill)
    {

        this.ID = ID;
        this.path = Path;
        this.Branch = Branch;
        this.Type = type;
        this.numSlots = numSlots;
        this.notes = notes;
        this.slotNum = slotnum;
        this.requiredFill = requiredFill;
        this.advisedFill = advisedFill;

        CurrentCompoundKey = this.compoundKey();

        if (Path.Length < 5 && Path != "")
        {
            Debugger.Break();
        }

        if (Type.ID <= 0)
        {
            Debugger.Break();
        }

        //Auto -DeDupe (see loadslots)
        if (Branch.i_Slots.ContainsKey(this.compoundKey()))
        {
            this.ID = -1;
        }
        else
        {
            //Note duplicated slots are NOT added to the branch/indexed
            Branch.i_Slots.Add(this.compoundKey(), this);
            Branch.slots.Add(this.ID, this);
        }

    }


    public void delete(ref List<string> errorMessages)
    {

        try
        {
            object sql = null;
            sql = "DELETE FROM slot where id=" + System.Convert.ToString(this.ID);
            da.DBExecutesql(sql);

            this.Branch.slots.Remove(this.ID);
            this.Branch.i_Slots.Remove(this.compoundKey());


        }
        catch
        {
            //delete = False  'failed (almost certainly due to RI) (although with slots specifically - it's hard to see how that would happen
            throw;
        }

    }

    public string displayName(clsLanguage Language)
    {

        return System.Convert.ToString(string.Format("maj:{0} mnr:{1} ns:{2} sn:{3} nts:{4} rqf:{5} adf:{6}", this.Type.MajorCode, this.Type.MinorCode, this.numSlots, this.slotNum == null ? "" : this.slotNum, this.notes == null ? "" : this.notes, this.requiredFill, this.advisedFill));

    }


}