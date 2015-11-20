using dataAccess;
using System.Xml;

public class clsSlot : i_Editable
{

	//Physical slots in a machine

	private int ID {
		get { return m_ID; }
		set { m_ID = Value; }
	}
	private int m_ID;
	private string path {
		get { return m_path; }
		set { m_path = Value; }
	}
	private string m_path;
	//OPTIONAL where (the context in which) this 'gives' works  - leave blank for it to work wherever it is grafted
	private clsBranch Branch {
		get { return m_Branch; }
		set { m_Branch = Value; }
	}
	private clsBranch m_Branch;
	//the branch (which may appear in many locations in the tree) to which these slots apply - single branch can have many of the same slot type with different paths (one for each of the positiosn it's grafted at)
	private clsSlotType Type {
		get { return m_Type; }
		set { m_Type = Value; }
	}
	private clsSlotType m_Type;

	private bool deleted {
		get { return m_deleted; }
		set { m_deleted = Value; }
	}
	private bool m_deleted;

	public clsSlotType NonStrictType {
		get {
			if (iq.StrictSlotValidation | Type.EnforceMinorCode) {
				return Type;
			} else {
				clsSlotType st;
				if (iq.i_slotType_Code(Type.MajorCode).ContainsKey("")) {
					st = iq.i_slotType_Code(Type.MajorCode)("");
				} else {
					st = new clsSlotType(Type.MajorCode, "") {
						Translation = Type.TranslationShort != null ? Type.TranslationShort : Type.Translation,
						TranslationShort = Type.TranslationShort
					};
				}
				//Dim st = iq.SlotTypes.Where(Function(slt) slt.Value.MajorCode = Type.MajorCode AndAlso slt.Value.MinorCode = If(Type.TranslationShort IsNot Nothing, Type.TranslationShort.Key.ToString, "")).FirstOrDefault

				//Return If(st.Value Is Nothing, , st.Value)
				return st;
			}
		}
	}

	private int numSlots {
		get { return m_numSlots; }
		set { m_numSlots = Value; }
	}
	private int m_numSlots;
	//slots given + / - taken (per item) 
	private clsTranslation notes {
		get { return m_notes; }
		set { m_notes = Value; }
	}
	private clsTranslation m_notes;
	private IQ.NullableInt slotNum {
		get { return m_slotNum; }
		set { m_slotNum = Value; }
	}
	private IQ.NullableInt m_slotNum;
	// for 'gives' slots you *can* specify the slot number 
	//                               (and must do so, if specifying more than one slot of the same type in the same product) 
	//                               numslots MUST be 1 if slotNum is specified
	//                               slotnum MUST be null for 'takes' slots (there is no functionality to specify that a particular card must go in a particular slot)

	private int requiredFill {
		get { return m_requiredFill; }
		set { m_requiredFill = Value; }
	}
	private int m_requiredFill;
	// the number of "given" slots (of this type) that *must* be filled - eg.. you MUST have a PSU in certain servers
	private int advisedFill {
		get { return m_advisedFill; }
		set { m_advisedFill = Value; }
	}
	private int m_advisedFill;


	public string CurrentCompoundKey;
	private clsSlot clone(newpath)
	{

		return new clsSlot(this.Type, this.Branch, newpath, this.numSlots, this.notes, this.slotNum, this.requiredFill, this.advisedFill);

	}

	public object writeXml(xmltextwriter W)
	{








			///slot


			//Return String.Format("<slot id='{0}' majorType='{1}' minorType='{2}' numSlots='{3}' notes='{4}' slotNum'{5}'/>" _
			//                              , .ID, _
			//                              xmlEncode(.Type.MajorCode), _
			//xmlEncode(.Type.MinorCode), _
			//                          .numSlots, _
			//                        If(.notes Is Nothing, "", xmlEncode(.notes.text(English))), _
			//                      .slotNum.sqlvalue)
		 // ERROR: Not supported in C#: WithStatement


	}


	private clsSlot(clsSlotType type, clsBranch Branch, string Path, int numslots, clsTranslation notes, IQ.NullableInt slotnum, int requiredfill, int advisedFill, DataTable writecache = null)
	{
		this.path = Path;
		this.Branch = Branch;
		this.Type = type;
		this.numSlots = numslots;
		this.notes = notes;
		this.slotNum = slotnum;
		this.requiredFill = requiredfill;
		this.advisedFill = advisedFill;


		if (type.ID <= 0)
			System.Diagnostics.Debugger.Break();

		//        If Val(slotnum.sqlvalue) > 100 Then Stop

		//A temproary (branchless) slot is created during import - just so we can constuct/access a compound key
		if (Branch != null && Branch.ID > 0) {

			object nk;
			if (notes == null)
				nk = "null";
			else
				nk = notes.Key;

			if (Branch.i_Slots == null)
				Branch.i_Slots = new Dictionary<string, clsSlot>();

			CurrentCompoundKey = this.compoundKey();
			if (Branch.slots == null)
				Branch.slots = new Dictionary<int, clsSlot>();

			if (writecache == null) {
				object sql;
				sql = "INSERT INTO [slot] (path,fk_branch_id,fk_slottype_id,numslots,slotnum,fk_translation_key_notes,requiredFill,advisedFill) ";
				sql += "VALUES (" + da.SqlEncode(Path) + "," + Branch.ID + "," + type.ID + "," + numslots + "," + slotnum.sqlvalue + "," + nk + "," + requiredfill + "," + advisedFill + ");";


				try {
					this.ID = da.DBExecutesql(sql, true);
				} catch {
					Beep();
				}


				Branch.slots.Add(this.ID, this);
				//    If Not Branch.i_Slots.ContainsKey(CurrentCompoundKey) Then
				Branch.i_Slots.Add(CurrentCompoundKey, this);
			//End If


			} else {
				if (Branch.i_Slots.ContainsKey(CurrentCompoundKey)) {
				// Beep()
				//no biggie (the same brachs is master into many FAMILIES
				//Logit("Duplicate branch slot key " & CurrentCompoundKey)

				} else {
					// Me.ID = -1
					System.Data.DataRow row;
					row = writecache.NewRow();
					row("path") = this.path;
					row("fk_branch_id") = this.Branch.ID;
					row("fk_slottype_id") = this.Type.ID;
					row("numslots") = this.numSlots;

					if (this.slotNum.sqlvalue == "null") {
						row("slotnum") = DBNull.Value;
					} else {
						row("slotnum") = this.slotNum.sqlvalue;
					}

					if (nk == "null") {
						row("fk_translation_key_notes") = DBNull.Value;
					} else {
						row("fk_translation_key_notes") = nk;
					}

					row("requiredFill") = requiredfill;
					row("advisedFill") = advisedFill;
					row("deleted") = false;
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

		HasSlotNum = false;

		if (this.slotNum != null) {
			if (this.slotNum.value != null) {
				if (!(object.ReferenceEquals(this.slotNum.value, DBNull.Value))) {
					HasSlotNum = true;
				}
			}
		}

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


	public object update(clsBranch newbranch, string newpath)
	{


		//this is a littel delicat e- we have to maintain the index of slits on the branch carefully.
		this.Branch.i_Slots.Remove(this.compoundKey);

		this.Branch = newbranch;
		this.path = newpath;

		object sql;
		sql = "UPDATE [slot] set ";
		sql += "path=" + da.SqlEncode(this.path) + ",fk_branch_id=" + this.Branch.ID;
		sql += " WHERE ID=" + this.ID;

		da.DBExecutesql(sql, false);

		this.CurrentCompoundKey = this.compoundKey;
		this.Branch.i_Slots.Add(this.CurrentCompoundKey, this);


	}


	public void i_Editable.Update(ref List<string> Errormessages)
	{

		try {
			if (this.Type.ID <= 0)
				System.Diagnostics.Debugger.Break();

			object sql;
			sql = "UPDATE [slot] set ";
			sql += "path=" + da.SqlEncode(this.path) + ",fk_branch_id=" + this.Branch.ID;
			sql += ",fk_slottype_id=" + this.Type.ID + ",numslots=" + this.numSlots;
			sql += ",requiredFill=" + this.requiredFill;
			sql += ",advisedfill=" + this.advisedFill;
			if (this.notes != null) {
				sql += ",fk_translation_key_notes=" + this.notes.Key;
			} else {
				sql += ",fk_translation_key_notes=null";
			}
			sql += ",deleted=" + IIf(this.deleted, 1, 0);


			sql += " WHERE ID=" + this.ID;

			da.DBExecutesql(sql, false);


			if (Branch.i_Slots.ContainsKey(CurrentCompoundKey)) {
				this.Branch.i_Slots.Remove(CurrentCompoundKey);
			}

			if (!this.deleted) {
				CurrentCompoundKey = compoundKey();
				this.Branch.i_Slots.Add(CurrentCompoundKey, this);
			}



		} catch (System.Exception ex) {
			Errormessages.Add(ex.Message);
		}

	}


	public clsSlot()
	{
		//needs to add it to the parent products distionary of slots

	}

	public object i_Editable.Insert(ref List<string> Errormessages)
	{
		if (this.Branch == null)
			this.Branch = iq.RootBranch;
		//temporary for editor, always gets updated...
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

		if (Len(Path) < 5 & Path != "")
			System.Diagnostics.Debugger.Break();

		if (type.ID <= 0)
			System.Diagnostics.Debugger.Break();

		//Auto -DeDupe (see loadslots)
		if (Branch.i_Slots.ContainsKey(this.compoundKey)) {
			this.ID = -1;
		} else {
			//Note duplicated slots are NOT added to the branch/indexed
			Branch.i_Slots.Add(this.compoundKey, this);
			Branch.slots.Add(this.ID, this);
		}

	}



	public void i_Editable.delete(ref List<string> errorMessages)
	{
		try {
			object sql;
			sql = "DELETE FROM slot where id=" + this.ID;
			da.DBExecutesql(sql);

			this.Branch.slots.Remove(this.ID);
			this.Branch.i_Slots.Remove(this.compoundKey);


		} catch {
			//delete = False  'failed (almost certainly due to RI) (although with slots specifically - it's hard to see how that would happen
			throw;
		}

	}

	public string i_Editable.displayName(clsLanguage Language)
	{

		return string.Format("maj:{0} mnr:{1} ns:{2} sn:{3} nts:{4} rqf:{5} adf:{6}", this.Type.MajorCode, this.Type.MinorCode, this.numSlots, IIf(this.slotNum == null, "", this.slotNum), IIf(this.notes == null, "", this.notes), this.requiredFill, this.advisedFill);

	}


}
