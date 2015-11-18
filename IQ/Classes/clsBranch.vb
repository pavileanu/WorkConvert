
Option Explicit On
'Option Strict On

Imports dataAccess
Imports IQ.clsBranchState
Imports System.IO
Imports System.Xml
Imports log4net

'This class represents branches of the product tree - each instance is a branch.
'however - each branch may be grafted in many places in the tree - updating any of these grafts of the brach will update them all.
'Each branch references a single product
'More than one branch can reference the same product - allowing the same (underlying) product to appear in different places in the tree

'Each branch has a collection of childbranches
'and *usually* a parent branch - except when it's a graft
'Grafts effectively mean that a branch can have more than one parent - hence you cannot reliably recurse 'backwards' by parent
'To get arround this - the front end renders and mantains (as each branch is opened) an address or 'path' to every branch - typically into the ID of a DIV
'things like Quanty limits can then be 'scoped' with these unique branch addresses - such that they can apply only locally (in that context) if neccessary.

Public Class clsBranch

    Implements i_Editable

    Property ID As Integer
    Property Parent As clsBranch

    'many branches (roots of grafts, have no parent... a branch can have many parents, it can be grafted in many places ie. it can be the child of many branches)
    Property Product As clsProduct
    Property Translation As clsTranslation   'the branch text (if no product is present)
    Property childBranches As Dictionary(Of Integer, clsBranch)

    Property Picture As String
    Property Quantities As Dictionary(Of Integer, clsQuantity) 'A 'flat' dictionary by ID for the generic editor - would be nice if we could edit the more complex multi-dimensional dictionaries.. but that's a bridge too far at the moment
    Property AllParents As Dictionary(Of Integer, clsBranch) = New Dictionary(Of Integer, clsBranch)()

    '                                                                                                  path
    '    Public i_Quantities As Dictionary(Of clsRegion, Dictionary(Of String, clsQuantity))  'Quantity preInstalled, minimum and preferred increments (no maximum because that is handled by slots)
    '                                                                                   there can be more than one because they *may* have a path  (and only apply in that context)
    '                                                                                   Preinstalled quantities by country is what handles carepack auto-adds (which differ by country - yuck)

    'Property Matches As Integer  'a set of bitwise flags for WHETHER this branch featues each of the current keywords
    ' Property Points As Integer 'the total number of matches (including multiple matches on the same keyword)

    Public i_Slots As Dictionary(Of String, clsSlot)  'contains a compound key of slottype, path, and wether it is a + (give) or - (take)
    Property slots As Dictionary(Of Integer, clsSlot)  'containts gives (+) and takes (-) slot info

    Property CollectiveNoun As clsTranslation ' the key to the translation containing the collective noun eg. "Options" for items under this branch (used in the branch child counts - blue numbers in brackets)
    Property collectiveNounSingular As clsTranslation 'eg. "Option"
    Property Matrix As clsScreen 'which screen (set of fields) is used to display the matrix in the front end
    Property Prunes As Dictionary(Of Integer, clsPrune) 'List(Of String) ' a list of the paths at which this branch is pruned
    Property order As Integer
    Property Hidden As Boolean 'For Chassis.Mobos etc
    Property locked As Boolean
    Property rca As String 'A string containing BSGT 'Branches Squares Grid Tabs

    Property Tag As String 'used temporarily during import, not persisted
    Property deleted As Boolean  'soft' deleted (will not be loaded into the OM next time)

    Public HasGrafts As Boolean
    Property GraftedOnAt As List(Of String) 'SOME branches have grafts that only work at specific locations (used for CPUs)
    Property unSearchable As Boolean

    ' Private log As ILog = LogManager.GetLogger("IQDebug")

    'This is only used for the processor import, generally - surfing branches by name is a BAD idea (Names are case and spacing senstitive, language specific and not unique)
    Public Function NameSurf(ByRef path$, nm$) As Boolean

        Dim nseg = Split(nm$, "/")

        For Each b In Me.childBranches.Values
            Dim bn$ = LCase(b.Translation.text(English))
            If bn$.Contains(LCase(nseg(0))) Then
                path$ &= "." & b.ID.ToString.Trim
                If nseg.Count = 1 Then
                    Return True
                Else
                    If b.NameSurf(path$, Mid$(nm$, InStr(nm$, "/") + 1)) Then
                        Return True
                    End If
                End If
                ' Else
                '    Return True
            End If
        Next

    End Function

    Public Function descendantQuantities(ByRef dic As Dictionary(Of String, HashSet(Of clsQuantity)))


        'for each FIO sku there may be multiple quanities (localistations)
        'no skuless branch should have a quantity
        'the same sku should not appear more than once under the same system (at the moment)

        Dim optSKU As String

        If Me.HasSKU Then ' And Me.isOption Then
            optSKU = Me.Product.SKU

            If Me.Quantities.Count Then
                If Not dic.ContainsKey(optSKU) Then
                    If optSKU = "A8007B" Then Stop
                    dic.Add(optSKU, New HashSet(Of clsQuantity))
                End If

                For Each q In Me.Quantities.Values
                    dic(optSKU).Add(q)
                Next
            End If

        End If


        For Each c In Me.childBranches.Values
            c.descendantQuantities(dic)
        Next

    End Function


    'aobranch.compareagainst(validPaths, aobranch)
    Public Function compareAgainst(validSkus As HashSet(Of String), ByRef kept As Integer, ByRef delList As HashSet(Of String))

        If Not Me.deleted Then
            If Me.Product IsNot Nothing Then
                If Me.Product.hasSKU Then
                    If validSkus.Contains(Product.SKU) Then
                        kept += 1
                    Else
                        delList.Add(Me.ID)
                        Me.deleted = True
                    End If

                End If
            End If

            For Each c In Me.childBranches.Values
                c.compareAgainst(validSkus, kept, delList)
            Next
        End If

    End Function


    Public Function flagAsUnsearchable(ByRef count As Integer)

        Me.unSearchable = True
        count += 1

        For Each c In Me.childBranches.Values
            c.flagAsUnsearchable(count)
        Next

    End Function


    Public message As String  'used for 'one shot' (see branch.title) messages for some editor operations (notably) 'shred' there is a (vanishingly) small chance the message will be displayed to the wrong user - so this wan'ts improving at some point (along with the whole ProcesCommand 'cycle' - to remove all the stuff !tagged! on the end)

    ''' <summary>Returns the distinct slots by type, with Path matches overringing empty paths    ''' </summary>
    ''' <returns></returns>
    Public Function slotsInForce(path$) As List(Of clsSlot)

        'ML Change to a list so we can have multiple slots, 
        'problem with mod'ing the qty using i_slots (first idea) is they are a reference so you mod the qty and the object changes everywhere, not what we want

        Dim Dic As New List(Of clsSlot)

        If Not Me.deleted Then 'If the branch is soft deleted - it's slots are no longer in effect)

            For Each slot In Me.slots.Values
                If Not slot.deleted Then
                    If LCase(slot.path) = LCase(path) Or slot.path = "" Then
                        If Not Dic.Exists(Function(f) f.Type Is slot.Type AndAlso Math.Sign(f.numSlots) = Math.Sign(slot.numSlots) AndAlso f.slotNum.Equals(slot.slotNum)) Then
                            Dic.Add(slot)
                        Else
                            'Get a list of slots already there
                            Dim sls = Dic.Where(Function(f) f.Type Is slot.Type AndAlso Math.Sign(f.numSlots) = Math.Sign(slot.numSlots) AndAlso f.slotNum.Equals(slot.slotNum))
                            If slot.path <> "" Then
                                If sls.Count = 1 AndAlso String.IsNullOrEmpty(sls.First().path) Then
                                    Dic.Remove(Dic.Where(Function(f) f.Type Is slot.Type).First())
                                End If
                                Dic.Add(slot)
                            End If
                        End If
                    End If
                End If
            Next

            'Note - memory slots *may* be on the CPU - but there should be a corrseponding quoteitem - so this will just work.

            'Not proud of this - recurses to include the slots off the chassis in the system
            'NB: - there is no quoteItem for the chassis - so we move the slots 'up' - the alternative - of a hidden, chassis quoteitem is (arguably) even uglier
            If Me.Product Is Nothing OrElse Me.Product.isSystem Then
                For Each b In Me.childBranches.Values
                    ' If Not b.Product Is Nothing Then
                    ' If b.Product.ProductType.Code = "CHAS" Then
                    If b.slots.Count Then  'this will be the chassis branch
                        Dim cs As List(Of clsSlot) = b.slotsInForce(path$ & "." & b.ID)
                        Dic.AddRange(cs)
                    End If
                    '  Exit For
                    '               End If
                    '    End If
                Next
            End If
        End If

        Return Dic

    End Function

    Public Function hasQuantity(path$, region As clsRegion) As Boolean

        For Each q In Me.Quantities.Values
            If q.Path$ = path$ And q.Region Is region Then Return True
        Next

        Return False

    End Function

    ''' <summary>
    ''' Helper function - recursively populates a dictionary of option products below the branch, and their paths
    ''' </summary>
    Public Sub optionsBelow(path$, ByRef options As Dictionary(Of clsProduct, String))

        If Me.DisplayName(English).ToLower <> "fios" Then
            If Me.Product IsNot Nothing Then
                If Me.Product.isOption Then
                    If options.ContainsKey(Me.Product) Then
                        If Me.Product.ProductType.Code = "MEM" Or Me.Product.ProductType.Code = "CPU" Then Stop
                        Dim x As String = PathName(path) & " is a duplicate of " & PathName(options(Me.Product))
                        ' Beep()
                    Else
                        options.Add(Me.Product, path)
                    End If

                End If
            End If

            For Each cb In Me.childBranches.Values
                cb.optionsBelow(path & "." & cb.ID, options)
            Next
        Else
            ' Beep()
        End If

    End Sub


    Public Function HasSiblingWithSameProduct() As Boolean

        If Me.Product IsNot Nothing Then
            If Me.Parent IsNot Nothing Then
                For Each b In Me.Parent.childBranches.Values
                    If Not b Is Me Then
                        If Me.Product Is b.Product Then
                            Return True
                        End If
                    End If
                Next
            End If
        End If

    End Function

    Public Sub OptionsPersystem(systemSKU As String, ByRef opts As HashSet(Of String), path As String, ByRef prunes As Integer, ByRef dupes As Integer, sw As StreamWriter, inSkus As HashSet(Of String))

        Dim bn$ = Me.Translation.text(English).ToLower
        If bn.Contains("accessories and") Then Exit Sub

        '  Dim systems As HashSet(Of String)

        ' If Me.Product IsNot Nothing AndAlso Product.SKU = "QK765A" Then Stop

        If Me.PruneInForce(path, HP) <> 0 Then
            prunes += 1
        Else
            If Me.deleted Then
                Exit Sub
            Else
                If Me.Product IsNot Nothing Then
                    ' If Me.Product.deleted Then Exit Sub
                    If Me.Product.hasSKU Then
                        'If Product.SKU = "QK765A" Then Stop
                        If Product.isSystem Then 'And Me.childBranches.Count > 0 Then
                            '  If Product.Publish = False Or Product.Active = False Then Exit Sub 'dont recurse throuh unpublished systems
                            systemSKU = Product.SKU
                        End If

                        If Me.Product.isOption Then
                            'If Me.Product.SKU = "AN975A" And systemSKU = "671163-425" Then Stop

                            '        Dim pn As String = PathName(path)
                            If Product.Active And Product.Publish Then
                                If inSkus.Count = 0 OrElse inSkus.Contains(systemSKU) Then
                                    Dim ck$ = systemSKU & "^" & Me.Product.SKU
                                    If opts.Contains(ck) Then
                                        dupes += 1
                                        ' sw.WriteLine(systemSKU & " " & Me.Product.SKU & " " & PathName(path$))
                                    Else
                                        opts.Add(ck)

                                    End If
                                End If
                            End If
                        End If
                    End If
                    If Me.Product.isSystem = False And Me.HasSKU And Me.childBranches.Count > 0 Then Stop

                End If


                For Each c In Me.childBranches.Values
                    c.OptionsPersystem(systemSKU, opts, path & "." & c.ID, prunes, dupes, sw, inSkus)
                Next
            End If

        End If


    End Sub

    Public Sub DistinctOptionsRecursive(ty As String, fam As String, opts As Dictionary(Of String, clsProduct))

        'famMajor^optsku

        If Me.Product IsNot Nothing Then
            If Me.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
                fam$ = Me.Product.i_Attributes_Code("famMajor")(0).Translation.text(English)
                ty = Me.Product.ProductType.Code  'system level product type SVR,SWD,DTO,NBK etc
            End If

            '  If Me.Product.SKU = "202997-001" Then Stop

            If Me.Product.isOption = True Then
                If Me.Product.hasSKU Then
                    If Me.Product.Active And Not Me.Product.EOL Then
                        Dim ck$ = ty$ & "^" & fam$ & "^" & Me.Product.SKU
                        If Not opts.ContainsKey(ck$) Then opts.Add(ck, Me.Product)
                    End If
                End If
            End If
        End If

        For Each b In Me.childBranches.Values
            b.DistinctOptionsRecursive(ty, fam$, opts)
        Next

    End Sub

    Public Sub systemsBelow(lst As HashSet(Of clsProduct))

        'only used by the SNAP code (probably a duplicate of something)

        If Me.HasSKU AndAlso Not Me.Product.isSystem Then Exit Sub 'don't recurse into options


        If Me.HasSKU AndAlso Me.Product.isSystem Then
            '            If Me.Product.EOL Then Stop
            If Me.Product.Active = True And Me.Product.EOL = False Then
                lst.Add(Me.Product)
            End If
        End If

        For Each b In Me.childBranches.Values
            b.systemsBelow(lst)
        Next

    End Sub

    Public Sub serializeRecursive(bi As clsBranchInfo, depth As Integer, path As String, sw As XmlTextWriter, crossSKUs As Boolean, errormessages As List(Of String))

        Dim indent As String = "" 'As String = StrDup(depth, ChrW(9))
        Dim l

        Dim nogos As New List(Of String)
        nogos.Add("Upsell Opportunities")
        nogos.Add("Top Recommended")
        'nogos.Add("All Options")

        If nogos.Contains(Me.Translation.text(English)) Then
            Exit Sub
        End If

        'Hide the chassis branches for now (longer term we may need to expose)
        If Me.Translation.text(English).EndsWith(" chassis") Then Exit Sub

        'do not write out (at all) inactive or end of life products
        If Me.HasSKU AndAlso (Me.Product.EOL Or Not Me.Product.Active) Then Exit Sub

        If crossSKUs = False Then
            If Me.Product IsNot Nothing AndAlso Not Me.Product.isSystem AndAlso Me.Product.hasSKU Then
                Exit Sub 'don't recurse into otpions
            End If
        End If

        sw.WriteStartElement("branch")
        sw.WriteStartAttribute("path")
        sw.WriteString(path)
        sw.WriteEndAttribute()

        If Me.Translation IsNot Nothing Then
            sw.WriteStartAttribute("text")
            sw.WriteString(Me.Translation.text(English))
            sw.WriteEndAttribute()
        End If

        Dim hr As List(Of String) = Me.ReasonsForHide(bi.buyerAccount, bi.foci, path, bi.buyerAccount.SellerChannel.priceConfig, False, errormessages)

        If hr.Any Then
            sw.WriteStartElement("hideReasons")
            For Each l In hr
                sw.WriteStartElement("reason")
                sw.WriteStartAttribute("text")
                sw.WriteString(l)
                sw.WriteEndAttribute()
                sw.WriteEndElement() '/reason
            Next
            sw.WriteEndElement() '/hideReasons
        Else
            If Me.Product IsNot Nothing Then
                sw.WriteStartElement("product")

                sw.WriteStartAttribute("id")
                sw.WriteString(Me.Product.ID)
                sw.WriteEndAttribute()

                sw.WriteStartAttribute("SKU")
                sw.WriteString(Me.Product.SKU)
                sw.WriteEndAttribute()

                sw.WriteStartAttribute("mfr")
                sw.WriteString(Me.Product.mfrCode)
                sw.WriteEndAttribute()

                If Me.Product.Attributes.Any Then
                    sw.WriteStartElement("productAttributes")
                    For Each pa In Me.Product.Attributes.Values
                        pa.writeXML(sw) ' sw.WriteRaw(pa.XML)
                    Next
                    sw.WriteEndElement() '/productAttributes
                End If

                'count slots by major type
                If Me.Product.isSystem Then
                    Dim slotsummary As New Dictionary(Of String, Integer)
                    Me.summariseSlots(slotsummary)
                    For Each c In Me.childBranches.Values
                        c.summariseSlots(slotsummary)
                    Next

                    sw.WriteStartElement("slotSummary")
                    For Each kvp In slotsummary
                        sw.WriteStartElement(kvp.Key)
                        sw.WriteStartAttribute("number")
                        sw.WriteString(kvp.Value)
                        sw.WriteEndAttribute()
                        sw.WriteEndElement()
                    Next
                    sw.WriteEndElement()

                End If


                If Me.slots.Any Then
                    sw.WriteStartElement("slots")
                    For Each slot In Me.slots.Values
                        slot.writeXml(sw)
                    Next
                    sw.WriteEndElement() '/slots
                End If

                If Me.Quantities.Any Then
                    sw.WriteStartElement("quantities")
                    For Each q In Me.Quantities.Values
                        sw.WriteRaw(q.XML)
                    Next
                    sw.WriteEndElement() ' /quantities
                End If

                sw.WriteEndElement() ' /product
            End If

            For Each b In Me.childBranches.Values
                b.serializeRecursive(bi, depth + 1, path & "." & b.ID, sw, crossSKUs, errormessages)
            Next

        End If

        sw.WriteEndElement() '/branch


    End Sub

    Public Function summariseSlots(slotSummary As Dictionary(Of String, Integer))

        'NOT recursive - only used by the XML SNAP/export - typically called for a system branch and all its children (to get the chassis branch)

        For Each s In Me.slots.Values
            If Not slotSummary.ContainsKey(s.Type.MajorCode) Then slotSummary.Add(s.Type.MajorCode, 0)
            slotSummary(s.Type.MajorCode) += s.numSlots
        Next

    End Function



    Public Sub DoPrunes(ByRef pwc As DataTable, ByRef npid As Integer, path$, famMinor$, dic As Dictionary(Of String, Dictionary(Of String, String)), ByRef kept As Integer, ByRef pruned As Integer)
        'Walks the entire tree - checking the compatibilty of options, by their technology againstagainst the family under which they are appearing
        'pruning off incompatibles on the way

        If Me.DisplayName(English).ToLower.Contains("accessories") Then
            Exit Sub  'Do NOT stumble into the Accessories catalogue
        End If


        If Me.Product IsNot Nothing Then

            'If Product.isSystem Then Stop

            'the fammino attribute appears on both the family and system branches... neither of which are what we're pruning (which is options!)
            If Me.Product.i_Attributes_Code.ContainsKey("famMinor") Then
                famMinor = Me.Product.i_Attributes_Code("famMinor")(0).Translation.text(English)

                If Product.SKU = "803860-B21" Then
                    Dim b = 0
                End If
                'If LCase(Left(famMinor, 3)) = "dl3" Then Stop
                '  If Me.Product.isSystem = False Then Stop
                '  If famMinor = "" Then Stop
                '       opttype = Me.Product.i_Attributes_Code("OptType")(0).Translation.text(English)
            Else
                If Not Me.Product.isSystem Then  'only for options...
                    If famMinor <> "" Then
                        Dim sku$ = Me.SKU
                        If Me.Product.ProductType.Code.ToLower = "hdd" Then
                            If Me.Product.i_Attributes_Code.ContainsKey("desc") Then
                                Dim desc$ = Me.Product.i_Attributes_Code("desc")(0).Translation.text(English)
                                If desc.Contains("3.5") Then
                                    Dim a = 0
                                End If
                            End If
                        End If

                        If sku$ <> "" Then 'NB: ### SKUS return an empty string !!!

                            'if the minor option type (eg NHLLFF35 is not right right for this subfamiles 'tech' .. prune it
                            If Me.Product.i_Attributes_Code.ContainsKey("optFamily") Then
                                Dim optfam As String = Me.Product.i_Attributes_Code("optFamily")(0).Translation.text(English)
                                Dim opttype As String = Me.Product.i_Attributes_Code("optType")(0).Translation.text(English)
                                If dic(famMinor).ContainsKey(opttype) Then
                                    If dic(famMinor)(opttype) <> optfam Then
                                        Dim aprune = New clsPrune(path$, New NullableInt, "DoPrunes Button", pwc, npid)
                                        pruned += 1
                                    Else
                                        kept += 1
                                    End If
                                End If
                            End If
                        End If
                    End If
                Else

                    'Stop 'reached a system - keep going, we want options...
                End If
            End If
        End If


        For Each child In Me.childBranches.Values
            'If famMinor <> "" Then Stop
            child.DoPrunes(pwc, npid, path$ & "." & child.ID, famMinor$, dic, kept, pruned)
        Next

    End Sub


    Public Function toDisk(sw As StreamWriter, depth As Integer, path$)

        Dim sku As String = ""
        If Me.HasSKU Then sku$ = Me.SKU

        Dim l$ = Space(depth * 2) & Me.DisplayName(English)
        If sku$ <> "" Then l$ &= " - " & sku
        If Me.PruneInForce(path$, RootChannel) <> 0 Then
            l$ = "X " & l$ & " X PRUNED"
        End If

        sw.WriteLine(l$)

        For Each c In Me.childBranches.Values
            c.toDisk(sw, depth + 1, path$ & "." & c.ID)
        Next

    End Function

    ''' <summary>Used for import only - OptFamily 'holder' branches are tagged with the optfamily code - such that options with the wrong optfamily for the familyPriStor can be pruned</summary>
    ''' <returns>Dictionary tag>path</returns>
    Public Function TaggedPaths(path$) As Dictionary(Of String, String)

        'will fail if the same tag appears more than once under the branch the methos id called on (which is a good thing!)

        Dim idic As Dictionary(Of String, String) = New Dictionary(Of String, String)
        If Me.Tag <> "" Then
            idic.Add(Me.Tag, path$)
        Else
            For Each c In Me.childBranches.Values
                AppendDic(idic, c.TaggedPaths(path$ & "." & c.ID.ToString))
            Next
        End If

        Return idic

    End Function

    Public Function OptionPaths(Path$) As Dictionary(Of String, String)  'Flattens

        Dim idic As Dictionary(Of String, String) = New Dictionary(Of String, String)
        If Me.HasSKU Then
            idic.Add(Me.SKU, Path$)
        Else
            For Each c In Me.childBranches.Values
                AppendDic(idic, c.OptionPaths(Path$ & "." & c.ID.ToString))
            Next

        End If

        Return idic

    End Function


    ''' <summary>
    ''' recurses until it finds a descendant branch which appears in the view
    ''' </summary>
    ''' <param name="vw"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function isInOrHasDescendantIn(vw As DataView) As Boolean


        iicalls += 1

        ' If vw.Sort <> "ID" Then Stop
        If vw.Count = 0 Then Return False

        Dim b As Integer = vw.Table.Rows(0).Item("Id")
        Dim tl As String = iq.Branches(b).Translation.text(English)

        If vw.Find(Me.ID) <> -1 Then
            Return True
        End If

        If Not (Me.Product IsNot Nothing AndAlso Me.Product.isSystem) Then   'Stop recusing at SKUs (don't cross systems)
            For Each child In Me.childBranches.Values

                If child.isInOrHasDescendantIn(vw) Then Return True

            Next
        End If

    End Function

    Public Function treeNode() As WebControls.TreeNode

        treeNode = New WebControls.TreeNode(Me.DisplayName(English))
        treeNode.Value = Me.ID

        For Each child In Me.childBranches.Values
            treeNode.ChildNodes.Add(child.treeNode)
        Next

    End Function


    Private Sub ensurePath(pth$)

        'makes sure a child named with the first section in the path exists and then recurses

        Dim psegs() As String = Split(pth, "/")

        Dim nm$ = psegs(0)
        If Me.ChildNamed(nm$) Is Nothing Then
            Dim newbranch As clsBranch = New clsBranch(Nothing, Me, iq.AddTranslation(nm$, English, "HWCP", 0, Nothing, -1, False), "", Nothing, Nothing, iq.Screens(719), 0, False, "B")
        End If


        'if there was more than one / delimited segment - recurse on the child we have just created 
        If psegs.Count > 1 Then
            Me.ChildNamed(nm$).ensurePath(Mid$(pth$, InStr(pth$, "/") + 1))
        End If


    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="path"></param>
    ''' <param name="buyeraccount"></param>
    ''' <remarks></remarks>
    Public Sub createCarePacks(path$, buyeraccount As clsAccount)

        'Create carepacks - just in time for the system on this branch

        'Exit Sub

        'If Me.arent.childBranches.ContainsKey(-1) Then Exit Sub 'we already created them !

        'If Me Then


        Me.ensurePath("All Options/Services/HW support")

        Dim cpholder As clsBranch
        If Me.NameSurf(path$, "All Options/Services/HW support") Then  'never planned to use NameSurf this way - it will probably bite us in the arse
            cpholder = iq.Branches(CInt(Split(path$, ".").Last))


            Dim systemSku$ = Me.Product.SKU
            Dim country As clsRegion = buyeraccount.SellerChannel.Region

            Dim cptl As clsTranslation, cpstl As clsTranslation
            cptl = iq.AddTranslation("Carepack", English, "collect", 0, Nothing, 0, False)
            cpstl = iq.AddTranslation("Carepacks", English, "collect", 0, Nothing, 0, False)

            '    Dim packsHolder As clsBranch = New clsBranch(-1, Nothing, Me, cptl, "", cpstl, cptl, iq.i_screens_code("base"), 100, False, "GB")

            Dim H1con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;") 'change this
            Dim countryCode As String = String.Empty
            countryCode = IIf(country.Code = "UK", "GB", country.Code)
            'open the recordset here - to get carepacks for this systemSku$, for this country.code
            Dim sql$ = "select CountryCode	,HWpartnum,	CPKpartnum,	txtStartDate,	txtEndDate,	sortorder from DataStore.products.CarePacks  where HWpartnum = '" & Trim(systemSku$) & "'  and CountryCode = '" & countryCode & "'"  'and this

            Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(H1con, sql$)

            Dim cpkBranch As clsBranch

            'use an ever decreasing negative branch number - so we can create branches with a unique ID, using the correct consturtor (ie.. withou them being persisted to the database)!
            Dim nextid As Integer = -2

            Dim l As List(Of String) = New List(Of String)

            If rdr.HasRows Then cpholder.childBranches.Clear() 'Blow away anything thats defined (or already loaded) IF there are products.carepacks

            While rdr.Read
                l.Add(rdr.Item("CPKpartnum"))
                ' Continue While

                Dim cpkSKU As String = rdr.Item("CPKpartnum")

                ' there's a lot of junk in here - post warranty carepacks etc.
                'NB**  If some systems *only* have junk - this will cause problems
                If iq.i_SKU.ContainsKey(cpkSKU) Then
                    'NB the screen here doesn't matter - it's the screen on the holding branch (parent) thats important !
                    cpkBranch = New clsBranch(nextid, iq.i_SKU(cpkSKU), cpholder, cptl, "", cpstl, cptl, Nothing, 0, False, False, "")
                    nextid -= 1 'decrement
                End If

            End While
            rdr.Close()
            H1con.Close()

        Else
            Beep()

        End If

        'Alternative method (where ALLL carepacks) are grafted pruneCarePacks(path, l, buyeraccount.BuyerChannel)

    End Sub
    Public Sub pruneCarePacks(Path As String, ByRef l As List(Of String), ByRef channel As clsChannel)
        If Path.Split(".").Last() <> Me.ID Then Path = Path & "." & Me.ID
        For Each c In Me.childBranches
            If c.Value.HasSKU() AndAlso c.Value.Product.i_Attributes_Code.ContainsKey("optFam") AndAlso c.Value.Product.i_Attributes_Code("optFam")(0).Translation IsNot Nothing AndAlso c.Value.Product.i_Attributes_Code("optFam")(0).Translation.text(English) = "CAREPACK" Then
                If Not l.Contains(c.Value.Product.SKU) Then

                    Dim Path2 As String = Path & "." & c.Value.ID
                    If c.Value.PruneInForce(Path2, channel) = 0 Then
                        Dim p = New clsPrune(Path2, New NullableInt(channel.ID), "AutoCarePack")
                    End If
                End If
            End If
            c.Value.pruneCarePacks(Path, l, channel)
        Next
    End Sub


    Public Sub index2(ByRef famPaths As Dictionary(Of String, clsBranch), depth As Integer, path$)

        Dim seg() As String
        Dim segs As Integer
        Dim sc$ = ""
        Dim famName = ""

        'root/sector/family
        If depth = 3 Then
            seg = Split(path, ".")
            segs = seg.Count
            'this is the un-'abreviated' PK 
            Dim bid As Integer = CInt(seg(segs - 1))

            If iq.Branches(bid).Product IsNot Nothing Then
                famName = iq.Branches(bid).Product.i_Attributes_Code("FamMajor")(0).Translation.text(English)
                'This needs to be family CODE not the reanslation therof
                'famName = LCase(iq.Branches(CInt(seg(segs - 2))).famname) 'Translation.text(English))
                'famName = Replace(famName, " family", "")
                sc$ = LCase(iq.Branches(CInt(seg.Last)).DisplayName(English))
                famPaths.Add(famName$ & "|" & sc$, Me)
            End If

        End If

        If depth < 3 Then
            For Each child In Me.childBranches.Values
                child.index2(famPaths, depth + 1, path$ & "." & child.ID)
            Next
        End If

    End Sub


    Public Sub OrderFamilies(depth As Integer, ByRef errormessages As List(Of String))

        If Me.childBranches.Count > 0 Then
            If depth = 3 Then

                'NOTE DOUBLE CHILDBRANCHES HERE BECAUSE OFF SUPPLY CHAIN - REMOVE
                If Me.childBranches.Values(0).childBranches.Values(0).Product IsNot Nothing Then
                    If Me.childBranches.Values(0).childBranches.Values(0).Product.i_Attributes_Code.ContainsKey("formFactor") Then
                        Dim ff = Me.childBranches.Values(0).childBranches.Values(0).Product.i_Attributes_Code("formFactor")(0)
                        Dim fft As String = ff.Translation.text(English)

                        Me.order = InStr(LCase("Rack Mount>SMALL FORM FACTOR RACK-MOUNT>Tower>MicroTower>ultra micro tower>Blade>Desktop Mini>Desktops>All in one>Rackable MiniTower>Convertible mini tower>small form factor>Thin Client>horizontally mounted / desktop>wall outlet box>ceiling mount only>WALL/CEILING/DESKTOP/UNDER-TABLE>WALL/DESKTOP/UNDER-TABLE MOUNT>elitebook>laptops>Probook>elitebook mobile workstation>ultrabook>tablet pc>mini-notebook>RACK MOUNT - LARGE FORM FACTOR DISKS>3U rack Unit>RACK-MOUNT MODULAR CHASSIS"), LCase(fft))
                        '                        If Me.order = 0 Then Stop
                        Me.Update(errormessages)

                        Exit Sub
                    End If

                End If
            Else
                For Each b In Me.childBranches.Values.ToArray
                    b.OrderFamilies(depth + 1, errormessages)
                Next b
            End If
        End If



    End Sub


    Public Sub setRCA(depth As Integer, parent As clsBranch, ByRef How As Dictionary(Of String, List(Of Integer)), ByRef errormessages As List(Of String))

        Dim rca As String = ""




        Select Case depth
            Case Is = 1
                rca = "S" 'root branch renders its children (sectors) as squares
            Case Is = 2
                rca = "DGB" 'sector children (families) are rendered as squares - with the option of (united) grid, or branches
            Case Is = 3
                rca = "BG" 'families children (supply chains) are rendered as open branches - with th eoption of a Unitied) grid
            Case Is = 4
                rca = "K" 'systems children (TRO, Upsell and All Options)  render as hyperlinks (not tabs)
            Case Is = 5
                If Me.Picture = "hptop" Then
                    rca = "H" 'TROs children render as TRO headers
                ElseIf Me.Picture = "upsell" Then
                    rca = "U" 'up-sells don't have any real children
                ElseIf Me.Picture <> "" Then
                    'Stop
                ElseIf Me.Translation.text(English).ToLower.Contains("chassis") Then
                    rca = "B"
                Else
                    rca = "TGB" 'all options - renders its children (opt cats) as tabs                
                End If
            Case Is = 6
                If parent.rca = "H" Then
                    rca = "I" 'TRO items
                Else
                    rca = "YTGB" 'all options - renders its children (opt cats) as HYPERLINKS (not tabs)
                End If

            Case Is >= 7
                If Me.childBranches.Count > 0 AndAlso Me.childBranches.First.Value.Translation.Group = "OL3" Then rca = "B" Else rca = "GB" 'systems children render as tabs

        End Select

        If Me.rca <> rca Then
            Me.rca = rca
            If Not How.ContainsKey(rca) Then How.Add(rca, New List(Of Integer))
            How(rca).Add(Me.ID) 'Me.Update(errormessages) - this was very slow - moved to a dictionary of similar updates
        End If

        Dim ord As Integer = 0
        For Each child In From j In Me.childBranches.Values.ToList Order By j.order
            If child.DisplayName(English) <> "Accessories" Then
                child.setRCA(depth + 1, Me, How, errormessages)
            Else
                Beep()
            End If

        Next

    End Sub


    Public Function findProductPathByAttributeValueRecursive(Path$, attributeCode As String, value As String, useWildcard As Boolean) As String

        findProductPathByAttributeValueRecursive = ""

        If Me.Product IsNot Nothing Then
            If Me.Product.i_Attributes_Code.ContainsKey(attributeCode) Then
                If Me.Product.i_Attributes_Code(attributeCode)(0).Translation IsNot Nothing Then
                    If useWildcard Then
                        If Me.Product.i_Attributes_Code(attributeCode)(0).Translation.text(English) Like value Then Return Path$
                    Else
                        If Me.Product.i_Attributes_Code(attributeCode)(0).Translation.text(English) = value Then Return Path$
                    End If
                Else
                    If Me.Product.i_Attributes_Code(attributeCode)(0).NumericValue = value Then Return Path$
                End If
            End If

        End If

        For Each child In Me.childBranches.Values
            Dim pth$ = child.findProductPathByAttributeValueRecursive(Path$ & "." & Trim$(CStr(child.ID)), attributeCode, value, useWildcard)
            If pth$ <> "" Then Return pth$
        Next

    End Function



    Public Function findAllProductPathsByAttributeValueRecursive(Path$, attributeCode As String, value As String, useWildcard As Boolean, bi As clsAccount) As List(Of String)
        Dim errorMessages = New List(Of String)
        findAllProductPathsByAttributeValueRecursive = New List(Of String)()

        'If Me.Product IsNot Nothing Then
        '    If Me.Product.i_Attributes_Code.ContainsKey(attributeCode) Then
        '        If Me.Product.i_Attributes_Code(attributeCode)(0).Translation IsNot Nothing Then
        '            If useWildcard Then
        '                If Me.Product.i_Attributes_Code(attributeCode)(0).Translation.text(English).ToUpper Like value.ToUpper Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
        '            Else
        '                If Me.Product.i_Attributes_Code(attributeCode)(0).Translation.text(English).ToUpper = value.ToUpper Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
        '            End If
        '        Else
        '            If Me.Product.i_Attributes_Code(attributeCode)(0).NumericValue = value Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
        '        End If
        '    End If

        'End If

        For Each child In Me.childBranches.Values.ToArray
            If child.Translation.text(English) = "Top Recommended" Then Continue For 'Special exception for TRO's as we dont want them showing up
            If child.Product IsNot Nothing AndAlso child.Hidden = False AndAlso child.ReasonsForHide(bi, New HashSet(Of String)(Split(bi.BuyerChannel.Focus, ",")), Path, bi.SellerChannel.priceConfig, False, errorMessages).Count = 0 Then
                If child.Product.i_Attributes_Code.ContainsKey(attributeCode) Then
                    If child.Product.i_Attributes_Code(attributeCode)(0).Translation IsNot Nothing Then
                        If useWildcard Then
                            If child.Product.i_Attributes_Code(attributeCode)(0).Translation.text(English).ToUpper Like value.ToUpper Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
                        Else
                            If child.Product.i_Attributes_Code(attributeCode)(0).Translation.text(English).ToUpper = value.ToUpper Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
                        End If
                    Else
                        If child.Product.i_Attributes_Code(attributeCode)(0).NumericValue = value Then findAllProductPathsByAttributeValueRecursive.Add(Path) : Exit Function
                    End If
                End If

            End If
            findAllProductPathsByAttributeValueRecursive.AddRange(child.findAllProductPathsByAttributeValueRecursive(Path$ & "." & Trim$(CStr(child.ID)), attributeCode, value, useWildcard, bi))
        Next

    End Function

    Public Function EnglishName() As String

        Return Me.DisplayName(English)

    End Function

    Dim oParent As clsBranch

    Public Sub New()

        Prunes = New Dictionary(Of Integer, clsPrune)
        GraftedOnAt = New List(Of String)

    End Sub

    Public Function HasSystem() As Boolean

        If Me.Product Is Nothing Then Return False
        If Me.Product.isSystem Then Return True

    End Function
    Public Function HasSKU() As Boolean

        HasSKU = False
        If Me.Product Is Nothing Then Return False
        If Not String.IsNullOrEmpty(Product.SKU) Then Return True
        If Me.Product.i_Attributes_Code.ContainsKey("MfrSKU") Then Stop 'HasSKU = True

    End Function

    Public Function clone(path$, ByRef errormessages As List(Of String)) As clsBranch

        'returns an independent copy of the branch and it's product - not (yet) designed to be used recursively

        clone = New clsBranch(Me.Product.clone, Me.Parent, Me.Translation.clone, Me.Picture, Me.CollectiveNoun, Me.collectiveNounSingular, Me.Matrix, Me.order + 1, Me.Hidden, Me.rca)

        'copy the child branches (chassis and option categories)  as grafts (shallow copy)
        For Each c In Me.childBranches.Values
            ' If c.Parent Is Nothing Then  'grafted branches have no parent
            clone.Graft(c, "grafted during deep copy", "", errormessages)
            ' End If
        Next

        'we only need to copy those slots and quantities that have a specific path - as the others will 'work' anyway (on grafts)
        Dim lq As List(Of clsQuantity) = Me.PathedQuantities() '<this is recursive

        Dim qty As clsQuantity
        For Each q In lq
            Dim newpath As String = Utility.ReplaceSegment(q.Path, Me.ID, clone.ID)
            qty = q.clone(newpath$)
        Next

        Dim ls As List(Of clsSlot) = Me.PathedSlots()

        Dim slt As clsSlot
        For Each slot In ls
            Dim newpath As String = Utility.ReplaceSegment(slot.path, Me.ID, clone.ID)
            slt = slot.clone(newpath$)
        Next

        'Dim ns As clsSlot
        'For Each s In Me.slots.Values
        ' If s.path = "" Then
        ' ns = New clsSlot(s.Type, clone, s.path, s.numSlots, s.notes, s.slotNum, s.requiredFill, s.advisedFill)
        ' End If
        ' Next

    End Function


    'Public Sub IndexProductPaths(path$, ByRef monsterIndex As Dictionary(Of clsProduct, List(Of String)), systems As Boolean, options As Boolean, ProductsToFind As List(Of clsProduct))
    '    'THIS ONLY RETURNS THE FIRST OCCURANCE OF THE PRODUCT
    '    'IT *DOES NOT* INDEX PATHS

    '    If Me.Product IsNot Nothing Then
    '        If Me.Product.hasSKU Then
    '            If ProductsToFind Is Nothing OrElse ProductsToFind.Contains(Me.Product) Then
    '                If (Me.Product.isSystem And systems) Or (Not Me.Product.isSystem And options) Then
    '                    If Not monsterIndex.ContainsKey(Me.Product) Then
    '                        monsterIndex.Add(Me.Product, New List(Of String))
    '                    End If
    '                    monsterIndex(Me.Product).Add(path$)
    '                End If
    '            End If
    '        End If
    '    End If

    '    For Each child In Me.childBranches.Values
    '        child.IndexProductPaths(path$ & "." & Trim$(CStr(child.ID)), monsterIndex, systems, options, ProductsToFind)
    '    Next

    'End Sub
    Public Function PathedQuantities() As List(Of clsQuantity)

        PathedQuantities = New List(Of clsQuantity)

        For Each b In Me.childBranches.Values
            For Each q In b.Quantities.Values
                If q.Path <> "" Then
                    PathedQuantities.Add(q)
                End If
            Next

            PathedQuantities.AddRange(b.PathedQuantities()) '<recurse
        Next


    End Function

    Public Function PathedSlots() As List(Of clsSlot)

        PathedSlots = New List(Of clsSlot)

        For Each b In Me.childBranches.Values
            For Each s In b.slots.Values
                If s.path <> "" Then
                    PathedSlots.Add(s)
                End If
            Next
            PathedSlots.AddRange(b.PathedSlots()) ' < recurse
        Next

    End Function

    Public Function preInstalled(buyeraccount As clsAccount, path$, ByRef errormessages As List(Of String)) As List(Of clsQuantity)

        'Recursive - not to be confused with AddPreinstalledRecursive which is used when adding items to quotes
        'Returns a list of all the preinstalled quantities under the this Branch - (when at the path specified).
        'pass the path to a system - you'll get all the preinstalled FOC options
        'used by Contrast.ASPX - for comparing the preinstalled options in systems

        preInstalled = New List(Of clsQuantity)

        Dim Region As clsRegion = buyeraccount.SellerChannel.Region
        Dim ipath$
        Dim q As clsQuantity

        For Each b In Me.childBranches.Values
            If Not b.deleted Then
                ipath = path & "." & b.ID
                q = b.LocalisedQuantity(Region, ipath$, errormessages) 'returns the 'best' quantity record for this user - ie, the deepest, narrowest match
                If Not q Is Nothing Then
                    If q.NumPreInstalled > 0 Then
                        If q.FOC Then
                            preInstalled.Add(q)
                        End If
                    End If
                End If
                preInstalled.AddRange(b.preInstalled(buyeraccount, ipath$, errormessages))
            End If
        Next

    End Function

    Public Function StalePrices(buyerAccount As clsAccount, ByRef errorMessages As List(Of String)) As List(Of clsVariant) 'Dictionary(Of String, clsProductVariant)   'only a combination of Product and Variant is unique

        'returns a a list of the variants whose prices are 'stale'
        'The variants include the distiSKU

        StalePrices = New List(Of clsVariant) 'Dictionary(Of String, ClsProductVariant)  'distiSKUs >ProductVariants

        Dim prices As List(Of IQ.clsPrice)

        'Getprices won't actually queue or make any webservice request 
        '        If Me.Product.inFeed(buyerAccount.SellerChannel) Then
        'If Me.Product.i_Variants IsNot Nothing Then 'Some (inactive typically) products have no variants - becuase nobody stocks or has prices for them)
        ' If Me.Product.i_Variants.ContainsKey(buyerAccount.SellerChannel) Then 'it it in the sellers feed

        '9 will return Everyone Prices, customer specific prices plus POA's for those that don't exist (regardless of the channels priceconfig)

        prices = Me.Product.GetPrices(buyerAccount, 9, iq.AllVariants, errorMessages, True)

        For Each p In prices
            If p IsNot Nothing Then 'POA's are 'nothings' in the list of retrieved prices                
                If p IsNot Nothing Then '~~
                    If Not p.SKUVariant.DistiSku.Contains("FAKE") And Not p.SKUVariant.DistiSku.Contains("###") Then
                        'fetch a new price for all 'old' prices, POA's and temporary clones of listprices
                        Dim minutesold As Long = DateDiff(DateInterval.Minute, p.lastRequested, Now)
                        If minutesold > 60 Or p.Price.isValid = False Then
                            StalePrices.Add(p.SKUVariant)
                            p.lastRequested = Now
                        End If
                    End If
                End If
            End If
        Next

    End Function

    Public Function RenderTabHeads(pbi As clsBranchInfo, pbs As clsBranchState, tabs As Dictionary(Of clsBranch, clsVisibility), ByRef errorMessages As List(Of String)) As Panel

        'Renders this branches visible children as a set of tab heads

        RenderTabHeads = New Panel
        RenderTabHeads.CssClass = "tabStrip"
        RenderTabHeads.ID = "tabStrip" & pbi.path.Split(".").Length.ToString

        Dim tab As Panel
        Dim autoOpen As Boolean = True
        Dim langEN As clsLanguage = (From l In iq.Languages.Values Where l.Code = "EN").First

        For Each vis In From k In tabs.Values 'Needs optimizing so we dont get branchinto twice in here, quick fix - ML
            Dim cbi As clsBranchInfo = New clsBranchInfo(pbi.lid, pbi.path & "." & vis.branch.ID.ToString.Trim, Nothing, pbi.treeWidth, pbi.Paradigm, errorMessages)
            Dim bs As clsBranchState = getbranchstate(cbi.lid, vis.path)

            If bs IsNot Nothing Then autoOpen = False
        Next
        Dim intFirstItem As Integer = 0

        For Each vis In From k In tabs.Values Order By k.branch.order  'Me.childBranches.Values 'was me.childbranches
            If vis.branch.Hidden AndAlso Not pbi.showAll Then Continue For
            tab = New Panel
            RenderTabHeads.Controls.Add(tab)

            Dim cbi As clsBranchInfo = New clsBranchInfo(pbi.lid, pbi.path & "." & vis.branch.ID.ToString.Trim, Nothing, pbi.treeWidth, pbi.Paradigm, errorMessages)
            Dim bs As clsBranchState = getbranchstate(cbi.lid, vis.path)

            ' Auto Open the first big hyperlink (or one with an order of 10)
            '     If bs Is Nothing AndAlso vis.branch.order = 10 AndAlso pbs.rca = enumBt.bighyperlinK AndAlso autoOpen Then
            If bs Is Nothing AndAlso intFirstItem = 0 AndAlso (pbs.rca = enumBt.bighyperlinK Or pbs.rca = enumBt.Tab) AndAlso autoOpen Then

                Dim bt As enumBt = CType(BTchar.IndexOf(vis.branch.rca.First), enumBt)
                bs = New clsBranchState(pbi.lid, cbi.path, bt, False, 0, 100)

            End If
            intFirstItem += 1
            Dim title As Panel = vis.branch.Title(cbi, False, True, False, 0, 0, vis.hideReasonList, errorMessages, pbs, bs)
            'If vis.branch.childBranches.Count > 0 Then
            tab.Controls.Add(title)
            'End If
            tab.Controls.Add(vis.branch.PromoIndicators(cbi, errorMessages))
            tab.ID = vis.path & ".tab"

            Dim func$ = ""
            Dim q$ = Chr(34)
            'NB: there is no way to close a tab per se, (you just select another)


            Dim pth As String = vis.path 'tabs(branch).path

            func$ &= "burstBubble(event);"

            If (Not langEN Is Nothing) AndAlso (vis.branch.Translation.text(langEN) = "HW Support") Then
                func$ &= "getBranches('cmd=openFiltered&path=" & pth$ & "');"
            Else
                func$ &= "getBranches('cmd=openTab&path=" & pth$ & "');"
            End If

            If Trim(vis.branch.rca) = "U" Then
                'func$ &= "setTimeout(function(){showQuote()},200);" 'Selecting the upsell opportunities tab needs to refresh the quote (to generate the VM's and update the div)
                tab.CssClass &= " upsell"
            ElseIf LCase(vis.branch.Picture = "hptop") Then
                tab.CssClass &= " hpTopRecommended"
            End If

            func$ &= "return false;"
            tab.Attributes("onclick") = func$ 'was omd

            If pbs.rca = enumBt.hYperlink Or pbs.rca = enumBt.bighyperlinK Then
                tab.CssClass &= " ib"
                If pbs.rca = enumBt.hYperlink Then
                    tab.CssClass &= " optionsLink"  'this is temporary
                ElseIf pbs.rca = enumBt.bighyperlinK Then
                    tab.CssClass &= " bigLink"  'this is temporary
                End If

                If bs Is Nothing OrElse bs.rca = enumBt.Hidden Then
                    tab.CssClass &= " inActiveLink"
                Else
                    tab.CssClass &= " ActiveLink hpOrange"
                End If
            Else
                If bs Is Nothing OrElse bs.rca = enumBt.Hidden Then
                    tab.CssClass = "inActiveTab"
                Else

                    tab.CssClass = "activeTab"
                    'AutoOpen the active tab
                    '  Dim bt As enumBt = CType(BTchar.IndexOf(vis.branch.rca.First), enumBt)
                    '  Dim bss As clsBranchState = New clsBranchState(pbi.lid, pbi.path & "." & vis.branch.ID.ToString, bt, 1, 0, 100)
                    'put the switcher in the active tab

                    ' tab.Controls.Add(NewLit("<div class='switcherGap'>&nbsp;</div>"))
                    'tab.Controls.Add(Switcher(cbi, bs, False, vis.branch.rca))

                End If
            End If
            'tab.Controls.Add(NewLit(func$))
        Next


        '****Options search Hyperlink/tab ***
        If pbi.branch.Product IsNot Nothing Then
            If pbi.branch.Product.isSystem Then

                tab = New Panel
                RenderTabHeads.Controls.Add(tab)

                Dim ttlpnl As Panel = New Panel
                tab.CssClass &= "ib"
                tab.Controls.Add(ttlpnl)

                Dim lbl As Label = New Label
                lbl.Text = Xlt("Search", pbi.agentAccount.Language)
                ttlpnl.Controls.Add(lbl)
                ttlpnl.CssClass &= "bigLink"


                tab.Attributes("onclick") = "burstBubble(event);$('#optionsSearch').show();$('#systemsSearch').hide();searchClick('" & pbi.path & "');return false;"

            End If
        End If


        'tab.Controls.Add(vis.branch.PromoIndicators(cbi, errorMessages))
        'tab.ID = vis.path & ".tab"

        'Dim c As Literal
        'c = New Literal
        'RenderTabHeads.Controls.Add(c)
        'c.Text = "<div style='clear:both;'></div>"

    End Function
    Public Function getPrune(path$, sellerchannel As clsChannel) As clsPrune

        getPrune = Nothing
        Dim c As IEnumerable(Of clsPrune) = From j In Me.Prunes.Values Where LCase(j.Path) = LCase(path$) And (j.ChannelID.value Is DBNull.Value OrElse CInt(j.ChannelID.value) = sellerchannel.ID)
        If c.Count > 0 Then
            getPrune = c.First
        End If

    End Function

    Public Function PruneInForce(path$, sellerchannel As clsChannel) As Integer

        'returns the ID of any prune

        PruneInForce = 0
        If Me.Prunes.Count Then
            Dim p = From j In Me.Prunes.Values Where LCase(j.Path) = LCase(path$) And (j.ChannelID.value Is DBNull.Value OrElse CInt(j.ChannelID.value) = sellerchannel.ID)
            If p.Count > 0 Then
                PruneInForce = p.First.ID : Exit Function
            End If
        End If


        '(cpu) Branches are 'virtually pruned' if they're at the 'wrong' location   - this is NOT obvious
        If Me.GraftedOnAt.Count > 0 Then
            ' Dim j$ = PathName(path$)
            ' Dim k$ = PathName(GraftedOnAt(0))

            Dim spath As String = Left(path, InStrRev(path, ".") - 1)
            If Me.GraftedOnAt.Contains(spath) Then
                PruneInForce = 0
            Else
                PruneInForce = 1000000
            End If
        End If

        'If path.Contains("136780") Then
        '    Dim test As String = ""
        'End If
        'life would be easier (and faster) if the prunes were indexed by path - but this would make them tricky to edit (editiong a dictionary keyed by a path is not yet supported)



    End Function

    Public Function renderChildren(pbi As clsBranchInfo, pbs As clsBranchState, isGrid As Boolean, ByRef EndPath As String, descendants As Dictionary(Of clsBranch, clsVisibility), ByRef errorMessages As List(Of String), into As Panel) As PlaceHolder 'Panel

        'NB - descendants contains Branches which may not ultimately display in 'normal' (non-admin) mode
        'descendants is also already ordered, and have been filtered by a view 
        'see HideReason
        Dim language = CType(iq.sesh(pbi.lid, "BuyerAccount"), clsAccount).Language

        If Me IsNot pbi.branch Then errorMessages.Add("wrong branch in PBI") ': Return childrenPanel

        Dim priceconfig As Integer = pbi.buyerAccount.SellerChannel.priceConfig
        If Left$(pbi.buyerAccount.SellerChannel.Code, 3) = "MHP" Then priceconfig = priceconfig And Not 8 'HP (universal instances)  dont have a webservice (temporary hack)

        If (priceconfig And 8) > 0 Then  'customer specific (webservice) pricing
            EmbedUpdateRequest(pbi, descendants, into, errorMessages)
        End If

        If (Not pbi Is Nothing) AndAlso (Not pbi.ScreenHeader Is Nothing) AndAlso (Not pbi.ScreenHeader.screen Is Nothing) AndAlso (pbi.ScreenHeader.screen.code = "hmcSOFOS") AndAlso (descendants.Count > 0) Then
            Dim rok = RenderROK(pbi, descendants)
            If Not rok Is Nothing Then into.Controls.Add(rok)
        End If

        Dim showOnly As Integer = iq.sesh(pbi.lid, "showOnly")  'Keyword search results - supress systems siblings thing


        Dim numrows As Integer = 0
        Dim rendered As Integer = 0

        For Each kvp In descendants  'these are already ordered
            Dim branch As clsBranch = kvp.Value.branch
            Dim visibility As clsVisibility = kvp.Value

            If visibility.hideReasonList.Count = 0 Or pbi.showAll Then
                If rendered < pbs.maxChildren Then

                    Dim cbi As clsBranchInfo  'note Branchinfo is not persisted in the session (thats branchstate)
                    cbi = New clsBranchInfo(pbi.lid, visibility.path, pbi.lblMatches, pbi.treeWidth, pbi.Paradigm, errorMessages, If(pbs.United, pbi.path, Nothing)) 'for closed branches the branchinfo.branchSTATE will be NOTHING

                    rendered += 1
                    'Dim pnl As Panel = branch.UI(cbi, EndPath, errorMessages)

                    Dim supressed As Boolean = False
                    If showOnly <> 0 Then 'we're showing just one system (a system from the keyword search results)
                        If branch.HasSKU AndAlso branch.Product.isSystem Then
                            If branch.ID <> showOnly Then supressed = True
                        End If
                    End If

                    If Not supressed Then
                        into.Controls.Add(branch.UI(cbi, EndPath, errorMessages))
                    End If

                    If False AndAlso (pbs.rca = enumBt.DetailSquare Or pbs.rca = enumBt.Square) Then
                        'throw in an advert
                        If Rnd(1) > 100 Then  'NB this is NEVER turn (effective comment)
                            Dim newlit As Literal = New Literal
                            newlit.Text = "<div class='squareAdvert'>Banner</div>"
                            into.Controls.Add(newlit)
                        End If
                    End If
                End If
                numrows += 1
            End If
        Next kvp

        If pbs.rca = enumBt.Square AndAlso EndPath = "tree.1" Then


            'Assume that this is only the top page...
            'Could ultimately do with a type of "linksquare" or something but it wouldnt fit with renderchildrenas, would have to be renderself...
            Dim agentAccount = iq.seshTyped(Of clsAccount)(pbi.lid, "AgentAccount")

            If agentAccount.Manufacturer = Manufacturer.HPE Then
                into.Controls.Add(NewLit("<div class=""square dropShadow ib"" onclick=""burstBubble(event);ShowSolutionStore('" & pbi.lid & "','" & agentAccount.User.Email & "','" & agentAccount.SellerChannel.Code & "','" & agentAccount.mfrCode & "');return false;"">" & _
                    "<div class=""branchTitle""><span>" & Xlt("Solution Store", English) & "</span></div><div class=""hpBlue"" style=""margin-top:1.5em;margin-left:1.3em;text-align:left;font-size:1.5em;width: 60%;"">" & Xlt("Flex-Bundle Solutions", English) & "</div><div style=""margin-top:1.2em;margin-left: 2em;width:70%;text-align:left;"">" & Xlt("Workload optimized solutions including servers, storage, networking and services", English) & "</div>" & _
                    "</div>"))
            End If
        End If

        Dim func$

        'End If

        'If pbs.rca = enumBt.TROitem AndAlso (descendants.Count - 1 = numrows Or rendered = pbs.maxChildren - 1) Then 'might need a hack to only show on carepacks for now
        'Add help me choose in here
        'Find this category in all options....
        'OptionPaths()
        If pbs.rca = enumBt.TROitem Then
            Dim d As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))()
            iq.Branches(Split(pathToSystem(pbi.path), ".").Last).SkuPaths(d, pathToSystem(pbi.path), True)
            Dim s As String = (From sk In descendants.Values Where sk.branch.Product IsNot Nothing Select sk.branch.Product.SKU).FirstOrDefault
            If d.ContainsKey(SKU) Then
                For Each p In d(SKU)
                    If FindBranchByName(p, "All Options") IsNot Nothing AndAlso Me.Translation.text(English) = "Care Pack" Or Me.Translation.text(English).Contains("Microsoft") Then 'yuuuck, this must be taken out as it relies on the text, needed to get it in quick
                        p = p.Substring(0, p.Length - Split(p, ".").Last.Length - 1)
                        into.Controls.Add(NewLit("<button class='hpBlueButton smallfont' onclick='getBranches(""cmd=defFilterOn&path=" + pathToSystem(pbi.path) + "&to=" + p + "&into=tree"");return false;'>" & Xlt("Help Me Choose", language) & "</button>"))
                        Exit For
                    End If
                Next
            End If

        End If

        If isGrid Then

            Dim q$ = "'"
            If Not pbi.lblMatches Is Nothing Then
                'If cbi IsNot Nothing Then
                If pbi.ScreenHeader.Vw.Count = 1 Then
                    pbi.lblMatches.Text = "1 match " 'e  'This isn't *quite* right 
                Else
                    If pbi.ScreenHeader.Vw.Count > 0 Then
                        pbi.lblMatches.Text = pbi.ScreenHeader.Vw.Count & " matches " ' & cbi.CollectivePlural 'This isn't *quite* right 
                    End If
                End If
            End If



            Dim occ$

            'Compare/Constrast (scales) has been removed until it can get some more attention
            '  occ$ = "contrast('" & pbi.path$ & "');return false;"
            '  into.Controls.Add(MakeRoundButton("scales.png", "Compare selected systems", occ$, "", "contrast", pbi.AgentAccount.Language))  'positioned 2 ems 'over' (relatively) so it falls nicely under the the checkboxes and remains in the flow

            'call showchildren.aspx ..
            occ$ = "exportGridAsCSV('" & pbi.path & "');"

            into.Controls.Add(MakeRoundButton("excl.png", "Export Grid as CSV", occ$, "", "contrast ib", pbi.agentAccount.Language))  'positioned 2 ems 'over' (relatively) so it falls nicely under the the checkboxes and remains in the flow


        End If




        'contrast/compare/scales functionality disbabled for now
        If False Then
            Dim contrastpanel As New Panel
            contrastpanel.ID = "contrast." & pbi.path$
            contrastpanel.CssClass &= " compareTablePanel"
            Dim lit As Literal
            lit = New Literal
            lit.Text = "&nbsp;"

            contrastpanel.Controls.Add(lit)
            into.Controls.Add(contrastpanel)
        End If


        If numrows > 100 Then

            Dim btnShowall As HtmlGenericControl = New HtmlGenericControl("button")

            into.Controls.Add(btnShowall)
            If pbs.maxChildren = 1000 Then
                btnShowall.InnerHtml = Xlt("Show first 100 items only", pbi.agentAccount.Language) 'note - the page doesnt post back to this isn't actually what changes the button ! - see recaption script
                func$ = ButtonScript("path=" & pbi.path & "&cmd=maxrows&rows=100")
            Else
                If numrows <= 1000 Then
                    btnShowall.InnerHtml = String.Format(Xlt("Show all {0} items", pbi.agentAccount.Language), numrows)
                    func$ = ButtonScript("path=" & pbi.path & "&cmd=maxrows&rows=1000")
                Else
                    btnShowall.InnerHtml = Xlt("Show first 1,000 items", pbi.agentAccount.Language)
                    func$ = ButtonScript("path=" & pbi.path & "&cmd=maxrows&rows=1000")
                    Dim slowlit As Literal
                    slowlit = New Literal
                    slowlit.Text = "<div class='perfNote'>This may be slow !, For performance reasons - we never show more than 1000 rows on a page </div>"

                    into.Controls.Add(slowlit)
                End If
            End If

            btnShowall.Attributes("onclick") = func$
            btnShowall.Attributes("class") = "textButton"
            btnShowall.Attributes.Add("style", "display:block;clear:both") 'the 'button' style places things 'inline' - which we dont actually want here (so we override it)

        End If

        Pacc("Branch.renderChildren")
        '   Return childrenPanel

    End Function

    ''' <summary>
    ''' ROK - Display any OS extra information :
    ''' Look through all the descendent products and see if all share the same Windows edition.
    ''' If they do, display extra OS information
    ''' </summary>
    ''' <param name="pbi"></param>
    ''' <param name="descendants"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function RenderROK(pbi As clsBranchInfo, descendants As Dictionary(Of clsBranch, clsVisibility)) As Literal

        Dim kyLanguage As clsLanguage = (From l In iq.Languages.Values Where l.Code = "KY").First
        Dim osTitle As String = Nothing
        Dim osEdition As String = Nothing
        Dim osCategory As String = Nothing
        Dim osKey As String = Nothing

        For Each branch In descendants.Keys
            If Not branch.Product Is Nothing Then
                If branch.Product.i_Attributes_Code.ContainsKey("Category") AndAlso branch.Product.i_Attributes_Code.ContainsKey("edition") Then
                    Dim category = branch.Product.i_Attributes_Code("Category")(0).Translation.text(kyLanguage)
                    Dim edition = branch.Product.i_Attributes_Code("edition")(0).Translation.text(kyLanguage)

                    Dim key1 As String = Nothing
                    Dim key2 As String = Nothing

                    If String.Equals(category, "Windows Server 2012", StringComparison.InvariantCultureIgnoreCase) Then
                        key1 = "W2012"
                    ElseIf String.Equals(category, "Windows Server 2012 R2", StringComparison.InvariantCultureIgnoreCase) Then
                        key1 = "W2012R2"
                    End If

                    If edition.ToLower().StartsWith("standard") Then
                        key2 = "STD"
                    ElseIf edition.ToLower().StartsWith("essentials") Then
                        key2 = "ESS"
                    ElseIf edition.ToLower().StartsWith("datacenter") Then
                        key2 = "DAT"
                    End If

                    If Not key1 Is Nothing AndAlso Not key2 Is Nothing Then
                        Dim key = String.Format("{0}_{1}", key1, key2)

                        If osEdition Is Nothing Or osCategory Is Nothing Then
                            osEdition = edition
                            osCategory = category
                            osKey = key
                        Else
                            If String.Compare(key, osKey, True) <> 0 Then
                                osEdition = Nothing    ' No common edition info can be displayed
                                osCategory = Nothing
                                osKey = Nothing
                                Exit For
                            End If
                        End If
                    End If

                End If
            End If
        Next

        If Not String.IsNullOrEmpty(osKey) Then

            If iq.ROKAttributes.ContainsKey(osKey) Then

                Dim attributes = iq.ROKAttributes(osKey)

                Dim attrLicence = attributes.Where(Function(a) a.Code = "licences").FirstOrDefault()
                Dim attrVirt = attributes.Where(Function(a) a.Code = "virtualisation").FirstOrDefault()
                Dim attrCals As clsROKAttribute = attributes.Where(Function(a) a.Code = "cals").FirstOrDefault()
                Dim attrCpus As clsROKAttribute = attributes.Where(Function(a) a.Code = "maxcpus").FirstOrDefault()
                Dim attrUsers As clsROKAttribute = attributes.Where(Function(a) a.Code = "maxusers").FirstOrDefault()
                Dim attrRam As clsROKAttribute = attributes.Where(Function(a) a.Code = "maxram").FirstOrDefault()

                Dim lang = pbi.buyerAccount.Language

                Dim licence As String = String.Empty
                Dim virt As String = String.Empty
                Dim cals As String = String.Empty
                Dim maxCpus As String = String.Empty
                Dim maxUsers As String = String.Empty
                Dim maxRam As String = String.Empty

                If Not attrLicence Is Nothing Then licence = attrLicence.Translation.textTranslation(lang)
                If Not attrVirt Is Nothing Then virt = attrVirt.Translation.textTranslation(lang)
                If Not attrCals Is Nothing Then cals = attrCals.Translation.textTranslation(lang)
                If Not attrCpus Is Nothing Then maxCpus = attrCpus.Translation.textTranslation(lang)
                If Not attrUsers Is Nothing Then maxUsers = attrUsers.Translation.textTranslation(lang)
                If Not attrRam Is Nothing Then maxRam = attrRam.Translation.textTranslation(lang)

                Dim title As String = osCategory & " " & osEdition
                Dim table As String = Nothing
                table = String.Format("<span class='leftcol'><span class='subtitle'>{0}: </span>{1}</span><span class='rightcol'><span class='subtitle'>{2}: </span>{3}</span><br/>",
                                      Xlt("Licenses", lang), licence, Xlt("Virtualisation", lang), virt)
                table += String.Format("<span class='leftcol'><span class='subtitle'>{0}: </span>{1}</span><span class='rightcol'><span class='subtitle'>{2}: </span>{3}</span><br/>",
                                       Xlt("CALs", lang), cals, Xlt("Max. CPUs", lang), maxCpus)
                table += String.Format("<span class='leftcol'><span class='subtitle'>{0}: </span>{1}</span><span class='rightcol'><span class='subtitle'>{2}: </span>{3}</span><br/>",
                                       Xlt("Max. Users", lang), maxUsers, Xlt("Max. RAM", lang), maxRam)

                Dim infoDisplay As Literal = New Literal
                infoDisplay.Text = String.Format("<div class='quickFilterInfo'><span class='title'>{0}</span><br/><br/>{1}</div><br/>", title, table)

                RenderROK = infoDisplay

            End If
        End If

    End Function

    Public Function Insert(ByRef errormessages As List(Of String)) As Object Implements i_Editable.Insert

        Return New clsBranch(Me.Product, Me.Parent, Me.Translation, Me.Picture, Me.CollectiveNoun, Me.collectiveNounSingular, Me.Matrix, Me.order, Me.Hidden, Me.rca)

    End Function

    Public Function AsTextRecursive(level As Integer) As String

        AsTextRecursive = "" 'new (possibly not requeired)
        AsTextRecursive &= StrDup(level, "    ") & Me.Translation.text(English) & vbCrLf

        For Each c In (From cb In childBranches.Values Order By cb.order) 'Me.childBranches.Values
            AsTextRecursive &= c.AsTextRecursive(level + 1)
        Next

    End Function

    Public Function LocalisedQuantity(ByVal region As clsRegion, path$, ByRef errorMessages As List(Of String)) As clsQuantity

        '    Pmark("LocalisedQuantity")
        '        Try

        LocalisedQuantity = Nothing

        'this could be speeded up by adding a dictioanary of region to path/quanity - but it's probably unnecessary

        Dim bubbleRegion As clsRegion = region

        'IF there are ONLY pathed Quanitites and none of them apply - return NOTHING - NOT! an

        Do
            If Me.Quantities IsNot Nothing Then
                For Each qty In Me.Quantities.Values   'branches now carry quite a small number of quanities.. becuase of the neat way we do regions
                    If Not qty.deleted Then
                        '   If Not qty.IsAutoAdd Then  'ClsQuantity handes autoadds AND regionsalisation - but a branch with an autoadd is NOT necessarilt restircted to only that region
                        If qty.Region Is bubbleRegion Then
                            '   If qty.NumPreInstalled <> 0 Then Stop
                            If LCase(qty.Path) = LCase(path$) Or qty.Path = "" Then
                                'If qty.Path = path$ Then
                                '  If qty.Path <> "" Then Stop
                                ' If Me.Product.ProductType.Code <> "SVR" Then Stop
                                If qty.Path <> "" Or LocalisedQuantity Is Nothing Then  'don't think this is qute right - because we want an explicit match to override a blank path
                                    LocalisedQuantity = qty
                                    If qty.Path <> "" Then
                                        Exit Do 'we're done (becuase this item has specific scope (and overrdies any quantity present with an empty path (global scope)))
                                    End If
                                End If
                            ElseIf qty.Path <> "" Then
                                '   Beep()
                                ' qty's on the branch but whos path does not match . .
                                '  Dim nmq$ = PathName(qty.Path)
                                '   nmq$ = ""

                                '   Debug.Print(qty.Path & iq.Branches(Split(qty.Path, ".").Last).DisplayName(English))
                            End If
                        End If
                    End If
                    '  End If
                Next
            Else
                Dim test As String = ""
            End If

            If bubbleRegion Is r_worldwide Then
                Exit Do 'we've reached the top
            End If

            If bubbleRegion.Parent Is Nothing Then
                errorMessages.Add("* region " & bubbleRegion.Name.text(English) & "(" & bubbleRegion.Code & ") is detached (not connected to XW (r_worldwide) - See LocalisedQuantity())")
                Exit Do 'some region is detached - we couldn't reach the root from it
            End If

            bubbleRegion = bubbleRegion.Parent

        Loop While LocalisedQuantity Is Nothing

        'Catch ex As Exception
        '    Beep()
        ' Finally

        '   Pacc("LocalisedQuantity")
        ' End Try

    End Function

    ''' <summary>
    '''  Determines branch visiblity (at this path) for the buyer account - based on Focus, Geography, Available pricing, Active Dates, EOL, Presence in the feed, "AlsoHost" etc.
    ''' </summary>
    ''' <remarks>Does NOT call the webservice
    ''' </remarks>
    Public Function ReasonsForHide(buyeraccount As clsAccount, foci As HashSet(Of String), path$, priceconfig As Integer, exitEarly As Boolean, ByRef errorMessages As List(Of String)) As List(Of String)

        'TODO if we're not in showall mode.. exit asap (as soon as we have a reason)
        'This function never calls the webservice

        ReasonsForHide = New List(Of String)

        If Me.Product Is Nothing Then Exit Function

        If Me.deleted Then ReasonsForHide.Add("Branch is deleted")

        If Not Me.Product.Active Then ReasonsForHide.Add("Product is not Active")
        If Not Me.Product.Publish Then ReasonsForHide.Add("Product is not Published (was AAonly)")

        If Now < Me.Product.activeFrom Then ReasonsForHide.Add("Product is NOT YET active - activeFrom " & Me.Product.activeFrom)
        If Now > Me.Product.activeTo Then ReasonsForHide.Add("Product is NO LONGER active - activeTo " & Me.Product.activeTo)

        If exitEarly And ReasonsForHide.Count > 0 Then Exit Function

        If Not Me.HasSKU Then
            Exit Function 'some placeholding branches (for example families) have products but not SKUS
        Else

            '***** temporary - default to HPE for Pre-Split (undefined) accounts - REMOVE
            If String.IsNullOrEmpty(buyeraccount.mfrCode) Then
                buyeraccount.mfrCode = "HPE"
            End If
            '*******************

            If Not String.IsNullOrEmpty(Product.mfrCode) Then
                If Not Product.Manufacturer = buyeraccount.Manufacturer Then ReasonsForHide.Add("Wrong Company (HPI/E)")
            End If

            Dim rh As List(Of String) = Me.inFocus(foci)
            'If rh.Count > 0 Then Stop
            ReasonsForHide.AddRange(rh)

            If exitEarly And ReasonsForHide.Count > 0 Then Exit Function

            'Dim channelSKU$
            'channelSKU$ = buyeraccount.SellerChannel.ChannelSKU(Me.Product, iq.StandardVariant)
            'channelSKU$ = Product.i_variants(buyeraccount.SellerChannel.ChannelSKU(Me.Product, iq.StandardVariant)
            '  If (buyeraccount.SellerChannel.priceConfig And 2) = 0 Then ' if They don't show list prices .. require it to be in their feed
            If Product.i_Variants Is Nothing Then ReasonsForHide.Add("No Variants (not in anyones feed ?)")
            If Not Product.i_Variants.ContainsKey(buyeraccount.SellerChannel.IsCloneOf) Then
                'this seller channel has no variant of this product - is there a list price ?
                If Not Product.i_Variants.ContainsKey(HP) Then
                    ReasonsForHide.Add("Not in the feed - AND *no* list prices (no Disti or HP variants)")
                Else
                    Dim haveListPriceForRegion As Boolean = False
                    For Each v In Product.i_Variants(HP)
                        If v.Region.Encompasses(buyeraccount.SellerChannel.IsCloneOf.Region) Then haveListPriceForRegion = True : Exit For
                    Next
                    If Not haveListPriceForRegion Then
                        ReasonsForHide.Add("No list price in force for disti region (" & buyeraccount.SellerChannel.IsCloneOf.Region.Code & ")")
                    End If
                End If

            Else
                If Product.i_Variants(buyeraccount.SellerChannel.IsCloneOf).Count = 0 Then
                    ReasonsForHide.Add("Product is not in the feed (No ChannelSKU)")
                    If exitEarly And ReasonsForHide.Count > 0 Then Exit Function

                    '  Exit Function
                End If
                'End If
            End If
        End If

        If Me.Product.EOL And Not Me.Product.anyStock(buyeraccount.SellerChannel) Then
            ReasonsForHide.Add("Product is End Of Life AND not in stock")
        End If

        If exitEarly And ReasonsForHide.Count > 0 Then Exit Function

        'NB this adds to the list of reasons for hiding ...
        ReasonsForHide.AddRange(Me.AvailableInRegion(buyeraccount, path$, errorMessages))

        If exitEarly And ReasonsForHide.Count > 0 Then Exit Function

        If ReasonsForHide.Count > 0 Then Exit Function

        'IF you have a webservice then (potential) visbility is ONLY a function of 'infeed'
        If (priceconfig And 8) > 0 Then
            If Product.inFeed(buyeraccount.SellerChannel.IsCloneOf) Then Exit Function
        Else
            Dim prices As List(Of clsPrice) = Me.Product.GetPrices(buyeraccount, priceconfig, iq.AllVariants, errorMessages, False)
            If prices.Count = 0 Then
                ReasonsForHide.Add("No price for Product - Priceconfig:" & buyeraccount.SellerChannel.priceConfig)
            End If
        End If

    End Function
    ''' <summary>Checks if a branch is avbailable (by region) (ie. is not restricted for this sellerchannels region.</summary>
    ''' <returns>"" if the branch should be displayed (or a HideReaon of the branch should be supressed</returns>
    ''' <remarks>Also checks the products set of ALSOHOST attributes (which overried geographical restrictions)</remarks>
    Public Function AvailableInRegion(buyeraccount As clsAccount, path As String, ByRef errorMessages As List(Of String)) As List(Of String)

        AvailableInRegion = New List(Of String) 'this is the list of regionalisation reasons NOT to display a product
        Dim skipGeography As Boolean = False
        If Me.Product.i_Attributes_Code.ContainsKey("alsoHost") Then
            Dim j = From h In Me.Product.i_Attributes_Code("alsoHost") Where h.Translation.text(English) = buyeraccount.SellerChannel.Code
            If j IsNot Nothing Then Exit Function 'This product *is* visible becuase of AlsoHost
        End If

        Dim geoRestrictions As Boolean = False 'Are there any purely geographic restrictions (ie. non auto adds)
        For Each q In Me.Quantities.Values
            If q.NumPreInstalled = 0 Then geoRestrictions = True
        Next

        'The seller wasn't listed in AlsoHosts)
        'Quantity records should be thought of as restrictions (of minimum installed, Min Increment, Preferred increment etc.. if none is present there is no limitation !
        Dim qty As clsQuantity
        If Not geoRestrictions Then 'Me.Quantities.Count = 0 Then
            'There are *no* quantity restrictions (most branches have none)
            Exit Function
        Else
            ' If Me.Product IsNot Nothing And Me.Product.ProductType.Code = "wty" Then Stop
            'gets the most appropriate quantity record for this sellers region/country
            qty = Me.LocalisedQuantity(buyeraccount.SellerChannel.Region, path$, errorMessages)
            If qty Is Nothing Then
                'this branch has quantities - but none appropriate for this sellers region
                AvailableInRegion.Add("Product will not appear in this REGION " & buyeraccount.SellerChannel.Region.Code & " (no localised Quantity record) - " & buyeraccount.SellerChannel.Region.Displayname(English) & " Path:" & path)
                'For Each q In Me.Quantities.Values
                ' AvailableInRegion.Add("Region:" & q.Region.Name.text(English) & " (" & q.Region.Code & ")")
                ' Next
            Else
                'A minIncrement of 0 in a most appropriate localised quantity disables the product (for that region)
                If qty.MinIncrement = 0 Then
                    AvailableInRegion.Add("Product is explicitly disallowed in this region by a minIncrement of  0 at the " & qty.Region.Code & " level. " & buyeraccount.SellerChannel.Region.Displayname(English) & "Branchid:" & Me.ID & " Path:" & path)
                Else
                    Exit Function
                End If
            End If
        End If

    End Function

    Public Function GetPreInstalledRecursive(region As clsRegion, ByVal path$, ByRef errorMessages As List(Of String)) As List(Of clsQuantity)

        'Returns a dictionary of the (relative) paths of all descendant, pre-installed quantities - with quanities thereof
        'Remember a branch can be grafted in multiple places - and the quantities have a path (which must match, or be blank)
        'Each branch will carry (typically) many quanities... most of which will be irrelevant becuase their paths wont match

        ' If (iq.Cache.ContainsKey(region.ID) AndAlso iq.Cache(region.ID).ContainsKey(path$)) Then Return iq.Cache(region.ID)(path$)


        GetPreInstalledRecursive = New List(Of clsQuantity)

        Dim q As clsQuantity

        'this branch has many quanity records - find the single 'best' (geographically narrowest)
        If region Is Nothing Then
            'we'e just compiling a list of all quanities (for the PreINstalled table for debugging/product maintenance)
            'the child rbanches are grafted in many places and so carry lot of quanitites that are not relevant to this location (path)
            For Each l In Me.Quantities.Values
                If Not l.deleted Then
                    If l.Path = path Or l.Path = "" Then 'this was mysteriously commented out - nick reinsttated
                        GetPreInstalledRecursive.Add(l)
                    End If
                End If
            Next

        Else
            q = Me.LocalisedQuantity(region, path$, errorMessages) 'returns the 'best' quantity record for this user - ie, the deepest, narrowest match
            If Not q Is Nothing Then

                If LCase(path$) = LCase(q.Path) Or q.Path = "" Then '@@@
                    '     If q.NumPreInstalled > 0 Then
                    'If Not q.deleted Then
                    'WE DO want to include deleted items in the preinstalled table (otherwise we have no way to undelete them !)
                    GetPreInstalledRecursive.Add(q)
                    'End If
                    'End If
                End If
            End If
        End If


        'For Each q In Me.Quantities.Values
        'If q.Region Is region Then



        For Each child In Me.childBranches.Values
            'Dim j As List(Of clsQuantity)

            GetPreInstalledRecursive.AddRange(child.GetPreInstalledRecursive(region, path$ & "." & Trim$(CStr(child.ID)), errorMessages)) 'recursively find the child branches preinstalled options - and append them to the dictionary that is ultimately returned

            '    j = branch.GetPreInstalledRecursive(region, path$ & "." & Trim$(CStr(branch.ID)), errormessages) 'recursively find the child branches preinstalled options - and append them to the dictionary that is ultimately returned
            'For Each v In j
            ' GetPreInstalledRecursive.Add(v)
            ' Next
        Next

        'If Not iq.Cache.ContainsKey(region.ID) Then iq.Cache.Add(region.ID, New Dictionary(Of String, List(Of clsQuantity)))
        'If Not iq.Cache(region.ID).ContainsKey(path$) Then iq.Cache(region.ID).Add(path$, GetPreInstalledRecursive)

    End Function

    ''' <summary>
    ''' Returns "" if its OK to show the product (it's in focus) - otherwise return the HideReason
    ''' </summary>
    ''' <param name="foci"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function inFocus(foci As HashSet(Of String)) As List(Of String)

        inFocus = New List(Of String) 'list of reasons NOT to display

        If foci.Count = 0 Then
            Exit Function   'We're not focussing on anything (in particular)
        Else
            If Me.Product.i_Attributes_Code.ContainsKey("focus") Then
                For Each focus In Me.Product.i_Attributes_Code("focus")
                    Dim ft As String = focus.Translation.text(English)

                    If Not foci.Contains(ft) Then
                        inFocus.Add("You are not focussing on " & ft)
                    End If
                Next
            End If
        End If

        ''NB this is a list so we can look at SmartBuy AND receta (at the same time) for example
        'If Not Me.Product.i_Attributes_Code.ContainsKey("focus") Then
        '    inFocus.Add("Product does not have a focus attribute (receta etc.) - you're currenctly focusing on:" & Join(foci.ToArray, ","))
        '    Exit Function
        'End If

        'Dim j = From v In Me.Product.i_Attributes_Code("focus") Select v.Translation.text(English)
        'Dim l As List(Of String) = j.ToList

        'If l.Intersect(foci).Count > 0 Then
        '    'all good - one focus of the product matches that of the session 
        '    Exit Function
        'Else
        '    inFocus.Add("Product not in focus -  Product foci:" & Join(l.ToArray, ",") & " current foci:" & Join(foci.ToArray, ","))
        'End If
        '        End If

    End Function

    Public Function DisplayName(language As clsLanguage) As String Implements i_Editable.displayName

        Return Me.Translation.text(language)

    End Function

    'Public Function HasPriceRecursive(buyerAccount As clsAccount) As Integer


    '    'Returns 1 if any descendant branch carries a product which has a price (according to priceconfig)
    '    'returns -1 if if hits a system for which we have no price
    '    'returns 0 (or keeps processing) if there is no price

    '    'NOTE: - this stops recursing at any system for which there is no price to prevent higher level branches 
    '    '        (families, supply chains, systems) from being visible where an option shared with anouther system *does* have a price

    '    '      If Me.HasSKU = False Then Return False 'todo - remove (was needed as some placehol;ders had acquired pricing)

    '    HasPriceRecursive = False

    '    If Not Me.Product Is Nothing Then
    '        If Me.Product.GetPrices(buyerAccount, buyerAccount.SellerChannel.priceConfig, iq.AllVariants).Count > 0 Then
    '            Return 1
    '        Else
    '            'Return False 'we dont recurse through non priced items (yet)
    '            If Me.Product.isSystem Then Return -1 ' see NOTE - we need to toatally bail here (somehow)
    '        End If
    '    End If

    '    For Each child In Me.childBranches.Values

    '        Dim r As Integer = child.HasPriceRecursive(buyerAccount)
    '        If r <> 0 Then Return r

    '    Next

    'End Function


    Public Function minorKeywords(language As clsLanguage) As String

        'Minor Keywords come from the branch.product 
        minorKeywords = ""

        If Not Me.Product Is Nothing Then

            If Me.Product.i_Attributes_Code.ContainsKey("subTitle") Then
                minorKeywords &= Me.Product.i_Attributes_Code("subTitle")(0).Translation.text(language)
            End If

            If Me.Product.i_Attributes_Code.ContainsKey("desc") Then
                If Me.Product.i_Attributes_Code("desc")(0).Translation IsNot Nothing Then
                    minorKeywords &= " " & Me.Product.i_Attributes_Code("desc")(0).Translation.text(language)
                End If

            End If

            minorKeywords &= " " & Me.Product.ProductType.Code 'Allow searhing by things like SSD and HDD

        End If

    End Function

    Public Function Majorkeywords(language As clsLanguage) As String

        'Indexes the branch.translation - which it either a category/family name or a SKU
        Majorkeywords = Me.Translation.text(language)

    End Function

    Public Function BuyUI(buyeraccount As clsAccount, Path$, lid As UInt64, Optional searchResults As Boolean = False) As Panel '  , skud As Boolean, matrix As clsBranch, filter As String, sort As String) As PlaceHolder

        Dim errorMessages As List(Of String) = New List(Of String)
        'returns customer facing UI for price and stock (including flex buttons),
        ' Pricing and stock for ALL variants of a SKU

        Dim ui As Panel = New Panel


        'Print("hello")
        'ui.ID = "Prices_" & Me.Product.ID
        ui.ID = "buyUI_" & Path 'this may not be distinct enough
        If searchResults Then
            ui.CssClass = "buyUISearch"
        Else
            ui.CssClass = "buyUI"
        End If

        'ui.Attributes("style") = "" 'we have to explicitly position relative - so we can subsequently specify a left postion for child elements

        Dim prices As List(Of clsPrice) = Me.Product.GetPrices(buyeraccount, buyeraccount.SellerChannel.IsCloneOf.priceConfig, iq.AllVariants, errorMessages, True)

        If prices Is Nothing Then
            errorMessages.Add("* Missing prices")
            ui.Controls.Add(NewLit("wait"))

        Else
            Dim tb_qty As TextBox

            Dim vpanel As Panel 'a panel for each variant
            Dim vnp As Panel 'within that - a panel for each variant name
            Dim pp As Panel ' a price panel
            Dim sp As Panel
            Dim qp As Panel

            Dim vn As Integer = 0

            Dim vnl As Label
            For Each price In prices 'For this one product there can be more than one price/stock (one per variant!)

                'supress the test variants for any non admin user
                If Not price Is Nothing Then 'POAs are 'NOthings' in the list of prices
                    If price.SKUVariant.Code <> "TST" Or (price.SKUVariant.Code = "TST" And buyeraccount.HasRight("SEETEST")) Then

                        vpanel = New Panel
                        vpanel.ID = "v_" & vn & "." & Path
                        vpanel.CssClass = "buyUIvariant"
                        ' vpanel.Attributes("style") = "left:" & vn * 20 & "em;"
                        ui.Controls.Add(vpanel)

                        vn += 1
                        If prices.Count > 1 Then
                            'vnp = New Panel
                            vnl = New Label
                            'vnp.Controls.Add(vnl)
                            vpanel.Controls.Add(vnl)
                            vnl.Text = price.SKUVariant.Code         'Variant
                            vnl.BackColor = Drawing.Color.Blue
                            vnl.ForeColor = Drawing.Color.White
                            vnl.ToolTip = price.SKUVariant.displayName(buyeraccount.Language)
                        End If

                        'PRICE  - TODO - supress/display differently prices whose variants are deleted ??? makes no sense ?
                        pp = New Panel
                        sp = New Panel
                        vpanel.Controls.Add(pp)
                        If searchResults Then
                            pp.Attributes("class") = "buyUIprSearch" 'float:left
                            sp.CssClass = "buyUIstSearch"
                        Else
                            pp.Attributes("class") = "buyUIprice" 'float:left
                            sp.CssClass = "buyUIstock"
                        End If


                        'returns a DIV with the ID P_Product.id_Price.ID,  containing a label with tooltip info - 
                        'NB: = it has the cssClass Refresh, so that it can be updated after a webservie call (see FillPrices)
                        pp.Controls.Add(price.Ui(buyeraccount, 1, lid))

                        'STOCK - we show the stock of the variant we showed the price of - note buyUi may return many panels (one for each variant)

                        vpanel.Controls.Add(sp)
                        If price.SKUVariant Is Nothing Then
                            errorMessages.Add("* Price " & price.ID & " SkuVariant was nothing")
                        Else
                            If price.SKUVariant.Product Is Nothing Then
                                errorMessages.Add("* Price " & price.ID & " SKUVariants product was nothing")
                            Else
                                If price.SKUVariant.shipments.Count = 0 Then
                                    'dont' show any indication of stock is these are no shipments at all

                                Else
                                    Dim sl As New Label


                                    sp.Controls.Add(price.SKUVariant.StockUI(1, String.Empty, buyeraccount.Language, buyeraccount.SellerChannel)) 'returns a DIV with the ID S_ Price.ID,  containing a label with tooltip info
                                    If Not buyeraccount.SellerChannel.BinaryStock Then
                                        sl.Text = "&nbsp; " & Xlt("in stock", buyeraccount.Language)
                                    Else
                                        sl.Text = "&nbsp; "
                                    End If
                                    sp.Controls.Add(sl)
                                End If
                            End If
                        End If

                        Dim quoteLocked As Boolean = False
                        If iq.sesh(lid, "QuoteLocked") IsNot Nothing Then
                            quoteLocked = CBool(iq.sesh(lid, "QuoteLocked"))
                        End If

                        If Not price.SKUVariant.Deleted Then

                            ' If there is a current quote, work out whether it's HPI or HPE
                            Dim quote As clsQuote
                            Dim quoteSplit = Manufacturer.Unknown
                            If iq.sesh(lid, "QuoteID") IsNot Nothing Then
                                If iq.sesh(lid, "AgentAccount") IsNot Nothing Then
                                    Dim agentAccount As clsAccount = CType(iq.sesh(lid, "AgentAccount"), clsAccount)
                                    quote = agentAccount.Quotes(iq.sesh(lid, "QuoteID"))
                                    quoteSplit = quote.QuoteSplit
                                End If
                            End If

                            ' Work out whether adding is enabled according to the HPE/HPI split
                            Dim addEnabled As Boolean = True
                            If Product.isSystem(Path) Then
                                If Not quoteSplit = Manufacturer.Unknown Then
                                    addEnabled = (quoteSplit = Product.Manufacturer)
                                End If
                            End If

                            ' Set up the message to display if the user attempts to create a mixed quote
                            Dim splitMessage As String = String.Empty
                            If Not addEnabled Then
                                splitMessage = GetSplitMessage(quoteSplit, buyeraccount.Language)
                            End If

                            qp = New Panel
                            qp.Attributes("class") = "buyUIqty"
                            vpanel.Controls.Add(qp)
                            tb_qty = New TextBox
                            tb_qty.ID = "qtytxt." & Path$
                            If addEnabled Then
                                tb_qty.CssClass = "qty UI"
                                tb_qty.Attributes.Add("onmousedown", "burstBubble(event);")
                            Else
                                tb_qty.CssClass = "qtyDisabled UI"
                                tb_qty.ReadOnly = True
                                tb_qty.Attributes.Add("onmousedown", String.Format("burstBubble(event); displayAddMsg('{0}', '{1}');", tb_qty.ID, splitMessage))
                            End If
                            qp.Controls.Add(tb_qty)

                            qp.Controls.Add(TreeAddButton(tb_qty, Path$, Me, price.SKUVariant, buyeraccount.Language, addEnabled, splitMessage))

                            If UserIsAdmin(lid) Then
                                Dim lt As Literal = FunctionButton(Path, price.SKUVariant.ID, "deleteVariant&into=tree", "DEL", "Removes this variant from the feed" & vbCrLf & "(until it`s next loaded/refreshed)" & vbCrLf & "Generally not something you want to be doing!'")
                                qp.Controls.Add(lt)
                            End If
                        End If
                    End If
                End If
            Next

            OutputErrors(ui.Controls, errorMessages, lid)

            Return ui

        End If

    End Function


    Public Function ChildNamed(nm$) As clsBranch

        'NOTE: - its generally a very bad idea to navigate banches by name - as their names may change
        'This is used in CreateCarePacks (and called from ensurePath)

        ChildNamed = Nothing
        For Each child In Me.childBranches.Values
            If child.Translation.text(English) = nm$ Then
                Return child
            End If
        Next

    End Function

    Public Function SKU() As String

        'could be speeded up by pre-loading into a string property

        SKU = ""
        If Not Me.Product Is Nothing Then
            If Me.Product.SKU <> "" Then
                SKU$ = Me.Product.SKU
            End If
        End If

    End Function



    'Public ReadOnly Property Name As String
    '    Get
    '        If Me.Product IsNot Nothing Then
    '            If Me.Product.i_Attributes_Code.ContainsKey("~ame") Then
    '                Name = Me.Product.i_Attributes_Code("Name")(0).Translation.text(s_lang)
    '            Else
    '                Name = "branches product has no name"
    '            End If
    '        End If
    '    End Get
    'End Property


    Public Function SlotsTable(bi As clsBranchInfo) As Table

        'returns HTML UI showing slots
        Dim errormessages As List(Of String) = New List(Of String)()
        'Got a quote?
        Dim quote As clsQuote
        If iq.sesh(bi.lid, "QuoteID") IsNot Nothing Then
            quote = bi.agentAccount.Quotes(iq.sesh(bi.lid, "QuoteID"))
            If quote.RootItem.Children.Count = 0 Then quote.LoadItems(errormessages)
        End If

        'Show any slots
        Dim atable As New Table
        atable.CssClass = "adminTable"

        If Me.slots.Values.Count > 0 Then

            Dim help$ = "Broad slot type (many validations are only performed agains this)|"
            help$ &= "Narrow slot type (some PCI validations are performed against this)|"
            help$ &= "description|"
            help$ &= "Number of slots (+given/-taken)|"
            help$ &= "specific slot number (used for *some* options which only go in specific slots in a given system)|"
            help$ &= "path at which this slot is 'active' (or empty if it applies everywhere)|"
            help$ &= "Notes (to user)|"
            help$ &= "User MUST fill this many of this slot|"
            help$ &= "You may 'soft delete' slots (and undelete them if you change your mind)"

            Dim thr As TableHeaderRow = MakeTHR("Major,Minor,Description,NumSlots,SlotNum,path,notes,RequiredFill,Del", help, "")
            atable.Controls.Add(thr)


            Dim tr As TableRow
            Dim tc As TableCell
            For Each slot In Me.slots.Values

                tr = New TableRow

                atable.Rows.Add(tr)
                If slot.deleted Then tr.CssClass &= " deletedRow"
                If slot.path <> "" And Not bi.path.Contains(slot.path) Then tr.Attributes.Add("style", "text-decoration:line-through;")

                tc = New TableCell
                tc.Text = (slot.Type.MajorCode)
                tr.Controls.Add(tc)

                tc = New TableCell
                tc.Text = (slot.Type.MinorCode)
                tr.Controls.Add(tc)

                tc = New TableCell
                tc.Text = slot.Type.Translation.text(s_lang)
                tr.Controls.Add(tc)

                tc = New TableCell
                tc.Text = CStr(slot.numSlots)
                tr.Controls.Add(tc)

                tc = New TableCell
                If slot.slotNum Is Nothing Then
                    tc.Text = "Undefined"
                Else
                    tc.Text = slot.slotNum.sqlvalue
                End If
                tr.Controls.Add(tc)

                tc = New TableCell

                If slot.path = bi.path Then
                    tc.Text = "Only here"
                ElseIf slot.path = "" Then
                    tc.Text = "Everywhere"
                Else
                    tc.Text = "Not here"
                End If
                tc.ToolTip = slot.path
                tr.Controls.Add(tc)

                tc = New TableCell
                If Not slot.notes Is Nothing Then
                    tc.Text = slot.notes.text(s_lang)
                End If
                tr.Controls.Add(tc)

                tc = New TableCell
                tc.Text = CStr(slot.requiredFill)

                tr.Controls.Add(tc)

                'If quote IsNot Nothing Then
                '    tc = New TableCell
                '    If quote.RootItem IsNot Nothing AndAlso quote.RootItem.Descendants IsNot Nothing Then
                '        For Each quoteItem In quote.RootItem.Descendants
                '            If quoteItem.dicslots IsNot Nothing Then
                '                If quoteItem.dicslots.ContainsKey(slot.NonStrictType) Then
                '                    tc.Text = quoteItem.dicslots(slot.NonStrictType).taken
                '                    Exit For
                '                End If
                '            End If
                '        Next
                '    End If
                '    tr.Controls.Add(tc)
                'End If

                tc = New TableCell

                If Not slot.deleted Then
                    Dim lt As Literal = FunctionButton(bi.path, slot.ID, "deleteSlot", "DEL", "Delete this slot")
                    tc.Controls.Add(lt)
                Else
                    Dim lt As Literal = FunctionButton(bi.path, slot.ID, "unDeleteSlot", "UNDEL", "Un-Delete this slot")
                    tc.Controls.Add(lt)
                End If


                tr.Controls.Add(tc)


            Next

            tr = New TableRow
            atable.Rows.Add(tr)
            tc = New TableCell
            tc.CssClass = "slotCell"
            tr.Controls.Add(tc)

            tc.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit these slots", bi.agentAccount.Language), _
                "window.open('edit.aspx?path=Branches(" & Me.ID & ").slots&TreePath=" & bi.path & "&lid=" & bi.lid.ToString & "');return(false);", _
                "", "width:25px;height:25px;", bi.buyerAccount.Language))


        End If
        Return atable

    End Function

    'ByVal path As List(Of Integer)
    Friend Sub Score(branchScores As Dictionary(Of Integer, clsKwScore), ByRef path() As Integer, _
                     ByVal majorMatchedBits As Integer, ByVal minorMatchedbits As Integer, _
                     ByVal majorMatchCount As Integer, ByVal minorMatchCount As Integer, _
                     ByRef worst As Integer, ByVal depth As Integer, _
                     ByRef PathScores As Dictionary(Of String, Integer), _
                     buyeraccount As clsAccount, searchType As String, _
                     crossSystems As Boolean, lid As UInt64, searchid As Integer, _
                     ByRef abandon As Boolean, ByRef cyclecount As Integer, ByVal pth$)

        If depth > 14 Then Exit Sub 'Dim l$ = PathName(pth$) : Stop

        pth$ &= Me.ID & "."


        'Each branch has been (pre) tested to see if it contains each of up to 32 keywords, 
        'Each of which is represented by a single bit in the 'branchscores.MatchedBits property
        'BranchScores is an INPUT to this fuction, and never changes 
        'PathScore is populated (through recursion) and contains a Path>Score

        'MinorMatches are in descriptions - Majors are in Names


        'matchedBits is the 'working variable' and has its bits set for the 'current' position in the tree
        'we keep track of the recursion depth and use an BYREF array of integers to track path to this point

        'This is a speedup - but at the cost of possbile 
        ' If PathScores.Count > 1000 Then Exit Sub

        path(depth) = Me.ID
        'matchedBits = matchedBits Or Me.Matches 'Bitwise logical OR mask with the matches of the branch
        ' points = points + Me.Points

        If abandon Then Exit Sub

        Dim bn$ = Me.Translation.text(English).ToLower
        If bn.Contains("accessories and") Then
            Exit Sub
            'Beep()
        End If

        cyclecount += 1
        If cyclecount = 100 Then
            Dim ssid As Integer = iq.sesh(lid, "searchID")
            If ssid <> searchid Then 'abandon
                abandon = True
                Exit Sub
            End If
            cyclecount = 0
        End If

        If branchScores.ContainsKey(Me.ID) Then

            'Bitwise logical OR mask with the all the matches of the branch
            majorMatchedBits = majorMatchedBits Or branchScores(Me.ID).majorMatchBits
            minorMatchedbits = minorMatchedbits Or branchScores(Me.ID).minorMatchbits

            majorMatchCount += branchScores(Me.ID).MajorMatchCount 'sum the points (total number of matches) of each branch
            minorMatchCount += branchScores(Me.ID).MinorMatchCount 'sum the points (total number of matches) of each branch

            'matechedbits carries matches of frags 'down' to this point in the tree - so *everything* under a 'SSD' category for example 
            'Scores a 1 for SSD (even if it itself doesnt have SSD in its (own) .Keyswords)

            ' Dim bc As Integer
            ' bc = BitCount(majorMatchedBits)  'this is the matched bits down to this point in the tree
            Dim score As Integer
            score = BitCount(majorMatchedBits) * 200 \ (depth + 1) + majorMatchCount * 3
            score += BitCount(minorMatchedbits) * 100 \ (depth + 1) + minorMatchCount

            'If (score >= worst) Or (score = worst And results.Count < 20) Then
            If score > 0 Then
                Dim abc As String = ""
            End If

            'worst' is initially 0 so only things with *some* score will get added

            If score > 0 And score >= worst Or PathScores.Count < 1000 Then

                Dim rp$ = ""
                For i = 1 To depth
                    rp$ &= "." & path(i).ToString
                Next

                PathScores.Add(rp$, score)

                Dim topPathScores = From r In PathScores Order By r.Value Descending
                If PathScores.Count > 1024 Then '64 Then  'if we get to more than 64 results .. keep only the 32 'best'

                    Dim sorted As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
                    For Each kvp In topPathScores.Take(32)
                        sorted.Add(kvp.Key, kvp.Value)  'values are scores
                    Next
                    worst = sorted.Values.Last 'the worst of the current scorers (futures scores must be better than this to get added)
                    PathScores.Clear()
                    PathScores = sorted
                End If
            End If
        Else
            'many branches are not in branchscores (for example hidden chassis branches)
        End If


        '@@@ - systems only search (don't recurse through systems)
        If Not crossSystems Then
            If Me.Product IsNot Nothing AndAlso Me.Product.isSystem Then Exit Sub
        End If

        'Branches are unfiltered at this point - there's a very real danger that none of our 64 results are in the distis feed
        For Each branch In Me.childBranches.Values
            If branch Is Me Then Stop 'circular refernece - very bad
            Dim include As Boolean = True
            If branch.Product IsNot Nothing Then

                If branch.Product.isSystem Then 'If the system is not in the feed we do not recurse into the otpions

                    If searchType = "priced" Then

                        If (Not branch.Product.inFeed(buyeraccount.SellerChannel)) AndAlso (Not branch.Product.HasListPrice(buyeraccount)) Then
                            include = False
                        End If

                    End If
                End If
            End If

            If include Then

                Dim dnr As Boolean = False
                For i = 1 To depth + 1
                    If path(i) = branch.ID Then
                        Dim l$ = PathName(pth$ & branch.ID)  'Ut oh - there's a loop in the tree
                        dnr = True 'Don not recurse
                    End If
                Next

                If Not dnr Then branch.Score(branchScores, path, majorMatchedBits, minorMatchedbits, majorMatchCount, minorMatchCount, worst, depth + 1, PathScores, buyeraccount, searchType, crossSystems, lid, searchid, abandon, cyclecount, pth)

            End If
            If abandon Then Exit For
        Next

    End Sub

    ''' <summary>Grafts the supplied source branch onto this instance (the target branch) (making it a child thereof)</summary>
    ''' <param name="sourceBranch">The branch being grafted (stuck on)</param>
    ''' <param name="Source">Audit trail/data source text (Who made this graft)</param>
    ''' <param name="writecache"></param>
    ''' <returns></returns>
    ''' <remarks>Does *not* make this branch the parent of the source branch..(otherwise a branch would need multiple parents..The parent chain cannot be navigated through grafts )</remarks>
    Public Function Graft(ByVal sourceBranch As clsBranch, Source As String, path As String, ByRef errorMessages As List(Of String), Optional writecache As DataTable = Nothing) As Boolean

        'make sure the target is not already a descendant of the source  (circular reference)

        Graft = False

        Dim descendants As List(Of clsBranch)
        If writecache Is Nothing Then 'For imports, where we use bulk write, Don't check for ciruclar references (becuase it's expensive)
            descendants = sourceBranch.Descendants
        Else
            descendants = New List(Of clsBranch)
        End If

        'if any of the target branches children have a SKU and the source branch doesn't
        ' If childBranches.Where(Function(a) a.Value.HasSKU() <> sourceBranch.HasSKU()).Count() > 0 Then
        ' errorMessages.Add("!Mixed Branch Error - You can't mix the type, SKU and Category under this branch")
        ' Exit Function
        ' End If

        If descendants.Contains(Me) Then
            errorMessages.Add("!Circular Reference Error - You can't graft onto a branch that is contained within the branch you are grafting (or very bad things would happen)")
            Exit Function
        Else

            If path$ = "" Then 'we allow multiple grafts of the same source branch IF a path is specified
                If Me.childBranches.Values.Contains(sourceBranch) Then
                    errorMessages.Add("!Error - The branch you're trying to graft is already a child of this branch - you probably want to make a copy instead")
                    Exit Function
                End If
            End If


            If Not sourceBranch.AllParents.ContainsKey(Me.ID) Then sourceBranch.AllParents.Add(Me.ID, Me)
            Dim blnAddRecords As Boolean = False

            If Not Me.childBranches.ContainsKey(sourceBranch.ID) Then
                Me.childBranches.Add(sourceBranch.ID, sourceBranch)
                Me.HasGrafts = True
                blnAddRecords = True
                If path <> "" Then
                    sourceBranch.GraftedOnAt.Add(path)
                End If
            ElseIf path <> "" And Not sourceBranch.GraftedOnAt.Contains(path) Then
                Me.HasGrafts = True
                sourceBranch.GraftedOnAt.Add(path)
                blnAddRecords = True
            End If
            If blnAddRecords Then
                If writecache Is Nothing Then
                    Dim sql$
                    sql$ = "INSERT INTO [graft] (fk_branch_id_target,fk_branch_id_source,created,source,path) VALUES (" & Me.ID & "," & sourceBranch.ID & ",getdate()," & da.SqlEncode(Source) & "," & da.SqlEncode(path) & ");"
                    da.DBExecutesql(sql, True)  'return the ID of the graft record
                Else

                    Dim row As System.Data.DataRow
                    row = writecache.NewRow()
                    row("FK_Branch_ID_Target") = Me.ID
                    row("FK_Branch_ID_Source") = sourceBranch.ID
                    row("Path") = path

                    row("Created") = Now
                    row("Source") = Source
                    row("marginsystems") = "1"
                    row("marginoptions") = "1"

                    writecache.Rows.Add(row)
                End If
            End If

        End If

        Return True

    End Function

    Public Function DeleteGraftedOnBranch(sourceId As Integer) As Boolean

        da.DBExecutesql(String.Format("DELETE FROM [graft] WHERE [FK_Branch_ID_Source] = {0} AND [FK_Branch_ID_Target] = {1};", sourceId, Me.ID))

    End Function


    Public Sub IndexPaths(ByRef con As SqlClient.SqlConnection, ByVal path$, ByVal pathname$, ByRef dt As DataTable, ByRef segcache As DataTable, depth As Integer, ByRef cc As Integer)

        'Recurses all branches, adding the product ID and path - populates the datatable DT (ready for a fast bulk insert to SQL via an SP)
        'creates the Path table - allowing us to quickly find every occurence of a product in the tree

        path$ &= "." & Trim$(CStr(Me.ID))

        'do not recurse pruned branches !
        'If Not iq.Prunes.ContainsKey(path$) Then

        Dim row As DataRow
        'pathname$ &= " / " & Me.displayname


        'If Not Me.Product Is Nothing Then 'Not all branches carry a product
        row = dt.NewRow()
        row("Path") = path
        If Me.Product Is Nothing Then
            row("fk_product_id") = DBNull.Value
        Else
            row("fk_product_id") = Me.Product.ID
        End If

        cc += 1 'this will tally with the path ID
        row("cc") = cc

        dt.Rows.Add(row)
        If dt.Rows.Count Mod 50000 = 0 Then
            Beep()
            da.BulkWrite(con, segcache, "PathSegment")
            segcache.Rows.Clear()

            da.BulkWrite(con, dt, "Path")
            dt.Rows.Clear()
            Beep()
        End If


        Dim j() As String = Split(path$, ".")

        'the 0th element contained the literal 'tree'
        For i = 1 To UBound(j)
            row = segcache.NewRow
            row("fk_path_id") = cc
            row("fk_branch_id") = j(i)
            row("fk_translation_key") = iq.Branches(CInt(j(i))).Translation.Key
            row("order") = i
            segcache.Rows.Add(row)
        Next

        'add any attributes of the product we want to index

        'however we recurse them all
        For Each child In Me.childBranches.Values
            child.IndexPaths(con, path$, pathname$, dt, segcache, depth + 1, cc)
        Next

        'Else

        'End If

    End Sub

    Public Function findSystemBranches(path$) As Dictionary(Of String, clsBranch)

        findSystemBranches = New Dictionary(Of String, clsBranch)

        If Not Me.Product Is Nothing Then
            If Me.Product.isSystem Then findSystemBranches.Add(path$, Me) : Exit Function
        End If
        For Each child In Me.childBranches.Values
            AppendDic(CType(findSystemBranches, Dictionary(Of String, clsBranch)), child.findSystemBranches(path$ & "." & Trim$(CStr(child.ID))))
        Next

    End Function

    Public Function findFamilyBranches(Path$) As Dictionary(Of String, Pair) '(Of String, clsBranch))

        'Only used during the power sizing import - generally NOT robust.
        'return a dictionary of SysFamily codes to a weakly typed pair of Path,Branch

        findFamilyBranches = New Dictionary(Of String, Pair)(StringComparer.CurrentCultureIgnoreCase)


        If Me.Product IsNot Nothing Then
            If Me.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
                Dim famcode As String = Me.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English)
                findFamilyBranches.Add(famcode, New Pair(Path, Me))
            End If

            Exit Function
        End If

        For Each child In Me.childBranches.Values
            AppendDic(CType(findFamilyBranches, Dictionary(Of String, Pair)), child.findFamilyBranches(Path$ & "." & Trim$(CStr(child.ID))))
        Next

    End Function


    ''' <returns>The number of slots (on balance - ie. Gives-Takes) of a specified slotType</returns>
    Public Function slotsGiven(path$, slotType As clsSlotType) As Integer

        'on balance!
        slotsGiven = 0

        If Me.HasSKU Then 'Only things with a SKU can give slots (otherwise the virtual chassis gets suggested to solve all sorts of slot shortfalls! )
            Dim min As String = ""
            If iq.StrictSlotValidation Then min = slotType.MinorCode

            Dim gives As Integer = 0
            Dim takes As Integer = 0

            'This is to 'fake' a dictionary with less strict slot types using NonStrictType from the slot, logic then remains the same so it can be switched on / off easily
            Dim ist = If(iq.StrictSlotValidation, Me.i_Slots, Me.slots.Values.ToList().GroupBy(Function(iss) iss.NonStrictCompoundKey).ToDictionary(Function(dk) dk.Key, Function(dk) New clsSlot() With {.Type = dk.First.NonStrictType, .numSlots = dk.Sum(Function(dkchild) dkchild.numSlots)}))

            If ist.ContainsKey(slotType.MajorCode & "_" & min & "_" & path & "_1_null") Then
                gives = ist(slotType.MajorCode & "_" & min & "_" & path & "_1_null").numSlots ''find the 'gives ' slots -SPECIFICALY at this path (there can only be one of each type)
            ElseIf ist.ContainsKey(slotType.MajorCode & "_" & min & "__1_null") Then  'look for Globally scoped gives slots (without a path)
                gives = ist(slotType.MajorCode & "_" & min & "__1_null").numSlots
            End If

            If ist.ContainsKey(slotType.MajorCode & "_" & min & "_" & path & "_-1_null") Then
                takes = ist(slotType.MajorCode & "_" & min & "_" & path & "_-1_null").numSlots ''find the 'takes' slots -SPECIFICALY at this path (there can only be one of each type)
            ElseIf ist.ContainsKey(slotType.MajorCode & "_" & min & "__-1_null") Then  'look for Globally scoped takes slots (without a path)
                takes = ist(slotType.MajorCode & "_" & min & "__-1_null").numSlots
            End If

            slotsGiven = gives + takes  'NB: Takes are alread NEGATIVE so we ADD them (to find the number of slots given on balance)

        End If

    End Function

    Public Function findSlotGivers(path$, slotType As clsSlotType, Optional include As Boolean = True) As Dictionary(Of String, Integer)

        'recusively builds a dictionary of path>NumSlots(given) for those systems/options giving slots of the specified type (the branch can be determined easiy from the last segment of the path)
        'Include' is used to supress the slot of the system unit itself (so we can easily find options that give slots - otherwsie it suggests we buy another system unit when we're out of slots !

        'TODO - we could build a 3d dictionary on the system branch when it's added to the basket of Path>SlotType>NumberGiven - which would allow faster resolution of slot overflows

        findSlotGivers = New Dictionary(Of String, Integer)
        If include Then
            Dim slotsGiven As Integer = Me.slotsGiven(path$, slotType)
            If slotsGiven > 0 Then
                findSlotGivers.Add(path, slotsGiven)
            End If
        End If

        'and recurse
        For Each child In Me.childBranches.Values.ToArray
            AppendDic(CType(findSlotGivers, Dictionary(Of String, Integer)), child.findSlotGivers(path$ & "." & Trim$(CStr(child.ID)), slotType))
        Next

    End Function


    Public Function findSlotTakers(path$, slotType As clsSlotType, Optional include As Boolean = True) As Dictionary(Of String, Integer)

        'recusively builds a dictionary of path>NumSlots(given) for those systems/options taking slots of the specified type (the branch can be determined easiy from the last segment of the path)
        'Include' is used to supress the slot of the system unit itself (so we can easily find options that give slots - otherwsie it suggests we buy another system unit when we're out of slots !

        'TODO - we could build a 3d dictionary on the system branch when it's added to the basket of Path>SlotType>NumberGiven - which would allow faster resolution of slot overflows

        findSlotTakers = New Dictionary(Of String, Integer)
        If include Then
            Dim slotsTaken As Integer = Me.slotsGiven(path$, slotType)
            If slotsTaken < 0 Then
                findSlotTakers.Add(path, slotsTaken)
            End If
        End If

        'and recurse
        For Each child In Me.childBranches.Values
            AppendDic(CType(findSlotTakers, Dictionary(Of String, Integer)), child.findSlotTakers(path$ & "." & Trim$(CStr(child.ID)), slotType))
        Next

    End Function

    ''' <summary>Builds a dictionary of the path>branch of all the branches under this one which carry the specified product.</summary>
    ''' <remarks>call it (for example) on the root branch to find all the locations of a system, or on a system branch to find the location(s) of an option</remarks>
    Public Function findProductBranches(path$, sellerchannel As clsChannel, ProductToFind As clsProduct, crossSystems As Boolean, ByRef fruitlessGrafts As HashSet(Of clsBranch), ExitOnFound As Boolean) As Dictionary(Of String, clsBranch)

        'Note: a HashSet is very like a list - only it is MUCH faster (binary chop vs linear lookup)
        'hashsets only contain unique values !

        'Pmark("findProductBranches")
        'CheckedBranches contains a built list of the branches (we have checked) under which we *know* the product *doesn't* appear (this stops us from recursing grafts and provides a tenfold speed up)

        findProductBranches = New Dictionary(Of String, clsBranch) 'this is the return value of the function
        If Me.PruneInForce(path, sellerchannel) = 0 Then

            If Me.Product Is ProductToFind Then
                findProductBranches.Add(path$, Me)
            End If

            Dim keepGoing As Boolean = True
            If Not Me.Product Is Nothing Then
                If Me.Product.isSystem And Not crossSystems Then keepGoing = False
            End If

            If keepGoing Then
                For Each child In Me.childBranches.Values
                    '    If InStr(path$ & ".", "." & Trim(child.ID) & ".") Then Stop 'Circular reference (this branch already appeared on the path) - todo remove for spped
                    If Not fruitlessGrafts.Contains(child) Then 'did we already (fruitlessly) check this (grafted) branch (it's grafted in many places - but its children (notwithstanding prunes) are always the same)
                        Dim locations As Dictionary(Of String, clsBranch) = child.findProductBranches(path$ & "." & Trim$(CStr(child.ID)), sellerchannel, ProductToFind, crossSystems, fruitlessGrafts, ExitOnFound) 'recurse
                        If locations.Count = 0 Then
                            If child.HasGrafts Then fruitlessGrafts.Add(child) 'Checked branches contains a list of the branches under which we *know* the product *doesn't* appear
                        Else
                            AppendDic(CType(findProductBranches, Dictionary(Of String, clsBranch)), locations)
                            If ExitOnFound Then Exit For
                        End If
                    End If
                Next
            End If
        End If
        ' Pacc("findProductBranches")

    End Function

    Public Class clsSKUBranchPathIndexEntry
        Public FirstPath As String
        Public BranchId As Int32
        Public SKU As String
    End Class

    ''' <summary>Builds a dictionary of the path>branch of all the branches under this one which carry the specified product.</summary>
    ''' <remarks>call it (for example) on the root branch to find all the locations of a system, or on a system branch to find the location(s) of an option</remarks>
    Public Sub indexProductBranchesByPath(path As String, crossSystems As Boolean, ByRef lb As Dictionary(Of Int32, String))
        If Me.ID = 10093 Then Exit Sub 'Remove this, its to discount the accessories branch..
        If Me.ID = 10443 Then
            Dim d = 9
        End If
        If Me.Product IsNot Nothing AndAlso lb.ContainsKey(Me.ID) Then
            lb(Me.ID) = path
            Exit Sub
        End If

        For Each c In Me.childBranches.Values
            c.indexProductBranchesByPath(path & "." & Me.ID.ToString, True, lb)
        Next

    End Sub

    Function FindBranchByNameBelow(name As String, fioPath As String, appendPath As Boolean, stopAtDepth As Int32, Optional ByRef outPath As String = Nothing)
        If appendPath Then fioPath = fioPath & "." & Me.ID
        If stopAtDepth <> 0 AndAlso fioPath.Split(".").Length > stopAtDepth Then Return Nothing
        If Me.Translation IsNot Nothing AndAlso Me.Translation.text(English) = name Then outPath = fioPath : Return Me

        For Each child In Me.childBranches.Values
            Dim p = child.FindBranchByNameBelow(name, fioPath, True, stopAtDepth, outPath)
            If Not p Is Nothing Then
                If outPath IsNot Nothing Then outPath = outPath
                Return p
            End If
        Next
        Return Nothing
    End Function

    'fammajor ..


    Sub FindFamilyBranchesBelow(Path As String, stopAtDepth As Int32, ByRef dic As Dictionary(Of String, String))

        Dim retval As List(Of String) = New List(Of String)

        ' If appendPath Then Path = Path & "." & Me.ID
        '  Path = Path & "." & Me.ID
        If Path.Split(".").Length > stopAtDepth Then Exit Sub
        If Me.Product IsNot Nothing AndAlso Me.Product.i_Attributes_Code.ContainsKey("FamMajor") Then

            'If Me.Product.i_Attributes_Code("FamMajor").Count > 1 Then Stop

            Dim fn$ = Me.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English)
            If dic.ContainsKey(fn$) Then Exit Sub
            dic.Add(fn$, Path)
            'If Me.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English).ToLower = famname.ToLower Then
            ' retval.Add(Path)
            'End If
        End If

        For Each child In Me.childBranches.Values
            child.FindFamilyBranchesBelow(Path & "." & child.ID, stopAtDepth, dic)

        Next

    End Sub


    Public Sub findChildBySKU(ByVal path$, ByVal pathname$, ByRef branch As clsBranch, ByVal sku As String, language As clsLanguage)

        '(Slow (sepecially if context is unknown) ..   for a faster version which finds all occurences - see FindSkuPaths (which uses the [SKUIndex], generated by indexSKUs()
        'Recurses down the branches until it encounters a branch with a product with a SKU = sku
        'Adds the branch ID's to the path$ - such that the final result path$ is correct for the branch located

        '    If Not iq.Prunes.ContainsKey(path$) Then 'dont recurse pruned branches
        If Me.Product IsNot Nothing Then
            path$ &= "." & Trim$(CStr(Me.ID))
            pathname$ &= " / " & Me.DisplayName(language)
            Dim mysku As String

            With Me.Product
                If .SKU <> "" Then
                    mysku = .SKU
                    If mysku = sku Then
                        branch = Me
                        Exit Sub
                    End If
                End If
            End With
        End If
        For Each child In Me.childBranches.Values
            child.findChildBySKU(path$, pathname$, branch, sku, language)
            If Not branch Is Nothing Then Exit Sub 'found somehting - climb back out
        Next
        '        End If


    End Sub

    Public Function findChildByProductType(ByVal address As String, producttype As clsProductType, account As clsAccount, foci As HashSet(Of String)) As clsBranch
        Dim errormessages = New List(Of String)
        If Me.Product IsNot Nothing AndAlso Not Me.Product.SKU.StartsWith("###") AndAlso Me.Product.ProductType Is producttype Then Return Me

        For Each child In Me.childBranches.Values
            If child.ReasonsForHide(account, foci, address, account.SellerChannel.priceConfig, False, errormessages).Count = 0 Then
                Dim res = child.findChildByProductType(address & "." & child.ID, producttype, account, foci)
                If res IsNot Nothing Then
                    Return res : Exit Function
                End If
            End If
        Next
        Return Nothing
    End Function


    Public Function FastfindChildBySKU2(ByVal findsku As String, segs() As String, ByVal j As Integer) As Integer

        '(Slow (sepecially if context is unknown) ..   for a faster version which finds all occurences - see FindSkuPaths (which uses the [SKUIndex], generated by indexSKUs()
        'Recurses down the branches until it encounters a branch with a product with a SKU = sku
        'Adds the branch ID's to the path$ - such that the final result path$ is correct for the branch located

        '  If Me.GraftedOnAt.Count > 0 AndAlso Not Me.GraftedOnAt.Contains(address) Then Return Nothing


        FastfindChildBySKU2 = Nothing

        ' If Not iq.Prunes.ContainsKey(address$) Then 'dont recurse pruned branches

        Dim mysku As String

        If Not Me.Product Is Nothing Then  'Some placeholder branches (families, supply chains etc - have no product.. so we recurse on down)
            With Me.Product
                If .SKU = findsku Then

                    Return j
                    Exit Function

                End If
            End With
            If Me.Product.isOption Then Exit Function 'we found an option - but it's not 'the one' - exit -so as not to recurse through tape drives etc
        End If

        Dim result As Integer
        If j > 20 Then Stop
        For Each child In Me.childBranches.Values
            segs(j) = child.ID
            result = child.FastfindChildBySKU2(findsku, segs, j + 1)
            If result <> 0 Then
                Return result : Exit Function 'found something - climb back out
            End If
        Next

    End Function


    Public Function findChildBySKU2(ByVal address As String, ByVal findsku As String, ByRef resultpath$, Optional crossystems As Boolean = True) As clsBranch

        '(Slow (sepecially if context is unknown) ..   for a faster version which finds all occurences - see FindSkuPaths (which uses the [SKUIndex], generated by indexSKUs()
        'Recurses down the branches until it encounters a branch with a product with a SKU = sku
        'Adds the branch ID's to the path$ - such that the final result path$ is correct for the branch located

        '  If Me.GraftedOnAt.Count > 0 AndAlso Not Me.GraftedOnAt.Contains(address) Then Return Nothing


        findChildBySKU2 = Nothing

        ' If Not iq.Prunes.ContainsKey(address$) Then 'dont recurse pruned branches

        Dim mysku As String

        If Not Me.Product Is Nothing Then  'Some placeholder branches (families, supply chains etc - have no product.. so we recurse on down)
            With Me.Product
                If .SKU = findsku Then
                    resultpath$ = address
                    Return Me
                    Exit Function

                End If

                If .isSystem And Not crossystems Then Exit Function 'early exit/speedup
            End With
        End If


        Dim result As clsBranch

        For Each child In Me.childBranches.Values

            result = child.findChildBySKU2(address$ & "." & Trim$(CStr(child.ID)), findsku, resultpath$)
            If Not result Is Nothing Then
                Return result : Exit Function 'found something - climb back out
            End If
        Next

    End Function

    ''' <summary>Returns a list of paths of all the higest level branches that can be pruned whilst the specified those branches to keep</summary>
    ''' <param name="Path">The path of the branch you're starting at</param>
    ''' <remarks>Efficienlty prunes as 'far back' as possible - maintainting only the </remarks>
    Public Function SeverePrune(Path$, Keep As Dictionary(Of String, clsBranch)) As List(Of String)

        SeverePrune = New List(Of String)

        Dim pd As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)
        If Me.childBranches.Count > 0 Then pd = Me.PathedDescendants(Path$) 'Get a dictioanry of Path>Branch for ALL by descendants SLOW
        If pd.Intersect(Keep).Count = 0 Then  'does is it contain any of the branches we want to keep
            If Me.Product IsNot Nothing Then
                If Me.Product.isSystem Then Stop 'A receta system with no (receta) options ??
            End If
            SeverePrune.Add(Path) 'nope - prune it and stop recursing
        Else
            For Each child In Me.childBranches.Values
                SeverePrune.AddRange(child.SeverePrune(Path$ & "." & Trim(CStr(child.ID)), Keep))  'Recurse
            Next
        End If

    End Function


    Public Function PathedDescendants(Path$) As Dictionary(Of String, clsBranch)

        PathedDescendants = New Dictionary(Of String, clsBranch)
        PathedDescendants.Add(Path$, Me)

        For Each child In Me.childBranches.Values
            If child.childBranches.Count > 0 Then
                Dim cpd As Dictionary(Of String, clsBranch) = child.PathedDescendants(Path & "." & Trim(CStr(child.ID)))
                ' If cpd Is Nothing Then Stop
                AppendDic(CType(PathedDescendants, Dictionary(Of String, clsBranch)), cpd)
            End If
        Next

    End Function

    Function Descendants() As List(Of clsBranch)

        Descendants = New List(Of clsBranch)
        Descendants.Add(Me)

        For Each branch In childBranches.Values
            Descendants.AddRange(branch.Descendants)
        Next

    End Function


    Sub New(ByVal id As Integer, ByVal Product As clsProduct, ByVal ParentBranch As clsBranch, translation As clsTranslation, picture As String, collectiveNoun As clsTranslation, collectiveNounSingular As clsTranslation, matrix As clsScreen, order As Integer, hidden As Boolean, locked As Boolean, rca As String)

        'This particular constructor is most often called when (re)constructing from the database

        'Branches carry a reference to a product - and although each branch exists under (exactly) 1 parent branch
        'there can be many branches referencing the same product (in the 'master list' of products).. each with a different parent
        GraftedOnAt = New List(Of String)

        Me.ID = id
        Me.Product = Product
        Me.Quantities = New Dictionary(Of Integer, clsQuantity)
        'Me.i_Quantities = Nothing
        Me.i_Slots = New Dictionary(Of String, clsSlot)
        Me.slots = New Dictionary(Of Integer, clsSlot)

        Me.Translation = translation
        Me.CollectiveNoun = collectiveNoun
        Me.collectiveNounSingular = collectiveNounSingular
        Me.Picture = picture
        Me.Matrix = matrix 'Which screen to use in the front end Matrix
        Me.order = order
        Me.Hidden = hidden
        Me.locked = locked
        Me.rca = rca

        'i_childbranches = New List(Of clsBranch)
        childBranches = New Dictionary(Of Integer, clsBranch)

        If Not ParentBranch Is Nothing Then

            ParentBranch.childBranches.Add(Me.ID, Me)
            Me.Parent = ParentBranch
            Me.AllParents.Add(ParentBranch.ID, ParentBranch)
        Else

        End If
        If Not iq.Branches.ContainsKey(Me.ID) Then
            iq.Branches.Add(Me.ID, Me)
        End If

        If Product IsNot Nothing Then Product.Branches.Add(Me)

        If iq.Branches.Count = 1 Then iq.RootBranch = Me 'The first branch we (EVER) add - becomes the root

        oParent = Me.Parent
        Me.Prunes = New Dictionary(Of Integer, clsPrune)

    End Sub

    Public Sub New(ByVal Product As clsProduct, ByVal ParentBranch As clsBranch, translation As clsTranslation, picture As String, CollectiveNoun As clsTranslation, _
                   CollectiveNounsingular As clsTranslation, matrix As clsScreen, order As Integer, hidden As Boolean, rca As String, Optional ByRef writecache As DataTable = Nothing, Optional ByRef nextID As Integer = -1)



        Me.Product = Product
        Me.Picture = picture
        Me.CollectiveNoun = CollectiveNoun
        Me.collectiveNounSingular = CollectiveNounsingular
        'Me.i_Quantities = Nothing
        Me.Quantities = New Dictionary(Of Integer, clsQuantity)
        Me.slots = New Dictionary(Of Integer, clsSlot)
        Me.i_Slots = New Dictionary(Of String, clsSlot)

        Me.Matrix = matrix
        Me.order = order
        Me.Hidden = hidden
        Me.locked = locked
        Me.rca = rca

        Dim sql$
        Dim PBID As String

        If ParentBranch Is Nothing Then
            PBID = "null"
        Else
            PBID = CStr(ParentBranch.ID)
        End If

        Dim prodID As String 'product id
        If Product Is Nothing Then
            prodID = "null"
        Else
            prodID = CStr(Product.ID)

        End If

        Dim matrixID As String
        If Me.Matrix Is Nothing Then
            matrixID = "null"
        Else
            matrixID = CStr(Me.Matrix.ID)
        End If

        Me.Translation = translation

        If PBID <> "null" Then
            If CInt(PBID) <= 0 Then Stop
        End If

        If writecache Is Nothing Then
            sql$ = "INSERT INTO BRANCH(fk_Product_ID,FK_branch_id_parent,fk_translation_key,picture,fk_translation_key_collective,fk_translation_key_collectiveSingular,fk_screen_id_matrix,[order],hidden,locked,rca) "
            sql$ &= "VALUES (" & prodID & ", " & PBID & "," & Me.Translation.Key & ",'" & picture & "'," & Me.CollectiveNoun.Key & "," & Me.collectiveNounSingular.Key & "," & matrixID & "," & order & "," & CStr(IIf(hidden, "1", "0")) & "," & CStr(IIf(hidden, "1", "0")) & "," & da.SqlEncode(Me.rca) & ");"

            Me.ID = da.DBExecutesql(sql$, True)
            If PBID <> "null" Then
                If CInt(PBID) > Me.ID Then
                    If CInt(PBID) > nextID Then Stop

                End If
            End If
        Else
            If nextID = -1 Then
                Beep()
            End If

            '  If CInt(PBID) > nextID Then Stop
            If iq.Branches.ContainsKey(nextID) Then nextID = iq.Branches.ToList.Max(Function(m) m.Value.ID) + 1
            Me.ID = nextID
            nextID += 1


            Dim row As System.Data.DataRow
            row = writecache.NewRow()
            writecache.Rows.Add(row)
            row("ID") = Me.ID '- we EXPLICITLY set ids on branches

            row("fk_product_id") = IIf(prodID = "null", DBNull.Value, prodID)
            row("fk_branch_id_parent") = IIf(PBID = "null", DBNull.Value, PBID)

            row("fk_translation_key") = Me.Translation.Key
            row("picture") = picture
            row("fk_translation_key_collective") = Me.CollectiveNoun.Key
            row("fk_translation_key_collectiveSingular") = Me.collectiveNounSingular.Key
            row("fk_screen_id_matrix") = IIf(matrixID = "null", DBNull.Value, matrixID)
            row("order") = Me.order
            row("hidden") = IIf(hidden, 1, 0)
            row("locked") = IIf(hidden, 1, 0)
            row("rca") = Me.rca
            row("deleted") = False
            If PBID <> "null" Then
                If CInt(PBID) > Me.ID Then
                    Beep() 'my parent is further 'down' the tree than me - which is weird
                End If
            End If
        End If

        If childBranches Is Nothing Then
            childBranches = New Dictionary(Of Integer, clsBranch)
        End If

        If Not ParentBranch Is Nothing Then 'The root node has no parents (so we dont add this child)
            'If ParentBranch.Branches Is Nothing Then ParentBranch.Branches = New List(Of clsBranch)
            ParentBranch.childBranches.Add(Me.ID, Me)
            Me.Parent = ParentBranch

        End If

        If Product IsNot Nothing Then Product.Branches.Add(Me)
        ' If Not iq.Branches.ContainsKey(Me.ID) Then

        iq.Branches.Add(Me.ID, Me)

        'End If

        If iq.Branches.Count = 1 Then iq.RootBranch = Me 'The first branch we add - becomes the root

        oParent = Me.Parent
        Me.Prunes = New Dictionary(Of Integer, clsPrune)
        Me.GraftedOnAt = New List(Of String)

    End Sub
    Public Sub SetParent(ParentBranch As clsBranch)

        If Not ParentBranch Is Nothing Then

            ParentBranch.childBranches.Add(Me.ID, Me)
            Me.Parent = ParentBranch
            Me.AllParents.Add(ParentBranch.ID, ParentBranch)
        Else

        End If
    End Sub

    Public Sub Update(ByRef errorMessages As List(Of String)) Implements i_Editable.update  'As clsBranch

        'todo - allow reparenting

        Dim sql$
        Dim pid$
        Dim matrixid As String
        If Me.Parent Is Nothing Then pid = "null" Else pid = CStr(Me.Parent.ID)
        If Me.Matrix Is Nothing Then matrixid = "null" Else matrixid = CStr(Me.Matrix.ID)

        sql$ = "UPDATE branch SET fk_branch_id_parent=" & pid & ",picture=" & da.SqlEncode(Me.Picture) & _
            ",fk_translation_key=" & Me.Translation.Key.ToString & ",fk_screen_id_matrix=" & matrixid & ",[order]=" & Me.order & _
            ",rca=" & da.SqlEncode(Me.rca) & ",FK_PRODUCT_ID=" & If(Me.Product IsNot Nothing, Me.Product.ID, "null") & _
            ",fk_translation_key_collective=" & Me.CollectiveNoun.Key & ",fk_translation_key_collectiveSingular=" & Me.collectiveNounSingular.Key & _
            ",deleted=" & IIf(Me.deleted, "1", "0") & _
            ",hidden=" & IIf(Me.Hidden, "1", "0") & _
            ",locked=" & IIf(Me.deleted, "1", "0") & _
            " WHERE ID=" & Me.ID
        da.DBExecutesql(sql$, False)


        If Not oParent Is Nothing Then
            oParent.childBranches.Remove(Me.ID)
        End If

        If Not Me.Parent Is Nothing AndAlso Not Me.Parent.childBranches.ContainsKey(Me.ID) Then
            Me.Parent.childBranches.Add(Me.ID, Me)
        End If

        oParent = Me.Parent

        '  Return Me

    End Sub


    ''' <summary>
    ''' Performs a hard (and cascading) delete from the branch - including its referencing quantities, slots and quotItems (and any quotes featuring those quoteitems)
    ''' </summary>
    ''' <param name="errorMessages"></param>
    ''' <param name="summary">verbose, indented (tabbed), textual summary of the atomic operation(s) performed</param>
    ''' <param name="depth">Pass 0, used to foramt the summary</param>
    ''' <remarks></remarks>
    Public Sub HardDelete(ByRef errorMessages As List(Of String), ByRef summary As String, depth As Integer, DoIT As Boolean, counts As Dictionary(Of String, Integer))

        Dim found As Integer = 0
        Dim descendants As List(Of clsBranch) = Me.Descendants  'getSKUdDescendants(True, bi.path, bi, True, 100000, found, errorMessages)

        Dim j As New List(Of String)
        For Each b In descendants
            j.Add(b.ID)
        Next

        Dim bids As String = Join(j.ToArray, ",")  'Construct a comma seperated list of the descendant branch ID's (regardless of visibility !)

        'If Me.childBranches.Count Then
        '    For Each c In Me.childBranches.Values.ToList
        '        c.HardDelete(errorMessages, summary, depth + 1, DoIT, counts)
        '    Next
        'Else
        '    'ok to delete 'me' now (i have no childbranches)

        Dim sql$

        sql$ = "FROM quoteItem where fk_branch_id in (" & bids & ") "
        summary$ &= DeleteOrcount(DoIT, sql$, depth, counts, "QuoteItems") & " QuoteItems<br>"

        sql$ = "FROM quote where id in (select fk_quote_id from quoteitem where fk_branch_id in (" & bids & "))"
        summary$ &= DeleteOrcount(DoIT, sql$, depth, counts, "Quotes") & " Quotes<br>"

        sql$ = "FROM graft where fk_branch_id_source in (" & bids & ")"
        summary$ &= DeleteOrcount(DoIT, sql$, depth, counts, "GraftSource") & " Grafts (sources)<br>"

        sql$ = "FROM graft where fk_branch_id_target in (" & bids & ")"
        summary$ &= DeleteOrcount(DoIT, sql$, depth, counts, "GraftTarget") & " Grafts (target)<br>"

        sql$ = "FROM quantity where fk_branch_id in (" & bids & ")"
        summary$ &= DeleteOrcount(DoIT, sql$, depth, counts, "Quantities") & " quantities<br>"

        sql$ = "FROM slot where fk_branch_id in (" & bids & ")"
        summary$ &= DeleteOrcount(DoIT, sql$, depth, counts, "Slots") & " slots<br>"

        sql$ = "FROM [Branch] WHERE ID in (" & bids & ")"
        ' summary$ &= DeleteOrcount(DoIT, sql$, depth, counts, "Branches") & " branches<br>"

        da.DBExecutesql("update branch set deleted=1 where id in (" & bids & ")")


        If DoIT Then
            If Me.Parent IsNot Nothing Then
                Me.Parent.childBranches.Remove(Me.ID)
            End If
        End If
        ' End If

    End Sub

    Public Function QuoteAllSystemsBelow(lid As UInt64, Path$, quote As clsQuote, errorMessages As List(Of String), results As List(Of String))


        If Me.Product IsNot Nothing AndAlso Me.Product.isSystem(Path) Then

            'add this system to a quote
            Dim prices As List(Of clsPrice) = Me.Product.GetPrices(quote.BuyerAccount, quote.BuyerAccount.SellerChannel.IsCloneOf.priceConfig, iq.AllVariants, errorMessages, True)

            If prices Is Nothing Then
                results.Add(Product.SKU & " **NO PRICES**")
            Else
                For Each price In prices 'For this one product there can be more than one price/stock (one per variant!)

                    'supress the test variants for any non admin user
                    If Not price Is Nothing Then 'POAs are 'NOthings' in the list of prices

                        Dim qi As clsQuoteItem = quote.setQtyByPath(Path$, prices.First.SKUVariant, 1, True, 1, errorMessages)

                        If qi IsNot Nothing Then
                            qi.fetchPreinstalled(lid, quote.BuyerAccount, errorMessages)

                            If quote.PassesValidation(lid) Then
                                results.Add(Product.SKU & " **PASS**")
                            Else
                                results.Add(Product.SKU & " **FAIL**")
                            End If

                            'remove the system ((ready to quote the next one)
                            quote.SetQtyByItemID(qi.ID, 0, True, 1, errorMessages)
                            'qi.Update()

                        End If

                        Exit For  'we quote on the first variant we come accorss
                    End If
                Next
            End If
        End If

        For Each child In Me.childBranches.Values
            child.QuoteAllSystemsBelow(lid, Path & "." & child.ID, quote, errorMessages, results)
        Next


    End Function

    Private Function DeleteOrcount(doit As Boolean, fromsql$, depth As Integer, counts As Dictionary(Of String, Integer), entity As String) As String

        If Not counts.ContainsKey(entity) Then
            counts.Add(entity, 0)
        End If


        Dim num As Integer
        If doit Then
            num = da.DBExecutesql("delete " & fromsql$)
            counts(entity) += num
            Return "Deleted " & num.ToString
        Else

            num = da.DBSelectFirst("select count(*) " & fromsql$)
            counts(entity) += num
            Return "Would delete - " & num
        End If

    End Function


    Public Sub delete(ByRef errorMessages As List(Of String)) Implements i_Editable.delete

        'todo - remove all grafts (and prunes) of this branch
        'deleting a branch can orphan products, but that's OK

        Dim sql$

        sql$ = "delete from graft where fk_branch_id_source=" & Me.ID
        da.DBExecutesql(sql$)

        sql$ = "delete from graft where fk_branch_id_target=" & Me.ID
        da.DBExecutesql(sql$)


        'this is not robust or complete and will not cope with deleteting branches with more than one level of descendants
        sql$ = "delete from branch where fk_branch_id_parent=" & Me.ID
        da.DBExecutesql(sql$)

        If oParent IsNot Nothing Then
            oParent.childBranches.Remove(Me.ID)
        End If

        If Me.Parent IsNot Nothing Then Me.Parent.childBranches.Remove(Me.ID)


        sql$ = "DELETE FROM [Branch] WHERE ID=" & Me.ID
        da.DBExecutesql(sql$, False)


    End Sub


    ''' <summary>
    ''' Recursivley returns a list of the paths of descendant branches named (with the unique/specific trasnlation) TL
    ''' </summary>
    ''' <remarks>Note - Walking branches by name is rarely a good idea - It's possible there is more than one tranlation with the same text </remarks>
    Public Function DescendantsNamed(path$, tl As clsTranslation) As List(Of String)

        DescendantsNamed = New List(Of String)
        If Me.Translation Is tl Then
            DescendantsNamed.Add(path$)
            Exit Function 'speedup
        End If

        For Each child In Me.childBranches.Values
            DescendantsNamed.AddRange(child.DescendantsNamed(path$ & "." & child.ID.ToString.Trim, tl))
            If DescendantsNamed.Count > 0 Then Exit Function 'Speedup
        Next

    End Function

    '''<summary>Returns the direct children of this branch</summary>
    ''' <param name="bi">Includes the SHOWALL paramter - if true, branches that would normally be hidden are also returned (with their HideReasons())</param>
    ''' <param name="errormessages"></param>
    ''' <remarks> SKUless (category) branches are only returned if the have visible descdants (ones with NO hidereasons)</remarks>
    Public Function getVisibleChildren(bi As clsBranchInfo, ByRef errormessages As List(Of String), ByRef skus As Integer, ByRef cats As Integer) As Dictionary(Of clsBranch, clsVisibility)

        getVisibleChildren = New Dictionary(Of clsBranch, clsVisibility)
        Dim RememberSet As New HashSet(Of String)
        'LogMessage("Parent Branch ID : " & Me.ID & "-  Name : " & Me.Translation.text(English))
        'If Me.Translation.text(English) = "HW Support" Then
        '    Dim a = 8
        'End If
        For Each child In Me.childBranches.Values.ToList().OrderBy(Function(cb) cb.order)
            'If child.Translation.text(English) = "HW Support" Then
            '    Dim a = 8
            'End If
            'LogMessage("Child Branch ID : " & child.ID & "-  Name : " & child.Translation.text(English))
            If child.Product IsNot Nothing Then
                'LogMessage("Child product ID : " & child.Product.ID & "-  Name : " & child.Product.DisplayName(English) & " Type : " & child.Product.ProductType.Translation.text(English))
            End If
            ' Debug.WriteLine(child.Translation.text(English) & " : " & child.ID)
            Dim cpath$ = bi.path & "." & child.ID
            If bi.showAll = True OrElse (child.PruneInForce(bi.path & "." & child.ID, bi.buyerAccount.SellerChannel.IsCloneOf) = 0) Then
                If child.HasSKU Then

                    Dim hrs As List(Of String) = child.ReasonsForHide(bi.buyerAccount, bi.foci, cpath$, bi.buyerAccount.SellerChannel.priceConfig, False, errormessages)
                    If (hrs.Count) = 0 Or (bi.showAll = True) Then
                        skus += 1
                        getVisibleChildren.Add(child, New clsVisibility(child, cpath, hrs))
                    End If
                Else
                    'Always include the upsell opportunities branch (even thouh it has no children!)
                    If child.rca = "U" AndAlso Not bi.showAll Then getVisibleChildren.Add(child, New clsVisibility(child, cpath, New List(Of String)))

                    'determine the visibility of the SKUless (category) branches (Based on whether they have a visible descendant)
                    Dim vCats As Dictionary(Of clsBranch, clsVisibility)
                    vCats = child.getSKUdDescendants(True, cpath, bi, False, 1, 0, errormessages)
                    ' If child.rca = "U" Then getVisibleChildren.Add(child, New clsVisibility(child, cpath, New List(Of String)))
                    If (From j In vCats.Values Where j.hideReasonList.Count = 0).Any Or bi.showAll Then
                        '   For Each j In (From v In vCats.Values Order By v.branch.order)  'Order category branches by their bracnch.order
                        'If j.hideReasonList.Count = 0 Or bi.showAll Then
                        cats += 1
                        If RememberSet.Add(child.Translation.text(English)) Then
                            getVisibleChildren.Add(child, New clsVisibility(child, cpath, New List(Of String)))
                        End If
                        'end if
                        '   Next
                    End If
                End If
            End If
        Next



    End Function

    Public Sub BranchFirstPaths(ByRef Locations As Dictionary(Of clsBranch, String), path$, forBranches As List(Of clsBranch))

        'builds a dictionary of clsBraanch>Paths at which it first occurs under this branch 

        If forBranches.Contains(Me) Then
            Locations.Add(Me, path)
            Exit Sub 'Assume a branch won't appear again under itself !
        End If

        If Me.childBranches IsNot Nothing Then
            For Each child In Me.childBranches.Values
                child.BranchFirstPaths(Locations, path$ & "." & child.ID, forBranches)
            Next
        End If

    End Sub

    Public Sub SkuPaths(ByRef Dic As Dictionary(Of String, List(Of String)), path$, CrossSKUs As Boolean)

        'builds a dictionary of SKU>List of Paths at which it occurs under this branch (which happens a lot for things like all the occurances of an option in a family)

        If Me.HasSKU Then
            If Not Dic.ContainsKey(Me.Product.SKU) Then
                Dic.Add(Me.Product.SKU, New List(Of String))
            End If
            Dic(Me.Product.SKU).Add(path)

            If Not CrossSKUs Then Exit Sub 'stop recursing if deep is false and we have just found a SKUd part (siblings will still be processed)
        End If

        Dim cpath$ = ""
        If Me.childBranches IsNot Nothing Then
            For Each child In Me.childBranches.Values

                cpath$ = path$ & "." & Trim$(CStr(child.ID))
                child.SkuPaths(Dic, cpath, CrossSKUs)

            Next
        End If

    End Sub

    Public Function getSKUdDescendants(includeself As Boolean, path$, bi As clsBranchInfo, goDeep As Boolean, maxSKUs As Integer, ByRef skusFound As Integer, ByRef errorMessages As List(Of String)) As Dictionary(Of clsBranch, clsVisibility)

        getSKUdDescendants = New Dictionary(Of clsBranch, clsVisibility)
        ' Debug.WriteLine(Me.Translation.text(English).ToUpper())
        'If Me.Translation.text(English).ToUpper() = "TOP RECOMMENDED" Then
        '    Dim a = 9
        'End If

        If bi.buyerAccount Is Nothing Or bi.showAll OrElse Me.PruneInForce(path, bi.buyerAccount.SellerChannel) = 0 Then  'Prunes are not host specific (although that will almost certainly become a requirement)
            If includeself Then
                ' NOTE this WAS BI .path which doesn't track the recursion properly and so didnt work !

                If Me.HasSKU Then

                    'IMPORTANT We need to make this call to get the hidereason populated - Generally it will be fast for hidden products anyway
                    'branches with a hidereason are not displayed in 'normal' operation

                    Dim hrs As List(Of String)

                    If bi.buyerAccount Is Nothing Then  'Descendants can be called with no buyeraccount (if showall is true)
                        hrs = New List(Of String)
                    Else
                        Dim pc As Integer = bi.buyerAccount.SellerChannel.priceConfig
                        hrs = Me.ReasonsForHide(bi.buyerAccount, bi.foci, path$, pc, False, errorMessages)  'Calls GetPrices

                    End If

                    If Me.PruneInForce(path, bi.buyerAccount.SellerChannel) <> 0 Then
                        hrs.Add("Branch is PRUNED at this location ")
                        ' If Not bi.showAll Then Exit Function
                    End If
                    If Me.SKU.StartsWith("###") Then hrs.Add("Fake/Soldered Part")

                    If (hrs.Count = 0) Or (bi.showAll = True) Then
                        skusFound += 1
                        getSKUdDescendants.Add(Me, New clsVisibility(Me, path, hrs)) 'buyeraccount, focus, Path$)
                    End If

                    If Not goDeep Then Exit Function
                    If skusFound > maxSKUs Then Exit Function
                End If
            End If
            Dim cpath$ = ""
            For Each child In Me.childBranches.Values
                cpath$ = path$ & "." & Trim$(CStr(child.ID))
                AppendDic(getSKUdDescendants, child.getSKUdDescendants(True, cpath, bi, goDeep, maxSKUs, skusFound, errorMessages))
                If skusFound >= maxSKUs Then Exit Function
            Next
        End If

    End Function

    Public Sub getSKUdParents(path$, bi As clsBranchInfo, ByRef errorMessages As List(Of String), ByRef rs As List(Of String))

        Dim pathArray() As String = Split(path, ".")
        Dim brID As String
        Dim hrs As List(Of String)
        For Each brID In pathArray
            If IsNumeric(brID) Then
                Dim currentBranch As clsBranch = iq.Branches(CInt(brID))
                If currentBranch.HasSKU Then
                    Dim pc As Integer = bi.buyerAccount.SellerChannel.priceConfig
                    hrs = currentBranch.ReasonsForHide(bi.buyerAccount, bi.foci, path$, pc, False, errorMessages)
                    If (hrs.Count > 0) Then
                        For Each hr In hrs
                            rs.Add(hr)
                        Next
                    End If
                End If
            End If
        Next
    End Sub
    Public Function CheckBundles(buyeraccount As clsAccount, foci As HashSet(Of String), path As String, Showall As Boolean, priceconfig As Int16, ByRef Errormessages As List(Of String)) As List(Of clsBranch)

        CheckBundles = Nothing

        Pmark("CheckBundles")

        Dim retval As List(Of clsBranch) = New List(Of clsBranch)
        Dim reachedSystem As Boolean = False

        Try
            'bundles only exist on systems 
            If Not Me.Product Is Nothing Then
                If Me.Product.isSystem Then
                    reachedSystem = True
                    If Not Me.Product.Bundles Is Nothing Then
                        If (Me.ReasonsForHide(buyeraccount, foci, path, priceconfig Or 1, True, Errormessages).Count = 0) Or Showall Then
                            For Each bundle In Me.Product.Bundles.Values
                                If buyeraccount.SellerChannel.Region.Encompasses(bundle.Region) Then
                                    'does this system have any current bundles for this (sellers) region
                                    If Now > bundle.validFrom And Now < bundle.validTo Then 'getBundle("", 0, Now, region) IsNot Nothing Then
                                        retval.Add(Me)
                                    End If
                                End If
                            Next
                        End If
                    End If
                End If
            End If

            'and recurse.. for each child 

            If Not reachedSystem Then  'we don't recurse beyond systems  (presently - for speed - we may need to in future (for sub-system) - and will probably need a table of BundleBranches for speed at that point)
                Dim descendants As List(Of clsBranch)
                For Each b In Me.childBranches.Values

                    descendants = b.CheckBundles(buyeraccount, foci, path & "." & Trim$(CStr(Me.ID)), Showall, priceconfig, Errormessages)
                    If descendants.Count > 0 Then
                        retval.AddRange(descendants)
                        retval.Add(Me) 'I have descendants who have bundles , so add me too.. (to show where those descendants sit... so the root branch, servers branch, family branch, top value branch etc get their bundle circles)
                    End If
                Next
            End If

            Return retval

        Catch

        Finally
            Pacc("CheckBundles")
        End Try

    End Function

    Public Function checkAvalanche(ByVal system As clsProduct, buyeraccount As clsAccount, foci As HashSet(Of String), path As String, showAll As Boolean, priceconfig As Integer, ByRef errorMessages As List(Of String)) As List(Of clsBranch)

        ' Pmark("CheckAvalanche")

        'called once at startup on the root branch
        'recurses the full tree, creating a list of which branches feature avalanche offers for the specified buyer account
        'this is necessary as different products are visible (see branch.productvisible) to different buyers (by virtue of having a price or not, regional restrictions etc.)
        'and recursing branches in realtime to look for 

        Dim retval As List(Of clsBranch) = New List(Of clsBranch)
        Dim prodRef As String

        'Try
        If Not Me.Product Is Nothing Then
            If Me.Product.isSystem Then
                Dim hr As List(Of String) = Me.ReasonsForHide(buyeraccount, foci, path$, priceconfig Or 1, True, errorMessages)
                If (hr.Count = 0) Or showAll Then
                    system = Me.Product

                    'if this system has no avalancheOPGs we don't need to recurse into its options - WILL NEED TO BE REMOVED FOR SYSTEMS WITHIN SYSTEMS
                    If Me.Product.AvalancheOPGs.Count = 0 Then Return retval : Exit Function

                    For Each av In Me.Product.AvalancheOPGs.Values
                        'does this system have any current avalanche options for this (sellers) region
                        If av.getAvalancheOptions("", 0, Now, buyeraccount.SellerChannel.Region).Count > 0 Then
                            'iq.avalancheBranches(buyeraccount.BuyerChannel).Add(Me)
                            retval.Add(Me)
                            Exit For
                        End If
                    Next
                Else
                    'the system is not visible.. stop recursing
                    Return retval
                    Exit Function
                End If
            Else
                'you're an option (or a placeholder product) - do you have a refcode, and is it valid for one of your systems' avalanches ?
                If (Me.ReasonsForHide(buyeraccount, foci, path$, buyeraccount.SellerChannel.priceConfig, True, errorMessages).Count = 0) Or showAll Then
                    If Me.Product.i_Attributes_Code.ContainsKey("ProdRef") Then
                        prodRef = Me.Product.i_Attributes_Code("ProdRef")(0).Translation.text(English)
                        If system IsNot Nothing Then
                            For Each av In system.AvalancheOPGs.Values
                                If av.getAvalancheOptions(prodRef, 0, Now, Nothing).Count > 0 Then
                                    'iq.avalancheBranches(buyeraccount.BuyerChannel).Add(Me)
                                    retval.Add(Me)
                                    Exit For
                                End If
                            Next
                        End If
                    End If
                End If
            End If
        End If

        'and recurse.. for each child
        Dim descendants As List(Of clsBranch)
        For Each b In Me.childBranches.Values
            If b.PruneInForce(path & "." & b.ID.ToString.Trim, buyeraccount.SellerChannel) = 0 Then 'respect the Prunes man !
                descendants = b.checkAvalanche(system, buyeraccount, foci, path & "." & Trim$(CStr(b.ID)), showAll, priceconfig, errorMessages)
                If descendants.Count > 0 Then
                    retval.AddRange(descendants)
                    retval.Add(Me) 'I have descendants who have avalanche offers, so add me too.. (to show where those descendants sit... so the root branch, servers branch, family branch, top value branch etc get their avalanche stars
                End If
            Else
                '    Beep()
            End If
        Next

        'Catch ex As Exception

        'Finally
        '    Pacc("CheckAvalanche")

        'End Try

        Return retval

    End Function

    Public Sub checkFlex(buyeraccount As clsAccount, foci As HashSet(Of String), path As String, PriceConfig As Int16, ByRef branches As List(Of clsBranch), ByRef errormessages As List(Of String))

        'called once at startup on the root branch
        'recurses the full tree, creating a list of which branches feature (descendant) offers for the specified buyer account
        'this is necessary as different products are visible to different buyers (by virtue of having a price or not, regional restrictions etc.)
        ' Dim retval As List(Of clsBranch) = New List(Of clsBranch)

        'Try
        If Not buyeraccount.BuyerChannel.SchemeEnabled("F") Then Exit Sub 'Disable this if the user cannot see flex...

        'NB priceconfig at this point has had is's 8 bit ANDEd out - so we will NOT check the webservice (becuase we'd be making a call for far too many products)
        If Me.Product IsNot Nothing Then

            Dim rh As List(Of String) = Me.ReasonsForHide(buyeraccount, foci, path$, PriceConfig, True, errormessages)
            If rh.Count = 0 Then

                'If isPrunedAt(path$) And Not showAll Then Return retval 'PRUNES ARE NOT HANDLED HERE (there are too many paths)


                'IMPORTANT
                If Product.isSystem And Product.OPGflexLines.Count = 0 Then Exit Sub 'Return retval

                Dim oneRegionHasFlex = False
                For Each FlexLine In Me.Product.OPGflexLines.Values  'Flexlines contain a rebate on a product, valid between certain dates, under a certain OPG

                    If FlexLine.FlexOPG.AppliesToRegion(buyeraccount.BuyerChannel.Region) Then
                        If FlexLine.isCurrent And FlexLine.FlexOPG.isCurrent Then
                            For Each b In Split(path, ".")
                                If b <> "tree" Then
                                    If Not branches.Contains(iq.Branches(CInt(b))) Then
                                        branches.Add(iq.Branches(CInt(b)))
                                    End If
                                End If
                            Next
                            ' retval.Add(Me) 'add me (to the list of branches which have flex on them)
                            'Exit For ' was exit for
                            '        Return retval
                            oneRegionHasFlex = True
                        End If

                    Else

                        'STOP recursing if you hit a system then does not qualify regionally !
                        'otherwise we recurse to options that migh be part of a DIFFERENT flex
                    End If
                Next
                If Product.isSystem AndAlso Not oneRegionHasFlex Then Exit Sub
            Else
                'the product is not visible.. (so neither are any of its descendants !) - stop recursing
                'Return retval
                Exit Sub
            End If

        End If

        'and recurse.. for each child
        'Dim descendants As List(Of clsBranch)
        For Each b In Me.childBranches.Values

            If b.PruneInForce(path & "." & Trim$(CStr(b.ID)), buyeraccount.SellerChannel) = 0 Then 'respect the Prunes man !
                b.checkFlex(buyeraccount, foci, path & "." & b.ID.ToString.Trim, PriceConfig, branches, errormessages)

                ' For Each d In descendants
                ' If Not retval.Contains(d) Then retval.Add(d)
                ' Next
            End If
        Next

        'Return retval

    End Sub

    Public Function HasMajorSlot(majorCode As String) As Boolean

        HasMajorSlot = False

        For Each slot In Me.slots.Values
            If Not slot.deleted Then
                If LCase(slot.Type.MajorCode) = LCase(majorCode) Then Return True
            End If
        Next

    End Function


    Public Function hasSlot(SlotType As clsSlotType) As Boolean


        hasSlot = False
        For Each slot In Me.slots.Values
            If slot.Type Is SlotType And Not slot.deleted Then Return True
        Next

    End Function

    Public Sub MajorSlots(ByRef dic As Dictionary(Of String, Integer))

        'Returns the number of (gives) slots of each of the major types - recursing branches - but not crossing systems

        For Each t In Me.slots.Values
            If Not t.deleted Then
                If t.numSlots > 0 Then
                    'If t.Type.MajorCode = "CPU" Then Stop
                    If Not dic.ContainsKey(t.Type.MajorCode) Then dic.Add(t.Type.MajorCode, 0)
                    dic(t.Type.MajorCode) += t.numSlots
                End If
            End If
        Next

        Dim isSystem As Boolean
        For Each c In Me.childBranches.Values

            If c.Product IsNot Nothing Then
                isSystem = c.Product.isSystem
            End If

            If Not isSystem Then
                c.MajorSlots(dic)  'recurse (if its not a system)
            End If
        Next

    End Sub

    Private Function BranchUI(bi As clsBranchInfo, numSKUs As Integer, numCats As Integer, hideReasons As List(Of String), ByRef errorMessages As List(Of String), lid As UInt64) As PlaceHolder

        'Called for branches being rendered as a BRANCH (bt.branch) .. ie. not a square, matrix row, etc
        'DO NOT CONFUSE WITH  clsBranch.UI()

        BranchUI = New PlaceHolder
        Dim imageAdded As Boolean = False
        If iq.sesh(bi.lid, "Paradigm") = enumParadigm.configuringSystem Then
            If Me.Picture <> "" Then
                Dim img As New WebControls.Image
                img.ImageUrl = "http://www.channelcentral.net" & Me.Picture
                img.CssClass = "prodPhoto"
                BranchUI.Controls.Add(img)
                imageAdded = True

            End If
        End If

        Dim showcounts As Boolean = True
        If numSKUs < 0 Then showcounts = False

        BranchUI.Controls.Add(Me.Title(bi, showcounts, False, False, numSKUs, numCats, hideReasons, errorMessages))
        BranchUI.Controls.Add(Me.PromoIndicators(bi, errorMessages))


        If Not IsNothing(Me.Product) Then

        End If

        If Me.HasSKU Then BranchUI.Controls.Add(Me.BuyUI(bi.buyerAccount, bi.path$, lid))


        Dim lit As New Literal
        If Me.Product IsNot Nothing Then
            'Show the HighPerformace and energy star attributes if present
            Dim perf As List(Of clsProductAttribute) = Nothing

            If Product.i_Attributes_Code.TryGetValue("Perf", perf) Then
                lit = New Literal
                lit.Text = "<div class='highPerf'><img class='highPerformance' src='" & imagebase & "/images/icons/icon_iq2_highp.png' title='" & perf(0).Attribute.Translation.text(bi.buyerAccount.Language) & "'/></div>"
                BranchUI.Controls.Add(lit)
            End If

            Dim eStar As List(Of clsProductAttribute) = Nothing
            If Product.i_Attributes_Code.TryGetValue("eStar", eStar) Then
                lit = New Literal
                'http://iquote2.channelcentral.net/sandbox/daisyimages//images/logo/logo_energystar.jpg'
                lit.Text = "<div class='eStar'><img class = 'energyStar' src='" & imagebase & "/images/logo/logo_energystar.jpg' title='" & eStar(0).Attribute.Translation.text(bi.buyerAccount.Language) & "'/></div>"

                BranchUI.Controls.Add(lit)
            End If

            Dim sc As List(Of clsProductAttribute) = Nothing
            If Product.i_Attributes_Code.TryGetValue("SC", sc) Then
                lit = New Literal
                Dim sct As String = Replace(sc(0).Translation.text(English), " ", "_")
                lit.Text = "<div class='supC'><img class = 'supplyChain' src='" & imagebase & "images/logo/logo_" & sct & ".png' title='" & Replace(sct, "_", " ") & "'/></div>"

                BranchUI.Controls.Add(lit)
            End If

        End If


        'BranchUI.Controls.Add(NewLit("<div>&nbsp;</div>"))
        If Me.Picture <> "" AndAlso Not bi.treeMode And imageAdded = False Then

            'supress the picture if the same one exists 'above'
            'Supress the photo if it appears on the path - but always show the photo on 'configuringsystem' mode
            If (Not PictureOnPath(oneAbove(bi.path), Me.Picture)) AndAlso bi.Paradigm = enumParadigm.configuringSystem Then
                Dim picpanel As Panel = New Panel
                picpanel.CssClass = "photoDiv"
                BranchUI.Controls.Add(picpanel)

                Dim img As New WebControls.Image
                img.ImageUrl = imagebase & Me.Picture
                img.CssClass = "prodPhoto"
                'BranchUI.Controls.Add(img)
                picpanel.Controls.Add(img)
            End If
        End If

        If bi.treeMode = True Then

            Dim ipanel As Panel = New Panel : BranchUI.Controls.Add(ipanel) : ipanel.CssClass = "ib"
            Dim toolsPanel As Panel = ExpandablePanel(ipanel, "Tools", "adm", bi)

            If toolsPanel IsNot Nothing Then toolsPanel.Controls.Add(adminControls(bi, bi.path, bi.path$)) : toolsPanel.CssClass &= " ib"
            'one shot messages (not robust - extend/re-use with caution - if somebody else happens to render the same branch before you - they will get your message (and you won't))
            If Me.message <> "" Then
                BranchUI.Controls.Add(NewLit(Me.message))
                Me.message = ""
            End If

        End If


        'If Me.Product IsNot Nothing Then
        '    Dim lblID As New Label
        '    lblID.Text = "ID:" & Me.Product.ID
        '    BranchUI.Controls.Add(lblID)
        'End If
        'BranchUI.Controls.Add(SellerSkus(Bi))

        'listprice
        'If Me.HasSKU Then
        '    Dim pr As clsPrice = Me.Product.ListPrice(Bi.BuyerAccount)
        '    If pr IsNot Nothing Then
        '        Dim lp As Label = pr.Price.DisplayPrice(Bi.BuyerAccount, errorMessages)
        '        BranchUI.Controls.Add(lp)
        '    End If
        'End If


        'Subtitle and description are mutually exclusive at the moment


        If Product IsNot Nothing Then
            If Product.i_Attributes_Code.ContainsKey("subTitle") Then  'The subtitle is shown on closed branches - open branches also show the proddesc (see ProductInfo)
                'lit = New Literal
                'lit.Text = "<div class='ProdSubTitle'>" & Product.i_Attributes_Code("subTitle")(0).Translation.text(Bi.buyerAccount.Language) & "</div>"
                'BranchUI.Controls.Add(lit)
            ElseIf Product.i_Attributes_Code.ContainsKey("desc") Then
                lit = New Literal
                lit.Text = "<span class='prodDesc'>" & Product.i_Attributes_Code("desc")(0).Translation.text(bi.buyerAccount.Language) & "</span>"
                BranchUI.Controls.Add(lit)
            End If

            If Me.PruneInForce(bi.path, bi.buyerAccount.SellerChannel) Then
                'Beep()
                BranchUI.Controls.Add(ErrorDymo("Branch is PRUNED here"))
            End If


            If hideReasons.Count > 0 Then
                'OK this is it
                'tl.ToolTip = Join(hideReasons.ToArray, ",")
                Dim hrs As New Panel
                hrs.CssClass = "hidereasons"
                BranchUI.Controls.Add(hrs)
                For Each reason In hideReasons
                    If reason.ToLower.Contains("pruned") Then
                        '     Beep()
                    End If
                    hrs.Controls.Add(ErrorDymo(reason))
                Next
            End If

        End If

        ''iterate the slots - output the HDD bay type and count
        'For Each b In Me.childBranches  'the slots are in the chassis branch!
        '    For Each s In Me.slots.Values
        '        If s.path = Bi.path Or s.path = "" Then  'slots *may* have paths  .. for example, some card might give 4 slots on one machine, but only two in another - due to some physical or electrical constraint - and pathed version takes precedence
        '            If s.Type.MajorCode = "HDD" Then

        '                Dim DriveType As Panel = New Panel
        '                DriveType.CssClass = "DriveType"
        '                DriveType.Controls.Add(iq.EnglishIndex("Hard Drives:").HTML(Bi.AgentAccount.Language))
        '                DriveType.Controls.Add(s.Type.Translation.HTML(Bi.AgentAccount.Language))  ' Eg  "Hot Plug 2.5inch smart Carrier"
        '                BranchUI.Controls.Add(DriveType)

        '                Dim Bays As Panel = New Panel
        '                Bays.CssClass = "Bays"
        '                lit = New Literal : lit.Text = "Drive bays:" 'fixed' in a hurry for vegas - needs trasnaltiosn
        '                Bays.Controls.Add(lit) 'iq.EnglishIndex("Drive Bays:").HTML(Bi.AgentAccount.Language))
        '                lit = New Literal
        '                lit.Text = s.numSlots.ToString
        '                Bays.Controls.Add(lit)  ' Number of slots (bays) - eg 8
        '                BranchUI.Controls.Add(Bays)

        '            End If
        '        End If
        '    Next
        'Next

        ''Conditions for showing quickfilter button need work
        'If numSKUs > 4 Then
        'If Split(Bi.path, ".").Count > 3 Then
        'End If


    End Function

    Private Function SellerSkus(bi As clsBranchInfo) As PlaceHolder

        SellerSkus = New PlaceHolder

        Dim lit As Literal
        lit = New Literal
        If Me.HasSKU Then
            If Me.Product.i_Variants IsNot Nothing Then
                lit.Text = "(" & bi.buyerAccount.SellerChannel.DisplayName(English) & "SKUs:"
                If Me.Product.i_Variants.ContainsKey(bi.buyerAccount.SellerChannel) Then
                    For Each v In Me.Product.i_Variants(bi.buyerAccount.SellerChannel)
                        lit.Text &= "SKU:" & v.DistiSku
                    Next
                    SellerSkus.Controls.Add(lit)
                End If
            End If
        End If

    End Function

    ''' <summary>
    ''' Typically display a title and some Expand Buttons - 
    ''' It may be a branch, square, tab, or matrix row (or soemething else as we add new ways to render branches)
    ''' </summary>
    ''' <remarks>The BranchHeader is the bit you see when the branch is closed (you typically still see it when its open)</remarks>
    Public Function BranchHeader(bi As clsBranchInfo, ByRef bs As clsBranchState, pbs As clsBranchState, hideReasons As List(Of String), skus As Integer, cats As Integer, ByRef errorMessages As List(Of String), lid As UInt64) As Panel

        BranchHeader = New Panel
        BranchHeader.ID = bi.path

        'BranchHeader.Controls.Add(NewLit("<p>DIV:" & bi.path & "</p>")) 

        'sd is the SKUd descendant(s).. all these branches will have a product
        'dd is the direct descendant(s) .. may contain categories (product-less branches)

        If bi.treeMode Then
            pbs.rca = enumBt.Branch
            Dim PruneID As Integer = bi.branch.PruneInForce(bi.path, bi.buyerAccount.SellerChannel)
            If PruneID <> 0 Then
                'BranchHeader.Controls.Add(NewLit("PRUNED"))
                BranchHeader.Controls.Add(FunctionButton(bi.path, PruneID, "unprune", "unprune", "reinstates/unprunes this branch"))
            End If
        End If


        If pbs.rca <> enumBt.Hidden And pbs.rca <> enumBt.OpenSquare And AccountHasRight(lid, "EDITTREE") And bi.treeMode = True Then
            'Dim BtnEdit As New HyperLink
            'BtnEdit.Text = Xlt("Edit", bi.buyerAccount.Language)
            'BtnEdit.ToolTip = Xlt("Edit - Edits this branch (and the product, slots, quantities etc. attached to it) " & bi.path, bi.buyerAccount.Language)
            'BtnEdit.ImageUrl = "/images/navigation/pencil.png"
            'Dim url$
            'url$ = "edit.aspx?cmd=expand&path=Branches(" & Me.ID & ")&TreePath=" & bi.path$ & "&lid=" & bi.lid

            'BtnEdit.NavigateUrl = url$
            'BtnEdit.Attributes("target") = "_blank"
            'BranchHeader.Controls.Add(BtnEdit)
        End If

        '   If bi.treeMode = True Then
        ' BranchHeader.Controls.Add(adminControls(bi, bi.path, bi.path$))
        ' End If

        Select Case pbs.rca 'bi.branchState.renderAs     - How a branch is rendered is determined by how a it's parent renders its children

            'Case bt.hidden
            'don't need to render *anything*

            Case enumBt.OpenSquare

                'an open square renders nothing ... except its children

                If bs IsNot Nothing AndAlso Not pbs.rca = enumBt.OpenSquare Then 'only (hidden) openSquares that are themselves open render their children
                    BranchHeader.CssClass &= " branch"
                    BranchHeader.Controls.Add(Me.BranchUI(bi, skus, cats, hideReasons, errorMessages, lid))
                End If

            Case Is = enumBt.Branch, enumBt.OpenBranch

                If pbs.rca = enumBt.OpenBranch Then 'AutoOpen the branch
                    If bs IsNot Nothing Then  'AM I OPEN ?  otherwise you cant (ever) close auto opening branches
                        Dim bt As enumBt = CType(BTchar.IndexOf(bi.branch.rca.First), enumBt) 'rember an OpenBranch doesnt neccessarily render its children as branches
                        bs = New clsBranchState(bi.lid, bi.path, bt, (bt = enumBt.gridrow), 0, 100)
                    End If
                End If

                ' If bi.Paradigm <> enumParadigm.configuringSystem Then 

                If (Me.Product Is Nothing OrElse (Me.Product IsNot Nothing AndAlso (Not Me.Product.isSystem(bi.path) Or bi.Paradigm = enumParadigm.AddingSystem))) Or AccountHasRight(lid, "DIAGVIEW") Then
                    BranchHeader.Controls.Add(Me.ExpandCollapseButton(pbs, bi, bs, errorMessages))
                End If

                BranchHeader.CssClass &= " branch"
                ' a branch renders pratically the same whether it is open or closed - the only singinifcant difference is that an open branch renders its children
                BranchHeader.Controls.Add(Me.BranchUI(bi, skus, cats, hideReasons, errorMessages, lid))


            Case enumBt.Square, enumBt.DetailSquare

                If bs IsNot Nothing Then
                    'this is an open sqaure
                    'we render nothing (but the children)
                Else
                    'The header IS the square
                    BranchHeader.CssClass &= " square dropShadow ib"
                    'BranchHeader.Attributes("onclick") = ButtonScript(bi.path, "open", "bcHolder")
                    BranchHeader.Attributes("onclick") = ButtonScript("cmd=openSquare&path=" & bi.path & "&into=tree")
                    Dim IsMin As Boolean
                    BranchHeader.Controls.Add(Me.SquareUI(bi, hideReasons, skus, cats, lid, Nothing, IsMin))

                    For Each h In hideReasons
                        BranchHeader.Controls.Add(ErrorDymo(h))
                    Next
                    If IsMin Then BranchHeader.CssClass = " squareHidden dropShadow ib"
                    '               BranchHeader.ToolTip = BranchHeader.Attributes("onclick")
                End If

            Case enumBt.Tab, enumBt.hYperlink, enumBt.bighyperlinK

                'note this is the (empty) panel which will hold the tabstrip 
                'see also renderTabHeads()I 
                'every tab has a panel (the branhcheader) - but only the open one gets any content
                '          BranchHeader.CssClass &= " isTabPanel" 'just for identification in the page source its not a used style
                '   If bi.branchState.state = oc.open Then 'only the open tab (body) displays anything
                '                BranchHeader.CssClass &= " tabBody dropShadow"
                ' End If

            Case enumBt.gridrow  'Remember the gridrow IS the branchHeader.. (it can be opened !)
                If bs IsNot Nothing AndAlso bs.rca = enumBt.gridrow Then BranchHeader.CssClass &= " highlighted"
                BranchHeader.CssClass &= " isMatrixRow hideOverflow" 'just for identification in the page source its not a used style

                If CBool(bi.rownum And 1) Then BranchHeader.CssClass &= " matrixRowOdd" Else BranchHeader.CssClass &= " matrixRowEven" 'alternating stripes

                Dim panel As Panel = New Panel()
                panel.CssClass = "matrixRowColumns"
                For Each b In BranchHeader.Controls
                    panel.Controls.Add(b)
                    BranchHeader.Controls.Remove(b)
                Next

                If Me.HasSKU Then
                    If Not Me.isOptionOrOptionHolder(bi.path) Or AccountHasRight(lid, "DIAGVIEW") Then
                        'Bodge to hide the expand description in "simple" option mode, request from Greg via DM/JN
                        panel.Controls.Add(Me.ExpandCollapseButton(pbs, bi, bs, errorMessages))
                    End If

                    'panel.Controls.Add(Me.PromoIndicators(bi, errorMessages)) 'new
                    If bi.EffectiveHeader Is Nothing AndAlso Not pbs.United Then
                        'Dim matrixHeaders As Dictionary(Of String, clsScreenHeader) = CType(iq.sesh(bi.lid, "matrixHeaders"), Dictionary(Of String, clsScreenHeader))
                        '    If (matrixHeaders.ContainsKey(bi.path)) Then
                        '        bi.matrixHeader = matrixHeaders(bi.path)
                        '    Else

                        Dim descendants As Dictionary(Of clsBranch, clsVisibility) = bi.visibleChildren(errorMessages, True, 0, 0, False, False)
                        bi.CreateMatrixHeader(descendants)
                    End If
                    '    'bi.EffectiveHeader = bi.matrixHeader 'new - ML Removed
                    'End If


                    For Each hr In hideReasons
                        Dim lt As Literal = New Literal
                        lt.Text = "<span style='color:white;background-color:red;'>" & hr & "</span>"
                        panel.Controls.Add(lt)
                    Next

                    If pbs.United AndAlso (bs Is Nothing OrElse pbs.rca = enumBt.gridrow) Then
                        'Must be a header for the rendering grid, find it
                        Dim screenHeaders = CType(iq.sesh(lid, "screenHeaders"), Dictionary(Of String, clsScreenHeader))
                        If bi.rootPath Is Nothing Then
                            'Somethings wrong, we have no rootpath, maybe rendering directly to a div rather than the whole tree, lets find a matrix above this to render as there must be one?
                            bi.rootPath = matrixHeaderAbove(lid, bi.path, errorMessages).Path
                        End If
                        If screenHeaders.ContainsKey(bi.rootPath) Then
                            panel.Controls.Add(screenHeaders(bi.rootPath).screen.MatrixRow(Me, bi, errorMessages, True))
                        Else
                            panel.Controls.Add(bi.EffectiveMatrix.MatrixRow(Me, bi, errorMessages, False))
                        End If
                    Else
                        If bi.EffectiveMatrix IsNot Nothing Then
                            panel.Controls.Add(bi.EffectiveMatrix.MatrixRow(Me, bi, errorMessages, False))
                        End If

                    End If

                    BranchHeader.Controls.Add(panel)

                    If bi.branch.Product.i_Attributes_Code.ContainsKey("desc") AndAlso Me.Product.isSystem(bi.path) AndAlso (Not Me.isOptionOrOptionHolder(bi.path) Or AccountHasRight(lid, "DIAGVIEW")) Then
                        Dim lit As Literal = New Literal
                        Dim txt As String = bi.branch.Product.i_Attributes_Code("desc")(0).Translation.text(bi.buyerAccount.Language)
                        lit.Text = "<div class='inGridDescription'>" & txt & "</div>"
                        BranchHeader.Controls.Add(lit)
                    End If


                Else
                    BranchHeader.Controls.Add(NewLit("SKUless branch in grid " & Me.EnglishName))
                End If
                '     Case Is = enumBt.headless  'System branches in 'configure' paradigm render as this 
                '         BranchHeader.Controls.Add(Me.BranchUI(bi, -1, -1, False, hideReasons, errorMessages, lid))

            Case Is = enumBt.TROhead
                Dim p As Panel = New Panel
                p.CssClass = "troSectionImage"
                p.ID = "troSectionImage"
                BranchHeader.Controls.Add(p)

                If Me.Picture <> "" Then
                    Dim img As New WebControls.Image
                    img.ImageUrl = "http://www.channelcentral.net" & Me.Picture
                    p.Controls.Add(img)
                End If

                'auto open tro headers
                ' If bs IsNot Nothing Then  'otherwise you cant (ever) close auto opening branches
                Dim bt As enumBt = CType(BTchar.IndexOf(bi.branch.rca.First), enumBt)
                bs = New clsBranchState(bi.lid, bi.path, bt, (bt = enumBt.gridrow), 0, 100)

            Case Is = enumBt.TROitem
                BranchHeader.Controls.Add(Me.TROitem(bi, errorMessages))

            Case Is = enumBt.helpMechoose
                Dim p As Panel = New Panel
                p.CssClass = "hmcSectionHeader"
                p.ID = "troSectionImage"
                BranchHeader.Controls.Add(p)

                p.Controls.Add(NewLit("<span class='hmcHead'>" & Me.Translation.text(bi.buyerAccount.Language) & "</span>"))

                'auto open tro headers
                ' If bs IsNot Nothing Then  'otherwise you cant (ever) close auto opening branches
                Dim bt As enumBt = CType(BTchar.IndexOf(bi.branch.rca.First), enumBt)
                bs = New clsBranchState(bi.lid, bi.path, enumBt.TROitem, (bt = enumBt.gridrow), 0, 100)

            Case Is = enumBt.Hidden
                'render nothing
                Beep()

            Case Else
                errorMessages.Add("* Parent Branch RCA not set/unhandled (" & pbs.rca & ")")
        End Select





        If bs IsNot Nothing Then  'switchers only appear on open branches!
            If bs.rca <> enumBt.Tab Then
                If bs.rca = enumBt.helpMechoose Or (Not (pbs.rca = enumBt.Tab And bs.rca = enumBt.gridrow) AndAlso (Not Me.isOptionOrOptionHolder(bi.path) Or AccountHasRight(lid, "DIAGVIEW"))) Then  'supress switcher on grids hosted in tabs
                    BranchHeader.Controls.Add(Switcher(bi, bs, False, Me.rca))
                End If
            End If
        End If


        ''Quick filters
        'If bs IsNot Nothing And Not Me.HasSKU Then
        '    'this is an 'open','category' branch (eligible for quick filters)

        '    'this is the 'show filters' button the hide filters is in the matrixheader itself
        '    If bi.MatrixHeader Is Nothing OrElse bi.MatrixHeader.QuickFiltersVisible = False Then

        '        If bi.Morethan(5) Then
        '            Dim bid As String = "hmcb." & bi.path  'just needs a unique DIV id (serves no other purpose)
        '            Dim lit As New Literal
        '            lit.Text = Replace("<div id=|" & bid & "| class=|hmc hpBlueButton ib showHMC| onclick=|getBranches('cmd=quickFilter&path=" & bi.path & "');return false|> Quick Filter </div>", "|", Chr(34))
        '            BranchHeader.Controls.Add(lit)
        '        End If
        '    End If

        'End If

    End Function

    Private Function TROitem(bi As clsBranchInfo, ByRef errormessages As List(Of String)) As Panel

        TROitem = New Panel
        TROitem.CssClass = "TROitem"

        Dim lt As Literal = New Literal
        Dim desc$ = ""
        If Me.Product Is Nothing Then
            errormessages.Add("* Cannot render a productless branch as a TRO Item (I)")
        Else
            If Me.Product.i_Attributes_Code.ContainsKey("desc") Then
                Dim da As clsProductAttribute
                da = Me.Product.i_Attributes_Code("desc")(0)
            End If

            Dim attribute As clsAttribute = iq.i_attribute_code("desc")

            Dim att As clsProductAttribute = (From at In Me.Product.Attributes.Values Where at.Attribute Is attribute).FirstOrDefault

            Dim cpkDesc As String

            If att IsNot Nothing AndAlso att.Translation IsNot Nothing AndAlso att.Translation.text(bi.buyerAccount.Language) IsNot Nothing Then
                cpkDesc = att.Translation.text(bi.buyerAccount.Language)
            Else
                cpkDesc = Me.Product.DisplayName(bi.buyerAccount.Language, True)

            End If

            lt.Text = "<span class='TROpartNum'>" & Me.SKU & "</span>&nbsp;<span class='TROdesc'>" & cpkDesc & "</span>"
            ' lt.Text = "<span class='TROdesc'>" & Me.Product.DisplayName(bi.BuyerAccount.Language) & "</span>"
            TROitem.Controls.Add(lt)

            TROitem.Controls.Add(Me.BuyUI(bi.buyerAccount, bi.path, bi.lid))

            ' Code commented as a bug has been raised by Gregg
            'If UserIsAdmin(bi.lid) Then

            '    For Each q In bi.branch.Quantities.Values
            '        Dim lit As Literal = New Literal
            '        lit.Text = "<Span>(" & q.Region.Code & ")</span>"
            '        TROitem.Controls.Add(lit)
            '    Next
            'End If

        End If


    End Function


    Public Shared Function Breadcrumbs(lid As UInt64, path As String, language As clsLanguage, ByRef errormessages As List(Of String)) As Literal

        'for everything on the path above here that is a breadcrumb, draw one (until we reach a non breadcrumb)

        ' Dim branchStates As Dictionary(Of String, clsBranchState) = CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))

        Dim paradigm As enumParadigm = CType(iq.sesh(lid, "Paradigm"), enumParadigm)

        Dim l$ = "<div id='bcHolder'>"
        Dim pth = ""
        Dim agentAccount As clsAccount = iq.sesh(lid, "AgentAccount")




        Dim segs() As String = Split(path, ".")

        Dim slash As String = " ► "

        'Dim abovesystem As Boolean = True
        For Each seg In segs
            If seg = "1" And segs.Length = 2 Then Exit For
            '            If seg <> segs.Last Then

            ' If paradigm = enumParadigm.configuringSystem And iq.Branches(CInt(seg)).hassystem Then l$ &= "<div class>"

            pth &= seg
            If pth <> "tree" Then
                If iq.Branches(seg).Product IsNot Nothing Then
                    If iq.Branches(seg).Product.isSystem Then Exit For
                End If
                Dim bs As String = "cmd=open&path=" & pth & "&configuration=0&Paradigm=B"
                bs &= "&into=tree"
                l$ &= Replace("<div class='breadcrumbs' onclick=|" & ButtonScript(bs) & "|>&nbsp;" & If(seg = segs(1), String.Empty, slash$) & iq.Branches(CInt(seg)).Translation.text(language).Replace("[mfr]", agentAccount.mfrCode) & "</div>", "|", Chr(34))

            End If

            pth &= "."



            ' End If
        Next

        Breadcrumbs = New Literal
        Breadcrumbs.Text = l$ & "</div>"


    End Function

    ''' <summary>
    ''' Returns the prefix plus first segment of the path - eg tree.1
    ''' </summary>
    ''' <param name="path"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function rootOf(path$, ByRef errorMessages As List(Of String)) As String

        Dim segs() As String = Split(path$, ".")
        If segs.Count >= 2 Then
            Return segs(0) & "." & segs(1)
        Else
            errorMessages.Add("could not find rootOf '" & path$ & "'")
            Return "tree.1"
        End If

    End Function

    Private Shared Function StateOfFirstDescendantPresent(lid As UInt64, values As IEnumerable(Of clsVisibility)) As clsBranchState

        'This is not very intuitive - but, The situation arises where we switch an already opened page/branch to 'show all' (admin mode)
        'whereupon we must determine it's 'mode' (from how its first child has been rendered)
        'only (in this scenario) it's first child *was (potentially) never* rendered (as it was previously hidden by some restriction (geography, not in feed etc)
        'Because it wasn't rendered - no session variable for it (it's branchstate at that path) exists
        'so we need to iterate until we find a branch that *was* renedered (and would prior to the switch to admin mode) have been the one on display

        StateOfFirstDescendantPresent = Nothing

        Dim branchStates As Dictionary(Of String, clsBranchState) = CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
        For Each c In values
            If branchStates.ContainsKey(c.path) Then
                Return branchStates(c.path)
            End If
        Next

    End Function


    Private Function skudDescendantIDs() As List(Of Integer)

        skudDescendantIDs = New List(Of Integer)
        If Me.HasSKU Then skudDescendantIDs.Add(Me.ID) : Exit Function 'We've hit a SKU stop recursing

        For Each child In Me.childBranches.Values
            skudDescendantIDs.AddRange(child.skudDescendantIDs)
        Next

    End Function

    Public Function SquareUI(bi As clsBranchInfo, ByRef hideReasons As List(Of String), numSKUs As Integer, numCats As Integer, lid As UInt64, Optional view As DataView = Nothing, Optional ByRef IsMinimised As Boolean = False) As PlaceHolder



        Dim errormessages As List(Of String) = New List(Of String)
        Dim acct As clsAccount = iq.sesh(lid, "BuyerAccount")

        SquareUI = New PlaceHolder
        Dim tp As Panel
        tp = Me.Title(bi, True, False, False, numSKUs, numCats, hideReasons, errormessages)

        tp.Controls.Add(Me.PromoIndicators(bi, errormessages)) 'Put the promo indicators on squares too

        SquareUI.Controls.Add(tp)

        '        Dim spacer As Panel = New Panel
        '       spacer.Attributes("style") = "height:.7em;"
        '      SquareUI.Controls.Add(spacer)

        'extract from productinfo

        Dim ppnl As Panel = New Panel  'add the image in a DIV (so its forced onto a new line)
        If Me.Picture <> "" Then

            '; ppnl.BackColor = Drawing.Color.Aquamarine

            Dim img As New WebControls.Image
            img.ImageUrl = imagebase & Me.Picture
            img.CssClass = "squarePhoto"
            img.Attributes.Add("onerror", "this.src='/images/navigation/redBlob.png';")

            ppnl.Controls.Add(img)
            'Don't add this until later incase we have family counts turned on (complex square)

        End If

        'familyFinder/HeaderSquares roll up (of what's in the view)
        Dim counts As Literal
        Dim attb As List(Of Literal) = New List(Of Literal)()
        Dim promopanel As Panel = New Panel()
        Dim pbs As clsBranchState = getBranchStateAbove(bi.lid, bi.path, errormessages)
        If pbs IsNot Nothing AndAlso pbs.rca = enumBt.DetailSquare Then
            Dim descendants As List(Of Integer) = Me.skudDescendantIDs  'this is a very simple (and fast) list of ALL the (first) SKUd descendants of this branch - it is intersected (ANDed) with the Matrix's Views Datatable - which has had more robust visibility checking

            'used to find the range (min-max) of each of the colums we want to roll up
            Dim ranges As Dictionary(Of String, clsRange) = New Dictionary(Of String, clsRange) 'lazy re-use of a structure used elsewhere 
            'Dim descendantsobj As Dictionary(Of clsBranch, clsVisibility) = bi.visibleChildren(errormessages, True, 0, 0, True)
            'bi.CreateMatrixHeader(descendantsobj, True) 'this creates the clsmatrix header AND stores it in the users session

            If bi.EffectiveHeader IsNot Nothing Then

                For Each col In bi.EffectiveHeader.FieldResultSet.Keys.Where(Function(d) d.visibleSquare)
                    'If col.visibleList Then
                    ranges.Add(col.propertyName, New clsRange()) 'Int64.MaxValue, Int64.MinValue))
                    ' End If
                Next col

                'The instersection of the (unfiltered) datatable, and the descendants - tell us the number of (unfiltered, visible products)
                'The intersection of the (filtered) VIEW and the Descendant branches - tells us the number of matches

                Dim Count As Integer = 0, matches As Integer = 0

                'Scan ALL the UNFILTERED rows to get the of unfiltered attributes
                For Each dr As DataRow In bi.EffectiveHeader.Vw.Table.Rows '. Item(ID)   'makes more sense to iterate over the (filtered) VIEW - iq.Translations(dr.Item(colname)).text(acct.Language) + If(col.LinkedFieldID IsNot Nothing, If(dr.Item(iq.Fields(col.LinkedFieldID).propertyName) <> UInt64.MinValue, String.Format("({0})", dr.Item(iq.Fields(col.LinkedFieldID).propertyName)), String.Empty), String.Empty) = ""ONCE than run many datatable.select min(),Max() queries
                    If descendants.Contains(CInt(dr("id"))) Then  'Does this row descend from this squares branch ..
                        Count += 1
                        For Each colname In ranges.Keys
                            Dim col As clsField = bi.EffectiveHeader.screen.i_field_property(colname)

                            If bi.EffectiveHeader.Vw.Table.Columns.Contains(colname) AndAlso (Not IsDBNull(dr.Item(colname)) AndAlso dr.Item(colname) <> Int64.MaxValue AndAlso dr.Item(colname) <> Int64.MinValue) Then

                                ranges(colname).stretch(bi.EffectiveHeader.FieldResultSet(col).ConvertValueToUnit(dr.Item(colname), If(dr.Item(colname + "UNIT") Is DBNull.Value, Nothing, CDbl(dr.Item(colname + "UNIT")))))  'updates the min and max of the range pertaining to this column
                                ranges(colname).UnitText = If(dr.Item(colname + "UNIT") Is DBNull.Value, String.Empty, iq.Units(dr.Item(colname + "UNIT")).Symbol)
                                If col.InputType.code <> "int32" And col.InputType.code <> "single" And col.InputType.code <> "nullint" Then
                                    If ranges(colname).TextRepresentation Is Nothing OrElse ranges(colname).TextRepresentation = iq.Translations(dr.Item(colname)).text(acct.Language) + If(col.LinkedFieldID IsNot Nothing, If(CType(dr.Item(iq.Fields(col.LinkedFieldID).propertyName), Long) <> Long.MinValue, String.Format(" ({0}{1})", dr.Item(iq.Fields(col.LinkedFieldID).propertyName), iq.Units(dr.Item(iq.Fields(col.LinkedFieldID).propertyName + "UNIT")).Symbol), String.Empty), String.Empty) Then
                                        ranges(colname).TextRepresentation = iq.Translations(dr.Item(colname)).text(acct.Language) + If(col.LinkedFieldID IsNot Nothing, If(CType(dr.Item(iq.Fields(col.LinkedFieldID).propertyName), Long) <> Long.MinValue, String.Format(" ({0}{1})", dr.Item(iq.Fields(col.LinkedFieldID).propertyName), iq.Units(dr.Item(iq.Fields(col.LinkedFieldID).propertyName + "UNIT")).Symbol), String.Empty), String.Empty)
                                    Else
                                        ranges(colname).IsMixed = True
                                        ranges(colname).TextRepresentation = Nothing
                                    End If
                                End If
                            End If
                        Next
                    End If
                Next

                'count the matches (survivors in this family)
                For Each dr As DataRowView In bi.EffectiveHeader.Vw '
                    If descendants.Contains(CInt(dr("id"))) Then
                        matches += 1
                    End If
                Next

                If matches = 0 Then IsMinimised = True

                counts = NewLit("<div style=""text-align:center;""><div class='familyCount'>" & matches & " of " & Count & " " & Xlt("match", bi.agentAccount.Language) & "</div></div>")

                If matches = Count Then counts = Nothing 'remove counts on matches

                attb.Add(NewLit("<div style=""padding-top:2px;"">"))
                'output the range of each attibute
                For Each colname In ranges.Keys
                    If ranges(colname).max = Int64.MinValue Or ranges(colname).min = Int64.MaxValue Then
                        Continue For
                    End If

                    Dim col As clsField = bi.EffectiveHeader.screen.i_field_property(colname)

                    If col.propertyName.ToLower = "customerprice" Then
                        Dim MinP As String, MaxP As String
                        MinP = New NullablePrice(ranges(colname).min / 100, bi.buyerAccount.Currency, False).text(bi.buyerAccount, errormessages)
                        MaxP = New NullablePrice(ranges(colname).max / 100, bi.buyerAccount.Currency, False).text(bi.buyerAccount, errormessages)
                        If ranges(colname).min <> Int64.MinValue And ranges(colname).max <> Int64.MaxValue Then
                            If ranges(colname).min = ranges(colname).max Then
                                attb.Add(NewLit("<p><span class='bold'>" & col.labelText.text(bi.agentAccount.Language) & " :</span> " & MinP & "</p>"))
                            Else
                                attb.Add(NewLit("<p><span class='bold'>" & col.labelText.text(bi.agentAccount.Language) & " :</span> " & MinP & " to " & MaxP & "</p>"))
                            End If

                        End If
                    ElseIf col.InputType.code = "int32" Or col.InputType.code = "single" Or col.InputType.code = "nullint" Then
                        If ranges(colname).min <> Int64.MinValue And ranges(colname).max <> Int64.MaxValue Then
                            If ranges(colname).min = ranges(colname).max Then
                                attb.Add(NewLit("<p><span class='bold'>" & col.labelText.text(bi.agentAccount.Language) & " :</span> " & ranges(colname).min & ranges(colname).UnitText & "</p>"))
                            Else
                                If ranges(colname).min = 0 Then
                                    attb.Add(NewLit("<p><span class='bold'>" & col.labelText.text(bi.agentAccount.Language) & " :</span> up to " & ranges(colname).max & ranges(colname).UnitText & "</p>"))
                                Else
                                    'attb.Add(NewLit("<p><b>" & col.labelTextLanguage & " :</b> " & ranges(colname).min & bi.EffectiveHeader.FieldResultSet(col).DisplayUnitSymbol & " to " & ranges(colname).max & bi.EffectiveHeader.FieldResultSet(col).DisplayUnitSymbol & "</p>"))
                                    attb.Add(NewLit("<p><span class='bold'>" & col.labelText.text(bi.agentAccount.Language) & " :</span> " & ranges(colname).min & ranges(colname).UnitText & " to " & ranges(colname).max & ranges(colname).UnitText & "</p>"))
                                End If

                            End If
                        End If
                    Else
                        If Not ranges(colname).IsMixed Then
                            attb.Add(NewLit("<p><span class='bold'>" & col.labelText.text(bi.agentAccount.Language) & "</span> : " & ranges(colname).TextRepresentation & " </p>"))
                        Else
                            attb.Add(NewLit("<p><span class='bold'>" & col.labelText.text(bi.agentAccount.Language) & "</span> : " & Xlt("Mixed", acct.Language) & "</p>"))
                        End If
                    End If
                Next

                attb.Add(NewLit("</div>"))

            End If
        Else
            'Simple Square, add promos
            If iq.i_PromoRegions.ContainsKey(bi.buyerAccount.BuyerChannel.Region) Then
                promopanel.CssClass = "square_promoPanel"
                Dim t As String = ""
                For Each promo In iq.i_PromoRegions(bi.buyerAccount.BuyerChannel.Region)
                    If iq.i_PromoSystemTypes.ContainsKey(promo) AndAlso iq.i_PromoSystemTypes(promo).Contains(Me.Translation.text(English)) Then
                        t &= "<li onclick=""burstBubble(event);getBranches('cmd=openSquare&path=" & bi.path & "&into=tree&promoLink=" & promo.Id & "');"" class='square_promo square_promo_" & promo.Code & "'>" & promo.displayName(bi.buyerAccount.Language) & "</li>"
                    End If
                Next
                If Not String.IsNullOrEmpty(t) Then
                    t = "<div class='square_promo_Header'><span>Promotions</span></div><ul>" & t
                    promopanel.Controls.Add(NewLit(t & "</ul>"))
                End If

            End If
        End If
        If counts IsNot Nothing AndAlso bi.ScreenHeader IsNot Nothing AndAlso bi.ScreenHeader.hasQuickFilters() Then
            SquareUI.Controls.Add(counts) 'add the counts into the title panel
        End If

        SquareUI.Controls.Add(ppnl)
        SquareUI.Controls.Add(promopanel)

        Dim lit As Literal
        If Product IsNot Nothing AndAlso (pbs Is Nothing OrElse pbs.rca <> enumBt.DetailSquare) Then
            If Product.i_Attributes_Code.ContainsKey("subTitle") Then
                lit = New Literal
                lit.Text = "<div class='ProdSubTitle liftBottom' style='clear:both'>" & Product.i_Attributes_Code("subTitle")(0).Translation.text(s_lang) & "</div>"
                SquareUI.Controls.Add(lit)
            End If

            If Product.i_Attributes_Code.ContainsKey("xNote") Then  'ProductNote from Products_UnionSytsems
                lit = New Literal
                lit.Text = "<div class='ProdSubTitle' style = 'clear:both'>" & Product.i_Attributes_Code("xText")(0).Translation.text(s_lang) & "</div>"
                SquareUI.Controls.Add(lit)
            End If
        End If

        If attb.Count > 0 Then
            Dim txt As New Panel()
            txt.CssClass = "SquareAttributePanel"
            txt.Style.Item("overflow") = "hidden"
            For Each l As Literal In attb
                txt.Controls.Add(l)
            Next
            SquareUI.Controls.Add(txt)
        End If

        ' SquareUI.Controls.Add(Me.PromoIndicators(bi, errormessages))
        If Me.HasSKU Then
            SquareUI.Controls.Add(Me.BuyUI(bi.buyerAccount, bi.path$, lid))
        End If

        OutputErrors(SquareUI.Controls, errormessages, bi.lid)

    End Function


    ''' <summary>
    ''' Renders the UI for a branch, and recurses for those (open) children with state
    ''' </summary>
    ''' <param name="bi"></param>
    ''' <param name="errorMessages"></param>
    ''' <returns></returns>
    ''' <remarks>State is stored in the branchstates dictionary of the users session</remarks>
    Public Function UI(ByVal bi As clsBranchInfo, ByRef EndPath As String, ByRef errorMessages As List(Of String)) As Panel

        Dim pbs As clsBranchState = getBranchStateAbove(bi.lid, bi.path, errorMessages)
        Dim bs As clsBranchState = getbranchstate(bi.lid, bi.path)  'this is my branchstae - !! which will be NOTHING if I am closed !!

        Dim hidereasons As List(Of String) = Me.ReasonsForHide(bi.buyerAccount, bi.foci, bi.path, bi.buyerAccount.SellerChannel.priceConfig, False, errorMessages)  'Calls GetPrices
        '   UI = New Panel

        If hidereasons.Count > 0 And bi.showAll = False Then

            UI = New Panel
            'UI.Controls.Add(NewLit("Hidden")) '*KW
            'UI.Controls.Add(outputMessages(hidereasons)) '*KW
            Exit Function

        End If

        'Dim maxFind As Integer
        'maxFind = 100 'If bi.branchState.state = oc.open Then maxFind = 10000 Else maxFind = 100
        'If pbs.rca = enumBt.gridrow Then maxFind = 1

        Dim segs() As String = Split(bi.path, ".")
        'If segs.Count < 3 And pbs.United = False Then maxFind = 1 'Above families we do not need counts (or to recurse down to systems - UNLESS we're 'united')

        If Me.Hidden AndAlso Not bi.showAll Then
            'a hidden branch (such as a chassis) will return an empty panel (which will not be rendered - becuase it's empty)
            UI = New Panel
            If bi.showAll Then UI.Controls.Add(ErrorDymo(Me.DisplayName(English)))
        Else

            Dim descendants As Dictionary(Of clsBranch, clsVisibility) = New Dictionary(Of clsBranch, clsVisibility)

            'Dim united As Boolean = False
            'If bs IsNot Nothing AndAlso bs.United Then united = True

            Dim skus As Integer = 0, cats As Integer = 0

            'descendants = bi.visibleChildren(errorMessages, united, skus, cats, fbv) '<<<THIS is where most of the action happens - if we dont need counts, we could do it only if the branch is open

            UI = Me.BranchHeader(bi, bs, pbs, hidereasons, skus, cats, errorMessages, bi.lid)

            If bs IsNot Nothing Then  'any branch with state is OPEN we never render the body of closed branches

                'This filters the visible children against the view in effect (and returns an ordered list)
                Dim fbv As Boolean = True
                If Me.HasSKU Then fbv = False 'stop filtering by view when we're 'at' a system
                'If Me.Product IsNot Nothing AndAlso Me.Product.isSystem Then fbv = False 'stop filtering by view when we're 'at' a system
                If bi.treeMode Then fbv = False
                descendants = bi.visibleChildren(errorMessages, bs.United, skus, cats, fbv, True) '<<<THIS is where most of the action happens - if we dont need counts, we could do it only if the branch is open

                'render the contents of this - will include it's children
                'If this is tro then nest into the header for layout....
                If bs.rca = enumBt.TROitem Then
                    Dim toadd As Dictionary(Of Control, Control) = New Dictionary(Of Control, Control)()
                    Dim ctlarr As Control() = New Control(UI.Controls.Count - 1) {}
                    UI.Controls.CopyTo(ctlarr, 0)
                    For Each c In ctlarr
                        If c.ID = "troSectionImage" Then
                            toadd.Add(c, Me.BranchBody(bi, bs, pbs, UI, EndPath, descendants, errorMessages))

                        End If
                    Next
                    For Each t In toadd
                        t.Key.Controls.Add(t.Value)
                    Next
                Else
                    UI.Controls.Add(Me.BranchBody(bi, bs, pbs, UI, EndPath, descendants, errorMessages))  'BranchBody renders open sub branches recursively (according to thier 'renderas')
                End If
            End If
        End If

        'UI.Controls.Add(NewLit("<span style='background-color:green;color:white;'>" & CoreCode.iicalls & "</span>"))

        OutputErrors(UI.Controls, errorMessages, bi.lid)
        errorMessages.Clear()

        If bi.isTreeCursor Then 'And bs IsNot Nothing AndAlso bs.rca <> enumBt.Tab Then
            UI.CssClass &= " treeCursor"
        End If

    End Function

    Private Function rendertest(Panel As Control)
        Dim s As StringWriter = New StringWriter()
        Dim h As HtmlTextWriter = New HtmlTextWriter(s)
        Panel.RenderControl(h)
        s.ToString()
        h.Close()
        s.Close()
        h.Dispose()
        s.Dispose()
    End Function

    ''' <summary>Returns a panel for an open branch - contains the product info and the children (themselves either open or closed) for the branch
    ''' </summary>
    ''' <param name="bi"></param>
    ''' <returns></returns>
    ''' <remarks>A panel populated with UI (based on the type and state of the branch)</remarks>
    Private Function BranchBody(bi As clsBranchInfo, bs As clsBranchState, pbs As clsBranchState, parentPanel As Panel, ByRef EndPath As String, ByRef descendants As Dictionary(Of clsBranch, clsVisibility), ByRef errorMessages As List(Of String)) As Panel


        EndPath = bi.path
        BranchBody = New Panel

        If bs.rca = enumBt.Hidden Then Exit Function

        If bi.treeMode Then bs.rca = enumBt.Branch ' Force tree mode to branch, would be nicer to do this on .open methods so the switcher can be used but that will take more time (and requires that the branch as B in its rca)  - ML

        BranchBody.ID = bi.path$ & ".body" 'this is a matrix (or set of child branches)

        If bs.rca = enumBt.Upsell Then
            BranchBody.CssClass &= " tabIndent tabBody dropShadow ib upsellBody" 'Yuck
        ElseIf bs.rca <> enumBt.OpenSquare Then

            'need to create a matrix??

            Dim mh As Dictionary(Of String, clsScreenHeader) = iq.sesh(bi.lid, "screenHeaders")

            If Not mh.ContainsKey(bi.path) Then
                Dim skus = 0
                descendants = bi.visibleChildren(errorMessages, bs.United, skus, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                If skus > 0 AndAlso MatrixAbove(bi.lid, bi.path) IsNot Nothing Then bi.CreateMatrixHeader(descendants, True) 'this creates the clsmatrix header AND stores it in the users session
            End If
            If mh.ContainsKey(bi.path.Substring(0, bi.path.LastIndexOf("."))) Then
                'If mh(bi.path.substring(0,bi.path.LastindexOf("."))).hasQuickFilters() Filters.Count 0  Then
                'create matrix
                'descendants = bi.visibleChildren(errorMessages, bs.United, 0, 0, False, True)  'get the United results (SKUd descendants)    Neither VisibleChildren, Nor ReasonsForHide Call the webservice
                'If Not mh.ContainsKey(bi.path) AndAlso (bs.United Or (descendants.Count > 0 AndAlso descendants.First().Value.branch.HasSKU())) Then ' only generate a mh if there are any products to show
                ' bi.CreateMatrixHeader(descendants, True) 'this creates the clsmatrix header AND stores it in the users session
                ' bi.ScreenHeader.QuickFiltersVisible = False
                'End If

                'End If
            End If

            'Quick filters
            If bs IsNot Nothing And Not Me.HasSKU AndAlso bi.PathLevel <> 1 AndAlso (Not bi.branch.Product Is Nothing) AndAlso (Not bi.branch.Product.isSystem(bi.path)) Then
                'this is an 'open','category' branch (eligible for quick filters)

                'this is the 'show filters' button the hide filters is in the matrixheader itself
                If bi.ScreenHeader Is Nothing Then
                    'do or can we have filters here?
                    If Me.Matrix IsNot Nothing AndAlso Me.Matrix.Fields.ToList().Where(Function(f) f.Value.QuickFilterGroup IsNot Nothing).Count() > 0 Then
                        If bi.MoreThanXskus(5) Then 'need to remove blank filters if posible

                            Dim bid As String = "hmcb." & bi.path  'just needs a unique DIV id (serves no other purpose)
                            Dim lit As New Literal
                            lit.Text = Replace("<div class=|quickFilterGroupHolder|><div id=|" & bid & "| class=|hmc hpBlueButton ib showHMC| onclick=|getBranches('cmd=quickFilter&path=" & bi.path & "');return false|> " & Xlt("Filter", bi.buyerAccount.Language) & "</div></div>", "|", Chr(34)) ' ML Removed <div class='clear'></div> as this was adding a huge space in scenaro, add quote click on earlier breadcrumb
                            BranchBody.Controls.Add(lit)
                        End If
                    End If
                End If
            End If


            If pbs.rca = enumBt.Tab Then
                bi.treeWidth = bi.treeWidth - 1
                BranchBody.CssClass &= " tabIndent tabBody dropShadow ib"
            ElseIf pbs.rca = enumBt.Branch Then
                bi.treeWidth = bi.treeWidth - 2.25
                BranchBody.CssClass &= " treeIndent"
            End If

            If bi.ScreenHeader IsNot Nothing AndAlso Not Me.isOption(bi.path) Then
                Dim pnlMatrixHeader As Panel = New Panel

                'Dim tw As Label = New Label
                'tw.Text = bi.treeWidth
                'tw.BackColor = Drawing.Color.Green
                'tw.ForeColor = Drawing.Color.White
                'pnlMatrixHeader.Controls.Add(tw)
                BranchBody.Controls.Add(pnlMatrixHeader)

                bi.ScreenHeader.CollapseColumns(bi.treeWidth, errorMessages)

                pnlMatrixHeader.Controls.Add(bi.ScreenHeader.UI(bi, errorMessages, bi.lid))
            End If
        End If
        'BranchBody.Controls.Add(NewLit("<span style='background-color:yellow;'>P:" + (bi.treeWidth * 12).ToString() + ",E:" + bi.treeWidth.ToString() + "</span>"))
        If (Not Me.isOption(bi.path) Or AccountHasRight(bi.lid, "DIAGVIEW")) Then

            If Me.Product IsNot Nothing Then
                'was branchbody
                If Product.hasSKU Then
                    parentPanel.Controls.Add(ProductInfo(bi, errorMessages)) '.BuyerAccount, bi.path$, bi.AgentAccount.Language, True, bi.lid)) '   skud, matrixBranch, filters, sorts, showPriceInBody))
                End If

                If Me.Product.isSystem(bi.path) Then
                    If showOptions(bi) = False Then Exit Function
                End If

                ' BranchBody.Controls.Add(Me.SlotsTable)
            Else
                'If Me.slots.Count Then Stop
                'If Me.Quantities.Count Then Stop
            End If

            If Not Me.Product Is Nothing Then
                If Not Me.Product.Bundles Is Nothing Then
                    For Each bundle In Me.Product.Bundles.Values
                        If bundle.Region.Encompasses(bi.buyerAccount.SellerChannel.Region) Then
                            BranchBody.Controls.Add(bundle.UI)
                        End If
                    Next
                End If
            End If

            'If CType(iq.sesh(bi.lid, "paradigm"), enumParadigm) = enumParadigm.configuringSystem Or (iq.sesh(bi.lid, "configuring") IsNot Nothing AndAlso bi.PathLevel = iq.sesh(bi.lid, "configuring").ToString().Split(".").Length - 2) Then
            '    For Each d In descendants.ToList()
            '        If d.Value.path <> bi.buyerAccount.Quotes(iq.sesh(bi.lid, "QuoteID")).RootItem.Children(0).Path Then descendants.Remove(d.Key) 'iq.sesh(bi.lid, "configuring") Then descendants.Remove(d.Key)
            '    Next
            'End If

            For Each d In descendants.ToList()
                If CType(iq.sesh(bi.lid, "Paradigm"), enumParadigm) = enumParadigm.configuringSystem Then
                    If bs.rca = enumBt.invisibLe Then
                        descendants.Remove(d.Key)
                    End If
                End If
            Next

            If bs.rca = enumBt.gridrow Then
                parentPanel.CssClass &= " openGrid dropShadow faintBorder ib"
            End If

        End If
        If descendants.Count = 0 Then
            If Not Me.isOptionOrOptionHolder(bi.path) Then
                BranchBody.Controls.Add(If(UserIsAdmin(bi.lid), NewLit("No visible descendants"), NewLit(Xlt("No Results", bi.buyerAccount.Language))))
            Else
                BranchBody.Controls.Add(NewLit(""))
            End If
        Else
            'we're about to render a set of descendant branches - based on the type of the first branch we may need a header
            '(matrix view and tabs view being the main cases)
            Select Case bs.rca
                Case Is = enumBt.gridrow
                    'BranchBody.CssClass &= " dropShadow faintBorder matrix"
                    BranchBody.CssClass &= " matrix"

                Case Is = enumBt.Tab, enumBt.hYperlink, enumBt.bighyperlinK
                    If bs.rca = enumBt.Hidden Or bi.treeMode Then  'Me.Product IsNot Nothing AndAlso Me.Product.isSystem AndAlso bi.Paradigm = enumParadigm.AddingSystem Then
                        'we don't show tabs on systems in addingsystem mode
                    Else
                        BranchBody.Controls.Add(Me.RenderTabHeads(bi, bs, descendants, errorMessages))
                    End If
            End Select

            If descendants.Count Then
                Me.renderChildren(bi, bs, (bs.rca = enumBt.gridrow), EndPath, descendants, errorMessages, BranchBody) ', pnlHeadsquares)
            End If
        End If

    End Function
    ''' <summary>
    ''' This function is important!! If the if the product is in the basket and the paradigm is not equal to addsystem or branchinfo.paradigm = to configuringSystem or BranchInfo.treemode = true want to show options otherwise dont and return false.
    ''' </summary>
    ''' <param name="bi">An instance of BranchInfo.</param>
    ''' <returns>A boolean value.</returns>
    ''' <remarks></remarks>
    Private Function showOptions(bi As clsBranchInfo) As Boolean

        If (clsQuote.CurrentQuoteContains(bi.lid, Me.Product) AndAlso bi.Paradigm <> enumParadigm.AddingSystem) OrElse (bi.Paradigm = enumParadigm.configuringSystem OrElse bi.treeMode) Then Return True
        Return False

    End Function

    Public Function SystemOPGsProdRefs(path$) As List(Of String)

        SystemOPGsProdRefs = New List(Of String)
        Dim segs$() = Split(path$, ".")
        Dim branch As clsBranch
        For i = UBound(segs) To 1 Step -1
            branch = iq.Branches(CInt(segs(i)))
            If branch.Product IsNot Nothing Then
                If branch.Product.isSystem Then
                    For Each av In branch.Product.AvalancheOPGs.Values
                        For Each o In av.getAvalancheOptions
                            SystemOPGsProdRefs.Add(o.ProdRef)
                        Next
                    Next
                End If
            End If
        Next

    End Function

    Public Function hasFlexAttach(buyeraccount As clsAccount, path$, foci As HashSet(Of String), ByRef errormessages As List(Of String)) As Boolean

        Dim priceconfig As Int32 = buyeraccount.SellerChannel.priceConfig And Not 8 'DON'T Check the webservice for a price when checking for flew attach

        hasFlexAttach = False
        If iq.PromoBranches.ContainsKey(buyeraccount.BuyerChannel) AndAlso iq.PromoBranches(buyeraccount.BuyerChannel).ContainsKey("F") AndAlso iq.PromoBranches(buyeraccount.BuyerChannel)("F").Contains(Me) Then
            'It's possible the actual promo branches are pruned (or 'defocussed' eg.recetta view)  in this context - so we must recurse/check
            Dim newpath$ = ""
            Dim sysbranch = Me.FindSystemAbove2(path$, newpath$)
            If sysbranch IsNot Nothing AndAlso Not sysbranch.hasFlexAttach(buyeraccount, newpath$, foci, errormessages) Then Return False
            If Me.checkPromo(buyeraccount, path$, foci, "F", priceconfig, errormessages) Then
                hasFlexAttach = True
            End If
        End If

    End Function

    Public Function hasAvalanche(buyerchannel As clsChannel, path$) As Boolean

        hasAvalanche = False
        If iq.PromoBranches.ContainsKey(buyerchannel) Then
            If iq.PromoBranches(buyerchannel).ContainsKey("A") Then
                If iq.PromoBranches(buyerchannel)("A").Contains(Me) Then
                    If Not ContainsSystem(path) Then
                        hasAvalanche = True 'Above the system level - we can work based on the precalculated promobranches - below we need to worry about grafts
                    Else
                        If Me.Product IsNot Nothing Then
                            If Me.Product.isSystem Then
                                hasAvalanche = True
                            End If
                        End If
                        If hasAvalanche = False Then
                            Dim Prodrefs As List(Of String) = SystemOPGsProdRefs(path$) 'walks up the path to the system.. returns a list of the prodrefs for qualifying OPG options
                            hasAvalanche = Me.DescendantProductHasProdrefIn(Prodrefs)
                        End If
                    End If
                End If
            End If
        End If

    End Function

    ''' <summary>Returns a placeholder with UI (Letter indicators with tooldtips) for Promtions available under this branch</summary>
    Public Function PromoIndicators(Buyeraccount As clsAccount, Agentaccount As clsAccount, path$, foci As HashSet(Of String), inBasket As Boolean, greyed As Boolean, ByRef errorMessages As List(Of String), bi As clsBranchInfo) As PlaceHolder

        PromoIndicators = New PlaceHolder
        Dim lblAvalanche As Label = Nothing
        Dim lblBundle As Label = Nothing
        Dim lblFlex As Label = Nothing


        If Me.hasAvalanche(Buyeraccount.BuyerChannel, path$) Then
            lblAvalanche = New Label
            With lblAvalanche
                .Text = "*"
                .ToolTip = Xlt("Avalanche rebates available", Buyeraccount.Language)
                .CssClass = "OrangeStar"
                If bi IsNot Nothing AndAlso bi.PathLevel < 4 Then .Attributes.Add("onclick", "burstBubble(event);getBranches('cmd=promofilter&path=" + path$ + "&promoType=*&into=tree');return false;")
                If inBasket Then .CssClass &= " basketAvalanche"
                If greyed Then .CssClass &= " greyedPromo"
                PromoIndicators.Controls.Add(lblAvalanche)
            End With
        End If

        If iq.PromoBranches.ContainsKey(Buyeraccount.BuyerChannel) AndAlso iq.PromoBranches(Buyeraccount.BuyerChannel).ContainsKey("B") AndAlso iq.PromoBranches(Buyeraccount.BuyerChannel)("B").Contains(Me) Then
            lblBundle = New Label
            With lblBundle
                .Text = "O"
                .ToolTip = Xlt("Promotional bundles available", Buyeraccount.Language)
                .CssClass = "bundleCircle "
                If bi IsNot Nothing AndAlso bi.PathLevel < 4 Then .Attributes.Add("onclick", "burstBubble(event);getBranches('cmd=promofilter&path=" + path$ + "&promoType=O&into=tree');return false;")
                PromoIndicators.Controls.Add(lblBundle)
                If inBasket Then .CssClass &= " basketBundleCircle"
                If greyed Then .CssClass &= " greyedPromo"

            End With
        End If

        If Me.hasFlexAttach(Buyeraccount, path$, foci, errorMessages) AndAlso Me.rca <> "DGB" Then
            lblFlex = New Label
            With lblFlex
                .Text = "F"
                .ToolTip = Xlt("Flex rebates available", Buyeraccount.Language)
                .CssClass = "flexF"
                If bi IsNot Nothing AndAlso bi.PathLevel < 4 Then .Attributes.Add("onclick", "burstBubble(event);getBranches('cmd=promofilter&path=" + path$ + "&promoType=F&into=tree');return false;")
                If inBasket Then .CssClass &= " basketFlexF"
                If greyed Then .CssClass &= " greyedPromo"
            End With
            PromoIndicators.Controls.Add(lblFlex)

        End If

        'Loyalty Points

        ' Loyalty points have been commented out because these are now shown 
        ' at the top of the quote section.

        'If Me.Product IsNot Nothing Then
        '    If Me.Product.Points.Count > 0 Then
        '        For Each scheme In Me.Product.Points.Keys
        '            If scheme.Region.Encompasses(Agentaccount.SellerChannel.Region) Then
        '                Dim lblpoints As Label
        '                lblpoints = New Label
        '                lblpoints.BackColor = Drawing.Color.HotPink
        '                lblpoints.ForeColor = Drawing.Color.White
        '                lblpoints.Text = Product.Points(scheme).ToString.Trim
        '                lblpoints.ToolTip = scheme.displayName(Agentaccount.Language) & " points"
        '                PromoIndicators.Controls.Add(lblpoints)
        '                Dim lit As Literal = New Literal
        '                lit.Text = "&nbsp;"
        '                PromoIndicators.Controls.Add(lit)
        '            End If
        '        Next
        '    End If
        'End If


    End Function
    ''' <summary>'double' checks that there is a descendant (NOT pruned) promotional branch of the specified type)</summary>
    ''' <param name="path"></param>
    ''' <param name="Type">The type of promotion</param>
    ''' <returns></returns>
    ''' <remarks>Having tagged the promoBranches, we now only need recurse the tagged ones which *might* (probably) have a descendant promo branch (unless its pruned in this context)</remarks>
    Public Function checkPromo(buyeraccount As clsAccount, path$, foci As HashSet(Of String), Type As String, priceconfig As Int32, ByRef errormessages As List(Of String)) As Boolean

        checkPromo = False
        If Me.PruneInForce(path, buyeraccount.SellerChannel) = 0 Then

            Dim recurse As Boolean = False
            If Me.Product Is Nothing Then
                recurse = True 'recurse
            Else
                'if there's a Price... (the product is visible to this user)
                recurse = CBool(Me.ReasonsForHide(buyeraccount, foci, path, priceconfig, True, errormessages).Count = 0)  'IMPORTANT don't call the webservice
            End If

            If recurse Then
                If iq.PromoBranches(buyeraccount.BuyerChannel)(Type).Contains(Me) AndAlso Not Me.Product Is Nothing Then
                    checkPromo = True
                    Exit Function
                Else
                    'this branch does not have a promo (of this type) on it , we don't recurse through a SKUD part (ie, we stop at the system, or the first option encountered)
                    If Me.Product IsNot Nothing Then
                        If Me.Product.hasSKU Then
                            checkPromo = False
                            Exit Function
                        End If
                    End If
                End If

                For Each ch In Me.childBranches.Values
                    If iq.PromoBranches(buyeraccount.BuyerChannel)(Type).Contains(ch) Then  'trivially check this branch *might* have a promo of the requisite type before recursing into it
                        If ch.checkPromo(buyeraccount, path$ & "." & ch.ID, foci, Type, priceconfig, errormessages) Then
                            checkPromo = True : Exit For
                        End If
                    End If
                Next
            End If
        Else
            checkPromo = False
        End If

    End Function
    Public Function PromoIndicators(Bi As clsBranchInfo, ByRef errormessages As List(Of String)) As PlaceHolder

        Return PromoIndicators(Bi.buyerAccount, Bi.agentAccount, Bi.path, Bi.foci, False, False, errormessages, Bi)

    End Function

    Public Function ContainsSystem(path$) As Boolean

        ContainsSystem = False
        Dim segs() As String = Split(path$, ".")
        For Each seg In segs
            If Val(seg) > 0 Then
                If iq.Branches(CInt(seg)).Product IsNot Nothing Then
                    If iq.Branches(CInt(seg)).Product.isSystem Then
                        Return True
                    End If
                End If
            End If
        Next

    End Function


    Public Function FindSystemAbove(path$, ByRef newpath$) As clsBranch
        newpath = path
        Dim segs() As String = Split(path$, ".")
        For i = segs.Count - 1 To 0 Step -1
            'For Each seg In segs
            If Val(segs(i)) > 0 Then

                If iq.Branches(CInt(segs(i))).Product IsNot Nothing Then
                    If iq.Branches(CInt(segs(i))).Product.isSystem Then
                        Return iq.Branches(CInt(segs(i)))
                    End If
                End If
                newpath = Left(newpath, InStrRev(newpath, ".") - 1)
            End If
        Next
        Return Nothing
    End Function

    Public Function FindSystemAbove2(path$, ByRef newpath$) As clsBranch
        newpath = path
        Dim segs() As String = Split(path$, ".")
        For i = segs.Count - 1 To 0 Step -1
            'For Each seg In segs
            If Val(segs(i)) > 0 Then

                newpath = Left(newpath, InStrRev(newpath, ".") - 1)

                If iq.Branches(CInt(segs(i))).Product IsNot Nothing Then
                    If iq.Branches(CInt(segs(i))).Product.isSystem Then
                        Return iq.Branches(CInt(segs(i)))
                    End If
                End If
            End If
        Next
        Return Nothing
    End Function

    Public Function DescendantProductHasProdrefIn(ProdRefs As List(Of String)) As Boolean

        'Recursivley checks wether any descendendant product of this branch has a ProdRef attribute value in the supplied list

        DescendantProductHasProdrefIn = False

        If Not Me.Product Is Nothing Then
            If Me.Product.i_Attributes_Code.ContainsKey("ProdRef") Then
                If ProdRefs.Contains(Me.Product.i_Attributes_Code("ProdRef")(0).Translation.text(English)) Then
                    Return True
                End If
            End If
        End If

        For Each b In Me.childBranches.Values
            'todo - don't recurse branches that have no avalanche optiosn on them
            If b.DescendantProductHasProdrefIn(ProdRefs) Then Return True
        Next

    End Function


    'Private Function ChildOpen(lid As UInt64, ByVal path$) As Boolean

    '    'Returns true if a child of the specified path is open

    '    ChildOpen = False

    '    Dim branchStates As Dictionary(Of String, clsBranchState) = CType(iq.sesh(lid, "branchStates"), Dictionary(Of String, clsBranchState))
    '    For Each p In branchStates.Keys
    '        If Left(p, Len(path$) + 1) = path$ & "." Then
    '            If branchStates(p).state = oc.open Then
    '                ChildOpen = True : Exit Function
    '            End If
    '        End If
    '    Next
    'End Function

    Private Function ProductInfo(bi As clsBranchInfo, ByRef errorMessages As List(Of String)) As Panel 'buyeraccount As clsAccount, path$, language As clsLanguage, showprice As Boolean, lid As UInt64) As Panel  ', skud As Boolean, matrixBranch As clsBranch, filter$, sort$, showprice As Boolean) As PlaceHolder

        Dim ui As Panel = New Panel
        'ui.Attributes("style") = "width:100%;margin-bottom:.5em;"
        ui.CssClass = "prodInfo"
        ' ui.CssClass = "treeIndent"

        Dim lit As Literal


        'If Bi.branchState.state = oc.open Then

        'If Product.i_Attributes_Code.ContainsKey("subTitle") Then
        '    lit = New Literal
        '    lit.Text = "<div class='ProdSubTitle'>" & Product.i_Attributes_Code("subTitle")(0).Translation.text(s_lang) & "</div>"
        '    ui.Controls.Add(lit)
        'End If

        If Product.i_Attributes_Code.ContainsKey("xNote") Then  'ProductNote from Products_UnionSytsems
            lit = New Literal
            lit.Text = "<div class='ProdSubTitle xXote'>" & Product.i_Attributes_Code("xText")(0).Translation.text(s_lang) & "</div>"
            ui.Controls.Add(lit)
        End If


        'the description is on the branch header- so not needed here (see gregs powerpoint)

        'If Product.i_Attributes_Code.ContainsKey("desc") Then
        '    lit = New Literal
        '    lit.Text = "<div class='prodDesc desc'>" & Product.i_Attributes_Code("desc")(0).Translation.text(s_lang) & "</div>"
        '    ui.Controls.Add(lit)
        'End If

        'the product photo is floated left of the desctiption and subtitle - so we need to clear the float
        ' lit = New Literal
        ' lit.Text = "<div style='height:0px;clear:both';>&nbsp;</div>"
        ' ui.Controls.Add(lit)

        If Me.Product.hasSKU Then 'we ONLY display prices on those products with a SKU (some products are placeholders)
            If Me.Product.isSystem(bi.path) Then
                ui.CssClass &= " isSystem"
            End If

            '  If showprice Then
            '          ui.Controls.Add(Me.PriceUI(buyeraccount, path$)) ' , skud, matrixBranch, filter, sort))
            '  End If
            Dim preinstalled As List(Of clsQuantity) = bi.branch.GetPreInstalledRecursive(bi.buyerAccount.SellerChannel.Region, bi.path, errorMessages)


            Dim st As Panel = ExpandablePanel(ui, "Specification", "st", bi)
            If st IsNot Nothing Then
                st.Controls.Add(Me.Product.Spectable(bi.buyerAccount.Language, preinstalled, bi.branch, bi.path, bi.showAll))
            End If
            'Dim spectable As Panel = Me.Product.Spectable(bi.buyerAccount.Language, preinstalled, bi.branch, bi.path)
            'spectable.ID = "spec." & bi.path

            ''spectable starts collapsed - so the collapse button is initially visible
            'Dim SpecHeader = New Panel()
            'SpecHeader.CssClass = "specHeader"
            'Dim btnCollapse = New Literal  ' when the collapseButton is pressed we will . . .
            'Dim omd$ = "$(this).toggleClass('collapsed');" 'toggle the + and -
            'omd$ &= "$(document.getElementById('spec." & bi.path$ & "')).toggle();"   'show the expand button
            'omd$ &= "return false;"  'supress the postback
            'btnCollapse.Text = Replace("<div id=|collapseSpec." & bi.path & "| class=|expandContract collapsed| onclick=|" & omd$ & "|>&nbsp;</div> ", "|", Chr(34))
            'SpecHeader.Controls.Add(btnCollapse)

            'SpecHeader.Controls.Add(NewLit("<span class='specHeader'>Specification</span>"))

            'ui.Controls.Add(SpecHeader)

            'ui.Controls.Add(spectable)

        End If

        'Dim hl As New HyperLink
        'hl.Target = "new"
        'hl.NavigateUrl = "edit.aspx?path=Products(" & Me.Product.ID & ").i_variants(Channels(" & buyeraccount.SellerChannel.ID & "))" & "&lid=" & lid
        'hl.Text = "Edit Price Variants"
        'ui.Controls.Add(hl)

        '     ui.Controls.Add(NewLit("<p>" & bi.path & "</p>"))

        ' If False Then

        If AccountHasRight(bi.lid, "DIAGVIEW") Then

            'Dim pth As Literal = New Literal
            'pth.Text = "<div style='background-color:magenta;color:white;'>" & bi.path & "</div>"
            'pth.Text &= "<div style='background-color:cyan;color:blue;'>" & PathName(bi.path) & "</div>"
            'ui.Controls.Add(pth)


            Dim ttl As String = "Slots"
            If Me.Product IsNot Nothing AndAlso Me.Product.isSystem Then ttl = "System Slots"
            Dim st As Panel = ExpandablePanel(ui, "Slots", "ss", bi)

            If st IsNot Nothing Then
                st.Controls.Add(NewLit("<div class='adminHelp'>Slots are attached to system and option branches - 'gives' slots are positive numbers - and generally appear on systems, 'takes' slots are negative numbers and (generally) appear against options.</div>"))
                st.Controls.Add(Me.SlotsTable(bi))
            End If

            'don't show 'chassis slots link (at all) on options
            If Me.Product IsNot Nothing AndAlso Me.Product.isSystem Then

                'locate the chassis branch
                Dim cbl = From b In Me.childBranches.Values Where b.slots.Count > 0 'Where b.EnglishName.ToLower.Contains("chassis")

                If cbl.Any Then 'is there a chassis branch
                    For Each b In cbl ' there shouls only be  1 !! 'Dim b As clsBranch = cbl.First
                        Dim cp As Panel = ExpandablePanel(ui, b.DisplayName(English) & " Slots (" & b.countGrafts & " models)", "cs" & b.ID, bi)
                        If cp IsNot Nothing Then
                            cp.Controls.Add(NewLit("<div class='adminHelp'>Slots which are common to a number of machines in a family are defined on the (sub) chassis</div>"))
                            Dim cbi As New clsBranchInfo(bi.lid, bi.path & "." & b.ID.ToString, bi.lblMatches, bi.treeWidth, bi.Paradigm, errorMessages)
                            cp.Controls.Add(b.SlotsTable(cbi))
                            '    Exit For 'there should be only 1 !
                        End If
                        '  Beep()
                    Next

                End If
            End If

            Dim qp As Panel = ExpandablePanel(ui, "Quantities", "qt", bi)
            If qp IsNot Nothing Then
                qp.Controls.Add(NewLit("<div class='adminHelp'>Quantities control, Regionalisaion, Pre-installed componentry and AutoAdds</div>"))
                qp.Controls.Add(Me.QuantitiesTable(bi.buyerAccount, bi, errorMessages))
            End If

            Dim ap As Panel = ExpandablePanel(ui, "Attributes", "at", bi)
            If ap IsNot Nothing Then
                ap.Controls.Add(NewLit("<div class='adminHelp'>Attributes hold core product information and form much of the 'spec table'</div>"))
                ap.Controls.Add(Me.AttributeTable(bi.buyerAccount, bi, errorMessages))
            End If

            Dim bp As Panel = ExpandablePanel(ui, "Branches", "tl", bi)
            If bp IsNot Nothing Then
                bp.Controls.Add(NewLit("<div class='adminHelp'>Shows other branches with this SKU/Product</div>"))
                bp.Controls.Add(Me.BranchesTable(bi)) 'AttributeTable(bi.buyerAccount, bi.path, errorMessages))
            End If

            'ui.Controls.Add(NewLit("<span class=""expandContract collapsed specHeader"" onclick=""burstBubble(event);$(this).next().next().toggle();$(this).toggleClass('collapsed');"">&nbsp;</span>"))
            'ui.Controls.Add(NewLit("<span class=""specHeader"">System Slots</span>"))
            'Dim t As Table = Me.SlotsTable(bi)
            't.Style("display") = "none"
            'ui.Controls.Add(t)

            'output the slots of the chassis too
            'ui.Controls.Add(NewLit("<span class=""expandContract collapsed specHeader"" onclick=""burstBubble(event);$(this).next().next().toggle();$(this).toggleClass('collapsed');"">&nbsp;</span>"))
            'ui.Controls.Add(NewLit("<span class=""specHeader"">Chassis Slots</span>"))
            'Dim p As Panel = New Panel()
            'p.Style("display") = "none"
            'For Each b In Me.childBranches.Values
            '    Dim cbi As New clsBranchInfo(bi.lid, bi.path & "." & b.ID.ToString, bi.lblMatches, bi.treeWidth, bi.Paradigm, errorMessages)
            '    p.Controls.Add(b.SlotsTable(cbi))
            'Next
            'ui.Controls.Add(p)

            '            ui.Controls.Add(NewLit("<span class=""expandContract collapsed specHeader"" onclick=""burstBubble(event);$(this).next().next().toggle();$(this).toggleClass('collapsed');"">&nbsp;</span>"))
            '           ui.Controls.Add(NewLit("<span class=""specHeader"">Preinstalled, quantityrestrictions & localisations)</span>"))
            '          ui.Controls.Add(Me.PreinstalledTable(bi.buyerAccount, bi.path, errorMessages))

            'ui.Controls.Add(NewLit("<span class=""expandContract collapsed specHeader"" onclick=""burstBubble(event);$(this).next().next().toggle();$(this).toggleClass('collapsed');"">&nbsp;</span>"))
            'ui.Controls.Add(NewLit("<span class=""specHeader"">Attributes</span>"))
            'ui.Controls.Add(Me.AttributeTable(bi.buyerAccount, bi.path, errorMessages))
        End If

        Return ui

    End Function
    ''' <summary>
    ''' Renders an expandable panel - who's (expanded/collapsed) state is maintained server side under the session LID
    ''' </summary>
    ''' <param name="addTo">Into which div should we place this expandadable panel</param>
    ''' <param name="title">Display title</param>
    ''' <param name="uniquizer">Short code, used along with bi.path to create a unique</param>
    ''' <param name="bi">the BI.LID and BI.Path are used internally</param>
    ''' <returns>A reference to the (empty) content panel - for you to add content to IF it's expanded or nothing</returns>



    Private Function ExpandablePanel(addTo As Panel, title As String, uniquizer As String, bi As clsBranchInfo) As Panel

        Dim outerPanel As Panel = New Panel
        '     outerPanel.CssClass = "ib"
        Dim contentPanel As Panel
        addTo.Controls.Add(outerPanel)

        Dim css As String
        Dim oc$
        Dim ky As String = "expanded_" & uniquizer & "_" & bi.path
        'determines whether a + or a - button shows
        If iq.SeshContains(bi.lid, ky) Then
            'we are currently expanded
            css = "expandContract specHeader"
            oc$ = ButtonScript("path=" & bi.path & "&cmd=collapsepanel&key=" & ky)
            contentPanel = New Panel

        Else
            'we are currently collapsed
            css = "expandContract collapsed specHeader"
            oc$ = ButtonScript("path=" & bi.path & "&cmd=expandpanel&key=" & ky)
            contentPanel = Nothing
        End If

        outerPanel.Controls.Add(NewLit("<span class=""" & css & """ onclick=""" & oc$ & """>&nbsp;</span>"))
        outerPanel.Controls.Add(NewLit("<span class='specHeader'>" & title & "</span>"))

        If contentPanel IsNot Nothing Then
            outerPanel.Controls.Add(contentPanel)
        End If

        Return contentPanel

    End Function

    Private Function AttributeTable(buyeraccount As clsAccount, bi As clsBranchInfo, ByRef errormessages As List(Of String)) As Table

        AttributeTable = New Table()
        AttributeTable.CssClass = "adminTable"

        Dim thr As TableHeaderRow = MakeTHR("Name,Value,Text,Unit,Delete", "", "")
        AttributeTable.Controls.Add(thr)

        If Me.Product IsNot Nothing Then
            Dim tr As TableRow
            Dim td As TableCell
            For Each pa In Me.Product.Attributes.Values
                tr = New TableRow
                AttributeTable.Controls.Add(tr)
                If pa.deleted Then tr.CssClass &= " deletedRow"

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = pa.Attribute.Translation.text(buyeraccount.Language)
                tr.Controls.Add(td)

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = pa.NumericValue.ToString
                tr.Controls.Add(td)

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = If(pa.Translation IsNot Nothing, pa.Translation.text(buyeraccount.Language), "")
                tr.Controls.Add(td)

                td = New TableCell
                tr.Controls.Add(td)
                td.Text = If(pa.Unit IsNot Nothing, pa.Unit.Symbol, "")
                tr.Controls.Add(td)

                td = New TableCell
                tr.Controls.Add(td)
                If Not pa.deleted Then
                    Dim lt As Literal = FunctionButton(bi.path, pa.Product.ID, "deleteProductAttribute&PAID=" & pa.ID, "DEL", "Delete this product attribute")
                    td.Controls.Add(lt)
                Else
                    Dim lt As Literal = FunctionButton(bi.path, pa.Product.ID, "unDeleteProductAttribute&PAID=" & pa.ID, "unDEL", "Undelete this product attribute")
                    td.Controls.Add(lt)
                End If
            Next

            tr = New TableRow
            AttributeTable.Controls.Add(tr)
            td = New TableCell
            tr.Controls.Add(td)
            td.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit these attributes", bi.agentAccount.Language), _
"window.open('edit.aspx?path=Products(" & Me.Product.ID & ").Attributes&TreePath=" & bi.path & "&lid=" & bi.lid.ToString & "');return(false);", _
"", "width:25px;height:25px;", bi.buyerAccount.Language))

        End If
    End Function


    Private Function countGrafts() As Integer

        'return the number of grafts of this branch

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        countGrafts = da.DBSelectFirst("SELECT COUNT(*) AS c FROM [graft] WHERE fk_branch_id_source=" & Me.ID)
        con.Close()

    End Function
    Public Function findSKUpaths(FindSku As String, path$, Paths As List(Of String), crossSkus As Boolean)

        'adds all the paths a sku appears at (below this branch) to the list

        If Me.Product IsNot Nothing Then
            If Product.SKU = FindSku Then Paths.Add(path$)
            If crossSkus = False And Me.Product.hasSKU Then Exit Function
        End If


        For Each b In Me.childBranches.Values
            b.findSKUpaths(FindSku, path$ & "." & b.ID, Paths, crossSkus)
        Next

    End Function


    Private Function BranchesTable(bi As clsBranchInfo) As Table

        Dim tbl As Table = New Table
        tbl.Attributes("class") = "adminTable"

        Dim thr As TableHeaderRow = MakeTHR("path", "", "")
        tbl.Controls.Add(thr)

        Dim tr As TableRow
        Dim td As TableCell

        'iq.RootBranch.SkuPaths(

        Dim paths As New List(Of String)
        iq.RootBranch.findSKUpaths(Me.Product.SKU, "tree." & iq.RootBranch.ID, paths, Not Me.Product.isSystem)

        For Each pth In paths

            tr = New TableRow
            tbl.Controls.Add(tr)

            td = New TableCell
            tr.Controls.Add(td)
            td.Controls.Add(KWbreadcrumbs(bi.lid, pth, English, True, False, "", True))

            'td = New TableCell
            'tr.Controls.Add(td)

        Next

        Return tbl

    End Function
    Friend Function ancestorMinorFamilies(all As List(Of String))

        '    Dim fams As List(Of String) = New List(Of String)
        If Me.Product IsNot Nothing AndAlso Me.Product.i_Attributes_Code.ContainsKey("FamMinor") Then
            Dim fm As String = Me.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English)
            If Not all.Contains(fm) Then
                all.Add(fm)
            End If

            Exit Function
            'Return Me.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English)
        End If

        'If Me.AllParents.Values.Count = 0 Then Return "Undetermined"
        ' all.AddRange(fams)
        For Each p In Me.AllParents.Values
            p.ancestorMinorFamilies(all)
        Next

    End Function

    Private Function SystemLocalisationTable()

    End Function

    Private Function QuantitiesTable(buyeraccount As clsAccount, bi As clsBranchInfo, ByRef errormessages As List(Of String)) As Panel

        Dim pnl As New Panel

        Dim region As clsRegion
        'Presintalled options
        If Not Me.Product Is Nothing Then


            'todo Title
            Dim lt

            'localisations
            '(heavily simplifived version of the branches quantities - which can be edited more fully in the editor)
            For Each q In Me.Quantities.Values
                If q.Path = bi.path Or q.Path = "" Then
                    pnl.Controls.Add(NewLit("<span style='background-color:#004040;color:white;'>" & q.Region.Code & If(q.NumPreInstalled, "(" & q.NumPreInstalled & ")", "") & "</span>&nbsp;"))
                End If
            Next

            'Edit localistaiotns button
            pnl.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit these quantity rows", bi.agentAccount.Language), _
    "window.open('edit.aspx?path=Branches(" & Me.ID & ").Quantities&TreePath=" & bi.path & "&lid=" & bi.lid.ToString & "');return(false);", _
    "", "width:25px;height:25px;", bi.buyerAccount.Language))


            region = buyeraccount.SellerChannel.Region

            'get the qtys that apply to my region (wider regions will tend to have LESS qtys) (think about it!) 
            Dim preinstalled As List(Of clsQuantity) = Nothing
            If Me.Product IsNot Nothing AndAlso Me.Product.isSystem Then
                preinstalled = Me.GetPreInstalledRecursive(region, bi.path, errormessages)
            End If

            If preinstalled IsNot Nothing Then

                Dim tbl As Table = New Table
                pnl.Controls.Add(tbl)
                tbl.CssClass = "adminTable"
                Dim tr As TableRow
                Dim td As TableCell


                Dim help$ = "Use the buttons to edit individual quantity rows|"
                help$ &= "Path at which the quantity specifically only works (or blank for everywhere)|"
                help$ &= "Number Preinstalled|"
                help$ &= "Minimum increment (users must add this many at a time to a system)|,"
                help$ &= "Preferred increment (users are *recommended* to add this many at a time (e.g. memory modules the perfom best in threes)|"
                help$ &= "What is the product type of the product, of the branch, that this quanity is attached to|"
                help$ &= "Is this (auto-added) quanitity FREE OF CHARGE (ie. 'preinstalled')|"
                help$ &= "In which region/county does this quantity specifically apply|"
                help$ &= "This quanity is attached to a branch which appears in many locations - is it pruned here|"
                help$ &= "You can 'soft delete' quantities (and undelete them if you change your mind)"

                Dim thr As TableHeaderRow = MakeTHR("Edit,Path,Num,MinIncr,PrefIncr,ProdType,FOC,Region,Pruned,Delete", help$, "")

                tbl.Controls.Add(thr)
                tr = New TableRow
                tbl.Controls.Add(tr)
                td = New TableCell : tr.Controls.Add(td) : td.Text = "Pre Installed (Quantities on descendant branches)"

                For Each i In preinstalled
                    'we don't render ALL the descendant quanitites (many are regionalisations for options under this system) -the hae a numinstalled of zero and 1,1 for min and pref increments .. but (importantly) a region.
                    'If i.Path.Contains(bi.path) Or i.Path = "" Then
                    tbl.Controls.Add(i.adminTableRow(bi))
                    'End If
                Next
            End If
        End If

        Return pnl

    End Function
    Private Function adminControls(bi As clsBranchInfo, PanelId As String, path$) As Panel

        Dim url$

        Dim outerpanel As New Panel 'holds the checkbox
        outerpanel.CssClass = "ib"
        Dim adminPanel As Panel = New Panel
        outerpanel.Controls.Add(adminPanel)

        adminPanel.ID = "admin_" & path$

        '  adminPanel.Attributes("class") &= "admin_collapsed"
        'adminPanel.Attributes("onclick") = "this.style.width='230px';this.style.height='auto';"

        'toggle between expanded and collapsed (ie.. if you are currently collapsed (when clicked) then switch to then admin_expanded class
        '    Dim collapserOnClickScript As String = _
        '    "burstBubble(event);var ex;ex=document.getElementById('admin_" & path$ & "');" & _
        '      "if (ex.className=='admin_collapsed'){ex.className='admin_expanded'} " & _
        '      "else {ex.className='admin_collapsed'};return(false)"

        'this DIV is just a 'spacer', the expand/collapse image is actually in the background of the admintools
        '(so we can change the appearance of it client side - with no server side knowledge of the exapnd/collapsed state))
        'adminPanel.Controls.Add(NewLit("<div style='width:30px;height:30px;display:inline-block;' onclick=" & Chr(34) & collapserOnClickScript & Chr(34) & "></div>"))



        adminPanel.Controls.Add(NewLit("&nbsp;"))

        'todo - implement commands - plus button graphics
        If bi.branch.DisplayName(English).ToLower = "all options" Then
            If bi.branch.locked Then
                adminPanel.Controls.Add(MakeRoundButton("lock.png", Xlt("click to unlock (allow imports to overwrite)", bi.agentAccount.Language), _
                ButtonScript("cmd=unlock&path=" & bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language))
            Else
                adminPanel.Controls.Add(MakeRoundButton("unlock.png", Xlt("click to lock (prevent imports overwriting)", bi.agentAccount.Language), _
                ButtonScript("cmd=lock&path=" & bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language))
            End If
            adminPanel.Controls.Add(NewLit("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp"))
        End If

        adminPanel.Controls.Add(MakeRoundButton("pencil.png", Xlt("Edit - Edits this branch (and the product, slots, quantities etc. attached to it))", bi.agentAccount.Language), "window.open('edit.aspx?path=Branches(" & Me.ID & ")&TreePath=" & path$ & "&lid=" & bi.lid.ToString & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))

        Dim tt As String
        If Me.deleted Then
            tt = Xlt("Undeletes (reinstates) this branch everywhere))", bi.agentAccount.Language)
            adminPanel.Controls.Add(MakeRoundButton("undelete.png", tt, ButtonScript("cmd=unDeleteBranch&path=" & bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language))
        Else
            'only put delete buttons on options for now .. (Systems and categories will need cascading deletes - which will need more thought)

            tt = Xlt("Marks the branch as deleted (everywhere it appears) " & bi.branch.AllPaths.Count & " Locations", bi.agentAccount.Language)
            adminPanel.Controls.Add(MakeRoundButton("delete.png", tt, ButtonScript("cmd=deleteBranch&path=" & bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language))
        End If

        'adminPanel.Controls.Add(MakeRoundButton("copy.png", Xlt("Copy - Copy - Mark this branch for a subsequent graft/adopt operation", bi.agentAccount.Language), "copyBranch(" & Me.ID.ToString.Trim & ");", "", "width:25px;height:25px;", bi.buyerAccount.Language))
        adminPanel.Controls.Add(MakeRoundButton("Graft.png", Xlt("Graft - attaches the branch you have previously copied or pruned, to this branch as a new child", bi.agentAccount.Language), "pasteBranch(" & Me.ID.ToString.Trim & ",'" & Trim$(PanelId) & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))
        adminPanel.Controls.Add(MakeRoundButton("Prune.png", Xlt("Prune - (deletes) this specific occurance of the branch (the same branch may still appear elsewhere)", bi.buyerAccount.Language), "pruneBranch('" & path$ & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))

        tt = Xlt("Shred preview (shows the impact of shredding (completely destroying this branch and all its descendants and dependecies would be)", bi.agentAccount.Language)
        adminPanel.Controls.Add(MakeRoundButton("ShredPreview.png", tt, ButtonScript("cmd=previewShredBranch&path=" & bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language))

        Dim ttt = Xlt("Validate - Runs a test quote/Validation on every system under this branch.", bi.agentAccount.Language)
        adminPanel.Controls.Add(MakeRoundButton("tick.png", ttt, ButtonScript("cmd=quoteAll&path=" & bi.path), "", "width:25px;height:25px;", bi.buyerAccount.Language))


        If Not Me.Parent Is Nothing Then
            adminPanel.Controls.Add(MakeRoundButton("Retract.png", Xlt("Retract - removes this branch and promotes all of its children to its level (useful for collapsing redundant categories)", bi.agentAccount.Language), "retractBranch('" & path$ & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))
        End If
        adminPanel.Controls.Add(MakeRoundButton("genericFilter.png", Xlt("Clone, makes an independent 'deep' copy of this branch (as a sibling) - use to add a model to a family", bi.agentAccount.Language), "clone('" & path$ & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))
        adminPanel.Controls.Add(MakeRoundButton("adopt.png", Xlt("Adopt - Makes this branch the new parent of the Selected branches ", bi.agentAccount.Language), "adopt('" & path$ & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))

        Dim bs As String = ButtonScript("cmd=snap&=path=" & bi.path)
        adminPanel.Controls.Add(MakeRoundButton("hierarchy.png", Xlt("XML Snapshot from this point", bi.agentAccount.Language), bs, "", "width:25px;height:25px;", bi.buyerAccount.Language))


        'different approach (ML) - haven't attempted topring ito the fold - i think he hads an event handelr with jquery
        If Me.Matrix IsNot Nothing Then
            adminPanel.Controls.Add(NewLit("<div title='" + Me.Matrix.ID.ToString() + "' class=""hasScreen""></div>"))
        End If


        ' adminPanel.Controls.Add(MakeRoundButton("cross.png", Xlt("Having - Marks this branch (and all its descendants) as being Incompatible with ...", bi.agentAccount.Language), "setHaving('" & path & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))
        ' adminPanel.Controls.Add(MakeRoundButton("excl.png", Xlt("Excludes - Excludes all items under this branch (in combintaion with the having button)", bi.agentAccount.Language), "makeExclude('" & path & "');", "", "width:25px;height:25px;", bi.buyerAccount.Language))

        ' Dim excludedBy As List(Of clsExclude) = Me.isExcludedBy
        For Each eb In Me.ExcludedBy ' I' can  be excluded by more than one branch

            Dim tooltip As String = "EDIT - This branch is excluded by Having " & eb.havingAnyOf.First.EnglishName & "....(" & eb.Reason & ")"
            url$ = "edit.aspx?path=Excludes(" & eb.ID & ")&TreePath=" & path$
            adminPanel.Controls.Add(MakeLinkButton("isexcl.png", tooltip, url, bi.buyerAccount.Language))
        Next

        For Each e In Me.iExclude

            Dim btn As New HyperLink
            Dim ToolTip As String = "EDIT - Having anything under this branch excludes excludes " & e.excludesAllOf.First.EnglishName & "....(" & e.Reason & ")"
            url$ = "edit.aspx?path=Excludes(" & e.ID & ")&TreePath=" & path$

            adminPanel.Controls.Add(MakeLinkButton("excl.png", ToolTip, url, bi.buyerAccount.Language))
        Next


        'the ecb  (editor check box) class has no styling effect but is used in the JS to GetElementsByClassName
        adminPanel.Controls.Add(NewLit("&nbsp;<Input title='used to select multiple branches for Adopt operation' type='checkbox' style='vertical-align:top' class='ecb ib' id='cb" & bi.path & "'></input>&nbsp;"))


        Return outerpanel

    End Function

    Private Function iExclude() As List(Of clsExclude)

        iExclude = New List(Of clsExclude)

        For Each exclude In iq.Excludes.Values
            If exclude.havingAnyOf.Contains(Me) Then iExclude.Add(exclude)
        Next

    End Function

    Private Function ExcludedBy() As List(Of clsExclude)

        ExcludedBy = New List(Of clsExclude)
        For Each exclude In iq.Excludes.Values
            If exclude.excludesAllOf.Contains(Me) Then ExcludedBy.Add(exclude)
        Next

    End Function

    'Private Function CountLabel(language As clsLanguage, buyerAccount As clsAccount, Path$) As Literal
    '    'add the count of sub products (or families - or options or whatever - with the appropriate wording - including singulars)
    '    Return lit
    'End Function

    Private Function Title(bi As clsBranchInfo, ShowCount As Boolean, Previewchildren As Boolean, offsetCount As Boolean, _
                           numSKUs As Integer, numCats As Integer, ByRef hideReasons As List(Of String), ByRef errorMessages As List(Of String), Optional pbs As clsBranchState = Nothing, Optional bs As clsBranchState = Nothing)

        'Clickable section title (will jump the TreeCursor)

        Title = New Panel



        'hyperlinks (replace some of the options tabs) 
        If pbs IsNot Nothing AndAlso (pbs.rca = enumBt.hYperlink Or pbs.rca = enumBt.bighyperlinK) Then
            'If pbs.rca = enumBt.bighyperlinK Then
            '    Title.cssclass = "bigLink "
            'Else
            '    Title.cssclass = "link optionsLink"
            'End If

            If bs IsNot Nothing Then Title.CssClass &= " visited"
            If Me.deleted Then Title.cssclass &= " strikethru"
        Else
            Title.cssclass = "branchTitle"
            If Me.deleted Then Title.cssclass &= " strikethru"

        End If

        If hideReasons.Count > 0 Then
            Title.CssClass &= " HiddenProduct"
            For Each h In hideReasons
                Title.tooltip &= h
            Next
        End If


        Dim tl As New Label
        Title.Controls.Add(tl)

        ' Dim lbl As Label = New Label
        ' lbl.Text = " " & bi.path
        ' Title.controls.add(lbl)

        'If iq.sesh(bi.lid, "configuring") IsNot Nothing AndAlso Me.Product IsNot Nothing AndAlso Me.Parent.Translation.text(English) IsNot Nothing Then
        '    tl.Text = Me.Parent.Translation.text(English) + " - "
        '    If Me.Product IsNot Nothing AndAlso Me.Product.i_Attributes_Code.ContainsKey("SC") Then
        '        tl.Text &= Me.Product.i_Attributes_Code("SC")(0).displayName(English) + " - "
        '    End If
        'Else
        '    tl.Text = String.Empty
        'End If

        tl.Text &= FormatName(bi.agentAccount, Me.Translation.text(bi.buyerAccount.Language))

        'If Not Me.Translation Is Nothing Then
        '    tl.Text &= "-" & Me.Translation.text(bi.BuyerAccount.Language)
        '    'For the System Title in 'Configuring' mode - prepend the Family name and Supply chain
        '    If iq.sesh(bi.lid, "configuring") IsNot Nothing AndAlso Me.Product IsNot Nothing Then

        '        'the family name is the systems parent branches name (this is the correctly 'unabreviated' one)
        '        tl.Text = Me.Parent.Translation.text(English)

        '        If Me.Product IsNot Nothing Then
        '            Dim supplychain As String = ""
        '            If Me.Product.i_Attributes_Code.ContainsKey("SC") Then
        '                supplychain = Product.i_Attributes_Code("SC")(0).Translation.text(bi.BuyerAccount.Language)
        '            End If

        '            If supplychain <> "" Then
        '                tl.Text &= " - " & supplychain
        '            End If
        '            tl.Text &= "-" & Me.Translation.text(bi.BuyerAccount.Language) ' My branch name (the system)
        '        Else
        '            tl.Text = "HP" 'not sure what this is for - I may have stuffed it up when altering martrins code
        '        End If
        '    End If
        'End If



        'Active filters
        Dim mh As Dictionary(Of String, clsScreenHeader) = iq.sesh(bi.lid, "matrixHeaders")

        'Removed for Greg, now at filterui level
        'If mh IsNot Nothing Then
        '    Dim pth As String = ""
        '    For Each seg In Split(bi.path, ".")
        '        pth &= seg

        '        If mh.ContainsKey(pth) Then
        '            If mh(pth).Vw IsNot Nothing Then
        '                If mh(pth).Vw.RowFilter <> "" Then
        '                    'find the first thing whos rca is NOT opensquares .. render from and into there
        '                    Dim fe As String = "<span title='Filters are in effect (click to remove) " & mh(pth).Vw.RowFilter & _
        '                        "' onclick=|" & ButtonScript("cmd=removeFilters&path=" + bi.path & "&into=" + pth + "&filterPath=" & pth) & "|>*</span>"
        '                    fe = Replace(fe, "|", Chr(34))
        '                    Title.Controls.Add(NewLit(fe))
        '                End If
        '            End If
        '        End If
        '        pth &= "."
        '    Next
        'End If

        If Previewchildren Then  'this Is to see what will be on tabs (as tooltips) - there's no 'easy' was - as we have open 
            Dim bn As List(Of String) = New List(Of String)
            For Each c In (From cb In childBranches.Values Order By cb.order) 'Me.childBranches.Values
                'add it to the preview if it is a (or has a descendant) visible,SKUd product
                '         If c.Descendants(True, bi.BuyerAccount, bi.Foci, bi.path$ & "." & Trim$(CStr(c.ID)), False, False, 1, True, errorMessages, False).Count > 0 Then
                ' bn.Add(c.DisplayName( bi.BuyerAccount.Language))
                ' End If
            Next
            If bn.Count > 0 Then
                tl.ToolTip = Join(bn.ToArray, ",") & "."
            End If
        End If

        '  tl.ToolTip &= "branchID:" & Me.ID & " " & Me.childBranches.Count & " children"

        Dim occ$
        'moveTreeCursor - jumps the tree cursor to this section without altering the open/closed state of the branch - calls maniupulation.aspx?cursor=..
        occ$ = "moveTreeCursor('" & bi.path & "');return false;"

        Title.Attributes("onclick") = occ$

        If ShowCount Then
            Dim lit As Literal = New Literal

            'Child branch count (number in parenthesis at the end of each line)
            If Me.childBranches.Count > 0 Then

                'This is the count of skud products 
                'Which at various points we refer to as different things (systems, option, drives, modules etc.)

                'Dim childProducts As Integer = Me.ChildProductCount(bi.BuyerAccount, bi.Foci, bi.path, False, bi.ShowAll)

                Dim ProdWord$

                If plen(bi.path) < 3 Then

                    Dim cnoun As String
                    'If numCats = 1 Then
                    '    cnoun = Me.collectiveNounSingular.text(s_lang)
                    'Else
                    If Me.CollectiveNoun Is Nothing Then
                        cnoun = "products"
                    Else
                        cnoun = Me.CollectiveNoun.text(s_lang)
                    End If
                    'End If

                    'Dim skus As String = ""
                    'If numSKUs >= 100 Then
                    '    skus = "100+"
                    'Else
                    '    skus = numSKUs.ToString
                    'End If

                    'in the 'Squares' mode, we break the count label out of the flow and put it under the title (becuase horizontal space is tight)
                    If numSKUs = 0 And numCats > 1 Then
                        lit.Text = "<div class='childCount" & CStr(IIf(offsetCount, " squareCount", "")) & "'>(" & numCats & " " & cnoun
                        lit.Text &= ")</div>"
                    Else

                        If numSKUs > 2 Then
                            If Split(bi.path, ".").Count > 5 Then ProdWord = "options" Else ProdWord = "systems"

                            lit.Text = "<div class='childCount" & CStr(IIf(offsetCount, " squareCount", "")) & "'>(" & numSKUs & " " & ProdWord

                            'If numCats = 1 Then
                            '    lit.Text &= ")</div>"
                            'Else
                            '    If False Then 'bi.branchState.United Or Split(bi.path, ".").Count < 5 Then   -Nobbled at greg/dans request
                            '        lit.Text &= " in " & numCats & "&nbsp;" & cnoun & ")</div>"
                            '    Else
                            lit.Text &= ")</div>"
                            '  End If
                        End If
                    End If
                End If
                Title.Controls.Add(lit)
            End If
        End If


    End Function

    Private Function FormatName(agentAccount As clsAccount, title As String) As String

        If title.IndexOf("[") < 0 Then Return title ' Bale out if there are no substitutions

        FormatName = title.Replace("[mfr]", agentAccount.mfrCode)

    End Function

    Private Function ExpandCollapseButton(pbs As clsBranchState, bi As clsBranchInfo, bs As clsBranchState, ByRef errorMessages As List(Of String)) As PlaceHolder

        ExpandCollapseButton = New PlaceHolder

        Dim visclass As String = String.Empty

        Dim diagnostic As Boolean = AccountHasRight(bi.lid, "DIAGVIEW")
        If Not diagnostic Then
            'reasons NOT to display an open/close button
            'If Me.isOptionOrOptionHolder Then Exit Function    ' SK - OptionHolders allowed through to show the expander
            If Me.isOption Then Exit Function
            If Me.PruneInForce(bi.path$, bi.buyerAccount.SellerChannel) <> 0 Then Exit Function
        End If

        Dim lit As Literal = New Literal
        If bs Is Nothing Then
            'No Branch State - We are closed - Add an expand (+) button
            lit.Text = "<div class='expandContract collapsed' title='" & Xlt("Click to expand", bi.buyerAccount.Language) & "' onclick=|" & ButtonScript("cmd=open&path=" & bi.path) & "|>&nbsp;</div>"
            lit.Text = Replace(lit.Text, "|", Chr(34))
            ExpandCollapseButton.Controls.Add(lit)
        Else
            'there is branchstate (we are open) - Add a collapse (-) button
            lit.Text = "<div class='expandContract" & visclass & "'  onclick=|" & ButtonScript("cmd=close&path=" & bi.path) & "|>&nbsp;</div>"
            lit.Text = Replace(lit.Text, "|", Chr(34))
            ExpandCollapseButton.Controls.Add(lit)
        End If

    End Function

    Private Function oldExpandCollapseButton(pbs As clsBranchState, bi As clsBranchInfo, bs As clsBranchState, ByRef errorMessages As List(Of String)) As PlaceHolder

        oldExpandCollapseButton = New PlaceHolder

        'Dim visible As Boolean = True
        'If Not bi.ShowAll Then  'Show all is 'admin mode' - where we wall draw all branches (regardless of Geographic restrictions and prunes)
        '    If Not Me.Product Is Nothing Then
        '        Dim rh As List(Of String) = Me.ReasonsForHide(bi.BuyerAccount, bi.Foci, bi.path$, bi.BuyerAccount.SellerChannel.priceConfig, True, errorMessages)
        '        If rh.Count <> 0 Or bi.ShowAll Then
        '            visible = False
        '        End If
        '    End If
        'End If

        Dim visclass As String = String.Empty
        'If visible Then visclass = " greyed" 'this product Isnt visibisible (we must be in 'show all' mode)

        If Me.PruneInForce(bi.path$, bi.buyerAccount.SellerChannel) <> 0 Then
            'this branch is pruned here
            Exit Function

        Else
            'We make a close button for all open branches, ... individual branches we opened and then closed (at any given level) have their RCA set to hidden
            If Me.Product IsNot Nothing Then
                'where we have used 'openttreeto' (primarily show in tree buttons in the basket) - we render the branches with + signs (as expandable)
                If bs IsNot Nothing AndAlso Me.Product.isSystem(bi.path) AndAlso (Not Me.isOptionOrOptionHolder(bi.path) Or pbs.rca = enumBt.Branch Or AccountHasRight(bi.lid, "DIAGVIEW")) Then
                    'add the close (-) button . .
                    Dim lit As Literal = New Literal
                    lit.Text = "<div class='expandContract" & visclass & "'  onclick=|" & ButtonScript("cmd=close&path=" & bi.path) & "|>&nbsp;</div>" '-
                    lit.Text = Replace(lit.Text, "|", Chr(34))
                    oldExpandCollapseButton.Controls.Add(lit)
                End If
            End If
        End If

        If bs Is Nothing AndAlso (Not Me.isOptionOrOptionHolder(bi.path) Or pbs.rca = enumBt.Branch Or AccountHasRight(bi.lid, "DIAGVIEW")) Then

            ' Don't display an expand/collapse box on systems if they are being displayed as components of another system
            If Me.Product Is Nothing OrElse (String.IsNullOrEmpty(Me.Product.SKU) OrElse Me.Product.isSystem(bi.path) OrElse AccountHasRight(bi.lid, "DIAGVIEW")) Then
                If bs Is Nothing Then
                    'No Branch State - We are closed - Add an expand button
                    Dim lit As Literal = New Literal : lit.Text = "<div class='expandContract collapsed' title='" & Xlt("Click to expand", bi.buyerAccount.Language) & "' onclick=|" & ButtonScript("cmd=open&path=" & bi.path) & "|>&nbsp;</div>"
                    lit.Text = Replace(lit.Text, "|", Chr(34))
                    oldExpandCollapseButton.Controls.Add(lit)
                Else
                    'there is branchstate (we are open) - Add a collapse button
                    Dim lit As Literal = New Literal : lit.Text = "<div class='expandContract" & visclass & "'  onclick=|" & ButtonScript("cmd=close&path=" & bi.path) & "|>&nbsp;</div>" '-
                    lit.Text = Replace(lit.Text, "|", Chr(34))
                    oldExpandCollapseButton.Controls.Add(lit)
                End If
            End If
        End If
    End Function

    Private Function Switcher(bi As clsBranchInfo, bs As clsBranchState, atSkus As Boolean, RCAs As String) As Panel  'View switcher

        Dim allButtons As Boolean = False
        Switcher = New Panel
        Switcher.CssClass = "switcher"

        'Dim q$ = Chr(34)

        'Dim css$ = ""
        'If current = bt.Branch Then css$ = "selected" Else css$ = ""
        'SwitchViewButtons.Controls.Add(MakeRoundButton("tree.png", "View in a tree", ButtonScript(bi, "branches"), css, ""))

        'If current = bt.gridrow Then css$ = "selected" Else css$ = ""
        'SwitchViewButtons.Controls.Add(MakeRoundButton("matrix.png", "View/compare in a grid", ButtonScript(bi, "grid"), css, ""))

        ''Squares button
        'If current = bt.Square Then css$ = "selected" Else css$ = ""
        'SwitchViewButtons.Controls.Add(MakeRoundButton("squares.png", "View as tiles", ButtonScript(bi, "squares"), css, ""))

        ''Tabs button
        'If current = bt.Tab Then css$ = "selected" Else css$ = ""
        'SwitchViewButtons.Controls.Add(MakeRoundButton("tabs.png", "View on tabs", ButtonScript(bi, "tabs"), css, ""))


        If Len(rca.Trim) <= 1 Then Exit Function 'Only one choice - so we dont render a switcher

        Dim vt As Dictionary(Of enumBt, String) = New Dictionary(Of enumBt, String)
        'these will need translating
        vt.Add(enumBt.Branch, Xlt("Branches", bi.agentAccount.Language))
        vt.Add(enumBt.gridrow, Xlt("Grid", bi.agentAccount.Language))
        vt.Add(enumBt.Square, Xlt("Squares", bi.agentAccount.Language))
        vt.Add(enumBt.Tab, Xlt("Tabs", bi.agentAccount.Language))
        vt.Add(enumBt.OpenBranch, Xlt("Branches (open)", bi.agentAccount.Language))
        vt.Add(enumBt.DetailSquare, Xlt("Squares", bi.agentAccount.Language))
        vt.Add(enumBt.helpMechoose, Xlt("Help Me Choose", bi.agentAccount.Language))


        For Each t In vt.Keys.ToArray
            If Not RCAs.Contains(BTchar(t)) Then vt.Remove(t)
        Next

        Dim dicCss As Dictionary(Of enumBt, String) = New Dictionary(Of enumBt, String)
        dicCss.Add(enumBt.Branch, "v_Branch")
        dicCss.Add(enumBt.gridrow, "v_Grid")
        dicCss.Add(enumBt.Square, "v_Square")
        dicCss.Add(enumBt.Tab, "v_Tab")
        ' dicCss.Add(enumBt.BreadCrumb, "v_Breadcrumb")
        dicCss.Add(enumBt.OpenBranch, "v_Branch")
        dicCss.Add(enumBt.DetailSquare, "v_CompSquare")
        dicCss.Add(enumBt.helpMechoose, "v_HelpMeChoose")

        Dim lit As Literal
        If dicCss.ContainsKey(bs.rca) And vt.ContainsKey(bs.rca) Then '(branches may be hidden))

            lit = New Literal

            lit.Text = vbCrLf & "<!--View Switch DropDown-->" & vbCrLf & "<div id='outer." & bi.path & "' class='dd_form dd_closed'>"
            ' lit.Text &= "<div class='dd_wrap'>"
            lit.Text &= "<div id='ddh." & bi.path & "' class='dd_head " & dicCss(bs.rca) & "'"
            lit.Text &= " onmousedown=|burstBubble(event);"
            lit.Text &= "displayDropDown('" & bi.path & "') |"
            lit.Text &= " style='z-index: 3;'>" '/*downside is the 'thin' bottom border and should be added when it's expanded
            lit.Text = lit.Text.Replace("|", Chr(34))
            'lit.Text &= "<span class='dd_label_text'><span class='js_dd_input_value dd_input_value dd_selectedOption'>" & vt(current) & "</span><span class='dd_icn_container'><span class='dd_icn'>&nbsp;</span></span></span></a>"
            lit.Text &= "<span>&nbsp;</span><span class='dd_icn_container'></span><span id='txt." & bi.path & "' style='display:none'>" & vt(bs.rca) & "</span>"
            lit.Text &= "</div>"

            lit.Text &= vbCrLf & "<!--DropDownBody-->" & vbCrLf
            lit.Text &= "<div id='ddb." & bi.path$ & "' class='dd_thinBottom dd_form' style='visibility:visible;display: none;'>" 'block
            '        lit.Text &= "<div class='js_dd_list_items dd_list_items h150'>"


            For Each nonselected In From j In vt 'Where j.Key <> current
                'NB the buttonscript needs an ENGLISH version *tabs,squares,branches or grid*
                lit.Text &= "<div class='dd_item " & dicCss(nonselected.Key) & "' onmousedown=|" & ButtonScript("cmd=switchTo&bt=" & BTchar(nonselected.Key) & "&path=" & bi.path) & ";|>"
                lit.Text &= "<span>" & nonselected.Value & "</span>"
                lit.Text &= "</div>" & vbCrLf
                lit.Text = lit.Text.Replace("|", Chr(34))
            Next

            '        lit.Text &= "</div>"
            ' lit.Text &= "</div>"
            lit.Text &= "</div> <!--/DropDrownBody-->" & vbCrLf
            lit.Text &= "</div><!--/Outer-->"

            Switcher.Controls.Add(lit)

        End If

        Dim css$

        Dim vegas As Boolean = True

        If Not vegas Then
            If Not atSkus Then

                'Unite
                If bs.United Then css$ = "selected" Else css$ = ""
                Switcher.Controls.Add(MakeRoundButton("unite.png", "Unite (View all products)", ButtonScript("cmd=unite&path=" & bi.path), css, "", bi.buyerAccount.Language))

                If Not bs.United Then css$ = "selected" Else css$ = "" 'divided
                Switcher.Controls.Add(MakeRoundButton("divide.png", "Divide (View categories)", ButtonScript("cmd=divide&path=" & bi.path), css, "", bi.buyerAccount.Language))

            End If
        End If

    End Function


    Public Shared Function ButtonScript(cmd As String) As String

        'Builds the JS for a getBranches 

        'burstbubble stops even propagation (the event firing on ancestor element)
        'return false - stops the 'default' (submit/postback) action on button elements

        If InStr(cmd, "path=") = 0 Then Stop
        ButtonScript = "burstBubble(event);$(this).attr('onclick','');getBranches('" & cmd & "');return false;"

    End Function


    Public Function SystemsThatTake(ByVal system As clsProduct, ByRef mustHost As List(Of clsProduct), ByVal bundle As clsBundle) As List(Of clsProduct)

        'recurses, typically from ther root branch to return a list of all the systems for which all the items in the bundle are an option.
        'used only during import to implement Chris's bright idea of allowing NULLs on the bundleIndex_ISSRebates.systems (to mean appy to all systems)

        'This is not an easy function to understand.. mustHost is a set of options - copied from the bundle every time we encounter a system
        'options (non-system products) are removed from this 'musthost' checklist as they are encountered - once we have them all - we add the system to the list 
        ' all the lists are concatenated by the addrange... the whole thing is recursive

        Dim retval As List(Of clsProduct) = New List(Of clsProduct)

        If Not Me.Product Is Nothing Then
            If Me.Product.isSystem Then
                system = Me.Product
                If mustHost Is Nothing Then mustHost = New List(Of clsProduct)
                mustHost.Clear()
                For Each i In bundle.Items.Values
                    mustHost.Add(i.Product)
                Next
            Else
                If Not system Is Nothing Then 'have we traveresed a system yet
                    If mustHost.Contains(Me.Product) Then mustHost.Remove(Me.Product)
                    If mustHost.Count = 0 Then
                        retval.Add(system) : Return retval : Exit Function
                    End If
                End If
            End If
        End If

        Dim sys As List(Of clsProduct)
        For Each b In Me.childBranches.Values
            sys = b.SystemsThatTake(system, mustHost, bundle)
            If sys.Count > 0 Then
                retval.AddRange(sys)
                Return retval
            End If
        Next

        Return retval

    End Function

    <Obsolete("Based on flawed logic - ml")>
    Private Function OptLevel() As Integer

        If Me.Translation IsNot Nothing AndAlso Me.Translation.Group.StartsWith("OL") Then
            Return Integer.Parse(Me.Translation.Group.Substring(Me.Translation.Group.Length - 1, 1))
        End If
        If Me.Parent IsNot Nothing AndAlso Me.Parent.Translation IsNot Nothing AndAlso Me.Parent.Translation.Group.StartsWith("OL") Then
            Return Integer.Parse(Me.Parent.Translation.Group.Substring(Me.Parent.Translation.Group.Length - 1, 1)) + 1
        End If
        If Me.Parent IsNot Nothing AndAlso Me.Parent.Parent IsNot Nothing AndAlso Me.Parent.Parent.Translation IsNot Nothing AndAlso Me.Parent.Parent.Translation.Group.StartsWith("OL") Then
            Return Integer.Parse(Me.Parent.Parent.Translation.Group.Substring(Me.Parent.Parent.Translation.Group.Length - 1, 1)) + 2
        End If


    End Function

    ''' <summary>Returns True If the Branch is an option, or hold options</summary>
    ''' <param name="lid"></param>
    ''' <returns></returns>
    ''' <remarks>Userd to supress switchers</remarks>
    Private Function isOptionOrOptionHolder(Optional path As String = "") As Boolean

        '        If UserIsAdmin(lid) Then Return True 'FOR ADMINS - all branches should  be expandable

        Return (
            Me.Product IsNot Nothing _
            AndAlso Me.Product.hasSKU() _
            AndAlso Not Me.Product.isSystem(path)) _
            Or _
            (Me.childBranches.Count > 0 AndAlso _
             Me.childBranches.First.Value.Product IsNot Nothing _
             AndAlso Me.childBranches.First.Value.Product.hasSKU() _
             AndAlso Not Me.childBranches.First.Value.Product.isSystem(path) _
             )

    End Function

    Private Function isOption(Optional path As String = "") As Boolean
        Return (Me.Product IsNot Nothing AndAlso Me.Product.hasSKU() AndAlso Not Me.Product.isSystem(path))
    End Function

    Public Sub BuildPathDic(ck As String, ByRef lDic As Dictionary(Of String, clsBranch), useMe As Boolean)
        If Me.HasSKU Then Exit Sub
        If useMe AndAlso Me.Translation IsNot Nothing AndAlso IsNumeric(Right(Me.Translation.Group, 1)) AndAlso Right(Me.Translation.Group, 1) > ck.Split("^").Length + 1 Then ck = ck & "^"
        If useMe Then
            ck = ck & If(String.IsNullOrEmpty(ck), "", "^") & Translation.text(English)
            If Not lDic.ContainsKey(ck) Then lDic.Add(ck, Me)
        End If
        For Each child In childBranches
            child.Value.BuildPathDic(ck, lDic, True)
        Next
    End Sub

    Public Function AllPaths() As List(Of String)
        If Me.ID = 1 Then Return New List(Of String) From {"tree.1"}
        AllPaths = New List(Of String)()
        For Each p In AllParents.Values
            AllPaths.AddRange(p.AllPaths().Select(Function(f) f & "." & Me.ID))
        Next
    End Function

    'Private Sub LogMessage(message As String)

    '    If (Not log4net.LogManager.GetRepository().Configured) Then
    '        Config.XmlConfigurator.Configure()
    '    End If
    '    log.Info(message)

    'End Sub
End Class 'clsbranch


