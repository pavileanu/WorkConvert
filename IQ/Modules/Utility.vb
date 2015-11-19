
Imports dataAccess
Imports System.Data.SqlClient
Imports System.IO
Imports System.Runtime.Serialization
Imports System.Xml
Imports System.Net.Mail
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Xml.Serialization
Imports System.Globalization
Imports System.Linq
Imports System.Drawing
Imports System.Web.UI.DataVisualization.Charting
Imports System.Security.Cryptography
Imports System.Web.UI.WebControls

Imports System.Threading
Imports System.Threading.Tasks

Imports Google
Imports Google.Apis.Auth.OAuth2
Imports Google.Apis.Drive.v2
Imports Google.Apis.Drive.v2.Data
Imports Google.Apis.Services
Imports log4net
Imports log4net.Config


Module Utility

    Public Const imagebase As String = "http://www.channelcentral.net/"
    'Public Const imagebase = "http://iquote2.channelcentral.net/sandbox/daisyimages/"

    Public eim$ = "../editor/images/"

    Public ev_Warning As clsState
    Public ev_Critical As clsState
    Public ev_Info As clsState
    Private log As ILog = LogManager.GetLogger("IQUtility")
    Private switchAccountLock As Object = New Object
    Private logLock As Object = New Object

    ''' <summary>Wraps the string in "quotes" - escaping and quotes, singles quores and \'s therein for an excel compatible CSV format</summary>
    ''' <remarks>Attempts to comply with http://tools.ietf.org/html/rfc4180 which appears to be the closest thing there is to a standard </remarks>
    Public Function CSV(l$) As String

        Dim r$
        r$ = Replace(l$, Chr(34), Chr(34) & Chr(34))
        '    r$ = Replace(r$, "\", "\\")  'there is no mention of this in 'the standard'
        '    r$ = Replace(r$, "'", "''")
        'finally - wrap in quotes
        r$ = Chr(34) & r$ & Chr(34)

        Return r$

    End Function
    ''' <summary>
    ''' Creates delete button pass path id and command in. 
    ''' </summary>
    ''' <param name="path">An String object that represents the path of the item</param>
    ''' <param name="id">A integer value that prepresnts the id of item.</param>
    ''' <param name="cmd">An string object that represent the cmd to run.</param>
    ''' <returns>An instance of Literal Control.</returns>
    ''' <remarks></remarks>
    Public Function FunctionButton(path As String, id As Integer, cmd As String, caption As String, tooltip As String) As Literal
        Dim lt As Literal = New Literal
        lt.Text = String.Format("{0}{1}{2}{3}{4}{5}{6}", "<div onclick='burstBubble();getBranches(|path=", path, "&cmd=", cmd, "&id=", id, "|);return false;'")
        lt.Text = Replace(lt.Text, "|", Chr(34))
        lt.Text &= "style='background-color:red;color:white;display:inline-block;cursor:pointer;z-index:20;'"
        lt.Text &= "title='" & tooltip & "'>"
        lt.Text &= caption & "</div>"
        Return lt

    End Function

    Public Function writeSystemsBelow(b As clsBranch)

        'compile a list of all systems by family
        Dim systems As New HashSet(Of clsProduct)
        b.systemsBelow(systems) 'recurses finding all systems (by sku)

        Dim scs As Dictionary(Of String, String)
        scs = New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
        scs.Add("HP Renew", "R")
        scs.Add("Promotional", "P")
        scs.Add("Regular models", "A")
        scs.Add("Smart Buy", "SB")
        scs.Add("Top Value", "TV")

        Dim sw As StreamWriter = New StreamWriter("c:\temp\systems.txt")


        Dim c$ = "Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=datastore; password=wainwright; connection timeout=35; MultipleActiveResultSets=true;"
        Dim con As SqlClient.SqlConnection = da.OpenDatabase(c$)


        'INSERT H1.DataStore.admin.Compare_Systems
        '(Version, SysType, SysFamilyName, SystemSKU, SystemDesc, SystemSupplyChain)

        Dim wc As DataTable = da.MakeWriteCacheFor(con, "admin.compare_systems")


        Dim ts As DateTime = Now

        Dim ss As Integer
        For Each s In systems
            'SysType SysFamilyName SystemSKU SystemSupplyChain SystemDesc
            'SWD TAPE_DRIVE AG576B A HP StoreEver 3U SAS Rackmount Kit

            If s.Active And s.activeFrom < Now And Now < DateAdd(DateInterval.Month, 3, s.activeTo) And s.Publish Then
                Dim fm As String = ""
                If s.i_Attributes_Code.ContainsKey("fammajor") Then 'Somes Microservers have no family etc.
                    If s.i_Attributes_Code.ContainsKey("fammajor") Then fm = s.i_Attributes_Code("fammajor")(0).Translation.text(English)
                    Dim sc As String = s.i_Attributes_Code("SC")(0).Translation.text(English)
                    ss += 1
                    Dim desc$ = s.i_Attributes_Code("desc")(0).Translation.text(English)
                    sw.WriteLine("IQ2^" & s.ProductType.Code & "^" & fm & "^" & s.SKU & "^" & scs(sc) & "^" & desc)

                    If s.SKU.Length < 40 Then
                        If s.SKU.Length > 40 Then Stop
                        If desc.Length > 400 Then Stop
                        If s.ProductType.Code.Length > 3 Then Stop
                        If fm.Length > 20 Then Stop
                        'If sc.Length > 4 Then Stop

                        Dim r As DataRow = wc.NewRow
                        r.Item("sysType") = s.ProductType.Code
                        r.Item("sysFamilyName") = fm
                        r.Item("systemSKU") = s.SKU
                        r.Item("systemDesc") = desc
                        r.Item("systemSupplyChain") = scs(sc)
                        r.Item("version") = "IQ2"
                        r.Item("reportTime") = ts

                        wc.Rows.Add(r)
                    End If
                End If
            End If
        Next
        sw.Close()


        da.BulkWrite(con, wc, "admin.compare_systems")
        con.Close()


    End Function

    Public Function OptionsPerSystem()

        Dim opts As HashSet(Of String) = New HashSet(Of String)

        Dim prunes, dupes As Integer
        Dim inskus As New HashSet(Of String)

        'For Each sku In Split("AP838B,AW568B,E1Z55LT,AP838B,AW568B,E1Z55LT,F1K76LA,F1P36EA,J6D94UA,470065-743,470065-861,769503-291,WM448ET,WM698EA,F1K76LA,F1P36EA,J6D94UA,470065-743,470065-861,769503-291,WM448ET,WM698EA", ",")
        '    inskus.Add(sku)
        'Next

        Dim jjj As Boolean = iq.i_SKU.ContainsKey("QK765A")

        prunes = 0
        dupes = 0
        Dim sw As StreamWriter = New StreamWriter("c:\temp\dupedOptions.txt")
        iq.RootBranch.OptionsPersystem("", opts, "tree." & iq.RootBranch.ID, prunes, dupes, sw, inskus)
        sw.Close()

        Dim c$ = "Data Source=www3.channelcentral.net,8484; user id=editor;Initial Catalog=datastore; password=wainwright; connection timeout=35; MultipleActiveResultSets=true;"
        Dim con As SqlClient.SqlConnection = da.OpenDatabase(c$)

        'INSERT H1.DataStore.admin.Compare_Systems
        '(Version, SysType, SysFamilyName, SystemSKU, SystemDesc, SystemSupplyChain)

        da.DBExecutesql(con, "truncate table datastore.admin.compare_optionsPerSystem")

        Dim wc As DataTable = da.MakeWriteCacheFor(con, "datastore.admin.compare_optionsPerSystem")

        For Each o In opts
            Dim bits() As String = Split(o, "^")
            Dim r As DataRow = wc.NewRow
            'If bits(0) = "QK765A" Then Stop
            r.Item("systemSKU") = bits(0)
            r.Item("optionSKU") = bits(1)
            wc.Rows.Add(r)
        Next

        da.BulkWrite(con, wc, "admin.compare_optionsPerSystem")
        con.Close()


    End Function

    Public Function writeOptionsBelow(b As clsBranch)

        Dim c$ = "Data Source=www.channelcentral.net,8484; user id=editor;Initial Catalog=datastore; password=wainwright; connection timeout=35; MultipleActiveResultSets=true;"
        Dim con As SqlClient.SqlConnection = da.OpenDatabase(c$)

        'INSERT H1.DataStore.admin.Compare_Systems
        '(Version, SysType, SysFamilyName, SystemSKU, SystemDesc, SystemSupplyChain)

        Dim wc As DataTable = da.MakeWriteCacheFor(con, "admin.compare_options")

        Dim ts As DateTime = Now
        Dim sw As StreamWriter
        'CK - SKU^sysfamcode
        'Dim ck$ = ty$ & "^" & fam$ & "^" & Me.Product.i_Attributes_Code("MfrSKU")(0).Translation.text(English)
        Dim opts As New Dictionary(Of String, clsProduct)
        b.DistinctOptionsRecursive("", "", opts)


        sw = New StreamWriter("c:\temp\options.txt")
        For Each kvp In opts
            Dim ot As String = ""
            If kvp.Value.i_Attributes_Code.ContainsKey("optType") Then
                ot = kvp.Value.i_Attributes_Code("opttype")(0).Translation.text(English)
            End If

            Dim o As clsProduct = kvp.Value

            If o.Active And o.activeFrom < Now And Now < DateAdd(DateInterval.Month, 3, o.activeTo) And o.Publish Then

                'don't write 'carepacks' for .. now
                If Not ("WTY,SUP,HWSW,SVC").Contains(ot.ToUpper) Then
                    Dim dsc As String = ""
                    If kvp.Value.i_Attributes_Code.ContainsKey("desc") Then
                        dsc = kvp.Value.i_Attributes_Code("desc")(0).Translation.text(English)
                    End If

                    'Dim sku$ = ""
                    'If kvp.Value.i_Attributes_Code.ContainsKey("mfrsku") Then
                    ' sku = kvp.Value.i_Attributes_Code("mfrsku")(0).Translation.text(English)
                    ' End If

                    Dim bits() As String = Split(kvp.Key, "^")
                    Dim skuType As String = bits(0)
                    Dim fam As String = bits(1)
                    Dim sku As String = bits(2)

                    sw.WriteLine("IQ2^" & skuType & "^" & fam & "^" & ot & "^" & sku & "^" & dsc)

                    Dim r As DataRow = wc.NewRow
                    If Len(ot) > 5 Then ot = "HDD" 'HORRIBLE HACK FIX
                    r.Item("optType") = ot
                    r.Item("sysFamilyName") = fam
                    r.Item("optsku") = sku
                    r.Item("optdesc") = dsc
                    r.Item("version") = "IQ2"
                    r.Item("reportTime") = ts
                    r.Item("sysType") = skuType

                    wc.Rows.Add(r)

                End If
            End If

        Next kvp
        sw.Close()

        da.BulkWrite(con, wc, "admin.compare_options")
        con.Close()


    End Function


    Public Function systemPath(path$) As String

        Dim segs() As String
        segs = Split(path$, ".")

        Dim p As clsProduct
        Dim pth As String = "tree"
        For Each seg In segs

            If LCase(seg) <> "tree" Then
                pth &= "." & seg
                p = iq.Branches(CInt(seg)).Product
                If p IsNot Nothing AndAlso p.isSystem Then Return pth
            End If
        Next seg

        Return ""

    End Function

    Public Function PictureOnPath(Path$, picture As String) As Boolean

        PictureOnPath = False
        Dim seg() As String
        seg = Split(Path$, ".")

        For Each s In seg
            If LCase(s) <> "tree" Then
                If LCase(iq.Branches(CInt(s)).Picture) = LCase(picture) Then Return True
            End If
        Next

    End Function

    Public Function simpleHash(l$) As UInt64

        Dim j As SHA1
        j = New SHA1Cng

        Dim i As Integer = 0

        ' SK - I can't see the point of the fixed (and very short) salt value in the following line. Each salt value
        ' should be unique (preferably randomly generated, e.g. via RNGCryptoServiceProvider) and stored. A fixed salt
        ' value isn't really adding much security...
        Dim bytes() As Byte = Encoding.UTF8.GetBytes(l$ + "s34dog") 'salt http://en.wikipedia.org/wiki/Salt_(cryptography)

        Dim hashbytes() As Byte

        hashbytes = j.ComputeHash(bytes) ' And UInt64.MaxValue

        simpleHash = BitConverter.ToUInt64(hashbytes, 0)

    End Function

    Public Class Ra
        Public Required As Integer
        Public Available As Integer

        Public Sub New(r As Integer, a As Integer)
            Me.Required = r
            Me.Available = a
        End Sub

    End Class

    Public Class clsRange
        Public UnitText As String
        Public min As Int64
        Public max As Int64
        Public IsMixed As Boolean = False
        Public TextRepresentation As String

        Public Sub New() 'min As Int64, max As Int64)
            Me.min = Int64.MaxValue   'this is a little counterinuitive - but its right... the first update 'stretc' will set min and max
            Me.max = Int64.MinValue
        End Sub

        Public Function stretch(v As Int64) As Boolean
            'Updates the min and max based on some new value - returns true if the range was extended

            stretch = False
            If v < Me.min Then Me.min = v : stretch = True
            If v > Me.max Then Me.max = v : stretch = True

            If Me.min < 0 And Me.min <> Int64.MinValue Then Me.min = 0 'Never show negatives...
        End Function


    End Class
    Public Class clsMinMaxTotalUsed
        Public Min As Integer
        Public Max As Integer
        Public Total As Integer
        Public Used As Integer
        Public optionalRule As Boolean

        Public Sub New(min As Integer, max As Integer, total As Integer, Used As Integer, optionalRule As Boolean)
            Me.Min = min
            Me.Max = max
            Me.Total = total
            Me.Used = Used
            Me.optionalRule = optionalRule
        End Sub

        Public Function stretch(v As Integer) As Boolean
            'Updates the min and max based on some new value - returns true if the range was extended

            stretch = False
            If v < Me.Min Then Me.Min = v : stretch = True
            If v > Me.Max Then Me.Max = v : stretch = True

        End Function

    End Class



    Public Function ReplaceSegment(path$, find As Integer, replace As Integer) As String

        Dim segs() As String = Split(path$, ".")

        'we go from 1 becuas the 0th seg is 'tree.'
        For i = 1 To UBound(segs)
            If CInt(segs(i)) = find Then segs(i) = Trim$(CStr(replace))
        Next

        ReplaceSegment = Join(segs, ".")

    End Function
    Public Function BitCount(b As Integer) As Integer
        'Returns the number of bits set in the byte
        Dim c As Integer
        Dim m As Integer = 1 'mask
        For i = 0 To 7 'maxbits is the number of bits to check
            If b And m Then c += 1
            m = m + m
        Next

        Return c

    End Function

    Public Function LowestPrice(prices As List(Of clsPrice)) As clsPrice

        Dim nonTestPrices = From p In prices Where p.SKUVariant.Code.ToUpper <> "TST" Order By p.Price.value Ascending

        If nonTestPrices.Count > 0 Then
            LowestPrice = nonTestPrices.First
        Else
            LowestPrice = (From p In prices Order By p.Price.value Ascending).First
        End If

        'LowestPrice = If((From p In lp Where p.SKUVariant.Code.ToUpper <> "TST" Order By p.Price.value Ascending).Count > 0, (From p In lp Where p.SKUVariant.Code.ToUpper <> "TST" Order By p.Price.value Ascending).First, (From p In lp Order By p.Price.value Ascending).First)

    End Function

    ''' <summary>'return the .Price property of supplied clsPrice instance - unless it's Nothing in which case it returns an empty price of the specified currency</summary>
    Public Function NullPrice(price As clsPrice, currency As clsCurrency) As NullablePrice

        If price Is Nothing Then
            Return New NullablePrice(currency)
        Else
            Return price.Price
        End If

    End Function

    Public Function s_lang() As clsLanguage
        s_lang = English
    End Function

    Public Sub FillDDL(ByRef ddl As DropDownList, values As Object)

        For Each v In values
            ddl.Items.Add(New ListItem(v.displayname(s_lang), v.id))
        Next

    End Sub

    Public Function GetParenthesisValue(l$) As String

        'returns a value enclosed in parantheses from a strring

        If InStr(l$, "(") Then
            Dim j As String = Split(l$, "(")(1)
            Dim cp As Integer = InStr(j$, ")")
            If cp = 0 Then
                Stop 'missing close parentehise
            End If
            j = Left$(j$, cp - 1)
            Return j
        Else
            Return ""
        End If

    End Function

    'Public Function ChannelSKU(product As clsProduct, skuvariant As clsVariant, sellerChannel As clsChannel) As String

    '    'this really needs to be cached in a dictionary - as it could be called several hundred times in the opening of a single branch

    '    If skuvariant Is Nothing Then skuvariant = iq.StandardVariant

    '    ChannelSKU = ""

    '    Dim con As SqlClient.SqlConnection
    '    Dim rdr As SqlClient.SqlDataReader
    '    con = da.opendatabase()
    '    Dim sql$
    '    sql$ = "SELECT ChannelMFRSKU FROM channelSKU where fk_product_id=" & product.ID & " AND FK_Variant_ID=" & skuvariant.ID & " AND FK_Channel_ID=" & sellerChannel.ID

    '    rdr = da.dbexecuteReader(con, sql$)

    '    If rdr.HasRows Then
    '        rdr.Read()
    '        ChannelSKU = rdr.Item("channelmfrsku")
    '    End If

    '    rdr.Close()
    '    con.Close()

    'End Function


    ''' <summary>
    ''' Makes the UI for the 'specification section of a quote item (mostly information on slot utilisations)    ''' 
    ''' </summary>
    ''' <param name="dicslots">The consolidated information of slot usage within this system</param>
    ''' <param name="slottypes"></param>
    ''' <param name="Open">If open - it returns a richer panel, with a close button</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function SpecUI(lid As UInt64, i As clsQuoteItem, dicslots As Dictionary(Of clsSlotType, clsSlotSummary), slottypes As List(Of String), Open As Boolean, language As clsLanguage) As Panel

        'it would be nicer if this as a member of the clsQuoteItem - but the fact that this is based on a precompiled dictionary complicated thigs
        'dicslots is  my 'minor' type - slot types is the list of 'categories' we're validating by W,MEM,HDD etc
        SpecUI = New Panel
        SpecUI.CssClass = "panelOuter"


        '        Dim totalavail, used, unused As Integer

        Dim SpecHeader As Panel = New Panel
        SpecHeader.CssClass = "panelHeader"
        SpecUI.Controls.Add(SpecHeader)
        Dim script As String = "" 'passed byref and POPULATES the script (which goes onto the WHOLE panel (not just the button)
        SpecHeader.Controls.Add(i.PanelButton(panelEnum.Spec, Open, script$))
        SpecHeader.Attributes("onmousedown") = script$

        Dim title As Literal = New Literal
        title.Text = String.Format("<div class='panelTitle'>{0}</div>", Xlt("Specification", i.quote.AgentAccount.Language))
        SpecHeader.Controls.Add(title)

        Dim specRollup As Panel = New Panel
        specRollup.CssClass = "specRollup"
        SpecHeader.Controls.Add(specRollup)


        Dim list As HtmlGenericControl = New HtmlGenericControl("UL")

        'For Each st In dicslots.Keys
        '    If slottypes.Contains(st.MajorCode) Then 'consolidate by major code
        '        With dicslots(st)
        '            'unused = dicslots(st).Given - dicslots(st).taken
        '            'totalAvail = dicslots(st).Given
        '            'used = totalavail - unused

        '            Dim lit As Literal = New Literal
        '            If st.MajorCode = "CPU" Then
        '                lit.Text = "<div class='specRollupItem'>" & st.MajorCode & ":" & .taken & " x " & .TotalCapacity / .taken & "&nbsp;"
        '            ElseIf st.MajorCode = "PWR" Then
        '                lit.Text = "<div class='specRollupItem'>" & st.MinorCode & ":" & .taken & "&nbsp;"
        '            Else
        '                lit.Text = "<div class='specRollupItem'>" & st.MajorCode & ":" & .TotalCapacity & "&nbsp;"
        '            End If

        '            If dicslots(st).CapacityUnit IsNot Nothing Then
        '                lit.Text &= dicslots(st).CapacityUnit.Translation.text(English) & "&nbsp;"
        '            End If
        '            lit.Text &= "</div>"
        '            specRollup.Controls.Add(lit)

        '            If Open Then
        '                'open - verbose version

        '                'we ONLY want to present the amber light for Watts ('AKA Power Sizing')
        '                Dim cu As String = ""
        '                If dicslots(st).CapacityUnit IsNot Nothing Then
        '                    cu = dicslots(st).CapacityUnit.Code
        '                End If

        '                Dim li As HtmlGenericControl = New HtmlGenericControl("LI")
        '                list.Controls.Add(li)

        '                If .taken > .Given Then
        '                    li.Attributes("Style") &= " traffic redLight"
        '                ElseIf .taken >= .Given * 0.75 And cu = "W" Then   ' Greg didn't like this (other than for PSU)
        '                    li.Attributes("Style") &= " traffic amberlight"
        '                Else
        '                    li.Attributes("Style") &= " traffic greenLight"
        '                End If

        '                lit = New Literal
        '                'lit.Text &= translateable(st.Translation.text(English), lid) & " used " & .taken & " of " & .Given & " available"

        '                'stopgap until we get round to translating the minor slot types above - to things like 'Small Drive Bay'
        '                lit.Text &= .taken & " " & fullMajor(st.MajorCode) & " of " & .Given '& " available"

        '                If dicslots(st).CapacityUnit IsNot Nothing Then
        '                    If st.MajorCode = "CPU" Then
        '                        lit.Text &= "&nbsp;totaling " & dicslots(st).taken & " x " & dicslots(st).TotalCapacity / dicslots(st).taken & "&nbsp;" & dicslots(st).CapacityUnit.Translation.text(English)
        '                    ElseIf st.MinorCode = "W" Then
        '                        'lit.Text &= "&nbsp;totaling " & dicslots(st).TotalCapacity & "&nbsp;" & dicslots(st).CapacityUnit.Translation.text(English)
        '                    Else
        '                        lit.Text &= "&nbsp;totaling " & dicslots(st).TotalCapacity & "&nbsp;" & dicslots(st).CapacityUnit.Translation.text(English)
        '                    End If

        '                End If
        '                li.Controls.Add(lit)

        '            End If
        '        End With
        '    End If
        'Next

        'SpecUI.Controls.Add(list)

        Dim maxPower = 0
        For Each p In i.Children.GroupBy(Function(c) c.Branch.Product.ProductType.Code.ToLower())
            Dim dis = dicslots.Where(Function(ds) ds.Key.MajorCode.ToLower() = p.Key).FirstOrDefault
            If i.Msgs.Where(Function(msg) msg.slotTypeMajor IsNot Nothing AndAlso msg.slotTypeMajor.ToLower() = p.Key).Count = 0 Then
                If p.Sum(Function(f) If(f.validate, f.Quantity, 0)) > 0 Then
                    If slottypes.Contains(p.Key.ToUpper) Then
                        Dim text = ""
                        Dim totalCapacity = If(p.Count > 0 AndAlso p.First.Branch.Product.i_Attributes_Code.ContainsKey("capacity"), p.Sum(Function(dd) If(dd.Branch.Product.i_Attributes_Code.ContainsKey("capacity"), If(dd.validate, dd.Quantity, 0) * dd.Branch.Product.i_Attributes_Code("capacity").First.NumericValue, 0)), "")
                        If dis.Value IsNot Nothing Then text = String.Format("{0} {1}{2} ({3} slots of {4}) {5}", p.First.Branch.Product.ProductType.Translation.text(language), If(dis.Value.TotalCapacity > 0, dis.Value.TotalCapacity.ToString(), ""), If(dis.Value.CapacityUnit IsNot Nothing, dis.Value.CapacityUnit.Code, ""), dis.Value.taken.ToString(), dis.Value.Given.ToString(), If(p.First.Branch.Product.ProductType.Code.ToLower() = "psu", " - (" & If(dicslots.Where(Function(ds) ds.Key.MajorCode.ToLower = "pwr").Sum(Function(ds) ds.Value.taken) <= dis.Value.TotalRedundantCapacity, Xlt("Redundant", language), Xlt("Non Redundant", language)) & ")", ""))

                        If Open Then list.Controls.Add(NewLit("<li>" & text & "</li>"))

                        If (i.Branch.Product.ProductType.Code = "SVR" AndAlso {"cpu", "mem", "pwr"}.Contains(p.Key)) Or (i.Branch.Product.ProductType.Code = "HPN" AndAlso {"upconnectivity", "priconnectivity"}.Contains(p.Key)) Then
                            If slottypes.Contains(p.Key.ToUpper) Then

                                specRollup.Controls.Add(NewLit(String.Format("<div class='specRollupItem'><span style='font-weight: bold;'>{0}</span> : {1}{2}</div>", p.Key.ToUpperInvariant, If(dis.Value IsNot Nothing, dis.Value.TotalCapacity, totalCapacity.ToString()), If(dis.Value IsNot Nothing AndAlso dis.Value.CapacityUnit IsNot Nothing, dis.Value.CapacityUnit.Code, ""))))

                                'specRollup.Controls.Add(NewLit("<span>" & p.Key & ":" & p.Sum(Function(f) If(f.validate, f.Quantity, 0)) & "</span>"))
                            End If
                        End If
                    End If
                End If
            End If

        Next

        For Each p In dicslots

            If Not slottypes.Contains(p.Key.MajorCode.ToUpper()) Then Continue For
            If i.Branch.Product.ProductType.Code = "SVR" AndAlso {"PWR"}.Contains(p.Key.MajorCode.ToUpper()) AndAlso i.Msgs.Where(Function(msg) msg.slotTypeMajor IsNot Nothing AndAlso msg.slotTypeMajor.ToLower() = p.Key.MajorCode.ToLower()).Count = 0 Then
                Dim text As String = String.Format("{0} {1}{2} ({3} slots of {4})", If(p.Key.TranslationShort IsNot Nothing, p.Key.TranslationShort.text(language), p.Key.Translation.text(language)), p.Value.TotalCapacity.ToString(), If(p.Value.CapacityUnit IsNot Nothing, p.Value.CapacityUnit.Code, ""), p.Value.taken.ToString(), p.Value.Given.ToString())
                'Here we are in hackly land again, can't use slot total as soldered parts dont take a slot so we need to equate slottype or producttype and total the preinstalled of that product type....

                'Once again power is a special case....
                If p.Key.MajorCode.ToUpper() = "PWR" Then
                    text = String.Format("{0} {1}W of {2}W", Xlt("Power Consumption", language), p.Value.taken.ToString(), p.Value.Given.ToString())
                End If
                If Open Then list.Controls.Add(NewLit("<li>" & text & "</li>"))

                Dim midt As String = ""
                If p.Key.MajorCode.ToUpper() = "PWR" Then midt = "W"
                specRollup.Controls.Add(NewLit("<div class='specRollupItem'><span style='font-weight: bold;'>" & p.Key.MajorCode & "</span> : " & p.Value.taken & midt & "/" & p.Value.Given & midt & "</div>"))
            End If
        Next

        SpecUI.Controls.Add(list)



    End Function

    Public Function translateable(i$, lid As UInt64) As String

        'can link from here to the editor to edit specific translations

        'translateable = "<span style='color:blue'>" & i$ & "</span>"
        translateable = "<span>" & i$ & "</span>"

    End Function

    Private Function fullMajor(code As String) As String

        Select Case UCase(code)
            Case Is = "HDD"
                fullMajor = "Drive bays"
            Case Is = "MEM"
                fullMajor = "Memory Slots"
            Case Is = "OPT"
                fullMajor = "Optical Drive bays"
            Case Is = "FAN"
                fullMajor = "Fan Bays"
            Case Is = "CPU"
                fullMajor = "CPU Slots"
            Case Is = "PWR"
                fullMajor = "Watts"
            Case Else
                Return code
        End Select
    End Function



    Public Function IndexPaths() As Integer

        Dim WC As DataTable = New DataTable()
        Dim segcache As DataTable = New DataTable()

        da.DBExecutesql("DROP INDEX [Nick] ON [dbo].[PathSegment] WITH ( ONLINE = OFF )")

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase()
        WC = da.MakeWriteCacheFor(con, "Path")
        segcache = da.MakeWriteCacheFor(con, "PathSegment")

        da.DBExecutesql("truncate table Path")
        da.DBExecutesql("DBCC CHECKIDENT('[Path]', RESEED, 1)")

        da.DBExecutesql("truncate table PathSegment")
        da.DBExecutesql("DBCC CHECKIDENT('[PathSegment]', RESEED, 1)")

        iq.RootBranch.IndexPaths(con, "tree", "", WC, segcache, 1, 0)

        'these are now just the final bulk writes (as we write in blocks of 50k rows)
        da.BulkWrite(con, segcache, "PathSegment")
        segcache = Nothing

        'takes around 22 seconds (for circa 1.5 million rows, on my laptop)
        da.BulkWrite(con, WC, "Path")

        Dim sql$

        Beep() : Beep() : Beep()
        sql$ = "CREATE NONCLUSTERED INDEX [Nick] ON [dbo].[PathSegment] ([fk_branch_id] ASC,[fk_path_id] Asc) "
        sql$ &= "WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY];"
        da.DBExecutesql(sql$)
        Beep() : Beep() : Beep()

        IndexPaths = WC.Rows.Count

        WC = Nothing

    End Function

    'Public Function FormatPrice(amount As Single, region As clsRegion) As String

    '    Dim ci As CultureInfo = Nothing
    '    Try
    '        If region.Culture Is Nothing Then
    '            ci = New CultureInfo("en-us")

    '        Else
    '            ci = New CultureInfo(region.Culture.Code) ' & "-" & region.Code))
    '        End If
    '    Catch
    '        Err.Raise(100, Nothing, "The culture code " & region.Culture.Code & " for country " & region.Name.text(s_lang) & " is probably wrong.")
    '    End Try

    '    Return amount.ToString("C", ci)

    '    'this UN converts it !
    '    'If Single.TryParse(pr, NumberStyles.Any, ci.NumberFormat, p!) Then

    'End Function
    Public Function FormatPrice(amount As Single, culture As clsCulture) As String

        Dim ci As CultureInfo = Nothing
        Try
            If culture Is Nothing Then
                ci = New CultureInfo("en-us")

            Else
                ci = New CultureInfo(culture.Code) ' & "-" & region.Code))
            End If
        Catch
            Err.Raise(100, Nothing, "The culture code " & culture.Code & "is probably wrong.")
        End Try

        Return amount.ToString("C", ci)

        'this UN converts it !
        'If Single.TryParse(pr, NumberStyles.Any, ci.NumberFormat, p!) Then

    End Function

    'Public Sub WriteRows(ByRef DT As DataTable) - OBSOLETED by SQLBulkCopy (which doesnt require a dedicated SP and Table Type variable definition) - see Make WriteCacheFor() and da.bulkwrite()

    '    'Writes the accumlated SKUIndex rows via an SP
    '    '(and EMPTIES the dataTable) - ready for the next batch

    '    Dim con As SqlClient.SqlConnection = da.opendatabase()

    '    Dim params As Dictionary(Of String, Object)
    '    params = New Dictionary(Of String, Object)
    '    params.Add("tvp", DT)

    '    ExecuteSP(con, "SKUIndexInsert", params, Nothing)
    '    con.Close()

    '    'IMPORTANT !
    '    DT.Rows.Clear()

    'End Sub

    Public Function cloneQuoteItemRecursive(originalItem As clsQuoteItem, ontoQuote As clsQuote, newParentItem As clsQuoteItem)

        'Clones a quote item and all its children recursively - returning a new item - with now children (on the specified quote)

        With originalItem
            'this is the final return value
            If originalItem.Branch Is Nothing Then 'virtual' root item
                cloneQuoteItemRecursive = New clsQuoteItem(ontoQuote)
            Else

                cloneQuoteItemRecursive = New clsQuoteItem(ontoQuote, .Branch, .SKUVariant, .Path, .Quantity, .BasePrice, .ListPrice, .IsPreInstalled, IIf(originalItem.Parent.Branch Is Nothing, Nothing, newParentItem), .OPG, .Bundle, .rebate, .Margin, .Note, .order)

                cloneQuoteItemRecursive.Created = .Created
                cloneQuoteItemRecursive.parent = newParentItem


            End If
        End With

        Dim ci As clsQuoteItem
        For Each child In originalItem.Children ' was Me.Children
            ci = cloneQuoteItemRecursive(child, ontoQuote, cloneQuoteItemRecursive)
        Next

    End Function




    Public Function SaveXML(doc As XmlDocument, filename As String, ByRef Message As String) As String

        'returns the physical path to the file created
        'find the virtual, and from that the physical path
        Dim vPath = HttpContext.Current.Request.ApplicationPath
        Dim pPath = HttpContext.Current.Request.MapPath(vPath)

        SaveXML = pPath & filename

        Try
            doc.Save(pPath & filename)
            Message = "Saved Successfully."
        Catch ex As Exception
            Message = "ERROR saving " & ex.Message.ToString
            SaveXML = "FAIL"
        End Try

    End Function


    Public Sub LoadEnglish()

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim r As SqlClient.SqlDataReader = da.DBExecuteReader(con, "Select id,code,localname,rtl,live,active from language where code= 'en' ")

        If r.HasRows Then
            r.Read()
            English = New clsLanguage(CInt(r.Item("ID")), Trim$(r.Item("Code")), r.Item("Localname"), CBool(r.Item("rtl")), CBool(r.Item("live")), CBool(r.Item("active")))
        Else
            English = New clsLanguage("EN", "English", False, True, True)
        End If

        r.Close()
        con.Close()

    End Sub


    Public Sub bootstrap()

        'things we cannot even iq.Load() - without . .
        BootStrapTranslations()
        LoadEnglish()
        ' loadWorldWide()
        ' r_worldwide = clsRegion.getOrMake(Nothing, "XW", "Worldwide", False)


    End Sub

    'Private Function loadWorldWide()


    '    Dim con As SqlClient.SqlConnection

    '    con = da.OpenDatabase()
    '    Dim r As SqlClient.SqlDataReader = da.DBExecuteReader(con, "SELECT Id,[fk_region_id_parent],code,[fk_translation_key_name],isCountry,culture FROM [Region] where code='XW'")

    '    '   LoadTranslation(r.Item("fk_translation_key_name"))

    '    Dim aRegion As clsRegion
    '    If r.HasRows Then
    '        r.Read()
    '        aRegion = New clsRegion(CInt(r.Item("id")), Nothing, r.Item("code").ToString(), _
    '                                iq.Translations(CInt(r.Item("fk_translation_key_name"))), _
    '                                CBool(r.Item("isCountry")), r.Item("culture").ToString())

    '    End If

    '    r.Close()
    '    con.Close()




    'End Function

    Public Sub BootStrapTranslations()

        'we need at least 1 translation to exist for addtranslation to work


        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, "select top 1 * from translation")

        Dim addOne As Boolean
        If Not rdr.HasRows Then
            addOne = True
        End If
        rdr.Close()
        con.Close()


        If addOne Then da.DBExecutesql("INSERT INTO [translation] ([key],[text],fk_language_id,[group],[order]) VALUES (1,'bootstrap',1,'',0)")



    End Sub
    Public Sub checkEssentials()

        'Creates (once) a bunch of standard systemwide stuff - such as some base units, translations etc. - IF they don't exist (weren't LOADed)
        'Generally the root objects (RootBranch,RootEvent, RootThread and RootChannel are set during iq.Load - to the first object instanced (see the constructors)

        Reflection.setupClassList() 'required prior to makescreen() calls

        'If Not iq.i_state_GroupCode.ContainsKey("EV-Info") Then

        '    ev_Info = New clsState("EV", "Info", iq.AddTranslation("Info", English, ), 1, "#0000a0")
        '    ev_Warning = New clsState("EV", "Warn", iq.AddTranslation("Warning", English), 1, "#a0a060")
        '    ev_Critical = New clsState("EV", "Crit", iq.AddTranslation("Critical", English), 1, "#FF0000")
        'Else
        '    ev_Info = iq.i_state_GroupCode("EV-Info")
        '    ev_Warning = iq.i_state_GroupCode("EV-Warn")
        '    ev_Critical = iq.i_state_GroupCode("EV-Crit")
        'End If

        If iq.Channels.Count Then
            Dim uu As String = "unknown@unknown.com"
            If Not iq.i_user_email.ContainsKey(uu) Then Dim aUser As clsUser = New clsUser(iq.Channels.Values.First, uu, "System use", New nullableString(), New nullableString)
            UnknownUser = iq.i_user_email(uu)
        End If

        r_worldwide = clsRegion.getOrMake(Nothing, "XW", "Worldwide", False, False, "") '- see Bootstrap()
        r_RestOfWorld = clsRegion.getOrMake(r_worldwide, "ROW", "Rest Of World", False, False, "")
        r_Americas = clsRegion.getOrMake(r_worldwide, "AMS", "The Americas", False, True, "")
        r_USCA = clsRegion.getOrMake(r_Americas, "USCA", "The United States and Canada", False, True, "")
        r_EMEA = clsRegion.getOrMake(r_worldwide, "EMEA", "Europe, Middle East and Africa", False, False, "")
        r_GWE = clsRegion.getOrMake(r_EMEA, "GWE", "Greater and Western Europe", False, False, "")
        r_UKIE = clsRegion.getOrMake(r_GWE, "UKIE", "United Kingdom & Ireland", False, False, "")
        r_GB = clsRegion.getOrMake(r_UKIE, "GB", "United Kingdom", True, False, "") '!!!! GB !!!
        r_IE = clsRegion.getOrMake(r_UKIE, "IE", "Ireland", True, False, "")
        r_MEMA = clsRegion.getOrMake(r_EMEA, "MEMA", "Middle East, Mediterranean and Africa", False, False, "")
        r_CEE = clsRegion.getOrMake(r_EMEA, "CEE", "Central and Eastern Europe", False, False, "")

        If Not iq.i_attribute_code.ContainsKey("FamMajor") Then
            Dim fmaj = New clsAttribute("FamMajor", iq.AddTranslation("Major Family", English, "attribs", 0, Nothing, 0, 0), 0)
        End If

        If Not iq.i_attribute_code.ContainsKey("FamMinor") Then
            Dim famMin = New clsAttribute("FamMinor", iq.AddTranslation("Minor Family", English, "attribs", 0, Nothing, 0, 0), 0)
        End If

        If Not iq.i_attribute_code.ContainsKey("FamDisp") Then
            Dim famDisp = New clsAttribute("FamDisp", iq.AddTranslation("Family name (for display)", English, "attribs", 0, Nothing, 0, 0), 0)
        End If

        If Not iq.i_channel_code.ContainsKey("Root") Then Dim achannel As clsChannel = New clsChannel(Nothing, "All channels", "", "", "Root", r_worldwide, New nullableString(), New nullableString(), New nullableString(), 15, "tree.1", "", 0, 0, "R", "", "", Nothing, False, "", "", "")
        RootChannel = iq.i_channel_code("Root")

        If Not iq.i_channel_code.ContainsKey("HP") Then Dim achannel As clsChannel = New clsChannel(Nothing, "HP", "Hewlett Packard", "Hewlett-Packard Company,3000 Hanover(Street),Palo Alto, CA,94304-1185,USA", "HP", r_worldwide, New nullableString(), New nullableString(), New nullableString("http://www.hp.com"), 2, "tree.1", "", 0, 0, "R", "", "", iq.i_currency_code("USD"), False, "", "", "")
        HP = iq.i_channel_code("HP")

        If iq.Branches.Count = 0 Then Dim aBranch As clsBranch = New clsBranch(Nothing, Nothing, iq.AddTranslation("All HP Products", English, "UI", 0, Nothing, 0, False), "", iq.AddTranslation("Sectors", English, "collect", 0, Nothing, 0, False), iq.AddTranslation("Sector", English, "collect", 0, Nothing, 0, False), iq.i_screens_code("Servers"), 100, False, "S")
        RootBranch = iq.Branches(1)


        Dim Status As clsState
        If iq.i_state_GroupCode.ContainsKey("TH-InProg") Then
            Status = iq.i_state_GroupCode("TH-InProg")
        Else
            Status = New clsState("TH", "InProg", iq.AddTranslation("In progress", English, "ticks", 0, Nothing, 0, False), 10, "#a08040")
        End If

        Dim normal As clsState
        If iq.i_state_GroupCode.ContainsKey("PR-Normal") Then
            normal = iq.i_state_GroupCode("PR-Normal")
        Else
            normal = New clsState("PR", "Normal", iq.AddTranslation("Normal", English, "Priority", True, Nothing, 0, False), 50, "#30a040") 'green
        End If

        'If iq.Threads.Count = 0 Then
        '    Dim aThread As clsThread = New clsThread(sysAdmin, sysAdmin, Nothing, normal, Status, 100, "All Threads", New nullableString("This is the root of all threads - do not delete it ! (although that should be impossible)"), Nothing, Now, Now, True)
        'End If

        Dim reset As Boolean

        If iq.Products.Count = 0 Then reset = True
        Dim j As clsAttribute 'this is a 'throwaway' variable be use to create instances.. they are added (internally) to the IQ.Attributes 'master' list - holding a reference to them so they are not destroyed

        If Not iq.i_attribute_code.ContainsKey("name") Then j = New clsAttribute("Name", iq.AddTranslation("Name", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("desc") Then j = New clsAttribute("desc", iq.AddTranslation("Description", English, "UI", 0, Nothing, 0, False), 0)

        If Not iq.i_attribute_code.ContainsKey("cores") Then j = New clsAttribute("cores", iq.AddTranslation("Cores", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("threads") Then j = New clsAttribute("threads", iq.AddTranslation("Threads", English, "UI", 0, Nothing, 0, False), 0)


        If Not iq.i_attribute_code.ContainsKey("mfrSKU") Then j = New clsAttribute("mfrSKU", iq.AddTranslation("MfrSKU", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("PLcode") Then j = New clsAttribute("PLcode", iq.AddTranslation("PLcode", English, "UI", 0, Nothing, 0, False), 0)

        If Not iq.i_attribute_code.ContainsKey("mass") Then j = New clsAttribute("mass", iq.AddTranslation("Mass", English, "UI", 0, Nothing, 0, False), 0)

        If Not iq.i_attribute_code.ContainsKey("optFamily") Then j = New clsAttribute("optFamily", iq.AddTranslation("Option Family (legacy/import)", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("optType") Then j = New clsAttribute("optType", iq.AddTranslation("Option type (legacy/import)", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("famMinor") Then j = New clsAttribute("famMinor", iq.AddTranslation("Minor family (legacy/import)", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("famMajor") Then j = New clsAttribute("famMajor", iq.AddTranslation("System family (legacy/import)", English, "UI", 0, Nothing, 0, False), 0)

        If Not iq.i_attribute_code.ContainsKey("alsoHost") Then j = New clsAttribute("alsoHost", iq.AddTranslation("Additional host codes (comma seperated) - overrides usuaual geographic restrictions)", English, "UI", 0, Nothing, 0, False), 0)


        If Not iq.i_attribute_code.ContainsKey("incompat") Then j = New clsAttribute("incompat", iq.AddTranslation("Incompatible with subafamilies:(legacy/import)", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("altSKU") Then j = New clsAttribute("altSKU", iq.AddTranslation("Alternative Part:(legacy/import)", English, "UI", 0, Nothing, 0, False), 0)

        If Not iq.i_attribute_code.ContainsKey("speed") Then j = New clsAttribute("speed", iq.AddTranslation("Speed", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("capacity") Then j = New clsAttribute("capacity", iq.AddTranslation("Capacity", English, "UI", 0, Nothing, 0, False), 0)

        If Not iq.i_attribute_code.ContainsKey("cpuSKU") Then j = New clsAttribute("cpuSKU", iq.AddTranslation("CPU Part number (legacy/import)", English, "UI", 0, Nothing, 0, False), 0)

        If Not iq.i_attribute_code.ContainsKey("lifeCycle") Then j = New clsAttribute("lifeCycle", iq.AddTranslation("Life Cycle (months)", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("management") Then j = New clsAttribute("management", iq.AddTranslation("Management", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("security") Then j = New clsAttribute("security", iq.AddTranslation("Security", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("subTitle") Then j = New clsAttribute("subTitle", iq.AddTranslation("Sub Title", English, "UI", 0, Nothing, 0, False), 0)

        If Not iq.i_attribute_code.ContainsKey("displaySize") Then j = New clsAttribute("displaySize", iq.AddTranslation("Display Size", English, "UI", 0, Nothing, 0, False), 0) 'for laptops - so we can filter by size independent of resolution

        If Not iq.i_attribute_code.ContainsKey("SC") Then j = New clsAttribute("SC", iq.AddTranslation("Supply Chain", English, "UI", 0, Nothing, 0, False), 0)
        If Not iq.i_attribute_code.ContainsKey("focus") Then j = New clsAttribute("focus", iq.AddTranslation("Focus", English, "UI", 0, Nothing, 0, False), 0)


        If Not iq.Measures.ContainsKey(0) Then
            iq.Measures.Add(0, "None")
            Dim sql$
            sql$ = "SET IDENTITY_INSERT Measure ON;INSERT INTO [Measure] (ID,MeasureName) values (0,'None');SET IDENTITY_INSERT Measure OFF"
            da.DBExecutesql(sql$, True)
        End If

        If Not iq.i_unit_code.ContainsKey("txt") Then
            Dim Txt As New clsUnit("txt", iq.AddTranslation("Text", English, "UNITS", 0, Nothing, 0, False), "*", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("num") Then
            Dim num As New clsUnit("num", iq.AddTranslation("number", English, "UNITS", 0, Nothing, 0, False), "", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("year") Then
            Dim num As New clsUnit("year", iq.AddTranslation("year", English, "UNITS", 0, Nothing, 0, False), "", 0)
        End If


        If Not iq.i_unit_code.ContainsKey("hour") Then
            Dim num As New clsUnit("hour", iq.AddTranslation("hour", English, "UNITS", 0, Nothing, 0, False), "", 0)
        End If


        If Not iq.i_unit_code.ContainsKey("U") Then
            Dim U As New clsUnit("U", iq.AddTranslation("U", English, "UNITS", 0, Nothing, 0, False), "U", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("Feet") Then
            Dim ft As New clsUnit("Feet", iq.AddTranslation("Feet", English, "UNITS", 0, Nothing, 0, False), "ft", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("Inch") Then
            Dim Inch As New clsUnit("Inch", iq.AddTranslation("Inch", English, "UNITS", 0, Nothing, 0, False), "in", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("mm") Then
            Dim Milimeter As New clsUnit("mm", iq.AddTranslation("Milimeters", English, "UNITS", 0, Nothing, 0, False), "mm", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("cm") Then
            Dim Centimeter As New clsUnit("cm", iq.AddTranslation("Centimeter", English, "UNITS", 0, Nothing, 0, False), "cm", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("W") Then
            Dim Watt As New clsUnit("W", iq.AddTranslation("Watts", English, "UNITS", 0, Nothing, 0, False), "W", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("kW") Then
            Dim KW As New clsUnit("kW", iq.AddTranslation("KiloWatt", English, "UNITS", 0, Nothing, 0, False), "kW", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("kg") Then
            Dim Int As New clsUnit("kg", iq.AddTranslation("kg", English, "UNITS", 0, Nothing, 0, False), "kg", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("lb") Then
            Dim lb As New clsUnit("lb", iq.AddTranslation("lb", English, "UNITS", 0, Nothing, 0, False), "lb", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("C") Then
            Dim c As New clsUnit("C", iq.AddTranslation("Celcius", English, "UNITS", 0, Nothing, 0, False), "°C", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("F") Then
            Dim f As New clsUnit("F", iq.AddTranslation("Farenheit", English, "UNITS", 0, Nothing, 0, False), "°F", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("RPM") Then
            Dim rpm As New clsUnit("RPM", iq.AddTranslation("Revolutions per minute", English, "UNITS", 0, Nothing, 0, False), "r/min", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("Gbyte") Then
            Dim rpm As New clsUnit("Gbyte", iq.AddTranslation("Gigabytes", English, "UNITS", 0, Nothing, 0, False), "GB", 0)
        End If

        If Not iq.i_unit_code.ContainsKey("Gbit") Then
            Dim rpm As New clsUnit("Gbit", iq.AddTranslation("Gigabits", English, "UNITS", 0, Nothing, 0, False), "Gb", 0)
        End If

        If Not iq.i_ProductType_Code.ContainsKey("none") Then
            Dim npt As clsProductType = New clsProductType("none", iq.AddTranslation("none", English, "Prod", 0, Nothing, 0, False), 0)
        End If



        If Not iq.i_slotType_Code.ContainsKey("none") Then
            Dim nst As clsSlotType = New clsSlotType("none", "none", iq.AddTranslation("none", English, "slot", 0, Nothing, 0, False))
        End If

        If Not iq.i_slotType_Code.ContainsKey("CPU") AndAlso Not iq.i_slotType_Code.ContainsKey("GEN_CPU ") Then
            Dim nst As clsSlotType = New clsSlotType("CPU", "GEN_CPU", iq.AddTranslation("CPU", English, "slot", 0, Nothing, 0, False))
        End If

        '   If Not iq.Variants.ContainsKey(-1) Then
        ' Dim npt As clsProduct = iq.Products.Where(Function(f) f.Value.sku = "none").Select(Function(f) f.Value).FirstOrDefault
        ' If npt Is Nothing Then
        ' npt = New clsProduct("none", False, iq.i_sector_code("NoSector"), iq.i_ProductType_Code("none"), New Date(2014, 1, 1), New Date(2080, 1, 1), True, False, True)
        ' End If

        'Dim a = New clsVariant("none", npt, HP, "1234", "None", "", "", iq.i_region_code("XW"), False)
        '     End If

        iq.AddTranslation("Unknown", English, "avail", 0, Nothing, 0, False)
        iq.AddTranslation("Unstocked", English, "avail", 0, Nothing, 0, False)
        iq.AddTranslation("Unstocked Variant", English, "avail", 0, Nothing, 0, False)
        iq.AddTranslation("Hard Drives:", English, "UI", 0, Nothing, 0, False)

        Dim errormessages As List(Of String) = New List(Of String)
        If Not iq.i_screens_code.ContainsKey("Base") Then
            Dim base As clsScreen = New clsScreen("branch", "Base", "Base Grid", errormessages)
            Dim fld As clsField
            fld = New clsField(base, "ID", "", iq.AddTranslation("id", English, "FLDLBL", 1, Nothing, 0, False), "Primary key", Nothing, iq.i_inputType_code("string"), 10, 1, 10, 10, "", True, False, "", "", 1, Nothing, "", False, Nothing, True)
            fld = New clsField(base, "Product.i_Attributes_Code(MfrSKU)(0)", "", iq.AddTranslation("Part Number", English, "FLDLBL", 1, Nothing, 0, False), "HP Part Number", Nothing, iq.i_inputType_code("string"), 10, 1, 10, 10, "", True, False, "", "", 1, Nothing, "", False, Nothing, True)
        End If

        'Add any missing rights
        If Not iq.i_right_Code.ContainsKey("GLOBALADM") Then Dim r = New clsRight("GLOBALADM", iq.AddTranslation("Global Administrator", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("TAKEOVER") Then Dim r = New clsRight("TAKEOVER", iq.AddTranslation("Takeover Session ", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("PWDRESET") Then Dim r = New clsRight("PWDRESET", iq.AddTranslation("Password Reset", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("DISABLEUSR") Then Dim r = New clsRight("DISABLEUSR", iq.AddTranslation("Disable User", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("ENABLEUSR") Then Dim r = New clsRight("ENABLEUSR", iq.AddTranslation("Enable User", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("CREATEUSR") Then Dim r = New clsRight("CREATEUSR", iq.AddTranslation("Create User", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("VIEWALL") Then Dim r = New clsRight("VIEWALL", iq.AddTranslation("View All Products", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("TREEVIEW") Then Dim r = New clsRight("TREEVIEW", iq.AddTranslation("Treeview Access", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("DIAGVIEW") Then Dim r = New clsRight("DIAGVIEW", iq.AddTranslation("Diagnositcs View", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("FULLDIST") Then Dim r = New clsRight("FULLDIST", iq.AddTranslation("Full Distributor Access", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("EDITTREE") Then Dim r = New clsRight("EDITTREE", iq.AddTranslation("Edit and Add to Tree Structure", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("SEETEST") Then Dim r = New clsRight("SEETEST", iq.AddTranslation("View additional test Variants (AA - equiveilent)", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("SHOWERRORS") Then Dim r = New clsRight("SHOWERRORS", iq.AddTranslation("Enable show errors", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("SHOWALL") Then Dim r = New clsRight("SHOWALL", iq.AddTranslation("Access show all products", English, "Rights", 0, Nothing, 0, False))
        If Not iq.i_right_Code.ContainsKey("ADMINMENU") Then Dim r = New clsRight("ADMINMENU", iq.AddTranslation("Access show admin menu", English, "Rights", 0, Nothing, 0, False))


        '        If Not iq.i_right_Code.ContainsKey("EXPORTGRID") Then Dim r = New clsRight("EXPORTGRID", iq.AddTranslation("Export grid as CSV file", English, "Rights", 0, Nothing, 0, False))

        If Not iq.i_role_Code.ContainsKey("ADMIN") Then Dim r = New clsRole("ADMIN", iq.AddTranslation("Administrator", English, "Roles", 0, Nothing, 0, False))
        If Not iq.i_role_Code("ADMIN").i_right_code.ContainsKey("GLOBALADM") Then iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("GLOBALADM"))
        If Not iq.i_role_Code("ADMIN").i_right_code.ContainsKey("DIAGVIEW") Then iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("DIAGVIEW"))
        If Not iq.i_role_Code("ADMIN").i_right_code.ContainsKey("TREEVIEW") Then iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("TREEVIEW"))
        If Not iq.i_role_Code("ADMIN").i_right_code.ContainsKey("TAKEOVER") Then iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("TAKEOVER"))
        If Not iq.i_role_Code("ADMIN").i_right_code.ContainsKey("VIEWALL") Then iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("VIEWALL"))
        If Not iq.i_role_Code("ADMIN").i_right_code.ContainsKey("SHOWALL") Then iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("SHOWALL"))
        If Not iq.i_role_Code("ADMIN").i_right_code.ContainsKey("SHOWERRORS") Then iq.i_role_Code("ADMIN").AddRight(iq.i_right_Code("SHOWERRORS"))

        If Not iq.i_role_Code.ContainsKey("EDITOR") Then Dim r = New clsRole("EDITOR", iq.AddTranslation("Editor", English, "Roles", 0, Nothing, 0, False))
        If Not iq.i_role_Code("EDITOR").i_right_code.ContainsKey("EDITTREE") Then iq.i_role_Code("EDITOR").AddRight(iq.i_right_Code("EDITTREE"))
        If Not iq.i_role_Code("EDITOR").i_right_code.ContainsKey("VIEWALL") Then iq.i_role_Code("EDITOR").AddRight(iq.i_right_Code("VIEWALL"))
        If Not iq.i_role_Code("EDITOR").i_right_code.ContainsKey("TREEVIEW") Then iq.i_role_Code("EDITOR").AddRight(iq.i_right_Code("TREEVIEW"))

        If Not iq.i_role_Code.ContainsKey("USER") Then Dim r = New clsRole("USER", iq.AddTranslation("Basic User", English, "Roles", 0, Nothing, 0, False))

        If Not iq.i_role_Code.ContainsKey("DISTADMIN") Then Dim r = New clsRole("DISTADMIN", iq.AddTranslation("Distributor Admin", English, "Roles", 0, Nothing, 0, False))
        If Not iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("FULLDIST") Then iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("FULLDIST"))
        If Not iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("DISABLEUSR") Then iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("DISABLEUSR"))
        If Not iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("CREATEUSR") Then iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("CREATEUSR"))
        If Not iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("PWDRESET") Then iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("PWDRESET"))
        If Not iq.i_role_Code("DISTADMIN").i_right_code.ContainsKey("ADMINMENU") Then iq.i_role_Code("DISTADMIN").AddRight(iq.i_right_Code("ADMINMENU"))

        If Not iq.i_role_Code.ContainsKey("SUPPORT") Then Dim r = New clsRole("SUPPORT", iq.AddTranslation("Support", English, "Roles", 0, Nothing, 0, False))
        If Not iq.i_role_Code("SUPPORT").i_right_code.ContainsKey("SHOWALL") Then iq.i_role_Code("SUPPORT").AddRight(iq.i_right_Code("SHOWALL"))
        If Not iq.i_role_Code("SUPPORT").i_right_code.ContainsKey("SHOWERRORS") Then iq.i_role_Code("SUPPORT").AddRight(iq.i_right_Code("SHOWERRORS"))
        If Not iq.i_role_Code("SUPPORT").i_right_code.ContainsKey("ADMINMENU") Then iq.i_role_Code("SUPPORT").AddRight(iq.i_right_Code("ADMINMENU"))





        'SpecialBranches - is a crap idea and a stupid name
        ''CPQ branch
        If Not iq.i_SpecialBranches.ContainsKey("cpqroot") Then
            Dim r = New clsBranch(Nothing, Nothing, iq.AddTranslation("CPQ Root", English, "Root", 0, Nothing, 0, False), "", iq.AddTranslation("Roots", English, "Root", 0, Nothing, 0, True), iq.AddTranslation("Root", English, "Root", 0, Nothing, 0, True), Nothing, 0, True, "B", Nothing, 0)
            da.DBExecutesql(da.OpenDatabase(), "INSERT INTO SpecialBranch VALUES ('cpqroot'," & r.ID & ")")
            iq.i_SpecialBranches.Add("cpqroot", r)
        End If

    End Sub

    Public Function ErrorDymo(message As String, Optional lid As UInt64 = 0, Optional dismissable As Boolean = False, Optional extraStyle As String = "") As Literal
        'Returns a red Dymo-Tape style error message
        Dim lit As New Literal
        Dim displayErrors As Boolean = True

        'Dont do this otherwise we can't use the 'force' parameter (on outputerrors) to output critical errors 
        'The whole thing needs a bit of a rethink - with them going into the error log, having a 'severity' - and probably being list of some errrorCls (with timestamp, severity, errornumber, message, callstack - etc)
        'If lid > 0 Then
        ' displayErrors = CType(iq.sesh(lid, "ErrorDisplay"), Boolean)
        ' End If

        'ML - have removed the screen display on Rob's request for UAT, things will appear in the audit log now with code Dymo.
        ' AuditLog.Instance.Add(AuditType.Warning, message, "Dymo", lid)

        If displayErrors Then
            lit.Text = "<div><span class='errorLabel'"
            If extraStyle <> "" Then lit.Text &= " style='" & extraStyle & "'"
            lit.Text &= ">" & message
            If dismissable Then
                lit.Text &= MakeRoundButton("Dismiss.png", "Ignore this error", "this.parentNode.parentNode.style.display='none'", "", "", English).Text
            End If
            lit.Text &= "</span></div>"
        End If

        Return lit

    End Function

    Public Sub Logit(l$, Optional reset As Boolean = False, Optional flush As Boolean = False)

        'Logs acitivity messages to a file
        'Reset empties the file (and writes the 1 new line) (typicall called at the beginning)
        'Flush forces buffered conents to be written to the file. (typicall called at the end)
        'actually writing to the file (appending) is very slow (well, several milliseconds) - so we have a rotating buffer of 50 lines
        'and only do the acutal write once every 50 lines (or if we explicitly flush)

        'Exit Sub

        AuditLog.Instance.Add(AuditType.Information, l$, "", 0)
        Return

        Static line(500) As String    'static variables keep their values between calls
        Static linepointer As Integer

        line(linepointer) = l$
        linepointer += 1

        If reset Then

            Dim Sw = New StreamWriter("c:\temp\import.log", Not reset)
            Sw.WriteLine(l$)
            Sw.Close()

        Else

            If flush Or linepointer = 501 Then

                Dim Sw = New StreamWriter("c:\temp\import.log", Not reset)
                For i As Integer = 0 To linepointer - 1
                    Sw.WriteLine(line(i))
                Next i
                Sw.Close()
                linepointer = 0

            End If
        End If

    End Sub

    '                             hello                 fr      bonjour
    'Dim xlate As New Dictionary(Of String, Dictionary(Of String, String))

    Public xlate As Dictionary(Of String, Dictionary(Of String, String)) 'see BtnImport_click 

    Public Function XmlEscape(l$) As String

        'replaces some special HTML &thing; markups with their unicode equivilents
        'Removes any remaining ampersands

        l$ = Replace(l$, "&plusmn;", "^#x00B1;", , , Microsoft.VisualBasic.CompareMethod.Text)

        l$ = Replace(l$, "&", "&#038")

        XmlEscape = Replace(l$, "^", "&")

    End Function

    Public Function Xlt(ky As String, language As clsLanguage) As String

        Dim kyCompositeKey As String = ky & "^UI"
        If iq.KYIndex.ContainsKey(kyCompositeKey) Then
            'we have already created a translation  object for this kie
            Dim tlo As clsTranslation = iq.KYIndex(kyCompositeKey)
            If tlo.Group = "" Then
                tlo.Group = "UI"
                tlo.Update(language)
                If KYlanguage IsNot Nothing Then tlo.Update(KYlanguage)
            End If

            'Return UCase(iq.KYIndex(ky).text(language))  ' if the language version is not present - it will return the EN version then the KY version
            If (language Is English) Then
                Return iq.KYIndex(kyCompositeKey).text(language)
            Else
                Return iq.KYIndex(kyCompositeKey).text(language)
            End If
            'Use the ucase version above to SHOUT the things we dont have translations for
        Else

            If ky.Trim().Length > 0 And (language Is KYlanguage Or language Is English) Then

                ' NICK NOBBLED FOR NOR    
                Dim tl As clsTranslation = New clsTranslation(KYlanguage, ky, "UI", 0)
                If language Is English Then
                    tl = iq.AddTranslation(ky, English, "UI", 0, Nothing, 0, False) 'New clsTranslation(English, ky, "UI", 0)
                End If

                Dim cl As Object = iq.KYIndex(kyCompositeKey).text(language)
                If language Is English Then
                    Return If(cl IsNot Nothing, iq.KYIndex(kyCompositeKey).text(language), "Translation for key and group " & kyCompositeKey & " language ID " & language.ID & "is missing")
                Else
                    Return If(cl IsNot Nothing, iq.KYIndex(kyCompositeKey).text(language), "Translation for key and group " & kyCompositeKey & " language ID " & language.ID & "is missing")
                End If
            Else
                Return ky
            End If

        End If
    End Function
    ''' <summary></summary>
    ''' <param name="ky"></param>
    ''' <param name="text"></param>
    ''' <param name="language"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>

    Public Function AddXlt(ky As String, text As String, language As clsLanguage) As Boolean
        If iq.KYIndex.ContainsKey(ky) Then
            If language IsNot KYlanguage Then
                Dim kyTrans As clsTranslation = iq.KYIndex(ky)
                If kyTrans.textTranslation(language) = "" Then
                    kyTrans.addLanguage(language, text, Nothing)
                Else
                    If kyTrans.delete(language) Then
                        kyTrans.addLanguage(language, text, Nothing)
                    End If
                End If
            End If
            Return True
        Else
            If language Is KYlanguage Then
                Dim kyTrans As clsTranslation = New clsTranslation(language, ky, "UI", 0)
                Return True
            ElseIf language Is English Then
                Dim kyTrans As clsTranslation = New clsTranslation(iq.i_language_Code("KY"), ky, "UI", 0)
                kyTrans.addLanguage(language, text, Nothing)
                Return True
            Else
                Return False
            End If
        End If
    End Function
    Public Function NewLit(l$) As Literal
        NewLit = New Literal
        NewLit.Text = l$
    End Function
    Public Function NullIt(o As Object) As String

        'Returns NULL or a 'quoted' (and encoded) value (for strings) or an unquoted value for non strings - suitable for INSERTing

        NullIt = Nothing


        If o Is Nothing Then Return "NULL"
        If IsDBNull(o) Then Return "NULL"

        If TypeOf (o) Is Integer Then
            Return o.ToString
        End If
        If TypeOf (o) Is String Then
            Return da.SqlEncode(o.ToString)
        End If
        If TypeOf (o) Is nullableString Then
            Return da.SqlEncode(o.DisplayValue)
        End If

        Stop

    End Function


    Public Sub Serialize(O As Object, filename$)

        Dim objStreamWriter As New StreamWriter(filename$)
        Dim x As New XmlSerializer(O.GetType)
        x.Serialize(objStreamWriter, O)
        objStreamWriter.Close()

    End Sub

    Public Function GeneratePassword() As String

        Randomize(Math.Sin(Now.Millisecond / 287) * 100) 'Seeding just with the timer wouldn't be very secure

        Dim c$

        c$ = "ghjabcdefkmnpqrstwxy23456878" 'we don't use U's V's 1's I's or O's

        Dim pw$ = ""
        For i = 1 To 8
            pw$ &= Mid$(c$, 1 + Rnd(1) * (Len(c$) - 2), 1)
        Next
        pw = Left$(pw, 4) & "-" & Mid$(pw, 5) 'Split with a dash - for something of the form JK5-LTA
        Return pw

    End Function

    Public Sub DiscardUnChangedQuote(lid As UInt64)


        Dim buyerAccount As clsAccount = CType(iq.sesh(lid, "BuyerAccount"), clsAccount)

        If buyerAccount IsNot Nothing Then

            Dim todel As List(Of clsQuote) = New List(Of clsQuote)
            Dim state_new As clsState = iq.i_state_GroupCode("QT-#NW")

            'discards empty new quotes
            For Each q In buyerAccount.Quotes.Values
                If q.State Is state_new And q.RootItem.Children.Count = 0 Then
                    todel.Add(q) 'we can't directly delete them (becuase we're iteratring the collection we'd be removing them from)
                End If
            Next

            For Each q In todel
                If q.ID = iq.sesh(lid, "QuoteID") Then
                    iq.getSeshDic(lid).Remove("QuoteID") 'We just discarded a quote we'd started (and added nothing to)

                End If

                q.delete()
            Next

        End If

    End Sub

    Public Function UiTrans(l$) As String

        UiTrans = l$  'Pre login translatin function


    End Function
    Public Function md5(i$) As String

        Dim con As SqlConnection
        con = da.OpenDatabase()
        Dim rdr As SqlClient.SqlDataReader

        rdr = da.DBExecuteReader(con, "select dbo.md5(" & dataAccess.da.SqlEncode(i$) & ")")
        rdr.Read()
        md5 = rdr.Item(0)
        rdr.Close()
        con.Close()



    End Function
    Public Function Shuffle(i$) As String
        If Len(i$) = 35 Then i$ = Left$(i$, 32)
        If Len(i$) < 32 Then Return ""
        'Performs a simple (non commutative) 3 way cut/shuffle on a 32 byte MD5 hash
        'so that we're no longer storing 'standard' (easily reversible via dictionary lookup) MD5 hashes.
        'A hacker would now have to have the database, and have and Dissassemble the DLL's to find out passwords

        Dim r$ = i$
        r$ = Mid$(i$, 10, 10) & Mid$(i$, 1, 10) & Mid$(i$, 21)

        Return r$

    End Function



    Public Sub ShowError(ByVal ph As Panel, ByVal text As String)

        Dim em As Label
        em = New Label
        ph.Controls.Add(em)
        em.BackColor = Drawing.Color.Red
        em.ForeColor = Drawing.Color.White
        em.Text = text

    End Sub

    Public Function TimeSince(ByRef LastMilestone As Double) As String

        'Uses the system.diagnositic.stopwatch to return the elapsed time in Milliseconds (formatted to two decimal places) since LastMilestone
        'IMPORTANT: - this UPDTAES last milestone (to the current time.. so you can call is many times to measure the time between stages)

        Dim TimeNow As Double
        TimeNow = Stopwatch.GetTimestamp()
        TimeSince = ((TimeNow - LastMilestone) / Stopwatch.Frequency * 1000).ToString("0.00") & "ms"
        LastMilestone = TimeNow

    End Function

    'Public Function FindProductPaths(id As Integer) As Dictionary(Of String, String)

    '    'fetches the paths to all occurances of the SKU
    '    'returns a dictionary of Paths:PathNames

    '    FindProductPaths = New Dictionary(Of String, String)

    '    Dim con As SqlClient.SqlConnection
    '    con = da.opendatabase()
    '    Dim rdr As SqlClient.SqlDataReader
    '    Dim path$

    '    rdr = da.dbexecuteReader(con, "SELECT [path] FROM [ProductPath] WHERE fk_product_id=" & id & ";")
    '    If rdr.HasRows Then
    '        While rdr.Read
    '            path$ = rdr.Item("Path")
    '            FindProductPaths.Add(path$, PathName(path$, English))
    '        End While
    '    Else
    '        FindProductPaths.Add("", "[ProductPath] table is not populated - see default.aspx Index SKU's button")
    '    End If
    '    rdr.Close()
    '    con.Close()

    'End Function

    Public Function KWbreadcrumbs(lid As UInt64, path$, language As clsLanguage, isoptionsSearch As Boolean, greyed As Boolean, rfh As String, isDiagView As Boolean) As Panel

        'returns a panel containing clickable divs to every segment in the path 

        KWbreadcrumbs = New Panel
        KWbreadcrumbs.CssClass = "KWbreadcrumbs"

        Dim p$()
        p$ = Split(path$, ".")

        Dim branch As clsBranch
        Dim lowerbound As Integer = 1
        Dim pth$ = p$(0)
        Dim sysPath As String = ""
        If isoptionsSearch And Not isDiagView Then  'We present slightly modified breadcrumbs - only from the system.. down to the option(s)
            lowerbound = UBound(p) - 2
            For i = 1 To lowerbound - 1
                pth$ &= "." & p(i)
                If iq.Branches(p(i)).Product IsNot Nothing AndAlso iq.Branches(p(i)).Product.isSystem Then sysPath = pth
            Next
        Else
            lowerbound = 2 'nick
        End If

        For i = lowerbound To UBound(p) 'The 0'th item is 'tree'  (from tree.2.7.910.2005 etc) - the 1st is "Root Branch"
            Dim seg As Panel = New Panel

            If greyed Then
                seg.CssClass = "disabledKWcrumb" 'Inline-block
            Else
                seg.CssClass = "KWcrumb" 'Inline-block
            End If

            KWbreadcrumbs.Controls.Add(seg)
            pth$ &= "." & p(i)

            Dim pdesc As String = ""

            branch = iq.Branches(p(i))
            If Not branch.Product Is Nothing Then
                If branch.Product.i_Attributes_Code.ContainsKey("desc") Then
                    Dim pa As clsProductAttribute = branch.Product.i_Attributes_Code("desc")(0)
                    pdesc = pa.Translation.text(language).Replace("[mfr]", branch.Product.mfrCode)
                End If
            End If

            If greyed Then
                seg.ToolTip = "Unavailable product (" & rfh & ")"
            Else
                seg.ToolTip = pdesc
            End If


            Dim root As String = iq.sesh(lid, "Root")
            'seg.Attributes("onclick") = "hideKeywordSearchResults();getBranches('path=" & pth & "&cmd=open&into=tree');return false;"

            'The difference between these and 'normal' breadcrumbs is that the branches are not yet open.
            'See - proccesCommand() in showchildren.aspx

            Dim pathToSeg As String = Join(p.Take(i + 1).ToArray, ".")

            'disabled (greyed) lines have no onclick events (because navigating to things not in the portfolio yields a host of problems)
            If isoptionsSearch Then  'we remain in the configuringSystem paradigm
                seg.Attributes("onclick") = String.Format("hideKeywordSearchResults();getBranches('cmd=open&path={0}&to={1}&into=tree');return false;", sysPath, pathToSeg)
            Else
                If iq.Branches(Split(path, ".").Last).Product IsNot Nothing AndAlso iq.Branches(Split(path, ".").Last).Product.isSystem AndAlso pathToSeg = path Then
                    'clicked on a system (this MUST BE) a systems search
                    seg.Attributes("onclick") = String.Format("hideKeywordSearchResults();getBranches('cmd=open&path=tree.1&to={0}&Paradigm=B&into=tree&showOnly={1}');return false;", pathToSeg, p.Last)
                ElseIf i = lowerbound Then  ' Breadcrumbs root item
                    seg.Attributes("onclick") = String.Format("hideKeywordSearchResults();getBranches('cmd=open&path={0}&configuration=0&Paradigm=B&into=tree');return false;", pathToSeg)
                Else
                    'clicked on something that isn't a system - whilst in a system search
                    'we need to change paradigm to enumparadigm.addingsystem (aka Browsing)
                    seg.Attributes("onclick") = String.Format("hideKeywordSearchResults();getBranches('cmd=open&Paradigm=B&path=tree.1&to={0}&into=tree&configuration=0');return false;", pathToSeg)
                End If
            End If

            seg.Controls.Add(NewLit(" ▶" & branch.DisplayName(language)))

            If isDiagView Then
                If branch.Product IsNot Nothing AndAlso branch.Product.i_Attributes_Code.ContainsKey("FamMinor") Then
                    seg.Controls.Add(NewLit(" (" & branch.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English) & ")"))
                End If
            End If

            'seg.Attributes("title") = pathToSeg

            If p(i) = p.Last Then
                Dim LINETEXT = pdesc
                If LINETEXT <> "" Then
                    Dim MAXLEN As Integer = 150
                    If Len(LINETEXT) > MAXLEN Then LINETEXT = Left(LINETEXT, MAXLEN) & ".."
                    'this is the light grey description 'result' (which is set earlier into the tooltip on the last segment)
                    seg.Controls.Add(NewLit("<div CLASS='searchLine'>" & LINETEXT & "</div>"))
                End If
            End If

        Next

    End Function

    Public Function FQP(path$, branch As clsBranch) As String

        'returns the fully qualified path from a specified branch and 'address'
        'For example .. from tree.123.345  will return tree.123
        'at present only simple .. and ... operations are supported

        If path$ = ".." Or path$ = "..." Then

            Dim p() As String
            p = Split(path$, ".")

            Dim r$ = ""
            For i = 0 To UBound(p) - (Len(path$) - 1)
                r$ &= p(i) & "."
            Next

            r$ = Left$(r$, Len(r$ - 1))

            Return r$
        Else
            Return path$
        End If

    End Function

    Public Function MakeSpannedHeader(Styleprefix As String, l$) As Panel

        MakeSpannedHeader = New Panel

        Dim p$() = Split(l$, ",")
        Dim celltext As String

        For i As Integer = 0 To UBound(p$)

            celltext = p(i)
            If Left$(celltext, 1) = "!" Then celltext = "" 'the title is not displayed for any column starting with a ! (however it still has a css class)

            MakeSpannedHeader.Controls.Add(NewLit("<div class='" & Styleprefix & Trim$(p$(i)) & "'>" & celltext & "</div>"))
        Next

    End Function
    Public Function MakeTHR(l$, tooltips As String, css As String) As TableHeaderRow
        'Constructs a table header row from a comma delimited list

        MakeTHR = New TableHeaderRow

        Dim p$() = Split(l$, ",")

        Dim t$()
        t$ = Split(tooltips, "|")

        Dim acell As TableCell
        For i = 0 To UBound(p$)
            acell = New TableHeaderCell
            acell.Text = p(i)

            If UBound(t) > 0 And i < t.Length - 1 Then
                acell.ToolTip = t(i)
            End If

            MakeTHR.Cells.Add(acell)

        Next

        MakeTHR.CssClass &= " " & css

        'job done - return value is the completed table header row <TH>

    End Function

    Public Function outputMessages(msgs As List(Of String)) As Panel


        outputMessages = New Panel
        Dim lit As Literal
        For Each msg In msgs
            lit = New Literal
            lit.Text = "<div><span class='messageLabel'>" & msg
            lit.Text &= MakeRoundButton("Dismiss.png", "Ignore this error", "this.parentNode.parentNode.style.display='none';return(false);", "", "", English).Text
            lit.Text &= "</span></div>"
            outputMessages.Controls.Add(lit)
        Next

    End Function


    Public Sub OutputErrors(ByRef cnts As ControlCollection, ByRef msgs As List(Of String), lid As UInt64, Optional Force As Boolean = False)

        'Anything starting with a * will only appear to users with "ErrorDispay" on (for which they need a right to the button)
        Dim OutputErrors As Panel

        Dim displayerrors As Boolean = False

        If lid <> 0 Then
            displayerrors = CType(iq.sesh(lid, "ErrorDisplay"), Boolean)
        End If

        Dim showall As Boolean = CType(iq.sesh(lid, "showAll"), Boolean)
        If showall Then displayerrors = True

        OutputErrors = New Panel
        For Each m In msgs
            If Not m.StartsWith("*") Or displayerrors Then
                cnts.Add(ErrorDymo(m, lid, True))
            End If
        Next
        cnts.Add(OutputErrors)

        msgs.Clear()

        ' End If

    End Sub

    Public Function TreeAddButton(tb As TextBox, path$, branch As clsBranch, skuvariant As clsVariant, language As clsLanguage, enabled As Boolean, disabledMessage As String) As Panel 'PlaceHolder

        'This is the flex up/add button that appears in the product tree
        'NB: there is no corresopnding flex down button as items cannot be flexed down via the product tree (only in the basket - as the basket may contain multiple instances of th eproduct in question))

        'Dim p As New PlaceHolder
        TreeAddButton = New Panel

        ' If adding isn't enabled (e.g. HPI/HPE split), set the style/message and exit before setting up the add to basket script
        If Not enabled Then
            TreeAddButton.CssClass = "treeAddButtonDisabled UI"
            TreeAddButton.Attributes.Add("onmousedown", String.Format("burstBubble(event); displayAddMsg('{0}', '{1}');", tb.ID, disabledMessage))
            Exit Function
        End If

        Dim script$

        TreeAddButton.CssClass = "treeAddButton UI"

        'we pass 'absolute' as false - to say add this *relative* amount to the quote

        'changeQty(boxID, itemID,path,SKUvariantID,absolute) {
        script$ = "burstBubble(event);hideKeywordSearchResults();setToOneIfBlank('" & tb.ID & "');sourceQty='" & tb.ID & "';"

        Dim productSystem As String = ""

        If branch.Product.isSystem(path) Then
            productSystem = "true"
        Else
            productSystem = "false"
        End If


        script$ &= "changeQty('" & tb.ID & "',0,'" & path$ & "'," & skuvariant.ID & ",false, " & productSystem & ");blank('" & tb.ID & "');return false;"
        'Automatic 'show in tree' if a system is added (to the basket)
        'If branch.Product.isSystem Then
        'setTimeout (function(){fillPrices('" & path & "'," & requestHandle & ");return false;},3000)"   'fill Prices Calls GetPriceUis.aspx for - 3000 gives the page time to render - any less time and the filling becomes unreliable
        'script$ &= "setTimeout(function(){getBranches('path=" & path & "&cmd=open&into=tree');return false;},200);"
        ' End If
        ' script$ &= "return false;"

        'tb.Attributes.Add("onKeyUp", "if(keyIs(e){" & script$ & "};") 'For when they press enter (add the quantity in the textbox to the quote)

        tb.Attributes.Add("onkeydown", "if(event.keyCode==13){" & script$ & "};") 'For when they press enter (add the quantity in the textbox to the quote)
        'tb.Attributes("onmousedown") = "burstBubble(event);" 'was on mousedown

        'Dim imgBtnFlexUp As New WebControls.Image 'Button
        TreeAddButton.Attributes("onmousedown") = script$ 'was onclick

        'imgBtnFlexUp.ImageUrl = "/images/navigation/plus.png"
        'imgBtnFlexUp.CssClass = "treeAddButton"
        TreeAddButton.ToolTip = Xlt("Add a quantity to the quote", language)
        ' Dim lit As Literal = New Literal
        ' lit.Text = "&nbsp;"
        ' TreeAddButton.Controls.Add(lit)

        'p.Controls.Add(imgBtnFlexUp)

        'Return p

    End Function

    Public Function TranslationKey(t As clsTranslation) As String

        If t Is Nothing Then Return "null"
        'If t Is DBNull.Value Then Stop
        Return t.Key

    End Function


    Public Sub SendEmail(toAddress As String, templatename As String, tags As Dictionary(Of String, String), language As clsLanguage, ByRef errorMessages As List(Of String), HighPriority As Boolean, Optional attachment As System.Net.Mail.Attachment = Nothing)


        'Subject - i pulled form the <subject> tag INSIDE the template
        'Reads the email template (currently from the file system - may move to DB)
        'Replaces the [tags] in the dictionary - which are tag,value pairs

        Dim e$ = ""

        Dim ppath As String = ""
        Dim vpath As String = ""

        Try
            vpath = HttpContext.Current.Request.ApplicationPath
            ppath = HttpContext.Current.Request.MapPath(vpath)

            Dim tr As StreamReader = Nothing

            Try
                tr = New StreamReader(ppath & "/EMT/" & templatename)
                e$ = tr.ReadToEnd()
                tr.Close()
            Catch
                tr.Dispose()
            End Try

            Dim regex As Regex = New Regex("\|[A-z\ 0-9]+\|") ' ML translate anything between |'s
            Dim matches = regex.Matches(e$)
            For Each m As Match In matches
                e$ = Replace(e$, m.Value, iq.AddTranslation(m.Value.Trim("|".ToArray()), English, "Export", 0, Nothing, 0, False).text(language))
            Next

            For Each t In tags.Keys
                e = e.Replace("[" & t & "]", tags(t))  'brackets are easier to deal with in the IDE (which inisist on trying to close <tags>)
            Next
        Catch ex As System.Exception

            errorMessages.Add(" Error peparing email ")
            errorMessages.Add("* Vpath:" & vpath)
            errorMessages.Add("* Ppath:" & ppath)

            ErrorLog.Add(ex)

        End Try

        Dim subject As String = "HP iQuote"

        Dim s As Integer = InStr(e, "<subject>")
        If s Then
            'This is pretty ugly - it just pulls the contents out of the <subject></subject> tag
            subject = Mid(e, InStr(s, e, ">") + 1)
            subject = Left(subject, InStr(subject, "<") - 1)
        End If

        Dim msg As MailMessage = Nothing
        Dim smtpclient As System.Net.Mail.SmtpClient = Nothing
        Try
            smtpclient = New System.Net.Mail.SmtpClient

            'you can't just change the from address to hpiquote.NET !
            msg = New MailMessage(iq.Addresses("iQuoteSupportEmail").Translation.text(English), toAddress, subject, e$)
            msg.ReplyToList.Add(New MailAddress(iq.Addresses("iQuoteSupportEmail").Translation.text(English)))
            'msg.Bcc.Add(New MailAddress("nick.axworthy@channelcentral.net"))
            msg.IsBodyHtml = True

            If attachment IsNot Nothing Then
                msg.Attachments.Add(attachment)
            End If
            If HighPriority Then
                msg.Priority = MailPriority.High
            End If
        Catch ex As Exception
            errorMessages.Add("* Error building mail" & ex.Message)
            errorMessages.Add("Unable to send email at this time")
        End Try

        If msg IsNot Nothing And smtpclient IsNot Nothing Then
            Try
                smtpclient.ServicePoint.MaxIdleTime = 1
                smtpclient.Send(msg)
                msg.Dispose()
            Catch ex As System.Exception

                errorMessages.Add("Send email failed")
                errorMessages.Add("* " & ex.Message)
                If ex.InnerException IsNot Nothing Then
                    errorMessages.Add(ex.InnerException.Message)
                End If

            End Try
        End If

    End Sub

    Public Function SimpleEmail(to$, Subject$, body$) As Boolean

        SimpleEmail = True
        Dim smtpclient As System.Net.Mail.SmtpClient = Nothing
        Dim msg As MailMessage
        smtpclient = New System.Net.Mail.SmtpClient

        msg = New MailMessage("support@channelcentral.net", to$, Subject, body$)
        '      msg.ReplyToList.Add(New MailAddress("dan.mason@channelcentral.net"))
        ' msg.CC.Add(New MailAddress("nick.axworthy@channelcentral.net"))
        msg.IsBodyHtml = True
        msg.Priority = MailPriority.High

        Try
            smtpclient.ServicePoint.MaxIdleTime = 1
            smtpclient.Send(msg)
            msg.Dispose()
        Catch ex As System.Exception
            SimpleEmail = False
        End Try

    End Function


    Public Sub postpaint(chart As Object, e As ChartPaintEventArgs)

        If TypeOf (e.ChartElement) Is Chart Then


            Dim g As System.Drawing.Graphics = e.ChartGraphics.Graphics



            'Dim installedFontCollection As New System.Drawing.Text.InstalledFontCollection()

            ' Get the array of FontFamily objects.
            ' Dim fontFamilies() As FontFamily
            ' fontFamilies = installedFontCollection.Families

            Dim DrawFont As Drawing.Font = New System.Drawing.Font("HP simplified w04 regular", 8) 'System.Drawing.Font.'System.Drawing.SystemFonts.CaptionFont

            Dim drawbrush As System.Drawing.Brush = Drawing.Brushes.Black

            '// see how big the text will be
            ' Dim txtWidth As Integer = g.MeasureString(txt, DrawFont).Width
            ' Dim TxtHeight As Integer = g.MeasureString(txt, DrawFont).Height
            '// where to draw

            Dim x As Integer = 5
            Dim y As Integer = CInt(e.Chart.Height.Value) - 10

            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
            g.ScaleTransform(1, 1)

            Dim v(0) As System.Drawing.Point

            g.ResetTransform()

            g.TranslateTransform(x, y)
            g.RotateTransform(-50)
            '        v(0) = New System.Drawing.Point(20, 0)  'a vector of 20 pixels 'accross' in device space (pixels)
            g.TransformPoints(Drawing2D.CoordinateSpace.World, Drawing2D.CoordinateSpace.Device, v)

            g.DrawString(chart.Attributes("text"), DrawFont, drawbrush, 0, 0)
            'x += 100
        End If

    End Sub

    Public Function MakeFilterButtons(language As clsLanguage) As Panel

        'Makes the full set of every possibly filter button
        'The correct ones are subsequently shown/hidden by the JS showFilterButtons

        MakeFilterButtons = New Panel
        MakeFilterButtons.ID = "filterButtons" 'the master page prepends ctl00_   !!!!argh !
        MakeFilterButtons.Attributes("Style") = "display:none;position:relative;z-index:150;width:0px;"  'was none
        MakeFilterButtons.CssClass = "filterButtons moreFilterButtonStyle"
        'MakeFilterButtons.Attributes("Style") &= ""

        Dim sc$
        sc$ = "onSpeechBubble=true;return false;"
        MakeFilterButtons.Attributes("onmouseover") = sc$

        sc$ = "onSpeechBubble=false;display('ctl00_filterButtons','none');return false;"
        MakeFilterButtons.Attributes("onmouseleave") = sc$

        Dim sib As WebControls.Image '(in cell - sample filter button)



        'returns the physical path to the file created
        'find the virtual, and from that the physical path
        Dim vPath = HttpContext.Current.Request.ApplicationPath
        Dim pPath = HttpContext.Current.Request.MapPath(vPath)

        Dim lt As Literal = New Literal

        lt.Text = "<!--bPath " & vPath$ & "-->"
        MakeFilterButtons.Controls.Add(lt)

        lt = New Literal
        lt.Text = "<!--PPath " & pPath$ & "-->"
        MakeFilterButtons.Controls.Add(lt)


        For Each f In iq.Filters.Values 'filterKey In iq.i_Filters_Code.Keys 'filters.Keys
            sib = New WebControls.Image 'Button

            '@@@

            Dim ih As String = ""

            If My.Computer.FileSystem.FileExists(pPath$ & "/images/navigation/" & f.Code & ".png") Then
                sib.ImageUrl = "/images/navigation/" & f.Code & ".png"
            Else
                sib.ImageUrl = "/images/navigation/genericfilter.png"  '" & f & ".png"
                ih = "(missing " & pPath & "/images/navigation/" & f.Code & ".png)"
            End If

            sib.ToolTip = f.DisplayText.text(language) & ih
            'sib.Attributes.Add("filter",  iq.filters(fi))")
            sib.Attributes.Add("code", f.Code)
            sib.ID = "FIB_" & f.Code
            sib.CssClass = "FB" 'used to get all of the buttons in the jscript - see showFilterButtons()

            'filterField,filterPath and FilterVale are global variables in iQuote.js - they are set in the onmouseover scripts of the matrixUI

            Dim occ$
            occ$ = "burstBubble(event);getBranches('path='+filterPath+'&cmd=changeFilter&filterParams='+ filterField + '|" & f.Code & "|' + filterValue);onSpeechBubble=false;return false;"

            'sib.ToolTip = occ$
            sib.Attributes("onclick") = occ$
            'sib.Attributes("onclick") = occ$
            sib.Width = New Unit(18, UnitType.Pixel)
            MakeFilterButtons.Controls.Add(sib)

        Next

    End Function

    Public Function NothingFromNull(v As Object) As Object

        If TypeOf (v) Is DBNull Then Return Nothing Else Return v

    End Function


    Public Class clsKwScore
        Public majorMatchBits As Integer = 0
        Public minorMatchbits As Integer = 0
        Public MajorMatchCount As Integer = 0 'the total count of fragment macthes in titles, part numbers etc
        Public MinorMatchCount 'the total count of fragment matches in things like descriptions, attribtues etc

    End Class
    Public Sub SavePromoBranches(branches As List(Of clsBranch), buyerChannel As clsChannel, type As String)

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim dt As DataTable = da.MakeWriteCacheFor(con, "PromoBranch")

        Dim row As System.Data.DataRow
        For Each branch In branches
            row = dt.NewRow
            row("FK_Branch_id") = branch.ID
            row("FK_Channel_ID_Buyer") = buyerChannel.ID
            row("promoType") = type
            dt.Rows.Add(row)
        Next

        da.BulkWrite(con, dt, "PromoBranch")

        con.Close()

    End Sub

    Public Function PromoUpdated(buyerChannel As clsChannel, type As String) As DateTime

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()
        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, "SELECT max(timestamp) from PromoScan WHERE fk_channel_id_buyer=" & buyerChannel.ID & " AND [PromoType]='" & type & "'")

        rdr.Read()
        If IsDBNull(rdr.Item(0)) Then
            PromoUpdated = DateAdd(DateInterval.Day, -100, Now)
        Else
            PromoUpdated = rdr.Item(0)
        End If

        rdr.Close()
        con.Close()

    End Function


    ''' <summary>It is expensive to work out which branches feature a valid (possibly deeply descendant) Promo in realtime - so we create/cache a list (of those branches - per buyerChannel) at logon</summary>
    ''' <remarks>We do this per buyerchannel (rather than per region).. becuase some of the promo products may be hidden (from a particular buyer) for other reasons (such as not being in the feed)</remarks>
    Public Sub TagPromoBranches(buyeraccount As clsAccount, errormessages As List(Of String))

        Dim age As Single

        Dim dontCheckWebService As Int16 = buyeraccount.SellerChannel.priceConfig And Not 8

        For Each t In Split("A B F") 'Avalance, Bundles and Flex

            If Not iq.PromoBranches.ContainsKey(buyeraccount.BuyerChannel) Then iq.PromoBranches.Add(buyeraccount.BuyerChannel, New Dictionary(Of String, List(Of clsBranch)))
            If Not iq.PromoBranches(buyeraccount.BuyerChannel).ContainsKey(t) Then iq.PromoBranches(buyeraccount.BuyerChannel).Add(t, New List(Of clsBranch))

            If da.DatabaseAlive Then 'Just testing, remove!

                age = DateDiff(DateInterval.Hour, PromoUpdated(buyeraccount.BuyerChannel, t), Now)

                If age > 4 Then

                    Dim branches As List(Of clsBranch)
                    branches = New List(Of clsBranch)

                    da.DBExecutesql("DELETE FROM PromoBranch WHERE promotype='" & t & "' and fk_channel_id_buyer=" & buyeraccount.BuyerChannel.ID)

                    'TODO - note we're not checking focus here - so if we were looking for (for example) avalanche within receta - this wouldn't work
                    Select Case t
                        Case Is = "A"
                            'branches = iq.RootBranch.checkAvalanche(Nothing, buyeraccount, New List(Of String), "tree." & Trim$(iq.RootBranch.ID), False, dontCheckWebService)  'Circa 3 seconds
                        Case Is = "B"
                            '          branches = iq.RootBranch.CheckBundles(buyeraccount, New List(Of String), "tree." & Trim$(iq.RootBranch.ID), False, dontCheckWebService)
                        Case Is = "F"
                            iq.RootBranch.checkFlex(buyeraccount, New HashSet(Of String), "tree." & Trim$(iq.RootBranch.ID), dontCheckWebService, branches, errormessages)
                    End Select

                    'Bulk writes them to the databse
                    If branches IsNot Nothing Then
                        If branches.Count Then
                            SavePromoBranches(branches, buyeraccount.BuyerChannel, t)
                            da.DBExecutesql("INSERT INTO PromoScan (fk_channel_id_buyer,promotype,timestamp) VALUES (" & buyeraccount.BuyerChannel.ID & ",'" & t & "',getdate());")
                        End If
                    End If
                End If

                iq.PromoBranches(buyeraccount.BuyerChannel)(t) = loadPromoBranches(buyeraccount.BuyerChannel, t)
            End If
        Next

    End Sub

    Public Function loadPromoBranches(BuyerChannel As clsChannel, type As String) As List(Of clsBranch)

        loadPromoBranches = New List(Of clsBranch)

        Dim con As SqlClient.SqlConnection = da.OpenDatabase()

        Dim rdr As SqlClient.SqlDataReader = da.DBExecuteReader(con, "SELECT FK_branch_id FROM [PromoBranch] WHERE fk_channel_id_buyer=" & BuyerChannel.ID & " AND [PromoType]='" & type & "'")

        Dim branch As clsBranch
        While rdr.Read
            Dim bid As Integer = rdr.Item("fk_branch_id")
            branch = iq.Branches(bid)
            loadPromoBranches.Add(iq.Branches(rdr.Item("fk_branch_id")))
        End While
        rdr.Close()

        con.Close()

    End Function

    Public Function xmlEncode(l$) As String


        xmlEncode = HttpUtility.HtmlEncode(l$)

        Exit Function

        'There's probably some intrinsic way to do this - but after 10 minutes of googling i counldn't find it - so here we go
        If l$ <> "" Then
            Dim p() As String
            For Each sr In Split("&=&amp;|<=&lt;|>=&gt;|'=&apos;|" & Chr(34) & "=&quot;", "|") 'create and iterate a list of the symbol=replacement pairs
                p = Split(sr, "=") 'split the sybmol into p(0), and it's replacement into p(1) at the "="
                l$ = Replace(l$, p(0), p(1)) 'there, that was easy wasn't it  (all the complexities of replacing &'s with &'s are handled for us
            Next
        End If

        Return l$ 'tada

    End Function

    Public Function rdus(l$) As String

        'replaces all dots with underscores (for valid control names) based on paths
        Return Replace(l$, ".", "_")

    End Function

    Public Function FindBranchByName(path, name) As clsBranch

        FindBranchByName = Nothing

        Dim segs() As String = Split(path, ".")
        Dim branch As clsBranch
        For i = UBound(segs) To 1 Step -1
            branch = iq.Branches(segs(i))
            If branch.Translation IsNot Nothing AndAlso branch.Translation.text(English) = name Then Return branch
        Next

        Return Nothing
    End Function


    Public Function FindSystemBranch(path$) As clsBranch

        FindSystemBranch = Nothing

        Dim segs() As String = Split(path$, ".")
        Dim branch As clsBranch
        For i = UBound(segs) To 1 Step -1
            branch = iq.Branches(segs(i))
            If branch.Product IsNot Nothing Then
                If branch.Product.isSystem And Not branch.Product.isOption Then Return branch 'DONT find tabe drives !
            End If
        Next

    End Function

    Public Function matrixHeaderAbove(lid As UInt64, path$, ByRef errormessages As List(Of String)) As clsScreenHeader

        Dim mh As Dictionary(Of String, clsScreenHeader) = iq.sesh(lid, "screenHeaders")
        Dim ppath$ = path$

        'If ppath = "tree" Or ppath = "" Or ppath = "swift" Then Return New clsScreenHeader(lid, path, iq.Screens(719))

        Do
            Dim branch As clsBranch
            branch = iq.Branches(CInt(Split(ppath, ".").Last))
            If branch.Product IsNot Nothing Then
                If branch.Product.isSystem Then
                    If path$ = ppath$ Then
                        ppath = oneAbove(ppath)
                    Else
                        Exit Do 'do NOT cross systems when looking back up through to find the effectivematrix
                    End If
                End If
            End If

            If mh.ContainsKey(ppath) Then
                matrixHeaderAbove = mh(ppath)
                Exit Function
            End If
            ppath = oneAbove(ppath)
            If ppath = "tree" Or ppath = "" Or ppath = "swift" Then Exit Do
        Loop

        'errormessages.Add("couldn't locate matrixHeaderAbove" & path$)
        ' Return Nothing
        Return Nothing 'New clsScreenHeader(path, iq.Screens(719))

    End Function


    Public Function MatrixAbove(lid As UInt64, path$) As clsScreen

        'returns the first ClsScreen referenced by a branch above the path 

        Dim i As Integer
        Dim matrix As clsScreen = Nothing
        Dim branch As clsBranch = Nothing
        Dim pth = path
        Dim sysabove = isSystemAbove(lid, path)
        Dim segs() As String = path.Split(".")
        For i = segs.Count - 1 To 1 Step -1   'note.. seg(0) is 'tree'
            pth = Left(pth, Len(pth) - Len(segs(i)) - 1)
            If pth = "tree" OrElse isSystemAbove(lid, pth) <> sysabove Then Exit For
            branch = iq.Branches(Val(segs(i)))
            Dim bn$ = branch.Translation.text(English)
            matrix = branch.Matrix
            If matrix IsNot Nothing Then
                Exit For
            End If
        Next
        If matrix Is Nothing Then matrix = iq.i_screens_code("Servers")

        Return matrix

    End Function
    ' ''' <summary>
    ' ''' Works out the available width in ems from the state of every branch (above this one) in the path - (most DIVs will subtract 2ems which comes from the treeindent class - however breadcrumbs don't
    ' ''' </summary>
    ' ''' <param name="lid">handle to the users sesh variables</param>
    ' ''' <param name="path"></param>
    ' ''' <returns></returns>
    ' ''' <remarks></remarks>
    'Public Function emsAvailable(lid As UInt64, ByVal path$, ByVal treewidth As Single) As Single
    '    'note params are passed byval (are copies not references) so we don't mess up the originals

    '    emsAvailable = treewidth

    '    Dim branchStates As Dictionary(Of String, clsBranchState) = iq.sesh(lid, "branchStates")
    '    If InStr(path$, ".") = 0 Then Exit Function 'Swift 'paths' have no .'s  


    '    'terrible hack
    '    If UBound(Split(path, ".")) > 4 Then
    '        emsAvailable = treewidth - 8
    '    Else
    '        emsAvailable = treewidth - 2.5
    '    End If


    '    Exit Function

    '    Do
    '        path$ = Left$(path$, InStrRev(path$, ".") - 1)

    '        If path$ <> "tree" Then 'yuck - but its late and i'm tried TODO
    '            If branchStates.ContainsKey(path) Then  'some placeholding branches are never rendered
    '                If branchStates(path$).renderAs <> bt.BreadCrumb Then
    '                    emsAvailable -= 2.25 ' every indent reduces the available space
    '                End If
    '            End If

    '        End If

    '    Loop Until path$ = "tree" Or path$ = "swift"


    'End Function

    Public Function oneAbove(path$) As String

        oneAbove = Left(path, InStrRev(path, ".") - 1)

    End Function


    Public Function PathName(path$) As String  'aka FullPath

        PathName$ = "."
        Dim seg() As String
        seg = path.Split(".")
        For i = 1 To UBound(seg)
            If iq.Branches.ContainsKey(seg(i)) Then
                PathName &= "/" & iq.Branches(Val(seg(i))).DisplayName(English)
            Else
                PathName &= "/???" & seg(i) & "???"
            End If

        Next
    End Function

    'Public Function RequiringUpdate(branches As Dictionary(Of String, clsBranch), buyeraccount As clsAccount) As Dictionary(Of String, ClsProductVariant)


    '    'Returns a dictionary of DistiSKUs > our ProductVariant
    '    'Our branches carry Product-Variants
    '    'we need get a list of DistiSkus that represent those product-variants (and return a dictionary)

    '    RequiringUpdate = New Dictionary(Of String, ClsProductVariant)


    '    For Each b In branches.Values
    '        If Not b.Product Is Nothing Then


    '            'variant key
    '            Dim vdic As Dictionary(Of clsVariant, String)
    '            vdic = buyeraccount.SellerChannel.ChannelSKUs(b.Product) 'fetch the dictionary of Variants to DistiSKUs
    '            For Each k In vdic.Keys    'This disti can have SKUs for several variants of this (branches) product 
    '                '
    '                Dim prices As List(Of IQ.clsPrice)
    '                prices = b.Product.GetPrices(buyeraccount, 9, k)
    '                If prices.Count <> 1 Then Stop 'but for each variant there should only be one price !

    '                Dim minutesold As Integer = DateDiff(DateInterval.Minute, prices(0).DateStamp, Now)
    '                If minutesold < 0 Then Stop
    '                If minutesold > 60 Or prices(0).Price.valid = False Then  'fetch a new price forall POAs
    '                    If Not RequiringUpdate.ContainsKey(vdic(k)) Then
    '                        RequiringUpdate.Add(vdic(k), New ClsProductVariant(b.Product, k))
    '                    Else
    '                        Beep()
    '                    End If
    '                End If
    '            Next
    '        End If
    '    Next

    'End Function

    'Public Sub AppendDic(ByRef a As Dictionary(Of Object, Object), ByRef b As Dictionary(Of Object, Object))
    Public Sub AppendDic(ByRef a As Object, b As Object)  'Dictionary(Of Object, Object), ByRef b As Dictionary(Of Object, Object))

        For Each k In b.Keys
            If Not a.ContainsKey(k) Then
                a.Add(k, b(k))
            Else
                '     Beep()  '    Stop 'duplicate value
            End If
        Next

    End Sub

    Public Function ScriptImage(func$) As WebControls.Image

        Dim img As WebControls.Image = New WebControls.Image
        img.ImageUrl = "/images/navigation/refresh.png"  'this is just *an* image (to attach the script to - it's not visible)
        img.Width = 1
        img.Height = 1
        img.Attributes("style") &= "position:absolute" 'take it out of the flow 
        img.Attributes.Add("onload", func$)

        Return img

    End Function


    Public Function additionalUpdates(parentBranch As clsBranch, buyeraccount As clsAccount, ByVal path$, ByRef errorMessages As List(Of String)) As List(Of clsVariant)

        'returns a dictionary of distis SKUS>ProductVariants

        additionalUpdates = New List(Of clsVariant) 'Dictionary(Of String, ClsProductVariant)
        'if it's a keyword search, the parent (ie. the branch we clicked on in the keyword results) could be out of date.. (for a 'normal' branch opening - it should have already been update by opening it's parent)

        If Not parentBranch.Product Is Nothing Then
            If parentBranch.Product.inFeed(buyeraccount.SellerChannel) Then
                'AppendDic(additionalUpdates, parentBranch.StalePrices(buyeraccount)) 'appends to needupdate()
                additionalUpdates.AddRange(parentBranch.StalePrices(buyeraccount, errorMessages))
                If parentBranch.Product.isSystem Then
                    Dim system As clsBranch = parentBranch
                    Dim preinstalled As List(Of clsQuantity) = system.GetPreInstalledRecursive(buyeraccount.SellerChannel.Region, path$, errorMessages)

                    For Each i In preinstalled
                        If i.Branch.Product IsNot Nothing Then
                            '    If Not i.FOC Then  - *DO* fetch Free of charge (FIO's) .. becuase we're very likely flex them (and second or or asubsequent ones *would* have a price)
                            If i.Branch.Product.inFeed(buyeraccount.SellerChannel) Then
                                'AppendDic(additionalUpdates, i.Branch.StalePrices(buyeraccount))
                                additionalUpdates.AddRange(i.Branch.StalePrices(buyeraccount, errorMessages))
                            End If
                        Else
                            Beep() '  TODO - why is it doing this ?
                        End If
                    Next i
                    Debug.Print("queued reqeusts for prices on " & additionalUpdates.Count & " preinstalled items")
                End If
            End If
        End If

    End Function



    Public Function LongSQL(ByVal SQL$, Optional ByVal ReturnIdentity As Boolean = False) As Integer
        'Runs aribitrary SQL with a 4 minute timeout (large deletions - during Initial data loads via PNA)

        'Dirty fix for the fact you can't control to connection timeout in dataaccess 
        'and i didn't have the time to do a new build (and get everyone to adopt it)

        'Dim sw As StreamWriter = New StreamWriter("c:\temp\tomlog.txt", True)
        'sw.WriteLine(SQL)
        'sw.Close()

        Dim con As SqlClient.SqlConnection
        con = da.OpenDatabase

        Dim com As System.Data.SqlClient.SqlCommand
        com = New SqlClient.SqlCommand(SQL$, con)
        com.CommandTimeout = 240

        LongSQL = com.ExecuteNonQuery() 'returns number of rows affected

        If ReturnIdentity Then
            com.CommandText = "select @@identity"
            LongSQL = CType(com.ExecuteScalar(), Integer)
        End If


        'sw = New StreamWriter("c:\temp\tomlog.txt", True)
        'sw.WriteLine(LongSQL)
        'sw.Close()



        con.Close()

    End Function



    Public Sub TidySwiftBranches(lid As UInt64)

        Dim b As clsBranch
        If iq.SeshContains(lid, "swiftStart") Then
            For i = iq.sesh(lid, "swiftEnd") To iq.sesh(lid, "swiftStart")   'swiftEnd is a more negative number than swiftstart
                b = iq.Branches(i)
                If b.Parent IsNot Nothing Then b.Parent.childBranches.Remove(b.ID)
                iq.Branches.Remove(i)
            Next
        End If



        ' iq.sesh(lid, "SwiftStart") = Nothing

    End Sub


    Public Function fetcherImage(path$, requestHandle As Integer, needUpdate As List(Of clsVariant)) As WebControls.Image

        'returns an image control with the script attached which will check for prices already requested from unitran

        Dim img As WebControls.Image
        img = New WebControls.Image
        img.CssClass = "fetcherImage"
        img.ImageUrl = eim$ & "resort.png" ' this is just AN arbitrary image - it's not visible
        'img.ImageUrl = "http://www.channelcentral.net/images/cloud_man_sml_focus.jpg"
        img.Width = 1
        img.Height = 1

        Dim script$

        'any Prices_div Beneath the div 'path' will be refreshed by a consolidated call to PriceRefresh.aspx
        script = "setTimeout (function(){fillPrices('" & path & "'," & requestHandle & ");return false;},3000)"   'fill Prices Calls GetPriceUis.aspx for - 3000 gives the page time to render - any less time and the filling becomes unreliable
        img.Attributes.Add("onload", script$)
        img.ToolTip = "Handle:" & requestHandle & " Price/stock was requested for : " & Join((From j In needUpdate Select j.Product.SKU).ToArray, ",") 'uses LINQ to assemble a CDlist of SKUS

        Return img

    End Function




    Public Function findFamily(path$, Optional ByRef famminor As String = "", Optional SystemOnly As Boolean = False, Optional includeSelf As Boolean = True) As String

        'Returns the family name (from the FamMajor attribute of the (family) branches stub product.

        findFamily = ""
        Dim i As Integer

        Dim branch As clsBranch
        Dim pa As clsProductAttribute

        Dim seg() As String = Split(path, ".")
        For i = UBound(seg) To 1 Step -1
            If Not includeSelf Then includeSelf = True : Continue For
            branch = iq.Branches(Val(seg((i))))
            If branch.Product IsNot Nothing AndAlso (Not SystemOnly OrElse branch.Product.isSystem) Then
                If branch.Product.i_Attributes_Code.ContainsKey("FamMinor") Then
                    famminor = branch.Product.i_Attributes_Code("FamMinor")(0).Translation.text(English)
                End If

                If branch.Product.i_Attributes_Code.ContainsKey("FamMajor") Then
                    pa = branch.Product.i_Attributes_Code("FamMajor")(0)
                    findFamily = pa.Translation.text(English)
                    Exit Function
                End If
            End If
        Next

    End Function
    Public Function SerializeToString(obj As Object) As String
        Dim serializer As New XmlSerializer(obj.[GetType]())
        Dim xns = New XmlSerializerNamespaces()
        xns.Add(String.Empty, String.Empty)
        Using writer As New StringWriter()
            serializer.Serialize(writer, obj, xns)

            Return writer.ToString()
        End Using
    End Function
    'Public Sub ExportExcelToPDF()
    '    Dim stream As FileStream = New FileStream("client_secrets.json", FileMode.Open, FileAccess.Read)

    '    Dim drive As List(Of String) = New List(Of String)
    '    'Dim localClientSecret As ClientSecrets = New ClientSecrets()
    '    'localClientSecret.ClientId = "520683653454-datlp7gs19vo40sp1lrrbskjtbcaa09h.apps.googleusercontent.com"
    '    'localClientSecret.ClientSecret = "CLIENT_SECRET_HERE"
    '    drive.Add(DriveService.Scope.Drive)
    '    Dim credential As UserCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, drive, "user", CancellationToken.None).Result

    '    ' Create the service.
    '    Dim serviceInitialiser As BaseClientService.Initializer = New BaseClientService.Initializer()
    '    serviceInitialiser.HttpClientInitializer = credential
    '    serviceInitialiser.ApplicationName = "Drive API Sample"

    '    Dim service = New DriveService(serviceInitialiser)

    '    Dim body As New Google.Apis.Drive.v2.Data.File()
    '    body.Title = "My document"
    '    body.Description = "A test document"
    '    body.MimeType = "text/plain"

    '    Dim byteArray As Byte() = System.IO.File.ReadAllBytes("document.txt")
    '    Dim stream2 As New System.IO.MemoryStream(byteArray)

    '    Dim request As FilesResource.InsertMediaUpload = service.Files.Insert(body, stream2, "text/plain")
    '    request.Upload()

    '    Dim file As Google.Apis.Drive.v2.Data.File = request.ResponseBody
    '    Console.WriteLine("File id: " + file.Id)
    '    Console.WriteLine("Press Enter to end this process.")
    '    Console.ReadLine()



    'End Sub


    Public Function getQuoteExport(quoteRootID As Integer) As DataTable
        Try
            Dim con As SqlClient.SqlConnection = da.OpenDatabase()
            Dim sqlQuery As String = "SELECT  qe.[ID],[FK_Quote_ID],[Type],[TimeStamp] , [Version] FROM [QuoteExport] qe inner join Quote q on [FK_Quote_ID] = q.ID where FK_Quote_ID_Root = " & quoteRootID
            Dim r As SqlClient.SqlDataReader = da.DBExecuteReader(con, sqlQuery)
            Dim dt As DataTable = New DataTable()
            dt.Load(r)
            Return dt
        Catch
        End Try
    End Function

    Public Function getQuoteVersionExports(QuoteID As Integer, agentaccount As clsAccount) As DataTable
        Try
            Dim con As SqlClient.SqlConnection = da.OpenDatabase()
            Dim sqlQuery As String = "SELECT  qe.[ID],[FK_Quote_ID],[Type],[TimeStamp] , [Version] FROM [QuoteExport] qe inner join Quote q on [FK_Quote_ID] = q.ID where q.id= " & QuoteID & " and q.fk_account_id_agent=" & agentaccount.ID
            Dim r As SqlClient.SqlDataReader = da.DBExecuteReader(con, sqlQuery)
            Dim dt As DataTable = New DataTable()
            dt.Load(r)
            Return dt
        Catch


        End Try

    End Function

    Public Function decodeTrebbleHash(code As String) As String
        'basic, this can be tuned
        code = Right(code, Len(code) - 3)
        code = code.Replace("_", " ")
        code = code.Replace(" Generic ", "")
        code = code.Substring(0, If(code.Length > 13, 13, code.Length))
        Return code
    End Function

    Function isSystemAbove(lid As UInt64, path As String) As Boolean
        For Each seg In path.Split(".").Reverse
            If path = "tree" Then Return False
            If path = "tree.1" Then Return False
            If iq.Branches(path.Split(".").Last).Product IsNot Nothing AndAlso iq.Branches(path.Split(".").Last).Product.isSystem Then Return True
            path = Left(path, Len(path) - Len(seg) - 1)
        Next
        Return False
    End Function


    Public Sub SwitchAccount(lid As UInt64, buyerAccount As clsAccount, agentAccount As clsAccount, errorMessages As List(Of String))


        SyncLock switchAccountLock
            Dim dic As Dictionary(Of String, Object) = iq.getSeshDic(lid)
            If dic.ContainsKey("AgentAccount") Then

                Dim uid As Integer
                Dim ag As clsAccount = dic("AgentAccount")
                Dim pwh As String = Nothing
                Dim md5 As String = Nothing
                Dim root As String = Nothing
                Dim accountList = Nothing
                Dim mfr As String = Nothing
                Dim base As String = Nothing
                Dim viaGatekeeper As Boolean?
                Dim gkPriceBand As String = Nothing
                Dim gkBasketUrl As String = Nothing
                Dim gkToken As String = Nothing
                Dim screenName As String = Nothing

                If dic.ContainsKey("passwordHash") Then pwh = dic("passwordHash")
                If dic.ContainsKey("passwordMD5") Then md5 = dic("passwordMD5")
                If dic.ContainsKey("Root") Then root = dic("Root")
                If dic.ContainsKey("UserID") Then uid = dic("UserID")
                If dic.ContainsKey("AccountList") Then accountList = dic("AccountList")
                If dic.ContainsKey("MFR") Then mfr = dic("MFR")
                If dic.ContainsKey("Base") Then base = dic("Base")
                If dic.ContainsKey("viaGatekeeper") Then viaGatekeeper = dic("viaGatekeeper")
                If dic.ContainsKey("gk_cPriceBand") Then gkPriceBand = dic("gk_cPriceBand")
                If dic.ContainsKey("gk_BasketURL") Then gkBasketUrl = dic("gk_BasketURL")
                If dic.ContainsKey("gk_Token") Then gkToken = dic("gk_Token")
                If dic.ContainsKey("screenName") Then screenName = dic("screenName")

                dic.Clear()
                dic.Add("UserID", uid)
                dic.Add("AgentAccount", ag)
                dic.Add("BuyerAccount", ag)
                If Not pwh Is Nothing Then dic.Add("passwordHash", pwh)
                If Not md5 Is Nothing Then dic.Add("passwordMD5", md5)
                If Not root Is Nothing Then dic.Add("Root", root)
                If Not mfr Is Nothing Then dic.Add("MFR", mfr)
                If Not base Is Nothing Then dic.Add("Base", base)
                If Not accountList Is Nothing Then dic.Add("AccountList", accountList)
                If viaGatekeeper.HasValue Then dic.Add("viaGatekeeper", viaGatekeeper)
                If Not gkPriceBand Is Nothing Then dic.Add("gk_cPriceBand", gkPriceBand)
                If Not gkBasketUrl Is Nothing Then dic.Add("gk_BasketURL", gkBasketUrl)
                If Not gkToken Is Nothing Then dic.Add("gk_Token", gkToken)
                If Not screenName Is Nothing Then dic.Add("screenName", screenName)
            End If

            iq.sesh(lid, "AgentAccount") = agentAccount   'Initially an agent of themself (might change when they choose a customer)
            iq.sesh(lid, "BuyerAccount") = buyerAccount
            If (iq.sesh(lid, "QuoteID") <> 0) Then iq.sesh(lid, "QuoteID") = 0
            iq.sesh(lid, "Paradigm") = enumParadigm.AddingSystem
            iq.updateLogin(lid, agentAccount)

            buyerAccount.SellerChannel.IsCloneOf.LoadVariants(errorMessages, 1)  'loads (and refreshes) them if neccessary

            If Not agentAccount.SellerChannel.IsCloneOf.stockLoaded Then
                agentAccount.SellerChannel.IsCloneOf.LoadStock()
            End If

            If Not agentAccount.SellerChannel.IsCloneOf.pricesLoadedFor.ContainsKey(agentAccount.Priceband) Then
                agentAccount.SellerChannel.IsCloneOf.LoadPrices(agentAccount.Priceband, errorMessages)
            End If

            If Not agentAccount.SellerChannel.IsCloneOf.pricesLoadedFor.ContainsKey(Everyone) Then
                agentAccount.SellerChannel.IsCloneOf.LoadPrices(Everyone, errorMessages)
            End If

            Dim rgn As clsRegion = agentAccount.SellerChannel.IsCloneOf.Region
            If Not HP.listPricesLoadedFor.ContainsKey(rgn) OrElse HP.listPricesLoadedFor(rgn) = 0 Then
                HP.LoadPrices(Everyone, errorMessages, agentAccount.SellerChannel.IsCloneOf.Region)
            End If

            iq.sesh(lid, "Root") = "tree.1"

        End SyncLock

    End Sub

    Public Function GetSplitMessage(quoteSplit As Manufacturer, language As clsLanguage) As String

        GetSplitMessage = String.Format("Due to the upcoming HP separation into HP Inc and Hewlett Packard Enterprise, PPS and EG products need to be quoted separately. " &
                                        "To quote {0} products, first save this quote and then create a new quote.", IIf(quoteSplit = Manufacturer.HPI, "EG", "PPS"))

        GetSplitMessage = Xlt(GetSplitMessage, language)

    End Function

    ' Attempts to look for a Universal request URL and infer the manufacturer from it.
    Public Function InferUniversalManufacturer(request As System.Web.HttpRequest) As String

        InferUniversalManufacturer = Nothing

        If Not request Is Nothing AndAlso Not iq.Addresses Is Nothing Then

            Dim requestHost = request.Url.Host.ToLower()

            If requestHost.Contains("hpiquote.channelcentral.net") OrElse requestHost.Contains("iquote.hp.com") OrElse (iq.Addresses.ContainsKey("HPIUniversalHost") AndAlso requestHost.Contains(iq.Addresses("HPIUniversalHost").Translation.text(English))) Then
                InferUniversalManufacturer = "HPI"
            ElseIf requestHost.Contains("hpeiquote.channelcentral.net") OrElse requestHost.Contains("iquote.hpe.com") OrElse (iq.Addresses.ContainsKey("HPEUniversalHost") AndAlso requestHost.Contains(iq.Addresses("HPEUniversalHost").Translation.text(English))) Then
                InferUniversalManufacturer = "HPE"
            End If

        End If

    End Function

    Public Sub Log4NetMessage(message As String)

        If (Not log4net.LogManager.GetRepository().Configured) Then
            XmlConfigurator.Configure()
        End If
        log.Info(message)

    End Sub

End Module
