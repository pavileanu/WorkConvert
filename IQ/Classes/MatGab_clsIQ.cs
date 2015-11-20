//Option Strict On
using dataAccess;
using System.Data.SqlClient;
using System.Threading;
using System.Xml.Serialization;
using System.IO;

//A single 'master' instace of the IQ class is instantiated, and holds the entire 'object model'.
//All IIS users access this (one) underlying, memory based copy of all of product data, quotes, translations, channels, accounts etc.
//all of which are held in dictionaries - typically accessed by an integer key which - correspods to their primary key field (ID).. in the database.
//Note:- their Key is NOT their ordinal - ie.Product(5) is NOT the 5th product.. it's the product with the key '5' - (which could be the 167th product)
//Some of the dictionaries are sorted - so *sometimes* the ordinals will match the keys - but it can *never* be relied upon
//It's vital to understand the difference between product(5) and product.values(5) - and to never confuse the two

public class clsIQ
{

	private static clsIQ _instance;
	private static readonly object _LockObj = new object();
	public static List<string> errorMessages = new List<string>();

	public static LoggingList<string> messages = new LoggingList<string>();
		//
	public Dictionary<int, clsProduct> REMAPS = new Dictionary<int, clsProduct>();

	public static clsIQ Instance {
		get {
			lock (_LockObj) {
				if (_instance == null || (!IsLoading & !IsLoaded)) {
					IsLoading = true;
					//AuditLog.Instance.Add()
					_instance = new clsIQ();
					Thread d = new Thread(new ParameterizedThreadStart(_instance.load));

					d.Start(errorMessages);
				}

			}

			return _instance;
		}
	}

	private static long? StartBytes;
	private static long? EndBytes;
	public static void reset()
	{
		StartBytes = System.GC.GetTotalMemory(true);
		lock (_LockObj) {
			_instance = null;
			IsLoaded = false;
		}

	}

	public static bool IsLoading = false;

	public static bool IsLoaded = false;
		//used for the abandonment of keyword searches 'mid flight' - 
	public int nextSearchID;

	// Public PathCache As clsProductCache
	//the sets are shared amongst users, with the smallest  one being contiually replaced

	public clsVariant AllVariants;
		// as simple counter for unique ID's on info (blue circle) divs - may get quite large 
	public int infoID;
	//Public NextKey As Integer
	private Dictionary<int, clsLanguage> Languages {
		get { return m_Languages; }
		set { m_Languages = Value; }
	}
	private Dictionary<int, clsLanguage> m_Languages;
	private Dictionary<int, clsLanguage> ActiveLanguages {
		get { return m_ActiveLanguages; }
		set { m_ActiveLanguages = Value; }
	}
	private Dictionary<int, clsLanguage> m_ActiveLanguages;
	private Dictionary<int, clsSlotType> SlotTypes {
		get { return m_SlotTypes; }
		set { m_SlotTypes = Value; }
	}
	private Dictionary<int, clsSlotType> m_SlotTypes;

	//This is a dictionary of dictionaries
	//                                                 KEY  (NOT ID!) >  Translation
	public Dictionary<int, clsTranslation> Translations {
		get { return m_Translations; }
		set { m_Translations = Value; }
	}
	private Dictionary<int, clsTranslation> m_Translations;
	public Dictionary<int, clsProduct> Products {
		get { return m_Products; }
		set { m_Products = Value; }
	}
	private Dictionary<int, clsProduct> m_Products;
	public Dictionary<int, clsstock> Stock {
		get { return m_Stock; }
		set { m_Stock = Value; }
	}
	private Dictionary<int, clsstock> m_Stock;
	//would be nice to get rid of this (accessible via the product - stock now belongs to variants)

	private clsBranch RootBranch {
		get { return m_RootBranch; }
		set { m_RootBranch = Value; }
	}
	private clsBranch m_RootBranch;
	//THE root node of the product tree... *every* product tree is attached to here - but it never exists in the database - it has an ID of 0
	private clsBranch RootCPQBranch {
		get { return m_RootCPQBranch; }
		set { m_RootCPQBranch = Value; }
	}
	private clsBranch m_RootCPQBranch;
	//THE root node of all carepacks

	//Note, events load their children from the database 'just in time' - only root level events (those whose parents are themselves) are initially loaded (see iq.LoadEvents).. accessing the children property fetches them from the database.
	//events are *written* real time - into the object model only - and persisted (to the databse) at key points via a events PersistRecursive() metho
	//This allows very large numbers of events to be recorded at high speed (via bulk writes) with minimal memory footprint.

	private clsThread RootThread {
		get { return m_RootThread; }
		set { m_RootThread = Value; }
	}
	private clsThread m_RootThread;
	private clsChannel RootChannel {
		get { return m_RootChannel; }
		set { m_RootChannel = Value; }
	}
	private clsChannel m_RootChannel;
	//The channel hierarchy is not a rigid reflection of the 'real life' supply chain - it's just for grouping and presentation withing the editor (and potentially reporting) 
	//The actual relationships between channels are defined by the margins/prices - each linking a buyer and a seller  in a many:many relationship (the same reseller may buy from several distributors)

	//Property StandardVariant As clsVariant
	private Dictionary<int, clsBranch> Branches {
		get { return m_Branches; }
		set { m_Branches = Value; }
	}
	private Dictionary<int, clsBranch> m_Branches;
	private Dictionary<int, clsQuantity> Quantities {
		get { return m_Quantities; }
		set { m_Quantities = Value; }
	}
	private Dictionary<int, clsQuantity> m_Quantities;

	private Dictionary<string, clsBranch> i_SpecialBranches {
		get { return m_i_SpecialBranches; }
		set { m_i_SpecialBranches = Value; }
	}
	private Dictionary<string, clsBranch> m_i_SpecialBranches;

	//Grafts provide cross linking in the tree - allowing a branch to be reused in may places
	//becasue a single branch (eg. a system unit) may have many branches grafted onto it (eg, Drive bays, memory slots, PCI slots)
	//we must have ready access to a list of grafts onto a particular branch
	//the integer index is the 'target' portion of the graft - this item contains a list of all the grafts onto it
	//property Grafts As SortedDictionary(Of clsBranch, List(Of clsGraft))
	private Dictionary<int, clsAttribute> Attributes {
		get { return m_Attributes; }
		set { m_Attributes = Value; }
	}
	private Dictionary<int, clsAttribute> m_Attributes;
	//the 'master' list of all (types of) attribute

	//iEnglishIndex uses a compund key internally - and iexposed via a public function  (forcing a group to be specified)
	private Dictionary<string, clsTranslation> iEnglishIndex {
		get { return m_iEnglishIndex; }
		set { m_iEnglishIndex = Value; }
	}
	private Dictionary<string, clsTranslation> m_iEnglishIndex;
	//Watch out !... the integer part of this is the translation.KEY
	private Dictionary<string, clsTranslation> KYIndex {
		get { return m_KYIndex; }
		set { m_KYIndex = Value; }
	}
	private Dictionary<string, clsTranslation> m_KYIndex;
	//Watch out !... the integer part of this is the translation.KEY
	private Dictionary<int, clsUnit> Units {
		get { return m_Units; }
		set { m_Units = Value; }
	}
	private Dictionary<int, clsUnit> m_Units;
	//master list of units - keyed by our internal short code eg KG, MM, M, LBS, BTU
	private Dictionary<int, clsChannel> Channels {
		get { return m_Channels; }
		set { m_Channels = Value; }
	}
	private Dictionary<int, clsChannel> m_Channels;
	//Each channel has a set of users, each user has a set of quotes
	private Dictionary<int, clsBuyerGroup> BuyerGroups {
		get { return m_BuyerGroups; }
		set { m_BuyerGroups = Value; }
	}
	private Dictionary<int, clsBuyerGroup> m_BuyerGroups;
	private Dictionary<int, clsTeam> Teams {
		get { return m_Teams; }
		set { m_Teams = Value; }
	}
	private Dictionary<int, clsTeam> m_Teams;
	private Dictionary<int, clsUser> Users {
		get { return m_Users; }
		set { m_Users = Value; }
	}
	private Dictionary<int, clsUser> m_Users;
	private Dictionary<int, clsProductType> ProductTypes {
		get { return m_ProductTypes; }
		set { m_ProductTypes = Value; }
	}
	private Dictionary<int, clsProductType> m_ProductTypes;

	//Did spend a lot of time considering this as channel.customeraccounts - but it makes operations on the whole list harder
	private Dictionary<int, clsAccount> Accounts {
		get { return m_Accounts; }
		set { m_Accounts = Value; }
	}
	private Dictionary<int, clsAccount> m_Accounts;
	//Each user has an account with one (or more) distributors(channels)
	//Quotes live under the agentaccounts - property Quotes As SortedDictionary(Of Integer, clsquote) 'Each channel has a set of users, each user has a set of quote
	private Dictionary<int, clsCurrency> Currencies {
		get { return m_Currencies; }
		set { m_Currencies = Value; }
	}
	private Dictionary<int, clsCurrency> m_Currencies;
	private Dictionary<int, clsCulture> Cultures {
		get { return m_Cultures; }
		set { m_Cultures = Value; }
	}
	private Dictionary<int, clsCulture> m_Cultures;
	// Property Countries As Dictionary(Of Integer, clsCountry) - replaced by regions which are more generalised and heirarchical - 
	private Dictionary<int, clsState> States {
		get { return m_States; }
		set { m_States = Value; }
	}
	private Dictionary<int, clsState> m_States;
	//property Quantities As Dictionary(Of Integer, clsQuantity) 'Quantities apply to specific nodes in the tree (via their paths)
	//property Prices As Dictionary(Of Integer, clsPrice)
	//Property Prunes As Dictionary(Of String, clsPrune)
	//Property Prunes As Dictionary(Of Integer, clsPrune)
	private Dictionary<int, clsSector> Sectors {
		get { return m_Sectors; }
		set { m_Sectors = Value; }
	}
	private Dictionary<int, clsSector> m_Sectors;
	//Property Events As Dictionary(Of Integer, clsEvent)
	private Dictionary<int, clsQuote> Quotes {
		get { return m_Quotes; }
		set { m_Quotes = Value; }
	}
	private Dictionary<int, clsQuote> m_Quotes;
	// Quotes are *not* populated at startup (as there are an awful lot of them) - however we need a root level dictionary to enable viewing/editing via the OM
	private Dictionary<int, clsThread> Threads {
		get { return m_Threads; }
		set { m_Threads = Value; }
	}
	private Dictionary<int, clsThread> m_Threads;
	private Dictionary<int, clsRegion> Regions {
		get { return m_Regions; }
		set { m_Regions = Value; }
	}
	private Dictionary<int, clsRegion> m_Regions;
	private Dictionary<int, ClsAvalancheOPG> AvalancheOPGs {
		get { return m_AvalancheOPGs; }
		set { m_AvalancheOPGs = Value; }
	}
	private Dictionary<int, ClsAvalancheOPG> m_AvalancheOPGs;
	private Dictionary<int, clsFlexOPG> FlexOPGs {
		get { return m_FlexOPGs; }
		set { m_FlexOPGs = Value; }
	}
	private Dictionary<int, clsFlexOPG> m_FlexOPGs;
	private Dictionary<int, clsBundle> Bundles {
		get { return m_Bundles; }
		set { m_Bundles = Value; }
	}
	private Dictionary<int, clsBundle> m_Bundles;
	private Dictionary<int, clsExclude> Excludes {
		get { return m_Excludes; }
		set { m_Excludes = Value; }
	}
	private Dictionary<int, clsExclude> m_Excludes;
	//having any member of key list exclude all members of the values list
	private Dictionary<string, List<clsMessage>> UserMessages {
		get { return m_UserMessages; }
		set { m_UserMessages = Value; }
	}
	private Dictionary<string, List<clsMessage>> m_UserMessages;
	private Dictionary<string, List<clsROKAttribute>> ROKAttributes {
		get { return m_ROKAttributes; }
		set { m_ROKAttributes = Value; }
	}
	private Dictionary<string, List<clsROKAttribute>> m_ROKAttributes;
	private Dictionary<string, clsAddress> Addresses {
		get { return m_Addresses; }
		set { m_Addresses = Value; }
	}
	private Dictionary<string, clsAddress> m_Addresses;
	private Dictionary<string, clsLegal> Legal {
		get { return m_Legal; }
		set { m_Legal = Value; }
	}
	private Dictionary<string, clsLegal> m_Legal;
	private Dictionary<int, clsResourceCategory> ResourceCategories {
		get { return m_ResourceCategories; }
		set { m_ResourceCategories = Value; }
	}
	private Dictionary<int, clsResourceCategory> m_ResourceCategories;

	// HP care pack service levels
	private Dictionary<int, clsServiceLevel> ServiceLevels {
		get { return m_ServiceLevels; }
		set { m_ServiceLevels = Value; }
	}
	private Dictionary<int, clsServiceLevel> m_ServiceLevels;
	private Dictionary<int, clsResponse> ServiceLevelResponse {
		get { return m_ServiceLevelResponse; }
		set { m_ServiceLevelResponse = Value; }
	}
	private Dictionary<int, clsResponse> m_ServiceLevelResponse;
	private Dictionary<int, clsServiceType> ServiceLevelServiceType {
		get { return m_ServiceLevelServiceType; }
		set { m_ServiceLevelServiceType = Value; }
	}
	private Dictionary<int, clsServiceType> m_ServiceLevelServiceType;
	private Dictionary<int, clsTROAA> ServiceLevelTROAA {
		get { return m_ServiceLevelTROAA; }
		set { m_ServiceLevelTROAA = Value; }
	}
	private Dictionary<int, clsTROAA> m_ServiceLevelTROAA;
	private Dictionary<string, clsAttribute> ServiceLevelAttributeMap {
		get { return m_ServiceLevelAttributeMap; }
		set { m_ServiceLevelAttributeMap = Value; }
	}
	private Dictionary<string, clsAttribute> m_ServiceLevelAttributeMap;
	private IDictionary<string, DateTime> CarePackLastRefresh {
		get { return m_CarePackLastRefresh; }
		set { m_CarePackLastRefresh = Value; }
	}
	private IDictionary<string, DateTime> m_CarePackLastRefresh;

	//ultimately these are references - so not a expensive as they might look (around 12 bytes per row)
	private Dictionary<int, clsPrice> Prices {
		get { return m_Prices; }
		set { m_Prices = Value; }
	}
	private Dictionary<int, clsPrice> m_Prices;
	//I've been all 'round the houses with this - (where to 'store' prices) - as dictionaries within the products etc,etc - but at the end of the day it makes most sense to have a root level 'master' list - most becuase the lookups are Olog(n)
	private Dictionary<int, clsVariant> Variants {
		get { return m_Variants; }
		set { m_Variants = Value; }
	}
	private Dictionary<int, clsVariant> m_Variants;


	private Dictionary<int, clsCampaign> Campaigns {
		get { return m_Campaigns; }
		set { m_Campaigns = Value; }
	}
	private Dictionary<int, clsCampaign> m_Campaigns;
	private Dictionary<int, clsAdvert> Adverts {
		get { return m_Adverts; }
		set { m_Adverts = Value; }
	}
	private Dictionary<int, clsAdvert> m_Adverts;
	private List<clsScreenOverride> ScreenOverrides {
		get { return m_ScreenOverrides; }
		set { m_ScreenOverrides = Value; }
	}
	private List<clsScreenOverride> m_ScreenOverrides;
	private Dictionary<int, clsPromo> Promos {
		get { return m_Promos; }
		set { m_Promos = Value; }
	}
	private Dictionary<int, clsPromo> m_Promos;
	public Dictionary<clsRegion, List<clsPromo>> i_PromoRegions;

	public Dictionary<clsPromo, List<string>> i_PromoSystemTypes;
	private Dictionary<int, Dictionary<int, double>> Conversions {
		get { return m_Conversions; }
		set { m_Conversions = Value; }
	}
	private Dictionary<int, Dictionary<int, double>> m_Conversions;
	private Dictionary<int, string> Measures {
		get { return m_Measures; }
		set { m_Measures = Value; }
	}
	private Dictionary<int, string> m_Measures;
	private Dictionary<string, string> ActiveUniversalCountries {
		get { return m_ActiveUniversalCountries; }
		set { m_ActiveUniversalCountries = Value; }
	}
	private Dictionary<string, string> m_ActiveUniversalCountries;
	private Dictionary<int, clsValidationInclusion> ValidationInclusions {
		get { return m_ValidationInclusions; }
		set { m_ValidationInclusions = Value; }
	}
	private Dictionary<int, clsValidationInclusion> m_ValidationInclusions;
	private Dictionary<string, string> Locations {
		get { return m_Locations; }
		set { m_Locations = Value; }
	}
	private Dictionary<string, string> m_Locations;


	public Dictionary<int, clsFilter> Filters;
	private Dictionary<int, clsCurrency> DefaultCurrencies {
		get { return m_DefaultCurrencies; }
		set { m_DefaultCurrencies = Value; }
	}
	private Dictionary<int, clsCurrency> m_DefaultCurrencies;

	//These i_ dictionaries are effectively indexes for various columns in the object model - In obscure places, or where speed isn't critical, we use LINQ - but these
	//lookup tables will be much faster - as a general principle these 'indexes' are updated when the objects they index are created (ie., in their New() constructors
		// allows state (messages and ID's) to be looked up via their codes
	public Dictionary<string, clsState> i_state_GroupCode;
		//index of username to user objects (as there are several thousand)
	public Dictionary<string, clsUser> i_user_email;
	public Dictionary<string, clsChannel> i_channel_code;
		//used primarly by the webservice to look up products by SKU
	public Dictionary<string, clsProduct> i_SKU;

	//Public i_buyerGroups As Dictionary(Of String, clsBuyerGroup) 'used by ther webservice to look up buyer groups (fast!) by the distis own reference (eg.. buyerGroup.ownerID)
	//Public i_variant_code As Dictionary(Of String, clsVariant)
	public Dictionary<string, clsCurrency> i_currency_code;
	public Dictionary<string, clsCulture> i_culture_code;
	//Public i_country_code As Dictionary(Of String, clsCountry) - countries have been superseded by regions

	public Dictionary<string, clsRegion> i_region_code;
	public Dictionary<string, clsLanguage> i_language_Code;
	public Dictionary<string, clsProductType> i_ProductType_Code;
	public Dictionary<string, clsRole> i_role_Code;
	public Dictionary<string, clsRight> i_right_Code;
	public Dictionary<string, clsPriceBand> priceBands {
		get { return m_priceBands; }
		set { m_priceBands = Value; }
	}
	private Dictionary<string, clsPriceBand> m_priceBands;


	public Dictionary<int, clsRole> Roles {
		get { return i_role_Code.Values.ToDictionary(rc => rc.ID, rc => rc); }
		set { i_role_Code = value.Values.ToDictionary(irc => irc.Code, irc => irc); }
	}
	public Dictionary<int, clsRight> Rights {
		get { return i_right_Code.Values.ToDictionary(rc => rc.ID, rc => rc); }
		set { i_right_Code = value.Values.ToDictionary(irc => irc.Code, irc => irc); }
	}
	public Dictionary<string, clsSector> i_sector_code;
	public Dictionary<string, clsUnit> i_unit_code;
	public Dictionary<string, clsAttribute> i_attribute_code;
	public Dictionary<string, ClsAvalancheOPG> i_OpgRef;
	public Dictionary<string, clsBundle> i_Bundle_code;
	public Dictionary<string, clsFilter> i_Filters_Code;
	public Dictionary<string, clsAccount> i_Account_HostIDpriceBand;
	public Dictionary<string, Dictionary<string, clsSlotType>> i_slotType_Code;

	public Dictionary<string, List<clsScheme>> i_scheme_code;
	private Dictionary<int, clsScheme> Schemes {
		get { return m_Schemes; }
		set { m_Schemes = Value; }
	}
	private Dictionary<int, clsScheme> m_Schemes;
	//Loyalty schemes  ' products have a dictionary of scheme>points

	//Generic editor stuff

	public Dictionary<int, clsScreen> Screens {
		get { return m_Screens; }
		set { m_Screens = Value; }
	}
	private Dictionary<int, clsScreen> m_Screens;
	public Dictionary<int, clsField> Fields {
		get { return m_Fields; }
		set { m_Fields = Value; }
	}
	private Dictionary<int, clsField> m_Fields;
	public Dictionary<int, clsValidation> Validations {
		get { return m_Validations; }
		set { m_Validations = Value; }
	}
	private Dictionary<int, clsValidation> m_Validations;
	public Dictionary<int, clsInputType> InputTypes {
		get { return m_InputTypes; }
		set { m_InputTypes = Value; }
	}
	private Dictionary<int, clsInputType> m_InputTypes;

	public Dictionary<string, clsInputType> i_inputType_code;
		// ' Used by the generic aditor makesecreen() code.. to see if we already have a screen for the TYPE of object we are currently making a screen for
	public Dictionary<string, clsScreen> i_screens_title;
		//Not
	public Dictionary<string, clsScreen> i_screens_code;
	//Public I_SCREENS_TYPE As Dictionary(Of String, clsScreen)
	//Public i_quantity_path As Dictionary(Of String, clsQuantity)

		//makes all the other dictionaries accessible by a name.. see Suggestor.aspx
	public Dictionary<string, object> Gateway;

	//Public channelSKU As Dictionary(Of clsChannel,cls

	//Public Updates As Dictionary(Of Integer, clsUniTran) 'Used for asynchronous webservice pricing calls - hol

	public Dictionary<string, List<clsProductValidation>> ProductValidationsAssignment;
	// a dictioanary  by  BUYER channel a dictionary (by string Promo Letter) - of which branches have visble, descendant promotions 
	//Promos are currently A or B (AB is acceptable)  - for avalance and bundles   (needs to be per buyer - because different buyers can see different products ie.they may not have a price for some)
	//                                            Reseller (e-buyer)                    'A'-for avalanche 'B' for bundles             
	public Dictionary<clsChannel, Dictionary<string, List<clsBranch>>> PromoBranches;
	//    Public bundleBranches As Dictionary(Of clsChannel, List(Of clsBranch))


	public DateTime loadedTimestamp;
	//  Public Property Recommendations As Dictionary(Of Integer, clsRecommendation)


	public bool PNAdown = false;
	//This dictionary and the associated public property replace the standard ASP.NET session object (which is after all just a dictionary of objects, keyed by objects)
	//which gets it's knickers in a twist accross tabs/browsers
	//the entire set of session variables is indexed by a LoginID - which has the advantage of being able to join/snoop on any session in progress
	public Dictionary<UInt64, Dictionary<string, object>> seshDic;
	Dictionary<UInt64, List<string>> seshLog = new Dictionary<ulong, List<string>>();

	Dictionary<UInt64, DateTime> SeshTimes;
	//Switch for strict validation

	public readonly bool StrictSlotValidation = false;
	public clsTranslation EnglishIndex(string text, string @group)
	{

		//the circumflex is just a delimiter for the two parts of the compound key
		object ck = text + "^" + @group;
		if (iEnglishIndex.ContainsKey(ck)) {
			return iEnglishIndex(ck);
		} else {
			return null;
		}

	}
	public int recordLogin(clsUser User, bool failed, string email, string ua)
	{

		if (User == null)
			User = iq.i_user_email("unknown@unknown.com");

		//Store useragent
		da.ExecuteSP(dataAccess.da.OpenDatabase(true), "usp_UpdateUserAgentList", new Dictionary<string, object> { {
			"AgentName",
			ua
		} }, null);

		recordLogin = da.DBExecutesql("INSERT INTO [login] (fk_user_id,timestamp,failed,TypedEmail,lid,FK_UserAgent_Id,ServerNode) VALUES (" + User.ID + ",getdate()," + IIf(failed, 1, 0).ToString() + "," + da.SqlEncode(email) + ",NULL,(SELECT Id FROM UserAgents WHERE AgentName=" + da.SqlEncode(ua) + ")," + da.SqlEncode(Environment.MachineName) + ");", true);


	}

	public clsPriceBand getPriceBand(string text)
	{

		if (this.priceBands.ContainsKey(text)) {
			return this.priceBands(text);
		} else {
			return new clsPriceBand(text);
			//This Should/must be/is the ONLY call to the constructor of clsPriceband
		}

	}
	public void updateLogin(int tid, UInt64 lid)
	{
		da.DBExecutesql("Update Login set lid=" + lid + " WHERE id=" + tid);
	}

	public void updateLogin(UInt64 lid, clsAccount account)
	{
		da.DBExecutesql("Update Login set fk_account_id_agent=" + account.ID + " WHERE lid=" + lid);

		//Dim focus As List(Of String) = New List(Of String)
		//For Each f In Split(account.SellerChannel.Focus, ",")
		//    If Trim(f) <> "" Then
		//        focus.Add(f)  'focus is a list of 'codes' for product groupings we (only) wish the display - eg. Receta
		//    End If
		//Next
		iq.sesh(lid, "foci") = account.SellerChannel.Focus;
		//focus is a CD list in the session variable too

	}

	public bool SeshAlive(UInt64 lid)
	{
		SeshAlive = false;
		if (seshDic == null)
			return false;
		if (seshDic.Count > 0) {
			if (seshDic.ContainsKey(lid)) {
				return true;
			} else {
				return LoadUserState(lid);
			}

		}

	}

	public bool SeshContains(UInt64 lid, string key)
	{

		if (seshDic == null)
			return false;

		if (seshDic.ContainsKey(lid)) {
			return (seshDic(lid).ContainsKey(key));
		} else {
			return false;
		}
	}

	public object SeshValue(UInt64 lid, string key, object AbsentValue)
	{

		//returns a  'sesh'ion variable - returns false if the variable is absent

		if (!seshDic(lid).ContainsKey(key)) {
			return AbsentValue;
		} else {
			return seshDic(lid)(key);
		}

	}


	public Dictionary<string, object> getSeshDic(UInt64 lid)
	{

		UpdateSeshTime(lid);
		return seshDic(lid);

	}


	public T seshTyped<T>(UInt64 lid, string key)
	{
		object res = sesh(lid, key);
		if (res == null)
			return res;
		if (object.ReferenceEquals(res.GetType, typeof(T))) {
			return (T)res;
		} else {
			return null;
		}
	}

	public object sesh {


		get {
			//   If lid = 0 Then Stop

			UpdateSeshTime(lid);
			if (seshDic == null)
				return null;
			if (!seshDic.ContainsKey(lid)) {
				return null;
			} else {
				if (seshDic(lid).ContainsKey(key)) {
					return seshDic(lid)(key);
				} else {
					return null;
				}
			}
		}


		set {
			//            If lid = 0 Then Stop

			if (seshDic == null)
				seshDic = new Dictionary<UInt64, Dictionary<string, object>>();
			if (!seshDic.ContainsKey(lid)) {
				seshDic.Add(lid, new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase));
			}

			// If lid = 0 Then Stop
			seshDic(lid)(key) = value;
		}
	}


	public Table SessionTable()
	{
		if (seshDic == null)
			return null;
		Table t = new Table();
		t.CssClass = "sessionTable";

		object help = "Session ID - Click to view session||||Time to live (session will expire in X minutes)|Buyer|Current Value of basket items";
		TableHeaderRow thr = MakeTHR("lid,Current Page,Agent Email, HostID,TTL,QuotingFor,Quote Value", help, "adminTable");

		t.Rows.Add(thr);

		foreach ( k in seshDic.Keys) {
			t.Rows.Add(SeshTableRow(k));
		}

		return t;

	}

	public TableRow SeshTableRow(UInt64 lid)
	{

		List<string> errorMessages = new List<string>();

		TableCell tc;
		HyperLink hl;
		Label lbl;

		SeshTableRow = new TableRow();

		clsAccount buyerAccount = (clsAccount)iq.sesh(lid, "BuyerAccount");
		clsAccount agentAccount = (clsAccount)iq.sesh(lid, "AgentAccount");


		if (agentAccount != null) {







				//Agent (sellers)  Email

				//Sellers HOSTID

				//Time to live (that's 'LIV' not 'Lyve'

				//Buyers Email


				//Quote value


			 // ERROR: Not supported in C#: WithStatement

		}

	}


	public void KillSesh(UInt64 lid)
	{
		// Dim StartBytes As Long = System.GC.GetTotalMemory(True)

		if (SeshTimes != null) {
			if (SeshTimes.ContainsKey(lid)) {
				SeshTimes.Remove(lid);
			}
		}

		if (seshDic != null) {
			if (seshDic.ContainsKey(lid)) {
				seshDic.Remove(lid);
			}
		}

		//  Dim Stopbytes As Long = System.GC.GetTotalMemory(True)

		// KillSesh = "Terminated session " & lid & " freeing approximately " & Int((StartBytes - Stopbytes) / (1024)) & "KB"

		TidySwiftBranches(lid);



	}
	public int KillOldSessions()
	{

		List<UInt64> toKill = new List<UInt64>();
		if (SeshTimes != null) {
			foreach ( kvp in SeshTimes.ToArray()) {
				if (Math.Abs(DateDiff(DateInterval.Minute, Now, kvp.Value)) > 60) {
					toKill.Add(kvp.Key);
				}
			}

			foreach ( s in toKill) {
				SeshTimes.Remove(s);
				if (seshDic != null)
					seshDic.Remove(s);
				TidySwiftBranches(s);
			}
		}

		return toKill.Count;

	}


	private void UpdateSeshTime(UInt64 lid)
	{
		if (SeshTimes == null)
			SeshTimes = new Dictionary<UInt64, DateTime>();
		lock (SeshTimes) {
			if (!SeshTimes.ContainsKey(lid)) {
				SeshTimes.Add(lid, Now);
			} else {
				SeshTimes(lid) = Now;
			}
		}
	}


	public clsIQ()
	{
		Randomize(Now.Millisecond);

		this.AllVariants = new clsVariant();

		this.Branches = new Dictionary<int, clsBranch>();
		//Me.Events = New Dictionary(Of Integer, clsEvent)
		this.Threads = new Dictionary<int, clsThread>();
		this.i_SpecialBranches = new Dictionary<string, clsBranch>();

		//PathCache = New clsProductCache
		Languages = new Dictionary<int, clsLanguage>();
		ActiveLanguages = new Dictionary<int, clsLanguage>();
		Translations = new Dictionary<int, clsTranslation>();
		//

		Products = new Dictionary<int, clsProduct>();
		Stock = new Dictionary<int, clsstock>();
		//we need a 'flat' list of the stock for incremental import purposes - the stock embeded in the product is more generally what's used
		Variants = new Dictionary<int, clsVariant>();

		iEnglishIndex = new Dictionary<string, clsTranslation>();
		//makes the dictionary keys case insensitive
		KYIndex = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase);
		//makes the dictionary keys case insensitive
		Attributes = new Dictionary<int, clsAttribute>();
		Units = new Dictionary<int, clsUnit>();

		Quantities = new Dictionary<int, clsQuantity>();

		Users = new Dictionary<int, clsUser>();
		Accounts = new Dictionary<int, clsAccount>();
		Teams = new Dictionary<int, clsTeam>();
		Channels = new Dictionary<int, clsChannel>();
		BuyerGroups = new Dictionary<int, clsBuyerGroup>();
		Currencies = new Dictionary<int, clsCurrency>();
		Cultures = new Dictionary<int, clsCulture>();
		//    Countries = New Dictionary(Of Integer, clsCountry)
		Regions = new Dictionary<int, clsRegion>();
		States = new Dictionary<int, clsState>();
		//  Prunes = New Dictionary(Of Integer, clsPrune)
		SlotTypes = new Dictionary<int, clsSlotType>();
		ProductTypes = new Dictionary<int, clsProductType>();
		Sectors = new Dictionary<int, clsSector>();
		InputTypes = new Dictionary<int, clsInputType>();
		Validations = new Dictionary<int, clsValidation>();
		Screens = new Dictionary<int, clsScreen>();
		Fields = new Dictionary<int, clsField>();
		Quotes = new Dictionary<int, clsQuote>();
		AvalancheOPGs = new Dictionary<int, ClsAvalancheOPG>();
		FlexOPGs = new Dictionary<int, clsFlexOPG>();
		Bundles = new Dictionary<int, clsBundle>();
		Excludes = new Dictionary<int, clsExclude>();
		//Variants = New Dictionary(Of Integer, clsVariant) - moved into the Products
		Prices = new Dictionary<int, clsPrice>();
		Campaigns = new Dictionary<int, clsCampaign>();
		Adverts = new Dictionary<int, clsAdvert>();
		ScreenOverrides = new List<clsScreenOverride>();
		priceBands = new Dictionary<string, clsPriceBand>();
		Locations = new Dictionary<string, string>();
		DefaultCurrencies = new Dictionary<int, clsCurrency>();

		PromoBranches = new Dictionary<clsChannel, Dictionary<string, List<clsBranch>>>();
		Schemes = new Dictionary<int, clsScheme>();
		//Master list of Loyalty schemes  - products have a dictionary of scheme>points
		//  Recommendations = New Dictionary(Of Integer, clsRecommendation)


		//these 'index' dictionaries allow us to look things up very quickly. typically by some human readable code (rather than Row ID)
		//they are automatically added to in the constructors (sub New's) of the Classes they hold.
		//they carry only the string key, as a *reference* to an instance of an object - so their footprint is quite modsest
		i_user_email = new Dictionary<string, clsUser>(StringComparer.CurrentCultureIgnoreCase);
		//index of username to user objects (as there are several thousand)
		i_state_GroupCode = new Dictionary<string, clsState>(StringComparer.CurrentCultureIgnoreCase);
		// allows state (messages and ID's) to be looked up via a compound key of group-code
		i_channel_code = new Dictionary<string, clsChannel>(StringComparer.CurrentCultureIgnoreCase);
		i_SKU = new Dictionary<string, clsProduct>(StringComparer.CurrentCultureIgnoreCase);
		//used primarly by the webservice to look up products by SKU

		//i_variant_code = New Dictionary(Of String, clsVariant)
		i_currency_code = new Dictionary<string, clsCurrency>(StringComparer.CurrentCultureIgnoreCase);
		i_culture_code = new Dictionary<string, clsCulture>(StringComparer.CurrentCultureIgnoreCase);
		//i_country_code = New Dictionary(Of String, clsCountry)
		i_region_code = new Dictionary<string, clsRegion>(StringComparer.CurrentCultureIgnoreCase);
		i_language_Code = new Dictionary<string, clsLanguage>(StringComparer.CurrentCultureIgnoreCase);
		i_ProductType_Code = new Dictionary<string, clsProductType>(StringComparer.CurrentCultureIgnoreCase);
		i_role_Code = new Dictionary<string, clsRole>(StringComparer.CurrentCultureIgnoreCase);
		i_right_Code = new Dictionary<string, clsRight>(StringComparer.CurrentCultureIgnoreCase);
		i_sector_code = new Dictionary<string, clsSector>(StringComparer.CurrentCultureIgnoreCase);
		i_unit_code = new Dictionary<string, clsUnit>(StringComparer.CurrentCultureIgnoreCase);
		i_attribute_code = new Dictionary<string, clsAttribute>(StringComparer.CurrentCultureIgnoreCase);
		i_inputType_code = new Dictionary<string, clsInputType>(StringComparer.CurrentCultureIgnoreCase);
		i_screens_title = new Dictionary<string, clsScreen>(StringComparer.CurrentCultureIgnoreCase);
		i_screens_code = new Dictionary<string, clsScreen>(StringComparer.CurrentCultureIgnoreCase);

		i_OpgRef = new Dictionary<string, ClsAvalancheOPG>(StringComparer.CurrentCultureIgnoreCase);
		i_Bundle_code = new Dictionary<string, clsBundle>(StringComparer.CurrentCultureIgnoreCase);
		i_Filters_Code = new Dictionary<string, clsFilter>(StringComparer.CurrentCultureIgnoreCase);
		i_Account_HostIDpriceBand = new Dictionary<string, clsAccount>(StringComparer.CurrentCultureIgnoreCase);
		i_slotType_Code = new Dictionary<string, Dictionary<string, clsSlotType>>(StringComparer.CurrentCultureIgnoreCase);
		i_scheme_code = new Dictionary<string, List<clsScheme>>(StringComparer.CurrentCultureIgnoreCase);

		Gateway = new Dictionary<string, object>();
		//makes other dictionaries accessible by name - allowing suggestion against many dictionaries (see suggestor.aspx)
		//                                             could be done with reflection - but I didn't have a good handle on that at the time, and this works just fine.

		Gateway.Add("channels", Channels);
		Gateway.Add("currencies", Currencies);
		Gateway.Add("regions", Regions);
		Gateway.Add("accounts", Accounts);
		//we will re-point this to the SellerChannels CustomerAccounts - just in time
		Gateway.Add("SKUs", i_SKU);

		Profiling.Profile = new Dictionary<string, clsProfile>();

	}


	public void load(List<string> errormessages)
	{
		//If IsLoading Then Exit Sub
		if (clsIQ.IsLoaded){clsIQ.reset();return;
}



		//        Try
		IsLoading = true;
		if (StartBytes == null)
			StartBytes = System.GC.GetTotalMemory(true);

		errormessages.Clear();
		messages.Clear();
		messages.Start();

		bootstrap();

		long StopBytes;

		//        p.ID = "loadinfo"
		//       p.Attributes("style") = "height:1.5em;overflow:hidden"

		//Dim b As New Image
		//b.ImageUrl = "/images/navigation/expand.png"
		//b.Attributes("onclick") = "document.getElementById('ctl00_MainContent_loadinfo').style.height='auto';"
		//p.Controls.Add(b)

		//Loads everything from the database

		SqlClient.SqlConnection con;

		con = da.OpenDatabase();
		messages.Add("Opened the database");

		SqlClient.SqlDataReader r;
		r = null;

		messages.Add(LoadLanguages(con, r));
		messages.Add(LoadTranslations(con));

		// messages.Add(LoadProductValidations(con, r))

		messages.Add(LoadCultures(con, r));
		//messages.Add("size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")
		messages.Add(LoadRegions(con, r));

		r_worldwide = clsRegion.getOrMake(null, "XW", "Worldwide", false, false, "Not precious about XW");

		messages.Add(LoadStates(con, r));
		//LoadEvents(con) 'NB the Root Event is set in here

		//messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		messages.Add(LoadUnits(con, r));
		messages.Add(LoadAttributes(con, r));
		//messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		messages.Add(LoadSlotTypes(con, r));
		messages.Add(LoadProductTypes(con, r, errormessages));
		messages.Add(LoadSectors(con, r));

		//messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		messages.Add(LoadProducts(con, r));

		//messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		messages.Add(LoadCurrencies(con, r));

		messages.Add(LoadChannels(con, r));

		//If Not iq.i_channel_code.ContainsKey("Everyone") Then Dim achannel As clsChannel = New clsChannel(Nothing, "Everyone", "Public (list) pricing", "", "Everyone", r_worldwide, New nullableString, New nullableString, New nullableString, 0, "tree.1", "", 0, 0, "R", "", "") 'everyone is not a selling channel (so no priceConfig is required)

		Everyone = iq.getPriceBand("");
		//HPList = iq.getPriceBand("HPlist")

		messages.Add(LoadCampaigns(con, r));
		messages.Add(LoadPromos(con, r));
		messages.Add(LoadAdverts(con, r));
		messages.Add(LoadScreenOverrides(con, r));

		//StopBytes = System.GC.GetTotalMemory(False)
		//messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		//txt &= LoadVariants(con, r)

		//StopBytes = System.GC.GetTotalMemory(False)
		//txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"


		messages.Add(loadInputTypes(con, r));
		messages.Add(LoadScreens(con, r));
		double ts = Stopwatch.GetTimestamp;
		messages.Add(LoadBranches(con, r, errormessages));
		int et = (Stopwatch.GetTimestamp - ts) / Stopwatch.Frequency * 1000;


		messages.Add(LoadExcludes(con, r));

		messages.Add(LoadSpecialBranches(con, r, errormessages));

		messages.Add(LoadValidationsInclusions(con, r));

		//StopBytes = System.GC.GetTotalMemory(False)
		//messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		messages.Add(LoadSlots(con, r));
		//StopBytes = System.GC.GetTotalMemory(False)
		//messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		con.Close();
		con = da.OpenDatabase();
		messages.Add(LoadUserMessages(con, r));
		messages.Add(LoadAddresses(con, r));
		messages.Add(LoadLegal(con, r));
		messages.Add(LoadResources(con, r));

		messages.Add(LoadProductAttributes(con, r));
		messages.Add(CleanProducts(con, r));

		messages.Add(LoadROKAttributes(con, r));

		//StopBytes = System.GC.GetTotalMemory(False)
		//messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		//txt &= LoadCountries(con, r)

		messages.Add(LoadHPServiceLevels(con, r));


		messages.Add(LoadAvalancheOPGs(con, r));
		//StopBytes = System.GC.GetTotalMemory(False)
		// messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		messages.Add(LoadBundles(con, r));

		messages.Add(LoadFlex(con, r));
		messages.Add(LoadFlexRegions(con, r));
		messages.Add(LoadFlexLines(con, r));
		messages.Add(LoadFlexRules(con, r));

		//StopBytes = System.GC.GetTotalMemory(False)
		//  messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		messages.Add(LoadSchemes(con, r));
		messages.Add(LoadPoints(con, r));

		//StopBytes = System.GC.GetTotalMemory(False)
		//   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")


		messages.Add(LoadQuantities(con, r));

		//StopBytes = System.GC.GetTotalMemory(False)
		//    messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		//txt &= LoadChannelSkus(con, r)


		//StopBytes = System.GC.GetTotalMemory(False)
		//  messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		con.Close();
		con = da.OpenDatabase();

		// txt &= LoadBuyerGroups(con, r)
		messages.Add(LoadUsers(con, r, errormessages));
		messages.Add(LoadTeams(con, r));
		messages.Add(LoadRoles(con, r));
		messages.Add(LoadRights(con, r));
		messages.Add(LoadRoleRights(con, r));
		messages.Add(LoadAccounts(con, r));
		//StopBytes = System.GC.GetTotalMemory(False)
		//   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		//  txt &= LoadPrices(con, r, errorMessages)
		//  StopBytes = System.GC.GetTotalMemory(False)
		//  txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"

		messages.Add(LoadMargins(con, r));
		//StopBytes = System.GC.GetTotalMemory(False)
		//   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		//txt &= LoadStock(con, r)
		//StopBytes = System.GC.GetTotalMemory(False)
		//txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"
		con.Close();
		con = da.OpenDatabase();
		//        txt &= LoadScreens(con, r)
		messages.Add(LoadValidations(con, r).ToString());
		messages.Add(LoadThreads(con, r).ToString());
		messages.Add(LoadFilters());

		messages.Add(LoadConversions(con, r));
		messages.Add(LoadMeasures(con, r));
		messages.Add(LoadActiveUniversal(con, r));
		//messages.Add(LoadLocations(con, r))

		if (iq.i_channel_code.ContainsKey("HP")) {
			HP = iq.i_channel_code("HP");
			if (HP != null) {
				messages.Add(HP.LoadVariants(errormessages, 1));
				//     messages.Add("size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")
			}

			//We now, dynamically load listprices for the users, accounts, sellerchannels, country - at signin (see accounts.aspx.vp)
			//txt &= HP.LoadPrices(Everyone, errormessages, r_GB.ID)

			//StopBytes = System.GC.GetTotalMemory(False)
			//    messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

		}


		object distinctlang = from j in iq.Accounts.Valuesj.Language;

		foreach (clsLanguage selectedlang in distinctlang) {
			ActiveLanguages.Add(selectedlang.ID, selectedlang);
		}


		foreach ( cb in iq.RootBranch.childBranches.Values) {
			string btl = cb.Translation.text(English);
			if (btl.ToLower.Contains("accessories and services")) {
				int c;
				cb.flagAsUnsearchable(c);
				Debug.Print(c);
			}
		}

		messages.Add("Loaded complete Object Model ");
		messages.Add("HP iQuote 2 version " + My.Application.Info.Version.ToString);

		//If Not iq.Root Is Nothing Then
		//    LastMilestone = Stopwatch.GetTimestamp
		//    IndexSKUs()
		//    txt &= "Indexed SKUs"
		//End If
		messages.Add(LoadUserStates(con, r));
		messages.Add(LoadProductValidations(con, r));
		this.loadedTimestamp = Now;
		con.Close();
		//p.Controls.Add(NewLit())

		StopBytes = System.GC.GetTotalMemory(true);
		messages.Add("Total OM size (minus channel specific pricing) now  :" + Int((StopBytes - StartBytes) / (Math.Pow(1024, 2))) + "MB");

		//  p.Controls.Add(OutputErrors(errorMessages, lid))

		checkEssentials();

		_instance.IsLoaded = true;

		if (StartBytes != null) {
			EndBytes = System.GC.GetTotalMemory(true);
			if (StopBytes - StartBytes > 0) {
				messages.Add("Object model reload freeing approximately " + Int((StopBytes - StartBytes) / (Math.Pow(1024, 2))) + "MB");
			} else {
				messages.Add("Object model load taking apporx " + Int((StartBytes - StopBytes) / (Math.Pow(1024, 2))) + "MB");
			}
		}

		messages.StopClock();

		// Dim count As Integer = 0
		// RecurseChildren(iq.Root, count, "Tree")
		// load &= "<b>Walked tree of " & count.ToString("###,###,###,###") & " options" & TimeSince(Start) & "<br/></b>"

		//Catch ex As Exception
		//messages.Add("Exception: " + ex.Message)
		//        End Try




		clsBranch mastercpusbranch = null;
		foreach ( branch in iq.Branches.Values) {
			if (branch.Translation.text(English).ToUpper == "CPU") {
				if (branch.childBranches.Count > 30) {
					mastercpusbranch = branch;
					break; // TODO: might not be correct. Was : Exit For
				}

			}
		}

		if (mastercpusbranch == null)
			System.Diagnostics.Debugger.Break();

		cleanChassisBranches();


		IsLoading = false;
	}

	public Dictionary<UInt64, clsUserState> OldUserSessions = new Dictionary<ulong, clsUserState>();

	public void cleanChassisBranches()
	{
		List<string> em = new List<string>();
		string summary = "";

		Dictionary<string, int> counts = new Dictionary<string, int>();

		foreach ( b in iq.Branches.Values.ToList) {
			if (b.Product != null) {
				if (b.Product.ProductType.Code.ToLower == "chas") {
					if (b.Translation.text(English) != "CPU") {
						string pd = b.EnglishName;
						if (!pd.ToLower.EndsWith(" chassis")) {
							object a = 9;
							summary = "";
							b.HardDelete(em, summary, 0, true, counts);
						}
					}
				}
			}
		}

		foreach ( k in counts.Keys) {
			Debug.Print(k + " - " + counts(k));
		}

	}



	private string LoadProductValidations(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{
		try {
			ProductValidationsAssignment = new Dictionary<string, List<clsProductValidation>>();
			r = da.DBExecuteReader(con, "Select ProductValidations.Id,SystemType,[OptType],[ValidationType],[Severity],[FK_Translation_Key_Message],[RequiredQuantity],[CheckAttribute],[DependantOptType],DependantCheckAttribute,DependantCheckAttributeValue,CheckAttributeValue,FK_Translation_Key_CorrectMessage,ValidationMessageType,[LinkTechnology],[LinkOptType],[LinkOptionFamily] from ProductValidations INNER JOIN ProductValidationMappings on FK_ProductValidation_ID = Id");

			int duds;
			if (r.HasRows) {
				while (r.Read) {
					if (!ProductValidationsAssignment.ContainsKey(r("SystemType").ToString())) {
						ProductValidationsAssignment.Add(r("SystemType").ToString(), new List<clsProductValidation>());
					}


					int mkey = r.Item("FK_Translation_Key_Message");
					int cmkey = r.Item("FK_Translation_Key_correctMessage");


					if (iq.Translations.ContainsKey(mkey) && iq.Translations.ContainsKey(cmkey)) {
						clsTranslation mt = iq.Translations(mkey);
						clsTranslation cmt = iq.Translations(cmkey);

						clsProductValidation pv = new clsProductValidation {
							ID = r("ID"),
							CheckAttribute = r("CheckAttribute").ToString(),
							DependantOptType = r("DependantOptType").ToString(),
							Message = iq.Translations(r("FK_Translation_Key_Message")),
							CorrectMessage = iq.Translations.ContainsKey(r("FK_Translation_Key_CorrectMessage")) ? iq.Translations(r("FK_Translation_Key_CorrectMessage")) : null,
							RequiredOptType = r("OptType").ToString(),
							RequiredQuantity = r("RequiredQuantity"),
							Severity = Enum.Parse(typeof(EnumValidationSeverity), r("Severity").ToString()),
							ValidationType = Enum.Parse(typeof(enumValidationType), r("ValidationType").ToString()),
							DependantCheckAttribute = object.ReferenceEquals(r("DependantCheckAttribute"), DBNull.Value) ? null : r("DependantCheckAttribute"),
							DependantCheckAttributeValue = object.ReferenceEquals(r("DependantCheckAttributeValue"), DBNull.Value) ? null : r("DependantCheckAttributeValue"),
							CheckAttributeValue = object.ReferenceEquals(r("CheckAttributeValue"), DBNull.Value) ? null : r("CheckAttributeValue"),
							ValidationMessageType = Enum.Parse(typeof(enumValidationMessageType), r("ValidationMessageType").ToString()),
							LinkOptType = r("LinkOptType").ToString(),
							LinkTechnology = r("LinkTechnology").ToString(),
							LinkOptionFamily = r("LinkOptionFamily").ToString
						};

						ProductValidationsAssignment(r("SystemType").ToString()).Add(pv);
					} else {
						duds += 1;

					}
				}
			}

		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;
		}

	}

	private string LoadUserStates(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{
		try {
			r = da.DBExecuteReader(con, "Select states from UserStates WHERE HostName='" + Environment.MachineName + "' order by datetime desc");
			iq.seshDic = new Dictionary<ulong, Dictionary<string, object>>();

			if (r.HasRows) {

				while (r.Read) {
					List<clsUserState> states = new List<clsUserState>();
					XmlSerializer x = new XmlSerializer(states.GetType());
					using (StringReader t = new StringReader(r("states"))) {
						states = x.Deserialize(t);
						foreach ( us in states) {
							//Nick added as a 'quick fix' 10/12/2014 10:58
							if (!iq.OldUserSessions.ContainsKey(us.lid)) {
								iq.OldUserSessions.Add(us.lid, us);
							}
						}
					}
				}
			}
			return string.Format("Importing existing user sessions: {0} imported", iq.seshDic.Count);
		} catch (Exception ex) {
			return "Failed: " + ex.Message;
		}
	}

	public bool LoadUserState(UInt64 lid)
	{

		bool loaded = false;


		if (iq.OldUserSessions.ContainsKey(lid)) {

			try {
				clsUserState us = iq.OldUserSessions(lid);
				if (iq.seshDic.ContainsKey(us.lid))
					return false;

				iq.seshDic.Add(us.lid, new Dictionary<string, object>());
				iq.seshDic(us.lid).Add("path", us.path);
				iq.seshDic(us.lid).Add("Root", us.root);
				iq.seshDic(us.lid).Add("foci", us.foci);
				iq.seshDic(us.lid).Add("QuoteID", us.QuoteID);
				iq.seshDic(us.lid).Add("showOnly", us.showOnly);
				iq.seshDic(us.lid).Add("AgentAccount", iq.Accounts(us.AgentAccount));
				iq.seshDic(us.lid).Add("BuyerAccount", iq.Accounts(us.BuyerAccount));
				iq.seshDic(us.lid).Add("treeCursorPath", us.treeCursorPath);
				iq.seshDic(us.lid).Add("branchStates", us.branchStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
				iq.seshDic(us.lid).Add("Paradigm", us.Paradigm);

				//Load nescessary details for accounts
				clsAccount buyerAccount = (clsAccount)iq.sesh(us.lid, "BuyerAccount");
				clsAccount agentAccount = (clsAccount)iq.sesh(us.lid, "AgentAccount");

				TagPromoBranches(buyerAccount, errorMessages);
				agentAccount.SellerChannel.IsCloneOf.LoadVariants(errorMessages, 0.1);

				if (!agentAccount.SellerChannel.IsCloneOf.stockLoaded) {
					agentAccount.SellerChannel.IsCloneOf.LoadStock();
				}

				if (!agentAccount.SellerChannel.IsCloneOf.pricesLoadedFor.ContainsKey(agentAccount.Priceband)) {
					agentAccount.SellerChannel.IsCloneOf.LoadPrices(agentAccount.Priceband, errorMessages);
				}

				clsRegion rgn = agentAccount.SellerChannel.IsCloneOf.Region;
				if (!HP.listPricesLoadedFor.ContainsKey(rgn) || HP.listPricesLoadedFor(rgn) == 0) {
					HP.LoadPrices(Everyone, errorMessages, agentAccount.SellerChannel.Region);
				}

				//Add quote in 
				agentAccount.LoadQuotes(Val(buyerAccount.ID));
				if (us.QuoteID != null && us.QuoteID != 0)
					agentAccount.Quotes(us.QuoteID).LoadItems(errorMessages);

				//Matrix Headers
				iq.seshDic(us.lid).Add("screenHeaders", new Dictionary<string, clsScreenHeader>());
				object mhs = (Dictionary<string, clsScreenHeader>)iq.sesh(us.lid, "screenHeaders");
				foreach ( mh in us.ScreenHeaders) {
					clsBranchInfo bi = new clsBranchInfo(us.lid, mh.Path, null, 70, enumParadigm.errorNotSet, errorMessages);
					//Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
					object descendants = bi.visibleChildren(errorMessages, true, 0, 0, false, true);
					object nmh = new clsScreenHeader(bi, descendants, mh.QuickFiltersVisible);
					foreach ( f in mh.Filters) {
						object d = new Dictionary<clsFilter, List<long>>();
						foreach ( fil in f.Value) {
							d.Add(iq.Filters(fil.Key.ID), fil.Value);
						}
						nmh.Filters.Add(iq.Fields(f.Key), d);
					}
					foreach ( s in mh.Sorts) {
						if (nmh.sorts.ContainsKey(s.Key)) {
							nmh.sorts(s.Key).columnid = s.Value.columnid;
							nmh.sorts(s.Key).Direction = s.Value.Direction;
							nmh.sorts(s.Key).Priority = s.Value.Priority;
						} else {
							nmh.sorts.Add(s.Key, s.Value);
						}

					}
				}

				// Pick up any mop-up values
				if (!us.mopUpvalues == null) {
					foreach ( kvp in us.mopUpvalues) {
						if (!iq.seshDic(us.lid).ContainsKey(kvp.Key)) {
							iq.seshDic(us.lid).Add(kvp.Key, kvp.Value);
						}
					}
				}

				loaded = true;


			} catch (Exception ex) {
				ErrorLog.Add(ex);

			}

		}

		return loaded;

	}

	private string LoadFilters()
	{

		Filters = new Dictionary<int, clsFilter>();

		clsFilter afilter;

		afilter = new clsFilter(1, "SW", iq.AddTranslation("Starts with", English, "FT", 0, null, 0, false), "[col] LIKE '[filterValue]*'");
		afilter = new clsFilter(2, "EW", iq.AddTranslation("Ends with", English, "FT", 0, null, 0, false), "[col] LIKE '*[filterValue]'");
		afilter = new clsFilter(3, "CN", iq.AddTranslation("Contains", English, "FT", 0, null, 0, false), "[col] LIKE '*[filterValue]*'");

		//afilter = New clsFilter(4, "ONLY", iq.AddTranslation("Only", English, "FT"), "[col]= '[filterValue]'")             '
		// afilter = New clsFilter(5, "EX", iq.AddTranslation("Excluding", English, "FT"), "[col]<>'[filterValue]'")

		afilter = new clsFilter(4, "ONLY", iq.AddTranslation("Only", English, "FT", 0, null, 0, false), "[col]= [filterValue]");
		//NOTE - these filter by the numeric values - which is faster
		afilter = new clsFilter(5, "EX", iq.AddTranslation("Excluding", English, "FT", 0, null, 0, false), "[col]<>[filterValue]");

		// afilter = New clsFilter("HN", iq.AddTranslation("HavingStarts with", English, "FT"), "[col]=[filterValue]")
		// afilter = New clsFilter("HNL", iq.AddTranslation("Starts with", English, "FT"), "[col]<=[filterValue]")
		// afilter = New clsFilter("HNM", iq.AddTranslation("Starts with", English, "FT"), "[col]>=[filterValue]")

		afilter = new clsFilter(6, "GE", iq.AddTranslation("Greater than or Equal to this", English, "FT", 0, null, 0, false), "[col]>=[filterValue]");

		afilter = new clsFilter(7, "EQ", iq.AddTranslation("Equal to this", English, "FT", 0, null, 0, false), "[col]=[filterValue]");
		afilter = new clsFilter(8, "LE", iq.AddTranslation("Less than or Equal to this", English, "FT", 0, null, 0, false), "[col]<=[filterValue]");
		afilter = new clsFilter(9, "PM10", iq.AddTranslation("within 10% of this", English, "FT", 0, null, 0, false), "[col]>=[filterValue]*.9 and [col]<=[filterValue]*1.1");
		afilter = new clsFilter(10, "PM20", iq.AddTranslation("within 20% of this", English, "FT", 0, null, 0, false), "[col]>=[filterValue]*.8 and [col]<=[filterValue]*1.2");

		afilter = new clsFilter(11, "B4", iq.AddTranslation("before", English, "FT", 0, null, 0, false), "[col]<[filterValue]");
		afilter = new clsFilter(12, "AFT", iq.AddTranslation("After", English, "FT", 0, null, 0, false), "[col]>[filterValue]");
		afilter = new clsFilter(13, "ON", iq.AddTranslation("On", English, "FT", 0, null, 0, false), "[col]=[filterValue]");

		afilter = new clsFilter(14, "T", iq.AddTranslation("Only Ticked", English, "FT", 0, null, 0, false), "[col]=true");
		afilter = new clsFilter(15, "F", iq.AddTranslation("Only UnTicked", English, "FT", 0, null, 0, false), "[col]=false");

		afilter = new clsFilter(16, "WITH", iq.AddTranslation("With", English, "FT", 0, null, 0, false), "[col]<>0");
		afilter = new clsFilter(17, "WITHOUT", iq.AddTranslation("Without", English, "FT", 0, null, 0, false), "[col]=0");
		//"(isnull([col],-100)=-100)")

		afilter = new clsFilter(18, "GZ", iq.AddTranslation("In stock", English, "FT", 0, null, 0, false), "[col]>0");
		afilter = new clsFilter(19, "FS", iq.AddTranslation("Like this and faster", English, "FT", 0, null, 0, false), "[col]>=[filterValue]");
		afilter = new clsFilter(20, "SL", iq.AddTranslation("Like this and slower", English, "FT", 0, null, 0, false), "[col]<=[filterValue]");
		afilter = new clsFilter(21, "LEGE", iq.AddTranslation("Between", English, "FT", 0, null, 0, false), "[col]<=[filterValue]");

		//non
		// afilter = New clsFilter(21, "TXT", iq.AddTranslation("Containing", English, "FT"), "[col] like '*[filterValue]*'")

		return "loaded " + Filters.Values.Count + " Filters<br/>";

	}



	public string UserAccountName(int UserID, int AccountID)
	{

		try {
			return iq.Users(UserID).Email + "/" + iq.Accounts(AccountID).SellerChannel.Name;
		} catch {
			return "invalid/unknown";
		}


	}

	public void RecurseChildren(clsBranch parent, ref int count, path)
	{
		//walks' the tree of every possible branch - soley to count them

		object bp;
		foreach ( child in parent.childBranches.Values) {
			bp = path + "." + child.ID;
			//If Not Prunes.ContainsKey(bp$) Then 'dont recurse into pruned braches
			count += 1;
			RecurseChildren(child, count, bp);
			//End If
		}

	}
	public bool Retract(clsBranch branch, username, ref List<string> errormessages)
	{

		//reParents each child of Branch under Branches' parent

		//Check that we are only retracting a branch containing other items and not a 'model' with a quantity etc
		if (branch.HasSKU()) {
			errormessages.Add("!Error - cannot retract a branch with a SKU, it has no children!");
			return false;
		}


		List<clsBranch> tomove = new List<clsBranch>();
		//we need to build a list - becuase we can't iterate a collection we're modifying (branch.childbranches)

		foreach ( child in branch.childBranches.Values) {
			tomove.Add(child);
		}

		foreach ( child in tomove) {
			child.Parent = branch.Parent;
			//  child.Parent.childBranches.Add(child.ID, child)  'so it appears immediately
			child.Update(errormessages);
		}

		//finally remove the branch we're retracting

		branch.delete(errormessages);

		return true;

	}
	public int Prune(string path, string userName)
	{
		//prunes are done by path (branch is not unique or specific!)

		//Creates a new Prune, 
		//NB - *all* prunes have a path - and take off, *one* instance of a branch (and all its descendants)
		//if you want to remove a branch from everywhere - delete it !

		//Persists it to the DB and adds it to the global prunes list (iq.prunes)
		//Thereby preventing the branch from appearing when (re)openend
		//Username is for audit trail purposes - and so that prunes can be easily located/removed in future

		//If iq.Prunes.ContainsKey(path) Then
		//   MsgBox("That branch is already pruned")
		//Return 0
		//        Else
		clsPrune aprune;
		aprune = new clsPrune(path, new NullableInt(), userName);
		return aprune.ID;
		//       End If

	}
	private string LoadProductAttributes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{
		try {
			int count;
			object sql;

			//sql$ = "Select productattribute.id,FK_Product_Id,FK_Attribute_id,Numericvalue,fk_translation_key_text, a.code as acode,u.code as ucode from ProductAttribute "
			// sql$ &= "Join [attribute] a on a.id=fk_attribute_id "
			// sql$ &= "Join unit u on u.id=fk_unit_id"
			sql = "Select id,FK_Product_Id,FK_Attribute_id,Numericvalue,fk_translation_key_text, fk_unit_id  from ProductAttribute ";
			sql += "WHERE deleted = 0";

			r = da.DBExecuteReader(con, sql);

			int failed = 0;
			clsProductAttribute PA;
			// the act of creating a product attribute - adds it to the specified product
			int indexedSKUs;
			float numval;
			int translationKey;

			int missingProducts = 0;
			int missingAttributes = 0;
			int missingUnits = 0;
			int bad = 0;

			HashSet<clsProductAttribute> toupdate = new HashSet<clsProductAttribute>();
			//runtime fix for rempapping productacttributes of duplictaed products (this code can be removed in the long term)
			HashSet<string> todel = new HashSet<string>();
			//

			if (r.HasRows) {
				while (r.Read) {
					int aId = (int)r.Item("fk_attribute_id");
					if (iq.Attributes.ContainsKey(aId)) {
						clsAttribute attrib = iq.Attributes(aId);
						numval = (float)r.Item("NumericValue");
						int uId = (int)r.Item("fk_unit_id");

						bool updateIt = false;
						if (iq.Units.ContainsKey(uId)) {
							clsUnit unit = iq.Units(uId);
							clsProduct product = null;
							int pid = r.Item("FK_product_id");

							if (iq.Products.ContainsKey(pid)) {
								product = iq.Products(pid);
							} else {
								if (REMAPS.ContainsKey(pid)) {
									product = REMAPS(pid);
									//UGLY LIVE FIXUP OF DATA
									if (product.i_Attributes_Code.ContainsKey(attrib.Code)) {
										todel.Add(r.Item("ID"));
										continue;
									} else {
										updateIt = true;
										//
									}
								} else {
									//Beep()
									product = null;
								}
							}

							if (product == null) {
								missingProducts += 1;
							} else {
								clsTranslation tl;
								if (!IsDBNull(r.Item("fk_translation_key_text"))) {
									translationKey = (int)r.Item("fk_translation_key_text");
									if (iq.Translations.ContainsKey(translationKey)) {
										tl = iq.Translations(translationKey);
									} else {
										tl = null;
									}
								} else {
									tl = null;
								}

								PA = new clsProductAttribute((int)r.Item("Id"), product, attrib, numval, unit, tl);

								if (updateIt)
									toupdate.Add(PA);

								count += 1;
							}
						} else {
							missingUnits += 1;
						}
					} else {
						missingAttributes += 1;
					}

				}
			}
			r.Close();

			if (todel.Count) {
				da.DBExecutesql("DELETE FROM productattribute WHERE id in (" + Join(todel.ToArray, ",") + ")");
			}
			foreach ( PA in toupdate) {
				PA.update(errorMessages);
			}



			return "Loaded " + count + " ProductAttributes. " + missingAttributes + "Missing attributes" + "," + missingUnits + " missing units, " + missingProducts + " missing (deleted ?) products." + " Deleted " + todel.Count + " duplicate productattributes, updated (remapped) " + toupdate.Count;
		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return "Failed: " + ex.Message;
		}
	}

	private string CleanProducts(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		// Build a temporary list of PL code to Manufacturer mappings
		object plcodeLookup = new Dictionary<string, string>();
		object Sql = "Select plCode, mfrCode from PLCodeLookup";
		r = da.DBExecuteReader(con, Sql);
		if (r.HasRows) {
			while (r.Read) {
				plcodeLookup.Add(r.Item("plCode"), r.Item("mfrCode"));
			}
		}
		r.Close();


		foreach ( product in iq.Products.Values) {
			bool update = false;

			// Fill in any missing Product.SKU values from the relevant attribute
			if (string.IsNullOrEmpty(product.SKU)) {
				if (product.Attributes != null) {
					if (product.i_Attributes_Code.ContainsKey("mfrsku") && product.i_Attributes_Code("mfrsku").Count > 0) {
						product.SKU = product.i_Attributes_Code("mfrsku")(0).Translation.text(English);
						update = true;
					}
					//    If (product.isOption Or product.isSystem) And String.IsNullOrEmpty(product.SKU) Then Stop
				}
			}

			// Fill in any missing Product.mfrCode values
			if (string.IsNullOrEmpty(product.mfrCode)) {
				if (product.Attributes != null) {
					if (product.i_Attributes_Code.ContainsKey("mfrsku") && product.i_Attributes_Code("mfrsku").Count > 0) {
						if (!product.i_Attributes_Code("mfrsku")(0).Attribute.Translation.text(English).Contains("###")) {
							if (product.isSystem) {
								// Desktop/Notebook
								if (product.ProductType.Code == "DTO" || product.ProductType.Code == "NBK") {
									product.mfrCode = "HPI";
								} else if (!product.ProductType.Code == "WTY" && !product.ProductType.Code == "EDU" && !product.ProductType.Code == "SVC") {
									product.mfrCode = "HPE";
								}
								update = true;
							} else {
								if (product.i_Attributes_Code.ContainsKey("plcode") && product.i_Attributes_Code("plcode").Count > 0) {
									string plCode = product.i_Attributes_Code("plcode")(0).Translation.text(English);
									if (plcodeLookup.ContainsKey(plCode)) {
										product.mfrCode = plcodeLookup(plCode);
										update = true;
									}
								}
							}
						}
					}
				}
			}

			if (update) {
				product.update(errorMessages);
			}

		}

		return "Cleaned product records";

	}

	private string LoadHPServiceLevels(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		iq.ServiceLevelResponse = new Dictionary<int, clsResponse>();
		iq.ServiceLevelServiceType = new Dictionary<int, clsServiceType>();
		iq.ServiceLevels = new Dictionary<int, clsServiceLevel>();
		iq.ServiceLevelTROAA = new Dictionary<int, clsTROAA>();
		iq.ServiceLevelAttributeMap = new Dictionary<string, clsAttribute>();
		iq.CarePackLastRefresh = new Dictionary<string, DateTime>();

		// Response
		r = da.DBExecuteReader(con, "SELECT [ID], [mfrCode], [FK_Translation_Key_Title], [FK_Translation_Key_Description], [ResponseDefault] FROM [Response]");

		while (r.Read) {
			clsResponse response = new clsResponse();





			 // ERROR: Not supported in C#: WithStatement


			iq.ServiceLevelResponse.Add(response.ID, response);

		}
		r.Close();

		// Service Type
		r = da.DBExecuteReader(con, "SELECT [ID], [mfrCode], [FK_Translation_Key_Title], [FK_Translation_Key_Description], [ServiceTypeDefault] FROM [ServiceType]");

		while (r.Read) {
			clsServiceType serviceType = new clsServiceType();





			 // ERROR: Not supported in C#: WithStatement


			iq.ServiceLevelServiceType.Add(serviceType.ID, serviceType);

		}
		r.Close();

		// Service Level Map
		r = da.DBExecuteReader(con, "SELECT [ID], [mfrCode], [ServiceLevel], [ServiceLevelGroup], [SuperGroup], [FK_Translation_Key_Description], [Duration], [PostWarranty], [Disabled], [FK_ServiceType_ID], [FK_Response_ID], [hpeDMR], [hpeCDMR], [hpiADP], [hpiDMR], [hpiTravel], [hpiTracing], [hpiTheft] FROM [ServiceLevelMap]");

		while (r.Read) {
			clsServiceLevel serviceLevel = new clsServiceLevel();






			 // ERROR: Not supported in C#: WithStatement


			iq.ServiceLevels.Add(serviceLevel.ID, serviceLevel);

		}
		r.Close();

		// TROAA
		r = da.DBExecuteReader(con, "SELECT [ID], [SysFamily], [SlotTypeCode], [ServiceLevel], [DisplayOrder], [FK_ServiceLevelMap_ID] FROM [TROAA]");

		while (r.Read) {
			clsTROAA troaa = new clsTROAA();

			 // ERROR: Not supported in C#: WithStatement


			iq.ServiceLevelTROAA.Add(troaa.ID, troaa);

		}
		r.Close();

		// Service Level Attribute Map
		r = da.DBExecuteReader(con, "SELECT [ID], [Code], [FK_Attribute_Code] FROM [ServiceLevelAttributeMap]");

		while (r.Read) {
			string code = r.Item("Code");
			string serviceLevelAttributeCode = r.Item("FK_Attribute_Code");

			if (iq.i_attribute_code.ContainsKey(serviceLevelAttributeCode)) {
				iq.ServiceLevelAttributeMap.Add(code, iq.i_attribute_code(serviceLevelAttributeCode));
			}

		}
		r.Close();


		return "Loaded HP Service Pack Levels";

	}


	public string LoadPoints(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{


		r = da.DBExecuteReader(con, "SELECT ID,fk_Product_id,fk_scheme_id, points FROM [Points]");

		clsProduct product;
		clsScheme scheme;

		int points = 0;
		while (r.Read) {

			if (iq.Products.ContainsKey((int)r.Item("fk_product_id"))) {
				int pid = (int)r.Item("fk_product_id");
				if (iq.Products.ContainsKey(pid)) {
					product = iq.Products(pid);
				} else {
					product = iq.REMAPS(pid);
				}

				scheme = iq.Schemes((int)r.Item("fk_scheme_id"));
				product.Points(scheme) = (int)r.Item("Points");
				points += 1;
			}
		}
		r.Close();

		return "Loaded " + points + " loyalty points onto products";


	}


	public string LoadSchemes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		iq.Schemes.Clear();

		r = da.DBExecuteReader(con, "SELECT ID,code,fk_translation_key_name,StartDate,EndDate,fk_region_id FROM [Scheme]");

		clsScheme ascheme;

		int active = 0;

		int id;
		string code;
		System.DateTime startdate;
		System.DateTime enddate;
		while (r.Read) {
			id = (int)r.Item("Id");
			code = (r.Item("code").ToString());
			startdate = (System.DateTime)r.Item("startdate");
			enddate = (System.DateTime)r.Item("enddate");
			ascheme = new clsScheme(id, code, iq.Translations((int)r.Item("fk_translation_key_name")), iq.Regions((int)r.Item("fk_region_id")), startdate, enddate);
			if (startdate < Now & enddate > Now) {
				active += 1;
			}
		}
		r.Close();

		return "Loaded " + iq.Schemes.Count + " loyalty schemes, " + active + " of which " + active + " are current/active.";


	}

	public string LoadBundles(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{


		//the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems
		r = da.DBExecuteReader(con, "SELECT ID,fk_translation_key_name,OPGref,code,validFrom,validTo,fk_region_id FROM [Bundle]");

		iq.Bundles.Clear();
		iq.i_Bundle_code.Clear();

		//Load the bundles
		clsRegion region = null;
		clsBundle Bundle;

		int Active = 0;
		clsTranslation bundleName = null;
		int tk;
		System.DateTime fromDate;
		System.DateTime toDate;
		int regionID;
		if (r.HasRows) {
			while (r.Read) {
				regionID = (int)r.Item("fk_region_id");
				tk = (int)r.Item("fk_translation_key_name");
				region = iq.Regions(regionID);
				fromDate = (System.DateTime)r.Item("validfrom");
				toDate = (System.DateTime)r.Item("validto");

				bundleName = iq.Translations(tk);
				Bundle = new clsBundle((int)r.Item("ID"), bundleName, r.Item("OPGref").ToString(), r.Item("code").ToString(), region, fromDate, toDate);
				if (Now > Bundle.validFrom & Now < Bundle.validTo)
					Active += 1;
				//count the 'active' bundles
			}
		}
		r.Close();


		//load the items into the bundles 

		r = da.DBExecuteReader(con, "SELECT ID,fk_Bundle_id,fk_product_id,price,rebate,fk_currency_id,qtyMin from BundleItem");

		clsBundleItem bundleitem;
		int bundleItems = 0;
		NullablePrice price;
		int currencyId;
		int bundleID;
		int productID;
		int rowID;
		float rebate;
		int minQty;
		if (r.HasRows) {
			while (r.Read) {
				currencyId = (int)r.Item("fk_currency_id");
				price = new NullablePrice(r.Item("Price"), iq.Currencies(currencyId), false);
				bundleID = (int)r.Item("fk_bundle_id");
				productID = (int)r.Item("fk_product_id");
				rowID = (int)r.Item("ID");
				rebate = (float)r.Item("rebate");
				minQty = (int)r.Item("qtyMin");
				//see the constructor ... it adds the item to the bundle and
				Bundle = iq.Bundles(bundleID);
				bundleitem = new clsBundleItem(rowID, Bundle, iq.Products(productID), price, rebate, minQty);
				bundleItems += 1;
			}
		}
		r.Close();


		//Attach the bundles to the systems

		r = da.DBExecuteReader(con, "SELECT ID,fk_Bundle_id,fk_product_id_system,rebate from BundleSystem");

		clsBundleSystem bundlesystem;
		int bundleSystems = 0;
		clsProduct system;

		if (r.HasRows) {
			while (r.Read) {
				bundleID = (int)r.Item("fk_bundle_id");
				productID = (int)r.Item("fk_product_id_system");
				rowID = (int)r.Item("ID");
				Bundle = iq.Bundles(bundleID);
				system = iq.Products(productID);
				rebate = (float)r.Item("rebate");
				bundlesystem = new clsBundleSystem(rowID, Bundle, system, rebate);
				bundleSystems += 1;

			}
		}
		r.Close();

		return "Loaded " + iq.Bundles.Count + " bundles, " + Active + " of which are current/active, applied to" + bundleSystems + " systems ,There were a total of " + bundleItems + " bundle items.";

	}



	public string LoadFlex(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems

		r = da.DBExecuteReader(con, "SELECT ID,description,OPGref,validFrom,validTo,fk_currency_id,minOptions,maxOptions,OPGSysType  FROM [Flex]");

		iq.FlexOPGs.Clear();

		//Load the FlexOPGs

		int active = 0;
		clsCurrency currency;
		clsFlexOPG flexOPG;
		int currencyID;
		System.DateTime fromDate;
		System.DateTime toDate;
		int minOptions;
		int maxOptions;
		string OPGSysType;
		int rowID;
		if (r.HasRows) {
			while (r.Read) {
				currencyID = (int)r.Item("fk_currency_id");
				fromDate = (System.DateTime)r.Item("validfrom");
				toDate = (System.DateTime)r.Item("validto");
				currency = iq.Currencies(currencyID);
				minOptions = (int)r.Item("MinOptions");
				maxOptions = IsDBNull(r.Item("MaxOptions")) ? null : (int)r.Item("MaxOptions");
				OPGSysType = IsDBNull(r.Item("OPGSysType")) ? "" : (string)r.Item("OPGSysType");

				rowID = (int)r.Item("ID");
				flexOPG = new clsFlexOPG(rowID, r.Item("OPGref").ToString(), r.Item("Description").ToString(), fromDate, toDate, currency, minOptions, maxOptions, OPGSysType);
				if (flexOPG.isCurrent)
					active += 1;
				//count the 'active' bundles            
			}
		}
		r.Close();

		return "Loaded " + iq.FlexOPGs.Count + " Flex OPGs, " + active + " of which are current/active";

	}

	public string LoadFlexRegions(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems

		r = da.DBExecuteReader(con, "SELECT ID,fk_flex_id,fk_region_id FROM [FlexRegion]");

		clsFlexOPG flexOPG;
		int frs = 0;
		clsRegion region;
		int flexID;
		int regionID;
		if (r.HasRows) {
			while (r.Read) {
				flexID = (int)r.Item("fk_flex_id");
				regionID = (int)r.Item("fk_region_id");
				flexOPG = iq.FlexOPGs(flexID);
				region = iq.Regions(regionID);
				flexOPG.regions.Add(region.ID, region);
				frs += 1;
			}
		}
		r.Close();

		return "Loaded " + frs + " Flex Regions ";

	}


	public string LoadFlexLines(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{
		//load the lines into the FlexOPGs

		r = da.DBExecuteReader(con, "SELECT ID,fk_flex_id,fk_product_id,rebate,validfrom,validto FROM FlexLine");
		//- Rebate = Additionl disc% * list - do at import !!

		clsFlexLine flexline;

		int lines = 0;
		int activelines = 0;
		int flexID;
		int pID;
		decimal rebate;
		System.DateTime fromDate;
		System.DateTime toDate;
		int rowID;
		clsProduct product;

		if (r.HasRows) {
			while (r.Read) {
				flexID = (int)r.Item("fk_flex_id");
				pID = (int)r.Item("fk_product_id");
				rebate = (decimal)r.Item("rebate");
				fromDate = (System.DateTime)r.Item("validfrom");
				toDate = (System.DateTime)r.Item("validto");
				rowID = (int)r.Item("ID");

				//                If FlexOPGs(flexID).OPGRef = "90989236" And iq.Products(productID).sku = "712317-421" Then Stop

				//  price = New nullablePrice(r.Item("Price"), iq.Currencies(r.Item("fk_currency_id")))

				if (iq.Products.ContainsKey(pID)) {
					product = iq.Products(pID);
				} else {
					product = iq.REMAPS(pID);
				}

				flexline = new clsFlexLine(rowID, iq.FlexOPGs(flexID), product, rebate, fromDate, toDate);

				lines += 1;
				if (flexline.isCurrent)
					activelines += 1;
			}
		}

		r.Close();

		return "There were a total of " + lines + " Flex (product) lines, " + activelines + " of which are current/active.";
	}
	public string LoadFlexRules(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//   Dim rRule As SqlDataReader = da.DBExecuteReader(con, "SELECT [ID],[FK_ProductType_ID],[FK_Flex_ID],[min],[max] FROM [FlexRule]")
		r = da.DBExecuteReader(con, "SELECT [ID],[FK_ProductType_ID],[FK_Flex_ID],[min],[max],[optionalRule] FROM [FlexRule]");
		int lines = 0;
		clsFlexRule flexRules;
		int rowID;
		int productTypeID;
		int flexID;
		int min;
		int max;
		bool optionalRule;
		if (r.HasRows) {
			while (r.Read) {
				rowID = (int)r("ID");
				productTypeID = (int)r("FK_ProductType_ID");
				flexID = (int)r("FK_Flex_ID");
				min = (int)r("min");
				max = (int)r("max");
				optionalRule = (bool)r("optionalRule");
				flexRules = new clsFlexRule(rowID, iq.FlexOPGs(flexID), iq.ProductTypes(productTypeID), min, max, optionalRule);
				lines += 1;
			}
		}
		r.Close();
		return "There were a total of " + lines + " Flex (product) Rules ";
	}

	public string LoadAvalancheOPGs(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//Avalanche is an HP discount scheme wich provides tiered discounts (typically 5% and 10%) on options (HDDs, Memory, cables, etc) when certain quantity thresholds are met
		//the discount (rebate) on  options is a percentage of it's *list* price
		//you can 'mix and match' options - each avalanche offer applies to one or more systems, and in one or more regions 

		//the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems

		r = da.DBExecuteReader(con, "SELECT ID,OPGref,optMin,optMax,validFrom,validTo,fk_region_id FROM AvalancheOPG");

		iq.AvalancheOPGs.Clear();
		iq.i_OpgRef.Clear();

		clsRegion region = null;
		ClsAvalancheOPG AvalancheOPG;
		int regionID;
		System.DateTime fromDate;
		System.DateTime toDate;
		int rowID;
		string opgRef;
		int optMin;
		int optMax;
		if (r.HasRows) {
			while (r.Read) {
				regionID = (int)r.Item("fk_region_id");
				fromDate = (System.DateTime)r.Item("validfrom");
				toDate = (System.DateTime)r.Item("validto");
				region = iq.Regions(regionID);
				rowID = (int)r.Item("ID");
				opgRef = r.Item("opgref").ToString();
				optMin = (int)r.Item("optmin");
				optMax = (int)r.Item("optmax");
				if (!iq.i_OpgRef.ContainsKey(opgRef)) {
					AvalancheOPG = new ClsAvalancheOPG(rowID, opgRef, region, fromDate, toDate, optMin, optMax);
				} else {
					Beep();
				}
			}
		}
		r.Close();

		//clear them all (as re have to re-load them during import)
		foreach ( product in iq.Products.Values) {
			if (product.AvalancheOPGs != null) {
				product.AvalancheOPGs.Clear();
			}

		}

		r = da.DBExecuteReader(con, "SELECT fk_product_id_system,fk_avalancheOPG_id FROM AvalancheSystem");
		int attached = 0;
		int productID;
		int avalancheID;

		if (r.HasRows) {
			while (r.Read) {
				productID = (int)r.Item("fk_product_id_system");
				avalancheID = (int)r.Item("fk_avalancheOPG_id");

				iq.Products(productID).AvalancheOPGs.Add(avalancheID, iq.AvalancheOPGs(avalancheID));
				attached += 1;
			}
		}
		r.Close();


		//now read the AvalancheOptions - which gives us the percent (of list) discount per 'ref code', under this OPG  (options have a refcode attribute)
		clsAvalancheOption opt;
		int opts;

		r = da.DBExecuteReader(con, "SELECT ID,FK_AvalancheOPG_id,LPDiscountPercent,prodRef FROM avalancheOption");
		if (r.HasRows) {
			while (r.Read) {
				avalancheID = (int)r.Item("fk_avalancheOPG_id");
				rowID = (int)r.Item("ID");
				//creating the Avalance option adds it to the OPG (those OPG's have already been attached to the relevant systems
				opt = new clsAvalancheOption(rowID, iq.AvalancheOPGs(avalancheID), r.Item("Prodref").ToString(), (float)r.Item("LPDiscountPercent"));
				opts += 1;
			}
		}
		r.Close();

		return "Loaded " + iq.AvalancheOPGs.Count + " Avalanche offers and attached them to " + attached + " systems, Loaded discounts for " + opts + " Refcodes ";

	}

	private string LoadSectors(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key_name from [Sector]");

		int count;

		clsSector aSector;

		if (r.HasRows) {
			while (r.Read) {
				aSector = new clsSector((int)r.Item("id"), (string)r.Item("code"), iq.Translations((int)r.Item("fk_translation_key_name")));
				count += 1;
			}
		}
		r.Close();

		return "Loaded " + count + " sectors (Business Units) ";

	}
	private string LoadProducts(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT id,sku,IsSystem,IsOption,fk_producttype_id,fk_sector_id,activeFrom,activeTo,active,eol,publish,mfrCode,buCode,PLcode FROM Product where deleted = 0 order by id");

		iq.Products = new Dictionary<int, clsProduct>();

		clsProductType ProductType;

		int dupes = 0;

		List<string> todel = new List<string>();

		if (r.HasRows) {
			while (r.Read) {
				if (IsDBNull(r.Item("FK_ProductType_ID"))) {
					ProductType = null;
				} else {
					ProductType = iq.ProductTypes((int)r.Item("FK_ProductType_ID"));
				}
				//this should not be needed ! - remove !
				object sku = r("sku");

				//If sku$ = "QK765A" Then Stop

				if (sku != "" && iq.i_SKU.ContainsKey(sku)) {
					//DELETE THIS PRODUCT AND REMAP TO THE FIRST INSTANCE
					//Stop
					REMAPS.Add(r.Item("ID"), i_SKU(sku));
					todel.Add(r.Item("id"));
					dupes += 1;
				} else {
					clsProduct product = new clsProduct((int)r("id"), sku, (bool)r("isSystem"), (bool)r("isOption"), iq.Sectors((int)r("fk_sector_id")), ProductType, (System.DateTime)r("activefrom"), (System.DateTime)r("activeto"), (bool)r("active"), (bool)r("EOL"),
					(bool)r("Publish"), (string)r("mfrCode"), (string)r("buCode"), (string)r.Item("plCode"));

					iq.Products.Add((int)r("id"), product);
				}

				//Else
				//duplicated product !
				// Beep()
				// dupes = +1
				// End If

			}
		}
		r.Close();


		if (todel.Count) {
			//soft delete the 2nd and subsequent versions of the product
			object sql = "update product set deleted =1 where id in (" + Join(todel.ToArray, ",") + ")";

			da.DBExecutesql(sql);


		}



		return "Loaded " + iq.Products.Count + " products";

	}


	public string LoadTranslation(int Key)
	{

		//used to load a single translation ("worldwide") - as part of the bootstrap process

		SqlClient.SqlDataReader r;
		SqlClient.SqlConnection con = da.OpenDatabase;

		r = da.DBExecuteReader(con, "Select id,[key],[text],fk_language_id,[group],[order] from translation where key=" + Key);

		int id = 0;
		clsLanguage Lang = null;
		string Text;
		string @group;
		int order;

		clsTranslation aTranslation;


		if (r.HasRows) {
			while (r.Read) {
				id = (int)r.Item("id");
				Key = (int)r.Item("key");
				Lang = iq.Languages((int)r.Item("fk_language_id"));
				Text = r.Item("text").ToString();
				@group = r.Item("group").ToString();
				order = (int)r.Item("order");


				//will also add it to the translations(key)(lang) dictionary
				if (iq.Translations.ContainsKey(Key)) {
					iq.Translations(Key).addLanguage(Lang, Text, null);

				} else {
					aTranslation = new clsTranslation(Key, Lang, Text, id, @group, order);
				}
			}
		}
		r.Close();

	}

	public string LoadTranslations(SqlClient.SqlConnection con)
	{

		SqlClient.SqlDataReader r;

		r = da.DBExecuteReader(con, "SELECT id,[key],[text],fk_language_id,[group],[order] FROM translation where deleted=0");
		// where deleted=0
		int id = 0;
		clsLanguage Lang = null;
		int Key = 0;
		string Text;
		string @group;
		int order;

		clsTranslation aTranslation;

		iq.Translations.Clear();
		iq.iEnglishIndex.Clear();
		iq.KYIndex.Clear();


		if (r.HasRows) {
			clsTranslation tl;

			while (r.Read) {
				id = (int)r.Item("id");
				Key = (int)r.Item("key");

				Lang = iq.Languages((int)r.Item("fk_language_id"));
				if (!Lang.Active)
					Lang.Active = true;
				Text = r.Item("text").ToString();
				@group = r.Item("group").ToString();
				order = (int)r.Item("order");

				//each translation object exposes:-
				//Property Text As Dictionary(Of clsLanguage, String)
				//Property ID As Dictionary(Of clsLanguage, Integer)

				//will also add it to the translations(key)(lang) dictionary
				if (!iq.Translations.ContainsKey(Key)) {
					tl = new clsTranslation(Key, Lang, Text, id, @group, order);
				} else {
					//                    iq
					tl = iq.Translations(Key);
					tl.addLanguage(Lang, id, Text);
				}
			}
		}
		r.Close();

		//for performance- and cleaner code - in particular, cellUI needs these ALOT

		Yes = iq.AddTranslation("Yes", English, "UI", 0, null, 0, false);
		No = iq.AddTranslation("No", English, "UI", 0, null, 0, false);
		InStock = iq.AddTranslation("in stock", English, "UI", 0, null, 0, false);
		OutOfStock = iq.AddTranslation("out of stock", English, "UI", 0, null, 0, false);
		return "Loaded " + iq.Translations.Count + " translations";

	}
	private string LoadUnits(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "Select [id],[code],[symbol],fk_translation_key_name,FK_Measure_ID from [unit]");
		clsUnit u;
		if (r.HasRows) {
			while (r.Read) {
				//becuase we're specifying the ID here - it won't autmatically be written to the database

				string code = r.Item("code");
				if (!i_unit_code.ContainsKey(code)) {
					// u = New clsUnit(CInt(r.Item("ID")), code, iq.Translations(CInt(r.Item("fk_translation_key_name"))), r.Item("Symbol").ToString())
					u = new clsUnit((int)r.Item("ID"), Trim(r.Item("Code").ToString()), iq.Translations((int)r.Item("fk_translation_key_name")), r.Item("Symbol").ToString(), (int)r.Item("FK_Measure_ID"));
				} else {
					errorMessages.Add(code + " is duplicated in the Units table");
				}

			}
		}
		r.Close();

		return "Loaded " + iq.Units.Count + " units";

	}

	private string LoadLanguages(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{
		r = da.DBExecuteReader(con, "Select distinct id,code,localname,rtl,live,active from [language]");
		clsLanguage l;
		string code;
		if (r.HasRows) {
			while (r.Read) {
				code = Trim(r.Item("code").ToString());
				//The act of creating the language - adds it to the master list (see the clsLanguage constructor)
				//becuase we're specifying the ID here - it won't autmatically be written to the database
				if (!iq.i_language_Code.ContainsKey(code)) {
					l = new clsLanguage((int)r.Item("ID"), code, r.Item("Localname").ToString(), (bool)r.Item("rtl"), (bool)r.Item("live"), (bool)r.Item("active"));
				}
			}
		}
		r.Close();
		if (!iq.i_language_Code.ContainsKey("KY")) {
			l = new clsLanguage("KY", "UI Key", false, true, true);

		}
		KYlanguage = iq.i_language_Code("KY");
		return "Loaded " + iq.Languages.Count + " languages " + "<br/>";

	}

	private string LoadExcludes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//we have to load the excludes *after* we'v loaded all the branches as we need to walk the descendants

		r = da.DBExecuteReader(con, "SELECT [id],FK_Branch_ID_Having,FK_Branch_ID_Excludes,reason  FROM [exclude]");
		clsExclude ex;

		if (r.HasRows) {
			while (r.Read) {
				ex = new clsExclude((int)r.Item("id"), iq.Branches((int)r.Item("fk_branch_id_having")), iq.Branches((int)r.Item("fk_branch_id_excludes")), r.Item("reason").ToString());
			}
		}
		r.Close();

		return "Loaded " + Excludes.Count + " Excludes.";

	}
	private string LoadValidationsInclusions(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{
		ValidationInclusions = new Dictionary<int, clsValidationInclusion>();
		r = da.DBExecuteReader(con, "SELECT [id],MajorSlotType,MinorSlotType,InclusionType FROM [validationinclusion]");

		if (r.HasRows) {
			while (r.Read) {
				object ex = new clsValidationInclusion((int)r.Item("id"), r.Item("MajorSlotType"), IsDBNull(r.Item("MinorSlotType")) ? null : r.Item("MinorSlotType"), Enum.Parse(typeof(enumInclusionType), r.Item("inclusionType").ToString));
			}

		}
		r.Close();

		return "Loaded " + Excludes.Count + " Excludes.";
	}

	private string LoadSpecialBranches(SqlClient.SqlConnection con, SqlClient.SqlDataReader r, List<string> errormessages)
	{

		//CPQ branch
		r = da.DBExecuteReader(con, "SELECT [id],FK_Branch_ID,Code FROM SpecialBranch");

		if (r.HasRows) {
			while (r.Read) {
				iq.i_SpecialBranches.Add(r("code"), iq.Branches((int)r("fk_branch_id")));
			}
		}
		return "Special Branches Loaded";

	}


	private string LoadBranches(SqlClient.SqlConnection con, SqlClient.SqlDataReader r, List<string> errormessages)
	{

		//read ALL the branches in before attempting to create the structure (by grafting)

		object sql = "SELECT [id],[FK_branch_id_parent],[fk_product_id],[fk_translation_key],[picture],[fk_translation_key_collective],[fk_translation_key_collectiveSingular],fk_screen_id_matrix,[order],hidden,locked,rca  ";
		sql += "FROM [branch] WHERE deleted = 0 ORDER BY id";

		// Dim sql$  'Only load branches with live products
		// sql$ = "SELECT b.[id],[FK_branch_id_parent],[fk_product_id],[fk_translation_key],[picture],[fk_translation_key_collective],[fk_translation_key_collectiveSingular],"
		// sql$ &= "fk_screen_id_matrix, [order], hidden, locked, rca "
		// sql$ &= "FROM [branch] b left join product p on b.fk_product_id=p.id WHERE b.deleted=0 and p.deleted = 0 and p.active=1 and p.publish=1 ORDER BY b.id"

		r = da.DBExecuteReader(con, sql);
		clsBranch b;
		clsProduct product;
		clsBranch parent = null;
		int parentBranchId;

		StreamWriter sw = new StreamWriter("c:\\temp\\badbranches.txt");

		List<clsBranch> toDel__1 = new List<clsBranch>();
		HashSet<int> toUpdate = new HashSet<int>();
		//this is for remapping/deduping

		int mps;
		int rmps = 0;
		int neps = 0;

		int orphaned = 0;
		if (r.HasRows) {

			while (r.Read) {
				product = null;
				if (!IsDBNull(r.Item("fk_product_id"))) {
					int pid = r.Item("fk_product_id");

					//this product has been removed/deleted
					if (iq.Products.ContainsKey(pid)) {
						product = iq.Products(pid);
					} else {
						//do we have a rempap for this (duplicate) prodcut ..
						if (REMAPS.ContainsKey(pid)) {
							product = iq.REMAPS(pid);
							rmps += 1;
							toUpdate.Add(r.Item("id"));
							//add the BRANCH to a list of branches we will need to update in the db becuase their product has been rempapped (once we've set their parent)
						// sw.WriteLine("Branch " & r.Item("id") & " " & iq.Translations(r.Item("fk_translation_key")).text(English) & " references non existent (or deleted) product " & pid)

						} else {
							//oh dear - really broken (foreign key to a completely non existen product (but RI should never have allowed this ?)
							//    Beep()
							neps += 1;
						}
					}
				}

				clsScreen matrix = null;
				if (!IsDBNull(r.Item("fk_screen_id_matrix"))) {
					matrix = iq.Screens((int)r.Item("fk_screen_id_matrix"));
				}

				//we don't set the parents yet - we need them ALL in first
				b = new clsBranch((int)r.Item("ID"), product, null, iq.Translations((int)r.Item("fk_translation_key")), r.Item("picture").ToString(), iq.Translations((int)r.Item("fk_translation_key_collective")), iq.Translations((int)r.Item("fk_Translation_key_collectiveSingular")), matrix, (int)r.Item("order"), (bool)r.Item("hidden"),
				(bool)r.Item("locked"), (string)r.Item("rca"));

			}
			r = da.DBExecuteReader(con, sql);
			while (r.Read) {

				if (IsDBNull(r.Item("fk_branch_id_parent"))) {
					parent = null;
					// There are also some branches with a null parent - these are 'floaters' and (generally) only appear in the tree as grafts

				} else if ((int)r.Item("id") == (int)r.Item("fk_branch_id_parent")) {
					parent = RootBranch;
					//any branch which is it's own parent - is added directly under the root level - this is a bit of a catchall/saftey net
				} else {
					//THIS SHOULD NOT BE HERE - something is wrong - some parents are coming after their children
					if (iq.Branches.ContainsKey((int)r.Item("fk_branch_id_parent"))) {
						parent = iq.Branches((int)r.Item("fk_branch_id_parent"));
					} else {
						orphaned += 1;
						//Dim exm As String = "- Detail should be here."
						//Try
						//    exm = iq.Translations(r.Item("fk_translation_key")).text(English) & "(" & r.Item("ID").ToString & ")"
						//    exm &= " as it's parent " & r.Item("fk_branch_id_parent").ToString & " was not (yet) present"
						//Catch ex2 As Exception

						//End Try

						//Dim ex As Exception = New Exception("Could not load branch " & exm)
						//Throw ex
					}
				}

				clsBranch branch = iq.Branches((int)r.Item("id"));
				branch.SetParent(parent);
				if (object.ReferenceEquals(branch, parent) & branch.ID != 1)
					System.Diagnostics.Debugger.Break();
				//a branch cannot be its own parent


				//there are 6,000 plus of these need sorting out
				//     If branch.HasSiblingWithSameProduct Then todel.Add(branch)


			}
		}

		//  Debug.Print(neps)

		sw.Close();

		r.Close();


		//update the FK_Product_id based on the mappings to fix any duplicate
		List<string> erromessages = new List<string>();
		foreach ( bid in toUpdate) {
			iq.Branches(bid).Update(errormessages);
		}



		string graftinfo;
		graftinfo = LoadGrafts(con, errormessages);

		string pruneInfo;
		pruneInfo = loadPrunes(con, errormessages);

		object l;
		l = "Loaded " + iq.Branches.Count + " branches, " + graftinfo + "," + pruneInfo + " " + neps + " missing/deleted products. " + rmps + " products are duped (and being remapped)";

		Debug.Print(todel.Count);


		//Dim sw As StreamWriter = New StreamWriter("c:\temp\dupeProds.txt")
		//Dim qtys As Integer = 0
		//For Each branch In todel
		//    sw.WriteLine(branch.ID & " " & branch.EnglishName)
		//    If branch.Product IsNot Nothing Then
		//        sw.WriteLine(branch.Product.SKU())
		//    End If

		//    For Each q In branch.Quantities
		//        qtys += 1
		//    Next

		//Next

		//sw.Close()


		return l;

	}
	public string loadPrunes(SqlClient.SqlConnection con, List<string> errormessages)
	{


		//Prune paths should be distinct - this is 'self healing' code to fix up mess in UAT/Production and dev databases (without having to run scripts all over the place)

		HashSet<string> distinctPaths = new HashSet<string>();
		HashSet<string> toDel = new HashSet<string>();

		int Prunes = 0;
		SqlClient.SqlDataReader r;
		r = da.DBExecuteReader(con, "SELECT id,Path,fk_channel_id,source,created FROM Prune");

		clsPrune aPrune;
		if (r.HasRows) {
			while (r.Read) {
				//use iq.channel(.Item("fk_channel_id"))) here to impliment scoped prunes
				if (distinctPaths.Contains(r.Item("path"))) {
					//duplicate prune (store the Prunes ID for deletion
					toDel.Add(r.Item("id").ToString);
				} else {
					if (!iq.Branches.ContainsKey(Split(r.Item("Path"), ".").Last)) {
					//       toDel.Add(r.Item("id").ToString)  'prune of a non-existent branch
					} else {
						distinctPaths.Add(r.Item("path"));
						aPrune = new clsPrune((int)r.Item("ID"), r.Item("path").ToString(), new NullableInt(), r.Item("source").ToString(), (System.DateTime)r.Item("created"));
						//iq.Branches(Split(r.Item("path"), ".").Last).Prunes.Add(r.Item("path"))
						Prunes += 1;
					}
				}
			}
		}
		r.Close();


		if (toDel.Count) {
			int toskip = 0;
			int chunk = 1000;
			do {
				object l = from j in toDel.Skip(toskip).Take(chunk);
				if (!l.Any)
					break; // TODO: might not be correct. Was : Exit Do
				object sql = "DELETE FROM PRUNE WHERE ID IN(" + Join(l.ToArray, ",") + ")";

				LongSQL(sql);
				toskip += 1000;
			} while (true);

		}

		return "Loaded " + Prunes + " Prunes, (Deleted " + toDel.Count + ")";


	}



	public string LoadGrafts(SqlClient.SqlConnection con, ref List<string> errorMessages)
	{

		SqlClient.SqlDataReader r;
		clsBranch source;
		clsBranch Target;

		//This is run tiem 'fixup' code - could be removed once we establish what is creating the dupes !
		int dupes = 0;
		HashSet<string> todel = new HashSet<string>();

		//load the grafts - parts of the tree are re-used within itself - for example Operating Systems appear under *many* servers
		int grafts = 0;

		r = da.DBExecuteReader(con, "SELECT id,fk_branch_id_source, fk_branch_id_target,path FROM graft");
		object path;
		if (r.HasRows) {
			while (r.Read) {
				//here's where the magic happens

				int sourceid = (int)r.Item("fk_branch_id_source");
				//Soft deleted branches are not loaded - so will not be 'there 'to graft
				if (iq.Branches.ContainsKey(sourceid)) {

					source = iq.Branches(sourceid);
					//the target may have been soft deleted
					if (iq.Branches.ContainsKey((int)r.Item("fk_branch_id_target"))) {
						Target = iq.Branches((int)r.Item("fk_branch_id_target"));
						path = (string)r.Item("Path");
						//                    '*some* (very few) grafts have a path - ie. they are only active at one specific point in the tree (CPUs are a case in point)


						if (path != "") {
							source.GraftedOnAt.Add(path);
						}

						if (!source.AllParents.ContainsKey(Target.ID))
							source.AllParents.Add(Target.ID, Target);


						if (!Target.childBranches.ContainsKey(source.ID)) {
							Target.childBranches.Add(source.ID, source);
							//note - this does NOT set the branches parent - branches are grafted in many places and so do not have one parent - their parent property is used only by the editor within a single graft
							Target.HasGrafts = true;
							grafts += 1;
							//Else
							//    'Beep() 'Duplicate graft
							//    errorMessages.Add("Graft " & r.Item("ID").ToString & " is a duplicate")
							//    dupes += 1
							//    todel.Add(r.Item("id"))

						}
					}
				}
			}
		}
		r.Close();

		//If todel.Count Then

		//    Dim toskip As Integer = 0
		//    Dim chunk As Integer = 1000
		//    Do
		//        Dim l = From j In todel.Skip(toskip).Take(chunk)
		//        If Not l.Any Then Exit Do
		//        Dim sql$ = "DELETE FROM graft WHERE ID IN(" & Join(l.ToArray, ",") & ")"

		//        LongSQL(sql)
		//        toskip += 1000
		//    Loop

		//End If

		return "Loaded " + grafts + " grafts (Deleted " + todel.Count + ")";

	}
	private string LoadSlots(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "Select [id],path,fk_branch_id,fk_slottype_id,numslots,fk_translation_key_notes,slotnum,requiredfill,advisedfill from [slot] where deleted =0");

		//Some duplicate slots have crept in (JIT carepacks - but not just that) - this is an on-the-fly fix (which can be removed in due course)
		List<string> todel = new List<string>();

		clsSlot s;
		int slots;

		int ms = 0;

		if (r.HasRows) {
			while (r.Read) {
				//the act of making the slot adds it to its branch (so we don't need to)
				clsTranslation NOTES = null;
				if (object.ReferenceEquals(r.Item("FK_TRANSLATION_KEY_NOTES"), DBNull.Value)) {
					NOTES = null;
				} else {
					NOTES = iq.Translations((int)r.Item("FK_TRANSLATION_KEY_NOTES"));
				}

				int branchid = (int)r.Item("fk_branch_id");
				object path = r.Item("path").ToString;
				int slotid = (int)r.Item("id");

				//Heal' some malformed path (duplicate last segments)
				bool skip = false;
				if (path != "") {
					string[] bits = Split(path, ".");
					if (UBound(bits) > 2 && bits(UBound(bits)) == bits(UBound(bits) - 1)) {
						todel.Add(slotid);
						skip = true;
						ms += 1;
					}

					if ((int)bits.Last != branchid) {
						todel.Add(slotid);
					}
				}

				if (!skip) {
					//soft deleted branches are not loaded - so we can't attach the slots
					if (iq.Branches.ContainsKey(branchid)) {
						int stid = (int)r.Item("fk_slottype_id");
						clsSlotType slottype = iq.SlotTypes(stid);
						s = new clsSlot(slotid, slottype, iq.Branches(branchid), path, (int)r.Item("numslots"), NOTES, new NullableInt(r.Item("slotnum")), (int)r.Item("requiredFill"), (int)r.Item("advisedFill"));

						//part of the on-the-fly dupes fix (see alos Ctor above)
						if (s.ID == -1) {
							todel.Add(slotid);
						} else {
							slots += 1;
						}
					}
				}
			}
		}
		r.Close();

		//dupes fix (note the dupes aren't actually loaded - and they won't exist next time)

		if (todel.Count > 0) {
			int toskip = 0;
			int chunk = 1000;
			do {
				object l = from j in todel.Skip(toskip).Take(chunk);
				if (!l.Any)
					break; // TODO: might not be correct. Was : Exit Do
				object sql = "DELETE FROM SLOT WHERE ID IN(" + Join(l.ToArray, ",") + ")";

				LongSQL(sql);
				toskip += 1000;
			} while (true);
		}
		Debug.Print(ms);
		return "Loaded " + slots + " Slot Give/Takes - (deleted " + todel.Count + ")";

	}
	private string LoadSlotTypes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//Slottypes include PCI (full and half length slots, drive bays, fan bays and anything else with finite capacity that can affect the availability of options

		object sql;
		sql = "SELECT id,fk_translation_key,fk_translation_key_short,majorcode,minorCode,EnforceMinorCode ";
		sql += "FROM slottype order by id";

		r = da.DBExecuteReader(con, sql);

		int Count = 0;

		clsSlotType aSlotType;

		List<string> todel = new List<string>();

		Dictionary<int, int> mappings = new Dictionary<int, int>();
		//DeDupe -remap to fix.7c data corrupted by the incremental import

		if (r.HasRows) {
			while (r.Read) {
				if (!IsDBNull(r("fk_translation_key_short"))) {
					object a = 9;
				}

				clsTranslation tl = iq.Translations((int)r.Item("fk_translation_key"));
				clsTranslation tls = null;
				if (!object.ReferenceEquals(r.Item("fk_translation_key_short"), DBNull.Value)) {
					int ti = r.Item("fk_translation_key_short");
					tls = iq.Translations(ti);
				}

				string mjc = r.Item("majorcode");
				string mnc = r.Item("minorcode");

				if (iq.i_slotType_Code.ContainsKey(mjc)) {
					if (iq.i_slotType_Code(mjc).ContainsKey(mnc)) {
						clsSlotType mst = iq.i_slotType_Code(mjc)(mnc);
						//master slot type (the one we're keeping!)
						//this is a dupe
						todel.Add(r.Item("id"));
						mappings.Add(r.Item("id").ToString, mst.ID);
					}
				}

				aSlotType = new clsSlotType((int)r.Item("id"), mjc, mnc, tl, tls, (bool)r.Item("EnforceMinorCode"));
				Count += 1;
			}
		}
		r.Close();


		//sweep/remap the alt slot types

		List<string> sqls = new List<string>();
		r = da.DBExecuteReader(con, "select ID,FK_Slottype_ID,fk_slottype_id_alternative from AltSlotType");
		while (r.Read) {

			if (mappings.ContainsKey(r.Item("fk_slotType_id")) | mappings.ContainsKey(r.Item("fk_slottype_id_alternative"))) {
				int m1 = r.Item("fk_slottype_id");
				int m2 = r.Item("fk_slottype_id_alternative");
				if (mappings.ContainsKey(m1))
					m1 = mappings(m1);
				if (mappings.ContainsKey(m2))
					m2 = mappings(m2);


				sqls.Add("Update altSlotType set fk_slotType_id=" + m1 + ",fk_slottype_id_alternative=" + m2 + " where id = " + r.Item("id"));
			}
		}
		r.Close();

		foreach ( u in sqls) {
			da.DBExecuteReader(con, u);
		}

		///sweep


		if (todel.Count) {
			int toskip = 0;
			int chunk = 1000;
			do {
				object ll = from j in todel.Skip(toskip).Take(chunk);
				if (!ll.Any)
					break; // TODO: might not be correct. Was : Exit Do

				sql = "Delete from slottype WHERE [id] IN(" + Join(ll.ToArray, ",") + ")";

				LongSQL(sql);
				toskip += 1000;
			} while (true);

		}


		//read in the alternative slot types (and their relative priorties) for each primary slot type

		sql = "SELECT fk_slottype_id as [primary],fk_slottype_id_alternative as alt,priority ";
		sql += "FROM altSlotType";

		int fallBacks = 0;
		r = da.DBExecuteReader(con, sql);

		if (r.HasRows) {
			while (r.Read) {
				iq.SlotTypes((int)r.Item("primary")).Fallback.Add((int)r.Item("priority"), iq.SlotTypes((int)r.Item("alt")));
				fallBacks += 1;
			}
		}
		r.Close();




		return "Loaded " + Count + " SlotTypes and " + fallBacks + " fallbacks" + " (deleted " + todel.Count + ")";

	}
	private string LoadProductTypes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r, List<string> errormessages)
	{

		//ProductTypes were OptTypes before - but now include SVR,DTO,NBK,SWD etc... they are what margins are based on - with Sectors 

		object sql;
		sql = "SELECT id,fk_translation_key_text,code,[order] FROM ProductType";

		r = da.DBExecuteReader(con, sql);

		int Count = 0;
		clsProductType aProductType;

		if (r.HasRows) {
			while (r.Read) {
				if (i_ProductType_Code.ContainsKey(r.Item("code"))) {
					errormessages.Add("Product type code " + r.Item("code") + " is duplicated !");
				} else {
					aProductType = new clsProductType((int)r.Item("id"), r.Item("code").ToString, iq.Translations((int)r.Item("fk_translation_key_text")), (short)r.Item("order"));
					Count += 1;
				}
			}
		}
		r.Close();

		return "Loaded " + Count + " Product Types (formerly OptTypes)";

	}

	private string LoadQuantities(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		object sql;
		sql = "SELECT q.id,[path], fk_region_id,fk_branch_id, preinstalled,minIncrement,preferredIncrement,foc FROM quantity q";
		sql += " INNER JOIN Branch b ON FK_Branch_ID = b.id inner join Product p on b.FK_Product_ID = p.id\t";
		sql += " WHERE q.deleted = 0 AND p.deleted = 0";

		r = da.DBExecuteReader(con, sql);

		int Count = 0;

		clsBranch branch;
		clsQuantity aQuantity;
		int qid;
		int numPreinstalled;
		int prefincr;

		HashSet<string> h = new HashSet<string>();
		//On the fly fix for duplication created by the incremental import
		List<string> toDel = new List<string>();
		//ids of the the quantities to delete

		if (r.HasRows) {

			while (r.Read) {
				qid = (int)r.Item("ID");

				int branchID = (int)r.Item("fk_branch_id");
				//some branches are soft deleted (and not loaded)
				if (iq.Branches.ContainsKey(branchID)) {
					branch = iq.Branches(branchID);
					numPreinstalled = (int)r.Item("preInstalled");
					object path = r.Item("path");
					bool foc = (bool)r.Item("foc");
					clsRegion rgn = iq.Regions((int)r.Item("fk_region_id"));
					object minIncr = (int)r.Item("MinIncrement");
					prefincr = (int)r.Item("PreferredIncrement");

					aQuantity = new clsQuantity(qid, rgn, r.Item("Path").ToString(), branch, numPreinstalled, minIncr, prefincr, foc);

					//Remove line above - and reinstate below for 'self healing' quanitites
					//Dim ck$ = branch.ID & "_" & path$ & "_" & rgn.Code & "_" & foc.ToString & "_" & minIncr

					//If Not h.Contains(ck$) Then
					//    'creating the quantity - adds it to the branch - the branch is may (have already been) grafted (in many places) - where the quantity limits will apply if there is a scope match (by Path)

					//    If branch.Product IsNot Nothing AndAlso ("tap,swd".Contains(branch.Product.ProductType.Code.ToLower) And numPreinstalled = 1) Then
					//        'dodgy fix for preisntalled tape drives (that shoould never have been)
					//        'remove!
					//        toDel.Add(qid.ToString)
					//    Else
					//        aQuantity = New clsQuantity(qid, rgn, r.Item("Path").ToString(), branch, numPreinstalled, minIncr, prefincr, foc)
					//        h.Add(ck$)
					//    End If
					//Else
					//    toDel.Add(qid.ToString)
					//End If

					Count += 1;
				}
			}
		}
		r.Close();



		if (toDel.Count) {
			int toskip = 0;
			int chunk = 1000;
			do {
				object l = from j in toDel.Skip(toskip).Take(chunk);
				if (!l.Any)
					break; // TODO: might not be correct. Was : Exit Do
				sql = "DELETE FROM quantity WHERE ID IN(" + Join(l.ToArray, ",") + ")";

				LongSQL(sql);
				toskip += 1000;
			} while (true);

		}

		return "Loaded " + Count + " Quantity limits, (Deleted " + toDel.Count + ")";

	}

	/// <summary>Loads the quantity records (AutoAdds, Min Increment and Preferred increments) for the specified regions</summary>

	//Public Function LoadQuantities(regions As List(Of clsRegion)) As String

	//    Dim con As SqlClient.SqlConnection = da.OpenDatabase()
	//    Dim r As SqlClient.SqlDataReader

	//    For Each rgn In regions.ToList
	//        If rgn.quantitiesLoaded Then regions.Remove(rgn) 'remove those regions that have already been loaded from the list to get
	//    Next

	//    If regions.Count = 0 Then
	//        Return "All required quantites already loaded"
	//    Else
	//        Dim regionIDs As List(Of String) = New List(Of String)
	//        For Each rgn In regions : regionIDs.Add(rgn.ID.ToString) : rgn.quantitiesLoaded = True : Next

	//        Dim sql$
	//        sql$ = "SELECT id,[path], fk_region_id,fk_branch_id, preinstalled,minIncrement,preferredIncrement,foc "
	//        sql$ &= "FROM quantity WHERE fk_region_id in (" & Join(regionIDs.ToArray, ",") & ")"

	//        r = da.DBExecuteReader(con, sql$)

	//        Dim Count As Integer = 0

	//        Dim branch As clsBranch
	//        Dim aQuantity As clsQuantity
	//        Dim rid As Integer
	//        Dim numPreinstalled As Integer

	//        If r.HasRows Then
	//            While r.Read
	//                branch = iq.Branches(CInt(r.Item("fk_branch_id")))
	//                rid = CInt(r.Item("ID"))
	//                numPreinstalled = CInt(r.Item("preInstalled"))

	//                'creating the quantity - adds it to the branch - the branch is may (have already been) grafted (in many places) - where the quantity limits will apply if there is a scope match (by Path)

	//                'aQuantity = New clsQuantity(id, iq.Regions(r.Item("fk_region_id")), r.Item("Path"), branch, Nothing, numPreinstalled, r.Item("MinIncrement"), r.Item("PreferredIncrement"), r.Item("foc"))
	//                aQuantity = New clsQuantity(rid, iq.Regions(CInt(r.Item("fk_region_id"))), r.Item("Path").ToString(), branch, numPreinstalled, CInt(r.Item("MinIncrement")), CInt(r.Item("PreferredIncrement")), CBool(r.Item("foc")))


	//                Count += 1
	//            End While
	//        End If
	//        r.Close()
	//        con.Close()
	//        Return "Loaded " & Count & " Quantity limits"
	//    End If


	//End Function

	//Private Function LoadCountries(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

	//    r = da.dbexecuteReader(con, "SELECT Id,code,fk_translation_key_countryname,culture from Country")

	//    Dim aCountry As clsCountry
	//    Dim count As Integer = 0
	//    If r.HasRows Then
	//        While r.Read
	//            aCountry = New clsCountry(r.Item("id"), r.Item("code"), iq.Translations(r.Item("fk_translation_key_countryname")), r.Item("culture"))
	//            count += 1
	//        End While
	//    End If

	//    r.Close()

	//    Return "Loaded " & count & " countries"

	//End Function


	private string LoadRegions(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//This is not as clean as other loads - as we do it in two passes to ensure all regions are present before we set up the heirarchy..
		//we do this becuase the regions parents don't necessarily appear before their children

		int pass = 0;
		int count = 0;


		//we have to load all regions in, before we can set up their parents (becuase if a regions's parent is not yet loaded - bad things happen
		for (pass = 1; pass <= 2; pass++) {

			r = da.DBExecuteReader(con, "SELECT Id,[fk_region_id_parent],code,[fk_translation_key_name],isCountry,fk_culture_id,isPlaceholder,Notes,[FK_Region_ID_Geo] FROM [Region]");

			clsRegion aRegion;

			clsRegion parent;
			if (r.HasRows) {
				while (r.Read) {
					if (pass == 1) {
						//all parents are set to nothing on the first pass
						clsCulture culture;
						int tid = (int)r.Item("fk_translation_key_name");
						if (iq.Cultures.ContainsKey((int)r.Item("fk_culture_id"))) {
							culture = iq.Cultures((int)r.Item("fk_culture_id"));
						} else {
							culture = iq.i_culture_code("en-us");
						}


						clsTranslation tlrn;
						if (iq.Translations.ContainsKey((int)r.Item("fk_translation_key_name"))) {
							tlrn = iq.Translations((int)r.Item("fk_translation_key_name"));
						} else {
							tlrn = iq.Translations.Values.First;
							//TEMPORARY
						}


						aRegion = new clsRegion((int)r.Item("id"), null, r.Item("code").ToString(), tlrn, (bool)r.Item("isCountry"), culture, (bool)r.Item("isPlaceholder"), r.Item("notes").ToString, r.Item("FK_Region_ID_Geo").ToString());
						count += 1;
					} else {
						//set parents (on the seconds pass)
						//only set the parent on the second pass
						if (!IsDBNull(r.Item("fk_region_id_parent"))) {
							parent = iq.Regions((int)r.Item("fk_region_id_parent"));
							parent.Children.Add((int)r.Item("ID"), iq.Regions((int)r.Item("ID")));
							//add to the parent children
						} else {
							parent = null;
						}
						iq.Regions((int)r.Item("id")).Parent = parent;
					}
				}
			}

			r.Close();
		}


		return "Loaded " + count + " regions";

	}

	private string LoadCurrencies(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT Id,code,code_HP,symbol,rate,fk_translation_key_name,fk_translation_key_notes from currency");

		clsCurrency acurrency;
		int count = 0;
		clsTranslation notes;

		if (r.HasRows) {
			while (r.Read) {
				if (IsDBNull(r.Item("fk_translation_key_notes"))) {
					notes = null;
				} else {
					notes = iq.Translations((int)r.Item("fk_translation_key_notes"));
				}

				acurrency = new clsCurrency((int)r.Item("id"), r.Item("code").ToString(), r.Item("Code_HP").ToString(), iq.Translations((int)r.Item("fk_translation_key_name")), r.Item("symbol").ToString(), (float)r.Item("rate"), notes);
				// r.Item("culture"))

				// Add to the DefaultCurrencies collection (used when setting up new Channels etc.)
				DefaultCurrencies.Add(acurrency.ID, acurrency);

				count += 1;
			}
		}

		r.Close();

		return "Loaded " + count + " currencies";

	}


	private string LoadCultures(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT Id,culturecode,[Name] from culture where visible = 1");

		clsCulture aCulture;
		int count = 0;


		if (r.HasRows) {

			while (r.Read) {
				aCulture = new clsCulture((int)r.Item("id"), r.Item("culturecode").ToString(), r.Item("name").ToString());
				count += 1;
			}
		}

		r.Close();

		return "Loaded " + count + " cultures";

	}
	private string LoadStates(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT Id,[group],Code,fk_translation_key,[order],colour from [State]");

		int count;

		clsState aState;

		if (r.HasRows) {
			while (r.Read) {
				//creates a new user adds them to their channel
				aState = new clsState((int)r.Item("id"), r.Item("group").ToString(), Trim(r.Item("code").ToString()), iq.Translations((int)r.Item("fk_translation_key")), (int)r.Item("order"), r.Item("colour").ToString());
				count += 1;
			}
		}
		r.Close();

		return "Loaded " + count + "  States";


	}
	private string LoadRoles(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key from [Role]");

		int count;

		clsRole aRole;

		if (r.HasRows) {
			while (r.Read) {
				aRole = new clsRole((int)r.Item("id"), r.Item("code").ToString(), iq.Translations((int)r.Item("fk_translation_key")));
				count += 1;


			}
		}
		r.Close();


		return "Loaded " + count + " Roles";

	}
	private string LoadRights(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key from [Right]");

		int count;

		clsRight aRight;

		if (r.HasRows) {
			while (r.Read) {
				aRight = new clsRight((int)r.Item("id"), r.Item("code").ToString(), iq.Translations((int)r.Item("fk_translation_key")));
				count += 1;


				if (!i_right_Code.ContainsKey(aRight.Code)) {
					i_right_Code.Add(aRight.Code, aRight);
				}
			}
		}
		r.Close();

		return "Loaded " + count + " rights";

	}
	private string LoadRoleRights(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT roleright.Id,role.code as rolecode,[right].code as rightcode from [RoleRight] inner join role on role.id=fk_role_id inner join [right] on [right].id=fk_right_id");

		int count;

		clsRight right;

		if (r.HasRows) {
			while (r.Read) {
				right = iq.i_right_Code(r.Item("rightcode"));
				iq.i_role_Code(r.Item("rolecode")).Rights.Add(right.ID, right);
				iq.i_role_Code(r.Item("rolecode")).i_right_code.Add(right.Code, right);
				count += 1;
			}
		}
		r.Close();

		return "Loaded " + count + " Role-Rights ";

	}

	private string LoadThreads(SqlClient.SqlConnection con, SqlClient.SqlDataReader rdr)
	{

		int count = 0;

		object sql;
		sql = "SELECT id,FK_user_id_createdby,FK_user_id_AssignedTo,fk_thread_id_parent,fk_state_id_priority,fk_state_id_status,[hours],title, text,fk_event_id,created,updated,internal from [Thread] order by ID";

		rdr = da.DBExecuteReader(con, sql);

		clsThread aThread;

		clsUser CreatedBy;
		clsUser AssignedTo;
		clsThread Parent = null;
		clsState State;


		if (rdr.HasRows) {

			while (rdr.Read) {
				CreatedBy = iq.Users((int)rdr.Item("fk_user_id_createdby"));
				AssignedTo = iq.Users((int)rdr.Item("fk_user_id_assignedto"));

				if (IsDBNull(rdr.Item("fk_thread_id_parent"))) {
					Parent = null;
					//this is the (one and only) top level/root thread
				} else if ((int)rdr.Item("fk_thread_id_parent") == (int)rdr.Item("id")) {
				//Stop
				} else {
					Parent = iq.Threads((int)rdr.Item("fk_thread_id_parent"));
				}
				State = iq.States((int)rdr.Item("fk_state_id_status"));

				//    If IsDBNull(rdr.Item("fk_event_id")) Then
				// EventLog = Nothing
				//Else
				//EventLog = iq.Events(CInt(rdr.Item("fk_event_id")))
				//End If

				clsState priority;
				priority = iq.States((int)rdr.Item("fk_state_id_priority"));
				aThread = new clsThread((int)rdr.Item("id"), CreatedBy, AssignedTo, Parent, priority, State, (float)rdr.Item("hours"), rdr.Item("title").ToString(), new nullableString(rdr.Item("text")), (System.DateTime)rdr.Item("Created"),
				(System.DateTime)rdr.Item("Updated"), (bool)rdr.Item("internal"));
				count += 1;

			}
		}

		rdr.Close();

		return "Loaded " + count + " support threads <br/>";

	}
	private string LoadValidations(SqlClient.SqlConnection con, SqlClient.SqlDataReader rdr)
	{

		Validations.Clear();

		object sql;
		sql = "SELECT ID,description,regex,violation FROM Validation";

		rdr = da.DBExecuteReader(con, sql);

		clsValidation v;
		int count = 0;

		if (rdr.HasRows) {
			while (rdr.Read) {
				v = new clsValidation((int)rdr.Item("ID"), rdr.Item("description").ToString(), rdr.Item("regex").ToString(), rdr.Item("violation").ToString());
				count += 1;
			}
		}

		rdr.Close();

		return "loaded " + count + " Validations.<br/>";


	}

	public string LoadScreens(SqlClient.SqlConnection con, SqlClient.SqlDataReader rdr)
	{
		try {
			i_screens_title.Clear();
			i_screens_code.Clear();
			Screens.Clear();
			Fields.Clear();

			object sql;
			// sql$ = "SELECT ID,code,Title,[object],[dictionary] FROM [Screen]"
			sql = "SELECT ID,code,Title,[object] FROM [Screen] order by code";
			//NOTE the FIRST screen (by code) of each Object type is used for editing

			rdr = da.DBExecuteReader(con, sql);

			int CountScreens = 0;

			clsScreen screen;
			if (rdr.HasRows) {
				while (rdr.Read) {
					screen = new clsScreen((int)rdr.Item("id"), rdr.Item("code").ToString(), rdr.Item("object").ToString(), rdr.Item("Title").ToString());
					CountScreens += 1;
				}
			}
			rdr.Close();

			sql = "SELECT ID,FK_Field_Id,FK_Region_ID FROM [FIELDRestriction]";
			object restrictions = da.FilledDataTable(con, sql);


			//load fields
			//sql$ = "SELECT ID,fk_screen_id,property,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,[order],fk_screen_id_embed,width,length,defaultvalue,visible FROM [FIELD]"
			sql = "SELECT ID,fk_screen_id,property,fk_translation_key_label,helptext,fk_validation_id,lookupof,fk_inputtype_id,[order],width,height,length,defaultvalue,visibleList,VisiblePage,defaultFilter,defaultSort,[priority],fk_translation_key_widgetGroup,widgetUI,CanUserSelect,visibleSquare,FK_Field_ID_Linked,Grows,DefaultFilterValues,FilterVisible,[HMC_MutualExclusivity],[InvertFilterOrder] FROM [FIELD]";

			rdr = da.DBExecuteReader(con, sql);

			clsField afield;

			clsValidation validation = null;

			int countFields = 0;
			if (rdr.HasRows) {
				while (rdr.Read) {

						//                    If IsDBNull(rdr.Item("fk_screen_id_embed")) Then
						//embedscreen = Nothing
						// Else
						//embedscreen = Screens(.Item("fk_screen_id_embed"))
						// End If

						//need to fix this -  saving nullable foriend keys (field.validations is one example)


						//     If rdr.Item("lookupof") <> "" Then Stop





						//This Class has a root property - grouping instances (and a Current Property)


					 // ERROR: Not supported in C#: WithStatement

				}
			}

			rdr.Close();

			return "loaded " + CountScreens + " screens containing a total of " + countFields + " fields.<br/>";
		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;
		}

	}

	private string loadInputTypes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		iq.InputTypes.Clear();
		iq.i_inputType_code.Clear();

		object sql;
		sql = "SELECT ID,code,name FROM [InputType]";

		r = da.DBExecuteReader(con, sql);

		int count = 0;
		clsInputType inputType;
		if (r.HasRows) {
			while (r.Read) {
				inputType = new clsInputType((int)r.Item("ID"), r.Item("code").ToString(), r.Item("name").ToString());
				count += 1;
			}
		}
		r.Close();

		return "loaded " + count + "input types<br/>";

	}


	private string LoadMargins(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		r = da.DBExecuteReader(con, "SELECT Id,fk_channel_id_buyer,fk_channel_id_seller,factor,fk_sector_id,sampledsku from [margin]");

		clsChannel Seller;
		clsChannel Buyer;
		clsSector sector;
		//Dim productType As clsProductType
		int count;

		clsMargin aMargin;

		if (r.HasRows) {
			while (r.Read) {
				//creates a new price adding it to the master price list
				Buyer = iq.Channels((int)r.Item("fk_channel_id_buyer"));
				Seller = iq.Channels((int)r.Item("fk_channel_id_seller"));
				sector = iq.Sectors((int)r.Item("fk_sector_id"));
				// productType = iq.ProductTypes(CInt(r.Item("fk_producttype_id")))

				//the act of creating a margin adds it to the sellers dictionary of buyers margins
				aMargin = new clsMargin((int)r.Item("ID"), Seller, Buyer, (float)r.Item("factor"), "", sector, r.Item("sampledSKU").ToString());
				count += 1;
			}
		}
		r.Close();

		return "Loaded " + count + " margins";

	}
	private string LoadTeams(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{


		object sql;
		sql = "SELECT id,name,fk_channel_id from TEAM";

		r = da.DBExecuteReader(con, sql);

		int count;
		clsTeam aTeam;

		if (r.HasRows) {
			while (r.Read) {
				aTeam = new clsTeam((int)r.Item("id"), iq.Channels((int)r.Item("fk_channel_id")), r.Item("Name").ToString());
				count += 1;
			}
		}
		r.Close();

		return "Loaded " + count + " teams";


	}
	private string LoadUsers(SqlClient.SqlConnection con, SqlClient.SqlDataReader r, ref List<string> errormessages)
	{

		object sql;

		try {
			sql = "SELECT id,fk_channel_id,email,realname,tel1,tel2,Disabled FROM [User]";

			r = da.DBExecuteReader(con, sql);

			int count;
			clsUser aUser;
			string currentuser = "";
			if (r.HasRows) {
				while (r.Read) {
					int cid = (int)r.Item("fk_channel_id");
					int uid = (int)r.Item("id");
					string email = r.Item("email").ToString();
					bool disabled = r.Item("Disabled");

					if (iq.Channels.ContainsKey(cid)) {
						aUser = new clsUser(uid, iq.Channels(cid), email, r.Item("realname").ToString(), new nullableString(r.Item("tel1")), new nullableString(r.Item("tel2")), disabled);

						currentuser = r.Item("id").ToString();
						count += 1;
					} else {
						errormessages.Add("User " + uid + " " + email + " has an invalid channel " + cid);
					}

				}
			}
			r.Close();

			return "Loaded " + count + " users";
		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;
		}


	}

	private string LoadUserMessages(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		iq.UserMessages = new Dictionary<string, List<clsMessage>>();

		try {
			r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_key_Name, FK_Channel_ID, ValidFrom, ValidTo, Enabled from [message] order by ValidFrom");
			if (r.HasRows) {

				while (r.Read()) {
					string code = r.Item("Code");


					if (!iq.UserMessages.ContainsKey(code)) {
						iq.UserMessages.Add(code, new List<clsMessage>());
					}

					int tkey = r.Item("FK_Translation_key_Name");
					if (Translations.ContainsKey(tkey)) {
						clsTranslation translation = this.Translations(tkey);
						iq.UserMessages(code).Add(new clsMessage(r.Item("ID"), code, translation, r.Item("ValidFrom"), r.Item("ValidTo"), r.Item("Enabled"), r.Item("FK_Channel_ID")));
					} else {
						ErrorLog.Add(new Exception("Missing translation key in LoadUserMessages fk_translations_key_name was " + tkey));
					}
				}
			}


		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;

		}

		return "Loaded user messages";

	}

	private string LoadAddresses(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		iq.Addresses = new Dictionary<string, clsAddress>();

		try {
			r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_Key_Address from [Address]");
			if (r.HasRows) {

				while (r.Read()) {
					string code = r.Item("Code");
					clsTranslation translation = this.Translations(r.Item("FK_Translation_Key_Address"));
					iq.Addresses.Add(code, new clsAddress(r.Item("ID"), code, translation));

				}
			}


		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;

		}

		return "Loaded addresses";

	}

	private string LoadLegal(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		iq.Legal = new Dictionary<string, clsLegal>();

		try {
			r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_Key_Name from [Legal]");
			if (r.HasRows) {

				while (r.Read()) {
					string code = r.Item("Code");
					clsTranslation translation = this.Translations(r.Item("FK_Translation_Key_Name"));
					object legal = new clsLegal(r.Item("ID"), code, translation);
					if (!iq.Legal.ContainsKey(code)) {
						iq.Legal.Add(code, legal);
					}

				}
			}


		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;

		}

		return "Loaded legal";

	}

	private string LoadResources(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		iq.ResourceCategories = new Dictionary<int, clsResourceCategory>();

		// Resource Categories
		try {
			r = da.DBExecuteReader(con, "select [ID], [Name], [FK_Translation_Key_Name], [Order] from [ResourceCategory]");
			if (r.HasRows) {

				while (r.Read()) {
					int tkey = r.Item("FK_Translation_Key_Name");
					if (Translations.ContainsKey(tkey)) {
						iq.ResourceCategories.Add(r.Item("ID"), new clsResourceCategory(r.Item("ID"), r.Item("Name"), Translations(tkey), r.Item("Order")));
					} else {
						ErrorLog.Add(new Exception("Translation missing in LoadResources - key was" + tkey));
					}

				}
			}


		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;

		}

		// Resources Files
		try {
			r = da.DBExecuteReader(con, "select [ID], [Description], [FK_Resource_Category_ID], [Type], [Code], [FK_Translation_Key_Title], [FK_Region_ID], [FK_Language_ID], [FK_SellerChannel_ID], [mfrCode], [Order], [Embed] from [Resource]");
			if (r.HasRows) {

				while (r.Read()) {

					int tkey = r.Item("FK_Translation_Key_Title");
					if (Translations.ContainsKey(tkey)) {
						clsTranslation translation = this.Translations(tkey);
						int categoryId = r.Item("FK_Resource_Category_ID");
						clsRegion region = null;
						clsLanguage language = null;
						clsChannel sellerChannel = null;
						string mfrCode = null;

						if (!IsDBNull(r.Item("FK_Region_ID"))) {
							int regionId = r.Item("FK_Region_ID");
							if (iq.Regions.ContainsKey(regionId)) {
								region = iq.Regions(regionId);
							}
						}

						if (!IsDBNull(r.Item("FK_Language_ID"))) {
							int languageId = r.Item("FK_Language_ID");
							if (iq.Languages.ContainsKey(languageId)) {
								language = iq.Languages(languageId);
							}
						}

						if (!IsDBNull(r.Item("FK_SellerChannel_ID"))) {
							int sellerChannelId = r.Item("FK_SellerChannel_ID");
							if (iq.Channels.ContainsKey(sellerChannelId)) {
								sellerChannel = iq.Channels(sellerChannelId);
							}
						}

						if (!IsDBNull(r.Item("mfrCode"))) {
							mfrCode = r.Item("MfrCode");
						}

						clsResource resource = new clsResource(r.Item("ID"), r.Item("Description"), r.Item("Type"), r.Item("Code"), translation, region, language, sellerChannel, mfrCode, r.Item("Order"),
						categoryId, r.Item("Embed"));

						if (iq.ResourceCategories.ContainsKey(categoryId)) {
							if (iq.ResourceCategories(categoryId).Resources == null) {
								iq.ResourceCategories(categoryId).Resources = new List<clsResource>();
							}
							iq.ResourceCategories(categoryId).Resources.Add(resource);
						}
					} else {
						ErrorLog.Add(new Exception("Missing translation in LoadResources " + tkey));
					}

				}
			}


		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;

		}

		return "Loaded resources";

	}

	private string LoadROKAttributes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		iq.ROKAttributes = new Dictionary<string, List<clsROKAttribute>>();

		try {
			r = da.DBExecuteReader(con, "select ID, OS_Code, FK_Attribute_Code, FK_Translation_Key_Name from ROKAttributes order by OS_Code");
			if (r.HasRows) {

				while (r.Read()) {
					string osCode = r.Item("OS_Code");

					if (!iq.ROKAttributes.ContainsKey(osCode)) {
						iq.ROKAttributes.Add(osCode, new List<clsROKAttribute>());
					}

					clsTranslation translation = this.Translations(r.Item("FK_Translation_key_Name"));
					clsROKAttribute rok = new clsROKAttribute(r.Item("ID"), osCode, r.Item("FK_Attribute_Code"), translation);

					iq.ROKAttributes(osCode).Add(rok);

				}
			}


		} catch (Exception ex) {
			ErrorLog.Add(ex);
			return ex.Message;

		}

		return "Loaded ROK attributes";

	}

	private string LoadAccounts(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		object sql;

		sql = "SELECT fk_account_id,role.code as rolecode FROM [AccountRoles] inner join role on role.id=fk_role_id";
		r = da.DBExecuteReader(con, sql);
		Dictionary<int, List<clsRole>> dr = new Dictionary<int, List<clsRole>>();
		if (r.HasRows) {
			while (r.Read) {
				if (!dr.ContainsKey(r.Item("fk_account_id"))) {
					dr.Add(r.Item("fk_account_id"), new List<clsRole>());
				}
				dr(r.Item("fk_account_id")).Add(iq.i_role_Code(r.Item("rolecode")));

			}
		}

		//sql$ = "SELECT id,fk_user_id,password,fk_role_id,fk_team_id,fk_language_id,fk_buyergroup_id,fk_currency_id FROM [Account]"
		sql = "SELECT id,fk_user_id,password,fk_team_id,fk_language_id,fk_channel_id_buyer,fk_currency_id,fk_channel_id_seller,priceBand,fk_culture_id,mfrCode FROM [Account]";
		r = da.DBExecuteReader(con, sql);

		int count;
		clsAccount anAccount;

		//Dim buyerGroup As clsBuyerGroup
		clsChannel Buyer;
		clsUser User;
		clsTeam Team;
		clsLanguage Language;
		clsCurrency currency;
		clsRole Role;
		clsChannel seller;
		clsCulture culture;

		if (r.HasRows) {
			while (r.Read) {
				try {
					if (count == 174130) {
						object a = 6;
					}
					//buyergroup = iq.BuyerGroups(r.Item("fk_buyergroup_id"))
					Buyer = iq.Channels((int)r.Item("fk_channel_id_buyer"));
					seller = iq.Channels((int)r.Item("fk_channel_id_seller"));
					if (iq.Cultures.ContainsKey((int)r.Item("fk_culture_id"))) {
						culture = iq.Cultures((int)r.Item("fk_culture_id"));
					} else {
						culture = iq.i_culture_code("en-us");
					}

					User = iq.Users((int)r.Item("fk_user_id"));

					// If r.Item("FK_USER_ID") = 65538 Then Stop

					if (IsDBNull(r.Item("fk_team_id"))) {
						Team = null;
					} else {
						int tid;
						tid = (int)r.Item("fk_team_id");
						Team = iq.Teams(tid);
					}

					Language = iq.Languages((int)r.Item("fk_language_id"));
					currency = iq.Currencies((int)r.Item("fk_currency_id"));

					anAccount = new clsAccount((int)r.Item("id"), User, r.Item("password").ToString(), Buyer, dr.ContainsKey((int)r.Item("id")) ? dr((int)r.Item("id")).ToArray : {
						
					}, Team, Language, currency, seller, iq.getPriceBand(r.Item("priceBand").ToString()),
					culture, r.Item("mfrcode"));
					count += 1;
				} catch (Exception ex) {
					object a = ex.Message;
				}

			}
		}
		r.Close();

		return "Loaded " + count + " accounts";

	}


	private string LoadChannels(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//Channels include Distributors, Resellers, Manufacturers - they are basically sets of users (for whom a set of pricing may exist)
		//in the case of resellers - chanel reall just represent the buying company - for who the dist's set up margin records

		r = da.DBExecuteReader(con, "SELECT Id,fk_channel_id_parent,Name,BusinessName,Address,code,fk_region_id,webtoken,pic1,pic2,url,fk_channel_id_cloneof,priceConfig,treePath,Focus,margintype,marginMin,marginMax,SchemeOverride,Legal,FK_Currency_ID_Default,universal,orderemail,basketmode,basketurl From Channel order by id");

		clsChannel achannel;
		int coId;
		//clone of' ID

		// child > parent - Used to construct the heirarchy of channels once they're all loaded
		//(as the order of channels cannot be guaranteed and the parent may not be present at the point we instance the child)
		Dictionary<int, int> dicClones = new Dictionary<int, int>();
		Dictionary<int, int> dicChildren = new Dictionary<int, int>();

		if (r.HasRows) {
			while (r.Read) {
				//creates a new channel and adds it to the 'master' list
				object code;
				if (IsDBNull(r.Item("code")))
					code = "";
				else
					code = r.Item("code").ToString();

				int id = (int)r.Item("id");

				if (code != "" & iq.i_channel_code.ContainsKey(code)) {
					object l = iq.i_channel_code.ContainsKey(code) + " is duplicated";

				} else {
					//Dim webtoken As Guid = r.Item("webtoken")
					string webtoken = r.Item("webtoken").ToString();
					object WT = webtoken.ToString;

					coId = (int)r.Item("fk_channel_id_cloneof");
					if (IsDBNull(coId)) {
						System.Diagnostics.Debugger.Break();
					//this channel is a clone of ITSELF
					} else if ((int)r.Item("id") == coId) {

					//it's a clone of something OTHER than itself
					} else {
						dicClones.Add(id, coId);
						//I' am the 'child' 
					}

					dicChildren.Add(id, r.Item("fk_channel_id_cloneof"));
					//dictionary of child >parent

					clsChannel parent;
					if (object.ReferenceEquals(r.Item("fk_channel_id_parent"), DBNull.Value)) {
						parent = null;
					} else {
						if (iq.Channels.ContainsKey((int)r.Item("fk_channel_id_parent"))) {
							parent = iq.Channels((int)r.Item("fk_channel_id_parent"));
						} else {
							//this channel has been orphaned
							parent = null;
						}
					}

					//we can't set the 'iscloneof' or 'parent' unti we've loaded all channels (becuase the parent may not be there yet)
					achannel = new clsChannel((int)r.Item("id"), null, r.Item("Name").ToString(), r.Item("BusinessName").ToString(), null, r.Item("Address").ToString(), code, iq.Regions((int)r.Item("fk_region_id")), WT, new nullableString(r.Item("pic1")),
					new nullableString(r.Item("pic2")), new nullableString(r.Item("url")), (int)r.Item("priceConfig"), r.Item("TreePath").ToString(), r.Item("Focus").ToString(), r.Item("marginMin"), r.Item("Marginmax"), r.Item("MarginType"), r.Item("schemeOverride"), r.Item("legal"),
					IsDBNull(r.Item("FK_Currency_ID_Default")) ? null : iq.Currencies((int)r.Item("FK_Currency_ID_Default")), (bool)r.Item("universal"), r.Item("orderemail"), r.Item("basketMode"), r.Item("BasketURL"));

				}
			}
		}
		r.Close();

		//now (all channels are loaded) setup the clones
		//clones are 'copies' of other channels which have the same portfolio (set of products) and pricing which is some factor of one of the price bands ('A','B','','internal','external' etc) according to the [Margins]
		foreach ( childID in dicClones.Keys) {
			iq.Channels(childID).IsCloneOf = iq.Channels(dicClones(childID));
		}

		//And similarly the parent-child relationship ('food chain') of channels (which is a completely different thing from clones and is really only used for organisation/reporting)
		//(we can't guaranteed the order of chanels and children may be defined before their parents )
		foreach ( childID in dicChildren.Keys) {
			iq.Channels(childID).Parent = iq.Channels(dicChildren(childID));
		}

		DataTable domainTable = LoadDomains(con);
		if (domainTable != null) {
			foreach (DataRow row in domainTable.Rows) {
				int cid = (int)row("fk_channel_id");
				//A check that *should be unnecessary (but i broke some data)
				if (iq.Channels.ContainsKey(cid)) {
					clsChannel chnl = iq.Channels(cid);
					chnl.Domains.Add(row("domain").ToString());
				}
			}
		}

		return "Loaded " + iq.Channels.Count + " channels";

	}
	private string LoadBuyerGroups(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{

		//BuyerGroups allow prices to be defined for more than one buyer
		//you can have a buyergroup of 1 channel - if pricing really is different for every customer

		r = da.DBExecuteReader(con, "SELECT id,name,fk_channel_id_owner,ownersid FROM BuyerGroup");

		clsBuyerGroup aBuyerGroup;
		if (r.HasRows) {
			while (r.Read) {
				//creates a new BuyerGroup and adds it to the 'master' list                
				aBuyerGroup = new clsBuyerGroup((int)r.Item("id"), r.Item("Name").ToString(), iq.Channels((int)r.Item("fK_channel_id_owner")), r.Item("ownersID").ToString());
			}
		}
		r.Close();

		int placed = 0;
		r = da.DBExecuteReader(con, "SELECT fk_channel_id,fk_buyerGroup_id FROM ChannelGroup");
		if (r.HasRows) {
			while (r.Read) {
				iq.BuyerGroups((int)r.Item("fk_buyerGroup_id")).Channels.Add(iq.Channels((int)r.Item("fk_channel_id")));
				placed += 1;
			}
		}
		r.Close();

		return "Loaded " + iq.BuyerGroups.Count + " buyer groups, and placed " + placed + " Channels in them";

	}
	private string LoadAttributes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
	{
		try {
			r = da.DBExecuteReader(con, "Select [id],[code],[order],fk_translation_key_name from [Attribute]");

			clsAttribute a;
			if (r.HasRows) {
				while (r.Read) {
					//becuase we're specifying the ID here - it won't autmatically be written to the database
					if (!iq.i_attribute_code.ContainsKey(Trim(r.Item("code").ToString()))) {
						clsTranslation tl = iq.Translations((int)r.Item("fk_translation_key_name"));
						a = new clsAttribute((int)r.Item("ID"), Trim(r.Item("Code").ToString()), tl, (int)r.Item("ORDER"));
					} else {
						Logit("attribute " + r.Item("code").ToString() + " is duplicated");
					}
				}
			}
			r.Close();

			return "Loaded " + iq.Attributes.Count + " attributes";
		} catch (Exception ex) {
			return "Failed: " + ex.Message;
		}
	}
	public clsTranslation AddTranslation(string Text, clsLanguage language, string @group, int order, DataTable writecache, ref int nextKey, bool dupe)
	{

		//Set dupe if you (deliberately) want to create an addtional copy of the translation.. perhaps for isolated editing)

		//    Exit Function

		if (!object.ReferenceEquals(language, English))
			System.Diagnostics.Debugger.Break();

		if (writecache != null & nextKey == 0)
			System.Diagnostics.Debugger.Break();
		if (writecache == null & nextKey != 0)
			System.Diagnostics.Debugger.Break();

		clsTranslation existing = EnglishIndex(Text, @group);
		if (existing != null & dupe == false) {
			return existing;
		} else {
			//note, instancing a new clstranslation adds it to the englishindex
			return new clsTranslation(language, Text, @group, order, writecache, nextKey);
		}

	}
	private DataTable LoadDomains(SqlClient.SqlConnection con)
	{

		string query = "Select FK_Channel_ID,Domain from Domain";

		try {
			SqlCommand cmd = new SqlCommand(query, con);
			if (con.State == ConnectionState.Closed)
				con.Open();
			SqlDataReader dr = cmd.ExecuteReader();
			DataTable dt = new DataTable();
			dt.Load(dr);
			return dt;

		} catch (Exception ex) {
			return null;
			//Finally
			//    con.Close()

		}

	}

	private string LoadPromos(SqlConnection con, SqlDataReader r)
	{
		SqlCommand command = new SqlCommand();
		command.CommandType = CommandType.Text;
		command.CommandText = "Select * from promo inner join promoregion pr on promo.id=pr.fk_promo_id inner join promosystemtype pst on pst.fk_promo_id=promo.id";
		command.Connection = con;

		iq.Promos = new Dictionary<int, clsPromo>();
		iq.i_PromoRegions = new Dictionary<clsRegion, List<clsPromo>>();
		iq.i_PromoSystemTypes = new Dictionary<clsPromo, List<string>>();
		r = command.ExecuteReader();
		clsPromo promo;
		if (r.HasRows) {
			while (r.Read) {
				if (iq.Promos.ContainsKey((int)r("ID"))) {
					iq.Promos((int)r("ID")).AddRegion(iq.Regions((int)r("FK_Region_ID")));
					iq.Promos((int)r("ID")).AddSystemType(r("SystemType"));
				} else {
					promo = new clsPromo((int)r("ID"), r("Code").ToString(), iq.Translations((int)r("FK_Translation_Key_Description")), iq.Regions((int)r("FK_Region_ID")), r("FieldProperty_Filter").ToString, r("FieldProperty_Value").ToString, r("SystemType").ToString);
				}
			}
		}
		r.Close();

		command.CommandType = CommandType.Text;
		command.CommandText = "Select * from promoproduct";
		command.Connection = con;
		r = command.ExecuteReader();
		if (r.HasRows) {
			while (r.Read) {
				int pid = r("FK_Product_Id");
				clsProduct product = null;
				if (iq.Products.ContainsKey(pid)) {
					product = iq.Products(pid);
				} else {
					if (REMAPS.ContainsKey(pid)) {
						product = REMAPS(pid);
					}

				}

				if (product != null) {
					if (product.Promos != null && !product.Promos.ContainsKey(iq.Promos((int)r("FK_Promo_Id")).Code)) {
						product.Promos.Add(iq.Promos((int)r("FK_Promo_Id")).Code, new List<clsRegion>());
						product.Promos(iq.Promos((int)r("FK_Promo_Id")).Code).Add(iq.Regions(r("FK_REGION_ID")));
					}
				}

			}
		}

		r.Close();
		return "Loaded " + iq.Promos.Count + " Promos";

	}

	private string LoadCampaigns(SqlConnection con, SqlDataReader r)
	{
		SqlCommand command = new SqlCommand();
		command.CommandType = CommandType.Text;
		command.CommandText = "Select * from Campaign";
		command.Connection = con;

		r = command.ExecuteReader();
		clsCampaign campaign;
		if (r.HasRows) {
			while (r.Read) {
				campaign = new clsCampaign((int)r(0), r(1).ToString(), iq.Channels((int)r(2)), iq.Regions((int)r(3)), iq.Channels((int)r(4)), iq.Channels((int)r(5)), (System.DateTime)r(6), (System.DateTime)r(7));
				iq.Campaigns.Add(campaign.ID, campaign);
			}
		}
		r.Close();

		return "Loaded " + iq.Campaigns.Count + " Campaigns";
	}


	private string LoadAdverts(SqlConnection con, SqlDataReader r)
	{
		SqlCommand command = new SqlCommand();
		string query = "Select  [ID],[FK_Campaign_ID],[Name],[ImageURL],[URL],[Type],[BasketProductBelowAbsent],[BasketProductBelowPresent],";
		query += "[FK_ProdType_Present],[FK_ProdType_Absent],[FK_SlotType_ID],[FillThresholdPercent],[imageWide],[SlotTypeCode],[FK_Region_Id_Present],[FK_Region_Id_Absent], [visible], [mfrCode] from Advert";
		command.CommandType = CommandType.Text;
		command.CommandText = query;
		command.Connection = con;
		int adCount;
		r = command.ExecuteReader();
		clsAdvert advert;
		if (r.HasRows) {
			while (r.Read) {
				advert = new clsAdvert((int)r("ID"), iq.Campaigns((int)r("FK_Campaign_ID")), r("Name").ToString(), r("ImageURL").ToString(), r("Url").ToString(), (short)r("Type"), r("BasketProductBelowAbsent").ToString(), r("BasketProductBelowPresent").ToString(), iq.ProductTypes((int)r("FK_ProdType_Present")), iq.ProductTypes((int)r("FK_ProdType_Absent")),
				iq.SlotTypes((int)r("FK_SlotType_ID")), (int)r("FillThresholdPercent"), (bool)r("imageWide"), IsDBNull(r("SlotTypeCode")) ? null : r("SlotTypeCode"), IsDBNull(r("FK_Region_Id_Present")) ? null : iq.Regions((int)r("FK_Region_Id_Present")), IsDBNull(r("FK_Region_Id_Absent")) ? null : iq.Regions((int)r("FK_Region_Id_Absent")), (bool)r("visible"), (string)r("mfrCode"));

				adCount = adCount + 1;
			}
		}
		r.Close();

		return "Loaded " + adCount + " Adverts";
	}

	private string LoadScreenOverrides(SqlConnection con, SqlDataReader r)
	{
		SqlCommand command = new SqlCommand();
		string query = "Select  [FK_Account_ID],[FK_Screen_ID],Path,[FK_Field_ID],[ForceVisibilityTo],[ForceOrderTo],[ForceWidthTo],[ForceSortTo],[ForceFilterTo],[FK_DisplayUnit_ID] from AccountScreenOverride";
		command.CommandType = CommandType.Text;
		command.CommandText = query;
		command.Connection = con;
		int screenoverrideCount;
		r = command.ExecuteReader();
		clsScreenOverride @override;
		if (r.HasRows) {
			while (r.Read) {
				@override = new clsScreenOverride((int)r("FK_Account_ID"), (int)r("FK_Screen_ID"), r("Path").ToString(), (int)r("FK_Field_ID"), object.ReferenceEquals(r("ForceVisibilityTo"), DBNull.Value) ? null : (r("ForceVisibilityTo")), object.ReferenceEquals(r("ForceOrderTo"), DBNull.Value) ? new int?() : (int)r("ForceOrderTo"), object.ReferenceEquals(r("ForceWidthTo"), DBNull.Value) ? new double?() : (double)r("ForceWidthTo"), r("ForceSortTo").ToString(), r("ForceFilterTo").ToString(), object.ReferenceEquals(r("FK_DisplayUnit_ID"), DBNull.Value) ? null : iq.Units((int)r("FK_DisplayUnit_ID")));
				screenoverrideCount += 1;
			}
		}
		r.Close();

		return "Loaded " + screenoverrideCount + " Overrides";
	}

	public string LoadConversions(SqlConnection con, SqlDataReader r)
	{
		SqlCommand command = new SqlCommand();
		string query = "Select  FK_Units_From,FK_Units_To,Rate from Conversion";
		command.CommandType = CommandType.Text;
		command.CommandText = query;
		command.Connection = con;
		int conversioncount;
		r = command.ExecuteReader();
		Conversions = new Dictionary<int, Dictionary<int, double>>();
		if (r.HasRows) {
			while (r.Read) {
				if (!Conversions.ContainsKey((int)r("FK_Units_From"))) {
					Conversions.Add((int)r("FK_Units_From"), new Dictionary<int, double>());
				}
				Conversions((int)r("FK_Units_From")).Add((int)r("FK_Units_To"), (double)r("Rate"));
				conversioncount += 1;
			}
		}
		r.Close();

		return "Loaded " + conversioncount + " Conversions";
	}

	public string LoadMeasures(SqlConnection con, SqlDataReader r)
	{
		SqlCommand command = new SqlCommand();
		string query = "Select ID,MeasureName from Measure";
		command.CommandType = CommandType.Text;
		command.CommandText = query;
		command.Connection = con;
		int measurecount;
		r = command.ExecuteReader();
		Measures = new Dictionary<int, string>();
		if (r.HasRows) {
			while (r.Read) {
				Measures.Add((int)r("ID"), r("MeasureName").ToString());
				measurecount += 1;
			}
		}
		r.Close();

		return "Loaded " + measurecount + " Measures";
	}

	public string LoadActiveUniversal(SqlConnection con, SqlDataReader r)
	{
		SqlCommand command = new SqlCommand();
		string query = "select r.code, t.[Text] from Region r inner join Translation t on r.FK_Translation_key_name = t.[key] inner join Universal on [Name] = t.[Text] where r.IsCountry = 1 And [Enabled] = 1 order by t.[Text]";
		command.CommandType = CommandType.Text;
		command.CommandText = query;
		command.Connection = con;
		int countryCount;
		r = command.ExecuteReader();
		ActiveUniversalCountries = new Dictionary<string, string>();
		if (r.HasRows) {
			while (r.Read) {
				ActiveUniversalCountries.Add(r("Code"), r("Text").ToString());
				countryCount += 1;
			}
		}
		r.Close();

		return "Loaded " + countryCount + " Active Universal Countries";
	}




	public static void DeleteTL(clsTranslation tl, clsLanguage language)
	{
		iq.Translations(tl.Key).@remove(language);
		if (object.ReferenceEquals(language, English)) {
			iq.iEnglishIndex.Remove(tl.text(English) + "^" + tl.Group);
		}

	}




	public static void IndexTL(clsTranslation tl, clsLanguage language)
	{
		//longer term - we should probably never be looking anything up by 'english'
		//(and thus don't need an 'englishindex'
		//one of the main reasons we haveto do that is that part numbers are attributes
		//HPPart numbers should really only exist on the HP variant of the product (clsvariant)
		//although another pratical solution is to have a product.sku as string
		//Some attributes - such as familcodes, or compatible SKUS should possibly be untranslated text - ie clsProductAttributes should have a 'Text' *aswell* as a translation (and a numeric value)

		if (!iq.Translations.ContainsKey(tl.Key))
			iq.Translations.Add(tl.Key, tl);

		object ck = tl.text(language) + "^" + tl.Group;
		if (object.ReferenceEquals(language, English)) {
			if (!iq.iEnglishIndex.ContainsKey(ck)) {
				iq.iEnglishIndex.Add(ck, tl);
			}
		} else if (language.Code == "KY") {
			if (!iq.KYIndex.ContainsKey(ck)) {
				iq.KYIndex.Add(ck, tl);
			}
		}

	}


	public static string CleanString(string s)
	{
		StringBuilder sb = new StringBuilder(s);
		string x = sb.ToString();
		int intx = 0;
		intx = x.IndexOf("<");
		int inty = 0;
		while (intx > 0) {
			inty = x.IndexOf(">", intx);
			sb.Remove(intx, inty - intx + 1);
			x = sb.ToString();
			intx = x.IndexOf("<");

		}

		return sb.ToString();
	}

	private string LoadLocations(SqlConnection con, SqlDataReader r)
	{
		SqlCommand command = new SqlCommand();
		string query = "SELECT [Code],[Description] FROM [Location]";
		command.CommandType = CommandType.Text;
		command.CommandText = query;
		command.Connection = con;
		int locationsCount;
		r = command.ExecuteReader();
		ActiveUniversalCountries = new Dictionary<string, string>();
		if (r.HasRows) {
			while (r.Read) {
				Locations.Add(r("Code"), r("Description"));
				locationsCount += 1;
			}
		}
		r.Close();

		return "Loaded " + locationsCount + " Locations";
	}


}
