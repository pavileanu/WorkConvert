using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Threading;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Globalization;
using System.IO;




//Possible states for each column of each matrix 
public enum enumColState : int
{

	unused,
	//I don't like to use 0 in enums - in case something is unitialised
	HardCollapsed,
	//The user has actively collapsed this colum
	SoftCollapsed,
	//This column has been collapsed becuase there isn't enough space and an it's of low priority
	SoftExpanded,
	//This column is being displayed - becuase there's enough room
	HardExpanded
	//this column has been actively opened.. so we treat it as a higher priority
}

public enum enumParadigm
{

	errorNotSet = 0,
	AddingSystem = 1,
	configuringSystem = 2
}

//Quote/Basket item (system) viewtypes (sleected tabs)
public enum panelEnum
{
	System = 1,
	Options = 2,
	Spec = 4,
	Validation = 8,
	Promo = 16
}


public enum EnumHideButton
{
	Up,
	Down,
	Both,
	Neither
}


public enum ButtonsEnum
{

	Tabs = 1,
	Branches = 2,
	Squares = 4,
	Matrix = 8,
	close = 16,
	Auto = 32

}

public enum EnumOpenWhich
{
	errorNotset,
	None,
	First,
	All
}

public enum ud
{
	united,
	divided
}
//Public Enum oc
//    closed
//    open
//End Enum

public enum EnumValidationSeverity
{
	notSet,
	greenTick,
	BlueInfo,
	Question,
	Exclamation,
	DoesQualify,
	//cation
	DoesntQualify,
	//cation
	amberalert,
	RedCross,
	Upsell
}

public enum Manufacturer
{
	Unknown,
	HPI,
	HPE
}

public class CoreCode
{


	public int iicalls = 0;
	public enum enumBt
	{
		//Branch type
		errorNotSet,
		OpenSquare,
		// an open square renders nothing... except its children
		Square,
		Branch,
		Tab,
		gridrow,
		TROhead,
		TROitem,
		OpenBranch,
		Hidden,
		Upsell,
		hYperlink,
		DetailSquare,
		bighyperlinK,
		//branchForGrid 'A contrivance for showing options in grids, under branches - for tools/showOptions for system WITHDRAWN
		invisibLe,
		helpMechoose

	}


	//NOTE:-
	// "A Module statement defines a reference type available throughout its namespace. A module (sometimes called a standard module)is similar to a class but with some important distinctions.
	// Every module has exactly one instance and does not need to be created or assigned to a variable. Modules do not support inheritance or implement interfaces. Notice that a module is not a type in the sense that a class or structure is — you cannot declare a programming element to have the data type of a module. //
	// A module has the same lifetime as your program. Because its members are all Shared, they also have lifetimes equal to that of the program. "

	public List<string> BTchar = new string[] {
		"E",
		"C",
		"S",
		"B",
		"T",
		"G",
		"H",
		"I",
		"O",
		"X",
		"U",
		"Y",
		"D",
		"K",
		"L",
		"M"

	}.ToList;
		//ems was 1.6
	public float collapsedColumnWidth = 2.2;

	public clsIQ iq {
		//This IS the object model - ML if this is going to work, need to work out how to log, singleton seems the way to go though
		get { return clsIQ.Instance; }
	}

	public void reloadIQ()
	{
		clsIQ.reset();
		object a = iq.loadedTimestamp;
	}

		//You guessed it - Hewlett-Packard
	public clsChannel HP;
		//clsChannel       'The target channel for list pricing
	public clsPriceBand Everyone;

	public clsPriceBand HPList;
		//The Root of all regions (until we sell to Mars)
	public clsRegion r_worldwide;
		//The parent of otherwise unassigned countries - keeps level 1 tidy
	public clsRegion r_RestOfWorld;
	public clsRegion r_Americas;

	public clsRegion r_USCA;
	public clsRegion r_EMEA;
	public clsRegion r_GWE;
	public clsRegion r_UKIE;
		//formerly UK
	public clsRegion r_GB;
	public clsRegion r_IE;
	public clsRegion r_MEMA;

	public clsRegion r_CEE;
		//Like it says
	public clsLanguage English;
		//Primarily for recording failed logins
	public clsUser UnknownUser;
		//In truth there are many Roots - but for bootstrapping we need 1
	public clsBranch RootBranch;
	public clsBranch CarePackRootBranch;
		//Channels are loosely assembled into a tree - some channels are just placeholders - like 'Techdata'
	public clsChannel RootChannel;

		//used in translations
	public clsLanguage KYlanguage;
		//used when presenting grids - (saves an awful lot of XLT'ing) - thousands of cells for hundreds of users
	public clsTranslation Yes;
	public clsTranslation No;
	public clsTranslation InStock;
	public clsTranslation OutOfStock;

	public ServiceHost StockWebservice;

	public string NameOf(string mfrsku)
	{

		clsProduct product;
		if (iq.i_SKU.ContainsKey(mfrsku)) {
			product = iq.i_SKU(mfrsku);

			if (product.i_Attributes_Code.ContainsKey("Name")) {
				return product.i_Attributes_Code("Name")(0).Translation.text(s_lang);
			} else {
				return product.i_Attributes_Code("desc")(0).Translation.text(s_lang);
			}
		} else {
			return "Not a valid SKU";
		}

	}

	//Public Function UpdatePrices(buyerAccount As clsAccount) As String

	//    Static LastTimes As Dictionary(Of String, DateTime)
	//    If LastTimes Is Nothing Then LastTimes = New Dictionary(Of String, DateTime)

	//    Dim ck$
	//    ck$ = buyerAccount.SellerChannel.ID & "^" & buyerAccount.priceBand

	//    If Not LastTimes.ContainsKey(ck$) Then LastTimes.Add(ck$, DateAdd(DateInterval.Day, -1000, Now))

	//    If DateDiff(DateInterval.Minute, LastTimes(ck$), Now) > 30 Then
	//        Return Import.HostPrices(buyerAccount.SellerChannel.Code, buyerAccount.priceBand)
	//    Else
	//        Return "Cached within the last 30 minutes"
	//    End If

	//End Function

	public string BranchID(clsBranch b)
	{

		if (b == null) {
			return "0";
		} else {
			return Trim(b.ID);
		}

	}


	public HyperLink MakeLinkButton(imagefile, string tooltip, Url, clsLanguage language)
	{

		HyperLink btn = new HyperLink();
		btn.ToolTip = Xlt(tooltip, language);
		btn.ImageUrl = "/images/navigation/" + imagefile;
		btn.NavigateUrl = Url;
		btn.Attributes("target") = "_blank";

		return btn;

	}
	public Literal MakeRoundButton(imagefile, string tooltip, string clickscript, string cssClass, string style, clsLanguage language, string objectId = "")
	{
		//, Optional AbsX As Single = -1, Optional pos As String = "absolute") As Literal

		Literal lit = new Literal();
		lit.Text = Replace("<img id=|" + objectId + "| class=|unpressedButton " + cssClass + "| src=|/images/navigation/" + imagefile + "| onclick=|" + clickscript + "| title=|" + Xlt(tooltip, language) + "| style=|" + style + "|/>", "|", Chr(34));

		return lit;

	}

	public void wipeCachedDataView(path, UInt64 lid)
	{
		object key, filters, sorts;

		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");

		//        filters = iq.sesh(lid,"filters." & path$)
		Dictionary<clsField, Dictionary<clsFilter, string>> activeFilters = iq.sesh(lid, "filters." + path);
		filters = textRep(activeFilters);

		sorts = iq.sesh(lid, "sorts." + path);

		key = path + "-" + filters + "-" + sorts + "-" + buyerAccount.BuyerChannel.Code + "-" + buyerAccount.SellerChannel.Code + "-branches";

		System.Web.Caching.Cache cache = System.Web.HttpContext.Current.Cache;
		cache.Remove(key);

	}



	public clsScreen Effectivematrix(path)
	{

		Effectivematrix = null;

		string[] seg;
		seg = Split(path, ".");

		for (i = UBound(seg); i >= 1; i += -1) {
			if (iq.Branches(seg(i)).Matrix != null) {
				return iq.Branches(seg(i)).Matrix;
			}
		}

		if (Effectivematrix == null) {
			return iq.Screens(719);
		}


	}

	public void SaveUserStates()
	{
		object li = new List<clsUserState>();
		if (iq.seshDic == null)
			return;

		foreach ( s in iq.seshDic) {
			clsUserState uState = new clsUserState();



			 // ERROR: Not supported in C#: WithStatement


			uState.mopUpvalues = new List<KeyValuePair<object, object>>();
			//makes the dictionary keys case insensitive))
			foreach ( kvp in s.Value) {
				if (kvp.Value == null || kvp.Value.GetType().IsPrimitive) {
					uState.mopUpvalues.Add(new KeyValuePair<object, object> {
						Key = kvp.Key,
						Value = kvp.Value
					});
				}
			}

			li.Add(uState);

		}

		XmlSerializer b = new XmlSerializer(li.GetType());
		string st;
		using (MemoryStream mem = new MemoryStream()) {
			using (TextReader t = new StreamReader(mem)) {
				b.Serialize(mem, li);
				mem.Seek(0, SeekOrigin.Begin);
				st = t.ReadToEnd();

				dataAccess.da.DBExecutesql("INSERT INTO UserStates (datetime,hostname,states) VALUES (GetDate(),'" + Environment.MachineName + "','" + st + "')");

			}
		}
	}
	public struct KeyValuePair<K, V>
	{
		public K Key {
			get { return m_Key; }
			set { m_Key = Value; }
		}
		private K m_Key;
		public V Value {
			get { return m_Value; }
			set { m_Value = Value; }
		}
		private V m_Value;
	}

	public bool UserIsAdmin(UInt64 lid)
	{

		return ((clsAccount)iq.sesh(lid, "BuyerAccount")).HasRight("GLOBALADM");
	}
	public bool AccountHasRight(UInt64 lid, string rightcode)
	{
		if (iq.SeshAlive(lid) && iq.seshDic(lid).ContainsKey("Elevated"))
			return true;

		object ba = (clsAccount)iq.sesh(lid, "BuyerAccount");
		if (ba != null)
			return ba.HasRight(rightcode);
		else
			return false;
	}




	public enum enumValidationMessageType
	{
		Validation,
		Flex,
		Upsell,
		Specification,
		UpsellHolder
	}

	public class clsActionList
	{


		private int ID = 0;
		private List<clsAction> _actions = new List<clsAction>();
		public void Add(string Sku, ObjectType ObjectType, string Message)
		{
			ID = ID + 1;
			_actions.Add(new clsAction {
				ID = ID,
				ObjectType = ObjectType,
				SKU = Sku,
				Message = Message
			});
		}
		public void Add(string Sku, ActionType ActionType, ObjectType ObjectType, clsProductAttribute From, clsProductAttribute AttributTo)
		{
			ID = ID + 1;
			_actions.Add(new clsAction {
				ID = ID,
				ActionType = ActionType,
				SKU = Sku,
				AttributeFrom = From,
				ObjectType = ObjectType,
				AttributeTo = AttributTo
			});
		}
		public void Add(string Sku, ActionType ActionType, ObjectType ObjectType, clsBranch Branch)
		{
			ID = ID + 1;
			_actions.Add(new clsAction {
				ID = ID,
				ActionType = ActionType,
				SKU = Sku,
				ObjectType = ObjectType,
				SourceBranch = Branch
			});
		}
		public void Add(string Sku, ActionType ActionType, ObjectType ObjectType, clsBranch Branch, clsBranch TargetBranch)
		{
			ID = ID + 1;
			_actions.Add(new clsAction {
				ID = ID,
				ActionType = ActionType,
				SKU = Sku,
				ObjectType = ObjectType,
				SourceBranch = Branch,
				TargetBranch = TargetBranch
			});
		}
		public void Add(string Sku, ActionType ActionType, ObjectType ObjectType, clsBranch Branch, string TargetBranchName)
		{
			ID = ID + 1;
			_actions.Add(new clsAction {
				ID = ID,
				ActionType = ActionType,
				SKU = Sku,
				ObjectType = ObjectType,
				SourceBranch = Branch,
				TargetBranchName = TargetBranchName
			});
		}
		public void Add(string Sku, string SysSku, ActionType ActionType, ObjectType ObjectType, string quantityDetails)
		{
			ID = ID + 1;
			_actions.Add(new clsAction {
				ID = ID,
				ActionType = ActionType,
				SKU = Sku,
				ObjectType = ObjectType,
				SysSKU = SysSku,
				QuantityDetails = quantityDetails
			});
		}
		public void Add(string Sku, ActionType ActionType, ObjectType ObjectType, clsBranch TargetBranch, clsSlotType slottype, string path, int quantity)
		{
			ID = ID + 1;
			_actions.Add(new clsAction {
				ID = ID,
				ActionType = ActionType,
				SKU = Sku,
				ObjectType = ObjectType,
				Quantity = quantity,
				SlotType = slottype,
				Path = path,
				TargetBranch = TargetBranch
			});
		}

		public bool IsGo(string Sku, ActionType ActionType, ObjectType ObjectType, clsBranch TargetBranch, clsSlotType slottype, string path, int quantity)
		{
			return _actions.Where(ac => ac.ActionType == ActionType && ac.SKU == Sku && ac.ObjectType == ObjectType && ac.Quantity == quantity && object.ReferenceEquals(ac.SlotType, slottype) && ac.Path == path && ac.Authorized & object.ReferenceEquals(ac.TargetBranch, TargetBranch)).Count > 0;
		}
		public bool IsGo(string Sku, string SysSku, ActionType ActionType, ObjectType ObjectType, string quantityDetails)
		{
			return _actions.Where(ac => ac.ActionType == ActionType && ac.SKU == Sku && ac.ObjectType == ObjectType && ac.QuantityDetails == quantityDetails && ac.SysSKU == SysSku && ac.Authorized).Count > 0;
		}
		public bool IsGo(string Sku, ActionType ActionType, ObjectType ObjectType, clsBranch Branch, string TargetBranchName)
		{
			return _actions.Where(ac => ac.ActionType == ActionType && ac.SKU == Sku && ac.ObjectType == ObjectType && object.ReferenceEquals(ac.SourceBranch, Branch) && ac.TargetBranchName == TargetBranchName && ac.Authorized).Count > 0;
		}
		public bool IsGo(string Sku, ActionType ActionType, ObjectType ObjectType, clsBranch Branch, clsBranch TargetBranch)
		{
			return _actions.Where(ac => ac.SKU == Sku && ac.ActionType == ActionType && ac.ObjectType == ObjectType && object.ReferenceEquals(ac.SourceBranch, Branch) && object.ReferenceEquals(ac.TargetBranch, TargetBranch) && ac.Authorized).Count > 0;
		}
		public bool IsGo(string Sku, ActionType ActionType, ObjectType ObjectType, clsProductAttribute From, clsProductAttribute AttributeTo)
		{
			return _actions.Where(ac => ac.ActionType == ActionType && ac.SKU == Sku && object.ReferenceEquals(ac.AttributeFrom, From) && ac.ObjectType == ObjectType && object.ReferenceEquals(ac.AttributeTo, AttributeTo) && ac.Authorized).Count > 0;
		}

		public List<clsAction> ToList()
		{
			return _actions;
		}

		public object ToClientList()
		{
			return _actions.Select(ac =>
			{
				switch (ac.ObjectType) {
					case ObjectType.Attribute:
						return new {
							ID = ac.ID,
							SKU = ac.SKU,
							Authorized = false,
							ObjectType = ac.ObjectType.ToString(),
							Type = ac.ActionType.ToString(),
							Col1 = ac.AttributeFrom == null ? ac.AttributeTo : ac.AttributeFrom.Attribute.displayName(English),
							Col2 = ac.AttributeFrom != null ? ac.AttributeFrom.Translation != null ? ac.AttributeFrom.Translation.text(English) : ac.AttributeFrom.NumericValue.ToString : "None",
							Col3 = ac.AttributeTo != null ? ac.AttributeTo.Translation != null ? ac.AttributeTo.Translation.text(English) : ac.AttributeTo.NumericValue.ToString : "None"
						};
					case ObjectType.Branch | ObjectType.Graft | ObjectType.Prune:
						return new {
							ID = ac.ID,
							SKU = ac.SKU,
							Authorized = false,
							ObjectType = ac.ObjectType.ToString(),
							Type = ac.ActionType.ToString(),
							Col1 = ac.SourceBranch.Translation.text(English),
							Col2 = ac.TargetBranch != null ? ac.TargetBranch.Translation.text(English) : ac.TargetBranchName
						};
					case ObjectType.Quantity:
						return new {
							ID = ac.ID,
							SKU = ac.SKU,
							Authorized = false,
							ObjectType = ac.ObjectType.ToString(),
							Type = ac.ActionType.ToString(),
							Col1 = ac.SysSKU,
							Col2 = ac.Path,
							Col3 = ac.QuantityDetails
						};
					case ObjectType.Slot:
						return new {
							ID = ac.ID,
							SKU = ac.SKU,
							Authorized = false,
							ObjectType = ac.ObjectType.ToString(),
							Type = ac.ActionType.ToString(),
							Col1 = ac.TargetBranch.Translation.text(English),
							Col2 = ac.Path,
							Col3 = ac.SlotType.MajorCode + ":" + ac.SlotType.MinorCode,
							Col4 = ac.Quantity.ToString
						};
					case ObjectType.WARNING:
						return new {
							ID = ac.ID,
							SKU = ac.SKU,
							ObjectType = ac.ObjectType,
							Col1 = ac.Message
						};
					default:
						return new {
							ID = ac.ID,
							SKU = ac.SKU,
							Authorized = false,
							ObjectType = ac.ObjectType.ToString(),
							Type = ac.ActionType.ToString()
						};
				}
			});
		}

	}
	public class clsAction
	{
		public int ID {
			get { return m_ID; }
			set { m_ID = Value; }
		}
		private int m_ID;
		public ActionType ActionType {
			get { return m_ActionType; }
			set { m_ActionType = Value; }
		}
		private ActionType m_ActionType;
		public string SKU {
			get { return m_SKU; }
			set { m_SKU = Value; }
		}
		private string m_SKU;
		public string SysSKU {
			get { return m_SysSKU; }
			set { m_SysSKU = Value; }
		}
		private string m_SysSKU;
		public ObjectType ObjectType {
			get { return m_ObjectType; }
			set { m_ObjectType = Value; }
		}
		private ObjectType m_ObjectType;
		public clsProductAttribute AttributeFrom {
			get { return m_AttributeFrom; }
			set { m_AttributeFrom = Value; }
		}
		private clsProductAttribute m_AttributeFrom;
		public clsProductAttribute AttributeTo {
			get { return m_AttributeTo; }
			set { m_AttributeTo = Value; }
		}
		private clsProductAttribute m_AttributeTo;
		public clsBranch SourceBranch {
			get { return m_SourceBranch; }
			set { m_SourceBranch = Value; }
		}
		private clsBranch m_SourceBranch;
		public clsBranch TargetBranch {
			get { return m_TargetBranch; }
			set { m_TargetBranch = Value; }
		}
		private clsBranch m_TargetBranch;
		public string TargetBranchName {
			get { return m_TargetBranchName; }
			set { m_TargetBranchName = Value; }
		}
		private string m_TargetBranchName;
		public string QuantityDetails {
			get { return m_QuantityDetails; }
			set { m_QuantityDetails = Value; }
		}
		private string m_QuantityDetails;
		public clsSlotType SlotType {
			get { return m_SlotType; }
			set { m_SlotType = Value; }
		}
		private clsSlotType m_SlotType;
		public int Quantity {
			get { return m_Quantity; }
			set { m_Quantity = Value; }
		}
		private int m_Quantity;
		public string Message {
			get { return m_Message; }
			set { m_Message = Value; }
		}
		private string m_Message;
		public string Path {
			get { return m_Path; }
			set { m_Path = Value; }
		}
		private string m_Path;
		public bool Authorized {
			get { return m_Authorized; }
			set { m_Authorized = Value; }
		}
		private bool m_Authorized;
	}
	public enum ObjectType
	{
		[DataMember()]
		,
		Attribute,
		[DataMember()]
		,
		Branch,
		[DataMember()]
		,
		Graft,
		[DataMember()]
		,
		Prune,
		[DataMember()]
		,
		Quantity,
		[DataMember()]
		,
		Slot,
		[DataMember()]
		,
		WARNING
	}
	public enum ActionType
	{
		[DataMember()]
		,
		INSERT,
		[DataMember()]
		,
		UPDATE,
		[DataMember()]
		,
		DELETE,
		[DataMember()]
		,
		NONE

	}

	public class clsImportRow
	{
		private DateTime DateTime {
			get { return m_DateTime; }
			set { m_DateTime = Value; }
		}
		private DateTime m_DateTime;
		private string Message {
			get { return m_Message; }
			set { m_Message = Value; }
		}
		private string m_Message;
		private Int32 Id {
			get { return m_Id; }
			set { m_Id = Value; }
		}
		private Int32 m_Id;
	}
	public class clsImportLog
	{
		public static Int32 nextId = 0;
		public Dictionary<int, clsImportRow> data = new Dictionary<int, clsImportRow>();
		public void Add(DateTime DateTime, string Message)
		{
			data.Add(nextId, new clsImportRow {
				DateTime = DateTime,
				Message = Message,
				Id = nextId
			});
			nextId = nextId + 1;
		}
		public void clear()
		{
			data.Clear();
		}
	}
}



