Imports System.Linq
Imports System.Xml
Imports System.Xml.Serialization
Imports System.Runtime.Serialization
Imports System.Threading
Imports System.ServiceModel
Imports System.ServiceModel.Description
Imports System.Globalization
Imports System.IO




'Possible states for each column of each matrix 
Public Enum enumColState As Integer

    unused   'I don't like to use 0 in enums - in case something is unitialised
    HardCollapsed   'The user has actively collapsed this colum
    SoftCollapsed    'This column has been collapsed becuase there isn't enough space and an it's of low priority
    SoftExpanded  'This column is being displayed - becuase there's enough room
    HardExpanded   'this column has been actively opened.. so we treat it as a higher priority
End Enum

Public Enum enumParadigm

    errorNotSet = 0
    AddingSystem = 1
    configuringSystem = 2
End Enum

'Quote/Basket item (system) viewtypes (sleected tabs)
Public Enum panelEnum
    System = 1
    Options = 2
    Spec = 4
    Validation = 8
    Promo = 16
End Enum


Public Enum EnumHideButton
    Up
    Down
    Both
    Neither
End Enum


Public Enum ButtonsEnum

    Tabs = 1
    Branches = 2
    Squares = 4
    Matrix = 8
    close = 16
    Auto = 32

End Enum

Public Enum EnumOpenWhich
    errorNotset
    None
    First
    All
End Enum

Public Enum ud
    united
    divided
End Enum
'Public Enum oc
'    closed
'    open
'End Enum

Public Enum EnumValidationSeverity
    notSet
    greenTick
    BlueInfo
    Question
    Exclamation
    DoesQualify 'cation
    DoesntQualify 'cation
    amberalert
    RedCross
    Upsell
End Enum

Public Enum Manufacturer
    Unknown
    HPI
    HPE
End Enum

Public Module CoreCode

    Public iicalls As Integer = 0

    Public Enum enumBt 'Branch type
        errorNotSet
        OpenSquare ' an open square renders nothing... except its children
        Square
        Branch
        Tab
        gridrow
        TROhead
        TROitem
        OpenBranch
        Hidden
        Upsell
        hYperlink
        DetailSquare
        bighyperlinK
        'branchForGrid 'A contrivance for showing options in grids, under branches - for tools/showOptions for system WITHDRAWN
        invisibLe
        helpMechoose

    End Enum


    'NOTE:-
    ' "A Module statement defines a reference type available throughout its namespace. A module (sometimes called a standard module)is similar to a class but with some important distinctions.
    ' Every module has exactly one instance and does not need to be created or assigned to a variable. Modules do not support inheritance or implement interfaces. Notice that a module is not a type in the sense that a class or structure is — you cannot declare a programming element to have the data type of a module. //
    ' A module has the same lifetime as your program. Because its members are all Shared, they also have lifetimes equal to that of the program. "

    Public BTchar As List(Of String) = New String() {"E", "C", "S", "B", "T", "G", "H", "I", "O", "X", "U", "Y", "D", "K", "L", "M"}.ToList

    Public collapsedColumnWidth As Single = 2.2 'ems was 1.6

    Public ReadOnly Property iq As clsIQ 'This IS the object model - ML if this is going to work, need to work out how to log, singleton seems the way to go though
        Get
            Return clsIQ.Instance
        End Get
    End Property

    Public Sub reloadIQ()
        clsIQ.reset()
        Dim a = iq.loadedTimestamp
    End Sub

    Public HP As clsChannel             'You guessed it - Hewlett-Packard
    Public Everyone As clsPriceBand 'clsChannel       'The target channel for list pricing
    Public HPList As clsPriceBand

    Public r_worldwide As clsRegion       'The Root of all regions (until we sell to Mars)
    Public r_RestOfWorld As clsRegion     'The parent of otherwise unassigned countries - keeps level 1 tidy
    Public r_Americas As clsRegion
    Public r_USCA As clsRegion

    Public r_EMEA As clsRegion
    Public r_GWE As clsRegion
    Public r_UKIE As clsRegion
    Public r_GB As clsRegion 'formerly UK
    Public r_IE As clsRegion
    Public r_MEMA As clsRegion
    Public r_CEE As clsRegion

    Public English As clsLanguage       'Like it says
    Public UnknownUser As clsUser       'Primarily for recording failed logins
    Public RootBranch As clsBranch      'In truth there are many Roots - but for bootstrapping we need 1
    Public CarePackRootBranch As clsBranch
    Public RootChannel As clsChannel    'Channels are loosely assembled into a tree - some channels are just placeholders - like 'Techdata'

    Public KYlanguage As clsLanguage    'used in translations
    Public Yes As clsTranslation        'used when presenting grids - (saves an awful lot of XLT'ing) - thousands of cells for hundreds of users
    Public No As clsTranslation
    Public InStock As clsTranslation
    Public OutOfStock As clsTranslation
    Public StockWebservice As ServiceHost


    Public Function NameOf(mfrsku As String) As String

        Dim product As clsProduct
        If iq.i_SKU.ContainsKey(mfrsku) Then
            product = iq.i_SKU(mfrsku)

            If product.i_Attributes_Code.ContainsKey("Name") Then
                Return product.i_Attributes_Code("Name")(0).Translation.text(s_lang)
            Else
                Return product.i_Attributes_Code("desc")(0).Translation.text(s_lang)
            End If
        Else
            Return "Not a valid SKU"
        End If

    End Function

    'Public Function UpdatePrices(buyerAccount As clsAccount) As String

    '    Static LastTimes As Dictionary(Of String, DateTime)
    '    If LastTimes Is Nothing Then LastTimes = New Dictionary(Of String, DateTime)

    '    Dim ck$
    '    ck$ = buyerAccount.SellerChannel.ID & "^" & buyerAccount.priceBand

    '    If Not LastTimes.ContainsKey(ck$) Then LastTimes.Add(ck$, DateAdd(DateInterval.Day, -1000, Now))

    '    If DateDiff(DateInterval.Minute, LastTimes(ck$), Now) > 30 Then
    '        Return Import.HostPrices(buyerAccount.SellerChannel.Code, buyerAccount.priceBand)
    '    Else
    '        Return "Cached within the last 30 minutes"
    '    End If

    'End Function

    Public Function BranchID(b As clsBranch) As String

        If b Is Nothing Then
            Return "0"
        Else
            Return Trim$(b.ID)
        End If

    End Function


    Public Function MakeLinkButton(imagefile$, tooltip As String, Url$, language As clsLanguage) As HyperLink

        Dim btn As New HyperLink
        btn.ToolTip = Xlt(tooltip, language)
        btn.ImageUrl = "/images/navigation/" & imagefile$
        btn.NavigateUrl = Url$
        btn.Attributes("target") = "_blank"

        Return btn

    End Function
    Public Function MakeRoundButton(imagefile$, tooltip As String, clickscript As String, cssClass As String, style As String, language As clsLanguage, Optional objectId As String = "") As Literal  ', Optional AbsX As Single = -1, Optional pos As String = "absolute") As Literal

        Dim lit As New Literal
        lit.Text = Replace("<img id=|" & objectId & "| class=|unpressedButton " & cssClass & "| src=|/images/navigation/" & imagefile$ & "| onclick=|" & clickscript & "| title=|" & Xlt(tooltip, language) & "| style=|" & style & "|/>", "|", Chr(34))

        Return lit

    End Function
    Public Sub wipeCachedDataView(path$, lid As UInt64)

        Dim key$, filters$, sorts$

        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)

        '        filters = iq.sesh(lid,"filters." & path$)
        Dim activeFilters As Dictionary(Of clsField, Dictionary(Of clsFilter, String)) = iq.sesh(lid, "filters." & path)
        filters$ = textRep(activeFilters)

        sorts = iq.sesh(lid, "sorts." & path$)

        key$ = path$ & "-" & filters$ & "-" & sorts$ & "-" & buyerAccount.BuyerChannel.Code & "-" & buyerAccount.SellerChannel.Code & "-branches"

        Dim cache As System.Web.Caching.Cache = System.Web.HttpContext.Current.Cache
        cache.Remove(key)

    End Sub



    Public Function Effectivematrix(path$) As clsScreen

        Effectivematrix = Nothing

        Dim seg() As String
        seg = Split(path$, ".")

        For i = UBound(seg) To 1 Step -1
            If iq.Branches(seg(i)).Matrix IsNot Nothing Then
                Return iq.Branches(seg(i)).Matrix
            End If
        Next i

        If Effectivematrix Is Nothing Then
            Return iq.Screens(719)
        End If


    End Function
    Public Sub SaveUserStates()

        Dim li = New List(Of clsUserState)
        If iq.seshDic Is Nothing Then Exit Sub
        For Each s In iq.seshDic

            Dim uState As clsUserState = New clsUserState

            With uState

                .lid = s.Key
                .root = If(s.Value.ContainsKey("Root"), s.Value("Root").ToString(), String.Empty)
                .path = If(s.Value.ContainsKey("path"), s.Value("path").ToString(), String.Empty)
                .foci = If(s.Value.ContainsKey("foci"), s.Value("foci"), Nothing)
                .QuoteID = If(s.Value.ContainsKey("QuoteID"), s.Value("QuoteID"), New Integer?)
                .showOnly = If(s.Value.ContainsKey("showOnly"), s.Value("showOnly"), 0)
                .treeCursorPath = If(s.Value.ContainsKey("treeCursorPath"), s.Value("treeCursorPath"), String.Empty)
                .branchStates = If(s.Value.ContainsKey("branchStates"), CType(s.Value("branchStates"), Dictionary(Of String, clsBranchState)).Select(Function(kvp) New KeyValuePair(Of String, clsBranchState) With {.Key = kvp.Key, .Value = kvp.Value}).ToList(), New List(Of KeyValuePair(Of String, clsBranchState)))
                .AgentAccount = If(s.Value.ContainsKey("AgentAccount"), s.Value("AgentAccount").Id, Nothing)
                .BuyerAccount = If(s.Value.ContainsKey("BuyerAccount"), s.Value("BuyerAccount").Id, Nothing)
                .Paradigm = If(s.Value.ContainsKey("Paradigm"), s.Value("Paradigm"), Nothing)

                .ScreenHeaders = If(s.Value.Keys.Contains("screenHeaders"),
                        CType(s.Value("screenHeaders"),  _
                        Dictionary(Of String, clsScreenHeader)).Select(Function(mh) New clsScreenHeaderState _
                            With {.Path = mh.Key, _
                                .Filters = If(mh.Value.Filters IsNot Nothing, mh.Value.Filters.Select(Function(fil) New KeyValuePair(Of Integer, List(Of KeyValuePair(Of clsFilter, List(Of Int64)))) With {.Key = fil.Key.ID, .Value = fil.Value.Select(Function(flt) New KeyValuePair(Of clsFilter, List(Of Int64)) With {.Key = flt.Key, .Value = flt.Value}).ToList()}).ToList(), Nothing), _
                                .QuickFiltersVisible = mh.Value.QuickFiltersVisible, _
                                .Sorts = mh.Value.sorts.Select(Function(so) New KeyValuePair(Of Integer, clsPriorityDirection) With {.Key = so.Key, .Value = New clsPriorityDirection() With {.columnid = so.Value.column.ID, .Direction = so.Value.Direction, .Priority = so.Value.Priority}}).ToList() _
                                }).ToList(), Nothing)
            End With

            uState.mopUpvalues = New List(Of KeyValuePair(Of Object, Object))  'makes the dictionary keys case insensitive))
            For Each kvp In s.Value
                If kvp.Value Is Nothing OrElse kvp.Value.GetType().IsPrimitive Then
                    uState.mopUpvalues.Add(New KeyValuePair(Of Object, Object)() With {.Key = kvp.Key, .Value = kvp.Value})
                End If
            Next

            li.Add(uState)

        Next

        Dim b As XmlSerializer = New XmlSerializer(li.GetType())
        Dim st As String
        Using mem As MemoryStream = New MemoryStream()
            Using t As TextReader = New StreamReader(mem)
                b.Serialize(mem, li)
                mem.Seek(0, SeekOrigin.Begin)
                st = t.ReadToEnd()

                dataAccess.da.DBExecutesql("INSERT INTO UserStates (datetime,hostname,states) VALUES (GetDate(),'" + Environment.MachineName + "','" + st + "')")

            End Using
        End Using
    End Sub
    <Serializable>
    Public Structure KeyValuePair(Of K, V)
        Public Property Key As K
        Public Property Value As V
    End Structure

    Public Function UserIsAdmin(lid As UInt64) As Boolean

        Return CType(iq.sesh(lid, "BuyerAccount"), clsAccount).HasRight("GLOBALADM")
    End Function
    Public Function AccountHasRight(lid As UInt64, rightcode As String) As Boolean
        If iq.SeshAlive(lid) AndAlso iq.seshDic(lid).ContainsKey("Elevated") Then Return True

        Dim ba = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)
        If ba IsNot Nothing Then Return ba.HasRight(rightcode) Else Return False
    End Function




    Public Enum enumValidationMessageType
        Validation
        Flex
        Upsell
        Specification
        UpsellHolder
    End Enum

    Public Class clsActionList

        Private ID As Integer = 0

        Private _actions As List(Of clsAction) = New List(Of clsAction)()
        Public Sub Add(Sku As String, ObjectType As ObjectType, Message As String)
            ID = ID + 1
            _actions.Add(New clsAction() With {.ID = ID, .ObjectType = ObjectType, .SKU = Sku, .Message = Message})
        End Sub
        Public Sub Add(Sku As String, ActionType As ActionType, ObjectType As ObjectType, From As clsProductAttribute, AttributTo As clsProductAttribute)
            ID = ID + 1
            _actions.Add(New clsAction() With {.ID = ID, .ActionType = ActionType, .SKU = Sku, .AttributeFrom = From, .ObjectType = ObjectType, .AttributeTo = AttributTo})
        End Sub
        Public Sub Add(Sku As String, ActionType As ActionType, ObjectType As ObjectType, Branch As clsBranch)
            ID = ID + 1
            _actions.Add(New clsAction() With {.ID = ID, .ActionType = ActionType, .SKU = Sku, .ObjectType = ObjectType, .SourceBranch = Branch})
        End Sub
        Public Sub Add(Sku As String, ActionType As ActionType, ObjectType As ObjectType, Branch As clsBranch, TargetBranch As clsBranch)
            ID = ID + 1
            _actions.Add(New clsAction() With {.ID = ID, .ActionType = ActionType, .SKU = Sku, .ObjectType = ObjectType, .SourceBranch = Branch, .TargetBranch = TargetBranch})
        End Sub
        Public Sub Add(Sku As String, ActionType As ActionType, ObjectType As ObjectType, Branch As clsBranch, TargetBranchName As String)
            ID = ID + 1
            _actions.Add(New clsAction() With {.ID = ID, .ActionType = ActionType, .SKU = Sku, .ObjectType = ObjectType, .SourceBranch = Branch, .TargetBranchName = TargetBranchName})
        End Sub
        Public Sub Add(Sku As String, SysSku As String, ActionType As ActionType, ObjectType As ObjectType, quantityDetails As String)
            ID = ID + 1
            _actions.Add(New clsAction() With {.ID = ID, .ActionType = ActionType, .SKU = Sku, .ObjectType = ObjectType, .SysSKU = SysSku, .QuantityDetails = quantityDetails})
        End Sub
        Public Sub Add(Sku As String, ActionType As ActionType, ObjectType As ObjectType, TargetBranch As clsBranch, slottype As clsSlotType, path As String, quantity As Integer)
            ID = ID + 1
            _actions.Add(New clsAction() With {.ID = ID, .ActionType = ActionType, .SKU = Sku, .ObjectType = ObjectType, .Quantity = quantity, .SlotType = slottype, .Path = path, .TargetBranch = TargetBranch})
        End Sub

        Public Function IsGo(Sku As String, ActionType As ActionType, ObjectType As ObjectType, TargetBranch As clsBranch, slottype As clsSlotType, path As String, quantity As Integer) As Boolean
            Return _actions.Where(Function(ac) ac.ActionType = ActionType AndAlso ac.SKU = Sku AndAlso ac.ObjectType = ObjectType AndAlso ac.Quantity = quantity AndAlso ac.SlotType Is slottype AndAlso ac.Path = path AndAlso ac.Authorized And ac.TargetBranch Is TargetBranch).Count > 0
        End Function
        Public Function IsGo(Sku As String, SysSku As String, ActionType As ActionType, ObjectType As ObjectType, quantityDetails As String) As Boolean
            Return _actions.Where(Function(ac) ac.ActionType = ActionType AndAlso ac.SKU = Sku AndAlso ac.ObjectType = ObjectType AndAlso ac.QuantityDetails = quantityDetails AndAlso ac.SysSKU = SysSku AndAlso ac.Authorized).Count > 0
        End Function
        Public Function IsGo(Sku As String, ActionType As ActionType, ObjectType As ObjectType, Branch As clsBranch, TargetBranchName As String) As Boolean
            Return _actions.Where(Function(ac) ac.ActionType = ActionType AndAlso ac.SKU = Sku AndAlso ac.ObjectType = ObjectType AndAlso ac.SourceBranch Is Branch AndAlso ac.TargetBranchName = TargetBranchName AndAlso ac.Authorized).Count > 0
        End Function
        Public Function IsGo(Sku As String, ActionType As ActionType, ObjectType As ObjectType, Branch As clsBranch, TargetBranch As clsBranch) As Boolean
            Return _actions.Where(Function(ac) ac.SKU = Sku AndAlso ac.ActionType = ActionType AndAlso ac.ObjectType = ObjectType AndAlso ac.SourceBranch Is Branch AndAlso ac.TargetBranch Is TargetBranch AndAlso ac.Authorized).Count > 0
        End Function
        Public Function IsGo(Sku As String, ActionType As ActionType, ObjectType As ObjectType, From As clsProductAttribute, AttributeTo As clsProductAttribute) As Boolean
            Return _actions.Where(Function(ac) ac.ActionType = ActionType AndAlso ac.SKU = Sku AndAlso ac.AttributeFrom Is From AndAlso ac.ObjectType = ObjectType AndAlso ac.AttributeTo Is AttributeTo AndAlso ac.Authorized).Count > 0
        End Function

        Public Function ToList() As List(Of clsAction)
            Return _actions
        End Function

        Public Function ToClientList()
            Return _actions.Select(Function(ac)
                                       Select Case ac.ObjectType
                                           Case ObjectType.Attribute
                                               Return New With {.ID = ac.ID, .SKU = ac.SKU, .Authorized = False, .ObjectType = ac.ObjectType.ToString(), .Type = ac.ActionType.ToString(), .Col1 = If(ac.AttributeFrom Is Nothing, ac.AttributeTo, ac.AttributeFrom).Attribute.displayName(English), .Col2 = If(ac.AttributeFrom IsNot Nothing, If(ac.AttributeFrom.Translation IsNot Nothing, ac.AttributeFrom.Translation.text(English), ac.AttributeFrom.NumericValue.ToString), "None"), .Col3 = If(ac.AttributeTo IsNot Nothing, If(ac.AttributeTo.Translation IsNot Nothing, ac.AttributeTo.Translation.text(English), ac.AttributeTo.NumericValue.ToString), "None")}
                                           Case ObjectType.Branch Or ObjectType.Graft Or ObjectType.Prune
                                               Return New With {.ID = ac.ID, .SKU = ac.SKU, .Authorized = False, .ObjectType = ac.ObjectType.ToString(), .Type = ac.ActionType.ToString(), .Col1 = ac.SourceBranch.Translation.text(English), .Col2 = If(ac.TargetBranch IsNot Nothing, ac.TargetBranch.Translation.text(English), ac.TargetBranchName)}
                                           Case ObjectType.Quantity
                                               Return New With {.ID = ac.ID, .SKU = ac.SKU, .Authorized = False, .ObjectType = ac.ObjectType.ToString(), .Type = ac.ActionType.ToString(), .Col1 = ac.SysSKU, .Col2 = ac.Path, .Col3 = ac.QuantityDetails}
                                           Case ObjectType.Slot
                                               Return New With {.ID = ac.ID, .SKU = ac.SKU, .Authorized = False, .ObjectType = ac.ObjectType.ToString(), .Type = ac.ActionType.ToString(), .Col1 = ac.TargetBranch.Translation.text(English), .Col2 = ac.Path, .Col3 = ac.SlotType.MajorCode + ":" + ac.SlotType.MinorCode, .Col4 = ac.Quantity.ToString}
                                           Case ObjectType.WARNING
                                               Return New With {.ID = ac.ID, .SKU = ac.SKU, .ObjectType = ac.ObjectType, .Col1 = ac.Message}
                                           Case Else
                                               Return New With {.ID = ac.ID, .SKU = ac.SKU, .Authorized = False, .ObjectType = ac.ObjectType.ToString(), .Type = ac.ActionType.ToString()}
                                       End Select
                                   End Function
            )
        End Function

    End Class
    <DataContract>
    Public Class clsAction
        <DataMember>
        Public Property ID As Integer
        <DataMember>
        Public Property ActionType As ActionType
        <DataMember>
        Public Property SKU As String
        <DataMember>
        Public Property SysSKU As String
        <DataMember>
        Public Property ObjectType As ObjectType
        <DataMember>
        Public Property AttributeFrom As clsProductAttribute
        <DataMember>
        Public Property AttributeTo As clsProductAttribute
        <DataMember>
        Public Property SourceBranch As clsBranch
        <DataMember>
        Public Property TargetBranch As clsBranch
        <DataMember>
        Public Property TargetBranchName As String
        <DataMember>
        Public Property QuantityDetails As String
        <DataMember>
        Public Property SlotType As clsSlotType
        <DataMember>
        Public Property Quantity As Integer
        <DataMember>
        Public Property Message As String
        <DataMember>
        Public Property Path As String
        Public Property Authorized As Boolean = False
    End Class
    <DataContract>
    Public Enum ObjectType
        <DataMember>
        Attribute
        <DataMember>
        Branch
        <DataMember>
        Graft
        <DataMember>
        Prune
        <DataMember>
        Quantity
        <DataMember>
        Slot
        <DataMember>
       WARNING
    End Enum
    <DataContract>
    Public Enum ActionType
        <DataMember>
        INSERT
        <DataMember>
        UPDATE
        <DataMember>
        DELETE
        <DataMember>
        NONE

    End Enum

    Public Class clsImportRow
        Property DateTime As DateTime
        Property Message As String
        Property Id As Int32
    End Class
    Public Class clsImportLog
        Public Shared nextId As Int32 = 0
        Public data As Dictionary(Of Integer, clsImportRow) = New Dictionary(Of Integer, clsImportRow)()
        Public Sub Add(DateTime As DateTime, Message As String)
            data.Add(nextId, New clsImportRow() With {.DateTime = DateTime, .Message = Message, .Id = nextId})
            nextId = nextId + 1
        End Sub
        Public Sub clear()
            data.Clear()
        End Sub
    End Class
End Module



