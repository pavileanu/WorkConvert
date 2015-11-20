using dataAccess;
using System.Data.SqlClient;
using System.Threading;
using System.Xml.Serialization;
using System.IO;

//Option Strict On

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
    public static List<string> errorMessages; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
    public static LoggingList<string> messages; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    public Dictionary<int, clsProduct> REMAPS = new Dictionary<int, clsProduct>(); //

    public static clsIQ Instance
    {
        get
        {
            lock (_LockObj)
            {
                if (_instance == null || (!IsLoading && !IsLoaded))
                {
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
        lock (_LockObj)
        {
            _instance = null;
            IsLoaded = false;
        }

    }

    public static bool IsLoading = false;
    public static bool IsLoaded = false;

    public int nextSearchID; //used for the abandonment of keyword searches 'mid flight' -

    // Public PathCache As clsProductCache
    //the sets are shared amongst users, with the smallest  one being contiually replaced

    public clsVariant AllVariants;
    public int infoID; // as simple counter for unique ID's on info (blue circle) divs - may get quite large
    //Public NextKey As Integer
    public Dictionary<int, clsLanguage> Languages { get; set; }
    public Dictionary<int, clsLanguage> ActiveLanguages { get; set; }
    public Dictionary<int, clsSlotType> SlotTypes { get; set; }

    //This is a dictionary of dictionaries
    //                                                 KEY  (NOT ID!) >  Translation
    public Dictionary<int, clsTranslation> Translations { get; set; }
    public Dictionary<int, clsProduct> Products { get; set; }
    public Dictionary<int, clsstock> Stock { get; set; } //would be nice to get rid of this (accessible via the product - stock now belongs to variants)

    public clsBranch RootBranch { get; set; } //THE root node of the product tree... *every* product tree is attached to here - but it never exists in the database - it has an ID of 0
    public clsBranch RootCPQBranch { get; set; } //THE root node of all carepacks

    //Note, events load their children from the database 'just in time' - only root level events (those whose parents are themselves) are initially loaded (see iq.LoadEvents).. accessing the children property fetches them from the database.
    //events are *written* real time - into the object model only - and persisted (to the databse) at key points via a events PersistRecursive() metho
    //This allows very large numbers of events to be recorded at high speed (via bulk writes) with minimal memory footprint.

    public clsThread RootThread { get; set; }
    public clsChannel RootChannel { get; set; } //The channel hierarchy is not a rigid reflection of the 'real life' supply chain - it's just for grouping and presentation withing the editor (and potentially reporting)
    //The actual relationships between channels are defined by the margins/prices - each linking a buyer and a seller  in a many:many relationship (the same reseller may buy from several distributors)

    //Property StandardVariant As clsVariant
    public Dictionary<int, clsBranch> Branches { get; set; }
    public Dictionary<int, clsQuantity> Quantities { get; set; }

    public Dictionary<string, clsBranch> i_SpecialBranches { get; set; }

    //Grafts provide cross linking in the tree - allowing a branch to be reused in may places
    //becasue a single branch (eg. a system unit) may have many branches grafted onto it (eg, Drive bays, memory slots, PCI slots)
    //we must have ready access to a list of grafts onto a particular branch
    //the integer index is the 'target' portion of the graft - this item contains a list of all the grafts onto it
    //property Grafts As SortedDictionary(Of clsBranch, List(Of clsGraft))
    public Dictionary<int, clsAttribute> Attributes { get; set; } //the 'master' list of all (types of) attribute

    //iEnglishIndex uses a compund key internally - and iexposed via a public function  (forcing a group to be specified)
    private Dictionary<string, clsTranslation> iEnglishIndex { get; set; } //Watch out !... the integer part of this is the translation.KEY
    public Dictionary<string, clsTranslation> KYIndex { get; set; } //Watch out !... the integer part of this is the translation.KEY
    public Dictionary<int, clsUnit> Units { get; set; } //master list of units - keyed by our internal short code eg KG, MM, M, LBS, BTU
    public Dictionary<int, clsChannel> Channels { get; set; } //Each channel has a set of users, each user has a set of quotes
    public Dictionary<int, clsBuyerGroup> BuyerGroups { get; set; }
    public Dictionary<int, clsTeam> Teams { get; set; }
    public Dictionary<int, clsUser> Users { get; set; }
    public Dictionary<int, clsProductType> ProductTypes { get; set; }

    //Did spend a lot of time considering this as channel.customeraccounts - but it makes operations on the whole list harder
    public Dictionary<int, clsAccount> Accounts { get; set; } //Each user has an account with one (or more) distributors(channels)
    //Quotes live under the agentaccounts - property Quotes As SortedDictionary(Of Integer, clsquote) 'Each channel has a set of users, each user has a set of quote
    public Dictionary<int, clsCurrency> Currencies { get; set; }
    public Dictionary<int, clsCulture> Cultures { get; set; }
    // Property Countries As Dictionary(Of Integer, clsCountry) - replaced by regions which are more generalised and heirarchical -
    public Dictionary<int, clsState> States { get; set; }
    //property Quantities As Dictionary(Of Integer, clsQuantity) 'Quantities apply to specific nodes in the tree (via their paths)
    //property Prices As Dictionary(Of Integer, clsPrice)
    //Property Prunes As Dictionary(Of String, clsPrune)
    //Property Prunes As Dictionary(Of Integer, clsPrune)
    public Dictionary<int, clsSector> Sectors { get; set; }
    //Property Events As Dictionary(Of Integer, clsEvent)
    public Dictionary<int, clsQuote> Quotes { get; set; } // Quotes are *not* populated at startup (as there are an awful lot of them) - however we need a root level dictionary to enable viewing/editing via the OM
    public Dictionary<int, clsThread> Threads { get; set; }
    public Dictionary<int, clsRegion> Regions { get; set; }
    public Dictionary<int, ClsAvalancheOPG> AvalancheOPGs { get; set; }
    public Dictionary<int, clsFlexOPG> FlexOPGs { get; set; }
    public Dictionary<int, clsBundle> Bundles { get; set; }
    public Dictionary<int, clsExclude> Excludes { get; set; } //having any member of key list exclude all members of the values list
    public Dictionary<string, List<clsMessage>> UserMessages { get; set; }
    public Dictionary<string, List<clsROKAttribute>> ROKAttributes { get; set; }
    public Dictionary<string, clsAddress> Addresses { get; set; }
    public Dictionary<string, clsLegal> Legal { get; set; }
    public Dictionary<int, clsResourceCategory> ResourceCategories { get; set; }

    // HP care pack service levels
    public Dictionary<int, clsServiceLevel> ServiceLevels { get; set; }
    public Dictionary<int, clsResponse> ServiceLevelResponse { get; set; }
    public Dictionary<int, clsServiceType> ServiceLevelServiceType { get; set; }
    public Dictionary<int, clsTROAA> ServiceLevelTROAA { get; set; }
    public Dictionary<string, clsAttribute> ServiceLevelAttributeMap { get; set; }
    public IDictionary<string, DateTime> CarePackLastRefresh { get; set; }

    //ultimately these are references - so not a expensive as they might look (around 12 bytes per row)
    public Dictionary<int, clsPrice> Prices { get; set; } //I've been all 'round the houses with this - (where to 'store' prices) - as dictionaries within the products etc,etc - but at the end of the day it makes most sense to have a root level 'master' list - most becuase the lookups are Olog(n)
    public Dictionary<int, clsVariant> Variants { get; set; }


    public Dictionary<int, clsCampaign> Campaigns { get; set; }
    public Dictionary<int, clsAdvert> Adverts { get; set; }
    public List<clsScreenOverride> ScreenOverrides { get; set; }
    public Dictionary<int, clsPromo> Promos { get; set; }
    public Dictionary<clsRegion, List<clsPromo>> i_PromoRegions;
    public Dictionary<clsPromo, List<string>> i_PromoSystemTypes;

    public Dictionary<int, Dictionary<int, double>> Conversions { get; set; }
    public Dictionary<int, string> Measures { get; set; }
    public Dictionary<string, string> ActiveUniversalCountries { get; set; }
    public Dictionary<int, clsValidationInclusion> ValidationInclusions { get; set; }
    public Dictionary<string, string> Locations { get; set; }

    public Dictionary<int, clsFilter> Filters;

    public Dictionary<int, clsCurrency> DefaultCurrencies { get; set; }

    //These i_ dictionaries are effectively indexes for various columns in the object model - In obscure places, or where speed isn't critical, we use LINQ - but these
    //lookup tables will be much faster - as a general principle these 'indexes' are updated when the objects they index are created (ie., in their New() constructors
    public Dictionary<string, clsState> i_state_GroupCode; // allows state (messages and ID's) to be looked up via their codes
    public Dictionary<string, clsUser> i_user_email; //index of username to user objects (as there are several thousand)
    public Dictionary<string, clsChannel> i_channel_code;
    public Dictionary<string, clsProduct> i_SKU; //used primarly by the webservice to look up products by SKU

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
    public Dictionary<string, clsPriceBand> priceBands { get; set; }


    public Dictionary<int, clsRole> Roles
    {
        get
        {
            return i_role_Code.Values.ToDictionary(rc => rc.ID, rc => rc);
        }
        set
        {
            i_role_Code = value.Values.ToDictionary(irc => irc.Code, irc => irc);
        }
    }
    public Dictionary<int, clsRight> Rights
    {
        get
        {
            return i_right_Code.Values.ToDictionary(rc => rc.ID, rc => rc);
        }
        set
        {
            i_right_Code = value.Values.ToDictionary(irc => irc.Code, irc => irc);
        }
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

    public Dictionary<int, clsScheme> Schemes { get; set; } //Loyalty schemes  ' products have a dictionary of scheme>points

    //Generic editor stuff

    public Dictionary<int, clsScreen> Screens { get; set; }
    public Dictionary<int, clsField> Fields { get; set; }
    public Dictionary<int, clsValidation> Validations { get; set; }
    public Dictionary<int, clsInputType> InputTypes { get; set; }

    public Dictionary<string, clsInputType> i_inputType_code;
    public Dictionary<string, clsScreen> i_screens_title; // ' Used by the generic aditor makesecreen() code.. to see if we already have a screen for the TYPE of object we are currently making a screen for
    public Dictionary<string, clsScreen> i_screens_code; //Not
    //Public I_SCREENS_TYPE As Dictionary(Of String, clsScreen)
    //Public i_quantity_path As Dictionary(Of String, clsQuantity)

    public Dictionary<string, object> Gateway; //makes all the other dictionaries accessible by a name.. see Suggestor.aspx

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
    Dictionary<UInt64, List<string>> seshLog; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
    Dictionary<UInt64, DateTime> SeshTimes;

    //Switch for strict validation
    public bool StrictSlotValidation = false;

    public clsTranslation EnglishIndex(string text, string group)
    {

        //the circumflex is just a delimiter for the two parts of the compound key
        System.String ck = text + "^" + group;
        if (iEnglishIndex.ContainsKey(ck))
        {
            return iEnglishIndex(ck);
        }
        else
        {
            return null;
        }

    }
    public int recordLogin(clsUser User, bool failed, string email, string ua)
    {
        int returnValue = 0;

        if (User == null)
        {
            User = iq.i_user_email("unknown@unknown.com");
        }

        //Store useragent
        da.ExecuteSP(dataAccess.da.OpenDatabase(true), "usp_UpdateUserAgentList", new Dictionary<string, object>[] { { "AgentName", ua } }, null);

        returnValue = System.Convert.ToInt32(da.DBExecutesql("INSERT INTO [login] (fk_user_id,timestamp,failed,TypedEmail,lid,FK_UserAgent_Id,ServerNode) VALUES (" + User.ID + ",getdate()," + (failed ? 1 : 0).ToString() + "," + da.SqlEncode(email) + (",NULL,(SELECT Id FROM UserAgents WHERE AgentName=" + da.SqlEncode(ua) + "),") + da.SqlEncode(Environment.MachineName) + ");", true));


        return returnValue;
    }

    public clsPriceBand getPriceBand(string text)
    {

        if (this.priceBands.ContainsKey(text))
        {
            return this.priceBands(text);
        }
        else
        {
            return new clsPriceBand(text); //This Should/must be/is the ONLY call to the constructor of clsPriceband
        }

    }
    public void updateLogin(int tid, UInt64 lid)
    {
        da.DBExecutesql("Update Login set lid=" + System.Convert.ToString(lid) + " WHERE id=" + System.Convert.ToString(tid));
    }
    public void updateLogin(UInt64 lid, clsAccount account)
    {

        da.DBExecutesql("Update Login set fk_account_id_agent=" + account.ID + " WHERE lid=" + System.Convert.ToString(lid));

        //Dim focus As List(Of String) = New List(Of String)
        //For Each f In Split(account.SellerChannel.Focus, ",")
        //    If Trim(f) <> "" Then
        //        focus.Add(f)  'focus is a list of 'codes' for product groupings we (only) wish the display - eg. Receta
        //    End If
        //Next
        iq.sesh(lid, "foci") = account.SellerChannel.Focus; //focus is a CD list in the session variable too

    }

    public bool SeshAlive(UInt64 lid)
    {
        bool returnValue = false;
        returnValue = false;
        if (seshDic == null)
        {
            return false;
        }
        if (seshDic.Count > 0)
        {
            if (seshDic.ContainsKey(lid))
            {
                return true;
            }
            else
            {
                return LoadUserState(lid);
            }

        }

        return returnValue;
    }

    public bool SeshContains(UInt64 lid, string key)
    {

        if (seshDic == null)
        {
            return false;
        }

        if (seshDic.ContainsKey(lid))
        {
            return (seshDic[lid].ContainsKey(key));
        }
        else
        {
            return false;
        }
    }

    public dynamic SeshValue(UInt64 lid, string key, object AbsentValue)
    {

        //returns a  'sesh'ion variable - returns false if the variable is absent

        if (!seshDic[lid].ContainsKey(key))
        {
            return AbsentValue;
        }
        else
        {
            return seshDic[lid][key];
        }

    }


    public Dictionary<string, object> getSeshDic(UInt64 lid)
    {

        UpdateSeshTime(lid);
        return seshDic[lid];

    }


    public T seshTyped<T>(UInt64 lid, string key)
    {
        object res = get_sesh(lid, key);
        if (res == null)
        {
            return res;
        }
        if (res.GetType() == typeof(T))
        {
            return ((T)res);
        }
        else
        {
            return null;
        }
    }


    public dynamic get_sesh(UInt64 lid, string key)
    {

        //   If lid = 0 Then Stop

        UpdateSeshTime(lid);
        if (seshDic == null)
        {
            return null;
        }
        if (!seshDic.ContainsKey(lid))
        {
            return null;
        }
        else
        {
            if (seshDic[lid].ContainsKey(key))
            {
                return seshDic[lid][key];
            }
            else
            {
                return null;
            }
        }
    }

    public void set_sesh(UInt64 lid, string key, object value)
    {

        //            If lid = 0 Then Stop

        if (seshDic == null)
        {
            seshDic = new Dictionary<UInt64, Dictionary<string, object>>();
        }
        if (!seshDic.ContainsKey(lid))
        {
            seshDic.Add(lid, new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase));
        }

        // If lid = 0 Then Stop
        seshDic[lid][key] = value;
    }


    public Table SessionTable()
    {
        if (seshDic == null)
        {
            return null;
        }
        Table t = new Table();
        t.CssClass = "sessionTable";

        string help = "Session ID - Click to view session||||Time to live (session will expire in X minutes)|Buyer|Current Value of basket items";
        TableHeaderRow thr = MakeTHR("lid,Current Page,Agent Email, HostID,TTL,QuotingFor,Quote Value", help, "adminTable");

        t.Rows.Add(thr);

        foreach (var k in seshDic.Keys)
        {
            t.Rows.Add(SeshTableRow(k));
        }

        return t;

    }

    public TableRow SeshTableRow(UInt64 lid)
    {
        TableRow returnValue = default(TableRow);

        List<string> errorMessages = new List<string>();

        TableCell tc = default(TableCell);
        HyperLink hl = default(HyperLink);
        Label lbl = default(Label);

        returnValue = new TableRow();

        clsAccount buyerAccount = (clsAccount)(iq.sesh(lid, "BuyerAccount"));
        clsAccount agentAccount = (clsAccount)(iq.sesh(lid, "AgentAccount"));

        if (agentAccount != null)
        {

            Dictionary with_1 = seshDic[lid];


            tc = new TableCell();
            returnValue.Controls.Add(tc);

            hl = new HyperLink();
            tc.Controls.Add(hl);

            hl.Text = "View";

            hl.NavigateUrl = with_1.Item("currentPage").ToString();

            tc = new TableCell();
            lbl = new Label();
            lbl.Text = with_1.Item("currentPage").ToString();
            tc.Controls.Add(lbl);
            returnValue.Controls.Add(tc);

            //Agent (sellers)  Email
            tc = new TableCell();
            lbl = new Label();
            lbl.Text = agentAccount.User.Email;
            tc.Controls.Add(lbl);
            returnValue.Controls.Add(tc);

            //Sellers HOSTID
            tc = new TableCell();
            lbl = new Label();
            lbl.Text = agentAccount.SellerChannel.Code;
            tc.Controls.Add(lbl);
            returnValue.Controls.Add(tc);

            //Time to live (that's 'LIV' not 'Lyve'
            tc = new TableCell();
            lbl = new Label();
            lbl.Text = (50 - DateAndTime.DateDiff(DateInterval.Minute, SeshTimes[lid], DateTime.Now)).ToString();
            tc.Controls.Add(lbl);
            returnValue.Controls.Add(tc);

            //Buyers Email
            tc = new TableCell();
            lbl = new Label();
            lbl.Text = buyerAccount.User.Email;
            tc.Controls.Add(lbl);
            returnValue.Controls.Add(tc);


            //Quote value
            tc = new TableCell();
            if (with_1.ContainsKey("QuoteID"))
            {
                Panel qvp = agentAccount.Quotes(System.Convert.ToInt32(with_1.Item("QuoteID"))).QuotedPrice.DisplayPrice(buyerAccount, errorMessages);
                tc.Controls.Add(qvp);
            }
            else
            {

            }
            returnValue.Controls.Add(tc);

        }

        return returnValue;
    }

    public void KillSesh(UInt64 lid)
    {

        // Dim StartBytes As Long = System.GC.GetTotalMemory(True)

        if (SeshTimes != null)
        {
            if (SeshTimes.ContainsKey(lid))
            {
                SeshTimes.Remove(lid);
            }
        }

        if (seshDic != null)
        {
            if (seshDic.ContainsKey(lid))
            {
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
        if (SeshTimes != null)
        {
            foreach (var kvp in SeshTimes.ToArray())
            {
                if (Math.Abs(DateAndTime.DateDiff(DateInterval.Minute, DateTime.Now, System.Convert.ToDateTime(kvp.Value))) > 60)
                {
                    toKill.Add(kvp.Key);
                }
            }

            foreach (var s in toKill)
            {
                SeshTimes.Remove(s);
                if (seshDic != null)
                {
                    seshDic.Remove(s);
                }
                TidySwiftBranches(s);
            }
        }

        return toKill.Count;

    }

    private void UpdateSeshTime(UInt64 lid)
    {

        if (SeshTimes == null)
        {
            SeshTimes = new Dictionary<UInt64, DateTime>();
        }
        lock (SeshTimes)
        {
            if (!SeshTimes.ContainsKey(lid))
            {
                SeshTimes.Add(lid, DateTime.Now);
            }
            else
            {
                SeshTimes[lid] = DateTime.Now;
            }
        }
    }

    public clsIQ()
    {
        // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
        errorMessages = new List<string>();
        messages = new LoggingList<string>();
        seshLog = new Dictionary<UInt64, List<string>>();
        OldUserSessions = new Dictionary<UInt64, clsUserState>();


        VBMath.Randomize(DateTime.Now.Millisecond);

        this.AllVariants = new clsVariant();

        this.Branches = new Dictionary<int, clsBranch>();
        //Me.Events = New Dictionary(Of Integer, clsEvent)
        this.Threads = new Dictionary<int, clsThread>();
        this.i_SpecialBranches = new Dictionary<string, clsBranch>();

        //PathCache = New clsProductCache
        Languages = new Dictionary<int, clsLanguage>();
        ActiveLanguages = new Dictionary<int, clsLanguage>();
        Translations = new Dictionary<int, clsTranslation>(); //

        Products = new Dictionary<int, clsProduct>();
        Stock = new Dictionary<int, clsstock>(); //we need a 'flat' list of the stock for incremental import purposes - the stock embeded in the product is more generally what's used
        Variants = new Dictionary<int, clsVariant>();

        iEnglishIndex = new Dictionary<string, clsTranslation>(); //makes the dictionary keys case insensitive
        KYIndex = new Dictionary<string, clsTranslation>(StringComparer.CurrentCultureIgnoreCase); //makes the dictionary keys case insensitive
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
        Schemes = new Dictionary<int, clsScheme>(); //Master list of Loyalty schemes  - products have a dictionary of scheme>points
        //  Recommendations = New Dictionary(Of Integer, clsRecommendation)


        //these 'index' dictionaries allow us to look things up very quickly. typically by some human readable code (rather than Row ID)
        //they are automatically added to in the constructors (sub New's) of the Classes they hold.
        //they carry only the string key, as a *reference* to an instance of an object - so their footprint is quite modsest
        i_user_email = new Dictionary<string, clsUser>(StringComparer.CurrentCultureIgnoreCase); //index of username to user objects (as there are several thousand)
        i_state_GroupCode = new Dictionary<string, clsState>(StringComparer.CurrentCultureIgnoreCase); // allows state (messages and ID's) to be looked up via a compound key of group-code
        i_channel_code = new Dictionary<string, clsChannel>(StringComparer.CurrentCultureIgnoreCase);
        i_SKU = new Dictionary<string, clsProduct>(StringComparer.CurrentCultureIgnoreCase); //used primarly by the webservice to look up products by SKU

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

        Gateway = new Dictionary<string, object>(); //makes other dictionaries accessible by name - allowing suggestion against many dictionaries (see suggestor.aspx)
        //                                             could be done with reflection - but I didn't have a good handle on that at the time, and this works just fine.

        Gateway.Add("channels", Channels);
        Gateway.Add("currencies", Currencies);
        Gateway.Add("regions", Regions);
        Gateway.Add("accounts", Accounts); //we will re-point this to the SellerChannels CustomerAccounts - just in time
        Gateway.Add("SKUs", i_SKU);

        Profiling.Profile = new Dictionary<string, clsProfile>();

    }


    public void load(List<string> errormessages)
    {
        //If IsLoading Then Exit Sub
        if (clsIQ.IsLoaded)
        {
            clsIQ.reset();
        }
        return;



        //        Try
        //		IsLoading = true;
        //		if (StartBytes == null)
        //		{
        //			StartBytes = System.GC.GetTotalMemory(true);
        //			}

        //			errorMessages.Clear();
        //			messages.Clear();
        //			messages.Start();

        //			bootstrap();

        //			long StopBytes = 0;

        //        p.ID = "loadinfo"
        //       p.Attributes("style") = "height:1.5em;overflow:hidden"

        //Dim b As New Image
        //b.ImageUrl = "/images/navigation/expand.png"
        //b.Attributes("onclick") = "document.getElementById('ctl00_MainContent_loadinfo').style.height='auto';"
        //p.Controls.Add(b)

        //Loads everything from the database

        //			SqlClient.SqlConnection con = default(SqlClient.SqlConnection);

        //			con = da.OpenDatabase();
        //			messages.Add("Opened the database");

        //			SqlClient.SqlDataReader r = default(SqlClient.SqlDataReader);
        //			r = null;

        //			messages.Add(LoadLanguages(con, r));
        //			messages.Add(LoadTranslations(con));

        // messages.Add(LoadProductValidations(con, r))

        //			messages.Add(LoadCultures(con, r));
        //messages.Add("size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")
        //			messages.Add(LoadRegions(con, r));

        //			r_worldwide = clsRegion.getOrMake(null, "XW", "Worldwide", false, false, "Not precious about XW");

        //			messages.Add(LoadStates(con, r));
        //LoadEvents(con) 'NB the Root Event is set in here

        //messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			messages.Add(LoadUnits(con, r));
        //			messages.Add(LoadAttributes(con, r));
        //messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			messages.Add(LoadSlotTypes(con, r));
        //			messages.Add(LoadProductTypes(con, r, errormessages));
        //			messages.Add(LoadSectors(con, r));

        //messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			messages.Add(LoadProducts(con, r));

        //messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			messages.Add(LoadCurrencies(con, r));

        //			messages.Add(LoadChannels(con, r));

        //If Not iq.i_channel_code.ContainsKey("Everyone") Then Dim achannel As clsChannel = New clsChannel(Nothing, "Everyone", "Public (list) pricing", "", "Everyone", r_worldwide, New nullableString, New nullableString, New nullableString, 0, "tree.1", "", 0, 0, "R", "", "") 'everyone is not a selling channel (so no priceConfig is required)

        //			Everyone = iq.getPriceBand("");
        //HPList = iq.getPriceBand("HPlist")

        //			messages.Add(LoadCampaigns(con, r));
        //			messages.Add(LoadPromos(con, r));
        //			messages.Add(LoadAdverts(con, r));
        //			messages.Add(LoadScreenOverrides(con, r));

        //StopBytes = System.GC.GetTotalMemory(False)
        //messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //txt &= LoadVariants(con, r)

        //StopBytes = System.GC.GetTotalMemory(False)
        //txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"


        //			messages.Add(loadInputTypes(con, r));
        //			messages.Add(LoadScreens(con, r));
        //			double ts = System.Convert.ToDouble(Stopwatch.GetTimestamp);
        //			messages.Add(LoadBranches(con, r, errormessages));
        //			int et = System.Convert.ToInt32((Stopwatch.GetTimestamp - ts) / Stopwatch.Frequency * 1000);


        //			messages.Add(LoadExcludes(con, r));

        //			messages.Add(LoadSpecialBranches(con, r, errormessages));

        //			messages.Add(LoadValidationsInclusions(con, r));

        //StopBytes = System.GC.GetTotalMemory(False)
        //messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			messages.Add(LoadSlots(con, r));
        //StopBytes = System.GC.GetTotalMemory(False)
        //messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			con.Close();
        //			con = da.OpenDatabase();
        //			messages.Add(LoadUserMessages(con, r));
        //			messages.Add(LoadAddresses(con, r));
        //			messages.Add(LoadLegal(con, r));
        //			messages.Add(LoadResources(con, r));

        //			messages.Add(LoadProductAttributes(con, r));
        //			messages.Add(CleanProducts(con, r));

        //			messages.Add(LoadROKAttributes(con, r));

        //StopBytes = System.GC.GetTotalMemory(False)
        //messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //txt &= LoadCountries(con, r)

        //			messages.Add(LoadHPServiceLevels(con, r));


        //			messages.Add(LoadAvalancheOPGs(con, r));
        //StopBytes = System.GC.GetTotalMemory(False)
        // messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			messages.Add(LoadBundles(con, r));

        //			messages.Add(LoadFlex(con, r));
        //			messages.Add(LoadFlexRegions(con, r));
        //			messages.Add(LoadFlexLines(con, r));
        //			messages.Add(LoadFlexRules(con, r));

        //StopBytes = System.GC.GetTotalMemory(False)
        //  messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			messages.Add(LoadSchemes(con, r));
        //			messages.Add(LoadPoints(con, r));

        //StopBytes = System.GC.GetTotalMemory(False)
        //   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")


        //			messages.Add(LoadQuantities(con, r));

        //StopBytes = System.GC.GetTotalMemory(False)
        //    messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //txt &= LoadChannelSkus(con, r)


        //StopBytes = System.GC.GetTotalMemory(False)
        //  messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //			con.Close();
        //			con = da.OpenDatabase();

        // txt &= LoadBuyerGroups(con, r)
        //			messages.Add(LoadUsers(con, r, errormessages));
        //			messages.Add(LoadTeams(con, r));
        //			messages.Add(LoadRoles(con, r));
        //			messages.Add(LoadRights(con, r));
        //			messages.Add(LoadRoleRights(con, r));
        //			messages.Add(LoadAccounts(con, r));
        //StopBytes = System.GC.GetTotalMemory(False)
        //   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //  txt &= LoadPrices(con, r, errorMessages)
        //  StopBytes = System.GC.GetTotalMemory(False)
        //  txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"

        //			messages.Add(LoadMargins(con, r));
        //StopBytes = System.GC.GetTotalMemory(False)
        //   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //txt &= LoadStock(con, r)
        //StopBytes = System.GC.GetTotalMemory(False)
        //txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"
        //			con.Close();
        //			con = da.OpenDatabase();
        //        txt &= LoadScreens(con, r)
        //			messages.Add(LoadValidations(con, r).ToString());
        //			messages.Add(LoadThreads(con, r).ToString());
        //			messages.Add(LoadFilters());

        //			messages.Add(LoadConversions(con, r));
        //			messages.Add(LoadMeasures(con, r));
        //			messages.Add(LoadActiveUniversal(con, r));
        //messages.Add(LoadLocations(con, r))

        //			if (iq.i_channel_code.ContainsKey("HP"))
        //			{
        //				HP = iq.i_channel_code("HP");
        //				if (HP != null)
        //				{
        //					messages.Add(HP.LoadVariants(errormessages, 1));
        //     messages.Add("size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")
        //					}

        //We now, dynamically load listprices for the users, accounts, sellerchannels, country - at signin (see accounts.aspx.vp)
        //txt &= HP.LoadPrices(Everyone, errormessages, r_GB.ID)

        //StopBytes = System.GC.GetTotalMemory(False)
        //    messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        //					}


        //					System.Object distinctlang = (from j in iq.Accounts.Values select j.Language).Distinct();

        //					foreach (clsLanguage selectedlang in distinctlang)
        //					{
        //						ActiveLanguages.Add(selectedlang.ID, selectedlang);
        //						}


        //						foreach (var cb in iq.RootBranch.childBranches.Values)
        //						{
        //							string btl = System.Convert.ToString(cb.Translation.text(English));
        //							if (btl.ToLower().Contains("accessories and services"))
        //							{
        //								int c = 0;
        //								cb.flagAsUnsearchable(c);
        //								Debug.Print(c);
        //								}
        //								}

        //								messages.Add("Loaded complete Object Model ");
        //								messages.Add("HP iQuote 2 version " + (new Microsoft.VisualBasic.ApplicationServices.ConsoleApplicationBase()).Info.Version.ToString());

        //If Not iq.Root Is Nothing Then
        //    LastMilestone = Stopwatch.GetTimestamp
        //    IndexSKUs()
        //    txt &= "Indexed SKUs"
        //End If
        //								messages.Add(LoadUserStates(con, r));
        //								messages.Add(LoadProductValidations(con, r));
        //								this.loadedTimestamp = DateTime.Now;
        //								con.Close();
        //p.Controls.Add(NewLit())

        //								StopBytes = System.Convert.ToInt64(System.GC.GetTotalMemory(true));
        //								messages.Add("Total OM size (minus channel specific pricing) now  :" + System.Convert.ToString(Int(System.Convert.ToDouble(StopBytes - StartBytes) / (Math.Pow(1024, 2)))) + "MB");

        //  p.Controls.Add(OutputErrors(errorMessages, lid))

        //								checkEssentials();

        //								_instance.IsLoaded = true;

        //								if (StartBytes != null)
        //								{
        //									EndBytes = System.GC.GetTotalMemory(true);
        //									if (StopBytes - StartBytes > 0)
        //									{
        //										messages.Add("Object model reload freeing approximately " + System.Convert.ToString(Int(System.Convert.ToDouble(StopBytes - StartBytes) / (Math.Pow(1024, 2)))) + "MB");
        //										}
        //										else
        //										{
        //											messages.Add("Object model load taking apporx " + System.Convert.ToString(Int(System.Convert.ToDouble(StartBytes - StopBytes) / (Math.Pow(1024, 2)))) + "MB");
        //											}
        //											}

        //											messages.StopClock();

        // Dim count As Integer = 0
        // RecurseChildren(iq.Root, count, "Tree")
        // load &= "<b>Walked tree of " & count.ToString("###,###,###,###") & " options" & TimeSince(Start) & "<br/></b>"

        //Catch ex As Exception
        //messages.Add("Exception: " + ex.Message)
        //        End Try




        //											clsBranch mastercpusbranch = null;
        //											foreach (var branch in iq.Branches.Values)
        //											{
        //												if (branch.Translation.text(English).ToUpper == "CPU")
        //												{
        //													if (branch.childBranches.Count > 30)
        //													{
        //														mastercpusbranch = branch;
        //														break;
        //														}

        //														}
        //														}

        //														if (mastercpusbranch == null)
        //														{
        //															Debugger.Break();
        //															}

        //															cleanChassisBranches();


        //															IsLoading = false;
    }
    public Dictionary<UInt64, clsUserState> OldUserSessions; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.

    public void cleanChassisBranches()
    {

        List<string> em = new List<string>();
        string summary = "";

        Dictionary<string, int> counts = new Dictionary<string, int>();

        foreach (var b in iq.Branches.Values.ToList)
        {
            if (b.Product != null)
            {
                if (b.Product.ProductType.Code.ToLower == "chas")
                {
                    if (b.Translation.text(English) != "CPU")
                    {
                        string pd = System.Convert.ToString(b.EnglishName);
                        if (!pd.ToLower().EndsWith(" chassis"))
                        {
                            int a = 9;
                            summary = "";
                            b.HardDelete(em, summary, 0, true, counts);
                        }
                    }
                }
            }
        }

        foreach (var k in counts.Keys)
        {
            Debug.Print(k + " - " + System.Convert.ToString(counts[k]));
        }

    }



    private string LoadProductValidations(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {
        try
        {
            ProductValidationsAssignment = new Dictionary<string, List<clsProductValidation>>();
            r = da.DBExecuteReader(con, "Select ProductValidations.Id,SystemType,[OptType],[ValidationType],[Severity],[FK_Translation_Key_Message],[RequiredQuantity],[CheckAttribute],[DependantOptType],DependantCheckAttribute,DependantCheckAttributeValue,CheckAttributeValue,FK_Translation_Key_CorrectMessage,ValidationMessageType,[LinkTechnology],[LinkOptType],[LinkOptionFamily] from ProductValidations INNER JOIN ProductValidationMappings on FK_ProductValidation_ID = Id");

            int duds = 0;
            if (r.HasRows)
            {
                while (r.Read)
                {
                    if (!ProductValidationsAssignment.ContainsKey(r("SystemType").ToString()))
                    {
                        ProductValidationsAssignment.Add(r("SystemType").ToString(), new List<clsProductValidation>());
                    }


                    int mkey = System.Convert.ToInt32(r.Item("FK_Translation_Key_Message"));
                    int cmkey = System.Convert.ToInt32(r.Item("FK_Translation_Key_correctMessage"));

                    if (iq.Translations.ContainsKey(mkey) && iq.Translations.ContainsKey(cmkey))
                    {

                        clsTranslation mt = iq.Translations(mkey);
                        clsTranslation cmt = iq.Translations(cmkey);

                        clsProductValidation pv = new clsProductValidation()
                        {
                            ID = r("ID"),
                            CheckAttribute = r("CheckAttribute").ToString(),
                            DependantOptType = r("DependantOptType").ToString(),
                            Message = iq.Translations(r("FK_Translation_Key_Message")),
                            CorrectMessage = ((iq.Translations.ContainsKey(r("FK_Translation_Key_CorrectMessage"))) ? (iq.Translations(r("FK_Translation_Key_CorrectMessage"))) : null),
                            RequiredOptType = r("OptType").ToString(),
                            RequiredQuantity = r("RequiredQuantity"),
                            Severity = Enum.Parse(typeof(EnumValidationSeverity), r("Severity").ToString()),
                            ValidationType = Enum.Parse(typeof(enumValidationType), r("ValidationType").ToString()),
                            DependantCheckAttribute = ((r("DependantCheckAttribute") == DBNull.Value) ? null : (r("DependantCheckAttribute"))),
                            DependantCheckAttributeValue = ((r("DependantCheckAttributeValue") == DBNull.Value) ? null : (r("DependantCheckAttributeValue"))),
                            CheckAttributeValue = ((r("CheckAttributeValue") == DBNull.Value) ? null : (r("CheckAttributeValue"))),
                            ValidationMessageType = Enum.Parse(typeof(enumValidationMessageType), r("ValidationMessageType").ToString()),
                            LinkOptType = r("LinkOptType").ToString(),
                            LinkTechnology = r("LinkTechnology").ToString(),
                            LinkOptionFamily = r("LinkOptionFamily").ToString()
                        };

                        ProductValidationsAssignment[r("SystemType").ToString()].Add(pv);
                    }
                    else
                    {
                        duds++;

                    }
                }
            }

        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return ex.Message;
        }

    }

    private string LoadUserStates(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {
        try
        {
            r = da.DBExecuteReader(con, "Select states from UserStates WHERE HostName=\'" + Environment.MachineName + "\' order by datetime desc");
            iq.seshDic = new Dictionary<UInt64, Dictionary<string, object>>();

            if (r.HasRows)
            {
                while (r.Read)
                {

                    List<clsUserState> states = new List<clsUserState>();
                    XmlSerializer x = new XmlSerializer(states.GetType());
                    using (StringReader t = new StringReader(r("states")))
                    {
                        states = x.Deserialize(t);
                        foreach (var us in states)
                        {
                            if (!iq.OldUserSessions.ContainsKey(us.lid)) //Nick added as a 'quick fix' 10/12/2014 10:58
                            {
                                iq.OldUserSessions.Add(us.lid, us);
                            }
                        }
                    }

                }
            }
            return string.Format("Importing existing user sessions: {0} imported", iq.seshDic.Count);
        }
        catch (Exception ex)
        {
            return "Failed: " + ex.Message;
        }
    }

    public bool LoadUserState(UInt64 lid)
    {

        bool loaded = false;

        if (iq.OldUserSessions.ContainsKey(lid))
        {

            try
            {

                clsUserState us = iq.OldUserSessions(lid);
                if (iq.seshDic.ContainsKey(us.lid))
                {
                    return false;
                }

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
                clsAccount buyerAccount = (clsAccount)(iq.sesh(us.lid, "BuyerAccount"));
                clsAccount agentAccount = (clsAccount)(iq.sesh(us.lid, "AgentAccount"));

                TagPromoBranches(buyerAccount, errorMessages);
                agentAccount.SellerChannel.IsCloneOf.LoadVariants(errorMessages, 0.1);

                if (!agentAccount.SellerChannel.IsCloneOf.stockLoaded)
                {
                    agentAccount.SellerChannel.IsCloneOf.LoadStock();
                }

                if (!agentAccount.SellerChannel.IsCloneOf.pricesLoadedFor.ContainsKey(agentAccount.Priceband))
                {
                    agentAccount.SellerChannel.IsCloneOf.LoadPrices(agentAccount.Priceband, errorMessages);
                }

                clsRegion rgn = agentAccount.SellerChannel.IsCloneOf.Region;
                if (!HP.listPricesLoadedFor.ContainsKey(rgn) || HP.listPricesLoadedFor(rgn) == 0)
                {
                    HP.LoadPrices(Everyone, errorMessages, agentAccount.SellerChannel.Region);
                }

                //Add quote in
                agentAccount.LoadQuotes(Val(buyerAccount.ID));
                if (us.QuoteID != null && us.QuoteID != 0)
                {
                    agentAccount.Quotes(us.QuoteID).LoadItems(errorMessages);
                }

                //Matrix Headers
                iq.seshDic(us.lid).Add("screenHeaders", new Dictionary<string, clsScreenHeader>());
                System.Object mhs = (Dictionary<string, clsScreenHeader>)(iq.sesh(us.lid, "screenHeaders"));
                foreach (var mh in us.ScreenHeaders)
                {
                    clsBranchInfo bi = new clsBranchInfo(us.lid, mh.Path, null, 70, enumParadigm.errorNotSet, errorMessages); //Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
                    object descendants = bi.visibleChildren(errorMessages, true, 0, 0, false, true);
                    clsScreenHeader nmh = new clsScreenHeader(bi, descendants, mh.QuickFiltersVisible);
                    foreach (var f in mh.Filters)
                    {
                        Dictionary<clsFilter, List<long>> d = new Dictionary<clsFilter, List<long>>();
                        foreach (var fil in f.Value)
                        {
                            d.Add(iq.Filters(fil.Key.ID), fil.Value);
                        }
                        nmh.Filters.Add(iq.Fields(f.Key), d);
                    }
                    foreach (var s in mh.Sorts)
                    {
                        if (nmh.sorts.ContainsKey(s.Key))
                        {
                            nmh.sorts(s.Key).columnid = s.Value.columnid;
                            nmh.sorts(s.Key).Direction = s.Value.Direction;
                            nmh.sorts(s.Key).Priority = s.Value.Priority;
                        }
                        else
                        {
                            nmh.sorts.Add(s.Key, s.Value);
                        }

                    }
                }

                // Pick up any mop-up values
                if (!(us.mopUpvalues == null))
                {
                    foreach (var kvp in us.mopUpvalues)
                    {
                        if (!iq.seshDic(us.lid).ContainsKey(kvp.Key))
                        {
                            iq.seshDic(us.lid).Add(kvp.Key, kvp.Value);
                        }
                    }
                }

                loaded = true;

            }
            catch (Exception ex)
            {

                ErrorLog.Add(ex);

            }

        }

        return loaded;

    }

    private string LoadFilters()
    {

        Filters = new Dictionary<int, clsFilter>();

        clsFilter afilter;

        afilter = new clsFilter(1, "SW", iq.AddTranslation("Starts with", English, "FT", 0, null, 0, false), "[col] LIKE \'[filterValue]*\'");
        afilter = new clsFilter(2, "EW", iq.AddTranslation("Ends with", English, "FT", 0, null, 0, false), "[col] LIKE \'*[filterValue]\'");
        afilter = new clsFilter(3, "CN", iq.AddTranslation("Contains", English, "FT", 0, null, 0, false), "[col] LIKE \'*[filterValue]*\'");

        //afilter = New clsFilter(4, "ONLY", iq.AddTranslation("Only", English, "FT"), "[col]= '[filterValue]'")             '
        // afilter = New clsFilter(5, "EX", iq.AddTranslation("Excluding", English, "FT"), "[col]<>'[filterValue]'")

        afilter = new clsFilter(4, "ONLY", iq.AddTranslation("Only", English, "FT", 0, null, 0, false), "[col]= [filterValue]"); //NOTE - these filter by the numeric values - which is faster
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
        afilter = new clsFilter(17, "WITHOUT", iq.AddTranslation("Without", English, "FT", 0, null, 0, false), "[col]=0"); //"(isnull([col],-100)=-100)")

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

        try
        {
            return iq.Users(UserID).Email + "/" + iq.Accounts(AccountID).SellerChannel.Name;
        }
        catch
        {
            return "invalid/unknown";
        }


    }
    public void RecurseChildren(clsBranch parent, ref int count, object path)
    {

        //walks' the tree of every possible branch - soley to count them

        object bp = null;
        foreach (var child in parent.childBranches.Values)
        {
            bp = path + "." + child.ID;
            //If Not Prunes.ContainsKey(bp$) Then 'dont recurse into pruned braches
            count++;
            RecurseChildren(child, ref count, bp);
            //End If
        }

    }
    public bool Retract(clsBranch branch, object username, List<string> errormessages)
    {

        //reParents each child of Branch under Branches' parent

        //Check that we are only retracting a branch containing other items and not a 'model' with a quantity etc
        if (branch.HasSKU())
        {
            errorMessages.Add("!Error - cannot retract a branch with a SKU, it has no children!");
            return false;
        }


        List<clsBranch> tomove = new List<clsBranch>(); //we need to build a list - becuase we can't iterate a collection we're modifying (branch.childbranches)

        foreach (var child in branch.childBranches.Values)
        {
            tomove.Add(child);
        }

        foreach (var child in tomove)
        {
            child.Parent = branch.Parent;
            //  child.Parent.childBranches.Add(child.ID, child)  'so it appears immediately
            child.Update(errormessages);
        }

        //finally remove the branch we're retracting

        branch.delete(errormessages);

        return true;

    }
    public int Prune(string path, string userName) //prunes are done by path (branch is not unique or specific!)
    {

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
        clsPrune aprune = default(clsPrune);
        aprune = new clsPrune(path, new NullableInt(), userName);
        return aprune.ID;
        //       End If

    }
    private string LoadProductAttributes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {
        try
        {
            int count = 0;
            object sql = null;

            //sql$ = "Select productattribute.id,FK_Product_Id,FK_Attribute_id,Numericvalue,fk_translation_key_text, a.code as acode,u.code as ucode from ProductAttribute "
            // sql$ &= "Join [attribute] a on a.id=fk_attribute_id "
            // sql$ &= "Join unit u on u.id=fk_unit_id"
            sql = "Select id,FK_Product_Id,FK_Attribute_id,Numericvalue,fk_translation_key_text, fk_unit_id  from ProductAttribute ";
            sql += "WHERE deleted = 0";

            r = da.DBExecuteReader(con, sql);

            int failed = 0;
            clsProductAttribute PA = default(clsProductAttribute); // the act of creating a product attribute - adds it to the specified product
            int indexedSKUs;
            float numval = 0;
            int translationKey = 0;

            int missingProducts = 0;
            int missingAttributes = 0;
            int missingUnits = 0;
            int bad = 0;

            HashSet<clsProductAttribute> toupdate = new HashSet<clsProductAttribute>(); //runtime fix for rempapping productacttributes of duplictaed products (this code can be removed in the long term)
            HashSet<string> todel = new HashSet<string>(); //

            if (r.HasRows)
            {
                while (r.Read)
                {
                    int aId = System.Convert.ToInt32(r.Item("fk_attribute_id"));
                    if (iq.Attributes.ContainsKey(aId))
                    {
                        clsAttribute attrib = iq.Attributes(aId);
                        numval = System.Convert.ToSingle(r.Item("NumericValue"));
                        int uId = System.Convert.ToInt32(r.Item("fk_unit_id"));

                        bool updateIt = false;
                        if (iq.Units.ContainsKey(uId))
                        {
                            clsUnit unit = iq.Units(uId);
                            clsProduct product = null;
                            int pid = System.Convert.ToInt32(r.Item("FK_product_id"));

                            if (iq.Products.ContainsKey(pid))
                            {
                                product = iq.Products(pid);
                            }
                            else
                            {
                                if (REMAPS.ContainsKey(pid))
                                {
                                    product = REMAPS[pid]; //UGLY LIVE FIXUP OF DATA
                                    if (product.i_Attributes_Code.ContainsKey(attrib.Code))
                                    {
                                        todel.Add(r.Item("ID"));
                                        continue;
                                    }
                                    else
                                    {
                                        updateIt = true; //
                                    }
                                }
                                else
                                {
                                    //Beep()
                                    product = null;
                                }
                            }

                            if (product == null)
                            {
                                missingProducts++;
                            }
                            else
                            {
                                clsTranslation tl = default(clsTranslation);
                                if (!Information.IsDBNull(r.Item("fk_translation_key_text")))
                                {
                                    translationKey = System.Convert.ToInt32(r.Item("fk_translation_key_text"));
                                    if (iq.Translations.ContainsKey(translationKey))
                                    {
                                        tl = iq.Translations(translationKey);
                                    }
                                    else
                                    {
                                        tl = null;
                                    }
                                }
                                else
                                {
                                    tl = null;
                                }

                                PA = new clsProductAttribute(System.Convert.ToInt32(r.Item("Id")), product, attrib, numval, unit, tl);

                                if (updateIt)
                                {
                                    toupdate.Add(PA);
                                }

                                count++;
                            }
                        }
                        else
                        {
                            missingUnits++;
                        }
                    }
                    else
                    {
                        missingAttributes++;
                    }

                }
            }
            r.Close();

            if (todel.Count)
            {
                da.DBExecutesql("DELETE FROM productattribute WHERE id in (" + string.Join(",", todel.ToArray) + ")");
            }
            foreach (clsProductAttribute tempLoopVar_PA in toupdate)
            {
                PA = tempLoopVar_PA;
                PA.update(errorMessages);
            }



            return "Loaded " + System.Convert.ToString(count) + " ProductAttributes. " + System.Convert.ToString(missingAttributes) + "Missing attributes" + "," + System.Convert.ToString(missingUnits) + " missing units, " + System.Convert.ToString(missingProducts) + " missing (deleted ?) products." + " Deleted " + todel.Count + " duplicate productattributes, updated (remapped) " + toupdate.Count;
        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return "Failed: " + ex.Message;
        }
    }

    private string CleanProducts(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        // Build a temporary list of PL code to Manufacturer mappings
        Dictionary<string, string> plcodeLookup = new Dictionary<string, string>();
        string Sql = "Select plCode, mfrCode from PLCodeLookup";
        r = da.DBExecuteReader(con, Sql);
        if (r.HasRows)
        {
            while (r.Read)
            {
                plcodeLookup.Add(r.Item("plCode"), r.Item("mfrCode"));
            }
        }
        r.Close();

        foreach (var product in iq.Products.Values)
        {

            bool update = false;

            // Fill in any missing Product.SKU values from the relevant attribute
            if (string.IsNullOrEmpty(System.Convert.ToString(product.SKU)))
            {
                if (product.Attributes != null)
                {
                    if (product.i_Attributes_Code.ContainsKey("mfrsku") && product.i_Attributes_Code("mfrsku").Count() > 0)
                    {
                        product.SKU = product.i_Attributes_Code("mfrsku")[0].Translation.text(English);
                        update = true;
                    }
                    //    If (product.isOption Or product.isSystem) And String.IsNullOrEmpty(product.SKU) Then Stop
                }
            }

            // Fill in any missing Product.mfrCode values
            if (string.IsNullOrEmpty(System.Convert.ToString(product.mfrCode)))
            {
                if (product.Attributes != null)
                {
                    if (product.i_Attributes_Code.ContainsKey("mfrsku") && product.i_Attributes_Code("mfrsku").Count() > 0)
                    {
                        if (!product.i_Attributes_Code("mfrsku")[0].Attribute.Translation.text(English).Contains("###"))
                        {
                            if (product.isSystem)
                            {
                                if (product.ProductType.Code == "DTO" || product.ProductType.Code == "NBK") // Desktop/Notebook
                                {
                                    product.mfrCode = "HPI";
                                }
                                else if (!(product.ProductType.Code == "WTY") && !(product.ProductType.Code == "EDU") && !(product.ProductType.Code == "SVC"))
                                {
                                    product.mfrCode = "HPE";
                                }
                                update = true;
                            }
                            else
                            {
                                if (product.i_Attributes_Code.ContainsKey("plcode") && product.i_Attributes_Code("plcode").Count() > 0)
                                {
                                    string plCode = System.Convert.ToString(product.i_Attributes_Code("plcode")[0].Translation.text(English));
                                    if (plcodeLookup.ContainsKey(plCode))
                                    {
                                        product.mfrCode = plcodeLookup[plCode];
                                        update = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (update)
            {
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
        while (r.Read)
        {

            clsResponse response = new clsResponse();

            response.ID = r.Item("ID");
            if (!Information.IsDBNull(r.Item("mfrCode")))
            {
                response.mfrCode = r.Item("mfrCode");
            }

            int transKey = System.Convert.ToInt32(r.Item("FK_Translation_Key_Title"));
            if (iq.Translations.ContainsKey(transKey))
            {
                response.Title = iq.Translations(transKey);
            }

            transKey = System.Convert.ToInt32(r.Item("FK_Translation_Key_Description"));
            if (iq.Translations.ContainsKey(transKey))
            {
                response.Description = iq.Translations(transKey);
            }

            if (!Information.IsDBNull(r.Item("ResponseDefault")))
            {
                response.ResponseDefault = r.Item("ResponseDefault");
            }


            iq.ServiceLevelResponse.Add(response.ID, response);

        }
        r.Close();

        // Service Type
        r = da.DBExecuteReader(con, "SELECT [ID], [mfrCode], [FK_Translation_Key_Title], [FK_Translation_Key_Description], [ServiceTypeDefault] FROM [ServiceType]");
        while (r.Read)
        {

            clsServiceType serviceType = new clsServiceType();

            serviceType.ID = r.Item("ID");
            if (!Information.IsDBNull(r.Item("mfrCode")))
            {
                serviceType.mfrCode = r.Item("mfrCode");
            }

            int transKey = System.Convert.ToInt32(r.Item("FK_Translation_Key_Title"));
            if (iq.Translations.ContainsKey(transKey))
            {
                serviceType.Title = iq.Translations(transKey);
            }

            if (!Information.IsDBNull(r.Item("FK_Translation_Key_Description")))
            {
                transKey = System.Convert.ToInt32(r.Item("FK_Translation_Key_Description"));
                if (iq.Translations.ContainsKey(transKey))
                {
                    serviceType.Description = iq.Translations(transKey);
                }
            }

            if (!Information.IsDBNull(r.Item("ServiceTypeDefault")))
            {
                serviceType.ServiceTypeDefault = r.Item("ServiceTypeDefault");
            }


            iq.ServiceLevelServiceType.Add(serviceType.ID, serviceType);

        }
        r.Close();

        // Service Level Map
        r = da.DBExecuteReader(con, "SELECT [ID], [mfrCode], [ServiceLevel], [ServiceLevelGroup], [SuperGroup], [FK_Translation_Key_Description], [Duration], [PostWarranty], [Disabled], [FK_ServiceType_ID], [FK_Response_ID], [hpeDMR], [hpeCDMR], [hpiADP], [hpiDMR], [hpiTravel], [hpiTracing], [hpiTheft] FROM [ServiceLevelMap]");
        while (r.Read)
        {

            clsServiceLevel serviceLevel = new clsServiceLevel();

            serviceLevel.ID = r.Item("ID");
            if (!Information.IsDBNull(r.Item("mfrCode")))
            {
                serviceLevel.MfrCode = r.Item("mfrCode");
            }
            if (!Information.IsDBNull(r.Item("ServiceLevel")))
            {
                serviceLevel.ServiceLevel = r.Item("ServiceLevel");
            }
            if (!Information.IsDBNull(r.Item("ServiceLevelGroup")))
            {
                serviceLevel.ServiceLevelGroup = r.Item("ServiceLevelGroup");
            }
            if (!Information.IsDBNull(r.Item("SuperGroup")))
            {
                serviceLevel.SuperGroup = r.Item("SuperGroup");
            }

            int transKey = System.Convert.ToInt32(r.Item("FK_Translation_Key_Description"));
            if (iq.Translations.ContainsKey(transKey))
            {
                serviceLevel.Description = iq.Translations(transKey);
            }

            if (!Information.IsDBNull(r.Item("Duration")))
            {
                serviceLevel.Duration = r.Item("Duration");
            }
            if (!Information.IsDBNull(r.Item("PostWarranty")))
            {
                serviceLevel.PostWarranty = r.Item("PostWarranty");
            }
            if (!Information.IsDBNull(r.Item("Disabled")))
            {
                serviceLevel.Disabled = r.Item("Disabled");
            }

            if (!Information.IsDBNull(r.Item("FK_Response_ID")))
            {
                int responseID = System.Convert.ToInt32(r.Item("FK_Response_ID"));
                if (iq.ServiceLevelResponse.ContainsKey(responseID))
                {
                    serviceLevel.Response = iq.ServiceLevelResponse(responseID);
                }
            }

            if (!Information.IsDBNull(r.Item("FK_ServiceType_ID")))
            {
                int serviceTypeID = System.Convert.ToInt32(r.Item("FK_ServiceType_ID"));
                if (iq.ServiceLevelServiceType.ContainsKey(serviceTypeID))
                {
                    serviceLevel.ServiceType = iq.ServiceLevelServiceType(serviceTypeID);
                }
            }

            if (!Information.IsDBNull(r.Item("hpeDMR")))
            {
                serviceLevel.HpeDmr = r.Item("hpeDMR");
            }
            if (!Information.IsDBNull(r.Item("hpeCDMR")))
            {
                serviceLevel.HpeCdmr = r.Item("hpeCDMR");
            }
            if (!Information.IsDBNull(r.Item("hpiADP")))
            {
                serviceLevel.HpiAdp = r.Item("hpiADP");
            }
            if (!Information.IsDBNull(r.Item("hpiDMR")))
            {
                serviceLevel.HpiDmr = r.Item("hpiDMR");
            }
            if (!Information.IsDBNull(r.Item("hpiTravel")))
            {
                serviceLevel.HpiTravel = r.Item("hpiTravel");
            }
            if (!Information.IsDBNull(r.Item("hpiTracing")))
            {
                serviceLevel.HpiTracing = r.Item("hpiTracing");
            }
            if (!Information.IsDBNull(r.Item("hpiTheft")))
            {
                serviceLevel.HpiTheft = r.Item("hpiTheft");
            }

            iq.ServiceLevels.Add(serviceLevel.ID, serviceLevel);

        }
        r.Close();

        // TROAA
        r = da.DBExecuteReader(con, "SELECT [ID], [SysFamily], [SlotTypeCode], [ServiceLevel], [DisplayOrder], [FK_ServiceLevelMap_ID] FROM [TROAA]");
        while (r.Read)
        {

            clsTROAA troaa = new clsTROAA();

            troaa.ID = r.Item("ID");
            if (!Information.IsDBNull(r.Item("SysFamily")))
            {
                troaa.SysFamily = r.Item("SysFamily");
            }
            if (!Information.IsDBNull(r.Item("SlotTypeCode")))
            {
                troaa.SlotTypeCode = r.Item("SlotTypeCode");
            }
            if (!Information.IsDBNull(r.Item("ServiceLevel")))
            {
                troaa.ServiceLevelID = r.Item("ServiceLevel");
            }
            if (!Information.IsDBNull(r.Item("DisplayOrder")))
            {
                troaa.DisplayOrder = r.Item("DisplayOrder");
            }
            if (!Information.IsDBNull(r.Item("FK_ServiceLevelMap_ID")))
            {
                int serviceLevelID = System.Convert.ToInt32(r.Item("FK_ServiceLevelMap_ID"));
                if (iq.ServiceLevels.ContainsKey(serviceLevelID))
                {
                    troaa.ServiceLevel = iq.ServiceLevels(serviceLevelID);
                }
            }

            iq.ServiceLevelTROAA.Add(troaa.ID, troaa);

        }
        r.Close();

        // Service Level Attribute Map
        r = da.DBExecuteReader(con, "SELECT [ID], [Code], [FK_Attribute_Code] FROM [ServiceLevelAttributeMap]");
        while (r.Read)
        {

            string code = System.Convert.ToString(r.Item("Code"));
            string serviceLevelAttributeCode = System.Convert.ToString(r.Item("FK_Attribute_Code"));

            if (iq.i_attribute_code.ContainsKey(serviceLevelAttributeCode))
            {
                iq.ServiceLevelAttributeMap.Add(code, iq.i_attribute_code(serviceLevelAttributeCode));
            }

        }
        r.Close();


        return "Loaded HP Service Pack Levels";

    }


    public string LoadPoints(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {


        r = da.DBExecuteReader(con, "SELECT ID,fk_Product_id,fk_scheme_id, points FROM [Points]");

        clsProduct product = default(clsProduct);
        clsScheme scheme = default(clsScheme);

        int points = 0;
        while (r.Read)
        {
            if (iq.Products.ContainsKey(System.Convert.ToInt32(r.Item("fk_product_id"))))
            {

                int pid = System.Convert.ToInt32(r.Item("fk_product_id"));
                if (iq.Products.ContainsKey(pid))
                {
                    product = iq.Products(pid);
                }
                else
                {
                    product = iq.REMAPS(pid);
                }

                scheme = iq.Schemes(System.Convert.ToInt32(r.Item("fk_scheme_id")));
                product.Points(scheme) = System.Convert.ToInt32(r.Item("Points"));
                points++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(points) + " loyalty points onto products";


    }


    public string LoadSchemes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        iq.Schemes.Clear();

        r = da.DBExecuteReader(con, "SELECT ID,code,fk_translation_key_name,StartDate,EndDate,fk_region_id FROM [Scheme]");

        clsScheme ascheme;

        int active = 0;

        int id = 0;
        string code = "";
        DateTime startdate = default(DateTime);
        DateTime enddate = default(DateTime);
        while (r.Read)
        {
            id = System.Convert.ToInt32(r.Item("Id"));
            code = System.Convert.ToString(r.Item("code").ToString());
            startdate = System.Convert.ToDateTime(r.Item("startdate"));
            enddate = System.Convert.ToDateTime(r.Item("enddate"));
            ascheme = new clsScheme(id, code, iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_name"))), iq.Regions(System.Convert.ToInt32(r.Item("fk_region_id"))), startdate, enddate);
            if (startdate < DateTime.Now && enddate > DateTime.Now)
            {
                active++;
            }
        }
        r.Close();

        return "Loaded " + iq.Schemes.Count + " loyalty schemes, " + System.Convert.ToString(active) + " of which " + System.Convert.ToString(active) + " are current/active.";


    }

    public string LoadBundles(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {


        //the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems
        r = da.DBExecuteReader(con, "SELECT ID,fk_translation_key_name,OPGref,code,validFrom,validTo,fk_region_id FROM [Bundle]");

        iq.Bundles.Clear();
        iq.i_Bundle_code.Clear();

        //Load the bundles
        clsRegion region = null;
        clsBundle Bundle = default(clsBundle);

        int Active = 0;
        clsTranslation bundleName = null;
        int tk = 0;
        DateTime fromDate = default(DateTime);
        DateTime toDate = default(DateTime);
        int regionID = 0;
        if (r.HasRows)
        {
            while (r.Read)
            {
                regionID = System.Convert.ToInt32(r.Item("fk_region_id"));
                tk = System.Convert.ToInt32(r.Item("fk_translation_key_name"));
                region = iq.Regions(regionID);
                fromDate = System.Convert.ToDateTime(r.Item("validfrom"));
                toDate = System.Convert.ToDateTime(r.Item("validto"));

                bundleName = iq.Translations(tk);
                Bundle = new clsBundle(System.Convert.ToInt32(r.Item("ID")), bundleName, r.Item("OPGref").ToString(), r.Item("code").ToString(), region, fromDate, toDate);
                if (DateTime.Now > Bundle.validFrom && DateTime.Now < Bundle.validTo)
                {
                    Active++; //count the 'active' bundles
                }
            }
        }
        r.Close();


        //load the items into the bundles

        r = da.DBExecuteReader(con, "SELECT ID,fk_Bundle_id,fk_product_id,price,rebate,fk_currency_id,qtyMin from BundleItem");

        clsBundleItem bundleitem;
        int bundleItems = 0;
        NullablePrice price = default(NullablePrice);
        int currencyId = 0;
        int bundleID = 0;
        int productID = 0;
        int rowID = 0;
        float rebate = 0;
        int minQty = 0;
        if (r.HasRows)
        {
            while (r.Read)
            {
                currencyId = System.Convert.ToInt32(r.Item("fk_currency_id"));
                price = new NullablePrice(r.Item("Price"), iq.Currencies(currencyId), false);
                bundleID = System.Convert.ToInt32(r.Item("fk_bundle_id"));
                productID = System.Convert.ToInt32(r.Item("fk_product_id"));
                rowID = System.Convert.ToInt32(r.Item("ID"));
                rebate = System.Convert.ToSingle(r.Item("rebate"));
                minQty = System.Convert.ToInt32(r.Item("qtyMin"));
                //see the constructor ... it adds the item to the bundle and
                Bundle = iq.Bundles(bundleID);
                bundleitem = new clsBundleItem(rowID, Bundle, iq.Products(productID), price, rebate, minQty);
                bundleItems++;
            }
        }
        r.Close();


        //Attach the bundles to the systems

        r = da.DBExecuteReader(con, "SELECT ID,fk_Bundle_id,fk_product_id_system,rebate from BundleSystem");

        clsBundleSystem bundlesystem;
        int bundleSystems = 0;
        clsProduct system = default(clsProduct);

        if (r.HasRows)
        {
            while (r.Read)
            {
                bundleID = System.Convert.ToInt32(r.Item("fk_bundle_id"));
                productID = System.Convert.ToInt32(r.Item("fk_product_id_system"));
                rowID = System.Convert.ToInt32(r.Item("ID"));
                Bundle = iq.Bundles(bundleID);
                system = iq.Products(productID);
                rebate = System.Convert.ToSingle(r.Item("rebate"));
                bundlesystem = new clsBundleSystem(rowID, Bundle, system, rebate);
                bundleSystems++;

            }
        }
        r.Close();

        return "Loaded " + iq.Bundles.Count + " bundles, " + System.Convert.ToString(Active) + " of which are current/active, applied to" + System.Convert.ToString(bundleSystems) + " systems ,There were a total of " + System.Convert.ToString(bundleItems) + " bundle items.";

    }



    public string LoadFlex(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        //the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems

        r = da.DBExecuteReader(con, "SELECT ID,description,OPGref,validFrom,validTo,fk_currency_id,minOptions,maxOptions,OPGSysType  FROM [Flex]");

        iq.FlexOPGs.Clear();

        //Load the FlexOPGs

        int active = 0;
        clsCurrency currency = default(clsCurrency);
        clsFlexOPG flexOPG = default(clsFlexOPG);
        int currencyID = 0;
        DateTime fromDate = default(DateTime);
        DateTime toDate = default(DateTime);
        int minOptions = 0;
        int maxOptions = 0;
        string OPGSysType = "";
        int rowID = 0;
        if (r.HasRows)
        {
            while (r.Read)
            {
                currencyID = System.Convert.ToInt32(r.Item("fk_currency_id"));
                fromDate = System.Convert.ToDateTime(r.Item("validfrom"));
                toDate = System.Convert.ToDateTime(r.Item("validto"));
                currency = iq.Currencies(currencyID);
                minOptions = System.Convert.ToInt32(r.Item("MinOptions"));
                maxOptions = System.Convert.ToInt32((Information.IsDBNull(r.Item("MaxOptions"))) ? null : (System.Convert.ToInt32(r.Item("MaxOptions"))));
                OPGSysType = System.Convert.ToString((Information.IsDBNull(r.Item("OPGSysType"))) ? "" : ((r.Item("OPGSysType")).ToString()));

                rowID = System.Convert.ToInt32(r.Item("ID"));
                flexOPG = new clsFlexOPG(rowID, r.Item("OPGref").ToString(), r.Item("Description").ToString(), fromDate, toDate, currency, minOptions, maxOptions, OPGSysType);
                if (flexOPG.isCurrent)
                {
                    active++; //count the 'active' bundles
                }
            }
        }
        r.Close();

        return "Loaded " + iq.FlexOPGs.Count + " Flex OPGs, " + System.Convert.ToString(active) + " of which are current/active";

    }

    public string LoadFlexRegions(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        //the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems

        r = da.DBExecuteReader(con, "SELECT ID,fk_flex_id,fk_region_id FROM [FlexRegion]");

        clsFlexOPG flexOPG = default(clsFlexOPG);
        int frs = 0;
        clsRegion region = default(clsRegion);
        int flexID = 0;
        int regionID = 0;
        if (r.HasRows)
        {
            while (r.Read)
            {
                flexID = System.Convert.ToInt32(r.Item("fk_flex_id"));
                regionID = System.Convert.ToInt32(r.Item("fk_region_id"));
                flexOPG = iq.FlexOPGs(flexID);
                region = iq.Regions(regionID);
                flexOPG.regions.Add(region.ID, region);
                frs++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(frs) + " Flex Regions ";

    }


    public string LoadFlexLines(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {
        //load the lines into the FlexOPGs

        r = da.DBExecuteReader(con, "SELECT ID,fk_flex_id,fk_product_id,rebate,validfrom,validto FROM FlexLine"); //- Rebate = Additionl disc% * list - do at import !!

        clsFlexLine flexline = default(clsFlexLine);

        int lines = 0;
        int activelines = 0;
        int flexID = 0;
        int pID = 0;
        decimal rebate = new decimal();
        DateTime fromDate = default(DateTime);
        DateTime toDate = default(DateTime);
        int rowID = 0;
        clsProduct product = default(clsProduct);

        if (r.HasRows)
        {
            while (r.Read)
            {
                flexID = System.Convert.ToInt32(r.Item("fk_flex_id"));
                pID = System.Convert.ToInt32(r.Item("fk_product_id"));
                rebate = System.Convert.ToDecimal(r.Item("rebate"));
                fromDate = System.Convert.ToDateTime(r.Item("validfrom"));
                toDate = System.Convert.ToDateTime(r.Item("validto"));
                rowID = System.Convert.ToInt32(r.Item("ID"));

                //                If FlexOPGs(flexID).OPGRef = "90989236" And iq.Products(productID).sku = "712317-421" Then Stop

                //  price = New nullablePrice(r.Item("Price"), iq.Currencies(r.Item("fk_currency_id")))

                if (iq.Products.ContainsKey(pID))
                {
                    product = iq.Products(pID);
                }
                else
                {
                    product = iq.REMAPS(pID);
                }

                flexline = new clsFlexLine(rowID, iq.FlexOPGs(flexID), product, rebate, fromDate, toDate);

                lines++;
                if (flexline.isCurrent)
                {
                    activelines++;
                }
            }
        }

        r.Close();

        return "There were a total of " + System.Convert.ToString(lines) + " Flex (product) lines, " + System.Convert.ToString(activelines) + " of which are current/active.";
    }
    public string LoadFlexRules(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        //   Dim rRule As SqlDataReader = da.DBExecuteReader(con, "SELECT [ID],[FK_ProductType_ID],[FK_Flex_ID],[min],[max] FROM [FlexRule]")
        r = da.DBExecuteReader(con, "SELECT [ID],[FK_ProductType_ID],[FK_Flex_ID],[min],[max],[optionalRule] FROM [FlexRule]");
        int lines = 0;
        clsFlexRule flexRules;
        int rowID = 0;
        int productTypeID = 0;
        int flexID = 0;
        int min = 0;
        int max = 0;
        bool optionalRule = false;
        if (r.HasRows)
        {
            while (r.Read)
            {
                rowID = System.Convert.ToInt32(r("ID"));
                productTypeID = System.Convert.ToInt32(r("FK_ProductType_ID"));
                flexID = System.Convert.ToInt32(r("FK_Flex_ID"));
                min = System.Convert.ToInt32(r("min"));
                max = System.Convert.ToInt32(r("max"));
                optionalRule = System.Convert.ToBoolean(r("optionalRule"));
                flexRules = new clsFlexRule(rowID, iq.FlexOPGs(flexID), iq.ProductTypes(productTypeID), min, max, optionalRule);
                lines++;
            }
        }
        r.Close();
        return "There were a total of " + System.Convert.ToString(lines) + " Flex (product) Rules ";
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
        int regionID = 0;
        DateTime fromDate = default(DateTime);
        DateTime toDate = default(DateTime);
        int rowID = 0;
        string opgRef = "";
        int optMin = 0;
        int optMax = 0;
        if (r.HasRows)
        {
            while (r.Read)
            {
                regionID = System.Convert.ToInt32(r.Item("fk_region_id"));
                fromDate = System.Convert.ToDateTime(r.Item("validfrom"));
                toDate = System.Convert.ToDateTime(r.Item("validto"));
                region = iq.Regions(regionID);
                rowID = System.Convert.ToInt32(r.Item("ID"));
                opgRef = System.Convert.ToString(r.Item("opgref").ToString());
                optMin = System.Convert.ToInt32(r.Item("optmin"));
                optMax = System.Convert.ToInt32(r.Item("optmax"));
                if (!iq.i_OpgRef.ContainsKey(opgRef))
                {
                    AvalancheOPG = new ClsAvalancheOPG(rowID, opgRef, region, fromDate, toDate, optMin, optMax);
                }
                else
                {
                    Interaction.Beep();
                }
            }
        }
        r.Close();

        //clear them all (as re have to re-load them during import)
        foreach (var product in iq.Products.Values)
        {
            if (product.AvalancheOPGs != null)
            {
                product.AvalancheOPGs.Clear();
            }

        }

        r = da.DBExecuteReader(con, "SELECT fk_product_id_system,fk_avalancheOPG_id FROM AvalancheSystem");
        int attached = 0;
        int productID = 0;
        int avalancheID = 0;
        if (r.HasRows)
        {

            while (r.Read)
            {
                productID = System.Convert.ToInt32(r.Item("fk_product_id_system"));
                avalancheID = System.Convert.ToInt32(r.Item("fk_avalancheOPG_id"));

                iq.Products(productID).AvalancheOPGs.Add(avalancheID, iq.AvalancheOPGs(avalancheID));
                attached++;
            }
        }
        r.Close();


        //now read the AvalancheOptions - which gives us the percent (of list) discount per 'ref code', under this OPG  (options have a refcode attribute)
        clsAvalancheOption opt;
        int opts = 0;

        r = da.DBExecuteReader(con, "SELECT ID,FK_AvalancheOPG_id,LPDiscountPercent,prodRef FROM avalancheOption");
        if (r.HasRows)
        {
            while (r.Read)
            {
                avalancheID = System.Convert.ToInt32(r.Item("fk_avalancheOPG_id"));
                rowID = System.Convert.ToInt32(r.Item("ID"));
                //creating the Avalance option adds it to the OPG (those OPG's have already been attached to the relevant systems
                opt = new clsAvalancheOption(rowID, iq.AvalancheOPGs(avalancheID), r.Item("Prodref").ToString(), System.Convert.ToSingle(r.Item("LPDiscountPercent")));
                opts++;
            }
        }
        r.Close();

        return "Loaded " + iq.AvalancheOPGs.Count + " Avalanche offers and attached them to " + System.Convert.ToString(attached) + " systems, Loaded discounts for " + System.Convert.ToString(opts) + " Refcodes ";

    }

    private string LoadSectors(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key_name from [Sector]");

        int count = 0;

        clsSector aSector;

        if (r.HasRows)
        {
            while (r.Read)
            {
                aSector = new clsSector(System.Convert.ToInt32(r.Item("id")), (r.Item("code")).ToString(), iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_name"))));
                count++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(count) + " sectors (Business Units) ";

    }
    private string LoadProducts(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT id,sku,IsSystem,IsOption,fk_producttype_id,fk_sector_id,activeFrom,activeTo,active,eol,publish,mfrCode,buCode,PLcode FROM Product where deleted = 0 order by id");

        iq.Products = new Dictionary<int, clsProduct>();

        clsProductType ProductType = default(clsProductType);

        int dupes = 0;

        List<string> todel = new List<string>();

        if (r.HasRows)
        {
            while (r.Read)
            {
                if (Information.IsDBNull(r.Item("FK_ProductType_ID")))
                {
                    ProductType = null;
                }
                else
                {
                    ProductType = iq.ProductTypes(System.Convert.ToInt32(r.Item("FK_ProductType_ID")));
                }
                //this should not be needed ! - remove !
                object sku = r("sku");

                //If sku$ = "QK765A" Then Stop

                if (sku != "" && iq.i_SKU.ContainsKey(sku))
                {
                    //DELETE THIS PRODUCT AND REMAP TO THE FIRST INSTANCE
                    //Stop
                    REMAPS.Add(r.Item("ID"), i_SKU[sku]);
                    todel.Add(r.Item("id"));
                    dupes++;
                }
                else
                {
                    clsProduct product = new clsProduct(System.Convert.ToInt32(r("id")), sku, System.Convert.ToBoolean(r("isSystem")), System.Convert.ToBoolean(r("isOption")), iq.Sectors(System.Convert.ToInt32(r("fk_sector_id"))), ProductType, System.Convert.ToDateTime(r("activefrom")), System.Convert.ToDateTime(r("activeto")), System.Convert.ToBoolean(r("active")), System.Convert.ToBoolean(r("EOL")), System.Convert.ToBoolean(r("Publish")), (r("mfrCode")).ToString(), (r("buCode")).ToString(), (r.Item("plCode")).ToString());

                    iq.Products.Add(System.Convert.ToInt32(r("id")), product);
                }

                //Else
                //duplicated product !
                // Beep()
                // dupes = +1
                // End If

            }
        }
        r.Close();


        if (todel.Count)
        {
            //soft delete the 2nd and subsequent versions of the product
            string sql = "update product set deleted =1 where id in (" + string.Join(",", todel.ToArray) + ")";

            da.DBExecutesql(sql);


        }



        return "Loaded " + iq.Products.Count + " products";

    }


    public string LoadTranslation(int Key)
    {

        //used to load a single translation ("worldwide") - as part of the bootstrap process

        SqlClient.SqlDataReader r = default(SqlClient.SqlDataReader);
        SqlClient.SqlConnection con = da.OpenDatabase;

        r = da.DBExecuteReader(con, "Select id,[key],[text],fk_language_id,[group],[order] from translation where key=" + System.Convert.ToString(Key));

        int id = 0;
        clsLanguage Lang = null;
        string Text = "";
        string group = "";
        int order = 0;

        clsTranslation aTranslation;

        if (r.HasRows)
        {

            while (r.Read)
            {
                id = System.Convert.ToInt32(r.Item("id"));
                Key = System.Convert.ToInt32(r.Item("key"));
                Lang = iq.Languages(System.Convert.ToInt32(r.Item("fk_language_id")));
                Text = System.Convert.ToString(r.Item("text").ToString());
                group = System.Convert.ToString(r.Item("group").ToString());
                order = System.Convert.ToInt32(r.Item("order"));


                //will also add it to the translations(key)(lang) dictionary
                if (iq.Translations.ContainsKey(Key))
                {
                    iq.Translations(Key).addLanguage(Lang, Text, null);

                }
                else
                {
                    aTranslation = new clsTranslation(Key, Lang, Text, id, group, order);
                }
            }
        }
        r.Close();

    }

    public string LoadTranslations(SqlClient.SqlConnection con)
    {

        SqlClient.SqlDataReader r = default(SqlClient.SqlDataReader);

        r = da.DBExecuteReader(con, "SELECT id,[key],[text],fk_language_id,[group],[order] FROM translation where deleted=0"); // where deleted=0
        int id = 0;
        clsLanguage Lang = null;
        int Key = 0;
        string Text = "";
        string group = "";
        int order = 0;

        clsTranslation aTranslation;

        iq.Translations.Clear();
        iq.iEnglishIndex.Clear();
        iq.KYIndex.Clear();

        if (r.HasRows)
        {

            clsTranslation tl = default(clsTranslation);

            while (r.Read)
            {
                id = System.Convert.ToInt32(r.Item("id"));
                Key = System.Convert.ToInt32(r.Item("key"));

                Lang = iq.Languages(System.Convert.ToInt32(r.Item("fk_language_id")));
                if (!Lang.Active)
                {
                    Lang.Active = true;
                }
                Text = System.Convert.ToString(r.Item("text").ToString());
                group = System.Convert.ToString(r.Item("group").ToString());
                order = System.Convert.ToInt32(r.Item("order"));

                //each translation object exposes:-
                //Property Text As Dictionary(Of clsLanguage, String)
                //Property ID As Dictionary(Of clsLanguage, Integer)

                //will also add it to the translations(key)(lang) dictionary
                if (!iq.Translations.ContainsKey(Key))
                {
                    tl = new clsTranslation(Key, Lang, Text, id, group, order);
                }
                else
                {
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
        if (r.HasRows)
        {
            while (r.Read)
            {
                //becuase we're specifying the ID here - it won't autmatically be written to the database

                string code = System.Convert.ToString(r.Item("code"));
                if (!i_unit_code.ContainsKey(code))
                {
                    // u = New clsUnit(CInt(r.Item("ID")), code, iq.Translations(CInt(r.Item("fk_translation_key_name"))), r.Item("Symbol").ToString())
                    u = new clsUnit(System.Convert.ToInt32(r.Item("ID")), Strings.Trim(System.Convert.ToString(r.Item("Code").ToString())), iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_name"))), r.Item("Symbol").ToString(), System.Convert.ToInt32(r.Item("FK_Measure_ID")));
                }
                else
                {
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
        string code = "";
        if (r.HasRows)
        {
            while (r.Read)
            {
                code = Strings.Trim(System.Convert.ToString(r.Item("code").ToString()));
                //The act of creating the language - adds it to the master list (see the clsLanguage constructor)
                //becuase we're specifying the ID here - it won't autmatically be written to the database
                if (!iq.i_language_Code.ContainsKey(code))
                {
                    l = new clsLanguage(System.Convert.ToInt32(r.Item("ID")), code, r.Item("Localname").ToString(), System.Convert.ToBoolean(r.Item("rtl")), System.Convert.ToBoolean(r.Item("live")), System.Convert.ToBoolean(r.Item("active")));
                }
            }
        }
        r.Close();
        if (!iq.i_language_Code.ContainsKey("KY"))
        {
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

        if (r.HasRows)
        {
            while (r.Read)
            {
                ex = new clsExclude(System.Convert.ToInt32(r.Item("id")), iq.Branches(System.Convert.ToInt32(r.Item("fk_branch_id_having"))), iq.Branches(System.Convert.ToInt32(r.Item("fk_branch_id_excludes"))), r.Item("reason").ToString());
            }
        }
        r.Close();

        return "Loaded " + Excludes.Count + " Excludes.";

    }
    private string LoadValidationsInclusions(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {
        ValidationInclusions = new Dictionary<int, clsValidationInclusion>();
        r = da.DBExecuteReader(con, "SELECT [id],MajorSlotType,MinorSlotType,InclusionType FROM [validationinclusion]");

        if (r.HasRows)
        {
            while (r.Read)
            {
                clsValidationInclusion ex = new clsValidationInclusion(System.Convert.ToInt32(r.Item("id")), r.Item("MajorSlotType"), (Information.IsDBNull(r.Item("MinorSlotType"))) ? null : (r.Item("MinorSlotType")), Enum.Parse(typeof(enumInclusionType), r.Item("inclusionType").ToString()));
            }

        }
        r.Close();

        return "Loaded " + Excludes.Count + " Excludes.";
    }

    private string LoadSpecialBranches(SqlClient.SqlConnection con, SqlClient.SqlDataReader r, List<string> errormessages)
    {

        //CPQ branch
        r = da.DBExecuteReader(con, "SELECT [id],FK_Branch_ID,Code FROM SpecialBranch");

        if (r.HasRows)
        {
            while (r.Read)
            {
                iq.i_SpecialBranches.Add(r("code"), iq.Branches(System.Convert.ToInt32(r("fk_branch_id"))));
            }
        }
        return "Special Branches Loaded";

    }


    private string LoadBranches(SqlClient.SqlConnection con, SqlClient.SqlDataReader r, List<string> errormessages)
    {

        //read ALL the branches in before attempting to create the structure (by grafting)

        string sql = "SELECT [id],[FK_branch_id_parent],[fk_product_id],[fk_translation_key],[picture],[fk_translation_key_collective],[fk_translation_key_collectiveSingular],fk_screen_id_matrix,[order],hidden,locked,rca  ";
        sql += "FROM [branch] WHERE deleted = 0 ORDER BY id";

        // Dim sql$  'Only load branches with live products
        // sql$ = "SELECT b.[id],[FK_branch_id_parent],[fk_product_id],[fk_translation_key],[picture],[fk_translation_key_collective],[fk_translation_key_collectiveSingular],"
        // sql$ &= "fk_screen_id_matrix, [order], hidden, locked, rca "
        // sql$ &= "FROM [branch] b left join product p on b.fk_product_id=p.id WHERE b.deleted=0 and p.deleted = 0 and p.active=1 and p.publish=1 ORDER BY b.id"

        r = da.DBExecuteReader(con, sql);
        clsBranch b;
        clsProduct product = default(clsProduct);
        clsBranch parent = null;
        int parentBranchId;

        StreamWriter sw = new StreamWriter("c:\\temp\\badbranches.txt");

        List<clsBranch> toDel = new List<clsBranch>();
        HashSet<int> toUpdate = new HashSet<int>(); //this is for remapping/deduping

        int mps;
        int rmps = 0;
        int neps = 0;

        int orphaned = 0;
        if (r.HasRows)
        {
            while (r.Read)
            {

                product = null;
                if (!Information.IsDBNull(r.Item("fk_product_id")))
                {
                    int pid = System.Convert.ToInt32(r.Item("fk_product_id"));

                    if (iq.Products.ContainsKey(pid)) //this product has been removed/deleted
                    {
                        product = iq.Products(pid);
                    }
                    else
                    {
                        if (REMAPS.ContainsKey(pid)) //do we have a rempap for this (duplicate) prodcut ..
                        {
                            product = iq.REMAPS(pid);
                            rmps++;
                            toUpdate.Add(r.Item("id")); //add the BRANCH to a list of branches we will need to update in the db becuase their product has been rempapped (once we've set their parent)
                            // sw.WriteLine("Branch " & r.Item("id") & " " & iq.Translations(r.Item("fk_translation_key")).text(English) & " references non existent (or deleted) product " & pid)

                        }
                        else
                        {
                            //oh dear - really broken (foreign key to a completely non existen product (but RI should never have allowed this ?)
                            //    Beep()
                            neps++;
                        }
                    }
                }

                clsScreen matrix = null;
                if (!Information.IsDBNull(r.Item("fk_screen_id_matrix")))
                {
                    matrix = iq.Screens(System.Convert.ToInt32(r.Item("fk_screen_id_matrix")));
                }

                //we don't set the parents yet - we need them ALL in first
                b = new clsBranch(System.Convert.ToInt32(r.Item("ID")), product, null, iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key"))), r.Item("picture").ToString(), iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_collective"))), iq.Translations(System.Convert.ToInt32(r.Item("fk_Translation_key_collectiveSingular"))), matrix, System.Convert.ToInt32(r.Item("order")), System.Convert.ToBoolean(r.Item("hidden")), System.Convert.ToBoolean(r.Item("locked")), (r.Item("rca")).ToString());

            }
            r = da.DBExecuteReader(con, sql);
            while (r.Read)
            {
                if (Information.IsDBNull(r.Item("fk_branch_id_parent")))
                {

                    parent = null; // There are also some branches with a null parent - these are 'floaters' and (generally) only appear in the tree as grafts
                }
                else if (System.Convert.ToInt32(r.Item("id")) == System.Convert.ToInt32(r.Item("fk_branch_id_parent")))
                {

                    parent = RootBranch; //any branch which is it's own parent - is added directly under the root level - this is a bit of a catchall/saftey net
                }
                else
                {
                    if (iq.Branches.ContainsKey(System.Convert.ToInt32(r.Item("fk_branch_id_parent")))) //THIS SHOULD NOT BE HERE - something is wrong - some parents are coming after their children
                    {
                        parent = iq.Branches(System.Convert.ToInt32(r.Item("fk_branch_id_parent")));
                    }
                    else
                    {
                        orphaned++;
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

                clsBranch branch = iq.Branches(System.Convert.ToInt32(r.Item("id")));
                branch.SetParent(parent);
                if (branch == parent && branch.ID != 1)
                {
                    Debugger.Break(); //a branch cannot be its own parent
                }


                //there are 6,000 plus of these need sorting out
                //     If branch.HasSiblingWithSameProduct Then todel.Add(branch)


            }
        }

        //  Debug.Print(neps)

        sw.Close();

        r.Close();


        //update the FK_Product_id based on the mappings to fix any duplicate
        List<string> erromessages = new List<string>();
        foreach (var bid in toUpdate)
        {
            iq.Branches(bid).Update(errormessages);
        }



        string graftinfo = "";
        graftinfo = LoadGrafts(con, errormessages);

        string pruneInfo = "";
        pruneInfo = loadPrunes(con, errormessages);

        object l = null;
        l = "Loaded " + iq.Branches.Count + " branches, " + graftinfo + "," + pruneInfo + " " + System.Convert.ToString(neps) + " missing/deleted products. " + System.Convert.ToString(rmps) + " products are duped (and being remapped)";

        Debug.Print(toDel.Count);


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


        return System.Convert.ToString(l);

    }
    public string loadPrunes(SqlClient.SqlConnection con, List<string> errormessages)
    {


        //Prune paths should be distinct - this is 'self healing' code to fix up mess in UAT/Production and dev databases (without having to run scripts all over the place)

        HashSet<string> distinctPaths = new HashSet<string>();
        HashSet<string> toDel = new HashSet<string>();

        int Prunes = 0;
        SqlClient.SqlDataReader r = default(SqlClient.SqlDataReader);
        r = da.DBExecuteReader(con, "SELECT id,Path,fk_channel_id,source,created FROM Prune");

        clsPrune aPrune;
        if (r.HasRows)
        {
            while (r.Read)
            {
                //use iq.channel(.Item("fk_channel_id"))) here to impliment scoped prunes
                if (distinctPaths.Contains(r.Item("path")))
                {
                    //duplicate prune (store the Prunes ID for deletion
                    toDel.Add(r.Item("id").ToString());
                }
                else
                {
                    if (!iq.Branches.ContainsKey(Strings.Split(System.Convert.ToString(r.Item("Path")), ".").Last))
                    {
                        //       toDel.Add(r.Item("id").ToString)  'prune of a non-existent branch
                    }
                    else
                    {
                        distinctPaths.Add(r.Item("path"));
                        aPrune = new clsPrune(System.Convert.ToInt32(r.Item("ID")), r.Item("path").ToString(), new NullableInt(), r.Item("source").ToString(), System.Convert.ToDateTime(r.Item("created")));
                        //iq.Branches(Split(r.Item("path"), ".").Last).Prunes.Add(r.Item("path"))
                        Prunes++;
                    }
                }
            }
        }
        r.Close();

        if (toDel.Count)
        {

            int toskip = 0;
            int chunk = 1000;
            do
            {
                System.Object l = from j in toDel.Skip(toskip).Take(chunk) select j;
                if (!l.Any)
                {
                    break;
                }
                string sql = "DELETE FROM PRUNE WHERE ID IN(" + string.Join(",", l.ToArray) + ")";

                LongSQL(sql);
                toskip += 1000;
            } while (true);

        }

        return "Loaded " + System.Convert.ToString(Prunes) + " Prunes, (Deleted " + toDel.Count + ")";


    }



    public string LoadGrafts(SqlClient.SqlConnection con, List<string> errorMessages)
    {

        SqlClient.SqlDataReader r = default(SqlClient.SqlDataReader);
        clsBranch source = default(clsBranch);
        clsBranch Target = default(clsBranch);

        //This is run tiem 'fixup' code - could be removed once we establish what is creating the dupes !
        int dupes = 0;
        HashSet<string> todel = new HashSet<string>();

        //load the grafts - parts of the tree are re-used within itself - for example Operating Systems appear under *many* servers
        int grafts = 0;

        r = da.DBExecuteReader(con, "SELECT id,fk_branch_id_source, fk_branch_id_target,path FROM graft");
        object path = null;
        if (r.HasRows)
        {
            while (r.Read)
            {
                //here's where the magic happens

                int sourceid = System.Convert.ToInt32(r.Item("fk_branch_id_source"));
                if (iq.Branches.ContainsKey(sourceid)) //Soft deleted branches are not loaded - so will not be 'there 'to graft
                {

                    source = iq.Branches(sourceid);
                    if (iq.Branches.ContainsKey(System.Convert.ToInt32(r.Item("fk_branch_id_target")))) //the target may have been soft deleted
                    {
                        Target = iq.Branches(System.Convert.ToInt32(r.Item("fk_branch_id_target")));
                        path = (r.Item("Path")).ToString(); //                    '*some* (very few) grafts have a path - ie. they are only active at one specific point in the tree (CPUs are a case in point)


                        if (!string.IsNullOrEmpty(path))
                        {
                            source.GraftedOnAt.Add(path);
                        }

                        if (!source.AllParents.ContainsKey(Target.ID))
                        {
                            source.AllParents.Add(Target.ID, Target);
                        }

                        if (!Target.childBranches.ContainsKey(source.ID))
                        {

                            Target.childBranches.Add(source.ID, source); //note - this does NOT set the branches parent - branches are grafted in many places and so do not have one parent - their parent property is used only by the editor within a single graft
                            Target.HasGrafts = true;
                            grafts++;
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

        return "Loaded " + System.Convert.ToString(grafts) + " grafts (Deleted " + todel.Count + ")";

    }
    private string LoadSlots(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "Select [id],path,fk_branch_id,fk_slottype_id,numslots,fk_translation_key_notes,slotnum,requiredfill,advisedfill from [slot] where deleted =0");

        //Some duplicate slots have crept in (JIT carepacks - but not just that) - this is an on-the-fly fix (which can be removed in due course)
        List<string> todel = new List<string>();

        clsSlot s;
        int slots = 0;

        int ms = 0;

        if (r.HasRows)
        {
            while (r.Read)
            {
                //the act of making the slot adds it to its branch (so we don't need to)
                clsTranslation NOTES = null;
                if (r.Item("FK_TRANSLATION_KEY_NOTES") == DBNull.Value)
                {
                    NOTES = null;
                }
                else
                {
                    NOTES = iq.Translations(System.Convert.ToInt32(r.Item("FK_TRANSLATION_KEY_NOTES")));
                }

                int branchid = System.Convert.ToInt32(r.Item("fk_branch_id"));
                object path = r.Item("path").ToString();
                int slotid = System.Convert.ToInt32(r.Item("id"));

                //Heal' some malformed path (duplicate last segments)
                bool skip = false;
                if (path != "")
                {
                    string[] bits = Strings.Split(System.Convert.ToString(path), ".");
                    if ((bits.Length - 1) > 2 && bits[bits.Length - 1] == bits[(bits.Length - 1) - 1])
                    {
                        todel.Add(slotid);
                        skip = true;
                        ms++;
                    }

                    if (System.Convert.ToInt32(bits.Last) != branchid)
                    {
                        todel.Add(slotid);
                    }
                }

                if (!skip)
                {
                    if (iq.Branches.ContainsKey(branchid)) //soft deleted branches are not loaded - so we can't attach the slots
                    {
                        int stid = System.Convert.ToInt32(r.Item("fk_slottype_id"));
                        clsSlotType slottype = iq.SlotTypes(stid);
                        s = new clsSlot(slotid, slottype, iq.Branches(branchid), path, System.Convert.ToInt32(r.Item("numslots")), NOTES, new NullableInt(r.Item("slotnum")), System.Convert.ToInt32(r.Item("requiredFill")), System.Convert.ToInt32(r.Item("advisedFill")));

                        if (s.ID == -1) //part of the on-the-fly dupes fix (see alos Ctor above)
                        {
                            todel.Add(slotid);
                        }
                        else
                        {
                            slots++;
                        }
                    }
                }
            }
        }
        r.Close();

        //dupes fix (note the dupes aren't actually loaded - and they won't exist next time)
        if (todel.Count > 0)
        {

            int toskip = 0;
            int chunk = 1000;
            do
            {
                System.Object l = from j in todel.Skip(toskip).Take(chunk) select j;
                if (!l.Any)
                {
                    break;
                }
                string sql = "DELETE FROM SLOT WHERE ID IN(" + string.Join(",", l.ToArray) + ")";

                LongSQL(sql);
                toskip += 1000;
            } while (true);
        }
        Debug.Print(ms);
        return "Loaded " + System.Convert.ToString(slots) + " Slot Give/Takes - (deleted " + todel.Count + ")";

    }
    private string LoadSlotTypes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        //Slottypes include PCI (full and half length slots, drive bays, fan bays and anything else with finite capacity that can affect the availability of options

        object sql = null;
        sql = "SELECT id,fk_translation_key,fk_translation_key_short,majorcode,minorCode,EnforceMinorCode ";
        sql += "FROM slottype order by id";

        r = da.DBExecuteReader(con, sql);

        int Count = 0;

        clsSlotType aSlotType;

        List<string> todel = new List<string>();

        Dictionary<int, int> mappings = new Dictionary<int, int>(); //DeDupe -remap to fix.7c data corrupted by the incremental import

        if (r.HasRows)
        {
            while (r.Read)
            {
                if (!Information.IsDBNull(r("fk_translation_key_short")))
                {
                    int a = 9;
                }

                clsTranslation tl = iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key")));
                clsTranslation tls = null;
                if (r.Item("fk_translation_key_short") != DBNull.Value)
                {
                    int ti = System.Convert.ToInt32(r.Item("fk_translation_key_short"));
                    tls = iq.Translations(ti);
                }

                string mjc = System.Convert.ToString(r.Item("majorcode"));
                string mnc = System.Convert.ToString(r.Item("minorcode"));

                if (iq.i_slotType_Code.ContainsKey(mjc))
                {
                    if (iq.i_slotType_Code(mjc).ContainsKey(mnc))
                    {
                        clsSlotType mst = iq.i_slotType_Code(mjc)[mnc]; //master slot type (the one we're keeping!)
                        //this is a dupe
                        todel.Add(r.Item("id"));
                        mappings.Add(r.Item("id").ToString(), mst.ID);
                    }
                }

                aSlotType = new clsSlotType(System.Convert.ToInt32(r.Item("id")), mjc, mnc, tl, tls, System.Convert.ToBoolean(r.Item("EnforceMinorCode")));
                Count++;
            }
        }
        r.Close();


        //sweep/remap the alt slot types

        List<string> sqls = new List<string>();
        r = da.DBExecuteReader(con, "select ID,FK_Slottype_ID,fk_slottype_id_alternative from AltSlotType");
        while (r.Read)
        {
            if (mappings.ContainsKey(r.Item("fk_slotType_id")) || mappings.ContainsKey(r.Item("fk_slottype_id_alternative")))
            {

                int m1 = System.Convert.ToInt32(r.Item("fk_slottype_id"));
                int m2 = System.Convert.ToInt32(r.Item("fk_slottype_id_alternative"));
                if (mappings.ContainsKey(m1))
                {
                    m1 = mappings[m1];
                }
                if (mappings.ContainsKey(m2))
                {
                    m2 = mappings[m2];
                }


                sqls.Add("Update altSlotType set fk_slotType_id=" + System.Convert.ToString(m1) + ",fk_slottype_id_alternative=" + System.Convert.ToString(m2) + " where id = " + r.Item("id"));
            }
        }
        r.Close();

        foreach (var u in sqls)
        {
            da.DBExecuteReader(con, u);
        }

        ///sweep


        if (todel.Count)
        {
            int toskip = 0;
            int chunk = 1000;
            do
            {
                System.Object ll = from j in todel.Skip(toskip).Take(chunk) select j;
                if (!ll.Any)
                {
                    break;
                }

                sql = "Delete from slottype WHERE [id] IN(" + string.Join(",", ll.ToArray) + ")";

                LongSQL(sql);
                toskip += 1000;
            } while (true);

        }


        //read in the alternative slot types (and their relative priorties) for each primary slot type

        sql = "SELECT fk_slottype_id as [primary],fk_slottype_id_alternative as alt,priority ";
        sql += "FROM altSlotType";

        int fallBacks = 0;
        r = da.DBExecuteReader(con, sql);

        if (r.HasRows)
        {
            while (r.Read)
            {
                iq.SlotTypes(System.Convert.ToInt32(r.Item("primary"))).Fallback.Add(System.Convert.ToInt32(r.Item("priority")), iq.SlotTypes(System.Convert.ToInt32(r.Item("alt"))));
                fallBacks++;
            }
        }
        r.Close();




        return "Loaded " + System.Convert.ToString(Count) + " SlotTypes and " + System.Convert.ToString(fallBacks) + " fallbacks" + " (deleted " + todel.Count + ")";

    }
    private string LoadProductTypes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r, List<string> errormessages)
    {

        //ProductTypes were OptTypes before - but now include SVR,DTO,NBK,SWD etc... they are what margins are based on - with Sectors

        object sql = null;
        sql = "SELECT id,fk_translation_key_text,code,[order] FROM ProductType";

        r = da.DBExecuteReader(con, sql);

        int Count = 0;
        clsProductType aProductType;

        if (r.HasRows)
        {
            while (r.Read)
            {
                if (i_ProductType_Code.ContainsKey(r.Item("code")))
                {
                    errorMessages.Add("Product type code " + r.Item("code") + " is duplicated !");
                }
                else
                {
                    aProductType = new clsProductType(System.Convert.ToInt32(r.Item("id")), r.Item("code").ToString(), iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_text"))), System.Convert.ToInt16(r.Item("order")));
                    Count++;
                }
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(Count) + " Product Types (formerly OptTypes)";

    }

    private string LoadQuantities(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        object sql = null;
        sql = "SELECT q.id,[path], fk_region_id,fk_branch_id, preinstalled,minIncrement,preferredIncrement,foc FROM quantity q";
        sql += " INNER JOIN Branch b ON FK_Branch_ID = b.id inner join Product p on b.FK_Product_ID = p.id	";
        sql += " WHERE q.deleted = 0 AND p.deleted = 0";

        r = da.DBExecuteReader(con, sql);

        int Count = 0;

        clsBranch branch = default(clsBranch);
        clsQuantity aQuantity;
        int qid = 0;
        int numPreinstalled = 0;
        int prefincr = 0;

        HashSet<string> h = new HashSet<string>(); //On the fly fix for duplication created by the incremental import
        List<string> toDel = new List<string>(); //ids of the the quantities to delete

        if (r.HasRows)
        {
            while (r.Read)
            {

                qid = System.Convert.ToInt32(r.Item("ID"));

                int branchID = System.Convert.ToInt32(r.Item("fk_branch_id"));
                if (iq.Branches.ContainsKey(branchID)) //some branches are soft deleted (and not loaded)
                {
                    branch = iq.Branches(branchID);
                    numPreinstalled = System.Convert.ToInt32(r.Item("preInstalled"));
                    object path = r.Item("path");
                    bool foc = System.Convert.ToBoolean(r.Item("foc"));
                    clsRegion rgn = iq.Regions(System.Convert.ToInt32(r.Item("fk_region_id")));
                    System.Int32 minIncr = System.Convert.ToInt32(r.Item("MinIncrement"));
                    prefincr = System.Convert.ToInt32(r.Item("PreferredIncrement"));

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

                    Count++;
                }
            }
        }
        r.Close();


        if (toDel.Count)
        {

            int toskip = 0;
            int chunk = 1000;
            do
            {
                System.Object l = from j in toDel.Skip(toskip).Take(chunk) select j;
                if (!l.Any)
                {
                    break;
                }
                sql = "DELETE FROM quantity WHERE ID IN(" + string.Join(",", l.ToArray) + ")";

                LongSQL(sql);
                toskip += 1000;
            } while (true);

        }

        return "Loaded " + System.Convert.ToString(Count) + " Quantity limits, (Deleted " + toDel.Count + ")";

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


        for (pass = 1; pass <= 2; pass++) //we have to load all regions in, before we can set up their parents (becuase if a regions's parent is not yet loaded - bad things happen
        {

            r = da.DBExecuteReader(con, "SELECT Id,[fk_region_id_parent],code,[fk_translation_key_name],isCountry,fk_culture_id,isPlaceholder,Notes,[FK_Region_ID_Geo] FROM [Region]");

            clsRegion aRegion;

            clsRegion parent = default(clsRegion);
            if (r.HasRows)
            {
                while (r.Read)
                {
                    if (pass == 1)
                    {
                        //all parents are set to nothing on the first pass
                        clsCulture culture = default(clsCulture);
                        int tid = System.Convert.ToInt32(r.Item("fk_translation_key_name"));
                        if (iq.Cultures.ContainsKey(System.Convert.ToInt32(r.Item("fk_culture_id"))))
                        {
                            culture = iq.Cultures(System.Convert.ToInt32(r.Item("fk_culture_id")));
                        }
                        else
                        {
                            culture = iq.i_culture_code("en-us");
                        }


                        clsTranslation tlrn = default(clsTranslation);
                        if (iq.Translations.ContainsKey(System.Convert.ToInt32(r.Item("fk_translation_key_name"))))
                        {
                            tlrn = iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_name")));
                        }
                        else
                        {
                            tlrn = iq.Translations.Values.First; //TEMPORARY
                        }


                        aRegion = new clsRegion(System.Convert.ToInt32(r.Item("id")), null, r.Item("code").ToString(), tlrn, System.Convert.ToBoolean(r.Item("isCountry")), culture, System.Convert.ToBoolean(r.Item("isPlaceholder")), r.Item("notes").ToString(), r.Item("FK_Region_ID_Geo").ToString());
                        count++;
                    }
                    else
                    {
                        //set parents (on the seconds pass)
                        if (!Information.IsDBNull(r.Item("fk_region_id_parent"))) //only set the parent on the second pass
                        {
                            parent = iq.Regions(System.Convert.ToInt32(r.Item("fk_region_id_parent")));
                            parent.Children.Add(System.Convert.ToInt32(r.Item("ID")), iq.Regions(System.Convert.ToInt32(r.Item("ID")))); //add to the parent children
                        }
                        else
                        {
                            parent = null;
                        }
                        iq.Regions(System.Convert.ToInt32(r.Item("id"))).Parent = parent;
                    }
                }
            }

            r.Close();
        }


        return "Loaded " + System.Convert.ToString(count) + " regions";

    }

    private string LoadCurrencies(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT Id,code,code_HP,symbol,rate,fk_translation_key_name,fk_translation_key_notes from currency");

        clsCurrency acurrency = default(clsCurrency);
        int count = 0;
        clsTranslation notes = default(clsTranslation);

        if (r.HasRows)
        {
            while (r.Read)
            {
                if (Information.IsDBNull(r.Item("fk_translation_key_notes")))
                {
                    notes = null;
                }
                else
                {
                    notes = iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_notes")));
                }

                acurrency = new clsCurrency(System.Convert.ToInt32(r.Item("id")), r.Item("code").ToString(), r.Item("Code_HP").ToString(), iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_name"))), r.Item("symbol").ToString(), System.Convert.ToSingle(r.Item("rate")), notes); // r.Item("culture"))

                // Add to the DefaultCurrencies collection (used when setting up new Channels etc.)
                DefaultCurrencies.Add(acurrency.ID, acurrency);

                count++;
            }
        }

        r.Close();

        return "Loaded " + System.Convert.ToString(count) + " currencies";

    }


    private string LoadCultures(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT Id,culturecode,[Name] from culture where visible = 1");

        clsCulture aCulture;
        int count = 0;


        if (r.HasRows)
        {
            while (r.Read)
            {

                aCulture = new clsCulture(System.Convert.ToInt32(r.Item("id")), r.Item("culturecode").ToString(), r.Item("name").ToString());
                count++;
            }
        }

        r.Close();

        return "Loaded " + System.Convert.ToString(count) + " cultures";

    }
    private string LoadStates(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT Id,[group],Code,fk_translation_key,[order],colour from [State]");

        int count = 0;

        clsState aState;

        if (r.HasRows)
        {
            while (r.Read)
            {
                //creates a new user adds them to their channel
                aState = new clsState(System.Convert.ToInt32(r.Item("id")), r.Item("group").ToString(), Strings.Trim(System.Convert.ToString(r.Item("code").ToString())), iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key"))), System.Convert.ToInt32(r.Item("order")), r.Item("colour").ToString());
                count++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(count) + "  States";


    }
    private string LoadRoles(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key from [Role]");

        int count = 0;

        clsRole aRole;

        if (r.HasRows)
        {
            while (r.Read)
            {
                aRole = new clsRole(System.Convert.ToInt32(r.Item("id")), r.Item("code").ToString(), iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key"))));
                count++;


            }
        }
        r.Close();


        return "Loaded " + System.Convert.ToString(count) + " Roles";

    }
    private string LoadRights(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key from [Right]");

        int count = 0;

        clsRight aRight = default(clsRight);

        if (r.HasRows)
        {
            while (r.Read)
            {
                aRight = new clsRight(System.Convert.ToInt32(r.Item("id")), r.Item("code").ToString(), iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key"))));
                count++;


                if (!i_right_Code.ContainsKey(aRight.Code))
                {
                    i_right_Code.Add(aRight.Code, aRight);
                }
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(count) + " rights";

    }
    private string LoadRoleRights(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT roleright.Id,role.code as rolecode,[right].code as rightcode from [RoleRight] inner join role on role.id=fk_role_id inner join [right] on [right].id=fk_right_id");

        int count = 0;

        clsRight right = default(clsRight);

        if (r.HasRows)
        {
            while (r.Read)
            {
                right = iq.i_right_Code(r.Item("rightcode"));
                iq.i_role_Code(r.Item("rolecode")).Rights.Add(right.ID, right);
                iq.i_role_Code(r.Item("rolecode")).i_right_code.Add(right.Code, right);
                count++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(count) + " Role-Rights ";

    }

    private string LoadThreads(SqlClient.SqlConnection con, SqlClient.SqlDataReader rdr)
    {

        int count = 0;

        object sql = null;
        sql = "SELECT id,FK_user_id_createdby,FK_user_id_AssignedTo,fk_thread_id_parent,fk_state_id_priority,fk_state_id_status,[hours],title, text,fk_event_id,created,updated,internal from [Thread] order by ID";

        rdr = da.DBExecuteReader(con, sql);

        clsThread aThread;

        clsUser CreatedBy = default(clsUser);
        clsUser AssignedTo = default(clsUser);
        clsThread Parent = null;
        clsState State = default(clsState);


        if (rdr.HasRows)
        {
            while (rdr.Read)
            {

                CreatedBy = iq.Users(System.Convert.ToInt32(rdr.Item("fk_user_id_createdby")));
                AssignedTo = iq.Users(System.Convert.ToInt32(rdr.Item("fk_user_id_assignedto")));

                if (Information.IsDBNull(rdr.Item("fk_thread_id_parent")))
                {
                    Parent = null; //this is the (one and only) top level/root thread
                }
                else if (System.Convert.ToInt32(rdr.Item("fk_thread_id_parent")) == System.Convert.ToInt32(rdr.Item("id")))
                {
                    //Stop
                }
                else
                {
                    Parent = iq.Threads(System.Convert.ToInt32(rdr.Item("fk_thread_id_parent")));
                }
                State = iq.States(System.Convert.ToInt32(rdr.Item("fk_state_id_status")));

                //    If IsDBNull(rdr.Item("fk_event_id")) Then
                // EventLog = Nothing
                //Else
                //EventLog = iq.Events(CInt(rdr.Item("fk_event_id")))
                //End If

                clsState priority = default(clsState);
                priority = iq.States(System.Convert.ToInt32(rdr.Item("fk_state_id_priority")));
                aThread = new clsThread(System.Convert.ToInt32(rdr.Item("id")), CreatedBy, AssignedTo, Parent, priority, State, System.Convert.ToSingle(rdr.Item("hours")), rdr.Item("title").ToString(), new nullableString(rdr.Item("text")), System.Convert.ToDateTime(rdr.Item("Created")), System.Convert.ToDateTime(rdr.Item("Updated")), System.Convert.ToBoolean(rdr.Item("internal")));
                count++;

            }
        }

        rdr.Close();

        return "Loaded " + System.Convert.ToString(count) + " support threads <br/>";

    }
    private string LoadValidations(SqlClient.SqlConnection con, SqlClient.SqlDataReader rdr)
    {

        Validations.Clear();

        object sql = null;
        sql = "SELECT ID,description,regex,violation FROM Validation";

        rdr = da.DBExecuteReader(con, sql);

        clsValidation v;
        int count = 0;
        if (rdr.HasRows)
        {

            while (rdr.Read)
            {
                v = new clsValidation(System.Convert.ToInt32(rdr.Item("ID")), rdr.Item("description").ToString(), rdr.Item("regex").ToString(), rdr.Item("violation").ToString());
                count++;
            }
        }

        rdr.Close();

        return "loaded " + System.Convert.ToString(count) + " Validations.<br/>";


    }

    public string LoadScreens(SqlClient.SqlConnection con, SqlClient.SqlDataReader rdr)
    {
        try
        {
            i_screens_title.Clear();
            i_screens_code.Clear();
            Screens.Clear();
            Fields.Clear();

            object sql = null;
            // sql$ = "SELECT ID,code,Title,[object],[dictionary] FROM [Screen]"
            sql = "SELECT ID,code,Title,[object] FROM [Screen] order by code"; //NOTE the FIRST screen (by code) of each Object type is used for editing

            rdr = da.DBExecuteReader(con, sql);

            int CountScreens = 0;

            clsScreen screen;
            if (rdr.HasRows)
            {
                while (rdr.Read)
                {
                    screen = new clsScreen(System.Convert.ToInt32(rdr.Item("id")), rdr.Item("code").ToString(), rdr.Item("object").ToString(), rdr.Item("Title").ToString());
                    CountScreens++;
                }
            }
            rdr.Close();

            sql = "SELECT ID,FK_Field_Id,FK_Region_ID FROM [FIELDRestriction]";
            object restrictions = da.FilledDataTable(con, sql);


            //load fields
            //sql$ = "SELECT ID,fk_screen_id,property,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,[order],fk_screen_id_embed,width,length,defaultvalue,visible FROM [FIELD]"
            sql = "SELECT ID,fk_screen_id,property,fk_translation_key_label,helptext,fk_validation_id,lookupof,fk_inputtype_id,[order],width,height,length,defaultvalue,visibleList,VisiblePage,defaultFilter,defaultSort,[priority],fk_translation_key_widgetGroup,widgetUI,CanUserSelect,visibleSquare,FK_Field_ID_Linked,Grows,DefaultFilterValues,FilterVisible,[HMC_MutualExclusivity],[InvertFilterOrder] FROM [FIELD]";

            rdr = da.DBExecuteReader(con, sql);

            clsField afield = default(clsField);

            clsValidation validation = null;

            int countFields = 0;
            if (rdr.HasRows)
            {
                while (rdr.Read)
                {
                    SqlClient.SqlDataReader with_1 = rdr;

                    //                    If IsDBNull(rdr.Item("fk_screen_id_embed")) Then
                    //embedscreen = Nothing
                    // Else
                    //embedscreen = Screens(.Item("fk_screen_id_embed"))
                    // End If

                    if (Information.IsDBNull(rdr.Item("fk_validation_id")))
                    {
                        validation = null;
                    }
                    else
                    {
                        if (Validations.ContainsKey(System.Convert.ToInt32(with_1.Item("fk_validation_id"))))
                        {
                            validation = Validations(System.Convert.ToInt32(with_1.Item("fk_validation_id")));
                        }
                        else
                        {
                            Interaction.Beep(); //need to fix this -  saving nullable foriend keys (field.validations is one example)

                        }
                    }

                    //     If rdr.Item("lookupof") <> "" Then Stop
                    clsInputType inputType = default(clsInputType);
                    inputType = InputTypes(System.Convert.ToInt32(with_1.Item("fk_inputtype_id")));

                    clsTranslation wtl = null;
                    if (!Information.IsDBNull(rdr.Item("fk_translation_key_widgetGroup")))
                    {
                        if (iq.Translations.ContainsKey(System.Convert.ToInt32(rdr.Item("fk_translation_key_widgetGroup"))))
                        {
                            wtl = iq.Translations(System.Convert.ToInt32(rdr.Item("fk_translation_key_widgetGroup")));
                        }
                        else
                        {
                            wtl = iq.AddTranslation("No Group", English, "", 0, null, 0, false);
                        }

                    }

                    clsTranslation ltl = null;
                    if (!Information.IsDBNull(rdr.Item("fk_translation_key_label")))
                    {
                        if (iq.Translations.ContainsKey(System.Convert.ToInt32(rdr.Item("fk_translation_key_label"))))
                        {
                            ltl = iq.Translations(System.Convert.ToInt32(rdr.Item("fk_translation_key_label")));
                        }
                        else
                        {
                            ltl = iq.AddTranslation("", English, "", 0, null, 0, false);
                        }
                    }

                    afield = new clsField(Screens(System.Convert.ToInt32(with_1.Item("fk_screen_id"))), System.Convert.ToInt32(rdr.Item("ID")), with_1.Item("property").ToString(), with_1.Item("LookUpOf").ToString(), ltl, with_1.Item("helptext").ToString(), validation, inputType, System.Convert.ToInt32(with_1.Item("length")), System.Convert.ToInt32(with_1.Item("order")), System.Convert.ToInt32(with_1.Item("width")), System.Convert.ToSingle(with_1.Item("height")), with_1.Item("defaultvalue").ToString(), System.Convert.ToBoolean(with_1.Item("visibleList")), System.Convert.ToBoolean(with_1.Item("visiblePage")), System.Convert.ToBoolean(with_1.Item("visibleSquare")), with_1.Item("defaultfilter").ToString(), with_1.Item("defaultsort").ToString(), System.Convert.ToInt32(with_1.Item("Priority")), wtl, rdr.Item("widgetUI").ToString(), System.Convert.ToBoolean(with_1.Item("CanUserSelect")), (rdr("FK_Field_ID_Linked") == DBNull.Value) ? new int?() : (System.Convert.ToInt32(rdr("FK_Field_ID_Linked"))), bool.Parse(rdr("Grows")), (rdr("DefaultFilterValues") == DBNull.Value) ? null : (rdr("DefaultFilterValues").ToString()), System.Convert.ToBoolean(rdr("FilterVisible")), System.Convert.ToBoolean(rdr("HMC_MutualExclusivity")), System.Convert.ToBoolean(rdr("InvertFilterOrder")));
                    countFields++;

                    if (Strings.LCase(System.Convert.ToString(rdr.Item("property").ToString())) == "auditroot")
                    {
                        Screens(System.Convert.ToInt32(with_1.Item("fk_screen_id"))).Auditable = true; //This Class has a root property - grouping instances (and a Current Property)
                    }

                    foreach (var fr in restrictions.Select("FK_Field_Id=" + afield.ID))
                    {
                        afield.ValidRegions.Add(fr("FK_REGION_ID"), iq.Regions(fr("FK_REGION_ID")));
                    }

                }
            }

            rdr.Close();

            return "loaded " + System.Convert.ToString(CountScreens) + " screens containing a total of " + System.Convert.ToString(countFields) + " fields.<br/>";
        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return ex.Message;
        }

    }

    private string loadInputTypes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        iq.InputTypes.Clear();
        iq.i_inputType_code.Clear();

        object sql = null;
        sql = "SELECT ID,code,name FROM [InputType]";

        r = da.DBExecuteReader(con, sql);

        int count = 0;
        clsInputType inputType;
        if (r.HasRows)
        {
            while (r.Read)
            {
                inputType = new clsInputType(System.Convert.ToInt32(r.Item("ID")), r.Item("code").ToString(), r.Item("name").ToString());
                count++;
            }
        }
        r.Close();

        return "loaded " + System.Convert.ToString(count) + "input types<br/>";

    }


    private string LoadMargins(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        r = da.DBExecuteReader(con, "SELECT Id,fk_channel_id_buyer,fk_channel_id_seller,factor,fk_sector_id,sampledsku from [margin]");

        clsChannel Seller = default(clsChannel);
        clsChannel Buyer = default(clsChannel);
        clsSector sector = default(clsSector);
        //Dim productType As clsProductType
        int count = 0;

        clsMargin aMargin;

        if (r.HasRows)
        {
            while (r.Read)
            {
                //creates a new price adding it to the master price list
                Buyer = iq.Channels(System.Convert.ToInt32(r.Item("fk_channel_id_buyer")));
                Seller = iq.Channels(System.Convert.ToInt32(r.Item("fk_channel_id_seller")));
                sector = iq.Sectors(System.Convert.ToInt32(r.Item("fk_sector_id")));
                // productType = iq.ProductTypes(CInt(r.Item("fk_producttype_id")))

                //the act of creating a margin adds it to the sellers dictionary of buyers margins
                aMargin = new clsMargin(System.Convert.ToInt32(r.Item("ID")), Seller, Buyer, System.Convert.ToSingle(r.Item("factor")), "", sector, r.Item("sampledSKU").ToString());
                count++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(count) + " margins";

    }
    private string LoadTeams(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {


        object sql = null;
        sql = "SELECT id,name,fk_channel_id from TEAM";

        r = da.DBExecuteReader(con, sql);

        int count = 0;
        clsTeam aTeam;

        if (r.HasRows)
        {
            while (r.Read)
            {
                aTeam = new clsTeam(System.Convert.ToInt32(r.Item("id")), iq.Channels(System.Convert.ToInt32(r.Item("fk_channel_id"))), r.Item("Name").ToString());
                count++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(count) + " teams";


    }
    private string LoadUsers(SqlClient.SqlConnection con, SqlClient.SqlDataReader r, List<string> errormessages)
    {

        object sql = null;
        try
        {

            sql = "SELECT id,fk_channel_id,email,realname,tel1,tel2,Disabled FROM [User]";

            r = da.DBExecuteReader(con, sql);

            int count = 0;
            clsUser aUser;
            string currentuser = "";
            if (r.HasRows)
            {
                while (r.Read)
                {
                    int cid = System.Convert.ToInt32(r.Item("fk_channel_id"));
                    int uid = System.Convert.ToInt32(r.Item("id"));
                    string email = System.Convert.ToString(r.Item("email").ToString());
                    bool disabled = System.Convert.ToBoolean(r.Item("Disabled"));

                    if (iq.Channels.ContainsKey(cid))
                    {
                        aUser = new clsUser(uid, iq.Channels(cid), email, r.Item("realname").ToString(), new nullableString(r.Item("tel1")), new nullableString(r.Item("tel2")), disabled);

                        currentuser = System.Convert.ToString(r.Item("id").ToString());
                        count++;
                    }
                    else
                    {
                        errorMessages.Add("User " + System.Convert.ToString(uid) + " " + email + " has an invalid channel " + System.Convert.ToString(cid));
                    }

                }
            }
            r.Close();

            return "Loaded " + System.Convert.ToString(count) + " users";
        }
        catch (Exception ex)
        {
            ErrorLog.Add(ex);
            return ex.Message;
        }


    }

    private string LoadUserMessages(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        iq.UserMessages = new Dictionary<string, List<clsMessage>>();

        try
        {
            r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_key_Name, FK_Channel_ID, ValidFrom, ValidTo, Enabled from [message] order by ValidFrom");
            if (r.HasRows)
            {
                while (r.Read())
                {

                    string code = System.Convert.ToString(r.Item("Code"));


                    if (!iq.UserMessages.ContainsKey(code))
                    {
                        iq.UserMessages.Add(code, new List<clsMessage>());
                    }

                    int tkey = System.Convert.ToInt32(r.Item("FK_Translation_key_Name"));
                    if (Translations.ContainsKey(tkey))
                    {
                        clsTranslation translation = this.Translations(tkey);
                        iq.UserMessages(code).Add(new clsMessage(r.Item("ID"), code, translation, r.Item("ValidFrom"), r.Item("ValidTo"), r.Item("Enabled"), r.Item("FK_Channel_ID")));
                    }
                    else
                    {
                        ErrorLog.Add(new Exception("Missing translation key in LoadUserMessages fk_translations_key_name was " + System.Convert.ToString(tkey)));
                    }
                }
            }

        }
        catch (Exception ex)
        {

            ErrorLog.Add(ex);
            return ex.Message;

        }

        return "Loaded user messages";

    }

    private string LoadAddresses(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        iq.Addresses = new Dictionary<string, clsAddress>();

        try
        {
            r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_Key_Address from [Address]");
            if (r.HasRows)
            {
                while (r.Read())
                {

                    string code = System.Convert.ToString(r.Item("Code"));
                    clsTranslation translation = this.Translations(r.Item("FK_Translation_Key_Address"));
                    iq.Addresses.Add(code, new clsAddress(r.Item("ID"), code, translation));

                }
            }

        }
        catch (Exception ex)
        {

            ErrorLog.Add(ex);
            return ex.Message;

        }

        return "Loaded addresses";

    }

    private string LoadLegal(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        iq.Legal = new Dictionary<string, clsLegal>();

        try
        {
            r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_Key_Name from [Legal]");
            if (r.HasRows)
            {
                while (r.Read())
                {

                    string code = System.Convert.ToString(r.Item("Code"));
                    clsTranslation translation = this.Translations(r.Item("FK_Translation_Key_Name"));
                    clsLegal legal = new clsLegal(r.Item("ID"), code, translation);
                    if (!iq.Legal.ContainsKey(code))
                    {
                        iq.Legal.Add(code, legal);
                    }

                }
            }

        }
        catch (Exception ex)
        {

            ErrorLog.Add(ex);
            return ex.Message;

        }

        return "Loaded legal";

    }

    private string LoadResources(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        iq.ResourceCategories = new Dictionary<int, clsResourceCategory>();

        // Resource Categories
        try
        {
            r = da.DBExecuteReader(con, "select [ID], [Name], [FK_Translation_Key_Name], [Order] from [ResourceCategory]");
            if (r.HasRows)
            {
                while (r.Read())
                {

                    int tkey = System.Convert.ToInt32(r.Item("FK_Translation_Key_Name"));
                    if (Translations.ContainsKey(tkey))
                    {
                        iq.ResourceCategories.Add(r.Item("ID"), new clsResourceCategory(r.Item("ID"), r.Item("Name"), Translations(tkey), r.Item("Order")));
                    }
                    else
                    {
                        ErrorLog.Add(new Exception("Translation missing in LoadResources - key was" + System.Convert.ToString(tkey)));
                    }

                }
            }

        }
        catch (Exception ex)
        {

            ErrorLog.Add(ex);
            return ex.Message;

        }

        // Resources Files
        try
        {
            r = da.DBExecuteReader(con, "select [ID], [Description], [FK_Resource_Category_ID], [Type], [Code], [FK_Translation_Key_Title], [FK_Region_ID], [FK_Language_ID], [FK_SellerChannel_ID], [mfrCode], [Order], [Embed] from [Resource]");
            if (r.HasRows)
            {
                while (r.Read())
                {


                    int tkey = System.Convert.ToInt32(r.Item("FK_Translation_Key_Title"));
                    if (Translations.ContainsKey(tkey))
                    {
                        clsTranslation translation = this.Translations(tkey);
                        int categoryId = System.Convert.ToInt32(r.Item("FK_Resource_Category_ID"));
                        clsRegion region = null;
                        clsLanguage language = null;
                        clsChannel sellerChannel = null;
                        string mfrCode = null;

                        if (!Information.IsDBNull(r.Item("FK_Region_ID")))
                        {
                            int regionId = System.Convert.ToInt32(r.Item("FK_Region_ID"));
                            if (iq.Regions.ContainsKey(regionId))
                            {
                                region = iq.Regions(regionId);
                            }
                        }

                        if (!Information.IsDBNull(r.Item("FK_Language_ID")))
                        {
                            int languageId = System.Convert.ToInt32(r.Item("FK_Language_ID"));
                            if (iq.Languages.ContainsKey(languageId))
                            {
                                language = iq.Languages(languageId);
                            }
                        }

                        if (!Information.IsDBNull(r.Item("FK_SellerChannel_ID")))
                        {
                            int sellerChannelId = System.Convert.ToInt32(r.Item("FK_SellerChannel_ID"));
                            if (iq.Channels.ContainsKey(sellerChannelId))
                            {
                                sellerChannel = iq.Channels(sellerChannelId);
                            }
                        }

                        if (!Information.IsDBNull(r.Item("mfrCode")))
                        {
                            mfrCode = System.Convert.ToString(r.Item("MfrCode"));
                        }

                        clsResource resource = new clsResource(r.Item("ID"), r.Item("Description"), r.Item("Type"), r.Item("Code"), translation, region, language, sellerChannel, mfrCode, r.Item("Order"), categoryId, r.Item("Embed"));

                        if (iq.ResourceCategories.ContainsKey(categoryId))
                        {
                            if (iq.ResourceCategories(categoryId).Resources == null)
                            {
                                iq.ResourceCategories(categoryId).Resources = new List<clsResource>();
                            }
                            iq.ResourceCategories(categoryId).Resources.Add(resource);
                        }
                    }
                    else
                    {
                        ErrorLog.Add(new Exception("Missing translation in LoadResources " + System.Convert.ToString(tkey)));
                    }

                }
            }

        }
        catch (Exception ex)
        {

            ErrorLog.Add(ex);
            return ex.Message;

        }

        return "Loaded resources";

    }

    private string LoadROKAttributes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        iq.ROKAttributes = new Dictionary<string, List<clsROKAttribute>>();

        try
        {
            r = da.DBExecuteReader(con, "select ID, OS_Code, FK_Attribute_Code, FK_Translation_Key_Name from ROKAttributes order by OS_Code");
            if (r.HasRows)
            {
                while (r.Read())
                {

                    string osCode = System.Convert.ToString(r.Item("OS_Code"));

                    if (!iq.ROKAttributes.ContainsKey(osCode))
                    {
                        iq.ROKAttributes.Add(osCode, new List<clsROKAttribute>());
                    }

                    clsTranslation translation = this.Translations(r.Item("FK_Translation_key_Name"));
                    clsROKAttribute rok = new clsROKAttribute(r.Item("ID"), osCode, r.Item("FK_Attribute_Code"), translation);

                    iq.ROKAttributes(osCode).Add(rok);

                }
            }

        }
        catch (Exception ex)
        {

            ErrorLog.Add(ex);
            return ex.Message;

        }

        return "Loaded ROK attributes";

    }

    private string LoadAccounts(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        object sql = null;

        sql = "SELECT fk_account_id,role.code as rolecode FROM [AccountRoles] inner join role on role.id=fk_role_id";
        r = da.DBExecuteReader(con, sql);
        Dictionary<int, List<clsRole>> dr = new Dictionary<int, List<clsRole>>();
        if (r.HasRows)
        {
            while (r.Read)
            {
                if (!dr.ContainsKey(r.Item("fk_account_id")))
                {
                    dr.Add(r.Item("fk_account_id"), new List<clsRole>());
                }
                dr[r.Item("fk_account_id")].Add(iq.i_role_Code(r.Item("rolecode")));

            }
        }

        //sql$ = "SELECT id,fk_user_id,password,fk_role_id,fk_team_id,fk_language_id,fk_buyergroup_id,fk_currency_id FROM [Account]"
        sql = "SELECT id,fk_user_id,password,fk_team_id,fk_language_id,fk_channel_id_buyer,fk_currency_id,fk_channel_id_seller,priceBand,fk_culture_id,mfrCode FROM [Account]";
        r = da.DBExecuteReader(con, sql);

        int count = 0;
        clsAccount anAccount;

        //Dim buyerGroup As clsBuyerGroup
        clsChannel Buyer = default(clsChannel);
        clsUser User = default(clsUser);
        clsTeam Team = default(clsTeam);
        clsLanguage Language = default(clsLanguage);
        clsCurrency currency = default(clsCurrency);
        clsRole Role;
        clsChannel seller = default(clsChannel);
        clsCulture culture = default(clsCulture);

        if (r.HasRows)
        {
            while (r.Read)
            {
                try
                {
                    if (count == 174130)
                    {
                        int a = 6;
                    }
                    //buyergroup = iq.BuyerGroups(r.Item("fk_buyergroup_id"))
                    Buyer = iq.Channels(System.Convert.ToInt32(r.Item("fk_channel_id_buyer")));
                    seller = iq.Channels(System.Convert.ToInt32(r.Item("fk_channel_id_seller")));
                    if (iq.Cultures.ContainsKey(System.Convert.ToInt32(r.Item("fk_culture_id"))))
                    {
                        culture = iq.Cultures(System.Convert.ToInt32(r.Item("fk_culture_id")));
                    }
                    else
                    {
                        culture = iq.i_culture_code("en-us");
                    }

                    User = iq.Users(System.Convert.ToInt32(r.Item("fk_user_id")));

                    // If r.Item("FK_USER_ID") = 65538 Then Stop

                    if (Information.IsDBNull(r.Item("fk_team_id")))
                    {
                        Team = null;
                    }
                    else
                    {
                        int tid = 0;
                        tid = System.Convert.ToInt32(r.Item("fk_team_id"));
                        Team = iq.Teams(tid);
                    }

                    Language = iq.Languages(System.Convert.ToInt32(r.Item("fk_language_id")));
                    currency = iq.Currencies(System.Convert.ToInt32(r.Item("fk_currency_id")));

                    anAccount = new clsAccount(System.Convert.ToInt32(r.Item("id")), User, r.Item("password").ToString(), Buyer, (dr.ContainsKey(System.Convert.ToInt32(r.Item("id")))) ? (dr[System.Convert.ToInt32(r.Item("id"))].ToArray) : new[] { }, Team, Language, currency, seller, iq.getPriceBand(r.Item("priceBand").ToString()), culture, r.Item("mfrcode"));
                    count++;
                }
                catch (Exception ex)
                {
                    object a = ex.Message;
                }

            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(count) + " accounts";

    }


    private string LoadChannels(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {

        //Channels include Distributors, Resellers, Manufacturers - they are basically sets of users (for whom a set of pricing may exist)
        //in the case of resellers - chanel reall just represent the buying company - for who the dist's set up margin records

        r = da.DBExecuteReader(con, "SELECT Id,fk_channel_id_parent,Name,BusinessName,Address,code,fk_region_id,webtoken,pic1,pic2,url,fk_channel_id_cloneof,priceConfig,treePath,Focus,margintype,marginMin,marginMax,SchemeOverride,Legal,FK_Currency_ID_Default,universal,orderemail,basketmode,basketurl From Channel order by id");

        clsChannel achannel;
        int coId = 0; //clone of' ID

        // child > parent - Used to construct the heirarchy of channels once they're all loaded
        //(as the order of channels cannot be guaranteed and the parent may not be present at the point we instance the child)
        Dictionary<int, int> dicClones = new Dictionary<int, int>();
        Dictionary<int, int> dicChildren = new Dictionary<int, int>();

        if (r.HasRows)
        {
            while (r.Read)
            {
                //creates a new channel and adds it to the 'master' list
                object code = null;
                if (Information.IsDBNull(r.Item("code")))
                {
                    code = "";
                }
                else
                {
                    code = r.Item("code").ToString();
                }

                int id = System.Convert.ToInt32(r.Item("id"));

                if (!string.IsNullOrEmpty(code) && iq.i_channel_code.ContainsKey(code))
                {
                    object l = iq.i_channel_code.ContainsKey(code) + " is duplicated";
                }
                else
                {

                    //Dim webtoken As Guid = r.Item("webtoken")
                    string webtoken = System.Convert.ToString(r.Item("webtoken").ToString());
                    System.String WT = webtoken.ToString();

                    coId = System.Convert.ToInt32(r.Item("fk_channel_id_cloneof"));
                    if (Information.IsDBNull(coId))
                    {
                        Debugger.Break();
                    }
                    else if (System.Convert.ToInt32(r.Item("id")) == coId) //this channel is a clone of ITSELF
                    {

                    }
                    else //it's a clone of something OTHER than itself
                    {
                        dicClones.Add(id, coId); //I' am the 'child'
                    }

                    dicChildren.Add(id, r.Item("fk_channel_id_cloneof")); //dictionary of child >parent

                    clsChannel parent;
                    if (r.Item("fk_channel_id_parent") == DBNull.Value)
                    {
                        parent = null;
                    }
                    else
                    {
                        if (iq.Channels.ContainsKey(System.Convert.ToInt32(r.Item("fk_channel_id_parent"))))
                        {
                            parent = iq.Channels(System.Convert.ToInt32(r.Item("fk_channel_id_parent")));
                        }
                        else
                        {
                            //this channel has been orphaned
                            parent = null;
                        }
                    }

                    //we can't set the 'iscloneof' or 'parent' unti we've loaded all channels (becuase the parent may not be there yet)
                    achannel = new clsChannel(System.Convert.ToInt32(r.Item("id")), null, r.Item("Name").ToString(), r.Item("BusinessName").ToString(), null, r.Item("Address").ToString(), code, iq.Regions(System.Convert.ToInt32(r.Item("fk_region_id"))), WT, new nullableString(r.Item("pic1")), new nullableString(r.Item("pic2")), new nullableString(r.Item("url")), System.Convert.ToInt32(r.Item("priceConfig")), r.Item("TreePath").ToString(), r.Item("Focus").ToString(), r.Item("marginMin"), r.Item("Marginmax"), r.Item("MarginType"), r.Item("schemeOverride"), r.Item("legal"), (Information.IsDBNull(r.Item("FK_Currency_ID_Default"))) ? null : (iq.Currencies(System.Convert.ToInt32(r.Item("FK_Currency_ID_Default")))), System.Convert.ToBoolean(r.Item("universal")), r.Item("orderemail"), r.Item("basketMode"), r.Item("BasketURL"));

                }
            }
        }
        r.Close();

        //now (all channels are loaded) setup the clones
        //clones are 'copies' of other channels which have the same portfolio (set of products) and pricing which is some factor of one of the price bands ('A','B','','internal','external' etc) according to the [Margins]
        foreach (var childID in dicClones.Keys)
        {
            iq.Channels(childID).IsCloneOf = iq.Channels(dicClones[childID]);
        }

        //And similarly the parent-child relationship ('food chain') of channels (which is a completely different thing from clones and is really only used for organisation/reporting)
        //(we can't guaranteed the order of chanels and children may be defined before their parents )
        foreach (var childID in dicChildren.Keys)
        {
            iq.Channels(childID).Parent = iq.Channels(dicChildren[childID]);
        }

        DataTable domainTable = LoadDomains(con);
        if (domainTable != null)
        {
            foreach (DataRow row in domainTable.Rows)
            {
                int cid = System.Convert.ToInt32(row["fk_channel_id"]);
                if (iq.Channels.ContainsKey(cid)) //A check that *should be unnecessary (but i broke some data)
                {
                    clsChannel chnl = iq.Channels(cid);
                    chnl.Domains.Add(row["domain"].ToString());
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
        if (r.HasRows)
        {
            while (r.Read)
            {
                //creates a new BuyerGroup and adds it to the 'master' list
                aBuyerGroup = new clsBuyerGroup(System.Convert.ToInt32(r.Item("id")), r.Item("Name").ToString(), iq.Channels(System.Convert.ToInt32(r.Item("fK_channel_id_owner"))), r.Item("ownersID").ToString());
            }
        }
        r.Close();

        int placed = 0;
        r = da.DBExecuteReader(con, "SELECT fk_channel_id,fk_buyerGroup_id FROM ChannelGroup");
        if (r.HasRows)
        {
            while (r.Read)
            {
                iq.BuyerGroups(System.Convert.ToInt32(r.Item("fk_buyerGroup_id"))).Channels.Add(iq.Channels(System.Convert.ToInt32(r.Item("fk_channel_id"))));
                placed++;
            }
        }
        r.Close();

        return "Loaded " + iq.BuyerGroups.Count + " buyer groups, and placed " + System.Convert.ToString(placed) + " Channels in them";

    }
    private string LoadAttributes(SqlClient.SqlConnection con, SqlClient.SqlDataReader r)
    {
        try
        {
            r = da.DBExecuteReader(con, "Select [id],[code],[order],fk_translation_key_name from [Attribute]");

            clsAttribute a;
            if (r.HasRows)
            {
                while (r.Read)
                {
                    //becuase we're specifying the ID here - it won't autmatically be written to the database
                    if (!iq.i_attribute_code.ContainsKey(Strings.Trim(System.Convert.ToString(r.Item("code").ToString()))))
                    {
                        clsTranslation tl = iq.Translations(System.Convert.ToInt32(r.Item("fk_translation_key_name")));
                        a = new clsAttribute(System.Convert.ToInt32(r.Item("ID")), Strings.Trim(System.Convert.ToString(r.Item("Code").ToString())), tl, System.Convert.ToInt32(r.Item("ORDER")));
                    }
                    else
                    {
                        Logit("attribute " + r.Item("code").ToString() + " is duplicated");
                    }
                }
            }
            r.Close();

            return "Loaded " + iq.Attributes.Count + " attributes";
        }
        catch (Exception ex)
        {
            return "Failed: " + ex.Message;
        }
    }
    public clsTranslation AddTranslation(string Text, clsLanguage language, string group, int order, DataTable writecache, int nextKey, bool dupe)
    {

        //Set dupe if you (deliberately) want to create an addtional copy of the translation.. perhaps for isolated editing)

        //    Exit Function

        if (language != English)
        {
            Debugger.Break();
        }

        if (writecache != null && nextKey == 0)
        {
            Debugger.Break();
        }
        if (writecache == null && nextKey != 0)
        {
            Debugger.Break();
        }

        clsTranslation existing = EnglishIndex(Text, group);
        if (existing != null && dupe == false)
        {
            return existing;
        }
        else
        {
            //note, instancing a new clstranslation adds it to the englishindex
            return new clsTranslation(language, Text, group, order, writecache, nextKey);
        }

    }
    private DataTable LoadDomains(SqlClient.SqlConnection con)
    {

        string query = "Select FK_Channel_ID,Domain from Domain";
        try
        {

            SqlCommand cmd = new SqlCommand(query, con);
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
            }
            SqlDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            return dt;

        }
        catch (Exception)
        {
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
        if (r.HasRows)
        {
            while (r.Read)
            {
                if (iq.Promos.ContainsKey(System.Convert.ToInt32(r("ID"))))
                {
                    iq.Promos(System.Convert.ToInt32(r("ID"))).AddRegion(iq.Regions(System.Convert.ToInt32(r("FK_Region_ID"))));
                    iq.Promos(System.Convert.ToInt32(r("ID"))).AddSystemType(r("SystemType"));
                }
                else
                {
                    promo = new clsPromo(System.Convert.ToInt32(r("ID")), r("Code").ToString(), iq.Translations(System.Convert.ToInt32(r("FK_Translation_Key_Description"))), iq.Regions(System.Convert.ToInt32(r("FK_Region_ID"))), r("FieldProperty_Filter").ToString(), r("FieldProperty_Value").ToString(), r("SystemType").ToString());
                }
            }
        }
        r.Close();

        command.CommandType = CommandType.Text;
        command.CommandText = "Select * from promoproduct";
        command.Connection = con;
        r = command.ExecuteReader();
        if (r.HasRows)
        {
            while (r.Read)
            {
                int pid = r("FK_Product_Id");
                clsProduct product = null;
                if (iq.Products.ContainsKey(pid))
                {
                    product = iq.Products(pid);
                }
                else
                {
                    if (REMAPS.ContainsKey(pid))
                    {
                        product = REMAPS[pid];
                    }

                }

                if (product != null)
                {
                    if (product.Promos != null && !product.Promos.ContainsKey(iq.Promos(System.Convert.ToInt32(r("FK_Promo_Id"))).Code))
                    {
                        product.Promos.Add(iq.Promos(System.Convert.ToInt32(r("FK_Promo_Id"))).Code, new List<clsRegion>());
                        product.Promos(iq.Promos(System.Convert.ToInt32(r("FK_Promo_Id"))).Code).Add(iq.Regions(r("FK_REGION_ID")));
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
        clsCampaign campaign = default(clsCampaign);
        if (r.HasRows)
        {
            while (r.Read)
            {
                campaign = new clsCampaign(System.Convert.ToInt32(r(0)), r(1).ToString(), iq.Channels(System.Convert.ToInt32(r(2))), iq.Regions(System.Convert.ToInt32(r(3))), iq.Channels(System.Convert.ToInt32(r(4))), iq.Channels(System.Convert.ToInt32(r(5))), System.Convert.ToDateTime(r(6)), System.Convert.ToDateTime(r(7)));
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
        int adCount = 0;
        r = command.ExecuteReader();
        clsAdvert advert;
        if (r.HasRows)
        {
            while (r.Read)
            {
                advert = new clsAdvert(System.Convert.ToInt32(r("ID")), iq.Campaigns(System.Convert.ToInt32(r("FK_Campaign_ID"))), r("Name").ToString(), r("ImageURL").ToString(), r("Url").ToString(), System.Convert.ToInt16(r("Type")), r("BasketProductBelowAbsent").ToString(), r("BasketProductBelowPresent").ToString(), iq.ProductTypes(System.Convert.ToInt32(r("FK_ProdType_Present"))), iq.ProductTypes(System.Convert.ToInt32(r("FK_ProdType_Absent"))), iq.SlotTypes(System.Convert.ToInt32(r("FK_SlotType_ID"))), System.Convert.ToInt32(r("FillThresholdPercent")), System.Convert.ToBoolean(r("imageWide")), (Information.IsDBNull(r("SlotTypeCode"))) ? null : (r("SlotTypeCode")), (Information.IsDBNull(r("FK_Region_Id_Present"))) ? null : (iq.Regions(System.Convert.ToInt32(r("FK_Region_Id_Present")))), (Information.IsDBNull(r("FK_Region_Id_Absent"))) ? null : (iq.Regions(System.Convert.ToInt32(r("FK_Region_Id_Absent")))), System.Convert.ToBoolean(r("visible")), (r("mfrCode")).ToString());

                adCount++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(adCount) + " Adverts";
    }

    private string LoadScreenOverrides(SqlConnection con, SqlDataReader r)
    {
        SqlCommand command = new SqlCommand();
        string query = "Select  [FK_Account_ID],[FK_Screen_ID],Path,[FK_Field_ID],[ForceVisibilityTo],[ForceOrderTo],[ForceWidthTo],[ForceSortTo],[ForceFilterTo],[FK_DisplayUnit_ID] from AccountScreenOverride";
        command.CommandType = CommandType.Text;
        command.CommandText = query;
        command.Connection = con;
        int screenoverrideCount = 0;
        r = command.ExecuteReader();
        clsScreenOverride @override = default(clsScreenOverride);
        if (r.HasRows)
        {
            while (r.Read)
            {
                @override = new clsScreenOverride(System.Convert.ToInt32(r("FK_Account_ID")), System.Convert.ToInt32(r("FK_Screen_ID")), r("Path").ToString(), System.Convert.ToInt32(r("FK_Field_ID")), (r("ForceVisibilityTo") == DBNull.Value) ? null : (r("ForceVisibilityTo")), (r("ForceOrderTo") == DBNull.Value) ? new int?() : (System.Convert.ToInt32(r("ForceOrderTo"))), (r("ForceWidthTo") == DBNull.Value) ? new double() : (System.Convert.ToDouble(r("ForceWidthTo"))), r("ForceSortTo").ToString(), r("ForceFilterTo").ToString(), (r("FK_DisplayUnit_ID") == DBNull.Value) ? null : (iq.Units(System.Convert.ToInt32(r("FK_DisplayUnit_ID")))));
                screenoverrideCount++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(screenoverrideCount) + " Overrides";
    }

    public string LoadConversions(SqlConnection con, SqlDataReader r)
    {
        SqlCommand command = new SqlCommand();
        string query = "Select  FK_Units_From,FK_Units_To,Rate from Conversion";
        command.CommandType = CommandType.Text;
        command.CommandText = query;
        command.Connection = con;
        int conversioncount = 0;
        r = command.ExecuteReader();
        Conversions = new Dictionary<int, Dictionary<int, double>>();
        if (r.HasRows)
        {
            while (r.Read)
            {
                if (!Conversions.ContainsKey(System.Convert.ToInt32(r("FK_Units_From"))))
                {
                    Conversions.Add(System.Convert.ToInt32(r("FK_Units_From")), new Dictionary<int, double>());
                }
                Conversions(System.Convert.ToInt32(r("FK_Units_From"))).Add(System.Convert.ToInt32(r("FK_Units_To")), System.Convert.ToDouble(r("Rate")));
                conversioncount++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(conversioncount) + " Conversions";
    }

    public string LoadMeasures(SqlConnection con, SqlDataReader r)
    {
        SqlCommand command = new SqlCommand();
        string query = "Select ID,MeasureName from Measure";
        command.CommandType = CommandType.Text;
        command.CommandText = query;
        command.Connection = con;
        int measurecount = 0;
        r = command.ExecuteReader();
        Measures = new Dictionary<int, string>();
        if (r.HasRows)
        {
            while (r.Read)
            {
                Measures.Add(System.Convert.ToInt32(r("ID")), r("MeasureName").ToString());
                measurecount++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(measurecount) + " Measures";
    }

    public string LoadActiveUniversal(SqlConnection con, SqlDataReader r)
    {
        SqlCommand command = new SqlCommand();
        string query = "select r.code, t.[Text] from Region r inner join Translation t on r.FK_Translation_key_name = t.[key] inner join Universal on [Name] = t.[Text] where r.IsCountry = 1 And [Enabled] = 1 order by t.[Text]";
        command.CommandType = CommandType.Text;
        command.CommandText = query;
        command.Connection = con;
        int countryCount = 0;
        r = command.ExecuteReader();
        ActiveUniversalCountries = new Dictionary<string, string>();
        if (r.HasRows)
        {
            while (r.Read)
            {
                ActiveUniversalCountries.Add(r("Code"), r("Text").ToString());
                countryCount++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(countryCount) + " Active Universal Countries";
    }



    public static void DeleteTL(clsTranslation tl, clsLanguage language)
    {

        iq.Translations(tl.Key).remove(language);
        if (language == English)
        {
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
        {
            iq.Translations.Add(tl.Key, tl);
        }

        object ck = tl.text(language) + "^" + tl.Group;
        if (language == English)
        {
            if (!iq.iEnglishIndex.ContainsKey(ck))
            {
                iq.iEnglishIndex.Add(ck, tl);
            }
        }
        else if (language.Code == "KY")
        {
            if (!iq.KYIndex.ContainsKey(ck))
            {
                iq.KYIndex.Add(ck, tl);
            }
        }

    }


    public static string CleanString(string s)
    {
        StringBuilder sb = new StringBuilder(s);
        string x = System.Convert.ToString(sb.ToString());
        int intx = 0;
        intx = x.IndexOf("<");
        int inty = 0;
        while (intx > 0)
        {
            inty = x.IndexOf(">", intx);
            sb.Remove(intx, inty - intx + 1);
            x = System.Convert.ToString(sb.ToString());
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
        int locationsCount = 0;
        r = command.ExecuteReader();
        ActiveUniversalCountries = new Dictionary<string, string>();
        if (r.HasRows)
        {
            while (r.Read)
            {
                Locations.Add(r("Code"), r("Description"));
                locationsCount++;
            }
        }
        r.Close();

        return "Loaded " + System.Convert.ToString(locationsCount) + " Locations";
    }


}