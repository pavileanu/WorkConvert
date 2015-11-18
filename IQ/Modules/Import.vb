Imports System.Linq
Imports System
Imports System.Globalization
Imports System.Security.Cryptography
Imports System.Data.SqlClient
Imports dataAccess
Imports System.IO

Module Import
    Dim restrictImportToFamily As String = "" ' change this to '' for all
    Public ActionListLid As Dictionary(Of UInt64, clsActionList) = New Dictionary(Of ULong, clsActionList)()

    Dim dicAbbreviations As Dictionary(Of String, String)
    'we don't immediately turn all of these into translations (chances are many of them are un-needed)
    Public Const server As String = "h3." '"h3." '"[www3.channelcentral.net,8484]."
    Public Const DSserver As String = "h3." '"[www.channelcentral.net,8484]."  'datastore

    Public i_Quantities As New List(Of String) 'Early enforcement of the unique contrain on clsQuantitys (by branch,region, path)
    Public ImportLog As clsImportLog = New clsImportLog()
    Public Function Incremental(lid As UInt64, submitlist As List(Of System.Collections.Generic.KeyValuePair(Of Integer, Boolean))) As clsActionList  'Pass in a list of SKU's to add or update...
        For Each ac In ActionListLid(lid).ToList()
            If submitlist(ac.ID).Value Then ac.Authorized = True
        Next
        Return Incremental(lid, ActionListLid(lid).ToList().Where(Function(al) al.Authorized).Select(Function(al) al.SKU).Distinct().ToList())

    End Function


    Public Function checkfamilies()

        Dim haves, havenots As Integer
        For Each b In iq.Branches.Values
            If b.Product IsNot Nothing Then
                If b.Product.isSystem And Not b.Product.isOption Then
                    If b.Parent.Product IsNot Nothing Then
                        If b.Parent.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
                            haves += 1
                        Else
                            havenots += 1
                        End If
                    End If
                End If
            End If
        Next
        'If Me.Product IsNot Nothing AndAlso Me.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
        ' If Me.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English).ToLower = famname.ToLower Then

        Beep()

    End Function



    Public Sub sweepFios()

        'options that are FIOs in the (sub) family should appear on the FIOs tab
        'thier altSKU's should appear under Options
        'The FIO should have the autoadd
        'check for missing options (some L21 parts have never been imported)


        Dim sql$
        Dim sw As New StreamWriter("c:\temp\fioinfo.txt")

        Dim made As Integer = 0
        Dim con As SqlClient.SqlConnection
        Dim rdr As SqlClient.SqlDataReader

        con = da.OpenDatabase

        Dim dicCPUqmax As New Dictionary(Of String, Integer)
        sql$ = "select sysfamily,qtymax from h3.iq.products.optionlimits where opttype='cpu'"
        rdr = da.DBExecuteReader(con, sql)
        While rdr.Read
            dicCPUqmax.Add(rdr("sysfamily"), rdr("qtymax"))
        End While
        rdr.Close()

        sql$ = "select optsku,altsku,opttype,DescriptionGen,fio from h3.iq.products.options where altsku is not null and opttype='cpu'"

        rdr = da.DBExecuteReader(con, sql)

        While rdr.Read

            'the optskus are the 'normal' B21 parts and the altskus are the FIO versions

            Dim optsku As String = rdr.Item("optsku")
            Dim altsku As String = rdr.Item("altsku")

            Dim optProduct As clsProduct
            If iq.i_SKU.ContainsKey(optsku) Then
                optProduct = iq.i_SKU(optsku)
                If iq.i_SKU.ContainsKey(altsku) Then
                    Dim altproduct As clsProduct = iq.i_SKU(altsku)
                    If altproduct.FirstAttributeEnglishText("altsku") = "" Then

                        sw.WriteLine("The alternate for " & optsku & "(" & altsku & ") does not have a reciprocal altsku")
                        '         Dim raltsku As New clsProductAttribute(altproduct, iq.i_attribute_code("altSKU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(optsku, English, "alstskus", 0, Nothing, 0, False))
                        ' Beep()
                        'this product B21 doesnt have its (valid) altsku populated

                    ElseIf altproduct.FirstAttributeEnglishText("altsku") <> optsku Then
                        If optProduct.ProductType.Code = "cpu" Then
                            sw.WriteLine("The alternate for " & optsku & "(" & altsku & ") has the WRONG reciprocal altsku " & altproduct.FirstAttributeEnglishText("altsku"))
                        End If

                    Else
                        'ok This L21 already has the B21 as an alt

                    End If
                Else
                    'the altsku (L21) was never imported (does not exist in iquote2)
                    sw.WriteLine("The alternate for " & optsku & "(" & altsku & ") is not in iQuote2 - this is normal ")

                    'Dim altproduct As clsProduct = optProduct.clone(altsku)

                    'If rdr.Item("fio") = 1 Then
                    '    Dim isfio = New clsProductAttribute(altproduct, iq.i_attribute_code("focus"), 1, iq.i_unit_code("txt"), iq.AddTranslation("FIO", English, "Foci", 0, Nothing, 0, False))

                    '    Dim raltsku As New clsProductAttribute(altproduct, iq.i_attribute_code("altSKU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(optsku, English, "alstskus", 0, Nothing, 0, False))
                    '    Dim raltDesc As New clsProductAttribute(altproduct, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), optProduct.i_Attributes_Code("desc")(0).Translation)
                    '    made += 1

                    'End If
                End If
            Else
                sw.WriteLine("option " & optsku & " is NOT IN IQUOTE2 (ancient history ?)")
            End If

        End While
        rdr.Close()
        con.Close()
        sw.Close()

        Dim errormessages As New List(Of String)

        Dim systems As New Dictionary(Of String, clsBranch)
        systems = iq.RootBranch.findSystemBranches("tree.1")

        Dim currentOptions As New Dictionary(Of clsProduct, String)
        Dim currentalloptionsbranch As clsBranch = Nothing

        Dim qm, qd, sm, sd As Integer

        Dim genericCPU As clsSlotType = iq.i_slotType_Code("cpu")("gen_cpu")

        Dim slotstodel As New HashSet(Of clsSlot)

        Dim done As Integer = 0
        For Each systemPath As String In systems.Keys

            done += 1

            Dim systemBranch As clsBranch = systems(systemPath)
            Dim system As clsProduct = systems(systemPath).Product



            Dim sysfam As String = system.FirstAttributeEnglishText("famminor")

            For Each cb In systemBranch.childBranches.Values

                If sysfam <> "" Then
                    If cb.Translation.text(English).ToLower.EndsWith("chassis") Then
                        For Each s In cb.slots.Values.ToList
                            If s.Type.MajorCode.ToLower = "cpu" Then  'make sure we have 1 cpu slot oin
                                If s.Type.MinorCode.ToLower <> "gen_cpu" Then
                                    If cb.hasSlot(genericCPU) Then
                                        s.deleted = True
                                        s.update(errormessages)
                                    Else
                                        cb.i_Slots.Remove(s.compoundKey)
                                        s.Type = genericCPU 'some non generic cpu
                                        s.CurrentCompoundKey = s.compoundKey 'remake the compound key (so 'update' can remove the 'right old' one)
                                        cb.i_Slots.Add(s.compoundKey, s)
                                        s.update(errormessages)
                                    End If
                                Else
                                    If s.numSlots > 1 Then 'More than one CPU slot
                                        For Each cs In cb.slots.Values.ToList 'look  over the slots on the chassis branch
                                            If cs.Type.MajorCode.ToLower = "mem" Then
                                                'Memory slot on the chassis (shouldn't be here!)

                                                Dim cputl As clsTranslation = systemBranch.Product.i_Attributes_Code("cpuSKU")(0).Translation
                                                Dim cpusku = cputl.text(English)
                                                Dim CPUpth$ = ""
                                                Dim cpubranch As clsBranch = systemBranch.findChildBySKU2(systemPath, cpusku, CPUpth$)
                                                If cpubranch Is Nothing Then
                                                    Stop
                                                Else
                                                    Dim cpuHasMemory As Boolean = False
                                                    For Each cms In cpubranch.slots.Values.ToList 'cpu memory slots
                                                        If cms.Type.MajorCode = "MEM" And (cms.path = "" Or cms.path = CPUpth$) Then

                                                            If Not slotstodel.Contains(cs) Then slotstodel.Add(cs) 'cs.delete(errormessages) 'we already have memory in force - just delete what was on the chassis branch
                                                            cpuHasMemory = True
                                                        End If
                                                    Next

                                                    If Not cpuHasMemory Then
                                                        '*copy* the memory off the chassis branch onto the cpu (With a specific path)
                                                        Dim cpuMem As New clsSlot(cs.Type, cpubranch, CPUpth, cs.numSlots, Nothing, New NullableInt(), 0, 0)
                                                        If Not slotstodel.Contains(cs) Then slotstodel.Add(cs)
                                                    End If
                                                End If
                                            End If
                                        Next
                                    End If
                                End If

                                If Not s.deleted Then
                                    If dicCPUqmax.ContainsKey(sysfam) Then
                                        If s.numSlots <> dicCPUqmax(sysfam) Then
                                            s.numSlots = dicCPUqmax(sysfam)
                                            s.update(errormessages)
                                        End If
                                    End If
                                End If
                                'Else
                                '                                s.Type = iq.i_slotType_Code("cpu")("gen_cpu")
                                'break
                                'non generic cpu 
                                ' Beep()
                                'End If
                            End If
                        Next
                    End If
                Else
                    Dim l$ = PathName(systemPath)
                    ' Beep()
                End If

                If cb.Translation.text(English).ToLower = "all options" Then
                    If cb IsNot currentalloptionsbranch Then
                        currentOptions.Clear()
                        cb.optionsBelow(cb.ID.ToString, currentOptions)
                        currentalloptionsbranch = cb
                    End If

                    For Each gb In cb.childBranches.Values
                        If gb.Translation.text(English).ToLower = "fios" Then
                            For Each fiob In gb.childBranches.Values 'option branch

                                Dim altsku As String = fiob.Product.FirstAttributeEnglishText("altsku")

                                If fiob.Product.ProductType.Code.ToLower = "cpu" Then
                                    'If Not ob.Product.isFIO Then
                                    If Not fiob.Product.SKU.StartsWith("###") Then

                                        If currentOptions.ContainsKey(fiob.Product) Then

                                            Dim optionpath$ = systemPath & "." & currentOptions(fiob.Product)
                                            Dim optionBranch As clsBranch = iq.Branches(Split(optionpath, ".").Last)

                                            '                                    Beep()
                                            'this FIO is an option (and that option should have the autoadd,memory slots and altSKU)
                                            'If ob.Quantities.Count > 0 Then

                                            Dim fiopath As String = systemPath & "." & cb.ID & "." & gb.ID & "." & fiob.ID
                                            Dim l$ = PathName(fiopath)
                                            Dim eaoa$ = PathName(systemPath & "." & currentOptions(fiob.Product))

                                            'move the quantities (autoadds) - onto the option (off the FIO)
                                            For Each q In fiob.Quantities.Values.ToList
                                                If q.Path = fiopath Or q.Path = "" Then
                                                    If Not optionBranch.hasQuantity(optionpath, q.Region) Then
                                                        q.Path = optionpath
                                                        q.Branch = optionBranch
                                                        fiob.Quantities.Remove(q.ID)
                                                        'optionBranch.Quantities.Add(q.ID, q) -not needed as the q.update does it
                                                        qm += 1
                                                        q.update(errormessages)
                                                    Else
                                                        q.deleted = True
                                                        qd += 1
                                                        q.update(errormessages)
                                                    End If
                                                End If
                                            Next

                                            For Each s In fiob.slots.Values.ToList
                                                'If s.path = "" Then Stop
                                                If s.path = fiopath Or s.path = "" Then
                                                    If Not optionBranch.HasMajorSlot(s.Type.MajorCode) Then '.ContainsKey(s.compoundKey) Then
                                                        s.path = optionpath
                                                        s.Branch = optionBranch

                                                        fiob.slots.Remove(s.ID)
                                                        optionBranch.slots.Add(s.ID, s)
                                                        ' optionBranch.i_Slots.Add(s.compoundKey, s) done by ste s.update
                                                        sm += 1
                                                        s.update(errormessages)
                                                    Else
                                                        s.deleted = True
                                                        s.update(errormessages)
                                                    End If
                                                    sd += 1

                                                End If
                                            Next

                                            'see if an option has this FIO as its altSKU (Ie. a b21 has this l21)


                                            '    'we have the wrong 'real' (B21) cpu listed as an FIO
                                            '    ob.Product = iq.Products(altsku)
                                            '    ob.Update(errormessages)

                                            'End If
                                        Else
                                            'it's NOT an option for the system (Probably an L21) or ### - and the autoadd and slots need to stay here
                                            'this system should have no processor !
                                        End If
                                    End If


                                    If altsku <> "" Then
                                        If iq.i_SKU.ContainsKey(altsku) Then
                                            If Not currentOptions.ContainsKey(iq.i_SKU(altsku)) Then
                                                'Beep()
                                            Else
                                                ' Beep()
                                            End If
                                        End If
                                    End If
                                End If
                            Next
                        End If
                    Next
                End If
            Next
        Next

        For Each s In slotstodel
            s.delete(errormessages)
        Next

        Debug.Print(qm, qd, sm, sd)

    End Sub

    Function MultiCPUs()






    End Function




    Public Function fixFilterDefaults()

        'Key>Text^Group
        Dim dt As New Dictionary(Of Integer, String) 'Deleted translations
        Dim sql$ = "SELECT [key],text,[group] FROM Translation WHERE deleted=1 AND fk_language_id=1"
        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim r As SqlClient.SqlDataReader
        r = da.DBExecuteReader(con, sql$)

        Dim duped As Integer = 0
        While r.Read
            Dim k As Integer = r.Item("key")
            If Not dt.ContainsKey(k) Then
                dt.Add(k, r.Item("text") & "^" & r.Item("group"))
            Else
                duped += 1
            End If

        End While
        con.Close()

        Dim em As New List(Of String)

        Dim cantfix As New List(Of String)
        For Each f In iq.Fields.Values.ToList
            If f.DefaultFilterValues <> "" Then
                Dim bits() As String = Split(f.DefaultFilterValues, "|")
                If bits.Count = 2 Then
                    Dim okey As Integer = bits(1) 'old key
                    If dt.ContainsKey(okey) Then
                        Dim kb() As String = Split(dt(okey), "^") 'Text^group (of old translations)
                        Dim nkey = iq.EnglishIndex(kb(0), kb(1)).Key
                        bits(1) = nkey
                        f.DefaultFilterValues = bits(0) & "^" & bits(1)
                        f.update(em)
                    Else
                        cantfix.Add(f.ID & "^" & okey)
                    End If

                End If
            End If
        Next

    End Function

    Public Function fixTranslations()

        'get the occurance of this phrase with the most translations first
        Dim sql$ = "select count(*) as c,[key] as k,[order],[group] from translation group by [key],[order],[group] order by c desc"

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim r As SqlClient.SqlDataReader

        'the iq.translations is already indexed by KEY - NOT ID
        r = da.DBExecuteReader(con, sql)

        Dim cks As New Dictionary(Of String, Integer)  'Compound key > key  'stores the best 'unique' versiona
        Dim mappings As New Dictionary(Of Integer, Integer) 'other keys >master key

        Dim counts As New Dictionary(Of String, Integer)
        Dim errormessages As List(Of String) = New List(Of String)

        While r.Read
            Dim k As Integer = r.Item("k")
            Dim t As clsTranslation = iq.Translations(k)

            If cks.ContainsKey(t.compoundkey(English)) Then
                'this is a dupe = we need to add a mapping..
                'of other key > best key
                If Not mappings.ContainsKey(r.Item("k")) Then
                    If cks(t.compoundkey(English)) <> r.Item("k") Then  'DON'T map rows to themselves !
                        mappings.Add(r.Item("k"), cks(t.compoundkey(English)))
                    End If
                End If
            Else
                'compound key will be text^group^language?
                cks.Add(t.compoundkey(English), r.Item("K"))  ' this will be our master
            End If

        End While
        r.Close()

        counts.Add("SlotTypes", 0)

        For Each st In iq.SlotTypes.Values
            With st.Translation
                If mappings.ContainsKey(.Key) Then
                    st.Translation = iq.Translations(mappings(.Key))
                    counts("SlotTypes") += 1
                    st.Update()
                End If
            End With
            If st.TranslationShort IsNot Nothing Then
                With st.TranslationShort
                    If mappings.ContainsKey(.Key) Then
                        st.TranslationShort = iq.Translations(mappings(.Key))
                        counts("SlotTypes") += 1
                        st.Update()
                    End If
                End With
            End If
        Next

        counts.Add("Promos", 0)
        For Each p In iq.Promos.Values
            With p.Description
                If mappings.ContainsKey(.Key) Then
                    Dim tt As clsTranslation = iq.Translations(mappings(.Key))
                    p.Description = tt
                    counts("Promos") += 1
                    p.update(errormessages)
                End If
            End With
        Next

        'Some transaltions are getting mapped to themselves !- yuck
        counts.Add("Attributes", 0)
        For Each a In iq.Attributes.Values

            With a.Translation
                If mappings.ContainsKey(.Key) Then
                    Dim tt As clsTranslation = iq.Translations(mappings(.Key))
                    If mappings.ContainsKey(mappings(.Key)) Then
                        Stop 'Doubly mapped
                    End If
                    a.Translation = tt
                    counts("Attributes") += 1
                    a.update(errormessages)
                End If
            End With
        Next


        counts.Add("ROKAttributes", 0)
        For Each la In iq.ROKAttributes.Values

            For Each Ra In la
                With Ra.Translation
                    If mappings.ContainsKey(.Key) Then
                        Dim tt As clsTranslation = iq.Translations(mappings(.Key))
                        If mappings.ContainsKey(mappings(.Key)) Then
                            Stop 'Doubly mapped
                        End If
                        Ra.Translation = tt
                        counts("ROKAttributes") += 1
                        Ra.update()
                    End If
                End With
            Next
        Next


        ''Attributes
        'counts.Add("Attributes", 0)
        'For Each a In iq.Attributes.Values
        '    With a.Translation
        '        If mappings.ContainsKey(.Key) Then
        '            a.Translation = iq.Translations(mappings(.Key))
        '            counts("Attributes") += 1
        '            a.update(errormessages)
        '        End If
        '    End With
        'Next

        'fields
        counts.Add("Fields", 0)
        For Each f In iq.Fields.Values
            With f.labelText
                If mappings.ContainsKey(.Key) Then
                    f.labelText = iq.Translations(mappings(.Key))
                    counts("Fields") += 1
                    f.update(errormessages)
                End If
            End With
        Next

        'Product Attributes (includes descrption & sku)
        counts.Add("ProductAttributes", 0)
        For Each p In iq.Products.Values
            For Each pa In p.Attributes.Values
                With pa.Translation
                    If pa.Translation IsNot Nothing Then
                        If mappings.ContainsKey(.Key) Then
                            pa.Translation = iq.Translations(mappings(.Key))
                            counts("ProductAttributes") += 1
                            pa.update(errormessages)
                        End If
                    End If
                End With
            Next
        Next

        'ProductTypes
        counts.Add("ProductTypes", 0)
        For Each pt In iq.ProductTypes.Values
            With pt.Translation
                If mappings.ContainsKey(.Key) Then
                    pt.Translation = iq.Translations(mappings(.Key))
                    counts("ProductAttributes") += 1
                    pt.Update()
                End If
            End With
        Next


        'states
        counts.Add("States", 0)
        For Each s In iq.States.Values
            With s.Translation
                If mappings.ContainsKey(.Key) Then
                    s.Translation = iq.Translations(mappings(.Key))
                    counts("States") += 1
                    s.Update()
                End If
            End With
        Next


        'units
        counts.Add("units", 0)
        For Each u In iq.Units.Values
            With u.Translation
                If mappings.ContainsKey(.Key) Then
                    u.Translation = iq.Translations(mappings(.Key))
                    counts("Units") += 1
                    u.Update(errormessages)
                End If
            End With
        Next

        'sector
        counts.Add("sectors", 0)
        For Each sctr In iq.Sectors.Values
            With sctr.Translation
                If mappings.ContainsKey(.Key) Then
                    sctr.Translation = iq.Translations(mappings(.Key))
                    sctr.update()
                    counts("sectors") += 1
                End If
            End With
        Next

        'regions
        counts.Add("regions", 0)
        For Each rgn In iq.Regions.Values
            With rgn.Name
                If mappings.ContainsKey(.Key) Then
                    rgn.Name = iq.Translations(mappings(.Key))
                    rgn.Update()
                    counts("regions") += 1
                End If
            End With
        Next



        'validation messages
        counts.Add("vm", 0)
        For Each vl In iq.ProductValidationsAssignment.Values
            For Each v In vl

                With v
                    If mappings.ContainsKey(.CorrectMessage.Key) Then
                        .CorrectMessage = iq.Translations(mappings(.CorrectMessage.Key))
                        v.Update()
                        counts("vm") += 1
                    End If
                    If mappings.ContainsKey(.Message.Key) Then
                        .Message = iq.Translations(mappings(.Message.Key))
                        v.Update()
                        counts("vm") += 1
                    End If

                End With
            Next
        Next


        'branches
        counts.Add("branches", 0)
        counts.Add("slots", 0)

        For Each branch In iq.Branches.Values


            Dim ub As Boolean = False
            With branch.Translation
                If mappings.ContainsKey(.Key) Then
                    branch.Translation = iq.Translations(mappings(.Key))
                    ub = True
                    counts("branches") += 1
                End If
            End With

            With branch.CollectiveNoun
                If mappings.ContainsKey(.Key) Then
                    branch.CollectiveNoun = iq.Translations(mappings(.Key))
                    ub = True
                End If
            End With

            With branch.collectiveNounSingular
                If mappings.ContainsKey(.Key) Then
                    branch.collectiveNounSingular = iq.Translations(mappings(.Key))
                    ub = True
                End If
            End With

            If ub Then branch.Update(errormessages)


            For Each s In branch.slots.Values
                If s.notes IsNot Nothing Then
                    With s.notes
                        If mappings.ContainsKey(.Key) Then
                            s.notes = iq.Translations(mappings(.Key))
                            counts("slots") += 1
                            s.update(errormessages)
                        End If
                    End With
                End If
            Next
            'quantities have no text

        Next

        con.Close()

        'map every FK through the mappings table

        'we can now delete all the translations in the keys of the mappings

        Dim todel As New List(Of String)
        For Each k In mappings.Keys
            todel.Add(CStr(k))
        Next

        If todel.Count Then
            Dim toskip As Integer = 0
            Dim chunk As Integer = 1000
            Do
                Dim ll = From j In todel.Skip(toskip).Take(chunk)
                If Not ll.Any Then Exit Do

                sql$ = "update translation set deleted=1 WHERE [key] IN(" & Join(ll.ToArray, ",") & ")"

                LongSQL(sql)
                toskip += 1000
            Loop

        End If


    End Function


    Public Sub Manufacturer()  'Manufactuer 'group' ?

        'get a definitive list of skus>manufactirer from iq1
        'Dim sql$ = "SELECT h.UPCNum, pl.PL, bu.IQBU, "
        'sql$ &= "CASE WHEN bu.IQBU IN ('IPG','PSG') THEN 'HPI' ELSE 'HPE' END as NewMfrCode,h.ccdescription "
        'sql$ &= "FROM h3.Channelcentral.products.Hierarchy h "
        'sql$ &= "INNER JOIN h3.ChannelCentral.products.Codes_PL pl ON h.PL = pl.PL "
        'sql$ &= "INNER JOIN h3.Channelcentral.products.TranslateBU bu ON bu.BUID2 = pl.BUCode "
        'sql$ &= "WHERE bu.IQBU IS NOT NULL "
        'sql$ &= "AND bu.IQBU <> 'SER' -- these are not iQuote products"


        Dim sql As String = "SELECT      h.UPCNum, pl.PL, bu.IQBU, bu.Mfr_Code AS NewMfrCode,h.ccdescription ,syssn,"
        sql &= "            ISNULL(s.ActiveFromDate, o.ActiveFromDate) ActiveFromDate, ISNULL(s.ActiveToDate, o.ActiveToDate) ActiveToDate, "
        sql &= "            ISNULL(s.EOL, o.EOL) EOL, ISNULL(s.Active, o.Active) Active,s.aaonly "
        sql &= "FROM h3.Channelcentral.products.Hierarchy h "
        sql &= "      LEFT JOIN h3.iQ.products.Systems s ON s.ModelSKU = h.UPCNum "
        sql &= "      LEFT JOIN h3.iQ.products.Options o ON o.OptSKU = h.UPCNum "
        sql &= "INNER JOIN h3.ChannelCentral.products.Codes_PL pl ON h.PL = pl.PL "
        sql &= "INNER JOIN h3.Channelcentral.products.TranslateBU bu ON bu.BUID2 = pl.BUCode AND ISNULL(bu.IQBU,'SER')<>'SER' "
        '     sql &= "AND ISNULL(s.Active,o.Active)=1"


        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql)

        'Dim dicMfr As Dictionary(Of String, String) = New Dictionary(Of String, String)

        'Dim tls As Dictionary(Of String, clsTranslation) = New Dictionary(Of String, clsTranslation)
        'tls.Add("HPI", iq.AddTranslation("HPI", English, "Division", 1, Nothing, 0, False))
        'tls.Add("HPE", iq.AddTranslation("HPE", English, "Division", 2, Nothing, 0, False))

        Dim niiq2 As HashSet(Of String) = New HashSet(Of String) 'Not in iq2 list

        Dim em As List(Of String) = New List(Of String)

        Dim updated As Integer = 0
        Dim skipped As Integer = 0

        updated += 1

        Dim iiq1 As New HashSet(Of String)

        Dim ad As Integer

        Dim ca As Integer
        While rdr.Read
            Dim sku As String = rdr.Item("upcnum")
            If sku = "589256-B21" Then Stop
            iiq1.Add(rdr.Item("upcnum"))
            If iq.i_SKU.ContainsKey(sku) Then
                Dim product As clsProduct = iq.i_SKU(sku)
                If product.SKU <> sku And product.SKU <> "" Then Stop

                Dim update As Boolean = False
                If product.mfrCode <> rdr.Item("newmfrcode") Then product.mfrCode = rdr.Item("newmfrcode") : update = True
                If product.buCode <> rdr.Item("iqbu") Then product.buCode = rdr.Item("iqbu") : update = True
                If product.plCode <> rdr.Item("pl") Then product.plCode = rdr.Item("pl") : update = True

                'Publish = NOT aaONLY
                If rdr.Item("aaonly") IsNot DBNull.Value Then 'AndAlso rdr.Item("aaonly") <> 0 Then

                    If product.Publish And rdr.Item("aaonly") = 1 Then
                        product.Publish = 0 : update = True
                    ElseIf product.Publish = False And rdr.Item("aaonly") = 0 Then
                        product.Publish = True : update = True
                    End If
                End If

                If rdr.Item("active") IsNot DBNull.Value Then
                    Dim active As Boolean = rdr.Item("active")
                    If product.Active <> active Then
                        ad += 1
                        product.Active = rdr.Item("active")
                        update = True
                    End If
                End If


                If rdr.Item("eol") IsNot DBNull.Value Then
                    Dim eol As Boolean = rdr.Item("eol")
                    If product.EOL <> eol Then
                        product.EOL = eol : update = True
                    End If

                Else
                    If product.Active <> 0 Then product.Active = 0 : update = True
                End If

                If rdr.Item("activeFromDate") IsNot DBNull.Value Then
                    If product.activeFrom <> rdr.Item("activeFromDate") Then product.activeFrom = rdr.Item("activeFromDate") : update = True
                End If
                If rdr.Item("activeToDate") IsNot DBNull.Value Then
                    If product.activeTo <> rdr.Item("activeToDate") Then
                        product.activeTo = rdr.Item("activeToDate") : update = True
                    End If
                End If

                If update Then
                    product.update(em) : updated += 1
                Else
                    skipped += 1
                End If
            Else
                If Not rdr.Item("syssn") Is DBNull.Value Then  'only output 'missing' systems
                    niiq2.Add(rdr.Item("upcnum"))  '& rdr.Item("ccdescription"))
                End If
            End If

            '        dicMfr.Add(sku, rdr.Item("newMfrCode") & "|" 
            '& rdr.Item("iqbu") & "|" & rdr.Item("pl"))
        End While
        rdr.Close()

        Dim niiq1 As New HashSet(Of String)
        For Each p In iq.Products.Values
            If p.hasSKU Then 'And p.mfrCode = "" Then
                If Not iiq1.Contains(p.SKU) Then
                    niiq1.Add(p.SKU)
                    'If p.Active = True Then
                    '    p.Active = False
                    '    p.update(em)
                    'Else
                    '    Beep()
                    'End If
                End If
            End If
        Next


        sql$ = "update product set active = 0 where sku in ('" & Join(niiq1.ToArray, "','") & "')"
        LongSQL(sql$)


        'Dim updates As Integer = 0
        'Dim SKUless As Integer = 0
        'Dim already As Integer = 0
        'Dim missing As New List(Of String)

        'For Each Product In iq.Products 'iq.i_SKU
        '    'sku>product
        '    If Product.i_attributes_code Then
        '        Dim sku As String = kvp.Key
        '        Dim product As clsProduct = kvp.Value

        '        'there are few products (Chassis and 'Family' products) that don't have skus
        '        If dicMfr.ContainsKey(sku) Then
        '            Dim b As String() = Split(dicMfr(sku), "|")
        '            Dim update As Boolean = False
        '            If product.SKU <> sku Then product.SKU = sku : update = True
        '            If product.mfrCode <> b(0) Then product.mfrCode = b(0) : update = True
        '            If product.buCode <> b(1) Then product.buCode = b(1) : update = True
        '            If product.plCode <> b(2) Then product.plCode = b(2) : update = True
        '            If update Then
        '                product.update(em) : updates += 1
        '            Else
        '                already += 1
        '            End If
        '        Else
        '            'skuless product . . set them all to HPE for now
        '            missing.Add(sku)
        '            SKUless += 1
        '        End If
        '    End If
        'Next

        Dim sw As StreamWriter = New StreamWriter("c:\temp\niiq2.txt")
        For Each s In niiq2
            sw.Write(s & ",")
        Next
        sw.Close()

        sw = New StreamWriter("c:\temp\niiq1.txt")
        For Each s In niiq1
            sw.WriteLine(s)
        Next
        sw.Close()

        con.Close()

    End Sub


    Public Function fixCurrencies(HOSTID As String, currency As clsCurrency, ByRef errs As List(Of String)) As Integer

        fixCurrencies = 0
        Dim errors As List(Of String) = New List(Of String)

        Dim okalready As Integer = 0
        If iq.i_channel_code.ContainsKey(HOSTID) Then
            Dim sc As clsChannel = iq.i_channel_code(HOSTID)
            sc.DefaultCurrency = currency
            sc.Update(errors)

            For Each ac In iq.Accounts.Values
                If ac.SellerChannel Is sc Then
                    If ac.Currency IsNot currency Then
                        ac.Currency = sc.DefaultCurrency
                        ac.update(errors)
                        fixCurrencies += 1
                    Else
                        okalready += 1
                    End If
                End If
            Next
            errs.Add("Updated " & fixCurrencies & "," & okalready & " were already OK -  success")
        Else
            errs.Add("No such host")
        End If

    End Function


    Public Function Tokens(errormessages As List(Of String)) As Integer

        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader

        Dim query As String = "SELECT PropName,valuechar from h3.iq.admin.properties"

        rdr = da.DBExecuteReader(con, query)


        Dim done As Integer

        While rdr.Read
            Dim bits() As String = Split(rdr.Item("Propname"), "_")
            If bits(0) = "TOKEN" Then
                If iq.i_channel_code.ContainsKey(bits(1)) Then
                    Dim c As clsChannel = iq.i_channel_code(bits(1))
                    If Not c.Code.StartsWith("DSYUS") And Not c.Code.StartsWith("DYSCA") Then
                        c.WebToken = rdr.Item("valuechar")
                        c.Update(errormessages)
                        done += 1
                    End If
                End If
            End If
        End While

        rdr.Close()
        con.Close()

        Return done

    End Function

    Public Function Incremental(lid As UInt64, skus As List(Of String)) As clsActionList  'Pass in a list of SKU's to add or update...

        If skus.Contains("ALL") Then

            skus.Clear()
            Dim sql2 As String = "SELECT  h.UPCNum, pl.PL, bu.IQBU, bu.Mfr_Code AS NewMfrCode," & _
            "            s.ActiveFromDate, s.ActiveToDate," & _
            "s.EOL, s.Active, s.aaonly " & _
            "FROM h3.Channelcentral.products.Hierarchy h " & _
            "LEFT JOIN h3.iQ.products.Systems s ON s.ModelSKU = h.UPCNum " & _
            "INNER JOIN h3.ChannelCentral.products.Codes_PL pl ON h.PL = pl.PL " & _
            "INNER JOIN h3.Channelcentral.products.TranslateBU bu ON bu.BUID2 = pl.BUCode " & _
            "AND s.Active=1"

            Dim con As SqlClient.SqlConnection = da.OpenDatabase
            Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql2)
            While rdr.Read
                skus.Add(rdr("upcnum"))
            End While
            rdr.Close()
            con.Close()
        End If

        Dim ActionList As clsActionList
        If Not ActionListLid.ContainsKey(lid) Then
            ActionList = New clsActionList()
            ActionListLid(lid) = ActionList
        Else
            ActionList = ActionListLid(lid)
        End If

        'Try
        ImportLog.Add(DateTime.Now, "Beginning Import")

        Dim errorMessages As List(Of String) = New List(Of String)()

        'Collect system information
        'fetch all the skus we need to update (thae EXIST in IQ2
        Dim updateSkus = iq.i_SKU.Where(Function(s) skus.Contains(s.Key)).Select(Function(s) s.Key).ToList() 'What do we have?
        'The rest we're going to need to ADD
        Dim addSkus = skus.Where(Function(s) Not iq.i_SKU.ContainsKey(s)).ToList() 'What do we need?

        ImportLog.Add(DateTime.Now, String.Format("Found: {0} SKU's to update and {1} SKU's to add", updateSkus.Count, addSkus.Count))

        'Create lDic...
        'Find the alloptions branch? - This WAS for SBSO - (but doesnt exist yet)
        Dim ck As String = ""
        Dim lDic As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)()
        For Each branch In iq.RootBranch.childBranches
            If branch.Value.Translation.text(English).ToLower = "accessories and services" Then
                branch.Value.BuildPathDic("", lDic, False)
                Exit For
            End If
        Next

        LoadAbbreviations(da.OpenDatabase())

        Dim dicSlotTypes As Dictionary(Of String, clsSlotType)
        dicSlotTypes = Import.slotTypes(da.OpenDatabase(), Nothing, True) '20 secs
        Dim dicOptLocalization As Dictionary(Of clsProduct, List(Of clsRegion)) = New Dictionary(Of clsProduct, List(Of clsRegion))()


        Dim AllowDelete As Boolean = True

        'Add any missing families...
        ImportLog.Add(DateTime.Now, String.Format("Checking if new families are needed"))
        Dim famlocs As Dictionary(Of String, String) = FamiliesInc(da.OpenDatabase(), String.Join(",", skus.Select(Function(ass) da.SqlEncode(ass))))


        'Add any systems, branches and products with attributes
        ImportLog.Add(DateTime.Now, String.Format("checking for systems"))
        SystemsInc(da.OpenDatabase(), String.Join(",", skus.Select(Function(ass) da.SqlEncode(ass))), errorMessages, ActionList, AllowDelete, famlocs)

        'Options for systems

        Dim AllSystemOptions As New List(Of String)
        Dim systemOptionsToAdd As New List(Of String)

        Dim Sql = "SELECT DISTINCT familycode,sysfamilyname " & _
            "FROM h3.iq.products.systems " & _
            "INNER JOIN h3.iq.products.sysfamilydefinitions sd ON sd.sysfamily=familycode " & _
            "WHERE modelsku IN (" & String.Join(",", skus.Select(Function(ass) da.SqlEncode(ass))) & ")"

        Dim rdr3 = da.DBExecuteReader(da.OpenDatabase(), Sql)
        Dim famList = New HashSet(Of String)(StringComparer.CurrentCultureIgnoreCase)
        While rdr3.Read
            If Not famList.Contains(rdr3("familycode")) Then famList.Add(UCase(rdr3("familycode")))
            If Not famList.Contains(rdr3("sysfamilyname")) Then famList.Add(UCase(rdr3("sysfamilyname")))
        End While
        rdr3.Close()

        Sql = "SELECT optsku,sysfamily FROM h3.iq.products.options WHERE sysfamily is not null and opttype<>'wty'"
        rdr3 = da.DBExecuteReader(da.OpenDatabase(), Sql)

        While rdr3.Read
            Dim sp = Split(rdr3("sysfamily"), ",")
            For Each s In sp

                If Not String.IsNullOrEmpty(s) AndAlso famList.Contains(s) Then
                    Dim optsku As String = rdr3("optsku")
                    If Not iq.i_SKU.ContainsKey(optsku) Then
                        If Not systemOptionsToAdd.Contains(optsku) Then
                            systemOptionsToAdd.Add(optsku)
                        End If
                    End If

                    If Not AllSystemOptions.Contains(optsku) Then
                        AllSystemOptions.Add(optsku)
                    End If
                End If
            Next
        End While
        rdr3.Close()

        'Now add any FIO's
        Sql = "SELECT psu,cpu,ram,WLAN,WWAN,FAN,PriStor,SecStor,RAID,NIC,ICEincluded,options,software,ILOLicense from h3.iq.products.systems where modelsku IN (" & String.Join(",", skus.Select(Function(ass) da.SqlEncode(ass))) & ")"
        rdr3 = da.DBExecuteReader(da.OpenDatabase(), Sql)
        While rdr3.Read
            For i = 0 To rdr3.FieldCount - 1
                If Not IsDBNull(rdr3(i)) Then
                    Dim sp = Split(rdr3(i), ",")
                    For Each s In sp
                        If Not AllSystemOptions.Contains(s) And s.Contains("###") = False Then
                            If Not AllSystemOptions.Contains(s) Then AllSystemOptions.Add(s)
                        End If

                        If Not iq.i_SKU.ContainsKey(s) And s.Contains("###") = False Then
                            If Not systemOptionsToAdd.Contains(s) Then systemOptionsToAdd.Add(s)
                        End If
                    Next
                End If
            Next
        End While
        rdr3.Close()

        Dim optSkus As List(Of String) = New List(Of String)()

        ImportLog.Add(DateTime.Now, String.Format("Adding System Options"))

        If systemOptionsToAdd.Any Then
            'this is really only maintaining the 'accessories and services' catalogue - see buildTreeInc for system options
            optSkus.AddRange(optionsIncremental(da.OpenDatabase(), systemOptionsToAdd, iq.i_unit_code, lDic, dicOptLocalization, ActionList, AllowDelete))
        End If


        'Add any options, branches and products with attributes
        'ImportLog.Add(DateTime.Now, String.Format("Adding Options"))
        'optSkus.AddRange(optionsIncremental(da.OpenDatabase(), skus, iq.i_unit_code, lDic, dicOptLocalization, ActionList, AllowDelete))

        ''NOTE - AT THIS POINT WE HAVE SYSTEMS AND OPTIONS BUT NO RELATIONSHIPS
        ImportLog.Add(DateTime.Now, String.Format("Building Tree"))
        ' If optSkus.Count > 0 Then 


        If AllSystemOptions.Contains("AN975A") Then
            ' Beep()

        End If

        Dim sw As New StreamWriter("c:\temp\allSystemOptions.txt")
        For Each l In AllSystemOptions
            sw.WriteLine(l)
        Next
        sw.Close()

        Buildtreeinc(da.OpenDatabase(), AllSystemOptions, dicOptLocalization, dicSlotTypes, ActionList, AllowDelete, skus)

        'option compatibility is defined gasint the broad falimi

        'Add options to relevant systems, scan the tree to find where the options go

        'AddAndCheckOptionsForSystem()

        'AddOptionToSystemsAndCheck()
        ImportLog.Add(DateTime.Now, String.Format("Complete"))

        Return ActionList
        'Create preinstalled quantity information FIO and autoadd's by region

        'Add slot information

        'Populate attributes
        '        Catch ex As Exception
        ' ImportLog.Add(DateTime.Now, "Exception: " & ex.Message & "-" & If(ex.InnerException IsNot Nothing, ex.InnerException.Message, ""))
        ' Return ActionList
        ' End Try

    End Function

    Public Sub SystemsInc(con As SqlClient.SqlConnection, skus As String, ByRef errormessages As List(Of String), ActionList As clsActionList, AllowDelete As Boolean, famlocs As Dictionary(Of String, String))
        Dim U As clsProductAttribute
        Dim aa As clsAttribute
        If Not iq.i_attribute_code.ContainsKey("U") Then
            aa = New clsAttribute("U", iq.AddTranslation("U", English, "U", 0, Nothing, 0, False), 0)
        End If

        Dim pristorAtt
        If Not iq.i_attribute_code.ContainsKey("PriStor") Then
            pristorAtt = New clsAttribute("PriStor", iq.AddTranslation("Primary Storage (import only)", English, "UI", 0, Nothing, 0, False), 0)
        Else
            pristorAtt = iq.i_attribute_code("PriStor")
        End If

        Dim sctl = iq.AddTranslation("Supply Chain", English, "cats", 0, Nothing, 0, False)

        'returns a dictionary of system branches by ModelSKU

        Dim nextPaID = -1, nextQuantityId = -1
        Dim Product As clsProduct
        Dim sysBranch As clsBranch 'used to create systems (which go into the dictionaries)

        Dim AttribWriteCache As New DataTable
        AttribWriteCache = da.MakeWriteCacheFor(con, "ProductAttribute", nextPaID, True)

        Dim QtyWritecache As DataTable = da.MakeWriteCacheFor(con, "Quantity", nextQuantityId, True)
        'Disabling bulkwrite 
        'QtyWritecache = Nothing
        'AttribWriteCache = Nothing
        'Dim dicFormFactors As Dictionary(Of String, clsTranslation) = FormFactors(con)

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$

        'small dictionary of supply chains to their translations keys - used to look up 
        'the supply chain branches (under the family branches) 

        'supply chains are obsoleted (before they ever really saw the light of day!)
        Dim dicChains As New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)
        dicChains = New Dictionary(Of String, clsTranslation)

        'hard coded - until someone can tell me where to find the full supply chain names/list
        dicChains.Add("A", iq.AddTranslation("Regular models", English, "SC", 10, Nothing, 0, False))
        dicChains.Add("TV", iq.AddTranslation("Top value", English, "SC", 20, Nothing, 0, False))
        dicChains.Add("SB", iq.AddTranslation("Smart buy", English, "SC", 30, Nothing, 0, False))
        dicChains.Add("R", iq.AddTranslation("HP Renew", English, "SC", 30, Nothing, 0, False))
        dicChains.Add("PR", iq.AddTranslation("Promotional", English, "SC", 30, Nothing, 0, False))
        dicChains.Add("GO", iq.AddTranslation("Golden Offers", English, "SC", 30, Nothing, 0, False))


        'the focus attributes are matched against the code (but theyr'e attributes - so they need trasnlations (until and unless we invent a text type for attributes!)
        Dim dicSC As New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)
        dicSC = New Dictionary(Of String, clsTranslation)

        dicSC.Add("A", iq.AddTranslation("A", English, "SCC", 10, Nothing, 0, False))
        dicSC.Add("TV", iq.AddTranslation("TV", English, "SCC", 20, Nothing, 0, False))
        dicSC.Add("SB", iq.AddTranslation("SB", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("R", iq.AddTranslation("R", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("PR", iq.AddTranslation("PR", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("GO", iq.AddTranslation("GO", English, "SCC", 30, Nothing, 0, False))


        Dim sysTypeToPortfolio As Dictionary(Of String, clsTranslation) = New Dictionary(Of String, clsTranslation)

        'FYI
        'HP's Corporate hierarchy goes
        'Division (ESSN..
        '  BU (business unit) ISS/PSG/HPN/SWD
        '     Exhibit  'Desktops/Notebooks

        sysTypeToPortfolio.Add("DTO", iq.AddTranslation("PSG", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("HPN", iq.AddTranslation("HPN", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("IPG", iq.AddTranslation("IPG", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("NBK", iq.AddTranslation("PSG", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("SVR", iq.AddTranslation("ISS", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("SWD", iq.AddTranslation("SWD", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("PSG", iq.AddTranslation("PPS", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("RAK", iq.AddTranslation("ISS", English, "BU", 1, Nothing, 0, False))

        'Create a dictionary of all the abbreviations referenced in any of these columns (of products.union_systems)
        'these are NOT the columns which contain only part numbers (RAM,discretegraphics, etc - handled in import.fios()
        'theyre the ones that may have abbreviations in

        Dim columns As String = "extras,options,software,warrantyCode,formFactor,mfrBuildCode,display,intVideo,ILOhardware,terStorTech,raidTech"

        'extras and options contain moslty abbreviations - but some part no's
        'software contains a CD list of abbreviations

        Dim optabbreviations As Dictionary(Of String, clsTranslation)
        optabbreviations = Import.OptAbbreviations(con, columns)

        If Not optabbreviations.ContainsKey("TOWER") Then
            optabbreviations.Add("TOWER", iq.AddTranslation("Tower", English, "FF", 100, Nothing, 0, False))
        End If

        If Not optabbreviations.ContainsKey("BLADE") Then
            optabbreviations.Add("BLADE", iq.AddTranslation("Blade", English, "FF", 90, Nothing, 0, False))
        End If

        columns = Replace(columns, "ILOhardware", "Sys.ILOhardware") 'the column nane is ambiguous otherise (this isn't pretty - but it's only an import)


        'Build a dictionary of all xtext for systems we're concerened with
        '(Running a query per system in import.xtext was VERY slow)
        Dim xtdic As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))
        Dim xrdr As SqlClient.SqlDataReader

        sql = "SELECT  [SKU],[SKUtext],[SysFamilyShowText],[SysFamilyHideText],[MsgType] from " & server$ & "[iq].[Products].[TextExt]  WHERE SKU in(" & skus & ");"

        xrdr = da.DBExecuteReader(con, sql)

        While xrdr.Read
            Dim sku As String = xrdr.Item("sku")
            If Not xtdic.ContainsKey(sku) Then
                xtdic.Add(sku, New List(Of String))
            End If

            Dim mt As Object = xrdr.Item("msgtype")
            If mt Is DBNull.Value Then mt = "NULL"
            xtdic(sku).Add(xrdr.Item("skuText") & "^" & xrdr.Item("sysFamilyShowText") & "^" & xrdr.Item("sysFamilyHideText") & "^" & mt)

        End While
        xrdr.Close()





        Dim nextkey As Integer = clsTranslation.NextKey()
        Dim Tlwc As New DataTable
        Tlwc = da.MakeWriteCacheFor(con, "Translation")

        'Tlwc = Nothing
        ' nextkey = 0
        sql$ = "SELECT h.ccdescription,familyPriStor,familySecStor,busunit,modelSKU,sysfamilyname,familycode,cpu,sfd.systype,h.ccDescription as [desc],Isnull([SupplyChainCode],'A') as [supplyChainCode],sfd.u, Activesites,sfd.instformfactor, "
        sql$ &= columns  'THIS FORMS THE BULK OF THE SPEC TABLE
        sql$ &= ",alsoHost,extras,options,[DiscreteGraphics],[IntVideo],[InstVGA],[WLAN],[WWAN],[InstNIC]"
        sql$ &= ",productNote,vga,energystar,weightUnboxed,activeFromDate,activeToDate,active,eol,sfd.sysfamilyimg,aaOnly,isnull(pl,'none') as pl "
        sql$ &= "FROM " & server$ & "[iq].products.union_systems sys "
        sql$ &= "INNER join " & server$ & "[iq].products.union_sysfamilydefinitions sfd ON sfd.SysFamily=sys.FamilyCode "
        sql$ &= "INNER join " & server$ & "[iq].products.hierarchyiq h ON modelSKU=h.UPCNum "
        sql$ &= "WHERE modelsku in (" & skus & ")"


        columns = Replace(columns, "Sys.ILOhardware", "ILOhardware") 'put it back so we can pull out this column later
        rdr = da.DBExecuteReader(con, sql$)

        Dim FamMajor As clsProductAttribute
        Dim FamMinor As clsProductAttribute
        Dim FamDisp As clsProductAttribute

        Dim cpuSKU As clsProductAttribute
        Dim mfrSKU As clsProductAttribute
        Dim PLcode As clsProductAttribute

        Dim sector As clsSector
        Dim sysTrans As clsTranslation = iq.AddTranslation("systems", English, "collect", 0, Tlwc, nextkey, False)
        Dim sysTransSingular As clsTranslation = iq.AddTranslation("system", English, "collect", 0, Tlwc, nextkey, False)

        Dim optTrans As clsTranslation = iq.AddTranslation("options", English, "collect", 0, Tlwc, nextkey, False)
        Dim optTransSingular As clsTranslation = iq.AddTranslation("option", English, "collect", 0, Tlwc, nextkey, False)
        Dim textUnit As clsUnit = iq.i_unit_code("txt")

        Dim att As clsAttribute = Nothing

        Dim tlyes As clsTranslation = iq.AddTranslation("Yes", English, "hasFeature", 0, Tlwc, nextkey, False)

        Dim jj = InStr(skus, "QK765A")


        Dim sqlMfr As String = "SELECT      h.UPCNum, pl.PL, bu.IQBU, bu.Mfr_Code AS NewMfrCode,h.ccdescription ,"
        sqlMfr &= "            ISNULL(s.ActiveFromDate, o.ActiveFromDate) ActiveFromDate, ISNULL(s.ActiveToDate, o.ActiveToDate) ActiveToDate, "
        sqlMfr &= "            ISNULL(s.EOL, o.EOL) EOL, ISNULL(s.Active, o.Active) Active "
        sqlMfr &= "FROM h3.Channelcentral.products.Hierarchy h "
        sqlMfr &= "      LEFT JOIN h3.iQ.products.Systems s ON s.ModelSKU = h.UPCNum "
        sqlMfr &= "      LEFT JOIN h3.iQ.products.Options o ON o.OptSKU = h.UPCNum "
        sqlMfr &= "INNER JOIN h3.ChannelCentral.products.Codes_PL pl ON h.PL = pl.PL "
        sqlMfr &= "INNER JOIN h3.Channelcentral.products.TranslateBU bu ON bu.BUID2 = pl.BUCode AND ISNULL(bu.IQBU,'SER')<>'SER' "
        sqlMfr &= "AND ISNULL(s.Active,o.Active)=1 and  h.UPCNum in (" & skus & ")"

        ''" & rdr.Item("ModelSKU") & "'"

        Dim rdrMfr As SqlClient.SqlDataReader
        rdrMfr = da.DBExecuteReader(con, sqlMfr)

        Dim dicmfr As New Dictionary(Of String, String)
        While rdrMfr.Read
            dicmfr.Add(rdrMfr.Item("upcnum"), rdrMfr("NewMfrCode") & "^" & rdrMfr("PL") & "^" & rdrMfr("IQBU"))
            'mfrCode = rdrMfr("NewMfrCode")
            'mfrplcode = rdrMfr("PL")
            'mfrbuCode = rdrMfr("IQBU")

        End While
        rdrMfr.Close()

        If Not rdr.HasRows Then
            ImportLog.Add(DateTime.Now, String.Format("no systems affected"))
        Else
            While rdr.Read
                ImportLog.Add(DateTime.Now, String.Format("Checking system:" & rdr.Item("modelSku")))
                If LCase(Left$(rdr.Item("ModelSKU"), 1)) <> "x" Then ' do not import systems begging with X they are 'fake'

                    'Gather some common information about the system to create / check 
                    sector = iq.i_sector_code("HP" & rdr.Item("busunit"))

                    Dim activeTo As Date = CDate("31/12/2100")
                    If Not IsDBNull(rdr.Item("activeToDate")) Then activeTo = rdr.Item("activetodate")

                    Dim publish As Boolean = True
                    If rdr.Item("AAonly") <> 0 Then
                        publish = False
                    End If

                    Dim Inserting As Boolean = True
                    'Get the product if we already have it otherwise create a new one

                    Dim modelsku As String = rdr.Item("ModelSKU")
                    '      If modelsku = "QK765A" Then Stop



                    If Not dicmfr.ContainsKey(modelsku) Then
                        Beep()
                    Else
                        Dim b() As String = Split(dicmfr(modelsku), "^")
                        Dim mfrCode As String = b(0)
                        Dim mfrbuCode As String = b(1)
                        Dim mfrplcode As String = b(2)


                        Dim ep As clsProduct
                        If iq.i_SKU.ContainsKey(modelsku) Then
                            ep = iq.i_SKU(modelsku)
                            If Not ep.isSystem() Then
                                ep.isSystem = True
                                ep.update(errormessages)
                                ImportLog.Add(DateTime.Now, String.Format("SETTING SYSTEM FLAG:" & rdr.Item("modelSku")))
                                Inserting = False 'lets' be explicit about this
                            Else
                                ' Product = iq.i_SKU(rdr.Item("ModelSKU")) ---Do not  get the current iq2 data
                                Inserting = False
                                'Reconstruct a product only in memory from iq1 data
                                Product = New clsProduct(modelsku, True, False, sector, iq.i_ProductType_Code(rdr.Item("systype")), rdr.Item("activefromdate"), activeTo, rdr.Item("active"), rdr.Item("eol"), publish, mfrCode, mfrbuCode, mfrplcode, Nothing, -1, True)
                            End If

                        Else
                            Product = New clsProduct(modelsku, True, False, sector, iq.i_ProductType_Code(rdr.Item("systype")), rdr.Item("activefromdate"), activeTo, rdr.Item("active"), rdr.Item("eol"), publish, mfrCode, mfrbuCode, mfrplcode) 'this IS a system 
                            ImportLog.Add(DateTime.Now, String.Format("ADDING:" & rdr.Item("modelSku")))
                        End If


                            'Make a focus attibute based on the system type (lightly translated to the portfolio)
                            'ISS PSG SWD
                            Dim FA As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("FOCUS"), 0, textUnit, sysTypeToPortfolio(rdr.Item("systype")), AttribWriteCache, Not Inserting)
                            Dim scc As String = rdr.Item("supplyChainCode")
                            If dicChains.ContainsKey(scc) Then
                                'If Product.Attributes.Count > 0 Then
                                '    Dim scFound = From f In Product.Attributes.Values Where f.Attribute.Code = "SC"

                                'End If

                                Dim sc As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("SC"), 0, iq.i_unit_code("txt"), dicChains(scc), AttribWriteCache, Not Inserting)
                                Dim SCFA As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("FOCUS"), 0, textUnit, dicSC(scc), AttribWriteCache, Not Inserting)
                            End If


                            If Not IsDBNull(rdr.Item("U")) Then
                                'U = New clsProductAttribute(Product, iq.i_attribute_code("U"), rdr.Item("U"), iq.i_unit_code("U"),  iq.AddTranslation(rdr.Item("U") & " U", English, "U"), AttribWriteCache)
                                U = New clsProductAttribute(Product, iq.i_attribute_code("U"), rdr.Item("U"), iq.i_unit_code("U"), Nothing, AttribWriteCache, Not Inserting)
                            End If

                            If Not IsDBNull(rdr.Item("productNote")) Then
                                If Trim$(rdr.Item("productnote")) <> "" Then
                                    Dim note As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("note"), 0, textUnit, iq.AddTranslation(rdr.Item("productNote"), English, "ProdNote", 0, Tlwc, nextkey, True), AttribWriteCache, Not Inserting)
                                End If
                            End If

                            If Not IsDBNull(rdr.Item("EnergyStar")) Then
                                If CType(rdr.Item("energystar"), Integer) > 0 Then
                                    Dim es As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("eStar"), 1, textUnit, tlyes, AttribWriteCache, Not Inserting)
                                End If
                            End If

                            If Not IsDBNull(rdr.Item("WLAN")) Then
                                Dim wl As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("WLAN"), 1, textUnit, tlyes, AttribWriteCache, Not Inserting)
                            End If

                            If Not IsDBNull(rdr.Item("WWAN")) Then
                                Dim ww As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("WWAN"), 1, textUnit, tlyes, AttribWriteCache, Not Inserting)
                            End If


                            If Not IsDBNull(rdr.Item("vga")) AndAlso CInt(rdr("vga")) = 1 Then
                                Dim note As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("vga"), 1, textUnit, tlyes, AttribWriteCache, Not Inserting)
                            End If


                            'will need to do the same for the secondary storage/optical drives
                            If Not IsDBNull(rdr.Item("FamilyPriStor")) Then
                                'optfamily translation  -- This is a code like NHP355SFF
                                Dim oftl As clsTranslation = iq.AddTranslation(rdr.Item("familypristor"), English, "", 0, Tlwc, nextkey, False)
                                Dim pristor As clsProductAttribute = New clsProductAttribute(Product, pristorAtt, 0, textUnit, oftl, AttribWriteCache, Not Inserting)
                            End If


                            'same as formfactor

                            'If Not IsDBNull(rdr.Item("instFormFactor")) Then
                            ' Dim FormF As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("formFactor"), 1, textUnit, iq.AddTranslation(rdr.Item("instFormFactor"), English, "FF"), AttribWriteCache)
                            ' End If


                            If Not IsDBNull(rdr.Item("weightUnboxed")) Then
                                '21kg&nbsp;&nbsp;(46.30lb)
                                'take the --- text out and use the conversions

                                Dim wu$ = rdr.Item("weightUnboxed")
                                Dim p$() = Split(wu$, "kg")
                                If UBound(p$) <> 1 Then Stop
                                Dim kg As Single = Val(p$(0))
                                'Dim tl As clsTranslation = iq.AddTranslation(wu$, English, "WU", 0, Nothing, True)
                                '     Dim mass As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("mass"), kg, textUnit, tl, AttribWriteCache)
                                Dim mass As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("mass"), kg, iq.i_unit_code("kg"), Nothing, AttribWriteCache, Not Inserting)

                            End If

                            'MAKE THE MAJOR SPEC TABLE ATTRIBUTES - preinstalled options are in import.FIOs()
                            'Make an attribute for every abbreviation referenced in the various COLUMNS of products.union_systems
                            Dim pa As clsProductAttribute
                            Dim abtl As clsTranslation          'abbreviation translation
                            For Each k In Split(columns, ",")

                                ' If k = "formFactor" Then Stop
                                If Not IsDBNull(rdr.Item(k)) Then
                                    'some of the columns (notably options and extras) contain CD lists

                                    Dim nv As Single = -1
                                    If k = "display" Then
                                        If InStr(rdr.Item(k), "_") Then
                                            Dim p() As String = Split(rdr.Item(k), "_")
                                            Dim res As String = p(3)
                                            If res = "LED" Then res = p(4)
                                            Dim dm As String() = Split(res, "x")
                                            If dm.Count > 1 Then
                                                nv = Val(dm(0)) * Val(dm(1)) 'find the number of pixels
                                                If nv = 0 Then Stop 'we create the productattribute a little later

                                                If Not iq.i_attribute_code.ContainsKey("displaySize") Then
                                                    Dim ds As clsAttribute = New clsAttribute("displaySize", iq.AddTranslation("Display Size (diagonal)", English, "DispSZ", 0, Tlwc, nextkey, False), 0)
                                                End If

                                                'DIS_15.6_WXGA_1366x768_AGBV
                                                pa = New clsProductAttribute(Product, iq.i_attribute_code("displaySize"), p(1), iq.i_unit_code("Inch"), Nothing, AttribWriteCache, Not Inserting)
                                            End If
                                            'If InStr(p(1)(3)),"x" then do something here for megapixels

                                        End If
                                    End If

                                    If k = "extras" Or k = "options" Or k = "software" Or k = "raidTech" Then
                                        For Each ik In Split(rdr.Item(k), ",")
                                            'for each of the CD values ad an attribute of the type of the value
                                            'abtl = optabbreviations(UCase(k)) 'abbreviations translation of MCR,CAM,SDR,BT etc
                                            If Not iq.i_attribute_code.ContainsKey(Left(ik, 20)) Then
                                                'we don't have an MCR, CAM, SDR *attribute* yet - so make one
                                                If Not optabbreviations.ContainsKey(UCase(ik)) Then
                                                    'well it wasn't in abbreviations - so it's *probably* a part number.. or maybe something like "keyboard kit"
                                                    ' Stop
                                                    'append it to the "additional" attribute
                                                    att = Nothing
                                                Else
                                                    If LCase(ik) = "name" Then Stop
                                                    att = New clsAttribute(Left(ik, 20), optabbreviations(UCase(ik)), 0)  'an MCR,CAM,SDR (or some other recogised abbreviation)
                                                End If
                                            End If

                                            If Not att Is Nothing Then
                                                att = iq.i_attribute_code(Left(ik, 20))                                    '                                                                                      yes
                                                If Not Product.i_Attributes_Code.ContainsKey(att.Code) Then
                                                    pa = New clsProductAttribute(Product, att, 1, textUnit, Nothing, AttribWriteCache, Not Inserting)
                                                Else
                                                    Product.i_Attributes_Code(att.Code)(0).NumericValue += 1
                                                    Product.i_Attributes_Code(att.Code)(0).update(errormessages)
                                                    Logit("duplicate " & k & ":" & ik)
                                                End If
                                            End If
                                        Next
                                    Else
                                        'add an attribute of the type of the column header (e.g. warrantyCode,formFactor,mfrBuildCode,display,intVideo,ILOhardware,terStorTech,raidTech"
                                        ''  If InStr(rdr.Item(k), ",") Then Stop
                                        If LCase(rdr.Item(k)) = "name" Then Stop
                                        If LCase(k) = "name" Then Stop
                                        If optabbreviations.ContainsKey(UCase(rdr.Item(k))) Then
                                            abtl = optabbreviations(UCase(rdr.Item(k))) 'the translation of theis abbreviation will (should) alrery exist .. eg."WTY111NBD" = [[IQ.clsLanguage, 1 Year Parts / 1 Year Labour / 1 Year Onsite Warranty Next Business Day]
                                            pa = New clsProductAttribute(Product, iq.i_attribute_code(k), nv, textUnit, abtl, AttribWriteCache, Not Inserting)
                                        Else
                                            'Something for which there was no IQ1 abbreviation like 'french keyboard' or EMA7029 
                                            '    Beep()
                                        End If
                                    End If
                                End If
                            Next

                            'This is done in import descriptions
                            ' Dim desc = New clsProductAttribute(Product, iq.Attributes("desc"), 0, iq.Units("txt"), iq.AddTranslation(Trim$(rdr.Item("desc"))).Key, AttribWriteCache)

                            Dim sku$
                            sku$ = Trim$(rdr.Item("modelsku"))
                            If InStr(LCase(sku$), "paul") Then Stop
                            If sku$ = "" Then Stop

                            ' mfrSKU = New clsProductAttribute(Product, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(sku$, English, "SKU", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)
                            PLcode = New clsProductAttribute(Product, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(rdr.Item("pl"), English, "PL", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)


                            'for systems - their 'name' *is* their part number
                            ' SystemName = New clsProductAttribute(Product, iq.i_attribute_code("~ame"), 0, textUnit, mfrSKU.Translation, AttribWriteCache)

                            ' If InStr(LCase(SystemName.displayName(English)), "paul") Then Stop
                            'SystemName = New clsProductAttribute(Product, iq.Attributes("~ame"), 0, iq.Units("txt"), iq.AddText(rdr.Item("familycode"), s_lang, TranslationWriteCache).Key, AttribWriteCache)

                            'product attributes are a list of each type.. so we can have multiple alsohosts and don't need a horrid comma separated list)
                            Dim alsoHost As clsProductAttribute
                            If Not IsDBNull(rdr.Item("alsohost")) Then
                                For Each h In Split(rdr.Item("alsoHost"), ",")
                                    alsoHost = New clsProductAttribute(Product, iq.i_attribute_code("alsoHost"), 0, textUnit, iq.AddTranslation(rdr.Item("alsoHost"), English, "", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)
                                Next
                            End If

                            Dim fn$ = rdr.Item("sysFamilyname")

                            'DO NOT unabreviate it here !!
                            'If dicAbbreviations.ContainsKey(fn$) Then fn$ = dicAbbreviations(fn$)
                        'NOTE - the translations of the family name won't be duplicated - so, although every system will have a family attribute - all those attributes to a s set of a hundred or so tranlsations
                        If Not Product.i_Attributes_Code.ContainsKey("FamMajor") Then
                            FamMajor = New clsProductAttribute(Product, iq.i_attribute_code("FamMajor"), 0, textUnit, iq.AddTranslation(fn$, English, "FamMajor", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)
                        End If

                        If dicAbbreviations.ContainsKey(fn$) Then fn$ = dicAbbreviations(fn$)
                        FamDisp = New clsProductAttribute(Product, iq.i_attribute_code("FamDisp"), 0, textUnit, iq.AddTranslation(fn$, English, "FamDisp", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)


                        'Family Minor -- (Familycode - granular)
                        Dim tl As clsTranslation
                        '   nextkey = 0
                        '   Tlwc = Nothing
                        tl = iq.AddTranslation(rdr.Item("ccdescription"), s_lang, "sysDesc", 0, Tlwc, nextkey, False)
                        Dim desc = New clsProductAttribute(Product, iq.i_attribute_code("desc"), 0, textUnit, tl, AttribWriteCache, Not Inserting)

                        Dim prda As clsProductAttribute
                        'Also Included and Options
                        If Not IsDBNull(rdr.Item("Extras")) Then
                            prda = New clsProductAttribute(Product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("Extras"), English, "", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)
                        End If

                        If Not IsDBNull(rdr.Item("Options")) Then
                            prda = New clsProductAttribute(Product, iq.i_attribute_code("Options"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("Options"), English, "", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)
                        End If


                        'Graphics
                        Dim linkAttributes As String = ""
                        If rdr("DiscreteGraphics") IsNot Nothing AndAlso rdr("DiscreteGraphics") IsNot DBNull.Value Then
                            linkAttributes += rdr("DiscreteGraphics")
                        ElseIf rdr("IntVideo") IsNot Nothing AndAlso rdr("IntVideo") IsNot DBNull.Value Then
                            linkAttributes += rdr("IntVideo")
                        ElseIf rdr("InstVGA") IsNot Nothing AndAlso rdr("InstVGA") IsNot DBNull.Value Then
                            linkAttributes += rdr("InstVGA")
                        End If
                        prda = New clsProductAttribute(Product, iq.i_attribute_code("Graphics"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "VID", 1, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)

                        'Networking
                        linkAttributes = ""
                        If rdr("WLAN") IsNot Nothing AndAlso rdr("WLAN") IsNot DBNull.Value Then
                            If (iq.i_SKU.ContainsKey(rdr("WLAN"))) Then
                                Dim prodAlsoIncuded As clsProduct = iq.i_SKU(rdr("WLAN"))
                                If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                    prda = New clsProductAttribute(Product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation, AttribWriteCache, Not Inserting)
                                End If
                            Else
                                linkAttributes += rdr("WLAN") & ", "
                            End If

                        End If
                        If rdr("WWAN") IsNot Nothing AndAlso rdr("WWAN") IsNot DBNull.Value Then
                            If (iq.i_SKU.ContainsKey(rdr("WWAN"))) Then
                                Dim prodAlsoIncuded As clsProduct = iq.i_SKU(rdr("WWAN"))
                                If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                    prda = New clsProductAttribute(Product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation, AttribWriteCache, Not Inserting)
                                End If
                            Else
                                linkAttributes += rdr("WWAN") & ", "
                            End If

                        End If
                        If rdr("InstNIC") IsNot Nothing AndAlso rdr("InstNIC") IsNot DBNull.Value Then
                            If (iq.i_SKU.ContainsKey(rdr("InstNIC"))) Then
                                Dim prodAlsoIncuded As clsProduct = iq.i_SKU(rdr("InstNIC"))
                                If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                    prda = New clsProductAttribute(Product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation, AttribWriteCache, Not Inserting)
                                End If
                            Else
                                linkAttributes += rdr("InstNIC")
                            End If
                        End If
                        If linkAttributes.Length > 0 Then
                            linkAttributes = Left(linkAttributes, Len(linkAttributes) - 2)
                            prda = New clsProductAttribute(Product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "NKW", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)
                        End If

                        'QuickSpecs
                        '   ImportQuickSpecsInc(Product, Trim$(rdr.Item("sysfamilyname")), Inserting, AttribWriteCache)

                        'xText
                        Import.ExtText(Product, Inserting, AttribWriteCache, nextkey, Tlwc, xtdic, famlocs)
                        '  nextkey = 0
                        'End of attrs
                        If Trim$(rdr.Item("familycode")) = "" Then Stop
                        tl = iq.AddTranslation(Trim$(rdr.Item("familycode")), English, "FamMinor", 0, Tlwc, nextkey, False)
                        FamMinor = New clsProductAttribute(Product, iq.i_attribute_code("FamMinor"), 0, textUnit, tl, AttribWriteCache, Not Inserting)


                        'this was missing - added by nick 1.10.2015
                        tl = iq.AddTranslation(Trim$(rdr.Item("sysfamilyname")), English, "FamMajor", 0, Tlwc, nextkey, False)
                        FamMajor = New clsProductAttribute(Product, iq.i_attribute_code("FamMajor"), 0, textUnit, tl, AttribWriteCache, Not Inserting)



                        If Not rdr.Item("cpu") Is DBNull.Value Then
                            cpuSKU = New clsProductAttribute(Product, iq.i_attribute_code("cpuSKU"), 0, textUnit, iq.AddTranslation(Trim$(rdr.Item("cpu")), English, "CPUSKU", 0, Tlwc, nextkey, False), AttribWriteCache, Not Inserting)
                        End If

                        Dim fcode As String
                        Dim famBranch As clsBranch

                        fcode = Trim$(rdr.Item("sysfamilyname"))

                        'Find the family branch
                        'famBranch = FindBranchByName("tree.1", fcode)
                        Dim path As String = ""

                        If Not famlocs.ContainsKey(fcode) Then
                            Dim dud$ = modelsku & " " & fcode
                            Dim kkk = 0
                        Else
                            Dim fampath As String = famlocs(fcode)
                            famBranch = iq.Branches(Split(fampath, ".").Last)

                            Dim famname As String = famBranch.DisplayName(English)

                            If famBranch.Product.SKU <> "" Then
                                '    famBranch = famBranch.Parent
                                famBranch.Product.SKU = ""
                                famBranch.Product.update(errormessages)
                                If famBranch.Product.i_Attributes_Code.ContainsKey("mfrSKU") Then
                                    Dim pat As clsProductAttribute = famBranch.Product.i_Attributes_Code("mfrsku")(0)
                                    pat.delete(errormessages)
                                End If
                            End If


                            Dim picture$

                            picture = famBranch.Picture
                            If Not IsDBNull(rdr.Item("sysfamilyimg")) Then
                                picture = rdr.Item("sysfamilyimg")
                                'picture = Split(picture, "_")(1)
                            End If

                            If Inserting Then ' Need to do this for editing...
                                sysBranch = New clsBranch(Product, famBranch, iq.AddTranslation(Product.SKU, English, "SKU", 10, Tlwc, nextkey, False), picture, optTrans, optTransSingular, Nothing, famBranch.childBranches.Count * 10, False, "K") 'these ARE the systems (so we use the opt key - becuase they *contain* options)
                                'make the quantity records the make the system visible by region/country - these are the gobal/pathless ones

                                Dim rgns As String = ""
                                If Not IsDBNull(rdr.Item("activesites")) Then rgns = rdr.Item("activesites")
                                If rdr.Item("aaonly") <> 0 Then
                                    rgns &= ",AA"
                                End If

                                'there are a few 'junk' systems with no activesites
                                If rgns = "" Then
                                    Dim qty As clsQuantity  'EXCLUDE this system eveywhere (with a min increment of 0) - 
                                    qty = New clsQuantity(r_worldwide, "", sysBranch, 0, 0, 0, 0, QtyWritecache)
                                    'Public Sub New(region As clsRegion, ByVal Path As String, ByVal branch As clsBranch, ByVal numPreInstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean, Optional Writecache As DataTable = Nothing)
                                Else
                                    MakeSystemQuantities(sysBranch, rgns, clsRegion.containment(), QtyWritecache)
                                End If
                            Else
                                Dim outPath As String = ""
                                Dim branchIDs As List(Of Integer) = New List(Of Integer)
                                sysBranch = famBranch.findChildBySKU2(path, Product.SKU, outPath, False)
                                If sysBranch Is Nothing Then
                                    For Each branch In famBranch.childBranches.Values
                                        If branch.Product IsNot Nothing AndAlso branch.Product.SKU = "" Then
                                            branchIDs.Add(branch.ID)
                                        End If
                                    Next
                                    Dim errorMsg As List(Of String) = New List(Of String)
                                    For Each brID In branchIDs
                                        famBranch.childBranches.Remove(brID)
                                        iq.Branches(brID).deleted = True
                                        iq.Branches(brID).Update(errormessages)
                                        'famBranch.childBranches(brID).delete(errormessages)
                                    Next
                                End If
                                '    Dim Product = New clsProduct(rdr.Item("ModelSKU"), True, sector, iq.i_ProductType_Code(rdr.Item("systype")), rdr.Item("activefromdate"), activeTo, rdr.Item("active"), rdr.Item("eol"), publish, "", "", "") 'this IS a system 
                                '    famBranch.Product = Product
                                '    Dim errorMsg As List(Of String) = New List(Of String)
                                '    famBranch.Update(errorMsg)
                                'End If
                                'Compare the products
                                CompareProduct(iq.i_SKU(Product.SKU), Product, True, ActionList, True, AttribWriteCache)
                            End If
                        End If
                        End If
                End If
            End While
        End If
        rdr.Close()


        da.BulkWrite(con, QtyWritecache, "Quantity")
        QtyWritecache = Nothing

        'write the accumulated product attributes (bulk copy)
        da.BulkWrite(con, AttribWriteCache, "ProductAttribute")
        AttribWriteCache = Nothing

        da.BulkWrite(con, Tlwc, "Translation")
        Tlwc = Nothing

    End Sub

    Public Sub CullBadOptions()

        Dim famlocs As New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
        RootBranch.FindFamilyBranchesBelow("tree.1", 4, famlocs)

        Dim sql$

        Sql$ = "SELECT v.OptSN,po.optsku,v.sortorder,"
        Sql$ &= "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,sysfamily,slotaddtype,slotaddqty,"
        Sql$ &= "unitQty as capacity,ot.optTypeUnit as capacityUnit,"
        Sql$ &= "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription "
        Sql$ &= "FROM [iq].products.V2_OptionCats v "
        Sql$ &= "JOIN [iq].products.options po ON v.optsn=po.optsn "
        Sql$ &= "JOIN [iq].products.optTypes as OT on OT.optTypeCode=optType "
        Sql$ &= "JOIN [channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku "
        'Sql$ &= "WHERE po.optsku IN (" & optString & ")"
        Sql &= " order by sysfamily"

        Dim con As SqlClient.SqlConnection = New SqlConnection("data source=www3.channelcentral.net,8484;initial catalog=IQ;uid=editor;pwd=wainwright")
        con.Open()

        Dim rdr As SqlClient.SqlDataReader

        rdr = da.DBExecuteReader(con, sql)
        Dim dicValid As New Dictionary(Of String, HashSet(Of String))(StringComparer.CurrentCultureIgnoreCase)

        While rdr.read
            If rdr("sysfamily") IsNot DBNull.Value Then

                Dim sf As String = rdr.Item("sysfamily")
                If sf <> "" Then
                    For Each f In sf.Split(",")

                        If Not famlocs.ContainsKey(f) Then
                            Dim a = 0
                        Else
                            If Not dicValid.ContainsKey(f) Then dicValid.Add(f, New HashSet(Of String)(StringComparer.CurrentCultureIgnoreCase))
                            Dim optsku As String = rdr.Item("optsku")
                            If dicValid(f).Add(optsku) = False Then
                                Beep() 'option sku is listed twice in the same family
                            End If
                        End If
                    Next
                End If
            End If
        End While
        rdr.Close()
        con.Close()

        'now walk each all options branch and delete anything not the list per family

        Dim done As New HashSet(Of clsBranch)
        Dim todel As New HashSet(Of String)
        Dim naobs As Integer = 0
        Dim kept As Integer = 0
        Dim toclean As New HashSet(Of String) 'these (category) branches need their products removed

        For Each fam In famlocs.Keys 'we will walk down each 'all options' branch - making sure that every option we come accross appears in the 'validpaths'

            Dim fambranch As clsBranch = iq.Branches(Split(famlocs(fam), ".").Last)
            Dim aoBranch As clsBranch = fambranch.FindBranchByNameBelow("All Options", "tree.1", False, 6)

            If aoBranch IsNot Nothing Then
                'check each 'all options' branch


                If Not done.Contains(aoBranch) Then

                    'no child of alloptions (L1 BRANCH) should have a SKU !
                    For Each l1 In aoBranch.childBranches.Values
                        If l1.HasSKU Then todel.Add(l1.ID) 'these branches need deleting
                        l1.deleted = True

                        'If l1.Product IsNot Nothing Then toclean.Add(l1.ID) 'these need their products removed
                        'For Each l2 In 
                    Next

                    If dicValid.ContainsKey(fam) Then
                        aoBranch.compareAgainst(dicValid(fam), kept, todel)
                        done.Add(aoBranch)
                    End If
                End If
            Else
                naobs += 1
            End If
        Next

        If todel.Count Then
            Dim toskip As Integer = 0
            Dim chunk As Integer = 1000
            Do
                Dim ll = From j In todel.Skip(toskip).Take(chunk)
                If Not ll.Any Then Exit Do

                sql$ = "update branch set deleted =1 WHERE [id] IN(" & Join(ll.ToArray, ",") & ")"

                LongSQL(sql)
                toskip += 1000
            Loop

        End If

    End Sub

    Public Sub FixMissingMemory()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim ofDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamilyOptTypeToOptFamily()  'Gives a lookup of narrow optfamily from BroadOptType per sysfamily
        Dim dicOptLimits As Dictionary(Of String, IQ.clsLimit)

        'the actual installed cpu quantity comes form products.systems (overriding optionlimits !)
        Dim dicCpuQty As New Dictionary(Of String, Integer)(StringComparer.CurrentCultureIgnoreCase)
        Dim dicCpuSku As New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
        Dim sql$ = "select modelsku,cpuqty,cpu from h3.iq.products.systems"
        Dim r As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql)
        While r.Read
            If r("CpuQty") IsNot DBNull.Value Then
                dicCpuQty.Add(r("modelsku"), r("cpuqty"))
            End If
            If r("Cpu") IsNot DBNull.Value Then
                If r("CPU") <> "" Then
                    dicCpuSku.Add(r("MODELSKU"), r("Cpu"))
                End If
            End If
        End While
        r.Close()


        Dim delSlots As New List(Of String)

        'returns a dictinoary of the narrow,minor sysFamilyCode^optfamily>clslimit EG.. DL580PLFF^NHPLFF>blah
        dicOptLimits = Import.BuildOptLimits(con, ofDic)

        Dim pth$ = ""
        Dim sysLocs As Dictionary(Of String, clsBranch) = RootBranch.findSystemBranches("tree.1")


        Dim added As Integer = 0
        Dim swc As DataTable = da.MakeWriteCacheFor(con, "slot")

        For Each sysPath In sysLocs.Keys
            Dim sysBranch As clsBranch = sysLocs(sysPath)

            If sysBranch.Product.i_Attributes_Code.ContainsKey("famminor") Then
                Dim sysMinorFam = sysBranch.Product.i_Attributes_Code("famminor")(0).Translation.text(English)

                Dim systemSKU As String = sysBranch.Product.SKU

                '   If sysBranch.Product.SKU = "765822-031" Then Stop 'this is a 2 processor ML350g9 - fully pop'd

                Dim chassisBranch As clsBranch
                Dim chassispath As String
                For Each cb In sysBranch.childBranches
                    If cb.Value.Translation.text(English).Contains(" chassis") Then
                        chassisBranch = cb.Value
                        chassispath = sysPath & "." & chassisBranch.ID
                        Exit For
                    End If
                Next

                If chassisBranch Is Nothing Then Stop

                ' If sysMinorFam.ToUpper.Contains("BL460") Then Stop

                Dim memMinor As String = ""

                Dim cpuLimits As clsLimit = Nothing
                Dim memLimits As clsLimit = Nothing
                For Each k In dicOptLimits.Keys
                    If k.StartsWith(sysMinorFam & "^CPU") Then
                        cpuLimits = dicOptLimits(k)
                    ElseIf k.StartsWith(sysMinorFam & "^MEM") Then
                        memLimits = dicOptLimits(k)
                        memMinor = Split(k, "^")(2)
                    End If
                Next

                If cpuLimits IsNot Nothing And memLimits IsNot Nothing Then

                    If dicCpuQty.ContainsKey(systemSKU) Then
                        cpuLimits.Qinstalled = dicCpuQty(systemSKU)
                    End If

                    'make sure CPU slots reside on the chassis branch and are 'generic'
                    Dim errormessages As New List(Of String)
                    Dim hadone As Boolean = False
                    For Each s In chassisBranch.slots.Values
                        If s.Type.MajorCode = "CPU" Then
                            If s.Type.MinorCode = "GEN_CPU" And Not hadone Then
                                If s.numSlots <> cpuLimits.Qmax Then s.numSlots = cpuLimits.Qmax : s.update(errormessages)
                                hadone = True
                            Else
                                s.deleted = True
                                delSlots.Add(s.ID)
                            End If
                        End If
                    Next
                    If Not hadone Then
                        'there was no generic cpu slot on this chassisbranch .. so make one
                        Dim cpuslots As clsSlot = New clsSlot(iq.i_slotType_Code("CPU")("GEN_CPU"), chassisBranch, "", cpuLimits.Qmax, Nothing, New NullableInt, 0, 1, swc)
                    End If



                    'Some multiprocesor machines (typically workstations) have the same SKU for the upgrade and presintalled CPU - so we *must* put the memory sockets on the CPU branch (not the chassis)
                    '(if they are on the cassis, and the 'upgrade' CPU we end up with double the right initial number)

                    'the upshot is that for multicpu machines the memory slots must NOT be on the branch, but must be on both the preinstalled (sometimes L21) and (if it's different) the additional CPU sku




                    Dim st As clsSlotType
                    st = iq.SlotTypes.Values.Where(Function(sst) sst.MajorCode.ToUpper = "MEM" And sst.MinorCode.ToUpper = memMinor.ToUpper).FirstOrDefault
                    If st Is Nothing Then
                        st = New clsSlotType("MEM", memMinor)
                    End If



                    'we will delete all chassis branch memory slots - and recreate them either on the chassis or the cpu(s)
                    Dim chassisMemslot As clsSlot
                    Dim ok As Boolean = False
                    For Each s In chassisBranch.slots.Values
                        If s.Type.MajorCode.ToUpper = "MEM" Then
                            s.deleted = True
                            delSlots.Add(s.ID)
                        End If
                    Next


                    If cpuLimits.Qinstalled = cpuLimits.Qmax Then
                        'single cpu (Or fully popupulated)  machine - memory on the chassis branch (the cpu isnt an option)
                        Dim gslot As clsSlot = New clsSlot(st, chassisBranch, "", memLimits.Qmax, Nothing, New NullableInt, memLimits.Qmin, 0, swc)
                    Else
                        Dim gslot As clsSlot = New clsSlot(st, chassisBranch, "", memLimits.Qmax, Nothing, New NullableInt, memLimits.Qmin, 0, swc)


                        'Find the B21 under all options
                        'make a memory (enablement) slots *with a correct path*
                        If dicCpuSku.ContainsKey(systemSKU) Then
                            Dim cpuSku As String = dicCpuSku(systemSKU)
                            Dim optlocs As New Dictionary(Of clsProduct, String)
                            Dim foundCpuAsOption As Boolean = False
                            sysBranch.optionsBelow(sysPath, optlocs)
                            Dim skuToPath As New Dictionary(Of String, String)
                            For Each p In optlocs
                                skuToPath.Add(p.Key.SKU, p.Value)
                            Next

                            If skuToPath.ContainsKey(cpuSku) Then
                                Dim cpupath As String = skuToPath(cpuSku)
                                Dim fullpathname As String = PathName(cpupath)
                                'see if the cpu is already giving memory slots at this path 
                                Dim cpubranch As clsBranch = iq.Branches(Split(cpupath, ".").Last)

                                Dim addit As Boolean = True
                                For Each s In cpubranch.slots.Values
                                    If s.path = "" Then delSlots.Add(s.ID) 'no (upgradeable) cpu should give memory slots everywhere 
                                    If s.path = cpupath Then 'ok this is good - but we should check
                                        addit = False 'we don't need to add
                                        If s.numSlots <> memLimits.Qmax Then Stop
                                    End If
                                Next

                                If addit Then
                                    'make a new slot
                                    '    Dim st As clsSlotType
                                    '    st = iq.SlotTypes.Values.Where(Function(sst) sst.MajorCode.ToUpper = "MEM" And sst.MinorCode.ToUpper = memMinor.ToUpper).FirstOrDefault
                                    '    If st Is Nothing Then
                                    ' st = New clsSlotType("MEM", memMinor)
                                    '  End If

                                    Dim ssss = 1
                                    Dim cpumemslot As clsSlot = New clsSlot(st, cpubranch, cpupath, memLimits.Qmax, Nothing, New NullableInt, memLimits.Qmin, 0, swc)
                                End If

                                If cpubranch.Product.i_Attributes_Code.ContainsKey("altsku") Then
                                    Dim altsku As String = cpubranch.Product.i_Attributes_Code("altsku")(0).Translation.text(English)
                                    Dim fff = 0


                                End If



                                ' Beep()
                            Else
                                Beep() 'cpu is not an option..
                            End If
                        Else
                            'unknown CPU


                        End If
                    End If
                End If
            End If
        Next

        con.Close()

        con = da.OpenDatabase()
        Debug.Print(added)
        da.BulkWrite(con, swc, "slot")

        da.DBExecutesql("update slot set deleted = 1 where id in ('" & Join(delSlots.ToArray, "','") & "');")


        con.Close()



    End Sub

    Public Function checkoptions()



        Dim sysfamilies As New HashSet(Of String)(StringComparer.CurrentCultureIgnoreCase)

        Dim con As SqlClient.SqlConnection = da.OpenDatabase

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, "select sysfamilyname from h3.iq.products.sysfamilydefinitions")
        While rdr.Read
            sysfamilies.add(rdr(0))
        End While
        rdr.Close()

        rdr = da.DBExecuteReader(con, "select optsku,sysfamily from h3.iq.products.options")

        Dim badfamilies As New HashSet(Of String)

        Dim duds As Integer = 0
        Dim sw As StreamWriter = New StreamWriter("c:\temp\dudoptions.txt")
        While rdr.Read
            If rdr("sysfamily") IsNot DBNull.Value Then
                For Each sf In Split(rdr("sysfamily"), ",")
                    If sf.Trim <> "" Then
                        If Not sysfamilies.Contains(sf) Then
                            sw.WriteLine(rdr("optsku") & "-" & sf)
                            duds += 1
                            If sysfamilies.Contains(Trim(sf)) Then Stop
                            badfamilies.Add(sf)
                        End If
                    End If

                Next
            End If
        End While
        sw.Close()
        rdr.Close()
        con.Close()


        sw = New StreamWriter("c:\temp\badfamilies.txt")
        For Each f In badfamilies
            sw.WriteLine(f)
        Next
        sw.Close()





    End Function


    'Options only
    Public Function Buildtreeinc(con As SqlClient.SqlConnection, optskus As List(Of String), dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)), dicSlotTypes As Dictionary(Of String, clsSlotType), ActionList As clsActionList, AllowDelete As Boolean, systemSKUs As List(Of String))

        'adds any missing optSKUs to the broad options tree - note, they may need to be pruned in manyy locations if they
        'are explicity incompatible OR impliciltly incompatible (wrong opttype for the sysfamily)


        Dim JustDoit As Boolean = True 'added in to skip the user prompts as there are sooooo many changes at the moment, the ActionList idea can be reinstigated when itsd 1 or 2 changes a run
        Dim atCount = 0
        Dim totalCount = optskus.Count

        Dim optString = String.Join(",", optskus.Select(Function(l) "'" & l & "'"))

        Dim sysString = String.Join(",", systemSKUs.Select(Function(l) "'" & l & "'"))

        Dim ERRORMESSAGES As List(Of String) = New List(Of String)

        Dim kept As Integer = 0
        Dim pruned As Integer = 0

        'Dim dicSlotTypes As Dictionary(Of String, clsSlotType)
        'dicSlotTypes = Import.slotTypes(con, dicsystems) 'dicFamily) '20 secs

        Dim ofDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamilyOptTypeToOptFamily()  'Gives a lookup of narrow optfamily from BroadOptType per sysfamily
        Dim dicOptLimits As Dictionary(Of String, IQ.clsLimit)
        'returns a dictinoary of the narrow,minor sysFamilyCode^optfamily>clslimit EG.. DL580PLFF^NHPLFF>blah
        dicOptLimits = Import.BuildOptLimits(con, ofDic, systemSKUs)

        '        fixmissingGiveSLOTS()



        Dim chassisBranch As clsBranch
        '        Dim chassisVariant As clsVariant
        '        Dim chassisProduct As clsProduct
        Dim chassisTL As clsTranslation = iq.AddTranslation("Chassis", English, "", 0, Nothing, 0, False)

        '    'FACTORY INSTALLED OPTIONS/components - call them what you will
        '    'get a list (by system mfrSKU) of the part numbers and quantities of all factory installed components (PriStor, sec stor CPU, MEM etc,Raid)
        ImportLog.Add(DateTime.Now, String.Format("Checking FIOs"))
        Dim dicFIOs As Dictionary(Of String, Dictionary(Of String, Integer))
        dicFIOs = Import.FIOs(con, optString, sysString)

        Dim NEXTbId As Integer = 0
        'Dim NEXTgId As Integer = 0
        ' Dim nextpruneid As Integer = 0

        Dim tlOptions As clsTranslation = iq.AddTranslation("Options", English, "collect", 0, Nothing, 0, False)
        Dim tlOption As clsTranslation = iq.AddTranslation("Option", English, "collect", 0, Nothing, 0, False)


        Dim sql2$ = "SELECT * FROM h3.iQ.products.SysFamilyDefinitions"
        Dim FamilyOptionDefs As DataTable
        FamilyOptionDefs = SlowFilledDataTable(con, sql2$)


        Dim bwc As DataTable = da.MakeWriteCacheFor(con, "branch", NEXTbId, True) 'nextID is SET by this call !
        Dim Gwc As DataTable = da.MakeWriteCacheFor(con, "GRAFT")
        Dim qwc As DataTable = da.MakeWriteCacheFor(con, "quantity")
        Dim swc As DataTable = da.MakeWriteCacheFor(con, "slot")
        Dim pwc As DataTable = da.MakeWriteCacheFor(con, "prune")

        Dim nextkey As Integer = clsTranslation.NextKey
        Dim tlwc As DataTable = da.MakeWriteCacheFor(con, "Translation")

        Dim sql$


        If optString = "" Then
            ImportLog.Add(DateTime.Now, String.Format("No options are missing ..."))
        Else

            ImportLog.Add(DateTime.Now, String.Format("Querying IQ1 for tree details..."))

            sql$ = "SELECT v.OptSN,po.optsku,v.sortorder,"
            sql$ &= "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,sysfamily,slotaddtype,slotaddqty,"
            sql$ &= "unitQty as capacity,ot.optTypeUnit as capacityUnit,"
            sql$ &= "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription "
            sql$ &= "FROM [iq].products.V2_OptionCats v "
            sql$ &= "JOIN [iq].products.options po ON v.optsn=po.optsn "
            sql$ &= "JOIN [iq].products.optTypes as OT on OT.optTypeCode=optType "
            sql$ &= "JOIN [channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku "
            sql$ &= "WHERE po.optsku IN (" & optString & ")"
            sql &= " order by sysfamily"


            'CK branches contians an 'all options' branch for every family (and holds references to every sub-branch to 
            Dim ckBranches As Dictionary(Of String, clsBranch) 'Compound key of Sysfamily^l1^l2^l3>Branch
            ckBranches = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)
            '   Dim allOptions As clsBranch
            Dim famlist As List(Of String) = New List(Of String)
            Dim stDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamilyOptTypeToOptFamily

            Dim sql3 = "SELECT [SysFamilyName],optsku as [mfrPartNum],ISNULL([HPPS_Options].[powerMin],options.powermin) as PowerMin,isnull([HPPS_Options].[powerMax] ,options.powermax) as PowerMax "
            sql3 &= "FROM h3.iq.products.options left outer join h3.[iq].[Products].[HPPS_Options] on optsku=mfrPartNum "
            sql3 &= "WHERE (ISNULL([HPPS_Options].[powerMin],options.powermin) is not null or ISNULL([HPPS_Options].[powerMax],options.powermax) is not null) "
            sql3 &= "and mfrPartNum in (" & optString & ") order by sysfamilyname,OPTTYPE DESC"

            Dim iq1con = New SqlConnection("data source=www3.channelcentral.net,8484;initial catalog=IQ;uid=editor;pwd=wainwright")
            iq1con.Open()
            Dim powerDT = SlowFilledDataTable(iq1con, sql3) 'seems to close the connection?

            iq1con = New SqlConnection("data source=www3.channelcentral.net,8484;initial catalog=IQ;uid=editor;pwd=wainwright")
            iq1con.Open()

            Dim optionTable As DataTable
            optionTable = SlowFilledDataTable(iq1con, sql$)
            ' Dim rdr = da.DBExecuteReader(iq1con, sql$)
            Dim optionRdr = New DataTableReader(optionTable)
            Dim ock$
            Dim todel As List(Of clsSlot) = New List(Of clsSlot)()

            Dim mastercpusbranch As clsBranch
            For Each branch In iq.Branches.Values
                If branch.Translation.text(English).ToUpper = "CPU" Then
                    If branch.childBranches.Count > 30 Then
                        mastercpusbranch = branch
                        Exit For
                    End If

                End If
            Next

            If mastercpusbranch Is Nothing Then Stop

            Dim proctrans As clsTranslation = iq.AddTranslation("Processor", English, "OL2", 0, tlwc, nextkey, False)
            Dim procTransPlural As clsTranslation = iq.AddTranslation("Processors", English, "OL2", 0, Nothing, 0, False)



            Dim originalOptionCount As Integer = totalCount
            totalCount = optionTable.Rows.Count
            ImportLog.Add(DateTime.Now, String.Format("Totals Skus looked up {0} total returned ({1}", originalOptionCount, totalCount))

            'Dim bcache As New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase) 'cache paths (by various keys) for speed (branch is alaways the least segment)
            ' Dim validPaths As New HashSet(Of String) 'we're compiling a list of 'subpaths' under a number of 'all options' branches (one for each familty affected)
            Dim dudFamilies As HashSet(Of String) 'log those families that options are listed as compatible with that do not exist

            Dim famlocs As New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
            RootBranch.FindFamilyBranchesBelow("tree.1", 4, famlocs)

            Dim jjj As Integer = 0
            Dim kkk As Integer = 0
            Dim lll As Integer = 0

            Dim dudoptions As List(Of String) = New List(Of String)

            Dim tlFIOs As clsTranslation = iq.AddTranslation("FIOs", English, "FIO", 0, tlwc, nextkey, False)

            While optionRdr.Read

                atCount = atCount + 1
                Dim optfamily As String = optionRdr.Item("Optfamily") 'CAREPACK

                ImportLog.Add(DateTime.Now, String.Format("Checking placement for option SKU: {0} ({1}/{2})", optionRdr("optsku"), atCount, totalCount))

                Dim ot As String = optionRdr.Item("opttype")
                '  If ot = "CPU" Or ot = "MEM" Then

                If optionRdr.Item("sysfamily") IsNot DBNull.Value Then  'This is a CD list of the broad families an OPTION is compatible with eg. DL380

                    Dim sf() = Split(optionRdr.Item("sysfamily"), ",")

                    '                    If optionRdr.Item("optsku") = "N3R88AA" Then Stop


                    For Each f In sf

                        'If optionRdr("optsku") = "AN975A" And f.ToUpper = "DL380PG8" Then Stop
                        'If optionRdr("optSku") = "AE465A" And f.ToUpper = "ML310EG8V2" Then Stop

                        If String.IsNullOrEmpty(f) Then Continue For

                        f = f.Trim

                        ' If f.ToUpper.StartsWith("PROX261") Then Stop

                        Dim ck As String
                        Dim famPath As String = ""
                        Dim famBranch As clsBranch = Nothing

                        If famlocs.ContainsKey(f) Then
                            famPath = famlocs(f)
                            famBranch = iq.Branches(Split(famPath, ".").Last)
                        End If

                        If famBranch Is Nothing Then
                            dudoptions.Add(optionRdr("optsku") & " *" & f & "*")
                            Dim j = 0
                            jjj += 1
                            If dudFamilies Is Nothing Then dudFamilies = New HashSet(Of String)
                            dudFamilies.Add(f)

                            'options are listed in many obsolete families - so we get a stupid number of these warnings
                            '     ActionList.Add(rdr.Item("optsku"), ObjectType.WARNING, "Family " & f & " cannot be found, this import cannot create families at present")
                        Else

                            lll += 1
                            Dim outPath As String = ""
                            Dim order As Integer = optionRdr.Item("sortorder")
                            Dim AllOptions As clsBranch = famBranch.FindBranchByNameBelow("All Options", "tree.1", False, 6)

                            If AllOptions Is Nothing Then
                                'holder = New clsBranch(Nothing, Nothing, iq.AddTranslation("All Options", English, "", 0, tlwc, nextkey, False), "", iq.AddTranslation("All Options", English, "", 0, tlwc, nextkey, False), iq.AddTranslation("All Options", English, "", 0, tlwc, nextkey, False), Nothing, 0, False, "T", bwc, NEXTbId)
                                'famBranch.Graft(holder, "IncImport", "", ERRORMESSAGES, Gwc)
                                'Eek no systems in this family then...
                                If famBranch.childBranches.Count = 0 Then
                                    AuditLog.Instance.Add(AuditType.Warning, "No systems found for family, " & f, "IncImport", 0)
                                    Continue For
                                Else

                                    Dim aot As clsTranslation
                                    aot = iq.AddTranslation("All Options", English, "AO", 0, tlwc, nextkey, False)

                                    'Create the all options branch
                                    Dim b = New clsBranch(Nothing, Nothing, aot, "", aot, aot, Nothing, 0, False, "TGB", bwc, NEXTbId)
                                    'Graft it to the first system...
                                    For Each cb In famBranch.childBranches.Values
                                        If cb.Product IsNot Nothing AndAlso cb.Product.isSystem Then
                                            cb.Graft(b, "IncImport", "", ERRORMESSAGES, Gwc)
                                            AllOptions = b
                                            Exit For
                                        End If
                                    Next

                                    Dim FIOBranch = New clsBranch(Nothing, b, tlFIOs, "", tlOptions, tlOption, Nothing, order, True, "B", bwc, NEXTbId)
                                End If
                            Else
                                If AllOptions.Parent IsNot Nothing Then Stop 'The families all options branch is grafted on and should NEVER have a parent

                                If AllOptions.locked Then Continue For
                            End If

                            Dim slotchanged As Boolean
                            Dim l1branch As clsBranch = Nothing
                            Dim l2branch As clsBranch = Nothing
                            Dim l3branch As clsBranch = Nothing

                            If Not IsDBNull(optionRdr("l1")) Then l1branch = AllOptions.FindBranchByNameBelow(optionRdr("l1"), "", False, 6)
                            If l1branch Is Nothing Then
                                Dim tl1 As clsTranslation = iq.AddTranslation(optionRdr.Item("l1"), English, "OL1", 0, tlwc, nextkey, False)
                                l1branch = New clsBranch(Nothing, AllOptions, tl1, "", tlOptions, tlOption, Nothing, order, False, "YTGB", bwc, NEXTbId)
                            End If

                            If Not IsDBNull(optionRdr("l2")) Then l2branch = l1branch.FindBranchByNameBelow(optionRdr("l2"), "", False, 6)
                            If l2branch Is Nothing Then
                                Dim tl2 As clsTranslation = iq.AddTranslation(optionRdr.Item("l2"), English, "OL2", 0, tlwc, nextkey, False)
                                l2branch = New clsBranch(Nothing, l1branch, tl2, "", tlOptions, tlOption, Nothing, order, False, If(IsDBNull(optionRdr("l3")), "GTB", "B"), bwc, NEXTbId)
                            End If

                            Dim holderbranch As clsBranch = l2branch  'there isn't always an l3 branch ! - sometimes options are only 2 levels deep

                            If Not IsDBNull(optionRdr("l3")) Then
                                l3branch = l2branch.FindBranchByNameBelow(optionRdr("l3"), "", False, 6)
                                If l3branch Is Nothing Then
                                    Dim txt$ = optionRdr.Item("l3")
                                    Dim tl3 As clsTranslation = iq.AddTranslation(txt$, English, "OL3", 0, tlwc, nextkey, False)
                                    l3branch = New clsBranch(Nothing, l2branch, tl3, "", tlOptions, tlOption, Nothing, order, False, "G", bwc, NEXTbId)
                                End If
                                holderbranch = l3branch
                            End If

                            Dim resultPath As String = ""
                            Dim optionbranch As clsBranch = New clsBranch()
                            Dim existingOption As clsBranch = holderbranch.findChildBySKU2("", optionRdr("optSKU"), resultPath)

                            'Compile a list of the valid options - anything under the all otpions and not in this list needs deleting
                            'Dim pth$ = AllOptions.ID & "." & l1branch.ID & "." & l2branch.ID & "."
                            'If l3branch IsNot Nothing Then pth$ &= l3branch.ID & "."
                            'If existingOption IsNot Nothing Then
                            '    pth$ &= existingOption.ID
                            'ElseIf optionbranch IsNot Nothing Then
                            '    pth$ &= optionbranch.ID
                            'Else
                            '    Stop
                            'End If

                            'If validPaths.Contains(pth$) Then
                            '    kkk += 1
                            'Else
                            '    validPaths.Add(pth$)
                            'End If


                            If existingOption IsNot Nothing And optionRdr("l2") <> "Processor" Then
                                'ADD NOTHING TO DO
                                'ImportLog.Add(DateTime.Now, String.Format("Option exist: {0} )", rdr("optsku")))
                                Dim a = 0

                                optionbranch = existingOption

                                'need to check here that the opttype is compatible with the sub families FamilyMem, FamilyPriStore etc

                                ' If existingOption.Product.i_Attributes_Code.ContainsKey("opttype") Then
                                'optionbranch = New clsBranch(anOption, holderbranch, SKUTL, "", tlOption, tlOptions, Nothing, 0, False, "B", bwc, NEXTbId)
                                '     For Each slot In 

                                'Dim s = New clsSlot(iq.i_slotType_Code(rdr("opttype"))(rdr("optfamily")), optionbranch, "", -CInt(rdr("slots")), Nothing, New NullableInt(), 0, 0, swc)

                            Else
                                'ADD TO ADD
                                'ImportLog.Add(DateTime.Now, String.Format("Option Not found : {0} )", rdr("optsku")))
                                Dim opttype As String = optionRdr.Item("Opttype")

                                If opttype.ToUpper.Trim = "CPU" Then

                                    Dim cpusku As String = optionRdr("optsku")
                                    ImportLog.Add(DateTime.Now, String.Format("CPU Found SKU: {0}", cpusku))

                                    'CPU's are handled very differently - see import.cpus
                                    '(only the CPU preinstalled in the system is an option for it - and CPUs enable banks of memory etc)

                                    'Find CPU's branch (in the master list)
                                    Dim cpusbranch As clsBranch = mastercpusbranch
                                    Dim cpubranch As clsBranch = cpusbranch.findChildBySKU2("tree." & cpusbranch.ID.ToString(), cpusku, outPath)

                                    If cpubranch Is Nothing Then

                                        Dim cpuProd As clsProduct = iq.i_SKU(cpusku)
                                        Dim skuTrans As clsTranslation = iq.AddTranslation(cpusku, English, "SKU", 10, tlwc, nextkey, False)
                                        Dim processor As clsTranslation = iq.AddTranslation("Processor", English, "OL2", 0, tlwc, nextkey, False)
                                        Dim processors As clsTranslation = iq.AddTranslation("Processors", English, "OL2", 0, tlwc, nextkey, False)
                                        cpubranch = New clsBranch(cpuProd, cpusbranch, skuTrans, "", processors, processor, iq.Screens(719), 0, False, "B", bwc, NEXTbId)
                                    End If

                                    Dim rws = powerDT.Select("mfrpartnum = '" & optionRdr("optsku") & "'")
                                    ' If rws.Count > 1 Then Stop

                                    Dim cpusPowerSlotFound As Boolean = False
                                    Dim count As Integer = 0
                                    For Each cpuslot In cpubranch.slots.Values
                                        If cpuslot.Type.MajorCode = iq.i_slotType_Code("PWR")("W").MajorCode Then
                                            cpusPowerSlotFound = True
                                            count += 1
                                        End If
                                    Next

                                    If cpusPowerSlotFound Then
                                        If count > 1 Then 'duplicate slots
                                            ImportLog.Add(DateTime.Now, String.Format("Duplicate power slots found", optionRdr("optsku")))
                                        End If
                                    Else
                                        For Each rw In rws
                                            'Handles power consumptiuon (of the CPU)
                                            Dim s = New clsSlot(iq.i_slotType_Code("PWR")("W"), cpubranch, "", -CInt(rw("powerMax")), Nothing, New NullableInt(), 0, 0, swc)
                                        Next
                                    End If

                                    'Graft to relevant systems (find ALL the systems having this cpu) - we will cehck each system in in the importing systems list  in the loop
                                    Dim rdr2 = da.DBExecuteReader(con, "SELECT modelsku,familycode,cpu,cpuqty,[QtyInstalled],[QtyMax],qtymin,[Incr_Min],[Incr_Pref]  FROM h3.iq.products.systems inner join h3.iq.products.optionlimits sfd on sfd.sysfamily = systems.familycode where cpu='" & optionRdr.Item("optsku") & "' AND opttype='CPU'")
                                    While rdr2.Read
                                        'Find system
                                        Dim aSystemSku = rdr2("modelsku")
                                        If systemSKUs.Contains(aSystemSku) Then  'Is this a system we're importing !
                                            If iq.i_SKU.ContainsKey(aSystemSku) Then

                                                Dim sysProduct = iq.i_SKU(aSystemSku)
                                                If sysProduct.Branches.Count = 1 Then
                                                    For Each systemBranch In sysProduct.Branches ' this system will only appear once 

                                                        For Each Path In systemBranch.AllPaths 'It will only appear in one place
                                                            'Find our Processors
                                                            Dim cpuPath = ""
                                                            Dim systemPath As String = ""
                                                            systemPath = Path
                                                            Dim g As clsBranch = systemBranch.FindBranchByNameBelow("Processor", systemPath, False, 10, cpuPath)

                                                            Dim pn$ = PathName(cpuPath)

                                                            If g Is Nothing Then
                                                                'Doesn't exist
                                                                Dim AllOptionsSystem = systemBranch.FindBranchByNameBelow("System", Path, False, 10, cpuPath)
                                                                Dim AlloptionsSystemProcessor = New clsBranch(Nothing, AllOptionsSystem, proctrans, "", proctrans, procTransPlural, Nothing, 0, False, "G", bwc, NEXTbId)
                                                                cpuPath = cpuPath & "." & AlloptionsSystemProcessor.ID
                                                                g = AlloptionsSystemProcessor
                                                            End If
                                                            Dim chassisPath As String = ""
                                                            Dim chassisB As clsBranch = Nothing

                                                            For Each cb In systemBranch.childBranches
                                                                If cb.Value.Translation.text(English).Contains(" chassis") Then
                                                                    chassisB = cb.Value
                                                                    chassisPath = Path & "." & chassisB.ID
                                                                    Exit For
                                                                End If
                                                            Next

                                                            'Graft it on to chassis - Total red heerring ??
                                                            Dim chassispathFound As String = cpuPath 'REMOVE 
                                                            chassispathFound = chassisPath
                                                            'If chassisB IsNot Nothing AndAlso chassisB.findChildBySKU2(chassisPath, rdr("optsku"), chassispathFound) Is Nothing Then
                                                            'chassisB.Graft(cpubranch, "CPUIncImport", cpuPath, ERRORMESSAGES, Gwc)
                                                            If Not g.childBranches.ContainsKey(cpubranch.ID) Then
                                                                g.Graft(cpubranch, "buildtreeinc", cpuPath, ERRORMESSAGES, Gwc)
                                                            End If
                                                            'End If

                                                            If cpuPath.Contains(cpubranch.ID) Then Stop

                                                            cpuPath = cpuPath & "." & cpubranch.ID

                                                            If cpubranch.slots.Values.Where(Function(sl) sl.Type.MajorCode = "CPU" AndAlso (sl.path = "" Or sl.path = chassispathFound)).Count = 0 Then
                                                                Dim sl = New clsSlot(iq.i_slotType_Code("CPU")("GEN_CPU"), cpubranch, "", -1, Nothing, New NullableInt(), rdr2("qtymin"), If(IsDBNull(rdr2("incr_pref")), Nothing, rdr2("incr_pref")), swc)
                                                                ' Dim sl = New clsSlot(iq.i_slotType_Code(rdr("opttype"))(rdr("optfamily")), cpubranch, "", -1, Nothing, New NullableInt(), rdr2("qtymin"), If(IsDBNull(rdr2("incr_pref")), Nothing, rdr2("incr_pref")), swc)
                                                            Else
                                                                Dim genCpufound As Boolean = False
                                                                For Each slot In cpubranch.slots.Values
                                                                    Dim optfam As String = optionRdr.Item("optfamily")
                                                                    Dim oty As String = optionRdr.Item("opttype")
                                                                    If slot.Type.MajorCode = oty And slot.Type.MinorCode = optfam Then
                                                                        slot.deleted = True
                                                                        slot.update(ERRORMESSAGES)

                                                                    ElseIf slot.Type.MajorCode = "CPU" And slot.Type.MinorCode = "GEN_CPU" Then
                                                                        genCpufound = True
                                                                    End If
                                                                    If slot.Type.MajorCode = "CPU" And genCpufound = False Then
                                                                        Dim sl = New clsSlot(iq.i_slotType_Code("CPU")("GEN_CPU"), cpubranch, "", -1, Nothing, New NullableInt(), rdr2("qtymin"), If(IsDBNull(rdr2("incr_pref")), Nothing, rdr2("incr_pref")), swc)
                                                                    End If
                                                                Next

                                                                Dim cpuSlot As clsSlot = cpubranch.slots.Values.Where((Function(sl) sl.Type.MajorCode = "CPU" AndAlso (sl.path = "" Or sl.path = chassispathFound))).FirstOrDefault
                                                                If cpuSlot.numSlots > 0 Then
                                                                    cpuSlot.numSlots = -1
                                                                    cpuSlot.update(ERRORMESSAGES)
                                                                End If
                                                            End If
                                                            'Slot to the chassis?

                                                            'Create limits, FIOs etc
                                                            If Not IsDBNull(rdr2("cpuqty")) Then
                                                                If cpubranch.Quantities.Values.Where(Function(q) q.NumPreInstalled = rdr2("cpuqty") AndAlso q.FOC = True And q.Path = cpuPath).Count = 0 Then
                                                                    Dim qty = New clsQuantity(iq.i_region_code("XW"), cpuPath, cpubranch, rdr2("cpuqty"), rdr2("qtymin"), If(IsDBNull(rdr2("incr_pref")), 0, rdr2("incr_pref")), True, qwc)
                                                                End If
                                                            End If

                                                            'If Not IsDBNull(rdr2("qtymax")) AndAlso rdr2("qtymax") > 1 Then

                                                            '    'Dim chassisB = branch.Value.FindBranchByNameBelow("chassis", chassisPath, False, 10)
                                                            '    If chassisB IsNot Nothing Then

                                                            '        'The CPU (which exists in one 'global' place) - will give lots of different (minor) types of memory slot 
                                                            '        ' at lots of different paths
                                                            '        Dim memorymax As Integer = 0
                                                            '        Dim foundmem As Boolean = False
                                                            '        For Each slot In chassisB.slots.Values.ToArray
                                                            '            If slot.Type.MajorCode = "MEM" Then 'locate the memory slots in the chassis.. 
                                                            '                Dim newpath As String = cpuPath
                                                            '                'slot.Update(cpuBranch, newpath)

                                                            '                Dim sysMinorFamily As String = "" 'comes from the iq.systems.familycode
                                                            '                If sysProduct.i_Attributes_Code.ContainsKey("FamMinor") Then sysMinorFamily = sysProduct.i_Attributes_Code("FamMinor")(0).Translation.text(English) 'IMPORTANT for compatibility
                                                            '                If sysMinorFamily = "" Then Continue For
                                                            '                For Each k In dicOptLimits.Keys
                                                            '                    Dim bits() As String = Split(k, "^")
                                                            '                    If LCase(bits(0)) = LCase(sysMinorFamily) Then
                                                            '                        'for every narrow OptFamily in the sysfamily
                                                            '                        Dim Limit As clsLimit = dicOptLimits(k)
                                                            '                        If UCase(bits(1)) = "FAMILYMEM" Then Stop
                                                            '                        optfamily = bits(2)
                                                            '                        Dim opttypeMem As String = bits(1)

                                                            '                        If opttypeMem = "MEM" Then
                                                            '                            'code to update memory 
                                                            '                            memorymax = Limit.Qmax
                                                            '                        End If
                                                            '                    End If
                                                            '                Next

                                                            '                ImportLog.Add(DateTime.Now, String.Format("Moving memory from chassis {0} to option SKU: {1}", chassisB.Translation.text(English), optionRdr("optsku")))

                                                            '                Dim cpath As String = PathName(cpuPath)
                                                            '                Dim alreadyThere = False
                                                            '                For Each s In cpubranch.slots.Values
                                                            '                    If s.Type.MajorCode = "MEM" Then
                                                            '                        If (String.IsNullOrEmpty(s.path) OrElse s.path = cpuPath) Then
                                                            '                            s.numSlots = memorymax
                                                            '                            s.update(ERRORMESSAGES)
                                                            '                            alreadyThere = True
                                                            '                        End If
                                                            '                    End If
                                                            '                Next

                                                            '                If Not alreadyThere Then
                                                            '                    Dim cpuMemEnable As clsSlot = New clsSlot(slot.Type, cpubranch, cpuPath, memorymax, slot.notes, slot.slotNum, slot.requiredFill, slot.advisedFill, swc)
                                                            '                End If

                                                            '                slot.numSlots = memorymax
                                                            '                slot.deleted = True
                                                            '                slot.update(ERRORMESSAGES)
                                                            '                foundmem = True

                                                            '            ElseIf slot.Type.MajorCode = "CPU" Then
                                                            '                If slot.Type.MinorCode <> "GEN_CPU" Then

                                                            '                    Dim st As clsSlotType = iq.i_slotType_Code("CPU")("GEN_CPU")
                                                            '                    Dim x = From z In chassisB.slots.Values Where z.Type.MajorCode = st.MajorCode And z.Type.MinorCode = st.MinorCode
                                                            '                    If x.Count = 0 Then
                                                            '                        slot.Type = st
                                                            '                    Else
                                                            '                        slot.deleted = True
                                                            '                    End If
                                                            '                    slot.update(ERRORMESSAGES)
                                                            '                End If
                                                            '            End If

                                                            '        Next
                                                            '        If Not foundmem Then
                                                            '            'memory in cpu branch
                                                            '            For Each s In cpubranch.slots.Values
                                                            '                slotchanged = False
                                                            '                If s.Type.MajorCode = "MEM" Then
                                                            '                    If s.path = cpuPath Then
                                                            '                        Dim sysMinorFamily As String = "" 'comes from the iq.systems.familycode
                                                            '                        If sysProduct.i_Attributes_Code.ContainsKey("FamMinor") Then sysMinorFamily = sysProduct.i_Attributes_Code("FamMinor")(0).Translation.text(English) 'IMPORTANT for compatibility
                                                            '                        If sysMinorFamily = "" Then Continue For

                                                            '                        For Each k In dicOptLimits.Keys
                                                            '                            Dim bits() As String = Split(k, "^")
                                                            '                            If LCase(bits(0)) = LCase(sysMinorFamily) Then
                                                            '                                'for every narrow OptFamily in the sysfamily
                                                            '                                Dim Limit As clsLimit = dicOptLimits(k)
                                                            '                                If UCase(bits(1)) = "FAMILYMEM" Then Stop
                                                            '                                optfamily = bits(2)
                                                            '                                Dim opttypeMem As String = bits(1)

                                                            '                                If opttypeMem = "MEM" Then
                                                            '                                    'code to update memory 
                                                            '                                    memorymax = Limit.Qmax
                                                            '                                End If
                                                            '                            End If
                                                            '                        Next
                                                            '                        s.numSlots = memorymax
                                                            '                        slotchanged = True
                                                            '                    ElseIf String.IsNullOrEmpty(s.path) Then
                                                            '                        s.deleted = True
                                                            '                        slotchanged = True
                                                            '                    Else
                                                            '                        Dim slotPaths() = s.path.Split(".")
                                                            '                        If slotPaths(slotPaths.Length - 1) <> cpubranch.ID Then
                                                            '                            s.deleted = True
                                                            '                            slotchanged = True
                                                            '                        End If
                                                            '                    End If
                                                            '                    If slotchanged Then
                                                            '                        s.update(ERRORMESSAGES)
                                                            '                    End If
                                                            '                End If
                                                            '            Next
                                                            '        End If
                                                            '    End If
                                                            'End If
                                                        Next
                                                    Next
                                                Else
                                                    ImportLog.Add(DateTime.Now, String.Format("Sys Product: {0}  has multiple branches", sysProduct.SKU))
                                                End If
                                            End If
                                        End If
                                    End While
                                    rdr2.Close()


                                    optionbranch = cpubranch

                                ElseIf opttype.ToUpper.Trim <> "WTY" Then
                                    'Dont add warrenties
                                    'If ActionList.IsGo(rdr("optSKU"), ActionType.INSERT, ObjectType.Branch, holder, rdr("optSKU")) Then

                                    Dim optsku As String = optionRdr.Item("optSKU")
                                    Dim anOption As clsProduct = iq.i_SKU(optsku)

                                    Dim SKUTL As clsTranslation = iq.AddTranslation(anOption.SKU, English, "SKU", 10, tlwc, nextkey, False)


                                    optionbranch = New clsBranch(anOption, holderbranch, SKUTL, "", tlOption, tlOptions, Nothing, 0, False, "B", bwc, NEXTbId)
                                    'The branch.translation is the part number (Points to the same TL)

                                    'SLOT!
                                    If Not IsDBNull(optionRdr("slots")) AndAlso CInt(optionRdr("slots")) > 0 Then
                                        If Not iq.i_slotType_Code.ContainsKey(optionRdr("opttype")) OrElse Not iq.i_slotType_Code(optionRdr("opttype")).ContainsKey(optionRdr("optfamily")) Then
                                            Dim c = New clsSlotType(optionRdr("opttype"), optionRdr("optfamily"), iq.AddTranslation("", English, "slottype", 0, tlwc, nextkey, False))
                                        End If
                                        Dim s = New clsSlot(iq.i_slotType_Code(optionRdr("opttype"))(optionRdr("optfamily")), optionbranch, "", -CInt(optionRdr("slots")), Nothing, New NullableInt(), 0, 0, swc)
                                    End If

                                    '  Else
                                    '   ActionList.Add(rdr("optSKU"), ActionType.INSERT, ObjectType.Branch, holder, rdr("optSKU"))
                                    '  End If
                                End If
                            End If

                            If optionbranch IsNot Nothing AndAlso optionbranch.ID > 0 Then
                                'PowerSizing...
                                Dim rws = powerDT.Select("mfrpartnum = '" & optionRdr("optsku") & "'")
                                Dim optsPowerSlotFound As Boolean = False
                                For Each optslot In optionbranch.slots.Values
                                    If optslot.Type.MajorCode = iq.i_slotType_Code("PWR")("W").MajorCode Then
                                        optsPowerSlotFound = True
                                    End If
                                Next

                                If Not optsPowerSlotFound Then
                                    For Each rw In rws
                                        Dim s = New clsSlot(iq.i_slotType_Code("PWR")("W"), optionbranch, "", If({"psu", "psum"}.Contains(optionbranch.Product.ProductType.Code), CInt(rw("powerMax")), -CInt(rw("powerMax"))), Nothing, New NullableInt(), 0, 0, swc)
                                    Next
                                End If

                                'SlotAdds
                                Dim types() As String = optionRdr.Item("slotaddType").ToString().Split(";".ToArray(), StringSplitOptions.RemoveEmptyEntries)
                                Dim qtys() As String = optionRdr.Item("slotaddqty").ToString().Split(";".ToArray(), StringSplitOptions.RemoveEmptyEntries)
                                Dim aslot As clsSlot
                                Dim st As clsSlotType

                                ' stdic contains a dictionary of option type > optionfamily - per systemfamilyCODE (minorFamilty)
                                'eg              dl580pg825NHPlff > HDD > NHP35LFFSC


                                For Each Path In optionbranch.AllPaths
                                    For i = 0 To UBound(types)
                                        If Not iq.i_slotType_Code.ContainsKey(types(i)) Then
                                            'THIS causes massive duplication (new clsslottype!)) - investigate !
                                            ' If Not iq.i_slotType_Code(types(i)).ContainsKey(types(i)) Then
                                            st = New clsSlotType(types(i), types(i), iq.AddTranslation(types(i), English, "st", 0, tlwc, nextkey, False))
                                            ' End If
                                        End If

                                        Dim systemBranchAbove As clsBranch = FindSystemBranch(Path)
                                        Dim productAbove As clsProduct = Nothing
                                        Dim famAbove As String = ""
                                        If systemBranchAbove Is Nothing Then
                                            Beep()
                                        Else

                                            If Not systemBranchAbove.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
                                                If systemBranchAbove.Parent.Product Is Nothing Then
                                                    famAbove = ""
                                                else


                                                    Dim fa As clsProductAttribute = systemBranchAbove.Parent.Product.i_Attributes_Code("FamMajor").First
                                                    famAbove = fa.Translation.text(English)
                                                    'create the missing family major attribute
                                                    Dim missingFa As New clsProductAttribute(systemBranchAbove.Product, fa.Attribute, fa.NumericValue, fa.Unit, fa.Translation)
                                                End If

                                            Else
                                                famAbove = systemBranchAbove.Product.i_Attributes_Code("FamMajor").First.Translation.text(English)
                                            End If

                                            '                                        End If

                                            If dicSlotTypes.ContainsKey(famAbove) Then
                                                If stDic(famAbove).ContainsKey(types(i)) AndAlso iq.i_slotType_Code(types(i)).ContainsKey(stDic(famAbove)(types(i))) Then
                                                    st = iq.i_slotType_Code(famAbove)(types(i))
                                                    'Dim alreadythere = branch.slots.Where(Function(sl) sl.Value.Type Is st AndAlso Math.Sign(sl.Value.numSlots) <> Math.Sign(CInt(qtys(i))))
                                                    'For Each s In alreadythere.ToList()
                                                    '    s.Value.delete()
                                                    'Next
                                                    If optionbranch.slots IsNot Nothing AndAlso optionbranch.slots.Where(Function(sl) sl.Value.Type Is st AndAlso Math.Sign(sl.Value.numSlots) = Math.Sign(CInt(qtys(i)))).Count = 0 Then
                                                        aslot = New clsSlot(st, optionbranch, "", qtys(i), Nothing, New NullableInt(), 0, 0, swc)
                                                    End If
                                                Else
                                                    If iq.i_slotType_Code.ContainsKey(types(i)) Then
                                                        st = iq.i_slotType_Code(types(i)).First.Value
                                                        If optionbranch.slots IsNot Nothing AndAlso optionbranch.slots.Where(Function(sl) sl.Value.Type Is st).Count = 0 Then aslot = New clsSlot(st, optionbranch, "", qtys(i), Nothing, New NullableInt(), 0, 0, swc)
                                                    End If
                                                End If
                                            End If
                                        End If
                                    Next
                                Next
                            End If


                            'only 'do' each family ONCE
                            If Not famlist.Contains(famBranch.Translation.text(English)) Then

                                'Graft the all options branch onto every system, and make chassis slots

                                famlist.Add(famBranch.Translation.text(English))
                                '  Debug.WriteLine(famBranch.Translation.text(English))
                                For Each systembranch In famBranch.childBranches.Values.ToList
                                    If systembranch.Product IsNot Nothing AndAlso systembranch.Product.SKU = "" Then Stop

                                    If systembranch.Product Is Nothing Then Stop

                                    'Do we need to add the All Options branch to this system or is it already there?
                                    Dim AOfound = False
                                    Dim CBfound As clsBranch = Nothing

                                    Dim dupSlotBranch As List(Of clsBranch) = (From dup In systembranch.childBranches.Values Where dup.Product IsNot Nothing AndAlso dup.Product.ProductType.Code = "CHAS").ToList()
                                    If dupSlotBranch.Count > 1 Then
                                        For Each br In dupSlotBranch
                                            If Not br.Translation.text(English).Contains(" chassis") Then
                                                br.deleted = True
                                                br.Update(ERRORMESSAGES)
                                            End If
                                        Next
                                    End If

                                    For Each child In systembranch.childBranches.Values
                                        ' Debug.WriteLine(child.Translation.text(English))
                                        If child.Translation.text(English) = "All Options" Then
                                            'we are not importing the family so this is ok...
                                            AOfound = True
                                        End If
                                        If child.Translation.text(English).Contains(" chassis") Then
                                            CBfound = child
                                        End If
                                    Next

                                    Dim systemsku$ = systembranch.Product.SKU

                                    If Not AOfound Then
                                        'Need to find family all options branch..., do we need to check if this is a system we are already adding???
                                        Dim faob = famBranch.FindBranchByNameBelow("All Options", famPath, False, 8)
                                        ' If ActionList.IsGo(rdr("optSKU"), ActionType.INSERT, ObjectType.Graft, systembranch, faob) Then
                                        systembranch.Graft(faob, "buildtreeInc", "", ERRORMESSAGES, Gwc)
                                        'Else
                                        '      ActionList.Add(rdr("optSKU"), ActionType.INSERT, ObjectType.Graft, systembranch, faob)
                                        '    End If
                                        'What if others are missing?  TRO's etc?
                                    End If

                                    Dim sysMinorFamily As String = "" 'comes from the iq.systems.familycode
                                    If iq.i_SKU(systemsku).i_Attributes_Code.ContainsKey("FamMinor") Then sysMinorFamily = iq.i_SKU(systemsku).i_Attributes_Code("FamMinor")(0).Translation.text(English) 'IMPORTANT for compatibility
                                    If sysMinorFamily = "" Then Continue For

                                    'Do we have a chassis branch already?
                                    If CBfound IsNot Nothing Then
                                        chassisBranch = CBfound
                                    Else
                                        If JustDoit OrElse ActionList.IsGo(optionRdr("optSKU"), ActionType.INSERT, ObjectType.Branch, systembranch, "Chassis Branch") Then
                                            ' chassisProduct = New clsProduct("", False, True, iq.i_sector_code("HPPSG"), iq.i_ProductType_Code("CHAS"), DateAdd(DateInterval.Day, -500, Now), DateAdd(DateInterval.Day, 10000, Now), True, False, True, "", "", "")
                                            ' chassisVariant = New clsVariant("", chassisProduct, HP, chassisProduct.ID.ToString, "", "", "", r_worldwide, 0) 'Every product needs a variant - so it can be stored in a QuoteItem
                                            'chassisBranch = New clsBranch(chassisProduct, Nothing, iq.AddTranslation(f & " chassis", English, "UI", 0, tlwc, nextkey, False), "", chassisTL, chassisTL, Nothing, 100, True, "B", bwc, NEXTbId)
                                            chassisBranch = New clsBranch(Nothing, Nothing, iq.AddTranslation(f & " chassis", English, "UI", 0, tlwc, nextkey, False), "", chassisTL, chassisTL, Nothing, 100, True, "B", bwc, NEXTbId)
                                        End If
                                    End If

                                    'chassis branch needs to be per MinorFamily !!
                                    'Gives Slots

                                    Dim gslot As clsSlot
                                    For Each k In dicOptLimits.Keys
                                        Dim bits() As String = Split(k, "^")
                                        If LCase(bits(0)) = LCase(sysMinorFamily) Then
                                            'for every narrow OptFamily in the sysfamily
                                            Dim Limit As clsLimit = dicOptLimits(k)
                                            If UCase(bits(1)) = "FAMILYMEM" Then
                                                'Beep()
                                                Dim zzz = 0
                                            End If


                                            Dim opttype As String = bits(1) 'slot major
                                            optfamily = bits(2) 'slot minor

                                            Dim st As clsSlotType = Nothing
                                            'make the slot TYPE on the fly if necessary
                                            If iq.SlotTypes.Values.Where(Function(sst) sst.MajorCode.ToUpper = opttype.ToUpper And sst.MinorCode.ToUpper = optfamily.ToUpper).Count > 0 Then
                                                st = iq.SlotTypes.Values.Where(Function(sst) sst.MajorCode.ToUpper = opttype.ToUpper And sst.MinorCode.ToUpper = optfamily.ToUpper).First
                                            Else
                                                Dim slotname As clsTranslation = iq.AddTranslation(opttype & " " & optfamily & " slot(s)", English, "slots", 0, tlwc, nextkey, False)
                                                st = New clsSlotType(opttype.ToUpper, optfamily.ToUpper, slotname)
                                            End If
                                            'the gives stos do NOT need a path (systempath & "." & chassisBranch.ID) - becuase they are active weherever this subchassis appears

                                            If Not chassisBranch.hasSlot(st) Then
                                                gslot = New clsSlot(st, chassisBranch, "", Limit.Qmax, Nothing, New NullableInt, Limit.Qmin, 0, swc)
                                                '
                                                'MakeGivesSlots(chassisBranch, dicOptLimits(sysSubFamily), dicSlotTypes, dicSubFamOptTypeSlotType, QuantityWriteCache, SlotWriteCache)
                                            End If
                                        End If
                                    Next

                                    'Add PCIs here!!!!
                                    '' removed for speed ath the moment      AddPCIChassisSlots(sysMinorFamily, f, systembranch, tlwc, nextkey, swc)

                                    systembranch.Graft(chassisBranch, "", "", ERRORMESSAGES, Gwc)
                                    ActionList.Add(optionRdr("optSKU"), ActionType.INSERT, ObjectType.Branch, systembranch, "Chassis Branch")


                                Next
                            End If
                        End If
                    Next
                Else
                    'What do we do if the option has no sysfamily?  This seems to be a valid setting for some FIO's
                End If
                '  End If


            End While

            optionRdr.Close()

            For Each slot In todel.ToList()
                slot.delete(ERRORMESSAGES)
            Next

            ImportLog.Add(DateTime.Now, String.Format("Writing DB Changes"))

            da.BulkWrite(con, qwc, "quantity")
            da.BulkWrite(con, bwc, "Branch", , True)
            con.Close()
            con = da.OpenDatabase()
            da.BulkWrite(con, Gwc, "Graft")
            da.BulkWrite(con, tlwc, "translation")
            da.BulkWrite(con, swc, "slot")
            da.BulkWrite(con, pwc, "prune")



            bwc = da.MakeWriteCacheFor(con, "branch", NEXTbId, True) 'nextID is SET by this call !
            Gwc = da.MakeWriteCacheFor(con, "GRAFT")
            qwc = da.MakeWriteCacheFor(con, "quantity")
            swc = da.MakeWriteCacheFor(con, "slot")
            pwc = da.MakeWriteCacheFor(con, "prune")

            nextkey = clsTranslation.NextKey


            Debug.Print(jjj)
            Debug.Print(kkk)
            Debug.Print(lll)
            tlwc = da.MakeWriteCacheFor(con, "Translation")

            Dim sw As StreamWriter  '= New StreamWriter("c:\temp\validpaths.txt")
            sw = New StreamWriter("c:\temp\dudfamilies.txt")
            If dudFamilies IsNot Nothing Then
                For Each l In dudFamilies
                    sw.WriteLine(l)
                Next
            End If
            sw.Close()


            sw = New StreamWriter("c:\temp\dudoptions.txt")
            For Each l In dudoptions
                sw.WriteLine(l)
            Next
            sw.Close()



            End If


            ImportLog.Add(DateTime.Now, String.Format("Checking FIOS"))

            Dim cc As Integer
            For Each sku In dicFIOs.Keys
                cc += 1
                If iq.i_SKU.ContainsKey(sku) Then
                    For Each optionsku In dicFIOs(sku)
                        If optskus.Contains(optionsku.Key) Then

                            ImportLog.Add(DateTime.Now, String.Format("Checking FIOS for system {0}, option {1} {2}/{3}", sku, optionsku.Key, cc, dicFIOs.Count))

                            For Each br In iq.i_SKU(sku).Branches
                                For Each p In br.AllPaths

                                    Dim resPath = ""

                                    Dim segs() As String = Split(p, ".")
                                    Dim j As Integer = segs.Count
                                    ReDim Preserve segs(20)
                                    Dim l As Integer = br.FastfindChildBySKU2(optionsku.Key, segs, j)  'locate this FIO (under each (although there is only 1) occurance of the systemsku)
                                    Dim optp As clsBranch = Nothing


                                    If l Then
                                        Dim sb As StringBuilder = New StringBuilder(100)
                                        For i = 0 To l - 1
                                            sb.Append(segs(i).ToString)
                                            If i <> l - 1 Then sb.Append(".")
                                        Next
                                        optp = iq.Branches(segs(l - 1))
                                        resPath = sb.ToString
                                    End If

                                    ' ''     Dim check As String = optp.Product.SKU

                                    ' ''Scan here to make sure we dont have any dups, particalully under the FIO's
                                    ''If optp IsNot Nothing Then
                                    ''    Dim holderBranch = iq.Branches(Split(resPath, ".")(Split(resPath, ".").Count - 2))
                                    ''    Dim btodel = New List(Of clsBranch)
                                    ''    For Each child In holderBranch.childBranches.Values
                                    ''        If child.Product IsNot Nothing AndAlso child.Product.SKU = optionsku.Key AndAlso child.ID <> optp.ID Then
                                    ''            'prune it out of caution!
                                    ''            'Remove qty records
                                    ''            Dim todel2 = New List(Of clsQuantity)
                                    ''            For Each q In child.Quantities.Values
                                    ''                todel2.Add(q)
                                    ''            Next
                                    ''            For Each too In todel2
                                    ''                too.Delete(ERRORMESSAGES)
                                    ''            Next
                                    ''            Dim ptodel As List(Of clsPrune) = New List(Of clsPrune)()
                                    ''            For Each pr In child.Prunes.Values
                                    ''                ptodel.Add(pr)
                                    ''            Next
                                    ''            For Each ptd In ptodel
                                    ''                ptd.delete()
                                    ''            Next
                                    ''            Dim stodel As List(Of clsSlot) = New List(Of clsSlot)()
                                    ''            For Each pr In child.slots.Values
                                    ''                stodel.Add(pr)
                                    ''            Next
                                    ''            For Each std In stodel
                                    ''                std.delete(ERRORMESSAGES)
                                    ''            Next
                                    ''            Dim sqlc = "UPDATE QuoteItem SET FK_Branch_ID = " & optp.ID & " WHERE FK_Branch_ID = " & child.ID
                                    ''            da.DBExecutesql(con, sqlc)

                                    ''            btodel.Add(child)
                                    ''            'Dim pr = New clsPrune(resPath, New NullableInt(), "FIOPrune", Nothing)
                                    ''        End If
                                    ''    Next
                                    ''    For Each btd In btodel
                                    ''        '      btd.delete(ERRORMESSAGES)
                                    ''    Next
                                    ''End If

                                    For Each cb In br.childBranches.Values
                                        If cb.Translation.text(English).Contains(" chassis") Then
                                            chassisBranch = cb
                                            Exit For
                                        End If
                                    Next

                                    'If this option isnt there then create it under FIO's
                                    Dim fioPath As String = ""
                                    If optp Is Nothing AndAlso iq.i_SKU.ContainsKey(optionsku.Key) Then
                                        Dim fioBranch = br.FindBranchByNameBelow("FIOs", fioPath, False, 0)
                                        If fioBranch Is Nothing Then fioBranch = New clsBranch(Nothing, br.FindBranchByNameBelow("All Options", fioPath, False, 0), iq.AddTranslation("FIOs", English, "", 0, tlwc, nextkey, False), "", iq.AddTranslation("FIOs", English, "", 0, Nothing, 0, False), iq.AddTranslation("FIOs", English, "", 0, Nothing, 0, False), Nothing, 0, True, "B", bwc, NEXTbId) ' What if AO branch doesnt exit???
                                        ' If ActionList.IsGo(optsku, ActionType.INSERT, ObjectType.Branch, fioBranch, optsku) Or Inserting Then
                                        Dim branch As clsBranch = New clsBranch(iq.i_SKU(optionsku.Key), fioBranch, iq.AddTranslation(optionsku.Key, English, "", 0, tlwc, nextkey, False), _
                                                                   "", tlOption, tlOptions, Nothing, 0, False, "B", bwc, NEXTbId)
                                        optp = br.findChildBySKU2(p, optionsku.Key, resPath)
                                        'Else
                                        '    ActionList.Add(optsku, ActionType.INSERT, ObjectType.Branch, fioBranch, optsku)
                                        '   End If
                                    End If
                                    If Not String.IsNullOrEmpty(resPath) Then
                                        Dim aa = PathName(resPath)
                                        'NB: Makelimits prunes off incompatible options !
                                        MakeLimits(p, optionsku.Key, Right(resPath, Len(resPath) - Len(p)) _
                                             , Gwc, swc, Nothing, 0, qwc, False, _
                                             dicOptLimits, dicSlotTypes, dicOptLocalisation, _
                                             dicFIOs, sku, kept, pruned, chassisBranch, br, FamilyOptionDefs, Nothing)
                                    Else
                                        'This will only happen on a dummy run...
                                        isFIO(optionsku.Key, sku, p, dicFIOs, dicOptLocalisation, New clsLimit(0, 1, 100, 1, 1), qwc, Nothing)
                                    End If
                                Next

                                ' Next
                            Next
                        End If
                    Next
                End If
            Next


            ImportLog.Add(DateTime.Now, String.Format("Writing DB Changes"))

            da.BulkWrite(con, qwc, "quantity")
            da.BulkWrite(con, bwc, "Branch", , True)
            da.BulkWrite(con, Gwc, "Graft")
            da.BulkWrite(con, tlwc, "translation")
            da.BulkWrite(con, swc, "slot")
            da.BulkWrite(con, pwc, "prune")

            con.Close()




            'For Each l In validPaths
            '    sw.WriteLine(PathName(l))
            'Next
            'sw.Close()


            Dim todelete As Integer = 0
            Dim tokeep As Integer = 0
            Dim done As New HashSet(Of clsBranch)

            '        LongSQL("update branch set deleted=1 where id in (" & Join(dellist.ToArray, ",") & ")")

    End Function

    Public Function TestFastFind()



        Dim sku$ = "652749-z21"

        Dim p As String = "tree.1.5"
        Dim resPath = ""

        Dim t = Stopwatch.GetTimestamp

        Dim segs() As String = Split(p)
        Dim j As Integer = segs.Count
        ReDim Preserve segs(20)
        Dim l As Integer = iq.Branches(5).FastfindChildBySKU2(sku$, segs, j)
        If l > 0 Then
            Dim optp As clsBranch = iq.Branches(segs(l))
            resPath = Join(segs.Take(l).ToArray, ".")
        End If

        Dim t1 As Double = (Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000

        p = "tree.1.5"
        t = Stopwatch.GetTimestamp
        Dim b As clsBranch = iq.Branches(5).findChildBySKU2("tree.1.5", sku$, p)
        Dim t2 As Double = (Stopwatch.GetTimestamp - t) / Stopwatch.Frequency * 1000

    End Function


    Public Function optionsIncremental(con As SqlClient.SqlConnection, _
                                       addSkus As List(Of String), dicUnits As Dictionary(Of String, clsUnit), _
                                       lDic As Dictionary(Of String, clsBranch), _
                                       dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)), _
                                       ActionList As clsActionList, AllowDelete As Boolean) As List(Of String)

        'lDic is used to construct the L1/L2/L3 global options (accessories and services) catalogue

        'NB: in Iq1 a 'family' is a 'narrow/specific' designation - and type is broad (major type)
        'This *always* confuses me

        optionsIncremental = New List(Of String)

        Dim dicSC As New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)
        dicSC = New Dictionary(Of String, clsTranslation)

        dicSC.Add("A", iq.AddTranslation("A", English, "SCC", 10, Nothing, 0, False))
        dicSC.Add("TV", iq.AddTranslation("TV", English, "SCC", 20, Nothing, 0, False))
        dicSC.Add("SB", iq.AddTranslation("SB", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("R", iq.AddTranslation("R", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("PR", iq.AddTranslation("PR", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("GO", iq.AddTranslation("GO", English, "SCC", 30, Nothing, 0, False))

        If Not iq.i_attribute_code.ContainsKey("Slots") Then
            Dim sa As clsAttribute = New clsAttribute("Slots", iq.AddTranslation("Slots used (legacy/import)", English, "", 0, Nothing, 0, False), 0)
        End If

        ' LoadAbbreviations(con)
        Dim sql$

        sql$ = "SELECT v.OptSN,optsc,po.optsku,v.sortorder,fio,"
        sql$ &= "case when po.sysfamily = ''  then isnull((select  sysfamilyname+', ' as 'data()' from h3.iq.products.systems inner join  h3.[iQ].[products].[SysFamilyDefinitions] on [SysFamilyDefinitions].sysfamily=systems.familycode  where PSU = po.optsku and opttype='PSU'  group by sysfamilyname FOR XML PATH('')),'') else po.sysfamily end as sysfamily,"
        sql$ &= "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,"
        sql$ &= "unitQty as capacity,ot.optTypeUnit as capacityUnit,localisation,h.manuf7,"
        sql$ &= "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,po.opttype2,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription,isnull(h.pl,'none') as pl "
        sql$ &= "FROM h3.iq.products.V2_OptionCats v "
        sql$ &= "JOIN h3.iq.products.options po ON v.optsn=po.optsn "
        sql$ &= "JOIN h3.[iq].products.optTypes as OT on OT.optTypeCode=optType "
        sql$ &= "JOIN h3.[channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku "
        sql$ &= "WHERE po.optsku IN ('" & Join(addSkus.ToArray, "','") & "')"

        Dim nextBid As Integer = 0
        Dim nextProdID As Integer = 0
        Dim nextsId As Integer = 0
        Dim tlOptions As clsTranslation = iq.AddTranslation("Options", English, "cats", 0, Nothing, 0, False)
        Dim tlOption As clsTranslation = iq.AddTranslation("Option", English, "cats", 0, Nothing, 0, False)

        'Write caches (for MUCH faster bulk writes)
        Dim pawc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")
        Dim bwc As DataTable = da.MakeWriteCacheFor(con, "branch", nextBid, True) 'nextID is SET by this call !

        Dim twc As DataTable = da.MakeWriteCacheFor(con, "Translation")
        Dim pwc As DataTable = da.MakeWriteCacheFor(con, "Product", nextProdID, True)
        Dim swc As DataTable = da.MakeWriteCacheFor(con, "Slot", nextsId, True)

        Dim nextKey As Integer = clsTranslation.NextKey()

        Dim tlacs As clsTranslation = iq.AddTranslation("Accessories and Services", English, "cat", 0, twc, nextKey, False)

        Dim allOptions As clsBranch = iq.RootBranch.FindBranchByNameBelow("Accessories and Services", "tree.1", False, 3)
        'New clsBranch(Nothing, iq.RootBranch, tlacs, "/images/iq/accSvcs.gif", tlOptions, tlOption, Nothing, 0, False, "B", bwc, nextBid)

        ImportLog.Add(DateTime.Now, String.Format("Querying IQ1 for option details"))

        Dim rdr As SqlClient.SqlDataReader
        Try
            rdr = da.DBExecuteReader(con, sql$)

            Dim l1Branch As clsBranch
            Dim l2Branch As clsBranch
            Dim l3Branch As clsBranch
            Dim l4Branch As clsBranch

            Dim addTo As clsBranch

            Dim options As Integer = 0
            While rdr.Read

                ImportLog.Add(DateTime.Now, String.Format("Checking Option SKU: {0}", rdr("optsku")))

                optionsIncremental.Add(rdr("OptSku"))

                Dim Inserting As Boolean = True
                If iq.i_SKU.ContainsKey(rdr("OptSku")) Then
                    Inserting = False
                End If

                If Inserting Then

                    Dim ck As String = rdr.Item("l1").trim

                    If Not lDic.ContainsKey(ck) Then
                        l1Branch = New clsBranch(Nothing, allOptions, iq.AddTranslation(rdr.Item("l1"), English, "OL1", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "B", bwc, nextBid)
                        lDic.Add(ck, l1Branch)
                    Else
                        l1Branch = lDic(ck)
                    End If

                    addTo = Nothing

                    ck = rdr.Item("l1").trim & "^" & rdr.Item("l2").trim
                    If Not lDic.ContainsKey(ck) Then
                        l2Branch = New clsBranch(Nothing, l1Branch, iq.AddTranslation(rdr.Item("l2"), English, "OL2", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "BGT", bwc, nextBid)
                        lDic.Add(ck, l2Branch)
                    Else
                        l2Branch = lDic(ck)
                    End If
                    addTo = l2Branch

                    If rdr.Item("l3") IsNot DBNull.Value Then
                        ck = rdr.Item("l1").trim & "^" & rdr.Item("l2").trim & "^" & rdr.Item("l3").trim
                        If Not lDic.ContainsKey(ck) Then
                            l3Branch = New clsBranch(Nothing, l2Branch, iq.AddTranslation(rdr.Item("l3"), English, "OL3", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "BGT", bwc, nextBid)
                            lDic.Add(ck, addTo)
                        Else
                            l3Branch = lDic(ck)
                        End If
                        addTo = l3Branch
                    End If

                    'optfamily is not globally unique... 5.25lff drives appear in optical and HDD
                    Dim optfam = rdr.Item("optFamily") ' is 'L4'

                    Dim l3t As String = ""
                    If rdr.Item("l3") IsNot DBNull.Value Then l3t = rdr.Item("l3").trim.tolower
                    ck = rdr.Item("l1").trim & "^" & rdr.Item("l2").trim & "^" & l3t & "^" & optfam.trim
                    If Not lDic.ContainsKey(ck) Then
                        Dim txt$ = ""
                        If dicAbbreviations.ContainsKey(optfam) Then txt = dicAbbreviations(optfam) Else txt = Replace(txt$, "_", " ")
                        If txt Is Nothing Then txt = ""
                        l4Branch = New clsBranch(Nothing, addTo, iq.AddTranslation(txt, English, "OL4", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "G", bwc, nextBid)
                        lDic.Add(ck, l4Branch)
                    Else
                        l4Branch = lDic(ck)
                    End If
                End If

                Dim otc As String = rdr.Item("opttype")  'these are broad
                Dim otc2 As String = If(IsDBNull(rdr.Item("opttype2")), "", rdr.Item("opttype2")) 'ML horrid but don't understand opttype2 and its causing data categorization issues for cables, even giving them a W value
                If otc2 = "CAB" Then otc = "CAB"

                Dim OptionProduct As clsProduct = Nothing
                Dim optionbranch As clsBranch

                If iq.i_ProductType_Code.ContainsKey(otc) Then
                    Dim pt As clsProductType = iq.i_ProductType_Code(otc)
                    Dim af As Date = CDate("01/01/1980")
                    Dim at As Date = CDate("01/01/2400")

                    If Not IsDBNull(rdr.Item("activeFromDate")) Then af = rdr.Item("activeFromDate")
                    If Not IsDBNull(rdr.Item("activeToDate")) Then at = rdr.Item("activeToDate")

                    If Not iq.i_SKU.ContainsKey(rdr.Item("optsku")) Then
                        OptionProduct = New clsProduct(rdr.Item("optsku"), False, True, iq.Sectors.Values(0), pt, af, at, rdr.Item("active"), rdr.Item("eol"), Not rdr.Item("AAonly"), "", "", "", pwc, nextProdID)
                    Else
                        OptionProduct = iq.i_SKU(rdr.Item("optsku"))
                    End If


                    Dim TLdesc As clsTranslation = Nothing
                    If Not IsDBNull(rdr.Item("ccDescription")) Then

                        Dim dsc$ = rdr.Item("ccdescription")
                        If rdr.Item("ccDescription").tolower.contains("amd cpu") Then Stop

                        TLdesc = iq.AddTranslation(rdr.Item("ccdescription"), English, "OPTDSC", 0, twc, nextKey, False)

                        If rdr("opttype") = "CPU" Then
                            Dim cpuroot As clsBranch = Nothing
                            For Each b In iq.Branches.Values
                                If b.Translation.text(English) = "CPU" AndAlso b.childBranches.Count > 30 Then
                                    cpuroot = b
                                    Exit For
                                End If
                            Next
                            optionbranch = New clsBranch(OptionProduct, cpuroot, iq.AddTranslation(rdr.Item("optsku"), English, "OPTDSC", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "B", bwc, nextBid)
                        Else
                            optionbranch = New clsBranch(OptionProduct, l4Branch, iq.AddTranslation(rdr.Item("optsku"), English, "OPTDSC", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "B", bwc, nextBid)
                        End If

                        addOptionAttributesInc(OptionProduct, pawc, twc, nextKey, rdr, New Dictionary(Of String, String) From {{rdr("optsku"), rdr("pl")}}, dicUnits, TLdesc, Inserting)
                    Else
                        Logit("Missing description")
                    End If
                Else
                    Logit("Missing opttype:" & otc)
                End If
                'Supply Chain Focus Attribute
                If Not IsDBNull(rdr.Item("optSC")) Then
                    Dim optsc As String = Trim(rdr.Item("optsc"))
                    If optsc <> "" And optsc <> "Z" Then
                        Dim SCfa As clsProductAttribute = New clsProductAttribute(OptionProduct, iq.i_attribute_code("focus"), 0, iq.i_unit_code("txt"), dicSC(optsc), pawc)
                    End If
                End If

                If Not Inserting Then
                    CompareProduct(iq.i_SKU(OptionProduct.SKU), OptionProduct, True, ActionList, True, pawc)
                End If

                'systypefocus attribute

                options += 1

                'Populate the Dictionary of option localisations (Countries in which it's active) - which is used later in BuildTree
                'we DO NOT add options which are not localised to the dicoptlocalisation dictionary (they are unrestricted)

                If Inserting Then 'need to work out what to do while editing...
                    Dim rgns As String = ""
                    If Not IsDBNull(rdr.Item("localisation")) Then rgns = rdr.Item("localisation")

                    If rdr.Item("aaonly") <> 0 Then
                        rgns &= ",AA"
                    End If

                    If rgns <> "" Then
                        If OptionProduct IsNot Nothing Then

                            Dim regions As List(Of clsRegion) = New List(Of clsRegion)
                            Dim cs As List(Of String) = Split(rgns, ",").ToList

                            If Not cs.Contains("XW") Then   'Anything paul has localised 'worldwide' needs no restriction

                                cleanRegions(cs, New Dictionary(Of String, List(Of String))())
                                For Each c In cs

                                    If c = "UCSA" Then c = "USCA" 'fix a typo
                                    If iq.i_region_code.ContainsKey(c) Then
                                        regions.Add(iq.i_region_code(c))
                                    Else
                                        Logit("invalid region " & c & " (in products.options.localisation)")
                                        '    Stop
                                    End If
                                Next
                                dicOptLocalisation.Add(OptionProduct, regions)
                            End If
                        End If
                    End If
                End If
            End While


        Catch ex As Exception
            optionsIncremental = Nothing
        Finally

            rdr.Close()

            ImportLog.Add(DateTime.Now, String.Format("Writing Option Changes"))

            da.BulkWrite(con, twc, "translation", , True)
            da.BulkWrite(con, pwc, "product", , True)
            da.BulkWrite(con, bwc, "branch", , True)
            da.BulkWrite(con, pawc, "productattribute")
            da.BulkWrite(con, swc, "slot")

            con.Close()
        End Try

    End Function


    Public Function addOptionAttributesInc(optionProduct As clsProduct, pawc As DataTable, twc As DataTable, ByRef nextKey As Integer, rdr As SqlClient.SqlDataReader, dicplcode As Dictionary(Of String, String), dicunits As Dictionary(Of String, clsUnit), tldesc As clsTranslation, Inserting As Boolean)

        Dim incompatible As clsProductAttribute
        Dim altsku As clsProductAttribute
        Dim anAttribute As clsProductAttribute
        Dim mfrsku As clsProductAttribute
        Dim plcode As clsProductAttribute


        ' Dim ssde As clsTranslation = iq.AddTranslation("Solid State Drive", English, "DriveType")
        Dim textUnit As clsUnit = iq.i_unit_code("txt")
        If textUnit Is Nothing Then Stop


        Dim desc As clsProductAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("desc"), 0, textUnit, tldesc, pawc, Not Inserting)

        'record the options OptFamily - this is the MinorOption type - but isn't globally unique..
        'eg. HPL35inchLFF may appear under oth OPT and HDD opt types
        anAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("optFamily"), 0, textUnit, iq.AddTranslation(rdr.Item("optfamily"), English, "", 0, twc, nextKey, False), pawc, Not Inserting)

        'This IS used in the quote summary (amongst other places)

        If Len(rdr.Item("opttype")) > 5 Then Stop
        anAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("optType"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("optType"), English, "", 0, twc, nextKey, False), pawc, Not Inserting)

        'If Not iq.Attributes.ContainsKey("MfrSKU") Then j = New clsAttribute("MfrSKU", New clsText(iq.addTranslation("MfrSKU")))

        Dim speed As clsProductAttribute
        Dim capacity As clsProductAttribute
        If Not IsDBNull(rdr.Item("speed")) Then
            If Not IsDBNull(rdr.Item("speedunit")) Then  'Some things (tape drives/Graphics cards/batteries have 'speeds' without units - we're not imprtiong - mentioned to dan 02/08/2012
                speed = New clsProductAttribute(optionProduct, iq.i_attribute_code("speed"), rdr.Item("speed"), dicunits(rdr.Item("speedUnit")), Nothing, pawc, Not Inserting)
            End If
        Else
            If rdr.Item("Opttype") = "HDD" Then
                'HDD's without a speed are SSD's - give them a numerically high RPM (so they sort to the 'top' speed wise - but display the text SSD (instead of 100,000 rpm)
                Dim ssd As clsTranslation = iq.AddTranslation("SSD", English, "DriveType", 0, twc, nextKey, False)
                speed = New clsProductAttribute(optionProduct, iq.i_attribute_code("speed"), 100000, iq.i_unit_code("txt"), ssd, pawc, Not Inserting)
            End If
        End If

        If Not IsDBNull(rdr.Item("capacity")) Then

            Dim uk$
            If Not IsDBNull(rdr.Item("capacityUnit")) Then ''Some things (cables,newtork cards have  capacities without units - we're not importing - mentioned to dan 02/08/2012 - am now... with a TXT unit
                uk$ = rdr.Item("capacityUnit")
            Else
                uk$ = "txt"
            End If

            capacity = New clsProductAttribute(optionProduct, iq.i_attribute_code("capacity"), rdr.Item("capacity"), iq.i_unit_code(uk$), Nothing, pawc, Not Inserting)

        End If


        If Not IsDBNull(rdr.Item("opttype2")) Then
            Dim ot2 = New clsProductAttribute(optionProduct, iq.i_attribute_code("opttype2"), 0, textUnit, iq.AddTranslation(rdr.Item("opttype2"), English, "", 0, twc, nextKey, False), pawc, Not Inserting)
        End If

        Dim optsku As String = rdr.Item("optsku")

        If Not IsDBNull(rdr.Item("technology")) Then
            Dim t$ = rdr.Item("technology")
            Dim cp As Integer
            cp = InStr(t$, "CORE")
            Dim numcores As Integer
            If cp Then
                numcores = Val(Left$(t$, cp - 1))
                '  If numcores = 3 Or numcores = 5 Or numcores = 7 Or numcores > 16 Then Stop 'odd number of cores
                Dim cores As clsProductAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("cores"), numcores, iq.i_unit_code("num"), Nothing, pawc, Not Inserting)

                Dim numthreads As Integer
                cp = InStr(t$, "TH")
                If cp Then
                    numthreads = Val(Mid$(t$, cp - 2, 2))
                    Dim threads As clsProductAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("threads"), numthreads, iq.i_unit_code("num"), Nothing, pawc, Not Inserting)
                End If
            End If
        End If

        'mfrsku = New clsProductAttribute(optionProduct, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(Trim$(rdr.Item("OptSKU")), English, "", 0, twc, nextKey, False), pawc, Not Inserting)
        Dim pl$
        'iq.i_SKU.Add(Trim$(rdr.Item("OptSKU")), optionProduct)

        If Not dicplcode.ContainsKey(rdr.Item("optSKU")) Then
            Logit("No PL code for option '" & rdr.Item("Optsku") & "' (not in HeirarchyIQ).")
        Else
            pl = dicplcode(rdr.Item("optSKU"))
            plcode = New clsProductAttribute(optionProduct, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(pl, English, "", 0, twc, nextKey, False), pawc, Not Inserting)
        End If

        'Dim opttype As clsProductAttribute
        'Dim opt$
        'opt$ = rdr.Item("OptType")
        'opttype = New clsProductAttribute(optionproduct, iq.Attributes("OptType"), 0, iq.Units("txt"), iq.addTranslation(opt, twc).Key, awc)
        'End If

        If Not IsDBNull(rdr.Item("incompatible")) Then
            If Trim$(rdr.Item("incompatible")) <> "" Then
                Dim ic$ = Replace(rdr.Item("incompatible"), " ", "")
                incompatible = New clsProductAttribute(optionProduct, iq.i_attribute_code("incompat"), 0, textUnit, _
                iq.AddTranslation(ic$, English, "incompat", 0, twc, nextKey, False), pawc, Not Inserting)
            End If
        End If

        If Not IsDBNull(rdr.Item("altsku")) Then
            If Trim$(rdr.Item("altsku")) <> "" Then
                altsku = New clsProductAttribute(optionProduct, iq.i_attribute_code("altSKU"), 0, textUnit, _
                iq.AddTranslation(Replace(rdr.Item("altSKU"), " ", ""), English, "atSKU", 0, twc, nextKey, False), pawc, Not Inserting)
            End If
        End If

        'required later when making 'takes' slots - to respect iq.products.options.slots
        Dim slots As clsProductAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("Slots"), rdr.Item("slots"), iq.i_unit_code("num"), Nothing, pawc, Not Inserting)
        'Dont do this for PSU enablement kits, they dont take a PSU slot....
        If Not IsDBNull(rdr.Item("technology")) AndAlso rdr.Item("technology") = "UPGRADE" Then
            slots.NumericValue = 0
        End If

        If Not IsDBNull(rdr.Item("technology")) Then
            Dim tech = New clsProductAttribute(optionProduct, iq.i_attribute_code("technology"), 0, textUnit, _
                iq.AddTranslation(Replace(rdr.Item("technology"), " ", ""), English, "", 0, twc, nextKey, False), pawc, Not Inserting)
        End If

        If Not IsDBNull(rdr.Item("fio")) AndAlso rdr.Item("fio") <> 0 Then
            Dim tech = New clsProductAttribute(optionProduct, iq.i_attribute_code("focus"), 1, textUnit, _
                iq.AddTranslation("FIO", English, "Foci", 0, twc, nextKey, False), pawc, Not Inserting)
        End If


        Dim ofa As clsAttribute
        If Not iq.i_attribute_code.ContainsKey("optFam") Then
            ofa = New clsAttribute("optFam", iq.AddTranslation("Options family", English, "", 0, twc, nextKey, False), 0)
        Else
            ofa = iq.i_attribute_code("optFam")
        End If

        Dim ofm$ = rdr.Item("OptFamily")
        Dim optfam As clsProductAttribute = New clsProductAttribute(optionProduct, ofa, 0, textUnit, iq.AddTranslation(ofm, English, "", 0, twc, nextKey, False), pawc, Not Inserting)

        If Not IsDBNull(rdr("manuf7")) Then
            Dim refAttr = New clsProductAttribute(optionProduct, iq.i_attribute_code("ProdRef"), 0, textUnit, iq.AddTranslation(rdr("manuf7"), English, "", 0, twc, nextKey, False), pawc, Not Inserting)
        End If

    End Function


    ''' <summary>
    ''' Imports families incrementally
    ''' </summary>
    ''' <returns>Returns a dictionary of Dans SysFamilyName to The family Branch I create for it</returns>
    ''' <remarks></remarks>
    Public Function FamiliesInc(con As SqlClient.SqlConnection, SKUs As String) As Dictionary(Of String, String)


        Dim rdr As SqlClient.SqlDataReader

        If Not iq.i_attribute_code.ContainsKey("bays") Then Dim ba As clsAttribute = New clsAttribute("bays", iq.AddTranslation("Drive bays", English, "attribs", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("HPL") Then Dim hpa As clsAttribute = New clsAttribute("HPL", iq.AddTranslation("Hot Pluggable", English, "attribs", 0, Nothing, 0, False), 0)


        'The family branches can only carry the Major' fore
        Dim fMaj As clsAttribute = iq.i_attribute_code("FamMajor")
        Dim fMin As clsAttribute = iq.i_attribute_code("FamMinor")
        Dim fDisp As clsAttribute = iq.i_attribute_code("FamDisp")

        'the Unabbreviated family name is the BRANCH.Translation


        Dim lff As clsTranslation = iq.AddTranslation("LFF", English, "bays", 0, Nothing, 0, 0)
        Dim lffL As clsTranslation = iq.AddTranslation("Large form factor (3.5 inch) drive bays ", English, "bays", 0, Nothing, 0, False)  ' Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

        Dim sff As clsTranslation = iq.AddTranslation("SFF", English, "bays", 0, Nothing, 0, False)
        Dim sffL As clsTranslation = iq.AddTranslation("Small Form Factor (2.5 inch) drive bays ", English, "bays", 0, Nothing, 0, False)  ' Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

        Dim bff As clsTranslation = iq.AddTranslation("Both", English, "bays", 0, Nothing, 0, False)
        Dim bffL As clsTranslation = iq.AddTranslation("Has both Small Form Factor (2.5 inch) and Large Form Factor (3.5 inch) drive bays ", English, "bays", 0, Nothing, 0, False)  ' Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

        Dim HPL As clsTranslation = iq.AddTranslation("HP", English, "bays", 0, Nothing, 0, False)
        Dim HPLL As clsTranslation = iq.AddTranslation("Hot Pluggable", English, "bays", 0, Nothing, 0, False)  ' Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

        Dim sql$
        sql$ = "SELECT DISTINCT sysfamilyname,systype,lifeCycleMonths,managementTxt,SecurityTxt,RangeText,subTitle,FamilyPriStor,FamilySecStor "
        sql$ &= "from " & server$ & "[iq].products.union_sysfamilydefinitions right join " & server$ & "[iq].products.sysrangetext ON sysfamilyname=rangename"
        sql$ &= " INNER JOIN h3.iq.products.systems ON systems.familycode=sysfamily WHERE modelsku IN (" & SKUs & ")"
        rdr = da.DBExecuteReader(con, sql$)

        Dim product As clsProduct ' family branches need a product to attach additional attributes to (primarly descriptions)

        Dim pa As clsProductAttribute

        Dim sysTrans As clsTranslation = iq.AddTranslation("systems", English, "collect", 0, Nothing, 0, False)
        Dim sysTransSingular As clsTranslation = iq.AddTranslation("system", English, "collect", 0, Nothing, 0, False)

        Dim fnpa As clsProductAttribute


        'find the existing families - check and fix them here



        'get a dictionary of the locations of all family branches by their fammajor attributes
        Dim famlocs As New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
        RootBranch.FindFamilyBranchesBelow("tree.1", 4, famlocs)

        Dim errormessages As New List(Of String)

        Dim FamBranch As clsBranch
        If rdr.HasRows Then
            While rdr.Read

                If Not IsDBNull(rdr.Item("sysfamilyname")) Then
                    Dim famMajor As String = rdr.Item("sysfamilyname")
                    If famlocs.ContainsKey(famMajor) Then
                        FamBranch = iq.Branches(Split(famlocs(famMajor), ".").Last)
                        If FamBranch.Product Is Nothing Then Stop
                        If FamBranch.Product.SKU <> "" Then Stop

                    Else

                        'If Not iq.EnglishIndex(rdr.Item("sysfamilyname"), "FamMajor") Is Nothing Then

                        ImportLog.Add(DateTime.Now, String.Format("Creating Family" & rdr.Item("sysfamilyname")))

                        'this is a 'virtual' product on the family branch = to hold a number of pan-family attributes - and the family image
                        Dim st As String = rdr.Item("systype")
                        If Not iq.i_ProductType_Code.ContainsKey(st) Then Dim nst As clsProductType = New clsProductType(st, iq.AddTranslation(st, English, "", 0, Nothing, 0, False), 0)
                        product = New clsProduct("", False, False, iq.i_sector_code("NoSector"), iq.i_ProductType_Code(st), CDate("01/01/2000"), CDate("31/12/2100"), True, False, True, "", "", "")

                        'record the family name under the 'majorFamily'  attribute on the branch - required for suppressing/displaying notes by family - see import.ExtText
                        fnpa = New clsProductAttribute(product, fMaj, 0, iq.i_unit_code("txt"), iq.AddTranslation(Trim$(rdr.Item("sysfamilyname")), English, "FamMajor", 0, Nothing, 0, False))

                        If Not rdr.Item("lifecyclemonths") Is DBNull.Value Then
                            pa = New clsProductAttribute(product, iq.i_attribute_code("lifeCycle"), rdr.Item("lifecyclemonths"), iq.i_unit_code("num"), iq.AddTranslation(rdr.Item("lifecyclemonths"), English, "", 0, Nothing, 0, False))
                        End If

                        If Not rdr.Item("managementTxt") Is DBNull.Value Then
                            pa = New clsProductAttribute(product, iq.i_attribute_code("management"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("managementTxt"), English, "", 0, Nothing, 0, False))
                        End If

                        If Not rdr.Item("securityTxt") Is DBNull.Value Then
                            pa = New clsProductAttribute(product, iq.i_attribute_code("security"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("securityTxt"), English, "", 0, Nothing, 0, False))
                        End If

                        If Not rdr.Item("rangeText") Is DBNull.Value Then
                            pa = New clsProductAttribute(product, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("rangeText"), English, "", 0, Nothing, 0, False))
                        End If

                        If Not rdr.Item("subTitle") Is DBNull.Value Then
                            pa = New clsProductAttribute(product, iq.i_attribute_code("subTitle"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("subTitle"), English, "", 0, Nothing, 0, False))
                        End If

                        'Large/small form factor dirve bays
                        Dim bays As Integer = 0 '1=sff 2 = lff 3 = both
                        If Not IsDBNull(rdr.Item("FamilyPriStor")) Then

                            If InStr(UCase(rdr.Item("FamilyPriStor")), "LFF") Then
                                bays = bays Or 2
                            End If
                            If InStr(UCase(rdr.Item("FamilyPriStor")), "SFF") Then
                                bays = bays Or 1
                            End If

                            Dim baytran As clsTranslation = Nothing
                            If bays = 1 Then baytran = sff
                            If bays = 2 Then baytran = lff
                            If bays = 3 Then baytran = bff ' both form factors

                            pa = New clsProductAttribute(product, iq.i_attribute_code("bays"), bays, iq.i_unit_code("txt"), baytran)

                            If InStr(UCase(rdr.Item("FamilyPriStor")), "HP") And InStr(UCase(rdr.Item("FamilyPriStor")), "NHP") = 0 Then
                                pa = New clsProductAttribute(product, iq.i_attribute_code("HPL"), 1, iq.i_unit_code("txt"), HPL)
                            End If
                        End If


                        Dim code As String = rdr.Item("sysFamilyName")
                        Dim FnEn As String
                        If dicAbbreviations.ContainsKey(code.ToLower) Then
                            FnEn = dicAbbreviations(code.ToLower) ''xlate()("en")
                        Else
                            FnEn = code
                            Logit("no abbreviation for " & code)
                        End If

                        Dim fntl As clsTranslation
                        'If iq.EnglishIndex.ContainsKey(FnEn) Then 'this is the abbreviation/key   - we do not append the word "family" (dans choice)
                        ' fntl = iq.EnglishIndex(FnEn)
                        'Else
                        fntl = iq.AddTranslation(FnEn, English, "", 0, Nothing, 0, False)
                        '               End If
                        '
                        Dim stBranch As clsBranch = Nothing
                        For Each cb In iq.Branches.Values
                            If cb.Picture IsNot Nothing AndAlso cb.Picture.Contains(rdr.Item("systype")) Then stBranch = cb : Exit For
                            '    If cb.ID > 100 Then Stop
                        Next

                        If stBranch Is Nothing Then
                            'Panic?!!
                            stBranch = New clsBranch(Nothing, iq.RootBranch, iq.AddTranslation(rdr.Item("systype"), English, "ST", 50, Nothing, 0, 0), "/images/iq/prod_range_" & rdr.Item("systype") & ".jpg", sysTrans, sysTransSingular, iq.Screens(719), 100, False, "S", Nothing, 0)
                            'Stop
                        End If

                        If stBranch.Product IsNot Nothing AndAlso stBranch.Product.SKU <> "" Then Stop
                        If product.i_Attributes_Code.ContainsKey("mfrsku") Then Stop
                        '  If SKUs <> "" Then Stop
                        FamBranch = New clsBranch(product, stBranch, fntl, "/images/iq/prod_" & rdr.Item("sysfamilyname") & ".gif", sysTrans, sysTransSingular, Nothing, 100, False, "B")

                        'add the family under its systype branch (Servers, Notebooks, desktops, storage etc)
                        ' - NO need - it's done internall now dicSysTypes(rdr.Item("systype")).childBranches.Add(FamBranch.ID, FamBranch)

                    End If
                End If
            End While
        Else
            ImportLog.Add(DateTime.Now, String.Format("No families affected"))
        End If

        rdr.Close()


        famlocs = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
        RootBranch.FindFamilyBranchesBelow("tree.1", 4, famlocs)

        Return famlocs


    End Function



    Public Sub PruneOffNonCompatableFamilyMinorSLotTypes() ' Can enable this for memory if needed, enforces a minor slot type against system slot types and prunes 'incompatable' ones
        Dim con = da.OpenDatabase(False)
        Dim sql$
        sql$ = "SELECT SysFamilyDefinitions.systype,SysFamilyDefinitions.sysfamily,sysfamilyname,familymem,familypristor,familysecstor,familyterstor FROM  h3.iQ.products.SysFamilyDefinitions"
        Dim dt = dataAccess.da.FilledDataTable(con, sql)

        sql$ = "SELECT  optsku from h3.iQ.products.options  where  opttype='HDD'"

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr("optsku").ToString) Then
                Dim prod = iq.i_SKU(rdr("optsku").ToString)
                If prod.SKU = "652605-B21" Then
                    Dim g = 8
                End If
                If prod.hasSKU Then
                    For Each branch In prod.Branches
                        If branch.Product IsNot Nothing AndAlso branch.Product.ProductType.Code.ToUpper = "HDD" Then
                            Dim hddslots = branch.slots.Where(Function(sl) sl.Value.Type.MajorCode = "HDD" AndAlso sl.Value.numSlots < 0)
                            If hddslots.Count > 0 Then
                                For Each path In branch.AllPaths
                                    Dim sto As List(Of String) = New List(Of String)()
                                    Dim newpath = ""
                                    Dim fam = branch.FindSystemAbove(path, newpath)
                                    If fam IsNot Nothing Then
                                        Dim rw = dt.Select("SysFamily='" & fam.Product.i_Attributes_Code("FamMinor").First.Translation.text(English) & "'")
                                        If rw.Length > 0 Then


                                            If Not IsDBNull(rw(0)("familypristor")) Then sto.Add(rw(0)("familypristor").ToString.ToUpper)
                                            If Not IsDBNull(rw(0)("familysecstor")) Then sto.Add(rw(0)("familysecstor").ToString.ToUpper)
                                            If Not IsDBNull(rw(0)("familyterstor")) Then sto.Add(rw(0)("familyterstor").ToString.ToUpper)


                                            If hddslots.Where(Function(hd) sto.Contains(hd.Value.Type.MinorCode.ToUpper)).Count = 0 Then
                                                Dim p = New clsPrune(path, New NullableInt(), "MinorCodeComp")
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        End If
                    Next
                End If
            End If
        End While
    End Sub

    Public Sub EnableSASLicense()
        For Each prod In iq.Products.Values
            If {"BC393A", "BC393AAE", "BC393B"}.Contains(prod.SKU) Then
                Dim f = New clsProductAttribute(prod, iq.i_attribute_code("supportSAS"), 1, iq.i_unit_code("num"), Nothing)
            End If
            If prod.i_Attributes_Code.ContainsKey("desc") AndAlso prod.i_Attributes_Code("desc").First.Translation.text(English).StartsWith("HP Dynamic Smart Array B320i") Then
                For Each branch In prod.Branches
                    Dim s = New clsSlot(iq.i_slotType_Code("MAN3").First.Value, branch, "", 1, Nothing, New NullableInt(), 0, 0)
                Next
            End If
        Next
    End Sub


    Public Sub preInstalledParts()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader


        Dim oCon As SqlClient.SqlConnection = da.OpenDatabase()
        Dim qwc As DataTable = da.MakeWriteCacheFor(oCon, "Quantity")

        Dim parts As Integer = 0
        Dim dupes As Integer = 0

        Dim query As String = "SELECT  pp.SysSN,pp.optsn,[OptQty],s.HierDescription as sysDesc,s.ModelSKU,o.DescriptionGen as optDesc,o.Optsku,o.OptType,o.OptType2 "
        query &= " FROM  [iQ].[products].[Systems_PreInstalledParts] pp  join products.systems as s on s.syssn=pp.syssn  join products.options as o on o.OptSN=pp.OptSN"

        Dim systemSku As String = String.Empty
        Dim optionSku As String = String.Empty
        rdr = da.DBExecuteReader(con, query)


        Dim systemLocations As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))(StringComparer.CurrentCultureIgnoreCase)  'SKU>Path
        iq.RootBranch.SkuPaths(systemLocations, "tree.1", False)
        While rdr.Read
            If rdr.Item("ModelSKU") IsNot DBNull.Value Then
                systemSku = rdr.Item("ModelSKU").ToString()

                If systemLocations.ContainsKey(systemSku) Then

                    If systemLocations(systemSku).Count > 1 Then Stop 'System appears in more than one place !
                    Dim systemTreePath As String = systemLocations(systemSku)(0)
                    Dim systemBranch As clsBranch = iq.Branches(Split(systemTreePath, ".").Last)
                    Dim optLocations As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))(StringComparer.CurrentCultureIgnoreCase)

                    'index the options
                    systemBranch.SkuPaths(optLocations, "", True)
                    Dim optSku As String = rdr.Item("Optsku").ToString()

                    '   If systemSku = "728546-421" And optSku = "732411-B21" Then Stop

                    If optLocations.ContainsKey(optSku) Then
                        If optLocations(optSku).Count > 1 Then Stop
                        Dim branch As clsBranch = iq.Branches(Split(optLocations(optSku)(0), ".").Last)
                        Dim fullpath As String = systemLocations(systemSku)(0) & optLocations(optSku)(0)
                        Dim whereItsAt As String = PathName(fullpath)

                        Dim ck As String = branch.ID & "^" & r_worldwide.ID & "^" & fullpath
                        If i_Quantities.Contains(ck) Then
                            NoOp()
                            dupes += 1
                        Else
                            Dim qty As Integer = CInt(rdr("OptQty"))
                            Dim aQty As clsQuantity = New clsQuantity(r_worldwide, fullpath, branch, qty, 1, 1, True, qwc)
                            parts += 1
                        End If
                    End If
                End If
            Else
                Stop 'WTF ?
            End If
        End While
        rdr.Close()

        da.BulkWrite(oCon, qwc, "Quantity")

        con.Close()

    End Sub

    'Public Sub OtherPreInstalled()

    '    Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
    '    Dim rdr As SqlClient.SqlDataReader


    '    Dim oCon As SqlClient.SqlConnection = da.OpenDatabase()
    '    Dim qwc As DataTable = da.MakeWriteCacheFor(oCon, "Quantity")

    '    Dim parts As Integer = 0
    '    Dim dupes As Integer = 0

    '    Dim query As String = "select nic,display,raid,modelsku from h3.iq.products.systems"
    '    Dim systemSku As String = String.Empty
    '    Dim optionSku As String = String.Empty
    '    rdr = da.DBExecuteReader(con, query)
    '    Dim SkuLocations As Dictionary(Of String, String) = New Dictionary(Of String, String)  'SKU>Path
    '    iq.RootBranch.SkuPaths(SkuLocations, "Tree.1", False)
    '    While rdr.Read
    '        If rdr.Item("ModelSKU") IsNot DBNull.Value Then
    '            systemSku = rdr.Item("ModelSKU").ToString()
    '            If SkuLocations.ContainsKey(systemSku) Then

    '                Dim systemTreePath As String = SkuLocations(systemSku)
    '                Dim systemBranch As clsBranch = iq.Branches(Split(systemTreePath, ".").Last)
    '                Dim optLocations As Dictionary(Of String, String) = New Dictionary(Of String, String)
    '                systemBranch.SkuPaths(optLocations, Split(systemTreePath, ".").Last, True)
    '                Dim optSku As String = rdr.Item("NIC").ToString()
    '                If optLocations.ContainsKey(optSku) Then
    '                    Dim optTreePath As String = optLocations(optSku)
    '                    Dim branch As clsBranch = iq.Branches(Split(optTreePath, ".").Last)
    '                    Dim fullpath As String = Left(systemTreePath, InStrRev(systemTreePath, ".")) & optTreePath

    '                    Dim ck As String = branch.ID & "^" & r_worldwide.ID & "^" & fullpath
    '                    If i_Quantities.Contains(ck) Then
    '                        NoOp()
    '                        dupes += 1
    '                    Else
    '                        Dim aQty As clsQuantity = New clsQuantity(r_worldwide, fullpath, branch, 1, 1, 1, True, qwc)
    '                        parts += 1
    '                    End If
    '                Else
    '                    'No part so lets just add the info (for spec table etc)
    '                    Dim a = New clsProductAttribute(systemBranch.Product, iq.i_attribute_code("NetworkCard"), 0, iq.i_unit_code("txt"), New clsTranslation(English, optSku))
    '                End If
    '                optSku = rdr.Item("raid").ToString()
    '                If optLocations.ContainsKey(optSku) Then
    '                    Dim optTreePath As String = optLocations(optSku)
    '                    Dim branch As clsBranch = iq.Branches(Split(optTreePath, ".").Last)
    '                    Dim fullpath As String = Left(systemTreePath, InStrRev(systemTreePath, ".")) & optTreePath

    '                    Dim ck As String = branch.ID & "^" & r_worldwide.ID & "^" & fullpath
    '                    If i_Quantities.Contains(ck) Then
    '                        NoOp()
    '                        dupes += 1
    '                    Else
    '                        Dim aQty As clsQuantity = New clsQuantity(r_worldwide, fullpath, branch, 1, 1, 1, True, qwc)
    '                        parts += 1
    '                    End If
    '                Else
    '                    'No part so lets just add the info (for spec table etc)
    '                    Dim a = New clsProductAttribute(systemBranch.Product, iq.i_attribute_code("RaidCard"), 0, iq.i_unit_code("txt"), New clsTranslation(English, optSku))
    '                End If
    '                optSku = rdr.Item("display").ToString()
    '                If optLocations.ContainsKey(optSku) Then
    '                    Dim optTreePath As String = optLocations(optSku)
    '                    Dim branch As clsBranch = iq.Branches(Split(optTreePath, ".").Last)
    '                    Dim fullpath As String = Left(systemTreePath, InStrRev(systemTreePath, ".")) & optTreePath

    '                    Dim ck As String = branch.ID & "^" & r_worldwide.ID & "^" & fullpath
    '                    If i_Quantities.Contains(ck) Then
    '                        NoOp()
    '                        dupes += 1
    '                    Else
    '                        Dim aQty As clsQuantity = New clsQuantity(r_worldwide, fullpath, branch, 1, 1, 1, True, qwc)
    '                        parts += 1
    '                    End If
    '                Else
    '                    'No part so lets just add the info (for spec table etc)
    '                    Dim a = New clsProductAttribute(systemBranch.Product, iq.i_attribute_code("Display"), 0, iq.i_unit_code("txt"), New clsTranslation(English, optSku))
    '                End If
    '            End If
    '        End If
    '    End While
    '    rdr.Close()

    '    da.BulkWrite(oCon, qwc, "Quantity")

    '    con.Close()

    'End Sub

    Public Sub fixProductFamilies()

        'DONT use this any more
        Stop

        'Pushes the Family attributes of products through dans Abbreviations - to make G8 families into Gen8

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        Dim errormessages As List(Of String) = New List(Of String)

        LoadAbbreviations(con)

        Dim done As Integer = 0
        Dim pa As clsProductAttribute

        For Each product In iq.Products.Values
            If product.i_Attributes_Code.ContainsKey("FamMajor") Then
                pa = product.i_Attributes_Code("FamMajor")(0)
                Dim txt$ = pa.Translation.text(English)
                If dicAbbreviations.ContainsKey(txt$) Then
                    If dicAbbreviations(txt$) <> txt$ Then
                        pa.Translation = iq.AddTranslation(dicAbbreviations(txt$), English, "fams", 0, Nothing, 0, False)
                        pa.update(errormessages)
                        done += 1
                    End If
                Else
                    Debug.Print(txt$)
                End If
            End If
        Next

        ' OutputErrors()

    End Sub



    Public Sub WriteDicOptions(options As Dictionary(Of String, clsProduct), filename$)

        Dim sw As New IO.StreamWriter(filename$, False)

        For Each ck In options.Keys
            sw.WriteLine(ck & " ---" & options(ck).DisplayName(English))
        Next

        sw.Close()

    End Sub

    Public Function fixPci()

        Dim con As SqlClient.SqlConnection
        Dim rdr As SqlClient.SqlDataReader

        con = da.OpenDatabase()

        Dim dicSystems As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "system")

        Import.slotTypes(con, dicSystems)


    End Function

    Public Sub everything()

        iq.PNAdown = True 'Suspend the webservice whilst running the import

        Logit("Import started " & Now.ToString)

        '        Dim ImportEvent As clsEvent
        '        ImportEvent = New clsEvent(iq.RootEvent, "Iquote 1 Import", ev_Info)

        'Dim EventDicLoad As clsEvent = New clsEvent(ImportEvent, "Loading IQ1 dictionaries", ev_Info)
        'Dim eventDicSave As clsEvent = New clsEvent(ImportEvent, "Saving updated dictionaries", ev_Info)

        xlate = New Dictionary(Of String, Dictionary(Of String, String))(StringComparer.CurrentCultureIgnoreCase) 'see BtnImport_click 

        Dim con As SqlClient.SqlConnection
        Dim rdr As SqlClient.SqlDataReader

        con = da.OpenDatabase()

        Dim dicSystems As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "system")
        Dim OptionsBySku As Dictionary(Of String, clsProduct) = loadDic(con, iq.Products, "option")
        '        Dim DicVariants As Dictionary(Of String, clsVariant) = loadDic(con, iq.Variants, "variant")

        ' If Not DicVariants.ContainsKey("") Then DicVariants.Add("", iq.StandardVariant)
        ' If Not DicVariants.ContainsKey("ABU") Then DicVariants.Add("ABU", New clsVariant("ABU", "", "UK", "", "#ABU", ""))
        ' If Not DicVariants.ContainsKey("ABF") Then DicVariants.Add("ABF", New clsVariant("ABF", "", "France", "", "#ABF", ""))
        'saveDic(con, DicVariants, "variant")

        Dim dicChannels As Dictionary(Of String, clsChannel) = loadDic(con, iq.Channels, "channel")
        Dim dicRegions As Dictionary(Of String, clsRegion) = loadDic(con, iq.Regions, "region")

        ' build a lookup of currencies by country code
        Dim DicRegionCurrency As Dictionary(Of String, clsCurrency) = loadDic(con, iq.Currencies, "coCurr")

        con.Close()
        con = da.OpenDatabase
        Import.LoadAbbreviations(con)

        'LANGUAGES
        Dim aLanguage As clsLanguage = Nothing
        Dim dicLanguage As Dictionary(Of String, clsLanguage) = loadDic(con, iq.Languages, "lang")
        'If Not dicLanguage.ContainsKey("en") Then dicLanguage.Add("en", iq.i_language_Code("EN"))

        con.Close()
        con = da.OpenDatabase

        Import.Languages(con, dicLanguage)
        saveDic(con, dicLanguage, "lang")

        'Iquote 1 Code to Expanded name
        'populate the dictionary of all option types (MEM/HDD/CPU) + NOTEBOOK,DESKTOP,SERVER

        Dim dicOptTypes As Dictionary(Of String, clsProductType) = New Dictionary(Of String, clsProductType)(StringComparer.CurrentCultureIgnoreCase)
        dicOptTypes = loadDic(con, iq.ProductTypes, "optType")
        Import.ProductTypes(con, dicOptTypes)

        If Not iq.i_ProductType_Code.ContainsKey("CHAS") Then
            Dim chassisPT As clsProductType = New clsProductType("CHAS", iq.AddTranslation("Chassis", English, "UI", 0, Nothing, 0, False), 0) 'Add a Chassis Option type
        End If

        If Not dicOptTypes.ContainsKey("CHAS") Then
            dicOptTypes.Add("CHAS", iq.i_ProductType_Code("CHAS")) '
        End If
        'dicOptTypes.Add("MOBO", New clsProductType("MOBO", iq.AddTranslation("MotherBoard", English, "UI", 0, Nothing, 0, False))) 'Add a Mobo

        saveDic(con, dicOptTypes, "optType")

        Import.LoadTranslations(con) 'populates the xlate dictionary (from dbo.language_key)

        'makes branches for the Desktop/notebook/server level branches
        Dim dicSysTypes As Dictionary(Of String, clsBranch) = loadDic(con, iq.Branches, "sysType")
        Import.SysTypes(con, dicSysTypes)
        saveDic(con, dicSysTypes, "sysType")

        Dim DicSectors As Dictionary(Of String, clsSector) = loadDic(con, iq.Sectors, "sector")
        Import.Sectors(con, DicSectors) 'poulates iq.sectors - HP Business UNITS ISS/PSG/SWD etc
        saveDic(con, DicSectors, "sector")

        'contains the family branches (each one containing a number of systems)
        ' loadDic(con, iq.Branches, "family")
        Dim dicFamily As Dictionary(Of String, clsBranch) = Import.Families(con, dicSysTypes)
        saveDic(con, dicFamily, "family")

        Logit("building PLcode lookup dictionary")
        Dim dicplcode As Dictionary(Of String, String)
        dicplcode = LoadPLCodes(con) 'generates a dictionary of SKU>Plcode

        Dim dicCurrencies As Dictionary(Of String, clsCurrency) = loadDic(con, iq.Currencies, "currency")
        ' Import.Currencies(con, dicCurrencies)
        saveDic(con, dicCurrencies, "currency")

        dicRegions = Import.Regions(con)  ', dicRegions) ', DicRegionCurrency) '15 seconds or so ! (yuck)
        saveDic(con, dicRegions, "region")

        'used for high speed fixing of localisations

        Dim containment As Dictionary(Of String, List(Of String)) = clsRegion.containment()
        Dim errormessages As List(Of String) = New List(Of String)

        'bloody ages *circa 1 minute)
        'saveDic(con, dicSystems, "systemB4")  'keep a copy of the systems we had *before* this update (so we know which to add in buildtree() later)
        Import.Systems(con, dicSystems, dicFamily, dicplcode, containment, errormessages)
        saveDic(con, dicSystems, "system")

        Logit("Imported " & dicSystems.Count & " systems", False, True)

        'Import.givesslots(con, dicSystems, dicOptTypes) 'makes the non PCI 'gives' slots (drive bays, memory etc)

        'ND NOT ((options.OptType='CPU' AND options.OptSKU<>sys.CPU))

        '4 secs
        Dim numDescs As Integer
        Dim dicDescs As Dictionary(Of String, clsTranslation) = loadDic(con, iq.Translations, "sysDesc")
        numDescs = Import.SystemDescriptions(con, dicDescs, dicSystems)
        saveDic(con, dicDescs, "sysDesc")

        'OS
        'Dim numOS As Integer
        ' Dim dicOS As Dictionary(Of String, clsTranslation) = loadDic(con, iq.Translations, "sysOS")
        ' numOS = Import.SystemDescriptions(con, dicOS, dicSystems)
        ' saveDic(con, dicOS, "sysOS")


        'units
        Dim dicUnits As Dictionary(Of String, clsUnit) = loadDic(con, iq.Units, "unit")
        Import.units(con, dicUnits)
        saveDic(con, dicUnits, "unit")

        'Options is a Biggie - returns a flat list (of option products by SKU)  and populates the 6D dictionary Dicsysfam
        'of all potential options under each SysFamily



        '                           sysfam^l1^l2^l3^optSn 
        'Dim optionsByCK As Dictionary(Of String, clsProduct) = _
        'Import.options(con, OptionsBySku, dicplcode, dicOptLocalisation, dicUnits, containment)

        'system family names are the short(broad) codes - like DL580eG8
        'sysFamily (or sysfamilyCode) are the narrower long codes like DL580eG8C25SFFLRD        
        'WriteDicOptions(optionsByCK, "c:\temp\options.txt") 'just for debugging purposes

        'saveDic(con, optionsByCK, "option")

        'buildtree takes the 5D dicitonary and grafts the 'master' (per family) copy options onto every system - then prunes the incompatible options off
        'for incremental imports..it only adds systems that are not already in the import dictionary
        'loadDic(con, dicSystems, iq.Branches, "systemB4")

        'option quanitities (products.options.localisation)
        'around 3 minutes

        ' Import.BuildTree(con, dicSystems, OptionsBySku, dicFamily, optionsByCK, dicOptTypes, dicOptLocalisation, ImportEvent, errormessages)

        Dim dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)) = New Dictionary(Of clsProduct, List(Of clsRegion))
        Dim opts As Dictionary(Of String, clsBranch) = Import.options2(con, dicplcode, dicUnits, dicOptLocalisation, containment) '- POPULATES dicOptLocatlisations

        con.Close()

        'new - 'Flags' some options as FIOs
        Import.FIOfocus()

        con = da.OpenDatabase()
        Buildtree2(con, opts, dicFamily, dicSystems, dicOptLocalisation) '1 minute  

        con.Close()

        con = da.OpenDatabase()

        Logit("BuiltTree", False, True)

        iq.LoadGrafts(con, errormessages) 'we'll be needing these ! (to recurse the prodcut tree correctly)
        ' Import.TopRecommendations()
        Import.HighPerformance()

        'Import.CPUs(con) 'fails beacuse we cant get descendants if we're not logged in

        opts = Nothing ' free some memory !

        'saveDic(con, DicRegionCurrency, "rgCurr")

        'Import channels and clones
        'old' Channeld ID's to new IQ2 channel objects

        con.Close()
        con.Dispose()
        con = da.OpenDatabase()

        'around 40 secs
        Import.channels(con, dicChannels, dicRegions, errormessages)

        con.Close()
        con = da.OpenDatabase()
        saveDic(con, dicChannels, "channel")

        'USERS, ACCOUNTS and TEAMS about 21 seconds
        Dim dicAccounts As Dictionary(Of String, clsAccount) = loadDic(con, iq.Accounts, "account")
        Dim dicTeams As Dictionary(Of String, clsTeam) = loadDic(con, iq.Teams, "team")
        Dim dicUsers As Dictionary(Of String, clsUser) = loadDic(con, iq.Users, "user")

        '15 secs
        Import.users(con, dicChannels, dicAccounts, dicTeams, dicUsers)

        saveDic(con, dicAccounts, "account")
        saveDic(con, dicTeams, "team")
        saveDic(con, dicUsers, "user")

        dicAccounts = Nothing
        dicTeams = Nothing
        dicUsers = Nothing

        '     Import.DoPrunes()


        'TODO write the dictionary out to create priceBands table - actually, no we'll put them in the clsAccounts
        'For Each seller In dicpriceBands.Keys
        ' For Each buyer In dicpriceBands(seller).Keys
        'Next
        'Next

        'PRICES
        'gets the 'base' pricing for each seller
        'about 40 secs

        System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
        GC.Collect()

        ' anevent = New clsEvent(ImportEvent, "prices", ev_Info)
        ' Import.Prices(con, dicSystems, dicOptions, dicChannels) ', DicVariants)
        ' anevent.update()

        con.Close()
        con = da.OpenDatabase()

        Dim dicStock As Dictionary(Of String, clsstock) = loadDic(con, iq.Stock, "Stock") 'now loadUp the dictionary with previously imported stock (from PNA_Stock)

        'around 40 secs
        'NOBBLED   Import.Stock(con, dicStock, dicSystems, dicOptions, dicChannels, anevent)

        '        con.Close()
        '        con = da.OpenDatabase()
        '        saveDic(con, dicStock, "Stock")
        '        anevent.update()

        '14 secs
        'Calculates the margins per product type per (buying) customer
        'Import.ExtText()

        Import.Margins(con, dicSystems, OptionsBySku, dicChannels)


        con.Close()
        con = da.OpenDatabase()

        'around 18 seconds
        Dim dicAutoAdds As Dictionary(Of String, clsProduct) ' = loadDic(con, iq.Products, "autoAdd")
        dicAutoAdds = New Dictionary(Of String, clsProduct)

        Import.autoadds(con, dicAutoAdds, dicSystems, dicRegions)
        saveDic(con, dicAutoAdds, "autoadd")

        con.Close()
        con.Dispose()
        con = da.OpenDatabase()

        System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
        GC.Collect()

        Dim dicStates As Dictionary(Of String, clsState)
        dicStates = Import.quoteStates(con)

        ' - temporaily removed        Import.slotAdds(con) ' options that add slots (like CPUs that enable memory)

        'Get the disting listprice countries - so we can load one country at once
        'need the 'everyone channel' to exist first
        If False Then

            Dim con2 As SqlClient.SqlConnection = da.OpenDatabase("Data Source=iquote2.channelcentral.net\charliel,8484\;Initial Catalog=Pricing; password=wainwright; user id=editor; Connection Timeout=10;")
            Dim lpr As SqlDataReader = da.DBExecuteReader(con2, "SELECT DISTINCT country from pricing.products.hpPriceList")
            Dim lpcountries As List(Of String) = New List(Of String)
            While lpr.Read
                lpcountries.Add(lpr.Item("country"))
            End While
            lpr.Close()
            con2.Close()

            For Each lpc In lpcountries
                Import.listprices(con, lpc)  'needed for calculation of avalanche rebates during import of quote options
            Next lpc
        End If



        GetVariants("UNHOSTED", "", errormessages)

        con = da.OpenDatabase()
        Import.RefCodes(con)  'adds the refcodes to every option
        '  Import.Avalanche(con)  'imports the avalanche offers from Datastore.Products_Avalalance_rules
        '  Import.Bundles(con)


        Import.preInstalledParts()  'DL570 Memory boards (amongst other thigns)

        '        Import.defaultWarranty()

        If False Then
            quoteImport.all(con)
        End If

        con.Close()
        con.Dispose()
        con.Dispose()

        'Setup username sand passwords
        'Dim ac As clsAccount
        'Dim u As clsUser = iq.i_user_email("tim.moyle@channelcentral.net")
        'ac = u.Accounts(iq.i_channel_code("DAZRG248NE").ID)
        'ac.priceBand = "325009"
        'ac.update(errormessages)

        '  ac = iq.i_user_email("tim.moyle@channelcentral.net").Accounts(iq.i_channel_code("DWERG74AH").ID)
        '  ac.priceBand = "CHA097"
        '  ac.update(errormessages)

        'reload the OM
        clsIQ.reset()
        Dim d = iq.Users.Count
        While (Not clsIQ.IsLoaded)
            System.Threading.Thread.Sleep(100)
        End While

        Import.SoftwareSlots()

        con = da.OpenDatabase()
        Import.CPUs(con, errormessages)

        Import.Extras()
        Import.Networking()
        Import.Graphics()
        Import.ImportQuickSpecs()
        Import.InterfaceSlots()
        _Default.SetRCAs(errormessages)
        Import.TopRecommendations()
        ' Import.ExtText()
        Import.FlexOPGs()
        Import.PowerSizing(con)
        Import.RunSQLScripts()
        Import.NetworkSlots(con)
        con.Close()


        Logit("Import complete " & Now.ToString, False, True)
        iq.PNAdown = False

    End Sub


    Sub RunSQLScripts()
        For Each f As FileInfo In New DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.GetData("DataDirectory"), "..", "Modules", "ImportSQL")).GetFiles("*.sql")
            Dim con As SqlConnection = da.OpenDatabase(True)
            Dim fs As FileStream = f.OpenRead()
            Dim tr As TextReader = New StreamReader(fs)

            Dim sql As SqlCommand = New SqlCommand(tr.ReadToEnd(), con)
            sql.CommandTimeout = 500000
            sql.ExecuteNonQuery()

        Next

        clsIQ.reset()
    End Sub
    'Public Sub addTROtoSC(tro As clsBranch, SCk As String, dicFinished As Dictionary(Of String, clsBranch), hptro As clsTranslation, sing As clsTranslation, plur As clsTranslation)


    '    'SCK is the supplcani branches Compound key
    '    Dim hb As clsBranch 'header branch
    '    If Not dicFinished.ContainsKey(SCk) Then

    '        hb = New clsBranch(Nothing, Nothing, hptro, "", plur, sing, Nothing, 0, False, )
    '        dicFinished.Add(SCk, hb)

    '    Else
    '        hb = dicFinished(SCk)
    '    End If

    '    'check wether the header has the tros catergory branch - and/or make it
    '    'add the tro to the cat

    'End Sub

    Public Sub NetworkSlots(con)

        Dim dt As New DataTable
        dt = da.MakeWriteCacheFor(con, "Slot")

        For Each b In iq.Branches.Values
            If b.Product IsNot Nothing Then
                If b.Product.i_Attributes_Code.ContainsKey("PriPorts") Then
                    Dim slt = splitNetworkSlotType(b.Product.i_Attributes_Code("PriConnectivity").First.Translation.text(English))
                    If slt.Length > 0 Then
                        If Not iq.i_slotType_Code.ContainsKey(slt(0)) OrElse Not iq.i_slotType_Code(slt(0)).ContainsKey(slt(1)) Then
                            Dim c = New clsSlotType(slt(0), slt(1), iq.AddTranslation("", English, "slottype", 0, Nothing, 0, False))
                        End If
                        Dim s = New clsSlot(iq.i_slotType_Code(slt(0))(slt(1)), b, Nothing, b.Product.i_Attributes_Code("PriPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, Nothing, 0, False), New NullableInt(), 0, 0, dt)
                        If slt(2) <> "" Then
                            If Not iq.i_slotType_Code.ContainsKey(slt(2)) OrElse Not iq.i_slotType_Code(slt(2)).ContainsKey(slt(3)) Then
                                Dim c = New clsSlotType(slt(2), slt(3), iq.AddTranslation("", English, "slottype", 0, Nothing, 0, False))
                            End If
                            s = New clsSlot(iq.i_slotType_Code(slt(2))(slt(3)), b, Nothing, b.Product.i_Attributes_Code("PriPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, Nothing, 0, False), New NullableInt(), 0, 0, dt)
                        End If
                    End If
                End If
                If b.Product.i_Attributes_Code.ContainsKey("SecPorts") Then
                    Dim slt = splitNetworkSlotType(b.Product.i_Attributes_Code("SecConnectivity").First.Translation.text(English))
                    If slt.Length > 0 Then
                        If Not iq.i_slotType_Code.ContainsKey(slt(0)) OrElse Not iq.i_slotType_Code(slt(0)).ContainsKey(slt(1)) Then
                            Dim c = New clsSlotType(slt(0), slt(1), iq.AddTranslation("", English, "slottype", 0, Nothing, 0, False))
                        End If
                        Dim s = New clsSlot(iq.i_slotType_Code(slt(0))(slt(1)), b, Nothing, b.Product.i_Attributes_Code("SecPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, Nothing, 0, False), New NullableInt(), 0, 0, dt)
                        If slt(2) <> "" Then
                            If Not iq.i_slotType_Code.ContainsKey(slt(2)) OrElse Not iq.i_slotType_Code(slt(2)).ContainsKey(slt(3)) Then
                                Dim c = New clsSlotType(slt(2), slt(3), iq.AddTranslation("", English, "slottype", 0, Nothing, 0, False))
                            End If
                            s = New clsSlot(iq.i_slotType_Code(slt(2))(slt(3)), b, Nothing, b.Product.i_Attributes_Code("SecPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, Nothing, 0, False), New NullableInt(), 0, 0, dt)
                        End If
                    End If
                End If
                If b.Product.i_Attributes_Code.ContainsKey("UpPorts") Then
                    Dim slt = splitNetworkSlotType(b.Product.i_Attributes_Code("UpConnectivity").First.Translation.text(English))
                    If slt.Length > 0 Then
                        If Not iq.i_slotType_Code.ContainsKey(slt(0)) OrElse Not iq.i_slotType_Code(slt(0)).ContainsKey(slt(1)) Then
                            Dim c = New clsSlotType(slt(0), slt(1), iq.AddTranslation("", English, "slottype", 0, Nothing, 0, False))
                        End If
                        Dim s = New clsSlot(iq.i_slotType_Code(slt(0))(slt(1)), b, Nothing, b.Product.i_Attributes_Code("UpPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, Nothing, 0, False), New NullableInt(), 0, 0, dt)
                        If slt(2) <> "" Then
                            If Not iq.i_slotType_Code.ContainsKey(slt(2)) OrElse Not iq.i_slotType_Code(slt(2)).ContainsKey(slt(3)) Then
                                Dim c = New clsSlotType(slt(2), slt(3), iq.AddTranslation("", English, "slottype", 0, Nothing, 0, False))
                            End If
                            s = New clsSlot(iq.i_slotType_Code(slt(2))(slt(3)), b, Nothing, b.Product.i_Attributes_Code("UpPorts").First.NumericValue, iq.AddTranslation("", English, "", 0, Nothing, 0, False), New NullableInt(), 0, 0, dt)
                        End If
                    End If
                End If
                If b.Product.i_Attributes_Code.ContainsKey("POEP") AndAlso b.Product.i_Attributes_Code.ContainsKey("POETech") Then
                    If Not iq.i_slotType_Code.ContainsKey("POE") OrElse Not iq.i_slotType_Code("POE").ContainsKey(b.Product.i_Attributes_Code("POETech").First.Translation.text(English)) Then
                        Dim c = New clsSlotType("POE", b.Product.i_Attributes_Code("POETech").First.Translation.text(English), iq.AddTranslation("", English, "slottype", 0, Nothing, 0, False))
                    End If
                    Dim s = New clsSlot(iq.i_slotType_Code("POE")(b.Product.i_Attributes_Code("POETech").First.Translation.text(English)), b, Nothing, b.Product.i_Attributes_Code("POEP").First.NumericValue, iq.AddTranslation("", English, "", 0, Nothing, 0, False), New NullableInt(), 0, 0, dt)
                End If
            End If
        Next
        da.BulkWrite(con, dt, "slot")
    End Sub
    Private Function splitNetworkSlotType(desc As String) As String()
        Dim majorCode1 = ""
        Dim majorCode2 = ""
        Dim minorCode1 = ""
        Dim minorCode2 = ""
        Dim j = 0
        Dim bump = 0

        Dim i = desc.IndexOf("Dual Personality")
        majorCode1 = "RJ45"
        If i > -1 Then
            bump = 17
            majorCode2 = "SFP"
            minorCode2 = "Open Mini-GBIC"
            j = desc.IndexOf(" or ") - i
        End If
        If i < 0 Then
            i = desc.IndexOf("SFP+")
            majorCode1 = "SFP+"
        End If
        If i < 0 Then
            i = desc.IndexOf("SFP")
            majorCode1 = "SFP"
        End If
        If i < 0 Then
            i = desc.IndexOf("RJ45")
            majorCode1 = "RJ45"
        End If
        If i < 0 Then
            i = desc.IndexOf("XENPACK")
            majorCode1 = "XENPACK"
        End If
        If i < 0 Then
            Return {}
        End If
        If j = 0 Then j = desc.Length - i - majorCode1.Length - 1
        minorCode1 = desc.Substring(i + majorCode1.Length + bump + 1, j).Replace("Ethernet", "").Trim()

        Return {majorCode1, minorCode1, majorCode2, minorCode2}
    End Function

    Public Sub energyStar()

        'this is a one off to patch the ballsed up import

        Dim estar As clsAttribute = iq.i_attribute_code("eStar")

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim wc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")

        'Energy star options
        Dim Sql$ = "SELECT modelsku from h3.iQ.products.systems where energystar=1"

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, Sql$)

        Dim esc As Integer = 0
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("modelsku")) Then
                Dim sys As clsProduct = iq.i_SKU(rdr.Item("modelsku"))
                Dim pa As clsProductAttribute = New clsProductAttribute(sys, estar, 1, iq.i_unit_code("txt"), Nothing, wc)
                esc += 1
            End If
        End While
        rdr.Close()

        da.BulkWrite(con, wc, "ProductAttribute")

    End Sub

    Public Sub CPUs(con As SqlClient.SqlConnection, ByRef errormessages As List(Of String))


        'Delete any (preinstalled) quantities pertaining to CPU's

        Dim cpuProdID As Integer = iq.i_ProductType_Code("CPU").ID
        Dim sql$ = "DELETE FROM Quantity WHERE FK_Branch_ID IN (SELECT ID from Branch where FK_Product_ID in (select id from product where fk_productType_id='" & cpuProdID & "'));"
        da.DBExecutesql(sql$)

        'delete any slots on branches carrying CPU products
        sql$ = "DELETE FROM Slot WHERE FK_Branch_ID IN (SELECT ID from Branch where FK_Product_ID in (select id from product where fk_productType_id='" & cpuProdID & "'));"
        da.DBExecutesql(sql$)

        'delete the grafts of cpu branches
        sql$ = "DELETE FROM Graft WHERE FK_Branch_ID_Source in (select ID from Branch where FK_Product_ID in (select id from product where fk_producttype_id='" & cpuProdID & "'))"
        da.DBExecutesql(sql$)


        'delete quoteitems which reference CPU branches
        sql$ = "DELETE FROM Quoteitem WHERE FK_Branch_ID IN (SELECT ID from Branch where FK_Product_ID in (select id from product where fk_productType_id='" & cpuProdID & "'));"
        da.DBExecutesql(sql$)


        'delete the actual exsiting CPU branches
        sql$ = "DELETE FROM Branch where FK_Product_ID in (select id from product where fk_productType_id='" & cpuProdID & "');"
        da.DBExecutesql(sql$)

        'Note - we don't delete the cpu PRODUCTS at any point (they're not part of the CPU import)

        'Fetch IQ1 limits for CPUs (specifically) into a dictionary - accesible by family code

        sql$ = "SELECT [SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],qtymin,[Incr_Min],[Incr_Pref] FROM " & server$ & "[iq].[products].[OptionLimits] "
        sql$ &= "INNER JOIN " & server$ & "[iq].[products].[opttypes] o ON o.OptTypeCode = opttype WHERE opttype='CPU'"




        Dim dicCpuLimitsBySysFamCode As Dictionary(Of String, clsLimit)
        dicCpuLimitsBySysFamCode = New Dictionary(Of String, clsLimit)(StringComparer.CurrentCultureIgnoreCase)

        Dim dicCPUs As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase) 'SysFamily>CPU Branch

        Dim cpuRoot As clsBranch = New clsBranch(Nothing, Nothing, _
                                                 iq.AddTranslation("CPU", English, "UI", 0, Nothing, 0, False), "", _
                                                 iq.AddTranslation("Processors", English, "collect", 0, Nothing, 0, False), _
                                                 iq.AddTranslation("Processor", English, "collect", 0, Nothing, 0, False), iq.Screens(719), 1, False, "BG")

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql)

        With dicCpuLimitsBySysFamCode
            While rdr.Read
                Dim prf As Integer = 1
                If Not IsDBNull(rdr.Item("incr_pref")) Then prf = rdr.Item("incr_pref")
                'If rdr.Item("qtymax") > 1 Then Stop
                .Add(rdr.Item("sysfamily"), New clsLimit(rdr.Item("QtyInstalled"), rdr.Item("QtyMin"), rdr.Item("qtymax"), rdr.Item("incr_min"), prf))

            End While
        End With
        rdr.Close()

        'Whilst the increments and max come from the subfamily - the actual number of installed CPUS comes from products.systems
        Dim SysCpuQTY As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
        sql$ = "SELECT cpuqty,modelsku from h3.iq.products.systems"
        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            If Not IsDBNull(rdr.Item("cpuqty")) Then
                SysCpuQTY.Add(rdr.Item("modelsku"), rdr.Item("cpuqty"))
            End If
        End While
        rdr.Close()

        Dim todel As List(Of clsSlot) = New List(Of clsSlot)

        'anything without a set of optionlimits - will use this
        Dim StandardLimits As clsLimit = New clsLimit(1, 1, 1, 0, 0)

        Dim sysPaths As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))
        iq.RootBranch.SkuPaths(sysPaths, "tree.1", False)  'find the location of every system

        Dim processor As clsTranslation = iq.AddTranslation("Processor", English, "OL2", 0, Nothing, 0, False)
        Dim processors As clsTranslation = iq.AddTranslation("Processors", English, "OL2", 0, Nothing, 0, False)

        Dim performance As clsTranslation = iq.AddTranslation("Performance", English, "OL1", 0, Nothing, 0, False)
        Dim pbranches As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)

        Dim qtywritecache As DataTable = da.MakeWriteCacheFor(con, "quantity")
        Dim slotwritecache As DataTable = da.MakeWriteCacheFor(con, "slot")

        Dim done As Integer = 0
        Dim nonexistent As New List(Of String)


        For Each systemSKU In sysPaths.Keys.ToArray
            '            If sysPaths(systemSKU).Count > 1 Then Stop 'this system appears in more than one place in the tree
            Dim systembranch As clsBranch = iq.Branches(Split(sysPaths(systemSKU)(0), ".").Last)
            If systembranch.Product.i_Attributes_Code.ContainsKey("cpuSKU") Then
                Dim cputl As clsTranslation = systembranch.Product.i_Attributes_Code("cpuSKU")(0).Translation
                Dim cpusku = cputl.text(English)

                If Not systembranch.Product.i_Attributes_Code.ContainsKey("FamMinor") Then
                    Logit(systembranch.DisplayName(English) & " Has no minor family")
                Else
                    Dim subfam As String = systembranch.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English)

                    'If Not cpusku.StartsWith("###") Then
                    If cpusku = "664011-B21" Then
                        Dim a = 9
                    End If
                    If Not iq.i_SKU.ContainsKey(cpusku) Then
                        Logit("CPU " & cpusku & " does not exist""")
                        If Not nonexistent.Contains(cpusku) Then
                            nonexistent.Add(cpusku)
                        End If

                    Else
                        Dim cpuProd As clsProduct = iq.i_SKU(cpusku)

                        'constructs the 'master' set of CPU's unde a cpuRoot branch (hanging in space)
                        Dim cpuBranch As clsBranch
                        If dicCPUs.ContainsKey(cpusku) Then
                            cpuBranch = dicCPUs(cpusku)
                        Else
                            cpuBranch = New clsBranch(cpuProd, cpuRoot, cputl, "", processors, processor, iq.Screens(719), 0, False, "B")
                            dicCPUs.Add(cpusku, cpuBranch)
                        End If

                        Dim path$ = sysPaths(systemSKU)(0)
                        If systemSKU = "646905-421" Then
                            Dim a = ""
                        End If
                        ' If sysPaths(systemSKU).Count > 1 Then Stop

                        If Not systembranch.NameSurf(path$, "All Options/System/Processor") Then
                            Logit("could not locate All Options/System/processor under " & systembranch.DisplayName(English))
                        Else
                            Dim processorCatbranch As clsBranch = iq.Branches(CInt(Split(path, ".").Last))

                            Dim exp$ = PathName(path$)

                            'NB: this graft has a path !! - generally grafts apply to every occurance of the branch 
                            'but CPU's is one scenario where you only want the graft to 'work' in one place (it's the *same* procesor branch on every model in the family !)
                            'not: the SAM CPU may be (is often) grafted to more than one syyem in the family
                            processorCatbranch.Graft(cpuBranch, "CPU Import", path$, errormessages)

                            Dim limits As clsLimit
                            If dicCpuLimitsBySysFamCode.ContainsKey(subfam) Then
                                limits = dicCpuLimitsBySysFamCode(subfam)
                            Else
                                limits = StandardLimits
                            End If

                            'NEW (and important) get the number of CPUS from products.systems
                            'ML added containskey check as it was tripping the import up
                            If SysCpuQTY.ContainsKey(systemSKU) Then limits.Qinstalled = SysCpuQTY(systemSKU)

                            Dim qty As clsQuantity = New clsQuantity(r_worldwide, path$ & "." & cpuBranch.ID.ToString.Trim, cpuBranch, limits.Qinstalled, limits.MinIncr, limits.PrefIncr, True, qtywritecache)

                            'put a 'gives' CPU slot on the system
                            Dim sysPath$ = sysPaths(systemSKU)(0)
                            Dim CPUgiveSlot As clsSlot = New clsSlot(iq.i_slotType_Code("CPU")("GEN_CPU"), systembranch, sysPath$, limits.Qmax, Nothing, New NullableInt, limits.Qmin, limits.Qinstalled, slotwritecache)
                            Dim st As clsSlotType = iq.i_slotType_Code("CPU")("GEN_CPU")
                            'And a TAKES slot on the CPU itself
                            Dim cpuTakeslot As clsSlot = New clsSlot(st, cpuBranch, "", -1, Nothing, New NullableInt(), 1, 0, slotwritecache)
                            '                'and give memory slots... (see import.slotadds)

                            If limits.Qmax > 1 Then
                                Logit("MultiCpu machine " & systemSKU)
                                'for multiCPU machines take the memory gives slots off the chassis and put them on the CPU
                                'although a CPU potentially enables more (or less) slots if using UDIMM vs RDIMM 

                                Dim pth = sysPaths(systemSKU)(0)
                                If systembranch.NameSurf(pth, "chassis") Then

                                    'The CPU (which exists in one 'global' place) - will give lots of different (minor) types of memory slot 
                                    ' at lots of different paths
                                    Dim chassisbranch As clsBranch = iq.Branches(Split(pth, ".").Last)

                                    Dim foundmem As Boolean = False
                                    For Each slot In chassisbranch.slots.Values.ToArray
                                        If slot.Type.MajorCode = "MEM" Then 'locate the memory slots in the chassis.. 
                                            Dim newpath As String = path & "." & cpuBranch.ID.ToString.Trim 'path has been surfed down to the processor (option) branch
                                            'slot.Update(cpuBranch, newpath)

                                            Dim cpuMemEnable As clsSlot = New clsSlot(slot.Type, cpuBranch, newpath, slot.numSlots, slot.notes, slot.slotNum, slot.requiredFill, slot.advisedFill, slotwritecache)
                                            Logit("Duped memory slots from chassis to CPU " & cpusku)
                                            Logit("Path is " & newpath)

                                            If Not todel.Contains(slot) Then todel.Add(slot)
                                            Dim fpn$ = PathName(newpath)
                                            Logit(fpn$)
                                            foundmem = True
                                        End If
                                    Next
                                Else
                                    Stop
                                End If

                                done += 1
                            End If
                        End If
                        'End If
                    End If
                End If
            End If
        Next

        Dim nex As Integer = nonexistent.Count 'non-existent cpus (not in I_Sku)

        da.BulkWrite(con, qtywritecache, "quantity")
        da.BulkWrite(con, slotwritecache, "slot")
        For Each slot In todel
            slot.delete(errormessages)
        Next

        Logit("Finished CPUs", False, True)

    End Sub



    Public Sub HighPerformance()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim Sql$ = "SELECT modelsku from h3.iQ.products.Systems s join h3.iQ.products.options o on s.CPU = o.optsku where o.highperformance = 1"

        If Not iq.i_attribute_code.ContainsKey("Perf") Then
            Dim perfa As clsAttribute = New clsAttribute("Perf", iq.AddTranslation("High Performance", English, "UI", 0, Nothing, 0, False), 0)
        End If

        Dim perf As clsAttribute = iq.i_attribute_code("Perf")
        Dim wc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")


        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, Sql$)

        'high performance systems

        Dim perfsys As Integer = 0

        '   Dim yes As clsTranslation = iq.AddTranslation("Yes", English)
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("ModelSKU")) Then
                Dim system As clsProduct = iq.i_SKU(rdr.Item("ModelSKU"))
                Dim pa As clsProductAttribute = New clsProductAttribute(system, perf, 1, iq.i_unit_code("txt"), Nothing, wc)
                perfsys += 1
            End If
        End While
        rdr.Close()

        'High perforamce options
        Sql$ = "SELECT optsku from h3.iQ.products.options where highperformance = 1"

        rdr = da.DBExecuteReader(con, Sql$)
        Dim perfopt As Integer = 0
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("optSKU")) Then
                Dim opt As clsProduct = iq.i_SKU(rdr.Item("optsku"))
                Dim pa As clsProductAttribute = New clsProductAttribute(opt, perf, 1, iq.i_unit_code("txt"), Nothing, wc)
                perfopt += 1
            End If
        End While
        rdr.Close()

        da.BulkWrite(con, wc, "ProductAttribute")

    End Sub

    Public Function TopRecommendations() As String

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        Dim errorMessages As List(Of String) = New List(Of String)

        'Every family/supplychain has a distinct TRO branch 

        'there are 11 (or so) categories
        Dim sql$ = "SELECT [Category_ID],[Category_textID],[Category_Rank],[Category_Image],l.en as cat_text FROM(h3.[iq].[Products].[Option_Recommendations_Categories]"
        sql$ &= "  join h3.[iQ].[dbo].[Language_Key] l on l.textID = category_textid)"

        'CatID>rank|image|text
        Dim cats As Dictionary(Of Integer, String) = New Dictionary(Of Integer, String)
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)
        Dim tl As clsTranslation
        While rdr.Read
            tl = iq.AddTranslation(CType(rdr.Item("CAT_TEXT"), String), English, "TROct", CInt(rdr.Item("category_rank")), Nothing, 0, False)
            cats.Add(CInt(rdr.Item("category_id")), CStr(rdr.Item("category_rank")) & "|" & CStr(rdr.Item("category_image")) & "|" & CStr(tl.Key))
        End While
        rdr.Close()

        'index the first 3 levels of the tree.. (root/sector/family)  
        Dim famBranches As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)
        iq.RootBranch.index2(famBranches, 1, "tree.1")

        'Make the 'Upsell opportunity' branches (yuck) - one per family, grafted on to every system in that family
        Dim grafts As Integer
        For Each family As clsBranch In famBranches.Values
            If family.childBranches.Count > 0 Then
                Dim UpsellBranch As clsBranch = New clsBranch(Nothing, Nothing, _
                                                              iq.AddTranslation("Upsell Opportunities", English, "UI", 0, Nothing, 0, False), "upsell", _
                                                              iq.AddTranslation("Products", English, "collect", 0, Nothing, 0, False), _
                                                              iq.AddTranslation("Product", English, "collect", 0, Nothing, 0, False), _
                                                              iq.i_screens_code("Base"), 20, False, "U", Nothing)
                For Each sys In family.childBranches.Values
                    grafts += 1
                    sys.Graft(UpsellBranch, "TRO/Upsell", "", errorMessages)
                Next
            End If
        Next


        Dim nbid As Integer = 0
        Dim bwc As DataTable = da.MakeWriteCacheFor(con, "Branch", nbid, True)

        sql$ = "SELECT [SysFamilyName],[Category_ID],[MfrPartNum],[Region],[SupplyChain] FROM h3.[iq].[Products].[Option_Recommendations]"

        Dim qwc As DataTable
        qwc = da.MakeWriteCacheFor(con, "Quantity")


        rdr = da.DBExecuteReader(con, sql$)

        '                        CK           header branch (contains cat branches)        
        Dim troHeads As Dictionary(Of String, clsBranch)
        troHeads = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)

        'Dim upsells As Dictionary(Of String, clsBranch)
        'upsells = New Dictionary(Of String, clsBranch)

        'make a branch for every TRO HEADING and put them in a dictionary, compound keyed by sysfamily|supplychain
        Dim tlpart As clsTranslation = iq.AddTranslation("Option", English, "collect", 0, Nothing, 0, False)
        Dim tlparts As clsTranslation = iq.AddTranslation("Options", English, "collect", 0, Nothing, 0, False)

        Dim hptr As clsTranslation = iq.AddTranslation("Top Recommended", English, "UI", 0, Nothing, 0, False)

        Dim bits() As String
        Dim trobranches As Integer = 0

        Dim dicsc As Dictionary(Of String, String)
        dicsc = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
        dicsc.Add("TV", "top value")
        dicsc.Add("A", "regular models")
        dicsc.Add("SB", "smart buy")
        dicsc.Add("R", "manufacturer refurbished")


        Dim scs As Integer = 0
        Dim duds As Integer = 0

        Dim k$
        While rdr.Read

            Dim sku$ = rdr.Item("mfrpartnum")
            If Not iq.i_SKU.ContainsKey(sku) Then
                Logit(sku & " is not a recognised SKU for TRO")
                duds += 1
            Else

                k$ = LCase(rdr.Item("sysfamilyname"))

                Dim headerbranch As clsBranch
                If troHeads.ContainsKey(k) Then
                    headerbranch = troHeads(k)
                Else
                    'make a new TRO Overall header branch 'HP Top Recommended'
                    headerbranch = New clsBranch(Nothing, Nothing, hptr, "hptop", tlpart, tlparts, Nothing, 10, False, "H", bwc, nbid)
                    troHeads.Add(k, headerbranch)
                    scs += 1
                    'each sysfamily/supplychain will get its own upsells branch (but they share a translation)
                    'nb: The upsell opportunities branch would not usually display (becuase it has no descendant products).. so there is a (cough) feature - to ensure it is always returned as a descendant 

                End If

                Dim catbranch As clsBranch
                bits = Split(cats(rdr.Item("category_id")), "|")

                Dim catbranches = From CB In troHeads(k).childBranches.Values Where CB.Translation.Key = bits(2)

                If catbranches.Any Then ' If Not tros(ck).ContainsKey(rdr.Item("category_id")) Then
                    catbranch = catbranches.First
                Else
                    '  MAKE THE CATEGORY BRANCH                                                          tKey      pic                               order
                    catbranch = New clsBranch(Nothing, headerbranch, iq.Translations(bits(2)), bits(1), tlparts, tlpart, Nothing, bits(0), False, "I", bwc, nbid)
                End If



                Dim product As clsProduct = iq.i_SKU(sku)
                Dim tln As clsTranslation
                tln = iq.AddTranslation(product.SKU, English, "SKU", 10, Nothing, 0, False)

                'the branches name isn't theat important as TROItems display the product.displamyname
                Dim trobranch As clsBranch = New clsBranch(product, catbranch, tln, "", tlparts, tlpart, Nothing, 0, False, "", bwc, nbid)
                trobranches += 1

                'TODO - make qty records to limit them by region
                'Most branches don't have a quantity record 
                'a quantities restrict availablity to the region(s) specified
                Dim qty As clsQuantity = New clsQuantity(iq.i_region_code(rdr.Item("region")), "", trobranch, 0, 1, 1, False, qwc)

            End If

        End While

        rdr.Close()
        Debug.Print(trobranches)

        da.BulkWrite(con, bwc, "branch", , True)
        da.BulkWrite(con, qwc, "quantity")

        Dim gwc As DataTable

        gwc = da.MakeWriteCacheFor(con, "graft")

        'Graft the finished headerBranches onto each system in the family

        Dim fb As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)
        For Each k In famBranches.Keys
            fb.Add(Split(k, "|")(0), famBranches(k))
        Next


        For Each famkey In troHeads.Keys 'header branches Compound key, of the form Famname|sc   (sc may be blank !)

            'Dim sccks = From scCk In scBranches.Keys Where scCk = hbCK Or (Split(hbCK, "|").Last = "" And Left(hbCK, Len(scCk)) = scCk)

            'sysfamilyname > branch
            '    For Each k In famBranches.Keys 'Each scck In sccks 'for each Supply Chain Compound Key in Supply Chain Compound Keys
            '                If LCase(k) = hbCK Or (Split(hbCK, "|")(1) = "" And Left(hbCK, Len(k)) = LCase(k)) Then
            If fb.ContainsKey(famkey) Then
                For Each Sys In fb(famkey).childBranches.Values
                    grafts += 1
                    If Not Sys.Product.isSystem Then Stop
                    Sys.Graft(troHeads(famkey), "TRO import", "", errorMessages, gwc)
                Next
            End If
            'End If
        Next

        da.BulkWrite(con, gwc, "graft")

        con.Close()

        Dim r$ = duds & " Unrecognised options, grafted " & grafts & " option sets " & trobranches & " TRO branches"
        Return r$

    End Function
    Public Sub Receta(ByRef errormessages As List(Of String))

        'Tag every product as receta (or not)

        'walk the entire tree - not crossing systems --graft all receta systems to a top level receta branch

        'We create a 'focus' attribute and add a 'receta' productAttribute (of attribute type focus) to every system and option flagged Receta (in IQ1).
        'We could later add addtional product grouping/focus attributes (smart buy..whatevever) - the front end has a 'matching' session variable which is a list of focuses.. when enabled - products are filtered by their focus
        'Countries !?? (ask dan)  have a 'focus' - and root branch

        Dim FocusAttrib As clsAttribute

        FocusAttrib = iq.i_attribute_code("focus")


        'Grab ALL the recta Skus (systems and options) into a list
        Dim rs As List(Of String) = New List(Of String) 'RecetaSkus
        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net\charliel,8484\;Initial Catalog=iQuote2; password=wainwright; user id=editor; Connection Timeout=10;")
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, "Select ModelSKU FROM iq.products.systems WHERE recetaSystem=1 UNION SELECT OptSKU FROM iq.products.options WHERE receta=1")
        While rdr.Read
            rs.Add(rdr.Item(0))
        End While
        rdr.Close()

        con.Close()
        con = da.OpenDatabase()

        'Add a Recta attribute to every product in the list
        Dim rt As clsTranslation = iq.AddTranslation("receta", English, "UI", 0, Nothing, 0, False)
        Dim product As clsProduct
        Dim pa As clsProductAttribute
        Dim textUnit As clsUnit = iq.i_unit_code("txt")
        Dim pawc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")
        For Each sku In rs
            If iq.i_SKU.ContainsKey(sku) Then
                product = iq.i_SKU(sku)
                If Not product.i_Attributes_Code.ContainsKey("focus") Then
                    pa = New clsProductAttribute(product, FocusAttrib, 1, textUnit, rt, pawc)
                Else
                    '     Beep()
                End If
            End If
        Next
        da.BulkWrite(con, pawc, "ProductAttribute")

        con.Close()

        'Dim systemBranches As Dictionary(Of String, IQ.clsBranch) = New Dictionary(Of String, IQ.clsBranch)
        'Now get ALL the 'first' SKUD branches (which will be systems)
        'systemBranches = iq.RootBranch.SKUdDescendants(Nothing, "tree.1", True, False, False, False)

        'We don't want supply chains in the receta tree -so we construct a slightly simiplified 'deep copy' of the top two levels of the tree (sysType,Family - then graft every system in
        'The actual displayed systems are (additonally) filtered in real-time against the 'receta' attribute (see HideReasons)

        Dim recetaRoot As New clsBranch(Nothing, Nothing, _
                                        iq.AddTranslation("Receta", English, "UI", 0, Nothing, 0, False), "", _
                                        iq.AddTranslation("Systems", English, "collect", 0, Nothing, 0, False), _
                                        iq.AddTranslation("System", English, "collect", 0, Nothing, False, False), _
                                        iq.i_screens_code("Servers"), 1, False, "S")


        con = da.OpenDatabase()
        Dim gwc As DataTable = da.MakeWriteCacheFor(con, "graft")
        Dim pwc As DataTable = da.MakeWriteCacheFor(con, "Prune")

        Dim rcat As clsBranch
        Dim rfam As clsBranch
        For Each cat In iq.RootBranch.childBranches.Values

            Dim kk = cat.DisplayName(English)
            rcat = New clsBranch(cat.Product, recetaRoot, cat.Translation, cat.Picture, cat.CollectiveNoun, cat.collectiveNounSingular, cat.Matrix, cat.order, False, "S")
            For Each fam In cat.childBranches.Values
                rfam = New clsBranch(fam.Product, rcat, fam.Translation, fam.Picture, fam.CollectiveNoun, fam.collectiveNounSingular, fam.Matrix, fam.order, False, "B")
                For Each sc In fam.childBranches.Values 'these are the supply chains - we want to skip 
                    For Each sys In sc.childBranches.Values
                        If sys.Product.i_Attributes_Code.ContainsKey("focus") Then
                            rfam.Graft(sys, "receta", "", errormessages, gwc)  'graft each system into the (new, receta) family

                            ' below is an attempt to construct a pre-compiled Receta tree - which is not without its merits.. (Would be faster, giving a smaller tree and removing the realtime checks - which is signinicant when counting options)
                            'However - it makes the Receta attribute confusing/redundant - 'flagging' things as receta is probably more intuitive for the product team (than grafting) - although new families will need to be grafted.

                            ' ''find ALL the skud products (options) and their paths under this receta system
                            'Dim syspath$ = "tree." & Trim(recetaRoot.ID) & "." & Trim(rcat.ID) & "." & Trim(rfam.ID)
                            'Dim all As Dictionary(Of String, clsBranch) = sys.SKUdDescendants(Nothing, syspath, True, True, False, False)

                            'Dim Keep As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)
                            'For Each kvp In all
                            '    If kvp.Value.Product IsNot Nothing Then
                            '        If kvp.Value.Product.i_Attributes_Code.ContainsKey("receta") Then
                            '            Keep.Add(kvp.Key, kvp.Value)
                            '        End If
                            '    End If
                            'Next

                            'Dim prune As clsPrune = Nothing
                            'Dim prunelist As List(Of String) = sys.SeverePrune(syspath, Keep)
                            'For Each path In prunelist
                            '    prune = New clsPrune(path, New NullableInt, "Receta", pwc)
                            'Next
                        End If
                    Next
                Next
            Next
        Next

        da.BulkWrite(con, gwc, "Graft")
        da.BulkWrite(con, pwc, "Prune")

        con.Close()

        iq.RootBranch.Graft(recetaRoot, "Receta", "", errormessages)

    End Sub

    Public Sub LoyaltyPoints()


        'source (Iq1)
        Dim scon As SqlClient.SqlConnection = da.OpenDatabase()

        Dim tcon As SqlClient.SqlConnection = da.OpenDatabase 'target IQ2
        Dim wc As DataTable = da.MakeWriteCacheFor(tcon, "Points")

        Dim ix_lp As Dictionary(Of String, clsScheme) = New Dictionary(Of String, clsScheme)(StringComparer.CurrentCultureIgnoreCase) 'Build an index of the (existsing) 
        Dim scheme As clsScheme
        For Each scheme In iq.Schemes.Values
            ix_lp.Add(scheme.compoundKey, scheme)  'Buid
        Next

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(scon, "Select MfrPartnum,points,country,startdate,enddate from h3.iq.products.loyaltypoints where enddate>getdate()")

        Dim bc As clsTranslation = iq.AddTranslation("Blue Carpet", English, "schemes", 0, Nothing, 0, False)

        Dim product As clsProduct = Nothing

        Dim cc$ 'Country Code
        Dim ck$  'Compound Key (for determining distinct schemes)

        Dim dudCountries As List(Of String) = New List(Of String)
        While rdr.Read

            If Not iq.i_SKU.ContainsKey(rdr.Item("MfrPartNum")) Then
                'If rdr.Item("mfrpartnum") = "C9299A" Then Stop
                Logit("Part " & rdr.Item("mfrpartnum") & " does not exist")
            Else
                cc$ = rdr.Item("Country")
                If Not iq.i_region_code.ContainsKey(cc$) Then
                    If Not dudCountries.Contains(cc) Then
                        dudCountries.Add(cc$)
                        Logit("Country " & cc$ & " does not exists")
                    End If
                Else
                    Dim region As clsRegion = iq.i_region_code(cc$)
                    ck$ = region.ID & "^" & rdr.Item("startdate") & "^" & rdr.Item("enddate")

                    ' We create a scheme for each distinct Country,StartDate,Enddate combo
                    If ix_lp.ContainsKey(ck$) Then
                        scheme = ix_lp(ck$)
                    Else
                        If Not iq.i_scheme_code.ContainsKey("BC") Then

                            scheme = New clsScheme("BC", bc, region, rdr.Item("startdate"), rdr.Item("enddate"))
                            ix_lp.Add(ck$, scheme) ' add it to the index (we only use locally for the import)
                        End If
                    End If

                    product = iq.i_SKU(rdr.Item("MfrPartNum"))

                    Dim row As DataRow = wc.NewRow
                    wc.Rows.Add(row)
                    row.Item("Fk_Product_id") = product.ID
                    row.Item("Fk_scheme_id") = scheme.ID
                    row.Item("Points") = rdr.Item("Points")
                End If
            End If
        End While

        rdr.Close()
        scon.Close()

        Logit("imported " & wc.Rows.Count & " points sets into " & iq.Schemes.Count & " schemes")
        Logit("All done", 0, True)


        da.BulkWrite(tcon, wc, "Points")


        tcon.Close()



    End Sub


    Public Function PowerSizing(con As SqlClient.SqlConnection) As String


        Dim watts As clsSlotType
        If iq.i_slotType_Code.ContainsKey("PWR") AndAlso iq.i_slotType_Code("PWR").ContainsKey("W") Then
            watts = iq.i_slotType_Code("PWR")("W")
        Else
            watts = New clsSlotType("PWR", "W", iq.AddTranslation("Watts", English, "units", 0, Nothing, 0, False))
        End If

        Dim dt As New DataTable
        dt = da.MakeWriteCacheFor(con, "Slot")

        Dim rdr As SqlClient.SqlDataReader

        rdr = da.DBExecuteReader(con, "SELECT [rowID],[SysFamilyName],[powerMin],[powerMax]  from " & server$ & "[iq].[Products].[HPPS_SystemFamilies] order by sysfamilyname")

        '                                         sysfamilyname      path>branch
        Dim FamilyBranches As Dictionary(Of String, Pair) = iq.Branches(1).findFamilyBranches("tree." & Trim(iq.Branches(1).ID))

        'make the 'takes' watts slots for every system - which are for the Motherboard

        Dim locations As Dictionary(Of String, clsBranch)

        Dim systemBranch As clsBranch
        Dim systempath$

        Dim takes As Integer

        Dim err$ = ""

        Dim familyName$

        Dim aslot As clsSlot
        While rdr.Read
            familyName = rdr.Item("sysfamilyname")
            If FamilyBranches.ContainsKey(familyName) Then
                Dim path$ = FamilyBranches(familyName).First
                Dim familyBranch As clsBranch = FamilyBranches(familyName).Second
                locations = familyBranch.findSystemBranches(path$) 'find the locations of all the system branches (under the family branch)

                For Each sysloc In locations
                    systempath$ = sysloc.Key
                    systemBranch = sysloc.Value
                    aslot = New clsSlot(watts, systemBranch, systempath$, -rdr.Item("powermax"), Nothing, New NullableInt(), 0, 0, dt)
                    takes += 1
                Next
            Else
                err$ = err$ & "Skipped family " & familyName & "<br/>"
            End If

        End While
        rdr.Close()

        rdr = da.DBExecuteReader(con, "SELECT [rowID],[SysFamilyName],optsku as [mfrPartNum],ISNULL(HPPS_Options.[powerMin],options.powermin),ISNULL(HPPS_Options.[powerMax],options.powermax)  from " & server$ & "[iq].[Products].[HPPS_Options] right outer join " & server$ & "[iq].[Products].[Options] on options.optsku = mfrPArtNum WHERE (ISNULL(HPPS_Options.[powerMax],options.powermax) is not null or ISNULL(HPPS_Options.[powerMax],options.powermax) is not null) and opttype<>'PSUm' order by sysfamilyname")

        'make the 'takes' (watts)  slot for every option
        'NOTE these 'TAKES' slots have paths - they take different amounts of power depending on which stsyems they are installed in

        Dim consumingParts As List(Of clsProduct) = New List(Of clsProduct)
        Dim partno As String
        While rdr.Read
            partno = rdr.Item("mfrPartNum")
            If iq.i_SKU.ContainsKey(partno) Then
                consumingParts.Add(iq.i_SKU(partno))
            End If
        End While
        rdr.Close()

        'Dim MonsterIndex As Dictionary(Of clsProduct, List(Of String)) 'A list of all the paths at which a product appears
        'MonsterIndex = New Dictionary(Of clsProduct, List(Of String))
        'iq.RootBranch.IndexProductPaths("tree." & Trim(iq.RootBranch.ID), MonsterIndex, False, True, consumingParts)  ' 5 SECS !

        'locate all the branches carrying this product
        Dim locs As New Dictionary(Of clsProduct, List(Of clsBranch))
        For Each b In iq.Branches.Values.ToArray  'to array is a (bad) fix for a collection modified error(which is almost certainly a double ajax call)
            If b.Product IsNot Nothing Then
                If consumingParts.Contains(b.Product) Then
                    If Not locs.ContainsKey(b.Product) Then locs.Add(b.Product, New List(Of clsBranch))
                    locs(b.Product).Add(b)
                End If
            End If
        Next

        Dim optionProduct As clsProduct
        Dim optPaths As New Dictionary(Of String, List(Of String))(StringComparer.CurrentCultureIgnoreCase)

        Dim branch As clsBranch

        Dim ofn As String = ""
        Dim fn As String

        Dim invalids As New List(Of String)

        rdr = da.DBExecuteReader(con, "SELECT [SysFamilyName],optsku as [mfrPartNum],ISNULL([HPPS_Options].[powerMin],options.powermin) as PowerMin,isnull([HPPS_Options].[powerMax] ,options.powermax) as PowerMax from " & server$ & "iq.products.options left outer join " & server$ & "[iq].[Products].[HPPS_Options] on optsku=mfrPartNum where ISNULL([HPPS_Options].[powerMin],options.powermin) is not null or ISNULL([HPPS_Options].[powerMax],options.powermax) is not null order by sysfamilyname")


        Dim sums As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer) 'We need to work out the average consumption per opttype
        Dim counts As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)

        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("mfrpartnum")) Then 'for partial imports
                Dim optionSKU As String = rdr.Item("mfrPartnum")

                optionProduct = iq.i_SKU(optionSKU)

                If Not optionProduct.isSystem Then 'There are soms systems  in here !

                    Dim skip As Boolean = False
                    If optionProduct.i_Attributes_Code.ContainsKey("optType") Then
                        If {"PSU", "PSUm"}.Contains(optionProduct.i_Attributes_Code("optType")(0).Translation.text(English)) Then skip = True
                    End If

                    If Not skip Then
                        If IsDBNull(rdr.Item("sysfamilyname")) Then
                            'row applies *wherever*  this part appears .. trouble is it can appear on many branches
                            If locs.ContainsKey(optionProduct) Then
                                For Each b In locs(optionProduct)
                                    aslot = New clsSlot(watts, b, "", -rdr.Item("powermax"), Nothing, New NullableInt(), 0, 0, dt)  'at each distinct branch (carrying this product) make a 'global' slot
                                    Dim opttype As String = optionProduct.i_Attributes_Code("opttype")(0).Translation.text(English)
                                    If Not sums.ContainsKey("NONE^" & opttype) Then sums.Add("NONE^" & opttype, 0)
                                    If Not counts.ContainsKey("NONE^" & opttype) Then counts.Add("NONE^" & opttype, 0)
                                    sums("NONE^" & opttype) += rdr.Item("powermax")
                                    counts("NONE^" & opttype) += 1

                                Next
                            End If
                        Else
                            fn = rdr.Item("sysfamilyname")
                            If FamilyBranches.ContainsKey(fn) Then
                                Dim fampath$ = FamilyBranches(fn).First
                                Dim familyBranch As clsBranch = FamilyBranches(fn).Second

                                'find the path of every option under this family
                                If fn <> ofn Then
                                    optPaths.Clear()  'Important !! (or they'd just build up in here!)
                                    familyBranch.SkuPaths(optPaths, "", True)
                                    ofn = fn
                                End If

                                If optPaths.ContainsKey(optionProduct.SKU) Then

                                    For Each optionPath In optPaths(optionProduct.SKU) 'contains every path of this option under the family
                                        Dim optbranch As clsBranch = iq.Branches(Split(optionPath, ".").Last)

                                        aslot = New clsSlot(watts, optbranch, fampath & optionPath, -rdr.Item("powermax"), Nothing, New NullableInt(), 0, 0, dt)
                                        takes += 1

                                        Dim opttype As String = optionProduct.i_Attributes_Code("opttype")(0).Translation.text(English)
                                        If Not sums.ContainsKey(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) & "^" & opttype) Then sums.Add(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) & "^" & opttype, 0)
                                        If Not counts.ContainsKey(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) & "^" & opttype) Then counts.Add(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) & "^" & opttype, 0)
                                        sums(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) & "^" & opttype) += rdr.Item("powermax")
                                        counts(familyBranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English) & "^" & opttype) += 1

                                    Next

                                End If
                            Else
                                '   Beep() 'invalid family name
                                NoOp()
                                If Not invalids.Contains(fn) Then
                                    invalids.Add(fn)
                                    Logit("HPPSOptions references a sysFamilyname '" & fn & "' which does not exist")
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End While
        rdr.Close()


        Dim made As Integer = 0
        Dim cannyDo As Integer

        Dim tlapprox As clsTranslation = iq.AddTranslation("* Estimated/typical power consumption", English, "U", 1, Nothing, 0, False)
        'Fill the power sizing gaps
        'For EVERY option branch - make a takes watts slot based on the average consumption of the opttype in the family

        Dim neededList As Dictionary(Of Int32, String) = New Dictionary(Of Integer, String)()

        For Each branch In iq.Branches.Values

            If branch.Product IsNot Nothing AndAlso branch.Product.hasSKU AndAlso Not branch.Product.isSystem Then

                'it's an option...
                Dim needed As Boolean = True
                If branch.Product.i_Attributes_Code.ContainsKey("opttype") Then
                    Dim opttype As String = branch.Product.i_Attributes_Code("opttype")(0).Translation.text(English)
                    If opttype <> "PSU" Then
                        For Each slot In branch.slots.Values
                            If slot.Type.MinorCode = "W" Then
                                needed = False
                                Exit For
                            End If
                        Next

                        If needed Then
                            'Make a slot takes slot - based on the average max consumption for the opt type

                            neededList.Add(branch.ID, "")


                        End If
                    Else
                        '    Stop
                    End If
                End If
            End If
        Next
        iq.Branches(1).indexProductBranchesByPath("tree", True, neededList)
        'Very slow, this needs improving but is a quick fix to try and match up a family if known (on the power record)

        For Each n In neededList
            Dim fambranchl = FamilyBranches.Where(Function(fb) fb.Value.First = Left(n.Value, Len(fb.Value.First)))
            fn = "NONE"
            If fambranchl.Count > 0 Then
                Dim fambranch = fambranchl(0).Value.Second
                fn = fambranch.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English)
            End If
            branch = iq.Branches(n.Key)
            Dim opttype = branch.Product.i_Attributes_Code("opttype")(0).Translation.text(English)


            If sums.ContainsKey(fn & "^" & opttype) Then
                Dim AvgTakes As clsSlot = New clsSlot(watts, branch, "", -sums(fn & "^" & opttype) / counts(fn & "^" & opttype), tlapprox, New NullableInt, 0, 0, dt)
                made += 1
            Else
                If sums.ContainsKey("NONE^" & opttype) Then
                    Dim AvgTakes As clsSlot = New clsSlot(watts, branch, "", -sums("NONE^" & opttype) / counts("NONE^" & opttype), tlapprox, New NullableInt, 0, 0, dt)
                    made += 1
                Else
                    cannyDo += 1
                End If
            End If
        Next



        'make the 'Gives' WATTS slots for the power supplies - ML added PSUm watts in this and excluded from the takes
        ''rdr = da.DBExecuteReader(con, "SELECT optSKU,unitqty from " & server$ & "[iq].products.options where opttype='PSU' and active=1")
        rdr = da.DBExecuteReader(con, "SELECT optSKU,unitqty from " & server$ & "[iq].products.options where opttype='PSU' and opttype2 is null and active=1	union SELECT mfrpartnum,[HPPS_Options].powerMax from " & server$ & "[iq].[Products].[HPPS_Options] inner join h3.[iq].[Products].options on options.optsku= mfrpartnum where opttype='PSUm'")

        Dim PSUs As List(Of clsProduct) = New List(Of clsProduct)
        While rdr.Read
            Dim psuSku As String = CType(rdr.Item("optsku"), String)
            If iq.i_SKU.ContainsKey(psuSku) Then
                PSUs.Add(iq.i_SKU(psuSku))
            End If
        End While
        rdr.Close()

        locs.Clear()
        For Each b In iq.Branches.Values
            If b.Product IsNot Nothing Then
                If PSUs.Contains(b.Product) Then
                    If Not locs.ContainsKey(b.Product) Then locs.Add(b.Product, New List(Of clsBranch))
                    locs(b.Product).Add(b)
                End If
            End If
        Next

        Dim gives As Integer

        'For Each psu In PSUs
        rdr = da.DBExecuteReader(con, "SELECT optSKU,unitqty from " & server$ & "[iq].products.options where opttype='PSU' and active=1	union SELECT mfrpartnum,[HPPS_Options].powerMax from " & server$ & "[iq].[Products].[HPPS_Options] inner join h3.[iq].[Products].options on options.optsku= mfrpartnum where opttype='PSUm'")

        Dim done As List(Of clsBranch) = New List(Of clsBranch)
        While rdr.Read


            Dim psusku As String = rdr.Item("optsku")
            If iq.i_SKU.ContainsKey(psusku) Then

                'If Left$(psusku, 3) = "###" Then Stop ' this is good
                Dim psu As clsProduct = iq.i_SKU(psusku)

                If locs.ContainsKey(psu) Then
                    If Not IsDBNull(rdr.Item("unitqty")) Then
                        Dim qty As Integer = rdr.Item("unitqty")

                        For Each branch In locs(psu) 'the same power supply is attached to many branches
                            aslot = New clsSlot(watts, branch, "", qty, Nothing, New NullableInt(), 0, 0, dt)
                            gives += 1
                        Next

                    End If
                End If
            End If
        End While
        rdr.Close()

        Dim rc As Integer = dt.Rows.Count

        da.BulkWrite(con, dt, "slot")

        PowerSizing = err$ & "<p>made " & gives & " gives and " & takes & " takes WATT slots. "

        Logit("Completed powersizing import", False, True)

    End Function



    'Public Function ExtText()
    '    Return ExtText(Nothing, True, Nothing, 0, Nothing)
    'End Function


    Public Function ExtText(prod As clsProduct, Inserting As Boolean, AttribWriteCache As DataTable, ByRef nextkey As Int32, TlWC As DataTable, xtdic As Dictionary(Of String, List(Of String)), famlocs As Dictionary(Of String, String)) As String

        'If AttribWriteCache Is Nothing Then AttribWriteCache = da.MakeWriteCacheFor(da.OpenDatabase(), "ProductAttribute")
        'If TranslationWriteCache Is Nothing Then
        '    TranslationWriteCache = da.MakeWriteCacheFor(da.OpenDatabase(), "Translation")
        '    nextkey = clsTranslation.NextKey()
        'End If

        'Static FamilyBranches As Dictionary(Of String, Pair)

        'If FamilyBranches Is Nothing Then
        '    FamilyBranches = iq.RootBranch.findFamilyBranches("tree." & Trim(iq.RootBranch.ID))  'One time conversion of all family branch names to 'FamMajor' attributes
        'End If

        Dim err$ = ""
        '    Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        ' Dim PAWC As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute") 'Allows us to bulk write (many times faster than lots of INSERTS


        Dim sku As String
        Dim product As clsProduct = Nothing
        Dim xText As clsProductAttribute = Nothing

        Dim noteAtt As clsAttribute
        If Not iq.i_attribute_code.ContainsKey("xText") Then
            noteAtt = New clsAttribute("xText", iq.AddTranslation("Note", English, "UI", 0, tlwc, nextkey, False), 0)
        Else
            noteAtt = iq.i_attribute_code("xText")
        End If

        Dim HideAtt As clsAttribute
        If Not iq.i_attribute_code.ContainsKey("HideF") Then
            HideAtt = New clsAttribute("HideF", iq.AddTranslation("Hide in families", English, "UI", 0, TlWC, nextkey, False), 0)
        Else
            HideAtt = iq.i_attribute_code("HideF")
        End If

        Dim showAtt As clsAttribute
        If Not iq.i_attribute_code.ContainsKey("ShowF") Then
            showAtt = New clsAttribute("ShowF", iq.AddTranslation("Show (only) in families", English, "UI", 0, TlWC, nextkey, False), 0)
        Else
            showAtt = iq.i_attribute_code("ShowF")
        End If

        Dim dicSeverity As Dictionary(Of String, Integer)
        dicSeverity = New Dictionary(Of String, Integer)(StringComparer.CurrentCultureIgnoreCase)
        dicSeverity.Add("NULL", 4)
        dicSeverity.Add("NOTE", 4)
        dicSeverity.Add("MANDATORY", 7)
        dicSeverity.Add("WARNING", 7)
        Dim hide As clsProductAttribute = Nothing
        Dim show As clsProductAttribute = Nothing

        Dim notes As Integer

        ' Dim mt As Object


        Dim su$, hu$
        'While rdr.Read

        If xtdic.ContainsKey(prod.SKU) Then
            For Each line In xtdic(prod.SKU) 'all lines peratining to a SKu are in a list

                Dim bits() As String = Split(line, "^")
                Dim skutext As String = bits(0)
                Dim sysFamilyShowText As String = bits(1)
                Dim sysFamilyHideText As String = bits(2)
                Dim msgType As String = bits(3)

                'sku$ = rdr.Item("SKU")
                '                If iq.i_SKU.ContainsKey(sku) Then
                product = iq.i_SKU(prod.SKU)
                Dim textUnit As clsUnit = iq.i_unit_code("txt")


                Dim tl As clsTranslation = iq.AddTranslation(skutext, English, "xText", 0, TlWC, nextkey, True)
                xText = New clsProductAttribute(product, noteAtt, dicSeverity(msgType), textUnit, tl, AttribWriteCache, Not Inserting)
                notes += 1

                'we have to make a showUnder and hideUnder attribute for *every* xText - so that they stay 'in synch' with the xTexts themselves - this is all becuase a single product can have multiple external texts
                su$ = IIf(IsDBNull(sysFamilyShowText), "", "SysFamilyShowText")
                show = New clsProductAttribute(product, showAtt, 0, textUnit, iq.AddTranslation(su$, English, "shows", 0, TlWC, nextkey, False), AttribWriteCache, Not Inserting)

                hu$ = IIf(IsDBNull(sysFamilyHideText), "", "sysFamilyHideText")
                hide = New clsProductAttribute(product, HideAtt, 0, textUnit, iq.AddTranslation(hu$, English, "hides", 0, TlWC, nextkey, False), AttribWriteCache, Not Inserting)

                'Else
                'err$ &= "Invalid SKU " & sku
                'End If
            Next
        End If

        '  End While

        'da.BulkWrite(con, TranslationWriteCache, "Translation")
        'da.BulkWrite(con, AttribWriteCache, "ProductAttribute")

        '        rdr.Close()
        ' con.Close()



        Return err$ & "<p/>Added " & notes & " Notes"

    End Function

    Public Function listprices(con As SqlClient.SqlConnection, countryCode As String) As String


        Logit("Importing list prices", False, False)

        iq.PNAdown = True 'STOP the webservices

        'NB: - this is not 'thread safe' - we MUST make sure than nothing else creates prices at the same time
        'ie. make the webservices unavailable

        Dim nextVid As Integer = 0
        Dim nextPid As Integer = 0

        con.Close()
        con = da.OpenDatabase()

        Dim PriceWriteCache As New DataTable
        PriceWriteCache = da.MakeWriteCacheFor(con, "Price", nextPid, True)
        Dim vwc As DataTable = da.MakeWriteCacheFor(con, "variant", nextVid, True)

        Dim rdr As SqlClient.SqlDataReader
        'iq.products.hplp
        'rdr = da.dbexecuteReader(con, "SELECT oursku as pn,lp,curr FROM " & DSserver & "datastore.products.hplp_lite")

        Dim con2 As SqlClient.SqlConnection = da.OpenDatabase("Data Source=iquote2.channelcentral.net,8484; user id=editor;Initial Catalog=pricing; password=wainwright; connection timeout=35;")
        Dim sql$ = "SELECT Mfrpartnum,Currency,country,listprice FROM pricing.products.hpPriceList"
        If countryCode <> "" Then sql$ &= " WHERE country='" & countryCode & "' "
        sql$ &= " ORDER BY country,currency"
        rdr = da.DBExecuteReader(con2, sql$) 'datastore.products.hplp_lite")

        Logit("Importing HP list prices for " & countryCode & " " & Now.ToString, False)

        Dim bpn$ 'Base part number #variant stripped

        Dim SKUVariant As clsVariant = Nothing
        Dim Product As clsProduct = Nothing
        Dim Currency As clsCurrency = Nothing
        Dim Price As NullablePrice = Nothing

        Dim rows As Integer = 0
        Dim Updated As Integer = 0
        Dim Added As Integer = 0
        Dim Unchanged As Integer = 0
        Dim unknown As Integer = 0
        Dim dupes As Integer = 0

        Dim newcurrency As clsCurrency
        If Not iq.i_currency_code.ContainsKey("CHF") Then newcurrency = New clsCurrency("CHF", Nothing, iq.AddTranslation("Swiss Franc", English, "currencies", 0, Nothing, 0, False), "Fr", 1, Nothing)



        Dim aPrice As clsPrice

        While rdr.Read

            bpn$ = rdr.Item("MfrPartNum")
            bpn$ = Split(bpn$, "#")(0)  'take the part preceeding any #

            Dim cc$
            cc$ = rdr.Item("currency")

            If Not iq.i_currency_code.ContainsKey(cc$) Then
                If cc$ <> "CHF" And cc$ <> "MXN" Then
                    Logit("unknown currency:" & cc$)
                End If

            Else
                Currency = iq.i_currency_code(cc$)

                ' If cc$ = "GBP" Then Stop

                If Not iq.i_SKU.ContainsKey(bpn$) Then
                    '  Logit("Unknown part number '" & bpn$ & "'")
                    unknown += 1
                Else

                    Product = iq.i_SKU(bpn$)

                    '''' <summary>Provides access to a List of sellerChannel specific variants of the product</summary>
                    '    Property Variants As Dictionary(Of clsChannel, List(Of clsVariant))

                    SKUVariant = Nothing

                    Dim country As clsRegion
                    country = iq.i_region_code(rdr.Item("country"))

                    'we use UK (ask Dan!) - so map accross
                    'HP use GB - the correct ISO code IS GB
                    ' If rdr.Item("country") = "GB" Then country = iq.i_region_code("UK") '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    If Product.i_Variants IsNot Nothing Then
                        If Product.i_Variants.ContainsKey(HP) Then 'Get the HP (sellers) variant for this country
                            For Each v In Product.i_Variants(HP)
                                If v.Region Is country Then
                                    SKUVariant = v 'Variants *can* have a region (precisely to allow list prices per region)
                                End If
                            Next
                        End If
                    End If

                    If SKUVariant Is Nothing Then 'there was no HP variant for this country - make one
                        SKUVariant = New clsVariant("list", Product, HP, rdr.Item("MfrPartnum"), "List Price", "", "", country, False, vwc, nextVid)
                    End If

                    aPrice = Nothing
                    'SKUvariant is now the HP variant for the correct region (country for now)
                    If SKUVariant.i_prices.ContainsKey(Everyone) Then
                        If SKUVariant.i_prices(Everyone).ContainsKey(Currency) Then
                            aPrice = SKUVariant.i_prices(Everyone)(Currency)
                        End If
                    End If

                    If aPrice Is Nothing Then
                        'the HP variant for 'eveyone' in this region - DIDN'T have a price record - make one
                        'create a new price - the price carries the currency - but the variant carries the region
                        aPrice = New clsPrice(SKUVariant, Everyone, New NullablePrice(rdr.Item("listprice"), Currency, True), "I", PriceWriteCache, nextPid)

                        Added += 1
                    Else
                        If aPrice.Price.value = CDec(rdr.Item("listprice")) Then
                            Unchanged += 1 'no need to do anything
                        Else
                            aPrice.Price.value = rdr.Item("listprice")
                            aPrice.lastUpdated = Now
                            aPrice.lastRequested = Now

                            aPrice.Update()
                            Updated += 1

                        End If
                    End If
                End If
            End If

            rows += 1
        End While
        rdr.Close()
        con2.Close()

        da.BulkWrite(con, PriceWriteCache, "Price")
        da.BulkWrite(con, vwc, "Variant")
        PriceWriteCache = Nothing


        For Each c In iq.Channels.Values
            c.pricesLoadedFor.Clear()  'force a relaod of pricing 
        Next


        Logit("dupes " & dupes)
        Dim l$
        l$ = "Import listprices - Processed:" & rows & " Updated:" & Updated & " Checked (unchanged):" & Unchanged & " Unknown (not in iQuoute):" & unknown & " Added:" & Added

        iq.PNAdown = False

        Return l$


    End Function

    Public Sub LoadAbbreviations(con As SqlClient.SqlConnection)

        dicAbbreviations = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)

        Dim sql$
        sql$ = "SELECT CODE,TRANSLATION from " & server$ & "[iq].dbo.abbreviations"

        Dim rdr As SqlClient.SqlDataReader

        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            If Not dicAbbreviations.ContainsKey(rdr.Item("code")) Then
                dicAbbreviations.Add(LCase(rdr.Item("code")), rdr.Item("translation"))
            End If

        End While
        rdr.Close()

    End Sub

    Public Structure pciStruct
        Dim tech As String
        Dim connector As Integer
        Dim speed As Integer
        Dim w As Integer
        Dim h As Integer
        Dim generation As String 'can be blank - obsolete (we think)
        Dim dedicated As Boolean
        Dim fullText As String

    End Structure

    Public Function DefaultCulture(CountryCode$) As String

        'returns a .net culture code for the specified country code (to be stored in the countries table... only used at import)

        If CountryCode = "BE" Then
            Return "NL"
        Else
            Return CountryCode
        End If

    End Function

    Public Sub updateQuoteDescriptionsAndTotals(ByRef con As SqlClient.SqlConnection, dicquotes As Dictionary(Of String, clsQuote))

        Dim sql$

        sql$ = Space$(8192) ' define a large buffer to avoid string concatenation (which is slow)
        Dim c$
        Dim PriceBefore As NullablePrice

        'Update the descriptions and price, execute the SQL in large blocks for improved efficiency (could be faster with some fancy/dancy merge/stored procedure
        Dim ip As Integer = 0
        Dim cp As Integer = 0
        Dim p As Integer = 1
        For Each q In dicquotes.Values
            PriceBefore = q.QuotedPrice
            q.Saved = True
            q.Locked = True
            'updates the quote and saves it in db
            q.Update(Nothing, False)



        Next

    End Sub
    Public Sub RefCodes(con As SqlClient.SqlConnection)

        'HP Product COdes (used for avalanche)

        Dim sql$
        Dim tlwc As DataTable

        Dim prodref As clsAttribute
        If Not iq.i_attribute_code.ContainsKey("ProdRef") Then
            prodref = New clsAttribute("ProdRef", iq.AddTranslation("Product Reference code", English, "UI", 0, Nothing, 0, False), 0)
        Else
            prodref = iq.i_attribute_code("ProdRef")
        End If


        sql$ = "DELETE FROM PRODUCTATTRIBUTE WHERE FK_ATTRIBUTE_ID=" & prodref.ID
        da.DBExecutesql(con, sql$)

        'remove all existing prodref codes
        sql$ = "DELETE FROM Translation WHERE [group]='rc'"
        da.DBExecutesql(con, sql$)
        iq.LoadTranslations(con)


        'remove all existing prodref attributes from all products in the OM
        For Each p In iq.Products.Values
            If p.i_Attributes_Code.ContainsKey("Prodref") Then
                For Each pa In p.i_Attributes_Code("ProdRef")
                    p.Attributes.Remove(pa.ID)
                Next
                p.i_Attributes_Code.Remove("ProdRef")
            End If
        Next

        'First make a translation for every distinct RefCode (do this a a first pass so we can bulk-write them
        tlwc = da.MakeWriteCacheFor(con, "translation")
        Dim nextkey As Integer = clsTranslation.NextKey()

        'sql$ = "Select distinct [Manuf7] from " & server$ & "[channelcentral].[products].[Hierarchy] where ISNULL (Manuf7,'')<>''"
        sql$ = "Select distinct cast([Manuf7] as nvarchar) as manuf7  from h3.[channelcentral].[products].[Hierarchy]"

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, sql$)

        While rdr.Read
            If Not IsDBNull(rdr.Item("manuf7")) Then
                iq.AddTranslation(rdr.Item("manuf7"), English, "refcode", 0, tlwc, nextkey, False)
            End If
        End While
        rdr.Close()

        da.BulkWrite(con, tlwc, "translation")

        iq.LoadTranslations(con)  'now we *must* load the translations up again from disk - so that they have their ID's

        'now we can import the options's refcodes
        sql$ = "SELECT [UPCNum],[Manuf7] from " & server$ & "[channelcentral].[products].[Hierarchy] where ISNULL (Manuf7,'')<>''"

        Dim pawc As DataTable = da.MakeWriteCacheFor(con, "productAttribute")

        rdr = da.DBExecuteReader(con, sql$)

        Dim Product As clsProduct
        Dim textUnit As clsUnit = iq.i_unit_code("txt")
        Dim duds As Integer = 0
        Dim skipped As Integer = 0

        Dim tl As clsTranslation


        Dim anAttribute As New clsProductAttribute
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("UPCNum")) Then
                Product = iq.i_SKU(rdr.Item("UPCnum"))
                If Not Product.i_Attributes_Code.ContainsKey("ProdRef") Then
                    tl = iq.EnglishIndex(rdr.Item("manuf7"), "refcode")
                    If tl Is Nothing Then Stop
                    anAttribute = New clsProductAttribute(Product, prodref, 0, textUnit, tl, pawc)
                Else
                    skipped += 1
                End If
            Else
                '     Stop
                duds += 1
            End If

        End While
        rdr.Close()

        da.BulkWrite(con, pawc, "productAttribute")
        pawc = Nothing

    End Sub

    'Public Sub Bundles(con As SqlClient.SqlConnection)

    '    Dim writecache As DataTable = da.MakeWriteCacheFor(con, "bundle")
    '    Dim TranslationWritecache As DataTable = da.MakeWriteCacheFor(con, "Translation")

    '    Dim rdr As SqlClient.SqlDataReader
    '    Dim bundleNames As Dictionary(Of String, clsTranslation) = New Dictionary(Of String, clsTranslation)

    '    iq.Bundles.Clear()
    '    iq.i_Bundle_code.Clear()

    '    da.DBExecutesql(con, "Delete from bundleitem")
    '    da.DBExecutesql(con, "Delete from bundle")
    '    da.DBExecutesql(con, "Delete from bundleSystem")

    '    da.DBExecutesql(con, "Delete from translation where  [group]='bc'")
    '    iq.LoadTranslations(con)

    '    For Each p In iq.Products.Values
    '        If Not p.Bundles Is Nothing Then
    '            p.Bundles.Clear()
    '        End If
    '    Next

    '    'This is done in several passes to allow us to use bulkwrites generally - which makes it orders of magnitude faster (despite the multiple passes)

    '    'Pass 1 - Make the translations for every bundle name
    '    'note, several bundle codes can have the same name

    '    Dim sql$ = "SELECT bundleCode,bundleName from " & server$ & "[iq].products.bundleIndex_ISSRebates"

    '    rdr = da.DBExecuteReader(con, sql$)

    '    Dim bName As clsTranslation
    '    Dim bnametext As String
    '    While rdr.Read

    '        If IsDBNull(rdr.Item("bundlename")) Then
    '            bnametext = ""
    '        Else
    '            bnametext = rdr.Item("bundlename")
    '        End If
    '        bName = iq.AddTranslation(bnametext, English, "bc", 0, )
    '        bundleNames.Add(rdr.Item("Bundlecode"), bName)
    '    End While
    '    rdr.Close()

    '    da.BulkWrite(con, TranslationWritecache, "Translation")

    '    iq.LoadTranslations(con)  'load the translation up (so the all have their ID's) after the bulk insert

    '    'Pass 2 - Create the bundles
    '    sql$ = "SELECT opgref,bundleCode,rebate,systems,startdate,enddate,sites from " & server$ & "[iq].[products].bundleIndex_ISSRebates"

    '    iq.Bundles.Clear()

    '    rdr = da.DBExecuteReader(con, sql$)

    '    Dim aBundle As clsBundle
    '    Dim bn As clsTranslation
    '    Dim region As clsRegion

    '    Dim bs As clsBundleSystem

    '    While rdr.Read

    '        If Not IsDBNull(rdr.Item("opgref")) Then
    '            If Not IsDBNull(rdr.Item("sites")) Then
    '                If InStr(rdr.Item("sites"), ",") = 0 Then
    '                    If iq.i_region_code.ContainsKey(rdr.Item("sites")) Then
    '                        region = iq.i_region_code(rdr.Item("sites"))
    '                        bn = bundleNames(rdr.Item("bundlecode"))

    '                        aBundle = New clsBundle(bn, rdr.Item("opgref"), rdr.Item("bundlecode"), region, rdr.Item("startdate"), rdr.Item("enddate"), writecache)
    '                    Else
    '                        Logit("Invalid region code " & rdr.Item("sites") & " in bundle " & rdr.Item("bundlecode"))
    '                    End If
    '                Else
    '                    Logit("Invalid region code " & rdr.Item("sites") & " in bundle " & rdr.Item("bundlecode"))
    '                End If
    '            End If
    '        End If
    '    End While

    '    rdr.Close()
    '    da.BulkWrite(con, writecache, "BUNDLE")
    '    iq.LoadBundles(con, rdr)



    '    'Pass3 - load the bundle items (options)

    '    Dim bundle As clsBundle

    '    Dim bi As clsBundleItem
    '    sql$ = "SELECT bs.BundleCode,bundlePn,bundlePnPrice,qty from " & server$ & "[iq].products.Bundle_prices bp join " & server$ & "[iq].products.BundleStore_ISSrebates bs on bp.bundleCode=bs.BundleCode and bp.bundlePn=bs.OptSKU"

    '    Dim currencies As Dictionary(Of clsRegion, clsCurrency) = New Dictionary(Of clsRegion, clsCurrency)

    '    currencies.Add(iq.i_region_code("GB"), iq.i_currency_code("GBP"))
    '    currencies.Add(iq.i_region_code("AA"), iq.i_currency_code("GBP"))
    '    currencies.Add(iq.i_region_code("US"), iq.i_currency_code("USD"))
    '    currencies.Add(iq.i_region_code("NL"), iq.i_currency_code("EUR"))
    '    currencies.Add(iq.i_region_code("IE"), iq.i_currency_code("EUR"))

    '    Dim itemwritecache As DataTable = da.MakeWriteCacheFor(con, "BundleItem")

    '    Dim price As NullablePrice
    '    Dim product As clsProduct

    '    rdr = da.DBExecuteReader(con, sql$)

    '    While rdr.Read
    '        If iq.i_Bundle_code.ContainsKey(rdr.Item("bundlecode")) Then
    '            bundle = iq.i_Bundle_code(rdr.Item("bundlecode"))
    '            If iq.i_SKU.ContainsKey(rdr.Item("bundlepn")) Then
    '                product = iq.i_SKU(rdr.Item("bundlepn"))
    '                price = New NullablePrice(rdr.Item("Bundlepnprice"), currencies(bundle.Region), False)
    '                bi = New clsBundleItem(bundle, product, price, 0, rdr.Item("qty"), itemwritecache) 'makes the enty in the [BundleItem] table
    '            Else
    '                'invalid bundle item sku 
    '            End If
    '        Else
    '            'invalid bundle code
    '        End If


    '    End While
    '    rdr.Close()

    '    da.BulkWrite(con, itemwritecache, "BundleItem")

    '    iq.LoadBundles(con, rdr) 'we must load the bundles - becuase they were created with bulkwrite, the bundleitems had no ID's so could not yet be added to the bundles

    '    'Pass 4 - Add the bundles (now they have their ID's to the systems)

    '    Dim bswc As DataTable = da.MakeWriteCacheFor(con, "BundleSystem") 'bundleSystem,WriteCache
    '    Dim system As clsProduct
    '    Dim rebate As Single
    '    sql$ = "SELECT bundleCode,systems,rebate from " & server$ & "[iq].products.bundleIndex_ISSRebates"
    '    rdr = da.DBExecuteReader(con, sql$)

    '    Dim systems As String
    '    While rdr.Read
    '        'add the bundles to the systems
    '        If iq.i_Bundle_code.ContainsKey(rdr.Item("bundlecode")) Then
    '            bundle = iq.i_Bundle_code(rdr.Item("bundlecode"))
    '            If bundle.Items.Count Then


    '                If IsDBNull(rdr.Item("systems")) Then
    '                    'this wont be fast (but is rare) - chris's 'leave the systems blank to mean it works on every system
    '                    Dim systemList As List(Of clsProduct) = iq.RootBranch.SystemsThatTake(Nothing, Nothing, bundle)
    '                    For Each system In systemList
    '                        If IsDBNull(rdr.Item("rebate")) Then rebate = 0 Else rebate = rdr.Item("rebate")
    '                        bs = New clsBundleSystem(bundle, system, rebate, bswc)  'makes the entry in the [BundleSystem] table, and adds the bundle to the system
    '                    Next
    '                Else
    '                    systems = rdr.Item("systems")
    '                    For Each sys In Split(rdr.Item("systems"), ",")
    '                        If iq.i_SKU.ContainsKey(sys) Then
    '                            system = iq.i_SKU(sys)
    '                            If IsDBNull(rdr.Item("rebate")) Then rebate = 0 Else rebate = rdr.Item("rebate")
    '                            bs = New clsBundleSystem(bundle, system, rebate, bswc)  'makes the entry in the [BundleSystem] table, and adds the bundle to the system
    '                        End If
    '                    Next
    '                End If
    '            End If
    '        End If

    '    End While
    '    rdr.Close()

    '    da.BulkWrite(con, bswc, "BundleSystem")



    'End Sub

    Public Function Hierarchy() As String

        'heirarcy import for SBSO and Printers plus standalone options
        'heirarcy import for SBSO and Printers plus standalone options

        Dim sql$ = "SELECT top 1000 [UPCNum],[MfrName],[ccDescription],[H1],[H2],[H3],[H4],[BUcode],[BU],[PL],[PLDesc],[Manuf4],[Manuf5],[Manuf6],[Manuf7],[AltPartNum],[Long Desc],[ProdCreated],[LastUpdated],[Source]"
        sql$ &= "UPCID,[Electronic],[UPCURL],[OEM] FROM [ChannelCentral].[products].[Hierarchy]   where H1 is not null and h2 is not null and h3 is not null and BUcode is not null and UPCNum not like '###%' order by h1,h2,h3"

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase("Data Source=www3.channelcentral.net\charliel,8484\;Initial Catalog=iQuote2; password=wainwright; user id=editor; Connection Timeout=10;")

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)

        'hang the H1 branches of a 'catalogue view' branch

        'columns to become attributes
        'products should not (ever) appear twice - use iq.i_SKU

        'we will augment he existing product with any missing columns (as attribites)

        Dim catroot As clsBranch = Nothing
        Dim l1Branch, l2branch, l3branch As clsBranch
        Dim ch1, ch2, ch3 As String
        Dim product As clsProduct
        Dim sector As clsSector

        ch1 = String.Empty
        ch2 = String.Empty
        ch3 = String.Empty

        'For SBSO generalise everything to "Products"
        Dim TLProducts As clsTranslation = iq.AddTranslation("Products", English, "collect", 0, Nothing, 0, False)  'The Collective noun is used for counds and labels of categories - such as 57 Printers (or cacti)
        Dim TLProduct As clsTranslation = iq.AddTranslation("Product", English, "collect", 0, Nothing, 0, False)  'The Collective noun singular is the 'singleton' version 1 printer (or 1 cactus ! )

        'Dim nbid, npid, ntid, npaid As Integer
        'Dim bwc As DataTable = da.MakeWriteCacheFor(con, "Branch", nbid)
        'Dim pwc As DataTable = da.MakeWriteCacheFor(con, "Product", npid)
        'Dim tlwc As DataTable = da.MakeWriteCacheFor(con, "Translation", ntid)
        'Dim pawc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute", npaid)

        Dim h1count, h2count, h3count, productCount As Integer

        While rdr.Read

            If rdr.Item("H1") IsNot DBNull.Value Then

                If iq.i_sector_code.ContainsKey(rdr.Item("BUcode")) Then
                    sector = iq.i_sector_code(rdr.Item("BUcode"))

                    Dim activeTo As Date = CDate("31/12/2100")
                    Dim activefrom As Date = CDate("1/1/2000")

                    product = New clsProduct(rdr.Item("UpcNum"), True, False, sector, iq.i_ProductType_Code("SYS"), activefrom, activeTo, True, False, True, "", "", "")
                    productCount += 1

                    If String.IsNullOrEmpty(ch1) Then
                        catroot = New clsBranch(Nothing, Nothing, iq.AddTranslation("SBSO Catalogue View", English, "UI", 0, Nothing, 0, False), "", TLProducts, TLProduct, iq.Screens(719), 1, False, "B")
                    End If

                    If rdr.Item("h1") <> ch1 Then

                        h1count += 1
                        Dim TLH1 As clsTranslation = iq.AddTranslation(rdr.Item("H1"), English, "H1", 0, Nothing, 0, False)

                        l1Branch = New clsBranch(Nothing, catroot, TLH1, "", TLProducts, TLProduct, Nothing, 1, True, "SB")
                        ch1 = rdr.Item("h1")

                        If rdr.Item("h2") <> ch2 Then
                            h2count += 1
                            Dim tlh2 As clsTranslation = iq.AddTranslation(rdr.Item("H2"), English, "H2", 0, Nothing, 0, False)
                            l2branch = New clsBranch(Nothing, l1Branch, tlh2, "", TLProducts, TLProduct, Nothing, 1, True, "BG")
                            ch2 = rdr.Item("h2")

                            If rdr.Item("h3") <> ch3 Then
                                h3count += 1
                                Dim tlh3 As clsTranslation = iq.AddTranslation(rdr.Item("H3"), English, "H3", 0, Nothing, 0, False)
                                l3branch = New clsBranch(product, l2branch, tlh3, "", TLProducts, TLProduct, Nothing, 1, False, "BG")
                                ch3 = rdr.Item("h3")
                            End If
                        End If
                    End If

                    AddAttribute("ccDescription", rdr, product)
                    AddAttribute("Manuf4", rdr, product)
                    AddAttribute("Manuf5", rdr, product)
                    AddAttribute("Manuf6", rdr, product)
                    AddAttribute("Manuf7", rdr, product)

                End If

            End If
        End While

        rdr.Close()
        con.Close()

        Return h1count & " H1's " & h2count & " H2's " & h3count & " H3's " & productCount & " Products - CatrootID is " & catroot.ID

    End Function


    Public Sub FlexOPGs()

        da.DBExecutesql("DELETE FROM FlexRegion")
        da.DBExecutesql("DELETE FROM FlexLine")
        da.DBExecutesql("DELETE FROM FlexRule")
        da.DBExecutesql("DELETE FROM Flex")

        iq.FlexOPGs.Clear()

        Dim scon As SqlClient.SqlConnection
        scon = da.OpenDatabase("Data Source=www3.channelcentral.net\charlie,8484\;Initial Catalog=iQuote2; password=wainwright; user id=editor; Connection Timeout=10;")
        Dim tcon As SqlClient.SqlConnection = da.OpenDatabase()

        'read the headers
        Dim sql As String = "SELECT OPG_ID,opg_description,OPG_StartDate,Opg_EndDate,opg_currencycode,opg_OptionCount_Min,OPG_OptionCount_Max,OPG_SysType FROM iq.products.opg_FlexPromo_Header WHERE opg_startDate<getdate() AND opg_enddate>getdate()"

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(scon, sql$)

        Dim flexOPG As clsFlexOPG
        Dim currency As clsCurrency
        Dim i_opgref As Dictionary(Of Integer, clsFlexOPG) = New Dictionary(Of Integer, clsFlexOPG)

        Dim dt As DataTable = da.MakeWriteCacheFor(tcon, "Flex")

        While rdr.Read
            If iq.i_currency_code.ContainsKey(rdr.Item("opg_currencycode")) Then
                currency = iq.i_currency_code(rdr.Item("opg_currencycode"))
                flexOPG = New clsFlexOPG(rdr.Item("opg_id"), rdr.Item("opg_description"), rdr.Item("opg_startDate"), rdr.Item("opg_endDate"), currency, rdr("opg_OptionCount_Min"), If(IsDBNull(rdr("OPG_OptionCount_Max")), 999, rdr("OPG_OptionCount_Max")), rdr.Item("OPG_SysType"), dt)

            Else
                Logit(rdr.Item("opg_currency") & " is not recognised")
            End If
        End While
        rdr.Close()

        da.BulkWrite(tcon, dt, "flex")

        'We have to load them back (having bulk written them) so the get their ID's
        iq.LoadFlex(tcon, rdr)
        For Each v In iq.FlexOPGs.Values
            i_opgref.Add(v.OPGRef, v)
        Next

        'read the lines
        Dim orphaned As Integer = 0
        Dim product As clsProduct
        Dim flexLine As clsFlexLine

        dt = da.MakeWriteCacheFor(tcon, "FlexLine")

        'sql$ = "SELECT OPG_ID as ref,OPG_LINE_UPC_NUM as sku,opg_line_listprice as listprice,opg_line_netprice as netprice,opg_line_discount_additional as DiscPerc,opg_line_startDate as Vf, opg_line_Enddate as Vt "
        'sql$ &= "FROM iq.products.OPG_FlexPromo_Lines WHERE opg_line_startDate<getdate() and opg_line_endDate>getdate()"
        ' Using wrong discount field use OPG_Line_Discount_Std instead of opg_line_discount_additional.
        sql = "SELECT OPG_ID as ref,OPG_LINE_UPC_NUM as sku,opg_line_listprice as listprice,opg_line_netprice as netprice,OPG_Line_Discount_Std as discPerc,opg_line_startDate as Vf, opg_line_Enddate as Vt "
        sql &= "FROM iq.products.OPG_FlexPromo_Lines WHERE opg_line_startDate<getdate() and opg_line_endDate>getdate()"
        rdr = da.DBExecuteReader(scon, sql)
        While rdr.Read
            Dim ref As String = rdr.Item("ref")

            If Not i_opgref.ContainsKey(ref) Then
                orphaned += 1
            Else
                flexOPG = i_opgref(ref)
                Dim sku As String = rdr.Item("SKU")

                If Not iq.i_SKU.ContainsKey(sku) Then
                    orphaned += 1
                Else
                    product = iq.i_SKU(sku)
                    'Making the flexline adds it to the OPG (and a few other places (the product, the root dictionary of flexLines))
                    'Dim rebate As Single = rdr.Item("listprice") * rdr.Item("discPerc") / 100
                    Dim rebate As Single = 0

                    If Not IsDBNull(rdr.Item("netprice")) Then
                        Dim listPrice As String = rdr.Item("listprice")
                        Dim lps As Single = CSng(listPrice)
                        Dim discountPerc As Single = CSng(rdr("discPerc")) ' using OPG_Line_Discount_Std as discount instead of opg_line_discount_additional
                        rebate = (listPrice - (listPrice * discountPerc / 100)) - CDec(rdr.Item("netprice"))
                        'rebate = listPrice * discountPerc / 100   'not included - rdr.Item("netprice") >>>old formula..
                    End If

                    flexLine = New clsFlexLine(flexOPG, product, rebate, rdr.Item("vf"), rdr.Item("vt"), dt)
                End If
            End If
        End While

        rdr.Close()

        da.BulkWrite(tcon, dt, "Flexline")

        Logit("Orphaned lines" & orphaned)

        'read the rules

        dt = da.MakeWriteCacheFor(tcon, "FlexRule")
        sql$ = "SELECT OPG_ID as opgref,UPC_Type as ProdType,UPC_qty_min as [min],UPC_qty_max as [max],[optional] FROM iq.Products.opg_flexPromo_ProductRules"

        rdr = da.DBExecuteReader(scon, sql$)


        Dim badrules As Integer = 0
        Dim Rule As clsFlexRule = Nothing
        Dim productType As clsProductType = Nothing

        While rdr.Read

            Dim ref As String = rdr.Item("opgref")

            If Not i_opgref.ContainsKey(ref) Then
                badrules += 1
            Else
                flexOPG = i_opgref(ref)
                Dim pt As String = rdr.Item("ProdType")   'Flex opgs should only be valid for storage in the UK
                If pt = "SYS" Then pt = flexOPG.OPGSysType ' ugly mapping to IQ2 ProductType
                If Not iq.i_ProductType_Code.ContainsKey(pt) Then
                    Logit("Unknown Product type:" & pt)
                Else
                    Rule = New clsFlexRule(flexOPG, iq.i_ProductType_Code(pt), rdr.Item("min"), rdr.Item("max"), CBool(rdr.Item("optional")), dt)
                End If
            End If
        End While
        rdr.Close()

        da.BulkWrite(tcon, dt, "FlexRule")

        Dim badCountries As Integer = 0
        Dim missingOPGs As Integer = 0

        'read the countries
        Dim flexRegion As clsFlexRegion
        sql$ = "SELECT OPG_ID,OPG_COUNTRYcode FROM iq.products.OPG_FlexPromo_Countries"
        rdr = da.DBExecuteReader(scon, sql$)

        dt = da.MakeWriteCacheFor(tcon, "flexRegion")

        Dim region As clsRegion

        While rdr.Read
            Dim Ref As String = rdr.Item("OPG_ID")
            Dim cc As String = rdr.Item("opg_countrycode")


            If cc = "UK" Then
                cc = "GB"
            End If


            If Not iq.i_region_code.ContainsKey(cc) Then
                badCountries += 1
            Else
                region = iq.i_region_code(cc)
                If Not i_opgref.ContainsKey(Ref) Then
                    missingOPGs += 1
                Else
                    flexOPG = i_opgref(Ref)
                    flexRegion = New clsFlexRegion(flexOPG, region, dt)
                End If
            End If
        End While

        da.BulkWrite(tcon, dt, "FlexRegion")

        scon.Close()
        tcon.Close()

        Logit("Done flex import", False, True)

    End Sub

    Public Sub Avalanche(con As SqlClient.SqlConnection)

        'around 600 rows

        'first pass = gets the distinct OPGs

        da.DBExecutesql("DELETE FROM avalancheSystem")
        da.DBExecutesql("DELETE FROM avalancheOption")
        da.DBExecutesql("DELETE FROM avalancheOPG")

        da.DBExecutesql("DELETE FROM promoScan")

        iq.i_OpgRef.Clear()

        For Each product In iq.Products.Values
            If product.AvalancheOPGs IsNot Nothing Then
                If product.AvalancheOPGs.Count Then
                    product.AvalancheOPGs.Clear()
                End If
            End If
        Next


        Dim writecache As DataTable = da.MakeWriteCacheFor(con, "avalancheOPG")
        Dim WriteAvSys As DataTable = da.MakeWriteCacheFor(con, "avalancheSystem")  ' a datatable to bulk insert the foriegn key pairs which relates systems (products) to avalanche offers

        Dim rdr As SqlClient.SqlDataReader

        Dim sql As String = "SELECT optionCountMin,systems,optionCountMax,startDate,endDate,opgREF,countries FROM " & DSserver & "[DataStore].[products].[Avalanche_Rules] "
        sql &= " WHERE enddate > getdate() "
        sql &= "GROUP BY optioncountmin,optioncountmax,startdate,enddate,opgref,countries,systems  ORDER BY enddate"
        rdr = da.DBExecuteReader(con, sql)


        Dim AvOPG As ClsAvalancheOPG
        Dim systems() As String


        Dim row As System.Data.DataRow

        Dim opg As String = String.Empty
        Dim avopt As clsAvalancheOption
        While rdr.Read
            If Not iq.i_OpgRef.ContainsKey(rdr.Item("opgref")) Then

                Dim region As clsRegion = Nothing
                If iq.i_region_code.ContainsKey(rdr.Item("countries")) Then
                    region = iq.i_region_code(rdr.Item("countries"))
                    AvOPG = New ClsAvalancheOPG(rdr.Item("opgref"), region, rdr.Item("startDate"), rdr.Item("endDate"), rdr.Item("OptionCountMin"), rdr.Item("OptionCountMax"), writecache)
                Else
                    Logit("Unrecognised region/country " & rdr.Item("Countries") & " In opg " & rdr.Item("opgRef"))
                End If
            Else
                Stop
            End If
        End While
        rdr.Close()

        da.BulkWrite(con, writecache, "AvalancheOPG")
        Logit("Wrote " & writecache.Rows.Count & " avalanche systems")


        iq.LoadAvalancheOPGs(con, rdr) 'need to load them up to give them their ID's (after the bulk-write)


        'Now need to (in a second pass becuase they didn't have their ID's until the were re-loaded after the bulk write) add the OPG's to the systems
        sql = "SELECT DISTINCT  systems,opgref FROM " & DSserver & "[DataStore].[products].[Avalanche_Rules] "
        rdr = da.DBExecuteReader(con, sql)
        While rdr.Read
            systems = Split(rdr.Item("systems"), ",")
            Dim ref As String = rdr.Item("opgREF")
            If Not iq.i_OpgRef.ContainsKey(ref) Then
                Logit("Avalanche Rules")

            Else
                AvOPG = iq.i_OpgRef(ref)

                For Each Sys In systems
                    If iq.i_SKU.ContainsKey(Sys) Then
                        If Not iq.i_SKU(Sys).AvalancheOPGs.ContainsKey(AvOPG.ID) Then
                            iq.i_SKU(Sys).AvalancheOPGs.Add(AvOPG.ID, AvOPG)  'add the avalanche to every qualifying system 
                            row = WriteAvSys.NewRow()
                            row("fk_product_id_system") = iq.i_SKU(Sys).ID
                            row("fk_avalancheOPG_id") = AvOPG.ID
                            WriteAvSys.Rows.Add(row)

                        Else
                            Logit("Part " & Sys & " is listed more than once in opg " & AvOPG.OPGref)
                        End If
                    Else
                        Logit("Avalanche " & rdr.Item("OpgRef") & " contains unrecognised system SKU " & Sys)
                        '    Stop
                    End If
                Next
            End If
        End While
        rdr.Close()


        Dim WriteAvOpts As DataTable = da.MakeWriteCacheFor(con, "avalancheOption")

        da.BulkWrite(con, WriteAvSys, "AvalancheSystem")

        Logit("Wrote " & WriteAvOpts.Rows.Count & " avalanche systems")

        sql = "Select prodrefcode,lpdiscountpercent,opgref,systems  FROM " & DSserver & "[DataStore].[products].[Avalanche_Rules] "
        rdr = da.DBExecuteReader(con, sql)

        While rdr.Read
            If iq.i_OpgRef.ContainsKey(rdr.Item("opgref")) Then
                'and for every line make an OPGoption - hooking them to the OPG's we made earlier 
                avopt = New clsAvalancheOption(iq.i_OpgRef(rdr.Item("opgref")), rdr.Item("ProdRefCode"), rdr.Item("LPDiscountPercent"), WriteAvOpts)
            End If

        End While
        rdr.Close()

        da.BulkWrite(con, WriteAvOpts, "AvalancheOption")

        Logit("Wrote " & WriteAvOpts.Rows.Count & " avalanche options")

        Logit("done avalanche import", False, True)

        iq.LoadAvalancheOPGs(con, rdr)

    End Sub


    Public Function Regions(con As SqlClient.SqlConnection) As Dictionary(Of String, clsRegion)

        'returns a dictionary of region code to clsRegion

        Dim dicRegions As Dictionary(Of String, clsRegion)
        dicRegions = New Dictionary(Of String, clsRegion)(StringComparer.CurrentCultureIgnoreCase)

        'improve with... (get rid of the try catch(
        'CultureInfo[] cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures);

        'load  the regions we already have (from the bootstrap)
        For Each r In iq.Regions.Values
            dicRegions.Add(r.Code, r)
        Next


        Dim sql$

        sql$ = "SELECT DISTINCT region from " & server$ & "[iq].dbo.countries"

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, sql$)

        Dim rgn As Object

        While rdr.Read
            rgn = rdr.Item("region")
            If Not rgn Is DBNull.Value Then
                If Not dicRegions.ContainsKey(rgn) Then
                    dicRegions.Add(rgn, New clsRegion(r_worldwide, rgn, iq.AddTranslation(rgn, English, "RR", 0, Nothing, 0, False), False, iq.i_culture_code("en-gb"), False, ""))  'Make the root level regions
                End If
            End If
        End While
        rdr.Close()

        sql$ = "SELECT Region,countryName,countrycode FROM " & server$ & "iq.dbo.[Countries] ORDER BY countryName"
        rdr = da.DBExecuteReader(con, sql$)

        Dim cntry$
        Dim cname$
        Dim o As Integer = 0

        Dim region As clsRegion

        'make a root level placeholder for those countries that have not yet been assigned to a region


        While rdr.Read

            'lots of the piddly countries haven't been assigned to a region yet (fun job for someone !)
            If IsDBNull(rdr.Item("Region")) Then
                region = r_RestOfWorld
            Else
                region = dicRegions(rdr.Item("Region"))
            End If

            cntry = rdr.Item("Countrycode")

            Dim culture As clsCulture = iq.i_culture_code("en-gb")


            ' If cntry = "UK" Then culture = iq.i_culture_code("en-gb") : cntry = "GB"
            If cntry = "US" Then culture = iq.i_culture_code("en-us")
            If cntry = "DK" Then culture = iq.i_culture_code("da-dk")
            If cntry = "FR" Then culture = iq.i_culture_code("fr-fr")
            If cntry = "AF" Then culture = iq.i_culture_code("af-ZA")
            If cntry = "AL" Then culture = iq.i_culture_code("sq-AL") 'albania
            If cntry = "DZ" Then culture = iq.i_culture_code("ar-DZ") '
            If cntry = "AS" Then culture = iq.i_culture_code("as-EN") 'America samoa
            If cntry = "CH" Then culture = iq.i_culture_code("ch-DE") 'switzerlan
            If cntry = "AT" Then culture = iq.i_culture_code("at-DE") 'austria





            cname = rdr.Item("countryName")

            If Not iq.i_region_code.ContainsKey(cntry) Then

                dicRegions.Add(cntry, New clsRegion(region, cntry, iq.AddTranslation(cname, English, "CN", o, Nothing, 0, False), True, culture, False, ""))  'make a region for each Country
                o += 1
            End If
        End While


        rdr.Close()

        'sql$ = "SELECT distinct activeSites FROM   [IQ].[products].[Systems]"

        'Dim sd As SortedList = New SortedList
        'Dim c() As String

        'Dim uniqueCombos As Dictionary(Of String, clsRegion) = New Dictionary(Of String, clsRegion)
        'rdr = da.dbexecuteReader(con, sql$)

        'While rdr.Read
        '    c = Split(rdr.Item("activeSites"), ",")
        '    sd = New SortedList
        '    For Each cc In c
        '        If cc <> "ZZ" Then
        '            sd.Add(cc, cc)
        '        End If
        '    Next

        '    If sd.Count Then
        '        Dim sorted As List(Of String) = New List(Of String)
        '        For Each cc In sd.Keys
        '            sorted.Add(cc)
        '        Next
        '        Dim sl As String = Join(sorted.ToArray, ",")

        '        If Not uniqueCombos.ContainsKey(sl) Then
        '            uniqueCombos.Add(sl, Nothing)
        '        End If
        '    End If

        'End While

        'rdr.Close()

        Return dicRegions

    End Function


    ''' <summary>Builds and returns a dictionary of SysFamilycode^optfamily>clsLimit</summary>
    ''' <remarks>ofdic Is an INPUT used to work our narrow option families from broad option types (per family) eg. (HDD>NHP35LFF)  - these are later made into clsQuantitys (autoAdds) and pointed to the option branches - </remarks>
    Public Function BuildOptLimits(con As SqlClient.SqlConnection, ofdic As Dictionary(Of String, Dictionary(Of String, String)), Optional sysSkus As List(Of String) = Nothing, Optional filter As String = "") As Dictionary(Of String, clsLimit)

        Dim dicLimits As New Dictionary(Of String, clsLimit)(StringComparer.CurrentCultureIgnoreCase) 'we RETURN this

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$
        If sysSkus IsNot Nothing Then
            sql$ = "SELECT [SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref] "
            sql$ &= "from  " & server$ & "[iq].[products].[OptionLimits] INNER join  " & server$ & "[iq].[products].[opttypes] o on o.OptTypeCode = opttype "
            sql$ &= "inner join   " & server$ & "[iq].[products].[systems] on [SysFamily] = [familycode]"
            sql$ &= "WHERE modelsku IN ('" & Join(sysSkus.ToArray, "','") & "')"

        Else

            sql$ = "SELECT [SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref] from " & server$ & "[iq].[products].[OptionLimits] "
            sql$ &= "INNER join " & server$ & "[iq].[products].[opttypes] o on o.OptTypeCode = opttype"
            If filter <> "" Then sql$ &= " where opttype='" & filter & "'"


        End If
        'Return WLNA,FAN,MEM,PSU etc (opt types)

        'OptCat - Chassis
        'opttype fan
        'qtyinstalled 4


        rdr = da.DBExecuteReader(con, sql$)

        Dim sysfam As String
        Dim OptionType As String

        While rdr.Read
            sysfam = rdr.Item("sysfamily")  'this is actual the narrow sysfamily code
            '        optCat = Trim$(rdr.Item("optCat"))
            OptionType = Trim$(rdr.Item("optType")) '- We have to work out the narrow option family (NHP35LFF) from the broad option type (HDD)
            If ofdic.ContainsKey(sysfam) Then
                If ofdic(sysfam).ContainsKey(OptionType) Or OptionType = "CPU" Then


                    Dim optionfamily As String
                    If OptionType = "CPU" Then
                        optionfamily = "GEN_CPU" ' ;ERIC"
                    Else
                        optionfamily = ofdic(sysfam)(OptionType)
                    End If


                    '  If OptionType = "CPU" Then Stop

                    Dim ck As String = sysfam & "^" & OptionType & "^" & optionfamily


                    'If Not dicLimits.ContainsKey(ck) Then
                    'dicLimits.Add(ck,ring, clsLimit))
                    'End If

                    'The qinstalled are all set to zero 
                    'otherwise EVERY option of the type (eg MEM) has an installed qty
                    Dim qtyInstalled As Integer = 0
                    If Not IsDBNull(rdr.Item("qtyInstalled")) Then qtyInstalled = rdr.Item("qtyInstalled")
                    Dim alimit As clsLimit = New clsLimit(qtyInstalled, 0, LockNull(rdr.Item("QtyMax"), 999, 1), LockNull(rdr.Item("incr_min"), 1, 1), LockNull(rdr.Item("incr_pref"), 1, 1))
                    If alimit.MinIncr = 1 And alimit.PrefIncr = 1 And alimit.Qinstalled = 0 And alimit.Qmin = 0 And alimit.Qmax = 999 Then
                        'no limit required 
                        NoOp() 'somewhere to hang a breakpoint
                    Else
                        If dicLimits.ContainsKey(ck) Then
                            dicLimits(ck).MinIncr = alimit.MinIncr
                            dicLimits(ck).PrefIncr = alimit.PrefIncr
                            dicLimits(ck).Qinstalled = alimit.Qinstalled
                            dicLimits(ck).Qmax = alimit.Qmax
                            dicLimits(ck).Qmin = alimit.Qmin
                        Else

                            dicLimits.Add(ck, alimit)
                        End If
                    End If
                Else
                    '    Stop

                End If
            End If

        End While
        rdr.Close()

        Return dicLimits

    End Function

    Private Function NoOp()

    End Function

    Private Function LockNull(f As Object, nullsubst As Integer, min As Integer) As Integer
        If IsDBNull(f) Then Return nullsubst

        If f < min Then f = min

        Return f

    End Function

    Public Function defaultWarranty()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim rdr As SqlClient.SqlDataReader


        If dicAbbreviations Is Nothing Then LoadAbbreviations(con)

        Dim sql$


        Dim wc As DataTable = da.MakeWriteCacheFor(con, "productAttribute")


        'these are the most granular 'sub' families - family code
        '                  narrow     broad
        sql$ = "SELECT sysfamily,defaultWTY,familyRaid,instRaid,sysfamily_cat "
        sql$ &= "FROM " & server$ & "[iq].products.sysfamilydefinitions"
        rdr = da.DBExecuteReader(con, sql$)

        'Group all products by their sysfamily
        Dim dsf As Dictionary(Of String, List(Of clsProduct)) = New Dictionary(Of String, List(Of clsProduct))(StringComparer.CurrentCultureIgnoreCase)


        Dim pa As clsProductAttribute
        Dim sysfam As String
        For Each product In iq.Products.Values
            If product.i_Attributes_Code.ContainsKey("FamMinor") Then
                pa = product.i_Attributes_Code("FamMinor")(0)
                sysfam = pa.Translation.text(English)
                If Not dsf.ContainsKey(sysfam) Then dsf.Add(sysfam, New List(Of clsProduct))
                dsf(sysfam).Add(product)
            End If
        Next



        Dim attrib_wty As clsAttribute = Nothing
        iq.i_attribute_code.TryGetValue("DWTY", attrib_wty)
        If attrib_wty Is Nothing Then attrib_wty = New clsAttribute("dDWTY", iq.AddTranslation("CC-Warranty", English, "aNames", 0, Nothing, 0, False), 0)

        Dim attrib_fRAID As clsAttribute = Nothing
        iq.i_attribute_code.TryGetValue("fRAID", attrib_fRAID)
        If attrib_fRAID Is Nothing Then attrib_fRAID = New clsAttribute("fRAID", iq.AddTranslation("CC-RAID", English, "aNames", 0, Nothing, 0, False), 0)

        Dim attrib_cat As clsAttribute = Nothing
        iq.i_attribute_code.TryGetValue("sCat", attrib_cat)
        If attrib_cat Is Nothing Then attrib_cat = New clsAttribute("sCat", iq.AddTranslation("CC-Category", English, "aNames", 0, Nothing, 0, False), 0)

        Dim tl As clsTranslation

        While rdr.Read
            sysfam = rdr.Item("sysfamily")
            If dsf.ContainsKey(sysfam) Then
                For Each p In dsf(sysfam)

                    'Default warranty
                    If Not IsDBNull(rdr.Item("defaultwty")) Then
                        Dim wtycode As String = rdr.Item("defaultwty")
                        If dicAbbreviations.ContainsKey(wtycode) Then
                            tl = iq.AddTranslation(dicAbbreviations(wtycode), English, "DWTY", 0, Nothing, 0, False)  'Each translation will only be created once
                            Dim value As Single = Val(Left(tl.text(English), 1))  'Take the value of the first char (which is usually the length in years) as the numeric value
                            pa = New clsProductAttribute(p, attrib_wty, value, iq.i_unit_code("Text"), tl, wc)
                        Else
                            Logit(sysfam & " defaultWTY " & wtycode & " does not exist in abbreviations")
                        End If
                    End If

                    If Not IsDBNull(rdr.Item("familyRaid")) Then
                        Dim raidcode As String = rdr.Item("familyRaid")
                        If dicAbbreviations.ContainsKey(raidcode) Then
                            tl = iq.AddTranslation(dicAbbreviations(raidcode), English, "fRAID", 0, Nothing, 0, False)  'Each translation will only be created once
                            pa = New clsProductAttribute(p, attrib_fRAID, 0, iq.i_unit_code("Text"), tl, wc)
                        Else
                            Logit(sysfam & " familyRaid " & raidcode & " does not exist in abbreviations")
                        End If
                    End If

                    If Not IsDBNull(rdr.Item("sysFamily_cat")) Then
                        Dim catcode As String = rdr.Item("sysFamily_Cat")
                        If dicAbbreviations.ContainsKey(catcode) Then
                            tl = iq.AddTranslation(dicAbbreviations(rdr.Item("sysFamily_cat")), English, "sCat", 0, Nothing, 0, False)  'Each translation will only be created once
                            pa = New clsProductAttribute(p, attrib_cat, 0, iq.i_unit_code("Text"), tl, wc)
                        Else
                            Logit(catcode & " sysFamily_cat " & catcode & " does not exist in abbreviations")
                        End If
                    End If
                Next
            End If
        End While

        rdr.Close()

        da.BulkWrite(con, wc, "productAttribute")

        con.Close()

        Logit("Warranty import complete ", False, True)


    End Function


    Public Function FamilyOptTypeToOptFamily() As Dictionary(Of String, Dictionary(Of String, String))

        'Returns a dictionary of option type > optionfamily - per systemfamilyCODE (minorFamilty)
        'eg              dl580pg825NHPlff > HDD > NHP35LFFSC

        Dim dic As Dictionary(Of String, Dictionary(Of String, String))
        dic = New Dictionary(Of String, Dictionary(Of String, String))(StringComparer.CurrentCultureIgnoreCase)

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim rdr As SqlClient.SqlDataReader
        Dim sql$

        'these are the most granular 'sub' families - family code
        '                  narrow     broad


        sql$ = "SELECT * "
        sql$ &= "from " & server$ & "[iq].products.sysfamilydefinitions"
        rdr = da.DBExecuteReader(con, sql$)

        Dim cols As Dictionary(Of String, String)

        'Gives' slots are created for each sysystem - according to the OptionLimits

        While rdr.Read
            cols = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
            If dic.ContainsKey(rdr.Item("sysFamily")) Then Continue While
            dic.Add(rdr.Item("sysFamily"), cols)

            If Not dic.ContainsKey(rdr.Item("sysfamilyname")) Then
                dic.Add(rdr.Item("sysfamilyname"), cols)  'add a duplicate for the broader Sysfamilyname.. as come option limits are specified against this (grrr)
            End If

            AddOTcol(cols, rdr, "MEM", "familyMem")
            AddOTcol(cols, rdr, "HDD", "familyPriStor")
            AddOTcol(cols, rdr, "OPT", "familySecStor")
            AddOTcol(cols, rdr, "FAN", "familyFan")
            AddOTcol(cols, rdr, "PSU", "familyPSU")
            AddOTcol(cols, rdr, "WTY", "familyWAR")
            AddOTcol(cols, rdr, "MAN", "familyMAN")
            AddOTcol(cols, rdr, "VGA", "familyVGA")
            AddOTcol(cols, rdr, "RAID", "familyRAID")
            '  AddOTcol(cols, rdr, "CPU", "familyMAN")

        End While
        rdr.Close()
        con.Close()

        Return dic

    End Function

    Private Sub AddOTcol(ByRef cols As Dictionary(Of String, String), rdr As SqlClient.SqlDataReader, opttype As String, FamCol As String)

        'add an option type code > Slot type to the inner dictionary of one sysFamily
        'eg. HDD > NHP35SFF (for a DL580pG7)

        Dim optFam As String
        If Not IsDBNull(rdr.Item(FamCol)) Then
            optFam = rdr.Item(FamCol)
            'If cols.ContainsKey(opttype) Then Stop
            cols.Add(opttype, optFam)
        End If

    End Sub


    Public Sub Sectors(con As SqlClient.SqlConnection, ByRef dicSectors As Dictionary(Of String, clsSector))

        'returns a list of sectors by HP "BU" code

        Dim sql$
        sql$ = "select BUID2,BUlabelShort from " & server$ & "[channelcentral].products.translateBU"

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, sql$)

        Dim sector As clsSector

        While rdr.Read

            If Not dicSectors.ContainsKey(Trim$(rdr.Item("BUID2"))) Then
                sector = New clsSector(rdr.Item("BUID2"), iq.AddTranslation(rdr.Item("BULabelShort"), English, "BUs", 0, Nothing, 0, False))
                dicSectors.Add(Trim$(rdr.Item("BUID2")), sector)
            End If

        End While

        If Not dicSectors.ContainsKey("NoSector") Then
            sector = New clsSector("NoSector", iq.AddTranslation("No Sector", English, "BUs", 0, Nothing, 0, False))
            dicSectors.Add("NoSector", sector)
        End If

        rdr.Close()

    End Sub
    ''' <summary>
    ''' Imports families 
    ''' </summary>
    ''' <returns>Returns a dictionary of Dans SysFamilyName to The family Branch I create for it</returns>
    ''' <remarks></remarks>
    Public Function Families(con As SqlClient.SqlConnection, dicSysTypes As Dictionary(Of String, clsBranch)) As Dictionary(Of String, clsBranch)

        Families = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)

        Dim rdr As SqlClient.SqlDataReader

        Dim ba As clsAttribute = New clsAttribute("bays", iq.AddTranslation("Drive bays", English, "attribs", 0, Nothing, 0, 0), 0)
        Dim hpa As clsAttribute = New clsAttribute("HPL", iq.AddTranslation("Hot Pluggable", English, "attribs", 0, Nothing, 0, 0), 0)


        'The family branches can only carry the Major' fore
        Dim fMaj As clsAttribute = iq.i_attribute_code("FamMajor")
        Dim fMin As clsAttribute = iq.i_attribute_code("FamMinor")
        Dim fDisp As clsAttribute = iq.i_attribute_code("FamDisp")

        'the Unabbreviated family name is the BRANCH.Translation


        Dim lff As clsTranslation = iq.AddTranslation("LFF", English, "bays", 0, Nothing, 0, 0)
        Dim lffL As clsTranslation = iq.AddTranslation("Large form factor (3.5 inch) drive bays ", English, "bays", 0, Nothing, 0, False)  ' Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

        Dim sff As clsTranslation = iq.AddTranslation("SFF", English, "bays", 0, Nothing, 0, False)
        Dim sffL As clsTranslation = iq.AddTranslation("Small Form Factor (2.5 inch) drive bays ", English, "bays", 0, Nothing, 0, False)  ' Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

        Dim bff As clsTranslation = iq.AddTranslation("Both", English, "bays", 0, Nothing, 0, False)
        Dim bffL As clsTranslation = iq.AddTranslation("Has both Small Form Factor (2.5 inch) and Large Form Factor (3.5 inch) drive bays ", English, "bays", 0, Nothing, 0, False)  ' Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

        Dim HPL As clsTranslation = iq.AddTranslation("HP", English, "bays", 0, Nothing, 0, False)
        Dim HPLL As clsTranslation = iq.AddTranslation("Hot Pluggable", English, "bays", 0, Nothing, 0, False)  ' Consecutive translations (keys) are used to expand Abbreviation - whilst this isn't wildly inuitive, it saves a (rarely used) field in translations - and a lot of code

        Dim sql$
        sql$ = "SELECT DISTINCT sysfamilyname,systype,lifeCycleMonths,managementTxt,SecurityTxt,RangeText,subTitle,FamilyPriStor,FamilySecStor "
        sql$ &= "from " & server$ & "[iq].products.union_sysfamilydefinitions right join " & server$ & "[iq].products.sysrangetext ON sysfamilyname=rangename"
        rdr = da.DBExecuteReader(con, sql$)


        '    '                             hello                 fr      bonjour
        'Dim xlate As New Dictionary(Of String, Dictionary(Of String, String))

        Dim product As clsProduct ' family branches need a product to attach additional attributes to (primarly descriptions)

        Dim pa As clsProductAttribute

        Dim sysTrans As clsTranslation = iq.AddTranslation("systems", English, "collect", 0, Nothing, 0, False)
        Dim sysTransSingular As clsTranslation = iq.AddTranslation("system", English, "collect", 0, Nothing, 0, False)

        Dim fnpa As clsProductAttribute

        Dim FamBranch As clsBranch
        While rdr.Read
            If Not IsDBNull(rdr.Item("sysfamilyname")) Then
                If Not Families.ContainsKey(Trim$(rdr.Item("sysfamilyname"))) Then ' this is the short (e.g. 'G8' version)

                    product = New clsProduct("", False, False, iq.i_sector_code("NoSector"), iq.i_ProductType_Code(rdr.Item("systype")), CDate("01/01/2000"), CDate("31/12/2100"), True, False, True, "", "", "")

                    'record the family name under the 'majorFamily'  attribute on the branch - required for suppressing/displaying notes by family - see import.ExtText
                    fnpa = New clsProductAttribute(product, fMaj, 0, iq.i_unit_code("txt"), iq.AddTranslation(Trim$(rdr.Item("sysfamilyname")), English, "FamMajor", 0, Nothing, 0, False))

                    If Not rdr.Item("lifecyclemonths") Is DBNull.Value Then
                        pa = New clsProductAttribute(product, iq.i_attribute_code("lifeCycle"), rdr.Item("lifecyclemonths"), iq.i_unit_code("num"), iq.AddTranslation(rdr.Item("lifecyclemonths"), English, "", 0, Nothing, 0, False))
                    End If

                    If Not rdr.Item("managementTxt") Is DBNull.Value Then
                        pa = New clsProductAttribute(product, iq.i_attribute_code("management"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("managementTxt"), English, "", 0, Nothing, 0, False))
                    End If

                    If Not rdr.Item("securityTxt") Is DBNull.Value Then
                        pa = New clsProductAttribute(product, iq.i_attribute_code("security"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("securityTxt"), English, "", 0, Nothing, 0, False))
                    End If

                    If Not rdr.Item("rangeText") Is DBNull.Value Then
                        pa = New clsProductAttribute(product, iq.i_attribute_code("desc"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("rangeText"), English, "", 0, Nothing, 0, False))
                    End If

                    If Not rdr.Item("subTitle") Is DBNull.Value Then
                        pa = New clsProductAttribute(product, iq.i_attribute_code("subTitle"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("subTitle"), English, "", 0, Nothing, 0, False))
                    End If

                    'Large/small form factor dirve bays
                    Dim bays As Integer = 0 '1=sff 2 = lff 3 = both
                    If Not IsDBNull(rdr.Item("FamilyPriStor")) Then

                        If InStr(UCase(rdr.Item("FamilyPriStor")), "LFF") Then
                            bays = bays Or 2
                        End If
                        If InStr(UCase(rdr.Item("FamilyPriStor")), "SFF") Then
                            bays = bays Or 1
                        End If

                        Dim baytran As clsTranslation = Nothing
                        If bays = 1 Then baytran = sff
                        If bays = 2 Then baytran = lff
                        If bays = 3 Then baytran = bff ' both form factors

                        pa = New clsProductAttribute(product, iq.i_attribute_code("bays"), bays, iq.i_unit_code("txt"), baytran)

                        If InStr(UCase(rdr.Item("FamilyPriStor")), "HP") And InStr(UCase(rdr.Item("FamilyPriStor")), "NHP") = 0 Then
                            pa = New clsProductAttribute(product, iq.i_attribute_code("HPL"), 1, iq.i_unit_code("txt"), HPL)
                        End If
                    End If


                    Dim code As String = rdr.Item("sysFamilyName")
                    Dim FnEn As String
                    If dicAbbreviations.ContainsKey(code.ToLower) Then
                        FnEn = dicAbbreviations(code.ToLower) ''xlate()("en")
                    Else
                        FnEn = code
                        Logit("no abbreviation for " & code)
                    End If

                    Dim fntl As clsTranslation
                    'If iq.EnglishIndex.ContainsKey(FnEn) Then 'this is the abbreviation/key   - we do not append the word "family" (dans choice)
                    ' fntl = iq.EnglishIndex(FnEn)
                    'Else
                    fntl = iq.AddTranslation(FnEn, English, "", 0, Nothing, 0, False)
                    '               End If
                    '
                    FamBranch = New clsBranch(product, dicSysTypes(rdr.Item("systype")), fntl, "/images/iq/prod_" & rdr.Item("sysfamilyname") & ".gif", sysTrans, sysTransSingular, Nothing, 100, False, "B")

                    Families.Add(Trim$(rdr.Item("sysfamilyname")), FamBranch)
                    'add the family under its systype branch (Servers, Notebooks, desktops, storage etc)
                    ' - NO need - it's done internall now dicSysTypes(rdr.Item("systype")).childBranches.Add(FamBranch.ID, FamBranch)

                End If
            End If
        End While

        rdr.Close()

    End Function



    Public Function options2(con As SqlClient.SqlConnection, dicplcode As Dictionary(Of String, String), dicUnits As Dictionary(Of String, clsUnit), ByRef dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)), containment As Dictionary(Of String, List(Of String))) As Dictionary(Of String, clsBranch)  '

        'NB: in Iq1 a 'family' is a 'narrow/specific' designation - and type is broad (major type)
        'This *always* confuses me

        options2 = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)

        Dim dicSC As New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)
        dicSC = New Dictionary(Of String, clsTranslation)

        dicSC.Add("A", iq.AddTranslation("A", English, "SCC", 10, Nothing, 0, False))
        dicSC.Add("TV", iq.AddTranslation("TV", English, "SCC", 20, Nothing, 0, False))
        dicSC.Add("SB", iq.AddTranslation("SB", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("R", iq.AddTranslation("R", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("PR", iq.AddTranslation("PR", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("GO", iq.AddTranslation("GO", English, "SCC", 30, Nothing, 0, False))

        If Not iq.i_attribute_code.ContainsKey("Slots") Then
            Dim sa As clsAttribute = New clsAttribute("Slots", iq.AddTranslation("Slots used (legacy/import)", English, "", 0, Nothing, 0, False), 0)
        End If

        'If Not iq.i_attribute_code.ContainsKey("OptFam") Then  'Narrow
        '    Dim optfamAtt As clsAttribute = New clsAttribute("OptFam", iq.AddTranslation("Option Family (legacy/import)", English, "", 0, Nothing, 0, False), 0)
        'End If
        'Builds the global otpions tree- and returns Returns a dictionary of l1^l2^(l3)^OptFamily (slot.minor) > optfamily branch. 
        'e_Code(rdr.Item("opttype")), rdr.Item("activefrom"), rdr.Item("activeto"), rdr.Item("active"), rdr.Item("eol"), Not rdr.Item("AAonly"))

        LoadAbbreviations(con)
        Dim sql$

        sql$ = "SELECT v.OptSN,optsc,po.optsku,v.sortorder,fio,"
        sql$ &= "case when po.sysfamily = ''  then isnull((select  sysfamilyname+', ' as 'data()' from h3.iq.products.systems inner join  h3.[iQ].[products].[SysFamilyDefinitions] on [SysFamilyDefinitions].sysfamily=systems.familycode  where PSU = po.optsku and opttype='PSU'  group by sysfamilyname FOR XML PATH('')),'') else po.sysfamily end as sysfamily,"
        sql$ &= "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,"
        sql$ &= "unitQty as capacity,ot.optTypeUnit as capacityUnit,localisation,"
        sql$ &= "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,po.opttype2,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription "
        sql$ &= "FROM h3.iq.products.V2_OptionCats v "
        sql$ &= "JOIN h3.iq.products.options po ON v.optsn=po.optsn "
        sql$ &= "JOIN h3.[iq].products.optTypes as OT on OT.optTypeCode=optType "
        sql$ &= "JOIN h3.[channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku "
        sql$ &= "WHERE (sYSFAMILY LIKE '%" & restrictImportToFamily & "%' or sysfamily = '' or sysfamily is null or sysfamily='E2610') and active=1"
        'sql$ &= "WHERE active=1"

        Dim nextBid As Integer = 0
        Dim nextProdID As Integer = 0

        Dim tlOptions As clsTranslation = iq.AddTranslation("Options", English, "cats", 0, Nothing, 0, False)
        Dim tlOption As clsTranslation = iq.AddTranslation("Option", English, "cats", 0, Nothing, 0, False)

        'Write caches (for MUCH faster bulk writes)
        Dim pawc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")
        Dim bwc As DataTable = da.MakeWriteCacheFor(con, "branch", nextBid, True) 'nextID is SET by this call !

        Dim twc As DataTable = da.MakeWriteCacheFor(con, "Translation")
        Dim pwc As DataTable = da.MakeWriteCacheFor(con, "Product", nextProdID, True)
        Dim nextKey As Integer = clsTranslation.NextKey()

        Dim tlacs As clsTranslation = iq.AddTranslation("Accessories and Services", English, "cat", 0, twc, nextKey, False)
        Dim allOptions As clsBranch = New clsBranch(Nothing, iq.RootBranch, tlacs, "/images/iq/accSvcs.gif", tlOptions, tlOption, Nothing, 0, False, "B", bwc, nextBid)
        options2.Add("ALL", allOptions)

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, sql$)

        Dim ldic As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)

        Dim l1Branch As clsBranch
        Dim l2Branch As clsBranch
        Dim l3Branch As clsBranch
        Dim l4Branch As clsBranch

        Dim addTo As clsBranch

        Dim options As Integer = 0
        While rdr.Read

            Dim ck As String = rdr.Item("l1").trim
            If rdr.Item("optsku") = "###16MB_FB_128MB_SD_2MB" Then 'Can remove
                Dim a = 1
            End If

            If Not ldic.ContainsKey(ck) Then
                l1Branch = New clsBranch(Nothing, allOptions, iq.AddTranslation(rdr.Item("l1"), English, "OL1", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "B", bwc, nextBid)
                ldic.Add(ck, l1Branch)
            Else
                l1Branch = ldic(ck)
            End If

            addTo = Nothing

            ck = rdr.Item("l1").trim & "^" & rdr.Item("l2").trim
            If Not ldic.ContainsKey(ck) Then
                l2Branch = New clsBranch(Nothing, l1Branch, iq.AddTranslation(rdr.Item("l2"), English, "OL2", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "BGT", bwc, nextBid)
                ldic.Add(ck, l2Branch)
            Else
                l2Branch = ldic(ck)
            End If
            addTo = l2Branch

            If rdr.Item("l3") IsNot DBNull.Value Then
                ck = rdr.Item("l1").trim & "^" & rdr.Item("l2").trim & "^" & rdr.Item("l3").trim
                If Not ldic.ContainsKey(ck) Then
                    l3Branch = New clsBranch(Nothing, l2Branch, iq.AddTranslation(rdr.Item("l3"), English, "OL3", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "BGT", bwc, nextBid)
                    ldic.Add(ck, addTo)
                Else
                    l3Branch = ldic(ck)
                End If
                addTo = l3Branch
            End If

            'optfamily is not globally unique... 5.25lff drives appear in optical and HDD
            Dim optfam = rdr.Item("optFamily") ' is 'L4'

            Dim l3t As String = ""
            If rdr.Item("l3") IsNot DBNull.Value Then l3t = rdr.Item("l3").trim.tolower
            ck = rdr.Item("l1").trim & "^" & rdr.Item("l2").trim & "^" & l3t & "^" & optfam.trim
            If Not ldic.ContainsKey(ck) Then
                Dim txt$ = ""
                If dicAbbreviations.ContainsKey(optfam) Then txt = dicAbbreviations(optfam) Else txt = Replace(txt$, "_", " ")
                If txt Is Nothing Then txt = ""
                l4Branch = New clsBranch(Nothing, addTo, iq.AddTranslation(txt, English, "OL4", 0, twc, nextKey, False), "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "G", bwc, nextBid)
                ldic.Add(ck, l4Branch)
            Else
                l4Branch = ldic(ck)
            End If

            If Not options2.ContainsKey(ck) Then
                If ck.ToLower.Contains("amd socket") Then Stop
                options2.Add(ck, l4Branch)
            End If

            Dim otc As String = rdr.Item("opttype")  'these are broad
            Dim otc2 As String = If(IsDBNull(rdr.Item("opttype2")), "", rdr.Item("opttype2")) 'ML horrid but don't understand opttype2 and its causing data categorization issues for cables, even giving them a W value
            If otc2 = "CAB" Then otc = "CAB"

            Dim OptionProduct As clsProduct = Nothing

            If iq.i_ProductType_Code.ContainsKey(otc) Then
                Dim pt As clsProductType = iq.i_ProductType_Code(otc)
                Dim af As Date = CDate("01/01/1980")
                Dim at As Date = CDate("01/01/2400")

                If Not IsDBNull(rdr.Item("activeFromDate")) Then af = rdr.Item("activeFromDate")
                If Not IsDBNull(rdr.Item("activeToDate")) Then at = rdr.Item("activeToDate")

                OptionProduct = New clsProduct(rdr.Item("optsku"), False, True, iq.Sectors.Values(0), pt, af, at, rdr.Item("active"), rdr.Item("eol"), Not rdr.Item("AAonly"), "", "", "", pwc, nextProdID)

                Dim TLdesc As clsTranslation = Nothing
                If Not IsDBNull(rdr.Item("ccDescription")) Then

                    Dim dsc$ = rdr.Item("ccdescription")
                    If rdr.Item("ccDescription").tolower.contains("amd cpu") Then Stop

                    TLdesc = iq.AddTranslation(rdr.Item("ccDescription"), English, "OPTDSC", 0, twc, nextKey, False)
                    Dim optionbranch As clsBranch = New clsBranch(OptionProduct, l4Branch, TLdesc, "", tlOptions, tlOption, Nothing, rdr.Item("sortorder"), False, "B", bwc, nextBid)
                    addOptionAttributes(OptionProduct, pawc, twc, nextKey, rdr, dicplcode, dicUnits, TLdesc)
                Else
                    Logit("Missing description")
                End If
            Else
                Logit("Missing opttype:" & otc)
            End If

            'Supply Chain Focus Attribute
            If Not IsDBNull(rdr.Item("optSC")) Then
                Dim optsc As String = Trim(rdr.Item("optsc"))
                If optsc <> "" And optsc <> "Z" Then
                    Dim SCfa As clsProductAttribute = New clsProductAttribute(OptionProduct, iq.i_attribute_code("focus"), 0, iq.i_unit_code("txt"), dicSC(optsc), pawc)
                End If
            End If

            'systypefocus attribute

            options += 1

            'Populate the Dictionary of option localisations (Countries in which it's active) - which is used later in BuildTree
            'we DO NOT add options which are not localised to the dicoptlocalisation dictionary (they are unrestricted)


            Dim rgns As String = ""
            If Not IsDBNull(rdr.Item("localisation")) Then rgns = rdr.Item("localisation")

            If rdr.Item("aaonly") <> 0 Then
                rgns &= ",AA"
            End If


            If rgns <> "" Then
                If OptionProduct IsNot Nothing Then

                    Dim regions As List(Of clsRegion) = New List(Of clsRegion)
                    Dim cs As List(Of String) = Split(rgns, ",").ToList

                    If Not cs.Contains("XW") Then   'Anything paul has localised 'worldwide' needs no restriction

                        cleanRegions(cs, containment)
                        For Each c In cs

                            If c = "UCSA" Then c = "USCA" 'fix a typo
                            If iq.i_region_code.ContainsKey(c) Then
                                regions.Add(iq.i_region_code(c))
                            Else
                                Logit("invalid region " & c & " (in products.options.localisation)")
                                '    Stop
                            End If
                        Next
                        dicOptLocalisation.Add(OptionProduct, regions)
                    End If
                End If
            End If
        End While

        rdr.Close()

        da.BulkWrite(con, twc, "translation", , True)
        da.BulkWrite(con, pwc, "product", , True)
        da.BulkWrite(con, bwc, "branch", , True)
        da.BulkWrite(con, pawc, "productattribute")

        con.Close()

        Dim sw As StreamWriter = New StreamWriter("c:\temp\allOptions.txt")
        allOptions.toDisk(sw, 0, "")
        sw.Close()

    End Function

    Public Sub DoPrunes()


        'Options are generally set a compatible with broad families - (sysfamilyName)
        'however, certain option types (HDD,MEM etc) - have further contstraints based on the more granular (and badly named) optFamily (eg.25SFFNHPHDD)
        'and the equally confusing  sysfamilyCode  (which i call Minor Family)


        Dim nextpruneid As Integer = 0

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim pwc As DataTable = da.MakeWriteCacheFor(con, "prune", nextpruneid)

        Dim dic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamilyOptTypeToOptFamily

        Dim kept As Integer = 0
        Dim pruned As Integer = 0

        iq.RootBranch.DoPrunes(pwc, nextpruneid, "tree.1", "", dic, kept, pruned)

        Dim rows As Integer = pwc.Rows.Count

        da.BulkWrite(con, pwc, "Prune", 10000, True)

        con.Close()

    End Sub

    Public Function SoftwareSlots()

        'make gives slot on systems for a qty record on all software options

        'Build a dictioanry of FamilyMinor code >List(of systems)
        Dim families As Dictionary(Of String, List(Of clsBranch)) = New Dictionary(Of String, List(Of clsBranch))(StringComparer.CurrentCultureIgnoreCase)
        For Each b In iq.Branches.Values
            If b.Product IsNot Nothing Then
                If b.Product.isSystem Then
                    Dim famname As String = b.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English)
                    If Not families.ContainsKey(famname) Then
                        families.Add(famname, New List(Of clsBranch))
                    End If
                    families(famname).Add(b)

                    famname = b.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English)
                    If Not families.ContainsKey(famname) Then
                        families.Add(famname, New List(Of clsBranch))
                    End If
                    families(famname).Add(b)

                End If
            End If
        Next

        Dim sql$ = "SELECT [SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref] from h3.[iq].[products].[OptionLimits] "
        sql$ &= "INNER join h3.[iq].[products].[opttypes] o on o.OptTypeCode = opttype "
        sql$ &= "where opttype like 'sof1%'"

        Dim con As SqlClient.SqlConnection = da.OpenDatabase


        Dim swc As DataTable = da.MakeWriteCacheFor(con, "Slot")
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)

        Dim systems As List(Of clsProduct) = New List(Of clsProduct)

        Dim duds As New List(Of String)

        While rdr.Read
            Dim sf As String = rdr.Item("sysfamily")
            If families.ContainsKey(sf) Then
                For Each SysBranch In families(sf)
                    If Not SysBranch.hasSlot(iq.i_slotType_Code("SOF")("OPERATING_SYSTEMS")) Then
                        Dim aslot As clsSlot = New clsSlot(iq.i_slotType_Code("SOF1")("OPERATING_SYSTEMS"), SysBranch, "", rdr.Item("qtymax"), Nothing, New NullableInt, 0, 0, swc)
                        Dim aslot2 As clsSlot = New clsSlot(iq.i_slotType_Code("SOF2")("OPERATING_SYSTEMS"), SysBranch, "", rdr.Item("qtymax"), Nothing, New NullableInt, 0, 0, swc)
                        ' Dim aslot3 As clsSlot = New clsSlot(iq.i_slotType_Code("SOF3")("OPERATING_SYSTEMS"), SysBranch, "", rdr.Item("qtymax"), Nothing, New NullableInt, 0, 0, swc)
                    End If
                Next
            Else
                If Not duds.Contains(sf) Then
                    duds.Add(sf)
                End If
            End If

        End While

        rdr.Close()


        Dim rc As Integer = swc.Rows.Count

        da.BulkWrite(con, swc, "Slot")

        con.Close()



    End Function

    'Do we need this?? ML

    Public Sub chassisMemSlots()

        Dim sql$
        sql$ = "SELECT ol.[SysFamily],isnull(optTypeParent,'Miscellaneous') as optCat,[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref],familymem  from h3.[iq].[products].[OptionLimits] ol "
        sql$ &= "INNER join h3.[iq].[products].[opttypes] o on o.OptTypeCode = ol.opttype "
        sql$ &= "join h3.iq.products.sysfamilydefinitions sfd on ol.SysFamily = sfd.sysfamily "
        sql$ &= "where opttype like 'mem'"


        'build a dictionary of all the systems in every major and minor family
        Dim families As Dictionary(Of String, List(Of clsBranch)) = New Dictionary(Of String, List(Of clsBranch))(StringComparer.CurrentCultureIgnoreCase)
        For Each b In iq.Branches.Values
            If b.Product IsNot Nothing Then
                If b.Product.isSystem And Not b.Product.isOption Then
                    Dim famname As String = b.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English)
                    If Not families.ContainsKey(famname) Then
                        families.Add(famname, New List(Of clsBranch))
                    End If
                    families(famname).Add(b)

                    'famname = b.Product.i_Attributes_Code("FamMajor")(0).Translation.text(English)
                    'If Not families.ContainsKey(famname) Then
                    '    families.Add(famname, New List(Of clsBranch))
                    'End If
                    'families(famname).Add(b)

                End If
            End If
        Next

        Dim con As SqlClient.SqlConnection = da.OpenDatabase


        Dim swc As DataTable = da.MakeWriteCacheFor(con, "Slot")
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)

        Dim systems As List(Of clsProduct) = New List(Of clsProduct)

        Dim duds As New List(Of String)

        Dim added As Integer = 0

        While rdr.Read
            Dim sf As String = rdr.Item("sysfamily") 'this is the granular (minor) one
            If families.ContainsKey(sf) Then
                For Each SysBranch In families(sf)

                    Dim found As Boolean = False
                    For Each branch In SysBranch.childBranches.Values
                        If branch.Translation.text(English).ToLower.Contains("chassis") Then
                            If Not branch.HasMajorSlot("MEM") Then
                                'If Not SysBranch.hasSlot(iq.i_slotType_MinorCode("OPERATING_SYSTEMS")) Then
                                If rdr.Item("familymem") IsNot DBNull.Value Then
                                    Dim fm As String = rdr.Item("familyMem")
                                    branch.i_Slots.Clear() 'MAKE SURE WE MAKE IT !
                                    Dim aslot As clsSlot = New clsSlot(iq.i_slotType_Code("MEM")(fm), branch, "", rdr.Item("qtymax"), Nothing, New NullableInt, 0, 0, swc)
                                    added += 1
                                    found = True

                                    'End If
                                End If
                            End If
                        End If
                    Next
                    '  If Not found Then Stop

                    Exit For 'we only need do this for the first branch in the family as they all share a chassis
                Next
            Else
                If Not duds.Contains(sf) Then
                    duds.Add(sf)
                End If
            End If

        End While

        rdr.Close()


        Dim rc As Integer = swc.Rows.Count

        da.BulkWrite(con, swc, "Slot")

        Debug.Print(added)

        con.Close()



    End Sub


    Public Function Buildtree2(con As SqlClient.SqlConnection, ProductCat As Dictionary(Of String, clsBranch), dicfamilies As Dictionary(Of String, clsBranch), dicsystems As Dictionary(Of String, clsBranch), dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)))

        'NB: in Iq1 a 'family' is a 'narrow/specific' designation - and type is broad (major type)
        'This *always* confuses me

        'Products.options tell us which (broad) families an option is available under 
        'however ! .. Option limits are specified per narrow sysfamily
        'so we graft the same set of options to every system in a broad family - but apply limits at the narrower level


        Dim ERRORMESSAGES As List(Of String) = New List(Of String)

        Dim kept As Integer = 0
        Dim pruned As Integer = 0

        Dim dicSlotTypes As Dictionary(Of String, clsSlotType)

        'Return a dictionary of minorSlot Type codes to slot types (needs systems dictionary because of some PCI stuff)
        dicSlotTypes = Import.slotTypes(con, dicsystems) 'dicFamily) '20 secs

        'Build a dictionary to look up the slot type per minorFamily/option type
        '                                           minorfamily            option type
        'Dim dicSubFamOptTypeSlotType As Dictionary(Of String, Dictionary(Of String, clsSlotType))
        'dicSubFamOptTypeSlotType = Import.SubFamiliyOptionTypes(con, dicSlotTypes)


        'Returns a dictionary of option type > optionfamily - per systemfamilyCODE (minorFamilty)
        'eg              dl580pg825NHPlff > HDD > NHP35LFFSC
        Dim ofDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamilyOptTypeToOptFamily()  'Gives a lookup of narrow optfamily from BroadOptType per sysfamily

        'OPTION LIMITS 
        'build a dictionary by sysfamily/option family of the Limits - used later to attach instances of clsQuantity (autoAdds/Preinstalled) to the option branches

        '    '                                                  sysSubFam      optionfaimily       limit
        '    '                                                                       NHP35lff
        Dim dicOptLimits As Dictionary(Of String, IQ.clsLimit)
        'returns a dictinoary of the narrow,minor sysFamilyCode^optfamily>clslimit EG.. DL580PLFF^NHPLFF>blah
        dicOptLimits = Import.BuildOptLimits(con, ofDic)

        writeDicOptLimits(dicOptLimits, "c:\temp\optlimits.txt")

        ' FACTORY INSTALLED OPTIONS/components - call them what you will
        ' get a list (by system mfrSKU) of the part numbers and quantities of all factory installed components (PriStor, sec stor CPU, MEM etc,Raid)

        Dim dicFIOs As Dictionary(Of String, Dictionary(Of String, Integer))
        'returns a list (by system mfrSKU) of the quantities and part numbers of all factory installed components (PriStor, sec stor CPU, MEM etc)
        dicFIOs = Import.FIOs(con)

        WriteDicFIOs(dicFIOs, "c:\temp\fios.txt")


        Dim NEXTbId As Integer = 0
        Dim nextpruneid As Integer = 0

        Dim bwc As DataTable = da.MakeWriteCacheFor(con, "branch", NEXTbId, True) 'nextID is SET by this call !
        Dim Gwc As DataTable = da.MakeWriteCacheFor(con, "GRAFT")
        Dim qwc As DataTable = da.MakeWriteCacheFor(con, "quantity")
        Dim swc As DataTable = da.MakeWriteCacheFor(con, "slot")
        Dim pwc As DataTable = da.MakeWriteCacheFor(con, "prune", nextpruneid)

        Dim nextkey As Integer = clsTranslation.NextKey
        Dim tlwc As DataTable = da.MakeWriteCacheFor(con, "Translation")

        Dim sql$

        sql$ = "SELECT v.OptSN,po.optsku,v.sortorder,"
        sql$ &= "speedUnitQty as speed,optTypeSpeedUnit as speedUnit,sysfamily,"
        sql$ &= "unitQty as capacity,ot.optTypeUnit as capacityUnit,"
        sql$ &= "technology,altsku,incompatible,v.L1,v.L2,v.L3, po.optfamily,po.opttype,activeFromDate,activeToDate,active,eol,aaonly,descriptionHP,slots,ccdescription "
        sql$ &= "FROM h3.iq.products.V2_OptionCats v "
        sql$ &= "JOIN h3.iq.products.options po ON v.optsn=po.optsn "
        sql$ &= "JOIN h3.[iq].products.optTypes as OT on OT.optTypeCode=optType "
        sql$ &= "JOIN h3.[channelcentral].products.Hierarchy h ON h.upcNUM = po.optsku "
        sql$ &= "WHERE (sYSFAMILY LIKE '%" & restrictImportToFamily & "%' or sysfamily = '' or sysfamily is null) and active=1 order by sysfamily"
        'sql$ &= "where active=1 order by sysfamily"

        Dim rdr = da.DBExecuteReader(con, sql$)

        sql$ = "SELECT * FROM h3.iQ.products.SysFamilyDefinitions"
        Dim FamilyOptionDefs As DataTable
        FamilyOptionDefs = da.FilledDataTable(con, sql$)

        'CK branches contians an 'all options' branch for every family (and holds references to every sub-branch to 
        Dim ckBranches As Dictionary(Of String, clsBranch) 'Compound key of Sysfamily^l1^l2^l3>Branch
        ckBranches = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)
        Dim tlOptions As clsTranslation = iq.AddTranslation("Options", English, "collect", 0, tlwc, nextkey, False)
        Dim tlOption As clsTranslation = iq.AddTranslation("Option", English, "collect", 0, tlwc, nextkey, False)
        Dim allOptions As clsBranch

        ' this is the All options branch for each family (we will prune incompatibles)
        Dim carepacks As New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase) 'record the carepack branch under each Broad Family name

        Dim tlcarepack As clsTranslation = iq.AddTranslation("Care Pack", English, "cats", 0, tlwc, nextkey, False)
        Dim tlcarepacks As clsTranslation = iq.AddTranslation("Care Packs", English, "cats", 0, tlwc, nextkey, False)

        While rdr.Read

            ' WRONG - >>>>>>>>for every option - add its parent branch (ie, it, and all its sibilings) to the family branch.. if it's not there already
            ' the optfamily must match the sysfamily defintions FamilyPriStro,Secstop for optical and HDDS
            ' TODO - If rdr.Item("optfamily") = dicfamilies(rdr.option("sysFamilies").product) Then
            ' what we'ere actually doing, is constructing an all options branch per (broad) sysfamily

            Dim l3$ = ""

            If rdr.Item("l3") IsNot DBNull.Value Then
                l3 = rdr.Item("l3")
            End If

            Dim optfamily As String = rdr.Item("Optfamily") ' CAREPACK

            If rdr.Item("sysfamily") IsNot DBNull.Value Then  ' This is a CD list of broad families eg DL580PG8

                Dim sf() = Split(rdr.Item("sysfamily"), ",")


                For Each f In sf
                    Dim ck As String
                    ck = f & "^" & rdr.Item("l1").trim & "^" & rdr.Item("l2").trim & "^" & l3.Trim ' & "^" & optfamily.Trim

                    If (f.ToUpper.Contains(restrictImportToFamily.ToUpper)) Or String.IsNullOrEmpty(restrictImportToFamily) Then  ''switch between OR True and OR False to restirct what's imported
                        If dicfamilies.ContainsKey(f) Then
                            Dim fambranch As clsBranch = dicfamilies(f) ' these are broad families - possible options are defined at the broad level (option limits are defined at the narrower familycode level)
                            Dim holder As clsBranch = makeholder(rdr, ckBranches, f, tlwc, nextkey, bwc, NEXTbId, tlOptions, tlOption)

                            Dim opttype As String = rdr.Item("Opttype")

                            If opttype.ToUpper.Trim = "CPU" Then

                                ' CPU's are handled very differently - see import.cpus
                                ' (only the CPU preinstalled in the system is an option for it - and CPUs enable banks of memory etc)
                            Else

                                Dim optsku As String = rdr.Item("optSKU")
                                Dim anOption As clsProduct = iq.i_SKU(optsku)

                                Dim SKUTL As clsTranslation = iq.AddTranslation(anOption.SKU, English, "SKU", 10, Nothing, 0, False)

                                ' The branch.translation is the part number (Points to the same TL)
                                Dim branch As clsBranch = New clsBranch(anOption, holder, SKUTL, _
                                                                        "", tlOption, tlOptions, Nothing, 0, False, "B", bwc, NEXTbId)

                            End If
                        End If
                    Else

                    End If

                Next
            End If

        End While

        rdr.Close()
        Dim done As Integer = 0

        Dim invalidSlotTypes As New List(Of String)

        Dim chassisBranch As clsBranch
        '        Dim chassisVariant As clsVariant
        '        Dim chassisProduct As clsProduct
        Dim chassisRoot As clsBranch
        Dim chassisTL As clsTranslation = iq.AddTranslation("Chassis", English, "", 0, tlwc, nextkey, False)
        '  Dim chassisProductType As clsProductType = (From j In iq.ProductTypes.Values Where j.Code = "CHAS").First

        'BROAD FAMILIES - but we're grafting onto systems

        Dim chassis As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)

        Dim grafted As Integer = 0

        For Each f In ckBranches.Keys

            If Not f.Contains("^") Then

                'create a flat dictionary of the options, by SKU
                'SKU>OptionPath  (it's path under the system)

                If (f.ToUpper.Contains(restrictImportToFamily.ToUpper)) Or String.IsNullOrEmpty(restrictImportToFamily) Then 'switch between OR True and OR False to restirct what's imported

                    'For debug - 
                    Dim sw As StreamWriter = New StreamWriter("c:\temp\FAMS\" & f & ".txt")
                    ckBranches(f).toDisk(sw, 0, "")
                    sw.Close()

                    Dim aoBranch As clsBranch = ckBranches(f)
                    Dim optionPaths As Dictionary(Of String, String) = aoBranch.OptionPaths("." & CStr(aoBranch.ID)) 'NOTE - theses are the paths below the system

                    'find the paths (relative to the system) of the (previously 'tagged') optfamily/holder Branches
                    '                            tag>Path
                    '  Dim ofpths As Dictionary(Of String, String) = aoBranch.TaggedPaths("." & CStr(aoBranch.ID))

                    If Not dicfamilies.ContainsKey(f) Then
                        Beep()
                    Else
                        Dim fb As clsBranch = dicfamilies(f)
                        Dim firstinfamily As Boolean = True

                        'chassisProduct = New clsProduct(False, )
                        'chassisBranches.Add(sysSubFamily, chassisBranch)

                        '                    'Systems Name (is SKU)
                        '                    'Dim n As clsProductAttribute = New clsProductAttribute(chassisProduct, iq.i_attribute_code("~ame"), 0, iq.i_unit_code("txt"), dicFamily(sysfamkey).Translation, ProductAttributeWriteCache)
                        '                    Dim cq As clsQuantity = New clsQuantity(r_worldwide, "", chassisBranch, 1, 0, 0, True, QuantityWriteCache) 'make a global auto add - to add the chassis to the system
                        '                    'all the gives slots are in the chassis (for now)
                        '                    ' MakeGivesSlots(chassisBranch, dicOptLimits(sysSubFamily), dicSlotTypes, dicSubFamOptTypeSlotType, QuantityWriteCache, SlotWriteCache)

                        'DicOptLimits sysFamilyCode^optfamily>clslimit EG.. DL580PLFF^NHPLFF>blah

                        For Each systembranch In fb.childBranches.Values

                            'were grafting on at root.sector.family.system

                            Dim systempath$
                            With systembranch
                                systempath = "tree." & .Parent.Parent.Parent.ID & "." & .Parent.Parent.ID & "." & .Parent.ID & "." & .ID
                            End With


                            For Each child In systembranch.childBranches.Values
                                If child.Translation.text(English) = "All Options) Then" Then
                                    Stop 'ut oh - wer'e trying to graft it on twice !
                                End If
                            Next

                            Dim systemsku$ = systembranch.Product.SKU

                            systembranch.Graft(ckBranches(f), "buildtree2", "", ERRORMESSAGES, Gwc)
                            grafted += 1

                            Dim sysMinorFamily As String  'comes from the iq.systems.familycode
                            sysMinorFamily = iq.i_SKU(systemsku).i_Attributes_Code("FamMinor")(0).Translation.text(English)  'IMPORTANT for compatibility
                            If sysMinorFamily = "" Then Stop

                            If chassis.ContainsKey(sysMinorFamily) Then
                                chassisBranch = chassis(sysMinorFamily)
                            Else
                                'chassisProduct = New clsProduct("", False, False, iq.i_sector_code("HPPSG"), iq.i_ProductType_Code("CHAS"), DateAdd(DateInterval.Day, -500, Now), DateAdd(DateInterval.Day, 10000, Now), True, False, True, "", "", "")
                                'chassisVariant = New clsVariant("", chassisProduct, HP, chassisProduct.ID.ToString, "", "", "", r_worldwide, 0) 'Every product needs a variant - so it can be stored in a QuoteItem
                                chassisBranch = New clsBranch(Nothing, Nothing, iq.AddTranslation(f & " chassis", English, "UI", 0, tlwc, nextkey, False), "", chassisTL, chassisTL, Nothing, 100, True, "B", bwc, NEXTbId)


                                'chassis branch needs to be per MinorFamily !!
                                'Gives Slots
                                Dim gslot As clsSlot
                                For Each k In dicOptLimits.Keys
                                    Dim bits() As String = Split(k, "^")
                                    If LCase(bits(0)) = LCase(sysMinorFamily) Then
                                        'for every narrow OptFamily in the sysfamily
                                        Dim Limit As clsLimit = dicOptLimits(k)



                                        If UCase(bits(1)) = "FAMILYMEM" Then Stop
                                        Dim optfamily As String = bits(2)
                                        Dim opttype As String = bits(1)

                                        'If Left(optfamily, 3).ToLower = "mem" Then Stop
                                        If iq.SlotTypes.Values.Where(Function(sst) sst.MajorCode = opttype And sst.MinorCode = optfamily).Count > 0 Then
                                            ' iq.i_slotType_MinorCode.ContainsKey(optfamily) Then

                                            Dim st As clsSlotType = iq.SlotTypes.Values.Where(Function(sst) sst.MajorCode = opttype And sst.MinorCode = optfamily).First
                                            '                                            If st.MajorCode = "MEM" Then Stop

                                            'the gives stos do NOT need a path (systempath & "." & chassisBranch.ID) - becuase they are active weherever this subchassis appears
                                            gslot = New clsSlot(st, chassisBranch, "", Limit.Qmax, Nothing, New NullableInt, Limit.Qmin, 0, swc)
                                        Else
                                            'Weird ones like PSUm which is in option limits as PSU??? why                                            
                                            ' If invalidSlotTypes Is Nothing Then
                                            If Not invalidSlotTypes.Contains(sysMinorFamily & "^" & opttype & "^" & optfamily) Then
                                                invalidSlotTypes.Add(sysMinorFamily & "^" & opttype & "^" & optfamily)
                                            End If
                                            'End If
                                        End If
                                        '
                                        '     MakeGivesSlots(chassisBranch, dicOptLimits(sysSubFamily), dicSlotTypes, dicSubFamOptTypeSlotType, QuantityWriteCache, SlotWriteCache)
                                    End If
                                Next
                                chassis.Add(sysMinorFamily, chassisBranch)
                            End If
                            systembranch.Graft(chassisBranch, "", "", ERRORMESSAGES, Gwc)
                            'makes autoAdds (quantities) for FIOs, Takes slots 
                            ' and prunes incompatible option branches (by thier narrow sysfamilies)
                            If dicFIOs.ContainsKey(systembranch.Product.SKU) Then ' Double check to see if we have a part in dicFIO's which is NOT in all options, should never happen
                                For Each s In dicFIOs(systembranch.Product.SKU).Keys
                                    'Do we have a slot for this FIO???
                                    If Not optionPaths.ContainsKey(s) Then
                                        'Add it somewhere here??? Todo ML
                                        If s = "###16MB_FB_128MB_SD_2MB" Then
                                            Dim a = 9
                                        End If
                                        Dim fioPath As String = systempath
                                        If iq.i_SKU.ContainsKey(s) Then
                                            Dim branch As clsBranch = New clsBranch(iq.i_SKU(s), systembranch.FindBranchByNameBelow("FIOs", fioPath, False, 0), iq.AddTranslation(s, English, "", 0, Nothing, 0, False), _
                                                                        "", tlOption, tlOptions, Nothing, 0, False, "B", bwc, NEXTbId)
                                            isFIO(s, systembranch.Product.SKU, fioPath & "." & branch.ID, dicFIOs, dicOptLocalisation, New clsLimit(dicFIOs(systembranch.Product.SKU)(s), 0, dicFIOs(systembranch.Product.SKU)(s), Nothing, Nothing), qwc)

                                        End If

                                    End If
                                Next
                            End If

                            For Each optionsku In optionPaths.Keys
                                If optionsku = "###16MB_FB_128MB_SD_2MB" Then
                                    Dim a = 1
                                End If
                                MakeLimits(systempath, optionsku, optionPaths(optionsku) _
                                           , Gwc, swc, pwc, nextpruneid, qwc, firstinfamily, _
                                           dicOptLimits, dicSlotTypes, dicOptLocalisation, _
                                           dicFIOs, systemsku, kept, pruned, chassisBranch, systembranch, FamilyOptionDefs)
                            Next
                            'If dicFIOs.ContainsKey(systembranch.Product.sku) Then ' Double check to see if we have a part in dicFIO's which is NOT in all options, should never happen
                            '    For Each s In dicFIOs(systembranch.Product.sku).Keys
                            '        'Do we have a slot for this FIO???


                            '        If Not optionPaths.ContainsKey(s) Then
                            '            'ML - maybe need to add these to an FIO branch under the system / family? These are things like RAM for HPN
                            '            isFIO(s, systembranch.Product.sku, systempath, dicFIOs, dicOptLocalisation, New clsLimit(dicFIOs(systembranch.Product.sku)(s), 0, dicFIOs(systembranch.Product.sku)(s), Nothing, Nothing), qwc)
                            '            Logit("Preinstalled part found but no match in all options: " & systemsku & ":" & s)
                            '            '            Dim a = 1 
                            '            '            'Add optionfi

                            '            '            '  MakeLimits(systempath, s, Nothing _
                            '            '            '        , Gwc, swc, pwc, nextpruneid, qwc, firstinfamily, _
                            '            '            '        dicOptLimits, dicSlotTypes, dicOptLocalisation, _
                            '            '        dicFIOs, systemsku, kept, pruned)
                            '        End If
                            '    Next
                            'End If
                            'prune Optical and HDD drives which are not compatible with their minorFamilies
                            'locate the optfamily branches

                            'Dim syspruned As Integer = 0

                            'If systembranch.Product.i_Attributes_Code.ContainsKey("PriStor") Then
                            '    Dim pristor As String = systembranch.Product.i_Attributes_Code("PriStor")(0).Translation.text(English)

                            '    For Each k In ofpths.Keys 'there are the paths of the nodes to which we graft optyfamily branches - (like NHP35LFF)
                            '        Dim bits() = Split(k, "^")
                            '        If bits(0) = "HDD" And bits(1) <> pristor And bits(1) <> "" Then
                            '            Dim pruneat As String = systempath & ofpths(k)
                            '            Dim jj As String = iq.Branches(2084).DisplayName(English)
                            '            Dim aprune = New clsPrune(pruneat, New NullableInt, "import", pwc, nextpruneid)
                            '            syspruned += 1
                            '        End If
                            '    Next
                            'End If

                            '   If syspruned > 0 Then
                            Dim syssku$ = systembranch.SKU
                            Dim ssw As StreamWriter = New StreamWriter("c:\temp\SYSTEMS\" & syssku & ".txt")
                            systembranch.toDisk(ssw, 0, systempath)
                            ssw.Close()
                            'End If

                            '        firstinfamily = False

                        Next
                        done += 1

                    End If
                End If
            End If 'Only do DL's - REMOVE
        Next 'family

        Debug.Print(grafted)

        da.BulkWrite(con, qwc, "quantity")
        da.BulkWrite(con, bwc, "Branch", , True)
        da.BulkWrite(con, Gwc, "Graft")
        da.BulkWrite(con, tlwc, "translation")
        da.BulkWrite(con, swc, "slot")
        da.BulkWrite(con, pwc, "prune", , True)

        con.Close()

        Logit("These slot types were invalid")
        For Each s In invalidSlotTypes
            Logit(s)
        Next
        Logit("End of list", False, True)


    End Function

    Private Function makeholder(rdr As SqlClient.SqlDataReader, ByRef ckBranches As Dictionary(Of String, clsBranch), _
                                famname As String, tlwc As DataTable, ByRef nextkey As Integer, bwc As DataTable, ByRef nextbid As Integer, tloptions As clsTranslation, tloption As clsTranslation, Optional outPath As String = "") As clsBranch

        'makes (or returns) a bottom level category branch to which we attach options

        Dim FamAlloptBranch As clsBranch, l1branch, l2branch, l3branch, FIOBranch As clsBranch
        Dim order As Integer = rdr.Item("sortorder")
        '   Dim alloptions As clsBranch

        Dim ck$

        If ckBranches Is Nothing Then ckBranches = New Dictionary(Of String, clsBranch)(StringComparer.CurrentCultureIgnoreCase)

        With ckBranches
            If Not .ContainsKey(famname) Then
                Dim tall As clsTranslation = iq.AddTranslation("All Options", English, "UI", 20, tlwc, nextkey, False)
                'An all options branch is created for each family (hanging in space) 
                'it is subsequently grafted on to every system in the family
                '(and pruned off those subFamilies with which it is incompatible)
                Dim tbranch As clsBranch = New clsBranch(Nothing, Nothing, tall, "", tloption, tloptions, Nothing, 20, False, "B", bwc, nextbid)
                .Add(famname, tbranch)
            End If

            FamAlloptBranch = ckBranches(famname)
            outPath &= "." & FamAlloptBranch.ID

            ck$ = famname & "^FIOs"
            If Not .ContainsKey(ck) Then
                FIOBranch = New clsBranch(Nothing, FamAlloptBranch, iq.AddTranslation("FIOs", English, "FIO", 0, tlwc, nextkey, False), "", tloptions, tloption, Nothing, order, True, "H", bwc, nextbid)
                .Add(ck, FIOBranch)
            End If
            FIOBranch = ckBranches(ck)

            'Level branches (1 to 3/4) for option in one family

            ck$ = famname & "^" & rdr.Item("l1")
            If Not .ContainsKey(ck) Then
                l1branch = New clsBranch(Nothing, FamAlloptBranch, iq.AddTranslation(rdr.Item("l1"), English, "OL1", 0, tlwc, nextkey, False), "", tloptions, tloption, Nothing, order, False, "B", bwc, nextbid)
                .Add(ck, l1branch)
            End If
            l1branch = ckBranches(ck)
            outPath &= "." & l1branch.ID

            ck = famname & "^" & rdr.Item("l1") & "^" & rdr.Item("l2")
            If .ContainsKey(ck) Then
                l2branch = ckBranches(ck)
            Else
                l2branch = New clsBranch(Nothing, l1branch, iq.AddTranslation(rdr.Item("l2"), English, "OL2", 0, tlwc, nextkey, False), "", tloptions, tloption, Nothing, order, False, "B", bwc, nextbid)
                .Add(ck, l2branch)
            End If
            outPath &= "." & l2branch.ID

            Dim l3 As String = ""
            If rdr.Item("l3") IsNot DBNull.Value Then l3 = rdr.Item("l3")

            If l3 = "" Then

                Return l2branch



            Else
                ck = famname & "^" & rdr.Item("l1") & "^" & rdr.Item("l2") & "^" & l3
                If .ContainsKey(ck) Then
                    l3branch = ckBranches(ck)
                Else
                    l3branch = New clsBranch(Nothing, l2branch, iq.AddTranslation(l3, English, "OL3", 0, tlwc, nextkey, False), "", tloptions, tloption, Nothing, order, False, "B", bwc, nextbid)
                    .Add(ck, l3branch)
                End If
                outPath &= "." & l3branch.ID
                Return l3branch
            End If

        End With

    End Function


    Public Function addOptionAttributes(optionProduct As clsProduct, pawc As DataTable, twc As DataTable, ByRef nextKey As Integer, rdr As SqlClient.SqlDataReader, dicplcode As Dictionary(Of String, String), dicunits As Dictionary(Of String, clsUnit), tldesc As clsTranslation)

        Dim incompatible As clsProductAttribute
        Dim altsku As clsProductAttribute
        Dim anAttribute As clsProductAttribute
        Dim mfrsku As clsProductAttribute
        Dim plcode As clsProductAttribute

        ' Dim ssde As clsTranslation = iq.AddTranslation("Solid State Drive", English, "DriveType")
        Dim textUnit As clsUnit = iq.i_unit_code("txt")
        If textUnit Is Nothing Then Stop


        Dim desc As clsProductAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("desc"), 0, textUnit, tldesc, pawc)

        'record the options OptFamily - this is the MinorOption type - but isn't globally unique..
        'eg. HPL35inchLFF may appear under oth OPT and HDD opt types
        anAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("optFamily"), 0, textUnit, iq.AddTranslation(rdr.Item("optfamily"), English, "", 0, twc, nextKey, False), pawc)

        'This IS used in the quote summary (amongst other places)
        If Len(rdr.Item("opttype")) > 5 Then Stop
        anAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("optType"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("optType"), English, "", 0, twc, nextKey, False), pawc)

        'If Not iq.Attributes.ContainsKey("MfrSKU") Then j = New clsAttribute("MfrSKU", New clsText(iq.addTranslation("MfrSKU")))

        Dim speed As clsProductAttribute
        Dim capacity As clsProductAttribute
        If Not IsDBNull(rdr.Item("speed")) Then
            If Not IsDBNull(rdr.Item("speedunit")) Then  'Some things (tape drives/Graphics cards/batteries have 'speeds' without units - we're not imprtiong - mentioned to dan 02/08/2012
                speed = New clsProductAttribute(optionProduct, iq.i_attribute_code("speed"), rdr.Item("speed"), dicunits(rdr.Item("speedUnit")), Nothing, pawc)
            End If
        Else
            If rdr.Item("Opttype") = "HDD" Then
                'HDD's without a speed are SSD's - give them a numerically high RPM (so they sort to the 'top' speed wise - but display the text SSD (instead of 100,000 rpm)
                Dim ssd As clsTranslation = iq.AddTranslation("SSD", English, "DriveType", 0, twc, nextKey, False)
                speed = New clsProductAttribute(optionProduct, iq.i_attribute_code("speed"), 100000, iq.i_unit_code("txt"), ssd, pawc)
            End If
        End If

        If Not IsDBNull(rdr.Item("capacity")) Then

            Dim uk$
            If Not IsDBNull(rdr.Item("capacityUnit")) Then ''Some things (cables,newtork cards have  capacities without units - we're not importing - mentioned to dan 02/08/2012 - am now... with a TXT unit
                uk$ = rdr.Item("capacityUnit")
            Else
                uk$ = "txt"
            End If

            capacity = New clsProductAttribute(optionProduct, iq.i_attribute_code("capacity"), rdr.Item("capacity"), iq.i_unit_code(uk$), Nothing, pawc)

        End If


        If Not IsDBNull(rdr.Item("opttype2")) Then
            Dim ot2 = New clsProductAttribute(optionProduct, iq.i_attribute_code("opttype2"), 0, textUnit, iq.AddTranslation(rdr.Item("opttype2"), English, "", 0, twc, nextKey, False), pawc)
        End If

        Dim optsku As String = rdr.Item("optsku")

        If Not IsDBNull(rdr.Item("technology")) Then
            Dim t$ = rdr.Item("technology")
            Dim cp As Integer
            cp = InStr(t$, "CORE")
            Dim numcores As Integer
            If cp Then
                numcores = Val(Left$(t$, cp - 1))
                '  If numcores = 3 Or numcores = 5 Or numcores = 7 Or numcores > 16 Then Stop 'odd number of cores
                Dim cores As clsProductAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("cores"), numcores, iq.i_unit_code("num"), Nothing, pawc)

                Dim numthreads As Integer
                cp = InStr(t$, "TH")
                If cp Then
                    numthreads = Val(Mid$(t$, cp - 2, 2))
                    Dim threads As clsProductAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("threads"), numthreads, iq.i_unit_code("num"), Nothing, pawc)
                End If
            End If
        End If

        ' mfrsku = New clsProductAttribute(optionProduct, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(Trim$(rdr.Item("OptSKU")), English, "", 0, twc, nextKey, False), pawc)
        Dim pl$

        If Not dicplcode.ContainsKey(rdr.Item("optSKU")) Then
            Logit("No PL code for option '" & rdr.Item("Optsku") & "' (not in HeirarchyIQ).")
        Else
            pl = dicplcode(rdr.Item("optSKU"))
            plcode = New clsProductAttribute(optionProduct, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(pl, English, "", 0, twc, nextKey, False), pawc)
        End If

        'Dim opttype As clsProductAttribute
        'Dim opt$
        'opt$ = rdr.Item("OptType")
        'opttype = New clsProductAttribute(optionproduct, iq.Attributes("OptType"), 0, iq.Units("txt"), iq.addTranslation(opt, twc).Key, awc)
        'End If

        If Not IsDBNull(rdr.Item("incompatible")) Then
            If Trim$(rdr.Item("incompatible")) <> "" Then
                Dim ic$ = Replace(rdr.Item("incompatible"), " ", "")
                incompatible = New clsProductAttribute(optionProduct, iq.i_attribute_code("incompat"), 0, textUnit, _
                iq.AddTranslation(ic$, English, "incompat", 0, twc, nextKey, False), pawc)
            End If
        End If

        If Not IsDBNull(rdr.Item("altsku")) Then
            If Trim$(rdr.Item("altsku")) <> "" Then
                altsku = New clsProductAttribute(optionProduct, iq.i_attribute_code("altSKU"), 0, textUnit, _
                iq.AddTranslation(Replace(rdr.Item("altSKU"), " ", ""), English, "atSKU", 0, twc, nextKey, False), pawc)
            End If
        End If

        'required later when making 'takes' slots - to respect iq.products.options.slots
        Dim slots As clsProductAttribute = New clsProductAttribute(optionProduct, iq.i_attribute_code("Slots"), rdr.Item("slots"), iq.i_unit_code("num"), Nothing, pawc)
        'Dont do this for PSU enablement kits, they dont take a PSU slot....
        If Not IsDBNull(rdr.Item("technology")) AndAlso rdr.Item("technology") = "UPGRADE" Then
            slots.NumericValue = 0
        End If

        If Not IsDBNull(rdr.Item("technology")) Then
            Dim tech = New clsProductAttribute(optionProduct, iq.i_attribute_code("technology"), 0, textUnit, _
                iq.AddTranslation(Replace(rdr.Item("technology"), " ", ""), English, "", 0, twc, nextKey, False), pawc)
        End If


        Dim ofa As clsAttribute
        If Not iq.i_attribute_code.ContainsKey("optFam") Then
            ofa = New clsAttribute("optFam", iq.AddTranslation("Options family", English, "", 0, twc, nextKey, False), 0)
        Else
            ofa = iq.i_attribute_code("optFam")
        End If

        Dim ofm$ = rdr.Item("OptFamily")
        Dim optfam As clsProductAttribute = New clsProductAttribute(optionProduct, ofa, 0, textUnit, iq.AddTranslation(ofm, English, "", 0, twc, nextKey, False), pawc)

    End Function


    'Public Function options(con As SqlClient.SqlConnection, ByRef OptionsBySKU As Dictionary(Of String, clsProduct), _
    '                   dicPlcode As Dictionary(Of String, String), _
    '                   ByRef dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)), _
    '                   dicUnits As Dictionary(Of String, clsUnit), _
    '                   containment As Dictionary(Of String, List(Of String))) _
    '                   As Dictionary(Of String, clsProduct)

    '    'Options returns a dictionary of compound key 
    '    'Dim ck$ = sysfamily & "^" & rdr.Item("l1") & "^" & rdr.Item("l2") & "^" & l3 & "^" & rdr.Item("optsn")


    '    options = New Dictionary(Of String, clsProduct)

    '    dicOptLocalisation = New Dictionary(Of clsProduct, List(Of clsRegion))

    '    Dim sql$
    '    Dim rdr As SqlClient.SqlDataReader

    '    'Makes option Procucts and multi-dimensional dictionary of them

    '    'Options

    '    'OptSN is and IQ1 PK (unique ID)
    '    sql$ = "SELECT po.optSN,po.optsku,sysfamily,optType,cu.optTypeParent as optCat,optfamily,technology,active,activetodate,altsku,eol,fiosysfamily,descriptionHP"
    '    sql$ &= ",ccDescription,incompatible,h.bucode,localisation,unitQty as capacity,SpeedUnitQty as speed, "
    '    sql$ &= "su.OptTypeSpeedUnit as speedUnit,Cu.OptTypeUnit as capacityUnit,Technology ,slots,aaonly,l1,l2,l3 "
    '    sql$ &= "from " & server$ & "[iq].Products.options "
    '    sql$ &= "join " & server$ & "[channelcentral].products.Hierarchy h ON h.upcNUM = optsku "
    '    sql$ &= "join " & server$ & "[iq].products.optTypes as su on su.optTypeCode=optType "
    '    sql$ &= "join " & server$ & "[iq].products.optTypes as cu on cu.optTypeCode=optType "
    '    sql$ &= "join " & server$ & "[iq].products.optTypeParents as pu on pu.optTypeParent=cu.optTypeParent "
    '    sql$ &= "join products.V2_OptionCats v on v.optsn=po.optsn "
    '    sql$ &= "WHERE active=1 "
    '    'sql$ &= "where sysfamily like '%DL380%'"
    '    sql$ &= "ORDER BY pu.ParentRank,su.OptTypeRank"

    '    '                          performance,storage etc, HDD,TAP etc

    '    'the ordering is new


    '    'becuase (for example_ CPU's are not an option Type for laptops.. then laptops get no CPU

    '    'Create a set of options under each optType, under each Broad SysFamily name (options.sysfamily,sysfamilydefinitons.sysfamilyname - NOT the (narrow) sysfamilycode) 
    '    'We will subsequently graft the optType branches (containg the products from the inner most dictionary)
    '    'under each (pre-existing) system branch in the sysfamily 

    '    'We create something like this (in DicSysFam)
    '    'DL385G5p (family)
    '    '     +Performance (opttype parent) -(optCat)
    '    '        + MEM (option type)
    '    '              + MEM_PC3-10600SODIMM (option family)
    '    '                    +DDR 3 (technology)
    '    '                        HP SB 8GB Dual Rank x4 PC3-10600 (DDR3-1333MHz) (option)
    '    '                        HP 16GB Quad Rank x4 PC3-8500 RDIMM (option
    '    '      +Storage (optTypeParent)
    '    '        + HDD (option type)
    '    '              +5.25LFF (option family)
    '    '                    + SATA (technology)
    '    '                        750GB SATA 1.5G 7K Mid-Line HDD (option)
    '    '                        .... (etc)

    '    'the outer dictionary is keyed by sysfamily - and exposes a set of optTypes (MEM,HDD etc)
    '    'the optTypes expose a dictionary of option families .. etc 

    '    Dim attribwritecache As DataTable
    '    attribwritecache = da.MakeWriteCacheFor(con, "ProductAttribute")

    '    Dim TranslationWriteCache As DataTable
    '    TranslationWriteCache = da.MakeWriteCacheFor(con, "Translation")

    '    rdr = da.DBExecuteReader(con, sql$)

    '    'Dim optType As Dictionary(Of String, Dictionary(Of Integer, clsProduct))  '
    '    'optType = New Dictionary(Of String, Dictionary(Of Integer, clsProduct))

    '    'we will make one of these for each option catergory (optTypeParent)  (storage, performance etc) - it contains the opt Types... (eg. HDD/MEM etc)
    '    '                                               cat                            type                              family                     tech                           SN
    '    'Dim dicOptCat As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct)))))  '
    '    '  dicOptCat = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct)))))

    '    Dim anOption As clsProduct

    '    Dim count As Integer = 0
    '    '        Dim optTypeName As String
    '    '        Dim optFamilyName As String
    '    '        Dim technologyName As String

    '    Dim anAttribute As clsProductAttribute
    '    Dim MfrSKU As clsProductAttribute
    '    Dim PLCode As clsProductAttribute
    '    Dim Incompatible As clsProductAttribute

    '    Dim ssd As clsTranslation = iq.AddTranslation("SSD", English, "DriveType")
    '    Dim ssde As clsTranslation = iq.AddTranslation("Solid State Drive", English, "DriveType")

    '    'circa 110 secs

    '    Dim ProductType As clsProductType
    '    Dim l2$

    '    Dim textUnit As clsUnit = iq.i_unit_code("txt")
    '    Dim sector As clsSector

    '    While rdr.Read

    '        l2$ = rdr.Item("l2")
    '        If iq.i_ProductType_Code.ContainsKey(l2) Then
    '            ProductType = iq.i_ProductType_Code(l2$)
    '        Else
    '            If iq.i_ProductType_Code.ContainsKey("FIO") Then
    '                ProductType = iq.i_ProductType_Code("FIO") 'Nothing  'these are FIO's (fingerprint readers, Multi card readers etc.. that are never standalone products - so there are no productTypes for them)
    '            Else
    '                ProductType = New clsProductType("FIO", iq.AddTranslation("Factory Installed/Ancillary Option", English))
    '            End If
    '        End If

    '        If iq.i_sector_code.ContainsKey("BUcode") Then
    '            sector = iq.i_sector_code(rdr.Item("BUcode"))
    '        Else
    '            sector = iq.i_sector_code("NoSector")
    '        End If

    '        'If rdr.Item("ccDescription") Is DBNull.Value Then Stop

    '        If OptionsBySKU.ContainsKey(rdr.Item("optsku")) Then 'Have we already made this option ((on a previous import)
    '            'Beep()
    '            anOption = OptionsBySKU(rdr.Item("optsku"))
    '        Else

    '            Dim activeto As Date = CDate("31/12/2100")
    '            If Not IsDBNull(rdr.Item("activetodate")) Then activeto = rdr.Item("activeToDate")

    '            Dim publish As Boolean = True
    '            If rdr.Item("AAonly") <> 0 Then publish = False
    '            anOption = New clsProduct(CStr(rdr.Item("ccDescription")), False, sector, ProductType, CDate("01/01/2000"), activeto, rdr.Item("active"), rdr.Item("eol"), publish)

    '            'Populate the Dictionary of option localisations (Countries in which it's active) - which is used later in BuildTree
    '            'we DO NOT add options which are not localised to the dicoptlocalisation dictionary (they are unrestricted)
    '            If Not IsDBNull(rdr.Item("localisation")) Then
    '                Dim regions As List(Of clsRegion) = New List(Of clsRegion)

    '                Dim cs As List(Of String) = Split(rdr.Item("localisation"), ",").ToList

    '                If Not cs.Contains("XW") Then   'Anything paul has localised 'worldwide' needs no restriction
    '                    cleanRegions(cs, containment)
    '                    For Each c In cs
    '                        If c = "UCSA" Then c = "USCA" 'fix a typo
    '                        If iq.i_region_code.ContainsKey(c) Then
    '                            regions.Add(iq.i_region_code(c))
    '                        Else
    '                            Logit("invalid region " & c & " (in products.options.localisation)")
    '                            '    Stop
    '                        End If
    '                    Next
    '                    dicOptLocalisation.Add(anOption, regions)
    '                End If
    '            End If

    '            'record the options OptFamily
    '            anAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("optFamily"), 0, textUnit, iq.AddTranslation(rdr.Item("optfamily"), English, , , TranslationWriteCache), attribwritecache)

    '            'This IS used in the quote summary (amongst other places)
    '            anAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("optType"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("optType"), English, , , TranslationWriteCache), attribwritecache)

    '            'If Not iq.Attributes.ContainsKey("MfrSKU") Then j = New clsAttribute("MfrSKU", New clsText(iq.addTranslation("MfrSKU")))

    '            Dim speed As clsProductAttribute
    '            Dim capacity As clsProductAttribute
    '            If Not IsDBNull(rdr.Item("speed")) Then
    '                If Not IsDBNull(rdr.Item("speedunit")) Then  'Some things (tape drives/Graphics cards/batteries have 'speeds' without units - we're not imprtiong - mentioned to dan 02/08/2012
    '                    speed = New clsProductAttribute(anOption, iq.i_attribute_code("speed"), rdr.Item("speed"), dicUnits(rdr.Item("speedUnit")), Nothing, attribwritecache)
    '                End If
    '            Else
    '                If rdr.Item("Opttype") = "HDD" Then
    '                    'HDD's without a speed are SSD's - give them a numerically high RPM (so they sort to the 'top' speed wise - but display the text SSD (instead of 100,000 rpm)
    '                    speed = New clsProductAttribute(anOption, iq.i_attribute_code("speed"), 100000, iq.i_unit_code("txt"), ssd, attribwritecache)
    '                End If
    '            End If

    '            If Not IsDBNull(rdr.Item("capacity")) Then

    '                Dim uk$
    '                If Not IsDBNull(rdr.Item("capacityunit")) Then ''Some things (cables,newtork cards have  capacities without units - we're not importing - mentioned to dan 02/08/2012 - am now... with a TXT unit
    '                    uk$ = rdr.Item("capacityunit")
    '                Else
    '                    uk$ = "txt"
    '                End If

    '                capacity = New clsProductAttribute(anOption, iq.i_attribute_code("capacity"), rdr.Item("capacity"), iq.i_unit_code(uk$), Nothing, attribwritecache)

    '            End If

    '            If Not IsDBNull(rdr.Item("technology")) Then
    '                Dim t$ = rdr.Item("technology")
    '                Dim cp As Integer
    '                cp = InStr(t$, "CORE")
    '                Dim numcores As Integer
    '                If cp Then
    '                    numcores = Val(Left$(t$, cp - 1))
    '                    Dim cores As clsProductAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("cores"), numcores, iq.i_unit_code("num"), Nothing)

    '                    Dim numthreads As Integer
    '                    cp = InStr(t$, "TH")
    '                    If cp Then
    '                        numthreads = Val(Mid$(t$, cp - 2, 2))
    '                        Dim threads As clsProductAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("threads"), numthreads, iq.i_unit_code("num"), Nothing)
    '                    End If
    '                End If
    '            End If

    '            MfrSKU = New clsProductAttribute(anOption, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(Trim$(rdr.Item("OptSKU")), English, "SKU", , TranslationWriteCache), attribwritecache)
    '            Dim pl$

    '            If Not dicPlcode.ContainsKey(rdr.Item("optSKU")) Then
    '                Logit("No PL code for option '" & rdr.Item("Optsku") & "' (not in HeirarchyIQ).")
    '            Else
    '                pl = dicPlcode(rdr.Item("optSKU"))
    '                PLCode = New clsProductAttribute(anOption, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(pl, English, , , TranslationWriteCache), attribwritecache)
    '            End If

    '            'Dim opttype As clsProductAttribute
    '            'Dim opt$
    '            'opt$ = rdr.Item("OptType")
    '            'opttype = New clsProductAttribute(anOption, iq.Attributes("OptType"), 0, iq.Units("txt"), iq.addTranslation(opt, TranslationWriteCache).Key, attribwritecache)
    '            'End If

    '            If Not IsDBNull(rdr.Item("incompatible")) Then
    '                If Trim$(rdr.Item("incompatible")) <> "" Then
    '                    Incompatible = New clsProductAttribute(anOption, iq.i_attribute_code("incompat"), 0, textUnit, _
    '                    iq.AddTranslation(Replace(rdr.Item("incompatible"), " ", ""), English, , , TranslationWriteCache), attribwritecache)
    '                End If
    '            End If

    '            If Not IsDBNull(rdr.Item("altsku")) Then
    '                If Trim$(rdr.Item("altsku")) <> "" Then
    '                    Incompatible = New clsProductAttribute(anOption, iq.i_attribute_code("altSKU"), 0, textUnit, _
    '                    iq.AddTranslation(Replace(rdr.Item("altSKU"), " ", ""), English, , , TranslationWriteCache), attribwritecache)
    '                End If
    '            End If

    '            If Not IsDBNull(rdr.Item("altsku")) Then
    '                If Trim$(rdr.Item("altsku")) <> "" Then
    '                    Incompatible = New clsProductAttribute(anOption, iq.i_attribute_code("altSKU"), 0, textUnit, _
    '                    iq.AddTranslation(Replace(rdr.Item("altSKU"), " ", ""), English, , , TranslationWriteCache), attribwritecache)
    '                End If
    '            End If

    '            'required later when making 'takes' slots - to respect iq.products.options.slots
    '            If Not iq.i_attribute_code.ContainsKey("Slots") Then
    '                Dim sa As clsAttribute = New clsAttribute("Slots", iq.AddTranslation("Slots used (legacy/import)", English, , , TranslationWriteCache), 0)
    '            End If
    '            Dim slots As clsProductAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("Slots"), rdr.Item("slots"), iq.i_unit_code("num"), Nothing, attribwritecache)

    '            If Not iq.i_attribute_code.ContainsKey("OptFam") Then
    '                Dim optfamAtt As clsAttribute = New clsAttribute("OptFam", iq.AddTranslation("Option Family (legacy/import)", English, , , TranslationWriteCache), 0)
    '            End If
    '            Dim optfam As clsProductAttribute = New clsProductAttribute(anOption, iq.i_attribute_code("OptFam"), 0, iq.i_unit_code("txt"), iq.AddTranslation(rdr.Item("OptFamily"), English, , , TranslationWriteCache))

    '            '  If Not IsDBNull(rdr.Item("speedUnitQuantity")) Then
    '            '      speed = New clsProductAttribute(anOption,IQ.Attributes(IQ.i_attribute("Speed")
    '            ' End If

    '        End If
    '        If Not rdr.Item("sysfamily") Is DBNull.Value Then
    '            For Each sysfamily In Split(rdr.Item("sysfamily"), ",")
    '                sysfamily = Trim$(sysfamily)
    '                If sysfamily <> "" Then

    '                    Dim l3 As String
    '                    If rdr.Item("l3") = DBNull.Value Then
    '                        l3 = ""
    '                    Else
    '                        l3 = rdr.Item("l3")
    '                        If l3 = UCase(l3) And Len(l3) > 3 Then l3 = capitalise(l3)
    '                    End If
    '                    Dim ck$ = sysfamily & "^" & rdr.Item("l1") & "^" & rdr.Item("l2") & "^" & l3 & "^" & rdr.Item("optsn")

    '                    'some options are listed as being in the same family more than once 
    '                    ' so ignore - if it's already listed
    '                    If Not options.ContainsKey(ck) Then
    '                        'add this as a general otpion for the family - it may get pruned later in some contexts
    '                        options.Add(ck, anOption)
    '                        count += 1

    '                        'create a handy lookup in a 'flat' dictionary
    '                        If Not OptionsBySKU.ContainsKey(Trim$(rdr.Item("optsku"))) Then
    '                            OptionsBySKU.Add(Trim$(rdr.Item("optsku")), anOption)
    '                        End If

    '                        'Make 'Takes' slots on this branch from Products.options.[Slots] and .[OptType]
    '                        'hard to do when the branch doesn't exist yet !

    '                        'the exact slot type (optfamily) in question must be looked up from the optType and sysfamily
    '                        'we can use the dictionary to look it up (may need a second pass) <<< (go through options table again.. use Dic(fam)(opttype) to get optfams
    '                        'make Gives and Takes slots - for this branch the slotaddqty and slotaddtype (which refers to a set of opt types)
    '                        'eg -3;4   RAC,OPT
    '                        'or
    '                        '5 MEM

    '                    End If
    '                End If
    '            Next
    '        End If
    '        'End If
    '    End While

    '    rdr.Close()

    '    'write all the accumulated ProductAttributes
    '    Dim Pas As Integer = attribwritecache.Rows.Count
    '    da.BulkWrite(con, attribwritecache, "ProductAttribute")
    '    attribwritecache = Nothing


    '    da.BulkWrite(con, TranslationWriteCache, "Translation")

    '    Logit("Done options", False, True)


    'End Function

    Public Function cleanRegions(i As List(Of String), containment As Dictionary(Of String, List(Of String))) As List(Of String)

        'For each non-country region in the list 'dirty' list 'I', remove all contained, regions/countries
        'containment - is a pre-prepared dicitionary of the descendants of each region


        'First - see if all the countries of any region are in the list - and if so add that region (in lieu of the many countries)

        For Each rc In i.ToList 'for each region code..
            If rc <> "" And iq.i_region_code.ContainsKey(rc) Then
                Dim r As clsRegion = iq.i_region_code(rc) 'get a reference to the actual region

                If Not r.isCountry Then
                    If Not i.Contains(r.Code) Then
                        If containment(r.Code).Intersect(i).Count = containment(r.Code).Count Then
                            'all the countries in this region are in the list
                            i.Add(r.Code) ' add  the region (we will subsequently remove all of the consitiuent countries)
                        End If
                    End If
                End If
            Else
                If rc <> "" Then
                    'Note, UK is NOT a valid country code
                    '        Beep()
                End If

            End If
        Next


        If i.Contains("XW") Then Stop
        If i.Contains("GWE") Then  'Sustitute GWE for EMEA if both are present
            If i.Contains("EMEA") Then i.Remove("EMEA")
        End If

        Dim o As List(Of String) = New List(Of String)

        Dim toremove As List(Of String) = New List(Of String)
        For Each rc In i 'for each region code..
            If rc = "UCSA" Then rc = "USCA" 'fix a typo
            If rc <> "" And iq.i_region_code.ContainsKey(rc) Then
                Dim r As clsRegion = iq.i_region_code(rc) 'get a reference to the actual region
                If Not r.isCountry Then

                    toremove = toremove.Union(clsRegion.containment(rc)).ToList 'remove all the countries (and regions) this region contains
                    toremove.Remove(r.Code) '@@@
                End If
            End If
        Next

        '   cleanRegions = From j In i Where Not j in toremove 'didnt work
        If toremove.Count Then
            cleanRegions = i.Except(toremove).ToList 'very Neat LINQ
            If cleanRegions.Count < i.Count Then
                Logit("collapsed " & Join(i.ToArray, ",") & " to " & Join(cleanRegions.ToArray, ","))
                '   Beep()
            End If
        Else
            cleanRegions = i
        End If



    End Function

    'Public Sub countries(con As SqlClient.SqlConnection, ByRef dicCountries As Dictionary(Of String, clsCountry), dicCountryCurrencies As Dictionary(Of String, clsCurrency))

    '    Dim sql$
    '    Dim rdr As SqlClient.SqlDataReader

    '    sql$ = "SELECT countrycode,[CountryName],[Currency],[Region],[active],[MainsV],[Notes],[possible] from " & server$ & "[iq].dbo.countries"

    '    rdr = da.dbexecuteReader(con, sql$)

    '    Dim acountry As clsCountry
    '    While rdr.Read
    '        If Not dicCountries.ContainsKey(Trim$(rdr.Item("countrycode"))) Then
    '            acountry = New clsCountry(rdr.Item("countrycode"), iq.AddTranslation(rdr.Item("CountryName"), English), DefaultCulture(rdr.Item("countrycode")))
    '            dicCountries.Add(Trim$(acountry.Code), acountry)
    '            If Not IsDBNull(rdr.Item("currency")) Then
    '                dicCountryCurrencies.Add(Trim$(rdr.Item("countrycode")), iq.i_currency_code(rdr.Item("currency")))  'used when importing quotes
    '            End If
    '        End If
    '    End While
    '    rdr.Close()



    'End Sub

    Public Sub ProductTypes(con As SqlClient.SqlConnection, ByRef dicOptType As Dictionary(Of String, clsProductType))

        Dim rdr As SqlClient.SqlDataReader
        Dim aproducttype As clsProductType
        'Dim dicOptType As New Dictionary(Of String, clsProductType)

        'populate the dictionary of all option types (MEM/HDD/CPU)
        rdr = da.DBExecuteReader(con, "SELECT optTypecode as code, optTypename as name from " & server$ & "[iq].products.optTypes")

        ''Dim np As clsSector = iq.i_sector_code("nonProdut")

        Dim existed, added As Integer

        While rdr.Read

            Dim lc$ = Trim$(LCase$(rdr.Item("code")))
            If Not dicOptType.ContainsKey(lc$) Then
                aproducttype = New clsProductType(lc$, iq.AddTranslation(Abbreviation(rdr.Item("name")), English, "PT", 0, Nothing, 0, False), 0)
                dicOptType.Add(lc$, aproducttype)
                added += 1
            Else
                existed += 1
            End If

        End While
        rdr.Close()

        Logit("Loaded " & dicOptType.Count & " option type codes, " & existed & " existed, " & added & " added.")


        ' I *think* this is  replaced by systypes
        If Not dicOptType.ContainsKey("DTO") Then
            aproducttype = New clsProductType("DTO", iq.AddTranslation("Desktop", English, "TOP", 0, Nothing, 0, False), 0) : dicOptType.Add("DTO", aproducttype)
            aproducttype = New clsProductType("NBK", iq.AddTranslation("Notebook", English, "TOP", 0, Nothing, 0, False), 0) : dicOptType.Add("NBK", aproducttype)
            aproducttype = New clsProductType("SVR", iq.AddTranslation("Server", English, "TOP", 0, Nothing, 0, False), 0) : dicOptType.Add("SVR", aproducttype)
            aproducttype = New clsProductType("SWD", iq.AddTranslation("Storage device", English, "TOP", 0, Nothing, 0, False), 0) : dicOptType.Add("SWD", aproducttype) 'storage
            aproducttype = New clsProductType("HPN", iq.AddTranslation("Network device", English, "TOP", 0, Nothing, 0, False), 0) : dicOptType.Add("HPN", aproducttype) 'networking
        End If

        '    Return dicOptType

    End Sub
    Public Sub users(con As SqlClient.SqlConnection, ByVal dicChannels As Dictionary(Of String, clsChannel), ByRef dicAccounts As Dictionary(Of String, clsAccount), ByRef DicTeams As Dictionary(Of String, clsTeam), dicUsers As Dictionary(Of String, clsUser))

        Dim sql$
        Dim rdr As SqlClient.SqlDataReader
        Dim anaccount As clsAccount
        Dim auser As clsUser = Nothing
        Dim buyer As clsChannel
        Dim seller As clsChannel
        Dim chanid As String

        'USERS (& Accounts)
        'each user can have accounts with many sellers - they will have one username, but a password for each seller
        'Dictionary used to construct priceBand table - first index is the host (seller)  - pointing to a dictionary of BuyerAccounts(buyers)>priceBands
        'users are also loaded into the buyer channel teams

        'TEAMS

        Dim aTeam As clsTeam
        rdr = da.DBExecuteReader(con, "select TeamID,ChanID,TeamName from " & server$ & "[channelcentral].customers.host_teams")
        Dim lc$
        While rdr.Read
            lc$ = Trim$(LCase$(rdr.Item("teamID")))
            If Not DicTeams.ContainsKey(lc$) Then
                aTeam = New clsTeam(dicChannels(Trim$(rdr.Item("chanid"))), Trim$(rdr.Item("TeamName")))
                DicTeams.Add(lc$, aTeam)
            End If
        End While
        rdr.Close()

        Dim nextUID As Integer = 0
        Dim nextACid As Integer = 0
        Dim uwc As DataTable = da.MakeWriteCacheFor(con, "user", nextUID, True)
        Dim awc As DataTable = da.MakeWriteCacheFor(con, "account", nextACid, True)

        Dim countUsers As Integer

        sql$ = "SELECT username,[password],realname,chanID,priceBand,realname,admin,email,team,tel1,tel2,[admin],[lang],[disabled],"
        sql$ &= "(SELECT TOP 1 currency from " & server$ & "[channelcentral].[customers].[HostAccounts] where priceBand = u.priceBand) as currency"
        sql$ &= " from " & server$ & "[channelcentral].customers.users u " & If(Not String.IsNullOrEmpty(restrictImportToFamily), " WHERE username like '%@channelcentral.net'", "") & " order by ltrim(rtrim(email))"  'order by email so users multiple accounts appear together

        dicAccounts = New Dictionary(Of String, clsAccount)(StringComparer.CurrentCultureIgnoreCase)

        Dim DicHostAccounts As Dictionary(Of String, clsAccount)
        DicHostAccounts = New Dictionary(Of String, clsAccount)(StringComparer.CurrentCultureIgnoreCase)

        rdr = da.DBExecuteReader(con, sql$)
        Dim email As String = "xxx"
        Dim dud As Boolean
        Dim role As clsRole

        While rdr.Read
            dud = False

            If Left$(rdr.Item("Username"), 2) = "IQ" Then
                chanid = Split(rdr.Item("username"), "_")(1) 'seller dist 'EG Computer Gross (DCOIT00143)

                If Not dicChannels.ContainsKey(chanid) Then

                    Logit("channel " & chanid & " does not exist")

                Else

                    seller = dicChannels(chanid) 'seller dist 'EG Computer Gross (DCOIT00143)

                    If dicChannels.ContainsKey(Trim$(rdr.Item("chanID"))) Then
                        buyer = dicChannels(Trim$(rdr.Item("chanID")))  'BUYER 'EG tcsystems.is - will have a priceBand
                    Else
                        Logit("The buyer channelID " & Trim$(rdr.Item("ChanID")) & " referenced in channelcentral.customers.users.chanID does not exist in channelcentral.customers.channel ")
                        dud = True
                        buyer = Nothing
                        '    Stop
                    End If

                    If Trim$(rdr.Item("email")) = "" Then dud = True 'there are some duds

                    If Not dud Then
                        'we make (multiple) accounts (for the same user) until the user changes (the list we're iterating is ordered by email)
                        If LCase(Trim$(rdr.Item("email"))) <> LCase(Trim$(email)) Then  'each user has many accounts (potentially)
                            If Not dicUsers.ContainsKey(rdr.Item("username")) Then
                                auser = New clsUser(buyer, CStr(rdr.Item("email")), rdr.Item("realname"), New nullableString(rdr.Item("tel1")), New nullableString(rdr.Item("tel2")), uwc, nextUID)
                            End If
                        End If
                        email = Trim$(rdr.Item("email"))

                        Dim team As clsTeam = Nothing
                        If Not IsDBNull(rdr.Item("team")) Then
                            If DicTeams.ContainsKey(Trim$(rdr.Item("team"))) Then
                                team = DicTeams(Trim$(rdr.Item("Team")))
                                team.Members.Add(auser)
                            Else
                                Logit("Team " & Trim$(rdr.Item("team")) & " referenced in channelcentral.customers.users.team is not present in channelcentral.customers.host_teams")
                                '    Stop
                            End If
                        End If

                        Dim cur$
                        If IsDBNull(rdr.Item("currency")) Then
                            cur$ = "GBP"
                        Else
                            cur$ = rdr.Item("currency")
                        End If
                        If cur$ = "nul" Then cur$ = "GBP" 'fix for bad data (techdata)

                        Dim arole As clsRole
                        If Not iq.i_role_Code.ContainsKey("user") Then arole = New clsRole("user", iq.AddTranslation("User", English, "UI", 0, Nothing, 0, False))
                        If Not iq.i_role_Code.ContainsKey("admin") Then arole = New clsRole("admin", iq.AddTranslation("Administrator", English, "UI", 0, Nothing, 0, False))


                        If rdr.Item("admin") = "Y" Then role = iq.i_role_Code("admin") Else role = iq.i_role_Code("user")

                        Dim language As clsLanguage


                        If Not iq.i_language_Code.ContainsKey(Trim$(UCase(rdr.Item("Lang")))) Then

                            Logit(rdr.Item("username") & " has an invalid language code of '" & rdr.Item("lang") & "'")
                        Else

                            language = iq.i_language_Code(Trim$(UCase(rdr.Item("Lang"))))

                            'If UCase(language.Code) = "EL" Then Stop


                            'If Not dicpriceBands.ContainsKey(seller) Then
                            ' dicpriceBands.Add(seller, New Dictionary(Of clsBuyerGroup, String)) ' for each seller create a lookup of buyergroup>priceBand
                            'End If

                            '                             what we're importing is already MD5'd (so we only need to shuffle it)
                            'anAccount = New clsAccount(aUser, Shuffle(Trim$(rdr.Item("password"))), buyer, Role, team, language, dicCurrencies(cur$))

                            'Dim buyerGroup As clsBuyerGroup
                            'If IsDBNull(rdr.Item("priceBand")) Then
                            ' buyerGroup = dicBuyerGroups(Trim$(rdr.Item("chanid")) & "_self")
                            'Els() 'e
                            ' buyerGroup = dicBuyerGroups(Trim$(rdr.Item("chanid")) & "_" & rdr.Item("priceBand"))
                            'End If

                            'we cannot know the passwords - becuase they are hashed 
                            anaccount = New clsAccount(auser, Shuffle(Trim$(rdr.Item("password"))), buyer, {role}, team, language, iq.i_currency_code(cur$), seller, IIf(IsDBNull(rdr.Item("priceBand")), "", rdr.Item("priceBand")), buyer.Region.Culture, "HPE", awc, nextACid)

                            dicAccounts.Add(Trim$(rdr.Item("Username")), anaccount)

                            If Not IsDBNull(rdr.Item("priceBand")) Then
                                If Not DicHostAccounts.ContainsKey(Trim$(rdr.Item("priceBand"))) Then
                                    DicHostAccounts.Add(Trim$(rdr.Item("priceBand")), anaccount)
                                End If
                            End If

                            'If Not IsDBNull(rdr.Item("priceBand")) Then
                            '    If Not dicpriceBands(seller).ContainsKey(buyer) Then

                            '        dicpriceBands(seller).Add(buyer, Trim$(rdr.Item("priceBand")))
                            '    Else
                            '        'conflicting host account nums - ie two users working for the same buyer (with the same ChanID) - have different host account numbers
                            '        'OK to take a arbitrary
                            '        'If dicpriceBands(seller)(buyer) <> Trim$(rdr.Item("priceBand")) Then Stop
                            '    End If
                            'End If

                            countUsers += 1
                        End If
                    End If 'not dud
                End If
            End If
        End While
        rdr.Close()

        da.BulkWrite(con, uwc, "[user]")
        da.BulkWrite(con, awc, "account")

    End Sub


    Public Function Languages(con As SqlClient.SqlConnection, ByRef diclanguages As Dictionary(Of String, clsLanguage)) As Integer

        Dim rdr As SqlClient.SqlDataReader
        Dim alanguage As clsLanguage
        Languages = 0

        rdr = da.DBExecuteReader(con, "SELECT LangCode,LangName from " & server$ & "[iq].dbo.languages")
        Dim lc$
        While rdr.Read
            lc$ = Trim$(LCase(rdr.Item("langcode")))
            If Not iq.i_language_Code.ContainsKey(UCase(lc$)) Then
                alanguage = New clsLanguage(UCase$(Trim$(lc$)), rdr.Item("LangName"), False, True, True)
                diclanguages.Add(lc$, alanguage)
                Languages += 1
            End If
        End While

        rdr.Close()

    End Function

    'stand alone legal (& margins) import (one off)
    Public Sub Legal()

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase

        Dim sql$
        Dim rdr As SqlClient.SqlDataReader

        Dim dicClones As Dictionary(Of String, String)
        dicClones = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase) 'A temporary dictionary (by channel id, child:parent)

        sql$ = "SELECT parenthost as parent,subhost as child,margin,marginPSG from " & server$ & "[channelcentral].customers.host_parents"
        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            dicClones.Add(rdr.Item("child"), rdr.Item("parent"))
        End While
        rdr.Close()

        Dim errorMessages As List(Of String) = New List(Of String)

        sql$ = "SELECT hostid,supplyChains,portfolios,ChannelID,ChannelName,h.CountryCode,c.Currency,hp.pic,hp.pic2,hp.url,hp.dp,hp.listpriceonlyskus,hp.feedonlyskus,hpreceta,"
        sql$ &= "terms,marginMin,MarginMax,marginType "
        sql$ &= "FROM " & server$ & "[channelcentral].customers.channel h "
        sql$ &= "JOIN " & server$ & "[iq].dbo.countries c on h.countrycode=c.countrycode "
        sql$ &= "LEFT JOIN " & server$ & "[channelcentral].customers.host_properties hp on hp.hostid= h.channelid "
        sql$ &= "where hostid is not null "

        rdr = da.DBExecuteReader(con, sql$)

        While rdr.Read

            Dim channel As clsChannel
            Dim hostid As String = rdr.Item("hostid")

            If iq.i_channel_code.ContainsKey(hostid) Then
                channel = iq.i_channel_code(hostid)

                If IsDBNull(rdr.Item("terms")) Then
                    channel.Legal = "<b>Usage of iQuote means that you agree to the following Terms & Conditions:<b>"
                    channel.Legal &= "Every care is taken to ensure that the information contained within this site is accurate, however Errors and Omissions Excepted."
                Else
                    channel.Legal = rdr.Item("Terms")
                End If

                channel.marginMin = -20
                channel.marginMax = 40
                If Not IsDBNull(rdr.Item("marginMin")) Then
                    channel.marginMin = rdr.Item("MarginMin")
                End If

                If Not IsDBNull(rdr.Item("marginMax")) Then
                    channel.marginMax = rdr.Item("MarginMax")
                End If

                channel.Update(errorMessages)

            End If

        End While

        rdr.Close()
        con.Close()



    End Sub

    Private Class clsTmpClone


        Friend parentChannel As clsChannel 'this is the clone (child) channel

        Friend marginPSG As Single
        Friend marginISS As Single
        Friend priceband As String

        Public Sub New(ParentChannel As clsChannel, marginpsg As Single, marginIss As Single, priceband As String)
            Me.parentChannel = ParentChannel
            Me.marginPSG = marginpsg
            Me.marginISS = marginIss
            Me.priceband = priceband

        End Sub

    End Class


    Public Sub clones(errormessages As List(Of String))
        'one time import of clones - as it hasn't worked :(

        Dim con As SqlClient.SqlConnection = da.OpenDatabase

        Dim sql$
        Dim rdr As SqlClient.SqlDataReader

        Dim dicClones As Dictionary(Of String, clsTmpClone)
        dicClones = New Dictionary(Of String, clsTmpClone)(StringComparer.CurrentCultureIgnoreCase) 'A temporary dictionary (by channel id, child:parent)

        sql$ = "SELECT parenthost as parent,subhost as child,margin,isnull(marginpsg,0) as marginpsg,externalPrice FROM " & server$ & "[channelCentral].customers.host_parents"
        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            dicClones.Add(rdr.Item("child"), New clsTmpClone(iq.i_channel_code(rdr.Item("parent")), rdr.Item("marginpsg"), rdr.Item("margin"), IIf(CBool(rdr.Item("externalprice")), "EXT", "")))
        End While
        rdr.Close()

        Dim dicBUs As Dictionary(Of String, clsSector) = New Dictionary(Of String, clsSector)
        sql$ = "select upcnum,bucode from h3.ChannelCentral.products.Hierarchy where bucode is not null"
        rdr = da.DBExecuteReader(con, sql$)

        Dim dudbucs As List(Of String) = New List(Of String)
        Dim nullBUs As Integer = 0
        While rdr.Read
            If Not rdr.Item("upcnum").startswith("###") Then
                If Not rdr.Item("Bucode") Is DBNull.Value Then
                    Dim buc$ = rdr.Item("bucode")
                    If iq.i_sector_code.ContainsKey(buc$) Then
                        dicBUs.Add(rdr.Item("upcnum"), iq.i_sector_code(rdr.Item("bucode")))
                    Else
                        If Not dudbucs.Contains(buc) Then
                            errormessages.Add(buc$ & " is invalid")
                        End If
                    End If
                Else
                    nullBUs += 1
                End If
            End If
        End While
        rdr.Close()
        errormessages.Add(nullBUs & " null BUs")

        Dim done As Integer

        For Each sku In iq.i_SKU.Keys  'SKU>Product
            Dim product As clsProduct = iq.i_SKU(sku)
            If Not product.isSystem Then
                If Not product.SKU.StartsWith("###") Then
                    If dicBUs.ContainsKey(sku) Then
                        If product.Sector IsNot dicBUs(sku) Then
                            product.Sector = dicBUs(sku)
                            product.update(errormessages)
                            done += 1
                        End If
                    End If
                End If
            End If
        Next

        errormessages.Add("Updated BU on " & done & " options.")

        For Each channel In iq.Channels.Values
            channel.Margin.Clear()
        Next
        da.DBExecutesql(con, "DELETE FROM MARGIN")

        Dim fixed As Integer = 0
        Dim made As Integer

        Dim parent As clsChannel
        Dim child As clsChannel

        'for every child (clone) set its parent
        For Each childChannelid In iq.i_channel_code.Keys
            If dicClones.ContainsKey(childChannelid) Then  'this channel is a clone (a child)
                Dim clone As clsTmpClone = dicClones(childChannelid)
                parent = clone.parentChannel
                child = iq.i_channel_code(childChannelid)
                child.IsCloneOf = parent
                child.Update(errormessages) 'writes the new FK_Channel_Id_IsCloneOf  to the database
                Dim pb As String = ""

                'If rdr.Item("ExternalPrice") <> 0 Then pb$ = "EXT" Else pb$ = ""

                Dim mfISS, mfPSG As Single

                If child.marginType = "R" Then
                    mfISS = 100 / (100 - clone.marginISS)
                    mfPSG = 100 / (100 - clone.marginPSG)
                Else
                    mfISS = (100 + clone.marginISS) / 100
                    mfPSG = (100 + clone.marginPSG) / 100
                End If
                Dim margISS As clsMargin = New clsMargin(parent, child, mfISS, pb$, iq.i_sector_code("HPISS"), "")
                Dim margPSG As clsMargin = New clsMargin(parent, child, mfPSG, pb$, iq.i_sector_code("HPPSG"), "")
                made += 2

            Else
                'the parent isn't loaded yet
                Beep()
            End If
        Next

        con.Close()


    End Sub


    Public Sub channels(Con As SqlClient.SqlConnection, ByRef dicChannels As Dictionary(Of String, clsChannel), dicRegions As Dictionary(Of String, clsRegion), ByRef errormessages As List(Of String))  'As Dictionary(Of String, clsChannel)

        Dim nextcID As Integer
        Dim channelWriteCache As DataTable = da.MakeWriteCacheFor(Con, "Channel", nextcID)

        Dim sql$
        Dim rdr As SqlClient.SqlDataReader

        Dim dicClones As Dictionary(Of String, String)
        dicClones = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase) 'A temporary dictionary (by channel id, child:parent)

        sql$ = "SELECT parenthost as parent,subhost as child,margin,marginpsg from " & server$ & "[channelcentral].customers.host_parents"
        rdr = da.DBExecuteReader(Con, sql$)
        While rdr.Read
            dicClones.Add(rdr.Item("child"), rdr.Item("parent"))
        End While
        rdr.Close()

        sql$ = "SELECT supplyChains,portfolios,ChannelID,ChannelName,h.CountryCode,c.Currency,hp.pic,hp.pic2,hp.url,hp.dp,hp.listpriceonlyskus,hp.feedonlyskus,hpreceta,terms,marginMin,MarginMax,marginType,hp.universal "
        sql$ &= "FROM " & server$ & "[channelcentral].customers.channel h "
        sql$ &= "JOIN " & server$ & "[iq].dbo.countries c on h.countrycode=c.countrycode "
        sql$ &= "LEFT JOIN " & server$ & "[channelcentral].customers.host_properties hp on hp.hostid= h.channelid "

        rdr = da.DBExecuteReader(Con, sql$)

        Dim achannel As clsChannel
        Dim isCloneOf As clsChannel = Nothing

        Dim cnc, crn, chn, channelID As String
        Dim priceConfig As Integer = 0

        While rdr.Read

            channelID = Trim$(rdr.Item("channelid"))
            If Not dicChannels.ContainsKey(channelID) Then

                crn = "" : cnc = "" : chn = ""
                cnc = UCase(Trim$(rdr.Item("countrycode")))
                If IsDBNull(rdr.Item("currency")) Then
                    Beep()
                Else
                    crn = Trim$(UCase(rdr.Item("currency")))
                End If

                priceConfig = 0
                If Not IsDBNull(rdr.Item("feedonlyskus")) Then
                    If rdr.Item("feedonlyskus") = 0 Then priceConfig = 1 'Show POA=NOT feedOnlySkus
                End If
                If Not IsDBNull(rdr.Item("ListPriceOnlySkus")) Then
                    If rdr.Item("ListpriceOnlyskus") Then priceConfig = priceConfig Or 2 'Locically OR on the '2 bit'
                End If

                If Not IsDBNull(rdr.Item("DP")) Then
                    If rdr.Item("DP") <> 0 Then priceConfig = priceConfig Or 4 'Show Base Price = DataProvider 
                End If


                priceConfig = priceConfig Or 8 ' we pretty much always want to display a specific price if we have it (with the posible exception of ebuyer)

                chn = Trim$(rdr.Item("channelname"))
                Dim focus As String = rdr.Item("SupplyChains") & "," & rdr.Item("portfolios")
                If rdr.Item("hpreceta") Then focus &= ",receta"
                Dim universal As Boolean = False
                If rdr.Item("univeral") IsNot Nothing Then
                    universal = CBool(rdr.Item("univeral"))
                End If

                If cnc = "UK" Then cnc = "GB"
                achannel = New clsChannel(Nothing, chn, "", "", channelID, dicRegions(cnc), New nullableString(rdr.Item("pic")), New nullableString(rdr.Item("pic2")), New nullableString(rdr.Item("URL")), priceConfig, "tree.1", focus, If(IsDBNull(rdr.Item("marginmin")), 0, rdr.Item("marginmin")), If(IsDBNull(rdr.Item("marginmax")), 20, rdr.Item("marginmax")), If(IsDBNull(rdr.Item("margintype")), "", Left(rdr.Item("margintype"), 1)), "", If(IsDBNull(rdr.Item("terms")), "", rdr.Item("terms")), Nothing, universal, "", "", "", channelWriteCache, nextcID)

                'this is NOT the iq.channels dictionary (which is autmoatically added to)
                dicChannels.Add(channelID, achannel)

            End If

        End While

        rdr.Close()

        'DBExecutesql(Con, "set identity_insert Channel ON")
        da.BulkWrite(Con, channelWriteCache, "Channel", , True)
        ' DBExecutesql(Con, "set identity_insert Channel OFF")

        channelWriteCache = Nothing

        'This bit isn't very clear - for each channel - it checks to see if it's a clone of another (in dicclones)

        Dim parent As clsChannel
        Dim child As clsChannel
        For Each channelID In dicChannels.Keys
            If dicClones.ContainsKey(channelID) Then  'this channel is a clone (a child)
                If dicChannels.ContainsKey(dicClones(channelID)) Then 'does the dictionary contain the parent (it should do now)
                    parent = dicChannels(dicClones(channelID))
                    child = dicChannels(dicClones(channelID))
                    child.IsCloneOf = parent
                    child.Update(errormessages) 'writes the new FK_Channel_Id_IsCloneOf  to the database
                Else
                    'the parent isn't loaded yet
                    Beep()
                End If
            Else
                isCloneOf = Nothing 'this will be turned to a max(ID)+1 - to clone itself
                ' End If
            End If
        Next

        Con.Close()
        Con = da.OpenDatabase()

        Dim dt As DataTable = da.MakeWriteCacheFor(Con, "Domain")

        Dim query As String = String.Empty
        query = "SELECT HostID, Host_Domain FROM " & server & "ChannelCentral.customers.Host_Domains  hd inner join  "
        query = query & server & "ChannelCentral.customers.Host_Properties hp on hd.HID= hp.HID order by hp.hid"
        rdr = da.DBExecuteReader(Con, query)
        Dim hostID As String
        Dim channel As clsChannel

        While rdr.Read
            hostID = Trim$(rdr.Item("HostID"))
            If Not iq.i_channel_code.ContainsKey(hostID) Then
                Logit("couldnt locate channel " & hostID)

            Else
                channel = iq.i_channel_code(hostID)
                Dim row As System.Data.DataRow
                row = dt.NewRow()
                row("Domain") = rdr.Item("Host_Domain")
                row("FK_Channel_ID") = channel.ID

                dt.Rows.Add(row)
            End If
        End While
        rdr.Close()

        da.BulkWrite(Con, dt, "Domain")


    End Sub

    Public Sub Currencies(con As SqlClient.SqlConnection, dicCurrencies As Dictionary(Of String, clsCurrency))

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$

        Dim added As Integer
        Dim loaded As Integer

        'CURRENCIES
        sql$ = "SELECT [CurrencyCode],[CurrencyName],[CurrencySymbol],[Notes] from " & server$ & "[iq].[dbo].[Currencies]"
        rdr = da.DBExecuteReader(con, sql$)

        Dim aCurrency As clsCurrency
        Dim cs As String
        Dim notes As clsTranslation

        While rdr.Read

            If dicCurrencies.ContainsKey(rdr.Item("currencycode")) Then
                loaded += 1
            Else
                added += 1

                cs = rdr.Item("currencySymbol")

                If InStr(cs, "&") Then
                    cs = HttpUtility.HtmlDecode(cs)
                End If

                notes = Nothing
                If Not IsDBNull(rdr.Item("notes")) Then
                    If Trim$(rdr.Item("notes")) <> "" Then
                        notes = iq.AddTranslation(rdr.Item("Notes"), English, "currnote", 0, Nothing, 0, False)
                    End If
                End If

                'If rdr.Item("currencycode") = "GBP" Then culture = "EN-gb"
                aCurrency = New clsCurrency(Trim$(rdr.Item("currencycode")), Nothing, iq.AddTranslation(Trim$(rdr.Item("currencyname")), English, "curr", 0, Nothing, 0, False), cs, 1, notes)
                dicCurrencies.Add(rdr.Item("currencycode"), aCurrency)

            End If
        End While
        rdr.Close()


    End Sub

    'Public Sub BuildTree(con As SqlClient.SqlConnection, _
    '                         dicSystems As Dictionary(Of String, clsBranch), _
    '                          optionsbysku As Dictionary(Of String, clsProduct), _
    '                          dicFamily As Dictionary(Of String, clsBranch), _
    '                          ByRef optionsByCK As Dictionary(Of String, clsProduct), _
    '                          dicOptType As Dictionary(Of String, clsProductType), _
    '                          dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)), _
    '                         ByRef errormessages As List(Of String))

    '    'OptionsByCK is keyed sysfamily^l1^l2^l3^optsn>Product
    '    'DicSystems - mfrSkU>Branch   (these breanches are already attached to their families)
    '    'we  need to graft on the correct optCats (L2's)

    '    Dim rdr As SqlClient.SqlDataReader
    '    Dim chassisTL As clsTranslation = iq.AddTranslation("Chassis", English, "")

    '    Dim chassisRoot As clsBranch = New clsBranch(Nothing, Nothing, chassisTL, "", chassisTL, chassisTL, Nothing, 100, False, "B")

    '    Dim alltl As clsTranslation = iq.AddTranslation("All Options", English, "UI")
    '    Dim tlOption As clsTranslation = iq.AddTranslation("Option", English, "UI")
    '    Dim tlOptions As clsTranslation = iq.AddTranslation("Options", English, "UI")


    '    '
    '    Dim dicSlotTypes As Dictionary(Of String, clsSlotType)

    '    'Return a dictionary of minorSlot|Type codes to slot types 
    '    dicSlotTypes = Import.slotTypes(con, dicSystems) 'dicFamily) '20 secs

    '    'Build a dictionary to look up the slot type per subfamily/option type
    '    '                                                                subfamily            option type
    '    'Dim dicSubFamOptTypeSlotType As Dictionary(Of String, Dictionary(Of String, clsSlotType))
    '    ' dicSubFamOptTypeSlotType = Import.SubFamiliyOptionTypes(con, dicSlotTypes)

    '    Dim ofDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamiliyOptTypeToOptFamily()

    '    'OPTION LIMITS 
    '    'build a dictionary by sysfamily/option family of the Limits - used later to attach instances of clsQuantity (autoAdds/Preinstalled) to the option branches

    '    '                                                  sysSubFam      optionfaimily       limit
    '    '                                                                       NHP35lff
    '    Dim dicOptLimits As Dictionary(Of String, IQ.clsLimit)
    '    dicOptLimits = Import.OptLimits(con, ofDic) 'returns sysfamilycode^optfamily>clslimit EG.. DL380PLFF^NHPLFF>blah

    '    'FACTORY INSTALLED OPTIONS/components - call them what you will
    '    'get a list (by system mfrSKU) of the part numbers and quantities of all factory installed components (PriStor, sec stor CPU, MEM etc,Raid)


    '    Dim dicFIOs As Dictionary(Of String, Dictionary(Of String, Integer))
    '    'returns a list (by system mfrSKU) of the quantities and part numbers of all factory installed components (PriStor, sec stor CPU, MEM etc)
    '    dicFIOs = Import.FIOs(con)


    '    WriteDicFIOs(dicFIOs, "c:\temp\fios.txt")

    '    'anevent = New clsEvent(buildTreeEvent, "", ev_Info)
    '    'we need just a translation key for each opt type (although many branches will reference each opt type)
    '    Dim dicOptFam As New Dictionary(Of String, clsTranslation)

    '    'Make a dictionary of option parent (Performance, storage etc) > branches

    '    'Populate the dictionary of all option families (MEM_PC3-1060/5.25LFF/Socket/INTEL-SocketT-52)
    '    rdr = da.DBExecuteReader(con, "SELECT distinct optfamily from " & server$ & "[iq].products.options")
    '    While rdr.Read
    '        dicOptFam.Add(Trim$(rdr.Item("optfamily")), iq.AddTranslation(Abbreviation(rdr.Item("optfamily")), English))
    '    End While
    '    rdr.Close()
    '    'anevent.update("Imported " & dicOptFam.Count & " options family codes")

    '    'Populate the dictionary of all option technologies (UDIMM/RDIMM/DDR3)/(SAS/SATA) (4 Core/8 Core)
    '    ' anevent = New clsEvent(buildTreeEvent, "", ev_Info)
    '    Dim dicOptTech As New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)  'makes the dictionary keys case insensitive
    '    rdr = da.DBExecuteReader(con, "SELECT distinct technology from " & server$ & "[iq].products.options")
    '    While rdr.Read
    '        If IsDBNull(rdr.Item("technology")) Then
    '            'many technologies are NULL
    '            dicOptTech.Add("unspecified technology", iq.AddTranslation("Unspecified technology", English, "", 0, Nothing, 0, False))  'New clsProduct("Unspecified technology", False, np, Nothing))
    '        Else
    '            dicOptTech.Add(Trim$(rdr.Item("technology")), iq.AddTranslation(Abbreviation(rdr.Item("Technology")), English)) 'New clsProduct(rdr.Item("technology").ToString, False, np, Nothing))
    '        End If
    '    End While
    '    rdr.Close()

    '    'anevent.update("Imported " & dicOptTech.Count & " options technology codes")

    '    Dim L1branch As clsBranch
    '    Dim L2branch As clsBranch
    '    'Dim optFamBranch As clsBranch
    '    ' Dim optTechBranch As clsBranch
    '    ' Dim optBranch As clsBranch

    '    'Dim OptProduct As clsProduct
    '    Dim grafts As Integer = 0
    '    Dim kept As Integer = 0
    '    Dim pruned As Integer = 0

    '    Dim Incompatibles As Integer = 0

    '    Dim xx As Integer = 0

    '    Dim invalidFamilies As New List(Of String)
    '    Dim invalidOptTypes As New List(Of String)

    '    Dim optTrans As clsTranslation
    '    optTrans = iq.AddTranslation("options", English)
    '    Dim optTransSingular As clsTranslation
    '    optTransSingular = iq.AddTranslation("option", English)

    '    'anevent = New clsEvent(buildTreeEvent, "", ev_Info)

    '    Dim nextBranchID As Integer = 0 'Will force the use of Next IDs (allowing un to know the branch ID before it's be BulkWritten
    '    Dim QuantityWriteCache As DataTable = da.MakeWriteCacheFor(con, "Quantity")
    '    Dim SlotWriteCache As DataTable = da.MakeWriteCacheFor(con, "Slot")
    '    Dim ProductAttributeWriteCache As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")

    '    Dim branchWriteCache As DataTable = da.MakeWriteCacheFor(con, "Branch", nextBranchID, True) 'will populate nextbrnachID with MAX(ID)+1

    '    Dim GraftWriteCache As DataTable = da.MakeWriteCacheFor(con, "Graft")
    '    Dim pruneWriteCache As DataTable = da.MakeWriteCacheFor(con, "Prune")

    '    Dim fams As Integer = 0
    '    '        Dim cats As Dictionary(Of String, Dictionary(Of String, clsBranch)) 'Holds sets of option categories (per sysfamily)


    '    Dim chassisProductType As clsProductType = (From j In iq.ProductTypes.Values Where j.Code = "CHAS").First
    '    Dim chassis As Dictionary(Of String, clsBranch)  'store up all the chassis products - so we can make the 'gives' slots en-mass
    '    chassis = New Dictionary(Of String, clsBranch)

    '    'SubFamily>Chassis Branch
    '    Dim chassisBranches As Dictionary(Of String, clsBranch) = New Dictionary(Of String, clsBranch)

    '    'The options dictionary has a compund key (sysfam^l1^l2^L3^optSN>product)
    '    '(the multidimensional dictionary is a sm

    '    'go through

    '    For Each ck In optionsByCK.Keys

    '        fams += 1

    '        Dim p() = Split(ck, "^")  '(sysfam^l1^l2^L3^optSN>product)

    '        Dim sysfamkey As String = p(0)
    '        If Not dicFamily.ContainsKey(sysfamkey) Then  'check the family is valid
    '            If Not invalidFamilies.Contains(sysfamkey) Then
    '                Logit("invalid family:[" & sysfamkey & "] not in (distinct sysFamilyName from sysfamilydefinitions)")
    '                invalidFamilies.Add(sysfamkey)
    '            End If
    '        Else

    '            Dim l1key As String = p(1)
    '            Dim l2key As String = p(2)
    '            Dim l3key As String = p(3)
    '            Dim optsn As String = p(4)

    '            Dim l2keys As Dictionary(Of clsBranch, String)  'needed in GraftOn, for a 'reverse' lookup of the OptTypeKey from the optTypeBranch
    '            l2keys = New Dictionary(Of clsBranch, String)

    '            'Dim optFamkeys As Dictionary(Of clsBranch, String)
    '            'optFamkeys = New Dictionary(Of clsBranch, String)

    '            Dim chassisBranch As clsBranch
    '            Dim chassisVariant As clsVariant
    '            Dim chassisProduct As clsProduct
    '            '  Dim MoboBranch As clsBranch
    '            '   Dim moboProduct As clsBranch


    '            'make the 'all options' branch (we'll graft this onto every system in the family later)
    '            'If Not iq.i_screens_code.ContainsKey("base") Then
    '            'Dim ascreen As clsScreen = New clsScreen("Branch", "Options", "Options", errormessages)
    '            'End If

    '            Dim AllBranch As clsBranch = New clsBranch(Nothing, Nothing, alltl, "", tlOptions, tlOption, iq.i_screens_code("Base"), 30, False, "B", branchWriteCache, nextBranchID)

    '            Dim familybranch As clsBranch = dicFamily(sysfamkey)  'We already made family branches earlies

    '            Dim firstsystem As clsProduct = dicFamily(sysfamkey).childBranches.Values(0).Product

    '            'Create a chassis for every subFamily, and graft it onto every system in that subfamily
    '            ' For Each SupplyChain In dicFamily(sysfamkey).childBranches.Values 'systems reside under supply chains
    '            For Each Systembranch In familybranch.childBranches.Values
    '                Dim sysSubFamily As String  'comes from the iq.systems.familycode
    '                sysSubFamily = Systembranch.Product.i_Attributes_Code("subFamily")(0).Translation.text(s_lang)  'IMPORTANT for compatibility

    '                If chassisBranches.ContainsKey(sysSubFamily) Then 'only make 1 chassis per subfamily
    '                    chassisBranch = chassisBranches(sysSubFamily)
    '                Else
    '                    chassisProduct = New clsProduct(False, firstsystem.Sector, chassisProductType, DateAdd(DateInterval.Day, -500, Now), DateAdd(DateInterval.Day, 10000, Now), True, False, True)
    '                    chassisVariant = New clsVariant("", chassisProduct, HP, chassisProduct.ID.ToString, "", "", "", r_worldwide, 0) 'Every product needs a variant - so it can be stored in a QuoteItem
    '                    chassisBranch = New clsBranch(chassisProduct, chassisRoot, iq.AddTranslation(sysSubFamily & " chassis", English), "", chassisTL, chassisTL, Nothing, 100, True, "B", branchWriteCache, nextBranchID)
    '                    chassisBranches.Add(sysSubFamily, chassisBranch)

    '                    'Systems Name (is SKU)
    '                    'Dim n As clsProductAttribute = New clsProductAttribute(chassisProduct, iq.i_attribute_code("~ame"), 0, iq.i_unit_code("txt"), dicFamily(sysfamkey).Translation, ProductAttributeWriteCache)

    '                    Dim cq As clsQuantity = New clsQuantity(r_worldwide, "", chassisBranch, 1, 0, 0, True, QuantityWriteCache) 'make a global auto add - to add the chassis to the system

    '                    'all the gives slots are in the chassis (for now)
    '                    ' MakeGivesSlots(chassisBranch, dicOptLimits(sysSubFamily), dicSlotTypes, dicSubFamOptTypeSlotType, QuantityWriteCache, SlotWriteCache)
    '                End If
    '                'graft the chassis on to every system - It already has a (global) quanity to auto-add it
    '                Systembranch.Graft(chassisBranch, "BuildTree", "", errormessages, GraftWriteCache)
    '                Systembranch.Graft(AllBranch, "Buildtree", "", errormessages, GraftWriteCache) 'pre-graft the 'all options' branch onto every system in the family

    '            Next 'system (In the family)


    '            ' These branches need to hang in space (before being grafted into multiple locations) (under each system in a family)
    '            'make the l1-3 branches, plus options ..

    '            L1branch = New clsBranch(Nothing, AllBranch, iq.AddTranslation(l1key, English), "", optTrans, optTransSingular, iq.Screens(719), 100, False, "T", branchWriteCache, nextBranchID)

    '            'dicsysfam = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct))))))
    '            '                                            sysfam(dl380g6Lffa)      optcat(perf)            opttype(mem)           optFamily (SODIM)    OptTech(ddr3)           IQ1SN                       


    '            'All Options - TRO and Upsell Opportunities
    '            L2branch = New clsBranch(Nothing, L1branch, dicOptType(LCase(l2key)).Translation, "", optTrans, optTransSingular, Nothing, 100, False, "T", branchWriteCache, nextBranchID)
    '            l2keys.Add(L2branch, l2key)
    '            If l2key IsNot Nothing Then
    '                'adds the the opt family,opttechnology and option branches - to this optTypeBranch
    '                If l2key <> "CPU" Then ' Stop 'we don't need to add CPU option.. we make a bespoke, singleton branch on the fly.                  
    '                    '                    AddOptions(L2branch, l2key, sysfamkey, l1key, optionsByCK, dicOptType, optTransSingular, optTrans, branchWriteCache, dicOptFam, dicOptTech, nextBranchID)
    '                Else
    '                    'just make an empty branch which will hold the lone CPU
    '                    '            cpuHolder = optTypeBranch 'this CPU opttype branch exists once for each family (every system in the family uses it)
    '                End If
    '            End If
    '            '  Next l2key
    '            '   Dim cpusku As String
    '            '   Dim cpuBranch As clsBranch = Nothing

    '            'some things (takes slots) - will have global scope (apply wherever this BRANCH appears) and only need be made once (per family) 
    '            'Others (preinstalled quanitites, localisations) - vary by system - and must be made for each option in the category branch

    '            Dim systemSKU As String
    '            Dim firstinfamily As Boolean = True

    '            '            'we now have a completed the All Branch (containing all the optCatbranches) we can graft this onto every system in the family  (Pruning off incomatibles)
    '            'For Each SupplyChain In dicFamily(l1key).childBranches.Values 'systems reside under supply chains
    '            '    For Each Systembranch In SupplyChain.childBranches.Values
    '            '        'IMPORTANT
    '            '        If Systembranch.Product.isSystem Then   'for each system
    '            '            systemSKU = Systembranch.Product.sku

    '            '            'If systemSKU = "662257-421" Then Stop ' should have a 662266-b21 cpu

    '            '            ' Systembranch.Graft(optCatBranch, "import", GraftWriteCache) 'Graft the WHOLE option category Branch on to each system (in the supply chain, in the family)
    '            '            ' grafts += 1

    '            '            Systembranch.Graft(AllBranch, "import", "", errormessages, GraftWriteCache) 'Graft the WHOLE option category Branch on to each system (in the supply chain, in the family)
    '            '            grafts += 1

    '            '            'make autoAdds (quantities), takes slots - and prune incompatible option branches
    '            '            Dim syspath$
    '            '            syspath$ = "tree." & Trim$(iq.RootBranch.ID)
    '            '            syspath$ &= "." & Trim$(SupplyChain.Parent.Parent.ID) 'System type
    '            '            syspath$ &= "." & Trim$(SupplyChain.Parent.ID) 'Family
    '            '            syspath$ &= "." & Trim$(SupplyChain.ID) 'supply chain  '<<<new
    '            '            syspath$ &= "." & Trim$(Systembranch.ID) 'system

    '            '            'If systemSKU = "668812-421" Then Stop
    '            '            '                                If systemSKU = "646902-421" Then Stop 'DL380P
    '            '            'Option:'647893-B21' 647893-B21 QTY:4
    '            '            'Option:'656362-B21' 656362-B21 QTY:1


    '            '            'makes autoAdds (quantities) for FIOs, Takes slots - and prunes incompatible option branches - on a set of option category branches (eg. Performance,Managment....)
    '            '            Limits(Systembranch, syspath$, AllBranch, options, l2keys, GraftWriteCache, SlotWriteCache, pruneWriteCache, _
    '            '                                 QuantityWriteCache, firstinfamily, SupplyChain, dicOptLimits, dicSlotTypes, dicOptLocalisation, dicFIOs, systemSKU, kept, pruned, ofDic)

    '            '            firstinfamily = False

    '            '            Dim sysSubFamily As String  'comes from the iq.systems.familycode
    '            '            sysSubFamily = Systembranch.Product.i_Attributes_Code("subFamily")(0).Translation.text(s_lang)  'IMPORTANT for compatibility

    '            '        End If 'isSystem
    '            '        ' End If
    '            '    Next Systembranch
    '            'Next SupplyChain

    '            da.BulkWrite(con, SlotWriteCache, "Slot")
    '            SlotWriteCache = da.MakeWriteCacheFor(con, "Slot")


    '            da.BulkWrite(con, pruneWriteCache, "Prune")


    '            pruneWriteCache = da.MakeWriteCacheFor(con, "Prune")

    '            da.BulkWrite(con, GraftWriteCache, "Graft")

    '            GraftWriteCache = da.MakeWriteCacheFor(con, "Graft")

    '            da.BulkWrite(con, QuantityWriteCache, "Quantity")
    '            QuantityWriteCache = da.MakeWriteCacheFor(con, "Quantity")

    '            da.DBExecutesql(con, "set identity_insert Branch ON")
    '            da.BulkWrite(con, branchWriteCache, "Branch", , True)
    '            da.DBExecutesql(con, "SET IDENTITY_INSERT branch OFF")

    '            Dim nbid As Integer = nextBranchID
    '            nbid = nextBranchID
    '            nextBranchID = 0
    '            branchWriteCache = da.MakeWriteCacheFor(con, "Branch", nextBranchID, True)
    '            'If nextBranchID <> nbid Then Stop 'elaborate  error tracking - remove

    '            da.BulkWrite(con, ProductAttributeWriteCache, "ProductAttribute")

    '        End If
    '    Next ck



    '    'BulkWrite(con, SlotWriteCache, "Slot")
    '    'SlotWriteCache = Nothing

    '    'anevent = New clsEvent(buildTreeEvent, "", ev_Info)
    '    'BulkWrite(con, pruneWriteCache, "Prune")

    '    'anevent.update("Bulk Wrote " & pruneWriteCache.Rows.Count & " prunes")
    '    'pruneWriteCache = Nothing

    '    'anevent = New clsEvent(buildTreeEvent, "", ev_Info)

    '    'BulkWrite(con, GraftWriteCache, "Graft")

    '    'anevent.update("Bulk Wrote " & GraftWriteCache.Rows.Count & " grafts ")
    '    'GraftWriteCache = Nothing

    '    'BulkWrite(con, QuantityWriteCache, "Quantity")
    '    'QuantityWriteCache = Nothing

    '    ''DBExecutesql(con, "set identity_insert Branch ON")
    '    'BulkWrite(con, branchWriteCache, "Branch", , True)
    '    ''DBExecutesql(con, "SET IDENTITY_INSERT branch OFF")
    '    'branchWriteCache = Nothing

    '    optionsByCK = Nothing 'free the (very large amount of) memory
    '    ' dicSKUOptionProduct = Nothing

    '    'makes the gives slots (on the chassis branches)
    '    Import.OptionLimits(chassisBranches, dicSlotTypes)

    '    ' Logit("Recorded " & cpuBranches.Count & " CPU branches")

    '    Logit("built tree", False, True)


    'End Sub

    ''Private Function RecordCPUsku(systembranch As clsBranch, cpubranches As Dictionary(Of String, clsBranch), cpuroot As clsBranch, _
    ''                              ByRef branchWriteCache As DataTable, ByRef nextbranchid As Integer, systemsku As String) As String

    ''    Dim cpusku As String = ""
    ''    Dim cpubranch As clsBranch

    ''    If systembranch.Product.i_Attributes_Code.ContainsKey("cpuSKU") Then
    ''        cpusku = systembranch.Product.i_Attributes_Code("cpuSKU")(0).Translation.text(English)

    ''        'buld a master tree of CPU's as we go
    ''        If cpubranches.ContainsKey(cpusku) Then
    ''            cpubranch = cpubranches(cpusku)
    ''        Else
    ''            If iq.i_SKU.ContainsKey(cpusku) Then
    ''                Dim cpuProd As clsProduct = iq.i_SKU(cpusku)
    ''                'cpubranch = New clsBranch(iq.i_SKU(cpusku),  cpuroot, cpuProd.i_Attributes_Code("~ame")(0).Translation, "", cpuroot.CollectiveNoun, cpuroot.collectiveNounSingular, Nothing, 100, branchwritecache, nextbranchid)
    ''                cpubranch = New clsBranch(iq.i_SKU(cpusku), cpuroot, cpuProd.i_Attributes_Code("MfrSKU")(0).Translation, "", cpuroot.CollectiveNoun, cpuroot.collectiveNounSingular, Nothing, 100, False, branchWriteCache, nextbranchid)
    ''                cpubranches.Add(cpusku, cpubranch)
    ''            Else
    ''                If Not cpusku.StartsWith("###") Then
    ''                    Logit("No such CPU " & cpusku & " for " & systembranch.DisplayName(English))
    ''                End If

    ''            End If
    ''        End If

    ''        'If dicFIOs(systemSKU).ContainsKey(cpusku) Then
    ''        '    cpuqty = dicFIOs(systemSKU)(cpusku)
    ''        'Else
    ''        '    Stop
    ''        'End If
    ''    Else
    ''        Logit(systemsku & " has no CPU ")
    ''    End If

    ''    Return cpusku

    ''End Function

    <Obsolete("ML - do not run")>
    Public Sub InterfaceSlots()
        'Ok, lets get the data on any drives which take SAS

        '  Dim MonsterIndex As Dictionary(Of clsProduct, List(Of String)) 'A list of all the paths at which a product appears
        '  MonsterIndex = New Dictionary(Of clsProduct, List(Of String))
        '  iq.RootBranch.IndexProductPaths("tree." & Trim(iq.RootBranch.ID), MonsterIndex, False, True, Nothing)  ' 180 SECS !

        Dim requiresSASAttribute = If(iq.i_attribute_code.ContainsKey("requireSAS"), iq.i_attribute_code("requireSAS"), New clsAttribute("requireSAS", iq.AddTranslation("Requires SAS", English, "", 0, Nothing, 0, False), 0))
        Dim supportsSASAttribute = If(iq.i_attribute_code.ContainsKey("supportSAS"), iq.i_attribute_code("supportSAS"), New clsAttribute("supportSAS", iq.AddTranslation("Supports SAS", English, "", 0, Nothing, 0, False), 0))
        Dim aProp As clsProductAttribute

        Dim iq1con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")

        'Dim SkuIndex As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))()
        'iq.Branches(1).SkuPaths(SkuIndex, "tree.1", True)

        Dim iq2con As SqlClient.SqlConnection = da.OpenDatabase()

        'Dim wc As DataTable = da.MakeWriteCacheFor(iq2con, "Slot")
        Dim wc As DataTable = da.MakeWriteCacheFor(iq2con, "ProductAttribute")

        Dim rdr As SqlClient.SqlDataReader
        Dim sql = "select 0 as gives, 1 as takes,optsku from products.options where Technology='SAS' and optsku not like '###%' and opttype  in ('TAP','HDD') and (activetodate > getdate() or activetodate is null) union all select intport as gives,0 as takes,optsku from products.options left outer join  products.[HierarchyFull] h on optsku=h.upcnum  left outer join products.OptRAIDprops ro on h.ccDescription LIKE '%'+ro.RAIDfamily+'%'  where Technology='SAS' and optsku not like '###%' and opttype  not in ('TAP','HDD','CHK','IOC')"

        rdr = da.DBExecuteReader(iq1con, sql$)

        Dim locs As New Dictionary(Of String, List(Of clsBranch))

        Dim givers As New List(Of clsProduct) 'all the options that give slots
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("optsku")) Then
                givers.Add(iq.i_SKU(rdr.Item("optsku")))
            End If
        End While
        rdr.Close()

        Dim branch As clsBranch
        Dim index As Dictionary(Of Int32, String) = New Dictionary(Of Int32, String)
        For Each branch In iq.Branches.Values
            If givers.Contains(branch.Product) Then
                If Not locs.ContainsKey(branch.Product.SKU) Then locs.Add(branch.Product.SKU, New List(Of clsBranch))
                locs(branch.Product.SKU).Add(branch)
                index.Add(branch.ID, Nothing)
            End If
        Next

        iq.Branches(1).indexProductBranchesByPath("tree", True, index)

        Dim aslot As clsSlot
        Dim st As clsSlotType

        If Not iq.i_slotType_Code.ContainsKey("SAS") OrElse iq.i_slotType_Code("SAS").ContainsKey("SAS") Then Dim s = New clsSlotType("SAS", "SAS", iq.AddTranslation("SAS", English, "", 0, Nothing, 0, False))
        st = iq.i_slotType_Code("SAS")("SAS")
        Dim added As Integer = 0

        Dim notfound As Integer = 0

        Dim product As clsProduct
        rdr = da.DBExecuteReader(iq1con, sql$)
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("optsku")) Then
                If rdr.Item("optsku") = "726821-B21" Then
                    Dim a = 1
                End If
                product = iq.i_SKU(rdr.Item("optsku"))
                If locs.ContainsKey(product.SKU) Then
                    Dim done As List(Of clsBranch) = New List(Of clsBranch)
                    For Each path In locs(product.SKU)
                        'branch = iq.Branches(Split(path, ".").Last)

                        If Not done.Contains(path) Then
                            If rdr.Item("gives") IsNot DBNull.Value AndAlso CInt(rdr.Item("gives")) > 0 Then aProp = New clsProductAttribute(product, supportsSASAttribute, 1, iq.i_unit_code("txt"), Nothing, wc)
                            If rdr.Item("gives") IsNot DBNull.Value AndAlso CInt(rdr.Item("gives")) > 0 Then aProp = New clsProductAttribute(product, requiresSASAttribute, 1, iq.i_unit_code("txt"), Nothing, wc)

                            'If rdr.Item("gives") IsNot DBNull.Value AndAlso CInt(rdr.Item("gives")) > 0 Then aslot = New clsSlot(st, path, "", CInt(rdr.Item("gives")), Nothing, New NullableInt(), 0, 0, wc)
                            'If rdr.Item("takes") IsNot DBNull.Value AndAlso CInt(rdr.Item("takes")) > 0 Then aslot = New clsSlot(st, path, "", -CInt(rdr.Item("takes")), Nothing, New NullableInt(), 0, 0, wc)

                            done.Add(path)
                        End If
                    Next
                Else
                    notfound += 1
                End If
            Else
                '        Stop
            End If

        End While

        rdr.Close()

        'da.BulkWrite(iq2con, wc, "Slot")
        da.BulkWrite(iq2con, wc, "ProductAttribute")

        If notfound > 0 Then Stop

    End Sub


    'TODO add to incremental import
    ' ''' <summary>
    ' ''' Imports OPTIONS that add slots (such as drive cages)
    ' ''' </summary>
    Public Function slotAdds(con As SqlClient.SqlConnection) As String

        Dim i As Integer

        '        Dim MonsterIndex As Dictionary(Of clsProduct, List(Of String)) 'A list of all the paths at which a product appears
        '        MonsterIndex = New Dictionary(Of clsProduct, List(Of String))
        '        iq.RootBranch.SkuPaths("tree." & Trim(iq.RootBranch.ID), MonsterIndex, False, True, Nothing)  ' 180 SECS !

        Dim wc As DataTable = da.MakeWriteCacheFor(con, "Slot")
        Dim stDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamilyOptTypeToOptFamily


        Dim sql$
        Dim rdr As SqlClient.SqlDataReader

        sql$ = "SELECT optsku,optType,slotAddType,SlotAddQty,optFamily,sysFamily "
        sql$ &= "FROM " & server$ & "[iq].Products.options "
        sql$ &= "WHERE slotaddqty is not null AND slotaddtype is not null "
        sql$ &= "AND (sYSFAMILY LIKE '%" & restrictImportToFamily & "%' or sysfamily = '' or sysfamily is null )"

        '487936-B21	CHK	HDD;OPT	2;-2	HP ML350/ML370/DL370 G6 Two-bay LFF Drive Cage Option Kit

        'We need a list of every branch holding a product which add slots (we don't actually need the paths!)
        rdr = da.DBExecuteReader(con, sql$)

        'OptionSKU>List of branches having that product
        Dim locs As New Dictionary(Of String, List(Of clsBranch))

        Dim givers As New List(Of clsProduct) 'all the options that give slots
        While rdr.Read
            If iq.i_SKU.ContainsKey(rdr.Item("optsku")) Then
                givers.Add(iq.i_SKU(rdr.Item("optsku")))
            End If
        End While
        rdr.Close()

        Dim branch As clsBranch
        Dim index As Dictionary(Of Int32, String) = New Dictionary(Of Int32, String)
        For Each branch In iq.Branches.Values
            If givers.Contains(branch.Product) Then
                If Not locs.ContainsKey(branch.Product.SKU) Then locs.Add(branch.Product.SKU, New List(Of clsBranch))
                locs(branch.Product.SKU).Add(branch)
                index.Add(branch.ID, Nothing)
            End If
        Next

        iq.Branches(1).indexProductBranchesByPath("tree", True, index)

        Dim aslot As clsSlot
        Dim st As clsSlotType
        Dim added As Integer = 0
        Dim notfound As Integer = 0

        'same SQL pass 2
        rdr = da.DBExecuteReader(con, sql$)

        Dim optionProduct As clsProduct
        While rdr.Read

            Dim optSKU As String = rdr.Item("optsku")
            If optSKU = "662883-B21" Then
                Dim a = 1
            End If
            If iq.i_SKU.ContainsKey(optSKU) Then
                If optSKU = "675843-B21" Then
                    Dim a = 90
                End If
                optionProduct = iq.i_SKU(optSKU)

                Dim types() As String = rdr.Item("slotaddType").ToString().Split(";".ToArray(), StringSplitOptions.RemoveEmptyEntries)
                Dim qtys() As String = rdr.Item("slotaddqty").ToString().Split(";".ToArray(), StringSplitOptions.RemoveEmptyEntries)

                If locs.ContainsKey(optSKU) Then
                    Dim done As List(Of clsBranch) = New List(Of clsBranch)
                    For Each branch In locs(optSKU)

                        For i = 0 To UBound(types)
                            ' If iq.i_slotType_Code.ContainsKey(types(i)) Then
                            ' st = iq.i_slotType_MinorCode(types(i))
                            ' aslot = New clsSlot(st, branch, "", qtys(i), Nothing, New NullableInt(), 0, 0, wc)
                            ' added += 1
                            ' Else
                            If index.ContainsKey(branch.ID) Then
                                Dim idx = index(branch.ID)
                                If Not iq.i_slotType_Code.ContainsKey(types(i)) Then Dim f = New clsSlotType(types(i), types(i), iq.AddTranslation(types(i), English, "st", 0, Nothing, 0, False))
                                If FindSystemBranch(idx) IsNot Nothing AndAlso FindSystemBranch(idx).Product.i_Attributes_Code.ContainsKey("FamMajor") AndAlso stDic.ContainsKey(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English)) Then
                                    If stDic(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English)).ContainsKey(types(i)) AndAlso iq.i_slotType_Code(types(i)).ContainsKey(stDic(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English))(types(i))) Then
                                        st = iq.i_slotType_Code(types(i))(stDic(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English))(types(i)))
                                        'Dim alreadythere = branch.slots.Where(Function(sl) sl.Value.Type Is st AndAlso Math.Sign(sl.Value.numSlots) <> Math.Sign(CInt(qtys(i))))
                                        'For Each s In alreadythere.ToList()
                                        '    s.Value.delete()
                                        'Next
                                        If branch.slots IsNot Nothing AndAlso branch.slots.Where(Function(sl) sl.Value.Type Is st AndAlso Math.Sign(sl.Value.numSlots) = Math.Sign(CInt(qtys(i)))).Count = 0 Then
                                            aslot = New clsSlot(st, branch, "", qtys(i), Nothing, New NullableInt(), 0, 0, wc)
                                            added += 1
                                            Logit("DICT MATCH," & optSKU & "," & types(i) & "," & If(IsDBNull(rdr.Item("optFamily")), "", rdr.Item("optFamily")) & "," & stDic(FindSystemBranch(idx).Product.i_Attributes_Code("FamMajor").First.Translation.text(English))(types(i)))
                                        End If
                                    Else
                                        If iq.i_slotType_Code.ContainsKey(types(i)) Then
                                            st = iq.i_slotType_Code(types(i)).First.Value
                                            If branch.slots IsNot Nothing AndAlso branch.slots.Where(Function(sl) sl.Value.Type Is st).Count = 0 Then aslot = New clsSlot(st, branch, "", qtys(i), Nothing, New NullableInt(), 0, 0, wc)
                                            added += 1
                                        End If
                                        Logit("NO MATCH FOR MINOR" & optSKU & "," & types(i) & "," & If(IsDBNull(rdr.Item("optFamily")), "", rdr.Item("optFamily")))

                                    End If
                                Else : Logit("NO MATCH," & optSKU & "," & types(i) & "," & If(IsDBNull(rdr.Item("optFamily")), "", rdr.Item("optFamily")))
                                End If
                            Else : Logit("NO MATCH," & optSKU & "," & types(i) & "," & If(IsDBNull(rdr.Item("optFamily")), "", rdr.Item("optFamily")))
                            End If


                            'Dim sf As String() = rdr.Item("sysFamily").split(",")
                            'Dim sf2 = stDic.Where(Function(std) stDic.Keys.Intersect(sf).Contains(std.Key)).Where(Function(fg) fg.Value.ContainsKey(types(i))).Select(Function(ff) ff.Value(types(i))).Distinct()
                            'If sf2.Count > 1 Then
                            '    Stop
                            'ElseIf sf2.Count > 0 Then
                            '    'Is our slot type actually in the family??? PSU Kits for example
                            '    If iq.i_slotType_MinorCode.ContainsKey(sf2.First) Then
                            '        st = iq.i_slotType_MinorCode(sf2.First)
                            '        aslot = New clsSlot(st, branch, "", qtys(i), Nothing, New NullableInt(), 0, 0, wc)
                            '        added += 1
                            '        Logit("Added option type based on family dictionary lookup: " & optSKU & "," & types(i) & "," & If(IsDBNull(rdr.Item("optFamily")), "", rdr.Item("optFamily")))
                            '    Else
                            '        Debug.Print(types(i) & " is an unknown slot type")
                            '    End If


                            '  End If
                        Next

                    Next
                Else
                    notfound += 1
                End If
            Else
                Logit("NO SKU," & optSKU)
            End If

        End While

        rdr.Close()

        da.BulkWrite(con, wc, "Slot")

        ' If notfound > 0 Then Stop

        Return "Added:" & added & " slot adds"

    End Function

    'Not used?
    Private Sub OptionLimits(chassisbranches As Dictionary(Of String, clsBranch), dicslottypes As Dictionary(Of String, clsSlotType))

        'option limits become the 'gives' slots on the chassis branches
        'Option limits only tell us the optType - but we need the more granular OptFamily - this dictionary provided the necessary (and tricky lookup)
        '                                sysfamily                  opttype optfamily
        Dim stDic As Dictionary(Of String, Dictionary(Of String, String)) = Import.FamilyOptTypeToOptFamily

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()

        'chassisBranches are at a MinorFamily level
        'however some of the option limits are specified at a family level - wherein we need to make a slot in every chassis in the family
        'so - we need a dictionary of family>Minorfamilies

        Dim dicfamilies As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))(StringComparer.CurrentCultureIgnoreCase)

        'Sysfamilyname is broad(major)family, sysfamily is the narrow(minor) subfamily
        'DL560G8	DL560G8C5SFFLRD
        sql$ = "Select sysfamilyname as FamMajor,sysfamily as FamMinor from " & server$ & "iq.products.sysfamilydefinitions"

        'we build a dictionary of the 'broads' to all the 'narrows'
        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            If Not dicfamilies.ContainsKey(rdr.Item("FamMajor")) Then
                dicfamilies.Add(rdr.Item("FamMajor"), New List(Of String))
            End If

            dicfamilies(rdr.Item("famMajor")).Add(rdr.Item("famMinor"))
        End While
        rdr.Close()




        Dim slotWriteCache As DataTable = da.MakeWriteCacheFor(con, "slot")

        sql$ = "SELECT [SysFamily],[OptFamily],[OptType],[QtyInstalled],[QtyMax],[Incr_Min],[Incr_Pref] from " & server$ & "[iq].[products].[OptionLimits] "

        'Return WLAN,FAN,MEM,PSU etc (opt types)

        rdr = da.DBExecuteReader(con, sql$)

        Dim aslot As clsSlot
        Dim branch As clsBranch

        Dim st As clsSlotType
        Dim sysFam As String
        Dim opttype As String
        Dim optfamily As String
        Dim Gives As Integer = 0
        Dim lines As Integer = 0

        Dim dudFamilies As List(Of String) = New List(Of String)
        Dim dudOptFamilies As List(Of String) = New List(Of String)

        While rdr.Read
            lines += 1

            sysFam = Trim(rdr.Item("sysfamily"))  'this is sometimes a 'narrow' subFamily - sometimes the broad 'sysfamilyname'
            opttype = rdr.Item("opttype")
            optfamily = rdr.Item("optfamily")

            If Not stDic.ContainsKey(sysFam) Then
                If Not dudFamilies.Contains(sysFam) Then
                    dudFamilies.Add(sysFam)
                    Logit(sysFam & " is not a valid system family") 'This is probably a MinorCode
                    'and woudl expainl why we get no OS slots on some systems !
                End If
            Else
                If Not stDic(sysFam).ContainsKey(opttype) Then
                    'some opt type for which we dont' need to limit by slots (software or card readers or something)
                    '   Beep()
                Else
                    'Dim optfamily As String = stDic(sysFam)(opttype)
                    If Not dicslottypes.ContainsKey(opttype & "^" & optfamily) Then
                        If Not dudOptFamilies.Contains(opttype & "^" & optfamily) Then
                            Logit(opttype & "^" & optfamily & " is not valid")
                            dudOptFamilies.Add(opttype & "^" & optfamily)
                        End If
                    Else
                        st = dicslottypes(opttype & "^" & optfamily)
                        If chassisbranches.ContainsKey(sysFam) Then
                            branch = chassisbranches(sysFam)
                            If st.MajorCode = "CPU" Then Stop
                            aslot = New clsSlot(st, branch, "", rdr.Item("qtymax"), Nothing, New NullableInt(), 0, 0, slotWriteCache)
                            Gives += 1
                        Else
                            'The sysFam wasn't a subfamily (it must be a broader 'family') - so we need to make this slot on every chassis in the family

                            If False Then
                                'this is obsoleted 'Codes'

                                If dicfamilies.ContainsKey(sysFam) Then
                                    'make a slot on every chassis in the family

                                    For Each subfam In dicfamilies(sysFam)

                                        If chassisbranches.ContainsKey(subfam) Then
                                            branch = chassisbranches(subfam)
                                            '     If rdr.Item("qtymax") = 0 Then Stop - means 'no maximum'
                                            aslot = New clsSlot(st, branch, "", rdr.Item("qtymax"), Nothing, New NullableInt(), 0, 0, slotWriteCache)
                                            Gives += 1
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End While
        rdr.Close()

        da.BulkWrite(con, slotWriteCache, "slot")

        Logit("Done optionlimiits", False, True)

        con.Close()


    End Sub


    'Not used -  ML?
    Private Sub MakeGivesSlots(chassisBranch As clsBranch, sysSubFamily As String, _
                               dicoptlimits As Dictionary(Of String, Object), _
                               dicslottypes As Object, dicSubFamOptTypeSlotType As Dictionary(Of String, Dictionary(Of String, clsSlotType)), _
                              quantityWriteCache As DataTable, slotWriteCache As DataTable) '    ,  cpuBranch As clsBranch, CPUsInstalled As Integer)

        '---- OBSOLETED ---
        'now done 'in-line' in buildtree



        'Gives slots do NOT come from products.systems, or sysfamilydefinitions.. these only tell us the preinstalled quantities (although they do tell us the optionFamily)
        'they come from the optType limits - per system family

        'dicsysfam = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct))))))
        '                                                 sysfam                       optcat                         optfam                          opttech                  opttype                       

        'Now make the 'gives' slots

        'Dim sysSubFamily As String  'comes from the iq.systems.familycode
        'sysSubFamily = Systembranch.Product.i_Attributes_Code("subFamily")(0).Translation.text(s_lang)  'IMPORTANT for compatibility

        'Dim syspath$
        'syspath$ = "tree." & Trim$(iq.RootBranch.ID)
        'syspath$ &= "." & Trim$(supplychain.Parent.Parent.ID) 'System type
        'syspath$ &= "." & Trim$(supplychain.Parent.ID) 'Family
        'syspath$ &= "." & Trim$(supplychain.ID) 'supply chain  '<<<new
        'syspath$ &= "." & Trim$(systemBranch.ID) 'system

        Dim limit As clsLimit

        'make the gives slots on each system for each option type (MEM/HDD/CPU etc.) - according to the option type limits  (note - this does not do the PCI slots)
        Dim gslot As clsSlot

        Dim st As clsSlotType

        'Dim optTypekey As String
        'For Each opttypebranch In optCatBranch.childBranches.Values  'key In dicsysfam(optCatkey).keys
        '    '                                                                                                                subfam       cat           OptType>limit 
        '    optTypekey = optTypekeys(opttypebranch)

        '    'graft on the CPU (from a 'master' tree of CPU's)
        '    If optTypekey = "CPU" Then
        '        ' If Not opttypebranch.childBranches.Values.Contains(cpuBranch) Then
        '        If Not opttypebranch.childBranches.Values.Contains(cpuBranch) Then
        '            opttypebranch.Graft(cpuBranch, "", graftwriteCache)  'very neat
        '            ' End If
        '            Dim cpupath$ = syspath$ & "." & Trim$(opttypebranch.ID) & "." & Trim$(cpuBranch.ID)

        '            Dim minIncr As Integer = 1
        '            Dim preferredIncr As Integer = 1
        '            Dim limits As clsLimit
        '            If dicoptlimits(sysSubFamily).containskey("Performance") Then
        '                If dicoptlimits(sysSubFamily)("Performance").containskey("CPU") Then
        '                    limits = dicoptlimits(sysSubFamily)("Performance")("CPU")
        '                    preferredIncr = limits.PrefIncr
        '                    minIncr = limits.MinIncr
        '                End If
        '            End If

        '            'AutoAdd (of the right CPU)
        '            Dim qty As New clsQuantity(iq.i_region_code("IX"), cpupath$, cpuBranch, iq.StandardVariant, cpuqty, minIncr, preferredIncr, True, quantityWriteCache)

        '            'make each CPU take a CPU slot
        '            Dim cpuTakeslot As clsSlot = New clsSlot(iq.i_slotType_code("CPU"), cpuBranch, cpupath, -1, Nothing, New NullableInt(), 1, 0, slotWriteCache)
        '        End If
        '    End If

        'are there special limits.increments on this option Type (slot type) 

        If dicoptlimits.ContainsKey(sysSubFamily) Then
            For Each optCatkey In dicoptlimits(sysSubFamily).keys
                For Each optTypekey In dicoptlimits(sysSubFamily)(optCatkey).keys  'The opttype keys are MEM,CPU, HDD etc ... not granular enough for slots - so we look the right slot type up for the subFamily/OptType

                    limit = dicoptlimits(sysSubFamily)(optCatkey)(optTypekey)

                    st = dicSubFamOptTypeSlotType(sysSubFamily)(optTypekey)
                    'If dicslottypes.ContainsKey(optTypekey) Then

                    'st = dicslottypes(optTypekey)
                    '                                                                                                                     requiredFill
                    'If limit.Qmin Then Stop ' Required fill (check they're goning in on)
                    gslot = New clsSlot(st, chassisBranch, "", limit.Qmax, Nothing, New NullableInt, limit.Qmin, 0, slotWriteCache)
                    'Else
                    'Logit("invalid slot/option type:" & optTypekey)
                    ' End If
                Next optTypekey
            Next optCatkey
        End If

    End Sub


    ''' <summary>makes autoAdds (quantities) for FIOs, takes slots - and prunes incompatible option branches - on single option category branch (eg. Performance,Managment....)</summary>
    ''' 
    '                         sysfam                  l1                  l2                     optSn  
    'dicsysfam As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String,  Dictionary(Of Integer, clsProduct)))))), _
    'dicsysfam As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, clsProduct)))


    'Private Function AddOptions(ByRef l2branch As clsBranch, l2key As String, sysfamkey As String, l1key As String, _
    '                            optionsByCk As Dictionary(Of String, clsProduct), _
    '                            dicopttype As Object, opttranssingular As clsTranslation, opttrans As clsTranslation, _
    '                            ByRef branchWritecache As DataTable, dicoptfam As Object, dicOptTech As Object, ByRef NextBranchID As Integer) As clsBranch

    '    'makes a complete option type branch  


    '    AddOptions = Nothing

    '    '    Dim optTypeBranch As clsBranch
    '    Dim optFamBranch As clsBranch
    '    Dim optTechBranch As clsBranch
    '    Dim optBranch As clsBranch

    '    '    optTypeBranch = New clsBranch(Nothing, optCatBranch, dicopttype(LCase(optTypeKey)).Translation, "", optTrans, optTransSingular, Nothing, branchWriteCache, nextBranchID)

    '    For Each l3keyoptFamKey In optionsByCk(sysfamkey)(l1key)(l2).Keys
    '        'create an option family branch under the option type branch
    '        optFamBranch = New clsBranch(Nothing, optTypeBranch, dicoptfam(optFamKey), "", opttrans, opttranssingular, Nothing, 100, False, "B", branchWritecache, NextBranchID)
    '        For Each optTechKey In dicsysfam(sysfamkey)(optCatKey)(opttypekey)(optFamKey).Keys
    '            'create an option technology branch under the option family branch

    '            optTechBranch = New clsBranch(Nothing, optFamBranch, dicOptTech(optTechKey), "", opttrans, opttranssingular, Nothing, 100, False, "B", branchWritecache, NextBranchID)
    '            'create the Option branches under the technology branch
    '            For Each opt As clsProduct In dicsysfam(sysfamkey)(optCatKey)(opttypekey)(optFamKey)(optTechKey).Values

    '                Dim optName As clsTranslation
    '                If opt.i_Attributes_Code.ContainsKey("~ame") Then
    '                    optName = opt.i_Attributes_Code("~ame")(0).Translation
    '                Else
    '                    optName = opt.i_Attributes_Code("MfrSKU")(0).Translation
    '                End If

    '                '                                               If opt.i_attributes_code("~ame").Translation.ID(English) <= 0 Then Stop
    '                ' Debug.Print(opt.i_attributes_code("MfrSKU").Translation.text(English))

    '                optBranch = New clsBranch(opt, optTechBranch, optName, "", opttrans, opttranssingular, Nothing, 100, False, "B", branchWritecache, NextBranchID)

    '                'Else
    '                ' Beep()
    '                'End If
    '            Next opt
    '        Next optTechKey
    '    Next optFamKey

    'End Function


    Private Function HaveLimits(dicOptLimits As Object, SysSubFamily As String, optcatkey As String, opTtypeKey As String) As Boolean

        '                                                  sysSubFam             optcat                       optiontype       limit
        '        Dim dicOptLimits As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, IQ.clsLimit)))


        HaveLimits = False
        If dicOptLimits.ContainsKey(SysSubFamily) Then
            If dicOptLimits(SysSubFamily).ContainsKey(optcatkey) Then
                If dicOptLimits(SysSubFamily)(optcatkey).ContainsKey(opTtypeKey) Then
                    Return True
                End If
            End If
        End If


    End Function
    Public Function Abbreviation(in$) As String

        If dicAbbreviations.ContainsKey(in$) Then Return dicAbbreviations(in$)

        Return in$

    End Function

    Public Function isFIO(optionSKU As String, systemSKU As String, path$, dicFIOs As Object, dicOptLocalisation As Object, Increments As clsLimit, ByRef quantityWriteCache As DataTable, Optional ActionList As clsActionList = Nothing) As Boolean

        'FIOs are per system - whereas option limits are per family
        'hence FIOS qtyIns override options limits --

        isFIO = False

        'make pre-installed quantity in the context of the specified system branch
        'add a more specific (locally scoped) quantity limits carrying the preinstalled qty's
        'for mem,cpu, pristore etc

        Dim aQuantity As clsQuantity

        Dim optionbranch As clsBranch
        optionbranch = iq.Branches(Split(path, ".").Last)

        'If optionbranch.Product.i_Attributes_Code.ContainsKey("cpuSKU") Then Stop

        ' If systemSKU = "704558-421" And optionSKU = "715218-B21" Then Stop

        'Is this option - factory fitted in the currrent system
        If dicFIOs.ContainsKey(systemSKU) Then
            If dicFIOs(systemSKU).ContainsKey(optionSKU) Then

                Dim fittedQTY As Integer
                fittedQTY = dicFIOs(systemSKU)(optionSKU)
                If fittedQTY = 0 Then Stop

                If fittedQTY = -1 Then
                    'there was a system whos PUSqty (for example) was Null - see import.fios
                    fittedQTY = Increments.Qinstalled 'Increments come from the (vague)  vw291eaoptionLimits - which are per familiy PSUQty, RamQty etc.
                    '    If fittedQTY = 0 Then Stop -  investigate - this does happen ! vw291ea  - ar941aa (for example)

                End If

                isFIO = True

                If dicOptLocalisation.ContainsKey(optionbranch.Product) Then
                    For Each region In dicOptLocalisation(optionbranch.Product)

                        Dim ck$ = optionbranch.ID & "^" & region.id & "^" & path$
                        'aQuantity = New clsQuantity(region, path, optionbranch, Nothing, fittedQTY, limit.MinIncr, limit.PrefIncr, True, quantityWriteCache) 'note - preinstalled options are 'Free of Charge' (last parameter)
                        If i_Quantities.Contains(ck$) Then
                            NoOp()
                        Else
                            If path$ = "" Then Stop 'these should alwyas have a path (i think)
                            If ActionList Is Nothing OrElse ActionList.IsGo(optionSKU, systemSKU, ActionType.INSERT, ObjectType.Quantity, fittedQTY & "," & Increments.MinIncr & "," & Increments.PrefIncr) Then
                                aQuantity = New clsQuantity(region, path, optionbranch, fittedQTY, Increments.MinIncr, Increments.PrefIncr, True, quantityWriteCache) 'note - preinstalled options are 'Free of Charge' (last parameter)
                                i_Quantities.Add(ck$)
                            Else
                                ActionList.Add(optionSKU, systemSKU, ActionType.INSERT, ObjectType.Quantity, fittedQTY & "," & Increments.MinIncr & "," & Increments.PrefIncr)
                            End If
                        End If
                    Next
                Else
                    If fittedQTY > 0 Or Increments.MinIncr > 1 Or Increments.PrefIncr > 1 Then
                        Dim CK$ = optionbranch.ID & "^" & r_worldwide.ID & "^" & path$
                        'aQuantity = New clsQuantity(r_worldwide, path, optionbranch, Nothing, fittedQTY, limit.MinIncr, limit.PrefIncr, True, quantityWriteCache)
                        If i_Quantities.Contains(CK) Then
                            NoOp()
                        Else
                            If path$ = "" Then Stop 'these should alwyas have a path (i think)
                            '   If fittedQTY < 1 Then Stop
                            'If ActionList Is Nothing OrElse ActionList.IsGo(optionSKU, systemSKU, ActionType.INSERT, ObjectType.Quantity, fittedQTY & "," & Increments.MinIncr & "," & Increments.PrefIncr) Then
                            aQuantity = New clsQuantity(r_worldwide, path, optionbranch, fittedQTY, Increments.MinIncr, Increments.PrefIncr, True, quantityWriteCache)
                            i_Quantities.Add(CK$)
                            'Else
                            'ActionList.Add(optionSKU, systemSKU, ActionType.INSERT, ObjectType.Quantity, fittedQTY & "," & Increments.MinIncr & "," & Increments.PrefIncr)
                            'End If
                        End If
                    End If
                End If
            End If
        Else
            NoOp()  'no fios in the system 
        End If

    End Function

    Public Function Compatible(optionbranch As clsBranch, sysSubFamily As String)

        Compatible = True

        If Not optionbranch.Product Is Nothing Then
            If optionbranch.Product.i_Attributes_Code.ContainsKey("incompat") Then
                Dim IncompatibleSubFamilies As String = optionbranch.Product.i_Attributes_Code("incompat")(0).Translation.text(s_lang)
                Dim li As List(Of String)
                li = Split(UCase(IncompatibleSubFamilies), ",").ToList
                If li.Contains(UCase(sysSubFamily)) Then
                    Compatible = False
                End If
            End If
        End If

    End Function

    '''<summary>RESTRICTS *where* a product can be (or is auto)  added - We don't make quantity records for unrestricted parts</summary>
    '''<remarks>If there is no quantity record attached to a branch it is assumed to be available everywhere with a qinstalled of zero, and a minIncr of 1</remarks>
    Public Sub MakeLocalisedQuantity(optionbranch As clsBranch, skuvariant As clsVariant, limit As clsLimit, dicOptLocalisation As Dictionary(Of clsProduct, List(Of clsRegion)), quantityWriteCache As DataTable, path$, Optional ActionList As clsActionList = Nothing)

        Dim aquantity As clsQuantity
        With limit

            If dicOptLocalisation.ContainsKey(optionbranch.Product) Then
                For Each region In dicOptLocalisation(optionbranch.Product)
                    'aquantity = New clsQuantity(region, "", optionbranch, skuvariant, .Qinstalled, .MinIncr, .PrefIncr, False, quantityWriteCache)
                    Dim ck As String = optionbranch.ID & "^" & region.ID & "^"

                    If i_Quantities.Contains(ck) Then
                        NoOp()
                    Else
                        'aquantity = New clsQuantity(region, path$, optionbranch, .Qinstalled, .MinIncr, .PrefIncr, False, quantityWriteCache)
                        If ActionList Is Nothing OrElse ActionList.IsGo(optionbranch.SKU, "", ActionType.INSERT, ObjectType.Quantity, region.Code & "," & "" & "," & optionbranch.ID & "," & .Qinstalled & "," & .MinIncr & "," & .PrefIncr) Then
                            aquantity = New clsQuantity(region, "", optionbranch, .Qinstalled, .MinIncr, .PrefIncr, False, quantityWriteCache)
                            i_Quantities.Add(ck)
                        Else
                            ActionList.Add(optionbranch.SKU, "", ActionType.INSERT, ObjectType.Quantity, region.Code & "," & "" & "," & optionbranch.ID & "," & .Qinstalled & "," & .MinIncr & "," & .PrefIncr)
                        End If
                    End If
                Next
            Else
                If .Qinstalled > 0 Then
                    Dim ck As String = optionbranch.ID & "^" & r_worldwide.ID & "^"
                    If i_Quantities.Contains(ck) Then
                        NoOp()
                    Else
                        'aquantity = New clsQuantity(r_worldwide, path$, optionbranch, .Qinstalled, .MinIncr, .PrefIncr, False, quantityWriteCache)
                        If ActionList Is Nothing OrElse ActionList.IsGo(optionbranch.SKU, "", ActionType.INSERT, ObjectType.Quantity, r_worldwide.Code & "," & "" & "," & optionbranch.ID & "," & .Qinstalled & "," & .MinIncr & "," & .PrefIncr) Then
                            aquantity = New clsQuantity(r_worldwide, "", optionbranch, .Qinstalled, .MinIncr, .PrefIncr, False, quantityWriteCache)
                            i_Quantities.Add(ck)
                        Else
                            ActionList.Add(optionbranch.SKU, "", ActionType.INSERT, ObjectType.Quantity, r_worldwide.Code & "," & "" & "," & optionbranch.ID & "," & .Qinstalled & "," & .MinIncr & "," & .PrefIncr)
                        End If
                    End If
                End If
            End If

        End With

    End Sub


    Public Sub units(con As SqlClient.SqlConnection, dicunits As Dictionary(Of String, clsUnit))

        'returns a dictionary of unit codes

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$
        Dim aunit As clsUnit


        ' NULL
        ' GB
        'GHz
        'in
        'k rpm
        ' MB
        'MHz
        'TB
        'VA

        sql$ = "SELECT DISTINCT optTypeSpeedUnit u from " & server$ & "[iq].products.opttypes UNION SELECT optTypeUnit u from " & server$ & "[iq].products.opttypes"
        rdr = da.DBExecuteReader(con, sql$)
        While rdr.Read
            If Not IsDBNull(rdr.Item("u")) Then
                If Not dicunits.ContainsKey(rdr.Item("u")) Then

                    aunit = New clsUnit(Trim$(rdr.Item("u")), iq.AddTranslation(rdr.Item("u"), English, "units", 0, Nothing, 0, False), "", 0)
                    dicunits.Add(rdr.Item("u"), aunit)

                End If
            End If
        End While
        rdr.Close()

    End Sub


    'Not used - ML
    Public Sub OSs()  'Operating systems

        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader

        Dim count As Integer
        count = 0

        Dim textUnit As clsUnit = iq.i_unit_code("txt")
        Dim prods = iq.Products.Where(Function(p) p.Value.hasSKU()).Select(Function(p) New With {.Sku = p.Value.SKU, .Id = p.Key}).ToList()
        'fetch the (long) descriptions for every System
        rdr = da.DBExecuteReader(con, "select software,ModelSKU from " & server$ & "[iq].[products].[systems] where software is not null")
        Dim desc As clsProductAttribute
        While rdr.Read
            Dim g = prods.Where(Function(a) a.Sku = Trim$(rdr.Item("ModelSKU"))).FirstOrDefault()
            If g IsNot Nothing Then
                Dim cp As clsProduct = iq.Products(g.Id)

                Dim tl As clsTranslation
                Dim s As String = If(rdr.Item("software").ToString().Contains(","), Split(rdr.Item("software"), ",")(0), rdr.Item("software"))
                tl = iq.Translations.Where(Function(d) d.Value.text(English) = s).Select(Function(f) f.Value).FirstOrDefault()
                If tl Is Nothing Then
                    tl = iq.AddTranslation(s, English, "", 0, Nothing, 0, False)
                End If
                If Not cp.i_Attributes_Code.ContainsKey("os") Then desc = New clsProductAttribute(cp, iq.i_attribute_code("os"), 0, textUnit, tl)
                count += 1

            End If
        End While
        rdr.Close()


    End Sub

    'This is done in the incremental import
    Public Function SystemDescriptions(con As SqlClient.SqlConnection, ByRef dicDescs As Dictionary(Of String, clsTranslation), dicsystems As Dictionary(Of String, clsBranch)) As Integer

        Dim rdr As SqlClient.SqlDataReader

        Dim count As Integer
        count = 0


        Dim nextkey As Integer = clsTranslation.NextKey()
        Dim TranslationWriteCache As DataTable


        TranslationWriteCache = da.MakeWriteCacheFor(con, "Translation")

        Dim textUnit As clsUnit = iq.i_unit_code("txt")

        'fetch the (long) descriptions for every System
        rdr = da.DBExecuteReader(con, "select ccdescription,upcnum from " & server$ & "[iq].[products].[HierarchyIQ]")
        Dim system As clsProduct
        Dim desc As clsProductAttribute
        While rdr.Read
            If dicsystems.ContainsKey(Trim$(rdr.Item("upcnum"))) Then
                If Not dicDescs.ContainsKey(rdr.Item("upcnum")) Then
                    system = dicsystems(Trim$(rdr.Item("upcnum"))).Product

                    'note we don't need to add this to the systems attributes collection - the object model does that for us
                    'just making the ProductAttribute (against the product) is enough
                    Dim tl As clsTranslation
                    tl = iq.AddTranslation(rdr.Item("ccdescription"), s_lang, "sysDesc", 0, TranslationWriteCache, nextkey, False)
                    dicDescs.Add(rdr.Item("upcnum"), tl)
                    desc = New clsProductAttribute(system, iq.i_attribute_code("desc"), 0, textUnit, tl)

                End If
                count += 1
            End If
        End While
        rdr.Close()

        da.BulkWrite(con, TranslationWriteCache, "Translation")
        TranslationWriteCache = Nothing


        Return count

    End Function

    Public Function LoadPLCodes(con As SqlClient.SqlConnection) As Dictionary(Of String, String)

        'returns a dictionary of SKU to PLcode

        Dim plc As New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, "SELECT UPCNum,Isnull(PL,'none') as pl from " & server$ & "[iq].products.hierarchyIQ")

        While rdr.Read
            plc.Add(Trim$(rdr.Item("upcnum")), rdr.Item("PL"))
        End While

        rdr.Close()

        Return plc


    End Function


    Public Sub LoadTranslations(con As SqlClient.SqlConnection)

        'Loads up Dans IQ Translations into the public Dictionary 'Xlate' - only used for the purposes of importing

        'which looks like this:-
        '                             hello                 fr      bonjour
        'Dim xlate As New Dictionary(Of String, Dictionary(Of String, String))

        'and is accessed like this:-

        'greeting=xlate("hello")("fr")

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, "SELECT textid,en,de,es,fr,it,tr from " & server$ & "[iq].dbo.language_key")

        Dim languages = Split("de,es,fr,it,tr", ",")

        xlate.Clear()

        Dim Key As String
        While rdr.Read
            Key = rdr.Item("EN")

            If Not xlate.ContainsKey(Key) Then
                xlate.Add(Key, New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase))
                With xlate(Key)
                    For Each language In languages
                        If Not IsDBNull(rdr.Item(language)) Then
                            .Add(language, rdr.Item(language))
                        End If
                    Next
                End With
            End If
        End While

        rdr.Close()

    End Sub

    Public Sub SysTypes(con As SqlClient.SqlConnection, ByRef dicSysTypes As Dictionary(Of String, clsBranch))

        'SysTypes (Desktop,Server,Storage,notebook) are now the first level in the tree.
        'and are added as branches under the root node IQ.Root

        Dim sysType As clsBranch
        Dim rdr As SqlClient.SqlDataReader
        Dim sysTypeEN As String

        rdr = da.DBExecuteReader(con, "SELECT code,translation from " & server$ & "[iq].dbo.Abbreviations WHERE code IN ('DTO','NBK','SVR','SWD','HPN')")

        'Dim Name As clsProductAttribute
        'Dim TextUnit As clsUnit
        'TextUnit = iq.Units("txt")
        'Dim NameAttribute As clsAttribute
        'NameAttribute = iq.Attributes("~ame")

        'tranlation keys for the collective noun 
        Dim collective As clsTranslation = iq.AddTranslation("families", English, "collect", 0, Nothing, 0, False)
        Dim collectiveSingular As clsTranslation = iq.AddTranslation("family", English, "collect", 0, Nothing, 0, False)

        While rdr.Read

            sysTypeEN = rdr.Item("translation")
            If Not dicSysTypes.ContainsKey(rdr.Item("code")) Then
                '                                                                       \/ iQ1

                Dim bn$ = rdr.Item("translation")
                Dim btl As clsTranslation = iq.AddTranslation(bn$, English, "SysTypes", 0, Nothing, 0, False)

                sysType = New clsBranch(Nothing, iq.RootBranch, btl, "/images/iq/prod_range_" & rdr.Item("code") & ".jpg", collective, collectiveSingular, Nothing, 100, False, "S")
                dicSysTypes.Add(rdr.Item("code"), sysType)
            End If

        End While

        rdr.Close()

    End Sub

    Public Function ConvertPCIMinorToMajor(minor As String) As String
        Select Case Left(minor.ToUpper, 4)
            Case "PCIE", "PCIG"
                Dim cw = Mid(minor, 7, minor.IndexOf("B", 7) - 6) 'connector width
                Dim bw = Mid(minor, minor.IndexOf("B", 7) + 2, (minor.IndexOf("_", minor.IndexOf("B", 7) + 1)) - (minor.IndexOf("B", 7) + 1)) 'bus width
                Select Case bw
                    Case "133"
                        Return "PCIX"
                    Case "16"
                        Return "PCIG"
                    Case "8"
                        Return "PCIF"
                    Case "4"
                        Return "PCIE"
                    Case "1"
                        Return "PCIC"
                    Case "0"
                        Return "RISER"
                End Select

            Case "PCI_"
                Dim speed = Mid(minor, 6, minor.IndexOf("B", 6) - 5)
                Dim bw = Mid(minor, minor.IndexOf("B", 6) + 2, (minor.IndexOf("_", minor.IndexOf("B", 6) + 1)) - (minor.IndexOf("B", 6) + 1))
                Select Case speed
                    Case "0"
                        Return "RISER"
                    Case Else
                        Return "PCI"
                End Select
            Case "KOD"
                Return "KOD"
            Case Else
                If Mid(minor, 5, 1) = "_" Then Return Left(minor, 4)
                Return minor
        End Select
        Return minor
    End Function


    Public Sub AddPCIChassisSlots(minorfamily As String, majorfamily As String, systemBranch As clsBranch, tlwc As DataTable, ByRef nextkey As Integer, swc As DataTable)

        Dim con = da.OpenDatabase()
        'for every system in the family of each slot type - set the slots (1127 rows)
        Dim Sql$ = "SELECT familyname,slotnum,pciCode,dedicated,dedisku,notes FROM h3.[iq].products.sysfamilyPCIslots "
        Sql$ &= " WHERE FamilyName='" & minorfamily & "' "
        Sql$ &= "ORDER BY familyname,pcicode,slotnum "

        Dim rdr = da.DBExecuteReader(con, Sql$)

        If rdr.RecordsAffected = 0 Then
            Sql$ = "SELECT familyname,slotnum,pciCode,dedicated,dedisku,notes FROM h3.[iq].products.sysfamilyPCIslots "
            Sql$ &= " WHERE FamilyName='" & majorfamily & "' "
            Sql$ &= "ORDER BY familyname,pcicode,slotnum "

            rdr = da.DBExecuteReader(con, Sql$)
        End If

        Dim aslot As clsSlot
        Dim slots As Integer = 0
        Dim found As Boolean = False
        Dim notFound As Integer = 0
        Dim notes As IQ.clsTranslation
        Dim aNull As DBNull = DBNull.Value
        Dim slotNum As IQ.NullableInt
        Dim dudFams As List(Of String) = New List(Of String)
        Dim majorcode As String
        Dim minorcode As String


        Dim sw As New StreamWriter("c:\temp\badPCISlots.txt", True)

        While rdr.Read  'For every PCI slot definition
            found = False

            'This column is actually familyCODE - the longer version - although there is some left over FamilyName (EG.DL360) data
            Dim rfam$ = rdr.Item("familyname")
            'go through every system - and if the familyCODE matches - make slots on that system

            If rdr.Item("pcicode") = "" Then
                minorcode = "KOD"  'Knocked out slots (some pci slots are 'knocked out' by risres cards, chassis kits etc.
            Else
                minorcode = fixPci(rdr.Item("pcicode")) 'make sure it has 4 parts (inlcluding a GEN which may well be blank
                minorcode &= "_" & Math.Abs(CInt(rdr.Item("dedicated")))
            End If

            If IsDBNull(rdr.Item("notes")) Then
                notes = Nothing
            Else
                notes = iq.AddTranslation(rdr.Item("notes"), s_lang, "SlotNotex", 0, tlwc, nextkey, False)
            End If

            If IsDBNull(rdr.Item("slotnum")) Then
                slotNum = New IQ.NullableInt(DBNull.Value)
            Else
                'This is a little messy as the IQ1 slotnum is a byte (usually you could pass the value straight from the reader
                If IsDBNull(rdr.Item("slotNum")) Then
                    slotNum = New IQ.NullableInt()
                Else
                    slotNum = New NullableInt(CInt(rdr.Item("slotnum")))
                End If
            End If

            majorcode = ConvertPCIMinorToMajor(minorcode)

            If majorcode = minorcode Then
                sw.WriteLine("couldn't convert" & minorcode & " for " & rfam$)

            Else

                If Not (iq.i_slotType_Code.ContainsKey(majorcode) AndAlso iq.i_slotType_Code(majorcode).ContainsKey(minorcode)) Then
                    'need to create the missing slot type 
                    'Dim slotMajor As clsSlotType = New clsSlotType(majorcode, minorcode)
                    sw.WriteLine("iq.i_slottype(" & majorcode & ") does not contain " & minorcode)
                Else

                    Dim st As clsSlotType = iq.i_slotType_Code(majorcode)(minorcode)
                    Dim tmpslot As clsSlot = New clsSlot(st, Nothing, "", 1, notes, slotNum, 0, 0)
                    If Not systemBranch.i_Slots.ContainsKey(tmpslot.compoundKey) Then

                        'Make the missing slot (which addes it to the branch index etc,etc)
                        aslot = New clsSlot(st, systemBranch, "", 1, notes, slotNum, 0, 0, swc)

                        'ck = aslot.compoundKey  
                        'b.Slots.Add(ck, aslot) 'NOOOO you don't need to do this - it's automatically added to the branch
                        slots += 1

                        '  ImportLog.Add(DateTime.Now, String.Format("Creating new Slot " & tmpslot.compoundKey))

                    Else
                        '  ImportLog.Add(DateTime.Now, String.Format("Found " & tmpslot.compoundKey & ". Not importing."))
                        'aslot = New clsSlot(dicSlotTypes("UNAVAIL"), systemBranch, "", 1, notes, slotNum, 0, 0)

                        'Logit(systemBranch.SKU & " in " & rfam & " has an invalid PCI slot type '" & rdr.Item("pcicode") & "'")
                        '    Stop
                    End If
                End If
            End If

        End While

        sw.Close()

        rdr.Close()
        con.Close()

    End Sub


    Public Function slotTypes(con As SqlClient.SqlConnection, dicSystems As Dictionary(Of String, clsBranch), Optional rOnly As Boolean = False) As Dictionary(Of String, clsSlotType)

        'SlotType minor code (E.G.NHPSFF3.5DD > clsslottype (containing majorcode, minorcode, ID, fallback info)

        Dim sql$

        Dim dicSlotTypes As Dictionary(Of String, clsSlotType)  'this is the return value of the function
        dicSlotTypes = New Dictionary(Of String, clsSlotType)(StringComparer.CurrentCultureIgnoreCase) 'SlotType minor code (E.G.NHPSFF3.5DD > clsslottype (containing majorcode, minorcode, ID, fallback info)

        sql$ = "Select distinct PCICode,dedicated from " & server$ & "[iq].products.SysFamilyPCIslots"

        Dim rdr As SqlClient.SqlDataReader
        Dim aSlotType As clsSlotType
        rdr = da.DBExecuteReader(con, sql$)

        Dim minorCode As String
        Dim majorCode As String
        Dim slotdesc As pciStruct

        While rdr.Read
            If rdr.Item("pciCode") <> "" Then 'was = ???
                minorCode = fixPci(rdr.Item("pcicode")) 'make sure it has 4 parts (inlcluding a GEN which may well be blank
                minorCode &= "_" & Math.Abs(CInt(rdr.Item("dedicated")))
                majorCode = ConvertPCIMinorToMajor(minorCode)
                slotdesc = ExpandPCI(minorCode)
                If iq.i_slotType_Code.ContainsKey(majorCode) AndAlso iq.i_slotType_Code(majorCode).ContainsKey(minorCode) Then
                    aSlotType = iq.i_slotType_Code(majorCode)(minorCode)
                Else
                    If rOnly Then
                        If iq.i_slotType_Code.ContainsKey(majorCode) AndAlso iq.i_slotType_Code(majorCode).ContainsKey(minorCode) Then
                            aSlotType = iq.i_slotType_Code(majorCode)(minorCode)
                        End If

                    Else
                        aSlotType = New clsSlotType(majorCode, minorCode, iq.AddTranslation(slotdesc.fullText, English, "PCIST", 0, Nothing, 0, True))
                    End If

                End If
                dicSlotTypes.Add(majorCode & "^" & minorCode, aSlotType)  'store a lookup by original code

            End If
        End While

        'Knocked out (KO'd) slot's (obscured PCI slots)
        If Not dicSlotTypes.ContainsKey("PCI^KOD") Then
            Dim kod As clsSlotType
            If rOnly Then
                If iq.i_slotType_Code.ContainsKey("PCI") AndAlso iq.i_slotType_Code("PCI").ContainsKey("KOD") Then
                    kod = iq.i_slotType_Code("PCI")("KOD")
                Else
                    kod = New clsSlotType("PCI", "KOD", iq.AddTranslation("Physically Obstructed", English, "PCIST", 0, Nothing, 0, True))
                End If


            End If
            dicSlotTypes.Add("PCI^KOD", kod)
        End If

        rdr.Close()

        Dim ospec As pciStruct
        Dim ispec As pciStruct
        If Not rOnly Then

            'Find All the fallback slot types

            'probably need to consider dedicated slots more carefully
            For Each ko In iq.SlotTypes.Keys
                If UBound(Split(iq.SlotTypes(ko).MinorCode, "_")) = 4 Then 'nneceesary (had to hack it in during an import)
                    ospec = ExpandPCI(iq.SlotTypes(ko).MinorCode)
                    For Each ki In iq.SlotTypes.Keys
                        If UBound(Split(iq.SlotTypes(ki).MinorCode, "_")) = 4 Then 'nneceesary (had to hack it in during an import)
                            If ki > ko Then
                                ispec = ExpandPCI(iq.SlotTypes(ki).MinorCode)
                                If ispec.tech = ospec.tech Then 'same 'technology' (PCIe/x/BLcm
                                    If ispec.connector > ospec.connector Then 'slotform 1,8,16 - only add higher connector width slots
                                        If ispec.speed > ospec.speed Then 'Speed 4x 8x 16x - only use higher speed slots
                                            If ispec.w >= ospec.w And ispec.h >= ospec.h Then 'only an alternative if same or wider AND same or higher
                                                With iq.SlotTypes(ko)
                                                    .Fallback.Add(.Fallback.Count, iq.SlotTypes(ki))
                                                End With
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Next



            'for every system in the family of each slot type - set the slots (1127 rows)
            sql$ = "SELECT familyname,slotnum,pciCode,dedicated,dedisku,notes FROM " & server$
            sql$ &= "[iq].products.sysfamilyPCIslots "
            sql$ &= "WHERE (FAMILYname LIKE '%" & restrictImportToFamily & "%' or familyname = '' or familyname is null)"
            sql$ &= "ORDEr BY familyname,pcicode,slotnum "

            rdr = da.DBExecuteReader(con, sql$)

            Dim aslot As clsSlot
            Dim slots As Integer = 0
            Dim found As Boolean = False
            Dim Fam As String
            Dim FamMaj As String
            Dim notFound As Integer = 0
            Dim notes As IQ.clsTranslation
            Dim aNull As DBNull = DBNull.Value
            Dim slotNum As IQ.NullableInt
            Dim dudFams As List(Of String) = New List(Of String)

            While rdr.Read  'For every PCI slot definition
                found = False

                'This column is actually familyCODE - the longer version - although there is some left over FamilyName (EG.DL360) data
                Dim rfam$ = rdr.Item("familyname")
                'go through every system - and if the familyCODE matches - make slots on that system
                For Each systemBranch In dicSystems.Values
                    Fam = systemBranch.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English)
                    FamMaj = systemBranch.Product.i_Attributes_Code("famMajor")(0).Translation.text(English)

                    If LCase(Trim$(Fam)) = LCase(Trim$(rfam)) Or LCase(Trim$(FamMaj)) = LCase(Trim$(rfam)) Then
                        found = True

                        If rdr.Item("pcicode") = "" Then
                            minorCode = "KOD"  'Knocked out slots (some pci slots are 'knocked out' by risres cards, chassis kits etc.
                        Else
                            minorCode = fixPci(rdr.Item("pcicode")) 'make sure it has 4 parts (inlcluding a GEN which may well be blank
                            minorCode &= "_" & Math.Abs(CInt(rdr.Item("dedicated")))
                        End If

                        If IsDBNull(rdr.Item("notes")) Then
                            notes = Nothing
                        Else
                            notes = iq.AddTranslation(rdr.Item("notes"), s_lang, "SlotNotex", 0, Nothing, 0, False)
                        End If

                        If IsDBNull(rdr.Item("slotnum")) Then
                            slotNum = New IQ.NullableInt(DBNull.Value)
                        Else
                            'This is a little messy as the IQ1 slotnum is a byte (usually you could pass the value straight from the reader
                            If IsDBNull(rdr.Item("slotNum")) Then
                                slotNum = New IQ.NullableInt()
                            Else
                                slotNum = New NullableInt(CInt(rdr.Item("slotnum")))
                            End If
                        End If

                        majorCode = ConvertPCIMinorToMajor(minorCode)
                        If dicSlotTypes.ContainsKey(majorCode & "^" & minorCode) Then

                            'these are the GIVES slots (on every system) - for PCIslots only

                            aslot = New clsSlot(dicSlotTypes(majorCode & "^" & minorCode), systemBranch, "", 1, notes, slotNum, 0, 0)

                            'ck = aslot.compoundKey  
                            'b.Slots.Add(ck, aslot) 'NOOOO you don't need to do this - it's automatically added to the branch
                            slots += 1

                        Else
                            'aslot = New clsSlot(dicSlotTypes("UNAVAIL"), systemBranch, "", 1, notes, slotNum, 0, 0)

                            Logit(systemBranch.SKU & " in " & rfam & " has an invalid PCI slot type '" & rdr.Item("pcicode") & "'")
                            '    Stop
                        End If
                    End If
                Next

                If Not found Then
                    If Not dudFams.Contains(rfam) Then
                        Logit("Could not locate a system within the subFamily #" & rfam & "#")
                        dudFams.Add(rfam)
                    End If
                    notFound += 1
                End If
            End While

            rdr.Close()
        End If
        Logit("making slot types", False, True)
        'Read the distinct product.options.optfamily codes to make the slot types for memory, drive bays etc.

        'OptType is not granular enough - it doesnt distinguish between the drive types - so inappropriate drive type are not pruned off the 
        'optfamily is too grainy - meaning you can't search by the number of drive bays or processor slots - because they're not the same slot type

        sql$ = "SELECT DISTINCT optfamily,opttype  from " & server$ & "[iq].products.options order by opttype"  'was optfamily (is more granular)
        rdr = da.DBExecuteReader(con, sql$)

        Dim major As String = ""
        Dim minor As String = ""

        'build a dictionary of minor > slottype - where 
        Dim mapTo As String = ""

        While rdr.Read
            major = rdr.Item("OptType") 'HDD,PSU,WAR,CPU
            minor = rdr.Item("optFamily") 'NHP35LFFSC   'was optfamily - opttype is the broader types HDD,PSU,WTY etc  - OptType is no good to us
            Dim txt$

            '@@@@@ IMPORTANT
            If major = "CHK" And InStr(UCase(minor), "_SC") Then
                major = "HDD" ' map all the chassis kit smart carriers to hard drive (major) slot types
            End If

            ' If major = "HDD" Or major = "OPT" Then  'consolidate all non-drive type slots (CPUs, Carepacks etc)
            ' mapTo = minor
            ' Else
            ' mapTo = major
            ' End If

            If dicAbbreviations.ContainsKey(minor) Then txt$ = dicAbbreviations(minor) Else txt$ = minor

            'need to map - 
            If dicSlotTypes.ContainsKey(major & "^" & minor) Then
                ' If Not dicSlotTypes.ContainsKey(minor) Then
                dicSlotTypes.Add(major & "^" & minor, dicSlotTypes(mapTo))
                'End If
            Else
                'If Not dicSlotTypes.ContainsKey(minor) Then
                Dim st
                If rOnly Then
                    If iq.i_slotType_Code.ContainsKey(major) AndAlso iq.i_slotType_Code(major).ContainsKey(minor) Then st = iq.i_slotType_Code(major)(minor)
                Else
                    st = New clsSlotType(major, minor, iq.AddTranslation(txt, English, "ST", 0, Nothing, 0, False))
                End If

                dicSlotTypes.Add(major & "^" & minor, st)
                'End If
            End If
        End While
        rdr.Close()

        'familyMan


        Logit("finished slot types", False, True)

        Return dicSlotTypes

    End Function

    Public Function FixPCI(ByVal code As String) As String

        'some PCI codes are missing their generation - add the required extra underscore
        If code = "" Then
            code = "___"
        Else
            If Split(code, "_").Length < 4 Then code &= "_"
        End If

        FixPCI = code


    End Function
    Public Function ExpandPCI(code As String) As pciStruct

        'Takes an IQuote1 style code for a PCI slot
        'Returns a popuplauteds pci Structure - will all the info including a .fullText description

        Dim bits() As String = Split(code, "_")

        With ExpandPCI
            .tech = bits(0)
            .connector = Val(Split(Mid$(bits(1), 2), "B")(0))

            .speed = Val(Split(Mid$(bits(1), 2), "B")(1))
            .generation = bits(3) 'Don't mention the generation (I did once but I think I got away with it)
            .dedicated = CBool(bits(4))

            If InStr(bits(2), "FX") Then .w = 2 Else .w = 1
            If InStr(bits(2), "FY") Then .h = 2 Else .h = 1
            .fullText = .tech & " " & .speed & "x speed " & .connector & "x socket."
            If .w = 2 Then .fullText &= " Full length," Else .fullText &= " Half length,"
            If .h = 2 Then .fullText &= " Full height." Else .fullText &= " Half height."
            If .dedicated Then .fullText &= " Dedicated."
        End With

        'returns the populated structure

    End Function

    Public Function FormFactors(con As SqlClient.SqlConnection) As Dictionary(Of String, clsTranslation)

        FormFactors = New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)

        Dim sql$
        sql$ = "SELECT DISTINCT instformfactor, a.Translation,a.code from " & server$ & "[iq].products.UNION_SysFamilyDefinitions sfd left join " & server$ & "[iq].dbo.Abbreviations a ON sfd.InstFormFactor=a.code order by instformfactor"

        '   sql = "select distinct sf.InstFormFactor as ff from IQ.products.Systems s join IQ.products.SysFamilyDefinitions sf on s.FamilyCode = sf.SysFamily"

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, sql$)
        Dim o As Integer = 0

        Dim tl As clsTranslation

        While rdr.Read
            If Not IsDBNull(rdr.Item("instformfactor")) Then
                o += 10
                If IsDBNull(rdr.Item("translation")) Then
                    tl = iq.AddTranslation(rdr.Item("instformfactor"), English, "FF", o, Nothing, 0, True) 'iq1 translations don't exsit for some of the codes (such as 'blade')
                Else
                    tl = iq.AddTranslation(rdr.Item("translation"), English, "FF", o, Nothing, 0, True)  'this creates a translation for each possible form factor and groups them under the group code FF
                End If
                FormFactors.Add(rdr.Item("instformfactor"), tl)
            End If
        End While

        rdr.Close()

    End Function

    Public Function OptAbbreviations(Con As SqlClient.SqlConnection, columns As String) As Dictionary(Of String, clsTranslation)

        'Imports dans abbreviations - creating groups (of translations) for some of the abbreviations which weren't previously grouped


        Dim nextKey As Integer = clsTranslation.NextKey
        Dim Tlwc As New DataTable 'transaltion Wcrite cache
        Tlwc = da.MakeWriteCacheFor(Con, "Translation")
        nextKey = 0
        Tlwc = Nothing

        OptAbbreviations = New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)

        'these are all the columns that contain abbreviations (they may contain a CD list, they may alos contain part numbers)
        Dim sql$
        Dim rdr As SqlClient.SqlDataReader

        Dim aa As clsAttribute
        For Each k In Split(columns, ",")

            'make an attribute for each column - ProductAttributes will be made later to carry the actual data
            If Not iq.i_attribute_code.ContainsKey(k) Then
                aa = New clsAttribute(k, iq.AddTranslation(k, English, "attrib", 0, Tlwc, nextKey, False), 0)
            End If

            sql$ = "SELECT distinct " & k & " from " & server$ & "[iq].products.union_systems"  'each row may be a cd list - so the DISTNCT reduces things - but we still need to check we havent already processed each one
            rdr = da.DBExecuteReader(Con, sql$)

            Dim uniqueCodes As New List(Of String)
            While rdr.Read
                If Not IsDBNull(rdr.Item(k)) Then
                    For Each c In Split(rdr.Item(k), ",")   'split any/all comma seperated value in each column
                        If Not uniqueCodes.Contains(UCase(c)) Then
                            uniqueCodes.Add(UCase(c))
                            'uniqueCodes.Add(rdr.Item(k))
                        End If
                    Next
                End If
            End While
            'we now have a list of all the unique abbreviation codes  in this column 'k'  (with a few part numbers jumbled in perhaps)
            rdr.Close()

            sql$ = "SELECT CODE,TRANSLATION from " & server$ & "[iq].dbo.abbreviations where code IN ('" & Join(uniqueCodes.ToArray, "','") & "');"
            rdr = da.DBExecuteReader(Con, sql$)
            While rdr.Read
                If Not OptAbbreviations.ContainsKey(UCase(rdr.Item("code"))) Then
                    OptAbbreviations.Add(UCase(rdr.Item("code")), iq.AddTranslation(rdr.Item("translation"), English, "AT_" & k, 0, Tlwc, nextKey, False))
                    uniqueCodes.Remove(UCase(rdr.Item("code")))
                End If
            End While

            '            For Each H In uniqueCodes
            ' Debug.Print(H)
            ' Next

            rdr.Close()


            'anything now left in unqiueCodes wasn't in the abbreviations table - so is either a part number of some of Pauls random junk - EG., French keyboard kit

            For Each leftover In uniqueCodes
                If Not OptAbbreviations.ContainsKey(leftover) Then
                    OptAbbreviations.Add(UCase(leftover), iq.AddTranslation(leftover, English, "LO_" & k, 0, Tlwc, nextKey, False)) ' don't add part numbers here - (in the options and extra columns they shoudl become FOC preinstalled parts)
                End If
            Next

        Next

        '   da.BulkWrite(Con, Tlwc, "Translation")
        Tlwc = Nothing
    End Function

    Public Sub fixFamMinor()

        Dim sql$
        Dim sever$ = "h3"

        sql$ = "SELECT familyPriStor,familySecStor,busunit,modelSKU,sysfamilyname,familycode,cpu,sfd.systype,h.ccDescription as [desc],Isnull([SupplyChainCode],'A') as [supplyChainCode],sfd.u, Activesites,sfd.instformfactor, "
        ';Sql$ &= columns  'THIS FORMS THE BULK OF THE SPEC TABLE
        sql$ &= "WLAN,WWAN,alsoHost"
        sql$ &= ",productNote,vga,energystar,weightUnboxed,activeFromDate,activeToDate,active,eol,sfd.sysfamilyimg,aaOnly from " & server$ & "[iq].products.union_systems sys "
        sql$ &= "INNER join " & server$ & "[iq].products.union_sysfamilydefinitions sfd ON sfd.SysFamily=sys.FamilyCode "
        sql$ &= "INNER join " & server$ & "[iq].products.hierarchyiq h ON modelSKU=h.UPCNum "
        sql$ &= "WHERE (sYSFAMILY LIKE '%" & restrictImportToFamily & "%' or sysfamily = '' or sysfamily is null)"

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, sql$)

        While rdr.Read
            Dim sku$ = rdr.Item("modelsku")
            If iq.i_SKU.ContainsKey(sku) Then
                Dim system As clsProduct
                system = iq.i_SKU(sku)
                If system.i_Attributes_Code.ContainsKey("famMinor") Then
                    Dim fm As clsProductAttribute = system.i_Attributes_Code("famMinor")(0)

                    Dim fmc As String = fm.Translation.text(English)
                    If fmc = "" Then Stop


                Else
                    Stop
                End If
            End If

        End While

        rdr.Close()
        con.Close()


    End Sub


    Public Sub Systems(con As SqlClient.SqlConnection, dicsystems As Dictionary(Of String, clsBranch), dicfamily As Dictionary(Of String, clsBranch), dicPlcode As Dictionary(Of String, String), containment As Dictionary(Of String, List(Of String)), ByRef errormessages As List(Of String)) 'As Dictionary(Of String, clsBranch)

        Dim U As clsProductAttribute
        Dim aa As clsAttribute
        If Not iq.i_attribute_code.ContainsKey("U") Then
            aa = New clsAttribute("U", iq.AddTranslation("U", English, "U", 0, Nothing, 0, False), 0)
        End If

        Dim pristorAtt
        If Not iq.i_attribute_code.ContainsKey("PriStor") Then


            pristorAtt = New clsAttribute("PriStor", iq.AddTranslation("Primary Storage (import only)", English, "UI", 0, Nothing, 0, False), 0)
        Else
            pristorAtt = iq.i_attribute_code("PriStor")
        End If

        Dim sctl = iq.AddTranslation("Supply Chain", English, "cats", 0, Nothing, 0, False)

        'returns a dictionary of system branches by ModelSKU

        Dim Product As clsProduct
        Dim sysBranch As clsBranch 'used to create systems (which go into the dictionaries)

        Dim AttribWriteCache As New DataTable
        AttribWriteCache = da.MakeWriteCacheFor(con, "ProductAttribute")

        Dim QtyWritecache As DataTable = da.MakeWriteCacheFor(con, "Quantity")


        'Dim dicFormFactors As Dictionary(Of String, clsTranslation) = FormFactors(con)

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$

        'small dictionary of supply chains to their translations keys - used to look up 
        'the supply chain branches (under the family branches) 

        'supply chains are obsoleted (before they ever really saw the light of day!)
        Dim dicChains As New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)
        dicChains = New Dictionary(Of String, clsTranslation)

        'hard coded - until someone can tell me where to find the full supply chain names/list
        dicChains.Add("A", iq.AddTranslation("Regular models", English, "SC", 10, Nothing, 0, False))
        dicChains.Add("TV", iq.AddTranslation("Top value", English, "SC", 20, Nothing, 0, False))
        dicChains.Add("SB", iq.AddTranslation("Smart buy", English, "SC", 30, Nothing, 0, False))
        dicChains.Add("R", iq.AddTranslation("HP Renew", English, "SC", 30, Nothing, 0, False))
        dicChains.Add("PR", iq.AddTranslation("Promotional", English, "SC", 30, Nothing, 0, False))
        dicChains.Add("GO", iq.AddTranslation("Golden Offers", English, "SC", 30, Nothing, 0, False))


        'the focus attributes are matched against the code (but theyr'e attributes - so they need trasnlations (until and unless we invent a text type for attributes!)
        Dim dicSC As New Dictionary(Of String, clsTranslation)(StringComparer.CurrentCultureIgnoreCase)
        dicSC = New Dictionary(Of String, clsTranslation)

        dicSC.Add("A", iq.AddTranslation("A", English, "SCC", 10, Nothing, 0, False))
        dicSC.Add("TV", iq.AddTranslation("TV", English, "SCC", 20, Nothing, 0, False))
        dicSC.Add("SB", iq.AddTranslation("SB", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("R", iq.AddTranslation("R", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("PR", iq.AddTranslation("PR", English, "SCC", 30, Nothing, 0, False))
        dicSC.Add("GO", iq.AddTranslation("GO", English, "SCC", 30, Nothing, 0, False))


        Dim sysTypeToPortfolio As Dictionary(Of String, clsTranslation) = New Dictionary(Of String, clsTranslation)

        'FYI
        'HP's Corporate hierarchy goes
        'Division (ESSN..
        '  BU (business unit) ISS/PSG/HPN/SWD
        '     Exhibit  'Desktops/Notebooks

        sysTypeToPortfolio.Add("DTO", iq.AddTranslation("PSG", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("HPN", iq.AddTranslation("HPN", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("IPG", iq.AddTranslation("IPG", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("NBK", iq.AddTranslation("PSG", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("SVR", iq.AddTranslation("ISS", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("SWD", iq.AddTranslation("SWD", English, "BU", 1, Nothing, 0, False))
        sysTypeToPortfolio.Add("PSG", iq.AddTranslation("PPS", English, "BU", 1, Nothing, 0, False))

        'Create a dictionary of all the abbreviations referenced in any of these columns (of products.union_systems)
        'these are NOT the columns which contain only part numbers (RAM,discretegraphics, etc - handled in import.fios()
        'theyre the ones that may have abbreviations in

        Dim columns As String = "extras,options,software,warrantyCode,formFactor,mfrBuildCode,display,intVideo,ILOhardware,terStorTech,raidTech"

        'extras and options contain moslty abbreviations - but some part no's
        'software contains a CD list of abbreviations

        Dim optabbreviations As Dictionary(Of String, clsTranslation)
        optabbreviations = Import.OptAbbreviations(con, columns)

        If Not optabbreviations.ContainsKey("TOWER") Then
            optabbreviations.Add("TOWER", iq.AddTranslation("Tower", English, "FF", 100, Nothing, 0, False))
        End If

        If Not optabbreviations.ContainsKey("BLADE") Then
            optabbreviations.Add("BLADE", iq.AddTranslation("Blade", English, "FF", 90, Nothing, 0, False))
        End If

        columns = Replace(columns, "ILOhardware", "Sys.ILOhardware") 'the column nane is ambiguous otherise (this isn't pretty - but it's only an import)

        makeSpecAttributes("vga^has onboard VGA,eStar^Energy Star Compliant,mass^Weight unboxed,note^Product Note,WLAN^Wireless LAN,WWAN^3G/CellularConnectivity,displaySize^Display Size (diagonal)")

        Dim nextkey As Integer = clsTranslation.NextKey()
        Dim Tlwc As New DataTable
        Tlwc = da.MakeWriteCacheFor(con, "Translation")

        sql$ = "SELECT familyPriStor,familySecStor,busunit,modelSKU,sysfamilyname,familycode,cpu,sfd.systype,h.ccDescription as [desc],Isnull([SupplyChainCode],'A') as [supplyChainCode],sfd.u, Activesites,sfd.instformfactor, "
        sql$ &= columns  'THIS FORMS THE BULK OF THE SPEC TABLE
        sql$ &= ",WLAN,WWAN,alsoHost"
        sql$ &= ",productNote,vga,energystar,weightUnboxed,activeFromDate,activeToDate,active,eol,sfd.sysfamilyimg,aaOnly from " & server$ & "[iq].products.union_systems sys "
        sql$ &= "INNER join " & server$ & "[iq].products.union_sysfamilydefinitions sfd ON sfd.SysFamily=sys.FamilyCode "
        sql$ &= "INNER join " & server$ & "[iq].products.hierarchyiq h ON modelSKU=h.UPCNum "
        sql$ &= "WHERE (sYSFAMILY LIKE '%" & restrictImportToFamily & "%' or sysfamily = '' or sysfamily is null)"

        columns = Replace(columns, "Sys.ILOhardware", "ILOhardware") 'put it back so we can pull out this column later
        rdr = da.DBExecuteReader(con, sql$)

        Dim SystemName As clsProductAttribute
        Dim FamMajor As clsProductAttribute
        Dim FamMinor As clsProductAttribute
        Dim FamDisp As clsProductAttribute

        Dim cpuSKU As clsProductAttribute
        Dim mfrSKU As clsProductAttribute
        Dim PLcode As clsProductAttribute

        Dim sector As clsSector
        Dim sysTrans As clsTranslation = iq.AddTranslation("systems", English, "collect", 0, Tlwc, nextkey, False)
        Dim sysTransSingular As clsTranslation = iq.AddTranslation("system", English, "collect", 0, Tlwc, nextkey, False)

        Dim optTrans As clsTranslation = iq.AddTranslation("options", English, "collect", 0, Tlwc, nextkey, False)
        Dim optTransSingular As clsTranslation = iq.AddTranslation("option", English, "collect", 0, Tlwc, nextkey, False)
        Dim textUnit As clsUnit = iq.i_unit_code("txt")

        Dim att As clsAttribute = Nothing

        Dim tlyes As clsTranslation = iq.AddTranslation("Yes", English, "hasFeature", 0, Tlwc, nextkey, False)

        While rdr.Read
            If LCase(Left$(rdr.Item("ModelSKU"), 1)) <> "x" Then ' do not import systems begging with X they are 'fake'
                If Not dicsystems.ContainsKey(Trim$(rdr.Item("ModelSku"))) Then

                    sector = iq.i_sector_code("HP" & rdr.Item("busunit"))

                    Dim activeTo As Date = CDate("31/12/2100")
                    If Not IsDBNull(rdr.Item("activeToDate")) Then activeTo = rdr.Item("activetodate")

                    Dim publish As Boolean = True
                    If rdr.Item("AAonly") <> 0 Then
                        publish = False
                    End If

                    Product = New clsProduct(rdr.Item("modelsku"), True, False, sector, iq.i_ProductType_Code(rdr.Item("systype")), rdr.Item("activefromdate"), activeTo, rdr.Item("active"), rdr.Item("eol"), publish, "", "", "") 'this IS a system 

                    'Make a focus attibute based on the system type (lightly translated to the portfolio)
                    'ISS PSG SWD
                    Dim FA As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("FOCUS"), 0, textUnit, sysTypeToPortfolio(rdr.Item("systype")))

                    Dim scc As String = rdr.Item("supplyChainCode")
                    If dicChains.ContainsKey(scc) Then
                        Dim sc As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("SC"), 0, iq.i_unit_code("txt"), dicChains(scc), AttribWriteCache)
                        Dim SCFA As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("FOCUS"), 0, textUnit, dicSC(scc))
                    Else
                        Beep()
                    End If


                    If Not IsDBNull(rdr.Item("U")) Then
                        'U = New clsProductAttribute(Product, iq.i_attribute_code("U"), rdr.Item("U"), iq.i_unit_code("U"),  iq.AddTranslation(rdr.Item("U") & " U", English, "U"), AttribWriteCache)
                        U = New clsProductAttribute(Product, iq.i_attribute_code("U"), rdr.Item("U"), iq.i_unit_code("U"), Nothing, AttribWriteCache)
                    End If

                    If Not IsDBNull(rdr.Item("productNote")) Then
                        If Trim$(rdr.Item("productnote")) <> "" Then
                            Dim note As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("note"), 0, textUnit, iq.AddTranslation(rdr.Item("productNote"), English, "ProdNote", 0, Tlwc, nextkey, True), AttribWriteCache)
                        End If
                    End If

                    If Not IsDBNull(rdr.Item("EnergyStar")) Then
                        If CType(rdr.Item("energystar"), Integer) > 0 Then
                            Dim es As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("eStar"), 1, textUnit, tlyes, AttribWriteCache)
                        End If
                    End If

                    If Not IsDBNull(rdr.Item("WLAN")) Then
                        Dim wl As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("WLAN"), 1, textUnit, tlyes, AttribWriteCache)
                    End If

                    If Not IsDBNull(rdr.Item("WWAN")) Then
                        Dim ww As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("WWAN"), 1, textUnit, tlyes, AttribWriteCache)
                    End If


                    If Not IsDBNull(rdr.Item("vga")) Then
                        Dim note As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("vga"), 1, textUnit, tlyes, AttribWriteCache)
                    End If


                    'will need to do the same for the secondary storage/optical drives
                    If Not IsDBNull(rdr.Item("FamilyPriStor")) Then
                        'optfamily translation  -- This is a code like NHP355SFF
                        Dim oftl As clsTranslation = iq.AddTranslation(rdr.Item("familypristor"), English, "", 0, Tlwc, nextkey, False)
                        Dim pristor As clsProductAttribute = New clsProductAttribute(Product, pristorAtt, 0, textUnit, oftl, AttribWriteCache)
                    End If


                    'same as formfactor

                    'If Not IsDBNull(rdr.Item("instFormFactor")) Then
                    ' Dim FormF As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("formFactor"), 1, textUnit, iq.AddTranslation(rdr.Item("instFormFactor"), English, "FF"), AttribWriteCache)
                    ' End If


                    If Not IsDBNull(rdr.Item("weightUnboxed")) Then
                        '21kg&nbsp;&nbsp;(46.30lb)
                        'take the --- text out and use the conversions

                        Dim wu$ = rdr.Item("weightUnboxed")
                        Dim p$() = Split(wu$, "kg")
                        If UBound(p$) <> 1 Then Stop
                        Dim kg As Single = Val(p$(0))
                        'Dim tl As clsTranslation = iq.AddTranslation(wu$, English, "WU", 0, Nothing, True)
                        '     Dim mass As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("mass"), kg, textUnit, tl, AttribWriteCache)
                        Dim mass As clsProductAttribute = New clsProductAttribute(Product, iq.i_attribute_code("mass"), kg, iq.i_unit_code("kg"), Nothing, AttribWriteCache)

                    End If

                    'MAKE THE MAJOR SPEC TABLE ATTRIBUTES - preinstalled options are in import.FIOs()
                    'Make an attribute for every abbreviation referenced in the various COLUMNS of products.union_systems
                    Dim pa As clsProductAttribute
                    Dim abtl As clsTranslation          'abbreviation translation
                    For Each k In Split(columns, ",")

                        ' If k = "formFactor" Then Stop
                        If Not IsDBNull(rdr.Item(k)) Then
                            'some of the columns (notably options and extras) contain CD lists

                            Dim nv As Single = -1
                            If k = "display" Then
                                If InStr(rdr.Item(k), "_") Then
                                    Dim p() As String = Split(rdr.Item(k), "_")
                                    Dim res As String = p(3)
                                    If res = "LED" Then res = p(4)
                                    Dim dm As String() = Split(res, "x")
                                    nv = Val(dm(0)) * Val(dm(1)) 'find the number of pixels
                                    If nv = 0 Then Stop 'we create the productattribute a little later

                                    If Not iq.i_attribute_code.ContainsKey("displaySize") Then
                                        Dim ds As clsAttribute = New clsAttribute("displaySize", iq.AddTranslation("Display Size (diagonal)", English, "DispSZ", 0, Tlwc, nextkey, False), 0)
                                    End If

                                    'DIS_15.6_WXGA_1366x768_AGBV
                                    pa = New clsProductAttribute(Product, iq.i_attribute_code("displaySize"), p(1), iq.i_unit_code("Inch"), Nothing, AttribWriteCache)

                                    'If InStr(p(1)(3)),"x" then do something here for megapixels

                                End If
                            End If

                            If k = "extras" Or k = "options" Or k = "software" Or k = "raidTech" Then
                                For Each ik In Split(rdr.Item(k), ",")
                                    'for each of the CD values ad an attribute of the type of the value
                                    'abtl = optabbreviations(UCase(k)) 'abbreviations translation of MCR,CAM,SDR,BT etc
                                    If Not iq.i_attribute_code.ContainsKey(Left(ik, 20)) Then
                                        'we don't have an MCR, CAM, SDR *attribute* yet - so make one
                                        If Not optabbreviations.ContainsKey(UCase(ik)) Then
                                            'well it wasn't in abbreviations - so it's *probably* a part number.. or maybe something like "keyboard kit"
                                            ' Stop
                                            'append it to the "additional" attribute
                                            att = Nothing
                                        Else
                                            If LCase(ik) = "name" Then Stop
                                            att = New clsAttribute(Left(ik, 20), optabbreviations(UCase(ik)), 0)  'an MCR,CAM,SDR (or some other recogised abbreviation)
                                        End If
                                    End If

                                    If Not att Is Nothing Then
                                        att = iq.i_attribute_code(Left(ik, 20))                                    '                                                                                      yes
                                        If Not Product.i_Attributes_Code.ContainsKey(att.Code) Then
                                            pa = New clsProductAttribute(Product, att, 1, textUnit, Nothing, AttribWriteCache)
                                        Else
                                            Product.i_Attributes_Code(att.Code)(0).NumericValue += 1
                                            Product.i_Attributes_Code(att.Code)(0).update(errormessages)
                                            Logit("duplicate " & k & ":" & ik)
                                        End If

                                    End If

                                Next
                            Else
                                'add an attribute of the type of the column header (e.g. warrantyCode,formFactor,mfrBuildCode,display,intVideo,ILOhardware,terStorTech,raidTech"
                                ''  If InStr(rdr.Item(k), ",") Then Stop
                                If LCase(rdr.Item(k)) = "name" Then Stop
                                If LCase(k) = "name" Then Stop
                                If optabbreviations.ContainsKey(UCase(rdr.Item(k))) Then
                                    abtl = optabbreviations(UCase(rdr.Item(k))) 'the translation of theis abbreviation will (should) alrery exist .. eg."WTY111NBD" = [[IQ.clsLanguage, 1 Year Parts / 1 Year Labour / 1 Year Onsite Warranty Next Business Day]
                                    pa = New clsProductAttribute(Product, iq.i_attribute_code(k), nv, textUnit, abtl, AttribWriteCache)
                                Else
                                    'Something for which there was no IQ1 abbreviation like 'french keyboard' or EMA7029 
                                    '    Beep()
                                End If
                            End If
                        End If
                    Next

                    'This is done in import descriptions
                    'desc = New clsProductAttribute(Product, iq.Attributes("desc"), 0, iq.Units("txt"), iq.addTranslation(Trim$(rdr.Item("desc"))).Key, AttribWriteCache)

                    Dim sku$
                    sku$ = Trim$(rdr.Item("modelsku"))
                    If InStr(LCase(sku$), "paul") Then Stop
                    If sku$ = "" Then Stop

                    '    mfrSKU = New clsProductAttribute(Product, iq.i_attribute_code("MfrSKU"), 0, textUnit, iq.AddTranslation(sku$, English, "SKU", 0, Tlwc, nextkey, False), AttribWriteCache)

                    If Not dicPlcode.ContainsKey(sku$) Then
                        Logit("Can't locate PLCode for system '" & sku$ & "'")
                    Else
                        Dim pl$
                        pl = dicPlcode(sku$)
                        PLcode = New clsProductAttribute(Product, iq.i_attribute_code("PLcode"), 0, textUnit, iq.AddTranslation(pl, English, "PL", 0, Tlwc, nextkey, False), AttribWriteCache)
                    End If

                    'for systems - their 'name' *is* their part number
                    ' SystemName = New clsProductAttribute(Product, iq.i_attribute_code("~ame"), 0, textUnit, mfrSKU.Translation, AttribWriteCache)

                    ' If InStr(LCase(SystemName.displayName(English)), "paul") Then Stop
                    'SystemName = New clsProductAttribute(Product, iq.Attributes("~ame"), 0, iq.Units("txt"), iq.AddText(rdr.Item("familycode"), s_lang, TranslationWriteCache).Key, AttribWriteCache)

                    'product attributes are a list of each type.. so we can have multiple alsohosts and don't need a horrid comma separated list)
                    Dim alsoHost As clsProductAttribute
                    If Not IsDBNull(rdr.Item("alsohost")) Then
                        For Each h In Split(rdr.Item("alsoHost"), ",")
                            alsoHost = New clsProductAttribute(Product, iq.i_attribute_code("alsoHost"), 0, textUnit, iq.AddTranslation(rdr.Item("alsoHost"), English, "", 0, Tlwc, nextkey, False), AttribWriteCache)
                        Next
                    End If

                    Dim fn$ = rdr.Item("sysFamilyname")

                    'DO NOT unabreviate it here !!
                    'If dicAbbreviations.ContainsKey(fn$) Then fn$ = dicAbbreviations(fn$)
                    'NOTE - the translations of the family name won't be duplicated - so, although every system will have a family attribute - all those attributes to a s set of a hundred or so tranlsations
                    FamMajor = New clsProductAttribute(Product, iq.i_attribute_code("FamMajor"), 0, textUnit, iq.AddTranslation(fn$, English, "FamMajor", 0, Tlwc, nextkey, False), AttribWriteCache)

                    If dicAbbreviations.ContainsKey(fn$) Then fn$ = dicAbbreviations(fn$)
                    FamDisp = New clsProductAttribute(Product, iq.i_attribute_code("FamDisp"), 0, textUnit, iq.AddTranslation(fn$, English, "FamDisp", 0, Tlwc, nextkey, False), AttribWriteCache)


                    'Family Minor -- (Familycode - granular)
                    Dim tl As clsTranslation

                    If Trim$(rdr.Item("familycode")) = "" Then Stop
                    tl = iq.AddTranslation(Trim$(rdr.Item("familycode")), English, "FamMinor", 0, Tlwc, nextkey, False)
                    FamMinor = New clsProductAttribute(Product, iq.i_attribute_code("FamMinor"), 0, textUnit, tl, AttribWriteCache)

                    If Not rdr.Item("cpu") Is DBNull.Value Then
                        cpuSKU = New clsProductAttribute(Product, iq.i_attribute_code("cpuSKU"), 0, textUnit, iq.AddTranslation(Trim$(rdr.Item("cpu")), English, "CPUSKU", 0, Tlwc, nextkey, False), AttribWriteCache)
                    End If

                    Dim fcode As String
                    Dim famBranch As clsBranch

                    fcode = Trim$(rdr.Item("sysfamilyname"))
                    If dicfamily.ContainsKey(fcode) Then  'family dictionary contains familycode>branch 

                        famBranch = dicfamily(fcode)


                        'If dicChains.ContainsKey(sc$) Then 'There are some PR supply chains

                        '        Dim scbranch As clsBranch = famBranch.ChildNamed(dicChains(sc$)) 'supply chain (top value/regular) - contains systems

                        ' If scbranch Is Nothing Then
                        'the 'regular' suuply chain is less impotant (comes after) any promo supply chain TV/SB
                        ' scbranch = New clsBranch(Nothing, famBranch, dicChains(sc$), "", sysTrans, sysTransSingular, Nothing, IIf(sc$ = "A", 100, 10), False, "B")
                        ' End If

                        'creates a new branch and adds it as a child of the SUPPLY CHAIN under the family 

                        'aBranch = New clsBranch(Product, dicfamily(fcode), product.i_attributes_code("~ame").TextKey, "")
                        '    aBranch = New clsBranch(Product, scbranch, Product.i_Attributes_Code("~ame").Translation, "", optTrans, optTransSingular, Nothing) 'these ARE the systems (so we use the opt key - becuase they *contain* options)

                        '   If InStr(LCase(SystemName.Translation.text(English)), "paul") Then Stop

                        Dim picture$
                        picture = famBranch.Picture
                        If Not IsDBNull(rdr.Item("sysfamilyimg")) Then
                            picture = rdr.Item("sysfamilyimg")
                            'picture = Split(picture, "_")(1)
                        End If

                        'aBranch = New clsBranch(Product, scbranch, SystemName.Translation, picture, optTrans, optTransSingular, Nothing, scbranch.childBranches.Count * 10, False, "T") 'these ARE the systems (so we use the opt key - becuase they *contain* options)
                        '  Dim SKU As clsTranslation = iq.AddTranslation(rdr.Item("MoDELSKU"),English,"sysSKUs",0,tlwc
                        sysBranch = New clsBranch(Product, famBranch, mfrSKU.Translation, picture, optTrans, optTransSingular, Nothing, famBranch.childBranches.Count * 10, False, "T") 'these ARE the systems (so we use the opt key - becuase they *contain* options)
                        dicsystems.Add(Trim$(rdr.Item("ModelSKU")), sysBranch)

                        'make the quantity records the make the system visible by region/country - these are the gobal/pathless ones

                        Dim rgns As String = ""
                        If Not IsDBNull(rdr.Item("activesites")) Then rgns = rdr.Item("activesites")
                        If rdr.Item("aaonly") <> 0 Then
                            rgns &= ",AA"
                        End If


                        'there are a few 'junk' systems with no activesites
                        If rgns = "" Then
                            Dim qty As clsQuantity  'EXCLUDE this system eveywhere (with a min increment of 0) - 
                            qty = New clsQuantity(r_worldwide, "", sysBranch, 0, 0, 0, 0, QtyWritecache)
                            'Public Sub New(region As clsRegion, ByVal Path As String, ByVal branch As clsBranch, ByVal numPreInstalled As Integer, ByVal MinIncrement As Integer, ByVal PreferredIncrement As Integer, freeOfCharge As Boolean, Optional Writecache As DataTable = Nothing)
                        Else
                            MakeSystemQuantities(sysBranch, rgns, containment, QtyWritecache)
                        End If
                    Else
                        Stop
                    End If

                End If
            End If


        End While
        rdr.Close()
        da.BulkWrite(con, QtyWritecache, "Quantity")
        QtyWritecache = Nothing

        'write the accumulated product attributes (bulk copy)
        da.BulkWrite(con, AttribWriteCache, "ProductAttribute")
        AttribWriteCache = Nothing

        da.BulkWrite(con, Tlwc, "Translation")
        Tlwc = Nothing


    End Sub
    Public Sub makeSpecAttributes(att$)

        'Accepts a comma seperated list of ^ delimited code^name pairs - for which to make attributes (if they don't exist)
        'prefixes each attribute with 

        Dim p() As String
        Dim anAttribute As clsAttribute

        For Each a In Split(att$, ",")
            p = Split(a, "^")
            If Not iq.i_attribute_code.ContainsKey(p(0)) Then
                anAttribute = New clsAttribute(p(0), iq.AddTranslation(p(1), English, "SpecAtts", 0, Nothing, 0, False), 0)
            End If
        Next

    End Sub
    Public Sub MakeSystemQuantities(ByVal branch As clsBranch, ByVal regionList As String, containment As Dictionary(Of String, List(Of String)), ByVal qtyWriteCache As DataTable)

        'RegionList contains a comma seperated list of region and country codes (now all regions) - from the ActiveSties column of dbo.systems

        regionList = Replace(regionList, ".", ",")  'Fix some minor issues in the source data - some .'s that should be commas
        regionList = Replace(regionList, " ", "")  ' and some spaces - that shouldn't be there

        Dim rl As List(Of String) = Split(regionList, ",").ToList

        Dim RGN As clsRegion

        If Not rl.Contains("XW") Then
            Dim codelist As List(Of String) = cleanRegions(rl, containment)

            Dim aQuantity As clsQuantity

            For Each code In codelist
                If code <> "" Then
                    If code = "UK" Then code = "GB"
                    'If code = "AA" Then Stop
                    If Not iq.i_region_code.ContainsKey(Trim$(code)) Then
                        Dim sku$ = branch.SKU
                        Logit(code & " is not a valid region for system " & sku)
                    Else
                        RGN = iq.i_region_code(Trim$(code))

                        Dim ck As String = branch.ID & "^" & RGN.ID & "^"
                        If Not Import.i_Quantities.Contains(ck) Then
                            aQuantity = New clsQuantity(RGN, "", branch, 0, 1, 1, 0, qtyWriteCache)
                            Import.i_Quantities.Add(ck)
                        Else
                            NoOp()
                        End If
                    End If
                End If
            Next
        End If


    End Sub

    Public Function autoadds(con As SqlClient.SqlConnection, dicAutoadds As Dictionary(Of String, clsProduct), dicSystems As Dictionary(Of String, clsBranch), dicRegions As Dictionary(Of String, clsRegion)) As String

        Dim QuantityWriteCache As DataTable
        QuantityWriteCache = da.MakeWriteCacheFor(con, "Quantity")

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$

        Dim added As Integer

        Logit("Importing autoadds", True, True)

        ' sql$ = "SELECT [CountryCode],[ModelSKU],[AddSKU],[OptType] from " & server$ & "[iq].[Products].[AutoAdds] order by modelsku,addsku,countrycode"  'If we were clever we could collpase autoadds by region


        sql$ = "SELECT [CountryCode],a.[ModelSKU],[AddSKU],[OptType],s.FamilyCode,a.ranking from " & server$ & "[iq].[Products].[AutoAdds] a "
        sql$ &= "JOIN " & server & "iq.products.Systems s on a.modelsku=s.ModelSKU "
        sql$ &= "WHERE (familycode LIKE '%" & restrictImportToFamily & "%' or familycode = '' or familycode is null )"
        sql$ &= "ORDER by s.familycode,modelsku,addsku,countrycode "

        Dim path$
        Dim sysBranch As clsBranch
        Dim optionbranch As clsBranch
        Dim optionpath$ = ""
        Dim optsku$ = ""
        Dim syssku$
        Dim ck$

        Dim missed As Integer = 0

        Dim sysfam As String = ""
        Dim osysfam As String = ""

        Dim ranking As Integer = 0

        Dim optPaths As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))(StringComparer.CurrentCultureIgnoreCase)

        Dim sw As StreamWriter = New StreamWriter("c:\temp\autoAdds.txt")

        Dim dudopts As Integer = 0
        Dim there As Integer = 0
        Dim inactive As Integer = 0
        Dim adds As List(Of String) = New List(Of String)



        rdr = da.DBExecuteReader(con, sql$)
        While (rdr.Read)

            'compound key (used to check if we've already imported)
            ck$ = rdr.Item("countrycode") & "^" & rdr.Item("ModelSku") & "^" & rdr.Item("addSku")

            sw.WriteLine(ck$)

            syssku = rdr.Item("modelsku")
            sysfam = rdr.Item("familycode")
            ranking = rdr.Item("ranking")

            If (Left(sysfam, restrictImportToFamily.Length).ToUpper = restrictImportToFamily.ToUpper) Or String.IsNullOrEmpty(restrictImportToFamily) Then ''switch between OR True and OR False to restirct what's imported
                '                If ck$ = "UK^704560-421^U2GC1E" Then Stop
                If dicAutoadds.ContainsKey(ck$) Then
                    Logit("Already there:" & ck)
                    there += 1
                Else

                    If Not dicSystems.ContainsKey(syssku) Then
                        'TODO Reinstate anerror = New clsEvent(parentEvent, "AutoAdd " & ck$ & "system " & syssku & " is not recognised", ev_Warning)
                        Logit("Auto add for sku " & ck$ & " is not vaild (system may be inactive")
                        inactive += 1
                        'Beep()
                    Else

                        If ranking = 1 Then ' Only autoadd if ranking is 1 otherwise it is a top recomendation 
                            optsku = rdr.Item("addsku")


                            '  If optsku = "UK066E" Then Stop '

                            If Not iq.i_SKU.ContainsKey(optsku) Then
                                'TODO  reinstate        anerror = New clsEvent(parentEvent, "Autoadd option sku " & optsku & " not recognised", ev_Warning)
                                Logit("Autoadd option sku " & optsku & " not recognised")
                                dudopts += 1
                            Else
                                If iq.i_SKU(optsku).ProductType.Code.ToLower <> "wty" Then 'Comment this line if you want warrent in autoadds.
                                    sysBranch = dicSystems(syssku)
                                    Dim syspath = "tree." & Trim$(iq.RootBranch.ID) 'root
                                    syspath &= "." & Trim$(sysBranch.Parent.Parent.ID) 'System type
                                    syspath &= "." & Trim$(sysBranch.Parent.ID) 'Family
                                    syspath &= "." & Trim$(sysBranch.ID)

                                    If sysfam <> osysfam Then  'when the system subfamily changes - re-populate sysbanch.skupaths
                                        osysfam = sysfam
                                        optPaths.Clear()  'Important !! (or they'd just build up in here!)
                                        sysBranch.SkuPaths(optPaths, "", True)
                                    End If

                                    If optPaths.ContainsKey(optsku) Then
                                        If optPaths(optsku).Count > 1 Then  'this options appears more than once under the system
                                            'Need to find the non TRO one...
                                            For Each ob In optPaths(optsku)
                                                If iq.Branches(Split(ob, ".").Last) IsNot Nothing AndAlso iq.Branches(Split(ob, ".").Last).Parent IsNot Nothing AndAlso iq.Branches(Split(ob, ".").Last).Parent.Parent IsNot Nothing AndAlso Not iq.Branches(Split(ob, ".").Last).Parent.Parent.Translation.text(English) = "Top Recommended" Then
                                                    optionbranch = iq.Branches(Split(ob, ".").Last)
                                                    optionpath = syspath & ob
                                                    Exit For
                                                End If
                                            Next
                                        Else
                                            optionbranch = iq.Branches(Split(optPaths(optsku)(0), ".").Last)
                                            optionpath = syspath & optPaths(optsku)(0)
                                        End If
                                    Else
                                        optionbranch = Nothing
                                    End If

                                    'optionbranch = sysBranch.findChildBySKU2(path$, optsku, optionpath$) 'staring at this branch/path - recurse down until you find the sku - returns branch and its address 
                                    If optionbranch Is Nothing Then
                                        Logit("Could not locate autoadd option " & optsku & " under system " & syssku)
                                        missed += 1
                                    Else
                                        'If optionpath$ = "" Then Stop
                                        Dim ffff$ = PathName(optionpath)
                                        'If ck$ = "UK^704560-421^U2GC1E" Then Stop
                                        Dim found = False
                                        For Each q In optionbranch.Quantities.Values
                                            If q.NumPreInstalled > 0 And q.Path = optionpath$ Then found = True
                                        Next
                                        If Not found Then
                                            makeAutoAdd(optionbranch, rdr.Item("countrycode"), optionpath$, QuantityWriteCache)
                                            dicAutoadds.Add(ck$, optionbranch.Product)
                                            adds.Add(ck$ & " " & optionbranch.Product.DisplayName(English))

                                            Logit("Added " & ck$ & optionbranch.Product.DisplayName(English) & " " & optionbranch.Product.ProductType.Code)
                                            added += 1
                                        End If
                                    End If
                                End If
                            End If
                        Else
                            'Top Recomended option code goes here 
                        End If
                    End If
                End If
            End If
        End While



        rdr.Close()
        sw.Close()

        da.BulkWrite(con, QuantityWriteCache, "Quantity")
        QuantityWriteCache = Nothing

        Logit("added:" & added & " missed:" & missed & " dudopts:" & dudopts & " There:" & there & " inactive: " & inactive)
        Logit("completed autoadds", False, True)




    End Function

    'Private Function makeAutoAdd(branch As clsBranch, skuvariant As clsVariant, countryCodes As String, path As String, writecache As DataTable)
    Private Sub makeAutoAdd(branch As clsBranch, countryCodes As String, path As String, writecache As DataTable)

        Dim aQuantity As clsQuantity = Nothing

        Dim cclist As List(Of String) = Split(countryCodes, ",").ToList

        For Each ccode In cclist
            If ccode = "UK" Then
                ccode = "GB"

            End If
            If iq.i_region_code.ContainsKey(ccode) Then
                Dim rgn As clsRegion = iq.i_region_code(ccode)
                'aQuantity = New clsQuantity(iq.i_region_code(ccode), path$, branch, skuvariant, 1, 1, 1, 0, writecache)

                Dim ck$ = branch.ID & "^" & rgn.ID & "^" & path$
                If i_Quantities.Contains(ck$) Then
                    NoOp()
                Else
                    aQuantity = New clsQuantity(iq.i_region_code(ccode), path$, branch, 1, 1, 1, 0, writecache)
                    i_Quantities.Add(ck$)
                End If


            Else
                Stop
                Logit("country " & ccode & " is not in the imported dictionary")
            End If
        Next

    End Sub


    Public Sub Margins(con As SqlClient.SqlConnection, dicSystems As Dictionary(Of String, clsBranch), optionsbysku As Dictionary(Of String, clsProduct), _
                       dicChannels As Dictionary(Of String, clsChannel))

        Dim rdr As SqlClient.SqlDataReader
        Dim price As Decimal
        Dim pass As Integer = 1

        Dim badmargins As List(Of clsMargin)
        badmargins = New List(Of clsMargin)

        For pass = 1 To 2

            'fix rougue account numbers created by wescoast logins to the 'wrong' portal
            da.DBExecutesql("update h3.channelcentral.customers.users set priceBand=null where ChanID='DWERG74AH'")

            Dim sql$
            sql$ = "SELECT h.hostid as seller,ha.currency,u.chanid as buyer,mfrpartnum AS partno,hostmfrpartnum,internalprice AS iprice,ha.priceBand,hostPartNum,externalprice as price "
            sql$ &= "FROM " & server$ & "[iq].products.pricelistmaster h "
            sql$ &= "JOIN " & server$ & "[channelcentral].customers.users u ON u.priceBand=h.priceBand  "
            sql$ &= "JOIN " & server$ & "[channelcentral].customers.hostaccounts ha ON h.priceBand=ha.priceBand "
            sql$ &= "WHERE ha.priceBand is not null AND currency<>'nul' and internalprice is not null "
            sql$ &= "GROUP BY h.hostid,chanid,mfrpartnum,hostmfrpartnum,internalprice,curr,currency,ha.priceBand,hostpartnum,externalprice "
            sql$ &= "ORDER BY partno,seller,buyer"

            rdr = da.DBExecuteReader(con, sql$)

            Dim part As clsProduct = Nothing
            'For each product - each seller offers prices to each buyer in each currency
            'Dim prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of String, Single)))

            Dim partno As String
            Dim oPartno As String = ""

            Dim BuyerChannel As clsChannel
            Dim SellerChannel As clsChannel
            Dim Currency As clsCurrency
            Dim partnos As Integer

            Dim pricesrows As Integer = 0

            Dim bad As Integer = 0
            Dim good As Integer = 0
            Dim nobase As Integer = 0
            Dim zerobase As Integer = 0

            Dim dicbad As New Dictionary(Of clsChannel, Integer)
            dicbad = New Dictionary(Of clsChannel, Integer)

            While rdr.Read
                partno = Trim$(rdr.Item("partno"))
                If partno <> oPartno Then
                    partnos += 1
                    If dicSystems.ContainsKey(partno) Then
                        part = dicSystems(partno).Product
                    Else
                        If optionsbysku.ContainsKey(partno) Then
                            part = optionsbysku(partno)
                        Else
                            part = Nothing
                        End If
                    End If
                End If

                Dim SKUvariant As clsVariant

                Dim dupes As Integer = 0

                If part Is Nothing Then
                    Logit("Invalid SKU (mfrPartno) '" & partno & "' whilst importing pricelistmaster")
                Else
                    If Not dicChannels.ContainsKey(Trim$(rdr.Item("seller"))) Then
                        Logit("Couldn't locate the seller channel for '" & rdr.Item("seller") & "' (pricelistmaster.hostID) - check CaSe and trailing     spaces.")
                    Else
                        SellerChannel = dicChannels(Trim$(rdr.Item("seller")))
                        If Not dicChannels.ContainsKey(Trim$(rdr.Item("buyer"))) Then
                            Logit("Couldn't locate the buyer channel for '" & rdr.Item("buyer") & "' (channelcentral.customers.users.[chanid] - check CaSe and trailing     spaces.")
                        Else
                            If Trim$(rdr.Item("Buyer")) = "RTERG74AH" Then
                                Logit("Skipping test account " & rdr.Item("buyer"))
                            Else

                                BuyerChannel = dicChannels(Trim$(rdr.Item("buyer")))
                                Currency = iq.i_currency_code(Trim$(rdr.Item("currency")))

                                'See if the host partnumber has a # in, determine the variant
                                Dim hostpartnum As String
                                hostpartnum = rdr.Item("Hostmfrpartnum")
                                Dim ih As Integer = InStr(hostpartnum, "#")

                                '      Dim newvariant As clsVariant
                                '      Dim distiSku As String
                                '      If IsDBNull(rdr.Item("hostpartnum")) Then distiSku = rdr.Item("mfrpartnum") Else distiSku = rdr.Item("hostpartnum")

                                ' If distiSku = "" Then Stop
                                ' newvariant = New clsVariant("", distiSku, "", "", "", "")

                                'see if we already have a price for this part - for this seller/currency/variant combo
                                'we may well do as PricelistMaster contains rows for many buyers

                                If Trim(SellerChannel.Code) = "DWERG74AH" And Currency.Code = "EUR" Then
                                    Logit(partno & " was quoted in euros by Westcoast (not WestCoast Ireland!) - skipping")
                                Else

                                    If Not SellerChannel.Margin.ContainsKey(BuyerChannel) Then
                                        SellerChannel.Margin.Add(BuyerChannel, New Dictionary(Of clsSector, clsMargin))
                                    End If

                                    Dim producttype As clsProductType = part.ProductType
                                    Dim factor As String
                                    Dim basePrice As NullablePrice

                                    If part.i_Variants IsNot Nothing Then
                                        If Not part.i_Variants.ContainsKey(SellerChannel) Then

                                            'this part is not sold by this challel
                                            Logit(part.SKU & " is not sold by " & SellerChannel.Code)

                                        Else

                                            SKUvariant = part.i_Variants(SellerChannel)(0)
                                            basePrice = SKUvariant.BasePrice(Currency)

                                            If basePrice.NumericValue = 0 Then
                                                'Logit("basePrice price for " & partno & " (" & SellerChannel.Name & ") was 0")
                                                zerobase += 1
                                            Else

                                                If IsDBNull(basePrice.value) Then
                                                    Logit("No base price defined for " & partno)
                                                    nobase += 1
                                                Else

                                                    If rdr.Item("price") Is DBNull.Value Then
                                                        'Null external price
                                                    Else

                                                        price = rdr.Item("price")
                                                        factor = price / basePrice.NumericValue

                                                        '                              buyer                   sector                   product type    margin
                                                        'Public Margin As Dictionary(Of clsChannel, Dictionary(Of clsSector, Dictionary(Of clsProductType, clsMargin)))

                                                        Dim amargin As clsMargin

                                                        With SellerChannel.Margin(BuyerChannel)  'work with this sellers margins for this buyer 
                                                            'If Not .ContainsKey(part.Sector) Then
                                                            '    SellerChannel.Margin(BuyerChannel).Add(clsSector,ector, clsMargin))
                                                            'End If
                                                            If Not SellerChannel.Margin(BuyerChannel).ContainsKey(part.Sector) Then
                                                                'create a new margin (which adds it to the sellers(buyers) dictionary - and inserts it in the database)
                                                                amargin = New clsMargin(SellerChannel, BuyerChannel, factor, "", part.Sector, partno)
                                                            End If

                                                            Dim knownMargin As clsMargin
                                                            knownMargin = SellerChannel.Margin(BuyerChannel)(part.Sector)

                                                            If badmargins.Contains(knownMargin) Then
                                                                'we know there are some prices inconsistent with the margin
                                                                If pass = 2 Then
                                                                    ' make a specific price (for every product in sector any sector with bad margins)
                                                                    If rdr.Item("iprice") = 0 Then Stop

                                                                    ''@@NObbled - TODO                                           aprice = New clsPrice(part, SKUvariant, SellerChannel, BuyerChannel, New nullablePrice(rdr.Item("iprice"), Currency), "Specific price")

                                                                End If

                                                            Else

                                                                If Math.Abs(knownMargin.Factor - factor) > 0.001 Then
                                                                    'oh dear - we have conflicting margins on products of the same ProductType - Within one sector
                                                                    knownMargin.bad = True

                                                                    badmargins.Add(knownMargin)

                                                                    bad += 1

                                                                    Logit("inconsistent margin for seller:" & SellerChannel.Name & " buyer:" & BuyerChannel.Name & " product type " & part.ProductType.Code)
                                                                    Logit("Previous factor was " & knownMargin.Factor & " based on SKU " & knownMargin.SampledSKU)
                                                                    Logit("New factor for " & partno & " is " & factor)

                                                                Else
                                                                    'yep - that's fine (margins match)
                                                                    'Beep()
                                                                    good += 1

                                                                End If
                                                            End If

                                                        End With
                                                    End If
                                                End If
                                            End If
                                        End If
                                    End If

                                End If
                            End If
                        End If
                    End If
                End If

                oPartno = partno
                pricesrows += 1
            End While

            Logit(bad & " SKUs had inconsistent margins, " & nobase & "SKUs had no base price defined," & zerobase & " base prices were zero")
            'Logit("Imported " & good & " margins for " & partnos & " distinct SKUs in " & TimeSince(lastmilestone), False, True)
            rdr.Close()

        Next pass

    End Sub


    Public Sub Stock(con As SqlClient.SqlConnection, dicStock As Dictionary(Of String, clsstock), dicSystems As Dictionary(Of String, clsBranch), dicOptions As Dictionary(Of String, clsProduct), dicChannels As Dictionary(Of String, clsChannel))

        'Pna stock only contains 1 row per HostID/Sku ... with a maximum of one future shipment
        Dim sql$
        '   sql$ = "SELECT rowID,hostID,hostmfrpartnum,mfrpartnum,stock,ts,duedate,dueqty from " & server$ & "[iq].products.PNA_Stock"

        con.Close()
        con = da.OpenDatabase()

        sql$ = "SELECT hostid,hostmfrpartnum,mfrpartnum,hostsku,Stock,DueDate,dueqty,rowid from " & server$ & "[iq].products.PNA_stock group by hostid,hostmfrpartnum,mfrpartnum,hostsku,Stock,DueDate,dueqty,rowid order by rowid desc"

        Dim rdr As SqlClient.SqlDataReader

        Dim added As Integer = 0
        Dim Updated As Integer = 0
        rdr = da.DBExecuteReader(con, sql$)
        Dim stock As clsstock

        Dim problems As Integer = 0
        Dim dupes As Integer

        If rdr.HasRows Then
            While rdr.Read
                'current stock/future
                For pass As Integer = 1 To 2
                    If dicStock.ContainsKey(rdr.Item("rowid") & "-" & pass) Then   'UPDATE existing
                        stock = dicStock(rdr.Item("rowid") & "-" & pass)
                        Dim duedate As Object
                        duedate = IIf(rdr.Item("duedate") Is DBNull.Value, CDate("1980-01-01"), rdr.Item("duedate"))

                        If stock.quantity <> rdr.Item("stock") Or stock.Arrival <> duedate Then
                            stock.quantity = rdr.Item("stock")
                            stock.Arrival = duedate
                            stock.LastUpdated = Now
                            stock.Source = "IQ1 import"
                            stock.update() ' quite expensive - could in theory bulk delete/write (with INSERT_IDENTITY on), for better performance
                            Updated += 1
                        End If
                    Else

                        'and ADD new
                        Dim product As clsProduct = Nothing
                        Dim sku$
                        sku$ = rdr.Item("mfrpartnum")
                        If dicSystems.ContainsKey(sku) Then
                            product = dicSystems(sku).Product
                        ElseIf dicOptions.ContainsKey(sku) Then
                            product = dicOptions(sku)
                        Else
                            '    Dim Err = New clsEvent(parentEvent, sku & " is not a system or an option", "error")
                            problems += 1
                            '    Stop
                        End If

                        If Not product Is Nothing Then

                            Dim arrival As Date

                            If pass = 1 Then
                                arrival = DateAdd(DateInterval.Day, -1, Now) 'First pass is current stock - ie.. it arrived in the past
                            ElseIf pass = 2 Then
                                If rdr.Item("duedate") Is DBNull.Value Then
                                    arrival = Now
                                Else
                                    arrival = rdr.Item("duedate")
                                End If
                            End If

                            'if there are no variants (ie no 'internal' price - we can't add stock)
                            If product.Variants IsNot Nothing Then
                                addstock(rdr, product, arrival, dicChannels, dicStock, pass, dupes, added)
                            End If


                        Else

                            'anevent = New clsEvent(parentEvent, "Could not locate host/channel " & rdr.Item("hostid") & " from PNAStock", "error")
                            problems += 1

                        End If
                    End If
                Next
            End While
        End If
        rdr.Close()


    End Sub
    Private Sub addstock(rdr As SqlDataReader, Product As clsProduct, arrival As Date, dicChannels As Dictionary(Of String, clsChannel), dicStock As Dictionary(Of String, clsstock), pass As Integer, ByRef dupes As Integer, ByRef added As Integer)

        Dim skuvariant As clsVariant = Nothing
        Dim Existing As Boolean = False
        Dim stock As clsstock = Nothing
        Dim seller As clsChannel

        If Product.i_Variants Is Nothing Then Exit Sub

        If dicChannels.ContainsKey(rdr.Item("hostID")) Then
            seller = dicChannels(rdr.Item("hostid"))  '  ).IsCloneOf)  'get the stock from the source of any  clone ??? RM - ask dan

            If Product.i_Variants.ContainsKey(seller) Then  ' do we have a variant for this seller ?
                skuvariant = Product.i_Variants(seller)(0)
                For Each shipment In skuvariant.shipments.Values
                    If pass = 1 Then
                        If shipment.IsCurrent Then Existing = True : Exit For
                    Else
                        If Not shipment.IsCurrent Then Existing = True : Exit For
                    End If
                Next
                If Existing Then
                    ' anevent = New clsEvent(parentEvent, "Duplicate stock in PNA_Stock for host " & rdr.Item("hostid") & " part " & sku$, "error")
                    dupes += 1
                End If

                'check it's not a dupe - (IQ1.PNA_Stock had dupes)
                Dim isdupe As Boolean = False
                If Not Existing Then
                    Dim isCurrent As Boolean = (pass = 1)
                    If Product.i_Variants Is Nothing Then
                        'stock for a product nobody has a price for
                        Beep()
                    Else
                        If Product.i_Variants.ContainsKey(seller) Then
                            If Product.i_Variants(seller) IsNot Nothing Then
                                skuvariant = Product.i_Variants(seller)(0)


                                If skuvariant.shipments.ContainsKey(arrival.Date) Then
                                    isdupe = True
                                End If
                            End If
                        End If

                        If Not isdupe Then
                            If Not IsDBNull(rdr.Item("stock")) Then
                                stock = New clsstock(skuvariant, rdr.Item("stock"), arrival.Date, "IQ1ii " & Now.ToString, isCurrent)
                                dicStock.Add(rdr.Item("rowID") & "-" & pass, stock)
                                added += 1
                            End If

                        Else
                            'duplicated stock
                        End If
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub Prices(con As SqlClient.SqlConnection, dicSystems As Dictionary(Of String, clsBranch), dicskuoptionproduct As Dictionary(Of String, clsProduct), _
                      dicChannels As Dictionary(Of String, clsChannel)) ', dicvariants As Dictionary(Of String, clsVariant))

        Dim lastmilestone As Double

        'get the 'base' prices from pricelistmaster (for each Seller) - ie. the pricelistmaster.internalprice where priceBand is null
        'we do not import customer specific prices - they will come from the pricing database/feeds or webservice

        'This DOES NOT get prices for clones... who's prices now derive from the parent - and use the margin of the enquiring customer (of the clone)

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$

        sql$ = "SELECT h.hostid as seller,mfrpartnum AS partno,hostmfrpartnum,hostpartnum,internalprice AS iprice,c.currencyCode "
        sql$ &= "from " & server$ & "[iq].products.pricelistmaster h "
        sql$ &= "join " & server$ & "[iq].dbo.currencies c on h.curr=c.CurrencySymbol "
        sql$ &= "WHERE(h.priceBand Is null or h.priceBand='sp') " 'servers plus
        sql$ &= "AND h.HostID NOT IN (SELECT subhost from " & server$ & "[channelcentral].customers.Host_Parents)" 'DON't get prices for clones !
        sql$ &= "GROUP BY h.hostid,mfrpartnum,internalprice,c.CurrencyCode,hostmfrpartnum,hostpartnum  "
        sql$ &= "ORDER BY partno,seller "

        Dim PriceWriteCache As New DataTable
        PriceWriteCache = da.MakeWriteCacheFor(con, "Price")

        Dim vWritecache As DataTable = New DataTable
        Dim nextid As Integer = 0
        vWritecache = da.MakeWriteCacheFor(con, "Variant", nextid, True) 'fetches the next available ID


        rdr = da.DBExecuteReader(con, sql$)

        Dim part As clsProduct = Nothing
        'For each product - each seller offers prices to each buyer in each currency
        'Dim prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of String, Single)))

        Dim partno As String
        Dim oPartno As String = ""

        'Dim BuyerChannel As clsChannel
        Dim SellerChannel As clsChannel
        Dim Price As clsPrice
        Dim Currency As clsCurrency

        Dim numprices As Integer
        Dim partnos As Integer

        Dim pricesrows As Integer = 0

        Dim dupes As Integer = 0
        Dim Dupe As Boolean = False

        Dim wcon As SqlClient.SqlConnection

        While rdr.Read
            partno = Trim$(rdr.Item("partno"))
            If partno <> oPartno Then
                partnos += 1
                If dicSystems.ContainsKey(partno) Then
                    part = dicSystems(partno).Product

                Else
                    If dicskuoptionproduct.ContainsKey(partno) Then
                        part = dicskuoptionproduct(partno)
                    Else
                        part = Nothing
                        'can happen for things like printer cartridges
                        ' Logit("Invalid SKU (mfrPartno) '" & partno & "' whilst importing pricelistmaster")
                    End If
                End If
            End If

            Dim SKUvariant As clsVariant

            If part Is Nothing Then
                'logging moved to above so it only happens when the part changes
            Else
                If Not dicChannels.ContainsKey(Trim$(rdr.Item("seller"))) Then
                    Logit("Couldn't locate the seller channel for '" & rdr.Item("seller") & "' (pricelistmaster.hostID) - check CaSe and trailing     spaces.")
                Else
                    Currency = iq.i_currency_code(Trim$(rdr.Item("currencycode")))
                    SellerChannel = dicChannels(Trim$(rdr.Item("seller")))

                    'See if the host partnumber has a # in, determine the variant
                    'Dim hostpartnum As String
                    'hostpartnum = rdr.Item("Hostmfrpartnum")
                    'Dim ih As Integer = InStr(hostpartnum, "#")

                    'If ih Then
                    '    Dim vc As String
                    '    vc = Trim$(Mid$(hostpartnum, ih))
                    '    If Not dicvariants.ContainsKey(vc) Then
                    '        Dim newvariant As clsVariant
                    '        newvariant = New clsVariant(vc, vc) 'for now - we use the code 'eg #ABA as the name  (see the variants table in the DB to translate)
                    '        dicvariants.Add(vc, newvariant)
                    '    End If
                    '    SKUvariant = dicvariants(vc)
                    'Else
                    '    SKUvariant = iq.StandardVariant
                    'End If

                    'set the distiSKU in the variant to HostPartnum,HostMfrPartnum or Partnum - in that order of precedence
                    Dim distisku$
                    If IsDBNull(rdr.Item("hostPartnum")) Then
                        If IsDBNull(rdr.Item("hostmfrpartnum")) Then
                            distisku$ = rdr.Item("partno")
                        Else
                            distisku = rdr.Item("hostMfrPartNum")
                        End If
                    Else
                        distisku = rdr.Item("HostPartnum")
                    End If

                    SKUvariant = New clsVariant("", part, SellerChannel, distisku, "", "", "", Nothing, False, vWritecache, nextid)


                    'see if we already have a price for this part - for this seller/currency/variant combo
                    'we may well do as PricelistMaster contains rows for many buyers

                    If IsDBNull(rdr.Item("iPrice")) Then
                        '      Logit("Price was null for " & rdr.Item("seller") & " " & hostpartnum)
                    Else
                        Pmark("DupeCheck")
                        Dupe = False
                        If SKUvariant.PriceExists(Everyone, Currency) Then
                            Dupe = True
                            dupes += 1
                        End If
                        Pacc("DupeCheck")

                        If Dupe Then
                            Logit("Duplicate base price for " & SellerChannel.DisplayName(s_lang) & "('" & rdr.Item("seller") & "') SKU='" & partno & "' currency='" & Currency.Code & "' SKUVariant='" & SKUvariant.Code & "'")
                        Else

                            'If rdr.Item("iPrice") = 0 Then Stop
                            Price = New clsPrice(SKUvariant, Everyone, New NullablePrice(CDec(rdr.Item("iprice")), Currency, False), "Import", PriceWriteCache)
                            numprices += 1

                        End If
                    End If
                End If
            End If

            oPartno = partno
            pricesrows += 1


            If PriceWriteCache.Rows.Count > 5000 Then
                wcon = da.OpenDatabase()  'seperate conection for the bulk writers

                da.BulkWrite(wcon, PriceWriteCache, "Price")
                'clone the STRUCTURE (emptying the table)
                Dim temp As DataTable = PriceWriteCache.Clone
                PriceWriteCache.Dispose()

                PriceWriteCache = temp 'PriceWriteCache.Clone '.Clear() 'da.MakeWriteCacheFor(con, "Price")
                wcon.Close()
            End If

            If vWritecache.Rows.Count > 5000 Then
                wcon = da.OpenDatabase()

                da.BulkWrite(wcon, vWritecache, "Variant")

                Dim temp As DataTable = vWritecache.Clone
                vWritecache.Dispose()
                vWritecache = temp
                wcon.Close()

                System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
                GC.Collect()

            End If

        End While

        rdr.Close()

        wcon = da.OpenDatabase()  'seperate conection for the bulk writers

        da.BulkWrite(wcon, PriceWriteCache, "Price")
        PriceWriteCache = Nothing

        da.BulkWrite(wcon, vWritecache, "Variant")
        vWritecache = Nothing
        wcon.Close()

        Logit("Imported " & numprices & " base prices for " & partnos & " distinct SKUs with " & dupes & " duplicates in " & TimeSince(lastmilestone))

        Logit("Wrote them to database (BulkCopy) in " & TimeSince(lastmilestone))


        '        con = da.opendatabase()


        '       WE MUST RELOAD THEM NOW WE'VE (BULK) CREATED  THE PRICES AND VARIANTS - TO GET THIER id'S
        For Each P In iq.Products.Values
            P.i_Variants = Nothing
        Next

        iq.Variants.Clear()
        con.Close()
        con = da.OpenDatabase()
        ' iq.LoadVariants(con, rdr, 0)

        Dim errorMessages As List(Of String) = New List(Of String)
        '   iq.LoadPrices(con, rdr, 0, errorMessages)

    End Sub


    Public Sub CarePackProperties()


        Dim a As clsAttribute

        If English Is Nothing Then English = iq.i_language_Code("EN")

        If Not iq.i_attribute_code.ContainsKey("CP_DRN") Then a = New clsAttribute("CP_DRN", iq.AddTranslation("Duration", English, "CPP", 0, Nothing, 0, False), 0)
        '9x5,13x5,24x7,Next Business Day,6hr Call-to-Response,Pickup and Return,12x7,13x7,9x7
        If Not iq.i_attribute_code.ContainsKey("CP_SVC") Then a = New clsAttribute("CP_SVC", iq.AddTranslation("Service Level", English, "CPP", 0, Nothing, 0, False), 0) 'This isn't displayed at present and has values 24x7, 13x5, next business day, 6hr Call To Response, pickup an return
        If Not iq.i_attribute_code.ContainsKey("CP_DMR") Then a = New clsAttribute("CP_DMR", iq.AddTranslation("Defective Media Retention", English, "CPP", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("CP_TRV") Then a = New clsAttribute("CP_TRV", iq.AddTranslation("International Travel Cover", English, "CPP", 0, Nothing, 0, False), 0)
        '       If Not iq.i_attribute_code.ContainsKey("CP_CVR") Then a = New clsAttribute("CP_CVR", iq.AddTranslation("Coverage", English, "CPP", , , False))
        If Not iq.i_attribute_code.ContainsKey("CP_ADP") Then a = New clsAttribute("CP_ADP", iq.AddTranslation("Accidental Damage Protection", English, "CPP", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("CP_RSP") Then a = New clsAttribute("CP_RSP", iq.AddTranslation("Response Time", English, "CPP", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("CP_CTR") Then a = New clsAttribute("CP_CTR", iq.AddTranslation("Call-to-Repair", English, "CPP", 0, Nothing, 0, False), 0) 'repair/response
        If Not iq.i_attribute_code.ContainsKey("CP_ONS") Then a = New clsAttribute("CP_ONS", iq.AddTranslation("On-Site", English, "CPP", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("CP_TRC") Then a = New clsAttribute("CP_TRC", iq.AddTranslation("Tracing", English, "CPP", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("CP_PST") Then a = New clsAttribute("CP_PST", iq.AddTranslation("Post Warranty", English, "CPP", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("CP_EXC") Then a = New clsAttribute("CP_EXC", iq.AddTranslation("Exchange", English, "CPP", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("CP_PAR") Then a = New clsAttribute("CP_PAR", iq.AddTranslation("Pickup and Return", English, "CPP", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("CP_RTD") Then a = New clsAttribute("CP_RTD", iq.AddTranslation("Return to Depot", English, "CPP", 0, Nothing, 0, False), 0)

        'CDMR 

        'for the ISS carepacks - The are 3 foreign key columns  - each becomes a SINGLE attribute  - with  many possible values (translations)  - (linkes to the products via productSttributes)

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()
        Dim rdr As SqlClient.SqlDataReader

        'Dim cs As List(Of String) 'service level codes
        'note placeholder comma (for a 1 based list)
        'cs = Split(",9x5,13x5,24x7,NBD,PnR,12x7,13x7,9x7,6CTR,24CTR,24x7x4,13x5x4,PAC,Colab,HWO,CDMR,DMR,NoDMR,I&S", ",").ToList

        Dim dicSLs As Dictionary(Of Integer, clsTranslation) = New Dictionary(Of Integer, clsTranslation)
        rdr = da.DBExecuteReader(con, "SELECT scode,slabel FROM h3.iq.products.carepack_serviceLevels")  'This is a terrible name as it contains not just service Levels but Response Times & DMR  info too

        Dim tl As clsTranslation
        Dim tokill As IEnumerable(Of clsTranslation)
        tokill = From j In iq.Translations.Values Where j.Group = "CPSL"
        For Each tl In tokill
            tl.delete(English)
        Next

        While rdr.Read
            tl = iq.AddTranslation(rdr.Item("slabel"), English, "CPSL", 0, Nothing, 0, False)  'these MUST NOT have an order on them as that takes precedence
            dicSLs.Add(CType(rdr.Item("scode"), Integer), tl)
        End While
        rdr.Close()

        'These three attributes represent the ISS columns (which are pointers to iq.products.carePack_ServiceLevels

        If Not iq.i_attribute_code.ContainsKey("ISS_SL") Then a = New clsAttribute("ISS_SL", iq.AddTranslation("Service Level", English, "CPSL", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("ISS_OP") Then a = New clsAttribute("ISS_OP", iq.AddTranslation("Options", English, "CPSL", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("ISS_RC") Then a = New clsAttribute("ISS_RC", iq.AddTranslation("Response Time", English, "CPSL", 0, Nothing, 0, False), 0)

        'delete any existing carepack productattributes
        da.DBExecutesql("DELETE FROM productAttribute WHERE fk_attribute_id IN (SELECT attribute.id FROM attribute JOIN translation t ON fk_translation_key_name = t.[key] WHERE [group]='CPP')")
        da.DBExecutesql("DELETE FROM productAttribute WHERE fk_attribute_id IN (SELECT attribute.id FROM attribute JOIN translation t ON fk_translation_key_name = t.[key] WHERE [group]='CPSL')")

        con = da.OpenDatabase()

        Dim pawc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")  'Allows a bulk insert -many many times faster than INSERTING the individual rows

        'OptTechnology and OptProvision rules are pauls random stuff
        Dim sql$ = "SELECT [OptSN] ,[OptSKU],[Description],[Duration],[DMR],servicelevel,[Travel],[OptTechnology],[OptProvisionRules],[ADP],[ResponseTime],[CTR],[OnSite],[Tracing],[PostWarranty]"
        sql$ &= ",[Exchange],[PickUpReturn],[ReturnToDepot],servicelevel_iss,Responsecode_iss,options_iss "
        sql$ &= "FROM h3.[iq].[Products].[CarePack_Properties]" ' join h3.iq.products.carepack_servicelevels sl on sl.scode = servicelevel"
        'sql$ &= "join h3.iq.products.carepack_servicelevels iss_sl on iss_sl.scode = iss_servicelevel"
        'sql$ &= "join h3.iq.products.carepack_servicelevels iss_sl on iss_sl.scode = iss_servicelevel"

        rdr = da.DBExecuteReader(con, sql$)
        Dim sku$
        Dim product As clsProduct

        Dim pa As clsProductAttribute

        tokill = From j In iq.Translations.Values Where j.Group = "CPSVCLVLS"
        For Each tl In tokill
            tl.delete(English)
        Next

        While rdr.Read
            sku$ = rdr.Item("optSku")
            If iq.i_SKU.ContainsKey(sku) Then
                product = iq.i_SKU(sku)

                'we will display these as CSS lozenges - using the ProductAttributes (text) Value as the lozenge text and the using the attributes Name as a tool tip
                If rdr.Item("PostWarranty") = False Then  'For now we're not importing post warranty care packs
                    If Not IsDBNull(rdr.Item("Onsite")) Then  'We also ignore rows where onsite is null
                        'These are the 'boolean' ones
                        'Pipe is the major seperator - Attribute code,Sql Column Name, and displaly text are comma seperated within that
                        Dim l$ = "CP_DMR,DMR,DMR|CP_TRV,Travel,Travel|CP_ADP,ADP,ADP|CP_CTR,CTR,CTR|CP_ONS,Onsite,On-Site|CP_TRC,Tracing,Trace|CP_PST,PostWarranty,Post|CP_EXC,Exchange,Exch|CP_PAR,PickUpReturn,PickUp|CP_RTD,ReturnToDepot,RTD"

                        Dim bits() As String
                        For Each one In Split(l$, "|")
                            bits = one.Split(",")
                            If Not rdr.Item(bits(1)) Is DBNull.Value Then
                                pa = New clsProductAttribute(product, bits(0), rdr.Item(bits(1)), "txt", bits(2), pawc, Nothing, 0) 'Their numeric value will be 1 or 0 and their text will be the short code
                            End If
                        Next

                        'duration
                        If Not IsDBNull(rdr.Item("duration")) Then
                            pa = New clsProductAttribute(product, "CP_DRN", rdr.Item("duration"), "year", rdr.Item("Duration") & " yr", pawc, Nothing, 0)
                        End If

                        'response time
                        If Not IsDBNull(rdr.Item("ResponseTime")) Then
                            pa = New clsProductAttribute(product, "CP_RSP", CSng(rdr.Item("ResponseTime")), "hour", rdr.Item("ResponseTime") & " hrs", pawc, Nothing, 0)
                        End If

                        'Servicelevel is NON Iss (i beleive)
                        'servicelevel_ISS (Hardware only, colaborative support, ProActive Care, installation and startup)
                        'responsecode_ISS (24x7, 6hr CTR, NBD, 13x5 4hr, 24hr CTR
                        'options_ISS (NO DMR, DMR, Comprehnsive DMR)
                        '"SL_ISS") Then a = New clsAttribute("SL_ISS", iq.AddTranslation("Service Level", English, "CPSL"))


                        'CDMR ???
                        For Each k In Split("servicelevel^CP_SVC,servicelevel_iss^ISS_SL,options_iss^ISS_OP,responsecode_iss^ISS_RC", ",")
                            Dim p() As String = Split(k, "^") 'get the IQ1 database column name (in iq.products.carePackProperties) and corresponding attribute code (for the attribute we've made)
                            If Not IsDBNull(rdr.Item(p(0))) Then
                                Dim fk As Integer = CInt(rdr.Item(p(0))) 'This is the foriegn key value from the column
                                'Make the ProductAttribute (attaching an instance and value of this attribute to this product)
                                pa = New clsProductAttribute(product, iq.i_attribute_code(p(1)), fk, iq.i_unit_code("txt"), dicSLs(fk), pawc)  'dicSLS carries the pre-prepared translations one for each FK target (from iq.products.carepack_servicelevels)
                            End If
                        Next
                    End If
                End If
            End If
        End While
        rdr.Close()

        Debug.Print(pawc.Rows.Count)
        da.BulkWrite(con, pawc, "[ProductAttribute]")
        con.Close()


    End Sub

    'Public Function HostPrices(forHostID As String, priceBand As String) As String

    '    'Obsoleted


    '    Dim con As SqlClient.SqlConnection
    '    con = da.OpenDatabase()

    '    Dim lastmilestone As Double

    '    'This DOES NOT get prices for clones... who's prices now derive from the parent - and use the margin of the enquiring customer (of the clone)

    '    Dim hostID As String
    '    Dim rdr As SqlClient.SqlDataReader
    '    Dim sql$


    '    'sql$ = "DELETE FROM [Price] WHERE Fk_variant_id IN "
    '    'sql$ &= "(SELECT ID FROM [variant] WHERE fk_channel_id_seller="
    '    'sql$ &= iq.i_channel_code(forHostID).ID & ")"

    '    '        For Each product In iq.Products.Values
    '    ' product.i_Variants()
    '    ' Next


    '    'da.DBExecutesql(sql$)

    '    'read all prices for some buyeraccount in the pricing database..
    '    'create/update variants
    '    'and make the stock and price records


    '    'sql$ = "SELECT h.hostid as seller,hostpartnum,hostmfrpartnum,internalprice AS iprice,externalPrice as ePrice, priceBand,c.currencyCode "
    '    'sql$ &= "FROM " & server$ & "[iq].products.pricelistmaster h "
    '    'sql$ &= "JOIN " & server$ & "[iq].dbo.currencies c ON h.curr=c.CurrencySymbol "
    '    ''sql$ &= "WHERE(h.priceBand Is null) "
    '    'sql$ &= "WHERE h.hostid='" & forHostID & "'"
    '    'sql$ &= " AND h.HostID NOT IN (SELECT subhost from " & server$ & "[channelcentral].customers.Host_Parents)" 'DON't get prices for clones !
    '    'If priceBand = "" Then
    '    '    sql$ &= " AND priceBand is null "
    '    'Else
    '    '    sql$ &= " AND priceBand='" & priceBand & "'"
    '    'End If
    '    'sql$ &= " GROUP BY h.HostID,hostpartnum,hostmfrpartnum,InternalPrice,ExternalPrice,priceBand,currencycode "
    '    'sql$ &= "ORDER BY HOSTPARTNUM,seller,priceBand,currencycode "


    '    sql$ = "SELECT ba.id as baid, c.ID as catid, p.price,c.HostPartNum,c.hostmfrpartnum FROM h1.pricing.pna.buyeraccount AS ba"
    '    sql$ &= "JOIN h1.pricing.pna.price AS p ON p.buyeraccount_id=ba.id "
    '    sql$ &= "JOIN h1.pricing.pna.cat AS c ON p.Cat_ID = c.id "
    '    sql$ &= "JOIN h3.channelcentral.customers.host_properties hp on hp.HID=ba.host_id "
    '    sql$ &= "WHERE hp.HostID='DAZRG248NE'"


    '    Dim PriceWriteCache As New DataTable
    '    PriceWriteCache = da.MakeWriteCacheFor(con, "Price")

    '    Dim svWriteCache As DataTable = da.MakeWriteCacheFor(con, "variant")


    '    rdr = da.DBExecuteReader(con, sql$)

    '    Dim part As clsProduct = Nothing
    '    'For each product - each seller offers prices to each buyer in each currency
    '    'Dim prices As Dictionary(Of clsChannel, Dictionary(Of clsChannel, Dictionary(Of String, Single)))

    '    Dim HostPartNo As String
    '    Dim oHostPartNo As String = ""

    '    'Dim BuyerChannel As clsChannel
    '    Dim SellerChannel As clsChannel
    '    'Dim Price As clsPrice
    '    Dim Currency As clsCurrency

    '    Dim partnos As Integer

    '    Dim pricesrows As Integer = 0

    '    Dim dupes As Integer = 0
    '    Dim Dupe As Boolean = False
    '    Dim newPrice As clsPrice
    '    Dim ExistingPrice As clsPrice

    '    Dim increased As Integer
    '    Dim decreased As Integer
    '    Dim same As Integer
    '    Dim newPrices As Integer

    '    Dim skipped As Integer

    '    Dim ok$ = "", k$ = ""

    '    While rdr.Read

    '        hostID = rdr.Item("seller")
    '        If rdr.Item("HOSTPARTNUM") Is DBNull.Value Then
    '            HostPartNo = Trim$(rdr.Item("hostmfrpartnum"))  'YUCK - westcoast
    '        Else
    '            HostPartNo = Trim$(rdr.Item("hostpartnum"))
    '        End If

    '        HostPartNo = Split(HostPartNo, "#")(0)  'trim any #suffix

    '        If HostPartNo <> oHostPartNo Then
    '            partnos += 1
    '            Dim mfrpartno = rdr.Item("HostMfrPartNum")
    '            mfrpartno = Split(mfrpartno, "#")(0)  'trim any #suffix
    '            If iq.i_SKU.ContainsKey(mfrpartno) Then
    '                part = iq.i_SKU(mfrpartno)
    '            Else
    '                Logit("Invalid SKU:" & mfrpartno)
    '                part = Nothing
    '            End If
    '        End If

    '        Dim hac As String
    '        If IsDBNull(rdr.Item("priceBand")) Then hac$ = "" Else hac = rdr.Item("priceBand")
    '        k$ = HostPartNo & "^" & hostID & "^" & hac$ & "^" & rdr.Item("currencycode")
    '        ok$ = k$

    '        Dim SKUvariant As clsVariant

    '        If part IsNot Nothing Then
    '            'logging moved to above so it only happens when the part changes
    '            If Not iq.i_channel_code.ContainsKey(Trim$(rdr.Item("seller"))) Then
    '                Logit("Couldn't locate the seller channel for '" & rdr.Item("seller") & "' (pricelistmaster.hostID) - check CaSe and trailing     spaces.")
    '            Else
    '                Currency = iq.i_currency_code(Trim$(rdr.Item("currencycode")))
    '                SellerChannel = iq.i_channel_code(Trim$(rdr.Item("seller")))

    '                'See if the host partnumber has a # in, determine the variant
    '                'Dim hostpartnum As String
    '                'hostpartnum = rdr.Item("Hostmfrpartnum")
    '                'Dim ih As Integer = InStr(hostpartnum, "#")

    '                'If ih Then
    '                '    Dim vc As String
    '                '    vc = Trim$(Mid$(hostpartnum, ih))
    '                '    If Not iq.i_variant_code.ContainsKey(vc) Then
    '                '        Dim newvariant As clsVariant
    '                '        newvariant = New clsVariant(vc, vc) 'for now - we use the code 'eg #ABA as the name  (see the variants table in the DB to translate)
    '                '    End If
    '                '    SKUvariant = iq.i_variant_code(vc)
    '                'Else
    '                '    SKUvariant = iq.StandardVariant
    '                'End If

    '                Dim distiSKU As String = NothingFromNull(rdr.Item("HOSTpartnUM")) 'this is this hostpartnumber
    '                'SellerChannel.Variants(distiSKU)iq.i_variant_hostSKU(SellerChannel.ID & "^" & distiSKU))
    '                SKUvariant = New clsVariant("", part, SellerChannel, distiSKU, "", "", "", Nothing, False, svWriteCache)

    '                'see if we already have a price for this part - for this seller/currency/variant combo
    '                'we may well do as PricelistMaster contains rows for many buyers

    '                Dim buyerChannel As clsChannel = Nothing
    '                Dim price As Decimal
    '                If IsDBNull(rdr.Item("priceBand")) Then
    '                    'Internal Price
    '                    buyerChannel = Everyone
    '                    If IsDBNull(rdr.Item("iprice")) Then
    '                        price = -1
    '                    Else
    '                        price = rdr.Item("iprice")
    '                    End If
    '                Else
    '                    'the accounts hold an index to the (buyer)channel/priceBand (of that buyer)
    '                    If iq.i_Account_HostIDpriceBand.ContainsKey(hostID & "^" & rdr.Item("priceBand")) Then
    '                        buyerChannel = iq.i_Account_HostIDpriceBand(hostID & "^" & rdr.Item("priceBand")).BuyerChannel
    '                        If IsDBNull(rdr.Item("eprice")) Then
    '                            price = -1
    '                        Else
    '                            price = rdr.Item("eprice")
    '                        End If
    '                    Else
    '                        price = -1  'missing account info
    '                    End If
    '                End If

    '                Dupe = False

    '                If price = -1 Then
    '                    'null price (synnex and a few unhosted)
    '                    skipped += 1
    '                Else
    '                    If SKUvariant.PriceExists(buyerChannel, Currency) Then
    '                        'update
    '                        Dim existingPrices As List(Of clsPrice)
    '                        existingPrices = part.Prices(SellerChannel, buyerChannel, Currency, SKUvariant)

    '                        ExistingPrice = existingPrices(0)

    '                        If ExistingPrice.ID = -1 Then
    '                            dupes += 1
    '                        Else
    '                            If price = ExistingPrice.Price.value Then
    '                                same = same + 1
    '                                If Math.Abs(DateDiff(DateInterval.Day, Now, ExistingPrice.DateStamp)) > 6 Then ExistingPrice.Update() 'touch any file more than 7 days old
    '                            ElseIf price > ExistingPrice.Price.value Then
    '                                increased += 1
    '                                ExistingPrice.Update() 'slow
    '                            Else
    '                                decreased += 1
    '                                ExistingPrice.Update() 'slow
    '                            End If
    '                        End If
    '                    Else
    '                        'add
    '                        newPrice = New clsPrice(SKUvariant, buyerChannel, New NullablePrice(CDec(rdr.Item("iprice")), Currency), "zImport", PriceWriteCache)
    '                        newPrices += 1
    '                    End If
    '                End If
    '            End If
    '        End If

    '        oHostPartNo = HostPartNo
    '        pricesrows += 1
    '    End While

    '    rdr.Close()

    '    da.BulkWrite(con, PriceWriteCache, "Price")
    '    PriceWriteCache = Nothing

    '    con.Close()

    '    HostPrices = "Processed " & pricesrows & " pricelistmaster rows. " & _
    '        skipped & " prices were skipped (null), " & _
    '        increased & " prices increased," & _
    '        decreased & " prices decreased," & _
    '        same & " prices were the same. " & _
    '        newPrices & " prices were added. " & _
    '        "Total time:" & TimeSince(lastmilestone)

    '    Logit(HostPrices)

    '    Logit("Wrote them to database (BulkCopy) in " & TimeSince(lastmilestone))


    'End Function


    Public Function FIOfocus() As Integer

        Dim con As SqlClient.SqlConnection = da.OpenDatabase
        Dim rdr As SqlClient.SqlDataReader


        Dim pawc As DataTable = da.MakeWriteCacheFor(con, "ProductAttribute")

        rdr = da.DBExecuteReader(con, "SELECT Optsku,fio from h3.iq.products.options")

        Dim focusAtt As clsAttribute = iq.i_attribute_code("focus")
        Dim fioTl As clsTranslation = iq.AddTranslation("FIO", English, "Foci", 0, Nothing, 0, False)


        While rdr.Read
            If rdr.Item("fio") <> 0 Then
                Dim sku As String = rdr.Item("optsku")

                If iq.i_SKU.ContainsKey(sku) Then

                    Dim optionProd As clsProduct = iq.i_SKU(sku)
                    'make an FIO focus attribute
                    Dim fa As clsProductAttribute = New clsProductAttribute(optionProd, focusAtt, 1, iq.i_unit_code("txt"), fioTl, pawc)
                    FIOfocus += 1
                End If
            End If
        End While

        rdr.Close()

        da.BulkWrite(con, pawc, "ProductAttribute")

        con.Close()

    End Function





    Public Function FIOs(con As SqlClient.SqlConnection, Optional opt As String = Nothing, Optional sys As String = Nothing) As Dictionary(Of String, Dictionary(Of String, Integer))

        'Returns a dictionary (by system mfrSKU) of the quantities and part numbers of all factory installed components (PriStor, sec stor CPU, MEM etc)
        'for each system
        'Does NOT return the Qmax, Or Increments - as these come from optionLimits
        'which despite the name is much LESS specific (a better name would be optionTypeLimitsPerFamily !)

        FIOs = New Dictionary(Of String, Dictionary(Of String, Integer))(StringComparer.CurrentCultureIgnoreCase)

        ' [CPU],[CPUqty],[RAM],[RAMqty],[Comms/Controllers/Other],[PriStor],[PriStorQty],[SecStor],[SecStorQty]
        ',[TerStor],[terstorqty],[RAID],[RAIDtech],[RAIDcache],[VGA],[PSU],[PSUqty],[Extras],[Software],[IntVideo],[Display],[WarrantyCode],[TextInt]
        ',[SupplyChainCode],[EOL],[Active],[TerStorQty],[TerStorTech],[SysType2],[Options],[WLAN],[WWAN],[EnergyStar],[DiscreteGraphics]

        ' 	FormFactor	MfrBuildCode	ActiveSites	ActiveFromDate	AlsoHost	IntVideo	PriStor	PriStorQty	SecStor	SecStorQty	TerStor	TerStorQty	TerStorTech	RAID	RAIDcache	RAIDtech	VGA	PSU	PSUqty	Extras	Software	Options	WarrantyCode	WeightUnboxed	TextInt	Active	EOL	ActiveToDate	EnergyStar	DiscreteGraphics	ILOhardware	ILOlicense	ICEincluded	AvalancheSystem	ProductNote

        'outstanding
        '[RAIDtech],[VGA],[Extras],[Software],[IntVideo],[Display],[WarrantyCode],[TextInt]
        ',[SupplyChainCode],[EOL],[Active],[TerStorTech],[SysType2],[Options],[WLAN],[WWAN],[EnergyStar],[DiscreteGraphics]

        'the 'technology' (of terstore,Raid) - should be an attribute of the product they point to
        'Display DIS_size_abbrev_wxh_AG

        'intVideo and discreteGrapics are abbreviations -  become attributes in import(systems)

        'Extras and software are both a 'mix' of abbreviations and part numbers

        Dim rdr As SqlClient.SqlDataReader
        Dim sql$

        'IloHardware is an abbreviation (handled in import.systems)

        sql$ = "SELECT sysType,modelSKU,cpu,cpuqty,ram,ramqty,pristor,pristorqty,secstor,secstorqty,terstor,terstorqty,raid,raidcache,psu,psuqty,iloLicense,iloHardware,iceIncluded,"
        sql$ &= "WLAN,WWAN,[controllers],extras,software,discreteGraphics"
        sql$ &= " FROM " & server$ & "[iq].products.union_systems "
        'If opt IsNot Nothing Then
        '    sql &= "WHERE '%' + cpu + '%' like '%' + " & opt & " + '%' or '%' + ram + '%' like '%' + " & opt & " + '%' or '%' + pristor + '%' like '%' + " & opt & " + '%' or '%' + secstor + '%' like '%' + " & opt & " + '%' or '%' + terstor + '%' like '%' + " & opt & " + '%' or '%' + raid + '%' like '%' + " & opt & " + '%' or '%' + raidcache + '%' like '%' + " & opt & " + '%' or '%' + psu + '%' like '%' + " & opt & " + '%' or '%' + ilolicense + '%' like '%' + " & opt & " + '%' or '%' + ilohardware + '%' like '%' + " & opt & " + '%' or '%' + software + '%' like '%' + " & opt & " + '%'"
        'End If
        If sys IsNot Nothing Then
            If opt Is Nothing Then sql &= " AND "
            sql &= " where modelsku IN (" & sys & ")"
        End If
        sql$ &= " ORDER BY modelsku"
        rdr = da.DBExecuteReader(con, sql$)



        Dim errs As Integer

        Dim nex As Integer

        Dim sysSKU As String
        sysSKU = ""
        Dim dic As Dictionary(Of String, Integer) = Nothing 'this is the inner dictionary for one system (of all it's FIO's and their qtys)
        While rdr.Read
            If rdr.Item("ModelSKU") = "JE074A" Then 'can be removed
                Dim a = 1
            End If
            If LCase(Left$(rdr.Item("ModelSKU"), 1)) <> "x" Then ' do not import systems begining with X they are 'fake'
                If rdr.Item("modelsku") <> sysSKU Then
                    'we're onto the next system
                    dic = New Dictionary(Of String, Integer)(StringComparer.CurrentCultureIgnoreCase)
                    FIOs.Add(rdr.Item("modelsku"), dic)
                    sysSKU = rdr.Item("modelsku")
                    '     If sysSKU = "668812-421" Then Stop 'this should have 1 * 647893-B21  - for ram 

                End If


                'each of these columns (may) contain a part number - and has a corresponsing 'qty' column
                For Each Thing In Split("cpu,ram,pristor,secstor,terstor,psu", ",")

                    If Not IsDBNull(rdr.Item(Thing)) Then
                        Dim pn As String = rdr.Item(Thing) 'part number (for tis families CPU, HDD, DVDROM etc
                        If IsDBNull(rdr.Item(Thing + "qty")) Then  'there were a few parts with null qty columns
                            If Not dic.ContainsKey(pn) Then
                                dic.Add(pn, -1)  '
                            Else
                                dic(pn) = -1 'more than one option for this system both with Null qtys
                            End If
                        Else
                            ' If sysSKU = "675421-421" And pn = "675450-B21" Then Stop

                            If dic.ContainsKey(pn) Then
                                If errs < 100 Then
                                    Logit(sysSKU & " has " & pn & " as its " & Thing & " Which appears in another column (pirstor,secstor,terstor,psu or ram - see iq.products,union_systems (first 100 occurances logged)")
                                End If
                                errs += 1

                                dic(pn) += rdr.Item(Thing + "qty")
                            Else
                                Dim qty As Integer = rdr.Item(Thing + "qty")
                                If Not iq.i_SKU.ContainsKey(pn) Then nex += 1
                                dic.Add(pn, qty)  'add the part number and a quantity   - we don't record the option type... just that it is an (installed) option
                            End If
                        End If
                    End If
                Next

                'each of these columns (may) contain a part number - these are the 'quantityliess' ones
                For Each one In Split("raid,raidcache,wlan,wwan,ilolicense,ilohardware,iceIncluded,discreteGraphics,software", ",")
                    If Not IsDBNull(rdr.Item(one)) Then
                        If Not dic.ContainsKey(rdr.Item(one)) Then  'Some of the storage devices have a chache controller and CPU as the same sku - which was tripping this up
                            For Each s In Split(rdr.Item(one), ",")
                                If Not iq.i_SKU.ContainsKey(s) Then nex += 1
                                dic.Add(s, 1)  'add the part number and a quantity   - we don't record the option type... just that it is an (installed) option
                            Next

                        End If
                    End If
                Next
            End If
        End While
        rdr.Close()

        Debug.Print("there are " & nex & " nonexistent FIOs")

    End Function


    Public Function loadDic(con As SqlClient.SqlConnection, dicIQ2 As Object, dicCode As String) As Object


        'Dictionary(Of String, Object)


        'Reads all the rows of type dicCode from the IQ2 IMPORTDics table to 
        'RETURN a dictionary which resolves some string Key (in IQ1)  (a row ID, partnumber, or sometimes a compound key made by concatenating fields)
        'to some IQ2 Object (pulled from dicIQ2) - for exampole and IQ1 HostID to IQ2 Channel object


        'This bit was VERY hard.. creates (using reflection) a dictionary of string>correct IQ2 object

        Dim d1 As Type = dicIQ2.GetType.GetGenericTypeDefinition
        Dim typeargs() As Type = d1.GetGenericArguments

        typeargs(0) = GetType(String) 'Our new dictionary will *always* have a string key
        Dim IQdicttypes() As Type = dicIQ2.GetType.GetGenericArguments 'gets the types of the Key and Value in the IQ2 dictionary

        typeargs(1) = IQdicttypes(1) 'make the new dictionary have VALUES of the same type (class) as the IQ2 dictionary's values

        'This (convoluted) bit , createas a dictionary with a case insensitive key - so we can copy that argument.. (2).. and create similarly case insensitive dics
        'Dim adic As New Dictionary(Of String, Object)(StringComparer.CurrentCultureIgnoreCase)
        'ReDim Preserve typeargs(2)
        'Dim adicargs() As Type = dicIQ2.GetType.GetGenericArguments
        'typeargs(2) = adicargs(2)

        Dim d2 As Type = GetType(Dictionary(Of ,)) 'we'll be constucting a '2D' dictionary...

        Dim constructed As Type = d2.MakeGenericType(typeargs)

        loadDic = Activator.CreateInstance(constructed)  'Create it

        'Dim AnEvent As clsEvent = Nothing
        'If parentEvent IsNot Nothing Then
        '    AnEvent = New clsEvent(parentEvent, "", ev_Info)
        'End If
        'con.Close()
        'con = da.OpenDatabase()
        Dim sql$
        Dim rdr As SqlClient.SqlDataReader
        sql$ = "Select [key],id,dicCode from importDics where dicCode ='" & dicCode & "';"

        rdr = da.DBExecuteReader(con, sql$)

        Dim count As Integer = 0
        While rdr.Read
            If dicIQ2 Is Nothing Then
                loadDic.Add(rdr.Item("key"), rdr.Item("id"))  'If no target dictionary is specified - it's assumed to be a dictionary of integer keys
            Else
                Dim id As String = rdr.Item("id")
                If id <> "-1" Then
                    loadDic.Add(rdr.Item("key"), dicIQ2(rdr.Item("id")))  '<<Here's where it all happens
                    count += 1
                End If

            End If

        End While
        rdr.Close()


    End Function

    Public Sub saveDic(con As SqlClient.SqlConnection, Dic As Object, dicCode As String)

        da.DBExecutesql("DELETE FROM importDics WHERE diccode='" & dicCode & "';")

        Dim WriteCache As New DataTable
        WriteCache = da.MakeWriteCacheFor(con, "ImportDics")

        Dim row As System.Data.DataRow

        For Each k In Dic.keys
            row = WriteCache.NewRow()

            row("key") = k  'this is the original key or code in IQ1
            If dicCode = "sysDesc" Or dicCode = "sysOS" Then
                row("id") = Dic(k).key 'record translation onjects have keys not ID's
            Else
                row("id") = Dic(k).id 'record the ID of every object
            End If

            row("dicCode") = dicCode
            WriteCache.Rows.Add(row)
        Next

        da.BulkWrite(con, WriteCache, "ImportDics")
        WriteCache = Nothing

    End Sub

    Public Sub writeDicOptLimits(dic As Dictionary(Of String, clsLimit), filename$)

        Dim sw As New IO.StreamWriter(filename$, False)

        For Each kvp In dic
            sw.WriteLine(kvp.Key & "|In:" & kvp.Value.Qinstalled & " Mn" & kvp.Value.Qmin & " Mx" & kvp.Value.Qmax)
        Next

        sw.Close()


    End Sub
    Public Sub WriteDicFIOs(dicfios As Dictionary(Of String, Dictionary(Of String, Integer)), filename$)

        Dim sw As New IO.StreamWriter(filename$, False)

        Dim errs As Integer = 0

        Dim msys As Integer = 0
        For Each SystemSKU In dicfios.Keys
            If Left$(SystemSKU, 3) <> "###" Then

                Dim sysname$ = ""
                If iq.i_SKU.ContainsKey(SystemSKU) Then
                    sysname = iq.i_SKU(SystemSKU).DisplayName(English)
                Else
                    sysname = "Missing System SKU ???"
                    msys += 1
                End If

                sw.WriteLine("System:'" & SystemSKU & "' " & sysname$)
                For Each OptionSKU In dicfios(SystemSKU)
                    If iq.i_SKU.ContainsKey(OptionSKU.Key) Then
                        sw.WriteLine("  Option:'" & OptionSKU.Key & "' " & iq.i_SKU(OptionSKU.Key).DisplayName(English) & " QTY:" & OptionSKU.Value)
                    Else
                        sw.WriteLine("  Missing Option SKU:'" & OptionSKU.Key & "'")
                        errs += 1  'these arent really errors - they were just abbreviations (not part numbers) in some og the option columns
                    End If
                Next
            End If

        Next

        sw.Close()

        '    If errs Then Stop

    End Sub

    'If UCase(optTypeKey) = "CPU" Then
    '    'refactor the CPU branch - only one CPU is an option per system - we just create and graft one branch for it (rather than pruning many non-options)
    '    For Each SupplyChain In dicFamily(sysfamkey).childBranches.Values
    '        For Each systemBranch In SupplyChain.childBranches.Values

    '            '                                    If systemBranch.Product.Attributes("MfrSKU").Translation.Text(English) = "666157-B21" Then Stop

    '            If systemBranch.Product.isSystem Then  'IMPORTANT
    '                'Dim cpuBranch As New clsBranch(dicOptTypeProd("cpu"), Systembranch,)
    '                'Make the placholder branch "CPU"
    '                Dim cpuBranch As New clsBranch(Nothing, systemBranch, dicOptType("cpu").Translation, "", optTrans, optTransSingular, Nothing, branchWriteCache, nextBranchID)
    '                If systemBranch.Product.i_attributes_code.containskey("cpuSKU") Then
    '                    Dim cpusku As String = systemBranch.Product.i_attributes_code("cpuSKU").Translation.text(s_lang)
    '                    If Not optionsbysku.ContainsKey(cpusku) Then
    '                        Logit("cpu " & cpusku & " is not an option")  'Many systems don't have their processor as an option (becuase they are already fully populated, or are single CPU)
    '                    Else
    '                        product = optionsbysku(cpusku)
    '                        Dim cpu As New clsBranch(product, cpuBranch, product.i_attributes_code("~ame").Translation, "", optTrans, optTransSingular, Nothing, branchWriteCache, nextBranchID)

    '                        'need a quantity of cpu's  - 

    '                    End If
    '                Else
    '                    Logit("System:" & systemBranch.Name & " has no processor")
    '                End If
    '            End If
    '        Next
    '    Next
    'Else
    '(as they contain a different set of options in each family (although the same in each system))


    Public Function quoteStates(con As SqlClient.SqlConnection) As Dictionary(Of String, clsState)

        'STATES
        quoteStates = New Dictionary(Of String, clsState)(StringComparer.CurrentCultureIgnoreCase)

        Dim aState As clsState

        Dim rdr As SqlClient.SqlDataReader
        rdr = da.DBExecuteReader(con, "SELECT statuscode,statustext FROM " & server$ & "iq.quote.statuscodes")

        Dim code$

        Dim order As Integer = 10

        While rdr.Read
            code$ = Trim$(rdr.Item("statusCode"))
            aState = New clsState("QT", code, iq.AddTranslation(Trim$(rdr.Item("statustext")), English, "QS", 0, Nothing, 0, False), order, "#a0a0a0")
            order += 10
            quoteStates.Add(code, aState)
        End While
        rdr.Close()

    End Function



    Private Sub AddAttribute(ColName As String, rdr As SqlDataReader, product As clsProduct)
        Dim Attribute As clsAttribute
        Dim productAttribute As clsProductAttribute

        If Not iq.i_attribute_code.ContainsKey(ColName) Then
            Attribute = New clsAttribute(ColName, iq.AddTranslation(ColName, English, "", 0, Nothing, 0, False), 0) 'i'll let you off this one
        End If

        Attribute = iq.i_attribute_code(ColName)

        If Not IsDBNull(rdr.Item(ColName)) Then
            Dim tlcn As clsTranslation = iq.AddTranslation(rdr.Item(ColName), English, "attrib", 0, Nothing, 0, True)
            productAttribute = New clsProductAttribute(product, Attribute, 0, iq.i_unit_code("txt"), tlcn, Nothing)
        End If

    End Sub
    Public Sub MLCarePacks()
        ' Delete care packs 



    End Sub

    Public Sub CarePacks()

        '

        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader
        Dim query As String = "select distinct CPKpartnum from  datastore.products.carepacks"

        Dim systemSku As String = String.Empty
        Dim optionSku As String = String.Empty
        rdr = da.DBExecuteReader(con, query)
        Dim cptl, cpstl As clsTranslation
        cptl = iq.AddTranslation("Carepack", English, "collect", 0, Nothing, 0, False)
        cpstl = iq.AddTranslation("Carepacks", English, "collect", 0, Nothing, 0, False)
        Dim carePackBranch As clsBranch
        Dim carePackRoot As clsBranch = New clsBranch(Nothing, Nothing, cpstl, "", cpstl, cptl, iq.i_screens_code("Care"), 1, False, "B")
        Dim noProducts As Integer
        While rdr.Read
            Dim carepackPart As String = rdr.Item("CPKpartnum").ToString()
            If iq.i_SKU.ContainsKey(carepackPart) Then
                Dim carepackProduct As clsProduct = iq.i_SKU(carepackPart)
                Dim cpqBranch = iq.i_SKU(rdr("CPKpartnum")).Branches.First
                Dim carepackTrans As clsTranslation = iq.AddTranslation(rdr.Item("CPKpartnum"), English, "CPSKUS", 0, Nothing, 0, False)
                carePackBranch = New clsBranch(carepackProduct, carePackRoot, carepackTrans, "", cpstl, cptl, Nothing, 1, False, "B")

            Else
                ' if we cant find products 
                noProducts += 1

            End If
        End While
    End Sub

    '
    Public Sub ImportQuickSpecsInc(product As clsProduct, famName As String, Inserting As Boolean, AttributeCache As DataTable)

        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader
        Dim query As String = "select [SysFamilyName],[docType],[docURL],[URLexists] FROM [iQ].[products].[SupportDocs] WHERE SysFamilyName='" & famName & "'"

        rdr = da.DBExecuteReader(con, query)

        If Not iq.i_attribute_code.ContainsKey("Document Links") Then
            Dim quickspecAttribute As clsAttribute
            quickspecAttribute = New clsAttribute("Document Links", iq.AddTranslation("Document Links", English, "", 0, Nothing, 0, False), 0)
        End If

        Dim dt As DataTable = New DataTable()
        dt.Load(rdr)

        Dim linkAttributes As String = String.Empty
        For Each row As DataRow In dt.Rows
            Dim attributeName As String = row("docType")
            linkAttributes += "<a href =""" & row("docUrl") & """  target=""_blank"">" & attributeName & "</a>  "
        Next

        Dim fn As clsProductAttribute
        fn = New clsProductAttribute(product, iq.i_attribute_code("Document Links"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "QS", 0, Nothing, 0, False), AttributeCache, Not Inserting)

        con.Close()


    End Sub


    Public Sub ImportQuickSpecs()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader
        Dim query As String = "select [SysFamilyName],[docType],[docURL],[URLexists] FROM [iQ].[products].[SupportDocs]"

        rdr = da.DBExecuteReader(con, query)

        If Not iq.i_attribute_code.ContainsKey("Document Links") Then
            Dim quickspecAttribute As clsAttribute
            quickspecAttribute = New clsAttribute("Document Links", iq.AddTranslation("Document Links", English, "", 0, Nothing, 0, False), 0)
        End If

        Dim dt As DataTable = New DataTable()
        dt.Load(rdr)

        Dim systemSku As String = String.Empty
        Dim optionSku As String = String.Empty
        For Each product In iq.Products.Values
            Dim perf As List(Of clsProductAttribute) = Nothing
            product.i_Attributes_Code.TryGetValue("FamMajor", perf)
            If perf IsNot Nothing Then
                Dim prdAttribute As clsProductAttribute = perf(0)
                Dim valueAttribute As String = prdAttribute.Translation.text(English)
                Dim drarray() As DataRow
                Dim filterExp As String = "SysFamilyName = '" & Trim(valueAttribute) & "'"
                drarray = dt.Select(filterExp)

                If drarray.Length > 0 Then
                    Dim linkAttributes As String = String.Empty
                    For Each row As DataRow In drarray
                        Dim attributeName As String = row("docType")
                        linkAttributes += "<a href =""" & row("docUrl") & """  target=""_blank"">" & attributeName & "</a>  "
                    Next

                    Dim fn As clsProductAttribute
                    fn = New clsProductAttribute(product, iq.i_attribute_code("Document Links"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "QS", 0, Nothing, 0, False))

                End If

            End If

        Next

        'While rdr.Read
        '    Dim carepackPart As String = rdr.Item("CPKpartnum").ToString()
        '    If iq.i_SKU.ContainsKey(carepackPart) Then
        '        Dim carepackProduct As clsProduct = iq.i_SKU(carepackPart)
        '        Dim carepackTrans As clsTranslation = iq.AddTranslation(rdr.Item("CPKpartnum"), English)
        '        carePackBranch = New clsBranch(carepackProduct, carePackRoot, carepackTrans, "", cpstl, cptl, Nothing, 1, False, "B")

        '    Else
        '        ' if we cant find products 
        '        noProducts += 1

        '    End If
        'End While
    End Sub
    'ML - integrated into systeinc
    Public Sub Extras()
        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader
        Dim query As String = "SELECT  [SystemType],[ModelSKU],[FamilyCode],[Extras],[Options]  FROM [iQ].[products].[Systems] where (Extras is not null or Options is not null)"

        rdr = da.DBExecuteReader(con, query)

        If Not iq.i_attribute_code.ContainsKey("Also included") Then
            Dim quickspecAttribute As clsAttribute
            quickspecAttribute = New clsAttribute("Also included", iq.AddTranslation("Also included", English, "", 0, Nothing, 0, False), 0)
        End If
        If Not iq.i_attribute_code.ContainsKey("Options") Then
            Dim quickspecAttribute As clsAttribute
            quickspecAttribute = New clsAttribute("Options", iq.AddTranslation("Options", English, "", 0, Nothing, 0, False), 0)
        End If

        Dim dt As DataTable = New DataTable()
        dt.Load(rdr)

        Dim systemSku As String = String.Empty
        Dim optionSku As String = String.Empty
        For Each product In iq.Products.Values
            Dim perf As List(Of clsProductAttribute) = Nothing
            product.i_Attributes_Code.TryGetValue("Also included", perf)
            If perf Is Nothing Then
                Dim drarray() As DataRow
                Dim filterExp As String = "ModelSKU = '" & product.SKU & "'"
                drarray = dt.Select(filterExp)

                If drarray.Length > 0 Then
                    Dim fn As clsProductAttribute
                    Dim linkAttributes As String = String.Empty
                    For Each row As DataRow In drarray
                        If row("Extras") IsNot DBNull.Value Then
                            If (iq.i_SKU.ContainsKey(row("Extras"))) Then
                                Dim prodAlsoIncuded As clsProduct = iq.i_SKU(row("Extras"))
                                If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                    fn = New clsProductAttribute(product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation)
                                    '  ElseIf prodAlsoIncuded.i_Attributes_Code.ContainsKey("~ame") Then
                                    '     fn = New clsProductAttribute(product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("~ame")(0).Translation)
                                End If
                            Else
                                linkAttributes += row("Extras")
                                fn = New clsProductAttribute(product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "extras", 0, Nothing, 0, False))
                            End If
                        End If
                    Next
                End If

            End If
            perf = Nothing
            product.i_Attributes_Code.TryGetValue("Options", perf)
            If perf Is Nothing Then
                Dim drarray() As DataRow
                Dim filterExp As String = "ModelSKU = '" & product.SKU & "'"
                drarray = dt.Select(filterExp)

                If drarray.Length > 0 Then
                    Dim fn As clsProductAttribute
                    Dim linkAttributes As String = String.Empty
                    For Each row As DataRow In drarray
                        If row("Options") IsNot DBNull.Value Then
                            If (iq.i_SKU.ContainsKey(row("Options"))) Then
                                Dim prodAlsoIncuded As clsProduct = iq.i_SKU(row("Options"))
                                If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                    fn = New clsProductAttribute(product, iq.i_attribute_code("Options"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation)
                                    '  ElseIf prodAlsoIncuded.i_Attributes_Code.ContainsKey("~ame") Then
                                    '     fn = New clsProductAttribute(product, iq.i_attribute_code("Also included"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("~ame")(0).Translation)
                                End If
                            Else
                                linkAttributes += row("Options")
                                fn = New clsProductAttribute(product, iq.i_attribute_code("Options"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "Options", 0, Nothing, 0, False))
                            End If
                        End If
                    Next
                End If

            End If
        Next


    End Sub
    'ML - integrated into systeinc
    Public Sub Graphics()
        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader
        Dim query As String = "SELECT  distinct [SystemType],[ModelSKU],[DiscreteGraphics],[IntVideo],[InstVGA] FROM [iQ].[products].[Systems] s inner join [iQ].[products].[SysFamilyDefinitions] sf on s.FamilyCode = sf.sysfamily "

        rdr = da.DBExecuteReader(con, query)

        If Not iq.i_attribute_code.ContainsKey("Graphics") Then
            Dim quickspecAttribute As clsAttribute
            quickspecAttribute = New clsAttribute("Graphics", iq.AddTranslation("Graphics", English, "", 0, Nothing, 0, False), 0)
        End If

        Dim dt As DataTable = New DataTable()
        dt.Load(rdr)

        Dim systemSku As String = String.Empty
        Dim optionSku As String = String.Empty
        For Each product In iq.Products.Values

            Dim perf As List(Of clsProductAttribute) = Nothing
            product.i_Attributes_Code.TryGetValue("Graphics", perf)
            If perf Is Nothing Then
                Dim drarray() As DataRow
                Dim filterExp As String = "ModelSKU = '" & product.SKU & "'"
                drarray = dt.Select(filterExp)

                If drarray.Length > 0 Then
                    Dim linkAttributes As String = String.Empty
                    For Each row As DataRow In drarray
                        If row("DiscreteGraphics") IsNot Nothing AndAlso row("DiscreteGraphics") IsNot DBNull.Value Then
                            linkAttributes += row("DiscreteGraphics")
                        ElseIf row("IntVideo") IsNot Nothing AndAlso row("IntVideo") IsNot DBNull.Value Then
                            linkAttributes += row("IntVideo")
                        ElseIf row("InstVGA") IsNot Nothing AndAlso row("InstVGA") IsNot DBNull.Value Then
                            linkAttributes += row("InstVGA")
                        End If
                    Next
                    Dim fn As clsProductAttribute
                    fn = New clsProductAttribute(product, iq.i_attribute_code("Graphics"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "VID", 1, Nothing, 0, False))
                End If

            End If

        Next

    End Sub
    'ML 23/01/2015 added to incremental import
    Public Sub Networking()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader
        Dim query As String = " SELECT  distinct [SystemType],[ModelSKU],[WLAN],[WWAN],[InstNIC] FROM [iQ].[products].[Systems] s inner join [iQ].[products].[SysFamilyDefinitions] sf on s.FamilyCode = sf.sysfamily  "

        rdr = da.DBExecuteReader(con, query)

        If Not iq.i_attribute_code.ContainsKey("Networking") Then
            Dim nwa As clsAttribute
            nwa = New clsAttribute("Networking", iq.AddTranslation("Networking", English, "", 0, Nothing, 0, False), 1)
        End If

        Dim dt As DataTable = New DataTable()
        dt.Load(rdr)
        Dim errorMessages As List(Of String) = New List(Of String)()
        Dim systemSku As String = String.Empty
        Dim optionSku As String = String.Empty
        For Each product In iq.Products.Values

            If product.SKU = "C5A73ET" Then
                Dim a = 9
            End If
            Dim perf As List(Of clsProductAttribute) = Nothing
            product.i_Attributes_Code.TryGetValue("Networking", perf)
            '  If perf Is Nothing Then
            Dim drarray() As DataRow
            Dim filterExp As String = "ModelSKU = '" & product.SKU & "'"
            drarray = dt.Select(filterExp)

            If drarray.Length > 0 Then
                Dim linkAttributes As String = String.Empty
                For Each row As DataRow In drarray
                    If row("WLAN") IsNot Nothing AndAlso row("WLAN") IsNot DBNull.Value Then
                        If (iq.i_SKU.ContainsKey(row("WLAN"))) Then
                            Dim fn As clsProductAttribute
                            Dim prodAlsoIncuded As clsProduct = iq.i_SKU(row("WLAN"))
                            If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                Dim found = False
                                If product.i_Attributes_Code.ContainsKey("Networking") Then
                                    For Each a In product.i_Attributes_Code("Networking")
                                        If a.Translation.text(English) = Left(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English), Len(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)) - 2) Then
                                            found = True
                                            a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)
                                            a.Translation.Update(English)
                                        End If
                                        If a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English) Then found = True
                                    Next
                                End If
                                If Not found Then fn = New clsProductAttribute(product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation)
                            End If
                        Else
                            linkAttributes += row("WLAN") & ", "
                        End If

                    End If
                    If row("WWAN") IsNot Nothing AndAlso row("WWAN") IsNot DBNull.Value Then
                        If (iq.i_SKU.ContainsKey(row("WWAN"))) Then
                            Dim fn As clsProductAttribute
                            Dim prodAlsoIncuded As clsProduct = iq.i_SKU(row("WWAN"))
                            If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                Dim found = False
                                If product.i_Attributes_Code.ContainsKey("Networking") Then
                                    For Each a In product.i_Attributes_Code("Networking")
                                        If a.Translation.text(English) = Left(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English), Len(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)) - 2) Then
                                            found = True
                                            a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)
                                            a.Translation.Update(English)
                                        End If
                                        If a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English) Then found = True
                                    Next
                                End If
                                If Not found Then fn = New clsProductAttribute(product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation)
                            End If
                        Else
                            linkAttributes += row("WWAN") & ", "
                        End If

                    End If
                    If row("InstNIC") IsNot Nothing AndAlso row("InstNIC") IsNot DBNull.Value Then
                        If (iq.i_SKU.ContainsKey(row("InstNIC"))) Then
                            Dim fn As clsProductAttribute
                            Dim prodAlsoIncuded As clsProduct = iq.i_SKU(row("InstNIC"))
                            If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                Dim found = False
                                If product.i_Attributes_Code.ContainsKey("Networking") Then
                                    For Each a In product.i_Attributes_Code("Networking")
                                        If a.Translation.text(English) = Left(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English), Len(prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)) - 2) Then
                                            found = True
                                            a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English)
                                            a.Translation.Update(English)
                                        End If
                                        If a.Translation.text(English) = prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation.text(English) Then found = True
                                    Next
                                End If
                                If Not found Then fn = New clsProductAttribute(product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation)
                            End If
                        Else
                            linkAttributes += row("InstNIC")
                        End If
                    End If
                Next

                If linkAttributes.Length > 0 Then
                    'linkAttributes = Left(linkAttributes, Len(linkAttributes))
                    Dim fn As clsProductAttribute
                    Dim found = False
                    If product.i_Attributes_Code.ContainsKey("Networking") Then
                        For Each a In product.i_Attributes_Code("Networking")
                            If a.Translation.text(English) = Left(linkAttributes, Len(linkAttributes) - 2) Then
                                found = True
                                a.Translation.text(English) = linkAttributes
                                a.Translation.Update(English)
                            End If
                            If a.Translation.text(English) = linkAttributes Then found = True
                        Next
                    End If
                    If Not found Then fn = New clsProductAttribute(product, iq.i_attribute_code("Networking"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "NKW", 0, Nothing, 0, False))
                End If
            End If
            '     End If
        Next


    End Sub
    <Obsolete("Not called from anywhere so I presume this is old??? ML")>
    Public Sub ImportPSU()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase("Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=iq; password=wainwright; connection timeout=35;")
        Dim rdr As SqlClient.SqlDataReader
        Dim query As String = "SELECT distinct [SystemType],[ModelSKU],[FamilyCode],[PSU],[PSUqty], ccDescription FROM [iQ].[products].[Systems]" _
                             & "inner join iq.products.HierarchyFull on upcNum = [PSU]  where psu is not null order by FamilyCode"

        rdr = da.DBExecuteReader(con, query)

        If Not iq.i_attribute_code.ContainsKey("PSU") Then
            Dim quickspecAttribute As clsAttribute
            quickspecAttribute = New clsAttribute("PSU", iq.AddTranslation("PSU", English, "", 0, Nothing, 0, False), 0)
        End If

        Dim dt As DataTable = New DataTable()
        dt.Load(rdr)

        Dim systemSku As String = String.Empty
        Dim optionSku As String = String.Empty
        For Each product In iq.Products.Values
            Dim perf As List(Of clsProductAttribute) = Nothing
            product.i_Attributes_Code.TryGetValue("PSU", perf)
            If perf Is Nothing Then
                Dim drarray() As DataRow
                Dim filterExp As String = "ModelSKU = '" & product.SKU & "'"
                drarray = dt.Select(filterExp)
                If drarray.Length > 0 Then
                    Dim linkAttributes As String = String.Empty
                    Dim fn As clsProductAttribute
                    For Each row As DataRow In drarray
                        If row("PSU") IsNot Nothing AndAlso row("PSU") IsNot DBNull.Value Then
                            If (iq.i_SKU.ContainsKey(row("PSU"))) Then
                                Dim prodAlsoIncuded As clsProduct = iq.i_SKU(row("PSU"))

                                If prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                    fn = New clsProductAttribute(product, iq.i_attribute_code("PSU"), 0, iq.i_unit_code("txt"), prodAlsoIncuded.i_Attributes_Code("desc")(0).Translation)

                                    ' i might have messed this up .. Nick 
                                    'ElseIf prodAlsoIncuded.i_Attributes_Code.ContainsKey("desc") Then
                                    '    fn = New clsProductAttribute(product, iq.i_attribute_code("PSU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(row("ccDescription"), English, "PSU", 0, Nothing, 0, False))

                                End If
                            Else
                                linkAttributes += row("PSU")
                                fn = New clsProductAttribute(product, iq.i_attribute_code("PSU"), 0, iq.i_unit_code("txt"), iq.AddTranslation(linkAttributes, English, "PSU", 0, Nothing, 0, False))
                            End If
                        End If
                    Next
                End If
            End If
        Next
    End Sub
    Private Function capitalise(l As String) As String

        Dim w() = Split(l)  'splits at spaces
        For Each word In w
            Mid(word, 1, 1) = UCase(Mid(word, 1, 1))
        Next
        capitalise = Join(w, " ")

    End Function

    Public Sub MakeLimits(systemPath$, optionsku$, ByVal optionpath$, _
                              graftwriteCache As DataTable, slotWriteCache As DataTable, _
                             prunewritecache As DataTable, ByRef nextPruneID As Integer, quantityWriteCache As DataTable, FirstInFamily As Boolean, _
                             dicOptlimits As Dictionary(Of String, clsLimit), dicSlottypes As Dictionary(Of String, clsSlotType), _
                             dicOptLocalisation As Object, dicFIOs As Object, systemSKU As String, ByRef kept As Integer, ByRef pruned As Integer, ByRef chassisBranch As clsBranch, ByRef systemBranch As clsBranch, FamilyOptionDefs As DataTable, Optional ActionList As clsActionList = Nothing)

        Dim aprune As clsPrune

        Dim fullpath As String = systemPath & optionpath

        If InStr(fullpath$, "..") Then Stop


        Dim optionBranch As clsBranch = iq.Branches(CInt(Split(optionpath, ".").Last))
        Dim product As clsProduct = optionBranch.Product

        Dim sysSubFamily As String  'comes from the iq.systems.familycode
        sysSubFamily = iq.i_SKU(systemSKU).i_Attributes_Code("FamMinor")(0).Translation.text(English)  'IMPORTANT for compatibility

        ' Dim obn$ = Me.DisplayName(English)
        ' If InStr(LCase(obn$), "drive") > 0 Then Stop
        'Dim sku$ = Me.SKU
        If product.ID = 610 Then 'can be removed
            Dim a = 1
        End If


        'option limits are specified per broad option type eg HDD (not narrow option family.. NHPLFF2.5)
        If product.i_Attributes_Code.ContainsKey("optfamily") Then

            Dim optfamily As String = product.i_Attributes_Code("optFamily")(0).Translation.text(English)
            Dim optType As String = product.i_Attributes_Code("optType")(0).Translation.text(English)

            'option type limits become 'quanitities'  defining minimum and preferred increments for the option
            Dim incompat As Boolean = False
            If optType = "HDD" Then
                'We need to prune anything not compatibile with the family
                Dim r = FamilyOptionDefs.Select("SysFamily = '" & sysSubFamily & "'")
                If r.Length > 0 Then incompat = (Not IsDBNull(r(0)("FamilyPriStor")) AndAlso r(0)("FamilyPriStor") <> optfamily) AndAlso (Not IsDBNull(r(0)("FamilySecStor")) AndAlso r(0)("FamilySecStor") <> optfamily) AndAlso (Not IsDBNull(r(0)("FamilyTerStor")) AndAlso r(0)("FamilyTerStor") <> optfamily)

            End If

            If Not Compatible(optionBranch, sysSubFamily) Or incompat Then
                If optionBranch.Product.SKU = "AJ838A" Then Stop
                pruned += 1
                If ActionList Is Nothing OrElse ActionList.IsGo(optionsku, ActionType.INSERT, ObjectType.Prune, Nothing, fullpath) Then
                    aprune = New clsPrune(fullpath$, New NullableInt(), _
                                          "Import - Pruned Incompatible with subfamily", prunewritecache, nextPruneID)
                Else
                    ActionList.Add(optionsku, ActionType.INSERT, ObjectType.Prune, Nothing, fullpath)
                End If
            Else
                Dim ck$ = sysSubFamily & "^" & optType & "^" & optfamily
                '  If InStr(optfamily, "NHP") Then Stop


                'NOTE OPtionsLimits are the 'vague' - per sysfamily limits
                'eg 'dl320's have 2 PSU's (with an icrement of 1)

                'FIOs have the more definite quanitites - but no inccrements 


                Dim famLimits As clsLimit = Nothing
                If Not dicOptlimits.ContainsKey(ck$) Then
                    ' Its' legitimate for there to be no option limits for some combos
                    Dim j = False
                    '          Logit("no limits for " & ck$)
                    famLimits = New clsLimit(0, 1, 100, 1, 1)
                Else

                    famLimits = dicOptlimits(ck)  'get the Qinstalled, Qmax, Qmin, MinIncr, and PrefIncr for this opttion type in this subfamily - For example MEM,1,4,1,2

                    If famLimits.Qinstalled < 0 Then Stop
                    'this same (option type)  branch is grafted on to every system in the family - so we only need to make the quanities once (even though they're geographically localised - they have global scope ie. no paths)

                    'Category option limits does NOT do preinstalled options - it does MINs, Maxes and Increments
                    'THAT's a lie - it turns out that OptionLimits are overridden by FIOs
                    'qmax.Qinstalled = 0 '@@@

                    'makes a quantity record for the region(s) in which this option is available
                    'this one handles Increments and Maximums  - NB the path is empty ! (not fullpath)
                    'this is making a bunch of pathless quantities - but they're on the branch

                    famLimits.Qinstalled = 0
                    MakeLocalisedQuantity(optionBranch, Nothing, famLimits, dicOptLocalisation, quantityWriteCache, "", ActionList)  'Make quantity limits/increments per country/region
                    'isFIO was here - in error
                    kept += 1
                    'End If

                End If
                'make an (overriding because it has a path) quantity record for each option branch at its specific path if it's a SKUd 
                'factory installed option 
                'NOTE Fios (which come from Products.Systems) Override the quanitites that may be specified in OptionLimits
                'PSU's are a case in point where many of the optionLimits.qtyinstalled are zero but they have the correct inforamtion in 
                'products.systems.PSUqty

                Dim fio As Boolean = isFIO(optionsku, systemSKU, fullpath$, dicFIOs, dicOptLocalisation, famLimits, quantityWriteCache, ActionList) 'was limit

                'we only make takes slots for those options with limits (in the dictionary)
                'Dim optfam As String = optionBranch.Product.i_Attributes_Code("OptFam")(0).Translation.text(English)
                If product.i_Attributes_Code.ContainsKey("opttype") AndAlso product.i_Attributes_Code.ContainsKey("Slots") Then
                    Dim existingSlots = From y In chassisBranch.slots.Values Where y.Type.MajorCode = optType
                    If existingSlots.Count = 0 Then
                        If dicSlottypes.ContainsKey(optType & "^" & optfamily) Then
                            'Does the chassis (or system) have this slot as a give, if not we need to add one!!! - revised, from Paul, all factory installed should add a slot of its type, soldered parts should become non-removable...
                            'This is an odd one, if its a PCI card, it doesnt seem to follow the norm.....   Careful with memory though as it may be on the CPU...
                            If fio AndAlso product.ProductType.Code.ToUpper() <> "MEM" AndAlso product.i_Attributes_Code("Slots").First.NumericValue > 0 AndAlso optionBranch.Product.i_Attributes_Code.ContainsKey("opttype") AndAlso optionBranch.Product.i_Attributes_Code.ContainsKey("optfamily") AndAlso optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English).ToUpper() <> "PCI" Then
                                Dim cb = If(chassisBranch IsNot Nothing, chassisBranch, systemBranch).slots.Where(Function(sl) sl.Value.Type.MajorCode = optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English) AndAlso sl.Value.Type.MinorCode = optionBranch.Product.i_Attributes_Code("optfamily").First.Translation.text(English) AndAlso sl.Value.numSlots > 0)

                                If cb.Count = 0 Then
                                    If ActionList Is Nothing OrElse ActionList.IsGo(optionsku, ActionType.INSERT, ObjectType.Slot, If(chassisBranch IsNot Nothing, chassisBranch, systemBranch), dicSlottypes(optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English) & "^" & optionBranch.Product.i_Attributes_Code("optfamily").First.Translation.text(English)), "", dicFIOs(systemSKU)(optionsku) * product.i_Attributes_Code("Slots").First.NumericValue) Then
                                        Dim AddsSlot As clsSlot = New clsSlot(dicSlottypes(optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English) & "^" & optionBranch.Product.i_Attributes_Code("optfamily").First.Translation.text(English)), If(chassisBranch IsNot Nothing, chassisBranch, systemBranch), "", dicFIOs(systemSKU)(optionsku) * product.i_Attributes_Code("Slots").First.NumericValue, Nothing, New NullableInt(), 0, 0, slotWriteCache)
                                    Else
                                        ActionList.Add(optionsku, ActionType.INSERT, ObjectType.Slot, If(chassisBranch IsNot Nothing, chassisBranch, systemBranch), dicSlottypes(optionBranch.Product.i_Attributes_Code("opttype").First.Translation.text(English) & "^" & optionBranch.Product.i_Attributes_Code("optfamily").First.Translation.text(English)), "", dicFIOs(systemSKU)(optionsku) * product.i_Attributes_Code("Slots").First.NumericValue)
                                    End If
                                Else
                                    cb.First.Value.numSlots += (dicFIOs(systemSKU)(optionsku) * product.i_Attributes_Code("Slots").First.NumericValue)
                                End If
                            End If

                            If optionBranch.slotsGiven("", dicSlottypes(optType & "^" & optfamily)) = 0 Then
                                Dim consumes As Integer = -product.i_Attributes_Code("Slots")(0).NumericValue
                                'Need to convert PCI slot types back?
                                If ActionList Is Nothing OrElse ActionList.IsGo(optionsku, ActionType.INSERT, ObjectType.Slot, If(chassisBranch IsNot Nothing, chassisBranch, systemBranch), dicSlottypes(optType & "^" & optfamily), "", consumes) Then
                                    Dim TakesSlot As clsSlot = New clsSlot(dicSlottypes(optType & "^" & optfamily), optionBranch, "", consumes, Nothing, New NullableInt(), 0, 0, slotWriteCache)
                                Else
                                    ActionList.Add(optionsku, ActionType.INSERT, ObjectType.Slot, If(chassisBranch IsNot Nothing, chassisBranch, systemBranch), dicSlottypes(optType & "^" & optfamily), "", consumes)
                                End If
                            Else
                                Dim j = False
                            End If

                        End If
                    End If
                End If
            End If
        End If


    End Sub

    Private Function CompareProduct(iq2Prod As clsProduct, iq1prod As clsProduct, AllowDelete As Boolean, ActionList As clsActionList, JustDoIt As Boolean, ByRef awc As DataTable)
        'Attributes
        Dim errormessages As List(Of String) = New List(Of String)()
        For Each a In iq1prod.i_Attributes_Code.ToList()
            If Not iq2Prod.i_Attributes_Code.ContainsKey(a.Key) Then
                'EASY ADD THIS
                For Each atr In a.Value
                    If JustDoIt Then Dim at = New clsProductAttribute(iq2Prod, iq.i_attribute_code(a.Key), atr.NumericValue, atr.Unit, atr.Translation, awc)
                    ActionList.Add(iq2Prod.SKU, ActionType.INSERT, ObjectType.Attribute, Nothing, atr)
                Next
            End If

            If iq2Prod.i_Attributes_Code(a.Key).Count <> a.Value.Count Then
                'Panic and reset all
                'DELETE ALL on iq2 and ADD all of iq1 again
                For Each atr In iq2Prod.i_Attributes_Code(a.Key).ToList()
                    'If JustDoIt Then Dim at = New clsProductAttribute(prod1, iq.i_attribute_code(a.Key), atr.NumericValue, atr.Unit, atr.Translation) - not brave enough for now, needs more testing
                    ActionList.Add(iq2Prod.SKU, ActionType.DELETE, ObjectType.Attribute, atr, Nothing)
                Next
                For Each atr In a.Value.ToList()
                    If JustDoIt Then Dim at = New clsProductAttribute(iq2Prod, iq.i_attribute_code(a.Key), atr.NumericValue, atr.Unit, atr.Translation, awc)
                    ActionList.Add(iq2Prod.SKU, ActionType.INSERT, ObjectType.Attribute, Nothing, atr)
                Next
            End If

            For i = 0 To a.Value.Count - 1
                If Not CompareAttribute(iq2Prod.i_Attributes_Code(a.Key)(i), a.Value(i)) Then
                    'UPDATE THIS
                    If JustDoIt Then
                        a.Value(i).Translation = iq2Prod.i_Attributes_Code(a.Key)(i).Translation
                        a.Value(i).NumericValue = iq2Prod.i_Attributes_Code(a.Key)(i).NumericValue
                        a.Value(i).Unit = iq2Prod.i_Attributes_Code(a.Key)(i).Unit
                        If a.Value(i).ID <> 0 Then
                            a.Value(i).update(errormessages)
                        End If

                    End If
                    ActionList.Add(iq2Prod.SKU, ActionType.UPDATE, ObjectType.Attribute, iq2Prod.i_Attributes_Code(a.Key)(i), a.Value(i))
                End If
            Next
        Next
        If AllowDelete Then
            For Each a In iq2Prod.i_Attributes_Code
                If Not iq1prod.i_Attributes_Code.ContainsKey(a.Key) Then
                    'DELETE 
                    For Each atr In a.Value.ToList()
                        ActionList.Add(iq2Prod.SKU, ActionType.DELETE, ObjectType.Attribute, atr, Nothing)
                    Next
                End If
            Next
        End If

        'Deal with direct product properties... (and dont prompt, just update as these are deemed correct in iq1 for now)
        iq2Prod.EOL = iq1prod.EOL
        iq2Prod.Active = iq1prod.Active
        iq2Prod.activeFrom = iq1prod.activeFrom
        iq2Prod.activeTo = iq1prod.activeTo

        If iq2Prod.ID = 0 Then Stop
        iq2Prod.update(errormessages)

    End Function
    Public Function CompareAttribute(iq2Attr As clsProductAttribute, iq1Attr As clsProductAttribute) As Boolean

        If iq1Attr.Translation Is Nothing And iq2Attr.Translation IsNot Nothing Then Return False
        If iq1Attr.Translation IsNot Nothing And iq2Attr.Translation Is Nothing Then Return False

        If iq2Attr.Translation IsNot Nothing And iq1Attr.Translation IsNot Nothing Then 'for clarity
            Dim iq1txt$ = iq1Attr.Translation.text(English)
            Dim iq2txt$ = iq2Attr.Translation.text(English)
            If LCase(iq2txt$) <> LCase(iq1txt$) Then Return False
        End If

        If iq2Attr.NumericValue <> iq1Attr.NumericValue Then Return False

        Return True
    End Function
    'Private Sub AddOrUpdateProductAttribute(ActionList As clsActionList, DummyRun As Boolean, DontDelete As Boolean, Inserting As Boolean, Product As clsProduct, clsAttribute As clsAttribute, p3 As Integer, textUnit As clsUnit, clsTranslation As clsTranslation, AttribWriteCache As DataTable)
    '    If Inserting Then
    '        ActionList.Add(Product.sku, ActionType.INSERT, ObjectType.Attribute, clsAttribute.Code, clsTranslation.text(English))
    '        If Not DummyRun Then
    '            Dim FA As clsProductAttribute = New clsProductAttribute(Product, clsAttribute, p3, textUnit, clsTranslation, AttribWriteCache)
    '        End If
    '    Else
    '        Dim cr = CompareAttributeAndLog(Product, clsAttribute, clsTranslation)
    '        Select Case cr
    '            Case ActionType.UPDATE
    '                For Each pa In Product.i_Attributes_Code(clsAttribute.Code)

    '                Next
    '                ActionList.Add(Product.sku, ActionType.INSERT, ObjectType.Attribute, clsAttribute.Code, clsTranslation.text(English))
    '            Case ActionType.INSERT
    '                Dim FA As clsProductAttribute = New clsProductAttribute(Product, clsAttribute, p3, textUnit, clsTranslation, AttribWriteCache)
    '                ActionList.Add(Product.sku, ActionType.INSERT, ObjectType.Attribute, clsAttribute.Code, clsTranslation.text(English))
    '            Case ActionType.DELETE
    '                If Not DontDelete Then

    '                End If
    '        End Select


    '    End If
    '    End If

    'End Sub
    'Function CompareAttributeAndLog(ActionList As clsActionList, Product As clsProduct, clsAttribute As clsAttribute, newValue As clsTranslation) As ActionType
    '    'Ok how do we deal with multiple of the same attribute without the old value?
    '    Dim found As Boolean = False
    '    If Not Product.i_Attributes_Code.ContainsKey(clsAttribute.Code) Then Return ActionType.INSERT
    '    For Each pa In Product.i_Attributes_Code(clsAttribute.Code)
    '        If pa.Translation.text(English) = newValue.text(English) Then found = True
    '    Next
    '    If Not found Then
    '        If Product.i_Attributes_Code(clsAttribute.Code).Count > 1 Then
    '            For Each pa In Product.i_Attributes_Code(clsAttribute.Code)
    '                ActionList.Add(Product.sku, ActionType.DELETE, ObjectType.Attribute, clsAttribute.Code, pa.Translation.text(English))
    '            Next
    '            ActionList.Add(Product.sku, ActionType.INSERT, ObjectType.Attribute, clsAttribute.Code, pa.Translation.text(English))
    '            Return ActionType.UPDATE
    '        Else
    '            ActionList.Add(Product.sku, ActionType.UPDATE, ObjectType.Attribute, clsAttribute.Code, value)
    '            Return ActionType.UPDATE
    '        End If
    '    Else
    '        Return ActionType.NONE
    '    End If

    'End Function

    Public Function SlowFilledDataTable(con As SqlConnection, sql$) As DataTable

        Dim adpt As SqlDataAdapter = New SqlDataAdapter(sql$, con)
        adpt.SelectCommand.CommandTimeout = 120
        SlowFilledDataTable = New DataTable
        adpt.Fill(SlowFilledDataTable)

    End Function



End Module
