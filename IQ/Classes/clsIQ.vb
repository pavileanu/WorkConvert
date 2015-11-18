'Option Strict On
Imports dataAccess
Imports System.Data.SqlClient
Imports System.Threading
Imports System.Xml.Serialization
Imports System.IO

'A single 'master' instace of the IQ class is instantiated, and holds the entire 'object model'.
'All IIS users access this (one) underlying, memory based copy of all of product data, quotes, translations, channels, accounts etc.
'all of which are held in dictionaries - typically accessed by an integer key which - correspods to their primary key field (ID).. in the database.
'Note:- their Key is NOT their ordinal - ie.Product(5) is NOT the 5th product.. it's the product with the key '5' - (which could be the 167th product)
'Some of the dictionaries are sorted - so *sometimes* the ordinals will match the keys - but it can *never* be relied upon
'It's vital to understand the difference between product(5) and product.values(5) - and to never confuse the two

Public Class clsIQ

    Private Shared _instance As clsIQ
    Private Shared ReadOnly _LockObj As New Object()
    Public Shared errorMessages As List(Of String) = New List(Of String)()
    Public Shared messages As LoggingList(Of String) = New LoggingList(Of String)()

    Public REMAPS As New Dictionary(Of Integer, clsProduct) '

    Public Shared ReadOnly Property Instance As clsIQ
        Get
            SyncLock _LockObj
                If _instance Is Nothing OrElse (Not IsLoading And Not IsLoaded) Then
                    IsLoading = True
                    'AuditLog.Instance.Add()
                    _instance = New clsIQ()
                    Dim d As Thread = New Thread(New ParameterizedThreadStart(AddressOf _instance.load))

                    d.Start(errorMessages)
                End If

            End SyncLock

            Return _instance
        End Get
    End Property

    Private Shared StartBytes As Long?
    Private Shared EndBytes As Long?
    Public Shared Sub reset()
        StartBytes = System.GC.GetTotalMemory(True)
        SyncLock _LockObj
            _instance = Nothing
            IsLoaded = False
        End SyncLock

    End Sub

    Public Shared IsLoading As Boolean = False
    Public Shared IsLoaded As Boolean = False

    Public nextSearchID As Integer 'used for the abandonment of keyword searches 'mid flight' - 

    ' Public PathCache As clsProductCache
    'the sets are shared amongst users, with the smallest  one being contiually replaced

    Public AllVariants As clsVariant
    Public infoID As Integer ' as simple counter for unique ID's on info (blue circle) divs - may get quite large 
    'Public NextKey As Integer
    Property Languages As Dictionary(Of Integer, clsLanguage)
    Property ActiveLanguages As Dictionary(Of Integer, clsLanguage)
    Property SlotTypes As Dictionary(Of Integer, clsSlotType)

    'This is a dictionary of dictionaries
    '                                                 KEY  (NOT ID!) >  Translation
    Public Property Translations As Dictionary(Of Integer, clsTranslation)
    Public Property Products As Dictionary(Of Integer, clsProduct)
    Public Property Stock As Dictionary(Of Integer, clsstock)  'would be nice to get rid of this (accessible via the product - stock now belongs to variants)

    Property RootBranch As clsBranch 'THE root node of the product tree... *every* product tree is attached to here - but it never exists in the database - it has an ID of 0
    Property RootCPQBranch As clsBranch 'THE root node of all carepacks

    'Note, events load their children from the database 'just in time' - only root level events (those whose parents are themselves) are initially loaded (see iq.LoadEvents).. accessing the children property fetches them from the database.
    'events are *written* real time - into the object model only - and persisted (to the databse) at key points via a events PersistRecursive() metho
    'This allows very large numbers of events to be recorded at high speed (via bulk writes) with minimal memory footprint.

    Property RootThread As clsThread
    Property RootChannel As clsChannel  'The channel hierarchy is not a rigid reflection of the 'real life' supply chain - it's just for grouping and presentation withing the editor (and potentially reporting) 
    'The actual relationships between channels are defined by the margins/prices - each linking a buyer and a seller  in a many:many relationship (the same reseller may buy from several distributors)

    'Property StandardVariant As clsVariant
    Property Branches As Dictionary(Of Integer, clsBranch)
    Property Quantities As Dictionary(Of Integer, clsQuantity)

    Property i_SpecialBranches As Dictionary(Of String, clsBranch)

    'Grafts provide cross linking in the tree - allowing a branch to be reused in may places
    'becasue a single branch (eg. a system unit) may have many branches grafted onto it (eg, Drive bays, memory slots, PCI slots)
    'we must have ready access to a list of grafts onto a particular branch
    'the integer index is the 'target' portion of the graft - this item contains a list of all the grafts onto it
    'property Grafts As SortedDictionary(Of clsBranch, List(Of clsGraft))
    Property Attributes As Dictionary(Of Integer, clsAttribute) 'the 'master' list of all (types of) attribute

    'iEnglishIndex uses a compund key internally - and iexposed via a public function  (forcing a group to be specified)
    Private Property iEnglishIndex As Dictionary(Of String, clsTranslation) 'Watch out !... the integer part of this is the translation.KEY
    Property KYIndex As Dictionary(Of String, clsTranslation) 'Watch out !... the integer part of this is the translation.KEY
    Property Units As Dictionary(Of Integer, clsUnit) 'master list of units - keyed by our internal short code eg KG, MM, M, LBS, BTU
    Property Channels As Dictionary(Of Integer, clsChannel) 'Each channel has a set of users, each user has a set of quotes
    Property BuyerGroups As Dictionary(Of Integer, clsBuyerGroup)
    Property Teams As Dictionary(Of Integer, clsTeam)
    Property Users As Dictionary(Of Integer, clsUser)
    Property ProductTypes As Dictionary(Of Integer, clsProductType)

    'Did spend a lot of time considering this as channel.customeraccounts - but it makes operations on the whole list harder
    Property Accounts As Dictionary(Of Integer, clsAccount) 'Each user has an account with one (or more) distributors(channels)
    'Quotes live under the agentaccounts - property Quotes As SortedDictionary(Of Integer, clsquote) 'Each channel has a set of users, each user has a set of quote
    Property Currencies As Dictionary(Of Integer, clsCurrency)
    Property Cultures As Dictionary(Of Integer, clsCulture)
    ' Property Countries As Dictionary(Of Integer, clsCountry) - replaced by regions which are more generalised and heirarchical - 
    Property States As Dictionary(Of Integer, clsState)
    'property Quantities As Dictionary(Of Integer, clsQuantity) 'Quantities apply to specific nodes in the tree (via their paths)
    'property Prices As Dictionary(Of Integer, clsPrice)
    'Property Prunes As Dictionary(Of String, clsPrune)
    'Property Prunes As Dictionary(Of Integer, clsPrune)
    Property Sectors As Dictionary(Of Integer, clsSector)
    'Property Events As Dictionary(Of Integer, clsEvent)
    Property Quotes As Dictionary(Of Integer, clsQuote)  ' Quotes are *not* populated at startup (as there are an awful lot of them) - however we need a root level dictionary to enable viewing/editing via the OM
    Property Threads As Dictionary(Of Integer, clsThread)
    Property Regions As Dictionary(Of Integer, clsRegion)
    Property AvalancheOPGs As Dictionary(Of Integer, ClsAvalancheOPG)
    Property FlexOPGs As Dictionary(Of Integer, clsFlexOPG)
    Property Bundles As Dictionary(Of Integer, clsBundle)
    Property Excludes As Dictionary(Of Integer, clsExclude)  'having any member of key list exclude all members of the values list
    Property UserMessages As Dictionary(Of String, List(Of clsMessage))
    Property ROKAttributes As Dictionary(Of String, List(Of clsROKAttribute))
    Property Addresses As Dictionary(Of String, clsAddress)
    Property Legal As Dictionary(Of String, clsLegal)
    Property ResourceCategories As Dictionary(Of Integer, clsResourceCategory)

    ' HP care pack service levels
    Property ServiceLevels As Dictionary(Of Integer, clsServiceLevel)
    Property ServiceLevelResponse As Dictionary(Of Integer, clsResponse)
    Property ServiceLevelServiceType As Dictionary(Of Integer, clsServiceType)
    Property ServiceLevelTROAA As Dictionary(Of Integer, clsTROAA)
    Property ServiceLevelAttributeMap As Dictionary(Of String, clsAttribute)
    Property CarePackLastRefresh As IDictionary(Of String, DateTime)

    'ultimately these are references - so not a expensive as they might look (around 12 bytes per row)
    Property Prices As Dictionary(Of Integer, clsPrice)  'I've been all 'round the houses with this - (where to 'store' prices) - as dictionaries within the products etc,etc - but at the end of the day it makes most sense to have a root level 'master' list - most becuase the lookups are Olog(n)
    Property Variants As Dictionary(Of Integer, clsVariant)


    Property Campaigns As Dictionary(Of Integer, clsCampaign)
    Property Adverts As Dictionary(Of Integer, clsAdvert)
    Property ScreenOverrides As List(Of clsScreenOverride)
    Property Promos As Dictionary(Of Integer, clsPromo)
    Public i_PromoRegions As Dictionary(Of clsRegion, List(Of clsPromo))
    Public i_PromoSystemTypes As Dictionary(Of clsPromo, List(Of String))

    Property Conversions As Dictionary(Of Integer, Dictionary(Of Integer, Double))
    Property Measures As Dictionary(Of Integer, String)
    Property ActiveUniversalCountries As Dictionary(Of String, String)
    Property ValidationInclusions As Dictionary(Of Integer, clsValidationInclusion)
    Property Locations As Dictionary(Of String, String)

    Public Filters As Dictionary(Of Integer, clsFilter)

    Property DefaultCurrencies As Dictionary(Of Integer, clsCurrency)

    'These i_ dictionaries are effectively indexes for various columns in the object model - In obscure places, or where speed isn't critical, we use LINQ - but these
    'lookup tables will be much faster - as a general principle these 'indexes' are updated when the objects they index are created (ie., in their New() constructors
    Public i_state_GroupCode As Dictionary(Of String, clsState) ' allows state (messages and ID's) to be looked up via their codes
    Public i_user_email As Dictionary(Of String, clsUser) 'index of username to user objects (as there are several thousand)
    Public i_channel_code As Dictionary(Of String, clsChannel)
    Public i_SKU As Dictionary(Of String, clsProduct)  'used primarly by the webservice to look up products by SKU

    'Public i_buyerGroups As Dictionary(Of String, clsBuyerGroup) 'used by ther webservice to look up buyer groups (fast!) by the distis own reference (eg.. buyerGroup.ownerID)
    'Public i_variant_code As Dictionary(Of String, clsVariant)
    Public i_currency_code As Dictionary(Of String, clsCurrency)
    Public i_culture_code As Dictionary(Of String, clsCulture)
    'Public i_country_code As Dictionary(Of String, clsCountry) - countries have been superseded by regions
    Public i_region_code As Dictionary(Of String, clsRegion)

    Public i_language_Code As Dictionary(Of String, clsLanguage)
    Public i_ProductType_Code As Dictionary(Of String, clsProductType)
    Public i_role_Code As Dictionary(Of String, clsRole)
    Public i_right_Code As Dictionary(Of String, clsRight)
    Public Property priceBands As Dictionary(Of String, clsPriceBand)


    Public Property Roles As Dictionary(Of Integer, clsRole)
        Get
            Return i_role_Code.Values.ToDictionary(Function(rc) rc.ID, Function(rc) rc)
        End Get
        Set(value As Dictionary(Of Integer, clsRole))
            i_role_Code = value.Values.ToDictionary(Function(irc) irc.Code, Function(irc) irc)
        End Set
    End Property
    Public Property Rights As Dictionary(Of Integer, clsRight)
        Get
            Return i_right_Code.Values.ToDictionary(Function(rc) rc.ID, Function(rc) rc)
        End Get
        Set(value As Dictionary(Of Integer, clsRight))
            i_right_Code = value.Values.ToDictionary(Function(irc) irc.Code, Function(irc) irc)
        End Set
    End Property
    Public i_sector_code As Dictionary(Of String, clsSector)
    Public i_unit_code As Dictionary(Of String, clsUnit)
    Public i_attribute_code As Dictionary(Of String, clsAttribute)
    Public i_OpgRef As Dictionary(Of String, ClsAvalancheOPG)
    Public i_Bundle_code As Dictionary(Of String, clsBundle)
    Public i_Filters_Code As Dictionary(Of String, clsFilter)
    Public i_Account_HostIDpriceBand As Dictionary(Of String, clsAccount)
    Public i_slotType_Code As Dictionary(Of String, Dictionary(Of String, clsSlotType))
    Public i_scheme_code As Dictionary(Of String, List(Of clsScheme))

    Property Schemes As Dictionary(Of Integer, clsScheme) 'Loyalty schemes  ' products have a dictionary of scheme>points

    'Generic editor stuff

    Public Property Screens As Dictionary(Of Integer, clsScreen)
    Public Property Fields As Dictionary(Of Integer, clsField)
    Public Property Validations As Dictionary(Of Integer, clsValidation)
    Public Property InputTypes As Dictionary(Of Integer, clsInputType)

    Public i_inputType_code As Dictionary(Of String, clsInputType)
    Public i_screens_title As Dictionary(Of String, clsScreen) ' ' Used by the generic aditor makesecreen() code.. to see if we already have a screen for the TYPE of object we are currently making a screen for
    Public i_screens_code As Dictionary(Of String, clsScreen)  'Not
    'Public I_SCREENS_TYPE As Dictionary(Of String, clsScreen)
    'Public i_quantity_path As Dictionary(Of String, clsQuantity)

    Public Gateway As Dictionary(Of String, Object) 'makes all the other dictionaries accessible by a name.. see Suggestor.aspx

    'Public channelSKU As Dictionary(Of clsChannel,cls

    'Public Updates As Dictionary(Of Integer, clsUniTran) 'Used for asynchronous webservice pricing calls - hol

    Public ProductValidationsAssignment As Dictionary(Of String, List(Of clsProductValidation))
    ' a dictioanary  by  BUYER channel a dictionary (by string Promo Letter) - of which branches have visble, descendant promotions 
    'Promos are currently A or B (AB is acceptable)  - for avalance and bundles   (needs to be per buyer - because different buyers can see different products ie.they may not have a price for some)
    '                                            Reseller (e-buyer)                    'A'-for avalanche 'B' for bundles             
    Public PromoBranches As Dictionary(Of clsChannel, Dictionary(Of String, List(Of clsBranch)))
    '    Public bundleBranches As Dictionary(Of clsChannel, List(Of clsBranch))

    Public loadedTimestamp As DateTime

    '  Public Property Recommendations As Dictionary(Of Integer, clsRecommendation)

    Public PNAdown As Boolean = False

    'This dictionary and the associated public property replace the standard ASP.NET session object (which is after all just a dictionary of objects, keyed by objects)
    'which gets it's knickers in a twist accross tabs/browsers
    'the entire set of session variables is indexed by a LoginID - which has the advantage of being able to join/snoop on any session in progress
    Public seshDic As Dictionary(Of UInt64, Dictionary(Of String, Object))
    Dim seshLog As Dictionary(Of UInt64, List(Of String)) = New Dictionary(Of ULong, List(Of String))()
    Dim SeshTimes As Dictionary(Of UInt64, DateTime)

    'Switch for strict validation
    Public ReadOnly StrictSlotValidation As Boolean = False

    Public Function EnglishIndex(text As String, group As String) As clsTranslation

        'the circumflex is just a delimiter for the two parts of the compound key
        Dim ck$ = text & "^" & group
        If iEnglishIndex.ContainsKey(ck$) Then
            Return iEnglishIndex(ck$)
        Else
            Return Nothing
        End If

    End Function
    Public Function recordLogin(User As clsUser, failed As Boolean, email As String, ua As String) As Integer

        If User Is Nothing Then User = iq.i_user_email("unknown@unknown.com")

        'Store useragent
        da.ExecuteSP(dataAccess.da.OpenDatabase(True), "usp_UpdateUserAgentList", New Dictionary(Of String, Object)() From {{"AgentName", ua}}, Nothing)

        recordLogin = da.DBExecutesql("INSERT INTO [login] (fk_user_id,timestamp,failed,TypedEmail,lid,FK_UserAgent_Id,ServerNode) VALUES (" & User.ID & ",getdate()," & IIf(failed, 1, 0).ToString() & "," & da.SqlEncode(email) & ",NULL,(SELECT Id FROM UserAgents WHERE AgentName=" + da.SqlEncode(ua) + ")," & da.SqlEncode(Environment.MachineName) & ");", True)


    End Function

    Public Function getPriceBand(text As String) As clsPriceBand

        If Me.priceBands.ContainsKey(text) Then
            Return Me.priceBands(text)
        Else
            Return New clsPriceBand(text)  'This Should/must be/is the ONLY call to the constructor of clsPriceband
        End If

    End Function
    Public Sub updateLogin(tid As Integer, lid As UInt64)
        da.DBExecutesql("Update Login set lid=" & lid & " WHERE id=" & tid)
    End Sub
    Public Sub updateLogin(lid As UInt64, account As clsAccount)

        da.DBExecutesql("Update Login set fk_account_id_agent=" & account.ID & " WHERE lid=" & lid)

        'Dim focus As List(Of String) = New List(Of String)
        'For Each f In Split(account.SellerChannel.Focus, ",")
        '    If Trim(f) <> "" Then
        '        focus.Add(f)  'focus is a list of 'codes' for product groupings we (only) wish the display - eg. Receta
        '    End If
        'Next
        iq.sesh(lid, "foci") = account.SellerChannel.Focus 'focus is a CD list in the session variable too

    End Sub

    Public Function SeshAlive(lid As UInt64) As Boolean
        SeshAlive = False
        If seshDic Is Nothing Then Return False
        If seshDic.Count > 0 Then
            If seshDic.ContainsKey(lid) Then
                Return True
            Else
                Return LoadUserState(lid)
            End If

        End If

    End Function

    Public Function SeshContains(lid As UInt64, key As String) As Boolean

        If seshDic Is Nothing Then Return False

        If seshDic.ContainsKey(lid) Then
            Return (seshDic(lid).ContainsKey(key))
        Else
            Return False
        End If
    End Function

    Public Function SeshValue(lid As UInt64, key As String, AbsentValue As Object) As Object

        'returns a  'sesh'ion variable - returns false if the variable is absent

        If Not seshDic(lid).ContainsKey(key) Then
            Return AbsentValue
        Else
            Return seshDic(lid)(key)
        End If

    End Function


    Public Function getSeshDic(lid As UInt64) As Dictionary(Of String, Object)

        UpdateSeshTime(lid)
        Return seshDic(lid)

    End Function


    Public Function seshTyped(Of T)(lid As UInt64, key As String) As T
        Dim res = sesh(lid, key)
        If res Is Nothing Then Return res
        If res.GetType Is GetType(T) Then
            Return CType(res, T)
        Else
            Return Nothing
        End If
    End Function

    Public Property sesh(lid As UInt64, key As String) As Object

        Get

            '   If lid = 0 Then Stop

            UpdateSeshTime(lid)
            If seshDic Is Nothing Then Return Nothing
            If Not seshDic.ContainsKey(lid) Then
                Return Nothing
            Else
                If seshDic(lid).ContainsKey(key) Then
                    Return seshDic(lid)(key)
                Else
                    Return Nothing
                End If
            End If
        End Get

        Set(value As Object)

            '            If lid = 0 Then Stop

            If seshDic Is Nothing Then seshDic = New Dictionary(Of UInt64, Dictionary(Of String, Object))
            If Not seshDic.ContainsKey(lid) Then
                seshDic.Add(lid, New Dictionary(Of String, Object)(StringComparer.CurrentCultureIgnoreCase))
            End If

            ' If lid = 0 Then Stop
            seshDic(lid)(key) = value
        End Set

    End Property

    Public Function SessionTable() As Table
        If seshDic Is Nothing Then Return Nothing
        Dim t As Table = New Table
        t.CssClass = "sessionTable"

        Dim help$ = "Session ID - Click to view session||||Time to live (session will expire in X minutes)|Buyer|Current Value of basket items"
        Dim thr As TableHeaderRow = MakeTHR("lid,Current Page,Agent Email, HostID,TTL,QuotingFor,Quote Value", help$, "adminTable")

        t.Rows.Add(thr)

        For Each k In seshDic.Keys
            t.Rows.Add(SeshTableRow(k))
        Next

        Return t

    End Function

    Public Function SeshTableRow(lid As UInt64) As TableRow

        Dim errorMessages As List(Of String) = New List(Of String)

        Dim tc As TableCell
        Dim hl As HyperLink
        Dim lbl As Label

        SeshTableRow = New TableRow

        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)

        If agentAccount IsNot Nothing Then

            With seshDic(lid)


                tc = New TableCell
                SeshTableRow.Controls.Add(tc)

                hl = New HyperLink
                tc.Controls.Add(hl)

                hl.Text = "View"

                hl.NavigateUrl = .Item("currentPage").ToString()

                tc = New TableCell
                lbl = New Label
                lbl.Text = .Item("currentPage").ToString()
                tc.Controls.Add(lbl)
                SeshTableRow.Controls.Add(tc)

                'Agent (sellers)  Email
                tc = New TableCell
                lbl = New Label
                lbl.Text = agentAccount.User.Email
                tc.Controls.Add(lbl)
                SeshTableRow.Controls.Add(tc)

                'Sellers HOSTID
                tc = New TableCell
                lbl = New Label
                lbl.Text = agentAccount.SellerChannel.Code
                tc.Controls.Add(lbl)
                SeshTableRow.Controls.Add(tc)

                'Time to live (that's 'LIV' not 'Lyve'
                tc = New TableCell
                lbl = New Label
                lbl.Text = CStr(50 - DateDiff(DateInterval.Minute, SeshTimes(lid), Now))
                tc.Controls.Add(lbl)
                SeshTableRow.Controls.Add(tc)

                'Buyers Email
                tc = New TableCell
                lbl = New Label
                lbl.Text = buyerAccount.User.Email
                tc.Controls.Add(lbl)
                SeshTableRow.Controls.Add(tc)


                'Quote value
                tc = New TableCell
                If .ContainsKey("QuoteID") Then
                    Dim qvp As Panel = agentAccount.Quotes(CInt(.Item("QuoteID"))).QuotedPrice.DisplayPrice(buyerAccount, errorMessages)
                    tc.Controls.Add(qvp)
                Else

                End If
                SeshTableRow.Controls.Add(tc)

            End With
        End If

    End Function

    Public Sub KillSesh(lid As UInt64)

        ' Dim StartBytes As Long = System.GC.GetTotalMemory(True)

        If SeshTimes IsNot Nothing Then
            If SeshTimes.ContainsKey(lid) Then
                SeshTimes.Remove(lid)
            End If
        End If

        If seshDic IsNot Nothing Then
            If seshDic.ContainsKey(lid) Then
                seshDic.Remove(lid)
            End If
        End If

        '  Dim Stopbytes As Long = System.GC.GetTotalMemory(True)

        ' KillSesh = "Terminated session " & lid & " freeing approximately " & Int((StartBytes - Stopbytes) / (1024)) & "KB"

        TidySwiftBranches(lid)



    End Sub
    Public Function KillOldSessions() As Integer

        Dim toKill As List(Of UInt64) = New List(Of UInt64)
        If SeshTimes IsNot Nothing Then
            For Each kvp In SeshTimes.ToArray()
                If Math.Abs(DateDiff(DateInterval.Minute, Now, kvp.Value)) > 60 Then
                    toKill.Add(kvp.Key)
                End If
            Next

            For Each s In toKill
                SeshTimes.Remove(s)
                If seshDic IsNot Nothing Then seshDic.Remove(s)
                TidySwiftBranches(s)
            Next s
        End If

        Return toKill.Count

    End Function

    Private Sub UpdateSeshTime(lid As UInt64)

        If SeshTimes Is Nothing Then SeshTimes = New Dictionary(Of UInt64, DateTime)
        SyncLock SeshTimes
            If Not SeshTimes.ContainsKey(lid) Then
                SeshTimes.Add(lid, Now)
            Else
                SeshTimes(lid) = Now
            End If
        End SyncLock
    End Sub

    Public Sub New()

        Randomize(Now.Millisecond)

        Me.AllVariants = New clsVariant()

        Me.Branches = New Dictionary(Of Integer, clsBranch)
        'Me.Events = New Dictionary(Of Integer, clsEvent)
        Me.Threads = New Dictionary(Of Integer, clsThread)
        Me.i_SpecialBranches = New Dictionary(Of String, clsBranch)

        'PathCache = New clsProductCache
        Languages = New Dictionary(Of Integer, clsLanguage)
        ActiveLanguages = New Dictionary(Of Integer, clsLanguage)
        Translations = New Dictionary(Of Integer, clsTranslation) '

        Products = New Dictionary(Of Integer, clsProduct)
        Stock = New Dictionary(Of Integer, clsstock) 'we need a 'flat' list of the stock for incremental import purposes - the stock embeded in the product is more generally what's used
        Variants = New Dictionary(Of Integer, clsVariant)

        iEnglishIndex = New Dictionary(Of String, clsTranslation)  'makes the dictionary keys case insensitive
        KYIndex = New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)  'makes the dictionary keys case insensitive
        Attributes = New Dictionary(Of Integer, clsAttribute)
        Units = New Dictionary(Of Integer, clsUnit)

        Quantities = New Dictionary(Of Integer, clsQuantity)

        Users = New Dictionary(Of Integer, clsUser)
        Accounts = New Dictionary(Of Integer, clsAccount)
        Teams = New Dictionary(Of Integer, clsTeam)
        Channels = New Dictionary(Of Integer, clsChannel)
        BuyerGroups = New Dictionary(Of Integer, clsBuyerGroup)
        Currencies = New Dictionary(Of Integer, clsCurrency)
        Cultures = New Dictionary(Of Integer, clsCulture)
        '    Countries = New Dictionary(Of Integer, clsCountry)
        Regions = New Dictionary(Of Integer, clsRegion)
        States = New Dictionary(Of Integer, clsState)
        '  Prunes = New Dictionary(Of Integer, clsPrune)
        SlotTypes = New Dictionary(Of Integer, clsSlotType)
        ProductTypes = New Dictionary(Of Integer, clsProductType)
        Sectors = New Dictionary(Of Integer, clsSector)
        InputTypes = New Dictionary(Of Integer, clsInputType)
        Validations = New Dictionary(Of Integer, clsValidation)
        Screens = New Dictionary(Of Integer, clsScreen)
        Fields = New Dictionary(Of Integer, clsField)
        Quotes = New Dictionary(Of Integer, clsQuote)
        AvalancheOPGs = New Dictionary(Of Integer, ClsAvalancheOPG)
        FlexOPGs = New Dictionary(Of Integer, clsFlexOPG)
        Bundles = New Dictionary(Of Integer, clsBundle)
        Excludes = New Dictionary(Of Integer, clsExclude)
        'Variants = New Dictionary(Of Integer, clsVariant) - moved into the Products
        Prices = New Dictionary(Of Integer, clsPrice)
        Campaigns = New Dictionary(Of Integer, clsCampaign)
        Adverts = New Dictionary(Of Integer, clsAdvert)
        ScreenOverrides = New List(Of clsScreenOverride)
        priceBands = New Dictionary(Of String, clsPriceBand)
        Locations = New Dictionary(Of String, String)
        DefaultCurrencies = New Dictionary(Of Integer, clsCurrency)

        PromoBranches = New Dictionary(Of clsChannel, Dictionary(Of String, List(Of clsBranch)))
        Schemes = New Dictionary(Of Integer, clsScheme) 'Master list of Loyalty schemes  - products have a dictionary of scheme>points
        '  Recommendations = New Dictionary(Of Integer, clsRecommendation)


        'these 'index' dictionaries allow us to look things up very quickly. typically by some human readable code (rather than Row ID)
        'they are automatically added to in the constructors (sub New's) of the Classes they hold.
        'they carry only the string key, as a *reference* to an instance of an object - so their footprint is quite modsest
        i_user_email = New Dictionary(Of String, clsUser)(StringComparer.CurrentCultureIgnoreCase) 'index of username to user objects (as there are several thousand)
        i_state_GroupCode = New Dictionary(Of String, clsState)(StringComparer.CurrentCultureIgnoreCase) ' allows state (messages and ID's) to be looked up via a compound key of group-code
        i_channel_code = New Dictionary(Of String, clsChannel)(StringComparer.CurrentCultureIgnoreCase)
        i_SKU = New Dictionary(Of String, clsProduct)(StringComparer.CurrentCultureIgnoreCase)  'used primarly by the webservice to look up products by SKU

        'i_variant_code = New Dictionary(Of String, clsVariant)
        i_currency_code = New Dictionary(Of String, clsCurrency)(StringComparer.CurrentCultureIgnoreCase)
        i_culture_code = New Dictionary(Of String, clsCulture)(StringComparer.CurrentCultureIgnoreCase)
        'i_country_code = New Dictionary(Of String, clsCountry)
        i_region_code = New Dictionary(Of String, clsRegion)(StringComparer.CurrentCultureIgnoreCase)
        i_language_Code = New Dictionary(Of String, clsLanguage)(StringComparer.CurrentCultureIgnoreCase)
        i_ProductType_Code = New Dictionary(Of String, clsProductType)(StringComparer.CurrentCultureIgnoreCase)
        i_role_Code = New Dictionary(Of String, clsRole)(StringComparer.CurrentCultureIgnoreCase)
        i_right_Code = New Dictionary(Of String, clsRight)(StringComparer.CurrentCultureIgnoreCase)
        i_sector_code = New Dictionary(Of String, clsSector)(StringComparer.CurrentCultureIgnoreCase)
        i_unit_code = New Dictionary(Of String, clsUnit)(StringComparer.CurrentCultureIgnoreCase)
        i_attribute_code = New Dictionary(Of String, clsAttribute)(StringComparer.CurrentCultureIgnoreCase)
        i_inputType_code = New Dictionary(Of String, clsInputType)(StringComparer.CurrentCultureIgnoreCase)
        i_screens_title = New Dictionary(Of String, clsScreen)(StringComparer.CurrentCultureIgnoreCase)
        i_screens_code = New Dictionary(Of String, clsScreen)(StringComparer.CurrentCultureIgnoreCase)

        i_OpgRef = New Dictionary(Of String, ClsAvalancheOPG)(StringComparer.CurrentCultureIgnoreCase)
        i_Bundle_code = New Dictionary(Of String, clsBundle)(StringComparer.CurrentCultureIgnoreCase)
        i_Filters_Code = New Dictionary(Of String, clsFilter)(StringComparer.CurrentCultureIgnoreCase)
        i_Account_HostIDpriceBand = New Dictionary(Of String, clsAccount)(StringComparer.CurrentCultureIgnoreCase)
        i_slotType_Code = New Dictionary(Of String, Dictionary(Of String, clsSlotType))(StringComparer.CurrentCultureIgnoreCase)
        i_scheme_code = New Dictionary(Of String, List(Of clsScheme))(StringComparer.CurrentCultureIgnoreCase)

        Gateway = New Dictionary(Of String, Object)  'makes other dictionaries accessible by name - allowing suggestion against many dictionaries (see suggestor.aspx)
        '                                             could be done with reflection - but I didn't have a good handle on that at the time, and this works just fine.

        Gateway.Add("channels", Channels)
        Gateway.Add("currencies", Currencies)
        Gateway.Add("regions", Regions)
        Gateway.Add("accounts", Accounts) 'we will re-point this to the SellerChannels CustomerAccounts - just in time
        Gateway.Add("SKUs", i_SKU)

        Profiling.Profile = New Dictionary(Of String, clsProfile)

    End Sub


    Public Sub load(errormessages As List(Of String))
        'If IsLoading Then Exit Sub
        If clsIQ.IsLoaded Then clsIQ.reset() : Exit Sub



        '        Try
        IsLoading = True
        If StartBytes Is Nothing Then StartBytes = System.GC.GetTotalMemory(True)

        errormessages.Clear()
        messages.Clear()
        messages.Start()

        Call bootstrap()

        Dim StopBytes As Long

        '        p.ID = "loadinfo"
        '       p.Attributes("style") = "height:1.5em;overflow:hidden"

        'Dim b As New Image
        'b.ImageUrl = "/images/navigation/expand.png"
        'b.Attributes("onclick") = "document.getElementById('ctl00_MainContent_loadinfo').style.height='auto';"
        'p.Controls.Add(b)

        'Loads everything from the database

        Dim con As SqlClient.SqlConnection

        con = da.OpenDatabase()
        messages.Add("Opened the database")

        Dim r As SqlClient.SqlDataReader
        r = Nothing

        messages.Add(LoadLanguages(con, r))
        messages.Add(LoadTranslations(con))

        ' messages.Add(LoadProductValidations(con, r))

        messages.Add(LoadCultures(con, r))
        'messages.Add("size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")
        messages.Add(LoadRegions(con, r))

        r_worldwide = clsRegion.getOrMake(Nothing, "XW", "Worldwide", False, False, "Not precious about XW")

        messages.Add(LoadStates(con, r))
        'LoadEvents(con) 'NB the Root Event is set in here

        'messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        messages.Add(LoadUnits(con, r))
        messages.Add(LoadAttributes(con, r))
        'messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        messages.Add(LoadSlotTypes(con, r))
        messages.Add(LoadProductTypes(con, r, errormessages))
        messages.Add(LoadSectors(con, r))

        'messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        messages.Add(LoadProducts(con, r))

        'messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        messages.Add(LoadCurrencies(con, r))

        messages.Add(LoadChannels(con, r))

        'If Not iq.i_channel_code.ContainsKey("Everyone") Then Dim achannel As clsChannel = New clsChannel(Nothing, "Everyone", "Public (list) pricing", "", "Everyone", r_worldwide, New nullableString, New nullableString, New nullableString, 0, "tree.1", "", 0, 0, "R", "", "") 'everyone is not a selling channel (so no priceConfig is required)

        Everyone = iq.getPriceBand("")
        'HPList = iq.getPriceBand("HPlist")

        messages.Add(LoadCampaigns(con, r))
        messages.Add(LoadPromos(con, r))
        messages.Add(LoadAdverts(con, r))
        messages.Add(LoadScreenOverrides(con, r))

        'StopBytes = System.GC.GetTotalMemory(False)
        'messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        'txt &= LoadVariants(con, r)

        'StopBytes = System.GC.GetTotalMemory(False)
        'txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"


        messages.Add(loadInputTypes(con, r))
        messages.Add(LoadScreens(con, r))
        Dim ts As Double = Stopwatch.GetTimestamp
        messages.Add(LoadBranches(con, r, errormessages))
        Dim et As Integer = (Stopwatch.GetTimestamp - ts) / Stopwatch.Frequency * 1000


        messages.Add(LoadExcludes(con, r))

        messages.Add(LoadSpecialBranches(con, r, errormessages))

        messages.Add(LoadValidationsInclusions(con, r))

        'StopBytes = System.GC.GetTotalMemory(False)
        'messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        messages.Add(LoadSlots(con, r))
        'StopBytes = System.GC.GetTotalMemory(False)
        'messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        con.Close()
        con = da.OpenDatabase()
        messages.Add(LoadUserMessages(con, r))
        messages.Add(LoadAddresses(con, r))
        messages.Add(LoadLegal(con, r))
        messages.Add(LoadResources(con, r))

        messages.Add(LoadProductAttributes(con, r))
        messages.Add(CleanProducts(con, r))

        messages.Add(LoadROKAttributes(con, r))

        'StopBytes = System.GC.GetTotalMemory(False)
        'messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        'txt &= LoadCountries(con, r)

        messages.Add(LoadHPServiceLevels(con, r))


        messages.Add(LoadAvalancheOPGs(con, r))
        'StopBytes = System.GC.GetTotalMemory(False)
        ' messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        messages.Add(LoadBundles(con, r))

        messages.Add(LoadFlex(con, r))
        messages.Add(LoadFlexRegions(con, r))
        messages.Add(LoadFlexLines(con, r))
        messages.Add(LoadFlexRules(con, r))

        'StopBytes = System.GC.GetTotalMemory(False)
        '  messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        messages.Add(LoadSchemes(con, r))
        messages.Add(LoadPoints(con, r))

        'StopBytes = System.GC.GetTotalMemory(False)
        '   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")


        messages.Add(LoadQuantities(con, r))

        'StopBytes = System.GC.GetTotalMemory(False)
        '    messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        'txt &= LoadChannelSkus(con, r)


        'StopBytes = System.GC.GetTotalMemory(False)
        '  messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        con.Close()
        con = da.OpenDatabase()

        ' txt &= LoadBuyerGroups(con, r)
        messages.Add(LoadUsers(con, r, errormessages))
        messages.Add(LoadTeams(con, r))
        messages.Add(LoadRoles(con, r))
        messages.Add(LoadRights(con, r))
        messages.Add(LoadRoleRights(con, r))
        messages.Add(LoadAccounts(con, r))
        'StopBytes = System.GC.GetTotalMemory(False)
        '   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        '  txt &= LoadPrices(con, r, errorMessages)
        '  StopBytes = System.GC.GetTotalMemory(False)
        '  txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"

        messages.Add(LoadMargins(con, r))
        'StopBytes = System.GC.GetTotalMemory(False)
        '   messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        'txt &= LoadStock(con, r)
        'StopBytes = System.GC.GetTotalMemory(False)
        'txt &= " size now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB"
        con.Close()
        con = da.OpenDatabase()
        '        txt &= LoadScreens(con, r)
        messages.Add(LoadValidations(con, r).ToString())
        messages.Add(LoadThreads(con, r).ToString())
        messages.Add(LoadFilters())

        messages.Add(LoadConversions(con, r))
        messages.Add(LoadMeasures(con, r))
        messages.Add(LoadActiveUniversal(con, r))
        'messages.Add(LoadLocations(con, r))

        If iq.i_channel_code.ContainsKey("HP") Then
            HP = iq.i_channel_code("HP")
            If HP IsNot Nothing Then
                messages.Add(HP.LoadVariants(errormessages, 1))
                '     messages.Add("size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")
            End If

            'We now, dynamically load listprices for the users, accounts, sellerchannels, country - at signin (see accounts.aspx.vp)
            'txt &= HP.LoadPrices(Everyone, errormessages, r_GB.ID)

            'StopBytes = System.GC.GetTotalMemory(False)
            '    messages.Add(" size now  :" & Int((System.GC.GetTotalMemory(True) - StartBytes) / (1024 ^ 2)) & "MB")

        End If


        Dim distinctlang = From j In iq.Accounts.Values Select j.Language Distinct

        For Each selectedlang As clsLanguage In distinctlang
            ActiveLanguages.Add(selectedlang.ID, selectedlang)
        Next


        For Each cb In iq.RootBranch.childBranches.Values
            Dim btl As String = cb.Translation.text(English)
            If btl.ToLower.Contains("accessories and services") Then
                Dim c As Integer
                cb.flagAsUnsearchable(c)
                Debug.Print(c)
            End If
        Next

        messages.Add("Loaded complete Object Model ")
        messages.Add("HP iQuote 2 version " & My.Application.Info.Version.ToString)

        'If Not iq.Root Is Nothing Then
        '    LastMilestone = Stopwatch.GetTimestamp
        '    IndexSKUs()
        '    txt &= "Indexed SKUs"
        'End If
        messages.Add(LoadUserStates(con, r))
        messages.Add(LoadProductValidations(con, r))
        Me.loadedTimestamp = Now
        con.Close()
        'p.Controls.Add(NewLit())

        StopBytes = System.GC.GetTotalMemory(True)
        messages.Add("Total OM size (minus channel specific pricing) now  :" & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB")

        '  p.Controls.Add(OutputErrors(errorMessages, lid))

        Call checkEssentials()

        _instance.IsLoaded = True

        If StartBytes IsNot Nothing Then
            EndBytes = System.GC.GetTotalMemory(True)
            If StopBytes - StartBytes > 0 Then
                messages.Add("Object model reload freeing approximately " & Int((StopBytes - StartBytes) / (1024 ^ 2)) & "MB")
            Else
                messages.Add("Object model load taking apporx " & Int((StartBytes - StopBytes) / (1024 ^ 2)) & "MB")
            End If
        End If

        messages.StopClock()

        ' Dim count As Integer = 0
        ' RecurseChildren(iq.Root, count, "Tree")
        ' load &= "<b>Walked tree of " & count.ToString("###,###,###,###") & " options" & TimeSince(Start) & "<br/></b>"

        'Catch ex As Exception
        'messages.Add("Exception: " + ex.Message)
        '        End Try




        Dim mastercpusbranch As clsBranch = Nothing
        For Each branch In iq.Branches.Values
            If branch.Translation.text(English).ToUpper = "CPU" Then
                If branch.childBranches.Count > 30 Then
                    mastercpusbranch = branch
                    Exit For
                End If

            End If
        Next

        If mastercpusbranch is Nothing then stop

        cleanChassisBranches()


        IsLoading = False
    End Sub
    Public OldUserSessions As Dictionary(Of UInt64, clsUserState) = New Dictionary(Of ULong, clsUserState)()

    Public Sub cleanChassisBranches()

        Dim em As New List(Of String)
        Dim summary As String = ""

        Dim counts As New Dictionary(Of String, Integer)

        For Each b In iq.Branches.Values.ToList
            If b.Product IsNot Nothing Then
                If b.Product.ProductType.Code.ToLower = "chas" Then
                    If b.Translation.text(English) <> "CPU" Then
                        Dim pd As String = b.EnglishName
                        If Not pd.ToLower.EndsWith(" chassis") Then
                            Dim a = 9
                            summary = ""
                            b.HardDelete(em, summary, 0, True, counts)
                        End If
                    End If
                End If
            End If
        Next

        For Each k In counts.Keys
            Debug.Print(k & " - " & counts(k))
        Next

    End Sub



    Private Function LoadProductValidations(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String
        Try
            ProductValidationsAssignment = New Dictionary(Of String, List(Of clsProductValidation))()
            r = da.DBExecuteReader(con, "Select ProductValidations.Id,SystemType,[OptType],[ValidationType],[Severity],[FK_Translation_Key_Message],[RequiredQuantity],[CheckAttribute],[DependantOptType],DependantCheckAttribute,DependantCheckAttributeValue,CheckAttributeValue,FK_Translation_Key_CorrectMessage,ValidationMessageType,[LinkTechnology],[LinkOptType],[LinkOptionFamily] from ProductValidations INNER JOIN ProductValidationMappings on FK_ProductValidation_ID = Id")

            Dim duds As Integer
            If r.HasRows Then
                While r.Read
                    If Not ProductValidationsAssignment.ContainsKey(r("SystemType").ToString()) Then
                        ProductValidationsAssignment.Add(r("SystemType").ToString(), New List(Of clsProductValidation)())
                    End If


                    Dim mkey As Integer = r.Item("FK_Translation_Key_Message")
                    Dim cmkey As Integer = r.Item("FK_Translation_Key_correctMessage")

                    If iq.Translations.ContainsKey(mkey) AndAlso iq.Translations.ContainsKey(cmkey) Then

                        Dim mt As clsTranslation = iq.Translations(mkey)
                        Dim cmt As clsTranslation = iq.Translations(cmkey)

                        Dim pv As clsProductValidation = New clsProductValidation() _
                                                         With {.ID = r("ID"), _
                                                               .CheckAttribute = r("CheckAttribute").ToString(), _
                                                               .DependantOptType = r("DependantOptType").ToString(), _
                                                               .Message = iq.Translations(r("FK_Translation_Key_Message")), _
                                                               .CorrectMessage = If(iq.Translations.ContainsKey(r("FK_Translation_Key_CorrectMessage")), iq.Translations(r("FK_Translation_Key_CorrectMessage")), Nothing), _
                                                               .RequiredOptType = r("OptType").ToString(), _
                                                               .RequiredQuantity = r("RequiredQuantity"), _
                                                               .Severity = [Enum].Parse(GetType(EnumValidationSeverity), r("Severity").ToString()),
                                                               .ValidationType = [Enum].Parse(GetType(enumValidationType), r("ValidationType").ToString()), _
                                                               .DependantCheckAttribute = If(r("DependantCheckAttribute") Is DBNull.Value, Nothing, r("DependantCheckAttribute")), _
                                                               .DependantCheckAttributeValue = If(r("DependantCheckAttributeValue") Is DBNull.Value, Nothing, r("DependantCheckAttributeValue")), _
                                                               .CheckAttributeValue = If(r("CheckAttributeValue") Is DBNull.Value, Nothing, r("CheckAttributeValue")), _
                                                               .ValidationMessageType = [Enum].Parse(GetType(enumValidationMessageType), r("ValidationMessageType").ToString()), _
                                                               .LinkOptType = r("LinkOptType").ToString(), _
                                                               .LinkTechnology = r("LinkTechnology").ToString(), _
                                                               .LinkOptionFamily = r("LinkOptionFamily").ToString}

                        ProductValidationsAssignment(r("SystemType").ToString()).Add(pv)
                    Else
                        duds += 1

                    End If
                End While
            End If

        Catch ex As Exception
            ErrorLog.Add(ex)
            Return ex.Message
        End Try

    End Function

    Private Function LoadUserStates(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String
        Try
            r = da.DBExecuteReader(con, "Select states from UserStates WHERE HostName='" + Environment.MachineName + "' order by datetime desc")
            iq.seshDic = New Dictionary(Of ULong, Dictionary(Of String, Object))

            If r.HasRows Then
                While r.Read

                    Dim states As List(Of clsUserState) = New List(Of clsUserState)()
                    Dim x As XmlSerializer = New XmlSerializer(states.GetType())
                    Using t As StringReader = New StringReader(r("states"))
                        states = x.Deserialize(t)
                        For Each us In states
                            If Not iq.OldUserSessions.ContainsKey(us.lid) Then   'Nick added as a 'quick fix' 10/12/2014 10:58
                                iq.OldUserSessions.Add(us.lid, us)
                            End If
                        Next
                    End Using
                End While
            End If
            Return String.Format("Importing existing user sessions: {0} imported", iq.seshDic.Count)
        Catch ex As Exception
            Return "Failed: " & ex.Message
        End Try
    End Function

    Public Function LoadUserState(lid As UInt64) As Boolean

        Dim loaded As Boolean = False

        If iq.OldUserSessions.ContainsKey(lid) Then

            Try

                Dim us As clsUserState = iq.OldUserSessions(lid)
                If iq.seshDic.ContainsKey(us.lid) Then Return False

                iq.seshDic.Add(us.lid, New Dictionary(Of String, Object))
                iq.seshDic(us.lid).Add("path", us.path)
                iq.seshDic(us.lid).Add("Root", us.root)
                iq.seshDic(us.lid).Add("foci", us.foci)
                iq.seshDic(us.lid).Add("QuoteID", us.QuoteID)
                iq.seshDic(us.lid).Add("showOnly", us.showOnly)
                iq.seshDic(us.lid).Add("AgentAccount", iq.Accounts(us.AgentAccount))
                iq.seshDic(us.lid).Add("BuyerAccount", iq.Accounts(us.BuyerAccount))
                iq.seshDic(us.lid).Add("treeCursorPath", us.treeCursorPath)
                iq.seshDic(us.lid).Add("branchStates", us.branchStates.ToDictionary(Function(kvp) kvp.Key, Function(kvp) kvp.Value))
                iq.seshDic(us.lid).Add("Paradigm", us.Paradigm)

                'Load nescessary details for accounts
                Dim buyerAccount As clsAccount = CType(iq.sesh(us.lid, "BuyerAccount"), clsAccount)
                Dim agentAccount As clsAccount = CType(iq.sesh(us.lid, "AgentAccount"), clsAccount)

                TagPromoBranches(buyerAccount, errorMessages)
                agentAccount.SellerChannel.IsCloneOf.LoadVariants(errorMessages, 0.1)

                If Not agentAccount.SellerChannel.IsCloneOf.stockLoaded Then
                    agentAccount.SellerChannel.IsCloneOf.LoadStock()
                End If

                If Not agentAccount.SellerChannel.IsCloneOf.pricesLoadedFor.ContainsKey(agentAccount.Priceband) Then
                    agentAccount.SellerChannel.IsCloneOf.LoadPrices(agentAccount.Priceband, errorMessages)
                End If

                Dim rgn As clsRegion = agentAccount.SellerChannel.IsCloneOf.Region
                If Not HP.listPricesLoadedFor.ContainsKey(rgn) OrElse HP.listPricesLoadedFor(rgn) = 0 Then
                    HP.LoadPrices(Everyone, errorMessages, agentAccount.SellerChannel.Region)
                End If

                'Add quote in 
                agentAccount.LoadQuotes(Val(buyerAccount.ID))
                If us.QuoteID IsNot Nothing AndAlso us.QuoteID <> 0 Then agentAccount.Quotes(us.QuoteID).LoadItems(errorMessages)

                'Matrix Headers
                iq.seshDic(us.lid).Add("screenHeaders", New Dictionary(Of String, clsScreenHeader))
                Dim mhs = CType(iq.sesh(us.lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
                For Each mh In us.ScreenHeaders
                    Dim bi As clsBranchInfo = New clsBranchInfo(us.lid, mh.Path, Nothing, 70, enumParadigm.errorNotSet, errorMessages)  'Note the clsBranchInfo() constructor poppulates other properties internally' (from branchstate)
                    Dim descendants = bi.visibleChildren(errorMessages, True, 0, 0, False, True)
                    Dim nmh = New clsScreenHeader(bi, descendants, mh.QuickFiltersVisible)
                    For Each f In mh.Filters
                        Dim d = New Dictionary(Of clsFilter, List(Of Long))
                        For Each fil In f.Value
                            d.Add(iq.Filters(fil.Key.ID), fil.Value)
                        Next
                        nmh.Filters.Add(iq.Fields(f.Key), d)
                    Next
                    For Each s In mh.Sorts
                        If nmh.sorts.ContainsKey(s.Key) Then
                            nmh.sorts(s.Key).columnid = s.Value.columnid
                            nmh.sorts(s.Key).Direction = s.Value.Direction
                            nmh.sorts(s.Key).Priority = s.Value.Priority
                        Else
                            nmh.sorts.Add(s.Key, s.Value)
                        End If

                    Next
                Next

                ' Pick up any mop-up values
                If Not us.mopUpvalues Is Nothing Then
                    For Each kvp In us.mopUpvalues
                        If Not iq.seshDic(us.lid).ContainsKey(kvp.Key) Then
                            iq.seshDic(us.lid).Add(kvp.Key, kvp.Value)
                        End If
                    Next
                End If

                loaded = True

            Catch ex As Exception

                ErrorLog.Add(ex)

            End Try

        End If

        Return loaded

    End Function

    Private Function LoadFilters() As String

        Filters = New Dictionary(Of Integer, clsFilter)

        Dim afilter As clsFilter

        afilter = New clsFilter(1, "SW", iq.AddTranslation("Starts with", English, "FT", 0, Nothing, 0, False), "[col] LIKE '[filterValue]*'")
        afilter = New clsFilter(2, "EW", iq.AddTranslation("Ends with", English, "FT", 0, Nothing, 0, False), "[col] LIKE '*[filterValue]'")
        afilter = New clsFilter(3, "CN", iq.AddTranslation("Contains", English, "FT", 0, Nothing, 0, False), "[col] LIKE '*[filterValue]*'")

        'afilter = New clsFilter(4, "ONLY", iq.AddTranslation("Only", English, "FT"), "[col]= '[filterValue]'")             '
        ' afilter = New clsFilter(5, "EX", iq.AddTranslation("Excluding", English, "FT"), "[col]<>'[filterValue]'")

        afilter = New clsFilter(4, "ONLY", iq.AddTranslation("Only", English, "FT", 0, Nothing, 0, False), "[col]= [filterValue]")             'NOTE - these filter by the numeric values - which is faster
        afilter = New clsFilter(5, "EX", iq.AddTranslation("Excluding", English, "FT", 0, Nothing, 0, False), "[col]<>[filterValue]")

        ' afilter = New clsFilter("HN", iq.AddTranslation("HavingStarts with", English, "FT"), "[col]=[filterValue]")
        ' afilter = New clsFilter("HNL", iq.AddTranslation("Starts with", English, "FT"), "[col]<=[filterValue]")
        ' afilter = New clsFilter("HNM", iq.AddTranslation("Starts with", English, "FT"), "[col]>=[filterValue]")

        afilter = New clsFilter(6, "GE", iq.AddTranslation("Greater than or Equal to this", English, "FT", 0, Nothing, 0, False), "[col]>=[filterValue]")

        afilter = New clsFilter(7, "EQ", iq.AddTranslation("Equal to this", English, "FT", 0, Nothing, 0, False), "[col]=[filterValue]")
        afilter = New clsFilter(8, "LE", iq.AddTranslation("Less than or Equal to this", English, "FT", 0, Nothing, 0, False), "[col]<=[filterValue]")
        afilter = New clsFilter(9, "PM10", iq.AddTranslation("within 10% of this", English, "FT", 0, Nothing, 0, False), "[col]>=[filterValue]*.9 and [col]<=[filterValue]*1.1")
        afilter = New clsFilter(10, "PM20", iq.AddTranslation("within 20% of this", English, "FT", 0, Nothing, 0, False), "[col]>=[filterValue]*.8 and [col]<=[filterValue]*1.2")

        afilter = New clsFilter(11, "B4", iq.AddTranslation("before", English, "FT", 0, Nothing, 0, False), "[col]<[filterValue]")
        afilter = New clsFilter(12, "AFT", iq.AddTranslation("After", English, "FT", 0, Nothing, 0, False), "[col]>[filterValue]")
        afilter = New clsFilter(13, "ON", iq.AddTranslation("On", English, "FT", 0, Nothing, 0, False), "[col]=[filterValue]")

        afilter = New clsFilter(14, "T", iq.AddTranslation("Only Ticked", English, "FT", 0, Nothing, 0, False), "[col]=true")
        afilter = New clsFilter(15, "F", iq.AddTranslation("Only UnTicked", English, "FT", 0, Nothing, 0, False), "[col]=false")

        afilter = New clsFilter(16, "WITH", iq.AddTranslation("With", English, "FT", 0, Nothing, 0, False), "[col]<>0")
        afilter = New clsFilter(17, "WITHOUT", iq.AddTranslation("Without", English, "FT", 0, Nothing, 0, False), "[col]=0") '"(isnull([col],-100)=-100)")

        afilter = New clsFilter(18, "GZ", iq.AddTranslation("In stock", English, "FT", 0, Nothing, 0, False), "[col]>0")
        afilter = New clsFilter(19, "FS", iq.AddTranslation("Like this and faster", English, "FT", 0, Nothing, 0, False), "[col]>=[filterValue]")
        afilter = New clsFilter(20, "SL", iq.AddTranslation("Like this and slower", English, "FT", 0, Nothing, 0, False), "[col]<=[filterValue]")
        afilter = New clsFilter(21, "LEGE", iq.AddTranslation("Between", English, "FT", 0, Nothing, 0, False), "[col]<=[filterValue]")

        'non
        ' afilter = New clsFilter(21, "TXT", iq.AddTranslation("Containing", English, "FT"), "[col] like '*[filterValue]*'")

        Return "loaded " & Filters.Values.Count & " Filters<br/>"

    End Function



    Public Function UserAccountName(UserID As Integer, AccountID As Integer) As String

        Try
            Return iq.Users(UserID).Email & "/" & iq.Accounts(AccountID).SellerChannel.Name
        Catch
            Return "invalid/unknown"
        End Try


    End Function
    Public Sub RecurseChildren(parent As clsBranch, ByRef count As Integer, ByVal path$)

        'walks' the tree of every possible branch - soley to count them

        Dim bp$
        For Each child In parent.childBranches.Values
            bp$ = path$ & "." & child.ID
            'If Not Prunes.ContainsKey(bp$) Then 'dont recurse into pruned braches
            count += 1
            RecurseChildren(child, count, bp$)
            'End If
        Next

    End Sub
    Public Function Retract(branch As clsBranch, username$, ByRef errormessages As List(Of String)) As Boolean

        'reParents each child of Branch under Branches' parent

        'Check that we are only retracting a branch containing other items and not a 'model' with a quantity etc
        If branch.HasSKU() Then
            errormessages.Add("!Error - cannot retract a branch with a SKU, it has no children!")
            Return False
        End If


        Dim tomove As List(Of clsBranch) = New List(Of clsBranch) 'we need to build a list - becuase we can't iterate a collection we're modifying (branch.childbranches)

        For Each child In branch.childBranches.Values
            tomove.Add(child)
        Next

        For Each child In tomove
            child.Parent = branch.Parent
            '  child.Parent.childBranches.Add(child.ID, child)  'so it appears immediately
            child.Update(errormessages)
        Next

        'finally remove the branch we're retracting

        branch.delete(errormessages)

        Return True

    End Function
    Public Function Prune(path As String, userName As String) As Integer 'prunes are done by path (branch is not unique or specific!)

        'Creates a new Prune, 
        'NB - *all* prunes have a path - and take off, *one* instance of a branch (and all its descendants)
        'if you want to remove a branch from everywhere - delete it !

        'Persists it to the DB and adds it to the global prunes list (iq.prunes)
        'Thereby preventing the branch from appearing when (re)openend
        'Username is for audit trail purposes - and so that prunes can be easily located/removed in future

        'If iq.Prunes.ContainsKey(path) Then
        '   MsgBox("That branch is already pruned")
        'Return 0
        '        Else
        Dim aprune As clsPrune
        aprune = New clsPrune(path$, New NullableInt, userName)
        Return aprune.ID
        '       End If

    End Function
    Private Function LoadProductAttributes(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String
        Try
            Dim count As Integer
            Dim sql$

            'sql$ = "Select productattribute.id,FK_Product_Id,FK_Attribute_id,Numericvalue,fk_translation_key_text, a.code as acode,u.code as ucode from ProductAttribute "
            ' sql$ &= "Join [attribute] a on a.id=fk_attribute_id "
            ' sql$ &= "Join unit u on u.id=fk_unit_id"
            sql$ = "Select id,FK_Product_Id,FK_Attribute_id,Numericvalue,fk_translation_key_text, fk_unit_id  from ProductAttribute "
            sql$ &= "WHERE deleted = 0"

            r = da.DBExecuteReader(con, sql$)

            Dim failed As Integer = 0
            Dim PA As clsProductAttribute  ' the act of creating a product attribute - adds it to the specified product
            Dim indexedSKUs As Integer
            Dim numval As Single
            Dim translationKey As Integer

            Dim missingProducts As Integer = 0
            Dim missingAttributes As Integer = 0
            Dim missingUnits As Integer = 0
            Dim bad As Integer = 0

            Dim toupdate As New HashSet(Of clsProductAttribute)  'runtime fix for rempapping productacttributes of duplictaed products (this code can be removed in the long term)
            Dim todel As New HashSet(Of String)                  '

            If r.HasRows Then
                While r.Read
                    Dim aId As Integer = CInt(r.Item("fk_attribute_id"))
                    If iq.Attributes.ContainsKey(aId) Then
                        Dim attrib As clsAttribute = iq.Attributes(aId)
                        numval = CSng(r.Item("NumericValue"))
                        Dim uId As Integer = CInt(r.Item("fk_unit_id"))

                        Dim updateIt As Boolean = False
                        If iq.Units.ContainsKey(uId) Then
                            Dim unit As clsUnit = iq.Units(uId)
                            Dim product As clsProduct = Nothing
                            Dim pid As Integer = r.Item("FK_product_id")

                            If iq.Products.ContainsKey(pid) Then
                                product = iq.Products(pid)
                            Else
                                If REMAPS.ContainsKey(pid) Then
                                    product = REMAPS(pid)   'UGLY LIVE FIXUP OF DATA
                                    If product.i_Attributes_Code.ContainsKey(attrib.Code) Then
                                        todel.Add(r.Item("ID"))
                                        Continue While
                                    Else
                                        updateIt = True '
                                    End If
                                Else
                                    'Beep()
                                    product = Nothing
                                End If
                            End If

                            If product Is Nothing Then
                                missingProducts += 1
                            Else
                                Dim tl As clsTranslation
                                If Not IsDBNull(r.Item("fk_translation_key_text")) Then
                                    translationKey = CInt(r.Item("fk_translation_key_text"))
                                    If iq.Translations.ContainsKey(translationKey) Then
                                        tl = iq.Translations(translationKey)
                                    Else
                                        tl = Nothing
                                    End If
                                Else
                                    tl = Nothing
                                End If

                                PA = New clsProductAttribute(CInt(r.Item("Id")), product, attrib, numval, unit, tl)

                                If updateIt Then toupdate.Add(PA)

                                count += 1
                            End If
                        Else
                            missingUnits += 1
                        End If
                    Else
                        missingAttributes += 1
                    End If

                End While
            End If
            r.Close()

            If todel.Count Then
                da.DBExecutesql("DELETE FROM productattribute WHERE id in (" & Join(todel.ToArray, ",") & ")")
            End If
            For Each PA In toupdate
                PA.update(errorMessages)
            Next



            Return "Loaded " & count & " ProductAttributes. " & missingAttributes & "Missing attributes" & "," & missingUnits & " missing units, " & missingProducts & " missing (deleted ?) products." & " Deleted " & todel.Count & " duplicate productattributes, updated (remapped) " & toupdate.Count
        Catch ex As Exception
            ErrorLog.Add(ex)
            Return "Failed: " & ex.Message
        End Try
    End Function

    Private Function CleanProducts(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        ' Build a temporary list of PL code to Manufacturer mappings
        Dim plcodeLookup = New Dictionary(Of String, String)
        Dim Sql = "Select plCode, mfrCode from PLCodeLookup"
        r = da.DBExecuteReader(con, Sql)
        If r.HasRows Then
            While r.Read
                plcodeLookup.Add(r.Item("plCode"), r.Item("mfrCode"))
            End While
        End If
        r.Close()

        For Each product In iq.Products.Values

            Dim update As Boolean = False

            ' Fill in any missing Product.SKU values from the relevant attribute
            If String.IsNullOrEmpty(product.SKU) Then
                If product.Attributes IsNot Nothing Then
                    If product.i_Attributes_Code.ContainsKey("mfrsku") AndAlso product.i_Attributes_Code("mfrsku").Count > 0 Then
                        product.SKU = product.i_Attributes_Code("mfrsku")(0).Translation.text(English)
                        update = True
                    End If
                    '    If (product.isOption Or product.isSystem) And String.IsNullOrEmpty(product.SKU) Then Stop
                End If
            End If

            ' Fill in any missing Product.mfrCode values
            If String.IsNullOrEmpty(product.mfrCode) Then
                If product.Attributes IsNot Nothing Then
                    If product.i_Attributes_Code.ContainsKey("mfrsku") AndAlso product.i_Attributes_Code("mfrsku").Count > 0 Then
                        If Not product.i_Attributes_Code("mfrsku")(0).Attribute.Translation.text(English).Contains("###") Then
                            If product.isSystem Then
                                If product.ProductType.Code = "DTO" OrElse product.ProductType.Code = "NBK" Then    ' Desktop/Notebook
                                    product.mfrCode = "HPI"
                                ElseIf Not product.ProductType.Code = "WTY" AndAlso Not product.ProductType.Code = "EDU" AndAlso Not product.ProductType.Code = "SVC" Then
                                    product.mfrCode = "HPE"
                                End If
                                update = True
                            Else
                                If product.i_Attributes_Code.ContainsKey("plcode") AndAlso product.i_Attributes_Code("plcode").Count > 0 Then
                                    Dim plCode As String = product.i_Attributes_Code("plcode")(0).Translation.text(English)
                                    If plcodeLookup.ContainsKey(plCode) Then
                                        product.mfrCode = plcodeLookup(plCode)
                                        update = True
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            End If

            If update Then
                product.update(errorMessages)
            End If

        Next

        Return "Cleaned product records"

    End Function

    Private Function LoadHPServiceLevels(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        iq.ServiceLevelResponse = New Dictionary(Of Integer, clsResponse)
        iq.ServiceLevelServiceType = New Dictionary(Of Integer, clsServiceType)
        iq.ServiceLevels = New Dictionary(Of Integer, clsServiceLevel)
        iq.ServiceLevelTROAA = New Dictionary(Of Integer, clsTROAA)
        iq.ServiceLevelAttributeMap = New Dictionary(Of String, clsAttribute)
        iq.CarePackLastRefresh = New Dictionary(Of String, DateTime)

        ' Response
        r = da.DBExecuteReader(con, "SELECT [ID], [mfrCode], [FK_Translation_Key_Title], [FK_Translation_Key_Description], [ResponseDefault] FROM [Response]")
        While r.Read

            Dim response As clsResponse = New clsResponse()

            With response
                .ID = r.Item("ID")
                If Not IsDBNull(r.Item("mfrCode")) Then .mfrCode = r.Item("mfrCode")

                Dim transKey As Integer = CInt(r.Item("FK_Translation_Key_Title"))
                If iq.Translations.ContainsKey(transKey) Then .Title = iq.Translations(transKey)

                transKey = CInt(r.Item("FK_Translation_Key_Description"))
                If iq.Translations.ContainsKey(transKey) Then .Description = iq.Translations(transKey)

                If Not IsDBNull(r.Item("ResponseDefault")) Then .ResponseDefault = r.Item("ResponseDefault")

            End With

            iq.ServiceLevelResponse.Add(response.ID, response)

        End While
        r.Close()

        ' Service Type
        r = da.DBExecuteReader(con, "SELECT [ID], [mfrCode], [FK_Translation_Key_Title], [FK_Translation_Key_Description], [ServiceTypeDefault] FROM [ServiceType]")
        While r.Read

            Dim serviceType As clsServiceType = New clsServiceType()

            With serviceType
                .ID = r.Item("ID")
                If Not IsDBNull(r.Item("mfrCode")) Then .mfrCode = r.Item("mfrCode")

                Dim transKey As Integer = CInt(r.Item("FK_Translation_Key_Title"))
                If iq.Translations.ContainsKey(transKey) Then .Title = iq.Translations(transKey)

                If Not IsDBNull(r.Item("FK_Translation_Key_Description")) Then
                    transKey = CInt(r.Item("FK_Translation_Key_Description"))
                    If iq.Translations.ContainsKey(transKey) Then .Description = iq.Translations(transKey)
                End If

                If Not IsDBNull(r.Item("ServiceTypeDefault")) Then .ServiceTypeDefault = r.Item("ServiceTypeDefault")

            End With

            iq.ServiceLevelServiceType.Add(serviceType.ID, serviceType)

        End While
        r.Close()

        ' Service Level Map
        r = da.DBExecuteReader(con, "SELECT [ID], [mfrCode], [ServiceLevel], [ServiceLevelGroup], [SuperGroup], [FK_Translation_Key_Description], [Duration], [PostWarranty], [Disabled], [FK_ServiceType_ID], [FK_Response_ID], [hpeDMR], [hpeCDMR], [hpiADP], [hpiDMR], [hpiTravel], [hpiTracing], [hpiTheft] FROM [ServiceLevelMap]")
        While r.Read

            Dim serviceLevel As clsServiceLevel = New clsServiceLevel()

            With serviceLevel
                .ID = r.Item("ID")
                If Not IsDBNull(r.Item("mfrCode")) Then .MfrCode = r.Item("mfrCode")
                If Not IsDBNull(r.Item("ServiceLevel")) Then .ServiceLevel = r.Item("ServiceLevel")
                If Not IsDBNull(r.Item("ServiceLevelGroup")) Then .ServiceLevelGroup = r.Item("ServiceLevelGroup")
                If Not IsDBNull(r.Item("SuperGroup")) Then .SuperGroup = r.Item("SuperGroup")

                Dim transKey As Integer = CInt(r.Item("FK_Translation_Key_Description"))
                If iq.Translations.ContainsKey(transKey) Then .Description = iq.Translations(transKey)

                If Not IsDBNull(r.Item("Duration")) Then .Duration = r.Item("Duration")
                If Not IsDBNull(r.Item("PostWarranty")) Then .PostWarranty = r.Item("PostWarranty")
                If Not IsDBNull(r.Item("Disabled")) Then .Disabled = r.Item("Disabled")

                If Not IsDBNull(r.Item("FK_Response_ID")) Then
                    Dim responseID As Integer = r.Item("FK_Response_ID")
                    If iq.ServiceLevelResponse.ContainsKey(responseID) Then
                        .Response = iq.ServiceLevelResponse(responseID)
                    End If
                End If

                If Not IsDBNull(r.Item("FK_ServiceType_ID")) Then
                    Dim serviceTypeID As Integer = r.Item("FK_ServiceType_ID")
                    If iq.ServiceLevelServiceType.ContainsKey(serviceTypeID) Then
                        .ServiceType = iq.ServiceLevelServiceType(serviceTypeID)
                    End If
                End If

                If Not IsDBNull(r.Item("hpeDMR")) Then .HpeDmr = r.Item("hpeDMR")
                If Not IsDBNull(r.Item("hpeCDMR")) Then .HpeCdmr = r.Item("hpeCDMR")
                If Not IsDBNull(r.Item("hpiADP")) Then .HpiAdp = r.Item("hpiADP")
                If Not IsDBNull(r.Item("hpiDMR")) Then .HpiDmr = r.Item("hpiDMR")
                If Not IsDBNull(r.Item("hpiTravel")) Then .HpiTravel = r.Item("hpiTravel")
                If Not IsDBNull(r.Item("hpiTracing")) Then .HpiTracing = r.Item("hpiTracing")
                If Not IsDBNull(r.Item("hpiTheft")) Then .HpiTheft = r.Item("hpiTheft")
            End With

            iq.ServiceLevels.Add(serviceLevel.ID, serviceLevel)

        End While
        r.Close()

        ' TROAA
        r = da.DBExecuteReader(con, "SELECT [ID], [SysFamily], [SlotTypeCode], [ServiceLevel], [DisplayOrder], [FK_ServiceLevelMap_ID] FROM [TROAA]")
        While r.Read

            Dim troaa As clsTROAA = New clsTROAA()

            With troaa
                .ID = r.Item("ID")
                If Not IsDBNull(r.Item("SysFamily")) Then .SysFamily = r.Item("SysFamily")
                If Not IsDBNull(r.Item("SlotTypeCode")) Then .SlotTypeCode = r.Item("SlotTypeCode")
                If Not IsDBNull(r.Item("ServiceLevel")) Then .ServiceLevelID = r.Item("ServiceLevel")
                If Not IsDBNull(r.Item("DisplayOrder")) Then .DisplayOrder = r.Item("DisplayOrder")
                If Not IsDBNull(r.Item("FK_ServiceLevelMap_ID")) Then
                    Dim serviceLevelID As Integer = r.Item("FK_ServiceLevelMap_ID")
                    If iq.ServiceLevels.ContainsKey(serviceLevelID) Then
                        .ServiceLevel = iq.ServiceLevels(serviceLevelID)
                    End If
                End If
            End With

            iq.ServiceLevelTROAA.Add(troaa.ID, troaa)

        End While
        r.Close()

        ' Service Level Attribute Map
        r = da.DBExecuteReader(con, "SELECT [ID], [Code], [FK_Attribute_Code] FROM [ServiceLevelAttributeMap]")
        While r.Read

            Dim code As String = r.Item("Code")
            Dim serviceLevelAttributeCode As String = r.Item("FK_Attribute_Code")

            If iq.i_attribute_code.ContainsKey(serviceLevelAttributeCode) Then
                iq.ServiceLevelAttributeMap.Add(code, iq.i_attribute_code(serviceLevelAttributeCode))
            End If

        End While
        r.Close()


        Return "Loaded HP Service Pack Levels"

    End Function


    Public Function LoadPoints(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String


        r = da.DBExecuteReader(con, "SELECT ID,fk_Product_id,fk_scheme_id, points FROM [Points]")

        Dim product As clsProduct
        Dim scheme As clsScheme

        Dim points As Integer = 0
        While r.Read
            If iq.Products.ContainsKey(CInt(r.Item("fk_product_id"))) Then

                Dim pid As Integer = CInt(r.Item("fk_product_id"))
                If iq.Products.ContainsKey(pid) Then
                    product = iq.Products(pid)
                Else
                    product = iq.REMAPS(pid)
                End If

                scheme = iq.Schemes(CInt(r.Item("fk_scheme_id")))
                product.Points(scheme) = CInt(r.Item("Points"))
                points += 1
            End If
        End While
        r.Close()

        Return "Loaded " & points & " loyalty points onto products"


    End Function


    Public Function LoadSchemes(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        iq.Schemes.Clear()

        r = da.DBExecuteReader(con, "SELECT ID,code,fk_translation_key_name,StartDate,EndDate,fk_region_id FROM [Scheme]")

        Dim ascheme As clsScheme

        Dim active As Integer = 0

        Dim id As Integer
        Dim code As String
        Dim startdate As Date
        Dim enddate As Date
        While r.Read
            id = CInt(r.Item("Id"))
            code = (r.Item("code").ToString())
            startdate = CDate(r.Item("startdate"))
            enddate = CDate(r.Item("enddate"))
            ascheme = New clsScheme(id, code, iq.Translations(CInt(r.Item("fk_translation_key_name"))), iq.Regions(CInt(r.Item("fk_region_id"))), startdate, enddate)
            If startdate < Now And enddate > Now Then
                active += 1
            End If
        End While
        r.Close()

        Return "Loaded " & iq.Schemes.Count & " loyalty schemes, " & active & " of which " & active & " are current/active."


    End Function

    Public Function LoadBundles(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String


        'the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems
        r = da.DBExecuteReader(con, "SELECT ID,fk_translation_key_name,OPGref,code,validFrom,validTo,fk_region_id FROM [Bundle]")

        iq.Bundles.Clear()
        iq.i_Bundle_code.Clear()

        'Load the bundles
        Dim region As clsRegion = Nothing
        Dim Bundle As clsBundle

        Dim Active As Integer = 0
        Dim bundleName As clsTranslation = Nothing
        Dim tk As Integer
        Dim fromDate As Date
        Dim toDate As Date
        Dim regionID As Integer
        If r.HasRows Then
            While r.Read
                regionID = CInt(r.Item("fk_region_id"))
                tk = CInt(r.Item("fk_translation_key_name"))
                region = iq.Regions(regionID)
                fromDate = CDate(r.Item("validfrom"))
                toDate = CDate(r.Item("validto"))

                bundleName = iq.Translations(tk)
                Bundle = New clsBundle(CInt(r.Item("ID")), bundleName, r.Item("OPGref").ToString(), r.Item("code").ToString(), region, fromDate, toDate)
                If Now > Bundle.validFrom And Now < Bundle.validTo Then Active += 1 'count the 'active' bundles
            End While
        End If
        r.Close()


        'load the items into the bundles 

        r = da.DBExecuteReader(con, "SELECT ID,fk_Bundle_id,fk_product_id,price,rebate,fk_currency_id,qtyMin from BundleItem")

        Dim bundleitem As clsBundleItem
        Dim bundleItems As Integer = 0
        Dim price As NullablePrice
        Dim currencyId As Integer
        Dim bundleID As Integer
        Dim productID As Integer
        Dim rowID As Integer
        Dim rebate As Single
        Dim minQty As Integer
        If r.HasRows Then
            While r.Read
                currencyId = CInt(r.Item("fk_currency_id"))
                price = New NullablePrice(r.Item("Price"), iq.Currencies(currencyId), False)
                bundleID = CInt(r.Item("fk_bundle_id"))
                productID = CInt(r.Item("fk_product_id"))
                rowID = CInt(r.Item("ID"))
                rebate = CSng(r.Item("rebate"))
                minQty = CInt(r.Item("qtyMin"))
                'see the constructor ... it adds the item to the bundle and
                Bundle = iq.Bundles(bundleID)
                bundleitem = New clsBundleItem(rowID, Bundle, iq.Products(productID), price, rebate, minQty)
                bundleItems += 1
            End While
        End If
        r.Close()


        'Attach the bundles to the systems

        r = da.DBExecuteReader(con, "SELECT ID,fk_Bundle_id,fk_product_id_system,rebate from BundleSystem")

        Dim bundlesystem As clsBundleSystem
        Dim bundleSystems As Integer = 0
        Dim system As clsProduct

        If r.HasRows Then
            While r.Read
                bundleID = CInt(r.Item("fk_bundle_id"))
                productID = CInt(r.Item("fk_product_id_system"))
                rowID = CInt(r.Item("ID"))
                Bundle = iq.Bundles(bundleID)
                system = iq.Products(productID)
                rebate = CSng(r.Item("rebate"))
                bundlesystem = New clsBundleSystem(rowID, Bundle, system, rebate)
                bundleSystems += 1

            End While
        End If
        r.Close()

        Return "Loaded " & iq.Bundles.Count & " bundles, " & Active & " of which are current/active, applied to" & bundleSystems & " systems ,There were a total of " & bundleItems & " bundle items."

    End Function



    Public Function LoadFlex(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        'the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems

        r = da.DBExecuteReader(con, "SELECT ID,description,OPGref,validFrom,validTo,fk_currency_id,minOptions,maxOptions,OPGSysType  FROM [Flex]")

        iq.FlexOPGs.Clear()

        'Load the FlexOPGs

        Dim active As Integer = 0
        Dim currency As clsCurrency
        Dim flexOPG As clsFlexOPG
        Dim currencyID As Integer
        Dim fromDate As Date
        Dim toDate As Date
        Dim minOptions As Integer
        Dim maxOptions As Integer
        Dim OPGSysType As String
        Dim rowID As Integer
        If r.HasRows Then
            While r.Read
                currencyID = CInt(r.Item("fk_currency_id"))
                fromDate = CDate(r.Item("validfrom"))
                toDate = CDate(r.Item("validto"))
                currency = iq.Currencies(currencyID)
                minOptions = CInt(r.Item("MinOptions"))
                maxOptions = If(IsDBNull(r.Item("MaxOptions")), Nothing, CInt(r.Item("MaxOptions")))
                OPGSysType = If(IsDBNull(r.Item("OPGSysType")), "", CStr(r.Item("OPGSysType")))

                rowID = CInt(r.Item("ID"))
                flexOPG = New clsFlexOPG(rowID, r.Item("OPGref").ToString(), r.Item("Description").ToString(), fromDate, toDate, currency, minOptions, maxOptions, OPGSysType)
                If flexOPG.isCurrent Then active += 1 'count the 'active' bundles            
            End While
        End If
        r.Close()

        Return "Loaded " & iq.FlexOPGs.Count & " Flex OPGs, " & active & " of which are current/active"

    End Function

    Public Function LoadFlexRegions(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        'the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems

        r = da.DBExecuteReader(con, "SELECT ID,fk_flex_id,fk_region_id FROM [FlexRegion]")

        Dim flexOPG As clsFlexOPG
        Dim frs As Integer = 0
        Dim region As clsRegion
        Dim flexID As Integer
        Dim regionID As Integer
        If r.HasRows Then
            While r.Read
                flexID = CInt(r.Item("fk_flex_id"))
                regionID = CInt(r.Item("fk_region_id"))
                flexOPG = iq.FlexOPGs(flexID)
                region = iq.Regions(regionID)
                flexOPG.regions.Add(region.ID, region)
                frs += 1
            End While
        End If
        r.Close()

        Return "Loaded " & frs & " Flex Regions "

    End Function


    Public Function LoadFlexLines(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String
        'load the lines into the FlexOPGs

        r = da.DBExecuteReader(con, "SELECT ID,fk_flex_id,fk_product_id,rebate,validfrom,validto FROM FlexLine") '- Rebate = Additionl disc% * list - do at import !!

        Dim flexline As clsFlexLine

        Dim lines As Integer = 0
        Dim activelines As Integer = 0
        Dim flexID As Integer
        Dim pID As Integer
        Dim rebate As Decimal
        Dim fromDate As Date
        Dim toDate As Date
        Dim rowID As Integer
        Dim product As clsProduct

        If r.HasRows Then
            While r.Read
                flexID = CInt(r.Item("fk_flex_id"))
                pID = CInt(r.Item("fk_product_id"))
                rebate = CDec(r.Item("rebate"))
                fromDate = CDate(r.Item("validfrom"))
                toDate = CDate(r.Item("validto"))
                rowID = CInt(r.Item("ID"))

                '                If FlexOPGs(flexID).OPGRef = "90989236" And iq.Products(productID).sku = "712317-421" Then Stop

                '  price = New nullablePrice(r.Item("Price"), iq.Currencies(r.Item("fk_currency_id")))

                If iq.Products.ContainsKey(pID) Then
                    product = iq.Products(pID)
                Else
                    product = iq.REMAPS(pID)
                End If

                flexline = New clsFlexLine(rowID, iq.FlexOPGs(flexID), product, rebate, fromDate, toDate)
                
                lines += 1
                If flexline.isCurrent Then activelines += 1
            End While
        End If

        r.Close()

        Return "There were a total of " & lines & " Flex (product) lines, " & activelines & " of which are current/active."
    End Function
    Public Function LoadFlexRules(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        '   Dim rRule As SqlDataReader = da.DBExecuteReader(con, "SELECT [ID],[FK_ProductType_ID],[FK_Flex_ID],[min],[max] FROM [FlexRule]")
        r = da.DBExecuteReader(con, "SELECT [ID],[FK_ProductType_ID],[FK_Flex_ID],[min],[max],[optionalRule] FROM [FlexRule]")
        Dim lines As Integer = 0
        Dim flexRules As clsFlexRule
        Dim rowID As Integer
        Dim productTypeID As Integer
        Dim flexID As Integer
        Dim min As Integer
        Dim max As Integer
        Dim optionalRule As Boolean
        If r.HasRows Then
            While r.Read
                rowID = CInt(r("ID"))
                productTypeID = CInt(r("FK_ProductType_ID"))
                flexID = CInt(r("FK_Flex_ID"))
                min = CInt(r("min"))
                max = CInt(r("max"))
                optionalRule = CBool(r("optionalRule"))
                flexRules = New clsFlexRule(rowID, iq.FlexOPGs(flexID), iq.ProductTypes(productTypeID), min, max, optionalRule)
                lines += 1
            End While
        End If
        r.Close()
        Return "There were a total of " & lines & " Flex (product) Rules "
    End Function

    Public Function LoadAvalancheOPGs(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        'Avalanche is an HP discount scheme wich provides tiered discounts (typically 5% and 10%) on options (HDDs, Memory, cables, etc) when certain quantity thresholds are met
        'the discount (rebate) on  options is a percentage of it's *list* price
        'you can 'mix and match' options - each avalanche offer applies to one or more systems, and in one or more regions 

        'the OPG's are the 'offers' each has a start date, end date, reference and region - and each applies to a set of systems

        r = da.DBExecuteReader(con, "SELECT ID,OPGref,optMin,optMax,validFrom,validTo,fk_region_id FROM AvalancheOPG")

        iq.AvalancheOPGs.Clear()
        iq.i_OpgRef.Clear()

        Dim region As clsRegion = Nothing
        Dim AvalancheOPG As ClsAvalancheOPG
        Dim regionID As Integer
        Dim fromDate As Date
        Dim toDate As Date
        Dim rowID As Integer
        Dim opgRef As String
        Dim optMin As Integer
        Dim optMax As Integer
        If r.HasRows Then
            While r.Read
                regionID = CInt(r.Item("fk_region_id"))
                fromDate = CDate(r.Item("validfrom"))
                toDate = CDate(r.Item("validto"))
                region = iq.Regions(regionID)
                rowID = CInt(r.Item("ID"))
                opgRef = r.Item("opgref").ToString()
                optMin = CInt(r.Item("optmin"))
                optMax = CInt(r.Item("optmax"))
                If Not iq.i_OpgRef.ContainsKey(opgRef) Then
                    AvalancheOPG = New ClsAvalancheOPG(rowID, opgRef, region, fromDate, toDate, optMin, optMax)
                Else
                    Beep()
                End If
            End While
        End If
        r.Close()

        'clear them all (as re have to re-load them during import)
        For Each product In iq.Products.Values
            If product.AvalancheOPGs IsNot Nothing Then
                product.AvalancheOPGs.Clear()
            End If

        Next

        r = da.DBExecuteReader(con, "SELECT fk_product_id_system,fk_avalancheOPG_id FROM AvalancheSystem")
        Dim attached As Integer = 0
        Dim productID As Integer
        Dim avalancheID As Integer
        If r.HasRows Then

            While r.Read
                productID = CInt(r.Item("fk_product_id_system"))
                avalancheID = CInt(r.Item("fk_avalancheOPG_id"))

                iq.Products(productID).AvalancheOPGs.Add(avalancheID, iq.AvalancheOPGs(avalancheID))
                attached += 1
            End While
        End If
        r.Close()


        'now read the AvalancheOptions - which gives us the percent (of list) discount per 'ref code', under this OPG  (options have a refcode attribute)
        Dim opt As clsAvalancheOption
        Dim opts As Integer

        r = da.DBExecuteReader(con, "SELECT ID,FK_AvalancheOPG_id,LPDiscountPercent,prodRef FROM avalancheOption")
        If r.HasRows Then
            While r.Read
                avalancheID = CInt(r.Item("fk_avalancheOPG_id"))
                rowID = CInt(r.Item("ID"))
                'creating the Avalance option adds it to the OPG (those OPG's have already been attached to the relevant systems
                opt = New clsAvalancheOption(rowID, iq.AvalancheOPGs(avalancheID), r.Item("Prodref").ToString(), CSng(r.Item("LPDiscountPercent")))
                opts += 1
            End While
        End If
        r.Close()

        Return "Loaded " & iq.AvalancheOPGs.Count & " Avalanche offers and attached them to " & attached & " systems, Loaded discounts for " & opts & " Refcodes "

    End Function

    Private Function LoadSectors(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key_name from [Sector]")

        Dim count As Integer

        Dim aSector As clsSector

        If r.HasRows Then
            While r.Read
                aSector = New clsSector(CInt(r.Item("id")), CStr(r.Item("code")), iq.Translations(CInt(r.Item("fk_translation_key_name"))))
                count += 1
            End While
        End If
        r.Close()

        Return "Loaded " & count & " sectors (Business Units) "

    End Function
    Private Function LoadProducts(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT id,sku,IsSystem,IsOption,fk_producttype_id,fk_sector_id,activeFrom,activeTo,active,eol,publish,mfrCode,buCode,PLcode FROM Product where deleted = 0 order by id")

        iq.Products = New Dictionary(Of Integer, clsProduct)

        Dim ProductType As clsProductType

        Dim dupes As Integer = 0

        Dim todel As New List(Of String)

        If r.HasRows Then
            While r.Read
                If IsDBNull(r.Item("FK_ProductType_ID")) Then
                    ProductType = Nothing
                Else
                    ProductType = iq.ProductTypes(CInt(r.Item("FK_ProductType_ID")))
                End If
                'this should not be needed ! - remove !
                Dim sku$ = r("sku")

                'If sku$ = "QK765A" Then Stop

                If sku <> "" AndAlso iq.i_SKU.ContainsKey(sku) Then
                    'DELETE THIS PRODUCT AND REMAP TO THE FIRST INSTANCE
                    'Stop
                    REMAPS.Add(r.Item("ID"), i_SKU(sku))
                    todel.Add(r.Item("id"))
                    dupes += 1
                Else
                    Dim product As New clsProduct(CInt(r("id")), sku, CBool(r("isSystem")), CBool(r("isOption")), _
                    iq.Sectors(CInt(r("fk_sector_id"))), ProductType, _
                    CDate(r("activefrom")), CDate(r("activeto")), _
                    CBool(r("active")), CBool(r("EOL")), CBool(r("Publish")), _
                    CStr(r("mfrCode")), CStr(r("buCode")), CStr(r.Item("plCode")))

                    iq.Products.Add(CInt(r("id")), product)
                End If

                'Else
                'duplicated product !
                ' Beep()
                ' dupes = +1
                ' End If

            End While
        End If
        r.Close()


        If todel.Count Then
            'soft delete the 2nd and subsequent versions of the product
            Dim sql$ = "update product set deleted =1 where id in (" & Join(todel.ToArray, ",") & ")"

            da.DBExecutesql(sql$)


        End If



        Return "Loaded " & iq.Products.Count & " products"

    End Function


    Public Function LoadTranslation(Key As Integer) As String

        'used to load a single translation ("worldwide") - as part of the bootstrap process

        Dim r As SqlClient.SqlDataReader
        Dim con As SqlClient.SqlConnection = da.OpenDatabase

        r = da.DBExecuteReader(con, "Select id,[key],[text],fk_language_id,[group],[order] from translation where key=" & Key)

        Dim id As Integer = 0
        Dim Lang As clsLanguage = Nothing
        Dim Text As String
        Dim group As String
        Dim order As Integer

        Dim aTranslation As clsTranslation

        If r.HasRows Then

            While r.Read
                id = CInt(r.Item("id"))
                Key = CInt(r.Item("key"))
                Lang = iq.Languages(CInt(r.Item("fk_language_id")))
                Text = r.Item("text").ToString()
                group = r.Item("group").ToString()
                order = CInt(r.Item("order"))


                'will also add it to the translations(key)(lang) dictionary
                If iq.Translations.ContainsKey(Key) Then
                    iq.Translations(Key).addLanguage(Lang, Text, Nothing)

                Else
                    aTranslation = New clsTranslation(Key, Lang, Text, id, group, order)
                End If
            End While
        End If
        r.Close()

    End Function

    Public Function LoadTranslations(ByVal con As SqlClient.SqlConnection) As String

        Dim r As SqlClient.SqlDataReader

        r = da.DBExecuteReader(con, "SELECT id,[key],[text],fk_language_id,[group],[order] FROM translation where deleted=0") ' where deleted=0
        Dim id As Integer = 0
        Dim Lang As clsLanguage = Nothing
        Dim Key As Integer = 0
        Dim Text As String
        Dim group As String
        Dim order As Integer

        Dim aTranslation As clsTranslation

        iq.Translations.Clear()
        iq.iEnglishIndex.Clear()
        iq.KYIndex.Clear()

        If r.HasRows Then

            Dim tl As clsTranslation

            While r.Read
                id = CInt(r.Item("id"))
                Key = CInt(r.Item("key"))

                Lang = iq.Languages(CInt(r.Item("fk_language_id")))
                If Not Lang.Active Then Lang.Active = True
                Text = r.Item("text").ToString()
                group = r.Item("group").ToString()
                order = CInt(r.Item("order"))

                'each translation object exposes:-
                'Property Text As Dictionary(Of clsLanguage, String)
                'Property ID As Dictionary(Of clsLanguage, Integer)

                'will also add it to the translations(key)(lang) dictionary
                If Not iq.Translations.ContainsKey(Key) Then
                    tl = New clsTranslation(Key, Lang, Text, id, group, order)
                Else
                    '                    iq
                    tl = iq.Translations(Key)
                    tl.addLanguage(Lang, id, Text)
                End If
            End While
        End If
        r.Close()

        'for performance- and cleaner code - in particular, cellUI needs these ALOT

        Yes = iq.AddTranslation("Yes", English, "UI", 0, Nothing, 0, False)
        No = iq.AddTranslation("No", English, "UI", 0, Nothing, 0, False)
        InStock = iq.AddTranslation("in stock", English, "UI", 0, Nothing, 0, False)
        OutOfStock = iq.AddTranslation("out of stock", English, "UI", 0, Nothing, 0, False)
        Return "Loaded " & iq.Translations.Count & " translations"

    End Function
    Private Function LoadUnits(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "Select [id],[code],[symbol],fk_translation_key_name,FK_Measure_ID from [unit]")
        Dim u As clsUnit
        If r.HasRows Then
            While r.Read
                'becuase we're specifying the ID here - it won't autmatically be written to the database

                Dim code As String = r.Item("code")
                If Not i_unit_code.ContainsKey(code) Then
                    ' u = New clsUnit(CInt(r.Item("ID")), code, iq.Translations(CInt(r.Item("fk_translation_key_name"))), r.Item("Symbol").ToString())
                    u = New clsUnit(CInt(r.Item("ID")), Trim$(r.Item("Code").ToString()), iq.Translations(CInt(r.Item("fk_translation_key_name"))), r.Item("Symbol").ToString(), CInt(r.Item("FK_Measure_ID")))
                Else
                    errorMessages.Add(code & " is duplicated in the Units table")
                End If

            End While
        End If
        r.Close()

        Return "Loaded " & iq.Units.Count & " units"

    End Function

    Private Function LoadLanguages(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String
        r = da.DBExecuteReader(con, "Select distinct id,code,localname,rtl,live,active from [language]")
        Dim l As clsLanguage
        Dim code As String
        If r.HasRows Then
            While r.Read
                code = Trim$(r.Item("code").ToString())
                'The act of creating the language - adds it to the master list (see the clsLanguage constructor)
                'becuase we're specifying the ID here - it won't autmatically be written to the database
                If Not iq.i_language_Code.ContainsKey(code) Then
                    l = New clsLanguage(CInt(r.Item("ID")), code, r.Item("Localname").ToString(), CBool(r.Item("rtl")), CBool(r.Item("live")), CBool(r.Item("active")))
                End If
            End While
        End If
        r.Close()
        If Not iq.i_language_Code.ContainsKey("KY") Then
            l = New clsLanguage("KY", "UI Key", False, True, True)

        End If
        KYlanguage = iq.i_language_Code("KY")
        Return "Loaded " & iq.Languages.Count & " languages " & "<br/>"

    End Function

    Private Function LoadExcludes(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        'we have to load the excludes *after* we'v loaded all the branches as we need to walk the descendants

        r = da.DBExecuteReader(con, "SELECT [id],FK_Branch_ID_Having,FK_Branch_ID_Excludes,reason  FROM [exclude]")
        Dim ex As clsExclude

        If r.HasRows Then
            While r.Read
                ex = New clsExclude(CInt(r.Item("id")), iq.Branches(CInt(r.Item("fk_branch_id_having"))), iq.Branches(CInt(r.Item("fk_branch_id_excludes"))), r.Item("reason").ToString())
            End While
        End If
        r.Close()

        Return "Loaded " & Excludes.Count & " Excludes."

    End Function
    Private Function LoadValidationsInclusions(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String
        ValidationInclusions = New Dictionary(Of Integer, clsValidationInclusion)()
        r = da.DBExecuteReader(con, "SELECT [id],MajorSlotType,MinorSlotType,InclusionType FROM [validationinclusion]")

        If r.HasRows Then
            While r.Read
                Dim ex = New clsValidationInclusion(CInt(r.Item("id")), r.Item("MajorSlotType"), If(IsDBNull(r.Item("MinorSlotType")), Nothing, r.Item("MinorSlotType")), [Enum].Parse(GetType(enumInclusionType), r.Item("inclusionType").ToString))
            End While

        End If
        r.Close()

        Return "Loaded " & Excludes.Count & " Excludes."
    End Function

    Private Function LoadSpecialBranches(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader, errormessages As List(Of String)) As String

        'CPQ branch
        r = da.DBExecuteReader(con, "SELECT [id],FK_Branch_ID,Code FROM SpecialBranch")

        If r.HasRows Then
            While r.Read
                iq.i_SpecialBranches.Add(r("code"), iq.Branches(CInt(r("fk_branch_id"))))
            End While
        End If
        Return "Special Branches Loaded"

    End Function


    Private Function LoadBranches(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader, errormessages As List(Of String)) As String

        'read ALL the branches in before attempting to create the structure (by grafting)

        Dim sql$ = "SELECT [id],[FK_branch_id_parent],[fk_product_id],[fk_translation_key],[picture],[fk_translation_key_collective],[fk_translation_key_collectiveSingular],fk_screen_id_matrix,[order],hidden,locked,rca  "
        sql$ &= "FROM [branch] WHERE deleted = 0 ORDER BY id"

        ' Dim sql$  'Only load branches with live products
        ' sql$ = "SELECT b.[id],[FK_branch_id_parent],[fk_product_id],[fk_translation_key],[picture],[fk_translation_key_collective],[fk_translation_key_collectiveSingular],"
        ' sql$ &= "fk_screen_id_matrix, [order], hidden, locked, rca "
        ' sql$ &= "FROM [branch] b left join product p on b.fk_product_id=p.id WHERE b.deleted=0 and p.deleted = 0 and p.active=1 and p.publish=1 ORDER BY b.id"

        r = da.DBExecuteReader(con, sql$)
        Dim b As clsBranch
        Dim product As clsProduct
        Dim parent As clsBranch = Nothing
        Dim parentBranchId As Integer

        Dim sw As New StreamWriter("c:\temp\badbranches.txt")

        Dim toDel As New List(Of clsBranch)
        Dim toUpdate As New HashSet(Of Integer) 'this is for remapping/deduping

        Dim mps As Integer
        Dim rmps As Integer = 0
        Dim neps As Integer = 0

        Dim orphaned As Integer = 0
        If r.HasRows Then
            While r.Read

                product = Nothing
                If Not IsDBNull(r.Item("fk_product_id")) Then
                    Dim pid As Integer = r.Item("fk_product_id")

                    If iq.Products.ContainsKey(pid) Then 'this product has been removed/deleted
                        product = iq.Products(pid)
                    Else
                        If REMAPS.ContainsKey(pid) Then 'do we have a rempap for this (duplicate) prodcut ..
                            product = iq.REMAPS(pid)
                            rmps += 1
                            toUpdate.Add(r.Item("id")) 'add the BRANCH to a list of branches we will need to update in the db becuase their product has been rempapped (once we've set their parent)
                            ' sw.WriteLine("Branch " & r.Item("id") & " " & iq.Translations(r.Item("fk_translation_key")).text(English) & " references non existent (or deleted) product " & pid)

                        Else
                            'oh dear - really broken (foreign key to a completely non existen product (but RI should never have allowed this ?)
                            '    Beep()
                            neps += 1
                        End If
                    End If
                End If

                Dim matrix As clsScreen = Nothing
                If Not IsDBNull(r.Item("fk_screen_id_matrix")) Then
                    matrix = iq.Screens(CInt(r.Item("fk_screen_id_matrix")))
                End If

                'we don't set the parents yet - we need them ALL in first
                b = New clsBranch(CInt(r.Item("ID")), product, Nothing, iq.Translations(CInt(r.Item("fk_translation_key"))), r.Item("picture").ToString(), _
                                  iq.Translations(CInt(r.Item("fk_translation_key_collective"))), _
                                  iq.Translations(CInt(r.Item("fk_Translation_key_collectiveSingular"))), matrix, CInt(r.Item("order")), CBool(r.Item("hidden")), CBool(r.Item("locked")), CStr(r.Item("rca")))

            End While
            r = da.DBExecuteReader(con, sql$)
            While r.Read
                If IsDBNull(r.Item("fk_branch_id_parent")) Then

                    parent = Nothing ' There are also some branches with a null parent - these are 'floaters' and (generally) only appear in the tree as grafts
                ElseIf CInt(r.Item("id")) = CInt(r.Item("fk_branch_id_parent")) Then

                    parent = RootBranch  'any branch which is it's own parent - is added directly under the root level - this is a bit of a catchall/saftey net
                Else
                    If iq.Branches.ContainsKey(CInt(r.Item("fk_branch_id_parent"))) Then  'THIS SHOULD NOT BE HERE - something is wrong - some parents are coming after their children
                        parent = iq.Branches(CInt(r.Item("fk_branch_id_parent")))
                    Else
                        orphaned += 1
                        'Dim exm As String = "- Detail should be here."
                        'Try
                        '    exm = iq.Translations(r.Item("fk_translation_key")).text(English) & "(" & r.Item("ID").ToString & ")"
                        '    exm &= " as it's parent " & r.Item("fk_branch_id_parent").ToString & " was not (yet) present"
                        'Catch ex2 As Exception

                        'End Try

                        'Dim ex As Exception = New Exception("Could not load branch " & exm)
                        'Throw ex
                    End If
                End If

                Dim branch As clsBranch = iq.Branches(CInt(r.Item("id")))
                branch.SetParent(parent)
                If branch Is parent And branch.ID <> 1 Then Stop 'a branch cannot be its own parent


                'there are 6,000 plus of these need sorting out
                '     If branch.HasSiblingWithSameProduct Then todel.Add(branch)


            End While
        End If

        '  Debug.Print(neps)

        sw.Close()

        r.Close()


        'update the FK_Product_id based on the mappings to fix any duplicate
        Dim erromessages As New List(Of String)
        For Each bid In toUpdate
            iq.Branches(bid).Update(errormessages)
        Next



        Dim graftinfo As String
        graftinfo = LoadGrafts(con, errormessages)

        Dim pruneInfo As String
        pruneInfo = loadPrunes(con, errormessages)

        Dim l$
        l$ = "Loaded " & iq.Branches.Count & " branches, " & graftinfo & "," & pruneInfo & " " & neps & " missing/deleted products. " & rmps & " products are duped (and being remapped)"

        Debug.Print(todel.Count)


        'Dim sw As StreamWriter = New StreamWriter("c:\temp\dupeProds.txt")
        'Dim qtys As Integer = 0
        'For Each branch In todel
        '    sw.WriteLine(branch.ID & " " & branch.EnglishName)
        '    If branch.Product IsNot Nothing Then
        '        sw.WriteLine(branch.Product.SKU())
        '    End If

        '    For Each q In branch.Quantities
        '        qtys += 1
        '    Next

        'Next

        'sw.Close()


        Return l$

    End Function
    Public Function loadPrunes(con As SqlClient.SqlConnection, errormessages As List(Of String)) As String


        'Prune paths should be distinct - this is 'self healing' code to fix up mess in UAT/Production and dev databases (without having to run scripts all over the place)

        Dim distinctPaths As New HashSet(Of String)
        Dim toDel As New HashSet(Of String)

        Dim Prunes As Integer = 0
        Dim r As SqlClient.SqlDataReader
        r = da.DBExecuteReader(con, "SELECT id,Path,fk_channel_id,source,created FROM Prune")

        Dim aPrune As clsPrune
        If r.HasRows Then
            While r.Read
                'use iq.channel(.Item("fk_channel_id"))) here to impliment scoped prunes
                If distinctPaths.Contains(r.Item("path")) Then
                    'duplicate prune (store the Prunes ID for deletion
                    toDel.Add(r.Item("id").ToString)
                Else
                    If Not iq.Branches.ContainsKey(Split(r.Item("Path"), ".").Last) Then
                        '       toDel.Add(r.Item("id").ToString)  'prune of a non-existent branch
                    Else
                        distinctPaths.Add(r.Item("path"))
                        aPrune = New clsPrune(CInt(r.Item("ID")), r.Item("path").ToString(), New NullableInt(), r.Item("source").ToString(), CDate(r.Item("created")))
                        'iq.Branches(Split(r.Item("path"), ".").Last).Prunes.Add(r.Item("path"))
                        Prunes += 1
                    End If
                End If
            End While
        End If
        r.Close()

        If toDel.Count Then

            Dim toskip As Integer = 0
            Dim chunk As Integer = 1000
            Do
                Dim l = From j In toDel.Skip(toskip).Take(chunk)
                If Not l.Any Then Exit Do
                Dim sql$ = "DELETE FROM PRUNE WHERE ID IN(" & Join(l.ToArray, ",") & ")"

                LongSQL(sql)
                toskip += 1000
            Loop

        End If

        Return "Loaded " & Prunes & " Prunes, (Deleted " & toDel.Count & ")"


    End Function



    Public Function LoadGrafts(con As SqlClient.SqlConnection, ByRef errorMessages As List(Of String)) As String

        Dim r As SqlClient.SqlDataReader
        Dim source As clsBranch
        Dim Target As clsBranch

        'This is run tiem 'fixup' code - could be removed once we establish what is creating the dupes !
        Dim dupes As Integer = 0
        Dim todel As New HashSet(Of String)

        'load the grafts - parts of the tree are re-used within itself - for example Operating Systems appear under *many* servers
        Dim grafts As Integer = 0

        r = da.DBExecuteReader(con, "SELECT id,fk_branch_id_source, fk_branch_id_target,path FROM graft")
        Dim path$
        If r.HasRows Then
            While r.Read
                'here's where the magic happens

                Dim sourceid As Integer = CInt(r.Item("fk_branch_id_source"))
                If iq.Branches.ContainsKey(sourceid) Then   'Soft deleted branches are not loaded - so will not be 'there 'to graft

                    source = iq.Branches(sourceid)
                    If iq.Branches.ContainsKey(CInt(r.Item("fk_branch_id_target"))) Then  'the target may have been soft deleted
                        Target = iq.Branches(CInt(r.Item("fk_branch_id_target")))
                        path$ = CStr(r.Item("Path")) '                    '*some* (very few) grafts have a path - ie. they are only active at one specific point in the tree (CPUs are a case in point)


                        If path$ <> "" Then
                            source.GraftedOnAt.Add(path$)
                        End If

                        If Not source.AllParents.ContainsKey(Target.ID) Then source.AllParents.Add(Target.ID, Target)

                        If Not Target.childBranches.ContainsKey(source.ID) Then

                            Target.childBranches.Add(source.ID, source)  'note - this does NOT set the branches parent - branches are grafted in many places and so do not have one parent - their parent property is used only by the editor within a single graft
                            Target.HasGrafts = True
                            grafts += 1
                            'Else
                            '    'Beep() 'Duplicate graft
                            '    errorMessages.Add("Graft " & r.Item("ID").ToString & " is a duplicate")
                            '    dupes += 1
                            '    todel.Add(r.Item("id"))

                        End If
                    End If
                End If
            End While
        End If
        r.Close()

        'If todel.Count Then

        '    Dim toskip As Integer = 0
        '    Dim chunk As Integer = 1000
        '    Do
        '        Dim l = From j In todel.Skip(toskip).Take(chunk)
        '        If Not l.Any Then Exit Do
        '        Dim sql$ = "DELETE FROM graft WHERE ID IN(" & Join(l.ToArray, ",") & ")"

        '        LongSQL(sql)
        '        toskip += 1000
        '    Loop

        'End If

        Return "Loaded " & grafts & " grafts (Deleted " & todel.Count & ")"

    End Function
    Private Function LoadSlots(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "Select [id],path,fk_branch_id,fk_slottype_id,numslots,fk_translation_key_notes,slotnum,requiredfill,advisedfill from [slot] where deleted =0")

        'Some duplicate slots have crept in (JIT carepacks - but not just that) - this is an on-the-fly fix (which can be removed in due course)
        Dim todel As List(Of String) = New List(Of String)

        Dim s As clsSlot
        Dim slots As Integer

        Dim ms As Integer = 0

        If r.HasRows Then
            While r.Read
                'the act of making the slot adds it to its branch (so we don't need to)
                Dim NOTES As clsTranslation = Nothing
                If r.Item("FK_TRANSLATION_KEY_NOTES") Is DBNull.Value Then
                    NOTES = Nothing
                Else
                    NOTES = iq.Translations(CInt(r.Item("FK_TRANSLATION_KEY_NOTES")))
                End If

                Dim branchid As Integer = CInt(r.Item("fk_branch_id"))
                Dim path$ = r.Item("path").ToString
                Dim slotid As Integer = CInt(r.Item("id"))

                'Heal' some malformed path (duplicate last segments)
                Dim skip As Boolean = False
                If path <> "" Then
                    Dim bits() As String = Split(path, ".")
                    If UBound(bits) > 2 AndAlso bits(UBound(bits)) = bits(UBound(bits) - 1) Then
                        todel.Add(slotid)
                        skip = True
                        ms += 1
                    End If

                    If CInt(bits.Last) <> branchid Then
                        todel.Add(slotid)
                    End If
                End If

                If Not skip Then
                    If iq.Branches.ContainsKey(branchid) Then  'soft deleted branches are not loaded - so we can't attach the slots
                        Dim stid As Integer = CInt(r.Item("fk_slottype_id"))
                        Dim slottype As clsSlotType = iq.SlotTypes(stid)
                        s = New clsSlot(slotid, slottype, iq.Branches(branchid), path, CInt(r.Item("numslots")), _
                        NOTES, New NullableInt(r.Item("slotnum")), CInt(r.Item("requiredFill")), CInt(r.Item("advisedFill")))

                        If s.ID = -1 Then  'part of the on-the-fly dupes fix (see alos Ctor above)
                            todel.Add(slotid)
                        Else
                            slots += 1
                        End If
                    End If
                End If
            End While
        End If
        r.Close()

        'dupes fix (note the dupes aren't actually loaded - and they won't exist next time)
        If todel.Count > 0 Then

            Dim toskip As Integer = 0
            Dim chunk As Integer = 1000
            Do
                Dim l = From j In todel.Skip(toskip).Take(chunk)
                If Not l.Any Then Exit Do
                Dim sql$ = "DELETE FROM SLOT WHERE ID IN(" & Join(l.ToArray, ",") & ")"

                LongSQL(sql)
                toskip += 1000
            Loop
        End If
        Debug.Print(ms)
        Return "Loaded " & slots & " Slot Give/Takes - (deleted " & todel.Count & ")"

    End Function
    Private Function LoadSlotTypes(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        'Slottypes include PCI (full and half length slots, drive bays, fan bays and anything else with finite capacity that can affect the availability of options

        Dim sql$
        sql$ = "SELECT id,fk_translation_key,fk_translation_key_short,majorcode,minorCode,EnforceMinorCode "
        sql$ &= "FROM slottype order by id"

        r = da.DBExecuteReader(con, sql$)

        Dim Count As Integer = 0

        Dim aSlotType As clsSlotType

        Dim todel As New List(Of String)

        Dim mappings As New Dictionary(Of Integer, Integer) 'DeDupe -remap to fix.7c data corrupted by the incremental import

        If r.HasRows Then
            While r.Read
                If Not IsDBNull(r("fk_translation_key_short")) Then
                    Dim a = 9
                End If

                Dim tl As clsTranslation = iq.Translations(CInt(r.Item("fk_translation_key")))
                Dim tls As clsTranslation = Nothing
                If r.Item("fk_translation_key_short") IsNot DBNull.Value Then
                    Dim ti As Integer = r.Item("fk_translation_key_short")
                    tls = iq.Translations(ti)
                End If

                Dim mjc As String = r.Item("majorcode")
                Dim mnc As String = r.Item("minorcode")

                If iq.i_slotType_Code.ContainsKey(mjc) Then
                    If iq.i_slotType_Code(mjc).ContainsKey(mnc) Then
                        Dim mst As clsSlotType = iq.i_slotType_Code(mjc)(mnc) 'master slot type (the one we're keeping!)
                        'this is a dupe
                        todel.Add(r.Item("id"))
                        mappings.Add(r.Item("id").ToString, mst.ID)
                    End If
                End If

                aSlotType = New clsSlotType(CInt(r.Item("id")), mjc, mnc, tl, tls, CBool(r.Item("EnforceMinorCode")))
                Count += 1
            End While
        End If
        r.Close()


        'sweep/remap the alt slot types

        Dim sqls As New List(Of String)
        r = da.DBExecuteReader(con, "select ID,FK_Slottype_ID,fk_slottype_id_alternative from AltSlotType")
        While r.Read
            If mappings.ContainsKey(r.Item("fk_slotType_id")) Or mappings.ContainsKey(r.Item("fk_slottype_id_alternative")) Then

                Dim m1 As Integer = r.Item("fk_slottype_id")
                Dim m2 As Integer = r.Item("fk_slottype_id_alternative")
                If mappings.ContainsKey(m1) Then m1 = mappings(m1)
                If mappings.ContainsKey(m2) Then m2 = mappings(m2)


                sqls.Add("Update altSlotType set fk_slotType_id=" & _
                    m1 & ",fk_slottype_id_alternative=" & m2 & " where id = " & r.Item("id"))
            End If
        End While
        r.Close()

        For Each u In sqls
            da.DBExecuteReader(con, u)
        Next

        '/sweep


        If todel.Count Then
            Dim toskip As Integer = 0
            Dim chunk As Integer = 1000
            Do
                Dim ll = From j In todel.Skip(toskip).Take(chunk)
                If Not ll.Any Then Exit Do

                sql$ = "Delete from slottype WHERE [id] IN(" & Join(ll.ToArray, ",") & ")"

                LongSQL(sql)
                toskip += 1000
            Loop

        End If


        'read in the alternative slot types (and their relative priorties) for each primary slot type

        sql$ = "SELECT fk_slottype_id as [primary],fk_slottype_id_alternative as alt,priority "
        sql$ &= "FROM altSlotType"

        Dim fallBacks As Integer = 0
        r = da.DBExecuteReader(con, sql$)

        If r.HasRows Then
            While r.Read
                iq.SlotTypes(CInt(r.Item("primary"))).Fallback.Add(CInt(r.Item("priority")), iq.SlotTypes(CInt(r.Item("alt"))))
                fallBacks += 1
            End While
        End If
        r.Close()




        Return "Loaded " & Count & " SlotTypes and " & fallBacks & " fallbacks" & " (deleted " & todel.Count & ")"

    End Function
    Private Function LoadProductTypes(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader, errormessages As List(Of String)) As String

        'ProductTypes were OptTypes before - but now include SVR,DTO,NBK,SWD etc... they are what margins are based on - with Sectors 

        Dim sql$
        sql$ = "SELECT id,fk_translation_key_text,code,[order] FROM ProductType"

        r = da.DBExecuteReader(con, sql$)

        Dim Count As Integer = 0
        Dim aProductType As clsProductType

        If r.HasRows Then
            While r.Read
                If i_ProductType_Code.ContainsKey(r.Item("code")) Then
                    errormessages.Add("Product type code " & r.Item("code") & " is duplicated !")
                Else
                    aProductType = New clsProductType(CInt(r.Item("id")), r.Item("code").ToString, _
                                                      iq.Translations(CInt(r.Item("fk_translation_key_text"))), CShort(r.Item("order")))
                    Count += 1
                End If
            End While
        End If
        r.Close()

        Return "Loaded " & Count & " Product Types (formerly OptTypes)"

    End Function

    Private Function LoadQuantities(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        Dim sql$
        sql$ = "SELECT q.id,[path], fk_region_id,fk_branch_id, preinstalled,minIncrement,preferredIncrement,foc FROM quantity q"
        sql$ &= " INNER JOIN Branch b ON FK_Branch_ID = b.id inner join Product p on b.FK_Product_ID = p.id	"
        sql$ &= " WHERE q.deleted = 0 AND p.deleted = 0"

        r = da.DBExecuteReader(con, sql$)

        Dim Count As Integer = 0

        Dim branch As clsBranch
        Dim aQuantity As clsQuantity
        Dim qid As Integer
        Dim numPreinstalled As Integer
        Dim prefincr As Integer

        Dim h As New HashSet(Of String) 'On the fly fix for duplication created by the incremental import
        Dim toDel As New List(Of String) 'ids of the the quantities to delete

        If r.HasRows Then
            While r.Read

                qid = CInt(r.Item("ID"))

                Dim branchID As Integer = CInt(r.Item("fk_branch_id"))
                If iq.Branches.ContainsKey(branchID) Then 'some branches are soft deleted (and not loaded)
                    branch = iq.Branches(branchID)
                    numPreinstalled = CInt(r.Item("preInstalled"))
                    Dim path$ = r.Item("path")
                    Dim foc As Boolean = CBool(r.Item("foc"))
                    Dim rgn As clsRegion = iq.Regions(CInt(r.Item("fk_region_id")))
                    Dim minIncr = CInt(r.Item("MinIncrement"))
                    prefincr = CInt(r.Item("PreferredIncrement"))

                    aQuantity = New clsQuantity(qid, rgn, r.Item("Path").ToString(), branch, numPreinstalled, minIncr, prefincr, foc)

                    'Remove line above - and reinstate below for 'self healing' quanitites
                    'Dim ck$ = branch.ID & "_" & path$ & "_" & rgn.Code & "_" & foc.ToString & "_" & minIncr

                    'If Not h.Contains(ck$) Then
                    '    'creating the quantity - adds it to the branch - the branch is may (have already been) grafted (in many places) - where the quantity limits will apply if there is a scope match (by Path)

                    '    If branch.Product IsNot Nothing AndAlso ("tap,swd".Contains(branch.Product.ProductType.Code.ToLower) And numPreinstalled = 1) Then
                    '        'dodgy fix for preisntalled tape drives (that shoould never have been)
                    '        'remove!
                    '        toDel.Add(qid.ToString)
                    '    Else
                    '        aQuantity = New clsQuantity(qid, rgn, r.Item("Path").ToString(), branch, numPreinstalled, minIncr, prefincr, foc)
                    '        h.Add(ck$)
                    '    End If
                    'Else
                    '    toDel.Add(qid.ToString)
                    'End If

                    Count += 1
                End If
            End While
        End If
        r.Close()


        If toDel.Count Then

            Dim toskip As Integer = 0
            Dim chunk As Integer = 1000
            Do
                Dim l = From j In toDel.Skip(toskip).Take(chunk)
                If Not l.Any Then Exit Do
                sql$ = "DELETE FROM quantity WHERE ID IN(" & Join(l.ToArray, ",") & ")"

                LongSQL(sql)
                toskip += 1000
            Loop

        End If

        Return "Loaded " & Count & " Quantity limits, (Deleted " & toDel.Count & ")"

    End Function

    ''' <summary>Loads the quantity records (AutoAdds, Min Increment and Preferred increments) for the specified regions</summary>

    'Public Function LoadQuantities(regions As List(Of clsRegion)) As String

    '    Dim con As SqlClient.SqlConnection = da.OpenDatabase()
    '    Dim r As SqlClient.SqlDataReader

    '    For Each rgn In regions.ToList
    '        If rgn.quantitiesLoaded Then regions.Remove(rgn) 'remove those regions that have already been loaded from the list to get
    '    Next

    '    If regions.Count = 0 Then
    '        Return "All required quantites already loaded"
    '    Else
    '        Dim regionIDs As List(Of String) = New List(Of String)
    '        For Each rgn In regions : regionIDs.Add(rgn.ID.ToString) : rgn.quantitiesLoaded = True : Next

    '        Dim sql$
    '        sql$ = "SELECT id,[path], fk_region_id,fk_branch_id, preinstalled,minIncrement,preferredIncrement,foc "
    '        sql$ &= "FROM quantity WHERE fk_region_id in (" & Join(regionIDs.ToArray, ",") & ")"

    '        r = da.DBExecuteReader(con, sql$)

    '        Dim Count As Integer = 0

    '        Dim branch As clsBranch
    '        Dim aQuantity As clsQuantity
    '        Dim rid As Integer
    '        Dim numPreinstalled As Integer

    '        If r.HasRows Then
    '            While r.Read
    '                branch = iq.Branches(CInt(r.Item("fk_branch_id")))
    '                rid = CInt(r.Item("ID"))
    '                numPreinstalled = CInt(r.Item("preInstalled"))

    '                'creating the quantity - adds it to the branch - the branch is may (have already been) grafted (in many places) - where the quantity limits will apply if there is a scope match (by Path)

    '                'aQuantity = New clsQuantity(id, iq.Regions(r.Item("fk_region_id")), r.Item("Path"), branch, Nothing, numPreinstalled, r.Item("MinIncrement"), r.Item("PreferredIncrement"), r.Item("foc"))
    '                aQuantity = New clsQuantity(rid, iq.Regions(CInt(r.Item("fk_region_id"))), r.Item("Path").ToString(), branch, numPreinstalled, CInt(r.Item("MinIncrement")), CInt(r.Item("PreferredIncrement")), CBool(r.Item("foc")))


    '                Count += 1
    '            End While
    '        End If
    '        r.Close()
    '        con.Close()
    '        Return "Loaded " & Count & " Quantity limits"
    '    End If


    'End Function

    'Private Function LoadCountries(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

    '    r = da.dbexecuteReader(con, "SELECT Id,code,fk_translation_key_countryname,culture from Country")

    '    Dim aCountry As clsCountry
    '    Dim count As Integer = 0
    '    If r.HasRows Then
    '        While r.Read
    '            aCountry = New clsCountry(r.Item("id"), r.Item("code"), iq.Translations(r.Item("fk_translation_key_countryname")), r.Item("culture"))
    '            count += 1
    '        End While
    '    End If

    '    r.Close()

    '    Return "Loaded " & count & " countries"

    'End Function


    Private Function LoadRegions(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        'This is not as clean as other loads - as we do it in two passes to ensure all regions are present before we set up the heirarchy..
        'we do this becuase the regions parents don't necessarily appear before their children

        Dim pass As Integer = 0
        Dim count As Integer = 0


        For pass = 1 To 2  'we have to load all regions in, before we can set up their parents (becuase if a regions's parent is not yet loaded - bad things happen

            r = da.DBExecuteReader(con, "SELECT Id,[fk_region_id_parent],code,[fk_translation_key_name],isCountry,fk_culture_id,isPlaceholder,Notes,[FK_Region_ID_Geo] FROM [Region]")

            Dim aRegion As clsRegion

            Dim parent As clsRegion
            If r.HasRows Then
                While r.Read
                    If pass = 1 Then
                        'all parents are set to nothing on the first pass
                        Dim culture As clsCulture
                        Dim tid As Integer = CInt(r.Item("fk_translation_key_name"))
                        If iq.Cultures.ContainsKey(CInt(r.Item("fk_culture_id"))) Then
                            culture = iq.Cultures(CInt(r.Item("fk_culture_id")))
                        Else
                            culture = iq.i_culture_code("en-us")
                        End If


                        Dim tlrn As clsTranslation
                        If iq.Translations.ContainsKey(CInt(r.Item("fk_translation_key_name"))) Then
                            tlrn = iq.Translations(CInt(r.Item("fk_translation_key_name")))
                        Else
                            tlrn = iq.Translations.Values.First  'TEMPORARY
                        End If


                        aRegion = New clsRegion(CInt(r.Item("id")), Nothing, r.Item("code").ToString(), _
                                                tlrn, _
                                                CBool(r.Item("isCountry")), culture, CBool(r.Item("isPlaceholder")), r.Item("notes").ToString, r.Item("FK_Region_ID_Geo").ToString())
                        count += 1
                    Else
                        'set parents (on the seconds pass)
                        If Not IsDBNull(r.Item("fk_region_id_parent")) Then 'only set the parent on the second pass
                            parent = iq.Regions(CInt(r.Item("fk_region_id_parent")))
                            parent.Children.Add(CInt(r.Item("ID")), iq.Regions(CInt(r.Item("ID")))) 'add to the parent children
                        Else
                            parent = Nothing
                        End If
                        iq.Regions(CInt(r.Item("id"))).Parent = parent
                    End If
                End While
            End If

            r.Close()
        Next pass


        Return "Loaded " & count & " regions"

    End Function

    Private Function LoadCurrencies(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT Id,code,code_HP,symbol,rate,fk_translation_key_name,fk_translation_key_notes from currency")

        Dim acurrency As clsCurrency
        Dim count As Integer = 0
        Dim notes As clsTranslation

        If r.HasRows Then
            While r.Read
                If IsDBNull(r.Item("fk_translation_key_notes")) Then
                    notes = Nothing
                Else
                    notes = iq.Translations(CInt(r.Item("fk_translation_key_notes")))
                End If

                acurrency = New clsCurrency(CInt(r.Item("id")), r.Item("code").ToString(), r.Item("Code_HP").ToString(), _
                                            iq.Translations(CInt(r.Item("fk_translation_key_name"))), _
                                            r.Item("symbol").ToString(), CSng(r.Item("rate")), notes) ' r.Item("culture"))

                ' Add to the DefaultCurrencies collection (used when setting up new Channels etc.)
                DefaultCurrencies.Add(acurrency.ID, acurrency)

                count += 1
            End While
        End If

        r.Close()

        Return "Loaded " & count & " currencies"

    End Function


    Private Function LoadCultures(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT Id,culturecode,[Name] from culture where visible = 1")

        Dim aCulture As clsCulture
        Dim count As Integer = 0


        If r.HasRows Then
            While r.Read

                aCulture = New clsCulture(CInt(r.Item("id")), r.Item("culturecode").ToString(), r.Item("name").ToString())
                count += 1
            End While
        End If

        r.Close()

        Return "Loaded " & count & " cultures"

    End Function
    Private Function LoadStates(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT Id,[group],Code,fk_translation_key,[order],colour from [State]")

        Dim count As Integer

        Dim aState As clsState

        If r.HasRows Then
            While r.Read
                'creates a new user adds them to their channel
                aState = New clsState(CInt(r.Item("id")), r.Item("group").ToString(), Trim$(r.Item("code").ToString()), _
                                      iq.Translations(CInt(r.Item("fk_translation_key"))), CInt(r.Item("order")), r.Item("colour").ToString())
                count += 1
            End While
        End If
        r.Close()

        Return "Loaded " & count & "  States"


    End Function
    Private Function LoadRoles(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key from [Role]")

        Dim count As Integer

        Dim aRole As clsRole

        If r.HasRows Then
            While r.Read
                aRole = New clsRole(CInt(r.Item("id")), r.Item("code").ToString(), iq.Translations(CInt(r.Item("fk_translation_key"))))
                count += 1


            End While
        End If
        r.Close()


        Return "Loaded " & count & " Roles"

    End Function
    Private Function LoadRights(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT Id,Code,fk_translation_key from [Right]")

        Dim count As Integer

        Dim aRight As clsRight

        If r.HasRows Then
            While r.Read
                aRight = New clsRight(CInt(r.Item("id")), r.Item("code").ToString(), iq.Translations(CInt(r.Item("fk_translation_key"))))
                count += 1


                If Not i_right_Code.ContainsKey(aRight.Code) Then
                    i_right_Code.Add(aRight.Code, aRight)
                End If
            End While
        End If
        r.Close()

        Return "Loaded " & count & " rights"

    End Function
    Private Function LoadRoleRights(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT roleright.Id,role.code as rolecode,[right].code as rightcode from [RoleRight] inner join role on role.id=fk_role_id inner join [right] on [right].id=fk_right_id")

        Dim count As Integer

        Dim right As clsRight

        If r.HasRows Then
            While r.Read
                right = iq.i_right_Code(r.Item("rightcode"))
                iq.i_role_Code(r.Item("rolecode")).Rights.Add(right.ID, right)
                iq.i_role_Code(r.Item("rolecode")).i_right_code.Add(right.Code, right)
                count += 1
            End While
        End If
        r.Close()

        Return "Loaded " & count & " Role-Rights "

    End Function

    Private Function LoadThreads(con As SqlClient.SqlConnection, rdr As SqlClient.SqlDataReader) As String

        Dim count As Integer = 0

        Dim sql$
        sql$ = "SELECT id,FK_user_id_createdby,FK_user_id_AssignedTo,fk_thread_id_parent,fk_state_id_priority,fk_state_id_status,[hours],title, text,fk_event_id,created,updated,internal from [Thread] order by ID"

        rdr = da.DBExecuteReader(con, sql$)

        Dim aThread As clsThread

        Dim CreatedBy As clsUser
        Dim AssignedTo As clsUser
        Dim Parent As clsThread = Nothing
        Dim State As clsState


        If rdr.HasRows Then
            While rdr.Read

                CreatedBy = iq.Users(CInt(rdr.Item("fk_user_id_createdby")))
                AssignedTo = iq.Users(CInt(rdr.Item("fk_user_id_assignedto")))

                If IsDBNull(rdr.Item("fk_thread_id_parent")) Then
                    Parent = Nothing 'this is the (one and only) top level/root thread
                ElseIf CInt(rdr.Item("fk_thread_id_parent")) = CInt(rdr.Item("id")) Then
                    'Stop
                Else
                    Parent = iq.Threads(CInt(rdr.Item("fk_thread_id_parent")))
                End If
                State = iq.States(CInt(rdr.Item("fk_state_id_status")))

                '    If IsDBNull(rdr.Item("fk_event_id")) Then
                ' EventLog = Nothing
                'Else
                'EventLog = iq.Events(CInt(rdr.Item("fk_event_id")))
                'End If

                Dim priority As clsState
                priority = iq.States(CInt(rdr.Item("fk_state_id_priority")))
                aThread = New clsThread(CInt(rdr.Item("id")), CreatedBy, AssignedTo, Parent, priority, State, CSng(rdr.Item("hours")), _
                                        rdr.Item("title").ToString(), New nullableString(rdr.Item("text")), CDate(rdr.Item("Created")), _
                                        CDate(rdr.Item("Updated")), CBool(rdr.Item("internal")))
                count += 1

            End While
        End If

        rdr.Close()

        Return "Loaded " & count & " support threads <br/>"

    End Function
    Private Function LoadValidations(con As SqlClient.SqlConnection, rdr As SqlClient.SqlDataReader) As String

        Validations.Clear()

        Dim sql$
        sql$ = "SELECT ID,description,regex,violation FROM Validation"

        rdr = da.DBExecuteReader(con, sql$)

        Dim v As clsValidation
        Dim count As Integer = 0
        If rdr.HasRows Then

            While rdr.Read
                v = New clsValidation(CInt(rdr.Item("ID")), rdr.Item("description").ToString(), rdr.Item("regex").ToString(), rdr.Item("violation").ToString())
                count += 1
            End While
        End If

        rdr.Close()

        Return "loaded " & count & " Validations.<br/>"


    End Function

    Public Function LoadScreens(con As SqlClient.SqlConnection, rdr As SqlClient.SqlDataReader) As String
        Try
            i_screens_title.Clear()
            i_screens_code.Clear()
            Screens.Clear()
            Fields.Clear()

            Dim sql$
            ' sql$ = "SELECT ID,code,Title,[object],[dictionary] FROM [Screen]"
            sql$ = "SELECT ID,code,Title,[object] FROM [Screen] order by code"  'NOTE the FIRST screen (by code) of each Object type is used for editing

            rdr = da.DBExecuteReader(con, sql$)

            Dim CountScreens As Integer = 0

            Dim screen As clsScreen
            If rdr.HasRows Then
                While rdr.Read
                    screen = New clsScreen(CInt(rdr.Item("id")), rdr.Item("code").ToString(), rdr.Item("object").ToString(), rdr.Item("Title").ToString())
                    CountScreens += 1
                End While
            End If
            rdr.Close()

            sql$ = "SELECT ID,FK_Field_Id,FK_Region_ID FROM [FIELDRestriction]"
            Dim restrictions = da.FilledDataTable(con, sql)


            'load fields
            'sql$ = "SELECT ID,fk_screen_id,property,label,helptext,fk_validation_id,lookupof,fk_inputtype_id,[order],fk_screen_id_embed,width,length,defaultvalue,visible FROM [FIELD]"
            sql$ = "SELECT ID,fk_screen_id,property,fk_translation_key_label,helptext,fk_validation_id,lookupof,fk_inputtype_id,[order],width,height,length,defaultvalue,visibleList,VisiblePage,defaultFilter,defaultSort,[priority],fk_translation_key_widgetGroup,widgetUI,CanUserSelect,visibleSquare,FK_Field_ID_Linked,Grows,DefaultFilterValues,FilterVisible,[HMC_MutualExclusivity],[InvertFilterOrder] FROM [FIELD]"

            rdr = da.DBExecuteReader(con, sql$)

            Dim afield As clsField

            Dim validation As clsValidation = Nothing

            Dim countFields As Integer = 0
            If rdr.HasRows Then
                While rdr.Read
                    With rdr

                        '                    If IsDBNull(rdr.Item("fk_screen_id_embed")) Then
                        'embedscreen = Nothing
                        ' Else
                        'embedscreen = Screens(.Item("fk_screen_id_embed"))
                        ' End If

                        If IsDBNull(rdr.Item("fk_validation_id")) Then
                            validation = Nothing
                        Else
                            If Validations.ContainsKey(CInt(.Item("fk_validation_id"))) Then
                                validation = Validations(CInt(.Item("fk_validation_id")))
                            Else
                                Beep() 'need to fix this -  saving nullable foriend keys (field.validations is one example)

                            End If
                        End If

                        '     If rdr.Item("lookupof") <> "" Then Stop
                        Dim inputType As clsInputType
                        inputType = InputTypes(CInt(.Item("fk_inputtype_id")))

                        Dim wtl As clsTranslation = Nothing
                        If Not IsDBNull(rdr.Item("fk_translation_key_widgetGroup")) Then
                            If iq.Translations.ContainsKey(CInt(rdr.Item("fk_translation_key_widgetGroup"))) Then
                                wtl = iq.Translations(CInt(rdr.Item("fk_translation_key_widgetGroup")))
                            Else
                                wtl = iq.AddTranslation("No Group", English, "", 0, Nothing, 0, False)
                            End If

                        End If

                        Dim ltl As clsTranslation = Nothing
                        If Not IsDBNull(rdr.Item("fk_translation_key_label")) Then
                            If iq.Translations.ContainsKey(CInt(rdr.Item("fk_translation_key_label"))) Then
                                ltl = iq.Translations(CInt(rdr.Item("fk_translation_key_label")))
                            Else
                                ltl = iq.AddTranslation("", English, "", 0, Nothing, 0, False)
                            End If
                        End If

                        afield = New clsField(Screens(CInt(.Item("fk_screen_id"))), CInt(rdr.Item("ID")), .Item("property").ToString(), .Item("LookUpOf").ToString(), _
                        ltl, .Item("helptext").ToString(), validation, inputType, CInt(.Item("length")), CInt(.Item("order")), CInt(.Item("width")), _
                        CSng(.Item("height")), .Item("defaultvalue").ToString(), CBool(.Item("visibleList")), CBool(.Item("visiblePage")), CBool(.Item("visibleSquare")), .Item("defaultfilter").ToString(), _
                        .Item("defaultsort").ToString(), CInt(.Item("Priority")), wtl, rdr.Item("widgetUI").ToString(), CBool(.Item("CanUserSelect")), If(rdr("FK_Field_ID_Linked") Is DBNull.Value, New Integer?, CInt(rdr("FK_Field_ID_Linked"))), Boolean.Parse(rdr("Grows")), If(rdr("DefaultFilterValues") Is DBNull.Value, Nothing, rdr("DefaultFilterValues").ToString()), CBool(rdr("FilterVisible")), CBool(rdr("HMC_MutualExclusivity")), CBool(rdr("InvertFilterOrder")))
                        countFields += 1

                        If LCase(rdr.Item("property").ToString()) = "auditroot" Then
                            Screens(CInt(.Item("fk_screen_id"))).Auditable = True 'This Class has a root property - grouping instances (and a Current Property)
                        End If

                        For Each fr In restrictions.Select("FK_Field_Id=" & afield.ID)
                            afield.ValidRegions.Add(fr("FK_REGION_ID"), iq.Regions(fr("FK_REGION_ID")))
                        Next

                    End With
                End While
            End If

            rdr.Close()

            Return "loaded " & CountScreens & " screens containing a total of " & countFields & " fields.<br/>"
        Catch ex As Exception
            ErrorLog.Add(ex)
            Return ex.Message
        End Try

    End Function

    Private Function loadInputTypes(con As SqlClient.SqlConnection, r As SqlClient.SqlDataReader) As String

        iq.InputTypes.Clear()
        iq.i_inputType_code.Clear()

        Dim sql$
        sql$ = "SELECT ID,code,name FROM [InputType]"

        r = da.DBExecuteReader(con, sql$)

        Dim count As Integer = 0
        Dim inputType As clsInputType
        If r.HasRows Then
            While r.Read
                inputType = New clsInputType(CInt(r.Item("ID")), r.Item("code").ToString(), r.Item("name").ToString())
                count += 1
            End While
        End If
        r.Close()

        Return "loaded " & count & "input types<br/>"

    End Function


    Private Function LoadMargins(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        r = da.DBExecuteReader(con, "SELECT Id,fk_channel_id_buyer,fk_channel_id_seller,factor,fk_sector_id,sampledsku from [margin]")

        Dim Seller As clsChannel
        Dim Buyer As clsChannel
        Dim sector As clsSector
        'Dim productType As clsProductType
        Dim count As Integer

        Dim aMargin As clsMargin

        If r.HasRows Then
            While r.Read
                'creates a new price adding it to the master price list
                Buyer = iq.Channels(CInt(r.Item("fk_channel_id_buyer")))
                Seller = iq.Channels(CInt(r.Item("fk_channel_id_seller")))
                sector = iq.Sectors(CInt(r.Item("fk_sector_id")))
                ' productType = iq.ProductTypes(CInt(r.Item("fk_producttype_id")))

                'the act of creating a margin adds it to the sellers dictionary of buyers margins
                aMargin = New clsMargin(CInt(r.Item("ID")), Seller, Buyer, CSng(r.Item("factor")), "", sector, r.Item("sampledSKU").ToString())
                count += 1
            End While
        End If
        r.Close()

        Return "Loaded " & count & " margins"

    End Function
    Private Function LoadTeams(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String


        Dim sql$
        sql$ = "SELECT id,name,fk_channel_id from TEAM"

        r = da.DBExecuteReader(con, sql$)

        Dim count As Integer
        Dim aTeam As clsTeam

        If r.HasRows Then
            While r.Read
                aTeam = New clsTeam(CInt(r.Item("id")), iq.Channels(CInt(r.Item("fk_channel_id"))), r.Item("Name").ToString())
                count += 1
            End While
        End If
        r.Close()

        Return "Loaded " & count & " teams"


    End Function
    Private Function LoadUsers(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader, ByRef errormessages As List(Of String)) As String

        Dim sql$
        Try

            sql$ = "SELECT id,fk_channel_id,email,realname,tel1,tel2,Disabled FROM [User]"

            r = da.DBExecuteReader(con, sql$)

            Dim count As Integer
            Dim aUser As clsUser
            Dim currentuser As String = ""
            If r.HasRows Then
                While r.Read
                    Dim cid As Integer = CInt(r.Item("fk_channel_id"))
                    Dim uid As Integer = CInt(r.Item("id"))
                    Dim email As String = r.Item("email").ToString()
                    Dim disabled As Boolean = r.Item("Disabled")

                    If iq.Channels.ContainsKey(cid) Then
                        aUser = New clsUser(uid, iq.Channels(cid), email, r.Item("realname").ToString(), New nullableString(r.Item("tel1")), New nullableString(r.Item("tel2")), disabled)

                        currentuser = r.Item("id").ToString()
                        count += 1
                    Else
                        errormessages.Add("User " & uid & " " & email & " has an invalid channel " & cid)
                    End If

                End While
            End If
            r.Close()

            Return "Loaded " & count & " users"
        Catch ex As Exception
            ErrorLog.Add(ex)
            Return ex.Message
        End Try


    End Function

    Private Function LoadUserMessages(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        iq.UserMessages = New Dictionary(Of String, List(Of clsMessage))

        Try
            r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_key_Name, FK_Channel_ID, ValidFrom, ValidTo, Enabled from [message] order by ValidFrom")
            If r.HasRows Then
                While r.Read()

                    Dim code As String = r.Item("Code")


                    If Not iq.UserMessages.ContainsKey(code) Then
                        iq.UserMessages.Add(code, New List(Of clsMessage))
                    End If

                    Dim tkey As Integer = r.Item("FK_Translation_key_Name")
                    If Translations.ContainsKey(tkey) Then
                        Dim translation As clsTranslation = Me.Translations(tkey)
                        iq.UserMessages(code).Add(New clsMessage(r.Item("ID"), code, translation, r.Item("ValidFrom"), r.Item("ValidTo"), r.Item("Enabled"), r.Item("FK_Channel_ID")))
                    Else
                        ErrorLog.Add(New Exception("Missing translation key in LoadUserMessages fk_translations_key_name was " & tkey))
                    End If
                End While
            End If

        Catch ex As Exception

            ErrorLog.Add(ex)
            Return ex.Message

        End Try

        Return "Loaded user messages"

    End Function

    Private Function LoadAddresses(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        iq.Addresses = New Dictionary(Of String, clsAddress)

        Try
            r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_Key_Address from [Address]")
            If r.HasRows Then
                While r.Read()

                    Dim code As String = r.Item("Code")
                    Dim translation As clsTranslation = Me.Translations(r.Item("FK_Translation_Key_Address"))
                    iq.Addresses.Add(code, New clsAddress(r.Item("ID"), code, translation))

                End While
            End If

        Catch ex As Exception

            ErrorLog.Add(ex)
            Return ex.Message

        End Try

        Return "Loaded addresses"

    End Function

    Private Function LoadLegal(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        iq.Legal = New Dictionary(Of String, clsLegal)

        Try
            r = da.DBExecuteReader(con, "select ID, Code, FK_Translation_Key_Name from [Legal]")
            If r.HasRows Then
                While r.Read()

                    Dim code As String = r.Item("Code")
                    Dim translation As clsTranslation = Me.Translations(r.Item("FK_Translation_Key_Name"))
                    Dim legal = New clsLegal(r.Item("ID"), code, translation)
                    If Not iq.Legal.ContainsKey(code) Then
                        iq.Legal.Add(code, legal)
                    End If

                End While
            End If

        Catch ex As Exception

            ErrorLog.Add(ex)
            Return ex.Message

        End Try

        Return "Loaded legal"

    End Function

    Private Function LoadResources(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        iq.ResourceCategories = New Dictionary(Of Integer, clsResourceCategory)

        ' Resource Categories
        Try
            r = da.DBExecuteReader(con, "select [ID], [Name], [FK_Translation_Key_Name], [Order] from [ResourceCategory]")
            If r.HasRows Then
                While r.Read()

                    Dim tkey As Integer = r.Item("FK_Translation_Key_Name")
                    If Translations.ContainsKey(tkey) Then
                        iq.ResourceCategories.Add(r.Item("ID"), New clsResourceCategory(r.Item("ID"), r.Item("Name"), Translations(tkey), r.Item("Order")))
                    Else
                        ErrorLog.Add(New Exception("Translation missing in LoadResources - key was" & tkey))
                    End If

                End While
            End If

        Catch ex As Exception

            ErrorLog.Add(ex)
            Return ex.Message

        End Try

        ' Resources Files
        Try
            r = da.DBExecuteReader(con, "select [ID], [Description], [FK_Resource_Category_ID], [Type], [Code], [FK_Translation_Key_Title], [FK_Region_ID], [FK_Language_ID], [FK_SellerChannel_ID], [mfrCode], [Order], [Embed] from [Resource]")
            If r.HasRows Then
                While r.Read()


                    Dim tkey As Integer = r.Item("FK_Translation_Key_Title")
                    If Translations.ContainsKey(tkey) Then
                        Dim translation As clsTranslation = Me.Translations(tkey)
                        Dim categoryId As Integer = r.Item("FK_Resource_Category_ID")
                        Dim region As clsRegion = Nothing
                        Dim language As clsLanguage = Nothing
                        Dim sellerChannel As clsChannel = Nothing
                        Dim mfrCode As String = Nothing

                        If Not IsDBNull(r.Item("FK_Region_ID")) Then
                            Dim regionId As Integer = r.Item("FK_Region_ID")
                            If iq.Regions.ContainsKey(regionId) Then
                                region = iq.Regions(regionId)
                            End If
                        End If

                        If Not IsDBNull(r.Item("FK_Language_ID")) Then
                            Dim languageId As Integer = r.Item("FK_Language_ID")
                            If iq.Languages.ContainsKey(languageId) Then
                                language = iq.Languages(languageId)
                            End If
                        End If

                        If Not IsDBNull(r.Item("FK_SellerChannel_ID")) Then
                            Dim sellerChannelId As Integer = r.Item("FK_SellerChannel_ID")
                            If iq.Channels.ContainsKey(sellerChannelId) Then
                                sellerChannel = iq.Channels(sellerChannelId)
                            End If
                        End If

                        If Not IsDBNull(r.Item("mfrCode")) Then
                            mfrCode = r.Item("MfrCode")
                        End If

                        Dim resource As New clsResource(r.Item("ID"), r.Item("Description"), r.Item("Type"), r.Item("Code"), translation, region, language, sellerChannel, mfrCode, r.Item("Order"), categoryId, r.Item("Embed"))

                        If iq.ResourceCategories.ContainsKey(categoryId) Then
                            If iq.ResourceCategories(categoryId).Resources Is Nothing Then
                                iq.ResourceCategories(categoryId).Resources = New List(Of clsResource)
                            End If
                            iq.ResourceCategories(categoryId).Resources.Add(resource)
                        End If
                    Else
                        ErrorLog.Add(New Exception("Missing translation in LoadResources " & tkey))
                    End If

                End While
            End If

        Catch ex As Exception

            ErrorLog.Add(ex)
            Return ex.Message

        End Try

        Return "Loaded resources"

    End Function

    Private Function LoadROKAttributes(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        iq.ROKAttributes = New Dictionary(Of String, List(Of clsROKAttribute))

        Try
            r = da.DBExecuteReader(con, "select ID, OS_Code, FK_Attribute_Code, FK_Translation_Key_Name from ROKAttributes order by OS_Code")
            If r.HasRows Then
                While r.Read()

                    Dim osCode As String = r.Item("OS_Code")

                    If Not iq.ROKAttributes.ContainsKey(osCode) Then
                        iq.ROKAttributes.Add(osCode, New List(Of clsROKAttribute))
                    End If

                    Dim translation As clsTranslation = Me.Translations(r.Item("FK_Translation_key_Name"))
                    Dim rok As clsROKAttribute = New clsROKAttribute(r.Item("ID"), osCode, r.Item("FK_Attribute_Code"), translation)

                    iq.ROKAttributes(osCode).Add(rok)

                End While
            End If

        Catch ex As Exception

            ErrorLog.Add(ex)
            Return ex.Message

        End Try

        Return "Loaded ROK attributes"

    End Function

    Private Function LoadAccounts(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        Dim sql$

        sql$ = "SELECT fk_account_id,role.code as rolecode FROM [AccountRoles] inner join role on role.id=fk_role_id"
        r = da.DBExecuteReader(con, sql$)
        Dim dr As Dictionary(Of Integer, List(Of clsRole)) = New Dictionary(Of Integer, List(Of clsRole))()
        If r.HasRows Then
            While r.Read
                If Not dr.ContainsKey(r.Item("fk_account_id")) Then
                    dr.Add(r.Item("fk_account_id"), New List(Of clsRole))
                End If
                dr(r.Item("fk_account_id")).Add(iq.i_role_Code(r.Item("rolecode")))

            End While
        End If

        'sql$ = "SELECT id,fk_user_id,password,fk_role_id,fk_team_id,fk_language_id,fk_buyergroup_id,fk_currency_id FROM [Account]"
        sql$ = "SELECT id,fk_user_id,password,fk_team_id,fk_language_id,fk_channel_id_buyer,fk_currency_id,fk_channel_id_seller,priceBand,fk_culture_id,mfrCode FROM [Account]"
        r = da.DBExecuteReader(con, sql$)

        Dim count As Integer
        Dim anAccount As clsAccount

        'Dim buyerGroup As clsBuyerGroup
        Dim Buyer As clsChannel
        Dim User As clsUser
        Dim Team As clsTeam
        Dim Language As clsLanguage
        Dim currency As clsCurrency
        Dim Role As clsRole
        Dim seller As clsChannel
        Dim culture As clsCulture

        If r.HasRows Then
            While r.Read
                Try
                    If count = 174130 Then
                        Dim a = 6
                    End If
                    'buyergroup = iq.BuyerGroups(r.Item("fk_buyergroup_id"))
                    Buyer = iq.Channels(CInt(r.Item("fk_channel_id_buyer")))
                    seller = iq.Channels(CInt(r.Item("fk_channel_id_seller")))
                    If iq.Cultures.ContainsKey(CInt(r.Item("fk_culture_id"))) Then
                        culture = iq.Cultures(CInt(r.Item("fk_culture_id")))
                    Else
                        culture = iq.i_culture_code("en-us")
                    End If

                    User = iq.Users(CInt(r.Item("fk_user_id")))

                    ' If r.Item("FK_USER_ID") = 65538 Then Stop

                    If IsDBNull(r.Item("fk_team_id")) Then
                        Team = Nothing
                    Else
                        Dim tid As Integer
                        tid = CInt(r.Item("fk_team_id"))
                        Team = iq.Teams(tid)
                    End If

                    Language = iq.Languages(CInt(r.Item("fk_language_id")))
                    currency = iq.Currencies(CInt(r.Item("fk_currency_id")))

                    anAccount = New clsAccount(CInt(r.Item("id")), User, r.Item("password").ToString(), Buyer, If(dr.ContainsKey(CInt(r.Item("id"))), dr(CInt(r.Item("id"))).ToArray, {}), Team, Language, currency, seller, iq.getPriceBand(r.Item("priceBand").ToString()), culture, r.Item("mfrcode"))
                    count += 1
                Catch ex As Exception
                    Dim a = ex.Message
                End Try

            End While
        End If
        r.Close()

        Return "Loaded " & count & " accounts"

    End Function


    Private Function LoadChannels(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        'Channels include Distributors, Resellers, Manufacturers - they are basically sets of users (for whom a set of pricing may exist)
        'in the case of resellers - chanel reall just represent the buying company - for who the dist's set up margin records

        r = da.DBExecuteReader(con, "SELECT Id,fk_channel_id_parent,Name,BusinessName,Address,code,fk_region_id,webtoken,pic1,pic2,url,fk_channel_id_cloneof,priceConfig,treePath,Focus,margintype,marginMin,marginMax,SchemeOverride,Legal,FK_Currency_ID_Default,universal,orderemail,basketmode,basketurl From Channel order by id")

        Dim achannel As clsChannel
        Dim coId As Integer 'clone of' ID

        ' child > parent - Used to construct the heirarchy of channels once they're all loaded
        '(as the order of channels cannot be guaranteed and the parent may not be present at the point we instance the child)
        Dim dicClones As Dictionary(Of Integer, Integer) = New Dictionary(Of Integer, Integer)
        Dim dicChildren As Dictionary(Of Integer, Integer) = New Dictionary(Of Integer, Integer)

        If r.HasRows Then
            While r.Read
                'creates a new channel and adds it to the 'master' list
                Dim code$
                If IsDBNull(r.Item("code")) Then code$ = "" Else code$ = r.Item("code").ToString()

                Dim id As Integer = CInt(r.Item("id"))

                If code <> "" And iq.i_channel_code.ContainsKey(code) Then
                    Dim l$ = iq.i_channel_code.ContainsKey(code) & " is duplicated"
                Else

                    'Dim webtoken As Guid = r.Item("webtoken")
                    Dim webtoken As String = r.Item("webtoken").ToString()
                    Dim WT$ = webtoken.ToString

                    coId = CInt(r.Item("fk_channel_id_cloneof"))
                    If IsDBNull(coId) Then
                        Stop
                    ElseIf CInt(r.Item("id")) = coId Then 'this channel is a clone of ITSELF

                    Else 'it's a clone of something OTHER than itself
                        dicClones.Add(id, coId) 'I' am the 'child' 
                    End If

                    dicChildren.Add(id, r.Item("fk_channel_id_cloneof")) 'dictionary of child >parent

                    Dim parent As clsChannel
                    If r.Item("fk_channel_id_parent") Is DBNull.Value Then
                        parent = Nothing
                    Else
                        If iq.Channels.ContainsKey(CInt(r.Item("fk_channel_id_parent"))) Then
                            parent = iq.Channels(CInt(r.Item("fk_channel_id_parent")))
                        Else
                            'this channel has been orphaned
                            parent = Nothing
                        End If
                    End If

                    'we can't set the 'iscloneof' or 'parent' unti we've loaded all channels (becuase the parent may not be there yet)
                    achannel = New clsChannel(CInt(r.Item("id")), Nothing, r.Item("Name").ToString(), r.Item("BusinessName").ToString(), Nothing, r.Item("Address").ToString(), _
                                          code$, iq.Regions(CInt(r.Item("fk_region_id"))), WT, New nullableString(r.Item("pic1")), New nullableString(r.Item("pic2")), _
                                          New nullableString(r.Item("url")), CInt(r.Item("priceConfig")), r.Item("TreePath").ToString(), r.Item("Focus").ToString(), _
                                          r.Item("marginMin"), r.Item("Marginmax"), r.Item("MarginType"), r.Item("schemeOverride"), r.Item("legal"), If(IsDBNull(r.Item("FK_Currency_ID_Default")), Nothing, iq.Currencies(CInt(r.Item("FK_Currency_ID_Default")))), CBool(r.Item("universal")), r.Item("orderemail"), r.Item("basketMode"), r.Item("BasketURL"))

                End If
            End While
        End If
        r.Close()

        'now (all channels are loaded) setup the clones
        'clones are 'copies' of other channels which have the same portfolio (set of products) and pricing which is some factor of one of the price bands ('A','B','','internal','external' etc) according to the [Margins]
        For Each childID In dicClones.Keys
            iq.Channels(childID).IsCloneOf = iq.Channels(dicClones(childID))
        Next

        'And similarly the parent-child relationship ('food chain') of channels (which is a completely different thing from clones and is really only used for organisation/reporting)
        '(we can't guaranteed the order of chanels and children may be defined before their parents )
        For Each childID In dicChildren.Keys
            iq.Channels(childID).Parent = iq.Channels(dicChildren(childID))
        Next

        Dim domainTable As DataTable = LoadDomains(con)
        If domainTable IsNot Nothing Then
            For Each row As DataRow In domainTable.Rows
                Dim cid As Integer = CInt(row("fk_channel_id"))
                If iq.Channels.ContainsKey(cid) Then  'A check that *should be unnecessary (but i broke some data)
                    Dim chnl As clsChannel = iq.Channels(cid)
                    chnl.Domains.Add(row("domain").ToString())
                End If
            Next
        End If

        Return "Loaded " & iq.Channels.Count & " channels"

    End Function
    Private Function LoadBuyerGroups(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String

        'BuyerGroups allow prices to be defined for more than one buyer
        'you can have a buyergroup of 1 channel - if pricing really is different for every customer

        r = da.DBExecuteReader(con, "SELECT id,name,fk_channel_id_owner,ownersid FROM BuyerGroup")

        Dim aBuyerGroup As clsBuyerGroup
        If r.HasRows Then
            While r.Read
                'creates a new BuyerGroup and adds it to the 'master' list                
                aBuyerGroup = New clsBuyerGroup(CInt(r.Item("id")), r.Item("Name").ToString(), iq.Channels(CInt(r.Item("fK_channel_id_owner"))), r.Item("ownersID").ToString())
            End While
        End If
        r.Close()

        Dim placed As Integer = 0
        r = da.DBExecuteReader(con, "SELECT fk_channel_id,fk_buyerGroup_id FROM ChannelGroup")
        If r.HasRows Then
            While r.Read
                iq.BuyerGroups(CInt(r.Item("fk_buyerGroup_id"))).Channels.Add(iq.Channels(CInt(r.Item("fk_channel_id"))))
                placed += 1
            End While
        End If
        r.Close()

        Return "Loaded " & iq.BuyerGroups.Count & " buyer groups, and placed " & placed & " Channels in them"

    End Function
    Private Function LoadAttributes(ByVal con As SqlClient.SqlConnection, ByVal r As SqlClient.SqlDataReader) As String
        Try
            r = da.DBExecuteReader(con, "Select [id],[code],[order],fk_translation_key_name from [Attribute]")

            Dim a As clsAttribute
            If r.HasRows Then
                While r.Read
                    'becuase we're specifying the ID here - it won't autmatically be written to the database
                    If Not iq.i_attribute_code.ContainsKey(Trim$(r.Item("code").ToString())) Then
                        Dim tl As clsTranslation = iq.Translations(CInt(r.Item("fk_translation_key_name")))
                        a = New clsAttribute(CInt(r.Item("ID")), Trim$(r.Item("Code").ToString()), tl, CInt(r.Item("ORDER")))
                    Else
                        Logit("attribute " & r.Item("code").ToString() & " is duplicated")
                    End If
                End While
            End If
            r.Close()

            Return "Loaded " & iq.Attributes.Count & " attributes"
        Catch ex As Exception
            Return "Failed: " & ex.Message
        End Try
    End Function
    Public Function AddTranslation(ByVal Text As String, ByVal language As clsLanguage, group As String, order As Integer, writecache As DataTable, ByRef nextKey As Integer, dupe As Boolean) As clsTranslation

        'Set dupe if you (deliberately) want to create an addtional copy of the translation.. perhaps for isolated editing)

        '    Exit Function

        If language IsNot English Then Stop

        If writecache IsNot Nothing And nextKey = 0 Then Stop
        If writecache Is Nothing And nextKey <> 0 Then Stop

        Dim existing As clsTranslation = EnglishIndex(Text, group)
        If existing IsNot Nothing And dupe = False Then
            Return existing
        Else
            'note, instancing a new clstranslation adds it to the englishindex
            Return New clsTranslation(language, Text, group, order, writecache, nextKey)
        End If

    End Function
    Private Function LoadDomains(ByVal con As SqlClient.SqlConnection) As DataTable

        Dim query As String = "Select FK_Channel_ID,Domain from Domain"
        Try

            Dim cmd As SqlCommand = New SqlCommand(query, con)
            If con.State = ConnectionState.Closed Then con.Open()
            Dim dr As SqlDataReader = cmd.ExecuteReader()
            Dim dt As DataTable = New DataTable()
            dt.Load(dr)
            Return dt

        Catch ex As Exception
            Return Nothing
            'Finally
            '    con.Close()

        End Try

    End Function

    Private Function LoadPromos(con As SqlConnection, r As SqlDataReader) As String
        Dim command As SqlCommand = New SqlCommand()
        command.CommandType = CommandType.Text
        command.CommandText = "Select * from promo inner join promoregion pr on promo.id=pr.fk_promo_id inner join promosystemtype pst on pst.fk_promo_id=promo.id"
        command.Connection = con

        iq.Promos = New Dictionary(Of Integer, clsPromo)()
        iq.i_PromoRegions = New Dictionary(Of clsRegion, List(Of clsPromo))()
        iq.i_PromoSystemTypes = New Dictionary(Of clsPromo, List(Of String))()
        r = command.ExecuteReader()
        Dim promo As clsPromo
        If r.HasRows Then
            While r.Read
                If iq.Promos.ContainsKey(CInt(r("ID"))) Then
                    iq.Promos(CInt(r("ID"))).AddRegion(iq.Regions(CInt(r("FK_Region_ID"))))
                    iq.Promos(CInt(r("ID"))).AddSystemType(r("SystemType"))
                Else
                    promo = New clsPromo(CInt(r("ID")), r("Code").ToString(), iq.Translations(CInt(r("FK_Translation_Key_Description"))), iq.Regions(CInt(r("FK_Region_ID"))), r("FieldProperty_Filter").ToString, r("FieldProperty_Value").ToString, r("SystemType").ToString)
                End If
            End While
        End If
        r.Close()

        command.CommandType = CommandType.Text
        command.CommandText = "Select * from promoproduct"
        command.Connection = con
        r = command.ExecuteReader()
        If r.HasRows Then
            While r.Read
                Dim pid As Integer = r("FK_Product_Id")
                Dim product As clsProduct = Nothing
                If iq.Products.ContainsKey(pid) Then
                    product = iq.Products(pid)
                Else
                    If REMAPS.ContainsKey(pid) Then
                        product = REMAPS(pid)
                    End If

                End If

                If product IsNot Nothing Then
                    If product.Promos IsNot Nothing AndAlso Not product.Promos.ContainsKey(iq.Promos(CInt(r("FK_Promo_Id"))).Code) Then
                        product.Promos.Add(iq.Promos(CInt(r("FK_Promo_Id"))).Code, New List(Of clsRegion))
                        product.Promos(iq.Promos(CInt(r("FK_Promo_Id"))).Code).Add(iq.Regions(r("FK_REGION_ID")))
                    End If
                End If

            End While
        End If

        r.Close()
        Return "Loaded " & iq.Promos.Count & " Promos"

    End Function

    Private Function LoadCampaigns(con As SqlConnection, r As SqlDataReader) As String
        Dim command As SqlCommand = New SqlCommand()
        command.CommandType = CommandType.Text
        command.CommandText = "Select * from Campaign"
        command.Connection = con

        r = command.ExecuteReader()
        Dim campaign As clsCampaign
        If r.HasRows Then
            While r.Read
                campaign = New clsCampaign(CInt(r(0)), r(1).ToString(), iq.Channels(CInt(r(2))), iq.Regions(CInt(r(3))), iq.Channels(CInt(r(4))), _
                                           iq.Channels(CInt(r(5))), CDate(r(6)), CDate(r(7)))
                iq.Campaigns.Add(campaign.ID, campaign)
            End While
        End If
        r.Close()

        Return "Loaded " & iq.Campaigns.Count & " Campaigns"
    End Function


    Private Function LoadAdverts(con As SqlConnection, r As SqlDataReader) As String
        Dim command As SqlCommand = New SqlCommand()
        Dim query As String = "Select  [ID],[FK_Campaign_ID],[Name],[ImageURL],[URL],[Type],[BasketProductBelowAbsent],[BasketProductBelowPresent],"
        query &= "[FK_ProdType_Present],[FK_ProdType_Absent],[FK_SlotType_ID],[FillThresholdPercent],[imageWide],[SlotTypeCode],[FK_Region_Id_Present],[FK_Region_Id_Absent], [visible], [mfrCode] from Advert"
        command.CommandType = CommandType.Text
        command.CommandText = query
        command.Connection = con
        Dim adCount As Integer
        r = command.ExecuteReader()
        Dim advert As clsAdvert
        If r.HasRows Then
            While r.Read
                advert = New clsAdvert(CInt(r("ID")), iq.Campaigns(CInt(r("FK_Campaign_ID"))), r("Name").ToString(), r("ImageURL").ToString(), r("Url").ToString(), CShort(r("Type")), _
                                       r("BasketProductBelowAbsent").ToString(), r("BasketProductBelowPresent").ToString(), iq.ProductTypes(CInt(r("FK_ProdType_Present"))), _
                                       iq.ProductTypes(CInt(r("FK_ProdType_Absent"))), iq.SlotTypes(CInt(r("FK_SlotType_ID"))), CInt(r("FillThresholdPercent")), CBool(r("imageWide")),
                                      If(IsDBNull(r("SlotTypeCode")), Nothing, r("SlotTypeCode")),
                                      If(IsDBNull(r("FK_Region_Id_Present")), Nothing, iq.Regions(CInt(r("FK_Region_Id_Present")))),
                                      If(IsDBNull(r("FK_Region_Id_Absent")), Nothing, iq.Regions(CInt(r("FK_Region_Id_Absent")))),
                                      CBool(r("visible")), CStr(r("mfrCode")))

                adCount = adCount + 1
            End While
        End If
        r.Close()

        Return "Loaded " & adCount & " Adverts"
    End Function

    Private Function LoadScreenOverrides(con As SqlConnection, r As SqlDataReader) As String
        Dim command As SqlCommand = New SqlCommand()
        Dim query As String = "Select  [FK_Account_ID],[FK_Screen_ID],Path,[FK_Field_ID],[ForceVisibilityTo],[ForceOrderTo],[ForceWidthTo],[ForceSortTo],[ForceFilterTo],[FK_DisplayUnit_ID] from AccountScreenOverride"
        command.CommandType = CommandType.Text
        command.CommandText = query
        command.Connection = con
        Dim screenoverrideCount As Integer
        r = command.ExecuteReader()
        Dim override As clsScreenOverride
        If r.HasRows Then
            While r.Read
                override = New clsScreenOverride(CInt(r("FK_Account_ID")), CInt(r("FK_Screen_ID")), r("Path").ToString(), CInt(r("FK_Field_ID")), If(r("ForceVisibilityTo") Is DBNull.Value, Nothing, (r("ForceVisibilityTo"))), If(r("ForceOrderTo") Is DBNull.Value, New Integer?, CInt(r("ForceOrderTo"))), If(r("ForceWidthTo") Is DBNull.Value, New Double?, CDbl(r("ForceWidthTo"))), r("ForceSortTo").ToString(), r("ForceFilterTo").ToString(), If(r("FK_DisplayUnit_ID") Is DBNull.Value, Nothing, iq.Units(CInt(r("FK_DisplayUnit_ID")))))
                screenoverrideCount += 1
            End While
        End If
        r.Close()

        Return "Loaded " & screenoverrideCount & " Overrides"
    End Function

    Public Function LoadConversions(con As SqlConnection, r As SqlDataReader) As String
        Dim command As SqlCommand = New SqlCommand()
        Dim query As String = "Select  FK_Units_From,FK_Units_To,Rate from Conversion"
        command.CommandType = CommandType.Text
        command.CommandText = query
        command.Connection = con
        Dim conversioncount As Integer
        r = command.ExecuteReader()
        Conversions = New Dictionary(Of Integer, Dictionary(Of Integer, Double))
        If r.HasRows Then
            While r.Read
                If Not Conversions.ContainsKey(CInt(r("FK_Units_From"))) Then
                    Conversions.Add(CInt(r("FK_Units_From")), New Dictionary(Of Integer, Double))
                End If
                Conversions(CInt(r("FK_Units_From"))).Add(CInt(r("FK_Units_To")), CDbl(r("Rate")))
                conversioncount += 1
            End While
        End If
        r.Close()

        Return "Loaded " & conversioncount & " Conversions"
    End Function

    Public Function LoadMeasures(con As SqlConnection, r As SqlDataReader) As String
        Dim command As SqlCommand = New SqlCommand()
        Dim query As String = "Select ID,MeasureName from Measure"
        command.CommandType = CommandType.Text
        command.CommandText = query
        command.Connection = con
        Dim measurecount As Integer
        r = command.ExecuteReader()
        Measures = New Dictionary(Of Integer, String)
        If r.HasRows Then
            While r.Read
                Measures.Add(CInt(r("ID")), r("MeasureName").ToString())
                measurecount += 1
            End While
        End If
        r.Close()

        Return "Loaded " & measurecount & " Measures"
    End Function

    Public Function LoadActiveUniversal(con As SqlConnection, r As SqlDataReader) As String
        Dim command As SqlCommand = New SqlCommand()
        Dim query As String = "select r.code, t.[Text] from Region r inner join Translation t on r.FK_Translation_key_name = t.[key] inner join Universal on [Name] = t.[Text] where r.IsCountry = 1 And [Enabled] = 1 order by t.[Text]"
        command.CommandType = CommandType.Text
        command.CommandText = query
        command.Connection = con
        Dim countryCount As Integer
        r = command.ExecuteReader()
        ActiveUniversalCountries = New Dictionary(Of String, String)
        If r.HasRows Then
            While r.Read
                ActiveUniversalCountries.Add(r("Code"), r("Text").ToString())
                countryCount += 1
            End While
        End If
        r.Close()

        Return "Loaded " & countryCount & " Active Universal Countries"
    End Function



    Public Shared Sub DeleteTL(tl As clsTranslation, language As clsLanguage)

        iq.Translations(tl.Key).remove(language)
        If language Is English Then
            iq.iEnglishIndex.Remove(tl.text(English) & "^" & tl.Group)
        End If

    End Sub



    Public Shared Sub IndexTL(tl As clsTranslation, language As clsLanguage)

        'longer term - we should probably never be looking anything up by 'english'
        '(and thus don't need an 'englishindex'
        'one of the main reasons we haveto do that is that part numbers are attributes
        'HPPart numbers should really only exist on the HP variant of the product (clsvariant)
        'although another pratical solution is to have a product.sku as string
        'Some attributes - such as familcodes, or compatible SKUS should possibly be untranslated text - ie clsProductAttributes should have a 'Text' *aswell* as a translation (and a numeric value)

        If Not iq.Translations.ContainsKey(tl.Key) Then iq.Translations.Add(tl.Key, tl)

        Dim ck$ = tl.text(language) & "^" & tl.Group
        If language Is English Then
            If Not iq.iEnglishIndex.ContainsKey(ck) Then
                iq.iEnglishIndex.Add(ck, tl)
            End If
        ElseIf language.Code = "KY" Then
            If Not iq.KYIndex.ContainsKey(ck) Then
                iq.KYIndex.Add(ck, tl)
            End If
        End If

    End Sub


    Public Shared Function CleanString(s As String) As String
        Dim sb As StringBuilder = New StringBuilder(s)
        Dim x As String = sb.ToString()
        Dim intx As Integer = 0
        intx = x.IndexOf("<")
        Dim inty As Integer = 0
        While intx > 0
            inty = x.IndexOf(">", intx)
            sb.Remove(intx, inty - intx + 1)
            x = sb.ToString()
            intx = x.IndexOf("<")

        End While

        Return sb.ToString()
    End Function

    Private Function LoadLocations(con As SqlConnection, r As SqlDataReader) As String
        Dim command As SqlCommand = New SqlCommand()
        Dim query As String = "SELECT [Code],[Description] FROM [Location]"
        command.CommandType = CommandType.Text
        command.CommandText = query
        command.Connection = con
        Dim locationsCount As Integer
        r = command.ExecuteReader()
        ActiveUniversalCountries = New Dictionary(Of String, String)
        If r.HasRows Then
            While r.Read
                Locations.Add(r("Code"), r("Description"))
                locationsCount += 1
            End While
        End If
        r.Close()

        Return "Loaded " & locationsCount & " Locations"
    End Function


End Class
